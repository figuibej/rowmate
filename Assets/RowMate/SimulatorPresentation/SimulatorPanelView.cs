using System;
using RowMate.Core.Design;
using RowMate.Presentation;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Simulator.Panel;
using UnityEngine.UIElements;

namespace RowMate.Simulator.Presentation
{
    // Panel de control del simulador (F-BLE-16, AC-BLE-16-03 a -12; legacy simulator_overlay.dart:88-173). Cada control llama
    // a su método del store y la vista se redibuja con State. La cabecera queda FUERA del desplazamiento: la X está siempre
    // visible (AC-BLE-16-03; en el legacy se desplazaba con el resto). Textos fijos en español (Q-03).
    //   #sim-panel .rm-sim-panel
    //   ├── .rm-sim-panel__header: "Simulador" .rm-type-title-s + #sim-close (IconButton close)
    //   └── #sim-panel-scroll (ScrollView) > .rm-sim-panel__content
    //       ├── .rm-sim-presets > #sim-preset-suave / -medio / -fuerte .rm-chip
    //       ├── #sim-watts-row, #sim-spm-row, #sim-hour-row .rm-sim-stepper (rótulo, −, valor .rm-type-num-stat, +)
    //       ├── #sim-hour-real (texto, solo con la hora forzada), #sim-rowing (SwitchToggle "Remando")
    //       └── #sim-drop-rower (link_off), #sim-drop-hrm (heart_broken)
    public sealed class SimulatorPanelView : VisualElement
    {
        private readonly SimulatorPanelStore _store;
        private readonly Label _watts;
        private readonly Label _spm;
        private readonly Label _hour;
        private readonly RmButton _realHour;
        private readonly SwitchToggle _rowing;

        public SimulatorPanelView(UiContext ui, SimulatorPanelStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            name = "sim-panel";
            AddToClassList("rm-sim-panel");

            var header = new VisualElement();
            header.AddToClassList("rm-sim-panel__header");
            header.pickingMode = PickingMode.Ignore;
            var title = new Label(SimulatorTexts.Title);
            title.AddToClassList("rm-sim-panel__title");
            title.AddToClassList("rm-type-title-s");
            title.pickingMode = PickingMode.Ignore;
            header.Add(title);
            Close = new IconButton("sim-close", Icons.Close, store.Close);
            header.Add(Close);
            Add(header);

            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "sim-panel-scroll" };
            Scroll.AddToClassList("rm-scroll");
            ScrollPolicy.Apply(Scroll, ui.IsDesktop);
            var content = Scroll.contentContainer;
            content.AddToClassList("rm-sim-panel__content");
            Add(Scroll);

            var presets = new VisualElement();
            presets.AddToClassList("rm-sim-presets");
            presets.pickingMode = PickingMode.Ignore;
            foreach (var preset in SimulatorPanelStore.Presets)
            {
                var chosen = preset;
                presets.Add(Chip(chosen.ElementName, chosen.Name, () => _store.ApplyPreset(chosen)));
            }

            content.Add(presets);
            content.Add(Stepper("sim-watts", SimulatorTexts.Watts, store.WattsMinus, store.WattsPlus, out _watts));
            content.Add(Stepper("sim-spm", SimulatorTexts.Spm, store.SpmMinus, store.SpmPlus, out _spm));
            content.Add(Stepper("sim-hour", SimulatorTexts.SceneHour, store.HourMinus, store.HourPlus, out _hour));
            _realHour = new RmButton("sim-hour-real", "text", null, SimulatorTexts.RealHour, store.RealHour);
            content.Add(_realHour);
            _rowing = new SwitchToggle("sim-rowing", SimulatorTexts.Rowing);
            _rowing.RegisterValueChangedCallback(evt => _store.SetRowing(evt.newValue));
            content.Add(_rowing);
            content.Add(new RmButton("sim-drop-rower", "outline", Icons.LinkOff, SimulatorTexts.SimulateDisconnect, store.SimulateRowerDrop));
            content.Add(new RmButton("sim-drop-hrm", "outline", Icons.HeartBroken, SimulatorTexts.SimulateHeartRateDrop, store.SimulateHeartRateDrop));

            this.Bind(store.State, Render);
        }

        public IconButton Close { get; }

        public ScrollView Scroll { get; }

        public SwitchToggle Rowing => _rowing;

        private void Render(SimulatorPanelState state)
        {
            Set(_watts, state.WattsText);
            Set(_spm, state.SpmText);
            Set(_hour, state.HourText);
            _realHour.style.display = state.ShowRealHourButton ? DisplayStyle.Flex : DisplayStyle.None;
            if (_rowing.value != state.Rowing)
            {
                _rowing.SetValueWithoutNotify(state.Rowing);
            }
        }

        private static void Set(Label label, string text)
        {
            if (label.text != text)
            {
                label.text = text;
            }
        }

        private static Button Chip(string name, string text, Action onClick)
        {
            var chip = new Button(onClick) { name = name, text = "" };
            RmButton.StripUnityChrome(chip);
            chip.AddToClassList("rm-chip");
            chip.focusable = true;
            chip.Add(RmButton.StateLayer());
            var label = new Label(text);
            label.AddToClassList("rm-chip__label");
            label.AddToClassList("rm-type-button");
            label.pickingMode = PickingMode.Ignore;
            chip.Add(label);
            chip.Add(RmButton.FocusRing());
            return chip;
        }

        private static VisualElement Stepper(string id, string text, Action minus, Action plus, out Label value)
        {
            var row = new VisualElement { name = id + "-row" };
            row.AddToClassList("rm-sim-stepper");
            row.pickingMode = PickingMode.Ignore;
            var label = new Label(text);
            label.AddToClassList("rm-sim-stepper__label");
            label.AddToClassList("rm-type-body-m");
            label.pickingMode = PickingMode.Ignore;
            row.Add(label);
            row.Add(new IconButton(id + "-minus", Icons.Remove, minus));
            value = new Label { name = id + "-value" };
            value.AddToClassList("rm-sim-stepper__value");
            value.AddToClassList("rm-type-num-stat");
            value.pickingMode = PickingMode.Ignore;
            row.Add(value);
            row.Add(new IconButton(id + "-plus", Icons.Add, plus));
            return row;
        }
    }
}
