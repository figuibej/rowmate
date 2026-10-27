using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Escala de letra y "reducir movimiento" de prueba (01-po-spec §0.4). Set avisa solo si algo cambió, como el de Platform.
    public sealed class FakeAccessibilitySettings : IAccessibilitySettings
    {
        private readonly ObservableValue<AccessibilitySnapshot> _current;

        public FakeAccessibilitySettings(double textScale = 1.0, bool reduceMotion = false, IAppLogger? logger = null)
        {
            _current = new ObservableValue<AccessibilitySnapshot>(new AccessibilitySnapshot(textScale, reduceMotion),
                logger ?? NullAppLogger.Instance);
            Current = new ReadOnlyObservable<AccessibilitySnapshot>(_current);
        }

        public IReadOnlyObservable<AccessibilitySnapshot> Current { get; }

        public void Set(double textScale, bool reduceMotion)
        {
            _current.Value = new AccessibilitySnapshot(textScale, reduceMotion);
        }
    }
}
