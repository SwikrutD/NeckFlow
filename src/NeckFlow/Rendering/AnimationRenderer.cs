using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using NeckFlow.Models;
using MediaColor = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace NeckFlow.Rendering;

/// <summary>
/// Offscreen renderer behind <c>--render-animations=&lt;dir&gt;</c>. Writes one seamless loop of every exercise as
/// supersampled PNG frames (<c>&lt;dir&gt;/&lt;Exercise&gt;/f0000.png</c>) plus tray icon previews, so the motion can be
/// reviewed or encoded without running the tray app. See <c>animations/render.ps1</c>.
/// </summary>
internal static class AnimationRenderer
{
    private const int Width = 720, Height = 420, Supersample = 2, Fps = 30;

    /// <param name="stride">Render every Nth frame only; useful for a quick look at key poses.</param>
    public static void RenderAll(string outDir, int stride = 1)
    {
        Directory.CreateDirectory(outDir);
        foreach (var exercise in Exercises.All)
        {
            var dir = Path.Combine(outDir, exercise.Kind.ToString()); Directory.CreateDirectory(dir);
            foreach (var old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
            var scene = new MannequinScene(exercise.Kind);
            var frame = BuildFrame(scene, exercise);
            var (start, length) = MannequinScene.PreviewWindow(exercise.Kind);
            var count = (int)Math.Round(length * Fps);
            for (var i = 0; i < count; i += Math.Max(1, stride))
            {
                scene.Update(start + i / (double)Fps);
                frame.UpdateLayout();
                var bitmap = new RenderTargetBitmap(Width * Supersample, Height * Supersample, 96 * Supersample, 96 * Supersample, PixelFormats.Pbgra32);
                bitmap.Render(frame);
                Save(bitmap, Path.Combine(dir, $"f{i:0000}.png"));
            }
            Console.WriteLine($"{exercise.Kind}: {count} frames");
        }
        foreach (var size in new[] { 16, 24, 32, 64, 256 })
        {
            using var icon = TrayIconArt.Render(size);
            icon.Save(Path.Combine(outDir, $"tray-icon-{size}.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
    }

    private static Grid BuildFrame(MannequinScene scene, ExerciseDefinition exercise)
    {
        var viewport = new Viewport3D { Camera = scene.Camera, ClipToBounds = true };
        viewport.Children.Add(new ModelVisual3D { Content = scene.Lights });
        viewport.Children.Add(new ModelVisual3D { Content = scene.Root });
        var frame = new Grid { Width = Width, Height = Height, Background = Stage(false) };
        frame.Children.Add(viewport);
        var caption = new StackPanel { Margin = new Thickness(22, 18, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        caption.Children.Add(new TextBlock { Text = exercise.Name, FontSize = 22, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(MediaColor.FromRgb(23, 36, 58)) });
        caption.Children.Add(new TextBlock { Text = "Targets: " + exercise.Targets, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(MediaColor.FromRgb(194, 52, 36)), Margin = new Thickness(0, 2, 0, 0) });
        frame.Children.Add(caption);
        frame.Measure(new Size(Width, Height)); frame.Arrange(new Rect(0, 0, Width, Height));
        return frame;
    }

    /// <summary>Soft spotlight backdrop shared with the exercise popup.</summary>
    public static System.Windows.Media.Brush Stage(bool dark)
    {
        var brush = new RadialGradientBrush(dark ? MediaColor.FromRgb(52, 64, 86) : MediaColor.FromRgb(250, 252, 255), dark ? MediaColor.FromRgb(27, 33, 45) : MediaColor.FromRgb(214, 224, 239))
        { Center = new Point(.5, .42), GradientOrigin = new Point(.5, .38), RadiusX = .75, RadiusY = .9 };
        brush.Freeze(); return brush;
    }

    private static void Save(BitmapSource bitmap, string path)
    {
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
