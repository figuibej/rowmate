# Builds de RowMate por linea de comandos (ARCHITECTURE Sec. 19, 02-tech-design Sec. 1.1): Unity -batchmode -executeMethod
# RowMate.EditorTools.BuildCli.Build. Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
#
#   tools/build/build.ps1 -Target Windows|Android [-Simulator] [-Backend Mono|IL2CPP] [-SmokeTest] [-Out <carpeta>]
#                         [-Unity <Unity.exe>] [-ArtifactsPath <carpeta>]
#
# - Con -Simulator: BuildCli pasa ROWMATE_SIMULATOR por extraScriptingDefines (nunca a PlayerSettings) y fuerza Development
#   (R-01, AC-BLE-13-01). Sin -Simulator: al terminar, BuildOutputInspector busca rastros del simulador en la carpeta del
#   build y el build falla (codigo 2) si encuentra alguno (AC-BLE-13-02).
# - -Backend Mono es el temporal de la It. 2 (D-16: no esta el modulo IL2CPP de Windows); BuildCli lo restaura al terminar.
# - Si falta el modulo de la plataforma: NO VERIFICABLE, codigo 3.
# Salida por defecto: Builds/<Target>/<normal|simulator>/RowMate.exe (Builds/ esta en .gitignore).
# Codigos: 0 bien; 1 el build fallo; 2 rastros del simulador en un build normal; 3 falta un modulo; 10 proyecto abierto en
# otro Editor; 12 no encuentra Unity.exe.
param(
    [ValidateSet('Windows', 'Android')]
    [string]$Target = 'Windows',
    [switch]$Simulator,
    [ValidateSet('Mono', 'IL2CPP')]
    [string]$Backend = 'Mono',
    [switch]$SmokeTest,
    [string]$Out = '',
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

$unityExe = Resolve-Unity
if (-not (Test-Path $unityExe)) {
    Write-Host ('[build] no existe ' + $unityExe + ' (usar -Unity o ROWMATE_UNITY)')
    exit 12
}

if (Test-ProjectLocked) {
    Write-Host '[build] el proyecto esta abierto en otro Editor (Temp/UnityLockfile tomado): cerralo y volve a correr.'
    exit 10
}

$variant = 'normal'
if ($Simulator) { $variant = 'simulator' }
if ($Out -eq '') { $Out = 'Builds/' + $Target + '/' + $variant }
$artifacts = Get-FullPath $ArtifactsPath
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$log = Join-Path $artifacts ('build-' + $Target.ToLowerInvariant() + '-' + $variant + '.log')

$unityArgs = @('-batchmode', '-quit', '-projectPath', $repo, '-executeMethod', $buildMethod, '-logFile', $log,
    '-rowmateTarget', $Target, '-rowmateBackend', $Backend, '-rowmateOutput', (Get-FullPath $Out))
if ($Simulator) { $unityArgs += '-rowmateSimulator' }
if ($SmokeTest) { $unityArgs += '-rowmateSmokeTest' }

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
exit $code
