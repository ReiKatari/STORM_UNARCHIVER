using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StormUnarchiver.Models;
using Windows.UI;

namespace StormUnarchiver.Services;

public enum ThemeType
{
    StormDark,
    StormNight,
    StormDay,
    StormMidnight,
    StormMatrix,
    StormCyberpunk,
    StormFantasy,
    StormWarhammer
}

public class ThemeInfo
{
    public ThemeType Type { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string AccentHex { get; set; } = "";
    public string BackgroundHex { get; set; } = "";
    public SolidColorBrush AccentBrush => new(ThemeManager.ColorFromHex(AccentHex));
    public SolidColorBrush BackgroundBrush => new(ThemeManager.ColorFromHex(BackgroundHex));
}

public class ThemeManager
{
    private static ThemeManager? _instance;
    public static ThemeManager Instance => _instance ??= new ThemeManager();

    public event EventHandler<ThemeType>? ThemeChanged;

    public ThemeType CurrentTheme { get; private set; } = ThemeType.StormDark;

    public static readonly ThemeInfo[] AllThemes =
    [
        new() { Type = ThemeType.StormDark, Name = "STORM DARK", Description = "Тёмный кибер (Циан)", AccentHex = "#00D2FF", BackgroundHex = "#1A1B2E" },
        new() { Type = ThemeType.StormNight, Name = "STORM NIGHT", Description = "Чёрный OLED (Неон)", AccentHex = "#00F0FF", BackgroundHex = "#0A0B10" },
        new() { Type = ThemeType.StormDay, Name = "STORM DAY", Description = "Светлая тема (Лазурь)", AccentHex = "#0284C7", BackgroundHex = "#F8FAFC" },
        new() { Type = ThemeType.StormMidnight, Name = "STORM MIDNIGHT", Description = "Аметистовая ночь", AccentHex = "#A855F7", BackgroundHex = "#15112B" },
        new() { Type = ThemeType.StormMatrix, Name = "STORM MATRIX", Description = "Матричный зелёный", AccentHex = "#00FF66", BackgroundHex = "#0D1F17" },
        new() { Type = ThemeType.StormCyberpunk, Name = "STORM CYBERPUNK", Description = "Неон Найт-Сити", AccentHex = "#FF007F", BackgroundHex = "#180D2B" },
        new() { Type = ThemeType.StormFantasy, Name = "STORM FANTASY", Description = "Королевское золото", AccentHex = "#F59E0B", BackgroundHex = "#1A162B" },
        new() { Type = ThemeType.StormWarhammer, Name = "STORM WARHAMMER 40K", Description = "Имперская готика", AccentHex = "#D4AF37", BackgroundHex = "#17181C" }
    ];

    private ThemeManager()
    {
    }

