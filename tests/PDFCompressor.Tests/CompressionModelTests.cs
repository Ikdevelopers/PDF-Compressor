using System;
using PDFCompressor.Core.Models;
using Xunit;

namespace PDFCompressor.Tests
{
    public class PresetOptionsTests
    {
        [Fact]
        public void ExtremePreset_ShouldHaveLowerResolutionAndQuality()
        {
            var options = CompressionOptions.FromPreset(CompressionPreset.Extreme);
            Assert.Equal(CompressionPreset.Extreme, options.Preset);
            Assert.Equal(72, options.ColorImageResolution);
            Assert.Equal(40, options.JpegQuality);
            Assert.True(options.DownsampleColorImages);
            Assert.True(options.OptimizeObjectStreams);
        }

        [Fact]
        public void RecommendedPreset_ShouldHaveBalancedSettings()
        {
            var options = CompressionOptions.FromPreset(CompressionPreset.Recommended);
            Assert.Equal(CompressionPreset.Recommended, options.Preset);
            Assert.Equal(150, options.ColorImageResolution);
            Assert.Equal(75, options.JpegQuality);
            Assert.True(options.DownsampleColorImages);
            Assert.True(options.OptimizeObjectStreams);
        }

        [Fact]
        public void HighQualityPreset_ShouldHaveHighDpiAndQuality()
        {
            var options = CompressionOptions.FromPreset(CompressionPreset.HighQuality);
            Assert.Equal(CompressionPreset.HighQuality, options.Preset);
            Assert.Equal(300, options.ColorImageResolution);
            Assert.Equal(90, options.JpegQuality);
        }

        [Fact]
        public void LowCompressionPreset_ShouldDisableDownsampling()
        {
            var options = CompressionOptions.FromPreset(CompressionPreset.LowCompression);
            Assert.Equal(CompressionPreset.LowCompression, options.Preset);
            Assert.False(options.DownsampleColorImages);
            Assert.Equal(95, options.JpegQuality);
        }
    }

    public class CompressionMathTests
    {
        [Fact]
        public void SavedPercentage_CalculatesAccurately()
        {
            var result = new CompressionResult
            {
                OriginalSizeBytes = 10_000_000,
                CompressedSizeBytes = 3_000_000
            };

            Assert.Equal(7_000_000, result.SavedBytes);
            Assert.Equal(70.0, result.SavedPercentage);
            Assert.True(result.IsSmaller);
        }

        [Fact]
        public void SavedPercentage_NeverReportsNegativeSavings()
        {
            var result = new CompressionResult
            {
                OriginalSizeBytes = 1_000_000,
                CompressedSizeBytes = 1_500_000
            };

            Assert.Equal(0, result.SavedBytes);
            Assert.Equal(0.0, result.SavedPercentage);
            Assert.False(result.IsSmaller);
        }

        [Fact]
        public void SavedPercentage_HandlesZeroOriginalSize()
        {
            var result = new CompressionResult
            {
                OriginalSizeBytes = 0,
                CompressedSizeBytes = 0
            };

            Assert.Equal(0, result.SavedBytes);
            Assert.Equal(0.0, result.SavedPercentage);
        }

        [Theory]
        [InlineData(500, "500 B")]
        [InlineData(1024, "1 KB")]
        [InlineData(1024 * 1024 * 3, "3 MB")]
        public void FormatFileSize_FormatsCorrectly(long bytes, string expected)
        {
            Assert.Equal(expected, CompressionResult.FormatFileSize(bytes));
        }
    }
}
