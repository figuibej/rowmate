using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Ble.Native;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // El contrato de RowMateBle.dll (10a 02-tech-design §2.6, §3; AC-BLE-SEC-01): la DLL versionada escribe la fijación v1 byte por
    // byte igual que el codificador de C# (Tests/Golden/ble/events-v1.bin), declara la versión 1, su reloj es la base del Stopwatch
    // (DT10-18) y no lleva rutas de la PC (QA-G10a-01, NHY). Solo Windows x64: en otro sistema, Ignore. Nunca llama rm_ble_init:
    // no toca la radio ni el adaptador.
    [TestFixture]
    [Category("AC-BLE-SEC-01")]
    public sealed class WindowsDllContractTests
    {
        private const string Dll = "RowMateBle";
        private const string RelativeDll = "Assets/Plugins/Windows/x86_64/RowMateBle.dll";

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string path);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_wire_fixture(byte[] buffer, int capacity);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_api_version();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern double rm_clock_monotonic();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rm_ble_pending();

        private static string DllPath => RepoPaths.Combine(RelativeDll);

        private static void RequireWindowsDll()
        {
            if (Path.DirectorySeparatorChar != '\\' || !Environment.Is64BitProcess)
            {
                Assert.Ignore("RowMateBle.dll es de Windows x64.");
            }

            Assert.That(File.Exists(DllPath), Is.True, RelativeDll + " no está versionada (native/windows/build.ps1).");
            // En dotnet la DLL no está junto al ensamblado: se carga por su ruta y el DllImport por nombre la encuentra ya cargada. En
            // el Editor de Unity ya la cargó el importador de plugins (Editor Win64).
            Assert.That(LoadLibraryW(DllPath), Is.Not.EqualTo(IntPtr.Zero), "no se pudo cargar " + RelativeDll);
        }

        [Test]
        public void TheDllWritesTheGoldenFixture()
        {
            RequireWindowsDll();
            var buffer = new byte[4096];
            var length = rm_ble_wire_fixture(buffer, buffer.Length);
            Assert.That(length, Is.GreaterThan(0));
            var actual = new byte[length];
            Buffer.BlockCopy(buffer, 0, actual, 0, length);
            var golden = File.ReadAllBytes(RepoPaths.Combine(BleWireFixture.RelativePath));
            Assert.That(actual, Is.EqualTo(golden), "la DLL escribe la fijación distinta de events-v1.bin");
            Assert.That(rm_ble_wire_fixture(new byte[8], 8), Is.EqualTo(-golden.Length), "con poco lugar devuelve -n");
        }

        [Test]
        public void TheDllDeclaresVersionOneAndNothingPendingWithoutInit()
        {
            RequireWindowsDll();
            Assert.That(rm_ble_api_version(), Is.EqualTo(BleWire.ApiVersion));
            Assert.That(rm_ble_pending(), Is.EqualTo(0));
        }

        // DT10-18: el reloj de los eventos corre al MISMO ritmo que el Stopwatch de la app (los dos son QPC). El origen puede no coincidir:
        // en el Mono de Unity el Stopwatch cuenta desde que arrancó el proceso y QPC desde que arrancó el equipo (medido en el Editor
        // 6000.3.27f1); BleEventClock calibra ese desfase en cada drenaje, así que lo que importa es que sea constante.
        [Test]
        public void TheDllClockRunsAtTheStopwatchRate()
        {
            RequireWindowsDll();
            if (!Stopwatch.IsHighResolution)
            {
                Assert.Ignore("Stopwatch sin QPC.");
            }

            var offset0 = rm_clock_monotonic() - Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
            System.Threading.Thread.Sleep(250);
            var offset1 = rm_clock_monotonic() - Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
            Assert.That(rm_clock_monotonic(), Is.GreaterThan(0));
            Assert.That(Math.Abs(offset1 - offset0), Is.LessThan(0.005), "el desfase QPC − Stopwatch no es constante (DT10-18)");
        }

        // NHY (QA-G10a-01): ninguna ruta de la PC en el binario versionado, en ASCII ni en UTF-16. Corre en cualquier sistema (lee el
        // archivo).
        [Test]
        public void TheVersionedDllHasNoPaths()
        {
            if (!File.Exists(DllPath))
            {
                Assert.Ignore(RelativeDll + " no está (clon sin el plugin).");
            }

            var bytes = File.ReadAllBytes(DllPath);
            var ascii = Encoding.GetEncoding(28591).GetString(bytes);
            var wide0 = Encoding.Unicode.GetString(bytes);
            var wide1 = Encoding.Unicode.GetString(bytes, 1, bytes.Length - 1);
            foreach (var needle in new[] { "C:\\", "\\Users\\", "\\Windows Kits\\", "Microsoft Visual Studio" })
            {
                foreach (var haystack in new[] { ascii, wide0, wide1 })
                {
                    Assert.That(haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase), Is.LessThan(0), "la DLL contiene '" + needle + "'");
                }
            }
        }

        // NHY: las fuentes nativas propias, sin macros de archivo ni APIs de emparejado o de la radio (AC-BLE-SEC-01). Lo mismo que
        // revisa native/windows/build.ps1 antes de compilar.
        [Test]
        public void TheNativeSourcesHaveNoFileMacrosNorPairing()
        {
            var folder = RepoPaths.Combine("native/windows/RowMateBle");
            if (!Directory.Exists(folder))
            {
                Assert.Ignore("native/windows no está.");
            }

            var forbidden = new[]
            {
                "__FILE__", "__builtin_FILE", "source_location", "assert(", "PairAsync", "DeviceInformationPairing", "UnpairAsync",
                "SetStateAsync", "RequestAccessAsync",
            };
            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            Assert.That(files.Length, Is.GreaterThan(0));
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                foreach (var pattern in forbidden)
                {
                    Assert.That(text.Contains(pattern), Is.False, Path.GetFileName(file) + " contiene '" + pattern + "'");
                }
            }
        }
    }
}
