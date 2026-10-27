using RowMate.Core.Common;

namespace RowMate.Core.Ports
{
    // Escala de letra y "reducir movimiento" del sistema (ARCHITECTURE §13.2). Lo implementa Platform (It. 2: Android por
    // JNI, Windows por P/Invoke y registro, Editor por menú); en los tests, FakeAccessibilitySettings.
    public interface IAccessibilitySettings
    {
        // Se vuelve a leer al recuperar el foco; avisa solo si algo cambió.
        IReadOnlyObservable<AccessibilitySnapshot> Current { get; }
    }
}
