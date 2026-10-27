# Captura de la ventana entera de un build de Windows, con su barra de titulo (CAP-22 en equipo, [Device-Win]; 02-tech-design
# Sec. 4.3). Opcional, para QA: abre el ejecutable, espera, captura la ventana con su marco y la cierra.
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
#
#   tools/unity/capture-window.ps1 -Exe Builds/Windows/normal/RowMate.exe -Out <archivo.png> [-WaitSeconds 6] [-Arguments '...']
#
# Codigos: 0 bien; 1 no aparecio la ventana; 2 no existe el ejecutable.
param(
    [string]$Exe = 'Builds/Windows/normal/RowMate.exe',
    [string]$Out = 'CAP-22-ventana.png',
    [int]$WaitSeconds = 6,
    [string]$Arguments = ''
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$exePath = $Exe
if (-not [System.IO.Path]::IsPathRooted($exePath)) { $exePath = Join-Path $repo $exePath }
if (-not (Test-Path $exePath)) {
    Write-Host ('[ventana] no existe ' + $exePath)
    exit 2
}

Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class RowMateWindow {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
'@
[RowMateWindow]::SetProcessDPIAware() | Out-Null

$process = Start-Process -FilePath $exePath -ArgumentList $Arguments -PassThru
try {
    $deadline = (Get-Date).AddSeconds($WaitSeconds + 20)
    while ($process.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
    }
    if ($process.MainWindowHandle -eq [IntPtr]::Zero) {
        Write-Host '[ventana] el build no abrio ninguna ventana'
        exit 1
    }
    Start-Sleep -Seconds $WaitSeconds
    $handle = $process.MainWindowHandle
    [RowMateWindow]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Milliseconds 500
    $rect = New-Object RowMateWindow+RECT
    [RowMateWindow]::GetWindowRect($handle, [ref]$rect) | Out-Null
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $outPath = $Out
    if (-not [System.IO.Path]::IsPathRooted($outPath)) { $outPath = Join-Path (Get-Location) $outPath }
    $bitmap.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    Write-Host ('[ventana] ' + $process.MainWindowTitle + ' (' + $width + 'x' + $height + ') -> ' + $outPath)
}
finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        Start-Sleep -Seconds 2
        if (-not $process.HasExited) { $process.Kill() }
    }
}
exit 0
