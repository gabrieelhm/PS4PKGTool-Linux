using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PS4PKGTool.Startup
{
    /// <summary>Shell actions the Explorer integration can request.</summary>
    public enum ShellCommandKind
    {
        Copy,
        Rename,
        Validate,
        Extract,
        InstallShadps4,
    }

    /// <summary>A validated shell request. FormatId is used by Rename only; Field by Copy only.</summary>
    public sealed record ShellRequest(
        ShellCommandKind Kind,
        string Field,
        int FormatId,
        IReadOnlyList<string> PackagePaths);

    /// <summary>
    /// Central parser for "PS4PKGTool.exe --shell &lt;command&gt; &lt;arg&gt; &lt;paths...&gt;".
    /// Explorer passes one path per quoted argument, so the args array is the
    /// authoritative source - nothing is re-joined or shell-interpreted.
    /// </summary>
    public static class ShellCommandRouter
    {
        public const string ShellArg = "--shell";

        public static bool IsShellMode(string[] args)
            => args != null && args.Length > 0
                && string.Equals(args[0], ShellArg, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Parses the args following --shell into a request, or null when the
        /// command is unknown or malformed (never throws).
        /// </summary>
        public static ShellRequest? Parse(string[] args)
        {
            if (args == null || args.Length < 2) return null;

            string command = args[1].ToLowerInvariant();
            int index = 2;

            switch (command)
            {
                case "copy":
                    if (args.Length < 4) return null;
                    string field = args[2].ToLowerInvariant();
                    if (!CopyFields.Contains(field)) return null;
                    return new ShellRequest(ShellCommandKind.Copy, field, 0, Args(args, 3));

                case "rename":
                    if (args.Length < 4 || !int.TryParse(args[2], out int formatId))
                        return null;
                    return new ShellRequest(ShellCommandKind.Rename, "", formatId, Args(args, 3));

                case "validate":
                    return new ShellRequest(ShellCommandKind.Validate, "", 0, Args(args, index));

                case "extract":
                    return new ShellRequest(ShellCommandKind.Extract, "", 0, Args(args, index));

                case "install-shadps4":
                    return new ShellRequest(ShellCommandKind.InstallShadps4, "", 0, Args(args, index));

                default:
                    return null;
            }
        }

        private static IReadOnlyList<string> Args(string[] args, int from)
        {
            var list = new List<string>();
            for (int i = from; i < args.Length; i++)
                if (!string.IsNullOrWhiteSpace(args[i]))
                    list.Add(args[i]);
            return list;
        }

        /// <summary>
        /// Validates every path: exists, is a file (not a directory), has a
        /// .pkg extension. Returns a user-facing error, or null when all are
        /// acceptable.
        /// </summary>
        public static string? ValidatePaths(IReadOnlyList<string> paths)
        {
            if (paths == null || paths.Count == 0)
                return "No PKG files were passed to the shell action.";

            foreach (string path in paths)
            {
                string full;
                try { full = Path.GetFullPath(path); }
                catch { return $"The path is not valid: {path}"; }

                if (File.Exists(full))
                {
                    if (!string.Equals(Path.GetExtension(full), ".pkg", StringComparison.OrdinalIgnoreCase))
                        return $"Not a PKG file: {Path.GetFileName(full)}";
                }
                else if (Directory.Exists(full))
                {
                    return $"The path is a folder, not a PKG file: {full}";
                }
                else
                {
                    return $"The PKG file was not found: {full}";
                }
            }
            return null;
        }

        /// <summary>Copy fields, in menu order.</summary>
        public static readonly string[] CopyFields =
        {
            "info", "title", "titleid", "contentid", "version", "appversion", "fullpath",
        };
    }
}
