using System;
using RowMate.Core.Ports;

namespace RowMate.Core.Design
{
    // Esquina de una zona del HUD que va en su ancla (el transform-origin de la escala por zona, DT4a-03).
    public enum HudCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    // Los valores que van en un FitBox (02-tech-design §6.4): sus cifras se achican si no entran (T1 a T5).
    public enum HudValue
    {
        Spm,
        Split,
        Watts,
        Kcal,
        Bpm,
        Band,
    }

    // Los anchos naturales, a tamaño BASE (dp antes de s), de lo que mide la vista (HudZones, con FitBox.NaturalWidth: sin
    // escala) en el GeometryChangedEvent de cada contenido (DN4a-13). Valores: lo que va en cada FitBox (las cifras; Watts
    // con 4 + "W"; la distancia de la banda con 3 + "m"). Rótulos: el de cada tarjeta (BPM con el corazón de 11 y su aire de
    // 4). Objetivos: el `obj. N` de cada tarjeta con objetivo (sin objetivo no cuenta).
    public readonly struct HudNaturalWidths
    {
        public HudNaturalWidths(double spmValue, double splitValue, double wattsValue, double kcalValue, double bpmValue,
                                double bandTime, double bandDistance, double spmLabel, double splitLabel, double wattsLabel,
                                double kcalLabel, double bpmLabel, double spmTarget, double splitTarget, double wattsTarget)
        {
            SpmValue = spmValue;
            SplitValue = splitValue;
            WattsValue = wattsValue;
            KcalValue = kcalValue;
            BpmValue = bpmValue;
            BandTime = bandTime;
            BandDistance = bandDistance;
            SpmLabel = spmLabel;
            SplitLabel = splitLabel;
            WattsLabel = wattsLabel;
            KcalLabel = kcalLabel;
            BpmLabel = bpmLabel;
            SpmTarget = spmTarget;
            SplitTarget = splitTarget;
            WattsTarget = wattsTarget;
        }

        public double SpmValue { get; }

        public double SplitValue { get; }

        public double WattsValue { get; }

        public double KcalValue { get; }

        public double BpmValue { get; }

        public double BandTime { get; }

        public double BandDistance { get; }

        public double SpmLabel { get; }

        public double SplitLabel { get; }

        public double WattsLabel { get; }

        public double KcalLabel { get; }

        public double BpmLabel { get; }

        public double SpmTarget { get; }

        public double SplitTarget { get; }

        public double WattsTarget { get; }
    }

    // Lo que necesita HudGeometry.Layout: la ventana, los márgenes seguros, si reserva la llave (build con simulador), si hay
    // BPM, qué tarjetas tienen objetivo y los anchos naturales.
    public readonly struct HudLayoutInput
    {
        public HudLayoutInput(double width, double height, SafeInsets insets, bool reserveWrench, bool hasBpm, bool spmTarget,
                              bool splitTarget, bool wattsTarget, HudNaturalWidths natural)
        {
            Width = width;
            Height = height;
            Insets = insets;
            ReserveWrench = reserveWrench;
            HasBpm = hasBpm;
            SpmTarget = spmTarget;
            SplitTarget = splitTarget;
            WattsTarget = wattsTarget;
            Natural = natural;
        }

        public double Width { get; }

        public double Height { get; }

        public SafeInsets Insets { get; }

        public bool ReserveWrench { get; }

        public bool HasBpm { get; }

        public bool SpmTarget { get; }

        public bool SplitTarget { get; }

        public bool WattsTarget { get; }

        public HudNaturalWidths Natural { get; }
    }

    // Una zona: su ancla en dp de ventana (y desde arriba), la esquina que va en el ancla y el ancho máximo de su contenido a
    // tamaño base (el tope de su FitBox, T1 a T5). La vista pone el ancla, scale = s con el origen en esa esquina y el tope.
    public readonly struct HudZone
    {
        public HudZone(double anchorX, double anchorY, HudCorner corner, double maxBaseWidth)
        {
            AnchorX = anchorX;
            AnchorY = anchorY;
            Corner = corner;
            MaxBaseWidth = maxBaseWidth;
        }

        public double AnchorX { get; }

        public double AnchorY { get; }

        public HudCorner Corner { get; }

        public double MaxBaseWidth { get; }
    }

    // La disposición calculada para una ventana y unos valores (02-tech-design §6.4; 04-design-notes §3.1-§3.9): las zonas,
    // la escala, los rectángulos de cada pieza en dp de ventana (los que comprueban HGT, HudOverlap y WrenchAudit), la escala
    // de las cifras de cada FitBox, la zona del bote, el horizonte y, con el simulador, el lugar de la llave y el techo del
    // panel. Inmutable.
    public sealed class HudLayout
    {
        private readonly double _spmScale;
        private readonly double _splitScale;
        private readonly double _wattsScale;
        private readonly double _vitalsScale;
        private readonly double _bandScale;

