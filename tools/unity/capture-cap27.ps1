# CAP-27 de la It. 3: la ventana entera del BUILD NORMAL (barra de titulo oscura del sistema, pestana Rutinas con una rutina
# sintetica, sin cinta ni llave). 03-rutinas/02-tech-design Sec. 5.5; spec Sec. 7.3; DT-12 y QA-G3-06 (aceptada con condiciones).
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
# Compatible con Windows PowerShell 5.1 y pwsh 7 (sin los operadores null-conditional, null-coalescing ni pipeline chain de PS 7).
#
#   tools/unity/capture-cap27.ps1 -ReplaceUserDatabase [-Exe Builds/Windows/normal/RowMate.exe] [-Out <png>] [-Unity <Unity.exe>]
#   tools/unity/capture-cap27.ps1 -Rehearsal [-Unity <Unity.exe>]
#
# Es la UNICA excepcion de AC-HIS-20-04: el build normal solo lee la carpeta real (LocalLow/iguisoft/RowMate), asi que la toma
# reemplaza un rato su rower_app.db por una base sintetica y la deja como estaba. Por eso:
#   0. SOLO A PEDIDO: sin -ReplaceUserDatabase (ni -Rehearsal) no toca nada, dice lo que haria y sale con 4. No lo llaman
#      capture-snapshots.ps1, run-unity-tests.ps1, build.ps1 ni ningun workflow (ItThreeToolingTests.Cap27RunsOnlyOnRequest).
#      Si Windows no esta YA en modo Oscuro (AppsUseLightTheme = 0), sale con 3 (NO VERIFICABLE) sin tocar nada: nunca cambia
#      el tema del usuario.
#   1. Base sintetica: la escribe Unity en una carpeta temporal (SnapshotCli -snapshotSeedDb <carpeta>/semilla/cap27.db
#      -seed Cap27, con ROWMATE_DATA_DIR en otra carpeta temporal): solo "Escalera SPM" de F-LISTA, nunca datos del usuario (el
#      repo es publico). Lleva un nombre NEUTRO porque TempDatabase.Guard no deja escribir rower_app.db fuera de la carpeta de
#      pruebas (QA-V3-01); el paso 4 la copia como rower_app.db. Sale con 7 si Unity falla o si el archivo no es una base SQLite.
#   2. Se niega (sale con 5, sin tocar nada) si hay un proceso RowMate o un Editor de Unity abierto, o si rower_app.db, -wal o
#      -shm no se pueden abrir con FileShare.None (archivo en uso). El Editor se reconoce por la ruta del ejecutable
#      (...\Editor\Unity.exe, de cualquier proyecto: una copia de este repo usa la misma carpeta de datos) o por el
#      Temp/UnityLockfile tomado de este proyecto. El unity.exe de la Hub ("unity.exe serve", siempre abierto con la Hub) no es
#      un Editor y no cuenta (QA-V3-01); un proceso Unity cuya ruta no se puede leer si cuenta (en la duda, no se toca nada).
#      Se comprueba antes de sembrar y otra vez justo antes del resguardo (la semilla abre Unity y tarda).
#   3. Resguardo VERIFICADO antes de tocar: copia db, -wal y -shm (o anota cuales no existen) a una carpeta calculada en el
#      momento ([IO.Path]::GetTempPath() + RowMate.Cap27.<fecha-hora>) con resguardo.json (tamano, SHA-256 y LastWriteTimeUtc);
#      anota tambien rower_app_dev.db, que no se toca. Si el SHA-256 de alguna copia no coincide, sale con 5 ANTES de reemplazar.
#   4. Reemplaza rower_app.db por la base sintetica, borra -wal y -shm, abre el build normal, toca la pestana Rutinas y captura
#      la ventana con su barra (capture-window.ps1, como en la It. 2) y cierra el build. El toque va a 0.625 del ancho (el
#      centro de la 3.a de las 4 pestanas) y a 0.98 del alto (dentro de la barra de 64 dp en ventanas de hasta 3200 dp de alto;
#      NormalBuildTests.Cap27ClickSelectsTheRoutinesTab lo prueba con la UI real). capture-window.ps1 solo hace clic y captura
#      si la ventana del build es la que esta adelante. La llamada esta UNA vez, en Invoke-WindowCapture, y el ensayo la corre
#      igual con un ejecutable de prueba sin ventana (QA-V3-11).
#   5. Restauracion en finally, pase lo que pase: cierra solo el build que abrio este script, borra lo que dejo, vuelve a
#      copiar cada archivo con su LastWriteTimeUtc original (o deja la carpeta sin base si no habia) y VERIFICA: mismos
#      archivos, mismo SHA-256 y fecha, y rower_app_dev.db igual. Si algo no coincide NO borra el resguardo: sale con 6 y dice
#      en la consola como recuperar (con las rutas reales: es la consola del usuario, no un documento). Si todo coincide, borra
#      el resguardo.
# Ni el script ni la captura escriben rutas de esta PC en el repo: CAP-27 no entra al manifest ni al INDEX del arnes.
#
# -Rehearsal (ensayo, QA-V3-01): los pasos 1, 2, 3 y 5 y el reemplazo del 4, de verdad, sobre una carpeta temporal con
# archivos de prueba (rower_app.db y -wal de mentira con una fecha vieja, rower_app_dev.db) en lugar de la carpeta del build
# normal, que nunca se nombra. No hace falta -ReplaceUserDatabase (con los dos switches manda el ensayo), no abre ningun build,
# no toca el mouse ni la pantalla y no pide el modo Oscuro. En lugar de la captura comprueba que la base sintetica quedo como
# rower_app.db (mismo SHA-256 que la semilla, sin -wal ni -shm). Despues corre la llamada de la toma real a capture-window.ps1
# (Invoke-WindowCapture, los mismos parametros) con un ejecutable de prueba sin ventana en lugar del build
# (new-windowless-exe.ps1, en la carpeta de trabajo): tiene que salir con 1 y "el build no abrio ninguna ventana", sin clic ni
# captura porque no hay ventana (QA-V3-11). Al final deja un -wal y un -shm, como el build; la restauracion los tiene que
# sacar. Los procesos que harian negarse a la toma real se informan sin negarse; este proyecto abierto en un Editor si frena
# el ensayo (5), porque la semilla necesita Unity. Sirve para probar la herramienta de punta a punta sin la base del usuario.
#
# Codigos: 0 bien; 3 NO VERIFICABLE (Windows no esta en Oscuro); 4 falta -ReplaceUserDatabase (o -Rehearsal); 5 se nego
# (proceso abierto, proyecto abierto en un Editor, archivo en uso o resguardo que no coincide); 6 la restauracion no coincide
# (resguardo conservado); 7 no se pudo armar la base sintetica; 8 fallo la captura o, en el ensayo, la comprobacion del
# reemplazo o el arranque de capture-window.ps1 (la base igual se restauro); 12 no encuentra Unity.exe o el build.
param(
    [switch]$ReplaceUserDatabase,
    [switch]$Rehearsal,
    [string]$Exe = 'Builds/Windows/normal/RowMate.exe',
    [string]$Out = 'docs/migration/iterations/03-rutinas/snapshots/CAP-27-ventana-build-normal.png',
    [string]$Unity = '',
    [int]$WaitSeconds = 8
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$names = @('rower_app.db', 'rower_app.db-wal', 'rower_app.db-shm')
$devName = 'rower_app_dev.db'

function Get-FullPath([string]$Path) {
    $p = $Path -replace '/', '\'
    if (-not [System.IO.Path]::IsPathRooted($p)) { $p = Join-Path $repo $p }
    return [System.IO.Path]::GetFullPath($p)
}

function Resolve-Unity {
    if ($Unity -ne '') { return $Unity }
    if ($null -ne $env:ROWMATE_UNITY -and $env:ROWMATE_UNITY -ne '') { return $env:ROWMATE_UNITY }
    $versionFile = Join-Path $repo 'ProjectSettings\ProjectVersion.txt'
    $match = [regex]::Match([System.IO.File]::ReadAllText($versionFile), 'm_EditorVersion:\s*(\S+)')
    if (-not $match.Success) { throw "no se pudo leer m_EditorVersion de $versionFile" }
    return 'C:\Program Files\Unity\Hub\Editor\' + $match.Groups[1].Value + '\Editor\Unity.exe'
}

function Get-Hash([string]$Path) {
    return (Get-FileHash -Algorithm SHA256 -Path $Path).Hash
}

# Descripcion de un archivo (o $null si no existe): tamano, SHA-256 y fecha.
function Get-Facts([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $item = Get-Item -LiteralPath $Path
    return [ordered]@{ size = $item.Length; sha256 = (Get-Hash $Path); lastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o') }
}

function Test-Unlocked([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $true }
    try {
        $stream = [System.IO.File]::Open($Path, 'Open', 'ReadWrite', 'None')
        $stream.Close()
        return $true
    }
    catch {
        return $false
    }
}

# Este proyecto abierto en un Editor: Temp/UnityLockfile existe y esta tomado (como run-unity-tests.ps1 y build.ps1).
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

# Un Editor de Unity es ...\Editor\Unity.exe (las versiones de la Hub y las instaladas a mano). El unity.exe de la Hub vive en
# la carpeta de la Hub y no es un Editor. Sin ruta legible: cuenta como Editor (QA-V3-01).
function Test-UnityEditorPath([string]$Path) {
    if ($null -eq $Path -or $Path -eq '') { return $true }
    return ($Path -match '[\\/]Editor[\\/]Unity\.exe$')
}

# Los procesos que podrian tener abierta la base del build normal: RowMate y los Editores de Unity (por la ruta, no por el
# nombre: -eq no distingue mayusculas y el "unity.exe serve" de la Hub tambien se llama unity).
function Get-OpenProcesses {
    $found = @()
    foreach ($process in @(Get-Process -ErrorAction SilentlyContinue)) {
        if ($process.ProcessName -eq 'RowMate') {
            $found += ('RowMate (' + $process.Id + ')')
        }
        elseif ($process.ProcessName -eq 'Unity') {
            $path = $null
            try { $path = $process.Path } catch { $path = $null }
            if (Test-UnityEditorPath $path) { $found += ('Editor de Unity (' + $process.Id + ')') }
        }
    }
    return $found
}

# Paso 2: lo que haria negarse a la toma ('' = nada).
function Get-Refusal {
    $open = @(Get-OpenProcesses)
    if ($open.Count -gt 0) { return ('hay un proceso que podria tener la base abierta: ' + ($open -join ', ')) }
    foreach ($name in $names) {
        if (-not (Test-Unlocked (Join-Path $userFolder $name))) { return ($name + ' esta en uso') }
    }
    return ''
}

# Se niega o, en el ensayo, lo informa y sigue. Este proyecto abierto en un Editor frena tambien el ensayo: la semilla
# necesita Unity (una sola instancia por proyecto).
function Test-MayProceed([string]$When) {
    if (Test-ProjectLocked) {
        Write-Host ('[cap27] se niega (' + $When + '): este proyecto esta abierto en un Editor (Temp/UnityLockfile tomado). Cerralo y volve a correr.')
        return $false
    }
    $reason = Get-Refusal
    if ($reason -eq '') { return $true }
    if ($Rehearsal) {
        Write-Host ('[cap27] ensayo (' + $When + '): la toma real se negaria: ' + $reason + '. El ensayo sigue: no usa esa carpeta.')
        return $true
    }
    Write-Host ('[cap27] se niega (' + $When + '): ' + $reason + '. Cerralo y volve a correr.')
    return $false
}

# Las 16 primeras letras de una base SQLite: "SQLite format 3" y un 0.
function Test-SqliteFile([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $bytes = New-Object byte[] 16
    $stream = [System.IO.File]::OpenRead($Path)
    try { $read = $stream.Read($bytes, 0, 16) } finally { $stream.Close() }
    return ($read -eq 16 -and [System.Text.Encoding]::ASCII.GetString($bytes, 0, 15) -eq 'SQLite format 3' -and $bytes[15] -eq 0)
}

# Ensayo: archivos de prueba en lugar de los del usuario. rower_app.db y -wal con texto cualquiera y una fecha vieja con
# fraccion de segundo (para ver que vuelve exacta); sin -shm (el "build" lo deja y la restauracion lo tiene que sacar); y
# rower_app_dev.db, que no se toca.
function New-RehearsalData {
    New-Item -ItemType Directory -Force -Path $userFolder | Out-Null
    $old = (New-Object DateTime 2020, 1, 2, 3, 4, 5, ([DateTimeKind]::Utc)).AddTicks(6789012)
    foreach ($name in @('rower_app.db', 'rower_app.db-wal', $devName)) {
        $path = Join-Path $userFolder $name
        [System.IO.File]::WriteAllText($path, ('RowMate CAP-27, ensayo: ' + $name + ' de prueba (no es una base).' + "`n"))
        (Get-Item -LiteralPath $path).LastWriteTimeUtc = $old
    }
}

# Cierra lo que haya quedado abierto del build despues de la captura (capture-window.ps1 ya lo cierra): solo procesos RowMate
# con la ruta del build que arrancaron despues que este script. Nunca uno que ya estaba.
function Stop-OwnBuild {
    foreach ($process in @(Get-Process -Name 'RowMate' -ErrorAction SilentlyContinue)) {
        $path = $null
        $start = $null
        try { $path = $process.Path; $start = $process.StartTime } catch { $path = $null }
        if ($null -ne $path -and $path -eq $exePath -and $null -ne $start -and $start -ge $started) {
            try { $process.Kill(); $process.WaitForExit(10000) | Out-Null } catch { }
        }
    }
}

# Borra un archivo; si todavia esta abierto (el build cerrando), reintenta 5 s.
function Remove-Quietly([string]$Path) {
    for ($i = 0; $i -lt 20; $i++) {
        if (-not (Test-Path -LiteralPath $Path)) { return }
        try { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop; return } catch { Start-Sleep -Milliseconds 250 }
    }
}

# La UNICA llamada a capture-window.ps1 (QA-V3-11): la toma real la hace con el build normal y el ensayo con un ejecutable de
# prueba sin ventana, con los mismos parametros. Sin -Arguments, como siempre. Las lineas salen en la consola y vuelven con
# el codigo (solo la salida estandar: un error de PowerShell sale directo a la consola y no corta este script).
function Invoke-WindowCapture([string]$Target, [string]$Destination) {
    $capture = Join-Path $PSScriptRoot 'capture-window.ps1'
    $lines = @(& powershell -NoProfile -File $capture -Exe $Target -Out $Destination -WaitSeconds $WaitSeconds -ClickRelative '0.625,0.98')
    $code = $LASTEXITCODE
    foreach ($line in $lines) { Write-Host $line }
    return [pscustomobject]@{ Code = $code; Lines = $lines }
}

# 0. Solo a pedido.
if (-not $ReplaceUserDatabase -and -not $Rehearsal) {
    Write-Host '[cap27] sin -ReplaceUserDatabase no se toca nada. Con el switch, este script:'
    Write-Host '  - comprueba que Windows ya este en modo Oscuro (si no: NO VERIFICABLE, sale con 3) y que no haya RowMate ni un Editor de Unity abiertos;'
    Write-Host '  - resguarda y verifica rower_app.db, -wal y -shm de la carpeta de datos del build normal;'
    Write-Host '  - los reemplaza por una base sintetica (solo Escalera SPM), captura la ventana y restaura todo en finally, verificando.'
    Write-Host '  Con -Rehearsal hace lo mismo sobre una carpeta temporal de prueba, sin abrir el build: no toca nada del usuario.'
    exit 4
}

$exePath = ''
if (-not $Rehearsal) {
    # 0 (b). El tema del usuario: nunca se cambia. (El ensayo no captura: no lo pide.)
    $personalize = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize'
    $light = $null
    try { $light = (Get-ItemProperty -Path $personalize -Name 'AppsUseLightTheme' -ErrorAction Stop).AppsUseLightTheme } catch { $light = $null }
    if ($light -ne 0) {
        Write-Host '[cap27] NO VERIFICABLE: Windows no esta en modo Oscuro para las apps (AppsUseLightTheme != 0). No se toca nada.'
        exit 3
    }

    $exePath = Get-FullPath $Exe
    if (-not (Test-Path $exePath)) {
        Write-Host ('[cap27] no existe el build normal: ' + $Exe + ' (tools/build/build.ps1)')
        exit 12
    }
}
$unityExe = Resolve-Unity
if (-not (Test-Path $unityExe)) {
    Write-Host '[cap27] no se encuentra Unity.exe (usar -Unity o ROWMATE_UNITY)'
    exit 12
}

$started = Get-Date
$stamp = $started.ToString('yyyyMMdd-HHmmss')
$work = Join-Path ([System.IO.Path]::GetTempPath()) ('RowMate.Cap27.' + $stamp)
$seedFolder = Join-Path $work 'semilla'
$backupFolder = Join-Path $work 'resguardo'

# La carpeta de datos del build normal (AC-HIS-17-01): se calcula, nunca se escribe en el script. En el ensayo, una carpeta
# temporal con archivos de prueba: la del usuario ni se nombra.
if ($Rehearsal) {
    $userFolder = Join-Path $work 'ensayo-datos'
    Write-Host '[cap27] ENSAYO: carpeta temporal de prueba en lugar de la del build normal; no se abre ningun build.'
}
else {
    $userFolder = Join-Path $env:USERPROFILE 'AppData\LocalLow\iguisoft\RowMate'
}

# 2. Negativas, antes de sembrar: nada abierto y ningun archivo en uso.
if (-not (Test-MayProceed 'antes de sembrar')) { exit 5 }
if ($Rehearsal) { New-RehearsalData }

# 1. Base sintetica en una carpeta temporal, con un nombre neutro (QA-V3-01).
New-Item -ItemType Directory -Force -Path $seedFolder | Out-Null
$seedDb = Join-Path $seedFolder 'cap27.db'
$seedLog = Join-Path $work 'seed.log'
$info = New-Object System.Diagnostics.ProcessStartInfo
$info.FileName = $unityExe
$info.Arguments = '-batchmode -quit -projectPath "' + $repo + '" -executeMethod RowMate.Snapshots.SnapshotCli.SeedDatabase -snapshotSeedDb "' + $seedDb + '" -seed Cap27 -logFile "' + $seedLog + '"'
$info.UseShellExecute = $false
# Como run-unity-tests.ps1 y capture-snapshots.ps1: si algo del Editor buscara la carpeta de datos, encontraria una temporal.
$info.EnvironmentVariables['ROWMATE_DATA_DIR'] = (Join-Path $work 'datos-unity')
$seedProcess = [System.Diagnostics.Process]::Start($info)
$seedProcess.WaitForExit()
if ($seedProcess.ExitCode -ne 0 -or -not (Test-SqliteFile $seedDb)) {
    Write-Host ('[cap27] no se pudo armar la base sintetica (Unity -> ' + $seedProcess.ExitCode + '; registro: ' + $seedLog + '). No se toco nada.')
    exit 7
}
$seedHash = Get-Hash $seedDb
Write-Host '[cap27] base sintetica lista (Escalera SPM).'

# 2 (otra vez). Justo antes de tocar: mientras Unity sembraba se pudo abrir algo. Todavia no se toco nada: se va la semilla.
if (-not (Test-MayProceed 'antes del resguardo')) {
    Remove-Item -Recurse -Force -Path $work -ErrorAction SilentlyContinue
    exit 5
}

# 3. Resguardo verificado ANTES de tocar.
New-Item -ItemType Directory -Force -Path $backupFolder | Out-Null
$facts = [ordered]@{}
foreach ($name in $names) {
    $source = Join-Path $userFolder $name
    $facts[$name] = Get-Facts $source
    if ($null -ne $facts[$name]) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $backupFolder $name) -Force
        if ((Get-Hash (Join-Path $backupFolder $name)) -ne $facts[$name].sha256) {
            Write-Host ('[cap27] se niega: la copia de resguardo de ' + $name + ' no coincide. No se toco nada.')
            exit 5
        }
    }
}
$devFacts = Get-Facts (Join-Path $userFolder $devName)
$record = [ordered]@{ files = $facts; dev = $devFacts }
[System.IO.File]::WriteAllText((Join-Path $backupFolder 'resguardo.json'), ($record | ConvertTo-Json -Depth 4))

$exitCode = 0
try {
    # 4. Reemplazo; captura y cierre (en el ensayo, la comprobacion del reemplazo y lo que dejaria el build).
    New-Item -ItemType Directory -Force -Path $userFolder | Out-Null
    foreach ($name in $names) { Remove-Item -LiteralPath (Join-Path $userFolder $name) -Force -ErrorAction SilentlyContinue }
    Copy-Item -LiteralPath $seedDb -Destination (Join-Path $userFolder 'rower_app.db') -Force
    if ($Rehearsal) {
        $placed = Join-Path $userFolder 'rower_app.db'
        if ((Get-Hash $placed) -ne $seedHash -or (Test-Path -LiteralPath ($placed + '-wal')) -or (Test-Path -LiteralPath ($placed + '-shm'))) {
            Write-Host '[cap27] ensayo: el reemplazo no quedo bien (rower_app.db distinto de la semilla, o con -wal o -shm).'
            $exitCode = 8
        }
        else {
            Write-Host '[cap27] ensayo: la base sintetica quedo como rower_app.db (mismo SHA-256 que la semilla), sin -wal ni -shm.'
        }
        # El arranque de capture-window.ps1 de verdad, con la llamada de la toma real, contra un ejecutable de prueba sin
        # ventana en lugar del build: 1 y "el build no abrio ninguna ventana", sin clic ni captura (QA-V3-11).
        $probe = Join-Path $work 'sin-ventana.exe'
        $probeShot = Join-Path $work 'sin-ventana.png'
        & powershell -NoProfile -File (Join-Path $PSScriptRoot 'new-windowless-exe.ps1') -Out $probe
        if ($LASTEXITCODE -ne 0) {
            Write-Host '[cap27] ensayo: no se pudo armar el ejecutable de prueba sin ventana (new-windowless-exe.ps1).'
            $exitCode = 8
        }
        else {
            $shot = Invoke-WindowCapture $probe $probeShot
            $noWindow = @($shot.Lines | Where-Object { $_ -like '*el build no abrio ninguna ventana*' }).Count -gt 0
            if ($shot.Code -eq 1 -and $noWindow -and -not (Test-Path -LiteralPath $probeShot)) {
                Write-Host '[cap27] ensayo: capture-window.ps1 arranca con la llamada de la toma real (sin ventana: 1, sin clic ni captura).'
            }
            else {
                Write-Host ('[cap27] ensayo: el arranque de capture-window.ps1 no dio "el build no abrio ninguna ventana" (-> ' + $shot.Code + ').')
                $exitCode = 8
            }
        }
        [System.IO.File]::WriteAllText(($placed + '-wal'), "wal que dejaria el build`n")
        [System.IO.File]::WriteAllText(($placed + '-shm'), "shm que dejaria el build`n")
    }
    else {
        $outPath = Get-FullPath $Out
        $shot = Invoke-WindowCapture $exePath $outPath
        if ($shot.Code -ne 0) {
            Write-Host ('[cap27] la captura fallo (capture-window.ps1 -> ' + $shot.Code + ').')
            $exitCode = 8
        }
        else {
            Write-Host '[cap27] captura lista (CAP-27).'
        }
    }
}
finally {
    # 5. Restauracion y verificacion, pase lo que pase.
    if (-not $Rehearsal) { Stop-OwnBuild }
    foreach ($name in $names) { Remove-Quietly (Join-Path $userFolder $name) }
    $mismatch = @()
    foreach ($name in $names) {
        if ($null -ne $facts[$name]) {
            $target = Join-Path $userFolder $name
            try {
                Copy-Item -LiteralPath (Join-Path $backupFolder $name) -Destination $target -Force
                # RoundtripKind: la fecha vuelve en UTC, exacta (sin pasar por la hora local).
                $when = [DateTime]::Parse($facts[$name].lastWriteTimeUtc, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)
                (Get-Item -LiteralPath $target).LastWriteTimeUtc = $when
            }
            catch {
                $mismatch += ($name + ' no se pudo volver a copiar')
            }
        }
    }
    foreach ($name in $names) {
        $now = Get-Facts (Join-Path $userFolder $name)
        $was = $facts[$name]
        if ($null -eq $was -and $null -ne $now) { $mismatch += ($name + ' no existia y quedo') }
        elseif ($null -ne $was -and ($null -eq $now -or $now.sha256 -ne $was.sha256 -or $now.lastWriteTimeUtc -ne $was.lastWriteTimeUtc)) { $mismatch += ($name + ' no coincide') }
    }
    $devNow = Get-Facts (Join-Path $userFolder $devName)
    if (($null -eq $devFacts) -ne ($null -eq $devNow) -or ($null -ne $devFacts -and $devNow.sha256 -ne $devFacts.sha256)) { $mismatch += ($devName + ' cambio') }
    if ($mismatch.Count -gt 0) {
        Write-Host ('[cap27] ATENCION: la restauracion no coincide: ' + ($mismatch -join '; '))
        Write-Host ('[cap27] El resguardo queda en ' + $backupFolder + ' (con resguardo.json).')
        Write-Host ('[cap27] Para recuperar: cerra RowMate y copia cada archivo de esa carpeta a ' + $userFolder)
        $exitCode = 6
    }
    else {
        Remove-Item -Recurse -Force -Path $work -ErrorAction SilentlyContinue
        if ($Rehearsal) {
            Write-Host '[cap27] ensayo: restaurado y verificado (mismos archivos, SHA-256 y fecha; el -shm que dejo el "build" se fue; rower_app_dev.db igual).'
        }
        else {
            Write-Host '[cap27] restaurado y verificado: la base del build normal quedo como estaba.'
        }
    }
}
exit $exitCode
