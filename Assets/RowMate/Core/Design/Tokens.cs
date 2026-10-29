// FUENTE ÚNICA de tokens de diseño (ADR 016, QA-P-05). La mantiene el Designer.
// Después de editar: correr tools/tokens para regenerar Presentation/Styles/Tokens.uss.
// Reglas: un campo por token; nombre del campo = TokenCatalog.FieldNameFor(nombre); nombres de ART_BIBLE §2 / ANIMATION_BIBLE §1.
//
// Notas del Designer (detalle y contraste verificado en docs/migration/iterations/01-fundaciones/04-design-notes.md):
// - [CONTRATO] = hex del legacy (rowmate-legacy, solo lectura). Son los 21 de 02-tech-design §4.4; no se cambian nunca.
// - Los demás colores pueden ajustarse solo con el mismo matiz y sin bajar su rol de contraste (R-10, D-ART-01).
// - Hex opacos "#RRGGBB"; transparencia en porcentaje entero (HexAlpha, "@NN %").
// - Longitudes en dp (1 px de USS = 1 dp, ART_BIBLE §2.4). Duraciones en ms. Tipografía: tamaño / alto de línea en dp y peso.
namespace RowMate.Core.Design
{
    public static class Tokens
    {
        // ── 2.1 Fondos, superficies, texto y bordes ───────────────────────────────
        public static readonly ColorToken BgApp = ColorToken.Contract("bg.app", "#0D1B2A", ColorRole.Surface);                 // legacy theme.dart:13 (y AppBar :20)
        public static readonly ColorToken BgSunken = ColorToken.Hex("bg.sunken", "#08131F", ColorRole.Surface);                // campos, pistas de sliders, fondo de gráficos
        public static readonly ColorToken SurfaceCard = ColorToken.Contract("surface.card", "#1A2E45", ColorRole.Surface);     // theme.dart:15
        public static readonly ColorToken SurfaceRaised = ColorToken.Hex("surface.raised", "#23364C", ColorRole.Surface);      // filas internas, tarjeta presionada, seleccionado
        public static readonly ColorToken SurfaceOverlay = ColorToken.Hex("surface.overlay", "#1E3550", ColorRole.Surface);    // diálogos, menús, hojas inferiores
        public static readonly ColorToken SurfaceNav = ColorToken.HexAlpha("surface.nav", "#0A1622", 92, ColorRole.Surface);   // barra de pestañas translúcida, sin desenfoque (QA-P-22)
        public static readonly ColorToken NavIndicator = ColorToken.HexAlpha("nav.indicator", "#00B4D8", 18);                  // pastilla del destino seleccionado (decorativa)
        public static readonly ColorToken BorderSubtle = ColorToken.HexAlpha("border.subtle", "#FFFFFF", 8);                   // borde de vidrio, separadores de barra
        public static readonly ColorToken BorderDefault = ColorToken.HexAlpha("border.default", "#FFFFFF", 12);                // separadores, miniatura no elegida
        public static readonly ColorToken BorderStrong = ColorToken.HexAlpha("border.strong", "#FFFFFF", 24);                  // botón contorno neutro, foco inactivo
        public static readonly ColorToken BorderInput = ColorToken.HexAlpha("border.input", "#FFFFFF", 40, ColorRole.Graphic);  // borde de la caja del campo en reposo (It. 3, DN3-01: 3,807 contra bg.sunken)
        public static readonly ColorToken FocusRing = ColorToken.Hex("focus.ring", "#00B4D8", ColorRole.Graphic);              // anillo de foco 2 dp (desktop)
        public static readonly ColorToken FocusRingHalo = ColorToken.HexAlpha("focus.ringHalo", "#00B4D8", 30);                // halo del foco 4 dp
        public static readonly ColorToken TextPrimary = ColorToken.Contract("text.primary", "#FFFFFF", ColorRole.UiTextPrimary); // texto de la barra superior, theme.dart:20-21
        public static readonly ColorToken TextSecondary = ColorToken.HexAlpha("text.secondary", "#FFFFFF", 70, ColorRole.UiText); // subtítulos, etiquetas (≈ #BAC0C7 sobre card)
        public static readonly ColorToken TextTertiary = ColorToken.HexAlpha("text.tertiary", "#FFFFFF", 54, ColorRole.UiText);   // fechas, ejes, ayudas (D-ART-03; ≈ #969FA9 sobre card)
        public static readonly ColorToken TextDisabled = ColorToken.HexAlpha("text.disabled", "#FFFFFF", 38);                  // solo deshabilitado o decorativo (exento de AA)
        public static readonly ColorToken TextOnAccent = ColorToken.Hex("text.onAccent", "#04131C");                           // texto sobre cian, menta y amarillo (par verificado en 04-design-notes)
        public static readonly ColorToken BrandSeed = ColorToken.Contract("brand.seed", "#0077B6", ColorRole.Graphic);         // theme.dart:5 (_seed)
        public static readonly ColorToken AccentPrimary = ColorToken.Hex("accent.primary", "#00B4D8", ColorRole.UiText);       // acento general; también texto (botones de texto, "Serie")
        public static readonly ColorToken AccentPrimaryPressed = ColorToken.Hex("accent.primaryPressed", "#0096B4", ColorRole.Graphic); // botón primario presionado
        public static readonly ColorToken AccentTonalBg = ColorToken.HexAlpha("accent.tonalBg", "#00B4D8", 12);                // fondo del botón tonal (decorativo; It. 2, DN-01: con @16 % el acento no llegaba a AA)
        public static readonly ColorToken AccentOnTonal = ColorToken.Hex("accent.onTonal", "#38CBEF", ColorRole.UiText);       // texto e ícono del botón tonal (5,88 sobre el tonal en la tarjeta; 5,24 con hover)

