# Arnes de capturas (It. 2: 02-tech-design Sec. 4.6; It. 3: 03-rutinas/02-tech-design Sec. 5.1; It. 4a:
# 04a-motor-hud-funcional/02-tech-design Sec. 7.1): corre
# RowMate.Snapshots.SnapshotCli.Run en el Editor, en modo edicion (sin Play), con ROWMATE_SIMULATOR puesto solo para la corrida.
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
# Compatible con Windows PowerShell 5.1 y pwsh 7 (sin los operadores null-conditional, null-coalescing ni pipeline chain de PS 7).
#
#   tools/unity/capture-snapshots.ps1 [-Set it4b|it4a|it3|it2] [-Filter "CAP-01,CAP-1*,VID-*"] [-Pipeline GammaRt|Overlay] [-Out <carpeta>]
#                                     [-Unity <Unity.exe>] [-ArtifactsPath <carpeta>] [-NoSimulator] [-RestoreOnly]
#
# -Set: el catalogo (it4b por defecto desde la It. 4b: el HUD EXR, CAP-01..31, DEMO-01..12 y VID-01..08 en
# docs/migration/iterations/04b-hud-exr/snapshots; it4a: el entrenamiento de la 4a, con las tomas del HUD reemplazadas por las de
# it4b (no se vuelven a tomar), 30 CAP (CAP-27 con a, b y c), DEMO-01..14 y VID-01..03
# = 49 archivos en docs/migration/iterations/04a-motor-hud-funcional/snapshots; it3: Rutinas, 42 IDs = 47 archivos en
# docs/migration/iterations/03-rutinas/snapshots;
# it2: Dispositivo y simulador, en docs/migration/iterations/02-dispositivo-simulador/snapshots). Sin -Out, la carpeta del
# conjunto. CAP-27 (ventana del build normal) NUNCA sale de aca: es tools/unity/capture-cap27.ps1, a pedido.
# Carpeta de datos (AC-HIS-20-04): la corrida pone ROWMATE_DATA_DIR en una carpeta temporal propia
# (<temp>/RowMate.Tests/capture-<id>) y la borra en finally. Las tomas usan bases de prueba (TempDatabase): nunca las del
# usuario.
#
# Tres corridas de Unity, una detras de otra (una sola instancia por proyecto):
#   1. Unity -batchmode -quit -executeMethod RowMate.EditorTools.SimulatorDefine.EnableForTests: agrega el define (y deja la
#      marca Library/RowMate/simulator-tests.txt, con la que SimulatorBuildGuard frena cualquier build).
#   2. Unity -batchmode -executeMethod RowMate.Snapshots.SnapshotCli.Run, SIN -quit: el arnes registra su maquina de estados
#      en EditorApplication.update y sale solo con EditorApplication.Exit (0 bien, 1 alguna toma fallo, 3 tope de 30 min).
#   3. SIEMPRE (finally), aunque el paso 2 falle o se corte: RestoreAfterTests deja los defines como estaban y borra la marca;
#      despues se verifica en ProjectSettings/ProjectSettings.asset que ROWMATE_SIMULATOR quedo igual que antes del paso 1.
# Con -NoSimulator solo el paso 2 (sin el define el arnes hace CAP-22 y CAP-23).
#
# Codigos propios: 10 el proyecto esta abierto en otro Editor (Temp/UnityLockfile tomado; no toca nada), 11 el define no quedo
# como antes (correr -RestoreOnly), 12 no encuentra Unity.exe. Si no, el codigo del arnes (o el del paso 1 o 3 si fallaron).
param(
    [ValidateSet('it4b', 'it4a', 'it3', 'it2')]
    [string]$Set = 'it4b',
    [string]$Filter = '',
    [ValidateSet('GammaRt', 'Overlay')]
    [string]$Pipeline = 'GammaRt',
    [string]$Out = '',
    [string]$Unity = '',
    [string]$ArtifactsPath = 'tools/unity/.artifacts',
    [switch]$NoSimulator,
    [switch]$RestoreOnly
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$symbol = 'ROWMATE_SIMULATOR'
$enableMethod = 'RowMate.EditorTools.SimulatorDefine.EnableForTests'
$restoreMethod = 'RowMate.EditorTools.SimulatorDefine.RestoreAfterTests'
$harnessMethod = 'RowMate.Snapshots.SnapshotCli.Run'

function Resolve-Unity {
    if ($Unity -ne '') { return $Unity }
    if ($null -ne $env:ROWMATE_UNITY -and $env:ROWMATE_UNITY -ne '') { return $env:ROWMATE_UNITY }
    $versionFile = Join-Path $repo 'ProjectSettings\ProjectVersion.txt'
    $match = [regex]::Match([System.IO.File]::ReadAllText($versionFile), 'm_EditorVersion:\s*(\S+)')
    if (-not $match.Success) { throw "no se pudo leer m_EditorVersion de $versionFile" }
    return 'C:\Program Files\Unity\Hub\Editor\' + $match.Groups[1].Value + '\Editor\Unity.exe'
}

function Get-FullPath([string]$Path) {
    $p = $Path -replace '/', '\'
    if (-not [System.IO.Path]::IsPathRooted($p)) { $p = Join-Path $repo $p }
    return [System.IO.Path]::GetFullPath($p)
}

# Editor abierto sobre este proyecto: Temp/UnityLockfile existe y esta tomado.
function Test-ProjectLocked {
    $lock = Join-Path $repo 'Temp\UnityLockfile'
    if (-not (Test-Path $lock)) { return $false }
    try {
        $stream = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
        $stream.Close()
        return $false
    }
    catch {
        return $true
    }
}

function Test-SimulatorInSettings {
    $settings = Join-Path $repo 'ProjectSettings\ProjectSettings.asset'
    if (-not (Test-Path $settings)) { return $false }
    return ([System.IO.File]::ReadAllText($settings)).Contains($symbol)
}

function Quote-Arg([string]$Value) {
    if ($Value -match '[\s"]') { return '"' + ($Value -replace '"', '\"') + '"' }
    return $Value
}

# Unity.exe es una app de ventana: con & PowerShell no espera. Process.Start + WaitForExit da el codigo de salida real.
function Invoke-Unity([string[]]$UnityArgs) {
    $line = ($UnityArgs | ForEach-Object { Quote-Arg $_ }) -join ' '
    Write-Host ('"' + $script:unityExe + '" ' + $line)
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $script:unityExe
    $info.Arguments = $line
    $info.UseShellExecute = $false
    $process = [System.Diagnostics.Process]::Start($info)
    $process.WaitForExit()
    return $process.ExitCode
}

function Invoke-Method([string]$Method, [string]$LogName) {
    $log = Join-Path $script:artifacts $LogName
    $code = Invoke-Unity @('-batchmode', '-quit', '-projectPath', $repo, '-executeMethod', $Method, '-logFile', $log)
    Write-Host ('[capturas] ' + $Method + ' -> ' + $code + ' (log: ' + $log + ')')
    return $code
}

function Restore-Simulator([bool]$Check, [bool]$WasInSettings) {
    $code = Invoke-Method $restoreMethod 'capture-simulator-restore.log'
    $now = Test-SimulatorInSettings
    if ($Check -and $now -ne $WasInSettings) {
        Write-Host ('[capturas] ATENCION: ' + $symbol + ' en ProjectSettings.asset quedo en ' + $now + ' y antes era ' + $WasInSettings + '. Corre de nuevo con -RestoreOnly.')
        if ($code -eq 0) { $code = 11 }
    }
    return $code
}

function Get-SetFolder {
    if ($Out -ne '') { return $Out }
    if ($Set -eq 'it2') { return 'docs/migration/iterations/02-dispositivo-simulador/snapshots' }
    if ($Set -eq 'it3') { return 'docs/migration/iterations/03-rutinas/snapshots' }
    if ($Set -eq 'it4a') { return 'docs/migration/iterations/04a-motor-hud-funcional/snapshots' }
    return 'docs/migration/iterations/04b-hud-exr/snapshots'
}

function Invoke-Harness {
    $log = Join-Path $script:artifacts 'capture-snapshots.log'
    $harnessArgs = @('-batchmode', '-projectPath', $repo, '-executeMethod', $harnessMethod, '-logFile', $log,
        '-snapshotSet', $Set, '-snapshotOut', (Get-FullPath (Get-SetFolder)), '-snapshotPipeline', $Pipeline)
    if ($Filter -ne '') { $harnessArgs += @('-snapshotFilter', $Filter) }
    $code = Invoke-Unity $harnessArgs
    Write-Host ('[capturas] ' + $harnessMethod + ' -> ' + $code + ' (log: ' + $log + ')')
    if (Test-Path $log) {
        Select-String -Path $log -Pattern '\[SNAPSHOT\]', 'error CS\d+', 'Scripts have compiler errors' | Select-Object -Last 60 | ForEach-Object { Write-Host ('  ' + $_.Line) }
    }
    return $code
}

$script:unityExe = Resolve-Unity
if (-not (Test-Path $script:unityExe)) {
    Write-Host ('[capturas] no existe ' + $script:unityExe + ' (usar -Unity o ROWMATE_UNITY)')
    exit 12
}
$script:artifacts = Get-FullPath $ArtifactsPath
New-Item -ItemType Directory -Force -Path $script:artifacts | Out-Null

if (Test-ProjectLocked) {
    Write-Host '[capturas] el proyecto esta abierto en otro Editor (Temp/UnityLockfile tomado): cerralo y volve a correr.'
    exit 10
}

if ($RestoreOnly) {
    exit (Restore-Simulator $false $false)
}

# Carpeta de datos propia de la corrida (AC-HIS-20-04): la hereda Unity y se borra en finally.
$previousDataDir = $env:ROWMATE_DATA_DIR
$dataDir = Join-Path ([System.IO.Path]::GetTempPath()) ('RowMate.Tests\capture-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
$env:ROWMATE_DATA_DIR = $dataDir
$wasInSettings = Test-SimulatorInSettings
$enabled = $false
$exitCode = 0
try {
    if ($NoSimulator) {
        $exitCode = Invoke-Harness
    }
    else {
        $enabled = $true
        $enableCode = Invoke-Method $enableMethod 'capture-simulator-enable.log'
        if ($enableCode -ne 0) {
            Write-Host '[capturas] no se pudo activar el define: no se corre el arnes.'
            $exitCode = $enableCode
        }
        else {
            $exitCode = Invoke-Harness
        }
    }
}
finally {
    # SIEMPRE: los defines como estaban (verificado) y la carpeta de datos temporal borrada.
    if ($enabled) {
        $restoreCode = Restore-Simulator $true $wasInSettings
        if ($exitCode -eq 0 -and $restoreCode -ne 0) { $exitCode = $restoreCode }
    }
    if ($null -eq $previousDataDir) { Remove-Item Env:ROWMATE_DATA_DIR -ErrorAction SilentlyContinue } else { $env:ROWMATE_DATA_DIR = $previousDataDir }
    Remove-Item -Recurse -Force -Path $dataDir -ErrorAction SilentlyContinue
}
exit $exitCode
