using System;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Botón de la app (02-tech-design §3.7; 04-design-notes §4.7, DN-04, DN-05, DN-20). Hijos de contrato, en este orden:
    //   .rm-button__state  capa de estado (hover/presión) absoluta, inset 0, pickingMode Ignore
    //   .rm-button__icon   ícono (IconView 18) o, ocupado, .rm-spinner (16)
    //   .rm-button__label  .rm-type-button, white-space: normal, centrada (a 1,3× pasa a dos líneas y el botón crece)
    //   .rm-focus-ring > .rm-focus-ring__halo   absolutos, no ocupan lugar (visibles solo con .rm-keyboard-nav)
    // Variante: --primary, --tonal, --outline, --text (y --danger sobre --text), --danger-fill (relleno state.dangerFill con
    // texto blanco: Eliminar del diálogo, It. 3). Alto de contrato como min-height (USS). Desde la It. 3 (DN3-17) el tamaño
    // del ícono va por parámetro (18 por defecto; 16 en el pie de la serie) y la clase de texto también (.rm-type-button por
    // defecto; .rm-type-button-s en el pie).
    // Deshabilitado = SetEnabled(false): :disabled, opacidad 0,38 y sin respuesta (AC-HRM-13-04). Enter (NavigationSubmit)
    // activa por Button; Espacio lo agrega este control (AC-APP-05-07).
    public class RmButton : Button
    {
        public const string UssClass = "rm-button";
        public const string StateClass = "rm-button__state";
        public const string IconClass = "rm-button__icon";
        public const string LabelClass = "rm-button__label";

        public const float DefaultIconDp = 18;
        public const string DefaultTypeClass = "rm-type-button";

        private readonly VisualElement _iconSlot;
        private readonly float _iconDp;
        private IconView? _icon;
        private Spinner? _spinner;

        public RmButton(string name, string variant, string? iconCodepoint, string label, Action? onClick, float iconDp = DefaultIconDp,
                        string typeClass = DefaultTypeClass)
        {
            _iconDp = iconDp;
            this.name = name;
            text = "";
            StripUnityChrome(this);
            AddToClassList(UssClass);
            if (!string.IsNullOrEmpty(variant))
            {
                foreach (var part in variant.Split(' '))
                {
                    if (part.Length > 0)
                    {
                        AddToClassList(UssClass + "--" + part);
                    }
                }
            }

            focusable = true;
            style.flexShrink = 0;
            Add(StateLayer());
            _iconSlot = new VisualElement { name = "" };
            _iconSlot.AddToClassList(IconClass);
            _iconSlot.pickingMode = PickingMode.Ignore;
            Add(_iconSlot);
            if (!string.IsNullOrEmpty(iconCodepoint))
            {
                SetIcon(iconCodepoint);
            }
            else
            {
                _iconSlot.style.display = DisplayStyle.None;
            }

            Label = new Label(label);
            Label.AddToClassList(LabelClass);
            Label.AddToClassList(string.IsNullOrEmpty(typeClass) ? DefaultTypeClass : typeClass);
            Label.pickingMode = PickingMode.Ignore;
            Add(Label);
            Add(FocusRing());
            if (onClick != null)
            {
                clicked += onClick;
            }

            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public Label Label { get; }

        public IconView? Icon => _icon;

        public bool IsBusy => _spinner != null && _spinner.IsShown;

        public void SetLabel(string label)
        {
            if (Label.text != label)
            {
                Label.text = label;
            }
        }

        public void SetIcon(string? codepoint)
        {
            if (string.IsNullOrEmpty(codepoint))
            {
                _iconSlot.style.display = DisplayStyle.None;
                return;
            }

            _iconSlot.style.display = DisplayStyle.Flex;
            // Los glifos que solo están en la fuente rellena (play_arrow, pause: Icons) van con ella; si no, la de contorno.
            var filled = FilledOnly(codepoint!);
            if (_icon == null)
            {
                _icon = new IconView(codepoint!, filled, _iconDp);
                _iconSlot.Add(_icon);
            }
            else
            {
                _icon.SetGlyph(codepoint!, filled);
            }
        }

        private static bool FilledOnly(string codepoint)
        {
            foreach (var icon in Core.Design.Icons.All)
            {
                if (icon.Glyph == codepoint)
                {
                    return !icon.InOutline && icon.InFilled;
                }
            }

            return false;
        }

        // Ocupado: el spinner reemplaza al ícono (#device-search buscando, AC-BLE-05-01).
        public void SetBusy(bool busy, Motion.MotionDriver? motion)
        {
            if (busy)
            {
                if (_spinner == null)
                {
                    _spinner = new Spinner(motion);
                    _iconSlot.Add(_spinner);
                }

                _spinner.SetShown(true);
                if (_icon != null)
                {
                    _icon.style.display = DisplayStyle.None;
                }

                _iconSlot.style.display = DisplayStyle.Flex;
                return;
            }

            if (_spinner != null)
            {
                _spinner.SetShown(false);
            }

            if (_icon != null)
            {
                _icon.style.display = DisplayStyle.Flex;
            }
        }

        // Saca la clase .unity-button: el aspecto del botón de Unity (fondo gris, borde, :hover/:active con más especificidad que
        // las reglas de la app) no aplica a los controles de RowMate (en el arnés, un ± quedaba con un círculo gris en :active).
        public static void StripUnityChrome(Button button)
        {
            button.RemoveFromClassList(Button.ussClassName);
        }

        public static VisualElement StateLayer(string className = StateClass)
        {
            var state = new VisualElement();
            state.AddToClassList(className);
            state.pickingMode = PickingMode.Ignore;
            return state;
        }

        public static VisualElement FocusRing()
        {
            var ring = new VisualElement();
            ring.AddToClassList("rm-focus-ring");
            ring.pickingMode = PickingMode.Ignore;
            var halo = new VisualElement();
            halo.AddToClassList("rm-focus-ring__halo");
            halo.pickingMode = PickingMode.Ignore;
            ring.Add(halo);
            return ring;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != UnityEngine.KeyCode.Space || !enabledInHierarchy)
            {
                return;
            }

            evt.StopPropagation();
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = this;
                SendEvent(submit);
            }
        }
    }
}
