using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PDFCompressor.App.UI.Controls;
using PDFCompressor.App.UI.Theme;
using PDFCompressor.Core.Models;
using PDFCompressor.Core.Services;

namespace PDFCompressor.App.UI.Views
{
    public class SettingsView : UserControl
    {
        private readonly SettingsService _settingsService;
        private readonly HistoryService _historyService;

        private ModernComboBox _cbTheme = null!;
        private ModernComboBox _cbDefaultPreset = null!;
        private RadioButton _rbSubfolder = null!;
        private RadioButton _rbSameFolder = null!;
        private RadioButton _rbCustomFolder = null!;
        private TextBox _txtCustomFolder = null!;
        private ModernButton _btnBrowseFolder = null!;
        private NumericUpDown _numConcurrency = null!;
        private CheckBox _chkOpenFolder = null!;
        private CheckBox _chkRetainLarger = null!;

        public SettingsView(SettingsService settingsService, HistoryService historyService)
        {
            _settingsService = settingsService;
            _historyService = historyService;

            InitializeComponent();
            LoadCurrentSettings();
            ApplyTheme();
            AppTheme.ThemeChanged += ApplyTheme;
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            AutoScroll = true;
            Padding = new Padding(24, 16, 24, 24);

            var rootLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = 860
            };

            Resize += (s, e) =>
            {
                int targetWidth = Math.Max(700, ClientSize.Width - 60);
                rootLayout.Width = targetWidth;
                foreach (Control c in rootLayout.Controls)
                {
                    c.Width = targetWidth;
                }
            };

            var lblTitle = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };
            rootLayout.Controls.Add(lblTitle);

