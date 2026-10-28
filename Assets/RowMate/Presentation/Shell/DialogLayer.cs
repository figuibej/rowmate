using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Capa de los diálogos (03-rutinas/02-tech-design §4.1, §4.5): #dialog-layer .rm-layer, sin eventos propios; cada diálogo
    // agrega su .rm-dialog-host (velo + diálogo). Arriba de las páginas y abajo de los avisos, los menús y el simulador.
    // Mientras hay uno abierto, lo de atrás no responde (el velo tapa) y Tab recorre solo el diálogo (FocusTrap).
    // "Atrás" y Escape cancelan el de arriba que no está saliendo (CancelTop, BackNavigation).
    public sealed class DialogLayer : VisualElement, IDisposable
    {
        private readonly UiContext _ui;
        private readonly ShellLayout _layout;
        private readonly List<ModalDialog> _dialogs = new List<ModalDialog>();
        private double _keyboardDp;
        private bool _disposed;

        public DialogLayer(UiContext ui, ShellLayout layout)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            name = "dialog-layer";
            AddToClassList("rm-layer");
            pickingMode = PickingMode.Ignore;
            _layout.Changed += RelayoutAll;
        }

        // El de arriba que no está saliendo.
        public ModalDialog? Top
        {
            get
            {
                for (var i = _dialogs.Count - 1; i >= 0; i--)
                {
                    if (_dialogs[i].IsOpen)
                    {
                        return _dialogs[i];
                    }
                }

                return null;
            }
        }

        public bool HasOpen => Top != null;

        // Algún diálogo entrando o saliendo (regla de toma).
        public bool IsTransitioning
        {
            get
            {
                for (var i = 0; i < _dialogs.Count; i++)
                {
                    if (_dialogs[i].IsTransitioning)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public int Count => _dialogs.Count;

        internal double WindowWidthDp => _layout.WindowSize.x;

        internal double WindowHeightDp => _layout.WindowSize.y;

        internal double SafeVerticalDp => _layout.Insets.Top + _layout.Insets.Bottom;

        internal double KeyboardDp => _keyboardDp;

        public void Open(ModalDialog dialog)
        {
            if (dialog == null)
            {
                throw new ArgumentNullException(nameof(dialog));
            }

            if (_disposed || _dialogs.Contains(dialog))
            {
                return;
            }

            var focused = panel?.focusController?.focusedElement;
            _dialogs.Add(dialog);
            Add(dialog.Host);
            dialog.Open(this, focused);
        }

        // "Atrás" o Escape (BackNavigation, paso 2): cancela el de arriba. false = no había ninguno abierto.
        public bool CancelTop()
        {
            var top = Top;
            if (top == null)
            {
                return false;
            }

            top.Cancel();
            return true;
        }

        // KeyboardAvoider: el diálogo se recentra sobre el teclado y su alto máximo baja (DN3-18).
        public void SetKeyboardInset(double dp)
        {
            if (Math.Abs(dp - _keyboardDp) < 0.01)
            {
                return;
            }

            _keyboardDp = dp < 0 ? 0 : dp;
            RelayoutAll();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _layout.Changed -= RelayoutAll;
            for (var i = _dialogs.Count - 1; i >= 0; i--)
            {
                _dialogs[i].Host.RemoveFromHierarchy();
            }

            _dialogs.Clear();
        }

        internal void OnDialogClosing(ModalDialog dialog)
        {
        }

        internal void OnDialogClosed(ModalDialog dialog)
        {
            _dialogs.Remove(dialog);
            dialog.Host.RemoveFromHierarchy();
        }

        private void RelayoutAll()
        {
            for (var i = 0; i < _dialogs.Count; i++)
            {
                _dialogs[i].Relayout(WindowWidthDp, WindowHeightDp, SafeVerticalDp, _keyboardDp);
            }
        }
    }
}
