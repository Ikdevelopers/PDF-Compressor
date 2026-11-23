using System;

namespace PDFCompressor.Core.Models
{
    public enum CompressionPreset
    {
        Recommended,
        Extreme,
        HighQuality,
        LowCompression,
        Custom
    }

    public enum OutputDirectoryType
    {
        CompressedSubfolder,
        SameAsOriginal,
        CustomFolder
    }

    public enum CompressionStatus
    {
        Pending,
        Validating,
        Processing,
        Compressing,
        Optimizing,
        Succeeded,
        Failed,
        Cancelled,
        SkippedLarger
    }

    public enum AppThemeMode
    {
        System,
        Dark,
        Light
    }
}
