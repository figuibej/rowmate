# It. 2 — Verificación de QA (después de implementar)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-27 (19:24-19:45) |
| Iteración | 2 · `02-dispositivo-simulador` |
| Entrada | [01-po-spec](01-po-spec.md) (125 AC, 15 LT, 25 CAP + 3 VID), [02-tech-design](02-tech-design.md), [03-qa-harmony](03-qa-harmony.md), [04-design-notes](04-design-notes.md) (incluida §15, WP-D), código en `Assets/RowMate/`, legacy `rowmate-legacy` (solo lectura, `lib/` y `test/`; no se leyó `strava_secrets.dart`) |
| Herramientas | .NET 8 (`run-tests.ps1`), Unity **6000.3.27f1** (Windows Standalone Mono; **sin** módulos Windows IL2CPP ni Android), arnés `capture-snapshots.ps1`, builds con `tools/build/build.ps1`, `capture-window.ps1` y `RowMate.exe -rowmate-color-probe` |
| Legacy | `git -C ../rowmate-legacy status --porcelain` **vacío** al empezar y al terminar |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.**

- Todo lo automatizable corre y da verde: **dotnet 1264 pruebas (1247 pasan, 0 fallan, 17 ignoradas)**; **Unity EditMode 1132 / 1116 / 0 / 16** y **con simulador 1281 / 1265 / 0 / 16**; **PlayMode 53 / 53** y **con simulador 85 / 85**. El proyecto abre sin errores (código 0) y el *bootstrapper* no repite pasos.
- El arnés genera **41 tomas (25 capturas con sus tomas, 2 AUX y 3 videos), 0 con problemas, 0 desbordes**, y la corrida de QA dio **los mismos bytes** que la anterior en los 43 archivos (solo cambian las duraciones de `manifest.json`): las capturas son deterministas. Miré las 38 PNG una por una.
- R-T1 medido en las dos tuberías (CAP-23) y **repetido por QA en un build de Windows recién compilado** (S-B1): `#2A2F37` / `#BAC1C8`, primer cuadro `bg.app`, latencia 0, clic correcto.
- Build normal de Windows recién compilado: sin código, ensamblados ni textos del simulador (lo dice `BuildOutputInspector` y lo comprobé aparte en UTF-8 y UTF-16), ventana `RowMate` de 1280 × 720, datos en `LocalLow\iguisoft\RowMate`, preferencias del legacy intactas.
- **AC: 118 PASS, 1 FAIL (menor), 6 NO VERIFICABLE.** De los 118, 13 pasan en lo verificable y tienen una parte de equipo (Android, IL2CPP, escala 150 %) que queda NV. **15 de 15 casos legacy portados 1 a 1 y en verde.**
- 15 issues, **ninguno bloqueante ni mayor**. El que hace fallar un AC (QA-V2-01: `Rower` + `Conectado` visibles 120 ms al desconectar) se arregla con una línea y conviene cerrarlo antes del REPORT. QA-V2-02 (metadatos de tests dentro del build normal) y QA-V2-14 (`ci-unity.yml` inválido) son los que más importan para builds y CI.

---

## 1. Qué se corrió (evidencia)

Carpetas propias: `…/scratchpad/it2-qa-r1` (dotnet), `…/it2-qa-r1m` (dotnet + matriz con Unity), `…/it2-qa-unity` (Unity), `…/it2-qa-build` (builds). Una sola instancia de Unity por vez, en serie. No había otro Editor abierto: `Temp/UnityLockfile` no existía y los `unity.exe` vivos eran ayudantes de la Hub (`serve` y `--internal-auth-broker-serve`), que no toqué.

### 1.1 dotnet (`run-tests.ps1 -Matrix -ArtifactsPath …/it2-qa-r1`)

```
Build succeeded.    0 Warning(s)    0 Error(s)
RowMate.Tests.Simulator:  Total tests: 149   Passed: 149
RowMate.Tests.EditMode:   Total tests: 1115  Passed: 1098  Skipped: 17
[run-tests] matriz: … (PASA 254, FALLA 0, IGNORADO 1, PENDIENTE UNITY 31, PENDIENTE WP-B 0, SIN TEST 0)
exit=0
```

Con los XML de Unity (`-UnityResults …/it2-qa-unity`): **PASA 285, FALLA 0, IGNORADO 1 (AC-APP-09-07, It. 3), PENDIENTE UNITY 0, SIN TEST 0.** La matriz automática cuenta como PASA un AC con al menos un test verde. Por eso las partes de equipo las decido a mano en §3.

### 1.2 Unity 6000.3.27f1

| Paso | Comando | Resultado |
|---|---|---|
| Abrir (AC-BASE-10) | `Unity.exe -batchmode -quit -projectPath … -logFile unity-open.log` | **exit 0** en 22 s; sin `error CS`, `Scripts have compiler errors` ni `Compilation failed`; `[BOOTSTRAP] versión 3 ya aplicada (ProjectSettings/RowMateBootstrap.json): sin cambios.` `ProjectVersion.txt` = `6000.3.27f1` |
| EditMode | `run-unity-tests.ps1 -Platform EditMode` | **total 1132, pasan 1116, fallan 0, ignorados 16**, exit 0 |
| EditMode + simulador (QA-V-05) | `run-unity-tests.ps1 -Platform EditMode -Simulator` | **total 1281, pasan 1265, fallan 0, ignorados 16, inconclusos 0**; Enable → 0, Restore → 0 |
| PlayMode | `run-unity-tests.ps1 -Platform PlayMode` | **53 / 53 pasan** |
| PlayMode + simulador | `run-unity-tests.ps1 -Platform PlayMode -Simulator` | **85 / 85 pasan** (las 53 + 32 del simulador, con LT-26 a LT-31) |
| Arnés | `capture-snapshots.ps1` (Enable → `SnapshotCli.Run` sin `-quit` → Restore) | **exit 0**, `listo: 41 tomas, 0 con problemas`; `R-T1: app #2A2F37 / #BBC1C8 · directa #6A6A6C / #DADBDC` |

**El define, siempre restaurado.** SHA-1 de `ProjectSettings/ProjectSettings.asset` antes de empezar y después de las 3 corridas con simulador: `0922f776…` en los dos casos. `ROWMATE_SIMULATOR` no queda en el archivo y `Library/RowMate/` (la marca) no existe.

**Conteos dotnet ↔ Unity (AC-BASE-08).** 1281 (Unity con simulador) − 1264 (dotnet) = +17: suman los 21 de `RowMate.Tests.UnityEditMode` y restan los 4 que solo existen en `net8.0`. Ignorados: 17 en dotnet y 16 en Unity, porque `ManifestAndVersionTests.ProjectOpensWithoutCompileErrors` solo corre dentro de Unity. Los 16 ignorados de Unity son los NV de equipo, CI o iteraciones futuras (lista en §6.3).

### 1.3 Builds de Windows (recién compilados por QA, salida en el scratchpad)

| Build | Resultado |
|---|---|
| Normal, Mono | `Build Finished, Result: Success.` · `[BUILD] BuildOutputInspector: sin rastros del simulador.` En `Managed/` solo hay `RowMate.{App,Bootstrap,Core,Infrastructure,Platform,Presentation,Scene}.dll` |
| Simulador, Mono | `[BUILD] Windows simulator Mono2x Development …` (BuildCli fuerza Development, AC-BLE-13-01) · Success |
| Normal, IL2CPP (AC-BASE-01) | **NO VERIFICABLE**: `Error building Player: Currently selected scripting backend (IL2CPP) is not installed.` El script sale con 1 en vez de 3 (QA-V2-03) |
| Android | **NO VERIFICABLE**: `[BUILD] NO VERIFICABLE: falta el módulo de Android en este Editor.`, exit 3 |

**Búsqueda propia de rastros** (todos los archivos del build, en UTF-8 y UTF-16: `Remo simulado`, `Pulsómetro simulado`, `SIMULADOR`, `Hora escena`, `Simular desconexión`, `RowMate.Simulator`, `SimulatorPanelStore`, `ControllableRower`, `FakeScheduler`, `ScriptedHeartRate`): **0 en el build normal**. Como control, el build de simulador sí los tiene (`RowMate.Simulator.dll` y `RowMate.Simulator.Presentation.dll`). El normal incluye los estilos del simulador (`rm-sim-…` en `sharedassets0.assets`): son solo selectores de USS, sin texto ni código, y el tema lo documenta. Además, el build normal incluye `PerformanceTestRunInfo` en `resources.assets` (QA-V2-02).

