# It. 6a — Verificación QA (principal)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa (verificación principal; el único agente de la verificación que usó Unity) |
| Fecha | 2026-11-03 |
| Iteración | 6a · `06a-escena-lago` |
| Entrada | [01-po-spec](01-po-spec.md) (101 ID, con "Cambios por gate QA"), [02-tech-design](02-tech-design.md) (con "Cambios por gate QA" y "por implementación" de WP-A y WP-B), [04-design-notes](04-design-notes.md) (con "Cambios por gate QA" y "por implementación" de WP-D1 y WP-D2), [00-deuda-5](00-deuda-5.md), [00-deuda-diseno](00-deuda-diseno.md), [03-qa-harmony-paridad](03-qa-harmony-paridad.md), [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) en `3ae0f3a`, solo lectura (`docs/image-03.png`; el oráculo `-Scene` lo lee dentro de su guardia). `git -C ../rowmate-legacy status --porcelain` vacío al empezar, después del oráculo y al terminar. No se leyó `lib/core/strava/strava_secrets.dart` |
| Artefactos | `<scratch>/it6a/qa-main-r1/` (resultados de dotnet y de Unity, registros, corrida A del arnés y las de `it4b` e `it5`, hojas de cuadros de los videos, corridas `[PERF]` y la prueba a mano en el build, con sus capturas) |
| Repo | Sin *commits*. Lo único que esta verificación escribió en el repo es este documento y el build de `Builds/Windows/simulator` (ignorado por git). El oráculo reescribió los seis `scene_*.json` con otro `generatedAt` y se devolvieron a sus bytes de antes (§2.3) |

---

## 0. Veredicto

