# It. 4b — Deuda de la It. 4a, saldada al empezar

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-29 |
| Iteración | 4b · `04b-hud-exr` ([ROADMAP §It. 4b](../../ROADMAP.md)). Primer trabajo de la iteración, antes de construir encima |
| Entrada | [REPORT de la 4a](../04a-motor-hud-funcional/REPORT.md) §5.1, §7 a §9; [05-qa-verification de la 4a](../04a-motor-hud-funcional/05-qa-verification.md), ronda 2 (R2.6); [lente de paridad](../04a-motor-hud-funcional/05-qa-lens-paridad.md) §2 y §4; [lente de robustez](../04a-motor-hud-funcional/05-qa-lens-robustez.md); [02-tech-design de la 4a](../04a-motor-hud-funcional/02-tech-design.md) §6.14 y §7.4; [00-deuda-diseno de la 4a](../04a-motor-hud-funcional/00-deuda-diseno.md) §3; el visto bueno del Designer en [00-deuda-diseno de la 4b](00-deuda-diseno.md) §1 y §2 |
| Qué se salda | Las dos **condiciones** del cierre de la 4a: QA-V4a-03 (AC-WKT-17-04) y QA-V4a-04 (AC-APP-05-07). Además QA-V4a-05, QA-V4a-06 (WP-B0) y QA-V4a-L02. QA-V4a-08 lo resolvió el coordinador |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) en `3ae0f3a`, solo lectura. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; la guardia de `run-oracle.ps1` dio "sin cambios". No se leyó el módulo de credenciales de Strava |
| Repo | Sin commits. Salidas, registros y sondas en `<scratch>/it4b/debt/` |

---

## 0. Resumen

| ID | Qué pasaba | Qué se hizo | Estado |
|---|---|---|---|
| **QA-V4a-03** (condición) | El divisor de la banda cedía su ancho y quedaba en 0 en TV, TH y L640 | `flex-shrink: 0` en el divisor; prueba del ancho en los 4 perfiles y 5 casos; sonda de color en CAP-12 y CAP-15 (DD4b-01) | **AC-WKT-17-04 pasa** |
| **QA-V4a-04** (condición) | Después del mouse, el primer Tab enfocaba sin anillo (HUD, Workout y editor de Rutinas) | `FocusHighlight` escucha en la raíz del panel; 3 PlayMode "sin foco, un Tab enfoca con anillo"; visto en el build | **AC-APP-05-07 pasa** |
| QA-V4a-05 | `TabularNumber` seguía rodando y midiendo fuera del panel: avisos de DPI en el registro del player | El rodillo termina al salir del panel y nada mide sin panel; PlayMode nueva | **Cerrado**: 128 avisos antes, **0** después, en el player |
| QA-V4a-06 | WP-B0 sin integrar: kerning K2 y RowMate Numeric SC | El *bootstrapper* escribe las tablas K2 y crea la derivada; Barlow fuera; pruebas en dotnet y Unity; `ScalesOfTheDesignNotes` sin `[Ignore]`; CAP-15, -16 y -24 ajustadas | **Cerrado** |
| QA-V4a-L02 | El oráculo del motor no cubría los caminos que la lente probó a mano | 15 guiones nuevos en el oráculo de Flutter y en `WorkoutGoldenTests`; DV-37 como diferencia esperada | **Cerrado** |

Cada arreglo tiene una prueba que **falla sin él** (§6.3). Una sola regeneración de `it2`, `it3` e `it4a`, con dos corridas seguidas iguales byte a byte (§7).

