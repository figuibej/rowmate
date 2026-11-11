using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // HBT · El presupuesto del HUD (ARCHITECTURE §7.8; 04a-motor-hud-funcional/02-tech-design §6.13): en el peor caso (rutina
    // con objetivos en SPM y Watts, BPM, Próximo y el spinner del guardado) a lo sumo 60 elementos a la vista bajo #immersive,
    // y 0 B por cuadro mientras solo late (entre dos lecturas).
    public sealed class HudBudgetTests : WorkoutUiTestBase
    {
        public const int MaxVisibleElements = 60;

        private static int CountVisible(VisualElement root)
        {
            var count = 0;
            foreach (var element in root.Query<VisualElement>().Build())
            {
                if (UiQuery.IsShown(element))
                {
                    count++;
                }
            }

            return count;
        }

        [UnityTest, Category("AC-WKT-17-01")]
        public IEnumerator AtMostSixtyVisibleElementsInTheWorstCase()
        {
            CreateWorkout(RoutineSeeds.F4A, o => o.Sessions = HostSessions.Scripted(r => r.FinishDelay = TimeSpan.FromSeconds(5)),
                k => RowerScripts.R4(k).CopyWith(heartRate: 140));
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(80.05);
            yield return Settle();
            Assert.That(Hud.SpmHasTarget && Hud.WattsHasTarget, Is.True, "objetivos en SPM y Watts");
            Assert.That(H.IsShown("hud-bpm"), Is.True, "con BPM");
            Assert.That(H.IsShown("hud-up-next"), Is.True, "con Próximo");
            var routine = CountVisible(Immersive.Root);
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            H.Advance(0.4);
            yield return Frames(3);
            Assert.That(Hud.Controls.ShowsSpinner, Is.True, "con el spinner del guardado");
            var withSpinner = CountVisible(Immersive.Root);
            Assert.That(Math.Max(routine, withSpinner), Is.LessThanOrEqualTo(MaxVisibleElements),
                "elementos a la vista: " + routine + " con la rutina, " + withSpinner + " con el spinner");
        }

        // It. 4b (§3.11; DT4b-17; R4a-03): el peor caso de la 4b, en los cuatro perfiles: los tres objetivos fuera (tres flechas), BPM,
        // Próximo, la cuenta grande y el sello a la vista a la vez. Con las juntas M1, M2, M4 y M6 (y el sello en dos etiquetas, el
        // plan B de S-B4b) queda en 59 o menos; cuando falla, lista cada elemento visible.
        [UnityTest, Category("AC-WKT-17-01"), Category("AC-WKT-14-19")]
        public IEnumerator AtMostSixtyVisibleElementsInTheFourBWorstCase()
        {
            foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280 })
            {
                CreateWorkout(RoutineSeeds.Budget, o =>
                {
                    o.Profile = profile;
                    o.IsDesktop = profile.Code == DeviceProfile.W1280.Code;
                }, k => RowerScripts.R4(k).CopyWith(heartRate: 140));
                yield return StartRoutine("Presupuesto 4b");
                yield return AdvanceTo(5.5);
                yield return Settle();
                Assert.That(Hud.Spm.ArrowShown && Hud.Split.ArrowShown && Hud.Watts.ArrowShown, Is.True, profile.Code + ": las tres flechas");
                Assert.That(H.IsShown("hud-bpm"), Is.True, profile.Code + ": BPM");
                Assert.That(H.IsShown("hud-up-next"), Is.True, profile.Code + ": Próximo");
                Assert.That(H.IsShown("hud-big-countdown"), Is.True, profile.Code + ": la cuenta grande");
                Assert.That(Hud.Steps!.Seal.IsShowing, Is.True, profile.Code + ": el sello");
                var count = CountVisible(Immersive.Root);
                Assert.That(count, Is.LessThanOrEqualTo(MaxVisibleElements), profile.Code + ": " + count + " elementos: " + DescribeVisible(Immersive.Root));
            }
        }

        // It. 15 (AC-D15-30; 14-pulido-plataformas/02-tech-design DT14-30, gate QA-G14-03, R14-10; QA-V15-02): el peor caso de la 4b con la
        // escena DEMORADA (FakeImmersiveScene sin preparar al entrar): el indicador de carga (+1) a la vista desde los 120 ms y, cuando la
        // escena queda lista justo en el peor momento (las tres flechas, BPM, Próximo, la cuenta grande y el sello, a los 5,5 s), su fundido
        // de 0,6 s con el fondo provisorio todavía a la vista (+1) mientras el indicador se va. Se cuenta cuadro a cuadro de los 5,0 a los
        // 6,2 s, en los cuatro perfiles: el máximo ≤ 60.
        [UnityTest, Category("AC-D15-30"), Category("AC-DEBT-09"), Category("AC-WKT-17-01")]
        public IEnumerator DelayedScene()
        {
            foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280 })
            {
                var fake = new FakeImmersiveScene(new RecordingLogger());
                CreateWorkout(RoutineSeeds.Budget, o =>
                {
                    o.Profile = profile;
                    o.IsDesktop = profile.Code == DeviceProfile.W1280.Code;
                    o.FakeScene = fake;
                }, k => RowerScripts.R4(k).CopyWith(heartRate: 140));
                yield return StartRoutine("Presupuesto 4b");
                Assert.That(Hud.SceneLoading, Is.Not.Null);
                var indicator = Hud.SceneLoading!;
                Assert.That(indicator.IsShown, Is.True, profile.Code + ": el indicador a la vista mientras la escena no está");
                yield return AdvanceTo(5.0);
                var worst = 0;
                var worstText = "";
                var sawBoth = false;
                while (H.Scheduler.MonotonicSeconds < SessionStart + 6.2)
                {
                    if (fake.Readiness.Value != RowMate.Presentation.Workout.ImmersiveSceneReadiness.Ready && H.Scheduler.MonotonicSeconds >= SessionStart + 5.4)
                    {
                        fake.SetReadiness(RowMate.Presentation.Workout.ImmersiveSceneReadiness.Ready);
                    }

                    H.Step();
                    yield return null;
                    var count = CountVisible(Immersive.Root);
                    sawBoth |= indicator.IsShown && Hud.Backdrop.style.display.value != DisplayStyle.None && Hud.SceneShown;
                    if (count > worst)
                    {
                        worst = count;
                        worstText = DescribeVisible(Immersive.Root);
                    }
                }

                Assert.That(fake.InPlace, Is.EqualTo(1), profile.Code + ": la escena demorada en su lugar");
                Assert.That(sawBoth, Is.True, profile.Code + ": hubo cuadros con el indicador, el provisorio y la escena a la vez (el peor caso)");
                Debug.Log("[HBT] escena demorada en " + profile.Code + ": hasta " + worst + " elementos");
                Assert.That(worst, Is.LessThanOrEqualTo(MaxVisibleElements), profile.Code + ": " + worst + " elementos: " + worstText);
            }
        }

        // §3.13 (QA-G4bT-11 (a), (c); QA-V4b-05): el trabajo del HUD (aplicar el estado y el generateVisualContent de la fila) con 200
        // pasos, a lo sumo 16 ms por cuadro, de los 26,2 a los 31,5 s de Serie larga: el halo de los últimos 5 s, la cuenta grande
        // (3, 2, 1), el cambio de paso a los 30 s con el pulgar que pasa a la 1 y el halo que se apaga. Serie larga no tiene objetivos
        // (no hay sello): el sello con todo lo demás es el peor caso de AtMostSixtyVisibleElementsInTheFourBWorstCase. Los 0 B por
        // cuadro, en NoAllocationsPerFrameWithTwoHundredPills.
        [UnityTest, Category("AC-WKT-14-19")]
        public IEnumerator LongSeriesStaysSmooth()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Serie larga");
            yield return AdvanceTo(26.2);
            var row = Hud.Steps!.PillRow;
            var worstApply = 0.0;
            var worstDraw = 0.0;
            var sawHalo = false;
            var sawCount = false;
            var sawChange = false;
            for (var i = 0; i < 318; i++)
            {
                H.Step();
                yield return null;
                worstApply = Math.Max(worstApply, Hud.LastApplyMs);
                worstDraw = Math.Max(worstDraw, row.LastDrawMs);
                sawHalo |= row.HaloAlpha > 0.2;
                sawCount |= H.IsShown("hud-big-countdown");
                sawChange |= row.LookAt(0) != RowMate.Core.Workout.StepPillLook.Current && row.LookAt(1) == RowMate.Core.Workout.StepPillLook.Current;
            }

            Assert.That(row.DrawCount, Is.GreaterThan(0), "la fila se dibujó");
            Assert.That(sawHalo && sawCount && sawChange, Is.True, "pasó por el halo (" + sawHalo + "), la cuenta grande (" + sawCount + ") y el cambio de paso (" + sawChange + ")");
            Assert.That(row.LookAt(1), Is.EqualTo(RowMate.Core.Workout.StepPillLook.Current), "el pulgar en la 1");
            Assert.That(worstApply + worstDraw, Is.LessThanOrEqualTo(16), "el trabajo del HUD: aplicar " + worstApply.ToString("0.00") + " ms, la fila " + worstDraw.ToString("0.00") + " ms");
        }

        // QA-V6a-R2-01 (06a-escena-lago/02-tech-design, "Cambios por verificación (ronda 2)"; AC-WKT-14-19, AC-WKT-17-06): las
        // asignaciones EN CUADROS REALES en la franja de LongSeriesStaysSmooth (26,2 a 31,5 s de Serie larga, remando, sin pausa): el
        // halo de los últimos 5 s, el latido de la cuenta del paso, la cuenta grande con su aro, el cambio de paso a los 30 s y el halo
        // que se apaga. Se mide TODO lo del cuadro menos el reloj de prueba: el C# del cuadro (el MotionDriver con el HUD), cada
        // generateVisualContent del inmersivo (FrameAllocations.WatchTree, con la cuenta por elemento) y lo que UI Toolkit hace por el
        // panel (PanelStep: el recálculo de estilos, el layout, los eventos de geometría y las páginas de mallas de UIR, que es donde
        // el Profiler del build vio las asignaciones de cada cuadro). El reloj de prueba (las lecturas del remo y el segundo del motor
        // con su Apply: lo que se arma a 1 Hz, que §20.2 permite) va aparte, con su tope.
        // Lo que se afirma es lo de QA-V6a-R2-01: NINGUNA asignación continua. Antes de la corrección, 242 de 318 cuadros asignaban
        // (cada cuadro de los últimos 5 s). Quedan los cuadros de los eventos: el cambio de paso (≈ 0,2 s seguidos), la primera vez de
        // la cuenta grande y alguna página nueva de UIR cuando la fila se repinta por su relleno o por una lectura (de a un cuadro).
        // El control positivo (un elemento del árbol que asigna en su dibujo y en su evento de geometría en CADA cuadro) se ve en la
        // misma medida y rompe los mismos topes.
        public const int LongSeriesMaxFramesWithAllocations = 32;
        public const int LongSeriesMaxRun = 14;

        [UnityTest, Category("AC-WKT-14-19"), Category("AC-WKT-17-06")]
        public IEnumerator LongSeriesNoAllocationsPerFrame()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Serie larga");
            yield return AdvanceTo(25.7);

            var allocating = new AllocatingElement { name = "control-positivo" };
            Immersive.Root.Add(allocating);
            yield return null;
            var positiveStats = new WindowStats();
            using (var positive = FrameAllocations.WatchTree(Immersive.Root))
            {
                yield return MeasureWindow(positive, positiveStats, SessionStart + 26.2, i =>
                {
                    allocating.MarkDirtyRepaint();
                    allocating.Resize(i);
                });
                positive.Stop();
                Debug.Log("[HBT] control positivo de Serie larga: " + positive + "; " + positiveStats);
                Assert.That(positive.DrawsOf("control-positivo"), Is.GreaterThanOrEqualTo(positiveStats.Frames), "el control se dibujó en cada cuadro: " + positive);
                Assert.That(positive.DrawAllocations, Is.GreaterThanOrEqualTo(positiveStats.Frames), "su dibujo asigna en cada cuadro: " + positive);
                Assert.That(positive.PanelSteps, Is.EqualTo(positiveStats.Frames), "el panel se midió en cada cuadro: " + positive);
                Assert.That(positive.PanelFramesWithAllocations, Is.EqualTo(positiveStats.Frames), "lo que asigna el panel en cada cuadro se ve: " + positive);
                Assert.That(positiveStats.LongestRun, Is.GreaterThan(LongSeriesMaxRun), "el control positivo rompe el tope de cuadros seguidos: " + positiveStats);
            }

            allocating.RemoveFromHierarchy();
            var row = Hud.Steps!.PillRow;
            var countdown = Hud.Steps.Countdown;
            var sawHalo = false;
            var sawCount = false;
            var sawChange = false;
            var sawPulse = false;
            var stats = new WindowStats();
            using (var measured = FrameAllocations.WatchTree(Immersive.Root))
            {
                yield return MeasureWindow(measured, stats, SessionStart + 31.5, _ =>
                {
                    sawHalo |= row.HaloAlpha > 0.2;
                    sawCount |= H.IsShown("hud-big-countdown");
                    sawChange |= row.LookAt(1) == RowMate.Core.Workout.StepPillLook.Current;
                    sawPulse |= Hud.Steps!.Pulse.Scale > 1.05 && countdown.DrawnColor != countdown.resolvedStyle.color;
                });
                measured.Stop();
                Debug.Log("[HBT] Serie larga de 26,2 a 31,5 s: " + measured + "; " + stats);
                Assert.That(sawHalo && sawCount && sawChange && sawPulse, Is.True, "pasó por el halo (" + sawHalo + "), la cuenta grande (" + sawCount
                    + "), el cambio de paso (" + sawChange + ") y el latido de la cuenta del paso (" + sawPulse + ")");
                Assert.That(measured.DrawsOf("hud-big-countdown"), Is.GreaterThan(60), "la cuenta grande se repintó: " + measured);
                Assert.That(measured.DrawsOf("hud-countdown"), Is.GreaterThan(60), "la cuenta del paso se repintó (su latido va en el dibujo): " + measured);
                Assert.That(row.HaloDrawCount, Is.GreaterThan(60), "el halo (en la barra) se repintó: " + row.HaloDrawCount);
                Assert.That(measured.PanelSteps, Is.EqualTo(stats.Frames), "el panel se midió en cada cuadro: " + measured);
                Assert.That(stats.Frames, Is.GreaterThan(300));
                Assert.That(stats.FramesWithAllocations, Is.LessThanOrEqualTo(LongSeriesMaxFramesWithAllocations), "sin asignaciones continuas: " + stats + "; " + measured);
                Assert.That(stats.LongestRun, Is.LessThanOrEqualTo(LongSeriesMaxRun), "ningún tramo de cuadros seguidos más largo que el cambio de paso: " + stats);
                Assert.That(stats.SteadyFramesWithAllocations, Is.LessThanOrEqualTo(2), "de 28,25 a 29,65 s (el halo, el latido y la cuenta grande, sin eventos del reloj): " + stats);
                // Lo de 1 Hz (el segundo del motor con su Apply y las lecturas del remo): a lo sumo unos pocos cuadros por segundo.
                Assert.That(measured.ClockFramesWithAllocations, Is.LessThanOrEqualTo(4 * 6), "el reloj asigna solo a 1 Hz: " + measured);
            }
        }

        // Los cuadros de una ventana: cuántos asignaron (en el paso, en un dibujo o en el panel), el tramo seguido más largo, cuáles
        // (en segundos de la sesión) y los que asignaron en el tramo quieto sin eventos del reloj.
        private sealed class WindowStats
        {
            public int Frames;
            public int FramesWithAllocations;
            public int LongestRun;
            public int SteadyFramesWithAllocations;
            public readonly System.Text.StringBuilder Times = new System.Text.StringBuilder();
            private int _run;

            public void Add(double second, int allocations, bool clockEvent)
            {
                Frames++;
                if (allocations <= 0)
                {
                    _run = 0;
                    return;
                }

                FramesWithAllocations++;
                _run++;
                LongestRun = Math.Max(LongestRun, _run);
                if (second >= 28.25 && second <= 29.65 && !clockEvent)
                {
                    SteadyFramesWithAllocations++;
                }

                Times.Append(second.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append(" (").Append(allocations).Append(clockEvent ? ", con el reloj" : "").Append(") ");
            }

            public override string ToString()
            {
                return Frames + " cuadros, " + FramesWithAllocations + " con asignaciones (tramo seguido más largo " + LongestRun + ", en el tramo quieto "
                       + SteadyFramesWithAllocations + "): " + Times;
            }
        }

        private IEnumerator MeasureWindow(FrameAllocations measured, WindowStats stats, double until, Action<int> beforeFrame)
        {
            var dt = TimeSpan.FromTicks((long)Math.Round(UiTestHost.FrameSeconds * TimeSpan.TicksPerSecond));
            Action advance = () => H.Scheduler.Advance(dt);
            Action tick = H.Handle.Tick;
            var i = 0;
            while (H.Scheduler.MonotonicSeconds < until - 1e-9)
            {
                beforeFrame(i);
                var before = measured.Total;
                var clockBefore = measured.ClockAllocations;
                measured.Clock(advance);
                measured.Step(tick);
                measured.PanelStep();
                yield return null;
                measured.Sample();
                // Lo que el panel no rehízo en PanelStep (lo de después del cuadro: los dibujos del cuadro real) cuenta en este cuadro.
                stats.Add(H.Scheduler.MonotonicSeconds - SessionStart, measured.Total - before, measured.ClockAllocations > clockBefore);
                i++;
            }
        }

        private sealed class AllocatingElement : VisualElement
        {
            public byte[]? Last;
            public byte[]? Geometry;

            public void Resize(int frame)
            {
                style.height = frame % 2 == 0 ? 4 : 5;
            }

            public AllocatingElement()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.width = 4;
                // El control del panel: un cambio de tamaño por cuadro (Resize) y un byte[64] en su GeometryChangedEvent, que UI Toolkit
                // despacha en el Update del panel (adentro de PanelStep).
                RegisterCallback<GeometryChangedEvent>(_ => Geometry = new byte[64]);
                style.height = 4;
                generateVisualContent += context =>
                {
                    Last = new byte[64];
                    var painter = context.painter2D;
                    painter.fillColor = Color.gray;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(0, 0));
                    painter.LineTo(new Vector2(4, 0));
                    painter.LineTo(new Vector2(4, 4));
                    painter.ClosePath();
                    painter.Fill();
                };
            }
        }

        // §3.13 (QA-G4bT-11 (c); QA-V4b-05): 0 B por cuadro (el Recorder de GC.Alloc) con 200 píldoras y el halo que late, y con la
        // fila sola de 1998 pasos (el tope del editor, una HudPillRow armada a mano con el pulgar en el medio y el halo, sin correr el
        // motor 1998 pasos): la tira por columnas de píxel cuesta lo mismo que con 200 (DT4b-21). En pausa y sin lecturas nada cambia
        // de valor; el halo sigue latiendo en pausa (AC-WKT-14-11).
        [UnityTest, Category("AC-WKT-14-19"), Category("AC-WKT-17-06")]
        public IEnumerator NoAllocationsPerFrameWithTwoHundredPills()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Serie larga");
            yield return AdvanceTo(27.1);
            yield return Tap("hud-pause");
            H.ScriptedRower!.Disconnect();
            yield return Settle();
            var row = Hud.Steps!.PillRow;
            Assert.That(row.Count, Is.EqualTo(200));
            Assert.That(row.HaloAlpha, Is.GreaterThan(0.2), "el halo de la en curso");
            yield return MeasureAllocations("200 pasos");

            var types = new RowMate.Core.Models.StepType[1998];
            var looks = new RowMate.Core.Workout.StepPillLook[1998];
            for (var i = 0; i < types.Length; i++)
            {
                types[i] = i % 2 == 0 ? RowMate.Core.Models.StepType.Work : RowMate.Core.Models.StepType.Rest;
                looks[i] = i < 999 ? (i % 3 == 0 ? RowMate.Core.Workout.StepPillLook.Hollow : RowMate.Core.Workout.StepPillLook.Solid)
                    : i == 999 ? RowMate.Core.Workout.StepPillLook.Current : RowMate.Core.Workout.StepPillLook.Pending;
            }

            row.Apply(new RowMate.App.Workout.HudPillRow(types, looks, 999, 27, 30, false, true), false);
            var worstDraw = 0.0;
            var draws = row.DrawCount;
            for (var i = 0; i < 20; i++)
            {
                H.Step();
                yield return null;
                worstDraw = Math.Max(worstDraw, row.LastDrawMs);
            }

            Assert.That(row.Count, Is.EqualTo(1998), "la fila de 1998");
            Assert.That(row.CurrentLayout.Strip && row.CurrentLayout.Narrow, Is.True, "la tira con el pulgar");
            Assert.That(row.DrawCount, Is.GreaterThan(draws), "se dibujó con el halo");
            Assert.That(worstDraw, Is.LessThanOrEqualTo(16), "la fila de 1998: " + worstDraw.ToString("0.00") + " ms");
            yield return MeasureAllocations("1998 pasos");
            Assert.That(row.Count, Is.EqualTo(1998), "siguió la fila de 1998 mientras se medía");
        }

        // It. 5 (WP-B0; 05-series-parciales/02-tech-design §3.6; R4a-03): el peor caso de la 4b medido con M5 (sin el espaciador de la
        // fila de información): 54 en TV y P360 y 55 en TH, L640 y W1280 (antes de M5, 55 y 56: el informe de B0). Lista lo que se ve.
        [UnityTest, Category("AC-WKT-17-01"), Category("AC-WKT-14-04")]
        public IEnumerator TheFourBWorstCaseWithMFive()
        {
            foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.P360, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280 })
            {
                CreateWorkout(RoutineSeeds.Budget, o =>
                {
                    o.Profile = profile;
                    o.IsDesktop = profile.Code == DeviceProfile.W1280.Code;
                }, k => RowerScripts.R4(k).CopyWith(heartRate: 140));
                yield return StartRoutine("Presupuesto 4b");
                yield return AdvanceTo(5.5);
                yield return Settle();
                Assert.That(Immersive.Root.Q(className: "rm-flex-spacer"), Is.Null, profile.Code + ": M5, sin el espaciador");
                // Sin el chip de parciales (la 5 lo suma: Presupuesto 4b es una rutina): lo de la 4b solo.
                var count = CountVisible(Immersive.Root) - (H.IsShown("hud-laps-panel") ? 1 : 0);
                Debug.Log("[HBT] peor caso de la 4b con M5 en " + profile.Code + ": " + count + " elementos (sin el chip de parciales): " + DescribeVisible(Immersive.Root));
                Assert.That(count, Is.LessThanOrEqualTo(profile.Code == DeviceProfile.TV.Code || profile.Code == DeviceProfile.P360.Code ? 54 : 55), profile.Code + ": " + count + " elementos: " + DescribeVisible(Immersive.Root));
            }
        }

        // It. 5 (AC-WKT-18-21; §3.6): el peor caso de la 5: Presupuesto 5 (la rutina del presupuesto de la 4b dentro de una serie ×2: a
        // los 5,5 s los tres objetivos fuera, BPM, Próximo, la cuenta grande, el sello y el panel de repeticiones) con los dos paneles
        // abiertos, en los cinco perfiles: a lo sumo 60 (cada panel es un elemento en cualquier estado; sus hijos ocultos no cuentan).
        [UnityTest, Category("AC-WKT-18-21"), Category("AC-WKT-17-01")]
        public IEnumerator AtMostSixtyVisibleElementsInTheFiveWorstCase()
        {
            foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.P360, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280 })
            {
                CreateWorkout(RoutineSeeds.Budget5, o =>
                {
                    o.Profile = profile;
                    o.IsDesktop = profile.Code == DeviceProfile.W1280.Code;
                    o.Preferences = BothOpen();
                }, k => RowerScripts.R4(k).CopyWith(heartRate: 140));
                yield return StartRoutine("Presupuesto 5");
                yield return AdvanceTo(5.5);
                yield return Settle();
                var series = Hud.Series!;
                Assert.That(H.IsShown("hud-laps-panel") && H.IsShown("hud-reps-panel"), Is.True, profile.Code + ": los dos paneles");
                Assert.That(series.Expanded.Laps && series.Expanded.Reps, Is.True, profile.Code + ": abiertos");
                Assert.That(H.IsShown("hud-bpm") && H.IsShown("hud-up-next") && H.IsShown("hud-big-countdown"), Is.True,
                    profile.Code + ": BPM, Próximo y la cuenta grande");
                Assert.That(Hud.Steps!.Seal.IsShowing, Is.True, profile.Code + ": el sello");
                var count = CountVisible(Immersive.Root);
                Debug.Log("[HBT] peor caso de la 5 en " + profile.Code + ": " + count + " elementos");
                Assert.That(count, Is.LessThanOrEqualTo(MaxVisibleElements), profile.Code + ": " + count + " elementos: " + DescribeVisible(Immersive.Root));
            }
        }

        private static RowMate.Core.Common.MemoryPreferences BothOpen(bool laps = true)
        {
            var prefs = new RowMate.Core.Common.MemoryPreferences();
            prefs.SetBool(RowMate.App.Workout.SeriesPanelPrefs.LapsKey, laps);
            prefs.SetBool(RowMate.App.Workout.SeriesPanelPrefs.RepsKey, true);
            return prefs;
        }

        // It. 5 (AC-WKT-18-21; §3.6; QA-G5T-03; DT5-18): 0 B por cuadro EN CUADROS REALES, con el repintado adentro (FrameAllocations),
        // 30 cuadros de cada animación (0,5 s): abrir, la vuelta nueva con su marca, el cambio de contenido por pieza, la repetición
        // nueva y cerrar. Se afirma 0 sin restar ninguna línea de base (medido solo adentro del dibujo y del paso, lo limpio da 0). El
        // control positivo, en FrameAllocationsTests. Las demás escenas del diseño, en SeriesNoAllocationsPerFrameInMoreScenes (LM5-04).
        [UnityTest, Category("AC-WKT-18-21")]
        public IEnumerator SeriesNoAllocationsPerFrame()
        {
            CreateWorkout(RoutineSeeds.F5, o => o.Preferences = BothOpen(false));
            yield return StartRoutine("Series 600");
            // Calentamiento de los caminos: abrir y cerrar una vez antes de medir.
            yield return AdvanceTo(30.2);
            H.TapText("500 m");
            yield return Settle();
            H.TapText("Parciales 500 m");
            yield return Settle();
            // Cada ventana: 30 cuadros (0,5 s) desde justo después de un segundo. El reloj de prueba (el segundo del motor y la lectura
            // del remo, lo que se arma a 1 Hz, que §20.2 permite) avanza fuera de la medida: lo que queda es el cuadro a cuadro de las
            // animaciones, el paso y el repintado.
            // Abrir (el resorte, los fundidos, la flecha, el revelar) con el FLIP de repeticiones.
            yield return AdvanceTo(150.02);
            H.TapText("500 m");
            yield return MeasureSeries("abrir");
            // La vuelta nueva (#1 a los 155) con su marca.
            yield return AdvanceTo(155.02);
            yield return MeasureSeries("la vuelta nueva");
            // El cambio de contenido por pieza (a los 181: la vuelta en curso sale, el vacío entra).
            yield return AdvanceTo(181.02);
            yield return MeasureSeries("el cambio por pieza");
            // La repetición nueva (a los 201) y el cambio de pieza de repeticiones.
            yield return AdvanceTo(201.02);
            yield return MeasureSeries("la repetición nueva");
            // Cerrar.
            yield return AdvanceTo(210.02);
            H.TapText("Parciales 500 m");
            yield return MeasureSeries("cerrar");
        }

        // LM5-04 (verificación de la 5, ronda 1): las escenas de §4.5 que faltaban, 30 cuadros cada una: la serie que empieza (el
        // panel de repeticiones aparece y entra la línea Rep), la marca que se apaga (600-1400 ms de la vuelta nueva), el anillo de
        // foco en modo teclado con el resorte, la rueda de las filas en W1280 y la máscara de ColumnScroll en TH (DrawMaskedGlass y
        // la regla 3 en cada cuadro). Antes de medir, cada camino corre una vez (los glifos y las cachés de la primera vez).
        [UnityTest, Category("AC-WKT-18-21")]
        public IEnumerator SeriesNoAllocationsPerFrameInMoreScenes()
        {
            CreateWorkout(RoutineSeeds.F5, o => o.Preferences = BothOpen());
            yield return StartRoutine("Series 600");
            // Calentamiento: los dos paneles en serie (con el vacío de repeticiones y la línea Rep) y la salida de repeticiones.
            yield return AdvanceTo(12.2);
            var texts = H.App.Texts.Value;
            var inSeries = SeriesSeeds.Largo(texts, H.HudState!.Session) with { Reps = Array.Empty<RowMate.App.Workout.HudRepRow>() };
            H.OverrideSeries(inSeries);
            yield return Settle();
            H.OverrideSeries(null);
            yield return AdvanceTo(14.2);
            yield return Settle();
            Assert.That(Hud.Series!.RepsShown, Is.False, "el calentamiento no está en serie");
            // La serie que empieza (a los 31: P-01), desde antes del segundo para que el dato entre en la ventana.
            yield return AdvanceTo(30.9);
            yield return MeasureSeries("la serie que empieza", 36);
            Assert.That(Hud.Series!.RepsShown, Is.True, "apareció repeticiones");
            yield return Settle();
            // La marca de #1 (a los 155) que se apaga: de los 620 a los 1420 ms.
            yield return AdvanceTo(155.62);
            Assert.That(Hud.Series!.Laps.Visual.MarkerAlpha0, Is.GreaterThan(0.5), "la marca encendida");
            yield return MeasureSeries("la marca que se apaga", 48);
            Assert.That(Hud.Series!.Laps.Visual.MarkerAlpha0, Is.LessThan(0.05), "y apagada");
            // El anillo de foco en modo teclado, con el resorte de cerrar y abrir.
            yield return AdvanceTo(160.02);
            H.PressTab();
            H.PressTab();
            H.PressTab();
            Assert.That(H.Focused, Is.SameAs(Hud.Series!.Laps));
            H.PressEnter();
            yield return Settle();
            H.PressEnter();
            yield return MeasureSeries("el anillo de foco", 30);
            Assert.That(Hud.Series!.Laps.HasFocusRing, Is.True, "con el anillo");
            // La capa de hover (REPORT de la 5 §7.1; 06a-escena-lago/00-deuda-5 §3): no había entrado en la medida porque el puntero
            // de prueba no la prendía. El puntero entra en parciales (sube en 90 ms) y sale (baja en 160 ms) en la misma ventana.
            yield return Settle();
            var hoverPeak = 0.0;
            var lapsName = Hud.Series!.Laps.name;
            yield return MeasureSeries("la capa de hover", 30, i =>
            {
                if (i == 0)
                {
                    H.PointerEnter(lapsName);
                }
                else if (i == 12)
                {
                    H.PointerLeave(lapsName);
                }

                hoverPeak = Math.Max(hoverPeak, Hud.Series!.Laps.Visual.HoverAlpha);
            });
            Assert.That(hoverPeak, Is.EqualTo(RowMate.Core.Design.MotionSpecs.SeriesHoverAlpha).Within(1e-6), "el hover llegó a su alfa");
            Assert.That(Hud.Series!.Laps.Visual.HoverAlpha, Is.EqualTo(0).Within(1e-6), "y se apagó");

            // La rueda de las filas en W1280 (S-LARGO: 4 vueltas y 3 repeticiones).
            CreateWorkout(RoutineSeeds.F5, o =>
            {
                o.Profile = DeviceProfile.W1280;
                o.IsDesktop = true;
                o.Preferences = BothOpen();
            });
            yield return StartRoutine("Series 600");
            yield return AdvanceTo(340.2);
            H.OverrideSeries(SeriesSeeds.Largo(H.App.Texts.Value, H.HudState!.Session));
            yield return Settle();
            H.Wheel(Hud.Series!.Laps.worldBound.center, 1);
            yield return Settle();
            H.Wheel(Hud.Series!.Laps.worldBound.center, -1);
            yield return Settle();
            H.Wheel(Hud.Series!.Laps.worldBound.center, 1);
            yield return MeasureSeries("la rueda (W1280)", 30);
            Assert.That(Hud.Series!.Laps.RowsScrollBase, Is.GreaterThan(1), "las filas se desplazaron");

            // La máscara de ColumnScroll en TH (los dos abiertos, CAP-16): la columna con la rueda.
            CreateWorkout(RoutineSeeds.F5, o =>
            {
                o.Profile = DeviceProfile.TH;
                o.Preferences = BothOpen();
            });
            yield return StartRoutine("Series 600");
            yield return AdvanceTo(380.2);
            H.Wheel(Hud.Series!.Laps.worldBound.center, 1);
            yield return Settle();
            H.Wheel(Hud.Series!.Laps.worldBound.center, -1);
            yield return Settle();
            H.Wheel(Hud.Series!.Laps.worldBound.center, 1);
            yield return MeasureSeries("la máscara (TH)", 30);
            Assert.That(Hud.Series!.ScrollBase, Is.GreaterThan(1), "la columna se desplazó");
        }

        private const int FramesPerScene = 30;

        private IEnumerator MeasureSeries(string label, int frames = FramesPerScene, Action<int>? beforeFrame = null)
        {
            var measured = FrameAllocations.Watch(Hud.Series!.Laps, Hud.Series.Reps);
            var dt = TimeSpan.FromTicks((long)Math.Round(UiTestHost.FrameSeconds * TimeSpan.TicksPerSecond));
            for (var i = 0; i < frames; i++)
            {
                // Lo que la escena hace antes del cuadro (el puntero de prueba de la capa de hover), fuera de la medida, como el reloj.
                beforeFrame?.Invoke(i);
                // El reloj de prueba (las lecturas del remo y el segundo del motor, con su Apply: lo que se arma a 1 Hz, que §20.2 permite)
                // fuera de la medida; adentro, el cuadro de la UI (el MotionDriver con las motions de los paneles y sus callbacks) y el
                // repintado de los paneles. La sonda de WP-B1 vio que lo único que asignaba en una ventana era la lectura del remo.
                H.Scheduler.Advance(dt);
                measured.Step(H.Handle.Tick);
                yield return null;
                measured.Sample();
            }

            measured.Stop();
            Debug.Log("[HBT] " + label + ": " + measured);
            Assert.That(measured.Draws, Is.GreaterThan(5), label + ": los paneles se repintaron en los cuadros medidos");
            Assert.That(measured.Total, Is.EqualTo(0), label + ": " + measured);
            measured.Dispose();
        }

        // It. 5 (§3.6; ARCHITECTURE §20.2): el trabajo de los paneles por cuadro en el peor caso animando (S-LARGO con 4 vueltas, la
        // línea y 3 repeticiones; abrir y una vuelta entrando): la mediana ≤ 1,0 ms (el tope de UI, en el Editor como indicador) y el
        // peor cuadro ≤ 16 ms (el de la 4b), con el Apply del HUD y el generateVisualContent de los dos paneles.
        [UnityTest, Category("AC-WKT-18-21")]
        public IEnumerator SeriesWorstCaseWork()
        {
            CreateWorkout(RoutineSeeds.F5, o => o.Preferences = BothOpen(false));
            yield return StartRoutine("Series 600");
            yield return AdvanceTo(170.2);
            var largo = SeriesSeeds.Largo(H.App.Texts.Value, H.HudState!.Session);
            H.OverrideSeries(largo);
            yield return Settle();
            var samples = new System.Collections.Generic.List<double>();
            var worst = 0.0;
            // Abrir con todo adentro.
            H.TapText("500 m");
            for (var i = 0; i < 40; i++)
            {
                H.Step();
                yield return null;
                var work = Hud.LastApplyMs + Hud.Series!.Laps.LastDrawMs + Hud.Series.Reps.LastDrawMs;
                samples.Add(work);
                worst = Math.Max(worst, work);
            }

            // Una vuelta entrando (la 2000) con su marca.
            var newer = new RowMate.App.Workout.HudLapRow[4];
            newer[0] = SeriesSeeds.Lap(H.App.Texts.Value, 2000, SeriesSeeds.LongLapSeconds);
            for (var i = 1; i < 4; i++)
            {
                newer[i] = largo.Laps[i - 1];
            }

            H.OverrideSeries(largo with { Laps = newer, LapCount = 2000 });
            for (var i = 0; i < 20; i++)
            {
                H.Step();
                yield return null;
                var work = Hud.LastApplyMs + Hud.Series!.Laps.LastDrawMs + Hud.Series.Reps.LastDrawMs;
                samples.Add(work);
                worst = Math.Max(worst, work);
            }

            samples.Sort();
            var median = samples[samples.Count / 2];
            Debug.Log("[HBT] trabajo de los paneles: mediana " + median.ToString("0.000") + " ms, peor " + worst.ToString("0.000") + " ms en "
                      + samples.Count + " cuadros");
            Assert.That(median, Is.LessThanOrEqualTo(1.0), "la mediana (§20.2): " + median.ToString("0.000") + " ms");
            Assert.That(worst, Is.LessThanOrEqualTo(16), "el peor cuadro: " + worst.ToString("0.000") + " ms");
        }

        private IEnumerator MeasureAllocations(string label)
        {
            // Calentamiento de los caminos: unos cuadros antes de medir.
            for (var i = 0; i < 10; i++)
            {
                H.Step();
                yield return null;
            }

            var allocations = GcAllocations.During(() =>
            {
                for (var i = 0; i < 20; i++)
                {
                    H.Step();
                }
            });
            Assert.That(allocations, Is.EqualTo(0), label + ": " + allocations + " asignaciones en 20 cuadros");
        }

        private static string DescribeVisible(VisualElement root)
        {
            var names = new System.Text.StringBuilder();
            foreach (var element in root.Query<VisualElement>().Build())
            {
                if (UiQuery.IsShown(element))
                {
                    names.Append(string.IsNullOrEmpty(element.name) ? element.GetType().Name : element.name).Append(", ");
                }
            }

            return names.ToString();
        }

        [UnityTest, Category("AC-WKT-17-06")]
        public IEnumerator NoAllocationsPerFrameWhileItOnlyBeats()
        {
            CreateWorkout(RoutineSeeds.F4A, null, k => RowerScripts.R4(k).CopyWith(heartRate: 140));
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(80.1);
            // En pausa y sin lecturas nada cambia de valor: solo laten el corazón y Próximo (que sigue en pausa, AC-WKT-14-06).
            yield return Tap("hud-pause");
            H.ScriptedRower!.Disconnect();
            yield return Settle();
            Assert.That(Hud.Steps!.Pulse.IsPulsing, Is.True);
            // Calentamiento de los caminos: unos cuadros antes de medir.
            for (var i = 0; i < 10; i++)
            {
                H.Step();
            }

            yield return Frames();
            // Con el Recorder de "GC.Alloc" (GcAllocations): en Unity GC.GetAllocatedBytesForCurrentThread da siempre 0 y esta
            // prueba no medía nada (04b-hud-exr, "Cambios por verificación (ronda 1)").
            var allocations = GcAllocations.During(() =>
            {
                for (var i = 0; i < 20; i++)
                {
                    H.Step();
                }
            });
            Assert.That(allocations, Is.EqualTo(0), "0 B por cuadro del HUD (ARCHITECTURE §7.8): " + allocations + " asignaciones en 20 cuadros");
        }
    }
}
