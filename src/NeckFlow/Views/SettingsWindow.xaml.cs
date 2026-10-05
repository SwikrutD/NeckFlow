using System.Globalization;
using System.Windows;
using NeckFlow.Models;
using NeckFlow.Services;

namespace NeckFlow.Views;

public partial class SettingsWindow : Window
{
    private readonly App _app;
    public SettingsWindow(App app)
    {
        InitializeComponent(); _app = app;
        var c = app.Settings.Current;
        Startup.IsChecked = c.StartWithWindows; Paused.IsChecked = c.RemindersPaused; Sound.IsChecked = c.SoundEnabled;
        AvoidFullscreen.IsChecked = c.AvoidFullscreen; TopmostPopup.IsChecked = c.TopmostPopup;
        Interval.SelectedValue = c.ReminderMinutes.ToString(CultureInfo.InvariantCulture);
        ActiveStartText.Text = c.ActiveStart.ToString(@"hh\:mm"); ActiveEndText.Text = c.ActiveEnd.ToString(@"hh\:mm"); WeekdaysOnly.IsChecked = c.RunOnWeekdaysOnly;
        ThemeCombo.SelectedIndex = c.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 2 : c.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 1 : 0; ExercisesPanel.DataContext = Exercises.All;
    }

    private void ShowPanel(UIElement panel) { GeneralPanel.Visibility = Visibility.Collapsed; SchedulePanel.Visibility = Visibility.Collapsed; ExercisesPanel.Visibility = Visibility.Collapsed; AppearancePanel.Visibility = Visibility.Collapsed; AboutPanel.Visibility = Visibility.Collapsed; panel.Visibility = Visibility.Visible; }
    private void GeneralClick(object sender, RoutedEventArgs e) => ShowPanel(GeneralPanel);
    private void ScheduleClick(object sender, RoutedEventArgs e) => ShowPanel(SchedulePanel);
    private void ExercisesClick(object sender, RoutedEventArgs e) => ShowPanel(ExercisesPanel);
    private void AppearanceClick(object sender, RoutedEventArgs e) => ShowPanel(AppearancePanel);
    private void AboutClick(object sender, RoutedEventArgs e) => ShowPanel(AboutPanel);
    private void ExerciseRowClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ExerciseDefinition ex }) _app.ShowExercise(true, ex.Kind);
    }
    public void ExercisesPage() => ShowPanel(ExercisesPanel);

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        var c = _app.Settings.Current;
        c.StartWithWindows = Startup.IsChecked == true; c.RemindersPaused = Paused.IsChecked == true; c.SoundEnabled = Sound.IsChecked == true;
        c.AvoidFullscreen = AvoidFullscreen.IsChecked == true; c.TopmostPopup = TopmostPopup.IsChecked == true; c.RunOnWeekdaysOnly = WeekdaysOnly.IsChecked == true;
        if (int.TryParse(Interval.SelectedValue?.ToString(), out var minutes)) c.ReminderMinutes = Math.Clamp(minutes, 1, 240);
        if (TimeSpan.TryParse(ActiveStartText.Text, CultureInfo.InvariantCulture, out var start)) c.ActiveStart = start;
        if (TimeSpan.TryParse(ActiveEndText.Text, CultureInfo.InvariantCulture, out var end)) c.ActiveEnd = end;
        if (ThemeCombo.SelectedItem is System.Windows.Controls.ComboBoxItem theme) c.Theme = theme.Content?.ToString() ?? "System";
        StartupService.SetEnabled(c.StartWithWindows); _app.Settings.Save(); ThemeService.Apply(c.Theme); Close();
    }
}
