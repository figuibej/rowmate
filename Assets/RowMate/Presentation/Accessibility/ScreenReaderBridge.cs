using System;
using System.Collections.Generic;
using RowMate.Core.Accessibility;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Presentation.Shell;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Accessibility
{
    // El puente con el lector de pantalla del sistema (It. 14, AC-APP-03-12 a -15; DT14-17 a DT14-22, QA-G14-06, QA-G14-21; ADR 026):
    // arma un AccessibilityHierarchy de Unity 6.3 recorriendo el árbol visual con A11yRules (sin tocar los controles ni agregar
    // VisualElement). Con el lector APAGADO, Tick lee un bool y vuelve: sin árbol, sin suscripciones, 0 B. Encendido:
    // - Estructura: como mucho cada 250 ms se recolecta, se ordena (A11yOrder) y se RECONCILIA por clave estable (A11yReconciler:
    //   InsertNode / MoveNode / RemoveNode); un elemento que sigue conserva su AccessibilityNode (y el foco del lector). Clear() solo al
    //   cambiar de raíz (shell ↔ diálogo). Si el enfocado salió, SendLayoutChanged al vecino más cercano en el orden.
    // - Geometría: los marcos salen de frameGetter (worldBound × escala → píxeles de pantalla) y RefreshNodeFrames los avisa, sin
    //   tocar el árbol.
    // - Métricas: su label ("{nombre}: {valor}") se actualiza una vez por segundo; nunca se anuncian solas.
    // - Con un diálogo abierto, el árbol tiene SOLO su subárbol (el lector no sale); al cerrarlo, el foco vuelve al que lo abrió.
    // - invoked = la misma acción del clic (NavigationSubmitEvent al elemento).
    // - Anuncios (IA11yAnnouncer + A11yAnnouncePolicy): el título nuevo al cambiar de pantalla, cada aviso una vez (ToastLayer.Shown),
    //   el cambio de paso ("Paso 2 de 5: Trabajo") y la rutina completada.
    // ForceEnabled (pruebas) usa AssistiveSupport.screenReaderStatusOverride; IsActiveHierarchySupported dice si Unity aceptó la jerarquía
    // (en el Editor puede no hacer nada: R14-01; el modelo propio, Items, es el que comparan las pruebas y CAP-27).
    public sealed class ScreenReaderBridge : IDisposable
    {
        public const double StructureSeconds = 0.25;
        public const double LabelSeconds = 1.0;

        private readonly AppShellView _shell;
        private readonly IA11yAnnouncer _announcer;
        private readonly A11yAnnouncePolicy _policy = new A11yAnnouncePolicy();
        private readonly List<A11yItem> _items = new List<A11yItem>();
        private readonly List<A11yItem> _scratch = new List<A11yItem>();
        private readonly List<object> _previousKeys = new List<object>();
        private readonly List<object> _nextKeys = new List<object>();
        private readonly List<A11yEdit> _edits = new List<A11yEdit>();
        private readonly Dictionary<VisualElement, AccessibilityNode> _nodes = new Dictionary<VisualElement, AccessibilityNode>();
        private readonly Dictionary<AccessibilityNode, VisualElement> _elements = new Dictionary<AccessibilityNode, VisualElement>();
        private readonly Dictionary<VisualElement, A11yRules.Kind> _kinds = new Dictionary<VisualElement, A11yRules.Kind>();
        private AccessibilityHierarchy? _hierarchy;
        private VisualElement? _root;
        private VisualElement? _focused;
        private VisualElement? _opener;
        private IDisposable? _workout;
        private bool _active;
        private bool _disposed;
        private double _nextStructure;
        private double _nextLabels;
        private long _stepSession = -1;
        private int _stepIndex = -1;

        public ScreenReaderBridge(AppShellView shell, IA11yAnnouncer announcer)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _announcer = announcer ?? throw new ArgumentNullException(nameof(announcer));
        }

        // Lo que el lector alcanza, en orden de recorrido (el mismo modelo que la jerarquía de Unity).
        public IReadOnlyList<A11yItem> Items => _items;

        public bool IsActive => _active;

        public AccessibilityHierarchy? Hierarchy => _hierarchy;

        // ¿Unity aceptó la jerarquía como activa? (false en el Editor si la plataforma no la soporta).
        public bool IsActiveHierarchySupported { get; private set; }

        // Contadores para las pruebas (QA-G14-06, QA-G14-21).
        public int ClearCount { get; private set; }

        public int ReconcileCount { get; private set; }

        public int LayoutChangedCount { get; private set; }

        public int FrameRefreshCount { get; private set; }

        public int LabelRefreshCount { get; private set; }

        public VisualElement? FocusedElement => _focused;

        // El nodo de un elemento (null si no está en el árbol).
        public AccessibilityNode? NodeOf(VisualElement element)
        {
            return element != null && _nodes.TryGetValue(element, out var node) ? node : null;
        }

        public string Dump()
        {
            return A11yTreeDump.Dump(_items);
        }

        // Lo que haría el lector al enfocar un elemento (pruebas).
        internal void SimulateFocus(VisualElement element)
        {
            _focused = element;
        }

        // La acción del lector sobre un elemento (pruebas): lo mismo que invoked.
        public bool Invoke(VisualElement element)
        {
            if (element == null || element.panel == null)
            {
                return false;
            }

            _focused = element;
            if (IsDialogOpener(element))
            {
                _opener = element;
            }

            // El botón que hace el clic: el elemento o el primer Button de adentro (la pestaña es una caja con su botón).
            var target = element as Button ?? element.Q<Button>() ?? element;
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = target;
                target.SendEvent(submit);
            }

            return true;
        }

        // Una vez por cuadro. Con el lector apagado: una lectura de un bool estático.
        public void Tick(double now)
        {
            if (_disposed)
            {
                return;
            }

            if (!AssistiveSupport.isScreenReaderEnabled)
            {
                if (_active)
                {
                    Deactivate();
                }

                return;
            }

            if (!_active)
            {
                Activate();
                _nextStructure = now;
                _nextLabels = now + LabelSeconds;
            }

            if (now >= _nextStructure)
            {
                _nextStructure = now + StructureSeconds;
                Rebuild();
            }

            if (now >= _nextLabels)
            {
                _nextLabels = now + LabelSeconds;
                RefreshMetricLabels();
            }
        }

        // Fuerza una reconciliación ya (pruebas).
        public void RebuildNow()
        {
            if (_active)
            {
                Rebuild();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Deactivate();
            _disposed = true;
        }

        private void Activate()
        {
            _active = true;
            _hierarchy = new AccessibilityHierarchy();
            try
            {
                AssistiveSupport.activeHierarchy = _hierarchy;
                IsActiveHierarchySupported = ReferenceEquals(AssistiveSupport.activeHierarchy, _hierarchy);
            }
            catch (Exception)
            {
                IsActiveHierarchySupported = false;
            }

            _shell.Toasts.Shown += OnToast;
            _workout = _shell.Ui.App.Workout.State.Subscribe(OnWorkout, false);
        }

        private void Deactivate()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _shell.Toasts.Shown -= OnToast;
            _workout?.Dispose();
            _workout = null;
            try
            {
                if (ReferenceEquals(AssistiveSupport.activeHierarchy, _hierarchy))
                {
                    AssistiveSupport.activeHierarchy = null;
                }
            }
            catch (Exception)
            {
                // Sin soporte en esta plataforma: no hay nada que soltar.
            }

            _hierarchy?.Clear();
            _hierarchy = null;
            _nodes.Clear();
            _elements.Clear();
            _kinds.Clear();
            _items.Clear();
            _previousKeys.Clear();
            _root = null;
        }

        // La raíz del recorrido: el diálogo de arriba (solo su subárbol) o el shell entero.
        private VisualElement ModalRoot()
        {
            var dialog = _shell.Dialogs.Top;
            return dialog != null ? dialog.Box : _shell;
        }

        private void Rebuild()
        {
            var hierarchy = _hierarchy;
            if (hierarchy == null)
            {
                return;
            }

            var root = ModalRoot();
            var rootChanged = !ReferenceEquals(root, _root);
            if (rootChanged)
            {
                var leavingDialog = _root != null && !ReferenceEquals(_root, _shell) && ReferenceEquals(root, _shell);
                hierarchy.Clear();
                ClearCount++;
                _nodes.Clear();
                _elements.Clear();
                _previousKeys.Clear();
                _root = root;
                if (leavingDialog && _opener != null)
                {
                    _focused = _opener;
                }
            }

            _scratch.Clear();
            _kinds.Clear();
            A11yRules.Collect(root, _shell.Ui.Texts, _scratch, _kinds);
            A11yOrder.Sort(_scratch);
            _nextKeys.Clear();
            for (var i = 0; i < _scratch.Count; i++)
            {
                _nextKeys.Add(_scratch[i].Key);
            }

            A11yReconciler.Diff(_previousKeys, _nextKeys, _edits);
            var changed = A11yReconciler.HasChanges(_edits);
            var focusLost = false;
            if (changed)
            {
                for (var i = 0; i < _edits.Count; i++)
                {
                    var edit = _edits[i];
                    var element = (VisualElement)edit.Key;
                    switch (edit.Kind)
                    {
                        case A11yEditKind.Remove:
                            if (_nodes.TryGetValue(element, out var removed))
                            {
                                hierarchy.RemoveNode(removed, true);
                                _nodes.Remove(element);
                                _elements.Remove(removed);
                            }

                            if (ReferenceEquals(element, _focused))
                            {
                                focusLost = true;
                            }

                            break;
                        case A11yEditKind.Insert:
                            var item = Find(_scratch, element)!;
                            var node = hierarchy.InsertNode(edit.Index, item.Label, null);
                            Configure(node, element, item);
                            _nodes[element] = node;
                            _elements[node] = element;
                            break;
                        case A11yEditKind.Move:
                            if (_nodes.TryGetValue(element, out var moved))
                            {
                                hierarchy.MoveNode(moved, null, edit.Index);
                            }

                            break;
                    }
                }

                ReconcileCount++;
            }

            // Lo que sigue: nombre, valor y estado al día (sin crear nodos).
            for (var i = 0; i < _scratch.Count; i++)
            {
                var item = _scratch[i];
                if (_nodes.TryGetValue((VisualElement)item.Key, out var node))
                {
                    Update(node, item);
                }
            }

            _items.Clear();
            _items.AddRange(_scratch);
            _previousKeys.Clear();
            _previousKeys.AddRange(_nextKeys);
            hierarchy.RefreshNodeFrames();
            FrameRefreshCount++;

            if (focusLost)
            {
                _focused = NearestAfterLoss();
                LayoutChangedCount++;
                _announcer.LayoutChanged(_focused != null ? NodeOf(_focused) : null);
            }

            if (changed || rootChanged)
            {
                AnnounceTitle();
            }
        }

        private VisualElement? NearestAfterLoss()
        {
            return _items.Count > 0 ? (VisualElement)_items[0].Key : null;
        }

        private void AnnounceTitle()
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Role == A11yRole.Header)
                {
                    if (_policy.Title(_items[i].Label))
                    {
                        _announcer.ScreenChanged(_items[i].Label);
                    }

                    return;
                }
            }
        }

        private void RefreshMetricLabels()
        {
            if (_hierarchy == null)
            {
                return;
            }

            foreach (var pair in _nodes)
            {
                if (_kinds.TryGetValue(pair.Key, out var kind) && kind == A11yRules.Kind.Metric)
                {
                    var item = A11yRules.Describe(pair.Key, kind, _shell.Ui.Texts);
                    if (item != null && pair.Value.label != item.Label)
                    {
                        pair.Value.label = item.Label;
                    }
                }
            }

            LabelRefreshCount++;
        }

        private void Configure(AccessibilityNode node, VisualElement element, A11yItem item)
        {
            Update(node, item);
            var scale = (float)Math.Max(0.01, _shell.Ui.Display.DpScale);
            node.frameGetter = () =>
            {
                var bounds = element.worldBound;
                return new Rect(bounds.x * scale, bounds.y * scale, bounds.width * scale, bounds.height * scale);
            };
            if (item.Role == A11yRole.Button || item.Role == A11yRole.TabButton || item.Role == A11yRole.Toggle || item.Role == A11yRole.TextField)
            {
                node.invoked += () => Invoke(element);
            }

            node.focusChanged += (focusedNode, focused) =>
            {
                if (focused)
                {
                    _focused = element;
                }
            };
        }

        private static void Update(AccessibilityNode node, A11yItem item)
        {
            if (node.label != item.Label)
            {
                node.label = item.Label;
            }

            if (node.value != item.Value)
            {
                node.value = item.Value;
            }

            var role = RoleFor(item.Role);
            if (node.role != role)
            {
                node.role = role;
            }

            var state = StateFor(item.State);
            if (node.state != state)
            {
                node.state = state;
            }
        }

        private static AccessibilityRole RoleFor(A11yRole role)
        {
            switch (role)
            {
                case A11yRole.Button:
                    return AccessibilityRole.Button;
                case A11yRole.TabBar:
                    return AccessibilityRole.TabBar;
                case A11yRole.TabButton:
                    return AccessibilityRole.TabButton;
                case A11yRole.Header:
                    return AccessibilityRole.Header;
                case A11yRole.StaticText:
                    return AccessibilityRole.StaticText;
                case A11yRole.TextField:
                    return AccessibilityRole.TextField;
                case A11yRole.Toggle:
                    return AccessibilityRole.Toggle;
                case A11yRole.ScrollView:
                    return AccessibilityRole.ScrollView;
                case A11yRole.Container:
                    return AccessibilityRole.Container;
                case A11yRole.Image:
                    return AccessibilityRole.Image;
                default:
                    return AccessibilityRole.None;
            }
        }

        private static AccessibilityState StateFor(A11yState state)
        {
            var result = AccessibilityState.None;
            if ((state & A11yState.Selected) != 0)
            {
                result |= AccessibilityState.Selected;
            }

            if ((state & A11yState.Disabled) != 0)
            {
                result |= AccessibilityState.Disabled;
            }

            if ((state & A11yState.Expanded) != 0)
            {
                result |= AccessibilityState.Expanded;
            }

            return result;
        }

        private static A11yItem? Find(List<A11yItem> items, VisualElement element)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (ReferenceEquals(items[i].Key, element))
                {
                    return items[i];
                }
            }

            return null;
        }

        // Un botón que abre un diálogo: no se sabe de antemano; se recuerda el último invocado fuera de un diálogo.
        private bool IsDialogOpener(VisualElement element)
        {
            return _shell.Dialogs.Top == null;
        }

        private void OnToast(string text, long showId)
        {
            if (_active && _policy.Toast(showId))
            {
                _announcer.Announce(text);
            }
        }

        // El cambio de paso dentro de la misma sesión (no el primero) y la rutina completada (AC-APP-03-14).
        private void OnWorkout(WorkoutSnapshot snapshot)
        {
            if (!_active || snapshot == null)
            {
                return;
            }

            var texts = _shell.Ui.Texts;
            if (snapshot.Phase == WorkoutPhase.Finished && snapshot.EndedBy == WorkoutEndKind.Completed)
            {
                if (_policy.Completion(snapshot.Session))
                {
                    _announcer.Announce(texts.Get(L10nKeys.WorkoutCompleted));
                }

                return;
            }

            if (!snapshot.Step.HasValue || (snapshot.Phase != WorkoutPhase.Active && snapshot.Phase != WorkoutPhase.Paused))
            {
                return;
            }

            var step = snapshot.Step.Value;
            var sameSession = snapshot.Session == _stepSession;
            var changed = sameSession && step.StepIndex != _stepIndex;
            _stepSession = snapshot.Session;
            _stepIndex = step.StepIndex;
            if (changed && _policy.Step(snapshot.Session, step.StepIndex))
            {
                var type = texts.Get(StepTypeInfo.NameKey(step.Step.Type));
                _announcer.Announce(A11yText.StepProgress(texts, step.StepIndex + 1, step.TotalSteps, type));
            }
        }
    }
}
