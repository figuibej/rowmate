# It. 4b — Verificación QA (principal)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa (verificación principal; el único agente de la verificación que usó Unity) |
| Fecha | 2026-10-29 |
| Iteración | 4b · `04b-hud-exr` |
| Entrada | [01-po-spec](01-po-spec.md) (con §16), [02-tech-design](02-tech-design.md) (con "Cambios por implementación de WP-A / WP-B"), [04-design-notes](04-design-notes.md) (con §15, WP-D2), [00-deuda-4a](00-deuda-4a.md), [00-deuda-diseno](00-deuda-diseno.md), las capturas de [snapshots/](snapshots/INDEX.md) y `docs/image-03.png` de [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) en `3ae0f3a`, solo lectura. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó el módulo de credenciales de Strava |
| Artefactos | `<scratch>/it4b/qa-main-r1/` (resultados de dotnet y de Unity, registros, capturas A, regeneraciones de it2 / it3 / it4a, fotos y registros del build) |
| Lentes paralelos | [05-qa-lens-motion](05-qa-lens-motion.md) y [05-qa-lens-paridad](05-qa-lens-paridad.md) (sin Unity). Lo que esta verificación pudo contrastar de ellos está en §8 |
| Ronda 2 | 2026-10-30, sobre "Cambios por verificación (ronda 1)" de [02-tech-design](02-tech-design.md): sección [Ronda 2](#ronda-2) al final. Veredicto de la ronda 2: **APROBADO CON OBSERVACIONES** (0 FAIL) |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** 0 bloqueantes, 0 mayores y **7 menores** (§7).

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix`) | Compilación 0 avisos, 0 errores · herramientas **182 / 182** · Core **2306: 2279 pasan, 0 fallan, 27 ignoradas** |
| Unity EditMode | **2355: 2324 pasan, 0 fallan, 31 ignoradas**, 0 inconclusas |
| Unity EditMode con simulador | **2537: 2506 pasan, 0 fallan, 31 ignoradas, 0 inconclusas**; el *define* restaurado |
| Unity PlayMode | **346: 346 pasan, 0 fallan, 0 ignoradas** |
| Unity PlayMode con simulador | **410: 410 pasan, 0 fallan, 0 ignoradas**; el *define* restaurado |
| Matriz (`-UnityResults` con los 4 XML) | 597 IDs: **PASA 595, FALLA 0, IGNORADO 2** (AC-APP-15-03 [Apple] y AC-WKT-02-10 de la 4a [Android]), PENDIENTE UNITY 0, SIN TEST 0. De los 101 AC de la 4b y LT-46: 101 PASA y 1 IGNORADO |
| Arnés `it4b` | 40 escenarios = **51 tomas, 0 con problemas**, `HudOverlap` y `WrenchAudit` en verde; **1 sonda de aspecto fuera** (CAP-22, H-D2-01 del Designer). Corrida A (`<scratch>`) y B (repo) **iguales byte a byte** en los 53 archivos salvo `durationMs`; B igual a lo que había en el repo |
| `it2` / `it3` / `it4a` | Iguales byte a byte a lo versionado en la regeneración única (salvo `durationMs`); las diferencias con `HEAD` son las de 00-deuda-4a §7 (§2.4) |
| Build de Windows con simulador | `Windows simulator Mono2x Development`, bien, en `Builds/Windows/simulator` |
| Equipo (build de simulador) | Rutina hasta el final, terminar a mano, libre, entrada y salida, minimizada con la rutina que termina y el proceso matado: **bien**; 2 desvíos menores ya conocidos (§5) |
| **Criterios** (101) | **96 PASS · 2 FAIL (menores) · 3 NO VERIFICABLE** |
| Condiciones de la 4a | **AC-WKT-17-04 PASS** y **AC-APP-05-07 PASS** (§4) |
| Repo público | La búsqueda de rutas de la PC con `git grep -nIE` (rutas de usuario de Windows y de Git Bash, y el prefijo de las carpetas de trabajo de Claude), sobre lo versionado y lo nuevo (`--untracked`): **0** |

Los dos FAIL son menores y de presentación: la llave del simulador queda a la vista durante la salida desde el diálogo de completado (AC-BLE-16-02, QA-V4b-01) y el *tracking* del chip de fase (y de `PAUSA`) es una novena parte del pedido (AC-WKT-16-03, QA-V4b-02). Los NO VERIFICABLE son Android (AC-APP-15-01), Apple (AC-APP-15-03) y la suspensión real del equipo, que verifica el usuario (AC-WKT-02-11).

---

## 1. Corridas

Todas desde la raíz del repo, una instancia de Unity por vez (solo estaba el `unity.exe serve` de la Hub, que no se tocó). Artefactos en `<scratch>/it4b/qa-main-r1/`.

| # | Comando | Resultado |
|---|---|---|
| 1 | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it4b/qa-main-r1` | `Build succeeded. 0 Warning(s) 0 Error(s)`; `Total tests: 182 Passed: 182`; `Total tests: 2306 Passed: 2279 Skipped: 27`; matriz sin Unity: PASA 478, FALLA 0, IGNORADO 2, PENDIENTE UNITY 117, SIN TEST 0 |
| 2 | `tools/unity/run-unity-tests.ps1 -Platform EditMode` | `total 2355, pasan 2324, fallan 0, ignorados 31, inconclusos 0`; Unity sale con 0 |
| 3 | `… -Platform EditMode -Simulator` | `total 2537, pasan 2506, fallan 0, ignorados 31, inconclusos 0`; las tres corridas con 0 |
| 4 | `… -Platform PlayMode` | `total 346, pasan 346, fallan 0, ignorados 0, inconclusos 0` |
| 5 | `… -Platform PlayMode -Simulator` | `total 410, pasan 410, fallan 0, ignorados 0, inconclusos 0` |
| 6 | `run-tests.ps1 -UnityResults <scratch>/…/unity` | Matriz: PASA 595, FALLA 0, IGNORADO 2, PENDIENTE UNITY 0, SIN TEST 0 (597 IDs) |
| 7 | `tools/unity/capture-snapshots.ps1 -Set it4b -Out <scratch>/…/capA` (A) y sin `-Out` (B, al repo) | Las dos: `listo: 51 tomas, 0 con problemas, 1 sondas de aspecto que fallan`; atlas `0 glifos antes, 1484 después` |
| 8 | `capture-snapshots.ps1 -Set it2`, `-Set it3`, `-Set it4a`, cada uno con `-Out <scratch>` | 41 / 47 / 49 tomas, 0 con problemas, 0 sondas que fallan |
| 9 | `Unity -batchmode -quit -executeMethod RowMate.Snapshots.SnapshotCli.SeedDatabase -snapshotSeedDb <scratch>/…/seed-f4a.db -seed F4A` | Bien. (Un primer intento con el nombre `rower_app_dev.db` lo frenó la guardia de `TempDatabase`, como corresponde) |
| 10 | `tools/build/build.ps1 -Target Windows -Simulator -Backend Mono` | `Build Finished, Result: Success`; no había ningún `RowMate.exe` abierto |
| 11 | Dos pruebas PlayMode **temporales** de QA (§6.3): `-Platform PlayMode -Filter QaTemp…` | 1 / 1 y 2 / 2 pasan. Los archivos y sus `.meta` se borraron después; `git status` quedó con las mismas 405 entradas que al empezar |

**Diferencias con lo que reportó el Architect** (2329 / 2511 en EditMode): +26 en las dos, que son las pruebas de contraste del Designer de WP-D2 (`ItFourBDesignerPairs`, 25 casos, e `ItFourBControlValuesMatchTheDesignNotes`). PlayMode coincide (346 y 410).

**Un falso inconcluso, explicado.** La corrida 6 dio `Passed: 2278` (una menos) porque corrió mientras el arnés tenía puesto el *define* del simulador: `SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator` ve la marca sin la variable de la corrida y queda sin ejecutar, como está diseñado. Es la única diferencia entre los dos `.trx` (comparados prueba por prueba). Vale la corrida 1.

Después de todas las corridas: `ProjectSettings/ProjectSettings.asset` sin `ROWMATE_SIMULATOR`, sin la marca `Library/RowMate/simulator-tests.txt`, y los `FontAsset` de `UI/Fonts/Generated` con los mismos md5 que dejó 00-deuda-4a (no se volvieron a escribir).

---

## 2. Capturas y videos

### 2.1 Reglas del arnés

| Regla | Cómo se verificó | Resultado |
|---|---|---|
| UI real en *batchmode*, reloj de prueba con la sesión a los `13:00Z` y zona UTC−03:00 | Toda toma del primer minuto muestra `10:00`; CAP-05 `10:12` (725,6 s), CAP-22 `10:50` (3016,6 s), DEMO-07 `10:01`, DEMO-10 `10:03` | Bien |
| Simulador sin ruido, base temporal | `R-4` exacto en las cifras (`1000 + 4k` m, kcal `40 + ⌊k/10⌋`); DEMO con 150 W y 24 spm → `02:13` | Bien |
| Toma en el instante exacto, sin esperar | `SnapshotTakeRule`: en el *manifest* `inFlight` solo lista lo continuo y rodillos pasada su mitad; ninguna toma falló | Bien |
| Pulsos en su fase fija | CAP-02 / -25 / -29 a los 47,6 (`p` = 1): halo al máximo; sus sondas del halo pasan | Bien |
| `HudOverlap` y `WrenchAudit` con las piezas nuevas | `WrenchAudit.HudPieces` incluye `hud-topbar` (con píldoras, reloj, chip y sello), `hud-big-countdown` y `hud-paused-label`; CAP-07 a -11 con sus comprobaciones del diálogo (12/12 en CAP-07) | Bien |
| Sondas de aspecto con los valores de DN4b-11 | 148 de aspecto + 2 comprobaciones (divisor de CAP-13 y "…" de CAP-31) | 1 fuera: CAP-22 (QA-V4b-03) |
| Videos a 30 fps con los valores por cuadro y la comparación con las líneas de tiempo puras (± 0,01) | `manifest.json`: VID-01 (67 valores, `EntryCheck` por cuadro), VID-03, -04 (555), -05 (81), -06, -07 y -08 con valores; VID-01, -04 y -05 con `FrameCheck` y `passed: true` | Bien, salvo VID-02 sin valores y VID-07 con `NaN` (QA-V4b-07) |
| Dos corridas iguales | A contra B: 53 de 53 archivos iguales salvo `durationMs` del *manifest*; B contra lo que había en el repo (la corrida de WP-D2): igual | Bien |

### 2.2 Cada toma, mirada (Read de cada PNG; cuadros extraídos de los videos con PyAV)

| Toma | Qué se vio | Contra la spec / las notas |
|---|---|---|
| CAP-01 | 5 píldoras iguales: 0 sólida rosa, 1 hueca con borde, 2 sólida verde, 3 en curso con relleno ≈ 0,51 y sin marco, 4 tenue; `Trabajo`, `18 spm` entera, `Próximo: Descanso`, `0:10`, `⏱ 10:00` pegados a la derecha; `obj. 18`; la cinta sobre el comienzo | Bien. Comparable con `docs/image-03.png` (fila de píldoras arriba, tipo a la izquierda, cuenta y reloj a la derecha) |
| CAP-01 con deuteranopía y protanopía (simulación Machado sobre la fila, `<scratch>`) | Sólida, hueca (por el borde), en curso (relleno + pista) y tenue se distinguen por forma | AC-WKT-15-06 bien |
| CAP-02 / CAP-25 / CAP-29 | Halo de la 3 solo por fuera, cuenta grande `3` con su aro en el centro alto, `0:03` agrandada y teñida, `Próximo: Descanso`; en TH y L640 entra sin tapar tarjetas | Bien |
| CAP-03 / CAP-04 / CAP-26 / CAP-30 | Sello en la tercera fila: `Objetivo cumplido · 24.0 / 18 spm` (verde) y `No alcanzado · 24.0 / 30 spm` (ámbar); la 1 hueca con el borde entero; en TH y L640, en una línea | Bien |
| CAP-05 | 14 píldoras iguales, la 2 (250 m) con la pista sola, `250m`, `Objetivo cumplido · 150 / 150 W · 02:05 / 02:05/500m` entero | Bien |
| CAP-06 | La última llena con su brillo, sin borde; `Descanso` `0:00`; controles al 0,38; sin `Próximo`, halo, cuenta ni sello | Bien |
| CAP-07 a CAP-11 | Diálogo: check, `¡Entrenamiento` / `completado!` en dos líneas junto al check (TV), mensaje, `00:30` · `1120 m` · `43`, un botón; detrás la barra de CAP-06 y los valores en vivo (banda `1128 m`); cinta y llave sobre el velo; en inglés, TH, L640 × 1,3 (se desplaza con `Terminar` a la vista) y W1280 | Bien. En L640 × 1,3 el contenido se corta a mitad de una fila (es el *viewport* del desplazamiento, lo permite el AC); sin indicio visual de que hay más (observación para el Designer) |
| CAP-12 | La 1 (100 m, última) con la pista sola, `100m`, sin sello | Bien |
| CAP-13 / -16 / -17 / -18 | Barra libre: ícono, `Entrenamiento Libre` entero (achicado), `⏱ 10:00`, `EN MARCHA` con punto; en inglés `Free Workout` a su tamaño; el divisor de la banda a la vista | Bien, salvo el *tracking* del chip (QA-V4b-02) |
| CAP-14 / CAP-27 | Velo, `PAUSA` en el disco, chip `PAUSADO`, `Reanudar` verde, tarjetas al 100 %; en CAP-27 sin cuenta grande, con `0:03` teñida y el halo | Bien. En CAP-27 se ve el píxel suelto en el relleno de la 3 (QA-V4b-04) |
| CAP-15 | `FINALIZADO` en `#FF7A92`, controles al 0,38 con el indicador de `Terminar`, sin diálogo | Bien |
| CAP-19 / CAP-20 / CAP-31 | La fila más larga sin pastilla (`Próximo: Enfriamiento` / `Up next: Cooldown`); a los 2,2 s `>150W · 24 spm…` con el `…` blanco | Bien |
| CAP-21 / CAP-23 / CAP-24 | 6 píldoras, la pastilla entera, `▲` en SPM y Watts fuera; cuenta y reloj pegados a la derecha | Bien |
| CAP-22 | 200 tramos en la franja, el pulgar en el 100 más alto; `Trabajo`, `Próximo: Descanso`, `0:14`, `10:50` | Bien de lejos; la pista del pulgar deja ver la franja (QA-V4b-03) |
| CAP-28 | Workout en reposo con `No se pudo guardar la sesión`, sin diálogo encima | Bien |
| DEMO-01 a DEMO-12 | La secuencia de §7.4: libre con `10:00` y `EN MARCHA`; pausa; reposo (DEMO-01, -04 y -12 iguales byte a byte); `Prueba 4a` con la amarilla al 60,7 %; cuenta `3` y halo; `No alcanzado · 150 / 160 W · 24.0 / 22 spm`; el panel abierto con Split y Watts a la vista; `Objetivo cumplido · 170 / 160 W · 26.0 / 24 spm`; cuenta `1`; diálogo `04:00` · `957 m` · `58` con la 1 hueca, la 3 sólida y la 5 llena | Bien |
| VID-01 / VID-03 / VID-08 | Workout a negro, el fondo, la cascada de arriba abajo (barra, SPM, Split y Watts, banda, kcal, controles), la banda pasa a `00:01` durante la cascada, la llave al final; con reducir movimiento, un fundido lineal | Bien. VID-08: los primeros 0,5 s muestran `0.0`, `--:--` y `0` hasta la primera lectura (conducta de la 4a, ya anotada por el Designer) |
| VID-02 | La salida con `00:12` y `EN MARCHA` → `FINALIZADO`, nunca `00:00` ni `—`; la llave sale en el cuadro 0 y vuelve en Workout; después de la salida los cuadros quedan quietos (solo el ruido del cuadro clave del códec en el 60) | Bien |
| VID-04 | Halo desde `0:05`, cuenta grande `3` / `2` / `1` en el mismo cuadro que la barra; a los 10 s la 0 se solidifica con el destello, la barra crece para el sello, la 1 se llena; a los 20 s el borde de la 1 se dibuja y entra `No alcanzado` | Bien |
| VID-05 | Cuenta grande del último paso; a los 30 s la última se llena; a los 30,3 el diálogo con el check que se dibuja y los totales que cuentan hasta el valor exacto; `Terminar` a los 33 s | Bien, salvo **la llave sobre el negro de toda la salida** (cuadros 210 a 239: QA-V4b-01) |
| VID-06 | Velo en 300, `PAUSA` en el disco, chip en el acto, reloj de la banda quieto, valores en vivo | Bien |
| VID-07 | BPM funde en 0-120 y kcal baja 71,4 dp en 120-360; a los 3 s, al revés | Bien |

### 2.3 `it4a`: las tomas reemplazadas

`-Set it4a` sacó CAP-01 a CAP-11 iguales byte a byte a las del repo y no volvió a tomar las del HUD: el `manifest.json` tiene 38 entradas con `"superseded": "it4b"` y sus archivos de la 4a quedan como registro (DT4b-07).

### 2.4 `it2` e `it3` no cambiaron sin motivo

- **Contra lo versionado de esta iteración** (la regeneración única de 00-deuda-4a §7): la corrida de QA a `<scratch>` dio **los mismos bytes** en los 44 archivos de `it2` y los 49 de `it3` (en `manifest.json`, solo `durationMs`). El código de la 4b no las tocó.
- **Contra `HEAD`** (el cierre de la 4a): cambian 32 de 44 (`it2`) y 41 de 49 (`it3`), los mismos números de 00-deuda-4a §7. Se compararon con `git lfs smudge`:
  - `it2` CAP-01: las cifras de la derivada (`02:13`, `24.0`, `150`, `226m`) un poco más angostas y con el `1` tabular; en el *manifest*, `hrm-bpm` de 85 a 82 dp y `hrm-title` de 142,5 a 144,4 (los pares K2 de Inter 700). Es QA-V4a-06 (WP-B0);
  - `it3` CAP-04: solo cambia la franja de la barra de pestañas (y 2352-2384), el kerning de Inter.
- No se encontró ninguna diferencia sin explicar.

---

## 3. Criterios de aceptación (101)

Marcas de evidencia: **d** dotnet · **u** Unity EditMode · **p** PlayMode · **ps** PlayMode con simulador · **cap** captura · **eq** el build en la PC · **qa** revisión de QA · **tmp** prueba temporal de QA (§6.3). El número entre paréntesis es la cantidad de resultados de la matriz que pasan.

### 3.1 F-WKT-15 — Píldora

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-15-01 | PASS | d p (5): `PIL.ColorIsTheBaseOfTheStepType`, `PRT.OnePillPerStepSameWidth`; CAP-01 |
| AC-WKT-15-02 | PASS | d p (8): LT-46; CAP-01 (sonda tenue) |
| AC-WKT-15-03 | PASS | d p (11); CAP-01 y CAP-06 (brillo, sin marco) |
| AC-WKT-15-04 | PASS | d p (8): LT-46 afirmación 2; CAP-01 |
| AC-WKT-15-05 | PASS | d p (11); CAP-01, CAP-04; rieles en `PRG` |
| AC-WKT-15-06 | PASS | d (3) `CVD`; qa: CAP-01 con deuteranopía y protanopía (§2.2) |

### 3.2 F-WKT-14 — Barra de pasos

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-14-07 | PASS | d p (5); CAP-01, CAP-24 |
| AC-WKT-14-08 | PASS | d p (11); CAP-01, CAP-05 (14 iguales), CAP-24 |
| AC-WKT-14-09 | PASS | d p (11); CAP-05, CAP-12 |
| AC-WKT-14-10 | PASS | d p (12): `PPG.*` con `InstantInTheSameStepNeverGoesBack`; VID-04 (`fill` sube 0,003 por cuadro, nunca baja) |
| AC-WKT-14-11 | PASS | d p (11); CAP-02, CAP-25; VID-04 (halo 0,30-0,70) |
| AC-WKT-14-12 | PASS | d p (12); VID-04 (destello, borde que se dibuja) |
| AC-WKT-14-13 | PASS | d p (5); CAP-06, CAP-12; eq: la última llena en las dos rutinas que terminaron en el build (§5). La lente de paridad propone un caso por *jitter* (QA-V4b-P02) que no se reprodujo (§8) |
| AC-WKT-14-14 | PASS | d p (5); CAP-01, CAP-24 |
| AC-WKT-14-15 | PASS | d p (16); CAP-01, CAP-19, CAP-20, CAP-31 |
| AC-WKT-14-16 | PASS | d p (20); CAP-22. Observación: la pista del pulgar deja ver la franja (QA-V4b-03); se siguen distinguiendo hechos, en curso y pendientes |
| AC-WKT-14-17 | PASS | p (2) `PRT.NoInteractionsNorTab` |
| AC-WKT-14-18 | PASS | d p (37); CAP-02, -25, -29; VID-04 y VID-05 (el mismo cuadro que la barra); eq: `3` a los 7,6 s y `2` a los 28,6 s de `Corta` |
| AC-WKT-14-19 | PASS | p (4) `HBT.LongSeriesStaysSmooth` (200 pasos ≤ 16 ms); tmp: **0 B en 20 cuadros** en cinco ventanas de `Serie larga` (halo + cuenta grande, y el sello con el pulgar pasando); eq: en el build, las ventanas de 10 s con cambios de paso, halo, sello y cuenta grande dieron máx 1,8-7,7 ms de CPU del hilo principal. `Serie larga` en el build: NO VERIFICABLE (la semilla del build solo trae `F-4A`). El primer cuadro de cada inmersivo tarda 35-69 ms (QA-V4b-06) |

### 3.3 F-WKT-19 — Reloj de pared

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-19-01 | PASS | d p (7); CAP-01, CAP-13; eq: `19:45` en 24 h con su ícono |
| AC-WKT-19-02 | PASS | d p (11) `WCL`, `FBT.WallClockChangesOnTheMinuteAndWithTheZone` |
| AC-WKT-19-03 | PASS | d p (9); eq: siguió en pausa, con la sesión terminada y con el diálogo (`19:45` → `19:46` con el diálogo abierto) |
| AC-WKT-19-04 | PASS | d p (16) (UTC−03:00 → UTC−05:00) |
| AC-WKT-19-05 | PASS | p (2); CAP-01, CAP-13 |

### 3.4 F-WKT-16 — Barra del libre y chip

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-16-02 | PASS | p (2) `FBT.ContentInOrderWithClockAndChip`; CAP-13, CAP-18 |
| AC-WKT-16-03 | **FAIL** (menor) | d p (21) pasan, y CAP-13 / -14 / -15 / -18 tienen texto y colores bien, pero el **tracking 0,8 no se aplica**: `PhaseChipView.cs:87` pone `letterSpacing = 0,08 × 11 = 0,88`, y en TextCore `N px` son N/100 em (ART_BIBLE §4.3; `Typography.uss:71-72`): queda en 0,0088 em. QA-V4b-02 |
| AC-WKT-16-04 | PASS | d p (5); CAP-15; eq: `FINISHED` hasta la salida, sin diálogo |
| AC-WKT-16-05 | PASS | d p (11); CAP-13 (el punto), CAP-14 (sin punto) |
| AC-WKT-16-06 | PASS | d p (5); CAP-13 (entero, achicado), CAP-16, -17, -18. Ojo: corregir QA-V4b-02 ensancha el chip y puede bajar la escala del título en TV |

### 3.5 F-WKT-21 — Diálogo de rutina completada

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-21-01 | PASS | d p (11) `CDT.AppearsThreeHundredMsAfterTheEndWithItsTotals`; CAP-07; VID-05 (entra en el cuadro 129, 30,3 s); eq |
| AC-WKT-21-02 | PASS | p (2) `CDT.ModalScrimEscapeDoNothing`; eq: un clic afuera y Escape no hicieron nada. "Atrás" de Android: NO VERIFICABLE (su `[Ignore]`) |
| AC-WKT-21-03 | PASS | d p (5); CAP-07, CAP-08; eq en inglés (`Workout Completed!`, `Time`, `Distance`, `Calories`, `Finish`) |
| AC-WKT-21-04 | PASS | d p (5); CAP-07 (`1120 m` fijo con la banda en `1128 m`); eq: `200 m` / `12` fijos mientras la banda pasaba de 207 a 222 m; tmp: los totales iguales después de rotar |
| AC-WKT-21-05 | PASS | d p (12); VID-05 (valores por cuadro: `check` 0,6 → 1,039 → 1, los tres totales terminan en `00:30`, `1120`, `43`) |
| AC-WKT-21-06 | PASS | d p (22); CAP-28; eq: `Finish` (con Enter) vuelve a Workout en reposo sin aviso |
| AC-WKT-21-07 | PASS | d p (8); eq: `Corta` terminada a mano a los 7 s, fotos a los 150 ms, 500 ms y 2 s: nunca el diálogo de completado |
| AC-WKT-21-08 | PASS | d p (14); eq: la sesión de `Corta` tiene 7 puntos y su fin en el segundo 30, aunque el diálogo quedó abierto unos 6 s con lecturas |
| AC-WKT-21-09 | PASS | d p (11); eq: ventana minimizada a los 8 s y restaurada a los 40 s: a los 120 ms ya está el diálogo, sin entrada, con los totales finales; la sesión con 7 puntos y fin a los 30 s; sin avisos |
| AC-WKT-21-10 | PASS | p (2); qa: un solo botón, ningún camino a Strava en `CompletionDialog` |
| AC-WKT-21-11 | PASS | p (2) `CDT.FirstTabWithoutFocusReachesTheDialog`; eq: después del mouse, el primer Tab enfoca `Finish` con el anillo, el segundo no sale del diálogo y Enter lo activa |
| AC-WKT-21-12 | PASS | p (2) `CDT.SizesAndLetter` (con `Maratón`); CAP-09, -10, -11 |
| AC-WKT-21-13 | PASS | d (6) `WorkoutGoldenTests` (los 15 guiones de 00-deuda-4a §5 y `CompletedHoldReset`) |

### 3.6 F-WKT-13 — Inmersivo

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-13-06 | PASS | d p (16); VID-01, -03, -08 (`EntryCheck` por cuadro, `passed`); eq: a los 450 ms la barra ya entró |
| AC-WKT-13-07 | PASS | p (4) `ITT.KeyboardDuringTheCascade`, `.BackFromTheFirstFrame`; "Atrás" de Android NO VERIFICABLE |
| AC-WKT-13-08 | PASS | d p (10); VID-02 (última foto, nunca `00:00` ni `—`, < 1 s) |
| AC-WKT-13-09 | PASS | p (2) `ITT.NothingAnimatesAfterTheExit`, `IHT.LeavingTheImmersiveStopsItsContinuousMotions`; VID-02 y VID-05 quietos después de la salida; eq: **0 avisos de DPI** en el registro del build después de cuatro entrenamientos (y 0 en la corrida de la ventana minimizada) |
| AC-WKT-13-10 | PASS | p (2) `WSC.StartDuringThePreviousExitHasItsOwnImmersive` |
| AC-WKT-13-11 | PASS | d p (5); tmp: al rotar con el diálogo abierto sigue el mismo diálogo, uno solo, entero en TH y otra vez en TV; eq: uno solo al restaurar la ventana |
| AC-WKT-13-12 | PASS | d p (13); CAP-14, CAP-27, VID-06; eq: `PAUSED` en el disco |
| AC-WKT-13-13 | PASS | d p (15); `HudOverlap` sin problemas en las 31 CAP |
| AC-WKT-13-14 | PASS | d p (18) `PIN.EngineSequenceIsTheSameWithAndWithoutTheHud`, `PPG.NeverPassesTheNextTick`; VID-04 y VID-05 cuadro por cuadro |

### 3.7 F-WKT-17, F-WKT-09, F-WKT-02, F-APP-15, F-APP-10

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-17-11 | PASS | d (3) `HTL.KcalSlide`; VID-07 (valores por cuadro y píxeles). Sin prueba PlayMode propia (QA-V4b-05) |
| AC-WKT-17-12 | PASS | d (3) `ItFourAFontTests` ×5; u `DerivedDigitsShareOneAdvance`; p `ScalesOfTheDesignNotes` (sin `[Ignore]`) y `DerivedDigitWidthsOfTheDesignNotes`; CAP-13; la regeneración de `it2` / `it3` / `it4a` explicada (§2.4) |
| AC-WKT-09-08 | PASS | d p (21); CAP-03, -04, -05, -26, -30, VID-04; eq: `Target met · 24.0 / 18 spm` a los 10,7 s de `Corta` |
| AC-WKT-09-09 | PASS | d p (17); CAP-03 a -05, DEMO-07, DEMO-09 |
| AC-WKT-02-11 | **NO VERIFICABLE** (equipo) | d p (87) pasan (`SUS.*`, `WSU.*` en los dos casos del monótono, `USC`, `SSL`); el build registra `[TIME] avisos de suspensión registrados (P-17)`. La suspensión real la verifica **el usuario** (demo (c)); QA no suspende el equipo |
| AC-APP-15-01 | **NO VERIFICABLE** (Android) | d p (10) la regla y los márgenes con el doble pasan; sin el módulo ni el teléfono |
| AC-APP-15-02 | PASS | p (2) `SBU.DesktopDoesNotTouchTheWindow`; eq: la ventana (posición, tamaño 1296 × 759, cliente 1280 × 720 y estilo) igual al abrir, en el inmersivo, en Workout y al restaurar |
| AC-APP-15-03 | **NO VERIFICABLE** ([Apple], 10b) | Su `[Ignore]` en WDC |
| AC-APP-15-04 | PASS | d p (14); eq: en el registro, `ocultas` / `visibles` en orden por cada entrada y salida, y otra vez `ocultas` al restaurar la ventana minimizada |
| AC-APP-10-11 | PASS | d (6); qa: 214 claves en es y en, las 12 nuevas con los textos de §11.2 |

### 3.8 §5.1 — Las dos condiciones de la 4a

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-17-04 | **PASS** | p (4) `HudLayoutTests.BandDividerKeepsItsWidth` en las dos suites; CAP-13 con la sonda del divisor (comprobación ± 2) dentro; el divisor a la vista en todas las tomas con banda de la 4b |
| AC-APP-05-07 | **PASS** | p (35): `FirstTabWithoutFocusShowsTheRing`, `…AtRest`, `…InTheEditor`, `CDT.FirstTabWithoutFocusReachesTheDialog`, `ITT.KeyboardDuringTheCascade`; eq: el primer Tab después del mouse, con anillo, en el HUD y en el diálogo |

### 3.9 §5.2 — Cambian por esta iteración

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-01-06 | PASS | d p (14); eq: a mano vuelve sola a reposo; sola, espera al diálogo |
| AC-WKT-02-04 | PASS | d (27) |
| AC-WKT-02-06 | PASS | d p (33) |
| AC-WKT-02-08 | PASS | d p (46); eq (ventana restaurada: diálogo sin entrada). Android NO VERIFICABLE |
| AC-WKT-02-09 | PASS | d p (6); eq: minimizada con la rutina que termina → el diálogo al restaurar, una vez, sin aviso |
| AC-WKT-03-05 | PASS | d p (18); CAP-07, DEMO-11; eq |
| AC-WKT-06-12 | PASS | d p (46); CAP-28 |
| AC-WKT-13-01 | PASS | p (6) |
| AC-WKT-13-04 | PASS | p (14); eq: nunca quedó el inmersivo en reposo |
| AC-WKT-14-01 | PASS | d p (7); CAP-01, CAP-03 (la barra crece, las tarjetas quietas) |
| AC-WKT-14-06 | PASS | p (6); CAP-06, CAP-27 |
| AC-WKT-16-01 | PASS | p (4); CAP-13 |
| AC-WKT-20-07 | PASS | p (7) `HCT.RoutineEndsWithTheFinishDialogOpen`, `.FinishTapAfterTheEndDoesNothing` |
| AC-APP-03-09 | PASS | p (4); VID-01, VID-02 |
| AC-APP-03-10 | PASS | p (6); CAP-07 |
| AC-APP-03-11 | PASS | d p (9); CAP-28; eq: sin avisos con el diálogo abierto ni al volver |
| AC-APP-05-05 | PASS | p (74); CAP-10 |
| AC-APP-05-08 | PASS | d p (39) `HTL.ReducedVariants`; VID-08. La lente de motion encontró que el punto en vivo no se entera de un cambio de "reducir movimiento" a mitad de sesión (QA-V4b-MOT-03, §8) |
| AC-BLE-16-01 | PASS | ps (3); CAP-01, CAP-07, CAP-13 (la cinta sobre la primera píldora y sobre el velo) |
| AC-BLE-16-02 | **FAIL** (menor) | s ps (64) y `WrenchAudit` pasan, pero en la salida desde el diálogo de completado **la llave no se va en el primer cuadro**: en VID-05 queda a la vista, sobre el negro, de los cuadros 210 a 239 (en VID-02, sin diálogo, sí sale en el cuadro 0). QA-V4b-01 (= H-D2-02 del Designer, sin corregir) |
| AC-BLE-16-03 | PASS | d s ps (47) `SPL.WorkoutCeilingFiveHeights`; DEMO-08 |

### 3.10 §5.3 — Se vuelven a verificar

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-13-02 | PASS | p (2) `IHT.Layers`; CAP-07 |
| AC-WKT-13-03 | PASS | p (4); eq: Escape con el diálogo de completado no hace nada |
| AC-WKT-13-05 | PASS | p (4) `SVT.NoSealWithoutTargetsNorOnTheLastStepAndNoRoutineName` |
| AC-WKT-14-02 | PASS | p (2); CAP-01 |
| AC-WKT-14-03 | PASS | d p (10); CAP-01, -21, -24, -31 |
| AC-WKT-14-04 | PASS | d p (7); CAP-01, -19, -20 |
| AC-WKT-14-05 | PASS | p (4); CAP-02 |
| AC-WKT-17-01 | PASS | p (196); CAP-13 |
| AC-WKT-17-02 | PASS | p (2) |
| AC-WKT-17-03 | PASS | p (2) |
| AC-WKT-17-05 | PASS | p (2); CAP-16 |
| AC-WKT-17-06 | PASS | p (4); CAP-07 y eq (valores en vivo detrás del velo) |
| AC-WKT-17-07 | PASS | p (2) `IHT.RotationKeepsTheSession` (de la 4a); tmp: al rotar con el diálogo, el sello o la cuenta grande a la vista, todo se reacomoda y nada se repite (`QaTempRotate…`, 2 / 2). La prueba prometida `HLT.RotationWithDialogSealOrBigCount` no existe (QA-V4b-05) |
| AC-WKT-17-08 | PASS | d p (615); `HudOverlap` |
| AC-WKT-17-09 | PASS | d p (10); CAP-29 |
| AC-WKT-17-10 | PASS | p (4); `CDT.SizesAndLetter` con `Maratón` y `R-4X`; CAP-10 |
| AC-HIS-20-04 | PASS | d (50); qa: la base del usuario (`rower_app_dev.db` en la carpeta de datos de la app) con la misma fecha y el mismo md5 antes y después; todas las corridas del build usaron `ROWMATE_DATA_DIR` en `<scratch>`; las preferencias de la ventana del player (registro de Windows) se exportaron antes y se restauraron (la exportación final es igual byte a byte a la de antes) |

### 3.11 LT-46

| LT | Resultado | Evidencia |
|---|---|---|
| LT-46 `pendiente, en curso, hecho y no cumplido se pintan distinto` | PASS | d p (5): `StepPillsTests` (las 12 afirmaciones, armadas con `LookOf`) y `PillRowTests.PendingActiveDoneAndMissedPaintDifferently` en las dos suites |

**Conteo:** 96 PASS · 2 FAIL (AC-WKT-16-03, AC-BLE-16-02) · 3 NO VERIFICABLE (AC-WKT-02-11, AC-APP-15-01, AC-APP-15-03).

---

## 4. La deuda de la 4a declarada resuelta (00-deuda-4a)

| ID | Qué dice 00-deuda-4a | Qué se verificó | Estado |
|---|---|---|---|
| QA-V4a-03 (condición) | `flex-shrink: 0` en el divisor, prueba en 4 perfiles y 5 casos, sonda de color | `BandDividerKeepsItsWidth` pasa en las dos suites; la sonda del divisor de CAP-13 dentro; se ve en CAP-13 y en toda toma con banda; en el *manifest* `hud-band-divider` mide 0,8 dp en TV | **Cerrado** |
| QA-V4a-04 (condición) | `FocusHighlight` en la raíz del panel; 3 PlayMode | Las 3 `FirstTabWithoutFocus…` pasan; en el build, el primer Tab después del mouse muestra el anillo en el HUD y en el diálogo | **Cerrado** |
| QA-V4a-05 | `TabularNumber` no mide fuera del panel | `DigitsLeavingThePanelStopRolling` pasa; 0 avisos de DPI en el registro del build tras cuatro entrenamientos | **Cerrado** |
| QA-V4a-06 (WP-B0) | Kerning K2 y RowMate Numeric SC | `ItFourAFontTests` ×5 pasan (dotnet), `DerivedDigitsShareOneAdvance` (Unity EM), `ScalesOfTheDesignNotes` sin `[Ignore]` y `DerivedDigitWidthsOfTheDesignNotes` (PM); los `FontAsset` sin reescribir; la regeneración de §2.4 | **Cerrado** |
| QA-V4a-L02 | 15 guiones del oráculo | `WorkoutGoldenTests` (`ScriptsAreIdenticalToTheLegacy`, `NegativeStepIsTheExpectedDifference`, `ScriptsComeFromTheLegacyEngine`, `MonitorEveryTwoSecondsMatchesUntilTheFirstStepChange`, `CompletedHoldReset`) pasan; la lente de paridad regeneró el oráculo desde el legacy y dio el mismo archivo | **Cerrado** |
| §7, la regeneración única | Dos corridas iguales de `it2`, `it3` e `it4a` | §2.3 y §2.4: lo versionado es igual a una tercera corrida de QA | **Cerrado** |
| §9.2, "para la 4b" | El "…" blanco, kcal que se desliza, la última foto, la espera del diálogo y el presupuesto | CAP-31, VID-07, VID-02, AC-WKT-21-08 y `HBT.AtMostSixtyVisibleElementsInTheFourBWorstCase` | **Hecho** |

---

## 5. Prueba en equipo (build de Windows con simulador)

Build de `Builds/Windows/simulator` (el de la corrida 10), 1280 × 720 en ventana, Windows en inglés, `ROWMATE_DATA_DIR` en `<scratch>` con la base de `F-4A` copiada como `rower_app_dev.db`, registro del player en `<scratch>`. Manejado con las ayudas Win32 de QA de la 4a (clic solo si la ventana es la de adelante y el punto es suyo; el puntero vuelve a su lugar; se cierra o se mata **solo el PID lanzado**). Unos 4 minutos de ventana en total.

| Paso | Qué se vio | Resultado |
|---|---|---|
| Libre: entrada | A los 450 ms la barra de arriba y el fondo; a los 2,5 s el HUD entero con `Free Workout`, `⏱ 19:45` (24 h) y `● ACTIVE` | Bien |
| Libre: pausa | Disco `PAUSED`, chip `PAUSED` ámbar, `Resume` verde, valores en vivo; `Resume` vuelve todo | Bien |
| Libre: teclado | `Finish` → `Cancel` con el mouse: nada enfocado; el primer Tab enfoca `Finish` con el anillo | Bien (AC-APP-05-07) |
| Libre: salida a mano | `FINISHED` en el chip hasta la salida; Workout en reposo, sin diálogo ni aviso | Bien |
| `Corta` hasta el final | Píldoras de `Corta`, `18 spm`, halo y `3` grande a los 7,6 s, `Target met · 24.0 / 18 spm` a los 10,7 s, la 0 sólida y la 1 verde a los 16,6 s, `2` grande a los 28,6 s, `Workout Completed!` a los 31 s con `00:30`, `200 m`, `12` | Bien |
| El diálogo | Clic afuera y Escape: nada; Tab → `Finish` con anillo; Tab otra vez: sigue en `Finish`; Enter → Workout sin aviso | Bien |
| `Corta` terminada a mano | Fotos a 150 ms, 500 ms y 2 s del `Finish` del diálogo de terminar: ningún diálogo de completado | Bien (AC-WKT-21-07, Q-53) |
| Minimizada | `Corta`, minimizada a los 8 s y restaurada a los 40 s: el diálogo ya abierto, sin entrada, con los totales finales; `Finish` → reposo sin aviso | Bien (AC-WKT-21-09, AC-WKT-02-09) |
| La ventana | Igual en todo momento (posición, tamaño, cliente y estilo) | Bien (AC-APP-15-02) |
| El registro | `[UI] barras del sistema: ocultas` / `visibles` por cada entrada y salida; `[TIME] avisos de suspensión registrados (P-17)`; **0 avisos de DPI**; ningún error ni excepción propios | Bien |
| Rendimiento | `[PERF]`: ventanas con cambios de paso, halo, sello y cuenta grande: máx 1,8-7,7 ms, p99 ≤ 1,5 ms. **La primera ventana de cada inmersivo**: máx 49,8 / 34,6 / 68,8 ms (el cuadro del toque que arma el HUD) | Observación QA-V4b-06 |
| Proceso matado | Libre, el proceso (solo su PID) matado a los 13,5 s: la base queda con **una sesión cortada** (sin fin) **con 2 puntos** (5 y 10 s), y se abre sin errores con `tools/dotnet/RowMate.SessionList --db` sobre una copia | Bien |

Sesiones que quedaron en las bases de `<scratch>` (`RowMate.SessionList --db` sobre copias): libre (2 puntos, cerrada), `Corta` sola (7 puntos, fin 31 s después del inicio), `Corta` a mano (2 puntos), libre (2 puntos); en la de la ventana minimizada, `Corta` con 7 puntos y fin a los 30 s del inicio; en la del proceso matado, una cortada con 2 puntos. La distancia del diálogo (`200 m`) es la del monitor al terminar y la banda sigue en vivo detrás (207 → 222 m): es lo que pide AC-WKT-21-04 y lo que hace el legacy (`w.data.distanceMeters`, `immersive_workout_screen.dart:984-987`). La nota "para QA" de WP-B (501 contra 455 m) es esa misma diferencia, no un desvío.

---

## 6. Revisión estática

### 6.1 Lo pedido

| Tema | Qué se miró | Resultado |
|---|---|---|
| Motion que no toca el dato | `Presentation/Motion/Hud/*` no referencia el motor, el *store* ni el programador; `HudMotion` lee `Driver.Now` y nunca lo mueve; los únicos `Schedule` de presentación son el diálogo a los 300 ms (`ImmersiveWorkoutPage.cs:271`) y el indicador de `Terminar` (`CompletionDialog.cs:226`), ninguno cambia datos. `PIN.EngineSequenceIsTheSameWithAndWithoutTheHud` pasa | Bien |
| Nada animando al salir | `HudMotion.Stop` en `Dispose` y `DetachFromPanelEvent` de cada vista; `BeginExit` corta la cascada, los diálogos y el temporizador del diálogo; `ITT.NothingAnimatesAfterTheExit`, `IHT.LeavingTheImmersiveStopsItsContinuousMotions` y `HMT.NoAllocationsPerFrameAndStopLeavesNothingAnimating` pasan; cuadros quietos en VID-02 y VID-05 | Bien (la lente de motion pide dos casos más en la prueba: QA-V4b-MOT-05) |
| Frame pacing en reposo | `FramePacer` / `FramePacingPolicy` sin cambios de la 4a; `WorkoutScreenTests` (`Pacing.Demands == 0` en reposo) e `IHT` / `ITT` (ninguna demanda de 60 fps de más después de la salida) pasan | Bien |
| Allocations por frame en el HUD | `HBT.NoAllocationsPerFrameWhileItOnlyBeats` (4a) pasa; la prueba con 200 píldoras que prometía §6.2 no existe, y la cubrió la prueba temporal de QA: 0 B (§6.3) | Bien, con el hueco de QA-V4b-05. La lente de motion mide asignaciones en el conteo del diálogo (QA-V4b-MOT-01) |
| Android (`AndroidSystemUi`) | `runOnUiThread` con `AndroidJavaRunnable`, API 30+ con `WindowInsetsController` y `BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE`, 26-29 con `IMMERSIVE_STICKY`; compila solo con `UNITY_ANDROID && !UNITY_EDITOR` | Se ve correcto; NO VERIFICABLE sin módulo ni teléfono |
| Escritorio (`DesktopSystemUi`) | Solo cuenta los pedidos; no toca `Screen` ni la ventana | Bien |
| Repo público | La búsqueda de rutas de la PC con `git grep -nIE` (rutas de usuario de Windows y de Git Bash, y el prefijo de las carpetas de trabajo de Claude) y lo mismo con `--untracked`: **0**, también después de escribir este documento; tampoco el nombre del equipo; `manifest.json` e `INDEX.md` de `it4b` sin rutas | **Limpio** |
| Legacy | `git -C ../rowmate-legacy status --porcelain` | Vacío |

### 6.2 Cobertura frente a 02-tech-design §6.2 y §6.4

De los 142 nombres de prueba que cita §6.4, **53 no existen** con ese nombre. La mayoría quedaron agrupados en pruebas más grandes con las categorías del AC (B-12 de "Cambios por implementación de WP-B" lo declara), y la matriz no tiene ningún AC sin prueba. Pero algunos comportamientos nuevos de la 4b no los prueba nadie en PlayMode: la rotación con el diálogo, el sello o la cuenta grande (`HLT.RotationWithDialogSealOrBigCount`), las asignaciones con 200 píldoras y la fila sola de 1998 (`HBT`), el cambio de paso, el sello y la cuenta grande en `LongSeriesStaysSmooth` (mide solo a los 26,2 s), kcal que se desliza montado (`IHT.KcalSlides`, `WSM.HeartRateDropSlidesKcal`), la ventana minimizada (`WBU.MinimizedEndShowsTheDialogOnRestore`) y la cinta sobre el diálogo (`SOT.RibbonAboveTheCompletionDialog`). QA cubrió lo principal con pruebas temporales y con el equipo (QA-V4b-05).

### 6.3 Pruebas temporales de QA (borradas al terminar)

| Prueba | Qué hizo | Resultado |
|---|---|---|
| `QaTempAllocProbeTests.QaTempSerieLargaBytesPerFrame` | `Serie larga` (200 píldoras): 10 cuadros de calentamiento y 20 medidos con `GC.GetAllocatedBytesForCurrentThread`, desde los 27,57 s (halo + cuenta grande), 28,57, 30,47 y 30,79 (sello, cambio de paso y pulgar que pasa) y 31,47 | **0 B en las cinco ventanas** |
| `QaTempRotationProbeTests.QaTempRotateWithTheCompletionDialog` | `Corta` a los 32 s, TV → TH → TV con el diálogo | El mismo diálogo, uno solo, entero en TH, totales `00:30 · 1120 · 43` iguales |
| `QaTempRotationProbeTests.QaTempRotateWithTheSealAndTheBigCount` | `Estados` a los 7,6 s (cuenta `3`) TV → TH, y a los 10,6 s (sello) TH → TV | La cuenta y el sello siguen, uno de cada uno, dentro de la ventana |

Se escribieron en `Assets/RowMate/Tests/PlayMode/`, se corrieron con `-Filter` y se borraron con sus `.meta` (el repo quedó con las mismas entradas de `git status` que al empezar).

---

## 7. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V4b-01 | menor | architect | **La llave del simulador queda a la vista durante toda la salida desde el diálogo de completado** (AC-BLE-16-02: "en la salida se va en el primer cuadro"). Con el diálogo abierto la llave está en el carril izquierdo; al tocar `Terminar` salta unos 4 dp a su lugar de Workout y queda sobre el negro hasta que entra Workout | VID-05, cuadros 210 a 239 (en VID-02, sin el diálogo, sale en el cuadro 0 y vuelve a los 640). Es H-D2-02 de 04-design-notes §15.5, sin corregir. Ninguna prueba mira esa salida | Que `BeginExit` saque la llave en el cuadro 0 también cuando la sostiene el diálogo (`ShellLayout.HoldWrench` / `SimulatorLayerView`) y la vuelva a mostrar al terminar la salida, como en VID-02. Sumar ese caso a `SOT` |
| QA-V4b-02 | menor | architect | **El *tracking* del chip de fase y de `PAUSA` es 1/9 del pedido** (AC-WKT-16-03: "texto de 11 con tracking 0,8"). Se escribe en dp, pero en TextCore `letter-spacing: N px` es N/100 em | `Presentation/Workout/PhaseChipView.cs:87` (`0,08 × 11 = 0,88` → 0,0088 em) y `Presentation/Workout/CenterCallouts.cs:67` (`0,08 × 12`); la regla del proyecto en ART_BIBLE §4.3 y `Presentation/Styles/Typography.uss:71-72` (`8px` = 0,08 em). En el *manifest*, el chip de CAP-13 mide 100,47 dp (95,3 base) contra 101,8 de 04-design-notes §3.5; la explicación de B-03 (que Unity mide distinto que fontTools) no es la causa. Coincide con QA-V4b-P01 de la lente de paridad | `letterSpacing = PhaseChipTrackingEm × 100` (8) y lo mismo en `PAUSA`. Después, volver a medir el ancho fijo del chip, la escala del título libre en TV (AC-WKT-16-06: con el chip más ancho puede bajar de 0,818 o recortarse) y `PAUSED` en el disco (2,7 dp por lado en las notas), y regenerar las tomas (dos corridas) |
| QA-V4b-03 | menor | architect | **La pista del pulgar deja ver la franja de abajo** (AC-WKT-14-16 se cumple: se distingue; pero 04-design-notes §3.2 pide "la pista, el relleno y el brillo de siempre") | `Presentation/Workout/PillRowView.cs:475-487` (`DrawThumb` pinta la cápsula al 30 % sobre la tira sin taparla); la sonda de CAP-22 `pulgar: pista step.work @30 %` da `#6F344E` contra `#4D253F` (diferencia 34) en las corridas A y B. Es H-D2-01 del Designer | Recortar la tira y las cápsulas vecinas en el rectángulo del pulgar (o pintar debajo un rectángulo con el vidrio) para que el 30 % quede sobre el vidrio; la sonda ya lo espera |
| QA-V4b-04 | menor | architect | **Un píxel suelto del color de la pista dentro del relleno** de la píldora en curso (grieta de la teselación) | CAP-27, píldora 3, en (275,8; 22,1) dp: se ve a simple vista como un punto oscuro. Es H-D2-03 | Revisar `PillShapes.FillPolygon` en el centro de la franja (vértice en T o triángulo degenerado) o cerrar con un rectángulo interior debajo del polígono |
| QA-V4b-05 | menor | architect | **Faltan pruebas PlayMode que prometía 02-tech-design §6.2 / §6.4** para comportamientos nuevos (53 de 142 nombres no existen; B-12 lo anuncia sin listarlos) | §6.2 de este documento: la rotación con el diálogo, el sello o la cuenta grande; 0 B con 200 píldoras y la fila de 1998; `LongSeriesStaysSmooth` sin cambio de paso, sello ni cuenta grande (`HudBudgetTests.cs:84-101`, con un comentario que promete "0 B por cuadro" que el cuerpo no mide); kcal montado; la ventana minimizada; la cinta sobre el diálogo | Sumar como permanentes las tres pruebas temporales de §6.3 y las de la lista, o anotar en el diseño cuáles se cubren con captura o equipo |
| QA-V4b-06 | menor | architect | **El primer cuadro del inmersivo tarda 35-69 ms de CPU** en el build (arma el HUD en el cuadro del toque): un tirón justo al empezar el fundido a negro de "sumergirse" | Registro del build: primera ventana `[PERF]` de cada inmersivo con máx 49,8, 34,6 y 68,8 ms (las siguientes, ≤ 7,7). WP-B vio lo mismo (≈ 50 ms) | Armar el HUD antes (al abrir Workout, o repartido en los primeros cuadros del negro, que no muestra el HUD), o al menos medir que el fundido no pierda cuadros |
| QA-V4b-07 | menor | architect | **Valores por cuadro del arnés incompletos**: VID-02 (la salida) no deja `values` (solo comprueba que no aparezca `00:00`) y VID-07 deja `kcalShift=NaN` en sus primeros 30 cuadros | `manifest.json` de `it4b`: VID-02 0 valores; VID-07 30 de 180 con `NaN`; `Snapshots/HudExrCatalog.cs:410-412` y `:468` | `FrameValues` para VID-02 con las opacidades de `ImmersiveExitMotion`, y `ShiftDp` en 0 antes del primer deslizamiento |

**Observaciones (sin issue):**

- O-01 (Designer): en CAP-10 (L640 × 1,3) el contenido del diálogo que se desplaza se corta a mitad de una fila sin ningún indicio de que hay más. Lo permite el AC; un degradé o una barra de desplazamiento ayudaría.
- O-02: `INDEX.md` de `it4b` marca CAP-22 como "ok" con la sonda `5/6 ⚠` (las sondas de aspecto no hacen fallar la toma, como está diseñado) y el reporte de WP-B dice "0 sondas de aspecto que fallan" porque es anterior a las sondas de WP-D2: no hay contradicción, pero conviene que el REPORT lo aclare.
- O-03: en VID-08 los primeros 0,5 s muestran `0.0`, `--:--` y `0` hasta la primera lectura del remo de prueba; es conducta de la 4a (en VID-01 lo tapa la cascada).

---

## 8. Hallazgos de los lentes paralelos que esta verificación pudo contrastar con Unity o el build

| Lente | ID | Qué dice | Contraste de QA principal |
|---|---|---|---|
| paridad | QA-V4b-P01 | El *tracking* del chip (y de `PAUSA`) es 1/9 | **Confirmado** de forma independiente (QA-V4b-02): el código contra la regla del proyecto y el ancho del chip en el *manifest* |
| paridad | QA-V4b-P02 | La última píldora puede quedar al 98,4-99,9 % por el *jitter* del cuadro | **No se reprodujo** en el build: en las dos rutinas que terminaron (una en primer plano y otra minimizada) la última quedó llena hasta el borde (píxel por píxel en la fila). No lo descarta: depende del cuadro |
| motion | QA-V4b-MOT-02 | Un toque durante los 400 ms en negro de la salida le llega a Workout | **Coherente con el código**: `PageStack.Pop` aplica `ExitLock` a la página que sale (`PageStack.cs:158`) y Workout, debajo, no se bloquea mientras se ve el negro. No se probó en el build |
| motion | QA-V4b-MOT-01, -03, -04, -05 | Asignaciones del conteo del diálogo, el punto en vivo con reducir movimiento a mitad de sesión, el registro de la vuelta del segundo plano, y casos de la prueba de salida | No se contrastaron con Unity. MOT-05 coincide en parte con QA-V4b-05 |

---

## 9. NO VERIFICABLE (y por qué)

| Qué | Motivo |
|---|---|
| AC-APP-15-01, las partes Android de AC-WKT-13-07, -21-02 y -02-08 | Sin el módulo de Android ni un teléfono (cada uno con su `[Ignore]` en `WorkoutDeviceChecksTests`) |
| AC-APP-15-03 | [Apple], It. 10b |
| AC-WKT-02-11 en equipo | Lo verifica el usuario en la demo (c): QA no suspende el equipo |
| AC-WKT-14-19 con `Serie larga` en el build | La semilla del build (`SnapshotCli.SeedDatabase -seed F4A`) no trae `F-4B`; se midió con `Corta` y, en PlayMode, con 200 y con la prueba temporal |
| IL2CPP y Android (builds) | Faltan los módulos en este Editor (como en las It. 2 a 4a) |

---

## 10. Verificación hecha

1. **Legacy, solo lectura.** Se leyó `lib/features/workout/immersive_workout_screen.dart:955-1019` (el resumen del diálogo) y se miró `docs/image-03.png`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó el módulo de credenciales de Strava.
2. **Unity**, una instancia por vez; solo se cerraron procesos lanzados por QA (el player, por su PID). El `unity.exe serve` de la Hub no se tocó.
3. **Bases del usuario intactas**: misma fecha y mismo md5 de `rower_app_dev.db` antes y después; las preferencias de la ventana del player, exportadas y restauradas (igual byte a byte). Todo dato del build y de las pruebas quedó en `<scratch>`.
4. **Repo**: sin commits. Lo único que QA escribió en el repo es este documento y la corrida B del arnés (igual a lo que había salvo `durationMs`). Las dos pruebas temporales se borraron con sus `.meta`.
5. **Repo público**: este documento cita el repo con rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) o `../rowmate-legacy` y el scratch como `<scratch>`; ninguna ruta de la PC ni datos del equipo.

