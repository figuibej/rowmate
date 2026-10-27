# It. 2 — Gate de armonía de QA (antes de implementar)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-27 |
| Iteración | 2 · `02-dispositivo-simulador` ([ROADMAP §It. 2](../../ROADMAP.md)) |
| Entradas | [01-po-spec](01-po-spec.md) (125 AC, 15 LT, 25 CAP + 3 VID), [02-tech-design](02-tech-design.md) (13:46), [04-design-notes](04-design-notes.md) (14:16), [00-primer-open](00-primer-open.md) |
| Legacy | `rowmate-legacy` en `3ae0f3a`, solo lectura. Leídos completos: `lib/main.dart`, `lib/core/dev/{dev_config,rowing_simulator,simulator_overlay}.dart`, `lib/core/bluetooth/simulated_{ble,heart_rate}_service.dart`, `lib/features/device/{device_provider,device_screen,heart_rate_card,heart_rate_provider}.dart`, `lib/shared/theme.dart`, `lib/features/workout/scene/scene_settings.dart:1-43`, `ble_service.dart:1-80`, los 5 tests de §6 de la spec y `test/widget_test.dart`; claves de la It. 2 en `lib/l10n/app_{es,en}.arb`; `android/.../styles.xml`, `AndroidManifest.xml:20-40`, `windows/runner/main.cpp:20-40`. Además, el SDK de Flutter local (`<flutter-sdk>`) para los valores por defecto de `Banner` y `NavigationBar`. No se leyó `strava_secrets.dart`. `git -C rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Evidencia propia | (1) `dotnet test` de línea de base; (2) tres sondas de Unity 6000.3.27f1 en batchmode sobre una **copia** del proyecto en el scratchpad (URP 17.3, D3D11, Linear, igual que el repo; el repo no se tocó). Detalle en §1 |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.**

El QUÉ y el CÓMO están en armonía en lo esencial: los 125 AC tienen componente y prueba o captura planificada, los 15 LT están mapeados 1 a 1 con nombre y línea correctos, y las reglas, números y tiempos del legacy que muestreé (23 casos, §6) están bien portados en la lógica pura. El canal de color (D-01) y el arnés en modo edición, que eran los dos riesgos técnicos grandes, los confirmé en la configuración real del proyecto (§9).

No hay bloqueantes. Hay **7 issues mayores** que hay que resolver en el orden de abajo; ninguno pide rehacer el diseño, pero dos tocan archivos de WP-A y uno rompería el arranque de la app.

| Cuándo | Issues | Quién |
|---|---|---|
| **Antes de WP-A** | QA-G2-02 (`letter-spacing`), QA-G2-03 (integrar 04-design-notes en 02-tech-design, paso D-0 de tokens) | Architect + Designer |
| **Antes de B.5 de WP-B** | QA-G2-01 (`AppRoot` arma la UI en `Awake` con la raíz nula), QA-G2-05 (`UiTestHost` no puede armar las variantes pedidas), QA-G2-06 (área segura contada dos veces), QA-G2-07 (`Tap` sin *hit-test*) | Architect |
| **Antes de B.7 (capturas)** | QA-G2-04 (CAP-04 imposible en 1280 × 720) | PO |
| Al cerrar la It. 2 | menores QA-G2-08 a QA-G2-19 | cada dueño |

---

## 1. Qué se corrió (evidencia)

`<S>` = `<scratch>/qa-gate2`.

**1.1 Línea de base de dotnet** (`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <S>/dotnet`, código 0):

```
Test Run Successful.  Total tests: 69   Passed: 69                 (RowMate.Tests.Simulator)
Test Run Successful.  Total tests: 892  Passed: 875  Skipped: 17   (RowMate.Tests.EditMode)
```

= 961 tests, 944 pasan, 17 ignorados, 0 fallan: lo mismo que 00-primer-open §0. WP-A arranca desde verde.

**1.2 Sondas de Unity** (copia del repo en `<S>/proj`, sin `Library/`; fuentes de la sonda guardadas en `<S>/probe-src/QaProbe`). Una sola instancia de Unity a la vez; ningún Editor abierto sobre el repo (solo la Hub, que no se tocó). Primer intento falló por la ruta larga del scratchpad al clonar el submódulo de `com.gilzoide.sqlite-net` (`Filename too long`, `<S>/open1.log`); se resolvió copiando esa entrada de `Library/PackageCache` del repo. No afecta al repo (ruta corta), pero ver §12.

| Sonda | Comando | Resultado (log) |
|---|---|---|
| P1 compilación de API | `-batchmode -quit` con un asmdef `QaProbeApi` que solo usa las API que el diseño nombra | código 0, `QaProbeApi.dll` compilado, sin `error CS` (`<S>/open2.log`) |
| P2 comportamiento | `-batchmode -executeMethod QaProbe.Run` (modo edición, sin `-quit`) | `<S>/probe1.log:403-548`, código 0 |
| P3 orden de arranque | `-batchmode -executeMethod QaPlayProbe.Run` (entra a Play con un GameObject como el de `Boot.unity`) | `<S>/probe2.log:552-766`, código 0 |

Salida de P2 y P3 (textual):

```
[QAPROBE] gfx=Direct3D11 batch=True cs=Linear srp=UniversalRenderPipelineAsset
[QAPROBE] GammaRt(UNorm+force) glassOnWhite=#2A2F37 textSecOnCard=#BBC1C8
[QAPROBE] Overlay-like(SRGB) glassOnWhite=#6A6A6C textSecOnCard=#DADBDC
[QAPROBE] CopyTexture UNorm->SRGB glassOnWhite=#2A2F37 textSecOnCard=#BBC1C8 sameBytes=True
[QAPROBE] scroll before hide: display=500 vis=500; clicks while disabled=0
[QAPROBE] scroll while hidden: display=500 vis=500
[QAPROBE] scroll after show: display=500 vis=500; clicks after enable=1
[QAPROBE] letterSpacing 12dp: 0px=32.00 0.96px=33.00 8px=41.00; 48dp: 0px=127.00 8px=162.00
[QAPROBE] UIDocument DefaultExecutionOrder=-100
[QAPROBE] Awake: doc=True panelSettings=True root=False
[QAPROBE] OnEnable: root=False
[QAPROBE] Start: root=True rootPanel=True
```

Lectura: (a) R-T1 y la opción A dan lo mismo que S-0 **con URP y D3D11** (S-0 fue sin URP y en D3D12); (b) `CopyTexture` UNorm → sRGB conserva los bytes en el Editor; (c) una `ScrollView` conserva su desplazamiento con `display: none` y con `visibility: hidden`; (d) un `Button` deshabilitado no responde a `NavigationSubmitEvent`; (e) `letter-spacing` escala con el tamaño de letra (≈ N/100 em), como midió el Designer; (f) `UIDocument.rootVisualElement` es **nulo** en `Awake` y `OnEnable` de un componente con `[DefaultExecutionOrder(-10000)]` en el mismo GameObject.

---

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-G2-01 | mayor | architect | `AppRoot` arma la UI en `Awake` (pasos 1-8), pero ahí `UIDocument.rootVisualElement` es nulo: la app no arranca (`ShellHost.Build` recibe `null`) | 02-tech-design §1.4:354 y :386; P3: `Awake: … root=False`, `OnEnable: root=False`, `Start: root=True`; `UIDocument` tiene `DefaultExecutionOrder(-100)` y `AppRoot` −10000 | Pasos 1-6 en `Awake` (asignar ahí la copia del `PanelSettings`, antes del `OnEnable` del `UIDocument`) y pasos 7-8 en `Start`; o crear el `UIDocument` por código después de configurar. Test PlayMode `BootSceneStartsWithoutExceptions` (carga `Boot.unity`, 5 cuadros, sin `Error` en el log) |
| QA-G2-02 | mayor | architect | `Typography.uss` generado con `.rm-type-label { letter-spacing: 0.96px }`: en UI Toolkit 6000.3 eso agrega ≈ 0,0096 em por letra, no 0,08 em. Es archivo de WP-A | 02-tech-design §2.9:931; P2: a 12 dp, `0px=32.00`, `0.96px=33.00`, `8px=41.00`; a 48 dp, `0px=127.00`, `8px=162.00` (escala con la letra). Coincide con 04-design-notes §2.2 / DN-02 | `letter-spacing: 8px` con el comentario de la unidad; `TypographyUssSyncTests` con ese valor; el test PlayMode de DN-02. Corregir la fila de §2.9 antes de WP-A |
| QA-G2-03 | mayor | architect + designer | 04-design-notes (posterior al diseño técnico) hace 25 pedidos (DN-01 a DN-25) que el diseño técnico no integra, y varios lo contradicen: **DN-01** pide un paso de tokens **antes de WP-A** (contra §6.0: orden WP-A → B → D y `Tokens.cs` solo en WP-D); **DN-03** cambia clases de texto (`#device-search-hint` y `#hrm-watch-hint` son `.rm-type-caption` = 11 dp en §3.3:1056 y §3.4:1086, pero la spec y el legacy dicen 12 dp: AC-BLE-05-01, `heart_rate_card.dart:103-104`); **DN-07** acento @60 % en reposo (§3.12 dice "en el color de la métrica"); **DN-11** color de la pestaña por código (§3.9:1186 lo pone en `Motion.uss`); **DN-12** vistas superpuestas y absolutas (§3.3 cambia `display`); **DN-19** 64 dp (§3.5:1109 dice 72); **DN-23** *padding* dentro del `ScrollView` (§3.2:1025 lo pone en `.rm-screen`); DN-04/05/06/17/18 suman elementos que crea WP-B (`.rm-button__state`, `.rm-tab__state`, `.rm-focus-ring__halo`, `.rm-card__glow`/`#hrm-glow`, `.rm-tab-bar__sheen`, `.rm-switch__track/__thumb`, `.rm-icon-button`); DN-07/08/10 cambian constantes y curvas de WP-A (`MotionSpecs`, `DigitRoll`, `HeartbeatClock`) | 02-tech-design §1.1, §2.9, §3, §3.12, §6.0:1671-1672; 04-design-notes §2.1, §2.3, §9 | Antes de WP-A, un anexo en 02-tech-design ("§8 Cambios por 04-design-notes") que acepte o rechace cada DN, actualice los árboles de §3, la tabla de contrato de §3.12 (con el alfa 0,6 del acento y la clase de cada texto), los archivos y dueños de §1.1 y el orden de §6.0 con un **WP-D0** (Designer: 5 tokens + `Tokens.uss`, `--check` en verde) antes de WP-A |
| QA-G2-04 | mayor | po | CAP-04 no se puede tomar: en W1280 la tarjeta del pulsómetro arranca en y = 666,7 dp y la barra de pestañas ocupa 656-720 (en el legacy, con barra de 80, lo visible termina en 640: tampoco se ve) | Cálculo: 56 + 16 + 8 + 32 + 24 + 2 × 253,3 + 12 + 12 = 666,7 (celda (1240 − 24) / 3 = 405,3; alto / 1,6). 01-po-spec CAP-04:445; 04-design-notes §3:119 y §7.2:456 (DN-22); 02-tech-design §4.3:1350 la toma sin desplazar y la sonda del pulso caería bajo la barra | Adoptar DN-22: CAP-04a (desplazamiento 0, grilla de 3 columnas) y CAP-04b (`ScrollTo("hrm-card")`, `142` `bpm`, `Desconectar`); actualizar §7.3 de la spec y el catálogo §4.3 |
| QA-G2-05 | mayor | architect | `UiTestHost` no puede armar lo que el plan le pide: (a) `RowMate.Tests.UiSupport` no referencia `RowMate.Simulator` ni `RowMate.Simulator.Presentation`, y `HostBackend.Simulator` necesita `SimulatorBackend.Create(noise: false)` y la capa (la fábrica de Bootstrap fija `noise: true`); (b) LT-31 pide remo de prueba **+ pulsómetro simulado**, combinación que `HostBackend` no tiene; (c) las capturas "Prueba" montan la capa del simulador "sin abrir", pero `SimulatorPanelStore` exige un `SimulatorDeviceBackend` | §1.2:246; §1.4:376; §4.2:1283; §5.3 LT-31; §4.3:1343; §2.7:773 | Sumar las dos referencias al asmdef (sin el define Unity las descarta, como en `RowMate.Snapshots`) y el código en `#if ROWMATE_SIMULATOR`; reemplazar el `enum` por una fábrica de *backend* (`Func<IScheduler, IAppLogger, DeviceBackend>`); para "Prueba", capa en modo "solo cinta y llave" o `SimulatorBackend` que acepte un remo externo |
| QA-G2-06 | mayor | architect | El área segura se aplica de forma inconsistente: §3.1 la pone como *padding* de la raíz, y además la barra suma el inset inferior (§3.2: "64 + área segura"), el panel resta el superior otra vez (`MaxHeight(alto de la capa, área segura superior)`, §3.5:1118) y la cinta se ubica "en el área segura" (04-design-notes §4.8:277). Con insets (Android: barra de estado 24, gestos 48) el inferior se cuenta dos veces (franja de fondo bajo la barra) y el superior se resta dos veces del panel. El legacy no rellena el contenido a los costados, la `NavigationBar` llega al borde y la `Banner` va en la esquina de la pantalla. Tampoco está fijado el modo de la barra de estado en Android (Unity arranca en pantalla completa por defecto; el legacy la muestra) | §3.1:957; §3.2:1024; §3.5:1118; `main.dart:108-110`, `simulator_overlay.dart:33-36` | Una sola regla: raíz sin *padding*; la barra superior consume el inset superior, la de pestañas el inferior, el contenido los laterales (si se decide); `MaxHeight` con el alto de la ventana. Test PlayMode con `FakeDisplayMetrics` con insets (TV, 24 / 48). Fijar en el bootstrapper el modo de la barra de estado en Android (NV hoy) |
| QA-G2-07 | mayor | architect | `UiTestHost.Tap` manda `NavigationSubmitEvent` al elemento por nombre, sin *hit-test*: un botón tapado por la barra de pestañas (absoluta, encima del contenido), por el panel del simulador o fuera de la vista se "toca" igual. Los tests legacy tocan con *hit-test*, y LT-27, LT-28 y LT-30 dependen de eso ("dentro de la ventana", "hay que desplazarse", "alcanzable desplazando") | §4.2:1304; §5.1 "Cómo son las pruebas de UI". P2: deshabilitado → 0 clics, habilitado → 1 (eso sí funciona) | `Tap` verifica que `panel.Pick(centro del worldBound)` cae en el elemento o un descendiente (y que está dentro del *viewport* visible) antes de mandar el evento; `IPanel.Pick` existe en 6.3 (P1). Un `TapUnchecked` explícito solo para lo que el legacy toca por API |
| QA-G2-08 | menor | designer | Color de `bpm`: el legacy lo dibuja en el rosa del pulso al 70 % y la spec pide "`142` `bpm` en rosa"; las notas lo ponen en `text.secondary` (blanco 70 %) | `heart_rate_card.dart:178-180`; 01-po-spec CAP-04:445; 04-design-notes §2.3:91 y §4.6 | `metric.hr.hud` (`#FF5A7E`, mismo matiz, 4,61:1) o que el PO acepte el cambio por escrito |
| QA-G2-09 | menor | po | Desvíos del legacy que la spec da como paridad sin marcarlos: (a) la cinta tiene texto **blanco** en el legacy (`Banner` de Flutter, `_kTextStyle` `#FFFFFF`, 10,2/900) y la spec pide texto oscuro; se justifica (blanco sobre `#FF5722` da 3,2:1), pero va como decisión; (b) el borde superior de las tarjetas es el color de la métrica **@60 %** (`device_screen.dart:300`, `heart_rate_card.dart:32`), no "el color de su métrica" (AC-BLE-11-06, AC-APP-06-01); (c) ícono y etiqueta del tablero @70 % (`:310`, `:315`) y título `PULSÓMETRO` @70 % (`heart_rate_card.dart:45`) | `flutter/.../widgets/banner.dart:25-30`; 01-po-spec :215, :179, :255 | Anotarlos en §15 / §11 de la spec; en AC-BLE-11-06: "@60 % en reposo, 100 % en el destello" (lo que ya hacen las notas, DN-07) |
| QA-G2-10 | menor | po + designer | VID-01 pide "brillo del borde superior **en cada lectura**"; las notas lo dan solo en las tarjetas cuyo texto cambió (sin ruido, solo DISTANCIA) | 01-po-spec :467; 04-design-notes §5.2:347 y §7.2 VID-01 | Alinear el texto de VID-01 con §5.2 de las notas (es lo que pide la spec §10.2: "cada dato nuevo se nota") |
| QA-G2-11 | menor | designer | `deviceNoResults` va en blanco al 54 % en el legacy; las notas lo ponen en `text.secondary` (70 %) | `device_screen.dart:430-431`; 04-design-notes §4.5:213 | `text.tertiary` o anotarlo como cambio |
| QA-G2-12 | menor | architect | La vista conectada del legacy deja 32 + 16 dp debajo de la tarjeta del pulsómetro (además del relleno de 16); el diseño no los tiene, así que el final del desplazamiento difiere (L640, TH) | `device_screen.dart:123`, `:135`; 02-tech-design §3.3 | `margin-bottom: 48` en `#hrm-card-slot-connected` o anotar el desvío |
| QA-G2-13 | menor | architect | Con `GammaRt`, el presentador puede mostrar un primer cuadro sin limpiar (la RT sRGB recién creada) y un cuadro de atraso si la copia corre en `Update` antes de que UI Toolkit repinte la RT. AC-APP-01-07 pide `#0D1B2A` desde el primer cuadro. El fondo y el *splash* de Android tampoco los configura el bootstrapper (paso `player.window` es solo Standalone) | §1.4 `Update()`, §3.1; P2 (`CopyTexture` conserva bytes, así que el mecanismo vale) | Limpiar las dos RT a `bg.app` al crearlas, copiar después del repintado de la RT, y sumar a S-B1 "primer cuadro presentado = `bg.app`" y "latencia de 0 cuadros". Android: NV sin módulo, dejarlo anotado |
| QA-G2-14 | menor | architect | Consultas y auditoría de los tests: (a) los números del tablero y del pulso los dibuja `TabularNumber` con `DrawText` (no hay `Label`), así que "se ve `--`", "se ve el bpm", "`02:13`" necesitan que `UiQuery` lea el valor de `TabularNumber`; (b) `LayoutAudit` solo marca lo que sale de "un contenedor que recorta", pero en UI Toolkit casi nada recorta: es más débil que "nada fuera de su tarjeta ni de la ventana" (spec §0.3) y que el *overflow* de Flutter | §3.7 `TabularNumber`; §5.1; LT-28, AC-BLE-11-10 | `UiQuery.FindText` que incluya `TabularNumber.Text`; `LayoutAudit` con contenedores explícitos (`.rm-card`, `.rm-screen`, raíz, contenido de `ScrollView`) |
| QA-G2-15 | menor | architect | `UnityScheduler`: "a lo sumo un disparo periódico por cuadro" con vencimientos en múltiplos exactos hace que, después de un corte del *loop* (en Windows, arrastrar o redimensionar la ventana bloquea el *loop* modal, que es justo AC-APP-02-07; en Android, la pausa), las lecturas perdidas lleguen una por cuadro: N lecturas en N cuadros, la distancia y el pulso "corren". Los timers de Dart del legacy siguen en su hilo | §1.1 Platform `UnityScheduler`; ADR 017 (4a) | Política explícita para la It. 2: si el atraso pasa de un período, se reancla en `ahora + período` (se pierden las lecturas, no se comprimen). Test EditMode del *scheduler* con un salto de 5 s |
| QA-G2-16 | menor | architect | El "Hecho cuando" de WP-A usa `run-tests.ps1 -Matrix` mostrando los [p]/[ps]/[u] como "pendiente WP-B", pero `run-tests.ps1` es de WP-B | §6.1:1682; §6.2 (dueño de `tools/dotnet/run-tests.ps1`) | Que lo resuelva `It02Ids`/`TraceabilityTests` (WP-A) o pasar ese cambio del script a WP-A |
| QA-G2-17 | menor | architect | Automatizaciones baratas para cuatro de los "sin test" (ver §4) | §7.3 | Ver §4 |
| QA-G2-18 | menor | po | Confirmaciones pendientes que no frenan: D-01 (la evidencia de §1.2 la respalda), D-06 (`snapshots/`), D-09 ("Atrás" en Android 12+ manda la app al fondo), D-24 (arnés en modo edición), la aclaración del corazón (`--` conectado = pleno y quieto), la frase del ROADMAP de §15 | 02-tech-design §7.1, §7.4 | Decidir y reflejarlo en la spec |
| QA-G2-19 | menor | architect | El error de conexión se interpola con `e.Message`; el legacy usa `'${hr.error}'` (el `toString()` de Dart, con prefijo de tipo: `Exception: …`). Con el simulador no se ve; con sensores reales (It. 11) cambia el texto | `heart_rate_card.dart:57`; 02-tech-design §2.5 | Anotarlo como desvío (D-25) o emular `Tipo: mensaje` |

