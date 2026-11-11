using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RowMate.Platform.Export
{
    // El respaldo de "Abrir carpeta" en Windows (It. 15, #70, AC-D15-13, DT15-06): si explorer.exe no arranca, la carpeta se abre con
    // el archivo seleccionado por la API del shell (SHParseDisplayName + SHOpenFolderAndSelectItems de shell32, P/Invoke, sin DLL
    // nueva). Devuelve false si no pudo (y WindowsFileExporter lanza RevealFailedException). Fuera de Windows, false.
    public static class ShellReveal
    {
        public static bool TrySelect(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var target = File.Exists(path) ? path : Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            var pidl = IntPtr.Zero;
            try
            {
                var hr = SHParseDisplayName(target!, IntPtr.Zero, out pidl, 0, out _);
                if (hr != 0 || pidl == IntPtr.Zero)
                {
                    return false;
                }

                return SHOpenFolderAndSelectItems(pidl, 0, null, 0) == 0;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (pidl != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pidl);
                }
            }
#else
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHParseDisplayName(string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint sfgaoOut);

        [DllImport("shell32.dll")]
        private static extern int SHOpenFolderAndSelectItems(IntPtr folder, uint count, IntPtr[]? items, uint flags);
#endif
    }
}
