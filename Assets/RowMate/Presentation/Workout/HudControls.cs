using System;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // Los controles del HUD (AC-WKT-20-01 a -06; legacy immersive_workout_screen.dart:893-953; 02-tech-design §6.8 de la It. 4a;
    // 04-design-notes §3.5, §4.6):
    //   #hud-controls .rm-hud-controls        fila centrada, 24 de relleno a los lados, 16 entre botones (base)
    //   ├── #hud-finish .rm-button --hud-danger   workoutFinish 16/600 state.dangerText; contorno 1,5; vidrio; mín. 128 × 56
    //   ├── #hud-pause  .rm-button --hud-outline  pause 22 + workoutPause 16/600 blanco; contorno 1,5 border.strong; mín. 148 × 56
    //   └── #hud-resume .rm-button --hud-success  play_arrow 22 + workoutResume 16/700 text.onAccent; relleno state.success
    // Pausa y Reanudar se alternan con display en el mismo cuadro (miden lo mismo: nada se mueve; si el que se oculta tenía el
    // foco, lo toma el otro). Desde que se confirma Terminar (fase Finished) los dos pasan al 0,38 en el acto
    // (ControlsEnabled = false) y, si el cierre tarda más de 300 ms (FinishSpinnerDelayMs), Terminar muestra el spinner de 16 a
    // la izquierda de su texto, sin cambiar de ancho (DN4a-20). Es la zona: HudZones la ancla abajo al centro y la escala por s.
    public sealed class HudControls : VisualElement
    {
        public const float TextSizeDp = 16;
        public const float IconSizeDp = 22;

        private readonly UiContext _ui;
        private IDisposable? _spinnerTimer;
        private bool _enabled = true;
        private bool _resume;
        private bool _saving;

        public HudControls(UiContext ui, Action finish, Action pause, Action resume)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            name = "hud-controls";
            AddToClassList("rm-hud-controls");
            pickingMode = PickingMode.Ignore;

            Finish = Button("hud-finish", "hud-danger", null, finish, "rm-type-button");
            Add(Finish);
            Pause = Button("hud-pause", "hud-outline", Icons.Pause, pause, "rm-type-button");
            Pause.style.marginLeft = (float)HudGeometry.ControlsGap;
            Add(Pause);
            Resume = Button("hud-resume", "hud-success", Icons.PlayArrow, resume, "rm-type-header-s");
            Resume.style.marginLeft = (float)HudGeometry.ControlsGap;
            Resume.style.display = DisplayStyle.None;
            Add(Resume);
        }

        public RmButton Finish { get; }

        public RmButton Pause { get; }

        public RmButton Resume { get; }

        public bool ShowsResume => _resume;

        public bool IsEnabled => _enabled;

        // El spinner de Terminar a la vista (el cierre tardó más de 300 ms).
        public bool ShowsSpinner => Finish.IsBusy;

        public void RenderTexts(Localizer texts)
        {
            Finish.SetLabel(texts.Get(L10nKeys.WorkoutFinish));
            Pause.SetLabel(texts.Get(L10nKeys.WorkoutPause));
            Resume.SetLabel(texts.Get(L10nKeys.WorkoutResume));
        }

        // enabled: en marcha o en pausa; resume: en pausa; saving: terminada con el cierre en vuelo.
        public void Apply(bool enabled, bool resume, bool saving)
        {
            if (resume != _resume)
            {
                _resume = resume;
                var focus = _ui.Pages.panel?.focusController?.focusedElement;
                var hadFocus = focus == Pause || focus == Resume;
                Pause.style.display = resume ? DisplayStyle.None : DisplayStyle.Flex;
                Resume.style.display = resume ? DisplayStyle.Flex : DisplayStyle.None;
                if (hadFocus)
                {
                    (resume ? Resume : Pause).Focus();
                }
            }

            if (enabled != _enabled)
            {
                _enabled = enabled;
                Finish.SetEnabled(enabled);
                Pause.SetEnabled(enabled);
                Resume.SetEnabled(enabled);
            }

            if (saving != _saving)
            {
                _saving = saving;
                _spinnerTimer?.Dispose();
                _spinnerTimer = null;
                if (saving)
                {
                    _spinnerTimer = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(MotionSpecs.FinishSpinnerDelayMs), () =>
                    {
                        _spinnerTimer = null;
                        if (_saving)
                        {
                            Finish.SetBusy(true, _ui.Motion);
                        }
                    });
                }
                else
                {
                    Finish.SetBusy(false, _ui.Motion);
                }
            }
        }

        public void Dispose()
        {
            _spinnerTimer?.Dispose();
            _spinnerTimer = null;
        }

        private static RmButton Button(string elementName, string variant, string? icon, Action onClick, string typeClass)
        {
            var button = new RmButton(elementName, variant, icon, "", onClick, IconSizeDp, typeClass);
            button.Label.style.fontSize = TextSizeDp;
            return button;
        }
    }
}
