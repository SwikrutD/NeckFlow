using System.Diagnostics;
using System.Windows.Threading;
using NeckFlow.Models;
namespace NeckFlow.Services;
public sealed class ReminderService : IDisposable
{
    private readonly App _app; private readonly SettingsService _settings; private readonly TrayService _tray; private readonly DispatcherTimer _timer;
    private DateTime _nextUtc;
    private DateTime? _snoozedUntilUtc;
    private DateTime _lastTickUtc;
    public ReminderService(App app, SettingsService settings, TrayService tray) { _app=app; _settings=settings; _tray=tray; _timer=new DispatcherTimer(DispatcherPriority.Background) { Interval=TimeSpan.FromSeconds(15) }; _timer.Tick += (_,_) => Tick(); }
    public void Start() { ScheduleFromNow(); _lastTickUtc = DateTime.UtcNow; _tray.UpdateTooltip(); _timer.Start(); }
    public void ScheduleFromNow() { _snoozedUntilUtc = null; _nextUtc = DateTime.UtcNow.AddMinutes(Math.Max(1, _settings.Current.ReminderMinutes)); }
    public void Snooze(int minutes) { _snoozedUntilUtc = DateTime.UtcNow.AddMinutes(Math.Max(1, minutes)); }
    public void SnoozeUntilTomorrow() { var tomorrow = DateTime.Now.Date.AddDays(1).Add(_settings.Current.ActiveStart); _snoozedUntilUtc = tomorrow.ToUniversalTime(); }
    private void Tick()
    {
        var now = DateTime.UtcNow;
        // A resume from sleep can jump the clock by hours. Rebase instead of firing a burst.
        if ((now - _lastTickUtc).TotalMinutes > 5) { _nextUtc = now.AddMinutes(Math.Max(1, _settings.Current.ReminderMinutes)); _lastTickUtc = now; return; }
        _lastTickUtc = now;
        if (_settings.Current.RemindersPaused || (_snoozedUntilUtc is { } snooze && now < snooze) || now < _nextUtc) { _tray.UpdateTooltip(); return; }
        if (MonitorService.IsWorkstationLocked() || (_settings.Current.AvoidFullscreen && MonitorService.IsFullscreenForeground()) || !IsActiveHours() ||
            (_settings.Current.RunOnWeekdaysOnly && (DateTime.Now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)))
        { _nextUtc = now.AddMinutes(1); _tray.UpdateTooltip(); return; }
        _nextUtc = now.AddMinutes(Math.Max(1, _settings.Current.ReminderMinutes)); _tray.UpdateTooltip(); _app.Dispatcher.Invoke(() => _app.ShowExercise());
    }
    private bool IsActiveHours()
    {
        var t = DateTime.Now.TimeOfDay; var start = _settings.Current.ActiveStart; var end = _settings.Current.ActiveEnd;
        return start <= end ? t >= start && t <= end : t >= start || t <= end;
    }
    public TimeSpan UntilNext => (_snoozedUntilUtc ?? _nextUtc) - DateTime.UtcNow;
    public void Dispose() => _timer.Stop();
}
