using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PDFCompressor.Core.Models;

namespace PDFCompressor.Core.Services
{
    public class CompressionProgressReport
    {
        public int Percent { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public string? CurrentFileName { get; set; }
    }

    public class PdfCompressionEngine
    {
        private readonly GhostscriptService _ghostscript;
        private readonly QpdfService _qpdf;
        private readonly HistoryService _history;
        private readonly EngineResolver _resolver;

        public PdfCompressionEngine(EngineResolver? resolver = null, HistoryService? history = null)
        {
            _resolver = resolver ?? new EngineResolver();
            _ghostscript = new GhostscriptService(_resolver);
            _qpdf = new QpdfService(_resolver);
            _history = history ?? new HistoryService();
        }

        public GhostscriptService Ghostscript => _ghostscript;
        public QpdfService Qpdf => _qpdf;
        public EngineResolver Resolver => _resolver;

        public string DetermineOutputPath(string inputPath, CompressionOptions options)
        {
            string dir = Path.GetDirectoryName(inputPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string ext = Path.GetExtension(inputPath);

            string targetDir;
            switch (options.OutputDirType)
            {
                case OutputDirectoryType.CustomFolder when !string.IsNullOrWhiteSpace(options.CustomOutputFolder):
                    targetDir = options.CustomOutputFolder;
                    break;
                case OutputDirectoryType.SameAsOriginal:
                    targetDir = dir;
                    break;
                case OutputDirectoryType.CompressedSubfolder:
                default:
                    targetDir = Path.Combine(dir, "Compressed PDFs");
                    break;
            }

            Directory.CreateDirectory(targetDir);

            string candidateName = $"{baseName}_compressed{ext}";
            string candidatePath = Path.Combine(targetDir, candidateName);

            if (options.OverwriteExisting || !File.Exists(candidatePath))
            {
                return candidatePath;
            }

            // If file already exists and overwrite is false, find unique index
            int index = 1;
            while (File.Exists(candidatePath))
            {
                candidateName = $"{baseName}_compressed ({index}){ext}";
                candidatePath = Path.Combine(targetDir, candidateName);
                index++;
            }

            return candidatePath;
        }

        public async Task<CompressionResult> CompressAsync(
            string inputPath,
            CompressionOptions options,
            IProgress<CompressionProgressReport>? progress = null,
            CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var result = new CompressionResult
            {
                OriginalFilePath = inputPath,
                PresetUsed = options.Preset,
                Status = CompressionStatus.Processing
            };

            string tempGsOutput = string.Empty;
            string tempQpdfOutput = string.Empty;

            try
            {
                // 1. Validation
                progress?.Report(new CompressionProgressReport { Percent = 5, StatusMessage = "Validating PDF...", CurrentFileName = result.OriginalFileName });

                if (!File.Exists(inputPath))
                {
                    result.Status = CompressionStatus.Failed;
                    result.ErrorMessage = $"Source file not found: {inputPath}";
                    return result;
                }

                var fileInfo = new FileInfo(inputPath);
                result.OriginalSizeBytes = fileInfo.Length;

                if (result.OriginalSizeBytes == 0)
                {
                    result.Status = CompressionStatus.Failed;
                    result.ErrorMessage = "The PDF file is empty (0 bytes).";
                    return result;
                }

                // Check magic bytes %PDF-
                byte[] header = new byte[8];
                using (var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int bytesRead = await fs.ReadAsync(header.AsMemory(0, header.Length), ct);
                    if (bytesRead < 4 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46) // %PDF
                    {
                        result.Status = CompressionStatus.Failed;
                        result.ErrorMessage = "The file is not a valid PDF document (missing %PDF- header).";
                        return result;
                    }
                }

                // Digital signature check
                try
                {
                    string sampleText = string.Empty;
                    using (var sr = new StreamReader(new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    {
                        char[] buffer = new char[8192];
                        int count = await sr.ReadAsync(buffer, 0, buffer.Length);
                        sampleText = new string(buffer, 0, count);
                    }
                    if (sampleText.Contains("/ByteRange") || sampleText.Contains("/Sig") || sampleText.Contains("/DocTimeStamp"))
                    {
                        result.WarningMessage = "This document appears to contain digital signatures. Compression will invalidate existing digital signatures.";
                        Logger.Warn($"Digital signature detected in '{inputPath}'");
                    }
                }
                catch { }

                // Inspect with qpdf if available
                var inspection = await _qpdf.InspectPdfAsync(inputPath, ct);
                if (inspection.RequiresPassword)
                {
                    result.Status = CompressionStatus.Failed;
                    result.ErrorMessage = "The PDF is password protected. Please remove password protection before compressing.";
                    return result;
                }
                if (!inspection.IsValid && !string.IsNullOrEmpty(inspection.ErrorMessage))
                {
                    result.Status = CompressionStatus.Failed;
                    result.ErrorMessage = $"Corrupted PDF: {inspection.ErrorMessage}";
                    return result;
                }
                result.PageCountBefore = inspection.PageCount;

                // 2. Prepare paths
                string finalOutputPath = options.CustomOutputFolder != null && options.OutputDirType == OutputDirectoryType.CustomFolder
                    ? DetermineOutputPath(inputPath, options)
                    : DetermineOutputPath(inputPath, options);

                result.OutputFilePath = finalOutputPath;

                string tempDir = Path.Combine(Path.GetTempPath(), "PDFCompressor", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                tempGsOutput = Path.Combine(tempDir, "gs_output.pdf");
                tempQpdfOutput = Path.Combine(tempDir, "qpdf_output.pdf");

                // 3. Stage 1: Ghostscript Compression
                progress?.Report(new CompressionProgressReport { Percent = 25, StatusMessage = "Optimizing images and document content...", CurrentFileName = result.OriginalFileName });

                var gsProgress = new Progress<int>(pct =>
                {
                    int scaled = 25 + (int)(pct * 0.45);
                    progress?.Report(new CompressionProgressReport { Percent = scaled, StatusMessage = "Compressing PDF streams...", CurrentFileName = result.OriginalFileName });
                });

                var (gsSuccess, gsError) = await _ghostscript.CompressPdfAsync(inputPath, tempGsOutput, options, gsProgress, ct);
                if (!gsSuccess || !File.Exists(tempGsOutput))
                {
                    result.Status = CompressionStatus.Failed;
                    result.ErrorMessage = $"Ghostscript optimization error: {gsError ?? "Unknown error"}";
                    return result;
                }

                // 4. Stage 2: QPDF Structural Optimization & Linearization
                progress?.Report(new CompressionProgressReport { Percent = 75, StatusMessage = "Performing structural optimization & linearization...", CurrentFileName = result.OriginalFileName });

                string readyForOutput = tempGsOutput;
                if (_qpdf.IsAvailable && options.OptimizeObjectStreams)
                {
                    bool qpdfSuccess = await _qpdf.OptimizePdfAsync(
                        tempGsOutput, 
                        tempQpdfOutput, 
                        options.LinearizeForFastWebView, 
                        options.OptimizeObjectStreams, 
                        ct);

                    if (qpdfSuccess && File.Exists(tempQpdfOutput) && new FileInfo(tempQpdfOutput).Length > 0)
                    {
                        readyForOutput = tempQpdfOutput;
                    }
                }

                // 5. Verify & Move to Final Destination
                progress?.Report(new CompressionProgressReport { Percent = 90, StatusMessage = "Verifying output document...", CurrentFileName = result.OriginalFileName });

                var compressedFileInfo = new FileInfo(readyForOutput);
                result.CompressedSizeBytes = compressedFileInfo.Length;

                // Verify page count if qpdf is available
                if (_qpdf.IsAvailable)
                {
                    var postInspection = await _qpdf.InspectPdfAsync(readyForOutput, ct);
                    result.PageCountAfter = postInspection.PageCount;
                    if (result.PageCountBefore > 0 && result.PageCountAfter > 0 && result.PageCountBefore != result.PageCountAfter)
                    {
                        result.Status = CompressionStatus.Failed;
                        result.ErrorMessage = $"Page count mismatch after compression (Original: {result.PageCountBefore}, Compressed: {result.PageCountAfter}).";
                        return result;
                    }
                }

                // Handle file size comparison
                if (result.CompressedSizeBytes >= result.OriginalSizeBytes && options.RetainOriginalIfLarger)
                {
                    result.Status = CompressionStatus.SkippedLarger;
                    result.WarningMessage = "The PDF is already highly optimized. Compressed version was not smaller than original.";
                    Logger.Info($"Skipped saving larger output for '{inputPath}' ({result.CompressedSizeBytes} >= {result.OriginalSizeBytes})");
                }
                else
                {
                    // Copy to final output path
                    string? outDir = Path.GetDirectoryName(finalOutputPath);
                    if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);
                    File.Copy(readyForOutput, finalOutputPath, true);
                    result.Status = CompressionStatus.Succeeded;
                }

                progress?.Report(new CompressionProgressReport { Percent = 100, StatusMessage = "Done", CurrentFileName = result.OriginalFileName });
            }
            catch (OperationCanceledException)
            {
                result.Status = CompressionStatus.Cancelled;
                result.ErrorMessage = "Operation cancelled by user.";
            }
            catch (Exception ex)
            {
                result.Status = CompressionStatus.Failed;
                result.ErrorMessage = ex.Message;
                Logger.Error($"Unexpected compression failure for '{inputPath}'", ex);
            }
            finally
            {
                sw.Stop();
                result.ProcessingDuration = sw.Elapsed;

                // Clean up temp directory
                try
                {
                    if (!string.IsNullOrEmpty(tempGsOutput))
                    {
                        string? dir = Path.GetDirectoryName(tempGsOutput);
                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            Directory.Delete(dir, true);
                        }
                    }
                }
                catch { }

                // Record into history
                try
                {
                    _history.AddRecord(new HistoryRecord
                    {
                        Timestamp = DateTime.UtcNow,
                        FileName = result.OriginalFileName,
                        OriginalPath = result.OriginalFilePath,
                        OutputPath = result.OutputFilePath,
                        OriginalSizeBytes = result.OriginalSizeBytes,
                        CompressedSizeBytes = result.CompressedSizeBytes,
                        SpaceSavedBytes = result.SavedBytes,
                        ReductionPercentage = Math.Round(result.SavedPercentage, 1),
                        PresetName = options.Preset.ToString(),
                        Status = result.Status.ToString(),
                        DurationSeconds = Math.Round(result.ProcessingDuration.TotalSeconds, 2)
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to record compression to history", ex);
                }
            }

            return result;
        }
    }
}
