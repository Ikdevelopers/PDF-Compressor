using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PDFCompressor.App.UI.Theme;

namespace PDFCompressor.App.UI.Controls
{
    public class DropZoneControl : UserControl
    {
        public event Action<string[]>? FilesSelected;
        public bool AllowMultipleFiles { get; set; } = false;

        private bool _isDragOver;
        private readonly ModernButton _browseButton;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;

        public DropZoneControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            DoubleBuffered = true;
            AllowDrop = true;
            Height = 160;
            Padding = new Padding(16);

            _titleLabel = new Label
            {
                Text = "Drag and drop your PDF files here",
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30
            };

            _subtitleLabel = new Label
            {
                Text = "Supports standard PDF documents • 100% processed locally on your PC",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 24
            };

            _browseButton = new ModernButton
            {
                Text = "Browse Files",
                Style = ButtonStyle.Primary,
                Width = 140,
                Height = 36,
                Cursor = Cursors.Hand
            };
            _browseButton.Click += (s, e) => OpenFileDialog();

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44
            };
            buttonPanel.Resize += (s, e) =>
            {
                _browseButton.Location = new Point((buttonPanel.Width - _browseButton.Width) / 2, (buttonPanel.Height - _browseButton.Height) / 2);
            };
            buttonPanel.Controls.Add(_browseButton);

            Controls.Add(buttonPanel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_titleLabel);

            ApplyThemeColors();
            AppTheme.ThemeChanged += ApplyThemeColors;
        }

        public void SetLabels(string title, string subtitle)
        {
            _titleLabel.Text = title;
            _subtitleLabel.Text = subtitle;
        }

        private void ApplyThemeColors()
        {
            var p = AppTheme.Current;
            BackColor = p.CardBackground;
            _titleLabel.ForeColor = p.TextPrimary;
            _subtitleLabel.ForeColor = p.TextSecondary;
            Invalidate();
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);
            if (drgevent.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[]?)drgevent.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Any(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
                {
                    drgevent.Effect = DragDropEffects.Copy;
                    _isDragOver = true;
                    Invalidate();
                    return;
                }
            }
            drgevent.Effect = DragDropEffects.None;
        }

        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            _isDragOver = false;
            Invalidate();
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);
            _isDragOver = false;
            Invalidate();

            if (drgevent.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[]?)drgevent.Data.GetData(DataFormats.FileDrop);
                if (files != null)
                {
                    var validPdfs = files.Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (validPdfs.Length > 0)
                    {
                        FilesSelected?.Invoke(AllowMultipleFiles ? validPdfs : new[] { validPdfs[0] });
                    }
                    else
                    {
                        MessageBox.Show("Please select valid PDF documents (.pdf).", "Unsupported File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void OpenFileDialog()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "PDF Documents (*.pdf)|*.pdf|All Files (*.*)|*.*",
                Multiselect = AllowMultipleFiles,
                Title = AllowMultipleFiles ? "Select PDF Files for Batch Compression" : "Select PDF Document"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                FilesSelected?.Invoke(ofd.FileNames);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var p = AppTheme.Current;
            Color parentBg = Parent != null && Parent.BackColor != Color.Transparent
                ? Parent.BackColor
                : p.Background;

            g.Clear(parentBg);

            var rect = ClientRectangle;
            rect.Width -= 2;
            rect.Height -= 2;
            rect.X += 1;
            rect.Y += 1;

            if (rect.Width <= 0 || rect.Height <= 0) return;

            var fillBg = _isDragOver ? Color.FromArgb(40, p.Accent) : p.CardBackground;

            using (var brush = new SolidBrush(fillBg))
            {
                g.FillRectangle(brush, rect);
            }

            var borderColor = _isDragOver ? p.Accent : p.CardBorder;
            using var pen = new Pen(borderColor, 2)
            {
                DashStyle = DashStyle.Dash,
                DashPattern = new float[] { 4, 3 }
            };
            g.DrawRectangle(pen, rect);
        }
    }
}
