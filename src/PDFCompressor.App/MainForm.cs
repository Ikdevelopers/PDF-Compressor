using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PDFCompressor.App.UI.Controls;
using PDFCompressor.App.UI.Theme;
using PDFCompressor.App.UI.Views;
using PDFCompressor.Core.Models;
using PDFCompressor.Core.Services;

namespace PDFCompressor.App
{
    public class MainForm : Form
    {
        private readonly EngineResolver _resolver;
        private readonly HistoryService _historyService;
        private readonly SettingsService _settingsService;
        private readonly PdfCompressionEngine _engine;

        // Navigation Views
        private HomeView _homeView = null!;
        private CompressView _compressView = null!;
        private BatchCompressView _batchView = null!;
        private HistoryView _historyView = null!;
        private SettingsView _settingsView = null!;
        private AboutView _aboutView = null!;

        // Layout Controls
        private Panel _sidebarPanel = null!;
        private Panel _contentPanel = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private ToolStripStatusLabel _engineStatusLabel = null!;
        private ToolStripStatusLabel _archLabel = null!;

        // Sidebar Navigation Buttons
        private ModernButton _btnNavHome = null!;
        private ModernButton _btnNavCompress = null!;
        private ModernButton _btnNavBatch = null!;
        private ModernButton _btnNavHistory = null!;
        private ModernButton _btnNavSettings = null!;
        private ModernButton _btnNavAbout = null!;

        public MainForm()
        {
            // Set AutoScaleMode to DPI for crisp scaling
            AutoScaleMode = AutoScaleMode.Dpi;

            // Initialize Core Services
            _resolver = new EngineResolver();
            _historyService = new HistoryService();
            _settingsService = new SettingsService();
            _engine = new PdfCompressionEngine(_resolver, _historyService);

            // Apply saved theme
            AppTheme.SetTheme(_settingsService.Current.Theme);

            InitializeComponent();
            InitializeViews();
            ApplyTheme();
            AppTheme.ThemeChanged += ApplyTheme;

            NavigateTo("Home");
        }

        private void InitializeComponent()
        {
            Text = "PDF Compressor";
            Size = new Size(1220, 800);
            MinimumSize = new Size(1000, 680);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Double Buffering
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch { }

            // Allow Drag & Drop on Form
            AllowDrop = true;
            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;

            // 1. Status Strip (Bottom)
            _statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                SizingGrip = false,
                Height = 28
            };

            _statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            bool gsAvailable = _engine.Ghostscript.IsAvailable;
            bool qpdfAvailable = _engine.Qpdf.IsAvailable;
            string enginesText = $"Ghostscript: {(gsAvailable ? "Ready" : "Missing")} • qpdf: {(qpdfAvailable ? "Ready" : "Missing")}";

            _engineStatusLabel = new ToolStripStatusLabel
            {
                Text = enginesText,
                TextAlign = ContentAlignment.MiddleRight
            };

            _archLabel = new ToolStripStatusLabel
            {
                Text = $"[{_resolver.ArchitectureName.ToUpperInvariant()}]",
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            _statusStrip.Items.Add(_statusLabel);
            _statusStrip.Items.Add(_engineStatusLabel);
            _statusStrip.Items.Add(_archLabel);

            // 2. Sidebar Panel (Left)
            _sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 230,
                Padding = new Padding(12)
            };

            // Branding Header
            var brandPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                Padding = new Padding(6, 10, 6, 6)
            };
            var lblLogo = new Label
            {
                Text = "⚡ PDF Compressor",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 30
            };
            var lblSub = new Label
            {
                Text = "Professional Edition",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = AppTheme.Current.TextMuted,
                Dock = DockStyle.Top,
                Height = 20
            };
            brandPanel.Controls.Add(lblSub);
            brandPanel.Controls.Add(lblLogo);
            _sidebarPanel.Controls.Add(brandPanel);