Además, sin número (observaciones chicas para WP-B): la cinta es el **primer** hijo de `#simulator-layer` (§3.5:1102); para garantizar "por encima de toda la app" conviene que sea el último. Y `StateRecorder` en LT-20 graba `Status` sin el valor actual y `AdapterState` con el actual (es la única forma de obtener `[On]` de un observable constante): dejarlo escrito en el test.

---

## 3. Cobertura: AC → componente → test o captura

Comparé la lista de los 125 AC de la spec (`**AC-… ·`) con las filas de 02-tech-design §5.2: **los 125 están** (122 filas + "AC-APP-16-01 a -04"). Ninguno queda sin componente ni sin verificación planificada. Estado del gate: **OK** = componente y prueba coherentes con la spec y el legacy; **OBS** = cubierto, con un issue de §2; **EQ** = solo en equipo/QA/CI (§4).

| Ficha | AC | Componente | Test / captura | Gate |
|---|---|---|---|---|
| F-APP-01 | -01 | `ShellStore`, `TabHost` | SHL, SHT, [eq] | OK |
| | -02 | `SimulatorBackend`, `DeviceBackendFactory` | SBK ×2, SMT | OK |
| | -03 | `UnavailableRowerService`, `SimulatedRowerService.Start`, `AppContext.Start` | UNV, NBT, DSS | OBS QA-G2-01 |
| | -04 | `AppContext.Start`, `SimulatedHeartRateService.AutoConnectAsync` | APC ×2, SHS, DSS | OK |
| | -05 | `TabHost` + `ViewBinding` | DSS (10 s en Rutinas) | OK |
| | -06 | `PlayerSettingsStep`, capa del simulador | SHT, BVT, CAP-01/22, [eq] | OK (P-01) |
| | -07 | `PlayerSettingsStep`, `UiSurface`, `UiCamera` | BVT (config) + [eq] | EQ · OBS QA-G2-13 |
| | -08 | `AppContext.Dispose`, `DeviceBackend.Dispose` | APC, SBK, LT-20..25, LT-32 | OK |
| F-APP-02 | -01 | `ShellStore.Destinations`, `TabBarView` | SHL, SHT, CAP-20 | OK |
| | -02 | `ShellOptions` | SHL, SHT, CAP-25 | OK |
| | -03 | `ShellStore.Select`, `TabBarView` | SHL, SHT, CAP-20 | OK |
| | -04 | `TabHost` (`display`) | SHT, DSS | OK (P2: el desplazamiento sobrevive a `display: none`) |
| | -05 | `ShellStore` (ícono por selección) | SHL, SHT | OK |
| | -06 | `BackButtonPolicy` | SMT (regla) + [eq] | EQ (NV sin módulo) |
| | -07 | `UiSurface`/`PanelScaleController`, `MetricGridView` | SHT, DSS, [eq] | OBS QA-G2-15 |
| | -08 | `PlaceholderScreenView` | SHT, CAP-21 | OK |
| F-APP-05 | -01 | tema sin variante | THM (estático) + [eq] | EQ |
| | -02 | `Base.uss` (`.rm-app`, `.rm-card`, `.rm-appbar`) | THM (píxeles), CAP-01 + SET | OK |
| | -03 | `FontAssetBuilder`, `Typography.uss`, `IconView` | FIT ×2, IconsTests, CAP-01 | OK |
| | -04 | `TabularNumber` | THM ×2 | OK |
| | -05 | `TextScaleSteps`, `Typography.uss`, `FitBox` | TextScaleSteps, TypographyUssSync, THM, CAP-24 | OBS QA-G2-02 |
| | -06 | `.rm-text--tertiary` | THM, CAP-08/10 + SET | OBS QA-G2-03 (clase de 11 dp) |
| | -07 | `RmButton`, `FocusHighlight` | THM ×2, [eq] | OK (R2-06 en equipo) |
| | -08 | `MotionSettings`, `DigitRoll`, `HeartbeatAnimator`, `TabTransition` | DigitRollTests, THM | OK |
| | -09 | `IDisplayMetrics`, `PanelScaleController` | THM (densidad) + [eq] | EQ |
| F-APP-06 | -01 | `MetricPalette`, `Base.uss`, `TabularNumber` | MetricPaletteTests, CAP-04/06 + SET | OBS QA-G2-04 |
| | -02 | tokens únicos | ColorSourceTests | OK |
| F-APP-08 | -01 | descarte | LegacyDiscardTests + QA | OK (QA: `metric_card`, `interval_tile`, `shared/widgets` en `lib/` y `test/`: 0 fuera de los dos archivos) |
| | -02 | `.rm-card` compartida | DSP, CAP-04 | OK |
| F-APP-19 | -05 | `ci-unity.yml` | ItTwoToolingTests (estático) + [CI] | EQ (NV) |
| F-BLE-05 | -01 | `DeviceScreenRules.Scan`, `ScanView` | DSR, DSP, CAP-08 | OBS QA-G2-03 (hint 11 dp) |
| | -02 | `ScanView` (vacío) | DSR, NBT, CAP-08/22 | OK |
| | -03 | = AC-HRM-13-08 | LT-29, LT-31 | OBS QA-G2-05 (b) |
| F-BLE-07 | -01 | `DeviceScreenRules.ShowDisconnectAction`, `AppBarView` | DSR, DSP, CAP-01/08 | OK |
| | -02 | `SimulatedRowerService.DisconnectAsync`, `DeviceStore` | SBS, DSS | OK |
| | -03 | ídem | LT-22, DSS | OK |
| | -04 | ídem | SBS | OK |
| F-BLE-08 | -01 | `SimulatedRowerService`, `DeviceScreenView` | DSF, DSS, VID-03 | OK |
| F-BLE-10 | -01 | `ShellStore` | SHT | OK |
| | -02 | = LT-31 | LT-31 | OBS QA-G2-05 (b) |
| F-BLE-11 | -01 | `DeviceScreenRules.ViewFor` | DSR, DSP | OK |
| | -02 | `DeviceHeaderView` | DSP, CAP-01 | OK |
| | -03 | `DeviceScreenRules.Tiles` | DSR, DSP, CAP-01/06 | OK |
| | -04 | ídem (PULSO solo FTMS > 0) | DSR, DSP, CAP-06 | OK |
| | -05 | `DeviceScreenRules.Grid`, `MetricGridView`, *scroller* superpuesto | DSR (360…1280), DSP, CAP-01/03/04/05 | OK |
| | -06 | `MetricTileView`, `FitBox` | DSP, CAP-07 | OBS QA-G2-09 (b) |
| | -07 | `ConnectedView` | DSP, LT-28 | OBS QA-G2-12 |
| | -08 | `Tiles` (sin calorías/tiempo/paladas) | DSR, DSP | OK |
| | -09 | `DeviceStore` (`RowingData.Empty` al conectar) | DST, DSS, VID-03 | OK |
| | -10 | `RowingFormats` + simulador sin ruido | DSF, DSS, CAP-01/19 | OK (valores recalculados en §6) |
| | -11 | `TabularNumber`, `DigitRoll` | DigitRollTests ×2, DSP, VID-01 | OK |
| F-BLE-13 | -01 | `SimulatorBuildGuard`, `BuildCli`, `build.ps1` | SMT ×3, ItTwoToolingTests, QA | OK |
| | -02 | `defineConstraints`, `#if` de Bootstrap | NBT, CAP-22, QA del build | EQ · QA-G2-17 |
| | -03 | `SimulatorBackend` | SBK | OK |
| | -04 | vistas sin conocimiento del modo | DSS (mismo árbol), AsmdefLint | OK |
| | -05 | `Unavailable*`, `HardwareBackend` | UNV, NBT, CAP-22 | OK |
| F-BLE-15 | -01 a -10 | `SimulatedRowerService` (+ `DeviceStore` en -10) | LT-20/21/22, SBS ×8, DSF, DSS, CAP-08, VID-03 | OK |
| F-BLE-16 | -01 | `SimulatorLayerView`, `SimulatorRibbon` | SOT, CAP-01 | OK (ver observación de orden) |
| | -02 | `SimulatorLayerView`, `SimulatorPanelLayout.FabBottom` | LT-26, SOT, CAP-01/18 | OK |
| | -03 | `SimulatorPanelLayout.MaxHeight` | SPS, LT-27 | OBS QA-G2-06 |
| | -04 a -11 | `SimulatorPanelStore`, `SimulatorPanelView` | SPS ×7, DSF, LT-26, SOT ×3, CAP-16/17/18/19 | OK |
| | -12 | ídem | LT-27, CAP-16 | OBS QA-G2-07 |
| F-HRM-04 | -01 a -05 | `SimulatedHeartRateService`, `HeartRateContact`, `HeartRateStore`, `HrmCardRules` | LT-23, LT-25, SHS ×2, HeartRateContactTests, HRS, HCR, DSF, DSS, LT-28 | OK |
| F-HRM-12 | -01 a -06 | `HeartRateStore` | LT-32, LT-33, LT-34, HRS ×5 | OK |
| F-HRM-13 | -01 | `DeviceScreenView` (una instancia que cambia de lugar) | LT-28, LT-30, DSP | OK |
| | -02 | `HrmCardRules`, `HeartRateCardView` | HCR, HCS, CAP-10..15 | OBS QA-G2-09 (c) |
| | -03 a -05 | `HrmCardRules.BodyFor`, `RowTitle`, botones | HCR ×3, HCS ×3, LT-31, CAP-10..15 | OK |
| | -06 | `HeartRateCardView` (conectado) | HCS, CAP-12 | OBS QA-G2-08 |
| | -07 | `TextClamp` | HCR, HCS, CAP-14 | OK |
| | -08 | `DeviceScreenRules.Scan`, `HrmCardRules` | DSR, HCR, LT-29, LT-31, CAP-09/15 | OBS QA-G2-05 (b) |
| | -09 | flujo completo | LT-28 (800 × 600) | OBS QA-G2-07, QA-G2-14 |
| | -10 | vista de búsqueda en horizontal | LT-30, CAP-09 | OBS QA-G2-07 |
| | -11 | modelo sin controles extra | HCR, HCS | OK |
| | -12 | `HeartbeatClock`, `HeartbeatAnimator` | HeartbeatClockTests ×2, DSS, VID-01 | OK |
| F-HRM-15 | -01 a -12 | `SimulatedHeartRateService` | LT-23/24/25/32, SHS ×7, SBK, DSF, DSS ×3, LT-26, CAP-13 | OK |
| It. 1 | AC-BLE-01-05 | `Tiles` + cultura del hilo | DSP, CAP-01/04/06 | OK |
| | AC-APP-09-04 / -05 | `LanguageState`, `AppContext.RefreshSystemSettings` | LOS ×2, LanguageStateTests, [eq] | OK |
| | AC-APP-09-06 | `SystemLocaleProvider` | LocaleResolverTests + [eq] | EQ · QA-G2-17 |
| | AC-APP-16-01 a -04 | bootstrapper (identidad) | AppIdentityTests, BVT, [eq] | EQ |
| | AC-BASE-01 | `ParitySmoke` | ParitySmokeTests, humo IL2CPP | EQ (NV sin IL2CPP) |
| | AC-BASE-05 | arnés CAP-23 | CAP-23 + SET | OK (QA ya lo midió en la configuración real, §1.2) |
| | AC-BASE-08 / -10 | `run-unity-tests.ps1`, bootstrapper v3 | ídem + BVT | OK |

