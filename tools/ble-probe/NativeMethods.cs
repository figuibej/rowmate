using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RowMate.BleProbe
{
    // Las funciones de RowMateBle.dll que usa la herramienta (native/windows/RowMateBle/rm_ble.h): solo estado y búsqueda. A
    // propósito NO declara connect, write ni nada de GATT: la herramienta no puede conectarse a ningún equipo.
    internal static class NativeMethods
    {
        private const string Dll = "RowMateBle";
        public const string RelativeDll = "Assets/Plugins/Windows/x86_64/RowMateBle.dll";

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string path);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rm_ble_init();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void rm_ble_shutdown();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rm_ble_api_version();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rm_ble_adapter_state();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rm_ble_start_scan(byte[] servicesCsv);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rm_ble_stop_scan();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rm_ble_pending();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rm_ble_drain(byte[] buffer, int capacity);

        // La DLL versionada del repo (sube desde la carpeta actual y desde la del ejecutable hasta ProjectSettings/ProjectVersion.txt).
        public static bool Load(out string error)
        {
            var root = FindRoot(Directory.GetCurrentDirectory()) ?? FindRoot(AppContext.BaseDirectory);
            if (root == null)
            {
                error = "no encontré la raíz del repositorio (corré la herramienta desde el clon)";
                return false;
            }

            var path = Path.Combine(root, RelativeDll.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                error = "falta " + RelativeDll + " (native/windows/build.ps1)";
                return false;
            }

            if (LoadLibraryW(path) == IntPtr.Zero)
            {
                error = "no se pudo cargar " + RelativeDll + " (código " + Marshal.GetLastWin32Error() + ")";
                return false;
            }

            error = "";
            return true;
        }

        private static string? FindRoot(string start)
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ProjectSettings", "ProjectVersion.txt")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
