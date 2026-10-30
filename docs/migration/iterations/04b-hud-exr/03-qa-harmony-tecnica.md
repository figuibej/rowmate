# It. 4b — HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema: gate de armonía de QA (lente técnica)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-29 |
| Iteración | 4b · `04b-hud-exr` ([ROADMAP §It. 4b](../../ROADMAP.md)) |
| Lente | **Técnica**: cobertura AC → componente → test o captura; motion que nunca toca el dato ni el reloj; nada animando al salir (ROB-02); frame pacing; reducir movimiento; transiciones en *batchmode* y estados intermedios de las tomas; el motor que espera al diálogo de completado; P-17 de la 4a y el guardado incremental; barras del sistema por plataforma; ciclo de vida de UI Toolkit; paquetes disjuntos y contratos USS y de motion; contradicciones spec ↔ diseño técnico ↔ notas de diseño; regla de repo público. El aspecto visual y el texto de producto los miran las otras lentes |
| Entrada | [01-po-spec](01-po-spec.md) (100 AC = 61 + 39, LT-46, 30 CAP, 12 DEMO, 8 VID), [02-tech-design](02-tech-design.md), [04-design-notes](04-design-notes.md) (DN4b-01 a DN4b-29), [00-deuda-4a](00-deuda-4a.md), [00-deuda-diseno](00-deuda-diseno.md); [REPORT de la 4a](../04a-motor-hud-funcional/REPORT.md) §7 a §9; [lente de robustez de la 4a](../04a-motor-hud-funcional/05-qa-lens-robustez.md) (ROB-02, ROB-05); [gate técnico de la 4a](../04a-motor-hud-funcional/03-qa-harmony-tecnica.md) (lecciones) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/step_pill.dart` (entero), `test/step_pill_test.dart` (entero), `lib/features/workout/immersive_workout_screen.dart:1-600` y `:940-1019`, `workout_provider.dart` (entero), `step_target_tracker.dart` (entero), `series_panels.dart:1-115`, `workout_screen.dart:100-185`, `lib/shared/theme.dart:56-69`, `lib/l10n/app_{es,en}.arb` (claves del diálogo); lista de `test/` (solo `step_pill_test.dart` toca la 4b). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío antes y después |
| Verificación hecha | `dotnet test` de línea base (§1); *script* de cobertura en `<scratch>/it4b/gate-tecnica/coverage.py`; 24 casos del legacy recalculados (§3); lectura del código que el diseño reutiliza (`FrameScheduler`, `UnityScheduler`, `AppRoot` y `AppLifecycle`, `WorkoutEngine`, `WorkoutHudStore`, `SessionRecorder`, `MotionDriver`, `SpringMotion`, `CriticalSpring`, `StepPulse`, `StepInfoBar`, `ImmersiveWorkoutPage`, `ModalDialog`, `FocusTrap`, `HudControls`, `ScreenKeepAwake`, `FakeScheduler`, el catálogo); cuentas de coma flotante y de los resortes con Python de `uv` sin red. **Unity no se abrió** (la tarea es sin Unity) |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** **0 bloqueantes, 4 mayores, 8 menores** (§16).

- **Cobertura**: los 100 AC de la spec (61 de §4 y 39 de §5) están en la matriz de 02-tech-design §6.4, más LT-46: 101 filas, 0 faltantes y 0 sobrantes. Los 41 `UnityOnlyIds` son exactamente las filas sin **d** ni **s**, y ninguna fila queda solo con **eq** o **qa**. Las siete partes que el Architect declara sin test están justificadas (§2.2). Cada AC tiene un componente responsable en §1.1.
- **Fidelidad**: recalculé 24 casos contra el legacy (§3) y coinciden al número o al carácter. Entre ellos están LT-46, los cuatro estados de la píldora, el último paso en curso (Q-52), el "asoma" del diálogo al terminar a mano (Q-53), el chip, el reloj de pared, el resumen del diálogo, los promedios del sello con el redondeo de P-03, los números de `Corta`, `Estados`, `Prueba 4a` y `Pirámide 500` con `R-4`, y P-17 (25 puntos). Hay un matiz de paridad sin registrar: el halo del legacy también tiñe la pista de la píldora (QA-G4bT-12).
- **Lo que más pesa**:
  1. Las 04-design-notes se escribieron **después** del diseño técnico y no están integradas. Cambian reglas, geometría, constantes, el contrato USS y comprobaciones del arnés en los **primeros** paquetes (WP-A y WP-D1). Además, cuatro movimientos nuevos no tienen dueño en el contrato de motion. Es el mismo caso que QA-G4aT-01 en la 4a (QA-G4bT-01).
  2. Falta que el PO decida o precise siete puntos que cambian textos de AC y lo que esperan las tomas. El más claro: AC-WKT-16-03 pide ≥ 4,5:1 y nombra `#FF5D7F`, que da 3,942. Además, AC-BLE-16-03 cambia de números y no está entre los criterios de la 4b (QA-G4bT-02).
  3. Con reducir movimiento, la spec dice "sin destello". El diseño y las notas lo conservan (QA-G4bT-03).
  4. La regla de toma de §5.2 punto 1 ("si corre una animación finita, la toma falla") no tiene definición operativa. Tal como está, haría fallar 9 capturas y 7 tomas de la demo (QA-G4bT-07). Además, CAP-22 cae justo en el instante en que entra `Próximo` (QA-G4bT-06).
- **Las dos condiciones de la 4a** están en el código, cada una con su prueba (`HudBand.cs:77`, `Base.uss:2035`, `FocusHighlight.cs:30-32`; `BandDividerKeepsItsWidth`, las tres `FirstTabWithoutFocusShowsTheRing*`). Sigue pendiente la verificación con Unity y la regeneración única. QA la hace **antes de WP-B1** (spec §12 punto 1).

Qué hay que cerrar antes de cada paquete:

| Antes de | Issues |
|---|---|
| **WP-A** (reglas puras y dotnet) | QA-G4bT-01: DN4b-14 a -18, -22, -23 y -25 en `HudGeometry`, `PillRowGeometry`, `PillShapes`, `HudTimelines`, `MotionSpecs`, `PhaseChip`, `WrenchPlacement`, `SimulatorPanelLayout` y el *token*. QA-G4bT-02: (a), (c), (d) y (e). También QA-G4bT-03, -04 y -05, y QA-G4bT-10 (b) |
| **WP-D1** (motion) | QA-G4bT-01 (los cuatro movimientos sin dueño), -03, -07 (a) y -08 (a) |
| **WP-B0** (sonda y juntas) | QA-G4bT-10 (c): sumar a S-B4b el texto enriquecido del sello, el glifo de `PAUSA`, la textura del halo y los fragmentos con "…" |
| **WP-B1** (HUD) | QA-G4bT-01 (el resto), -02 (b), (f) y (g), -08 (b) y (c), -12 |
| **WP-B2** (diálogo, página, plataforma) | QA-G4bT-05 (Platform), -08 (d), -09 |
| **WP-B3** (arnés y builds) | QA-G4bT-06, -07 (b) a (d), -10 (a), -11 |

---

## 1. Evidencia corrida

