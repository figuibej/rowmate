using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

namespace RowMate.Tests.UiSupport
{
    // Cuántas veces asigna memoria del GC una acción, en este hilo (04b-hud-exr, "Cambios por verificación (ronda 1)"; QA-V4b-MOT-01).
    // Cuenta las muestras "GC.Alloc" del Profiler y devuelve el número para el mensaje de la prueba. En Unity (Mono)
    // GC.GetAllocatedBytesForCurrentThread devuelve siempre 0 (medido: 0 con un byte[4096] en el medio), así que las pruebas de "0 B por
    // cuadro" de Unity miden con esto. En dotnet (net8.0) GC.GetAllocatedBytesForCurrentThread sí mide (Tests/EditMode/Workout/AllocatedBytes.cs).
    // It. 16 (#78, #99; 16-tests-intermitentes/02-tech-design): cuenta con un ProfilerRecorder con CollectOnlyOnCurrentThread
    // (GcAllocCounter), no con el Recorder de UnityEngine.Profiling con FilterToCurrentThread (el de AllocatingGCMemoryConstraint): ese
    // Recorder se lleva a veces UNA muestra de otro hilo que asigna mientras se prende o se apaga (medido: 177 de 1000 medidas de 200 µs
    // con un hilo que asigna sin parar; 0 de 1000 con el ProfilerRecorder). Era el intermitente de BleAllocationTests.
    public static class GcAllocations
    {
        [ThreadStatic]
        private static Stack<GcAllocCounter>? _free;

        public static int During(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var free = _free ??= new Stack<GcAllocCounter>();
            // Uno por medida en curso (una medida adentro de otra usa otro contador del mismo hilo).
            var counter = free.Count > 0 ? free.Pop() : new GcAllocCounter();
            var count = 0;
            counter.Begin();
            try
            {
                action();
            }
            finally
            {
                count = counter.End();
                free.Push(counter);
            }

            return count;
        }
    }

    // Las muestras "GC.Alloc" de UN hilo entre Begin y End (It. 16): un ProfilerRecorder del marcador "GC.Alloc" con
    // CollectOnlyOnCurrentThread, creado en el hilo que mide (el principal en las pruebas), que no ve las asignaciones de los otros
    // hilos (el de los finalizadores, el ThreadPool, los del corredor de pruebas). Cada asignación es una muestra; con más de Capacity
    // en una medida, la cuenta queda en Capacity (Saturated): alcanza para cualquier tope de las pruebas (0, o unas decenas).
    public sealed class GcAllocCounter : IDisposable
    {
        public const int Capacity = 4096;

        private static ProfilerRecorderHandle _handle;
        private static bool _handleFound;

        private ProfilerRecorder _recorder;
        private readonly int _threadId;

        public GcAllocCounter()
        {
            _threadId = Environment.CurrentManagedThreadId;
            _recorder = new ProfilerRecorder(Handle(), Capacity, ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
        }

        // Lo que contó la última medida (End).
        public int Last { get; private set; }

        public bool Saturated { get; private set; }

        public void Begin()
        {
            if (Environment.CurrentManagedThreadId != _threadId)
            {
                throw new InvalidOperationException("GcAllocCounter: se mide en el hilo que lo creó.");
            }

            _recorder.Reset();
            _recorder.Start();
        }

        public int End()
        {
            _recorder.Stop();
            var count = _recorder.Count;
            Saturated = count >= Capacity;
            Last = Math.Min(count, Capacity);
            return Last;
        }

        public void Dispose()
        {
            _recorder.Dispose();
        }

        private static ProfilerRecorderHandle Handle()
        {
            if (_handleFound)
            {
                return _handle;
            }

            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (var handle in handles)
            {
                if (ProfilerRecorderHandle.GetDescription(handle).Name == "GC.Alloc")
                {
                    _handle = handle;
                    _handleFound = true;
                    return handle;
                }
            }

            throw new InvalidOperationException("GcAllocCounter: el Profiler no tiene el marcador GC.Alloc.");
        }
    }
}
