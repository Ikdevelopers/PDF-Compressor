using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PDFCompressor.App.UI.Theme;

namespace PDFCompressor.App.UI.Controls
{
    public class CardPanel : Panel
    {
        public int BorderRadius { get; set; } = 10;
        public Color? CustomBorderColor { get; set; }
        public Color? CustomBackgroundColor { get; set; }
        public bool ShowBorder { get; set; } = true;

        public CardPanel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            DoubleBuffered = true;
            Padding = new Padding(16);
            BackColor = AppTheme.Current.CardBackground;

            AppTheme.ThemeChanged += UpdateTheme;
        }

        private void UpdateTheme()
        {
            BackColor = CustomBackgroundColor ?? AppTheme.Current.CardBackground;
            foreach (Control child in Controls)
            {
                if (child is Label or RadioButton or CheckBox or Panel)
                {
                    child.BackColor = BackColor;
                }
            }
            Invalidate();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control != null && (e.Control is Label or RadioButton or CheckBox or Panel))
            {
                e.Control.BackColor = BackColor;
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

            // Clear outside corners to parent background
            g.Clear(parentBg);

            var rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            if (rect.Width <= 0 || rect.Height <= 0) return;

            using var path = CreateRoundedRectanglePath(rect, BorderRadius);
            var bgColor = CustomBackgroundColor ?? p.CardBackground;
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }

            if (ShowBorder)
            {
                var borderColor = CustomBorderColor ?? p.CardBorder;
                using var pen = new Pen(borderColor, 1);
                g.DrawPath(pen, path);
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
