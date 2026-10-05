Build a complete Windows 11 desktop application called **NeckFlow**.

The application is a lightweight neck-exercise reminder, timer, and guided exercise tool that runs continuously in the Windows system tray. At configurable intervals, it should display a small polished popup in the **bottom-right corner of the screen** showing a **3D animated demonstration of the neck exercise the user should perform**, along with instructions and a countdown timer.

The application should feel like a native Windows 11 utility, not a web application.

Do not just scaffold the application. Implement it completely and make it runnable.

Do not ask me questions unless something is genuinely impossible. Make sensible engineering and UI decisions yourself.

---

# 1. Technology

Use:

- C#
- Current stable .NET supported by Windows 11
- WPF for the main Windows UI
- MVVM architecture where useful
- `System.Windows.Forms.NotifyIcon` or another reliable native solution for the system tray icon
- WebView2 + Three.js for the 3D exercise visualizer, OR a good native .NET 3D library if that produces a cleaner implementation
- JSON for local settings/preferences
- No cloud services
- No login/account
- No telemetry
- Completely offline after installation
- No Electron
- No Python runtime
- No browser window

Prefer Microsoft/native APIs wherever practical.

Use NuGet packages only when they provide a meaningful benefit.

Use current stable package versions compatible with the chosen .NET version.

---

# 2. Core behavior

When Windows starts, NeckFlow should optionally launch automatically.

Normally the program has NO visible main window.

It should sit quietly in the Windows system tray.

Example tray menu:

NeckFlow
─────────────
Start Exercise Now
Next break: 18 min
─────────────
Pause reminders
Pause for 30 minutes
Pause for 1 hour
Pause until tomorrow
─────────────
Settings
Exercise Library
─────────────
Start with Windows ✓
─────────────
About
Exit

Double-clicking the tray icon should open Settings.

---

# 3. Reminder schedule

Default reminder:

Every 30 minutes.

Make this configurable:

- 15 minutes
- 20 minutes
- 30 minutes
- 45 minutes
- 60 minutes
- 90 minutes
- custom interval

Allow:

- Active hours
- Example: 8:00 AM – 10:00 PM
- Days of week
- Skip reminders when computer is locked
- Reset timer after returning from sleep
- Pause reminders
- Snooze reminders
- Disable reminders temporarily

Settings must persist after restarting the program.

---

# 4. Exercise popup

At reminder time, show a small popup near the bottom-right corner of the monitor.

Approximate dimensions:

420 × 340 px

Do NOT make this a normal taskbar application window.

The popup should:

- have rounded corners
- subtle Windows 11 style shadow
- modern typography
- support light/dark theme
- appear above normal windows
- avoid covering the Windows taskbar
- use the monitor's WORK AREA rather than raw resolution
- work correctly when the taskbar is positioned differently
- work on multi-monitor systems

If possible, show the popup on the monitor containing the currently active application.

Position:

approximately

right = workArea.Right - popup.Width - 20
bottom = workArea.Bottom - popup.Height - 20

Animate it entering from the bottom/right using a subtle slide + fade animation.

Do not aggressively steal keyboard focus from whatever application the user is currently using.

---

# 5. Popup layout

Something like:

┌──────────────────────────────────────┐
│ NeckFlow                       ×     │
│                                      │
│        [ LARGE 3D ANIMATION ]        │
│                                      │
│           Chin Tuck                  │
│                                      │
│ Pull your chin gently backward       │
│ while keeping your eyes level.       │
│                                      │
│              00:20                   │
│                                      │
│      Pause       Skip      Done      │
└──────────────────────────────────────┘

The **3D animation should be the visual focus**.

Avoid clutter.

---

# 6. 3D exercise animation

Create a simple stylized 3D human upper torso consisting of:

- torso
- shoulders
- neck
- head

It does NOT need photorealistic graphics.

Think of a clean anatomical/mannequin-style figure similar to an exercise-training application.

The user needs to clearly understand the direction of movement.

Use smooth animations.

Approximately 30–60 FPS when the popup is visible.

DO NOT render continuously while the popup is hidden.

The model should show:

- neutral starting position
- movement
- short hold
- return to neutral
- repeat

Highlight the direction of movement subtly.

You can optionally add arrows or ghosted/reference positions if this makes the movement easier to understand.

