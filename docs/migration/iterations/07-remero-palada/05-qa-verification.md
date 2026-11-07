# It. 7 — Verificación de QA (principal)

| Campo | Valor |
|---|---|
| Iteración | 7 · `07-remero-palada` |
| Entrada | [01-po-spec](01-po-spec.md) (38 AC), [02-tech-design](02-tech-design.md) (con "Cambios por implementación" WP-A, WP-B y WP-D), [04-design-notes](04-design-notes.md), [03-qa-gate](03-qa-gate.md), [snapshots/](snapshots/INDEX.md) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) `3ae0f3a`; `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Y Bot | `Assets/ThirdParty/Mixamo/YBot/YBot.fbx` **está** en la máquina de desarrollo: lo de Y Bot se verificó |
| Modo | Entrega rápida: mayores solo si rompen un AC, la sesión, el rendimiento o la paridad; el resto, menor o "Para issues" |

## Veredicto: **APROBADO CON OBSERVACIONES**

> **Ronda 2 (al final):** QA7-V-01 corregido; AC-SCN-05-08 pasa a PASS. Queda **37 PASS · 0 FAIL · 1 NO VERIFICABLE**, sin mayores.

Ronda 1: **36 PASS · 1 FAIL · 1 NO VERIFICABLE** (de 38). Todo compila y todas las suites están en verde, la palada, el bote, los charcos, la salpicadura, la sombra, la boya y el slot de Y Bot funcionan, y la sesión es idéntica con y sin escena. Hay **1 mayor**: AC-SCN-05-08 falla tal como está escrito (la tabla de 10 poses de ANIMATION_BIBLE §3.4 no se cumple en 21 de 50 ángulos; el test solo exige un subconjunto). La corrección por defecto es de documento, sin código: adoptar la hoja medida (A7-01) como tabla y exigirla entera. El `[PERF]` de tiempos de cuadro quedó NO VERIFICABLE en esta sesión por el entorno (§4).

## 1. Suites (salida real)

| Suite | Comando | Resultado |
|---|---|---|
| dotnet (.NET 8) | `tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it7/qa-r1` | Build `0 Warning(s)`. **192/192** (herramientas) + **3190: 3151 pasan, 39 omitidos, 0 fallan**. Matriz: PASA 656, FALLA 0, IGNORADO 4, PENDIENTE UNITY 149, SIN TEST 0 |
| Unity EditMode | `tools/unity/run-unity-tests.ps1 -Platform EditMode` | **3289: 3244 pasan, 45 ignorados, 0 fallan** |
| Unity EditMode + simulador | `... -Platform EditMode -Simulator` | **3481: 3436 pasan, 45 ignorados, 0 fallan** (el `EnableForTests` salió con −1073741819 al cerrar, caso conocido QA-V4b-R2-03; el *define* quedó y se restauró) |
| Unity PlayMode | `... -Platform PlayMode` | **500/500**, 0 ignorados, 0 inconclusos |
| Unity PlayMode + simulador | `... -Platform PlayMode -Simulator` | **573/573** |
| Guardia de `ProjectSettings` | las cuatro corridas y el build | "sin cambios" |
| Build | `tools/build/build.ps1 -Target Windows -Simulator` | `Builds/Windows/simulator/RowMate.exe`, Development, `Result: Success`, `THIRD_PARTY_NOTICES.md` al lado |

PlayMode de la 7, todos `Passed`: `RowerRigTests.TheRigFollowsCoreInTheTenPoses(Mannequin|Slot)`, `RowerSlotTests` (sin *binding*, con Y Bot, *binding* sin modelo y modelo inválido: un aviso `[SLOT] rower.character: …` cada uno), `RowerSceneTests.EveryPhaseDrawsWithThreePuddles(390|844 × Mannequin|Slot)` (LT-97, LT-98), `TheSessionIsTheSameWithAndWithoutTheScene`, `TheShadowIsTheMapOrTheOvalNeverBoth(High|Medium|Low)`, `RowerOcclusionTests(TV|P360)`, `SceneAdaptationTests.TheRowerFitsTheMediumBudget(Mannequin|Slot)` (`[SBT]` Medio 12 h: maniquí 82 dibujos / 50 SetPass / 164 k triángulos, remero 3 696; Y Bot 84 / 50 / 267 k), `SceneBudgetTests.NoAllocationsPerFrame` (control positivo 120/120) y `TheHudAddsNoElementsWithTheScene`.

## 2. Capturas y videos (mirados)

Se miraron CAP-01 a -21 (con CAP-17 y CAP-18 sueltas y en hoja), CAP-19 contra LEG-01/-02, DEMO-05 y cuadros extraídos de VID-02, VID-04, VID-06 y VID-07; los valores por cuadro de los siete videos salen del `manifest.json`.

| Qué | Evidencia | Resultado |
|---|---|---|
| Drive y recuperación | CAP-01 (c 0,18: piernas extendiéndose, palas enterradas, plumeo 0°), CAP-02 (c 0,60: plumeo 90°, centro de pala +0,09) | ok |
| Ritmo | VID-01: catch cada 3,000 s, catch → finish (c 0,36 = 0,9·r) 0,900 s ⇒ drive 1,00 s. VID-02: cada 2,000 s, 0,733 s ⇒ 0,815 s. VID-03: `r` cambia solo en el cruce del catch (5,4 s y 7,4 s) | ok (± 0,05 s) |
| Pausa y reposo | VID-04: pausa a 3,0 s, `φ` quieta en 3,17 s, `ω_s` < 0,01 a 3,57 s; charcos 2 → 0 en pausa; respira desde 5,47 s; reanuda a 8,0 s sin salto (`φ` 0,226 → 0,358 a 8,5 s) | ok, respiración tarde (QA7-V-05) |
| Sin paladas | VID-05: `p = φ = c = 0` y `ω = 0` desde 2,93 s; respira desde 4,40 s (1,47 s después) | ok |
| Salpicadura y gotas | CAP-03: 26 gotas + corona en las dos palas a c 0,043; VID-01 máx 22 gotas vs VID-02 máx 44 (`8 + 14·e` por pala) | ok |
| Charcos | VID-04 (2,0 s) y VID-06 (1,3 s): pares de remolinos con espuma; **CAP-04 no los muestra** (QA7-V-03) | ok por video |
| Sombra | CAP-05 (09 h, astro a la izquierda → sombra a la derecha, +0,35), CAP-06 (12 h, centrado → izquierda), CAP-07 (17 h → izquierda), CAP-08 (22 h, luna, fuerza 0,25, todo oscuro y azulado), CAP-09 (Bajo: óvalo +0,35, alfa 0,078) | ok |
| Boya | VID-07: `relZ` cruza 9,5 en el mismo cuadro en que el panel cierra `#1` (t 10,033 s = 155,03 s); ningún salto > 0,3 m por cuadro. CAP-15 (`relZ` 28,7, vueltas 0), CAP-16 (8,7, vueltas 1, fija), DEMO-05 (W1280, naranja a la derecha) | ok |
| Horizonte | máx 2,41 dp en los videos; 2,87 dp en DEMO-05 (W1280) | ok (≤ 3) |
| Zona, paneles y llave | CAP-10/-11: cuerpo dentro de la zona; CAP-14: llave [415; 246 – 455; 286] y cuerpo [306,9; 190,5 – 331,5; 225,5] sin cruce; CAP-12: los paneles no tocan el cuerpo | ok; **CAP-12 = CAP-13** (QA7-V-02) |
| Poses | CAP-17 y CAP-18: el rig sigue a Core (≤ 1,6° con Y Bot, ≤ 0,02° con el maniquí); manos a ≤ 1,5 cm del puño; **la tabla de §3.4 no se cumple** (QA7-V-01) | ver QA7-V-01 |
| Composición del legacy | CAP-19a/b contra LEG-01/-02: cabeza del remero en y 395 / 388 dp (legacy 428 dp el punto del contrato); de frente; bote y remos más finos (DV-68). El remero se ve bastante más chico que en el legacy (DV-74: proporciones reales) | ok (Para issues) |
| Y Bot | CAP-21, DEMO-06 y el *player*: `[PERF] remero: slot …/YBot.fbx, 55320 triángulos, 1.786 m` | ok (altura: QA7-V-06) |

