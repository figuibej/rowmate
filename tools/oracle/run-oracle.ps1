# Oraculo Dart (02-tech-design Sec. 6.1): genera los vectores dorados importando el legacy en SOLO LECTURA.
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
# Compatible con Windows PowerShell 5.1 y pwsh 7 (sin los operadores null-conditional, null-coalescing ni pipeline chain de PS 7).
#
#   tools/oracle/run-oracle.ps1 [-LegacyPath <ruta>] [-DartExe <ruta>] [-Only <nombre>[,...]] [-L10n]
#
# Pasos: guardia R-02 (ningun script menciona el modulo de credenciales del legacy), guardia del legacy ANTES
# (status --porcelain vacio, listado --ignored archivo por archivo e instantanea de metadatos sin abrir archivos),
# package_config propio en tools/oracle/.dart_tool/, cada script de bin/ (o los de -Only), y la guardia DESPUES
# (tambien si un script falla). Nunca corre pub get ni escribe nada dentro del legacy.
param(
    [string]$LegacyPath = "",
    [string]$DartExe = "",
    [string[]]$Only = @(),
    [switch]$L10n
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ($LegacyPath -eq '') { $LegacyPath = Join-Path $repo '..\rowmate-legacy' }
$legacy = (Resolve-Path $LegacyPath).Path

if ($DartExe -eq '') {
    if ($null -ne $env:ROWMATE_DART -and $env:ROWMATE_DART -ne '') {
        $DartExe = $env:ROWMATE_DART
    } else {
        # El Dart que trae el SDK de Flutter (el del legacy) y, si no, cualquier dart del PATH.
        $flutter = Get-Command flutter -ErrorAction SilentlyContinue
        $bundled = ''
        if ($null -ne $flutter) { $bundled = Join-Path (Split-Path $flutter.Source -Parent) 'cache\dart-sdk\bin\dart.exe' }
        if ($bundled -ne '' -and (Test-Path $bundled)) {
            $DartExe = $bundled
        } else {
            $cmd = Get-Command dart -ErrorAction SilentlyContinue
            if ($null -eq $cmd) { Write-Host '[oracle] no encuentro dart: usa -DartExe o ROWMATE_DART'; exit 2 }
            $DartExe = $cmd.Source
        }
    }
}

$outDir = Join-Path $repo 'Assets\RowMate\Tests\Golden\oracle'
$binDir = Join-Path $PSScriptRoot 'bin'
$l10nScript = Join-Path $repo 'tools\l10n\arb_to_catalog.dart'

# --- Guardia R-02: ningun script del oraculo ni de l10n menciona el modulo de credenciales del legacy ---
$forbidden = 'str' + 'ava'
$scripts = @(Get-ChildItem -Path $PSScriptRoot -Recurse -File -Filter '*.dart' | Where-Object { $_.FullName -notmatch '\\\.dart_tool\\' })
if (Test-Path (Join-Path $repo 'tools\l10n')) {
    $scripts += @(Get-ChildItem -Path (Join-Path $repo 'tools\l10n') -File -Filter '*.dart')
}
foreach ($s in $scripts) {
    $text = [System.IO.File]::ReadAllText($s.FullName)
    if ($text.IndexOf($forbidden, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        Write-Host ('[oracle] ABORTA (R-02): ' + $s.FullName + ' menciona un nombre prohibido')
        exit 3
    }
}

function Invoke-Git([string[]]$GitArgs) {
    $all = @('--no-optional-locks', '-C', $legacy) + $GitArgs
    $out = & git @all
    if ($LASTEXITCODE -ne 0) { throw ('git ' + ($GitArgs -join ' ') + ' fallo con ' + $LASTEXITCODE) }
    return ($out -join "`n")
}

# Instantanea de metadatos del arbol de trabajo del legacy (fuera de .git): ruta, tamano y fecha.
# Get-ChildItem lee solo las entradas de directorio: no abre ningun archivo.
function Get-LegacySnapshot {
    $prefix = $legacy.TrimEnd('\') + '\'
    $items = @(Get-ChildItem -LiteralPath $legacy -Force | Where-Object { $_.Name -ne '.git' })
    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($item in $items) {
        if ($item.PSIsContainer) {
            $files = @(Get-ChildItem -LiteralPath $item.FullName -Recurse -File -Force)
        } else {
            $files = @($item)
        }
        foreach ($f in $files) {
            $rel = $f.FullName.Substring($prefix.Length)
            $lines.Add($rel + '|' + $f.Length + '|' + $f.LastWriteTimeUtc.Ticks)
        }
    }
    $sorted = $lines.ToArray()
    [System.Array]::Sort($sorted, [System.StringComparer]::Ordinal)
    return $sorted
}

function Compare-Lines([string[]]$Before, [string[]]$After, [string]$Label) {
    $diff = @(Compare-Object -ReferenceObject $Before -DifferenceObject $After)
    if ($diff.Count -gt 0) {
        Write-Host ('[oracle] CAMBIO en ' + $Label + ':')
        foreach ($d in $diff) { Write-Host ('  ' + $d.SideIndicator + ' ' + $d.InputObject) }
        return $false
    }
    return $true
}

# --- Guardia del legacy, ANTES ---
$porcelain = Invoke-Git @('status', '--porcelain')
if ($porcelain.Trim() -ne '') {
    Write-Host '[oracle] ABORTA: el legacy no esta limpio (git status --porcelain):'
    Write-Host $porcelain
    exit 4
}
$ignoredBefore = (Invoke-Git @('status', '--porcelain', '--ignored', '--untracked-files=all')) -split "`n"
$snapshotBefore = Get-LegacySnapshot
$legacyCommit = (Invoke-Git @('rev-parse', 'HEAD')).Trim()
$summary = '[oracle] legacy ' + $legacy + ' @ ' + $legacyCommit + ' (' + $snapshotBefore.Count + ' archivos fuera de .git, '
$summary = $summary + $ignoredBefore.Count + ' lineas --ignored)'
Write-Host $summary

# --- package_config propio (sin pubspec ni pub get) ---
$dartTool = Join-Path $PSScriptRoot '.dart_tool'
New-Item -ItemType Directory -Force -Path $dartTool | Out-Null
$config = Join-Path $dartTool 'oracle_package_config.json'
$rootUri = 'file:///' + ($legacy -replace '\\', '/').TrimEnd('/') + '/'
$json = '{"configVersion": 2, "packages": [{"name": "rowmate", "rootUri": "' + $rootUri + '", "packageUri": "lib/", "languageVersion": "3.3"}]}'
[System.IO.File]::WriteAllText($config, $json + "`n", (New-Object System.Text.UTF8Encoding($false)))

$toRun = @()
if ($Only.Count -gt 0) {
    foreach ($name in $Only) {
        foreach ($part in ($name -split ',')) {
            $n = $part.Trim()
            if ($n -eq '') { continue }
            if (-not $n.EndsWith('.dart')) { $n = $n + '.dart' }
            $p = Join-Path $binDir $n
            if (-not (Test-Path $p)) { Write-Host ('[oracle] no existe ' + $p); exit 2 }
            $toRun += $p
        }
    }
} else {
    $toRun = @(Get-ChildItem -Path $binDir -File -Filter '*.dart' | Sort-Object Name | ForEach-Object { $_.FullName })
}

$failed = 0
try {
    foreach ($script in $toRun) {
        Write-Host ('[oracle] dart ' + (Split-Path $script -Leaf))
        & $DartExe "--packages=$config" $script --out $outDir --legacy-commit $legacyCommit
        if ($LASTEXITCODE -ne 0) {
            Write-Host ('[oracle] FALLO ' + $script + ' (' + $LASTEXITCODE + ')')
            $failed++
        }
    }
    if ($L10n) {
        if (-not (Test-Path $l10nScript)) {
            Write-Host ('[oracle] no existe ' + $l10nScript)
            $failed++
        } else {
            Write-Host '[oracle] dart arb_to_catalog.dart (conversion unica de catalogos)'
            & $DartExe "--packages=$config" $l10nScript --legacy $legacy --out (Join-Path $repo 'Assets\RowMate\Localization') --changes (Join-Path $repo 'tools\l10n\it01-changes.json')
            if ($LASTEXITCODE -ne 0) { Write-Host ('[oracle] FALLO arb_to_catalog (' + $LASTEXITCODE + ')'); $failed++ }
        }
    }
}
finally {
    # --- Guardia del legacy, DESPUES (corre aunque un script falle) ---
    $ok = $true
    $porcelainAfter = Invoke-Git @('status', '--porcelain')
    if ($porcelainAfter.Trim() -ne '') {
        Write-Host '[oracle] ERROR: el legacy quedo con cambios (git status --porcelain):'
        Write-Host $porcelainAfter
        $ok = $false
    }
    $ignoredAfter = (Invoke-Git @('status', '--porcelain', '--ignored', '--untracked-files=all')) -split "`n"
    if (-not (Compare-Lines $ignoredBefore $ignoredAfter 'git status --ignored del legacy')) { $ok = $false }
    $snapshotAfter = Get-LegacySnapshot
    if (-not (Compare-Lines $snapshotBefore $snapshotAfter 'metadatos de archivos del legacy')) { $ok = $false }
    if (-not $ok) {
        Write-Host '[oracle] la guardia del legacy FALLO'
        exit 1
    }
    Write-Host '[oracle] guardia del legacy OK: sin cambios (porcelain vacio, --ignored y metadatos iguales)'
}

if ($failed -gt 0) { exit 1 }
exit 0
