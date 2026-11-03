using System;
using RowMate.Core.Workout;

namespace RowMate.Core.Design
{
    // Las reglas 1 (y 3), 2 y 4 de 00-deuda-diseno §2.4 de la It. 5: todo entra (TV, P360); las filas de cada panel se
    // desplazan adentro (W1280, con sp 1,20); la columna entera se desplaza (TH, L640).
    public enum SeriesFitRule
    {
        FitAll,
        RowsScroll,
        ColumnScroll,
    }

    // Las clases de renglón de un panel (04-design-notes §4.6): cada una con su alto, el centro de la banda de mayúsculas, la
    // línea de base y su tinta.
    public enum SeriesRowKind
    {
        HeaderLabel,
        HeaderTitle,
        Hero,
        LapRow,
        RepRow,
        RepLine,
        EmptyLine,
    }

    // La columna para una ventana (02-tech-design §3.2 de la It. 5): por ventana, con el peor caso (SPM con objetivo, en serie,
    // los dos abiertos), así no cambia con los datos ni con el paso. dp de ventana salvo lo que dice base.
    public readonly struct SeriesColumnLayout
    {
        public SeriesColumnLayout(double hudScale, double scale, double left, double widthBase, double topWithTarget, double topWithoutTarget,
                                  double floor, SeriesFitRule rule)
        {
            HudScale = hudScale;
            Scale = scale;
            Left = left;
            WidthBase = widthBase;
            TopWithTarget = topWithTarget;
            TopWithoutTarget = topWithoutTarget;
            Floor = floor;
            Rule = rule;
        }

        // s, la escala del HUD (los aires con SPM y con la banda son 8·s).
        public double HudScale { get; }

        // sp: s, o min(s; lugar / 182) ≥ 0,9 redondeado hacia abajo a 0,01 (W1280: 1,20).
        public double Scale { get; }

        // 14·s (el borde izquierdo de SPM).
        public double Left { get; }

        // min(137; ventanaBase / 2 − 58).
        public double WidthBase { get; }

        // El ancho del panel en dp (WidthBase · sp).
        public double Width => WidthBase * Scale;

        public double Right => Left + Width;

        // El borde de abajo de SPM con objetivo + 8·s.
        public double TopWithTarget { get; }

        // Ídem sin objetivo (Series 600, Trabajo 1000, Fondo 3000, Cinco reps, Dos series: R-12).
        public double TopWithoutTarget { get; }

        // 8·s arriba de la banda (vertical: alto − 220·s; horizontal: alto − 96·s).
        public double Floor { get; }

        // El lugar entre el techo y el piso, en base del panel (TH 77,1).
        public double RoomWithTargetBase => (Floor - TopWithTarget) / Scale;

        // Ídem sin objetivo (TH 93,9: R-13).
        public double RoomWithoutTargetBase => (Floor - TopWithoutTarget) / Scale;

        public SeriesFitRule Rule { get; }

        public double Top(bool spmHasTarget)
        {
            return spmHasTarget ? TopWithTarget : TopWithoutTarget;
        }

        public double RoomBase(bool spmHasTarget)
        {
            return spmHasTarget ? RoomWithTargetBase : RoomWithoutTargetBase;
        }
    }

    // El lugar de cada panel en un momento: el alto final, la ventana de sus filas y el área de toque.
    public readonly struct SeriesPanelPlacement
    {
        public SeriesPanelPlacement(double topDp, double heightBase, double rowsViewportBase, double rowsContentBase, DpRect hitRect)
        {
            TopDp = topDp;
            HeightBase = heightBase;
            RowsViewportBase = rowsViewportBase;
            RowsContentBase = rowsContentBase;
            HitRect = hitRect;
        }

        // Su borde de arriba final (sin el desplazamiento de la columna).
        public double TopDp { get; }

        // Su alto final.
        public double HeightBase { get; }

        // Regla 2: lo que se ve de sus filas (cerrado, 0); si no se desplazan, sus filas enteras.
        public double RowsViewportBase { get; }

        // El alto de sus filas abiertas (las vueltas, las repeticiones o los dos renglones de un vacío).
        public double RowsContentBase { get; }

        // Cerrado: el chip agrandado hacia arriba y hacia abajo hasta 56 dp (48 como mínimo), con la mitad del aire entre los
        // dos (04-design-notes §5.5); abierto: el panel entero, en su alto final.
        public DpRect HitRect { get; }
    }

