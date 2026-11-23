using System;
using System.IO;
using PDFCompressor.Core.Models;
using PDFCompressor.Core.Services;
using Xunit;

namespace PDFCompressor.Tests
{
    public class SettingsServiceTests : IDisposable
    {
        private readonly string _tempSettingsPath;

        public SettingsServiceTests()
        {
            _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid():N}.json");
        }

        public void Dispose()
        {
            if (File.Exists(_tempSettingsPath))
            {
                File.Delete(_tempSettingsPath);
            }
        }

        [Fact]
        public void Settings_SaveAndLoad_RoundtripsSuccessfully()
        {
            var service = new SettingsService(_tempSettingsPath);
            Assert.Equal(AppThemeMode.Dark, service.Current.Theme);

            service.Current.Theme = AppThemeMode.Light;
            service.Current.ConcurrencyLimit = 4;
            service.Current.CustomOutputFolder = @"C:\TestFolder";
            service.Save();

            var reloadedService = new SettingsService(_tempSettingsPath);
            Assert.Equal(AppThemeMode.Light, reloadedService.Current.Theme);
            Assert.Equal(4, reloadedService.Current.ConcurrencyLimit);
            Assert.Equal(@"C:\TestFolder", reloadedService.Current.CustomOutputFolder);
        }

        [Fact]
        public void Settings_ResetToDefaults_RestoresDefaults()
        {
            var service = new SettingsService(_tempSettingsPath);
            service.Current.ConcurrencyLimit = 8;
            service.Save();

            service.ResetToDefaults();
            Assert.Equal(2, service.Current.ConcurrencyLimit);
            Assert.Equal(AppThemeMode.Dark, service.Current.Theme);
        }
    }

    public class HistoryServiceTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly HistoryService _service;

        public HistoryServiceTests()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"history_test_{Guid.NewGuid():N}.db");
            _service = new HistoryService(_tempDbPath);
        }

        public void Dispose()
        {
            if (File.Exists(_tempDbPath))
            {
                try { File.Delete(_tempDbPath); } catch { }
            }
        }

        [Fact]
        public void History_AddAndQuery_WorksCorrectly()
        {
            var record1 = new HistoryRecord
            {
                FileName = "report.pdf",
                OriginalPath = @"C:\docs\report.pdf",
                OutputPath = @"C:\docs\Compressed PDFs\report_compressed.pdf",
                OriginalSizeBytes = 10_000_000,
                CompressedSizeBytes = 4_000_000,
                SpaceSavedBytes = 6_000_000,
                ReductionPercentage = 60.0,
                PresetName = "Recommended",
                Status = "Succeeded",
                DurationSeconds = 1.8
            };

            var record2 = new HistoryRecord
            {
                FileName = "slides.pdf",
                OriginalPath = @"C:\docs\slides.pdf",
                OutputPath = @"C:\docs\Compressed PDFs\slides_compressed.pdf",
                OriginalSizeBytes = 5_000_000,
                CompressedSizeBytes = 2_000_000,
                SpaceSavedBytes = 3_000_000,
                ReductionPercentage = 60.0,
                PresetName = "Extreme",
                Status = "Succeeded",
                DurationSeconds = 1.2
            };

            Assert.True(_service.AddRecord(record1));
            Assert.True(_service.AddRecord(record2));

            var all = _service.GetAll();
            Assert.Equal(2, all.Count);

            // Search query
            var searchResults = _service.GetAll("slides");
            Assert.Single(searchResults);
            Assert.Equal("slides.pdf", searchResults[0].FileName);

            // Stats
            var stats = _service.GetStatistics();
            Assert.Equal(2, stats.TotalFilesCompressed);
            Assert.Equal(15_000_000, stats.TotalOriginalBytes);
            Assert.Equal(6_000_000, stats.TotalCompressedBytes);
            Assert.Equal(9_000_000, stats.TotalSpaceSavedBytes);
            Assert.Equal(60.0, stats.AverageReductionPercentage);

            // Delete individual
            Assert.True(_service.DeleteRecord(record1.Id));
            Assert.Single(_service.GetAll());

            // Clear all
            Assert.True(_service.ClearAll());
            Assert.Empty(_service.GetAll());
        }
    }
}
