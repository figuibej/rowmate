using System;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Base de los diálogos modales de la app (F-APP-03.3, AC-APP-03-03; 03-rutinas/02-tech-design §4.5; 04-design-notes §4.9):
    //   .rm-dialog-host                         absoluto a pantalla completa, sin eventos
    //   ├── #dialog-scrim .rm-dialog__scrim     velo overlay.scrim (opacidad 0 → 0,6 por código); tocarlo = cancelar
    //   └── .rm-dialog-frame                    centra el diálogo (relleno inferior = teclado, KeyboardAvoider)
    //       └── #<nombre> .rm-dialog [--compact]
    //           ├── #dialog-title .rm-dialog__title .rm-type-title   ajusta, nunca "…"
    //           ├── .rm-dialog__pinned           opcional (la vista previa del paso, DN3-10)
    //           ├── ScrollView .rm-dialog__content [--scrolls]   ScrollPolicy + FocusScroller
    //           └── .rm-dialog__actions          abajo a la derecha: secundaria de texto, principal rellena
    // Geometría (DN3-10): ancho min(máximo, ventana − 80) con mínimo 280 (y nunca más que la ventana); máximo 400 (eliminar,
    // repeticiones) o 560 (paso); alto máximo ventana − 48 − área segura (− teclado); --compact con la ventana < 480 dp.
    // Cerrar: el primer cierre gana (idempotente). En ESE cuadro se aplica el resultado (Closed), ExitLock deja inerte el
    // árbol del diálogo, la trampa de foco se suelta y el foco vuelve al control que lo tenía al abrir. El velo sigue los
    // 150 ms de la salida y se TRAGA los toques sin cancelar nada (DT-21). Los nombres de contrato del diálogo que sale se
    // borran, así #dialog-confirm siempre es el del diálogo vivo.
    public abstract class ModalDialog
    {
        public const double MinWidthDp = 280;
        public const double SideMarginDp = 80;
        public const double VerticalMarginDp = 48;
        public const double CompactBelowDp = 480;
        public const double DeleteAndRepeatMaxWidthDp = 400;
        public const double StepMaxWidthDp = 560;

        private readonly DialogTransition _transition;
        private FocusTrap? _trap;
        private Focusable? _focusBefore;
        private DialogLayer? _layer;
        private bool _closing;
        private bool _opened;

        protected ModalDialog(UiContext ui, string name, double maxWidthDp)
        {
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MaxWidthDp = maxWidthDp;
            _transition = new DialogTransition(ui.Motion);

            Host = new VisualElement();
            Host.AddToClassList("rm-dialog-host");
            Host.pickingMode = PickingMode.Ignore;

            Scrim = new VisualElement { name = "dialog-scrim" };
            Scrim.AddToClassList("rm-dialog__scrim");
            Scrim.style.backgroundColor = TokenColors.ToColor(Tokens.OverlayScrim);
            Scrim.style.opacity = 0;
            Scrim.RegisterCallback<PointerDownEvent>(OnScrimPointerDown);
            Scrim.RegisterCallback<ClickEvent>(OnScrimClick);
            Scrim.RegisterCallback<NavigationSubmitEvent>(OnScrimSubmit);
            Host.Add(Scrim);

            Frame = new VisualElement();
            Frame.AddToClassList("rm-dialog-frame");
            Frame.pickingMode = PickingMode.Ignore;
            Host.Add(Frame);

            Box = new VisualElement { name = name };
            Box.AddToClassList("rm-dialog");
            Frame.Add(Box);

            Title = new Label { name = "dialog-title" };
            Title.AddToClassList("rm-dialog__title");
            Title.AddToClassList("rm-type-title");
            Title.pickingMode = PickingMode.Ignore;
            Box.Add(Title);

            Pinned = new VisualElement();
            Pinned.AddToClassList("rm-dialog__pinned");
            Pinned.pickingMode = PickingMode.Ignore;
            Pinned.style.display = DisplayStyle.None;
            Box.Add(Pinned);

            ContentScroll = new ScrollView(ScrollViewMode.Vertical);
            ContentScroll.AddToClassList("rm-dialog__content");
            ContentScroll.AddToClassList("rm-scroll");
            ScrollPolicy.Apply(ContentScroll, ui.IsDesktop);
            Content = ContentScroll.contentContainer;
            Content.AddToClassList("rm-dialog__content-body");
            Box.Add(ContentScroll);
            Scroller = new FocusScroller(ui, ContentScroll);
            ContentScroll.RegisterCallback<GeometryChangedEvent>(_ => UpdateScrollsClass());
            Content.RegisterCallback<GeometryChangedEvent>(_ => UpdateScrollsClass());

            Actions = new VisualElement();
            Actions.AddToClassList("rm-dialog__actions");
            Actions.pickingMode = PickingMode.Ignore;
            Box.Add(Actions);
        }

        public UiContext Ui { get; }

        public string Name { get; }

        public double MaxWidthDp { get; }

        public VisualElement Host { get; }

        public VisualElement Scrim { get; }

        public VisualElement Frame { get; }

        public VisualElement Box { get; }

        public Label Title { get; }

        public VisualElement Pinned { get; }

        public ScrollView ContentScroll { get; }

        public VisualElement Content { get; }

        public VisualElement Actions { get; }

        public FocusScroller Scroller { get; }

        public bool IsOpen => _opened && !_closing;

        public bool IsClosing => _closing;

        public bool WasConfirmed { get; private set; }

        public bool IsTransitioning => _transition.IsRunning;

        // Alto máximo vigente (lo usa el diálogo de paso para fijar o no la vista previa, DN3-10).
        public double MaxHeightDp { get; private set; }

        public double WidthDp { get; private set; }

        // Se cierra (con resultado o sin él): en el cuadro 0 de la salida.
        public event Action<ModalDialog>? Closed;

        // Cancelar, el velo, Escape y "Atrás" (AC-APP-03-03, AC-RUT-13-14).
        public void Cancel()
        {
            Close(false);
        }

        internal void Open(DialogLayer layer, Focusable? focusBefore)
        {
            _layer = layer;
            _focusBefore = focusBefore;
            _opened = true;
            _trap = new FocusTrap(Box);
            Relayout(layer.WindowWidthDp, layer.WindowHeightDp, layer.SafeVerticalDp, layer.KeyboardDp);
            Box.style.opacity = 0;
            _transition.PlayIn(Scrim, Box);
            OnOpened();
        }

        internal void Relayout(double windowWidthDp, double windowHeightDp, double safeVerticalDp, double keyboardDp)
        {
            if (!(windowWidthDp > 0) || !(windowHeightDp > 0))
            {
                return;
            }

            var width = Math.Min(MaxWidthDp, windowWidthDp - SideMarginDp);
            width = Math.Max(MinWidthDp, width);
            width = Math.Min(width, windowWidthDp);
            var maxHeight = windowHeightDp - keyboardDp - VerticalMarginDp - safeVerticalDp;
            WidthDp = width;
            MaxHeightDp = maxHeight;
            Box.style.width = (float)width;
            Box.style.maxHeight = (float)Math.Max(0, maxHeight);
            Frame.style.paddingBottom = (float)keyboardDp;
            Box.EnableInClassList("rm-dialog--compact", windowHeightDp < CompactBelowDp);
            OnLayout(width, maxHeight);
        }

        // Confirmar (o un resultado válido): cierra con resultado.
        protected void Accept()
        {
            Close(true);
        }

        protected RmButton AddAction(string name, string variant, string label, Action onClick)
        {
            var button = new RmButton(name, variant, null, label, () =>
            {
                if (IsOpen)
                {
                    onClick();
                }
            });
            Actions.Add(button);
            return button;
        }

        // Después de agregarse (el foco inicial, si lo hay: AC-RUT-14-04).
        protected virtual void OnOpened()
        {
        }

        // Ancho y alto máximo nuevos (mosaicos 2 × 2 o 4 en fila, vista previa fija o no: DN3-10).
        protected virtual void OnLayout(double widthDp, double maxHeightDp)
        {
        }

        // En el cuadro 0 de la salida, antes de Closed.
        protected virtual void OnClosing(bool confirmed)
        {
        }

        private void Close(bool confirmed)
        {
            if (!_opened || _closing)
            {
                return;
            }

            _closing = true;
            WasConfirmed = confirmed;
            _trap?.Release();
            ExitLock.Apply(Box, _focusBefore);
            ClearContractNames(Box);
            Scrim.name = "";
            OnClosing(confirmed);
            Closed?.Invoke(this);
            _layer?.OnDialogClosing(this);
            _transition.PlayOut(Scrim, Box, () => _layer?.OnDialogClosed(this));
        }

        private void UpdateScrollsClass()
        {
            var viewport = ContentScroll.contentViewport.layout.height;
            var content = Content.layout.height;
            ContentScroll.EnableInClassList("rm-dialog__content--scrolls", viewport > 0 && content > viewport + 0.5f);
        }

        private void OnScrimPointerDown(PointerDownEvent evt)
        {
            evt.StopPropagation();
        }

        private void OnScrimClick(ClickEvent evt)
        {
            evt.StopPropagation();
            if (!_closing)
            {
                Cancel();
            }
        }

        private void OnScrimSubmit(NavigationSubmitEvent evt)
        {
            evt.StopPropagation();
            if (!_closing)
            {
                Cancel();
            }
        }

        private static void ClearContractNames(VisualElement element)
        {
            if (!string.IsNullOrEmpty(element.name) && !element.name.StartsWith("unity-", StringComparison.Ordinal))
            {
                element.name = "";
            }

            var count = element.hierarchy.childCount;
            for (var i = 0; i < count; i++)
            {
                ClearContractNames(element.hierarchy[i]);
            }
        }
    }
}
