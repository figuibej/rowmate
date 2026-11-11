using System;

namespace RowMate.Core.Export
{
    // Opciones del armado del TCX (It. 12; 02-tech-design §2.1, DT12-03, DT12-16): la zona horaria del equipo AL EXPORTAR (cada
    // instante se convierte con su propio desfase) y si es el build del simulador (marca [SIM] en Notes, DV-123).
    // It. 15 (#73, AC-D15-15, DT15-07): AppVersion = la versión de la app (Application.version, la pasa el composition root) para
    // el <Version> de <Creator>; "1.0.0" por defecto.
    public sealed record TcxOptions(TimeZoneInfo Zone, bool Simulator, string AppVersion = TcxOptions.DefaultAppVersion)
    {
        public const string DefaultAppVersion = "1.0.0";
    }
}
