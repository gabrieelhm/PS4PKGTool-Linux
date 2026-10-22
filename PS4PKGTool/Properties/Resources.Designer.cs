using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace PS4PKGTool.Properties
{
    public static class Resources
    {
        private static readonly ConcurrentDictionary<string, byte[]> _cache = new(StringComparer.OrdinalIgnoreCase);

        private static byte[] LoadResourceBytes(string fileName)
        {
            return _cache.GetOrAdd(fileName, key =>
            {
                string[] candidates =
                {
                    Path.Combine(AppContext.BaseDirectory, "Resources", key),
                    Path.Combine(AppContext.BaseDirectory, key)
                };
                foreach (var candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        try { return File.ReadAllBytes(candidate); }
                        catch { }
                    }
                }

                var asm = typeof(Resources).Assembly;
                foreach (var resName in asm.GetManifestResourceNames())
                {
                    if (resName.EndsWith("." + key, StringComparison.OrdinalIgnoreCase)
                        || resName.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = asm.GetManifestResourceStream(resName);
                        if (stream != null)
                        {
                            using var ms = new MemoryStream();
                            stream.CopyTo(ms);
                            return ms.ToArray();
                        }
                    }
                }
                return Array.Empty<byte>();
            });
        }

        public static byte[] asia => LoadResourceBytes("asia.png");
        public static byte[] eu => LoadResourceBytes("eu.png");
        public static byte[] folder => LoadResourceBytes("Folder_16x.png");
        public static byte[] hk => LoadResourceBytes("hk.png");
        public static byte[] Image => LoadResourceBytes("Image_16x.png");
        public static byte[] jp => LoadResourceBytes("jp.png");
        public static byte[] kr => LoadResourceBytes("kr.png");
        public static byte[] PackageIcon => LoadResourceBytes("PackageIcon.png");
        public static byte[] us => LoadResourceBytes("us.png");

        public static byte[] tv_audio => LoadResourceBytes("audio.png");
        public static byte[] tv_binary => LoadResourceBytes("binary.png");
        public static byte[] tv_code => LoadResourceBytes("code.png");
        public static byte[] tv_config => LoadResourceBytes("config.png");
        public static byte[] tv_document => LoadResourceBytes("document.png");
        public static byte[] tv_file_unknown => LoadResourceBytes("file-unknown.png");
        public static byte[] tv_folder => LoadResourceBytes("folder.png");
        public static byte[] tv_folder_open => LoadResourceBytes("folder-open.png");
        public static byte[] tv_image => LoadResourceBytes("image.png");
        public static byte[] tv_package => LoadResourceBytes("package.png");
        public static byte[] tv_video => LoadResourceBytes("video.png");
    }
}