---

## 4. Lo que queda sin test automatizado (lista del Architect)

**Coincido** con la lista de 02-tech-design §7.3 (AC-APP-01-07, -02-06, -05-01, -05-09, -09-06, -16-01 a -04, -19-05 y AC-BLE-13-02): son todos de equipo, de build o de CI, y cada uno tiene al menos una prueba de su parte de configuración o de regla. No encontré otros AC que necesiten estar en esa lista. Propongo cuatro automatizaciones baratas (QA-G2-17), que no cambian el AC:

| AC | Qué se puede automatizar |
|---|---|
| AC-BLE-13-02 | `build.ps1` (o `BuildCli`) verifica el build normal al terminar: ningún `RowMate.Simulator*.dll` en `RowMate_Data/Managed` y ninguna aparición de `Remo simulado` / `Pulsómetro simulado` en los binarios; `ItTwoToolingTests` controla que el paso existe. QA igual lo mira |
| AC-APP-02-06 | PlayMode: `Escape` en la raíz del shell llama a un `IBackButtonPolicy` falso una vez; en escritorio no lo llama |
| AC-APP-09-06 | `[u]` en Windows: `SystemLocaleProvider` devuelve la lista del usuario como etiquetas BCP-47, no vacía (corre en esta PC; se ignora en otras plataformas) |
| AC-APP-01-07 | En S-B1, leer el primer cuadro presentado del player (QA-G2-13) |

