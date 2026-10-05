namespace NeckFlow.Models;

public sealed class AppSettings
{
    public int ReminderMinutes { get; set; } = 30;
    public bool RemindersPaused { get; set; }
    public bool StartWithWindows { get; set; }
    public bool RunOnWeekdaysOnly { get; set; }
    public TimeSpan ActiveStart { get; set; } = new(8,0,0);
    public TimeSpan ActiveEnd { get; set; } = new(22,0,0);
    public bool SkipLocked { get; set; } = true;
    public bool AvoidFullscreen { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool TopmostPopup { get; set; } = true;
    public string Theme { get; set; } = "System";
    public string SessionMode { get; set; } = "Quick";
    public bool HasCompletedOnboarding { get; set; }
    public List<ExerciseKind> ExerciseOrder { get; set; } = Exercises.All.Select(x => x.Kind).ToList();
}
