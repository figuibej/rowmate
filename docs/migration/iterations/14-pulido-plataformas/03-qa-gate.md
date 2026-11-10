# It. 14 — Gate de QA (armonía QUÉ ↔ CÓMO)

| Campo | Valor |
|---|---|
| Entrada | [01-po-spec](01-po-spec.md) (44 AC), [02-tech-design](02-tech-design.md) (DT14-01 a -30, D-01 a -04), [00-arte-pulido](00-arte-pulido.md) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) leído (solo lectura, `git status` vacío): `AndroidManifest.xml:4-19` y `configChanges`, `values-v31/styles.xml` (`#0D1B2A`), `Info.plist` (iPhone 3 orientaciones, iPad 4), `MainMenu.xib:335` (800 × 600), `windows/runner/main.cpp:28-33` (10,10; 1280 × 720; `rower_app`; `SetQuitOnClose`), `pubspec.yaml:4` (`1.0.0+1`). Todo coincide con lo que cita la spec. No se tocó el módulo de credenciales de Strava |
| API de Unity 6000.3.27f1 | Verificada en el editor instalado: `AssistiveSupport` (`activeHierarchy`, `isScreenReaderEnabled`, `screenReaderStatusOverride` con `ForceEnabled`/`OSDriven`, `notificationDispatcher`), `AccessibilityHierarchy` (`AddNode`, `Clear`, `TryGetNodeAt`, `RefreshNodeFrames`), `AccessibilityNode` (`label`, `hint`, `value`, `role`, `state`, `frameGetter`; eventos `invoked`, `focusChanged`, `scrolled`, `dismissed`), `AccessibilityRole` y `AccessibilityState` como dice el diseño. El `UnityPlayer.dll` win64 mono importa `UiaHostProviderFromHwnd`, `UiaRaiseNotificationEvent` y `UiaRaiseStructureChangedEvent`. Editor: `PlayerSettings.SetIcons`/`GetIconSizes(NamedBuildTarget…)`, `PlayerSettings.iOS.SetiPhoneLaunchScreenType`/`SetiPadLaunchScreenType`, `iOSLaunchScreenType.CustomStoryboard`, `requiresFullScreen`, `allowFullscreenSwitch`. Motor: `Screen.mainWindowDisplayInfo`, `MoveMainWindowTo`; sin mínimo de ventana de escritorio. **Ninguna API inventada** |
| `dotnet` (base para AC-POL-05) | `run-tests.ps1`: **3886 en total, 3847 pasan, 39 ignoradas, 0 fallan** |
| Veredicto | **APROBADO CON OBSERVACIONES**: 0 bloqueantes, 9 mayores (todos se corrigen con un apéndice corto al diseño y no cambian el alcance), 14 menores |

## 1. Mayores

