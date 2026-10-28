using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Portapapeles de prueba para el menú de texto (spec §0.4, R3-10): ninguna prueba toca el portapapeles real del usuario.
    public sealed class FakeClipboard : IClipboard
    {
        private string _text;

        public FakeClipboard(string text = "")
        {
            _text = text ?? "";
        }

        public bool HasText => _text.Length > 0;

        public string Text
        {
            get => _text;
            set => _text = value ?? "";
        }

        // Veces que la app escribió (Copiar, Cortar).
        public int Writes { get; private set; }

        public void SetText(string text)
        {
            _text = text ?? "";
            Writes++;
        }
    }
}