        // ── 2.2 Semántica: métricas [CONTRATO] ─────────────────────────────────────
        public static readonly ColorToken MetricSplit = ColorToken.Contract("metric.split", "#FF9F1C");                        // theme.dart:33 (naranja ámbar)
        public static readonly ColorToken MetricSpm = ColorToken.Contract("metric.spm", "#00B4D8");                            // theme.dart:34 (cian)
        public static readonly ColorToken MetricWatts = ColorToken.Contract("metric.watts", "#FFD60A");                        // theme.dart:35 (amarillo eléctrico)
        public static readonly ColorToken MetricDistance = ColorToken.Contract("metric.distance", "#4CC9F0");                  // theme.dart:36 (celeste)
        public static readonly ColorToken MetricHr = ColorToken.Contract("metric.hr", "#EF476F");                              // theme.dart:37 (rosa/rojo)
        public static readonly ColorToken MetricTime = ColorToken.Contract("metric.time", "#E0E0E0");                          // theme.dart:38 (gris claro)
        public static readonly ColorToken MetricKcal = ColorToken.Contract("metric.kcal", "#06D6A0");                          // theme.dart:39 (verde menta)

        // ── 2.2 Semántica: tipos de paso [CONTRATO] ────────────────────────────────
        public static readonly ColorToken StepWork = ColorToken.Contract("step.work", "#EF476F");                              // theme.dart:59
        public static readonly ColorToken StepRest = ColorToken.Contract("step.rest", "#06D6A0");                              // theme.dart:61
        public static readonly ColorToken StepWarmup = ColorToken.Contract("step.warmup", "#FFD166");                          // theme.dart:63
        public static readonly ColorToken StepCooldown = ColorToken.Contract("step.cooldown", "#118AB2");                      // theme.dart:65
        public static readonly ColorToken StepUnknown = ColorToken.Contract("step.unknown", "#607D8B");                        // theme.dart:67 (default: vacío, desconocido, sin tipo)

        // ── 2.2 Semántica: fases del entrenamiento [CONTRATO] ──────────────────────
        public static readonly ColorToken PhaseRunning = ColorToken.Contract("phase.running", "#06D6A0");                      // immersive_workout_screen.dart:531 (EN MARCHA)
        public static readonly ColorToken PhasePaused = ColorToken.Contract("phase.paused", "#FFD166");                        // :532 (PAUSADO)
        public static readonly ColorToken PhaseFinished = ColorToken.Contract("phase.finished", "#EF476F");                    // :533 (FINALIZADO)