## 3. Matriz AC → evidencia → resultado

| AC | Evidencia | Resultado |
|---|---|---|
| AC-SCN-04-01 (LT-88) | dotnet `SceneStateStrokeTests`, `StrokeGoldenTests` (oráculo 1e-12) | PASS |
| AC-SCN-04-02 (LT-89) | dotnet `SceneStateStrokeTests`, `ClosingAndPauseMatchTheLegacy` | PASS |
| AC-SCN-04-03 (LT-90) | dotnet `SceneStateStrokeTests`, `ClosingAndPauseMatchTheLegacy` | PASS |
| AC-SCN-04-04 | dotnet `VisualStrokeClockTests` (9); VID-04 y VID-05 (`φ` nunca retrocede; traba en el catch) | PASS |
| AC-SCN-04-05 | dotnet `StrokeRhythmTests` (16); VID-01, VID-02, VID-03 | PASS |
| AC-SCN-05-01 a -04 (LT-92 a -95) | dotnet `StrokeCycleTests` y oráculo | PASS |
| AC-SCN-05-05 (LT-96) | dotnet `LegacyBoatGeometryTests` y oráculo | PASS |
| AC-SCN-05-06 | dotnet `RiggingFitTests`, `ScullingKinematicsTests`; maniquí 1,85 m | PASS (QA7-V-06: Y Bot 1,786 m) |
| AC-SCN-05-07 | dotnet `ScullingKinematicsTests` (curvas, continuidad a 60 fps); VID-06 (plumeo continuo) | PASS |
| **AC-SCN-05-08** | manifest CAP-17/-18 (ángulos medidos en el rig); `TheTenPosesOfTheTable` solo exige 19 de 50 + tronco y barrido | **FAIL** (QA7-V-01) |
| AC-SCN-05-09 | dotnet (trayectoria y cruce); PlayMode `RowerRigTests`; manifest: manos a 1,46 / 1,25 cm | PASS |
| AC-SCN-05-10 | dotnet `EffortAndIdleTests`, `VisualStrokeClockTests`; VID-04 | PASS (QA7-V-05) |
| AC-SCN-06-01, -02 | dotnet `BoatMotionTests`, `SceneStateStrokeTests` | PASS |
| AC-SCN-06-03 | dotnet `CameraBoatTermsTests` (10 min, 7 tamaños); videos ≤ 2,41 dp, DEMO-05 2,87 dp | PASS |
| AC-SCN-07-01 (LT-91) | dotnet `SceneStateStrokeTests`, `PuddleFieldTests.Lt91…` | PASS |
| AC-SCN-07-02 | dotnet `PuddleFieldTests`; VID-04 | PASS |
| AC-SCN-07-03 | dotnet (alfa, radio); remolinos en VID-04 y VID-06 | PASS (QA7-V-03: CAP-04 no los muestra) |
| AC-SCN-08-01 | dotnet `RowerGeometryTests`; PlayMode `TheBoatIsAHierarchyAroundThePins`; CAP-01, -10, -11, CAP-08 (tinte de noche) | PASS |
| AC-SCN-08-02 | CAP-01, -02, -20, -21 (de frente, mono, franjas, medias, piel y pelo) | PASS |
| AC-SCN-08-03 | dotnet (plumeo, flexión); CAP-01, -02, VID-06 | PASS |
| AC-SCN-08-04 | dotnet `StrokeSprayTests`; CAP-03, VID-02 | PASS |
| AC-SCN-08-05 | dotnet `DripsFromTheExitToFiftyFiveHundredths`; VID-06 | PASS |
| AC-SCN-08-06 | dotnet `BoatShadowRuleTests`; PlayMode `TheShadowIsTheMapOrTheOvalNeverBoth`; CAP-05 a -09 | PASS |
| AC-SCN-08-07 (LT-97, -98) | PlayMode `EveryPhaseDrawsWithThreePuddles` (390 y 844, maniquí y Y Bot) | PASS |
| AC-SCN-08-08 | PlayMode `RowerSlotTests` (4 casos, un aviso), `MixamoImportTests`; CAP-20, CAP-21, DEMO-06; `[PERF] remero` en el build | PASS |
| AC-SCN-08-09 | `git ls-files Assets/ThirdParty` vacío; `git check-ignore` cubre `Mixamo/`, `Mixamo.meta`, `YBot.fbx.meta` y el *binding*; `RepoHygieneTests` (10); `ci-core` es solo dotnet | PASS |
| AC-SCN-08-10 | dotnet `BuoyTrackerTests` (5); VID-07, CAP-15, CAP-16, DEMO-05 | PASS |
| AC-SCN-08-11 | dotnet `SceneDataIsolationTests`; PlayMode `TheSessionIsTheSameWithAndWithoutTheScene` | PASS |
| AC-SCN-08-12 | dotnet (13); VID-01 contra VID-02 | PASS |
| AC-WKT-17-14 | dotnet `RowerFootprintTests`; CAP-10, -11, -14 | PASS |
| AC-WKT-18-23 | dotnet `RowerFootprintTests`; PlayMode `RowerOcclusionTests(TV|P360)` | PASS (QA7-V-02: CAP-13 no es el finish) |
| AC-SCN-01-16 `[PERF]` | §4 | **NO VERIFICABLE** (tiempos de cuadro; Android, como dice la spec). CPU, asignaciones y HUD: ok |
| AC-SCN-01-08 (cambia) | `RowerGeometryTests`; CAP-10, -11 | PASS |
| AC-SCN-02-04 (cambia) | `CameraBoatTermsTests`; videos | PASS |

