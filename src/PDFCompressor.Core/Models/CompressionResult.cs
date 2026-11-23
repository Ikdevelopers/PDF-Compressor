using System;

namespace PDFCompressor.Core.Models
{
    public class CompressionResult
    {
        public string OriginalFilePath { get; set; } = string.Empty;
        public string OutputFilePath { get; set; } = string.Empty;
        public long OriginalSizeBytes { get; set; }
        public long CompressedSizeBytes { get; set; }
        public TimeSpan ProcessingDuration { get; set; }
        public CompressionStatus Status { get; set; } = CompressionStatus.Pending;
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }
        public int PageCountBefore { get; set; }
        public int PageCountAfter { get; set; }
        public CompressionPreset PresetUsed { get; set; }

        public string OriginalFileName => System.IO.Path.GetFileName(OriginalFilePath);
        public string OutputFileName => System.IO.Path.GetFileName(OutputFilePath);

        public long SavedBytes => Math.Max(0, OriginalSizeBytes - CompressedSizeBytes);

        public double SavedPercentage
        {
            get
            {
                if (OriginalSizeBytes <= 0) return 0.0;
                if (CompressedSizeBytes >= OriginalSizeBytes) return 0.0;
                return ((double)(OriginalSizeBytes - CompressedSizeBytes) / OriginalSizeBytes) * 100.0;
            }
        }

        public bool IsSmaller => CompressedSizeBytes > 0 && CompressedSizeBytes < OriginalSizeBytes;

        public static string FormatFileSize(long bytes)
        {
            if (bytes < 0) bytes = 0;
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {suffixes[order]}";
        }
    }
}