        // ── 2.2 Semántica: estados, marca y desarrollo ─────────────────────────────
        public static readonly ColorToken StateOffTarget = ColorToken.Contract("state.offTarget", "#FF4444");                  // immersive_workout_screen.dart:581-582
        public static readonly ColorToken StateSuccess = ColorToken.Hex("state.success", "#06D6A0", ColorRole.Graphic);        // completado, reanudar (texto encima: text.onAccent)
        public static readonly ColorToken StateConnected = ColorToken.Hex("state.connected", "#69F0AE", ColorRole.UiText);     // texto del chip "Conectado", RECEIVING
        public static readonly ColorToken StateConnectedBg = ColorToken.HexAlpha("state.connectedBg", "#4CAF50", 20);          // fondo del chip "Conectado"
        public static readonly ColorToken StateWarning = ColorToken.Hex("state.warning", "#FF9800", ColorRole.UiText);         // texto del aviso "remo no conectado"
        public static readonly ColorToken StateWarningBg = ColorToken.HexAlpha("state.warningBg", "#FF9800", 15);              // fondo del aviso
        public static readonly ColorToken StateDanger = ColorToken.Hex("state.danger", "#FF5252", ColorRole.Graphic);          // contorno de terminar y desconectar (no es texto)
        public static readonly ColorToken StateDangerText = ColorToken.Hex("state.dangerText", "#FF7272", ColorRole.UiText);   // texto de error y de Terminar (antes #FF6B6B: 4,44:1 sobre surface.raised)
        public static readonly ColorToken StateDangerFill = ColorToken.Hex("state.dangerFill", "#D32F2F", ColorRole.Graphic);  // relleno de "borrar rutina" (texto blanco)
        public static readonly ColorToken BrandStrava = ColorToken.Contract("brand.strava", "#FC4C02");                        // history_screen.dart:99, profile_screen.dart:99
        public static readonly ColorToken DevSimulator = ColorToken.Hex("dev.simulator", "#FF5722", ColorRole.Graphic);        // cinta SIMULADOR
        public static readonly ColorToken DevSimulatorText = ColorToken.Hex("dev.simulatorText", "#1A0A00");                   // texto de la cinta SIMULADOR (par verificado)
        public static readonly ColorToken DevBleDebug = ColorToken.Hex("dev.bleDebug", "#18FFFF");                             // texto mono del diagnóstico BLE (par verificado)
        public static readonly ColorToken DevBleDebugBg = ColorToken.HexAlpha("dev.bleDebugBg", "#000000", 38);                // fondo del diagnóstico BLE
        public static readonly ColorToken OverlayScrim = ColorToken.Hex("overlay.scrim", "#000000", ColorRole.Graphic);        // velo de diálogos (0,6), oscurecido de la pila (0,08), capa del ítem de peligro (It. 3)
        public static readonly ColorToken TextSelection = ColorToken.HexAlpha("text.selection", "#00B4D8", 35, ColorRole.Surface); // selección de los campos de texto (It. 3, DN3-01: blanco encima 9,586)

        // ── 2.3 Variantes del HUD (mismo matiz OKLCH que la base) ──────────────────
        // Umbral contra el peor vidrio RENDERIZADO + 1 por canal (#2B3038 = #2A2F37 + 1 LSB), no contra el cálculo sin
        // redondear: con margen de 1 LSB, el píxel real también llega (It. 3, QA-V2-12; docs/migration/iterations/03-rutinas/
        // 00-deuda-diseno.md §2). Los 8 que cambiaron daban 4,497-4,515 / 6,987-7,023 en el píxel de 8 bits.
        public static readonly ColorToken MetricSplitHud = ColorToken.Hex("metric.split.hud", "#FFAA47", ColorRole.HudValue);       // era #FEA845
        public static readonly ColorToken MetricSpmHud = ColorToken.Hex("metric.spm.hud", "#3ACCF1", ColorRole.HudValue);           // era #38CBEF
        public static readonly ColorToken MetricWattsHud = ColorToken.Hex("metric.watts.hud", "#FFD60A", ColorRole.HudValue);       // = base
        public static readonly ColorToken MetricDistanceHud = ColorToken.Hex("metric.distance.hud", "#4ECBF2", ColorRole.HudValue); // era = base #4CC9F0
        public static readonly ColorToken MetricTimeHud = ColorToken.Hex("metric.time.hud", "#E0E0E0", ColorRole.HudValue);         // = base
        public static readonly ColorToken MetricKcalHud = ColorToken.Hex("metric.kcal.hud", "#06D6A0", ColorRole.HudValue);         // = base
        public static readonly ColorToken MetricHrHud = ColorToken.Hex("metric.hr.hud", "#FF5D7F", ColorRole.HudValueRed);          // D-ART-01 (familia roja, ≥ 4,5:1); era #FF5A7E
        public static readonly ColorToken StateOffTargetHud = ColorToken.Hex("state.offTarget.hud", "#FF625B", ColorRole.HudValueRed); // D-ART-01 + flecha ▲/▼ + borde; era #FF5F59
        public static readonly ColorToken StepWorkHud = ColorToken.Hex("step.work.hud", "#FF5D7F", ColorRole.HudText);              // = metric.hr.hud (misma base); era #FF5A7E
        public static readonly ColorToken StepRestHud = ColorToken.Hex("step.rest.hud", "#06D6A0", ColorRole.HudText);              // = base
        public static readonly ColorToken StepWarmupHud = ColorToken.Hex("step.warmup.hud", "#FFD166", ColorRole.HudText);          // = base
        public static readonly ColorToken StepCooldownHud = ColorToken.Hex("step.cooldown.hud", "#37A2CB", ColorRole.HudText);      // era #35A0C9
        public static readonly ColorToken StepUnknownHud = ColorToken.Hex("step.unknown.hud", "#7D9BA9", ColorRole.HudText);        // era #7C9AA8
        public static readonly ColorToken TextHudPrimary = ColorToken.Hex("text.hudPrimary", "#FFFFFF", ColorRole.HudValue);        // valor con objetivo cumplido (Q-60)