Se vuelven a verificar (spec §4): las suites de la 6a siguen en verde en dotnet, EditMode y PlayMode (con y sin simulador).

## 4. Build de Windows con simulador y `[PERF]`

Cada corrida con `ROWMATE_DATA_DIR` en una carpeta temporal propia (`<temp>/RowMate.Tests/qa7-perf/data-*`, con la base sembrada por `SnapshotCli.SeedDatabase -seed F5 -prefs laps,reps` y copiada como `rower_app_dev.db`). La carpeta de datos del usuario no cambió (fechas del 3/11). El remero del build es **Y Bot** (el *binding* de la máquina).

| Corrida | Qué hace | Resultado funcional | CPU del hilo principal en régimen |
|---|---|---|---|
| `-rowmate-perf-scene 40` (dos veces, una con la ventana adelante) | libre con `Fuerte` (30 spm) | sin excepciones; 0 subidas de *shaders* después de Ready; GC 0 | p99 3,4–3,7 ms, máx ≤ 5,2 ms |
| `-rowmate-perf-scene 25 -rowmate-quality low` | ídem en Bajo | ídem | p99 3,3 ms |
| `-rowmate-perf-series 60` | `Fondo 3000` y `Series 600`, paneles abriendo y cerrando | sin excepciones | p99 3,7–5,0 ms |
| `-rowmate-perf-hours` | `Hora escena` de 18 a 23 h | sin excepciones | p99 4,1 ms |
| `-rowmate-perf-completion` | `Corta` hasta el diálogo | "fotogramas a 60 fps con el diálogo quieto" (por el contador del arnés), sin excepciones | p99 4,8–8,9 ms |

