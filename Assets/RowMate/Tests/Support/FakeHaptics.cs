using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // La respuesta táctil falsa (A10-06): cuenta las vibraciones pedidas.
    public sealed class FakeHaptics : IHaptics
    {
        public int Confirms { get; private set; }

        public void Confirm()
        {
            Confirms++;
        }
    }
}
