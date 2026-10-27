using System.Collections.Generic;
using RowMate.App.Shell;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Barra de pestañas (AC-APP-02-01 a -05; ART_BIBLE §3.11; 04-design-notes §4.2, §5.4; legacy NavigationBar de main.dart).
    //   #tab-bar .rm-tab-bar                          absoluta abajo; alto 64 + área segura inferior (ShellLayout)
    //   ├── .rm-tab-bar__sheen                        brillo superior (DN-06), primer hijo, pickingMode Ignore
    //   ├── #tab-bar-pill .rm-tab-bar__pill           56 × 32, x por código (CriticalSpring ω 22)
    //   └── #tab-<destino> .rm-tab [.rm-tab--selected] (Button) × 4 o 5
    //       ├── .rm-tab__icon-box (56 × 32) > .rm-tab__state + .rm-tab__icon .rm-icon + .rm-focus-ring
    //       └── .rm-tab__label .rm-type-label
    // Tocar el elegido no hace nada (ShellStore). Al cambiar (cuadro 0): el glifo del nuevo pasa a su variante elegida (y
    // .rm-icon--filled en Workout y Perfil); la píldora va al nuevo con el resorte, conservando su velocidad; el ícono nuevo
    // escala 1 → 1,12 (0-90) → 1 (90-260); el color del ícono y la etiqueta funde en 160 ms ease.move por código y al
    // terminar se borra el estilo en línea (en reposo, los colores son de Base.uss) (DN-11). Reducir movimiento: la píldora
    // en el destino en el cuadro 0, sin escala; los colores funden igual.
    public sealed class TabBarView : VisualElement, IMotion
    {
        private const float PillW = 56;

        private readonly UiContext _ui;
        private readonly ShellStore _shell;
        private readonly List<Tab> _tabs = new List<Tab>();
        private readonly VisualElement _pill;
        private CriticalSpring _pillSpring;
        private double _pillTarget;
        private bool _pillPlaced;
        private int _selected = -1;
        private int _previous = -1;
        private double _changeStart = double.NegativeInfinity;
        private double _lastTick = double.NaN;
        private bool _animating;
        private bool _reduced;

        public TabBarView(UiContext ui, ShellStore shell)
        {
            _ui = ui;
            _shell = shell;
            name = "tab-bar";
            AddToClassList("rm-tab-bar");
            var sheen = new VisualElement();
            sheen.AddToClassList("rm-tab-bar__sheen");
            sheen.pickingMode = PickingMode.Ignore;
            if (ui.Fade != null)
            {
                sheen.style.backgroundImage = new StyleBackground(ui.Fade);
            }

            Add(sheen);
            _pill = new VisualElement { name = "tab-bar-pill" };
            _pill.AddToClassList("rm-tab-bar__pill");
            _pill.pickingMode = PickingMode.Ignore;
            Add(_pill);
            for (var i = 0; i < shell.Destinations.Count; i++)
            {
                var tab = new Tab(this, i, shell.Destinations[i]);
                _tabs.Add(tab);
                Add(tab.Button);
            }

            RegisterCallback<GeometryChangedEvent>(_ => PlacePill(true));
            this.Bind(ui.App.Texts, OnTexts);
            this.Bind(shell.SelectedIndex, OnSelected);
        }

        public bool IsFinite => true;

        public int Selected => _selected;

        public VisualElement Pill => _pill;

        public Button TabButton(int index)
        {
            return _tabs[index].Button;
        }

        public bool Tick(double nowSeconds)
        {
            var dt = double.IsNaN(_lastTick) ? 0 : nowSeconds - _lastTick;
            _lastTick = nowSeconds;
            var elapsed = (nowSeconds - _changeStart) * 1000.0;
            var colorsDone = ApplyColors(elapsed);
            var bumpDone = ApplyBump(elapsed);
            _pillSpring.Step(dt, _pillTarget, MotionSpecs.PillOmega);
            var pillDone = _pillSpring.IsSettled(_pillTarget, 0.05);
            if (pillDone)
            {
                _pillSpring.SnapTo(_pillTarget);
            }

            _pill.style.left = (float)_pillSpring.Position;
            _animating = !(colorsDone && bumpDone && pillDone);
            if (!_animating)
            {
                _lastTick = double.NaN;
                ClearInlineColors();
            }

            return _animating;
        }

        private void OnTexts(Localizer texts)
        {
            for (var i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].Label.text = texts.Get(_tabs[i].Destination.LabelKey);
            }
        }

        private void OnSelected(int index)
        {
            if (index == _selected)
            {
                return;
            }

            _previous = _selected;
            _selected = index;
            for (var i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].SetSelected(i == index);
            }

            var animate = _previous >= 0 && _ui.Motion.Armed;
            if (!animate)
            {
                ClearInlineColors();
                PlacePill(true);
                return;
            }

            _reduced = _ui.Motion.Settings.ReduceMotion;
            _changeStart = _ui.Motion.Now;
            _lastTick = double.NaN;
            PlacePill(_reduced);
            _animating = true;
            ApplyColors(0);
            ApplyBump(0);
            _ui.Motion.Add(this);
        }

        // Posición x de la píldora para el destino elegido (centrada en la caja del ícono).
        private void PlacePill(bool snap)
        {
            if (_selected < 0 || _selected >= _tabs.Count)
            {
                return;
            }

            var button = _tabs[_selected].Button;
            if (!(button.layout.width > 0))
            {
                return;
            }

            _pillTarget = button.layout.x + (button.layout.width - PillW) / 2;
            if (snap || !_pillPlaced)
            {
                _pillSpring.SnapTo(_pillTarget);
                _pill.style.left = (float)_pillTarget;
            }

            _pillPlaced = true;
        }

        private bool ApplyColors(double elapsedMs)
        {
            var u = elapsedMs / Tokens.DurFast.Milliseconds;
            var e = Easing.Evaluate(Tokens.EaseMove, u < 0 ? 0 : u > 1 ? 1 : u);
            var off = TokenColors.ToColor(Tokens.TextSecondary);
            var on = TokenColors.ToColor(Tokens.AccentPrimary);
            if (_selected >= 0)
            {
                _tabs[_selected].SetColor(TokenColors.Lerp(off, on, e));
            }

            if (_previous >= 0 && _previous != _selected)
            {
                _tabs[_previous].SetColor(TokenColors.Lerp(on, off, e));
            }

            return u >= 1;
        }

        private bool ApplyBump(double elapsedMs)
        {
            if (_selected < 0)
            {
                return true;
            }

            var icon = _tabs[_selected].Icon;
            if (_reduced || elapsedMs >= MotionSpecs.TabIconEndMs)
            {
                icon.style.scale = StyleKeyword.Null;
                return true;
            }

            double s;
            if (elapsedMs < MotionSpecs.TabIconPeakMs)
            {
                s = 1 + (MotionSpecs.TabIconBump - 1) * Easing.Evaluate(Tokens.EaseEnter, elapsedMs / MotionSpecs.TabIconPeakMs);
            }
            else
            {
                var u = (elapsedMs - MotionSpecs.TabIconPeakMs) / (MotionSpecs.TabIconEndMs - MotionSpecs.TabIconPeakMs);
                s = MotionSpecs.TabIconBump - (MotionSpecs.TabIconBump - 1) * Easing.Evaluate(Tokens.EaseMove, u);
            }

            icon.style.scale = new Scale(new Vector2((float)s, (float)s));
            return false;
        }

        private void ClearInlineColors()
        {
            for (var i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].ClearColor();
                _tabs[i].Icon.style.scale = StyleKeyword.Null;
            }
        }

        private sealed class Tab
        {
            public Tab(TabBarView bar, int index, TabDestination destination)
            {
                Destination = destination;
                Button = new Button(() => bar._shell.Select(index)) { name = destination.ElementName, text = "" };
                RmButton.StripUnityChrome(Button);
                Button.AddToClassList("rm-tab");
                Button.focusable = true;
                var box = new VisualElement();
                box.AddToClassList("rm-tab__icon-box");
                box.pickingMode = PickingMode.Ignore;
                box.Add(RmButton.StateLayer("rm-tab__state"));
                Icon = new IconView(destination.IconFor(false), destination.FilledFor(false), 24);
                Icon.AddToClassList("rm-tab__icon");
                box.Add(Icon);
                box.Add(RmButton.FocusRing());
                Button.Add(box);
                Label = new Label();
                Label.AddToClassList("rm-tab__label");
                Label.AddToClassList("rm-type-label");
                Label.pickingMode = PickingMode.Ignore;
                Button.Add(Label);
            }

            public TabDestination Destination { get; }

            public Button Button { get; }

            public IconView Icon { get; }

            public Label Label { get; }

            public void SetSelected(bool selected)
            {
                Button.EnableInClassList("rm-tab--selected", selected);
                Icon.SetGlyph(Destination.IconFor(selected), Destination.FilledFor(selected));
            }

            public void SetColor(Color color)
            {
                Icon.style.color = color;
                Label.style.color = color;
            }

            public void ClearColor()
            {
                Icon.style.color = StyleKeyword.Null;
                Label.style.color = StyleKeyword.Null;
            }
        }
    }
}