    // La geometría de los paneles de series (02-tech-design §3.2 de la It. 5; 00-deuda-diseno §2 y 04-design-notes §4, §5, §6.9:
    // DD5-03, DD5-13 a DD5-15, DD5-19, DD5-20, DD5-23, DD5-26). Pura, en base del panel (× sp = dp) salvo lo que dice dp. La
    // prueban SeriesPanelGeometryTests en dotnet (la tabla de 04-design-notes §5.1 ± 0,1).
    public static class SeriesPanelGeometry
    {
        // 00-deuda-diseno §4.7 y 04-design-notes §4 y §6.9 (base): el ancho, el relleno, los aires de la columna, los altos, los
        // aires de adentro, la cabecera, la marca, el desplazamiento y el toque, la escala mínima y las líneas de los vacíos.
        public const double PanelWidth = 137, PadH = 10, PadV = 8, ColumnGap = 8, ChipHeight = 34, HeaderHeight = 18;
        public const double HeroHeight = 28, RowHeight = 20, BlockGap = 6, RowsGap = 4, RowGap = 2, LabelGap = 4;
        public const double MetersMinGap = 6, HeroMetersGap = 6, Icon = 14, IconGap = 4, Chevron = 18;
        public const double MarkerWidth = 3, MarkerInset = 4, DragSlopDp = 8, HitMinDp = 48, HitRoomyDp = 56;
        public const double MinViewHeight = 182, MinScale = 0.9, EmptyLines = 2, EmptyLineHeight = 22, RepLineHeight = 16;

        // La anatomía (DD5-23; 04-design-notes §4.2 a §4.6): el contenido de x 10 a 127; el ícono en x 10-24; el rótulo y el
        // título desde x 28 (lugar hasta 109,7 y 113,7); la flecha, trazo de 1,5 por (118,75; 15,125) → (122,5; 18,875) →
        // (126,25; 15,125), centro (122,5; 17); la marca en x 4-7; el aire de 4 entre la columna del rótulo y el valor.
        public const double ContentLeft = 10, ContentRight = 127, IconLeft = 10, LabelLeft = 28, LabelRoom = 81.7, TitleRoom = 85.7;
        public const double ChevronCenterX = 122.5, ChevronCenterY = 17, SeriesChevronStroke = 1.5, MarkerLeft = 4, EmptyLineRoom = 117;
        public const double ChevronHalfWidth = 3.75, ChevronHalfHeight = 1.875, MarkerRadius = 1.5, MarkerHeight = RowHeight;

        // Las reglas de alfa por la tinta (§3.1; DD5-13 a DD5-15) y el piso de las escalas de números (QA-G5T-16).
        public const double SeriesRevealFade = 4, SeriesRowsEdgeFade = 4, SeriesScrollFade = 12, SeriesNumberMinScale = 0.6;
        public const double TitleMinScale = 0.8;

        // El aspecto (04-design-notes §3, §4.1, §4.9): el borde al 10 %, el ícono al 80 %, la flecha al 54 % (= text.tertiary),
        // los metros de la vuelta en curso al 80 % (la regla de las unidades del HUD), el rótulo del chip a 0,08 em, la capa de
        // estado en la banda de la cabecera (y 0-34) y el anillo de foco en dp de ventana: blanco de −4 a −2 y halo de −8 a −4
        // del borde visual, radios 14·sp + 4 y 14·sp + 8 (DD5-22).
        public const double GlassRadius = 14, BorderAlpha = 0.10, IconAlpha = 0.80, ChevronAlpha = 0.54, HeroMetersAlpha = HudGeometry.UnitAlpha;
        public const double LabelTrackingEm = 0.08, StateLayerBottom = ChipHeight;
        public const double FocusRingInnerDp = 2, FocusRingOuterDp = 4, FocusHaloOuterDp = 8, FocusHaloAlpha = 0.50;

