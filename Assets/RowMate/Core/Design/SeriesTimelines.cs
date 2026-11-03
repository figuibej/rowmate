using System;

namespace RowMate.Core.Design
{
    // Un resorte evaluado: la posición y la velocidad (por segundo) en ese instante.
    public readonly struct SpringValue
    {
        public SpringValue(double value, double velocity)
        {
            Value = value;
            Velocity = velocity;
        }

        public double Value { get; }

        public double Velocity { get; }
    }

    // Desde dónde arranca abrir o cerrar un panel (§6.1 de 04-design-notes; AC-WKT-18-18): el alto con su velocidad (un toque
    // en medio invierte el resorte desde ahí) y cada fundido con su valor. Closed / Open: un panel quieto.
    public readonly struct PanelToggleStart
    {
        public PanelToggleStart(double heightBase, double velocity, double contentAlpha, double chipLabelAlpha, double titleAlpha,
                                double chevronDegrees, double chevronDownAlpha, double chevronUpAlpha)
        {
            HeightBase = heightBase;
            Velocity = velocity;
            ContentAlpha = contentAlpha;
            ChipLabelAlpha = chipLabelAlpha;
            TitleAlpha = titleAlpha;
            ChevronDegrees = chevronDegrees;
            ChevronDownAlpha = chevronDownAlpha;
            ChevronUpAlpha = chevronUpAlpha;
        }

        public double HeightBase { get; }

        // base por segundo.
        public double Velocity { get; }

        public double ContentAlpha { get; }

        public double ChipLabelAlpha { get; }

        public double TitleAlpha { get; }

        public double ChevronDegrees { get; }

        public double ChevronDownAlpha { get; }

        public double ChevronUpAlpha { get; }

        // Cerrado y quieto: el chip, el rótulo a la vista, la flecha ∨.
        public static PanelToggleStart Closed(double heightBase = SeriesPanelGeometry.ChipHeight)
        {
            return new PanelToggleStart(heightBase, 0, 0, 1, 0, 0, 1, 0);
        }

        // Abierto y quieto: el contenido y el título a la vista, la flecha ∧ (el trazo ∨ girado 180°).
        public static PanelToggleStart Open(double heightBase)
        {
            return new PanelToggleStart(heightBase, 0, 1, 0, 1, 180, 1, 0);
        }
    }

    // Abrir o cerrar (§6.1; reducir movimiento, §6.8): el alto (el resorte), el contenido, el rótulo corto y el título que se
    // cruzan, y la flecha. La flecha: sin reducir movimiento, el trazo ∨ girado ChevronDegrees con ChevronDownAlpha (1); con
    // reducir movimiento no gira (0°): el trazo ∨ con ChevronDownAlpha y el ∧ con ChevronUpAlpha. La cola (la línea Rep)
    // queda en su lugar final al abrir o cerrar (TailShiftBase 0: la revela el vidrio).
    public readonly struct PanelToggleVisual
    {
        public PanelToggleVisual(double heightBase, double velocity, double contentAlpha, double chipLabelAlpha, double titleAlpha,
                                 double chevronDegrees, double chevronDownAlpha, double chevronUpAlpha)
        {
            HeightBase = heightBase;
            Velocity = velocity;
            ContentAlpha = contentAlpha;
            ChipLabelAlpha = chipLabelAlpha;
            TitleAlpha = titleAlpha;
            ChevronDegrees = chevronDegrees;
            ChevronDownAlpha = chevronDownAlpha;
            ChevronUpAlpha = chevronUpAlpha;
        }

        public double HeightBase { get; }

        public double Velocity { get; }

        public double ContentAlpha { get; }

        public double ChipLabelAlpha { get; }

        public double TitleAlpha { get; }

        public double ChevronDegrees { get; }

        public double ChevronDownAlpha { get; }

        public double ChevronUpAlpha { get; }

        // Este instante como punto de partida de una inversión (un toque en medio).
        public PanelToggleStart ToStart()
        {
            return new PanelToggleStart(HeightBase, Velocity, ContentAlpha, ChipLabelAlpha, TitleAlpha, ChevronDegrees, ChevronDownAlpha,
                ChevronUpAlpha);
        }
    }

