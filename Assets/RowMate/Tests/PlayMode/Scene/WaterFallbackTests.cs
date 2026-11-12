using System.Collections;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // WFT · La disponibilidad del agua avanzada y la caída a la degradada (F-SCN-13; 06b-agua-avanzada/02-tech-design §5; DT6b-01, -12, -13,
    // -22; AC-6B-06 a -08): sin soporte (un shader sin el tag RmWater o con un SubShader que no corre en URP, G6b-01) cae a la degradada con
    // UNA línea; una falla en medio de la sesión funde a la degradada en 0,6 s sin tocar la sesión; una excepción del dibujo corta en el acto;
    // queda pegada (sin reintento) y no hay ningún error visible; la avanzada que llega tarde aparece con su fundido sin cortar la escena.
    [Category("GPU")]
    public sealed class WaterFallbackTests : SceneTestBase
    {
        private const string NoTagShader = "Assets/RowMate/Tests/PlayMode/Scene/Shaders/WaterNoTagTest.shader";
        private const string UnsupportedShader = "Assets/RowMate/Tests/PlayMode/Scene/Shaders/WaterUnsupportedTest.shader";

        private int WaterLines()
        {
            return H.Logger.Lines.Count(l => l.Contains(WaterFallbackLog.Prefix));
        }

        private IEnumerator Enter()
        {
            CreateScene(DeviceProfile.TV, o =>
            {
                o.SceneTier = QualityTier.Medium;
                o.SceneWater = WaterForce.Auto;
            });
            yield return EnterFree();
        }

        [UnityTest, Category("AC-6B-06"), Category("AC-6B-08")]
        public IEnumerator UnsupportedSubShaderIsDetected([Values(NoTagShader, UnsupportedShader)] string path)
        {
            AssumeGpu();
            yield return Enter();
#if UNITY_EDITOR
            var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(path);
#else
            Shader? shader = null;
            Assert.Ignore("solo en el Editor");
#endif
            Assert.That(shader, Is.Not.Null, path);
            var material = new Material(shader);
            try
            {
                SceneHost.ForceWaterMaterialForTest(material);
                SceneHost.CompleteWaterForTest();
                yield return AdvanceTo(6);
                Assert.That(SceneHost.AdvancedWaterReady, Is.False, "no hay soporte");
                Assert.That(SceneHost.WaterAvailability.IsDegradedForApp, Is.True);
                Assert.That(SceneHost.WaterChoice.Showing, Is.EqualTo(WaterKind.Degraded));
                Assert.That(SceneHost.ShowingAdvancedWater, Is.False);
                Assert.That(WaterLines(), Is.EqualTo(1), "una línea: " + WaterFallbackLog.Line(WaterModeSelector.UnsupportedReason));
                Assert.That(H.Logger.Contains(WaterFallbackLog.Line(WaterModeSelector.UnsupportedReason)), Is.True);
            }
            finally
            {
                Object.Destroy(material);
            }

            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-08")]
        public IEnumerator FailureFadesToDegraded()
        {
            AssumeGpu();
            yield return Enter();
            yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
            Assert.That(SceneHost.ShowingAdvancedWater, Is.True);
            var distance = SceneHost.Frame.Distance;
            SceneHost.ReportWaterFailure("prueba");
            for (var i = 0; i < 6; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.ShowingAdvancedWater, Is.True, "la avanzada sigue mientras funde");
            Assert.That(SceneHost.DegradedBlend, Is.GreaterThan(0).And.LessThan(1), "el fundido de 0,6 s");
            yield return Advance(0.7);
            Assert.That(SceneHost.ShowingAdvancedWater, Is.False, "al terminar, la grilla");
            Assert.That(SceneHost.DegradedBlend, Is.EqualTo(1).Within(1e-6));
            Assert.That(SceneHost.Frame.Distance, Is.GreaterThan(distance), "la sesión siguió");
            Assert.That(WaterLines(), Is.EqualTo(1));
            // Pegada por la app, sin reintento y sin otra línea.
            SceneHost.ReportWaterFailure("otra");
            SceneHost.DegradeWater();
            yield return Advance(1);
            Assert.That(WaterLines(), Is.EqualTo(1), "una sola línea por sesión de la app");
            Assert.That(SceneHost.WaterChoice.Kind, Is.EqualTo(WaterKind.Degraded));
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-08")]
        public IEnumerator DrawExceptionCutsInstantly()
        {
            AssumeGpu();
            yield return Enter();
            yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
            SceneHost.FailNextWaterDrawForTest();
            H.Step();
            yield return null;
            Assert.That(SceneHost.ShowingAdvancedWater, Is.False, "la grilla en el mismo cuadro");
            H.Step();
            yield return null;
            Assert.That(SceneHost.DegradedBlend, Is.EqualTo(1).Within(1e-6), "corte en el acto (DT6b-12)");
            Assert.That(WaterLines(), Is.EqualTo(1));
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-07")]
        public IEnumerator AdvancedArrivesWithFadeKeepingTheScene()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV, o =>
            {
                o.SceneTier = QualityTier.Medium;
                o.SceneWater = WaterForce.Auto;
            });
            SceneHost.HoldAdvancedWaterForTest(true);
            yield return StartFree();
            yield return AdvanceTo(2.5);
            Assert.That(SceneHost.ShowingAdvancedWater, Is.False, "todavía la degradada");
            Assert.That(SceneHost.IsShowing, Is.True, "la escena a la vista con la degradada");
            var draws = SceneHost.Draws;
            SceneHost.HoldAdvancedWaterForTest(false);
            var sawFade = false;
            for (var i = 0; i < 180; i++)
            {
                H.Step();
                yield return null;
                var blend = SceneHost.DegradedBlend;
                sawFade |= blend > 0.05 && blend < 0.95;
            }

            Assert.That(sawFade, Is.True, "llegó con el fundido");
            Assert.That(SceneHost.ShowingAdvancedWater, Is.True);
            Assert.That(SceneHost.Draws, Is.GreaterThan(draws + 150), "la escena nunca dejó de dibujar");
            Assert.That(WaterLines(), Is.EqualTo(0));
            AssertClean(H);
        }

        // QA-V6b-11 (AC-6B-08: "ningún cuadro del cambio pasa de 33 ms"): con el reloj de prueba, el trabajo de la app en cada cuadro del
        // cambio (el Tick del shell y el de la escena, que dibuja la escena entera: la avanzada mientras funde y la degradada) cabe en 33 ms,
        // en el fundido de 0,6 s (una falla) y en el corte en el acto (una excepción del dibujo). La medida real es la del build
        // (-rowmate-water-fail: máx 6,8 ms de CPU en la ventana del cambio).
        [UnityTest, Category("AC-6B-08"), Category("Perf")]
        public IEnumerator NoFrameOver33([Values("fundido", "corte")] string how)
        {
            AssumeGpu();
            yield return Enter();
            yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
            Assert.That(SceneHost.ShowingAdvancedWater, Is.True);
            var watch = new System.Diagnostics.Stopwatch();
            var steady = 0.0;
            for (var i = 0; i < 30; i++)
            {
                watch.Restart();
                H.Step();
                steady = System.Math.Max(steady, watch.Elapsed.TotalMilliseconds);
                yield return null;
            }

            if (how == "fundido")
            {
                SceneHost.ReportWaterFailure("prueba");
            }
            else
            {
                SceneHost.FailNextWaterDrawForTest();
            }

            var worst = 0.0;
            var sawBlend = false;
            for (var i = 0; i < 48; i++)
            {
                watch.Restart();
                H.Step();
                worst = System.Math.Max(worst, watch.Elapsed.TotalMilliseconds);
                sawBlend |= SceneHost.DegradedBlend > 0 && SceneHost.DegradedBlend < 1;
                yield return null;
            }

            Debug.Log("[WFT] " + how + ": peor cuadro del cambio " + worst.ToString("0.00") + " ms (antes, con la avanzada: " + steady.ToString("0.00") + " ms)");
            Assert.That(sawBlend, Is.EqualTo(how == "fundido"), "fundido de 0,6 s solo con la falla; la excepción corta en el acto");
            Assert.That(SceneHost.ShowingAdvancedWater, Is.False, "terminó en la degradada");
            Assert.That(SceneHost.DegradedBlend, Is.EqualTo(1).Within(1e-6));
            Assert.That(worst, Is.LessThanOrEqualTo(33.0), "ningún cuadro del cambio pasa de 33 ms");
            Assert.That(WaterLines(), Is.EqualTo(1));
            AssertClean(H);
        }

        // QA-V6b-11, G6b-17 (AC-6B-08: "la sesión guardada es idéntica a la de una corrida sin falla"): con el reloj de prueba y el remo de
        // prueba deterministas, dos entrenamientos libres de 20 s con los mismos pasos, uno con la falla del agua a los 10 s, se guardan con
        // la misma fila (sin el id) y los mismos puntos (sin sus ids).
        [UnityTest, Category("AC-6B-08")]
        public IEnumerator SavedSessionIdentical()
        {
            AssumeGpu();
            RowMate.Core.Models.WorkoutSession? clean = null;
            RowMate.Core.Models.WorkoutSession? failed = null;
            System.Collections.Generic.IReadOnlyList<RowMate.Core.Models.DataPoint>? cleanPoints = null;
            System.Collections.Generic.IReadOnlyList<RowMate.Core.Models.DataPoint>? failedPoints = null;
            yield return RowAndSave(false, (s, p) => { clean = s; cleanPoints = p; });
            yield return RowAndSave(true, (s, p) => { failed = s; failedPoints = p; });
            Assert.That(clean, Is.Not.Null);
            Assert.That(failed, Is.Not.Null);
            Assert.That(cleanPoints!.Count, Is.GreaterThanOrEqualTo(4), "la sesión tiene puntos (uno cada 5 s)");
            Assert.That(clean!.FinishedAt, Is.Not.Null, "terminada");
            Assert.That(failed! with { Id = null }, Is.EqualTo(clean with { Id = null }), "la misma fila");
            Assert.That(failedPoints!.Count, Is.EqualTo(cleanPoints.Count), "la misma cantidad de puntos");
            for (var i = 0; i < cleanPoints.Count; i++)
            {
                Assert.That(failedPoints[i] with { Id = null, SessionId = 0 }, Is.EqualTo(cleanPoints[i] with { Id = null, SessionId = 0 }), "punto " + i);
            }
        }

        private IEnumerator RowAndSave(bool fail, System.Action<RowMate.Core.Models.WorkoutSession?, System.Collections.Generic.IReadOnlyList<RowMate.Core.Models.DataPoint>> done)
        {
            yield return Enter();
            yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
            yield return AdvanceTo(10);
            if (fail)
            {
                SceneHost.ReportWaterFailure("prueba");
            }

            for (var i = 0; i < 60; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.ShowingAdvancedWater, Is.EqualTo(!fail), fail ? "con la falla, la degradada" : "sin falla, la avanzada");
            Assert.That(WaterLines(), Is.EqualTo(fail ? 1 : 0));
            yield return AdvanceTo(20);
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            for (var i = 0; i < 60; i++)
            {
                H.Step();
                yield return null;
            }

            var listing = LastSession();
            Assert.That(listing, Is.Not.Null, "se guardó");
            var session = H.Sessions.GetByIdAsync(listing!.Id);
            var points = H.Sessions.GetPointsAsync(listing.Id);
            Assert.That(session.IsCompleted && points.IsCompleted, Is.True, "ejecutor en línea");
            AssertClean(H);
            done(session.Result, points.Result);
        }

        [UnityTest, Category("AC-6B-08")]
        public IEnumerator NoUserVisibleError()
        {
            AssumeGpu();
            yield return Enter();
            yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
            SceneHost.ReportWaterFailure("prueba");
            yield return Advance(1);
            Assert.That(H.Logger.Levels.Count(l => l == LogLevel.Error || l == LogLevel.Warning), Is.EqualTo(0), "la caída no es un error");
            Assert.That(H.Immersive, Is.Not.Null, "el inmersivo sigue");
            AssertClean(H);
        }
    }
}
