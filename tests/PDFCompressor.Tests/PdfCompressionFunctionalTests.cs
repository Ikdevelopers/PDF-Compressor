using System;
using System.IO;
using System.Threading.Tasks;
using PDFCompressor.Core.Models;
using PDFCompressor.Core.Services;
using Xunit;

namespace PDFCompressor.Tests
{
    public class PdfCompressionFunctionalTests
    {
        private readonly EngineResolver _resolver;
        private readonly PdfCompressionEngine _engine;
        private readonly string _testDataDir;

        public PdfCompressionFunctionalTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Locate TestData
            string candidate = Path.Combine(baseDir, "..", "..", "..", "..", "..", "TestData");
            if (!Directory.Exists(candidate))
            {
                candidate = Path.Combine(baseDir, "..", "..", "..", "TestData");
            }
            if (!Directory.Exists(candidate))
            {
                candidate = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "TestData"));
            }
            _testDataDir = Path.GetFullPath(candidate);

            _resolver = new EngineResolver();
            _engine = new PdfCompressionEngine(_resolver);
        }

        [Fact]
        public async Task Compress_ScannedPdf_RecommendedPreset_ReducesFileSizeAndKeepsPages()
        {
            string inputPath = Path.Combine(_testDataDir, "sample_scanned.pdf");
            Assert.True(File.Exists(inputPath), $"Test file not found: {inputPath}");

            var options = CompressionOptions.FromPreset(CompressionPreset.Recommended);
            string outDir = Path.Combine(Path.GetTempPath(), $"pdf_test_{Guid.NewGuid():N}");
            options.OutputDirType = OutputDirectoryType.CustomFolder;
            options.CustomOutputFolder = outDir;

            var result = await _engine.CompressAsync(inputPath, options);

            try
            {
                Assert.Equal(CompressionStatus.Succeeded, result.Status);
                Assert.True(File.Exists(result.OutputFilePath), "Output file was not created");
                Assert.True(result.CompressedSizeBytes > 0, "Output file is empty");
                Assert.True(result.CompressedSizeBytes < result.OriginalSizeBytes, 
                    $"Expected compression, but original was {result.OriginalSizeBytes} and compressed was {result.CompressedSizeBytes}");
                Assert.True(result.SavedPercentage > 20.0, $"Expected >20% reduction, got {result.SavedPercentage}%");
                Assert.Equal(result.PageCountBefore, result.PageCountAfter);
            }
            finally
            {
                if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            }
        }

        [Fact]
        public async Task Compress_ScannedPdf_ExtremePreset_YieldsMaximumReduction()
        {
            string inputPath = Path.Combine(_testDataDir, "sample_scanned.pdf");
            Assert.True(File.Exists(inputPath));

            var options = CompressionOptions.FromPreset(CompressionPreset.Extreme);
            string outDir = Path.Combine(Path.GetTempPath(), $"pdf_test_extreme_{Guid.NewGuid():N}");
            options.OutputDirType = OutputDirectoryType.CustomFolder;
            options.CustomOutputFolder = outDir;

            var result = await _engine.CompressAsync(inputPath, options);

            try
            {
                Assert.Equal(CompressionStatus.Succeeded, result.Status);
                Assert.True(result.SavedPercentage > 40.0, $"Expected >40% reduction for extreme preset, got {result.SavedPercentage}%");
            }
            finally
            {
                if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            }
        }

        [Fact]
        public async Task Compress_PasswordProtectedPdf_FailsGracefullyWithClearError()
        {
            string inputPath = Path.Combine(_testDataDir, "sample_password.pdf");
            Assert.True(File.Exists(inputPath));

            var options = CompressionOptions.FromPreset(CompressionPreset.Recommended);
            string outDir = Path.Combine(Path.GetTempPath(), $"pdf_test_pwd_{Guid.NewGuid():N}");
            options.OutputDirType = OutputDirectoryType.CustomFolder;
            options.CustomOutputFolder = outDir;

            var result = await _engine.CompressAsync(inputPath, options);

            try
            {
                Assert.Equal(CompressionStatus.Failed, result.Status);
                Assert.NotNull(result.ErrorMessage);
                Assert.Contains("password", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            }
        }

        [Fact]
        public async Task Compress_EmptyPdf_FailsGracefully()
        {
            string inputPath = Path.Combine(_testDataDir, "sample_empty.pdf");
            Assert.True(File.Exists(inputPath));

            var options = CompressionOptions.FromPreset(CompressionPreset.Recommended);
            var result = await _engine.CompressAsync(inputPath, options);

            Assert.Equal(CompressionStatus.Failed, result.Status);
            Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Compress_CorruptedPdf_FailsGracefully()
        {
            string inputPath = Path.Combine(_testDataDir, "sample_corrupted.pdf");
            Assert.True(File.Exists(inputPath));

            var options = CompressionOptions.FromPreset(CompressionPreset.Recommended);
            var result = await _engine.CompressAsync(inputPath, options);

            Assert.Equal(CompressionStatus.Failed, result.Status);
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Fact]
        public async Task Compress_UnicodeFilename_HandlesSuccessfully()
        {
            string inputPath = Path.Combine(_testDataDir, "sample_unicode_δοκιμή_テスト.pdf");
            Assert.True(File.Exists(inputPath));

            var options = CompressionOptions.FromPreset(CompressionPreset.Recommended);
            string outDir = Path.Combine(Path.GetTempPath(), $"pdf_test_unicode_{Guid.NewGuid():N}");
            options.OutputDirType = OutputDirectoryType.CustomFolder;
            options.CustomOutputFolder = outDir;

            var result = await _engine.CompressAsync(inputPath, options);

            try
            {
                Assert.Equal(CompressionStatus.Succeeded, result.Status);
                Assert.True(File.Exists(result.OutputFilePath));
            }
            finally
            {
                if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            }
        }

        [Fact]
        public void OutputPathDetermination_GeneratesUniqueNamesWithoutOverwriting()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"path_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                string inputPath = Path.Combine(tempDir, "document.pdf");
                File.WriteAllText(inputPath, "fake content");

                var options = new CompressionOptions
                {
                    OutputDirType = OutputDirectoryType.SameAsOriginal,
                    OverwriteExisting = false
                };

                string out1 = _engine.DetermineOutputPath(inputPath, options);
                Assert.Equal(Path.Combine(tempDir, "document_compressed.pdf"), out1);

                // Simulate file existing
                File.WriteAllText(out1, "already exists");

                string out2 = _engine.DetermineOutputPath(inputPath, options);
                Assert.Equal(Path.Combine(tempDir, "document_compressed (1).pdf"), out2);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }
}