        // La vertical de cada renglón dentro del panel (04-design-notes §4.4, §4.5): la cabecera en y 8, el contenido desde y 32
        // (8 + 18 + 6); la vuelta en curso en 32 y la fila k de vueltas en 64 + 22·k; la fila k de repeticiones en 32 + 22·k;
        // los renglones de un vacío en 32 y 54.
        public const double HeaderTop = PadV, ContentTop = PadV + HeaderHeight + BlockGap, LapRowsTop = ContentTop + HeroHeight + RowsGap;

        private const double Epsilon = 1e-6;

        // ── La columna (00-deuda-diseno §2.2 a §2.4; 04-design-notes §5.1) ──

        // La columna de una ventana: el lado de SPM, el ancho, el techo (con y sin objetivo, 8·s debajo de SPM), el piso (8·s
        // arriba de la banda) y la regla con el peor caso (los dos abiertos con 4 vueltas, la línea Rep y 3 repeticiones, con
        // SPM con objetivo): entra todo a s → FitAll; entra la vista mínima (182) a s → RowsScroll; entra a una escala menor
        // ≥ 0,9 (redondeada hacia abajo a 0,01) → RowsScroll con esa sp; si no, ColumnScroll a s.
        public static SeriesColumnLayout Column(HudLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var s = layout.Scale;
            var cardsTop = layout.Spm.Top;
            var withTarget = cardsTop + HudGeometry.CardHeight(HudGeometry.LargeValue, HudGeometry.LargeLabel, HudGeometry.LargePadV, true) * s
                             + ColumnGap * s;
            var withoutTarget = cardsTop + HudGeometry.CardHeight(HudGeometry.LargeValue, HudGeometry.LargeLabel, HudGeometry.LargePadV, false) * s
                                + ColumnGap * s;
            var floor = layout.Band.Top - ColumnGap * s;
            var widthBase = Math.Min(PanelWidth, layout.WindowWidth / s / 2 - 58);
            var roomDp = floor - withTarget;
            var worst = WorstCaseHeight;
            SeriesFitRule rule;
            var sp = s;
            if (worst * s <= roomDp + Epsilon)
            {
                rule = SeriesFitRule.FitAll;
            }
            else if (MinViewHeight * s <= roomDp + Epsilon)
            {
                rule = SeriesFitRule.RowsScroll;
            }
            else
            {
                var reduced = Math.Floor(Math.Min(s, roomDp / MinViewHeight) * 100 + Epsilon) / 100;
                if (reduced >= MinScale - Epsilon)
                {
                    rule = SeriesFitRule.RowsScroll;
                    sp = reduced;
                }
                else
                {
                    rule = SeriesFitRule.ColumnScroll;
                }
            }

            return new SeriesColumnLayout(s, sp, layout.Spm.Left, widthBase, withTarget, withoutTarget, floor, rule);
        }

        // Lo más alto que puede medir la columna: parciales con 4 vueltas y la línea Rep (180) + 8 + repeticiones con 3 (104).
        public static double WorstCaseHeight => LapsOpenHeight(true, VisibleLapsMax, true) + ColumnGap + RepsOpenHeight(SeriesTracker.MaxPreviousReps);

        private const int VisibleLapsMax = SeriesPanels.VisibleLaps;

        // ── Los altos (00-deuda-diseno §2.4 con R-01: la primera vuelta suma 24 y cada una más 22) ──

