using System;
using RowMate.App.Shell;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Device;
using RowMate.Presentation.Placeholder;
using RowMate.Presentation.Routines;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Capa superior opcional (la del simulador, §3.5): la arma quien compone la app (Bootstrap) con el shell ya creado.
    public delegate VisualElement? TopLayerFactory(UiContext ui, ShellLayout layout, VisualElement tabBar);

    // Shell de la app (F-APP-01, F-APP-02, F-APP-03; 02-tech-design §3.2 de la It. 2; 03-rutinas/02-tech-design §4.1; legacy
    // MainShell, main.dart:116-181). Los nombres son contrato:
    //   #app-root .rm-app .rm-text-scale-<n> [.rm-reduce-motion] [.rm-desktop] [.rm-keyboard-nav]   sin padding
    //   ├── #tab-content .rm-tab-content       capa 0 (TabHost: las 4/5 pantallas, construidas una vez; Rutinas = RoutinesScreenView)
    //   ├── #tab-bar .rm-tab-bar               capa 1 (absoluta abajo, sobre el contenido)
    //   ├── #page-stack .rm-layer              capa 2 (PageStack: el editor)
    //   ├── #dialog-layer .rm-layer            capa 3 (DialogLayer)
    //   ├── #toast-layer .rm-layer             capa 4 (ToastLayer)
    //   ├── #popup-layer .rm-layer             capa 5 (PopupLayer: el ⋮, el menú de texto y las ayudas)
    //   └── #simulator-layer .rm-layer         capa 6, último hijo: por encima de todo (solo con el define)
    // UI Toolkit dibuja y rinde eventos en orden de árbol: la capa del simulador queda arriba sin depender de ninguna pantalla
    // (AC-APP-03-07). Las capas se crean ANTES que las pantallas (las reciben por UiContext) y se agregan en este orden.
    public sealed class AppShellView : VisualElement, IDisposable
    {
        public const string ReduceMotionClass = "rm-reduce-motion";
        public const string DesktopClass = "rm-desktop";

        private string _textScaleClass = "";
        private bool _disposed;

        public AppShellView(UiContext ui, TopLayerFactory? topLayer)
        {
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));
            name = "app-root";
            AddToClassList("rm-app");
            EnableInClassList(DesktopClass, ui.IsDesktop);
            EnableInClassList(MeshColor.GammaPanelClass, ui.GammaPanel);
            pickingMode = PickingMode.Ignore;
            Layout = new ShellLayout(ui.Display);
            Layout.AttachWindow(this);
            var shell = ui.App.Shell;

            // Las capas primero (03-rutinas/02-tech-design §4.1): las pantallas las reciben por UiContext.
            Pages = new PageStack(ui);
            Dialogs = new DialogLayer(ui, Layout);
            Toasts = new ToastLayer(ui, Layout);
            Popups = new PopupLayer(ui);
            ui.AttachLayers(Layout, Pages, Dialogs, Toasts, Popups);
            Keyboard = new KeyboardAvoider(ui);

            Tabs = new TabHost(ui, shell);
            for (var i = 0; i < shell.Destinations.Count; i++)
            {
                var destination = shell.Destinations[i];
                if (destination.Id == TabDestinationId.Device)
                {
                    DeviceScreen = new DeviceScreenView(ui, Layout);
                    Tabs.AddScreen(DeviceScreen);
                }
                else if (destination.Id == TabDestinationId.Routines)
                {
                    RoutinesScreen = new RoutinesScreenView(ui, Layout, i);
                    Tabs.AddScreen(RoutinesScreen);
                }
                else
                {
                    Tabs.AddScreen(new PlaceholderScreenView(ui, Layout, destination));
                }
            }

            Add(Tabs);
            TabBar = new TabBarView(ui, shell);
            Add(TabBar);
            Layout.AttachTabBar(TabBar);
            Pages.AttachShell(Tabs, TabBar);
            Add(Pages);
            Add(Dialogs);
            Add(Toasts);
            Add(Popups);
            if (topLayer != null)
            {
                TopLayer = topLayer(ui, Layout, TabBar);
                if (TopLayer != null)
                {
                    Add(TopLayer);
                }
            }

            KeyboardFocus = new FocusHighlight(this);
            this.Bind(ui.App.Accessibility, OnAccessibility);
            Tabs.Start();
        }

        public UiContext Ui { get; }

        public ShellLayout Layout { get; }

        public TabHost Tabs { get; }

        public TabBarView TabBar { get; }

        public PageStack Pages { get; }

        public DialogLayer Dialogs { get; }

        public ToastLayer Toasts { get; }

        public PopupLayer Popups { get; }

        public KeyboardAvoider Keyboard { get; }

        public DeviceScreenView? DeviceScreen { get; }

        public RoutinesScreenView? RoutinesScreen { get; }

        public VisualElement? TopLayer { get; }

        public FocusHighlight KeyboardFocus { get; }

        public string TextScaleClass => _textScaleClass;

        // Algo del shell en movimiento que no es una animación del MotionDriver (regla de toma del arnés).
        public bool IsSettled => !Pages.IsTransitioning && !Dialogs.IsTransitioning && !Toasts.IsTransitioning && !Popups.IsTransitioning;

        // Una vez por cuadro (ShellHandle.Tick): el teclado del teléfono.
        public void Tick()
        {
            if (!_disposed)
            {
                Keyboard.Tick();
            }
        }

        // Cierra las capas (sin animar) y libera sus timers; lo llama ShellHandle.Dispose.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Toasts.Dispose();
            Popups.Dispose();
            Dialogs.Dispose();
            Pages.Dispose();
        }

        // Escala de letra (AC-APP-05-05, P-03): la del sistema cuantizada a 0,85 / 1 / 1,15 / 1,3 → .rm-text-scale-<n>;
        // reducir movimiento (AC-APP-05-08) → .rm-reduce-motion y MotionSettings.
        private void OnAccessibility(AccessibilitySnapshot snapshot)
        {
            var className = TextScaleSteps.ClassName(TextScaleSteps.Quantize(snapshot.TextScale));
            if (className != _textScaleClass)
            {
                if (_textScaleClass.Length > 0)
                {
                    RemoveFromClassList(_textScaleClass);
                }

                _textScaleClass = className;
                AddToClassList(className);
            }

            EnableInClassList(ReduceMotionClass, snapshot.ReduceMotion);
            Ui.Motion.Settings.ReduceMotion = snapshot.ReduceMotion;
        }
    }
}
