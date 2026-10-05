# NeckFlow

NeckFlow is an offline Windows tray utility that reminds you to take short, gentle neck and shoulder movement breaks. It shows a small bottom-right popup with a native WPF 3D mannequin, instructions, and a real-time countdown.

## Requirements

- Windows 10/11
- .NET 8 Windows Desktop Runtime to run; .NET 8 SDK to build

## Build and run

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project src/NeckFlow -c Release
```

Settings are stored at `%LOCALAPPDATA%\\NeckFlow\\settings.json`; activity history is in `activity.json`. Startup uses the per-user `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run` key and requires no administrator rights.

## Architecture

The tray service owns the popup and settings window. The reminder service uses elapsed wall-clock time and a dispatcher timer. The popup creates and disposes a native WPF `Viewport3D` scene on demand. Exercises are data definitions in `Models/Exercise.cs`. The 3D mannequin, its per-exercise motion curves, muscle highlights and direction arrows live in `Rendering/MannequinScene.cs`; to add a movement, add a definition plus a branch in `MannequinScene.Update`, `BuildHighlights` and `BuildArrows`.

Pre-rendered clips of every animation are in `animations/` (open `animations/index.html`); regenerate them with `animations/render.ps1`.
