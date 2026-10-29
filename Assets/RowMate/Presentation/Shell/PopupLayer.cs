using System;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Capa de menús y ayudas (03-rutinas/02-tech-design §4.1, §4.6; 04-design-notes §4.4): #popup-layer .rm-layer, ARRIBA de
    // los diálogos y los avisos y ABAJO de la capa del simulador (AC-APP-03-04, -07). Un menú por vez, con
    // .rm-popup-catcher a pantalla completa debajo (tocar afuera = cerrar), y una ayuda por vez.
    // - Menú anclado (⋮): debajo del botón con 4 dp de aire y su borde derecho alineado con el del botón; arriba si no entra;
    //   siempre a ≥ 8 dp de los bordes. Menú en un punto (texto): su esquina superior izquierda en el puntero + (2; 2), con
    //   las mismas vueltas. Entra con escala 0,96 → 1 y fundido en 120 (origen en la esquina del ancla); sale en 90.
    // - Al elegir, tocar afuera, Escape o "Atrás", el menú se cierra en el cuadro 0: ExitLock lo deja inerte, el foco vuelve al
    //   ancla y el atrapador se TRAGA los toques hasta que el menú sale del árbol (DT-21: dos toques en Eliminar abren un solo
    //   diálogo). Después corre la acción elegida.
    // - Ayuda (.rm-tooltip, DN3-12): centrada 8 dp debajo del ancla (arriba si no entra), ancho máximo 240, a ≥ 8 dp de los
    //   bordes; ella y TODO su árbol con pickingMode = Ignore: nunca recibe ni tapa un toque (QA-G3-01).
    public sealed class PopupLayer : VisualElement, IDisposable
    {
        public const float AnchorGapDp = 4;
        public const float EdgeMarginDp = 8;
        public const float PointOffsetDp = 2;
        public const float TooltipGapDp = 8;

        private readonly UiContext _ui;
        private readonly VisualElement _catcher;
        private readonly DialogTransition _menuTransition;
        private readonly DialogTransition _tooltipTransition;
        private PopupMenu? _menu;
        private PopupMenu? _closingMenu;
        private VisualElement? _anchor;
        private Vector2? _point;
        private Focusable? _focusReturn;
        private FocusTrap? _trap;
        private bool _disposed;

        public PopupLayer(UiContext ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            name = "popup-layer";
            AddToClassList("rm-layer");
            pickingMode = PickingMode.Ignore;

            _catcher = new VisualElement { name = "popup-catcher" };
            _catcher.AddToClassList("rm-popup-catcher");
            _catcher.style.display = DisplayStyle.None;
            _catcher.RegisterCallback<PointerDownEvent>(OnCatcherPointer);
            _catcher.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            _catcher.RegisterCallback<NavigationSubmitEvent>(OnCatcherSubmit);
            Add(_catcher);

            Tooltip = new Label { name = "tooltip" };
            Tooltip.AddToClassList("rm-tooltip");
            Tooltip.AddToClassList("rm-type-body-xs");
            Tooltip.pickingMode = PickingMode.Ignore;
            Tooltip.enableRichText = false;
            Tooltip.style.display = DisplayStyle.None;
            Tooltip.RegisterCallback<GeometryChangedEvent>(_ => PlaceTooltip());
            Add(Tooltip);

            _menuTransition = new DialogTransition(ui.Motion);
            _tooltipTransition = new DialogTransition(ui.Motion);
        }

        public PopupMenu? Menu => _menu;

        public bool HasOpenMenu => _menu != null;

        public Label Tooltip { get; }

        public VisualElement? TooltipAnchor { get; private set; }

        public bool IsTooltipVisible => TooltipAnchor != null;

        public bool IsTransitioning => _menuTransition.IsRunning || _tooltipTransition.IsRunning;

        public VisualElement Catcher => _catcher;

        public void ShowMenu(PopupMenu menu, VisualElement anchor)
        {
            Open(menu, anchor, null, anchor);
        }

        public void ShowMenuAt(PopupMenu menu, Vector2 panelPoint, Focusable? focusReturn)
        {
            Open(menu, null, panelPoint, focusReturn);
        }

        // Tocar afuera (o un elemento elegido): cierra el menú abierto, si hay.
        public void CloseMenu()
        {
            CloseMenuThen(null);
        }

        // "Atrás" y Escape (BackNavigation, paso 1): cierra el menú o la ayuda. false = no había nada abierto.
        public bool CloseForBack()
        {
            if (_menu != null)
            {
                CloseMenu();
                return true;
            }

            if (TooltipAnchor != null)
            {
                HideTooltipNow();
                return true;
            }

            return false;
        }

        public void ShowTooltip(VisualElement anchor, string text)
        {
            if (_disposed || anchor == null)
            {
                return;
            }

            var wasVisible = TooltipAnchor != null;
            TooltipAnchor = anchor;
            Tooltip.text = text ?? "";
            Tooltip.style.display = DisplayStyle.Flex;
            PlaceTooltip();
            if (!wasVisible)
            {
                Tooltip.style.opacity = 0;
                _tooltipTransition.PlayPopupIn(Tooltip, MotionSpecs.TooltipInMs, 1);
            }
        }

        // La ayuda de anchor sale (90 ms). Una ayuda de otro ancla no se toca.
        public void HideTooltip(VisualElement anchor)
        {
            if (TooltipAnchor == null || !ReferenceEquals(TooltipAnchor, anchor))
            {
                return;
            }

            HideTooltipNow();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _menuTransition.Finish();
            _tooltipTransition.Finish();
        }

        private void Open(PopupMenu menu, VisualElement? anchor, Vector2? point, Focusable? focusReturn)
        {
            if (_disposed || menu == null)
            {
                return;
            }

            if (_menu != null || _closingMenu != null)
            {
                return; // uno por vez; mientras uno sale, el atrapador se traga los toques
            }

            HideTooltipNow();
            _menu = menu;
            _anchor = anchor;
            _point = point;
            _focusReturn = focusReturn;
            menu.OnChoose = action => CloseMenuThen(action);
            _catcher.style.display = DisplayStyle.Flex;
            menu.style.position = Position.Absolute;
            menu.style.opacity = 0;
            Add(menu);
            Tooltip.BringToFront();
            menu.RegisterCallback<GeometryChangedEvent>(OnMenuGeometry);
            _trap = new FocusTrap(menu);
            PlaceMenu();
            _menuTransition.PlayPopupIn(menu, MotionSpecs.MenuInMs, MotionSpecs.MenuInScaleFrom);
            if (_ui.App != null && IsKeyboardNavigation() && menu.Items.Count > 0 && menu.Items[0].focusable)
            {
                menu.Items[0].Focus();
            }
        }

        private void CloseMenuThen(Action? action)
        {
            var menu = _menu;
            if (menu == null)
            {
                return;
            }

            _menu = null;
            _closingMenu = menu;
            _trap?.Release();
            _trap = null;
            ExitLock.Apply(menu, _focusReturn);
            ClearNames(menu);
            menu.UnregisterCallback<GeometryChangedEvent>(OnMenuGeometry);
            _menuTransition.PlayPopupOut(menu, MotionSpecs.MenuOutMs, () =>
            {
                menu.RemoveFromHierarchy();
                if (ReferenceEquals(_closingMenu, menu))
                {
                    _closingMenu = null;
                }

                if (_menu == null && _closingMenu == null)
                {
                    _catcher.style.display = DisplayStyle.None;
                }
            });
            action?.Invoke();
        }

        private void HideTooltipNow()
        {
            if (TooltipAnchor == null)
            {
                return;
            }

            TooltipAnchor = null;
            _tooltipTransition.PlayPopupOut(Tooltip, MotionSpecs.TooltipOutMs, () =>
            {
                if (TooltipAnchor == null)
                {
                    Tooltip.style.display = DisplayStyle.None;
                }
            });
        }

        private void OnMenuGeometry(GeometryChangedEvent evt)
        {
            PlaceMenu();
        }

        private void PlaceMenu()
        {
            var menu = _menu;
            if (menu == null)
            {
                return;
            }

            var window = worldBound;
            var size = menu.layout.size;
            if (!(window.width > 0))
            {
                return;
            }

            var width = size.x > 0 ? size.x : 140;
            var height = size.y > 0 ? size.y : 112;
            float left;
            float top;
            float alternateTop;
            var originLeft = true;
            var originTop = true;
            if (_anchor != null)
            {
                var a = _anchor.worldBound;
                left = a.xMax - width;
                top = a.yMax + AnchorGapDp;
                originLeft = false;
                if (top + height > window.yMax - EdgeMarginDp && a.yMin - AnchorGapDp - height >= window.yMin + EdgeMarginDp)
                {
                    top = a.yMin - AnchorGapDp - height;
                    originTop = false;
                }

                alternateTop = originTop ? a.yMin - AnchorGapDp - height : a.yMax + AnchorGapDp;
            }
            else
            {
                var p = _point ?? Vector2.zero;
                left = p.x + PointOffsetDp;
                top = p.y + PointOffsetDp;
                if (left + width > window.xMax - EdgeMarginDp)
                {
                    left = p.x - PointOffsetDp - width;
                    originLeft = false;
                }

                if (top + height > window.yMax - EdgeMarginDp)
                {
                    top = p.y - PointOffsetDp - height;
                    originTop = false;
                }

                alternateTop = originTop ? p.y - PointOffsetDp - height : p.y + PointOffsetDp;
            }

            left = Mathf.Clamp(left, window.xMin + EdgeMarginDp, Mathf.Max(window.xMin + EdgeMarginDp, window.xMax - EdgeMarginDp - width));
            top = Mathf.Clamp(top, window.yMin + EdgeMarginDp, Mathf.Max(window.yMin + EdgeMarginDp, window.yMax - EdgeMarginDp - height));

            // La llave del simulador, agrandada 8 dp, es un borde (D-ART-10, R6; DD4a-02).
            var placed = PopupMenu.AvoidWrench(new Rect(left, top, width, height), _ui.Layout.VisibleWrenchBounds(), alternateTop, window, EdgeMarginDp,
                out var flipped);
            if (flipped)
            {
                originTop = !originTop;
            }

            left = placed.x;
            top = placed.y;
            menu.style.transformOrigin = new TransformOrigin(new Length(originLeft ? 0 : 100, LengthUnit.Percent),
                new Length(originTop ? 0 : 100, LengthUnit.Percent));
            menu.style.left = left - window.xMin;
            menu.style.top = top - window.yMin;
        }

        private void PlaceTooltip()
        {
            var anchor = TooltipAnchor;
            if (anchor == null)
            {
                return;
            }

            var window = worldBound;
            var size = Tooltip.layout.size;
            var a = anchor.worldBound;
            var width = size.x;
            var height = size.y;
            var left = a.center.x - width / 2;
            var top = a.yMax + TooltipGapDp;
            if (top + height > window.yMax - EdgeMarginDp)
            {
                top = a.yMin - TooltipGapDp - height;
            }

            left = Mathf.Clamp(left, window.xMin + EdgeMarginDp, Mathf.Max(window.xMin + EdgeMarginDp, window.xMax - EdgeMarginDp - width));
            Tooltip.style.left = left - window.xMin;
            Tooltip.style.top = top - window.yMin;
        }

        private void OnCatcherPointer(PointerDownEvent evt)
        {
            evt.StopPropagation();
            if (_menu != null)
            {
                CloseMenu();
            }
        }

        private void OnCatcherSubmit(NavigationSubmitEvent evt)
        {
            evt.StopPropagation();
            if (_menu != null)
            {
                CloseMenu();
            }
        }

        private bool IsKeyboardNavigation()
        {
            for (var e = hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                if (e.ClassListContains(FocusHighlight.KeyboardNavClass))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ClearNames(VisualElement element)
        {
            if (!string.IsNullOrEmpty(element.name) && !element.name.StartsWith("unity-", StringComparison.Ordinal))
            {
                element.name = "";
            }

            var count = element.hierarchy.childCount;
            for (var i = 0; i < count; i++)
            {
                ClearNames(element.hierarchy[i]);
            }
        }
    }
}
