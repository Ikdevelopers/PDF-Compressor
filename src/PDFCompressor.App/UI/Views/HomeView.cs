using System;
using System.Drawing;
using System.Windows.Forms;
using PDFCompressor.App.UI.Controls;
using PDFCompressor.App.UI.Theme;
using PDFCompressor.Core.Models;
using PDFCompressor.Core.Services;

namespace PDFCompressor.App.UI.Views
{
    public class HomeView : UserControl
    {
        private readonly HistoryService _historyService;
        private readonly Action<string> _onNavigate;
        private readonly Action<string> _onFileDroppedForCompression;

        private Label _statTotalFilesValue = null!;
        private Label _statTotalSavedValue = null!;
        private Label _statAvgRatioValue = null!;
        private FlowLayoutPanel _recentListPanel = null!;
        private DropZoneControl _dropZone = null!;

        public HomeView(HistoryService historyService, Action<string> onNavigate, Action<string> onFileDroppedForCompression)
        {
            _historyService = historyService;
            _onNavigate = onNavigate;
            _onFileDroppedForCompression = onFileDroppedForCompression;

            InitializeComponent();
            ApplyTheme();
            AppTheme.ThemeChanged += ApplyTheme;
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            AutoScroll = true;
            Padding = new Padding(28, 20, 28, 20);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(0)
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Header banner
            var headerPanel = new Panel { Height = 64, Dock = DockStyle.Top };
            var title = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                Text = "Fast, secure, local PDF optimization without uploading your documents to the cloud.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(2, 34)
            };
            headerPanel.Controls.Add(title);
            headerPanel.Controls.Add(subtitle);
            rootLayout.Controls.Add(headerPanel, 0, 0);

