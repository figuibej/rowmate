# It. 5 — Verificación QA (principal)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa (verificación principal; el único agente de la verificación que usó Unity) |
| Fecha | 2026-10-30 |
| Iteración | 5 · `05-series-parciales` |
| Entrada | [01-po-spec](01-po-spec.md) (74 AC, con "Cambios por gate QA"), [02-tech-design](02-tech-design.md) (con "Cambios por gate QA" y "por implementación" de WP-A, WP-D1 y WP-B), [04-design-notes](04-design-notes.md) (con §15, WP-D2), [00-deuda-4b](00-deuda-4b.md), [00-deuda-diseno](00-deuda-diseno.md), las tomas de [snapshots/](snapshots/INDEX.md) y `docs/image-03.png` de [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) en `3ae0f3a`, solo lectura (`test/series_tracker_test.dart` y `docs/image-03.png`). `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó el módulo de credenciales de Strava |
| Artefactos | `<scratch>/it5/qa-main-r1/` (resultados de dotnet y de Unity, registros, corridas A del arnés, hojas de cuadros de los videos, el registro de la espera del `RowMate.exe`) |
| Repo | Sin *commits*. Lo único que esta verificación escribió en el repo es este documento y la corrida B de `it5` (iguales byte a byte a lo que había, salvo `durationMs`) |

> **Ronda 2** ([al final](#ronda-2)): **APROBADO CON OBSERVACIONES**, 0 bloqueantes, 0 mayores, 1 menor (QA-V5-R2-01, del Designer). Los 12 issues de la ronda 1 y de las lentes quedaron resueltos (QA-V5-P01, LM5-01 a y b, QA-V5-01 entre ellos); AC-BLE-16-02 pasa a PASS: **71 PASS · 0 FAIL · 3 NO VERIFICABLE**. Lo de abajo (§0 a §10) es la ronda 1 y queda como registro.

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** 0 bloqueantes, 0 mayores y **5 menores** (§8).

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix`) | Compilación **0 avisos, 0 errores** · herramientas **192 / 192** · Core **2536: 2502 pasan, 0 fallan, 34 ignoradas** (las de equipo de WDC y las de antes) |
| Unity EditMode | **2595: 2557 pasan, 0 fallan, 38 ignoradas**, 0 inconclusas |
| Unity EditMode con simulador | **2787: 2749 pasan, 0 fallan, 38 ignoradas, 0 inconclusas**; el *define* restaurado |
| Unity PlayMode | **420 / 420** |
| Unity PlayMode con simulador | **489 / 489**; el *define* restaurado |
| Matriz (`-UnityResults` con los 4 XML) | 658 IDs: **PASA 656, FALLA 0, IGNORADO 2** (AC-APP-15-03 [Apple] y AC-WKT-02-10 [Android]), PENDIENTE UNITY 0, SIN TEST 0. Los 74 AC de la 5 y LT-47 a LT-61: **89 de 89 en PASA** |
| Arnés `it5` | **47 tomas, 0 con problemas, 0 sondas de aspecto que fallan** (27 sondas), `HudOverlap` y `WrenchAudit` en verde, 0 desbordes y 0 textos cortados. **A = B byte a byte** en los 48 archivos (en `manifest.json`, solo `durationMs`); B igual a lo que había en el repo |
| `it4b` (la regeneración única) | Corrida A de QA **igual byte a byte** a lo versionado (52 de 53; `manifest.json` solo `durationMs`). Contra `HEAD`: las 35 PNG que cambian, cambian **solo** en el lugar de los chips (y CAP-09 además en la llave, B5-10), salvo CAP-13 a -15 y CAP-18 (la deuda, AC-WKT-16-07) (§2.4) |
| `it2` / `it3` / `it4a` | Iguales byte a byte a lo versionado (§2.5) |
| Build de Windows con simulador | **NO VERIFICABLE**: el `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator` (desde antes de empezar); se miró cada 2 minutos más de 20 minutos y no se tocó (§6) |
| **Criterios** (74) | **70 PASS · 1 FAIL (menor) · 3 NO VERIFICABLE** |
| LT-47 a LT-61 | 15 de 15 portados con el mismo nombre y las mismas afirmaciones, y en verde (§4) |
| Repo público | `git grep -nIE` de rutas de usuario de Windows, de Git Bash y del prefijo de las carpetas de trabajo de Claude, sobre lo versionado, lo modificado y lo nuevo sin seguimiento: **0**. Ningún documento publica datos del equipo ni de la base del usuario (§7.5) |

**El FAIL** es menor: en L640, con el diálogo de completado abierto, la llave del simulador tapa el chip `Reps` (inerte, debajo del velo), y AC-BLE-16-02 dice que la llave no tapa los paneles "en los cinco perfiles" (QA-V5-01; el Architect lo dejó anotado como B5-10 "para PO y QA" y nadie lo decidió). **Los NO VERIFICABLE** son la medida `[PERF]` del build (AC-WKT-13-15 y la tercera viñeta de AC-WKT-18-21; riesgo aceptado por el PO, QA-G5T-14) y la suspensión real del equipo (AC-WKT-02-11, la verifica el usuario).

---

## 1. Corridas

Todas desde la raíz del repo, una instancia de Unity por vez (solo estaba el `unity.exe serve` de la Hub, que no se tocó; ningún Editor abierto con este proyecto). Ninguna fuente se editó mientras corría Unity.

