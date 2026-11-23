using System;

namespace PDFCompressor.Core.Models
{
    public class AppSettings
    {
        public AppThemeMode Theme { get; set; } = AppThemeMode.Dark;
        public CompressionPreset DefaultPreset { get; set; } = CompressionPreset.Recommended;
        public OutputDirectoryType OutputDirType { get; set; } = OutputDirectoryType.CompressedSubfolder;
        public string CustomOutputFolder { get; set; } = string.Empty;
        public int ConcurrencyLimit { get; set; } = 2;
        public bool OpenOutputFolderOnComplete { get; set; } = true;
        public bool ShowCompletionNotification { get; set; } = true;
        public bool RetainOriginalIfLarger { get; set; } = true;
        public bool OverwriteExistingByDefault { get; set; } = false;

        // Custom default compression overrides
        public int CustomDpi { get; set; } = 150;
        public int CustomQuality { get; set; } = 75;
        public bool PreserveMetadata { get; set; } = true;
    }
}
