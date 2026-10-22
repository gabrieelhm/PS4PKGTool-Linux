#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace PS4PKGTool.Utilities.TrophyMetadata
{
    public sealed class TrophyMetadataService
    {
        private readonly TrpReader _reader = new();
        private readonly TrophyResourceDetector _detector = new();
        private readonly NpCommunicationIdResolver _idResolver = new();
        private readonly EsfmDecryptor _decryptor = new();
        private readonly TrophyMetadataParser _parser = new();

        public TrophyMetadataResult Read(string trpPath, string? explicitNpCommunicationId = null, IEnumerable<string>? idSourcePaths = null)
        {
            TrpArchive archive = _reader.Read(trpPath);
            TrpEntry? metadataEntry = SelectMetadataEntry(archive);
            if (metadataEntry == null)
                return Result(archive, null, null, false, false, false,
                    TrophyMetadataFailureKind.MissingMetadata,
                    "Metadata unavailable: no trophy XML or ESFM entry was found.");

            if (metadataEntry.Size > 32 * 1024 * 1024)
                return Result(archive, metadataEntry, null, true, false, false,
                    TrophyMetadataFailureKind.UnsupportedMetadata,
                    $"Metadata unavailable: {metadataEntry.Name} exceeds the 32 MiB safety limit.");

            byte[] payload = _reader.ReadEntry(archive, metadataEntry);
            TrophyResourceKind kind = _detector.Detect(payload, metadataEntry);
            if (kind == TrophyResourceKind.Xml)
                return ParseAndAttach(archive, metadataEntry, payload, explicitNpCommunicationId, false);
            if (kind != TrophyResourceKind.EncryptedEsfm)
                return Result(archive, metadataEntry, null, true, false, false,
                    TrophyMetadataFailureKind.UnsupportedMetadata,
                    $"Metadata unavailable: {metadataEntry.Name} is {kind}, not a supported ESFM payload.");

            string? npCommunicationId;
            try
            {
                npCommunicationId = _idResolver.Resolve(explicitNpCommunicationId, trpPath, idSourcePaths);
            }
            catch (Exception ex) when (ex is ArgumentException or System.IO.InvalidDataException)
            {
                return Result(archive, metadataEntry, null, true, false, false,
                    TrophyMetadataFailureKind.MissingNpCommunicationId,
                    $"Metadata unavailable: {ex.Message}");
            }
            if (npCommunicationId == null)
                return Result(archive, metadataEntry, null, true, false, false,
                    TrophyMetadataFailureKind.MissingNpCommunicationId,
                    "Metadata unavailable: NP Communication ID is required (NPWRxxxxx_00). No value was found in nearby npbind.dat, nptitle.dat, or param.sfo files.");

            try
            {
                byte[] xml = _decryptor.Decrypt(payload, npCommunicationId);
                return ParseAndAttach(archive, metadataEntry, xml, npCommunicationId, true);
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                return Result(archive, metadataEntry, npCommunicationId, true, true, false,
                    TrophyMetadataFailureKind.DecryptionFailed,
                    $"Metadata unavailable: {ex.Message}");
            }
        }

        private TrophyMetadataResult ParseAndAttach(TrpArchive archive, TrpEntry entry, byte[] xml, string? expectedId, bool decryptionAttempted)
        {
            try
            {
                IReadOnlyList<TrophyInfo> trophies = _parser.Parse(xml, expectedId);
                trophies = new TrophyIconResolver(_reader, _detector).AttachIcons(archive, trophies);
                return new TrophyMetadataResult
                {
                    Archive = archive,
                    Trophies = trophies,
                    NpCommunicationId = expectedId,
                    MetadataEntryName = entry.Name,
                    MetadataPayloadFound = true,
                    DecryptionAttempted = decryptionAttempted,
                    DecryptionSucceeded = decryptionAttempted,
                    FailureKind = TrophyMetadataFailureKind.None,
                    StatusMessage = $"Loaded {trophies.Count} trophies from {entry.Name}."
                };
            }
            catch (Exception ex)
            {
                return Result(archive, entry, expectedId, true, decryptionAttempted, false,
                    TrophyMetadataFailureKind.ParseFailed,
                    $"Metadata unavailable: {ex.Message}");
            }
        }

        private static TrpEntry? SelectMetadataEntry(TrpArchive archive)
        {
            string[] preferred = { "TROP.ESFM", "TROP.SFM", "TROPCONF.ESFM", "TROPCONF.SFM" };
            foreach (string name in preferred)
            {
                TrpEntry? match = archive.Entries.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            return archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".ESFM", StringComparison.OrdinalIgnoreCase) || e.Name.EndsWith(".SFM", StringComparison.OrdinalIgnoreCase));
        }

        private static TrophyMetadataResult Result(
            TrpArchive archive,
            TrpEntry? entry,
            string? id,
            bool found,
            bool attempted,
            bool succeeded,
            TrophyMetadataFailureKind failureKind,
            string message) => new()
        {
            Archive = archive,
            NpCommunicationId = id,
            MetadataEntryName = entry?.Name ?? string.Empty,
            MetadataPayloadFound = found,
            DecryptionAttempted = attempted,
            DecryptionSucceeded = succeeded,
            FailureKind = failureKind,
            StatusMessage = message
        };
    }
}
