using System;
using UnityEngine.Profiling;

namespace RowMate.Tests.UiSupport
{
    // Cuántas veces asigna memoria del GC una acción, en este hilo (04b-hud-exr, "Cambios por verificación (ronda 1)"; QA-V4b-MOT-01).
    // Cuenta las muestras "GC.Alloc" del Profiler con un Recorder, como hace AllocatingGCMemoryConstraint del Unity Test Framework,
    // pero devuelve el número para el mensaje de la prueba. En Unity (Mono) GC.GetAllocatedBytesForCurrentThread devuelve siempre 0
    // (medido: 0 con un byte[4096] en el medio), así que las pruebas de "0 B por cuadro" de Unity miden con esto. En dotnet (net8.0)
    // GC.GetAllocatedBytesForCurrentThread sí mide (Tests/EditMode/Workout/AllocatedBytes.cs).
    public static class GcAllocations
    {
        public static int During(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var recorder = Recorder.Get("GC.Alloc");
            // El Recorder nace prendido: apagarlo vacía lo que ya contó (su propia creación).
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                action();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }

            return recorder.sampleBlockCount;
        }
    }
}
