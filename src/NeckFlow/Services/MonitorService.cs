using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Forms;
namespace NeckFlow.Services;
public static class MonitorService
{
    public static Rect BottomRightWorkArea(Window owner)
    {
        var screen = ForegroundScreen();
        var dpi = VisualTreeHelper.GetDpi(owner);
        var scaleX = dpi.DpiScaleX <= 0 ? 1 : dpi.DpiScaleX;
        var scaleY = dpi.DpiScaleY <= 0 ? 1 : dpi.DpiScaleY;
        return new Rect(screen.WorkingArea.Left / scaleX, screen.WorkingArea.Top / scaleY,
            screen.WorkingArea.Width / scaleX, screen.WorkingArea.Height / scaleY);
    }

    public static bool IsFullscreenForeground()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return false;
        var screen = Screen.FromHandle(hwnd);
        if (!GetWindowRect(hwnd, out var r)) return false;
        return r.Left <= screen.Bounds.Left && r.Top <= screen.Bounds.Top &&
               r.Right >= screen.Bounds.Right && r.Bottom >= screen.Bounds.Bottom;
    }

    public static bool IsWorkstationLocked()
    {
        var desktop = OpenInputDesktop(0, false, 0x0100);
        if (desktop == IntPtr.Zero) return true;
        CloseDesktop(desktop);
        return false;
    }

    private static Screen ForegroundScreen()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd != IntPtr.Zero) return Screen.FromHandle(hwnd);
        return Screen.FromPoint(Cursor.Position);
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll")] private static extern bool CloseDesktop(IntPtr hDesktop);
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
}
