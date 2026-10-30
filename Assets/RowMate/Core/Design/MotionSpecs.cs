namespace RowMate.Core.Design
{
    // Números de motion que no son tokens (02-tech-design §2.9; ANIMATION_BIBLE §2.1, §2.4, §2.9; 04-design-notes §4.7, §5,
    // DN-07, -08, -10, -11, -12, -14, -15, -16). Las duraciones y curvas que SÍ son tokens se toman de Tokens (dur.micro 90,
    // dur.fast 160, dur.base 240, dur.slow 360; ease.*; shift.s 8, shift.m 12) y no se repiten acá. Milisegundos y em.
    public static class MotionSpecs
    {
        // ── Rodillo de dígitos (04-design-notes §5.1, DN-08; duración en DigitRoll.DurationMs = 180, ease.enter) ──
        // Desplazamiento de cada glifo, en em; el saliente se apaga en u ∈ [0; 0,6] y el entrante aparece en u ∈ [0,15; 1].
        public const double RollShiftEm = 0.35;
        public const double RollOutFadeEnd = 0.60;
        public const double RollInFadeStart = 0.15;

        // Reducir movimiento: fundido cruzado lineal en el lugar.
        public const double ReducedFadeMs = 80;

        // Celdas de ':' y '.' (el '-' usa su avance real en la fuente, DN-08 b).
        public const double ColonCellEm = 0.32;

        // ── Pestañas (04-design-notes §5.4, DN-11) ──
        // Contenido: sale 0-120 y entra 80-280. Ícono elegido: 1 → 1,12 (0-90, ease.enter) → 1 (90-260, ease.move).
        // Color del ícono y de la etiqueta: dur.fast 160 con ease.move, por código.
        public const double TabOutMs = 120;
        public const double TabInStartMs = 80;
        public const double TabInEndMs = 280;
        public const double TabIconBump = 1.12;
        public const double TabIconPeakMs = 90;
        public const double TabIconEndMs = 260;

        // Píldora de la barra: spring.snappy (ANIMATION_BIBLE §1.2).
        public const double PillOmega = CriticalSpring.SnappyOmega;

        // Escala de FitBox cuando un dato la cambia: spring.smooth (04-design-notes §5.9, DN-15).
        public const double FitScaleOmega = CriticalSpring.SmoothOmega;

        // ── Latido (04-design-notes §5.3, DN-10; HeartbeatClock) ──
        // s(τ) = 1 + 0,16·p₁(τ) + 0,06·p₂(τ). p₁ sube 0-90 (ease.enter) y baja 90-290 (ease.move); p₂ sube 170-240 (ease.enter)
        // y baja 240-390 (ease.move). Si T = 60/bpm < 420 ms, todos los tiempos × T/420. Parada: a 1 en 120 ms (ease.move).
        public const double BeatMainGain = 0.16;
        public const double BeatRiseMs = 90;
        public const double BeatFallMs = 200;
        public const double SecondBeatGain = 0.06;
        public const double SecondBeatOffsetMs = 170;
        public const double SecondBeatRiseMs = 70;
        public const double SecondBeatFallMs = 150;
        public const double BeatFullCycleMs = 420;
        public const double BeatStopMs = 120;

        // Regla de toma de las capturas (DN-21): el corazón está "en reposo" si |escala − 1| ≤ 0,002.
        public const double BeatRestTolerance = 0.002;

        // ── Acento "dato nuevo" (04-design-notes §5.2, DN-07; AccentPulse) ──
        // k = 1 − 0,4·E_pulse(t / 300): acento = color de la métrica con alfa k (0,60 en reposo, legacy withOpacity(0.6));
        // brillo = 0,14·k. La tarjeta del pulsómetro queda con k = 0,6 fijo (no destella).
        public const double AccentFlashMs = 300;
        public const double AccentRestAlpha = 0.60;
        public const double AccentGlowPeak = 0.14;

        // Opacidad del corazón sin conexión: [CONTRATO] heart_rate_card.dart:40 (withValues(alpha: 0.4)), por código (DN-13).
        public const double DimHeartOpacity = 0.40;

        // ── Vista conectada ↔ búsqueda (04-design-notes §5.5, DN-12) ──
        // Sale 0-120 (al "apagar", escala 0,985); entra 120-360 (shift.s); al "encender", la tarjeta i entra de 120 + 35·i a
        // +240 (shift.m).
        public const double ViewOutMs = 120;
        public const double ViewInEndMs = 360;
        public const double ViewOutScale = 0.985;
        public const double TileStaggerMs = 35;
        public const double TileInMs = 240;

        // ── Llave y panel del simulador (04-design-notes §5.7, DN-14) ──
        // Abrir: opacidad 160 y escala 0,92 → 1 en 240. Cerrar: el panel en el acto y la llave vuelve 0,85 → 1 en 160.
        public const double PanelInScaleFrom = 0.92;
        public const double FabInScaleFrom = 0.85;

        // ── Tarjeta PULSO (04-design-notes §5.9, DN-16): sale con opacidad en 120 y la grilla se recoge en 200 ──
        public const double PulseTileOutMs = 120;
        public const double PulseTileCollapseMs = 200;

        // ── Spinner (04-design-notes §4.7): arco de 270°, una vuelta en 900 ms, lineal ──
        public const double SpinnerTurnMs = 900;
        public const double SpinnerArcDegrees = 270;

        // ════ It. 3 — Rutinas y editor (03-rutinas/04-design-notes §5.0-§5.11, DN3-16). Los dueños de las animaciones son de
        // WP-B (Presentation/Motion: PageTransition, DialogTransition, ToastTransition, GrowMotion, CollapseMotion…). ════

        // ── Pila: abrir y cerrar el editor (§5.1). Entra x +24 → 0 en 0-240 (ease.enter) y opacidad 0 → 1 en 0-160; sale
        // x 0 → +24 en 0-200 (ease.exit) y opacidad 1 → 0 en 40-200. Lo de atrás se oscurece a 0,08. Reducir movimiento:
        // fundido de 120 (ease.linear) sin desplazamientos. ──
        public const double PageInMs = 240;
        public const double PageInFadeMs = 160;
        public const double PageOutMs = 200;
        public const double PageOutFadeStartMs = 40;
        public const double PageDimOpacity = 0.08;
        public const double ReducedPageFadeMs = 120;

        // shift.xl (24): el desplazamiento de la página.
        public static double PageShiftDp => Tokens.ShiftXl.Value;

        // ── Diálogos y velo (§5.2): velo 0 → 0,6 en 0-200 (ease.move); diálogo escala 0,96 → 1 y fundido en 0-220 (ease.enter);
        // sale a escala 0,98 y opacidad 0 en 150 (ease.exit). ──
        public const double DialogScrimOpacity = 0.6;
        public const double DialogScrimInMs = 200;
        public const double DialogInMs = 220;
        public const double DialogInScaleFrom = 0.96;
        public const double DialogOutMs = 150;
        public const double DialogOutScaleTo = 0.98;

        // ── Menú y ayuda (§5.3): menú escala 0,96 → 1 y fundido en 120 (ease.enter), sale en 90; ayuda: fundido 120 / 90. ──
        public const double MenuInMs = 120;
        public const double MenuInScaleFrom = 0.96;
        public const double MenuOutMs = 90;
        public const double TooltipInMs = 120;
        public const double TooltipOutMs = 90;

        // ── Aviso (§5.4, §4.12): entra 220, sale 160 bajando 8; reemplazo con empujón 1 → 1,02 (0-90) → 1 (90-240) y cruce
        // de texto (el viejo funde en 0-80, el nuevo entra en 80-200). Se aparta 8 dp de los obstáculos que se cruzan. ──
        public const double ToastInMs = 220;
        public const double ToastOutMs = 160;
        public const double ToastOutShiftDp = 8;
        public const double ToastBumpScale = 1.02;
        public const double ToastBumpPeakMs = 90;
        public const double ToastBumpEndMs = 240;
        public const double ToastTextOutMs = 80;
        public const double ToastTextInEndMs = 200;
        public const double BottomObstacleGapDp = 8;

        // ── Lista de rutinas (§5.5): la primera carga con la pestaña a la vista escalona 35 ms por tarjeta, hasta 8, y cada
        // una entra en 240; el vacío y el error suben shift.s (8). Indicador: gracia de 120 antes del fundido (DN3-15). ──
        public const double ListStaggerMs = 35;
        public const int ListStaggerMax = 8;
        public const double ListItemInMs = 240;
        public const double LoadingGraceMs = 120;

        // shift.s (8): el vacío y el error al entrar.
        public static double EmptyInShiftDp => Tokens.ShiftS.Value;

        // ── Crecer, colapsar y resaltar (§5.5, §5.6): lo nuevo crece (alto y márgenes con spring.smooth) y funde en 60-260;
        // lo borrado funde en 0-120 y colapsa en 80-280; la serie nueva escalona sus piezas cada 40; lo editado se ilumina
        // 0,08 → 0 en 600 (ease.move). Hojas de la serie: alto 0 ↔ 3 y opacidad en 160. ──
        public const double GrowOmega = CriticalSpring.SmoothOmega;
        public const double GrowFadeStartMs = 60;
        public const double GrowFadeEndMs = 260;
        public const double CollapseFadeMs = 120;
        public const double CollapseStartMs = 80;
        public const double CollapseEndMs = 280;
        public const double SeriesStaggerMs = 40;
        public const double HighlightPeak = 0.08;
        public const double HighlightMs = 600;
        public const double StackLayerMs = 160;

        // El vacío del editor (§5.6; QA-V3-02 de la It. 3): con el primer paso funde → 0 en 120 (ease.exit) en su lugar; con
        // el último que se va, entra (ListItemInMs, shift.s) recién cuando terminó el colapso de lo borrado.
        public const double EmptyOutMs = 120;

        // ── Campos y diálogo de paso (§5.7): la etiqueta flota con escala 0,8; el campo del modo que llega funde en 120. ──
        public const double FieldLabelScale = 0.8;
        public const double ModeSwapInMs = 120;

        // ── Llave, teclado y desplazamientos (§5.8): resortes críticos. Lo nuevo se muestra con ω 12; el primer error, el
        // foco por Tab y el teclado, con ω 22 (responden a una acción inmediata); 16 dp de aire. ──
        public const double KeyOmega = CriticalSpring.SnappyOmega;
        public const double KeyboardOmega = CriticalSpring.SnappyOmega;
        public const double ScrollToNewOmega = CriticalSpring.SmoothOmega;
        public const double ScrollToFocusOmega = CriticalSpring.SnappyOmega;
        public const double ScrollMarginDp = 16;

        // P2 (DN3-19): Guardar muestra un spinner de 16 si el guardado tarda más de 300 ms.
        public const double SaveSpinnerDelayMs = 300;

        // ════ It. 4a — HUD funcional (04a-motor-hud-funcional/04-design-notes §5, §5.13; DN4a-05, DN4a-06, DN4a-17). Los dueños
        // de las animaciones son de WP-B (Presentation/Motion: ImmersiveTransition, StepPulse, OffTargetMotion…). Se reusan sin
        // cambios DigitRoll.DurationMs (180, también el reloj y la cuenta), ReducedFadeMs, GrowOmega y los de CollapseMotion,
        // ToastTextOutMs / ToastTextInEndMs, FitScaleOmega, FabInScaleFrom y PanelInScaleFrom. Las de geometría van en
        // HudGeometry. ════

        // ── Fuera de objetivo (§5.3; VID-02): el color cruza en 150 (ease.move, también la rampa de "por terminar"); la flecha
        // entra en 120 (ease.enter) desde 4 dp en el sentido contrario al que indica y sale siguiendo 4 dp en su sentido; el
        // empujón 1 → 1,04 en 0-90 (ease.enter) → 1 en 90-240 (ease.move), como mucho uno cada 4 s por tarjeta y nunca al
        // empezar un paso. ──
        public const double HudColorMs = 150;
        public const double OffTargetArrowMs = 120;
        public const double OffTargetArrowShiftDp = 4;
        public const double OffTargetBumpScale = 1.04;
        public const double OffTargetBumpPeakMs = 90;
        public const double OffTargetBumpEndMs = 240;
        public const double OffTargetBumpCooldownMs = 4000;

        // ── Próximo y por terminar (§5.4): un solo reloj de 600 + 600 (ease.pulse), como el AnimationController(600)
        // ..repeat(reverse: true) del legacy (immersive_workout_screen.dart:319-322). Próximo entra con ancho 0 → N (ω 12) y
        // opacidad 0 → 1 en 200 por el latido 0,6 + 0,4·p; por terminar, escala 1 → 1,1 con ω 22. ──
        public const double StepPulseHalfMs = 600;
        public const double UpNextMinOpacity = 0.6;
        public const double UpNextFadeInMs = 200;
        public const double UpNextWidthOmega = CriticalSpring.SmoothOmega;
        public const double EndingSoonScale = 1.1;
        public const double EndingSoonOmega = CriticalSpring.SnappyOmega;

        // ── Cambio de paso (§5.5; VID-01): tipo y pastilla viejos 0-120 (ease.exit); nuevos 60-260 (ease.enter) subiendo
        // shift.s; borde, halo, hueco de la flecha y alto de obj. 0-240 (ease.move); el texto de obj. 60-240. ──
        public const double StepSwapOutMs = 120;
        public const double StepSwapInStartMs = 60;
        public const double StepSwapInEndMs = 260;
        public const double TargetDecorMs = 240;
        public const double TargetTextInStartMs = 60;
        public const double TargetTextInEndMs = 240;

        // ── Tarjeta BPM y kcal (§5.6): BPM aparece: kcal a su lugar en 0-240 (FLIP) y BPM funde en 120-360; se va: BPM funde
        // en 0-120 y kcal se mueve en 120-360. ──
        public const double BpmMoveMs = 240;
        public const double BpmFadeInStartMs = 120;
        public const double BpmFadeInEndMs = 360;
        public const double BpmFadeOutMs = 120;
        public const double BpmMoveOutStartMs = 120;
        public const double BpmMoveOutEndMs = 360;

        // ── El inmersivo (§5.1, DN4a-05): entra en 240 (ease.enter) y sale en 200 (ease.exit), por fundido; con reducir
        // movimiento, 120 lineal. La entrada "sumergirse" es de la 4b. ──
        public const double ImmersiveInMs = 240;
        public const double ImmersiveOutMs = 200;
        public const double ReducedImmersiveFadeMs = 120;

        // P2 (DN4a-20): Terminar muestra su spinner de 16 si el cierre tarda más de 300 ms (= SaveSpinnerDelayMs).
        public const double FinishSpinnerDelayMs = SaveSpinnerDelayMs;

        // ════ It. 4b — HUD EXR (04b-hud-exr/02-tech-design §3.1; 04-design-notes §4.15; DN4b-01 a -03 confirman los números de
        // la spec, que son estos; las alternativas de 04b/00-deuda-diseno quedan retiradas, §8.2). Las líneas de tiempo que los
        // usan son puras (HudTimelines, WP-A); los IMotion de WP-D1 solo las evalúan. Se reusan sin cambios DigitRoll.DurationMs
        // (180), ReducedFadeMs (80), HudColorMs, StepPulseHalfMs, UpNextMinOpacity, los de BPM y kcal, ToastTextOutMs /
        // ToastTextInEndMs (la etiqueta del chip), FabInScaleFrom (la llave) y los del diálogo de la It. 3. ════

        // ── Entrada "sumergirse" (§3.2; AC-WKT-13-06): Workout funde a negro en 220 (ease.enter); el fondo aparece en 220-400;
        // la cascada (barra 400, SPM 520, Split y Watts 600, banda 680, kcal y BPM 760, controles 840), cada pieza en 280
        // (ease.enter) de opacidad 0 a 1 y de +8 (shift.s; la spec pide ≤ 12) a 0; todo quieto a los 1120, cuando entra la llave
        // (160). Con reducir movimiento, fundido lineal de 120 (ReducedImmersiveFadeMs) sin negro ni cascada. ──
        public const double ImmersiveBlackInMs = 220;
        public const double ImmersiveBackdropInStartMs = 220;
        public const double ImmersiveBackdropInEndMs = 400;
        public const double ImmersiveCascadeTopBarMs = 400;
        public const double ImmersiveCascadeSpmMs = 520;
        public const double ImmersiveCascadeSplitWattsMs = 600;
        public const double ImmersiveCascadeBandMs = 680;
        public const double ImmersiveCascadeVitalsMs = 760;
        public const double ImmersiveCascadeControlsMs = 840;
        public const double ImmersivePieceInMs = 280;
        public const double ImmersivePieceShiftDp = 8;
        public const double ImmersiveEntryEndMs = 1120;
        public const double ImmersiveWrenchInMs = 160;

        // ── Salida con la última foto (§3.3; AC-WKT-13-08; O-01): el HUD se va junto en 200 (ease.exit), el fondo funde a negro
        // en 200-400 (ease.exit) y vuelve Workout en 400-640 (ease.enter); los avisos del final a los 400 (DN4b-24). ──
        public const double ImmersiveHudOutMs = 200;
        public const double ImmersiveBlackOutStartMs = 200;
        public const double ImmersiveBlackOutEndMs = 400;
        public const double ImmersiveRevealStartMs = 400;
        public const double ImmersiveRevealMs = 240;
        public const double EndToastStartMs = ImmersiveRevealStartMs;

        // ── Las píldoras (§3.4; 04-design-notes §4.5): la que termina completa su relleno en 0-120 (ease.move); cumplida o sin
        // objetivo se solidifica en 120-340 (1 → 0,85) con el destello 0,40·(1 − ease.enter) y el brillo que se va; no cumplida
        // baja su interior y dibuja su borde en 120-440; la nueva sube su pista en 0-150. Con reducir movimiento, SIN destello
        // ni trazo: el borde de la hueca funde en 160 (QA-G4bT-03). ──
        public const double PillFillCompleteMs = 120;
        public const double PillSolidifyStartMs = 120;
        public const double PillSolidifyEndMs = 340;
        public const double PillFlashPeak = 0.40;
        public const double PillMissedStartMs = 120;
        public const double PillMissedEndMs = 440;
        public const double PillTrackInMs = 150;
        public const double ReducedPillBorderFadeMs = 160;

        // El borde del relleno se repinta cuando avanzó un cuarto de píxel (§3.4).
        public const double PillRepaintPx = 0.25;

        // ── El halo de los últimos 5 s (§3.4; 04-design-notes §3.2, §4.4; DN4b-17): α = a·(0,30 + 0,40·p), con la rampa a de 150
        // (ease.move); en el borde de la píldora se ve 0,65·α (lo del BoxShadow del legacy) y se apaga como (1 − d/E)² hasta
        // 10·s a los costados, 8·s arriba y 4·s abajo. Con reducir movimiento, fijo en 0,70 con la rampa. ──
        public const double PillGlowMin = 0.30;
        public const double PillGlowMax = 0.70;
        public const double PillGlowRampMs = 150;
        public const double PillGlowEdgeFactor = 0.65;
        public const double PillGlowExtentSideDp = 10;
        public const double PillGlowExtentTopDp = 8;
        public const double PillGlowExtentBottomDp = 4;

        // Referencia del legacy (step_pill.dart:88-94), para la prueba del 0,65.
        public const double PillGlowBlurDp = 8;
        public const double PillGlowSpreadDp = 2;
        public const double PillGlowRadiusDp = 5;

        // El brillo del relleno (DN4b-19): sí, ya no es opcional; se va en 120-340 con la sólida.
        public const double PillHighlightAlpha = 0.25;
        public const double PillHighlightDp = 1;

        // El pulgar que pasa (DN4b-18): el que se va baja a la franja y se apaga mientras el nuevo sube, en 150 (ease.move).
        public const double PillThumbSwapMs = 150;

        // ── La fila de información (§2.15; DN4b-16): el cambio de modo en 0-240 (ease.move) con el cambio de paso; la pastilla
        // que no entra con Próximo funde en 120 (ease.exit). ──
        public const double InfoRowModeChangeMs = 240;
        public const double TargetPillOutMs = 120;

        // ── La cuenta grande y PAUSA, un disco por vez (§3.5; 04-design-notes §4.6, §4.8; DN4b-14): la cuenta entra en 200
        // (0,85 → 1, ease.enter) y sale en 200 (opacidad ease.exit, escala 1 → 1,10 ease.enter); con reducir movimiento, 160 y
        // 120 solo opacidad. PAUSA entra y sale en 240 (0,96 ↔ 1). En el cambio de disco el que sale funde en 0-120 (ease.exit,
        // sin escala) y el que entra empieza a los 120. El aro avanza en el sentido del reloj (DN4b-20). ──
        public const double BigCountInMs = 200;
        public const double BigCountInScaleFrom = 0.85;
        public const double BigCountOutMs = 200;
        public const double BigCountOutScaleTo = 1.10;
        public const double ReducedBigCountInMs = 160;
        public const double ReducedBigCountOutMs = 120;
        public const double CalloutSwapOutMs = 120;
        public const double CalloutSwapInStartMs = 120;
        public const bool BigCountRingClockwise = true;

        // ── El sello (§3.6; 04-design-notes §4.7): todo dentro de los 2 s. La barra crece de 52 a 74 en 0-240 (ease.move); el
        // contenido funde en 40-240 (ease.enter); el ícono se estampa de 0,6 a 1 en 60-300 (ease.emphasis); el contenido se va en
        // 1800-1920 (ease.exit) y la barra vuelve en 1800-2000 (ease.move). Un sello nuevo: el viejo funde en 0-80 y el nuevo
        // entra en 80-200 (el ícono otra vez en 80-320). Con reducir movimiento, el alto en el acto y el contenido en 160 / 120. ──
        public const double SealDurationMs = 2000;
        public const double SealGrowMs = 240;
        public const double SealContentInStartMs = 40;
        public const double SealContentInEndMs = 240;
        public const double SealIconScaleFrom = 0.6;
        public const double SealIconStartMs = 60;
        public const double SealIconEndMs = 300;
        public const double SealContentOutStartMs = 1800;
        public const double SealContentOutEndMs = 1920;
        public const double SealCollapseStartMs = 1800;
        public const double SealCollapseEndMs = 2000;
        public const double SealSwapOutMs = 80;
        public const double SealSwapInEndMs = 200;
        public const double SealSwapIconEndMs = 320;
        public const double ReducedSealContentInMs = 160;
        public const double ReducedSealContentOutMs = 120;

        // ── El diálogo de completado (§3.9; 04-design-notes §4.12; DN4b-21): entra a los 300 del fin; el disco del check de 0,6
        // a 1 en 60-300 (ease.emphasis) y su trazo en 180-500 (ease.move), desde que entra el diálogo; los totales cuentan en 700
        // (ease.enter) desde los 220, escalonados 120: el conteo termina a los 1160 del diálogo (1460 del fin). ──
        public const double CompletedDialogDelayMs = 300;
        public const double CompletedCheckScaleFrom = 0.6;
        public const double CompletedCheckStartMs = 60;
        public const double CompletedCheckEndMs = 300;
        public const double CompletedCheckStrokeStartMs = 180;
        public const double CompletedCheckStrokeEndMs = 500;
        public const double TotalsCountMs = 700;
        public const double TotalsStaggerMs = 120;
        public const double TotalsStartMs = 220;

        // ── Pausa (§3.7; 04-design-notes §4.8): el fondo se oscurece un 15 % en 300 (ease.move) y vuelve en 240; PAUSA entra en
        // 240 (ease.enter, 0,96 → 1) y sale en 240 (ease.exit, 1 → 0,96). ──
        public const double PauseVeilAlpha = 0.15;
        public const double PauseVeilInMs = 300;
        public const double PauseVeilOutMs = 240;
        public const double PausedLabelInMs = 240;
        public const double PausedLabelScaleFrom = 0.96;
        public const double PausedLabelOutMs = 240;
        public const double PausedLabelOutScaleTo = 0.96;

        // ── El chip de fase y el punto en vivo (§3.7; 04-design-notes §4.9; DN4b-26): los colores cruzan en 150 (ease.move) y la
        // etiqueta se reemplaza (0-80 y 80-200); el punto late entre 0,35 y 1 (800 + 800, ease.pulse) y empieza en 1, bajando,
        // cada vez que el chip pasa a EN MARCHA. ──
        public const double PhaseChipColorMs = HudColorMs;
        public const double LiveDotHalfMs = 800;
        public const double LiveDotMinOpacity = 0.35;
    }
}