**Por qué los tiempos de cuadro quedan NO VERIFICABLE.** Todas las corridas dieron exactamente ≈ 130 cuadros por ventana de 10 s (12,8 fps) con GPU 77–80 ms, **también en Bajo** (71 dibujos en lugar de 102) y con la ventana traída al frente: el tope es externo a la app (la PC llevaba más de 2 h sin entrada; la pantalla probablemente apagada, con la presentación limitada). La medida del Architect en WP-B (GPU 4,4–5,6 ms, p99 2,9–3,2 ms) no la pude repetir. Hay que rehacer `-rowmate-perf-scene` con la pantalla encendida.

**Asignaciones.** En régimen, 23–25 cuadros por ventana con alguna asignación (máx 85–94) y GC 0: es el HUD a 1 Hz, el mismo patrón que aceptó la 6a (22–24 cuadros con ≈ 85). La escena con el remero da 0 B por cuadro en PlayMode (`FrameAllocations` con control positivo). El HUD sigue en ≤ 57 de 60 (`TheHudAddsNoElementsWithTheScene`).

**No automatizable en el build** (sin interacción con la ventana): `Suave` (20 spm) y `Pausa`. Los cubren DEMO-01, DEMO-03, VID-01 y VID-04 del arnés en el Editor. Queda para el usuario probarlos en el build.

## 5. Repositorio público y terceros

- `git grep` de rutas de la PC (carpeta de usuario de Windows en sus dos grafías, carpeta de datos local, ids del *scratch*, nombre del equipo) sobre lo versionado y lo nuevo sin ignorar: **0 coincidencias**. Las únicas apariciones de `AppData/LocalLow/iguisoft/RowMate` son la ruta genérica del producto (ya aceptada). `manifest.json` e `INDEX.md` de la 7: 0 rutas. → **limpio**.
- Terceros: `git ls-files Assets/ThirdParty` vacío; `YBot.fbx`, su `.meta`, `Mixamo.meta` y `Bootstrap/Generated/Resources/RowerCharacterBinding.asset` ignorados (`.gitignore:56`, `:76`, `:77`).
- Sin versionar y que no tiene que entrar: `Assets/New Text Document.txt` (vacío) y su `.meta` (ya anotado en el gate).
- Legacy intacto (`status --porcelain` vacío).

## 6. Issues

