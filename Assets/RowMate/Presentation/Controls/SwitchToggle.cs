using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Interruptor de renglón (DN-17; = SwitchListTile del legacy, simulator_overlay.dart:151): el Toggle de Unity con el
    // checkmark oculto y .rm-switch__track > .rm-switch__thumb dentro de su .unity-toggle__input, a la derecha; el rótulo es
    // el label del Toggle (.rm-type-body-m), a la izquierda. El Toggle registra el clic en todo el renglón, así que tocar el
    // rótulo o la pista lo cambia. El aspecto (:checked) y sus transiciones son de WP-D.
    public sealed class SwitchToggle : Toggle
    {
        public const string UssClass = "rm-switch-row";

        public SwitchToggle(string name, string label)
            : base(label)
        {
            this.name = name;
            AddToClassList(UssClass);
            focusable = true;
            style.flexShrink = 0;
            labelElement.AddToClassList("rm-switch-row__label");
            labelElement.AddToClassList("rm-type-body-m");
            var input = this.Q(className: inputUssClassName);
            var checkmark = this.Q(className: checkmarkUssClassName);
            if (checkmark != null)
            {
                checkmark.style.display = DisplayStyle.None;
            }

            Track = new VisualElement();
            Track.AddToClassList("rm-switch__track");
            Track.pickingMode = PickingMode.Ignore;
            Thumb = new VisualElement();
            Thumb.AddToClassList("rm-switch__thumb");
            Thumb.pickingMode = PickingMode.Ignore;
            Track.Add(Thumb);
            // La pista va como hijo directo del renglón, absoluta a la derecha (no depende de la estructura interna del Toggle);
            // el input de Unity (checkmark) se oculta: el Toggle registra el clic en todo el renglón.
            if (input != null)
            {
                input.style.display = DisplayStyle.None;
            }

            Add(Track);
            Add(RmButton.FocusRing());
        }

        public VisualElement Track { get; }

        public VisualElement Thumb { get; }
    }
}