---

## 5. Los 15 casos legacy

Verifiqué cada nombre y línea contra los archivos de `test/`: los 15 de la spec §6 coinciden con la declaración `test(`/`testWidgets(` y con 02-tech-design §5.3.

| LT | Legacy | Port previsto | Gate |
|---|---|---|---|
| LT-20 | `simulated_ble_service_test.dart:13` | [s] `Start()` → `[Connected]`, `[On]`, 3 lecturas con pulso 0 | OK (anotar la semántica del grabador, §2) |
| LT-21 | `:39` | [s] 2 s `[Connected, Disconnected]` sin lecturas; 3 s `[…, Connected]` | OK |
| LT-22 | `:64` | [s] desconectar + 10 s; buscar → conectado | OK |
| LT-23 | `simulated_heart_rate_service_test.dart:10` | [s] buscar, conectar, 2 *ticks* + 3 s → 3 pulsos; sin timers | OK |
| LT-24 | `:49` | [s] 9 s buscando, 10 s desconectado | OK |
| LT-25 | `:62` | [s] caída: reintentando, recordado, pulso 0; 3 s vuelve; desconectar + 10 s | OK |
| LT-26 | `simulator_overlay_test.dart:17` | [ps] capa como último hijo de la raíz; mismas aserciones | OK · QA-G2-07. Diferencia aceptable: el legacy monta la capa sobre `Text('home')` sin `DeviceProvider` (el remo nunca se conecta y `Simular desconexión` no hace nada); el port monta la app, así que la caída sí ocurre. Ninguna aserción cambia |
| LT-27 | `:90` | [ps] P800, `sim-close` dentro de la ventana | OK · QA-G2-07 |
| LT-28 | `heart_rate_card_test.dart:83` | [ps] 800 × 600; compara con `hrm.CurrentBpm` (el legacy aclara que `sim.heartRate` puede ir un paso adelante) | OK · QA-G2-07, QA-G2-14 |
| LT-29 | `:121` | [ps] `device-search` habilitado → deshabilitado → habilitado | OK |
| LT-30 | `:142` | [ps] L640, `Audit()` vacío, fila dentro de `#hrm-card-slot-scan` | OK · QA-G2-07, QA-G2-14 (b) |
| LT-31 | `:161` | [ps] remo de prueba + pulsómetro **simulado** | **OBS QA-G2-05 (b)**: hoy no se puede armar |
| LT-32 | `heart_rate_provider_test.dart:54` | [s] con `StateRecorder`: el primer estado después de `ConnectAsync` trae el nombre elegido (D-04) | OK: el legacy afirma `connectingName` antes del *microtask*; grabar la secuencia conserva la intención |
| LT-33 | `:107` | [s] `ThrowingHeartRateService` | OK |
| LT-34 | `:122` | [s] incompatible sí, otro no | OK |

