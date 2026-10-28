namespace RowMate.Core.Ports
{
    // Portapapeles del sistema para el menú del clic derecho de los campos (AC-RUT-11-09, P-06). Lo implementa Platform
    // (SystemClipboard, sobre el mismo portapapeles que usan Ctrl+C/X/V de UI Toolkit); en pruebas, FakeClipboard: ninguna
    // prueba toca el portapapeles real del usuario (R3-10).
    public interface IClipboard
    {
        // Hay texto para pegar (habilita "Pegar").
        bool HasText { get; }

        // El texto actual ("" si no hay).
        string Text { get; }

        void SetText(string text);
    }
}