| ID | Sev. | Resp. | Problema | Evidencia | Corrección |
|---|---|---|---|---|---|
| QA7-V-01 | **mayor** | designer (+ po) | **AC-SCN-05-08 falla**: 21 de los 50 ángulos de rodilla, cadera, hombro y codo quedan fuera de ± 8° de la tabla de ANIMATION_BIBLE §3.4, con el maniquí y con Y Bot. El test lo esconde: `TheTenPosesOfTheTable` solo exige la lista `Checked` (19 de 50) más tronco y barrido. Se nota en la recuperación: con "manos afuera" (c 0,60) el codo está en 129° (tabla 178°), porque las manos siguen al puño del remo real | manifest CAP-17: c 0,09 rodilla 75,9 (112), cadera 32,5 (52), hombro 102,7 (78); c 0,18 rodilla 123,7 (170), cadera 61,8 (85); c 0,24 codo 132 (165); c 0,36 hombro −8,1 (−25), codo 70,8 (85); c 0,60 hombro 39 (72), codo 129,5 (178); c 0,75 rodilla 108,7 (150), codo 146,7 (178); c 0,85 rodilla 69,8 (95). `ScullingKinematicsTests.cs:245-291`; A7-01 | Por defecto, sin código: adoptar la hoja medida de A7-01 como tabla de §3.4 (el rig ya está a ≤ 1,6° de Core) y anotarla como DV en la spec; `TheTenPosesOfTheTable` pasa a exigir los 60 valores contra la tabla nueva (± 8°) y se borra `Checked` |
| QA7-V-02 | menor | architect | **CAP-12 y CAP-13 son el mismo archivo** (SHA-256 `5A2F36C9…` en los dos): las dos se tomaron con c 0,086, no con 0 y 0,36. El arnés las da por buenas | manifest: `c 0.086` en las dos; `RowerCatalog.cs:135` llama a `ForceCanonicalForTest` sin los dos `h.SceneTick()` que usan las otras tomas (`:231`, `:459`) | Sumar los dos `SceneTick` después de forzar la fase y volver a tomar CAP-12 y CAP-13 (el AC ya lo cubren `RowerFootprintTests` y `RowerOcclusionTests`) |
| QA7-V-03 | menor | architect | **CAP-04 no muestra los charcos**: la toma es ≈ 1,8 s después de la salida de la pala, no 1 s, y el par ya está fuera del cuadro de TV. El chequeo "un par de charcos a la vista" cuenta instancias, no lo que está en pantalla | manifest CAP-04: edad 1,817 s, `relZ` 1,078, x ± 2,108, `c` 0,086; se ven en VID-04 (2,0 s) y VID-06 (1,3 s) | Tomarla con la edad del par en 1,0 s (o esperar hasta que la `relZ` del par sea ≥ 4 m) y que el chequeo proyecte el charco a la pantalla |
| QA7-V-04 | menor | architect | **Texto mal codificado (UTF-8 doble)** en un repo público: `RowerCatalog.cs` (34 casos) y `SceneBudgetTests.cs` (48; en `HEAD` tenía 0), y por eso `snapshots/INDEX.md` (24) y `manifest.json` (29) de la 7 ("RecuperaciÃ³n", "maniquÃ­", "Ã³valo", "â‰¥") | `grep -c "Ã"` en los cuatro archivos | Volver a guardar los dos `.cs` en UTF-8 con el texto bien y regenerar `INDEX.md` y `manifest.json` (o corregirlos a mano) |
| QA7-V-05 | menor | architect | En una pausa, el remero empieza a respirar a los **2,47 s** de pausar, no a los ≈ 1,75 s de la spec (0,25 s de frenado + 1,5 s quieto): "quieto" cuenta desde que `ω_s` es casi 0 (≈ 1 s después de la pausa por la cola de τ 0,15). Con el catch trabado (VID-05) los 1,5 s se cumplen | VID-04: pausa 3,0 s, `φ` quieta en 3,17 s, respiración desde 5,47 s | Contar "quieto" desde `ω_s < 0,01` (o desde que `φ` deja de cambiar 1e-3 por cuadro) |
| QA7-V-06 | menor | po | Con Y Bot el remero mide **1,786 m** y AC-SCN-05-06 pide "remero de 1,80 a 1,88 m". El slot valida 1,55–2,05 m, así que es coherente con AC-SCN-08-08 | `[PERF] remero (n.º 1): slot …/YBot.fbx, 55320 triángulos, 1.786 m`; B7-02 | Aclarar en AC-SCN-05-06 que 1,80–1,88 m es el maniquí (un personaje del slot vale con su altura dentro de 1,55–2,05) |
| QA7-V-07 | menor | architect | El campo `eventos` del `manifest` pierde eventos: VID-01 no tiene ningún `BladeExit`, VID-02 ningún `BladeEntry`, VID-03 no tiene el `Catch` de los 5,4 s (sale uno solo por cuadro de video) | manifest VID-01 a -03 | Acumular las banderas de los `Tick` entre dos cuadros de video |

