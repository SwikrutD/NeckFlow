using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace NeckFlow.Services;

public static class ThemeService
{
    public static bool IsDark(string? theme)
    {
        if (string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase)) return false;
        try { return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int v && v == 0; }
        catch { return false; }
    }

    public static event Action? Changed;

    public static void Apply(string? theme)
    {
        try { ApplyResources(theme); } finally { Changed?.Invoke(); }
    }

    private static void ApplyResources(string? theme)
    {
        var dark = IsDark(theme); var r = System.Windows.Application.Current.Resources;
        void Set(string key, string hex) => r[key] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        Set("WindowBg", dark ? "#1B212C" : "#F7F8FC"); Set("TextBrush", dark ? "#E6EBF5" : "#24324A"); Set("MutedBrush", dark ? "#9AA7BC" : "#65738A");
        Set("HoverBg", dark ? "#2A3345" : "#E8EDF6"); Set("LineBrush", dark ? "#3A4558" : "#E1E6F0"); Set("InputBg", dark ? "#252D3B" : "#FFFFFF");
    }
}
