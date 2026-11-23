using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PDFCompressor.App.UI.Controls;
using PDFCompressor.App.UI.Theme;
using PDFCompressor.Core.Models;
using PDFCompressor.Core.Services;

namespace PDFCompressor.App.UI.Views
{
    public class CompressView : UserControl
    {
        private readonly PdfCompressionEngine _engine;
        private readonly SettingsService _settings;
        private string? _currentFilePath;
        private CancellationTokenSource? _cts;
        private CompressionResult? _lastResult;

        // UI Controls
        private DropZoneControl _dropZone = null!;
        private CardPanel _fileInfoCard = null!;
        private Label _lblFileName = null!;
        private Label _lblFileSize = null!;
        private Label _lblFilePages = null!;
        private Label _lblSigWarning = null!;

        // Presets
        private RadioButton _rbExtreme = null!;
        private RadioButton _rbRecommended = null!;
        private RadioButton _rbHighQuality = null!;
        private RadioButton _rbLow = null!;
        private RadioButton _rbCustom = null!;

        // Advanced Panel
        private CardPanel _advancedCard = null!;
        private TrackBar _tbQuality = null!;
        private Label _lblQualityValue = null!;
        private ModernComboBox _cbDpi = null!;
        private CheckBox _chkEmbedFonts = null!;
        private CheckBox _chkLinearize = null!;
        private CheckBox _chkObjectStreams = null!;
        private CheckBox _chkDownsample = null!;
        private CheckBox _chkRetainIfLarger = null!;

        // Progress
        private CardPanel _progressCard = null!;
        private ModernProgressBar _progressBar = null!;
        private Label _lblProgressStatus = null!;

        // Results
        private CardPanel _resultCard = null!;
        private Label _lblResultOrigSize = null!;
        private Label _lblResultCompSize = null!;
        private Label _lblResultSaved = null!;
        private Label _lblResultPercent = null!;
        private Label _lblResultTime = null!;
        private Label _lblResultStatus = null!;

        // Action Buttons
        private ModernButton _btnCompress = null!;
        private ModernButton _btnCancel = null!;
        private ModernButton _btnOpenPdf = null!;
        private ModernButton _btnOpenFolder = null!;
        private ModernButton _btnCompressAnother = null!;
        private ModernButton _btnSaveAs = null!;
        private ModernButton _btnCopyPath = null!;

        public CompressView(PdfCompressionEngine engine, SettingsService settings)
        {
            _engine = engine;
            _settings = settings;

            InitializeComponent();
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
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Resize rootLayout child cards when parent resizes
            Resize += (s, e) =>
            {
                int targetWidth = Math.Max(700, ClientSize.Width - 60);
                rootLayout.Width = targetWidth;
                foreach (Control c in rootLayout.Controls)
                {
                    c.Width = targetWidth;
                }
            };

            // Header
            var header = new Label
            {
                Text = "Single PDF Compression",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };
            rootLayout.Controls.Add(header);

            // 1. Drop Zone
            _dropZone = new DropZoneControl
            {
                Height = 150,
                Margin = new Padding(0, 0, 0, 14),
                AllowMultipleFiles = false
            };
            _dropZone.FilesSelected += files =>
            {
                if (files.Length > 0) LoadFile(files[0]);
            };
            rootLayout.Controls.Add(_dropZone);

            // 2. Selected File Info Card
            _fileInfoCard = new CardPanel
            {
                Height = 88,
                Visible = false,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(16, 12, 16, 12)
            };

            _lblFileName = new Label
            {
                Text = "File: -",
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                Location = new Point(16, 12),
                Width = 520,
                AutoEllipsis = true
            };
            _lblFileSize = new Label
            {
                Text = "Size: -",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Location = new Point(16, 38),
                Width = 180
            };
            _lblFilePages = new Label
            {
                Text = "Pages: -",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Location = new Point(200, 38),
                Width = 160
            };
            _lblSigWarning = new Label
            {
                Text = "⚠ Document contains digital signatures. Compression will invalidate them.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = AppTheme.Current.Warning,
                Location = new Point(16, 62),
                Width = 600,
                Visible = false
            };

            var btnChangeFile = new ModernButton
            {
                Text = "Change File",
                Style = ButtonStyle.Secondary,
                Width = 110,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(720, 14)
            };
            _fileInfoCard.Resize += (s, e) =>
            {
                btnChangeFile.Location = new Point(_fileInfoCard.Width - btnChangeFile.Width - 16, 14);
                _lblFileName.Width = Math.Max(200, _fileInfoCard.Width - btnChangeFile.Width - 40);
            };
            btnChangeFile.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog { Filter = "PDF Documents (*.pdf)|*.pdf" };
                if (ofd.ShowDialog() == DialogResult.OK) LoadFile(ofd.FileName);
            };