| # | Comando | Resultado |
|---|---|---|
| 1 | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it5/qa-main-r1/dotnet` | `Build succeeded. 0 Warning(s) 0 Error(s)`; `Total tests: 192 Passed: 192`; `Total tests: 2536 Passed: 2502 Skipped: 34`; matriz sin Unity: PASA 531, FALLA 0, IGNORADO 2, PENDIENTE UNITY 125, SIN TEST 0 |
| 2 | `tools/unity/run-unity-tests.ps1 -Platform EditMode` | `total 2595, pasan 2557, fallan 0, ignorados 38, inconclusos 0`; Unity sale con 0 |
| 3 | `… -Platform PlayMode` | `total 420, pasan 420, fallan 0`; "se borro el TestResults.xml que el Test Framework dejo en la carpeta de datos del Editor (O-R2-01)" |
| 4 | `… -Platform EditMode -Simulator` | `total 2787, pasan 2749, fallan 0, ignorados 38, inconclusos 0`; las tres corridas con 0 |
| 5 | `… -Platform PlayMode -Simulator` | `total 489, pasan 489, fallan 0`; las tres corridas con 0 |
| 6 | `run-tests.ps1 -UnityResults <scratch>/…/unity` | Matriz: PASA 656, FALLA 0, IGNORADO 2, PENDIENTE UNITY 0, SIN TEST 0 (658 IDs) |
| 7 | `tools/unity/capture-snapshots.ps1 -Set it5 -Out <scratch>/…/capA-it5` (A) y sin `-Out` (B, al repo) | Las dos: `listo: 47 tomas, 0 con problemas, 0 sondas de aspecto que fallan`; atlas `0 glifos antes, 1484 después` |
| 8 | `capture-snapshots.ps1 -Set it4b -Out <scratch>/…/capA-it4b` | `listo: 51 tomas, 0 con problemas, 0 sondas de aspecto que fallan` |
| 9 | `capture-snapshots.ps1 -Set it3`, `-Set it2`, `-Set it4a`, cada uno con `-Out <scratch>` | 47 / 41 / 49 tomas, 0 con problemas, 0 sondas que fallan; iguales byte a byte a lo versionado (§2.5) |
| 10 | Espera del `RowMate.exe` de `Builds/Windows/simulator` (un `Get-Process` cada 2 minutos) | **Abierto en los 11 intentos, durante 20 minutos** (§6). No se compiló |

**Lo que el Architect reportó y lo que dio acá**: los mismos números (EditMode 2595 / 2557, con simulador 2787 / 2749, PlayMode 420 y 489, dotnet 192 y 2536 / 2502).

**Un falso "ignorado", explicado.** La corrida 6 (que recompila y vuelve a correr dotnet) se hizo mientras el arnés de la corrida 7 tenía puesto el *define* del simulador: `SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator` ve la marca sin la variable de la corrida y queda sin ejecutar, como está diseñado (dio `Total tests: 2535 Passed: 2501`). Es la única diferencia entre los dos registros, prueba por prueba. Valen los números de la corrida 1.

**Unity que revienta al cerrar (QA-V4b-R2-03)**: en los 16 cambios de *define* de esta verificación (las dos corridas con simulador y las seis del arnés) no pasó ninguna vez (ninguna línea "revento" en las salidas). El comportamiento lo fija `I5D.DefineStepsSurviveTheCrashOnExit` (pasa) y el Architect lo vio de verdad cinco veces (00-deuda-4b §3).

**Después de todas las corridas**: `ProjectSettings/ProjectSettings.asset` sin `ROWMATE_SIMULATOR`, sin la marca `Library/RowMate/simulator-tests.txt`, ningún archivo de `Assets/RowMate/UI`, `Assets/RowMate/Settings`, `ProjectSettings` ni `Packages` cambiado, y `git status --porcelain` **idéntico** al de antes de empezar (234 entradas; la carpeta de la 5 está sin seguimiento, y adentro la corrida B solo cambió `durationMs` en `snapshots/manifest.json`).

---

## 2. Capturas y videos

### 2.1 Reglas del arnés

| Regla | Cómo se verificó | Resultado |
|---|---|---|
| La regla de toma de la 4b (instante exacto en ms enteros, falla si algo finito bloquea, `inFlight` en el *manifest*) más las fases finitas de los paneles | `SnapshotTakeRule.Blockers` suma `TabularCells` (rodillo antes de su mitad) y `SeriesPanelMotion` (el resorte del alto a 0,15 dp; cualquier fase de duración fija bloquea: el alto por un dato, los fundidos, la flecha, el empuje, la marca hasta 1400 ms, el cambio por pieza, la capa de estado) y `SeriesColumnMotion` como toda `HudMotion` (la serie que empieza o termina, el corrimiento, la rueda). En el *manifest*, 27 tomas con `inFlight` vacío y las demás solo con lo continuo (`PillRowMotion`, `StepPulse`, el disco) o rodillos de la banda pasada su mitad | Bien |
| Una `MemoryPreferences` por toma; la misma en la secuencia DEMO; nunca un archivo | `SeriesCatalog.Prefs(laps, reps)` (`SeriesCatalog.cs:835-848`); DEMO-11 abre los dos paneles (los que dejaron DEMO-03 y DEMO-05) y DEMO-12 los deja en falso (sus comprobaciones pasan); `I5T.TheCatalogUsesAMemoryPreferencesPerTakeAndNeverAFile` pasa | Bien |
| Toque al menos 0,82 s antes o preferencia en verdadero | Todas las tomas con un panel abierto arrancan con la preferencia en verdadero, salvo DEMO-03 y DEMO-05 (0,8 s): la regla de toma comprobó el resorte quieto (`inFlight` sin `SeriesPanelMotion`; G = 84 y 90) | Bien |
| CAP-11 a 881,2 s y DEMO-11 a 31,6 s (DT5-04) | `INDEX.md` y las tomas: CAP-11 `0:03.0` `12 m`, `Rep 2/4 · 12 m`; DEMO-11 `0:01.0` `4 m` con `Series anteriores` entero | Bien |
| `S-LARGO` con `UiTestHost.OverrideSeries` | CAP-25: "S-LARGO (OverrideSeries), los dos" | Bien |
| El contorno de la zona del bote en CAP-03 y CAP-14 a -18 | Visto en las seis (rectángulo naranja) | Bien |
| `HudOverlap` y `WrenchAudit` con los rectángulos visuales de los paneles | `WrenchAudit.SeriesRects` (la caja con el `translate` de la columna; en `ColumnScroll`, recortada a la ventana) entra en el cruce de a pares y en la llave; `SeriesProblems` con `RespectsBoatZone`. 0 problemas en las 47 tomas; 0 desbordes y 0 textos cortados en la auditoría | Bien, con la excepción `OverSeries` de L640 con el diálogo (QA-V5-01) |
| Videos a 30 fps con los valores por cuadro comparados con `SeriesTimelines` | 9 videos a 30/1 (VID-01 90 cuadros, -02 120, -03 180, -04 150, -05 300, -06 45, -07 45, -08 120, -09 90); valores por cuadro en el *manifest* (`G`, `tail`, fundidos, flecha, empuje, marca, FLIP, la vuelta en curso, la línea `Rep` y la banda); sus `FrameCheck` pasan | Bien. VID-06 y VID-07 duran 1,5 s (QA-V5-04) |
| Dos corridas iguales | A (`<scratch>`) contra B (repo): 48 de 48 archivos de imagen, video e `INDEX.md` iguales; `manifest.json` igual salvo `durationMs`. B contra lo que había en el repo (la corrida de WP-D2): igual | Bien |

### 2.2 Cada toma, mirada (Read de cada PNG)

Se miraron las 38 PNG enteras y, donde hacía falta, en recortes de los paneles. Los valores salen de la toma y del *manifest* (ranuras con su texto, su alfa y su escala).

| Toma | Qué se vio | Contra la spec / las notas |
|---|---|---|
| CAP-01 | Chips `500 m` (ámbar) y `Reps` (celeste) cerrados, del mismo ancho, con la flecha ∨ pegada a la derecha y vidrio vacío en el medio; `500 m` en y 211,0-246,9 y `Reps` en 255,2-291,1 (*manifest*); la barra con 8 píldoras, `Trabajo`, `600m`, `10:01` | Bien |
| CAP-02 | `Parciales 500 m`, `0:15.0` con `60 m` a la derecha, `#1 2:05.0`, `Rep 1/3 · 560 m`; el chip `Reps` debajo | Bien |
| CAP-03 | `0:50.0` `200 m`, `Rep 2/3 · 200 m`; `Series anteriores` con `Rep 1 2:05 680 m`, los metros de la vuelta y los de la repetición terminando en la misma x (148,835 dp las dos ranuras); el contorno: `Series anteriores` entra en la zona solo por su franja de afuera, lejos del centro | Bien. **Comparable con `docs/image-03.png`** del legacy: la misma lectura (título con ícono y flecha, tiempo grande ámbar con los metros, `Rep r/R · m` celeste; `Series anteriores` con `Rep 1`, split y metros en columnas), más angosto (DV-50) y con los metros a la derecha (DV-51) |
| CAP-04 | `0:10.0` `40 m`, `Rep 3/3 · 40 m`; `Rep 2` arriba y `Rep 1` abajo, `2:05` `680 m`, columnas alineadas | Bien |
| CAP-05 | En `Descanso`: `Sin parciales` / `en este paso` en dos renglones, `Rep 1/3 · 640 m`; `Sin repeticiones` / `completas`; `Próximo: Trabajo` | Bien |
| CAP-06 | La barra en `Descanso` `0:20`; los paneles todavía con el trabajo: `0:25.0` `100 m`, `#1 2:05.0`, `Rep 1/3 · 600 m`; `Sin repeticiones completas` | Bien (P-01) |
| CAP-07 | `Enfriamiento`: solo `Sin parciales en este paso`, sin línea `Rep` y sin el panel de repeticiones | Bien |
| CAP-08 | `0:50.0` `250 m`; `#5`, `#4`, `#3` `1:40.0` y `#2 2:05.0`; sin `#1`, sin línea `Rep`, sin chip `Reps`; Split `01:40` | Bien |
| CAP-09 | `0:03.3` `10 m` y `#1 2:46.7` (LT-47); Split `02:47` | Bien |
| CAP-10 | `0:05.0` `20 m`, `Rep 5/5 · 20 m`; `Rep 4`, `Rep 3`, `Rep 2` con `2:05` `540 m` | Bien |
| CAP-11 | `0:03.0` `12 m`, `Rep 2/4 · 12 m`, `Rep 1 2:05 1.112 m` (la fila a 0,958); sin sello | Bien |
| CAP-12 / CAP-13 | En inglés: `500 m splits`, `Previous reps`, `Rep 1 2:05 680 m`; `No splits` / `in this step`, `No completed` / `reps`, `Rep 1/3 · 640 m`; `Work`, `Rest`, `Up next: Work`; los títulos enteros a escala 1 | Bien |
| CAP-14 | P360: los paneles abiertos en la columna izquierda, sin cruzarse con Split ni Watts; `Series anteriores` entra en la franja de afuera del contorno | Bien |
| CAP-15 | TH: los dos chips enteros, sin desplazar y sin máscara, entre SPM y la banda; la zona del bote libre | Bien |
| CAP-16 / CAP-17 | TH y L640: parciales abierto con la cabecera, `0:10.0` `40 m` y `Rep 3/3 · 40 m`, el vidrio que se apaga en el piso; repeticiones debajo del piso, sin verse; en L640 el panel no se angosta y termina antes del contorno | Bien. En L640 la llave queda dentro del contorno, en su lugar de la 4b (it4b CAP-21 igual): es la herramienta del simulador, no una pieza del HUD |
| CAP-18 | W1280 a 1,20: `0:15.0` `60 m`, `#1 2:05.0`, `Rep 2/3 · 560 m`; `Rep 1 2:05 680 m`; todo sin desplazar; la zona del bote libre | Bien. **Comparable con `docs/image-03.png`** (la ventana de escritorio del legacy) |
| CAP-19 / CAP-20 | La cuenta grande `3` en su disco, junto a los chips y junto a parciales abierto (`Sin parciales` / `en este paso`, `Rep 1/3 · 668 m`), sin tocarlos; `Próximo: Trabajo`; `0:03` agrandada y teñida | Bien |
| CAP-21 | TH: la cuenta grande en el centro alto; parciales abierto con el vacío entero; la línea `Rep` todavía debajo del piso (alfa 0) | Bien |
| CAP-22 / CAP-23 | `PAUSA` en su disco al lado de los paneles abiertos (TV) y en el centro (TH); `0:50.0` `200 m` quieto; `Reanudar` | Bien |
| CAP-24 | El sello `No alcanzado · 150 / 160 W · 24.0 / 22 spm` en la tercera fila; `1:00.0` `240 m`, `Rep 1/2 · 240 m`; `Sin repeticiones completas`; las tarjetas en su lugar | Bien |
| CAP-25 | `S-LARGO`: `5999:59.9` `499 m`, `#1999` a `#1996` con `5999:59.9`, `Rep 999/999 · 999.999 m`, `Rep 999` a `997` con `5999:59` `999.999 m` (a 0,631): todo entero, sin `…`, dentro del vidrio | Bien |
| CAP-26 | W1280: el anillo de foco blanco con su halo alrededor de `Parciales 500 m`, entero, sin tocar SPM ni el panel de abajo | Bien |
| DEMO-01 a DEMO-12 | Workout con `Series 600` primera; el chip `500 m` solo en el calentamiento; `Parciales 500 m` · `Sin parciales en este paso`; `0:30.0` `113 m` · `Rep 1/3 · 113 m` con el chip `Reps`; `Series anteriores` vacío; `#1 2:12.5` (≈ `2:12.6`); `Rep 1 2:13 675 m` (≈ `676 m`) con `Rep 2/3 · 79 m`; el panel del simulador abierto sin tapar Split, Watts ni la barra, con `0:33.0` `117 m`; `0:41.0` `140 m` después del corte; reposo; la sesión nueva con los dos abiertos, `0:01.0` `4 m` (≈ `3 m`) y `Sin repeticiones completas`; reposo | Bien |

### 2.3 Los videos, cuadro por cuadro

Cuadros extraídos con PyAV (`uv run --offline`, sin red) en hojas de contacto de la zona de los paneles (`<scratch>/it5/qa-main-r1/sheets/`), y los valores del *manifest*.

| Video | Qué se vio | Resultado |
|---|---|---|
| VID-01 | Abrir (cuadros 8-24): el alto crece con el resorte, el contenido entra cuando el vidrio lo descubre, el rótulo y el título se cruzan, la flecha gira; `Series anteriores` baja con él a 8·sp; en el cuadro 30 `0:51.0` `204 m` y `Rep 2/3 · 204 m` con el rodillo, el mismo cuadro que la banda pasa a `04:11`. Cerrar (54-72): el contenido se va antes de que baje el vidrio y `Series anteriores` sube. El ancho no cambia | Bien |
| VID-02 | Cuadro 45: `0:00.0` `0 m` y `Rep 2/3 · 500 m`; `#1 2:05.0` entra desde arriba (0,331 en el 47, 0,983 en el 50) con su marca ámbar, que vale 1 hasta el 62 y 0,929 · 0,5 · 0,071 · 0 en los 68, 74, 80 y 86 (la tabla de 04-design-notes §6.2); el alto 91,1 → 114 con el empuje y la cola; en el 80, `0:01.0` `4 m` | Bien |
| VID-03 | Cuadro 45: la barra pasa a `Descanso` y los paneles siguen con `0:25.0` `100 m` y `#1`; cuadro 75-82: lo viejo se va, `Sin parciales` / `en este paso` entra, el alto se acomoda, la línea pasa a `604 m` con su rodillo | Bien |
| VID-04 | Cuadros 10-46: la cuenta grande `2` y `1` en su disco sin tocar los paneles; cuadro 75: `Rep 1/3 · 680 m` → `Rep 2/3 · 4 m`, `0:01.0` `4 m`, `Rep 1 2:05 680 m` entra en `Series anteriores` con su marca | Bien, salvo que **la línea `Rep` rueda entera** cuando cambia su largo (cuadros 75-76 y, con `8 m` → `12 m`, 134-136): QA-V5-02 (H5-D2-01 de WP-D2, sin corregir) |
| VID-05 | W1280, simulador sin ruido: `0:31.0` `117 m` (240 s), y después `0:32.0`, `0:33.0`, `0:34.0` con `117 m` y `Rep 2/3 · 117 m` quietos mientras la banda sigue; `0:35.0` `121 m`, `0:36.0` `125 m` …: sin salto, sin vuelta inventada, sin negativos | Bien (AC-BLE-15-09) |
| VID-06 | `Cinco reps`: los dos paneles, abiertos, entran con SPM en su tramo de la cascada, ya con `0:00.0` `0 m`, `Rep 1/5 · 0 m` y `Sin repeticiones completas`; nada aparece después | Bien la entrada (DV-48). Al segundo 1 muestra `0:01.0` **`0 m`** y no `4 m` (QA-V5-03); dura 1,5 s y no 2,5 (QA-V5-04) |
| VID-07 | La salida con `0:52.0` `208 m`, `Rep 2/3 · 208 m` y `Rep 1 2:05 680 m` en todos los cuadros, con la opacidad del HUD; después, Workout | Bien. Dura 1,5 s (QA-V5-04) |
| VID-08 | Reducir movimiento: en el cuadro 9 el alto pasa de 34 a 90 y `Series anteriores` se corre en el mismo cuadro; el contenido funde; la flecha cambia de trazo; cuadro 45: `#1` funde sin deslizarse, con su marca | Bien |
| VID-09 | La cuenta grande `3` aparece en el 15 y sigue en su lugar mientras parciales se abre a su lado (G 38,4 → 106; el FLIP de `Reps` de −71,4 a 0); `2` en el 46 y `1` en el 76; en ningún cuadro se enciman | Bien |

### 2.4 `it4b`: la regeneración única

- **Reproducible**: la corrida A de QA dio los mismos bytes que lo versionado en 52 de 53 archivos (`manifest.json`, solo `durationMs`). Desde la regeneración solo cambiaron `SeriesCatalog.cs` y pruebas (ninguna fuente que toque `it4b`).
- **Contra `HEAD`** (comparadas con `git lfs smudge` y una diferencia de píxeles con Pillow): las 35 PNG que cambian, cambian **solo dentro del lugar de los chips** (x 13-187 dp debajo de SPM: TV y TH 14,9-159,6 × 210,7-309,3; P360 y L640 13-140 × 185-271; W1280 21-187 × 319-439), salvo:
  - CAP-09 (TH con el diálogo): además la llave, que pasó a la derecha de los chips (x hasta 207,2; B5-10);
  - CAP-13, -14, -15: la barra libre en el modo compacto (y 19-40,4, x 193,9-376; la deuda, AC-WKT-16-07); visto: el título separado del reloj con más aire que el espacio entre sus palabras;
  - CAP-18: 20 píxeles en 1 nivel en el borde de la `I` de `ACTIVE` (00-deuda-4b §6).
- **Las del libre** (CAP-13 a -18, CAP-28, DEMO-01 a -04 y -12, VID-01, -02, -06 a -08) son iguales byte a byte a las de la corrida de la deuda (`<scratch>/it5/capA3-it4b`), como pide 02-tech-design §5.5. Los videos con rutina (VID-03 a -05) cambian por los chips.
- Ninguna diferencia sin explicar.