`test/widget_test.dart` es un *placeholder* vacío (`'App smoke test'`): no hay nada que portar.

---

## 6. Fidelidad con el legacy (muestreo: 23 casos)

| # | Qué | Legacy | Spec / diseño / notas | Resultado |
|---|---|---|---|---|
| 1 | Textos de la It. 2 (38 claves `nav*`, `device*`, `metric*`, `hrm*`, `*Title`) en es y en | `app_es.arb`, `app_en.arb` | spec §4 | **Iguales al carácter**, incluidos `Asegurate que el rower esté encendido\ny el Bluetooth activado`, `Heart for Bluetooth en Wear OS.`, `Workout` en es |
| 2 | Cuerpo de la tarjeta por estado y su prioridad | `heart_rate_card.dart:68-94` | AC-HRM-13-03; `HrmCardRules.BodyFor` | Igual (conectando + reintentando + recordado → recordado) |
| 3 | Botones habilitados | `:22-24`, `:108`, `:212`, `:218` | AC-HRM-13-04; §2.5 | Igual |
| 4 | Corazón pleno solo conectado, 40 % si no | `:39-40` | AC-HRM-13-02 | Igual |
| 5 | Nombre de la fila: plataforma → anunciado → `hrmUnknownSensor` | `:232-237` | AC-HRM-13-05 | Igual; el simulado tiene `platformName` vacío y `advName` `Pulsómetro simulado` |
| 6 | Color de `bpm` | `:180`, rosa @70 % | notas: blanco 70 % | **Difiere** (QA-G2-08) |
| 7 | Tamaño de la pista y las ayudas | `:103-104`, `device_screen.dart:414`, `:436`: 12 | diseño técnico: `caption` 11; notas: 12 | **Difiere en el diseño técnico** (QA-G2-03) |
| 8 | Caída del remo: 3 s, sin lecturas, la física sigue | `simulated_ble_service.dart:81-97`, `rowing_simulator.dart:45-54` | AC-BLE-15-06, -09 | Igual. 150 W → `paceForWatts` = 132,62 s → 3,770 m/s: 10 lecturas `37m`, 11 `41m` |
| 9 | Caída del pulsómetro: la vuelta se agenda antes de `_stop()` | `simulated_heart_rate_service.dart:107-116` | AC-HRM-15-08; §2.2 | Igual (por eso `disconnected` llega con `isRetrying`) |
| 10 | Búsqueda del pulsómetro: lista en el acto, vence a los 10 s | `:65-77` | AC-HRM-15-02 | Igual |
| 11 | Grilla: ancho de la **grilla** (ventana − 40) > 600 → 3 col. y 1,6; si no, 2 y 1,3; 12 de separación | `device_screen.dart:87-89`, `:261-276` | AC-BLE-11-05; `DeviceScreenRules.Grid` | Igual (640 → 2, 641 → 3) |
| 12 | Borde superior de la tarjeta | `device_screen.dart:300`: color @60 % | spec: "en el color"; notas: @60 % | Notas = legacy; spec imprecisa (QA-G2-09) |
| 13 | Presets | `simulator_overlay.dart:10-14`: 100/20, 180/24, 280/30 | AC-BLE-16-05 | Igual |
| 14 | Topes y pasos | `rowing_simulator.dart:20-26`; `simulator_overlay.dart:125-138` | AC-BLE-16-06 | Igual (±10 en 30-500, ±1 en 14-40) |
| 15 | Hora escena | `simulator_overlay.dart:74`, `:139-150`: `((h ?? now.hour) ± 1) % 24`, `${h.round() % 24}:00` | AC-BLE-16-07 | Igual (`DartMath.Mod`; `int?` equivalente) |
| 16 | Alto máximo del panel | `:83-95`: `H − 96 − top − 16`, mínimo 120, ancho 280 | AC-BLE-16-03 | Igual en la regla; ver QA-G2-06 en el insumo |
| 17 | Llave | `:40-49`: `right 16`, `bottom 96`, `FloatingActionButton.small` (40) | AC-BLE-16-02, D-12 | Equivalente (16 dp sobre una barra de 64) |
| 18 | Valores de referencia de la tabla de AC-BLE-11-10 | fórmula `500·(2,8/W)^(1/3)` | spec | Recalculados: 100 W 151,8 → `02:32`; 150 W `02:13`; 180 W 124,8 → `02:05`; 280 W 107,7 → `01:48`; 30 W 226,8 → `03:47`; 500 W 88,8 → `01:29` |
| 19 | Pulso a los 60 s | `rowing_simulator.dart:56-57`: objetivo 142,5, +10 %/s desde 70 | AC-HRM-15-09 | 142,5 − 72,5·0,9⁶⁰ = 142,37 → `142` (y 142 a los 58-61 s: el orden de los timers no lo cambia) |
| 20 | Barra de pestañas | M3 `NavigationBar`: 80 dp, etiquetas siempre (`navigation_bar.dart:1430-1432`) | AC-APP-02-03; diseño 64 (D-12) | Etiquetas iguales; alto distinto, aceptado |
| 21 | Cinta | `Banner` `topStart`: 80 × 12, centro (24, 24), texto **blanco** 10,2/900 | AC-BLE-16-01: texto oscuro | Geometría igual; color de texto cambiado sin marcar (QA-G2-09) |
| 22 | `deviceNoResults` | `device_screen.dart:431`: blanco 54 % | notas: 70 % | Difiere (QA-G2-11) |
| 23 | Espacio bajo la tarjeta del pulsómetro | `device_screen.dart:123`, `:135`: 32 + 16 | diseño: no está | Difiere (QA-G2-12) |