### 1.4 En la PC (Device-Win, Mono, escala de pantalla 96 dpi = 100 %)

| Qué | Evidencia |
|---|---|
| Build normal abierto (`capture-window.ps1`, ventana 1280 × 720) | Título de ventana **`RowMate`** (1296 × 759 con el marco). Dispositivo en la vista de búsqueda con el estado vacío; tarjeta `HEART RATE SENSOR` en `No sensor`; **sin cinta ni llave**; sin la marca "Development Build". Captura: `…/scratchpad/CAP-22-ventana-qa.png` |
| Idioma del sistema | `GetUserPreferredUILanguages` = `en-US, es-ES`: la app abre en **inglés**, como pide la regla (primer idioma soportado de la lista) |
| Registro | `Player.log` en `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`, con la línea propia **`[APP] RowMate 1.0.0 (1) · WindowsPlayer · simulador=no`** (P-06: el `.exe` trae la versión de Unity, `6000.3.27.6761814`, y no la de la app). Registro de Windows: `HKCU\Software\iguisoft\RowMate`, solo con las claves `Screenmanager …` del motor; nada de la app |
| Convivencia | `AppData\Roaming\com.example\rower_app\shared_preferences.json`: el mismo SHA-1 (`2ded10d3…`) y la misma fecha (2026-10-02) antes y después de abrir los dos builds |
| Build de simulador abierto | `Remo simulado` + `Connected`, con valores con ruido (`24.5`, `145`), cinta, llave y la marca **"Development Build"** del motor abajo a la derecha (P-01: se acepta solo en builds de desarrollo con simulador) |
| S-B1 repetido por QA (`RowMate.exe -rowmate-color-probe`) | `glassOnWhite #2A2F37` (esperado `#2A2F37`), `textSecondaryOnCard #BAC1C8` (legacy `#BAC0C7`, ± 1), `firstFrame notBgApp 0 / 1 024 000`, `latency 12 de 12`, `click pickedFromScreen probe-button`, `presentCostMs 0.001`, Direct3D 11 |

### 1.5 Limpieza

El build IL2CPP que falló (QA-V2-03) dejó `Assets/Resources/PerformanceTestRun{Info,Settings}.json` con sus `.meta` y `Assets/Resources.meta`, creados por el paquete `com.unity.test-framework.performance`, una dependencia de `com.unity.collections`. Ese paquete solo borra esos archivos si el build termina. Los creó mi corrida, así que los borré para dejar el repo como estaba. Hay una copia en `…/scratchpad/leftover-Resources`. Ver QA-V2-02. Fuera de eso, solo cambiaron `snapshots/manifest.json` (las duraciones) y los activos que Unity vuelve a serializar al compilar (QA-V2-07). No hice commits.

---

## 2. Capturas: revisión de las 38 PNG y los 3 videos

Todas a la densidad del perfil (TV 1080 × 2400 a 2,625 = 411,43 × 914,29 dp; L640, P800 y W1280 a 1). Fuentes e íconos definitivos (Inter, Barlow Semi Condensed, glifos propios); ningún ícono se ve como un cuadrado vacío. `LayoutAudit` dio 0 desbordes y 0 textos cortados en las 41 tomas. Todas las comprobaciones del catálogo pasaron. Contra `docs/image-01.png` del legacy (1600 × 1080, 3 columnas): misma estructura (barra con `Device` y `Disconnect`, cabecera centrada con insignia, grilla, tarjeta del pulsómetro a todo el ancho con `Disconnect` a la derecha). La cinta tapa la primera letra del título igual que en el legacy (`evice` en `image-01.png`).

| Captura | Qué tiene que verse (spec §7.3) | Resultado |
|---|---|---|
| CAP-01 | 2 columnas, `02:13` `24.0` `150` `226m`, sin PULSO, `PULSÓMETRO` en `Sin sensor`, cinta, llave, barra con Dispositivo elegido | **OK.** Sondas: fondo, tarjeta y barra superior `#0D1B2A` / `#1A2E45` / `#0D1B2A` (diferencia 0); trazos `#FF9F1B` `#00B4D7` `#FFD50A` `#4BC9EF` (± 1); cinta `#FF5722` |
| CAP-02 | Todo en inglés salvo `Remo simulado` y `SIMULADOR` | **OK.** `Device`, `Disconnect`, `Connected`, `WATTS`, `DISTANCE`, `HEART RATE SENSOR`, `No sensor`, `Search`, barra en inglés |
| CAP-03 | L640, 2 columnas a 1,3, tarjeta del pulsómetro fuera de la vista | **OK.** La llave queda sobre la tarjeta SPM, por encima del contenido, como el `Overlay` del legacy |
| CAP-04a | W1280, 3 columnas a 1,6; DISTANCIA sola; la tarjeta del pulsómetro bajo la barra | **OK.** `PULSÓMETRO` se ve a través de la barra translúcida |
| CAP-04b | Al final del desplazamiento: tarjeta entera sobre la barra, `142` `bpm` en la línea de base, 64 dp | **OK a la vista** (comprobación de 64 dp: pasa). La sonda de aspecto de `bpm` falla en silencio (`#443650`, diferencia 187): ver QA-V2-04 |
| CAP-05 | TH, 3 columnas, sin desbordes | **OK.** Barra de desplazamiento siempre visible en un perfil de teléfono (QA-V2-13) |
| CAP-06 | 5 tarjetas, PULSO `140` en `#EF476F` | **OK.** `23.5` / `149` / `652m` (AC-BLE-01-05); etiqueta `#FF5A7E` exacta; valor `#EF476E` |
| CAP-07 | `100:01` `40.5` `1999` `123456m` achicados | **OK.** La cabecera dice `Rower` (remo de prueba sin nombre: el caso del legacy, se cierra en la 10a) |
| CAP-08 | Sin `Desconectar`; `Buscar dispositivos` habilitado; estado vacío; ayuda al 54 % | **OK.** Ayuda `#90969D` (esperado, diferencia 0) |
| CAP-09 | L640: `Buscar dispositivos` al 38 %; fila `Pulsómetro simulado` en el área de la tarjeta | **OK.** El texto del estado vacío queda cortado por el borde de su área desplazable (se alcanza desplazando, como en el legacy). La llave tapa el *chevron* de la fila (capa por encima, paridad) |
| CAP-10 | Corazón al 40 %, pista completa al 54 %, `Buscar` tonal | **OK.** Pista `#969FAA` (esperado `#969FA9`) |
| CAP-11 | *Spinner*, `Buscando sensores...`, fila con corazón sin relleno, `SIM:HRM`, flecha | **OK** |
| CAP-12a / b | `--` `bpm` con corazón pleno; después, `142` | **OK.** `bpm` `#FF5A7E` exacto en b; `--` como dos guiones; la tarjeta no cambia de alto |
| CAP-13 | `Pulsómetro simulado · reconectando...`, `Conectar` al 38 %, corazón al 40 % | **OK** |
| CAP-14a-d | Conectando sin botones; `· no encontrado` con `Conectar`; incompatible en rojo; error largo en 3 líneas con "…" | **OK** las cuatro |
| CAP-15 | `Buscar` al 38 % con el remo buscando | **OK** |
| CAP-16 | P800: panel de 280 dp, X dentro de la ventana, `150 W` `24 spm` `real`, sin llave | **OK.** La grilla de atrás está en ceros (el panel se abre antes de la primera lectura; AC-BLE-11-09) |
| CAP-17 | `Hora escena` `14:00`, `Hora real`, `Remando`, las dos fallas | **Parcial:** al final del desplazamiento no se ve el renglón `14:00` (no entra en un panel de 248 dp) y el catálogo no lo comprueba: QA-V2-05 |
| CAP-18 | TV: el panel entra entero, cinta visible, estética de la app | **OK** |
| CAP-19 | `500 W` `40 spm` `0:00`, `Remando` apagado; grilla `--:--` `0.0` `0` y `226m` congelada | **OK** |
| CAP-20a-e | Píldora detrás del ícono elegido, variante elegida, etiquetas siempre visibles; inglés | **OK.** `bluetooth_connected` solo elegido; `timer` relleno solo elegido |
| CAP-21a-c | Título y el ícono de la pestaña apagado, sin otro texto | **OK** (comprobación "ningún otro texto": pasa) |
| CAP-22 | Build normal: búsqueda vacía, `Sin sensor`, sin cinta ni llave | **OK** (arnés) y **OK en el build real** (ventana `RowMate`, §1.4) |
| CAP-23 | Las dos composiciones con el valor anotado | **OK:** app `#2A2F37` / `#BBC1C8`; directa `#6A6A6C` / `#DADBDC` |
| CAP-24 | Letra 1,3×, nada cortado, valores achicados | **OK.** `Pulsómetro simula…` termina en "…" (AC-HRM-13-06 lo admite) y la distancia dice `229m` porque `Settle` adelanta el reloj: la spec no fija un valor |
| CAP-25 | 5 destinos, `Perfil` al final con `person`, Historial cuarto | **OK** |
| AUX-01 / AUX-02 | `--` en inglés; CAP-01 con reducir movimiento | **OK.** AUX-02 = CAP-01 byte a byte (en reposo no hay movimiento) |
| VID-01 / 02 / 03 | 10 s, 10 s, 8 s | MP4 válidos (`ftyp mp42`): 10,00 s con 300 cuadros, 10,00 s con 300 y 8,00 s con 240 (30 fps). **No los revisé cuadro a cuadro** (no hay decodificador en la PC). El comportamiento lo cubren las PlayMode (`DSP.RollEndsOnTheExactValueAndRestarts`, `DSP.AccentRestsAtSixtyAndFlashesOnlyWhenTheTextChanges`, `DSS.HeartBeatsAtTheRealRate`, `DSS.DropShowsSearchForThreeSeconds`, `DSS.ReconnectShowsZerosThenValues`) y la extracción de cuadros del Designer (04-design-notes §15.4). En VID-03, esa extracción encontró `Rower` en la vista que sale; el código lo confirma (QA-V2-01) |