---

## Ronda 2

| Campo | Valor |
|---|---|
| Fecha | 2026-10-30 |
| Entrada | [02-tech-design](02-tech-design.md) "Cambios por verificación (ronda 1)" (V1-01 a V1-13), [04-design-notes](04-design-notes.md) §16, las lentes ([05-qa-lens-motion](05-qa-lens-motion.md), [05-qa-lens-paridad](05-qa-lens-paridad.md)) y las capturas regeneradas por el Architect |
| Issues que se re-verifican | QA-V4b-01, -02, -03, -04, -05, -07, QA-V4b-P02, QA-V4b-MOT-01 a -05 (declarados corregidos) y QA-V4b-06 (declarado abierto) |
| Artefactos | `<scratch>/it4b/qa-main-r2/` (dotnet, los XML y registros de Unity, capturas A, regeneraciones de it2 / it3 / it4a, la prueba temporal, el build y la prueba en equipo) |
| Legacy | `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó el módulo de credenciales de Strava |

### R2.0 Veredicto

**APROBADO CON OBSERVACIONES.** 0 bloqueantes, 0 mayores, **4 menores** (R2.8): los 12 issues declarados corregidos están **cerrados** con evidencia; queda abierto QA-V4b-06 (diferido a la It. 5, como desvío) y aparecen tres menores nuevos, ninguno de comportamiento: el aire entre el título libre y el reloj en TV (Designer), el "≈ 0,82" que quedó en la spec (PO) y un cierre de Unity que a veces revienta en las herramientas (Architect).

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix`) | Compilación **0 avisos, 0 errores** · herramientas **182 / 182** · Core **2319: 2292 pasan, 0 fallan, 27 ignoradas** |
| Unity EditMode | **2368: 2337 pasan, 0 fallan, 31 ignoradas, 0 inconclusas** |
| Unity EditMode con simulador | **2550: 2519 pasan, 0 fallan, 31 ignoradas, 0 inconclusas** (dos corridas; R2.1); el *define* restaurado |
| Unity PlayMode | **354: 354 pasan, 0 fallan** |
| Unity PlayMode con simulador | **420: 420 pasan, 0 fallan**; el *define* restaurado |
| Matriz (`-UnityResults` con los 4 XML) | 597 IDs: **PASA 595, FALLA 0, IGNORADO 2** (AC-APP-15-03 [Apple], AC-WKT-02-10 [Android]), PENDIENTE UNITY 0, SIN TEST 0 |
| Arnés `it4b` | A (`<scratch>`) y B (repo): `listo: 51 tomas, 0 con problemas, 0 sondas de aspecto que fallan` en las dos; **A = B = lo que había en el repo**, byte a byte en los 53 archivos salvo `durationMs`; 150 / 150 sondas |
| `it2` / `it3` / `it4a` | Regenerados a `<scratch>`: 44 / 49 / 13 archivos **iguales byte a byte** a lo versionado (en `manifest.json`, solo `durationMs`) |
| Build de Windows con simulador | `Windows simulator Mono2x Development`, `Build Finished, Result: Success`, en `Builds/Windows/simulator` (no había ningún `RowMate.exe` abierto) |
| Equipo | Rutina hasta el final con el diálogo y su salida (la llave **nunca** sobre el negro), terminar a mano sin diálogo, libre con su barra, doble toque y toque en el negro **sin efecto**, y el control a los 744 ms que sí arranca: **bien** (R2.4) |
| **Criterios** (101) | **98 PASS · 0 FAIL · 3 NO VERIFICABLE** (los mismos tres de la ronda 1) |
| Condiciones de la 4a | **AC-WKT-17-04 PASS** y **AC-APP-05-07 PASS** |
| Repo público | La búsqueda de rutas de la PC con `git grep -nIE` (rutas de usuario de Windows y de Git Bash, y el prefijo de las carpetas de trabajo de Claude) sobre lo versionado: **0**; con `--untracked`: **0** (también después de escribir esta sección) |

