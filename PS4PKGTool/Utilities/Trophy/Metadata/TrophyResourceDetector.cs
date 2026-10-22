#nullable enable
using System;

namespace PS4PKGTool.Utilities.TrophyMetadata
{
    public sealed class TrophyResourceDetector
    {
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] EsfmMarker = { 0x45, 0x53, 0x46, 0x4D, 0, 0, 0, 0 };

        public TrophyResourceKind Detect(ReadOnlySpan<byte> data, TrpEntry? entry = null)
        {
            if (data.StartsWith(PngSignature))
                return TrophyResourceKind.Png;
            if (data.StartsWith(EsfmMarker) && data.Length == EsfmMarker.Length)
                return TrophyResourceKind.EsfmMarker;
            if (data.Length == 8 && IsAllZero(data))
                return TrophyResourceKind.EmptyMarker;

            ReadOnlySpan<byte> trimmed = TrimLeadingWhitespace(data);
            if (trimmed.StartsWith("<?xml"u8) || trimmed.StartsWith("<trophy"u8) || trimmed.StartsWith("<!--"u8))
                return TrophyResourceKind.Xml;

            if (entry != null && entry.Name.EndsWith(".ESFM", StringComparison.OrdinalIgnoreCase) &&
                entry.Flags == 3 && data.Length >= 32 && data.Length % 16 == 0)
                return TrophyResourceKind.EncryptedEsfm;

            return TrophyResourceKind.Unknown;
        }

        private static bool IsAllZero(ReadOnlySpan<byte> data)
        {
            foreach (byte value in data)
                if (value != 0) return false;
            return true;
        }

        private static ReadOnlySpan<byte> TrimLeadingWhitespace(ReadOnlySpan<byte> data)
        {
            int index = 0;
            while (index < data.Length && data[index] is 0x09 or 0x0A or 0x0D or 0x20)
                index++;
            return data[index..];
        }
    }
}
