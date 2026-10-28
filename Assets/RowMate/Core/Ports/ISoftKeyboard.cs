namespace RowMate.Core.Ports
{
    // Teclado del teléfono (AC-RUT-11-10, F-APP-15.4; ARCHITECTURE §7.3). Lo lee KeyboardAvoider en cada Tick para llevar el
    // campo con foco por encima del teclado. Lo implementa Platform (SoftKeyboard: TouchScreenKeyboard en Android/iOS; en
    // escritorio, siempre oculto); en pruebas, FakeSoftKeyboard.
    public interface ISoftKeyboard
    {
        bool IsVisible { get; }

        // Alto en px de pantalla que el teclado tapa desde el borde inferior (0 si está oculto).
        double OccludedHeightPx { get; }
    }
}