| Qué | Comando o fuente | Resultado |
|---|---|---|
| dotnet, línea base antes de WP-A | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it4b/gate-tecnica` | `Build succeeded. 0 Warning(s) 0 Error(s)` · simulador **173/173** · EditMode **2064: 2042 pasan, 0 fallan, 22 omitidos** · exit 0 · matriz **PASA 425, FALLA 0, IGNORADO 1, PENDIENTE UNITY 109, SIN TEST 0** (sin resultados de Unity). Coincide con 00-deuda-4a §0 (2064 / 2042 / 22; 173 / 173) |
| Cobertura de la matriz | `<scratch>/it4b/gate-tecnica/coverage.py`: lee los `- **AC-…**` y las filas `| AC-… ·` de spec §4 y §5 y las filas de 02-tech-design §6.4 | spec: 100 únicos (61 + 39); matriz: 101 (100 + LT-46); faltan 0, sobran 0; `UnityOnlyIds`: 41 = las filas sin d ni s; ninguna fila solo con eq o qa. La abreviatura `ACK` (AC-WKT-02-04) no está en §6.2: es `ActiveClockTests` de la 4a (`Tests/EditMode/Workout/ActiveClockTests.cs:82`) |
| Catálogo actual | `Assets/RowMate/Localization/strings.{es,en}.json` | 202 claves por idioma. Ninguna de las 12 nuevas existe todavía. Las reusadas están con el texto del legacy (`workoutCompleted`, `workoutCompletedMessage`, `workoutFinish`, `hudFreeTitle` `Entrenamiento Libre`) |
| Condiciones de la 4a (estático) | `Presentation/Workout/HudBand.cs:68-77`, `Styles/Base.uss:2035`, `Controls/FocusHighlight.cs:30-38`; `Tests/PlayMode/HudLayoutTests.cs:243`, `HudControlsTests.cs:306`, `:340`, `RoutineEditorTests.cs:607`, `ImmersiveHudTests.cs:319` | Arreglos y pruebas presentes. La corrida de Unity queda para QA antes de WP-B1 |
| Coma flotante y resortes | Python de `uv`, sin red | `30.2 − 30.0 = 0.1999999999999993` y `3015.2 − 3015.0 = 0.1999999999998181`, menos que 200 ms. `SpringMotion` (tolerancia 0,05 en posición y velocidad, ω 12) se asienta en 0,79 s con un cambio de 5,5 dp y en 1,05 s con 100 dp. A los 0,2 s el ancho va en el 69,2 % |
| Legacy intacto | `git -C ../rowmate-legacy status --porcelain` | vacío antes y después |

---

## 2. Cobertura: AC → componente → test

### 2.1 Matriz (agrupada por ficha)

**OK** = componente y test o captura planificados y coherentes. **OK · QA-G4bT-nn** = cubierto, con una observación. **OBS QA-G4bT-nn** = lo afecta un issue mayor. **Parcial (justificado)** = una parte queda para el equipo, con su `[Ignore]` en WDC. Abreviaturas: 02-tech-design §6.2.

| Ficha | AC | Estado |
|---|---|---|
| F-WKT-15 | -01 a -05 (PIL, PRT, CAP-01, -04); -06 (CVD, CAP-01 con QA) | OK. LT-46, en dotnet y en PlayMode, con el nombre exacto (§6.3) |
| F-WKT-14 | -07, -08, -09, -11, -12, -13, -14, -17 | OK |
| | -10 (PPG, PRT, VID-04) | OK · QA-G4bT-04 (retrocede con `Instant`) |
| | -15 (SBT, CAP-19, -20) | **OBS QA-G4bT-02** (b): en TV la pastilla no se ve |
| | -16 (PRG, PRT, CAP-22) | OK · QA-G4bT-01 y -02 (c) (el pulgar de DN4b-18) |
| | -18 (BCD, HGT, CCT, CAP-02, -25, -29, VID-04, -05) | OK · QA-G4bT-07 (las fases de `BigCountMotion`) |
| | -19 (HBT, `FrameTimeProbe`) | Parcial (justificado) · QA-G4bT-11 |
| | -01 a -06 (§5) | OK; -01 · QA-G4bT-02 (g): "dos filas" contra la tercera |
| F-WKT-19 | -01, -02, -04, -05 | OK. La zona con `LocalOffsetSource` cada 0,5 s y el periódico de 250 ms dan < 1 s |
| | -03 | OK · QA-G4bT-08 (c): el periódico corre en el alcance |
| F-WKT-16 | -01, -02, -04, -05 | OK |
| | -03 (PCH, CAP-15) | **OBS QA-G4bT-02** (a): `#FF5D7F` no llega a 4,5:1 |
| | -06 | OK · QA-G4bT-02 (f) (el título que se achica, DN4b-27) |
| F-WKT-21 | -01, -03, -04, -06, -07, -08, -10, -13 | OK |
| | -02 (CDT, WDC) | Parcial (justificado): "Atrás" de Android |
| | -05 (HTL, HMT, CDT, VID-05) | OK · QA-G4bT-08 (a): `CompletionMotion` al cerrar |
| | -09 (CHT, WBU, minimizada) | Parcial (justificado): la ventana minimizada, [Device-Win] |
| | -11 (CDT, [Device-Win]) | OK · QA-G4bT-09: falta el mecanismo del primer Tab sin foco con el diálogo |
| | -12 (CDT, CAP-09 a -11) | OK · QA-G4bT-11 (d) |
| F-WKT-13 | -08, -10, -11, -14; §5: -01, -02, -03, -04, -05 | OK |
| | -06 (HTL, ITT, HMT, VID-01, -03, -08) | OK. El Designer confirmó los números de la spec (DN4b-01) |
| | -07 (ITT, WDC) | Parcial (justificado) · QA-G4bT-09 (teclado durante la cascada) |
| | -09 (ITT, [Device-Win]) | OK · QA-G4bT-08 |
| | -12 (HTL, HGT, PVT, CCT) | OK · QA-G4bT-01 (`PAUSA` en el disco) |
| | -13 (HGT, HLT, `HudOverlap`) | OK |
| F-WKT-17 | -11, -12; §5: -01 a -10 | OK |
| F-WKT-09 | -08 (SEL, SVT, CAP) | OK · QA-G4bT-07 (las fases de `SealMotion`) |
| | -09 (SEL, SVT) | OK. El redondeo nunca contradice el veredicto, porque los objetivos son enteros (§3, caso 19) |
| F-WKT-02 | -11 (SUS, WSU, WBU, WDC) | OK · QA-G4bT-05 |
| | §5: -04, -06, -09 | OK |
| | §5: -08 | Parcial (justificado): el primer cuadro en Android |
| F-APP-15 | -01 | Parcial (justificado): Android |
| | -02 | Parcial (justificado): [Device-Win]. La PlayMode solo prueba que nada toca `Screen` |
| | -03 | `[Ignore]` [Apple] (10b) |
| | -04 | OK |
| F-APP-10 | -11 (CAT) | OK. La base real es 202 |
| §5 resto | AC-WKT-01-06, -03-05, -06-12, -20-07; AC-APP-03-09 a -11; AC-APP-05-05; AC-HIS-20-04 | OK |
| | AC-APP-05-07 | OK · QA-G4bT-09 |
| | AC-APP-05-08 | **OBS QA-G4bT-03** (el destello) |
| | AC-BLE-16-01, -02 | OK · QA-G4bT-01 (R10 de DN4b-22) |
| | AC-BLE-16-03 (4a) | **No está** en la spec de la 4b, y DN4b-23 le cambia los números: QA-G4bT-02 (d) |

### 2.2 Lo que el Architect declara sin test

