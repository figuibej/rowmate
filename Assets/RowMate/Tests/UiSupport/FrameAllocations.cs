using System;
using System.Collections.Generic;
using RowMate.Presentation.Workout;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Profiling;

namespace RowMate.Tests.UiSupport
{
    // Las asignaciones del GC en CUADROS REALES, con el repintado adentro (05-series-parciales/02-tech-design §3.6, §4.5; QA-G5T-03;
    // DT5-18). GcAllocations.During mide una acción sincrónica y no ve el generateVisualContent, que Unity corre en su loop entre dos
    // `yield return null`: con los paneles de la It. 5 casi todo el trabajo de cada cuadro es repintado. Esto mide, en cada cuadro
    // real, las dos partes de lo que hace la app:
    // - Step: el C# del cuadro (el programador, el MotionDriver, Apply, los callbacks), como GcAllocations.During;
    // - Draws: el generateVisualContent de cada elemento observado (los paneles, por SeriesPanelView.DrawObserver; un elemento de
    //   prueba, con BeginDraw/EndDraw), con el Recorder de "GC.Alloc" del hilo principal prendido solo durante el dibujo.
    // Lo que midió la sonda S-B5 de WP-B0 y la primera corrida de HBT: un contador por cuadro entero (el Recorder entre dos `yield`, o
    // "GC Allocated In Frame") no sirve en batchmode: el corredor de pruebas y lo que dejan las pruebas anteriores asignan miles de
    // veces por cuadro y el control positivo (un byte[64] por repintado) se pierde en esa variación. Medido solo adentro del dibujo y
    // del paso, el control positivo da 1 por repintado y lo limpio da 0, sin línea de base que restar (FrameAllocationsTests).
    // Bytes ("GC Allocated In Frame", todos los hilos) queda solo como dato.
    public sealed class FrameAllocations : IDisposable
    {
        private readonly Recorder _recorder;
        private readonly List<SeriesPanelView> _panels = new List<SeriesPanelView>();
        private readonly Action<bool> _observer;
        private ProfilerRecorder _bytes;
        private bool _stopped;
        private bool _inDraw;

        private FrameAllocations()
        {
            _recorder = Recorder.Get("GC.Alloc");
            _recorder.enabled = false;
            _observer = begin =>
            {
                if (begin)
                {
                    BeginDraw();
                }
                else
                {
                    EndDraw();
                }
            };
            _bytes = StartBytes();
        }

        // Cuadros muestreados (llamadas a Sample).
        public int Frames { get; private set; }

        // Asignaciones adentro de los dibujos observados y cuántos dibujos hubo.
        public int DrawAllocations { get; private set; }

        public int Draws { get; private set; }

        // Asignaciones adentro de los pasos (Step).
        public int StepAllocations { get; private set; }

        public int Total => DrawAllocations + StepAllocations;

        // Bytes de "GC Allocated In Frame" sumados por cuadro (solo como dato: es ruido del Editor y del corredor).
        public long Bytes { get; private set; }

        public static FrameAllocations Start()
        {
            return new FrameAllocations();
        }

        // Observa el dibujo de los paneles (SeriesPanelView.DrawObserver) hasta Stop.
        public static FrameAllocations Watch(params SeriesPanelView[] panels)
        {
            var allocations = new FrameAllocations();
            foreach (var panel in panels)
            {
                panel.DrawObserver = allocations._observer;
                allocations._panels.Add(panel);
            }

            return allocations;
        }

        // El dibujo de un elemento de prueba (el control positivo): BeginDraw al empezar su generateVisualContent, EndDraw al final.
        public void BeginDraw()
        {
            if (_stopped || _inDraw)
            {
                return;
            }

            _inDraw = true;
            _recorder.enabled = false;
            _recorder.FilterToCurrentThread();
            _recorder.enabled = true;
        }

        public void EndDraw()
        {
            if (!_inDraw)
            {
                return;
            }

            _inDraw = false;
            _recorder.enabled = false;
            _recorder.CollectFromAllThreads();
            DrawAllocations += _recorder.sampleBlockCount;
            Draws++;
        }

        // El C# del cuadro (H.Step()), como GcAllocations.During.
        public void Step(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_stopped)
            {
                action();
                return;
            }

            StepAllocations += GcAllocations.During(action);
        }

        // Después de cada `yield return null`: el cuadro que terminó.
        public void Sample()
        {
            if (_stopped)
            {
                return;
            }

            Frames++;
            if (_bytes.Valid)
            {
                Bytes += _bytes.LastValue;
            }
        }

        public void Stop()
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            foreach (var panel in _panels)
            {
                if (panel.DrawObserver == _observer)
                {
                    panel.DrawObserver = null;
                }
            }

            if (_bytes.Valid)
            {
                _bytes.Stop();
            }
        }

        public void Dispose()
        {
            Stop();
            _bytes.Dispose();
        }

        public override string ToString()
        {
            return Total + " asignaciones (" + DrawAllocations + " en " + Draws + " dibujos y " + StepAllocations + " en los pasos) en " + Frames
                   + " cuadros (" + Bytes + " B de GC Allocated In Frame, todo el proceso)";
        }

        private static ProfilerRecorder StartBytes()
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (var handle in handles)
            {
                if (ProfilerRecorderHandle.GetDescription(handle).Name == "GC Allocated In Frame")
                {
                    var recorder = new ProfilerRecorder(handle, 2, ProfilerRecorderOptions.Default);
                    recorder.Start();
                    return recorder;
                }
            }

            return default;
        }
    }
}