        // El alto natural (base) de un panel con su contenido: cerrado 34; parciales abierto: 8 + 18 + 6 + (trabajo ? 28 + (n > 0
        // ? 4 + 20·n + 2·(n − 1) : 0) : 2·22) + (en serie ? 6 + 16 : 0) + 8, con n ≤ 4; repeticiones abierto: 8 + 18 + 6 +
        // (m > 0 ? 20·m + 2·(m − 1) : 2·22) + 8.
        public static double NaturalHeight(SeriesPanelKind kind, bool expanded, in SeriesPanelsContent content)
        {
            if (!expanded)
            {
                return ChipHeight;
            }

            return kind == SeriesPanelKind.Laps
                ? LapsOpenHeight(content.IsWork, content.VisibleLaps, content.InSeries)
                : RepsOpenHeight(content.PreviousReps == null ? 0 : content.PreviousReps.Count);
        }

        public static double LapsOpenHeight(bool isWork, int visibleLaps, bool inSeries)
        {
            return LapsFixedHeight(isWork, visibleLaps, inSeries) + LapsRowsHeight(isWork, visibleLaps);
        }

        public static double RepsOpenHeight(int reps)
        {
            return RepsFixedHeight + RepsRowsHeight(reps);
        }

        // Lo que no se desplaza de parciales abierto (la cabecera, la vuelta en curso con su aire de 4, la línea Rep y los
        // rellenos) y sus filas (o los dos renglones del vacío fuera de un trabajo).
        public static double LapsFixedHeight(bool isWork, int visibleLaps, bool inSeries)
        {
            var n = Clamp(visibleLaps, 0, VisibleLapsMax);
            return PadV + HeaderHeight + BlockGap + (isWork ? HeroHeight + (n > 0 ? RowsGap : 0) : 0) + (inSeries ? BlockGap + RepLineHeight : 0) + PadV;
        }

        public static double LapsRowsHeight(bool isWork, int visibleLaps)
        {
            var n = Clamp(visibleLaps, 0, VisibleLapsMax);
            return isWork ? RowsHeight(n) : EmptyLines * EmptyLineHeight;
        }

        public static double RepsFixedHeight => PadV + HeaderHeight + BlockGap + PadV;

        public static double RepsRowsHeight(int reps)
        {
            var m = Clamp(reps, 0, SeriesTracker.MaxPreviousReps);
            return m > 0 ? RowsHeight(m) : EmptyLines * EmptyLineHeight;
        }

        // 20·n + 2·(n − 1) (0 sin filas).
        public static double RowsHeight(int rows)
        {
            return rows > 0 ? RowHeight * rows + RowGap * (rows - 1) : 0;
        }

        // ── La vertical de cada renglón (04-design-notes §4.4 a §4.6) ──

        // La fila k (0 = la más reciente) de vueltas: 64 + 22·k.
        public static double LapRowTop(int k)
        {
            return LapRowsTop + (RowHeight + RowGap) * k;
        }

        // La fila k de repeticiones: 32 + 22·k.
        public static double RepRowTop(int k)
        {
            return ContentTop + (RowHeight + RowGap) * k;
        }

        // El renglón i (0 o 1) de un vacío: 32 y 54.
        public static double EmptyLineTop(int i)
        {
            return ContentTop + EmptyLineHeight * i;
        }

        // La línea Rep: 66 sin vueltas, 68 + 22·n con n vueltas a la vista y 82 fuera de un trabajo.
        public static double RepLineTop(bool isWork, int visibleLaps)
        {
            var n = Clamp(visibleLaps, 0, VisibleLapsMax);
            if (!isWork)
            {
                return ContentTop + EmptyLines * EmptyLineHeight + BlockGap;
            }

            return n > 0 ? LapRowsTop + RowsHeight(n) + BlockGap : ContentTop + HeroHeight + BlockGap;
        }

