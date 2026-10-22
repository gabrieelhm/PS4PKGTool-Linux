#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;

namespace PS4PKGTool.Utilities.Ffpfsc;

/// <summary>
/// Inspects, validates, and repairs the trailing SHA-1 footer of a PS4
/// <c>sce_sys/npbind.dat</c> file. Ported from the SadykovIV
/// <c>ps4ffpsc/npbind.py</c> reference. The npbind.dat format is:
/// <list type="bullet">
/// <item>0x80-byte (128) header — magic, version, declared_size, entry_size, entry_count (big-endian <c>&gt;IIQQQ</c> in the first 28 bytes, zero-padded to 128).</item>
/// <item><c>entry_count</c> records of 0x180 (384) bytes each.</item>
/// <item>20-byte SHA-1 digest over every preceding byte (header + all entries).</item>
/// </list>
/// After a dump-tree merge, only the footer is recomputed — the header and
/// body records are left untouched. The repair operates on a temp copy and
/// never mutates the user's original dump source.
/// </summary>
public static class NpbindFooterRepair
{
    /// <summary>npbind.dat magic value (big-endian on disk).</summary>
    public const uint Magic = 0xD294A018;

    /// <summary>Expected npbind.dat version field.</summary>
    public const uint ExpectedVersion = 1;

    /// <summary>Header size in bytes (the struct fields occupy 28 bytes, zero-padded to 128).</summary>
    public const int HeaderSize = 0x80;

    /// <summary>Per-entry size in bytes.</summary>
    public const int EntrySize = 0x180;

    /// <summary>Trailing SHA-1 digest length in bytes.</summary>
    public const int DigestSize = 20;

    /// <summary>Minimum valid file length: header + one entry + digest.</summary>
    public const int MinimumLength = HeaderSize + EntrySize + DigestSize;

    /// <summary>
    /// Inspects an npbind.dat byte array and returns a structured report.
    /// Mirrors <c>inspect_npbind</c>: validates magic, version, declared size,
    /// entry size, and entry layout, then compares the trailing SHA-1. Never
    /// throws — callers decide whether to repair based on <see cref="Status"/>.
    /// </summary>
    public static NpbindReport Inspect(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < HeaderSize + DigestSize)
            return NpbindReport.Invalid("too small", data.Length);

        uint magic = ReadUInt32BigEndian(data, 0);
        if (magic != Magic)
            return NpbindReport.Invalid($"magic mismatch: 0x{magic:X8}", data.Length);

        uint version = ReadUInt32BigEndian(data, 4);
        if (version != ExpectedVersion)
            return NpbindReport.Invalid($"unsupported version: {version}", data.Length);

        long declaredSize = ReadInt64BigEndian(data, 8);
        if (declaredSize != data.Length)
            return NpbindReport.Invalid(
                $"declared size {declaredSize} does not match actual size {data.Length}", data.Length);

        long entrySize = ReadInt64BigEndian(data, 16);
        if (entrySize != EntrySize)
            return NpbindReport.Invalid(
                $"entry size 0x{entrySize:X} does not match expected 0x{EntrySize:X}", data.Length);

        long entryCount = ReadInt64BigEndian(data, 24);
        if (entryCount <= 0)
            return NpbindReport.Invalid($"entry count must be positive, got {entryCount}", data.Length);

        long expectedSize = HeaderSize + entryCount * EntrySize + DigestSize;
        if (expectedSize != data.Length)
            return NpbindReport.Invalid(
                $"expected size {expectedSize} does not match actual size {data.Length}", data.Length);

        byte[] storedHash = data.AsSpan(data.Length - DigestSize, DigestSize).ToArray();
        byte[] computedHash = SHA1.HashData(data.AsSpan(0, data.Length - DigestSize));
        bool footerValid = storedHash.AsSpan().SequenceEqual(computedHash);

