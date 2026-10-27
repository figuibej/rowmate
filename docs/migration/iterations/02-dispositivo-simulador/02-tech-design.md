# It. 2 — Shell, Dispositivo en vivo y simulador: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-27 |
| Iteración | 2 · `02-dispositivo-simulador` ([ROADMAP §It. 2](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (125 AC, 15 LT, 25 CAP + 3 VID), [00-deuda-it1](00-deuda-it1.md), [00-deuda-diseno](00-deuda-diseno.md), [00-primer-open](00-primer-open.md), [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §4-§8, §12-§13, §15-§16, ADR 002-007, 011, 013, 014, 016, [ART_BIBLE](../../design/ART_BIBLE.md) §2-§5, §10, [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §1-§2, [ASSET_SLOTS](../../design/ASSET_SLOTS.md) §3-§4 |
| Legacy leído (solo lectura, `3ae0f3a`) | `lib/main.dart`, `lib/core/dev/{dev_config,rowing_simulator,simulator_overlay}.dart`, `lib/core/bluetooth/{simulated_ble_service,simulated_heart_rate_service,ble_service,heart_rate_service}.dart` (estas dos, solo los tipos, `startScan`, `disconnect` y `_onMeasurement`), `lib/features/device/*.dart`, `lib/features/workout/scene/scene_settings.dart:1-40`, `lib/shared/theme.dart:1-45`, títulos de `workout_screen.dart:53`, `routines_screen.dart:16`, `history_screen.dart:21`, `profile_screen.dart:18`, `windows/runner/main.cpp:25-35` y los 5 archivos de test del §6 de la spec. No se leyó `strava_secrets.dart`. `git status --porcelain` vacío al empezar y al terminar |
| Verificación previa | **Spike S-0** en un proyecto Unity descartable del scratchpad (fuera del repo), Unity 6000.3.27f1, batchmode: render de UI Toolkit a PNG, medición de R-T1 en RT y en un player real de Windows, `forceGammaRendering`, `MeshGenerationContext.DrawText`, `MediaEncoder` y `FontAsset` persistido usado desde USS (Anexo A). El repo no se tocó |
| Estado | Diseño **corregido por el gate de armonía de QA** ([03-qa-harmony](03-qa-harmony.md), APROBADO CON OBSERVACIONES) y con [04-design-notes](04-design-notes.md) integrado (§8). Sin código. Sigue: **WP-D0** (Designer, tokens) → WP-A. Cambios al final ("Cambios por gate QA") |
| Corrección del gate | 2026-10-27. Entradas nuevas: 03-qa-harmony (14:42), 04-design-notes (14:16 y su corrección por el gate de las 15:02, §14 de las notas), 01-po-spec corregida por el gate (14:56: §11.1 DV-01 a DV-08, §16; AC-BLE-11-07, AC-BLE-11-06, AC-APP-06-01, AC-HRM-13-06, CAP-04a/b). Legacy releído: `device_screen.dart:80-136`, `:390-445`; `heart_rate_card.dart:1-238`; `main.dart:85-112`; `simulator_overlay.dart:20-100`; `test/heart_rate_card_test.dart:1-82`, `:155-200`; `android/app/src/main/res/values/styles.xml`; SDK local de Flutter `material/app_bar.dart:1191`, `material/navigation_bar.dart:291`, `widgets/banner.dart:18-30`. Sondas propias en Unity 6000.3.27f1 sobre un proyecto descartable del scratchpad (`…/scratchpad/archfix-g2`, nunca el repo; §8.3) |

Convenciones: rutas relativas a `Assets/RowMate/` salvo que empiecen con `tools/`, `docs/` o `ProjectSettings/`. "Dueño" = paquete de trabajo que crea y mantiene el archivo (§6). `dp` = px de USS (1 px de USS = 1 dp, ARCHITECTURE §7.3). Identificadores en inglés, comentarios y textos de diseño en español.

---

## 0. Resumen

1. **Lógica pura primero (WP-A, sin Unity).** Se portan `SimulatedBleService` → `SimulatedRowerService` y `SimulatedHeartRateService` con `IScheduler` (reloj falso en tests), los *providers* → `DeviceStore` y `HeartRateStore`, y toda la lógica de las pantallas como **reglas puras con textos ya resueltos** (`DeviceScreenRules`, `HrmCardRules`, `ShellStore`, `SimulatorPanelStore`). Así 9 de los 15 casos legacy y casi todas las reglas (columnas, PULSO, botones habilitados, topes del panel, hora, alto máximo) se verifican con `dotnet`, y las vistas quedan delgadas.
2. **Composición única.** `AppComposer` (App, puro) arma los *stores* desde un `DeviceBackend`; `ShellHost` (Bootstrap) arma la UI sobre cualquier `VisualElement` raíz. Lo usan **igual** el `AppRoot` de la escena Boot, las pruebas PlayMode y el arnés de capturas: las pantallas nunca saben si los servicios son simulados, reales o de prueba (R-06, AC-BLE-13-04). El swap simulador/hardware es un `#if ROWMATE_SIMULATOR` en un solo archivo (`DeviceBackendFactory`).
3. **UI Toolkit construida en C#, estilada por USS.** Las vistas crean sus elementos en código con **nombres y clases de contrato** (§3.12); todo el aspecto sale de un tema (`RowMateTheme.tss`) que importa `Tokens.uss` y `Typography.uss` (generados desde `Tokens.cs`), `Base.uss` (WP-B: estructura y colores de contrato) y `Skin.uss`/`Motion.uss` (WP-D: pulido). Sin UXML en esta iteración (D-02): no hay referencias a assets que mantener y el arnés y los tests construyen exactamente la misma UI.
4. **R-T1 ya está medido (spike S-0): Unity compone la UI en espacio lineal.** El vidrio sobre blanco da `#6A6A6C` y `text.secondary` sobre `surface.card` da `#DADBDC`, tanto en una RT sRGB como en el *backbuffer* de un player real de Windows. Con la **opción A** del Designer (`PanelSettings.forceGammaRendering = true` sobre una RT `R8G8B8A8_UNorm`) da `#2A2F37` y `#BBC1C8`: la composición del legacy. En *Screen Space Overlay* `forceGammaRendering` **no tiene efecto** (medido en el player). Propuesta D-01: la app renderiza su panel en esa RT y la presenta en pantalla (`UiSurface`, §3.1), con respaldo al modo *Overlay* si la verificación S-B1 falla. Así las capturas de la It. 2 salen con la composición del legacy y no hay que repetirlas antes de la 4a.
5. **Arnés de capturas por `-executeMethod`, en modo edición, sin entrar a Play.** Verificado en S-0: un `UIDocument` con `PanelSettings.targetTexture` se renderiza en batchmode en cada `EditorApplication.update`, la escala `ConstantPixelSize = densidad` da exactamente los dp pedidos (1080×2400 px a 2,625 → 411,43×914,29 dp) y `UnityEditor.Media.MediaEncoder` escribe MP4. El arnés arma cada estado con reloj falso y dobles de prueba, lee la RT, recorta, mide píxeles (colores de contrato, R-T1) y audita desbordes. Salida en `docs/…/02-dispositivo-simulador/snapshots/` (§4).
6. **Motion manejado por código y por un reloj inyectable** (`MotionDriver` + `IClock`): rodillo de dígitos, latido, píldora de la barra y transiciones de pestaña. Las mismas animaciones corren a la frecuencia de la pantalla en la app y cuadro a cuadro, deterministas, en los tests y en los videos. Las transiciones de USS quedan para micro-interacciones (hover, presión, foco).
7. **Cuatro paquetes en orden, con archivos disjuntos:** WP-D0 tokens nuevos del Designer (5 tokens en `Tokens.cs`, §6.1); WP-A lógica pura + tests dotnet (≈ 80 archivos); WP-B Unity: plataforma, UI, composición, escena Boot, bootstrapper v3, arnés, herramientas de build y tests de Unity (≈ 115 archivos + assets generados, en tres entregas); WP-D pulido visual del Designer sobre las clases de §3.12, iterando con el arnés (≈ 6 archivos).
8. **Gate de QA** (§8 y "Cambios por gate QA"): `AppRoot` arma la UI en `Start` (en `Awake` la raíz del `UIDocument` es nula), una sola regla de área segura, toques con *hit-test*, `UiTestHost` con fábrica de *backend*, `letter-spacing` en la unidad real de UI Toolkit y los 25 pedidos de 04-design-notes aceptados o rechazados uno por uno.

| Pregunta de la tarea | Dónde |
|---|---|
| Árbol, asmdefs, escena Boot, AppRoot, swap | §1 |
| Lógica pura con firmas y fuente legacy | §2 |
| UI: shell, Dispositivo, tarjeta, cinta, panel, placeholders, USS, binding, transiciones, idioma, wakelock | §3 |
| Arnés de capturas y R-T1 | §4 |
| Tests, matriz AC → test/captura, los 15 casos legacy | §5 |
| Paquetes de trabajo y contrato WP-B ↔ WP-D | §6 y §3.12 |
| Riesgos y desvíos | §7 |
| Pedidos de 04-design-notes (DN-01 a DN-25): aceptados, rechazados y dónde quedan | §8 |

---

## 1. Estructura: archivos, asmdefs, escena Boot y composición

### 1.1 Árbol de archivos

Marcas: **N** nuevo · **C** cambia · **G** generado (por herramienta o por Unity; se versiona) · **V** vacío con encabezado de dueño (lo crea WP-B para que el tema compile, el contenido es de WP-D).

#### Core (`RowMate.Core`, puro) — WP-A

| Archivo | Marca | Contenido |
|---|---|---|
| `Core/Common/IEventSource.cs` | N | `IEventSource<T>`: solo `Subscribe(Action<T>)`; lo que exponen las interfaces de servicios (que nadie de afuera pueda `Emit`) |
| `Core/Common/EventStream.cs` | C | implementa `IEventSource<T>` (sin cambiar su semántica ni su costo cero al emitir) |
| `Core/Common/DerivedObservable.cs` | N | `DerivedObservable<TIn, TOut> : IReadOnlyObservable<TOut>, IDisposable` (mapea un observable: idioma → `Localizer`) |
| `Core/Common/RealtimeTimerQueue.cs` | N | cola de timers de tiempo real que usa `UnityScheduler`; pura (el tiempo lo pasa quien la avanza), con la política de reanclado tras un corte del *loop* (§2.1, QA-G2-15) |
| `Core/Ble/BleStatus.cs` | N | `enum BleStatus { Off, Scanning, Connecting, Connected, Disconnected }` (`ble_service.dart:8`) |
| `Core/Ble/BleAdapterState.cs` | N | `enum BleAdapterState { Unknown, Unsupported, Unauthorized, Off, TurningOn, On, TurningOff }` (ARCHITECTURE §10.2) |
| `Core/Ble/HrmStatus.cs` | N | `enum HrmStatus { Scanning, Connecting, Connected, Disconnected }` (`heart_rate_service.dart:10`) |
| `Core/Ble/HrmIncompatibleException.cs` | N | `heart_rate_service.dart:20-24` |
| `Core/Ble/IRowerService.cs` | N | puerto del remo (§2.1) |
| `Core/Ble/IHeartRateService.cs` | N | puerto del pulsómetro (§2.1) |
| `Core/Ble/HeartRateContact.cs` | N | regla de contacto `heart_rate_service.dart:321-326` (AC-HRM-04-02) |
| `Core/Ble/Unavailable/UnavailableRowerService.cs` | N | remo del build normal de la It. 2: equipo sin BLE (P-04, AC-BLE-13-05) |
| `Core/Ble/Unavailable/UnavailableHeartRateService.cs` | N | ídem pulsómetro |
| `Core/Models/MetricKind.cs` | N | `Split, Spm, Watts, Distance, HeartRate, Time, Calories` |
| `Core/Models/MetricPalette.cs` | N | métrica → `ColorToken` (F-APP-06; pendiente D-09 de la It. 1) |
| `Core/Design/Icons.cs` | N | 42 + 7 codepoints de ASSET_SLOTS §4.3 (pendiente D-09 de la It. 1) |
| `Core/Design/FontAssetPaths.cs` | N | rutas de los `FontAsset` generados (una sola fuente para el generador de USS y el bootstrapper) |
| `Core/Design/TypographyUssWriter.cs` | N | genera `Typography.uss` (clases `.rm-type-*`, `.rm-icon`, escalas de letra) |
| `Core/Design/TextScaleSteps.cs` | N | escalas de letra cuantizadas 0,85 / 1 / 1,15 / 1,3 (P-03) |
| `Core/Design/Easing.cs` | N | evaluación de `EasingToken` (cubic-bezier) |
| `Core/Design/CriticalSpring.cs` | N | resorte críticamente amortiguado (ANIMATION_BIBLE §1.2) |
| `Core/Design/MotionSpecs.cs` | N | números de motion que no son tokens: ANIMATION_BIBLE §2.1, §2.4, §2.9 y 04-design-notes §5 (DN-07, -08, -10, -11, -12, -14, -15, -16; §2.9) |
| `Core/Design/DigitRoll.cs` | N | plan y curvas por celda del rodillo de dígitos (ANIMATION_BIBLE §2.4, 04-design-notes §5.1, DN-08) |
| `Core/Design/HeartbeatClock.cs` | N | latido a 60/bpm: suma de dos golpes, compresión sobre 142,9 bpm y parada en 120 ms (ANIMATION_BIBLE §2.9, 04-design-notes §5.3, DN-10, AC-HRM-13-12) |
| `Core/Design/AccentPulse.cs` | N | factor `k` del destello del acento (1 → 0,60 en 300 ms con `ease.pulse`, 04-design-notes §5.2, DN-07) |
| `Core/Design/FramePacingPolicy.cs` | N | 60/30 fps por demandas (ARCHITECTURE §13.1) |
| `Core/Ports/IAccessibilitySettings.cs`, `AccessibilitySnapshot.cs` | N | escala de letra y reducir movimiento, observables (ARCHITECTURE §13.2) |
| `Core/Ports/IDisplayMetrics.cs`, `SafeInsets.cs` | N | densidad dp → px y área segura del sistema (§3.1, D-03) |
| `Core/Ports/IBackButtonPolicy.cs` | N | qué hace "Atrás" en la raíz del shell (lo implementa Platform; lo llama `ShellHandle.HandleBack`, §3.10, QA-G2-17) |
| `Core/Text/ParitySmoke.cs` | N | tabla de AC-BASE-01 ejecutable en un player (prueba de humo IL2CPP, §5.1) |

#### App (`RowMate.App`, puro) — WP-A

| Archivo | Marca | Contenido |
|---|---|---|
| `App/Device/DeviceState.cs` | N | estado inmutable del remo para la pantalla |
| `App/Device/DeviceStore.cs` | N | puerto de `device_provider.dart` (sin vigilante ni diagnóstico: It. 10a) |
| `App/Device/DeviceScreenRules.cs` | N | vista, grilla, tarjetas, vista de búsqueda (`device_screen.dart`) |
| `App/Device/DeviceGridSpec.cs`, `MetricTileSpec.cs`, `ScanViewSpec.cs`, `DeviceViewKind.cs` | N | datos de las reglas |
| `App/Device/HeartRateState.cs` | N | estado inmutable del pulsómetro |
| `App/Device/HeartRateStore.cs` | N | puerto de `heart_rate_provider.dart` |
| `App/Device/HrmCardRules.cs`, `HrmCardViewModel.cs`, `HrmCardBody.cs`, `HrmDeviceRow.cs` | N | tarjeta del pulsómetro (`heart_rate_card.dart`) |
| `App/Shell/TabDestinationId.cs`, `TabDestination.cs`, `ShellOptions.cs`, `ShellStore.cs` | N | destinos y selección (`main.dart:116-181`) |
| `App/Scene/SceneSettings.cs` | N | solo `HourOverride` en la It. 2 (`scene_settings.dart:14-24`); el escenario persistido llega en la 4a |
| `App/Composition/DeviceBackend.cs` | N | par remo + pulsómetro y su dueño (`IDisposable`) |
| `App/Composition/AppServices.cs`, `AppContext.cs`, `AppComposer.cs` | N | composición pura (§2.8) |

#### Simulator (`RowMate.Simulator`, puro, solo con `ROWMATE_SIMULATOR`) — WP-A

| Archivo | Marca | Contenido |
|---|---|---|
| `Simulator/SimulatedRowerService.cs` | N | puerto de `simulated_ble_service.dart` |
| `Simulator/SimulatedHeartRateService.cs` | N | puerto de `simulated_heart_rate_service.dart` |
| `Simulator/SimulatorBackend.cs` | N | crea el `RowingSimulator` compartido y los dos servicios (`main.dart:51-54`) |
| `Simulator/SimulatorTexts.cs` | N | textos fijos en español (Q-03) |
| `Simulator/Panel/SimulatorPanelStore.cs`, `SimulatorPanelState.cs`, `SimulatorPreset.cs`, `SimulatorPanelLayout.cs` | N | lógica del panel (`simulator_overlay.dart`) |

#### Tests compartidos dotnet + Unity — WP-A

| Archivo | Marca | Contenido |
|---|---|---|
| `Tests/Support/ControllableRowerService.cs` | N | "remo de prueba" (= `_ControllableBleService`, `heart_rate_card_test.dart:25-48`), con lecturas a pedido |
| `Tests/Support/ScriptedHeartRateService.cs` | N | "pulsómetro de prueba" con estado, reintento, recordado y errores a pedido (§0.4 de la spec) |
| `Tests/Support/ThrowingHeartRateService.cs` | N | `_ThrowingHrm` 1 a 1 (`heart_rate_provider_test.dart:11-51`) |
| `Tests/Support/FakeAccessibilitySettings.cs`, `FakeSystemLocale.cs`, `FakeDisplayMetrics.cs`, `StateRecorder.cs` | N | idioma, letra, movimiento, densidad y **área segura** de prueba (`FakeDisplayMetrics(density, SafeInsets)`, QA-G2-06); grabador de secuencias de un observable |
| `Tests/Simulator/SimulatedBleServiceTests.cs` | N | LT-20, LT-21, LT-22 + AC del remo simulado |
| `Tests/Simulator/SimulatedHeartRateServiceTests.cs` | N | LT-23, LT-24, LT-25 + AC del pulsómetro simulado |
| `Tests/Simulator/HeartRateProviderTests.cs` | N | LT-32, LT-33, LT-34 |
| `Tests/Simulator/SimulatorPanelStoreTests.cs` | N | reglas del panel |
| `Tests/Simulator/SimulatorBackendTests.cs` | N | composición por variante, atleta compartido, 142 a los 60 s, sin timers al cerrar |
| `Tests/Simulator/DeviceSimulatorFlowTests.cs` | N | tabla de AC-BLE-11-10, física que no se reinicia, caída vista desde Dispositivo |
| `Tests/EditMode/Device/{DeviceStoreTests,DeviceScreenRulesTests,HeartRateStoreTests,HrmCardRulesTests}.cs` | N | reglas de Dispositivo y de la tarjeta |
| `Tests/EditMode/Shell/ShellStoreTests.cs` | N | destinos, selección |
| `Tests/EditMode/Composition/AppComposerTests.cs` | N | arranque, `[HRM] autoConnect`, cierre limpio |
| `Tests/EditMode/Ble/{UnavailableServicesTests,HeartRateContactTests}.cs` | N | build normal sin BLE; regla de contacto |
| `Tests/EditMode/Design/{IconsTests,MetricPaletteTests,ColorSourceTests,DigitRollTests,HeartbeatClockTests,AccentPulseTests,CriticalSpringTests,EasingTests,TextScaleStepsTests,FramePacingPolicyTests,TypographyUssSyncTests}.cs` | N | diseño y motion puros |
| `Tests/EditMode/Common/RealtimeTimerQueueTests.cs` | N | orden, cancelación, `Delay` y reanclado tras un salto de 5 s (QA-G2-15) |
| `Tests/EditMode/Project/{LegacyDiscardTests,SnapshotEvidenceTests}.cs` | N | AC-APP-08-01; lectura del `manifest.json` del arnés (se ignora si no hay capturas) |
| `Tests/EditMode/Text/ParitySmokeTests.cs` | N | la tabla de humo da 0 diferencias en .NET 8 |
| `Tests/EditMode/Traceability/It02Ids.cs` | N | 125 AC + 15 LT con su ubicación esperada; además `UnityOnlyIds` (los que solo tienen verificación [p], [ps], [u], [cap] o [eq]) como literales, que lee `run-tests.ps1 -Matrix` (QA-G2-16) |
| `Tests/EditMode/Traceability/TraceabilityTests.cs` | C | suma It. 2; mira también `Tests/PlayMode*` y `Tests/UnityEditMode` por texto (si la carpeta todavía no existe, esas filas se ignoran con motivo) |

#### Herramientas y generados de WP-A

| Archivo | Marca | Contenido |
|---|---|---|
| `tools/tokens/Program.cs` | C | además de `Tokens.uss`, escribe `Typography.uss` con `TypographyUssWriter` (`--check` verifica los dos) |
| `Presentation/Styles/Typography.uss` | G | generado (encabezado "GENERADO, no editar"); se regenera cuando el Designer cambia `Tokens.cs` |
| `tools/dotnet/run-tests.ps1` | C | **pasa de WP-B a WP-A** (QA-G2-16): `-Matrix` lee todos los `It0*Ids.cs`, marca `PENDIENTE UNITY` los `UnityOnlyIds` sin resultado y suma, si se le pasa `-UnityResults <carpeta>`, los XML de NUnit que escribe Unity (mismo formato) |

#### Platform (`RowMate.Platform`, Unity) — WP-B

| Archivo | Contenido |
|---|---|
| `Platform/Time/UnityScheduler.cs` | `IScheduler` delgado: `Stopwatch` (monótono) + `RealtimeTimerQueue` (Core), avanza en `Tick()` desde `AppRoot.Update`; a lo sumo un disparo periódico por cuadro y, si el atraso pasa de un período, **reancla** en `ahora + período` (QA-G2-15); `IsCatchingUp = false` (el alcance llega en la 4a, ADR 017) |
| `Platform/Logging/UnityAppLogger.cs` | `IAppLogger` → `Debug.Log/LogWarning/LogError` con `"[TAG] mensaje"` |
| `Platform/Locale/SystemLocaleProvider.cs` (+ `WindowsUiLanguages.cs`, `AndroidLocaleList.cs`) | `ISystemLocale`: Windows `GetUserPreferredUILanguages` (P/Invoke `kernel32`), Android `LocaleList.getDefault().toLanguageTags()` (JNI), resto `Application.systemLanguage`; en el Editor, override por menú |
| `Platform/Accessibility/AccessibilitySettingsProvider.cs` (+ `WindowsAccessibility.cs`, `AndroidAccessibility.cs`) | `IAccessibilitySettings`: Android `animator_duration_scale == 0` y `Configuration.fontScale` (JNI); Windows `SPI_GETCLIENTAREAANIMATION` y `TextScaleFactor` del registro; Editor: menús `RowMate/Simular …` |
| `Platform/Display/DisplayMetricsProvider.cs` | `IDisplayMetrics`: Android `DisplayMetrics.density` (JNI); Windows `GetDpiForWindow(GetActiveWindow()) / 96` con respaldo `Screen.dpi / 96`; Editor `Screen.dpi / 96` |
| `Platform/Input/BackButtonPolicy.cs` | `IBackButtonPolicy`. Android: "Atrás" en la raíz = `moveTaskToBack(true)` en API ≥ 31 y `Application.Quit()` en < 31 (D-09); escritorio y Editor: no hace nada |
| `Platform/PlatformServices.cs`, `Platform/PlatformFactory.cs` | agrupa lo anterior por plataforma |

#### Presentation (`RowMate.Presentation`, Unity) — WP-B salvo lo marcado

| Archivo | Dueño | Contenido |
|---|---|---|
| `Presentation/Surface/UiColorPipeline.cs`, `UiSurface.cs`, `PanelScaleController.cs` | B | RT gamma (limpia a `bg.app` al crearla), escala por densidad, área segura, redimensionado (§3.1) |
| `Presentation/Surface/OverlayCopyPresenter.cs`, `BlitPresenter.cs` | B | las dos formas de llevar la RT a la pantalla; S-B1 elige una (§3.1) |
| `Presentation/Shell/AppShellView.cs`, `TabBarView.cs`, `TabHost.cs`, `AppBarView.cs`, `ShellLayout.cs`, `BackNavigation.cs` | B | §3.2; `ShellLayout` aplica la regla única de área segura (§3.1, QA-G2-06); `BackNavigation` = "Atrás" en la raíz (§3.10) |
| `Presentation/Device/DeviceScreenView.cs`, `ConnectedView.cs`, `ScanView.cs`, `MetricGridView.cs`, `MetricTileView.cs`, `DeviceHeaderView.cs` | B | §3.3 |
| `Presentation/Device/HeartRateCardView.cs` | B | §3.4 |
| `Presentation/Placeholder/PlaceholderScreenView.cs` | B | §3.6 |
| `Presentation/Controls/TabularNumber.cs`, `FitBox.cs`, `IconView.cs`, `RmButton.cs`, `IconButton.cs`, `SwitchToggle.cs`, `Spinner.cs`, `TextClamp.cs`, `FocusHighlight.cs`, `VerticalFadeTexture.cs` | B | §3.7 (`IconButton` DN-18, `SwitchToggle` DN-17, `VerticalFadeTexture` DN-06) |
| `Presentation/Binding/ViewBinding.cs` | B | binding manual (§3.8) |
| `Presentation/Motion/MotionDriver.cs`, `IMotion.cs`, `TabTransition.cs`, `ViewSwap.cs`, `HeartbeatAnimator.cs`, `AccentFlash.cs`, `HeightTween.cs`, `PopTransition.cs`, `FramePacer.cs`, `MotionSettings.cs` | B | §3.9 (`ViewSwap` DN-12, `PopTransition` llave y panel DN-14) |
| `Presentation/Diagnostics/LayoutAudit.cs` | B | detector de desbordes y textos cortados con contenedores explícitos (tests y arnés, §5.1, QA-G2-14) |
| `Presentation/Styles/RowMateTheme.tss` | B | tema: importa todo en orden (§3.12) |
| `Presentation/Styles/Base.uss` | B | estructura y valores de contrato |
| `Presentation/Styles/Skin.uss`, `Motion.uss` | **D** (V) | pulido visual y transiciones USS |
| `Presentation/Styles/Tokens.uss`, `Typography.uss` | G | generados por `tools/tokens` |
| `Presentation/AssemblyInfo.cs` | C | `InternalsVisibleTo` de los assemblies de test de Unity y del arnés |

#### Simulator.Presentation (`RowMate.Simulator.Presentation`, Unity, con el define) — WP-B salvo lo marcado

| Archivo | Dueño | Contenido |
|---|---|---|
| `SimulatorPresentation/SimulatorLayerView.cs` | B | capa superior: cinta, llave y panel (§3.5); con `store == null`, solo cinta y llave (capturas "Prueba", QA-G2-05) |
| `SimulatorPresentation/SimulatorRibbon.cs`, `SimulatorPanelView.cs` | B | |
| `SimulatorPresentation/Styles/SimulatorBase.uss` | B | estructura y contrato |
| `SimulatorPresentation/Styles/SimulatorSkin.uss` | **D** (V) | aspecto del panel y la cinta |

#### Bootstrap (`RowMate.Bootstrap`, Unity) — WP-B

| Archivo | Contenido |
|---|---|
| `Bootstrap/AppRoot.cs` | composition root de la escena Boot (§1.4): configura en `Awake`, arma la UI en `Start` (QA-G2-01) |
| `Bootstrap/ShellHost.cs`, `ShellHostOptions.cs`, `ShellHandle.cs` | arma la UI sobre una raíz dada; lo usan AppRoot, PlayMode y el arnés |
| `Bootstrap/DeviceBackendFactory.cs` | **el único `#if ROWMATE_SIMULATOR`** que elige la variante de la app |
| `Bootstrap/HardwareBackend.cs` | It. 2: `Unavailable*` (P-04). En la 10a/11, `RowerService`/`HeartRateService` reales. Público: lo usa también `UiTestHost` |
| `Bootstrap/SimulatorLayerFactory.cs` | `#if ROWMATE_SIMULATOR`: `CreateOrNull(backend, app, clock)` crea `SimulatorLayerView` completa si el *backend* es `SimulatorDeviceBackend` (si no, `null`); `CreateChromeOnly()` crea solo cinta y llave (lo usa `UiTestHost`, QA-G2-05) |
| `Bootstrap/UiAssets.cs` | `ScriptableObject` con las referencias que necesita un player: `PanelSettings`, catálogos `TextAsset`, material del presentador |
| `Bootstrap/BuildFlags.cs` | sin cambios |

#### Editor (`RowMate.Editor`, namespace `RowMate.EditorTools`) — WP-B

| Archivo | Marca | Contenido |
|---|---|---|
| `Editor/Bootstrap/ProjectBootstrapper.cs` | C | `CurrentVersion = 3`, pasos nuevos (§1.3) |
| `Editor/Bootstrap/FontAssetBuilder.cs` | N | crea los 9 `FontAsset` y reimporta los USS que los nombran |
| `Editor/Bootstrap/UiAssetsBuilder.cs` | N | `PanelSettings`, `UiAssets`, material del presentador |
| `Editor/Bootstrap/BootSceneBuilder.cs` | N | crea o repara `Scenes/Boot.unity` y la registra en Build Settings |
| `Editor/Bootstrap/PlayerSettingsStep.cs` | C | ventana 1280×720 redimensionable, sin *splash* del motor, fondo `#0D1B2A`; Android: barras del sistema visibles como el legacy (`startInFullscreen = false`, `renderOutsideSafeArea = true`, QA-G2-06) |
| `Editor/Bootstrap/UrpProvisioner.cs` | C | mueve los dos assets de URP de la raíz a `Settings/URP/` (00-primer-open §8.3) |
| `Editor/Build/BuildCli.cs`, `BuildRequest.cs` | N | `-executeMethod RowMate.EditorTools.BuildCli.Build` (ARCHITECTURE §19), backend Mono temporal (D-16) |
| `Editor/Build/BuildOutputInspector.cs` | N | después de un build **normal**, busca rastros del simulador en la carpeta de datos (AC-BLE-13-02, QA-G2-17); `BuildCli` falla si encuentra alguno |
| `Editor/Simulator/SimulatorBuildGuard.cs` | C | expone `Evaluate(...)` puro para el test de AC-BLE-13-01 |
| `Editor/Menus/DeveloperMenus.cs` | N | `RowMate/Simular reducir movimiento`, `…/letra 1,3×`, `…/idioma` (EditorPrefs, solo Editor) |

#### Arnés y soporte de UI (Unity, solo con tests) — WP-B

| Archivo | Contenido |
|---|---|
| `Tests/UiSupport/RowMate.Tests.UiSupport.asmdef` + `UiTestHost.cs`, `HostBackends.cs`, `UiQuery.cs`, `DeviceProfile.cs`, `RtReader.cs`, `PixelProbe.cs` | host de UI determinista en una RT (lo comparten PlayMode y el arnés); `HostBackends` = fábricas de *backend* de prueba (QA-G2-05) |
| `Snapshots/RowMate.Snapshots.asmdef` + `SnapshotCli.cs`, `SnapshotRunner.cs`, `SnapshotScenario.cs`, `SnapshotCatalog.cs`, `ScenarioScript.cs`, `VideoRecorder.cs`, `SnapshotManifest.cs`, `Rt1Measurement.cs` | arnés de capturas (§4) |
| `Tests/PlayMode/{ShellTests,DeviceScreenTests,HeartRateCardStatesTests,ThemeTests,NormalBuildTests,BootSceneTests,SafeAreaTests}.cs` | UI sin simulador (remo y pulsómetro de prueba); arranque real de `Boot.unity` (QA-G2-01); área segura (QA-G2-06) |
| `Tests/PlayModeSimulator/RowMate.Tests.PlayMode.Simulator.asmdef` + `{HeartRateCardTests,SimulatorOverlayTests,DeviceSimulatorTests,LanguageOnScreenTests}.cs` | UI con los servicios simulados (LT-26 a LT-31) |
| `Tests/UnityEditMode/RowMate.Tests.UnityEditMode.asmdef` + `{SimulatorModeTests,BootstrapV3Tests,FontAndIconAssetsTests,SystemLocaleProviderTests}.cs` | EditMode que necesitan `UnityEditor` o la plataforma real (no compilan en dotnet) |
| `Tests/PlayMode/RowMate.Tests.PlayMode.asmdef` | C (referencias, §1.2) |
| `Tests/EditMode/Project/AsmdefLintTests.cs` | C: grafo con los 4 asmdefs nuevos y sus reglas |
| `Tests/EditMode/Project/ItTwoToolingTests.cs` | N: revisión estática de `capture-snapshots.ps1`, `build.ps1` y `ci-unity.yml` (ASCII, `finally`, sin define en distribución, el build normal pasa por `BuildOutputInspector`) |

#### Herramientas y CI — WP-B

| Archivo | Contenido |
|---|---|
| `tools/unity/capture-snapshots.ps1` | activa el define (`SimulatorDefine.EnableForTests`), corre el arnés y **siempre** restaura (mismo patrón que `run-unity-tests.ps1 -Simulator`) |
| `tools/unity/capture-window.ps1` | opcional para QA: abre un build, espera, captura la ventana entera con su título (CAP-22) y la cierra |
| `tools/build/build.ps1` | `-Target Windows|Android [-Simulator] [-Backend Mono|IL2CPP] [-SmokeTest]`; con `-Simulator` fuerza *Development*; sin `-Simulator`, el build falla si `BuildOutputInspector` encuentra rastros del simulador |
| `.github/workflows/ci-unity.yml` | opcional (AC-APP-19-05): EditMode y PlayMode con GameCI, solo si hay secretos |
| `.gitignore` | C: `/Builds/` |

#### Generados por Unity o por el bootstrapper (se versionan con su `.meta`) — WP-B

`UI/Fonts/Generated/{Inter-Regular,Inter-Medium,Inter-SemiBold,Inter-Bold,BarlowSemiCondensed-SemiBold,BarlowSemiCondensed-Bold,BarlowSemiCondensed-ExtraBold}-SDF.asset`, `UI/Icons/Generated/RowMateIcons-{Outline,Filled}-SDF.asset`, `Settings/UI/RowMatePanel.asset`, `Settings/UI/UiAssets.asset`, `Settings/UI/GammaPresent.mat` (solo si S-B1 elige el *blit*), `Art/Shaders/UI/GammaPresent.shader` (ídem, escrito a mano), `Scenes/Boot.unity`, `Settings/URP/UniversalRenderPipelineGlobalSettings.asset` y `DefaultVolumeProfile.asset` (movidos), `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/RowMateBootstrap.json`.

#### Documentos y capturas

| Archivo | Dueño |
|---|---|
| `docs/…/02-dispositivo-simulador/snapshots/*.png`, `*.mp4`, `manifest.json`, `INDEX.md`, `rt1-measurement.json` | generados por el arnés (WP-B los produce por primera vez; WP-D los regenera mientras itera) |
| `docs/…/02-dispositivo-simulador/04-design-notes.md` | D |
| `architecture/ARCHITECTURE.md` (§2, §7.2-§7.3, §8, §13, "Cambios de la It. 2 (diseño)"), ADR 006 y 007 (cambios), ADR 019 nuevo "Canal de color de la UI" | B (al cerrar) |

### 1.2 Asmdefs nuevos y cambiados

Los cuatro nuevos son **solo de Unity** (no tienen `.csproj` en `tools/dotnet`). `AsmdefLintTests` los suma a su tabla y a sus reglas (WP-B).

```json
// Tests/UiSupport/RowMate.Tests.UiSupport.asmdef — host de UI determinista (sin [Test])
// RowMate.Simulator* por nombre (QA-G2-05): sin el define no existen y Unity descarta la referencia, como en RowMate.Bootstrap.
{
    "name": "RowMate.Tests.UiSupport",
    "rootNamespace": "RowMate.Tests.UiSupport",
    "references": ["RowMate.Core", "RowMate.App", "RowMate.Presentation", "RowMate.Bootstrap", "RowMate.Tests.Support",
                   "RowMate.Simulator", "RowMate.Simulator.Presentation"],
    "includePlatforms": [], "excludePlatforms": [], "allowUnsafeCode": false,
    "overrideReferences": false, "precompiledReferences": [], "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"], "versionDefines": [], "noEngineReferences": false
}
```

```json
// Tests/PlayMode/RowMate.Tests.PlayMode.asmdef — cambia: + UiSupport (sigue sin UnityEditor.TestRunner)
{
    "name": "RowMate.Tests.PlayMode",
    "rootNamespace": "RowMate.Tests.PlayMode",
    "references": ["RowMate.Tests.Support", "RowMate.Tests.UiSupport", "RowMate.Core", "RowMate.App",
                   "RowMate.Presentation", "RowMate.Scene", "RowMate.Bootstrap", "UnityEngine.TestRunner"],
    "includePlatforms": [], "excludePlatforms": [], "allowUnsafeCode": false,
    "overrideReferences": true, "precompiledReferences": ["nunit.framework.dll"], "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"], "versionDefines": [], "noEngineReferences": false
}
```

```json
// Tests/PlayModeSimulator/RowMate.Tests.PlayMode.Simulator.asmdef — solo con ROWMATE_SIMULATOR
{
    "name": "RowMate.Tests.PlayMode.Simulator",
    "rootNamespace": "RowMate.Tests.PlayMode.Simulator",
    "references": ["RowMate.Tests.Support", "RowMate.Tests.UiSupport", "RowMate.Core", "RowMate.App",
                   "RowMate.Presentation", "RowMate.Bootstrap", "RowMate.Simulator", "RowMate.Simulator.Presentation",
                   "UnityEngine.TestRunner"],
    "includePlatforms": [], "excludePlatforms": [], "allowUnsafeCode": false,
    "overrideReferences": true, "precompiledReferences": ["nunit.framework.dll"], "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS", "ROWMATE_SIMULATOR"], "versionDefines": [], "noEngineReferences": false
}
```

```json
// Tests/UnityEditMode/RowMate.Tests.UnityEditMode.asmdef — EditMode que usa UnityEditor (no corre en dotnet)
{
    "name": "RowMate.Tests.UnityEditMode",
    "rootNamespace": "RowMate.Tests.UnityEditMode",
    "references": ["RowMate.Tests.Support", "RowMate.Core", "RowMate.App", "RowMate.Platform", "RowMate.Presentation", "RowMate.Bootstrap",
                   "RowMate.Editor", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"], "excludePlatforms": [], "allowUnsafeCode": false,
    "overrideReferences": true, "precompiledReferences": ["nunit.framework.dll"], "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"], "versionDefines": [], "noEngineReferences": false
}
```

```json
// Snapshots/RowMate.Snapshots.asmdef — arnés de capturas (Editor, solo con el test framework)
{
    "name": "RowMate.Snapshots",
    "rootNamespace": "RowMate.Snapshots",
    "references": ["RowMate.Core", "RowMate.App", "RowMate.Presentation", "RowMate.Bootstrap",
                   "RowMate.Simulator", "RowMate.Simulator.Presentation", "RowMate.Tests.Support", "RowMate.Tests.UiSupport"],
    "includePlatforms": ["Editor"], "excludePlatforms": [], "allowUnsafeCode": false,
    "overrideReferences": false, "precompiledReferences": [], "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"], "versionDefines": [], "noEngineReferences": false
}
```

- `RowMate.Snapshots` y `RowMate.Tests.UiSupport` referencian `RowMate.Simulator*` por nombre: sin el define esos assemblies no existen y Unity descarta la referencia (como ya hace `RowMate.Bootstrap`, verificado en el primer open). El código que los usa va en `#if ROWMATE_SIMULATOR` (`HostBackends.Simulator`, `HostBackends.TestRowerWithSimulatedHeartRate`, `UiTestHost.Simulator`); sin el define, el arnés solo puede hacer CAP-22 y CAP-23 y lo avisa.
- Sin cambios: `RowMate.Core`, `RowMate.App`, `RowMate.Simulator`, `RowMate.Platform`, `RowMate.Presentation` (sigue con Core, App e Input System: `RenderPipelineManager` y `CommandBuffer` son del módulo central), `RowMate.Simulator.Presentation`, `RowMate.Bootstrap`, `RowMate.Editor`, `RowMate.Tests.Support`, `RowMate.Tests.EditMode`, `RowMate.Tests.Simulator`.
- Reglas que suma el *lint*: `RowMate.Bootstrap` lo pueden referenciar Editor, PlayMode, PlayMode.Simulator, UiSupport, UnityEditMode y Snapshots; `RowMate.Simulator*` además PlayMode.Simulator, UiSupport y Snapshots; `RowMate.Platform` además UnityEditMode; `RowMate.Tests.UiSupport` no tiene `[Test]` ni `nunit.framework.dll`; ningún asmdef no-test referencia `RowMate.Tests.*`.
- Namespaces: `RowMate.Snapshots` y `RowMate.Tests.UnityEditMode` no tienen segmento `Editor` (`NoNamespaceShadowsUnityNames`).

### 1.3 Escena Boot y assets: bootstrapper versión 3 (sin YAML a mano)

Todo lo que es YAML de Unity lo crea código de Editor, idempotente, dentro del *bootstrapper* (`CurrentVersion = 3`; el estado queda en `ProjectSettings/RowMateBootstrap.json`). Los pasos nuevos se agregan **al final** de la lista actual, en este orden, cada uno con su respaldo manual en `ManualInstructions`:

| # | Paso | Qué hace | Verificación |
|---|---|---|---|
| 10 | `urp.tidy` | `AssetDatabase.MoveAsset` de `Assets/UniversalRenderPipelineGlobalSettings.asset` y `Assets/DefaultVolumeProfile.asset` a `Settings/URP/` (conserva el GUID) | `BootstrapV3Tests.UrpAssetsLiveUnderRowMate` |
| 11 | `player.window` | Standalone: `defaultScreenWidth/Height = 1280/720`, `fullScreenMode = Windowed`, `resizableWindow = true` (Q-88); `SplashScreen.show = false` (Unity 6 lo permite en Personal); `SplashScreen.backgroundColor = bg.app` (AC-APP-01-07). Android (no necesita el módulo para escribirse): `PlayerSettings.Android.startInFullscreen = false` (barra de estado y de navegación visibles, como el legacy: `styles.xml` `windowFullscreen=false` y Flutter `edgeToEdge`; Unity arranca en `true` por defecto, medido en la sonda §8.3) y `renderOutsideSafeArea = true` (= `windowLayoutInDisplayCutoutMode=shortEdges` del legacy; ya es el valor por defecto) (QA-G2-06) | `BootstrapV3Tests.WindowAndSplash`, `BootstrapV3Tests.AndroidSystemBarsLikeLegacy` |
| 12 | `ui.fonts` | `FontAssetBuilder`: crea los 9 `FontAsset` (tabla de abajo) **solo si no existen**, agrega atlas y material como subassets, guarda, y después **reimporta** (`ImportAssetOptions.ForceUpdate`) `Typography.uss`, `Base.uss` y el tema. Hallazgo de S-0: un USS importado antes de que exista el `FontAsset` queda con `MissingAssetReference` y no se arregla solo | `FontAndIconAssetsTests` |
| 13 | `ui.panel` | `UiAssetsBuilder`: `Settings/UI/RowMatePanel.asset` (`themeStyleSheet = RowMateTheme.tss`, `scaleMode = ConstantPixelSize`, `scale = 1`, `clearColor = true`, `colorClearValue = bg.app`, `sortingOrder = 0`) y `Settings/UI/UiAssets.asset` (panel, `strings.es.json`, `strings.en.json`, material del presentador si corresponde) | `BootstrapV3Tests.PanelSettingsAsset` |
| 14 | `scene.boot` | `BootSceneBuilder`: si `Scenes/Boot.unity` no existe, `EditorSceneManager.NewScene(EmptyScene)` con: `AppRoot` (GameObject con `AppRoot` + `UIDocument` **sin `panelSettings`**: el panel lo pone `AppRoot.Awake`, §1.4) y `UiCamera` (`Camera`, `clearFlags = SolidColor`, `backgroundColor = bg.app`, `cullingMask = 0`, datos de cámara de URP por defecto). Asigna `AppRoot._assets` con `SerializedObject`. Si existe, la abre y repara lo que falte (y vacía un `panelSettings` puesto a mano). Registra la escena como la única de `EditorBuildSettings.scenes`. Hallazgo de la sonda (§8.3): `NewScene(…, Single)` descarga los assets en memoria sin referencias, así que `UiAssets` y el panel se vuelven a cargar con `AssetDatabase.LoadAssetAtPath` **después** de crear la escena (una referencia tomada antes queda "nula de Unity" y se serializa como `{fileID: 0}`) | `BootstrapV3Tests.BootSceneIsTheOnlyBuildScene`, `BootstrapV3Tests.BootSceneDocumentHasNoPanelAsset` |
| 15 | `defines.check` | el de siempre, al final | (It. 1) |

`FontAsset` (ajustes de ASSET_SLOTS §3 y §4.2; `FontAsset.CreateFontAsset(font, puntos, padding, GlyphRenderMode.SDFAA, ancho, alto, AtlasPopulationMode.Dynamic, true)`, verificado en S-0):

| Fuente | Asset | Puntos / padding | Atlas |
|---|---|---|---|
| `UI/Fonts/Inter/Inter-{Regular,Medium,SemiBold,Bold}.ttf` | `UI/Fonts/Generated/Inter-<Peso>-SDF.asset` | 48 / 5 (10 %) | 1024² |
| `UI/Fonts/BarlowSemiCondensed/BarlowSemiCondensed-{SemiBold,Bold,ExtraBold}.ttf` | `UI/Fonts/Generated/BarlowSemiCondensed-<Peso>-SDF.asset` | 90 / 9 | 1024² |
| `UI/Icons/RowMateIcons-Outline.ttf` / `-Filled.ttf` | `UI/Icons/Generated/RowMateIcons-{Outline,Filled}-SDF.asset` | 64 / 6 | 512² / 256² |

Los nombres de asset **no llevan espacios** (las rutas van en `url()` de USS). Las rutas salen de `Core/Design/FontAssetPaths.cs` (las usan el generador de `Typography.uss` y `FontAssetBuilder`), así no pueden divergir.

`AppRoot` es el único `MonoBehaviour` de la escena; `UIDocument` queda en el mismo GameObject. El `PanelSettings` del asset **no se modifica en tiempo de ejecución**: `AppRoot` hace `Instantiate` y le asigna la copia al `UIDocument` en `Awake`, antes del `OnEnable` del documento (en el Editor, modificar el asset en Play lo deja modificado en disco). Como en la escena el documento no tiene panel, si algo falla antes de ese paso el documento no se engancha a ningún panel (nunca al asset).

### 1.4 `AppRoot` (composition root) y swap simulador/hardware

```csharp
namespace RowMate.Bootstrap
{
    // Composition root (ARCHITECTURE §4.1, ADR 003). Único MonoBehaviour de Boot.unity.
    // Orden (QA-G2-01, medido en 6000.3.27f1): UIDocument tiene [DefaultExecutionOrder(-100)]; este componente corre antes
    // (−10000), así que en su Awake y su OnEnable rootVisualElement todavía es null. Awake configura (y le da al documento la
    // copia del panel, antes del OnEnable del documento); Start arma la UI, cuando la raíz ya existe.
    [DefaultExecutionOrder(-10000)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppRoot : MonoBehaviour
    {
        [SerializeField] private UiAssets? _assets;   // lo asigna BootSceneBuilder

        private UnityScheduler? _scheduler;
        private PlatformServices? _platform;
        private DeviceBackend? _backend;
        private AppContext? _app;
        private ShellHandle? _shell;
        private UiSurface? _surface;
        private UIDocument? _document;

        private void Awake();                          // pasos 1 a 6 de abajo (sin tocar rootVisualElement)
        private void Start();                          // pasos 7 y 8
        private void Update();                         // _scheduler.Tick(); _surface.Update(); _shell.Tick(); "Atrás" → _shell.HandleBack()
        private void OnApplicationFocus(bool focus);   // focus → _platform.Refresh(); _app.RefreshSystemSettings(...)
        private void OnDestroy();                      // dispone en orden inverso (lo que exista: Start puede no haber corrido)
    }
}
```

Evidencia del orden: sonda P3 de QA (`Awake: root=False`, `OnEnable: root=False`, `Start: root=True`) y sonda propia sobre un proyecto descartable (§8.3): con el panel cambiado en `Awake` por una copia con `targetTexture` y `colorClearValue` propios, en `Start` la raíz existe, el documento usa la copia, el asset sigue sin `targetTexture` y la RT se pinta con el color de la copia (`#336699` leído en el cuadro 4). También vale con el documento de la escena **sin** panel (el caso de `BootSceneBuilder`): mismo resultado.

`Awake()` (configura; no toca la UI):

1. `CultureInfo.DefaultThreadCurrentCulture = CurrentCulture = InvariantCulture` (F-APP-12).
2. `logger = new UnityAppLogger()`; primera línea propia del registro: `[APP] RowMate 1.0.0 (1) · <plataforma> · simulador=<sí|no>` (P-06: si Windows no muestra la versión en el ejecutable, queda acá).
3. `_scheduler = new UnityScheduler()`; `_platform = PlatformFactory.Create(logger)` (idioma, accesibilidad, densidad, "Atrás").
4. `_backend = DeviceBackendFactory.Create(_scheduler, logger)`:

```csharp
// Bootstrap/DeviceBackendFactory.cs — el único lugar que elige la variante (= main.dart:51-54)
internal static class DeviceBackendFactory
{
    public static DeviceBackend Create(IScheduler scheduler, IAppLogger logger)
    {
#if ROWMATE_SIMULATOR
        return SimulatorBackend.Create(scheduler, logger, noise: true);   // remo + pulsómetro simulados, un solo RowingSimulator
#else
        return HardwareBackend.Create(logger);                             // It. 2: equipo sin BLE (P-04); 10a/11: servicios reales
#endif
    }
}
```

5. `_app = AppComposer.Compose(new AppServices(_backend, _scheduler, logger, LocalizationSet.Load(es, en), _platform.Locale, _platform.Accessibility, new ShellOptions(profileTabEnabled: false)))`. Perfil queda apagado en la It. 2: su regla real (`StravaConfig.IsConfigured`) es de la It. 12; PlayMode y el arnés la prenden con `ShellOptions(true)` (CAP-25).
6. `_surface = new UiSurface(Instantiate(_assets.Panel), _platform.Display, UiColorPipeline.GammaRt /* o el que fije S-B1 */)` (crea y **limpia a `bg.app`** las RT, §3.1); `_document = GetComponent<UIDocument>(); _document.panelSettings = _surface.Panel;` **acá, en `Awake`**: así el `OnEnable` del documento (que corre después, −100) se engancha a la copia y nunca al asset.

`Start()` (arma la UI):

7. `var root = _document.rootVisualElement;` si es `null` (no debería pasar), `[APP] UIDocument sin raíz en Start` como `Error`, `enabled = false` y nada más (sin excepción). Si no: `_shell = ShellHost.Build(_app, root, new ShellHostOptions(_scheduler, isDesktop, _platform.BackButton, SimulatorLayerFactory.CreateOrNull(_backend, _app, _scheduler)))`. En un build normal `SimulatorLayerFactory` no existe (está en `#if`) y la opción es `null`.
8. `_app.Start()`: el remo simulado se conecta (`IRowerService.Start()`, = primer oyente de `simulated_ble_service.dart:17-21`) y el pulsómetro intenta su reconexión sin esperar (`main.dart:68-79`). **Sin wakelock** en la It. 2 (D-08). Como `Start` corre antes del primer `Update`, el *scheduler* no avanza antes de que la UI esté suscripta.

`OnDestroy()` dispone en orden inverso lo que se haya creado: `_shell` (desuscribe vistas) → `_app` (desuscribe *stores*) → `_backend` (cancela todos los timers, AC-APP-01-08) → `_surface` (libera las RT). Ningún estático salvo `BuildFlags`.

Prueba: `Tests/PlayMode/BootSceneTests.BootSceneStartsWithoutExceptions` (QA-G2-01): carga `Boot.unity` (`SceneManager.LoadScene`, es la escena del build), espera 5 cuadros y verifica: ningún `Error` ni excepción en el registro (el *test framework* falla solo con un `Error` no esperado), `#app-root` existe bajo la raíz del documento, el documento usa una **copia** (`!= _assets.Panel`) y el asset sigue con `targetTexture == null`. Corre en las dos configuraciones (sin y con `-Simulator`): en la primera abre en la vista de búsqueda, en la segunda en la vista conectada.

`HardwareBackend.Create(logger)` devuelve `new DeviceBackend(new UnavailableRowerService(), new UnavailableHeartRateService(), owner: null)`. Es código de producción, no un doble de prueba: es lo que el legacy hace en un equipo sin Bluetooth LE (`ble_service.dart:65-67`: `if (!supported) return;`; `heart_rate_service.dart:108-113`).

---

## 2. Lógica pura (WP-A, `dotnet`)

Todo lo de esta sección compila en `netstandard2.1`, C# 9, sin `UnityEngine`, y se prueba con `FakeScheduler`. Bloques `namespace X { }`, clases `sealed`, nulabilidad habilitada, logger obligatorio en todo observable (ADR 004). Los métodos `…Async` de los servicios simulados completan sincrónicamente (`Task.CompletedTask`), como los `async` del legacy que no esperan nada.

**Diferencia de entrega que se acepta (D-04).** Los `StreamController.broadcast` del legacy entregan en una *microtask*; `ObservableValue`/`EventStream` entregan en el acto. Ningún comportamiento visible cambia (todo ocurre en el mismo cuadro), pero una aserción intermedia de los tests legacy se porta grabando la **secuencia** de estados en lugar de mirar el instante "antes de la microtask" (LT-32, §5.3).

### 2.1 Puertos de Core

```csharp
namespace RowMate.Core.Common
{
    // Lo que exponen los puertos: suscribirse, no emitir. Lo implementa EventStream<T>.
    public interface IEventSource<T>
    {
        IDisposable Subscribe(Action<T> handler);
    }

    // Observable derivado (p. ej. AppLanguage → Localizer). Se recalcula al cambiar la fuente.
    public sealed class DerivedObservable<TIn, TOut> : IReadOnlyObservable<TOut>, IDisposable
    {
        public DerivedObservable(IReadOnlyObservable<TIn> source, Func<TIn, TOut> map, IAppLogger logger);
        public TOut Value { get; }
        public IDisposable Subscribe(Action<TOut> onChange, bool emitCurrent = true);
        public void Dispose();
    }
}

namespace RowMate.Core.Ble
{
    // Puerto del remo. legacy: lib/core/bluetooth/ble_service.dart:10-60 (la superficie que usa la UI).
    public interface IRowerService : IDisposable
    {
        IReadOnlyObservable<BleStatus> Status { get; }                   // statusStream + status
        IReadOnlyObservable<BleAdapterState> AdapterState { get; }       // adapterStateStream + adapterState
        IEventSource<RowingData> Data { get; }                           // dataStream
        IEventSource<IReadOnlyList<BleScanResult>> Devices { get; }      // devicesStream
        IEventSource<byte[]> RawBytes { get; }                           // rawBytesStream (diagnóstico, It. 10a)
        string? ConnectedDeviceName { get; }

        // = el momento en que la app empieza a escuchar (onListen del legacy, D-05). Idempotente.
        void Start();
        Task StartScanAsync(TimeSpan? timeout = null);                   // 10 s por defecto
        Task StopScanAsync();
        Task ConnectAsync(string deviceId);
        Task DisconnectAsync();
    }

    // Puerto del pulsómetro. legacy: lib/core/bluetooth/heart_rate_service.dart:31-110 (superficie pública).
    public interface IHeartRateService : IDisposable
    {
        IReadOnlyObservable<HrmStatus> Status { get; }                   // reemite con ForceNotify cuando el legacy repite el estado
        IEventSource<int> Bpm { get; }
        int CurrentBpm { get; }
        IEventSource<IReadOnlyList<BleScanResult>> Devices { get; }
        string? ConnectedDeviceName { get; }
        string? RememberedDeviceName { get; }
        bool IsRetrying { get; }

        Task StartScanAsync(TimeSpan? timeout = null);
        Task StopScanAsync();
        Task ConnectAsync(string deviceId);
        Task AutoConnectAsync();
        Task DisconnectAsync();
        Task ForgetAsync();
    }

    // legacy: heart_rate_service.dart:20-24
    public sealed class HrmIncompatibleException : Exception
    {
        public HrmIncompatibleException() : base("El dispositivo no expone el servicio Heart Rate (0x180D)") { }
    }

    // AC-HRM-04-02. legacy: heart_rate_service.dart:321-326 (_onMeasurement)
    public static class HeartRateContact
    {
        // "sin contacto" → 0; "con contacto" o "no informa" → el pulso leído, aunque sea 0.
        public static int PublishedBpm(HeartRateReading reading) => reading.SensorContact == false ? 0 : reading.Bpm;
    }
}

namespace RowMate.Core.Ports
{
    public sealed record AccessibilitySnapshot(double TextScale, bool ReduceMotion)
    {
        public static readonly AccessibilitySnapshot Default = new AccessibilitySnapshot(1.0, false);
    }

    public interface IAccessibilitySettings
    {
        IReadOnlyObservable<AccessibilitySnapshot> Current { get; }      // se vuelve a leer al recuperar el foco
    }

    public readonly struct SafeInsets
    {
        public SafeInsets(double left, double top, double right, double bottom);
        public readonly double Left, Top, Right, Bottom;   // en dp, desde los bordes de la ventana (Screen.safeArea / DpScale)
        public static readonly SafeInsets None;
    }

    public interface IDisplayMetrics
    {
        double DpScale { get; }        // px por dp: Android DisplayMetrics.density; Windows dpi / 96 (AC-APP-05-09)
        SafeInsets SafeArea { get; }   // márgenes seguros en dp (regla única de uso: §3.1)
    }

    // "Atrás" del sistema en la raíz del shell (AC-APP-02-06, D-09). Android: segundo plano o cierre; escritorio: nada.
    public interface IBackButtonPolicy
    {
        void OnBackAtRoot();
    }
}

namespace RowMate.Core.Common
{
    // Timers de tiempo real de UnityScheduler (QA-G2-15). Pura y determinista: el tiempo lo pasa quien la avanza
    // (UnityScheduler con un Stopwatch; los tests con números). Un solo hilo (el principal).
    public sealed class RealtimeTimerQueue
    {
        public IDisposable Schedule(double nowSeconds, TimeSpan delay, Action callback);
        public IDisposable SchedulePeriodic(double nowSeconds, TimeSpan period, Action callback);
        public Task Delay(double nowSeconds, TimeSpan delay, CancellationToken ct);
        // Corre los vencidos hasta nowSeconds, en orden (vencimiento, después alta). Cada periódico dispara a lo sumo una
        // vez por llamada; su próximo vencimiento es due + period, salvo que eso ya haya pasado (atraso ≥ un período: corte
        // del loop por arrastrar o redimensionar la ventana en Windows, pausa en Android): entonces se reancla en
        // nowSeconds + period. Las lecturas perdidas se pierden, no llegan comprimidas una por cuadro.
        public int RunDue(double nowSeconds);
        public int Pending { get; }
    }
}
```

`RealtimeTimerQueueTests` [d]: periódico de 1 s avanzado de a 1/60 s dispara en 1, 2, 3 s (múltiplos exactos con atraso menor que un período); un salto de 1 a 6 s dispara **una** vez a los 6 s y la siguiente a los 7 s (no 5 seguidas); un *one-shot* vencido durante el salto dispara una vez; cancelar dentro de un callback; `Delay` completa en el `RunDue` que lo vence. Es la conducta de la It. 2 hasta el alcance de ADR 017 (4a); con el simulador, durante un corte el remo no manda lecturas (la distancia no suma esos segundos) y al volver sigue a 1 Hz.

**`UnavailableRowerService` / `UnavailableHeartRateService`** (build normal de la It. 2, P-04, AC-BLE-13-05): `Status = Disconnected`, `AdapterState = Unknown` (así la vista de búsqueda muestra el botón y el estado vacío, como el legacy sin BLE: `device_screen.dart:346-347` solo cambia de vista con `off`/`unauthorized`), ninguna fuente emite y todos los métodos devuelven `Task.CompletedTask` sin cambiar nada ni registrar errores. `Start()` no hace nada. Se reemplazan en las It. 10a (remo) y 11 (pulsómetro).

### 2.2 Simulador: `SimulatedRowerService` y `SimulatedHeartRateService`

```csharp
namespace RowMate.Simulator
{
    // Remo FTMS simulado y "conectado" (F-BLE-15). legacy: lib/core/bluetooth/simulated_ble_service.dart:8-120
    public sealed class SimulatedRowerService : IRowerService
    {
        public const string DeviceName = "Remo simulado";                                  // :11, fijo (Q-03)
        public static readonly TimeSpan TickPeriod = TimeSpan.FromSeconds(1);              // :93
        public static readonly TimeSpan DefaultReconnectAfter = TimeSpan.FromSeconds(3);   // :81

        public SimulatedRowerService(RowingSimulator simulator, IScheduler scheduler, IAppLogger logger);
        public RowingSimulator Simulator { get; }                                          // :29
        // IRowerService: Status (inicial Disconnected, :37), AdapterState (siempre On, :22-26, :55),
        // Data, Devices y RawBytes (estos dos nunca emiten, :34-35), ConnectedDeviceName (:59-60)
        public void Start();                                             // :17-21: conecta una sola vez en la vida del servicio
        public Task StartScanAsync(TimeSpan? timeout = null);            // :62-65 → Connect()
        public Task StopScanAsync();                                     // :67-68, no hace nada
        public Task ConnectAsync(string deviceId);                       // :70-71 → Connect()
        public Task DisconnectAsync();                                   // :73-78
        public void SimulateDisconnect(TimeSpan? reconnectAfter = null); // :80-86
        public void Dispose();                                           // :110-119
    }

    // Pulsómetro simulado (F-HRM-15). legacy: lib/core/bluetooth/simulated_heart_rate_service.dart:9-156
    public sealed class SimulatedHeartRateService : IHeartRateService
    {
        public const string DeviceName = "Pulsómetro simulado";                            // :13, fijo (Q-03)
        public const string DeviceId = "SIM:HRM";                                          // :14
        public const int Rssi = -50;                                                       // :61
        public static readonly TimeSpan DefaultScanTimeout = TimeSpan.FromSeconds(10);     // :65
        public static readonly TimeSpan DefaultReconnectAfter = TimeSpan.FromSeconds(3);   // :107

        public SimulatedHeartRateService(RowingSimulator simulator, IScheduler scheduler, IAppLogger logger);
        public static BleScanResult FakeScanResult();   // :50-63: SIM:HRM, PlatformName null, AdvName DeviceName, −50, [0x180D]
        public void SimulateDisconnect(TimeSpan? reconnectAfter = null);  // :107-116
        // IHeartRateService completo; AutoConnectAsync no hace nada (:89-91)
    }
}
```

Comportamiento, línea a línea (las dos clases guardan sus timers como `IDisposable?` del `IScheduler`):

| Legacy | Port |
|---|---|
| `_connect()` (`simulated_ble_service.dart:88-97`): cancela la vuelta pendiente; si ya está conectado, nada; `connected`; `Timer.periodic(1 s)` que emite `simulator.tick().copyWith(heartRate: 0)` | `Connect()`: `_reconnect?.Dispose()`; `if (Status == Connected) return;` (AC-BLE-15-07); `SetStatus(Connected)`; `_data = scheduler.SchedulePeriodic(TickPeriod, () => _dataStream.Emit(Simulator.Tick().WithHeartRate(0)))` (AC-BLE-15-03) |
| `_stop()` (`:99-103`) | `Stop()`: `_data?.Dispose()`; si no está desconectado, `SetStatus(Disconnected)` |
| `disconnect()` (`:73-78`) | cancela la vuelta y `Stop()` (AC-BLE-07-02 a -04, AC-BLE-15-05) |
| `simulateDisconnect()` (`:80-86`): solo si está conectado; `debugPrint('[SIM] Desconexión simulada, reconectando en 3s')`; `_stop()`; `Timer(3 s, _connect)` | ídem, con `logger.Log(Info, "SIM", "Desconexión simulada, reconectando en 3s")`. La física no se toca: distancia, paladas y tiempo siguen desde donde iban (AC-BLE-15-09) |
| `connectedDeviceName` (`:59-60`) | `Status == Connected ? DeviceName : null` (AC-BLE-15-01) |
| `onListen` del primer oyente (`:17-21`) y adaptador `on` (`:22-26`) | `Start()` con `_started` (una sola vez, AC-BLE-15-02); `AdapterState` es un observable constante en `On` |
| HRM `startScan()` (`simulated_heart_rate_service.dart:65-77`): cancela la vuelta; reinicia el plazo; `scanning`; emite `[fakeScanResult()]`; a los 10 s, si sigue buscando, `disconnected` | ídem (AC-HRM-15-02, -12). `SetStatus` hace `ForceNotify` si el estado se repite (el legacy emite siempre) |
| HRM `stopScan()` (`:79-84`) | ídem (AC-HRM-15-03) |
| HRM `_connect()` (`:118-130`): cancela búsqueda y vuelta; si conectado, nada; cancela un timer de datos viejo; `connected`; cada 1 s `_bpm = simulator.heartRate` y lo emite | ídem: **directo** a `Connected`, sin `Connecting` (AC-HRM-15-04); primera lectura al segundo |
| HRM `_stop()` (`:132-140`): cancela datos; si `_bpm != 0`, emite 0; `disconnected` | ídem, en ese orden (AC-HRM-04-04) |
| HRM `disconnect()` (`:93-98`) y `forget()` (`:100-105`: cancela la vuelta y **reemite** el estado) | ídem; `forget` termina en `ForceNotify()` (AC-HRM-15-06, -07) |
| HRM `simulateDisconnect()` (`:107-116`): solo conectado; agenda la vuelta **antes** de `_stop()`, así `disconnected` llega con `isRetrying == true` | ídem, mismo orden (AC-HRM-15-08) |
| `rememberedDeviceName` e `isRetrying` (`:46-48`): no nulos solo mientras hay vuelta pendiente | ídem |
| `dispose()` (`:147-155`; `simulated_ble_service.dart:110-119`) | cancela todos los timers; después de `Dispose` los métodos no hacen nada (AC-APP-01-08, AC-HRM-15-11) |

**`SimulatorBackend`** (`main.dart:51-54`, AC-APP-01-02, AC-BLE-13-03):

```csharp
namespace RowMate.Simulator
{
    public sealed class SimulatorDeviceBackend : DeviceBackend
    {
        public RowingSimulator Simulator { get; }
        public SimulatedRowerService SimRower { get; }
        public SimulatedHeartRateService SimHeartRate { get; }
    }

    public static class SimulatorBackend
    {
        // Un solo RowingSimulator: el pulso del pulsómetro sale de la física del remo (AC-HRM-15-09).
        // noise = false: el modo de prueba de la It. 1 (AC-BLE-14-06); la app usa noise = true.
        public static SimulatorDeviceBackend Create(IScheduler scheduler, IAppLogger logger, bool noise, IRandom? random = null);
    }
}
```

### 2.3 `DeviceStore` (puerto de `device_provider.dart`)

```csharp
namespace RowMate.App.Device
{
    public sealed record DeviceState(
        BleStatus Status,
        BleAdapterState AdapterState,
        RowingData Data,
        IReadOnlyList<BleScanResult> ScanResults,
        Exception? Error,                    // se guarda como el legacy; el banner es de la It. 10a (D-15)
        string? ConnectedDeviceName)
    {
        public bool IsConnected => Status == BleStatus.Connected;
    }

    // legacy: lib/features/device/device_provider.dart:10-141, sin el vigilante de 5 s (:69-86, F-BLE-09 → Core en la 10a)
    // ni el diagnóstico (:19-22, :98-102, F-BLE-12 → 10a).
    public sealed class DeviceStore : IDisposable
    {
        public DeviceStore(IRowerService rower, IAppLogger logger);
        public IReadOnlyObservable<DeviceState> State { get; }
        public Task StartScanAsync();                // :104-114: borra error y lista; captura la excepción
        public Task StopScanAsync();                 // :116
        public Task ConnectAsync(string deviceId);   // :118-127
        public Task DisconnectAsync();               // :129 (AC-BLE-07-02)
        public void Dispose();
    }
}
```

- Al pasar a `Connected` reemplaza `Data` por `RowingData.Empty` (`_resetSession`, `:38-42`, `:64-67`): la grilla muestra `--:--`, `0.0`, `0`, `0m` hasta la primera lectura de esa conexión (AC-BLE-11-09).
- Cada lectura de `Data` reemplaza el estado (una notificación por segundo con el simulador).
- El estado inicial toma `Status` y `AdapterState` del servicio (el legacy arranca en `disconnected`/`unknown`, que es lo que valen los dos servicios de la It. 2 al crearse).

### 2.4 `HeartRateStore` (puerto de `heart_rate_provider.dart`)

```csharp
namespace RowMate.App.Device
{
    public sealed record HeartRateState(
        HrmStatus Status, int Bpm, IReadOnlyList<BleScanResult> ScanResults, Exception? Error,
        string? ChosenName, bool IsRetrying, string? ConnectedDeviceName, string? RememberedDeviceName)
    {
        public bool IsConnected => Status == HrmStatus.Connected;     // :42
        public bool IsScanning => Status == HrmStatus.Scanning;       // :43
        public bool IsConnecting => Status == HrmStatus.Connecting;   // :44
        public bool HasRemembered => RememberedDeviceName != null;    // :50
        public string ConnectingName => ChosenName ?? RememberedDeviceName ?? "";   // :54
    }

    // legacy: lib/features/device/heart_rate_provider.dart:7-117
    public sealed class HeartRateStore : IDisposable
    {
        public HeartRateStore(IHeartRateService hrm, IAppLogger logger);
        public IReadOnlyObservable<HeartRateState> State { get; }
        public static bool IsIncompatible(Exception error) => error is HrmIncompatibleException;   // :56

        public Task StartScanAsync();                              // :58-63: borra error y lista, después busca
        public Task ConnectAsync(string deviceId, string? name);   // :65-70: borra error, fija el nombre elegido
        public Task RetryAsync();                                  // :72-78: borra error y nombre, AutoConnectAsync
        public Task DisconnectAsync();                             // :80-83
        public Task ForgetAsync();                                 // :85-89: + reaviso
        public void Dispose();                                     // :109-116
    }
}
```

- Suscripciones (`:23-39`): `Status` (si `Connected`, borra el error; si no es `Connecting`, borra el nombre elegido), `Bpm` y `Devices`. En cada aviso toma de nuevo `IsRetrying`, `ConnectedDeviceName` y `RememberedDeviceName` del servicio: el legacy los lee en vivo con *getters*, y un estado inmutable los fotografía en cada aviso, que es cuando la tarjeta se redibuja.
- `_run` (`:91-101`): `try { await action(); } catch (Exception e) { Error = e; NotifyIfAlive(); }`. El error se borra al **empezar** una acción y al llegar a `Connected`; nunca al pasar a `Disconnected` (AC-HRM-12-03). Con `_disposed` no se avisa nada (AC-HRM-12-06).
- `ForgetAsync` termina con un `ForceNotify` (`_notifyIfAlive()`, `:88`).

### 2.5 Reglas de las pantallas de Dispositivo

```csharp
namespace RowMate.App.Device
{
    public enum DeviceViewKind { Connected, Scan }

    public sealed record DeviceGridSpec(int Columns, double AspectRatio, double Spacing)
    {
        public double CellWidth(double gridWidthDp) => (gridWidthDp - Spacing * (Columns - 1)) / Columns;
        public double CellHeight(double gridWidthDp) => CellWidth(gridWidthDp) / AspectRatio;
    }

    public sealed record MetricTileSpec(MetricKind Kind, string LabelKey, string Label, string ValueText, double NumericValue,
                                        ColorToken Color, string IconCodepoint, bool IconFilled, string ElementName);

    public sealed record ScanViewSpec(bool SearchEnabled, bool Searching, bool ShowSearchHint, bool ShowEmptyState);

    public static class DeviceScreenRules
    {
        public const double ContentPaddingH = 20, ContentPaddingV = 16, HeaderGridGap = 24, GridCardGap = 12;   // device_screen.dart:89, :119, :121
        public const string FallbackRowerName = "Rower";                                                         // :98 (se localiza en la 10a)

        public static DeviceViewKind ViewFor(BleStatus status);                 // :33-38 (AC-BLE-11-01)
        public static bool ShowDisconnectAction(BleStatus status);              // :20-31 (AC-BLE-07-01)
        public static string DeviceName(string? connectedName);                 // :98
        public static DeviceGridSpec Grid(double gridWidthDp);                  // :261-276 (AC-BLE-11-05)
        public static IReadOnlyList<MetricTileSpec> Tiles(RowingData data, Localizer texts);   // :252-259 (AC-BLE-11-03, -04, -08)
        public static ScanViewSpec Scan(BleStatus rower, bool hrmScanning, int resultCount);  // :344-348, :397-442
    }
}
```

- `Grid` recibe el **ancho de la grilla** (ventana − 40 dp, `device_screen.dart:87-89`), no el de la ventana: más de 600 → 3 columnas y 1,6; si no, 2 y 1,3; separación 12. 640 dp de ventana → 600 → 2 columnas; 641 → 601 → 3 (tabla de AC-BLE-11-05 en `DeviceScreenRulesTests`).
- `Tiles` usa `RowingFormats` de la It. 1 (`Split`, `Spm`, `Watts`, `Meters`, `HeartRate`), `MetricPalette` para el color, `Icons` para el glifo (`timer` contorno, `speed`, `bolt`, `straighten`, `favorite` relleno) y agrega PULSO **solo** si `data.HeartRate > 0`; el pulso del pulsómetro nunca entra a la grilla (AC-BLE-11-04). Calorías, tiempo y paladas no aparecen (AC-BLE-11-08). `NumericValue` da el sentido del rodillo (segundos del split, spm, W, m, bpm). `ElementName` = `metric-tile-split|spm|watts|distance|hr`.
- `Scan`: `SearchEnabled = !(rower == Scanning || rower == Connecting || hrmScanning)` (AC-BLE-05-01, AC-HRM-13-08); `Searching = ShowSearchHint = rower == Scanning`; `ShowEmptyState = resultCount == 0 && rower != Scanning` (AC-BLE-05-02). La lista de remos encontrados no se dibuja en la It. 2 (10a).

```csharp
namespace RowMate.App.Device
{
    public enum HrmCardBody { Connected, Connecting, Scanning, Remembered, NoSensor }

    public sealed record HrmDeviceRow(string DeviceId, string Title, string Subtitle, string ElementName);   // "hrm-device-<id>"

    public sealed record HrmCardViewModel(
        string Title,                 // hrmTitle.ToUpperInvariant(): PULSÓMETRO / HEART RATE SENSOR (toUpperCase de Dart no depende del idioma)
        bool HeartFull,               // conectado, con o sin lectura (AC-HRM-13-02)
        bool HeartBeating,            // conectado y bpm > 0 (AC-HRM-13-12)
        int Bpm,
        HrmCardBody Body,
        string ConnectedName, string BpmText, string BpmUnit,          // Connected: nombre, "142" o "--", "bpm"
        string ConnectingText,                                          // Connecting: "Conectando a {name}..."
        string SearchingText, IReadOnlyList<HrmDeviceRow> Rows,         // Scanning
        string RememberedLine,                                          // Remembered: "{recordado} · reconectando..." o "· no encontrado"
        string NoSensorText, string WatchHint,                          // NoSensor
        bool SearchEnabled, bool ConnectEnabled,                        // Buscar y Buscar otro / Conectar (AC-HRM-13-04)
        string SearchLabel, string SearchOtherLabel, string ConnectLabel, string ForgetLabel, string DisconnectLabel,
        string? ErrorText);           // hrmIncompatible o hrmConnectError, hasta 3 líneas

    // legacy: lib/features/device/heart_rate_card.dart:13-238
    public static class HrmCardRules
    {
        public static HrmCardViewModel Build(HeartRateState hr, BleStatus rowerStatus, Localizer texts);
        public static HrmCardBody BodyFor(HeartRateState hr);                  // :68-94, prioridad de AC-HRM-13-03
        public static string RowTitle(BleScanResult r, Localizer texts);       // :232-237
    }
}
```

- `BodyFor`: `Connected` → Connected; `Connecting` con `IsRetrying` y `HasRemembered` → Remembered; `Connecting` → Connecting; `Scanning` → Scanning; `Disconnected` → Remembered si `HasRemembered`, si no NoSensor.
- `rowerBusy = rowerStatus == Scanning || rowerStatus == Connecting` (`:22-24`). `SearchEnabled = !rowerBusy`; `ConnectEnabled = !(hr.IsRetrying || rowerBusy)`; Olvidar, Desconectar y las filas, siempre (LT-31, AC-HRM-13-04).
- `RowTitle`: nombre de plataforma si no está vacío; si no, el anunciado; si no, `hrmUnknownSensor` (AC-HRM-13-05). Subtítulo = `DeviceId`. Tocar una fila llama a `HeartRateStore.ConnectAsync(id, RowTitle(r))`.
- `ErrorText`: `IsIncompatible(e) ? hrmIncompatible : Format(hrmConnectError, error: e.Message)`. El legacy interpola `'${hr.error}'` (`heart_rate_card.dart:57`): el `toString()` de Dart, que antepone el tipo (`Exception: …`). El port usa `e.Message`: **desvío D-25** (§7.1). Con el simulador no hay errores; las capturas CAP-14c/d usan el pulsómetro de prueba.
- El modelo no tiene `Cancelar`, señal, mensaje de lista vacía ni "detener búsqueda" (AC-HRM-13-11).

### 2.6 Shell

```csharp
namespace RowMate.App.Shell
{
    public enum TabDestinationId { Device, Workout, Routines, History, Profile }

    public sealed record TabDestination(TabDestinationId Id, string LabelKey, string TitleKey,
        string Icon, string SelectedIcon, bool SelectedFilled, string ElementName);   // "tab-device"…

    public sealed record ShellOptions(bool ProfileTabEnabled);

    // legacy: lib/main.dart:116-181
    public sealed class ShellStore
    {
        public const int HistoryIndex = 3;                               // :141
        public ShellStore(ShellOptions options, IAppLogger logger);
        public IReadOnlyList<TabDestination> Destinations { get; }       // 4, o 5 con Perfil al final (:126-134, :150-176)
        public IReadOnlyObservable<int> SelectedIndex { get; }           // 0 al abrir; no se guarda (:124)
        public void Select(int index);                                   // :146-149; elegir el ya elegido no cambia nada
    }
}
```

Destinos (AC-APP-02-01, -02): `Device` (`navDevice`, `deviceTitle`, `bluetooth` / `bluetooth_connected`), `Workout` (`navWorkout`, `workoutTitle`, `timer` contorno / `timer` relleno), `Routines` (`navRoutines`, `routinesTitle`, `fitness_center` / el mismo), `History` (`navHistory`, `historyTitle`, `history` / el mismo) y `Profile` (`navProfile`, `profileTitle`, `person` contorno / relleno) solo con `ProfileTabEnabled`. El ícono depende solo de la selección, nunca del remo (AC-APP-02-05). La recarga de Historial al tocarla (Q-22) es de la It. 9.

### 2.7 Panel del simulador

```csharp
namespace RowMate.Simulator.Panel
{
    public sealed record SimulatorPreset(string Name, int Watts, int Spm);   // simulator_overlay.dart:10-14

    public sealed record SimulatorPanelState(bool IsOpen, int TargetWatts, int TargetSpm, int? HourOverride, bool Rowing)
    {
        public string WattsText => Fmt.Integer(TargetWatts) + " W";              // :127
        public string SpmText => Fmt.Integer(TargetSpm) + " spm";                // :134
        public string HourText => HourOverride == null ? "real" : Fmt.Integer(HourOverride.Value) + ":00";   // :141
        public bool ShowRealHourButton => HourOverride != null;                  // :146
    }

    // legacy: lib/core/dev/simulator_overlay.dart:28-173 (estado y acciones, sin widgets)
    public sealed class SimulatorPanelStore : IDisposable
    {
        public static readonly IReadOnlyList<SimulatorPreset> Presets;           // Suave 100/20, Medio 180/24, Fuerte 280/30
        public SimulatorPanelStore(SimulatorDeviceBackend backend, SceneSettings scene, IClock clock, IAppLogger logger);
        public IReadOnlyObservable<SimulatorPanelState> State { get; }
        public void Open(); public void Close();                                 // :29, :44-47 (nada se guarda, AC-BLE-16-11)
        public void ApplyPreset(SimulatorPreset preset);                         // :115-121 (no toca Remando ni la hora)
        public void WattsMinus(); public void WattsPlus();                       // ±10 con los topes de RowingSimulator (:129-130)
        public void SpmMinus(); public void SpmPlus();                           // ±1 (:136-137)
        public void HourMinus(); public void HourPlus();                         // :143-144: ((hora ?? clock.LocalNow.Hour) ± 1) mod 24, DartMath.Mod
        public void RealHour();                                                  // :148
        public void SetRowing(bool rowing); public void ToggleRowing();          // :151-156
        public void SimulateRowerDrop();                                         // :160: SimRower.SimulateDisconnect()
        public void SimulateHeartRateDrop();                                     // :165: SimHeartRate.SimulateDisconnect()
    }

    public static class SimulatorPanelLayout
    {
        public const double Width = 280;            // :91-92
        public const double MinMaxHeight = 120;     // :93
        public const double FabRight = 16;          // :41
        public const double GapAboveTabBar = 16;    // AC-BLE-16-02 (legacy: 96 sobre una barra de 80)
        // AC-BLE-16-03 (:83-86): (alto − margen seguro inferior) − 96 − margen seguro superior − 16, mínimo 120 (D-26).
        // Sin márgenes = la fórmula de la spec: 360 → 248; 914 → 802; 200 → 120. Con márgenes 24 / 48 (Android con
        // barra de gestos): 914 → 730, y el borde superior del panel queda en 24 + 32 = 56, debajo de la barra de estado.
        public static double MaxHeight(double windowHeightDp, SafeInsets insets);
        // tabBarHeightDp = alto real de la barra, con su margen inferior (64 + inset, §3.2).
        public static double FabBottom(double tabBarHeightDp) => tabBarHeightDp + GapAboveTabBar;
    }
}
```

Por qué `− margen seguro inferior` (D-26): el panel se ancla en `FabBottom` = barra real + 16, y la barra crece con el margen inferior; con la fórmula literal y una barra de gestos de 48, el borde superior del panel quedaría 16 dp **debajo** de la barra de estado y la X de cerrar, tapada (la propia AC-BLE-16-03 pide que se vea siempre). Sin margen inferior (Windows, Editor, todos los ejemplos de la spec) da exactamente lo mismo. El legacy no tiene el caso porque su panel se ancla a 96 dp del borde de la pantalla aunque la `NavigationBar` crezca (en Android 15, con *edge-to-edge*, se encimaba con su propia barra).

`SimulatorTexts` (Q-03, fijos en los dos idiomas): `Simulador`, `Watts`, `Ritmo`, `Hora escena`, `Hora real`, `Remando`, `Simular desconexión`, `Simular caída del pulsómetro`, `SIMULADOR`. Los nombres `Remo simulado` y `Pulsómetro simulado` son constantes de los servicios. `App/Scene/SceneSettings` expone `ObservableValue<int?> HourOverride` (`scene_settings.dart:19-24`): el legacy lo guarda como `double?`, pero siempre con horas enteras (`DateTime.now().hour`), así que `int?` es equivalente. Nadie lo lee en la It. 2 (su efecto es de la 6a).

### 2.8 Composición pura

```csharp
namespace RowMate.App.Composition
{
    public class DeviceBackend : IDisposable
    {
        public DeviceBackend(IRowerService rower, IHeartRateService heartRate, IDisposable? owner);
        public IRowerService Rower { get; }
        public IHeartRateService HeartRate { get; }
        public virtual void Dispose();       // dispone los dos servicios (y el dueño)
    }

    public sealed record AppServices(DeviceBackend Devices, IScheduler Scheduler, IAppLogger Logger, LocalizationSet Texts,
                                     ISystemLocale Locale, IAccessibilitySettings Accessibility, ShellOptions Shell);

    public sealed class AppContext : IDisposable
    {
        public DeviceStore Device { get; }
        public HeartRateStore HeartRate { get; }
        public ShellStore Shell { get; }
        public SceneSettings Scene { get; }
        public LanguageState Language { get; }
        public IReadOnlyObservable<Localizer> Texts { get; }           // DerivedObservable sobre Language.Current
        public IReadOnlyObservable<AccessibilitySnapshot> Accessibility { get; }
        public IScheduler Scheduler { get; }
        public IAppLogger Logger { get; }
        public void Start();                                           // remo Start() + reconexión del pulsómetro sin esperar
        public void RefreshSystemSettings(IReadOnlyList<string> languageTags);   // al recuperar el foco (AC-APP-09-05)
        public void Dispose();                                         // stores en orden inverso; NO dispone el backend
    }

    public static class AppComposer
    {
        public static AppContext Compose(AppServices services);
    }
}
```

- `Start()` = `main.dart:68-79`: `HeartRate.AutoConnectAsync()` sin esperar; si falla, `logger.Log(Warning, "HRM", "autoConnect: " + e.Message, e)`, o sea `[HRM] autoConnect: <error>` en el registro y nada en pantalla (AC-APP-01-04).
- El orden de `Dispose` es el inverso de la creación; quien creó el `DeviceBackend` lo dispone después. Con `FakeScheduler`, `PendingTimers == 0` al final (AC-APP-01-08).

### 2.9 Motion y diseño puros (`Core/Design`, `Core/Models`)

```csharp
namespace RowMate.Core.Design
{
    // ASSET_SLOTS §4.3 (commit 10ecc14). IconsTests compara cada nombre y codepoint con UI/Icons/*.codepoints.
    public static class Icons
    {
        public const string Bluetooth = "", BluetoothConnected = "", BluetoothDisabled = "",
            BluetoothSearching = "", Timer = "", FitnessCenter = "", History = "", Person = "",
            Rowing = "", Speed = "", Bolt = "", Straighten = "", Favorite = "",
            ChevronRight = "", Refresh = "", Build = "", Close = "", Add = "",
            Remove = "", LinkOff = "", HeartBroken = "", Error = "";   // + el resto de §4.3
        public static IReadOnlyList<IconInfo> All { get; }      // nombre, codepoint, en contorno, en relleno
    }

    // ANIMATION_BIBLE y 04-design-notes §5: los números que no son tokens. Duraciones y curvas que SÍ son tokens se toman de
    // Tokens (dur.micro 90, dur.fast 160, dur.base 240, dur.slow 360; ease.*; shift.s 8, shift.m 12) y no se repiten acá.
    public static class MotionSpecs
    {
        // Rodillo (§5.1, DN-08): 180 ms ease.enter; saliente se apaga en u ∈ [0; 0,6]; entrante aparece en u ∈ [0,15; 1].
        public const double RollShiftEm = 0.35, ReducedFadeMs = 80, RollOutFadeEnd = 0.60, RollInFadeStart = 0.15;
        public const double ColonCellEm = 0.32;                                            // ":" y "." (el "-" usa su avance real)
        // Pestañas (§5.4, DN-11): contenido sale 0-120 y entra 80-280; ícono 1 → 1,12 (0-90) → 1 (90-260); color 160 (dur.fast).
        public const double TabOutMs = 120, TabInStartMs = 80, TabInEndMs = 280, TabIconBump = 1.12, TabIconPeakMs = 90, TabIconEndMs = 260;
        public const double PillOmega = 22;                                                // spring.snappy (§1.2)
        public const double FitScaleOmega = 12;                                            // spring.smooth (§5.9, DN-15)
        // Latido (§5.3, DN-10): s(τ) = 1 + 0,16·p₁(τ) + 0,06·p₂(τ); p₁ sube 0-90 (ease.enter) y baja 90-290 (ease.move);
        // p₂ sube 170-240 y baja 240-390. Si T = 60/bpm < 420 ms, todos los tiempos × T/420. Parada: a 1 en 120 ms.
        public const double BeatMainGain = 0.16, BeatRiseMs = 90, BeatFallMs = 200;
        public const double SecondBeatGain = 0.06, SecondBeatOffsetMs = 170, SecondBeatRiseMs = 70, SecondBeatFallMs = 150;
        public const double BeatFullCycleMs = 420, BeatStopMs = 120;
        // Acento (§5.2, DN-07): k = 1 − 0,4·E_pulse(t / 300); acento = color de la métrica con alfa k; brillo = 0,14·k.
        public const double AccentFlashMs = 300, AccentRestAlpha = 0.60, AccentGlowPeak = 0.14;
        public const double DimHeartOpacity = 0.40;                                        // [CONTRATO] heart_rate_card.dart:40 (por código, DN-13)
        // Vista conectada ↔ búsqueda (§5.5, DN-12): sale 0-120 (escala 0,985 al apagar); entra 120-360 (shift.s);
        // al encender, tarjeta i de 120 + 35·i a +240 (shift.m).
        public const double ViewOutMs = 120, ViewInEndMs = 360, ViewOutScale = 0.985, TileStaggerMs = 35, TileInMs = 240;
        // Llave y panel (§5.7, DN-14): panel opacidad 160 y escala 0,92 → 1 en 240; llave vuelve con 0,85 → 1 en 160.
        public const double PanelInScaleFrom = 0.92, FabInScaleFrom = 0.85;
        // Tarjeta PULSO (§5.9, DN-16): sale con opacidad en 120 y la grilla se recoge en 200.
        public const double PulseTileOutMs = 120, PulseTileCollapseMs = 200;
        public const double SpinnerTurnMs = 900, SpinnerArcDegrees = 270;                  // §4.7 de las notas
    }

    // Factor del destello del acento (§5.2 de las notas, DN-07). Solo lo arranca una tarjeta cuyo TEXTO cambió.
    public static class AccentPulse
    {
        public static double K(double elapsedMs);      // 1 en 0; 0,60 desde 300 ms; ease.pulse entre medio
    }

    public static class Easing { public static double Evaluate(EasingToken curve, double t); }   // cubic-bezier (Newton + bisección)

    public struct CriticalSpring
    {
        public double Position, Velocity;
        public void Step(double dtSeconds, double target, double omega);   // ζ = 1, sin asignaciones
    }

    public readonly struct DigitRollPlan
    {
        public int Length { get; }
        public int PreviousLength { get; }       // si cambia el largo, el ancho interpola 180 ms (DN-08 c)
        public int Direction { get; }            // +1 el valor subió, −1 bajó, 0 igual; un marcador "--" vale 0
        public bool IsChanged(int cell);         // alineado a la derecha; si cambia el largo, las celdas nuevas cambian
    }

    public static class DigitRoll                // ANIMATION_BIBLE §2.4; curvas de 04-design-notes §5.1 (DN-08 d)
    {
        public const double DurationMs = 180;    // ease.enter
        public static DigitRollPlan Plan(string previous, string next, double previousValue, double nextValue);
        // u = t/180, E = ease.enter. Saliente: y = −dir·0,35·E(u) em, alfa = 1 − E(min(1, u/0,6)).
        // Entrante: y = +dir·0,35·(1 − E(u)) em, alfa = E(clamp((u − 0,15)/0,85)). Reducir movimiento: fundido lineal de 80 ms.
        // En u ≥ 1: entrante en 0 con alfa 1 y saliente con alfa 0 → se ve el valor exacto (AC-BLE-11-11).
        public static void Evaluate(double elapsedMs, int direction, bool reduceMotion,
                                    out double outOffsetEm, out double outAlpha, out double inOffsetEm, out double inAlpha);
        public static double WidthAt(double elapsedMs, double fromWidth, double toWidth);   // ease.enter, 180 ms
    }

    public sealed class HeartbeatClock           // ANIMATION_BIBLE §2.9, 04-design-notes §5.3 (DN-10), AC-HRM-13-12
    {
        // El primer latido arranca en el instante del primer bpm > 0; un bpm nuevo vale desde el latido siguiente;
        // 0 → sin latidos nuevos y la escala vuelve a 1 en 120 ms (ease.move) desde donde esté.
        public void SetBpm(int bpm, double nowSeconds);
        public double ScaleAt(double nowSeconds);         // suma de los dos golpes (MotionSpecs); 1 en reposo
        public bool IsAtRest(double nowSeconds);          // |escala − 1| ≤ 0,002 (regla de toma de las capturas, DN-21)
        public int BeatsStarted { get; }
    }

    public sealed class FramePacingPolicy        // ARCHITECTURE §13.1
    {
        public FramePacingPolicy(double idleSecondsBeforeThirty = 1.0);
        public void Acquire(); public void Release();
        public int RenderFrameInterval(double nowSeconds);   // 1 (60 fps) con demandas o hasta 1 s después; si no, 2 (30 fps)
    }

    public static class TextScaleSteps           // P-03 (tope 1,3)
    {
        public static readonly double[] Steps = { 0.85, 1.0, 1.15, 1.3 };
        public static double Quantize(double systemScale);
        public static string ClassName(double step);         // "rm-text-scale-085" | "-100" | "-115" | "-130"
    }

    public static class FontAssetPaths           // §1.3; una sola fuente para Typography.uss y FontAssetBuilder
    {
        public static string For(FontFamilyRole family, int weight);   // Inter 400/500/600/700; Barlow SC 600/700/800
        public const string IconsOutline = "Assets/RowMate/UI/Icons/Generated/RowMateIcons-Outline-SDF.asset";
        public const string IconsFilled = "Assets/RowMate/UI/Icons/Generated/RowMateIcons-Filled-SDF.asset";
    }

    public static class TypographyUssWriter { public static string Write(); }
}

namespace RowMate.Core.Models
{
    public static class MetricPalette { public static ColorToken For(MetricKind kind); }   // F-APP-06: Split → metric.split …
}
```

**`Typography.uss` (generado).** Por cada `TypeScaleToken`: `.rm-type-<token> { font-size: <N>px; -unity-font-definition: url("project://database/<ruta>"); }`. Nombre de clase = nombre del token en *kebab case* con `.` → `-` (`title` → `rm-type-title`, `titleS` → `rm-type-title-s`, `bodyXs` → `rm-type-body-xs`, `num.xl` → `rm-type-num-xl`); los tres tokens de WP-D0 (`bodyM`, `bodyXs`, `button`) salen solos. `label` suma **`letter-spacing: 8px;`** con el comentario `/* unidad de TextCore: N px = N/100 em por letra; 8px = 0,08 em a cualquier tamaño */` (QA-G2-02, DN-02): en UI Toolkit 6000.3 la unidad de `letter-spacing` no es el píxel (sonda P2 de QA a 12 dp: `0px` = 32,00, `0.96px` = 33,00, `8px` = 41,00 dp de ancho para `IIIIIIIIII`; a 48 dp, `0px` = 127,00 y `8px` = 162,00: escala con la letra, coincide con 04-design-notes §2.2). La constante es `TypographyUssWriter.LabelTrackingEm = 0.08` y el generador escribe `em × 100`. Por cada escala distinta de 1: `.rm-text-scale-115 .rm-type-<token> { font-size: <N × 1,15>px; … }` (dos clases: le gana a la regla base) **sin** repetir el `letter-spacing` (ya escala con la letra). Además `:root { -unity-font-definition: <Inter 400>; }`, `.rm-icon { -unity-font-definition: <íconos contorno>; -unity-text-align: middle-center; white-space: nowrap; letter-spacing: 0; }` y `.rm-icon--filled { -unity-font-definition: <relleno>; }`. USS no tiene `line-height` (D-13): los altos de línea de los tokens no se aplican. `TypographyUssSyncTests` regenera en memoria y compara, como `TokensUssSyncTests`, y fija dos valores a mano: `.rm-type-label` lleva `letter-spacing: 8px` y ninguna regla `.rm-text-scale-*` tiene `letter-spacing`. En PlayMode, `THM.LabelTrackingIsEightHundredthsEm` mide `IIIIIIIIII` con `.rm-type-label` y con `.rm-type-label` + `letter-spacing: 0` en línea: la diferencia es 9 × 0,08 × tamaño de letra ± 1 dp (a 12 dp, ≈ 8,6; con `.rm-text-scale-130`, ≈ 11,2).

**`ParitySmoke`** (`Core/Text`): `public static IReadOnlyList<string> Run()` evalúa la tabla de AC-BASE-01 (`Fmt.Fixed` y `DartMath.Round` con `0.25`, `24.45`, `2.5`, −0,05…) y devuelve las diferencias. En dotnet da 0; un player arrancado con `-rowmate-parity-smoke` la corre, la escribe en el registro y sale (prueba de humo IL2CPP, §5.1).

### 2.10 Dobles de prueba (`Tests/Support`; nunca en un build)

| Doble | Equivale a | Qué permite |
|---|---|---|
| `ControllableRowerService : IRowerService` | `_ControllableBleService` (`heart_rate_card_test.dart:25-48`) | `Emit(BleStatus)`, `EmitData(RowingData)`, nombre conectado; no se conecta solo; registra las llamadas |
| `ScriptedHeartRateService : IHeartRateService` | `_ThrowingHrm` ampliado (spec §0.4) | estado, bpm, `IsRetrying`, recordado (`Polar H10 7E1A2B3C`), resultados de búsqueda y conducta de `ConnectAsync` (queda `Connecting`, conecta, lanza `HrmIncompatibleException` o `InvalidOperationException(texto)`), todo a pedido |
| `ThrowingHeartRateService` | `_ThrowingHrm` tal cual (`heart_rate_provider_test.dart:11-51`) | LT-33 |
| `FakeSystemLocale`, `FakeAccessibilitySettings`, `FakeDisplayMetrics` | spec §0.4 | idioma, escala de letra, reducir movimiento y densidad de prueba |
| `StateRecorder<T>` | las listas `statuses.add` de los tests legacy | graba la secuencia de un observable o de una fuente |

---

## 3. UI (WP-B, UI Toolkit)

Reglas que valen para todo el código de esta sección (Unity 6000.3, C# 9):

- Las vistas son `VisualElement` construidos **en C#**, una clase por pieza, con nombres (`name`) y clases USS de contrato (§3.12). No hay UXML (D-02). Ninguna vista sabe si los servicios son simulados (AC-BLE-13-04): reciben `AppContext` o sus *stores*.
- Sin LINQ, sin *closures* nuevas ni `foreach` sobre interfaces en caminos por cuadro (`MotionDriver.Tick`, `generateVisualContent`). Al cambiar un dato se actualizan solo los elementos que cambian.
- Hallazgos de S-0 para quien escribe el código: `Color32` no convierte solo a `StyleColor` (usar `(Color)`); `TextElement` es ambiguo si se importan `UnityEngine.UIElements` y `UnityEngine.TextCore.Text` a la vez (calificar); `PanelSettings.forceGammaRendering` es un **campo** público, no una propiedad; `PanelSettings` no tiene `textSettings` en 6.3.

### 3.1 Panel, escala, área segura y canal de color

**Escala (D-03).** Un solo modo en todas las plataformas: `PanelScaleMode.ConstantPixelSize` con `scale = IDisplayMetrics.DpScale` (Android `DisplayMetrics.density`, Windows `dpi / 96`, Editor `Screen.dpi / 96`). Es literalmente "1 dp = 1 px × densidad" de AC-APP-05-09 y lo mismo que hacen el arnés y los tests (S-0: 1080 × 2400 px a 2,625 → 411,43 × 914,29 dp). ARCHITECTURE §7.3 decía `ConstantPhysicalSize` a 160 dpi en móvil, que usa `Screen.dpi` (dpi físicos, no la densidad de Android): podía dar un 2-5 % distinto del legacy. `PanelScaleController` recalcula escala y área segura cada cuadro si cambiaron `Screen.width`, `Screen.height`, la densidad o `Screen.safeArea` (rotar o redimensionar no recrea nada, AC-APP-02-07). `Screen.safeArea` tiene el origen abajo a la izquierda: `Top = (Screen.height − safeArea.yMax) / escala`, `Bottom = safeArea.y / escala`, `Left = safeArea.x / escala`, `Right = (Screen.width − safeArea.xMax) / escala`.

**Área segura: una sola regla (QA-G2-06).** Cada margen lo consume **una sola** pieza, como en el legacy (Flutter: `AppBar` = `SafeArea(bottom: false)`, `app_bar.dart:1191`; `NavigationBar` = `SafeArea`, `navigation_bar.dart:291`; el cuerpo no se rellena; la `Banner` va en la esquina de la pantalla, `main.dart:108-110`, `simulator_overlay.dart:33-36`). `ShellLayout` la aplica por código cada vez que cambian los márgenes o el alto de la barra:

| Pieza | Qué consume | Cómo |
|---|---|---|
| `#app-root` | nada | sin *padding*; ocupa todo el panel (`position: absolute`, cuatro lados en 0, R2-09) |
| `.rm-appbar` (cada pantalla) | superior y laterales | `padding-top = Top`, `padding-left = Left`, `padding-right = Right`; alto `56 + Top`; el fondo `bg.app` llega al borde (detrás de la barra de estado, como la `AppBar`) |
| `#tab-bar` | inferior y laterales | `padding-bottom = Bottom`, `padding-left = Left`, `padding-right = Right`; alto `64 + Bottom`; llega al borde inferior (sin franja de fondo debajo) |
| Contenido de cada pantalla | nada lateral (paridad: el cuerpo de Flutter no se rellena) | abajo, `padding-bottom` = alto **real** de `#tab-bar` dentro del contenido desplazable (DN-23, §3.2) |
| `#simulator-layer` | nada | ocupa todo el panel; la cinta va en la esquina de la **pantalla** (centro en 24, 24, como la `Banner`; = 04-design-notes §4.8 corregida: "sin sumar insets propios", §8.2); la llave a 16 del borde derecho y a `FabBottom(alto real de la barra)`; el panel con `MaxHeight(alto de la ventana, márgenes)` (§2.7, D-26) |

Así ningún margen se cuenta dos veces: el alto de la ventana es el del panel (no el de una raíz rellenada) y el inferior entra una sola vez, en la barra. Prueba: `Tests/PlayMode/SafeAreaTests` con `FakeDisplayMetrics(2.625, new SafeInsets(0, 24, 0, 48))` en TV: `#app-root` sin *padding* y del tamaño del panel; `#device-appbar` arranca en y = 0 con alto 80 y su título en y ≥ 24; `#tab-bar` con alto 112 y `yMax` = alto del panel; al final del desplazamiento el último elemento queda por encima de la barra; y en TH con `Left = 32`, la barra superior y la de pestañas corridas 32 y la grilla con el mismo número de columnas (el contenido no se rellena). Con el simulador (`SimulatorOverlayTests.InsetsAreCountedOnce` [ps]): llave a 112 + 16 del borde inferior, panel con alto máximo 730,29 (914,29 − 48 − 96 − 24 − 16) y borde superior ≥ 40, cinta con centro en (24, 24) desde la esquina del panel.

**Android: barras del sistema.** El legacy las muestra (`styles.xml`: `windowFullscreen=false`; Flutter en `edgeToEdge`, la app dibuja detrás de la barra de estado). Unity arranca por defecto en pantalla completa (`PlayerSettings.Android.startInFullscreen = true`, leído en la sonda §8.3): el *bootstrapper* lo pone en `false` (paso `player.window`, §1.3). Si Unity dibuja detrás de las barras (Android 15+ con `targetSdk` 35 lo impone), `Screen.safeArea` trae los márgenes y la regla de arriba los consume; si no, los márgenes valen 0 y la barra de estado queda con el color del sistema (R2-15). NO VERIFICABLE sin el módulo de Android: [Device-And].

**Canal de color (D-01, R-T1).** Medido en S-0 (Anexo A):

| Camino | `glass.fallback` sobre blanco | `text.secondary` sobre `surface.card` | Composición |
|---|---|---|---|
| *Screen Space Overlay* en un player de Windows (con o sin `forceGammaRendering`) | `#6A6A6C` | `#DADBDC` | lineal |
| RT `R8G8B8A8_SRGB`, sin `forceGammaRendering` | `#6A6A6C` | `#DADBDC` | lineal (= pantalla) |
| RT `R8G8B8A8_UNorm` + `forceGammaRendering = true` | **`#2A2F37`** | **`#BBC1C8`** | sRGB, como el legacy |

`UiColorPipeline { Overlay, GammaRt }`. Por defecto **`GammaRt`**:

```csharp
namespace RowMate.Presentation.Surface
{
    public enum UiColorPipeline { Overlay, GammaRt }

    // Dónde se dibuja el panel de la app. GammaRt: RT UNorm del tamaño de la pantalla + forceGammaRendering,
    // presentada en el backbuffer sin volver a convertir los colores (opción A de 04-design-notes §5 de la It. 1).
    public sealed class UiSurface : IDisposable
    {
        public UiSurface(PanelSettings runtimePanel, IDisplayMetrics display, UiColorPipeline pipeline, IUiPresenter? presenter);
        public PanelSettings Panel { get; }
        public UiColorPipeline Pipeline { get; }
        public void Update();      // tamaño de pantalla, escala, área segura; recrea la RT si cambió el tamaño
        public void Dispose();
    }

    public interface IUiPresenter : IDisposable { void Present(RenderTexture gammaUi); }
}
```

Dos presentadores; **S-B1** (primera tarea de WP-B, en un build de desarrollo de Windows con Mono) elige uno midiendo el *backbuffer* con la muestra de CAP-23, como el *probe* de S-0:

| Presentador | Cómo | Pro / contra |
|---|---|---|
| `OverlayCopyPresenter` (preferido) | Cada cuadro dibujado, `Graphics.CopyTexture(rtUNorm, rtSrgb)` (mismo tamaño y familia de formato, copia de bytes) y un segundo panel *Overlay* (`sortingOrder` alto) con un único elemento a pantalla completa, `background-image = rtSrgb`, `pickingMode = Ignore`. Al muestrear el sRGB, la GPU decodifica a lineal y el *backbuffer* sRGB vuelve a codificar: quedan los mismos bytes (P2 de QA: `sameBytes=True` en el Editor D3D11) | Solo UI Toolkit, sin shader ni cambios de URP. Riesgo: `CopyTexture` entre UNorm y sRGB en GLES 3.0 (Android de gama baja) |
| `BlitPresenter` | `RenderPipelineManager.endContextRendering` → `CommandBuffer.Blit(rtUNorm, CameraTarget, GammaPresent.mat)`; el shader decodifica sRGB → lineal antes de escribir | Funciona igual en todos los backends. Suma un `.shader` y un material al build |

**Primer cuadro y latencia (QA-G2-13).** (a) `UiSurface` **limpia las dos RT a `bg.app` al crearlas** (y al recrearlas por un cambio de tamaño): `RenderTexture.active = rt; GL.Clear(false, true, bgApp)`; una RT recién creada tiene contenido indefinido. Verificado en la sonda §8.3: la primera lectura después del `GL.Clear` da `#0D1B2A`. (b) La copia **no** va en `Update`: va en el primer gancho de render posterior al repintado de los paneles con `targetTexture` (candidato: `RenderPipelineManager.beginContextRendering`; alternativa: `endContextRendering`, antes de que se dibujen los paneles *Overlay*). S-B1 fija cuál, con la prueba de latencia de abajo. (c) `FramePacer` nunca se saltea el cuadro de la copia: el presentador copia en cada cuadro dibujado.

Criterio de S-B1 (en el build de desarrollo de Windows, Mono): (1) el píxel del vidrio sobre blanco en pantalla da `#2A2F37` ± 1; (2) el clic en un botón de la UI llega al botón correcto (mapeo de pantalla a panel con la RT del tamaño de la pantalla; si el mapeo por defecto no alcanza, `PanelSettings.SetScreenToPanelSpaceFunction`); (3) el costo medido es ≤ 1 ms por cuadro en la PC; (4) **el primer cuadro presentado** (lectura del *backbuffer* al final del cuadro 1) es `bg.app` ± 1 en todo el cuadro (AC-APP-01-07); (5) **latencia 0**: la muestra dibuja en `Update` el número de cuadro como un color (canal rojo = cuadro mod 256) y el *backbuffer* de ese mismo cuadro muestra ese número. **Si ninguno de los dos presentadores cumple los cinco, el valor por defecto pasa a `Overlay`**, la It. 2 queda con la composición lineal (lo que el PO previó en AC-BASE-05) y el tema vuelve a la 4a. El arnés de capturas usa siempre la misma tubería que la app (§4).

El fondo es `bg.app` desde el primer cuadro (AC-APP-01-07): `UiCamera.backgroundColor`, `colorClearValue` del panel, las dos RT al crearse y `SplashScreen.backgroundColor` son `#0D1B2A`, y el *splash* del motor está apagado (paso `player.window`). En Android, el color de la pantalla de arranque del sistema (Android 12+) y el primer cuadro de la superficie de Unity no se pueden probar sin el módulo: NO VERIFICABLE, [Device-And]. La marca "Development Build" de los builds de desarrollo no se puede ocultar desde la API: queda solo en los builds de simulador (P-01).

### 3.2 Shell, capas y barra de pestañas

Árbol (los nombres son contrato):

```
#app-root .rm-app .rm-text-scale-<n> [.rm-reduce-motion] [.rm-desktop] [.rm-keyboard-nav]     sin padding (§3.1)
├── #tab-content .rm-tab-content                capa 0
│   ├── #screen-device   .rm-screen             (DeviceScreenView)
│   ├── #screen-workout  .rm-screen             (PlaceholderScreenView)
│   ├── #screen-routines .rm-screen
│   ├── #screen-history  .rm-screen
│   └── #screen-profile  .rm-screen             solo con ShellOptions.ProfileTabEnabled
├── #tab-bar .rm-tab-bar                         capa 1 (absoluta abajo, sobre el contenido); padding inferior y laterales = área segura
│   ├── .rm-tab-bar__sheen                       [DN-06] brillo superior, primer hijo, absoluto, pickingMode Ignore
│   ├── #tab-bar-pill .rm-tab-bar__pill          píldora 56 × 32, posición por código
│   └── #tab-device .rm-tab [.rm-tab--selected]  (Button) × 4 o 5
│       ├── .rm-tab__icon-box                    56 × 32
│       │   ├── .rm-tab__state                   [DN-04] capa de estado (hover/presión), absoluta, pickingMode Ignore
│       │   ├── .rm-tab__icon .rm-icon [.rm-icon--filled]
│       │   └── .rm-focus-ring > .rm-focus-ring__halo   [DN-05] absolutos, no ocupan lugar
│       └── .rm-tab__label .rm-type-label
├── #page-stack   .rm-layer   (vacía en la It. 2; pickingMode Ignore)   capa 2
├── #dialog-layer .rm-layer   (vacía)                                    capa 3
├── #toast-layer  .rm-layer   (vacía)                                    capa 4
└── #simulator-layer .rm-layer (solo con el define, §3.5)                capa 5
```

- **`TabHost`** = `IndexedStack` (`main.dart:143`): las 4/5 pantallas se construyen **una vez** en `ShellHost.Build` y siguen suscriptas a sus *stores* aunque no se vean (AC-APP-01-05). Cambiar de pestaña pone `display: none` en la anterior al final de su salida y `flex` en la nueva: el desplazamiento y el estado de cada pantalla se conservan (AC-APP-02-04; P2 de QA: una `ScrollView` conserva su desplazamiento con `display: none`). Ninguna se recrea.
- **`TabBarView`**: un `Button` por destino; las etiquetas se ven siempre; el elegido tiene `.rm-tab--selected`, el glifo "elegido" (y `.rm-icon--filled` en Workout y Perfil) y la píldora detrás del ícono, con la etiqueta debajo (ART_BIBLE §3.11). Tocar el elegido no hace nada (AC-APP-02-03). Alto de contrato 64 dp + área segura inferior como *padding* (ART_BIBLE §3.11; el legacy usaba la barra M3 de 80). En reposo, los colores de ícono y etiqueta salen de `Base.uss` (`.rm-tab` / `.rm-tab--selected`); durante el cambio, `TabTransition` los funde por código en 160 ms (DN-11) y al terminar **borra** el estilo en línea.
- **El contenido corre por debajo de la barra** (DN-23): el *padding* inferior por la barra va **dentro del contenido desplazable** de cada pantalla (`#device-connected-scroll`, contenido de los *placeholders*) o, en la vista de búsqueda, en `#device-scan-view` (sus dos áreas desplazables terminan sobre la barra). Vale el alto **real** de `#tab-bar` (`64 + Bottom`, leído de `#tab-bar.layout.height` en `GeometryChangedEvent`). Así, desplazado hasta el final, el último elemento queda por encima de la barra y la translucidez de `surface.nav` se nota al desplazar. `.rm-screen` no lleva *padding* propio.
- **`AppBarView`** (cada pantalla): `.rm-appbar` alto 56 dp (`kToolbarHeight` del legacy) + área segura superior como *padding*, fondo `bg.app` sin línea ni sombra, `.rm-appbar__title .rm-type-title` y `.rm-appbar__actions` a la derecha (AC-APP-05-02).

### 3.3 Dispositivo

```
#screen-device
├── #device-appbar .rm-appbar
│   ├── .rm-appbar__title                    deviceTitle
│   └── .rm-appbar__actions
│       └── #device-disconnect .rm-button .rm-button--text .rm-button--danger   solo conectado (AC-BLE-07-01)
│           ├── .rm-button__icon .rm-icon    bluetooth_disabled, 18 dp
│           └── .rm-button__label            deviceDisconnect
└── #device-views .rm-device-views                              [DN-12] flex-grow 1, position relative; aloja las dos vistas superpuestas
    ├── #device-connected-view .rm-device-connected             absoluta, cuatro lados en 0; en reposo display según DeviceScreenRules.ViewFor
    │   └── #device-connected-scroll (ScrollView vertical; contenido con padding 16 / 20 / 16 + alto real de #tab-bar / 20)
    │       ├── #device-header .rm-device-header                margin-top 8; fila centrada que ajusta (flex-wrap, DN-24), min-height 32
    │       │   ├── .rm-device-header__icon .rm-icon            rowing, 32 dp, accent.primary
    │       │   ├── #device-name .rm-device-header__name .rm-type-title   "Remo simulado", una línea con "…"
    │       │   └── #device-connected-badge .rm-badge .rm-badge--connected   radio 12, padding 4 / 10
    │       │       ├── .rm-badge__icon .rm-icon                bluetooth_connected, 12 dp
    │       │       └── .rm-badge__label .rm-type-body-xs       deviceConnected, 12 dp (DN-03)
    │       ├── #metric-grid .rm-metric-grid                    margin-top 24 (MetricGridView)
    │       │   └── #metric-tile-split .rm-card .rm-metric-tile .rm-metric--split   × 4 o 5
    │       │       ├── .rm-metric-tile__glow .rm-card__glow    [DN-06] absoluto arriba; textura VerticalFade y opacidad 0,14·k por código
    │       │       ├── .rm-metric-tile__header > .rm-metric-tile__icon .rm-icon (14) + .rm-metric-tile__label .rm-type-label
    │       │       └── .rm-metric-tile__value-box (FitBox) > .rm-metric-tile__value (TabularNumber) .rm-type-num-xl
    │       └── #hrm-card-slot-connected                        margin-top 12, margin-bottom 48 (QA-G2-12); aloja la tarjeta (§3.4)
    └── #device-scan-view .rm-device-scan                       absoluta, cuatro lados en 0; padding 16 / 16 / 16 + alto real de #tab-bar / 16; columna que estira
        ├── #device-search .rm-button .rm-button--primary        ancho completo (AC-BLE-05-01), min-height 48
        │   ├── .rm-button__state                                [DN-04]
        │   ├── .rm-button__icon .rm-icon / .rm-spinner          bluetooth_searching 18 o spinner 16 dp blanco
        │   ├── .rm-button__label .rm-type-button                deviceSearch / deviceSearching
        │   └── .rm-focus-ring > .rm-focus-ring__halo            [DN-05]
        ├── #device-search-hint .rm-type-body-xs .rm-text--tertiary   12 dp centrado (DN-03; antes caption 11), solo con el remo buscando
        ├── #device-empty-area (ScrollView) .rm-device-empty     margin-top 12, flex-grow 1, flex-basis 0, contenido centrado
        │   └── .rm-empty > .rm-empty__icon .rm-icon (bluetooth_disabled 48, @24 %)
        │                 + .rm-empty__title .rm-type-body (text.secondary, DV-05) + .rm-empty__hint .rm-type-body-xs .rm-text--tertiary
        └── #hrm-card-slot-scan (ScrollView) .rm-hrm-slot         margin-top 12, flex-grow 1, flex-basis 0
```

- **Márgenes de la vista conectada** (AC-BLE-11-07, corregida en el gate; `device_screen.dart:89`, `:92`, `:123`, `:135`): 20 a los costados, 16 + 8 = 24 sobre la cabecera, y **64 debajo de la tarjeta del pulsómetro** hasta el final del contenido = `margin-bottom` 48 del *slot* (los `SizedBox` de 32 y 16 del legacy, que alojan el acceso oculto al diagnóstico de la 10a) + el *padding* inferior de 16. A eso se suma, adentro del contenido, el alto real de la barra (DN-23), así que desplazado hasta el final quedan 64 dp entre la tarjeta y el borde superior de la barra (CAP-04b). Prueba: `DSP.ConnectedViewScrollsAsOneBlockWithMargins`.
- **Una sola instancia** de `HeartRateCardView` que se mueve entre `#hrm-card-slot-connected` y `#hrm-card-slot-scan` al cambiar de vista: el estado lo tiene el *store*, y así no hay dos `hrm-search` en el árbol.
- **Cambio de vista** (DN-12, 04-design-notes §5.5): `ViewSwap` (código, con el reloj inyectable). En reposo la vista inactiva tiene `display: none` (no recibe eventos ni ocupa *layout*). Al cambiar: la saliente se apaga 0-120 ms (al "apagar", escala 0,985 desde arriba al centro), y mientras sale un bloqueador `TrickleDown` en su raíz descarta los eventos de puntero y de navegación; a los 120 ms pasa a `display: none`, la tarjeta del pulsómetro cambia de *slot* y la entrante pasa a `flex` y entra 120-360 (al "encender", la cabecera con opacidad y cada tarjeta de la grilla escalonada 35 ms, con su destello). Si el estado vuelve a cambiar en medio, la transición actual salta a su final. El estado inicial se aplica sin animación (nada antes del primer cuadro presentado). Consecuencia para las pruebas: después de un cambio de estado del remo, la vista nueva está completa con `host.Settle()` (§4.2).
- **Vista de búsqueda en horizontal** (`device_screen.dart:419-424`, `:472-474`): `Expanded` + `Flexible` del legacy reparten el alto libre en mitades; acá `#device-empty-area` y `#hrm-card-slot-scan` tienen `flex-grow: 1; flex-basis: 0` y cada uno se desplaza por su cuenta. La tarjeta queda arriba de su mitad y lo que sobra queda vacío abajo, como en el legacy. En 640 × 360 nada desborda y `Buscar` se alcanza desplazando el área de la tarjeta (LT-30, AC-HRM-13-10).
- **`MetricGridView`**: no usa *wrap* de flexbox (no puede expresar la proporción). En `GeometryChangedEvent`, si cambió su ancho, pide `DeviceScreenRules.Grid(layout.width)` y fija `style.width/height` y posición absoluta de cada tarjeta (fila y columna) y su propio alto. Un cambio de alto solo no recalcula (no hay bucle). Agregar o sacar PULSO también recalcula, con la coreografía de 04-design-notes §5.9 (DN-16): al aparecer, el alto de la grilla anima en `dur.slow` `ease.move` y la tarjeta entra como al "encender"; al irse, opacidad a 0 en 120 y el alto se recoge en 200. Un cambio de columnas (rotar, redimensionar) no se anima (paridad).
- **Barras de desplazamiento superpuestas**: en `Base.uss`, el `Scroller` vertical de toda `ScrollView` de la app va `position: absolute` a la derecha. Si ocupara ancho, a 641 dp la grilla daría 2 columnas en lugar de 3 (Flutter superpone la barra en escritorio).
- **Vista que cambia**: al volver a la vista conectada (tras una caída o al reconectar), su desplazamiento vuelve a 0 (a los 120 ms, antes de que entre), como el legacy, que crea un `_ConnectedView` nuevo. El cambio de pestaña no lo toca.
- **Lecturas**: cada `MetricTileView` recibe su `MetricTileSpec`; **solo si cambió el texto** (DN-07, spec §10.2 y VID-01 corregidos por el gate), `TabularNumber.SetValue(texto, valor)` arranca el rodillo en las celdas que cambian y `AccentFlash` arranca `AccentPulse`: el borde superior va del color de la métrica al 100 % al 60 % en 300 ms y el brillo de 0,14 a 0,084. Si el texto no cambió (`24.0` → `24.0`), nada se mueve ni brilla. El color del borde con su alfa lo pone el código (`style.borderTopColor` = color del token × `k`; USS no puede aplicar un alfa a una variable): en reposo `k = 0,60` (legacy `withOpacity(0.6)`, `device_screen.dart:300`; DV-02). Al conectar se ven los ceros del estado (AC-BLE-11-09). Si un valor nuevo cambia la escala que necesita el `FitBox` (`9999m` → `10000m`), la escala sigue un resorte ω 12 (DN-15); el primer cálculo, un cambio de tamaño de ventana o de letra y reducir movimiento: en el acto.

### 3.4 Tarjeta del pulsómetro (`HeartRateCardView`)

```
#hrm-card .rm-card .rm-hrm-card .rm-hrm-card--<body>          ancho completo, padding 16, borde superior 3 dp metric.hr al 60 % fijo (por código; sin destello)
├── #hrm-glow .rm-card__glow                                  [DN-06] primer hijo, absoluto arriba; brillo en reposo 0,084 (= 0,14 × 0,60)
├── .rm-hrm-card__header                                      fila alineada a la izquierda
│   ├── #hrm-heart .rm-icon .rm-icon--filled .rm-hrm-card__heart   favorite 14 dp metric.hr; opacidad por código (DN-13)
│   └── #hrm-title .rm-hrm-card__title .rm-type-label         PULSÓMETRO / HEART RATE SENSOR, metric.hr.hud (DV-03)
├── #hrm-body .rm-hrm-card__body                              se reconstruye cuando cambia HrmCardBody; overflow hidden solo mientras anima su alto
│   ├── Connected:  .rm-hrm-connected
│   │   ├── .rm-hrm-connected__texts > #hrm-connected-name .rm-type-body-m (1 línea, "…") + #hrm-bpm-box (FitBox, alineado a la izquierda)
│   │   │       > #hrm-bpm (TabularNumber .rm-type-num-xl metric.hr) + #hrm-bpm-unit .rm-type-body-m (matiz del pulso, DV-04; por línea de base, DN-09)
│   │   └── #hrm-disconnect .rm-button .rm-button--text .rm-button--danger (bluetooth_disabled 18)
│   ├── Connecting: .rm-hrm-connecting > .rm-spinner (16) + #hrm-connecting-text .rm-type-body-m
│   ├── Scanning:   .rm-hrm-scanning > (.rm-spinner + #hrm-searching-text .rm-type-body-s .rm-text--tertiary) + #hrm-device-<id> .rm-hrm-device-row × n
│   │               fila (Button, min-height 56): .rm-button__state + .rm-hrm-device-row__icon (favorite contorno 24, metric.hr)
│   │               + __title .rm-type-body + __subtitle .rm-type-caption .rm-text--tertiary (11 dp) + __chevron (18, text.tertiary) + .rm-focus-ring
│   ├── Remembered: #hrm-remembered-text .rm-type-body-m + .rm-hrm-actions (wrap, separación 8 / 4)
│   │               > #hrm-retry .rm-button--tonal (refresh) + #hrm-search .rm-button--outline + #hrm-forget .rm-button--text
│   └── NoSensor:   #hrm-nosensor-text .rm-type-title-s + #hrm-watch-hint .rm-type-body-xs .rm-text--tertiary (12 dp, DN-03; antes caption 11)
│                   + #hrm-search .rm-button .rm-button--tonal (bluetooth_searching 18)
└── #hrm-error .rm-hrm-card__error .rm-type-body-xs           hasta 3 líneas con "…" (TextClamp), state.dangerText 12 dp
```

Cada botón de la tarjeta es un `RmButton` con sus hijos de contrato (`.rm-button__state`, ícono, `.rm-button__label .rm-type-button`, `.rm-focus-ring > .rm-focus-ring__halo`; §3.7). Los tamaños de texto del legacy, verificados: `hrmWatchHint` 12 (`heart_rate_card.dart:104`), nombre conectado y recordado 14 (`:161`, `:204`), `bpm` 14 (`:180`), `Buscando sensores...` 13 (`:126`), subtítulo de la fila 11 (`:141`), error 12 (`:60`), título 12 (`:44`); en Dispositivo, `deviceSearchHint` 12 (`device_screen.dart:414`) y `deviceNoResultsHint` 12 (`:436`).

- La vista solo traduce `HrmCardViewModel`: textos, `SetEnabled(...)` de cada botón (deshabilitado = `:disabled`, opacidad 0,38 y sin respuesta, AC-HRM-13-04) y clases de estado. Los nombres `hrm-search`, `hrm-retry`, `hrm-forget`, `hrm-disconnect` y `hrm-device-<id>` son los *keys* del legacy (spec §12.8).
- El corazón: pleno con el pulsómetro conectado, `@40 %` en cualquier otro estado (AC-HRM-13-02); late con `HeartbeatAnimator` solo si `HeartBeating` (conectado y bpm > 0). Con `--` queda quieto y pleno; sin conexión, quieto y al 40 % (AC-HRM-13-02, -12, confirmado por el PO en el gate). **Un solo mecanismo** para la opacidad (DN-13): estilo en línea por código, fundido de 160 ms (`dur.fast`, `ease.move`); no hay clase `--dim` en USS. Reducir movimiento: mismo fundido (es opacidad), sin latido.
- Cambiar de cuerpo no "salta" (spec §10.4, 04-design-notes §5.6): `HeightTween` anima el alto de `#hrm-body` del valor anterior al nuevo en `dur.slow` con `ease.move`, con `overflow: hidden` solo mientras dura (DN-13), y el contenido nuevo entra con un fundido de `dur.fast`; al terminar, alto automático. Con reducir movimiento, sin animación.
- `TextClamp` (§3.7) corta el error a 3 líneas con "…" (UI Toolkit 6.3 no tiene un límite de líneas).

### 3.5 Capa del simulador: cinta, llave y panel (equivalente del `Overlay`)

El legacy monta la capa en `MaterialApp.builder` (`main.dart:108-110`), por encima del `Navigator` y **sin `Overlay`** (por eso no usa `Tooltip`, `Slider` ni hojas inferiores, `simulator_overlay.dart:16-18`). El equivalente es **el último hijo de `#app-root`**: UI Toolkit dibuja y rinde los eventos en orden de árbol, así que la capa queda por encima de las pestañas y, desde la It. 3, también de `#page-stack`, `#dialog-layer` y `#toast-layer`, sin depender de ninguna pantalla (spec §12.5). La capa misma es `pickingMode = Ignore`: solo la llave y el panel reciben eventos.

```
#simulator-layer .rm-layer .rm-sim-layer                     (SimulatorLayerView) ocupa todo el panel, sin padding (§3.1)
├── #sim-open .rm-sim-fab (Button)                            40 × 40, ícono build 24; right 16, bottom = FabBottom(alto real de la barra)
│   └── .rm-button__state + ícono + .rm-focus-ring > .rm-focus-ring__halo   [DN-04, DN-05]
├── #sim-panel .rm-sim-panel                                  280 dp de ancho, max-height = SimulatorPanelLayout.MaxHeight(alto de la ventana, márgenes)
│   ├── .rm-sim-panel__header                                 fuera del scroll: "Simulador" .rm-type-title-s + #sim-close .rm-icon-button (close 24, caja 48)
│   └── #sim-panel-scroll (ScrollView) > .rm-sim-panel__content (columna que estira)
│       ├── .rm-sim-presets > #sim-preset-suave / -medio / -fuerte .rm-chip     (wrap, separación 8; etiqueta .rm-type-button)
│       ├── #sim-watts-row .rm-sim-stepper > __label .rm-type-body-m "Watts" + #sim-watts-minus .rm-icon-button
│       │                  + #sim-watts-value .rm-type-num-stat (64 dp, centrado; DN-19, legacy 72: D-27) + #sim-watts-plus .rm-icon-button
│       ├── #sim-spm-row   .rm-sim-stepper > "Ritmo" + #sim-spm-minus + #sim-spm-value + #sim-spm-plus
│       ├── #sim-hour-row  .rm-sim-stepper > "Hora escena" + #sim-hour-minus + #sim-hour-value + #sim-hour-plus
│       ├── #sim-hour-real .rm-button .rm-button--text        "Hora real", solo con hora forzada, alineado a la derecha
│       ├── #sim-rowing .rm-switch-row (SwitchToggle)         "Remando" .rm-type-body-m a la izquierda; .rm-switch__track > .rm-switch__thumb a la derecha (DN-17)
│       ├── #sim-drop-rower .rm-button .rm-button--outline    link_off 18 + "Simular desconexión"
│       └── #sim-drop-hrm   .rm-button .rm-button--outline    heart_broken 18 + "Simular caída del pulsómetro"
└── #sim-ribbon .rm-sim-ribbon (pickingMode Ignore)           ÚLTIMO hijo (por encima de la llave y del panel); 80 × 12 dp, centro en (24, 24)
    └── .rm-sim-ribbon__label                                 desde la esquina de la pantalla, rotate −45° (= Banner topStart); "SIMULADOR", sin clase .rm-type-*
```

- `SimulatorLayerView(SimulatorPanelStore? store, VisualElement tabBar, IDisplayMetrics display)`: con el panel cerrado (como arranca siempre) se ve la llave; al tocarla, el panel abre **en el mismo lugar**, anclado abajo a la derecha, y la llave se oculta; `#sim-close` lo cierra y vuelve la llave (AC-BLE-16-02). La posición se recalcula con `GeometryChangedEvent` de la capa y de la barra (`FabBottom(tabBar.layout.height)`, alto real con su margen) y el alto máximo con `MaxHeight(alto de la capa = alto de la ventana, display.SafeArea)` (AC-BLE-16-03, -12, D-26). Con `store == null` (solo `UiTestHost`, capturas "Prueba", QA-G2-05) la capa tiene cinta y llave, sin panel: tocar la llave no hace nada.
- Abrir y cerrar (DN-14, 04-design-notes §5.7, `PopTransition`, por código): al abrir, la llave se oculta en el cuadro 0 y el panel aparece en su lugar con opacidad 0 → 1 en 160 y escala 0,92 → 1 en 240 (origen abajo a la derecha). Al cerrar, el panel se oculta **en el acto** (así LT-26 y LT-27 ven "ya no se ve `280 W`" sin esperar) y la llave vuelve con opacidad y escala 0,85 → 1 en 160. Reducir movimiento: solo opacidad.
- `#sim-rowing` (DN-17): `SwitchToggle` = `Toggle` de Unity con `.unity-toggle__checkmark` oculto y `.rm-switch__track > .rm-switch__thumb` dentro de su `.unity-toggle__input`, a la derecha; el rótulo es el `label` del `Toggle`, a la izquierda. El `Toggle` registra el clic en todo el renglón, así que tocar el rótulo o la pista lo cambia (= `SwitchListTile`).
- La cabecera queda **fuera** del desplazamiento, así la X está siempre visible (AC-BLE-16-03). En el legacy la cabecera se desplaza con el resto; la spec pide que quede visible.
- Cada control llama a su método del *store*; la vista se redibuja con `State` (AC-BLE-16-04 a -10). Fallas sin errores: `SimulateDisconnect` no hace nada si el equipo no está conectado (AC-BLE-16-09).
- Estética de la app (spec §10.6): superficie `surface.overlay`, `r.card`, tipografía e íconos de la app; el detalle es de WP-D (`SimulatorSkin.uss`).
- Nombres de contrato del legacy: `sim-close`, `sim-watts-minus/-plus`, `sim-spm-minus/-plus`, `sim-hour-minus/-plus` (spec §12.8), más `sim-open` (00-deuda-diseno §3.4).

### 3.6 Pestañas provisorias (Workout, Rutinas, Historial, Perfil)

`PlaceholderScreenView(TabDestination)`: `.rm-placeholder` con su `AppBarView` (título del catálogo: `workoutTitle`, `routinesTitle`, `historyTitle`, `profileTitle`) y `.rm-placeholder__body` (flex-grow 1, *padding* inferior = alto real de `#tab-bar`, §3.2) con, centrado entre las dos barras, `.rm-placeholder__icon .rm-icon` con el glifo **sin elegir** de su pestaña a 56 dp, blanco `@24 %` (opacidad 0,24). Ningún otro texto ni acción; el catálogo no suma claves (AC-APP-02-08, P-05).

### 3.7 Controles propios

| Control | Diseño |
|---|---|
| `TabularNumber : VisualElement` (ARCHITECTURE §7.8) | **Un elemento por número.** Dibuja cada carácter en una celda con `MeshGenerationContext.DrawText(string, Vector2, float, Color, FontAsset)` (firma confirmada en S-0, RA-18 cerrado). Ancho de celda = avance máximo de `0`-`9` en el `FontAsset` y el tamaño resueltos (`resolvedStyle.unityFontDefinition.fontAsset`, `resolvedStyle.fontSize`); `:` y `.` en celdas de 0,32 em y `-` con su avance real en la fuente (≈ 0,37 em en Barlow SC, así `--` se lee como dos guiones; DN-08 b). Caja de alto 1 em con la **banda de mayúsculas centrada** (con `faceInfo.capLine` y la línea de base del `FontAsset`; en Barlow SC la línea de base queda a ≈ 0,85 em, DN-08 a). Cadenas de un carácter precalculadas: actualizar no asigna memoria. Fija su propio `style.width/height` cuando cambia el largo o el tamaño de letra; si cambia el largo por un dato nuevo, el ancho interpola 180 ms (`DigitRoll.WidthAt`) con las celdas pegadas a la derecha (DN-08 c), en un hijo absoluto del `FitBox` (no reacomoda a los vecinos). `SetValue(texto, valor)` arma un `DigitRollPlan`; mientras dura (180 ms) se registra en `MotionDriver` y llama `MarkDirtyRepaint()`; si llega otro valor, el rodillo en curso salta al final y arranca el nuevo (AC-BLE-11-11). Expone `Text` (el valor que muestra, para `UiQuery`, QA-G2-14) y `GetCellRect(i)` para los tests de AC-APP-05-04. Sin `FontAsset` (slot vacío) cae a un `Label` interno y avisa `[SLOT] ui.font.numeric` una vez |
| `FitBox : VisualElement` | Equivalente de `FittedBox(fit: scaleDown)` (`device_screen.dart:321-323`, `heart_rate_card.dart:163-166`): su hijo se mide a tamaño natural (posición absoluta) y, si no entra, se escala con `style.scale` (sin *relayout*) hasta entrar, con alineación central o a la izquierda. **Sin tope inferior** (D-10). Cuando un dato nuevo cambia la escala necesaria, la escala sigue un `CriticalSpring` ω 12 (DN-15); el primer cálculo, un redimensionado, un cambio de letra y reducir movimiento la aplican en el acto. `FitScale` (la escala destino) visible para los tests (AC-BLE-11-06, AC-APP-05-05) |
| `IconView` | `Label` con `.rm-icon` (y `.rm-icon--filled`) cuyo texto es el codepoint de `Icons`. Caja cuadrada del tamaño del ícono, `flex-shrink: 0`. Si el `FontAsset` de íconos falta o no tiene el codepoint, dibuja el cuadrado redondeado genérico de ASSET_SLOTS §4.4 con `generateVisualContent` + `Painter2D` en el `color` resuelto y avisa `[SLOT] ui.icons` una vez (D-07) |
| `RmButton` | Fábrica de `Button` con `.rm-button` + variante (`--primary`, `--tonal`, `--outline`, `--text`, `--danger`) y, en este orden, `.rm-button__state` (capa de estado absoluta, `inset 0`, `pickingMode Ignore`, DN-04), `.rm-button__icon`, `.rm-button__label .rm-type-button` (`white-space: normal`, centrada, DN-20) y `.rm-focus-ring > .rm-focus-ring__halo` (absolutos, no ocupan lugar, DN-05). Alto de contrato como **`min-height`** (DN-20): primario 48, tonal y contorno 44, texto 40; área táctil ≥ 48 con *padding* (ART_BIBLE §3.6); `flex-shrink: 0`. A 1,3× un texto largo pasa a dos líneas y el botón crece. `Space` también activa (AC-APP-05-07) |
| `IconButton` (DN-18) | `Button` `.rm-icon-button`: caja 48 × 48, ícono 24, `.rm-button__state` circular de 40 y anillo de foco. Lo usan `#sim-close` y los ± del panel |
| `SwitchToggle` (DN-17) | `Toggle` con el *checkmark* oculto y `.rm-switch__track > .rm-switch__thumb` (pista 40 × 24, perilla 20 × 20); el aspecto y sus transiciones son de WP-D (`:checked`) |
| `VerticalFadeTexture` (DN-06) | `Texture2D` de 1 × 64 RGBA32, blanca con alfa `smoothstep` (1 arriba → 0 abajo), *clamp*, bilineal, creada por código al armar el shell (sin asset, 256 B) y destruida al disponerlo. `ShellHost` la asigna como `style.backgroundImage` de cada `.rm-card__glow` y de `.rm-tab-bar__sheen`; el tinte (`-unity-background-image-tint-color`), el tamaño y el alto van en USS (WP-D). El blanco sale de `Color.white` (no es un color literal para `ColorSourceTests`) |
| `Spinner` | Arco de 270° con `Painter2D`, 16 dp, trazo 2 con extremos redondeados; una vuelta cada 900 ms lineal por `MotionDriver`; color `accent.primary` en la tarjeta y `text.primary` en el botón primario |
| `TextClamp` | Corta un `Label` a N líneas: si `MeasureTextSize` para su ancho pasa de N líneas, busca por bisección el prefijo más largo que entra con "…". Se recalcula con `GeometryChangedEvent` y al cambiar el texto |
| `FocusHighlight` | Pone `.rm-keyboard-nav` en la raíz al usar Tab o flechas y la saca con el primer toque o clic: el anillo de foco aparece solo navegando con teclado, como Flutter en escritorio. Anillo también en pestañas, filas `hrm-device-*`, chips, llave e interruptor (DN-05) |

### 3.8 Binding manual y ciclo de vida de las vistas

```csharp
namespace RowMate.Presentation.Binding
{
    // Suscribe al adjuntarse al panel y libera al quitarse (ARCHITECTURE §5). Sin reflexión.
    public static class ViewBinding
    {
        public static void Bind<T>(this VisualElement view, IReadOnlyObservable<T> source, Action<T> render);
        public static void Bind<T>(this VisualElement view, IEventSource<T> source, Action<T> onEvent);
    }
}
```

- `Bind` registra `AttachToPanelEvent` (suscribe con `emitCurrent: true`) y `DetachFromPanelEvent` (dispone). Como las pantallas ocultas siguen en el panel (`display: none` no las quita), siguen suscriptas: la grilla de Dispositivo está al día al volver (AC-APP-01-05).
- Cada `render(state)` compara con el estado anterior campo por campo y toca solo lo que cambió (texto, `SetEnabled`, clases). Los textos salen de los modelos ya localizados; las vistas también se suscriben a `AppContext.Texts` y reconstruyen sus modelos al cambiar el idioma (AC-APP-09-05).
- Si un `render` lanza, `ObservableValue` lo registra con el tag `STATE` y los demás suscriptores siguen (QA-V-01).

### 3.9 Motion, frecuencia de cuadros y reducir movimiento

```csharp
namespace RowMate.Presentation.Motion
{
    public interface IMotion { bool Tick(double nowSeconds); }   // false = terminó

    // Un solo reloj para todas las animaciones de la UI: IClock.MonotonicSeconds (UnityScheduler en la app,
    // FakeScheduler en tests y arnés). Lo avanza AppRoot.Update o el host de pruebas.
    public sealed class MotionDriver
    {
        public MotionDriver(IClock clock, FramePacingPolicy pacing, MotionSettings settings);
        public void Add(IMotion motion);          // pide 60 fps mientras haya algo activo
        public void Tick();                       // sin asignaciones (arreglo propio)
        public MotionSettings Settings { get; }   // ReduceMotion, desde AppContext.Accessibility
    }
}
```

| Animación | Implementación | Con reducir movimiento (AC-APP-05-08) |
|---|---|---|
| Rodillo de dígitos (§2.4 de ANIMATION_BIBLE, §5.1 de las notas) | `TabularNumber` + `DigitRoll` (curvas por celda, ancho interpolado si cambia el largo) | fundido de 80 ms |
| Destello del acento y brillo al llegar un dato **que cambió el texto** (§5.2, DN-07) | `AccentFlash` + `AccentPulse`: borde (`style.borderTopColor` con alfa `k`) y `.rm-card__glow` (opacidad 0,14·`k`) | igual (es opacidad) |
| Corazón: latido (§2.9, §5.3) | `HeartbeatAnimator` (escala desde el centro con `HeartbeatClock`) | quieto |
| Corazón: pleno ↔ 40 % (DN-13) | `HeartbeatAnimator`, opacidad en línea, 160 `ease.move` | igual |
| Cambio de pestaña (§2.1, §5.4) | `TabTransition`: saliente opacidad 1→0 y x 0→−12·dir en 120 ms (`ease.exit`); entrante 0→1 y +12·dir→0 de 80 a 280 ms (`ease.enter`); píldora con `CriticalSpring` ω 22 en x, conserva la velocidad si se reinterrumpe; ícono 1→1,12 (90) →1 (260); **color de ícono y etiqueta por código** en 160 `ease.move` y estilo en línea borrado al terminar (DN-11); el glifo cambia en el cuadro 0 | fundido sin desplazamiento; píldora en el destino en el cuadro 0; sin escala del ícono |
| Vista conectada ↔ búsqueda (§5.5, DN-12) | `ViewSwap` (§3.3) | solo opacidad, 0-120 y 120-240, sin escalonado (el destello sí) |
| Llave y panel (§5.7, DN-14) | `PopTransition` (§3.5) | solo opacidad |
| Escala de `FitBox` al cambiar el largo (§5.9, DN-15) | `CriticalSpring` ω 12 | en el acto |
| Tarjeta PULSO y alto de la grilla (§5.9, DN-16) | `MetricGridView` | fundido |
| Cuerpo de la tarjeta del pulsómetro (§5.6) | `HeightTween` + fundido | sin animación |
| Spinner | rotación continua (900 ms por vuelta) | sigue (indica espera, no es decorativo) |

- **Por qué por código y no con transiciones de USS (D-14):** con un reloj inyectable los tests PlayMode y los videos del arnés avanzan cuadro a cuadro y salen idénticos en cada corrida; las transiciones de USS corren con el reloj real del panel. USS queda solo para micro-interacciones de *hover*, presionado, foco y el interruptor (04-design-notes §5.8), en `Motion.uss` (WP-D). El fundido de color de la pestaña **ya no** va en `Motion.uss` (DN-11): va por código para que VID-02 sea determinista.
- **Nada se anima antes del primer cuadro presentado** (04-design-notes §5.0, AC-APP-01-07): `MotionDriver` arranca "desarmado" y el estado inicial (vista, colores, corazón, píldora) se aplica tal cual; se arma después del primer `Tick` con la UI dibujada.
- **`MotionDriver.HasFiniteMotions`**: verdadero mientras haya alguna animación con fin (todas salvo latido y *spinner*); lo usan `UiTestHost.Settle()` y las reglas de toma del arnés (DN-21).
- **`FramePacer`** aplica `FramePacingPolicy.RenderFrameInterval` a `OnDemandRendering.renderFrameInterval` (1 = 60 fps, 2 = 30 fps); `Application.targetFrameRate = 60`. Piden 60 fps: `MotionDriver` con algo activo, el corazón visible y latiendo, un desplazamiento con inercia o un dedo apoyado (y 500 ms después). Con `GammaRt`, el presentador presenta en cada cuadro dibujado.

### 3.10 Idioma, escala de letra, foco de teclado y "Atrás"

- **Idioma** (F-APP-09): `LanguageState(ISystemLocale.PreferredLanguageTags)` al arrancar; `AppRoot.OnApplicationFocus(true)` vuelve a leer la lista y llama a `AppContext.RefreshSystemSettings` (Android: cambiar el idioma en Ajustes y volver, AC-APP-09-05). `Texts` cambia y cada vista rearma sus textos; nada más se reinicia. Sin selector ni memoria (AC-APP-09-04). Windows pide cerrar sesión para cambiar el idioma de pantalla (P-07). En el Editor, menú `RowMate/Simular idioma/…` (EditorPrefs) y, en tests, `FakeSystemLocale`. Los textos del simulador siguen en español (Q-03).
- **Escala de letra** (AC-APP-05-05, P-03): `IAccessibilitySettings.Current.TextScale` → `TextScaleSteps.Quantize` → clase `rm-text-scale-<n>` en `#app-root`; las reglas generadas de `Typography.uss` agrandan cada `.rm-type-*`. `FitBox` achica los números grandes antes de desbordar. Windows: `HKCU\Software\Microsoft\Accessibility\TextScaleFactor` (si el legacy lo respeta se verifica en equipo, D-23).
- **Foco y teclado** (AC-APP-05-07): todos los accionables son `focusable` (los `Button` y `Toggle` lo son), en orden de árbol: contenido de la pestaña, barra, capa del simulador. UI Toolkit recibe Tab/flechas y Enter (`NavigationSubmitEvent`) del Input System (`activeInputHandler: 1`, ya aplicado); `RmButton` agrega `Space`. El anillo (`focus.ring` 2 dp + halo) es `.rm-focus-ring`, visible con `.rm-keyboard-nav .rm-button:focus > .rm-focus-ring`.
- **"Atrás"** (AC-APP-02-06): en Android, la tecla Atrás llega como `Escape` del Input System; `AppRoot.Update` la lee (`Keyboard.current?.escapeKey.wasPressedThisFrame`) y llama a `ShellHandle.HandleBack()`. `BackNavigation` decide: en la It. 2 siempre se está en la raíz (sin páginas ni diálogos), así que llama a `IBackButtonPolicy.OnBackAtRoot()` una vez y no cambia de pestaña. La política de Android hace `moveTaskToBack(true)` en API ≥ 31 (queda en segundo plano, como cualquier app desde Android 12) y `Application.Quit()` antes (D-09, DV-06: en Android 8-15 el legacy termina la app; el cambio está aceptado por el PO). En Windows y en el Editor la política no hace nada (como el legacy). Pruebas: [p] `SHT.BackAtRootGoesToThePolicyOnce` (política falsa, sin cambio de pestaña) y [u] `SMT.BackButtonPolicyByApiLevel` / `SMT.DesktopBackPolicyDoesNothing` (QA-G2-17); la tecla real en el teléfono, [Device-And].

### 3.11 Wakelock y ciclo de vida

- **Wakelock: no en la It. 2.** El legacy llama a `WakelockPlus.enable()` en `main()` (`main.dart:42`), pero la spec deja F-APP-14 para la 4a (§13). `AppRoot` no toca `Screen.sleepTimeout` (queda `SystemSetting`) ni llama a `SetThreadExecutionState`. El punto de enganche queda marcado en `AppRoot.Awake` (paso 8) y en ARCHITECTURE §13 (D-08).
- **Ciclo de vida**: `OnApplicationFocus(true)` relee idioma, letra y movimiento; `OnDestroy`/salida dispone todo sin avisarle nada al usuario (AC-APP-01-08). `runInBackground = true` en escritorio (ya aplicado): el simulador sigue con la ventana sin foco. El segundo plano de Android/iOS es de la 4a (ADR 017).
- **Rotar o redimensionar** (AC-APP-02-07): Unity no recrea la actividad al rotar; `UiSurface`/`PanelScaleController` adaptan tamaño y escala y `MetricGridView` recalcula columnas. Los servicios y los *stores* no se enteran.

### 3.12 Contrato USS entre WP-B y WP-D

**Orden de importación** (`Presentation/Styles/RowMateTheme.tss`, WP-B):

```css
@import url("unity-theme://default");                           /* controles de Unity (ScrollView, Toggle…) */
@import url("Tokens.uss");                                      /* GENERADO */
@import url("Typography.uss");                                  /* GENERADO */
@import url("Base.uss");                                        /* WP-B: estructura y valores de contrato */
@import url("../../SimulatorPresentation/Styles/SimulatorBase.uss");   /* WP-B */
@import url("Skin.uss");                                        /* WP-D: aspecto */
@import url("../../SimulatorPresentation/Styles/SimulatorSkin.uss");   /* WP-D */
@import url("Motion.uss");                                      /* WP-D: transiciones de USS */
```

Las hojas del simulador son solo estilos (sin textos ni código): incluirlas en el tema de todos los builds no agrega código del simulador (AC-BLE-13-02). Con la misma especificidad, lo que viene después gana: **WP-D sobrescribe en `Skin.uss` cualquier propiedad visual sin tocar los archivos de WP-B**.

**Reglas para las tres hojas.**

1. Ningún color literal (`#…`, `rgb(…)`, `rgba(…)`): todo color es `var(--rm-…)` de `Tokens.uss`; los "blanco al N %" que no son token se hacen con `opacity` (AC-APP-06-02; lo controla `ColorSourceTests` en dotnet, que lee todas las `.uss` y `.tss` del proyecto salvo `Tokens.uss`).
2. Nadie define `font-size` ni `-unity-font-definition` en un elemento que tenga una clase `.rm-type-*`: eso es de `Typography.uss` (si no, la escala de letra deja de funcionar). Cambiar un tamaño de letra = cambiar `Tokens.cs` y regenerar.
3. `Skin.uss` y `Motion.uss` **no cambian las propiedades de contrato** de la tabla de abajo (las verifican los tests PlayMode); si WP-D necesita cambiar una, se pide al Architect y se actualiza el test.
4. Las clases de estado las pone el código (`.rm-tab--selected`, `.rm-hrm-card--<body>`, `.rm-metric--<métrica>`, `.rm-text-scale-<n>`, `.rm-reduce-motion`, `.rm-desktop`, `.rm-keyboard-nav`); las pseudo-clases `:hover`, `:active`, `:focus`, `:disabled`, `:checked` son de Unity. La opacidad del corazón no es una clase (DN-13: estilo en línea por código).
5. **Lo que pone el código en línea manda** sobre cualquier hoja (es la regla de UI Toolkit) y es contrato: color del borde superior de las tarjetas (alfa `k`), opacidad de `.rm-card__glow`, opacidad del corazón, `background-image` de `.rm-card__glow` y `.rm-tab-bar__sheen`, *paddings* del área segura y de la barra, posiciones y tamaños de la grilla, la píldora, la llave y el panel, y los colores de la pestaña **durante** su fundido (en reposo son de `Base.uss`). WP-D estiliza todo lo demás de esos elementos (tinte, alto y radios del brillo, aspecto de la píldora, etc.).

**Propiedades de contrato** (WP-B en `Base.uss` o por código; WP-D no las cambia):

| Elemento | Contrato | Fuente |
|---|---|---|
| `.rm-app`, `.rm-screen`, `.rm-appbar` | fondo `--rm-bg-app`; barra superior alto 56 + área segura superior (*padding* por código), sin borde ni sombra, título y acciones blancos | AC-APP-05-02, §3.1 |
| `.rm-card` (tarjetas del tablero y del pulsómetro) | fondo `--rm-surface-card`, `border-radius` 16, sin sombra, `border-top-width` 3, resto de bordes 0, *padding* 16. **Color del borde superior por código**: el de la métrica (pulsómetro `metric.hr`) con alfa **0,60 en reposo** y 1 → 0,60 en 300 ms en el destello (solo tablero; DN-07, DV-02) | AC-APP-05-02, AC-BLE-11-06, AC-HRM-13-02, AC-APP-08-02 |
| `.rm-card__glow` (`.rm-metric-tile__glow`, `#hrm-glow`) | absoluto arriba; `background-image` = `VerticalFade` y `opacity` = 0,14 · `k` por código (pulsómetro 0,084 fijo) | AC-APP-08-02, DN-06, DN-07 |
| `.rm-metric-tile__icon` / `__label` / `__value` | 14 / `.rm-type-label` 12 600 / `.rm-type-num-xl` 52; color de la métrica al 100 % (ícono y etiqueta de PULSO `--rm-metric-hr-hud`; valor de PULSO `--rm-metric-hr`); centrados | AC-BLE-11-03, -06, AC-APP-06-01 (DV-03) |
| Grilla | separación 12, márgenes 20 / 16, cabecera a 24, tarjeta del pulsómetro a 12 y **64 bajo ella** hasta el final del contenido (48 + 16); proporción 1,3 / 1,6 (por código) | AC-BLE-11-05, -07 |
| `.rm-device-header__icon`, `.rm-badge` | ícono 32 `--rm-accent-primary`; insignia: ícono 12, texto `.rm-type-body-xs`, `--rm-state-connected` sobre `--rm-state-connected-bg`, radio 12 | AC-BLE-11-02 |
| `#device-disconnect`, `#hrm-disconnect` | ícono 18, `--rm-state-danger-text` | AC-BLE-07-01, AC-HRM-13-06 |
| `#device-search` | relleno, ancho completo, `min-height` 48, ícono `bluetooth_searching`; spinner blanco 16 | AC-BLE-05-01 |
| `#device-search-hint` | `.rm-type-body-xs` (12), `--rm-text-tertiary`, centrado | AC-BLE-05-01 |
| `.rm-empty__icon`, `.rm-empty__title`, `.rm-empty__hint` | 48, blanco `@24 %`; título `.rm-type-body` `--rm-text-secondary` (DV-05); ayuda `.rm-type-body-xs` centrada `--rm-text-tertiary` | AC-BLE-05-02 |
| `.rm-text--tertiary` | `--rm-text-tertiary` (54 %); 38 % solo para `:disabled` | AC-APP-05-06, D-ART-03 |
| `:disabled` | opacidad 0,38, sin transición | AC-HRM-13-04 |
| Botones (`.rm-button`, `.rm-icon-button`, `.rm-chip`) | alto como `min-height` (48 / 44 / 40; ícono 48 × 48; chip 32), etiqueta `.rm-type-button`, `flex-shrink: 0` | ART_BIBLE §3.6, DN-20 |
| `.rm-hrm-card__heart` | 14, `--rm-metric-hr`; opacidad 1 conectado y 0,40 si no, **por código** | AC-HRM-13-02, DN-13 |
| `#hrm-title` | `.rm-type-label`, `--rm-metric-hr-hud` | AC-HRM-13-02 (DV-03) |
| `#hrm-bpm`, `#hrm-bpm-unit` | `.rm-type-num-xl` `--rm-metric-hr`; unidad `.rm-type-body-m` (14) en `--rm-metric-hr-hud`, alineada por la línea de base (DN-09) | AC-HRM-13-06 (DV-04, QA-G2-08) |
| `#hrm-connected-name`, `#hrm-remembered-text` | `.rm-type-body-m` (14), blanco 70 % (`--rm-text-secondary`); el nombre, una línea con "…" | AC-HRM-13-06, -03 |
| `#hrm-watch-hint` | `.rm-type-body-xs` (12), `--rm-text-tertiary` | AC-HRM-13-03 |
| `#hrm-error` | `.rm-type-body-xs` (12), `--rm-state-danger-text`, 3 líneas | AC-HRM-13-07 |
| `.rm-hrm-device-row__subtitle` | `.rm-type-caption` (11), `--rm-text-tertiary` | AC-HRM-13-05 |
| `.rm-tab-bar` | alto 64 + área segura inferior (*padding* por código), `--rm-surface-nav`, borde superior 1 `--rm-border-subtle`; píldora 56 × 32 detrás del ícono, etiqueta debajo; en reposo no elegido `--rm-text-secondary`, elegido `--rm-accent-primary` (durante el cambio, por código) | AC-APP-02-03, ART_BIBLE §3.11 |
| `.rm-type-label` | `letter-spacing: 8px` (= 0,08 em; generado, QA-G2-02) | AC-BLE-11-06, AC-APP-05-05 |
| `.rm-sim-ribbon` | esquina superior izquierda **de la pantalla** (centro en 24, 24), 45°, `--rm-dev-simulator` + `--rm-dev-simulator-text`; último hijo de la capa, siempre visible, sin eventos | AC-BLE-16-01 (DV-01) |
| `#sim-open`, `#sim-panel` | llave 40, a 16 de la derecha y 16 sobre la barra real; panel 280 de ancho, alto máximo por código (D-26) | AC-BLE-16-02, -03 |
| `.rm-placeholder__icon` | 56, opacidad 0,24 | AC-APP-02-08 |
| `ScrollView` | `Scroller` vertical superpuesto (`position: absolute`) | AC-BLE-11-05 (ancho de la grilla) |

**Clase de texto por elemento** (= 04-design-notes §2.3, versión corregida por el gate). WP-B las pone por código; los tamaños salen solo de `Typography.uss` (regla 2):

| Elemento | Clase | Color |
|---|---|---|
| `.rm-appbar__title`, `#device-name` | `.rm-type-title` (20/600) | `text.primary` |
| `.rm-badge__label` | `.rm-type-body-xs` | `state.connected` |
| `.rm-metric-tile__label`, `#hrm-title`, `.rm-tab__label` | `.rm-type-label` (12/600, 0,08 em) | métrica / `metric.hr.hud` / pestaña |
| `.rm-metric-tile__value`, `#hrm-bpm` | `.rm-type-num-xl` | métrica / `metric.hr` |
| `#hrm-nosensor-text`, `.rm-sim-panel__header` (título) | `.rm-type-title-s` | `text.primary` |
| `#hrm-watch-hint`, `#device-search-hint`, `.rm-empty__hint` | `.rm-type-body-xs` | `text.tertiary` |
| `#hrm-error` | `.rm-type-body-xs` | `state.dangerText` |
| `#hrm-connected-name`, `#hrm-remembered-text` | `.rm-type-body-m` | `text.secondary` |
| `#hrm-bpm-unit` | `.rm-type-body-m` | `metric.hr.hud` (DV-04, QA-G2-08; par N13, 4,61:1) |
| `#hrm-connecting-text`, `.rm-sim-stepper__label`, rótulo de `#sim-rowing` | `.rm-type-body-m` | `text.primary` |
| `#hrm-searching-text` | `.rm-type-body-s` (13) | `text.tertiary` |
| `.rm-hrm-device-row__title`, `.rm-empty__title` | `.rm-type-body` (15) | `text.primary` / `text.secondary` |
| `.rm-hrm-device-row__subtitle` | `.rm-type-caption` (11) | `text.tertiary` |
| `.rm-button__label`, etiqueta de `.rm-chip` | `.rm-type-button` | según la variante |
| `#sim-watts-value`, `#sim-spm-value`, `#sim-hour-value` | `.rm-type-num-stat` (16/700) | `text.primary` |
| `.rm-sim-ribbon__label` | **ninguna** (tamaño fijo; no escala con la letra del sistema; 10/700 lo pone WP-D en `SimulatorSkin.uss`) | `dev.simulatorText` |

Las etiquetas del catálogo llegan con su caja (`SPLIT 500m`, `PULSÓMETRO` por `ToUpperInvariant`): no hay transformación a mayúsculas en USS. Los `.rm-type-body-m`, `-body-xs` y `-button` existen desde WP-A porque WP-D0 suma sus tokens antes (DN-01, §6.1).

**Elementos que crea WP-B para que WP-D los estilice** (estructura de §3.2-§3.7; sin propiedades de contrato salvo las de la tabla): `.rm-button__state`, `.rm-tab__state`, `.rm-focus-ring__halo`, `.rm-card__glow` (incluido `#hrm-glow`), `.rm-tab-bar__sheen`, `.rm-switch__track`, `.rm-switch__thumb`, `.rm-icon-button`, `#device-views`.

**Lo que es de WP-D** (sin límite fuera de las tablas): colores y opacidades de cada estado (hover, presionado, foco) sobre las capas `__state`, radios de botones, chips e interruptor, el tinte, el alto y los radios del brillo (`.rm-card__glow`, `.rm-tab-bar__sheen`), el aspecto de los botones primario, tonal, contorno y texto, la píldora y la barra de vidrio, el anillo y el halo de foco, la cinta (10/700 sin tracking, 04-design-notes §4.8, dentro de su caja), la llave, la superficie y el aspecto del panel, la separación interna del panel y de la tarjeta del pulsómetro, las barras de desplazamiento, `letter-spacing` de etiquetas que no son `.rm-type-label`, y las transiciones de USS de `Motion.uss` (solo *hover*, presionado, foco e interruptor). Los números de motion que no son tokens están en `Core/Design/MotionSpecs.cs` (WP-A): para cambiarlos, pedido al Architect. Duraciones, curvas, colores y tamaños de letra son tokens (`Tokens.cs`, del Designer).

---

## 4. Arnés de capturas

### 4.1 Cómo funciona (cada pieza verificada en S-0)

1. **Entrada:** `Unity.exe -batchmode -projectPath <repo> -executeMethod RowMate.Snapshots.SnapshotCli.Run -logFile <log> [-snapshotFilter "CAP-01,CAP-1*,VID-*"] [-snapshotOut <carpeta>] [-snapshotPipeline GammaRt|Overlay]`, **sin `-quit`**. `Run()` registra una máquina de estados en `EditorApplication.update` y vuelve; al terminar llama a `EditorApplication.Exit(0)` (1 si una captura falló, 3 si venció el tope de 15 min). En batchmode sin `-quit` el Editor sigue vivo y `update` corre (S-0).
2. **Sin entrar a Play.** Un `UIDocument` en la escena vacía, en modo edición (`UIDocument` es `[ExecuteAlways]`), con una **copia** del `PanelSettings` del proyecto (tema `RowMateTheme.tss`), `targetTexture` = RT del tamaño en px del perfil y `scaleMode = ConstantPixelSize`, `scale = densidad`. UI Toolkit la dibuja en cada `update`; los cambios de estilo de un `update` se ven en el siguiente (S-0: una caja movida cuadro a cuadro aparece donde debe en los cuadros 10 y 40). No hay `AppRoot` ni `MonoBehaviour` de la app: el arnés usa `UiTestHost` (el mismo de las pruebas PlayMode), que arma la UI con `AppComposer` + `ShellHost` y un `FakeScheduler`, y llama a `MotionDriver.Tick()` en cada paso.
3. **Determinismo:** `FakeScheduler` (reloj de prueba, hora local fija), `SimulatorBackend.Create(noise: false)`, `FakeSystemLocale`, `FakeAccessibilitySettings`, `FakeDisplayMetrics` y los dobles `ControllableRowerService` / `ScriptedHeartRateService`. Las animaciones van por código con el mismo reloj (D-14); un video avanza el reloj 1/30 s por cuadro. **Regla de toma de una foto fija** (DN-21, 04-design-notes §7.1): `MotionDriver.HasFiniteMotions == false` (solo latido y *spinner*), corazón en reposo (`HeartbeatClock.IsAtRest`) y ninguna transición de USS en curso (cuenta `TransitionRunEvent` contra `TransitionEndEvent` + `TransitionCancelEvent`, con un tope de 1 s de tiempo real); el arnés avanza de a 1/60 s hasta cumplirla **sin cruzar la lectura siguiente**: con una lectura en `N` s, la toma cae en `[N + 0,55; N + 1)` s. Si no la cumple en esa ventana, la captura falla con el motivo (no se toma "lo que haya").
4. **Tubería de color:** la misma de la app. `GammaRt` (por defecto) → RT `R8G8B8A8_UNorm` + `forceGammaRendering = true`, y los bytes leídos **son** los de la pantalla. `Overlay` → RT `R8G8B8A8_SRGB` sin `forceGammaRendering` (S-0: da lo mismo que el *backbuffer* de un player en *Overlay*).
5. **Lectura:** `RenderTexture.active = rt; tex.ReadPixels(...)` en una `Texture2D` RGBA32 (lineal = sin conversión), recorte opcional al rectángulo de un elemento (`worldBound` × densidad + 8 dp de margen), `EncodeToPNG`. Los videos se escriben con `UnityEditor.Media.MediaEncoder` (H.264, 30 fps; S-0: MP4 escrito en batchmode). Si `MediaEncoder` falla, se guardan los cuadros como PNG numerados y se avisa.
6. **Cada captura deja evidencia:** `LayoutAudit` (desbordes y textos cortados), sondas de píxel con el color esperado y la diferencia, y una línea en `manifest.json` e `INDEX.md`.
7. **Modo simulador:** casi todas las capturas necesitan `ROWMATE_SIMULATOR` (cinta, panel, servicios simulados). `tools/unity/capture-snapshots.ps1` hace lo mismo que `run-unity-tests.ps1 -Simulator`: no arranca si `Temp/UnityLockfile` está tomado (código 10), `SimulatorDefine.EnableForTests` → arnés → **siempre** `RestoreAfterTests` en `finally` y verificación de `ProjectSettings.asset` (código 11 si no quedó igual). Sin el define el arnés solo hace CAP-22 y CAP-23 y lo dice. Una sola instancia de Unity: el script corre las tres invocaciones en serie.

### 4.2 API

```csharp
namespace RowMate.Tests.UiSupport
{
    public readonly struct DeviceProfile
    {
        public static readonly DeviceProfile TV = new DeviceProfile("TV", 1080, 2400, 2.625);     // 411 × 914 dp
        public static readonly DeviceProfile TH = new DeviceProfile("TH", 2400, 1080, 2.625);     // 914 × 411
        public static readonly DeviceProfile L640 = new DeviceProfile("L640", 640, 360, 1);
        public static readonly DeviceProfile P800 = new DeviceProfile("P800", 800, 360, 1);
        public static readonly DeviceProfile W1280 = new DeviceProfile("W1280", 1280, 720, 1);
        public static DeviceProfile Dp(double widthDp, double heightDp, double density = 1);     // 800 × 600 de LT-28, etc.
        public string Code { get; } public int WidthPx { get; } public int HeightPx { get; } public double Density { get; }
    }

    // QA-G2-05: el host no elige entre variantes fijas; recibe una fábrica. Cada fábrica crea servicios nuevos sobre el
    // reloj del host y su dueño (DeviceBackend.Dispose los libera).
    public delegate DeviceBackend HostBackendFactory(IScheduler scheduler, IAppLogger logger);

    public static class HostBackends
    {
        public static readonly HostBackendFactory TestDevices;    // ControllableRowerService + ScriptedHeartRateService ("Prueba")
        public static readonly HostBackendFactory Unavailable;    // HardwareBackend.Create (build normal de la It. 2, CAP-22)
#if ROWMATE_SIMULATOR
        public static HostBackendFactory Simulator(bool noise = false);   // SimulatorBackend.Create(scheduler, logger, noise) ("Sim")
        // LT-31 (= _pump con bleFactory, heart_rate_card_test.dart:51-56, :161-165): un RowingSimulator sin ruido compartido
        // por un ControllableRowerService y un SimulatedHeartRateService.
        public static HostBackendFactory TestRowerWithSimulatedHeartRate();
#endif
    }

    public enum SimulatorLayerMode { None, Full, ChromeOnly }      // Full exige SimulatorDeviceBackend; ChromeOnly: cinta y llave sin panel

    public sealed class UiHostOptions
    {
        public DeviceProfile Profile; public HostBackendFactory Backend = HostBackends.TestDevices;
        public SimulatorLayerMode SimulatorLayer;                  // sin el define, solo None
        public SafeInsets Insets;                                  // FakeDisplayMetrics (QA-G2-06); por defecto ninguno
        public IReadOnlyList<string> LanguageTags = new[] { "es-AR" }; public double TextScale = 1; public bool ReduceMotion;
        public bool ProfileTab; public UiColorPipeline Pipeline = UiColorPipeline.GammaRt;
        public DateTimeOffset StartUtc; public TimeSpan LocalOffset;
        public bool IsDesktop = true; public IBackButtonPolicy? BackButton;   // política falsa para SHT.BackAtRoot…
    }

    // Host de UI determinista sobre una RT. Lo usan PlayMode (con yield) y el arnés (con EditorApplication.update).
    public sealed class UiTestHost : IDisposable
    {
        // GameObject INACTIVO + AddComponent<UIDocument> + copia del panel + SetActive(true): la raíz existe al volver
        // (sonda §8.3, en modo edición y en Play); después AppComposer + ShellHost, igual que AppRoot.Start.
        public static UiTestHost Create(UiHostOptions options);
        public FakeScheduler Scheduler { get; }
        public AppContext App { get; }
        public DeviceBackend Backend { get; }
        public VisualElement Root { get; }
        public ControllableRowerService? TestRower { get; }        // Backend.Rower si es el remo de prueba
        public ScriptedHeartRateService? TestHeartRate { get; }
#if ROWMATE_SIMULATOR
        public SimulatorDeviceBackend? Simulator { get; }          // Backend si es el del simulador
#endif
        public void Advance(TimeSpan by);                          // avanza el reloj y corre MotionDriver.Tick
        public void Settle(TimeSpan? max = null);                  // = pumpAndSettle: de a 1/60 s hasta !HasFiniteMotions (tope 2 s; latido y spinner no cuentan)
        // = tester.tap con hit-test (QA-G2-07): el centro de worldBound tiene que estar dentro de la ventana y
        // Root.panel.Pick(centro) tiene que ser el elemento o un descendiente; si no, falla con "fuera de la ventana" o
        // "tapado por #<nombre>". Recién entonces manda NavigationSubmitEvent (un elemento deshabilitado no responde).
        public void Tap(string elementName);
        public void TapText(string text);                          // ídem sobre el elemento que muestra ese texto
        public void TapUnchecked(string elementName);              // sin hit-test; solo para lo que el legacy hace por API (hoy, nada)
        // = ensureVisible: desplaza el ScrollView ancestro hasta que el elemento quede entero en la zona visible NO tapada
        // (viewport ∩ por encima de #tab-bar y de lo que la capa del simulador tape). ScrollView.ScrollTo no alcanza: deja
        // el elemento al pie del viewport, debajo de la barra absoluta (sonda §8.3).
        public void ScrollTo(string elementName);
        public void ScrollToEnd(string scrollViewName);            // CAP-04b
        public void Resize(DeviceProfile profile);                 // recrea la RT sin rearmar la UI (AC-APP-02-07)
        public void SetInsets(SafeInsets insets);                  // FakeDisplayMetrics + ShellLayout
        public void SetLanguage(params string[] tags);             // FakeSystemLocale + RefreshSystemSettings
        public VisualElement Find(string elementName);
        public string? TextOf(string elementName);                 // Label.text o TabularNumber.Text (QA-G2-14)
        public bool IsTextVisible(string text);                    // ≈ find.text: Labels y TabularNumber visibles y dentro de la ventana
                                                                   // (para skipOffstage: false del legacy, Find/TextOf sin mirar la posición)
        public Texture2D ReadPixels(Rect? cropPx = null);
        public LayoutAuditResult Audit();
        public void Dispose();
    }
}

namespace RowMate.Snapshots
{
    public static class SnapshotCli
    {
        public static void Run();                                   // -executeMethod
        [MenuItem("RowMate/Capturas/Generar todas")] public static void RunFromMenu();
        [MenuItem("RowMate/Capturas/Generar seleccionadas…")] public static void RunFiltered();
    }

    public sealed class SnapshotScenario
    {
        public string Id; public string FileStem; public UiHostOptions Host; public string State;   // texto de §7.3 de la spec
        public Action<ScenarioScript> Arrange;                      // acciones con el reloj de prueba
        public IReadOnlyList<CropSpec> Crops;                       // vacío = pantalla completa; "a", "b"… = varios recortes
        public IReadOnlyList<PixelProbe> Probes;
        public VideoSpec? Video;                                    // fps, segundos, acciones por instante
        public IReadOnlyList<string> MustNotOverflow;              // elementos que el audit revisa
    }

    public static class SnapshotCatalog { public static IReadOnlyList<SnapshotScenario> All { get; } }
}
```

`PixelProbe`: `Point(nombre, elemento, punto relativo, color esperado, tolerancia)` lee un píxel del fondo; `Dominant(nombre, elemento, color esperado, tolerancia)` toma el color más frecuente que no es el fondo dentro del elemento (el trazo de un número, AC-APP-06-01 "medido sobre el trazo del valor").

### 4.3 Catálogo: cada captura y video del PO → escenario

Tiempo de prueba: inicio `2026-10-27 13:45` hora local (así `Hora escena` + → `14:00`). "Sim" = `HostBackends.Simulator(noise: false)` con `SimulatorLayerMode.Full`; "Prueba" = `HostBackends.TestDevices` (`ControllableRowerService` + `ScriptedHeartRateService`) con `SimulatorLayerMode.ChromeOnly`: cinta y llave como en un build de simulador, sin panel (el panel exige el *backend* del simulador; QA-G2-05). Todas las acciones de UI son toques con *hit-test* (`Tap` falla si el elemento está tapado o fuera de la ventana; antes, `ScrollTo`), y cada foto se toma con la regla de toma de §4.1. Las sondas de aspecto del Designer (04-design-notes §7.2) se suman al catálogo en WP-D.

| ID | Archivo (`snapshots/`) | Perfil · back · idioma | Cómo se arma | Sondas y auditoría |
|---|---|---|---|---|
| CAP-01 | `CAP-01-dispositivo-tv.png` | TV · Sim · es | `Advance(60 s)` | fondo, tarjeta y barra superior = `#0D1B2A` / `#1A2E45` / `#0D1B2A` ± 1; trazo de cada valor = color de su métrica ± 2; 4 tarjetas; `02:13`, `24.0`, `150`, `226m` |
| CAP-02 | `CAP-02-dispositivo-tv-en.png` | TV · Sim · en-US | ídem | textos en inglés; `SIMULADOR` y `Remo simulado` fijos |
| CAP-03 | `CAP-03-dispositivo-l640.png` | L640 · Sim · es | ídem | 2 columnas, proporción 1,3; sin desbordes |
| CAP-04a | `CAP-04a-dispositivo-w1280.png` | W1280 · Sim · es | `ScrollTo("hrm-search")`, `Tap("hrm-search")`, `Tap("hrm-device-SIM:HRM")`, `Advance(60 s)`, desplazamiento a 0 (DN-22, QA-G2-04) | 3 columnas (1,6); `SPLIT 500m` `SPM` `VATIOS` en la fila 1 y `DISTANCIA` sola a la izquierda en la 2; trazos de valor = color de su métrica ± 2; la tarjeta del pulsómetro empieza en y ≈ 666,7, debajo de la barra (656-720) |
| CAP-04b | `CAP-04b-dispositivo-w1280-hrm.png` | W1280 · Sim · es | ídem y `ScrollToEnd("device-connected-scroll")` ("al final del desplazamiento", spec §7.3) | `DISTANCIA` entera; tarjeta del pulsómetro entera **por encima** de la barra, corazón pleno, `142` `bpm`; trazo del pulso `#EF476F` ± 2; trazo de `bpm` `#FF5A7E` ± 3 (DV-04); línea de base de `bpm` = la de `142` ± 0,5 dp; 64 dp entre la tarjeta y la barra (AC-BLE-11-07) |
| CAP-05 | `CAP-05-dispositivo-th.png` | TH · Sim · es | `Advance(60 s)` | 3 columnas; sin desbordes |
| CAP-06 | `CAP-06-grilla-pulso.png` | TV · Prueba · es | remo `Connected` + lectura 23,5 spm, 149 W, 652 m, split 133 s, pulso 140 | 5 tarjetas; PULSO `140` en `#EF476F`; también AC-BLE-01-05 (`23.5`, `149`, `652m`) |
| CAP-07 | `CAP-07-valores-largos.png` | TV · Prueba · es | lectura: split 6001 s, 40,5 spm, 1999 W, 123 456 m | `100:01`, `40.5`, `1999`, `123456m`; `FitScale` < 1 donde haga falta; ninguno cortado |
| CAP-08 | `CAP-08-busqueda-tv.png` | TV · Sim · es | `Advance(5 s)`, `Tap("device-disconnect")` | sin `Desconectar`; `Buscar dispositivos` habilitado; estado vacío; ayuda al 54 % (sonda) |
| CAP-09 | `CAP-09-busqueda-l640-hrm.png` | L640 · Sim · es | desconectar el remo, `ScrollTo("hrm-search")`, `Tap("hrm-search")` | sin desbordes; `Buscar dispositivos` `:disabled`; fila `hrm-device-SIM:HRM` dentro del área de la tarjeta |
| CAP-10 | `CAP-10-tarjeta-sin-sensor.png` | TV · Sim · es (recorte `hrm-card`) | `Advance(5 s)` | corazón al 40 %, pista completa al 54 %, `Buscar` |
| CAP-11 | `CAP-11-tarjeta-buscando.png` | ídem | `Tap("hrm-search")` | spinner, `Buscando sensores...`, fila `Pulsómetro simulado` / `SIM:HRM` |
| CAP-12 | `CAP-12a-…png`, `CAP-12b-…png` | ídem | a) tras tocar la fila; b) + `Advance(60 s)` | a) `--` (dos guiones), corazón pleno y quieto; b) `142` en el lugar de `--`, corazón pleno; `bpm` en `#FF5A7E` ± 3 por la línea de base |
| CAP-13 | `CAP-13-tarjeta-reconectando.png` | ídem | conectar, `Advance(2 s)`, panel → `SimulateHeartRateDrop()`, `Advance(1 s)` | `Pulsómetro simulado · reconectando...`; `Conectar` `:disabled` |
| CAP-14 | `CAP-14a`…`CAP-14d-…png` | TV · Prueba (recorte) | a) `ConnectAsync("7E:1A:2B:3C", "Polar H10 7E1A2B3C")` y el servicio queda `Connecting`; b) recordado `Polar H10 7E1A2B3C`, desconectado, sin reintentar; c) conectar lanza `HrmIncompatibleException`; d) lanza un error con 300 caracteres | a) sin botones; b) `· no encontrado`, `Conectar` habilitado; c) texto incompatible en rojo; d) 3 líneas con "…" |
| CAP-15 | `CAP-15-tarjeta-remo-buscando.png` | TV · Prueba (recorte) | remo `Scanning` | `Buscar` `:disabled` |
| CAP-16 | `CAP-16-panel-p800.png` | P800 · Sim · es | `Tap("sim-open")` | panel 280 dp; `sim-close` dentro de la ventana; `150 W`, `24 spm`, `real`; desplazamiento interno |
| CAP-17 | `CAP-17-panel-p800-final.png` | P800 · Sim · es | abrir, `Tap("sim-hour-plus")`, desplazar el panel al final | `14:00`, `Hora real`, `Remando`, los dos botones de falla |
| CAP-18 | `CAP-18-panel-tv.png` | TV · Sim · es | abrir | el panel entra entero (sin desplazamiento posible) |
| CAP-19 | `CAP-19-panel-limites.png` | TV · Sim · es | `Advance(60 s)`, abrir, `sim-watts-plus` × 40, `sim-spm-plus` × 20, `Tap("sim-hour-minus")` hasta `0:00`, apagar `Remando`, `Advance(1 s)` | `500 W`, `40 spm`, `0:00`; grilla `--:--` / `0.0` / `0` y distancia congelada |
| CAP-20 | `CAP-20a`…`CAP-20d-…png`, `CAP-20e-barra-en.png` | TV · Sim (recorte `tab-bar`) | elegir cada destino; e) en inglés | píldora detrás del ícono elegido; etiquetas siempre visibles |
| CAP-21 | `CAP-21a-workout.png`, `-21b-rutinas`, `-21c-historial` | TV · Sim · es | elegir cada pestaña | título + ícono apagado; ningún otro texto (auditoría de textos del árbol) |
| CAP-22 | `CAP-22-build-normal-w1280.png` | W1280 · Unavailable · sin capa | — | vista de búsqueda vacía, `Sin sensor`, sin cinta ni llave. La ventana con su título `RowMate` la captura QA del build real (`tools/unity/capture-window.ps1`, [Device-Win]) |
| CAP-23 | `CAP-23-medicion-rt1.png` + `rt1-measurement.json` | 400 × 240 dp · las dos tuberías | §4.4 | píxel leído y contraste anotados al pie |
| CAP-24 | `CAP-24-letra-130.png` | TV · Sim · es · letra 1,3 | conectar el pulsómetro, `Advance(60 s)` | nada cortado; valores achicados |
| CAP-25 | `CAP-25-strava-5-destinos.png` | TV · Sim · es · `ProfileTab` | — | 5 destinos; `Perfil` último con `person`; Historial cuarto |
| VID-01 | `VID-01-tablero.mp4` | TV · Sim · es | conectar el pulsómetro, `Advance(60 s)`; graba 10 s | rodillo y destello **solo** en las tarjetas cuyo valor cambió (sin ruido: `DISTANCIA`); `02:13`, `24.0` y `150` quietos y sin brillo; la tarjeta del pulsómetro no destella; corazón a 142 (spec §7.3 corregida, QA-G2-10) |
| VID-02 | `VID-02-pestanas.mp4` | TV · Sim · es | graba 10 s: toca Workout (1 s), Rutinas (3,5 s), Historial (6 s), Dispositivo (8,5 s) | píldora, fundido |
| VID-03 | `VID-03-caida-remo.mp4` | TV · Sim · es | `Advance(60 s)`; graba 8 s: `SimulateRowerDrop()` a 1 s | búsqueda 3 s, vuelta en ceros, distancia que sigue |

