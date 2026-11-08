namespace RowMate.Core.Ble.Native
{
    // Las funciones planas del plugin nativo (10a 02-tech-design §2.6): rm_ble_* de RowMateBle.dll (Windows, P/Invoke en
    // Platform/Ble/WindowsBleApi) y RmBle.* de Kotlin (Android, JNI en Platform/Ble/AndroidBleApi). Nada lanza: todo error es un
    // código (≤ 0) o un RequestDone con mensaje. Hilo principal (el plugin tiene sus hilos y guarda los eventos en su búfer).
    // Nunca emparejan, desemparejan ni cambian la radio (AC-BLE-SEC-01).
    public interface INativeBleApi
    {
        // 0 bien. Idempotente.
        int Init();

        void Shutdown();

        // BleWire.ApiVersion; otra → NativeBleAdapter queda Unsupported.
        int ApiVersion { get; }

        // Android filtra la búsqueda en el sistema y necesita keepalive nativo en segundo plano; Windows, ninguna de las dos.
        bool FiltersScanInOs { get; }

        bool NeedsNativeKeepAlive { get; }

        // El reloj de los eventos (QPC / System.nanoTime), en segundos.
        double MonotonicSeconds();

        // Lectura inicial (orden de BleAdapterState); después llegan eventos AdapterState.
        int AdapterState();

        // 0 bien; Windows ignora el filtro (DT10-07); Android: BleWire.ErrorPermission sin permiso.
        int StartScan(string servicesCsv);

        int StopScan();

        // reqId > 0 (termina con RequestDone), o un código ≤ 0. Sobre un id con sesión, el plugin primero la cierra (DT10-17).
        int Connect(string id, bool highMtu);

        // Cierra la sesión del id (o cancela su connect pendiente). Idempotente: sin sesión, 0.
        int Disconnect(string id);

        int Discover(string id);

        int SetNotify(string id, string service, string characteristic, bool enable);

        int Write(string id, string service, string characteristic, byte[] data, int length, bool withResponse);

        int KeepAliveStart(string id, string service, string characteristic, byte[] data, int length, int periodMs);

        int KeepAliveStop(string id);

        // Bytes pendientes en el búfer nativo; 0 → no se llama a Drain (sin asignar).
        int Pending();

        // Copia registros ENTEROS hasta capacity; devuelve los bytes copiados.
        int Drain(byte[] buffer, int capacity);

        // "Activar Bluetooth": Android, ACTION_REQUEST_ENABLE (BleWire.ErrorPermission si el sistema lo niega); Windows, los ajustes
        // de Bluetooth (ms-settings:bluetooth). 0 bien. Nunca cambia la radio por su cuenta. (El diseño la tenía void; Kotlin
        // devuelve el código, así que la API también.)
        int RequestEnable();
    }
}