### R2.1 Corridas

Desde la raíz del repo, una instancia de Unity por vez (solo estaba el `unity.exe serve` de la Hub, que no se tocó; ningún Editor abierto sobre el proyecto).

| # | Comando | Resultado |
|---|---|---|
| 1 | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it4b/qa-main-r2` | `Build succeeded. 0 Warning(s) 0 Error(s)`; `Total tests: 182 Passed: 182`; `Total tests: 2319 Passed: 2292 Skipped: 27`; matriz sin Unity: PASA 478, FALLA 0, IGNORADO 2, PENDIENTE UNITY 117, SIN TEST 0 |
| 2 | `tools/unity/run-unity-tests.ps1 -Platform EditMode` | `total 2368, pasan 2337, fallan 0, ignorados 31, inconclusos 0`; sale con 0 |
| 3 | `… -Platform EditMode -Simulator` | `total 2550, pasan 2519, fallan 0, ignorados 31, inconclusos 0`; `-runTests -> 0`, pero **el Unity de `RestoreAfterTests` salió con `-1073741819`** (0xC0000005) después de escribir `[SIMULATOR] defines restaurados` y `Batchmode quit successfully invoked`: el *define* quedó bien (0 en `ProjectSettings.asset`, sin la marca). Repetida: las tres corridas con 0 y los mismos conteos (QA-V4b-R2-03) |
| 4 | `… -Platform PlayMode` | `total 354, pasan 354, fallan 0, ignorados 0, inconclusos 0` |
| 5 | `… -Platform PlayMode -Simulator` | `total 420, pasan 420, fallan 0, ignorados 0, inconclusos 0`; las tres corridas con 0 |
| 6 | `run-tests.ps1 -Matrix -UnityResults <scratch>/…/unity` | PASA 595, FALLA 0, IGNORADO 2, PENDIENTE UNITY 0, SIN TEST 0 |
| 7 | Prueba temporal de QA `-Platform PlayMode -Filter QaTempGcProbe` (R2.5) | 2 / 2 pasan; el archivo se borró con su `.meta` |
| 8 | `capture-snapshots.ps1 -Set it4b -Out <scratch>/…/capA` (A) y sin `-Out` (B) | A: el primer intento se cortó porque el Unity de `EnableForTests` salió con 0xC0000005 (el arnés no corrió y el *define* se restauró); repetido, bien. Las dos: `listo: 51 tomas, 0 con problemas, 0 sondas de aspecto que fallan`; atlas `0 glifos antes, 1484 después` |
| 9 | `capture-snapshots.ps1 -Set it2`, `-Set it3`, `-Set it4a` con `-Out <scratch>` | 41 / 47 / 49 tomas, 0 con problemas, 0 sondas que fallan. `it2` necesitó tres intentos: los dos primeros, el mismo 0xC0000005 en `EnableForTests` |
| 10 | `tools/build/build.ps1 -Target Windows -Simulator -Backend Mono` | `Build Finished, Result: Success` en `Builds/Windows/simulator` |

Los conteos de Unity y de dotnet coinciden con los que reportó el Architect para la ronda (2368 / 2550 / 354 / 420 y 2319). Contra la ronda 1: +13 en EditMode (los casos nuevos de `PRG`, `PPG`, `SUS`, `IRF`, `TypographyUssSync`, `ITO`, `WorkoutFormats`) y +8 / +10 en PlayMode (las seis permanentes de V1-05, `PRT.ThumbTrackSitsOnTheGlass`, `PRT.CurrentFillHasNoHoles`, `ITT.TouchesDuringTheBlackExitDoNotReachWorkout`, y con simulador también `SOT.WrenchLeavesInTheFirstFrameAlsoFromTheCompletionDialog` y `SOT.RibbonAboveTheCompletionDialog`).

Después de todo: `ProjectSettings/ProjectSettings.asset` sin `ROWMATE_SIMULATOR`, sin `Library/RowMate/simulator-tests.txt`, los `FontAsset` de `UI/Fonts/Generated` con la fecha de 00-deuda-4a (no se reescribieron) y `git status` con las mismas 412 entradas que al empezar.

### R2.2 Capturas y videos

**Qué cambió desde la ronda 1** (las tomas de la ronda 1 son las de `<scratch>/it4b/qa-main-r1/capA`, iguales a las que había en el repo entonces): 49 de 53 archivos. Iguales: CAP-28, DEMO-01, DEMO-04 y DEMO-12 (sin HUD), como dice el Architect. Las diferencias quedan donde tienen que quedar:

| Tomas | Dónde cambian (px) | Por qué |
|---|---|---|
| CAP-01 a -12, -19 a -26, -29 a -31, DEMO-05 a -11 | solo la fila de píldoras (TV y 2400×1080: y 47-69; L640: y 15-22; W1280: y 27-40): diferencias de borde (máx 25-111 por canal, en unos pocos cientos de px) | el contorno sin vértices repetidos y el rectángulo bajo el relleno (V1-04) |
| CAP-22 | 136 px en (537-547; 47-69): solo el pulgar | la tira ya no se pinta debajo del pulgar (V1-03) |
| CAP-13 a -18, DEMO-02, DEMO-03 | la barra libre (título, reloj y chip) y, en CAP-14 / DEMO-03, `PAUSA` en el disco | el *tracking* (V1-02) |
| CAP-27 | la fila y `PAUSA` | V1-02 y V1-04 |

**Cada PNG que cambió, mirado con Read** (los 39), más recortes ampliados de la fila (r1 arriba, r2 abajo) y de la barra libre:

| Toma | Qué se vio | Contra la spec / las notas |
|---|---|---|
| CAP-01 a CAP-06, CAP-12, CAP-25, CAP-26, CAP-29, CAP-30, DEMO-05 a DEMO-10 | Lo mismo que en la ronda 1 (píldoras, halo, cuenta grande, sello, la última llena); a 4× los bordes de arriba y de abajo de cada cápsula ahora son rectos | Bien |
| CAP-27 | **Sin el píxel suelto** en el relleno de la 3 (a 4×, en el mismo lugar que en la ronda 1); `PAUSA` con su *tracking* dentro del aro | QA-V4b-04 cerrado |
| CAP-22 | 200 tramos; el pulgar del paso 100 con su pista sobre el vidrio: en y = 58, de x 543 a 546, `#4E2540` (antes `#6F344E` y `#4E3D4E`); esperado `#4D253F`. Sonda 6/6 | QA-V4b-03 cerrado; AC-WKT-14-16 |
| CAP-13 / CAP-14 / CAP-15 | `Entrenamiento Libre` **entero**, a 0,805 (texto 131,98 dp = 125,1 base a s = 1,055); reloj `10:00`; chip `EN MARCHA` / `PAUSADO` / `FINALIZADO` con su *tracking*, 106,9 dp = **101,3 base** (notas 101,8 ± 1). **Pero el título queda pegado al ícono del reloj**: 11 px (≈ 4 dp) de la `e` al ícono, menos que el espacio entre `Entrenamiento` y `Libre` (14 px); en la ronda 1 eran 22 px | AC-WKT-16-03 y -06 se cumplen; el aire es QA-V4b-R2-01 |
| CAP-16 / CAP-17 / DEMO-02 / DEMO-03 | TH y W1280: título a su tamaño, el chip más ancho, todo con aire | Bien |
| CAP-18 | `Free Workout` a su tamaño, `ACTIVE` 77,56 dp = **73,5 base** (notas 74,1 ± 1) | Bien |
| CAP-07 a CAP-11, DEMO-11 | El diálogo igual que en la ronda 1; solo cambia la fila de detrás | Bien |
| CAP-19 / -20 / -21 / -23 / -24 / -31 | La fila de información igual; solo los bordes de las píldoras | Bien |

