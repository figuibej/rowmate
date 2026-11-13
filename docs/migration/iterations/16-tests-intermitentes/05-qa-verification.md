# Ronda 16 — Tests intermitentes de asignaciones (verificación de QA)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-13 |
| Verifica | [02-tech-design.md](02-tech-design.md) (issues #78 y #99) |
| Veredicto | **APROBADO CON OBSERVACIONES** |

Artefactos en `<scratch>/it16qa/` (una carpeta por corrida, con `load.txt`). Las 6 corridas de PlayMode son independientes de las del Architect.

## 1. La causa es real

- **Código.** El diff toca solo `Tests/UiSupport/GcAllocations.cs`, `Tests/UiSupport/FrameAllocations.cs` y `Tests/PlayMode/FrameAllocationsTests.cs`. La app no cambia. Antes, cada tramo contaba con `UnityEngine.Profiling.Recorder` + `FilterToCurrentThread`. Ahora cuenta con `ProfilerRecorder` del marcador `GC.Alloc` con `CollectOnlyOnCurrentThread` (`GcAllocCounter`). Ese contador se crea en el hilo que mide y lo comprueba en `Begin`, sin `SumAllSamplesInFrame`, así que cada asignación es una muestra. Los cortes y reanudaciones del tramo (panel ↔ dibujo, escena ↔ URP) siguen como antes. El paso y el reloj tienen su propio contador, así que un dibujo dentro del paso ahora **suma** en el paso en vez de cortarlo: la medida queda igual de estricta o más.
- **Evidencia histórica** (`<scratch>`, 10 XML con `BleAllocationTests` en rojo, de las It. 6b a 15). Siempre son 1 o 2 asignaciones, en los dibujos, el paso o el reloj, y van rotando entre las 5 medidas (`TheSonar…`, `TheReconnectBanner…`, `HrmConnected…`, `TheHudLinkNotice…`). Es la firma de una muestra ajena, no de un camino de la app que asigne.
- **Evidencia en esta verificación (el testigo).** En `pms1` y `pms3`, el `Recorder` de antes se habría llevado **2 y 3** muestras en el sonar (`[BAT] sonar con 3 remos…: ajenas que el Recorder de antes habría contado: 2` / `3`). En esas mismas ventanas, el contador nuevo dio **0**. Con el arnés viejo, esas 2 de 6 corridas habrían salido rojas en `TheSonarDoesNotAllocatePerFrame`, igual que en la It. 12 (`sonar … 1 en los pasos`) y en la 6b. En la sonda de `OtherThreadsAllocatingAreNotCounted`, el `Recorder` de antes contó entre **85 y 150** muestras ajenas en 1000 medidas vacías. El nuevo contó **0** en las 6 corridas.
- **Sin trampas.** `git diff -- Assets/RowMate/Tests` no muestra cambios de topes ni de `Is.EqualTo(0)` / `LessThanOrEqualTo`. Tampoco hay `Retry`, `Ignore`, `Explicit` ni exclusiones de la CI. `BleAllocationTests.cs` no cambió, y `UiTestBase.Settle(maxSeconds)` sigue como lo dejó la It. 15.

## 2. Corridas

"Carga" es la de los otros procesos según `-LogLoad`: antes / media / máximo. No se tocó ningún proceso ajeno. Pantalla en el modo normal del monitor. Esta ronda no mide la GPU.

| Corrida | Resultado | Carga | `BleAllocationTests` | `FrameAllocationsTests` / `ChartAllocationTests` | Sonda FAT: nuevo / Recorder viejo | Testigo en `[BAT]` |
|---|---|---|---|---|---|---|
| PlayMode 1 | **797/797** | 0 / 7 / 22 % | 4/4 | 2/2 · 1/1 | 0 / 116 | 0 |
| PlayMode `-Simulator` 1 | **878/878** | 4 / 6,3 / 22 % | 4/4 | 2/2 · 1/1 | 0 / 150 | **2** (sonar) |
| PlayMode 2 | **797/797** | 5 / 5,9 / 19 % | 4/4 | 2/2 · 1/1 | 0 / 112 | 0 |
| PlayMode `-Simulator` 2 | **878/878** | 11 / 6 / 28 % | 4/4 | 2/2 · 1/1 | 0 / 85 | 0 |
| PlayMode 3 | **797/797** | 3 / 6,3 / 30 % | 4/4 | 2/2 · 1/1 | 0 / 105 | 0 |
| PlayMode `-Simulator` 3 | **878/878** | 0 / 5,7 / 32 % | 4/4 | 2/2 · 1/1 | 0 / 121 | **3** (sonar) |
| EditMode | 4325: 4269 pasan, **1 falla**, 55 ignorados | 16 / 4,2 / 8 % | — | — | — | — |
| EditMode `-Simulator` | 4519: 4463 pasan, **1 falla**, 55 ignorados | 3 / 5,2 / 12 % | — | — | — | — |
| dotnet `-Matrix` | 194/194 y 4182: **4143 pasan, 0 fallan**, 39 omitidos. Matriz: PASA 919, FALLA 0, SIN TEST 0 | — | — | — | — | — |

- **Asignaciones:** todos los tests que miden asignaciones pasaron en las 6 corridas completas: `BleAllocationTests`, `FrameAllocationsTests`, `ChartAllocationTests`, los de `HudBudgetTests`, `SeriesMotionTests`, `HudMotionTests`, `CompletionDialogTests`, `HistoryPerformanceTests` e `ItFourteenGapTests`. Si se suman las 6 del Architect, `BleAllocationTests` lleva **48/48** en 12 corridas completas.
- `SceneBudgetTests(High)` y `HudBudgetTests.SeriesWorstCaseWork` también pasaron en las 6, porque la carga externa fue baja. Están fuera de alcance y no se tocaron.
- **Las 2 fallas de EditMode son del entorno**, las mismas que vio el Architect:
  - `It14PlatformTests.ApplyIsIdempotent`: "Expected string length 26821 but was 27842". Lo esperado es el `ProjectSettings.asset` del checkout (CRLF). El resultado es el que reescribe Unity (LF, con 97 líneas de íconos de iPhone).
  - `ProjectSettingsIt14Tests.AndroidPlatformIcons`: "Expected: 6 But was: 10". Lee el archivo mientras el paso `EnableForTests` ya le agregó los íconos de iPhone.
  - Las dos leen `ProjectSettings` y esta ronda no las toca. Con el archivo como en el commit, `AndroidPlatformIcons` pasa en dotnet.
  - La causa es el módulo iOS Build Support, que esta PC tiene instalado desde hoy. La CI no lo tiene.
- **Deriva de `ProjectSettings.asset`:** cada apertura de Unity agregó las mismas 97 líneas: solo `m_BuildTarget: iPhone` con íconos vacíos, comprobado línea por línea. Por eso la guardia salió con 13 en cada corrida. Se revirtió con `git checkout` después de cada una.

## 3. Control positivo

- `OtherThreadsAllocatingAreNotCounted`, en las 6 corridas:
  - un `byte[64]` propio por medida: **300 de 300**, exacto;
  - `mesh.vertices`, que asigna desde una API de Unity: **300 de 300**;
  - cuadros reales: 60 sin asignar dan **0**, y 60 con un `byte[64]` por dibujo y otro por paso dan **uno por dibujo y 60 en los pasos**, exacto.
- `ThePositiveControlIsSeenInRealFrames` (It. 5), `SeriesMotionTests` (≥ 30) y el control de `HudBudgetTests` pasan en las 6 corridas.
- El contador nuevo **sigue viendo asignaciones reales** y deja de ver las de otros hilos.

## 4. Repo, legacy y settings

- **Repo público:** en los 3 `.cs` y en `02-tech-design.md` no hay rutas de la PC, usuarios ni datos del hardware. La pantalla figura solo como "modo normal". Los archivos están en UTF-8, sin doble codificación (no aparecen `Ã` ni `Â`).
- **Legacy:** `git -C ../rowmate-legacy status --porcelain` da vacío.
- **Settings:** al final, `git status` muestra solo los 3 `.cs` y la carpeta de la ronda. `ProjectSettings/`, `Packages/` y `Assets/RowMate/Settings` están como en el commit, y `HorizonFar.mat`/`HorizonNear.mat` no cambiaron. `ROWMATE_SIMULATOR` no aparece en `ProjectSettings.asset` y no queda la marca `Library/RowMate/simulator-tests.txt`. No queda ningún Editor abierto.

## 5. Matriz

| Criterio | Evidencia | Resultado |
|---|---|---|
| R16-01 La causa es una muestra de otro hilo, no la app | §1: código, historial y testigo 2 y 3 en el sonar con 0 en el contador nuevo | PASS |
| R16-02 No se relajó, reintentó ni ignoró nada | `git diff` de `Tests/` | PASS |
| R16-03 `BleAllocationTests` en todas las corridas completas | 6/6 (24/24 casos) | PASS |
| R16-04 Los demás tests de asignaciones en todas | 6/6 | PASS |
| R16-05 El control positivo detecta asignaciones reales | §3 | PASS |
| R16-06 dotnet `-Matrix` | 0 fallas, FALLA 0 en la matriz | PASS |
| R16-07 EditMode con y sin `-Simulator` en verde | 1 falla en cada uno, por el módulo iOS de esta PC (§2) | NO VERIFICABLE (entorno) |
| R16-08 La app no cambia | el diff está solo en `Tests/` | PASS |
| R16-09 Repo público, legacy y settings | §4 | PASS |

## 6. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V16-01 | menor | architect | Los `GcAllocCounter` del pool `[ThreadStatic]` de `GcAllocations.During` nunca hacen `Dispose`: su `ProfilerRecorder` nativo queda vivo hasta que se cierra el Editor, aunque se recargue el dominio | `GcAllocations.cs:19-41` | Liberarlos en `AppDomain.DomainUnload` / `AssemblyReloadEvents`, o crear y liberar uno por medida, fuera del tramo medido |
| QA-V16-02 | menor | architect | El `Recorder` testigo se comparte entre los tramos de dibujo y `MeasureAction`. Si hay un dibujo dentro del paso, el testigo se reinicia y `Foreign` queda aproximado. Es solo un diagnóstico: no entra en ninguna afirmación | `FrameAllocations.cs:242-290` | Documentarlo en el comentario de `Foreign`, o usar dos testigos |
| QA-V16-03 | menor | architect | En `OtherThreadsAllocatingAreNotCounted`, `quiet` y `drawing` se liberan solo si pasan las afirmaciones. Es el mismo patrón que en `ThePositiveControlIsSeenInRealFrames` | `FrameAllocationsTests.cs:160-170` | Pasar los `Dispose` al `finally` |
| QA-V16-04 | menor | architect | Con el módulo iOS Build Support y el `ProjectSettings.asset` restaurado por git (CRLF), `ApplyIsIdempotent` y `AndroidPlatformIcons` fallan en esta PC. Ya figura en "Para issues" del 02; se suma el detalle del CRLF contra LF | §2 | Decidir si se versionan los íconos de iPhone o si la prueba se limita a Android. La decisión es del usuario |

## Para issues

- QA-V16-01 a 04.
- Lo de "Para issues" del 02: pasar `LocalOffsetSourceTests` a `GcAllocCounter` y anotar la limitación de `AllocationProfile`.
