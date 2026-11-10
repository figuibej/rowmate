# It. 14 — Verificación de QA (rondas 1 y 2)

| Campo | Valor |
|---|---|
| Entrada | [01-po-spec](01-po-spec.md) (44 AC), [02-tech-design](02-tech-design.md) (con "Cambios por gate" y B-01 a B-07), [03-qa-gate](03-qa-gate.md), [04-design-notes](04-design-notes.md), [snapshots/](snapshots/) |
| Evidencia | `<scratch>/it14/qa-r1/` (resultados de dotnet y Unity, registros de build, capturas de ventana, `[PERF]`, *crash dumps*) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `git status --porcelain` vacío antes y después. No se tocó el módulo de credenciales de Strava |
| Veredicto ronda 2 | **APROBADO CON OBSERVACIONES**: los 7 mayores corregidos y verificados; quedan solo menores (ver [Ronda 2](#ronda-2)) |
| Veredicto ronda 1 | **RECHAZADO**: 7 mayores (3 defectos reales en el build de Windows y 4 AC sin cubrir) y 7 menores. Lo que sí está entregado está bien: ícono, versión del `.exe`, mínimo de ventana, F11, CI, catálogo y deuda de Dispositivo e Historial |

## 1. Corridas

| Corrida | Resultado | Frente a la It. 12 |
|---|---|---|
| `tools/dotnet/run-tests.ps1 -Matrix` | build sin advertencias; **4015: 3976 pasan, 39 ignoradas, 0 fallan**; herramientas 194/194 | 3886/3847/39 → sube |
| Unity EditMode | **4139: 4085 pasan, 54 ignoradas, 0 fallan** | 3999/3951 → sube (las 6 ignoradas nuevas son `[Device]`/`[Apple]` de AppIdentity y WorkoutDeviceChecks) |
| Unity EditMode `-Simulator` | **4333: 4279 pasan, 54 ignoradas, 0 fallan** | 4193/4145 → sube |
| Unity PlayMode | **682/682** | 660 → sube |
| Unity PlayMode `-Simulator` | **757/757** | 735 → sube |
| `build.ps1 -Target Windows` (normal) | 0; `BuildOutputInspector` limpio; `VersionInfo: RowMate 1.0.0 (iguisoft), archivo 1.0.0` | — |
| `build.ps1 -Target Windows -Simulator` | 0; misma `VersionInfo` | — |
| `build.ps1 -Target Windows -BootSmoke` | 0: mediana con la capa **1376 ms**, sin la capa **1374 ms** (Δ 2 ms ≤ 100); 10 salidas limpias, sin `Crashes` | — |
| `build.ps1 -Target Windows -BleSmoke` | 0: `[BLE] humo: estado=On busquedas=3 ftms=0 fallas=0 colgado=no soloFtms=si`, `[HRM] humo: busquedas=1 sensores=0 ajenos=0 fallas=0` | — |
| `native/android/check.ps1 -Golden` | 0: 10 fuentes Kotlin (1 de RowMateUi), firmas de RmBle, RmExport y RmHaptics iguales a los *golden*, `events-v1.bin` idéntico | — |
| Guardia de `ProjectSettings` | "sin cambios" en las 4 corridas de Unity y en los 4 builds; SHA-1 de `ProjectSettings.asset`, `HorizonFar.mat` y `HorizonNear.mat` iguales antes y después | — |

Ninguna cuenta bajó: **ningún test de las It. 1 a 12 se rompió**.

## 2. Pruebas en el build de Windows (desarrollo y simulador, `ROWMATE_DATA_DIR` temporal, `ROWMATE_BLE=off`)

El build normal no se abrió (no respeta `ROWMATE_DATA_DIR`); de él se revisaron las propiedades y el ícono del `.exe`. La PC de QA estaba con un solo monitor en **1024 × 768 a 96 DPI** (área de trabajo 1024 × 720).

| Prueba | Resultado |
|---|---|
| Propiedades de `RowMate.exe` (normal y simulador) | `ProductName` RowMate, `CompanyName` iguisoft, `FileVersion` y `ProductVersion` 1.0.0, `FileDescription` RowMate, `OriginalFilename` RowMate.exe, © iguisoft |
| Ícono del `.exe` | Extraído en 256, 48, 32 y 16: es el RowMate (remos cruzados sobre `#0D1B2A`), no el de Unity. La barra de título lo muestra |
| Título | `RowMate` |
| Arranque (12 capturas cada ≈ 150 ms, 3 corridas) | **Fallo**: durante **≈ 0,7 a 0,9 s** el área de contenido está **blanca** (`#FFFFFF` en el 94 al 100 %) antes del primer cuadro; después, `#0D1B2A` con la marca centrada (99 %) y el fundido al shell. Sin logo de Unity (QA-V14-03) |
| Ventana inicial sin `-screen-*` | **Fallo**: ventana en (−8, −31) a (1032, 728), contenido 1024 × 720 = el área de trabajo entera; la **barra de título queda fuera de la pantalla** (QA-V14-02) |
| Mínimo (arrastre real de la esquina) | contenido **640 × 360** exacto (marco 656 × 399); se vuelve a agrandar sin problemas |
| `F11` / `Alt+Enter` | `F11` → 1024 × 768 sin bordes (`WS_POPUP`); `F11` → vuelve al rectángulo exacto de antes. `Alt+Enter`: lo mismo, sin pitido |
| Cerrar con la X (`WM_CLOSE`) | **Fallo**: el proceso sale con **`0xC0000005`** y el *crash handler* deja `crash.dmp`, **6 de 6** (dev 5, simulador 1); la pila termina en una dirección del montón administrado después de "Input System … Shutdown" (QA-V14-01). `-BootSmoke` no lo ve porque sale con `Application.Quit` |
| Primer entrenamiento con la escena demorada (`-rowmate-scene-delay 20`, clics reales: Workout → `Free workout`) | `Preparing scene…` con el arco, centrado, y el HUD andando (reloj `00:01`, SPM, split, watts y kcal en vivo): AC-DEBT-09 |
| `[PERF]` de 120 s por escenario (simulador, Alto, W1280) | CPU del hilo principal p99 ≤ 4,3 ms en régimen, asignaciones ≤ 86 B por cuadro (igual que en la It. 8), GC 0, `kit X, sesión X` correctos. Pero **GPU ≈ 80 ms por cuadro, 129 cuadros en 10 s (≈ 13 fps) en los 4 escenarios y en la entrada**. En la It. 8, en esta misma PC, la GPU daba ≤ 7 ms. El monitor estaba en el modo de reserva de 1024 × 768 (sin la pantalla de 2560 × 1440 a 164 Hz), y la sospecha es la presentación estrangulada de una pantalla apagada: **no se puede medir acá** (QA-V14-14) |

Los *crash dumps* de QA se movieron de la carpeta temporal del sistema a `<scratch>/it14/qa-r1/crashes`. La primera corrida de ventana (sin `-logFile`) dejó, como cualquier player de Unity, su `Player.log` en la carpeta de registros del player; las bases y `prefs.json` del usuario no cambiaron (fechas anteriores a la corrida).

## 3. Capturas `it14`

Se miraron las 16 imágenes y el volcado. Están bien CAP-01 (el ícono se distingue a 16 px), CAP-02 (la figura no se corta en ninguna máscara), CAP-03/04 (`#0D1B2A` y la marca), CAP-05 (L640 sin desbordes), CAP-07 (tablero tabular), CAP-12 (fechas tabulares en `en`), CAP-15 (el nombre completo en 2 renglones y `Desconectar` entero), CAP-16 (indicador blanco y etiqueta legible), CAP-17 (lista y pulsómetro lado a lado, `Buscar` a la vista), CAP-18 (`Error al escanear: error 2`), CAP-19/20 (la ruta parte después de `\`, legible), CAP-22 (filas por paso en columnas fijas) y CAP-27 (25 nodos: nombres, roles y estado `Selected`).

Observaciones:

- **CAP-23 es byte a byte igual a CAP-27** (mismo MD5) y, como CAP-22, no muestra los ejes, el `0` ni los extremos redondos: las dos tomas no evidencian #32/#37 (lo cubren `ChartArtTests` y `HistoryScaleTests`).
- En CAP-19 el gráfico se ve a través del aviso (la línea amarilla y las etiquetas `100` y `50`): es el vidrio `glass.fallback` al 86 % que pide la propia spec. El contraste da bien (QA-V14-11).
- Faltan 11 tomas de la spec (CAP-06, -08 a -11, -13, -14, -21, -24 a -26) y VID-01/02 (B-07).
- `snapshots/INDEX.md` se titula "Capturas de la It. 9" y habla de `capture-cap27.ps1`.
- Iteraciones anteriores: `git status` no muestra ningún cambio en las carpetas de capturas de la 1 a la 12 (no se regeneraron, como dicen las notas de diseño §4). Las pantallas que cambiaron (pestañas, tarjetas y Detalle del Historial) se ven bien en CAP-05/07/12/22, y el tracking de 0,04 em de las pestañas no corta `Dispositivo` en TV a 1,3× (CAP-15).

## 4. Matriz AC → evidencia → resultado

PASS 31 · FAIL 7 · NO VERIFICABLE 6. Cuando un AC tiene parte [Editor] o [Device-Win] verificada y parte [Device-Android]/[Apple], se marca por la parte verificable y lo demás queda NV en AC-NV-01.

| AC | Evidencia | Resultado |
|---|---|---|
| APP-16-06 | `VersionInfoBlockTests`, `ItFourteenToolingTests.ExeVersionInfoChecked`, `ProjectSettingsIt14Tests`; propiedades del `.exe` normal y simulador; título `RowMate` | **PASS** (Android/Apple NV) |
| APP-16-07 | `ProjectSettingsIt14Tests.Identifiers` | **PASS** |
| APP-16-08 | 35 pruebas (`AppIconAssetsAreCurrent`, hashes, `AppIconSourceScan`, PNG/deflate), CAP-01, ícono extraído del `.exe` | **PASS** (Apple NV) |
| APP-16-09 | `AdaptiveForegroundInsideSafeZone`, `AndroidPlatformIcons`, CAP-02 | **PASS** (equipo NV) |
| APP-16-10 | `BootSplashTests`, `BootSplashMotionTests`, CAP-03/04, `-BootSmoke` Δ 2 ms; build: **≈ 0,8 s de cuadro blanco** | **FAIL** (QA-V14-03) |
| APP-16-11 | `ProjectSettingsIt14Tests.SplashAndLaunchScreen`, `IdentityStepTests.LaunchScreenAndSplash`, `ApplePostprocessTests` | **PASS** (configuración; equipo NV) |
| APP-16-12 | `ProjectSettingsIt14Tests.AndroidMinimum`, `AndroidManifestTests` | **PASS** (APK NV) |
| APP-15-05 | `WindowGeometryTests`, `WindowsWindowFrameTests`; build: barra de título fuera de la pantalla y crash al cerrar | **FAIL** (QA-V14-01, -02) |
| APP-15-06 | `MinTrackIsContentPlusFrame`, `MinMaxInfoCallsTheOriginalFirst…`, CAP-05; build: 640 × 360 exacto | **PASS** |
| APP-15-07 | `WindowModeStateTests`, `WindowModeTests`; build: `F11` y `Alt+Enter` ida y vuelta al rectángulo exacto | **PASS** |
| APP-15-08 | Un solo monitor; QA no cambia la escala | **NV** (demo del usuario) |
| APP-15-09 | `ProjectSettingsIt14Tests.Orientations`, `InfoPlistOrientations`, `Mac800x600` | **PASS** (configuración; equipo y Apple NV) |
| APP-15-10 | Solo `ScrollAnchorTests` (Rutinas) y la regla de 400 ms de `SceneMotionTests`; faltan `ResizeContinuityTests` y VID-02 | **FAIL** (QA-V14-06) |
| APP-11-01 | Clasificador, matcher y escaneo estático (dotnet, 11 pruebas); en vivo, solo las 14 tomas de `it14` (12 en `es`, 2 en `en`); faltan `TextAuditTourTests` y la sonda en `DemoWalkthroughTests` (condición de QA-G14-08) | **FAIL** (QA-V14-04) |
| APP-11-02 | No existe `LegacyFixedTextsTests` | **FAIL** (QA-V14-04) |
| APP-11-03 | `CatalogParityTests.FourItFourteenKeys`, `SameKeysInBothLanguages` (256) | **PASS** |
| APP-03-12 | `ScreenReaderTreeTests.DeviceScreenIsReadInVisualOrder`, CAP-27 (Detalle), `A11yOrder`/`Reconciler` | **PASS** (Narrador real: demo; TalkBack NV) |
| APP-03-13 | `InvokeIsTheClickAndFocusIsStable`; sin prueba del diálogo (recorrido encerrado y vuelta al que lo abrió) | **PASS** con observación (QA-V14-08, -12) |
| APP-03-14 | `TitleOnTabChangeAndEachToastOnce`, `A11yAnnouncePolicyTests`, `A11yTextTests` | **PASS** |
| APP-03-15 | `OffDoesNothing`; las pruebas de 0 B por cuadro de HUD y Dispositivo y `HudBudgetTests` siguen en verde; el *bridge* apagado solo lee un `bool` | **PASS** (60 fps con el lector: demo) |
| POL-01 | `TransitionInventoryTests` (3) | **PASS** (falta CAP-24) |
| POL-02 | `TypographyAuditTests` (3), CAP-12, CAP-22 | **PASS** |
| POL-03 | `WorkoutHapticsTests`, `HapticPolicyTests`, `AndroidHapticsWithoutPermissionsAndGolden`, `check.ps1` | **PASS** (equipo NV) |
| POL-04 | `CompletionDialogScaleTests` | **PASS** (falta CAP-13) |
| POL-05 | Las cuentas no bajan; **no existe `DemoWalkthroughTests`** | **FAIL** (QA-V14-05) |
| POL-06 | `[PERF]` de 120 s en los 4 escenarios: CPU y asignaciones como en la It. 8; la GPU no se puede medir con la pantalla en modo de reserva | **NV en esta ronda** (QA-V14-14) |
| POL-07 | Diff de §2.4 exacto, sin `ROWMATE_SIMULATOR`, `Horizon*.mat` sin cambios, guardia en verde | **PASS** |
| APP-19-06 | `CiUnityWorkflowTests` (jobs, artefactos, 14 días, solo el player o el `.apk`); revisión del YAML | **NV** (corrida real; local OK) |
| APP-19-07 | `GateSkipsWithWarning`; el `gate` solo lee `secrets.*` en `env` | **NV** (corrida real; local OK) |
| APP-19-08 | `AppleManualUnsigned`; `workflow_dispatch` + `inputs.apple`, `macos-14`, `CODE_SIGNING_ALLOWED=NO` | **NV** (corrida real; local OK) |
| APP-19-09 | `SecretsOnlyInEnvOrWith`, `NoMachinePathsNorCredentials`, `DebugSignedSuffix`, higiene; `git grep` limpio | **PASS** |
| APP-19-10 | `MiniYamlTests` + `WorkflowYamlTests` (corren en `ci-core` vía `run-tests.ps1`); todo `uses:` en `@v4`; `permissions: contents: read`; `timeout-minutes` en los 6 jobs; `unityVersion` 6000.3.27f1; sin tabulaciones | **PASS** |
| APP-19-11 | `ReadmeCiSection`; `tools/unity/README.md` "It. 14: CI" con los 7 secretos, los jobs, los artefactos y la primera corrida | **PASS** |
| NV-01 | Todavía no existe `REPORT.md` | **NV** (se revisa en el REPORT) |
| DEBT-01 | `LongRowerNameWrapsAndTheActionKeepsItsSize`, CAP-15 | **PASS** |
| DEBT-02 | `SearchingButtonWhiteSpinnerAndReadableLabel`, CAP-16 | **PASS** |
| DEBT-03 | CAP-17 y su comprobación ("sin el área vacía aparte") | **PASS** (sin prueba PlayMode, QA-V14-08) |
| DEBT-04 | `BleErrorTextTests`, `ScanErrorWithoutTheOperation`, CAP-18 | **PASS** |
| DEBT-05 | `PathBreakTests`, `ToastClockTests`, `PathBreaksAndPauseWithHoverOrFocus`, CAP-19/20 | **PASS** con observación (QA-V14-11) |
| DEBT-06 | `ColumnsShrinkWholeWithFourDpOfAir`, `TvChartShowsZeroAndTheXAxis` | **PASS** (falta CAP-21; la llave y la cinta en W1280 sin prueba propia) |
| DEBT-07 | `DropCrowded` en vista y Core, `NoAxisLabelTouchesAnother`; **`SplitRange.Widen` sin cablear** (WD-03) | **FAIL** (QA-V14-07) |
| DEBT-08 | `ChartArtTests.HistoryErrorHasTheEmptyIcon`, `ChartGeometryTests.RoundCapsAtBothEnds`/`BandsAndStrip`, tipografía | **PASS** (faltan CAP-25 y un CAP-23 que lo muestre) |
| DEBT-09 | `SceneLoadingSpecsTests`; build: `Preparing scene…` con el HUD andando | **PASS** (sin prueba PlayMode ni CAP-26; la entrada completa de la cámara no se observó) |
| DEBT-10 | `DeviceStoreReconnectHapticTests`, `HapticPolicyTests` | **PASS** (equipo NV) |

## 5. Issues

| ID | Sev. | Resp. | Problema | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V14-01 | **mayor** | architect | Cerrar la ventana con la X (`WM_CLOSE`, el mismo camino que Alt+F4) termina en **access violation** (`0xC0000005`) y deja `crash.dmp`: es el riesgo de QA-G14-04. En la It. 10a, `CloseMainWindow` salía con 0 | 6/6 corridas (dev y simulador); `Player.log`: "Input System … Shutdown" → `PlayerConnection::CleanupCrash`, pila en `0x0000020862447480` sin módulo (un *thunk* administrado); `<scratch>/it14/qa-r1/crashes` | Registrar en el log cuándo corre `Uninstall` y si restauró. Si el procedimiento actual no es el nuestro (otra subclase encima), hoy no se restaura y el *thunk* queda en la cadena: usar `SetWindowSubclass`/`RemoveWindowSubclass` de comctl32 (seguros con cadenas), o restaurar al recibir `WM_CLOSE`/`WM_DESTROY` en el propio manejador antes de delegar. Sumar a `build.ps1` un humo de cierre: 5 × `WM_CLOSE`, salida 0, sin `Crashes`, también en pantalla completa |
| QA-V14-02 | **mayor** | architect | La ventana inicial recorta el **contenido** al área de trabajo y después le suma el marco, así que la barra de título (y los bordes) quedan fuera de la pantalla cuando 1280 × 720 lógicos no entran con el marco. Pasa en 1366 × 768 y en 1920 × 1080 al 150 %, que son pantallas comunes | Monitor de 1024 × 768: ventana en (−8, −31)-(1032, 728); `WindowGeometry.Initial` (`Core/Common/WindowGeometry.cs:73-85`) + `PlaceContent` (`AdjustWindowRectExForDpi` después de recortar) | Recortar el rectángulo **exterior**: calcular el marco con el DPI, achicar el contenido para que contenido + marco entren en el área de trabajo y centrar el exterior. Probarlo en `WindowGeometryTests` con un área de 1024 × 720 y otra de 1366 × 728 |
| QA-V14-03 | **mayor** | architect | Antes del primer cuadro de Unity, el contenido de la ventana se ve **blanco durante ≈ 0,8 s**. AC-APP-16-10 y la spec §1 dicen "nunca un cuadro blanco"; §2.3 del diseño suponía que arrancaba en negro | 3/3 corridas; `<scratch>/it14/qa-r1/window*/boot-0*.png` (94-100 % `#FFFFFF`, después 99 % `#0D1B2A` con la marca) | Investigar con el editor instalado: (a) el *splash* de Unity encendido solo con el fondo `#0D1B2A` y la marca de RowMate, sin el logo de Unity; (b) la pintura del fondo de la clase de ventana o del primer *present* (D3D11 en lugar de D3D12, *flip model*). Si no hay forma dentro de Unity, el PO acota el AC a "desde el primer cuadro que dibuja Unity" y lo anota como desvío. Repetirlo con el monitor normal (ver QA-V14-14) |
| QA-V14-04 | **mayor** | architect | AC-APP-11-01/-02 sin cubrir: no existen `TextAuditTourTests` ni `LegacyFixedTextsTests`, y `TextAuditProbe` no corre en ningún recorrido PlayMode. La auditoría en vivo cubre solo 14 tomas (12 en `es` y 2 en `en`). B-07 ("la auditoría corre solo en `it14`") deja sin efecto la condición con la que el gate aceptó D-01 (QA-G14-08), sin pasar por el gate | `Tests/PlayMode/It14/` (2 archivos); `grep TextAuditTour` → solo comentarios; 02-tech-design §4 y §10 | Agregar `TextAuditTourTests` (es/en, con `EnvironmentPicker`, `SeriesPanelView`, `CenterCallouts`/`Próximo:` y los diálogos de inicio y fin) y `LegacyFixedTextsTests` (la tabla de F-APP-11) |
| QA-V14-05 | **mayor** | architect | AC-POL-05: no existe `DemoWalkthroughTests` (el recorrido de la demo, pasos 2 a 5, en es y en, con los 4 escenarios, el exportador falso y el registro sin errores) | `grep DemoWalkthrough` → solo un comentario en `TextAuditProbe.cs:12` | Escribirla (con `TextAuditProbe` en cada pantalla, D-01) |
| QA-V14-06 | **mayor** | architect | AC-APP-15-10: no existe `ResizeContinuityTests` (reloj de la sesión, texto y foco del editor, diálogo abierto, pestaña, primer cuadro en el *layout* nuevo, 200 ms `ease.enter`, reducir movimiento) ni VID-02. Solo están probados el ancla de Rutinas y la regla de 400 ms | `Tests/PlayMode/It14/It14DesignTests.cs:527-` | Escribir `ResizeContinuityTests` (TV ↔ TH y W1280 ↔ L640) |
| QA-V14-07 | **mayor** | architect | AC-DEBT-07: el Core tiene `SplitRange.Widen` (±5 s si el rango es < 10 s), pero el gráfico no lo usa. Con un split constante, el eje Y sigue como antes | `grep "Widen("` → solo `AxisCrowding.cs:59` y su test; 04-design-notes WD-03 | Cablearlo en `ChartSeries` (rango del split) y actualizar las pruebas de la 9 que fijan ese rango, con un caso de PlayMode de split constante |
| QA-V14-08 | menor | architect | Faltan pruebas planificadas cuyo comportamiento sí se verificó por otro lado: `ScreenReaderCost`, el diálogo de `ScreenReaderAction`, `MinWindowLayout`, `WindowMode` (PlayMode), `HapticEvents`, `DeviceLandscapeScroll` y `SceneLoading`. Además, `HudBudgetTests` no suma el caso de la escena demorada (QA-G14-03) | 02-tech-design §1.2 (B1) frente a `Tests/PlayMode/It14/`; `HudBudgetTests.cs` sin cambios | Agregarlas, o al menos `SceneLoading` con el tope del HUD y el diálogo del lector |
| QA-V14-09 | menor | architect | Faltan 11 tomas y VID-01/02 (B-07). CAP-23 es idéntica a CAP-27 y no muestra los ejes; CAP-22 tampoco muestra el `0` ni el eje X | MD5 `eab92848…` en los dos archivos | Tomar CAP-23 (y CAP-22) desplazadas al gráfico; el resto, a issue si no entra |
| QA-V14-10 | menor | architect | `snapshots/INDEX.md` dice "Capturas de la It. 9" y menciona `capture-cap27.ps1` | `snapshots/INDEX.md:1-3` | Corregir el encabezado en el generador del índice |
| QA-V14-11 | menor | po | AC-DEBT-05 se contradice: pide `glass.fallback` al 86 % "sin dejar ver el gráfico detrás", y al 86 % el gráfico se ve. El contraste sí cumple (≥ 4,5:1, 04-design-notes §3) | CAP-19 (la línea y `100`/`50` detrás del aviso) | Elegir entre el vidrio (y sacar "sin dejar ver") o un aviso opaco; ya está en "Para issues" del Designer |
| QA-V14-12 | menor | architect | `ScreenReaderBridge.IsDialogOpener(element)` no usa su parámetro (devuelve "no hay diálogo"). La vuelta del foco al que abrió el diálogo no tiene prueba | `Presentation/Accessibility/ScreenReaderBridge.cs:509-512` | Guardar el elemento invocado y probar la vuelta en `ScreenReaderActionTests` |
| QA-V14-13 | menor | architect | macOS: `Screen.SetResolution(800, 600)` va en píxeles, no en lógicos; en Retina, la ventana queda en 400 × 300 pt ([Apple], NV) | `Bootstrap/AppRoot.cs` (`SetUpWindow`, rama `UNITY_STANDALONE_OSX`) | Multiplicar por la escala del monitor (a la 10b si no hay forma de probarlo) |
| QA-V14-14 | menor | architect (QA) | AC-POL-06 no se pudo medir: con la pantalla en modo de reserva (1024 × 768), la GPU da ≈ 80 ms por cuadro en los 4 escenarios y en la entrada. Lo demás coincide con la It. 8: CPU p99 ≤ 4,3 ms, ≤ 86 B por cuadro, GC 0 | `<scratch>/it14/qa-r1/perf/perf-*.log` | Repetir los 4 `[PERF]` de 120 s con el monitor normal en la ronda 2. Si la GPU sigue lejos de los ≤ 7 ms de la It. 8, pasa a mayor |

## 6. Revisiones de reglas

| Tema | Resultado |
|---|---|
| `ProjectSettings` (AC-POL-07) | El diff trae solo lo de 02-tech-design §2.4: `allowFullscreenSwitch` 1→0, `uIRequiresFullScreen` 1→0, `iOSLaunchScreen*Type` 0→5 con el *storyboard*, `androidSplashScreen` → `splash-mark.png`, `m_BuildTargetIcons` (Standalone 1024 a 16, también 64) y `m_BuildTargetPlatformIcons` (Android adaptativo, redondo y *legacy*), y `RowMateBootstrap.json` v6→7. `scriptingDefineSymbols` vacíos. Los avisos LF→CRLF de git son de `core.autocrlf`, no del archivo |
| CI | `ci-unity.yml`: 6 jobs, todos con `timeout-minutes`; `secrets.*` solo en `env:`/`with:`; sin `pull_request`; artefactos solo de la carpeta del player o del `.apk`; caché de `Library` por plataforma, separada de la de tests; la matriz y los pasos del simulador, con su texto literal. `ci-core.yml` corre `run-tests.ps1`, que incluye `WorkflowYamlTests`. No se hizo *push* ni se lanzó ningún workflow |
| Repo público | `git grep` (versionados) y `grep -a` (sin versionar, incluidos PNG, `.asset` y binarios cambiados) de rutas de la PC, usuario, scratchpad y nombre del equipo: **0 apariciones**. Solo aparece el autor en `LICENSE`/`README.md`, como antes. Los PNG de `tools/icon` no llevan `tEXt`/`tIME` |
| UTF-8 | 170 archivos cambiados o nuevos: UTF-8 válido, sin doble codificación |
| Legacy | `git -C ../rowmate-legacy status --porcelain` vacío |
| Bluetooth | Solo `-BleSmoke` tal como está (búsquedas sin conectar); el resto de las corridas con `ROWMATE_BLE=off` o simulador |

## 7. Para issues

- PE `VS_VERSIONINFO` en el CI de Linux (ya está en 02-tech-design §13).
- Tracking de 0,04 em en el HUD, `SplitRange.Widen` (si no entra en la ronda 2), +8 dp en la entrada del HUD, ancla de desplazamiento en Historial, Detalle y Dispositivo (04-design-notes §7).
- Las tomas que falten después de la ronda 2 (CAP-06, -08 a -11, -14, -21, -24 a -26, VID-01/02).

---

## Ronda 2

Re-verificación de QA-V14-01 a -07 después de los arreglos de [02-tech-design § Cambios por verificación](02-tech-design.md#cambios-por-verificación). Evidencia en `<scratch>/it14/qa-r2/`. Legacy: `git status --porcelain` vacío antes y después.

**Veredicto: APROBADO CON OBSERVACIONES.** Los 7 mayores quedan cerrados con prueba y, los de Windows, también en el build. Quedan menores: los de la ronda 1 que siguen abiertos (QA-V14-08 a -14) y 3 nuevos (QA-V14-15 a -17).

### R2.1 Corridas

| Corrida | Resultado | Frente a la ronda 1 |
|---|---|---|
| `run-tests.ps1 -Matrix` | build con 0 advertencias; **4023: 3984 pasan, 39 ignoradas, 0 fallan**; herramientas 194/194; matriz: PASA 867, FALLA 0 | 4015/3976 → sube |
| Unity EditMode | **4152: 4098 pasan, 54 ignoradas, 0 fallan** | 4139/4085 → sube |
| Unity EditMode `-Simulator` | **4346: 4292 pasan, 54 ignoradas, 0 fallan** | 4333/4279 → sube |
| Unity PlayMode | **690/690** | 682 → sube |
| Unity PlayMode `-Simulator` | 1.ª corrida **767: 766 pasan, 1 falla** (`BleAllocationTests.TheReconnectBannerDoesNotAllocatePerFrame`, It. 11: "1 asignación en los pasos" en 120 cuadros, 2.º test de la corrida); el *fixture* solo, 3 veces: 4/4 cada vez; 2.ª corrida completa: **767/767** | 757 → sube; intermitente (QA-V14-15) |
| `build.ps1 -Target Windows` (normal) | 0; `BuildOutputInspector` limpio; `VersionInfo: RowMate 1.0.0 (iguisoft), archivo 1.0.0`; guardia "sin cambios" | igual |
| `build.ps1 -Target Windows -Simulator` | 0; misma `VersionInfo`; guardia "sin cambios" | igual |
| `build.ps1 -Target Windows -CloseSmoke -BootSmoke -BleSmoke` | 0. **Cierre: 7 de 7 con `WM_CLOSE` salen con `0x0`** (5 en ventana, 2 en pantalla completa), sin `Crashes`. Arranque: mediana con la capa 1374 ms, sin la capa 1389 ms (Δ −15 ms ≤ 100), 10 salidas limpias. BLE: `estado=On busquedas=3 ftms=0 fallas=0 colgado=no soloFtms=si`; HRM: `busquedas=1 sensores=0 ajenos=0 fallas=0` | cierre: antes 6 de 6 con `0xC0000005` |
| `native/android/check.ps1 -Golden` | 0 (Kotlin y manifiesto sin cambios desde la ronda 1; firmas y `events-v1.bin` iguales) | igual |
| Guardia propia | SHA-1 de `ProjectSettings/*.asset`, `RowMateBootstrap.json`, `Packages/manifest.json`, `HorizonFar.mat` y `HorizonNear.mat` iguales antes y después de todas las corridas y builds; `git status` con las mismas 227 entradas | — |

Ningún test de las It. 1 a 12 se rompió de forma reproducible; el único rojo es el intermitente de QA-V14-15.

### R2.2 Build de Windows (desarrollo, `ROWMATE_DATA_DIR` temporal, `ROWMATE_BLE=off`, sin `-screen-*`)

El mismo monitor de la ronda 1 (1024 × 768 a 96 DPI, área de trabajo 1024 × 720). 5 arranques con capturas del área de contenido desde que aparece la ventana.

| Prueba | Resultado |
|---|---|
| Ventana inicial (QA-V14-02) | 5 de 5: marco **(0, 0)-(1024, 720)** = el área de trabajo entera, contenido **1008 × 681 en (8, 31)**: la barra de título y los bordes adentro. Título `RowMate` |
| Arranque (QA-V14-03) | Cada ≈ 70 ms: **una sola toma blanca** (93 %) justo al aparecer la ventana (antes del primer código de la app), la siguiente ya `#0D1B2A` en el 93 % (un filo claro arriba) y desde ahí `#0D1B2A` al 100 % hasta la marca y el fundido. Antes: 0,7-0,9 s de blanco |
| Cierre con la X (`WM_CLOSE`, QA-V14-01) | 5 de 5 salen con `0x0` sin carpetas nuevas en `Crashes`; el registro dice `subclase de la ventana retirada: WndProc original restaurado`. Con `-CloseSmoke`: 12 de 12 en total |
| Mínimo, `F11`/`Alt+Enter` | Sin cambios en esa parte de `Handle` (el `WM_GETMINMAXINFO` y el `WM_SYSCHAR` van antes del `WM_CLOSE`); `AltEnterWithoutBeep` y las pruebas del mínimo en verde. No se repitió el arrastre a mano |
| `[PERF]` (QA-V14-14) | No se repitió: la pantalla sigue en el modo de reserva de 1024 × 768 |

Las capturas que mostraban el escritorio detrás de la ventana (antes de que se pinte) se borraron.

### R2.3 Revisión de los arreglos

| ID | Revisión | Estado |
|---|---|---|
| QA-V14-01 | `WindowsWindowFrame.Handle` (`Platform/Display/WindowsWindowFrame.cs:178-187`): con `WM_CLOSE` restaura el original y re-encola el mismo mensaje; si no era nuestro, delega y queda `NotOurs`; `WM_DESTROY`/`WM_NCDESTROY` restauran antes de delegar; `Uninstall` es idempotente. La app no usa `Application.wantsToQuit`, así que soltar la subclase al cerrar no deja una ventana viva sin el mínimo. 4 pruebas EditMode nuevas en verde, `-CloseSmoke` y 5 cierres de QA | **Cerrado** |
| QA-V14-02 | `WindowGeometry.Initial(workArea, escala, WindowFrameEdges, …)` (`Core/Common/WindowGeometry.cs:106-118`) recorta y centra el exterior. 4 pruebas dotnet nuevas (1024, 1366, 150 %, centrado) en verde; build: 1008 × 681 en (8, 31) | **Cerrado** |
| QA-V14-03 | `WindowsBootBackground` (`SubsystemRegistration`: pincel de clase `bg.app` y `FillRect`) y repintado en `AppRoot` después de ubicar la ventana. Queda ≈ 1 toma (≤ 0,1 s) antes de que corra código de la app: lo cubre la letra del AC ("desde el primer cuadro que dibuja la app"); el desvío DV-V14-03 lo tiene que asentar el PO (QA-V14-16) | **Cerrado** con observación |
| QA-V14-04 | `TextAuditProbe` en `Tests/UiSupport`; `TextAuditTourTests` (es y en, ≥ 50 paradas: los estados de Dispositivo, los 6 del pulsómetro, los 4 escenarios, el inicio de las 5 rutinas de F-4B, HUD con `Próximo`, pausa, terminar, completado, libre, series, Rutinas, editor, Historial y Detalle con exportar) y `LegacyFixedTextsTests` (2) en verde. El recorrido corre de verdad (≈ 17 s por idioma; las dos clases comparten la caché). La sonda es más permisiva que en la ronda 1 (separa por `·` y `,`), pero cada pedazo tiene que pasar el clasificador; no tiene una prueba negativa propia (QA-V14-17) | **Cerrado** |
| QA-V14-05 | `DemoWalkthroughTests.TheDemoWithoutErrors` (es y en, PlayModeSimulator): Dispositivo simulado, Escalera en el editor, Escalera 1 min y Terminar, libre en los 4 escenarios, Historial → Detalle → Split → Exportar TCX con el exportador falso, sonda en cada pantalla y registro sin errores. En verde en las dos corridas | **Cerrado** |
| QA-V14-06 | `ResizeContinuityTests` (3): TV → TH con el mismo inmersivo, reloj y fase, layout horizontal desde el primer cuadro, opacidad 0 → a mitad a 100 ms → 1 a 220 ms, regla de 400 ms y reducir movimiento; W1280 ↔ L640 con el editor, el texto, el foco, el diálogo y la pestaña. En verde. VID-02 sigue en "Para issues" | **Cerrado** |
| QA-V14-07 | `SessionChartData` (`Core/History/ChartSeries.cs:130-163`): con rango < 10 s usa `SplitRange.Widen` como referencia del eje y mete el rango abierto entero; curva, etiquetas y objetivos la usan; `SlowestPace` sigue siendo el real. 3 pruebas dotnet y `SplitRangeChartTests.ConstantSplitOpensTheAxis` (piso `2:18`, `YMax` 11) en verde; las de la It. 9 siguen en verde | **Cerrado** |

### R2.4 Capturas

Las 14 tomas de `it14` se regeneraron con los arreglos. Se miraron CAP-03 (fondo `#0D1B2A` y marca centrada), CAP-15 (nombre largo en 2 renglones, `Desconectar` entero) y CAP-22 (filas por paso en columnas fijas, gráfico con franjas y objetivos): sin cambios frente a la ronda 1. Siguen QA-V14-09 (CAP-23 es byte a byte CAP-27, MD5 `eab92848…`) y QA-V14-10 (`INDEX.md` dice "Capturas de la It. 9"). Iteraciones anteriores: `git status` sin cambios en sus carpetas de capturas; muestreo de 09-historial CAP-01: igual que al cerrarla.

### R2.5 Matriz AC (cambios frente a la ronda 1)

**PASS 38 · FAIL 0 · NO VERIFICABLE 6** (APP-15-08, POL-06, APP-19-06, -07, -08 y NV-01, por los mismos motivos de la ronda 1).

| AC | Evidencia de la ronda 2 | Resultado |
|---|---|---|
| APP-16-10 | Build: ≤ 1 toma blanca (≈ 70 ms) antes del primer código de la app, después `#0D1B2A` sin cortes; `BootBackgroundIsBgAppAsColorRef`; `-BootSmoke` Δ −15 ms | **PASS** con observación (QA-V14-16) |
| APP-15-05 | `WindowGeometryTests` (4 nuevas); build: barra de título adentro, 5 de 5; cierre 12 de 12 con 0 | **PASS** |
| APP-15-10 | `ResizeContinuityTests` (3) | **PASS** (VID-02 y equipo NV) |
| APP-11-01 | `TextAuditTourTests` (es, en), `DemoWalkthroughTests` (es, en), tomas `it14` | **PASS** |
| APP-11-02 | `LegacyFixedTextsTests` (2) | **PASS** |
| POL-05 | `DemoWalkthroughTests` (es, en); las cuentas suben en las 5 corridas | **PASS** con observación (QA-V14-15) |
| DEBT-07 | `ChartSeriesTests` (3 nuevas), `SplitRangeChartTests` | **PASS** (falta una CAP-21/23 que lo muestre) |

El resto queda como en la [matriz de la ronda 1](#4-matriz-ac--evidencia--resultado).

### R2.6 Issues

| ID | Sev. | Resp. | Problema | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V14-01 a -07 | — | architect | Cerrados (R2.3) | R2.1 a R2.3 | — |
| QA-V14-15 | menor | architect | `BleAllocationTests.TheReconnectBannerDoesNotAllocatePerFrame` (It. 11) es intermitente en la corrida completa de PlayMode con el simulador: 1 asignación en los pasos en 120 cuadros. Le pasó al architect en esta ronda y a QA en 1 de 2 corridas completas; solo, 3 de 3 en verde. En el CI (`ci-unity`, PlayMode + simulador) pondría el job en rojo sin motivo cuando se cargue `UNITY_LICENSE` | `<scratch>/it14/qa-r2/unity-PlayMode-sim/playmode-results.xml`; `Tests/PlayMode/BleAllocationTests.cs:30-48` (un solo cuadro de calentamiento) | Encontrar qué asigna una vez en el primer *fixture* del dominio (marcadores de `FrameAllocations`) o calentar 2-3 cuadros antes de medir; arreglarlo antes de cargar el secreto de Unity |
| QA-V14-16 | menor | po | AC-APP-16-10: queda ≈ 1 toma blanca (≤ 0,1 s) al aparecer la ventana, antes de cualquier código de la app. Se aprueba por "desde el primer cuadro que dibuja la app", pero el "nunca un cuadro blanco" de la misma línea lo contradice | R2.2 | Asentar DV-V14-03 en 01-po-spec (o pedir el *launcher* nativo como issue) |
| QA-V14-17 | menor | architect | `TextAuditProbe.IsAllowed` acepta textos armados separando por `·` y `,`, y no hay una prueba que muestre que la sonda falla con un texto fuera del catálogo: si se volviera permisiva de más, las pruebas de textos seguirían en verde | `Tests/UiSupport/TextAuditProbe.cs:58-90` | Una prueba negativa: un `Label` con un texto fijo inventado (y otro `"Hola, mundo"`) en una pantalla viva tiene que dar una violación |
| QA-V14-08 a -14 | menor | — | Siguen como en la ronda 1 (QA-V14-14: el `[PERF]` sigue sin poder medirse con esta pantalla) | §5 | A issues |

### R2.7 Reglas

| Tema | Resultado |
|---|---|
| `ProjectSettings` | Diff igual al de la ronda 1 (solo lo de 02-tech-design §2.4, más `com.unity.modules.accessibility` en `manifest.json`); `scriptingDefineSymbols` vacíos (sin `ROWMATE_SIMULATOR`); `Horizon*.mat` sin cambios; guardia "sin cambios" en los 3 builds |
| CI | `WorkflowYamlTests`/`MiniYamlTests` en verde dentro de `run-tests.ps1`; todo `uses:` con versión fija (`actions/*@v4`, `game-ci/*@v4`); `secrets.*` solo en `env:`/`with:`; `tools/unity/README.md` documenta los 7 secretos y `-CloseSmoke`. Sin *push* ni workflows lanzados |
| Repo público | `git grep` (versionados) y `grep -a` (sin versionar y modificados, incluidos PNG y binarios) de rutas de la PC, usuario, nombre del equipo y scratchpad: **0 apariciones** |
| UTF-8 | 173 archivos de texto cambiados o nuevos: UTF-8 válido, sin doble codificación |
| Bluetooth | Solo `-BleSmoke` tal como está; todo lo demás con `ROWMATE_BLE=off` o simulador |

### R2.8 Para issues

- QA-V14-15 a -17 (arriba) y los menores abiertos de la ronda 1 (QA-V14-08 a -14).
- Las tomas que faltan (CAP-06, -08 a -11, -13, -14, -21, -24 a -26; una CAP-23 propia) y VID-01/02.
- `[PERF]` de 120 s con la pantalla normal (QA-V14-14) y el humo de ventana en 1366 × 768 y al 150 % en un equipo real.