---

## 3. Matriz AC → test → resultado (125)

Abreviaturas de suites: [d] dotnet + Unity EditMode, [s] con simulador, [u] Unity EditMode, [p] PlayMode, [ps] PlayMode con simulador, [cap] captura, [win] PC con Windows. **PASS*** = pasa en lo verificable; la parte de equipo que se nombra queda NV.

### 3.1 F-APP-01 · F-APP-02 · F-APP-05 · F-APP-06 · F-APP-08 · F-APP-19 (30)

| AC | Evidencia | Resultado |
|---|---|---|
| AC-APP-01-01 | [d] `ShellStoreTests.InitialIndexIsDeviceAndNotPersisted` · [p] `ShellTests.StartsOnDeviceTab` · [win] los dos builds abren en Dispositivo y no hay clave de pestaña en `HKCU\Software\iguisoft\RowMate` | PASS |
| AC-APP-01-02 | [s] `SimulatorBackendTests.SimulatorReplacesBothServicesWithOneAthlete`, `.HeartRateComesFromRowerPhysics` · [u] `SimulatorModeTests.OnlyDeviceBackendFactoryChoosesTheVariant` · revisión: `DeviceBackendFactory.cs` | PASS |
| AC-APP-01-03 | [d] `UnavailableServicesTests.RowerStartsAndStaysDisconnected` · [p] `NormalBuildTests.NormalCompositionOpensOnSearchView`, `BootSceneTests.*` · [ps] `DSS.SimulatorOpensConnectedWithoutTouching` · [win] el normal abre en búsqueda y el de simulador en `Remo simulado` conectado | PASS |
| AC-APP-01-04 | [d] `AppComposerTests.StartLaunchesAutoConnectWithoutWaiting`, `.AutoConnectFailureGoesOnlyToTheLog` · [s] `SHS.AutoConnectDoesNothing` · [ps] `DSS.CardOpensInNoSensor` | PASS |
| AC-APP-01-05 | [ps] `DSS.DeviceTabKeepsUpdatingWhileHidden` | PASS |
| AC-APP-01-06 | [p] `ShellTests.NoDebugMarksBesidesTheSimulatorRibbon` · [u] `BVT.ProductIsRowMate` · [win] normal sin marcas y con título `RowMate`; el de simulador muestra "Development Build" (P-01) · Android NV | PASS* |
| AC-APP-01-07 | [u] `BVT.SplashOffAndBackgroundIsBgApp` · [p] `ThemeTests.SurfaceRtsStartCleared` · [win] S-B1 de QA: primer cuadro con 0 píxeles distintos de `bg.app` · Android NV | PASS* |
| AC-APP-01-08 | [d] `AppComposerTests.DisposeLeavesNoTimers` · [s] `SBK.DisposeLeavesNoTimers` + LT-20 a LT-25 y LT-32 | PASS |
| AC-APP-02-01 | [d] `SHL.FourDestinationsInOrderWithIcons` · [p] `ShellTests.TabBarHasFourDestinations` · [cap] CAP-20 | PASS |
| AC-APP-02-02 | [d] `SHL.ProfileOnlyWithStravaAndHistoryStaysThird` · [p] `ShellTests.FiveDestinationsWithStravaConfig` · [cap] CAP-25 | PASS |
| AC-APP-02-03 | [d] `SHL.SelectingTheSelectedDoesNothing` · [p] `ShellTests.SelectionShowsPillGlyphAndLabels` · [cap] CAP-20 | PASS |
| AC-APP-02-04 | [p] `ShellTests.ScrollPositionSurvivesTabSwitch` · [ps] `DSS.HrmSearchSurvivesTabSwitchAndExpiresAt10s` | PASS |
| AC-APP-02-05 | [d] `SHL.DeviceIconDependsOnSelectionOnly` · [p] `ShellTests.DeviceIconIgnoresRowerStatus` | PASS |
| AC-APP-02-06 | Reglas: [u] `SMT.BackButtonPolicyByApiLevel`, `.AppRootRoutesEscapeToHandleBack`; [p] `ShellTests.BackAtRootGoesToThePolicyOnce`. Falta el teléfono | **NV** (Device-And) |
| AC-APP-02-07 | [p] `ShellTests.ResizeChangesColumnsWithoutRebuilding` · [ps] `DSS.ResizeKeepsConnectionCardAndTab` · [d] `RealtimeTimerQueueTests.JumpOfFiveSecondsFiresOnceAndReanchors` · no se redimensionó la ventana real ni se rotó un teléfono | PASS* |
| AC-APP-02-08 | [p] `ShellTests.PlaceholdersShowTitleAndDimIconOnly` · [cap] CAP-21 | PASS |
| AC-APP-05-01 | [p] `ThemeTests.ThemeHasNoLightVariantAndIgnoresSystemTheme` (estático). En equipo no se probó: Windows del usuario en modo oscuro (`AppsUseLightTheme 0`) y no se cambió | **NV** (equipo) |
| AC-APP-05-02 | [p] `ThemeTests.BaseColorsReadFromPixels` · [cap] CAP-01 (diferencia 0) + `SnapshotEvidenceTests.BaseColors` | PASS |
| AC-APP-05-03 | [u] `FIT.FontAssetsExistAndCoverTheirSets`, `.IconFontHasEveryIconCodepoint` · [d] `IconsTests.*` · [cap] las 38 PNG | PASS |
| AC-APP-05-04 | [p] `ThemeTests.TabularCellsHaveFixedWidth`, `.UnchangedDigitsDoNotMove` | PASS |
| AC-APP-05-05 | [d] `TextScaleStepsTests.*` · [p] `ThemeTests.TextScale130HasNoOverflow`, `.LabelTrackingIsEightHundredthsEm` · [cap] CAP-24 · Android NV | PASS* |
| AC-APP-05-06 | [p] `ThemeTests.TertiaryTextsUseTextTertiary` · [cap] CAP-08 (`#90969D`) y CAP-10 (`#969FAA`) | PASS |
| AC-APP-05-07 | [p] `ThemeTests.TabWalksActionableControlsWithFocusRing`, `.EnterAndSpaceActivate` · en el build de Windows no se probó el teclado | PASS* |
| AC-APP-05-08 | [d] `DigitRollTests.ReducedMotionIsAnEightyMsFade` · [p] `ThemeTests.ReduceMotionChangesRollHeartAndTabs` · [cap] AUX-02 · Android NV | PASS* |
| AC-APP-05-09 | [p] `ThemeTests.PanelScaleEqualsDensity` · [win] a 96 dpi (100 %) la ventana mide 1280 × 720 y 1 dp = 1 px · 150 % y Android NV (no se cambió la escala del usuario) | PASS* |
| AC-APP-06-01 | [d] `MetricPaletteTests.ContractHexes` · [p] `DSP.TileIconLabelAndAccentColors` · [cap] CAP-01, CAP-06, CAP-12b (± 1) + `SET.MetricColorsOnValueStrokes` | PASS (observación QA-V2-04) |
| AC-APP-06-02 | [d] `ColorSourceTests.NoColorLiteralsOutsideTokens` | PASS |
| AC-APP-08-01 | [d] `LegacyDiscardTests.*` · QA: en `lib/` y `test/` del legacy nadie importa `shared/widgets`; los únicos `_MetricCard` y `_GlassMetricCard` son privados de `device_screen.dart:288` e `immersive_workout_screen.dart:741`; la app no porta `MetricCard` ni `IntervalTile` | PASS |
| AC-APP-08-02 | [p] `DSP.MetricTilesAndHrmCardShareTheCardContract` · [cap] CAP-04b, CAP-06 | PASS |
| AC-APP-19-05 | [d] `ItTwoToolingTests.CiUnityRunsEditAndPlayMode` (estático). Sin remoto ni licencia; además el *workflow* no es válido (QA-V2-14) | **NV** (CI) |