        internal HudLayout(double windowWidth, double windowHeight, double scale, bool isLandscape, HudZone topBarZone, HudZone spmZone, HudZone rightZone, HudZone vitalsZone,
                           HudZone bandZone, HudZone controlsZone, DpRect topBar, DpRect spm, DpRect split, DpRect watts, DpRect kcal,
                           DpRect? bpm, DpRect band, DpRect controls, DpRect boatZone, DpRect? wrench, double? panelCeiling,
                           double wattsWithTargetBottom, double horizon, bool vitalsInRow, bool vitalsMinWidth, double spmScale,
                           double splitScale, double wattsScale, double vitalsScale, double bandScale, DpRect topBarMax, DpRect bigCount,
                           bool bigCountFits, System.Collections.Generic.IReadOnlyList<DpRect> leftLaneObstacles)
        {
            TopBarMax = topBarMax;
            BigCount = bigCount;
            BigCountFits = bigCountFits;
            LeftLaneObstacles = leftLaneObstacles;
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
            Scale = scale;
            IsLandscape = isLandscape;
            TopBarZone = topBarZone;
            SpmZone = spmZone;
            RightZone = rightZone;
            VitalsZone = vitalsZone;
            BandZone = bandZone;
            ControlsZone = controlsZone;
            TopBar = topBar;
            Spm = spm;
            Split = split;
            Watts = watts;
            Kcal = kcal;
            Bpm = bpm;
            Band = band;
            Controls = controls;
            BoatZone = boatZone;
            Wrench = wrench;
            PanelCeiling = panelCeiling;
            WattsWithTargetBottom = wattsWithTargetBottom;
            Horizon = horizon;
            VitalsInRow = vitalsInRow;
            VitalsMinWidth = vitalsMinWidth;
            _spmScale = spmScale;
            _splitScale = splitScale;
            _wattsScale = wattsScale;
            _vitalsScale = vitalsScale;
            _bandScale = bandScale;
        }

        public double WindowWidth { get; }

        public double WindowHeight { get; }

        // hudScale (ART_BIBLE §5.3).
        public double Scale { get; }

        // Ancho > alto (immersive_workout_screen.dart:605-606).
        public bool IsLandscape { get; }

        public HudZone TopBarZone { get; }

        public HudZone SpmZone { get; }

        // Split y Watts (columna alineada a la derecha).
        public HudZone RightZone { get; }

        // kcal y BPM: en columna en vertical (T5), en fila a la altura de los controles en horizontal (T4; DV-33).
        public HudZone VitalsZone { get; }

        public HudZone BandZone { get; }

        public HudZone ControlsZone { get; }

        public DpRect TopBar { get; }

        public DpRect Spm { get; }

        public DpRect Split { get; }

        public DpRect Watts { get; }

        public DpRect Kcal { get; }

        // null sin BPM (pulso fusionado 0).
        public DpRect? Bpm { get; }

        public DpRect Band { get; }

        public DpRect Controls { get; }

        // 44 % × 30 %, centrada en x, con el centro en BoatCenterY (ART_BIBLE §5.2; DN4a-02).
        public DpRect BoatZone { get; }

        // El lugar de la llave (solo con ReserveWrench; DN4a-14): lo declara la página en OverlaySurface.WrenchSlot.
        public DpRect? Wrench { get; }

        // It. 4b (DN4b-23; AC-BLE-16-03): en las dos orientaciones. En vertical, 8 dp debajo del disco del lugar centro alto
        // (BigCount, que está debajo de Watts con su objetivo; en la 4a, 8 debajo de Watts); en horizontal, 8 dp debajo de la barra
        // en su alto mayor (74·s, con el sello). El panel abierto no tapa el sello ni el lugar de la cuenta grande y de PAUSA, y
        // su alto no cambia con el paso, con el sello ni con la cuenta.
        public double? PanelCeiling { get; }

        // It. 4b: la barra de pasos en su alto máximo (74·s, con el sello; DN4b-14). Las tarjetas siguen a 90·s del margen
        // superior: la barra crece hacia abajo dentro de ese lugar (AC-WKT-14-01).
        public DpRect TopBarMax { get; }

