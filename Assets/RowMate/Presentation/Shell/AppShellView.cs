using System;
using RowMate.App.Shell;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Device;
using RowMate.Presentation.Placeholder;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Capa superior opcional (la del simulador, §3.5): la arma quien compone la app (Bootstrap) con el shell ya creado.
    public delegate VisualElement? TopLayerFactory(UiContext ui, ShellLayout layout, VisualElement tabBar);

    // Shell de la app (F-APP-01, F-APP-02; 02-tech-design §3.2; legacy MainShell, main.dart:116-181). Los nombres son contrato:
    //   #app-root .rm-app .rm-text-scale-<n> [.rm-reduce-motion] [.rm-desktop] [.rm-keyboard-nav]   sin padding
    //   ├── #tab-content .rm-tab-content       capa 0 (TabHost: las 4/5 pantallas, construidas una vez)
    //   ├── #tab-bar .rm-tab-bar               capa 1 (absoluta abajo, sobre el contenido)
    //   ├── #page-stack .rm-layer              capa 2 (vacía en la It. 2)
    //   ├── #dialog-layer .rm-layer            capa 3 (vacía)
    //   ├── #toast-layer .rm-layer             capa 4 (vacía)
    //   └── #simulator-layer .rm-layer         capa 5, último hijo: por encima de todo (solo con el define)
    // UI Toolkit dibuja y rinde eventos en orden de árbol: la capa del simulador queda arriba sin depender de ninguna pantalla.
    public sealed class AppShellView : VisualElement
    {
        public const string ReduceMotionClass = "rm-reduce-motion";
        public const string DesktopClass = "rm-desktop";

        private string _textScaleClass = "";

        public AppShellView(UiContext ui, TopLayerFactory? topLayer)
        {
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));
            name = "app-root";
            AddToClassList("rm-app");
            EnableInClassList(DesktopClass, ui.IsDesktop);
            EnableInClassList(MeshColor.GammaPanelClass, ui.GammaPanel);
            pickingMode = PickingMode.Ignore;
            Layout = new ShellLayout(ui.Display);
            var shell = ui.App.Shell;

            Tabs = new TabHost(ui, shell);
            for (var i = 0; i < shell.Destinations.Count; i++)
            {
                var destination = shell.Destinations[i];
                if (destination.Id == TabDestinationId.Device)
                {
                    DeviceScreen = new DeviceScreenView(ui, Layout);
                    Tabs.AddScreen(DeviceScreen);
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
            Add(Layer("page-stack"));
            Add(Layer("dialog-layer"));
            Add(Layer("toast-layer"));
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

        public DeviceScreenView? DeviceScreen { get; }

        public VisualElement? TopLayer { get; }

        public FocusHighlight KeyboardFocus { get; }

        public string TextScaleClass => _textScaleClass;

        private static VisualElement Layer(string name)
        {
            var layer = new VisualElement { name = name };
            layer.AddToClassList("rm-layer");
            layer.pickingMode = PickingMode.Ignore;
            return layer;
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
