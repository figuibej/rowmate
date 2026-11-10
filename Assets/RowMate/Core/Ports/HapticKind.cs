namespace RowMate.Core.Ports
{
    // Los tipos de respuesta táctil (It. 14, DT14-23; ANIMATION_BIBLE §2.12; 00-arte §3.4):
    //   Confirm  "Listo" con el remo (It. 10a) y "Reconectado con {name}" (#51, AC-DEBT-10).
    //   Light    Pausa y Reanudar tocados por el usuario (H-01: CLOCK_TICK).
    //   Medium   cambio de paso de una rutina, no al empezar (H-02: VIRTUAL_KEY).
    //   Success  rutina completada (H-03: CONFIRM en API 30+; antes, VIRTUAL_KEY + 120 ms + VIRTUAL_KEY).
    public enum HapticKind
    {
        Confirm,
        Light,
        Medium,
        Success,
    }
}
