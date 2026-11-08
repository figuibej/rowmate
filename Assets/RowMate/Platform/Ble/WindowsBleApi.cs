using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using RowMate.Core.Ble.Native;
using UnityEngine;

namespace RowMate.Platform.Ble
{
    // INativeBleApi sobre RowMateBle.dll (Assets/Plugins/Windows/x86_64; native/windows; 10a 02-tech-design §3). P/Invoke plano:
    // rm_ble_pending y rm_ble_drain no asignan (el byte[] de drenaje es de NativeBleAdapter y se fija sin copiar); las cadenas viajan
    // como UTF-8 terminado en 0, en caché por texto (ids y UUID se repiten: el keepalive no asigna cada 2 s). La DLL no tiene
    // keepalive nativo ni filtro del sistema (DT10-07, DT10-09). "Activar Bluetooth" abre los ajustes de Bluetooth de Windows
    // (ms-settings:bluetooth, Q-32): la app nunca enciende la radio. Hilo principal.
    public sealed class WindowsBleApi : INativeBleApi
    {
        public const string SettingsUri = "ms-settings:bluetooth";

        private const string Dll = "RowMateBle";

        private readonly Dictionary<string, byte[]> _utf8 = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        private bool _initialized;

        // Veces que se llamó rm_ble_init en este proceso (AC-APP-13-03: el build "simulator" nunca lo llama).
        public static int InitCount { get; private set; }

        // Hay una sesión del plugin abierta (para el cierre al recargar el dominio en el Editor).
        public static bool AnyInitialized { get; private set; }

        public int ApiVersion => rm_ble_api_version();

        public bool FiltersScanInOs => false;

        public bool NeedsNativeKeepAlive => false;

        // Carga la DLL y lee su versión: DllNotFoundException o EntryPointNotFoundException si no está (BleAdapterFactory).
        public static int Probe()
        {
            return rm_ble_api_version();
        }

        // Cierra el plugin si este proceso lo inició (AssemblyReloadEvents en el Editor; sin cargar la DLL si nunca se usó).
        public static void ShutdownIfInitialized()
        {
            if (!AnyInitialized)
            {
                return;
            }

            AnyInitialized = false;
            rm_ble_shutdown();
        }

        public int Init()
        {
            InitCount++;
            var code = rm_ble_init();
            _initialized = code == 0;
            AnyInitialized |= _initialized;
            return code;
        }

        public void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            _initialized = false;
            AnyInitialized = false;
            rm_ble_shutdown();
        }

        public double MonotonicSeconds()
        {
            return rm_clock_monotonic();
        }

        public int AdapterState()
        {
            return rm_ble_adapter_state();
        }

        public int StartScan(string servicesCsv)
        {
            return rm_ble_start_scan(Utf8(servicesCsv ?? ""));
        }

        public int StopScan()
        {
            return rm_ble_stop_scan();
        }

        public int Connect(string id, bool highMtu)
        {
            return rm_ble_connect(Utf8(id), highMtu ? 1 : 0);
        }

        public int Disconnect(string id)
        {
            return rm_ble_disconnect(Utf8(id));
        }

        public int Discover(string id)
        {
            return rm_ble_discover(Utf8(id));
        }

        public int SetNotify(string id, string service, string characteristic, bool enable)
        {
            return rm_ble_set_notify(Utf8(id), Utf8(service), Utf8(characteristic), enable ? 1 : 0);
        }

        public int Write(string id, string service, string characteristic, byte[] data, int length, bool withResponse)
        {
            return rm_ble_write(Utf8(id), Utf8(service), Utf8(characteristic), data, length, withResponse ? 1 : 0);
        }

        public int KeepAliveStart(string id, string service, string characteristic, byte[] data, int length, int periodMs)
        {
            return rm_ble_keepalive_start(Utf8(id), Utf8(service), Utf8(characteristic), data, length, periodMs);
        }

        public int KeepAliveStop(string id)
        {
            return rm_ble_keepalive_stop(Utf8(id));
        }

        public int Pending()
        {
            return rm_ble_pending();
        }

        public int Drain(byte[] buffer, int capacity)
        {
            return rm_ble_drain(buffer, Math.Min(capacity, buffer.Length));
        }

        public int RequestEnable()
        {
            Application.OpenURL(SettingsUri);
            return 0;
        }

        private byte[] Utf8(string text)
        {
            if (_utf8.TryGetValue(text, out var bytes))
            {
                return bytes;
            }

            var raw = Encoding.UTF8.GetBytes(text);
            bytes = new byte[raw.Length + 1];
            Buffer.BlockCopy(raw, 0, bytes, 0, raw.Length);
            if (_utf8.Count < 256)
            {
                _utf8[text] = bytes;
            }

            return bytes;
        }

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_init();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rm_ble_shutdown();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_api_version();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern double rm_clock_monotonic();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_adapter_state();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_start_scan(byte[] servicesCsv);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_stop_scan();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_connect(byte[] id, int highMtu);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_disconnect(byte[] id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_discover(byte[] id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_set_notify(byte[] id, byte[] service, byte[] characteristic, int enable);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_write(byte[] id, byte[] service, byte[] characteristic, byte[] data, int length, int withResponse);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_keepalive_start(byte[] id, byte[] service, byte[] characteristic, byte[] data, int length, int periodMs);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_keepalive_stop(byte[] id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_pending();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_drain(byte[] buffer, int capacity);
    }
}
