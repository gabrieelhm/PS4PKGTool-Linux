using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace PS4PKGTool.Utilities
{
    internal static class WorkflowGuards
    {
        internal static decimal ParseCombinedAppVersion(string? appVersion)
        {
            if (string.IsNullOrWhiteSpace(appVersion)) return 0m;
            int open = appVersion.LastIndexOf('[');
            int close = appVersion.LastIndexOf(']');
            string value = open >= 0 && close > open
                ? appVersion.Substring(open + 1, close - open - 1)
                : appVersion.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal version)
                ? version
                : 0m;
        }

        internal static void VerifyDownloadedPiece(string path, long actualSize, long expectedSize, string expectedSha256)
        {
            if (expectedSize > 0 && actualSize != expectedSize)
                throw new InvalidDataException($"Downloaded size is {actualSize} bytes; expected {expectedSize} bytes.");

            string expected = new string((expectedSha256 ?? "").Where(Uri.IsHexDigit).ToArray());
            if (expected.Length != 64)
                throw new InvalidDataException("The update manifest contains an invalid SHA-256 value.");
            using var stream = File.OpenRead(path);
            string actual = Convert.ToHexString(SHA256.HashData(stream));
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Downloaded SHA-256 does not match the update manifest.");
        }
    }
}
