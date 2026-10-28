using RowMate.Core.Ports;
using UnityEngine;

namespace RowMate.Platform.Input
{
    // Portapapeles del sistema para el menú del clic derecho de los campos (AC-RUT-11-09, P-06; 03-rutinas/02-tech-design
    // §4.10): GUIUtility.systemCopyBuffer, el mismo que usan Ctrl+C, Ctrl+X y Ctrl+V del TextField de UI Toolkit. Ninguna
    // prueba lo usa (FakeClipboard, R3-10).
    public sealed class SystemClipboard : IClipboard
    {
        public bool HasText => !string.IsNullOrEmpty(GUIUtility.systemCopyBuffer);

        public string Text => GUIUtility.systemCopyBuffer ?? "";

        public void SetText(string text)
        {
            GUIUtility.systemCopyBuffer = text ?? "";
        }
    }
}
