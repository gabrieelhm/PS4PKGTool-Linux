using System;
using System.IO;
using System.Text.Json;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>
    /// Per-snapshot metadata in a save backup folder. A folder only counts as
    /// a managed backup when its manifest is present and valid - identity is
    /// never inferred from directory names alone.
    /// </summary>
    public sealed class Shadps4SaveManifest
    {
        public const int CurrentSchemaVersion = 1;

        /// <summary>"offline" = emulator was stopped; "live_unverified" = captured mid-run, files may mix states.</summary>
        public const string ConsistencyOffline = "offline";
        public const string ConsistencyLiveUnverified = "live_unverified";

        public int SchemaVersion { get; set; }
        public string SnapshotId { get; set; } = "";
        public string TitleId { get; set; } = "";
        public string UserId { get; set; } = "";
        public DateTime BackedUpAtUtc { get; set; }
        /// <summary>Informational only - never used to resolve restore sources.</summary>
        public string OriginalSourcePath { get; set; } = "";
        public string Consistency { get; set; } = ConsistencyOffline;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>Writes the manifest JSON next to the snapshot content.</summary>
        public static void Write(string manifestPath, Shadps4SaveManifest manifest)
            => File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));

        /// <summary>Reads and validates a manifest. Corrupt, foreign-schema or empty manifests return null.</summary>
        public static Shadps4SaveManifest? TryLoad(string manifestPath)
        {
            try
            {
                if (!File.Exists(manifestPath)) return null;
                var m = JsonSerializer.Deserialize<Shadps4SaveManifest>(File.ReadAllText(manifestPath), JsonOptions);
                if (m == null || m.SchemaVersion != CurrentSchemaVersion) return null;
                if (string.IsNullOrWhiteSpace(m.SnapshotId)) return null;
                if (string.IsNullOrWhiteSpace(m.TitleId)) return null;
                if (string.IsNullOrWhiteSpace(m.UserId)) return null;
                if (m.BackedUpAtUtc == default) return null;
                return m;
            }
            catch
            {
                return null;
            }
        }
    }
}
