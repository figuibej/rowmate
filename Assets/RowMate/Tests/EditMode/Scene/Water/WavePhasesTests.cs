using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Water;
using RowMate.Core.Text;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // WPH · Las olas sin salto a los 4096 m (Q-68; AC-6B-20; 00-arte-agua §3.3): todo lo que depende de la distancia de escena se reduce
    // en double; el shader trabaja en cinta y nunca ve D. La distancia acotada del legacy sigue siendo el contrato de otra cosa
    // (AC-SCN-03-06), no del agua.
    [TestFixture]
    public sealed class WavePhasesTests
    {
        private static readonly EnvironmentId[] All = { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };

        private static WaterPhaseSet At(double d, double t, in WaterProfile p)
        {
            var set = default(WaterPhaseSet);
            WavePhases.Compute(d, t, p, ref set);
            return set;
        }

        // El avance de cada corrimiento (fases en radianes; mapas y ruido en vueltas) entre dos distancias, envuelto.
        private static double[] Advance(in WaterPhaseSet a, in WaterPhaseSet b)
        {
            return new[]
            {
                WavePhases.WrappedDelta(a.Phase1, b.Phase1), WavePhases.WrappedDelta(a.Phase2, b.Phase2),
                Turns(a.B2U, b.B2U), Turns(a.B2V, b.B2V), Turns(a.B3U, b.B3U), Turns(a.B3V, b.B3V), Turns(a.B4U, b.B4U), Turns(a.B4V, b.B4V),
                Turns(a.NoiseV, b.NoiseV),
            };
        }

        private static double Turns(double a, double b)
        {
            return DartMath.Mod(b - a + 0.5, 1.0) - 0.5;
        }

        [Test, Category("AC-6B-20")]
        public void NoJumpAt4096()
        {
            // (a) la fase de cada ola, calculada a 4095,9 y a 4096,1 m, avanza lo mismo que entre 0,0 y 0,2 m (± 1e-6 rad).
            foreach (var id in All)
            {
                var p = WaterProfiles.For(id);
                foreach (var t in new[] { 0.0, 3.2, 1234.5 })
                {
                    var start = Advance(At(0.0, t, p), At(0.2, t, p));
                    var across = Advance(At(4095.9, t, p), At(4096.1, t, p));
                    var far = Advance(At(42194.9, t, p), At(42195.1, t, p));
                    for (var i = 0; i < start.Length; i++)
                    {
                        Assert.That(across[i], Is.EqualTo(start[i]).Within(1e-6), id + " corrimiento " + i + " a 4096 m");
                        Assert.That(far[i], Is.EqualTo(start[i]).Within(1e-6), id + " corrimiento " + i + " a 42 195 m");
                    }
                }
            }
        }

        [Test, Category("AC-6B-20")]
        public void TapeMatchesWorld()
        {
            // La ola en cinta (relZ con las fases) es la del mundo (± 1e-4) y la coordenada del mapa en cinta es la del mundo salvo un
            // entero (el mapa no tiene costura), en D = 0; 4095,9; 4096; 4096,1; 10 000 y 100 000 m.
            foreach (var id in All)
            {
                var p = WaterProfiles.For(id);
                foreach (var d in new[] { 0.0, 4095.9, 4096, 4096.1, 10000, 100000 })
                {
                    var t = 17.3;
                    var ph = At(d, t, p);
                    for (var relZ = -20.0; relZ <= 300; relZ += 13.7)
                    {
                        foreach (var x in new[] { -9.0, 0.0, 4.5 })
                        {
                            Assert.That(WavePhases.TapeHeight(x, relZ, p.Scale, ph), Is.EqualTo(WavePhases.WorldHeight(x, relZ + d, t, p.Scale)).Within(1e-4),
                                id + " h en D " + d + ", relZ " + relZ);
                            CheckUv(x, relZ, d, t, p.B2.Tile, p.B2.Drift, p.B2.DirectionDeg, 0, ph.B2U, ph.B2V);
                            CheckUv(x, relZ, d, t, p.B3.Tile, p.B3.Drift, p.B3.DirectionDeg, 0, ph.B3U, ph.B3V);
                            CheckUv(x, relZ, d, t, p.B3.Tile * WaterLookArt.B4TileScale, p.B3.Drift, p.B3.DirectionDeg, WaterLookArt.B4RotationDeg, ph.B4U, ph.B4V);
                        }
                    }

                    Assert.That(ph.Phase1, Is.InRange(0.0, WavePhases.TwoPi));
                    Assert.That(ph.NoiseV, Is.InRange(0.0, 1.0));
                }
            }
        }

        private static void CheckUv(double x, double relZ, double d, double t, double tile, double drift, double deg, double rot, double offU, double offV)
        {
            WavePhases.WorldUv(x, relZ + d, t, tile, drift, deg, rot, out var wu, out var wv);
            WavePhases.TapeUv(x, relZ, tile, rot, offU, offV, out var tu, out var tv);
            Assert.That(Turns(wu, tu), Is.EqualTo(0).Within(1e-6), "u en D " + d);
            Assert.That(Turns(wv, tv), Is.EqualTo(0).Within(1e-6), "v en D " + d);
        }

        [Test, Category("AC-6B-20")]
        public void DriftMovesLessThanATileHundredthPerFrame()
        {
            // Con el bote quieto (D fija; en pausa el tiempo de escena corre), la deriva de B2 y B3 cambia menos de 0,01 tile por cuadro y la de
            // B4 (solo Alto, el tile más chico) menos de 0,02: el agua sigue ondulando sin saltos. Con el bote en marcha el corrimiento sigue a
            // D exactamente (NoJumpAt4096).
            foreach (var id in All)
            {
                var p = WaterProfiles.For(id);
                var t = 100.0;
                var prev = At(4096, t, p);
                for (var i = 0; i < 600; i++)
                {
                    t += 1.0 / 60;
                    var next = At(4096, t, p);
                    var delta = Advance(prev, next);
                    for (var k = 2; k <= 5; k++)
                    {
                        Assert.That(Math.Abs(delta[k]), Is.LessThan(0.01), id + " corrimiento " + k);
                    }

                    Assert.That(Math.Abs(delta[6]), Is.LessThan(0.02), id + " B4 u");
                    Assert.That(Math.Abs(delta[7]), Is.LessThan(0.02), id + " B4 v");
                    prev = next;
                }
            }
        }

        [Test, Category("AC-6B-20")]
        public void TheRiverCurrentShiftsTheNoise()
        {
            var river = WaterProfiles.For(EnvironmentId.River);
            var lake = WaterProfiles.For(EnvironmentId.Lake);
            Assert.That(At(0, 256, river).NoiseV, Is.EqualTo(0.25).Within(1e-9), "0,25 m/s × 256 s = un cuarto de período");
            Assert.That(At(0, 256, lake).NoiseV, Is.EqualTo(0).Within(1e-12), "el lago no tiene corriente");
        }
    }
}
