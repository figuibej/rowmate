<#
.SYNOPSIS
  Sigue en vivo los workflows de agentes de RowMate: fases, agentes que arrancan y
  terminan, y cada herramienta que usan (archivos que leen/escriben, comandos).

.DESCRIPTION
  Lee los journals que Claude Code escribe en ~/.claude/projects/<proyecto>/<sesión>/
  subagents/workflows/wf_*/ y salta solo al workflow más reciente cuando arranca uno nuevo.
  No modifica nada. Ctrl+C para salir.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\watch-agents.ps1
  powershell -ExecutionPolicy Bypass -File tools\watch-agents.ps1 -Quiet   # solo inicio/fin de agentes
#>
param(
  [switch]$Quiet,
  [int]$PollMs = 1500,
  [string]$ProjectsDir = ''
)

$ErrorActionPreference = 'Stop'
# Claude Code guarda cada proyecto en ~/.claude/projects/<ruta del repo con ':' y separadores cambiados por '-'>.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ($ProjectsDir -eq '') { $ProjectsDir = Join-Path $env:USERPROFILE ('.claude\projects\' + ($repoRoot -replace '[:\\/]', '-')) }
# Carpeta que contiene el repo (y el legacy al lado): se recorta de las rutas que se muestran.
$devRootPattern = '(?i)' + (((Split-Path $repoRoot -Parent) -split '[\\/]' | ForEach-Object { [regex]::Escape($_) }) -join '[\\/]') + '[\\/]'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$offsets = @{}   # ruta de archivo -> bytes ya procesados
$labels = @{}    # agentId -> etiqueta del agente (p. ej. qa:plan-harmony)
$currentJournal = $null
$currentPhase = $null

function Now { (Get-Date).ToString('HH:mm:ss') }

# Devuelve las líneas completas agregadas desde la última lectura (la línea a medio escribir queda para la próxima).
function Read-NewLines([string]$path) {
  $start = 0L
  if ($offsets.ContainsKey($path)) { $start = [long]$offsets[$path] }
  $fs = [System.IO.File]::Open($path, 'Open', 'Read', 'ReadWrite')
  try {
    $len = $fs.Length
    if ($len -le $start) { return }
    [void]$fs.Seek($start, 'Begin')
    $buf = New-Object byte[] ($len - $start)
    $read = 0
    while ($read -lt $buf.Length) {
      $r = $fs.Read($buf, $read, $buf.Length - $read)
      if ($r -le 0) { break }
      $read += $r
    }
  } finally { $fs.Dispose() }
  if ($read -le 0) { return }
  $last = [Array]::LastIndexOf($buf, [byte]10, $read - 1)
  if ($last -lt 0) { return }
  $offsets[$path] = $start + $last + 1
  [System.Text.Encoding]::UTF8.GetString($buf, 0, $last) -split "`n"
}

function Short([string]$s) {
  $s = $s -replace '\\\\', '\' -replace '\\"', '"' -replace '\\n', ' '
  $s = $s -replace $devRootPattern, ''
  if ($s.Length -gt 110) { $s = $s.Substring(0, 107) + '...' }
  $s
}

function Label-For([string]$agentId, [string]$wfDir) {
  if ($labels.ContainsKey($agentId)) { return $labels[$agentId] }
  $meta = Join-Path $wfDir "agent-$agentId.meta.json"
  if (Test-Path $meta) {
    $m = [regex]::Match((Get-Content $meta -Raw), '"description":"([^"]+)"')
    if ($m.Success) { $labels[$agentId] = $m.Groups[1].Value; return $m.Groups[1].Value }
  }
  $agentId
}

