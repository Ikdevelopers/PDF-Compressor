using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using PDFCompressor.App.UI.Theme;

namespace PDFCompressor.App.UI.Controls
{
    public enum ButtonStyle
    {
        Primary,
        Secondary,
        Success,
        Danger,
        Ghost
    }

    public class ModernButton : Button
    {
        public ButtonStyle Style { get; set; } = ButtonStyle.Primary;
        public int BorderRadius { get; set; } = 6;
        private bool _isHovered;
        private bool _isPressed;

        public ModernButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            Height = 36;
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var p = AppTheme.Current;
            Color parentColor = Parent != null && Parent.BackColor != Color.Transparent
                ? Parent.BackColor
                : p.Background;

            // Clear corners cleanly
            g.Clear(parentColor);

            var rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            if (rect.Width <= 0 || rect.Height <= 0) return;

            Color bgColor;
            Color textColor;
            Color borderColor = Color.Transparent;

            switch (Style)
            {
                case ButtonStyle.Primary:
                    bgColor = _isPressed ? ControlPaint.Dark(p.Accent, 0.15f) : (_isHovered ? p.AccentHover : p.Accent);
                    textColor = p.AccentText;
                    break;
                case ButtonStyle.Secondary:
                    bgColor = _isPressed ? p.CardBorder : (_isHovered ? p.CardHoverBackground : p.CardBackground);
                    textColor = p.TextPrimary;
                    borderColor = _isHovered ? p.Accent : p.CardBorder;
                    break;
                case ButtonStyle.Success:
                    bgColor = _isPressed ? ControlPaint.Dark(p.Success, 0.15f) : (_isHovered ? ControlPaint.Light(p.Success, 0.1f) : p.Success);
                    textColor = Color.White;
                    break;
                case ButtonStyle.Danger:
                    bgColor = _isPressed ? ControlPaint.Dark(p.Danger, 0.15f) : (_isHovered ? ControlPaint.Light(p.Danger, 0.1f) : p.Danger);
                    textColor = Color.White;
                    break;
                case ButtonStyle.Ghost:
                default:
                    bgColor = _isHovered ? p.CardHoverBackground : parentColor;
                    textColor = _isHovered ? p.TextPrimary : p.TextSecondary;
                    break;
            }

            if (!Enabled)
            {
                bgColor = Color.FromArgb(30, p.TextMuted);
                textColor = p.TextMuted;
                borderColor = Color.Transparent;
            }

            using var path = CreateRoundedRectanglePath(rect, BorderRadius);

            // Fill
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }

            // Border
            if (borderColor != Color.Transparent)
            {
                using var pen = new Pen(borderColor, 1);
                g.DrawPath(pen, path);
            }

            // Draw text
            var textRect = new Rectangle(rect.X + 8, rect.Y, Math.Max(0, rect.Width - 16), rect.Height);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;
            if (TextAlign == ContentAlignment.MiddleLeft)
            {
                flags |= TextFormatFlags.Left;
                textRect.X += 8;
            }
            else if (TextAlign == ContentAlignment.MiddleRight)
            {
                flags |= TextFormatFlags.Right;
            }
            else
            {
                flags |= TextFormatFlags.HorizontalCenter;
            }

            TextRenderer.DrawText(g, Text, Font, textRect, textColor, flags);
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
