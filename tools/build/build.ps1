# Builds de RowMate por linea de comandos (ARCHITECTURE Sec. 19, 02-tech-design Sec. 1.1): Unity -batchmode -executeMethod
# RowMate.EditorTools.BuildCli.Build. Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
#
#   tools/build/build.ps1 -Target Windows|Android [-Simulator] [-Backend Mono|IL2CPP] [-SmokeTest] [-DbSmoke] [-Out <carpeta>]
#                         [-AllowNewFolder] [-Unity <Unity.exe>] [-ArtifactsPath <carpeta>]
#
# - Builds de DESARROLLO (-Simulator, -SmokeTest o -DbSmoke) en Windows: SIEMPRE en la misma carpeta (QA-V3-08 de la It. 3).
#   Un player de desarrollo abre el puerto de PlayerConnection de Unity y Windows pregunta por el Firewall una vez por cada
#   ruta de RowMate.exe; con la carpeta fija la pregunta sale una sola vez en la PC. Simulador: Builds/Windows/simulator;
#   normal de desarrollo: Builds/Windows/dev-smoke. Con otro -Out sale con 5 sin abrir Unity, salvo con -AllowNewFolder (y
#   entonces habra un aviso nuevo del Firewall al abrirlo). El build normal (release) no abre el puerto: su -Out es libre.
#
# - -DbSmoke (It. 3, R3-01; 03-rutinas/02-tech-design Sec. 6.1): build de DESARROLLO (como -SmokeTest) y despues corre el
#   player una vez con -rowmate-db-smoke sobre una carpeta temporal propia (<temp>/RowMate.Tests/db-smoke-<id>, borrada en
#   finally): crea la base v8, guarda y relee Escalera SPM y borra. Pasa si el registro del player dice "[DB] humo: ok".
#   Solo Windows (en Android hace falta el telefono: NO VERIFICABLE). Codigo 4 si el humo falla.
#
# - Con -Simulator: BuildCli pasa ROWMATE_SIMULATOR por extraScriptingDefines (nunca a PlayerSettings) y fuerza Development
#   (R-01, AC-BLE-13-01). Sin -Simulator: al terminar, BuildOutputInspector busca rastros del simulador en la carpeta del
#   build y el build falla (codigo 2) si encuentra alguno (AC-BLE-13-02).
# - -Backend Mono es el temporal de la It. 2 (D-16: no esta el modulo IL2CPP de Windows); BuildCli lo restaura al terminar.
# - Si falta el modulo de la plataforma o el de IL2CPP (-Backend IL2CPP): NO VERIFICABLE, codigo 3, sin compilar nada.
# Salida por defecto: Builds/<Target>/<normal|simulator|dev-smoke>/RowMate.exe (Builds/ esta en .gitignore).
# Codigos: 0 bien; 1 el build fallo; 2 rastros del simulador en un build normal; 3 falta un modulo; 4 el humo de -DbSmoke
# fallo; 5 build de desarrollo con otra carpeta (QA-V3-08); 10 proyecto abierto en otro Editor; 12 no encuentra Unity.exe.
param(
    [ValidateSet('Windows', 'Android')]
    [string]$Target = 'Windows',
    [switch]$Simulator,
    [ValidateSet('Mono', 'IL2CPP')]
    [string]$Backend = 'Mono',
    [switch]$SmokeTest,
    [switch]$DbSmoke,
    [string]$Out = '',
    [switch]$AllowNewFolder,
    [string]$Unity = '',
    [string]$ArtifactsPath = 'tools/unity/.artifacts'
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$buildMethod = 'RowMate.EditorTools.BuildCli.Build'

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

function Quote-Arg([string]$Value) {
    if ($Value -match '[\s"]') { return '"' + ($Value -replace '"', '\"') + '"' }
    return $Value
}

$variant = 'normal'
if ($Simulator) { $variant = 'simulator' }
# QA-V3-08: la carpeta fija de cada build de desarrollo (antes de abrir Unity: una negativa no toca nada).
$development = $Simulator -or $SmokeTest -or $DbSmoke
$fixedOut = 'Builds/' + $Target + '/' + $variant
if ($development -and -not $Simulator) { $fixedOut = 'Builds/' + $Target + '/dev-smoke' }
if ($Out -eq '') {
    $Out = $fixedOut
}
elseif ($development -and $Target -eq 'Windows' -and -not $AllowNewFolder) {
    $wanted = (Get-FullPath $Out).TrimEnd('\')
    $fixed = (Get-FullPath $fixedOut).TrimEnd('\')
    if ($wanted -ne $fixed) {
        Write-Host ('[build] los builds de desarrollo van siempre a ' + $fixedOut + ': cada carpeta nueva de un RowMate.exe de desarrollo dispara el aviso del Firewall de Windows (QA-V3-08, tools/unity/README.md). Sin -Out usa esa carpeta; -AllowNewFolder fuerza otra.')
        exit 5
    }
}

$unityExe = Resolve-Unity
if (-not (Test-Path $unityExe)) {
    Write-Host ('[build] no existe ' + $unityExe + ' (usar -Unity o ROWMATE_UNITY)')
    exit 12
}

if (Test-ProjectLocked) {
    Write-Host '[build] el proyecto esta abierto en otro Editor (Temp/UnityLockfile tomado): cerralo y volve a correr.'
    exit 10
}
$artifacts = Get-FullPath $ArtifactsPath
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
# Un log por destino, variante y backend (QA-V2-03): el de Mono y el de IL2CPP no se pisan.
$log = Join-Path $artifacts ('build-' + $Target.ToLowerInvariant() + '-' + $variant + '-' + $Backend.ToLowerInvariant() + '.log')

$unityArgs = @('-batchmode', '-quit', '-projectPath', $repo, '-executeMethod', $buildMethod, '-logFile', $log,
    '-rowmateTarget', $Target, '-rowmateBackend', $Backend, '-rowmateOutput', (Get-FullPath $Out))
if ($Simulator) { $unityArgs += '-rowmateSimulator' }
if ($SmokeTest -or $DbSmoke) { $unityArgs += '-rowmateSmokeTest' }

$line = ($unityArgs | ForEach-Object { Quote-Arg $_ }) -join ' '
Write-Host ('"' + $unityExe + '" ' + $line)
$info = New-Object System.Diagnostics.ProcessStartInfo
$info.FileName = $unityExe
$info.Arguments = $line
$info.UseShellExecute = $false
$process = [System.Diagnostics.Process]::Start($info)
$process.WaitForExit()
$code = $process.ExitCode
Write-Host ('[build] ' + $Target + ' ' + $variant + ' ' + $Backend + ' -> ' + $code + ' (log: ' + $log + ')')
if (Test-Path $log) {
    Select-String -Path $log -Pattern '\[BUILD\]', 'error CS\d+', 'Build Finished' | Select-Object -Last 20 | ForEach-Object { Write-Host ('  ' + $_.Line) }
}

# Humo de la base en el player (solo con -DbSmoke, en un build de desarrollo de Windows que salio bien).
if ($DbSmoke -and $code -eq 0) {
    if ($Target -ne 'Windows') {
        Write-Host '[build] -DbSmoke: solo en Windows (en Android: NO VERIFICABLE sin telefono).'
        exit 3
    }
    $exe = Join-Path (Get-FullPath $Out) 'RowMate.exe'
    $smokeDir = Join-Path ([System.IO.Path]::GetTempPath()) ('RowMate.Tests\db-smoke-' + [guid]::NewGuid().ToString('N'))
    $smokeLog = Join-Path $artifacts ('db-smoke-' + $variant + '-' + $Backend.ToLowerInvariant() + '.log')
    New-Item -ItemType Directory -Force -Path $smokeDir | Out-Null
    try {
        $smokeArgs = @('-batchmode', '-nographics', '-logFile', $smokeLog, '-rowmate-db-smoke', $smokeDir)
        $smokeLine = ($smokeArgs | ForEach-Object { Quote-Arg $_ }) -join ' '
        $smokeInfo = New-Object System.Diagnostics.ProcessStartInfo
        $smokeInfo.FileName = $exe
        $smokeInfo.Arguments = $smokeLine
        $smokeInfo.UseShellExecute = $false
        $smoke = [System.Diagnostics.Process]::Start($smokeInfo)
        if (-not $smoke.WaitForExit(120000)) { $smoke.Kill() }
        $lines = @()
        if (Test-Path $smokeLog) { $lines = @(Select-String -Path $smokeLog -Pattern '\[DB\] humo' | ForEach-Object { $_.Line }) }
        foreach ($l in $lines) { Write-Host ('  ' + $l) }
        if (($lines | Where-Object { $_ -match '\[DB\] humo: ok' }).Count -eq 0) {
            Write-Host '[build] -DbSmoke: el humo de la base fallo (ver el registro del player).'
            $code = 4
        }
        else {
            Write-Host '[build] -DbSmoke: [DB] humo: ok'
        }
    }
    finally {
        Remove-Item -Recurse -Force -Path $smokeDir -ErrorAction SilentlyContinue
    }
}
exit $code