### 3.2 F-BLE-05 · -07 · -08 · -10 · -11 · -13 · -15 · -16 (48)

| AC | Evidencia | Resultado |
|---|---|---|
| AC-BLE-05-01 | [d] `DSR.ScanSpecSearchButton` · [p] `DSP.SearchButtonStatesWithTestRower` · [cap] CAP-08 | PASS |
| AC-BLE-05-02 | [d] `DSR.EmptyStateFromTheFirstFrame` · [p] `NBT.EmptyStateWithoutEverSearching` · [cap] CAP-08, CAP-22 · [win] build normal | PASS |
| AC-BLE-05-03 | [ps] LT-29, LT-31 | PASS |
| AC-BLE-07-01 | [d] `DSR.DisconnectOnlyWhenConnected` · [p] `DSP.DisconnectActionVisibility` · [cap] CAP-01, CAP-08 | PASS |
| AC-BLE-07-02 | [s] `SBS.DisconnectStopsReadingsAtOnce` · [ps] `DSS.DisconnectShowsEmptySearch` | PASS |
| AC-BLE-07-03 | [s] LT-22 · [ps] `DSS.NoAutoReconnectAfterDisconnect` | PASS |
| AC-BLE-07-04 | [s] `SBS.DisconnectBeatsAPendingReconnect` | PASS |
| AC-BLE-08-01 | [s] `DSF.SimulatedDropShowsSearchAndSearchReconnects` · [ps] `DSS.DropShowsSearchForThreeSeconds` | PASS |
| AC-BLE-10-01 | [p] `ShellTests.StartsOnDeviceTab`, `.TabBarHasFourDestinations` | PASS |
| AC-BLE-10-02 | [ps] LT-31 | PASS |
| AC-BLE-11-01 | [d] `DSR.ViewForEveryStatus` · [p] `DSP.ViewSwitchesWithStatus` | PASS |
| AC-BLE-11-02 | [p] `DSP.HeaderIconNameAndBadge` · [cap] CAP-01. **Falla** la cláusula "con el simulador no pasa": durante los 120 ms de salida de la vista conectada la cabecera muestra `Rower` + `Conectado` (QA-V2-01; `DeviceScreenView.cs:119`; 04-design-notes §15.5, VID-03) | **FAIL** (menor) |
| AC-BLE-11-03 | [d] `DSR.TilesOrderLabelsIconsAndColors` · [p] `DSP.FourTilesInOrder` · [cap] CAP-01, CAP-06 | PASS |
| AC-BLE-11-04 | [d] `DSR.HeartRateTileOnlyWithFtmsPulse` · [p] `DSP.PulseTileComesAndGoes` · [cap] CAP-06 | PASS |
| AC-BLE-11-05 | [d] `DSR.ColumnsAndAspectByWidth` (360 a 1280) · [p] `DSP.ColumnsAtProfiles` · [cap] CAP-01, -03, -04a, -05 (comprobaciones de columnas y proporción) | PASS |
| AC-BLE-11-06 | [d] `AccentPulseTests.*` · [p] `DSP.TileGeometryAndValueFits`, `.AccentRestsAtSixtyAndFlashesOnlyWhenTheTextChanges` · [cap] CAP-07 | PASS |
| AC-BLE-11-07 | [p] `DSP.ConnectedViewScrollsAsOneBlockWithMargins` · [ps] LT-28 · [cap] CAP-04b ("64 dp sobre la barra") | PASS |
| AC-BLE-11-08 | [d] `DSR.NoCaloriesTimeOrStrokes` · [p] `DSP.NoExtraMetrics` | PASS |
| AC-BLE-11-09 | [d] `DST.ZerosOnEveryConnect` · [ps] `DSS.ReconnectShowsZerosThenValues` | PASS |
| AC-BLE-11-10 | [s] `DSF.ReferenceValuesTable` · [ps] `DSS.GridShowsReferenceValuesAt60s` · [cap] CAP-01, CAP-19 | PASS |
| AC-BLE-11-11 | [d] `DigitRollTests.EndsOnTheExactValue`, `.OnlyChangedCellsRoll` · [p] `DSP.RollEndsOnTheExactValueAndRestarts` · VID-01 generado | PASS |
| AC-BLE-13-01 | [u] `SMT.SimulatorMenuExists`, `.NonDevelopmentBuildWithSimulatorFails`, `.BuildCliForcesDevelopmentWithSimulator` · [d] `ItTwoToolingTests.*` · QA: mi build con simulador salió Development; `ci-*.yml` no activan el define | PASS (observación QA-V2-11) |
| AC-BLE-13-02 | [p] `NBT.NormalCompositionHasNoSimulatorLayer` · [u] `SMT.BuildOutputInspectorFindsSimulatorTraces` · [win] build normal nuevo: inspector sin rastros, búsqueda propia de 10 cadenas con 0 resultados y ventana sin cinta ni llave | PASS (observación QA-V2-10) |
| AC-BLE-13-03 | [s] `SBK.SimulatorReplacesBothServicesWithOneAthlete` | PASS |
| AC-BLE-13-04 | [ps] `DSS.SameTreeWithSimulatedAndTestDevices` · [d] `AsmdefLintTests` · revisión: Presentation no referencia Simulator (asmdefs) | PASS |
| AC-BLE-13-05 | [d] `UNV.ScanAndSearchDoNothing` · [p] `NBT.NormalBuildSearchDoesNothing` · [cap] CAP-22 · [win] abre en búsqueda vacía con `Sin sensor`; el toque en el build no se probó | PASS* |
| AC-BLE-15-01 | [s] LT-20, LT-22, `SBS.NameOnlyWhileConnected` | PASS |
| AC-BLE-15-02 | [s] LT-20, `SBS.StartConnectsOnlyOnce`, `.AdapterIsAlwaysOn` | PASS |
| AC-BLE-15-03 | [s] LT-20 | PASS |
| AC-BLE-15-04 | [s] LT-22, `SBS.ConnectReconnectsAtOnce`, `.StopScanDoesNothing` | PASS |
| AC-BLE-15-05 | [s] `SBS.DisconnectStopsReadingsAtOnce` | PASS |
| AC-BLE-15-06 | [s] LT-21, `SBS.SimulateDisconnectWhenNotConnectedDoesNothing` | PASS |
| AC-BLE-15-07 | [s] `SBS.ConnectWhileConnectedDoesNotRepeat` | PASS |
| AC-BLE-15-08 | [s] `SBS.NoRawBytesNorDeviceList` | PASS |
| AC-BLE-15-09 | [s] `DSF.PhysicsDoesNotRestartOnReconnect` (`37m` → `41m`, 11 s) | PASS |
| AC-BLE-15-10 | [ps] `DSS.OpensConnectedAndDropShowsSearch` / `.DropShowsSearchForThreeSeconds` · [cap] CAP-08 | PASS (el texto falso de la salida va en AC-BLE-11-02) |
| AC-BLE-16-01 | [ps] `SOT.RibbonIsAlwaysOnTop` (también con insets) · [cap] CAP-01 (`#FF5722`) | PASS |
| AC-BLE-16-02 | [ps] LT-26, `SOT.WrenchPositionAndToggle` · [cap] CAP-01, CAP-18 | PASS |
| AC-BLE-16-03 | [s] `SPS.MaxHeightRule` · [ps] LT-27, `SOT.InsetsAreCountedOnce` | PASS |
| AC-BLE-16-04 | [ps] LT-26, `SOT.PanelContentAndOrder` · [cap] CAP-16, -18 (CAP-17: QA-V2-05) | PASS |
| AC-BLE-16-05 | [s] `SPS.PresetsSetTargetsOnly` · [ps] LT-26 | PASS |
| AC-BLE-16-06 | [s] `SPS.WattsAndSpmStepsAndLimits` · [ps] LT-26 · [cap] CAP-19 | PASS |
| AC-BLE-16-07 | [s] `SPS.HourStepsWrapAndRealHour` · [ps] LT-26 · [cap] CAP-17 (`Hora real`), CAP-19 (`0:00`) | PASS |
| AC-BLE-16-08 | [s] `SPS.RowingToggle`, `DSF.RowingOffZeroesAndFreezes` · [ps] LT-26 · [cap] CAP-19 | PASS |
| AC-BLE-16-09 | [s] `SPS.FaultsWithoutErrors` · [ps] LT-26 | PASS |
| AC-BLE-16-10 | [s] `DSF.PanelChangesShowOnTheNextReading` · [ps] `SOT.ChangesVisibleWithinOneSecond` | PASS |
| AC-BLE-16-11 | [s] `SPS.NothingPersists` · [ps] `SOT.ReopenShowsCurrentValues` · [win] la app no escribe nada propio en el registro ni en `LocalLow` (solo `Player.log`) | PASS |
| AC-BLE-16-12 | [ps] LT-27 · [cap] CAP-16 | PASS |