Además, **sin pedido del PO**, el arnés escribe `AUX-*.png` útiles para QA: la tarjeta con `--` en inglés y las vistas de CAP-01 con reducir movimiento (deben verse iguales).

### 4.4 Medición de R-T1 (CAP-23, AC-BASE-05)

1. Escena de muestra (400 × 240 dp, densidad 1): rectángulo blanco con un rectángulo `--rm-glass-fallback` encima; rectángulo `--rm-surface-card` con un rectángulo `--rm-text-secondary` encima; un texto `text.secondary` de 15 dp sobre `surface.card`.
2. Se renderiza **dos veces**: `Overlay` (RT sRGB) y `GammaRt` (RT UNorm + `forceGammaRendering`). En cada una se lee el píxel central de cada muestra.
3. Contrastes con `Core.Design.Contrast` contra el píxel **medido**: `text.hudPrimary`, `metric.split.hud` y `metric.hr.hud` sobre el vidrio medido; `text.secondary` medido contra `surface.card`. Se comparan con lo que espera el test de dotnet (`CompositeSrgb` y `CompositeLinear`).
4. Se vuelve a renderizar con los valores escritos al pie y se guarda `CAP-23-medicion-rt1.png` (las dos mitades) y `rt1-measurement.json`:

```json
{ "pipeline": "GammaRt", "glassOnWhite": "#2A2F37", "textSecondaryOnCard": "#BBC1C8",
  "expectedSrgb": { "glassOnWhite": "#2A2F37", "textSecondaryOnCard": "#BAC0C7" },
  "expectedLinear": { "glassOnWhite": "#696A6B", "textSecondaryOnCard": "#DADBDC" },
  "contrast": { "text.hudPrimary/glass": 13.49, "metric.split.hud/glass": 7.00, "metric.hr.hud/glass": 4.51 },
  "verdict": "sRGB" }
```