    // Las filas cuando entra una vuelta o una repetición (§6.2; DD5-17, DD5-24): el empuje de las que estaban (−22·k → 0),
    // su progreso (la escala común llega a su valor nuevo con él), la fila nueva (opacidad y −8 → 0) y la que se va.
    public readonly struct RowsVisual
    {
        public RowsVisual(double rowsPushBase, double pushProgress, double newRowsAlpha, double newRowsShiftBase, double droppedAlpha, int newRows)
        {
            RowsPushBase = rowsPushBase;
            PushProgress = pushProgress;
            NewRowsAlpha = newRowsAlpha;
            NewRowsShiftBase = newRowsShiftBase;
            DroppedAlpha = droppedAlpha;
            NewRows = newRows;
        }

        public double RowsPushBase { get; }

        public double PushProgress { get; }

        public double NewRowsAlpha { get; }

        public double NewRowsShiftBase { get; }

        // La propia de la que se va (además, la vista le aplica la regla 2 de la ventana de filas).
        public double DroppedAlpha { get; }

        public int NewRows { get; }
    }

    // El cambio de contenido, por pieza (§6.3; DD5-18): lo viejo que sale, lo nuevo que entra subiendo, y las filas que se
    // vacían (salen en su lugar).
    public readonly struct SwapVisual
    {
        public SwapVisual(double oldAlpha, double newAlpha, double newRiseBase, double clearedRowsAlpha)
        {
            OldAlpha = oldAlpha;
            NewAlpha = newAlpha;
            NewRiseBase = newRiseBase;
            ClearedRowsAlpha = clearedRowsAlpha;
        }

        public double OldAlpha { get; }

        public double NewAlpha { get; }

        public double NewRiseBase { get; }

        public double ClearedRowsAlpha { get; }
    }

    // Un fundido con su subida: el panel de repeticiones que aparece o se va (la subida en base del HUD: × s) y la línea Rep
    // que entra o sale con la serie (en base del panel).
    public readonly struct FadeRiseVisual
    {
        public FadeRiseVisual(double opacity, double riseBase)
        {
            Opacity = opacity;
            RiseBase = riseBase;
        }

        public double Opacity { get; }

        public double RiseBase { get; }
    }

    // Lo que dibuja un panel en un cuadro (02-tech-design §4.2; lo arma SeriesPanelMotion, WP-D1, con estas líneas de tiempo):
    // el alto visual G, la cola (G − H_final por un dato; 0 al abrir o cerrar), los fundidos de abrir y cerrar, la flecha, el
    // empuje y las filas que entran y salen, las marcas (una por fila nueva), la pieza que cambia de clase, la línea Rep que
    // entra o sale con la serie y la capa de hover. Las reglas de alfa por la tinta no son de la motion: las aplica la vista
    // con G, TailShiftBase y el desplazamiento (SeriesPanelGeometry). Sin asignar.
    public readonly struct SeriesPanelVisual
    {
        public double HeightBase { get; init; }

        public double TailShiftBase { get; init; }

        public double ContentAlpha { get; init; }

        public double ChipLabelAlpha { get; init; }

        public double TitleAlpha { get; init; }

        public double ChevronDegrees { get; init; }

        public double ChevronDownAlpha { get; init; }

        public double ChevronUpAlpha { get; init; }

        public double RowsPushBase { get; init; }

        public double PushProgress { get; init; }

        public double NewRowsAlpha { get; init; }

        public double NewRowsShiftBase { get; init; }

        public int NewRows { get; init; }

        public double DroppedAlpha { get; init; }

        public double ClearedRowsAlpha { get; init; }

        public double MarkerAlpha0 { get; init; }

        public double MarkerAlpha1 { get; init; }

        public double MarkerAlpha2 { get; init; }

        public double MarkerAlpha3 { get; init; }

        public double BodyOldAlpha { get; init; }

        public double BodyNewAlpha { get; init; }

        public double BodyNewRiseBase { get; init; }

        public double RepLineAlpha { get; init; }

        public double RepLineRiseBase { get; init; }

        public double HoverAlpha { get; init; }

        // Quieto en su estado final: sin empuje, sin marcas, sin piezas que cambian y sin hover (el panel que vuelve a un
        // panel de UI Toolkit, la rotación, Instant).
        public static SeriesPanelVisual Rest(double heightBase, bool expanded)
        {
            return new SeriesPanelVisual
            {
                HeightBase = heightBase,
                ContentAlpha = expanded ? 1 : 0,
                ChipLabelAlpha = expanded ? 0 : 1,
                TitleAlpha = expanded ? 1 : 0,
                ChevronDegrees = expanded ? 180 : 0,
                ChevronDownAlpha = 1,
                PushProgress = 1,
                NewRowsAlpha = 1,
                DroppedAlpha = 0,
                ClearedRowsAlpha = 0,
                BodyNewAlpha = 1,
                RepLineAlpha = 1,
            };
        }
    }