Huecos del spec: ninguno de comportamiento. Lo que la spec no trae del legacy de esta área (banner de error de búsqueda, lista de remos, diagnóstico oculto, vigilante de 5 s, Bluetooth apagado) está declarado fuera de alcance en §13 con su iteración.

---

## 7. Contradicciones spec ↔ diseño técnico ↔ notas visuales

| # | Entre | Qué | Issue |
|---|---|---|---|
| 1 | diseño ↔ notas | `letter-spacing` 0,96px vs 8px (confirmado por QA) | QA-G2-02 |
| 2 | diseño ↔ notas | orden de WP y dueño de `Tokens.cs` (DN-01) | QA-G2-03 |
| 3 | spec ↔ diseño | pistas y ayudas en 11 dp (`caption`) vs 12 dp | QA-G2-03 |
| 4 | diseño ↔ notas | color de pestaña en `Motion.uss` vs por código (DN-11); `display` vs vistas superpuestas (DN-12); 72 vs 64 dp (DN-19); *padding* en `.rm-screen` vs dentro del `ScrollView` (DN-23) | QA-G2-03 |
| 5 | spec ↔ notas | CAP-04 en una toma vs geometría | QA-G2-04 |
| 6 | spec ↔ notas | `bpm` en rosa vs blanco | QA-G2-08 |
| 7 | spec ↔ notas | VID-01 "en cada lectura" vs "solo si cambió" | QA-G2-10 |
| 8 | spec ↔ notas | título y etiqueta de PULSO en `metric.hr.hud` (`#FF5A7E`) en lugar de "color pulso" | Aceptable: AC-APP-06-01 deja la etiqueta al Designer "sin cambiar el matiz" y `#FF5A7E` es el mismo matiz (OKLCH). El PO lo confirma con QA-G2-18 |
| 9 | spec ↔ diseño | corazón con `--` conectado: AC-HRM-13-02 vs AC-HRM-13-12 | Resuelto por el Designer (pleno y quieto); falta el OK del PO (QA-G2-18) |

