# Compila RowMateBle.dll (plugin de Bluetooth LE de Windows, It. 10a 02-tech-design Sec. 3) con MSVC y C++/WinRT del Windows SDK.
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
#
#   native/windows/build.ps1 [-Check] [-KeepObj]
#
# - Encuentra Visual Studio con vswhere (el mas nuevo con VC.Tools x64) y abre su entorno con Launch-VsDevShell (amd64); el SDK
#   sale de $env:WindowsSDKVersion. Ninguna ruta fija de la PC.
# - Antes de compilar revisa las fuentes propias: ningun __FILE__, __builtin_FILE, source_location ni assert( (codigo 3), y
#   ningun PairAsync, DeviceInformationPairing, UnpairAsync ni SetStateAsync (AC-BLE-SEC-01, codigo 3).
# - cl /std:c++20 /EHsc /O2 /MT /permissive- /W4 /WX /utf-8 /DNDEBUG /DWINRT_NO_SOURCE_LOCATION /Brepro (QA-G10a-01: sin la
#   macro, C++/WinRT guarda la ruta de sus encabezados en el binario) + link /DLL /DEBUG:NONE /Brepro WindowsApp.lib, sin PDB,
#   con la CRT estatica (sin redistribuible). Todo en una carpeta temporal propia (se borra al final salvo -KeepObj).
# - Despues revisa el binario: ningun "C:\", "\Users\", "\Windows Kits\", el usuario ni el nombre del equipo, en ASCII ni en
#   UTF-16 (codigo 2; nunca se saltea).
# - Sin -Check copia a Assets/Plugins/Windows/x86_64/RowMateBle.dll; se niega (codigo 5) con el proyecto abierto en Unity
#   (Temp/UnityLockfile tomado: el Editor tiene la DLL cargada).
# - -Check compila en temporal y compara el SHA-256 con el versionado (codigo 4 si difiere). No copia.
# Codigos: 0 bien; 1 no compilo (o no hay Visual Studio); 2 rutas en el binario; 3 patron prohibido en las fuentes; 4 -Check
# distinto; 5 Unity abierto.
param(
    [switch]$Check,
    [switch]$KeepObj
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sources = Join-Path $PSScriptRoot 'RowMateBle'
$target = Join-Path $repo 'Assets\Plugins\Windows\x86_64\RowMateBle.dll'

# 1. Las fuentes propias.
$forbidden = @('__FILE__', '__builtin_FILE', 'source_location', 'assert(', 'PairAsync', 'DeviceInformationPairing', 'UnpairAsync', 'SetStateAsync', 'RequestAccessAsync')
$bad = @()
foreach ($file in (Get-ChildItem -Path $sources -File -Include '*.h', '*.cpp' -Recurse)) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    foreach ($pattern in $forbidden) {
        if ($text.Contains($pattern)) { $bad += ($file.Name + ': ' + $pattern) }
    }
}
if ($bad.Count -gt 0) {
    foreach ($b in $bad) { Write-Host ('[native] patron prohibido en las fuentes: ' + $b) }
    exit 3
}

