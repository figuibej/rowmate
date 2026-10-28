# Corre TODOS los tests del Core sin Unity (02-tech-design Sec. 2.6). Es el mismo comando en la PC y en el CI (ci-core.yml).
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
# Compatible con Windows PowerShell 5.1 y pwsh 7 (sin los operadores null-conditional, null-coalescing ni pipeline chain de PS 7).
#
#   tools/dotnet/run-tests.ps1 [-Filter "<expresion>"] [-Ci] [-Demo] [-Matrix] [-UnityResults <carpeta>] [-ArtifactsPath <carpeta>]
#
# 1. dotnet build de la solucion ENTERA (9 proyectos: tambien RowMate.Tests.UnityApiCheck, contra NUnit 3.5.0, y
#    RowMate.Tokens). Si falla, sale con ese codigo y NO corre tests (con --no-build correrian binarios viejos).
# 2. dotnet test --no-build de la misma solucion, con los mismos binarios.
# 3. -Matrix: arma <artifacts>/TestResults/ac-matrix.md (AC -> test -> resultado) desde los .trx, aunque haya rojos.
# 4. Sale con el codigo del dotnet test: propaga el PRIMER codigo distinto de 0.
#
# -Filter  agrega --filter al dotnet test (el paso 1 compila igual la solucion entera).
# -Ci      ROWMATE_CI=1: la higiene que depende de git falla en vez de quedar inconclusa.
# -Demo    solo TestCategory=Demo. Cada linea [DEMO] sale UNA vez (QA-V-07 de la It. 1): el adaptador de NUnit la manda a la
#          consola en vivo (NUnit.ConsoleOut=1) y con verbosity=normal no se repite en "Standard Output Messages"
#          (con detailed salia dos veces).
# -Matrix  la lista de IDs sale de TODOS los Assets/RowMate/Tests/EditMode/Traceability/It0*Ids.cs (It. 2, QA-G2-16). Los IDs
#          del bloque UnityOnlyIds (solo PlayMode, Unity EditMode, captura o equipo) figuran como PENDIENTE UNITY y los de
#          WorkPackageBIds como PENDIENTE WP-B mientras no tengan resultado, en vez de SIN TEST.
# -UnityResults  carpeta con los XML de NUnit que escribe Unity (run-unity-tests.ps1, -testResults): se suman a la matriz
#          (mismo formato que los de dotnet) y los UnityOnlyIds sin resultado pasan a SIN TEST. Implica -Matrix.
# -ArtifactsPath  por defecto tools/dotnet/.artifacts (el del CI y la documentacion). Otra carpeta sirve para correr
#          dos compilaciones en paralelo sin pisarse.
param(
    [string]$Filter = "",
    [switch]$Ci,
    [switch]$Demo,
    [switch]$Matrix,
    [string]$UnityResults = "",
    [string]$ArtifactsPath = 'tools/dotnet/.artifacts'
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ($UnityResults -ne '') { $Matrix = $true }

$cmd = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -ne $cmd) { $dotnet = $cmd.Source } else { $dotnet = 'C:/Program Files/dotnet/dotnet.exe' }

if ($Ci) { $env:ROWMATE_CI = '1' }

$verbosity = 'normal'
$runSettings = @()
if ($Demo) {
    $Filter = 'TestCategory=Demo'
    $runSettings += 'NUnit.ConsoleOut=1'
}

$results = $ArtifactsPath + '/TestResults'
$build = @('build', 'tools/dotnet/RowMate.sln', '--configuration', 'Release', '--artifacts-path', $ArtifactsPath)
$test = @('test', 'tools/dotnet/RowMate.sln', '--configuration', 'Release', '--no-build', '--artifacts-path', $ArtifactsPath, '--results-directory', $results, '--logger', "console;verbosity=$verbosity", '--logger', 'trx;LogFilePrefix=rowmate-core')
if ($Filter -ne '') { $test += @('--filter', $Filter) }

$resultsDir = $results -replace '/', '\'
if (-not [System.IO.Path]::IsPathRooted($resultsDir)) { $resultsDir = Join-Path $repo $resultsDir }
$nunitDir = Join-Path $resultsDir 'nunit'
if ($Matrix) { $runSettings += @("NUnit.TestOutputXml=$nunitDir", 'NUnit.TestOutputXmlFolderMode=AsSpecified') }
if ($runSettings.Count -gt 0) { $test += @('--') + $runSettings }

function Show-Command([string[]]$CommandArgs) {
    $quoted = @()
    foreach ($a in $CommandArgs) {
        if ($a -match '[ ;|&()]') { $quoted += ('"' + $a + '"') } else { $quoted += $a }
    }
    Write-Host ('"' + $dotnet + '" ' + ($quoted -join ' '))
}