            _fileInfoCard.Controls.Add(btnChangeFile);
            _fileInfoCard.Controls.Add(_lblSigWarning);
            _fileInfoCard.Controls.Add(_lblFilePages);
            _fileInfoCard.Controls.Add(_lblFileSize);
            _fileInfoCard.Controls.Add(_lblFileName);
            rootLayout.Controls.Add(_fileInfoCard);

            // 3. Preset Selector Card
            var presetCard = new CardPanel
            {
                Height = 195,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(16, 12, 16, 14)
            };

            var presetHeader = new Label
            {
                Text = "Select Compression Preset",
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 26
            };
            presetCard.Controls.Add(presetHeader);

            var presetList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            _rbExtreme = CreatePresetRadio("Extreme", "Maximum reduction • Lower image quality (72 DPI)");
            _rbRecommended = CreatePresetRadio("Recommended", "Balanced size & visual quality (150 DPI) — Default");
            _rbHighQuality = CreatePresetRadio("High Quality", "Preserve crisp visuals • Moderate reduction (300 DPI)");
            _rbLow = CreatePresetRadio("Low Compression", "Minimal optimization • Highest fidelity");
            _rbCustom = CreatePresetRadio("Custom Settings", "Configure custom DPI, JPEG quality & advanced options");

            presetList.Controls.Add(_rbExtreme);
            presetList.Controls.Add(_rbRecommended);
            presetList.Controls.Add(_rbHighQuality);
            presetList.Controls.Add(_rbLow);
            presetList.Controls.Add(_rbCustom);
            presetCard.Controls.Add(presetList);

            _rbRecommended.Checked = true;
            _rbCustom.CheckedChanged += (s, e) => _advancedCard.Visible = _rbCustom.Checked;
            rootLayout.Controls.Add(presetCard);

            // 4. Advanced Settings Card (Collapsible)
            _advancedCard = new CardPanel
            {
                Height = 180,
                Visible = false,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(16)
            };

            var advHeader = new Label
            {
                Text = "Advanced Compression Options",
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true
            };
            _advancedCard.Controls.Add(advHeader);

            var lblDpi = new Label { Text = "Resolution (DPI):", Location = new Point(16, 44), AutoSize = true };
            _cbDpi = new ModernComboBox { Location = new Point(140, 40), Width = 130, Height = 32 };
            _cbDpi.Items.AddRange(new object[] { "72 (Screen)", "150 (eBook)", "300 (Print)", "600 (High)" });
            _cbDpi.SelectedIndex = 1;
            _advancedCard.Controls.Add(lblDpi);
            _advancedCard.Controls.Add(_cbDpi);

            var lblQuality = new Label { Text = "JPEG Quality:", Location = new Point(290, 44), AutoSize = true };
            _tbQuality = new TrackBar { Location = new Point(380, 38), Width = 200, Minimum = 10, Maximum = 100, Value = 75, TickFrequency = 10 };
            _lblQualityValue = new Label { Text = "75%", Location = new Point(590, 44), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) };
            _tbQuality.ValueChanged += (s, e) => _lblQualityValue.Text = $"{_tbQuality.Value}%";
            _advancedCard.Controls.Add(lblQuality);
            _advancedCard.Controls.Add(_tbQuality);
            _advancedCard.Controls.Add(_lblQualityValue);

