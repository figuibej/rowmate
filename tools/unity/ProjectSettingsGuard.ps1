# La guardia de ProjectSettings (06a-escena-lago/02-tech-design Sec. 4.2, 7.0 regla 4; QA-G6aT-05): ninguna corrida de Unity que no sea
# el bootstrapper (pruebas, arnes de capturas, build) puede dejar cambiados ProjectSettings/ ni Assets/RowMate/Settings/ (el nivel de
# calidad, un asset de URP, la compilacion asincrona del Editor). Se guarda el SHA-256 de cada archivo antes de abrir Unity y se compara
# al cerrar; si algo cambio, la corrida FALLA con la lista y no restaura nada sola (podria pisar un cambio del usuario).
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
#
#   . tools/unity/ProjectSettingsGuard.ps1
#   $guard = Get-ProjectSettingsSnapshot $repo
#   ... Unity ...
#   $changed = Compare-ProjectSettingsSnapshot $repo $guard
#   if ($changed.Count -gt 0) { ... falla ... }
#
# El define del simulador (ROWMATE_SIMULATOR en ProjectSettings.asset) lo cambian y lo restauran los scripts (-Simulator): esa linea
# se ignora al comparar ProjectSettings.asset. Tambien se ignoran los archivos que Unity reescribe solo por abrir el proyecto con
# el mismo contenido logico (ninguno hoy: si aparece uno, se agrega con su motivo).

# QA-V6a-REN-18 (ronda 1 de la verificacion de la It. 6a): tambien los assets de la escena que el codigo de ejecucion podria tocar
# (SceneWorld.asset y Boot.unity, los slots y los materiales de la escena). Hoy el codigo toca copias; la guardia lo demuestra.
$script:GuardFolders = @('ProjectSettings', 'Assets\RowMate\Settings', 'Assets\RowMate\Scenes', 'Assets\RowMate\Slots', 'Assets\RowMate\Art\Materials\Scene')

function Get-GuardHash([string]$Path) {
    $name = [System.IO.Path]::GetFileName($Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($name -eq 'ProjectSettings.asset') {
        # Sin las lineas de los defines (scriptingDefineSymbols y sus plataformas): las maneja SimulatorDefine.
        $text = [System.Text.Encoding]::UTF8.GetString($bytes)
        $lines = $text -split "`n"
        $kept = New-Object System.Collections.Generic.List[string]
        $inDefines = $false
        foreach ($line in $lines) {
            if ($line -match '^\s*scriptingDefineSymbols:') { $inDefines = $true; continue }
            if ($inDefines) {
                if ($line -match '^\s{4,}\S') { continue }
                $inDefines = $false
            }
            $kept.Add($line)
        }
        $bytes = [System.Text.Encoding]::UTF8.GetBytes(($kept -join "`n"))
    }
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

function Get-ProjectSettingsSnapshot([string]$Repo) {
    $map = @{}
    foreach ($folder in $script:GuardFolders) {
        $root = Join-Path $Repo $folder
        if (-not (Test-Path $root)) { continue }
        Get-ChildItem -Path $root -Recurse -File | ForEach-Object {
            $relative = $_.FullName.Substring($Repo.Length).TrimStart('\', '/') -replace '\\', '/'
            $map[$relative] = Get-GuardHash $_.FullName
        }
    }
    return $map
}

# Devuelve la lista de archivos que cambiaron, aparecieron o desaparecieron (rutas relativas al repo).
function Compare-ProjectSettingsSnapshot([string]$Repo, [hashtable]$Before) {
    $after = Get-ProjectSettingsSnapshot $Repo
    $changed = New-Object System.Collections.Generic.List[string]
    foreach ($key in $Before.Keys) {
        if (-not $after.ContainsKey($key)) { $changed.Add('borrado: ' + $key); continue }
        if ($after[$key] -ne $Before[$key]) { $changed.Add('cambiado: ' + $key) }
    }
    foreach ($key in $after.Keys) {
        if (-not $Before.ContainsKey($key)) { $changed.Add('nuevo: ' + $key) }
    }
    return ,$changed
}

# Escribe la lista y devuelve el codigo de salida (0 si no cambio nada; 13 si cambio algo).
function Write-ProjectSettingsGuardResult([string]$Prefix, [System.Collections.Generic.List[string]]$Changed) {
    if ($Changed.Count -eq 0) {
        Write-Host ($Prefix + ' guardia de ProjectSettings: sin cambios.')
        return 0
    }
    Write-Host ($Prefix + ' guardia de ProjectSettings: la corrida cambio ' + $Changed.Count + ' archivo(s) de ProjectSettings/ o Assets/RowMate/Settings/ (QA-G6aT-05). No se restaura nada solo; revisar con git:')
    foreach ($line in $Changed) { Write-Host ('  ' + $line) }
    return 13
}