### 2.5 `it2`, `it3` e `it4a` no cambiaron sin motivo

Ninguna de sus tomas versionadas cambió en esta iteración (`git status` sin entradas en esas carpetas). Como WP-B cambió `Base.uss`, `Typography.uss` y `Tokens.uss` y no las volvió a tomar, QA las volvió a tomar a `<scratch>`:

| Conjunto | Resultado |
|---|---|
| `it3` | `47 tomas, 0 con problemas, 0 sondas que fallan`; **48 de 49 archivos iguales** a lo versionado (`manifest.json`, solo `durationMs`) |
| `it2` | `41 tomas, 0 con problemas, 0 sondas que fallan`; **43 de 44 archivos iguales** (`manifest.json`, solo `durationMs`) |
| `it4a` | `49 tomas, 0 con problemas, 0 sondas que fallan`; sacó CAP-01 a CAP-11 (reposo y el diálogo de inicio) **iguales byte a byte** (12 de 13 archivos; `manifest.json`, solo `durationMs`) y no volvió a tomar las del HUD (`superseded: it4b`). No queda nada con rutina para regenerar (DT5-08) |

`num.lap` y `num.row` (los tokens que cambiaron de tamaño) solo los usa `SeriesPanelView` (búsqueda en `Assets/RowMate`), así que ninguna otra pantalla podía cambiar por eso.

---

## 3. Criterios de aceptación (74)

Marcas de evidencia: **d** dotnet · **u** Unity EditMode · **p** PlayMode · **ps** PlayMode con simulador · **cap** captura · **eq** el build en la PC · **qa** revisión de QA. El número entre paréntesis es la cantidad de resultados de la matriz que pasan (dotnet + los cuatro XML de Unity).

### 3.1 F-WKT-07 — Parciales de 500 m

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-07-01 | PASS | d (9): `SeriesPanelsTests.OnlyWithARoutine`, LT-50, `SeriesSeiscientosCurrentLapAndRepMeters`; cap CAP-05, CAP-07 |
| AC-WKT-07-02 | PASS | d (24): LT-47 a -49, `PiramideQuinientos`, `FondoTresMilWithRFourAKeepsEveryLapOfTheStep`, `Trabajo 1000` con R-4 |
| AC-WKT-07-03 | PASS | d (9): `SeriesEngineTests.OneReadingPerSecondBeforeTheEndOfTheStep`, `NothingInPauseInFreeOrAfterTheEnd`, `SeriesGoldenTests.TheEngineFeedsTheOracleInputs`; cap VID-03 |
| AC-WKT-07-04 | PASS | d (9): LT-47 (166,67 s), LT-48, `TrabajoMilWithRThreeInterpolates`; cap CAP-09 |
| AC-WKT-07-05 | PASS | d (12): LT-47, LT-50, `SeriesSeiscientosCurrentLapAndRepMeters`; cap CAP-02, CAP-06 |
| AC-WKT-07-06 | PASS | d (30): LT-55 a -57, `SanitizedDistanceTests`, `Trabajo 1000` con R-4R / R-4Z / R-4D, `SeriesGoldenTests.TheTrackerMatchesTheLegacy` |
| AC-WKT-07-07 | PASS | d (9): LT-58, `SeriesEngineTests.WhatIsRowedInPauseDoesNotCount` (124 m); cap CAP-22 |
| AC-WKT-07-08 | PASS | d (6): LT-56, `CountsFromTheMonitorDistanceAtTheStart` |
| AC-WKT-07-09 | PASS | d (42): `TheSameMetersAsTheDistanceStepSecondBySecond` con R-4, R-4R, R-4Z, R-4D…; `PureCodeRulesTests.OneDistanceSanitizerOnly` |
| AC-WKT-07-10 | PASS | d (12): `TheBackgroundGivesTheSameAsTheForeground`, `SuspendedWithRFourS_RawCounts` / `_RawStops` (`0:51.0` `200 m`, `0:52.0` `204 m`) |
| AC-WKT-07-11 | PASS | d (18): `BeginOnStartAndOnIdleNothingFromThePreviousSession`, `TheSnapshotCarriesTheTrackerStateWithoutCopying`; cap DEMO-11 |
| AC-WKT-07-12 | PASS | d (27): `SeriesGoldenTests` (el tracker contra `series_tracker.json` del legacy, la alimentación del motor con las 50 corridas, los resúmenes); `OracleToolingTests` |

### 3.2 F-WKT-08 — Repeticiones y comparación

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-08-01 | PASS | d (6): LT-50, `DosSeriesClearsTheComparison` |
| AC-WKT-08-02 | PASS | d (9): LT-50, LT-57, `SeriesSeiscientosCurrentLapAndRepMeters` |
| AC-WKT-08-03 | PASS | d (9): LT-51, `SeriesSeiscientosReps` |
| AC-WKT-08-04 | PASS | d (18): LT-51, LT-52, LT-55, `CincoRepsKeepsTheLastThree`; cap CAP-10 |
| AC-WKT-08-05 | PASS | d (6): LT-53, `DosSeriesClearsTheComparison` |
| AC-WKT-08-06 | PASS | d (12): LT-51, LT-54, `TheWorkSplitCountsTheSecondsOfAJumpButNotItsMeters`, `TheSummariesMatchTheLegacy` |
| AC-WKT-08-07 | PASS | d (6): `PruebaCuatroAAndCorta`, `SeriesSeiscientosReps` |
| AC-WKT-08-08 | PASS | d (15): `PiramideQuinientos` (1112 / 752 / 188 s), `PruebaCuatroAAndCorta`, `CincoRepsKeepsTheLastThree`; cap CAP-04, CAP-11 |

### 3.3 F-WKT-18 — Paneles colapsables

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-18-01 | PASS | d+p (34): `SeriesPanelsTests.FromTheFirstFrameTheFirstStep` (DV-48), LT-61, `HudSeriesChangeTests.TheSeriesStarts`; cap CAP-01, CAP-07, CAP-08, VID-06 |
| AC-WKT-18-02 | PASS | p (4): `SeriesPanelViewTests.SlotsAndRectanglesLikeTheDesign`, LT-59; cap CAP-01 (144,3 × 35,9 dp), CAP-15 |
| AC-WKT-18-03 | PASS | p (4): `SlotsAndRectanglesLikeTheDesign`; cap CAP-02, CAP-12. El título a 0,941 en español (lo mide Unity, R5-02; las notas, 0,949) y a 1 en inglés |
| AC-WKT-18-04 | PASS | d+p (32): `SeriesPanelGeometryTests.TheVerticalOfEachRow`, `WorkoutHudStoreTests`; cap CAP-02, CAP-08, CAP-09 |
| AC-WKT-18-05 | PASS | d+p (8): `TheEmptyTextsBreakIntoTwoEvenLines`; cap CAP-05 |
| AC-WKT-18-06 | PASS | d+p (13): LT-59, `WhatTheyShowSecondBySecondInSeriesSeiscientos`; cap CAP-02 a CAP-05 |
| AC-WKT-18-07 | PASS | d+p (25): LT-59, `NaturalHeights`; cap CAP-04, CAP-10, CAP-11 |
| AC-WKT-18-08 | PASS | d+p (19): `HudSeriesChangeTests.WorkToRest`, `RestToTheNextRep`, `ImmersiveHudTests.PresentationNeverTouchesTheDataWithThePanels`; cap CAP-06, VID-03, VID-04 |
| AC-WKT-18-09 | PASS | d+p (35): `TheTouchAreasOfTheChips` (56 dp, sin superponerse: en CAP-01 `500 m` toca y 195,2-251,2 y `Reps` 251,2-307,2), `SeriesColumnTests` por la selección real; cap DEMO-03, DEMO-05. El arrastre con el dedo es [Device-And] |
| AC-WKT-18-10 | PASS | d+p (27): LT-59, LT-60, `SeriesPanelPrefsTests.ATapBeforeTheReadWins`, `BootScenePreferencesTests.PreferencesSurviveARestart` (dos arranques de `AppRoot` sobre la misma carpeta temporal); cap DEMO-11, DEMO-12. La parte de equipo (cerrar y abrir el build) queda pendiente (§6) |
| AC-WKT-18-11 | PASS | d+u+p (73): `JsonFilePreferencesTests` (falta, vacío, dañado, BOM, otro tipo que conserva las demás claves, guardado que falla, corte entre el temporal y el reemplazo), `PreferencesFileTests`, `LegacyIsolationTests.ThePreferencesAreTheAppsOwn`; qa: el archivo del usuario intacto (§7.4). IL2CPP (R5-04): NO VERIFICABLE sin el módulo |
| AC-WKT-18-12 | PASS | p (6): `SeriesKeyboardTests` (Tab, Enter, `KeyDownEvent` de Espacio, el foco que se pierde va a `Terminar`); cap CAP-26. La ventana real queda pendiente (§6) |
| AC-WKT-18-13 | PASS | d+p (52): `FitAllAndColumnScrollKeepTheNaturalHeights` (cinco perfiles), `SeriesColumnTests.RowsScrollOnTheDesktop`; cap CAP-14 a CAP-18 |
| AC-WKT-18-14 | PASS | d+p (5): `SeriesPanelGeometryTests.TheBoatZone`, `SeriesColumnTests.ColumnScrollInLandscape`; cap CAP-03, CAP-14 a CAP-18 con el contorno. Observación de redacción: QA-V5-05 |
| AC-WKT-18-15 | PASS | d+p (32): `TheBigCountIsNeverBlockedWith137` (cinco perfiles), la regla de reserva probada con 200; cap CAP-19 a CAP-23, VID-09 |
| AC-WKT-18-16 | PASS | d+p (17): `NothingOfTheFourBIsCrossed`, `HudLayoutTests.SeriesPanelsOverlapNothingInAnyProfile`; cap CAP-01 a CAP-26 (`HudOverlap` en verde) |
| AC-WKT-18-17 | PASS | d+p (11): `WhatDoesNotFitShrinks`, `OneTitleScalePerLanguage`, SPV con `S-LARGO`, `R-4X` y `ExtremeSplit`; cap CAP-25 |
| AC-WKT-18-18 | PASS | d+p (35): `SeriesTimelinesTests` (abrir, cerrar desde los 80, la inversión), SMT; cap VID-01, VID-08 |
| AC-WKT-18-19 | PASS | d+p (95): `SeriesTimelinesTests`, `HudSeriesChangeTests`, SMT, WBU; cap VID-02 a VID-04, VID-08. Observación: la línea `Rep` rueda entera cuando cambia de largo (QA-V5-02) |
| AC-WKT-18-20 | PASS | p (6): `SeriesPanelsOverlapNothingInAnyProfile`, `NothingAnimatesAfterTheExitWithThePanels`; cap CAP-22, VID-07 |
| AC-WKT-18-21 | **NO VERIFICABLE** (parcial) | Lo del Editor **pasa**: el peor caso de la 5, **56 elementos en TV y P360 y 57 en TH, L640 y W1280** (tope 60, `HBT.AtMostSixtyVisibleElementsInTheFiveWorstCase`); **0 asignaciones** en los dibujos de los paneles y en los pasos al abrir, cerrar, la vuelta nueva, el cambio por pieza y la repetición nueva (14 a 32 dibujos por escena, `HBT.SeriesNoAllocationsPerFrame`), con el control positivo en la misma corrida (`FAT`: 30 de 30 con un `byte[64]` por repintado y 30 de 30 por paso; 0 sin asignar); el trabajo de los paneles, mediana 0,730 ms y peor 0,815 ms en 60 cuadros. **La ventana `[PERF]` del build** (ningún cuadro de más de 33 ms, p99 ≤ 16,7 ms) no se pudo medir (§6) |
| AC-WKT-18-22 | PASS | d+p (11): `CatalogParityTests.NineItFiveKeys`, `WorkoutHudStoreTests.SeriesTextsInSpanishAndEnglish`; cap CAP-12, CAP-13 |

### 3.4 F-APP-10 y la deuda de la 4b (§5.1)