| Corrida | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix -UnityResults`) | Build 0 advertencias, 0 errores · Simulator **173 / 173** · EditMode **2064: 2042 pasan, 0 fallan, 22 ignoradas** (eran 2055) · Matriz **PASA 534, FALLA 0, IGNORADO 1** (AC-WKT-02-10, Android), PENDIENTE UNITY 0, SIN TEST 0 |
| Unity EditMode | **2103: 2077 pasan, 0 fallan, 26 ignoradas** (eran 2093) |
| Unity EditMode con simulador | **2276: 2250 pasan, 0 fallan, 26 ignoradas, 0 inconclusas** (eran 2266) |
| Unity PlayMode | **270: 270 pasan, 0 fallan, 0 ignoradas** (eran 264, con 1 ignorada) |
| Unity PlayMode con simulador | **334: 334 pasan, 0 fallan, 0 ignoradas** (eran 328, con 1 ignorada) |
| Arnés `it4a` / `it3` / `it2` | 49 / 47 / 41 tomas, **0 con problemas, 0 sondas que fallan**; dos corridas iguales byte a byte (en `manifest.json` cambia solo `durationMs`) |
| Player de simulador (PC) | 0 avisos de DPI en tres cierres del inmersivo; el primer Tab después del mouse, con anillo |

---

## 1. QA-V4a-03: el divisor de la banda (condición; AC-WKT-17-04)

### 1.1 Causa y arreglo

- **Causa** (la de QA). La banda mide lo que calcula `HudGeometry` con los anchos naturales de las cifras, y `TabularNumber` redondea su caja hacia arriba a píxeles. Cuando el contenido quedaba apenas más ancho que la banda, el divisor era el único hijo que se podía achicar (`flex-shrink` 1, el valor por defecto de UI Toolkit) y cedía esa fracción.
- **Arreglo**:
  - `Presentation/Workout/HudBand.cs`: `flexShrink = 0` en el divisor, que además se llama `#hud-band-divider` (así lo ven el manifiesto del arnés y las sondas);
  - `Presentation/Styles/Base.uss`: la regla `.rm-hud-band__divider { flex-shrink: 0; }`.
- **Lo que sugería QA de revisar** (que `HudZones` le dé a la banda el ancho de su contenido redondeado a píxeles): revisado y se deja como está.
  - La diferencia es de menos de 2 px, de los dos `TabularNumber`.
  - Cae en el relleno de la derecha, de 20 o 14 dp.
  - Redondear en `HudGeometry` movería las comprobaciones de rectángulos de 04a/04-design-notes §3.8, que salen de la geometría sin redondear.

### 1.2 Pruebas

- **`HudLayoutTests.BandDividerKeepsItsWidth`** (PlayMode, AC-WKT-17-04). Corre en TV, TH, L640 y W1280, con libre, libre con pulso, rutina con objetivos, los largos de CAP-16 y los extremos de FTMS. Comprueba:
  - el divisor mide 1 × 32 dp base **al píxel**: nunca menos de un píxel físico y a menos de un píxel de 1 dp;
  - tiene `flex-shrink` 0, se dibuja y su alfa es 12 %;
  - en la ventana ocupa al menos un píxel de layout;
  - a cada lado tiene su margen (16 o 10 dp) × `s`, ± un píxel.
- Con el arreglo quitado, falla: `TV Free: el divisor mide 0.381 × 32 dp con densidad 2.625` y `flex-shrink del divisor = 1` (§6.3).
- **Sonda de color, DD4b-01** (`WorkoutCatalog.DividerProbe`): en CAP-12 (TV) y CAP-15 (TH), un punto en el medio del divisor tiene que dar blanco al 12 % sobre el vidrio y el agua, `#263241` ± 2. Las dos dan `#263241` exacto.

### 1.3 En las capturas regeneradas

Se midió el píxel del medio del divisor en todas las tomas de `it4a` con banda (Pillow, `<scratch>/it4b/debt/divider_check.py`):

| Tomas | Divisor (dp de ventana) | Color | Contra el vidrio |
|---|---|---|---|
| TV: CAP-12, -13, -14, -18 a -22, -27a, -27c, -29, -30 | x 139,44 (el Designer: 139,4), 0,80 de ancho (2 px de 2,625) | `#263241` | `#081627`: salto de 30 |
| TH: CAP-15, -23 | x 126,58 (el Designer: 126,8) | `#263241` | salto de 30 |
| W1280: CAP-17, -25, DEMO-02 a -04 y -08 a -12 | x 191,60, 1,6 | `#263241`, con sus bordes suavizados | salto de 30 |
| L640: CAP-16, CAP-24 | 0,92 de ancho (1 px de 1) | `#222F3E` en su columna (x 92 y x 101) | salto de 26: se ve |
| Con el velo del diálogo: CAP-26, CAP-27b, DEMO-05 | como TV y W1280 | `#0F141A` | salto de 12 contra el vidrio velado: se ve |

Con densidad 2,625, UI Toolkit pone cada borde en un píxel. Por eso el divisor mide 2 px (0,762 dp base, 0,80 en la ventana) y no los 2,77 px de la cuenta sin redondear de 04b/00-deuda-diseno §1.2. El color y el lugar son los de esa tabla.

---

## 2. QA-V4a-04: el primer Tab después del mouse (condición; AC-APP-05-07)

### 2.1 Causa y arreglo