| ID | Resp. | Problema | Evidencia | Corrección |
|---|---|---|---|---|
| QA-G14-01 | architect (+po) | AC-APP-16-06 pide que en `RowMate.exe` figuren el producto `RowMate`, la empresa `iguisoft` y la versión de archivo y de producto `1.0.0`, pero el diseño no tiene ningún mecanismo para lograrlo. Unity copia el `.exe` con los recursos del motor | `Builds/Windows/{normal,simulator,dev-smoke}/RowMate.exe`: `FileVersion=6000.3.27.6761814`, `ProductVersion=6000.3.27f1`, `ProductName` y `CompanyName` vacíos, aunque `ProjectSettings` ya tiene RowMate/iguisoft/1.0.0 | Agregar un post-proceso de Windows (`IPostprocessBuildWithReport`) que reescriba el `VS_VERSIONINFO` del `.exe`. En la PC alcanza con `BeginUpdateResource`/`UpdateResource`/`EndUpdateResource`; en el CI de Linux hace falta un escritor PE puro o se saltea con aviso. Lo cubre un test que lea `FileVersionInfo` en `-SmokeTest`. Si no se hace, el PO baja el AC a "título y nombre del `.exe`" y lo anota como desvío |
| QA-G14-02 | architect | DT14-10 dice que `BootSplashView` es "la primera hija de `AppShellView` y queda encima de todas las capas", pero en UI Toolkit el primer hijo se dibuja **debajo** del resto. Así, la capa quedaría tapada por `Tabs`/`Pages` y AC-APP-16-10 falla | `AppShellView.cs:93-107` agrega `Tabs`, `TabBar`, `Pages`, `Dialogs`, `Toasts`, `Popups` y `TopLayer` en ese orden; `HudView.cs:70-73` (D2-05: "el hijo de después se dibuja arriba") | Que sea la **última** hija, agregada después de `TopLayer` y de cualquier capa que se sume más tarde. Si no, `BringToFront()` en cada `Add` del shell mientras viva. `BootSplashTests` tiene que comprobar que en el cuadro 0 el píxel central del shell es el de la capa |
| QA-G14-03 | architect | DT14-30 pone `SceneLoadingIndicator` dentro de `SceneSurface` "para no sumar al tope de 60". Tiene dos problemas. (a) `SceneSurface` está en `display: none` justo mientras la escena **no** está lista, así que el indicador nunca se vería. (b) `SceneSurface` es hija de `HudView`, que vive bajo `#immersive`, y `HudBudgetTests` cuenta todo lo visible bajo `Immersive.Root`: el indicador **sí** suma | `HudView.cs:76-80` y `:328-334`; `HudBudgetTests.cs:13-24`, `:44` | Ponerlo sobre `HudBackdrop` (o en una hermana de `SceneSurface`) y contarlo. Sumar a `HudBudgetTests` el peor caso: rutina con objetivos, `Próximo`, indicador de escena y el fundido +1 de la escena demorada. Tiene que quedar ≤ 60 (hoy 56-57 + 2 o 3). Corregir R14-10 |
| QA-G14-04 | architect | La subclase del `WndProc` desde C# (DT14-14) no restaura el procedimiento original. Al cerrar, el player apaga Mono y la ventana sigue recibiendo mensajes (`WM_DESTROY`, `WM_NCDESTROY`…) hacia un delegado administrado que ya no existe. Hay riesgo de cuelgue o crash al cerrar, y eso choca con "cerrar la ventana cierra la app" (AC-APP-15-05) | DT14-14 y R14-02 no lo mencionan | Restaurar `GWLP_WNDPROC` en `Application.quitting` (y en `OnDestroy` de `AppRoot`), antes del apagado del dominio. Probar en el build: cerrar con la X, con Alt+F4 y estando en pantalla completa, 5 veces, sin crash ni `crash.dmp` |
| QA-G14-05 | architect | DT14-01 promete los mismos bytes en la PC, el CI y Unity, y `AppIconAssetsAreCurrent` corre en `ci-core` con **ubuntu y windows**. Pero `SineStroke` usa `Math.Sin`, que llama a la libm de la plataforma (UCRT o glibc) y puede diferir en 1 ulp. Con 8 × 8 muestras basta un ulp para cambiar una muestra y romper la comparación byte a byte en un solo SO. R14-08 solo cubre el deflate | `ci-core.yml`: `os: [ubuntu-latest, windows-latest]`; DT14-01/-02 | Rasterizar sin funciones trascendentes de la plataforma: un seno propio (polinomio con `+` y `*` en `double`, documentado) o una tabla precalculada versionada. Solo se usan `Math.Sqrt` y `Math.Round`, que son exactos según IEEE. Agregar un test que fije el SHA-256 de `icon-16.png` y `icon-1024.png`: así un cambio de plataforma se ve como falla nombrada |
| QA-G14-06 | architect | El `ScreenReaderBridge` rearma el árbol con `hierarchy.Clear()` + `AddNode` cada vez que está "sucio", y eso incluye cualquier `GeometryChangedEvent` del contenido (hasta 4 por segundo). Al desplazar una lista o con el HUD cambiando, el nodo enfocado desaparece y Narrador/TalkBack vuelven al principio o pierden el foco. Con eso, AC-APP-03-12/-13 quedan inutilizables en la práctica, aunque la prueba del árbol pase | DT14-19, §5 (diagrama) | Separar dos casos. (1) Cambio **estructural** (navegación, diálogo, clase de ventana, filas que entran o salen): rearmar, conservando y reenfocando el nodo del mismo elemento si sigue (`SendLayoutChanged(nodo)`). (2) Cambio solo de **geometría**: `RefreshNodeFrames()`, sin `Clear`. Nodos con clave estable por `VisualElement`. Agregar a `ScreenReaderActionTests` un caso: foco en la fila 5, desplazar la lista y el foco sigue en la fila 5 |
| QA-G14-07 | architect | El humo nuevo `-BootSmoke` (DT14-12) lanza el player 10 veces y no dice con qué aislamiento corre. El arranque normal abre la base, las preferencias y el adaptador BLE (reconexión al último remo). Las reglas exigen no tocar `prefs.json`, las bases del usuario ni la radio. Además, DT14-16 pisa los `-screen-width/-height` explícitos que usan los humos (`build.ps1:216`, `:270`, 800 × 480) | `build.ps1:20`, `:28` (`-ExportSmoke` usa `ROWMATE_DATA_DIR` temporal y `ROWMATE_BLE=off`); DT14-16 | `-BootSmoke` con la misma receta que `-ExportSmoke`: carpeta temporal propia borrada en `finally`, `ROWMATE_DATA_DIR` ahí, `ROWMATE_BLE=off` y build de desarrollo. DT14-16 no reubica la ventana si el player recibió `-screen-width`, `-screen-height` o `-screen-fullscreen` por línea de comandos. Lo prueba `ItFourteenToolingTests` |
| QA-G14-08 | architect | D-01 recorta AC-APP-11-01: no se vuelven a tomar `it4b` a `it8` y se reemplazan por `TextAuditTourTests`. Pero el recorrido (§4) no pasa por el selector de escenario `EnvironmentPicker` (It. 8), los paneles de series (`SeriesPanelView`, It. 5), los avisos centrales y `Próximo:` (`CenterCallouts`) ni `FinishWorkoutDialog`/`StartWorkoutDialog` con sus variantes. Son justamente las pantallas que se dejan de tomar | `Presentation/Workout/{EnvironmentPicker,SeriesPanelView,CenterCallouts,FinishWorkoutDialog,StartWorkoutDialog}.cs`; 02-tech-design §4 | Se acepta D-01 **con condición**: (a) sumar esos estados al recorrido, en `es` y `en`; (b) enganchar `TextAuditProbe` también en `DemoWalkthroughTests`, que ya pasa por los 4 escenarios. Con eso el AC queda cubierto sin volver a tomar las escenas |
| QA-G14-09 | architect | El nuevo `ci-unity.yml` (builds con `unity-builder`, en cada push a `main`) rompe una aserción vigente que el diseño no nombra: "no compila players" (R-01 de la It. 2). R14-12 lo trata en general, y AC-POL-05 prohíbe que baje ninguna cuenta | `ItTwoDebtTests.cs:166-178` (`Does.Not.Contain("unity-builder")`, matriz `simulator: [false, true]` y los pasos `enable`/`restore` con sangría exacta); `ItTwoToolingTests.cs:90-101` (`testMode: [EditMode, PlayMode]`, `workflow_dispatch`) | En WP-B2, reescribir la aserción de R-01 como "los builds van solo en `build-*`, nunca con el define del simulador y con caché de `Library` separada de la de tests" (no borrarla). Mantener el texto literal de la matriz de tests y de los pasos del simulador. Anotar en el REPORT que R-01 queda reemplazada por AC-APP-19-06 |

