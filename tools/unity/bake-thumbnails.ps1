# Horneado de las miniaturas del selector de escenario (It. 8: 08-entornos-selector/02-tech-design Sec. 3.4, DT8-16).
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
#
#   tools/unity/bake-thumbnails.ps1 [-Unity <Unity.exe>] [-ArtifactsPath <carpeta>]
#
# 1. tools/unity/capture-snapshots.ps1 -Set thumbs: el arnes (Editor sin -nographics, compilacion de shaders sincronica, nivel Alto,
#    ROWMATE_DATA_DIR temporal, guardia de ProjectSettings) dibuja cada escenario con ThumbnailRule (12:00, 20 m, sin bote, relZ < 150)
#    a 1824 x 1152, lo reduce 4x (BoxDownsample) y escribe Assets/RowMate/Art/Textures/Thumbnails/{lake,river,coast,regatta}.png y
#    thumbnails.json (la huella que vigila el test de dotnet). Las tomas del arnes van a Library/RowMate/thumbs (no se versiona).
# 2. Unity -batchmode -quit -executeMethod RowMate.EditorTools.ThumbnailAssets.AssignFromCommandLine: la importacion (sin mipmaps,
#    Clamp, Bilinear, BC7 / ASTC 4x4) y la asignacion a UiAssets.
# Dos horneados seguidos dejan los mismos bytes. Sale con el primer codigo distinto de 0.
param(
    [string]$Unity = '',
    [string]$ArtifactsPath = 'tools/unity/.artifacts'
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

$captureArgs = @('-NoProfile', '-File', (Join-Path $PSScriptRoot 'capture-snapshots.ps1'), '-Set', 'thumbs', '-ArtifactsPath', $ArtifactsPath)
if ($Unity -ne '') { $captureArgs += @('-Unity', $Unity) }
& powershell @captureArgs
$code = $LASTEXITCODE
Write-Host ('[miniaturas] horneado -> ' + $code)
if ($code -ne 0) { exit $code }

$exe = $Unity
if ($exe -eq '') {
    if ($null -ne $env:ROWMATE_UNITY -and $env:ROWMATE_UNITY -ne '') { $exe = $env:ROWMATE_UNITY }
    else {
        $versionFile = Join-Path $repo 'ProjectSettings\ProjectVersion.txt'
        $match = [regex]::Match([System.IO.File]::ReadAllText($versionFile), 'm_EditorVersion:\s*(\S+)')
        $exe = 'C:\Program Files\Unity\Hub\Editor\' + $match.Groups[1].Value + '\Editor\Unity.exe'
    }
}

$artifacts = $ArtifactsPath -replace '/', '\'
if (-not [System.IO.Path]::IsPathRooted($artifacts)) { $artifacts = Join-Path $repo $artifacts }
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$log = Join-Path $artifacts 'assign-thumbnails.log'
$p = Start-Process -FilePath $exe -ArgumentList @('-batchmode', '-quit', '-projectPath', ('"' + $repo + '"'), '-executeMethod',
    'RowMate.EditorTools.ThumbnailAssets.AssignFromCommandLine', '-logFile', ('"' + $log + '"')) -Wait -PassThru -NoNewWindow
Write-Host ('[miniaturas] asignacion -> ' + $p.ExitCode + ' (log: ' + $log + ')')
if (Test-Path $log) {
    Select-String -Path $log -Pattern '\[THUMBS\]', 'error CS\d+' | Select-Object -Last 10 | ForEach-Object { Write-Host ('  ' + $_.Line) }
}
exit $p.ExitCode