        public static double RowHeightOf(SeriesRowKind kind)
        {
            switch (kind)
            {
                case SeriesRowKind.HeaderLabel:
                case SeriesRowKind.HeaderTitle:
                    return HeaderHeight;
                case SeriesRowKind.Hero:
                    return HeroHeight;
                case SeriesRowKind.LapRow:
                case SeriesRowKind.RepRow:
                    return RowHeight;
                case SeriesRowKind.RepLine:
                    return RepLineHeight;
                default:
                    return EmptyLineHeight;
            }
        }

        // El centro de la banda de las mayúsculas, desde el borde de arriba del renglón (la mitad del renglón).
        public static double BandCenter(SeriesRowKind kind)
        {
            return RowHeightOf(kind) / 2;
        }

        // La línea de base desde el borde de arriba del renglón (04-design-notes §4.6).
        public static double Baseline(SeriesRowKind kind)
        {
            switch (kind)
            {
                case SeriesRowKind.HeaderLabel:
                    return 13.73;
                case SeriesRowKind.HeaderTitle:
                    return 13.00;
                case SeriesRowKind.Hero:
                    return 23.80;
                case SeriesRowKind.LapRow:
                case SeriesRowKind.RepRow:
                    return 17.00;
                case SeriesRowKind.RepLine:
                    return 13.60;
                default:
                    return 16.46;
            }
        }

        // La tinta de arriba, desde el borde de arriba del renglón.
        public static double InkTop(SeriesRowKind kind)
        {
            switch (kind)
            {
                case SeriesRowKind.HeaderLabel:
                    return 4.27;
                case SeriesRowKind.HeaderTitle:
                    return 5.00;
                case SeriesRowKind.Hero:
                    return 4.20;
                case SeriesRowKind.LapRow:
                case SeriesRowKind.RepRow:
                    return 3.00;
                case SeriesRowKind.RepLine:
                    return 2.40;
                default:
                    return 5.54;
            }
        }

        // La tinta de abajo, con descendentes: el rótulo Reps (16,87) y el título con p (15,65) solo si el texto los tiene
        // (descender); la fila de repetición (19,89) y la línea Rep (16,80) por la p de Rep; el vacío, 20,08.
        public static double InkBottom(SeriesRowKind kind, bool descender = false)
        {
            switch (kind)
            {
                case SeriesRowKind.HeaderLabel:
                    return descender ? 16.87 : 13.73;
                case SeriesRowKind.HeaderTitle:
                    return descender ? 15.65 : 13.00;
                case SeriesRowKind.Hero:
                    return 23.80;
                case SeriesRowKind.LapRow:
                    return 17.00;
                case SeriesRowKind.RepRow:
                    return 19.89;
                case SeriesRowKind.RepLine:
                    return 16.80;
                default:
                    return 20.08;
            }
        }

        // La tinta de un renglón en base del panel, achicada alrededor del centro de la banda con su escala de ajuste k:
        // top + centro + (tinta − centro)·k (04-design-notes §4.6).
        public static double InkTopAt(SeriesRowKind kind, double rowTop, double scale = 1)
        {
            var center = BandCenter(kind);
            return rowTop + center + (InkTop(kind) - center) * scale;
        }

        public static double InkBottomAt(SeriesRowKind kind, double rowTop, double scale = 1, bool descender = false)
        {
            var center = BandCenter(kind);
            return rowTop + center + (InkBottom(kind, descender) - center) * scale;
        }

        // ── Las reglas de alfa por la tinta (§3.1; DD5-13 a DD5-15; 04-design-notes §5.3), en base del panel ──

        // Regla 1, revelar (siempre, contra el vidrio de alto visual G): clamp((G − tintaAbajo) / 4).
        public static double RevealAlpha(double glassBase, double inkBottomBase)
        {
            return Clamp01((glassBase - inkBottomBase) / SeriesRevealFade);
        }

