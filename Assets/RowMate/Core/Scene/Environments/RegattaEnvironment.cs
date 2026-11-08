using System;

namespace RowMate.Core.Scene
{
    // Canal de regata (scene/environments/regatta_environment.dart:12-40; AC-SCN-14-08, AC-SCN-18-01 a -03): agua 0,3 / 1,5 m sin
    // tinte; boyas cada 10 m en x −9, −3, 3 y 9 con el color del tramo de 250 m ((k div 5) mod 3); cartel en (−10,5; z₀) cada
    // 250 m con los metros; tribuna en (12; z₀ + 10) si k mod 20 = 4; torre en (−12; z₀ + 10) si k mod 20 = 12; bandera a z₀ + 30,
    // a la derecha con k par. No consume el generador. Tope visible 200.
    public sealed class RegattaEnvironment : ShoreEnvironment
    {
        private static readonly double[] LaneX = { -9.0, -3.0, 3.0, 9.0 };   // regatta_environment.dart:28

        public override EnvironmentId Id => EnvironmentId.Regatta;

        public override double WaveAmplitude => 0.3;   // :17

        public override double WaveScale => 1.5;       // :19

        public override int MaxVisibleProps => 200;    // :23

        // 20 boyas, cartel, tribuna o torre y la bandera.
        public override int MaxPropsPerSegment => 23;

        // El color de las boyas del segmento k: (k ~/ 5) % 3 (k ≥ 0).
        public static int BuoyColor(long k)
        {
            return (int)(k / 5 % 3);
        }

        public override void Generate(long k, DartRandom rnd, ShorePropBuffer into)
        {
            var z0 = k * SegmentLength;
            var color = BuoyColor(k);
            // :27: for (z = z₀; z < z₀ + 50; z += 10): z es un double que se suma (exacto: múltiplos de 10).
            for (var z = z0; z < z0 + 50; z += 10)
            {
                for (var i = 0; i < LaneX.Length; i++)
                {
                    into.Add(new ShoreProp(LaneX[i], z, PropKind.LaneBuoy, 1.0, color));
                }
            }

            if (k % 5 == 0)
            {
                // (k·50).round(): k·50 ya es entero (exacto en double), el truncado da lo mismo.
                into.Add(new ShoreProp(-10.5, z0, PropKind.DistanceMarker, 1.0, (int)(k * SegmentLength)));
            }

            if (k % 20 == 4)
            {
                into.Add(new ShoreProp(12, z0 + 10, PropKind.Grandstand));
            }

            if (k % 20 == 12)
            {
                into.Add(new ShoreProp(-12, z0 + 10, PropKind.FinishTower));
            }

            into.Add(new ShoreProp(k % 2 == 0 ? 10.5 : -10.5, z0 + 30, PropKind.Flag, 1.0, (int)k));
        }
    }
}