- **Causa** (la de QA). Con DT4a-01, después de un clic no queda ningún control enfocado. Sin foco, UI Toolkit manda el `KeyDown` y el `NavigationMoveEvent` del Tab a la **raíz del panel**. Ese camino no pasa por `#app-root`, donde `FocusHighlight` escuchaba. El foco llegaba a `Terminar` (o a `←` en el editor) sin `.rm-keyboard-nav`, y por eso sin anillo.
- **Arreglo** (`Presentation/Controls/FocusHighlight.cs`, el camino que proponía QA):
  - registra sus tres callbacks (tecla, navegación y puntero, con `TrickleDown`) en `panel.visualTree`, al adjuntarse al panel, y los saca al salir;
  - la clase sigue en `#app-root`, donde la buscan el USS, `ExitLock` y `PopupLayer`;
  - `ListeningOn` expone dónde escucha, para las pruebas.
- Sin cambios de producto: el editor sigue saliendo sin confirmar, como el legacy (decisión del PO al cerrar la 4a).

### 2.2 Pruebas

Las tres arrancan **sin ningún control enfocado**: es la condición que dejó DT4a-01, y `UiTestHost.KeyTarget` manda entonces la tecla a la raíz.

| Prueba (PlayMode, AC-APP-05-07) | Qué hace | Afirma |
|---|---|---|
| `HudControlsTests.FirstTabWithoutFocusShowsTheRing` | Libre; `Terminar` y `Cancelar` con toques | Nada enfocado ni anillo antes; el primer Tab enfoca `#hud-finish` con `.rm-keyboard-nav` y su `.rm-focus-ring` a la vista. Un puntero lo apaga. `FocusHighlight` escucha en `panel.visualTree` |
| `HudControlsTests.FirstTabWithoutFocusShowsTheRingAtRest` | Workout en reposo, sin foco | El primer Tab enfoca algo, con el anillo a la vista |
| `RoutineEditorTests.FirstTabWithoutFocusShowsTheRingInTheEditor` | El editor abierto desde la tarjeta, sin foco | El primer Tab cae en `←` (`Editor.Back`) con el anillo a la vista |

Con el arreglo quitado, fallan las tres: `el primer Tab ya es navegación con teclado: Expected True But was False` (§6.3). Las pruebas anteriores de AC-APP-05-07 siguen pasando: el orden de Tab, Enter y Espacio, Escape, y el foco solo con teclado.

### 2.3 En el build (equipo)

En el build de simulador nuevo (§8), con 1280 × 720: `Free workout` → `Finish` → `Cancel` con el mouse.

- **Antes del Tab**, nada tiene anillo.
- **El primer Tab** enfoca `Finish` con el anillo blanco y el halo oscuro (`<scratch>/it4b/debt/ring-c-hud-tab1.png`).

Es el paso E9 (c) de QA, que en la 4a daba "sin anillo".

---

## 3. QA-V4a-05: `TabularNumber` fuera del panel

### 3.1 Arreglo (`Presentation/Controls/TabularNumber.cs`)

- En `DetachFromPanelEvent`, el rodillo en curso termina en el acto: sale del `MotionDriver` y no mide. La caja queda marcada para medirse de nuevo.
- Sin panel no se mide nada:
  - `ApplySize` no lee `scaledPixelsPerPoint`, que fuera del panel anota "Trying to access the DPI setting of a visual element that is not on a panel." y devuelve el DPI de la GUI;
  - `Tick` termina el rodillo si el número quedó afuera;
  - `SetValue` no arranca un rodillo sin panel.
- Al volver a un panel (`AttachToPanelEvent`), la caja se mide con el DPI de ese panel.

### 3.2 Prueba y verificación

- **`ImmersiveHudTests.DigitsLeavingThePanelStopRolling`** (PlayMode):
  1. hace rodar las cifras del HUD;
  2. saca el HUD del panel: ningún número queda rodando ni en el `MotionDriver`;
  3. avanza 0,5 s: **0 mensajes con "DPI"** en `Application.logMessageReceived`;
  4. lo vuelve a poner: cada número mide otra vez su ancho al píxel.

  Con el `TabularNumber` de la 4a, falla en el paso 2: `fuera del panel el rodillo termina en el acto: hud-spm-value` (§6.3).
- **En el player** (§8), el mismo recorrido tres veces: `Workout` → `Entrenamiento libre` → 8 s → `Terminar` → `Terminar`.

  | Build | Avisos de DPI en el registro | Líneas con `TabularNumber` | Sesiones |
  |---|---|---|---|
  | El de la 4a, el que dejó el coordinador | **128** | 264 (`ApplySize` ← `Tick`, como en la ronda 2) | 3 cerradas |
  | El del código de esta deuda | **0** | 0 | 3 cerradas |

