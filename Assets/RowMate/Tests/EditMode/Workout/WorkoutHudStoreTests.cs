using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WHS: el estado del HUD (02-tech-design §2.11 de la It. 4a, tabla "Campo del HUD"): los textos ya formateados y el
    // semáforo de cada tarjeta, en es y en, con el motor de verdad. La parte de datos de AC-WKT-10-05 (obj., la flecha) y de
    // AC-WKT-14-02 a -05 (tipo, pastilla, Próximo y la cuenta) se afirma acá sin su categoría: esos AC son de pantalla y su
    // resultado sale de IHT (PlayMode), así la matriz los deja en PENDIENTE UNITY hasta tener el de Unity.
    // legacy: lib/features/workout/immersive_workout_screen.dart:330-475 (fila del paso), :560-733 (tarjetas y banda).
    [TestFixture]
    public sealed class WorkoutHudStoreTests
    {
        [Test, Category("AC-WKT-03-08")]
        public void NoGoalCountdownIsQuestionMark()
        {
            // DV-25: un paso sin meta muestra ? (stepDurationNone), en es y en; sin "por terminar" ni "a continuación".
            foreach (var l10n in LocalizationFixture.Both)
            {
                using (var h = new WorkoutHarness())
                using (var hud = Hud(h, l10n))
                {
                    h.Routine(RoutineSeeds.SinMeta());
                    h.At(42);
                    var step = hud.State.Value.Step!;
                    Assert.That(step.Countdown, Is.EqualTo("?"), l10n.LanguageCode);
                    Assert.That(step.CountdownValue, Is.EqualTo(0.0));
                    Assert.That(step.EndingSoon, Is.False);
                    Assert.That(step.UpNextType, Is.Null);
                    Assert.That(step.Target, Is.Null, "sin objetivos, sin pastilla");
                    Assert.That(step.TypeText, Is.EqualTo(l10n.Get(L10nKeys.StepTypeWork)));
                }
            }
        }

        [Test, Category("AC-WKT-10-06")]
        public void EvaluatedOnEveryReadingAlsoInPause()
        {
            // Cada lectura rehace el semáforo en el acto, sin promedios, también en pausa: Corta, trabajo con SPM 18.
            using (var h = new WorkoutHarness())
            using (var hud = Hud(h, LocalizationFixture.Es))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(3);
                hud.Pause();
                Assert.That(hud.State.Value.Phase, Is.EqualTo(WorkoutPhase.Paused));
                Assert.That(hud.State.Value.ShowResume, Is.True);
                Assert.That(hud.State.Value.ControlsEnabled, Is.True);
                var states = new StateRecorder<WorkoutHudState>(hud.State);
                h.Rower.Emit(RowerScripts.R4(3) with { StrokeRate = 17.5 });
                Assert.That(states.Count, Is.EqualTo(1), "un estado por lectura");
                Assert.That(states.Last.Spm.OffTarget, Is.True, "fuera desde la lectura misma");
                Assert.That(states.Last.Spm.Arrow, Is.EqualTo(TargetArrow.Up));
                Assert.That(states.Last.Spm.Value, Is.EqualTo("17.5"));
                h.Rower.Emit(RowerScripts.R4(3) with { StrokeRate = 18.0 });
                Assert.That(states.Count, Is.EqualTo(2));
                Assert.That(states.Last.Spm.OffTarget, Is.False, "18 contra 18 cumple");
                Assert.That(states.Last.Spm.Arrow, Is.EqualTo(TargetArrow.None));
                h.Rower.Emit(RowerScripts.R4(3) with { StrokeRate = 17.9 });
                Assert.That(states.Last.Spm.OffTarget, Is.True, "sin margen de tolerancia");
                hud.Resume();
                Assert.That(hud.State.Value.Phase, Is.EqualTo(WorkoutPhase.Active));
                Assert.That(hud.State.Value.ShowResume, Is.False);
                states.Dispose();
            }
        }

        [Test, Category("AC-WKT-11-13")]
        public void TargetLabelOfTheExpandedStep()
        {
            // La etiqueta de objetivos del paso expandido, con el objetivo ya progresado y su 📈 (partida para dibujar el ícono),
            // el mismo texto en es y en: Prueba 4a, trabajo de la repetición 2; Pirámide 500, primer trabajo.
            foreach (var l10n in LocalizationFixture.Both)
            {
                using (var h = new WorkoutHarness())
                using (var hud = Hud(h, l10n))
                {
                    h.Routine(RoutineSeeds.PruebaCuatroA());
                    h.At(125);
                    var step = hud.State.Value.Step!;
                    Assert.That(step.Type, Is.EqualTo(StepType.Work));
                    var label = step.Target!;
                    Assert.That(label.Text, Is.EqualTo(">160W · 24 spm · " + TrendText.TrendEmoji + " SPM +2/rep"), l10n.LanguageCode);
                    Assert.That(label.Before, Is.EqualTo(">160W · 24 spm · "));
                    Assert.That(label.HasTrend, Is.True);
                    Assert.That(label.After, Is.EqualTo(" SPM +2/rep"));
                }

                using (var h = new WorkoutHarness())
                using (var hud = Hud(h, l10n))
                {
                    h.Routine(RoutineSeeds.PiramideQuinientos());
                    h.At(601);
                    var label = hud.State.Value.Step!.Target!;
                    Assert.That(label.Text, Is.EqualTo("150-180W · 02:05/500m · " + TrendText.TrendEmoji + " W +10/rep, Split -2s/rep"), l10n.LanguageCode);
                }
            }
        }

        [Test, Category("AC-WKT-11-13")]
        public void CardTargetsAndTrafficLight()
        {
            // Prueba 4a con R-4, trabajo de la repetición 2 (vatios mín. 160, SPM 24): SPM cumple (24 contra 24), Watts fuera
            // con ▲ (150 contra 160), Split sin objetivo. hudTarget con el objetivo: obj. 24 / target 160.
            using (var h = new WorkoutHarness())
            using (var es = Hud(h, LocalizationFixture.Es))
            using (var en = Hud(h, LocalizationFixture.En))
            {
                h.Routine(RoutineSeeds.PruebaCuatroA());
                h.At(125);
                var state = es.State.Value;
                Assert.That(state.Spm.Value, Is.EqualTo("24.0"));
                Assert.That(state.Spm.HasTarget, Is.True);
                Assert.That(state.Spm.OffTarget, Is.False);
                Assert.That(state.Spm.Arrow, Is.EqualTo(TargetArrow.None));
                Assert.That(state.Spm.TargetText, Is.EqualTo("obj. 24"));
                Assert.That(state.Watts.Value, Is.EqualTo("150"));
                Assert.That(state.Watts.OffTarget, Is.True);
                Assert.That(state.Watts.Arrow, Is.EqualTo(TargetArrow.Up));
                Assert.That(state.Watts.TargetText, Is.EqualTo("obj. 160"));
                Assert.That(state.Split.Value, Is.EqualTo("02:05"));
                Assert.That(state.Split.HasTarget, Is.False);
                Assert.That(state.Split.TargetText, Is.Null);
                Assert.That(en.State.Value.Watts.TargetText, Is.EqualTo("target 160"));
                Assert.That(en.State.Value.Spm.TargetText, Is.EqualTo("target 24"));
            }

            using (var h = new WorkoutHarness(k => RowerScripts.R4(k) with { Pace500mSeconds = 130 }))
            using (var hud = Hud(h, LocalizationFixture.Es))
            {
                // Pirámide 500, primer trabajo: split objetivo 125 con el formato de su objetivo (obj. 02:05); con 02:10, fuera
                // y con ▼ (hay que bajar).
                h.Routine(RoutineSeeds.PiramideQuinientos());
                h.At(601);
                var split = hud.State.Value.Split;
                Assert.That(split.Value, Is.EqualTo("02:10"));
                Assert.That(split.HasTarget, Is.True);
                Assert.That(split.TargetText, Is.EqualTo("obj. 02:05"));
                Assert.That(split.OffTarget, Is.True);
                Assert.That(split.Arrow, Is.EqualTo(TargetArrow.Down));
                Assert.That(hud.State.Value.Watts.OffTarget, Is.False, "150 contra 150 cumple");
            }
        }

        [Test, Category("AC-WKT-11-13")]
        public void StepRowTexts()
        {
            // El tipo localizado, la cuenta M:SS, "por terminar" de 1 a 5 s y "a continuación" con el tipo siguiente.
            using (var h = new WorkoutHarness())
            using (var es = Hud(h, LocalizationFixture.Es))
            using (var en = Hud(h, LocalizationFixture.En))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(7);
                var step = es.State.Value.Step!;
                Assert.That(step.TypeText, Is.EqualTo("Trabajo"));
                Assert.That(en.State.Value.Step!.TypeText, Is.EqualTo("Work"));
                Assert.That(step.Countdown, Is.EqualTo("0:03"));
                Assert.That(step.CountdownValue, Is.EqualTo(3.0));
                Assert.That(step.EndingSoon, Is.True);
                Assert.That(step.UpNextType, Is.EqualTo(StepType.Rest));
                Assert.That(step.UpNextTypeText, Is.EqualTo("Descanso"));
                Assert.That(en.State.Value.Step!.UpNextTypeText, Is.EqualTo("Rest"));
                Assert.That(step.Target!.Text, Is.EqualTo("18 spm · " + TrendText.TrendEmoji + " SPM +2/rep"));
                Assert.That(es.State.Value.Clock, Is.EqualTo("00:07"));
                Assert.That(es.State.Value.ClockValue, Is.EqualTo(7));
                Assert.That(es.State.Value.Distance, Is.EqualTo("1028"), "L(7), llegada a los 6,5 s");
                Assert.That(es.State.Value.Kcal, Is.EqualTo("40"));
                Assert.That(es.State.Value.Bpm, Is.Null, "sin pulso, sin tarjeta");
                Assert.That(es.State.Value.IsRoutine, Is.True);
            }

            using (var h = new WorkoutHarness())
            using (var hud = Hud(h, LocalizationFixture.Es))
            {
                // Por distancia: el largo total del paso (Q-51), sin "por terminar" ni "a continuación".
                h.Routine(RoutineSeeds.DistanciaCien());
                h.At(5);
                var step = hud.State.Value.Step!;
                Assert.That(step.Countdown, Is.EqualTo("100m"));
                Assert.That(step.CountdownValue, Is.EqualTo(100.0));
                Assert.That(step.EndingSoon, Is.False);
                Assert.That(step.UpNextType, Is.Null);
            }
        }

        [Test, Category("AC-WKT-02-08")]
        public void OneInstantStateAfterBackground()
        {
            // QA-G4aT-07: en segundo plano el motor no publica fotos, así que el HUD no arma nada durante el alcance; al volver,
            // UN estado con Instant (la vista lo aplica sin rodillos ni empujones) y el siguiente, ya sin Instant.
            var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
            var rower = new ScriptedRower(harness.Scheduler, RowerScripts.R4, harness.Logger);
            using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), new InMemorySessionRepository(),
                       harness.Scheduler, harness.Lifecycle, harness.Logger))
            using (var hud = new WorkoutHudStore(engine, Texts(LocalizationFixture.Es), harness.Logger))
            {
                rower.Start();
                harness.RunUntil(0);
                engine.StartWithRoutine(RoutineSeeds.Corta());
                harness.RunUntil(5);
                Assert.That(hud.State.Value.Instant, Is.False);
                harness.Background(6);
                var states = new StateRecorder<WorkoutHudState>(hud.State);
                harness.Foreground(23);
                Assert.That(states.Count, Is.EqualTo(1), "un solo estado al volver");
                Assert.That(states.Last.Instant, Is.True);
                Assert.That(states.Last.Clock, Is.EqualTo("00:23"), "el presente");
                Assert.That(states.Last.Step!.Countdown, Is.EqualTo("0:02"));
                harness.RunUntil(24);
                Assert.That(states.Count, Is.GreaterThan(1));
                Assert.That(states.Last.Instant, Is.False);
                states.Dispose();
            }

            rower.Dispose();
        }

        [Test, Category("AC-WKT-02-04"), Category("AC-WKT-02-08")]
        public void OneInstantStateAfterALateFrameInTheForeground()
        {
            // QA-V4a-ROB-05 (05-qa-lens-robustez, L05): un cuadro que llega 1 h tarde EN PRIMER PLANO (la app trabada, o el equipo
            // suspendido con un monótono que siguió contando) procesa en orden los 3600 segundos vencidos, sin perder ninguno,
            // pero publica UNA foto al final, con Instant, como la vuelta del segundo plano. Antes eran 3600 fotos y 3600 estados
            // del HUD en el mismo cuadro. Un cuadro con un solo segundo vencido sigue como siempre.
            var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
            var rower = new ScriptedRower(harness.Scheduler, RowerScripts.R4, harness.Logger);
            var sessions = new InMemorySessionRepository();
            using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), sessions, harness.Scheduler,
                       harness.Lifecycle, harness.Logger))
            using (var hud = new WorkoutHudStore(engine, Texts(LocalizationFixture.Es), harness.Logger))
            {
                rower.Start();
                harness.RunUntil(0);
                engine.StartFree();
                harness.RunUntil(2);
                var snapshots = new StateRecorder<WorkoutSnapshot>(engine.State);
                var states = new StateRecorder<WorkoutHudState>(hud.State);
                harness.Stall(3600);
                Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(3602), "ningún segundo perdido (AC-WKT-02-04)");
                Assert.That(sessions.AllPoints.Count, Is.EqualTo(720), "un punto cada 5 s activos");
                var instants = 0;
                foreach (var snapshot in snapshots.Values)
                {
                    instants += snapshot.Instant ? 1 : 0;
                }

                Assert.That(snapshots.Count, Is.LessThanOrEqualTo(2), "la lectura del remo y UNA foto del motor, no una por segundo");
                Assert.That(instants, Is.EqualTo(1), "la del motor, con Instant");
                Assert.That(states.Count, Is.LessThanOrEqualTo(2), "a lo sumo dos estados del HUD");
                Assert.That(states.Last.Instant, Is.True, "el último, con Instant: sin rodillos ni empujones");
                Assert.That(states.Last.Clock, Is.EqualTo("1:00:02"), "el presente");
                var before = states.Count;
                harness.RunUntil(3603.01);
                Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(3603));
                Assert.That(states.Count, Is.GreaterThan(before));
                Assert.That(states.Last.Instant, Is.False, "el segundo siguiente, como siempre");
                snapshots.Dispose();
                states.Dispose();
            }

            rower.Dispose();
        }

        [Test, Category("AC-WKT-11-13")]
        public void LanguageChangeRebuildsTheState()
        {
            using (var h = new WorkoutHarness())
            {
                var texts = new ObservableValue<Localizer>(LocalizationFixture.Es, h.Logger);
                using (var hud = new WorkoutHudStore(h.Engine, new ReadOnlyObservable<Localizer>(texts), h.Logger))
                {
                    h.Routine(RoutineSeeds.Corta());
                    h.At(2);
                    Assert.That(hud.State.Value.Spm.TargetText, Is.EqualTo("obj. 18"));
                    var revision = hud.State.Value.Revision;
                    texts.Value = LocalizationFixture.En;
                    Assert.That(hud.State.Value.Spm.TargetText, Is.EqualTo("target 18"));
                    Assert.That(hud.State.Value.Step!.TypeText, Is.EqualTo("Work"));
                    Assert.That(hud.State.Value.Revision, Is.EqualTo(revision), "la misma foto, otro idioma");
                }
            }
        }

        [Test, Category("AC-WKT-01-06")]
        public void ControlsFollowThePhase()
        {
            // :923-948: controles en marcha y en pausa; mientras se guarda (terminada) no; en reposo tampoco.
            using (var h = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FinishDelay = TimeSpan.FromSeconds(1) }))
            using (var hud = Hud(h, LocalizationFixture.Es))
            {
                Assert.That(hud.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(hud.State.Value.ControlsEnabled, Is.False);
                Assert.That(hud.State.Value.Step, Is.Null);
                h.Free();
                Assert.That(hud.State.Value.ControlsEnabled, Is.True);
                Assert.That(hud.State.Value.IsRoutine, Is.False);
                Assert.That(hud.State.Value.Step, Is.Null, "libre: sin fila del paso");
                h.At(3);
                var finishing = hud.FinishAsync();
                Assert.That(hud.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(hud.State.Value.ControlsEnabled, Is.False, "guardando");
                h.At(4.5);
                Assert.That(finishing.IsCompleted, Is.True);
                Assert.That(hud.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(hud.State.Value.ControlsEnabled, Is.False);
            }
        }

        [Test]
        public void DisposeStopsFollowingTheEngine()
        {
            using (var h = new WorkoutHarness())
            {
                var hud = Hud(h, LocalizationFixture.Es);
                h.Free();
                var last = hud.State.Value;
                hud.Dispose();
                h.At(5);
                Assert.That(hud.State.Value, Is.SameAs(last));
                hud.Pause();
                Assert.That(h.Engine.Phase, Is.EqualTo(WorkoutPhase.Active), "después de Dispose no manda nada");
                Assert.That(hud.FinishAsync().IsCompleted, Is.True);
                Assert.That(h.Engine.Phase, Is.EqualTo(WorkoutPhase.Active));
                hud.Dispose();
            }
        }

        [Test]
        public void ArgumentsAreChecked()
        {
            using (var h = new WorkoutHarness())
            {
                Assert.Throws<ArgumentNullException>(() => new WorkoutHudStore(null!, Texts(LocalizationFixture.Es), h.Logger));
                Assert.Throws<ArgumentNullException>(() => new WorkoutHudStore(h.Engine, null!, h.Logger));
                Assert.Throws<ArgumentNullException>(() => new WorkoutHudStore(h.Engine, Texts(LocalizationFixture.Es), null!));
                Assert.Throws<ArgumentNullException>(() => WorkoutHudStore.Build(null!, LocalizationFixture.Es));
                Assert.Throws<ArgumentNullException>(() => WorkoutHudStore.Build(h.State, null!));
            }
        }

        private static WorkoutHudStore Hud(WorkoutHarness h, Localizer l10n)
        {
            return new WorkoutHudStore(h.Engine, Texts(l10n), h.Logger);
        }

        private static IReadOnlyObservable<Localizer> Texts(Localizer l10n)
        {
            return new ReadOnlyObservable<Localizer>(new ObservableValue<Localizer>(l10n, NullAppLogger.Instance));
        }
    }
}