| AC | Resultado | Evidencia |
|---|---|---|
| AC-APP-10-12 | PASS | d (6): `NineItFiveKeys`, `SameKeysInBothLanguages` (223 por idioma) |
| AC-WKT-13-15 | **NO VERIFICABLE** | El orden de la entrada en el Editor **pasa** (`ITT.TheHudIsBuiltInTheBlack`, `ITT.WithReducedMotionTheHudIsThereAtOnce`, `4BD.HudWarmupIsNotSeenAndLeavesNothing`, `4BD.TheFirstImmersiveAfterTheWarmupOpensTheSame`, `BootSceneTests.TheBootWarmsUpTheHudAndLeavesNothing`). La medida `[PERF]` de las tres entradas en el build, que es el criterio, no se pudo hacer (§6; riesgo aceptado QA-G5T-14) |
| AC-WKT-13-16 | PASS | p (4): `4BD.ACompletionDialogThatCannotOpenStillReturnsToRest`; u `MDI.AThrowingMotionLeavesAndTheOthersKeepGoing`, `MDI.WithoutLoggerItStillIsolates` |
| AC-WKT-16-07 | PASS | p (4): `FBT.CompactBarSeparatesTheTitleFromTheClock`, `FBT.EnglishAndLandscapeKeepTheNormalBar`; cap it4b CAP-13 a -15 regeneradas (§2.4) |

### 3.5 §5.2 — Cambian por esta iteración

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-13-06 | PASS | d+p (25): `HudTimelinesTests.EntryTableOfTheSpec`, SMT `TheCascadeBringsThePanelsWithSpm`; cap VID-06 (los paneles con SPM, ya armados) |
| AC-WKT-13-08 | PASS | d+p (17): `HudTimelinesTests.ExitTable`; cap VID-07 (la última foto en todos los cuadros) |
| AC-WKT-13-12 | PASS | d+p (20): `HudGeometryTests.PausedInTheDisc`; cap CAP-22, CAP-23 |
| AC-WKT-13-13 | PASS | d+p (32): `NoOverlapWithTheNewPieces`, `NothingOfTheFourBIsCrossed`; `HudOverlap` en las 47 tomas |
| AC-WKT-14-18 | PASS | d+p (54): `BigCountKeepsItsDistances`, `TheBigCountIsNeverBlockedWith137`; cap CAP-19 a CAP-21 |
| AC-WKT-17-06 | PASS | p (8): `PresentationNeverTouchesTheDataWithThePanels` |
| AC-WKT-17-09 | PASS | d+p (15): `HudGeometryTests.BoatZone`, `TheBoatZone`; cap con contorno. Observación de redacción: QA-V5-05 |
| AC-WKT-02-08 | PASS | d+p (50): WBU, `OneSnapshotPerReturn`. La parte de Android, NO VERIFICABLE |
| AC-APP-05-07 | PASS | p (39): `HudControlsTests.FirstTabWithoutFocusShowsTheRing`, SKT; cap CAP-26. La ventana real queda pendiente (§6) |
| AC-APP-05-08 | PASS | d+p (50): `HudTimelinesTests.ReducedVariants`, STL `ReducedVariants`; cap VID-08 |
| AC-BLE-16-02 | **FAIL** (menor) | d+ps (85) en verde, pero en **L640 con el diálogo de completado** la llave tapa el chip `Reps` (it4b CAP-10: la llave en x 16-56, y 231-271 dp, sobre `Re` de `Reps`, inerte y debajo del velo). `WrenchAudit` lo acepta a propósito (`WrenchOverSeriesAccepted`, `WrenchAudit.cs:190-195`; B5-10). El AC dice "tampoco tapa los paneles, cerrados ni abiertos, en los cinco perfiles": QA-V5-01 |
| AC-BLE-16-03 | PASS | d+ps (48): `PanelCeilingInBothOrientations`; cap DEMO-08 |
| AC-HIS-20-04 | PASS | d+u+p (68): `I5T.TheCatalogUsesAMemoryPreferencesPerTakeAndNeverAFile`, `JsonFilePreferencesTests.OneFileNameForBothBuildsAndNeverTheLegacy…`; qa: el `prefs.json` del usuario no existía antes y no existe después; la base del usuario, sin cambios por esta verificación (§7.4) |

### 3.6 §5.3 — Se vuelven a verificar

| AC | Resultado | Evidencia |
|---|---|---|
| AC-WKT-01-05 | PASS | d (12): `SanitizedDistanceTests.RebaseIgnoresWhatWasRowedInPause`, `StepAdvanceTests.ResumeRebasesTheMeters` |
| AC-WKT-02-04 | PASS | d (39): `TheBackgroundGivesTheSameAsTheForeground` y las de la 4a |
| AC-WKT-02-11 | **NO VERIFICABLE** | d (124) en verde (con `R-4S`: la vuelta en curso no suma el tiempo dormido); la suspensión real la verifica el usuario (00-deuda-4b §1.5) |
| AC-WKT-03-03 | PASS | d (57): `TheSameMetersAsTheDistanceStepSecondBySecond`, `OneDistanceSanitizerOnly` |
| AC-WKT-11-09 / -10 / -11 | PASS | d (18 / 9 / 24): `SeriesPanelsTests.formatos` (LT-45), `WorkoutFormatsTests`; cap CAP-02 (`M:SS.d`), CAP-04 (`M:SS`), CAP-11 (`1.112 m`) |
| AC-WKT-13-07 | PASS | p (10): `ITT.BackFromTheFirstFrame`, SCT (los paneles responden desde su tramo de la cascada) |
| AC-WKT-13-09 | PASS | p+u (10): `ITT.NothingAnimatesAfterTheExit`, `NothingAnimatesAfterTheExitWithThePanels`, `4BD.TheStepBarLeavesWithoutItsFiniteMotions` (O-1) |
| AC-WKT-13-14 | PASS | d+p (20): `PresentationIndependenceTests`, `PresentationNeverTouchesTheDataWithThePanels` |
| AC-WKT-17-01 | PASS | d+p (204): las de la 4a y SPV (las sondas del vidrio); cap CAP-02, CAP-03 |
| AC-WKT-17-07 | PASS | p (6): `HudLayoutTests.RotationWithDialogSealOrBigCount`, `SeriesPanelsOverlapNothingInAnyProfile` |
| AC-WKT-17-10 | PASS | p (6): `HudLayoutTests.ExtremeValuesFit`, SPV con `R-4X` |
| AC-APP-05-05 | PASS | d+p (76): `TextScaleStepsTests` y los paneles fuera de la letra del sistema |
| AC-BLE-15-09 | PASS | s+ps (5): `SeriesSimulatorTests.ADisconnectDuringAWorkKeepsTheLapMeters`, `DeviceSimulatorFlowTests.PhysicsDoesNotRestartOnReconnect`, WSM; cap VID-05, DEMO-08, DEMO-09 |

**Conteo**: 74 = **70 PASS** + **1 FAIL** (AC-BLE-16-02) + **3 NO VERIFICABLE** (AC-WKT-13-15, AC-WKT-18-21 en su parte de build, AC-WKT-02-11).

---

## 4. Casos de test legacy (LT-47 a LT-61)

| LT | Portado | Resultado |
|---|---|---|
| LT-47 a LT-58 | `Tests/EditMode/Workout/SeriesTrackerTests.cs`, con el nombre exacto del legacy en `TestName` y la cita `test/series_tracker_test.dart:línea`. Se compararon línea a línea LT-55 a LT-58 con el legacy (`:155-235`): las mismas lecturas (250 de +4, la de 0, de 1004 a 1100; el salto a 5000; el reinicio con 61 lecturas; `rebase(9000)` y 9004) y las mismas afirmaciones (≥ 0, 2 vueltas de 125 ± 2, el trabajo de la rep 1 entre 1060 y 1110, 20 y 24 m) | 3 de 3 cada uno (dotnet, EditMode y EditMode con simulador) |
| LT-59 a LT-61 | `SeriesPanelPrefsTests` (la regla, dotnet) y `SeriesPanelViewTests` (los paneles montados, con `OverrideSeries` y el tracker a 300 m como `_trackerAt300m`) | 5 de 5 cada uno |

---

## 5. La deuda de la 4b declarada resuelta (00-deuda-4b)

| ID | Qué dice 00-deuda-4b | Lo que verificó QA | Estado |
|---|---|---|---|
| P-17 / AC-WKT-02-11 | Sin bug; una línea `[TIME]` en cada despertar | `SuspendAccountingTests` (las 8 nuevas), `UnitySchedulerTests.EveryWakeIsLoggedAtRestWithBothSources`, `SystemSleepSourceTests.TheCallbackTakesTheSystemTick` pasan. La suspensión real, del usuario | Bien en lo automático; NO VERIFICABLE en equipo |
| QA-V4b-06 (AC-WKT-13-15) | El precalentado y el HUD armado en el negro; en el build, NO VERIFICABLE | Las pruebas del Editor pasan (§3.4). **La medida `[PERF]` del build tampoco se pudo hacer acá** (§6) | Pendiente de la medida |
| QA-V4b-R2-03 | Los *scripts* siguen si Unity revienta al cerrar con el *define* como se pidió | `I5D.DefineStepsSurviveTheCrashOnExit` pasa; en esta verificación no reventó ninguna vez | Cerrado |
| O-4 / O-5 (AC-WKT-13-16) | El `MotionDriver` aísla; el diálogo que no se arma vuelve a reposo | MDI y `4BD.ACompletionDialogThatCannotOpenStillReturnsToRest` pasan | Cerrado |
| O-1 | La barra de pasos termina sus finitos al irse | `4BD.TheStepBarLeavesWithoutItsFiniteMotions` pasa | Cerrado |
| O-P01 / O-P02 | Comentarios al día; la espera del diálogo segundo a segundo | `WorkoutGoldenTests.CompletedHoldReset` pasa | Cerrado |
| O-R2-01 (4b) | `run-unity-tests.ps1 -Platform PlayMode` borra solo su `TestResults.xml` | Las dos corridas de PlayMode lo dicen ("se borro el TestResults.xml …"); `I5D.PlayModeRunsRemoveOnlyTheirOwnEditorTestResults` pasa; esa carpeta no tenía ese archivo antes | Cerrado |
| QA-V4b-R2-01 (AC-WKT-16-07) | El modo compacto de la barra libre | §2.4 y §3.4 | Cerrado |

---

## 6. Prueba en equipo (build de Windows con simulador)

**NO VERIFICABLE.** El `RowMate.exe` de la demo del usuario estuvo abierto en `Builds/Windows/simulator` desde antes de empezar esta verificación hasta el final. Se miró con `Get-Process` cada 2 minutos durante 20 minutos, mientras corrían las capturas (11 intentos; registro en `<scratch>/it5/qa-main-r1/exe-wait.txt`: todos "abierto"), sin tocar su proceso. Los builds de desarrollo van solo a esa carpeta y pisar sus archivos con el *player* corriendo la deja a medias, así que no se compiló. La carpeta sigue con el build de la 4b.

Queda sin hacer, para cuando el usuario cierre su `RowMate.exe` (los pasos están en 00-deuda-4b §2.4 y 02-tech-design §3.7, siempre con `ROWMATE_DATA_DIR` en una carpeta temporal y `-logFile` en ella):

| Qué | AC | Lo que lo cubre hoy en el Editor |
|---|---|---|
| `[PERF]` de las tres entradas (`-rowmate-perf-entries 3 -rowmate-perf-routines "Prueba 4a;Cinco reps"`, con `prefs.json` sembrado) | AC-WKT-13-15 | El orden de la entrada (ITT, 4BD, BST) |
| `[PERF]` con los paneles (`-rowmate-perf-series`) | AC-WKT-18-21 | HBT (presupuesto, 0 B, mediana 0,73 ms) |
| Una rutina con serie de trabajo por distancia: los paneles cerrados por defecto, abrir y cerrar con mouse y teclado, la vuelta en curso y las anteriores, `Series anteriores` al pasar de repetición | AC-WKT-18-01 a -12 | SPV, SCT (la selección real), SKT (teclado real de UI Toolkit), DEMO-01 a -12 |
| Cerrar y abrir la app y ver los paneles como quedaron | AC-WKT-18-10 | `BootScenePreferencesTests.PreferencesSurviveARestart` |
| `Simular desconexión` sin saltos | AC-BLE-15-09 | WSM, SSM, VID-05 |
| Convivencia con la cuenta grande, el sello y `PAUSA` | AC-WKT-18-15, -16 | CAP-19 a -24, VID-09 |
| Matar el proceso a mitad de sesión: la sesión queda consistente | AC-WKT-07-11 (Q-12) | `WorkoutPersistenceTests` (de la 4a y la 4b) |

