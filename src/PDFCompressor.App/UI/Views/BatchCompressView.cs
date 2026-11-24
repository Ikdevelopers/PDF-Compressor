using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PDFCompressor.App.UI.Controls;
using PDFCompressor.App.UI.Theme;
using PDFCompressor.Core.Models;
using PDFCompressor.Core.Services;

namespace PDFCompressor.App.UI.Views
{
    public class BatchFileItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => Path.GetFileName(FilePath);
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double ReductionPercentage { get; set; }
        public string Status { get; set; } = "Pending";
        public string? ErrorMessage { get; set; }
        public string? OutputPath { get; set; }
    }

    public class BatchCompressView : UserControl
    {
        private readonly PdfCompressionEngine _engine;
        private readonly SettingsService _settings;
        private readonly List<BatchFileItem> _items = new();
        private CancellationTokenSource? _cts;
        private bool _isProcessing;

        // UI Controls
        private DataGridView _grid = null!;
        private ModernComboBox _cbPreset = null!;
        private ModernProgressBar _batchProgress = null!;
        private Label _lblBatchStatus = null!;
        private Label _lblSummary = null!;
        private ModernButton _btnStart = null!;
        private ModernButton _btnCancel = null!;
        private ModernButton _btnOpenOutput = null!;
        private DropZoneControl _dropZone = null!;

        public BatchCompressView(PdfCompressionEngine engine, SettingsService settings)
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
            Padding = new Padding(24, 16, 24, 20);

            var mainPanel = new Panel { Dock = DockStyle.Fill };

            // Top Header & Actions in a clean vertical FlowLayoutPanel
            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            var lblTitle = new Label
            {
                Text = "Batch PDF Compression",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            topPanel.Controls.Add(lblTitle);

            // Controls Toolbar Row
            var toolbar = new FlowLayoutPanel
            {
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10),
                Width = 900
            };

            var btnAddFiles = new ModernButton { Text = "+ Add Files", Style = ButtonStyle.Primary, Width = 110, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            btnAddFiles.Click += (s, e) => AddFilesDialog();

            var btnAddFolder = new ModernButton { Text = "+ Add Folder", Style = ButtonStyle.Secondary, Width = 110, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            btnAddFolder.Click += (s, e) => AddFolderDialog();

            var btnRemove = new ModernButton { Text = "Remove Selected", Style = ButtonStyle.Secondary, Width = 130, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            btnRemove.Click += (s, e) => RemoveSelected();

            var btnClear = new ModernButton { Text = "Clear All", Style = ButtonStyle.Secondary, Width = 90, Height = 34, Margin = new Padding(0, 0, 16, 0) };
            btnClear.Click += (s, e) => ClearAll();

            var lblPreset = new Label { Text = "Preset:", Margin = new Padding(0, 8, 6, 0), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f) };
            _cbPreset = new ModernComboBox { Width = 200, Height = 32, Margin = new Padding(0, 2, 0, 0) };
            _cbPreset.Items.AddRange(new object[] { "Recommended (150 DPI)", "Extreme (72 DPI)", "High Quality (300 DPI)", "Low Compression" });
            _cbPreset.SelectedIndex = 0;

            toolbar.Controls.Add(btnAddFiles);
            toolbar.Controls.Add(btnAddFolder);
            toolbar.Controls.Add(btnRemove);
            toolbar.Controls.Add(btnClear);
            toolbar.Controls.Add(lblPreset);
            toolbar.Controls.Add(_cbPreset);
            topPanel.Controls.Add(toolbar);

            // Quick Drop Strip
            _dropZone = new DropZoneControl
            {
                Height = 110,
                Width = 900,
                AllowMultipleFiles = true,
                Margin = new Padding(0, 0, 0, 14)
            };
            _dropZone.SetLabels("Drag and drop multiple PDF files or folders here", "Files will be added to the queue below");
            _dropZone.FilesSelected += files => AddFiles(files);
            topPanel.Controls.Add(_dropZone);

            // Responsive width resize for toolbar & dropzone
            Resize += (s, e) =>
            {
                int w = Math.Max(700, ClientSize.Width - 52);
                toolbar.Width = w;
                _dropZone.Width = w;
            };

            // Center DataGridView in CardPanel
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                AutoGenerateColumns = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 32 },
                Font = new Font("Segoe UI", 9.25f)
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "File Name", DataPropertyName = "FileName", Width = 280, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Original Size", DataPropertyName = "OriginalSizeStr", Width = 110 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", Width = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Compressed Size", DataPropertyName = "CompressedSizeStr", Width = 125 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Space Saved", DataPropertyName = "SavedStr", Width = 115 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Reduction", DataPropertyName = "ReductionStr", Width = 95 });

            var gridContainer = new CardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                Margin = new Padding(0, 8, 0, 8)
            };
            gridContainer.Controls.Add(_grid);

            // Bottom Progress & Action Bar
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 95 };

            _lblBatchStatus = new Label
            {
                Text = "Ready to compress (0 files queued)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Location = new Point(0, 6),
                AutoSize = true
            };

            _lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Location = new Point(450, 6),
                AutoSize = true,
                ForeColor = AppTheme.Current.Success
            };

            _batchProgress = new ModernProgressBar
            {
                Dock = DockStyle.None,
                Location = new Point(0, 28),
                Width = 900,
                Height = 12,
                ShowPercentageText = false
            };

            var bottomButtons = new FlowLayoutPanel
            {
                Location = new Point(0, 48),
                Height = 44,
                Width = 900,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _btnStart = new ModernButton { Text = "▶  Start Batch Compression", Style = ButtonStyle.Primary, Width = 220, Height = 38, Margin = new Padding(0, 0, 10, 0) };
            _btnStart.Click += async (s, e) => await StartBatchAsync();

            _btnCancel = new ModernButton { Text = "Cancel Batch", Style = ButtonStyle.Danger, Width = 120, Height = 38, Margin = new Padding(0, 0, 10, 0), Visible = false };
            _btnCancel.Click += (s, e) => _cts?.Cancel();

            _btnOpenOutput = new ModernButton { Text = "Open Output Folder", Style = ButtonStyle.Secondary, Width = 160, Height = 38, Enabled = false };
            _btnOpenOutput.Click += (s, e) => OpenOutputDirectory();

            bottomButtons.Controls.Add(_btnStart);
            bottomButtons.Controls.Add(_btnCancel);
            bottomButtons.Controls.Add(_btnOpenOutput);

            bottomPanel.Controls.Add(bottomButtons);
            bottomPanel.Controls.Add(_batchProgress);
            bottomPanel.Controls.Add(_lblSummary);
            bottomPanel.Controls.Add(_lblBatchStatus);

            bottomPanel.Resize += (s, e) =>
            {
                _batchProgress.Width = Math.Max(700, bottomPanel.Width);
                bottomButtons.Width = Math.Max(700, bottomPanel.Width);
            };

            // STRICT DOCKING ORDER: Add gridContainer, bottomPanel, topPanel, and send top & bottom to back
            // so gridContainer (Dock = Fill) occupies only the exact space between them.
            mainPanel.Controls.Add(gridContainer);
            mainPanel.Controls.Add(bottomPanel);
            mainPanel.Controls.Add(topPanel);

            topPanel.SendToBack();
            bottomPanel.SendToBack();
            gridContainer.BringToFront();

            Controls.Add(mainPanel);
        }

        private void AddFilesDialog()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "PDF Documents (*.pdf)|*.pdf",
                Multiselect = true,
                Title = "Select PDF Files for Batch Compression"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                AddFiles(ofd.FileNames);
            }
        }

        private void AddFolderDialog()
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Select Folder Containing PDF Files"
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var pdfs = Directory.GetFiles(fbd.SelectedPath, "*.pdf", SearchOption.AllDirectories);
                    AddFiles(pdfs);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not read folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void AddFiles(IEnumerable<string> filePaths)
        {
            foreach (var path in filePaths)
            {
                if (File.Exists(path) && path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_items.Any(i => string.Equals(i.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                    {
                        var fi = new FileInfo(path);
                        _items.Add(new BatchFileItem
                        {
                            FilePath = path,
                            OriginalSize = fi.Length
                        });
                    }
                }
            }
            RefreshGrid();
        }

        private void RemoveSelected()
        {
            if (_isProcessing) return;
            var selectedIndices = _grid.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).OrderByDescending(i => i).ToList();
            foreach (var idx in selectedIndices)
            {
                if (idx >= 0 && idx < _items.Count)
                {
                    _items.RemoveAt(idx);
                }
            }
            RefreshGrid();
        }

        private void ClearAll()
        {
            if (_isProcessing) return;
            _items.Clear();
            _lblSummary.Text = "";
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            var dataSource = _items.Select(i => new
            {
                i.FileName,
                OriginalSizeStr = CompressionResult.FormatFileSize(i.OriginalSize),
                i.Status,
                CompressedSizeStr = i.CompressedSize > 0 ? CompressionResult.FormatFileSize(i.CompressedSize) : "-",
                SavedStr = i.OriginalSize > i.CompressedSize && i.CompressedSize > 0 ? CompressionResult.FormatFileSize(i.OriginalSize - i.CompressedSize) : "-",
                ReductionStr = i.ReductionPercentage > 0 ? $"{i.ReductionPercentage:0.#}%" : "-"
            }).ToList();

            _grid.DataSource = dataSource;
            _lblBatchStatus.Text = $"{_items.Count} files in queue";
            _btnStart.Enabled = _items.Count > 0 && !_isProcessing;
        }

        private CompressionPreset GetSelectedPreset()
        {
            return _cbPreset.SelectedIndex switch
            {
                0 => CompressionPreset.Recommended,
                1 => CompressionPreset.Extreme,
                2 => CompressionPreset.HighQuality,
                3 => CompressionPreset.LowCompression,
                _ => CompressionPreset.Recommended
            };
        }

        private async Task StartBatchAsync()
        {
            if (_items.Count == 0 || _isProcessing) return;

            _isProcessing = true;
            _btnStart.Enabled = false;
            _btnCancel.Visible = true;
            _btnOpenOutput.Enabled = false;
            _batchProgress.Value = 0;
            _batchProgress.Maximum = _items.Count;
            _lblSummary.Text = "";

            _cts = new CancellationTokenSource();
            var preset = GetSelectedPreset();
            var baseOptions = CompressionOptions.FromPreset(preset);
            baseOptions.OutputDirType = _settings.Current.OutputDirType;
            baseOptions.CustomOutputFolder = _settings.Current.CustomOutputFolder;
            baseOptions.RetainOriginalIfLarger = _settings.Current.RetainOriginalIfLarger;

            int completed = 0;
            int succeeded = 0;
            int failed = 0;
            long totalSavedBytes = 0;
            string? sampleOutputFolder = null;

            int maxConcurrency = Math.Max(1, Math.Min(4, _settings.Current.ConcurrencyLimit));
            var semaphore = new SemaphoreSlim(maxConcurrency);

            var tasks = _items.Select(async item =>
            {
                await semaphore.WaitAsync(_cts.Token);
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    item.Status = "Compressing...";
                    BeginInvoke(RefreshGrid);

                    var result = await _engine.CompressAsync(item.FilePath, baseOptions, null, _cts.Token);

                    item.CompressedSize = result.CompressedSizeBytes;
                    item.ReductionPercentage = result.SavedPercentage;
                    item.OutputPath = result.OutputFilePath;

                    if (result.Status == CompressionStatus.Succeeded)
                    {
                        item.Status = "Succeeded";
                        Interlocked.Increment(ref succeeded);
                        Interlocked.Add(ref totalSavedBytes, result.SavedBytes);
                        sampleOutputFolder = Path.GetDirectoryName(result.OutputFilePath);
                    }
                    else if (result.Status == CompressionStatus.SkippedLarger)
                    {
                        item.Status = "Skipped (Larger)";
                    }
                    else
                    {
                        item.Status = "Failed";
                        item.ErrorMessage = result.ErrorMessage;
                        Interlocked.Increment(ref failed);
                    }
                }
                catch (OperationCanceledException)
                {
                    item.Status = "Cancelled";
                }
                catch (Exception ex)
                {
                    item.Status = "Error";
                    item.ErrorMessage = ex.Message;
                    Interlocked.Increment(ref failed);
                }
                finally
                {
                    semaphore.Release();
                    int cur = Interlocked.Increment(ref completed);
                    BeginInvoke(() =>
                    {
                        _batchProgress.Value = Math.Min(_batchProgress.Maximum, cur);
                        _lblBatchStatus.Text = $"Completed {cur} of {_items.Count} files...";
                        RefreshGrid();
                    });
                }
            }).ToList();

            try
            {
                await Task.WhenAll(tasks);
                _lblBatchStatus.Text = $"Batch finished: {succeeded} succeeded, {failed} failed.";
                _lblSummary.Text = $"Saved {CompressionResult.FormatFileSize(totalSavedBytes)} in total!";

                if (!string.IsNullOrEmpty(sampleOutputFolder) && Directory.Exists(sampleOutputFolder))
                {
                    _btnOpenOutput.Enabled = true;
                    if (_settings.Current.OpenOutputFolderOnComplete)
                    {
                        OpenOutputDirectory();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _lblBatchStatus.Text = "Batch operation was cancelled.";
            }
            finally
            {
                _isProcessing = false;
                _btnStart.Enabled = true;
                _btnCancel.Visible = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void OpenOutputDirectory()
        {
            var firstSuccess = _items.FirstOrDefault(i => !string.IsNullOrEmpty(i.OutputPath) && File.Exists(i.OutputPath));
            string? dir = firstSuccess != null ? Path.GetDirectoryName(firstSuccess.OutputPath) : null;

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                dir = !string.IsNullOrWhiteSpace(_settings.Current.CustomOutputFolder) && Directory.Exists(_settings.Current.CustomOutputFolder)
                    ? _settings.Current.CustomOutputFolder
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyTheme()
        {
            var p = AppTheme.Current;
            BackColor = p.Background;
            ForeColor = p.TextPrimary;

            _grid.BackgroundColor = p.CardBackground;
            _grid.DefaultCellStyle.BackColor = p.CardBackground;
            _grid.DefaultCellStyle.ForeColor = p.TextPrimary;
            _grid.DefaultCellStyle.SelectionBackColor = p.Accent;
            _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = p.SidebarBackground;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = p.TextPrimary;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            _grid.GridColor = p.Divider;

            _lblBatchStatus.ForeColor = p.TextPrimary;
            Invalidate(true);
        }
    }
}
