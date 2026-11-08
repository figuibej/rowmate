# Verifica el plugin de Android (Kotlin) SIN el modulo "Android Build Support" de Unity (10a 02-tech-design Sec. 4.5).
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
#
#   native/android/check.ps1 [-Golden] [-Update] [-OutDir <carpeta>]
#
# 1. Busca las herramientas sin rutas fijas: Android Studio en $env:ANDROID_STUDIO o "$env:ProgramFiles\Android\Android Studio"
#    (su JBR y el kotlinc del plugin de Kotlin; $env:KOTLIN_HOME manda si esta), y el SDK en $env:ANDROID_HOME,
#    $env:ANDROID_SDK_ROOT o "$env:LOCALAPPDATA\Android\Sdk" (el android.jar de la plataforma mas nueva).
# 2. Revisa las fuentes Kotlin (Assets/Plugins/Android/RowMateBle): sin emparejar ni desemparejar, sin cambiar la radio y sin
#    UnityPlayer (AC-BLE-SEC-01, DT10-11). Codigo 3 si aparece algo.
# 3. Compila TODO el Kotlin contra android.jar (con -Werror) en -OutDir (por defecto, una carpeta temporal del sistema).
# 4. javap -s -public de com.rowmate.ble.RmBle -> "nombre descriptor" ordenados; lo compara con
#    native/android/golden/jni-signatures.txt y con la tabla de Core/Ble/Native/RmBleJniSignatures.cs. Codigo 4 si difiere
#    (QA-G10a-05). -Update reescribe jni-signatures.txt con lo de javap (solo si cambia la API, junto con la tabla de C#).
# 5. -Golden: compila Wire.kt + EventRing.kt + golden/EmitFixture.kt para la JVM, lo corre y compara su salida byte a byte con
#    Assets/RowMate/Tests/Golden/ble/events-v1.bin. Codigo 5 si difiere.
# Codigo 1: faltan herramientas o no compila. 0: todo bien. No instala nada ni toca variables del sistema (JAVA_HOME solo
# en este proceso). El APK, los permisos en el telefono y el segundo plano quedan NO VERIFICABLE hasta el modulo de Unity.
param(
    [switch]$Golden,
    [switch]$Update,
    [string]$OutDir = ''
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sources = Join-Path $repo 'Assets\Plugins\Android\RowMateBle'
$goldenDir = Join-Path $PSScriptRoot 'golden'
$signaturesFile = Join-Path $goldenDir 'jni-signatures.txt'
$tableFile = Join-Path $repo 'Assets\RowMate\Core\Ble\Native\RmBleJniSignatures.cs'
$eventsGolden = Join-Path $repo 'Assets\RowMate\Tests\Golden\ble\events-v1.bin'

function Fail([int]$Code, [string]$Message) {
    Write-Host ('check.ps1: ' + $Message)
    exit $Code
}

function First-Existing([string[]]$Candidates) {
    foreach ($c in $Candidates) {
        if ($c -ne $null -and $c -ne '' -and (Test-Path -LiteralPath $c)) { return $c }
    }
    return $null
}

# ---- 1. Herramientas ----
$studio = First-Existing @($env:ANDROID_STUDIO, (Join-Path $env:ProgramFiles 'Android\Android Studio'))
if ($null -eq $studio) { Fail 1 'no se encontro Android Studio ($env:ANDROID_STUDIO o Program Files\Android\Android Studio)' }
$jbr = First-Existing @((Join-Path $studio 'jbr'), $env:JAVA_HOME)
if ($null -eq $jbr) { Fail 1 'no se encontro el JBR de Android Studio' }
$java = Join-Path $jbr 'bin\java.exe'
$javap = Join-Path $jbr 'bin\javap.exe'
if (-not (Test-Path -LiteralPath $javap)) { Fail 1 'el JDK no tiene javap' }
$kotlinHome = First-Existing @($env:KOTLIN_HOME, (Join-Path $studio 'plugins\Kotlin\kotlinc'))
if ($null -eq $kotlinHome) { Fail 1 'no se encontro kotlinc (plugins\Kotlin\kotlinc de Android Studio o $env:KOTLIN_HOME)' }
$compilerJar = Join-Path $kotlinHome 'lib\kotlin-compiler.jar'
$stdlibJar = Join-Path $kotlinHome 'lib\kotlin-stdlib.jar'
if (-not (Test-Path -LiteralPath $compilerJar)) { Fail 1 'falta lib\kotlin-compiler.jar en kotlinc' }

$sdk = First-Existing @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, (Join-Path $env:LOCALAPPDATA 'Android\Sdk'))
if ($null -eq $sdk) { Fail 1 'no se encontro el Android SDK ($env:ANDROID_HOME o LOCALAPPDATA\Android\Sdk)' }
$platform = Get-ChildItem -LiteralPath (Join-Path $sdk 'platforms') -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '^android-(\d+)$' -and (Test-Path -LiteralPath (Join-Path $_.FullName 'android.jar')) } |
    Sort-Object { [int]($_.Name -replace '^android-', '') } -Descending | Select-Object -First 1