            _chkDownsample = new CheckBox { Text = "Downsample Images", Checked = true, Location = new Point(16, 80), AutoSize = true };
            _chkEmbedFonts = new CheckBox { Text = "Embed & Subset Fonts", Checked = true, Location = new Point(200, 80), AutoSize = true };
            _chkLinearize = new CheckBox { Text = "Linearize (Fast Web View)", Checked = true, Location = new Point(390, 80), AutoSize = true };
            _chkObjectStreams = new CheckBox { Text = "Optimize Object Streams", Checked = true, Location = new Point(580, 80), AutoSize = true };
            _chkRetainIfLarger = new CheckBox { Text = "Retain original PDF if compressed file is larger", Checked = true, Location = new Point(16, 114), AutoSize = true };

            _advancedCard.Controls.Add(_chkDownsample);
            _advancedCard.Controls.Add(_chkEmbedFonts);
            _advancedCard.Controls.Add(_chkLinearize);
            _advancedCard.Controls.Add(_chkObjectStreams);
            _advancedCard.Controls.Add(_chkRetainIfLarger);
            rootLayout.Controls.Add(_advancedCard);

            // 5. Actions & Compress Button Row
            var actionRow = new FlowLayoutPanel
            {
                Height = 48,
                Margin = new Padding(0, 0, 0, 14),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            _btnCompress = new ModernButton
            {
                Text = "⚡  Compress PDF Now",
                Style = ButtonStyle.Primary,
                Width = 210,
                Height = 42,
                Enabled = false,
                Margin = new Padding(0, 0, 12, 0)
            };
            _btnCompress.Click += async (s, e) => await StartCompressionAsync();

            _btnCancel = new ModernButton
            {
                Text = "Cancel",
                Style = ButtonStyle.Danger,
                Width = 100,
                Height = 42,
                Visible = false,
                Margin = new Padding(0)
            };
            _btnCancel.Click += (s, e) => _cts?.Cancel();

            actionRow.Controls.Add(_btnCompress);
            actionRow.Controls.Add(_btnCancel);
            rootLayout.Controls.Add(actionRow);

            // 6. Progress Card
            _progressCard = new CardPanel
            {
                Height = 84,
                Visible = false,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(16, 12, 16, 12)
            };
            _lblProgressStatus = new Label
            {
                Text = "Preparing...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Dock = DockStyle.Top,
                Height = 26
            };
            _progressBar = new ModernProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 16,
                ShowPercentageText = false
            };
            _progressCard.Controls.Add(_progressBar);
            _progressCard.Controls.Add(_lblProgressStatus);
            rootLayout.Controls.Add(_progressCard);

            // 7. Results Card
            _resultCard = new CardPanel
            {
                Height = 190,
                Visible = false,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(18, 14, 18, 14)
            };

            var resHeader = new Label
            {
                Text = "Compression Results",
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true
            };
            _lblResultStatus = new Label
            {
                Text = "✓ Compression Succeeded",
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = AppTheme.Current.Success,
                Location = new Point(16, 38),
                AutoSize = true
            };

            // Metrics row
            var metricsPanel = new FlowLayoutPanel
            {
                Location = new Point(16, 64),
                Height = 30,
                Width = 800,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            _lblResultOrigSize = new Label { Text = "Original: -", AutoSize = true, Margin = new Padding(0, 4, 18, 0), Font = new Font("Segoe UI", 9.5f) };
            _lblResultCompSize = new Label { Text = "Compressed: -", AutoSize = true, Margin = new Padding(0, 4, 18, 0), Font = new Font("Segoe UI", 9.5f) };
            _lblResultSaved = new Label { Text = "Space Saved: -", AutoSize = true, Margin = new Padding(0, 4, 18, 0), Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold), ForeColor = AppTheme.Current.Success };
            _lblResultPercent = new Label { Text = "Reduction: -", AutoSize = true, Margin = new Padding(0, 3, 18, 0), Font = new Font("Segoe UI Bold", 10.5f, FontStyle.Bold), ForeColor = AppTheme.Current.Accent };

