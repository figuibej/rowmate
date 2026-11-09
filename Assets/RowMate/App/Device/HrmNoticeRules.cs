namespace RowMate.App.Device
{
    // Cuándo se muestra un aviso del pulsómetro (It. 11, DT11-12, DT11-13; AC-HRM-07-05, AC-HRM-08-03).
    public static class HrmNoticeRules
    {
        // Connected sale en cualquier pestaña (también Dispositivo, CAP-12); Lost solo con el entrenamiento a la vista (el mismo
        // predicado que el aviso del remo).
        public static bool ShouldShow(HrmNoticeKind kind, bool workoutNoticesVisible)
        {
            return kind == HrmNoticeKind.Connected || workoutNoticesVisible;
        }

        // QA-G11-03: el aviso del pulsómetro cede SOLO ante uno del remo, a la vista o fijo pendiente (se descarta). Reemplaza uno
        // anterior del pulsómetro (Connected pisa a Lost) o uno común.
        public static bool Admit(ToastSlot visible, bool rowerStickyPending)
        {
            return visible != ToastSlot.Rower && !rowerStickyPending;
        }
    }
}