**Videos.** En el *manifest* los 8 tienen valores por cuadro y **ninguno tiene NaN** (VID-01 67, VID-02 31, VID-03 67, VID-04 555, VID-05 81, VID-06 150, VID-07 180, VID-08 52); los 8 con `passed: true`. Cuadros extraídos con PyAV:

| Video | Qué se vio |
|---|---|
| VID-05 (la salida desde el diálogo) | `Terminar` en el cuadro 210: la llave **se va en 210-212** (90 ms, `ease.exit`) y no está en 213-230 (HUD que funde, fondo azul, negro); **vuelve en 231-232, en el lugar de Workout**, cuando Workout ya se ve. Nunca sobre el negro. QA-V4b-01 cerrado |
| VID-02 (la salida a mano) | `00:12` y `EN MARCHA` → `FINALIZADO`, nunca `00:00` ni `—`; la llave se va en 14-16 y vuelve en Workout a los 40; los 31 valores (`hud`, `backdrop`, `page`, `t`, reloj y chip) los compara `ExitCheck` con `ImmersiveTimeline.Exit` (± 0,01): `passed: true`. QA-V4b-07 cerrado |
| VID-07 (kcal) | `kcalShift` 0 en los cuadros 0-32 (antes NaN en 0-29); BPM funde en 30-33, kcal se desliza de −71,4 a 0 en 33-40; al volver el pulso (150-160), al revés |

