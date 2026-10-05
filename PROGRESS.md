# NeckFlow progress

## Completed

- Built and verified the .NET 8 WPF/WinForms tray application in Release configuration.
- Added single-instance protection, local JSON settings, startup registration, activity history, tray controls, and settings UI.
- Added a deterministic `--exercise` launch argument for runtime smoke testing.
- Reworked the exercise popup lifecycle so its dispatcher timer and WPF 3D scene are stopped and released when the popup closes.
- Added a sequenced exercise flow for Chin Tuck, Look Left / Right, Side Tilt, Shoulder Rolls, and Shoulder Blade Squeeze.
- Replaced the single moving head with grouped mannequin parts and anatomically sensible neck, head, shoulder, and shoulder-blade transforms.
- Synchronised countdown and animation time, including pause/resume without elapsed-time jumps or repeated PAUSED labels.
- Added foreground-monitor placement, PerMonitorV2 DPI configuration, fullscreen detection, workstation-lock suppression, overnight active-hour handling, and sleep/resume timer rebasing.
- Replaced the reminder pause background task with reminder-owned snooze state to avoid delayed callbacks after shutdown or settings changes.

## Verification

- `dotnet build NeckFlow.sln -c Release` — passed with 0 warnings and 0 errors.
- `dotnet run --project tests/NeckFlow.Tests -c Release` — all core tests passed.
- `powershell -ExecutionPolicy Bypass -File publish.ps1` — Windows x64 publish passed.
- Published executable launches and remains resident in the tray; `--exercise` launches the popup path for smoke testing.

## Known limitations

- Rendering remains native WPF 3D rather than WebView2/Three.js. This avoids an additional runtime dependency and keeps the app offline.
- The test project is a dependency-free console runner because external test packages are unavailable in the build environment.
- Settings pages for advanced schedules and per-exercise enablement are still intentionally lightweight.

## Architecture decisions

- WPF owns the popup and settings windows; `System.Windows.Forms.NotifyIcon` owns the tray icon.
- The 3D scene exists only while the popup is visible to keep idle CPU and memory use low.
- Settings and activity history live under `%LOCALAPPDATA%\\NeckFlow`.

## Pre-Manual-Test Status

### Complete

- Release build, automated core tests, Windows x64 publish, and `--exercise` resident smoke test pass with zero build warnings or errors.
- Popup styling now has Windows 11 spacing, hierarchy, shadow, rounded card treatment, stable timer layout, hover/pressed states, fade/slide entrance, and fade exit.
- Popup completion shows a short “Neck break complete” state before closing.
- Settings now has functional General, Schedule, Exercises, Appearance, and About pages.
- Tray menu now exposes next-break status, pause/snooze choices, settings, exercise library, startup toggle, and exit.
- Recoverable settings/activity failures are written to a small rolling local log under `%LOCALAPPDATA%\\NeckFlow\\Logs`.

### Intentionally simplified

- The app continues to use native WPF 3D rather than adding WebView2/Three.js or external assets.
- Upper-Trapezius Stretch remains omitted because the existing mannequin has no arm/hand model and adding it would increase scope and risk.
- Exercise sessions remain one popup exercise per reminder; the existing sequence counter communicates the rotation without introducing a larger session state machine.

### Requires human visual testing

- Light/dark popup appearance, 3D movement comprehension, shadow/animation smoothness, keyboard focus behavior, and high-DPI rendering across 100–200% scaling.
- Multi-monitor placement with different taskbar positions, monitor disconnect/reconnect, fullscreen suppression, and lock/sleep resume behavior.
- Tray menu wording, tooltip truncation, settings navigation, and first-run feel on a normal Windows 11 desktop.

## Animation review update

- Regenerated the exercise motion curves with explicit neutral, eased travel, hold, and return phases rather than a single raw sine wave.
- Added translucent cyan 3D target highlights for the head/neck, shoulders, and shoulder blades so the exercised area is visible.
- Improved shoulder rolls with coupled vertical-axis, pitch, and depth motion; shoulder-blade squeeze now retracts both shoulders and the blade region.
- Replaced the tray information glyph with a locally drawn NeckFlow head-and-neck icon.
- Added focused preview launchers under `animations/`; run `animations/preview.ps1` with an exercise name to review one animation directly.

## 3D animation rebuild

- Replaced the box-and-sphere figure with a lit, smooth-shaded mannequin (`Rendering/MannequinScene.cs`): lofted torso, head with eyes/nose/ears so facing direction reads, a swept neck that bends through its length, shoulder girdles with upper arms, and shoulder blades that glide on the back.
- Muscles being worked are drawn as red bands that follow the pose each frame and glow with effort (SCM, deep neck flexors, suboccipitals, upper trapezius, scalenes, levator scapulae, rhomboids/middle trapezius). The popup also names them under the title.
- Cyan arrows show direction of travel and fade in with each phase; each exercise has its own camera angle (side view for chin tuck, back view for blade squeeze).
- Animation now runs on `CompositionTarget.Rendering` while the popup is open (was a 10 fps timer). Drag the stage to rotate, double-click to reset.
- New tray icon: a bust with a tilted head, the neck in the "worked muscle" colour and a motion arc, drawn at the shell's small-icon size.
- `--render-animations=<dir>` renders the clips offscreen; `animations/render.ps1` encodes them to MP4/GIF with ffmpeg.