---

## 4. QA-V4a-06: WP-B0, kerning K2 y RowMate Numeric SC

Es lo que pedían 02-tech-design §6.14 y §7.4 de la 4a (DD4a-06, DD4a-07, DN4a-22), con el visto bueno del Designer en 04b/00-deuda-diseno §2.

### 4.1 Código

| Archivo | Cambio |
|---|---|
| `Core/Design/FontAssetPaths.cs` | El rol numérico (600, 700 y 800) pasa a `RowMateNumericSC-*`. `KerningFor(familia, peso)` da la lista K2 de las caras que dibujan texto: Inter ×4 y la derivada 700; la derivada 600 y 800 no llevan (K4). `RetiredFontAssets`: los tres de Barlow. `All` sigue en 10 y `TextFaces` en 7 |
| `Core/Design/KerningList.cs` (nuevo, Core puro) | Lee `UI/Fonts/Kerning/<cara>.pairs.json` y hace la única cuenta del `XAdvance`: unidades / unidades por em × muestreo, en `float`. La usan el *bootstrapper* y la prueba |
| `Editor/Bootstrap/FontAssetBuilder.cs` | Crea los tres `FontAsset` de la derivada (90 / 9, 1024², dinámicos, vacíos). Escribe en `m_FontFeatureTable.m_GlyphPairAdjustmentRecords` **exactamente** la lista: el `XAdvance` del primero y el resto en 0. Lo hace solo si difiere, así que es idempotente. Falla si la lista quedó vieja, porque el SHA-256 del `.ttf` no coincide. Borra los `FontAsset` de Barlow |
| `Presentation/Styles/Typography.uss` | Regenerado con `tools/tokens`: `.rm-type-num-*` en la derivada |
| `Core/Design/Tokens.cs`, `FontFamilyRole.cs` | Solo comentarios. Ningún token cambia (135) |

El *bootstrapper* se aplicó con `ProjectBootstrapper.ReapplyFromCommandLine`, como en la It. 3. No se subió la versión, que sigue en 3.

| Corrida | `ui.fonts` |
|---|---|
| Primera | `FontAsset creados: 3, ya existían: 7; getFontFeatures apagado en 3; kerning K2 (pares): Inter-Regular-SDF 1445, Inter-Medium-SDF 1695, Inter-SemiBold-SDF 1695, Inter-Bold-SDF 1695, RowMateNumericSC-SemiBold-SDF 0, RowMateNumericSC-Bold-SDF 1272, RowMateNumericSC-ExtraBold-SDF 0; FontAsset de Barlow borrados: 3` |
| Segunda | `creados: 0 … borrados: 0`, y los `.asset` con los mismos md5: es idempotente |

Las tablas parciales de Inter y de Barlow 700 sumaban 20,5 MB; con K2, los `.asset` de Inter y de la derivada 700 suman unos 3,2 MB. Ninguna prueba, captura ni build los volvió a escribir: `GeneratedAssetGuard` y `ItTwoDebtTests` lo controlan.

### 4.2 Pruebas

| Prueba | Dónde | Qué afirma |
|---|---|---|
| `ItFourAFontTests` (5) | dotnet y Unity EditMode | K1: `m_GetFontFeatures: 0` en las 7 caras. K2 y K3: la tabla de cada `.asset` es exactamente su lista, en el mismo orden, con el mismo `XAdvance` en `float` y el resto en 0, y el muestreo (48 o 90) y las unidades por em coinciden. K4: la derivada 600 y 800 sin tabla. La lista es la del `.ttf` versionado (SHA-256), ordenada y sin ceros. El juego fijo cubre cada carácter del catálogo es/en, el ASCII imprimible y `áéíóúüñÁÉÍÓÚÜÑ¿¡×±·°`. Ningún `.uss`, `.tss`, `.asset`, `.unity` ni `.prefab` nombra los `FontAsset` de Barlow, que ya no existen |
| `FontAndIconAssetsTests.DerivedDigitsShareOneAdvance` | Unity EditMode | En cada derivada, familia "RowMate Numeric SC", muestreo 90, y las diez cifras con un solo avance: 507, 521 y 535 unidades |
| `HudLayoutTests.ScalesOfTheDesignNotes` | PlayMode, **sin `[Ignore]`** | Las 14 escalas de 04a/04-design-notes §3.7, ± 0,005 (por ejemplo, L640 banda 0,776; CAP-16 banda 0,561; L640 SPM con objetivo 0,962) |
| `HudLayoutTests.DerivedDigitWidthsOfTheDesignNotes` (nueva) | PlayMode | Los anchos de 04b/00-deuda-diseno §2.2, ± 0,5: `24.0` en 64/800 da 123,2; `02:13` en 44 da 105,8; `150`, 68,8; `00:42` en 34, 81,7; `1048`, 70,9; `11` en 32, 33,3; `0:40` en 20, 37,7 |
| `TypographyUssSyncTests` | dotnet | `.rm-type-num-xl` en `RowMateNumericSC-Bold-SDF.asset` |

