using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using PS4PKGTool.Utilities.PS4PKGToolHelper;

namespace PS4PKGTool.Utilities.Shadps4
{
    public sealed class Shadps4BuildManifest
    {
        public string ManagedBy { get; set; } = "PS4PKGTool";
        public string Component { get; set; } = "";      // "core" | "qtlauncher"
        public string Repository { get; set; } = "";
        public string Release { get; set; } = "";         // upstream release tag
        public string Commit { get; set; } = "";
        public string Asset { get; set; } = "";
        public string InstalledAtUtc { get; set; } = "";
    }

    public sealed record Shadps4InstalledBuild(
        string BuildId,
        Shadps4Component Component,
        string DirectoryPath,
        Shadps4BuildManifest Manifest,
        string ExecutablePath)
    {
        private static readonly Regex DateInTag = new(@"\d{4}-\d{2}-\d{2}", RegexOptions.Compiled);

        public string DisplayName
        {
            get
            {
                if (!LooksLikeSha(BuildId)) return BuildId;
                string date = DateInTag.Match(Manifest.Release ?? "").Value;
                string shortSha = BuildId.Length > 8 ? BuildId.Substring(0, 8) : BuildId;
                return string.IsNullOrEmpty(date) ? BuildId : $"{date} ({shortSha})";
            }
        }

        private static bool LooksLikeSha(string id)
            => id.Length == 40 && id.All(Uri.IsHexDigit);
    }

    public sealed class Shadps4ManagedBuilds
    {
        public const string ManifestFileName = "ps4pkgtool-manifest.json";

        public static string DefaultRootPath()
            => Path.Combine(Helper.UserDataDirectory, "shadPS4");

        private readonly string _root;

        public Shadps4ManagedBuilds(string? rootPath = null, IClock? clock = null)
        {
            _root = string.IsNullOrWhiteSpace(rootPath) ? DefaultRootPath() : rootPath;
            Clock = clock ?? new SystemClock();
        }

        public IClock Clock { get; }

        public string RootPath => _root;

        private string BuildsRoot => Path.Combine(_root, "builds");

        public string BuildDirectory(Shadps4Component component, string buildId)
            => Path.Combine(BuildsRoot, ComponentPrefix(component) + buildId);

        public Shadps4InstalledBuild Commit(Shadps4ReleaseInfo release, string stagingDir)
        {
            Shadps4Component component = release.Feed == Shadps4FeedKind.QtLauncher
                ? Shadps4Component.QtLauncher
                : Shadps4Component.Core;

            string buildId = release.BuildId;
            string finalDir = BuildDirectory(component, buildId);
            if (Directory.Exists(finalDir))
                throw new InvalidOperationException($"Managed build already exists: {finalDir}");

            string? expectedExe = Shadps4EnvironmentResolver.FindExecutableInDirectory(stagingDir, component);
            if (expectedExe == null || !File.Exists(expectedExe))
            {
                string exeName = component == Shadps4Component.Core
                    ? Shadps4EnvironmentResolver.CoreExeFileName
                    : Shadps4EnvironmentResolver.QtLauncherFileName;
                throw new InvalidOperationException(
                    $"Extracted build does not contain {exeName}.");
            }

            EnsureExecutablePermission(expectedExe);

            var manifest = new Shadps4BuildManifest
            {
                Component = component == Shadps4Component.Core ? "core" : "qtlauncher",
                Repository = release.Repository,
                Release = release.Tag,
                Commit = release.Commit,
                Asset = release.AssetName,
                InstalledAtUtc = Clock.UtcNow.ToString("O"),
            };
            File.WriteAllText(Path.Combine(stagingDir, ManifestFileName),
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            Directory.CreateDirectory(BuildsRoot);
            Directory.Move(stagingDir, finalDir);

            string? finalExe = Shadps4EnvironmentResolver.FindExecutableInDirectory(finalDir, component)
                ?? Path.Combine(finalDir, Path.GetFileName(expectedExe));
            EnsureExecutablePermission(finalExe);

            return new Shadps4InstalledBuild(buildId, component, finalDir, manifest, finalExe);
        }

        public IReadOnlyList<Shadps4InstalledBuild> ListBuilds(Shadps4Component component)
        {
            var result = new List<Shadps4InstalledBuild>();
            if (!Directory.Exists(BuildsRoot)) return result;

            string prefix = ComponentPrefix(component);
            foreach (string dir in Directory.GetDirectories(BuildsRoot))
            {
                string name = Path.GetFileName(dir);
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

                var manifest = ReadManifest(dir);
                if (manifest == null) continue;

                string? exe = Shadps4EnvironmentResolver.FindExecutableInDirectory(dir, component);
                if (exe == null || !File.Exists(exe)) continue;

                result.Add(new Shadps4InstalledBuild(
                    name[prefix.Length..], component, dir, manifest, exe));
            }
            return result.OrderByDescending(b => b.Manifest.InstalledAtUtc).ToList();
        }

        public string? FindExe(Shadps4Component component, string buildId)
        {
            var build = ListBuilds(component).FirstOrDefault(b =>
                string.Equals(b.BuildId, buildId, StringComparison.OrdinalIgnoreCase));
            return build?.ExecutablePath;
        }

        public string? ResolveManagedExecutable(string buildId)
            => FindExe(Shadps4Component.Core, buildId)
                ?? FindExe(Shadps4Component.QtLauncher, buildId);

        public void DeleteBuild(Shadps4Component component, string buildId)
        {
            string dir = BuildDirectory(component, buildId);
            if (!Directory.Exists(dir)) return;
            try
            {
                Directory.Delete(dir, true);
            }
            catch (Exception ex)
            {
                throw new IOException("Could not remove the build folder:\n" + dir + "\n\n" + ex.Message);
            }
        }

        private static void EnsureExecutablePermission(string path)
        {
            try
            {
                if (!OperatingSystem.IsWindows() && File.Exists(path))
                {
                    File.SetUnixFileMode(path,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }
            }
            catch { }
        }

        private static Shadps4BuildManifest? ReadManifest(string dir)
        {
            try
            {
                string path = Path.Combine(dir, ManifestFileName);
                if (!File.Exists(path)) return null;
                return JsonSerializer.Deserialize<Shadps4BuildManifest>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        private static string ComponentPrefix(Shadps4Component component)
            => component == Shadps4Component.Core ? "core-" : "launcher-";
    }
}
