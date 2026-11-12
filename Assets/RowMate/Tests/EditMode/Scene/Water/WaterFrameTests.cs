using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // WFR · Lo del agua avanzada en el cuadro de la sesión (06b-agua-avanzada/02-tech-design §3.3): el perfil, las fases, la columna del
    // astro con su visibilidad (DV-59), la estela con la velocidad del casco, los anillos, las columnas, la boya de la costa y el casco;
    // sin asignar en el Tick.
    [TestFixture]
    public sealed class WaterFrameTests
    {
        private static readonly DateTimeOffset Local = new DateTimeOffset(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(-3));
        private static readonly SceneInputs Strong = new SceneInputs(125, 24, true);
        private static readonly SceneInputs Paused = new SceneInputs(125, 24, false);

        private static SceneTickInputs Inputs(double now, in SceneInputs w)
        {
            return new SceneTickInputs(now, Local, Local.ToUniversalTime(), w, null, false, 411, 914, 0);
        }

        private static SceneSession Open(EnvironmentId id, double hour)
        {
            var s = new SceneSession(EnvironmentCatalog.Create(id), SceneSessionOptions.Default);
            s.Open(0);
            s.ForceHourForTest(hour);
            return s;
        }

        [Test, Category("AC-6B-14"), Category("AC-6B-13")]
        public void TheSessionFillsTheWaterFrame()
        {
            var frame = SceneFrame.Create();
            var s = Open(EnvironmentId.Lake, 12);
            s.Advance(Inputs(0, Strong), 15, ref frame);
            var water = frame.Water;
            Assert.That(water.Profile.Environment, Is.EqualTo(EnvironmentId.Lake));
            Assert.That(water.Phases.Phase1, Is.InRange(0.0, WavePhases.TwoPi));
            Assert.That(water.WakeIntensity, Is.GreaterThan(0.8), "con Fuerte (4 m/s) la estela a pleno, con el surge");
            Assert.That(water.WakeCount, Is.GreaterThan(40), "15 s remando: la historia casi llena");
            // El más nuevo nació en la popa que se dibuja hace menos de 0,25 s: a 4 m/s, a lo sumo ≈ 1 m más atrás (más el surge).
            Assert.That(water.WakePacked[0], Is.InRange((float)WaterModel.SternRelZ - 1.2f, (float)WaterModel.SternRelZ + 0.2f));
            Assert.That(water.WakePacked[2], Is.LessThan(WakeTrail.Interval + 1e-6f), "su edad");
            Assert.That(water.HullStartRelZ, Is.EqualTo(5.3).Within(0.2));
            Assert.That(water.HullEndRelZ - water.HullStartRelZ, Is.EqualTo(8.2).Within(1e-9));
            Assert.That(water.HullHalfWidth, Is.EqualTo(0.33 / 2 + 0.06).Within(1e-9));
            Assert.That(water.Rings, Is.Not.Null);
            Assert.That(water.Columns, Is.Not.Null);
            Assert.That(water.Columns.Count, Is.EqualTo(0), "de día, sin columnas");
            Assert.That(water.ColumnStrength, Is.GreaterThan(0.2), "el sol de las 12 h");
            Assert.That(water.ColumnCenterX, Is.EqualTo(frame.ShimmerCenterX), "la columna bajo el astro que se ve (las mismas x que las líneas)");
            Assert.That(water.LeftBand.G, Is.GreaterThan(0));
            Assert.That(water.BuoyHeave, Is.EqualTo(0), "el lago no tiene mar de fondo");

            // En pausa la estela se apaga con la inercia, pero el agua sigue (el tiempo de escena corre).
            var phase = water.Phases.Phase1;
            s.Advance(Inputs(15, Paused), 14, ref frame);
            Assert.That(frame.Water.WakeIntensity, Is.LessThan(0.05));
            Assert.That(frame.Water.Phases.Phase1, Is.Not.EqualTo(phase), "el agua sigue ondulando");
        }

        [Test, Category("AC-6B-13")]
        public void TheColumnFadesAtTheAstroSwapAndIsDimmerAtNight()
        {
            var frame = SceneFrame.Create();
            var noon = Open(EnvironmentId.Lake, 12);
            noon.Advance(Inputs(0, Strong), 1, ref frame);
            var day = frame.Water.ColumnStrength;
            var swap = Open(EnvironmentId.Lake, 19 + 34.0 / 60 + 19.0 / 3600);
            swap.Advance(Inputs(0, Strong), 1, ref frame);
            Assert.That(frame.Water.ColumnStrength, Is.LessThan(0.01), "DV-59: se apaga en el cambio de astro, sin saltar");
            var night = Open(EnvironmentId.Lake, 22);
            night.Advance(Inputs(0, Strong), 1, ref frame);
            Assert.That(frame.Water.ColumnStrength, Is.GreaterThan(0));
            Assert.That(frame.Water.ColumnStrength, Is.LessThan(day), "la luna, más tenue que el sol de las 12 h");
        }

        [Test, Category("AC-6B-19")]
        public void TheCoastBuoyRidesTheSwell()
        {
            var frame = SceneFrame.Create();
            var s = Open(EnvironmentId.Coast, 12);
            var seen = 0.0;
            for (var k = 0; k < 40; k++)
            {
                s.Advance(Inputs(k, Strong), 1, ref frame);
                Assert.That(Math.Abs(frame.Water.BuoyHeave), Is.LessThanOrEqualTo(0.15 + 1e-9));
                seen = Math.Max(seen, Math.Abs(frame.Water.BuoyHeave));
            }

            Assert.That(frame.Water.Profile.Swell, Is.True);
            Assert.That(seen, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(0.15));
        }

        [Test, Category("AC-6B-28")]
        public void TickDoesNotAllocateWithTheWater()
        {
#if NET5_0_OR_GREATER
            foreach (var id in new[] { EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta })
            {
                var frame = SceneFrame.Create();
                var s = Open(id, 22);
                Run(s, ref frame, 0, 600);
                var best = long.MaxValue;
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    best = Math.Min(best, Run(s, ref frame, 10 + attempt * 10, 600));
                }

                Assert.That(best, Is.EqualTo(0L), id + " de noche, con columnas, anillos y estela");
            }
#else
            Assert.Ignore("solo dotnet: en Mono el contador del hilo da 0 (la medida de Unity es la de cuadros reales)");
#endif
        }

#if NET5_0_OR_GREATER
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        private static long Run(SceneSession s, ref SceneFrame frame, double start, int frames)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < frames; i++)
            {
                s.Tick(Inputs(start + i / 60.0, Strong), ref frame);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
#endif
    }
}