    public void ApplyTheme(ThemeType theme)
    {
        CurrentTheme = theme;

        var (accent, surface, surfaceLight, surfaceLighter, text, textDim, success, error, warning) = theme switch
        {
            ThemeType.StormDark => (
                "#00D2FF", "#1A1B2E", "#242640", "#2E3052", "#E2E8F0", "#94A3B8", "#4ADE80", "#F87171", "#FBBF24"
            ),
            ThemeType.StormNight => (
                "#00F0FF", "#0A0B10", "#12131A", "#1C1E29", "#F1F5F9", "#64748B", "#22C55E", "#EF4444", "#EAB308"
            ),
            ThemeType.StormDay => (
                "#0284C7", "#F8FAFC", "#FFFFFF", "#E2E8F0", "#0F172A", "#64748B", "#16A34A", "#DC2626", "#D97706"
            ),
            ThemeType.StormMidnight => (
                "#A855F7", "#15112B", "#1E183D", "#292152", "#F3E8FF", "#A855F7", "#4ADE80", "#F87171", "#FBBF24"
            ),
            ThemeType.StormMatrix => (
                "#00FF66", "#0D1F17", "#132B20", "#1A3B2C", "#DCFCE7", "#4ADE80", "#22C55E", "#EF4444", "#EAB308"
            ),
            ThemeType.StormCyberpunk => (
                "#FF007F", "#180D2B", "#23123D", "#311954", "#FDF2F8", "#F472B6", "#00FFCC", "#FF3366", "#FFE600"
            ),
            ThemeType.StormFantasy => (
                "#F59E0B", "#1A162B", "#262040", "#342C56", "#FEF3C7", "#FBBF24", "#34D399", "#F87171", "#F59E0B"
            ),
            ThemeType.StormWarhammer => (
                "#D4AF37", "#17181C", "#212329", "#2D3038", "#E5E7EB", "#9CA3AF", "#4ADE80", "#EF4444", "#D4AF37"
            ),
            _ => (
                "#00D2FF", "#1A1B2E", "#242640", "#2E3052", "#E2E8F0", "#94A3B8", "#4ADE80", "#F87171", "#FBBF24"
            )
        };

        SetResourceColor("StormAccent", accent);
        SetResourceColor("StormSurface", surface);
        SetResourceColor("StormSurfaceLight", surfaceLight);
        SetResourceColor("StormSurfaceLighter", surfaceLighter);
        SetResourceColor("StormText", text);
        SetResourceColor("StormTextDim", textDim);
        SetResourceColor("StormSuccess", success);
        SetResourceColor("StormError", error);
        SetResourceColor("StormWarning", warning);

        SetResourceBrush("StormAccentBrush", accent);
        SetResourceBrush("StormSurfaceBrush", surface);
        SetResourceBrush("StormSurfaceLightBrush", surfaceLight);
        SetResourceBrush("StormSurfaceLighterBrush", surfaceLighter);
        SetResourceBrush("StormTextBrush", text);
        SetResourceBrush("StormTextDimBrush", textDim);
        SetResourceBrush("StormSuccessBrush", success);
        SetResourceBrush("StormErrorBrush", error);
        SetResourceBrush("StormWarningBrush", warning);
        SetResourceBrush("StormDarkBgBrush", surface);

        // Chip / Filter brushes
        var accentCol = ColorFromHex(accent);
        SetResourceBrushColor("StormChipBgBrush", Color.FromArgb(0x20, accentCol.R, accentCol.G, accentCol.B));
        SetResourceBrushColor("StormChipBorderBrush", Color.FromArgb(0x35, accentCol.R, accentCol.G, accentCol.B));
        SetResourceBrushColor("StormChipHoverBrush", Color.FromArgb(0x30, accentCol.R, accentCol.G, accentCol.B));
        SetResourceBrushColor("StormChipPressedBrush", Color.FromArgb(0x15, accentCol.R, accentCol.G, accentCol.B));
        SetResourceBrushColor("StormChipCheckedHoverBrush", Color.FromArgb(0xE5, accentCol.R, accentCol.G, accentCol.B));
        SetResourceBrushColor("StormChipCheckedPressedBrush", Color.FromArgb(0xB0, accentCol.R, accentCol.G, accentCol.B));

        ThemeChanged?.Invoke(this, theme);
    }

    private static void SetResourceColor(string key, string hex)
    {
        if (Application.Current?.Resources == null) return;
        var col = ColorFromHex(hex);
        Application.Current.Resources[key] = col;
    }

    private static void SetResourceBrush(string key, string hex)
    {
        if (Application.Current?.Resources == null) return;
        var col = ColorFromHex(hex);
        if (Application.Current.Resources[key] is SolidColorBrush brush)
        {
            brush.Color = col;
        }
        else
        {
            Application.Current.Resources[key] = new SolidColorBrush(col);
        }
    }

    private static void SetResourceBrushColor(string key, Color color)
    {
        if (Application.Current?.Resources == null) return;
        if (Application.Current.Resources[key] is SolidColorBrush brush)
        {
            brush.Color = color;
        }
        else
        {
            Application.Current.Resources[key] = new SolidColorBrush(color);
        }
    }

    public static Color ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(255, r, g, b);
        }
        if (hex.Length == 8)
        {
            byte a = Convert.ToByte(hex[..2], 16);
            byte r = Convert.ToByte(hex.Substring(2, 2), 16);
            byte g = Convert.ToByte(hex.Substring(4, 2), 16);
            byte b = Convert.ToByte(hex.Substring(6, 2), 16);
            return Color.FromArgb(a, r, g, b);
        }
        return Colors.White;
    }
}