### 4.3 Comprobaciones del arnés que dejó flojas la 4a

| Toma | Antes | Ahora |
|---|---|---|
| CAP-15 (TH) | Sin la banda ni Split | **Split** en el rectángulo de §3.8 (758,3-899,2 · 94,8-180,1), ± 1. **La banda**: el rectángulo de §3.8 (14,8-242,5) es el de `00:42 │ 1048 m`, y el simulador del arnés lleva `162 m`. Por eso se comprueban la misma esquina y el mismo alto (14,8 · 318,3-394,1, ± 1), el ancho de `HudGeometry` (± 1) y `00:42` en celdas de la derivada (4 × 0,521 + 0,32 em a 34, ± 0,6 dp base). Ver §9 |
| CAP-16 (L640) | La banda achicada, < 1 | La banda a **0,561 ± 0,005**, la de `HudGeometry` |
| CAP-24 (L640) | SPM achicado, < 1 | SPM a **0,962 ± 0,005** (T2), la de `HudGeometry` |
| CAP-19, -23 y -25 | La sonda de la tendencia, en (0,5936; 0,40) de la pastilla | Sin mover: con K2 la pastilla no se corrió y la sonda da `#4CC9F0` exacto |

---

## 5. QA-V4a-L02: los guiones del oráculo del motor

### 5.1 Oráculo (Flutter, `tools/oracle/engine/test/engine_vectors_test.dart`)

Sección nueva `scripts`, con el arnés de la lente pasado al repo (`script()`).

- **Las acciones.** Cada guion es una lista `[ms, tipo, argumento]`:
  - los tipos: `free`, `routine`, `pause`, `resume`, `finish`, `reset`, `bpm` y `status`;
  - las rutinas van **dentro del escenario**, con sus pasos, así el motor nuevo corre exactamente lo mismo.
- **Lo que se anota**, en la grilla de 100 ms:
  - los cambios de paso, con `-99` donde el `stepProgress` del legacy lanza;
  - el reloj y la fase después de cada acción;
  - el instante del fin automático y los objetivos en ese instante;
  - el orden de las operaciones;
  - las altas, los puntos y los cierres, con la marca de fin de cada cierre.

Los 15 guiones son los 13 caminos de la lente (§2 y §4), terminar en un múltiplo de 5 y el monitor que informa la distancia cada 2 s (QA-V4a-L01):

`zero_steps`, `neg_step`, `nogoal_finish`, `finish_in_pause`, `dist_last`, `time_and_distance`, `hr_points`, `pause_early`, `corta_pause_across_change`, `two_sessions`, `pause_resume_jitter`, `finish_at_step_change`, `last_step_zero`, `free_finish_at_5` y `dist100_every_2s`.

`powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Engine`:

- `pub get --offline` y `flutter test`, con la guardia del legacy en "sin cambios";
- `workout_engine.json` pasa de 81 020 a 132 874 bytes, dentro del tope de 1 MiB;
- los 11 escenarios de la 4a salen idénticos, y del encabezado cambia solo `generatedAt`.

Los resultados del legacy son los de la lente: por ejemplo, `zero_steps` termina a los 9 s y `neg_step` lanza mientras dura el paso de −5 s.

### 5.2 `Tests/EditMode/Workout/WorkoutGoldenTests.cs` (sin Flutter)

