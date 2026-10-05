# NeckFlow animation review

This folder holds pre-rendered copies of every exercise animation so they can be reviewed without running the tray app.

- `index.html` — open in a browser to see all five animations looping side by side, plus the tray icon at each size.
- `<Exercise>.mp4` / `<Exercise>.gif` — one seamless loop of each exercise.
- `tray-icon/` — the tray icon rendered at 16, 24, 32, 64 and 256 px.

The clips are rendered offscreen from the same `MannequinScene` the popup uses, so they match what ships.

## Regenerating

From the repository root (needs the .NET 8 SDK and `ffmpeg` on PATH):

```powershell
powershell -ExecutionPolicy Bypass -File .\animations\render.ps1
```

It takes a few minutes. Without ffmpeg, the raw PNG frames are left in `animations\frames`.

## Live preview in the real popup

To see one exercise in the actual popup, where you can drag to rotate the model and double-click to reset the view, close any running NeckFlow first and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\animations\preview.ps1 -Exercise SideTilt
```

That uses `publish\win-x64\NeckFlow.exe`, so run `publish.ps1` first.
