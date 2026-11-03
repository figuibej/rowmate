using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Workout;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Simulator
{
    // WSM · El entrenamiento con el simulador sin ruido (150 W y 24 spm; la demo de la spec §7.4; 04a-motor-hud-funcional/
    // 02-tech-design §8.2): el pulsómetro simulado, el semáforo de Prueba 4a, la caída del remo y la cinta sobre el inmersivo.
    public sealed class WorkoutSimulatorTests : SimulatorUiTestBase
    {
        private double _start = double.NaN;

        private HudView Hud => H.Immersive?.View ?? throw new InvalidOperationException("no hay inmersivo");

        private UiTestHost CreateWorkoutSim(RoutineSeed seed)
        {
            return Create(new UiHostOptions
            {
                Profile = DeviceProfile.TV,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                Routines = HostRoutines.Seeded(seed),
                StartUtc = UiHostOptions.WorkoutStartUtc,
            });
        }

        private IEnumerator StartFree()
        {
            yield return Tap("tab-workout");
            yield return Frames();
            H.Tap("workout-free");
            _start = H.Scheduler.MonotonicSeconds;
            yield return Settle();
        }

        private IEnumerator StartRoutine(string name)
        {
            yield return Tap("tab-workout");
            yield return Frames();
            H.TapRoutineNamed(name);
            yield return Settle();
            yield return Frames();
            H.Tap("dialog-confirm");
            _start = H.Scheduler.MonotonicSeconds;
            yield return Settle();
        }

        private IEnumerator AdvanceTo(double seconds)
        {
            var target = _start + seconds;
            var now = H.Scheduler.MonotonicSeconds;
            if (target > now)
            {
                H.Advance(target - now);
            }

            yield return Frames();
        }

        private IEnumerator WithPanel(string button)
        {
            yield return Tap("sim-open");
            yield return ScrollAndTap(button);
            yield return Tap("sim-close");
        }

        [UnityTest, Category("AC-WKT-05-06")]
        public IEnumerator SimulatedHeartRateOnTheHud()
        {
            CreateWorkoutSim(RoutineSeeds.Lista);
            yield return Tap("hrm-search");
            yield return Tap(HrmRow);
            yield return Settle();
            yield return StartFree();
            yield return AdvanceTo(5.05);
            Assert.That(H.IsShown("hud-bpm"), Is.True, "la tarjeta BPM con el pulso del sensor");
            Assert.That(int.Parse(H.TextOf("hud-bpm-value")!), Is.GreaterThan(0));
            yield return WithPanel("sim-drop-hrm");
            yield return Advance(1);
            Assert.That(H.IsShown("hud-bpm"), Is.False, "mientras dura la caída, la tarjeta desaparece");
            yield return Advance(10);
            yield return Settle();
            Assert.That(H.IsShown("hud-bpm"), Is.True, "cuando el sensor vuelve, la tarjeta vuelve");
        }

        [UnityTest, Category("AC-WKT-10-09")]
        public IEnumerator DemoTrafficLight()
        {
            CreateWorkoutSim(RoutineSeeds.F4A);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(10.05);
            Assert.That(Hud.SpmHasTarget || Hud.WattsHasTarget, Is.False, "en el calentamiento no hay semáforo");
            yield return AdvanceTo(50.05);
            Assert.That(Hud.Watts.IsOff, Is.True, "repetición 1, a 150 W: Watts fuera");
            Assert.That(H.TextOf("hud-target-watts"), Is.EqualTo("obj. 160"));
            Assert.That(Hud.Spm.HasTarget && !Hud.Spm.IsOff, Is.True, "SPM cumple (24.0 contra 22)");
            yield return WithPanel("sim-watts-plus");
            yield return Advance(1.1);
            Assert.That(Hud.Watts.IsOff, Is.False, "con + de Watts (160 W), cumple desde la lectura siguiente");
            yield return AdvanceTo(100.05);
            Assert.That(Hud.SpmHasTarget || Hud.WattsHasTarget, Is.False, "en el descanso, tampoco");
            yield return AdvanceTo(125.05);
            Assert.That(H.TextOf("hud-target-spm"), Is.EqualTo("obj. 24"), "repetición 2: objetivo 24");
            Assert.That(Hud.Spm.IsOff, Is.False, "a 24 spm cumple");
            yield return WithPanel("sim-spm-minus");
            yield return Advance(1.1);
            Assert.That(Hud.Spm.IsOff, Is.True, "con − de Ritmo (23), fuera");
            yield return AdvanceTo(215.05);
            Assert.That(Hud.SpmHasTarget || Hud.WattsHasTarget, Is.False, "en el enfriamiento, tampoco");
        }

        [UnityTest, Category("AC-WKT-03-10")]
        public IEnumerator RowerDropsDuringTheSession()
        {
            CreateWorkoutSim(RoutineSeeds.F4A);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(40.05);
            var spm = H.TextOf("hud-spm-value");
            var shown = H.Shell.Toasts.ShowCount;
            yield return WithPanel("sim-drop-rower");
            yield return AdvanceTo(42.05);
            Assert.That(H.Immersive, Is.Not.Null, "la sesión sigue");
            Assert.That(H.TextOf("hud-spm-value"), Is.EqualTo(spm), "el HUD queda con la última lectura");
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:42"), "el reloj avanza");
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo("0:48"), "y el paso por tiempo también");
            Assert.That(H.Shell.Toasts.ShowCount, Is.EqualTo(shown), "sin aviso (el de caída es de la It. 10a)");
            yield return AdvanceTo(50.05);
            Assert.That(H.App.Device.State.Value.IsConnected, Is.True, "el remo vuelve");
            Assert.That(Phase(), Is.EqualTo(WorkoutPhase.Active));
        }

        private WorkoutPhase Phase()
        {
            return H.App.Workout.State.Value.Phase;
        }

        // It. 5 (05-series-parciales/02-tech-design §6.3 WSM; AC-BLE-15-09; DEMO-08, -09, VID-05): Series 600 con el simulador sin ruido y
        // parciales abierto; "Simular desconexión" en el trabajo de la repetición 2: durante el corte la vuelta en curso no suma metros y
        // su tiempo sí; después sigue desde los mismos metros; ninguna vuelta inventada; nada negativo.
        [UnityTest, Category("AC-BLE-15-09")]
        public IEnumerator SimulateDisconnectInTheCurrentLap()
        {
            var prefs = new RowMate.Core.Common.MemoryPreferences();
            prefs.SetBool(RowMate.App.Workout.SeriesPanelPrefs.LapsKey, true);
            Create(new UiHostOptions
            {
                Profile = DeviceProfile.W1280,
                IsDesktop = true,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                Routines = HostRoutines.Seeded(RoutineSeeds.F5),
                StartUtc = UiHostOptions.WorkoutStartUtc,
                Preferences = prefs,
            });
            yield return StartRoutine("Series 600");
            yield return AdvanceTo(236.05);
            var laps = Hud.Series!.Laps.Series!.LapCount;
            // El panel abierto y el botón a la vista antes (los asentamientos de los toques mueven el reloj); el corte, sin
            // asentar, a los 238,6: vuelve a los 241,6 (SimulatedRowerService.DefaultReconnectAfter, 3 s) y la primera lectura
            // llega un segundo después. El segundo 239 del motor todavía toma lo que llegó antes del corte.
            yield return Tap("sim-open");
            yield return Frames();
            if (H.WhyNotTappable("sim-drop-rower") != null)
            {
                H.ScrollTo("sim-drop-rower");
                yield return Frames();
            }

            yield return AdvanceTo(238.6);
            H.Tap("sim-drop-rower");
            yield return Frames();
            Assert.That(H.App.Device.State.Value.IsConnected, Is.False, "el corte");
            yield return AdvanceTo(239.05);
            var meters = Meters();
            var time = H.TextOf("hud-laps-current");
            foreach (var second in new[] { 240.05, 241.05, 242.05 })
            {
                yield return AdvanceTo(second);
                if (second < 241.6)
                {
                    Assert.That(H.App.Device.State.Value.IsConnected, Is.False, "durante el corte (" + second + ")");
                }

                Assert.That(Meters(), Is.EqualTo(meters), "los metros de la vuelta en curso quietos (" + second + ")");
            }

            Assert.That(H.TextOf("hud-laps-current"), Is.Not.EqualTo(time), "el tiempo de la vuelta avanza");
            // Vuelve a los 241,6; las lecturas de los 242,6 y 243,6 (3,77 m/s sin ruido) suman ≈ 7,5 m en los segundos 243 y 244
            // del motor. Un salto con lo del corte (3 s más) pasaría de 18.
            yield return AdvanceTo(244.05);
            Assert.That(H.App.Device.State.Value.IsConnected, Is.True, "el remo vuelve");
            Assert.That(Meters(), Is.GreaterThanOrEqualTo(meters), "sigue desde los mismos metros, sin negativos");
            Assert.That(Meters() - meters, Is.LessThan(12), "sin un salto: solo las dos lecturas de después del corte");
            yield return Tap("sim-close");
            yield return AdvanceTo(252.05);
            Assert.That(Meters(), Is.GreaterThan(meters), "sigue remando");
            Assert.That(Hud.Series!.Laps.Series!.LapCount, Is.EqualTo(laps), "ninguna vuelta inventada");
        }

        private int Meters()
        {
            var text = H.TextOf("hud-laps-current-meters") ?? "";
            var digits = text.Replace(" m", "").Replace(".", "");
            return int.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
        }

        [UnityTest, Category("AC-BLE-10-05")]
        public IEnumerator NoRowerBannerFollowsTheRowerLive()
        {
            CreateWorkoutSim(RoutineSeeds.Lista);
            yield return Tap("tab-workout");
            Assert.That(H.IsShown("workout-banner"), Is.False);
            yield return WithPanel("sim-drop-rower");
            yield return Advance(0.5);
            yield return Settle();
            Assert.That(H.IsShown("workout-banner"), Is.True, "Simular desconexión: el aviso");
            Assert.That(H.Find("workout-free")!.enabledInHierarchy, Is.False, "y todo deshabilitado");
            yield return Advance(3.5);
            yield return Settle();
            Assert.That(H.IsShown("workout-banner"), Is.False, "al volver, se va");
            Assert.That(H.Find("workout-free")!.enabledInHierarchy, Is.True, "y todo se habilita");
            yield return Tap("tab-device");
            yield return ScrollAndTap("device-disconnect");
            yield return Tap("tab-workout");
            yield return Advance(5);
            yield return Settle();
            Assert.That(H.IsShown("workout-banner"), Is.True, "Desconectar lo deja así");
        }

        [UnityTest, Category("AC-BLE-16-01")]
        public IEnumerator RibbonOverTheImmersive()
        {
            CreateWorkoutSim(RoutineSeeds.F4A);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(20.05);
            var root = H.Find("app-root")!;
            Assert.That(root.hierarchy[root.hierarchy.childCount - 1].name, Is.EqualTo("simulator-layer"), "la capa por encima del inmersivo");
            var ribbon = H.BoundsOf("sim-ribbon");
            Assert.That(ribbon.Overlaps(H.BoundsOf("hud-topbar")), Is.True, "tapa el comienzo de la barra superior");
            Assert.That(ribbon.Overlaps(H.BoundsOf("hud-countdown")), Is.False, "sin tapar la cuenta regresiva");
            Assert.That(H.Find("sim-ribbon")!.pickingMode, Is.EqualTo(PickingMode.Ignore), "sin recibir toques");
            yield return Tap("hud-finish");
            Assert.That(H.IsShown("sim-ribbon"), Is.True, "y sobre sus diálogos");
            Assert.That(root.hierarchy.IndexOf(H.Find("simulator-layer")!), Is.GreaterThan(root.hierarchy.IndexOf(H.Find("dialog-layer")!)));
            Assert.That(root.hierarchy.IndexOf(H.Find("simulator-layer")!), Is.GreaterThan(root.hierarchy.IndexOf(H.Find("toast-layer") ?? H.Find("dialog-layer")!)));
        }
    }
}
