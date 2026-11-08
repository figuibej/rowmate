using System;
using System.Collections.Generic;
using RowMate.Core.Ble.Native;
using RowMate.Core.Common;
using UnityEngine;

namespace RowMate.Platform.Ble
{
    // INativeBleApi sobre el plugin de Kotlin (com.rowmate.ble.RmBle, Assets/Plugins/Android/RowMateBle; 10a 02-tech-design §4.3).
    // AndroidJNI crudo: la clase (referencia global) y los jmethodID en caché, y los jvalue[] reusados, así pending() de cada cuadro
    // no asigna; drain() solo con pending > 0 (la única asignación: el byte[] de los eventos reales, que se guarda y se entrega en
    // registros enteros hasta la capacidad del búfer de NativeBleAdapter). Los nombres y firmas JNI salen SOLO de
    // RmBleJniSignatures (QA-G10a-05). Las cadenas (ids y UUID se repiten) viajan como referencias globales en caché.
    // Sin #if (DT10-19): compila en todas las plataformas (UnityEngine.AndroidJNIModule está también en el Editor de Windows),
    // pero solo se crea en Android (BleAdapterFactory). Hilo principal (el de Unity, ya adjunto a la JVM).
    public sealed class AndroidBleApi : INativeBleApi
    {
        public const string Tag = "BLE";

        private const int StringCacheLimit = 128;

        private static readonly jvalue[] None = new jvalue[0];

        private readonly IAppLogger _logger;
        private readonly jvalue[] _one = new jvalue[1];
        private readonly jvalue[] _two = new jvalue[2];
        private readonly jvalue[] _four = new jvalue[4];
        private readonly jvalue[] _five = new jvalue[5];
        private readonly Dictionary<string, IntPtr> _strings = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
        private sbyte[]? _carry;
        private int _carryOffset;
        private int _carryLength;
        private bool _initialized;

