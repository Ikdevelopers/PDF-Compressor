using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PDFCompressor.App.UI.Controls;
using PDFCompressor.App.UI.Theme;
using PDFCompressor.Core.Models;
using PDFCompressor.Core.Services;

namespace PDFCompressor.App.UI.Views
{
    public class HistoryView : UserControl
    {
        private readonly HistoryService _historyService;
        private List<HistoryRecord> _records = new();

        private TextBox _txtSearch = null!;
        private ModernComboBox _cbSort = null!;
        private DataGridView _grid = null!;
        private Label _lblStatus = null!;
        private ModernButton _btnOpenFile = null!;
        private ModernButton _btnOpenFolder = null!;
        private ModernButton _btnDelete = null!;
        private ModernButton _btnClearAll = null!;

        public HistoryView(HistoryService historyService)
        {
            _historyService = historyService;

            InitializeComponent();
            ApplyTheme();
            AppTheme.ThemeChanged += ApplyTheme;
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(24, 16, 24, 20);

            var mainPanel = new Panel { Dock = DockStyle.Fill };

            // Top Header & Filters Panel
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
                Text = "Compression History",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            topPanel.Controls.Add(lblTitle);

            // Filter Toolbar
            var filterBar = new FlowLayoutPanel
            {
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10),
                Width = 900
            };

            var lblSearch = new Label { Text = "Search:", Margin = new Padding(0, 8, 6, 0), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f) };
            _txtSearch = new TextBox
            {
                Width = 220,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 4, 16, 0),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.TextChanged += (s, e) => LoadHistory();