| Prueba | Qué compara |
|---|---|
| `ScriptsComeFromTheLegacyEngine` | Los 15 nombres, en orden, y que cada cierre del legacy tenga fin |
| `ScriptsAreIdenticalToTheLegacy` | En **13 guiones**: los puntos (11 campos y el SPM por sus bits), las altas (la rutina de la plantilla), los cierres (totales y P99), los objetivos del fin automático, los cambios de paso, el instante del fin automático y el reloj y la fase después de cada acción. Quedan afuera `finish` y `reset`, que son distintos a propósito (DT4a-15) |
| `NegativeStepIsTheExpectedDifference` | DV-37: el legacy lanza una vez, y lo anota como `-99`. En ese cambio, el motor nuevo está en un paso de duración negativa con 0 restante, y todo lo demás es idéntico: el punto, el cierre, el fin del paso en el segundo 1 y el de la rutina en el 4 |
| `MonitorEveryTwoSecondsMatchesUntilTheFirstStepChange` | QA-V4a-L01: el legacy cambia de paso a los 20 s, y los 4 puntos del paso 0 son iguales en los dos motores. No se fija lo que hace el motor nuevo después: lo decide la 10a |

**Prueba de mutación.** En una copia de `workout_engine.json` se tocaron un reloj de una acción, un cambio de paso y una distancia, y `ScriptsAreIdenticalToTheLegacy` marcó las tres. El archivo se restauró y quedó con el mismo md5.

---

## 6. Corridas

### 6.1 Comandos (desde la raíz del repo)

| # | Comando | Resultado |
|---|---|---|
| 1 | `tools/oracle/run-oracle.ps1 -Engine` | §5.1; guardia del legacy OK |
| 2 | `Unity -batchmode -quit -executeMethod RowMate.EditorTools.ProjectBootstrapper.ReapplyFromCommandLine` (dos veces) | §4.1 |
| 3 | `tools/unity/run-unity-tests.ps1 -Platform EditMode` y `-Simulator` | 2103 / 2077 / 0 / 26 y 2276 / 2250 / 0 / 26 / 0 inconclusas |
| 4 | `tools/unity/run-unity-tests.ps1 -Platform PlayMode` y `-Simulator` | 270 / 270 / 0 / 0 y 334 / 334 / 0 / 0 |
| 5 | `tools/unity/capture-snapshots.ps1 -Set it4a\|it3\|it2`, una vez con `-Out <scratch>` y otra a la carpeta del conjunto | §7 |
| 6 | `tools/build/build.ps1 -Target Windows -Simulator` (carpeta por defecto) | `Windows simulator Mono2x Development`, `Success` |
| 7 | `tools/dotnet/run-tests.ps1 -Matrix -UnityResults <scratch>/it4b/debt/unity-final -ArtifactsPath <scratch>/it4b/debt` | 0 / 0; 173 / 173; 2064 / 2042 / 0 / 22; matriz 534 / 0 / 1 / 0 / 0 |

Después de todas las corridas:

- `ProjectSettings/ProjectSettings.asset` no tiene `ROWMATE_SIMULATOR`;
- no queda la marca `Library/RowMate/simulator-tests.txt` ni `Assets/Resources`;
- de `ProjectSettings` cambió solo `RowMateBootstrap.json`: la hora y el detalle de `ui.fonts`.

### 6.2 Pruebas nuevas

| Suite | Nuevas |
|---|---|
| dotnet y Unity EditMode (+9) | `ItFourAFontTests` ×5; `WorkoutGoldenTests` ×4 |
| Unity EditMode (+1) | `FontAndIconAssetsTests.DerivedDigitsShareOneAdvance` |
| PlayMode, con y sin simulador (+6) | `HudLayoutTests.BandDividerKeepsItsWidth` y `.DerivedDigitWidthsOfTheDesignNotes`; `HudControlsTests.FirstTabWithoutFocusShowsTheRing` y `.FirstTabWithoutFocusShowsTheRingAtRest`; `RoutineEditorTests.FirstTabWithoutFocusShowsTheRingInTheEditor`; `ImmersiveHudTests.DigitsLeavingThePanelStopRolling`. Además vuelve a correr `ScalesOfTheDesignNotes` |

En la matriz, AC-WKT-17-04 cuenta `BandDividerKeepsItsWidth`, y AC-APP-05-07 las tres de `FirstTabWithoutFocus…`.

### 6.3 Cada prueba falla sin su arreglo

Se sacaron a propósito los tres arreglos de Presentation:

- `FocusHighlight` volvió a escuchar en `#app-root`;
- `TabularNumber` volvió a ser el de la 4a;
- el divisor quedó sin `flex-shrink: 0`.

Con eso, `run-unity-tests.ps1 -Platform PlayMode -Filter "BandDividerKeepsItsWidth|FirstTabWithoutFocusShowsTheRing|DigitsLeavingThePanelStopRolling"` dio **5 de 5 que fallan**, cada una por su motivo (§1.2, §2.2 y §3.2). Después se restauraron los archivos, iguales a los de antes (`cmp`).