            // Card 1: Appearance & UI
            var cardAppearance = new CardPanel
            {
                Height = 105,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(18)
            };
            var lblAppHeader = new Label { Text = "Appearance & Interface", Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), Location = new Point(16, 12), AutoSize = true };
            var lblTheme = new Label { Text = "Application Theme:", Location = new Point(16, 48), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
            _cbTheme = new ModernComboBox { Location = new Point(180, 44), Width = 190, Height = 32 };
            _cbTheme.Items.AddRange(new object[] { "Dark Theme (Default)", "Light Theme" });
            _cbTheme.SelectedIndexChanged += (s, e) =>
            {
                var mode = _cbTheme.SelectedIndex == 1 ? AppThemeMode.Light : AppThemeMode.Dark;
                _settingsService.Current.Theme = mode;
                _settingsService.Save();
                AppTheme.SetTheme(mode);
            };

            cardAppearance.Controls.Add(_cbTheme);
            cardAppearance.Controls.Add(lblTheme);
            cardAppearance.Controls.Add(lblAppHeader);
            rootLayout.Controls.Add(cardAppearance);

            // Card 2: Compression Defaults
            var cardCompression = new CardPanel
            {
                Height = 160,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(18)
            };
            var lblCompHeader = new Label { Text = "Compression Defaults", Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), Location = new Point(16, 12), AutoSize = true };

            var lblPreset = new Label { Text = "Default Preset:", Location = new Point(16, 48), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
            _cbDefaultPreset = new ModernComboBox { Location = new Point(180, 44), Width = 220, Height = 32 };
            _cbDefaultPreset.Items.AddRange(new object[] { "Recommended (150 DPI)", "Extreme (72 DPI)", "High Quality (300 DPI)", "Low Compression" });
            _cbDefaultPreset.SelectedIndexChanged += (s, e) => SaveSettings();

            var lblWorkers = new Label { Text = "Batch Concurrency Limit:", Location = new Point(16, 86), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
            _numConcurrency = new NumericUpDown
            {
                Location = new Point(180, 84),
                Width = 70,
                Minimum = 1,
                Maximum = 8,
                Value = 2,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _numConcurrency.ValueChanged += (s, e) => SaveSettings();

            _chkRetainLarger = new CheckBox { Text = "Retain original PDF if compressed file is larger (Avoid space inflation)", Location = new Point(16, 120), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
            _chkRetainLarger.CheckedChanged += (s, e) => SaveSettings();

            cardCompression.Controls.Add(_chkRetainLarger);
            cardCompression.Controls.Add(_numConcurrency);
            cardCompression.Controls.Add(lblWorkers);
            cardCompression.Controls.Add(_cbDefaultPreset);
            cardCompression.Controls.Add(lblPreset);
            cardCompression.Controls.Add(lblCompHeader);
            rootLayout.Controls.Add(cardCompression);

            // Card 3: Output Destination
            var cardOutput = new CardPanel
            {
                Height = 180,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(18)
            };
            var lblOutHeader = new Label { Text = "Output Destination & Behavior", Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), Location = new Point(16, 12), AutoSize = true };

            _rbSubfolder = new RadioButton { Text = "Save in 'Compressed PDFs' subfolder inside original directory (Recommended)", Location = new Point(16, 44), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
            _rbSameFolder = new RadioButton { Text = "Save directly in original directory alongside source file", Location = new Point(16, 68), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
            _rbCustomFolder = new RadioButton { Text = "Save to custom folder:", Location = new Point(16, 92), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };

            _rbSubfolder.CheckedChanged += (s, e) => SaveSettings();
            _rbSameFolder.CheckedChanged += (s, e) => SaveSettings();
            _rbCustomFolder.CheckedChanged += (s, e) =>
            {
                _txtCustomFolder.Enabled = _rbCustomFolder.Checked;
                _btnBrowseFolder.Enabled = _rbCustomFolder.Checked;
                SaveSettings();
            };

            _txtCustomFolder = new TextBox { Location = new Point(180, 91), Width = 380, Enabled = false, Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };
            _txtCustomFolder.TextChanged += (s, e) => SaveSettings();

            _btnBrowseFolder = new ModernButton { Text = "Browse...", Style = ButtonStyle.Secondary, Width = 90, Height = 28, Location = new Point(570, 89), Enabled = false };
            _btnBrowseFolder.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _txtCustomFolder.Text = fbd.SelectedPath;
                    SaveSettings();
                }
            };

            _chkOpenFolder = new CheckBox { Text = "Automatically open output folder after compression finishes", Location = new Point(16, 130), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
            _chkOpenFolder.CheckedChanged += (s, e) => SaveSettings();

            cardOutput.Controls.Add(_chkOpenFolder);
            cardOutput.Controls.Add(_btnBrowseFolder);
            cardOutput.Controls.Add(_txtCustomFolder);
            cardOutput.Controls.Add(_rbCustomFolder);
            cardOutput.Controls.Add(_rbSameFolder);
            cardOutput.Controls.Add(_rbSubfolder);
            cardOutput.Controls.Add(lblOutHeader);
            rootLayout.Controls.Add(cardOutput);

            // Card 4: Maintenance & Reset
            var cardMaintenance = new CardPanel
            {
                Height = 110,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(18)
            };
            var lblMaintHeader = new Label { Text = "Data & Maintenance", Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), Location = new Point(16, 12), AutoSize = true };

            var btnClearHist = new ModernButton { Text = "Clear All History", Style = ButtonStyle.Danger, Width = 150, Height = 36, Location = new Point(16, 50) };
            btnClearHist.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to delete all compression history?", "Clear History", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _historyService.ClearAll();
                    MessageBox.Show("History cleared successfully.", "Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var btnResetSettings = new ModernButton { Text = "Reset Settings to Defaults", Style = ButtonStyle.Secondary, Width = 200, Height = 36, Location = new Point(180, 50) };
            btnResetSettings.Click += (s, e) =>
            {
                if (MessageBox.Show("Reset all settings to default values?", "Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _settingsService.ResetToDefaults();
                    LoadCurrentSettings();
                    AppTheme.SetTheme(AppThemeMode.Dark);
                    MessageBox.Show("Settings reset to defaults.", "Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            cardMaintenance.Controls.Add(btnResetSettings);
            cardMaintenance.Controls.Add(btnClearHist);
            cardMaintenance.Controls.Add(lblMaintHeader);
            rootLayout.Controls.Add(cardMaintenance);

            Controls.Add(rootLayout);
        }

        private void LoadCurrentSettings()
        {
            var s = _settingsService.Current;
            _cbTheme.SelectedIndex = s.Theme == AppThemeMode.Light ? 1 : 0;

            _cbDefaultPreset.SelectedIndex = s.DefaultPreset switch
            {
                CompressionPreset.Extreme => 1,
                CompressionPreset.HighQuality => 2,
                CompressionPreset.LowCompression => 3,
                _ => 0
            };

            _numConcurrency.Value = Math.Max(1, Math.Min(8, s.ConcurrencyLimit));
            _chkRetainLarger.Checked = s.RetainOriginalIfLarger;
            _chkOpenFolder.Checked = s.OpenOutputFolderOnComplete;

            switch (s.OutputDirType)
            {
                case OutputDirectoryType.SameAsOriginal:
                    _rbSameFolder.Checked = true;
                    break;
                case OutputDirectoryType.CustomFolder:
                    _rbCustomFolder.Checked = true;
                    _txtCustomFolder.Text = s.CustomOutputFolder;
                    break;
                case OutputDirectoryType.CompressedSubfolder:
                default:
                    _rbSubfolder.Checked = true;
                    break;
            }
        }

        private void SaveSettings()
        {
            var s = _settingsService.Current;
            s.Theme = _cbTheme.SelectedIndex == 1 ? AppThemeMode.Light : AppThemeMode.Dark;
            s.DefaultPreset = _cbDefaultPreset.SelectedIndex switch
            {
                1 => CompressionPreset.Extreme,
                2 => CompressionPreset.HighQuality,
                3 => CompressionPreset.LowCompression,
                _ => CompressionPreset.Recommended
            };
            s.ConcurrencyLimit = (int)_numConcurrency.Value;
            s.RetainOriginalIfLarger = _chkRetainLarger.Checked;
            s.OpenOutputFolderOnComplete = _chkOpenFolder.Checked;

            if (_rbSameFolder.Checked)
            {
                s.OutputDirType = OutputDirectoryType.SameAsOriginal;
            }
            else if (_rbCustomFolder.Checked)
            {
                s.OutputDirType = OutputDirectoryType.CustomFolder;
                s.CustomOutputFolder = _txtCustomFolder.Text.Trim();
            }
            else
            {
                s.OutputDirType = OutputDirectoryType.CompressedSubfolder;
            }

            _settingsService.Save();
        }

        private void ApplyTheme()
        {
            var p = AppTheme.Current;
            BackColor = p.Background;
            ForeColor = p.TextPrimary;

            _txtCustomFolder.BackColor = p.InputBackground;
            _txtCustomFolder.ForeColor = p.TextPrimary;

            _numConcurrency.BackColor = p.InputBackground;
            _numConcurrency.ForeColor = p.TextPrimary;

            Invalidate(true);
        }
    }
}