### 3.3 F-HRM-04 · -12 · -13 · -15 (35)

| AC | Evidencia | Resultado |
|---|---|---|
| AC-HRM-04-01 | [s] LT-23, `SHS.EveryReadingIsPublishedEvenIfRepeated` | PASS |
| AC-HRM-04-02 | [d] `HeartRateContactTests.*` | PASS |
| AC-HRM-04-03 | [d] `HRS.InitialBpmIsZero`, `HCR.ZeroShowsDashes` · [ps] LT-28 · [cap] CAP-12a | PASS |
| AC-HRM-04-04 | [s] LT-25, `SHS.ZeroIsPublishedOnlyOnce` | PASS |
| AC-HRM-04-05 | [s] `DSF.BpmDoesNotDependOnTheRower` · [ps] `DSS.PulseKeepsComingWithTheRowerDisconnected` | PASS |
| AC-HRM-12-01 | [s] LT-32 · [d] `HRS.InitialState` | PASS |
| AC-HRM-12-02 | [s] LT-32 · [d] `HRS.ChosenNameOnlyForThatAttempt` | PASS |
| AC-HRM-12-03 | [s] LT-33 · [d] `HRS.ErrorClearedOnStartAndOnConnectedNotOnDisconnected` | PASS |
| AC-HRM-12-04 | [s] LT-34 | PASS |
| AC-HRM-12-05 | [d] `HRS.StartScanClearsErrorAndList` | PASS |
| AC-HRM-12-06 | [d] `HRS.NoNotificationAfterDispose` | PASS |
| AC-HRM-13-01 | [ps] LT-28, LT-30 · [p] `DSP.CardPlacementInBothViews` | PASS |
| AC-HRM-13-02 | [d] `HCR.TitleIsUpperAndHeartState` · [p] `HCS.HeaderAndStructure` · [cap] CAP-10 a CAP-15 | PASS |
| AC-HRM-13-03 | [d] `HCR.BodyTable` · [p] `HCS.BodiesWithScriptedHrm` · [cap] CAP-10 a CAP-14 | PASS |
| AC-HRM-13-04 | [d] `HCR.EnabledTable` · [p] `HCS.DisabledButtonsDoNotRespond` · [ps] LT-31 · [cap] CAP-13, CAP-15 | PASS |
| AC-HRM-13-05 | [d] `HCR.RowTitleFallbacks` · [p] `HCS.DeviceRowContent` · [cap] CAP-11 | PASS |
| AC-HRM-13-06 | [p] `HCS.ConnectedBodyLayout` · [cap] CAP-12, CAP-04b, CAP-24 | PASS |
| AC-HRM-13-07 | [d] `HCR.ErrorTexts` · [p] `HCS.ErrorClampedToThreeLines` · [cap] CAP-14c/d | PASS |
| AC-HRM-13-08 | [d] `DSR.ScanSpecExcludesHrmScan` · [ps] LT-29, LT-31 · [cap] CAP-09, CAP-15 | PASS |
| AC-HRM-13-09 | [ps] LT-28 (800 × 600 dp, con *hit-test*: "hay que desplazarse") | PASS |
| AC-HRM-13-10 | [ps] LT-30 · [cap] CAP-09 | PASS |
| AC-HRM-13-11 | [d] `HCR.NoCancelSignalOrStop` · [p] `HCS.NoExtraControls` | PASS |
| AC-HRM-13-12 | [d] `HeartbeatClockTests.*` · [ps] `DSS.HeartBeatsAtTheRealRate` · VID-01 generado | PASS |
| AC-HRM-15-01 | [s] `SHS.NoMemoryAndAutoConnectDoesNothing` · [ps] `DSS.CardOpensInNoSensor` | PASS |
| AC-HRM-15-02 | [s] LT-23, LT-24, `SHS.SingleResultDetails` | PASS |
| AC-HRM-15-03 | [s] `SHS.StopScan` | PASS |
| AC-HRM-15-04 | [s] LT-23, `SHS.DirectToConnectedFirstReadingAfterOneSecond` | PASS |
| AC-HRM-15-05 | [s] LT-23, `SHS.NameOnlyWhileConnected` | PASS |
| AC-HRM-15-06 | [s] LT-25 | PASS |
| AC-HRM-15-07 | [s] `SHS.ForgetCancelsTheReturnAndKeepsTheConnection` · [ps] `DSS.ForgetDuringReconnecting` | PASS |
| AC-HRM-15-08 | [s] LT-25 · [ps] LT-26, `DSS.HrmDropShowsReconnecting` · [cap] CAP-13 | PASS |
| AC-HRM-15-09 | [s] `SBK.HeartRateIs142At60s` · [cap] CAP-12b (`142`) | PASS |
| AC-HRM-15-10 | [s] `DSF.BpmKeepsPublishingWithTheRowerStopped` | PASS |
| AC-HRM-15-11 | [s] LT-23, LT-24, LT-25, LT-32 | PASS |
| AC-HRM-15-12 | [s] `SHS.SearchOtherCancelsTheReturn` · [ps] `DSS.SearchOtherDuringTheDrop` | PASS |

### 3.4 Criterios de la It. 1 que se verifican en la It. 2 (12)