| Declaración | Evaluación |
|---|---|
| AC-APP-15-03, solo un `[Ignore]` NO VERIFICABLE ([Apple], 10b) | **Aceptado** (spec §0.2) |
| AC-WKT-02-11: la suspensión real la verifica el usuario en la demo (c); SUS y WSU cubren la regla en los dos casos del monótono | **Aceptado con QA-G4bT-05**. SUS y WSU no cubren la vuelta por `ResumeFromBackground`, el aviso consumido por un tirón antes de dormir ni la baja del *callback* en la recarga de dominio del Editor |
| AC-APP-15-01: la parte Android NO VERIFICABLE sin módulo ni teléfono; la regla (SBR) y los márgenes (SBU, con dobles) probados | **Aceptado** |
| AC-WKT-13-07, AC-WKT-21-02: "Atrás" de Android NO VERIFICABLE (`[Ignore]` en WDC); las partes de PlayMode probadas | **Aceptado**. En PlayMode, "Atrás" es `OnBackRequested`, el mismo camino (ITT.BackFromTheFirstFrame) |
| AC-WKT-02-08: el primer cuadro en Android NO VERIFICABLE; dotnet y PlayMode probados | **Aceptado** |
| [Device-Win] a mano por QA: AC-APP-15-02, AC-APP-05-07 / AC-WKT-21-11, AC-WKT-02-09 / -21-09, AC-WKT-13-09, AC-WKT-14-19 | **Aceptado**, con QA-G4bT-11 (definir la métrica de `FrameTimeProbe`) y QA-G4bT-09 (el mecanismo del primer Tab con el diálogo) |
| "Los 100 IDs más LT-46 tienen al menos un test automático o una entrada `[Ignore]` (100 = 100)" | **Verificado** con el *script* de §1 |

---

## 3. Fidelidad: casos del legacy recalculados

| # | Caso | Legacy | Spec y diseño | Resultado |
|---|---|---|---|---|
| 1 | LT-46: pendiente, en curso 0,5, hecha cumplida, no cumplida y hecha sin resultado en una fila de 400 | `test/step_pill_test.dart:19-53` | spec §6; 02-tech-design §6.3 | Coincide, afirmación por afirmación |
| 2 | Opacidades 0,18 / 0,30 / 0,85 | `step_pill.dart:30-32` | `StepPills` (§2.1) | Coincide |
| 3 | No cumplida: interior 0,18 y `Border` de 1,5 al 0,85 | `:49-51` | AC-WKT-15-05 | Coincide. El borde de Flutter va por dentro, como dicen las notas §3.2 |
| 4 | Hecha sin resultado = cumplida (rama `else`) | `:52-54` | AC-WKT-15-04 | Coincide |
| 5 | Relleno solo en curso y con progreso > 0, acotado | `:71-76` | `PillPaint.FillFraction` | Coincide |
| 6 | Halo: `BoxShadow` al `0,3 + 0,4·p`, desenfoque 8, expansión 2, radio 5 | `:82-101` | AC-WKT-14-11 | Coincide en los números. **La forma no**: con `BlurStyle.normal` la sombra se pinta también debajo de la píldora, y la pista al 30 % la deja ver. La en curso entera se aclara con el pulso (QA-G4bT-12) |
| 7 | Fila 10 / 8 / 10 / 4, 3 px, `Expanded`; la fila vacía desde ≈ 122 pasos en 411 | `immersive_workout_screen.dart:359-375`; `step_pill.dart:103` | AC-WKT-14-07, -16 (DV-40) | Coincide: 411 − 24 − 2 − 20 = 365; 365 / 3 + 1 = 122,7 |
| 8 | Estado por índice, resultado y pulso solo en la en curso "por terminar" | `:367-372` | `LookOf` y `HudPillRow` | Coincide |
| 9 | Progreso solo con duración > 0 (Q-51); "por terminar" con restante de 1 a 5 | `workout_provider.dart:31-41` | `Fills`, `BigCountdown` | Coincide |
| 10 | El último paso se evalúa antes de mirar si hay siguiente; el índice queda; terminada y `finish()` sin esperar | `:203-216` | AC-WKT-14-13, -21-01 (Q-52) | Coincide: el diálogo no espera al guardado |
| 11 | Terminar a mano: `finish()` pone `finished` **sin** avisar y avisa al final; `_confirmFinish` hace `await finish(); reset()` | `:252-303`; `immersive_workout_screen.dart:141-147` | AC-WKT-21-07 (Q-53), §15 de la spec (`FINALIZADO`) | Coincide: el diálogo asoma y `FINALIZADO` se ve solo si llega una lectura mientras se guarda |
| 12 | El diálogo, una vez por página (`_hasShownCompletionDialog`, cuadro siguiente) y vuelta a `false` al salir | `:29`, `:49-65` | `CompletionDialogRule` por `Session` | Coincide, con una mejora (por sesión, no por página) |
| 13 | Modal: `barrierDismissible: false` y `PopScope(canPop: false)` | `:207-212` | AC-WKT-21-02; `Dismissible = false` | Coincide |
| 14 | Resumen: reloj activo `H:MM:SS` o `MM:SS`, `{n} m`, kcal; etiquetas fijas en español; caja blanca `withAlpha(10)` (3,9 %), radio 8, relleno 12; filas a 8; ícono 20 `#00B4D8` y 12; etiqueta `white70`; valor en negrita de 16 | `:959-1019` | AC-WKT-21-03; §11.2 | Coincide |
| 15 | El resumen lee `w.data` al dibujarse: rotar lo redibuja | `:965-991` | DV-43 | Coincide: el `StatelessWidget` del diálogo no escucha al *provider* y se reconstruye con la ruta |
| 16 | `Terminar` del diálogo: `pop` y `reset()` sin esperar; `finish()` usa `_startedAt!` después de los `await` | `:260-291`; `workout_provider.dart:283-295` | DV-42 | Coincide |
| 17 | Chip: textos y colores, `—` en otra fase; 8 × 3, tinte 15 %, radio 8, borde 50 %, 11/w800, `letterSpacing` 0,8 | `:530-547` | AC-WKT-16-03, -04 | Coincide. 0,08 em de 11 = 0,88 px, contra 0,8 px: no se nota |
| 18 | Barra libre: `rowing` 20 `#00B4D8`, 8, `Entrenamiento Libre` 16/w600 fijo, `Spacer`, reloj, 10, chip | `:497-519` | AC-WKT-16-01, -02 | Coincide. El legacy no recorta: desborda |
| 19 | Promedios: lecturas con SPM > 0 o W > 0; split > 0; vatios ≥ mín., SPM ≥ objetivo, split ≤ objetivo; sin split no se evalúa; sin remar, no cumplido | `step_target_tracker.dart:30-62` | AC-WKT-09-09, P-03 | Coincide. Con objetivos enteros, `⌊W⌋`, `⌊10·SPM⌋ / 10` y `⌈split⌉` nunca contradicen el veredicto |
| 20 | Reloj: su propio `Timer` de 1 s, se redibuja al cambiar el minuto o la hora, `HH:mm`, `schedule` 16 `white70`, 5, 18/w800 tabular | `series_panels.dart:57-110` | AC-WKT-19-01 a -03 | Coincide (20 en la app: P-07) |
| 21 | Modo inmersivo: `immersiveSticky` en `initState` y `edgeToEdge` en `dispose` | `immersive_workout_screen.dart:32-41` | AC-APP-15-01, -04 | Coincide |
| 22 | El reloj del motor cuenta disparos de `Timer.periodic(1 s)`. La VM de Dart saltea los vencidos: con el equipo dormido, el lapso no cuenta | `workout_provider.dart:147`, `:247` | AC-WKT-02-11 | Coincide |
| 23 | Colores de tipo `#EF476F`, `#06D6A0`, `#FFD166`, `#118AB2` y `#607D8B` | `lib/shared/theme.dart:56-69` | AC-WKT-15-01 | Coincide |
| 24 | La entrada: `startWithRoutine` sin esperar y `MaterialPageRoute` | `workout_screen.dart:163-175` | AC-WKT-13-01, -06 | Coincide |