    // Lo que mueve la columna entera (02-tech-design §4.2; SeriesColumnMotion, WP-D1): el panel de repeticiones que aparece o
    // se va (su opacidad y su subida, en base del HUD), la columna que sigue a SPM (en base del HUD) y el desplazamiento de
    // ColumnScroll (en base del panel).
    public readonly struct SeriesColumnVisual
    {
        public SeriesColumnVisual(double repsAlpha, double repsRiseBase, double followBase, double scrollBase)
        {
            RepsAlpha = repsAlpha;
            RepsRiseBase = repsRiseBase;
            FollowBase = followBase;
            ScrollBase = scrollBase;
        }

        public double RepsAlpha { get; }

        public double RepsRiseBase { get; }

        public double FollowBase { get; }

        public double ScrollBase { get; }
    }

    // Las líneas de tiempo puras de los paneles de series (05-series-parciales/02-tech-design §4.1; 04-design-notes §6, que
    // revisa 00-deuda-diseno §4.1 a §4.6). Funciones de los MS ENTEROS desde el comienzo (HudTimelines.Ms, Reached; QA-G4bT-07
    // (b)), con la variante de reducir movimiento; devuelven structs y no asignan. Dos clases de movimiento (SD-12): lo que
    // hace el usuario (abrir, cerrar y su inversión) con spring.smooth desde su posición y su velocidad; lo que cambia el
    // dato, con curvas de duración fija que terminan antes del segundo siguiente (el alto por un dato, con el empuje). Nada de
    // esto decide cuándo pasa un valor (AC-WKT-13-14, AC-WKT-18-08). Las prueba SeriesTimelinesTests contra las tablas de
    // 04-design-notes §6.1 a §6.8 y §6.10.
    public static class SeriesTimelines
    {
        // Las inversiones: un fundido que no está en su valor de partida sigue desde donde está, en el acto, con la parte de su
        // duración que le falta (como una transición de USS).
        private const double FadeEpsilon = 1e-9;

        // Un resorte crítico (ζ = 1) desde (from, v0) hacia to, a los seconds: la forma cerrada de CriticalSpring.
        public static SpringValue Spring(double fromBase, double v0, double toBase, double omega, double seconds)
        {
            var spring = new CriticalSpring(fromBase, v0);
            spring.Step(seconds, toBase, omega);
            return new SpringValue(spring.Position, spring.Velocity);
        }

        // ── Abrir y cerrar (§6.1, §6.8) ──

