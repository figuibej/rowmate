using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Workout;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // IHT · El HUD funcional en pantalla (F-WKT-13, -14, -16, -17, F-WKT-10; AC-SCN-01-01; DV-31, DV-33, DV-38;
    // 04a-motor-hud-funcional/02-tech-design §6.3-§6.7, §6.11; 04-design-notes §3, §5). R-4: 150 W, 24 spm, split 125 s.
    // Prueba 4a (F-4A): calentamiento 30 s; serie ×2 [trabajo 60 s, vatios mín. 160, SPM 22, +2 SPM por repetición;
    // descanso 30 s]; enfriamiento 30 s.
    public sealed class ImmersiveHudTests : WorkoutUiTestBase
    {
        private static Func<int, RowMate.Core.Models.RowingData> WithPulse(int bpm)
        {
            return k => RowerScripts.R4(k).CopyWith(heartRate: bpm);
        }

        [UnityTest, Category("AC-WKT-13-01")]
        public IEnumerator OpensAtOnce()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return GoToWorkout();
            yield return Frames();
            H.Tap("workout-free");
            Assert.That(H.Immersive, Is.Not.Null, "en el acto, sin esperar a la base");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active), "el motor ya corre desde el toque");
            yield return Settle();
            var window = H.Root.worldBound;
            var root = Immersive.Root.worldBound;
            Assert.That(root.width, Is.EqualTo(window.width).Within(0.5f), "a pantalla completa");
            Assert.That(root.height, Is.EqualTo(window.height).Within(0.5f));
        }

        [UnityTest, Category("AC-WKT-13-02"), Category("AC-SCN-01-01")]
        public IEnumerator Layers()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            var hud = Hud;
            Assert.That(hud.IndexOf(hud.Backdrop), Is.EqualTo(0), "el fondo atrás de todo");
            Assert.That(hud.IndexOf(hud.TopBar), Is.LessThan(hud.IndexOf(hud.Spm)), "la barra, después el HUD");
            Assert.That(hud.IndexOf(hud.Controls), Is.EqualTo(hud.childCount - 1), "los controles adelante");
            Assert.That(Bounds("hud-topbar").yMin - H.Root.worldBound.yMin, Is.EqualTo(8 * (float)Immersive.Layout!.Scale).Within(1f),
                "la barra a margen seguro superior + 8");
            Assert.That(Immersive.Layout.Horizon, Is.EqualTo(0.40).Within(0.001), "el cielo arriba del 40 % en vertical");
            Assert.That(hud.Backdrop.childCount, Is.EqualTo(0), "fondo provisorio liso: sin dibujos");
        }

        [UnityTest, Category("AC-WKT-13-05"), Category("AC-WKT-14-01"), Category("AC-WKT-14-02")]
        public IEnumerator StepRowWithoutTheRoutineName()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(6.05);
            Assert.That(H.IsTextVisible("Prueba 4a"), Is.False, "sin el nombre de la rutina (paridad)");
            Assert.That(Hud.Steps, Is.Not.Null, "con rutina, la fila de pasos");
            Assert.That(Hud.Free, Is.Null);
            Assert.That(H.TextOf("hud-step-type"), Is.EqualTo("Calentamiento"));
            Assert.That(SameColor(H.Find("hud-step-type")!.resolvedStyle.color, Tokens.StepWarmupHud), Is.True, "step.warmup.hud");
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo("0:24"));
            Assert.That(H.IsShown("hud-step-target"), Is.False, "sin objetivos, sin pastilla");
            Assert.That(H.Find("hud-topbar")!.pickingMode, Is.EqualTo(PickingMode.Ignore), "no responde al toque");
        }

        [UnityTest, Category("AC-WKT-14-03"), Category("AC-WKT-10-05")]
        public IEnumerator TargetPillAndTrafficLight()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(50.05);
            Assert.That(H.TextOf("hud-step-type"), Is.EqualTo("Trabajo"));
            Assert.That(SameColor(H.Find("hud-step-type")!.resolvedStyle.color, Tokens.StepWorkHud), Is.True);
            var pill = Hud.Steps!.TargetText;
            Assert.That(pill, Does.Contain(">160W · 22 spm · "), "la pastilla de objetivo");
            Assert.That(pill, Does.Contain("SPM +2/rep"));
            Assert.That(SameColor(H.Find("hud-step-target")!.resolvedStyle.color, Tokens.TextPrimary), Is.True, "en blanco (DV-38)");
            Assert.That(Bounds("hud-countdown").xMax, Is.LessThanOrEqualTo(Bounds("hud-topbar").xMax + 0.5f), "nunca empuja afuera la cuenta");

            // Watts fuera: 150 contra 160 → state.offTarget.hud con ▲; SPM cumple: 24 contra 22 → blanco, sin flecha.
            Assert.That(Hud.Watts.HasTarget && Hud.Watts.IsOff, Is.True);
            Assert.That(SameColor(H.Find("hud-watts-value")!.resolvedStyle.color, Tokens.StateOffTargetHud), Is.True, "Watts en #FF625B");
            Assert.That(Hud.Watts.ArrowShown, Is.True, "la flecha (el prefijo del número, M1)");
            Assert.That(Hud.Watts.ArrowGlyph, Is.EqualTo(HudCard.Arrow));
            Assert.That(H.TextOf("hud-target-watts"), Is.EqualTo("obj. 160"));
            Assert.That(Hud.Spm.HasTarget && !Hud.Spm.IsOff, Is.True);
            Assert.That(SameColor(H.Find("hud-spm-value")!.resolvedStyle.color, Tokens.TextHudPrimary), Is.True, "SPM en blanco");
            Assert.That(Hud.Spm.ArrowShown, Is.False, "el hueco vacío");
            Assert.That(H.TextOf("hud-target-spm"), Is.EqualTo("obj. 22"));
            Assert.That(Hud.Split.HasTarget, Is.False, "Split sin objetivo");
            Assert.That(SameColor(H.Find("hud-split-value")!.resolvedStyle.color, Tokens.MetricSplitHud), Is.True, "sin objetivo, en su color .hud");
        }

        [UnityTest, Category("AC-WKT-14-04"), Category("AC-WKT-14-05"), Category("AC-WKT-04-06")]
        public IEnumerator UpNextAndEndingSoonOnScreen()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(74.05);
            Assert.That(H.IsShown("hud-up-next"), Is.False, "a 16 s del final, todavía no");
            yield return AdvanceTo(75.05);
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo("0:15"));
            Assert.That(Hud.Steps!.UpNextShown, Is.True, "Próximo desde 0:15");
            // La entrada de la pastilla (ancho con ω 12 y fundido de 200) termina antes de medirla.
            yield return Settle();
            Assert.That(H.IsShown("hud-up-next"), Is.True);
            Assert.That(H.TextOf("hud-up-next-label"), Is.EqualTo("Próximo:"));
            Assert.That(H.TextOf("hud-up-next-type")!.Trim(), Is.EqualTo("Descanso"), "un espacio y el tipo del paso siguiente");
            Assert.That(SameColor(H.Find("hud-up-next-type")!.resolvedStyle.color, Tokens.TextHudPrimary), Is.True, "el tipo en blanco (DV-38)");
            Assert.That(H.Find("hud-up-next-label")!.resolvedStyle.opacity, Is.EqualTo(1).Within(0.001f), "el texto siempre entero");
            // It. 4b (§2.15, DN4b-16): el aire antes de la cuenta es el del plan del paso (8 en normal, 6 en los compactos), × s.
            var gap = (float)(Hud.Steps!.Plan.CountdownGap * Immersive.Layout!.Scale);
            Assert.That(Bounds("hud-countdown").xMin - Bounds("hud-up-next").xMax, Is.EqualTo(gap).Within(1f), "a 8 o 6 (el plan) de la cuenta");
            var steps = Hud.Steps!;
            var need = steps.UpNextLabel.MeasureTextSize(steps.UpNextLabel.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x
                       + steps.UpNextType.MeasureTextSize(steps.UpNextType.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x
                       + 2 * (steps.Plan.UpNextPadH + 1);
            Assert.That(Bounds("hud-up-next").width / (float)Immersive.Layout!.Scale, Is.GreaterThanOrEqualTo(need - 1), "la pastilla entera: sus dos textos y el relleno");
            var pulse = Hud.Steps!.Pulse;
            Assert.That(pulse.IsPulsing, Is.True);
            var bg = Hud.Steps.UpNextBg;
            var low = 2f;
            var high = -1f;
            for (var i = 0; i < 12; i++)
            {
                yield return Advance(0.1);
                var opacity = bg.resolvedStyle.opacity;
                low = Mathf.Min(low, opacity);
                high = Mathf.Max(high, opacity);
            }

            Assert.That(high - low, Is.GreaterThan(0.2f), "late el fondo (0,6 → 1 en 600 ms y vuelve)");
            Assert.That(low, Is.GreaterThanOrEqualTo(0.59f));
            Assert.That(high, Is.LessThanOrEqualTo(1.001f));

            yield return AdvanceTo(85.05);
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo("0:05"), "por terminar desde 0:05");
            yield return Advance(0.6);
            Assert.That(pulse.Scale, Is.EqualTo(MotionSpecs.EndingSoonScale).Within(0.01), "la cuenta crece a 1,1");
            yield return AdvanceTo(90.05);
            yield return Settle();
            Assert.That(H.TextOf("hud-step-type"), Is.EqualTo("Descanso"), "0:00 nunca se ve: el paso cambió");
        }

        [UnityTest, Category("AC-WKT-14-06")]
        public IEnumerator PausedRowStaysWithItsPulses()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(80.05);
            yield return Tap("hud-pause");
            var countdown = H.TextOf("hud-countdown");
            yield return Advance(5);
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo(countdown), "en pausa la fila queda quieta");
            Assert.That(Hud.Steps!.Pulse.IsPulsing, Is.True, "los pulsos siguen (paridad)");
        }

        [UnityTest, Category("AC-WKT-16-01"), Category("AC-WKT-10-08")]
        public IEnumerator FreeTopBar()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            Assert.That(Hud.Free, Is.Not.Null);
            Assert.That(Hud.Steps, Is.Null);
            Assert.That(Hud.Free!.Icon.Codepoint, Is.EqualTo(Icons.Rowing));
            Assert.That(SameColor(Hud.Free.Icon.resolvedStyle.color, Tokens.AccentPrimary), Is.True, "rowing en #00B4D8");
            Assert.That(H.TextOf("hud-free-title-text"), Is.EqualTo("Entrenamiento Libre"), "con la L mayúscula del legacy");
            Assert.That(Hud.SpmHasTarget || Hud.SplitHasTarget || Hud.WattsHasTarget, Is.False, "en libre, sin semáforo");
        }

        [UnityTest, Category("AC-WKT-17-01"), Category("AC-WKT-17-02"), Category("AC-WKT-17-03"), Category("AC-APP-06-01")]
        public IEnumerator CardsAndColors()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return AdvanceTo(12.05);
            Assert.That(H.TextOf("hud-spm-value"), Is.EqualTo("24.0"));
            Assert.That(H.TextOf("hud-split-value"), Is.EqualTo("02:05"));
            Assert.That(H.TextOf("hud-watts-value"), Is.EqualTo("150"));
            Assert.That(Hud.Watts.UnitText, Is.EqualTo("W"), "la W, el sufijo del número (M2)");
            Assert.That(SameColor(H.Find("hud-spm-value")!.resolvedStyle.color, Tokens.MetricSpmHud), Is.True, "SPM .hud");
            Assert.That(SameColor(H.Find("hud-spm-label")!.resolvedStyle.color, Tokens.MetricSpm), Is.True, "etiqueta en el color base");
            Assert.That(SameColor(H.Find("hud-split-value")!.resolvedStyle.color, Tokens.MetricSplitHud), Is.True);
            Assert.That(SameColor(H.Find("hud-watts-value")!.resolvedStyle.color, Tokens.MetricWattsHud), Is.True);
            var unit = Hud.Watts.Number.SuffixColor;
            Assert.That(SameColor(unit, Tokens.MetricWattsHud), Is.True, "la unidad en metric.watts.hud");
            Assert.That(unit.a, Is.EqualTo(0.8f).Within(0.01f), "al 80 % (DV-31)");
            Assert.That(Hud.Spm.ValueSizeDp, Is.EqualTo(64), "SPM 64");
            Assert.That(Hud.Split.ValueSizeDp, Is.EqualTo(44), "Split y Watts 44");
            Assert.That(Hud.Kcal.ValueSizeDp, Is.EqualTo(32), "kcal y BPM 32");
            var window = H.Root.worldBound;
            var s = (float)Immersive.Layout!.Scale;
            Assert.That(Bounds("hud-spm").xMin - window.xMin, Is.EqualTo(14 * s).Within(1f), "SPM a 14 del borde");
            Assert.That(window.xMax - Bounds("hud-split").xMax, Is.EqualTo(14 * s).Within(1f), "Split y Watts a 14 del borde derecho");
            Assert.That(Bounds("hud-watts").yMin - Bounds("hud-split").yMax, Is.EqualTo(8 * s).Within(1f), "a 8 una de otra");
        }

        [UnityTest, Category("AC-WKT-17-04"), Category("AC-WKT-17-05"), Category("AC-WKT-17-06")]
        public IEnumerator BandVitalsAndLiveValues()
        {
            CreateWorkout(RoutineSeeds.Lista, reading: WithPulse(140));
            yield return StartFree();
            yield return AdvanceTo(12.05);
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:12"), "el reloj activo (AC-WKT-02-03)");
            Assert.That(H.TextOf("hud-distance-value"), Is.EqualTo(H.ScriptedRower!.Last!.DistanceMeters.ToString()), "la distancia del monitor, sin volver a 0");
            Assert.That(H.ScriptedRower.Last.DistanceMeters, Is.GreaterThan(1000), "los totales del monitor (Q-15)");
            Assert.That(Hud.Band.UnitText, Is.EqualTo("m"), "la m, el sufijo de la distancia (M2)");
            Assert.That(SameColor(H.Find("hud-time-value")!.resolvedStyle.color, Tokens.MetricTimeHud), Is.True);
            Assert.That(SameColor(H.Find("hud-distance-value")!.resolvedStyle.color, Tokens.MetricDistanceHud), Is.True);
            Assert.That(H.IsShown("hud-bpm"), Is.True, "BPM con pulso > 0");
            Assert.That(H.TextOf("hud-bpm-value"), Is.EqualTo("140"));
            Assert.That(SameColor(H.Find("hud-bpm-value")!.resolvedStyle.color, Tokens.MetricHrHud), Is.True);
            Assert.That(Bounds("hud-bpm").yMin, Is.GreaterThan(Bounds("hud-kcal").yMax - 0.5f), "en vertical, kcal arriba y BPM abajo");
            Assert.That(H.TextOf("hud-kcal-value"), Is.EqualTo("41"), "kcal del monitor");
            yield return Tap("hud-pause");
            var clock = H.TextOf("hud-time-value");
            yield return Advance(3);
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo(clock), "en pausa se congela el reloj");
            Assert.That(H.TextOf("hud-distance-value"), Is.EqualTo(H.ScriptedRower!.Last!.DistanceMeters.ToString()), "y los valores siguen con cada lectura");
        }

        [UnityTest, Category("AC-WKT-17-07")]
        public IEnumerator RotationKeepsTheSession()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return AdvanceTo(10.05);
            var page = Immersive;
            Assert.That(Immersive.Layout!.IsLandscape, Is.False);
            H.Resize(DeviceProfile.TH);
            yield return Frames(3);
            yield return Settle();
            Assert.That(H.Immersive, Is.SameAs(page), "sin reiniciar");
            Assert.That(Immersive.Layout!.IsLandscape, Is.True, "horizontal con el ancho mayor que el alto");
            Assert.That(Hud.Band.IsCompact, Is.True);
            Assert.That(Hud.VitalsInRow, Is.True, "kcal y BPM en fila (DV-33)");
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:10"), "los valores siguen");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
        }

        [UnityTest, Category("AC-WKT-10-07")]
        public IEnumerator StoppedIsOff()
        {
            CreateWorkout(RoutineSeeds.F4A, reading: RowerScripts.R4P);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(40.05);
            Assert.That(Hud.Spm.HasTarget && Hud.Spm.IsOff, Is.True, "parado: SPM fuera");
            Assert.That(Hud.Watts.HasTarget && Hud.Watts.IsOff, Is.True, "y Watts fuera");
            Assert.That(H.TextOf("hud-split-value"), Is.EqualTo("--:--"));
        }

        [UnityTest, Category("AC-WKT-03-08")]
        public IEnumerator StepWithoutGoal()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Sin meta");
            yield return AdvanceTo(30.05);
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo("?"), "stepDurationNone");
            Assert.That(H.IsShown("hud-up-next"), Is.False);
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active), "nunca termina solo");
        }

        // QA-V4a-02 = QA-V4a-ROB-02 (05-qa-lens-robustez §4.2): al salir del inmersivo no queda en el MotionDriver ninguna
        // animación del HUD. El latido de BPM (la foto de reposo conserva el pulso) y el pulso de la fila del paso (la foto de
        // reposo no trae paso) son continuos, así que Settle no los espera: quedaban para siempre, con el HUD de esa sesión y una
        // demanda de 60 fps. Tres salidas: Terminar con Próximo latiendo, Terminar con la cuenta por terminar y la vuelta del
        // segundo plano con la rutina ya terminada (sin transición, DN4a-17). Legacy: el controlador del pulso se libera con el
        // widget (lib/features/workout/immersive_workout_screen.dart:326-328).
        [UnityTest, Category("AC-WKT-13-04"), Category("AC-WKT-14-04"), Category("AC-WKT-14-05")]
        public IEnumerator LeavingTheImmersiveStopsItsContinuousMotions()
        {
            var exits = new[] { "Terminar con Próximo", "Terminar por terminar", "vuelta del segundo plano" };
            foreach (var exit in exits)
            {
                CreateWorkout(RoutineSeeds.F4A, null, WithPulse(140));
                yield return GoToWorkout();
                var restCount = H.Handle.Motion.ActiveCount;
                var restDemands = H.Handle.Pacing.Demands;
                Assert.That(restDemands, Is.EqualTo(0), exit + ": en reposo, antes de la sesión, a 30 fps");
                yield return StartRoutine("Prueba 4a");
                // Calentamiento de 30 s: Próximo desde los 15 s y por terminar desde los 25 s.
                yield return AdvanceTo(exit == "Terminar por terminar" ? 26.05 : 20.05);
                var heart = Hud.Bpm.Heartbeat!;
                var pulse = Hud.Steps!.Pulse;
                Assert.That(H.Handle.Motion.Contains(heart), Is.True, exit + ": el corazón late (pulso 140)");
                Assert.That(pulse.IsPulsing && H.Handle.Motion.Contains(pulse), Is.True, exit + ": la fila late");
                if (exit == "vuelta del segundo plano")
                {
                    H.SendToBackground();
                    H.BringToForeground(230); // la rutina (240 s) termina en segundo plano
                    // It. 4b (DT4a-15): al volver, el diálogo de completado ya está; su Terminar vuelve a reposo.
                    yield return Frames();
                    Assert.That(H.Completion, Is.Not.Null, exit + ": el diálogo de completado al volver");
                    yield return Tap("completion-finish");
                }
                else
                {
                    yield return Tap("hud-finish");
                    yield return Frames();
                    H.Tap("dialog-confirm");
                }

                yield return Settle();
                Assert.That(H.Immersive, Is.Null, exit + ": salió");
                Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle), exit);
                Assert.That(H.Handle.Motion.Contains(heart), Is.False, exit + ": el corazón del HUD sigue en el MotionDriver");
                Assert.That(H.Handle.Motion.Contains(pulse), Is.False, exit + ": el pulso de la fila sigue en el MotionDriver");
                Assert.That(H.Handle.Motion.ActiveCount, Is.EqualTo(restCount), exit + ": el MotionDriver vuelve a lo que tenía en reposo");
                Assert.That(H.Handle.Pacing.Demands, Is.EqualTo(restDemands), exit + ": ninguna demanda de 60 fps de más");
            }
        }

        // QA-V4a-05: un número de tablero que sale del panel con el rodillo en curso (el HUD que se va con las cifras rodando)
        // termina el rodillo en el acto y sale del MotionDriver, sin medir: fuera de un panel, scaledPixelsPerPoint anotaba
        // "Trying to access the DPI setting of a visual element that is not on a panel." en cada cuadro (88 en el player). Al
        // volver a un panel se mide de nuevo.
        [UnityTest, Category("AC-WKT-13-04")]
        public IEnumerator DigitsLeavingThePanelStopRolling()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return AdvanceTo(3.05);
            var warnings = new System.Collections.Generic.List<string>();
            Application.LogCallback onLog = (message, stack, type) =>
            {
                if (message.IndexOf("DPI", StringComparison.Ordinal) >= 0)
                {
                    warnings.Add(message);
                }
            };
            Application.logMessageReceived += onLog;
            try
            {
                var numbers = Hud.Query<RowMate.Presentation.Controls.TabularNumber>().ToList().FindAll(n => n.Text.Length > 0);
                Assert.That(numbers.Count, Is.GreaterThanOrEqualTo(6), "SPM, Split, Watts, kcal, reloj y distancia");
                foreach (var number in numbers)
                {
                    number.SetValue(number.Text + "1", number.Value + 1, true);
                }

                Assert.That(numbers.TrueForAll(n => n.IsRolling && H.Handle.Motion.Contains(n)), Is.True, "las cifras ruedan");
                var page = Hud.parent;
                var index = page.IndexOf(Hud);
                var hud = Hud;
                hud.RemoveFromHierarchy();
                foreach (var number in numbers)
                {
                    Assert.That(number.IsRolling, Is.False, "fuera del panel el rodillo termina en el acto: " + number.name);
                    Assert.That(H.Handle.Motion.Contains(number), Is.False, "y sale del MotionDriver: " + number.name);
                }

                yield return Advance(0.5);
                Assert.That(warnings, Is.Empty, "ningún aviso de DPI: " + string.Join(" | ", warnings));

                page.Insert(index, hud);
                yield return Frames(3);
                foreach (var number in numbers)
                {
                    var ppp = number.scaledPixelsPerPoint;
                    var expected = Mathf.Ceil(number.NaturalBoxWidth * ppp - 0.001f) / ppp;
                    Assert.That(number.resolvedStyle.width, Is.EqualTo(expected).Within(0.01f), "de vuelta en el panel se mide otra vez: " + number.name);
                }
            }
            finally
            {
                Application.logMessageReceived -= onLog;
            }
        }

        [UnityTest, Category("AC-APP-05-08")]
        public IEnumerator ReducedMotion()
        {
            CreateWorkout(RoutineSeeds.F4A, o => o.ReduceMotion = true);
            yield return GoToWorkout();
            yield return Frames();
            H.TapRoutineNamed("Prueba 4a");
            yield return Settle();
            H.Tap("dialog-confirm");
            H.Advance(0.06);
            yield return Frames();
            Assert.That(Immersive.Root.resolvedStyle.opacity, Is.InRange(0.01f, 0.99f), "el inmersivo entra con un fundido");
            yield return Settle();
            yield return Advance(80);
            Assert.That(H.IsShown("hud-up-next"), Is.True);
            var bg = Hud.Steps!.UpNextBg.resolvedStyle.opacity;
            yield return Advance(0.3);
            Assert.That(Hud.Steps.UpNextBg.resolvedStyle.opacity, Is.EqualTo(bg).Within(0.001f), "un realce fijo, sin latir");
            Assert.That(bg, Is.EqualTo(1).Within(0.001f));
        }

        // AC-WKT-17-11, -05 (§3.8; QA-V4b-05: IHT.KcalSlides; QA-V4b-07): montado, en TV. Cuando aparece BPM, kcal arranca desde su
        // lugar viejo (el alto de BPM más la separación) y llega a 0 en 240, con su translate en cada cuadro; BPM funde de 0 a 1. Cuando
        // se va, al revés. Y si BPM ya está al abrir el HUD (kcal todavía sin layout), kcal en su lugar: el translate nunca es NaN.
        [UnityTest, Category("AC-WKT-17-11"), Category("AC-WKT-17-05")]
        public IEnumerator KcalSlides()
        {
            CreateWorkout(RoutineSeeds.Lista, reading: k => k >= 3 && k < 8 ? RowerScripts.R4(k).CopyWith(heartRate: 140) : RowerScripts.R4(k));
            yield return StartFree();
            yield return AdvanceTo(2.2);
            Assert.That(Hud.HasBpm, Is.False, "sin pulso");
            var kcal = H.Find("hud-kcal")!;
            var slide = Hud.KcalSlide;
            var shifts = new System.Collections.Generic.List<double>();
            for (var i = 0; i < 150 && shifts.Count < 30; i++)
            {
                H.Step();
                yield return null;
                if (Hud.HasBpm)
                {
                    shifts.Add(slide.ShiftDp);
                    Assert.That(double.IsNaN(slide.ShiftDp), Is.False);
                    var expected = Math.Abs(slide.ShiftDp) < 1e-4 ? 0f : (float)slide.ShiftDp;
                    Assert.That(kcal.style.translate.value.y.value, Is.EqualTo(expected).Within(0.01f), "el translate de kcal, cuadro " + i);
                }
            }

            Assert.That(shifts.Count, Is.GreaterThan(20), "BPM apareció");
            Assert.That(shifts[0], Is.GreaterThan(30), "kcal arranca desde su lugar viejo (" + shifts[0].ToString("0.0") + ")");
            for (var i = 1; i < shifts.Count; i++)
            {
                Assert.That(shifts[i], Is.LessThanOrEqualTo(shifts[i - 1] + 1e-6), "baja sin volver, cuadro " + i);
            }

            Assert.That(shifts[shifts.Count - 1], Is.EqualTo(0).Within(1e-6), "y llega a su lugar");
            Assert.That(H.Find("hud-bpm")!.resolvedStyle.opacity, Is.EqualTo(1).Within(0.001f), "BPM entero");

            // Las lecturas llegan antes del segundo de la sesión: la k = 8 (sin pulso) ya llegó a los 7,9.
            yield return AdvanceTo(7.0);
            Assert.That(Hud.HasBpm, Is.True, "con pulso hasta la k = 8");
            var away = new System.Collections.Generic.List<double>();
            for (var i = 0; i < 150 && (away.Count == 0 || slide.IsRunning); i++)
            {
                H.Step();
                yield return null;
                if (!Hud.HasBpm)
                {
                    away.Add(slide.ShiftDp);
                    Assert.That(double.IsNaN(slide.ShiftDp), Is.False);
                }
            }

            Assert.That(away.Count, Is.GreaterThan(10), "BPM se fue");
            Assert.That(away[away.Count - 1], Is.EqualTo(0).Within(1e-6), "kcal en su lugar al terminar");
            Assert.That(Math.Abs(Min(away)), Is.GreaterThan(30), "kcal se movió (el alto de BPM)");

            // Con BPM desde el primer estado (VID-07): sin deslizarse y sin NaN.
            CreateWorkout(RoutineSeeds.Lista, reading: WithPulse(140));
            yield return StartFree();
            Assert.That(Hud.HasBpm, Is.True, "con pulso desde el principio");
            Assert.That(double.IsNaN(Hud.KcalSlide.ShiftDp), Is.False, "el primer estado con BPM, sin layout todavía");
            yield return AdvanceTo(0.5);
            Assert.That(Hud.KcalSlide.ShiftDp, Is.EqualTo(0).Within(1e-6), "kcal en su lugar");
            Assert.That(float.IsNaN(H.Find("hud-kcal")!.resolvedStyle.translate.y), Is.False, "el translate de kcal");
        }

        private static double Min(System.Collections.Generic.List<double> values)
        {
            var min = double.MaxValue;
            foreach (var v in values)
            {
                min = Math.Min(min, v);
            }

            return min;
        }
    }
}
