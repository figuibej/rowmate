namespace RowMate.Core.Design
{
    // Números del botón y los avisos de "Exportar TCX" (It. 12; 01-po-spec AC-STR-09-18, -19, -21; 02-tech-design DT12-12, A12-03).
    // Milisegundos y dp. Lo que ya es token no se repite.
    public static class ExportMotionSpecs
    {
        // El indicador se ve al menos 300 ms para que no parpadee (AC-STR-09-18).
        public const int MinBusyMs = 300;

        // El aviso de éxito o de error dura 6 s (AC-STR-09-19, -21).
        public const int NoticeMs = 6000;

        // El indicador circular del legacy al subir (session_detail_screen.dart:95-106): 18 dp con trazo de 2.
        public const double SpinnerDp = 18;
        public const double StrokeDp = 2;
    }
}