        // Abrir desde un panel cerrado y quieto (o desde v0, el resorte de un cierre que se invierte).
        public static PanelToggleVisual Open(long tMs, double fromBase, double toBase, double v0, bool reduced)
        {
            var start = PanelToggleStart.Closed(fromBase);
            return Open(tMs, new PanelToggleStart(fromBase, v0, start.ContentAlpha, start.ChipLabelAlpha, start.TitleAlpha, start.ChevronDegrees,
                start.ChevronDownAlpha, start.ChevronUpAlpha), toBase, reduced);
        }

        // Abrir: el alto con spring.smooth desde los 0 (con reducir movimiento, en el cuadro 0); el contenido 0 → 1 en 80-240
        // (ease.enter; con reducir movimiento, 0-160 lineal); el rótulo corto 1 → 0 en 0-80 (ease.exit) y el título 0 → 1 en
        // 80-200 (ease.enter); la flecha 0 → 180° en 240 (ease.move; con reducir movimiento, el ∨ funde 1 → 0 en 0-80 y el ∧
        // 0 → 1 en 80-160, lineales). Desde una inversión (start a medio camino), cada pieza sigue desde donde está.
        public static PanelToggleVisual Open(long tMs, in PanelToggleStart start, double toBase, bool reduced)
        {
            SpringValue height;
            double content;
            if (reduced)
            {
                height = new SpringValue(toBase, 0);
                content = FadeLinear(tMs, start.ContentAlpha, 1, 0, MotionSpecs.SeriesReducedInMs);
            }
            else
            {
                height = Spring(start.HeightBase, start.Velocity, toBase, MotionSpecs.SeriesHeightOmega, tMs / 1000.0);
                content = Fade(Tokens.EaseEnter, tMs, start.ContentAlpha, 1, MotionSpecs.SeriesContentInStartMs,
                    MotionSpecs.SeriesContentInEndMs - MotionSpecs.SeriesContentInStartMs);
            }

            var label = Fade(Tokens.EaseExit, tMs, start.ChipLabelAlpha, 0, 0, MotionSpecs.SeriesLabelOutMs);
            var title = Fade(Tokens.EaseEnter, tMs, start.TitleAlpha, 1, MotionSpecs.SeriesLabelInStartMs,
                MotionSpecs.SeriesLabelInEndMs - MotionSpecs.SeriesLabelInStartMs);
            Chevron(tMs, start, true, reduced, out var degrees, out var down, out var up);
            return new PanelToggleVisual(height.Value, height.Velocity, content, label, title, degrees, down, up);
        }

        // Cerrar desde un panel abierto y quieto (o desde v0).
        public static PanelToggleVisual Close(long tMs, double fromBase, double toBase, double v0, bool reduced)
        {
            var start = PanelToggleStart.Open(fromBase);
            return Close(tMs, new PanelToggleStart(fromBase, v0, start.ContentAlpha, start.ChipLabelAlpha, start.TitleAlpha, start.ChevronDegrees,
                start.ChevronDownAlpha, start.ChevronUpAlpha), toBase, reduced);
        }

