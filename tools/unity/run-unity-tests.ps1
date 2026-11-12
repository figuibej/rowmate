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
# It. 3 (AC-HIS-20-04; 03-rutinas/02-tech-design Sec. 2.4 y 6.1): toda corrida de tests usa una carpeta de datos PROPIA y
# temporal: ROWMATE_DATA_DIR = <temp>/RowMate.Tests/unity-<id> (Unity la hereda; AppRoot la usa en el Editor en lugar de
# LocalLow/iguisoft/RowMate) y se borra en finally, pase lo que pase. Ninguna corrida abre las bases del usuario.
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
    [switch]$RestoreOnly,
    [switch]$LogLoad
)

# It. 6b (06b-agua-avanzada/02-tech-design Sec. 6): con -LogLoad, la carga de los OTROS procesos (el total del procesador menos el de
# Unity, por WMI: no depende del idioma de Windows) antes de la corrida y cada 15 s durante; queda en load.txt de -ArtifactsPath, solo
# porcentajes (sin nombres de procesos, del equipo ni del usuario). Un tope de CPU que falla con carga externa anotada es "no concluyente
# por carga": no se relaja. Nunca toca, pausa ni mata otros procesos.
$script:loadSamples = New-Object System.Collections.Generic.List[double]
function Get-OtherLoad([System.Diagnostics.Process]$Own) {
    try {
        $total = [double](Get-CimInstance -ClassName Win32_PerfFormattedData_PerfOS_Processor -Filter "Name='_Total'" -ErrorAction Stop).PercentProcessorTime
        $mine = 0.0
        if ($null -ne $Own) {
            foreach ($p in @(Get-CimInstance -ClassName Win32_PerfFormattedData_PerfProc_Process -Filter "IDProcess=$($Own.Id)" -ErrorAction SilentlyContinue)) {
                $mine += [double]$p.PercentProcessorTime / [Environment]::ProcessorCount
            }
        }
        return [math]::Round([math]::Max(0, $total - $mine), 1)
    }
    catch { return -1 }
}

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
    if ($LogLoad) {
        while (-not $process.WaitForExit(15000)) { $script:loadSamples.Add((Get-OtherLoad $process)) }
    }
    $process.WaitForExit()
    # It. 6a (06a-escena-lago/00-deuda-5 Sec. 3): 198 = Unity no encontro una licencia valida. Cuando la licencia Personal la guarda el
    # cliente de licencias de Unity Hub en su propia carpeta, despues de reiniciar el equipo y con la Hub cerrada Unity no la encuentra.
    if ($process.ExitCode -eq 198) { Write-Host '[unity] Unity salio con 198: no encontro una licencia valida. Abri Unity Hub (con la sesion iniciada), espera a que cargue y repeti el comando.' }
    return $process.ExitCode
}

# QA-V4b-R2-03 (It. 5; docs/migration/iterations/05-series-parciales/00-deuda-4b.md Sec. 3): Unity a veces revienta al cerrar
# (0xC0000005) un -batchmode -quit que cambio los defines, con la compilacion que pidio ese cambio todavia en curso. El paso del
# define cuenta como bien si quedo como se pidio (ROWMATE_SIMULATOR en ProjectSettings.asset y la marca de Library/RowMate); si
# no, se repite UNA vez. Cualquier otro codigo distinto de 0 sigue siendo un error.
$crashCodes = @(-1073741819, 3221225477)
$markerFile = Join-Path $repo 'Library\RowMate\simulator-tests.txt'
$script:definesBefore = $null

function Test-DefineAsAsked([string]$Method) {
    if ($Method -eq $enableMethod) { return ((Test-SimulatorInSettings) -and (Test-Path $markerFile)) }
    if (Test-Path $markerFile) { return $false }
    if ($null -ne $script:definesBefore) { return ((Test-SimulatorInSettings) -eq $script:definesBefore) }
    return $true
}

function Invoke-Method([string]$Method, [string]$LogName) {
    $log = Join-Path $script:artifacts $LogName
    $code = Invoke-Unity @('-batchmode', '-quit', '-projectPath', $repo, '-executeMethod', $Method, '-logFile', $log)
    Write-Host ('[unity-tests] ' + $Method + ' -> ' + $code + ' (log: ' + $log + ')')
    if ($crashCodes -contains $code) {
        if (Test-DefineAsAsked $Method) {
            Write-Host '[unity-tests] Unity revento al cerrar despues de cambiar el define, pero quedo como se pidio: se sigue (QA-V4b-R2-03).'
            return 0
        }
        $retryLog = Join-Path $script:artifacts ($LogName -replace '\.log$', '-retry.log')
        Write-Host '[unity-tests] Unity revento al cerrar y el define no quedo como se pidio: se repite una vez (QA-V4b-R2-03).'
        $code = Invoke-Unity @('-batchmode', '-quit', '-projectPath', $repo, '-executeMethod', $Method, '-logFile', $retryLog)
        Write-Host ('[unity-tests] ' + $Method + ' (otra vez) -> ' + $code + ' (log: ' + $retryLog + ')')
        if (($crashCodes -contains $code) -and (Test-DefineAsAsked $Method)) { return 0 }
    }
    return $code
}