        public AndroidBleApi(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int ApiVersion => RmBleJni.Bind(_logger) ? CallInt(RmBleJniSignatures.ApiVersion.Name, None) : 0;

        // Android filtra la búsqueda en el sistema (FTMS) y necesita el keepalive nativo en segundo plano (DT10-09).
        public bool FiltersScanInOs => true;

        public bool NeedsNativeKeepAlive => true;

        public int Init()
        {
            if (!RmBleJni.Bind(_logger))
            {
                return BleWire.ErrorUnsupported;
            }

            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _one[0] = new jvalue { l = activity.GetRawObject() };
                    var code = CallInt(RmBleJniSignatures.Init.Name, _one);
                    _one[0] = default;
                    _initialized = code == 0;
                    return code;
                }
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Error, Tag, "RmBle.init: " + error.Message, error);
                return BleWire.ErrorGeneric;
            }
        }

        // Para los permisos: RmBle necesita el contexto antes de leerlos (init es idempotente).
        internal bool EnsureInitialized()
        {
            return _initialized || Init() == 0;
        }

        public void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            _initialized = false;
            CallVoid(RmBleJniSignatures.Shutdown.Name, None);
            ReleaseStrings();
            _carry = null;
            _carryOffset = 0;
            _carryLength = 0;
        }

        public double MonotonicSeconds()
        {
            return RmBleJni.Bind(_logger) ? CallDouble(RmBleJniSignatures.MonotonicSeconds.Name, None) : 0;
        }

        public int AdapterState()
        {
            return CallInt(RmBleJniSignatures.AdapterState.Name, None);
        }

        public int StartScan(string servicesCsv)
        {
            _one[0] = new jvalue { l = Str(servicesCsv ?? "") };
            return CallInt(RmBleJniSignatures.StartScan.Name, _one);
        }

        public int StopScan()
        {
            return CallInt(RmBleJniSignatures.StopScan.Name, None);
        }

        public int Connect(string id, bool highMtu)
        {
            _two[0] = new jvalue { l = Str(id) };
            _two[1] = new jvalue { z = highMtu };
            return CallInt(RmBleJniSignatures.Connect.Name, _two);
        }

        public int Disconnect(string id)
        {
            _one[0] = new jvalue { l = Str(id) };
            return CallInt(RmBleJniSignatures.Disconnect.Name, _one);
        }

        public int Discover(string id)
        {
            _one[0] = new jvalue { l = Str(id) };
            return CallInt(RmBleJniSignatures.Discover.Name, _one);
        }

        public int SetNotify(string id, string service, string characteristic, bool enable)
        {
            _four[0] = new jvalue { l = Str(id) };
            _four[1] = new jvalue { l = Str(service) };
            _four[2] = new jvalue { l = Str(characteristic) };
            _four[3] = new jvalue { z = enable };
            return CallInt(RmBleJniSignatures.SetNotify.Name, _four);
        }

        public int Write(string id, string service, string characteristic, byte[] data, int length, bool withResponse)
        {
            var array = Bytes(data, length);
            try
            {
                _five[0] = new jvalue { l = Str(id) };
                _five[1] = new jvalue { l = Str(service) };
                _five[2] = new jvalue { l = Str(characteristic) };
                _five[3] = new jvalue { l = array };
                _five[4] = new jvalue { z = withResponse };
                return CallInt(RmBleJniSignatures.Write.Name, _five);
            }
            finally
            {
                _five[3] = default;
                DeleteLocal(array);
            }
        }

        public int KeepAliveStart(string id, string service, string characteristic, byte[] data, int length, int periodMs)
        {
            var array = Bytes(data, length);
            try
            {
                _five[0] = new jvalue { l = Str(id) };
                _five[1] = new jvalue { l = Str(service) };
                _five[2] = new jvalue { l = Str(characteristic) };
                _five[3] = new jvalue { l = array };
                _five[4] = new jvalue { i = periodMs };
                return CallInt(RmBleJniSignatures.KeepAliveStart.Name, _five);
            }
            finally
            {
                _five[3] = default;
                DeleteLocal(array);
            }
        }

        public int KeepAliveStop(string id)
        {
            _one[0] = new jvalue { l = Str(id) };
            return CallInt(RmBleJniSignatures.KeepAliveStop.Name, _one);
        }

        // Cada cuadro: sin asignar. Lo que quedó del último drain va primero.
        public int Pending()
        {
            var carried = _carryLength - _carryOffset;
            if (carried > 0)
            {
                return carried;
            }

            return _initialized ? CallInt(RmBleJniSignatures.Pending.Name, None) : 0;
        }

        // Registros ENTEROS hasta capacity. Kotlin entrega hasta 64 KiB por llamada (BleWire.MaxRecordBytes); si no entran, el
        // resto queda para el próximo Drain (Pending lo cuenta).
        public int Drain(byte[] buffer, int capacity)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            capacity = Math.Min(capacity, buffer.Length);
            if (_carryLength - _carryOffset <= 0)
            {
                if (!_initialized || !FetchCarry())
                {
                    return 0;
                }
            }

            var carry = _carry!;
            var written = 0;
            while (_carryOffset + BleWire.HeaderSize <= _carryLength)
            {
                var length = BleWire.HeaderSize + ((byte)carry[_carryOffset + 2] | ((byte)carry[_carryOffset + 3] << 8));
                if (_carryOffset + length > _carryLength)
                {
                    _logger.Log(LogLevel.Error, Tag, "RmBle.drain: registro cortado");
                    _carryOffset = _carryLength;
                    break;
                }

                if (length > capacity)
                {
                    // No entra nunca (el plugin no escribe registros de más de 64 KiB): se descarta para no trabar la bomba.
                    _logger.Log(LogLevel.Warning, Tag, "RmBle.drain: registro de " + length + " bytes descartado");
                    _carryOffset += length;
                    continue;
                }

                if (written + length > capacity)
                {
                    break;
                }

                Buffer.BlockCopy(carry, _carryOffset, buffer, written, length);
                written += length;
                _carryOffset += length;
            }

            if (_carryOffset >= _carryLength)
            {
                _carry = null;
                _carryOffset = 0;
                _carryLength = 0;
            }

            return written;
        }

        // "Activar Bluetooth" (AC-BLE-04-04): el diálogo del sistema; BleWire.ErrorPermission → NativeBleAdapter abre los ajustes.
        public int RequestEnable()
        {
            return CallInt(RmBleJniSignatures.RequestEnable.Name, None);
        }

        // ── Para AndroidBlePermissions ──

        internal bool HasScanPermission()
        {
            return EnsureInitialized() && CallBool(RmBleJniSignatures.HasScanPermission.Name, None);
        }

        internal bool ShouldShowRationale(string permission)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            _one[0] = new jvalue { l = Str(permission) };
            return CallBool(RmBleJniSignatures.ShouldShowRationale.Name, _one);
        }

        internal bool WasDenied()
        {
            return EnsureInitialized() && CallBool(RmBleJniSignatures.WasDenied.Name, None);
        }

        internal void SetDenied(bool denied)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            _one[0] = new jvalue { z = denied };
            CallVoid(RmBleJniSignatures.SetDenied.Name, _one);
        }

        internal void OpenAppSettings()
        {
            if (EnsureInitialized())
            {
                CallVoid(RmBleJniSignatures.OpenAppSettings.Name, None);
            }
        }

        internal int SdkInt()
        {
            return RmBleJni.Bind(_logger) ? CallInt(RmBleJniSignatures.SdkInt.Name, None) : 0;
        }

        // ── JNI ──

        private bool FetchCarry()
        {
            var method = RmBleJni.Method(RmBleJniSignatures.Drain.Name);
            if (method == IntPtr.Zero)
            {
                return false;
            }

            var array = AndroidJNI.CallStaticObjectMethod(RmBleJni.Class, method, None);
            if (!RmBleJni.Check(_logger, RmBleJniSignatures.Drain.Name) || array == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                _carry = AndroidJNI.FromSByteArray(array);
            }
            finally
            {
                AndroidJNI.DeleteLocalRef(array);
            }

            _carryOffset = 0;
            _carryLength = _carry?.Length ?? 0;
            return _carryLength > 0;
        }

        private int CallInt(string name, jvalue[] args)
        {
            var method = RmBleJni.Method(name);
            if (method == IntPtr.Zero)
            {
                return BleWire.ErrorUnsupported;
            }

            var result = AndroidJNI.CallStaticIntMethod(RmBleJni.Class, method, args);
            return RmBleJni.Check(_logger, name) ? result : BleWire.ErrorGeneric;
        }

        private bool CallBool(string name, jvalue[] args)
        {
            var method = RmBleJni.Method(name);
            if (method == IntPtr.Zero)
            {
                return false;
            }

            var result = AndroidJNI.CallStaticBooleanMethod(RmBleJni.Class, method, args);
            return RmBleJni.Check(_logger, name) && result;
        }

        private double CallDouble(string name, jvalue[] args)
        {
            var method = RmBleJni.Method(name);
            if (method == IntPtr.Zero)
            {
                return 0;
            }

            var result = AndroidJNI.CallStaticDoubleMethod(RmBleJni.Class, method, args);
            return RmBleJni.Check(_logger, name) ? result : 0;
        }

        private void CallVoid(string name, jvalue[] args)
        {
            var method = RmBleJni.Method(name);
            if (method == IntPtr.Zero)
            {
                return;
            }

            AndroidJNI.CallStaticVoidMethod(RmBleJni.Class, method, args);
            RmBleJni.Check(_logger, name);
        }

        // Referencia global en caché (los ids y los UUID se repiten: el keepalive no crea cadenas cada 2 s).
        private IntPtr Str(string text)
        {
            if (_strings.TryGetValue(text, out var cached))
            {
                return cached;
            }

            if (_strings.Count >= StringCacheLimit)
            {
                ReleaseStrings();
            }

            var local = AndroidJNI.NewStringUTF(text);
            var global = AndroidJNI.NewGlobalRef(local);
            AndroidJNI.DeleteLocalRef(local);
            _strings[text] = global;
            return global;
        }

        private void ReleaseStrings()
        {
            foreach (var entry in _strings)
            {
                AndroidJNI.DeleteGlobalRef(entry.Value);
            }

            _strings.Clear();
        }

        // El byte[] de Java (jbyte es con signo: Unity 6 pide sbyte[]). Una copia por escritura: cada 2 s, no por cuadro.
        private static IntPtr Bytes(byte[] data, int length)
        {
            var count = data == null ? 0 : Math.Max(0, Math.Min(length, data.Length));
            var signed = new sbyte[count];
            if (count > 0)
            {
                Buffer.BlockCopy(data!, 0, signed, 0, count);
            }

            return AndroidJNI.ToSByteArray(signed);
        }

        private static void DeleteLocal(IntPtr reference)
        {
            if (reference != IntPtr.Zero)
            {
                AndroidJNI.DeleteLocalRef(reference);
            }
        }
    }

    // La clase com.rowmate.ble.RmBle y sus jmethodID, compartidos por AndroidBleApi y AndroidHaptics. Una sola vez por proceso.
    internal static class RmBleJni
    {
        private static readonly Dictionary<string, IntPtr> Methods = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
        private static bool _tried;

        public static IntPtr Class { get; private set; }

        public static bool Bind(IAppLogger logger)
        {
            if (Class != IntPtr.Zero)
            {
                return true;
            }

            if (_tried)
            {
                return false;
            }

            _tried = true;
            try
            {
                using (var type = new AndroidJavaClass(RmBleJniSignatures.ClassName.Replace('/', '.')))
                {
                    var raw = type.GetRawClass();
                    if (raw == IntPtr.Zero)
                    {
                        logger.Log(LogLevel.Error, AndroidBleApi.Tag, "No está la clase " + RmBleJniSignatures.ClassName);
                        return false;
                    }

                    var global = AndroidJNI.NewGlobalRef(raw);
                    foreach (var (name, signature) in RmBleJniSignatures.All)
                    {
                        var id = AndroidJNI.GetStaticMethodID(global, name, signature);
                        if (!Check(logger, name) || id == IntPtr.Zero)
                        {
                            logger.Log(LogLevel.Error, AndroidBleApi.Tag, "RmBle sin el método " + name + signature);
                            AndroidJNI.DeleteGlobalRef(global);
                            Methods.Clear();
                            return false;
                        }

                        Methods[name] = id;
                    }

                    Class = global;
                    return true;
                }
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, AndroidBleApi.Tag, "No se pudo cargar " + RmBleJniSignatures.ClassName + ": " + error.Message, error);
                Methods.Clear();
                return false;
            }
        }

        public static IntPtr Method(string name)
        {
            return Class != IntPtr.Zero && Methods.TryGetValue(name, out var id) ? id : IntPtr.Zero;
        }

        // Una excepción de Java pendiente: se limpia (si no, la próxima llamada JNI aborta el proceso) y se registra.
        public static bool Check(IAppLogger logger, string name)
        {
            var exception = AndroidJNI.ExceptionOccurred();
            if (exception == IntPtr.Zero)
            {
                return true;
            }

            AndroidJNI.ExceptionClear();
            AndroidJNI.DeleteLocalRef(exception);
            logger.Log(LogLevel.Error, AndroidBleApi.Tag, "RmBle." + name + " lanzó una excepción de Java");
            return false;
        }
    }
}