## 7. Lo que queda NO VERIFICABLE y quién lo cierra

- **AC-SCN-01-16, tiempos de cuadro en Windows**: rehacer `-rowmate-perf-scene 300` en `Builds/Windows/simulator` con la pantalla encendida (y la misma corrida con `-rowmate-quality medium`). Si da p99 ≤ 16,7 ms y ningún cuadro > 33 ms en régimen, pasa a PASS.
- **AC-SCN-01-16, Android**: sin teléfono (regla 9, It. 14).
- **Prueba a mano en el build** (`Suave`, `Pausa`, `Reanudar`, `Hora escena` desde el panel): el usuario.

## 8. Para issues

- La cámara lateral de prueba ve el cielo partido en dos tonos (CAP-17, CAP-18, VID-06; ya en "Para issues" de WP-B).
- La hoja de CAP-17/-18 usa una imagen de 2400 × 1080 con el 60 % en negro: armarla en 5 × 2 sobre todo el alto.
- En TV y V390 el remero se ve muy chico (CAP-01, CAP-19 contra LEG-01): para el look EXR, evaluar acercar la cámara del contrato o un plano más cerrado (backlog de cámaras, Q-66).
- En régimen el HUD sigue asignando en 23–25 cuadros por ventana de 10 s (máx 94, antes ≈ 85): mirar si la 7 sumó algo al patrón de 1 Hz de la 6a.
- La popa del casco cae debajo de la zona del bote en TH, L640, W1280 y H844 (A7-08, desde la 6a; se ve en CAP-14).
- En CAP-12 el panel `Series anteriores` toca la esquina de arriba a la izquierda de la zona (x 115–159 dp, y 324–378 dp), sobre el horizonte y lejos del remero: confirmar contra AC-WKT-18-14.
- `Assets/New Text Document.txt` (vacío) sin versionar: borrarlo antes del commit.

---

## Ronda 2 — re-verificación de QA7-V-01

### Veredicto: **APROBADO CON OBSERVACIONES** — 37 PASS · 0 FAIL · 1 NO VERIFICABLE (de 38), 0 mayores

QA7-V-01 quedó corregido como se sugirió (sin código de la app): la tabla de 10 poses de ANIMATION_BIBLE §3.4 ahora es la hoja medida de A7-01, con una columna propia para Y Bot (DV-78). AC-SCN-05-08 la exige entera ("los 60 valores sin excepciones") y `TheTenPosesOfTheTable` también: se borró `Checked`, el test compara los 60 valores (tronco, rodilla, cadera, hombro, codo y barrido) a ± 8° con cada cuerpo y además exige `report.Count == 60`. Desde la ronda 1 solo cambiaron el test y los documentos (es lo único posterior al build de las 22:45). Las suites siguen en verde y las capturas nuevas CAP-17 y CAP-18 cumplen la tabla. Queda abierto lo mismo de la ronda 1: los tiempos de cuadro de `[PERF]` (por el entorno) y los menores QA7-V-02 a -07, que esta ronda no trabaja y siguen igual. Hay un menor nuevo: QA7-V-R2-01.

### R2.1 Suites (salida real)

