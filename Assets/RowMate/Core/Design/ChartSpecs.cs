namespace RowMate.Core.Design
{
    // Medidas y tiempos del gráfico del detalle (It. 9; 00-arte-historial §3, §4, §5 y A9-08; 02-tech-design §2.9, §2.10, §6).
    // Como MotionSpecs: los números que no son tokens. Los colores salen de Tokens (ningún token nuevo, A9-08) y las curvas de
    // Tokens.Ease*. dp y ms. Sin la permanencia de 1 500 ms del cursor (A9-03: al soltar se va, AC-HIS-13-07).
    public static class ChartSpecs
    {
        // ── Marco (arte §3; AC-HIS-09-03: 220 en todos los tamaños, §10.2) ──
        public const double Height = 220;
        public const double Padding = 12;

        // Columna fija del eje Y (36 para todas las métricas) y aire de las etiquetas Y al área.
        public const double YAxisColumn = 36;
        public const double YLabelGap = 6;

        // Margen derecho del área (la última etiqueta X se alinea adentro).
        public const double RightMargin = 4;

        // Encima del área: la tira de tipo (3 dp, stroke.accent) y su aire hasta el área: 6 en total.
        public const double TypeStripHeight = 3;
        public const double TypeStripSpace = 6;

        // Hueco entre las tiras de dos franjas (1 de cada lado).
        public const double TypeStripGap = 2;

        // Debajo del área: las etiquetas X (14 + 4).
        public const double XLabelBand = 18;

        // ── Escala Y (DT9-09): fl_chart con el área del legacy, 220 − 24 de las etiquetas X = 196, y 40 px por división ──
        public const double LegacyYAxisViewSize = 196;
        public const double LegacyPixelsPerInterval = 40;

        // maxY · 1,1 o 100 si no es positivo (sd:401-402).
        public const double YHeadroom = 1.1;
        public const double EmptyYMax = 100;

        // ── Trazos (arte §4; AC-HIS-13-06, -14-03, -15-02) ──
        public const double LineWidth = 2;
        public const double MiterLimit = 2;
        public const double SeparatorWidth = 1;
        public const double SeparatorDash = 4;
        public const double SeparatorGap = 4;
        public const double TargetWidth = 2;
        public const double TargetDash = 8;
        public const double TargetGap = 5;
        public const double GridWidth = 1;
        public const double SinglePointDiameter = 6;

        // Alfas (0-1) sobre el color base: franja 10 %, grilla 8 %, separador 24 %, objetivo 78 % (blanco @200/255 del legacy),
        // área del objetivo 7 % (@18/255) y área de la serie: degradé de 24 % arriba del área a 0 en la base (DV-91).
        public const double BandAlpha = 0.10;
        public const double GridAlpha = 0.08;
        public const double SeparatorAlpha = 0.24;
        public const double TargetAlpha = 0.78;
        public const double TargetAreaAlpha = 0.07;
        public const double SeriesAreaTopAlpha = 0.24;

        // ── Reducción y suavizado (DT9-10; arte §7.2) ──
        // Mínimo y máximo por columna de 1 dp, hasta MaxColumns columnas (con más ancho, las columnas se ensanchan: así el
        // fundido cruzado de dos curvas entra en la capacidad fija también en W1280; desvío anotado en WP-A).
        public const double ColumnDp = 1;
        public const int MaxColumns = 630;

        // Cúbica de fl_chart (curveSmoothness 0,2, sin preventCurveOverShooting) solo con 3 dp o más entre puntos, en
        // ⌈separación / 3⌉ partes hasta 8.
        public const double CurveSmoothness = 0.2;
        public const double SmoothMinSpacing = 3;
        public const int SmoothMaxParts = 8;

        // ── Cursor y tooltip (arte §5.4, §5.5; A9-03) ──
        public const double CursorGuideAlpha = 0.40;
        public const double CursorDotDiameter = 8;
        public const double CursorDotRing = 2;
        public const double CursorOmega = CriticalSpring.SnappyOmega;
        public const double GestureSlop = 6;
        public const double TooltipOffset = 10;
        public const double TooltipEdge = 4;
        public const double TooltipMinWidth = 64;

        // ── Tiempos (ANIMATION_BIBLE §2.12; arte §5.1, §5.6; DT9-11) ──
        // Entrada: marco, franjas, grilla y etiquetas 0-200 (ease.enter); revelado 100-700 (ease.enter); área 150-450.
        public const double EntryFrameEndMs = 200;
        public const double EntryRevealStartMs = 100;
        public const double EntryRevealEndMs = 700;
        public const double EntryAreaStartMs = 150;
        public const double EntryAreaEndMs = 450;

        // Cambio de métrica: fundido cruzado 220 (lineal) y yMax 300 (ease.move); etiquetas y grilla 0-120 / 100-220; objetivos
        // 0-120 / 100-300.
        public const double SwitchCrossfadeMs = 220;
        public const double SwitchRescaleMs = 300;
        public const double SwitchLabelsOutEndMs = 120;
        public const double SwitchLabelsInStartMs = 100;
        public const double SwitchLabelsInEndMs = 220;
        public const double SwitchTargetsOutEndMs = 120;
        public const double SwitchTargetsInStartMs = 100;
        public const double SwitchTargetsInEndMs = 300;

        // Tooltip: entra en dur.micro 90 (ease.enter, escala 0,96 → 1) y sale en dur.fast 160 (ease.exit).
        public const double TooltipInMs = 90;
        public const double TooltipOutMs = 160;
        public const double TooltipInScale = 0.96;
    }
}
