#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Text;
using UnityEngine;

namespace RowMate.Bootstrap
{
    // El humo del Bluetooth real en el player (10a 02-tech-design §5.3; AC-BLE-04-07, AC-BLE-05-10, AC-BLE-91-02): con
    // -rowmate-ble-smoke (solo desarrollo; lo corre tools/build/build.ps1 -BleSmoke con ROWMATE_DATA_DIR temporal) lee el estado del
    // adaptador, hace 3 búsquedas de 10 s con el RowerService de la app (la segunda con la ventana minimizada: el loop sigue con
    // runInBackground) y sale. NUNCA conecta, empareja ni cambia la radio; el registro lleva solo contadores (ningún nombre ni
    // dirección):
    //   [BLE] humo: estado=On busquedas=3 ftms=N fallas=0 colgado=no soloFtms=si
    // Pasa (sale con 0) con el adaptador On, las 3 búsquedas terminadas sin falla ni cuelgue y la lista solo con FTMS; si no, 6.
    // It. 11 (DT11-16; AC-HRM-02-06): después, 1 búsqueda de pulsómetros de 10 s con el HeartRateService de la app, SIN conectar a nada
    // (un pulsómetro real cercano queda solo en el Player.log local, con la dirección enmascarada). Solo contadores:
    //   [HRM] humo: busquedas=1 sensores=N ajenos=0 fallas=0
    // ajenos = sensores listados que no anuncian 0x180D. Pasa con busquedas=1, ajenos=0 y fallas=0.
    internal static class BleSmoke
    {
        public const string Argument = "-rowmate-ble-smoke";
        public const string Tag = "BLE";
        public const string HrmTag = "HRM";
        public const int FailureCode = 6;

        private static readonly TimeSpan SearchCap = TimeSpan.FromSeconds(15);

        public static async Task RunAsync(IRowerService rower, IHeartRateService heartRate, IScheduler scheduler, IAppLogger logger)
        {
            var rowerOk = await RunRowerAsync(rower, scheduler, logger);
            var hrmOk = rower.AdapterState.Value == BleAdapterState.On && await RunHeartRateAsync(heartRate, scheduler, logger);
            Application.Quit(rowerOk && hrmOk ? 0 : FailureCode);
        }

        // La búsqueda del pulsómetro (10 s, el plazo del servicio). Nunca toca ConnectAsync.
        private static async Task<bool> RunHeartRateAsync(IHeartRateService heartRate, IScheduler scheduler, IAppLogger logger)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var foreign = 0;
            var searches = 0;
            var failures = 0;
            try
            {
                using (heartRate.Devices.Subscribe(list =>
                       {
                           for (var i = 0; i < list.Count; i++)
                           {
                               if (seen.Add(list[i].DeviceId) && !ScanFilter.AdvertisesService(list[i], HeartRateParser.Service))
                               {
                                   foreign++;
                               }
                           }
                       }))
                {
                    var scan = heartRate.StartScanAsync();
                    var cap = scheduler.Delay(SearchCap);
                    if (await Task.WhenAny(scan, cap) != scan)
                    {
                        failures++;
                        logger.Log(LogLevel.Warning, HrmTag, "humo: la búsqueda de pulsómetros no terminó en 15 s");
                    }
                    else
                    {
                        try
                        {
                            await scan;
                            if (heartRate.Status.Value == HrmStatus.Disconnected)
                            {
                                searches++;
                            }
                            else
                            {
                                failures++;
                            }
                        }
                        catch (Exception error)
                        {
                            failures++;
                            logger.Log(LogLevel.Warning, HrmTag, "humo: la búsqueda de pulsómetros falló (" + error.Message + ")");
                        }
                    }
                }
            }
            catch (Exception error)
            {
                failures++;
                logger.Log(LogLevel.Error, HrmTag, "humo: falla (" + error.GetType().Name + ")", error);
            }

            var ok = searches == 1 && foreign == 0 && failures == 0;
            logger.Log(ok ? LogLevel.Info : LogLevel.Error, HrmTag, "humo: busquedas=" + Fmt.Integer(searches) + " sensores=" + Fmt.Integer(seen.Count)
                + " ajenos=" + Fmt.Integer(foreign) + " fallas=" + Fmt.Integer(failures));
            return ok;
        }

        private static async Task<bool> RunRowerAsync(IRowerService rower, IScheduler scheduler, IAppLogger logger)
        {
            try
            {
                // La primera lectura del estado (el plugin la hace al iniciar; como mucho 5 s más).
                for (var i = 0; i < 20 && rower.AdapterState.Value == BleAdapterState.Unknown; i++)
                {
                    await scheduler.Delay(TimeSpan.FromMilliseconds(250));
                }

                var state = rower.AdapterState.Value;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var onlyFtms = true;
                var searches = 0;
                var failures = 0;
                var hung = false;
                using (rower.Devices.Subscribe(list =>
                       {
                           for (var i = 0; i < list.Count; i++)
                           {
                               seen.Add(list[i].DeviceId);
                               onlyFtms &= ScanFilter.AdvertisesService(list[i], FtmsUuids.Service);
                           }
                       }))
                {
                    if (state == BleAdapterState.On)
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            if (i == 1)
                            {
                                Window.Minimize();
                            }

                            var scan = rower.StartScanAsync();
                            var cap = scheduler.Delay(SearchCap);
                            if (await Task.WhenAny(scan, cap) != scan)
                            {
                                hung = true;
                                break;
                            }

                            try
                            {
                                await scan;
                                if (rower.Status.Value == BleStatus.Disconnected)
                                {
                                    searches++;
                                }
                                else
                                {
                                    failures++;
                                }
                            }
                            catch (Exception error)
                            {
                                failures++;
                                logger.Log(LogLevel.Warning, Tag, "humo: la búsqueda " + Fmt.Integer(i + 1) + " falló (" + error.Message + ")");
                            }

                            if (i == 1)
                            {
                                Window.Restore();
                            }
                        }
                    }
                }

                var ok = state == BleAdapterState.On && searches == 3 && failures == 0 && !hung && onlyFtms;
                logger.Log(ok ? LogLevel.Info : LogLevel.Error, Tag, "humo: estado=" + state + " busquedas=" + Fmt.Integer(searches) + " ftms="
                    + Fmt.Integer(seen.Count) + " fallas=" + Fmt.Integer(failures) + " colgado=" + (hung ? "si" : "no") + " soloFtms=" + (onlyFtms ? "si" : "no"));
                return ok;
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, Tag, "humo: falla (" + error.GetType().Name + ")", error);
                return false;
            }
        }

        // La ventana del player (solo Windows; en el Editor no hace nada).
        private static class Window
        {
            private const int SwMinimize = 6;
            private const int SwRestore = 9;

            public static void Minimize()
            {
                Show(SwMinimize);
            }

            public static void Restore()
            {
                Show(SwRestore);
            }

            private static void Show(int command)
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                try
                {
                    var handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        ShowWindow(handle, command);
                    }
                }
                catch (Exception)
                {
                    // Sin ventana (batchmode): la búsqueda sigue igual.
                }
#endif
            }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            [DllImport("user32.dll")]
            private static extern bool ShowWindow(IntPtr window, int command);
#endif
        }
    }
}
#endif
