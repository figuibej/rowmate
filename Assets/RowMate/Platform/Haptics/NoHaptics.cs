using RowMate.Core.Ports;

namespace RowMate.Platform.Haptics
{
    // Sin respuesta táctil (Windows, el Editor y el resto de las plataformas sin vibración; A10-06).
    public sealed class NoHaptics : IHaptics
    {
        public static readonly NoHaptics Instance = new NoHaptics();

        private NoHaptics()
        {
        }

        public void Confirm()
        {
        }
    }
}