Números recalculados con `R-4` (la lectura `L(k)` en `k − 0,5`):

- **`Corta`**: `00:30`, `1120 m` (1000 + 4·30) y `43` (40 + ⌊30 / 10⌋). Son 7 puntos: 6 de los 5 s y el final. Las píldoras 0 y 2 (SPM 18 y 20 contra 24) quedan cumplidas, la 1 sin objetivo y la 3 llena.
- **`Estados`**: sellos a los 10 (cumplido), 20 (24 < 30, no alcanzado) y 50 (cumplido), y ninguno a los 30 ni a los 60. La cuenta grande va en [7; 10), [17; 20), [27; 30), [47; 50) y [57; 60).
- **El reloj común**: el origen es 35,0, porque de 30 a 35 no hay "a continuación" ni "por terminar". Así p = 1 a los 47,6 = 35,6 + 10·1,2, y el aro en 0,8. En DEMO-06 el origen es 15,0, y en DEMO-10 es 235,0 (aro en 0,267).
- **`Pirámide 500`**: el trabajo de 500 m termina a los 725 s, con 4·(k − 600) ≥ 500. Da `150 / 150 W · 02:05 / 02:05/500m` (cumplido: 150 ≥ 150 y 125 ≤ 125).
- **`Prueba 4a`**: el trabajo 1 da `150 / 160 W · 24.0 / 22 spm` y el 2, `170 / 160 W · 26.0 / 24 spm`.
- **P-17**: `01:02`; puntos de los segundos 5 a 60 y después 65 a 120: 24 más el final, 25.
- **Anchos útiles**: 344,0 base en TV y 821,2 en TH.

---

## 4. La presentación no toca el dato ni el reloj (AC-WKT-13-14)

- **El mismo cuadro**: el motor publica en el acto en cada segundo y en cada lectura (`WorkoutEngine.cs` `Notify`/`Publish`), y el *store* del HUD rehace su estado en el mismo llamado (`WorkoutHudStore.OnSnapshot` → `Rebuild`). La banda, la cuenta de la barra y la cuenta grande salen de la misma foto, así que cambian en el mismo cuadro.
- **El instante del sello es el nominal**: `ProcessSecond` recibe `at = _clock.NextDueTicks`, el vencimiento y no la hora del cuadro. Así `LastOutcomeAt` no depende de los cuadros tarde, y "uno cuyos 2 s ya pasaron no se muestra" sale solo después de un alcance.
- **Un solo reloj**: `MotionDriver.Now` es `IClock.MonotonicSeconds` (`MotionDriver.cs:32`), el del programador, que pasa a ser el despierto. El relleno (tick del motor) y el `now` de la vista miden con el mismo reloj, también después de despertar.
- **Ninguna animación decide cuándo pasa algo**:
  - el `Schedule` de 300 ms del diálogo comparte la cola con el motor, pero no lo toca;
  - los controles que responden desde los 840 ms (P-15) frenan toques, no el motor;
  - la salida no demora la vuelta a reposo.
  - PIN (dotnet) e `IHT.PresentationNeverTouchesTheData` (PlayMode) lo prueban con y sin HUD.
- **Hueco**: `PillProgress` retrocede con un `Instant` dentro del mismo paso (QA-G4bT-04).

## 5. Nada animando al salir (ROB-02) y frame pacing

- **60 fps**: hoy la página los pide en la construcción y los suelta al empezar la salida (`ImmersiveWorkoutPage.cs`, `DeclareInitialSurface` y `ReleaseShell`). Durante los 640 ms, los pide el `MotionDriver` por sus animaciones. Al salir del árbol, `HudView.Dispose` para las diez piezas nuevas y el *store* dispone `WallClockTicker` (02-tech-design §3.12). La lista está completa para el HUD.
- **Huecos**:
  - `CompletionMotion` no se detiene al cerrar el diálogo;
  - las vistas nuevas que miden texto no tienen la guarda de QA-V4a-05 ("nada mide sin panel");
  - el periódico de 250 ms corre en el alcance;
  - el `Schedule` de 300 ms puede vencer dentro del alcance.

  Los cuatro van en QA-G4bT-08.
- **Medir la fluidez**: la medición en PlayMode y la sonda del build necesitan definir su métrica (QA-G4bT-11). Con el tope del editor (1998 pasos), la fila repinta 1998 tramos por cuadro en los últimos 5 s de cada paso, y HBT mide solo con 200.

## 6. Reducir movimiento (AC-APP-05-08)

La tabla de 02-tech-design §3.10 cumple la spec en todo salvo en un punto. **El destello del cambio de paso queda** (§3.4, §3.10; notas §4.5, §4.14), y la spec dice "sin destello ni trazo" (QA-G4bT-03). Las notas suman animaciones que no tienen variante de reducir movimiento: el cambio de modo de la fila (0-240) y la pastilla que funde (120). Van en QA-G4bT-01.

## 7. Capturas en *batchmode*: transiciones por código y estados intermedios

- **Por código**: en *batchmode*, una transición de USS queda congelada en su valor inicial (ANIMATION_BIBLE §0.1, principio 7). El diseño mueve todo por código con el reloj de prueba (§3), y el *spinner* también va por el programador (`HudControls.cs:99`). Ninguna pieza nueva depende de una transición de USS.
- **Los estados intermedios** los dan los videos: el arnés graba a 30 cuadros por segundo con el reloj de prueba, y el *manifest* guarda los valores de cada `IMotion` para compararlos con las líneas de tiempo puras (± 0,01). Está bien planteado.
- **Las tomas fijas**:
  - la regla "falla si corre una animación finita" no dice qué cuenta. `MotionDriver.HasFiniteMotions` (`:40`) incluye los resortes y los rodillos de la 4a, y §7.4 no dice cuándo son finitos el sello, la cuenta grande y el conteo (QA-G4bT-07);
  - CAP-22 no puede mostrar `Próximo` en su instante (QA-G4bT-06);
  - los +0,2 s de las notas (DN4b-25) corrigen el problema del rodillo en los segundos enteros, pero cambian lo esperado. Van en QA-G4bT-01 y -02 (e).

## 8. El motor espera al diálogo (DT4a-15) sin perder puntos ni cerrar mal

- **La tabla de fases** de §2.6 es coherente con el legacy: queda terminada hasta el `reset()`, y la rutina se evalúa y se cierra en el mismo segundo.
- **El cambio en el código** es de WP-A: hoy `AwaitClose` vuelve a reposo con el cierre (`WorkoutEngine.cs`, `ReturnToIdle(kind, saved)`), y con una rutina terminada sola tiene que quedarse en `Finished` con `Close = Saved/Failed`.
- **Sin perder puntos**:
  - el reloj se detiene en `EndSession`, así que no hay puntos después del fin;
  - el punto final y `Completion` salen de la misma lectura (`_fusion.Data`);
  - el grabador escribe punto por punto, sin temporizador (`SessionRecorder.cs`, encabezado), así que la espera no deja nada en memoria.
- **Sin cerrar mal**:
  - el tope de 15 s mide primer plano y reloj despierto;
  - si la app se cierra con el diálogo, `Abandon` no puede cortar una sesión ya cerrada (CHT.QuitWithTheDialogOpen lo prueba);
  - un segundo plano durante la espera manda lo pendiente (`FlushAndWait`) sobre un grabador que puede estar cerrando, igual que en la 4a;
  - `StartFree` en `Finished` da `Busy`.
