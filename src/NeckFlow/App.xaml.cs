using System.Threading;
using System.Windows;
using NeckFlow.Services;
using NeckFlow.Models;

namespace NeckFlow;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private TrayService? _tray;
    private ReminderService? _reminders;
    internal SettingsService Settings { get; private set; } = null!;
    internal ActivityService Activity { get; private set; } = null!;
    internal ReminderService Reminders { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var renderArg = e.Args.FirstOrDefault(a => a.StartsWith("--render-animations=", StringComparison.OrdinalIgnoreCase));
        if (renderArg is not null)
        {
            var outDir = renderArg["--render-animations=".Length..];
            var strideArg = e.Args.FirstOrDefault(a => a.StartsWith("--render-stride=", StringComparison.OrdinalIgnoreCase));
            var stride = strideArg is not null && int.TryParse(strideArg["--render-stride=".Length..], out var n) ? n : 1;
            try { Rendering.AnimationRenderer.RenderAll(outDir, stride); }
            catch (Exception ex) { System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "render-error.txt"), ex.ToString()); Shutdown(1); return; }
            Shutdown(); return;
        }
        _mutex = new Mutex(true, "NeckFlow.SingleInstance", out var created);
        if (!created) { Shutdown(); return; }
        Settings = new SettingsService();
        Activity = new ActivityService();
        Settings.Load();
        ThemeService.Apply(Settings.Current.Theme);
        LogService.Write("NeckFlow started");
        _tray = new TrayService(this);
        _reminders = new ReminderService(this, Settings, _tray);
        Reminders = _reminders;
        _reminders.Start();
        var kindArg = e.Args.FirstOrDefault(a => a.StartsWith("--exercise-kind=", StringComparison.OrdinalIgnoreCase));
        ExerciseKind? forced = null;
        if (kindArg is not null && Enum.TryParse<ExerciseKind>(kindArg[16..], true, out var parsed)) forced = parsed;
        if (e.Args.Any(a => string.Equals(a, "--exercise", StringComparison.OrdinalIgnoreCase)) || forced is not null)
            Dispatcher.BeginInvoke(new Action(() => ShowExercise(true, forced)));
    }

    internal void ShowExercise(bool manual = false, ExerciseKind? forced = null) => _tray?.ShowExercise(manual, forced);

    protected override void OnExit(ExitEventArgs e)
    {
        _reminders?.Dispose();
        _tray?.Dispose();
        Settings?.Save();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
