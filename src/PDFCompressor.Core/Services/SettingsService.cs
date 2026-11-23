using System;
using System.IO;
using System.Text.Json;
using PDFCompressor.Core.Models;

namespace PDFCompressor.Core.Services
{
    public class SettingsService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private readonly string _settingsPath;
        private AppSettings _current;

        public SettingsService(string? customPath = null)
        {
            if (string.IsNullOrWhiteSpace(customPath))
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PDFCompressor");
                Directory.CreateDirectory(dir);
                _settingsPath = Path.Combine(dir, "settings.json");
            }
            else
            {
                _settingsPath = customPath;
            }

            _current = Load();
        }

        public AppSettings Current => _current;

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                    if (settings != null)
                    {
                        _current = settings;
                        return _current;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load settings file, using defaults.", ex);
            }

            _current = new AppSettings();
            return _current;
        }

        public bool Save(AppSettings? settings = null)
        {
            try
            {
                if (settings != null) _current = settings;
                string json = JsonSerializer.Serialize(_current, _jsonOptions);
                string? dir = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(_settingsPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings file.", ex);
                return false;
            }
        }

        public void ResetToDefaults()
        {
            _current = new AppSettings();
            Save();
        }
    }
}
