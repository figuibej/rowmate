using RowMate.Core.Ports;
using UnityEngine;

namespace RowMate.Platform.Input
{
    // Teclado del teléfono (AC-RUT-11-10, F-APP-15.4; ARCHITECTURE §7.3): TouchScreenKeyboard.visible y el alto de
    // TouchScreenKeyboard.area (px de pantalla desde el borde inferior). En escritorio y en el Editor, siempre oculto.
    // Riesgo R3-13: si en [Device-And] .area no resulta confiable, el alto sale del marco visible de la ventana por JNI
    // (getWindowVisibleDisplayFrame); se decide con el teléfono (NO VERIFICABLE sin módulo).
    public sealed class SoftKeyboard : ISoftKeyboard
    {
        private readonly bool _mobile;

        public SoftKeyboard(bool mobile)
        {
            _mobile = mobile;
        }

        public bool IsVisible => _mobile && TouchScreenKeyboard.isSupported && TouchScreenKeyboard.visible;

        public double OccludedHeightPx => IsVisible ? TouchScreenKeyboard.area.height : 0;
    }
}