**`it2` / `it3` / `it4a`.** Lo versionado sigue igual a la regeneración de QA de la ronda 1, y la regeneración de esta ronda con el código nuevo da **los mismos bytes** (salvo `durationMs`). Los arreglos no tocaron Dispositivo, Rutinas ni las tomas de la 4a.

### R2.3 Los issues, uno por uno

| ID | Qué dice el Architect (V1-xx) | Qué verificó QA | Estado |
|---|---|---|---|
| QA-V4b-01 | V1-01: salir de la superficie del entrenamiento es siempre un cambio de carril | `SimulatorLayerView.Place` (`workoutChange` dispara `_laneSwap` aunque el carril sea el mismo); `SOT.WrenchLeavesInTheFirstFrameAlsoFromTheCompletionDialog` pasa; VID-05 (R2.2); **equipo**: la llave está en la foto del diálogo, ya no está a los 80 ms del clic ni en el negro, y vuelve en Workout a los 840 ms (cero píxeles cian en su zona de 80 a 530 ms) | **Cerrado** |
| QA-V4b-02 (= P01) | V1-02: `TypographyUssWriter.LetterSpacingUnits(em)` = em × 100 en el chip y en `PAUSA`; aire título-reloj de 6 a 2 | `PhaseChipView.cs:88` y `CenterCallouts.cs:68` usan la función; `FBT.ContentInOrderWithClockAndChip`, `FBT.EnglishTitleFitsAtItsSize`, `CCT.PausedTakesTheDiscOneAtATime`, `IRF.FreeTitleFitsInTvWithTheChipAtItsTracking` y `TypographyUssSyncTests.CodeTrackingUsesTheTextCoreUnit` pasan; el *manifest*: 101,3 / 73,5 base; la comprobación de CAP-13 / CAP-18 contra el ancho de las notas (O-P03) pasa; el equipo: `ACTIVE` y `PAUSED` con su *tracking* | **Cerrado** (con QA-V4b-R2-01) |
| QA-V4b-03 | V1-03: la tira no se pinta debajo del pulgar | `PRT.ThumbTrackSitsOnTheGlass` pasa; CAP-22 `#4E2540` (R2.2) | **Cerrado** |
| QA-V4b-04 | V1-04: contorno sin vértices repetidos y un rectángulo opaco sin suavizado debajo del relleno | `PRG.OutlinesHaveNoRepeatedVertices` (dotnet y Unity) y `PRT.CurrentFillHasNoHoles` pasan; CAP-27 sin el píxel. El rectángulo usa `MeshGenerationContext.Allocate` con un `Vertex[4]` propio y los índices estáticos: sin asignaciones del GC. Con alfa < 1 no se pone: durante el cruce del cambio de paso la grieta podría verse unos cuadros (aceptable) | **Cerrado** |
| QA-V4b-05 | V1-05: las PlayMode que faltaban, y medir asignaciones con el Recorder de `GC.Alloc` | Las seis existen y pasan en las dos suites (`HLT.RotationWithDialogSealOrBigCount`, `HBT.LongSeriesStaysSmooth` de 26,2 a 31,5 s, `HBT.NoAllocationsPerFrameWithTwoHundredPills` con la fila de 1998, `IHT.KcalSlides`, `SOT.RibbonAboveTheCompletionDialog`, `CDT.CountUpDoesNotAllocatePerFrame`). **Control positivo de QA** (R2.5): el Recorder ve 1 asignación con un `byte[4096]` y 160 con el camino de cadenas del conteo de la ronda 1, y 0 con el código actual. Lo que queda cubierto de otra forma (la ventana minimizada, kcal en el dispositivo) está justificado | **Cerrado** |
| QA-V4b-06 | V1-06: **queda abierto**, propuesto para la It. 5 | El equipo lo confirma: la primera ventana `[PERF]` de cada inmersivo da máx 47,7 / 31,5 / 69,0 / 69,1 ms; las demás, ≤ 9,3 ms (p99 ≤ 1,4) | **Abierto (diferido)**: lo tiene que aceptar el coordinador en el REPORT |
| QA-V4b-07 | V1-07: VID-02 con sus valores y la comparación; `kcalShift` nunca NaN | `ITO.VideosLeaveTheirFrameValuesWithoutNaN` y `HMT.KcalShiftIsNeverNaN` pasan; el *manifest* (R2.2) | **Cerrado** |
| QA-V4b-P02 | V1-08: con un segundo nuevo, el relleno vale al menos transcurrido / D | `PillProgress.OnState` (el presente solo sube y nunca pasa de (e + 1) / D); `PPG.LastTimedStepEndsFull` (4 casos) pasa en dotnet y en Unity; en el equipo, en **las seis** rutinas que terminaron, la última píldora está llena hasta x = 1242 y después viene el vidrio, sin pista | **Cerrado** |
| QA-V4b-MOT-01 | V1-09: el conteo escribe en un `char[]` y `TabularNumber.SetValue(char[], int, double)` | `CompletionDialog.ShowValues` usa `WriteSessionClock` / `WriteInteger` sobre `_digits`; `SetValue` compara y no crea cadenas (el `Text` perezoso solo lo lee el arnés). `CDT.CountUpDoesNotAllocatePerFrame` pasa y el control positivo de QA mide 0 en 30 cuadros del conteo con el código actual | **Cerrado** |
| QA-V4b-MOT-02 | V1-10: la raíz negra absorbe los toques hasta `RevealStarted` | `ImmersiveWorkoutPage.BeginExit` (`Root.pickingMode = Position` mientras sale); `ITT.TouchesDuringTheBlackExitDoNotReachWorkout` y `WSC.StartDuringThePreviousExitHasItsOwnImmersive` pasan; **equipo** (R2.4): el segundo toque a 233-264 ms y el toque en `Free workout` a 233-263 ms no hicieron nada; el mismo toque a 744-775 ms sí arrancó el libre | **Cerrado** |
| QA-V4b-MOT-03 | V1-11: `LiveDotMotion` lee reducir movimiento en cada cuadro | `Advance` aplica 1 y sale si se prende; `SetActive(true)` lo vuelve a arrancar si se apaga (`_active` no se pierde, porque salir del *driver* no llama a `OnStopped`); `HMT.LiveDotStartsAtOneAndPulsesWhileActive` pasa. En el equipo no se probó (cambiar las animaciones de Windows) | **Cerrado** |
| QA-V4b-MOT-04 | V1-12: la vuelta del segundo plano se anota como tal | `SuspendAccounting.Awake(…, fromBackground)` (`vuelta del segundo plano de N s sin aviso de suspensión: cuenta (Q-91)`); `SUS.BackgroundReturnIsNotLoggedAsALateFrame` pasa. En Windows no aplica (minimizar no es segundo plano) | **Cerrado** |
| QA-V4b-MOT-05 | V1-13: cinco salidas y 0 avisos de DPI | `ITT.NothingAnimatesAfterTheExit` recorre `a mano` (libre con chip y punto), `a mano con el sello y PAUSA`, `a mano con la cuenta grande` (y `Próximo`), `Terminar del diálogo` y `Terminar durante el conteo`, con `Application.logMessageReceived` y 0 avisos; pasa en las dos suites. En el equipo, 0 avisos de DPI en los tres registros | **Cerrado** |

