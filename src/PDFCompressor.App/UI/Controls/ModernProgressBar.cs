using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PDFCompressor.App.UI.Theme;

namespace PDFCompressor.App.UI.Controls
{
    public class ModernProgressBar : Control
    {
        private int _value = 0;
        private int _maximum = 100;
        public bool ShowPercentageText { get; set; } = true;
        public Color? CustomAccentColor { get; set; }

        public ModernProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 12;
            Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        }

        public int Value
        {
            get => _value;
            set
            {
                int val = Math.Max(0, Math.Min(_maximum, value));
                if (_value != val)
                {
                    _value = val;
                    Invalidate();
                }
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                if (value > 0 && _maximum != value)
                {
                    _maximum = value;
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            int radius = Math.Min(rect.Height / 2, 6);

            // Draw track background
            using var trackPath = CreateRoundedRectanglePath(rect, radius);
            using (var trackBrush = new SolidBrush(AppTheme.Current.CardBorder))
            {
                e.Graphics.FillPath(trackBrush, trackPath);
            }

            // Draw progress fill
            if (_value > 0)
            {
                float progressFraction = (float)_value / _maximum;
                int fillWidth = Math.Max(radius * 2, (int)(rect.Width * progressFraction));
                fillWidth = Math.Min(rect.Width, fillWidth);

                var fillRect = new Rectangle(rect.X, rect.Y, fillWidth, rect.Height);
                using var fillPath = CreateRoundedRectanglePath(fillRect, radius);
                var barColor = CustomAccentColor ?? AppTheme.Current.Accent;
                using (var fillBrush = new SolidBrush(barColor))
                {
                    e.Graphics.FillPath(fillBrush, fillPath);
                }
            }

            if (ShowPercentageText && rect.Height >= 18)
            {
                string text = $"{_value}%";
                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    Font,
                    rect,
                    AppTheme.Current.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            if (radius <= 0 || rect.Width < diameter || rect.Height < diameter)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