- **La pantalla encendida** se pide para toda la app (`ScreenKeepAwake`, como `lib/main.dart:42`), así que la espera indefinida del diálogo no deja que Windows se suspenda solo.
- **Una precisión** para WP-B2: `Dismissible = false` tiene que bloquear el velo y `CancelTop`, pero no el cierre que hace la página al volver a reposo. Si no, el diálogo queda sobre Workout en reposo, la lección de QA-G4aT-04 (QA-G4bT-09).

## 9. P-17 de la 4a: el reloj despierto y el guardado incremental

- **La idea es buena**: restar el sueño en un solo lugar (`FrameScheduler`) hace que el motor, el remo simulado, los topes, el reloj de pared y el motion lo ignoren sin enseñarle nada a cada parte. Cubre los dos casos del monótono. Coincide con el legacy (§3, caso 22), y el guardado incremental no depende del programador (§8).
- **Las señales**: en Windows, el aviso de `powrprof` y la diferencia `GetTickCount64·10⁴ − QueryUnbiasedInterruptTime` son una buena elección. El *callback* que solo hace `Interlocked.Increment` respeta el hilo principal.
- **El modo de espera moderno** queda como riesgo aceptado: lo verifica el usuario (R4b-01) y el registro dice qué faltó.
- **Huecos** (QA-G4bT-05):
  - el descuento está solo en `Tick(in FrameClock)` (02-tech-design §2.9.2, `:568`). `ResumeFromBackground` (`FrameScheduler.cs:88`, llamado desde `AppRoot.cs:253`) sigue con el monótono crudo y haría el alcance de todo el lapso;
  - el aviso pendiente se consume con el primer cuadro de ≥ 1 s, aunque sea un tirón antes de dormir;
  - en el Editor, el *callback* nativo registrado en PlayMode tiene que darse de baja antes de la recarga de dominio. Si no, una suspensión posterior llama a código descargado;
  - la estructura `DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS` tiene que vivir lo que dura el registro.

## 10. Barras del sistema por plataforma

- **La regla** (App, pura, con cuenta de inmersivos para QA-V4a-01) y sus casos (§4.1) son correctos, y el PO solo tiene que precisar DT4b-05.
- **Android** (JNI en el hilo de UI, API 30 o más y 26 a 29) queda NO VERIFICABLE. El riesgo de que Unity vuelva a poner sus banderas al recuperar el foco está mitigado con el nuevo pedido al volver (R4b-05).
- **Escritorio** no hace nada. La PlayMode de AC-APP-15-02 solo prueba que nada toca `Screen`: en *batchmode*, `Screen` no cambia de todos modos. La prueba real es [Device-Win].
- **Los dos juegos de márgenes** (DD4b-07) evitan el salto del HUD.
- **Notas y diseño ya coinciden** en cuándo vuelven las barras (a los 640 ms, C-03).

## 11. UI Toolkit: ciclo de vida, *hit-test*, unidades, doble toque y páginas que salen

- **Hit-test**: en UI Toolkit, la opacidad 0 no saca un elemento del *picking*. Por eso hacen falta `SetInteractive(false)` en la cascada y `ExitLock` en la salida, y los dos están. La fila de píldoras, el sello, la cuenta grande y `PAUSA` van con `pickingMode = Ignore`. El `translate` de la cascada mueve el *picking* con la caja.
- **Unidades**: §7.5 ya dice que USS no tiene `em` ni `baseline` (QA-G4aT-14), y lo que crece desde 0 va en un *slot* que recorta. El disco de `PAUSA` de las notas (`border-radius` 36·s, `border-width` 3·s) y el *tracking* en em van por código.
- **Doble toque**:
  - `Terminar` del diálogo queda tomado desde el primer toque;
  - hay un solo diálogo de terminar (4a);
  - un toque que llega después del fin no cambia nada.
- **Páginas que salen**: la última foto (`Freeze`), `PopImmediately` con `Instant`, `OnPopped` a los 640 y `ExitLock` están bien. El diálogo cierra a la vez que empieza la salida, así que una sesión nueva durante la salida no hereda nada.
- **Teclado**: tiene dos huecos, documentados en QA-G4bT-09.
  - **Durante la cascada**: el *hook* del primer Tab en la raíz del panel (`ImmersiveWorkoutPage.cs:58`, `:337-345`) lleva el foco a `Terminar` con opacidad 0, y Enter lo activa.
  - **Con el diálogo abierto y nada enfocado**: el Tab llega a la raíz del panel. `FocusTrap` escucha en el árbol del diálogo (`FocusTrap.cs:21`) y la página se abstiene si hay diálogo (`:276`, `:289`, `:339`), así que nadie lo lleva a `Terminar`.

## 12. Paquetes disjuntos y contratos

- **Archivos**: son disjuntos entre WP-A, WP-B (B0 a B3, secuenciales) y WP-D. Hay dos excepciones sin declarar:
  - `Tests/EditMode/Project/{SnapshotEvidenceTests,ItFourBToolingTests}.cs` (de B3, dentro de `Tests/EditMode/**`, que es de A);
  - `Tokens.cs` (del Designer), que con DN4b-15 cambia WP-A.

  Van en QA-G4bT-10.
- **Contrato de motion** (§7.4): está bien pensado (*callbacks* con *structs* puros, sin conocer las vistas). Le faltan dos cosas:
  - cuándo es finito cada `IMotion` (QA-G4bT-07);
  - cuatro movimientos de las notas (QA-G4bT-01).
- **Contrato USS** (§7.5): las notas lo cambian en `#hud-paused-label`, `#hud-phase-label` (el *token*), `.rm-completion__row-value` y `-label`, la fila de información (modos) y el ancho del chip (QA-G4bT-01).

## 13. Contradicciones spec ↔ diseño técnico ↔ notas de diseño

