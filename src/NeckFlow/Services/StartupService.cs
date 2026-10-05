using Microsoft.Win32;
namespace NeckFlow.Services;
public static class StartupService
{
    private const string Key = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string Name = "NeckFlow";
    public static void SetEnabled(bool enabled)
    {
        try { using var k = Registry.CurrentUser.OpenSubKey(Key, true); if (enabled) k?.SetValue(Name, '"' + Environment.ProcessPath + '"'); else k?.DeleteValue(Name, false); } catch { }
    }
    public static bool IsEnabled() { try { using var k=Registry.CurrentUser.OpenSubKey(Key); return k?.GetValue(Name) is not null; } catch { return false; } }
}
