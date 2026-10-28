using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RowMate.EditorTools
{
    // Después de un build NORMAL busca en la carpeta del build lo que ese build no puede llevar; BuildCli falla (código 2) si
    // encuentra algo. En UTF-8 y UTF-16, en los ensamblados y los datos del player: *.dll, *.dat (metadatos de IL2CPP), *.json,
    // *.assets (resources.assets, sharedassets*.assets) y los archivos sin extensión (level0, globalgamemanagers).
    // - Simulador (AC-BLE-13-02, QA-G2-17): ningún RowMate.Simulator*.dll, ni sus tipos, ni los textos que el simulador
    //   muestra (QA-V2-10: "Remo simulado", "Pulsómetro simulado", la cinta "SIMULADOR").
    // - Diagnósticos de desarrollo (QA-V2-09): los argumentos -rowmate-color-probe y -rowmate-ui-pipeline (AppRoot y
    //   ColorProbe solo los compilan en el Editor y en builds de desarrollo) y, desde la It. 3, -rowmate-db-smoke (el humo de
    //   la base) y la variable ROWMATE_DATA_DIR (la carpeta de datos de las pruebas, Bootstrap/DataLocation).
    // - Metadatos de tests (QA-V2-02): PerformanceTestRunInfo / PerformanceTestRunSettings, que el paquete
    //   com.unity.test-framework.performance deja en Assets/Resources (PerformanceTestRunCleaner los borra antes de juntar).
    public static class BuildOutputInspector
    {
        public static readonly string[] SimulatorNeedles =
        {
            "RowMate.Simulator",
            "SimulatedRowerService",
            "SimulatedHeartRateService",
            "SimulatorLayerView",
            "Remo simulado",
            "Pulsómetro simulado",
            "SIMULADOR",
        };

        public static readonly string[] DiagnosticNeedles =
        {
            "-rowmate-color-probe",
            "-rowmate-ui-pipeline",
            "-rowmate-db-smoke",
            "-rowmate-db-roundtrip",
            "ROWMATE_DATA_DIR",
        };

        public static readonly string[] TestNeedles =
        {
            "PerformanceTestRunInfo",
            "PerformanceTestRunSettings",
        };

        private static readonly string[] Extensions = { ".dll", ".dat", ".json", ".assets", "" };

        // Todo lo que un build normal no puede llevar (lo que usa BuildCli).
        public static List<string> FindForbiddenTraces(string buildFolder)
        {
            var needles = new List<string>(SimulatorNeedles);
            needles.AddRange(DiagnosticNeedles);
            needles.AddRange(TestNeedles);
            return Find(buildFolder, needles);
        }

        // Solo los rastros del simulador (AC-BLE-13-02).
        public static List<string> FindSimulatorTraces(string buildFolder)
        {
            return Find(buildFolder, SimulatorNeedles);
        }

        private static List<string> Find(string buildFolder, IEnumerable<string> needles)
        {
            var traces = new List<string>();
            if (!Directory.Exists(buildFolder))
            {
                traces.Add("no existe la carpeta del build: " + buildFolder);
                return traces;
            }

            var patterns = new List<(string Needle, byte[] Bytes, string Encoding)>();
            foreach (var needle in needles)
            {
                patterns.Add((needle, Encoding.UTF8.GetBytes(needle), "UTF-8"));
                patterns.Add((needle, Encoding.Unicode.GetBytes(needle), "UTF-16"));
            }

            foreach (var file in Directory.GetFiles(buildFolder, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(file);
                if (name.StartsWith("RowMate.Simulator", StringComparison.OrdinalIgnoreCase))
                {
                    traces.Add(file + ": ensamblado del simulador");
                    continue;
                }

                if (Array.IndexOf(Extensions, Path.GetExtension(file).ToLowerInvariant()) < 0)
                {
                    continue;
                }

                var bytes = File.ReadAllBytes(file);
                foreach (var pattern in patterns)
                {
                    if (IndexOf(bytes, pattern.Bytes) >= 0)
                    {
                        traces.Add(file + ": contiene \"" + pattern.Needle + "\" (" + pattern.Encoding + ")");
                    }
                }
            }

            return traces;
        }

        private static int IndexOf(byte[] haystack, byte[] needle)
        {
            if (needle.Length == 0 || haystack.Length < needle.Length)
            {
                return -1;
            }

            var first = needle[0];
            var last = haystack.Length - needle.Length;
            for (var i = 0; i <= last; i++)
            {
                if (haystack[i] != first)
                {
                    continue;
                }

                var j = 1;
                while (j < needle.Length && haystack[i + j] == needle[j])
                {
                    j++;
                }

                if (j == needle.Length)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