# O-R2-01 de la 4b: el Unity Test Framework deja un TestResults.xml en la carpeta de datos de la app del Editor
# (Application.persistentDataPath: LocalLow/<companyName>/<productName>) en cada PlayMode. No es una base; se borra si lo
# escribio esta corrida (ninguna otra cosa de esa carpeta se toca).
function Remove-EditorTestResults([datetime]$Since) {
    $settings = Join-Path $repo 'ProjectSettings\ProjectSettings.asset'
    if (-not (Test-Path $settings) -or $null -eq $env:USERPROFILE) { return }
    $text = [System.IO.File]::ReadAllText($settings)
    $company = [regex]::Match($text, '(?m)^\s*companyName:\s*(.+?)\s*$')
    $product = [regex]::Match($text, '(?m)^\s*productName:\s*(.+?)\s*$')
    if (-not $company.Success -or -not $product.Success) { return }
    $file = Join-Path $env:USERPROFILE ('AppData\LocalLow\' + $company.Groups[1].Value + '\' + $product.Groups[1].Value + '\TestResults.xml')
    if ((Test-Path $file) -and ((Get-Item $file).LastWriteTime -ge $Since)) {
        Remove-Item -Force -Path $file -ErrorAction SilentlyContinue
        Write-Host '[unity-tests] se borro el TestResults.xml que el Test Framework dejo en la carpeta de datos del Editor (O-R2-01).'
    }
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

# It. 6a (06a-escena-lago/02-tech-design Sec. 4.2; QA-G6aT-05): la guardia de ProjectSettings. Las pruebas no pueden dejar cambiados
# ProjectSettings/ ni Assets/RowMate/Settings/ (el define del simulador lo restaura este mismo script y no cuenta).
. (Join-Path $PSScriptRoot 'ProjectSettingsGuard.ps1')
$settingsBefore = Get-ProjectSettingsSnapshot $repo

$wasInSettings = Test-SimulatorInSettings
$script:definesBefore = $wasInSettings
$runStarted = Get-Date
$name = $Platform.ToLowerInvariant()
if ($Simulator) { $name = $name + '-simulator' }
$xml = Join-Path $script:artifacts ($name + '-results.xml')
$log = Join-Path $script:artifacts ($name + '.log')
if (Test-Path $xml) { Remove-Item $xml -Force }

$testArgs = @('-batchmode', '-projectPath', $repo, '-runTests', '-testPlatform', $Platform, '-testResults', $xml, '-logFile', $log)
$script:loadBefore = -1
if ($LogLoad) { $script:loadBefore = Get-OtherLoad $null }
if ($Filter -ne '') { $testArgs += @('-testFilter', $Filter) }
if ($Category -ne '') { $testArgs += @('-testCategory', $Category) }

# Carpeta de datos propia de la corrida (AC-HIS-20-04): la hereda Unity y se borra en finally. El bloque de la corrida
# va sin sangria adentro del try a proposito (es el mismo texto de la It. 1, que revisa SimulatorInUnityTests).
# It. 10a (02-tech-design Sec. 5.1): ninguna prueba toca la radio. ROWMATE_BLE=off deja el remo sin plugin nativo (BleAdapterFactory);
# se restaura en finally.
$previousBle = $env:ROWMATE_BLE
$env:ROWMATE_BLE = 'off'
$previousDataDir = $env:ROWMATE_DATA_DIR
$dataDir = Join-Path ([System.IO.Path]::GetTempPath()) ('RowMate.Tests\unity-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
$env:ROWMATE_DATA_DIR = $dataDir
try {
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
}
finally {
    if ($null -eq $previousDataDir) { Remove-Item Env:ROWMATE_DATA_DIR -ErrorAction SilentlyContinue } else { $env:ROWMATE_DATA_DIR = $previousDataDir }
    if ($null -eq $previousBle) { Remove-Item Env:ROWMATE_BLE -ErrorAction SilentlyContinue } else { $env:ROWMATE_BLE = $previousBle }
    Remove-Item -Recurse -Force -Path $dataDir -ErrorAction SilentlyContinue
}

Write-Host ('[unity-tests] -runTests -> ' + $exitCode + ' (log: ' + $log + ')')
if ($LogLoad) {
    $samples = @($script:loadSamples | Where-Object { $_ -ge 0 })
    $loadLine = '[unity-tests] carga de los otros procesos: antes ' + $script:loadBefore + ' %'
    if ($samples.Count -gt 0) {
        $loadLine += ', durante media ' + [math]::Round(($samples | Measure-Object -Average).Average, 1) + ' %, max ' + ($samples | Measure-Object -Maximum).Maximum + ' % (' + $samples.Count + ' muestras)'
    }
    Write-Host $loadLine
    Add-Content -Path (Join-Path $script:artifacts 'load.txt') -Value ($name + ': ' + $loadLine) -Encoding ASCII
}
$guardCode = Write-ProjectSettingsGuardResult '[unity-tests]' (Compare-ProjectSettingsSnapshot $repo $settingsBefore)
if ($exitCode -eq 0 -and $guardCode -ne 0) { $exitCode = $guardCode }
if ($Platform -eq 'PlayMode') { Remove-EditorTestResults $runStarted }
Show-Results $xml $log
exit $exitCode
