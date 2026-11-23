using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PDFCompressor.Core.Services
{
    public class QpdfInspectionResult
    {
        public bool IsValid { get; set; }
        public bool IsEncrypted { get; set; }
        public bool RequiresPassword { get; set; }
        public int PageCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class QpdfService
    {
        private readonly string? _qpdfPath;

        public QpdfService(EngineResolver resolver)
        {
            _qpdfPath = resolver.ResolveQpdf();
        }

        public QpdfService(string explicitPath)
        {
            _qpdfPath = explicitPath;
        }

        public bool IsAvailable => !string.IsNullOrEmpty(_qpdfPath) && File.Exists(_qpdfPath);
        public string? ExecutablePath => _qpdfPath;

        public async Task<QpdfInspectionResult> InspectPdfAsync(string filePath, CancellationToken ct = default)
        {
            var result = new QpdfInspectionResult();
            if (!IsAvailable)
            {
                result.IsValid = true; // Fallback if qpdf unavailable
                return result;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _qpdfPath!,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                // Check page count and encryption
                psi.ArgumentList.Add("--show-npages");
                psi.ArgumentList.Add(filePath);

                using var proc = new Process { StartInfo = psi };
                proc.Start();

                var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
                var stderrTask = proc.StandardError.ReadToEndAsync(ct);

                await proc.WaitForExitAsync(ct);
                string stdout = await stdoutTask;
                string stderr = await stderrTask;

                if (proc.ExitCode == 0 && int.TryParse(stdout.Trim(), out int npages))
                {
                    result.IsValid = true;
                    result.PageCount = npages;
                }
                else
                {
                    if (stderr.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                        stderr.Contains("encrypted", StringComparison.OrdinalIgnoreCase) ||
                        proc.ExitCode == 2)
                    {
                        result.IsEncrypted = true;
                        result.RequiresPassword = true;
                        result.ErrorMessage = "The PDF file is password protected or encrypted.";
                    }
                    else
                    {
                        result.IsValid = false;
                        result.ErrorMessage = string.IsNullOrWhiteSpace(stderr) ? "Invalid or corrupted PDF file." : stderr.Trim();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Qpdf inspection failed for '{filePath}'", ex);
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<bool> OptimizePdfAsync(
            string inputPath, 
            string outputPath, 
            bool linearize = true, 
            bool generateObjectStreams = true,
            CancellationToken ct = default)
        {
            if (!IsAvailable)
            {
                Logger.Warn("Qpdf is not available, skipping structural optimization stage.");
                if (inputPath != outputPath)
                {
                    File.Copy(inputPath, outputPath, true);
                }
                return true;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _qpdfPath!,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                if (linearize)
                {
                    psi.ArgumentList.Add("--linearize");
                }

                if (generateObjectStreams)
                {
                    psi.ArgumentList.Add("--object-streams=generate");
                }
                else
                {
                    psi.ArgumentList.Add("--object-streams=preserve");
                }

                psi.ArgumentList.Add("--recompress-flate");
                psi.ArgumentList.Add("--compression-level=9");
                psi.ArgumentList.Add(inputPath);
                psi.ArgumentList.Add(outputPath);

                using var proc = new Process { StartInfo = psi };
                proc.Start();

                var stderrTask = proc.StandardError.ReadToEndAsync(ct);
                await proc.WaitForExitAsync(ct);
                string stderr = await stderrTask;

                // qpdf exit code 0 = success, exit code 3 = warnings but file written successfully
                if (proc.ExitCode == 0 || proc.ExitCode == 3)
                {
                    if (proc.ExitCode == 3)
                    {
                        Logger.Warn($"Qpdf optimization completed with warnings: {stderr}");
                    }
                    return true;
                }

                Logger.Error($"Qpdf optimization failed with exit code {proc.ExitCode}: {stderr}");
                return false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Qpdf execution exception for '{inputPath}'", ex);
                return false;
            }
        }
    }
}