---

## 7. Revisión estática

### 7.1 Motion que no toca el dato y nada animando al salir

- Los `IMotion` de la 5 (`SeriesPanelMotion`, `SeriesColumnMotion`) reciben `HudSeriesChange` y devuelven *structs* de `SeriesTimelines`: ninguno lee ni escribe el motor, la sesión ni las preferencias; abrir y cerrar cambia la preferencia en el cuadro del toque y la motion solo anima el alto y los fundidos (02-tech-design §4.0). `PresentationIndependenceTests` (dotnet) e `ImmersiveHudTests.PresentationNeverTouchesTheDataWithThePanels` (PlayMode) lo fijan, y en los videos los valores de los paneles cambian en el cuadro de la banda (VID-01 cuadro 30, VID-02 cuadro 45).
- `SeriesColumn` es el único escritor de `style.translate` y `style.opacity` de los paneles (DT5-14).
- Nada animando al salir: `ITT.NothingAnimatesAfterTheExitWithThePanels` (con el resorte de un panel y una marca encendidos al terminar) y O-1 pasan; `Stop()` de las dos motions sale del `MotionDriver` y deja la capa de estado en 0 (D1-10).

### 7.2 0 B por cuadro, con el ayudante de cuadros reales

`Tests/UiSupport/FrameAllocations.cs` mide en **cuadros reales** (`yield return null`) con un `Recorder` de `GC.Alloc` del hilo principal prendido **dentro** de los dibujos observados (`SeriesPanelView.DrawObserver`, alrededor del `generateVisualContent`) y del paso de la UI de cada cuadro; no usa `GC.GetAllocatedBytesForCurrentThread`. El **control positivo** corre en la misma corrida (`FrameAllocationsTests.ThePositiveControlIsSeenInRealFrames`): 0 sin asignar, 30 de 30 con un `byte[64]` por repintado, 30 de 30 con uno por paso. `HBT.SeriesNoAllocationsPerFrame`: 0 en las cinco escenas. Límite conocido y documentado (B5-01): la lectura del remo de prueba, a 1 Hz, va afuera de la medida, y el repintado que UI Toolkit hace de los demás elementos no está observado (es el de la 4b, medido allá).

### 7.3 Presupuesto del HUD

El peor caso de la 4b con M5 da 54 / 55 y el de la 5, **56 / 57** (`DescribeVisible`): el tope de 60 de ARCHITECTURE §20.2 se cumple sin la junta (DT5-09 no se hizo; B0 lo explica: el sello dibujado no salía igual byte a byte). Cada panel es un elemento; sus *holders* y el hijo del área de toque están ocultos.

### 7.4 Los datos del usuario (AC-HIS-20-04)

En la carpeta de datos de la app del usuario se tomó, antes de empezar, el nombre, el tamaño, la fecha y un *hash* de cada archivo (en `<scratch>`, no en el repo): **no había `prefs.json`**. Al terminar todas las corridas: **sigue sin existir**, y los demás archivos tienen el mismo tamaño y la misma fecha (ninguno los tocó esta verificación; tampoco quedó un `TestResults.xml`). Las corridas de Unity y el arnés usan `ROWMATE_DATA_DIR` en carpetas temporales propias; cada toma usa una `MemoryPreferences`; las pruebas del almacén real usan `UiHostOptions.PreferencesFile` en una carpeta temporal que se borra.

### 7.5 Repositorio público

- La búsqueda con `git grep -nIE` de las rutas de usuario de Windows (con una o dos barras), de las de Git Bash y del prefijo de las carpetas de trabajo de Claude, sobre lo versionado: **0**. La misma búsqueda sobre los archivos modificados y los nuevos sin seguimiento (`git ls-files --others --exclude-standard`, `git diff --name-only`), este documento incluido: **0**.
- `manifest.json` e `INDEX.md` de `05-series-parciales/snapshots/` y `04b-hud-exr/snapshots/`: sin rutas de la PC (están en la búsqueda; además `ItFourAToolingTests.SnapshotOutputsHaveNoMachinePaths` pasa).
- 00-deuda-4b §1.1 quedó con el nivel que pidió el gate (QA-G5T-15): ni fechas, ni el nombre ni el contenido detallado de la base, ni la historia de sueño del equipo. Este documento no publica nada del equipo ni de la base del usuario: los registros de la espera y la línea de base de la carpeta de datos quedaron en `<scratch>`.

### 7.6 Unity 6 y el resto

Sin avisos de compilación nuevos (los 28 son los de antes: `Infrastructure/*Sqlite*` y dos pruebas viejas); el único `error CS` del registro es el `#error` buscado por `SimulatorModeTests.SimulatorDoesNotCompileInANonDevelopmentPlayer`. `Packages/manifest.json` sin cambios; ningún `.asmdef` nuevo. `JsonFilePreferences` escribe a `prefs.json.tmp` y reemplaza (`File.Replace` / `File.Move`), con las escrituras fuera del hilo principal; `AppRoot` espera hasta 1 s la pendiente al cerrar.

---

## 8. Issues

| ID | Sev. | Resp. | Qué | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **QA-V5-01** | menor | po | **AC-BLE-16-02 (FAIL)**: en L640 con el diálogo de completado abierto, la llave del simulador tapa el chip `Reps` (inerte, debajo del velo). El AC dice "tampoco tapa los paneles, cerrados ni abiertos, en los cinco perfiles". En L640 no hay hueco libre (B5-10), y el arnés lo acepta a propósito | it4b `CAP-10-dialogo-completado-l640-letra.png` (la llave en x 16-56, y 231-271 dp, sobre `Re` de `Reps`); `Snapshots/WrenchAudit.cs:109-113`, `:190-195` (`WrenchOverSeriesAccepted`); 02-tech-design B5-10 ("para PO y QA", sin decisión) | Que el PO decida: (a) sumar la excepción a AC-BLE-16-02 ("salvo en L640 con el diálogo de completado, sobre un panel inerte debajo del velo", como en B5-10), o (b) que el Architect busque otro lugar para la llave en ese caso (por ejemplo, sobre la barra de arriba, también inerte debajo del velo, si la 4b lo admite) |
| **QA-V5-02** | menor | architect | **La línea `Rep r/R · m` rueda entera cuando cambia de largo** (H5-D2-01 de WP-D2, sin corregir): al empezar cada repetición y en los 10, 100 y 1.000 m de cada una, todas las celdas ruedan (también `Rep 2/3 · `) y la línea se corre de costado. ANIMATION_BIBLE §2.4 pide que solo ruede lo que cambia | VID-04 cuadros 75-76 (`Rep 1/3 · 680 m` → `Rep 2/3 · 4 m`) y 134-136 (`8 m` → `12 m`); `Presentation/Controls/TabularCells.cs` (alinea las celdas a la derecha de toda la cadena) | El plan de `TabularCells` con el prefijo y el sufijo comunes (lo igual no rueda; el tramo del medio rueda alineado a su derecha), como propuso el Designer en 04-design-notes §15.5; volver a tomar VID-02 a -04 en dos corridas |
| **QA-V5-03** | menor | architect | **VID-06 muestra `0:01.0` `0 m`** al segundo 1, no `4 m` como pide spec §7.5 (H5-D2-02, sin corregir). Es del arnés: VID-06 no pone `ReadingZeroAtStart` y `L(1)` cuenta como reinicio (B5-09); el producto no cambia | *manifest* de VID-06, `values[24]`: `hero=0:01.0 0 m rep=Rep 1/5 · 0 m`; `Snapshots/SeriesCatalog.cs:537-538` | `s.ReadingZeroAtStart = true` antes de `TapRoutine("Cinco reps")` en VID-06 y volver a tomar `it5` dos veces |
| **QA-V5-04** | menor | architect | **VID-06 y VID-07 duran 1,5 s**; la spec (§7.5) dice 2,5 s (H5-D2-03). Lo que tienen que mostrar entra en 1,5 s | `Snapshots/SeriesCatalog.cs:537-545` (`Video(vid06, 1.5)`, `Video(vid07, 1.5)`); 45 cuadros cada uno | Llevarlos a 2,5 s junto con QA-V5-03, o que el PO ajuste §7.5 |
| **QA-V5-05** | menor | po | **AC-WKT-18-14 y AC-WKT-17-09 dicen que en vertical "los chips cerrados no entran"** en la zona del bote, pero con parciales abierto el chip `Reps` cerrado baja con él y entra en la franja de afuera (B5-06). Es lo que muestran las tomas que la spec pide (CAP-02, CAP-20) y la auditoría lo cuenta como la columna abierta; no es un defecto del producto, es la redacción | *manifest* de CAP-02 (`Reps` cerrado debajo de parciales abierto, y > 323,7, la zona) y CAP-20; `Snapshots/WrenchAudit.cs:252-290`; 02-tech-design B5-06 | Precisar el AC: "un chip cerrado que baja con un panel abierto cuenta como la columna abierta: solo la franja de afuera, nunca la columna del centro" |

Observaciones sin issue: el título del panel mide 0,941 en español y `Rep 1 2:05 1.112 m` 0,958 (las notas, 0,949 y 0,969; lo mide Unity, R5-02, dentro de lo que piden las comprobaciones); los valores por cuadro de VID-06 y VID-07 empiezan con el toque (36 y 31 de 45 cuadros).

---

## 9. NO VERIFICABLE (y por qué)

| Qué | Por qué | Quién lo cierra |
|---|---|---|
| AC-WKT-13-15 (`[PERF]` de la entrada en el build) y la ventana `[PERF]` de AC-WKT-18-21 | El `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator` (§6); riesgo aceptado (QA-G5T-14) | QA o el Architect, cuando el usuario cierre su `RowMate.exe` |
| La prueba en equipo de §6 (mouse y teclado reales, reiniciar la app, `Simular desconexión`, matar el proceso) | Ídem | Ídem |
| AC-WKT-02-11 (la suspensión real) | Hay que suspender el equipo; QA no lo hace | El usuario (00-deuda-4b §1.5) |
| Las partes de Android (el dedo, el área táctil, el primer cuadro al volver: AC-WKT-18-09, -13, AC-WKT-02-08) y el reemplazo atómico con IL2CPP (R5-04) | Faltan los módulos de Android e IL2CPP y el teléfono | Prerrequisitos del usuario (STATUS) |

---

## 10. Verificación hecha

1. **Legacy, solo lectura**: `test/series_tracker_test.dart:130-235` (para LT-55 a LT-58) y `docs/image-03.png`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó el módulo de credenciales de Strava.
2. **Las corridas** de §1, con los conteos reales copiados de la salida.
3. **Cada PNG** de `snapshots/` mirada con Read (38), y los nueve videos en hojas de cuadros con PyAV, contra 01-po-spec §7, 04-design-notes §9 y §15 y `docs/image-03.png`.
4. **La regeneración de `it4b`** contra `HEAD` (`git lfs smudge` y la diferencia de píxeles, caja por caja) y contra la corrida de la deuda; `it2`, `it3` e `it4a` tomadas otra vez.
5. **El repo**: ninguna fuente tocada; solo este documento y la corrida B de `it5` (los mismos bytes). Sin *commits*. Ninguna prueba temporal.

---

## Ronda 2