Keep the camera mostly fixed around a front/front-three-quarter view depending on the exercise.

Allow the user to drag the model with the mouse to rotate it if practical.

Add a small button:

"Reset View"

---

# 7. Initial exercises

Implement at least these exercises.

## Exercise 1 — Chin Tuck

Animation:

Head translates gently backward while remaining level.

Do NOT animate the head tilting down dramatically.

Instructions:

"Keep your eyes level and gently draw your chin straight backward."

Duration:

20 seconds.

---

## Exercise 2 — Look Left / Right

Animation:

Rotate head around the vertical neck axis.

Sequence:

center
→ left
→ center
→ right
→ center

Use gentle ranges of motion.

Duration:

30 seconds.

Instructions:

"Slowly turn your head left and right. Stay within a comfortable range."

---

## Exercise 3 — Side Tilt

Animation:

Tilt head gently toward left shoulder.

Return.

Tilt toward right shoulder.

Return.

Do NOT rotate the shoulders.

Duration:

30 seconds.

Instructions:

"Gently tilt your ear toward your shoulder without raising the shoulder."

---

## Exercise 4 — Upper-Trapezius Stretch

Animation:

Head tilts slightly to one side.

Optional simple arm/hand indication if the model supports arms.

Keep it gentle.

Duration:

20 seconds each side.

---

## Exercise 5 — Shoulder Rolls

Extend the mannequin enough to animate shoulders.

Sequence:

shoulders up
→ backward
→ down
→ forward
→ repeat

Duration:

30 seconds.

---

## Exercise 6 — Shoulder Blade Squeeze

Animate both shoulders moving slightly backward.

Duration:

20 seconds.

Instructions:

"Gently draw your shoulder blades backward and down."

---

# 8. Exercise sequencing

Default sequence can be:

Reminder 1:
Chin Tuck

Reminder 2:
Look Left / Right

Reminder 3:
Side Tilt

Reminder 4:
Shoulder Rolls

Reminder 5:
Shoulder Blade Squeeze

Then repeat.

Also provide these modes:

### Quick Break
1 exercise
~20–30 seconds

### Standard Break
3 exercises
~1–2 minutes

### Full Neck Session
all exercises
~3–5 minutes

The user can select their preferred default mode in Settings.

---

# 9. Guided sequence behavior

For multi-exercise sessions:

Show:

Exercise 1 of 3

After countdown reaches zero:

3
2
1

then automatically transition to the next exercise.

Allow:

Pause
Resume
Skip
Previous
Done

At completion show something simple like:

"Neck break complete"

with a small checkmark animation.

Automatically close after approximately 3 seconds.

---

# 10. Timer

Countdown should be highly visible.

For example:

00:20

Use a reliable timer implementation.

The timer must not drift badly if Windows is busy.

Base timing on elapsed real time rather than assuming each timer callback occurs exactly every second.

When paused:

00:14
PAUSED

Animation should pause too.

---

# 11. Snooze behavior

When reminder first appears, provide:

Start
5 min
Skip

Alternatively integrate these buttons elegantly into the popup.

Snoozing should NOT permanently alter the normal interval.

Example:

Normal reminder:
2:00 PM

User snoozes 5 minutes.

Exercise:
2:05 PM

Next normal reminder should be calculated appropriately instead of accidentally creating multiple overlapping timers.

There should never be multiple exercise popups open simultaneously.

---

# 12. User activity awareness

Avoid interrupting the user unnecessarily.

If practical using Windows APIs, detect:

- workstation locked
- sleep/resume
- full-screen applications

Settings option:

"Don't interrupt me while a full-screen application is running."

Default: enabled.

If a reminder occurs during fullscreen:

delay it until the user exits fullscreen.

Do NOT permanently lose the reminder.

---

# 13. Settings window

Create a polished Windows 11-style Settings window.

Left navigation:

General
Schedule
Exercises
Appearance
About

---

# 14. General settings

Include:

Start NeckFlow when Windows starts

Run minimized to tray

Reminder interval

Default session:

Quick
Standard
Full

Pause reminders

Show notification sound

Keep popup above other windows

Avoid reminders during fullscreen apps

---

# 15. Schedule page

Allow configurable:

Start time
End time

Days:

Mon
Tue
Wed
Thu
Fri
Sat
Sun