---

## 8. Paquetes de trabajo y contrato USS entre WP-B y WP-D

- **Disjuntos**: sí, con las excepciones declaradas en §6.0 (generados; hojas de WP-D creadas vacías por WP-B; `Tokens.cs` del Designer). Dos huecos: el paso de tokens antes de WP-A (QA-G2-03) y el `-Matrix` de WP-A sobre un script de WP-B (QA-G2-16).
- **Contrato USS**: el mecanismo es claro y verificable (orden de importación del tema, sin colores literales, nadie toca `font-size` en un `.rm-type-*`, tabla de propiedades que verifican los PlayMode). **No está cerrado** mientras la tabla de §3.12 y los árboles de §3 no incluyan lo que WP-D necesita: los elementos nuevos de DN-04/05/06/17/18, el alfa 0,6 del acento en reposo (DN-07), las clases de texto de 04-design-notes §2.3 y qué mecanismo manda en la opacidad del corazón (`.rm-hrm-card__heart--dim` en USS o estilo en línea por código, DN-13: no los dos). Con el anexo de QA-G2-03 queda cerrado.
- Riesgo de proceso: WP-B tiene ≈ 110 archivos y 8 pasos. Sugiero cortarlo en tres entregas con su "Hecho cuando" (B.1-B.3 plataforma y S-B1; B.4-B.6 UI; B.7-B.8 arnés y herramientas), para que QA pueda revisar a medio camino.

