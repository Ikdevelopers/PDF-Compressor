using System;
using System.Drawing;
using System.Windows.Forms;
using PDFCompressor.App.UI.Controls;
using PDFCompressor.App.UI.Theme;
using PDFCompressor.Core.Services;

namespace PDFCompressor.App.UI.Views
{
    public class AboutView : UserControl
    {
        private readonly EngineResolver _resolver;

        public AboutView(EngineResolver resolver)
        {
            _resolver = resolver;

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

            // Main Branding Card
            var cardBrand = new CardPanel
            {
                Height = 160,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(22)
            };

            var lblApp = new Label
            {
                Text = "PDF Compressor",
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = AppTheme.Current.Accent,
                Location = new Point(20, 16),
                AutoSize = true
            };

            var lblVer = new Label
            {
                Text = $"Version 1.0.0  •  {(_resolver.Is64BitProcess ? "Windows 64-bit (x64)" : "Windows 32-bit (x86)")} Native Edition",
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Location = new Point(22, 60),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = "A high-performance Windows desktop application designed to optimize, compress, and restructure PDF files without sacrificing readability or uploading private documents to external servers.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Location = new Point(22, 90),
                Width = 790,
                Height = 44
            };

            cardBrand.Controls.Add(lblDesc);
            cardBrand.Controls.Add(lblVer);
            cardBrand.Controls.Add(lblApp);
            rootLayout.Controls.Add(cardBrand);

            // Privacy Guarantee Card
            var cardPrivacy = new CardPanel
            {
                Height = 110,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(20)
            };
            var lblPrivHeader = new Label { Text = "🔒 100% Local Processing & Privacy Guarantee", Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), ForeColor = AppTheme.Current.Success, Location = new Point(16, 12), AutoSize = true };
            var lblPrivText = new Label
            {
                Text = "Your files are never transmitted across the network or uploaded to third-party servers. All compression, rasterization, downsampling, and linearization algorithms run locally on your computer. Zero telemetry. Zero tracking.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Location = new Point(16, 42),
                Width = 800,
                Height = 48
            };
            cardPrivacy.Controls.Add(lblPrivText);
            cardPrivacy.Controls.Add(lblPrivHeader);
            rootLayout.Controls.Add(cardPrivacy);

            // Engine & Licenses Card
            var cardEngines = new CardPanel
            {
                Height = 190,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(20)
            };
            var lblEngHeader = new Label { Text = "PDF Engines & Open-Source Compliance", Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), Location = new Point(16, 12), AutoSize = true };

            var gsStatus = !string.IsNullOrEmpty(_resolver.ResolveGhostscript()) ? "✓ Active & Integrated" : "⚠ Not Found";
            var qpdfStatus = !string.IsNullOrEmpty(_resolver.ResolveQpdf()) ? "✓ Active & Integrated" : "⚠ Not Found";

            var lblGs = new Label
            {
                Text = $"• Ghostscript 10.08.0 ({gsStatus})\n   Rasterization, color image downsampling, JPEG encoding, font subsetting.\n   Licensed under GNU General Public License (GPLv3).",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(16, 44),
                Width = 800,
                Height = 50
            };

            var lblQpdf = new Label
            {
                Text = $"• QPDF 12.4.2 ({qpdfStatus})\n   Object stream optimization, fast web view linearization, document syntax verification.\n   Licensed under the Apache License 2.0.",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(16, 100),
                Width = 800,
                Height = 50
            };

            var lblSqlite = new Label
            {
                Text = "• SQLite 3 via Microsoft.Data.Sqlite — Local database storage (Public Domain / MIT).",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(16, 154),
                Width = 800,
                Height = 22
            };

            cardEngines.Controls.Add(lblSqlite);
            cardEngines.Controls.Add(lblQpdf);
            cardEngines.Controls.Add(lblGs);
            cardEngines.Controls.Add(lblEngHeader);
            rootLayout.Controls.Add(cardEngines);

            Controls.Add(rootLayout);
        }

        private void ApplyTheme()
        {
            var p = AppTheme.Current;
            BackColor = p.Background;
            ForeColor = p.TextPrimary;
            Invalidate(true);
        }
    }
}
