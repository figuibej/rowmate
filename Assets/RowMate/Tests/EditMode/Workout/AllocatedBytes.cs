using System;

namespace RowMate.Tests.EditMode.Workout
{
    // Bytes reservados por el hilo actual, para las pruebas de memoria de la It. 4a (WSR, WBG). Solo con .NET 5 o más nuevo
    // (el proyecto de dotnet es net8.0): en Unity (Mono) devuelve null y la prueba afirma solo el tiempo
    // (tools/dotnet/README.md, "Código que solo existe en net8.0").
    internal static class AllocatedBytes
    {
        public static long? Now()
        {
#if NET5_0_OR_GREATER
            return GC.GetAllocatedBytesForCurrentThread();
#else
            return null;
#endif
        }

        // Lo reservado desde before (null si no se mide).
        public static long? Since(long? before)
        {
            var now = Now();
            return before.HasValue && now.HasValue ? now.Value - before.Value : (long?)null;
        }
    }
}
