using System;

namespace PDFCompressor.Core.Models
{
    public class CompressionOptions
    {
        public CompressionPreset Preset { get; set; } = CompressionPreset.Recommended;

        // Image DPI settings
        public int ColorImageResolution { get; set; } = 150;
        public int GrayImageResolution { get; set; } = 150;
        public int MonoImageResolution { get; set; } = 300;

        // Image downsampling
        public bool DownsampleColorImages { get; set; } = true;
        public bool DownsampleGrayImages { get; set; } = true;
        public bool DownsampleMonoImages { get; set; } = true;

        // Quality (1 to 100)
        public int JpegQuality { get; set; } = 75;

        // Fonts
        public bool EmbedAllFonts { get; set; } = true;
        public bool SubsetFonts { get; set; } = true;

        // PDF Features & Structure
        public bool PreserveMetadata { get; set; } = true;
        public bool LinearizeForFastWebView { get; set; } = true;
        public bool OptimizeObjectStreams { get; set; } = true;
        public bool RetainOriginalIfLarger { get; set; } = true;

        // Output configuration
        public OutputDirectoryType OutputDirType { get; set; } = OutputDirectoryType.CompressedSubfolder;
        public string? CustomOutputFolder { get; set; }
        public bool OverwriteExisting { get; set; } = false;

        public static CompressionOptions FromPreset(CompressionPreset preset)
        {
            var options = new CompressionOptions { Preset = preset };
            switch (preset)
            {
                case CompressionPreset.Extreme:
                    options.ColorImageResolution = 72;
                    options.GrayImageResolution = 72;
                    options.MonoImageResolution = 150;
                    options.JpegQuality = 40;
                    options.DownsampleColorImages = true;
                    options.DownsampleGrayImages = true;
                    options.DownsampleMonoImages = true;
                    options.OptimizeObjectStreams = true;
                    options.LinearizeForFastWebView = true;
                    break;

                case CompressionPreset.Recommended:
                    options.ColorImageResolution = 150;
                    options.GrayImageResolution = 150;
                    options.MonoImageResolution = 300;
                    options.JpegQuality = 75;
                    options.DownsampleColorImages = true;
                    options.DownsampleGrayImages = true;
                    options.DownsampleMonoImages = true;
                    options.OptimizeObjectStreams = true;
                    options.LinearizeForFastWebView = true;
                    break;

                case CompressionPreset.HighQuality:
                    options.ColorImageResolution = 300;
                    options.GrayImageResolution = 300;
                    options.MonoImageResolution = 600;
                    options.JpegQuality = 90;
                    options.DownsampleColorImages = true;
                    options.DownsampleGrayImages = true;
                    options.DownsampleMonoImages = true;
                    options.OptimizeObjectStreams = true;
                    options.LinearizeForFastWebView = true;
                    break;

                case CompressionPreset.LowCompression:
                    options.ColorImageResolution = 600;
                    options.GrayImageResolution = 600;
                    options.MonoImageResolution = 1200;
                    options.JpegQuality = 95;
                    options.DownsampleColorImages = false;
                    options.DownsampleGrayImages = false;
                    options.DownsampleMonoImages = false;
                    options.OptimizeObjectStreams = true;
                    options.LinearizeForFastWebView = true;
                    break;

                case CompressionPreset.Custom:
                    // default to recommended values as starting point
                    break;
            }
            return options;
        }
    }
}
