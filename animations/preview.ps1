param(
    [ValidateSet('ChinTuck','LookLeftRight','SideTilt','ShoulderRolls','ShoulderBladeSqueeze')]
    [string]$Exercise = 'ChinTuck'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'publish\win-x64\NeckFlow.exe'
if (-not (Test-Path $exe)) { throw "Published executable not found: $exe. Run publish.ps1 first." }
Start-Process -FilePath $exe -ArgumentList "--exercise-kind=$Exercise"