# Casos de un XML de NUnit (el del adaptador de dotnet o el que escribe Unity, mismo formato): nombre completo, resultado y
# categorias, tambien las heredadas del fixture y del metodo parametrizado. Cada AC-/LT- va a $ById con su origen.
function Add-NUnitResults([string]$Path, [hashtable]$ById, [string]$Origin) {
    $doc = New-Object System.Xml.XmlDocument
    $doc.Load($Path)
    foreach ($case in $doc.SelectNodes('//test-case')) {
        $cats = @()
        $node = $case
        while ($null -ne $node -and $node.NodeType -eq [System.Xml.XmlNodeType]::Element) {
            foreach ($p in $node.SelectNodes("properties/property[@name='Category']")) { $cats += $p.GetAttribute('value') }
            $node = $node.ParentNode
        }
        $outcome = $case.GetAttribute('result')
        if ($outcome -eq 'Skipped' -or $outcome -eq 'Inconclusive') { $outcome = 'NotExecuted' }
        foreach ($c in ($cats | Select-Object -Unique)) {
            if ($c -notmatch '^(AC|LT)-') { continue }
            if (-not $ById.ContainsKey($c)) { $ById[$c] = New-Object System.Collections.Generic.List[object] }
            $ById[$c].Add(@{ Test = $case.GetAttribute('fullname'); Outcome = $outcome; Origin = $Origin })
        }
    }
}

# IDs entre comillas de un texto (AC-XXX-nn-nn, AC-BASE-nn, LT-nn), sin repetir, en orden de aparicion.
function Get-QuotedIds([string]$Text) {
    $ids = New-Object System.Collections.Generic.List[string]
    foreach ($m in [regex]::Matches($Text, '"((?:AC-[A-Z]+-\d\d(?:-\d\d)?)|(?:LT-\d\d))"')) {
        if (-not $ids.Contains($m.Groups[1].Value)) { $ids.Add($m.Groups[1].Value) }
    }
    return ,$ids
}

# IDs del bloque "<Nombre> = { ... };" de un It0*Ids.cs (UnityOnlyIds, WorkPackageBIds). Vacio si no esta.
function Get-BlockIds([string]$Text, [string]$Name) {
    $m = [regex]::Match($Text, $Name + '\s*=\s*\{([^}]*)\}')
    if (-not $m.Success) { return ,(New-Object System.Collections.Generic.List[string]) }
    return ,(Get-QuotedIds $m.Groups[1].Value)
}

