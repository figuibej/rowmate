namespace RowMate.Tests.Support
{
    // Las excepciones Q-03 de la auditoría de textos (It. 14, AC-APP-11-01): los textos del simulador que quedan en español también en
    // inglés. Viven en el soporte de pruebas y no en Core: el build normal no puede llevar ningún texto del simulador (AC-BLE-13-02,
    // BuildOutputInspector). Las pruebas y las tomas los pasan con AuditContext.WithExceptions.
    public static class AuditExceptions
    {
        public static readonly string[] Simulator =
        {
            "Remo simulado", "Pulsómetro simulado", "SIMULADOR", "Hora escena", "Hora real",
        };
    }
}
