# Oraculo Dart (02-tech-design Sec. 6.1): genera los vectores dorados importando el legacy en SOLO LECTURA.
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
# Compatible con Windows PowerShell 5.1 y pwsh 7 (sin los operadores null-conditional, null-coalescing ni pipeline chain de PS 7).
#
#   tools/oracle/run-oracle.ps1 [-LegacyPath <ruta>] [-DartExe <ruta>] [-Only <nombre>[,...]] [-L10n]
#   tools/oracle/run-oracle.ps1 -Engine [-LegacyPath <ruta>] [-FlutterExe <ruta>]
#
# Pasos: guardia R-02 (ningun script menciona el modulo de credenciales del legacy), guardia del legacy ANTES
# (status --porcelain vacio, listado --ignored archivo por archivo e instantanea de metadatos sin abrir archivos),
# package_config propio en tools/oracle/.dart_tool/, cada script de bin/ (o los de -Only), y la guardia DESPUES
# (tambien si un script falla). Fuera de -Engine nunca corre pub get ni escribe nada dentro del legacy.
#
# -Engine (It. 4a, 02-tech-design Sec. 3.7): el oraculo del motor (tools/oracle/engine, un proyecto de Flutter que depende
# del legacy por ruta relativa). Comprueba antes que el .gitignore tenga las cinco lineas de tools/oracle/engine (si no,
# aborta: pub get deja ahi rutas absolutas de la PC), corre "flutter pub get --offline" y "flutter test" DENTRO de la misma
# guardia del legacy y deja Assets/RowMate/Tests/Golden/oracle/workout_engine.json. Al terminar, git status de
# tools/oracle/engine no puede mostrar nada fuera de pubspec.yaml y test/. Con -LegacyPath escribe
# tools/oracle/engine/pubspec_overrides.yaml (ignorado por git); sin -LegacyPath lo borra si quedo de antes. Solo corre el
# motor (los scripts de bin/, con -Only). -FlutterExe (o ROWMATE_FLUTTER) elige el flutter; si no, el del PATH.
param(
    [string]$LegacyPath = "",
    [string]$DartExe = "",
    [string[]]$Only = @(),
    [switch]$L10n,
    [switch]$Engine,
    [string]$FlutterExe = ""
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$legacyGiven = $LegacyPath -ne ''
if ($LegacyPath -eq '') { $LegacyPath = Join-Path $repo '..\rowmate-legacy' }
$legacy = (Resolve-Path $LegacyPath).Path
$runBin = (-not $Engine) -or ($Only.Count -gt 0)
$engineDir = Join-Path $PSScriptRoot 'engine'
$engineIgnores = @(
    '/tools/oracle/engine/.dart_tool/',
    '/tools/oracle/engine/build/',
    '/tools/oracle/engine/.flutter-plugins*',
    '/tools/oracle/engine/pubspec_overrides.yaml',
    '/tools/oracle/engine/pubspec.lock'
)

if ($Engine) {
    # QA-G4aT-05: las cinco lineas del .gitignore ANTES del primer pub get.
    $gitignore = @([System.IO.File]::ReadAllLines((Join-Path $repo '.gitignore')) | ForEach-Object { $_.Trim() })
    foreach ($line in $engineIgnores) {
        if ($gitignore -notcontains $line) {
            Write-Host ('[oracle] ABORTA: falta en .gitignore la linea ' + $line + ' (pub get deja rutas de la PC en tools/oracle/engine)')
            exit 5
        }
    }
    if ($FlutterExe -eq '') {
        if ($null -ne $env:ROWMATE_FLUTTER -and $env:ROWMATE_FLUTTER -ne '') {
            $FlutterExe = $env:ROWMATE_FLUTTER
        } else {
            $fcmd = Get-Command flutter -ErrorAction SilentlyContinue
            if ($null -eq $fcmd) { Write-Host '[oracle] no encuentro flutter: usa -FlutterExe o ROWMATE_FLUTTER'; exit 2 }
            $FlutterExe = $fcmd.Source
        }
    }
}

if ($DartExe -eq '' -and ($runBin -or $L10n)) {
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
$scripts = @(Get-ChildItem -Path $PSScriptRoot -Recurse -File -Filter '*.dart' | Where-Object { $_.FullName -notmatch '\\\.dart_tool\\' -and $_.FullName -notmatch '\\engine\\build\\' })
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
if (-not $runBin) {
    $toRun = @()
} elseif ($Only.Count -gt 0) {
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
    if ($Engine) {
        $overrides = Join-Path $engineDir 'pubspec_overrides.yaml'
        if ($legacyGiven) {
            $yaml = "dependency_overrides:`n  rowmate:`n    path: " + ($legacy -replace '\\', '/') + "`n"
            [System.IO.File]::WriteAllText($overrides, $yaml, (New-Object System.Text.UTF8Encoding($false)))
        } elseif (Test-Path $overrides) {
            Remove-Item -LiteralPath $overrides -Force
        }
        Push-Location $engineDir
        try {
            Write-Host '[oracle] flutter pub get --offline (tools/oracle/engine)'
            & $FlutterExe pub get --offline
            if ($LASTEXITCODE -ne 0) {
                Write-Host ('[oracle] FALLO flutter pub get (' + $LASTEXITCODE + ')')
                $failed++
            } else {
                $env:ROWMATE_ORACLE_OUT = $outDir
                $env:ROWMATE_LEGACY_COMMIT = $legacyCommit
                Write-Host '[oracle] flutter test test/engine_vectors_test.dart'
                & $FlutterExe test test/engine_vectors_test.dart
                if ($LASTEXITCODE -ne 0) { Write-Host ('[oracle] FALLO flutter test (' + $LASTEXITCODE + ')'); $failed++ }
            }
        }
        finally {
            Pop-Location
            Remove-Item Env:ROWMATE_ORACLE_OUT -ErrorAction SilentlyContinue
            Remove-Item Env:ROWMATE_LEGACY_COMMIT -ErrorAction SilentlyContinue
        }
        # Nada de lo que dejan pub get y flutter test puede quedar sin ignorar (QA-G4aT-05).
        $engineStatus = & git --no-optional-locks -C $repo status --porcelain --untracked-files=all -- tools/oracle/engine
        if ($LASTEXITCODE -ne 0) { throw ('git status de tools/oracle/engine fallo con ' + $LASTEXITCODE) }
        foreach ($line in @($engineStatus)) {
            if ($null -eq $line -or $line.Trim() -eq '') { continue }
            $path = $line.Substring(3).Trim('"')
            if ($path -ne 'tools/oracle/engine/pubspec.yaml' -and -not $path.StartsWith('tools/oracle/engine/test/')) {
                Write-Host ('[oracle] ERROR: quedo un archivo sin ignorar en tools/oracle/engine: ' + $path)
                $failed++
            }
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
