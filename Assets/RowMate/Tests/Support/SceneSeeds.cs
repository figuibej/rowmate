using System;
using RowMate.Core.Scene;

namespace RowMate.Tests.Support
{
    // Los estados de prueba de la escena (06a-escena-lago/01-po-spec §0.4; 02-tech-design §1.1): las distancias forzadas, los
    // siete tamaños y el escenario del tope (AC-SCN-14-05).
    public static class SceneSeeds
    {
        // E-0 (el arranque), E-920 (la cabaña a 40 m a la derecha y el muelle a 55 m a la izquierda), E-1234 (la de los tests del
        // legacy) y E-42K (42 195 m).
        public const double E0 = 0;
        public const double E920 = 920;
        public const double E1234 = 1234;
        public const double E42K = 42195;

        // El tiempo de escena de las tomas y de sky_shore_painter_test (3,2 s).
        public const double TakeSceneTime = 3.2;

        // Los siete tamaños de la spec (§0.3): los del legacy (V390, H844) y los de la app.
        public static readonly (string Name, double Width, double Height)[] Sizes =
        {
            ("V390", 390, 844),
            ("H844", 844, 390),
            ("TV", 411, 914),
            ("P360", 360, 800),
            ("TH", 914, 411),
            ("L640", 640, 360),
            ("W1280", 1280, 720),
        };

        public static SceneCamera Camera(string name)
        {
            foreach (var s in Sizes)
            {
                if (s.Name == name)
                {
                    return new SceneCamera(s.Width, s.Height);
                }
            }

            throw new ArgumentException("tamaño desconocido: " + name, nameof(name));
        }

        // La hora armada como el legacy (scene_view.dart:84-85) para el minuto m del día: hora + minuto/60.
        public static double HourOfMinute(int minute)
        {
            return (double)(minute / 60) + (minute % 60) / 60.0;
        }
    }

    // El escenario de prueba del tope (AC-SCN-14-05; QA-G6aT-15 a): muchas boyas (segmentos 0 a 19) y muchos hitos (desde el 20),
    // con z distintas alrededor de cualquier corte, tope 40. Es el mismo de tools/oracle/engine/test/scene_vectors_test.dart
    // (CapEnvironment): cualquier cambio va en los dos lados. Usa el índice de la regata (como el de Dart) para la semilla.
    public sealed class CapEnvironment : ShoreEnvironment
    {
        public const int Cap = 40;

        private static readonly PropKind[] Landmarks =
        {
            PropKind.Grandstand, PropKind.FinishTower, PropKind.Flag, PropKind.DistanceMarker, PropKind.Lamp, PropKind.Pine,
        };

        public override EnvironmentId Id => EnvironmentId.Regatta;

        public override int MaxVisibleProps => Cap;

        public override int MaxPropsPerSegment => 16;

        public override double WaveAmplitude => 0;

        public override double WaveScale => 1;

        public override void Generate(long k, DartRandom rnd, ShorePropBuffer into)
        {
            var z0 = k * SegmentLength;
            var buoyHeavy = k < 20;
            var buoys = buoyHeavy ? 12 : 2;
            for (var i = 0; i < buoys; i++)
            {
                var z = z0 + i * (50.0 / buoys) + 0.25 + rnd.NextDouble() * 1.5;
                var x = (i % 2 == 0 ? -1 : 1) * (1 + rnd.NextDouble() * 6);
                into.Add(new ShoreProp(x, z, PropKind.LaneBuoy, 1.0, i));
            }

            var marks = buoyHeavy ? 2 : 9;
            for (var i = 0; i < marks; i++)
            {
                var z = z0 + i * (50.0 / marks) + 2.0 + rnd.NextDouble() * 2.0;
                var kind = Landmarks[rnd.NextInt(Landmarks.Length)];
                var x = (i % 2 == 0 ? -1 : 1) * (10 + rnd.NextDouble() * 8);
                into.Add(new ShoreProp(x, z, kind, 1.0, (int)(k * 100 + i)));
            }
        }
    }
}