        // Regla 2, la ventana de filas (de windowTop a windowBottom): abajo siempre; arriba solo con el desplazamiento > 0.
        // clamp(1 − o / 4), con o lo que la tinta pasa el borde.
        public static double RowsEdgeAlpha(double inkTopBase, double inkBottomBase, double windowTopBase, double windowBottomBase, bool scrolled)
        {
            var alpha = Clamp01(1 - (inkBottomBase - windowBottomBase) / SeriesRowsEdgeFade);
            if (scrolled)
            {
                alpha *= Clamp01(1 - (windowTopBase - inkTopBase) / SeriesRowsEdgeFade);
            }

            return alpha;
        }

        // Regla 3, la máscara de la columna (solo ColumnScroll), en base: contra el borde de abajo (columnBottom) con lo que
        // queda escondido debajo, y contra el de arriba (columnTop) con lo escondido arriba. Para el vidrio, y de cada vértice
        // en los dos lados; para un renglón, su tinta de abajo contra el borde de abajo y la de arriba contra el de arriba:
        // α = 1 − f·(1 − clamp((Cb − y) / 12)), con la fuerza f = clamp(escondido / 12) (si nada pasa el borde, 1).
        public static double ColumnMaskAlpha(double topBase, double bottomBase, double columnTopBase, double columnBottomBase,
                                             double hiddenAboveBase, double hiddenBelowBase)
        {
            var below = Clamp01(hiddenBelowBase / SeriesScrollFade);
            var above = Clamp01(hiddenAboveBase / SeriesScrollFade);
            var alphaBelow = 1 - below * (1 - Clamp01((columnBottomBase - bottomBase) / SeriesScrollFade));
            var alphaAbove = 1 - above * (1 - Clamp01((topBase - columnTopBase) / SeriesScrollFade));
            return alphaBelow * alphaAbove;
        }

        // ── El reparto (DD5-26; 04-design-notes §5.2) ──

        // El lugar de cada panel con la regla de la ventana: FitAll y ColumnScroll, cada uno a su natural (en ColumnScroll la
        // columna entera se desplaza); RowsScroll, con las filas de cada uno desplazables y la cabecera, la vuelta en curso y la
        // línea Rep fijas: repeticiones tiene reservado su mínimo (34 cerrado; abierto, 60 —la cabecera y una fila o el primer
        // renglón de su vacío— o su natural si es menor); parciales recibe min(su natural; lugar − 8 − ese mínimo) y
        // repeticiones lo que queda, hasta su natural. Sin repeticiones a la vista, parciales recibe min(su natural; lugar).
        // columnContentBase: lo que mide la columna (parciales + 8 + repeticiones).
        public static void Place(in SeriesColumnLayout column, bool spmHasTarget, double lapsNatural, double repsNatural, bool repsShown,
                                 bool lapsExpanded, bool repsExpanded, in SeriesPanelsContent content,
                                 out SeriesPanelPlacement laps, out SeriesPanelPlacement reps, out double columnContentBase)
        {
            var room = column.RoomBase(spmHasTarget);
            var lapsHeight = lapsNatural;
            var repsHeight = repsNatural;
            var lapsFixed = lapsExpanded ? LapsFixedHeight(content.IsWork, content.VisibleLaps, content.InSeries) : ChipHeight;
            var repsFixed = repsExpanded ? RepsFixedHeight : ChipHeight;
            if (column.Rule == SeriesFitRule.RowsScroll)
            {
                if (repsShown)
                {
                    var repsMin = repsExpanded ? Math.Min(RepsFixedHeight + RowHeight, repsNatural) : ChipHeight;
                    lapsHeight = Math.Max(Math.Min(lapsNatural, room - ColumnGap - repsMin), Math.Min(lapsFixed, lapsNatural));
                    repsHeight = Math.Max(Math.Min(repsNatural, room - ColumnGap - lapsHeight), Math.Min(repsFixed, repsNatural));
                }
                else
                {
                    lapsHeight = Math.Max(Math.Min(lapsNatural, room), Math.Min(lapsFixed, lapsNatural));
                }
            }

            var sp = column.Scale;
            var lapsTop = column.Top(spmHasTarget);
            var repsTop = lapsTop + (lapsHeight + ColumnGap) * sp;
            var lapsRows = lapsExpanded ? LapsRowsHeight(content.IsWork, content.VisibleLaps) : 0;
            var repsRows = repsExpanded ? RepsRowsHeight(content.PreviousReps == null ? 0 : content.PreviousReps.Count) : 0;
            var lapsViewport = Clamp(lapsHeight - lapsFixed, 0, lapsRows);
            var repsViewport = Clamp(repsHeight - repsFixed, 0, repsRows);

            var lapsHit = Hit(column, spmHasTarget, lapsTop, lapsHeight, lapsExpanded, true, repsShown);
            var repsHit = Hit(column, spmHasTarget, repsTop, repsHeight, repsExpanded, false, true);
            laps = new SeriesPanelPlacement(lapsTop, lapsHeight, lapsViewport, lapsRows, lapsHit);
            reps = new SeriesPanelPlacement(repsTop, repsHeight, repsViewport, repsRows, repsHit);
            columnContentBase = lapsHeight + (repsShown ? ColumnGap + repsHeight : 0);
        }