function Handle-JournalLine([string]$line, [bool]$replay) {
  $t = if ($replay) { '  (antes) ' } else { "$(Now) " }
  $type = [regex]::Match($line, '^\{"type":"([a-z_]+)"').Groups[1].Value
  switch ($type) {
    'launched' { Write-Host "$t" -NoNewline; Write-Host 'Workflow iniciado' -ForegroundColor Cyan }
    'started' {
      $m = [regex]::Match($line, '"agentId":"([^"]+)","label":"([^"]+)"(?:,"phase":"([^"]*)")?')
      $labels[$m.Groups[1].Value] = $m.Groups[2].Value
      $phase = $m.Groups[3].Value
      if ($phase -and $phase -ne $script:currentPhase) {
        $script:currentPhase = $phase
        Write-Host ''
        Write-Host "=== Fase: $phase ===" -ForegroundColor Magenta
      }
      Write-Host "$t" -NoNewline; Write-Host '> arranca  ' -ForegroundColor Yellow -NoNewline; Write-Host $m.Groups[2].Value
    }
    'result' {
      $id = [regex]::Match($line, '"agentId":"([^"]+)"').Groups[1].Value
      $extra = ''
      $v = [regex]::Match($line, '"verdict":"([^"]+)"')
      if ($v.Success) { $extra = "  veredicto: $($v.Groups[1].Value)" }
      $tf = [regex]::Match($line, '"total_features":(\d+)')
      if ($tf.Success) { $extra = "  $($tf.Groups[1].Value) funcionalidades" }
      Write-Host "$t" -NoNewline; Write-Host 'OK termina ' -ForegroundColor Green -NoNewline; Write-Host "$($labels[$id])$extra"
    }
    '' { }
    default { Write-Host "$t" -NoNewline; Write-Host "evento: $type" -ForegroundColor DarkCyan }
  }
}

function Handle-AgentLine([string]$line, [string]$label) {
  if ($line -notmatch '"type":"assistant"' -or $line -notmatch '"tool_use"') { return }
  $ms = [regex]::Matches($line, '"type":"tool_use","id":"[^"]*","name":"([A-Za-z_]+)","input":\{(?:"([a-z_]+)":"((?:[^"\\]|\\.){0,300}))?')
  foreach ($m in $ms) {
    $tool = $m.Groups[1].Value
    $arg = if ($m.Groups[3].Success) { Short $m.Groups[3].Value } else { '' }
    $color = switch ($tool) { 'Write' { 'Green' } 'Edit' { 'Green' } 'Bash' { 'DarkYellow' } 'PowerShell' { 'DarkYellow' } 'StructuredOutput' { 'Cyan' } default { 'Gray' } }
    Write-Host "$(Now)   " -NoNewline
    Write-Host ($label.PadRight(26)) -ForegroundColor DarkGray -NoNewline
    Write-Host ($tool.PadRight(12)) -ForegroundColor $color -NoNewline
    Write-Host $arg -ForegroundColor DarkGray
  }
}

Write-Host "Siguiendo los agentes de RowMate (Ctrl+C para salir). Carpeta: $ProjectsDir" -ForegroundColor Cyan

while ($true) {
  try {
    $latest = Get-ChildItem -Path (Join-Path $ProjectsDir '*\subagents\workflows\wf_*\journal.jsonl') -ErrorAction SilentlyContinue |
      Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if ($latest -and $latest.FullName -ne $currentJournal) {
      $currentJournal = $latest.FullName
      $wfDir = $latest.DirectoryName
      $currentPhase = $null
      Write-Host ''
      Write-Host ("#" * 70) -ForegroundColor Cyan
      Write-Host "# Workflow $(Split-Path $wfDir -Leaf)" -ForegroundColor Cyan
      Write-Host ("#" * 70) -ForegroundColor Cyan
      # Historial del journal (inicio/fin de agentes); de los agentes, solo lo nuevo de acá en adelante.
      foreach ($l in @(Read-NewLines $currentJournal)) { Handle-JournalLine $l $true }
      Get-ChildItem (Join-Path $wfDir 'agent-*.jsonl') -ErrorAction SilentlyContinue | ForEach-Object { $offsets[$_.FullName] = $_.Length }
      Write-Host "--- en vivo desde $(Now) ---" -ForegroundColor DarkCyan
    }

    if ($currentJournal) {
      foreach ($l in @(Read-NewLines $currentJournal)) { Handle-JournalLine $l $false }
      if (-not $Quiet) {
        foreach ($f in @(Get-ChildItem (Join-Path $wfDir 'agent-*.jsonl') -ErrorAction SilentlyContinue)) {
          $id = $f.BaseName -replace '^agent-', ''
          $label = Label-For $id $wfDir
          foreach ($l in @(Read-NewLines $f.FullName)) { Handle-AgentLine $l $label }
        }
      }
    }
  } catch {
    Write-Host "$(Now) (error leyendo logs: $($_.Exception.Message))" -ForegroundColor DarkRed
  }
  Start-Sleep -Milliseconds $PollMs
}
