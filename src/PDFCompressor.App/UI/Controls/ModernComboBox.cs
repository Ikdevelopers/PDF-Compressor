using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PDFCompressor.App.UI.Theme;

namespace PDFCompressor.App.UI.Controls
{
    public class ModernComboBox : ComboBox
    {
        public ModernComboBox()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 24;
            Font = new Font("Segoe UI", 9.5f);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            ApplyThemeColors();
            AppTheme.ThemeChanged += ApplyThemeColors;
        }

        private void ApplyThemeColors()
        {
            var p = AppTheme.Current;
            BackColor = p.InputBackground;
            ForeColor = p.TextPrimary;
            Invalidate();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;

            var g = e.Graphics;
            var p = AppTheme.Current;
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color bgColor = isSelected ? p.Accent : p.InputBackground;
            Color textColor = isSelected ? Color.White : p.TextPrimary;

            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, e.Bounds);
            }

            string text = Items[e.Index]?.ToString() ?? string.Empty;
            var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height);
            TextRenderer.DrawText(g, text, Font, textRect, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
}