| # | Tema | Spec | 02-tech-design | 04-design-notes | Issue |
|---|---|---|---|---|---|
| 1 | Reducir movimiento, cambio de paso | "sin destello ni trazo" (`01-po-spec.md:474`) | el destello queda (`:846`, `:959`) | el destello queda (`:547`, `:689`) | QA-G4bT-03 |
| 2 | Texto de `FINALIZADO` | `#FF5D7F`, "≥ 4,5:1" (`:259`) | `step.work.hud` `#FF5D7F`; PCH y CAP-15 lo exigen (`:471`, `:1210`, `:1116`) | 3,942: no llega; `#FF7A92` y un *token* nuevo (`:24`, §2.1) | QA-G4bT-02 (a), -01 |
| 3 | La pastilla en la fila más larga (TV) | recortada con "…" blanco (AC-WKT-14-15; CAP-19, -20) | igual, con la sonda del "…" (`:1120-1121`) | en TV no se ve, ni en es ni en en (`:252`) | QA-G4bT-02 (b) |
| 4 | Píldoras angostas | "cada tramo conserva su color y su estado" (AC-WKT-14-16) | `StripCurrentMin` 2, solo en la tira (`:234`, `:242`) | pulgar ≥ 4·s × 10·s en toda píldora < 6 base; rieles en la hueca (DN4b-18) | QA-G4bT-01, -02 (c) |
| 5 | Techo del panel en el inmersivo | AC-BLE-16-03 de la 4a (no está en §5) | sin cambios | 8 debajo de la barra de 74 y del disco (DN4b-23, `:410`) | QA-G4bT-02 (d) |
| 6 | Instantes de las tomas | segundos enteros (§7.3) | "el instante es el de la spec" (`:1088`) | +0,2 s; CAP-06 a 30,25 (`:772-784`) | QA-G4bT-01, -02 (e) |
| 7 | Título de la barra libre en TV | "…" (AC-WKT-16-06) | "…" | se achica hasta 0,8 (DN4b-27) | QA-G4bT-02 (f) |
| 8 | La barra de pasos | "dos filas" (`:454`) | tres, con el sello (DT4b-02) | tres | QA-G4bT-02 (g) |
| 9 | `PAUSA` | en el lugar de la cuenta grande; tamaño del Designer | cápsula `title` 20/700 en `Callout` (`:871`, `:1115`, `:1466`) | disco de 72·s en `BigCount`, glifo de 28 y 12/700; "un disco por vez" (DN4b-14, `:604`) | QA-G4bT-01 |
| 10 | Halo | "solo por fuera", "mide lo del legacy" (`:189`) | agrandado 2, desenfoque 8, recortado a la fila (`:829`) | desde el borde a 0,65·α, `(1 − d/E)²` hasta 10 / 8 / 4 (DN4b-17) | QA-G4bT-01, -12 |
| 11 | La llave con el diálogo | por encima del velo y usable | `16 + margen inferior` (`:1174`) | el hueco libre más bajo del carril izquierdo: lo otro se encima con `Terminar` o la banda (DN4b-22) | QA-G4bT-01 |
| 12 | Avisos del final | al volver a Workout | en el cuadro 0 de la salida (`:802`) | a los 400 (DN4b-24, `:499`) | QA-G4bT-01 |
| 13 | Valores del diálogo | cifras de tablero; tamaño del Designer | `num.stat` 16/700 (`:1483`) | `num.s` 20/700, con la `m` de sufijo (DN4b-21) | QA-G4bT-01 |
| 14 | Ancho del chip | — | 102,2 / 84,1 base (`:909`) | 101,8 / 74,1 (DN4b-26) | QA-G4bT-01 |
| 15 | Fila de información, qué cede | "lo fija el Designer" | DT4b-12 (`:1517`) | modos y fragmentos (DN4b-16), con dos animaciones nuevas (`:241`, `:243`) | QA-G4bT-01 |
| — | Entrada, salida, sello, colores del sello, barras del sistema, cuenta grande de 64 en un disco de 72 | — | la spec | **confirmados** por el Designer (DN4b-01 a -03) | resuelto |

## 14. Regla de repo público

- **Los cinco documentos de la iteración la cumplen**:
  - ninguna ruta absoluta, carpeta de usuario ni dato del equipo;
  - el legacy va por URL o `../rowmate-legacy`, y aparecen `<scratch>` y `<flutter-sdk>`;
  - `HKCU\Software\iguisoft\RowMate` (00-deuda-4a §8) es el `companyName` público de `ProjectSettings/ProjectSettings.asset:15`, sin usuario.
- **El oráculo**: lo que deja `pub get` en `tools/oracle/engine/` está en `.gitignore:45-50` (QA-G4aT-05 de la 4a), así que regenerar el guion nuevo no ensucia el árbol.
- **Las líneas de registro nuevas** (`[TIME]`, `[WORKOUT] el equipo durmió…`, `[PERF]`) no llevan rutas. Este documento tampoco: el *script* queda en `<scratch>/it4b/gate-tecnica/`, sin versionar.

## 15. Decisiones que le tocan a QA

| ID | Decisión |
|---|---|
| DT4b-06 | **Aceptado con condiciones**: las de QA-G4bT-05 (el alcance pasa por `SuspendAccounting`, el despertar se ancla al aviso de reanudar y el *callback* se da de baja en la recarga del Editor). La verificación en equipo es del usuario (demo (c)) |
| DT4b-07 | **Aceptado**. Condiciones: `SnapshotEvidenceTests` exige el reemplazo de cada toma `superseded` en `it4b`, y CAP-01 a CAP-11 de la 4a dan los mismos bytes. La regeneración única se verifica antes de WP-B1 |
| DT4b-14 | **Aceptado con la condición de QA-G4bT-11** (b): métrica definida y `enableFrameTimingStats` solo en el build de desarrollo con simulador |
| DT4b-17 | **Aceptado**: el peor caso nuevo es más exigente. Si S-B4b falla entera (64), se decide antes de WP-B1 con el informe (R4b-12) |
| DT4b-18 | Informativo, de acuerdo. Con "un disco por vez" (notas §4.8), la toma de los 47,6 s muestra `PAUSA` quieto |

Para el PO: QA-G4bT-02, -06 y -12, y las decisiones de DT4b-02, -03, -04, -05 y -09 que pide el Architect. Para el Designer: alinear las notas con QA-G4bT-03.

---

## 16. Issues