            // Navigation Container: Vertical FlowLayoutPanel to PREVENT ANY BUTTON OVERLAP
            var navContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(0, 10, 0, 0),
                Margin = new Padding(0)
            };

            _btnNavHome = CreateNavButton("Dashboard", () => NavigateTo("Home"));
            _btnNavCompress = CreateNavButton("Compress PDF", () => NavigateTo("Compress"));
            _btnNavBatch = CreateNavButton("Batch Compression", () => NavigateTo("Batch"));
            _btnNavHistory = CreateNavButton("History", () => NavigateTo("History"));
            _btnNavSettings = CreateNavButton("Settings", () => NavigateTo("Settings"));
            _btnNavAbout = CreateNavButton("About", () => NavigateTo("About"));

            navContainer.Controls.Add(_btnNavHome);
            navContainer.Controls.Add(_btnNavCompress);
            navContainer.Controls.Add(_btnNavBatch);
            navContainer.Controls.Add(_btnNavHistory);
            navContainer.Controls.Add(_btnNavSettings);
            navContainer.Controls.Add(_btnNavAbout);

            _sidebarPanel.Controls.Add(navContainer);

            // 3. Content Panel (Fill)
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };

            // STRICT Z-ORDER ADDITION TO PREVENT OVERLAPPING
            // In WinForms: Add in order, and send sidebar to back so content panel docks cleanly beside it
            Controls.Add(_contentPanel);
            Controls.Add(_sidebarPanel);
            Controls.Add(_statusStrip);

            _statusStrip.SendToBack();
            _sidebarPanel.SendToBack();
            _contentPanel.BringToFront();
        }

        private ModernButton CreateNavButton(string text, Action onClick)
        {
            var btn = new ModernButton
            {
                Text = text,
                Style = ButtonStyle.Ghost,
                Width = 206,
                Height = 42,
                BorderRadius = 8,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 3, 0, 3),
                Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold)
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void InitializeViews()
        {
            _homeView = new HomeView(_historyService, section => NavigateTo(section), file =>
            {
                NavigateTo("Compress");
                _compressView.LoadFile(file);
            });

            _compressView = new CompressView(_engine, _settingsService);
            _batchView = new BatchCompressView(_engine, _settingsService);
            _historyView = new HistoryView(_historyService);
            _settingsView = new SettingsView(_settingsService, _historyService);
            _aboutView = new AboutView(_resolver);
        }

        public void NavigateTo(string section)
        {
            _contentPanel.SuspendLayout();
            _contentPanel.Controls.Clear();

            // Highlight active button and un-highlight others
            _btnNavHome.Style = section == "Home" ? ButtonStyle.Primary : ButtonStyle.Ghost;
            _btnNavCompress.Style = section == "Compress" ? ButtonStyle.Primary : ButtonStyle.Ghost;
            _btnNavBatch.Style = section == "Batch" ? ButtonStyle.Primary : ButtonStyle.Ghost;
            _btnNavHistory.Style = section == "History" ? ButtonStyle.Primary : ButtonStyle.Ghost;
            _btnNavSettings.Style = section == "Settings" ? ButtonStyle.Primary : ButtonStyle.Ghost;
            _btnNavAbout.Style = section == "About" ? ButtonStyle.Primary : ButtonStyle.Ghost;

            _sidebarPanel.Invalidate(true);

            UserControl activeControl = section switch
            {
                "Compress" => _compressView,
                "Batch" => _batchView,
                "History" => _historyView,
                "Settings" => _settingsView,
                "About" => _aboutView,
                "Home" or _ => _homeView
            };

            if (section == "Home") _homeView.RefreshData();
            if (section == "History") _historyView.LoadHistory();

            activeControl.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(activeControl);
            _contentPanel.ResumeLayout(true);

            _statusLabel.Text = $"Viewing {section}";
        }

        private void MainForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && files[0].EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    if (files.Length == 1)
                    {
                        NavigateTo("Compress");
                        _compressView.LoadFile(files[0]);
                    }
                    else
                    {
                        NavigateTo("Batch");
                        _batchView.AddFiles(files);
                    }
                }
            }
        }

        private void ApplyTheme()
        {
            var p = AppTheme.Current;
            BackColor = p.Background;
            ForeColor = p.TextPrimary;

            _sidebarPanel.BackColor = p.SidebarBackground;
            _contentPanel.BackColor = p.Background;

            _statusStrip.BackColor = p.SidebarBackground;
            _statusStrip.ForeColor = p.TextSecondary;

            Invalidate(true);
        }
    }
}