---

## 9. Viabilidad del arnés de capturas en batchmode

**Viable**, con evidencia en la configuración real (no solo en el *spike*):

| Pieza | Evidencia |
|---|---|
| `UIDocument` en modo edición dibujando en una RT dentro de `EditorApplication.update`, sin `-quit`, con URP 17.3 y D3D11 | P2: se leyó la RT en el cuadro 12 y la sonda salió con `EditorApplication.Exit(0)` |
| Tubería `GammaRt` = composición del legacy | P2: `#2A2F37` / `#BBC1C8` (esperado `#2A2F37` / `#BAC0C7` ± 1); `Overlay`: `#6A6A6C` / `#DADBDC` |
| `OverlayCopyPresenter` | P2: `CopyTexture` UNorm → sRGB, mismos bytes (en el Editor D3D11; en GLES 3.0 sigue siendo riesgo R2-02) |
| API del arnés y de los controles | P1: compilan `MediaEncoder`/`VideoTrackAttributes`, `DrawText(…, FontAsset)`, `MeasureTextSize`, `resolvedStyle.unityFontDefinition`, `SetScreenToPanelSpaceFunction`, `IPanel.Pick`, `NavigationSubmitEvent.GetPooled` con `target` |
| Escala en dp | S-0 #2 (no repetido) |
| MP4 en batchmode | S-0 #8 (no repetido; solo compilado en P1) |

Condiciones: el host de pruebas crea el `UIDocument` con `AddComponent` y recién después arma la UI (en P2, modo edición, la raíz existía en el cuadro 2), así que no debería tener el problema de QA-G2-01; `AppRoot`, que vive en la escena, sí lo tiene. Conviene que el primer test PlayMode de B.5 lo confirme. Y faltan las variantes de *backend* del host (QA-G2-05).

---

## 10. Hilos y reloj

- Todo corre en el hilo principal: los `…Async` del simulador completan en el acto, `HeartRateStore._run` no cambia de contexto, `FakeScheduler.Delay` completa en línea. Sin riesgo de carrera en la It. 2.
- `FakeScheduler` es determinista (vencimiento y orden de alta) y el pulso de 142 no depende del orden entre el timer del remo y el del pulsómetro (§6 #19).
- Un solo reloj para servicios y animaciones (`IClock` inyectado en `MotionDriver`): tests y videos deterministas. Las transiciones de USS usan el reloj real y por eso quedan fuera de las tomas (04-design-notes §7.1, DN-21): correcto.
- Entrega síncrona (D-04): el pulsómetro simulado emite `bpm 0` y después `disconnected`; con entrega síncrona la tarjeta recibe un estado intermedio (conectado, `--`) dentro del mismo cuadro. No se ve, y el modelo no reconstruye el cuerpo porque `HrmCardBody` sigue siendo `Connected`. Aceptable.
- Política de alcance del *scheduler* real: QA-G2-15.

---

## 11. UI Toolkit 6000.3: API que no existen

Revisé cada API que nombran el diseño y las notas. **Ninguna inexistente**: P1 compiló todas las que podían faltar. Las limitaciones reales están bien reconocidas: sin `line-height` (D-13), sin `gap`, `linear-gradient` ni `box-shadow` (notas §2.2), sin límite de líneas (`TextClamp`, D-21), sin `PanelSettings.textSettings`, `forceGammaRendering` es campo. La única trampa nueva es la unidad de `letter-spacing` (QA-G2-02).

---

## 12. Riesgos residuales y NO VERIFICABLE

| Qué | Estado |
|---|---|
| S-B1 en un player (presentador, clic, costo, primer cuadro) | NV hasta WP-B; P2 confirma solo la parte del Editor |
| Tab, Enter y Espacio en *runtime* con el Input System (R2-06) | NV hasta Device-Win |
| Android, IL2CPP (AC-BASE-01, AC-APP-16-04, Device-And) | NV sin módulos |
| `letter-spacing` en un player | NV (medido en el Editor) |
| Rutas largas: el submódulo de `com.gilzoide.sqlite-net` no se clona si el proyecto vive en una ruta profunda de Windows (`Filename too long`) | Observado en la sonda; no afecta al repo en `<repo>`. Tenerlo en cuenta si `ci-unity.yml` usa un *runner* de Windows o si alguien clona en una carpeta profunda |

---

## 13. Resumen por rol

| Para | Qué |
|---|---|
| **Architect** | Antes de WP-A: QA-G2-02 y el anexo de QA-G2-03 (con WP-D0). Antes de B.5: QA-G2-01, -05, -06, -07. Durante WP-B: QA-G2-12, -13, -14, -15, -16, -17, -19 y las dos observaciones sin número |
| **Designer** | WP-D0 (DN-01) coordinado con el Architect; QA-G2-08 y QA-G2-11 |
| **PO** | QA-G2-04 (CAP-04 en dos tomas) antes de las capturas; QA-G2-09, -10 y las confirmaciones de QA-G2-18 |
| **QA** | En la verificación: repetir P2 en el *player* (S-B1), inspeccionar el build normal (AC-BLE-13-02), Device-Win con Mono a 100 % y 150 % |

`git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó `lib/core/strava/strava_secrets.dart`. En el repo solo se escribió este archivo; las sondas corrieron en una copia en el scratchpad.