        // ── 3.2 Vidrio del HUD: respaldo sin desenfoque (QA-P-17, P-08) ───────────
        public static readonly ColorToken GlassFallback = ColorToken.HexAlpha("glass.fallback", "#070D16", 86, ColorRole.Surface);

        // ── Fondo provisorio del inmersivo (It. 4a; DN4a-03, DN4a-18; 04-design-notes §2.1): hasta la escena de la 6a ──
        public static readonly ColorToken BackdropSky = ColorToken.Hex("backdrop.sky", "#0B4FA8", ColorRole.Surface);       // skyTop de las 12 h del legacy (LEGACY_VISUAL_AUDIT §7.3), hasta el horizonte
        public static readonly ColorToken BackdropWater = ColorToken.Hex("backdrop.water", "#0E4F8F", ColorRole.Surface);   // waterNear de las 12 h del legacy, del horizonte abajo

        // ── 2.4 Espaciado (escala única, en dp) ────────────────────────────────────
        public static readonly LengthToken SpaceDp2 = LengthToken.Dp("space.dp2", 2);
        public static readonly LengthToken SpaceDp4 = LengthToken.Dp("space.dp4", 4);
        public static readonly LengthToken SpaceDp6 = LengthToken.Dp("space.dp6", 6);
        public static readonly LengthToken SpaceDp8 = LengthToken.Dp("space.dp8", 8);
        public static readonly LengthToken SpaceDp12 = LengthToken.Dp("space.dp12", 12);
        public static readonly LengthToken SpaceDp16 = LengthToken.Dp("space.dp16", 16);
        public static readonly LengthToken SpaceDp20 = LengthToken.Dp("space.dp20", 20);
        public static readonly LengthToken SpaceDp24 = LengthToken.Dp("space.dp24", 24);
        public static readonly LengthToken SpaceDp32 = LengthToken.Dp("space.dp32", 32);
        public static readonly LengthToken SpaceDp40 = LengthToken.Dp("space.dp40", 40);
        public static readonly LengthToken SpaceDp48 = LengthToken.Dp("space.dp48", 48);

        // ── 2.4 Radios ─────────────────────────────────────────────────────────────
        public static readonly LengthToken RXs = LengthToken.Dp("r.xs", 4);           // píldora de paso
        public static readonly LengthToken RS = LengthToken.Dp("r.s", 6);             // chips chicos
        public static readonly LengthToken RM = LengthToken.Dp("r.m", 8);             // chips, cajas internas
        public static readonly LengthToken RL = LengthToken.Dp("r.l", 12);            // botones, avisos, miniaturas
        public static readonly LengthToken RGlass = LengthToken.Dp("r.glass", 14);    // vidrio del HUD [CONTRATO]
        public static readonly LengthToken RCard = LengthToken.Dp("r.card", 16);      // tarjetas (theme.dart:16)
        public static readonly LengthToken RFull = LengthToken.Dp("r.full", 999);     // cápsulas: banda tiempo/distancia, píldoras de navegación

