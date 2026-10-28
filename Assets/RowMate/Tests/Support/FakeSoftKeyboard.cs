using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Teclado del teléfono simulado (AC-RUT-11-10 en PlayMode, KeyboardAvoiderTests): la prueba lo muestra con un alto en px
    // y lo oculta; KeyboardAvoider lo lee en cada Tick.
    public sealed class FakeSoftKeyboard : ISoftKeyboard
    {
        public bool IsVisible { get; private set; }

        public double OccludedHeightPx { get; private set; }

        public void Show(double occludedHeightPx)
        {
            IsVisible = true;
            OccludedHeightPx = occludedHeightPx < 0 ? 0 : occludedHeightPx;
        }

        public void Hide()
        {
            IsVisible = false;
            OccludedHeightPx = 0;
        }
    }
}
