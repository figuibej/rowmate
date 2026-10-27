# Corre los tests de RowMate DENTRO de Unity (Unity Test Framework, batchmode): AC-BASE-08, QA-V-05 de la It. 1,
# ARCHITECTURE Sec. 15.1. Es el complemento de tools/dotnet/run-tests.ps1 (que corre las mismas fuentes con dotnet).
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
# Compatible con Windows PowerShell 5.1 y pwsh 7 (sin los operadores null-conditional, null-coalescing ni pipeline chain de PS 7).
#
#   tools/unity/run-unity-tests.ps1 [-Platform EditMode|PlayMode] [-Simulator] [-Filter <regex>] [-Category <cats>]
#                                   [-Unity <Unity.exe>] [-ArtifactsPath <carpeta>] [-RestoreOnly]
#
# Sin -Simulator: una corrida (-runTests). RowMate.Tests.Simulator NO existe sin ROWMATE_SIMULATOR (defineConstraints).
# Con -Simulator (todas las fuentes de test, incluidas las 69 del simulador):
#   1. Unity -executeMethod RowMate.EditorTools.SimulatorDefine.EnableForTests -quit: guarda en
#      Library/RowMate/simulator-tests.txt en que plataformas ya estaba el define y lo agrega en Standalone, Android e iOS.
#   2. Unity -runTests con el define: compila RowMate.Simulator y RowMate.Tests.Simulator y corre todo.
#   3. SIEMPRE (finally), aunque el paso 2 falle o se corte: Unity -executeMethod
#      RowMate.EditorTools.SimulatorDefine.RestoreAfterTests -quit deja los defines como estaban y borra la marca. Despues
#      se verifica en ProjectSettings/ProjectSettings.asset que ROWMATE_SIMULATOR quedo igual que antes del paso 1.
#   Mientras existe la marca, SimulatorBuildGuard hace fallar cualquier build (R-01: el define nunca llega a un release),
#   y en el CI el test SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator falla si el define quedo versionado.
# -RestoreOnly: solo el paso 3 (para recuperar una corrida interrumpida).
#
# Una sola instancia de Unity por proyecto: si Temp/UnityLockfile esta tomado (Editor abierto), sale con 10 sin tocar nada.
# Unity.exe: -Unity, o la variable ROWMATE_UNITY, o el Editor de ProjectSettings/ProjectVersion.txt en la ruta por defecto de
# Unity Hub. Resultados y logs en -ArtifactsPath (por defecto tools/unity/.artifacts, ignorado por git).
# Sale con el codigo de Unity de la corrida de tests (0 = todo verde; 2 = hay tests rojos; otro = no pudo correr) o con el
# primer codigo distinto de 0 de los pasos 1 y 3.
param(
    [ValidateSet('EditMode', 'PlayMode')]
    [string]$Platform = 'EditMode',
    [switch]$Simulator,
    [string]$Filter = '',
    [string]$Category = '',
    [string]$Unity = '',
    [string]$ArtifactsPath = 'tools/unity/.artifacts',
    [switch]$RestoreOnly
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$symbol = 'ROWMATE_SIMULATOR'
$enableMethod = 'RowMate.EditorTools.SimulatorDefine.EnableForTests'
$restoreMethod = 'RowMate.EditorTools.SimulatorDefine.RestoreAfterTests'

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

# El define aparece en ProjectSettings.asset (scriptingDefineSymbols). Sin el archivo (proyecto nunca abierto): no.
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
    Write-Host ('[unity-tests] ' + $Method + ' -> ' + $code + ' (log: ' + $log + ')')
    return $code
}

function Restore-Simulator([bool]$Check, [bool]$WasInSettings) {
    $code = Invoke-Method $restoreMethod 'unity-simulator-restore.log'
    $now = Test-SimulatorInSettings
    if ($Check -and $now -ne $WasInSettings) {
        Write-Host ('[unity-tests] ATENCION: ' + $symbol + ' en ProjectSettings.asset quedo en ' + $now + ' y antes era ' + $WasInSettings + '. Corre de nuevo con -RestoreOnly.')
        if ($code -eq 0) { $code = 11 }
    }
    return $code
}

