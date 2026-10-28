# Ejecutable de prueba SIN ventana (QA-V3-11 de la It. 3): sirve para correr de verdad el arranque de capture-window.ps1 sin
# abrir un build. Es de subsistema grafico (no abre una consola) y su Main esta vacio: termina enseguida, no muestra nada, no
# toca el mouse ni la pantalla y no tiene efectos. Lo usan el ensayo de capture-cap27.ps1 (-Rehearsal) y
# ItThreeToolingTests.WindowCaptureStartsTheExeForReal, siempre en una carpeta temporal que despues borran. No entra al repo
# ni a ningun build.
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
# Solo Windows PowerShell 5.1 (powershell -NoProfile -File): el Add-Type de pwsh 7 no arma ejecutables.
#
#   tools/unity/new-windowless-exe.ps1 -Out <carpeta temporal>/sin-ventana.exe
#
# Codigos: 0 listo; 1 no se pudo compilar, o no salio de subsistema grafico (no queda el archivo); 2 falta -Out o ese archivo
# ya existe (no se pisa nada).
param(
    [string]$Out = ''
)

$ErrorActionPreference = 'Stop'
$target = ''
trap {
    Write-Host ('[sin-ventana] no se pudo armar: ' + $_.Exception.Message)
    if ($target -ne '' -and (Test-Path -LiteralPath $target)) { Remove-Item -LiteralPath $target -Force -ErrorAction SilentlyContinue }
    exit 1
}

if ($Out -eq '' -or (Test-Path -LiteralPath $Out)) {
    Write-Host '[sin-ventana] falta -Out o ese archivo ya existe: no se pisa nada'
    exit 2
}
# La ruta entera, respecto de la ubicacion de PowerShell (la misma para Add-Type y para ReadAllBytes).
$target = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Out)
Add-Type -OutputType WindowsApplication -OutputAssembly $target -TypeDefinition 'public static class SinVentana { public static void Main() { } }'

# Subsistema del encabezado PE (e_lfanew + 0x5C): 2 = grafico. Con 3 (consola) se abriria una ventana de consola.
$bytes = [System.IO.File]::ReadAllBytes($target)
$subsystem = [BitConverter]::ToUInt16($bytes, [BitConverter]::ToInt32($bytes, 0x3C) + 0x5C)
if ($subsystem -ne 2) {
    Remove-Item -LiteralPath $target -Force
    Write-Host ('[sin-ventana] el ejecutable no salio de subsistema grafico (' + $subsystem + '): se borra')
    exit 1
}
Write-Host '[sin-ventana] listo'
exit 0