| Campo | Valor |
|---|---|
| Autor | rowmate-qa (verificación principal, ronda 2; el único agente de la verificación que usó Unity) |
| Fecha | 2026-10-30 |
| Entrada | [02-tech-design](02-tech-design.md) "Cambios por verificación (ronda 1)" (V5-01 a V5-11), [01-po-spec](01-po-spec.md) "Cambios por verificación (ronda 1)", las lentes [05-qa-lens-paridad](05-qa-lens-paridad.md) y [05-qa-lens-motion](05-qa-lens-motion.md), la ronda 1 de arriba, las tomas de [snapshots/](snapshots/INDEX.md) y `docs/image-03.png` de [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) |
| Legacy | `3ae0f3a`, solo lectura (`docs/image-03.png`). `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó el módulo de credenciales de Strava |
| Artefactos | `<scratch>/it5/qa-main-r2/` (resultados de dotnet y de Unity, registros, corridas A del arnés, hojas de cuadros de los videos, la espera del `RowMate.exe`, las líneas de base de `git status` y de la carpeta de datos del usuario) |
| Repo | Sin *commits*. Lo único que escribió esta ronda en el repo es esta sección (y la línea que la anuncia arriba) y la corrida B de `it5`, con los mismos bytes que había (en `manifest.json`, solo `durationMs`) |

### R2.0 Veredicto

**APROBADO CON OBSERVACIONES.** 0 bloqueantes, 0 mayores y **1 menor** (QA-V5-R2-01, del Designer: DD5-27 en `ColumnScroll` sigue sin decidir; no hace fallar ningún AC). Los dos mayores de las lentes (QA-V5-P01 y LM5-01) y los diez menores de la ronda 1 y de las lentes están **resueltos** y verificados uno por uno (§R2.2).

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix`) | Compilación **0 avisos, 0 errores** · herramientas **192 / 192** · Core **2541: 2507 pasan, 0 fallan, 34 ignoradas** (+5 contra la ronda 1: QA-V5-P01 y `DigitRunsTests`) |
| Unity EditMode | **2601: 2563 pasan, 0 fallan, 38 ignoradas**, 0 inconclusas |
| Unity EditMode con simulador | **2793: 2755 pasan, 0 fallan, 38 ignoradas**, 0 inconclusas; el *define* restaurado |
| Unity PlayMode | **426 / 426** |
| Unity PlayMode con simulador | **495 / 495**; el *define* restaurado |
| Matriz (`-UnityResults` con los 4 XML) | 658 IDs: **PASA 656, FALLA 0, IGNORADO 2** (AC-APP-15-03 [Apple] y AC-WKT-02-10 [Android]), PENDIENTE UNITY 0, SIN TEST 0. Los 74 AC de la 5 y LT-47 a LT-61: **89 de 89 en PASA** |
| Arnés `it5` | A y B: **47 tomas, 0 con problemas, 0 sondas de aspecto que fallan**; **A = B byte a byte** (48 de 49 archivos; `manifest.json`, solo `durationMs`); B igual a lo que había en el repo (la corrida del Architect) |
| `it4b` | A: 51 tomas, 0 con problemas; **52 de 53 iguales byte a byte** a lo versionado (`manifest.json`, solo `durationMs`) |
| `it3` / `it2` / `it4a` | 47 / 41 / 49 tomas, 0 con problemas; 48 de 49, 43 de 44 y 12 de 13 archivos iguales byte a byte a lo versionado (`manifest.json`, solo `durationMs`) |
| Build de Windows con simulador | **NO VERIFICABLE**: el `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator` en los 11 intentos (cada 2 minutos, 20 minutos); no se tocó su proceso (§R2.6) |
| **Criterios** (74) | **71 PASS · 0 FAIL · 3 NO VERIFICABLE** (AC-BLE-16-02 pasa de FAIL a PASS) |
| Repo público | `git grep -nIE` de rutas de usuario: **0** en lo versionado y **0** en los 303 archivos modificados o nuevos sin seguimiento; ningún documento publica datos del equipo ni de la base del usuario (§R2.7) |

### R2.1 Corridas

Desde la raíz del repo, una instancia de Unity por vez (solo el `unity.exe serve` de la Hub, que no se tocó; ningún Editor abierto con este proyecto, sin `Temp/UnityLockfile` tomado). Esta ronda no editó ninguna fuente.

| # | Comando | Resultado |
|---|---|---|
| 1 | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it5/qa-main-r2/dotnet` | `0 Warning(s) 0 Error(s)`; `Total tests: 192 Passed: 192`; `Total tests: 2541 Passed: 2507 Skipped: 34`; matriz sin Unity: PASA 531, FALLA 0, IGNORADO 2, PENDIENTE UNITY 125, SIN TEST 0 |
| 2 | `tools/unity/run-unity-tests.ps1 -Platform EditMode` | `total 2601, pasan 2563, fallan 0, ignorados 38, inconclusos 0`; Unity sale con 0 |
| 3 | `… -Platform PlayMode` | `total 426, pasan 426, fallan 0`; "se borro el TestResults.xml que el Test Framework dejo en la carpeta de datos del Editor (O-R2-01)" |
| 4 | `… -Platform EditMode -Simulator` | `total 2793, pasan 2755, fallan 0, ignorados 38, inconclusos 0`; los tres pasos con 0 |
| 5 | `… -Platform PlayMode -Simulator` | `total 495, pasan 495, fallan 0`; los tres pasos con 0 |
| 6 | `run-tests.ps1 -UnityResults <scratch>/…/unity` (antes de las capturas, sin el *define* puesto) | Los mismos números de dotnet que la corrida 1; matriz: PASA 656, FALLA 0, IGNORADO 2, PENDIENTE UNITY 0, SIN TEST 0 |
| 7 | `tools/unity/capture-snapshots.ps1 -Set it5 -Out <scratch>/…/capA-it5` (A) y sin `-Out` (B, al repo) | Las dos: `listo: 47 tomas, 0 con problemas, 0 sondas de aspecto que fallan`; atlas `0 glifos antes, 1484 después` |
| 8 | `capture-snapshots.ps1 -Set it4b -Out <scratch>` | `listo: 51 tomas, 0 con problemas, 0 sondas de aspecto que fallan` |
| 9 | `capture-snapshots.ps1 -Set it3`, `-Set it2`, `-Set it4a`, cada uno con `-Out <scratch>` | 47 / 41 / 49 tomas, 0 con problemas, 0 sondas que fallan |
| 10 | Espera del `RowMate.exe` de `Builds/Windows/simulator` (un `Get-Process` cada 2 minutos) | **Abierto en los 11 intentos, durante 20 minutos**; abierto también al terminar. No se compiló |

**Lo que el Architect reportó y lo que dio acá**: los mismos números (dotnet 192 y 2541 / 2507; EditMode 2601 / 2563, con simulador 2793 / 2755; PlayMode 426 y 495; la matriz 656 / 0 / 2).

**Unity que revienta al cerrar (QA-V4b-R2-03)**: pasó **3 veces en los 16 cambios de *define*** de esta ronda (`EnableForTests` de la corrida A de `it5`, `RestoreAfterTests` de `it4b` y uno de `it4a`, con `-1073741819`). Las tres veces el *script* dijo "Unity revento al cerrar despues de cambiar el define, pero quedo como se pidio: se sigue" y la corrida terminó bien, como pide 00-deuda-4b §3.

**Avisos de compilación**: los de antes (`SqliteSessionRepository`, `SqliteRoutineRepository`, `RoutineMotionDebtTests`, `SimulatorModeTests`); el único `error CS` del registro es el `#error` buscado de `SimulatorReleaseGuard` (`SimulatorModeTests.SimulatorDoesNotCompileInANonDevelopmentPlayer`).

**Después de todas las corridas**: `ProjectSettings/ProjectSettings.asset` sin `ROWMATE_SIMULATOR`, sin la marca `Library/RowMate/simulator-tests.txt`, nada cambiado en `ProjectSettings`, `Packages`, `Assets/RowMate/UI` ni `Assets/RowMate/Settings`, y `git status --porcelain` **idéntico** al de antes de empezar (239 entradas). La escena `Assets/InitTestScene….unity` que hay en la carpeta es de una corrida anterior a esta ronda y está en `.gitignore`.

### R2.2 Los issues corregidos, uno por uno

#### QA-V5-P01 (mayor, de la lente de paridad): resuelto

- **Código** (`App/Workout/WorkoutHudStore.cs:342-437` y `:442-463`): `ComposeSeries` reusa las filas de `Series anteriores` solo si `ReferenceEquals(content.PreviousReps, previousRepsSource)` y la sesión es la misma; `SeriesFor` guarda `_seriesRepsSource = state.PreviousReps` en cada composición. `RepRows` ya no compara `Rep` ni metros: con otra lista arma las filas desde el tracker. La premisa se cumple: `SeriesTracker.EnterStep` (`Core/Workout/SeriesTracker.cs:300-321`) **reemplaza** `_previousReps` por un arreglo nuevo en cada repetición nueva (o por `NoReps` al cambiar de serie), y `SeriesPanels.For` (`Core/Workout/SeriesPanels.cs:135-138`) pasa `state.PreviousReps` sin copiar: la misma instancia es el mismo contenido. Un cambio de idioma deja `previous` en `null` y rearma todo.
- **Prueba**: `WorkoutHudStoreTests.TheRepsOfAnotherSeriesAreRebuiltAfterTheBackground` es la sonda de la lente (A ×2 y B ×2 [500 m; 60 s], la Rep 1 con 620 m en las dos y split 2:05 / 1:40; en primer plano y con segundo plano de 200,25 a 540,25 s) y afirma el split contra el del tracker, también 20 s después. **Pasa** en dotnet, EditMode y EditMode con simulador; el rojo contra el código anterior está en `<scratch>/it5/fix-architect-r2/red-p01.log` (`Expected: "1:40" But was: "2:05"`). `TheSeriesChangeOnlyOnTheSecondAndReuseTheirRows` (el reuso dentro de una serie) sigue pasando.
- **AC**: AC-WKT-02-08 y AC-WKT-18-07 en PASS también en el caso de la lente (la matriz: 53 y 28 resultados que pasan).

#### LM5-01 (mayor, de la lente de motion): resuelto en (a) y (b); (c) pasa a QA-V5-R2-01

- **(a) El recorte por un dato** (`Presentation/Workout/SeriesColumn.cs:161-238`): `Apply` llama `ClampScroll(instant || first)` después de `Compute` y `PlaceBoth`. `ClampScroll` (`:459-467`) lleva el destino al tope nuevo con `WheelTo(max, instant)`: `spring.snappy` desde donde está, en el acto solo con `Instant` (la vuelta del segundo plano, el primer estado) o con reducir movimiento. `ScrollMaxBase` sale del contenido final que `Compute` acaba de calcular, así que el arrastre y la rueda (`OnDragged`, `OnWheeled`) parten del destino nuevo y un tope 0 ya no deja la columna trabada. `VisibleBounds` (la llave con el diálogo) usa el destino del desplazamiento.
  - `SeriesColumnTests.TheColumnScrollFollowsAColumnThatShrinks` (TH y L640: los dos abiertos a los 380,2 s, la columna arrastrada al tope, la serie que termina a los 541,2 s) afirma el desplazamiento dentro del tope (0) y la cabecera de parciales entre el techo y el piso, elegida por `panel.Pick` y con el título a alfa 1. **Pasa** en PlayMode y en PlayMode con simulador; el rojo contra el código anterior está en `<scratch>/it5/fix-architect-r2/unity-red-play.log`.
- **(b) Ningún recorte salta**: `OnExpansion` y `Replace` recortan con el mismo criterio; en `RowsScroll` (`SeriesPanelView.cs:322-391` y `:582-591`), cerrar lleva las filas a 0 con `spring.snappy` mientras el contenido se apaga, `Retarget` y `Apply` recortan con el resorte y DD5-27 suma solo lo que cabe debajo del tope (lo que no, lo hace el empuje en su proporción, `_pushScale`).
  - `SCT.ClosingAPanelWithTheColumnAtTheTopDoesNotJump` (TH: el desplazamiento dibujado, cuadro a cuadro, < 12 dp por cuadro y termina en el tope nuevo; antes, 50,6 dp en el cuadro del toque) y `SCT.TheRowsScrollIsClampedWithoutJumps` (W1280 con S-LARGO: la vuelta 2000 con las filas al tope, < 8 dp por cuadro; cerrar parciales, < 12 dp por cuadro y a 0 al final): **pasan**. `SCT.ColumnScrollInLandscape` y `RowsScrollOnTheDesktop` siguen pasando.
- **(c) DD5-27 en `ColumnScroll`**: el Architect no lo cambió y lo dejó para el Designer (02-tech-design V5-04): en la columna, la fila nueva entra debajo de la cabecera y de la vuelta en curso, que se desplazan con la columna, y sumar 22·sp no deja quieto "lo que se ve". Hoy, con la columna desplazada, una vuelta nueva **baja lo que se ve con el empuje, sin saltos**: AC-WKT-18-13 ("lo que se ve no salta") se cumple. Queda que 04-design-notes §5.2 (`:309`) dice otra cosa que el código: QA-V5-R2-01.
- **AC**: AC-WKT-18-13 (58 resultados que pasan) y AC-WKT-18-20 (8) en PASS; las 38 PNG, iguales byte a byte a la ronda 1.

#### QA-V5-01 (menor, ronda 1; AC-BLE-16-02 en FAIL): resuelto, AC-BLE-16-02 pasa a PASS

