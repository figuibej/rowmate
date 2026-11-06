namespace RowMate.Core.Scene
{
    // Un escenario: constantes de la orilla, semilla por segmento, oleaje y los objetos de cada segmento
    // (scene/environment.dart:39-84). La caché de segmentos NO está acá: es de cada ShoreSegments (DT6a-15).
    public abstract class ShoreEnvironment
    {
        public const double SegmentLength = 50;   // environment.dart:43
        public const double BankX = 9;            // :46: |x| mínimo de la orilla (el canal tiene 18 m)
        public const double VisibleRange = 400;   // :47

        public abstract EnvironmentId Id { get; }

        // Tope de objetos dibujados por cuadro (:50); cada escenario lo ajusta a su densidad.
        public virtual int MaxVisibleProps => 120;

        // Lo más que puede dar Generate para un segmento (dimensiona los búferes del pool, sin asignar después).
        public virtual int MaxPropsPerSegment => 24;

        // 0-1, amplitud del oleaje (:57) y metros entre crestas (:61); los usa el agua avanzada de la 6b.
        public abstract double WaveAmplitude { get; }

        public abstract double WaveScale { get; }

        // El agua del escenario (:63): el lago no la tiñe.
        public virtual SceneColor TintWater(in SceneColor baseColor)
        {
            return baseColor;
        }

        // La semilla del segmento k (:77): id.index·1 000 003 + k·7 919 + 17, en enteros de 64 bits como la VM de Dart.
        public static long SeedFor(EnvironmentId id, long k)
        {
            return unchecked((long)id * 1000003L + k * 7919L + 17L);
        }

        // Llena `into` (sin vaciarlo) con los objetos del segmento k en el orden del legacy (z ∈ [50k; 50(k + 1)]).
        public abstract void Generate(long k, DartRandom rnd, ShorePropBuffer into);
    }
}
