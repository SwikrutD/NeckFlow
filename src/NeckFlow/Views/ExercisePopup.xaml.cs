using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using NeckFlow.Models;
using NeckFlow.Rendering;
using NeckFlow.Services;
using MediaColor = System.Windows.Media.Color;

namespace NeckFlow.Views;

public partial class ExercisePopup : Window
{
    private readonly App _app;
    private readonly DispatcherTimer _timer;
    private readonly ExerciseDefinition _exercise;
    private readonly MannequinScene _scene;
    private DateTime _phaseStartUtc;
    private double _pausedElapsed;
    private bool _paused;
    private bool _recorded;
    private bool _finishing;
    private bool _closing;
    private bool _rendering;
    private System.Windows.Point? _dragFrom;
    private readonly DispatcherTimer _closeTimer;
    private readonly DispatcherTimer _exitTimer;
    private static int _nextExercise;

    public ExercisePopup(App app, bool manual, ExerciseKind? forced = null)
    {
        InitializeComponent(); _app = app;
        var dark = ThemeService.IsDark(app.Settings.Current.Theme);
        ApplyTheme(dark);
        var order = app.Settings.Current.ExerciseOrder.Count == 0 ? Exercises.All.Select(x => x.Kind).ToList() : app.Settings.Current.ExerciseOrder;
        var kind = forced ?? order[_nextExercise++ % order.Count];
        if (forced is not null) _nextExercise++;
        _exercise = Exercises.All.FirstOrDefault(x => x.Kind == kind) ?? Exercises.All[0];
        ExerciseTitle.Text = _exercise.Name; Instructions.Text = _exercise.Instructions; TargetsText.Text = "Targets: " + _exercise.Targets;
        ExerciseProgress.Text = $"Exercise {((_nextExercise - 1) % order.Count) + 1} of {order.Count}";
        Topmost = app.Settings.Current.TopmostPopup; _phaseStartUtc = DateTime.UtcNow;
        _scene = new MannequinScene(_exercise.Kind);
        Stage.Background = AnimationRenderer.Stage(dark);
        Viewport.Camera = _scene.Camera;
        Viewport.Children.Add(new ModelVisual3D { Content = _scene.Lights }); Viewport.Children.Add(new ModelVisual3D { Content = _scene.Root });
        Loaded += (_, _) => { Position(); StartRendering(); if (app.Settings.Current.SoundEnabled) System.Media.SystemSounds.Asterisk.Play(); }; Closed += (_, _) => DisposeScene(); Closing += OnClosing;
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(100) }; _timer.Tick += Tick; _timer.Start();
        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.4) }; _closeTimer.Tick += (_, _) => { _closeTimer.Stop(); Close(); };
        _exitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) }; _exitTimer.Tick += (_, _) => { _exitTimer.Stop(); _closing = true; Close(); };
    }

    private void Position()
    {
        var r = MonitorService.BottomRightWorkArea(this); Left = Math.Max(r.Left, r.Right - Width - 20); Top = Math.Max(r.Top, r.Bottom - Height - 20);
        Opacity = 0;
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        EntryShift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(16, 0, TimeSpan.FromMilliseconds(240)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        EntryShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(16, 0, TimeSpan.FromMilliseconds(240)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private void ApplyTheme(bool dark)
    {
        if (!dark) return;
        PopupCard.Background = new SolidColorBrush(MediaColor.FromRgb(31, 38, 51)); PopupCard.BorderBrush = new SolidColorBrush(MediaColor.FromRgb(62, 73, 94));
        CompletionPanel.Background = PopupCard.Background; ExerciseTitle.Foreground = System.Windows.Media.Brushes.White; BrandText.Foreground = System.Windows.Media.Brushes.White; CloseButton.Foreground = System.Windows.Media.Brushes.White; TimerText.Foreground = System.Windows.Media.Brushes.White; CompletionTitle.Foreground = System.Windows.Media.Brushes.White; CompletionSubtitle.Foreground = new SolidColorBrush(MediaColor.FromRgb(194, 204, 220)); Instructions.Foreground = new SolidColorBrush(MediaColor.FromRgb(194, 204, 220));
        TargetsText.Foreground = new SolidColorBrush(MediaColor.FromRgb(255, 138, 117)); ExerciseProgress.Foreground = Instructions.Foreground;
        foreach (var b in new[] { PauseButton, SkipButton }) { b.Background = new SolidColorBrush(MediaColor.FromRgb(45, 55, 73)); b.BorderBrush = new SolidColorBrush(MediaColor.FromRgb(70, 83, 106)); b.Foreground = System.Windows.Media.Brushes.White; }
    }

    // The 3D scene is only animated while the popup is open; frames follow the display refresh rather than a timer.
    private void StartRendering() { if (_rendering) return; _rendering = true; CompositionTarget.Rendering += OnRendering; }
    private void StopRendering() { if (!_rendering) return; _rendering = false; CompositionTarget.Rendering -= OnRendering; }
    private void OnRendering(object? sender, EventArgs e) { if (!_paused && !_finishing) _scene.Update(Elapsed); }

    private void StageMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { _scene.ResetView(); return; }
        _dragFrom = e.GetPosition(Stage); Stage.CaptureMouse();
    }
    private void StageMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragFrom is not { } from) return;
        var now = e.GetPosition(Stage); _scene.Orbit(-(now.X - from.X) * .5, (now.Y - from.Y) * .3); _dragFrom = now;
    }
    private void StageMouseUp(object sender, MouseButtonEventArgs e) { _dragFrom = null; Stage.ReleaseMouseCapture(); }

    private double Elapsed => _paused ? _pausedElapsed : (DateTime.UtcNow - _phaseStartUtc).TotalSeconds;
    private void Tick(object? sender, EventArgs e)
    {
        if (_paused) return; var remaining = Math.Max(0, _exercise.DurationSeconds - Elapsed);
        TimerText.Text = TimeSpan.FromSeconds(Math.Ceiling(remaining)).ToString(@"mm\:ss"); if (remaining <= 0) ShowCompletion();
    }
    private void PauseClick(object sender, RoutedEventArgs e)
    {
        if (_paused) { _phaseStartUtc = DateTime.UtcNow - TimeSpan.FromSeconds(_pausedElapsed); _paused = false; PauseButton.Content = "Pause"; TimerText.Text = TimerText.Text.Replace("  PAUSED", ""); }
        else { _pausedElapsed = Elapsed; _paused = true; PauseButton.Content = "Resume"; TimerText.Text += "  PAUSED"; }
    }
    private void ShowCompletion()
    {
        if (_finishing) return; _finishing = true; _timer.Stop(); StopRendering(); _recorded = true; _app.Activity.Record(_exercise.Name, true);
        CompletionPanel.Visibility = Visibility.Visible; PauseButton.IsEnabled = false; _closeTimer.Start();
    }
    private void Complete(bool completed) { if (_recorded) return; _recorded = true; _app.Activity.Record(_exercise.Name, completed); Close(); }
    private void SkipClick(object sender, RoutedEventArgs e) => Complete(false); private void DoneClick(object sender, RoutedEventArgs e) => ShowCompletion(); private void CloseClick(object sender, RoutedEventArgs e) => Complete(false);
    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == Key.Space) PauseClick(sender, e); else if (e.Key == Key.Escape) Complete(false); }
    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_closing) return; e.Cancel = true; _timer.Stop(); _closeTimer.Stop(); StopRendering();
        BeginAnimation(OpacityProperty, new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
        EntryShift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, 10, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
        EntryShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, 10, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } }); _exitTimer.Start();
    }
    private void DisposeScene() { StopRendering(); _timer.Stop(); _timer.Tick -= Tick; _closeTimer.Stop(); _exitTimer.Stop(); Viewport.Children.Clear(); _scene.Root.Children.Clear(); }
}
