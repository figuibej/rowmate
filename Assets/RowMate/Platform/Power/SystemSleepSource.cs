using System;
using System.Runtime.InteropServices;
using System.Threading;
using RowMate.Core.Common;
using UnityEngine;

namespace RowMate.Platform.Power
{
    // P-17 de la 4a (AC-WKT-02-11; 04b-hud-exr/02-tech-design §2.9.3; ADR 020): lo que el sistema dice del sueño del equipo, para
    // el FrameClock de cada cuadro (UnityScheduler). Lo lee el hilo principal; los avisos llegan en un hilo del sistema y solo
    // suben contadores.
    public interface ISystemSleepSource : IDisposable
    {
        // Lo dormido según el sistema desde el arranque (s); NaN sin medida.
        double AsleepSeconds { get; }

        // Cuántos avisos de "se suspende" (PBT_APMSUSPEND) y de "reanuda" (PBT_APMRESUMEAUTOMATIC, PBT_APMRESUMESUSPEND) llegaron.
        int SuspendSignals { get; }

        int ResumeSignals { get; }

        // Se registró para los avisos del sistema (solo en el build de Windows).
        bool SignalsRegistered { get; }
    }

    // Android, iOS y quien no tiene la señal (la pantalla bloqueada es segundo plano: Q-91, P-12; el reloj del teléfono con el
    // sueño profundo es RA-19, de la 10a).
    public sealed class NullSleepSource : ISystemSleepSource
    {
        public static readonly NullSleepSource Instance = new NullSleepSource();

        public double AsleepSeconds => double.NaN;

        public int SuspendSignals => 0;

        public int ResumeSignals => 0;

        public bool SignalsRegistered => false;

        public void Dispose()
        {
        }
    }

    // Las llamadas nativas del registro de los avisos (powrprof.dll), detrás de una interfaz para probar el orden de registro y
    // baja sin registrar nada en el sistema (SystemSleepSourceTests, QA-G4bT-05 (d)).
    public interface IPowerNotifyApi
    {
        // DEVICE_NOTIFY_CALLBACK con la estructura DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS en parameters. 0 = bien.
        uint Register(IntPtr parameters, out IntPtr handle);

        uint Unregister(IntPtr handle);
    }

    // Windows (DT4b-06; §2.9.3): (a) la MEDIDA, una lectura que vale también en el Editor: GetTickCount64() cuenta el sueño y
    // QueryUnbiasedInterruptTime() no (Windows 7+), así que (GetTickCount64·10⁴ − QueryUnbiasedInterruptTime) / 10⁷ sube solo
    // mientras el equipo duerme (S3 y S4). (b) Los AVISOS, solo en el build: PowerRegisterSuspendResumeNotification
    // (DEVICE_NOTIFY_CALLBACK, Windows 8+), cuyo callback corre en un hilo del sistema y solo hace Interlocked.Increment. Lleva
    // [AOT.MonoPInvokeCallback] (IL2CPP) y el delegado vive en un campo estático; la estructura vive en Marshal.AllocHGlobal
    // mientras dura el registro: Dispose da de baja y RECIÉN DESPUÉS la libera. En el Editor no se registra: una recarga de
    // dominio dejaría al sistema llamando a código descargado (QA-G4bT-05 (c); DT4b-20). En el modo de espera moderno (S0) el
    // núcleo puede no contar el lapso como sueño: quedan los avisos. Si no llega ninguno, el lapso cuenta como un cuadro tarde.
    public sealed class WindowsSleepSource : ISystemSleepSource
    {
        public const uint DeviceNotifyCallback = 2;
        public const uint PbtApmSuspend = 4;
        public const uint PbtApmResumeSuspend = 7;
        public const uint PbtApmResumeAutomatic = 18;

        private static int _suspendSignals;
        private static int _resumeSignals;
        private static readonly DeviceNotifyCallbackRoutine Callback = OnPowerEvent;

        private readonly IPowerNotifyApi? _api;
        private readonly IAppLogger _logger;
        private IntPtr _parameters;
        private IntPtr _handle;
        private bool _measure = true;
        private bool _disposed;

