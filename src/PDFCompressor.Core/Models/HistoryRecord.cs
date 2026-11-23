using System;

namespace PDFCompressor.Core.Models
{
    public class HistoryRecord
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string FileName { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public long OriginalSizeBytes { get; set; }
        public long CompressedSizeBytes { get; set; }
        public long SpaceSavedBytes { get; set; }
        public double ReductionPercentage { get; set; }
        public string PresetName { get; set; } = "Recommended";
        public string Status { get; set; } = "Succeeded";
        public double DurationSeconds { get; set; }
    }
}
