using System;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // "Shell listo" (It. 14, DT14-11): el primer GeometryChangedEvent del contenido de la pestaña activa con ancho > 0, más un cuadro
    // (para que el texto quede generado). Lo usan BootSplashView (empieza la salida) y la medición del arranque (-rowmate-perf-boot).
    // Tick() una vez por cuadro (ShellHandle.Tick). Después de listo no hace nada y no queda suscrito.
    public sealed class ShellReadySignal
    {
        private readonly TabHost _tabs;
        private VisualElement? _watched;
        private bool _geometrySeen;

        public ShellReadySignal(TabHost tabs)
        {
            _tabs = tabs ?? throw new ArgumentNullException(nameof(tabs));
        }

        public bool IsReady { get; private set; }

        // El instante (reloj de las animaciones) en que quedó listo; NaN mientras no.
        public double ReadyAt { get; private set; } = double.NaN;

        public event Action? Ready;

        public void Tick(double nowSeconds)
        {
            if (IsReady)
            {
                return;
            }

            if (_geometrySeen)
            {
                IsReady = true;
                ReadyAt = nowSeconds;
                Unwatch();
                Ready?.Invoke();
                return;
            }

            var current = _tabs.Current >= 0 && _tabs.Current < _tabs.Screens.Count ? _tabs.Screens[_tabs.Current] : null;
            if (current == null || ReferenceEquals(current, _watched))
            {
                return;
            }

            Unwatch();
            _watched = current;
            _watched.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            if (_watched.layout.width > 0 && !float.IsNaN(_watched.layout.width))
            {
                _geometrySeen = true;
            }
        }

        private void OnGeometry(GeometryChangedEvent evt)
        {
            if (evt.newRect.width > 0)
            {
                _geometrySeen = true;
            }
        }

        private void Unwatch()
        {
            _watched?.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            _watched = null;
        }
    }

    // La capa de arranque (It. 14, AC-APP-16-10, Q-86; DT14-10, DT14-11, QA-G14-02; 00-arte-pulido §2): fondo bg.app y la marca de 96 dp
    // centrada (sin área segura) desde el primer cuadro que dibuja la app, hasta que el shell está listo (ShellReadySignal); ahí se funde
    // en 200 ms con ease.exit (120 ms lineal con reducir movimiento, BootSplashMotion) y sale de la jerarquía. No agrega espera. Si a los
    // 1 500 ms el shell no está listo, aparece el Spinner(32) 32 dp debajo de la marca con un fundido de 160 ms. Bloquea la entrada
    // (pickingMode Position) hasta que empieza la salida. Es la ÚLTIMA hija del shell (en UI Toolkit la hija de después se dibuja
    // arriba): AppShellView la agrega al final y, mientras viva, la vuelve a poner adelante si se agrega otra capa después. Los estilos
    // de la capa van en línea (fondo, centrado) y la clase .rm-boot-splash queda para el Designer.
    public sealed class BootSplashView : VisualElement, IMotion, ILayoutBoundMotion
    {
        public const string UssClass = "rm-boot-splash";
        public const string MarkClass = "rm-boot-splash__mark";
        public const string SpinnerClass = "rm-boot-splash__spinner";

        private readonly UiContext _ui;
        private readonly ShellReadySignal _ready;
        private double _startedAt = double.NaN;
        private bool _done;
        private bool _held;

        public BootSplashView(UiContext ui, ShellReadySignal ready, Texture2D? mark)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _ready = ready ?? throw new ArgumentNullException(nameof(ready));
            name = "boot-splash";
            AddToClassList(UssClass);
            pickingMode = PickingMode.Position;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.backgroundColor = TokenColors.ToColor(Tokens.BgApp);
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            Mark = new VisualElement { name = "boot-splash-mark", pickingMode = PickingMode.Ignore };
            Mark.AddToClassList(MarkClass);
            Mark.style.width = BootSplashMotion.MarkDp;
            Mark.style.height = BootSplashMotion.MarkDp;
            Mark.style.flexShrink = 0;
            if (mark != null)
            {
                Mark.style.backgroundImage = new StyleBackground(mark);
                Mark.style.unityBackgroundImageTintColor = Color.white;
            }

            Add(Mark);

            // El indicador va debajo de la marca, fuera del flujo (la marca queda centrada con o sin él).
            Spinner = new Spinner(ui.Motion, BootSplashMotion.SpinnerDp, 3);
            Spinner.AddToClassList(SpinnerClass);
            Spinner.style.position = Position.Absolute;
            Spinner.style.left = new Length(50, LengthUnit.Percent);
            Spinner.style.top = new Length(50, LengthUnit.Percent);
            Spinner.style.marginLeft = -BootSplashMotion.SpinnerDp / 2f;
            Spinner.style.marginTop = BootSplashMotion.MarkDp / 2f + BootSplashMotion.SpinnerGapDp;
            Spinner.style.color = TokenColors.ToColor(Tokens.AccentPrimary);
            Spinner.style.opacity = 0;
            Spinner.SetShown(false);
            Add(Spinner);

            RegisterCallback<AttachToPanelEvent>(_ => _ui.Motion.Add(this));
            RegisterCallback<DetachFromPanelEvent>(_ => _ui.Motion.Remove(this));
        }

        public VisualElement Mark { get; }

        public Spinner Spinner { get; }

        public bool IsDone => _done;

        // Mientras el shell no está listo, la línea de tiempo espera al layout (Settle deja pasar un cuadro real).
        public bool IsWaitingForLayout => !_ready.IsReady;

        public bool IsFinite => !_held;

        // Solo el arnés (CAP-03, CAP-04): la capa queda como en el primer cuadro (opacidad 1, sin salida) para la toma.
        internal void HoldForCapture()
        {
            _held = true;
            CurrentOpacity = 1;
            style.opacity = 1;
            pickingMode = PickingMode.Position;
        }

        // La opacidad que se aplicó en el último Tick (1 hasta que empieza la salida).
        public float CurrentOpacity { get; private set; } = 1;

        public bool Tick(double nowSeconds)
        {
            if (_done)
            {
                return false;
            }

            if (double.IsNaN(_startedAt))
            {
                _startedAt = nowSeconds;
            }

            KeepOnTop();
            if (_held)
            {
                return true;
            }

            if (!_ready.IsReady)
            {
                var spinner = BootSplashMotion.SpinnerOpacity((nowSeconds - _startedAt) * 1000.0);
                if (spinner > 0 && !Spinner.IsShown)
                {
                    Spinner.SetShown(true);
                }

                Spinner.style.opacity = (float)spinner;
                return true;
            }

            var since = (nowSeconds - _ready.ReadyAt) * 1000.0;
            var reduce = _ui.Motion.Settings.ReduceMotion;
            if (!BootSplashMotion.BlocksInput(since) && pickingMode != PickingMode.Ignore)
            {
                pickingMode = PickingMode.Ignore;
            }

            CurrentOpacity = (float)BootSplashMotion.Opacity(since, reduce);
            style.opacity = CurrentOpacity;
            if (BootSplashMotion.IsDone(since, reduce))
            {
                _done = true;
                Spinner.SetShown(false);
                RemoveFromHierarchy();
                return false;
            }

            return true;
        }

        // Siempre la última hija del shell mientras viva (QA-G14-02).
        private void KeepOnTop()
        {
            var host = parent;
            if (host != null && host.IndexOf(this) != host.childCount - 1)
            {
                BringToFront();
            }
        }
    }
}