| Suite | Comando | Resultado |
|---|---|---|
| dotnet (.NET 8) | `tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it7/qa-r2` | Build `0 Warning(s)`, `0 Error(s)`. **192/192** (herramientas) + **3191: 3152 pasan, 39 omitidos, 0 fallan** (uno más que en la ronda 1, porque `TheTenPosesOfTheTable` ahora tiene dos casos). Matriz: PASA 656, FALLA 0, IGNORADO 4, PENDIENTE UNITY 149, SIN TEST 0. `AC-SCN-05-08 \| PASA \| 4` (`TheTenPosesOfTheTable("maniqui")`, `("ybot")`, `TheHeadLooksAstern`, `TheTrunkGoesFurtherBackWithEffort`) |
| Unity EditMode | `tools/unity/run-unity-tests.ps1 -Platform EditMode` | **3290: 3245 pasan, 45 ignorados, 0 fallan**, exit 0 |
| Unity EditMode + simulador | `... -Platform EditMode -Simulator` | **3482: 3437 pasan, 45 ignorados, 0 fallan**, exit 0 |
| Unity PlayMode | `... -Platform PlayMode` | **500/500**, exit 0 (`RowerRigTests.TheRigFollowsCoreInTheTenPoses(Mannequin\|Slot)` Passed) |
| Unity PlayMode + simulador | `... -Platform PlayMode -Simulator` | **573/573**, exit 0 |
| Guardia de `ProjectSettings` | las cuatro corridas, el build y la siembra | "sin cambios" (`git diff --stat ProjectSettings` igual antes y después: solo la diferencia de la 7 en `RowMateBootstrap.json`, que ya estaba) |
| Build | `tools/build/build.ps1 -Target Windows -Simulator` | `Build Finished, Result: Success.`, Development, `THIRD_PARTY_NOTICES.md` al lado |

### R2.2 Capturas (miradas) contra la tabla nueva

CAP-17 y CAP-18 (sueltas y en hoja) se regeneraron después de la corrección. Comparé los 120 valores del `manifest.json` (10 poses × tronco, rodilla, cadera, hombro, codo y barrido × 2 cuerpos), medidos en el rig de Unity, contra la tabla nueva:

| Cuerpo | Mayor diferencia con la tabla | Fuera de ± 8° | Rig contra Core | Manos contra el puño |
|---|---|---|---|---|
| Maniquí (CAP-17) | 2,4° (tronco del finish −20,4 contra −18, por el esfuerzo de la toma) | **0 de 60** | ≤ 0,02° | ≤ 1,46 cm |
| Y Bot (CAP-18) | 3,0° (tronco del catch 31,0 contra 28) | **0 de 60** | ≤ 1,66° | ≤ 1,48 cm |

Ejemplos de lo que fallaba en la ronda 1 y ahora cierra: c 0,09, rodilla 75,9 (tabla 76; antes 112); c 0,60, codo 129,5 (128; antes 178); con Y Bot, c 0,36, hombro −41,2 (−43) y codo 54,7 (54). Las columnas de ANIMATION_BIBLE §3.4 y las de `ScullingKinematicsTests` (`Table`, `YBotTable`) coinciden valor por valor. En la hoja, las 10 poses se leen bien y en orden, y el maniquí y Y Bot hacen lo mismo. Se sigue viendo lo que ya está aceptado y anotado: en "manos afuera" (CAP-17-07) los brazos todavía están doblados (≈ 128°), porque las manos siguen al puño del remo real (04-design-notes, "Para issues").

Los videos son los de la ronda 1 (22:23, antes de la corrección) y la corrección no toca el código que los genera, así que lo que se miró en la ronda 1 sigue valiendo. No extraje cuadros nuevos (no hay `ffmpeg` en la máquina).

### R2.3 Matriz: cambios respecto de la ronda 1

| AC | Evidencia | Ronda 1 | Ronda 2 |
|---|---|---|---|
| AC-SCN-05-08 | dotnet y EditMode `TheTenPosesOfTheTable("maniqui"\|"ybot")` (60 valores cada uno, ± 8°); PlayMode `TheRigFollowsCoreInTheTenPoses`; manifest CAP-17 y CAP-18 (120 valores, 0 fuera) | FAIL | **PASS** |
| AC-SCN-01-16 `[PERF]` | R2.4 | NO VERIFICABLE | **NO VERIFICABLE** (sin cambios) |

Los otros 36 AC siguen en PASS: mismas suites en verde y ningún cambio en el código de la app.

### R2.4 Build de Windows con simulador y `[PERF]`

Cada corrida tuvo su propio `ROWMATE_DATA_DIR` en `<scratch>/it7/qa-r2/perf/data-*` y `-logFile` en la misma carpeta. La base se sembró con `SnapshotCli.SeedDatabase -seed F5 -prefs laps,reps` como `seed.db` y se copió como `rower_app_dev.db`. Remero: **Y Bot** (`[PERF] remero (n.º 1): slot …/YBot.fbx, 55320 triángulos, 1.786 m`). En las cinco corridas hubo 0 excepciones y 0 *shaders* subidos después de Ready.