# 2. Visual Studio y su entorno.
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) { Write-Host '[native] no se encontro vswhere (Visual Studio con C++)'; exit 1 }
$vs = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if ([string]::IsNullOrEmpty($vs)) { Write-Host '[native] no hay Visual Studio con VC.Tools x64'; exit 1 }
$devShell = Join-Path $vs 'Common7\Tools\Launch-VsDevShell.ps1'
if (-not (Test-Path $devShell)) { Write-Host '[native] falta Launch-VsDevShell.ps1'; exit 1 }
$here = Get-Location
& $devShell -Arch amd64 -HostArch amd64 -SkipAutomaticLocation *> $null
Set-Location $here
if ([string]::IsNullOrEmpty($env:WindowsSDKVersion)) { Write-Host '[native] el entorno de Visual Studio no tiene Windows SDK'; exit 1 }
Write-Host ('[native] Windows SDK ' + $env:WindowsSDKVersion.TrimEnd('\') + ', MSVC ' + $env:VCToolsVersion)

# 3. Compilar en una carpeta temporal propia.
$obj = Join-Path ([System.IO.Path]::GetTempPath()) ('rowmate-native-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $obj | Out-Null
$code = 1
try {
    $code = & {
    Push-Location $obj
    try {
        $cpp = @(Get-ChildItem -Path $sources -Filter '*.cpp' -File | Sort-Object Name | ForEach-Object { $_.FullName })
        $clArgs = @('/nologo', '/c', '/std:c++20', '/EHsc', '/O2', '/MT', '/permissive-', '/W4', '/WX', '/utf-8', '/DNDEBUG',
                    '/DWINRT_NO_SOURCE_LOCATION', '/DUNICODE', '/D_UNICODE', '/Brepro', '/bigobj') + $cpp
        & cl.exe @clArgs | Out-Host
        if ($LASTEXITCODE -ne 0) { Write-Host '[native] cl fallo'; return 1 }
        $objs = @(Get-ChildItem -Path $obj -Filter '*.obj' -File | Sort-Object Name | ForEach-Object { $_.Name })
        $linkArgs = @('/nologo', '/DLL', '/DEBUG:NONE', '/Brepro', '/INCREMENTAL:NO', '/OPT:REF', '/OPT:ICF', '/OUT:RowMateBle.dll') + $objs + @('WindowsApp.lib')
        & link.exe @linkArgs | Out-Host
        if ($LASTEXITCODE -ne 0) { Write-Host '[native] link fallo'; return 1 }
    }
    finally {
        Pop-Location
    }

    $dll = Join-Path $obj 'RowMateBle.dll'
    if (-not (Test-Path $dll)) { Write-Host '[native] no se genero la DLL'; return 1 }
    if (Test-Path (Join-Path $obj 'RowMateBle.pdb')) { Write-Host '[native] se genero un PDB'; return 1 }

    # 4. Rutas en el binario (ASCII y UTF-16, sin distinguir mayusculas).
    $bytes = [System.IO.File]::ReadAllBytes($dll)
    $ascii = [System.Text.Encoding]::GetEncoding(28591).GetString($bytes)
    $wide0 = [System.Text.Encoding]::Unicode.GetString($bytes)
    $wide1 = [System.Text.Encoding]::Unicode.GetString($bytes, 1, $bytes.Length - 1)
    $needles = @('C:\', '\Users\', '\Windows Kits\')
    if (-not [string]::IsNullOrEmpty($env:USERNAME)) { $needles += $env:USERNAME }
    if (-not [string]::IsNullOrEmpty($env:COMPUTERNAME)) { $needles += $env:COMPUTERNAME }
    $found = 0
    foreach ($needle in $needles) {
        foreach ($haystack in @($ascii, $wide0, $wide1)) {
            if ($haystack.IndexOf($needle, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { $found++ }
        }
    }
    if ($found -gt 0) { Write-Host ('[native] el binario tiene rutas o datos del equipo (' + $found + ' coincidencias): no se copia'); return 2 }

    $hash = (Get-FileHash -Algorithm SHA256 -Path $dll).Hash
    Write-Host ('[native] RowMateBle.dll ' + $bytes.Length + ' bytes, SHA-256 ' + $hash + ', sin rutas')

    if ($Check) {
        if (-not (Test-Path $target)) { Write-Host '[native] -Check: no hay DLL versionada'; return 4 }
        $versioned = (Get-FileHash -Algorithm SHA256 -Path $target).Hash
        if ($versioned -ne $hash) { Write-Host ('[native] -Check: distinta de la versionada (' + $versioned + ')'); return 4 }
        Write-Host '[native] -Check: identica a la versionada'
        return 0
    }

    $lock = Join-Path $repo 'Temp\UnityLockfile'
    if (Test-Path $lock) {
        try {
            $stream = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
            $stream.Close()
        }
        catch {
            Write-Host '[native] el proyecto esta abierto en Unity (la DLL queda cargada): cerralo y repeti'
            return 5
        }
    }

    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
    Copy-Item -Force -Path $dll -Destination $target
    Write-Host '[native] copiada a Assets/Plugins/Windows/x86_64/RowMateBle.dll'
    return 0
    }
}
finally {
    if (-not $KeepObj) { Remove-Item -Recurse -Force -Path $obj -ErrorAction SilentlyContinue }
    else { Write-Host ('[native] objetos en ' + $obj) }
}
exit $code