        // Cerrar: el contenido 1 → 0 en 0-120 (ease.exit; con reducir movimiento, lineal) y el alto con spring.smooth desde los
        // 80 (si arranca quieto; desde una inversión, en el acto, con su velocidad). Con reducir movimiento, el alto a los 120.
        // El título 1 → 0 en 0-80 y el rótulo 0 → 1 en 80-200; la flecha 180 → 0° en 240 (con reducir movimiento, el ∧ funde
        // en 0-80 y el ∨ entra en 80-160).
        public static PanelToggleVisual Close(long tMs, in PanelToggleStart start, double toBase, bool reduced)
        {
            SpringValue height;
            double content;
            if (reduced)
            {
                height = tMs < MotionSpecs.SeriesReducedOutMs ? new SpringValue(start.HeightBase, 0) : new SpringValue(toBase, 0);
                content = FadeLinear(tMs, start.ContentAlpha, 0, 0, MotionSpecs.SeriesReducedOutMs);
            }
            else
            {
                var delay = Math.Abs(start.Velocity) > FadeEpsilon ? 0 : MotionSpecs.SeriesShrinkDelayMs;
                height = tMs <= delay
                    ? new SpringValue(start.HeightBase, start.Velocity)
                    : Spring(start.HeightBase, start.Velocity, toBase, MotionSpecs.SeriesHeightOmega, (tMs - delay) / 1000.0);
                content = Fade(Tokens.EaseExit, tMs, start.ContentAlpha, 0, 0, MotionSpecs.SeriesContentOutMs);
            }

            var title = Fade(Tokens.EaseExit, tMs, start.TitleAlpha, 0, 0, MotionSpecs.SeriesLabelOutMs);
            var label = Fade(Tokens.EaseEnter, tMs, start.ChipLabelAlpha, 1, MotionSpecs.SeriesLabelInStartMs,
                MotionSpecs.SeriesLabelInEndMs - MotionSpecs.SeriesLabelInStartMs);
            Chevron(tMs, start, false, reduced, out var degrees, out var down, out var up);
            return new PanelToggleVisual(height.Value, height.Velocity, content, label, title, degrees, down, up);
        }

        // Cuándo terminan los fundidos y la flecha (el resorte del alto termina cuando le faltan menos de 0,15 dp: lo mira la
        // motion, ISettlingMotion): abrir 240 (reducir movimiento 200); cerrar 240 (200).
        public static long ToggleFadesEndMs(bool reduced)
        {
            return (long)(reduced ? MotionSpecs.SeriesLabelInEndMs : MotionSpecs.SeriesChevronMs);
        }

        // ── El alto que cambia por un dato (§6.2, §6.3; DD5-16) ──

        // Crece en 0-160 y se achica en 80-240 (ease.move). Con reducir movimiento, en el acto: a los 0 si crece y a los 120 si
        // se achica.
        public static double DataResize(long tMs, double fromBase, double toBase, bool reduced)
        {
            if (toBase > fromBase)
            {
                return reduced ? toBase : HudTimelines.Lerp(fromBase, toBase, HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.SeriesDataGrowMs));
            }

            if (toBase < fromBase)
            {
                if (reduced)
                {
                    return tMs < MotionSpecs.SeriesReducedOutMs ? fromBase : toBase;
                }

                return HudTimelines.Lerp(fromBase, toBase, HudTimelines.Ease(Tokens.EaseMove, tMs, MotionSpecs.SeriesDataShrinkStartMs,
                    MotionSpecs.SeriesDataShrinkEndMs - MotionSpecs.SeriesDataShrinkStartMs));
            }

            return toBase;
        }

        public static long DataResizeEndMs(bool growing, bool reduced)
        {
            if (growing)
            {
                return reduced ? 0 : (long)MotionSpecs.SeriesDataGrowMs;
            }

            return (long)(reduced ? MotionSpecs.SeriesReducedOutMs : MotionSpecs.SeriesDataShrinkEndMs);
        }

        // La cola (la línea Rep y la ventana de filas) en un cambio por dato: G − H_final.
        public static double TailShift(double glassBase, double finalBase)
        {
            return glassBase - finalBase;
        }

        // Si el resorte del alto estaba corriendo (un toque menos de 0,6 s antes), las filas y la cola nunca van más rápido que
        // el vidrio: corrimiento = min(empuje; G − H_final) (§6.2).
        public static double FollowGlass(double pushBase, double glassBase, double finalBase)
        {
            return Math.Min(pushBase, glassBase - finalBase);
        }

        // ── La vuelta nueva y la repetición nueva (§6.2) ──

