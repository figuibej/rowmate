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
            // It. 4b (QA-G4bT-09 (b); AC-WKT-21-11, AC-APP-05-07): sin nada enfocado, el primer Tab llega a la raíz del panel y no
            // al árbol del diálogo, donde escucha FocusTrap. Con un diálogo arriba y el foco fuera de su caja, el Tab va al primer
            // control del diálogo (FocusTrap.Next) y se ignora el evento. Vale para todos los diálogos.
            RegisterCallback<AttachToPanelEvent>(_ => HookPanelRoot());
            RegisterCallback<DetachFromPanelEvent>(_ => UnhookPanelRoot());
        }

        private VisualElement? _panelRoot;

        private void HookPanelRoot()
        {
            var root = panel?.visualTree;
            if (root == null || ReferenceEquals(root, _panelRoot) || _disposed)
            {
                return;
            }

            UnhookPanelRoot();
            _panelRoot = root;
            _panelRoot.RegisterCallback<NavigationMoveEvent>(OnPanelNavigation, TrickleDown.TrickleDown);
        }

        private void UnhookPanelRoot()
        {
            _panelRoot?.UnregisterCallback<NavigationMoveEvent>(OnPanelNavigation, TrickleDown.TrickleDown);
            _panelRoot = null;
        }

        private void OnPanelNavigation(NavigationMoveEvent evt)
        {
            if (evt.direction != NavigationMoveEvent.Direction.Next && evt.direction != NavigationMoveEvent.Direction.Previous)
            {
                return;
            }

            var top = Top;
            if (top == null)
            {
                return;
            }

            var target = evt.target as VisualElement;
            var focused = panel?.focusController?.focusedElement as VisualElement;
            var inside = focused != null && (ReferenceEquals(focused, top.Box) || top.Box.Contains(focused))
                         || target != null && !ReferenceEquals(target, _panelRoot) && (ReferenceEquals(target, top.Box) || top.Box.Contains(target));
            if (inside)
            {
                // Lo resuelve FocusTrap, en el árbol del diálogo.
                return;
            }

            var next = FocusTrap.Next(top.Box, null, evt.direction == NavigationMoveEvent.Direction.Next);
            next?.Focus();
            _panelRoot?.panel?.focusController?.IgnoreEvent(evt);
            evt.StopPropagation();
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
            Open(dialog, false);
        }

        // It. 4b (AC-WKT-21-09): instantly = sin transición, con todo en su estado final (el de rutina completada al volver del
        // segundo plano).
        public void Open(ModalDialog dialog, bool instantly)
        {
            if (dialog == null)
            {
                throw new ArgumentNullException(nameof(dialog));
            }

            if (_disposed || _dialogs.Contains(dialog))
            {
                return;
            }

            HookPanelRoot();
            var focused = panel?.focusController?.focusedElement;
            _dialogs.Add(dialog);
            Add(dialog.Host);
            dialog.Open(this, focused, instantly);
        }

        // "Atrás" o Escape (BackNavigation, paso 2): el camino del usuario del de arriba (ModalDialog.Dismiss): lo cancela si es
        // descartable y, si no lo es (el de rutina completada), se consume sin cerrarlo. false = no había ninguno abierto.
        public bool CancelTop()
        {
            var top = Top;
            if (top == null)
            {
                return false;
            }

            top.Dismiss();
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
            UnhookPanelRoot();
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
