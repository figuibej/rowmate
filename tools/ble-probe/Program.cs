using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Native;

namespace RowMate.BleProbe
{
    // tools/ble-probe (It. 10a, 02-tech-design §8.5; el S-1 pendiente): busca con RowMateBle.dll y, al terminar, lista SOLO los
    // equipos que anuncian FTMS (0x1826) o ritmo cardíaco (0x180D), con la dirección enmascarada (--show la muestra entera, para la
    // demo del usuario). Los demás equipos se cuentan y se descartan sin mostrar nada de ellos. Nunca conecta, empareja ni cambia la
    // radio; no escribe archivos.
    //
    //   dotnet run --project tools/ble-probe -- [--seconds 10] [--show] [--selftest]
    //
    // --selftest (QA): init, estado, 3 s de búsqueda, stop y shutdown; imprime solo contadores.
    // Códigos: 0 bien; 1 la DLL no cargó o no inició; 2 el adaptador no está encendido; 3 la búsqueda falló; 64 argumentos.
    internal static class Program
    {
        private static readonly BleUuid HeartRate = BleUuid.FromShort(0x180D);

        private static int Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Console.OutputEncoding = Encoding.UTF8;
            var seconds = 10;
            var show = false;
            var selftest = false;
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--seconds":
                        if (i + 1 >= args.Length || !int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds) || seconds < 1 || seconds > 120)
                        {
                            Console.Error.WriteLine("--seconds: de 1 a 120");
                            return 64;
                        }

                        i++;
                        break;
                    case "--show":
                        show = true;
                        break;
                    case "--selftest":
                        selftest = true;
                        break;
                    default:
                        Console.Error.WriteLine("uso: ble-probe [--seconds 10] [--show] [--selftest]");
                        return 64;
                }
            }

            if (selftest)
            {
                seconds = 3;
            }

            if (!NativeMethods.Load(out var error))
            {
                Console.Error.WriteLine("[ble-probe] " + error);
                return 1;
            }

            if (NativeMethods.rm_ble_api_version() != BleWire.ApiVersion || NativeMethods.rm_ble_init() != 0)
            {
                Console.Error.WriteLine("[ble-probe] el plugin no inició (versión " + NativeMethods.rm_ble_api_version() + ")");
                return 1;
            }

            var sink = new ProbeSink();
            try
            {
                var state = (BleAdapterState)NativeMethods.rm_ble_adapter_state();
                Console.WriteLine("[ble-probe] adaptador: " + state);
                if (state != BleAdapterState.On)
                {
                    Console.WriteLine("[ble-probe] el Bluetooth no está encendido: no se busca (la herramienta nunca lo enciende)");
                    return 2;
                }

                var buffer = new byte[BleWire.MaxRecordBytes];
                if (NativeMethods.rm_ble_start_scan(Encoding.UTF8.GetBytes(FtmsUuids.ServiceText + "\0")) != 0)
                {
                    Console.WriteLine("[ble-probe] no se pudo empezar la búsqueda");
                    return 3;
                }

                Console.WriteLine("[ble-probe] buscando " + seconds + " s…");
                var until = DateTime.UtcNow.AddSeconds(seconds);
                while (DateTime.UtcNow < until)
                {
                    Thread.Sleep(100);
                    Drain(buffer, sink);
                }

                NativeMethods.rm_ble_stop_scan();
                Thread.Sleep(200);
                Drain(buffer, sink);
            }
            finally
            {
                NativeMethods.rm_ble_shutdown();
            }

            var ftms = 0;
            var hr = 0;
            var others = 0;
            var lines = new List<string>();
            foreach (var device in sink.Devices.Values)
            {
                var isFtms = device.Services.Contains(FtmsUuids.Service);
                var isHr = device.Services.Contains(HeartRate);
                if (!isFtms && !isHr)
                {
                    others++;
                    continue;
                }

                if (isFtms)
                {
                    ftms++;
                }

                if (isHr)
                {
                    hr++;
                }

                var kind = isFtms && isHr ? "FTMS+pulso" : isFtms ? "FTMS" : "pulso";
                lines.Add(kind.PadRight(11) + " " + (device.Name ?? "(sin nombre)").PadRight(24) + " " + device.Rssi.ToString(CultureInfo.InvariantCulture).PadLeft(4)
                          + " dBm  " + (show ? device.Id : BleIds.Mask(device.Id)));
            }

            if (selftest)
            {
                Console.WriteLine("[ble-probe] selftest: estado=On eventos=" + sink.Events + " ftms=" + ftms + " pulso=" + hr + " descartados=" + others
                                  + " fallas=" + sink.Failures + " formato=" + (sink.FormatErrors == 0 ? "ok" : "mal"));
                return sink.Failures == 0 && sink.FormatErrors == 0 ? 0 : 3;
            }

            Console.WriteLine("[ble-probe] remos FTMS: " + ftms + ", pulsómetros: " + hr + " (otros " + others + " equipos descartados sin mostrar)");
            lines.Sort(StringComparer.Ordinal);
            foreach (var line in lines)
            {
                Console.WriteLine("  " + line);
            }

            if (sink.Failures > 0)
            {
                Console.WriteLine("[ble-probe] la búsqueda falló " + sink.Failures + " vez/veces: " + sink.LastFailure);
                return 3;
            }

            return 0;
        }

        private static void Drain(byte[] buffer, ProbeSink sink)
        {
            while (NativeMethods.rm_ble_pending() > 0)
            {
                var length = NativeMethods.rm_ble_drain(buffer, buffer.Length);
                if (length <= 0)
                {
                    return;
                }

                try
                {
                    BleEventReader.Read(buffer, length, sink);
                }
                catch (FormatException)
                {
                    sink.FormatErrors++;
                    return;
                }
            }
        }

        private sealed class Device
        {
            public Device(string id)
            {
                Id = id;
            }

            public string Id { get; }

            public string? Name { get; set; }

            public int Rssi { get; set; }

            public HashSet<BleUuid> Services { get; } = new HashSet<BleUuid>();
        }

        private sealed class ProbeSink : IBleEventSink
        {
            public Dictionary<string, Device> Devices { get; } = new Dictionary<string, Device>(StringComparer.Ordinal);

            public int Events { get; private set; }

            public int Failures { get; private set; }

            public int FormatErrors { get; set; }

            public string LastFailure { get; private set; } = "";

            public void OnAdapterState(double t, int state)
            {
                Events++;
            }

            public void OnScanResult(double t, string id, string? name, int rssi, BleUuid[] services)
            {
                Events++;
                if (!Devices.TryGetValue(id, out var device))
                {
                    device = new Device(id);
                    Devices[id] = device;
                }

                if (!string.IsNullOrEmpty(name))
                {
                    device.Name = name;
                }

                device.Rssi = rssi;
                for (var i = 0; i < services.Length; i++)
                {
                    device.Services.Add(services[i]);
                }
            }

            public void OnScanFailed(double t, int code, string? message)
            {
                Events++;
                Failures++;
                LastFailure = (message ?? "") + " (" + code + ")";
            }

            public void OnConnection(double t, string id, bool connected, int code)
            {
                Events++;
            }

            public void OnRequestDone(double t, int reqId, int status, ReadOnlySpan<byte> rest)
            {
                Events++;
            }

            public void OnNotify(double t, string id, BleUuid characteristic, byte[] value)
            {
                Events++;
            }

            public void OnDropped(double t, int count)
            {
                Events++;
            }
        }
    }
}