            var lblSort = new Label { Text = "Sort By:", Margin = new Padding(0, 8, 6, 0), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f) };
            _cbSort = new ModernComboBox
            {
                Width = 180,
                Height = 32,
                Margin = new Padding(0, 2, 10, 0)
            };
            _cbSort.Items.AddRange(new object[] { "Date (Newest First)", "Date (Oldest First)", "Space Saved", "Reduction %", "File Name" });
            _cbSort.SelectedIndex = 0;
            _cbSort.SelectedIndexChanged += (s, e) => LoadHistory();

            var btnRefresh = new ModernButton { Text = "Refresh", Style = ButtonStyle.Secondary, Width = 80, Height = 32, Margin = new Padding(0, 0, 12, 0) };
            btnRefresh.Click += (s, e) => LoadHistory();

            _btnClearAll = new ModernButton { Text = "Clear All History", Style = ButtonStyle.Danger, Width = 130, Height = 32 };
            _btnClearAll.Click += (s, e) => ClearAllHistory();

            filterBar.Controls.Add(lblSearch);
            filterBar.Controls.Add(_txtSearch);
            filterBar.Controls.Add(lblSort);
            filterBar.Controls.Add(_cbSort);
            filterBar.Controls.Add(btnRefresh);
            filterBar.Controls.Add(_btnClearAll);
            topPanel.Controls.Add(filterBar);

            mainPanel.Controls.Add(topPanel);

            // Center DataGridView
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
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

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date & Time", DataPropertyName = "DateStr", Width = 145 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "File Name", DataPropertyName = "FileName", Width = 230 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Original", DataPropertyName = "OriginalSizeStr", Width = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Compressed", DataPropertyName = "CompressedSizeStr", Width = 105 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Saved", DataPropertyName = "SavedStr", Width = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Reduction", DataPropertyName = "ReductionStr", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Preset", DataPropertyName = "PresetName", Width = 110 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", Width = 95 });

            _grid.SelectionChanged += (s, e) => UpdateButtonStates();

            var gridContainer = new CardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                Margin = new Padding(0, 8, 0, 8)
            };
            gridContainer.Controls.Add(_grid);

            // Bottom Action Bar
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 56 };

            _lblStatus = new Label
            {
                Text = "0 records found",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Location = new Point(0, 16),
                AutoSize = true
            };

            var bottomButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Height = 44,
                Width = 420,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 6, 0, 0)
            };

            _btnDelete = new ModernButton { Text = "Delete Record", Style = ButtonStyle.Secondary, Width = 120, Height = 36, Margin = new Padding(0, 0, 0, 0), Enabled = false };
            _btnDelete.Click += (s, e) => DeleteSelectedRecord();

            _btnOpenFolder = new ModernButton { Text = "Open Folder", Style = ButtonStyle.Secondary, Width = 120, Height = 36, Margin = new Padding(0, 0, 8, 0), Enabled = false };
            _btnOpenFolder.Click += (s, e) => OpenSelectedFolder();

            _btnOpenFile = new ModernButton { Text = "Open PDF", Style = ButtonStyle.Primary, Width = 110, Height = 36, Margin = new Padding(0, 0, 8, 0), Enabled = false };
            _btnOpenFile.Click += (s, e) => OpenSelectedFile();

            bottomButtons.Controls.Add(_btnDelete);
            bottomButtons.Controls.Add(_btnOpenFolder);
            bottomButtons.Controls.Add(_btnOpenFile);

            bottomPanel.Controls.Add(bottomButtons);
            bottomPanel.Controls.Add(_lblStatus);

            mainPanel.Controls.Add(gridContainer);
            mainPanel.Controls.Add(bottomPanel);

            Controls.Add(mainPanel);
        }

        public void LoadHistory()
        {
            string query = _txtSearch.Text;
            string sort = _cbSort.SelectedIndex switch
            {
                1 => "DateAsc",
                2 => "SizeSavedDesc",
                3 => "RatioDesc",
                4 => "FileNameAsc",
                _ => "DateDesc"
            };

            _records = _historyService.GetAll(query, sort);

            var list = _records.Select(r => new
            {
                r.Id,
                DateStr = r.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                r.FileName,
                OriginalSizeStr = CompressionResult.FormatFileSize(r.OriginalSizeBytes),
                CompressedSizeStr = r.CompressedSizeBytes > 0 ? CompressionResult.FormatFileSize(r.CompressedSizeBytes) : "-",
                SavedStr = r.SpaceSavedBytes > 0 ? CompressionResult.FormatFileSize(r.SpaceSavedBytes) : "-",
                ReductionStr = r.ReductionPercentage > 0 ? $"{r.ReductionPercentage:0.#}%" : "-",
                r.PresetName,
                r.Status,
                r.OutputPath
            }).ToList();

            _grid.DataSource = list;
            _lblStatus.Text = $"{_records.Count} records found";
            UpdateButtonStates();
        }

        private HistoryRecord? GetSelectedRecord()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _records.Count)
                return null;

            return _records[_grid.CurrentRow.Index];
        }

        private void UpdateButtonStates()
        {
            var selected = GetSelectedRecord();
            bool hasSelection = selected != null;
            _btnOpenFile.Enabled = hasSelection;
            _btnOpenFolder.Enabled = hasSelection;
            _btnDelete.Enabled = hasSelection;
        }

        private void OpenSelectedFile()
        {
            var record = GetSelectedRecord();
            if (record == null) return;

            if (File.Exists(record.OutputPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = record.OutputPath, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show($"The output file no longer exists at:\n{record.OutputPath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenSelectedFolder()
        {
            var record = GetSelectedRecord();
            if (record == null) return;

            if (File.Exists(record.OutputPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{record.OutputPath}\"", UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                string? dir = Path.GetDirectoryName(record.OutputPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Output directory no longer exists.", "Directory Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void DeleteSelectedRecord()
        {
            var record = GetSelectedRecord();
            if (record == null) return;

            if (MessageBox.Show($"Remove record for '{record.FileName}' from history?", "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _historyService.DeleteRecord(record.Id);
                LoadHistory();
            }
        }

        private void ClearAllHistory()
        {
            if (MessageBox.Show("Are you sure you want to clear all compression history?", "Clear All History", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _historyService.ClearAll();
                LoadHistory();
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

            _txtSearch.BackColor = p.InputBackground;
            _txtSearch.ForeColor = p.TextPrimary;

            _lblStatus.ForeColor = p.TextMuted;
            Invalidate(true);
        }
    }
}
