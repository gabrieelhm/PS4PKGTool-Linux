using System;
using System.Collections.Generic;

namespace PS4PKGTool.Utilities.PkgInspection
{
    public sealed class PkgSfoEntry
    {
        public PkgSfoEntry(string name, string value)
        {
            Name = name ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string Name { get; }
        public string Value { get; }
    }

    public sealed class PkgInspectionField
    {
        public PkgInspectionField(string name, string value)
        {
            Name = name ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string Name { get; }
        public string Value { get; }
    }

    public sealed class PkgInspectionSnapshot : IDisposable
    {
        private bool _disposed;

        public string PackagePath { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string TitleId { get; init; } = string.Empty;
        public string ContentId { get; init; } = string.Empty;
        public string PackageCategory { get; init; } = string.Empty;
        public string PackageState { get; init; } = string.Empty;
        public string ApplicationVersion { get; init; } = string.Empty;
        public string PackageVersion { get; init; } = string.Empty;
        public string RequiredFirmware { get; init; } = string.Empty;
        public string PackageSize { get; init; } = string.Empty;
        public IReadOnlyList<PkgSfoEntry> SfoEntries { get; init; } = Array.Empty<PkgSfoEntry>();
        public IReadOnlyList<PkgInspectionField> HeaderFields { get; init; } = Array.Empty<PkgInspectionField>();
        public IReadOnlyList<PkgInspectionField> BuildInfoFields { get; init; } = Array.Empty<PkgInspectionField>();
        public byte[] Icon0Bytes { get; init; }
        public byte[] Pic0Bytes { get; init; }
        public byte[] Pic1Bytes { get; init; }

        internal bool IsDisposed => _disposed;

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
