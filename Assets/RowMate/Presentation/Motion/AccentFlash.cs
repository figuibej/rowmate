using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Destello del acento de una tarjeta del tablero (04-design-notes §5.2, DN-07, AC-BLE-11-06): solo cuando el TEXTO del
    // valor cambió. k = AccentPulse.K(t): el borde superior va en el color de la métrica con alfa k (1 → 0,60 en 300 ms con
    // ease.pulse) y el brillo con opacidad 0,14·k. En reposo k = 0,60 (legacy withOpacity(0.6), DV-02). El color en línea es
    // contrato (02-tech-design §3.12 regla 5). Reducir movimiento: igual (es opacidad).
    public sealed class AccentFlash : TimedMotion
    {
        private readonly VisualElement _card;
        private readonly VisualElement? _glow;
        private ColorToken _color;

        public AccentFlash(MotionDriver driver, VisualElement card, VisualElement? glow, ColorToken color)
            : base(driver)
        {
            _card = card;
            _glow = glow;
            _color = color;
            ApplyK(AccentPulse.RestK);
        }

        public double CurrentK { get; private set; } = AccentPulse.RestK;

        // La tarjeta del destello (la regla de toma de la It. 4b no cuenta las animaciones de lo que no se ve: Dispositivo tapado por el
        // inmersivo).
        public VisualElement Card => _card;

        protected override double TotalMs => MotionSpecs.AccentFlashMs;

        public void SetColor(ColorToken color)
        {
            _color = color;
            ApplyK(CurrentK);
        }

        // Una lectura que cambió el texto: k vuelve a 1 y baja (si estaba corriendo, reinicia).
        public void Flash()
        {
            if (!Driver.Armed)
            {
                ApplyK(AccentPulse.RestK);
                return;
            }

            Begin();
        }

        public void Rest()
        {
            Finish();
            ApplyK(AccentPulse.RestK);
        }

        protected override void OnUpdate(double elapsedMs)
        {
            ApplyK(AccentPulse.K(elapsedMs >= TotalMs ? TotalMs : elapsedMs));
        }

        private void ApplyK(double k)
        {
            CurrentK = k;
            _card.style.borderTopColor = TokenColors.WithAlpha(_color, k);
            if (_glow != null)
            {
                _glow.style.opacity = (float)(MotionSpecs.AccentGlowPeak * k);
            }
        }
    }
}