# Matriz AC -> test -> resultado (AC-BASE-09). El logger trx no guarda las categorias de NUnit, asi que con -Matrix el
# adaptador escribe ademas su XML propio (NUnit.TestOutputXml) en <TestResults>/nunit, que si las tiene. Con -UnityResults
# se suman los XML de Unity (EditMode y PlayMode, con y sin simulador).
function Write-AcMatrix([string]$ResultsDir, [string]$NUnitDir, [datetime]$Since, [string]$UnityDir) {
    $matrixPath = Join-Path $ResultsDir 'ac-matrix.md'
    $byId = @{}
    $xmlFiles = @(Get-ChildItem -Path $NUnitDir -Filter '*.xml' -File -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -ge $Since })
    foreach ($xml in $xmlFiles) { Add-NUnitResults $xml.FullName $byId 'dotnet' }
    $unityFiles = @()
    if ($UnityDir -ne '') {
        $unityPath = $UnityDir -replace '/', '\'
        if (-not [System.IO.Path]::IsPathRooted($unityPath)) { $unityPath = Join-Path $repo $unityPath }
        if (-not (Test-Path $unityPath)) { throw ('-UnityResults: no existe la carpeta ' + $unityPath) }
        $unityFiles = @(Get-ChildItem -Path $unityPath -Filter '*.xml' -File -Recurse)
        foreach ($xml in $unityFiles) { Add-NUnitResults $xml.FullName $byId 'unity' }
    }

    # Lista completa de las specs: TODOS los It0*Ids.cs (It. 1: 139 AC y 19 LT; It. 2: 125 AC y 15 LT; It. 3: 102 AC), con sus bloques
    # UnityOnlyIds (PENDIENTE UNITY) y WorkPackageBIds (PENDIENTE WP-B).
    $traceDir = Join-Path $repo 'Assets\RowMate\Tests\EditMode\Traceability'
    $expected = New-Object System.Collections.Generic.List[string]
    $unityOnly = New-Object System.Collections.Generic.List[string]
    $pendingB = New-Object System.Collections.Generic.List[string]
    $idsFiles = @(Get-ChildItem -Path $traceDir -Filter 'It0*Ids.cs' -File -ErrorAction SilentlyContinue | Sort-Object Name)
    foreach ($f in $idsFiles) {
        $text = [System.IO.File]::ReadAllText($f.FullName)
        foreach ($id in (Get-QuotedIds $text)) { if (-not $expected.Contains($id)) { $expected.Add($id) } }
        foreach ($id in (Get-BlockIds $text 'UnityOnlyIds')) { if (-not $unityOnly.Contains($id)) { $unityOnly.Add($id) } }
        foreach ($id in (Get-BlockIds $text 'WorkPackageBIds')) { if (-not $pendingB.Contains($id)) { $pendingB.Add($id) } }
    }
    foreach ($k in $byId.Keys) { if (-not $expected.Contains($k)) { $expected.Add($k) } }
    $sorted = $expected | Sort-Object

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('# Matriz AC -> test -> resultado')
    $lines.Add('')
    $source = 'Generada por tools/dotnet/run-tests.ps1 -Matrix desde ' + $xmlFiles.Count + ' resultado(s) de NUnit de dotnet'
    if ($UnityDir -ne '') { $source += ' y ' + $unityFiles.Count + ' de Unity (' + $UnityDir + ')' }
    $lines.Add($source + ' (' + (Get-Date -Format 'yyyy-MM-dd HH:mm') + '), con los IDs de ' + $idsFiles.Count + ' archivo(s) It0*Ids.cs.')
    $lines.Add('PASA = al menos un test paso y ninguno fallo; IGNORADO = solo tests [Ignore] (NO VERIFICABLE); PENDIENTE UNITY = se')
    $lines.Add('verifica solo en Unity (PlayMode, EditMode con UnityEditor, captura) y no se pasaron sus resultados (-UnityResults);')
    $lines.Add('PENDIENTE WP-B = su test de dotnet es de un archivo de WP-B que todavia no existe.')
    $lines.Add('')
    $lines.Add('| ID | Resultado | Pasan | Fallan | Ignorados | Origen | Tests |')
    $lines.Add('|---|---|---|---|---|---|---|')
    $summary = @{ PASA = 0; FALLA = 0; IGNORADO = 0; 'PENDIENTE UNITY' = 0; 'PENDIENTE WP-B' = 0; 'SIN TEST' = 0 }
    foreach ($id in $sorted) {
        $passed = 0; $failed = 0; $skipped = 0; $names = @(); $origins = @()
        if ($byId.ContainsKey($id)) {
            foreach ($t in $byId[$id]) {
                if ($t.Outcome -eq 'Passed') { $passed++ } elseif ($t.Outcome -eq 'Failed') { $failed++ } else { $skipped++ }
                if ($origins -notcontains $t.Origin) { $origins += $t.Origin }
                $short = $t.Test -replace '^RowMate\.Tests\.(EditMode|Simulator|PlayMode\.Simulator|PlayMode|UnityEditMode)\.', ''
                if ($short.Length -gt 80) { $short = $short.Substring(0, 77) + '...' }
                if ($names.Count -lt 4) { $names += ($short -replace '\|', '/') }
            }
        }
        $isUnityOnly = $unityOnly.Contains($id) -and $UnityDir -eq ''
        if ($failed -gt 0) { $state = 'FALLA' }
        elseif ($passed -gt 0) { $state = 'PASA' }
        elseif ($isUnityOnly) { $state = 'PENDIENTE UNITY' }
        elseif ($skipped -gt 0) { $state = 'IGNORADO' }
        elseif ($pendingB.Contains($id)) { $state = 'PENDIENTE WP-B' }
        else { $state = 'SIN TEST' }
        $summary[$state]++
        $total = $passed + $failed + $skipped
        $more = ''
        if ($total -gt $names.Count) { $more = ' (+' + ($total - $names.Count) + ')' }
        $origin = '-'
        if ($origins.Count -gt 0) { $origin = ($origins | Sort-Object) -join '+' }
        $lines.Add('| ' + $id + ' | ' + $state + ' | ' + $passed + ' | ' + $failed + ' | ' + $skipped + ' | ' + $origin + ' | ' + ($names -join '; ') + $more + ' |')
    }
    $lines.Add('')
    $totals = 'PASA ' + $summary['PASA'] + ', FALLA ' + $summary['FALLA'] + ', IGNORADO ' + $summary['IGNORADO'] + ', PENDIENTE UNITY ' + $summary['PENDIENTE UNITY'] + ', PENDIENTE WP-B ' + $summary['PENDIENTE WP-B'] + ', SIN TEST ' + $summary['SIN TEST']
    $lines.Add('Resumen: ' + $totals + ' (de ' + $sorted.Count + ' IDs).')
    New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null
    [System.IO.File]::WriteAllLines($matrixPath, $lines, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host ('[run-tests] matriz: ' + $matrixPath + ' (' + $totals + ')')
}

Push-Location $repo
try {
    $started = Get-Date
    Show-Command $build
    & $dotnet @build
    $buildExit = $LASTEXITCODE
    if ($buildExit -ne 0) {
        Write-Host "[run-tests] la compilacion fallo ($buildExit): no se corren tests"
        exit $buildExit
    }

    Show-Command $test
    & $dotnet @test
    $testExit = $LASTEXITCODE

    if ($Matrix) {
        Write-AcMatrix $resultsDir $nunitDir $started $UnityResults
    }

    exit $testExit
}
finally {
    Pop-Location
}