| ID | Severidad | Responsable | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-G4bT-01 | mayor | architect | Las 04-design-notes se escribieron después de 02-tech-design y no están integradas. Hay pedidos DN4b-14 a DN4b-29, varios marcados **[contrato]**, y respuestas que cambian lo planificado:<br>- **WP-A**: el *token* `phase.finished.hud` (135 → 136, `PhaseChip`, `tools/tokens`, conteos); la regla de la fila por modos y fragmentos (reemplaza DT4b-12); el halo desde el borde con largos por lado y textura propia; el pulgar y los rieles (`StripCurrentMin` 2 → 4 y alto 10); `PAUSA` en el disco en `HudLayout.BigCount`; R10 de la llave; el techo del panel; los instantes +0,2 s, con lo que esperan (0,510, 0,503, 0,507, 0,337, 0,607 y CAP-06 a 30,25);<br>- **WP-B**: los avisos del final a los 400; los valores del diálogo en `num.s`; el ancho del chip;<br>- **Comprobaciones del arnés que hoy chocan**: CAP-01 (0,500), CAP-14 (`Callout`), CAP-15 (`#FF5D7F`), CAP-19 y -20 (la pastilla con "…"), CAP-22 (mínimo 2·s);<br>- **Cuatro movimientos sin dueño** en §7.4 ni línea de tiempo en §3.1: el cambio de modo de la fila (0-240), la pastilla que funde (120), el pulgar que pasa (150) y "un disco por vez" (120 / 120), que coordina `BigCountMotion` y `PauseMotion`. Ninguno tiene variante de reducir movimiento | `04-design-notes.md:7`, `:894-917`, `:241`, `:243`, `:604`, `:712-716`, `:772-784`; `02-tech-design.md:234`, `:242`, `:471`, `:802`, `:829`, `:871`, `:909`, `:1102`, `:1115`, `:1116`, `:1120-1123`, `:1174`, `:1433-1448`, `:1466`, `:1483`, `:1517` | Un anexo "Cambios por 04-design-notes de la It. 4b" que acepte o rechace cada DN4b y actualice §1.1, §2.2, §2.7, §3.1, §3.4 a §3.9, §5.2, §5.3, §6.2, §7.4, §7.5 y §7.6. DN4b-14 a -18, -22, -23 y -25 antes de WP-A; los cuatro movimientos, con su línea de tiempo pura (WP-A) y su `IMotion` (WP-D1), antes de WP-D1; el resto antes de WP-B1 |
| QA-G4bT-02 | mayor | po | Decisiones y precisiones del PO que cambian textos de AC y lo que esperan las tomas:<br>(a) AC-WKT-16-03 y DV-44 piden ≥ 4,5:1 y nombran `#FF5D7F`, que da 3,942 sobre su tinte; el Designer da `#FF7A92`;<br>(b) AC-WKT-14-15, CAP-19 y CAP-20: en TV la pastilla no entra ni con su primer fragmento, en es ni en en; el AC y las tomas esperan "recortada con un … blanco";<br>(c) Q-59 y DV-40: el pulgar más alto y más ancho en toda píldora de menos de 6 base, también con separación 1 (39 a 115 pasos en TV);<br>(d) DN4b-23 cambia los números de AC-BLE-16-03 (4a, cerrado), que no está en §5: la matriz no lo vuelve a verificar;<br>(e) §7.3: los instantes enteros pasan a +0,2 s y CAP-06 a 30,25;<br>(f) AC-WKT-16-06: el título que se achica hasta 0,8;<br>(g) AC-WKT-14-01 dice "dos filas" y la barra suma la del sello (DT4b-02).<br>Además, las precisiones DT4b-05 y -09 | `01-po-spec.md:259`, `:208-214`, `:215-219`, `:454`, `:553-583`; `04-design-notes.md:24`, `:252`, `:189-193`, `:282-291`, `:410`, `:772-786`, `:921-936`; `02-tech-design.md:1510`, `:1514` | Corregir los textos de la spec: (a), (c), (d) (AC-BLE-16-03 a §5.2) y (e) antes de WP-A; (b), (f) y (g) antes de WP-B1. Con (b), que el "…" blanco se pruebe en PlayMode y en AUX-01 |
| QA-G4bT-03 | mayor | architect | Reducir movimiento en el cambio de paso de la píldora. AC-APP-05-08 dice "la píldora cambia de estado con un fundido, sin destello ni trazo". El diseño y las notas conservan el destello blanco (0,40 de pico en 120-340). `HTL.ReducedVariants` e `IHT.ReducedMotionPieces` codificarían lo contrario del AC, y la verificación lo marcaría FALLA | `01-po-spec.md:474`; `02-tech-design.md:846`, `:959`; `04-design-notes.md:547`, `:689` | Con reducir movimiento, sin destello: solo el fundido de 1 a 0,85 y el borde que funde en 160. Que el Designer alinee §4.5 y §4.14 de las notas. Si el PO prefiere el destello, que cambie el AC |
| QA-G4bT-04 | menor | architect | `PillProgress` retrocede con `Instant`. La regla "con `Instant` vale transcurrido / D en el acto" pisa a "dentro de un paso nunca baja". Pasa cuando llega un `Instant` sin segundo nuevo:<br>(i) **despertar en marcha** (`Woke`): a los 62,4 s el relleno muestra 62,4 / D, salta a 62 / D un cuadro y vuelve a subir;<br>(ii) **pausa con segundo plano o despertar**: 3,555 → 3,000 en el caso de la tabla de §2.3, y así queda hasta reanudar.<br>AC-WKT-14-10 dice "nunca retrocede". PPG ("`Instant` … en el acto") lo dejaría escrito | `02-tech-design.md:277-282`, `:1203`; `00-deuda-diseno.md:331`; `01-po-spec.md:188` | Con `Instant` en el mismo paso, `max(anterior; transcurrido / D)`. Con el reloj despierto, `ahora − tick` ya sigue continuo después de despertar. Sumar los dos casos a PPG |
| QA-G4bT-05 | menor | architect | P-17: el descuento del sueño solo está en `FrameScheduler.Tick(in FrameClock)`.<br>(a) `ResumeFromBackground` recibe el monótono crudo y hace el alcance en tiempo virtual. Si la vuelta de una suspensión llegara por `OnApplicationPause(false)` (no está medido qué hace el *player* de Windows al suspender; en macOS, 10b), procesaría todo el lapso dormido: 145 puntos en vez de 25;<br>(b) el aviso pendiente se consume con el primer cuadro de ≥ 1 s, aunque sea un tirón antes de dormir. Después, en el modo de espera moderno sin medida, el sueño real cuenta;<br>(c) en el Editor, el *callback* nativo registrado en PlayMode tiene que darse de baja antes de la recarga de dominio. Si no, una suspensión posterior llama a código descargado;<br>(d) la estructura del registro tiene que vivir lo que dura el registro | `02-tech-design.md:568-571`, `:578`, `:587`, `:1554`; `Core/Common/FrameScheduler.cs:88`; `Bootstrap/AppRoot.cs:237-257` | (a) `ResumeFromBackground(in FrameClock)` pasa por `SuspendAccounting`.<br>(b) Contar también `PBT_APMRESUMEAUTOMATIC` y `PBT_APMRESUMESUSPEND` y anclar el despertar al primer cuadro después de un aviso de reanudar.<br>(c) Dar de baja en `AssemblyReloadEvents.beforeAssemblyReload`, o no registrar el aviso en el Editor.<br>(d) La estructura en `Marshal.AllocHGlobal`, liberada en `Dispose`.<br>SUS y WSU con "suspensión durante el segundo plano" y "tirón antes de dormir" |
| QA-G4bT-06 | menor | po | CAP-22 a los 3015,0 s cae en el instante en que entra `Próximo`: el paso 100, de 30 s, llega a restante 15 en ese tick. La entrada de la 4a es un fundido de 200 y un ancho con resorte ω 12 que se asienta en ≈ 1 s. A los 3015,0 no se ve, y a los 3015,2 (DN4b-25) va en el 69 % del ancho. "Qué tiene que verse" pide `Próximo: Descanso` y `0:15`, y la regla de §5.2 hace fallar la toma | `01-po-spec.md:574`; `02-tech-design.md:1123`; `04-design-notes.md:780`; `Presentation/Workout/StepInfoBar.cs:255`, `:446`, `:465`; `Presentation/Motion/SpringMotion.cs:92` | CAP-22 a los 3016,6 s (`0:14`, relleno 0,553 y el pulgar en su lugar), o a los 3014,6 sin `Próximo` (`0:16`), con "Qué tiene que verse" al día |
| QA-G4bT-07 | mayor | architect | La regla de toma de §5.2 punto 1 ("si en ese instante corre una animación finita, la toma falla") no tiene definición operativa:<br>(a) `MotionDriver.HasFiniteMotions` cuenta todo `IMotion` finito, y §7.4 no dice cuándo es finito cada uno. Si `SealMotion`, `BigCountMotion` y `CompletionMotion` son finitos toda su vida (lo natural en un movimiento con fin), fallan CAP-02 a -05, -25, -26, -29 y -30 y DEMO-06, -07, -09 y -10;<br>(b) **bordes exactos**: CAP-06 a los 30,2 coincide con el fin de `BigCountOutMs` (200), y `30.2 − 30.0 = 0.1999999999999993` s;<br>(c) DEMO-04 y DEMO-12 toman en "la salida (640 ms)", cuando la llave empieza a entrar en el carril izquierdo (hasta los 800). DEMO-08 toma justo después de `sim-open` y los `+`, con el resorte del panel, el rodillo y el semáforo corriendo;<br>(d) **los resortes de la 4a siguen finitos**: el ancho de `Próximo` al cambiar el tipo siguiente se asienta en ≈ 0,8 s, así que en CAP-03, -04, -26 y -30 (a +0,6 s del cambio) todavía corre | `02-tech-design.md:1088`, `:1107`, `:1142`, `:1146`, `:1150`, `:1435-1448`; `Presentation/Motion/MotionDriver.cs:40-55`; `SpringMotion.cs:92`; `StepInfoBar.cs:222-223`, `:324` | En §7.4, `IsFinite` por fase: finito en las transiciones; continuo en el sostén del sello (300-1800), entre ticks en la cuenta grande y en el aro. Que la regla nombre su lista (o un `IsTransitioning`) y diga si los resortes de la 4a cuentan. Comparar en ms enteros o con tolerancia. DEMO-04 y DEMO-12 a ≥ 800 ms de la salida, y DEMO-08 a `AdvanceTo(101,6)` |
| QA-G4bT-08 | menor | architect | ROB-02 y el ciclo de vida con las piezas nuevas:<br>(a) `CompletionMotion` no se detiene al cerrar el diálogo; §3.12 solo dice "salió antes". Si `Terminar` llega durante el conteo (300-1460 ms) con el cierre ya vuelto, la página sale a los 640 y el conteo sigue en el `MotionDriver` hasta los 1460, sobre números fuera del panel, con 60 fps de más. `ModalDialog.OnClosing` es el gancho;<br>(b) las vistas nuevas que miden texto (el chip, el sello, la fila por modos, `PAUSA`, el título que se achica) no tienen la guarda de QA-V4a-05 ("nada mide sin panel");<br>(c) el periódico de 250 ms del reloj de pared corre en el alcance: 4 disparos por segundo virtual que no publican nada, 115 200 en 8 h;<br>(d) el `Schedule` de 300 ms del diálogo puede vencer dentro del alcance, si la app pasa a segundo plano en esos 300 ms | `02-tech-design.md:460`, `:512`, `:993-997`; `Presentation/Shell/ModalDialog.cs:208`, `:212-229`; `00-deuda-4a.md` §3; `Core/Common/FrameScheduler.cs:88-107` | (a) `CompletionDialog.OnClosing` → `CompletionMotion.Stop()`.<br>(b) Cada vista nueva para su `IMotion` en `DetachFromPanelEvent` y no mide sin panel.<br>(c) `WallClockTicker` sin periódico mientras `InBackground`.<br>(d) Si vence en el alcance, abrir con `instantly`.<br>`NothingAnimatesAfterTheExit` con `Terminar` durante el conteo, y 0 avisos de DPI con el sello y el chip a la vista |
| QA-G4bT-09 | menor | architect | Teclado y el diálogo modal:<br>(a) **durante la cascada**, `SetInteractive(false)` solo habla de toques. El primer Tab sin foco entra al anillo por la raíz del panel y enfoca `Terminar` con opacidad 0, y Enter lo activa. Va contra AC-APP-05-07 ("un Enter nunca activa un control enfocado que no se ve") y contra P-15;<br>(b) **con el diálogo de completado abierto y nada enfocado** (AC-WKT-21-11), el Tab llega a la raíz del panel. `FocusTrap` escucha en el árbol del diálogo y la página se abstiene si hay diálogo: nadie lo lleva a `Terminar`, y el foco por defecto puede caer en otra capa;<br>(c) `Dismissible = false` tiene que bloquear el velo y `CancelTop`, no el cierre de la página al volver a reposo (QA-G4aT-04) | `02-tech-design.md:779`, `:942`, `:947`; `Presentation/Workout/ImmersiveWorkoutPage.cs:58`, `:276`, `:289`, `:337-345`; `Presentation/Shell/FocusTrap.cs:7-9`, `:21`; `00-deuda-4a.md:82` | (a) `SetInteractive(false)` también saca el foco, o la página ignora Tab y Enter hasta los 840 (con reducir movimiento, desde el 0).<br>(b) `DialogLayer` escucha la raíz del panel y, con un diálogo arriba y sin foco adentro, enfoca `FocusTrap.Next(Top.Box, null, …)`.<br>(c) Un cierre explícito de la página.<br>ITT con Tab y Enter a los 500 ms; CDT con el mouse antes |
| QA-G4bT-10 | menor | architect | Dueños y sondas:<br>(a) `Tests/EditMode/Project/{SnapshotEvidenceTests,ItFourBToolingTests}.cs` son de B3, pero `Tests/EditMode/**` es de WP-A, sin excepción en §7.6;<br>(b) `Tokens.cs` es del Designer y lo cambia WP-A (DN4b-15), sin excepción;<br>(c) S-B4b no prueba el texto enriquecido del sello (13/700 en el color del veredicto y 12/600 blanco en una etiqueta), ni el glifo de `PAUSA` en un `Label` (DN4b-14), la textura del halo con largos por lado (DN4b-17) o el corte por fragmentos (DN4b-16). Si el sello no da, son dos etiquetas (+1) | `02-tech-design.md:155`, `:1401-1411`, `:1424`, `:1488-1496`; `04-design-notes.md:972-975` | Sumar (a) y (b) a §7.6, como QA-G4aT-15, y los cuatro puntos de (c) a la sonda S-B4b, con su plan B y su costo en el presupuesto |
| QA-G4bT-11 | menor | architect | Rendimiento y pruebas largas:<br>(a) `HBT.LongSeriesStaysSmooth` acota a 33 ms el tiempo de CPU del cuadro del arnés en PlayMode y *batchmode*. Incluye el Editor y es frágil con la PC cargada;<br>(b) `FrameTimeProbe` con `FrameTimingManager` necesita `enableFrameTimingStats`. Con el `Stopwatch` mide el intervalo entre cuadros (con vsync): AC-WKT-14-19 no dice cuál;<br>(c) con el tope del editor (1998 pasos), la fila repinta 1998 tramos por cuadro en los últimos 5 s, y HBT mide solo 200;<br>(d) `CDT.SizesAndLetter` con `Maratón` (35 999 s) en 16 combinaciones: correr 10 h simuladas con el HUD montado en cada una es caro | `02-tech-design.md:948`, `:1001-1004`; `01-po-spec.md:234` | (a) Medir el trabajo del HUD (`Apply` y `generateVisualContent`) con su `Stopwatch`.<br>(b) Fijar la métrica (CPU del hilo principal) y prender las estadísticas solo en el build de desarrollo con simulador.<br>(c) HBT también con 1998, o juntar los tramos por píxel.<br>(d) Abrir el diálogo con el `HudCompletion` del caso |
| QA-G4bT-12 | menor | po | Paridad del halo. En el legacy, el `BoxShadow` (`BlurStyle.normal`) se pinta también debajo de la píldora, y la pista al 30 % lo deja ver: la en curso entera se aclara con el pulso (efectivo 0,3 + 0,7·α). La spec ("solo por fuera") y las notas ("la pista no se tiñe") lo quitan sin registrarlo, y AC-WKT-14-11 dice "mide lo del legacy" | `step_pill.dart:57-101`; `01-po-spec.md:189`; `04-design-notes.md:197`, `:207` | Registrarlo como diferencia (DV o "sistema visual", con el Designer) o teñir la pista como el legacy |

---

## 17. Lo que este gate no verificó

- **Unity no se abrió** (la tarea es sin Unity): las dos condiciones de la 4a, la regeneración única y las APIs de UI Toolkit 6000.3 de S-B4b quedan para QA antes de WP-B1.
- **La plataforma real**: el comportamiento de `OnApplicationPause` al suspender Windows, si llegan el aviso y la medida en el modo de espera moderno, IL2CPP y Android quedan NO VERIFICABLE. P-17 en el equipo es del usuario (demo (c)).
- **La geometría y los contrastes de las notas** (los rectángulos, los anchos medidos con K2, los pares B1 a B10) no se recalcularon: son de la lente visual. Solo rehíce los anchos útiles de la fila y los tiempos de los resortes que usa QA-G4bT-06 y -07.
- **El oráculo**: no corrí `run-oracle.ps1 -Engine`. El guion nuevo `completed_hold_reset` coincide con lo que hace el legacy, leído en el código (§3, casos 10 y 16).
