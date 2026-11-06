# tools/build/perf-log-check.ps1 - el detector de [PERF] sobre el registro del player (06a-escena-lago/02-tech-design 4.5 y
# "Cambios por verificacion (ronda 1)"; QA-V6a-REN-01).
#
# Con GraphicsSettings.logWhenShaderIsCompiled (lo prende FrameTimeProbe en el build de desarrollo con simulador) el player escribe
# "Uploaded shader variant to the GPU driver: <shader> ..." cada vez que sube una variante. Este script cuenta, DESPUES de la marca
# "[PERF] marca: escena Ready" que escribe FrameTimeProbe, toda subida (o compilacion) de un shader de la escena (RowMate/Scene/*) o
# de URP (Hidden/Universal Render Pipeline/*): tiene que dar 0 (el precalentado cubrio lo que usa la sesion). La UI no cuenta. Es la
# misma regla que Core/Scene/ShaderWorkLog.cs (ShaderWorkLogTests); ItSixAToolingTests corre este script con un registro de prueba
# (control positivo y negativo).
#
# Uso: powershell -NoProfile -File tools/build/perf-log-check.ps1 -Log <registro del player>
# Codigos: 0 ninguna; 2 alguna (las lista); 3 sin la marca (NO VERIFICABLE: la escena no llego a estar lista); 4 no existe el registro.
param(
    [Parameter(Mandatory = $true)][string]$Log
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Log)) {
    Write-Output ('[PERF-SHADERS] no existe el registro: ' + $Log)
    exit 4
}

$mark = '[PERF] marca: escena Ready'
$work = @('Uploaded shader variant to the GPU driver', 'Compiled shader', 'Compiling shader')
$shaders = @('RowMate/Scene/', 'Hidden/Universal Render Pipeline/')

function Test-SceneShaderWork([string]$Line) {
    $isWork = $false
    foreach ($w in $work) {
        if ($Line.IndexOf($w, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { $isWork = $true; break }
    }
    if (-not $isWork) { return $false }
    foreach ($s in $shaders) {
        if ($Line.IndexOf($s, [System.StringComparison]::Ordinal) -ge 0) { return $true }
    }
    return $false
}

$lines = [System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Log).Path)
$marked = $false
$found = New-Object System.Collections.Generic.List[string]
foreach ($line in $lines) {
    if (-not $marked) {
        if ($line.IndexOf($mark, [System.StringComparison]::Ordinal) -ge 0) { $marked = $true }
        continue
    }
    if (Test-SceneShaderWork $line) { $found.Add($line) }
}

if (-not $marked) {
    Write-Output '[PERF-SHADERS] sin la marca de Ready en el registro: NO VERIFICABLE'
    exit 3
}

Write-Output ('[PERF-SHADERS] despues de Ready: ' + $found.Count + ' subidas o compilaciones de shaders de la escena o de URP')
foreach ($line in $found) { Write-Output ('  ' + $line) }
if ($found.Count -gt 0) { exit 2 }
exit 0
