using System;
using System.Diagnostics;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Workout;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // QA-V4b-06 (04b-hud-exr/REPORT §7.1, 02-tech-design V1-06; 05-series-parciales/00-deuda-4b §2): el primer inmersivo de cada
    // arranque tardaba 31-69 ms de CPU en el cuadro del toque y los siguientes, unos 8. La diferencia es lo que se paga UNA vez
    // por proceso: compilar el código del HUD (el JIT de Mono), resolver por primera vez sus estilos y medir su texto. HudWarmup
    // paga esa primera vez en el arranque: en el cuadro del arranque (que todavía no se vio) arma un HUD de rutina y uno libre como
    // los del inmersivo, con el estado de reposo del store del HUD, detrás de la app (primer hijo de la raíz, transparentes y sin
    // eventos); los deja un cuadro en el panel (estilo, layout, texto y dibujo), les aplica la geometría (HudZones) y en el cuadro
    // siguiente los desarma. No crea sesión ni toca el motor, las barras del sistema, la llave, el obstáculo de los controles ni
    // los 60 fps: lo que ve el usuario no cambia (AC-WKT-13-01: el inmersivo se sigue armando entero en el cuadro del toque, ahora
    // sin la primera vez). Si algo falla, lo anota y se desarma: nunca frena la app. Lo usa AppRoot (no las pruebas de UI ni el
    // arnés, que tienen su propio orden de atlas: HudWarmupTests lo corre aparte). Hilo principal.
    public sealed class HudWarmup : IDisposable
    {
        public const string HolderName = "hud-warmup";
        public const string Tag = "UI";

        // Si el panel todavía no dio layout (una ventana sin tamaño), a lo sumo estos cuadros antes de desarmar igual.
        public const int MaxTicks = 5;

        private readonly UiContext _ui;
        private readonly VisualElement _host;
        private readonly Stopwatch _work = new Stopwatch();
        private VisualElement? _holder;
        private WorkoutHudStore? _store;
        private HudView? _routine;
        private HudView? _free;
        private int _ticks;
        private bool _done;

        public HudWarmup(UiContext ui, VisualElement host)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        // Armado y esperando el cuadro del panel.
        public bool IsRunning => _holder != null;

        public bool IsDone => _done;

        // Lo que tardó el armado y el desarme, en ms (sin el cuadro del panel).
        public double WorkMs => _work.Elapsed.TotalMilliseconds;

        public void Start()
        {
            if (_done || _holder != null)
            {
                return;
            }

            _work.Start();
            try
            {
                _store = _ui.App.CreateHud();
                var state = _store.State.Value;
                var holder = new VisualElement { name = HolderName, pickingMode = PickingMode.Ignore };
                holder.style.position = Position.Absolute;
                holder.style.left = 0;
                holder.style.top = 0;
                holder.style.right = 0;
                holder.style.bottom = 0;
                holder.style.opacity = 0;
                holder.style.overflow = Overflow.Hidden;
                _holder = holder;
                // It. 5 (QA-G5T-05; DD5-12): los paneles de series abiertos y con contenido, con un estado propio en memoria: nunca las
                // preferencias de la app (no escribe prefs.json ni se suscribe a su State).
                _routine = new HudView(_ui, true, _store.WallClock, Noop, Noop, Noop, new LocalSeriesPanelsExpansion(true, true));
                _free = new HudView(_ui, false, _store.WallClock, Noop, Noop, Noop);
                // El reposo y después "en marcha" con valores de ejemplo (que no se ven), sin animar: así el primer Apply de verdad
                // ya no paga la primera vez de la fila de pasos, las píldoras, la pastilla, la cuenta, el chip y las tarjetas con
                // objetivo. El motor no se mueve: los estados son del HUD.
                _routine.Apply(state, null);
                _free.Apply(state, null);
                _routine.Apply(Running(state, true, _ui.Texts), 1);
                _routine.Apply(Running(state, true, _ui.Texts) with { Series = WarmSeries(_ui.Texts, true) }, 2);
                _free.Apply(Running(state, false, _ui.Texts), null);
                holder.Add(_routine);
                holder.Add(_free);
                // Detrás de todo: la app lo tapa entero aunque el panel lo dibuje.
                _host.Insert(0, holder);
            }
            catch (Exception error)
            {
                _ui.Logger.Log(LogLevel.Warning, Tag, "no se pudo precalentar el HUD (QA-V4b-06): " + error.GetType().Name, error);
                Finish();
            }
            finally
            {
                _work.Stop();
            }
        }

        // Una vez por cuadro (AppRoot.Update, antes del panel): cuando el panel ya dio layout, la geometría y el desarme. Devuelve
        // true cuando terminó.
        public bool Tick()
        {
            if (_done)
            {
                return true;
            }

            if (_holder == null)
            {
                return false;
            }

            _ticks++;
            var routine = _routine;
            var laidOut = routine != null && routine.layout.width > 0 && routine.layout.height > 0;
            if (!laidOut && _ticks < MaxTicks)
            {
                return false;
            }

            _work.Start();
            try
            {
                if (laidOut)
                {
                    new HudZones(routine!, NoInsets, _ui.ReserveWrench).Update();
                    new HudZones(_free!, NoInsets, _ui.ReserveWrench).Update();
                }
            }
            catch (Exception error)
            {
                _ui.Logger.Log(LogLevel.Warning, Tag, "no se pudo precalentar el HUD (QA-V4b-06): " + error.GetType().Name, error);
            }
            finally
            {
                Finish();
                _work.Stop();
            }

            _ui.Logger.Log(LogLevel.Info, Tag, "HUD precalentado (QA-V4b-06): " + WorkMs.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                + " ms de armado" + (laidOut ? "" : ", sin layout"));
            return true;
        }

        public void Dispose()
        {
            Finish();
        }

        // Desarma todo (también si se corta a mitad): primero las vistas (sus animaciones y texturas), después el árbol y el store.
        private void Finish()
        {
            if (_done)
            {
                return;
            }

            _done = true;
            try
            {
                _routine?.Dispose();
                _free?.Dispose();
                _holder?.RemoveFromHierarchy();
                _store?.Dispose();
            }
            catch (Exception error)
            {
                _ui.Logger.Log(LogLevel.Warning, Tag, "al desarmar el HUD precalentado: " + error.GetType().Name, error);
            }

            _routine = null;
            _free = null;
            _holder = null;
            _store = null;
        }

        // Un estado "en marcha" de ejemplo (Instant: sin rodillos ni empujones), con los mismos tipos de texto que el de verdad.
        internal static WorkoutHudState Running(WorkoutHudState idle, bool routine, Localizer l10n)
        {
            var state = idle with
            {
                Phase = WorkoutPhase.Active,
                IsRoutine = routine,
                Spm = new HudMetric("24.0", 24, true, true, TargetArrow.Up, "obj. 26"),
                Split = new HudMetric("02:05.0", 125, true, false, TargetArrow.None, "obj. 02:05"),
                Watts = new HudMetric("150", 150, true, true, TargetArrow.Up, "obj. 160"),
                Kcal = "12",
                KcalValue = 12,
                Bpm = "128",
                BpmValue = 128,
                Clock = "01:23",
                ClockValue = 83,
                Distance = "345",
                DistanceValue = 345,
                ControlsEnabled = true,
                ShowResume = false,
                Instant = true,
            };
            if (!routine)
            {
                var spec = PhaseChip.For(WorkoutPhase.Active);
                return spec.HasValue
                    ? state with { Chip = new HudPhaseChip(l10n.Get(spec.Value.Key), spec.Value.Base, spec.Value.Text, spec.Value.LiveDot) }
                    : state;
            }

            var target = new HudTargetLabel("> 160W · 22 spm", "> 160W · 22 spm", false, "")
            {
                Fragments = new[] { new HudTargetFragment("> 160W", false), new HudTargetFragment("22 spm", false) },
            };
            state = state with { Series = WarmSeries(l10n, false) };
            return state with
            {
                Step = new HudStepRow(StepType.Work, l10n.Get(StepTypeInfo.NameKey(StepType.Work)), target, "0:42", 42, false, StepType.Rest,
                    l10n.Get(StepTypeInfo.NameKey(StepType.Rest))),
                Pills = new HudPillRow(new[] { StepType.Warmup, StepType.Work, StepType.Rest, StepType.Cooldown },
                    new[] { StepPillLook.Solid, StepPillLook.Current, StepPillLook.Pending, StepPillLook.Pending }, 1, 18, 60, true, false),
            };
        }

        // It. 5: un contenido de ejemplo de los paneles (una vuelta y la línea Rep; después la repetición anterior), armado con el
        // tracker y el store como el de verdad (sin el motor).
        internal static HudSeries? WarmSeries(Localizer l10n, bool secondRep)
        {
            var step = new IntervalStep(1, 0, StepType.Work) { DistanceMeters = 600, GroupId = "warmup", GroupRepeatCount = 3 };
            var first = new StepPosition("warmup", 1, 3);
            var second = new StepPosition("warmup", 2, 3);
            var steps = new[] { step, step };
            var positions = new[] { first, second };
            var tracker = new SeriesTracker();
            tracker.Begin(0, 0);
            tracker.Update(0, step, first, 300, 75);
            tracker.Update(0, step, first, 560, 140);
            if (secondRep)
            {
                tracker.Update(1, step, second, 620, 150);
                tracker.Update(1, step, second, 640, 155);
            }

            return WorkoutHudStore.ComposeSeries(tracker.State, steps, positions, l10n, 1);
        }

        private static SafeInsets NoInsets()
        {
            return default;
        }

        private static void Noop()
        {
        }
    }
}
