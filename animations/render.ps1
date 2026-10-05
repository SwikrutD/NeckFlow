# Renders every exercise animation to animations\<Exercise>.mp4 / .gif (plus tray icon previews) without starting the tray app.
# Requires ffmpeg on PATH for encoding; without it the raw PNG frames are left in animations\frames.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = $PSScriptRoot
$frames = Join-Path ([IO.Path]::GetTempPath()) 'NeckFlowFrames'

dotnet build (Join-Path $root 'src\NeckFlow\NeckFlow.csproj') -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$exe = Join-Path $root 'src\NeckFlow\bin\Release\net8.0-windows\NeckFlow.exe'

if (Test-Path $frames) { Remove-Item -Recurse -Force $frames }
New-Item -ItemType Directory -Force $frames | Out-Null
Write-Host 'Rendering frames (takes a few minutes)...'
$p = Start-Process -FilePath $exe -ArgumentList "--render-animations=$frames" -Wait -PassThru
$err = Join-Path $frames 'render-error.txt'
if ((Test-Path $err) -or $p.ExitCode -ne 0) { throw "Render failed: $(if (Test-Path $err) { Get-Content $err -Raw })" }

$iconDir = Join-Path $out 'tray-icon'
New-Item -ItemType Directory -Force $iconDir | Out-Null
Copy-Item (Join-Path $frames 'tray-icon-*.png') $iconDir -Force

$ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
$exercises = Get-ChildItem $frames -Directory
if (-not $ffmpeg) {
    $raw = Join-Path $out 'frames'
    if (Test-Path $raw) { Remove-Item -Recurse -Force $raw }
    Move-Item $frames $raw
    Write-Warning "ffmpeg not found; PNG frames left in $raw"
    return
}
foreach ($dir in $exercises) {
    $in = Join-Path $dir.FullName 'f%04d.png'
    $name = $dir.Name
    Write-Host "Encoding $name"
    & $ffmpeg.Source -loglevel error -y -framerate 30 -i $in -vf 'scale=720:-2:flags=lanczos' -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -movflags +faststart (Join-Path $out "$name.mp4")
    & $ffmpeg.Source -loglevel error -y -framerate 30 -i $in -vf 'fps=25,scale=560:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=192:stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a:diff_mode=rectangle' -loop 0 (Join-Path $out "$name.gif")
}
Remove-Item -Recurse -Force $frames
Write-Host "Done. Open $(Join-Path $out 'index.html') to review all animations side by side."