        return new NpbindReport(
            Status: footerValid ? "valid" : "repairable_footer",
            Magic: magic,
            Version: version,
            DeclaredSize: declaredSize,
            EntrySize: entrySize,
            EntryCount: entryCount,
            FooterValid: footerValid,
            Sha1: computedHash);
    }

    /// <summary>
    /// Strictly validates an npbind.dat file. Throws <see cref="InvalidDataException"/>
    /// when the structure is invalid or the footer does not match. Mirrors
    /// <c>validate_npbind</c>.
    /// </summary>
    public static NpbindReport Validate(byte[] data)
    {
        NpbindReport report = Inspect(data);
        if (report.Status == "invalid")
            throw new InvalidDataException($"npbind.dat {report.StatusDetail}.");
        if (!report.FooterValid)
            throw new InvalidDataException(
                $"npbind.dat SHA-1 footer mismatch: stored digest does not match recomputed SHA-1.");
        return report;
    }

    /// <summary>
    /// Recomputes the SHA-1 footer on a copy of <paramref name="data"/>.
    /// Returns the repaired copy when the structure is valid but the footer was
    /// wrong; returns the input unchanged when the footer was already correct.
    /// Throws <see cref="InvalidDataException"/> when the structure itself is
    /// invalid (a bad footer alone is repairable; a bad structure is not).
    /// </summary>
    public static byte[] Repair(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        NpbindReport report = Inspect(data);
        if (report.Status == "invalid")
            throw new InvalidDataException($"npbind.dat is structurally invalid: {report.StatusDetail}");
        if (report.FooterValid)
            return data; // already correct — no copy needed

        byte[] output = (byte[])data.Clone();
        byte[] hash = SHA1.HashData(output.AsSpan(0, output.Length - DigestSize));
        Buffer.BlockCopy(hash, 0, output, output.Length - DigestSize, DigestSize);
        return output;
    }

    /// <summary>
    /// Non-throwing check: true when the data is structurally valid AND the
    /// SHA-1 footer matches. Convenience wrapper over <see cref="Inspect"/>.
    /// </summary>
    public static bool Verify(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        NpbindReport report = Inspect(data);
        return report.FooterValid;
    }

    /// <summary>
    /// Reads the npbind.dat from <paramref name="sourcePath"/>, recomputes its
    /// SHA-1 footer, and writes the result to <paramref name="destinationPath"/>.
    /// The source file is never modified. Throws when the structure is invalid.
    /// </summary>
    public static void Repair(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        byte[] data = File.ReadAllBytes(sourcePath);
        byte[] repaired = Repair(data);

        string? directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(destinationPath, repaired);
    }

    /// <summary>
    /// Convenience: repairs <c>sce_sys/npbind.dat</c> inside
    /// <paramref name="gameRootDirectory"/> in place, only when the file exists
    /// and its footer is wrong. Used by the converter pipeline on the temp
    /// merged copy — never on the user's original dump source.
    /// </summary>
    public static bool TryRepairInPlace(string gameRootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameRootDirectory);
        string path = Path.Combine(gameRootDirectory, "sce_sys", "npbind.dat");
        if (!File.Exists(path)) return false;

        byte[] data = File.ReadAllBytes(path);
        byte[] repaired = Repair(data);
        if (ReferenceEquals(repaired, data)) return false; // already valid

        File.WriteAllBytes(path, repaired);
        return true;
    }

    private static uint ReadUInt32BigEndian(byte[] data, int offset) =>
        (uint)((uint)data[offset] << 24 | (uint)data[offset + 1] << 16 |
               (uint)data[offset + 2] << 8 | data[offset + 3]);

    private static long ReadInt64BigEndian(byte[] data, int offset)
    {
        long value = 0;
        for (int i = 0; i < 8; i++)
            value = (value << 8) | data[offset + i];
        return value;
    }
}

/// <summary>Structured inspection result for an npbind.dat file.</summary>
public sealed record NpbindReport(
    string Status,
    uint Magic,
    uint Version,
    long DeclaredSize,
    long EntrySize,
    long EntryCount,
    bool FooterValid,
    byte[] Sha1)
{
    /// <summary>Creates an "invalid" report with zeroed fields.</summary>
    internal static NpbindReport Invalid(string detail, int actualLength) => new(
        Status: "invalid",
        Magic: 0,
        Version: 0,
        DeclaredSize: actualLength,
        EntrySize: 0,
        EntryCount: 0,
        FooterValid: false,
        Sha1: [])
    {
        StatusDetail = detail
    };

    /// <summary>Human-readable detail for an invalid report; null for valid/repairable.</summary>
    public string? StatusDetail { get; internal set; }
}