Preset:

Weekdays

Example default:

Monday–Friday
8:00 AM–10:00 PM

---

# 16. Exercises page

Show all exercises.

Each exercise should have:

- name
- duration
- enable toggle
- preview button

Example:

☑ Chin Tuck                    20 sec     Preview
☑ Look Left / Right            30 sec     Preview
☑ Side Tilt                    30 sec     Preview
☑ Shoulder Rolls               30 sec     Preview
☑ Shoulder Blade Squeeze       20 sec     Preview

Allow the user to reorder exercises using drag-and-drop or Up/Down buttons.

Persist the order.

---

# 17. Appearance

Options:

Theme:

System
Light
Dark

Popup size:

Compact
Normal
Large

3D animation quality:

Low
Normal
High

Default:

System
Normal
Normal

Respect Windows scaling/DPI.

It should work properly on:

100%
125%
150%
175%
200%

DPI scaling.

---

# 18. Tray icon status

Use different tooltip text based on state.

Examples:

NeckFlow — Next exercise in 24 min

NeckFlow — Paused

NeckFlow — Exercise in progress

Keep tooltip length within Windows limits.

---

# 19. Startup behavior

Implement "Start with Windows" properly.

Prefer an appropriate per-user Windows startup method.

Do not require administrator privileges.

It should be reversible from Settings.

---

# 20. Single instance

Only allow ONE copy of NeckFlow to run.

If the user starts it a second time:

bring the existing Settings window forward or silently signal the existing process.

Use a Mutex or another robust IPC mechanism.

---

# 21. Configuration storage

Store data somewhere appropriate like:

%LOCALAPPDATA%\NeckFlow\

Example:

settings.json

Store:

schedule
exercise preferences
exercise order
theme
timer interval
startup preference
popup size
audio preference

Handle corrupted settings gracefully.

If settings cannot be loaded:

backup the corrupted file and regenerate defaults.

---

# 22. Exercise history

Keep simple local history.

Example:

October 3

9:31 AM   Chin Tuck              Completed
10:02 AM  Look Left / Right      Completed
10:34 AM  Side Tilt              Skipped
11:06 AM  Shoulder Rolls         Completed

Keep approximately the last 30 days.

Settings should contain:

"View Activity"

Display basic stats like:

Today:
4 completed
1 skipped

This week:
18 completed

Do not turn this into a complicated fitness tracker.

---

# 23. Safety

This is a general wellness reminder and NOT medical software.

Display a short disclaimer during first run and in About:

"NeckFlow provides general mobility reminders and is not medical advice. Perform movements gently and only within a comfortable range. Stop if you experience pain, dizziness, numbness, weakness, or unusual symptoms."

Avoid exercises involving:

- forceful neck rotation
- rapid movements
- extreme extension
- full neck circles
- resistance exercises against the head

Animations should always show slow, controlled motion.

---

# 24. First-run experience

On first launch show a very short setup.

Screen 1:

NeckFlow

"Small movement breaks for long hours at your computer."

Get Started

Screen 2:

"How often should NeckFlow remind you?"

15 min
30 min
45 min
60 min

Default: 30 min

Screen 3:

Active hours

8:00 AM
to
10:00 PM

Screen 4:

Start with Windows

ON

Then:

"You're ready."

Start NeckFlow

After this it minimizes to tray.

Do not show onboarding again unless selected from About.

---

# 25. Sound

Optional subtle notification sound when the popup appears.

Do NOT use a loud alarm.

Allow sound to be disabled.

Do not continuously play sound during exercise.

---

# 26. Animations

UI animations should be subtle and fast.

Popup opening:

~200–300 ms fade/slide

Popup closing:

~150–250 ms

Exercise transitions:

crossfade the exercise title/instructions and smoothly transition the 3D model.

Respect Windows accessibility/reduced-motion settings if reasonably possible.

---

# 27. Keyboard controls

While popup is focused:

Space:
Pause/Resume

Right arrow:
Next

Left arrow:
Previous

Escape:
Close/Snooze depending on current state

Enter:
Start/Done

Show keyboard hints only where useful.

---

# 28. Accessibility

Include:

- keyboard navigation
- proper accessible labels
- sufficient contrast
- scalable text
- DPI-aware UI