        // El área de toque (AC-WKT-18-09; 04-design-notes §5.5; R-14). Cerrado: el chip agrandado hacia arriba y hacia abajo
        // hasta 56 dp de alto (tope); cada chip toma la mitad del aire entre los dos (4·sp), así nunca se superponen, y si hace
        // falta el aire con SPM y su relleno de abajo (parciales) o el aire con la banda y su relleno de arriba (el de abajo de
        // la columna), que no responden. Se agranda lo mismo para cada lado y lo que un lado no puede, lo toma el otro.
        // Abierto: el panel entero.
        private static DpRect Hit(in SeriesColumnLayout column, bool spmHasTarget, double topDp, double heightBase, bool expanded, bool isLaps,
                                  bool repsShown)
        {
            var sp = column.Scale;
            var s = column.HudScale;
            var left = column.Left;
            var width = column.Width;
            var height = heightBase * sp;
            if (expanded)
            {
                return new DpRect(left, topDp, width, height);
            }

            var halfGap = ColumnGap * sp / 2;
            var towardSpm = ColumnGap * s + HudGeometry.LargePadV * s;
            var bottomLimit = column.Floor + ColumnGap * s + HudGeometry.BandPadV * s;
            double upRoom;
            double downRoom;
            if (isLaps)
            {
                upRoom = topDp - (column.Top(spmHasTarget) - towardSpm);
                downRoom = repsShown ? halfGap : bottomLimit - (topDp + height);
            }
            else
            {
                upRoom = halfGap;
                downRoom = bottomLimit - (topDp + height);
            }

            upRoom = Math.Max(0, upRoom);
            downRoom = Math.Max(0, downRoom);
            var need = Math.Max(0, HitRoomyDp - height);
            var up = Math.Min(need / 2, upRoom);
            var down = Math.Min(need / 2, downRoom);
            var left1 = need - up - down;
            if (left1 > 0)
            {
                var moreUp = Math.Min(left1, upRoom - up);
                up += moreUp;
                left1 -= moreUp;
                down += Math.Min(left1, downRoom - down);
            }

            return DpRect.FromEdges(left, topDp - up, left + width, topDp + height + down);
        }

        // ── Los textos (DV-49, DD5-19, DD5-20; 04-design-notes §4.7, §4.8) ──

        // La escala pareja para que un ancho entre: 1 si entra; si no, available / width, con su mínimo (el título 0,8; los
        // números 0,6). Lo que en el mínimo no entra, la vista lo recorta con … al final.
        public static double FitScale(double available, double width, double minimum = 0)
        {
            if (!(width > available) || !(width > 0))
            {
                return 1;
            }

            var scale = available / width;
            return scale < minimum ? minimum : scale;
        }

