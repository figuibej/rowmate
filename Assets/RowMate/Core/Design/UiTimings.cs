namespace RowMate.Core.Design
{
    // Tiempos de comportamiento (no de animación) que el legacy toma de Flutter, en milisegundos. Los mira el reloj del
    // scheduler (IScheduler): en pruebas, FakeScheduler.
    public static class UiTimings
    {
        // Aviso visible 4 s y se va solo (AC-APP-03-05): <flutter-sdk>/packages/flutter/lib/src/material/snack_bar.dart:29.
        // Uno nuevo reemplaza al visible y cuenta sus 4 s desde ahí (DV-14).
        public const int ToastVisibleMs = 4000;

        // Ayudas emergentes (AC-APP-03-06; <flutter-sdk>/packages/flutter/lib/src/material/tooltip.dart:392-394): en la PC
        // aparecen apenas el puntero se apoya (espera 0) y se van 0,1 s después de salir; en el teléfono, al mantener
        // presionado, quedan 1,5 s después de soltar.
        public const int TooltipWaitMs = 0;
        public const int TooltipExitMs = 100;
        public const int TooltipShowAfterLongPressMs = 1500;

        // Mantener presionado (kLongPressTimeout, <flutter-sdk>/packages/flutter/lib/src/gestures/constants.dart:29): la ayuda
        // en el teléfono y el menú de texto en modo táctil (R3-13).
        public const int LongPressMs = 500;
    }
}
