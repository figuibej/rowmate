using System;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // La línea de error debajo de un campo (RmTextField) o de un par de campos (StepDialog: Min/Seg, Split) (03-rutinas/
    // 04-design-notes §4.8, §5.2; QA-V3-02 y §16.6, punto 4):
    //   .rm-field__error-slot (sin estilo propio, flex-shrink 0) > #<nombre> .rm-field__error   el Label de contrato
    // El texto lógico (Text) cambia en el acto, y con él la clase de error de la caja (en el acto, por clase); la línea se abre
    // de 0 al alto de su texto y funde 0 → 1 en 160 ease.move (RevealMotion), y al corregirse al revés: el texto viejo se ve
    // hasta que termina de cerrarse. Reducir movimiento: en el acto.
    public sealed class ErrorLine
    {
        public const string SlotClass = "rm-field__error-slot";

        private readonly RevealMotion _reveal;

        public ErrorLine(MotionDriver motion, Label label)
        {
            if (motion == null)
            {
                throw new ArgumentNullException(nameof(motion));
            }

            Label = label ?? throw new ArgumentNullException(nameof(label));
            Slot = new VisualElement();
            Slot.AddToClassList(SlotClass);
            Slot.pickingMode = PickingMode.Ignore;
            Slot.Add(label);
            label.style.display = DisplayStyle.None;
            _reveal = new RevealMotion(motion, Slot, label, Tokens.DurFast.Milliseconds);
        }

        public VisualElement Slot { get; }

        public Label Label { get; }

        // El error que vale ("" = sin error), aunque la línea todavía se esté yendo.
        public string Text { get; private set; } = "";

        public bool HasText => Text.Length > 0;

        public bool IsAnimating => _reveal.IsRunning;

        public RevealMotion Reveal => _reveal;

        public void Set(string? text)
        {
            var value = text ?? "";
            if (string.Equals(value, Text, StringComparison.Ordinal))
            {
                return;
            }

            Text = value;
            if (value.Length > 0)
            {
                Label.text = value;
                _reveal.Set(true);
                return;
            }

            _reveal.Set(false, () =>
            {
                if (Text.Length == 0)
                {
                    Label.text = "";
                }
            });
        }

        // La línea entera se ve o no con su fila (el par Min/Seg en el modo por distancia): en el acto, sin cambiar el error.
        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                _reveal.Snap();
            }

            Slot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