| AC | Evidencia | Resultado |
|---|---|---|
| AC-BLE-01-05 | [p] `DSP.GridTextsLikeImage01` (23,5 / 149 / 652, cultura `es-AR` y `de-DE`) · [cap] CAP-06 (`23.5` `149` `652m`), CAP-01 (`24.0` `150` `226m`) | PASS |
| AC-APP-09-04 | [ps] `LOS.NoLanguageSelectorAndNoMemory` · [d] `LanguageStateTests.NoSetterNoPersistence` · [win] la app abre en el idioma de la lista del sistema (`en-US, es-ES` → inglés) y no guarda nada; no se cambió el idioma de Windows | PASS* |
| AC-APP-09-05 | [ps] `LOS.LiveLanguageChangeKeepsEverythingElse` · Android NV | PASS* |
| AC-APP-09-06 | [d] `LocaleResolverTests` · [u] `SystemLocaleProviderTests.WindowsListIsBcp47AndNotEmpty`. Las listas `fr-FR, es-AR` y `pt-BR` no están configuradas en la PC y no las cambié | **NV** (equipo) |
| AC-APP-16-01 | [u] `BVT.ProductIsRowMate` · [win] `RowMate.exe`, título `RowMate`, `iguisoft/RowMate`, `[APP] RowMate 1.0.0 (1)` como primera línea propia del registro (P-06; el `.exe` trae la versión del motor y no tiene producto ni empresa) · Android NV | PASS* |
| AC-APP-16-02 | [win] `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate\Player.log`; nada en `Roaming\com.example` · Android NV | PASS* |
| AC-APP-16-03 | [win] parcial: las preferencias del legacy no cambiaron (mismo SHA-1 y fecha). No abrí la app legacy para ver que conserva todo, y no hay teléfono | **NV** (parcial) |
| AC-APP-16-04 | Sin módulo de Android ni teléfono | **NV** |
| AC-BASE-01 | [d] y Unity EditMode verdes (`ParitySmokeTests.NoDifferences` incluido). IL2CPP: "scripting backend (IL2CPP) is not installed" | PASS* (IL2CPP NV) |
| AC-BASE-05 | [cap] CAP-23 + `rt1-measurement.json` (app `#2A2F37` / `#BBC1C8`, directa `#6A6A6C` / `#DADBDC`) + `SET.Rt1MeasurementPresent` · [win] S-B1 de QA en el build: `#2A2F37` / `#BAC1C8` | PASS |
| AC-BASE-08 | Unity con simulador 1281 / 1265 / 0 / 16 contra dotnet 1264 / 1247 / 0 / 17, con la diferencia explicada (§1.2). Las ignoradas "→ It. 2" que tienen UI se verifican en PlayMode (`DSP.GridTextsLikeImage01`, `LOS.*`), pero su motivo de `[Ignore]` sigue viejo (QA-V2-06) | PASS (observación) |
| AC-BASE-10 | Apertura con exit 0 y sin errores; `ProjectVersion.txt` `6000.3.27f1`; `[BOOTSTRAP] versión 3 ya aplicada …: sin cambios`; [u] `BVT.Version3AppliedOnce`, `.BootSceneIsTheOnlyBuildScene`; el define no se activó (SHA-1 de `ProjectSettings.asset` igual) | PASS |

### 3.5 Conteo

| | PASS | de los cuales PASS* | FAIL | NV |
|---|---|---|---|---|
| APP (30) | 27 | 7 (01-06, 01-07, 02-07, 05-05, 05-07, 05-08, 05-09) | 0 | 3 (02-06, 05-01, 19-05) |
| BLE (48) | 47 | 1 (13-05) | 1 (11-02) | 0 |
| HRM (35) | 35 | 0 | 0 | 0 |
| It. 1 (12) | 9 | 5 (09-04, 09-05, 16-01, 16-02, BASE-01) | 0 | 3 (09-06, 16-03, 16-04) |
| **Total (125)** | **118** | **13** | **1** | **6** |

---

## 4. Los 15 casos legacy

Todos con el nombre exacto como `TestName`, el comentario `// legacy: test/<archivo>:<línea> '<nombre>'` y la categoría `LT-nn`. `TraceabilityTests.It02LegacyCasesKeepTheirNames` pasa. Los comparé uno por uno con el legacy (`test/*.dart`).

| LT | Legacy | Port | Mismos casos | Resultado |
|---|---|---|---|---|
| LT-20 | `simulated_ble_service_test.dart:13` | `Tests/Simulator/SimulatedBleServiceTests.cs:33` | Sí: `[connected]`, adaptador `[on]` (grabado con el valor actual, documentado), nombre, 3 lecturas con pulso 0 y distancia creciente; suma "sin *timers*" | dotnet ✔ · Unity EM ✔ |
| LT-21 | `:39` | `:65` | Sí: 2 s `[connected, disconnected]` y 0 lecturas; 3 s `[…, connected]` | ✔ · ✔ |
| LT-22 | `:64` | `:85` | Sí | ✔ · ✔ |
| LT-23 | `simulated_heart_rate_service_test.dart:10` | `SimulatedHeartRateServiceTests.cs:30` | Sí: `[scanning]`, un solo resultado, conectado, 2 `tick` + 3 s = 3 pulsos, el último = `sim.HeartRate` > 0 = `CurrentBpm`, sin *timers* | ✔ · ✔ |
| LT-24 | `:49` | `:68` | Sí | ✔ · ✔ |
| LT-25 | `:62` | `:82` | Sí: reintentando, recordado, último 0; 2 s / 3 s; desconectar + 10 s; sin *timers* | ✔ · ✔ |
| LT-26 | `simulator_overlay_test.dart:17` | `Tests/PlayModeSimulator/SimulatorOverlayTests.cs:30` | Sí, con la UI entera en 800 × 600 (el legacy monta un `Scaffold('home')`), toques con *hit-test* y la capa como último hijo de la raíz. Diferencias sin efecto: espera 3,5 s después de `Simular desconexión` y conecta el pulsómetro por el *store* (el legacy, por el servicio) | PlayMode ✔ |
| LT-27 | `:90` | `:69` | Sí, en P800 (densidad 1); suma la comprobación del alto máximo | ✔ |
| LT-28 | `heart_rate_card_test.dart:83` | `HeartRateCardTests.cs:40` | Sí, en 800 × 600 dp; "hay que desplazarse" se exige con `WhyNotTappable` | ✔ |
| LT-29 | `:121` | `:67` | Sí | ✔ |
| LT-30 | `:142` | `:83` | Sí: L640, `Audit()` vacío antes y después; la fila dentro de `#hrm-card-slot-scan` | ✔ |
| LT-31 | `:161` | `:100` | Sí: remo de prueba + pulsómetro simulado (`TestRowerWithSimulatedHeartRate`), buscando → no, conectando → no, desconectado → sí | ✔ |
| LT-32 | `heart_rate_provider_test.dart:54` | `Tests/Simulator/HeartRateProviderTests.cs:19` | Sí; el nombre "conectando" se lee en el primer estado grabado (entrega sincrónica, D-04) | ✔ · ✔ |
| LT-33 | `:107` | `:73` | Sí; además comprueba que la `Task` no queda con la excepción | ✔ · ✔ |
| LT-34 | `:122` | `:95` | Sí | ✔ · ✔ |

---

## 5. Revisión estática