**Lo que dejó abierto el Architect** (V1-06) tiene motivo: mover la construcción del HUD cambia lo que ven las pruebas en el cuadro del toque (AC-WKT-13-01) y no es barato en esta ronda. Es un tirón al empezar el fundido a negro, no un problema de datos; QA está de acuerdo con diferirlo si el REPORT lo anota para la It. 5 con la medida del build.

### R2.4 Prueba en equipo (build de Windows con simulador)

Build de `Builds/Windows/simulator` (el de la corrida 10), 1280 × 720 en ventana, Windows en inglés, `ROWMATE_DATA_DIR` en `<scratch>` con la base de `F-4A` copiada como `rower_app_dev.db`, registro del player en `<scratch>`. Las ayudas Win32 de la ronda 1 (clic solo si la ventana es la de adelante y el punto es suyo; el puntero vuelve a su lugar; se cierra solo el PID lanzado) y un clic rápido nuevo (abajo y arriba con 20 ms) para medir los toques en el negro contra el "arriba" del primer toque. Tres sesiones de la app, unos 6 minutos de ventana.

| Paso | Qué se vio | Resultado |
|---|---|---|
| Libre: la barra de arriba | `Free Workout` entero, `⏱ 02:37` (24 h), `● ACTIVE` con su *tracking*; en pausa, `PAUSED` en el chip y en el disco, dentro del aro | Bien |
| Libre terminado a mano | 150 ms: `FINISHED` y el diálogo de terminar que se va; 600 ms: Workout que aparece; 2 s: Workout en reposo. Nunca el diálogo de completado | Bien (AC-WKT-21-07) |
| `Corta` hasta el final | `Workout Completed!` con `00:30`, `191 m`, `10`; la banda sigue en vivo detrás (`196 m`) | Bien |
| La salida desde el diálogo | Fotos a 80, 190, 280, 360, 450, 530 y 840 ms del clic: la llave ya no está a los 80 ms; el HUD que se va, el fondo, el negro (360), Workout (450); la llave vuelve en su lugar de Workout a los 840 ms | Bien (AC-BLE-16-02) |
| `Corta` terminada a mano | 150 ms, 600 ms y 2 s: nunca el diálogo de completado | Bien |
| Doble toque en `Finish` del diálogo | El segundo toque, abajo a +233 ms y arriba a +264 ms del primero: Workout en reposo a 1,5 y 3 s, sin `¿Empezar …?` ni sesión nueva | Bien (QA-V4b-MOT-02) |
| `Finish` y toque en `Free workout` en el negro | Abajo a +233, arriba a +263 ms: no arrancó nada | Bien |
| Control (ROB-01) | El mismo toque a +744 / +775 ms, con Workout a la vista: arrancó el libre (y se terminó a mano) | Bien (AC-WKT-01-09) |
| La última píldora | Llena hasta el borde (x 1242, después el vidrio) en las seis rutinas que terminaron | Bien (AC-WKT-14-13) |
| La ventana | 1296 × 759, cliente 1280 × 720, el mismo estilo, en las tres sesiones | Bien (AC-APP-15-02) |
| El registro | `[TIME] avisos de suspensión registrados (P-17)`; `barras del sistema: ocultas` / `visibles` por cada entrada y salida; **0 avisos de DPI**, 0 excepciones, 0 errores propios | Bien |
| Rendimiento | Primera ventana `[PERF]` de cada inmersivo: 47,7 / 31,5 / 69,0 / 69,1 ms; las demás ≤ 9,3 ms, p99 ≤ 1,4 ms | QA-V4b-06 (abierto) |

