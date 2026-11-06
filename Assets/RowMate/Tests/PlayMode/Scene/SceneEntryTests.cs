using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // SET · La entrada con la escena, como en el player (06a-escena-lago/02-tech-design §3.1, §3.4, §3.9; "Cambios por verificación (ronda
    // 1)"; AC-WKT-13-15, AC-SCN-01-03, AC-SCN-01-06, AC-SCN-01-07): con la orilla por tramos y el armado con el tramo del player,
    // - QA-V6a-REN-04: volver a entrar enseguida después de una sesión larga (el rebobinado a 0 m sin terminar) o la escena demorada que
    //   queda lista con el bote lejos de 0 m nunca arman la orilla entera en un cuadro con la escena a la vista;
    // - QA-V6a-REN-05: en Ready la RT de la sesión ya existe (con el tamaño de la ventana y el formato del nivel) y la entrada no la rehace;
    // - QA-V6a-REN-06: el costo de cada unidad del armado, medido (el peor, con su fase, en el registro).
    [Category("GPU")]
    public sealed class SceneEntryTests : SceneTestBase
    {
        private UiTestHost CreatePlayerLike(DeviceProfile profile)
        {
            return CreateScene(profile, o =>
            {
                o.ScenePlayerWarmup = true;
                o.ScenePlayerStreaming = true;
            });
        }

        private IEnumerator UntilReady(int frames = 60 * 30)
        {
            for (var i = 0; i < frames && SceneHost.Readiness.Value != SceneReadiness.Ready; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(SceneReadiness.Ready), "la precarga terminó");
        }

        // A: una sesión a 4 km, Terminar → Terminar y otra entrada apenas vuelve Workout (antes de que el rebobinado en reposo termine).
        [UnityTest, Category("AC-WKT-13-15"), Category("AC-SCN-01-03")]
        public IEnumerator ReenteringRightAfterALongSessionNeverRebuildsTheShoreInView()
        {
            AssumeGpu();
            CreatePlayerLike(DeviceProfile.TV);
            yield return GoToWorkout();
            yield return UntilReady();
            yield return StartFree();
            yield return AdvanceTo(3);
            SceneHost.ForceDistanceForTest(4000);
            for (var i = 0; i < 600 && !(SceneHost.IsStreamingIdle && SceneHost.Frame.Distance > 3990); i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.Frame.Distance, Is.GreaterThan(3990), "la sesión a 4 km");
            Assert.That(SceneHost.IsStreamingIdle, Is.True, "la orilla de los 4 km armada");
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            for (var i = 0; i < 120 && H.Immersive != null; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(H.Immersive, Is.Null, "Workout de vuelta");
            // El diálogo de terminar termina de irse (su salida es una transición: el tramo de reposo es 0 y el rebobinado no avanza).
            for (var i = 0; i < 40 && !Responds("workout-free"); i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(Responds("workout-free"), Is.True, "Entrenamiento libre responde");

            Assert.That(SceneHost.StreamDistanceForTest, Is.GreaterThan(1000), "el rebobinado a 0 m todavía no terminó (la orilla es la de los 4 km)");
            var rebuilds = SceneHost.InViewShoreRebuilds;
            yield return Frames();
            H.Tap("workout-free");
            var shownAt = -1;
            for (var frame = 0; frame < 150; frame++)
            {
                H.Step();
                yield return null;
                if (shownAt < 0 && SceneHost.IsShowing)
                {
                    shownAt = frame;
                }
            }

            Assert.That(shownAt, Is.GreaterThan(0), "la escena entró con el vuelo");
            Assert.That(SceneHost.Frame.Distance, Is.LessThan(30), "la sesión nueva, desde 0 m");
            Assert.That(SceneHost.InViewShoreRebuilds, Is.EqualTo(rebuilds), "ningún armado entero de la orilla con la escena a la vista");
            AssertClean(H);
        }

        // B: la escena demorada que queda lista con el bote a 2 km arma la orilla de la sesión antes de Ready, no la de 0 m.
        [UnityTest, Category("AC-SCN-01-07"), Category("AC-WKT-13-15")]
        public IEnumerator TheDelayedSceneArrivesWithTheShoreOfTheSession()
        {
            AssumeGpu();
            CreatePlayerLike(DeviceProfile.TV);
            yield return GoToWorkout();
            yield return UntilReady();
            SceneHost.DelayReadinessForTest(1.5);
            yield return StartFree();
            Assert.That(SceneHost.Readiness.Value, Is.Not.EqualTo(SceneReadiness.Ready), "demorada");
            SceneHost.ForceDistanceForTest(2000);
            var rebuilds = SceneHost.InViewShoreRebuilds;
            for (var i = 0; i < 60 * 8 && !(Hud.SceneShown && SceneHost.Draws > 30); i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(Hud.SceneShown, Is.True, "la escena demorada llegó");
            Assert.That(SceneHost.Frame.Distance, Is.GreaterThan(1990));
            Assert.That(SceneHost.InViewShoreRebuilds, Is.EqualTo(rebuilds), "la orilla de los 2 km se armó antes de Ready, no a la vista");
            AssertClean(H);
        }

        // QA-V6a-REN-05 (§3.1: "Ready: … la RT existe"): la RT de la sesión nace en la precarga, con la ventana y el formato del nivel, y el
        // dibujo de calentamiento va a ella (fuera de pantalla); la entrada no la rehace (los intermedios de URP ya tienen ese tamaño).
        [UnityTest, Category("AC-SCN-01-06"), Category("AC-WKT-13-15")]
        public IEnumerator ReadyAlreadyHasTheSessionTarget()
        {
            AssumeGpu();
            CreatePlayerLike(DeviceProfile.TV);
            yield return GoToWorkout();
            yield return UntilReady();
            var rt = SceneHost.Output;
            Assert.That(rt, Is.Not.Null, "en Ready la RT de la sesión existe");
            SceneRenderTarget.SizeFor(DeviceProfile.TV.WidthPx, DeviceProfile.TV.HeightPx, SceneHost.Resolution, out var width, out var height);
            Assert.That(rt!.width, Is.EqualTo(width));
            Assert.That(rt.height, Is.EqualTo(height));
            Assert.That(rt.graphicsFormat, Is.EqualTo(SceneRenderTarget.FormatFor(SceneHost.Profile.Hdr, SceneHost.Tier)), "el formato del nivel");
            Assert.That(SceneHost.WarmupDraws, Is.EqualTo(1));
            Assert.That(SceneHost.Draws, Is.EqualTo(0), "0 dibujos después de Ready fuera del inmersivo");
            var generation = SceneHost.Target.Generation;
            yield return EnterFree();
            Assert.That(SceneHost.Draws, Is.GreaterThan(0));
            Assert.That(SceneHost.Output, Is.SameAs(rt), "la entrada no rehace la RT");
            Assert.That(SceneHost.Target.Generation, Is.EqualTo(generation));
            AssertClean(H);
        }

        // QA-V6a-REN-06: con el tramo del player (3 ms en Workout en reposo) cada unidad del armado se mide; la peor queda en el registro con
        // su fase. La segunda precarga (después de Application.lowMemory) es la que se compara con el player: los shaders ya están
        // compilados en el Editor (la primera paga la compilación del Editor, que en el player hace el precalentado de variantes).
        [UnityTest, Category("AC-WKT-13-15"), Category("Perf")]
        public IEnumerator EachWarmupUnitIsMeasured()
        {
            AssumeGpu();
            CreatePlayerLike(DeviceProfile.TV);
            yield return GoToWorkout();
            yield return UntilReady();
            Debug.Log("[SET] primera precarga: peor unidad " + SceneHost.WorstUnitMs.ToString("0.00") + " ms (" + SceneHost.WorstUnitPhase + (SceneHost.WorstUnitStep.Length > 0 ? ": " + SceneHost.WorstUnitStep : "") + "), peor cuadro "
                      + SceneHost.WorstWarmupMs.ToString("0.00") + " ms");
            SceneHost.SimulateLowMemoryForTest();
            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(SceneReadiness.NotLoaded));
            // Lo que haría Workout al volver a quedar en reposo a la vista (Preload es idempotente).
            SceneHost.Preload();
            yield return UntilReady();
            var worst = SceneHost.WorstUnitMs;
            Debug.Log("[SET] segunda precarga: peor unidad " + worst.ToString("0.00") + " ms (" + SceneHost.WorstUnitPhase + (SceneHost.WorstUnitStep.Length > 0 ? ": " + SceneHost.WorstUnitStep : "") + "), peor cuadro "
                      + SceneHost.WorstWarmupMs.ToString("0.00") + " ms");
            Assert.That(worst, Is.GreaterThan(0), "las unidades se midieron");
            Assert.That(SceneHost.WorstUnitPhase, Is.Not.Empty);
            AssertClean(H);
        }
    }
}