Do not communicate exercise state using color alone.

---

# 29. Logging

Implement lightweight local logs useful for debugging.

Location:

%LOCALAPPDATA%\NeckFlow\Logs\

Use rolling logs.

Do not record personal data.

Log things like:

application startup
settings loaded
timer scheduled
popup shown
exercise completed
unexpected errors

Limit stored logs so they do not grow indefinitely.

---

# 30. Error handling

The app should never crash simply because:

- the 3D renderer failed
- settings JSON became invalid
- WebView2 temporarily failed
- monitor configuration changed
- computer resumed from sleep

Provide sensible fallbacks.

If 3D rendering fails:

show the exercise name, instructions and a simple static directional illustration rather than crashing.

---

# 31. Project structure

Use a clean structure approximately like:

NeckFlow/
│
├── NeckFlow.sln
├── README.md
│
├── src/
│   └── NeckFlow/
│       ├── App.xaml
│       ├── App.xaml.cs
│       │
│       ├── Models/
│       ├── ViewModels/
│       ├── Views/
│       │   ├── ExercisePopup.xaml
│       │   ├── SettingsWindow.xaml
│       │   └── OnboardingWindow.xaml
│       │
│       ├── Services/
│       │   ├── ReminderService.cs
│       │   ├── TrayService.cs
│       │   ├── SettingsService.cs
│       │   ├── StartupService.cs
│       │   ├── MonitorService.cs
│       │   ├── ActivityService.cs
│       │   └── FullscreenDetectionService.cs
│       │
│       ├── 3D/
│       │   ├── index.html
│       │   ├── renderer.js
│       │   ├── mannequin.js
│       │   └── exercises/
│       │
│       ├── Resources/
│       │   ├── Icons/
│       │   └── Sounds/
│       │
│       └── Utilities/
│
└── tests/
    └── NeckFlow.Tests/

Modify this structure if a cleaner architecture makes more sense.

---

# 32. 3D implementation details

If using Three.js:

Bundle everything locally.

DO NOT reference:

CDNs
Google-hosted JavaScript
external URLs

The program must work without internet.

Create the mannequin using simple Three.js primitives if necessary.

For example:

Head:
sphere or rounded shape

Neck:
cylinder

Torso:
rounded box / tapered form

Shoulders:
capsules/spheres

Parent objects correctly so the head rotates around an anatomically sensible neck pivot.

Example hierarchy:

Scene
 └── Torso
      ├── LeftShoulder
      ├── RightShoulder
      └── NeckBase
           └── Neck
                └── HeadPivot
                     └── Head

Exercise animations should manipulate transforms of these pivot objects.

Use easing.

Never simply rotate around the center of the head if that causes unnatural movement.

---

# 33. Communication between WPF and 3D renderer

Create a clean message API.

For example WPF → WebView:

{
    "command": "playExercise",
    "exercise": "chin_tuck"
}

Other commands:

pause
resume
reset
setTheme
setQuality
setCamera

Allow JS → C# messages such as:

rendererReady
animationError

Do not tightly couple the renderer to UI code.

---

# 34. Performance

Target extremely low usage while idle.

When sitting in tray:

CPU usage should be approximately zero.

The 3D renderer should not exist/render continuously if the popup is closed.

Do not maintain a 60 FPS animation loop in the background.

Memory usage should be reasonable for a small Windows utility.

Create/dispose heavy resources appropriately.

---

# 35. Window management

Exercise popup:

- no taskbar button
- topmost when configured
- rounded borderless window
- correct DPI scaling
- multi-monitor aware

Settings:

normal resizable Windows application window.

Opening Settings must not start another program instance.

---

# 36. Windows sleep handling

When computer enters sleep:

do not count sleeping time as active reminder usage.

When resumed:

recalculate the next reminder.

Do not immediately fire five missed reminders.

A sensible rule:

If a reminder was missed during sleep, wait approximately 2–5 minutes after resume before showing it.

---

# 37. Tests

Add unit tests for important non-UI logic:

- scheduling
- snooze calculation
- pause calculation
- settings serialization
- exercise sequencing
- active hours
- day-of-week scheduling
- sleep/resume timer recalculation

The build should not depend on tests that require visible desktop interaction.

---

# 38. README

Create a useful README containing:

