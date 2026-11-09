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
    // - It. 12 (12-exportar-tcx/02-tech-design §3.6; QA-G12-01; AC-STR-09-28): credenciales y red de Strava (CredentialNeedles). La
    //   palabra "Strava" no es aguja: los restos (StravaActivityId, strava_activity_id, BrandStrava, los textos profile*) se quedan.
    //   FindCredentialTraces corre sobre TODOS los builds (también el del simulador y los de desarrollo).
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
            // It. 5 (QA-V4b-06): la medida de la entrada al inmersivo, solo en desarrollo.
            "-rowmate-perf-entries",
            "-rowmate-no-hud-warmup",
            // It. 5 (05-series-parciales/02-tech-design §3.7): las rutinas de las entradas y la medida de los paneles de series.
            "-rowmate-perf-routines",
            "-rowmate-perf-series",
            // It. 6a (06a-escena-lago/02-tech-design §4.5): la medida de la escena, solo en desarrollo.
            "-rowmate-perf-scene",
            "-rowmate-scene-delay",
            "-rowmate-quality",
            "-rowmate-perf-hours",
            "-rowmate-perf-completion",
            // It. 6a, ronda 1 de la verificación (QA-V6a-13, QA-V6a-REN-03): Serie larga con la escena.
            "-rowmate-perf-long-series",
            // It. 6a, ronda 2 de la verificación (QA-V6a-R2-01): el Profiler con la pila de las asignaciones en Serie larga.
            "-rowmate-perf-profile",
            // It. 8 (08-entornos-selector/02-tech-design §5): el escenario y la hora de la medida de la escena.
            "-rowmate-perf-env",
            "-rowmate-perf-hour",
            // It. 10a (02-tech-design §5.3): el humo del Bluetooth real, solo en desarrollo.
            "-rowmate-ble-smoke",
            // It. 12 (12-exportar-tcx/02-tech-design DT12-20): el humo de "Exportar TCX", solo en desarrollo.
            "-rowmate-export-smoke",
        };

        // It. 12 (QA-G12-01): lo que delataría una integración viva con Strava: endpoints de la API y del OAuth, credenciales y tokens, y
        // el regreso del OAuth.
        public static readonly string[] CredentialNeedles =
        {
            "strava.com/api",
            "strava.com/oauth",
            "api/v3/uploads",
            "client_secret",
            "refresh_token",
            "rowmate://",
        };

        // It. 6a (QA-G6aT-19): el aviso de licencias de terceros (BSD-3 del SDK de Dart, por DartRandom) viaja con cada build de Windows,
        // junto al ejecutable (lo copia NoticesPostprocessor). null si está; si no, el motivo.
        public const string NoticesFile = "THIRD_PARTY_NOTICES.md";

        public static string? FindMissingNotices(string buildFolder)
        {
            var path = Path.Combine(buildFolder, NoticesFile);
            if (!File.Exists(path))
            {
                return "falta " + NoticesFile + " junto al ejecutable (QA-G6aT-19)";
            }

            var text = File.ReadAllText(path);
            return text.Contains("Dart") && text.Contains("Redistribution") ? null : NoticesFile + " sin la licencia del SDK de Dart";
        }

        // It. 10a (02-tech-design §5.3): el build de Windows que no es del simulador lleva el plugin de Bluetooth LE en
        // RowMate_Data/Plugins/x86_64/. null si está; si no, el motivo.
        public const string BlePluginRelative = "RowMate_Data/Plugins/x86_64/RowMateBle.dll";

        public static string? FindMissingBlePlugin(string buildFolder)
        {
            var path = Path.Combine(buildFolder, BlePluginRelative.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(path) ? null : "falta " + BlePluginRelative + " (el remo quedaría sin Bluetooth)";
        }

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
            needles.AddRange(CredentialNeedles);
            return Find(buildFolder, needles);
        }

        // It. 12: solo las credenciales y la red de Strava (todos los builds, AC-STR-09-28).
        public static List<string> FindCredentialTraces(string buildFolder)
        {
            // En el build del simulador sus ensamblados están a propósito: acá solo cuentan las agujas.
            return Find(buildFolder, CredentialNeedles, false);
        }

        // Solo los rastros del simulador (AC-BLE-13-02).
        public static List<string> FindSimulatorTraces(string buildFolder)
        {
            return Find(buildFolder, SimulatorNeedles);
        }

        private static List<string> Find(string buildFolder, IEnumerable<string> needles, bool simulatorAssemblies = true)
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
                if (simulatorAssemblies && name.StartsWith("RowMate.Simulator", StringComparison.OrdinalIgnoreCase))
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
