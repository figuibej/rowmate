using System;
using System.Collections.Generic;

namespace RowMate.Core.Scene
{
    // Las líneas del registro de Unity con trabajo de shaders que cuentan contra el precalentado de la escena (06a-escena-lago/02-tech-design
    // §4.5 y "Cambios por verificación (ronda 1)"; QA-V6a-REN-01). Con GraphicsSettings.logWhenShaderIsCompiled el player escribe
    // "Uploaded shader variant to the GPU driver: <shader> (instance …), pass: …, stage: …, keywords …, time: … ms" (y el Editor, "Compiled
    // shader" / "Compiling shader"). Cuenta toda línea de ese tipo de un shader de la escena (RowMate/Scene/*) o de URP (Hidden/Universal
    // Render Pipeline/*: el post, las copias, las sombras) DESPUÉS de la marca que FrameTimeProbe escribe al ver la escena lista: tiene que
    // dar 0 (lo que la escena usa ya se subió en la precarga). La de la UI (Hidden/Internal-UIR*) no cuenta. La misma regla usa el script
    // tools/build/perf-log-check.ps1 sobre el registro del player.
    public static class ShaderWorkLog
    {
        // La marca de FrameTimeProbe (una vez por proceso, el primer cuadro con la escena lista).
        public const string ReadyMark = "[PERF] marca: escena Ready";

        private static readonly string[] Work = { "Uploaded shader variant to the GPU driver", "Compiled shader", "Compiling shader" };

        private static readonly string[] Shaders = { "RowMate/Scene/", "Hidden/Universal Render Pipeline/" };

        public static bool IsReadyMark(string? line)
        {
            return line != null && line.IndexOf(ReadyMark, StringComparison.Ordinal) >= 0;
        }

        // ¿Es una subida (o compilación) de una variante de la escena o de URP?
        public static bool IsSceneShaderWork(string? line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            var work = false;
            for (var i = 0; i < Work.Length && !work; i++)
            {
                work = line!.IndexOf(Work[i], StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!work)
            {
                return false;
            }

            for (var i = 0; i < Shaders.Length; i++)
            {
                if (line!.IndexOf(Shaders[i], StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        // Las de después de la primera marca (en orden). -1 sin la marca (NO VERIFICABLE: la escena no llegó a estar lista).
        public static int CountAfterReady(IEnumerable<string> lines, List<string>? found = null)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            var marked = false;
            var count = 0;
            foreach (var line in lines)
            {
                if (!marked)
                {
                    marked = IsReadyMark(line);
                    continue;
                }

                if (IsSceneShaderWork(line))
                {
                    count++;
                    found?.Add(line);
                }
            }

            return marked ? count : -1;
        }
    }
}