| Tema | Qué miré | Conclusión |
|---|---|---|
| Composition root | `Bootstrap/AppRoot.cs`: Awake (cultura invariante, registro, *scheduler*, plataforma, `DeviceBackendFactory`, `AppComposer`, copia del panel y `UiSurface`) y Start (`ShellHost.Build`, `_app.Start()`); `OnDestroy` en orden inverso. `ShellHost` / `UiTestHost` / arnés usan la misma composición | Bien. Cierra QA-G2-01 (la UI se arma en `Start`). Un solo `#if ROWMATE_SIMULATOR` elige servicios; `SimulatorLayerFactory` solo agrega la capa. Diagnósticos activos en el build normal: QA-V2-09 |
| Hilos | `UnityScheduler` + `RealtimeTimerQueue`: un solo hilo, disparos en `AppRoot.Update`, excepción de un *callback* aislada, periódicos reanclados después de un corte (QA-G2-15). `HeartRateStore`: `async` sin `ConfigureAwait(false)` (las continuaciones vuelven al hilo principal por el `UnitySynchronizationContext`). `ObservableValue` y `EventStream` aíslan suscriptores (QA-V-01 cerrado) | Bien para la It. 2: todo corre en el hilo principal. Para la 10a (*callbacks* nativos de BLE), el `Dispose` de un `Entry` desde el `ct.Register` de otro hilo tocaría la lista sin *lock*: anotarlo para el diseño de la 10a |
| *Allocations* por cuadro | `MotionDriver.Tick` (arreglo propio), `TabularNumber` (cadenas de un carácter precalculadas, `DrawText`), `HeartbeatAnimator`, `Spinner`, `ShellLayout.Poll`, `UiSurface.Update`, `PanelScaleController.Update`, `FramePacer.Apply`, `RealtimeTimerQueue.RunDue` (lambda estática en `RemoveAll`). Revisé todo `Tick(double)` de `Presentation/Motion` y los controles | Sin asignaciones por cuadro. Lo que asigna es una vez por lectura (1 Hz: reglas y modelos de la tarjeta) o diagnóstico (`DescribeFinite`) |
| Simulador fuera del release | `defineConstraints` en `RowMate.Simulator`, `.Simulator.Presentation`, `Tests.*`, `Snapshots`; `SimulatorBuildGuard`; `BuildCli` (`extraScriptingDefines` + Development); `BuildOutputInspector`; `SimulatorDefine` + marca; build normal nuevo sin rastros (§1.3) | Bien. Refuerzos sugeridos: QA-V2-10 (agujas con los textos visibles) y QA-V2-11 (`#error` en el ensamblado). El build normal incluye otro artefacto de tests: QA-V2-02 |
| Locale | `AppRoot` fija `CultureInfo.InvariantCulture` en el hilo y por defecto. En Windows, `GetUserPreferredUILanguages` (verificado en la PC: `en-US, es-ES` → inglés). `OnApplicationFocus` vuelve a leer idioma, letra (`TextScaleFactor`) y movimiento (`SPI_GETCLIENTAREAANIMATION`). PlayMode con `es-AR` y `de-DE` | Bien |
| Wakelock | Ningún `Screen.sleepTimeout` ni `NeverSleep` en el código (D-08, DV-08). `Application.targetFrameRate = 60` + `OnDemandRendering` (30 fps en reposo) | Según lo acordado. Con vSync en escritorio `targetFrameRate` no limita; en Android, una pantalla de 120 Hz queda en 60 (ARCHITECTURE §13.1) |
| R-T1 | `UiSurface` (RT UNorm + `forceGammaRendering`, *stencil*, limpieza a `bg.app`), `OverlayCopyPresenter` (copia en `beginContextRendering`, objeto en la raíz de la escena), `MeshColor` (compensación de `DrawText`) | Medido en el arnés y en el build (§1.4). La entrada con `targetTexture` usa el mapeo por defecto y el clic llega (S-B1 criterio 2). Contraste `metric.hr.hud` sobre el vidrio medido: 4,50 (QA-V2-12) |
| Plataforma Windows | DPI con `GetDpiForWindow(GetActiveWindow())`, con respaldo a `Screen.dpi` | Si la ventana no es la activa en un cambio de tamaño, `GetActiveWindow()` es nulo y se usa `Screen.dpi` (del sistema): en monitores mixtos puede tomar la escala equivocada. A probar en la 14 con dos monitores. Barra de título clara con Windows en modo oscuro: QA-V2-08 |
| Deuda de la It. 1 | QA-V-01 (aislamiento), -03 (*bootstrapper* una vez por sesión), -04 (`RowMate.EditorTools`), -05 (procedimiento `-Simulator`), -06 (texto visible) | Cerrados en el código y con tests. QA-V-05 verificado de punta a punta (§1.2) |

---

## 6. Issues

### 6.1 Tabla

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V2-01 | menor | architect | Al desconectar el remo (botón `Desconectar` o `Simular desconexión`), la vista conectada que se va muestra **`Rower` + `Conectado`** durante su salida (120 ms; 4 cuadros en VID-03). El legacy nunca muestra `Rower` con el simulador (AC-BLE-11-02) | `Presentation/Device/DeviceScreenView.cs:119`: `Render()` siempre llama a `Connected.Header.Render(DeviceScreenRules.DeviceName(state.ConnectedDeviceName), …)`; desconectado, `ConnectedDeviceName` es `null` → `FallbackRowerName "Rower"` (`App/Device/DeviceScreenRules.cs:27`), y `_swap.Play(Connected, Scan, …)` mantiene visible la vista conectada mientras sale. 04-design-notes §15.5, fila VID-03 (cuadros extraídos) | Renderizar la cabecera (y la insignia) solo con `state.Status == Connected`, o congelar la vista saliente. Test PlayMode: tocar `device-disconnect`, avanzar 60 ms y que `TextOf("device-name") == "Remo simulado"` |
| QA-V2-02 | menor | architect | El **build normal** lleva `PerformanceTestRunInfo` y `PerformanceTestRunSettings` (lista de paquetes, versión del Editor) como `TextAsset` en `resources.assets`. Además, un build que falla deja `Assets/Resources/PerformanceTestRun*.json` en el repo, y el build siguiente los incluye | Paquete `com.unity.test-framework.performance` 3.0.3 (dependencia de `com.unity.collections`, `packages-lock.json:40`): `TestRunBuilder.OnPreprocessBuild` los crea y solo `OnPostprocessBuild` los borra (`Library/PackageCache/…/Editor/TestRunBuilder.cs:29-44`). Encontrados en `resources.assets` de mi build normal y de `Builds/Windows/normal`. Quedaron después del build IL2CPP fallido (§1.5) | `IPreprocessBuildWithReport` propio con `callbackOrder` > 0 que borre `Assets/Resources/PerformanceTestRun*` (+ `.meta`) y refresque antes de juntar los activos; sumar `PerformanceTestRunInfo` a las agujas de `BuildOutputInspector`; `.gitignore` de `Assets/Resources/PerformanceTestRun*` |
| QA-V2-03 | menor | architect | Sin el módulo IL2CPP, `build.ps1` sale con **1** ("el build falló") y no con **3** (NO VERIFICABLE), como dicen su cabecera y `BuildCli` | Log: `Error building Player: Currently selected scripting backend (IL2CPP) is not installed.`, `[BUILD] el build falló: Failed (1 errores)`, `[build] Windows normal IL2CPP -> 1`. Además el log se llama `build-windows-normal.log` para Mono y para IL2CPP, y uno pisa al otro | Antes de compilar, comprobar el backend instalado (carpeta `Variations/*il2cpp*` de `BuildPipeline.GetPlaybackEngineDirectory`) o mapear ese error del reporte a 3; incluir el backend en el nombre del log |
| QA-V2-04 | menor | architect | La sonda de aspecto `metric.hr.hud:bpm` de **CAP-04b falla** (`#443650`, diferencia 187) y la toma queda "ok": las sondas de aspecto no hacen fallar la toma, e `INDEX.md` solo dice "1/2" | `manifest.json` CAP-04b. A densidad 1, el "color dominante" de un texto Inter 14/400 es un borde suavizado. La misma sonda da `#FF5A7E` exacto en CAP-12b (2,625) | Sonda por "píxel más saturado" o solo en CAP-12b; marcar en `INDEX.md` las sondas de aspecto que fallan (columna o ⚠) |
| QA-V2-05 | menor | po + architect | **CAP-17** pide `Hora escena 14:00` junto con `Hora real`, `Remando` y las dos fallas, pero al final del desplazamiento el renglón `14:00` no entra (panel de 248 dp en P800). El catálogo no lo comprueba | `CAP-17-panel-p800-final.png`; `SnapshotCatalog.cs` (CAP-17 sin `Checks`); 04-design-notes §15.5 | PO: aceptar `14:00` fuera de la foto o pedir dos tomas; Architect: sumar la comprobación `TextOf("sim-hour-value") == "14:00"` (o `CAP-17b` desplazada hasta `#sim-hour-row`) |
| QA-V2-06 | menor | architect | Motivos de `[Ignore]` viejos: siguen diciendo "NO VERIFICABLE → It. 2" aunque la It. 2 ya los verifica en PlayMode o los deja para QA en equipo | `LanguageStateTests.cs:56`, `:116`; `RowingFormatsTests.cs:33`; `LocaleResolverTests.cs:119`; `AppIdentityTests.cs:83-98`. 02-tech-design §5.1 pedía cambiarlos a "se verifica en PlayMode: …" | Actualizar los motivos (PlayMode: `DSP.GridTextsLikeImage01`, `LOS.*`; equipo: "QA en equipo, ver 05-qa-verification It. 2") |
| QA-V2-07 | menor | architect | Cada build y cada corrida vuelven a serializar los 7 `*-SDF.asset` de fuentes (hasta 6,8 MB), los 2 de íconos y 2 de URP: ruido de varios MB en git y contenido que depende de la última corrida | Fechas iguales (19:37:19, durante mi build) en `UI/Fonts/Generated/*.asset`, `UI/Icons/Generated/*.asset`, `Settings/URP/RowMate_URP_Default.asset`, `UniversalRenderPipelineGlobalSettings.asset`; `m_AtlasPopulationMode: 1` (dinámico) y `m_ClearDynamicDataOnBuild: 1`; tamaños dispares (ExtraBold 5,7 KB, Regular 6,8 MB) | Atlas estáticos con los juegos de caracteres fijos para los activos versionados (lo dinámico como respaldo en tiempo de ejecución), o no versionar los datos dinámicos |
| QA-V2-08 | menor | po / architect | Con Windows en modo oscuro, la barra de título de la ventana sale **clara**. El legacy la pone oscura siguiendo al sistema. Es una diferencia visible que no está en §11.1 | `…/scratchpad/CAP-22-ventana-qa.png` con `AppsUseLightTheme = 0`; legacy `windows/runner/win32_window.cpp:275-285` (`UpdateTheme`, `DWMWA_USE_IMMERSIVE_DARK_MODE`) | Platform (Windows): `DwmSetWindowAttribute(hwnd, 20, …)` al arrancar y al cambiar el tema, como el legacy; o que el PO la sume como DV y la pase a la It. 14 |
| QA-V2-09 | menor | architect | Los diagnósticos de línea de comandos funcionan también en el **build normal**: `-rowmate-color-probe <ruta>` reemplaza la UI y escribe un JSON en la ruta que se le pase; `-rowmate-ui-pipeline Overlay` cambia la composición | `Bootstrap/AppRoot.cs:86`, `:112-117`; `Bootstrap/ColorProbe.cs:164` (`File.WriteAllText(_output, …)`) | Limitarlos a `Debug.isDebugBuild` (o `#if DEVELOPMENT_BUILD \|\| UNITY_EDITOR`); dejar `Overlay` como respaldo solo si el PO lo quiere en release |
| QA-V2-10 | menor | architect | Las agujas de `BuildOutputInspector` son solo nombres de tipos; no incluyen los textos que AC-BLE-13-02 nombra (`Remo simulado`, `Pulsómetro simulado`, `SIMULADOR`) | `Editor/Build/BuildOutputInspector.cs:13-19`. La búsqueda manual de QA dio 0 (§1.3) | Sumar esas tres cadenas (UTF-8 y UTF-16) |
| QA-V2-11 | menor | architect | R-01 depende de los defines de `PlayerSettings` y de `BuildCli`. Un build con `extraScriptingDefines` o con los defines de un *Build Profile* de Unity 6, sin Development y hecho por fuera de `BuildCli`, no lo frena `SimulatorBuildGuard` (que lee `PlayerSettings`) | `Editor/Simulator/SimulatorBuildGuard.cs:36-49`; no hay `#error` en el código | En `RowMate.Simulator`: `#if ROWMATE_SIMULATOR && !DEVELOPMENT_BUILD && !UNITY_EDITOR` + `#error`. Cuesta una línea y cubre cualquier camino |
| QA-V2-12 | menor | designer | Contraste medido de `metric.hr.hud` sobre el vidrio: **4,50**, justo en el umbral AA (el diseño esperaba 4,51). No se usa en la It. 2; sí en el HUD de la 4a | `rt1-measurement.json`: `"metric.hr.hud/glass": 4.50` | Confirmar el valor sin redondear ≥ 4,5 o subir un poco la luminancia antes de la 4a |
| QA-V2-13 | menor | architect / designer | La barra de desplazamiento vertical queda **siempre visible** en los perfiles de teléfono (CAP-05 TH). En Android el legacy no la muestra en reposo | `ConnectedView.cs:26` y `ScanView.cs:44`, `:68` solo ocultan la horizontal; CAP-05 | Con `!IsDesktop`, `verticalScrollerVisibility = Hidden` (o barra que aparece al desplazar) |
| QA-V2-14 | menor | architect | `ci-unity.yml` no es válido y no hace lo que pide AC-APP-19-05: usa `if: ${{ secrets.UNITY_LICENSE != '' }}` a nivel de *job* (GitHub no permite `secrets` en `jobs.<id>.if`), corre solo a mano (`workflow_dispatch`, no "en cada cambio") y no corre las suites del simulador (LT-26 a LT-31) | `.github/workflows/ci-unity.yml:6-11`; `ItTwoToolingTests.CiUnityRunsEditAndPlayMode` no lo detecta | Pasar el secreto a `env` y condicionar los *steps* (o un *job* previo que exponga un *output*); `on: push / pull_request`; matriz con `-Simulator` (Enable → tests → Restore) |
| QA-V2-15 | menor | architect | VID-03 provoca la caída por API (`h.Simulator!.SimRower.SimulateDisconnect()`) y no tocando `Simular desconexión` en el panel, como dice la spec | `SnapshotCatalog.cs` (VID-03) | Tocar `sim-drop-rower` con el panel abierto, o anotarlo en el catálogo (el panel ya lo cubre LT-26) |