- **Decisión del PO** (01-po-spec, "Cambios por verificación (ronda 1)"): la opción (a). AC-BLE-16-02 dice ahora "tampoco tapa los paneles … con **una sola excepción**": con el diálogo de completado, la llave busca primero un lugar sin tocar la columna (TV y P360, debajo del diálogo; TH, a la derecha de la columna) y, si no queda ninguno (de los cinco perfiles, solo L640), queda en su lugar de la 4b sobre la columna inerte y debajo del velo; sin el diálogo o con un lugar libre, no hay excepción. AC-WKT-18-16 remite a esa excepción.
- **Lo que hace la app**: `WrenchPlacement.LeftLaneAvoiding` (`Simulator/Panel/WrenchPlacement.cs:195-224`) prueba el carril sin la columna, después el carril a la derecha de la columna y solo entonces el lugar de la 4b con `OverSeries`. `WrenchAudit.WrenchOverSeriesAccepted` (`Snapshots/WrenchAudit.cs:191-195`) acepta el cruce **solo** con un diálogo arriba y `OverSeries`.
- **Pruebas**: `WrenchPlacementTests.WithTheDialogTheWrenchAvoidsTheSeriesColumn` afirma `OverSeries` y el cruce **solo** en L640 (con los lugares de TV, P360, TH y L640) y `TheWrenchNeverCoversTheSeriesPanels`, los cinco perfiles sin diálogo: pasan (dotnet, herramientas 192 / 192).
- **Tomas**: en los dos *manifest* (it4b e it5, 79 tomas con la llave) se cruzaron los rectángulos de `sim-open` con `hud-laps-panel` y `hud-reps-panel`: **un único cruce, it4b CAP-10** (L640, `Corta` a los 32,0 s con el diálogo; la llave en x 16-56 e y 231-271 dp sobre el `Re` de `Reps`). Es exactamente la excepción del AC. En it4b CAP-09 (TH con el diálogo) la llave queda a la derecha de la columna (x 167,2), sin tocarla.

#### Los demás issues de la ronda 1 y de las lentes

| Issue | Corrección (02-tech-design / 01-po-spec) | Verificación de QA | Estado |
|---|---|---|---|
| QA-V5-02 (la línea `Rep` rueda entera) | V5-09: `Core/Design/DigitRuns` y `TabularCells.DrawFromLeft` ruedan por tramos | `DigitRunsTests` 4 / 4 (dotnet y EditMode); `SPV.TheRepLineRollsOnlyWhatChanges` pasa. **Visto en VID-04** (hojas `vid04-rep-a.png` y `vid04-rep-b.png` en `<scratch>/it5/qa-main-r2/sheets/`): en los cuadros 75-76, `Rep `, `/3 · ` y ` m` quedan quietos y ruedan solo `1`→`2` y `680`→`4`; en 134-137, solo `8`→`12`, con ` m` que se corre con el ancho. En VID-05, `109 m`→`113 m` y `113 m`→`117 m` ruedan solo las cifras que cambian | Resuelto |
| QA-V5-03 (VID-06 con `0 m`) | V5-10: `L(0)` antes del toque | *manifest* de VID-06: `values[24]` = `hero=0:01.0 4 m rep=Rep 1/5 · 4 m band=00:01`; visto en la hoja (cuadro 40: `0:01.0` `4 m`, `Rep 1/5 · 4 m`) | Resuelto |
| QA-V5-04 (VID-06 y VID-07 de 1,5 s) | V5-11: 2,5 s | 75 cuadros cada uno a 30/1 (PyAV); `INDEX.md` dice "(2,5 s)" | Resuelto |
| QA-V5-05 (redacción de la zona del bote) | 01-po-spec: AC-WKT-18-14 y AC-WKT-17-09 con el chip que baja con un panel abierto | El texto nuevo describe lo que muestran CAP-02 y CAP-20 (sin cambios) | Resuelto |
| QA-V5-P02 (clave de otro tipo) | 01-po-spec: AC-WKT-18-11 separa "falta, vacío o dañado" de "una clave de otro tipo" | `JsonFilePreferencesTests` (otro tipo que conserva las demás claves) pasa; el texto dice lo que hace la app | Resuelto |
| LM5-02 (el anillo de foco en `ColumnScroll`) | V5-05: el panel enfocado con el teclado se trae a la vista; el anillo se recorta a la ventana | `SKT.InColumnScrollTheFocusedPanelComesIntoView` (TH y L640) pasa; CAP-26 igual byte a byte | Resuelto |
| LM5-03 (la `HudMotion` que lanzó queda colgada) | V5-06: `HudMotion.Tick` deja `_inDriver` en falso y vuelve a lanzar (`Presentation/Motion/Hud/HudMotion.cs:48-80`) | `MDI.AHudMotionThatThrewRecovers` pasa (EditMode y con simulador); `AThrowingMotionLeavesAndTheOthersKeepGoing` sigue pasando | Resuelto |
| LM5-04 (0 B: escenas que faltaban) | V5-07: 30 cuadros y `SeriesNoAllocationsPerFrameInMoreScenes` | §R2.7: las diez escenas en 0, con el control positivo en la misma corrida | Resuelto |
| LM5-05 (la suscripción al salir) | V5-08: `ITT.NothingAnimatesAfterTheExitWithThePanels` mira `SubscriberCount` | Pasa (PlayMode y con simulador) | Resuelto |

### R2.3 Capturas y videos

**Qué cambió desde la ronda 1.** Contra la corrida A de la ronda 1 (`<scratch>/it5/qa-main-r1/capA-it5`), en `snapshots/` cambiaron **solo** VID-04, VID-05, VID-06, VID-07, `INDEX.md` (la duración de VID-06 y VID-07) y `manifest.json`. **Las 38 PNG son iguales byte a byte** a las de la ronda 1, que se miraron enteras con Read (§2.2); en esta ronda se volvieron a mirar CAP-16 y CAP-18 (la máscara de TH y la ventana de escritorio, contra `docs/image-03.png` del legacy: la misma lectura de los dos paneles): sin cambios.

**Los cuatro videos que cambiaron**, en hojas de cuadros con PyAV (`uv run --offline`, sin red; en `<scratch>/it5/qa-main-r2/sheets/`):

| Video | Qué se vio | Contra la spec / las notas |
|---|---|---|
| VID-04 (150 cuadros, 30/1) | Cuadro 75: `Rep 1/3 · 680 m` → `Rep 2/3 · 4 m` en el mismo cuadro que la banda pasa a `03:21`; ruedan solo `1`→`2` y `680`→`4`, el texto queda quieto y ` m` se corre con el ancho interpolado. 77-81: `Sin parciales` / `en este paso` se va, entra `0:01.0` `4 m` y la línea baja a su lugar con el alto (G de 106 a 91,1). 134-137: `8 m`→`12 m`, solo rueda `12` | Bien (ANIMATION_BIBLE §2.4: solo rueda lo que cambia; AC-WKT-18-19) |
| VID-05 (300 cuadros, W1280) | `0:29.0` `109 m` → `0:30.0` `113 m` → `0:31.0` `117 m`; con la desconexión, de `0:32.0` a `0:34.0` con `117 m` y `Rep 2/3 · 117 m` quietos mientras la banda sigue; `0:35.0` `121 m`, `0:36.0` `125 m`, `0:38.0` `132 m`: sin salto y sin negativos | Bien (AC-BLE-15-09) |
| VID-06 (75 cuadros, 2,5 s) | El diálogo de `Cinco reps`, el negro, la cascada: los dos paneles abiertos entran con SPM ya con `0:00.0` `0 m`, `Rep 1/5 · 0 m` y `Sin repeticiones completas`; al segundo 1, `0:01.0` `4 m` y `Rep 1/5 · 4 m`; al 2, `0:02.0` `8 m` | Bien (DV-48, spec §7.5) |
| VID-07 (75 cuadros, 2,5 s) | `0:52.0` `208 m`, `Rep 2/3 · 208 m` y `Rep 1 2:05 680 m` en los 31 cuadros con valores (el HUD funde desde el 13-14 y se va en el 20); después, Workout en reposo hasta el final. La banda pasa de `2008 m` a `2012 m` en el cuadro 9, con la lectura de los 252,5 s y **antes** del toque de `Terminar` (el diálogo de confirmación abierto no pausa): durante la salida no cambia nada | Bien (AC-WKT-13-08) |

**Reglas del arnés** (las de §2.1, comprobadas otra vez en el *manifest* de B): 27 tomas con `inFlight` vacío; en las demás, solo lo continuo (`PillRowMotion`, `StepPulse`, `CenterDiscMotion`, `AccentFlash`, `SealMotion`) y rodillos de la banda pasada su mitad; **ningún** `SeriesPanelMotion` ni `SeriesColumnMotion` en vuelo. Los nueve videos con `passed: true` y sus valores por cuadro (90, 120, 180, 150, 300, 66, 31, 120 y 90). Fuera de VID-04 a -07 y `durationMs`, el *manifest* es el de la ronda 1, así que valen sin cambios la `MemoryPreferences` por toma, la regla del toque a 0,82 s, CAP-11 a 881,2 s y DEMO-11 a 31,6 s, S-LARGO con `OverrideSeries`, el contorno de la zona del bote en CAP-03 y CAP-14 a -18, y `HudOverlap` / `WrenchAudit` con los rectángulos visuales (§2.1).

**Dos corridas iguales**: A (`<scratch>`) contra B (repo): 48 de 49 archivos iguales byte a byte; `manifest.json` igual salvo `durationMs`. B contra lo que había en el repo antes de esta ronda: igual (48 de 49; `manifest.json`, solo `durationMs`).

**`it4b`** (la regeneración única, ya revisada en §2.4): la corrida A de esta ronda da los mismos bytes que lo versionado en 52 de 53 archivos (`manifest.json`, solo `durationMs`), también después del cambio de `HudMotion` (LM5-03); las del libre siguen iguales. **`it3`, `it2` e `it4a`**: tomadas otra vez a `<scratch>`, iguales byte a byte a lo versionado (48 de 49, 43 de 44 y 12 de 13; en `manifest.json`, solo `durationMs`); `git status` no tiene entradas en sus carpetas. Ninguna toma de otra iteración cambió sin motivo.

### R2.4 Criterios de aceptación (74)

Vale la matriz de §3 con estos cambios. La matriz automática da **PASA** en los 89 IDs (74 AC y LT-47 a LT-61), con más resultados en los que tocaron las correcciones: AC-WKT-18-07 (25 → 28), -18-12 (6 → 8), -18-13 (52 → 58), -18-16 (17 → 19), -18-18 (35 → 39), -18-19 (95 → 108), -18-20 (6 → 8), -18-21 (8 → 10), AC-WKT-13-16 (4 → 6), AC-WKT-02-08 (50 → 53) y AC-APP-05-07 (39 → 41).

| AC | Ronda 1 | Ronda 2 | Evidencia nueva |
|---|---|---|---|
| AC-BLE-16-02 | FAIL (QA-V5-01) | **PASS** | El texto nuevo del PO admite una sola excepción; `WrenchPlacementTests` la fija solo en L640; `WrenchAudit` la acepta solo con el diálogo y `OverSeries`; el único cruce en 79 tomas con la llave es it4b CAP-10 (§R2.2) |
| AC-WKT-02-08 | PASS (FAIL en el caso de QA-V5-P01, según la lente) | **PASS** | `TheRepsOfAnotherSeriesAreRebuiltAfterTheBackground`; la parte de Android sigue NO VERIFICABLE |
| AC-WKT-18-07 | PASS (ídem) | **PASS** | Ídem |
| AC-WKT-18-12 | PASS (la lente: el foco en `ColumnScroll`, LM5-02) | **PASS** | `SKT.InColumnScrollTheFocusedPanelComesIntoView` |
| AC-WKT-18-13 | PASS (la lente: FALLA, LM5-01) | **PASS** | `SCT.TheColumnScrollFollowsAColumnThatShrinks`, `ClosingAPanelWithTheColumnAtTheTopDoesNotJump`, `TheRowsScrollIsClampedWithoutJumps`. Observación: QA-V5-R2-01 |
| AC-WKT-18-19 | PASS con la observación QA-V5-02 | **PASS** | `DigitRunsTests`, `SPV.TheRepLineRollsOnlyWhatChanges`; VID-04 y VID-05 mirados (§R2.3) |
| AC-WKT-18-20 | PASS | **PASS** | `SCT.TheColumnScrollFollowsAColumnThatShrinks` (la columna no llega vacía al diálogo) |
| AC-WKT-18-21 | NO VERIFICABLE (parcial) | **NO VERIFICABLE (parcial)** | Lo del Editor pasa y cubre más: 56 / 57 elementos (tope 60); **0 asignaciones en las diez escenas** (§R2.7); el trabajo de los paneles, mediana 0,818 ms y peor 1,083 ms en 60 cuadros. La ventana `[PERF]` del build, sin medir (§R2.6) |
| AC-WKT-13-15 | NO VERIFICABLE | **NO VERIFICABLE** | El orden de la entrada en el Editor pasa (ITT, 4BD, `BootSceneTests`); la medida `[PERF]` del build, sin hacer (§R2.6) |
| AC-WKT-02-11 | NO VERIFICABLE | **NO VERIFICABLE** | dotnet en verde (con `R-4S`); la suspensión real, del usuario |
| AC-WKT-18-11, -18-14, -18-16, AC-WKT-17-09 | PASS | **PASS** | Cambió el texto (QA-V5-P02, QA-V5-05, la remisión a la excepción de la llave), no lo que se verifica |
| AC-WKT-13-16 | PASS | **PASS** | Más `MDI.AHudMotionThatThrewRecovers` |
| AC-WKT-13-09 | PASS | **PASS** | `ITT.NothingAnimatesAfterTheExitWithThePanels` con `SubscriberCount` |
| Los demás (58) | PASS | **PASS** | Sin cambios: las mismas pruebas en verde en las cinco corridas (§3) |

