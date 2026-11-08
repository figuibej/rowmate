namespace RowMate.Core.Scene
{
    // Río urbano (scene/environments/river_environment.dart:14-42; AC-SCN-14-08, AC-SCN-16-01 a -03): agua 0,35 / 2,0 m sin
    // tinte; por orilla (izquierda y después derecha) edificios (70 %) y clubes (30 %) desde z₀ + u·8 cada 15 + u·15 m, faroles
    // en ±9,5 a z₀ + 12,5 y z₀ + 37,5 y la boya amarilla en ±3 a z₀ + 25; un puente en x 0 a z₀ + 20 cuando k mod 16 = 5. Las
    // tiradas en el orden del legacy: z inicial; por casa el tipo, x, escala, variante y el paso. Faroles, boya y puente no
    // consumen.
    public sealed class RiverEnvironment : ShoreEnvironment
    {
        public override EnvironmentId Id => EnvironmentId.River;

        public override double WaveAmplitude => 0.35;   // river_environment.dart:15

        public override double WaveScale => 2.0;         // :17

        // Por orilla ≤ 4 casas (paso ≥ 15 m en 50 m), 2 faroles y una boya; más el puente: 2·7 + 1 = 15 (16 de margen).
        public override int MaxPropsPerSegment => 16;

        public override void Generate(long k, DartRandom rnd, ShorePropBuffer into)
        {
            var z0 = k * SegmentLength;
            for (var s = 0; s < 2; s++)
            {
                var side = s == 0 ? -1.0 : 1.0;
                var z = z0 + rnd.NextDouble() * 8;
                while (z < z0 + 50)
                {
                    var boathouse = rnd.NextDouble() < 0.3;
                    // Los argumentos con nombre de Dart se evalúan en el orden escrito: x, (z), (tipo), escala, variante.
                    var x = side * (BankX + 4 + rnd.NextDouble() * 4);
                    var scale = 0.6 + rnd.NextDouble() * 0.4;
                    var seed = rnd.NextInt(1000);
                    into.Add(new ShoreProp(x, z, boathouse ? PropKind.Boathouse : PropKind.Building, scale, seed));
                    z += 15 + rnd.NextDouble() * 15;
                }

                // :36-38: for (z = z₀ + 12,5; z < z₀ + 50; z += 25) (12,5 y 37,5: exactos en double).
                for (var lz = z0 + 12.5; lz < z0 + 50; lz += 25)
                {
                    into.Add(new ShoreProp(side * (BankX + 0.5), lz, PropKind.Lamp));
                }

                into.Add(new ShoreProp(side * 3, z0 + 25, PropKind.LaneBuoy, 1.0, 1));
            }

            if (k % 16 == 5)
            {
                into.Add(new ShoreProp(0, z0 + 20, PropKind.Bridge, 1.0, (int)k));
            }
        }
    }
}
