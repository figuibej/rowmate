using System;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using UnityEngine.Android;

namespace RowMate.Platform.Ble
{
    // IBlePermissions de Android (10a 02-tech-design §4.3; DT10-10, DT10-15, QA-G10a-02). Delgado: la regla (cuándo pedir, cuándo
    // Unauthorized, cuándo abrir los ajustes) es de NativeBleAdapter. Permisos: API >= 31 BLUETOOTH_SCAN + BLUETOOTH_CONNECT
    // ("Dispositivos cercanos"); 26-30 ACCESS_FINE_LOCATION. Estado:
    //   concedido → Granted; si no, sin negativa recordada → NotAsked (la primera vez Dispositivo muestra la búsqueda);
    //   negativa recordada y el sistema todavía explica (shouldShowRequestPermissionRationale) → Denied; si no → DeniedForever
    //   (el diálogo ya no aparece: "Activar Bluetooth" abre los ajustes de la app).
    // La negativa se recuerda en el plugin (SharedPreferences "rm_ble"), no en el prefs.json de la app, y se borra al quedar
    // concedido. La respuesta del diálogo llega en un hilo de Java: pasa al hilo principal por MainThreadQueue (DT10-10).
    // Sin #if (DT10-19); solo se crea en Android. NO VERIFICABLE sin el módulo de Android y un teléfono.
    public sealed class AndroidBlePermissions : IBlePermissions
    {
        public const string BluetoothScan = "android.permission.BLUETOOTH_SCAN";
        public const string BluetoothConnect = "android.permission.BLUETOOTH_CONNECT";
        public const string FineLocation = "android.permission.ACCESS_FINE_LOCATION";

        private readonly AndroidBleApi _api;
        private readonly MainThreadQueue _mainThread;
        private readonly IAppLogger _logger;
        private BlePermissionState? _state;
        private TaskCompletionSource<BlePermissionState>? _request;
        private int _answers;
        private bool _anyDenied;
        private string[]? _permissions;

        public AndroidBlePermissions(AndroidBleApi api, MainThreadQueue mainThread, IAppLogger logger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public BlePermissionState State
        {
            get
            {
                if (!_state.HasValue)
                {
                    Refresh();
                }

                return _state!.Value;
            }
        }

        // Los permisos de esta versión de Android (el primero es el que decide la explicación del sistema).
        public static string[] RequiredFor(int sdkInt)
        {
            return sdkInt >= 31 ? new[] { BluetoothScan, BluetoothConnect } : new[] { FineLocation };
        }

        // Regla pura del estado (la usa Refresh con lo que lee del plugin).
        public static BlePermissionState Classify(bool granted, bool wasDenied, bool showRationale)
        {
            if (granted)
            {
                return BlePermissionState.Granted;
            }

            if (!wasDenied)
            {
                return BlePermissionState.NotAsked;
            }

            return showRationale ? BlePermissionState.Denied : BlePermissionState.DeniedForever;
        }

        public Task<BlePermissionState> RequestAsync()
        {
            if (_request != null)
            {
                return _request.Task;
            }

            Refresh();
            if (_state == BlePermissionState.Granted)
            {
                return Task.FromResult(BlePermissionState.Granted);
            }

            var request = new TaskCompletionSource<BlePermissionState>();
            _request = request;
            _answers = 0;
            _anyDenied = false;
            _permissions = RequiredFor(_api.SdkInt());
            try
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => _mainThread.Post(() => OnAnswer(false));
                callbacks.PermissionDenied += _ => _mainThread.Post(() => OnAnswer(true));
                callbacks.PermissionRequestDismissed += _ => _mainThread.Post(Finish);
                Permission.RequestUserPermissions(_permissions, callbacks);
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Error, AndroidBleApi.Tag, "Pedido de permiso: " + error.Message, error);
                Finish();
            }

            return request.Task;
        }

        public void Refresh()
        {
            var sdk = _api.SdkInt();
            var required = RequiredFor(sdk);
            var granted = _api.HasScanPermission();
            var wasDenied = !granted && _api.WasDenied();
            if (granted && _api.WasDenied())
            {
                _api.SetDenied(false);
            }

            var rationale = wasDenied && _api.ShouldShowRationale(required[0]);
            _state = Classify(granted, wasDenied, rationale);
        }

        public void OpenAppSettings()
        {
            _api.OpenAppSettings();
        }

        // Hilo principal. Un callback por permiso pedido; con todos, termina.
        private void OnAnswer(bool denied)
        {
            if (_request == null)
            {
                return;
            }

            _anyDenied |= denied;
            _answers++;
            if (_answers >= (_permissions?.Length ?? 1))
            {
                Finish();
            }
        }

        private void Finish()
        {
            var request = _request;
            if (request == null)
            {
                return;
            }

            _request = null;
            if (_anyDenied)
            {
                _api.SetDenied(true);
            }

            Refresh();
            request.TrySetResult(_state!.Value);
        }
    }
}