**Conteo**: 74 = **71 PASS** + **0 FAIL** + **3 NO VERIFICABLE** (AC-WKT-13-15, AC-WKT-18-21 en su parte de build, AC-WKT-02-11). LT-47 a LT-61: 15 de 15 en verde, sin cambios (§4).

### R2.5 La deuda de la 4b declarada resuelta

Sin cambios contra §5. En esta ronda pasan otra vez, en las corridas de Unity, `ITT.TheHudIsBuiltInTheBlack`, `WithReducedMotionTheHudIsThereAtOnce`, `4BD.HudWarmupIsNotSeenAndLeavesNothing`, `TheFirstImmersiveAfterTheWarmupOpensTheSame`, `BootSceneTests.TheBootWarmsUpTheHudAndLeavesNothing`, `ACompletionDialogThatCannotOpenStillReturnsToRest`, `TheStepBarLeavesWithoutItsFiniteMotions`, `FBT.CompactBarSeparatesTheTitleFromTheClock`, `EnglishAndLandscapeKeepTheNormalBar`, `UnitySchedulerTests.EveryWakeIsLoggedAtRestWithBothSources`, `SystemSleepSourceTests.TheCallbackTakesTheSystemTick`, MDI, `I5D.DefineStepsSurviveTheCrashOnExit` y `PlayModeRunsRemoveOnlyTheirOwnEditorTestResults` (las dos corridas de PlayMode lo dijeron); en dotnet, `WorkoutGoldenTests.CompletedHoldReset` y `SuspendAccountingTests`. QA-V4b-R2-03 se vio de verdad tres veces (§R2.1) y los *scripts* siguieron. **La medida `[PERF]` de AC-WKT-13-15 en el build sigue sin hacerse** (§R2.6): es lo único de la deuda que queda abierto, con el riesgo ya aceptado (QA-G5T-14); la suspensión real (AC-WKT-02-11) la verifica el usuario.

### R2.6 Prueba en equipo (build de Windows con simulador)

**NO VERIFICABLE, otra vez.** El `RowMate.exe` de la demo del usuario siguió abierto en `Builds/Windows/simulator` durante toda la ronda. Se miró con `Get-Process` cada 2 minutos durante 20 minutos (11 intentos, registro en `<scratch>/it5/qa-main-r2/exe-wait.txt`: todos "abierto") y una vez más al terminar las capturas: abierto. No se tocó su proceso y no se compiló (pisar sus archivos con el *player* corriendo deja la carpeta a medias); la carpeta sigue con el build de la 4b. Quedan sin hacer, con los pasos de §6 y siempre con `ROWMATE_DATA_DIR` en una carpeta temporal:

- `[PERF]` de las tres entradas (AC-WKT-13-15) y la ventana `[PERF]` con los paneles (AC-WKT-18-21);
- la rutina con serie de trabajo por distancia: los paneles cerrados por defecto, abrir y cerrar con mouse y teclado, la vuelta en curso y las anteriores, `Series anteriores` al pasar de repetición; cerrar y abrir la app y ver los paneles como quedaron; `Simular desconexión` sin saltos; la convivencia con la cuenta grande, el sello y `PAUSA`; matar el proceso a mitad de sesión.

Lo cubren en el Editor, en verde en esta ronda: SPV, SCT (la selección real y los recortes de LM5-01), SKT (teclado real de UI Toolkit, también en `ColumnScroll`), `BootScenePreferencesTests.PreferencesSurviveARestart` (dos arranques de `AppRoot` sobre la misma carpeta temporal), WSM `SimulateDisconnectInTheCurrentLap`, `SeriesSimulatorTests.ADisconnectDuringAWorkKeepsTheLapMeters`, `WorkoutPersistenceTests`, DEMO-01 a -12, CAP-19 a -24, VID-05 y VID-09. **Para el coordinador**: pedirle al usuario que cierre su `RowMate.exe` para hacer estas pruebas antes del cierre, o que las haga él con el build que quede en esa carpeta.

### R2.7 Revisión estática

- **Motion que no toca el dato**: lo nuevo es de presentación. `DigitRuns` (Core, puro) solo parte textos en arreglos del que llama; `TabularCells` arma los tramos en el cuadro del dato y dibuja; `SeriesColumn.ClampScroll` y los recortes de `SeriesPanelView` mueven solo el desplazamiento; `HudMotion.Tick` solo cambia su propio estado. Ninguno lee ni escribe el motor, la sesión ni las preferencias (`PresentationIndependenceTests` e `ImmersiveHudTests.PresentationNeverTouchesTheDataWithThePanels` pasan); en VID-04 la línea `Rep` cambia en el mismo cuadro que la banda (75).
- **Nada animando al salir**: `ITT.NothingAnimatesAfterTheExitWithThePanels` (ahora también con `SubscriberCount` en reposo después de las dos salidas) y `4BD.TheStepBarLeavesWithoutItsFiniteMotions` pasan; con LM5-03, una `HudMotion` que lanzó ya no queda creyéndose dentro del `MotionDriver`.
- **0 B por cuadro, con el ayudante de cuadros reales** (`Tests/UiSupport/FrameAllocations.cs`: `yield return null` y el `Recorder` de `GC.Alloc` dentro de los dibujos observados y del paso de cada cuadro; ninguna prueba de Unity usa `GC.GetAllocatedBytesForCurrentThread`). Control positivo en la misma corrida (`FAT.ThePositiveControlIsSeenInRealFrames`): 0 sin asignar, 30 de 30 con un `byte[64]` por repintado y 30 de 30 con uno por paso. `HBT.SeriesNoAllocationsPerFrame`: abrir (31 dibujos), la vuelta nueva (14), el cambio por pieza (16), la repetición nueva (32) y cerrar (31), **0** en 30 cuadros cada una. `HBT.SeriesNoAllocationsPerFrameInMoreScenes`: la serie que empieza (19 dibujos en 36 cuadros), la marca que se apaga (48 en 48), el anillo de foco (31), la rueda en W1280 (30) y la máscara de TH (60), **0** en todas. Sigue sin medir la capa de *hover* (el puntero de prueba no la prende; documentado en V5-07).
- **Presupuesto del HUD**: el peor caso de la 5, **56 elementos en TV y P360 y 57 en TH, L640 y W1280** (tope 60); el de la 4b con M5, 54 / 55. El trabajo de los paneles, mediana 0,818 ms y peor 1,083 ms en 60 cuadros del Editor (en la ronda 1, 0,730 y 0,815; no hay criterio en ms fuera de la ventana `[PERF]` del build).
- **Unity 6 y el resto**: sin *asmdef* nuevos; `Packages/manifest.json` sin cambios; ningún aviso de compilación nuevo (§R2.1).
- **Los datos del usuario (AC-HIS-20-04)**: antes de empezar se tomó, en `<scratch>`, el nombre, el tamaño y la fecha de cada archivo de la carpeta de datos de la app del usuario: no había `prefs.json`. Al terminar, **los mismos archivos con el mismo tamaño y la misma fecha**, sin `prefs.json` y sin `TestResults.xml` (el que deja el Test Framework en PlayMode lo borró el *script*, O-R2-01). Las corridas usaron `ROWMATE_DATA_DIR` en carpetas temporales propias y cada toma una `MemoryPreferences`.
- **Repositorio público**: la búsqueda con `git grep -nIE` de las rutas de usuario de Windows (con una o dos barras), de las de Git Bash y del prefijo de las carpetas de trabajo de Claude (el patrón de la tarea, que no se copia acá para que el documento no se encuentre a sí mismo) sobre lo versionado: **0**. La misma búsqueda sobre los 303 archivos modificados o nuevos sin seguimiento (`git diff --name-only` y `git ls-files --others --exclude-standard`, este documento incluido): **0**. Esta sección cita la carpeta de trabajo como `<scratch>`, el legacy con su enlace y rutas relativas al repo; no publica nombres, datos del equipo ni de la base del usuario (las líneas de base quedaron en `<scratch>`).
- `git -C ../rowmate-legacy status --porcelain`: vacío al empezar y al terminar.

### R2.8 Issues

| ID | Sev. | Resp. | Qué | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **QA-V5-R2-01** | menor | designer | **DD5-27 en `ColumnScroll` sigue sin decidir**, y 04-design-notes dice algo que el código no hace. §5.2 (`:309`) pide, para el desplazamiento en general, que una fila que entra con la vista desplazada sume 22·sp·k al desplazamiento "así lo que se ve no se mueve"; el código lo hace solo en `RowsScroll` (W1280). En `ColumnScroll` (TH, L640), con la columna desplazada, una vuelta nueva baja lo que se ve con el empuje (animado, sin saltos: AC-WKT-18-13 se cumple). El Architect explicó por qué la regla no sirve tal cual en la columna (la fila entra debajo de la cabecera y de la vuelta en curso, que se desplazan con ella) y dejó una propuesta (02-tech-design V5-04; LM5-01 c) | `Presentation/Workout/SeriesPanelView.cs:326-344` (la compensación solo con `_rowsScrollRule`); `SeriesColumn.cs` (sin `ShiftScroll`); 04-design-notes §5.2 `:309` y la fila DD5-27 (`:695`, con la prueba de W1280); 02-tech-design V5-04 | Que el Designer decida: (a) DD5-27 vale solo para `RowsScroll` y §5.2 lo dice; o (b) la propuesta del Architect (en `ColumnScroll`, compensar solo cuando el lugar donde entra la fila ya está arriba del techo), con su prueba en SCT. Con (a) no cambian código ni tomas |

Observaciones sin issue: Unity reventó al cerrar 3 veces en 16 cambios de *define* y los *scripts* siguieron (QA-V4b-R2-03, cerrado); el trabajo de los paneles subió de 0,73 a 0,82 ms de mediana en el Editor, sin criterio en ms fuera del build.

### R2.9 NO VERIFICABLE (y por qué)

| Qué | Por qué | Quién lo cierra |
|---|---|---|
| AC-WKT-13-15 (`[PERF]` de la entrada en el build) y la ventana `[PERF]` de AC-WKT-18-21 | El `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator` los 20 minutos de espera (§R2.6); riesgo aceptado (QA-G5T-14) | QA o el Architect, cuando el usuario cierre su `RowMate.exe` |
| La prueba en equipo (mouse y teclado reales, reiniciar la app, `Simular desconexión`, la cuenta grande, el sello y `PAUSA`, matar el proceso) | Ídem | Ídem, o el usuario con el build de esa carpeta |
| AC-WKT-02-11 (la suspensión real) | Hay que suspender el equipo; QA no lo hace | El usuario (00-deuda-4b §1.5) |
| Las partes de Android (el dedo, el área táctil, el segundo plano de verdad de QA-V5-P01, el primer cuadro al volver) y el reemplazo atómico con IL2CPP (R5-04) | Faltan los módulos de Android e IL2CPP y el teléfono | Prerrequisitos del usuario (STATUS) |