        // It. 4b (§3.5; DN4b-14; AC-WKT-14-18, AC-WKT-13-12): el disco de 72·s del lugar "centro alto", de la cuenta grande y de
        // PAUSA. Centrado en x en la ventana; en y, centrado entre lo que tiene arriba más 8·s (la barra en su alto máximo, o
        // SPM, Split o Watts con objetivo si se cruzan con él en x, agrandados 8·s) y el borde de arriba de la zona del bote
        // menos 8·s. Al rotar cambia en el acto.
        public DpRect BigCount { get; }

        // false: entre lo de arriba y la zona del bote no entra el disco (no pasa en los cinco perfiles con los valores de la
        // spec; con valores extremos en vertical, una tarjeta de la derecha se cruza con él): la vista no lo muestra esa vez.
        public bool BigCountFits { get; }

        // It. 4b (R10; DN4b-22; AC-BLE-16-02): las piezas del HUD que se cruzan con el carril izquierdo de la llave (x 16-56 desde
        // el margen izquierdo, agrandado 8): las tarjetas, la banda, kcal y BPM, los controles y la barra. La página las declara
        // con el diálogo de completado abierto (OverlaySurface.Workout).
        public System.Collections.Generic.IReadOnlyList<DpRect> LeftLaneObstacles { get; }

        // El borde de abajo de Watts con su objetivo (con Split sin objetivo), y desde arriba.
        public double WattsWithTargetBottom { get; }

        // El horizonte del fondo provisorio y de la cámara, como fracción del alto (0,40 / 0,42).
        public double Horizon { get; }

        // El corte entre el cielo y el agua del fondo provisorio, en dp desde arriba (DN4a-18).
        public double HorizonY => Horizon * WindowHeight;

        public bool VitalsInRow { get; }

        // En horizontal: la fila de kcal y BPM usa los anchos mínimos (96·s) porque entran los dos (T4).
        public bool VitalsMinWidth { get; }

        // Banda compacta (relleno 14 y 10 a cada lado del divisor) en horizontal (DV-33).
        public bool BandCompact => IsLandscape;

