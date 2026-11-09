namespace RowMate.Core.Ports
{
    // Cómo entrega el archivo la plataforma (It. 12; 02-tech-design §3.1, DT12-07): None = no hay exportación (sin botón: iOS
    // hasta la 10b, dotnet y las pruebas que no la miran), SaveToFolder = una carpeta visible con aviso y "Abrir carpeta"
    // (Windows), Share = la hoja de compartir del sistema, sin aviso de éxito (Android).
    public enum ExportDelivery
    {
        None,
        SaveToFolder,
        Share,
    }
}
