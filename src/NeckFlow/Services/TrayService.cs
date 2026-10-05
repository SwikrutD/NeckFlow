using System.Drawing;
using System.Windows.Forms;
using NeckFlow.Models;
using NeckFlow.Rendering;
using NeckFlow.Views;

namespace NeckFlow.Services;

public sealed class TrayService : IDisposable
{
    private readonly App _app; private readonly NotifyIcon _icon; private ExercisePopup? _popup; private SettingsWindow? _settings;
    private ToolStripMenuItem _nextItem = null!; private ToolStripMenuItem _pauseItem = null!;
    public TrayService(App app)
    {
        _app = app; _icon = new NotifyIcon { Visible = true, Text = "NeckFlow — Ready", Icon = CreateNeckIcon(), ContextMenuStrip = BuildMenu() };
        _icon.DoubleClick += (_, _) => ShowSettings();
        ApplyMenuTheme(); ThemeService.Changed += ApplyMenuTheme;
    }
    private void ApplyMenuTheme()
    {
        var dark = ThemeService.IsDark(_app.Settings.Current.Theme); var menu = _icon.ContextMenuStrip!;
        var bg = dark ? Color.FromArgb(31, 38, 51) : Color.FromArgb(248, 250, 255); var fg = dark ? Color.FromArgb(230, 235, 245) : Color.FromArgb(36, 50, 74);
        menu.Renderer = new MenuRenderer(new MenuColors(dark), dark);
        menu.BackColor = bg; menu.ForeColor = fg;
        foreach (ToolStripItem i in menu.Items) { i.BackColor = bg; i.ForeColor = i.Enabled ? fg : (dark ? Color.FromArgb(140, 152, 172) : Color.FromArgb(110, 120, 138)); }
    }
    private sealed class MenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color _hot; private readonly Color _text;
        public MenuRenderer(ProfessionalColorTable colors, bool dark) : base(colors)
        {
            RoundedEdges = false;
            _hot = dark ? Color.FromArgb(52, 64, 86) : Color.FromArgb(225, 232, 245);
            _text = dark ? Color.FromArgb(230, 235, 245) : Color.FromArgb(36, 50, 74);
        }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected && e.Item.Enabled) { using var b = new SolidBrush(_hot); e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size)); }
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Enabled) e.TextColor = _text;
            base.OnRenderItemText(e);
        }
    }
    private sealed class MenuColors : ProfessionalColorTable
    {
        private readonly bool _d; public MenuColors(bool dark) { _d = dark; UseSystemColors = false; }
        private Color Bg => _d ? Color.FromArgb(31, 38, 51) : Color.FromArgb(248, 250, 255);
        private Color Hot => _d ? Color.FromArgb(52, 64, 86) : Color.FromArgb(225, 232, 245);
        private Color Line => _d ? Color.FromArgb(62, 73, 94) : Color.FromArgb(216, 224, 240);
        public override Color ToolStripDropDownBackground => Bg; public override Color MenuBorder => Line; public override Color MenuItemBorder => Hot;
        public override Color MenuItemSelected => Hot; public override Color MenuItemSelectedGradientBegin => Hot; public override Color MenuItemSelectedGradientEnd => Hot;
        public override Color ImageMarginGradientBegin => Bg; public override Color ImageMarginGradientMiddle => Bg; public override Color ImageMarginGradientEnd => Bg;
        public override Color SeparatorDark => Line; public override Color SeparatorLight => Line;
        public override Color CheckBackground => Hot; public override Color CheckSelectedBackground => Hot; public override Color CheckPressedBackground => Hot;
    }
    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip(); menu.Items.Add("NeckFlow", null, (_, _) => ShowSettings()); menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Start Exercise Now", null, (_, _) => ShowExercise(true));
        _nextItem = new ToolStripMenuItem("Next break"); _nextItem.Enabled = false; menu.Items.Add(_nextItem); menu.Items.Add(new ToolStripSeparator());
        _pauseItem = new ToolStripMenuItem("Pause reminders", null, (_, _) => TogglePause()); menu.Items.Add(_pauseItem);
        menu.Items.Add("Pause for 30 minutes", null, (_, _) => PauseFor(30)); menu.Items.Add("Pause for 1 hour", null, (_, _) => PauseFor(60));
        menu.Items.Add("Pause until tomorrow", null, (_, _) => PauseUntilTomorrow()); menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings", null, (_, _) => ShowSettings()); menu.Items.Add("Exercise Library", null, (_, _) => ShowExercises());
        var startup = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleStartup()); startup.Checked = _app.Settings.Current.StartWithWindows; menu.Items.Add(startup);
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("About", null, (_, _) => ShowSettings()); menu.Items.Add("Exit", null, (_, _) => _app.Shutdown()); return menu;
    }
    private void TogglePause() { _app.Settings.Current.RemindersPaused = !_app.Settings.Current.RemindersPaused; _app.Settings.Save(); UpdateTooltip(); }
    private void PauseFor(int minutes) { _app.Reminders.Snooze(minutes); UpdateTooltip(); }
    private void PauseUntilTomorrow() { _app.Reminders.SnoozeUntilTomorrow(); UpdateTooltip(); }
    private void ToggleStartup() { _app.Settings.Current.StartWithWindows = !_app.Settings.Current.StartWithWindows; StartupService.SetEnabled(_app.Settings.Current.StartWithWindows); _app.Settings.Save(); UpdateTooltip(); }
    private void ShowExercises() { ShowSettings(); ExercisesButtonClick(); }
    private void ExercisesButtonClick() { _settings?.ExercisesPage(); }
    internal void UpdateTooltip()
    {
        var paused = _app.Settings.Current.RemindersPaused; _icon.Text = paused ? "NeckFlow — Paused" : $"NeckFlow — Next break in {Math.Max(1, (int)Math.Ceiling(_app.Reminders.UntilNext.TotalMinutes))} min";
        if (_pauseItem is not null) _pauseItem.Text = paused ? "Resume reminders" : "Pause reminders";
        if (_nextItem is not null) _nextItem.Text = paused ? "Reminders paused" : $"Next break in {Math.Max(1, (int)Math.Ceiling(_app.Reminders.UntilNext.TotalMinutes))} min";
    }
    public void ShowSettings() { if (_settings is { IsVisible: true }) { _settings.Activate(); return; } _settings = new SettingsWindow(_app); _settings.Show(); }
    public void ShowExercise(bool manual = false, ExerciseKind? forced = null) { if (_popup is { IsVisible: true }) return; _popup = new ExercisePopup(_app, manual, forced); _popup.Closed += (_, _) => { _popup = null; UpdateTooltip(); }; _popup.Show(); }
    public void Dispose() { _popup?.Close(); _settings?.Close(); _icon.Visible = false; _icon.Dispose(); }

    // Drawn at the shell's small-icon size so the mark stays crisp at every display scale.
    private static Icon CreateNeckIcon() => TrayIconArt.CreateIcon(Math.Max(16, SystemInformation.SmallIconSize.Width));
}
