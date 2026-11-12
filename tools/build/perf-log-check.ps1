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
#
# It. 6b (06b-agua-avanzada/02-tech-design Sec. 2.1 (5); AC-6B-01, -02): con -EarlyLogs "<a>;<b>;..." y -EarlyMaxMs, junta las lineas
# "[PERF] ... entrada temprana: precarga en <fase>, toque X ms, peor con movimiento Y ms" de las corridas de build.ps1 -PerfEarly: el
# minimo y el maximo del toque, el peor con movimiento y la fase de la precarga en el toque. Codigos: 0 todo <= EarlyMaxMs; 5 alguno
# por encima (los lista); 3 alguna corrida sin la linea.
param(
    [Parameter(Mandatory = $true)][string]$Log,
    [string]$EarlyLogs = '',
    [double]$EarlyMaxMs = 16.7
)

if ($EarlyLogs -ne '') {
    $inv = [System.Globalization.CultureInfo]::InvariantCulture
    $taps = @(); $moving = @(); $over = @(); $missing = 0
    foreach ($path in $EarlyLogs.Split(';')) {
        if ($path -eq '' -or -not (Test-Path -LiteralPath $path)) { $missing++; continue }
        $line = @([System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $path).Path) | Where-Object { $_.IndexOf('entrada temprana: precarga en', [System.StringComparison]::Ordinal) -ge 0 }) | Select-Object -Last 1
        if ($null -eq $line) { $missing++; continue }
        $m = [regex]::Match($line, 'precarga en (.*?), toque ([\d.]+) ms(?: \(espera [\d.]+ ms\))?, peor con movimiento ([\d.]+) ms')
        if (-not $m.Success) { $missing++; continue }
        $tap = [double]::Parse($m.Groups[2].Value, $inv)
        $mov = [double]::Parse($m.Groups[3].Value, $inv)
        $taps += $tap
        $moving += $mov
        Write-Output ('[PERF-EARLY] ' + [System.IO.Path]::GetFileName($path) + ': precarga en ' + $m.Groups[1].Value + ', toque ' + $tap + ' ms, peor con movimiento ' + $mov + ' ms')
        if ($tap -gt $EarlyMaxMs) { $over += ([System.IO.Path]::GetFileName($path) + ': toque ' + $tap + ' ms') }
        if ($mov -gt $EarlyMaxMs) { $over += ([System.IO.Path]::GetFileName($path) + ': con movimiento ' + $mov + ' ms') }
    }
    if ($taps.Count -gt 0) {
        $min = ($taps | Measure-Object -Minimum).Minimum
        $max = ($taps | Measure-Object -Maximum).Maximum
        $worst = ($moving | Measure-Object -Maximum).Maximum
        Write-Output ('[PERF-EARLY] ' + $taps.Count + ' corridas: toque min ' + $min + ' ms, max ' + $max + ' ms; peor con movimiento ' + $worst + ' ms (tope ' + $EarlyMaxMs + ' ms)')
    }
    foreach ($o in $over) { Write-Output ('[PERF-EARLY] por encima del tope: ' + $o) }
    if ($missing -gt 0) { Write-Output ('[PERF-EARLY] ' + $missing + ' corrida(s) sin la linea de la entrada temprana'); exit 3 }
    if ($over.Count -gt 0) { exit 5 }
    exit 0
}

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
