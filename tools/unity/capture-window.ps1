# Captura de la ventana entera de un build de Windows, con su barra de titulo (CAP-22 en equipo, [Device-Win]; 02-tech-design
# Sec. 4.3). Opcional, para QA: abre el ejecutable, espera, captura la ventana con su marco y la cierra.
# Archivo solo ASCII a proposito: Windows PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI.
#
#   tools/unity/capture-window.ps1 -Exe Builds/Windows/normal/RowMate.exe -Out <archivo.png> [-WaitSeconds 6] [-Arguments '...']
#                                  [-ClickRelative 'x,y']
#
# -ClickRelative (It. 3, CAP-27): antes de capturar, un clic izquierdo en ese punto del area cliente, en fracciones de su
# ancho y alto (0.625,0.98 = el centro de la 3.a de las 4 pestanas de la barra de abajo: Rutinas; QA-V3-01), y 1,5 s de
# espera. Solo si en ese punto esta la ventana del build; despues el puntero vuelve a donde estaba.
# Antes de capturar comprueba en 9 puntos de la ventana que este adelante: si otra ventana la tapa (otra app, un aviso del
# sistema), no hace clic ni captura, porque saldria lo que hay en la pantalla del usuario, y sale con 3.
#
# Arranque (QA-V3-11): sin -Arguments no pasa -ArgumentList, porque Start-Process de Windows PowerShell 5.1 no acepta ''
# (asi la llama capture-cap27.ps1). Espera la ventana hasta WaitSeconds + 20 s, o hasta que el proceso termine: un build que
# se cierra sin ventana sale con 1 enseguida y nunca pasa por abierto (en 5.1, MainWindowHandle de un proceso que termino da
# $null). Lo corren de verdad ItThreeToolingTests.WindowCaptureStartsTheExeForReal y el ensayo de capture-cap27.ps1, con un
# ejecutable de prueba sin ventana (new-windowless-exe.ps1).
#
# Marco (QA-V3-12): la captura es el marco VISIBLE de la ventana (DwmGetWindowAttribute con DWMWA_EXTENDED_FRAME_BOUNDS),
# no el de GetWindowRect: en Windows 10 y 11 ese incluye bordes invisibles de unos 7-8 px a los costados y abajo (1296 x 759
# para una ventana de 1280 x 720) y la foto se llevaba franjas de lo que hubiera detras, de la pantalla del usuario. Si DWM
# no responde (sin composicion), GetWindowRect. Get-VisibleFrame lo corre de verdad
# ItThreeDebtTests.WindowCaptureUsesTheVisibleFrame sobre una ventana que ya existe (sin capturar ni tocar nada).
#
# Codigos: 0 bien; 1 no aparecio la ventana (o el build se cerro antes de la captura); 2 no existe el ejecutable; 3 otra
# ventana tapa la del build (sin clic ni captura); 4 error inesperado (por ejemplo, Start-Process no pudo abrir el
# ejecutable): el mensaje sale en la consola y el build que se haya abierto se cierra igual.
param(
    [string]$Exe = 'Builds/Windows/normal/RowMate.exe',
    [string]$Out = 'CAP-22-ventana.png',
    [int]$WaitSeconds = 6,
    [string]$Arguments = '',
    [string]$ClickRelative = ''
)

$ErrorActionPreference = 'Stop'
# Un error no previsto sale con 4 y su mensaje, no con el 1 de "no aparecio la ventana" (QA-V3-11: el error de parametro de
# Start-Process salia con 1). El finally de abajo corre antes que esto: el build que se haya abierto se cierra.
trap {
    Write-Host ('[ventana] error inesperado (linea ' + $_.InvocationInfo.ScriptLineNumber + '): ' + $_.Exception.Message)
    exit 4
}
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
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attribute, out RECT value, int size);
}
'@
[RowMateWindow]::SetProcessDPIAware() | Out-Null

# El marco visible de la ventana, en pixeles de pantalla (QA-V3-12): DWMWA_EXTENDED_FRAME_BOUNDS (9), sin los bordes
# invisibles que suma GetWindowRect. Si DWM no responde o da un marco vacio, GetWindowRect. Deja en $script:frameSource de
# donde salio ('dwm' o 'rect').
function Get-VisibleFrame([IntPtr]$Window) {
    $frame = New-Object RowMateWindow+RECT
    $size = [System.Runtime.InteropServices.Marshal]::SizeOf([type][RowMateWindow+RECT])
    $result = [RowMateWindow]::DwmGetWindowAttribute($Window, 9, [ref]$frame, $size)
    if ($result -eq 0 -and $frame.Right -gt $frame.Left -and $frame.Bottom -gt $frame.Top) {
        $script:frameSource = 'dwm'
        return $frame
    }
    $rect = New-Object RowMateWindow+RECT
    [RowMateWindow]::GetWindowRect($Window, [ref]$rect) | Out-Null
    $script:frameSource = 'rect'
    return $rect
}

