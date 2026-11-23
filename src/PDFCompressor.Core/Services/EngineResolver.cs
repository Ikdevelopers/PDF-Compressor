using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PDFCompressor.Core.Services
{
    public class EngineResolver
    {
        private readonly string _baseDirectory;

        public EngineResolver(string? baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        public bool Is64BitProcess => Environment.Is64BitProcess;
        public string ArchitectureName => Is64BitProcess ? "x64" : "x86";

        public string? ResolveGhostscript(string? forcedArch = null)
        {
            string arch = forcedArch ?? ArchitectureName;
            string exeName = arch == "x64" ? "gswin64c.exe" : "gswin32c.exe";

            // Candidate paths relative to application base directory
            string[] candidates = {
                Path.Combine(_baseDirectory, "Tools", $"gs-{arch}", "bin", exeName),
                Path.Combine(_baseDirectory, $"gs-{arch}", "bin", exeName),
                Path.Combine(_baseDirectory, "Tools", "gs", "bin", exeName),
                Path.Combine(_baseDirectory, "gs", "bin", exeName),
                Path.Combine(_baseDirectory, "gs", exeName),
                Path.Combine(_baseDirectory, exeName),
                // In project development directory
                Path.Combine(_baseDirectory, "..", "..", "..", "..", "..", "Tools", $"gs-{arch}", "bin", exeName),
                Path.Combine(_baseDirectory, "..", "..", "..", "Tools", $"gs-{arch}", "bin", exeName)
            };

            foreach (var path in candidates)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    Logger.Info($"Resolved Ghostscript ({arch}) at: {fullPath}");
                    return fullPath;
                }
            }

            // Fallback: check PATH
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    try
                    {
                        string candidate = Path.Combine(dir.Trim(), exeName);
                        if (File.Exists(candidate))
                        {
                            Logger.Info($"Resolved Ghostscript in PATH at: {candidate}");
                            return candidate;
                        }
                    }
                    catch { }
                }
            }

            Logger.Warn($"Ghostscript executable '{exeName}' could not be found.");
            return null;
        }

        public string? ResolveQpdf(string? forcedArch = null)
        {
            string arch = forcedArch ?? ArchitectureName;
            string exeName = "qpdf.exe";

            string[] candidates = {
                Path.Combine(_baseDirectory, "Tools", $"qpdf-{arch}", "bin", exeName),
                Path.Combine(_baseDirectory, $"qpdf-{arch}", "bin", exeName),
                Path.Combine(_baseDirectory, "Tools", "qpdf", "bin", exeName),
                Path.Combine(_baseDirectory, "qpdf", "bin", exeName),
                Path.Combine(_baseDirectory, "qpdf", exeName),
                Path.Combine(_baseDirectory, exeName),
                // In project development directory
                Path.Combine(_baseDirectory, "..", "..", "..", "..", "..", "Tools", $"qpdf-{arch}", "bin", exeName),
                Path.Combine(_baseDirectory, "..", "..", "..", "Tools", $"qpdf-{arch}", "bin", exeName)
            };

            foreach (var path in candidates)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    Logger.Info($"Resolved qpdf ({arch}) at: {fullPath}");
                    return fullPath;
                }
            }

            // Fallback: check PATH
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    try
                    {
                        string candidate = Path.Combine(dir.Trim(), exeName);
                        if (File.Exists(candidate))
                        {
                            Logger.Info($"Resolved qpdf in PATH at: {candidate}");
                            return candidate;
                        }
                    }
                    catch { }
                }
            }

            Logger.Warn($"qpdf executable '{exeName}' could not be found.");
            return null;
        }
    }
}
