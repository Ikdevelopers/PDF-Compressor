using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PDFCompressor.Core.Models;

namespace PDFCompressor.Core.Services
{
    public class GhostscriptService
    {
        private readonly string? _gsPath;

        public GhostscriptService(EngineResolver resolver)
        {
            _gsPath = resolver.ResolveGhostscript();
        }

        public GhostscriptService(string explicitPath)
        {
            _gsPath = explicitPath;
        }

        public bool IsAvailable => !string.IsNullOrEmpty(_gsPath) && File.Exists(_gsPath);
        public string? ExecutablePath => _gsPath;

        public List<string> BuildArguments(string inputPath, string outputPath, CompressionOptions options)
        {
            var args = new List<string>
            {
                "-sDEVICE=pdfwrite",
                "-dCompatibilityLevel=1.4",
                "-dNOPAUSE",
                "-dQUIET",
                "-dBATCH",
                "-dSAFER"
            };

            // Preset base configuration
            switch (options.Preset)
            {
                case CompressionPreset.Extreme:
                    args.Add("-dPDFSETTINGS=/screen");
                    break;
                case CompressionPreset.Recommended:
                    args.Add("-dPDFSETTINGS=/ebook");
                    break;
                case CompressionPreset.HighQuality:
                    args.Add("-dPDFSETTINGS=/printer");
                    break;
                case CompressionPreset.LowCompression:
                    args.Add("-dPDFSETTINGS=/prepress");
                    break;
                case CompressionPreset.Custom:
                default:
                    args.Add("-dPDFSETTINGS=/default");
                    break;
            }

            // Downsampling and Resolutions
            if (options.DownsampleColorImages)
            {
                args.Add("-dDownsampleColorImages=true");
                args.Add("-dColorImageDownsampleType=/Bicubic");
                args.Add($"-dColorImageResolution={options.ColorImageResolution}");
                args.Add("-dAutoFilterColorImages=false");
                args.Add("-dColorImageFilter=/DCTEncode");
            }
            else
            {
                args.Add("-dDownsampleColorImages=false");
            }

            if (options.DownsampleGrayImages)
            {
                args.Add("-dDownsampleGrayImages=true");
                args.Add("-dGrayImageDownsampleType=/Bicubic");
                args.Add($"-dGrayImageResolution={options.GrayImageResolution}");
                args.Add("-dAutoFilterGrayImages=false");
                args.Add("-dGrayImageFilter=/DCTEncode");
            }
            else
            {
                args.Add("-dDownsampleGrayImages=false");
            }

            if (options.DownsampleMonoImages)
            {
                args.Add("-dDownsampleMonoImages=true");
                args.Add("-dMonoImageDownsampleType=/Bicubic");
                args.Add($"-dMonoImageResolution={options.MonoImageResolution}");
                args.Add("-dMonoImageFilter=/CCITTFaxEncode");
            }
            else
            {
                args.Add("-dDownsampleMonoImages=false");
            }

            // JPEG Quality factor (Ghostscript uses QFactor where 0.15 is high quality ~90%, 0.40 is ~75%, 1.0 is ~40%)
            // Quality scale: 100 -> QFactor ~0.05, 75 -> 0.4, 40 -> 1.0, 10 -> 2.4
            double qFactor = Math.Round(Math.Max(0.05, Math.Min(3.0, (100.0 - options.JpegQuality) / 60.0)), 3);
            args.Add($"-dJPEGQFactor={qFactor.ToString("0.###", CultureInfo.InvariantCulture)}");

            // Fonts
            args.Add(options.EmbedAllFonts ? "-dEmbedAllFonts=true" : "-dEmbedAllFonts=false");
            args.Add(options.SubsetFonts ? "-dSubsetFonts=true" : "-dSubsetFonts=false");

            // Output & Input paths
            args.Add($"-sOutputFile={outputPath}");
            args.Add(inputPath);

            return args;
        }

        public async Task<(bool Success, string? Error)> CompressPdfAsync(
            string inputPath,
            string outputPath,
            CompressionOptions options,
            IProgress<int>? progress = null,
            CancellationToken ct = default)
        {
            if (!IsAvailable)
            {
                return (false, "Ghostscript engine executable is not available on this system.");
            }

            Process? proc = null;
            try
            {
                progress?.Report(10);

                var args = BuildArguments(inputPath, outputPath, options);

                var psi = new ProcessStartInfo
                {
                    FileName = _gsPath!,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                foreach (var arg in args)
                {
                    psi.ArgumentList.Add(arg);
                }

                proc = new Process { StartInfo = psi };
                proc.Start();

                var stderrTask = proc.StandardError.ReadToEndAsync(ct);
                var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);

                progress?.Report(30);

                // Wait with cancellation support
                using (ct.Register(() =>
                {
                    try
                    {
                        if (proc != null && !proc.HasExited)
                        {
                            proc.Kill(entireProcessTree: true);
                        }
                    }
                    catch { }
                }))
                {
                    await proc.WaitForExitAsync(ct);
                }

                progress?.Report(70);

                string stderr = await stderrTask;
                string stdout = await stdoutTask;

                if (proc.ExitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
                {
                    return (true, null);
                }

                string err = !string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : (!string.IsNullOrWhiteSpace(stdout) ? stdout.Trim() : $"Ghostscript exited with code {proc.ExitCode}");
                Logger.Error($"Ghostscript execution failed for '{inputPath}': {err}");
                return (false, err);
            }
            catch (OperationCanceledException)
            {
                Logger.Info($"Ghostscript compression cancelled by user for: {inputPath}");
                try
                {
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
                catch { }
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Ghostscript compression exception for '{inputPath}'", ex);
                return (false, ex.Message);
            }
            finally
            {
                try
                {
                    if (proc != null && !proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                    }
                    proc?.Dispose();
                }
                catch { }
            }
        }
    }
}
