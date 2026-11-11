using System;
using System.IO;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace RowMate.Tests.UiSupport
{
    // It. 15 (QA-V15-01, QA-V15-05; 15-deuda-pc/02-tech-design, "Cambios por verificación"): la PILA de cada asignación de una medida de
    // FrameAllocations, no solo la cuenta. Solo con la variable ROWMATE_ALLOC_PROFILE (una carpeta): mientras dura la medida, el Profiler
    // graba un registro binario con Profiler.enableAllocationCallstacks en <carpeta>/<etiqueta>.raw; sin la variable no hace nada (las
    // corridas normales no lo prenden). Lo que mide FrameAllocations (el paso, el reloj, el panel y cada dibujo observado) va adentro del
    // marcador RowMate.Diag.Measured, así el resumen (RowMate.EditorTools.ProfileAllocations.Dump con -rowmate-profile-filter) cuenta solo
    // eso y no lo que asigna el corredor de pruebas entre dos cuadros.
    public static class AllocationProfile
    {
        public const string Variable = "ROWMATE_ALLOC_PROFILE";
        public const string MarkerName = "RowMate.Diag.Measured";

        public static readonly ProfilerMarker Measured = new ProfilerMarker(MarkerName);

        private static bool _recording;

        public static string? Folder
        {
            get
            {
                var folder = Environment.GetEnvironmentVariable(Variable);
                return string.IsNullOrEmpty(folder) ? null : folder;
            }
        }

        public static bool IsRecording => _recording;

        // Empieza a grabar (si la variable está puesta y no hay otra grabación). Devuelve si grabó.
        public static bool Begin(string tag)
        {
            var folder = Folder;
            if (folder == null || _recording || !Profiler.supported)
            {
                return false;
            }

            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, tag + ".raw");
            for (var n = 2; File.Exists(file); n++)
            {
                file = Path.Combine(folder, tag + "-" + n + ".raw");
            }

            Profiler.maxUsedMemory = 256 * 1024 * 1024;
            Profiler.logFile = file;
            Profiler.enableBinaryLog = true;
            Profiler.enableAllocationCallstacks = true;
            Profiler.enabled = true;
            _recording = true;
            UnityEngine.Debug.Log("[ALLOC-PROFILE] grabando " + Path.GetFileName(file));
            return true;
        }

        // Una etiqueta de archivo con las letras ASCII del texto (a lo sumo 24).
        public static string Slug(string text)
        {
            var slug = new System.Text.StringBuilder();
            foreach (var c in text ?? "")
            {
                if (slug.Length >= 24)
                {
                    break;
                }

                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                {
                    slug.Append(c);
                }
                else if (c == ' ' && slug.Length > 0 && slug[slug.Length - 1] != '-')
                {
                    slug.Append('-');
                }
            }

            return slug.ToString();
        }

        public static void End()
        {
            if (!_recording)
            {
                return;
            }

            Profiler.enabled = false;
            Profiler.enableBinaryLog = false;
            Profiler.enableAllocationCallstacks = false;
            Profiler.logFile = "";
            _recording = false;
        }
    }
}
