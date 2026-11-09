namespace RowMate.Core.Ports
{
    // La carpeta de la exportación (It. 12; 02-tech-design §2.5, DT12-10): Directory es la ruta real y NUNCA se muestra ni se
    // registra tal cual; FixedLabel es la etiqueta que se muestra (null = la carpeta Documentos del usuario, que se nombra con
    // exportTcxDocuments en el idioma de la app: ExportFolderLabel).
    public sealed record ExportLocation(string Directory, string? FixedLabel);
}