# La ventana de arriba de todo en ese punto de la pantalla es la del build (GA_ROOT = 2: la ventana principal de lo que haya)?
function Test-OwnWindowAt([int]$X, [int]$Y) {
    $at = New-Object RowMateWindow+POINT
    $at.X = $X
    $at.Y = $Y
    $hit = [RowMateWindow]::WindowFromPoint($at)
    if ($hit -eq [IntPtr]::Zero) { return $false }
    return ([RowMateWindow]::GetAncestor($hit, 2) -eq $handle)
}

# La ventana principal del build, o cero mientras no haya. Si el proceso ya termino, tambien cero: en Windows PowerShell 5.1,
# MainWindowHandle de un proceso que termino da $null (el error del getter se traga) y $null -eq [IntPtr]::Zero es falso, asi
# que un build que se cerraba sin ventana pasaba por abierto y el script se caia mas adelante (QA-V3-11).
function Get-MainWindow {
    if ($process.HasExited) { return [IntPtr]::Zero }
    $process.Refresh()
    $window = $process.MainWindowHandle
    if ($null -eq $window -or $process.HasExited) { return [IntPtr]::Zero }
    return $window
}

# Start-Process de Windows PowerShell 5.1 no acepta -ArgumentList '': sin -Arguments, no se pasa (QA-V3-11).
$start = @{ FilePath = $exePath; PassThru = $true }
if ($Arguments -ne '') { $start['ArgumentList'] = $Arguments }
$process = Start-Process @start
try {
    # Hasta WaitSeconds + 20 s, o hasta que el proceso termine: sin proceso no hay ventana que esperar.
    $deadline = (Get-Date).AddSeconds($WaitSeconds + 20)
    $handle = Get-MainWindow
    while ($handle -eq [IntPtr]::Zero -and -not $process.HasExited -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        $handle = Get-MainWindow
    }
    if ($handle -eq [IntPtr]::Zero) {
        Write-Host '[ventana] el build no abrio ninguna ventana'
        if ($process.HasExited) { Write-Host ('[ventana] el proceso termino sin ventana (codigo ' + $process.ExitCode + ')') }
        exit 1
    }
    Start-Sleep -Seconds $WaitSeconds
    # Durante la espera el build se pudo cerrar.
    $handle = Get-MainWindow
    if ($handle -eq [IntPtr]::Zero) {
        Write-Host '[ventana] el build se cerro antes de la captura: no hay ventana que capturar'
        exit 1
    }
    [RowMateWindow]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Milliseconds 500
    if ($ClickRelative -ne '') {
        $parts = $ClickRelative.Split(',')
        $client = New-Object RowMateWindow+RECT
        [RowMateWindow]::GetClientRect($handle, [ref]$client) | Out-Null
        $point = New-Object RowMateWindow+POINT
        $point.X = [int]([double]::Parse($parts[0], [System.Globalization.CultureInfo]::InvariantCulture) * ($client.Right - $client.Left))
        $point.Y = [int]([double]::Parse($parts[1], [System.Globalization.CultureInfo]::InvariantCulture) * ($client.Bottom - $client.Top))
        [RowMateWindow]::ClientToScreen($handle, [ref]$point) | Out-Null
        if (-not (Test-OwnWindowAt $point.X $point.Y)) {
            Write-Host '[ventana] en el punto del clic hay otra ventana adelante: no se hace clic ni se captura'
            exit 3
        }
        $before = New-Object RowMateWindow+POINT
        $hadCursor = [RowMateWindow]::GetCursorPos([ref]$before)
        [RowMateWindow]::SetCursorPos($point.X, $point.Y) | Out-Null
        [RowMateWindow]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
        [RowMateWindow]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 1500
        if ($hadCursor) { [RowMateWindow]::SetCursorPos($before.X, $before.Y) | Out-Null }
    }
    $rect = Get-VisibleFrame $handle
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    foreach ($fx in @(0.1, 0.5, 0.9)) {
        foreach ($fy in @(0.1, 0.5, 0.9)) {
            if (-not (Test-OwnWindowAt ([int]($rect.Left + $fx * $width)) ([int]($rect.Top + $fy * $height)))) {
                Write-Host '[ventana] otra ventana tapa la del build: no se captura (saldria lo que hay en la pantalla)'
                exit 3
            }
        }
    }
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $outPath = $Out
    if (-not [System.IO.Path]::IsPathRooted($outPath)) { $outPath = Join-Path (Get-Location) $outPath }
    $bitmap.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    Write-Host ('[ventana] ' + $process.MainWindowTitle + ' (' + $width + 'x' + $height + ', marco ' + $script:frameSource + ') -> ' + $outPath)
}
finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        Start-Sleep -Seconds 2
        if (-not $process.HasExited) { $process.Kill() }
    }
}
exit 0
