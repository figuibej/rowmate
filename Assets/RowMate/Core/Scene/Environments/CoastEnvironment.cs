namespace RowMate.Core.Scene
{
    // Costa (scene/environments/coast_environment.dart:12-48; AC-SCN-14-08, AC-SCN-17-01 a -03): agua 0,8 / 4,0 m teñida al 30 %
    // con #0E6B7A; solo la orilla izquierda. Si k mod 5 = 2, playa con 3 sombrillas (por cada una u de x, u de z y la variante);
    // si no, dos acantilados en z₀ y z₀ + 25 (por cada uno u de escala y la variante). Un faro en (−14; z₀ + 30) cuando
    // k mod 20 = 10, sin consumir. Tope visible 60.
    public sealed class CoastEnvironment : ShoreEnvironment
    {
        public const double CliffX = -14;   // coast_environment.dart:16

        public const uint TintTone = 0xFF0E6B7A;   // :21
        public const double TintAmount = 0.3;

        private static readonly SceneColor Tint = SceneColor.FromArgb(TintTone);

        public override EnvironmentId Id => EnvironmentId.Coast;

        public override double WaveAmplitude => 0.8;   // :19

        public override double WaveScale => 4.0;       // :20

        public override int MaxVisibleProps => 60;     // :25

        // 3 sombrillas o 2 acantilados, más el faro.
        public override int MaxPropsPerSegment => 4;

        // :21: Color.lerp(base; #0E6B7A; 0,3).
        public override SceneColor TintWater(in SceneColor baseColor)
        {
            return SceneColor.Lerp(baseColor, Tint, TintAmount);
        }

        public override void Generate(long k, DartRandom rnd, ShorePropBuffer into)
        {
            var z0 = k * SegmentLength;
            if (k % 5 == 2)
            {
                for (var i = 0; i < 3; i++)
                {
                    var x = CliffX + 1 + rnd.NextDouble() * 3;
                    var z = z0 + 8 + i * 14 + rnd.NextDouble() * 6;
                    var seed = rnd.NextInt(1000);
                    into.Add(new ShoreProp(x, z, PropKind.BeachUmbrella, 1.0, seed));
                }
            }
            else
            {
                for (var c = 0; c < 2; c++)
                {
                    var z = c == 0 ? z0 + 0.0 : z0 + 25.0;
                    var scale = 0.8 + rnd.NextDouble() * 0.5;
                    var seed = rnd.NextInt(1000);
                    into.Add(new ShoreProp(CliffX, z, PropKind.Cliff, scale, seed));
                }
            }

            if (k % 20 == 10)
            {
                into.Add(new ShoreProp(CliffX, z0 + 30, PropKind.Lighthouse));
            }
        }
    }
}