Sesiones guardadas (`tools/dotnet/RowMate.SessionList --db` sobre copias): en la primera sesión de la app, libre (2 puntos), `Corta` sola (7 puntos, 30 s) y `Corta` a mano (2 puntos); en la de los toques, tres `Corta` de 7 puntos y 30 s y **un solo libre** (el del control): los toques en el negro no crearon ninguna sesión. Al terminar: la base del usuario (`rower_app_dev.db` en la carpeta de datos de la app) con la misma fecha y el mismo md5 que antes, y las preferencias de la ventana del player (registro de Windows), que las corridas cambian, restauradas desde la exportación de antes (la exportación final es igual byte a byte).

### R2.5 Prueba temporal de QA (borrada al terminar)

`Assets/RowMate/Tests/PlayMode/QaTempGcProbeTests.cs`, corrida con `-Filter QaTempGcProbe` y borrada con su `.meta` (`git status` igual que al empezar):

| Prueba | Qué midió | Resultado |
|---|---|---|
| `QaTempRecorderSeesAKnownAllocation` | `GcAllocations.During(() => new byte[4096])` y `GC.GetAllocatedBytesForCurrentThread` alrededor | `GcAllocations = 1`, **`GetAllocatedBytesForCurrentThread = 0`**; 0 sin asignación |
| `QaTempRecorderSeesTheOldStringPathOfTheCountUp` | En el diálogo de `Corta`, 30 cuadros del camino de la ronda 1 (`SetValue(string)` con `SessionClock` y `ToString`), y después 30 `H.Step()` del conteo actual | **160** asignaciones con cadenas; **0** con el código actual |

**Corrección de la ronda 1.** La prueba temporal de la ronda 1 (§6.3, `QaTempAllocProbeTests`, "0 B en las cinco ventanas") medía con `GC.GetAllocatedBytesForCurrentThread`, que en Unity (Mono) devuelve siempre 0: **ese 0 B no valía**. Lo que sostiene ahora AC-WKT-14-19 y la regla de 0 B por cuadro son las pruebas con el Recorder (`HBT.NoAllocationsPerFrameWithTwoHundredPills`, `HBT.NoAllocationsPerFrameWhileItOnlyBeats`, `HMT.NoAllocationsPerFrameAndStopLeavesNothingAnimating`, `CDT.CountUpDoesNotAllocatePerFrame`), y este control positivo muestra que el Recorder sí ve las asignaciones en esta corrida en *batchmode*. Miden el código del HUD de cada cuadro, no el `generateVisualContent` del repintado (lo acota el tiempo de `LongSeriesStaysSmooth`).

### R2.6 Criterios de aceptación