        // ── 2.4 Grosores de borde ──────────────────────────────────────────────────
        public static readonly LengthToken StrokeHairline = LengthToken.Dp("stroke.hairline", 1);     // borde de vidrio, separadores
        public static readonly LengthToken StrokeTarget = LengthToken.Dp("stroke.target", 1.5);       // tarjeta con objetivo activo, contornos de botón
        public static readonly LengthToken StrokeFocus = LengthToken.Dp("stroke.focus", 2);           // anillo de foco, campo con foco
        public static readonly LengthToken StrokeSelected = LengthToken.Dp("stroke.selected", 2.5);   // miniatura de escenario elegida [CONTRATO]
        public static readonly LengthToken StrokeAccent = LengthToken.Dp("stroke.accent", 3);         // acento superior del tablero y borde izquierdo de paso [CONTRATO]
        public static readonly LengthToken StrokeFocusHalo = LengthToken.Dp("stroke.focusHalo", 4);   // halo del foco

        // ── ANIMATION_BIBLE §1.3 Distancias de desplazamiento ─────────────────────
        public static readonly LengthToken ShiftS = LengthToken.Dp("shift.s", 8);
        public static readonly LengthToken ShiftM = LengthToken.Dp("shift.m", 12);
        public static readonly LengthToken ShiftL = LengthToken.Dp("shift.l", 16);
        public static readonly LengthToken ShiftXl = LengthToken.Dp("shift.xl", 24);  // tope para cualquier elemento del HUD

        // ── ANIMATION_BIBLE §1.1 Duraciones ────────────────────────────────────────
        public static readonly DurationToken DurMicro = DurationToken.Ms("dur.micro", 90);            // presionar, íconos chicos
        public static readonly DurationToken DurFast = DurationToken.Ms("dur.fast", 160);             // color, fundidos de texto, rodillo del reloj
        public static readonly DurationToken DurBase = DurationToken.Ms("dur.base", 240);             // entradas y salidas, indicadores
        public static readonly DurationToken DurSlow = DurationToken.Ms("dur.slow", 360);             // paneles que se expanden, reacomodos
        public static readonly DurationToken DurPage = DurationToken.Ms("dur.page", 420);             // transiciones entre pantallas
        public static readonly DurationToken DurScene = DurationToken.Ms("dur.scene", 900);           // cambios de escena, fundidos de luz
        public static readonly DurationToken DurCinematic = DurationToken.Ms("dur.cinematic", 1600);  // vuelos y transiciones de cámara

        // ── ANIMATION_BIBLE §1.2 Curvas (USS manda; el cubic-bezier es la aproximación para C#) ──
        public static readonly EasingToken EaseEnter = EasingToken.CubicBezier("ease.enter", 0.33, 1.0, 0.68, 1.0, ussKeyword: "ease-out-cubic");        // todo lo que entra
        public static readonly EasingToken EaseExit = EasingToken.CubicBezier("ease.exit", 0.32, 0.0, 0.67, 0.0, ussKeyword: "ease-in-cubic");           // todo lo que sale
        public static readonly EasingToken EaseMove = EasingToken.CubicBezier("ease.move", 0.65, 0.0, 0.35, 1.0, ussKeyword: "ease-in-out-cubic");       // desplazamientos y tamaños
        public static readonly EasingToken EasePulse = EasingToken.CubicBezier("ease.pulse", 0.37, 0.0, 0.63, 1.0, ussKeyword: "ease-in-out-sine");      // pulsos y respiraciones
        public static readonly EasingToken EaseEmphasis = EasingToken.CubicBezier("ease.emphasis", 0.34, 1.56, 0.64, 1.0, ussKeyword: "ease-out-back"); // solo sellos de éxito y "solidificar" la píldora (y1 > 1: rebote)
        public static readonly EasingToken EaseLinear = EasingToken.CubicBezier("ease.linear", 0.0, 0.0, 1.0, 1.0, ussKeyword: "linear");                // progreso, brillos en barrido