            // Statistics Row (3 cards)
            var statsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 16, 0, 16)
            };
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

            // Stat 1: Total files
            var card1 = CreateStatCard("Total PDFs Compressed", "0", out _statTotalFilesValue, AppTheme.Current.Accent);
            // Stat 2: Total space saved
            var card2 = CreateStatCard("Total Storage Saved", "0 MB", out _statTotalSavedValue, AppTheme.Current.Success);
            // Stat 3: Avg reduction
            var card3 = CreateStatCard("Average Size Reduction", "0%", out _statAvgRatioValue, AppTheme.Current.Warning);

            statsRow.Controls.Add(card1, 0, 0);
            statsRow.Controls.Add(card2, 1, 0);
            statsRow.Controls.Add(card3, 2, 0);
            rootLayout.Controls.Add(statsRow, 0, 1);

            // Quick Drop Zone Card
            var dropCard = new CardPanel
            {
                Dock = DockStyle.Top,
                Height = 220,
                Margin = new Padding(0, 0, 0, 20),
                Padding = new Padding(20)
            };
            _dropZone = new DropZoneControl
            {
                Dock = DockStyle.Fill,
                AllowMultipleFiles = false
            };
            _dropZone.SetLabels("Drag and drop a PDF to start compressing immediately", "Or click below to browse your computer");
            _dropZone.FilesSelected += files =>
            {
                if (files.Length > 0)
                {
                    _onFileDroppedForCompression(files[0]);
                }
            };
            dropCard.Controls.Add(_dropZone);
            rootLayout.Controls.Add(dropCard, 0, 2);

            // Quick Actions & Recent Activity Layout
            var bottomSection = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 20)
            };
            bottomSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            bottomSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            // Recent activity card
            var recentCard = new CardPanel
            {
                Dock = DockStyle.Top,
                Height = 260,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(18)
            };
            var recentHeader = new Label
            {
                Text = "Recent Compression Activity",
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 32
            };
            _recentListPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            recentCard.Controls.Add(_recentListPanel);
            recentCard.Controls.Add(recentHeader);
            bottomSection.Controls.Add(recentCard, 0, 0);

            // Quick actions card
            var actionsCard = new CardPanel
            {
                Dock = DockStyle.Top,
                Height = 260,
                Margin = new Padding(12, 0, 0, 0),
                Padding = new Padding(18)
            };
            var actionsHeader = new Label
            {
                Text = "Quick Navigation",
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 32
            };

            var actionsContainer = new Panel { Dock = DockStyle.Fill };

            var btnSingle = new ModernButton
            {
                Text = "📄  Single PDF Compression",
                Style = ButtonStyle.Primary,
                Dock = DockStyle.Top,
                Height = 44,
                Margin = new Padding(0, 0, 0, 10)
            };
            btnSingle.Click += (s, e) => _onNavigate("Compress");

            var btnBatch = new ModernButton
            {
                Text = "📚  Batch PDF Compression",
                Style = ButtonStyle.Secondary,
                Dock = DockStyle.Top,
                Height = 44,
                Margin = new Padding(0, 10, 0, 10)
            };
            btnBatch.Click += (s, e) => _onNavigate("Batch");

            var btnHistory = new ModernButton
            {
                Text = "🕒  Compression History",
                Style = ButtonStyle.Secondary,
                Dock = DockStyle.Top,
                Height = 44,
                Margin = new Padding(0, 10, 0, 0)
            };
            btnHistory.Click += (s, e) => _onNavigate("History");

            actionsContainer.Controls.Add(btnHistory);
            actionsContainer.Controls.Add(btnBatch);
            actionsContainer.Controls.Add(btnSingle);

            actionsCard.Controls.Add(actionsContainer);
            actionsCard.Controls.Add(actionsHeader);
            bottomSection.Controls.Add(actionsCard, 1, 0);

            rootLayout.Controls.Add(bottomSection, 0, 3);
            Controls.Add(rootLayout);
        }

        private CardPanel CreateStatCard(string label, string initialValue, out Label valueLabel, Color accentColor)
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 14),
                Margin = new Padding(4)
            };

            var lblTitle = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = AppTheme.Current.TextSecondary,
                Dock = DockStyle.Top,
                Height = 22
            };

            var lblVal = new Label
            {
                Text = initialValue,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = accentColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            valueLabel = lblVal;
            card.Controls.Add(lblVal);
            card.Controls.Add(lblTitle);
            return card;
        }

        public void RefreshData()
        {
            try
            {
                var stats = _historyService.GetStatistics();
                _statTotalFilesValue.Text = stats.TotalFilesCompressed.ToString("N0");
                _statTotalSavedValue.Text = CompressionResult.FormatFileSize(stats.TotalSpaceSavedBytes);
                _statAvgRatioValue.Text = $"{stats.AverageReductionPercentage:0.#}%";

                // Refresh recent activities
                _recentListPanel.Controls.Clear();
                var recent = _historyService.GetRecent(5);
                if (recent.Count == 0)
                {
                    var emptyLabel = new Label
                    {
                        Text = "No recent compressions yet. Drop a PDF above to get started!",
                        ForeColor = AppTheme.Current.TextMuted,
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                        AutoSize = true,
                        Padding = new Padding(8, 24, 8, 8)
                    };
                    _recentListPanel.Controls.Add(emptyLabel);
                }
                else
                {
                    foreach (var item in recent)
                    {
                        var row = new Panel
                        {
                            Width = _recentListPanel.Width - 25,
                            Height = 36,
                            Margin = new Padding(0, 2, 0, 2),
                            BackColor = Color.Transparent
                        };

                        var nameLabel = new Label
                        {
                            Text = item.FileName,
                            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
                            ForeColor = AppTheme.Current.TextPrimary,
                            Location = new Point(4, 8),
                            Width = 240,
                            AutoEllipsis = true
                        };

                        var savedLabel = new Label
                        {
                            Text = $"-{item.ReductionPercentage:0.#}% ({CompressionResult.FormatFileSize(item.SpaceSavedBytes)} saved)",
                            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                            ForeColor = item.ReductionPercentage > 0 ? AppTheme.Current.Success : AppTheme.Current.TextMuted,
                            Location = new Point(250, 8),
                            Width = 180
                        };

                        var dateLabel = new Label
                        {
                            Text = item.Timestamp.ToLocalTime().ToString("g"),
                            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                            ForeColor = AppTheme.Current.TextMuted,
                            Location = new Point(440, 9),
                            Width = 140
                        };

                        row.Controls.Add(dateLabel);
                        row.Controls.Add(savedLabel);
                        row.Controls.Add(nameLabel);
                        _recentListPanel.Controls.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to refresh HomeView dashboard", ex);
            }
        }

        private void ApplyTheme()
        {
            var p = AppTheme.Current;
            BackColor = p.Background;
            Invalidate(true);
        }
    }
}