            metricsPanel.Controls.Add(_lblResultOrigSize);
            metricsPanel.Controls.Add(_lblResultCompSize);
            metricsPanel.Controls.Add(_lblResultSaved);
            metricsPanel.Controls.Add(_lblResultPercent);

            _lblResultTime = new Label
            {
                Text = "Processing Time: -",
                Location = new Point(16, 96),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AppTheme.Current.TextMuted
            };

            // Result Action Buttons in a FLOW LAYOUT PANEL TO GUARANTEE NO OVERLAP
            var resultButtons = new FlowLayoutPanel
            {
                Location = new Point(16, 124),
                Height = 44,
                Width = 800,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            _btnOpenPdf = new ModernButton { Text = "Open PDF", Style = ButtonStyle.Primary, Width = 115, Height = 36, Margin = new Padding(0, 0, 10, 0) };
            _btnOpenPdf.Click += (s, e) => OpenResultPdf();

            _btnOpenFolder = new ModernButton { Text = "Open Folder", Style = ButtonStyle.Secondary, Width = 115, Height = 36, Margin = new Padding(0, 0, 10, 0) };
            _btnOpenFolder.Click += (s, e) => OpenResultFolder();

            _btnSaveAs = new ModernButton { Text = "Save As...", Style = ButtonStyle.Secondary, Width = 110, Height = 36, Margin = new Padding(0, 0, 10, 0) };
            _btnSaveAs.Click += (s, e) => SaveAsDialog();

            _btnCopyPath = new ModernButton { Text = "Copy Path", Style = ButtonStyle.Secondary, Width = 110, Height = 36, Margin = new Padding(0, 0, 10, 0) };
            _btnCopyPath.Click += (s, e) => CopyResultPath();

            _btnCompressAnother = new ModernButton { Text = "Compress Another", Style = ButtonStyle.Secondary, Width = 150, Height = 36, Margin = new Padding(0, 0, 10, 0) };
            _btnCompressAnother.Click += (s, e) => ResetForNewFile();

            resultButtons.Controls.Add(_btnOpenPdf);
            resultButtons.Controls.Add(_btnOpenFolder);
            resultButtons.Controls.Add(_btnSaveAs);
            resultButtons.Controls.Add(_btnCopyPath);
            resultButtons.Controls.Add(_btnCompressAnother);

            _resultCard.Controls.Add(resultButtons);
            _resultCard.Controls.Add(_lblResultTime);
            _resultCard.Controls.Add(metricsPanel);
            _resultCard.Controls.Add(_lblResultStatus);
            _resultCard.Controls.Add(resHeader);
            rootLayout.Controls.Add(_resultCard);

            Controls.Add(rootLayout);
        }

