namespace RowMate.Core.Scene
{
    // Lago alpino (scene/environments/lake_environment.dart:1-40): agua calma (0,25 / 2,5 m), pinos en las dos orillas, un
    // muelle cada 400 m y una cabaña cada 600 m. Las tiradas del generador en el orden del legacy (AC-SCN-15-02): por orilla
    // (izquierda y después derecha) la z inicial; por pino x, escala, variante y el paso a la z siguiente; con los mismos
    // operandos en el mismo orden (la paridad bit a bit depende de eso).
    public sealed class LakeEnvironment : ShoreEnvironment
    {
        public override EnvironmentId Id => EnvironmentId.Lake;

        public override double WaveAmplitude => 0.25;   // lake_environment.dart:14

        public override double WaveScale => 2.5;       // :16

        public override void Generate(long k, DartRandom rnd, ShorePropBuffer into)
        {
            var z0 = k * SegmentLength;
            for (var s = 0; s < 2; s++)
            {
                var side = s == 0 ? -1.0 : 1.0;
                var z = z0 + rnd.NextDouble() * 6;
                while (z < z0 + 50)
                {
                    // Los argumentos con nombre de Dart se evalúan en el orden escrito: x, (z), escala, variante.
                    var x = side * (BankX + 1 + rnd.NextDouble() * 6);
                    var scale = 0.7 + rnd.NextDouble() * 0.6;
                    var seed = rnd.NextInt(1000);
                    into.Add(new ShoreProp(x, z, PropKind.Pine, scale, seed));
                    z += 6 + rnd.NextDouble() * 6;
                }
            }

            if (k % 8 == 3)
            {
                into.Add(new ShoreProp(-BankX, z0 + 25, PropKind.Pier));
            }

            if (k % 12 == 7)
            {
                into.Add(new ShoreProp(BankX + 4, z0 + 10, PropKind.Cabin, 1.0, (int)k));
            }
        }
    }
}
