#nullable enable
using System;
using System.Collections.Generic;

namespace PS4PKGTool.Utilities.TrophyMetadata
{
    public enum TrophyGrade
    {
        Unknown,
        Bronze,
        Silver,
        Gold,
        Platinum
    }

    public enum TrophyResourceKind
    {
        Unknown,
        Png,
        Xml,
        EncryptedEsfm,
        EsfmMarker,
        EmptyMarker
    }

    public enum TrophyMetadataFailureKind
    {
        None,
        MissingMetadata,
        UnsupportedMetadata,
        MissingNpCommunicationId,
        DecryptionFailed,
        ParseFailed
    }

    public sealed class TrpEntry
    {
        public int Index { get; init; }
        public string Name { get; init; } = string.Empty;
        public long Offset { get; init; }
        public long Size { get; init; }
        public uint Flags { get; init; }
        public byte[] RawMetadata { get; init; } = Array.Empty<byte>();
    }

    public sealed class TrpArchive
    {
        public string FilePath { get; init; } = string.Empty;
        public uint Version { get; init; }
        public long DeclaredFileSize { get; init; }
        public uint EntrySize { get; init; }
        public uint DevelopmentFlag { get; init; }
        public byte[] Sha1 { get; init; } = Array.Empty<byte>();
        public IReadOnlyList<TrpEntry> Entries { get; init; } = Array.Empty<TrpEntry>();
    }

    public sealed class TrophyInfo
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public TrophyGrade Grade { get; init; }
        public bool IsHidden { get; init; }
        public int? GroupId { get; init; }
        public string GroupName { get; init; } = "Base Game";
        public int? PlatinumId { get; init; }
        public byte[]? IconData { get; init; }
        public string IconEntryName { get; init; } = string.Empty;
    }

    public sealed class TrophyMetadataResult
    {
        public IReadOnlyList<TrophyInfo> Trophies { get; init; } = Array.Empty<TrophyInfo>();
        public TrpArchive Archive { get; init; } = new TrpArchive();
        public string? NpCommunicationId { get; init; }
        public string MetadataEntryName { get; init; } = string.Empty;
        public bool MetadataPayloadFound { get; init; }
        public bool DecryptionAttempted { get; init; }
        public bool DecryptionSucceeded { get; init; }
        public TrophyMetadataFailureKind FailureKind { get; init; }
        public string StatusMessage { get; init; } = string.Empty;
    }
}
