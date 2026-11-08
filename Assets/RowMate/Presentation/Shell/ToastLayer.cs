using System;
using System.Collections.Generic;
using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Avisos (AC-APP-03-05; el SnackBar del legacy; 03-rutinas/02-tech-design §4.7; 04-design-notes §4.12):
    //   #toast-layer .rm-layer > #toast .rm-toast > #toast-text .rm-toast__text .rm-type-body-m (ajusta, nunca "…")
    // - Show(texto): si no hay uno a la vista, entra (sube 16 + fundido en 220) y se va a los 4 s (160, bajando 8); si hay
    //   uno, lo REEMPLAZA: cambia el texto (cruce 0-80 / 80-200 si cambia), da el empujón 1 → 1,02 → 1 y cuenta sus 4 s desde
    //   ahí (DV-14: el legacy los encola). El reloj de los 4 s es el IScheduler (FakeScheduler en pruebas): no es una
    //   animación, así Settle y la regla de toma no lo esperan.
    // - Ancho: ventana < 600 dp, de 16 a 16; si no, clamp(texto + 32; 344; 560), centrado.
    // - Posición (DN3-13): base = alto de la barra visible (o el área segura inferior, con el editor encima) + 16; si su
    //   rectángulo se cruza con un obstáculo inferior (el +, la llave) agrandado 8 dp, sube hasta 8 dp sobre él y repite
    //   (BottomObstacles.Lift). Si un obstáculo cambia mientras se ve, lo sigue con un resorte ω 22.
    // - Después de Dispose, Show no hace nada.
    // - It. 10a (02-tech-design §6.3; AC-BLE-08-06, -09-02; DV-98, DV-99): ShowLink reutiliza ESTE aviso para el enlace con el remo (no
    //   suma otro: el HUD está cerca de su tope y el ToastLayer está fuera de #immersive). Suma un hijo #toast-indicator (LinkIndicator,
    //   1 elemento) y la clase .rm-toast--link. "Reconectando…" queda FIJO hasta "Reconectado" (2 s) o Cleared (null); "El monitor dejó
    //   de enviar datos", 4 s. Un aviso común que llega con el fijo a la vista lo reemplaza 4 s y después vuelve el fijo.
    public sealed class ToastLayer : VisualElement, IDisposable
    {
        public const double NarrowWindowDp = 600;
        public const double SideMarginDp = 16;
        public const double MinWidthDp = 344;
        public const double MaxWidthDp = 560;
        public const double TextPaddingDp = 32;
        public const double BaseGapDp = 16;
        public const double LinkIndicatorDp = 24;

        private readonly UiContext _ui;
        private readonly ShellLayout _layout;
        private readonly ToastTransition _transition;
        private readonly SpringMotion _bottom;
        private readonly List<BottomObstacle> _obstacles = new List<BottomObstacle>();
        private IDisposable? _timer;
        private bool _visible;
        private bool _disposed;
        private string _text = "";
        private LinkToast? _sticky;
        private LinkToast? _link;

        public ToastLayer(UiContext ui, ShellLayout layout)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            name = "toast-layer";
            AddToClassList("rm-layer");
            pickingMode = PickingMode.Ignore;

            Toast = new VisualElement { name = "toast" };
            Toast.AddToClassList("rm-toast");
            Toast.pickingMode = PickingMode.Ignore;
            Toast.style.display = DisplayStyle.None;
            Text = new Label { name = "toast-text" };
            Text.AddToClassList("rm-toast__text");
            Text.AddToClassList("rm-type-body-m");
            Text.pickingMode = PickingMode.Ignore;
            Text.enableRichText = false;
            Text.parseEscapeSequences = false;
            Indicator = new LinkIndicator(ui.Motion, 16) { name = "toast-indicator" };
            Indicator.AddToClassList("rm-toast__indicator");
            Indicator.MarkAsToast = true;
            Indicator.Show(LinkIndicator.Mode.None);
            Toast.Add(Indicator);
            Toast.Add(Text);
            Add(Toast);

            _transition = new ToastTransition(ui.Motion);
            _bottom = new SpringMotion(ui.Motion, value => Toast.style.bottom = (float)value);
            _layout.ObstaclesChanged += Reposition;
            _layout.Changed += Reposition;
            Toast.RegisterCallback<GeometryChangedEvent>(_ => Reposition());
        }

        public VisualElement Toast { get; }

        public Label Text { get; }

        // It. 10a: el indicador del aviso del enlace (oculto en los avisos comunes).
        public LinkIndicator Indicator { get; }

        // El aviso del enlace a la vista (null si el que se ve es común o no hay ninguno).
        public LinkToast? CurrentLink => _visible ? _link : null;

        // "Reconectando…" fijo (aunque un aviso común lo tape por 4 s).
        public LinkToast? StickyLink => _sticky;

        // Hay un aviso a la vista (entrando o quieto; no saliendo).
        public bool IsVisible => _visible;

        public string CurrentText => _visible ? _text : "";

        // Veces que se mostró un aviso (las pruebas de "uno solo" miran Toast, no esto).
        public int ShowCount { get; private set; }

        public bool IsTransitioning => _transition.IsRunning || _bottom.IsRunning;

        public void Show(string text)
        {
            if (_disposed || text == null)
            {
                return;
            }

            Present(text, TimeSpan.FromMilliseconds(UiTimings.ToastVisibleMs), null);
        }

        // It. 10a (§6.3): el aviso del enlace (null = Cleared: se quita el fijo).
        public void ShowLink(LinkToast? toast)
        {
            if (_disposed)
            {
                return;
            }

            if (toast == null)
            {
                _sticky = null;
                if (_visible && _link != null)
                {
                    Hide();
                }

                return;
            }

            _sticky = toast.Duration.HasValue ? null : toast;
            Present(toast.Text, toast.Duration, toast);
        }

        private void Present(string text, TimeSpan? duration, LinkToast? link)
        {
            ShowCount++;
            _timer?.Dispose();
            _timer = duration.HasValue ? _ui.App.Scheduler.Schedule(duration.Value, OnTimer) : null;
            _link = link;
            Toast.EnableInClassList("rm-toast--link", link != null);
            // WP-D (00-arte §4.2): el texto del enlace en titleXs 14/600 (el aviso común sigue en bodyM 14).
            Text.EnableInClassList("rm-type-body-m", link == null);
            Text.EnableInClassList("rm-type-title-xs", link != null);
            Indicator.Show(link == null ? LinkIndicator.Mode.None : ModeFor(link.Kind));
            if (_visible && !_transition.IsLeaving)
            {
                var changed = !string.Equals(text, _text, StringComparison.Ordinal);
                _text = text;
                if (changed && (_ui.Motion.Settings.ReduceMotion || !_ui.Motion.Armed))
                {
                    Text.text = text;
                    Resize();
                }

                _transition.PlayBump(Toast, Text, changed ? (Action)(() =>
                {
                    Text.text = _text;
                    Resize();
                }) : null);
                return;
            }

            _visible = true;
            _text = text;
            Text.text = text;
            Toast.style.display = DisplayStyle.Flex;
            Toast.style.opacity = 0;
            Resize();
            _bottom.Snap(TargetBottom());
            _transition.PlayIn(Toast, Text);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer?.Dispose();
            _timer = null;
            _transition.Finish();
            _bottom.Stop();
            _layout.ObstaclesChanged -= Reposition;
            _layout.Changed -= Reposition;
        }

        private static LinkIndicator.Mode ModeFor(LinkNoticeKind kind)
        {
            switch (kind)
            {
                case LinkNoticeKind.Reconnecting:
                    return LinkIndicator.Mode.Spinner;
                case LinkNoticeKind.Reconnected:
                    return LinkIndicator.Mode.Check;
                case LinkNoticeKind.MonitorStopped:
                    return LinkIndicator.Mode.LinkOff;
                default:
                    return LinkIndicator.Mode.None;
            }
        }

        // Vencieron los 4 s (o los 2 s del enlace): si el fijo sigue vigente y lo tapaba un aviso común, vuelve; si no, se va.
        private void OnTimer()
        {
            var sticky = _sticky;
            if (sticky != null && !_disposed && _visible && !ReferenceEquals(_link, sticky))
            {
                Present(sticky.Text, null, sticky);
                return;
            }

            Hide();
        }

        private void Hide()
        {
            _timer?.Dispose();
            _timer = null;
            if (_disposed || !_visible)
            {
                return;
            }

            _visible = false;
            _transition.PlayOut(Toast, Text, () =>
            {
                if (!_visible)
                {
                    Toast.style.display = DisplayStyle.None;
                    Toast.style.opacity = StyleKeyword.Null;
                    Indicator.Show(LinkIndicator.Mode.None);
                    Toast.RemoveFromClassList("rm-toast--link");
                }
            });
        }

        private void Resize()
        {
            var window = _layout.WindowSize;
            if (!(window.x > 0))
            {
                return;
            }

            if (window.x < NarrowWindowDp)
            {
                Toast.style.left = (float)SideMarginDp;
                Toast.style.right = (float)SideMarginDp;
                Toast.style.width = StyleKeyword.Auto;
                return;
            }

            var natural = Text.MeasureTextSize(_text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
            // It. 10a: con el indicador del enlace, sus 16 y 8 de aire.
            var indicator = _link != null ? LinkIndicatorDp : 0;
            var width = Math.Max(MinWidthDp, Math.Min(MaxWidthDp, natural + TextPaddingDp + indicator));
            Toast.style.width = (float)width;
            Toast.style.left = (float)((window.x - width) / 2);
            Toast.style.right = StyleKeyword.Auto;
        }

        private double TargetBottom()
        {
            var window = _layout.WindowSize;
            var baseBottom = (_layout.TabBarVisible ? _layout.CurrentTabBarHeight : _layout.Insets.Bottom) + BaseGapDp;
            var height = Toast.layout.height > 0 ? Toast.layout.height : 48;
            double left;
            double right;
            if (window.x < NarrowWindowDp)
            {
                left = SideMarginDp;
                right = window.x - SideMarginDp;
            }
            else
            {
                var width = Toast.layout.width > 0 ? Toast.layout.width : MinWidthDp;
                left = (window.x - width) / 2;
                right = left + width;
            }

            _layout.CopyObstacles(_obstacles);
            return BottomObstacles.Lift(left, right, baseBottom, height, _obstacles, MotionSpecs.BottomObstacleGapDp);
        }

        private void Reposition()
        {
            if (_disposed || !_visible)
            {
                return;
            }

            Resize();
            _bottom.MoveTo(TargetBottom(), MotionSpecs.KeyOmega);
        }
    }
}