        private RadioButton CreatePresetRadio(string title, string description)
        {
            var rb = new RadioButton
            {
                Text = $"{title} — {description}",
                Width = 720,
                Height = 24,
                Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 0, 3)
            };
            return rb;
        }

        public async void LoadFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            _currentFilePath = filePath;
            var fi = new FileInfo(filePath);

            _lblFileName.Text = $"File: {fi.Name}";
            _lblFileSize.Text = $"Size: {CompressionResult.FormatFileSize(fi.Length)}";
            _lblFilePages.Text = "Pages: Loading...";
            _lblSigWarning.Visible = false;

            _fileInfoCard.Visible = true;
            _btnCompress.Enabled = true;
            _resultCard.Visible = false;

            try
            {
                var inspection = await _engine.Qpdf.InspectPdfAsync(filePath);
                _lblFilePages.Text = inspection.PageCount > 0 ? $"Pages: {inspection.PageCount}" : "Pages: -";
                if (inspection.RequiresPassword)
                {
                    _lblFilePages.Text = "Password Protected!";
                    _lblFilePages.ForeColor = AppTheme.Current.Danger;
                }
            }
            catch { }
        }

        private CompressionOptions BuildCurrentOptions()
        {
            CompressionPreset preset = CompressionPreset.Recommended;
            if (_rbExtreme.Checked) preset = CompressionPreset.Extreme;
            else if (_rbHighQuality.Checked) preset = CompressionPreset.HighQuality;
            else if (_rbLow.Checked) preset = CompressionPreset.LowCompression;
            else if (_rbCustom.Checked) preset = CompressionPreset.Custom;

            var options = CompressionOptions.FromPreset(preset);

            if (_rbCustom.Checked)
            {
                int dpi = _cbDpi.SelectedIndex switch
                {
                    0 => 72,
                    1 => 150,
                    2 => 300,
                    3 => 600,
                    _ => 150
                };
                options.ColorImageResolution = dpi;
                options.GrayImageResolution = dpi;
                options.MonoImageResolution = Math.Min(1200, dpi * 2);
                options.JpegQuality = _tbQuality.Value;
                options.DownsampleColorImages = _chkDownsample.Checked;
                options.DownsampleGrayImages = _chkDownsample.Checked;
                options.DownsampleMonoImages = _chkDownsample.Checked;
                options.EmbedAllFonts = _chkEmbedFonts.Checked;
                options.LinearizeForFastWebView = _chkLinearize.Checked;
                options.OptimizeObjectStreams = _chkObjectStreams.Checked;
                options.RetainOriginalIfLarger = _chkRetainIfLarger.Checked;
            }

            options.OutputDirType = _settings.Current.OutputDirType;
            options.CustomOutputFolder = _settings.Current.CustomOutputFolder;
            return options;
        }

        private async Task StartCompressionAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
            {
                MessageBox.Show("Please select a valid PDF file first.", "No File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnCompress.Enabled = false;
            _btnCancel.Visible = true;
            _progressCard.Visible = true;
            _resultCard.Visible = false;
            _progressBar.Value = 0;

            _cts = new CancellationTokenSource();

            var progress = new Progress<CompressionProgressReport>(r =>
            {
                _progressBar.Value = r.Percent;
                _lblProgressStatus.Text = r.StatusMessage;
            });

            var options = BuildCurrentOptions();

            try
            {
                _lastResult = await _engine.CompressAsync(_currentFilePath, options, progress, _cts.Token);
                DisplayResults(_lastResult);

                if (_lastResult.Status == CompressionStatus.Succeeded && _settings.Current.OpenOutputFolderOnComplete)
                {
                    OpenResultFolder();
                }
            }
            catch (OperationCanceledException)
            {
                _lblProgressStatus.Text = "Compression was cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Compression failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnCompress.Enabled = true;
                _btnCancel.Visible = false;
                _progressCard.Visible = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void DisplayResults(CompressionResult result)
        {
            _resultCard.Visible = true;

            if (result.Status == CompressionStatus.Succeeded)
            {
                _lblResultStatus.Text = "✓ Compression Succeeded!";
                _lblResultStatus.ForeColor = AppTheme.Current.Success;

                _lblResultOrigSize.Text = $"Original Size: {CompressionResult.FormatFileSize(result.OriginalSizeBytes)}";
                _lblResultCompSize.Text = $"Compressed Size: {CompressionResult.FormatFileSize(result.CompressedSizeBytes)}";
                _lblResultSaved.Text = $"Space Saved: {CompressionResult.FormatFileSize(result.SavedBytes)}";
                _lblResultPercent.Text = $"Reduction: {result.SavedPercentage:0.#}%";
                _lblResultTime.Text = $"Processing Time: {result.ProcessingDuration.TotalSeconds:0.##}s";

                _btnOpenPdf.Enabled = true;
                _btnOpenFolder.Enabled = true;
                _btnSaveAs.Enabled = true;
                _btnCopyPath.Enabled = true;
            }
            else if (result.Status == CompressionStatus.SkippedLarger)
            {
                _lblResultStatus.Text = "ℹ Retained Original (Document already optimized)";
                _lblResultStatus.ForeColor = AppTheme.Current.Warning;

                _lblResultOrigSize.Text = $"Original Size: {CompressionResult.FormatFileSize(result.OriginalSizeBytes)}";
                _lblResultCompSize.Text = $"Compressed Size: {CompressionResult.FormatFileSize(result.CompressedSizeBytes)}";
                _lblResultSaved.Text = "Space Saved: 0 B";
                _lblResultPercent.Text = "Reduction: 0%";
                _lblResultTime.Text = $"Time: {result.ProcessingDuration.TotalSeconds:0.##}s";

                _btnOpenPdf.Enabled = false;
                _btnOpenFolder.Enabled = false;
                _btnSaveAs.Enabled = false;
                _btnCopyPath.Enabled = false;
            }
            else
            {
                _lblResultStatus.Text = $"✗ Failed: {result.ErrorMessage}";
                _lblResultStatus.ForeColor = AppTheme.Current.Danger;

                _lblResultOrigSize.Text = $"Original Size: {CompressionResult.FormatFileSize(result.OriginalSizeBytes)}";
                _lblResultCompSize.Text = "-";
                _lblResultSaved.Text = "-";
                _lblResultPercent.Text = "-";
                _lblResultTime.Text = $"Time: {result.ProcessingDuration.TotalSeconds:0.##}s";

                _btnOpenPdf.Enabled = false;
                _btnOpenFolder.Enabled = false;
                _btnSaveAs.Enabled = false;
                _btnCopyPath.Enabled = false;
            }
        }

        private void OpenResultPdf()
        {
            if (_lastResult != null && File.Exists(_lastResult.OutputFilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = _lastResult.OutputFilePath, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OpenResultFolder()
        {
            if (_lastResult != null && File.Exists(_lastResult.OutputFilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{_lastResult.OutputFilePath}\"", UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveAsDialog()
        {
            if (_lastResult == null || !File.Exists(_lastResult.OutputFilePath)) return;

            using var sfd = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = Path.GetFileName(_lastResult.OutputFilePath),
                Title = "Save Compressed PDF As"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.Copy(_lastResult.OutputFilePath, sfd.FileName, true);
                    MessageBox.Show("Saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CopyResultPath()
        {
            if (_lastResult != null && !string.IsNullOrEmpty(_lastResult.OutputFilePath))
            {
                Clipboard.SetText(_lastResult.OutputFilePath);
                MessageBox.Show("File path copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ResetForNewFile()
        {
            _currentFilePath = null;
            _lastResult = null;
            _fileInfoCard.Visible = false;
            _resultCard.Visible = false;
            _progressCard.Visible = false;
            _btnCompress.Enabled = false;
        }

        private void ApplyTheme()
        {
            var p = AppTheme.Current;
            BackColor = p.Background;
            ForeColor = p.TextPrimary;

            _lblFileName.ForeColor = p.TextPrimary;
            _lblFileSize.ForeColor = p.TextSecondary;
            _lblFilePages.ForeColor = p.TextSecondary;

            _rbExtreme.ForeColor = p.TextPrimary;
            _rbRecommended.ForeColor = p.TextPrimary;
            _rbHighQuality.ForeColor = p.TextPrimary;
            _rbLow.ForeColor = p.TextPrimary;
            _rbCustom.ForeColor = p.TextPrimary;

            _chkDownsample.ForeColor = p.TextPrimary;
            _chkEmbedFonts.ForeColor = p.TextPrimary;
            _chkLinearize.ForeColor = p.TextPrimary;
            _chkObjectStreams.ForeColor = p.TextPrimary;
            _chkRetainIfLarger.ForeColor = p.TextPrimary;

            _lblProgressStatus.ForeColor = p.TextPrimary;
            _lblResultOrigSize.ForeColor = p.TextSecondary;
            _lblResultCompSize.ForeColor = p.TextSecondary;

            Invalidate(true);
        }
    }
}
