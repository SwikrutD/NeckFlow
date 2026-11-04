using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NeckFlow.Rendering;

/// <summary>
/// The NeckFlow mark: a bust with a gently tilted head, the neck picked out in the same warm "worked muscle" colour the
/// exercise popup uses, and a motion arc around the head at sizes large enough to read it. Drawn on a 64-unit grid.
/// </summary>
internal static class TrayIconArt
{
    public static Bitmap Render(int size)
    {
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.Clear(Color.Transparent);
        g.ScaleTransform(size / 64f, size / 64f);
        var small = size < 24;

        using var tile = RoundedRect(new RectangleF(1, 1, 62, 62), small ? 12 : 15);
        using (var fill = new LinearGradientBrush(new PointF(0, 0), new PointF(64, 64), Color.FromArgb(28, 98, 204), Color.FromArgb(0, 168, 196))) g.FillPath(fill, tile);
        g.SetClip(tile);

        using var white = new SolidBrush(Color.FromArgb(250, 252, 255));
        using (var shoulders = new GraphicsPath())
        {
            shoulders.AddBezier(6, 66, 6, 52, 14, 46, 25, 44);
            shoulders.AddLine(25, 44, 39, 44);
            shoulders.AddBezier(39, 44, 50, 46, 58, 52, 58, 66);
            shoulders.CloseFigure(); g.FillPath(white, shoulders);
        }

        // Head and neck tilt together about the base of the neck.
        var state = g.Save();
        g.TranslateTransform(32, 45); g.RotateTransform(-12); g.TranslateTransform(-32, -45);
        using (var neck = new SolidBrush(Color.FromArgb(255, 92, 66)))
        using (var neckPath = RoundedRect(new RectangleF(small ? 25.5f : 26, 27, small ? 13 : 12, 20), 3)) g.FillPath(neck, neckPath);
        g.FillEllipse(white, 21, small ? 8 : 10, 22, 24);
        g.Restore(state);

        if (!small)
        {
            // Motion arc around the head, ending in an arrowhead that points along the direction of travel.
            const float cx = 32, cy = 28, radius = 20, start = 192, sweep = 92;
            using var arc = new Pen(Color.FromArgb(225, 255, 255, 255), 3.2f) { StartCap = LineCap.Round, EndCap = LineCap.Flat };
            g.DrawArc(arc, cx - radius, cy - radius, radius * 2, radius * 2, start, sweep - 6);
            var end = (start + sweep) * Math.PI / 180; var tip = new PointF(cx + radius * (float)Math.Cos(end), cy + radius * (float)Math.Sin(end));
            var along = new PointF(-(float)Math.Sin(end), (float)Math.Cos(end)); var across = new PointF(-along.Y, along.X);
            using var arrowHead = new SolidBrush(Color.FromArgb(225, 255, 255, 255));
            g.FillPolygon(arrowHead, new[] { tip, new PointF(tip.X - along.X * 7 + across.X * 4.5f, tip.Y - along.Y * 7 + across.Y * 4.5f), new PointF(tip.X - along.X * 7 - across.X * 4.5f, tip.Y - along.Y * 7 - across.Y * 4.5f) });
        }
        g.ResetClip();
        return bitmap;
    }

    public static Icon CreateIcon(int size)
    {
        var resourceName = "NeckFlow.tray-icon-32.png";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            using var resourceBitmap = new Bitmap(stream);
            var resourceHandle = resourceBitmap.GetHicon();
            try { using var source = Icon.FromHandle(resourceHandle); return (Icon)source.Clone(); }
            finally { DestroyIcon(resourceHandle); }
        }

        using var bitmap = Render(size);
        var handle = bitmap.GetHicon();
        try { using var source = Icon.FromHandle(handle); return (Icon)source.Clone(); }
        finally { DestroyIcon(handle); }
    }

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath(); var d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }

    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
}