Valores esperados por S-0: `Overlay` `#6A6A6C` / `#DADBDC` (lineal, R-T1 confirmado); `GammaRt` `#2A2F37` / `#BBC1C8` (sRGB, ± 1 de lo esperado). El arnés no decide nada: deja los números y QA cierra AC-BASE-05.

### 4.5 Salidas

| Archivo | Contenido |
|---|---|
| `snapshots/<ID>-<tema>.png`, `VID-*.mp4` | capturas y videos (los nombres de §4.3) |
| `snapshots/manifest.json` | por captura: id, archivo, perfil, px, dp, densidad, idioma, escala de letra, tubería, *backend*, estado (texto de la spec), auditoría (`overflows`, `clippedTexts`), sondas (nombre, esperado, medido, diferencia, pasa), duración, y `elements`: nombre y `worldBound` en dp de cada elemento con nombre de contrato visible (lo compara `ShellTests.EditModeCaptureMatchesPlayModeTree`, condición de D-24 / R2-09) |
| `snapshots/INDEX.md` | la misma información en una tabla, con enlace a cada archivo (el REPORT de la iteración enlaza este índice) |
| `snapshots/rt1-measurement.json` | §4.4 |

`SnapshotEvidenceTests` (dotnet, `Tests/EditMode/Project`) lee `manifest.json` si existe y falla si una sonda o una auditoría marcada en el catálogo no pasa (AC-APP-05-02, AC-APP-06-01, AC-BASE-05); sin capturas se ignora con el motivo. La carpeta se versiona (son entregables de la iteración, PNG chicos; los MP4 van por Git LFS, `.gitattributes` ya cubre `png` y suma `mp4`).