### 6.2 Escenarios concretos de los dos bugs de comportamiento

- **QA-V2-01.** Build de simulador (o el arnés, VID-03), remo conectado: tocar `Desconectar`. Esperado (legacy): pasa a la vista de búsqueda sin mostrar otro nombre. Obtenido: durante la salida de 120 ms la cabecera dice `Rower` con la insignia `Conectado`, y después aparece la búsqueda.
- **QA-V2-02.** `tools/build/build.ps1 -Target Windows -Backend Mono` (sin simulador). Esperado: el build normal no trae nada de los tests. Obtenido: `RowMate_Data/resources.assets` contiene `PerformanceTestRunInfo`, `MeasurementCount` y `com.unity.test-framework`. Si antes falló un build (por ejemplo IL2CPP sin módulo), `Assets/Resources/` queda creado en el repo.

### 6.3 NO VERIFICABLE (con motivo)

| Qué | Motivo |
|---|---|
| AC-APP-02-06, AC-APP-16-04 y las partes Android de AC-APP-01-06, -07, -02-07, -05-05, -05-08, -05-09, -09-05, -16-01, -16-02, AC-BASE-01 | Sin módulo de Android ni teléfono |
| AC-BASE-01 (IL2CPP de Windows) | Módulo IL2CPP no instalado |
| AC-APP-05-01 (equipo) | Windows del usuario en modo oscuro; no cambié su configuración |
| AC-APP-09-06 | Listas de idioma de la spec no configuradas en la PC; no las cambié |
| AC-APP-16-03 | No abrí la app legacy; lo de la app nueva (no toca las preferencias del legacy) sí está verificado |
| AC-APP-05-09 a 150 % | No cambié la escala de pantalla del usuario |
| AC-APP-19-05 | Sin repositorio remoto ni licencia en el CI (y `ci-unity.yml` inválido, QA-V2-14) |
| VID-01 a VID-03 cuadro a cuadro | Sin decodificador de video en la PC; los cubren las PlayMode y la revisión del Designer |

Los 16 ignorados de Unity (`ScanFilterTests.RealPlatformScan`, `LanguageStateTests.NoSelectorOnScreen` / `.SystemTextsFollowSystemLanguage` / `.VisibleChangeWithoutRestart`, `LocaleResolverTests.NativeListOnDevices`, `RoutineSummaryTests.StartDialogOnScreen`, `RowingFormatsTests.DeviceGridShowsTheTextsOnScreen`, `StepLabelsTests.EditorSubtitleOnScreen`, `StepTypeTests.IconsInEditor`, 5 de `AppIdentityTests` y 2 de `CiWorkflowTests`) son de equipo, de CI o de las It. 3, 4a, 10a y 10b, o se verifican en PlayMode (QA-V2-06).

---

## 7. Resumen por rol

- **Architect:** QA-V2-01 (antes del REPORT: es una línea y un test), -02, -03, -04, -06, -07, -09, -10, -11, -13, -14, -15; y con el PO, -05 y -08.
- **PO:** QA-V2-05 (texto de CAP-17) y QA-V2-08 (barra de título: arreglar o anotarla como DV). El resto de §11 se cumple como se decidió (D-01 con S-B1 aprobado también en el build de QA; P-01, la marca "Development Build", solo en el build de simulador).
- **Designer:** QA-V2-12 (contraste 4,50 antes de la 4a) y QA-V2-13 con el Architect.
- **Usuario (prerrequisitos):** con los módulos Windows IL2CPP y Android y un teléfono 8.0+ de 64 bits se pueden cerrar AC-BASE-01 (IL2CPP), AC-APP-16-04 y las partes Android. Para AC-APP-16-03 hace falta abrir la app legacy junto a la nueva.

`git -C ../rowmate-legacy status --porcelain`: vacío al empezar y al terminar. No se leyó `lib/core/strava/strava_secrets.dart`. No hice commits. El único cambio que dejé en el repo, además de este archivo, es el `manifest.json` regenerado. Las PNG, los MP4, `INDEX.md` y `rt1-measurement.json` quedaron byte a byte iguales a los anteriores.