        // La escala de las cifras de cada FitBox (1 = entran). En horizontal kcal y BPM comparten la suya (T4).
        public double ValueScale(HudValue value)
        {
            switch (value)
            {
                case HudValue.Spm:
                    return _spmScale;
                case HudValue.Split:
                    return _splitScale;
                case HudValue.Watts:
                    return _wattsScale;
                case HudValue.Kcal:
                case HudValue.Bpm:
                    return _vitalsScale;
                case HudValue.Band:
                    return _bandScale;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, "HudLayout: valor desconocido.");
            }
        }
    }

    // La geometría del HUD (02-tech-design §6.4; ART_BIBLE §5.2-§5.3; 04-design-notes §3 de la It. 4a: DN4a-01, DN4a-02,
    // DN4a-04, DN4a-13, DN4a-14). Puerto de hud_layout.py del Designer: las zonas del legacy × s
    // (immersive_workout_screen.dart:91, :612-613, :640-641, :652, :672, :676, :693, :710-711, :112, :902, :920) con los
    // tamaños de D-ART-02, la fila de abajo en horizontal (kcal y BPM a la altura de los controles, kcal en el rincón; la banda
    // compacta), la zona del bote con el centro de la cámara de persecución, los topes T1 a T5 y el lugar de la llave. Pura:
    // la prueban HudGeometryTests en dotnet (los rectángulos de §3.8 ± 0,1 y las escalas de §3.7 ± 0,005). Las constantes de
    // geometría de 04-design-notes §5.13 viven acá; las de tiempo, en MotionSpecs.
    public static class HudGeometry
    {
        public const double ScaleShortSide = 390, MinScale = 0.9, MaxScale = 1.6;
        public const double TopBarTop = 8, TopBarSide = 12, TopBarHeight = 40, TopBarRowHeight = 22, TopBarPadV = 8;
        public const double StepRowPadH = 12, FreeRowPadH = 14;
        public const double CardsTop = 90, Side = 14, CardGap = 8, SmallCardMinWidth = 96;
        public const double PortraitBottom = 140, ControlsBottom = 16, ControlsSidePadding = 24, ControlsGap = 16;
        public const double FinishMinWidth = 128, PauseMinWidth = 148, ControlsHeight = 56;
        public const double BandPadHPortrait = 20, BandPadHLandscape = 14, BandDividerGapPortrait = 16, BandDividerGapLandscape = 10;
        public const double BandPadV = 10, BandHeight = 72, BandDivider = 1;
        public const double ArrowEm = 0.4, ArrowGutterEm = 0.48, ArrowUpBaselineEm = 0.196, ArrowDownBaselineEm = 0.214;
        public const double UnitAlpha = 0.8;
        public const double BoatZoneWidth = 0.44, BoatZoneHeight = 0.30;
        public const double HorizonPortrait = 0.40, HorizonLandscape = 0.42, CameraHeightM = 2.2, RowerDistanceM = 9.5;

        // dp sin s: la llave no escala (R1).
        public const double WrenchSize = 40, WrenchEdge = 16, WrenchGap = 8;

        // Caja de línea de Inter: 1,238 em (medida en Unity, It. 2). Tarjetas de ART_BIBLE §3.2 (D-ART-02): valor, rótulo,
        // ancho mínimo y relleno (horizontal × vertical); `obj.` 12/600 con 2 dp de aire.
        public const double LineBox = 1.238;
        public const double LargeValue = 64, LargeLabel = 13, LargeMinWidth = 140, LargePadH = 16, LargePadV = 10;
        public const double MediumValue = 44, MediumLabel = 12, MediumMinWidth = 118, MediumPadH = 14, MediumPadV = 10;
        public const double SmallValue = 32, SmallLabel = 11, SmallPadH = 12, SmallPadV = 8;
        public const double LabelGap = 2, TargetGap = 2, TargetSize = 12;

        // ── It. 4b (02-tech-design §3.1, §3.4 a §3.9; 04-design-notes §3; DN4b-14, -21 a -23), dp base ──
        // La barra de pasos: dos filas (píldoras 20 + información 22 + 8 abajo) = 52; con el sello, 74 (17 de contenido + 5).
        // La libre sigue en 40 (TopBarHeight, de la 4a).
        public const double TopBarHeightSteps = 52, TopBarHeightSeal = 74, TopBarHeightFree = TopBarHeight;
        public const double SealRowGap = 5, SealRowHeight = 17, SealRowPadH = 12, InfoRowPadBottom = 8;

        // El reloj de pared (series_panels.dart:92-103; 04-design-notes §3.4): 10 a la derecha de la cuenta, el ícono schedule de
        // 16 al 70 % con 5 de aire.
        public const double WallClockGap = 10, WallClockIcon = 16, WallClockIconGap = 5, WallClockIconAlpha = 0.70;

        // El chip de fase (immersive_workout_screen.dart:536-547; 04-design-notes §3.5): 10 del reloj, 8 × 3 de relleno, radio 8,
        // 11/700 con 0,08 em, el tinte al 15 % y el borde al 50 % del color base; el punto de 6 y su aire de 4 solo en EN MARCHA.
        public const double PhaseChipGap = 10, PhaseChipPadH = 8, PhaseChipPadV = 3, PhaseChipRadius = 8, PhaseChipText = 11;
        public const double PhaseChipTrackingEm = 0.08, PhaseChipTintAlpha = 0.15, PhaseChipBorderAlpha = 0.50, PhaseChipDotSlot = 10;
        public const double LiveDotSize = 6, LiveDotGap = 4;

        // El disco del lugar centro alto (DT4b-04, DN4b-03, -14): 72 con la cifra num.hero de 64, el aro de 3 (línea central a
        // 34,5) con la pista al 20 % y 8 de aire con lo de arriba y con la zona del bote. PAUSA en el mismo disco: el glifo de 28,
        // 4 de aire y la etiqueta 12/700 con 0,08 em.
        public const double BigCountDiameter = 72, BigCountDigit = 64, BigCountRingStroke = 3, BigCountRingTrackAlpha = 0.20, BigCountGap = 8;
        public const double PausedGlyphDp = 28, PausedGlyphGap = 4, PausedLabelSize = 12, PausedLabelTrackingEm = 0.08;

        // El techo del panel en el entrenamiento (DN4b-23), 8 dp debajo del disco o de la barra de 74·s; el diálogo de completado
        // (DN4b-21): el check de 32 y la m de sufijo a 3.
        public const double PanelCeilingGap = 8, CompletionCheckDp = 32, CompletionUnitGapDp = 3;

        // El carril izquierdo de la llave (R10): x 16-56 desde el margen izquierdo.
        public static double LeftLaneLeft(SafeInsets insets)
        {
            return insets.Left + WrenchEdge;
        }

        // La línea central del aro: (72 − 3) / 2 = 34,5 (× s).
        public static double BigCountRingRadius => (BigCountDiameter - BigCountRingStroke) / 2;

        // clamp(lado corto / 390; 0,9; 1,6): 0,923 en L640 y P360, 1,054 en TV y TH, 1,6 en W1280 (ART_BIBLE §5.3).
        public static double Scale(double width, double height)
        {
            var scale = Math.Min(width, height) / ScaleShortSide;
            return scale < MinScale ? MinScale : scale > MaxScale ? MaxScale : scale;
        }

        // 0,40 del alto en vertical, 0,42 en horizontal (ART_BIBLE §8.1): el fondo y la cámara.
        public static double Horizon(double width, double height)
        {
            return width > height ? HorizonLandscape : HorizonPortrait;
        }

        // El centro de la zona del bote, como fracción del alto: h₀ + (lado corto / alto) × 2,2 / 9,5, el punto donde se
        // proyecta el remero con la cámara de persecución del legacy (focal = lado corto): 0,504 en vertical, 0,652 en
        // horizontal (04-design-notes §3.6).
        public static double BoatCenterY(double width, double height)
        {
            return Horizon(width, height) + Math.Min(width, height) / height * (CameraHeightM / RowerDistanceM);
        }

        // Alto base de una tarjeta: relleno + rótulo (caja de línea) + 2 + valor + relleno (+ 2 + `obj.` con objetivo).
        public static double CardHeight(double valueSize, double labelSize, double padV, bool withTarget)
        {
            return padV + labelSize * LineBox + LabelGap + valueSize + padV + (withTarget ? TargetGap + TargetSize * LineBox : 0);
        }

        public static HudLayout Layout(in HudLayoutInput input)
        {
            var w = input.Width;
            var h = input.Height;
            var insets = input.Insets;
            var n = input.Natural;
            var s = Scale(w, h);
            var landscape = w > h;
            var top = insets.Top;
            var bottomInset = insets.Bottom;

            var topBar = DpRect.FromEdges(TopBarSide * s, top + TopBarTop * s, w - TopBarSide * s, top + (TopBarTop + TopBarHeight) * s);
            var controlsWidth = (FinishMinWidth + ControlsGap + PauseMinWidth) * s;
            var controlsBottom = h - bottomInset - ControlsBottom * s;
            var controls = DpRect.FromEdges((w - controlsWidth) / 2, controlsBottom - ControlsHeight * s, (w + controlsWidth) / 2, controlsBottom);
            var pauseRight = controls.Right;

            var center = BoatCenterY(w, h);
            var boat = DpRect.FromEdges(w * (0.5 - BoatZoneWidth / 2), h * (center - BoatZoneHeight / 2), w * (0.5 + BoatZoneWidth / 2),
                h * (center + BoatZoneHeight / 2));

            // La llave (04-design-notes §3.9; DN4a-14), por ventana.
            DpRect? wrench = null;
            var smallHeight = CardHeight(SmallValue, SmallLabel, SmallPadV, false) * s;
            if (input.ReserveWrench)
            {
                if (landscape)
                {
                    var room = (w - Side * s) - (pauseRight + WrenchGap + WrenchSize + WrenchGap);
                    if (room >= (2 * SmallCardMinWidth + CardGap) * s)
                    {
                        // (a) 8 dp a la derecha de Pausa, centrada en el alto de los controles (TH, W1280).
                        var middle = controls.CenterY;
                        wrench = DpRect.FromEdges(pauseRight + WrenchGap, middle - WrenchSize / 2, pauseRight + WrenchGap + WrenchSize, middle + WrenchSize / 2);
                    }
                    else
                    {
                        // (b) 8 dp sobre Pausa, alineada con su borde derecho (L640).
                        wrench = DpRect.FromEdges(pauseRight - WrenchSize, controls.Top - WrenchGap - WrenchSize, pauseRight, controls.Top - WrenchGap);
                    }
                }
                else
                {
                    // Carril derecho, 8 dp sobre la columna de kcal y BPM tal como queda CON BPM (no se mueve con el pulso).
                    var x0 = w - insets.Right - WrenchEdge - WrenchSize;
                    var columnTop = h - PortraitBottom * s - 2 * smallHeight - CardGap * s;
                    wrench = DpRect.FromEdges(x0, columnTop - WrenchGap - WrenchSize, x0 + WrenchSize, columnTop - WrenchGap);
                }
            }

            // Fila de arriba (T1, T2).
            var spm = new Card(LargeValue, LargeLabel, LargeMinWidth, LargePadH, LargePadV, n.SpmValue, n.SpmLabel,
                input.SpmTarget ? n.SpmTarget : 0, input.SpmTarget);
            var split = new Card(MediumValue, MediumLabel, MediumMinWidth, MediumPadH, MediumPadV, n.SplitValue, n.SplitLabel,
                input.SplitTarget ? n.SplitTarget : 0, input.SplitTarget);
            var watts = new Card(MediumValue, MediumLabel, MediumMinWidth, MediumPadH, MediumPadV, n.WattsValue, n.WattsLabel,
                input.WattsTarget ? n.WattsTarget : 0, input.WattsTarget);
            var left = Side * s;
            var right = w - Side * s;
            var cardsTop = top + CardsTop * s;
            var wattsTop = cardsTop + split.Height * s + CardGap * s;
            var spmBottom = cardsTop + spm.Height * s;
            var wattsBottom = wattsTop + watts.Height * s;
            var spmNatural = spm.Width(1.0, true) * s;
            var columnNatural = Math.Max(split.Width(1.0, true), watts.Width(1.0, true)) * s;
            var rowRoom = right - left - CardGap * s;
            var spmMax = spmNatural;
            var columnMax = columnNatural;
            if (spmNatural + columnNatural > rowRoom)
            {
                var k = rowRoom / (spmNatural + columnNatural);
                spmMax = spmNatural * k;
                columnMax = columnNatural * k;
            }

            if (DpRect.FromEdges(0, cardsTop, 1, spmBottom).OverlapsVertically(boat))
            {
                spmMax = Math.Min(spmMax, boat.Left - left);
            }

            var columnLeft = right - columnMax;
            if (DpRect.FromEdges(0, cardsTop, 1, wattsBottom).OverlapsVertically(boat))
            {
                columnLeft = Math.Max(columnLeft, boat.Right);
            }

            columnMax = right - columnLeft;
            var spmScale = spm.FitFor(spmMax / s);
            var spmRect = DpRect.FromEdges(left, cardsTop, left + spm.Width(spmScale, true) * s, spmBottom);
            var splitScale = split.FitFor(columnMax / s);
            var splitWidth = split.Width(splitScale, true) * s;
            var splitRect = DpRect.FromEdges(right - splitWidth, cardsTop, right, cardsTop + split.Height * s);
            var wattsScale = watts.FitFor(columnMax / s);
            var wattsWidth = watts.Width(wattsScale, true) * s;
            var wattsRect = DpRect.FromEdges(right - wattsWidth, wattsTop, right, wattsBottom);

            var wattsWithTargetBottom = top + (CardsTop + CardHeight(MediumValue, MediumLabel, MediumPadV, false) + CardGap
                + CardHeight(MediumValue, MediumLabel, MediumPadV, true)) * s;

            // kcal y BPM (T4 en horizontal, T5 en vertical).
            var kcal = new Card(SmallValue, SmallLabel, SmallCardMinWidth, SmallPadH, SmallPadV, n.KcalValue, n.KcalLabel, 0, false);
            var bpm = new Card(SmallValue, SmallLabel, SmallCardMinWidth, SmallPadH, SmallPadV, n.BpmValue, n.BpmLabel, 0, false);
            var gap = CardGap * s;
            DpRect kcalRect;
            DpRect? bpmRect = null;
            double vitalsLeft;
            var vitalsScale = 1.0;
            var vitalsMinWidth = false;
            HudZone vitalsZone;
            if (landscape)
            {
                var leftLimit = Math.Max(controls.Right + CardGap * s, wrench.HasValue ? wrench.Value.Right + WrenchGap : 0.0);
                var available = right - leftLimit;
                // Mínimos si entran los dos (también sin BPM: la tarjeta no cambia de modo cuando el pulso aparece o se va).
                var withMinimum = (kcal.Width(1.0, true) + bpm.Width(1.0, true)) * s + gap;
                vitalsMinWidth = withMinimum <= available;
                var count = input.HasBpm ? 2 : 1;
                var gaps = gap * (count - 1);
                if (!vitalsMinWidth)
                {
                    var natural = (kcal.Width(1.0, false) + (input.HasBpm ? bpm.Width(1.0, false) : 0)) * s + gaps;
                    if (natural > available)
                    {
                        double lo = 0, hi = 1;
                        for (var i = 0; i < 60; i++)
                        {
                            var mid = (lo + hi) / 2;
                            if ((kcal.Width(mid, false) + (input.HasBpm ? bpm.Width(mid, false) : 0)) * s + gaps <= available)
                            {
                                lo = mid;
                            }
                            else
                            {
                                hi = mid;
                            }
                        }

                        vitalsScale = lo;
                    }
                }

                var rowBottom = h - bottomInset - ControlsBottom * s;
                var x = right;
                var kcalWidth = kcal.Width(vitalsScale, vitalsMinWidth) * s;
                kcalRect = DpRect.FromEdges(x - kcalWidth, rowBottom - kcal.Height * s, x, rowBottom);
                x -= kcalWidth + gap;
                if (input.HasBpm)
                {
                    var bpmWidth = bpm.Width(vitalsScale, vitalsMinWidth) * s;
                    bpmRect = DpRect.FromEdges(x - bpmWidth, rowBottom - bpm.Height * s, x, rowBottom);
                    x -= bpmWidth + gap;
                }

                vitalsLeft = x + gap;
                vitalsZone = new HudZone(right, rowBottom, HudCorner.BottomRight, available / s);
            }
            else
            {
                var columnBottom = h - PortraitBottom * s;
                var columnWidth = (input.HasBpm ? Math.Max(kcal.Width(1.0, true), bpm.Width(1.0, true)) : kcal.Width(1.0, true)) * s;
                var y = columnBottom;
                if (input.HasBpm)
                {
                    bpmRect = DpRect.FromEdges(right - columnWidth, y - bpm.Height * s, right, y);
                    y -= bpm.Height * s + gap;
                }

                kcalRect = DpRect.FromEdges(right - columnWidth, y - kcal.Height * s, right, y);
                vitalsLeft = right - columnWidth;
                vitalsZone = new HudZone(right, columnBottom, HudCorner.BottomRight, columnWidth / s);
            }

            // Banda (T3): compacta en horizontal; las dos cifras con una sola escala (FitScaleGroup, DN4a-01).
            var padH = landscape ? BandPadHLandscape : BandPadHPortrait;
            var dividerGap = landscape ? BandDividerGapLandscape : BandDividerGapPortrait;
            var bandNatural = (padH + n.BandTime + dividerGap + BandDivider + dividerGap + n.BandDistance + padH) * s;
            var bandHeight = BandHeight * s;
            var bandBottom = landscape ? h - bottomInset - ControlsBottom * s : h - PortraitBottom * s;
            var limit = landscape ? controls.Left - CardGap * s : vitalsLeft - CardGap * s;
            if (DpRect.FromEdges(0, bandBottom - bandHeight, 1, bandBottom).OverlapsVertically(boat))
            {
                limit = Math.Min(limit, boat.Left);
            }

            var cap = limit - left;
            var innerNatural = (n.BandTime + n.BandDistance) * s;
            var fixedPart = (padH + dividerGap + BandDivider + dividerGap + padH) * s;
            var bandScale = bandNatural <= cap ? 1.0 : innerNatural > 0 ? Math.Max(0.0, (cap - fixedPart) / innerNatural) : 1.0;
            var bandRect = DpRect.FromEdges(left, bandBottom - bandHeight, left + Math.Min(bandNatural, cap), bandBottom);

            var topBarZone = new HudZone(TopBarSide * s, top + TopBarTop * s, HudCorner.TopLeft, (w - 2 * TopBarSide * s) / s);
            var spmZone = new HudZone(left, cardsTop, HudCorner.TopLeft, spmMax / s);
            var rightZone = new HudZone(right, cardsTop, HudCorner.TopRight, columnMax / s);
            var bandZone = new HudZone(left, bandBottom, HudCorner.BottomLeft, cap / s);
            var controlsZone = new HudZone(w / 2, controlsBottom, HudCorner.BottomCenter, w / s);

            // It. 4b: la barra en su alto máximo y el disco del lugar centro alto (§3.5; DN4b-14). Lo de arriba se mide con SPM,
            // Split y Watts CON su objetivo (así el disco no se mueve al cambiar de paso).
            var topBarMax = DpRect.FromEdges(TopBarSide * s, top + TopBarTop * s, w - TopBarSide * s, top + (TopBarTop + TopBarHeightSeal) * s);
            var spmTargeted = new Card(LargeValue, LargeLabel, LargeMinWidth, LargePadH, LargePadV, n.SpmValue, n.SpmLabel, n.SpmTarget, true);
            var splitTargeted = new Card(MediumValue, MediumLabel, MediumMinWidth, MediumPadH, MediumPadV, n.SplitValue, n.SplitLabel, n.SplitTarget, true);
            var wattsTargeted = new Card(MediumValue, MediumLabel, MediumMinWidth, MediumPadH, MediumPadV, n.WattsValue, n.WattsLabel, n.WattsTarget, true);
            var spmAbove = DpRect.FromEdges(left, cardsTop, Math.Max(spmRect.Right, left + spmTargeted.Width(spmTargeted.FitFor(spmMax / s), true) * s),
                cardsTop + spmTargeted.Height * s);
            var splitAbove = DpRect.FromEdges(Math.Min(splitRect.Left, right - splitTargeted.Width(splitTargeted.FitFor(columnMax / s), true) * s),
                cardsTop, right, cardsTop + splitTargeted.Height * s);
            var wattsAboveTop = cardsTop + splitTargeted.Height * s + CardGap * s;
            var wattsAbove = DpRect.FromEdges(Math.Min(wattsRect.Left, right - wattsTargeted.Width(wattsTargeted.FitFor(columnMax / s), true) * s),
                wattsAboveTop, right, wattsAboveTop + wattsTargeted.Height * s);
            var diameter = BigCountDiameter * s;
            var discLeft = (w - diameter) / 2;
            var discRight = discLeft + diameter;
            var above = topBarMax.Bottom;
            var aside = BigCountGap * s;
            foreach (var card in new[] { spmAbove, splitAbove, wattsAbove })
            {
                if (card.Left - aside < discRight && discLeft < card.Right + aside && card.Bottom > above)
                {
                    above = card.Bottom;
                }
            }

            var bandTop = above + aside;
            var bandBottomLimit = boat.Top - aside;
            var bigCountFits = bandBottomLimit - bandTop >= diameter - 1e-9;
            var discCenter = bigCountFits ? (bandTop + bandBottomLimit) / 2 : bandTop + diameter / 2;
            var bigCount = new DpRect(discLeft, discCenter - diameter / 2, diameter, diameter);

            // El techo del panel (DN4b-23): 8 dp debajo del disco en vertical, de la barra de 74·s en horizontal.
            double? ceiling = (landscape ? topBarMax.Bottom : Math.Max(bigCount.Bottom, wattsWithTargetBottom)) + PanelCeilingGap;

            // Las piezas del carril izquierdo (R10): las que se cruzan con x 16-56, agrandado 8.
            var laneLeft = LeftLaneLeft(insets);
            var lane = DpRect.FromEdges(laneLeft, 0, laneLeft + WrenchSize, h);
            var obstacles = new System.Collections.Generic.List<DpRect>(8);
            foreach (var piece in new DpRect?[] { topBarMax, spmAbove, splitAbove, wattsAbove, kcalRect, bpmRect, bandRect, controls })
            {
                if (piece.HasValue && piece.Value.Intersects(lane, WrenchGap))
                {
                    obstacles.Add(piece.Value);
                }
            }

            return new HudLayout(w, h, s, landscape, topBarZone, spmZone, rightZone, vitalsZone, bandZone, controlsZone, topBar, spmRect, splitRect,
                wattsRect, kcalRect, bpmRect, bandRect, controls, boat, wrench, ceiling, wattsWithTargetBottom, Horizon(w, h), landscape,
                vitalsMinWidth, spmScale, splitScale, wattsScale, vitalsScale, bandScale, topBarMax, bigCount, bigCountFits, obstacles);
        }

        // Alto de la barra superior (base) para la fila de pasos, con o sin sello, o la libre (AC-WKT-14-01).
        public static double TopBarHeightFor(bool routine, bool seal)
        {
            return !routine ? TopBarHeightFree : seal ? TopBarHeightSeal : TopBarHeightSteps;
        }

        // Una tarjeta a tamaño base (04-design-notes §3.2): ancho = max(rótulo, `obj.`, (hueco + valor) × escala) + 2 × relleno,
        // con el mínimo si corresponde; alto fijo. La escala de su FitBox es la del renglón del valor (el hueco de la flecha de
        // 0,48 em, las cifras y la unidad), como hud_layout.py.
        private readonly struct Card
        {
            private readonly double _minWidth;
            private readonly double _padH;
            private readonly double _value;
            private readonly double _label;
            private readonly double _target;
            private readonly double _gutter;

            public Card(double valueSize, double labelSize, double minWidth, double padH, double padV, double value, double label,
                        double target, bool withTarget)
            {
                _minWidth = minWidth;
                _padH = padH;
                _value = value;
                _label = label;
                _target = target;
                _gutter = withTarget ? ArrowGutterEm * valueSize : 0;
                Height = CardHeight(valueSize, labelSize, padV, withTarget);
            }

            public double Height { get; }

            public double Width(double fit, bool useMinimum)
            {
                var inner = Math.Max(Math.Max(_label, _target), (_gutter + _value) * fit);
                var width = inner + 2 * _padH;
                return useMinimum && _minWidth > width ? _minWidth : width;
            }

            // La escala más grande ≤ 1 con la que la tarjeta mide ≤ maxWidth (dp base); 0 si ni con las cifras en 0 entra.
            public double FitFor(double maxWidth)
            {
                if (Width(1.0, true) <= maxWidth + 1e-9)
                {
                    return 1.0;
                }

                var row = _gutter + _value;
                if (row <= 0 || Width(0.0, true) > maxWidth)
                {
                    return 0.0;
                }

                var fit = (maxWidth - 2 * _padH) / row;
                return fit > 1 ? 1 : fit < 0 ? 0 : fit;
            }
        }
    }
}
