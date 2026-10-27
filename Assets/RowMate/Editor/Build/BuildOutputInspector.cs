using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RowMate.EditorTools
{
    // Después de un build NORMAL busca rastros del simulador en la carpeta del build (AC-BLE-13-02, QA-G2-17): ningún
    // RowMate.Simulator*.dll y ninguna de las cadenas propias del simulador, en UTF-8 o UTF-16, en los ensamblados y datos del
    // player. BuildCli falla si encuentra alguno. Busca en *.dll, *.dat (metadatos de IL2CPP) y *.json (los catálogos).
    public static class BuildOutputInspector
    {
        public static readonly string[] Needles =
        {
            "RowMate.Simulator",
            "SimulatedRowerService",
            "SimulatedHeartRateService",
            "SimulatorLayerView",
        };

        private static readonly string[] Extensions = { ".dll", ".dat", ".json" };

        public static List<string> FindSimulatorTraces(string buildFolder)
        {
            var traces = new List<string>();
            if (!Directory.Exists(buildFolder))
            {
                traces.Add("no existe la carpeta del build: " + buildFolder);
                return traces;
            }

            var patterns = new List<(string Needle, byte[] Bytes, string Encoding)>();
            foreach (var needle in Needles)
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