---

## 7. Capturas: una sola regeneración de `it2`, `it3` e `it4a`

| Conjunto | Tomas | Corrida A (`<scratch>`) contra corrida B (repo) | Archivos distintos de `HEAD` |
|---|---|---|---|
| `it4a` | 49 (46 PNG y 3 MP4), 84 sondas | Iguales en 50 de 51; en `manifest.json`, solo `durationMs` | 51 de 51. `INDEX.md` suma las dos sondas del divisor |
| `it3` | 47 | Iguales en 48 de 49; en `manifest.json`, solo `durationMs` | 41 de 49. No cambian CAP-14, -16, -17, -20a, -21, -22, DEMO-03 e `INDEX.md`, que no tienen cifras ni pares nuevos |
| `it2` | 41 | Iguales en 43 de 44; en `manifest.json`, solo `durationMs` | 32 de 44. No cambian CAP-10, -11, -12a, -13, -14a a -14d y -15 (las tarjetas del pulsómetro y la del remo buscando), CAP-23, `INDEX.md` y `rt1-measurement.json` |

- **Lo que se ve cambiar** es lo que esperaba el Designer (04b/00-deuda-diseno §2.3):
  - toda cifra de `TabularNumber`: el HUD, el tablero de Dispositivo y el `×N`, entre un 3,5 y un 4,6 % más angostas y con el `1` tabular;
  - el panel del simulador, con las cifras de la derivada;
  - el divisor de la banda, ahora a la vista;
  - los pares nuevos de Inter 700.
- **Se miraron** CAP-15, CAP-19, DEMO-03 de `it4a` y CAP-01 de `it2`.
- `DEMO-06` sigue igual byte a byte a `DEMO-01`.
- Una primera corrida A de `it4a`, hecha antes de ajustar CAP-15, se descartó: quedó en `<scratch>` y no tocó el repo.

---

## 8. Prueba en la PC (build de simulador)

- **El build.** `tools/build/build.ps1 -Target Windows -Simulator` dejó el código de esta deuda en `Builds/Windows/simulator`, la carpeta por defecto, que ya tiene su regla del Firewall. No había ningún `RowMate.exe` abierto. **Ese build es ahora más nuevo que el de la demo de la 4a**: los pasos de REPORT §3.1 valen igual, con las cifras nuevas y el divisor a la vista.
- **Cómo se manejó.** Con las ayudas Win32 de QA de la 4a, adaptadas en `<scratch>/it4b/debt/dpi-run.ps1` y `ring-run.ps1`:
  - un clic solo si la ventana del player está adelante y el punto es suyo, y el puntero vuelve a su lugar;
  - las teclas, con su *scan code*;
  - el cierre, solo al PID lanzado por el script.
- **Los datos y el registro.**
  - `ROWMATE_DATA_DIR` apuntaba a `<scratch>/it4b/debt/devdata-*`, y el registro del player iba a `<scratch>`.
  - Antes se exportó `HKCU\Software\iguisoft\RowMate`, donde el player guarda la ventana. Al terminar se borró y se importó lo exportado: la exportación posterior es igual byte a byte a la de antes.
  - `rower_app_dev.db` del usuario no cambió: sigue con la fecha de antes de este trabajo.
- **Lo que se hizo:**
  1. las tres corridas de §3.2: el build de la 4a y el nuevo;
  2. la de §2.3: el anillo.

  En total fueron unos 2 minutos de ventana en la pantalla.

---

## 9. Desvíos y lo que queda

### 9.1 Desvíos (sin cambiar el QUÉ)

| # | Qué | Por qué |
|---|---|---|
| A-01 | `BandDividerKeepsItsWidth` mide el divisor "1 dp al píxel" (± un píxel físico, nunca menos de uno), no 1,000 exacto | UI Toolkit ubica cada borde en un píxel: con densidad 2,625 el divisor mide 2 px (0,762 dp base). Así se dibuja en el teléfono, y la sonda de color da `#263241` exacto |
| A-02 | CAP-15: la banda se comprueba con la esquina y el alto de §3.8 y el ancho de `HudGeometry`, no con el rectángulo 14,8-242,5 | Ese rectángulo es el de `00:42 │ 1048 m` (4 cifras), y la toma lleva `162 m` del simulador. El ancho con 4 cifras ya lo cubren `ScalesOfTheDesignNotes`, `ViewMatchesTheGeometry` y `DerivedDigitWidthsOfTheDesignNotes` |
| A-03 | Las comprobaciones de rectángulos siguen en ± 1 (el Designer pedía ± 0,1 para la banda y Split en 04b/00-deuda-diseno §2.2) | Con 2,625 px por dp y la escala de la zona, un píxel de layout son 0,4 dp: Split da 758,37-899,43 contra 758,3-899,2. Es la tolerancia de todo el arnés |
| A-04 | El *bootstrapper* sigue en la versión 3; se aplicó con `ReapplyFromCommandLine` | Como la It. 3 (B2). Las `.asset` nuevas se versionan, y en otra PC llegan por git. El paso es idempotente (§4.1) |