Las pruebas de la ronda 1 se volvieron a correr y pasan todas (la matriz con los 4 XML: PASA 595, FALLA 0). Los criterios que esta ronda cambia o que se pidieron en especial:

| AC | Ronda 1 | Ronda 2 | Evidencia |
|---|---|---|---|
| AC-BLE-16-02 | FAIL | **PASS** | ps `SOT.WrenchLeavesInTheFirstFrameAlsoFromTheCompletionDialog`, `SOT.RibbonAboveTheCompletionDialog` y los 65 de la matriz; `WrenchAudit` en las 51 tomas; VID-05 (sale en 210-212, vuelve en Workout en 231-232); eq (R2.4) |
| AC-WKT-16-03 | FAIL | **PASS** | d u p (27): `TypographyUssSyncTests.CodeTrackingUsesTheTextCoreUnit`, `FBT.ContentInOrderWithClockAndChip` (`letterSpacing` 8, ancho 101,8 ± 1), `FBT.EnglishTitleFitsAtItsSize` (74,1 ± 1), `CCT.PausedTakesTheDiscOneAtATime`; CAP-13 / -14 / -15 / -18 (101,3 y 73,5 base); eq (`ACTIVE`, `PAUSED`) |
| AC-WKT-16-06 | PASS (en riesgo) | **PASS** | d u p (8): `IRF.FreeTitleFitsInTvWithTheChipAtItsTracking` (TV y P360 en español, entero a 0,805 ≥ 0,8, sin "…"), `FBT.*`; CAP-13 / -16 / -17 / -18. Observaciones: QA-V4b-R2-01 (el aire con el reloj) y QA-V4b-R2-02 (la spec dice ≈ 0,82) |
| AC-WKT-14-13 | PASS (la lente: FAIL) | **PASS** | d u (17): `PPG.LastTimedStepEndsFull` ×4 (0,984 y 0,999 s → 1; el control con 1,016); CAP-06, CAP-12; eq: la última llena en las seis rutinas |
| AC-WKT-14-16 | PASS (con observación) | **PASS** | d u p (22): `PRT.ThumbTrackSitsOnTheGlass`; CAP-22 con 6/6 sondas |
| AC-WKT-14-19 | PASS | **PASS** | p `HBT.LongSeriesStaysSmooth` (26,2-31,5 s), `HBT.NoAllocationsPerFrameWithTwoHundredPills` (200 y 1998, Recorder); control positivo (R2.5); eq ≤ 9,3 ms fuera del primer cuadro (QA-V4b-06) |
| AC-WKT-13-08 | PASS | **PASS** | p `ITT.TouchesDuringTheBlackExitDoNotReachWorkout`; VID-02 con `ExitCheck`; eq (los toques en el negro) |
| AC-WKT-13-09 | PASS | **PASS** | p `ITT.NothingAnimatesAfterTheExit` (cinco salidas, 0 avisos de DPI), `IHT.LeavingTheImmersiveStopsItsContinuousMotions`; eq: 0 avisos de DPI |
| AC-WKT-17-07 | PASS (con prueba temporal) | **PASS** | p `HLT.RotationWithDialogSealOrBigCount` (permanente) |
| AC-WKT-17-11 | PASS | **PASS** | d `HTL.KcalSlide`; p `IHT.KcalSlides`, `HMT.KcalShiftIsNeverNaN`; VID-07 sin NaN |
| AC-WKT-21-05 | PASS | **PASS** | p `CDT.CountUpDoesNotAllocatePerFrame`; VID-05 (los totales terminan en `00:30`, `1120`, `43`) |
| AC-APP-05-08 | PASS | **PASS** | p `HMT.LiveDotStartsAtOneAndPulsesWhileActive` (el caso de MOT-03); VID-08 |
| AC-WKT-02-04 | PASS | **PASS** | d u `SUS.BackgroundReturnIsNotLoggedAsALateFrame` |
| AC-WKT-01-09 (4a, por MOT-02) | — | **PASS** | eq: el toque a 744 ms arranca el libre; `WSC.StartDuringThePreviousExitHasItsOwnImmersive` a 450 ms |
| AC-WKT-17-04 (condición de la 4a) | PASS | **PASS** | p (4) `HudLayoutTests.BandDividerKeepsItsWidth` en las dos suites; la sonda del divisor de CAP-13, `#263241`, igual a la esperada |
| AC-APP-05-07 (condición de la 4a) | PASS | **PASS** | p (35) `FirstTabWithoutFocusShowsTheRing`, `…AtRest`, `CDT.FirstTabWithoutFocusReachesTheDialog`, `ITT.KeyboardDuringTheCascade` |

El resto de los criterios queda como en §3 (con su evidencia de entonces, que se volvió a correr). **Conteo:** **98 PASS · 0 FAIL · 3 NO VERIFICABLE** (AC-WKT-02-11 en equipo, que verifica el usuario; AC-APP-15-01 [Android]; AC-APP-15-03 [Apple]), por los mismos motivos de §9.

### R2.7 Revisión estática de los arreglos

| Tema | Resultado |
|---|---|
| Nada animando al salir | `LaneSwap` es finito y sale al terminar la transición; `HudMotion.Stop` / `Leave` como en la ronda 1; `LiveDotMotion` con reducir movimiento sale del *driver* en el cuadro siguiente; la raíz que absorbe toques no tiene manejadores y se va con la página. Las pruebas de salida pasan con cinco salidas |
| Asignaciones por cuadro (HUD y diálogo) | `ShowValues` con `char[]` propio; `TabularNumber.SetValue(char[], …)` sin cadenas (el `Text` perezoso solo si alguien lo lee, como el arnés; `_fallback.text` solo sin `FontAsset`); `FillUnderlay` con `Vertex[4]` reutilizado y `Allocate` del contexto; `PillProgress` sin asignaciones. Medido con el Recorder y su control positivo (R2.5) |
| `PillProgress` (V1-08) | El presente solo sube y respeta el tope de (e + 1) / D: no adelanta el reloj (AC-WKT-14-10) y la pausa no trae segundo nuevo (Q-58). `PPG.NeverGoesBackWithinAStepAndStillInPause` ya no manda segundos imposibles |
| Repo público | `git grep -nIE` (rutas de usuario de Windows y de Git Bash, y el prefijo de las carpetas de trabajo de Claude) sobre lo versionado: 0; `--untracked`: 0. `manifest.json` e `INDEX.md` de `it4b` sin rutas. Los *scripts* de QA quedaron en `<scratch>` |
| Legacy | `git -C ../rowmate-legacy status --porcelain` vacío. Se miró `lib/core/dev/rowing_simulator.dart`: la distancia del simulador se acumula desde que conecta, como en la app (los totales del diálogo de la segunda `Corta` incluyen los metros de la primera, igual que en el legacy) |

### R2.8 Issues de la ronda 2

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V4b-06 | menor | architect | (Sigue abierto, diferido) El primer cuadro de cada inmersivo tarda 31-69 ms de CPU (arma el HUD en el cuadro del toque) | Equipo: 47,7 / 31,5 / 69,0 / 69,1 ms en la primera ventana `[PERF]`; las demás ≤ 9,3 ms | Lo de V1-06 para la It. 5 (armar el HUD en el negro o al abrir Workout y medir con `FrameTimeProbe`). Anotarlo en el REPORT como desvío aceptado |
| QA-V4b-R2-01 | menor | designer | **En TV y P360 en español, el título libre queda pegado al reloj.** Con el chip a su ancho y el aire mínimo de 2 (04-design-notes §16), entre la `e` de `Libre` y el ícono del reloj quedan ≈ 4 dp, menos que el espacio entre las dos palabras del título: se lee `Libre ⏱ 10:00` como un grupo. Se cumple AC-WKT-16-06 (nada se encima), pero no la jerarquía de la barra (el título a la izquierda; el reloj y el chip juntos a la derecha) | CAP-13, CAP-14, CAP-15 (y P360, que usa la misma escala): 11 px de la `e` al ícono contra 14 px entre `Entrenamiento` y `Libre`, y 21-22 px del ícono a las cifras y del reloj al chip; en la ronda 1, 22 px | Que el Designer lo valide o lo ajuste sin bajar el título de 0,8: por ejemplo, menos relleno horizontal en el chip (8 → 6) o menos aire entre el ícono del reloj y sus cifras solo en esa barra, para devolverle 2-3 dp al hueco; y que §16 diga lo que se ve de verdad (≈ 4 dp, no ≈ 5). Si se acepta así, anotarlo |
| QA-V4b-R2-02 | menor | po | **La spec todavía dice "≈ 0,82"** para el título libre en TV y P360 (AC-WKT-16-06 y la fila de CAP-13 de §9), y el valor ahora es 0,805 (04-design-notes §16) | `01-po-spec.md`: AC-WKT-16-06 y la tabla de capturas (CAP-13) | Cambiar a "≈ 0,8" (o "0,805"): la regla (achicar hasta 0,8 y después "…") no cambia |
| QA-V4b-R2-03 | menor | architect | **Unity a veces revienta (0xC0000005) al cerrar** un `-batchmode -quit -executeMethod` que cambió los *defines* (`EnableForTests` / `RestoreAfterTests`), después de `Batchmode quit successfully invoked`, con la compilación que pidió el cambio de *defines* todavía pendiente. Los *scripts* reaccionan bien (siempre restauran y verifican el *define*), pero la corrida queda marcada como fallida o el arnés no corre | 4 de unos 14 cambios de *define* en esta ronda: el `RestoreAfterTests` de EditMode con simulador (los tests ya habían pasado: `-runTests -> 0`) y tres `EnableForTests` del arnés (A y dos de `it2`); repetidos, bien. Registros en `<scratch>/it4b/qa-main-r2/` (`unity/unity-simulator-restore.log`, `cap-art-A-try1/`, `cap-art-it2-try1/`, `cap-art-it2-try2/`) | En `SimulatorDefine`, esperar la compilación antes de salir (o no pedirla en *batchmode*) o, en `run-unity-tests.ps1` y `capture-snapshots.ps1`, reintentar una vez el paso del *define* si sale con 0xC0000005 y el *define* quedó como se pidió. Anotarlo en `tools/unity/README.md` |

**Observaciones (sin issue):**

- O-R2-01: el Unity Test Framework deja un `TestResults.xml` en la carpeta de datos de la app (`Application.persistentDataPath` del Editor) en cada corrida de PlayMode. No es una base ni la toca (AC-HIS-20-04 se cumple: `rower_app_dev.db` con la misma fecha y el mismo md5), pero si se quiere esa carpeta limpia, el *script* de tests puede borrarlo al terminar.
- O-R2-02: en el equipo no se probaron "reducir movimiento" a mitad de sesión (MOT-03) ni la vuelta del segundo plano (MOT-04): en Windows minimizar no es segundo plano, y cambiar las animaciones del sistema afecta al usuario. Quedan con sus pruebas.
- O-01 y O-03 de la ronda 1 siguen igual (el desplazamiento de CAP-10 sin indicio y los ceros de VID-08); O-02 ya no aplica (la sonda de CAP-22 pasa).

### R2.9 Verificación hecha

1. **Legacy, solo lectura**: se leyó `lib/core/dev/rowing_simulator.dart` (la distancia acumulada) y se miró `docs/image-03.png`; `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó el módulo de credenciales de Strava.
2. **Unity**, una instancia por vez; solo se cerró el player lanzado por QA, por su PID. El `unity.exe serve` de la Hub no se tocó.
3. **Bases del usuario intactas** (misma fecha y mismo md5); las preferencias de la ventana del player, exportadas antes y restauradas después (igual byte a byte). Todo dato del build y de las pruebas quedó en `<scratch>`.
4. **Repo**: sin *commits*. Lo único que QA escribió en el repo es esta sección (y su fila en la tabla del encabezado) y la corrida B del arnés, igual a lo que había salvo `durationMs`. La prueba temporal se borró con su `.meta`.
5. **Repo público**: esta sección cita el repo con rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) o `../rowmate-legacy` y el scratch como `<scratch>`; ninguna ruta de la PC ni datos del equipo.
