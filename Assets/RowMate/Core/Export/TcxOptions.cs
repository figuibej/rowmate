using System;

namespace RowMate.Core.Export
{
    // Opciones del armado del TCX (It. 12; 02-tech-design §2.1, DT12-03, DT12-16): la zona horaria del equipo AL EXPORTAR (cada
    // instante se convierte con su propio desfase) y si es el build del simulador (marca [SIM] en Notes, DV-123).
    public sealed record TcxOptions(TimeZoneInfo Zone, bool Simulator);
}