## 2. Menores (una línea cada una)

- QA-G14-10 (architect, [Apple]): en iPhone, `UnityAppController` implementa `supportedInterfaceOrientationsForWindow`, que manda sobre `Info.plist`. Para que no haya vertical invertida hay que poner además `Screen.autorotateToPortraitUpsideDown = false` en tiempo de ejecución si el equipo es un teléfono.
- QA-G14-11 (architect): nombres en §10. `LocalizationCatalogTests` son en realidad `CatalogParityTests:225-227` y `L10nKeysTests:69` (252 → 256). Los permisos y `VIBRATE` están en `Ble/AndroidManifestTests`, no en `ItTwelveToolingTests`. `AppIdentityTests.ProvisionalIdsAndNoComExample` pasa a "definitivos" (Q-85).
- QA-G14-12 (architect): otras pruebas existentes que hay que actualizar sin bajar la cuenta: `ItFourAToolingTests:30`, `ItFourBToolingTests:35-36` y `ItSixAToolingTests:190` (`ValidateSet` y `$Set = 'it12'` → `it14`), y `ProjectBootstrapper.CurrentVersion` 6 → 7.
- QA-G14-13 (architect): #44. `BleErrorText.ForDisplay` va en la **vista** (`ErrorBanner` y la tarjeta del pulsómetro), no en `HrmCardRules`, porque `HrmCardRulesRealTests:117-124` exige el detalle sin tocar (`scan: error 2`).
- QA-G14-14 (architect): en `WM_GETMINMAXINFO`, llamar primero al `WndProc` original y después pisar solo `ptMinTrackSize`, para no perder el máximo que calcula Unity.
- QA-G14-15 (architect): CI. `CiUnityWorkflowTests` tiene que exigir que `upload-artifact` suba solo la carpeta del player o el `.apk`: nada de `*.log`, `*.keystore` ni `Library` (el *keystore* decodificado por GameCI queda en el *workspace*).
- QA-G14-16 (architect): `.gitattributes`. `*.png` va por LFS: la regla nueva tiene que cubrir también `snapshots/CAP-01.png` y `CAP-02.png` si `AppIconAssetsAreCurrent` las compara, o el test las excluye. Hay que versionar los `.meta` de `Art/Identity`, porque sus GUID los referencia `ProjectSettings` (`androidSplashScreen`).
- QA-G14-17 (designer): A14-08, `letter-spacing` en los rótulos del HUD. Toca una geometría verificada en la 4b: por defecto, solo en las pestañas, y el HUD solo con las sondas en verde.
- QA-G14-18 (architect): AC-DEBT-05 con `glass.fallback` al 86 %. `ExportToastPolishTests` tiene que medir el contraste sobre la composición del peor caso (una línea clara del gráfico detrás), no sobre el color del token.
- QA-G14-19 (po): AC-APP-16-08 pide 24 y 64 en Windows, pero el `.ico` de Unity no los lleva (DT14-05). Se acepta: se generan y se versionan, aunque no van al `.exe`.
- QA-G14-20 (architect): D-02 se acepta. La variante "con capa" tiene que ser el build completo de la 14 (con la reubicación de la ventana y el *bridge*), para que el delta no deje afuera nada de lo que sí agrega la iteración.
- QA-G14-21 (architect): en el HUD con el lector encendido, la `label` de las métricas se actualiza una vez por segundo y la geometría del HUD **no** marca el árbol como sucio (QA-G14-06). Así sostiene los 60 fps de AC-APP-03-15.
- QA-G14-22 (architect): R14-07. El README tiene que decir que la existencia de las imágenes de GameCI `6000.3.27f1` (windows-mono, android) y la activación Personal en el *runner* de macOS se comprueban en la primera corrida real. Si fallan, va a issue.
- QA-G14-23 (architect): `HapticPolicy` (250 ms) puede comerse el `Confirm` de una reconexión si coincide con un cambio de paso. Se acepta, documentado en el test.