### 9.2 Lo que queda, con dueño

- **Para la 4b**, en el diseño técnico: el "…" en blanco de la pastilla recortada (DD4b-03; K2 no lo arregla, faltan 1,98 dp), el desplazamiento de kcal cuando aparece BPM (04a/04-design-notes §17.6), la salida con la última foto (O-01), la espera del diálogo de completado en el motor (DT4a-15) y el presupuesto de elementos (R4a-03).
- **Designer**: con la integración hecha, ASSET_SLOTS §3.7 puede dejar la rama "si Barlow no trae `tnum`" (00-deuda-diseno de la 4a, DD4a-07).
- **QA**: verificar las dos condiciones (AC-WKT-17-04 y AC-APP-05-07) y la regeneración, como pide la 4a.
- **Para la 10a**: QA-V4a-L01 sigue con el PO, y el guion `dist100_every_2s` queda listo para medir el arreglo.

---

## 10. Archivos

| Tipo | Archivos |
|---|---|
| Código | `Assets/RowMate/Presentation/Workout/HudBand.cs`, `Presentation/Styles/Base.uss`, `Presentation/Controls/FocusHighlight.cs`, `Presentation/Controls/TabularNumber.cs`, `Core/Design/FontAssetPaths.cs`, `Core/Design/KerningList.cs` (nuevo), `Core/Design/Tokens.cs` y `FontFamilyRole.cs` (comentarios), `Editor/Bootstrap/FontAssetBuilder.cs`, `Snapshots/WorkoutCatalog.cs` |
| Generados | `Presentation/Styles/Typography.uss`; `UI/Fonts/Generated/`: Inter ×4 modificados, `RowMateNumericSC-{SemiBold,Bold,ExtraBold}-SDF.asset` nuevos y los tres `BarlowSemiCondensed-*-SDF.asset` borrados, con sus `.meta`; `ProjectSettings/RowMateBootstrap.json`; `Tests/Golden/oracle/workout_engine.json` |
| Pruebas | `Tests/EditMode/Design/ItFourAFontTests.cs` (nuevo) y `TypographyUssSyncTests.cs`, `Tests/EditMode/Workout/WorkoutGoldenTests.cs`, `Tests/UnityEditMode/FontAndIconAssetsTests.cs`, `Tests/PlayMode/{HudLayoutTests,HudControlsTests,ImmersiveHudTests,RoutineEditorTests}.cs` |
| Herramientas | `tools/oracle/engine/test/engine_vectors_test.dart`, `tools/oracle/README.md` |
| Capturas | `docs/migration/iterations/{02-dispositivo-simulador,03-rutinas,04a-motor-hud-funcional}/snapshots/` |
| Este documento | `docs/migration/iterations/04b-hud-exr/00-deuda-4a.md` |

Los `.meta` nuevos (`KerningList.cs`, `ItFourAFontTests.cs` y los tres `FontAsset`) los creó el Editor y se versionan con su archivo.

---

## 11. Verificación hecha

1. **Legacy.** `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; la guardia de `run-oracle.ps1`, en "sin cambios". No se leyó el módulo de credenciales de Strava.
2. **Repo público.** La búsqueda de rutas de la PC (rutas de usuario de Windows y de Git Bash, y el prefijo de las carpetas de trabajo de Claude) sobre lo versionado y lo nuevo no da ninguna coincidencia en los archivos de esta deuda. Este documento cita el repo con rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy), el scratch como `<scratch>` y el SDK de Flutter como `<flutter-sdk>`.
3. **Unity**, una instancia por vez. Solo estaba el `unity.exe` de la Hub, que no se tocó.
4. **El trabajo en paralelo.** Los archivos del Designer y del PO que cambiaron mientras tanto no se tocaron: las biblias, `04a/00-deuda-diseno.md`, `04b/00-deuda-diseno.md` y `04b/01-po-spec.md`.
5. **Sin commits.**