> **Ronda 3 (manda sobre esta sección y sobre la ronda 2): APROBADO CON OBSERVACIONES**, 98 PASS · 2 FAIL · 1 NO VERIFICABLE; QA-V6a-R2-01 a -05 cerrados (R2-04 y R2-05 con su deuda); 1 mayor nuevo (QA-V6a-R3-01, la entrada con la precarga sin terminar: AC-WKT-13-15) y QA-V6a-PAR-01 de la lente de paridad, que seguía abierto (AC-SCN-13-03). Ver [Ronda 3](#ronda-3) al final.
>
> **Ronda 2: APROBADO CON OBSERVACIONES**, 100 PASS · 0 FAIL · 1 NO VERIFICABLE; los mayores de la ronda 1 y QA-V6a-REN-01 a -10, cerrados. Ver [Ronda 2](#ronda-2) al final.

**Ronda 1: APROBADO CON OBSERVACIONES — con ronda 2 obligatoria.** 0 bloqueantes, **3 mayores** y 10 menores (§8). Los tres mayores dejan **4 criterios en FAIL** (AC-SCN-01-07, AC-APP-05-08 en su parte de la escena demorada, AC-WKT-17-07 y AC-SCN-02-07) y se corrigen y se vuelven a verificar antes del REPORT. Lo demás (la lógica, la escena, la paridad del paisaje, la hora, el rendimiento en la PC, el determinismo y el aislamiento de los datos) está bien y medido.

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix`) | Compilación **0 avisos, 0 errores** (también `RowMate.Tests.UnityApiCheck`) · herramientas **192 / 192** · Core **3045: 3006 pasan, 0 fallan, 39 ignoradas** (las de equipo de `WorkoutDeviceChecksTests` y las de antes) |
| Unity EditMode | **3130: 3085 pasan, 0 fallan, 45 ignoradas**, 0 inconclusas; guardia de `ProjectSettings`: sin cambios |
| Unity EditMode con simulador | **3322: 3277 pasan, 0 fallan, 45 ignoradas**, 0 inconclusas; el *define* restaurado; guardia: sin cambios |
| Unity PlayMode | **468 / 468**; guardia: sin cambios |
| Unity PlayMode con simulador | **540 / 540**; el *define* restaurado; guardia: sin cambios |
| Matriz (`-UnityResults` con los 4 XML) | 762 IDs: **PASA 758, FALLA 0, IGNORADO 4** (AC-APP-15-03 [Apple], AC-WKT-02-10 [Android], AC-SCN-01-09 [Android], AC-SCN-01-10 [equipo, medido acá a mano]), PENDIENTE UNITY 0, SIN TEST 0. Los 101 ID de la 6a y LT-62 a LT-87: 125 PASA y los 2 de equipo |
| Arnés `it6a` | **45 tomas, 0 con problemas, 0 sondas de aspecto que fallan**. La corrida A de QA es **igual byte a byte** a la publicada (34 PNG, 11 MP4 e `INDEX.md`); `manifest.json`, solo `durationMs` (45 líneas): dos corridas iguales, sin necesitar la tolerancia de 1 nivel |
| Oráculo `-Scene` | Las dos pruebas de Flutter en verde; **LEG-01 a -07 (10 PNG) iguales byte a byte**; los seis `scene_*.json` iguales salvo `generatedAt`; la guardia del legacy OK antes y después |
| `it4b` / `it5` | Corridas de QA iguales byte a byte a lo que hay en el árbol (52 y 48 archivos; `manifest.json`, solo `durationMs`). Contra `HEAD` cambian **solo** en la franja de la fila de píldoras (los tokens `.hud`, §2.5) |
| `it2` / `it3` / `it4a` | Sin cambios contra `HEAD` (no aparecen en `git status`) |
| Build de Windows con simulador | Generado (`tools/build/build.ps1 -Target Windows -Simulator`), con `THIRD_PARTY_NOTICES.md` al lado, igual al del repo. `[PERF]`: **libre con `Fuerte` 5 min: p99 ≤ 3,4 ms, máx ≤ 6,2 ms después de la entrada, GPU ≤ 5,7 ms, ≤ 131 MB**; entradas por el camino del usuario ≤ 16,1 ms; 0 compilaciones de *shaders* de la escena después de `Ready` (§4) |
| Prueba a mano en el build | Hecha con entrada sintética y una carpeta de datos temporal: entrada con el vuelo, `Fuerte`, `Hora escena` a las 15, 18, 22, 6 y 12 h y `Hora real`, pausa con el velo, `Corta` hasta el diálogo y la toma aérea, salidas, matar el proceso a mitad y volver a abrir (§4.3). Todo bien salvo lo de QA-V6a-03 |
| **Criterios** (101) | **96 PASS · 4 FAIL (mayores) · 1 NO VERIFICABLE** (AC-SCN-01-09, Android). Varias partes de equipo Android quedan NO VERIFICABLE dentro de criterios que pasan en la PC (§5) |
| LT-62 a LT-87 | 26 de 26 portados con el mismo nombre y en verde: 17 en dotnet y Unity EditMode, 9 en PlayMode (§6) |
| Repo público | `git grep -nIE` de rutas de usuario de Windows, de Git Bash y del prefijo de las carpetas de trabajo sobre lo versionado, lo modificado y lo nuevo sin seguimiento: **0**. Sin datos del equipo, del hardware ni del usuario en los documentos, `manifest.json`, `INDEX.md` ni en los bytes de los PNG y MP4 (§7) |

---

## 1. Corridas

Desde la raíz del repo, con el Editor cerrado y una corrida por vez:

| Comando | Resultado |
|---|---|
| `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it6a/qa-main-r1` | `Build succeeded. 0 Warning(s) 0 Error(s)`; `Total tests: 192 Passed: 192`; `Total tests: 3045 Passed: 3006 Skipped: 39`; matriz sola: PASA 615, FALLA 0, IGNORADO 4, PENDIENTE UNITY 134, SIN TEST 9 (LT-78 a -81 y LT-83 a -87, QA-V6a-11) |
| `tools/unity/run-unity-tests.ps1 -Platform EditMode` | `total 3130, pasan 3085, fallan 0, ignorados 45, inconclusos 0` |
| `… -Platform EditMode -Simulator` | `total 3322, pasan 3277, fallan 0, ignorados 45, inconclusos 0` |
| `… -Platform PlayMode` | `total 468, pasan 468, fallan 0` |
| `… -Platform PlayMode -Simulator` | `total 540, pasan 540, fallan 0` |
| `tools/dotnet/run-tests.ps1 -UnityResults <los 4 XML>` | `PASA 758, FALLA 0, IGNORADO 4, PENDIENTE UNITY 0, PENDIENTE WP-B 0, SIN TEST 0` |

Los números son los mismos que dejó WP-D2 (04-design-notes, "Cambios por implementación de WP-D2" §9). Las 39 ignoradas de dotnet son las de equipo (ahora con `AndroidSceneSixtyFps`, `WindowsScenePerf`, `WindowsSceneHourDoesNotSurviveRestart`, `WindowsThreeSessionsCleanLog`, `WindowsGpuIdleInWorkout`) y `ProjectOpensWithoutCompileErrors` (corre en Unity). La compilación de Unity deja los avisos de antes de `Infrastructure` y dos pruebas viejas, **más tres CS8625 nuevos de la escena** (`Scene/SceneLighting.cs:190`, `Scene/SceneRenderer.cs:78` y `:90`; D2-08, QA-V6a-10).

Medidas que dejan las pruebas en el registro de PlayMode (las que importan para la 6a):

- `[SBT]` por nivel, E-920, a las 12 y 22 h: **Alto 89-90 dibujos / 60-61 SetPass / 215 k triángulos; Medio 69-70 / 45-46 / 155 k; Bajo 56-57 / 32-33 / 106 k**; la UI sola, aparte. Dentro de ARCHITECTURE §20.1 con el tope de SetPass de Alto en 80 que confirmó el Designer en WP-D2.
- `[SBT] 0 B` remando, en pausa, en la transición 12 → 18 h y de noche: `0 asignaciones … escena 0 en 120 Tick, URP 0 en 120 dibujos`; el control positivo (un `byte[64]` en el `Tick`) se afirma en la misma prueba (`SceneBudgetTests.NoAllocationsPerFrame`). Ver QA-V6a-07: esa medida no observa el repintado del HUD con la escena (`0 en 0 dibujos`).
- `[SLOT]`: dos líneas, las del slot de prueba (`terrain.layer.grass` asignado y faltante → el procedural con un aviso), que es lo que pide AC-SCN-15-08. Ningún `[SLOT]` fuera de esa prueba.

---

## 2. Arnés, oráculo y capturas de iteraciones anteriores

### 2.1 Conjunto `it6a`

`tools/unity/capture-snapshots.ps1 -Set it6a -Out <scratch>/…/capA`: 45 tomas, 0 con problemas, 0 sondas de aspecto que fallan, en 1 min 40 s; la guardia de `ProjectSettings`, sin cambios; el *define* restaurado. **Los 46 archivos (34 PNG, 11 MP4 e `INDEX.md`) son iguales byte a byte** a los publicados en `snapshots/`, y `manifest.json` difiere solo en las 45 líneas de `durationMs`. Con la corrida publicada (la A de WP-D2) son dos corridas completas iguales: no hizo falta justificar ninguna toma con la tolerancia de 1 nivel por canal.

Lo que el `manifest.json` afirma y QA leyó: el astro con la cámara real a ≤ 2,23 dp de lo brillante cuando está a la vista (en CAP-08, con el sol detrás del paisaje, lo brillante es el resplandor sobre la cresta, a 10,9 dp); 80 estrellas con alfa > 0 a las 6, 18, 19, 20, 22 y 0 h y 0 a las 12 y 15 h; las 7 nubes en el punto del legacy (error 0 dp); **las marcas del legacy a 0 dp del ancla 3D proyectada** (98 objetos en CAP-02 y CAP-05, 92 en CAP-07; tope 2); el contrato del cielo de CAP-02 (`#0B4FA8` y `#2C70BE`, diferencia 0); 0 píxeles distintos en dos dibujos seguidos en CAP-12 (42 195 m) y en `it6a-low`; el brillo del sol de `it6a-brillo` en **30,48 dp** (mínimo 30); la distancia de escena de CAP-13 a -17 en 234,849 m con la banda en `1240` `m`; el remero en el centro de la zona del bote; el velo en 1 y 1,05 m/s en CAP-20 y -21; CAP-22 en modo aéreo, cabeceo −4°, respiración 0 y la popa en 553,2 dp; el fondo provisorio de CAP-24 (`#1E61BA` / `#145395`).

Valores por cuadro de los videos, recorridos por QA: el apartamiento del horizonte con la cámara del contrato llega a **2,932 dp en VID-03 (W1280)**, 1,779 en VID-04, 1,271 en VID-09 y 1,25 en VID-10 (tope 3); VID-04: el velo en 1 a los 20,3 s, el bote se desliza **5,92 m** y queda quieto a los **8,97 s**, el velo vuelve a 0 en 240 ms; VID-05: la visibilidad del sol baja de 0,757 (18:50) a 0,12 (19:19) y la de la luna sube de 0,165 (19:50) a 1 (20:50), las estrellas entran a las 18 h con el fundido (0,603 a mitad); VID-07: modo `Exit` desde el primer cuadro; VID-08: modo `Aerial` desde los 30,333 s y quieta desde los 36,333 s, velocidad 3,2 → 0,05 m/s.

### 2.2 Las capturas, una por una (QA las miró todas)

| Toma | Lo que se ve | Contra la spec y LEG |
|---|---|---|
| CAP-01 / LEG-01 (6 h) | Cielo violeta y naranja, estrellas tenues, el macizo de la izquierda iluminado por detrás, bruma, la cabaña a la derecha con la ventana apagada | El mismo tono dominante que LEG-01. Montañas más bajas en vertical (DV-65) y lavadas |
| CAP-02 / LEG-02 (12 h, marcas) | Cielo `#0B4FA8` → `#8FD3FF`, el sol alto y chico con resplandor, 7 nubes, nieve en el macizo y el pico central, cada pino, el muelle y la cabaña sobre su marca naranja | Paridad del paisaje bien: las x coinciden (cabaña ≈ 318 contra ≈ 321 de LEG; marcas a 0 dp). Las bases quedan más arriba por el terreno, dentro del margen |
| CAP-03 / LEG-03 (18 h) | Atardecer rojo y naranja, estrellas apenas | El sol queda detrás de la cabaña (bien, AC-SCN-10-04), pero el "contraluz con el halo" no se lee: el halo `sunGlow` casi no se distingue del horizonte (QA-V6a-12) |
| CAP-04 / LEG-04 (22 h) | Luna a la izquierda del centro, 80 estrellas, ventana encendida, líneas plateadas, nieve brillando con la luna | El disco mide ≈ 6 % menos de ancho que de alto (la gibosa al 94 %, AC-SCN-10-07); el terminador no se distingue a este tamaño. Nubes grises visibles, como en LEG-04 |
| CAP-05 / LEG-05 (H844, marcas) | Orilla con pasto, pinos y la cabaña sobre sus marcas; ladera con bosque | Las montañas del legacy (la silueta dominante de LEG-05) quedan casi todas tapadas por la ladera cercana: se ven dos manchas de nieve (QA-V6a-12) |
| CAP-06 / LEG-06, CAP-07 / LEG-07 | La noche en horizontal; el arranque a 0 m con las marcas | Bien. En CAP-07 las nubes están donde el legacy las pone a 0 m (DV-56: no se corren con la distancia) |
| CAP-08, CAP-09 (TH, 19 y 20 h) | El sol escondido con su resplandor rosado; la luna detrás del paisaje (el halo no asoma); ventana apagada a las 19 y encendida a las 20; estrellas | Bien (líneas × 0,536 y × 0,376 en el *manifest*) |
| CAP-10, CAP-11 (TV, 0 y 15 h) | Luna centrada y alta con las estrellas; a las 15 h sin estrellas, sol a la derecha, sombra del casco a la izquierda | Bien |
| CAP-12 (W1280, 42 195 m) | El lago igual que a 0 m; las montañas un poco más grandes (se acercan al 4 %) | Bien (sin temblor) |
| CAP-13 a CAP-17 | HUD de la 4b sobre el lago a las 10, degradés de pantalla detrás, el casco dentro del contorno | El casco medido en los PNG: dentro de la zona en los cinco tamaños (en TH su punta queda a 2,2 dp del borde de abajo) |
| CAP-18 a CAP-21, CAP-23 | Paneles con el atardecer; píldoras con la nube detrás (el vidrio opaco al 86 % no la deja ver); el velo (menos color y más oscuro, `PAUSA` en su disco, HUD a pleno); la noche en W1280 | Bien. CAP-19: las píldoras hechas sobre `#071222` (≫ 3:1) |
| CAP-22 (TV) | El diálogo de completado opaco **tapa el bote entero**; alrededor, el lago más alto y más oscuro por el velo | Ver QA-V6a-03 (AC-SCN-02-07). Además, la escena de esa toma quedó a 4,5 m y quieta (QA-V6a-04) |
| CAP-24 | El fondo provisorio teñido por las 10 h con el HUD llegando | Bien |
| DEMO-01 a -08 | Workout; la entrada; `Fuerte`; 18, 19-20, 22 h; pausa de noche; la salida | DEMO-03 (39,2 s) **no muestra el muelle**: ya salió del cuadro (QA-V6a-05). El reloj de pared en la hora real en todas |
| `it6a-low`, `it6a-brillo` | Bajo, sin temblor; el brillo del sol | Bien |

Contra `docs/image-03.png` del legacy (el río, con remero): el HUD es el de la 4b y la vista del casco desde atrás (una franja clara que se angosta hacia adelante) es la misma geometría que el legacy; el casco provisorio de la 6a no tiene remero ni remos (DV-61).

### 2.3 Videos, cuadro por cuadro (hojas en `<scratch>/it6a/qa-main-r1/sheets/`)

| Video | Lo que se vio | |
|---|---|---|
| VID-01 | Negro hasta los 200 ms; la escena aparece con la cámara alta, al costado y atrás, y baja como un dron mientras entra el HUD; a los 1,6-1,8 s, en la pose | Bien |
| VID-02 | El fundido de 120 ms con la cámara en su lugar | Bien |
| VID-03 | Pinos que vienen y salen por los costados; el muelle con sus pilotes se ve desde los 34 s hasta que sale por la izquierda a los ≈ 38,2 s; montañas quietas; líneas que avanzan | Bien |
| VID-04 | El velo en 300 ms, el bote que se desliza y para, el velo que se va al reanudar | Bien |
| VID-05, VID-06 | Cada cambio de hora en 1,5 s; el sol baja detrás de la cabaña y del paisaje; las estrellas con su fundido; la luna sale por la izquierda; la ventana se enciende y se apaga; el amanecer | Bien |
| VID-07 | El HUD se va; la escena funde a negro en 200-400; Workout desde los 400 | Bien |
| VID-08 | A los 30,3 s el diálogo; desde ≈ 31,5 s **el diálogo y su velo tapan el bote**: la toma aérea solo se adivina en los bordes | QA-V6a-03 |
| VID-09 | La escena se rehace en el cuadro 15 sin reiniciarse; **el HUD aparece entero en el primer cuadro de TH** (cuadros 15 y 16 iguales en opacidad): no hay fundido de 200 ms | QA-V6a-02 |
| VID-10 | La respiración: un vaivén lento, dentro de ± 3 dp | Bien |
| VID-11 | `scene-surface` sube de 0 a 1 entre los 1,533 y los 2,1 s (*manifest*), pero **en la imagen el fondo provisorio queda encima hasta los 2,1 s y la escena aparece de golpe en el cuadro 64** | QA-V6a-01 |

### 2.4 El oráculo (`tools/oracle/run-oracle.ps1 -Scene`)

Se resguardaron los seis `scene_*.json` y los diez PNG de `snapshots/legacy/`, se corrió el oráculo (las dos pruebas de Flutter en verde, `pub get --offline`, la guardia del legacy: "sin cambios (porcelain vacío, --ignored y metadatos iguales)") y se comparó: **los diez PNG iguales byte a byte**, los seis JSON sin ninguna línea distinta salvo `generatedAt`. Los JSON se devolvieron a sus bytes de antes (los seis comprobados con `cmp`).

### 2.5 Las capturas de iteraciones anteriores

- `it2`, `it3`, `it4a`: no figuran en `git status` (sin cambios contra `HEAD`).
- `it4b`: 31 PNG, 3 MP4 y `manifest.json` cambiados contra `HEAD`. QA comparó cada PNG de `HEAD` (con `git lfs`) contra el del árbol: **todos los píxeles distintos caen en la franja de la fila de píldoras** (en TV y 1080 × 2400, y 47-69; con la cuenta grande y su halo, y 28-78), con pares de color de los tokens nuevos (`#CD4065` → `#DA5272`, `#EF476F` → `#FF5D7F`, `#321F37` → `#35233A`, `#118AB2` → `#37A2CB`). El `manifest.json` cambia en las sondas de píldoras (`step.work` → `step.work.hud`, todas en tolerancia) y en `durationMs`. Motivo: los tokens `.hud` de AC-WKT-17-13 (B6a-21).
- `it5`: 44 PNG/MP4 cambiados; los 35 PNG, **solo en la franja de la fila de píldoras**; `manifest.json`, solo `durationMs`. Mismo motivo.
- Corridas de QA de `-Set it5` y `-Set it4b` en `<scratch>`: 47 y 51 tomas sin problemas, **iguales byte a byte** a lo que hay en el árbol (salvo `durationMs`).

---

## 3. La deuda de la It. 5 (§5.1 de la spec)

Hecha al empezar la 6a ([00-deuda-5](00-deuda-5.md)): AC-WKT-13-15 y AC-WKT-18-21 pasaron en el build de la 5 y la prueba a mano de la 5 dio bien. AC-WKT-02-11 sigue esperando la demo (c) del usuario (suspender el equipo a mano). QA-V5-R2-01 lo cerró el Designer ([00-deuda-diseno](00-deuda-diseno.md) §1).

---

## 4. Build de Windows con simulador

`tools/build/build.ps1 -Target Windows -Simulator` → `Builds/Windows/simulator/RowMate.exe` (Mono, Development), `[BUILD] THIRD_PARTY_NOTICES.md junto al ejecutable` (igual byte a byte al del repo), guardia de `ProjectSettings` sin cambios. Ningún `RowMate.exe` estaba abierto. Todas las corridas con `ROWMATE_DATA_DIR` en una carpeta propia de `<scratch>` con una copia de la base sembrada con `SnapshotCli.SeedDatabase -seed F5 -prefs laps,reps` (como `seed.db`, copiada a `rower_app_dev.db`), ventana de 1280 × 720. Las preferencias de la ventana del *player* (registro de Windows) se resguardaron antes y se restauraron al final, iguales al resguardo; la carpeta de datos del usuario quedó igual (nombre, tamaño, fecha y *hash* de cada archivo).

### 4.1 `[PERF]` (CPU del hilo principal; GPU de `FrameTimingManager`)

| Corrida | Entrada (2,5 s) | Ventanas de 10 s después de la entrada | Salida (0-1 s) | Otros |
|---|---|---|---|---|
| `-rowmate-perf-scene 300` (libre, `Fuerte`, Alto) | **16,1 ms** CPU, 5,7 GPU | máx 3,3-6,2 ms, **p99 2,7-3,4 ms**, GPU ≤ 4,8 ms, 109-110 MB (30 ventanas) | 10,7 ms | 0 compilaciones de la escena después de `Ready`; resolución 1,00 todo el tiempo |
| `-rowmate-perf-entries 10 -rowmate-perf-routines "Prueba 4a;Cinco reps"` (paneles abiertos) | 1.ª (libre) **12,3 ms**; 2.ª a 10.ª **7,0-10,0 ms** | paneles: máx ≤ 10,0 ms, p99 2,7-3,3 ms, ≤ 131 MB | 4,3-7,5 ms | 0 compilaciones en las diez |
| `-rowmate-perf-entries 3 -rowmate-scene-delay 1.5` | **15,1 / 11,4 / 9,8 ms** | — | 4,9-7,6 ms | La escena demorada llega en las tres; peor cuadro de su preparación 9,2-9,8 ms |
| `-rowmate-perf-hours` (dos corridas) | **22,0 / 22,5 ms** | máx 3,7 ms, p99 2,6-2,7 ms, GPU ≤ 6,1 ms | 9,1 / 7,5 ms | La hora se cambia en el mismo cuadro del toque (QA-V6a-06) |
| `-rowmate-perf-completion` (`Corta`, diálogo y toma aérea) | **22,8 ms** | máx 9,1 / 3,7 / 11,0 ms, p99 ≤ 3,3 ms | 9,6 ms | `[PERF] completado: fotogramas a 60 fps con el diálogo quieto`. `StartRoutine` directo (QA-V6a-06) |
| `-rowmate-perf-series 60` (`Fondo 3000`, `Series 600`, paneles cada 2 s) | 1.ª **24,9 ms**, 2.ª 8,4 ms | máx ≤ 8,4 ms, p99 2,8-3,2 ms | 8,3 / 5,5 ms | El artefacto de `StartRoutine` directo ya visto en la 5 (00-deuda-5 §1.2) |
| Prueba a mano (camino del usuario, §4.3) | 1.ª libre **13,0 ms**; `Corta` desde la lista con 18 h **9,6 ms**; 1.ª del proceso con 17 h puesta en Workout **11,9 ms** | — | 8,2 / 6,3 / 8,9 ms | — |

Lectura: **AC-SCN-01-10 pasa** en lo que se puede medir en la PC (p99 ≤ 16,7 ms y ningún cuadro > 33 ms en los cinco momentos; memoria ≪ 1,5 GB), con `Serie larga` sin semilla en el build (QA-V6a-13). **AC-WKT-13-15 pasa por el camino del usuario** (11,9 a 16,1 ms en la primera entrada del proceso, ≤ 10 ms en las siguientes, salidas ≤ 10,7 ms); con margen fino en la primera libre (16,1 ms) y con 22-25 ms en los tres *drivers* que cambian la hora o arrancan la rutina en el mismo cuadro del toque (QA-V6a-06).

**La escena apagada en Workout** (AC-SCN-01-06 [Device-Win]): con la app quieta en Workout, el motor 3D del proceso promedió la mitad que en el inmersivo (contador `GPU Engine` del proceso, 5 muestras de 1 s cada uno) y la CPU del proceso ≈ 16 % de un núcleo (la UI a su ritmo de reposo). DEMO-01 y DEMO-08 afirman 0 dibujos de la escena después de `Ready`.

### 4.2 Registro

En las dos sesiones de la prueba a mano (4 entrenamientos) y en las corridas `[PERF]`: **0 líneas `[SLOT]`, 0 avisos y 0 errores** (sin el Terrain Sample Asset Pack); `[SCENE] escena: nivel High`; `[UI] HUD precalentado … 70-79 ms de armado` (en reposo).

### 4.3 Prueba a mano (entrada sintética con el código de rastreo y `mouse_event` absoluto; ayudas en `<scratch>`)

| # | Qué se hizo | Qué se vio | |
|---|---|---|---|
| 1 | Workout → `Entrenamiento libre` (el HUD en el idioma del sistema; el panel del simulador, fijo en español, Q-03) | Negro, la cámara volando desde atrás y arriba, la pose detrás del bote a los ≈ 2,5 s; la luz de la hora real; el reloj corre desde el toque | Bien (AC-SCN-02-05, AC-WKT-13-06) |
| 2 | Llave → `Fuerte` | El bote acelera; el muelle de los 575 m pasa por la izquierda con sus pilotes; montañas quietas | Bien |
| 3 | `Hora escena` `+` hasta 15, 18, 22 h, `+` hasta 6 h (por 23 y 0) y hasta 12 h, `Hora real` | Cada cambio en ≈ 1,5 s con el paso intermedio visible (captura a mitad de 17 → 18); 18 h atardecer con estrellas tenues; **22 h noche con la luna en la fase del día real, con su terminador y la parte oscura apenas visible**, 80 estrellas; 6 h amanecer; 12 h mediodía; `Hora real` vuelve en ≈ 1,5 s. El reloj de pared, siempre en la hora real | Bien (AC-SCN-23-01, -02, -04, AC-SCN-10-07, AC-BLE-16-07) |
| 4 | `Pausa` → `Reanudar` | El velo: menos color y más oscuro, `PAUSA` en su disco, el HUD a pleno; al reanudar se va | Bien (AC-WKT-13-12) |
| 5 | `Terminar` → `Terminar` | El HUD se va, la escena funde a negro y vuelve Workout | Bien (AC-WKT-13-08) |
| 6 | `Hora escena` en 18 h desde Workout → `Corta` desde la lista → `Iniciar`, hasta el final | La rutina con el atardecer; a los 30 s el diálogo de completado y la cámara que sube (el horizonte sube); en W1280 **el diálogo tapa la mitad de arriba del casco** (se ve solo la popa debajo del diálogo); `Terminar` del diálogo → Workout | QA-V6a-03 |
| 7 | Libre de nuevo y, a los ≈ 20 s, **matar el proceso** | La copia de la base, leída con `RowMate.SessionList --db`: 3 sesiones, la libre cortada **sin fin y con 6 puntos** (cada 5 s), `Corta` y la primera libre completas | Bien (Q-12; P-03 de la 4a) |
| 8 | Volver a abrir sobre la misma carpeta | Arranca sin avisos; **`Hora escena` en `real`** (no sobrevivió al reinicio); 17 h desde Workout y otra libre: entrada 11,9 ms; `Terminar`; cerrar la ventana | Bien (AC-SCN-23-03) |

Nota para la ayuda de QA: un clic en la llave **sin mover antes el puntero** con `mouse_event` absoluto no la abre (UI Toolkit no recibe el *hover*); con el movimiento antes, sí. Los botones grandes (pestañas, `Entrenamiento libre`, `Pausa`) respondían igual. No es un error de la app.

---

## 5. Criterios de aceptación

Marcas: d = dotnet (y Unity EditMode), u = Unity EditMode, p = PlayMode, ps = PlayMode con simulador, cap = arnés, eq = equipo (§4). El número es la cantidad de pruebas en verde de la matriz combinada.

### 5.1 Nuevos (78)

| ID | Resultado | Evidencia |
|---|---|---|
| AC-SCN-02-01 | PASS | d p 49 (LT-62, los siete tamaños); horizonte y focal en el *manifest* de cada toma; el remero en el centro de la zona (CAP-13 a -17) |
| AC-SCN-02-02 | PASS | d p 68 (LT-63, LT-65; las marcas con la cámara real en los siete tamaños); CAP-02, -05, -07: 0 dp |
| AC-SCN-02-03 | PASS | d p 11 (LT-64, el plano cercano en 0,5, el tramado de 2 a 1 m); VID-03 sin cortes |
| AC-SCN-02-04 | PASS | d 60; horizonte ≤ 2,932 dp (VID-03, W1280), ≤ 1,25 (VID-10); la rampa de 0,6 s en VID-01 |
| AC-SCN-02-05 | PASS | d p 59; VID-01 (negro, vuelo, pose a los 1 820 ms); build §4.3 (1) |
| AC-SCN-02-06 | PASS | d p 11; VID-07; build §4.3 (5) |
| AC-SCN-02-07 | **FAIL** | La cámara cumple (d p 11; VID-08: `Aerial` a los 30,333 s, quieta a los 36,333, cabeceo −4°, horizonte +28,77 dp; CAP-22), pero **"el bote sigue a la vista" no**: el diálogo opaco lo tapa entero en TV (CAP-22, VID-08) y la mitad de arriba en W1280 (build). QA-V6a-03 |
| AC-SCN-02-08 | PASS | d p 8 (`TheSceneHasNoUserInput`) |
| AC-SCN-03-01 | PASS | d 12 (LT-66) |
| AC-SCN-03-02 | PASS | d 6; CAP-13 a -17: 234,849 m con la banda en `1240` `m` |
| AC-SCN-03-03 | PASS | d 12 (LT-67); VID-04: 5,92 m y quieto a los 8,97 s; CAP-20: 1,05 m/s |
| AC-SCN-03-04 | PASS | d p 20 |
| AC-SCN-03-05 | PASS | d p 9 (`TheSceneDistanceStaysInTheScene`); CAP-13 |
| AC-SCN-03-06 | PASS | d 6 (LT-68) |
| AC-SCN-03-07 | PASS | d ps 19 |
| AC-SCN-09-01 | PASS | d p 21 |
| AC-SCN-09-02 | PASS | d 45 (LT-70 a -72, LT-74; el oráculo cada minuto) |
| AC-SCN-09-03 | PASS | d 33 |
| AC-SCN-09-04 | PASS | d 6; CAP-01 a -04 |
| AC-SCN-09-05 | PASS | d 42; CAP-01 a -04 contra LEG-01 a -04: el mismo tono dominante a cada hora (QA) |
| AC-SCN-09-06 | PASS | d 18; CAP-04, CAP-09, VID-05/06 (la ventana se enciende y se apaga con su fundido). La sonda de contrato de `#FFE082` en la pasada sin post no está (QA-V6a-08) |
| AC-SCN-10-01 | PASS | d 15 (LT-69, -70, -73) |
| AC-SCN-10-02 | PASS | d 36 |
| AC-SCN-10-03 | PASS | d 39 |
| AC-SCN-10-04 | PASS | d 27; el astro en CAP-02, -04, -05, -06, -10, -11, -13 a -17 a ≤ 2,23 dp; siempre en el cuadro |
| AC-SCN-10-05 | PASS | d 21 (barrido minuto a minuto); VID-05, VID-06; CAP-08 y -09 |
| AC-SCN-10-06 | PASS | d 30; CAP-11 (sombra del casco del lado opuesto al sol) |
| AC-SCN-10-07 | PASS | d 63; CAP-04, CAP-10 (0,937 iluminada); build: la fase del día real con terminador y luz cenicienta |
| AC-SCN-11-01 | PASS | d p 22; CAP-01 a -04 |
| AC-SCN-11-02 | PASS | d 18; CAP-02: contrato arriba y a media altura, diferencia 0 |
| AC-SCN-11-03 | PASS | d 14; 80 estrellas en CAP-04, -10, -23; ninguna sobre el disco |
| AC-SCN-11-04 | PASS | d 48 (6:12 / 6:13 / 17:59 / 18:00, el fundido); CAP-08, CAP-11 (0), VID-05/06 |
| AC-SCN-11-05 | PASS | d 30; `it6a-brillo`: 30,48 dp (mínimo 30, margen fino) |
| AC-SCN-11-06 | PASS | d 63; las 7 nubes a 0 dp del legacy en todas las tomas |
| AC-SCN-11-07 | PASS | p 8 (LT-78 a LT-81) |
| AC-SCN-14-01 | PASS | d 9 (LT-75) |
| AC-SCN-14-02 | PASS | d 36 (vectores dorados; segmentos 0 a 2 000 bit a bit con el oráculo) |
| AC-SCN-14-03 | PASS | d 3 (LT-76) |
| AC-SCN-14-04 | PASS | d 12 (LT-77; las listas contra el oráculo) |
| AC-SCN-14-05 | PASS | d 9 |
| AC-SCN-14-06 | PASS | d 4 |
| AC-SCN-14-07 | PASS | d p 8 (la aparición 400 → 360 m y el tramado en Core); VID-03 mirado por QA sin apariciones de golpe. La sonda de VID-03 de §5.2 no existe (QA-V6a-08) |
| AC-SCN-15-01 | PASS | d 3 |
| AC-SCN-15-02 | PASS | d 9 |
| AC-SCN-15-03 | PASS | d 6; el muelle de 175 m en VID-03 y el de 575 m en el build |
| AC-SCN-15-04 | PASS | d 6 (LT-77) |
| AC-SCN-15-05 | PASS | d p 17 (`AnchorsLandOnTheLegacyX`, > 1 000 objetos en los siete tamaños); CAP-02, -05, -07 contra LEG con marcas |
| AC-SCN-15-06 | PASS | d p 30 (medidas y paletas) |
| AC-SCN-15-07 | PASS | d p 88; CAP-01, -02, -07, -12 (nada flota; bruma a las 6) |
| AC-SCN-15-08 | PASS | d p 35 (el slot de prueba: entra y, quitado, un aviso); todos los slots en `Procedural` y sin asset; sin `Assets/ThirdParty/`; build sin `[SLOT]`; `git check-ignore` (§7) |
| AC-SCN-23-01 | PASS | ps 1; build §4.3 (3) |
| AC-SCN-23-02 | PASS | d ps 43; VID-05/06; build |
| AC-SCN-23-03 | PASS | ps 1; build §4.3 (8): `real` después de reiniciar |
| AC-SCN-23-04 | PASS | ps 1; DEMO-04 a -08; build |
| AC-SCN-01-02 | PASS | p 6; CAP-13 a -17 |
| AC-SCN-01-03 | PASS | d p 8; VID-09 (la distancia sigue al rotar) |
| AC-SCN-01-04 | PASS (PC) | d p 6. [Device-And] NO VERIFICABLE (sin teléfono) |
| AC-SCN-01-05 | PASS | p 6 (LT-84 a LT-87); build: 4 entrenamientos sin avisos |
| AC-SCN-01-06 | PASS | p 2; DEMO-01 y -08 (0 dibujos después de `Ready`); build §4.1 (GPU en reposo) |
| AC-SCN-01-07 | **FAIL** | p d 10 miran la opacidad; **en la imagen la escena aparece de golpe** (VID-11, cuadros 45-64): el fondo provisorio queda encima durante el fundido. QA-V6a-01 (D2-05). En el build la escena demorada llega y prepara por tramos (≤ 9,8 ms) |
| AC-SCN-01-08 | PASS | d p 9; el casco dentro de la zona medido en CAP-13 a -17 |
| AC-SCN-01-09 | **NO VERIFICABLE** | Sin módulo de Android ni teléfono (It. 14) |
| AC-SCN-01-10 | PASS | build §4.1 (5 min libre, horas, entrada, completado; `Serie larga` sin semilla en el build, QA-V6a-13) |
| AC-SCN-01-11 | PASS (parcial) | p 10: dibujos, SetPass y triángulos por nivel; la memoria de texturas y la CPU de la escena ≤ 1,5 ms no se miden (QA-V6a-09) |
| AC-SCN-01-12 | PASS | d p 4: 0 en el `Tick` y 0 en URP en las cuatro escenas con control positivo; sin el repintado del HUD con la escena adentro (QA-V6a-07) |
| AC-SCN-01-13 | PASS (PC) | d p 29; en el build la resolución se mantuvo en 1,00 (no hizo falta bajar). [Device-And] NO VERIFICABLE |
| AC-SCN-01-14 | PASS | d u 32; las 427+ de `Tests/EditMode/Scene` en dotnet y en Unity, con y sin simulador |
| AC-SCN-01-15 | PASS | d p 8; CAP-12 |
| AC-SCN-13-01 | PASS | p 2 |
| AC-SCN-13-02 | PASS | d p 6; CAP-01 a -07 contra LEG |
| AC-SCN-13-03 | PASS | d p 32; a las 19:34 la visibilidad vale 0 (`NoShimmerAtTheChangeOfStar`, el dato que va al *shader*; sin conteo de píxeles, QA-V6a-08) |
| AC-SCN-13-04 | PASS | p 2 (LT-83) |
| AC-SCN-19-01 | PASS | d 60; CAP-01 a -07 (ver QA-V6a-12 sobre cómo se leen) |
| AC-SCN-19-02 | PASS | d 21; VID-03; CAP-12 |
| AC-SCN-20-01 | PASS | d 45; CAP-02, -04 |
| AC-SCN-20-02 | PASS | d 3; CAP-04, CAP-09 |
| AC-SCN-21-01 | PASS | p 4; sin selector |
| AC-WKT-17-13 | PASS | d 23 (contraste); CAP-19 en los píxeles: las hechas y el borde de la hueca sobre `#071222` |

### 5.2 De iteraciones anteriores (23)

| ID | Resultado | Evidencia |
|---|---|---|
| AC-WKT-13-02 | PASS | p 4; CAP-13 a -17 |
| AC-WKT-13-06 | PASS | d p 32; VID-01, VID-02; build |
| AC-WKT-13-08 | PASS | d p 22; VID-07; build |
| AC-WKT-13-12 | PASS | d p 31; CAP-20, -21 (sonda de WP-D2: canal máximo × 0,851 y croma × 0,724); VID-04; build |
| AC-WKT-13-15 | PASS | d p 18; build por el camino del usuario ≤ 16,1 ms; ver QA-V6a-06 |
| AC-WKT-14-19 | PASS (PC) | d p 6; en el build, con la escena y rutinas (`Series 600`, `Fondo 3000`, `Corta`), ≤ 11 ms fuera de la primera entrada; `Serie larga` sin semilla en el build (QA-V6a-13); [Device-And] NO VERIFICABLE |
| AC-WKT-17-07 | **FAIL** | d p 9 (el reacomodo, la sesión y la escena siguen); **sin el fundido cruzado de 200 ms** (VID-09). QA-V6a-02 (D2-04) |
| AC-WKT-17-09 | PASS | p 15; CAP-13 a -18 |
| AC-BLE-16-07 | PASS | d ps 17; DEMO-04 a -08; build |
| AC-SCN-01-01 | PASS | p 4; CAP-24 |
| AC-APP-05-08 | **FAIL (parte)** | d p 70 y VID-02 pasan; la parte "el fundido de la escena que llega tarde sigue" no se ve (mismo origen que QA-V6a-01) |
| AC-WKT-13-09 | PASS | p 18; build (4 entrenamientos) |
| AC-WKT-13-13 | PASS | d p 34 |
| AC-WKT-13-14 | PASS | d p 28 (`TheSceneDistanceStaysInTheScene`, la secuencia del motor) |
| AC-WKT-19-03 | PASS | d ps 10; build |
| AC-WKT-17-01 | PASS | d p 206 |
| AC-WKT-02-08 | PASS | d p 53 |
| AC-WKT-18-21 | PASS | d p 14 (56-57 de 60 con la escena); build: ventanas con paneles ≤ 10 ms |
| AC-BLE-16-02 | PASS | d ps 86; DEMO-02 a -08; build |
| AC-BLE-16-03 | PASS | d ps 49 |
| AC-BLE-15-09 | PASS | d ps 9 |
| AC-HIS-20-04 | PASS | d u 71; QA: todas las corridas con carpetas temporales; la carpeta de datos del usuario igual al terminar |
| AC-WKT-21-01 | PASS | d p 13; VID-08 (a los 300 ms, una vez); build |

**Totales: 96 PASS (entre ellos 4 con una parte de Android NO VERIFICABLE y 1 parcial, AC-SCN-01-11), 4 FAIL, 1 NO VERIFICABLE.**

---

## 6. Casos legacy LT-62 a LT-87

Los 26, con el nombre exacto de la spec y en verde: LT-62 a LT-77 y LT-82 en dotnet y Unity EditMode (`It06aLegacyCasesKeepTheirNames`); LT-78 a LT-81, LT-83 y LT-84 a LT-87 en PlayMode, con sus nombres (`EnvironmentId.lake 390x844 12.0h pinta sin excepciones` … `844x390 23.0h anima sin excepciones`, `sin shader pinta el fallback sin excepciones`), todos `Passed` en el XML de PlayMode. Sin la `-UnityResults`, la matriz de dotnet los muestra como SIN TEST (QA-V6a-11).

---

## 7. Revisión estática

- **La presentación no toca el dato.** `PureCodeRulesTests` (`TheSceneDistanceStaysInTheScene`, `TheSceneHasNoUserInput`, `NoSystemClockInTheSceneCore`, `SceneCallsCoreTestControlsOnlyFromItsOwnTestControls` con su control positivo) en verde. `SceneHost` lee `Workout.State` y la última lectura; `SceneBridge` no escribe en el motor, la sesión ni la telemetría (la única asignación es su propio estado de `Readiness`). La cámara y su *motion* son de la escena.
- **Nada sigue al salir.** La escena se cierra a los 400 ms de la salida (`CloseScene`), `AC-WKT-13-09` en verde con la escena, DEMO-01/08 en 0 dibujos y el build con la GPU de Workout en la mitad que en el inmersivo.
- **0 B por cuadro.** `SceneBudgetTests.NoAllocationsPerFrame` (cuadros reales, control positivo de 120 en 120, 0 en URP); el HUD sin escena, `HBT`. Falta el repintado del HUD con la escena dentro de la misma medida (QA-V6a-07).
- **Slots.** Los 52 slots de `Assets/RowMate/Slots/` con `_source: 0` (`Procedural`) y `_asset: {fileID: 0}`; ningún asset versionado referencia `Assets/ThirdParty/` (`NoVersionedAssetReferencesThirdPartyOutsideTheSlots`). Sin el pack, 0 avisos.
- **Terceros.** `Assets/ThirdParty/` no existe; `git check-ignore -v` lo ignora (`.gitignore:70`) y deja pasar los `.meta` (`.gitignore:72`); `git ls-files Assets/ThirdParty` vacío.
- **Licencia BSD-3.** `THIRD_PARTY_NOTICES.md` en la raíz con el aviso completo del SDK de Dart; `Core/Scene/DartRandom.cs` lo cita en su cabecera; el build lo lleva al lado del ejecutable, igual byte a byte (`TheDartPortCarriesItsLicense` en verde).
- **`ProjectSettings` y `Assets/RowMate/Settings`.** El SHA-256 de cada archivo, tomado antes de la primera corrida de Unity, es igual al de después de todas (tests, tres arnés, build, siembra); la guardia de los *scripts* dijo "sin cambios" en cada una. Los cambios que muestra `git status` en `ProjectSettings/` son los del *bootstrapper* v4 (B6a-08), no de las pruebas.
- **Repo público.** `git grep -nIE` de rutas de usuario de Windows, de Git Bash y del prefijo de las carpetas de trabajo, sobre lo versionado y sobre lo nuevo sin seguimiento: 0. Búsqueda de nombres de hardware, del nombre de usuario de la PC y de la carpeta de trabajo en todo lo versionable: 0 (el autor en `LICENSE` es público). Los PNG y MP4 de `snapshots/` no tienen esas cadenas. Este documento usa rutas relativas, `<scratch>` y la URL del legacy, y no da datos del equipo.
- **Legacy.** `git -C ../rowmate-legacy status --porcelain` vacío al empezar, después del oráculo y al terminar.

---

## 8. Issues

| ID | Sev. | Dueño | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V6a-01 | **mayor** | architect | **La escena demorada aparece de golpe** (AC-SCN-01-07; la parte de AC-APP-05-08): `scene-surface` es el primer hijo del HUD y el fondo provisorio, opaco, queda encima durante el fundido de 0,6 s; cuando pasa a `display: none` la escena aparece en un cuadro (D2-05, que seguía pendiente) | VID-11: cuadros 45-62 con el provisorio y la opacidad de `scene-surface` de 0,157 a 0,995 en el *manifest*; cuadro 64, la escena entera. Las pruebas miran la opacidad, no la imagen | Que `scene-surface` quede encima del provisorio mientras se ven los dos (o fundir el provisorio con `1 − opacidad`) y una prueba PlayMode que lea los píxeles a mitad del fundido (la mezcla entre el provisorio y la escena) |
| QA-V6a-02 | **mayor** | architect | **Al rotar no hay fundido cruzado de 200 ms** (AC-WKT-17-07, DT6a-06): el HUD aparece entero en el primer cuadro de TH (D2-04, pendiente). `HudReflowMotion` está probada sola (`SceneMotionTests`), pero ninguna prueba de integración mira la opacidad de las piezas después de un cambio de clase de ventana | VID-09: cuadros 15 y 16 (los primeros de TH) con el HUD a pleno. `ImmersiveWorkoutPage.OnShellLayoutChanged` → `_reflow.OnOrientationClassChanged` → `ApplyReflow` pone la opacidad en `PieceOf(...)`; algo la pisa o la aplica a piezas que la disposición nueva reemplaza | Encontrar quién pisa la opacidad (el orden con `HudZones.Update` o el cambio de disposición) y sumar una prueba PlayMode: a los 50 ms del cambio las piezas con opacidad < 1 y a los 200 ms, 1; con reducir movimiento, 1 en el acto |
| QA-V6a-03 | **mayor** | po | **La toma aérea pasa detrás del diálogo de completado** (AC-SCN-02-07, demo (c)): "el bote sigue a la vista, frenando" no se cumple, porque el diálogo opaco de la 4b ocupa el centro de la pantalla, justo la zona del bote | CAP-22 y VID-08 (TV): el casco no se ve desde que entra el diálogo; build en W1280: se ve solo la popa debajo del diálogo | Decidir con el Designer: correr o achicar el diálogo de completado fuera de la zona del bote (arriba en vertical; a un costado en horizontal), o aceptar que en la 6a el bote quede detrás y se vea a través del vidrio con desenfoque de la 6b, y corregir el texto de AC-SCN-02-07, CAP-22 y la demo (c) |
| QA-V6a-04 | menor | architect | **CAP-22 se toma con la escena a 4,5 m y quieta** (velocidad 0), cuando a los 36,5 s de `Corta` la escena real va por ≈ 119,9 m y 0,05 m/s (VID-08, la misma rutina). El salto de `AdvanceTo` integra la escena con las entradas del final del salto (la sesión ya terminada) | *manifest*: CAP-22 `distancia 4.501 m; velocidad 0 m/s`; VID-08 a los 36,5 s `119.908 m; 0.05 m/s` | En el arnés, avanzar en tramos cortos (≤ 1 s, o cuadro a cuadro) mientras haya escena, así la escena ve las lecturas y la fase de cada momento; o tomar CAP-22 cuadro a cuadro desde los 29 s, como VID-08 |
| QA-V6a-05 | menor | po | **DEMO-03 no muestra el muelle**: a los 39,2 s ya salió del cuadro por la izquierda (en W1280 la orilla sale a 10,1 m); se ve de los 34 a los 38,2 s | DEMO-03; VID-03 cuadros 120-245 | Tomar DEMO-03 a los ≈ 36 s y decir en la demo (a) "a los ≈ 36 s se ve el muelle a la izquierda" (pasa a la par a los ≈ 39 s) |
| QA-V6a-06 | menor | architect | **Primera entrada de 22-25 ms en los *drivers* `[PERF]`** que cambian la hora (`-rowmate-perf-hours`) o arrancan la rutina con `StartRoutine` directo (`-perf-completion`, `-perf-series`) en el cuadro del toque. Por el camino del usuario la primera entrada da 11,9-16,1 ms (AC-WKT-13-15 pasa), con **0,6 ms de margen** en la libre de `-rowmate-perf-scene`. B6a-20 ya anotaba "12,0 a 23,1 ms" sin marcarlo | §4.1 | Que los *drivers* fijen la hora antes del toque y arranquen las rutinas por la lista (como el usuario), o que anoten la excepción; y buscar el cuadro de ≈ 16 ms de la primera entrada (el armado del HUD con el primer dibujo de la escena) para recuperar margen |
| QA-V6a-07 | menor | architect | **AC-SCN-01-12 sin el repintado del HUD con la escena**: `FrameAllocations.WatchScene` engancha el `Tick` y el pedido de render pero no el `generateVisualContent` del HUD ni de `SceneSurface`, que repinta en cada cuadro con la escena (B6a-02) | `[SBT] 0 B …: 0 asignaciones (0 en 0 dibujos …)` en las cuatro escenas | Enganchar el observador de dibujo de `SceneSurface` (y de los paneles) en `WatchScene` y afirmar `Draws > 0` y `DrawAllocations == 0` |
| QA-V6a-08 | menor | architect | **Sondas de §5.2 que no existen** (D2-03 y más): la ventana `#FFE082` en la pasada de contrato (CAP-04), la luz de la cabaña ± 25 %, el contraste de las píldoras de CAP-19 contra el vidrio dibujado, "ningún objeto pasa de alfa 0 a 1 en un cuadro" (VID-03) y "0 píxeles de líneas a las 19:34" (VID-05). B6a-17 decía que quedaban como valores del *manifest*; no están ni como valores | `manifest.json`: sin entradas de contraste, ventana ni aparición; `SceneMarks.Contract` solo con las dos del cielo | Sumarlas a `SceneMarks.Contract` (las de contrato) y como valores las de aspecto; o corregir B6a-17 y §5.2 diciendo cuáles se miden a mano |
| QA-V6a-09 | menor | architect | **AC-SCN-01-11 en parte sin medir**: `DrawCountsPerTierWithinTheBudget` afirma dibujos, SetPass y triángulos, pero no la memoria de texturas ni la CPU de la escena (≤ 1,5 ms) | `SceneBudgetTests.cs:111-188` | Sumar el `ProfilerRecorder` de memoria de texturas y el tiempo del `Tick` + pedido de render (con `TickObserver`) con sus topes |
| QA-V6a-10 | menor | architect | Tres avisos CS8625 nuevos en la compilación de Unity (D2-08, pendiente) | `Scene/SceneLighting.cs(190,41)`, `Scene/SceneRenderer.cs(78,40)`, `(90,40)` | Tipos anulables o `null!` explícito, como en D2-01 |
| QA-V6a-11 | menor | architect | `It06aIds`: LT-78 a -81 y LT-83 a -87 (solo PlayMode) no están en `UnityOnlyIds`, así que `run-tests.ps1 -Matrix` sin `-UnityResults` los da como **SIN TEST** (A6a-03 decía "hasta WP-B1") | Matriz de dotnet sola: `SIN TEST 9` | Que `-Matrix` cuente también los LT con verificación `p`/`ps` como PENDIENTE UNITY (o sumarlos a la lista) |
| QA-V6a-12 | menor | designer | **Aspecto** (no rompe criterios): (a) en CAP-03 "la cabaña en contraluz con el halo" no se lee: a las 18 h el halo `sunGlow` casi no se distingue del horizonte; (b) en horizontal (CAP-05) la ladera con bosque tapa casi todas las cordilleras, que en LEG-05 son la silueta dominante; en vertical (CAP-01, -02, -07) se leen lavadas | CAP-03, CAP-05 contra LEG-05; CAP-01/02/07 | (a) un poco más de halo o de brillo detrás del collado a las 18 h; (b) bajar la ladera cercana o correrla más allá de los 18 m en el tramo que tapa el horizonte, o dar más contraste a la silueta lejana; mostrárselo al usuario con P-18 |
| QA-V6a-13 | menor | architect | **`Serie larga` no se puede medir en el build con la escena**: `SnapshotCli.SeedDatabase` solo siembra `Cap27`, `F4A` y `F5` (sin `F-4B`), y ningún *driver* `[PERF]` la arranca. Mismo hueco que en la 4b | `SnapshotCli.cs:106` | Sumar `-seed F4B` (o la rutina a `F5`) y un *driver* para los últimos 5 s de cada paso con la escena (AC-SCN-01-10, AC-WKT-14-19) |

**Resumen: 0 bloqueantes, 3 mayores (QA-V6a-01, -02, -03) y 10 menores.**

---

## 9. Para el coordinador

1. **Ronda 2 (antes del REPORT):** el Architect corrige QA-V6a-01 y -02 con sus pruebas de integración (las dos eran D2-05 y D2-04, ya vistas por el Designer) y, si puede, QA-V6a-04, -07, -08, -10 y -11, que son baratos; el PO decide QA-V6a-03 (con el Designer) y QA-V6a-05. Después, QA vuelve a correr la suite, el arnés (dos corridas) y mira VID-09, VID-11, CAP-22 y VID-08.
2. **Para el usuario (gate):** P-18 con las tomas de §11.1 de la spec, más QA-V6a-12 (cómo se leen las montañas en vertical y en horizontal) y la decisión de QA-V6a-03. Siguen fuera de alcance de QA la demo (c) de la 5 (AC-WKT-02-11) y el teléfono (AC-SCN-01-09).
3. **Nada quedó cambiado:** sin `RowMate.exe` ni Unity abiertos por esta verificación; las preferencias de la ventana del *player* y la carpeta de datos del usuario, iguales a como estaban; `ProjectSettings` y `Settings`, iguales.

---

## Verificación hecha

1. **Legacy**, solo lectura: `docs/image-03.png` (con Read) y lo que lee el oráculo `-Scene` dentro de su guardia. `git -C ../rowmate-legacy status --porcelain` vacío al empezar, después del oráculo y al terminar; no se leyó el módulo de credenciales.
2. **Corridas propias**: dotnet (`-Matrix` y `-UnityResults`), Unity EditMode y PlayMode con y sin simulador, el arnés `it6a`, `it5` e `it4b` (a `<scratch>`), el oráculo `-Scene`, el build de simulador, siete corridas `[PERF]` y dos sesiones a mano en el build. Herramientas propias en `<scratch>/it6a/qa-main-r1/tools/` (Python con Pillow, NumPy y PyAV sin red; PowerShell con Win32): comparación de PNG, hojas de cuadros de los MP4, medida del casco en las tomas, entrada sintética y capturas de la ventana del build.
3. **Datos**: toda corrida con `ROWMATE_DATA_DIR` temporal; las bases y el `prefs.json` del usuario no se abrieron (la carpeta de datos del usuario, igual por *hash*).
4. **Repo**: este documento y el build (ignorado). Sin *commits*.

---

# Ronda 2

| Campo | Valor |
|---|---|
| Autor | rowmate-qa (verificación principal, ronda 2; el único agente de la verificación que usó Unity) |
| Fecha | 2026-11-03 |
| Entrada | Lo de la ronda 1 más [02-tech-design](02-tech-design.md) "Cambios por verificación (ronda 1)" (V1-01 a V1-14, V1-M1 a V1-M11) y [01-po-spec](01-po-spec.md) "Cambios por verificación (ronda 1)" (QA-V6a-03 → (B) con P-19; QA-V6a-05; PAR-03 y -04) |
| Re-verificados uno por uno | QA-V6a-01, -02, -03 y QA-V6a-REN-01 a -10 (§R2.1) |
| Artefactos | `<scratch>/it6a/qa-main-r2/` (dotnet, los cuatro XML de Unity y sus registros, las corridas del arnés `it6a`, `it5` e `it4b`, las hojas de cuadros de VID-09 y VID-11, el build, las seis corridas `[PERF]`, la prueba a mano con sus capturas y las copias de la base) |
| Repo | Sin *commits*. Esta ronda escribió en el repo solo esta sección (y la línea de arriba en §0) y el build de `Builds/Windows/simulator` (ignorado). No se tocó ningún fuente |

## R2.0 Veredicto

**APROBADO CON OBSERVACIONES.** Los tres mayores de la ronda 1 y los diez de la lente de rendimiento que había que volver a mirar **están corregidos y verificados** (§R2.1); los cuatro criterios que estaban en FAIL pasan. **Criterios: 100 PASS · 0 FAIL · 1 NO VERIFICABLE** (AC-SCN-01-09, Android). Quedan **1 mayor nuevo** (QA-V6a-R2-01: el HUD con rutina asigna memoria en cada cuadro en el *player*; no es de la escena y viene de la 4b/5, lo muestra el contador por cuadro nuevo de esta ronda) y **4 menores** (§R2.7). Ninguno pide otra ronda de la 6a: R2-01 se propone como deuda al empezar la 6b (como la 00-deuda-5), y los menores se pueden cerrar con el REPORT o en esa deuda.

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix`) | Compilación **0 avisos, 0 errores** · herramientas **192 / 192** · Core **3049: 3010 pasan, 0 fallan, 39 ignoradas** · matriz sola: PASA 615, FALLA 0, IGNORADO 4, PENDIENTE UNITY 143, **SIN TEST 0** (QA-V6a-11 cerrado) |
| Unity EditMode | **3137: 3092 pasan, 0 fallan, 45 ignoradas**; guardia: sin cambios. **Sin los tres CS8625 de la escena** (QA-V6a-10 cerrado) |
| Unity EditMode con simulador | **3329: 3284 pasan, 0 fallan, 45 ignoradas**; el *define* restaurado; guardia: sin cambios |
| Unity PlayMode | **480 / 480**; guardia: sin cambios |
| Unity PlayMode con simulador | **552: 551 pasan, 1 falla**: `SceneBudgetTests.MemoryAndSceneCpuPerTierWithinTheBudget(High)`, "la CPU de la escena (p95) … Expected ≤ 1.5 But was 1.5119". Repetida dos veces (`-Filter SceneBudgetTests`, con simulador): **10 / 10 y 10 / 10** (p95 1,18 y 1,16 ms). Es una prueba de tiempo con poco margen (QA-V6a-R2-02), no un defecto de la escena |
| Matriz con los 4 XML | 762 IDs: PASA 757, **FALLA 1 (AC-SCN-01-11, por la prueba de arriba)**, IGNORADO 4, PENDIENTE UNITY 0, SIN TEST 0. Con las dos repeticiones en verde, AC-SCN-01-11 pasa |
| Arnés `it6a` | **45 tomas, 0 con problemas, 0 sondas que fallan**; la corrida de QA es **igual byte a byte** a la publicada (46 archivos); `manifest.json`, solo `durationMs`. Sin necesitar la tolerancia de 1 nivel |
| `it4b` / `it5` | Corridas de QA (51 y 47 tomas, 0 con problemas) **iguales byte a byte** a lo que hay en el árbol (52 y 48 archivos; `manifest.json`, solo `durationMs`): el cambio del orden de los hijos del HUD no tocó nada sin escena. Sin cambios desde la ronda 1. `it2`, `it3`, `it4a`: sin cambios contra `HEAD` |
| Oráculo `-Scene` | No se volvió a correr: desde la ronda 1 no cambió nada de `tools/oracle/`, de `snapshots/legacy/` ni del legacy (`find -newer` sobre esas carpetas, vacío; `git -C ../rowmate-legacy status --porcelain` vacío). Lo de la ronda 1 (LEG-01 a -07 iguales byte a byte) sigue valiendo |
| Build de Windows con simulador | Rehecho por QA con el código de esta ronda (`tools/build/build.ps1 -Target Windows -Simulator`), `THIRD_PARTY_NOTICES.md` al lado igual byte a byte al del repo, guardia sin cambios |
| `[PERF]` | Seis corridas (§R2.4): **0 subidas de *shaders* de la escena o de URP después de `Ready`** en las seis (`perf-log-check.ps1` sale con 0; 155 antes de la marca), 0 armados de la orilla a la vista, 0 `[SLOT]`, 0 avisos y 0 excepciones. Libre 5 min con `Fuerte`: **29 ventanas con máx 3,1-3,9 ms, p99 ≤ 2,8 ms**, sin el cuadro de 93,5 ms que vio el Architect |
| Prueba a mano en el build | Hecha (§R2.5): vuelo de entrada, `Fuerte`, `Hora escena` en 22, 6, 12 y 18 h, pausa con el velo, `Corta` hasta el diálogo y la toma aérea, salida, matar a mitad, reabrir. Entrada por el camino del usuario **13,1 ms** (la primera del proceso), 9,9 y 6,9 ms las siguientes; 12,6 ms la primera después de reabrir |
| Repo público | `git grep -nIE` de rutas de usuario de Windows, de Git Bash y del prefijo de las carpetas de trabajo sobre lo versionado y lo nuevo sin seguimiento: **0**. Sin datos del equipo, del hardware ni del usuario en esta sección, el *manifest* ni el `INDEX.md` (§R2.6) |

## R2.1 Los issues corregidos, uno por uno

| Issue | Qué se corrigió (02-tech-design, V1) | Cómo lo verificó QA | Estado |
|---|---|---|---|
| **QA-V6a-01** = **REN-10** (mayor; AC-SCN-01-07, AC-APP-05-08) | V1-01: el provisorio primero y `scene-surface` encima (`Presentation/Workout/HudView.cs:70-81`) | **VID-11 cuadro a cuadro**: cuadros 42-44, el provisorio solo; **46, 48, 50 y 52, la mezcla** (la orilla, el sol y el casco aparecen a través del azul, opacidad 0,157 → 0,769 en el *manifest*); del 54 en adelante, la escena. Ya no hay corte en el cuadro 64. `SceneLifecycleTests.TheLateSceneBlendsOverTheBackdrop` (lee la RT de la UI en 11 × 19 puntos) en verde en PlayMode con y sin simulador | **Cerrado** |
| **QA-V6a-02** (mayor; AC-WKT-17-07) | V1-02: `_landscape` se toma en el toque (`ImmersiveWorkoutPage.cs:131`) | **VID-09 cuadro a cuadro**: el cuadro 15 (el primero en TH) muestra la escena con el HUD en 0; 16 y 17, el HUD a medias; del 18 al 21, entero: el fundido de ≈ 200 ms. La sesión y la distancia siguen (1080 → 1084 m). `SceneImmersiveTests.TheHudFadesInWhenTheWindowChangesClass` en verde | **Cerrado** |
| **QA-V6a-03** (mayor; AC-SCN-02-07) | Decisión del PO (B), P-19: el diálogo opaco tapa el bote hasta la 6b; el criterio se mide con la cámara real | CAP-22 (nueva, cuadro a cuadro desde los 29 s): modo `Aerial`, cabeceo −4°, respiración 0, apartamiento del horizonte 28,77 dp, **popa en 553,175 dp (552,9 ± 2) dentro de la zona 323,85-598,135**, la escena a 119,872 m y 0,051 m/s (como VID-08). A la vista: el diálogo centrado y, a los costados, los pinos de esa distancia (en la ronda 1 eran los de 4,5 m: QA-V6a-04 cerrado). En el build (W1280), el diálogo a los 30 s y la popa asomando debajo | **Cerrado** con el texto nuevo (P-19 sigue para el usuario en el gate) |
| **REN-01** (mayor) | V1-06: `Core/Scene/ShaderWorkLog` y `tools/build/perf-log-check.ps1`; dos unidades de precalentado del post de URP | `perf-log-check.ps1` sobre los seis registros: `despues de Ready: 0 subidas …` (sale con 0). Control: QA contó en los registros **155 `Uploaded shader variant` antes de la marca y 0 después** en cada corrida. `ShaderWorkLogTests` (dotnet, con el control positivo) e `ItSixAToolingTests.ThePerfLogCheckCountsShaderUploadsAfterReady` en verde | **Cerrado** |
| **REN-02** (mayor) | V1-07: la sonda dice el peor cuadro, su momento, si era negro y quieto, y el GC | En cada entrada de las seis corridas y de la prueba a mano: "hilo principal (Main Thread): máx … (a los N ms del toque, con movimiento / negro y quieto, GC +k)". El cuadro que mide AC-WKT-13-15 queda identificado. El anillo propio de la sonda no lo ubica nunca ("el peor sin el cuadro (otro reloj o fuera de la memoria)" en todas): la ubicación sale solo del `ProfilerRecorder` (QA-V6a-R2-04) | **Cerrado** (con R2-04, menor) |
| **REN-03** (mayor) | V1-11: build y `[PERF]` con el código final, `Serie larga` y las asignaciones por cuadro | QA rehízo el build con el código de esta ronda y repitió seis corridas, con `-rowmate-perf-long-series 150` sobre una base `-seed F4B` sembrada por QA (§R2.4) | **Cerrado** (lo que mostró el contador nuevo es QA-V6a-R2-01) |
| **REN-04** (mayor) | V1-04: `StreamHidden`, `CatchUp` y la demorada con la orilla de la sesión | `SceneEntryTests.ReenteringRightAfterALongSessionNeverRebuildsTheShoreInView` y `TheDelayedSceneArrivesWithTheShoreOfTheSession` en verde. En el build: **"orilla entera a la vista: 0; orilla terminada en el negro: 0"** en las 19 entradas | **Cerrado** |
| **REN-05** (mayor) | V1-05: la RT de la sesión en la precarga y el calentamiento sobre ella | `SceneEntryTests.ReadyAlreadyHasTheSessionTarget` en verde; en el build las fases "RT de la sesión" (0,45 ms) y "post de URP" corren antes de `Ready`, y 0 subidas después | **Cerrado** |
| **REN-06** (mayor) | V1-08: cada unidad medida y más chica; el toque, el arrastre y la rueda frenan la precarga (`Bootstrap/ShellHost.cs:46-57`, `Bootstrap/SceneBridge.cs:99`) | `SceneEntryTests.EachWarmupUnitIsMeasured` en verde. En el build, con la escena demorada (lo que corre en el inmersivo): **peor cuadro de la preparación 3,8 a 6,1 ms**, peor unidad 3,6-5,1 ms. En la precarga en reposo siguen las unidades de la primera vez del código: post de URP (RT chica) **19,3-19,8 ms**, `Tick` sintético 13,0-13,2 ms, dibujo de calentamiento ≈ 9 ms (lo que el Architect dejó en "Lo que queda") | **Cerrado en lo que se ve**; lo de la precarga en reposo QA lo **acepta** para la PC (Workout quieto a 30 fps; el toque y el desplazamiento la frenan) y queda para medir en el teléfono (QA-V6a-R2-05) |
| **REN-07** (mayor; AC-SCN-01-11) | V1-09: Medio en `B10G11R11_UFloatPack32`, texturas no legibles, la CPU de la escena | `MemoryAndSceneCpuPerTierWithinTheBudget`: Medio 918 × 2040 `B10G11R11_UFloatPack32`, contenido 0,65 MB; Alto 1,15 MB; CPU p95 Medio 0,76-1,14 ms, Bajo 0,63-0,72 ms, Alto 1,16-1,51 ms. Las RT que suma la escena **dependen del orden de las pruebas** (Alto 46,9 MB en la corrida completa y 68,0 MB sola; Medio 51,2 y 41,9; Bajo 38,2 y 26,5): QA-V6a-R2-02 | **Cerrado** (con R2-02, menor) |
| **REN-08** (mayor; AC-SCN-01-05) | V1-03: `SceneHost.DropBuild()` con `Dispose` en `Fail`, `DisposeWorld` y `Dispose` (`Scene/SceneHost.cs:996-1079`) | `SceneLifecycleTests.ClosingWhileLoadingLeavesNoNativeBuffers` (con `NativeLeakDetection` y los dos cortes, `lowMemory` y `Dispose`) en verde; ningún "Leak Detected" en los registros de PlayMode | **Cerrado** |
| **REN-09** (mayor; AC-SCN-01-13) | V1-10: `SceneHost.LowerTier()` baja el perfil entero; `GraphicsTier` da Medio con gráficos integrados | `SceneAdaptationTests.LoweringTheTierLowersTheWholeSceneProfile` y `GraphicsTierTests` (Unity EditMode; los nombres de GPU se arman en la prueba) en verde. Leído en `Scene/SceneHost.cs:337-366`: cambia el perfil, aplica el asset de URP y `SceneQualityApplier` sobre el mundo armado sin rehacerlo; la RT se rehace en el siguiente dibujo si cambia el formato | **Cerrado** |
| **REN-10** | = QA-V6a-01 | — | **Cerrado** |

Los menores de la ronda 1 que el Architect dio por resueltos y QA vio de paso: QA-V6a-04 (CAP-22 en su lugar), -07 (`NoAllocationsPerFrameWithTheHudAndTheScene`: 0 en 120 cuadros con el HUD, los dos paneles y la escena, con un borde de segmento adentro, de 247,1 a 255,1 m, y el control positivo), -10, -11 y -13 (`-seed F4B` y `-rowmate-perf-long-series`): cerrados. **QA-V6a-05, no**: QA-V6a-R2-03.

## R2.2 Las tomas que cambiaron desde la ronda 1

Contra la corrida A de la ronda 1 (igual a lo publicado entonces) cambiaron **solo** CAP-22, `it6a-low`, VID-09, VID-11, `INDEX.md` y `manifest.json`. QA miró cada una:

| Toma | Lo que se ve | Contra spec / LEG |
|---|---|---|
| CAP-22 (TV) | El diálogo de completado centrado y opaco, el HUD velado, a los costados del diálogo los pinos y la orilla de ≈ 120 m (otros que en la ronda 1), el lago abierto abajo | AC-SCN-02-07 con el texto nuevo: el bote tapado y su zona afirmada con la cámara real (§R2.1). Sin LEG (el legacy no tiene toma aérea) |
| `it6a-low` (W1280) | El lago a las 12 h en Bajo: sol con halo, 7 nubes, nieve, los pinos, la cabaña, el casco; sin temblor ni peleas de profundidad | Su texto dice que la profundidad de GLES queda NO VERIFICABLE hasta el teléfono (QA-V6a-REN-16) |
| VID-09 | El fundido del HUD al rotar (§R2.1) | AC-WKT-17-07 |
| VID-11 | El fundido de la escena demorada sobre el provisorio (§R2.1) | AC-SCN-01-07, AC-APP-05-08 |

DEMO-03 **no cambió**: sigue a los 39,2 s (QA-V6a-R2-03). Contra `docs/image-03.png` del legacy, nada nuevo respecto de la ronda 1 (la vista del casco desde atrás es la misma geometría; sin remero hasta la It. 7).

## R2.3 Corridas

Desde la raíz del repo, con el Editor cerrado y una corrida por vez (los números, en §R2.0):

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it6a/qa-main-r2
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform EditMode [-Simulator] -ArtifactsPath <scratch>/...
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode [-Simulator] -ArtifactsPath <scratch>/...
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode -Simulator -Filter SceneBudgetTests   (dos veces)
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -UnityResults <scratch>/it6a/qa-main-r2/unity-xml
powershell -NoProfile -File tools/unity/capture-snapshots.ps1 -Set it6a|it5|it4b -Out <scratch>/...
powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator
```

Lo que dejan los registros de PlayMode: `[SBT]` por nivel igual que en la ronda 1 (Alto 89-90 dibujos / 60-61 SetPass / 215 k triángulos; Medio 69-70 / 45-46 / 155 k; Bajo 56-57 / 32-33 / 106 k); `[SBT] 0 B` en las cuatro escenas y con el HUD, los paneles y la escena; `[SLOT]` solo las dos líneas del *slot* de prueba (AC-SCN-15-08); la UI sin escena, 726 dibujos / 363 SetPass (QA-V6a-REN-14, como estaba).

`ProjectSettings/` y `Assets/RowMate/Settings/`: el SHA-256 de cada archivo, tomado antes de la primera corrida, es igual al de después de todas (pruebas, tres arnés, build, siembras); `git status` de `ProjectSettings`, `Settings`, `Scenes`, `Slots` y `Art/Materials/Scene`, igual antes y después; la guardia de los *scripts* (que ahora también mira esas tres carpetas, REN-18) dijo "sin cambios" en cada corrida.

## R2.4 `[PERF]` en el build (CPU del hilo principal; GPU de `FrameTimingManager`)

`ROWMATE_DATA_DIR` en una carpeta propia de `<scratch>` por corrida, con la base sembrada por QA (`SnapshotCli.SeedDatabase -seed F5 -prefs laps,reps` o `-seed F4B`, copiada como `rower_app_dev.db`), ventana de 1280 × 720, Alto, resolución 1,00 todo el tiempo. Ningún `RowMate.exe` del usuario estaba abierto.

| Corrida | Entrada (2,5 s) | Ventanas de 10 s después de la entrada | Salida (0-1 s) | La escena |
|---|---|---|---|---|
| `-rowmate-perf-entries 10 -rowmate-perf-routines "Prueba 4a;Cinco reps"` (paneles abiertos) | 1.ª (libre): **16,7 ms de CPU, 16,8 del hilo principal, en el cuadro del toque, con movimiento** (QA-V6a-R2-04); 2.ª a 10.ª **6,7 a 12,3 ms**, el peor en el armado del HUD (224-225 ms, negro y quieto); con movimiento ≤ 6,5 ms | paneles: máx ≤ 12,3 ms, p99 ≤ 3,0 ms, 124-130 MB; dibujos 89-104 / SetPass 61, sin crecer | 4,2 a 8,4 ms | 0 armados de la orilla a la vista |
| `... -rowmate-perf-entries 3 ... -rowmate-scene-delay 1.5` | 14,5 / 9,1 / 8,2 ms | — | 4,5 a 8,2 ms | **peor cuadro de la preparación 6,13 / 4,91 / 3,84 ms**; llega en las tres |
| `-rowmate-perf-scene 300` (libre con `Fuerte`) | 22,7 ms (el toque del *driver*) | **29 de 29 con máx 3,1-3,9 ms, p99 2,6-2,8 ms**, GPU ≤ 5,1 ms, 124 MB estables; 22-24 cuadros por ventana con ≈ 85 asignaciones (el HUD a 1 Hz) | 8,2 ms | 1 recolección en 5 min, sin pico |
| `-rowmate-perf-long-series 150` (`Serie larga`, `Fuerte`) | 23,0 ms (el toque) | **máx ≤ 7,7 ms**, p99 3,0-5,6 ms, GPU ≤ 6,0 ms; **cada ≈ 30 s, una ventana con ≈ 840 cuadros con asignaciones (máx 169-184 por cuadro), p99 5,2-5,6 ms y la memoria de 135 a 178-186 MB** (QA-V6a-R2-01) | 12,9 ms | — |
| `-rowmate-perf-completion` (`Corta` con `StartRoutine` directo) | 24,7 ms (el toque) | máx 9,7 / 3,9 / 11,8 ms (la entrada del diálogo); **de 10 a 30 s, 860 y 1 647 de 1 649 cuadros con asignaciones** (R2-01) | 10,2 ms | el diálogo quieto a 60 fps (REN-12, del PO) |
| `-rowmate-perf-hours` (la hora fija antes del toque; 18 → 23 h) | 21,1 ms (el toque) | máx 3,1 ms, p99 2,6 ms | 7,8 ms | — |

Lectura:

- **AC-SCN-01-10 pasa**: p99 ≤ 2,8 ms y máx ≤ 3,9 ms en las 29 ventanas de 5 min, ≤ 7,7 ms en `Serie larga`, ≤ 12,3 ms con los paneles; ningún cuadro de régimen de más de 16,7 ms. El cuadro de 93,5 ms de una corrida del Architect no apareció.
- **AC-WKT-13-15 pasa por el camino del usuario** (§R2.5: 13,1 y 12,6 ms la primera entrada del proceso; ≤ 9,9 ms las siguientes, ≤ 6,5 ms con movimiento) y en el *driver* de entradas de la 2.ª a la 10.ª. La primera del *driver* dio 16,8 ms del hilo principal en el cuadro del toque, 0,1 ms por encima del tope, contra 12,7 ms en la corrida del Architect: margen fino y variable en el primer toque del proceso (R2-04). Los *drivers* con `StartRoutine` directo o con la hora cambiada por programa siguen en 21-25 ms en su toque, como dice su propio registro (QA-V6a-06).
- **AC-WKT-14-19 pasa** con la escena (máx 7,7 ms en `Serie larga`), con la observación de R2-01.
- **La escena apagada en Workout** (AC-SCN-01-06): con la app quieta en Workout, el motor 3D del proceso promedió **2,2 %** contra **4,1 %** en el inmersivo (contador `GPU Engine` del proceso, 5 muestras de 1 s cada uno): la mitad, como en la ronda 1.

## R2.5 Prueba a mano en el build (entrada sintética con el código de rastreo y `mouse_event` absoluto; ayudas en `<scratch>`)

Carpeta de datos temporal con la base `F-5` sembrada; las preferencias de la ventana del *player* (registro de Windows) resguardadas antes y restauradas al final, iguales al resguardo; la carpeta de datos del usuario igual por nombre, tamaño, fecha y *hash* (salvo el `TestResults.xml` que el Test Framework escribe en cada PlayMode y `run-unity-tests.ps1` borra, O-R2-01 de la 4b: lo escribió y lo borró esta misma verificación).

| # | Qué se hizo | Qué se vio | |
|---|---|---|---|
| 1 | Workout → `Free workout` (el HUD en el idioma del sistema) | A los 0,6 s la cámara baja y al costado, ladeada, con el bote cruzado; al 1 s llegando; a los 3 s en la pose detrás del bote, con la luz de la hora real; el HUD entra con la cascada. **Entrada 13,1 ms** (el cuadro del toque) | Bien (AC-SCN-02-05, AC-WKT-13-06, AC-WKT-13-15) |
| 2 | Llave → `Fuerte` | El bote acelera (split 1:49); los pinos pasan; el muelle con sus pilotes a la izquierda | Bien (AC-SCN-03-01, AC-SCN-15-03) |
| 3 | `Hora escena` `+`: 19 → 22 h; → 6 h (por 23 y 0); → 12 h; → 18 h | **22 h**: noche con la luna en su fase (gibosa, con el terminador), estrellas, el lago oscuro; a mitad de la transición se ve el paso intermedio. **6 h**: amanecer violeta y rosado, estrellas tenues. **12 h**: cielo azul, sol alto, nieve, lago celeste. **18 h**: atardecer, el sol bajo detrás de los pinos de la derecha. Cada cambio, gradual (≈ 1,5 s). El reloj de pared, siempre en la hora real | Bien (AC-SCN-23-01, -02, -04, AC-SCN-10-07, AC-SCN-11-03, AC-BLE-16-07) |
| 4 | `Pause` → `Resume` | El velo (menos color, más oscuro), el disco `PAUSED`, el HUD a pleno, el bote que se desliza y para; al reanudar se va | Bien (AC-WKT-13-12, AC-SCN-03-03) |
| 5 | `Finish` → `Finish` | El HUD se va, la escena funde a negro (a los 0,25 s la escena sola, oscureciéndose) y vuelve Workout. Salida 7,9 ms | Bien (AC-SCN-02-06, AC-WKT-13-08) |
| 6 | `Corta` desde la lista → `Start`, hasta el final | La rutina con el atardecer y la cuenta grande `3` del descanso; a los 30 s el diálogo de completado; a los 34 y 39 s, el diálogo quieto con la escena detrás, velada, y la popa asomando debajo. Entrada 9,9 ms (negro y quieto), 6,5 ms con movimiento. `Finish` del diálogo → Workout | Bien (AC-SCN-02-07 con el texto de P-19, AC-WKT-21-01) |
| 7 | Workout quieto, 5 s | GPU del proceso 2,2 % (contra 4,1 % en el inmersivo); nada animando | Bien (AC-SCN-01-06) |
| 8 | Libre otra vez y, a los 20 s, **matar el proceso** | Una copia de la base, leída con `RowMate.SessionList --db`: 3 sesiones; la libre cortada **sin fin y con 3 puntos** (cada 5 s); `Corta` (7 puntos) y la primera libre, completas | Bien (P-03 de la 4a) |
| 9 | Volver a abrir sobre la misma carpeta, llave en Workout | Arranca sin avisos; **`Hora escena` en `real`**; `−` una vez (17:00) desde Workout y `Free workout`: la escena a las 17 h, **entrada 12,6 ms**. Después se cerró la ventana con esa sesión abierta (el clic en `Finish` del diálogo de confirmación cayó sobre el panel del simulador, que había quedado abierto encima: un error de la ayuda de QA, no de la app): la base queda con esa sesión sin fin y 1 punto, consistente | Bien (AC-SCN-23-03) |

En los dos registros: 0 `[SLOT]`, 0 avisos, 0 errores, 0 excepciones; `[SCENE] escena: nivel High`; "shaders de la escena o de URP después de Ready: 0" en las cuatro entradas.

## R2.6 Revisión estática

- **La presentación no toca el dato.** `PureCodeRulesTests` en verde (las reglas de la escena con su control positivo). Lo nuevo de esta ronda (`StreamHidden`, `CatchUp`, `PrepareNextSession`, `LowerTier`, `NoteUserInput`) lee la sesión y la entrada del usuario; no escribe en el motor, la sesión ni la telemetría. `MotionDriver.NoteUserInput` guarda un `double` y un `bool` (sin asignar).
- **Nada sigue al salir.** La escena se cierra con la salida; Workout en reposo con la GPU en la mitad que el inmersivo; `DropBuild` suelta un armado a medias con `Dispose`.
- **0 B por cuadro con la escena**: `SceneBudgetTests.NoAllocationsPerFrame` (cuatro escenas) y `NoAllocationsPerFrameWithTheHudAndTheScene` (todo el C# del cuadro, con el control positivo adentro): 0. En el *player*, remando en el libre con la escena, solo la actualización de 1 Hz del HUD. Lo de las rutinas es del HUD (R2-01).
- **Slots y terceros.** Los 52 *slots* con `_source: 0` (`Procedural`) y `_asset: {fileID: 0}`; `Assets/ThirdParty/` no existe y `git ls-files Assets/ThirdParty` está vacío; 0 `[SLOT]` en el build.
- **Licencia BSD-3**: `THIRD_PARTY_NOTICES.md` junto al ejecutable, igual byte a byte al de la raíz.
- **`ProjectSettings`**: sin cambios dejados por las pruebas (§R2.3).
- **Repo público**: `git grep -nIE` de rutas de usuario de Windows, de Git Bash y del prefijo de las carpetas de trabajo sobre lo versionado: 0; la misma búsqueda sobre los archivos nuevos sin seguimiento: 0. Búsqueda de nombres de hardware, del usuario de la PC y de la carpeta de trabajo en lo nuevo y lo modificado: solo los nombres genéricos de catálogo de `Platform/Display/GraphicsTier.cs` y de `Tests/UnityEditMode/GraphicsTierTests.cs` (armados en tiempo de ejecución), que no son de este equipo. Esta sección usa rutas relativas, `<scratch>` y la URL del legacy, y no da datos del equipo ni horarios del usuario (las horas que nombra son las de la escena).
- **Legacy**: `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó el módulo de credenciales.

## R2.7 Issues de la ronda 2

| ID | Sev. | Dueño | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V6a-R2-01 | **mayor** | architect | **El HUD con rutina asigna memoria en cada cuadro en el *player***, en los ≈ 5 s finales de cada paso (el halo y la cuenta grande) y en tramos de `Corta`: ≈ 840 cuadros por ventana de 10 s con 136-300 asignaciones cada uno, la memoria de Unity de 135 a 178-186 MB en esas ventanas y el p99 de 3,0 a 5,5 ms. No es la escena (en el libre con la escena, solo el HUD a 1 Hz; `SBT`, 0 en la escena). Contradice ARCHITECTURE §20.2 (0 B por cuadro del HUD) en el teléfono, con el GC de Boehm. Las pruebas `HBT` de 0 B no cubren esa franja: `LongSeriesStaysSmooth` (26,2-31,5 s de `Serie larga`) mide solo tiempos. Viene de la 4b/5: el contador por cuadro es nuevo de esta ronda y por eso no se vio antes | `<scratch>/it6a/qa-main-r2/runs/longseries/player.log` (las ventanas de los 25, 57, 85 y 120 s); `runs/completion/player.log` (10-30 s); `Tests/PlayMode/HudBudgetTests.cs:88-119` | Como deuda al empezar la 6b: ubicar la asignación con el Profiler del build de desarrollo en los últimos 5 s de un paso, corregirla y sumar a `LongSeriesStaysSmooth` (o a una prueba hermana) la cuenta de `FrameAllocations` de 26,2 a 31,5 s, con el control positivo |
| QA-V6a-R2-02 | menor | architect | **`MemoryAndSceneCpuPerTierWithinTheBudget` no es estable**: (a) la CPU de la escena p95 en Alto dio 1,24, **1,51** (falla, tope 1,5), 1,18 y 1,16 ms en cuatro corridas: una prueba de tiempo de pared en el Editor con ≈ 10 % de margen; (b) las RT que suma la escena dependen del orden de las pruebas (Alto 46,9 / 68,0 MB, Medio 51,2 / 41,9, Bajo 38,2 / 26,5), así que la columna de §V1-13 no mide la escena sola | XML de PlayMode con simulador y las dos repeticiones con `-Filter SceneBudgetTests` | (a) el p95 de varios tramos (o la mediana de varias corridas) o la categoría `Perf` para la parte de tiempo, con el tope del teléfono; (b) contar las RT de la escena (la de la sesión y los intermedios de su cámara) en lugar de la diferencia de "Render Textures Bytes" del proceso |
| QA-V6a-R2-03 | menor | architect | **DEMO-03 sigue a los 39,2 s** (QA-V6a-05 sin aplicar): la spec corregida pide la toma a los 36,2 s, con el muelle a la izquierda acercándose; la toma publicada no lo muestra | `Snapshots/SceneCatalog.cs:164` y `:197` (`AdvanceTo(39.2)`); *manifest* de DEMO-03; 01-po-spec §7.4 y "Cambios por verificación (ronda 1)" §2 | `AdvanceTo(36.2)` y el texto del estado; regenerar el conjunto (dos corridas) |
| QA-V6a-R2-04 | menor | architect | **La primera entrada del proceso tiene poco margen y la sonda no ubica el cuadro con su anillo**: en `-rowmate-perf-entries`, 16,7 ms de CPU y 16,8 ms del hilo principal en el cuadro del toque (con movimiento) contra 12,7 ms en la corrida del Architect; por el camino del usuario, 13,1 y 12,6 ms. En todas las entradas la sonda escribe "el peor sin el cuadro (otro reloj o fuera de la memoria)": la ubicación sale solo del `ProfilerRecorder` | `runs/entries10/player.log` (entrada 1); `manual/session1.log`, `session2.log`; `SimulatorPresentation/FrameTimeProbe.cs:445`, `:526` | Ver qué corre por primera vez en el toque de la primera entrada (el JIT del camino de la entrada: ejercitarlo en la precarga en reposo, como la sesión siguiente) para recuperar margen; sacar el anillo o arreglar su correspondencia y dejar el dato del `ProfilerRecorder` |
| QA-V6a-R2-05 | menor | architect | **Lo que queda de REN-06**: en la precarga en reposo, unidades de 19,3-19,8 ms (el primer pedido de URP a la RT chica), 13 ms (el primer `Tick` sintético) y ≈ 9 ms (el dibujo de calentamiento), contra el objetivo de 1,5 ms de §2.16. En la PC caen con Workout quieto a 30 fps y la entrada del usuario las frena: QA lo acepta para la 6a | `[PERF] escena (n.º 1): … peor unidad 19.25-19.83 ms (post de URP (RT chica))` en las seis corridas | Medirlo en el teléfono (It. 14) y, si hace falta, el JIT por adelantado en el arranque |

Siguen como estaban y fuera de esta ronda: QA-V6a-REN-12 (P-07, del PO), REN-13 y REN-14 en parte, QA-V6a-08 (las sondas de la ventana y la cabaña, que QA mide en los PNG) y QA-V6a-12 (el aspecto, con P-18 para el usuario).

**Resumen: 0 bloqueantes, 1 mayor (R2-01, propuesto como deuda al empezar la 6b) y 4 menores.**

## R2.8 Criterios de aceptación (los que cambiaron desde la ronda 1)

Los 96 que pasaban en la ronda 1 siguen en PASS con las suites de esta ronda en verde (la única roja, AC-SCN-01-11 por R2-02, pasa en las dos repeticiones). Los que cambian o suman evidencia:

| ID | Ronda 1 | Ronda 2 | Evidencia |
|---|---|---|---|
| AC-SCN-01-07 | FAIL | **PASS** | VID-11 cuadros 46-52 (la mezcla); `TheLateSceneBlendsOverTheBackdrop`; `TheDelayedSceneArrivesWithTheShoreOfTheSession`; build: la demorada llega en las tres entradas, peor cuadro de la preparación ≤ 6,13 ms |
| AC-APP-05-08 | FAIL (parte) | **PASS** | La misma evidencia del fundido de la escena demorada; VID-02 y las pruebas de la ronda 1 |
| AC-WKT-17-07 | FAIL | **PASS** | VID-09 cuadros 15-21; `TheHudFadesInWhenTheWindowChangesClass` (con reducir movimiento, en el acto) |
| AC-SCN-02-07 | FAIL | **PASS** (texto de P-19) | CAP-22: `Aerial`, −4°, popa en 553,175 dp dentro de la zona, la escena a 119,872 m; VID-08; build |
| AC-SCN-01-11 | PASS (parcial) | **PASS** | Dibujos, SetPass y triángulos; memoria de texturas y RT; CPU de la escena p95 ≤ 1,5 ms en 3 de 4 corridas de Alto y en todas las de Medio y Bajo (R2-02) |
| AC-SCN-01-12 | PASS | PASS | Ahora con el HUD, los paneles y la escena en la misma medida. En el *player* con rutina, el HUD asigna (R2-01, fuera de la escena) |
| AC-SCN-01-13 | PASS (PC) | PASS (PC) | Ahora con `LoweringTheTierLowersTheWholeSceneProfile`; [Device-And] NO VERIFICABLE |
| AC-WKT-13-15 | PASS | PASS | Camino del usuario 13,1 / 12,6 ms; *driver* 2.ª a 10.ª ≤ 12,3 ms (con movimiento ≤ 6,5); demorada ≤ 14,5 ms; salidas ≤ 12,9 ms; 0 subidas de *shaders* después de `Ready`. La 1.ª del *driver*, 16,8 ms (R2-04) |
| AC-SCN-01-10 | PASS | PASS | §R2.4: régimen p99 ≤ 2,8 ms y máx ≤ 3,9 ms (5 min), ≤ 7,7 ms en `Serie larga` |
| AC-WKT-14-19 | PASS (PC) | PASS (PC) | `Serie larga` con la escena en el build: máx 7,7 ms; [Device-And] NO VERIFICABLE |
| AC-SCN-01-09 | NO VERIFICABLE | NO VERIFICABLE | Sin módulo de Android ni teléfono (It. 14) |

**Totales: 100 PASS (4 con una parte de Android NO VERIFICABLE), 0 FAIL, 1 NO VERIFICABLE.**

## R2.9 Para el coordinador

1. **La 6a puede ir al REPORT.** Los mayores de la ronda 1 están cerrados con evidencia en la imagen, en las pruebas y en el build.
2. **Deuda al empezar la 6b** (propuesta): QA-V6a-R2-01 (las asignaciones del HUD con rutina) y, si el Architect no los hace antes del REPORT, R2-02 a R2-04, que son baratos (R2-03 pide regenerar `it6a` con dos corridas).
3. **Para el usuario (gate)**: P-18 y P-19 (la toma aérea detrás del diálogo hasta el vidrio de la 6b) y QA-V6a-12. Siguen fuera del alcance de QA la demo (c) de la 5 (AC-WKT-02-11) y el teléfono (AC-SCN-01-09).
4. **Nada quedó cambiado**: ningún Unity ni `RowMate.exe` abierto por esta verificación; el registro del *player* y la carpeta de datos del usuario, iguales al resguardo; `ProjectSettings` y `Settings`, iguales; el legacy, sin cambios.

---

# Ronda 3

| Campo | Valor |
|---|---|
| Autor | rowmate-qa (verificación principal, ronda 3; el único agente de la verificación que usó Unity) |
| Fecha | 2026-11-06 |
| Entrada | Lo de las rondas 1 y 2 más [02-tech-design](02-tech-design.md) "Cambios por verificación (ronda 2)" (V2-01 y V2-M1 a V2-M3) |
| Re-verificados uno por uno | QA-V6a-R2-01 a -05 (§R3.1) y, de las lentes, los de [paridad](05-qa-lens-paridad.md) y [rendimiento](05-qa-lens-rendimiento.md) que seguían en la mesa (§R3.2) |
| Artefactos | `<scratch>/it6a/qa-main-r3/` (dotnet, los cuatro XML de Unity y sus registros, la repetición de `SceneBudgetTests` y `HudBudgetTests`, las corridas del arnés `it6a`, `it5`, `it4b` e `it4a`, recortes y comparaciones, el build, las seis corridas `[PERF]` en `runs/`, la prueba a mano en `manual/` con sus capturas, registros y copias de la base) |
| Código | El mismo de la ronda 2 del Architect: ningún fuente, *shader*, prueba, *script* ni `Packages/` cambió después de su último build (comprobado con las fechas de todo el árbol salvo `Library`, `Temp`, `Logs`, `obj`, `Builds` y los `.csproj` que regenera el IDE). Las corridas de Unity de esta ronda empezaron justo después y se completaron con las de hoy (§R3.3) |
| Repo | Sin *commits*. Esta ronda escribió en el repo solo esta sección (y las líneas de §0) y el build de `Builds/Windows/simulator` (ignorado). No se tocó ningún fuente |

## R3.0 Veredicto

**APROBADO CON OBSERVACIONES.** Los cinco issues de la ronda 2 están **cerrados** (R2-04 y R2-05 con la parte que ya quedó como deuda): el HUD con rutina **ya no asigna en cada cuadro** en el *player* (de ≈ 840 a 49-53 cuadros con asignaciones por ventana de 10 s, la memoria se queda en 135-151 MB), DEMO-03 muestra el muelle, la prueba de CPU de la escena usa la mediana y la sonda de la entrada ya no dice "sin el cuadro". Pero la prueba a mano encontró un camino de usuario que ningún *driver* mide, y quedaba abierto un menor de la lente de paridad:

- **QA-V6a-R3-01 (mayor, architect)**: con la precarga de la escena **sin terminar** (abrir la app, ir a Workout y tocar `Free workout` en pocos segundos: el primer arranque normal), la entrada tiene un cuadro de **19,7-20,0 ms** en el toque y otro de **20,7-20,8 ms** a los ≈ 5,4-5,8 s, los dos con movimiento (el fondo provisorio y el HUD a la vista). AC-WKT-13-15 pide que la escena demorada se prepare "sin que ningún cuadro pase de 16,7 ms": **FAIL** en ese camino (2 de 2 arranques; con la escena lista, 13,6 ms).
- **QA-V6a-PAR-01 (menor, designer)**, de la lente de paridad: la **11.ª línea de brillo** del agua degradada en la fila de abajo sigue en las tomas publicadas. Ninguna ronda lo tomó (la ronda 1 dio AC-SCN-13-03 por PASS; la lente lo daba FAIL en la captura). **FAIL** en pantalla.

**Criterios: 98 PASS · 2 FAIL · 1 NO VERIFICABLE** (AC-SCN-01-09, Android). Ninguno rompe la sesión ni el dato, y nada pasa de 33 ms (AC-SCN-01-10 sigue en PASS). R3-01 y PAR-01 se pueden corregir en una ronda corta o, si el coordinador y el PO lo aceptan, ir como deuda al empezar la 6b junto con R2-04 (los dos son "lo que corre por primera vez en un cuadro a la vista"); QA no recomienda cerrar el REPORT sin una de las dos decisiones escrita.

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix`, dos corridas) | Compilación **0 avisos, 0 errores** · herramientas **192 / 192** · Core **3049: 3010 pasan, 0 fallan, 39 ignoradas** · matriz sola: PASA 615, FALLA 0, IGNORADO 4, PENDIENTE UNITY 143, SIN TEST 0 |
| Unity EditMode | **3137: 3092 pasan, 0 fallan, 45 ignoradas**, 0 inconclusas; guardia: sin cambios |
| Unity EditMode con simulador | **3329: 3284 pasan, 0 fallan, 45 ignoradas**; el *define* restaurado (Unity revienta al cerrar después de cambiar el *define*; el *script* lo absorbe); guardia: sin cambios |
| Unity PlayMode | **481 / 481**; guardia: sin cambios |
| Unity PlayMode con simulador | **553 / 553**; guardia: sin cambios. Repetición `-Filter "SceneBudgetTests\|HudBudgetTests"` con simulador: **21 / 21** |
| Matriz con los 4 XML | 762 IDs: **PASA 758, FALLA 0, IGNORADO 4**, PENDIENTE UNITY 0, SIN TEST 0 (la matriz automática no ve R3-01 ni PAR-01: uno sale de la prueba a mano en el build y el otro de la imagen) |
| Arnés `it6a` | **45 tomas, 0 con problemas, 0 sondas que fallan**; la corrida de QA es **igual byte a byte** a la publicada (46 archivos), que es la B del Architect: dos corridas iguales; `manifest.json`, solo `durationMs`. Contra la ronda 2 cambian **solo DEMO-03**, su línea de `INDEX.md` y el *manifest* (§R3.4) |
| `it4a` / `it4b` / `it5` | Corridas de QA (49, 51 y 47 tomas, 0 con problemas) **iguales byte a byte** a lo que hay en el árbol; `manifest.json`, solo `durationMs`. Contra la ronda 2 cambian solo CAP-22 de la 4b y VID-04 y VID-09 de la 5 (§R3.4). `it2` / `it3`: sin cambios contra `HEAD`; `it4a`, solo `durationMs` en su *manifest* |
| Oráculo `-Scene` | No se volvió a correr: desde la ronda 1 no cambió nada de `tools/oracle/`, `snapshots/legacy/`, `Core/Scene/` ni del legacy (`3ae0f3a`, `status --porcelain` vacío) |
| Build de Windows con simulador | Rehecho por QA (`tools/build/build.ps1 -Target Windows -Simulator`): éxito, `THIRD_PARTY_NOTICES.md` al lado igual byte a byte al de la raíz, guardia sin cambios. Los `RowMate.*.dll` son los del último build del Architect (Unity no reescribe un ensamblado igual): el código final |
| `[PERF]` | Seis corridas sobre el build, con la PC sin otra carga (§R3.5): **0 subidas de *shaders* después de `Ready`** en las seis (`perf-log-check.ps1` sale con 0); 5 min de libre con `Fuerte`: **29 ventanas con máx 3,0-5,0 ms, p99 2,6-2,9 ms** |
| Prueba a mano en el build | Hecha (§R3.6): vuelo de entrada, `Fuerte`, `Hora escena` 18, 22, 6 y 12 h, pausa con el velo, salida, `Corta` hasta el diálogo con la toma aérea, matar el proceso a mitad, reabrir (la hora vuelve a `real`) y un arranque más para medir la entrada temprana. Todo bien salvo R3-01 |
| Repo público | `git grep -nIE` de rutas de usuario de Windows, de Git Bash y del prefijo de las carpetas de trabajo sobre lo versionado: **0**; sobre lo nuevo sin seguimiento y lo modificado: **0**. Sin datos del equipo, del hardware ni del usuario en esta sección, el *manifest* ni el `INDEX.md` (§R3.7) |

## R3.1 Los issues de la ronda 2, uno por uno

| Issue | Qué se corrigió (02-tech-design, V2) | Cómo lo verificó QA | Estado |
|---|---|---|---|
| **QA-V6a-R2-01** (mayor; AC-SCN-01-12, AC-WKT-14-19, AC-WKT-17-06) | V2-01: el color del latido de la cuenta en el dibujo (`TabularNumber.SetPaintColor`), la escala solo si cambia; el halo de la fila lo dibuja la barra (`PillRowView.HostHalosIn`, `PillRowMotion.HaloRepaint`); la prueba `HudBudgetTests.LongSeriesNoAllocationsPerFrame` mide también el trabajo del panel de UI Toolkit | **Código**: `Presentation/Motion/StepPulse.cs:265-297` ya no escribe `style.color` por cuadro; `style.scale` solo si cambió; la opacidad de Próximo solo si cambia (`:241-246`). **Pruebas** (tres corridas: PlayMode, PlayMode con simulador y la repetición): `[HBT] Serie larga de 26,2 a 31,5 s`: **318 cuadros, 20 / 20 / 22 con asignaciones, tramo seguido más largo 12** (el cambio de paso, 30,000-30,183 s), **1 en el tramo quieto**; el control positivo se ve en 30 de 30 cuadros. **En el *player*** (`-rowmate-perf-long-series 150`, base `-seed F4B`): las ventanas con los últimos 5 s de un paso dan **49-53 cuadros con asignaciones** (antes ≈ 840), máx 134-181 por cuadro, **memoria 135 MB** (las otras ventanas, 27-32 cuadros, 135-151 MB), p99 5,2-5,6 ms y máx ≤ 7,8 ms. `-rowmate-perf-completion` (`Corta`) de 10 a 30 s: **73 y 50 cuadros** (antes 860 y 1 647) | **Cerrado**. Lo que queda por ventana son eventos (cambio de paso, lecturas del remo, la actualización de 1 Hz) que ARCHITECTURE §20.2 admite |
| **QA-V6a-R2-02** (menor; AC-SCN-01-11) | V2-M1: la CPU de la escena en tres tramos de 120 cuadros y la **mediana de sus tres p95** contra 1,5 ms | `[SBT] memoria y CPU High`: **mediana 1,31 / 1,39 / 1,38 ms** (tramos 1,31-1,38 / 1,38-1,41 / 1,34-1,44) en las tres corridas de QA; Medio 1,05-1,07; Bajo 0,64-0,79. Las tres pasan; el Architect había medido 1,15-1,16 en Alto. La parte (b), las RT que suma la escena según el orden de las pruebas (Alto 46,9 MB en la corrida completa y 68,0 MB en la filtrada; Medio 51,2 / 41,9; Bajo 38,2 / 26,5), queda como dice "Lo que queda" (It. 14, con el Memory Profiler) | **Cerrado** (a); (b) aceptado para la It. 14. **Observación**: el margen de Alto en este equipo es ≈ 7 % (1,39 contra 1,5): la mediana quita los picos sueltos, no una corrida entera más lenta. Si vuelve a fallar, pasar la parte de tiempo a la categoría `Perf` |
| **QA-V6a-R2-03** (menor; = QA-V6a-05) | V2-M2: DEMO-03 a los 36,2 s | La toma nueva (§R3.4): el muelle con sus pilotes a la izquierda, acercándose, a 168 m; el texto del estado lo dice. DEMO-04 en adelante, iguales byte a byte | **Cerrado** |
| **QA-V6a-R2-04** (menor; AC-WKT-13-15) | V2-M3: la sonda ya no busca el cuadro por `frameStartTimestamp`; la ubicación sale del "Main Thread" del `ProfilerRecorder`. El margen de la primera entrada quedó como deuda de la 6b | En las 19 entradas de los *drivers* y las 4 de la prueba a mano la línea dice "hilo principal (Main Thread): máx … (a los N ms del toque, con movimiento / negro y quieto, GC +k)"; **ninguna dice "sin el cuadro"**. La primera entrada de `-rowmate-perf-entries 10` dio **14,0 ms** (en la ronda 2, 16,8): dentro del tope, con un margen que varía entre corridas | **Cerrado** (la sonda); el margen, **deuda 6b** como se acordó. Ver R3-01, que es el mismo tipo de costo con la escena sin precargar |
| **QA-V6a-R2-05** (menor) | Aceptado por QA para la PC; el teléfono en la It. 14 | En la precarga en reposo siguen las unidades de la primera vez: post de URP (RT chica) **22,1 ms**, `Tick` sintético 14,1 ms, dibujo de calentamiento 11,3 ms (`[PERF] escena (n.º 1)` de `entries10`) | **Cerrado como estaba** (aceptado en la PC con Workout quieto). **Pero** la premisa de la aceptación ("caen con Workout quieto") no vale si el usuario entra antes de que la precarga termine: ver R3-01 |

## R3.2 Los de las lentes

- **Lente de rendimiento** (QA-V6a-REN-01 a -19): los diez mayores se cerraron en la ronda 2 y siguen cerrados con el código de esta ronda (0 subidas de *shaders* después de `Ready` en las seis corridas, 0 armados de la orilla a la vista, la escena demorada del *driver* con el peor cuadro de su preparación en **7,17 / 3,76 / 5,26 ms**, `ClosingWhileLoadingLeavesNoNativeBuffers` y `LoweringTheTierLowersTheWholeSceneProfile` en verde). Siguen como estaban REN-12 (P-07, del PO), REN-13 y REN-14 en parte (It. 14 / QA). REN-06 (las unidades de la precarga) reaparece en R3-01: el *driver* `-rowmate-scene-delay` demora la escena **después** de que la precarga terminó, así que no mide el caso real de la escena demorada (la precarga sin terminar).
- **Lente de paridad** (QA-V6a-PAR-01 a -05): PAR-02 = QA-V6a-08 sigue como estaba (QA mide en los PNG); PAR-03 y -04 los corrigió el PO en la ronda 1; PAR-05 = QA-V6a-11, cerrado. **PAR-01 sigue abierto y nadie lo tomó**: `Art/Shaders/Scene/WaterDegraded.shader:176` elige la línea con `n = max(round(√tp / 0,1 + fase), 0)`, sin tope en 9. Sonda de QA en las tomas publicadas, la fila de abajo contra la de arriba: CAP-02 y CAP-07 (V390) `#18518B` / `#0B4988` en x 195; CAP-12 (W1280) `#18518B` / `#0B4988` en x 640; CAP-04 (noche) `#142037` / `#07122A`. Es una línea de 1 px con alfa 0,05 en el borde de abajo de la pantalla: AC-SCN-13-03 pide 10 y el legacy dibuja 10.

## R3.3 Corridas

Desde la raíz del repo, con el Editor cerrado y una corrida por vez (los números, en §R3.0):

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it6a/qa-main-r3   (y otra a .../dotnet2)
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform EditMode|PlayMode [-Simulator] -ArtifactsPath <scratch>/...
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode -Simulator -Filter "SceneBudgetTests|HudBudgetTests"
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -UnityResults <scratch>/it6a/qa-main-r3/unity-xml
powershell -NoProfile -File tools/unity/capture-snapshots.ps1 -Set it6a|it5|it4b|it4a -Out <scratch>/...
powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator
```

La primera repetición de Unity salió con 198 (sin licencia: Unity Hub estaba cerrado); se abrió el Hub y se repitió. `ProjectSettings/` y `Assets/RowMate/Settings/`, `Scenes`, `Slots` y `Art/Materials/Scene`: el SHA-256 de cada archivo, tomado antes de la primera corrida, igual al final; `git status --porcelain` del repo, igual antes y después de todo.

## R3.4 Las tomas que cambiaron desde la ronda 2

| Toma | Lo que se ve (QA la miró con Read) | Contra spec / LEG |
|---|---|---|
| `it6a` DEMO-03 (W1280) | 00:36, 168 m, `Fuerte` (split 1:48, 280 W) a las 10 h: a la izquierda **el muelle de tablones con cuatro pilotes**, entrando en el cuadro sobre la orilla; pinos a los dos lados; el casco centrado. La de la ronda 2 (00:39, 182 m) no tenía el muelle | 01-po-spec §7.4 corregida ("el muelle a la izquierda, acercándose"): bien. Sin LEG (el legacy no tiene muelle). El *manifest* cambia en el estado, `takenAtSeconds` (40,7 → 37,7) y los dibujos acumulados de las tomas siguientes (+121, los 3 s que ahora se dibujan de más hasta los 39,2 s); sus PNG, iguales |
| `it4b` CAP-22 (`Serie larga`, 1080 × 2400) | Diferencia solo en un recuadro de 6 × 28 px (x 537-543, y 44-72): el borde del relleno de la en curso dentro del pulgar de la fila angosta, corrido menos de ¼ px. Ampliado × 8: indistinguible a simple vista | Lo que explica V2-01 (2): la fila ya no se repinta en cada cuadro por el halo |
| `it5` VID-04 y VID-09 | 60 de 150 y 90 de 90 cuadros con diferencias, **todas en la franja de la fila de píldoras** (x 190-225, y 14-50 a la escala del video); la peor, 52/255 en el cuadro 36 de VID-04: el borde suavizado del relleno de la en curso. El halo y el latido, iguales a la vista | Mismo motivo. Las tomas de la cuenta del paso y de la cuenta grande (4b y 5), iguales byte a byte |

Los once videos de `it6a` no cambiaron desde la ronda 2 (byte a byte), así que las hojas de cuadros de las rondas 1 y 2 siguen valiendo. Contra `docs/image-03.png` del legacy: nada nuevo (la vista del casco desde atrás es la misma geometría; sin remero hasta la It. 7).

## R3.5 `[PERF]` en el build (CPU del hilo principal; GPU de `FrameTimingManager`)

`ROWMATE_DATA_DIR` en una carpeta propia de `<scratch>` por corrida, con las bases `-seed F5 -prefs laps,reps` y `-seed F4B` sembradas por QA en la ronda 2; ventana de 1280 × 720, Alto, resolución 1,00. Ningún `RowMate.exe` del usuario estaba abierto y la PC no tenía otra carga.

| Corrida | Entrada (2,5 s) | Ventanas de 10 s | Salida (0-1 s) | Otros |
|---|---|---|---|---|
| `-rowmate-perf-entries 10 -rowmate-perf-routines "Prueba 4a;Cinco reps"` | 1.ª (libre) **14,0 ms** (14,1 del hilo principal, en el toque, con movimiento); 2.ª a 10.ª **7,5 a 9,4 ms** en el armado del HUD (224 ms, negro y quieto), **≤ 6,5 ms con movimiento** | p99 3,1 ms, dibujos 88 / SetPass 60 | 4,2 a 9,8 ms | 0 armados de la orilla a la vista (10 de 10) |
| `... -rowmate-perf-entries 3 ... -rowmate-scene-delay 1.5` | 14,7 / 13,1 (negro y quieto) / 8,0 ms | p99 3,8 ms | 4,4 a 7,9 ms | **peor cuadro de la preparación 7,17 / 3,76 / 5,26 ms**; llega en las tres |
| `-rowmate-perf-scene 300` (libre con `Fuerte`) | 21,9 ms (el toque del *driver*) | **29 de 29 con máx 3,0-5,0 ms, p99 2,6-2,9 ms**, GPU ≤ 4,9 ms, 123-124 MB, 21-24 cuadros con asignaciones por ventana (el HUD a 1 Hz) | 8,3 ms | 1 recolección en 5 min, sin pico |
| `-rowmate-perf-long-series 150` (`Serie larga`, `Fuerte`) | 22,8 ms (el toque del *driver*, `StartRoutine` directo) | **máx ≤ 7,8 ms**, p99 2,8-5,6 ms, GPU ≤ 7,9 ms, **135-151 MB**; las ventanas con los últimos 5 s de un paso, 49-53 cuadros con asignaciones (R2-01 cerrado) | 10,6 ms | — |
| `-rowmate-perf-completion` (`Corta`) | 23,9 ms (el toque del *driver*) | máx 9,5 / 4,4 / 13,4 ms (la entrada del diálogo); de 10 a 30 s, 73 y 50 cuadros con asignaciones | 10,2 ms | el diálogo quieto a 60 fps (P-07) |
| `-rowmate-perf-hours` (18 → 23 h) | 22,1 ms (el toque; la hora cambiada por programa) | máx 3,3 ms, p99 2,8 ms | 7,8 ms | — |

Lectura:

- **AC-SCN-01-10 pasa**: p99 ≤ 2,9 ms y máx ≤ 5,0 ms en las 29 ventanas de 5 min, ≤ 7,8 ms en `Serie larga`, ≤ 13,4 ms en el diálogo; ningún cuadro de régimen de más de 16,7 ms ni ninguno de más de 33 ms (tampoco en R3-01).
- **AC-WKT-14-19 pasa** (máx 7,8 ms con la escena) y **R2-01 está cerrado en el *player***.
- **AC-WKT-13-15**: con la escena **ya precargada** pasa (camino del usuario 13,6 y 9,7 ms; *driver* 1.ª 14,0, 2.ª a 10.ª ≤ 9,4; demorada del *driver* ≤ 14,7 ms con la preparación ≤ 7,17 ms; salidas ≤ 10,6 ms; los toques de 22-24 ms son de los *drivers* que arrancan con `StartRoutine` directo o cambian la hora por programa, como dice su registro, QA-V6a-06). Con la precarga **sin terminar**, falla: R3-01 (§R3.6).

## R3.6 Prueba a mano en el build (entrada sintética con el código de rastreo y `mouse_event` absoluto; ayudas en `<scratch>/it6a/qa-main-r3/tools/`)

Carpeta de datos temporal con la base `F-5` sembrada; las preferencias de la ventana del *player* (registro de Windows) resguardadas antes y restauradas al final (iguales al resguardo); la carpeta de datos del usuario igual por nombre, tamaño, fecha y *hash*. Tres arranques sobre la misma carpeta.

| # | Qué se hizo | Qué se vio | |
|---|---|---|---|
| 1 | Arranque → Workout (a los ≈ 8 s) → `Free workout` (≈ 5 s después) | **La escena no estaba lista** (`entrada … escena Loading`): la entrada siguió sobre el fondo provisorio con la cascada del HUD y la sesión corriendo; a los ≈ 5-6 s la escena **apareció con su fundido, ya al día** (a la hora real, con la distancia de la sesión). **Entrada 19,7 ms en el cuadro del toque, con movimiento; otro cuadro de 20,8 ms a los 5,39 s, con movimiento** | AC-SCN-01-07 bien; **AC-WKT-13-15 FAIL (R3-01)** |
| 2 | Llave → `Fuerte` | El bote acelera (split 1:46-1:50, 280-293 W); pinos y orilla pasan | Bien (AC-SCN-03-01) |
| 3 | `Hora escena` `+`: real → 18 h; → 22 h; → 6 h (por 23 y 0); → 12 h | **18 h**: atardecer rosado, el sol bajo a la derecha detrás de los pinos. **22 h**: noche, la luna en su fase con el halo, estrellas, líneas plateadas sobre el lago oscuro. **6 h**: amanecer violeta con estrellas tenues. **12 h**: cielo azul, sol alto, nieve, lago celeste. Cada cambio, gradual (a 0,4-0,6 s se ve el paso intermedio). El reloj de pared, siempre en la hora real | Bien (AC-SCN-23-01, -02, -04, AC-SCN-10-07, AC-SCN-11-03) |
| 4 | `Pause` → 3 s → `Resume` | El velo (menos color y más oscuro), el disco `PAUSED`, el HUD a pleno, el muelle quieto a la izquierda; al reanudar se va | Bien (AC-WKT-13-12, AC-SCN-03-03) |
| 5 | `Finish` → `Finish` | El HUD se va, a los 0,25 s la escena sola oscureciéndose, después Workout. **Salida 8,6 ms** | Bien (AC-SCN-02-06, AC-WKT-13-08) |
| 6 | La lista (rueda) → `Corta` → `Start`, hasta el final | La rutina con las píldoras, `Rest` con `Target met`; a los ≈ 30 s el diálogo de completado opaco con la escena detrás, velada, y la popa asomando debajo; a los 35 y 40 s el diálogo quieto. **Entrada 9,7 ms** (negro y quieto; 6,7 ms con movimiento). `Finish` del diálogo → Workout. Salida 7,2 ms | Bien (AC-SCN-02-07 con P-19, AC-WKT-21-01) |
| 7 | Workout quieto, 5 s; después `Free workout` | GPU 3D del proceso (contador `GPU Engine`, 5 muestras de 1 s): **2,36 %** en Workout contra **4,11 %** en el inmersivo | Bien (AC-SCN-01-06) |
| 8 | A los 20 s del libre, **matar el proceso** | Copia de la base leída con `RowMate.SessionList --db`: 3 sesiones; la libre cortada **sin fin y con 3 puntos**; `Corta` (7 puntos) y la primera libre (29 puntos), completas | Bien (P-03 de la 4a) |
| 9 | Volver a abrir sobre la misma carpeta, llave en Workout | **`Hora escena` en `real`**; ≈ 13 s después `Free workout`: la escena lista, **entrada 13,6 ms**; `Finish` y cerrar la ventana | Bien (AC-SCN-23-03, AC-WKT-13-15 con la escena lista) |
| 10 | Tercer arranque: Workout a los ≈ 2,5 s y `Free workout` 1,5 s después | Otra vez `escena Loading`: el provisorio hasta los ≈ 6 s y la escena con su fundido a los 8 s. **Entrada 20,0 ms en el toque (GPU 8,3 ms) y 20,8 ms a los 5,77 s, con movimiento**; mientras tanto el registro muestra las subidas de variantes de `Terrain` y `WaterDegraded` (las de la precarga, antes de `Ready`). `Finish`, cerrar | **R3-01 reproducido** |

Base final: 5 sesiones consistentes (la cortada sin fin; las demás con su fin). En los tres registros: 0 `[SLOT]`, 0 avisos, 0 errores, 0 excepciones; `[SCENE] escena: nivel High`; `perf-log-check.ps1`: 0 subidas de *shaders* después de `Ready`.

**Para reproducir R3-01**: un arranque nuevo del proceso y tocar `Free workout` antes de que termine la precarga (en esta PC, con ≈ 5 s de Workout quieto no termina; con ≈ 13 s, sí). Los *drivers* `[PERF]` esperan `escena Ready` antes de la primera entrada, y `-rowmate-scene-delay` demora la escena con la precarga ya hecha: por eso ninguna corrida lo vio. La distancia del HUD que arranca con lo que ya marcaba el remo simulado es de antes (igual en la ronda 2) y no es de esta iteración.

## R3.7 Revisión estática

- **La presentación no toca el dato.** Lo nuevo de V2 (`TabularNumber.SetPaintColor` / `ClearPaintColor` / `DrawnColor`, `PillRowView.HostHalosIn`, `PillRowMotion.HaloRepaint`) solo repinta: no escribe en el motor, la sesión ni la telemetría. `PureCodeRulesTests` en verde.
- **0 B por cuadro con escena**: `SceneBudgetTests` (cuatro escenas y con el HUD, los paneles y la escena) en 0 con su control positivo; `LongSeriesNoAllocationsPerFrame` ahora mide también el panel de UI Toolkit (§R3.1).
- **Nada sigue al salir**: la escena se cierra con la salida; Workout en reposo con la GPU en ≈ la mitad que el inmersivo.
- **Slots y terceros**: los 52 *slots* con `_source: 0` (`Procedural`) y `_asset: {fileID: 0}`; `Assets/ThirdParty/` no existe y `git ls-files Assets/ThirdParty` está vacío; 0 `[SLOT]` en el build.
- **Licencia BSD-3**: `THIRD_PARTY_NOTICES.md` junto al ejecutable, igual byte a byte al de la raíz.
- **`ProjectSettings`**: sin cambios dejados por las pruebas (§R3.3).
- **Repo público**: `git grep` de rutas de usuario de Windows, de Git Bash y del prefijo de las carpetas de trabajo sobre lo versionado: 0; la misma búsqueda sobre los archivos nuevos sin seguimiento y los modificados: 0. Búsqueda de nombres de hardware y del usuario de la PC en lo nuevo y lo modificado: solo los nombres genéricos de catálogo de `Platform/Display/GraphicsTier.cs` y `Tests/UnityEditMode/GraphicsTierTests.cs` (armados en tiempo de ejecución, no son de este equipo) y el identificador público de la app. Esta sección usa rutas relativas, `<scratch>` y la URL del legacy; no da datos del equipo ni horarios del usuario (las horas que nombra son las de la escena o tiempos desde el toque).
- **Legacy**: `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó el módulo de credenciales.

## R3.8 Issues de la ronda 3

| ID | Sev. | Dueño | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V6a-R3-01 | **mayor** | architect | **Con la precarga sin terminar, la entrada y la escena demorada tironean** (AC-WKT-13-15, la parte "si la escena llega tarde … sin que ningún cuadro pase de 16,7 ms"): en el primer arranque, si el usuario toca `Free workout` antes de que termine la precarga, el cuadro del toque tarda **19,7-20,0 ms** (13,6 con la escena lista) y otro, a los 5,4-5,8 s, **20,7-20,8 ms**, los dos con el HUD y el provisorio moviéndose. 2 de 2 arranques. Las unidades de la precarga que R2-05 aceptó "con Workout quieto" (post de URP con la RT chica 22 ms, `Tick` sintético 14 ms, dibujo de calentamiento 11 ms) corren entonces con el inmersivo a la vista. Ningún *driver* lo mide: esperan `Ready`, y `-rowmate-scene-delay` demora la escena con la precarga ya hecha | `<scratch>/it6a/qa-main-r3/manual/session1.log` y `session3.log` (`entrada (2,5 s): … escena Loading …`; `peor del hilo principal 20.8 ms (a los 5392 / 5769 ms del toque, con movimiento`); `session2.log` (la misma entrada con la escena lista, 13,6 ms); capturas `m03*` y `m25*` | (1) Con el inmersivo a la vista, no correr de una vez una unidad que se sabe larga: partirla, o hacerla en el negro de la entrada, o diferirla hasta que haya un cuadro quieto; (2) un *driver* `[PERF]` que entre **antes** de que termine la precarga (p. ej. una opción de `-rowmate-perf-entries` que no espere `Ready`) y su prueba en PlayMode con el presupuesto de cuadro; (3) si no se corrige ahora, que el PO lo escriba como deuda de la 6b con R2-04 y que AC-WKT-13-15 quede en FAIL en el REPORT con el motivo |
| QA-V6a-PAR-01 | menor | designer | **La 11.ª línea de brillo del agua degradada** (de la lente de paridad; seguía abierto y fuera de las tablas de las rondas 1 y 2): AC-SCN-13-03 pide 10, el legacy dibuja 10, y en la fila de abajo de la pantalla hay una más | `Art/Shaders/Scene/WaterDegraded.shader:176` (`n` sin tope); sondas de §R3.2 en CAP-02, -04, -07 y -12 | Tope `n ≤ 9` (o recortar `t` a la última línea del legacy) y una sonda en el *manifest* que cuente las líneas de una columna; regenerar `it6a` (dos corridas) |

Siguen como estaban y fuera de esta ronda: QA-V6a-REN-12 (P-07, del PO), REN-13 y REN-14 en parte, QA-V6a-08 = PAR-02 (las sondas que QA mide en los PNG) y QA-V6a-12 (el aspecto, con P-18 para el usuario). Deuda de la 6b ya acordada: el margen de R2-04 y `DeviceScreenView.Render` con el inmersivo arriba (02-tech-design, "Lo que queda").

**Resumen: 0 bloqueantes, 1 mayor (R3-01) y 1 menor (PAR-01).**

## R3.9 Criterios de aceptación (los que cambiaron desde la ronda 2)

Los demás siguen en PASS con las suites de esta ronda en verde y las tomas iguales byte a byte.

| ID | Ronda 2 | Ronda 3 | Evidencia |
|---|---|---|---|
| AC-WKT-13-15 | PASS | **FAIL** (la parte de la escena demorada real) | Con la escena lista pasa (13,6 / 14,0 / ≤ 9,4 ms; salidas ≤ 10,6 ms; 0 subidas después de `Ready`). Con la precarga sin terminar: 19,7-20,0 ms en el toque y 20,7-20,8 ms a los ≈ 5,6 s, con movimiento (R3-01) |
| AC-SCN-13-03 | PASS | **FAIL** (en pantalla) | El dato de las 10 líneas, bit a bit contra el oráculo (dotnet; `NoShimmerAtTheChangeOfStar`); en pantalla, 11 (QA-V6a-PAR-01) |
| AC-SCN-01-12 | PASS | PASS | Ahora también el HUD con rutina en el *player* (R2-01 cerrado) y `LongSeriesNoAllocationsPerFrame` |
| AC-WKT-14-19 | PASS (PC) | PASS (PC) | `Serie larga` con la escena: máx 7,8 ms, memoria 135-151 MB; [Device-And] NO VERIFICABLE |
| AC-SCN-01-11 | PASS | PASS | Mediana de los p95 de Alto 1,31-1,39 ms (tope 1,5; margen fino, §R3.1) |
| AC-SCN-01-07 | PASS | PASS | Además, la escena demorada **real** (§R3.6, filas 1 y 10): aparece con su fundido, al día, sin reiniciar nada. Los tiempos son de AC-WKT-13-15 |
| AC-SCN-01-10 | PASS | PASS | §R3.5: régimen p99 ≤ 2,9 ms, máx ≤ 5,0 ms en 5 min; ≤ 7,8 ms en `Serie larga`; ningún cuadro > 33 ms |
| AC-SCN-01-09 | NO VERIFICABLE | NO VERIFICABLE | Sin módulo de Android ni teléfono (It. 14) |

**Totales: 98 PASS (4 con una parte de Android NO VERIFICABLE), 2 FAIL, 1 NO VERIFICABLE.** LT-62 a LT-87: 26 de 26 en verde (sin cambios).

## R3.10 Para el coordinador

1. **R2-01 a R2-05: cerrados**, con evidencia en las pruebas, en el *player* y en las tomas.
2. **Antes del REPORT hace falta una decisión sobre R3-01** (mayor, Architect): corregirlo en una ronda corta (con el *driver* de la entrada temprana y su prueba) o que el PO lo acepte como deuda de la 6b, con AC-WKT-13-15 en FAIL en el REPORT. Es un tirón de dos cuadros (≈ 20 ms) en el primer arranque si se entra rápido; no rompe la sesión ni el dato y no pasa de 33 ms.
3. **QA-V6a-PAR-01** (menor, Designer) es barato (un tope en el *shader* y regenerar `it6a`); si se hace junto con R3-01, una sola ronda de QA cubre los dos.
4. **Para el usuario (gate)**, como en la ronda 2: P-18, P-19 y QA-V6a-12; fuera del alcance de QA, la demo (c) de la 5 (AC-WKT-02-11) y el teléfono (AC-SCN-01-09).
5. **Nada quedó cambiado**: ningún Unity ni `RowMate.exe` abierto por esta verificación (Unity Hub quedó abierto para la licencia); el registro del *player* y la carpeta de datos del usuario, iguales al resguardo; `ProjectSettings` y `Settings`, iguales; el legacy, sin cambios.