        // ── 4.3 Tipografía: interfaz (Inter) ───────────────────────────────────────
        public static readonly TypeScaleToken Display = TypeScaleToken.Of("display", FontFamilyRole.Ui, 32, 40, 700);    // título del resumen final
        public static readonly TypeScaleToken Headline = TypeScaleToken.Of("headline", FontFamilyRole.Ui, 24, 32, 700);  // títulos de vistas grandes
        public static readonly TypeScaleToken Title = TypeScaleToken.Of("title", FontFamilyRole.Ui, 20, 28, 600);        // AppBar, diálogos
        public static readonly TypeScaleToken TitleS = TypeScaleToken.Of("titleS", FontFamilyRole.Ui, 16, 22, 600);      // secciones, nombres en tarjetas
        public static readonly TypeScaleToken Body = TypeScaleToken.Of("body", FontFamilyRole.Ui, 15, 22, 400);          // texto general
        public static readonly TypeScaleToken BodyM = TypeScaleToken.Of("bodyM", FontFamilyRole.Ui, 14, 20, 400);        // nombres y estado en tarjetas, rótulos del panel (It. 2, DN-01; legacy 14)
        public static readonly TypeScaleToken BodyS = TypeScaleToken.Of("bodyS", FontFamilyRole.Ui, 13, 18, 400);        // subtítulos, resúmenes
        public static readonly TypeScaleToken BodyXs = TypeScaleToken.Of("bodyXs", FontFamilyRole.Ui, 12, 16, 400);      // ayudas, errores, insignia (It. 2, DN-01; spec 12 dp)
        public static readonly TypeScaleToken TitleXs = TypeScaleToken.Of("titleXs", FontFamilyRole.Ui, 14, 20, 600);    // título de la fila de paso, Pasos (N) (It. 3, DN3-01)
        public static readonly TypeScaleToken HeaderS = TypeScaleToken.Of("headerS", FontFamilyRole.Ui, 13, 18, 700);    // "Serie" del encabezado (It. 3, DN3-01; legacy 13/700)
        public static readonly TypeScaleToken ButtonS = TypeScaleToken.Of("buttonS", FontFamilyRole.Ui, 12, 16, 600);    // Agregar paso a serie, nombres de los mosaicos (It. 3, DN3-01)
        public static readonly TypeScaleToken Label = TypeScaleToken.Of("label", FontFamilyRole.Ui, 12, 16, 600);        // etiquetas de métrica (mayúsculas, tracking 0,08 em en USS)
        public static readonly TypeScaleToken Caption = TypeScaleToken.Of("caption", FontFamilyRole.Ui, 11, 14, 500);    // fechas, ayudas, ejes (mínimo legible)
        public static readonly TypeScaleToken Button = TypeScaleToken.Of("button", FontFamilyRole.Ui, 14, 20, 600);      // etiqueta de botones, chips y acciones de texto (It. 2, DN-01; M3 labelLarge 14)

        // ── 4.3 Tipografía: números (Barlow Semi Condensed, siempre tabulares) ────
        public static readonly TypeScaleToken NumHero = TypeScaleToken.Of("num.hero", FontFamilyRole.Numeric, 64, 64, 800);  // SPM del HUD (D-ART-02)
        public static readonly TypeScaleToken NumXl = TypeScaleToken.Of("num.xl", FontFamilyRole.Numeric, 52, 52, 700);      // tablero de Dispositivo, pulso
        public static readonly TypeScaleToken NumL = TypeScaleToken.Of("num.l", FontFamilyRole.Numeric, 44, 44, 700);        // Split y Watts del HUD
        public static readonly TypeScaleToken NumM = TypeScaleToken.Of("num.m", FontFamilyRole.Numeric, 32, 34, 700);        // kcal y BPM del HUD, resumen final
        public static readonly TypeScaleToken NumBand = TypeScaleToken.Of("num.band", FontFamilyRole.Numeric, 34, 34, 700);  // banda tiempo │ distancia (antes "num.m 34")
        public static readonly TypeScaleToken NumLap = TypeScaleToken.Of("num.lap", FontFamilyRole.Numeric, 34, 36, 700);    // vuelta en curso
        public static readonly TypeScaleToken NumRow = TypeScaleToken.Of("num.row", FontFamilyRole.Numeric, 24, 28, 700);    // filas de vueltas y repeticiones
        public static readonly TypeScaleToken NumS = TypeScaleToken.Of("num.s", FontFamilyRole.Numeric, 20, 22, 700);        // cuenta regresiva, reloj
        public static readonly TypeScaleToken NumStat = TypeScaleToken.Of("num.stat", FontFamilyRole.Numeric, 16, 20, 700);  // stats de historial, chips numéricos
    }
}
