using System;
using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Ayuda emergente propia (AC-APP-03-06; 03-rutinas/02-tech-design §4.6, DT-15: el tooltip de UI Toolkit es solo del Editor).
    // Solo la llevan la flecha de volver (navBack), el ⋮ (menuShow) y el tacho de la serie (editorDeleteSeries), como el
    // legacy (<flutter-sdk>/packages/flutter/lib/src/material/tooltip.dart:392-394; UiTimings):
    // - Escritorio: aparece apenas el puntero se apoya (PointerEnter) y se va 0,1 s después de salir (PointerLeave).
    // - Teléfono: al mantener presionado 0,5 s se muestra, y se va 1,5 s después de soltar; ese toque NO activa el botón
    //   (ConsumesClick, que mira el botón).
    // El texto también queda como nombre accesible del elemento (AccessibleName; que lo lea un lector de pantalla es DT-05, It. 14).
    // Los tiempos van por el IScheduler (FakeScheduler en pruebas).
    public sealed class Tooltip
    {
        private readonly UiContext _ui;
        private readonly VisualElement _anchor;
        private readonly Func<string> _text;
        private IDisposable? _timer;
        private bool _pressed;
        private bool _longPressShown;

        private Tooltip(UiContext ui, VisualElement anchor, Func<string> text)
        {
            _ui = ui;
            _anchor = anchor;
            _text = text;
            if (ui.IsDesktop)
            {
                anchor.RegisterCallback<PointerEnterEvent>(_ => OnEnter());
                anchor.RegisterCallback<PointerLeaveEvent>(_ => OnLeave());
            }
            else
            {
                anchor.RegisterCallback<PointerDownEvent>(_ => OnPress(), TrickleDown.TrickleDown);
                anchor.RegisterCallback<PointerUpEvent>(_ => OnRelease(), TrickleDown.TrickleDown);
                anchor.RegisterCallback<PointerCancelEvent>(_ => OnRelease(), TrickleDown.TrickleDown);
            }

            anchor.RegisterCallback<DetachFromPanelEvent>(_ => Cancel());
        }

        // El texto que se muestra (y el nombre accesible).
        public string Text => _text();

        // El último toque largo mostró la ayuda: ese toque no activa el botón (teléfono).
        public bool ConsumesClick { get; private set; }

        public static Tooltip Attach(UiContext ui, VisualElement anchor, Func<string> text)
        {
            if (ui == null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            if (anchor == null)
            {
                throw new ArgumentNullException(nameof(anchor));
            }

            return new Tooltip(ui, anchor, text ?? throw new ArgumentNullException(nameof(text)));
        }

        // El nombre accesible (DT-05): el texto vigente de la ayuda.
        public string AccessibleName => _text();

        // Lo usa el botón al activarse: true = ignorar ESTE toque (fue un toque largo que mostró la ayuda).
        public bool TakeConsumedClick()
        {
            if (!ConsumesClick)
            {
                return false;
            }

            ConsumesClick = false;
            return true;
        }

        private void OnEnter()
        {
            Cancel();
            _ui.Popups.ShowTooltip(_anchor, _text());
        }

        private void OnLeave()
        {
            Cancel();
            _timer = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(UiTimings.TooltipExitMs), Hide);
        }

        private void OnPress()
        {
            Cancel();
            _pressed = true;
            _longPressShown = false;
            ConsumesClick = false;
            _timer = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(UiTimings.LongPressMs), () =>
            {
                _timer = null;
                if (!_pressed)
                {
                    return;
                }

                _longPressShown = true;
                ConsumesClick = true;
                _ui.Popups.ShowTooltip(_anchor, _text());
            });
        }

        private void OnRelease()
        {
            if (!_pressed)
            {
                return;
            }

            _pressed = false;
            if (!_longPressShown)
            {
                Cancel();
                return;
            }

            Cancel();
            _timer = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(UiTimings.TooltipShowAfterLongPressMs), Hide);
        }

        private void Hide()
        {
            _timer = null;
            _ui.Popups.HideTooltip(_anchor);
        }

        private void Cancel()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