if ($null -eq $platform) { Fail 1 'el SDK no tiene ninguna plataforma con android.jar' }
$androidJar = Join-Path $platform.FullName 'android.jar'

if ($OutDir -eq '') { $OutDir = Join-Path ([System.IO.Path]::GetTempPath()) 'rowmate-android-check' }
if (Test-Path -LiteralPath $OutDir) { Remove-Item -LiteralPath $OutDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$env:JAVA_HOME = $jbr

Write-Host ('check.ps1: ' + $platform.Name + ', kotlinc ' + ((Get-Content -LiteralPath (Join-Path $kotlinHome 'build.txt') -ErrorAction SilentlyContinue) -join ''))

# ---- 2. Higiene de las fuentes ----
$kt = @(Get-ChildItem -LiteralPath $sources -Filter '*.kt' -File | Sort-Object Name)
if ($kt.Count -eq 0) { Fail 1 'no hay fuentes Kotlin en Assets/Plugins/Android/RowMateBle' }
$forbidden = @('createBond', 'removeBond', 'cancelBondProcess', 'setPairingConfirmation', '.enable()', '.disable()', 'UnityPlayer', 'com.unity3d')
foreach ($f in $kt) {
    $text = [System.IO.File]::ReadAllText($f.FullName)
    foreach ($p in $forbidden) {
        if ($text.Contains($p)) { Fail 3 ($f.Name + " contiene '" + $p + "'") }
    }
}

# Compila con el compilador de Kotlin en la JVM del JBR (sin el .bat: rutas con espacios) y un archivo de argumentos.
function Invoke-Kotlinc([string[]]$KotlinArgs, [string]$Name) {
    $argFile = Join-Path $OutDir ($Name + '.args')
    $lines = @()
    foreach ($a in $KotlinArgs) { $lines += ('"' + ($a -replace '\\', '/') + '"') }
    [System.IO.File]::WriteAllLines($argFile, $lines)
    & $java ('-Dkotlin.home=' + $kotlinHome) -cp $compilerJar org.jetbrains.kotlin.cli.jvm.K2JVMCompiler ('@' + $argFile)
    return $LASTEXITCODE
}

# ---- 3. Compilacion contra android.jar ----
$classes = Join-Path $OutDir 'classes'
$compileArgs = @('-classpath', $androidJar, '-jvm-target', '17', '-Werror', '-d', $classes) + @($kt | ForEach-Object { $_.FullName })
$code = Invoke-Kotlinc $compileArgs 'android'
if ($code -ne 0) { Fail 1 ('kotlinc fallo (codigo ' + $code + ')') }
Write-Host ('check.ps1: ' + $kt.Count + ' fuentes Kotlin compiladas contra ' + $platform.Name)

# ---- 4. Firmas JNI ----
$javapOut = & $javap -s -public -cp $classes com.rowmate.ble.RmBle
if ($LASTEXITCODE -ne 0) { Fail 1 'javap fallo' }
$fromJavap = New-Object System.Collections.Generic.List[string]
$pendingName = $null
foreach ($line in $javapOut) {
    $t = $line.Trim()
    $m = [regex]::Match($t, '^public .*?\s([A-Za-z_][A-Za-z0-9_$]*)\(.*\);$')
    if ($m.Success) { $pendingName = $m.Groups[1].Value; continue }
    $d = [regex]::Match($t, '^descriptor: (\(.*)$')
    if ($d.Success -and $null -ne $pendingName) {
        if ($pendingName -notmatch '[$<]') { $fromJavap.Add($pendingName + ' ' + $d.Groups[1].Value) }
    }
    if (-not $t.StartsWith('public') ) { $pendingName = $null }
}
$fromJavap.Sort([System.StringComparer]::Ordinal)

if ($Update) {
    $header = @('# Firmas JNI de com.rowmate.ble.RmBle (nombre y descriptor, orden ordinal). Las genera native/android/check.ps1 -Update', '# con javap -s; JniSignatureTests las compara con Core/Ble/Native/RmBleJniSignatures.cs.')
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($signaturesFile, (($header + $fromJavap) -join "`n") + "`n", $utf8)
    Write-Host ('check.ps1: jni-signatures.txt reescrito (' + $fromJavap.Count + ' metodos)')
}

$goldenLines = New-Object System.Collections.Generic.List[string]
if (Test-Path -LiteralPath $signaturesFile) {
    foreach ($line in [System.IO.File]::ReadAllLines($signaturesFile)) {
        $t = $line.Trim()
        if ($t -ne '' -and -not $t.StartsWith('#')) { $goldenLines.Add($t) }
    }
}
$goldenLines.Sort([System.StringComparer]::Ordinal)

$table = New-Object System.Collections.Generic.List[string]
$tableText = [System.IO.File]::ReadAllText($tableFile)
foreach ($m in [regex]::Matches($tableText, 'Name\s*=\s*"([^"]+)";\s*public const string Signature\s*=\s*"([^"]+)";')) {
    $table.Add($m.Groups[1].Value + ' ' + $m.Groups[2].Value)
}
$table.Sort([System.StringComparer]::Ordinal)

$javapText = $fromJavap -join "`n"
if ($javapText -ne ($goldenLines -join "`n")) {
    Write-Host 'javap:'; $fromJavap | ForEach-Object { Write-Host ('  ' + $_) }
    Write-Host 'jni-signatures.txt:'; $goldenLines | ForEach-Object { Write-Host ('  ' + $_) }
    Fail 4 'las firmas de RmBle no coinciden con native/android/golden/jni-signatures.txt'
}
if ($javapText -ne ($table -join "`n")) {
    Write-Host 'RmBleJniSignatures.cs:'; $table | ForEach-Object { Write-Host ('  ' + $_) }
    Fail 4 'las firmas de RmBle no coinciden con Core/Ble/Native/RmBleJniSignatures.cs'
}
Write-Host ('check.ps1: firmas JNI: ' + $fromJavap.Count + ' metodos, iguales a jni-signatures.txt y a RmBleJniSignatures.cs')

# ---- 5. Fijacion del formato en la JVM ----
if ($Golden) {
    $jvmClasses = Join-Path $OutDir 'jvm'
    $jvmArgs = @('-jvm-target', '17', '-Werror', '-d', $jvmClasses, (Join-Path $sources 'Wire.kt'), (Join-Path $sources 'EventRing.kt'), (Join-Path $goldenDir 'EmitFixture.kt'))
    $code = Invoke-Kotlinc $jvmArgs 'jvm'
    if ($code -ne 0) { Fail 1 ('kotlinc (JVM) fallo (codigo ' + $code + ')') }
    $bin = Join-Path $OutDir 'events-v1.kotlin.bin'
    & $java -cp ($jvmClasses + ';' + $stdlibJar) com.rowmate.ble.EmitFixtureKt $bin
    if ($LASTEXITCODE -ne 0) { Fail 5 ('EmitFixture fallo (codigo ' + $LASTEXITCODE + ')') }
    $a = [System.IO.File]::ReadAllBytes($bin)
    $b = [System.IO.File]::ReadAllBytes($eventsGolden)
    $same = $a.Length -eq $b.Length
    if ($same) { for ($i = 0; $i -lt $a.Length; $i++) { if ($a[$i] -ne $b[$i]) { $same = $false; break } } }
    if (-not $same) { Fail 5 ('Kotlin escribe ' + $a.Length + ' bytes distintos de events-v1.bin (' + $b.Length + ')') }
    Write-Host ('check.ps1: golden: Kotlin escribe events-v1.bin identico (' + $a.Length + ' bytes)')
}

Write-Host 'check.ps1: OK'
exit 0
