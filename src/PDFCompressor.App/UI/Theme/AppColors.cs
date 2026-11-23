using System.Drawing;
using PDFCompressor.Core.Models;

namespace PDFCompressor.App.UI.Theme
{
    public class ColorPalette
    {
        public Color Background { get; set; }
        public Color SidebarBackground { get; set; }
        public Color CardBackground { get; set; }
        public Color CardHoverBackground { get; set; }
        public Color CardBorder { get; set; }
        public Color Accent { get; set; }
        public Color AccentHover { get; set; }
        public Color AccentText { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color TextMuted { get; set; }
        public Color Success { get; set; }
        public Color Warning { get; set; }
        public Color Danger { get; set; }
        public Color Divider { get; set; }
        public Color InputBackground { get; set; }
        public Color InputBorder { get; set; }
    }

    public static class AppTheme
    {
        // Fluent Windows 11 Dark Palette
        public static ColorPalette DarkPalette { get; } = new ColorPalette
        {
            Background = Color.FromArgb(20, 20, 23),            // Deep slate background #141417
            SidebarBackground = Color.FromArgb(16, 16, 18),     // Subtle sidebar contrast #101012
            CardBackground = Color.FromArgb(30, 30, 34),        // Surface card #1E1E22
            CardHoverBackground = Color.FromArgb(38, 38, 44),
            CardBorder = Color.FromArgb(48, 48, 54),           // Subtle card border
            Accent = Color.FromArgb(0, 120, 215),               // Windows 11 Fluent Blue #0078D7
            AccentHover = Color.FromArgb(26, 137, 230),
            AccentText = Color.White,
            TextPrimary = Color.FromArgb(242, 242, 245),       // High contrast clean white
            TextSecondary = Color.FromArgb(170, 170, 180),     // Soft neutral text
            TextMuted = Color.FromArgb(120, 120, 130),
            Success = Color.FromArgb(16, 185, 129),             // Emerald #10B981
            Warning = Color.FromArgb(245, 158, 11),             // Amber #F59E0B
            Danger = Color.FromArgb(239, 68, 68),               // Rose #EF4444
            Divider = Color.FromArgb(42, 42, 48),
            InputBackground = Color.FromArgb(26, 26, 30),
            InputBorder = Color.FromArgb(60, 60, 68)
        };

        // Fluent Windows 11 Light Palette
        public static ColorPalette LightPalette { get; } = new ColorPalette
        {
            Background = Color.FromArgb(245, 246, 248),
            SidebarBackground = Color.FromArgb(238, 240, 244),
            CardBackground = Color.FromArgb(255, 255, 255),
            CardHoverBackground = Color.FromArgb(248, 250, 252),
            CardBorder = Color.FromArgb(220, 224, 230),
            Accent = Color.FromArgb(0, 103, 192),
            AccentHover = Color.FromArgb(0, 85, 165),
            AccentText = Color.White,
            TextPrimary = Color.FromArgb(25, 28, 33),
            TextSecondary = Color.FromArgb(90, 98, 110),
            TextMuted = Color.FromArgb(140, 148, 160),
            Success = Color.FromArgb(13, 148, 94),
            Warning = Color.FromArgb(217, 119, 6),
            Danger = Color.FromArgb(220, 38, 38),
            Divider = Color.FromArgb(228, 232, 238),
            InputBackground = Color.FromArgb(255, 255, 255),
            InputBorder = Color.FromArgb(205, 212, 222)
        };

        public static ColorPalette Current { get; private set; } = DarkPalette;
        public static AppThemeMode CurrentMode { get; private set; } = AppThemeMode.Dark;

        public static event System.Action? ThemeChanged;

        public static void SetTheme(AppThemeMode mode)
        {
            CurrentMode = mode;
            Current = mode == AppThemeMode.Light ? LightPalette : DarkPalette;
            ThemeChanged?.Invoke();
        }
    }
}