        // register: pedir los avisos (en el build; nunca en el Editor). api: las llamadas nativas (null = powrprof.dll).
        public WindowsSleepSource(IAppLogger logger, bool register, IPowerNotifyApi? api = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _api = api ?? (register ? new PowrprofApi() : null);
            if (register && _api != null)
            {
                Register();
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint DeviceNotifyCallbackRoutine(IntPtr context, uint type, IntPtr setting);

        public double AsleepSeconds
        {
            get
            {
                if (!_measure)
                {
                    return double.NaN;
                }

                try
                {
                    var tick = GetTickCount64();
                    if (!QueryUnbiasedInterruptTime(out var unbiased))
                    {
                        return double.NaN;
                    }

                    return ((double)tick * 1e4 - unbiased) / 1e7;
                }
                catch (Exception error) when (error is DllNotFoundException || error is EntryPointNotFoundException)
                {
                    _measure = false;
                    _logger.Log(LogLevel.Warning, "TIME", "sin la medida del sueño del núcleo: " + error.GetType().Name);
                    return double.NaN;
                }
            }
        }

        public int SuspendSignals => Volatile.Read(ref _suspendSignals);

        public int ResumeSignals => Volatile.Read(ref _resumeSignals);

        public bool SignalsRegistered => _handle != IntPtr.Zero;

        // El bloque de memoria de la estructura (para la prueba del orden: vive mientras dura el registro).
        public IntPtr Parameters => _parameters;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_handle != IntPtr.Zero && _api != null)
            {
                // Primero la baja: el sistema deja de llamar; recién después se libera la estructura que tenía.
                _api.Unregister(_handle);
                _handle = IntPtr.Zero;
            }

            if (_parameters != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_parameters);
                _parameters = IntPtr.Zero;
            }
        }

        // Para las pruebas: el aviso como si llegara del sistema.
        public static uint SimulatePowerEvent(uint type)
        {
            return OnPowerEvent(IntPtr.Zero, type, IntPtr.Zero);
        }

        private void Register()
        {
            try
            {
                // DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS { PDEVICE_NOTIFY_CALLBACK_ROUTINE Callback; PVOID Context; }
                _parameters = Marshal.AllocHGlobal(2 * IntPtr.Size);
                Marshal.WriteIntPtr(_parameters, 0, Marshal.GetFunctionPointerForDelegate(Callback));
                Marshal.WriteIntPtr(_parameters, IntPtr.Size, IntPtr.Zero);
                var result = _api!.Register(_parameters, out var handle);
                if (result != 0 || handle == IntPtr.Zero)
                {
                    _logger.Log(LogLevel.Warning, "TIME", "sin los avisos de suspensión (código " + result + "): vale solo la medida");
                    Marshal.FreeHGlobal(_parameters);
                    _parameters = IntPtr.Zero;
                    return;
                }

                _handle = handle;
                _logger.Log(LogLevel.Info, "TIME", "avisos de suspensión registrados (P-17)");
            }
            catch (Exception error) when (error is DllNotFoundException || error is EntryPointNotFoundException)
            {
                _logger.Log(LogLevel.Warning, "TIME", "sin los avisos de suspensión: " + error.GetType().Name);
                if (_parameters != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_parameters);
                    _parameters = IntPtr.Zero;
                }
            }
        }

        // Corre en un hilo del sistema: solo sube los contadores (nunca toca la app).
        [AOT.MonoPInvokeCallback(typeof(DeviceNotifyCallbackRoutine))]
        private static uint OnPowerEvent(IntPtr context, uint type, IntPtr setting)
        {
            if (type == PbtApmSuspend)
            {
                Interlocked.Increment(ref _suspendSignals);
            }
            else if (type == PbtApmResumeAutomatic || type == PbtApmResumeSuspend)
            {
                Interlocked.Increment(ref _resumeSignals);
            }

            return 0;
        }

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryUnbiasedInterruptTime(out ulong unbiasedTime);

        private sealed class PowrprofApi : IPowerNotifyApi
        {
            public uint Register(IntPtr parameters, out IntPtr handle)
            {
                return PowerRegisterSuspendResumeNotification(DeviceNotifyCallback, parameters, out handle);
            }

            public uint Unregister(IntPtr handle)
            {
                return PowerUnregisterSuspendResumeNotification(handle);
            }

            [DllImport("powrprof.dll")]
            private static extern uint PowerRegisterSuspendResumeNotification(uint flags, IntPtr recipient, out IntPtr registrationHandle);

            [DllImport("powrprof.dll")]
            private static extern uint PowerUnregisterSuspendResumeNotification(IntPtr registrationHandle);
        }
    }

    // La fuente de la plataforma: Windows (el build registra los avisos; el Editor, solo la medida) o ninguna.
    public static class SleepSourceFactory
    {
        public static ISystemSleepSource Create(IAppLogger logger)
        {
            var platform = Application.platform;
            if (platform == RuntimePlatform.WindowsPlayer)
            {
                return new WindowsSleepSource(logger, true);
            }

            if (platform == RuntimePlatform.WindowsEditor)
            {
                return new WindowsSleepSource(logger, false);
            }

            return NullSleepSource.Instance;
        }
    }
}