- what NeckFlow is
- screenshots placeholder
- requirements
- how to build
- how to run
- architecture
- how exercises are represented
- how to add another exercise
- where settings are stored
- where logs are stored
- how Windows startup works
- how to create a release build

---

# 39. Build scripts

Provide an easy build command.

For example:

dotnet restore
dotnet build -c Release

Also provide a publishing script.

Create:

build.ps1

and:

publish.ps1

The publish script should create a Windows x64 release directory.

Ideally make the release self-contained if size remains reasonable.

---

# 40. Installer

If practical, add an installer project or script using a reputable Windows installer solution.

Installer should:

- install NeckFlow
- create Start Menu shortcut
- optionally start NeckFlow
- allow clean uninstall
- not require unusual system modifications

Do not bundle adware, analytics, or third-party installers.

---

# 41. App icon

Create a simple placeholder vector/icon concept for NeckFlow.

Concept:

side silhouette of a head/neck
+
small circular motion/reminder indicator

It needs to remain understandable as a tiny Windows tray icon.

Generate the required icon resources within the repository if practical.

Do not download copyrighted icons.

---

# 42. Quality requirements

This should feel like an application someone could actually install and use every day.

Do not leave:

TODO placeholders
fake buttons
nonfunctional menu items
empty Settings pages
mock timer logic
unfinished exercise animations

Features exposed in the UI must work.

If a feature cannot be implemented reliably, omit it rather than adding a button that does nothing.

---

# 43. Development workflow

Work incrementally.

Create a file:

PROGRESS.md

Maintain sections:

Completed
Currently Working On
Remaining
Known Issues
Architecture Decisions

Update it as you work.

Build the project frequently.

Fix compiler errors before continuing to large new features.

Run tests after meaningful changes.

Do not consider the project complete until:

dotnet build -c Release

passes successfully.

Also verify that the application actually launches.

---

# 44. Priority order

Implement in this order:

1. Project structure
2. Tray application
3. Reminder scheduler
4. Exercise popup
5. Timer
6. 3D mannequin renderer
7. Chin tuck animation
8. Remaining exercises
9. Exercise sequencing
10. Settings
11. Windows startup
12. Multi-monitor positioning
13. Fullscreen/sleep awareness
14. Activity history
15. Onboarding
16. Polish
17. Tests
18. Installer/release scripts
19. Documentation

Do not spend excessive time polishing architecture before there is a runnable application.

---

# 45. Final verification

Before finishing, perform a complete verification of these scenarios:

### Scenario A
Start NeckFlow.

Expected:
It appears in system tray and does not leave an unnecessary taskbar window.

### Scenario B
Right-click tray → Start Exercise Now.

Expected:
Exercise popup appears at bottom-right.

### Scenario C
Exercise starts.

Expected:
3D model demonstrates movement and timer counts down.

### Scenario D
Pause.

Expected:
timer and 3D animation both pause.

### Scenario E
Resume.

Expected:
both resume correctly.

### Scenario F
Exercise ends.

Expected:
completion message appears and popup closes.

### Scenario G
Change reminder interval.

Expected:
new schedule takes effect.

### Scenario H
Restart NeckFlow.

Expected:
settings persist.

### Scenario I
Enable Start with Windows.

Expected:
startup configuration is created correctly without administrator rights.

### Scenario J
Start second NeckFlow instance.

Expected:
only one instance remains.

### Scenario K
Move active program to second monitor.

Expected:
exercise popup uses an appropriate monitor and remains inside its work area.

### Scenario L
Computer sleeps and resumes.

Expected:
scheduler recovers without flooding reminders.

---

# 46. Final deliverable

At the end:

1. Make sure everything is saved.
2. Run the Release build.
3. Fix all compiler errors.
4. Run automated tests.
5. Update PROGRESS.md.
6. Give me a concise summary of what was implemented.
7. Tell me the exact command to run the application.
8. Tell me where the compiled `.exe` is located.
9. List any genuine remaining limitations.

The most important experience is:

**NeckFlow stays invisible in the Windows tray while I work, periodically slides a small beautiful popup into the bottom-right corner of my Windows 11 desktop, shows a clear looping 3D animation of the neck movement, guides me with a countdown timer, and disappears when the exercise is complete.**

Prioritize making that experience exceptionally reliable and polished.