### 4.6 Cómo correrlo

```
# todo (≈ 2-4 min con la caché de Library; tres arranques de Unity en serie)
powershell -NoProfile -File tools/unity/capture-snapshots.ps1 -ArtifactsPath <scratch>/snap
# una parte
powershell -NoProfile -File tools/unity/capture-snapshots.ps1 -Filter "CAP-1*,VID-01" -ArtifactsPath <scratch>/snap
# lo mismo a mano (el define ya puesto con el menú RowMate/Modo simulador)
"C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe" -batchmode -projectPath <repo> ^
    -executeMethod RowMate.Snapshots.SnapshotCli.Run -snapshotFilter "CAP-01" -logFile <scratch>/snap.log
```

El Designer itera así (WP-D): edita `Skin.uss`/`Motion.uss` → corre el script con el filtro de lo que tocó → mira el PNG. Una corrida de pocas capturas tarda lo que tarda abrir Unity con la caché (≈ 15-25 s × 3).

---

## 5. Tests

### 5.1 Suites y comandos

| Suite | Dónde | Corre en | Comando |
|---|---|---|---|
| Core, App y reglas | `Tests/EditMode/**` (shared) | dotnet (net8.0) **y** Unity EditMode | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it2` · `tools/unity/run-unity-tests.ps1` |
| Simulador | `Tests/Simulator/**` (shared, `ROWMATE_SIMULATOR`) | dotnet **y** Unity EditMode con `-Simulator` | ídem; `tools/unity/run-unity-tests.ps1 -Simulator` (AC-BASE-08) |
| Unity EditMode con `UnityEditor` | `Tests/UnityEditMode/**` | Unity EditMode | `tools/unity/run-unity-tests.ps1` |
| UI sin simulador | `Tests/PlayMode/**` | Unity PlayMode | `tools/unity/run-unity-tests.ps1 -Platform PlayMode` |
| UI con simulador | `Tests/PlayModeSimulator/**` | Unity PlayMode con el define | `tools/unity/run-unity-tests.ps1 -Platform PlayMode -Simulator` |
| Capturas | `Snapshots/**` | Unity, modo edición | `tools/unity/capture-snapshots.ps1` (§4.6) |
| Humo IL2CPP (AC-BASE-01) | `ParitySmoke` en un player | Windows IL2CPP / Android | `tools/build/build.ps1 -Target Windows -Backend IL2CPP -SmokeTest` → `RowMate.exe -batchmode -rowmate-parity-smoke -logFile …`; sin el módulo, **NO VERIFICABLE** |

**Cómo son las pruebas de UI.** `UiTestHost` (§4.2) monta la UI real en una RT del tamaño pedido (640 × 360, 800 × 360, 800 × 600, TV…) con la densidad y el área segura del perfil, sin depender del monitor (spec §12.2).

- **Toques con *hit-test*** (QA-G2-07), como `tester.tap` del legacy: `Tap` verifica que el centro del elemento cae dentro de la ventana y que `panel.Pick(centro)` devuelve el elemento o un descendiente, y recién ahí manda `NavigationSubmitEvent` (un elemento deshabilitado no responde: P2 de QA, 0 clics deshabilitado y 1 habilitado). Sonda propia (§8.3): `Pick` devuelve la barra absoluta sobre un botón tapado (`b5 → tab`), `null` para un botón recortado por un `ScrollView` o fuera del panel, atraviesa una capa con `pickingMode Ignore` y ve el desplazamiento nuevo en el mismo cuadro. Así LT-27 ("`sim-close` dentro de la ventana"), LT-28 ("hay que desplazarse hasta `Buscar`") y LT-30 ("alcanzable desplazando") fallan si la UI no deja tocar, como en Flutter. `ScrollTo` deja el elemento entero **por encima** de la barra (no al pie del *viewport*).
- **Tiempo**: `host.Advance(…)` + `yield return null` (dos cuadros para *layout* y dibujo); después de un cambio que dispara una transición (vista conectada ↔ búsqueda, cuerpo de la tarjeta, pestaña, panel), `host.Settle()` antes de mirar (= `pumpAndSettle`).
- **Textos**: `TextOf` / `IsTextVisible` leen `Label.text` **y** `TabularNumber.Text` (los números del tablero y del pulso se dibujan con `DrawText`, sin `Label`; QA-G2-14): "se ve `--`", "se ve el bpm", `02:13`.
- **"Sin desbordes"** = `LayoutAudit` vacío, con **contenedores explícitos** (QA-G2-14, spec §0.3 "nada fuera de su tarjeta ni de la ventana"): la ventana (`#app-root`), cada `.rm-screen`, `.rm-appbar`, `.rm-card`, `.rm-tab-bar`, `.rm-sim-panel` y el `contentContainer` de cada `ScrollView`. Todo descendiente visible (`display` y `visibility`; la opacidad no cuenta) tiene que caer dentro de su contenedor explícito más cercano (± 0,5 dp); lo que está dentro del contenido de un `ScrollView` se compara con ese contenido (se alcanza desplazando), no con el *viewport*. Exentos por diseño: `.rm-focus-ring` y `__halo` (inset −4), `.rm-sim-ribbon` (rotada; la ventana la corta como a la `Banner`). Además: ningún texto de una línea (`white-space: nowrap`) más ancho que su caja sin "…", ningún texto multilínea más alto que su caja, ningún `TabularNumber` más grande que su caja, ningún `FitBox` sin entrar.

**Lo ignorado de la It. 1 que se activa.** `RowingFormatsTests.DeviceGridShowsTheTextsOnScreen`, `LanguageStateTests.NoSelectorOnScreen` y `LanguageStateTests.VisibleChangeWithoutRestart` necesitan la UI: siguen `[Ignore]` en el archivo compartido (dotnet no tiene UI), con el motivo cambiado a "se verifica en PlayMode: `DeviceScreenTests.GridTextsLikeImage01` / `LanguageOnScreenTests.*`", y esos tests de PlayMode llevan las mismas categorías. `AppIdentityTests` (AC-APP-16-01 a -04) y `LocaleResolverTests.NativeListOnDevices` (AC-APP-09-06) siguen NO VERIFICABLES hasta que QA los vea en equipo.

### 5.2 Matriz AC → test / captura

Abreviaturas: **[d]** dotnet + Unity EditMode · **[s]** dotnet + Unity EditMode con `-Simulator` · **[u]** Unity EditMode solo · **[p]** PlayMode · **[ps]** PlayMode con simulador · **[cap]** captura del arnés (y `SnapshotEvidenceTests` si tiene sondas) · **[eq]** equipo (QA). Clases: `SBS` SimulatedBleServiceTests, `SHS` SimulatedHeartRateServiceTests, `HRP` HeartRateProviderTests, `SPS` SimulatorPanelStoreTests, `SBK` SimulatorBackendTests, `DSF` DeviceSimulatorFlowTests, `DST` DeviceStoreTests, `DSR` DeviceScreenRulesTests, `HRS` HeartRateStoreTests, `HCR` HrmCardRulesTests, `SHL` ShellStoreTests, `APC` AppComposerTests, `UNV` UnavailableServicesTests, `SHT` ShellTests, `DSP` DeviceScreenTests, `HCS` HeartRateCardStatesTests, `THM` ThemeTests, `NBT` NormalBuildTests, `HCT` HeartRateCardTests (legacy), `SOT` SimulatorOverlayTests (legacy), `DSS` DeviceSimulatorTests, `LOS` LanguageOnScreenTests, `SMT` SimulatorModeTests, `BVT` BootstrapV3Tests, `FIT` FontAndIconAssetsTests, `SET` SnapshotEvidenceTests.

**F-APP-01 (8)**

| AC | Verificación |
|---|---|
| AC-APP-01-01 | [d] `SHL.InitialIndexIsDeviceAndNotPersisted` · [p] `SHT.StartsOnDeviceTab` · [eq] |
| AC-APP-01-02 | [s] `SBK.SimulatorReplacesBothServicesWithOneAthlete`, `SBK.HeartRateComesFromRowerPhysics` · [u] `SMT.OnlyDeviceBackendFactoryChoosesTheVariant` (estático: en el código que llega a un build, un solo `#if ROWMATE_SIMULATOR` elige servicios, el de `DeviceBackendFactory`; `SimulatorLayerFactory` solo agrega la capa; los `#if` de `Tests/UiSupport` y `Snapshots` no cuentan, nunca llegan a un build) |
| AC-APP-01-03 | [d] `UNV.RowerStartsAndStaysDisconnected` · [p] `NBT.NormalCompositionOpensOnSearchView`, `BootSceneTests.BootSceneStartsWithoutExceptions` (QA-G2-01, en las dos configuraciones) · [ps] `DSS.SimulatorOpensConnectedWithoutTouching` · [eq] |
| AC-APP-01-04 | [d] `APC.StartLaunchesAutoConnectWithoutWaiting`, `APC.AutoConnectFailureGoesOnlyToTheLog` (`[HRM] autoConnect: …`) · [s] `SHS.AutoConnectDoesNothing` · [ps] `DSS.CardOpensInNoSensor` |
| AC-APP-01-05 | [ps] `DSS.DeviceTabKeepsUpdatingWhileHidden` (10 s en Rutinas, +≈ 38 m, lectura del segundo actual) |
| AC-APP-01-06 | [p] `SHT.NoDebugMarksBesidesTheSimulatorRibbon` · [u] `BVT.ProductIsRowMate` · [cap] CAP-01, CAP-22 · [eq] |
| AC-APP-01-07 | [u] `BVT.SplashOffAndBackgroundIsBgApp` (parcial: configuración) · [p] `THM.SurfaceRtsStartCleared` (RT recién creada = `bg.app`) · S-B1 en el build de Windows: primer cuadro presentado = `bg.app` y latencia 0 (QA-G2-13, §3.1) · [eq] Windows/Android |
| AC-APP-01-08 | [d] `APC.DisposeLeavesNoTimers` · [s] `SBK.DisposeLeavesNoTimers` + LT-20 a LT-25 y LT-32 |

**F-APP-02 (8)**

| AC | Verificación |
|---|---|
| AC-APP-02-01 | [d] `SHL.FourDestinationsInOrderWithIcons` · [p] `SHT.TabBarHasFourDestinations` · [cap] CAP-20 |
| AC-APP-02-02 | [d] `SHL.ProfileOnlyWithStravaAndHistoryStaysThird` · [p] `SHT.FiveDestinationsWithStravaConfig` · [cap] CAP-25 |
| AC-APP-02-03 | [d] `SHL.SelectingTheSelectedDoesNothing` · [p] `SHT.SelectionShowsPillGlyphAndLabels` · [cap] CAP-20 |
| AC-APP-02-04 | [p] `SHT.ScrollPositionSurvivesTabSwitch` · [ps] `DSS.HrmSearchSurvivesTabSwitchAndExpiresAt10s` |
| AC-APP-02-05 | [d] `SHL.DeviceIconDependsOnSelectionOnly` · [p] `SHT.DeviceIconIgnoresRowerStatus` |
| AC-APP-02-06 | [u] `SMT.BackButtonPolicyByApiLevel` (regla), `SMT.DesktopBackPolicyDoesNothing`, `SMT.AppRootRoutesEscapeToHandleBack` (estático: `AppRoot.Update` lee `escapeKey` y llama a `HandleBack`) · [p] `SHT.BackAtRootGoesToThePolicyOnce` (política falsa, sin cambio de pestaña; QA-G2-17) · [eq] Android |
| AC-APP-02-07 | [p] `SHT.ResizeChangesColumnsWithoutRebuilding` · [ps] `DSS.ResizeKeepsConnectionCardAndTab` · [d] `RealtimeTimerQueueTests.JumpOfFiveSecondsFiresOnceAndReanchors` (el *loop* cortado al arrastrar la ventana no comprime lecturas; QA-G2-15) · [eq] |
| AC-APP-02-08 | [p] `SHT.PlaceholdersShowTitleAndDimIconOnly` · [cap] CAP-21 |

**F-APP-05 (9)**

| AC | Verificación |
|---|---|
| AC-APP-05-01 | [p] `THM.ThemeHasNoLightVariantAndIgnoresSystemTheme` (estático + PlayMode) · [eq] |
| AC-APP-05-02 | [p] `THM.BaseColorsReadFromPixels` (RT, ± 1) · [cap] CAP-01 + `SET.BaseColors` |
| AC-APP-05-03 | [u] `FIT.FontAssetsExistAndCoverTheirSets`, `FIT.IconFontHasEveryIconCodepoint` · [d] `IconsTests.CodepointsMatchThePinnedFile` · [cap] CAP-01 (NV "a medio vestir" solo si faltaran las fuentes, que están versionadas) |
| AC-APP-05-04 | [p] `THM.TabularCellsHaveFixedWidth` (`111` = `888`, `11:11` = `00:00`), `THM.UnchangedDigitsDoNotMove` (149 → 151) |
| AC-APP-05-05 | [d] `TextScaleStepsTests.*`, `TypographyUssSyncTests.ScaledClassesMatchTokens`, `TypographyUssSyncTests.LabelTrackingIsEightPx` · [p] `THM.TextScale130HasNoOverflow` (TV y L640), `THM.LabelTrackingIsEightHundredthsEm` (QA-G2-02) · [cap] CAP-24 · [eq] Android |
| AC-APP-05-06 | [p] `THM.TertiaryTextsUseTextTertiary` · [cap] CAP-08, CAP-10 + `SET.TertiaryTexts` |
| AC-APP-05-07 | [p] `THM.TabWalksActionableControlsWithFocusRing`, `THM.EnterAndSpaceActivate` · [eq] Windows |
| AC-APP-05-08 | [d] `DigitRollTests.ReducedMotionIsAnEightyMsFade` · [p] `THM.ReduceMotionChangesRollHeartAndTabs` · [eq] Android |
| AC-APP-05-09 | [p] `THM.PanelScaleEqualsDensity` (TV a 2,625 → 411 × 914 dp) · [eq] Windows 100 % y 150 %, Android |

**F-APP-06, F-APP-08, F-APP-19 (5)**

| AC | Verificación |
|---|---|
| AC-APP-06-01 | [d] `MetricPaletteTests.ContractHexes` · [p] `DSP.TileIconLabelAndAccentColors` (ícono y etiqueta al 100 %, PULSO en `metric.hr.hud`, borde al 60 %) · [cap] CAP-04a, CAP-04b, CAP-06 + `SET.MetricColorsOnValueStrokes` (± 2) |
| AC-APP-06-02 | [d] `ColorSourceTests.NoColorLiteralsOutsideTokens` (`.cs` de Presentation, Simulator.Presentation, Bootstrap y Snapshots; `.uss` y `.tss` salvo `Tokens.uss`) |
| AC-APP-08-01 | [d] `LegacyDiscardTests.NoMetricCardNorIntervalTilePort` · QA |
| AC-APP-08-02 | [p] `DSP.MetricTilesAndHrmCardShareTheCardContract` (fondo, radio, borde al 60 %, brillo, encabezado; solo el tablero destella) · [cap] CAP-04b |
| AC-APP-19-05 | [d] `ItTwoToolingTests.CiUnityRunsEditAndPlayMode` (estático) · [CI] NO VERIFICABLE sin licencia ni remoto |

**F-BLE-05, -07, -08, -10 (10)**

| AC | Verificación |
|---|---|
| AC-BLE-05-01 | [d] `DSR.ScanSpecSearchButton` · [p] `DSP.SearchButtonStatesWithTestRower` (spinner + `Buscando...` + ayuda) · [cap] CAP-08 |
| AC-BLE-05-02 | [d] `DSR.EmptyStateFromTheFirstFrame` · [p] `NBT.EmptyStateWithoutEverSearching` · [cap] CAP-08, CAP-22 |
| AC-BLE-05-03 | = AC-HRM-13-08: [ps] `HCT` LT-29 y LT-31 |
| AC-BLE-07-01 | [d] `DSR.DisconnectOnlyWhenConnected` · [p] `DSP.DisconnectActionVisibility` · [cap] CAP-01, CAP-08 |
| AC-BLE-07-02 | [s] `SBS.DisconnectStopsReadingsAtOnce` · [ps] `DSS.DisconnectShowsEmptySearch` |
| AC-BLE-07-03 | [s] LT-22 · [ps] `DSS.NoAutoReconnectAfterDisconnect` |
| AC-BLE-07-04 | [s] `SBS.DisconnectBeatsAPendingReconnect` |
| AC-BLE-08-01 | [s] `DSF.SimulatedDropShowsSearchAndSearchReconnects` · [ps] `DSS.DropShowsSearchForThreeSeconds` · [cap] VID-03 |
| AC-BLE-10-01 | [p] `SHT.DeviceIsTheFirstDestination` |
| AC-BLE-10-02 | = AC-HRM-13-08 (LT-31) |

**F-BLE-11 (11)**

| AC | Verificación |
|---|---|
| AC-BLE-11-01 | [d] `DSR.ViewForEveryStatus` · [p] `DSP.ViewSwitchesWithStatus` |
| AC-BLE-11-02 | [p] `DSP.HeaderIconNameAndBadge` · [cap] CAP-01 |
| AC-BLE-11-03 | [d] `DSR.TilesOrderLabelsIconsAndColors` · [p] `DSP.FourTilesInOrder` · [cap] CAP-01, CAP-06 |
| AC-BLE-11-04 | [d] `DSR.HeartRateTileOnlyWithFtmsPulse` · [p] `DSP.PulseTileComesAndGoes` (140 → 0) · [cap] CAP-06 |
| AC-BLE-11-05 | [d] `DSR.ColumnsAndAspectByWidth` (360, 411, 640, 641, 914, 1280) · [p] `DSP.ColumnsAtProfiles` · [cap] CAP-01, -03, -04a, -05 |
| AC-BLE-11-06 | [d] `AccentPulseTests.FromFullToSixtyIn300ms` · [p] `DSP.TileGeometryAndValueFits` (`123456m` a 411 dp: `FitScale` < 1, sin cortes), `DSP.AccentRestsAtSixtyAndFlashesOnlyWhenTheTextChanges` (DN-07) · [cap] CAP-07, VID-01 |
| AC-BLE-11-07 | [p] `DSP.ConnectedViewScrollsAsOneBlockWithMargins` (20 a los costados, 24 sobre la cabecera, 64 bajo la tarjeta del pulsómetro; al final del desplazamiento, la tarjeta sobre la barra; QA-G2-12) · [ps] LT-28 · [cap] CAP-04b |
| AC-BLE-11-08 | [d] `DSR.NoCaloriesTimeOrStrokes` · [p] `DSP.NoExtraMetrics` |
| AC-BLE-11-09 | [d] `DST.ZerosOnEveryConnect` · [ps] `DSS.ReconnectShowsZerosThenValues` · [cap] VID-03 |
| AC-BLE-11-10 | [s] `DSF.ReferenceValuesTable` (60 s, Suave, Medio, Fuerte, 30 y 500 W, Remando apagado) · [ps] `DSS.GridShowsReferenceValuesAt60s` · [cap] CAP-01, CAP-19 |
| AC-BLE-11-11 | [d] `DigitRollTests.EndsOnTheExactValue`, `DigitRollTests.OnlyChangedCellsRoll` · [p] `DSP.RollEndsOnTheExactValueAndRestarts` · [cap] VID-01 |

**F-BLE-13 (5)**

| AC | Verificación |
|---|---|
| AC-BLE-13-01 | [u] `SMT.SimulatorMenuExists`, `SMT.NonDevelopmentBuildWithSimulatorFails` (`SimulatorBuildGuard.Evaluate`), `SMT.BuildCliForcesDevelopmentWithSimulator` · [d] `ItTwoToolingTests.BuildScriptForcesDevelopmentWithSimulator` · QA |
| AC-BLE-13-02 | [p] `NBT.NormalCompositionHasNoSimulatorLayer` · [u] `SMT.BuildOutputInspectorFindsSimulatorTraces` (carpeta falsa con un `RowMate.Simulator.dll` y la cadena en UTF-16) · [d] `ItTwoToolingTests.BuildScriptInspectsNormalBuilds` · build normal de `build.ps1`: `BuildOutputInspector` sin rastros (QA-G2-17) · [cap] CAP-22 · QA (lo mismo a mano en el build) · [eq] |
| AC-BLE-13-03 | [s] `SBK.SimulatorReplacesBothServicesWithOneAthlete` |
| AC-BLE-13-04 | [ps] `DSS.SameTreeWithSimulatedAndTestDevices` (mismos nombres y clases) · [d] `AsmdefLintTests` (Presentation no referencia Simulator) · QA |
| AC-BLE-13-05 | [d] `UNV.ScanAndSearchDoNothing` · [p] `NBT.NormalBuildSearchDoesNothing` · [cap] CAP-22 · [eq] |

**F-BLE-15 (10)**

| AC | Verificación |
|---|---|
| AC-BLE-15-01 | [s] LT-20, LT-22, `SBS.NameOnlyWhileConnected` |
| AC-BLE-15-02 | [s] LT-20, `SBS.StartConnectsOnlyOnce`, `SBS.AdapterIsAlwaysOn` |
| AC-BLE-15-03 | [s] LT-20 |
| AC-BLE-15-04 | [s] LT-22, `SBS.ConnectReconnectsAtOnce`, `SBS.StopScanDoesNothing` |
| AC-BLE-15-05 | [s] `SBS.DisconnectStopsReadingsAtOnce` |
| AC-BLE-15-06 | [s] LT-21, `SBS.SimulateDisconnectWhenNotConnectedDoesNothing` |
| AC-BLE-15-07 | [s] `SBS.ConnectWhileConnectedDoesNotRepeat` |
| AC-BLE-15-08 | [s] `SBS.NoRawBytesNorDeviceList` |
| AC-BLE-15-09 | [s] `DSF.PhysicsDoesNotRestartOnReconnect` (`37m` → `41m`, 11 s) |
| AC-BLE-15-10 | [ps] `DSS.OpensConnectedAndDropShowsSearch` · [cap] CAP-08, VID-03 |

**F-BLE-16 (12)**

| AC | Verificación |
|---|---|
| AC-BLE-16-01 | [ps] `SOT.RibbonIsAlwaysOnTop` (último hijo de la capa, que es la última de la raíz; centro en (24, 24) desde la esquina de la pantalla también con área segura) · [cap] CAP-01 |
| AC-BLE-16-02 | [ps] LT-26, `SOT.WrenchPositionAndToggle` · [cap] CAP-01, CAP-18 |
| AC-BLE-16-03 | [s] `SPS.MaxHeightRule` (360 → 248, 914 → 802, 200 → 120; con márgenes 24 / 48: 914 → 730, D-26) · [ps] LT-27, `SOT.InsetsAreCountedOnce` (QA-G2-06) |
| AC-BLE-16-04 | [ps] LT-26, `SOT.PanelContentAndOrder` · [cap] CAP-16, -17, -18 |
| AC-BLE-16-05 | [s] `SPS.PresetsSetTargetsOnly` · [ps] LT-26 |
| AC-BLE-16-06 | [s] `SPS.WattsAndSpmStepsAndLimits` · [ps] LT-26 |
| AC-BLE-16-07 | [s] `SPS.HourStepsWrapAndRealHour` (13:45 → 14:00 / 12:00; 23 → 0; 0 → 23) · [ps] LT-26 |
| AC-BLE-16-08 | [s] `SPS.RowingToggle`, `DSF.RowingOffZeroesAndFreezes` · [ps] LT-26 |
| AC-BLE-16-09 | [s] `SPS.FaultsWithoutErrors` · [ps] LT-26 |
| AC-BLE-16-10 | [s] `DSF.PanelChangesShowOnTheNextReading` · [ps] `SOT.ChangesVisibleWithinOneSecond` |
| AC-BLE-16-11 | [s] `SPS.NothingPersists` · [ps] `SOT.ReopenShowsCurrentValues` · [eq] Windows |
| AC-BLE-16-12 | [ps] LT-27 · [cap] CAP-16 |

**F-HRM-04, -12 (11)**

| AC | Verificación |
|---|---|
| AC-HRM-04-01 | [s] LT-23, `SHS.EveryReadingIsPublishedEvenIfRepeated` |
| AC-HRM-04-02 | [d] `HeartRateContactTests.ContactRule` (sin contacto → 0; con o sin informar → el valor, aunque sea 0) |
| AC-HRM-04-03 | [d] `HRS.InitialBpmIsZero`, `HCR.ZeroShowsDashes` · [ps] LT-28 |
| AC-HRM-04-04 | [s] LT-25, `SHS.ZeroIsPublishedOnlyOnce` |
| AC-HRM-04-05 | [s] `DSF.BpmDoesNotDependOnTheRower` · [ps] `DSS.PulseKeepsComingWithTheRowerDisconnected` |
| AC-HRM-12-01 | [s] LT-32 · [d] `HRS.InitialState` |
| AC-HRM-12-02 | [s] LT-32 · [d] `HRS.ChosenNameOnlyForThatAttempt` |
| AC-HRM-12-03 | [s] LT-33 · [d] `HRS.ErrorClearedOnStartAndOnConnectedNotOnDisconnected` |
| AC-HRM-12-04 | [s] LT-34 |
| AC-HRM-12-05 | [d] `HRS.StartScanClearsErrorAndList` |
| AC-HRM-12-06 | [d] `HRS.NoNotificationAfterDispose` (acción lenta con `TaskCompletionSource`) |

**F-HRM-13 (12)**

| AC | Verificación |
|---|---|
| AC-HRM-13-01 | [ps] LT-28, LT-30 · [p] `DSP.CardPlacementInBothViews` |
| AC-HRM-13-02 | [d] `HCR.TitleIsUpperAndHeartState` · [p] `HCS.HeaderAndStructure` (borde al 60 % fijo, título en `metric.hr.hud`, corazón 1 / 0,40 por código tras `Settle`) · [cap] CAP-10 a CAP-15 |
| AC-HRM-13-03 | [d] `HCR.BodyTable` (las 6 filas) · [p] `HCS.BodiesWithScriptedHrm` · [cap] CAP-10 a CAP-14 |
| AC-HRM-13-04 | [d] `HCR.EnabledTable` · [p] `HCS.DisabledButtonsDoNotRespond` · [ps] LT-31 · [cap] CAP-15 |
| AC-HRM-13-05 | [d] `HCR.RowTitleFallbacks` · [p] `HCS.DeviceRowContent` · [cap] CAP-11 |
| AC-HRM-13-06 | [p] `HCS.ConnectedBodyLayout` (una línea con "…", 52, `bpm` 14 en `metric.hr.hud` por la línea de base, `Desconectar` a la derecha, `FitScale` con letra grande) · [cap] CAP-12, CAP-04b |
| AC-HRM-13-07 | [d] `HCR.ErrorTexts` · [p] `HCS.ErrorClampedToThreeLines` · [cap] CAP-14 |
| AC-HRM-13-08 | [d] `DSR.ScanSpecExcludesHrmScan`, `HCR.EnabledTable` · [ps] LT-29, LT-31 · [cap] CAP-09, CAP-15 |
| AC-HRM-13-09 | [ps] LT-28 (800 × 600 dp) |
| AC-HRM-13-10 | [ps] LT-30 · [cap] CAP-09 |
| AC-HRM-13-11 | [d] `HCR.NoCancelSignalOrStop` · [p] `HCS.NoExtraControls` |
| AC-HRM-13-12 | [d] `HeartbeatClockTests.TwentyBeatsInTenSecondsAt120`, `HeartbeatClockTests.NewPeriodFromTheNextBeat` · [ps] `DSS.HeartBeatsAtTheRealRate` · [cap] VID-01 |

**F-HRM-15 (12)**

| AC | Verificación |
|---|---|
| AC-HRM-15-01 | [s] `SHS.NoMemoryAndAutoConnectDoesNothing` · [ps] `DSS.CardOpensInNoSensor` |
| AC-HRM-15-02 | [s] LT-23, LT-24, `SHS.SingleResultDetails` (−50 dBm, 0x180D) |
| AC-HRM-15-03 | [s] `SHS.StopScan` |
| AC-HRM-15-04 | [s] LT-23, `SHS.DirectToConnectedFirstReadingAfterOneSecond` |
| AC-HRM-15-05 | [s] LT-23, `SHS.NameOnlyWhileConnected` |
| AC-HRM-15-06 | [s] LT-25 |
| AC-HRM-15-07 | [s] `SHS.ForgetCancelsTheReturnAndKeepsTheConnection` · [ps] `DSS.ForgetDuringReconnecting` |
| AC-HRM-15-08 | [s] LT-25 · [ps] LT-26, `DSS.HrmDropShowsReconnecting` · [cap] CAP-13 |
| AC-HRM-15-09 | [s] `SBK.HeartRateIs142At60s` |
| AC-HRM-15-10 | [s] `DSF.BpmKeepsPublishingWithTheRowerStopped` |
| AC-HRM-15-11 | [s] LT-23, LT-24, LT-25, LT-32 |
| AC-HRM-15-12 | [s] `SHS.SearchOtherCancelsTheReturn` · [ps] `DSS.SearchOtherDuringTheDrop` |

**Criterios de la It. 1 que se verifican acá (12)**

| AC | Verificación |
|---|---|
| AC-BLE-01-05 | [p] `DSP.GridTextsLikeImage01` (23,5 / 149 / 652, y con la cultura del hilo en `es-AR` y `de-DE`) · [cap] CAP-01, CAP-04a, CAP-06 |
| AC-APP-09-04 | [ps] `LOS.NoLanguageSelectorAndNoMemory` · [d] `LanguageStateTests.NoSetterNoPersistence` (It. 1) · [eq] |
| AC-APP-09-05 | [ps] `LOS.LiveLanguageChangeKeepsEverythingElse` · [eq] Android |
| AC-APP-09-06 | [d] `LocaleResolverTests` (It. 1) · [u] `SystemLocaleProviderTests.WindowsListIsBcp47AndNotEmpty` (solo en el Editor de Windows; en otras plataformas se ignora con el motivo; QA-G2-17) · [eq] Windows y Android |
| AC-APP-16-01 a -04 | [d] `AppIdentityTests` (It. 1, configuración) · [u] `BVT.ProductIsRowMate` · [eq] (Android: NO VERIFICABLE sin módulo ni teléfono) |
| AC-BASE-01 | [d] y Unity EditMode (It. 1) · [d] `ParitySmokeTests.NoDifferences` · [eq] humo IL2CPP (NO VERIFICABLE sin el módulo) |
| AC-BASE-05 | [cap] CAP-23 + `rt1-measurement.json` + `SET.Rt1MeasurementPresent` |
| AC-BASE-08 | `run-unity-tests.ps1` y `-Simulator`: mismo conjunto que dotnet, 0 fallan (diferencias de conteo explicadas como en 00-primer-open §5) |
| AC-BASE-10 | [u] tests de la It. 1 + `BVT.Version3AppliedOnce`, `BVT.BootSceneIsTheOnlyBuildScene` |

### 5.3 Los 15 casos legacy

Cada uno con `// legacy: test/<archivo>:<línea> '<nombre>'` y `[TestCase(TestName = "<nombre exacto>", Category = "LT-nn,AC-…")]` (convención de la It. 1). Las clases se llaman como el archivo legacy.

| LT | Legacy | Clase · método | Suite | Cómo se porta |
|---|---|---|---|---|
| LT-20 | `simulated_ble_service_test.dart:13` | `SimulatedBleServiceTests.ConnectedAndAdapterOnForTheFirstListener` | [s] | `StateRecorder` de `Status` **sin** el valor actual (`emitCurrent: false`) + `Start()` → `[Connected]`; `StateRecorder` de `AdapterState` **con** el valor actual (`emitCurrent: true`) → `[On]` (es un observable constante: grabarlo con el actual es la única forma de obtener `[On]`, como el `adapterStateStream` del legacy, que emite al suscribirse; queda escrito en el test, observación de 03-qa-harmony §2); nombre; `Advance(3 s)` → 3 lecturas con pulso 0 y distancia creciente; `Dispose` |
| LT-21 | `:39` | `…SimulateDisconnectReconnectsAfterThreeSeconds` | [s] | `Advance(2 s)` → `[Connected, Disconnected]` y 0 lecturas; `Advance(1 s)` → `[…, Connected]` |
| LT-22 | `:64` | `…DisconnectDoesNotReconnectStartScanDoes` | [s] | `DisconnectAsync`, `Advance(10 s)` → desconectado y sin nombre; `StartScanAsync` → conectado, último estado `Connected` |
| LT-23 | `simulated_heart_rate_service_test.dart:10` | `SimulatedHeartRateServiceTests.ScanEmitsTheSyntheticSensorAndConnectEmitsBpm` | [s] | `[Scanning]`, un resultado `Pulsómetro simulado` / `SIM:HRM`; conectar; `sim.Tick()` × 2 + `Advance(3 s)` → 3 pulsos, el último = `sim.HeartRate` > 0 = `CurrentBpm`; `PendingTimers == 0` tras `Dispose` |
| LT-24 | `:49` | `…ScanExpiresToDisconnected` | [s] | 9 s buscando, 10 s desconectado, sin timers |
| LT-25 | `:62` | `…SimulateDisconnectDropsAndReturnsDisconnectDoesNot` | [s] | 1 pulso en 1 s; caída → reintentando, recordado, último pulso 0; 2 s `[Connected, Disconnected]`; 3 s conectado sin reintentar ni recordado; desconectar + 10 s → desconectado sin nombre; sin timers |
| LT-26 | `simulator_overlay_test.dart:17` | `SimulatorOverlayTests.PanelOpensAboveNavigationAndPlusRaisesWatts` | [ps] | capa montada como en la app (último hijo de la raíz); no se ve `150 W`; `Tap("sim-open")` → `150 W` y `24 spm`; `sim-watts-plus` → objetivo 160 y `160 W`; `sim-spm-plus` → 25; `sim-hour-plus` → aparece `Hora real`; `Simular desconexión` sin excepción (logger sin `Error`); pulsómetro conectado + `Simular caída del pulsómetro` → desconectado y reintentando; `Fuerte` → 280 / 30; `Remando` → apagado; `sim-close` → no se ve `280 W` |
| LT-27 | `:90` | `…CloseButtonStaysReachableInAShortWindow` | [ps] | `DeviceProfile.P800`: abre (`150 W` visible), `sim-close` dentro de la ventana, lo cierra (`150 W` no visible) |
| LT-28 | `heart_rate_card_test.dart:83` | `HeartRateCardTests.NoSensorSearchConnectBpmDisconnect` | [ps] | 800 × 600 dp, es; `PULSÓMETRO`, `Sin sensor`, pista con `Heart for Bluetooth`; `ScrollTo("hrm-search")` (hace falta desplazar) y tocar → `Buscando sensores...` + `Pulsómetro simulado`; tocar la fila → conectado, nombre y `--`; `sim.Tick()` + `Advance(1 s)` → se ve `hrm.CurrentBpm` y no `--`; `hrm-disconnect` → `Sin sensor` |
| LT-29 | `:121` | `…RowerAndHrmScansExcludeEachOther` | [ps] | `DisconnectAsync` del remo → `device-search` habilitado; `hrm-search` → deshabilitado; `StopScanAsync` del pulsómetro → habilitado |
| LT-30 | `:142` | `…LandscapeScanViewDoesNotOverflow` | [ps] | L640: `Audit()` vacío; `ScrollTo("hrm-search")`, tocar → `Scanning`; la fila existe dentro de `#hrm-card-slot-scan` (aunque fuera de la vista) y `Audit()` sigue vacío |
| LT-31 | `:161` | `…HrmSearchDisabledWhileRowerScans` | [ps] | `HostBackends.TestRowerWithSimulatedHeartRate()` (QA-G2-05) = `ControllableRowerService` + `SimulatedHeartRateService` sobre un `RowingSimulator` sin ruido (como `_pump` con `bleFactory`, `heart_rate_card_test.dart:51-56`), sin capa del simulador (el legacy monta solo `DeviceScreen`): habilitado; `Scanning` → no; `Connecting` → no; `Disconnected` → sí (cada paso con `Settle()`, porque el cambio de estado del remo cambia de vista) |
| LT-32 | `heart_rate_provider_test.dart:54` | `HeartRateProviderTests.ReflectsScanConnectionBpmAndDisconnection` | [s] | igual que el legacy, con `StateRecorder`: el **primer** estado emitido después de `ConnectAsync(id, name)` tiene `ConnectingName == "Pulsómetro simulado"` y el último `""` (D-04); `sim.Tick()` + 1 s → bpm = `sim.HeartRate` > 0; caída → no conectado, reintentando, recordado; 3 s → conectado sin reintentar; desconectar → bpm 0; más de 3 avisos; `PendingTimers == 0` |
| LT-33 | `:107` | `…ServiceExceptionIsKeptAndClearedOnRetry` | [s] | `ThrowingHeartRateService`: conectar → `HrmIncompatibleException`, `IsIncompatible` verdadero; `RetryAsync` → sin error; `DisconnectAsync` no lanza |
| LT-34 | `:122` | `…IsIncompatibleTellsTheIncompatibleSensorApart` | [s] | `HrmIncompatibleException` → sí; `InvalidOperationException("x")` → no |

Los 9 [s] corren en dotnet y en Unity EditMode con `-Simulator` (AC-BASE-08); los 6 [ps], en Unity PlayMode con `-Simulator`. LT-29, LT-30 y LT-31 se vuelven a correr en la 10a contra el remo real (QA-P-27).

### 5.4 Conteos esperados

| | Antes (00-primer-open) | Nuevos (aprox.) | Después |
|---|---|---|---|
| dotnet (EditMode + Simulador) | 961 | ≈ 230 (≈ 150 generales, ≈ 80 del simulador, con los `TestCase`) | ≈ 1 190 |
| Unity EditMode (sin / con simulador) | 888 / 957 | los mismos + ≈ 20 de `UnityEditMode` | ≈ 1 110 / ≈ 1 190 |
| Unity PlayMode (sin / con simulador) | 0 / 0 | ≈ 55 / ≈ 30 | ≈ 55 / ≈ 85 |

`run-tests.ps1 -Matrix` (con `-UnityResults`) tiene que dar **SIN TEST 0** para los 125 AC y los 15 LT; los que son solo de equipo figuran como NO VERIFICABLE con el motivo (§7.3).

---

## 6. Paquetes de trabajo

### 6.0 Orden y reglas

1. **WP-D0 → WP-A → WP-B → WP-D**, en ese orden (DN-01, QA-G2-03). WP-A arranca cuando WP-D0 está en verde; WP-B, cuando WP-A está en verde en dotnet; WP-D, cuando WP-B compila en Unity y el arnés produce las 28 salidas (25 capturas, varias en más de una toma, como CAP-04a y CAP-04b, y 3 videos).
2. **Archivos disjuntos.** Cada archivo tiene un solo dueño (§1.1). Excepciones explícitas: (a) los **generados** (`Tokens.uss`, `Typography.uss`, capturas, `.meta`, `ProjectSettings/*.asset`) los reescribe la herramienta de quien la corre; (b) `Skin.uss`, `Motion.uss` y `SimulatorSkin.uss` los **crea vacíos** WP-B (solo el encabezado "Dueño: WP-D") para que el tema importe; su contenido es exclusivo de WP-D; (c) `Tokens.cs` es del Designer desde la It. 1: en esta iteración lo tocan **WP-D0** (los 5 tokens nuevos) y WP-D (si hace falta otro, o el respaldo de 04-design-notes §8.3), y cada vez se regeneran los USS.
3. Nadie declara un paquete terminado sin la salida real de los comandos de su "Hecho cuando".
4. Ningún paquete toca `rowmate-legacy` ni lee `strava_secrets.dart`; `git -C rowmate-legacy status --porcelain` vacío al empezar y al terminar.

### 6.1 WP-D0 — tokens nuevos (Designer, antes de WP-A)

Pedido DN-01 de 04-design-notes, aceptado (QA-G2-03): sin esto, WP-A no puede generar `.rm-type-body-m`, `-body-xs` ni `-button`, y los PlayMode de contrato de WP-B fallarían hasta WP-D.

**Archivos** (alcance que fija 04-design-notes DN-01): `Core/Design/Tokens.cs` (solo datos: `bodyM` Inter 14 / 20 / 400, `bodyXs` Inter 12 / 16 / 400, `button` Inter 14 / 20 / 600, `accent.tonalBg` `#00B4D8 @12 %` decorativo, `accent.onTonal` `#38CBEF` `UiText`; 04-design-notes §2.1) y `Presentation/Styles/Tokens.uss` (G, con `tools/tokens`); además, los pares N1-N13 de 04-design-notes §8.1 en `ContrastTests` (`DesignerPairs`, archivo del Designer desde la It. 1), que es lo que el "hecho cuando" de DN-01 pide que pase. Ningún token existente cambia.

**Hecho cuando:** `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it2-d0` en verde (incluye `TokensUssSyncTests`, `ColorSourceTests`, `ContrastTests` con N1-N13 y las pruebas del catálogo de tokens) y `dotnet …/RowMate.Tokens.dll --check` con 0 (`Tokens.uss`; `Typography.uss` todavía no existe: lo crea WP-A).

### 6.2 WP-A — lógica pura y tests de dotnet (sin Unity)

**Archivos:** todos los de §1.1 marcados WP-A: `Core/Common/{IEventSource,DerivedObservable,RealtimeTimerQueue}.cs` y `EventStream.cs` (C); `Core/Ble/*` nuevos y `Unavailable/*`; `Core/Models/{MetricKind,MetricPalette}.cs`; `Core/Design/{Icons,FontAssetPaths,TypographyUssWriter,TextScaleSteps,Easing,CriticalSpring,MotionSpecs,AccentPulse,DigitRoll,HeartbeatClock,FramePacingPolicy}.cs`; `Core/Ports/{IAccessibilitySettings,IDisplayMetrics,IBackButtonPolicy}.cs`; `Core/Text/ParitySmoke.cs`; `App/Device/*`, `App/Shell/*`, `App/Scene/SceneSettings.cs`, `App/Composition/*`; `Simulator/*` y `Simulator/Panel/*`; `Tests/Support/*` nuevos; `Tests/Simulator/*` nuevos; `Tests/EditMode/{Common,Device,Shell,Composition,Ble,Design,Project,Text,Traceability}/*` de §1.1 (salvo `AsmdefLintTests` e `ItTwoToolingTests`, de WP-B); `tools/tokens/Program.cs` (C); `tools/dotnet/run-tests.ps1` (C, QA-G2-16); `Presentation/Styles/Typography.uss` (G).

**Hecho cuando:**
- `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it2-a` → compila 9 proyectos con 0 advertencias y 0 errores (incluye `UnityApiCheck` contra NUnit 3.5.0) y pasan todos los tests, 0 fallan.
- `-Matrix` (con el cambio del script que hace este mismo paquete, QA-G2-16): todos los AC [d] y [s] de §5.2 con al menos un test que pasa; los `It02Ids.UnityOnlyIds` figuran como `PENDIENTE UNITY` (no `SIN TEST`) mientras no se pase `-UnityResults`; `SIN TEST 0`. `TraceabilityTests.It02IdsAreComplete` verifica 125 AC y 15 LT y que cada `UnityOnlyIds` tenga al menos una ubicación esperada en `Tests/PlayMode*`, `Tests/UnityEditMode` o el catálogo de capturas (por texto; si la carpeta todavía no existe, `Ignore` con el motivo "pendiente WP-B").
- `dotnet …/RowMate.Tokens.dll --check` en verde (los dos USS; `.rm-type-label` con `letter-spacing: 8px`, QA-G2-02).
- `-Ci -Filter "FullyQualifiedName~RepoHygiene|FullyQualifiedName~PureCodeRules|FullyQualifiedName~ColorSource"` en verde.
- Los 9 LT [s] con su comentario `// legacy:` y `TestName` exacto.

### 6.3 WP-B — UI, composición, escena, arnés, herramientas y tests de Unity

**Archivos:** todos los de §1.1 marcados WP-B (Platform, Presentation salvo las hojas de WP-D, Simulator.Presentation salvo `SimulatorSkin.uss`, Bootstrap, Editor, `Tests/UiSupport`, `Snapshots`, `Tests/PlayMode*`, `Tests/UnityEditMode`, `Tests/EditMode/Project/{AsmdefLintTests,ItTwoToolingTests}.cs`, `tools/unity/{capture-snapshots,capture-window}.ps1`, `tools/build/build.ps1`, `.github/workflows/ci-unity.yml`, `.gitignore`, `.gitattributes` (+ `mp4`), los generados de Unity y del *bootstrapper*, y los cambios de documentos de §1.1). `tools/dotnet/run-tests.ps1` ya no es de WP-B (pasó a WP-A, QA-G2-16): WP-B solo lo usa con `-UnityResults`.

**Orden interno sugerido:**

| Paso | Qué | Comprobación |
|---|---|---|
| B.1 | Editor: *bootstrapper* v3 (fuentes, panel, escena Boot, ventana, URP ordenado), `BuildCli`, menús | `Unity -batchmode -quit …` código 0, sin `error CS`; `[BOOTSTRAP] versión 3 aplicada`, 15 pasos `ok`/`skipped`; `RowMateBootstrap.json` en 3 |
| B.2 | **S-B1**: `UiSurface` + los dos presentadores; build de desarrollo de Windows (Mono) con la muestra de CAP-23 y `-rowmate-color-probe`; leer el *backbuffer*, un clic, el primer cuadro y la latencia (§3.1, cinco criterios) | anotar en el diseño de cierre qué presentador y qué gancho de copia quedaron, o el pase a `Overlay` |
| B.3 | Platform (scheduler sobre `RealtimeTimerQueue`, logger, idioma, accesibilidad, densidad y área segura, "Atrás") | compila; `SMT.BackButtonPolicyByApiLevel`, `SMT.DesktopBackPolicyDoesNothing`, `SystemLocaleProviderTests` |
| B.4 | Controles (`TabularNumber`, `FitBox`, `IconView`, `RmButton`, `IconButton`, `SwitchToggle`, `Spinner`, `TextClamp`, `VerticalFadeTexture`), `MotionDriver`, `ViewBinding`, `UiTestHost` mínimo (crear, `Advance`, `Settle`, `Tap` con *hit-test*) | `THM.*` |
| B.5 | Shell (con `ShellLayout` y el área segura), Dispositivo, tarjeta, placeholders, `ShellHost`, `AppRoot` (configura en `Awake`, arma en `Start`) | `SHT`, `DSP`, `HCS`, `NBT`, `BootSceneTests`, `SafeAreaTests` |
| B.6 | Capa del simulador | `SOT`, `HCT`, `DSS`, `LOS` |
| B.7 | `UiTestHost` completo + arnés + `capture-snapshots.ps1` | las 25 capturas con todas sus tomas (CAP-04a y CAP-04b incluidas), 3 videos, `manifest.json`, `INDEX.md`, `rt1-measurement.json` |
| B.8 | `build.ps1` (con `BuildOutputInspector`), `capture-window.ps1`, `ci-unity.yml`, documentos | builds de Windows normal y de simulador (Mono) que abren; el normal sin rastros del simulador |

**Tres entregas con revisión de QA a mitad de camino** (sugerencia de 03-qa-harmony §8, aceptada):

| Entrega | Pasos | Hecho cuando |
|---|---|---|
| B-I · plataforma | B.1-B.3 | proyecto abre con y sin el define, 0 `error CS`; *bootstrapper* v3 aplicado; S-B1 con su resultado escrito; `run-unity-tests.ps1` EditMode (con y sin `-Simulator`) 0 fallan, incluidos `BVT`, `FIT`, `SMT` |
| B-II · UI | B.4-B.6 | `run-unity-tests.ps1 -Platform PlayMode` y `-Platform PlayMode -Simulator` 0 fallan, incluidos `BootSceneTests`, `SafeAreaTests` y los 6 LT [ps] |
| B-III · arnés y herramientas | B.7-B.8 | el "Hecho cuando" completo de abajo |

**Hecho cuando:**
- El proyecto abre en 6000.3.27f1 en batchmode con código 0 y sin `error CS` / `Scripts have compiler errors` / `Compilation failed`, con y sin `ROWMATE_SIMULATOR` (AC-BASE-10).
- `run-unity-tests.ps1` y `-Simulator` (EditMode): 0 fallan, 0 inconclusos; `-Platform PlayMode` y `-Platform PlayMode -Simulator`: 0 fallan. Al final `ProjectSettings.asset` sin el define y sin la marca de `Library/RowMate/`.
- `run-tests.ps1` (dotnet) sigue en verde (incluye el *lint* con los asmdefs nuevos).
- `capture-snapshots.ps1` sale con 0 y deja en `snapshots/` todo lo de §4.3, con todas las sondas y auditorías marcadas en verde (salvo las que dependen de WP-D, marcadas como "aspecto").
- `tools/build/build.ps1 -Target Windows -Backend Mono` y `-Simulator` generan `Builds/Windows/<variante>/RowMate.exe`, que abren (la de simulador en Dispositivo conectada; la normal en la vista de búsqueda). IL2CPP y Android: NO VERIFICABLE si falta el módulo, con el mensaje de `BuildCli`.
- Los 6 LT [ps] con su comentario `// legacy:` y `TestName` exacto.

### 6.4 WP-D — pulido visual del Designer

**Archivos:** `Presentation/Styles/Skin.uss`, `Presentation/Styles/Motion.uss`, `SimulatorPresentation/Styles/SimulatorSkin.uss` (contenido), `Core/Design/Tokens.cs` (solo si hace falta otro token, o el respaldo de 04-design-notes §8.3 si S-B1 cae a `Overlay`; regenera `Tokens.uss` y `Typography.uss`), los pares de contraste que sume WP-D (N1-N13 ya entran en WP-D0), las sondas de aspecto de 04-design-notes §7.2 en el catálogo del arnés (pedido al Architect si tocan `SnapshotCatalog`), `docs/…/02-dispositivo-simulador/04-design-notes.md`, y las capturas regeneradas.

**Trabajo:** el aspecto de §3.12 ("Lo que es de WP-D"); revisar cada captura y video contra la columna "Qué tiene que verse" del PO y las biblias; contraste de los pares nuevos con el método de 04-design-notes (si un par no llega, se ajusta el token y lo verifica `ContrastTests`); la decisión de aspecto de los íconos provisorios si alguna vez hicieran falta (D-07).

**Hecho cuando:** `run-tests.ps1` en verde (`ColorSourceTests`, `TokensUssSyncTests`, `TypographyUssSyncTests`, `ContrastTests`); los PlayMode de WP-B siguen en verde (ningún contrato roto); `capture-snapshots.ps1` regenera todo con 0 desbordes; `04-design-notes.md` lista cada cambio con su captura.

---

## 7. Riesgos y desvíos

### 7.1 Desvíos propuestos (para el gate)

| ID | Qué dice el plan o la spec | Qué propone este diseño | Por qué | Decide |
|---|---|---|---|---|
| D-01 | AC-BASE-05: medir R-T1 en la It. 2; si se confirma, aplicar la opción A antes de la 4a y repetir las capturas | Aplicar la opción A **ya** (`UiColorPipeline.GammaRt`: RT UNorm + `forceGammaRendering` + presentador), con respaldo a `Overlay` si S-B1 falla | R-T1 ya está confirmado (S-0) y la opción A funciona en RT: hacerlo ahora da paridad visual en la It. 2 (los blancos translúcidos del legacy) y evita rehacer las capturas. El arnés compone igual que la pantalla | **Confirmada** por el Designer (04-design-notes §1) y el PO (spec §11, QA-G2-18); falta S-B1 |
| D-02 | ARCHITECTURE §2: `Presentation/Uxml/` | Vistas construidas en C#, sin UXML | Sin referencias a assets que mantener; el arnés y los tests arman exactamente la misma UI; el Designer trabaja en USS con nombres de contrato | Architect (ADR 006, nota) |
| D-03 | ARCHITECTURE §7.3: `ConstantPhysicalSize` a 160 dpi en móvil | `ConstantPixelSize` con la densidad del sistema en todas las plataformas | `Screen.dpi` en Android son dpi físicos, no la densidad que usa el legacy; así 1 dp = 1 px × densidad exacto, y es lo mismo que usa el arnés | Architect |
| D-04 | Entrega de los *streams* del legacy en *microtask* | Entrega síncrona (`ObservableValue`, `EventStream`) | ADR 004; nada visible cambia. LT-32 se porta grabando la secuencia de estados | Architect (QA lo revisa en LT-32) |
| D-05 | El remo simulado se conecta "al primer oyente" | `IRowerService.Start()` explícito, idempotente, que llama `AppContext.Start()` después de armar la UI | Los observables propios no tienen *onListen*; el efecto es el mismo (una vez, al empezar a escuchar) | Architect |
| D-06 | Spec §7.1: capturas en `captures/` con `CAP-nn-<tema>` | `snapshots/CAP-nn-<tema>.png` (+ `manifest.json`, `INDEX.md`) | Pedido del coordinador; mismos nombres de archivo | **Confirmada** (spec §7.1) |
| D-07 | ASSET_SLOTS §4.4: recetas provisorias para los 22 íconos | En la It. 2 solo el ícono genérico (cuadrado redondeado) si falta un glifo | Las dos fuentes de íconos ya están versionadas en `UI/Icons/` (no puede faltar la fuente en un clon); las recetas quedan para cuando una iteración las necesite | Designer: **de acuerdo** |
| D-08 | ARCHITECTURE §4.1 paso 6: `IWakeLock.Enable()` al arrancar; legacy `main.dart:42` | Sin wakelock en la It. 2 | La spec deja F-APP-14 en la 4a (§13). Diferencia con el legacy durante la It. 2: la pantalla se apaga sola | PO (ya decidido) |
| D-09 | Spec AC-APP-02-06: "Atrás" cierra la app; ADR 007: la manda al fondo | API ≥ 31: `moveTaskToBack(true)`; < 31: `Application.Quit()` | **Motivo corregido en el gate** (spec §15, DV-06): el legacy **sí** termina la app con "Atrás" en Android 8 a 15 (`FlutterActivity` sin nada que cerrar llama a `SystemNavigator.pop`); solo en Android 16 (atrás predictivo con `targetSdk` 36) queda en segundo plano. La app hace en Android 12+ lo que el sistema hace con la pantalla raíz de cualquier app y lo que el propio legacy ya hace en 16; en la It. 2 no hay datos en juego | PO: **aceptada** como cambio (DV-06) |
| D-10 | ART_BIBLE §3.1: el valor "se achica hasta 40" | Sin tope inferior (`FitBox` como `FittedBox.scaleDown`) | AC-BLE-11-06 pide que `123456m` entre a 411 dp sin cortarse: necesita ≈ 39 dp | Designer: **de acuerdo** (alinea ART_BIBLE en WP-D) |
| D-11 | 00-deuda-diseno §3.5: ícono del chip 14 y del estado vacío 56 | 12 y 48, como pide la spec (AC-BLE-11-02, AC-BLE-05-02) | Manda la spec (y es el legacy) | Designer: **de acuerdo** |
| D-12 | Legacy: llave a 96 dp de abajo sobre una barra de 80 | Barra de 64 (ART_BIBLE §3.11); llave a 64 + 16 = 80; alto máximo del panel con la fórmula de la spec (− 96) | AC-BLE-16-02 pide 16 dp de aire sobre la barra; AC-BLE-16-03 fija la fórmula con sus ejemplos | — (cumple los dos AC) |
| D-13 | Tokens con alto de línea | No se aplica: USS no tiene `line-height` | Limitación de UI Toolkit 6.3 | Designer: **enterado** |
| D-14 | ANIMATION_BIBLE: transiciones de USS para la UI | Motion por código con reloj inyectable; USS solo para micro-interacciones | Tests y videos deterministas; mismo resultado a 60 fps en la app | Designer: **de acuerdo**, con DN-11 (el color de la pestaña también por código) |
| D-15 | `device_provider.dart`: banner de error de búsqueda y conexión | Se guarda el error en el estado, pero no se dibuja | La spec deja el banner para la 10a (§4.15); con el simulador no hay errores | — |
| D-16 | `ProjectSettings`: IL2CPP en Standalone | `BuildCli -Backend Mono` cambia el *backend* solo para ese build y lo restaura en `finally` | No está el módulo IL2CPP de Windows; los builds de Device-Win se hacen con Mono (spec §0.2) | — |
| D-17 | — | `ParitySmoke` en Core (tabla de AC-BASE-01) que un player corre con `-rowmate-parity-smoke` | Es la "prueba de humo IL2CPP" de ARCHITECTURE §16 sin un assembly de tests en el player | QA |
| D-18 | 00-primer-open §8.3: assets de URP en la raíz de `Assets/` | El *bootstrapper* v3 los mueve a `Settings/URP/` con `MoveAsset` (mismo GUID) | Todo lo propio bajo `Assets/RowMate` | — |
| D-19 | P-01 | *Splash* de Unity apagado (Unity 6 lo permite en Personal); "Development Build" queda en los builds de simulador | La marca de desarrollo no se puede ocultar desde la API | — (P-01 aceptada) |
| D-20 | F-STR-01 (It. 12) decide la pestaña Perfil | `ShellOptions(ProfileTabEnabled: false)` fijo en la It. 2; los tests y CAP-25 lo prenden | No hay Strava hasta la 12 | — |
| D-21 | AC-HRM-13-07: error en hasta 3 líneas con "…" | `TextClamp` propio | UI Toolkit no limita líneas | — |
| D-22 | `device_provider.dart:69-86`: vigilante de 5 s en el *provider* | No se porta en la It. 2; en la 10a va a Core (`RowerWatchdog`, ARCHITECTURE §10.1) | Con el simulador nunca actúa (AC-BLE-15-08) | — |
| D-23 | AC-APP-05-05 en Windows | Escala de letra de Windows desde `TextScaleFactor`; si el legacy no la respeta en Windows, se apaga en Windows | No se sabe si Flutter Windows la aplica; se compara en equipo | QA |
| D-24 | Spec §7.1: capturas "renderizadas por Unity (Editor, PlayMode)" | El arnés renderiza en modo edición; las pruebas de UI corren en PlayMode | Es la misma UI y el mismo `PanelSettings`; el modo edición evita la recarga de dominio y hace al arnés rápido para que el Designer itere (S-0) | PO: **aceptada**, con la condición de R2-09 (spec §7.1) |
| Aclaración | AC-HRM-13-02 ("pleno conectado") y AC-HRM-13-12 ("con `--` … quieto al 40 %") | Con `--` y conectado: pleno y quieto; sin conexión: quieto al 40 % | Es lo que muestran el legacy (`heart_rate_card.dart:39-40`) y CAP-12a | PO: **confirmada** en el gate |
| D-25 | `heart_rate_card.dart:57`: `hrmConnectError('${hr.error}')`, el `toString()` de Dart (`Exception: …`, con el prefijo del tipo) | `Format(hrmConnectError, error: e.Message)` | Imitar el `toString()` de Dart con excepciones de .NET o de un *plugin* nativo no daría el mismo texto igual (los tipos son otros). En la It. 2 no se ve: el simulador no tiene errores y CAP-14d usa un error de prueba. El texto técnico de los errores reales se fija en la It. 11, con los errores del pulsómetro real (QA-G2-19) | PO (enterado) |
| D-26 | AC-BLE-16-03: alto máximo del panel = alto de la ventana − 96 − margen seguro superior − 16 | (alto de la ventana − margen seguro **inferior**) − 96 − margen seguro superior − 16, mínimo 120 (§2.7) | La barra de pestañas crece con el margen inferior y el panel se ancla 16 dp encima de ella (AC-BLE-16-02, DV-07); con la fórmula literal y una barra de gestos de 48 dp, la X de cerrar quedaría debajo de la barra de estado, y la misma AC pide que se vea siempre. Sin margen inferior (Windows, Editor y todos los ejemplos de la AC) da lo mismo (QA-G2-06) | PO (confirmar) |
| D-27 | `simulator_overlay.dart:201`: valor de los renglones ± de 72 dp | 64 dp (DN-19, 04-design-notes §4.9) | Con Inter 14 (`bodyM`) y 72 dp, `Hora escena` (84,6 dp) queda con 0,7 dp de aire en el renglón de 280; con 64, con 94 dp. Es el panel de desarrollo (nunca en un build distribuible); no cambia datos ni comportamiento | PO (enterado; puede sumarlo a §11.1) |

### 7.2 Riesgos

| ID | Riesgo | Impacto | Mitigación |
|---|---|---|---|
| R2-01 | El presentador de `GammaRt` no da `#2A2F37` en pantalla o rompe la entrada (mapeo de pantalla a panel con `targetTexture`) | Sin la opción A en la It. 2 | S-B1 al principio de WP-B, con criterio de pase; respaldo a `Overlay` (= lo previsto por el PO). `SetScreenToPanelSpaceFunction` existe en 6.3 (S-0) |
| R2-02 | Costo de la copia o el *blit* de pantalla completa en Android de gama baja (≈ 10 MB por cuadro a 1080 × 2400) | Batería, cuadros | Solo cuando se dibuja un cuadro (30 fps en reposo, §3.9); medición en equipo en la 4a/14; `CopyTexture` UNorm → sRGB puede no estar en GLES 3.0: en ese caso, `BlitPresenter` |
| R2-03 | Los `FontAsset` dinámicos se ensucian al usarlos en el Editor (el atlas crece) y generan *diffs* | Ruido en git | `clearDynamicDataOnBuild = true`; `FontAssetBuilder` no reescribe si existen; QA revisa que los `.asset` no cambien en un commit sin motivo |
| R2-04 | Orden de importación USS ↔ `FontAsset` (S-0: un USS importado antes que su fuente queda con `MissingAssetReference`) | Texto sin fuente en un clon nuevo | Los `FontAsset` se versionan; el paso `ui.fonts` reimporta los USS; `FIT` verifica que `resolvedStyle` tenga la fuente |
| R2-05 | Modificar el `PanelSettings` del proyecto en Play lo deja modificado en disco | Diffs y estados raros | Siempre `Instantiate` (AppRoot, `UiTestHost`) |
| R2-06 | Tab, Enter y Espacio en UI Toolkit de *runtime* con el Input System | AC-APP-05-07 | `THM` con eventos de navegación; `RmButton` agrega Espacio; Device-Win |
| R2-07 | `DrawText` por celda: costo o asignaciones | Tablero con tirones | Cadenas de un carácter precalculadas; perfilar 10 s a 1 Hz (0 B/cuadro) en PlayMode; respaldo de *quads* del atlas (ARCHITECTURE §7.8) |
| R2-08 | `Screen.dpi` en Windows o la densidad de Android no son lo que usa el legacy | Tamaño físico distinto (AC-APP-05-09) | `GetDpiForWindow` en Windows; `DisplayMetrics.density` por JNI en Android; Device-Win a 100 % y 150 % |
| R2-09 | Diferencias entre modo edición (arnés) y Play (tests y app): S-0 mostró que en Play la raíz del `UIDocument` no se estira sola; el gate mostró que en `Awake` la raíz no existe (QA-G2-01) | Capturas distintas de la app; app que no arranca | `ShellHost` fija `position: absolute` y los cuatro lados en 0 en la raíz en los dos modos; `AppRoot` arma en `Start` y `UiTestHost` crea el documento con el patrón "inactivo → panel → activo" (sonda §8.3); `ShellTests.EditModeCaptureMatchesPlayModeTree` compara nombres, clases y `worldBound` de CAP-01 en PlayMode con los `elements` del `manifest.json` (± 0,5 dp; se ignora sin capturas), que es la condición del PO para D-24; `BootSceneTests` arranca la escena real |
| R2-10 | Faltan los módulos de IL2CPP o Android | AC-BASE-01 en build, AC-APP-16-04, Device-And | NO VERIFICABLE con el motivo; `BuildCli` falla con un mensaje claro y Mono cubre Device-Win |
| R2-11 | PlayMode en batchmode tarda (recarga de dominio por test) | Ciclo lento | Pocas clases con `[UnitySetUp]` compartido por clase; el arnés (modo edición) para iterar el aspecto |
| R2-12 | `MediaEncoder` no disponible en otra máquina (codificador del sistema) | Sin videos | Respaldo a secuencia PNG; en esta PC funciona (S-0) |
| R2-13 | El *scroller* de `ScrollView` ocupa ancho y cambia las columnas cerca de 640 dp | AC-BLE-11-05 | Barra superpuesta en `Base.uss` (contrato) y test a 641 dp |
| R2-14 | Dos instancias de Unity sobre el proyecto (tests, arnés, usuario con el Editor abierto) | Corridas que fallan | Los scripts no arrancan si `Temp/UnityLockfile` está tomado (código 10) y corren todo en serie |
| R2-15 | Android: si Unity no dibuja detrás de la barra de estado (Android < 15 sin *edge-to-edge*), la barra queda con el color del sistema y no con `bg.app` como en el legacy; si dibuja detrás, los márgenes vienen en `Screen.safeArea` y la regla de §3.1 los consume | Diferencia visual solo en Android | `startInFullscreen = false` en el *bootstrapper*; Device-And mide la barra de estado y los márgenes; si hace falta pintarla, es de F-APP-15 (It. 14) con un *plugin* mínimo. NO VERIFICABLE sin el módulo |
| R2-16 | El gancho de la copia de `OverlayCopyPresenter` corre antes del repintado del panel con `targetTexture` (un cuadro de atraso) | La UI va un cuadro detrás de los datos | Criterio (5) de S-B1 (latencia 0) con los dos ganchos candidatos; si ninguno da 0, `BlitPresenter` en `endContextRendering`; si tampoco, `Overlay` (§3.1) |
| R2-17 | Las transiciones de vista y de cuerpo (DN-12, §5.6 de las notas) hacen que la vista nueva aparezca hasta 360 ms después del cambio de estado | Tests de UI frágiles si miran antes | `host.Settle()` después de cada cambio que dispara una transición (§5.1); el estado (el *store*) cambia en el acto y lo prueban los [d]/[s] |

### 7.3 Lo que queda sin test automatizado (solo equipo, QA o CI)

| AC | Por qué | Quién |
|---|---|---|
| AC-APP-01-07 | Destellos del arranque: solo se ven en el equipo. Automatizado en parte (QA-G2-17): configuración ([u] `BVT`), RT limpias ([p] `THM.SurfaceRtsStartCleared`) y, en el build de Windows, el primer cuadro presentado y la latencia en S-B1 | QA [Device-Win], [Device-And] |
| AC-APP-02-06 | "Atrás" de Android. Automatizado en parte: regla por API y escritorio ([u] `SMT`), ruteo desde el shell con una política falsa ([p] `SHT.BackAtRootGoesToThePolicyOnce`) | QA [Device-And], NV sin módulo o teléfono |
| AC-APP-05-01 | Tema del sistema claro/oscuro (el test verifica que no haya variante) | QA [Device-Win], [Device-And] |
| AC-APP-05-09 | Tamaño físico con la escala de Windows y la densidad de Android | QA [Device-Win], [Device-And] |
| AC-APP-09-06 | Lista de idiomas del sistema real. Automatizado en parte: [u] `SystemLocaleProviderTests.WindowsListIsBcp47AndNotEmpty` en el Editor de Windows | QA [Device-Win], [Device-And] |
| AC-APP-16-01 a -04 | Nombre, versión, carpeta de datos, convivencia, Android mínimo | QA [Device-Win], [Device-And] |
| AC-APP-19-05 | CI de Unity | NV sin licencia ni remoto |
| AC-BLE-13-02 | "El build no trae el código del simulador": se mira en el build. Automatizado en parte: `BuildOutputInspector` al final de cada build normal (`build.ps1` falla si encuentra rastros) y su prueba [u] | QA |

### 7.4 Pendientes para otros roles

| Para | Qué |
|---|---|
| PO | Resueltos en el gate: D-01, D-06, D-09 (DV-06), D-24 y la aclaración del corazón; la frase del ROADMAP. Quedan: **D-26** (alto máximo del panel con margen inferior; confirmar) y, como enterado, **D-25** (texto del error) y **D-27** (renglones ± de 64 dp; si quiere, sumarlo a §11.1) |
| Designer | **WP-D0** antes de WP-A (§6.1, con N1-N13); enterarse de la precisión de DN-12 (en reposo la vista inactiva sigue con `display: none`) y de que WP-B no conserva `.rm-hrm-card__heart--dim` (DN-13); WP-D completo, con las sondas de 04-design-notes §7.2. Ya alineados con el gate: `bpm` en `metric.hr.hud` (QA-G2-08), `deviceNoResults` (QA-G2-11), la cinta sin insets propios. En 04-design-notes la referencia "02-tech-design §6.3" (WP-D) pasa a §6.4 |
| QA | S-B1 como evidencia de D-01 (cinco criterios, §3.1); Device-Win con Mono; inspección del build normal (AC-BLE-13-02, ahora también automática); revisión a mitad de WP-B en las entregas B-I y B-II (§6.3) |
| Architect (cierre de la It. 2) | ARCHITECTURE §2, §7.2-§7.3, §8, §13 y "Cambios de la It. 2 (diseño)"; ADR 006 (D-02, D-14), ADR 007 (D-09, con el motivo corregido: el legacy termina la app en Android 8-15) y **ADR 019 "Canal de color de la UI"** (D-01 y el resultado de S-B1, incluido el gancho de la copia) |

---

## 8. Cambios por 04-design-notes (antes de WP-A)

Pedido de QA-G2-03. 04-design-notes (14:16; corregida por el gate a las 15:02, y este anexo sigue esa versión) es posterior a la primera versión de este diseño (13:46) y hace 25 pedidos (DN-01 a DN-25, §9 de las notas) más algunos puntos sin número. Cada uno queda aceptado, aceptado con un cambio o rechazado, con el lugar del diseño que lo refleja. Regla: el pedido no cambia comportamiento ni datos (lo dicen las notas); cuando choca con la spec corregida (01-po-spec §11.1, §16) o con el legacy, manda la spec.

### 8.1 Pedidos numerados

| DN | Decisión | Qué cambia en este diseño | Dónde | Paquete |
|---|---|---|---|---|
| DN-01 | **Aceptado** | Paso nuevo **WP-D0** (Designer) con los 5 tokens y los pares N1-N13 antes de WP-A; orden WP-D0 → WP-A → WP-B → WP-D; `Tokens.cs` lo tocan WP-D0 y WP-D | §0, §6.0, §6.1 | D0 |
| DN-02 | **Aceptado** (= QA-G2-02) | `letter-spacing: 8px` en `.rm-type-label`, sin repetir en las escalas; `TypographyUssSyncTests` y `THM.LabelTrackingIsEightHundredthsEm` | §2.9, §3.12, §5.2 | A, B |
| DN-03 | **Aceptado** | Clases y colores de texto de las notas §2.3 (pistas y ayudas en `.rm-type-body-xs` = 12 dp, como la spec y el legacy; nombres y recordado en `.rm-type-body-m`; botones en `.rm-type-button`; `#hrm-bpm-unit` en `metric.hr.hud`, como quedó en las notas tras QA-G2-08 y DV-04, par N13 de 4,61:1) | §3.3, §3.4, §3.5, §3.12 (tabla de clases) | B |
| DN-04 | **Aceptado** | `.rm-button__state` en `RmButton`, chips, `IconButton`, filas `hrm-device-*` y la llave; `.rm-tab__state` en la caja del ícono de la pestaña | §3.2, §3.3, §3.4, §3.5, §3.7 | B (estructura), D (aspecto) |
| DN-05 | **Aceptado** | `.rm-focus-ring__halo` dentro de cada `.rm-focus-ring`; anillo también en pestañas, filas, chips, llave e interruptor | §3.2, §3.7 | B, D |
| DN-06 | **Aceptado** | `VerticalFadeTexture` (1 × 64, creada por código, sin asset) como fondo de `.rm-card__glow` (tablero y `#hrm-glow` nuevo) y de `.rm-tab-bar__sheen` nuevo; tinte y tamaño en USS | §3.2, §3.3, §3.4, §3.7, §3.12 | B, D |
| DN-07 | **Aceptado** | `AccentPulse` y `MotionSpecs.AccentGlowPeak = 0.14` (WP-A); destello **solo si cambió el texto**; borde por código con alfa `k` (0,60 en reposo, legacy `withOpacity(0.6)`; DV-02); brillo 0,14·`k`; tarjeta del pulsómetro con `k = 0,6` fijo. La tabla de contrato ya no dice "en el color de la métrica" a secas | §2.9, §3.3, §3.4, §3.9, §3.12 | A, B |
| DN-08 | **Aceptado** | `TabularNumber`: banda de mayúsculas centrada, `-` con su avance, `:`/`.` 0,32 em, ancho interpolado al cambiar el largo; `DigitRoll.Evaluate` con las curvas por celda de las notas §5.1 (`RollOutFadeEnd`, `RollInFadeStart`) | §2.9, §3.7 | A, B |
| DN-09 | **Aceptado** | `#hrm-bpm-unit` por línea de base con las métricas resueltas, dentro del `FitBox` | §3.4, §3.12 | B |
| DN-10 | **Aceptado** | `HeartbeatClock`: suma de los dos golpes, compresión `min(1; T/420 ms)`, parada a 1 en 120 ms, primer latido con el primer pulso > 0; `IsAtRest` para las tomas | §2.9 | A |
| DN-11 | **Aceptado** | Color de ícono y etiqueta de la pestaña por código (160 ms, `ease.move`) y estilo en línea borrado al terminar; §3.9 ya no lo pone en `Motion.uss`; escala del ícono 0-90-260; la píldora conserva la velocidad | §2.9, §3.2, §3.9 | A, B |
| DN-12 | **Aceptado con una precisión** | Vistas absolutas y superpuestas dentro de `#device-views`, transición `ViewSwap` de las notas §5.5. Precisión: **en reposo** la inactiva sigue con `display: none` (no recibe eventos ni ocupa *layout*; el desplazamiento de la conectada vuelve a 0 al reentrar, como el legacy) y, mientras sale, un bloqueador `TrickleDown` descarta sus eventos. Las pruebas usan `Settle()` (R2-17) | §3.3, §3.9, §5.1 | B |
| DN-13 | **Aceptado** | `overflow: hidden` en `#hrm-body` solo durante el `HeightTween`; opacidad del corazón **solo** por código, escrita siempre (en el acto al armar la tarjeta, fundido de 160 ms en cada cambio). WP-B **no** conserva la clase `.rm-hrm-card__heart--dim` (las notas lo dejan a elección); el contrato mira `resolvedStyle.opacity` | §3.4, §3.12 | B |
| DN-14 | **Aceptado** | `PopTransition`: abrir con opacidad y escala desde la llave; cerrar en el acto (paridad de LT-26 y LT-27) | §3.5, §3.9 | B |
| DN-15 | **Aceptado** | `FitBox` con resorte ω 12 cuando un dato cambia la escala; en el acto al primer cálculo, al redimensionar, al cambiar la letra y con reducir movimiento | §2.9, §3.3, §3.7 | A, B |
| DN-16 | **Aceptado** | Alto de la grilla y entrada/salida de PULSO de las notas §5.9; columnas sin animación (paridad) | §3.3 | B |
| DN-17 | **Aceptado** | `SwitchToggle` (`Toggle` sin *checkmark*, con `.rm-switch__track > .rm-switch__thumb`); el renglón entero lo cambia (= `SwitchListTile`, `simulator_overlay.dart:151`) | §3.5, §3.7 | B, D |
| DN-18 | **Aceptado** | `IconButton` `.rm-icon-button` en `#sim-close` y los ± | §3.5, §3.7 | B |
| DN-19 | **Aceptado**, como desvío **D-27** | Valor de los renglones ± de 64 dp (legacy 72, `simulator_overlay.dart:201`) | §3.5, §7.1 | B |
| DN-20 | **Aceptado** | `min-height` en botones, etiqueta `white-space: normal`, `flex-shrink: 0` en controles, íconos y filas | §3.7, §3.12 | B |
| DN-21 | **Aceptado** | Regla de toma de la foto fija (sin animaciones finitas, corazón en reposo, sin transiciones de USS, ventana `[N + 0,55; N + 1)`); las sondas de aspecto de las notas §7.2 entran al catálogo en WP-D | §3.9 (`HasFiniteMotions`), §4.1, §4.3 | B, D |
| DN-22 | **Aceptado** (el PO lo adoptó, QA-G2-04) | CAP-04a (sin desplazar) y CAP-04b. CAP-04b es "al final del desplazamiento" (spec §7.3), no `ScrollTo("hrm-card")`: así la tarjeta queda siempre sobre la barra | §4.3, §5.2 | B |
| DN-23 | **Aceptado** | *Padding* inferior por la barra **dentro** del contenido desplazable (y en `#device-scan-view`); `.rm-screen` sin *padding*. Es además la mitad de la regla única de área segura (QA-G2-06) | §3.1, §3.2, §3.3 | B |
| DN-24 | **Aceptado** | `#device-header` con `flex-wrap`, nombre con "…" (el legacy desbordaba a 360 dp con letra grande) | §3.3 | B, D |
| DN-25 | Nota para QA (sin cambio) | La marca "Development Build" de los builds con simulador es P-01, aceptada | — | — |

### 8.2 Puntos sin número de las notas

| Punto (04-design-notes) | Decisión | Motivo / dónde |
|---|---|---|
| §4.8 (primera versión): la cinta "en la esquina superior izquierda del **área segura**" | **No se adopta**; el Designer ya lo **retiró** en su corrección del gate (04-design-notes §4.8 y §14: "sin sumar insets propios") | La spec dice "misma ubicación" que el legacy (DV-01) y la `Banner` del legacy va en la esquina de la pantalla (`main.dart:108-110`; `simulator_overlay.dart:33-36`); la regla única de área segura (QA-G2-06) no deja que la capa del simulador consuma márgenes. En Windows y en el Editor los márgenes son 0. El resto de §4.8 (10/700 sin tracking, colores, geometría) es de WP-D. §3.1, §3.5, §3.12 |
| §2.3 (primera versión): `#hrm-bpm-unit` en `text.secondary` | Corregido por el Designer (QA-G2-08) a `metric.hr.hud`, que es DV-04 del PO | §3.12 (tabla de clases) |
| §4.5: `deviceNoResults` en `text.secondary` | **Aceptado** (DV-05, PO; QA-G2-11) | §3.3 |
| §5.0: nada se anima antes del primer cuadro presentado | **Aceptado** | `MotionDriver` "desarmado" hasta el primer `Tick`; §3.9 |
| §4.7: *spinner* de 270°, 900 ms por vuelta | **Aceptado** | `MotionSpecs.SpinnerTurnMs`; §3.7 |
| §4.4: brillo con alto 24 y radios superiores 14 | **Aceptado** (WP-D) | WP-B pone `.rm-card__glow` absoluto arriba (izquierda, derecha y arriba en 0) en `Base.uss`; alto, radios y tinte en `Skin.uss` |
| §1: llave con ícono `accent.primary` sobre `surface.overlay`; *chevron* `text.tertiary` | **Aceptado** (WP-D) | No cambian propiedades de contrato (la llave sigue 40 dp, a 16 de la derecha y 16 sobre la barra) |
| §8.3: respaldo de tokens si S-B1 cae a `Overlay` | **Aceptado** | Lo aplica WP-D (§6.4); la decisión la marca S-B1 (§3.1) |
| §10: "`Tokens.cs` en WP-D si DN-01 no se hizo antes" | Superado | DN-01 se hace en WP-D0 |
| §11: alineación de las biblias | **Aceptado** (Designer) | Fuera de este documento |
| §1: respuestas a D-01, D-07, D-10, D-11, D-13, D-14 y el corazón | Registradas | Columna "Decide" de §7.1 |

### 8.3 Sondas del Architect (evidencia de esta corrección)

Proyecto **descartable** en el scratchpad (`…/scratchpad/archfix-g2/proj`: copia del proyecto del *spike* S-0, con `uielements`, `test-framework` y el tema de *runtime* por defecto; *pipeline* integrado, sin URP), Unity 6000.3.27f1, batchmode. Nunca se abrió el repo ni se tocó `<repo>` con Unity. Una sola instancia por vez; los dos comandos terminaron con código 0 y sin `error CS`.

| Sonda | Comando | Resultado (textual, del registro) | Para |
|---|---|---|---|
| A · orden de arranque | `-executeMethod ArchProbe.RunPlay` (escena como `Boot.unity`: componente con `[DefaultExecutionOrder(-10000)]` + `UIDocument` con el asset; entra a Play) — `archfix-g2/play5.log` | `Awake: root=False docPanel=ArchPanel docPanelIsAsset=True` · `Awake: rt cleared first pixel=#0D1B2A` · `Awake: swapped; root=False` · `OnEnable: root=False` · `Start: root=True panel=True docUsesCopy=True assetTargetTextureStillNull=True` · `VariantB(inactive->AddComponent->panel->SetActive): root=True panel=True` · `VariantC after panel: root=True panel=True` · `Update4: rt pixel=#336699` (el `colorClearValue` de la copia) | QA-G2-01 (reproduce P3 y prueba la corrección), QA-G2-13 (RT limpia), `UiTestHost.Create` |
| A' · documento sin panel en la escena | mismas corridas con el `UIDocument` serializado sin panel (`play.log` a `play4.log`) | `Start: root=True panel=True docUsesCopy=True …` y `Update4: rt pixel=#336699` | el caso de `BootSceneBuilder` (§1.3) |
| A'' · hallazgo del *bootstrapper* | mismo `RunPlay`, primera versión | `ps after CreateAsset: null=False` → `ps after NewScene: null=True`; la escena guardada quedó con `m_PanelSettings: {fileID: 0}` | `NewScene(…, Single)` descarga los assets en memoria sin referencias: `BootSceneBuilder` vuelve a cargarlos después de crear la escena (§1.3) |
| B · *hit-test* y API | `-executeMethod ArchProbe.RunPick` (modo edición, panel en una RT de 400 × 300) — `archfix-g2/pick.log` | `android startInFullscreen=True renderOutsideSafeArea=True` · `hit b0 (visible) … picked=b0/Button targetOrDescendant=True` · `hit b5 (tapado por la barra absoluta) … picked=tab/Button targetOrDescendant=False` · `hit b8 (fuera del panel) … picked=null` · `hit c3 (recortado por sv2) … picked=null` · `hit fab (capa Ignore) … picked=fab/Button` · `pick (100,25) under ignore layer=b0` · `hit c3 tras scrollOffset, mismo cuadro … targetOrDescendant=True` · `hit b8 tras ScrollView.ScrollTo (queda abajo del viewport, bajo la barra) … picked=tab/Button targetOrDescendant=False` | QA-G2-07 (`Tap` con `panel.Pick`; `ScrollTo` propio), QA-G2-06 (valores por defecto de Android) |

Lo que no prueban (queda para S-B1 o equipo): el orden de repintado de los paneles con `targetTexture` respecto de los ganchos de URP (latencia, R2-16), el comportamiento en un *player* y todo lo de Android.

---

## Anexo A. Spike S-0 (evidencia)

Proyecto Unity **descartable** en el scratchpad de la sesión (`…/scratchpad/spk`, fuera del repo; manifest con `uielements`, `imgui`, `imageconversion`, `jsonserialize` y `test-framework`; Inter-Regular copiada del repo). Unity 6000.3.27f1, batchmode, `-executeMethod`, Direct3D 12 en el Editor. No se abrió ni se modificó `<repo>`.

| # | Pregunta | Resultado |
|---|---|---|
| 1 | ¿UI Toolkit se dibuja en una RT en batchmode, sin Play? | Sí. `UIDocument` en modo edición + `PanelSettings.targetTexture`; listo en el 3.er `EditorApplication.update` y estable en el 10 y el 40. También funciona entrando a Play desde `-executeMethod` (con recarga de dominio) |
| 2 | ¿La escala da los dp pedidos? | `ConstantPixelSize`, `scale = 2.625`, RT 1080 × 2400 → raíz de 411,43 × 914,29 dp |
| 3 | ¿Texto con una fuente propia? | Sí: `FontAsset.CreateFontAsset(Inter-Regular)` en tiempo de ejecución, y un `FontAsset` **persistido** por script (atlas y material como subassets) referenciado desde USS con `url("project://database/…")`. El USS tiene que importarse **después** de que exista el asset (si no, `MissingAssetReference` hasta reimportarlo) |
| 4 | R-T1 en RT | RT sRGB: vidrio sobre blanco `#6A6A6C`, `text.secondary` sobre card `#DADBDC` (lineal). RT UNorm sin forzar: bytes lineales (`#242526`, `#B3B4B7`). RT UNorm + `forceGammaRendering = true`: `#2A2F37` y `#BBC1C8`, con `bg.app`, `surface.card` y `metric.split` exactos |
| 5 | R-T1 en pantalla | Player de Windows (Mono, 400 × 300, ventana), *Screen Space Overlay*, lectura del *backbuffer* con `ReadPixels` al final del cuadro: `#6A6A6C` / `#DADBDC` **con y sin** `forceGammaRendering` |
| 6 | API | `PanelSettings.forceGammaRendering` es un campo público; no hay `textSettings`; `renderMode` = `ScreenSpaceOverlay` / `WorldSpace`; `SetScreenToPanelSpaceFunction` existe; `MeshGenerationContext.DrawText(string, Vector2, float, Color, FontAsset)`, `Painter2D.SaveToVectorImage` y `TextElement.MeasureTextSize` existen; `UIDocument` es `[ExecuteAlways]` |
| 7 | ¿Cambios cuadro a cuadro? | Una caja movida con `style.translate` en cada `update` aparece en la posición esperada en los cuadros 10 y 40 |
| 8 | ¿Videos? | `UnityEditor.Media.MediaEncoder` (H.264, 30 fps) escribió un MP4 de 60 cuadros en batchmode |
| 9 | Trampas de compilación | `Color32` → `StyleColor` necesita `(Color)`; `TextElement` ambiguo con `UnityEngine.TextCore.Text` |

Logs de las corridas: `…/scratchpad/spk-run3.log` (render y R-T1 en RT), `spk-player.log` (R-T1 en pantalla), `spk-run5.log` (`forceGammaRendering`), `spk-run7.log` (cuadro a cuadro y MP4), `spk-run11.log` (fuente por USS).

---

## Cambios por gate QA

Correcciones por el gate de armonía de QA ([03-qa-harmony](03-qa-harmony.md), APROBADO CON OBSERVACIONES), 2026-10-27, issues del Architect. Cada corrección se verificó contra el legacy (`device_screen.dart:80-136`, `:390-445`; `heart_rate_card.dart:1-238`; `main.dart:85-112`; `simulator_overlay.dart:20-100`, `:151`, `:201`; `simulated_ble_service.dart:14-40`; `test/heart_rate_card_test.dart:1-82`, `:155-200`; `test/simulated_ble_service_test.dart:13-38`; `android/app/src/main/res/values/styles.xml`; SDK local de Flutter: `material/app_bar.dart:1191`, `material/navigation_bar.dart:291`, `widgets/banner.dart:18-30`) y, donde hacía falta, con dos sondas propias en Unity 6000.3.27f1 sobre un proyecto descartable (§8.3). Se alineó con la spec corregida por el PO (01-po-spec §11.1 DV-01 a DV-08 y §16: AC-BLE-11-07, AC-BLE-11-06, AC-APP-06-01, AC-HRM-13-06, VID-01, CAP-04a/b, D-09) y con 04-design-notes (§8). No se disputa ningún issue; donde QA proponía alternativas (QA-G2-01, -05, -06, -16, -17, -19) se dice cuál se eligió y por qué, en la fila.

| Issue | Sev. | Qué cambió | Dónde |
|---|---|---|---|
| QA-G2-01 | mayor | `AppRoot` configura en `Awake` (pasos 1-6, y ahí le da al `UIDocument` la copia del panel, antes del `OnEnable` del documento) y arma la UI en `Start` (pasos 7-8), con guarda si la raíz fuera nula. `[RequireComponent(typeof(UIDocument))]`. El documento de la escena queda **sin** panel (`BootSceneBuilder`), y el *bootstrapper* recarga los assets después de `NewScene` (hallazgo de la sonda A''). `UiTestHost` crea el documento con "inactivo → panel → activo" (la raíz existe al volver). Prueba nueva `BootSceneTests.BootSceneStartsWithoutExceptions` (carga `Boot.unity`, 5 cuadros, sin `Error`, documento con una copia, asset intacto). Sonda A: reproduce P3 (`Awake`/`OnEnable` sin raíz) y confirma la corrección (`Start: root=True … docUsesCopy=True`, RT pintada con el color de la copia) | §1.1, §1.3, §1.4, §4.2, §5.2, §7.2 R2-09, §8.3 |
| QA-G2-02 | mayor | `.rm-type-label { letter-spacing: 8px; }` (= 0,08 em; unidad de TextCore 1/100 em) con el comentario de la unidad, sin repetirlo en `.rm-text-scale-*`; `TypographyUssSyncTests` fija ese valor y `THM.LabelTrackingIsEightHundredthsEm` lo mide. Antes de WP-A | §2.9, §3.12, §5.2, §6.2 |
| QA-G2-03 | mayor | Anexo **§8** con los 25 DN uno por uno (24 aceptados: DN-12 con una precisión y DN-19 como desvío D-27; DN-25 es una nota para QA) y los puntos sin número (la cinta en el área segura no se adopta; el Designer ya la había retirado en su corrección de las 15:02). Alineado con la versión de 04-design-notes corregida por el gate (§14 de las notas: `bpm` en `metric.hr.hud`, WP-D0 con N1-N13, DN-13 con un solo mecanismo). Árboles de §3 actualizados (elementos nuevos de DN-04/05/06/17/18, `#device-views`, `#hrm-glow`, `.rm-tab-bar__sheen`), clases de texto (pistas en 12 dp, `.rm-type-body-m`, `-body-xs`, `-button`), contrato §3.12 (borde al 60 % por código, opacidad del corazón solo por código, tabla de clases de texto, lista de elementos para WP-D), archivos y dueños de §1.1 (`AccentPulse`, `ViewSwap`, `PopTransition`, `IconButton`, `SwitchToggle`, `VerticalFadeTexture`), `MotionSpecs` con los números de las notas, y orden de §6.0 con **WP-D0** (Designer: 5 tokens + `Tokens.uss`, `--check` en verde) antes de WP-A | §0, §1.1, §2.9, §3.2-§3.12, §6.0, §6.1, §8 |
| QA-G2-05 | mayor | (a) `RowMate.Tests.UiSupport` referencia `RowMate.Simulator` y `RowMate.Simulator.Presentation` por nombre (Unity las descarta sin el define, como en `RowMate.Bootstrap`) y el código que los usa va en `#if ROWMATE_SIMULATOR`; el *lint* lo permite. (b) El `enum HostBackend` se reemplaza por `HostBackendFactory` + `HostBackends` (`TestDevices`, `Unavailable`, `Simulator(noise)`, `TestRowerWithSimulatedHeartRate` para LT-31). (c) "Prueba" monta la capa en `SimulatorLayerMode.ChromeOnly` (cinta y llave, sin panel; `SimulatorLayerView` con `store == null`). Se eligió el modo "solo cinta y llave" y no un `SimulatorBackend` con remo externo: no agrega código al simulador de producción | §1.1, §1.2, §3.5, §4.2, §4.3, §5.3 |
| QA-G2-06 | mayor | **Una sola regla** de área segura: raíz sin *padding*; la barra superior consume el margen superior y los laterales, la de pestañas el inferior y los laterales, el contenido no consume laterales (paridad con Flutter: `AppBar` = `SafeArea(bottom: false)`, `NavigationBar` = `SafeArea`, cuerpo sin relleno; era la parte "si se decide" del issue) y lleva adentro el alto real de la barra; la capa del simulador no consume nada (cinta en la esquina de la pantalla, como la `Banner`). `MaxHeight(alto de la ventana, márgenes)` con el margen inferior restado una vez (**D-26**, para el PO). Pruebas `SafeAreaTests` (TV con 24 / 48 y TH con lateral 32) y `SOT.InsetsAreCountedOnce`. Android: `startInFullscreen = false` y `renderOutsideSafeArea = true` en el *bootstrapper* (sonda B: Unity arranca en `true`); NO VERIFICABLE sin el módulo (R2-15) | §1.1, §1.3, §2.1, §2.7, §3.1, §3.2, §3.5, §3.12, §5.2, §7.1, §7.2 |
| QA-G2-07 | mayor | `UiTestHost.Tap` / `TapText` con *hit-test*: centro dentro de la ventana y `panel.Pick(centro)` = el elemento o un descendiente; si no, falla con el motivo. `TapUnchecked` explícito (hoy nadie lo usa). `ScrollTo` propio que deja el elemento sobre la barra (sonda B: `ScrollView.ScrollTo` lo deja bajo la barra absoluta), más `ScrollToEnd` y `Settle` | §4.2, §4.3, §5.1, §8.3 |
| QA-G2-12 | menor | `#hrm-card-slot-connected` con `margin-bottom: 48` + *padding* inferior 16 = los 64 dp que fija la AC-BLE-11-07 corregida (`device_screen.dart:123`, `:135`); prueba `DSP.ConnectedViewScrollsAsOneBlockWithMargins` con 24 / 64 y CAP-04b | §3.3, §3.12, §4.3, §5.2 |
| QA-G2-13 | menor | Las dos RT se limpian a `bg.app` al crearlas (`GL.Clear`; sonda A: `#0D1B2A`); la copia va en un gancho de render posterior al repintado (no en `Update`); S-B1 suma "primer cuadro presentado = `bg.app`" y "latencia 0" a sus criterios (si ningún presentador cumple, `Overlay`); Android anotado como NV | §1.4, §3.1, §5.2, §6.3, §7.2 R2-16, §7.3 |
| QA-G2-14 | menor | `TabularNumber.Text`; `UiTestHost.TextOf` / `IsTextVisible` leen `Label` y `TabularNumber`. `LayoutAudit` con contenedores explícitos (ventana, `.rm-screen`, `.rm-appbar`, `.rm-card`, `.rm-tab-bar`, `.rm-sim-panel`, contenido de cada `ScrollView`) y exenciones declaradas | §3.7, §4.2, §5.1 |
| QA-G2-15 | menor | `RealtimeTimerQueue` (Core, pura, WP-A) debajo de `UnityScheduler`: si el atraso de un periódico llega a un período, se reancla en `ahora + período` (las lecturas perdidas se pierden, no llegan una por cuadro). `RealtimeTimerQueueTests` [d] con un salto de 5 s; lo enlaza AC-APP-02-07 | §1.1, §2.1, §5.2 |
| QA-G2-16 | menor | El cambio de `tools/dotnet/run-tests.ps1` pasa a WP-A: `-Matrix` lee todos los `It0*Ids.cs`, marca `PENDIENTE UNITY` los `It02Ids.UnityOnlyIds` sin resultado y acepta `-UnityResults`. El "Hecho cuando" de WP-A ya no depende de un archivo de WP-B (se tomaron las dos sugerencias de QA juntas: el dato en `It02Ids`, el script en WP-A) | §1.1, §6.2, §6.3 |
| QA-G2-17 | menor | AC-BLE-13-02: `BuildOutputInspector` al final de cada build normal (sin `RowMate.Simulator*.dll` ni las cadenas en UTF-8 o UTF-16) + prueba [u] + revisión estática del script. AC-APP-02-06: `ShellHandle.HandleBack` → `IBackButtonPolicy` (Core) con una política falsa en PlayMode y revisión estática de `AppRoot` (se eligió no simular la tecla con el *fixture* del Input System: el ruteo es lo que tiene lógica; la tecla en el teléfono es [Device-And]). AC-APP-09-06: `SystemLocaleProviderTests` [u] en el Editor de Windows. AC-APP-01-07: primer cuadro y RT limpias (S-B1, `THM.SurfaceRtsStartCleared`). §7.3 dice qué parte de cada uno queda automatizada | §1.1, §1.2, §3.10, §5.2, §7.3 |
| QA-G2-19 | menor | Registrado como **D-25**: el port usa `e.Message`; imitar el `toString()` de Dart con excepciones de .NET o de un *plugin* no daría el mismo texto; el texto real se fija en la It. 11 | §2.5, §7.1 |
| Sin número (QA §2) | — | La cinta pasa a ser el **último** hijo de `#simulator-layer`; LT-20 deja escrito que `Status` se graba sin el valor actual y `AdapterState` con el actual | §3.5, §5.3 |
| Sugerencia (QA §8) | — | WP-B en tres entregas con su "Hecho cuando" (B-I plataforma, B-II UI, B-III arnés y herramientas) | §6.3 |

**Alineación con la spec corregida** (sin issue propio del Architect): CAP-04 partido en CAP-04a y CAP-04b en el catálogo (pendiente que la spec §15 le dejaba al Architect); VID-01 con destello solo en las tarjetas que cambian; D-09 con el motivo corregido (el legacy termina la app en Android 8-15; DV-06), y la corrección de ADR 007 queda para el cierre con ese motivo (§7.4); estados de D-01, D-06, D-24 y de la aclaración del corazón actualizados en §7.1; `bpm` en el matiz del pulso (DV-04) y la etiqueta y el ícono de PULSO en `metric.hr.hud` (DV-03) en el contrato.

**Numeración**: §6.1 es ahora WP-D0, WP-A pasa a §6.2, WP-B a §6.3 y WP-D a §6.4; §8 es nuevo. 04-design-notes cita "02-tech-design §6.3" para WP-D: ahora es §6.4.

`git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó `lib/core/strava/strava_secrets.dart`. En el repo solo se modificó este archivo; las sondas corrieron en `…/scratchpad/archfix-g2`.
