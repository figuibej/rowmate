# Builds de RowMate por linea de comandos (ARCHITECTURE Sec. 19, 02-tech-design Sec. 1.1): Unity -batchmode -executeMethod
# RowMate.EditorTools.BuildCli.Build. Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
#
#   tools/build/build.ps1 -Target Windows|Android [-Simulator] [-Backend Mono|IL2CPP] [-SmokeTest] [-DbSmoke] [-BleSmoke] [-ExportSmoke] [-Out <carpeta>]
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
# - -BleSmoke (It. 10a, 10a-ble-android-windows/02-tech-design Sec. 5.3; AC-BLE-04-07, -05-10, -91-02): build de DESARROLLO de la
#   variante normal (Builds/Windows/dev-smoke) y despues corre el player con -rowmate-ble-smoke, en ventana (la segunda busqueda la
#   minimiza y la restaura) y con ROWMATE_DATA_DIR en una carpeta temporal propia (borrada en finally; nunca la base del usuario).
#   El player lee el estado del adaptador REAL y hace 3 busquedas de 10 s con RowerService, sin conectar, emparejar ni cambiar la
#   radio, y escribe solo contadores: "[BLE] humo: estado=On busquedas=3 ftms=N fallas=0 colgado=no soloFtms=si". Pasa con esa
#   linea; codigo 6 si no (o si el player no termina en 90 s). Solo Windows.
#   It. 11 (DT11-16; AC-HRM-02-06): despues, 1 busqueda de pulsometros de 10 s con HeartRateService, sin conectar a nada:
#   "[HRM] humo: busquedas=1 sensores=N ajenos=0 fallas=0". Pasa solo con las dos lineas.
# - -ExportSmoke (It. 12, 12-exportar-tcx/02-tech-design DT12-20; AC-STR-09-26): build de DESARROLLO de la variante normal
#   (Builds/Windows/dev-smoke); siembra F-9 con tools/sessions (seed, --variant normal) en una carpeta de datos temporal propia
#   (<temp>/RowMate.Tests/export-smoke-<id>, borrada en finally) y corre el player en ventana con ROWMATE_DATA_DIR ahi, ROWMATE_BLE=off
#   y -rowmate-export-smoke 2: exporta la sesion mas nueva dos veces a <datos>\exports. Pasa si el registro dice "[EXPORT] humo: ok",
#   hay exactamente <nombre>.tcx y <nombre>_2.tcx, el primero es XML bien formado (TrainingCenterDatabase, vueltas y puntos) e igual,
#   nombre y bytes, al que arma tools/sessions tcx con la zona del equipo (m-05), y la carpeta Documentos\RowMate\TCX no cambio (la
#   lista de archivos antes y despues; nunca se escribe ahi). Nunca abre el Explorador. Codigo 7 si falla. Solo Windows.
# - Windows: si el RowMate.exe de la carpeta de salida esta en uso, espera 2 min y reintenta, hasta 20 veces, sin cerrar nada.
# - Con -Simulator: BuildCli pasa ROWMATE_SIMULATOR por extraScriptingDefines (nunca a PlayerSettings) y fuerza Development
#   (R-01, AC-BLE-13-01). Sin -Simulator: al terminar, BuildOutputInspector busca rastros del simulador en la carpeta del
#   build y el build falla (codigo 2) si encuentra alguno (AC-BLE-13-02).
# - -Backend Mono es el temporal de la It. 2 (D-16: no esta el modulo IL2CPP de Windows); BuildCli lo restaura al terminar.
# - Si falta el modulo de la plataforma o el de IL2CPP (-Backend IL2CPP): NO VERIFICABLE, codigo 3, sin compilar nada.
# Salida por defecto: Builds/<Target>/<normal|simulator|dev-smoke>/RowMate.exe (Builds/ esta en .gitignore).
# Codigos: 0 bien; 1 el build fallo; 2 rastros del simulador en un build normal; 3 falta un modulo; 4 el humo de -DbSmoke
# fallo; 5 build de desarrollo con otra carpeta (QA-V3-08); 6 el humo de -BleSmoke fallo; 7 el humo de -ExportSmoke fallo; 10 proyecto abierto en
# otro Editor; 12 no encuentra Unity.exe.
param(
    [ValidateSet('Windows', 'Android')]
    [string]$Target = 'Windows',
    [switch]$Simulator,
    [ValidateSet('Mono', 'IL2CPP')]
    [string]$Backend = 'Mono',
    [switch]$SmokeTest,
    [switch]$DbSmoke,
    [switch]$BleSmoke,
    [switch]$ExportSmoke,
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
$development = $Simulator -or $SmokeTest -or $DbSmoke -or $BleSmoke -or $ExportSmoke
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
# It. 6a (06a-escena-lago/02-tech-design Sec. 4.2; QA-G6aT-05): la guardia de ProjectSettings. El build no puede dejar cambiados
# ProjectSettings/ ni Assets/RowMate/Settings/ (BuildCli restaura el backend y Frame Timing Stats; el define lo maneja SimulatorDefine).
. (Join-Path $PSScriptRoot '../unity/ProjectSettingsGuard.ps1')
$settingsBefore = Get-ProjectSettingsSnapshot $repo
$artifacts = Get-FullPath $ArtifactsPath
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
# Un log por destino, variante y backend (QA-V2-03): el de Mono y el de IL2CPP no se pisan.
$log = Join-Path $artifacts ('build-' + $Target.ToLowerInvariant() + '-' + $variant + '-' + $Backend.ToLowerInvariant() + '.log')

$unityArgs = @('-batchmode', '-quit', '-projectPath', $repo, '-executeMethod', $buildMethod, '-logFile', $log,
    '-rowmateTarget', $Target, '-rowmateBackend', $Backend, '-rowmateOutput', (Get-FullPath $Out))
if ($Simulator) { $unityArgs += '-rowmateSimulator' }
if ($SmokeTest -or $DbSmoke -or $BleSmoke -or $ExportSmoke) { $unityArgs += '-rowmateSmokeTest' }

# It. 10a: si el RowMate.exe de la salida esta en uso (un build abierto), se espera: cada 2 min, hasta 20 veces. Nunca se cierra nada.
if ($Target -eq 'Windows') {
    $outExe = Join-Path (Get-FullPath $Out) 'RowMate.exe'
    for ($attempt = 1; $attempt -le 20; $attempt++) {
        if (-not (Test-Path $outExe)) { break }
        $busy = $false
        try { $s = [System.IO.File]::Open($outExe, 'Open', 'ReadWrite', 'None'); $s.Close() } catch { $busy = $true }
        if (-not $busy) { break }
        if ($attempt -eq 20) { Write-Host '[build] la carpeta de salida sigue en uso despues de 20 intentos'; exit 1 }
        Write-Host ('[build] ' + $Out + ' en uso (RowMate.exe abierto): reintento ' + $attempt + ' de 20 en 2 min')
        Start-Sleep -Seconds 120
    }
}

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
# It. 6a (06a-escena-lago/00-deuda-5 Sec. 3): 198 = Unity no encontro una licencia valida (la guarda el cliente de licencias de la Hub:
# despues de reiniciar el equipo y con la Hub cerrada, Unity no la encuentra).
if ($code -eq 198) { Write-Host '[build] Unity salio con 198: no encontro una licencia valida. Abri Unity Hub (con la sesion iniciada), espera a que cargue y repeti el comando.' }
if (Test-Path $log) {
    Select-String -Path $log -Pattern '\[BUILD\]', 'error CS\d+', 'Build Finished' | Select-Object -Last 20 | ForEach-Object { Write-Host ('  ' + $_.Line) }
}
$guardCode = Write-ProjectSettingsGuardResult '[build]' (Compare-ProjectSettingsSnapshot $repo $settingsBefore)
if ($code -eq 0 -and $guardCode -ne 0) { $code = $guardCode }

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
# Humo del Bluetooth real en el player (solo con -BleSmoke, en un build de desarrollo de Windows que salio bien).
if ($BleSmoke -and $code -eq 0) {
    if ($Target -ne 'Windows') {
        Write-Host '[build] -BleSmoke: solo en Windows (en Android: NO VERIFICABLE sin telefono).'
        exit 3
    }
    $exe = Join-Path (Get-FullPath $Out) 'RowMate.exe'
    $bleDir = Join-Path ([System.IO.Path]::GetTempPath()) ('RowMate.Tests\ble-smoke-' + [guid]::NewGuid().ToString('N'))
    $bleLog = Join-Path $artifacts ('ble-smoke-' + $Backend.ToLowerInvariant() + '.log')
    New-Item -ItemType Directory -Force -Path $bleDir | Out-Null
    try {
        $bleArgs = @('-logFile', $bleLog, '-screen-fullscreen', '0', '-screen-width', '800', '-screen-height', '480', '-rowmate-ble-smoke')
        $bleInfo = New-Object System.Diagnostics.ProcessStartInfo
        $bleInfo.FileName = $exe
        $bleInfo.Arguments = (($bleArgs | ForEach-Object { Quote-Arg $_ }) -join ' ')
        $bleInfo.UseShellExecute = $false
        $bleInfo.EnvironmentVariables['ROWMATE_DATA_DIR'] = $bleDir
        if ($bleInfo.EnvironmentVariables.ContainsKey('ROWMATE_BLE')) { $bleInfo.EnvironmentVariables.Remove('ROWMATE_BLE') }
        $ble = [System.Diagnostics.Process]::Start($bleInfo)
        if (-not $ble.WaitForExit(90000)) {
            Write-Host '[build] -BleSmoke: el player no termino en 90 s (se cierra el que lanzo este script).'
            $ble.Kill()
            $code = 6
        }
        $lines = @()
        if (Test-Path $bleLog) { $lines = @(Select-String -Path $bleLog -Pattern '\[BLE\] humo', '\[HRM\] humo' | ForEach-Object { $_.Line }) }
        foreach ($l in $lines) { Write-Host ('  ' + $l) }
        $ok = (@($lines | Where-Object { $_ -match '\[BLE\] humo: estado=On busquedas=3 ftms=\d+ fallas=0 colgado=no soloFtms=si' }).Count -gt 0) -and
              (@($lines | Where-Object { $_ -match '\[HRM\] humo: busquedas=1 sensores=\d+ ajenos=0 fallas=0' }).Count -gt 0)
        if (-not $ok) {
            Write-Host '[build] -BleSmoke: el humo del Bluetooth fallo (ver el registro del player; no lleva nombres ni direcciones).'
            $code = 6
        }
        else {
            Write-Host '[build] -BleSmoke: ok'
        }
    }
    finally {
        Remove-Item -Recurse -Force -Path $bleDir -ErrorAction SilentlyContinue
    }
}

# Humo de "Exportar TCX" en el player (solo con -ExportSmoke, en un build de desarrollo de Windows que salio bien).
if ($ExportSmoke -and $code -eq 0) {
    if ($Target -ne 'Windows') {
        Write-Host '[build] -ExportSmoke: solo en Windows (en Android: NO VERIFICABLE sin telefono).'
        exit 3
    }
    $exe = Join-Path (Get-FullPath $Out) 'RowMate.exe'
    $exportDir = Join-Path ([System.IO.Path]::GetTempPath()) ('RowMate.Tests\export-smoke-' + [guid]::NewGuid().ToString('N'))
    $referenceDir = Join-Path $exportDir 'reference'
    $exportLog = Join-Path $artifacts ('export-smoke-' + $Backend.ToLowerInvariant() + '.log')
    $sessionsProject = Join-Path $repo 'tools\dotnet\RowMate.SessionList\RowMate.SessionList.csproj'
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -ne $cmd) { $dotnet = $cmd.Source } else { $dotnet = 'C:\Program Files\dotnet\dotnet.exe' }
    # Documentos\RowMate\TCX: solo se LISTA (antes y despues); el humo nunca escribe ahi.
    $documentsTcx = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'RowMate\TCX'
    $documentsBefore = ''
    if (Test-Path -LiteralPath $documentsTcx) { $documentsBefore = (@(Get-ChildItem -LiteralPath $documentsTcx -File | ForEach-Object { $_.Name + '|' + $_.Length + '|' + $_.LastWriteTimeUtc.Ticks }) -join "`n") }
    $documentsExisted = Test-Path -LiteralPath $documentsTcx
    New-Item -ItemType Directory -Force -Path $exportDir | Out-Null
    try {
        $seed = & $dotnet run --project $sessionsProject --configuration Release -- seed F-9 --data-dir $exportDir --variant normal
        if ($LASTEXITCODE -ne 0) { throw ('tools/sessions seed fallo (codigo ' + $LASTEXITCODE + ')') }
        foreach ($l in @($seed)) { Write-Host ('  ' + $l) }
        $exportArgs = @('-logFile', $exportLog, '-screen-fullscreen', '0', '-screen-width', '800', '-screen-height', '480', '-rowmate-export-smoke', '2')
        $exportInfo = New-Object System.Diagnostics.ProcessStartInfo
        $exportInfo.FileName = $exe
        $exportInfo.Arguments = (($exportArgs | ForEach-Object { Quote-Arg $_ }) -join ' ')
        $exportInfo.UseShellExecute = $false
        $exportInfo.EnvironmentVariables['ROWMATE_DATA_DIR'] = $exportDir
        $exportInfo.EnvironmentVariables['ROWMATE_BLE'] = 'off'
        $player = [System.Diagnostics.Process]::Start($exportInfo)
        if (-not $player.WaitForExit(120000)) {
            Write-Host '[build] -ExportSmoke: el player no termino en 120 s (se cierra el que lanzo este script).'
            $player.Kill()
            $code = 7
        }
        $lines = @()
        if (Test-Path $exportLog) { $lines = @(Select-String -Path $exportLog -Pattern '\[EXPORT\]' | ForEach-Object { $_.Line }) }
        foreach ($l in $lines) { Write-Host ('  ' + $l) }
        $files = @()
        $exports = Join-Path $exportDir 'exports'
        if (Test-Path -LiteralPath $exports) { $files = @(Get-ChildItem -LiteralPath $exports -File | Sort-Object Name) }
        $reference = & $dotnet run --project $sessionsProject --configuration Release -- tcx --data-dir $exportDir --out $referenceDir --variant normal
        $referenceCode = $LASTEXITCODE
        foreach ($l in @($reference)) { Write-Host ('  ' + $l) }
        $referenceFiles = @()
        if (Test-Path -LiteralPath $referenceDir) { $referenceFiles = @(Get-ChildItem -LiteralPath $referenceDir -File) }
        $problems = @()
        if (@($lines | Where-Object { $_ -match '\[EXPORT\] humo: ok' }).Count -eq 0) { $problems += 'el registro no dice "[EXPORT] humo: ok"' }
        if ($referenceCode -ne 0 -or $referenceFiles.Count -ne 1) { $problems += ('tools/sessions tcx fallo (codigo ' + $referenceCode + ')') }
        elseif ($files.Count -ne 2) { $problems += ('exports tiene ' + $files.Count + ' archivos (se esperaban 2)') }
        else {
            $name = $referenceFiles[0].Name
            $stem = [System.IO.Path]::GetFileNameWithoutExtension($name)
            if ($files[0].Name -ne $name) { $problems += ('el primer archivo es ' + $files[0].Name + ' y tools/sessions arma ' + $name) }
            if ($files[1].Name -ne ($stem + '_2.tcx')) { $problems += ('el segundo archivo es ' + $files[1].Name + ' (se esperaba ' + $stem + '_2.tcx)') }
            $a = [System.IO.File]::ReadAllBytes($files[0].FullName)
            $b = [System.IO.File]::ReadAllBytes($referenceFiles[0].FullName)
            $same = $a.Length -eq $b.Length
            if ($same) { for ($i = 0; $i -lt $a.Length; $i++) { if ($a[$i] -ne $b[$i]) { $same = $false; break } } }
            if (-not $same) { $problems += ('el TCX del player (' + $a.Length + ' bytes) no es igual al de tools/sessions (' + $b.Length + ' bytes)') }
            $c = [System.IO.File]::ReadAllBytes($files[1].FullName)
            if ($c.Length -ne $a.Length) { $problems += 'la segunda exportacion no es igual a la primera' }
            try {
                $xml = New-Object System.Xml.XmlDocument
                $xml.Load($files[0].FullName)
                $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
                $ns.AddNamespace('t', 'http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2')
                $laps = $xml.SelectNodes('//t:Lap', $ns).Count
                $points = $xml.SelectNodes('//t:Trackpoint', $ns).Count
                $id = $xml.SelectSingleNode('//t:Activity/t:Id', $ns).InnerText
                Write-Host ('[build] -ExportSmoke: ' + $name + ': XML bien formado, Id ' + $id + ', ' + $laps + ' vueltas, ' + $points + ' puntos, ' + $a.Length + ' bytes')
                if ($xml.DocumentElement.LocalName -ne 'TrainingCenterDatabase' -or $laps -lt 1 -or $points -lt 1) { $problems += 'el TCX no tiene vueltas o puntos' }
            }
            catch {
                $problems += ('el TCX no es XML bien formado: ' + $_.Exception.GetType().Name)
            }
        }
        $documentsAfter = ''
        if (Test-Path -LiteralPath $documentsTcx) { $documentsAfter = (@(Get-ChildItem -LiteralPath $documentsTcx -File | ForEach-Object { $_.Name + '|' + $_.Length + '|' + $_.LastWriteTimeUtc.Ticks }) -join "`n") }
        if ($documentsAfter -ne $documentsBefore -or (Test-Path -LiteralPath $documentsTcx) -ne $documentsExisted) { $problems += 'Documentos\RowMate\TCX cambio' }
        if ($problems.Count -gt 0) {
            foreach ($p in $problems) { Write-Host ('[build] -ExportSmoke: ' + $p) }
            $code = 7
        }
        else {
            Write-Host ('[build] -ExportSmoke: ok (' + $files[0].Name + ' y ' + $files[1].Name + ' en ROWMATE_DATA_DIR\exports, iguales a tools/sessions tcx; Documentos sin cambios)')
        }
    }
    catch {
        Write-Host ('[build] -ExportSmoke: ' + $_.Exception.Message)
        $code = 7
    }
    finally {
        Remove-Item -Recurse -Force -Path $exportDir -ErrorAction SilentlyContinue
    }
}

exit $code
