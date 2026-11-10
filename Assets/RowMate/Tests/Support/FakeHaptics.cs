using System.Collections.Generic;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // La respuesta táctil falsa (A10-06; It. 14, DT14-23): registra cada pedido en orden y cuenta los Confirm.
    public sealed class FakeHaptics : IHaptics
    {
        private readonly List<HapticKind> _kinds = new List<HapticKind>();

        public int Confirms { get; private set; }

        public IReadOnlyList<HapticKind> Kinds => _kinds;

        public void Confirm()
        {
            Play(HapticKind.Confirm);
        }

        public void Play(HapticKind kind)
        {
            _kinds.Add(kind);
            if (kind == HapticKind.Confirm)
            {
                Confirms++;
            }
        }
    }
}