        // La escala de título de un idioma (DD5-20): la menor entre las de los dos títulos, con TitleMinScale; así nada cambia
        // de tamaño cuando aparece o se va el panel de repeticiones (es 0,949 por "Series anteriores"; en 1).
        public static double TitleScale(double lapsTitleWidth, double repsTitleWidth, double room = TitleRoom)
        {
            return Math.Min(FitScale(room, lapsTitleWidth, TitleMinScale), FitScale(room, repsTitleWidth, TitleMinScale));
        }

        // El corte parejo de un vacío en dos renglones (DD5-19): de los cortes entre palabras que dejan las dos líneas en room o
        // menos, el de la línea más larga más corta (en un empate, el primero). Devuelve el índice del espacio donde se corta
        // (0 = entre la primera y la segunda palabra), o −1 si ninguno entra (la regla del título).
        public static int BalancedBreak(ReadOnlySpan<double> wordWidths, double spaceWidth, double room = EmptyLineRoom)
        {
            var count = wordWidths.Length;
            if (count < 2)
            {
                return -1;
            }

            var total = 0.0;
            for (var i = 0; i < count; i++)
            {
                total += wordWidths[i];
            }

            total += spaceWidth * (count - 1);
            var best = -1;
            var bestLongest = double.MaxValue;
            var first = 0.0;
            for (var i = 0; i < count - 1; i++)
            {
                first += wordWidths[i] + (i > 0 ? spaceWidth : 0);
                var second = total - first - spaceWidth;
                if (first > room + Epsilon || second > room + Epsilon)
                {
                    continue;
                }

                var longest = Math.Max(first, second);
                if (longest < bestLongest - Epsilon)
                {
                    bestLongest = longest;
                    best = i;
                }
            }

            return best;
        }

        // ── La zona del bote y la cuenta grande (AC-WKT-18-14, -15; 00-deuda-diseno §2.2, P-03, P-04) ──

        // Cerrados, nunca entran en la zona del bote; abiertos en vertical, solo en su franja de afuera (x ≤ ancho / 2 − 44·s, la
        // columna del centro tiene el disco, el casco y el remero); en horizontal, nunca.
        public static bool RespectsBoatZone(DpRect panel, bool expanded, in HudLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (!Overlaps(panel, layout.BoatZone, 0))
            {
                return true;
            }

            if (!expanded || layout.IsLandscape)
            {
                return false;
            }

            return panel.Right <= layout.WindowWidth / 2 - 44 * layout.Scale + Epsilon;
        }

        // La cuenta grande y PAUSA (AC-WKT-18-15; spec §12 punto 6): true si el rectángulo FINAL de un panel (agrandado 8·s) se
        // cruza con el disco del lugar centro alto (rozarse no cuenta). Con el ancho de 137 no pasa en ningún perfil (el panel
        // termina exactamente 8·s antes del disco en TV); con los 200 del legacy pasaría en TV y P360 (P-04).
        public static bool BlocksCallout(DpRect lapsFinal, DpRect? repsFinal, in HudLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var gap = ColumnGap * layout.Scale;
            return Overlaps(lapsFinal, layout.BigCount, gap) || (repsFinal.HasValue && Overlaps(repsFinal.Value, layout.BigCount, gap));
        }

        // Cruce con más de 1e-6 dp de superposición (rozarse no cuenta), con a agrandado gap dp de cada lado.
        private static bool Overlaps(DpRect a, DpRect b, double gap)
        {
            var x = Math.Min(a.Right + gap, b.Right) - Math.Max(a.Left - gap, b.Left);
            var y = Math.Min(a.Bottom + gap, b.Bottom) - Math.Max(a.Top - gap, b.Top);
            return x > Epsilon && y > Epsilon;
        }

        private static double Clamp01(double value)
        {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
