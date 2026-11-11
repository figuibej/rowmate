using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace RowMate.EditorTools
{
    // It. 6a, ronda 2 de la verificación (QA-V6a-R2-01; 06a-escena-lago/02-tech-design, "Cambios por verificación (ronda 2)"): lee un
    // registro binario del Profiler de un build de desarrollo (el que graba -rowmate-perf-profile <archivo.raw> en Serie larga) y escribe,
    // por cada lugar del hilo principal que asigna, cuántas asignaciones hubo, cuántos bytes, los marcadores de arriba y la pila de C#
    // (la grabó el player con Profiler.enableAllocationCallstacks). Es lo que en la ventana del Profiler sería "GC.Alloc" con su pila.
    //   Unity -batchmode -quit -projectPath . -executeMethod RowMate.EditorTools.ProfileAllocations.Dump
    //         -rowmate-profile <archivo.raw o carpeta> -rowmate-profile-out <resumen.txt> [-rowmate-profile-filter <marcador>]
    // It. 15 (QA-V15-01, QA-V15-05): con una carpeta, un resumen por cada .raw (los que graba Tests/UiSupport/AllocationProfile en el
    // Editor); con -rowmate-profile-filter, solo las asignaciones que tienen ese marcador arriba (RowMate.Diag.Measured: lo que midió
    // FrameAllocations), con los marcadores de debajo de él.
    public static class ProfileAllocations
    {
        private const int MarkerDepth = 8;
        private const int StackDepth = 40;
        private const int MaxThreads = 128;

        public static void Dump()
        {
            var raw = Argument("-rowmate-profile");
            var output = Argument("-rowmate-profile-out");
            var filter = Argument("-rowmate-profile-filter");
            if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(output) || (!File.Exists(raw) && !Directory.Exists(raw)))
            {
                UnityEngine.Debug.LogError("[PROFILE] hacen falta -rowmate-profile <archivo.raw o carpeta existente> y -rowmate-profile-out <archivo>");
                EditorApplication.Exit(1);
                return;
            }

            var files = Directory.Exists(raw) ? Directory.GetFiles(raw, "*.raw").OrderBy(f => f, StringComparer.Ordinal).ToArray() : new[] { raw };
            var all = new StringBuilder();
            foreach (var file in files)
            {
                if (!ProfilerDriver.LoadProfile(file, false))
                {
                    UnityEngine.Debug.LogError("[PROFILE] no se pudo cargar " + Path.GetFileName(file));
                    EditorApplication.Exit(1);
                    return;
                }

                all.Append("=== ").Append(Path.GetFileName(file)).Append(" ===\n").Append(Summarize(filter)).Append('\n');
            }

            File.WriteAllText(output, all.ToString(), new UTF8Encoding(false));
            UnityEngine.Debug.Log("[PROFILE] " + files.Length + " registros resumidos");
            EditorApplication.Exit(0);
        }

        private static string Summarize(string? filter)
        {
            var first = ProfilerDriver.firstFrameIndex;
            var last = ProfilerDriver.lastFrameIndex;
            var places = new Dictionary<string, Place>();
            var frames = 0;
            var framesWithAllocations = 0;
            var total = 0;
            var callstack = new List<ulong>();
            var ancestors = new List<int>();
            var remaining = new List<int>();
            var otherThreads = 0;
            for (var frame = first; frame <= last; frame++)
            {
                for (var thread = 0; thread < MaxThreads; thread++)
                {
                    using (var view = ProfilerDriver.GetRawFrameDataView(frame, thread))
                    {
                        if (view == null || !view.valid)
                        {
                            if (thread > 0)
                            {
                                break;
                            }

                            continue;
                        }

                        if (thread == 0)
                        {
                            frames++;
                        }

                        var gc = view.GetMarkerId("GC.Alloc");
                        var inFrame = 0;
                        ancestors.Clear();
                        remaining.Clear();
                        for (var i = 0; i < view.sampleCount; i++)
                        {
                            while (remaining.Count > 0 && remaining[remaining.Count - 1] == 0)
                            {
                                remaining.RemoveAt(remaining.Count - 1);
                                ancestors.RemoveAt(ancestors.Count - 1);
                            }

                            if (remaining.Count > 0)
                            {
                                remaining[remaining.Count - 1]--;
                            }

                            var under = -1;
                            if (filter != null && view.GetSampleMarkerId(i) == gc)
                            {
                                for (var a = ancestors.Count - 1; a >= 0; a--)
                                {
                                    if (view.GetSampleName(ancestors[a]) == filter)
                                    {
                                        under = a;
                                        break;
                                    }
                                }
                            }

                            if (view.GetSampleMarkerId(i) == gc && (filter == null || under >= 0))
                            {
                                inFrame++;
                                var key = new StringBuilder();
                                key.Append('[').Append(view.threadName).Append("] ");
                                var from = filter != null ? Math.Max(under, ancestors.Count - 2 * MarkerDepth) : Math.Max(0, ancestors.Count - MarkerDepth);
                                for (var a = from; a < ancestors.Count; a++)
                                {
                                    key.Append(view.GetSampleName(ancestors[a])).Append(" > ");
                                }

                                key.Append("GC.Alloc\n");
                                view.GetSampleCallstack(i, callstack);
                                foreach (var address in callstack.Take(StackDepth))
                                {
                                    var method = view.ResolveMethodInfo(address);
                                    key.Append("      ").Append(string.IsNullOrEmpty(method.methodName) ? "0x" + address.ToString("x", CultureInfo.InvariantCulture) : method.methodName);
                                    if (!string.IsNullOrEmpty(method.sourceFileName))
                                    {
                                        key.Append(" (").Append(Path.GetFileName(method.sourceFileName)).Append(':').Append(method.sourceFileLine.ToString(CultureInfo.InvariantCulture)).Append(')');
                                    }

                                    key.Append('\n');
                                }

                                var text = key.ToString();
                                if (!places.TryGetValue(text, out var place))
                                {
                                    place = new Place();
                                    places[text] = place;
                                }

                                place.Count++;
                                long bytes = 0;
                                if (view.GetSampleMetadataCount(i) > 0)
                                {
                                    bytes = view.GetSampleMetadataAsLong(i, 0);
                                }

                                place.Bytes += bytes;
                                place.LastFrame = frame;
                            }

                            ancestors.Add(i);
                            remaining.Add(view.GetSampleChildrenCount(i));
                        }

                        if (thread == 0)
                        {
                            total += inFrame;
                            if (inFrame > 0)
                            {
                                framesWithAllocations++;
                            }
                        }
                        else
                        {
                            otherThreads += inFrame;
                        }
                    }
                }
            }

            var report = new StringBuilder();
            if (filter != null)
            {
                report.Append("[PROFILE] solo bajo ").Append(filter).Append('\n');
            }

            report.Append("[PROFILE] ").Append(frames).Append(" cuadros del hilo principal, ").Append(framesWithAllocations)
                .Append(" con asignaciones, ").Append(total).Append(" asignaciones en ").Append(places.Count).Append(" lugares; en los otros hilos, ")
                .Append(otherThreads).Append("\n\n");
            foreach (var pair in places.OrderByDescending(p => p.Value.Count))
            {
                report.Append(pair.Value.Count).Append(" asignaciones, ").Append(pair.Value.Bytes).Append(" B:\n  ").Append(pair.Key).Append('\n');
            }

            UnityEngine.Debug.Log("[PROFILE] " + frames + " cuadros, " + framesWithAllocations + " con asignaciones, " + total + " asignaciones");
            return report.ToString();
        }

        private sealed class Place
        {
            public int Count;
            public long Bytes;
            public int LastFrame;
        }

        private static string? Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