## 3. Revisión de reglas

| Tema | Resultado |
|---|---|
| `ProjectSettings` (AC-POL-07) | §2.4 justifica cada campo: íconos de Standalone, Android y la imagen de arranque; `allowFullscreenSwitch` 1 → 0 (DT14-15); `uIRequiresFullScreen` 1 → 0 (multitarea del iPad); arranque de iOS con *storyboard*; bootstrap v7. Los valores "sin cambios" coinciden con el archivo actual (1280 × 720, `resizableWindow` 1, `fullscreenMode` 3, minSdk 26, ARM64, autorrotación 1, las 4 orientaciones, ids, versión, splash `#0D1B2A` y `m_ShowUnitySplashScreen` 0). Ningún define del simulador. `ProjectSettingsGuard.ps1` toma una instantánea por corrida, así que los cambios a propósito no la rompen |
| Secretos en el CI | Los `secrets.*` entran solo por `env:`/`with:`. El job compuerta publica `true`/`false`, sin valores. No hay `pull_request` en `ci-unity` y las acciones están fijadas en `@v4`: **sin fuga**, salvo la observación QA-G14-15 sobre los artefactos |
| Repo público | Los tres documentos no tienen rutas de la PC, usuarios ni datos del equipo. Están en UTF-8 y sin doble codificación |
| Paridad | Nada cambia el Core verificado, salvo `AxisScale` (DV-137, aceptado) e `IHaptics` (se mantiene `Confirm()`). El riesgo está en las vistas que se tocan; AC-POL-05 lo controla con la cuenta base de arriba |
| Accesibilidad | Se basa en API reales de 6.3. El Narrador real y TalkBack quedan como [Device-Win] (demo) y [Device-Android] NV |

## 4. Matriz AC → test (planificada)

