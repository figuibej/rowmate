using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Scene;
using RowMate.Core.Workout;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // SIM · La escena en el inmersivo (06a-escena-lago/02-tech-design §3.9; AC-WKT-13-02, -06, -08, -09, -12, -13, -14; AC-SCN-02-05 a -07;
    // AC-APP-05-08; AC-WKT-17-07, -09; AC-SCN-01-08; AC-SCN-13-03): la escena se abre en el toque y corre aunque no se dibuje; el HUD se
    // arma en el primer cuadro ≥ 220 y la escena NO se dibuja ese cuadro; en el siguiente entra con el vuelo (a los 1 820 en la pose del
    // contrato); con reducir movimiento, en su lugar desde el cuadro 0; la pausa pasa el velo a la escena; la salida la sube y retrocede
    // y a los 400 se cierra; el diálogo de completado la lleva a la toma aérea; el casco en la zona del bote; la sesión del motor es la
    // misma con escena y sin ella.
    [Category("GPU")]
    public sealed class SceneImmersiveTests : SceneTestBase
    {
        [UnityTest, Category("AC-SCN-02-05"), Category("AC-WKT-13-06"), Category("AC-WKT-13-02"), Category("AC-SCN-01-02"), Category("AC-WKT-13-13")]
        public IEnumerator TheEntryFliesFromTheTap()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return GoToWorkout();
            yield return Frames();
            H.SceneTick();
            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(RowMate.Scene.SceneReadiness.Ready), "la precarga en reposo");
            H.Tap("workout-free");
            var tap = H.Scheduler.MonotonicSeconds;
            Assert.That(SceneHost.IsOpen, Is.True, "abierta en el toque");
            Assert.That(SceneHost.IsShowing, Is.False, "no se dibuja todavía");
            var draws = SceneHost.Draws;
            var builtFrame = -1;
            var flightFrame = -1;
            for (var frame = 0; frame < 140; frame++)
            {
                H.Step();
                yield return null;
                if (builtFrame < 0 && H.Immersive!.HasView)
                {
                    builtFrame = frame;
                    Assert.That(H.Scheduler.MonotonicSeconds - tap, Is.GreaterThanOrEqualTo(0.22 - 1e-6), "el HUD a los ≥ 220 ms");
                    Assert.That(SceneHost.Draws, Is.EqualTo(draws), "la escena no se dibuja en el cuadro del armado del HUD");
                }

                if (flightFrame < 0 && SceneHost.Frame.CameraMode == CameraMode.Flight)
                {
                    flightFrame = frame;
                }
            }

            Assert.That(flightFrame, Is.EqualTo(builtFrame + 1), "el vuelo entra en el cuadro siguiente");
            Assert.That(SceneHost.Frame.CameraMode, Is.EqualTo(CameraMode.Contract), "a los 1 820, la pose del contrato");
            Assert.That(SceneHost.Frame.SceneTime, Is.GreaterThan(2.0), "corrió desde el toque");
            Assert.That(Hud.SceneShown, Is.True);
            // AC-SCN-01-08: el punto del remero (z 9,5) cae en la zona del bote.
            var cam = SceneHost.Frame.Window;
            Assert.That(cam.TryProject(0, 0, SceneCamera.BoatZ, out _, out var rowerY), Is.True);
            Assert.That(rowerY, Is.EqualTo(cam.RowerY).Within(2.5));
            AssertClean(H);
        }

        [UnityTest, Category("AC-APP-05-08"), Category("AC-WKT-13-06")]
        public IEnumerator ReduceMotionShowsTheSceneInPlace()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV, o => o.ReduceMotion = true);
            yield return GoToWorkout();
            H.SceneTick();
            H.Tap("workout-free");
            H.Step();
            yield return null;
            Assert.That(SceneHost.Frame.CameraMode, Is.EqualTo(CameraMode.Contract), "en su lugar desde el cuadro 0");
            Assert.That(SceneHost.Frame.BreathingMultiplier, Is.EqualTo(0), "sin respiración");
        }

        [UnityTest, Category("AC-WKT-13-12"), Category("AC-SCN-03-04")]
        public IEnumerator ThePauseVeilReachesTheScene()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            yield return Tap("hud-pause");
            yield return AdvanceTo(4);
            Assert.That(SceneHost.Frame.Post.VeilWeight, Is.EqualTo(1).Within(1e-6), "el velo entero en la escena");
            Assert.That(SceneHost.Frame.Post.Saturation, Is.LessThan(SceneHost.Frame.Look.Saturation), "el velo desatura");
            yield return Tap("hud-resume");
            yield return AdvanceTo(5);
            Assert.That(SceneHost.Frame.Post.VeilWeight, Is.EqualTo(0).Within(1e-6));
        }

        [UnityTest, Category("AC-SCN-02-06"), Category("AC-WKT-13-08"), Category("AC-WKT-13-09"), Category("AC-WKT-13-14")]
        public IEnumerator TheExitRisesAndClosesBehindTheBlack()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            yield return AdvanceTo(20);
            var distance = SceneHost.Frame.Distance;
            Assert.That(distance, Is.GreaterThan(0));
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            H.Step();
            yield return null;
            Assert.That(SceneHost.Frame.CameraMode, Is.EqualTo(CameraMode.Exit), "la salida desde el cuadro 0");
            for (var i = 0; i < 30; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.IsOpen, Is.False, "cerrada a los 400 (detrás del negro)");
            Assert.That(H.App.Workout.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
        }

        [UnityTest, Category("AC-SCN-02-07"), Category("AC-WKT-21-01")]
        public IEnumerator TheCompletionDialogTakesTheAerialShot()
        {
            AssumeGpu();
            CreateWorkout(RoutineSeeds.F4A, o =>
            {
                o.Scene = SceneMode.Real;
                o.Profile = DeviceProfile.TV;
                o.IsDesktop = false;
            });
            yield return StartRoutine("Corta");
            yield return AdvanceTo(31);
            yield return Settle();
            Assert.That(H.Immersive!.Completion, Is.Not.Null, "el diálogo de completado");
            Assert.That(SceneHost.Frame.CameraMode, Is.EqualTo(CameraMode.Aerial));
            yield return AdvanceTo(38);
            Assert.That(SceneHost.Frame.Pose.PitchDeg, Is.EqualTo(MotionSpecs.AerialPitchDeg).Within(1e-6), "quieta con el cabeceo de −4°");
            Assert.That(SceneHost.Frame.BreathingMultiplier, Is.EqualTo(0).Within(1e-6));
        }

        // QA-V6a-02 (AC-WKT-17-07, DT6a-06; VID-09): al cambiar de clase de ventana (vertical ↔ horizontal) las piezas del HUD entran en
        // 200 ms en el layout nuevo, también en la PRIMERA rotación del inmersivo: en el cambio y a los 50 ms, < 1; a los 200 ms, 1. Con
        // reducir movimiento, 1 en el acto. La escena sigue (la misma sesión) y se adapta en el acto.
        [UnityTest, Category("AC-WKT-17-07")]
        public IEnumerator TheHudFadesInWhenTheWindowChangesClass()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            yield return AdvanceTo(4);
            var session = SceneHost.Session;
            Assert.That(MinPieceOpacity(), Is.EqualTo(1).Within(1e-4), "antes de rotar, el HUD a pleno");

            H.Resize(DeviceProfile.TH);
            // El layout de la ventana nueva (el GeometryChangedEvent de la ventana avisa a ShellLayout) sin mover el reloj.
            yield return Frames(3);
            Assert.That(MaxPieceOpacity(), Is.LessThan(0.05), "en el cuadro del cambio de clase, las piezas en 0 en el layout nuevo");
            for (var i = 0; i < 3; i++)
            {
                H.Step();
                yield return null;
            }

            var at50 = MaxPieceOpacity();
            Assert.That(at50, Is.GreaterThan(0).And.LessThan(1), "a los 50 ms, entrando");
            for (var i = 0; i < 10; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(MinPieceOpacity(), Is.EqualTo(1).Within(1e-4), "a los 216 ms, a pleno");
            Assert.That(SceneHost.Session, Is.SameAs(session), "la misma sesión de la escena");

            // Con reducir movimiento, en el acto.
            H.SetAccessibility(1, true);
            H.Resize(DeviceProfile.TV);
            yield return Frames(3);
            Assert.That(MinPieceOpacity(), Is.EqualTo(1).Within(1e-4), "con reducir movimiento, 1 en el acto");
            AssertClean(H);
        }

        private static readonly RowMate.Core.Design.HudPiece[] Pieces =
        {
            RowMate.Core.Design.HudPiece.TopBar, RowMate.Core.Design.HudPiece.Spm,
            RowMate.Core.Design.HudPiece.SplitWatts, RowMate.Core.Design.HudPiece.Band,
            RowMate.Core.Design.HudPiece.Vitals, RowMate.Core.Design.HudPiece.Controls,
        };

        // La opacidad que el código le puso a la pieza (inline; sin valor = 1).
        private static double OpacityOf(UnityEngine.UIElements.VisualElement element)
        {
            var style = element.style.opacity;
            return style.keyword == UnityEngine.UIElements.StyleKeyword.Undefined ? style.value : 1;
        }

        private double MinPieceOpacity()
        {
            var min = 1.0;
            foreach (var piece in Pieces)
            {
                var element = Hud.PieceOf(piece);
                if (element != null)
                {
                    min = System.Math.Min(min, OpacityOf(element));
                }
            }

            return min;
        }

        private double MaxPieceOpacity()
        {
            var max = 0.0;
            foreach (var piece in Pieces)
            {
                var element = Hud.PieceOf(piece);
                if (element != null)
                {
                    max = System.Math.Max(max, OpacityOf(element));
                }
            }

            return max;
        }

        // AC-SCN-13-03 en pantalla: en el cambio de astro (19:34) las líneas del agua valen 0 (la visibilidad del astro).
        [UnityTest, Category("AC-SCN-13-03")]
        public IEnumerator NoShimmerAtTheChangeOfStar()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            SceneHost.ForceHourForTest(SceneTimeOfDay.NightStartHour);
            H.Step();
            Assert.That(SceneHost.Frame.ShimmerVisibility, Is.EqualTo(0).Within(1e-9));
        }
    }
}
