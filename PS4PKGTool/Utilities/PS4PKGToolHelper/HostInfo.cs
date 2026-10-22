using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    /// <summary>
    /// Host CPU and GPU names for the feedback form's Processor and Graphics
    /// Card fields - detected once on Linux via /proc/cpuinfo and lspci.
    /// </summary>
    public static class HostInfo
    {
        public static string CpuName { get; } = DetectCpu();

        public static string GpuName { get; } = DetectGpu();

        private static string DetectCpu()
        {
            try
            {
                if (File.Exists("/proc/cpuinfo"))
                {
                    foreach (var line in File.ReadLines("/proc/cpuinfo"))
                    {
                        if (line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                        {
                            int colon = line.IndexOf(':');
                            if (colon >= 0)
                            {
                                string model = line[(colon + 1)..].Trim();
                                if (!string.IsNullOrEmpty(model))
                                    return model;
                            }
                        }
                    }
                }
            }
            catch { }
            return Environment.ProcessorCount + "-Core Processor";
        }

        private static string DetectGpu()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "lspci",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);
                    string fallback = "";
                    foreach (var raw in output.Split('\n'))
                    {
                        string line = raw.Trim();
                        if (line.Contains("VGA compatible controller:", StringComparison.OrdinalIgnoreCase)
                            || line.Contains("3D controller:", StringComparison.OrdinalIgnoreCase)
                            || line.Contains("Display controller:", StringComparison.OrdinalIgnoreCase))
                        {
                            int idx = line.IndexOf(": ", StringComparison.Ordinal);
                            if (idx >= 0)
                            {
                                string desc = line[(idx + 2)..].Trim();
                                int revIdx = desc.LastIndexOf("(rev ", StringComparison.OrdinalIgnoreCase);
                                if (revIdx > 0) desc = desc[..revIdx].Trim();
                                if (desc.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)
                                    || desc.Contains("Radeon", StringComparison.OrdinalIgnoreCase)
                                    || desc.Contains("AMD", StringComparison.OrdinalIgnoreCase)
                                    || desc.Contains("Arc", StringComparison.OrdinalIgnoreCase))
                                    return desc;
                                if (string.IsNullOrEmpty(fallback))
                                    fallback = desc;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(fallback))
                        return fallback;
                }
            }
            catch { }
            return "";
        }
    }
}