| AC | Test del diseño | Gate |
|---|---|---|
| APP-16-06 | `AppIdentityTests`, `ProjectSettingsIt14Tests.NameAndVersion`; QA `.exe` | **QA-G14-01** |
| APP-16-07 | `ProjectSettingsIt14Tests.Identifiers`, `AppIdentityTests` | OK |
| APP-16-08 | `AppIcon*Tests`, `PngEncoder`, `Deflate`, `AppIconAssetsAreCurrent`, `IdentityStepTests`, CAP-01 | QA-G14-05 |
| APP-16-09 | `AdaptiveForegroundInsideSafeZone`, `AndroidPlatformIcons`, CAP-02; NV | OK |
| APP-16-10 | `BootSplashMotionTests`, `BootSplashTests`, CAP-03/04, `-BootSmoke` | QA-G14-02, -07 |
| APP-16-11 | `SplashAndLaunchScreen`, `ApplePostprocessTests`; NV | OK |
| APP-16-12 | `AndroidMinimum` + `AndroidManifestTests`; `aapt2` NV | QA-G14-11 |
| APP-15-05 | `WindowGeometryTests.Initial*`, `WindowsWindowFrameTests`; Device-Win | QA-G14-04, -07 |
| APP-15-06 | `MinTrack*`, `MinWindowLayoutTests`, CAP-05/06 | QA-G14-14 |
| APP-15-07 | `WindowModeStateTests`, `WindowModeTests`; R14-04 | OK |
| APP-15-08 | Demo del usuario | OK |
| APP-15-09 | `Orientations`, `InfoPlistOrientations`, `Mac800x600`; NV | QA-G14-10 |
| APP-15-10 | `ResizeContinuityTests`, VID-02 | OK |
| APP-11-01 | Clasificador, matcher, escaneo estático, `TextAuditTourTests`, sonda en tomas | **QA-G14-08** |
| APP-11-02 | `LegacyFixedTextsTests` | OK |
| APP-11-03 | `CatalogParityTests`, `L10nKeysTests` (256) | QA-G14-11 |
| APP-03-12 | `A11y*Tests`, `ScreenReaderTreeTests`, CAP-27 | OK |
| APP-03-13 | `ScreenReaderActionTests` | **QA-G14-06** |
| APP-03-14 | `A11yAnnouncePolicyTests`, `ScreenReaderAnnouncementTests` | OK |
| APP-03-15 | `ScreenReaderCostTests`, `HudBudgetTests` | QA-G14-21 |
| POL-01 | `TransitionInventoryTests`, CAP-24, VID-01 | OK |
| POL-02 | `TabularAuditTests`, `FontAuditTests` | OK |
| POL-03 | `HapticPolicyTests`, `WorkoutHapticsTests`, `HapticEventsTests`, `check.ps1`; NV | OK |
| POL-04 | `CompletionDialogScaleTests`, CAP-13 | OK |
| POL-05 | `DemoWalkthroughTests` + cuentas ≥ base (dotnet 3886/3847/39) | QA-G14-09, -12 |
| POL-06 | QA Device-Win | OK |
| POL-07 | `ProjectSettingsIt14Tests`, `SimulatorInUnityTests`, guardia, diff | OK |
| APP-19-06 | `CiUnityWorkflowTests.BuildsAndArtifacts` | QA-G14-15 |
| APP-19-07 | `GateSkipsWithWarning` | OK |
| APP-19-08 | `AppleManualUnsigned`; NV | OK |
| APP-19-09 | `SecretsOnlyInEnvOrWith`, `RepoHygieneTests`, `DebugSignedSuffix` | OK |
| APP-19-10 | `WorkflowYamlTests` + `MiniYamlTests` (en `ci-core` vía `run-tests.ps1`) | OK |
| APP-19-11 | `ItFourteenToolingTests.ReadmeCiSection` | OK |
| NV-01 | Lista en el REPORT | OK |
| DEBT-01 a -10 | `LinkBannerName`, `SearchingButton`, `DeviceLandscapeScroll`, `BleErrorText` + `ScanErrorText`, `ToastClock`/`PathBreak` + `ExportToastPolish`, `HistoryScale`, `AxisCrowding`, `ChartArt`, `SceneLoadingSpecs` + `SceneLoading`, `DeviceStoreReconnectHaptic` | QA-G14-03 (DEBT-09), -13 (DEBT-04), -18 (DEBT-05) |

Desvíos D-01 (con la condición de QA-G14-08), D-02, D-03 y D-04: **aceptados**.

## 5. Para issues

- PE `VS_VERSIONINFO` del `.exe` compilado en Linux (si QA-G14-01 se resuelve solo en la PC).
- Mapeo propio a UIA si Narrador no lee algún rol en la demo (ya está en el §13 del diseño).