function Show-Results([string]$Xml, [string]$Log) {
    if (-not (Test-Path $Xml)) {
        Write-Host ('[unity-tests] Unity no escribio ' + $Xml + '. Revisar ' + $Log)
        if (Test-Path $Log) {
            Select-String -Path $Log -Pattern 'error CS\d+', 'Scripts have compiler errors', 'Compilation failed' | Select-Object -First 20 | ForEach-Object { Write-Host ('  ' + $_.Line) }
        }
        return
    }
    $doc = New-Object System.Xml.XmlDocument
    $doc.Load($Xml)
    $run = $doc.SelectSingleNode('/test-run')
    Write-Host ('[unity-tests] ' + $Platform + ': ' + $run.GetAttribute('result') + ' - total ' + $run.GetAttribute('total') + ', pasan ' + $run.GetAttribute('passed') + ', fallan ' + $run.GetAttribute('failed') + ', ignorados ' + $run.GetAttribute('skipped') + ', inconclusos ' + $run.GetAttribute('inconclusive') + ' (' + $Xml + ')')
    $failed = @($doc.SelectNodes("//test-case[@result='Failed']"))
    foreach ($case in ($failed | Select-Object -First 20)) { Write-Host ('  FALLA ' + $case.GetAttribute('fullname')) }
}

$script:unityExe = Resolve-Unity
if (-not (Test-Path $script:unityExe)) {
    Write-Host ('[unity-tests] no existe ' + $script:unityExe + ' (usar -Unity o ROWMATE_UNITY)')
    exit 12
}
$script:artifacts = Get-FullPath $ArtifactsPath
New-Item -ItemType Directory -Force -Path $script:artifacts | Out-Null

if (Test-ProjectLocked) {
    Write-Host '[unity-tests] el proyecto esta abierto en otro Editor (Temp/UnityLockfile tomado): cerralo y volve a correr.'
    exit 10
}

if ($RestoreOnly) {
    exit (Restore-Simulator $false $false)
}

$wasInSettings = Test-SimulatorInSettings
$name = $Platform.ToLowerInvariant()
if ($Simulator) { $name = $name + '-simulator' }
$xml = Join-Path $script:artifacts ($name + '-results.xml')
$log = Join-Path $script:artifacts ($name + '.log')
if (Test-Path $xml) { Remove-Item $xml -Force }

$testArgs = @('-batchmode', '-projectPath', $repo, '-runTests', '-testPlatform', $Platform, '-testResults', $xml, '-logFile', $log)
if ($Filter -ne '') { $testArgs += @('-testFilter', $Filter) }
if ($Category -ne '') { $testArgs += @('-testCategory', $Category) }

$exitCode = 0
if ($Simulator) {
    $enableCode = Invoke-Method $enableMethod 'unity-simulator-enable.log'
    if ($enableCode -ne 0) {
        Write-Host '[unity-tests] no se pudo activar el define: no se corren tests.'
        $restoreCode = Restore-Simulator $true $wasInSettings
        exit $enableCode
    }
    # Solo para la corrida de tests (Unity lo hereda): SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator
    # distingue la corrida en curso (pasa) de una cortada (Inconclusive). Unity sale con 2 si hay un Inconclusive
    # (primer open real, It. 2).
    $env:ROWMATE_SIMULATOR_TEST_RUN = '1'
    try {
        $exitCode = Invoke-Unity $testArgs
    }
    finally {
        Remove-Item Env:ROWMATE_SIMULATOR_TEST_RUN -ErrorAction SilentlyContinue
        $restoreCode = Restore-Simulator $true $wasInSettings
        if ($exitCode -eq 0 -and $restoreCode -ne 0) { $exitCode = $restoreCode }
    }
}
else {
    $exitCode = Invoke-Unity $testArgs
}

Write-Host ('[unity-tests] -runTests -> ' + $exitCode + ' (log: ' + $log + ')')
Show-Results $xml $log
exit $exitCode