| Corrida | Qué hace | Salida | CPU del hilo principal en régimen |
|---|---|---|---|
| `-rowmate-perf-scene 40` | libre con `Fuerte` (30 spm), Alto | 0 | p99 3,8–3,9 ms, máx 4,7 ms; asignaciones: máx 91–94 en 21 cuadros; GC 0 |
| `-rowmate-perf-scene 30 -rowmate-quality medium` | ídem, en Medio (79 dibujos) | 0 | p99 3,4 ms |
| `-rowmate-perf-hours` | `Hora escena` hasta las 23 h | 0 | p99 3,9 ms |
| `-rowmate-perf-series 60` | `Fondo 3000` y `Series 600`, abriendo y cerrando los paneles | 0 | p99 3,8–4,4 ms |
| `-rowmate-perf-completion` | `Corta` hasta el final (diálogo) | 0 | "fotogramas a 60 fps con el diálogo quieto"; p99 4,4–8,8 ms |

**Los tiempos de cuadro siguen NO VERIFICABLE**, por la misma causa que en la ronda 1: todas las ventanas dan 129–130 cuadros cada 10 s (≈ 13 fps), con GPU de 77–79 ms, también en Medio. El tope está afuera de la app (de noche, sin nadie usando la PC y con la pantalla probablemente apagada). Lo cierra la misma corrida de la §7, con la pantalla encendida. Hay un pico de entrada (23 ms en los primeros 30 cuadros de la primera ventana) que es el mismo de la 6a y no viene de esta corrección.

`Suave` (20 spm) y `Pausa` no se pueden automatizar en el build (no hay forma de interactuar con la ventana). Los cubren los DEMO y VID del arnés (ronda 1) y queda para que el usuario los pruebe a mano en el build.

**Datos del usuario.** La carpeta de datos del usuario no cambió (archivos con fecha del 3/11). Correr el *player* actualiza el contador de sesiones de Unity en sus preferencias del registro de Windows, igual que en la ronda 1. No son datos de la app.

### R2.5 Repositorio público y terceros

- `git grep` de la carpeta de usuario de Windows (en sus dos grafías), `AppData/Local/Temp`, los ids del *scratch*, el nombre del equipo y el nombre del usuario, sobre lo versionado y lo nuevo sin ignorar: 0 coincidencias, salvo el nombre del autor en `LICENSE` (el copyright, que tiene que estar). En 05-qa-verification (esta sección) no hay rutas de la PC. → **limpio**.
- Terceros: `git ls-files Assets/ThirdParty` sigue vacío, y `YBot.fbx` y su `.meta` quedan ignorados por `.gitignore:76`.
- Legacy en `3ae0f3a`, con `status --porcelain` vacío.
- `Assets/InitTestScene5d70e55b-….unity` es del 30/10 y lo ignora `.gitignore:111` (no es de esta ronda).

### R2.6 Issues

| ID | Sev. | Resp. | Problema | Evidencia | Corrección |
|---|---|---|---|---|---|
| QA7-V-01 | ~~mayor~~ **cerrado** | designer (+ po) | — | R2.1 a R2.3 | — |
| QA7-V-R2-01 | menor | architect | **`snapshots/INDEX.md` quedó a medias**: la corrida parcial de CAP-17 y CAP-18 lo reescribió con esas 22 tomas nada más ("el manifest conserva las demás tomas"). Faltan CAP-01 a -16, CAP-19 a -21, DEMO-01 a -06 y VID-01 a -07, que sí están en `manifest.json` (55 tomas) | `INDEX.md` (22 filas) contra `manifest.json` (55 `shots`) | Que una corrida parcial regenere `INDEX.md` a partir del `manifest.json` completo (o correr el set completo antes del commit) |

Siguen abiertos, sin cambios y fuera de esta ronda: QA7-V-02 (CAP-12 y CAP-13 siguen siendo el mismo archivo: SHA-256 `5a2f36c9…`), QA7-V-03, QA7-V-04 (texto mal codificado: `RowerCatalog.cs` 34, `SceneBudgetTests.cs` 48, `INDEX.md` 12, `manifest.json` 28), QA7-V-05, -06 y -07.

### R2.7 Para issues

- La tabla de §3.4 sale de lo que da Core (es la hoja medida). Desde ahora el test protege contra regresiones de más de 8°, pero no valida la técnica de remo. La recuperación con los brazos doblados (codo ≈ 128° en "manos afuera") ya está anotada en 04-design-notes.
- Un personaje Humanoid que no sea Y Bot no tiene columna en §3.4. El slot lo acepta (1,55–2,05 m) y el rig sigue a Core, pero ninguna tabla controla sus ángulos. Si se suman personajes, generar su columna con la hoja de A7-01.