        // newRows: las que entraron en este segundo (k); dropped: había 4 vueltas (o 3 repeticiones) y se va la más vieja. El
        // empuje −22·k → 0 en 0-160 (ease.move); la fila nueva 0 → 1 en 80-240 (ease.enter) con −8 → 0; la que se va 1 → 0 en
        // 0-120 (ease.exit). Con reducir movimiento: todo en su lugar en el cuadro 0, la nueva funde 0 → 1 en 0-160 y la que se
        // va en 0-120, lineales.
        public static RowsVisual RowsChange(long tMs, int newRows, bool dropped, bool reduced)
        {
            var push = (SeriesPanelGeometry.RowHeight + SeriesPanelGeometry.RowGap) * newRows;
            if (reduced)
            {
                var newAlpha = HudTimelines.Linear(tMs, 0, MotionSpecs.SeriesReducedInMs);
                var droppedAlpha = dropped ? 1 - HudTimelines.Linear(tMs, 0, MotionSpecs.SeriesReducedOutMs) : 1;
                return new RowsVisual(0, 1, newAlpha, 0, droppedAlpha, newRows);
            }

            var progress = HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.SeriesRowsPushMs);
            var enter = HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.SeriesRowInStartMs, MotionSpecs.SeriesRowInEndMs - MotionSpecs.SeriesRowInStartMs);
            var gone = dropped ? 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.SeriesRowOutMs) : 1;
            return new RowsVisual(-push * (1 - progress), progress, enter, -MotionSpecs.SeriesRowInShiftBase * (1 - enter), gone, newRows);
        }

        public static long RowsChangeEndMs(bool reduced)
        {
            return (long)(reduced ? MotionSpecs.SeriesReducedInMs : MotionSpecs.SeriesRowInEndMs);
        }

        // La marca de la fila nueva (DT5-03): entra con su fila, queda en 1 hasta los 600 y se apaga en 600-1400 (ease.move).
        // Con reducir movimiento, igual (es opacidad): entra con el fundido de su fila.
        public static double Marker(long tMs, bool reduced)
        {
            var enter = reduced
                ? HudTimelines.Linear(tMs, 0, MotionSpecs.SeriesReducedInMs)
                : HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.SeriesRowInStartMs, MotionSpecs.SeriesRowInEndMs - MotionSpecs.SeriesRowInStartMs);
            var fade = 1 - HudTimelines.Ease(Tokens.EaseMove, tMs, MotionSpecs.SeriesMarkerOutStartMs,
                MotionSpecs.SeriesMarkerOutEndMs - MotionSpecs.SeriesMarkerOutStartMs);
            return enter * fade;
        }

        public static long MarkerEndMs => (long)MotionSpecs.SeriesMarkerOutEndMs;

        // ── El cambio de contenido, por pieza (§6.3; DD5-18) ──

        // changesClass: la pieza cambia de clase (la vuelta en curso ↔ el vacío; la lista de repeticiones ↔ su vacío): lo viejo
        // 1 → 0 en 0-120 (ease.exit) y lo nuevo 0 → 1 en 120-280 (ease.enter) subiendo +4 → 0. Si no (de un trabajo a otro, la
        // vuelta en curso cambia con su rodillo; entre dos pasos que no son de trabajo, el vacío no se mueve): nada. Las filas
        // que se vacían salen en su lugar en 0-120. Con reducir movimiento, los mismos fundidos sin subir.
        public static SwapVisual Swap(long tMs, bool changesClass, bool reduced)
        {
            var cleared = 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.SeriesSwapOutMs);
            if (!changesClass)
            {
                return new SwapVisual(0, 1, 0, cleared);
            }

            var old = 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.SeriesSwapOutMs);
            var fresh = HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.SeriesSwapInStartMs, MotionSpecs.SeriesSwapInEndMs - MotionSpecs.SeriesSwapInStartMs);
            return new SwapVisual(old, fresh, reduced ? 0 : MotionSpecs.SeriesSwapRiseBase * (1 - fresh), cleared);
        }

        public static long SwapEndMs => (long)MotionSpecs.SeriesSwapInEndMs;

        // La línea Rep que entra o sale con la serie (§6.3, §6.4): entra con lo nuevo (120-280, +4 → 0) y sale en 0-120. Con
        // reducir movimiento, sin subir.
        public static FadeRiseVisual RepLine(long tMs, bool appearing, bool reduced)
        {
            if (!appearing)
            {
                return new FadeRiseVisual(1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.SeriesSwapOutMs), 0);
            }

            var fresh = HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.SeriesSwapInStartMs, MotionSpecs.SeriesSwapInEndMs - MotionSpecs.SeriesSwapInStartMs);
            return new FadeRiseVisual(fresh, reduced ? 0 : MotionSpecs.SeriesSwapRiseBase * (1 - fresh));
        }

        // ── La serie que empieza y la que termina (§6.4) ──

        // El panel de repeticiones: aparece en su estado guardado, sin abrirse: opacidad 0 → 1 y +8 → 0 (en base del HUD: × s)
        // en 120-400 (ease.enter); se va 1 → 0 en 0-120 (ease.exit), sin moverse. Con reducir movimiento, solo opacidad.
        public static FadeRiseVisual RepsPanel(long tMs, bool appearing, bool reduced)
        {
            if (!appearing)
            {
                return new FadeRiseVisual(1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.SeriesRepsLeaveMs), 0);
            }

            var e = HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.SeriesRepsAppearStartMs,
                MotionSpecs.SeriesRepsAppearEndMs - MotionSpecs.SeriesRepsAppearStartMs);
            return new FadeRiseVisual(e, reduced ? 0 : MotionSpecs.SeriesRepsAppearRise * (1 - e));
        }

        public static long RepsPanelEndMs(bool appearing)
        {
            return (long)(appearing ? MotionSpecs.SeriesRepsAppearEndMs : MotionSpecs.SeriesRepsLeaveMs);
        }

        // ── La columna que sigue a SPM (§6.5) ──

        // Lo que SPM crece o se achica con su objetivo (obj. con su aire): 2 + 12 × 1,238 = 16,856 base (16,9·s).
        public static double SpmTargetGrowthBase =>
            HudGeometry.CardHeight(HudGeometry.LargeValue, HudGeometry.LargeLabel, HudGeometry.LargePadV, true)
            - HudGeometry.CardHeight(HudGeometry.LargeValue, HudGeometry.LargeLabel, HudGeometry.LargePadV, false);

        // El corrimiento de la columna (en base del HUD: × s), sobre su lugar final: al ganar obj. viene de −16,9 (arriba) a 0
        // en 0-240; al perderlo, de +16,9 a 0 en 80-280 (ease.move). Con reducir movimiento, en el acto.
        public static double ColumnFollow(long tMs, bool gaining, bool reduced)
        {
            if (reduced)
            {
                return 0;
            }

            var delta = SpmTargetGrowthBase;
            if (gaining)
            {
                return -delta * (1 - HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.SeriesFollowGainMs));
            }

            return delta * (1 - HudTimelines.Ease(Tokens.EaseMove, tMs, MotionSpecs.SeriesFollowLoseStartMs,
                MotionSpecs.SeriesFollowLoseEndMs - MotionSpecs.SeriesFollowLoseStartMs));
        }

        public static long ColumnFollowEndMs(bool gaining, bool reduced)
        {
            if (reduced)
            {
                return 0;
            }

            return (long)(gaining ? MotionSpecs.SeriesFollowGainMs : MotionSpecs.SeriesFollowLoseEndMs);
        }

        // ── El desplazamiento (§6.6) ──

        // La rueda, de a una fila (22·sp), con spring.snappy (ω 22) desde su posición y su velocidad. Con reducir movimiento,
        // en el acto.
        public static SpringValue Wheel(long tMs, double fromBase, double toBase, double v0, bool reduced)
        {
            return reduced ? new SpringValue(toBase, 0) : Spring(fromBase, v0, toBase, MotionSpecs.SeriesWheelOmega, tMs / 1000.0);
        }

        // Lo que se desplaza una fila con la rueda (en base del panel).
        public static double WheelStepBase => SeriesPanelGeometry.RowHeight + SeriesPanelGeometry.RowGap;

        // ── La capa de hover y presionado (§6.10; DD5-28) ──

        // Desde el alfa que tiene hacia el objetivo: subiendo en 90 (ease.enter), bajando en 160 (ease.exit), siempre con la
        // duración entera de su sentido. Igual con reducir movimiento (es opacidad).
        public static double Hover(long tMs, double from, double to)
        {
            if (to > from)
            {
                return HudTimelines.Lerp(from, to, HudTimelines.Ease(Tokens.EaseEnter, tMs, 0, MotionSpecs.SeriesHoverInMs));
            }

            if (to < from)
            {
                return HudTimelines.Lerp(from, to, HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.SeriesHoverOutMs));
            }

            return to;
        }

        public static long HoverEndMs(double from, double to)
        {
            return (long)(to > from ? MotionSpecs.SeriesHoverInMs : MotionSpecs.SeriesHoverOutMs);
        }

        // ── Lo común ──

        // Un fundido de from a to. Si arranca en su valor de partida (0 si entra, 1 si sale), la ventana entera (con su
        // demora); si no (una inversión), en el acto y con la parte de la duración que le falta.
        private static double Fade(EasingToken curve, long tMs, double from, double to, double startMs, double durationMs)
        {
            var span = Math.Abs(to - from);
            if (span <= FadeEpsilon)
            {
                return to;
            }

            var fresh = Math.Abs(span - 1) <= FadeEpsilon;
            var start = fresh ? startMs : 0;
            var duration = fresh ? durationMs : durationMs * span;
            return HudTimelines.Lerp(from, to, HudTimelines.Ease(curve, tMs, start, duration));
        }

        private static double FadeLinear(long tMs, double from, double to, double startMs, double durationMs)
        {
            return Fade(Tokens.EaseLinear, tMs, from, to, startMs, durationMs);
        }

        // La flecha: sin reducir movimiento gira (ease.move en 240, con lo que le falta); con reducir movimiento no gira: el trazo
        // que se va funde en 0-80 y el que llega en 80-160, lineales. Un arranque girado (reducir movimiento que se prendió a
        // medio giro) cuenta como la mezcla de los dos trazos.
        private static void Chevron(long tMs, in PanelToggleStart start, bool opening, bool reduced, out double degrees, out double down, out double up)
        {
            var target = opening ? 180.0 : 0.0;
            if (!reduced)
            {
                var span = Math.Abs(target - start.ChevronDegrees);
                var duration = MotionSpecs.SeriesChevronMs * span / 180.0;
                degrees = span <= FadeEpsilon ? target : HudTimelines.Lerp(start.ChevronDegrees, target, HudTimelines.Ease(Tokens.EaseMove, tMs, 0, duration));
                down = 1;
                up = 0;
                return;
            }

            var turned = start.ChevronDegrees / 180.0;
            var downStart = start.ChevronDownAlpha * (1 - turned);
            var upStart = start.ChevronUpAlpha + start.ChevronDownAlpha * turned;
            var outgoingStart = opening ? downStart : upStart;
            var incomingStart = opening ? upStart : downStart;
            var outgoing = FadeLinear(tMs, outgoingStart, 0, 0, MotionSpecs.SeriesReducedChevronOutMs);
            var incoming = FadeLinear(tMs, incomingStart, 1, MotionSpecs.SeriesReducedChevronInStartMs,
                MotionSpecs.SeriesReducedChevronInEndMs - MotionSpecs.SeriesReducedChevronInStartMs);
            degrees = 0;
            down = opening ? outgoing : incoming;
            up = opening ? incoming : outgoing;
        }
    }
}
