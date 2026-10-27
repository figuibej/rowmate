# Gate de armonía del plan completo (QA)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-27 |
| Alcance | ROADMAP, FEATURE_INVENTORY + 8 inventarios de detalle, ARCHITECTURE, LEGACY_MAPPING, ADR 001-015, LEGACY_VISUAL_AUDIT, ART_BIBLE, ANIMATION_BIBLE, ASSET_SLOTS, propuestas P-01..P-10 del Architect y pedidos del Designer |
| Contrato | `rowmate-legacy/lib` y `test/` (solo lectura; `git status` del legacy limpio al terminar) |

## 1. Veredicto

**APROBADO CON OBSERVACIONES.** El plan es sólido: los inventarios son precisos (29 de 33 comportamientos muestreados coinciden exactamente con el código), la arquitectura soporta todas las funcionalidades y el orden del roadmap respeta las dependencias técnicas. No hay bloqueantes. Hay 14 observaciones mayores; cinco de ellas (QA-P-01 a QA-P-05) se tienen que resolver **dentro de la especificación y el diseño de la It. 1**.

**¿La It. 1 está lista para especificarse?** **Sí**, siempre que el spec del PO y el diseño del Architect incorporen QA-P-01 a QA-P-05 y la versión ampliada de P-01 (§4).

## 2. Cómo se verificó

- **Lectura del código legacy** contra cada número citado (archivo:línea en §3).
- **Oráculo Dart (nuevo, sin tocar el legacy).** En la máquina están Dart SDK 3.13.5 y Flutter (`<flutter-sdk>`). Un script en el scratchpad importó por ruta, en solo lectura, `routine.dart`, `interval_step.dart`, `workout_session.dart`, `rowing_data.dart` y `ftms_parser.dart`, y reprodujo resúmenes, etiquetas, P99, formatos y el paquete FTMS del inventario. El legacy no se modificó (`git status` limpio).
- **.NET 8 contra Dart** para formateo y redondeo (§3.2): evidencia concreta para ADR 012.
- **`git check-ignore`** del `.gitignore` actual con las rutas que proponen los docs (QA-P-01).
- **Versiones** (web): `com.gilzoide.sqlite-net` 1.3.3 existe (14/10/2026); Input System 1.20.1 existe (05/10/2026); `com.unity.vectorgraphics` **no** figura como paquete *released* ni *core* de 6000.3. El resto de las versiones queda como NO VERIFICABLE hasta el primer open en el Editor.

## 3. Muestreo de comportamientos (33)

### 3.1 Coinciden con el código (29)

| # | Comportamiento del inventario | Evidencia legacy |
|---|---|---|
| 1 | UUID FTMS `0x1826`, Rower Data `0x2AD1`, Control Point `0x2AD9` (DC-01) | `core/bluetooth/ftms_parser.dart:22-24` |
| 2 | Los 8 paquetes de ejemplo de F-BLE-02 (calculados a mano y con el oráculo: `24.0 298 482 133 150`) | `ftms_parser.dart:43-129` |
| 3 | Split `--:--` si ≤ 0, `MM:SS` con cero, `100:00`; tiempo `1:01:01` | `core/models/rowing_data.dart:24-40` |
| 4 | Simulador: pace `500·(2,80/W)^(1/3)`, kcal/s `(W·4·0,8604+300)/3600`, topes 30-500 W y 14-40 spm | `core/dev/rowing_simulator.dart:70-73`, `:140-145` |
| 5 | Presets Suave 100/20, Medio 180/24, Fuerte 280/30; ± 10 W | `core/dev/simulator_overlay.dart:11-13`, `:129-130` |
| 6 | P99 = índice `piso((n−1)·0,99)`; pulso promedio redondeado `[120,121] → 121` (oráculo) | `core/models/workout_session.dart:111-116`, `:133` |
| 7 | Progresiones solo si hay objetivo **y** progresión; repeticiones 0 → la serie desaparece | `core/models/routine.dart:29-70` |
| 8 | Resumen sobre la plantilla: `1 series · 1 descansos`, `1 pasos` (oráculo) | `routine.dart:100-105` |
| 9 | Etiquetas de duración `0s`, `1m 30s`, `60m`, `62m 5s`; objetivos con `📈` y `·` (oráculo) | `core/models/interval_step.dart:67-109` |
| 10 | Tick de 1 s, telemetría cada 5 ticks, último punto al terminar, P99 guardado como "promedio" | `features/workout/workout_provider.dart:147`, `:159`, `:255-305` |
| 11 | Fusión del pulso (DC-22): `_hrmBpm ?? d.heartRate`; vuelve a 0 ante estado ≠ connected solo si hubo pulso | `workout_provider.dart:220-237` |
| 12 | "Up next" con ≤ 15 s restantes y paso siguiente | `features/workout/immersive_workout_screen.dart:342-344` |
| 13 | Parciales: saltos negativos o > 10 m/s se descartan | `features/workout/series_tracker.dart:46`, `:123-125` |
| 14 | Objetivos: vatios ≥ mín., spm ≥ obj., split ≤ obj.; el vatio máximo nunca se evalúa (DC-36) | `features/workout/step_target_tracker.dart:54-59` |
| 15 | Remo: búsqueda 10 s, espera del adaptador 5 s, conexión 15 s, handshake `0x00` → 300 ms → `0x07`, keepalive 2 s | `core/bluetooth/ble_service.dart:65`, `:77`, `:144`, `:268-284` |
| 16 | Reconexión del remo: un solo intento a los 3 s (DC-02), porque `_cleanup` cancela el timer | `ble_service.dart:168-197`, `:315-319` |
| 17 | Watchdog ≥ 5 s con chequeo cada 1 s (de ahí "5-6 s") | `features/device/device_provider.dart:8`, `:71-79` |
| 18 | Pulsómetro: lectura vencida 10 s, 6 intentos cada 10 s al arrancar, 3 s tras caída, generación `_connectGen` | `core/bluetooth/heart_rate_service.dart:38-45`, `:69` |
| 19 | Cámara: 2,2 m, remero a 9,5 m, recorte 0,5 m, horizonte 0,40/0,42, focal = lado corto | `features/workout/scene/scene_camera.dart:8-19` |
| 20 | SceneState: τ 1,5 s, charcos 4 s, idle 12 spm, dt ≤ 0,1 s, exige spm > 0, charco izquierdo primero, cabeceo `0,02·sin(2πp)+0,006·sin(0,8t)` | `scene/scene_state.dart:16-23`, `:52`, `:68-85` |
| 21 | Hora: noche si elevación ≤ −0,4, luna con `h+12`, fuerza 0,45 de noche y `clamp(1,2·e; 0,2; 1)` de día, 8 keyframes cíclicos | `scene/time_of_day.dart:52-67`, `:102-105` |
| 22 | Orillas: semilla `id·1000003 + k·7919 + 17`, segmentos de 50 m, orilla en 9 m, rango 400 m, topes 120/60/200 | `scene/environment.dart:43-50`, `:77`; `environments/coast_environment.dart:25`; `regatta_environment.dart:20` |
| 23 | Hitos: muelle `k%8==3`, cabaña `k%12==7` en x = 13, puente `k%16==5`, playa `k%5==2`, faro `k%20==10`, boyas cada 10 m en ±3/±9, cartel cada 250 m en −10,5, tribuna y torre cada 1000 m | `lake_environment.dart:35-37`; `river_environment.dart:41`; `coast_environment.dart:31`, `:46`; `regatta_environment.dart:27-38` |
| 24 | 80 estrellas con `Random(42)`, titileo `0,5+0,5·sin(2t+i)`; nubes `0,0004·dist + 0,004·t` | `scene/painters/sky_painter.dart:59-67`, `:95` |
| 25 | Remo de 2,9 m afuera + 0,9 m adentro (3,8 m) y semimanga 0,28 m: la premisa de D-ART-04 es correcta | `scene/painters/boat_painter.dart:28-30`, `:135` |
| 26 | 6 claves de preferencias y 5 de almacenamiento seguro, con los mismos nombres que ARCHITECTURE §9.4-9.5 | `heart_rate_service.dart:34-35`; `scene_settings.dart:11`; `series_panels.dart:120-121`; `profile_provider.dart:45`; `strava_auth_service.dart:16-20` |
| 27 | Base v7, 4 tablas, cascada declarada pero apagada; `rower_app.db` / `rower_app_dev.db` | `core/database/database_service.dart:11-12`, `:77-136` |
| 28 | Strava: `external_id = rowmate_<id>`, renovación a `expires_at − 60` (939 no, 940 sí), configurado si el secreto no está vacío y no es `TU_CLIENT_SECRET`, `rowmate://rowmate.app/callback` | `strava_api_service.dart:106`; `strava_auth_service.dart:130`; `strava_config.dart:9-21` |
| 29 | Catálogo: 159 claves es = 159 en, mismos placeholders, sin plurales ICU, 7 metadatos `@`; `en` primero (respaldo). Paleta de métricas y pasos, wakelock en `main.dart:42`, bundle iOS `com.iguisoft.rowmate` | `l10n/app_*.arb`; `l10n/app_localizations.dart:96-98`; `shared/theme.dart:32-68`; `ios/Runner.xcodeproj/project.pbxproj:485` |

También se verificaron números del Designer: vidrio `#070D16 @86 %` sobre blanco = `#2A2F37` (L = 0,028); `#FF5A7E` da 4,50:1 y el terciario `@54 %` sobre card da 5,15:1. Coinciden con ART_BIBLE §2.

### 3.2 No coinciden o son inexactos (4)

| # | Doc | Dice | El código hace |
|---|---|---|---|
| E1 | LEGACY_MAPPING.md:110 | "RNG de nubes con `DartRandom(42)`" | `Random(42)` es de las **estrellas**; las nubes son datos fijos (`sky_painter.dart:29`, `:59`) |
| E2 | LEGACY_MAPPING.md:192 y ROADMAP.md:207 | `scene_view_test` = "escenarios × estados de remo", 16 casos en la It. 6 | Son 4 escenarios × 2 tamaños × 2 horas (`scene_view_test.dart:8-10`). En la It. 6 solo pueden correr los 4 del lago; los 12 restantes son de la It. 8 |
| E3 | ARCHITECTURE.md:457 | Fechas `…ss.fffZ` "como `toUtc().toIso8601String()`" | Dart emite microsegundos (oráculo: `2026-10-27T09:37:38.628537Z`). No afecta a una base nueva, pero sí a cualquier lectura de datos legacy |
| E4 | FEATURE_INVENTORY.md:35 | "2 062 líneas de tests en 26 archivos" | 2 129 líneas (26 archivos sin `widget_test`); los 145 casos sí coinciden |

**Formateo Dart vs .NET 8** (base de QA-P-02):

| Valor | Dart `toStringAsFixed` / `round` | .NET `ToString("F")` | `Math.Round(AwayFromZero)` |
|---|---|---|---|
| 0,15 → 1 dec. | `0.1` | `0.1` | `0.2` ✗ |
| 0,25 → 1 dec. | `0.3` | `0.2` ✗ | `0.3` |
| 0,35 → 1 dec. | `0.3` | `0.3` | `0.4` ✗ |
| 24,45 → 1 dec. | `24.4` | `24.4` | `24.5` ✗ |
| 2,5 → 0 dec. | `3` | `2` ✗ | `3` |
| 0,5 → 0 dec. | `1` | `0` ✗ | `1` |
| −0,05 → `round()` | `0` (int) | — | `-0` si se formatea un double ✗ |

Ninguna de las dos API de .NET reproduce a Dart. Dart redondea el valor binario **exacto** y desempata alejándose del cero.

## 4. Propuestas del Architect

| ID | Recomendación QA | Motivo / condición |
|---|---|---|
| P-01 | **Adoptar y ampliar** | Es necesaria: `scan_filter_test` compara `Guid('180d')` con el UUID de 128 bits (necesita `BleUuid`), F-APP-09/10 necesitan `Localizer` y F-BLE-01 necesita `Fmt`. Además hay que sumar: `BleScanResult`, `IRandom` (`rowing_simulator_test` usa el RNG por defecto), `ILogger`, el shim `IsExternalInit`, `RepoPaths`, `tools/oracle` (QA-P-23) y el asmdef de soporte de tests (QA-P-03) |
| P-02 | **Adoptar** | Ya está en ARCHITECTURE §9.2 y ADR 008. El ROADMAP.md:145 dice "equivalente a v7": hay que actualizarlo. No tiene costo visible y es necesario para Q-71 |
| P-03 | **Adoptar** | 26 IDs y esfuerzo alto. Corte sugerido: 4a = WKT-01..06, 09..12, 20, BLE-10, APP-14 (HUD funcional con el respaldo); 4b = WKT-13..17, 19, 21 y F-APP-15 |
| P-04 | **Adoptar** | Falta definir cuál es el respaldo (QA-P-17) |
| P-05 | **Adoptar con ajuste** | Pasar el **agua degradada** (R-04, port barato del respaldo legacy) a la 6a, para que la 6a tenga una escena completa y usable. La 6b queda para el agua avanzada |
| P-06 | **Adoptar** | Junta todo lo de Apple (R-07, Q-80, wakelock de macOS) en una sola vuelta con Mac |
| P-07 | **Adoptar (opcional)** | Se puede hacer **sin Unity**: VS 2022 Community con herramientas C++ está instalado. Alcanza con compilar `RowMateBle.dll` y manejarla con P/Invoke desde una consola o un test de dotnet contra el pulsómetro real (QA-P-24) |
| P-08 | **Adoptar, condicionada** | Depende de que el usuario active la licencia Personal y cargue el `.ulf`. Mientras tanto, `ci-core.yml` |
| P-09 | **Adoptar** | — |
| P-10 | **Adoptar** | Ya figura en ARCHITECTURE §1.1 y RA-12 |
| Aclaración DartRandom | **Aprobada** | Es factible y verificable ya mismo con el oráculo: `Random(42)` → `0.15092545597797424`, `0.6041479725361217`, `nextInt(1000) = 77`; semilla del lago con k = 7 → `0.854835531354132`. Hay que guardarlos como vectores de test |
| Aclaración Q-11 | **De acuerdo** | Se diseña y no se implementa. El id iOS `com.iguisoft.rowmate` coincide con el legacy; el de Android (`com.example.rower_app`) no |
| Aclaración Q-85 | **De acuerdo** | Ids provisionales `com.iguisoft.rowmate` |

## 5. Pedidos del Designer al Architect (CÓMO ↔ ARTE)

| Pedido | ¿Lo cubre la arquitectura? | Comentario |
|---|---|---|
| UI Shader Graph (6.3+) y blur en `_RmHudBlur` | Sí (6000.3.27f1). Nombre y pasadas distintos | QA-P-16 |
| Tokens únicos en `Core/Design/Tokens.cs`, generador USS y test WCAG | **No** (hoy los colores están repartidos en 3 lugares) | QA-P-05 |
| `VisualStrokeClock` sin tocar `SceneState` | Sí (compatible con `StrokeTiming`) | Unificar nombres en el diseño de la It. 7 |
| D-ANI-02 / D-ART-04 (medidas reales, charcos en `BladeExit`) | Compatible | Quedan dos sistemas de charcos: los tests de `PuddleField` tienen que cubrir las reglas legacy (2 por palada, izquierdo primero, 4 s, fijos en el mundo, nada en pausa); ANIMATION_BIBLE §7.5 ya lo prevé |
| D-ANI-03, D-ART-05, `CelestialPlacement`, `HourLighting`, `MoonPhase` | Sí (Core/Scene) | Compatible con `water_painter_test` (`uSunX` centrado al mediodía, acimut 0) |
| Agua con origen flotante, fases en `double`, textura de interacción, respaldo | Sí (ARCHITECTURE §14) | — |
| Asset slots genéricos, `SlotIds`, `Assets/ThirdParty` ignorado | **Parcial** (otro modelo de slots y otras rutas; `.gitignore` sin la carpeta) | QA-P-01, QA-P-15 |
| Mixamo Humanoid sin *Optimize Game Objects*; Animation Rigging, Shader Graph y Vector Graphics | AR y SG sí; **Vector Graphics no** | QA-P-09. AR necesita un `Animator` con controlador (pose de agarre) y avatar construido en runtime para el maniquí (QA-P-29) |
| Niveles Alto/Medio/Bajo y adaptación térmica | **No** (2 assets Mobile/Desktop, tier fijo al arrancar) | QA-P-10 |
| `TabularNumber` y PanelSettings a 160 DPI | DPI sí; `TabularNumber` no está y choca con el tope de 60 elementos del HUD | QA-P-08 |
| Reducir movimiento nativo | **No** (no hay puerto) | QA-P-21 |
| D-ANI-06 (contador de paladas) | Sí (`RowingData.strokeCount`) | Se verifica en la It. 10 con el AMS-670B |
| Decisiones para el gate del PO | No están agendadas en el ROADMAP | QA-P-13 |

**Presupuesto móvil.** Los números del nivel Medio (≤ 150 draw calls, ≤ 350 k triángulos, agua ≤ 70 ALU, blur ≤ 0,6 ms, AR ≤ 0,3 ms) son verosímiles para un Galaxy A54 / Pixel 6a, pero **NO VERIFICABLES** sin un dispositivo. El nivel Bajo (Mali-G52, vegetación con overdraw 2,5×) es el de mayor riesgo. El ROADMAP no pide [Device] en las It. 6-8 (QA-P-12).

## 6. Tabla de issues

Severidad: `bloqueante` / `mayor` / `menor`. "It." = iteración en la que hay que resolverlo.

| ID | Sev. | Resp. | It. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|---|
| QA-P-01 | mayor | architect | 1 | El `.gitignore` versionaría el secreto generado de Strava (R-02) y los assets de terceros | `.gitignore:34` ignora `**/StravaSecrets.cs`, pero ARCH genera `StravaSecrets.g.cs`. `git check-ignore`: `StravaSecrets.g.cs`, `Assets/ThirdParty/Mixamo/YBot.fbx` y `Assets/RowMate/Art/Models/ThirdParty/Mixamo/YBot.fbx` quedan **versionados**. ASSET_SLOTS.md:17 asume que `Assets/ThirdParty/` está ignorado | Agregar `**/StravaSecrets*.cs` y `/Assets/ThirdParty/**` con `!/Assets/ThirdParty/**/*.meta` (conserva los GUID que referencian los slots). Una sola ruta para terceros (QA-P-15). Test de CI que falle si se versiona `StravaSecrets*` |
| QA-P-02 | mayor | architect | 1 | `Fmt.Fixed` "con aritmética entera propia" y `DartMath.Round` no aseguran la paridad con Dart | Tabla §3.2: ni `ToString("F")` ni `Math.Round(AwayFromZero)` reproducen `toStringAsFixed` (0,25 / 0,35 / 2,5 / 24,45). Afecta lo visible (spm con 1 decimal) y lo calculado (`rowing_simulator.dart:123` usa `toStringAsFixed(1)` para el valor emitido) | Especificar en ADR 012: expansión decimal **exacta** del double (mantisa·2^exp con `System.Numerics.BigInteger`, compatible con IL2CPP), desempate alejándose del cero y `-0` como Dart. `DartMath.Round` devuelve `long`. Vectores dorados generados con el oráculo Dart (QA-P-23) que corran en dotnet y en Unity |
| QA-P-03 | mayor | architect | 1 | El layout de asmdefs de test no compila en Unity | ARCHITECTURE.md:202 y :208: `Tests/Support/` es hermana de `Tests/EditMode/`, así que un asmdef en `Tests/EditMode` no la incluye. `RowMate.Tests.Simulator` (:203) no referencia los fakes que necesita `SimulatedRowerServiceTests` (`FakeScheduler`). En dotnet no se ve porque un solo csproj compila todo | Crear un asmdef `RowMate.Tests.Support` (constraint `UNITY_INCLUDE_TESTS`, sin API de Unity) referenciado por EditMode, Simulator y PlayMode. EditMode con `includePlatforms: ["Editor"]`. Revisión estática de QA en la It. 1 |
| QA-P-04 | mayor | po | 1 | Textos esperados de la It. 1 ambiguos: Q-02 (corregir plurales) contra la tabla legacy de F-RUT-05 (`1 pasos`, `1 series · 0 descansos`), y faltan los textos en inglés de las claves nuevas (resumen y etiquetas, Q-01) | `inventory/routines-models.md:252-259`; FEATURE_INVENTORY.md:365-366; ROADMAP It. 1 casos 9-11 | El `01-po-spec` de la It. 1 fija la tabla exacta es/en de cada clave nueva (con plural ICU si se aplica Q-02) y lista qué textos `.arb` cambian al convertirse (por ejemplo `workoutStartTitle` "Iniciar entrenamiento?") |
| QA-P-05 | mayor | architect / designer | 1 | No hay una fuente única de colores y tokens | LEGACY_MAPPING `shared/theme.dart` → `StepTypeInfo`/`MetricPalette` (Core/Models) + `Tokens.uss` + `ThemeColors` (C#); ART_BIBLE §2 y §11 → `Core/Design/Tokens.cs` con generador. F-RUT-01 (colores de paso) se cierra en la It. 1 | `Core/Design/Tokens.cs` como única fuente (hex base del legacy y variantes `*.hud`). `StepTypeInfo` y `MetricPalette` lo consumen. Generador dotnet `tools/tokens` → `Tokens.uss` con un test que detecte desincronización. Test de contraste WCAG en dotnet |
| QA-P-06 | mayor | architect / po | 4 (diseño), 10 (verificación) | La premisa de segundo plano es falsa. ARCH dice que Unity pausado es "igual que los timers de Dart suspendidos", pero en Android Dart sigue corriendo en segundo plano, y en iOS con `bluetooth-central` corre mientras llegan notificaciones. En Unity se cortaría el keepalive `0x07` cada 2 s (el monitor sale del modo BT) y se frenaría el reloj activo | ARCHITECTURE.md:367, :594, RA-08; `ble_service.dart:278-284`; `workout_provider.dart:147` | El PO define qué pasa con la sesión en segundo plano (paridad: el reloj sigue). El Architect mueve el keepalive FTMS al plugin nativo mientras la app no está en primer plano y define el reloj con alcance por tiempo monótono. QA mide el legacy en un dispositivo como referencia (It. 10) |
| QA-P-07 | mayor | architect / designer | 2 | 30 fps fuera del inmersivo contra animaciones "super suaves" a 60 fps (pestañas, rodillos, corazón que late) | ARCHITECTURE.md:655 (`renderFrameInterval = 2`); ANIMATION_BIBLE.md:22 | 60 fps mientras haya una transición, un rodillo o el corazón visible (Dispositivo conectado). 30 fps solo en reposo sin animaciones. Se mide en dispositivo |
| QA-P-08 | mayor | architect / designer | 2, 4 | `TabularNumber` con celdas por dígito y rodillo (2 glifos por dígito durante la animación) supera el tope de ≤ 60 elementos visibles del HUD si se arma con `Label` | ARCHITECTURE §20 ("HUD ≤ 60 elementos"); ART_BIBLE §4.2; ANIMATION_BIBLE §2.4. El HUD tiene unas 9 cifras y paneles (≈ 40-80 celdas) | `TabularNumber` como **un** `VisualElement` que dibuja los glifos con `generateVisualContent` (texto en `MeshGenerationContext`) y anima por transformaciones. Test PlayMode de cantidad de elementos y de 0 B/frame |
| QA-P-09 | mayor | architect / designer | 2 | Íconos SVG con `com.unity.vectorgraphics`, que no figura como paquete *released* ni *core* en 6000.3 y tampoco está en el manifest | ASSET_SLOTS.md:250; docs de Unity 6000.3 (`pack-safe`, `pack-core`); ARCHITECTURE §1.2 | Íconos como glifos de la fuente Material Symbols (ya está como fallback de texto) o PNG/SDF a 2×. Si se insiste con SVG, aprobación explícita como paquete no *released*, con versión exacta |
| QA-P-10 | mayor | architect / designer | 6 (y bootstrapper de la It. 1) | Dos presupuestos y dos esquemas de calidad distintos. La adaptación térmica no tiene diseño | ARCH §20 contra ART §9: GPU 14 vs 12 ms, triángulos 300 k vs 350 k, SetPass 40 vs 50, sombras de 2 cascadas a 60 m vs 1 cascada a 40 m, texturas 200 vs 180 MB. Niveles "Mobile/Desktop" (ARCHITECTURE.md:723) contra Alto/Medio/Bajo. La resolución dinámica con `ScalableBufferManager` no funciona en GLES3/DX11, y el tiempo de GPU de `FrameTimingManager` no está en todos los Android | Una sola tabla en ARCH §20, que ART referencia. 3 niveles como ScriptableObject. Resolución por `renderScale` con cambios poco frecuentes. Si no hay tiempo de GPU, usar el tiempo de CPU del cuadro. El bootstrapper de la It. 1 no fija los niveles definitivos |
| QA-P-11 | mayor | architect / designer / po | 8 | Miniaturas: horneadas en el Editor (ARCH) contra render en tiempo de ejecución "a la hora actual" (ART y ASSET_SLOTS). La segunda opción obliga a cargar 4 escenas aditivas en Workout y cambia F-SCN-22 (legacy: 12:00) sin marca [DECIDIR] | ARCHITECTURE.md:667; ART_BIBLE.md:455; ASSET_SLOTS.md:121 | Elegir una. Recomendado: horneadas a 6, 12, 18 y 22 h y mostrar la más cercana (barato, sin cargar escenas). El cambio de hora lo aprueba el PO |
| QA-P-12 | mayor | po | 2, 6-8 | Faltan prerrequisitos y verificaciones: no se piden las fuentes y los íconos (It. 2), ni el Terrain Sample Asset Pack de 1,6 GB (It. 6), ni un Android de gama media. Las It. 6-8 no piden [Device], aunque R-12 y ART §11 exigen medir en el dispositivo de referencia | ROADMAP.md:36 (tabla de prerrequisitos), :211, :233, :255; ASSET_SLOTS §0.1 | Agregar esos prerrequisitos y [Device] (Android de gama media) a las It. 6-8, o declarar explícitamente el rendimiento como NO VERIFICABLE hasta la It. 14 |
| QA-P-13 | mayor | po | 2, 4, 6, 7 | Las decisiones del Designer (D-ART-01..09, D-ANI-01..07) no tienen gate en el ROADMAP. R-10 (≥ 7:1) choca con D-ART-01 (4,5:1 para la familia roja). El demo de la It. 4 usa `#FF4444` y el Designer propone `#FF5F59` + flecha | ROADMAP.md:165; FEATURE_INVENTORY R-10; ART_BIBLE §0.2; ANIMATION_BIBLE §0.2 | Tabla decisión → gate en el ROADMAP: D-ART-03 (It. 2); D-ART-01/02/09 y D-ANI-05 (It. 4); D-ART-05/06/08 (It. 6); D-ART-04 y D-ANI-01..04 (It. 7); D-ANI-06 (It. 10); D-ART-07 y D-ANI-07 al backlog. Actualizar R-10 según lo que se decida en D-ART-01 |
| QA-P-15 | mayor | architect / designer | 1 | Hay dos modelos de slots y rutas que no coinciden, justo cuando la It. 1 escribe el árbol | Slots tipados (ARCH §14, ADR 015) contra `AssetSlotDefinition` genérico + `SlotIds` (ASSET_SLOTS §0). Mixamo en `Art/Models/ThirdParty` (ARCHITECTURE.md:181) contra `Assets/ThirdParty` (ASSET_SLOTS.md:17). Fuentes en `Art/Fonts` contra `UI/Fonts`. Shaders en `Art/Shaders` contra `Presentation/Scene/Shaders` (ART_BIBLE.md:626). Slots en `Scene/Slots` contra `RowMate/Slots` | Un solo árbol en ARCH §2, que los docs de diseño referencian. Base genérica `AssetSlotDefinition` (validación, placeholder, presupuesto) con subtipos tipados y `SlotIds` en Core |
| QA-P-14 | menor | architect | 6 | P-05 deja la 6a sin agua | Propuesta P-05 | Agua degradada (R-04) en la 6a; avanzada, reflejos y estela en la 6b |
| QA-P-16 | menor | architect / designer | 6 | El blur tiene nombre y pasadas distintos en cada doc. Además, la textura de Render Graph tiene que sobrevivir hasta la UI en *Screen Space Overlay* | `_RowMateBackdrop` y 2 pasadas (ARCHITECTURE.md:409) contra `_RmHudBlur` y 3 pasadas (ART_BIBLE.md:164, :572) | Un nombre y un número de pasadas. `RTHandle` persistente (no transitorio de RG) + `SetGlobalTexture`. Prototipo en la It. 6 |
| QA-P-17 | menor | designer / po | 4 | No está claro qué respaldo del vidrio usa la It. 4 | P-04 dice "el look del legacy" (`#000000 @55 %`, audit §3.5); el respaldo del Designer es `#070D16 @80 %` (ART_BIBLE.md:169) | Usar el del Designer, porque es el que garantiza el contraste, y que el PO lo apruebe |
| QA-P-18 | menor | po / architect | 1 (bootstrapper), 14 | minSdk 26 contra un nivel Bajo pensado para "Android 7+" | ARCHITECTURE.md:722; ART_BIBLE.md:561; DC-59 | El PO confirma que se pierde Android 7. Ajustar la referencia del nivel Bajo |
| QA-P-19 | menor | architect / po | 1 | Errores de documentación | E1-E4 (§3.2) | Corregir LEGACY_MAPPING:110 y :192, ROADMAP:207 (repartir 4/12 entre las It. 6 y 8), ARCH:457 y FEATURE_INVENTORY:35 |
| QA-P-20 | menor | architect | 1-2 | Idioma: Flutter elige el primer idioma soportado de la **lista** de preferencias del sistema; `Application.systemLanguage` da solo el principal | `main.dart:103-105`; ARCH §12 | Leer la lista nativa de preferencias o aceptar y documentar la diferencia (F-APP-09) |
| QA-P-21 | menor | architect | 2 | No hay puerto para "reducir movimiento" | ART_BIBLE §10.4; ANIMATION_BIBLE §0.1 | `IAccessibilitySettings`: Android por JNI desde C# (`ANIMATOR_DURATION_SCALE`), iOS en Swift, Windows con `SPI_GETCLIENTAREAANIMATION`, macOS en la 10b. Por defecto "no reducir" |
| QA-P-22 | menor | designer | 2 | La barra de pestañas "+ blur" no se puede implementar: el blur de la escena no está detrás de la UI y UI Toolkit no desenfoca lo que queda debajo | ART_BIBLE.md:68 | Vidrio por translucidez `@88-94 %` + borde, sin blur |
| QA-P-23 | menor | architect | 1 | El oráculo Dart no se aprovecha: ADR 012 deja la captura de vectores para la It. 6 y no hay herramienta para hacerla | Demostrado: el oráculo funciona en solo lectura sobre el legacy (§2) | `tools/oracle/*.dart` que importan los archivos puros del legacy por ruta y generan JSON dorado (Fmt, DartRandom, etiquetas, P99 y, más adelante, TCX y metadatos). Los tests de dotnet los leen |
| QA-P-24 | menor | architect | 2-3 | P-07 puede hacerse sin Unity | `vswhere`: VS Community 2022 17.14 con VC Tools | Harness dotnet con P/Invoke a `RowMateBle.dll` contra el pulsómetro real |
| QA-P-25 | menor | architect | 1 | Lo dependiente de Unity en la It. 1 (asmdefs, manifest, bootstrapper) no se puede verificar | Sin Editor (RA-01, RA-13). Versiones verificadas: sqlite-net 1.3.3 e Input System 1.20.1; el resto no | Bootstrapper mínimo en la It. 1 y completo en la It. 2. Revisión estática: nombres de asmdef, referencias por nombre, JSON válido. Marcar como NO VERIFICABLE |
| QA-P-26 | menor | architect | 3 | En dotnet no hay `UnitySynchronizationContext`: las continuaciones del `SerialExecutor` correrían en el *thread pool* y los tests podrían ser no deterministas | ADR 005; ARCH §6 | `SynchronizationContext` de test (o ejecutor en línea) en `Tests/Support` |
| QA-P-27 | menor | po / architect | 2, 10 | `heart_rate_card_test` :121/:142/:161: el ROADMAP los porta en la It. 2 y LEGACY_MAPPING dice que "se cierran en la It. 10" | ROADMAP.md:108; LEGACY_MAPPING.md:179 | Portarlos en la It. 2 (usan servicios simulados o controlables) y volver a correrlos en la It. 10 |
| QA-P-28 | menor | architect | 6 | `scene_state_test:55` (`shaderDistance` mód. 4096) contra Q-68 (sin salto) | `scene_state.dart:34` | Portar la función como contrato y documentar que el agua nueva no la usa |
| QA-P-29 | menor | architect / designer | 7 | Animation Rigging trae Burst y Mathematics como dependencias, aunque ARCH los da por descartados. Las clases del rig figuran en Presentation (ANIMATION_BIBLE §7.1) y no en `RowMate.Scene`. Hay nombres duplicados (`BoatKinematics`/`ScullingKinematics`, `OarController`/`OarRig`, `PuddleDecals`/`PuddleField`) | ARCH §1.2; ANIMATION_BIBLE.md:465; LEGACY_MAPPING.md:113 | Aclarar en ARCH §1.2. Unificar nombres y assembly en el diseño de la It. 7. Animator con controlador de pose y `AvatarBuilder` para el maniquí |
| QA-P-30 | menor | po | 1 | `CLAUDE.md` remite a `docs/migration/STATUS.md`, que no existe | `CLAUDE.md` §Reglas | Crearlo al cerrar el gate (estado: It. 1 por especificar) |

## 7. Matriz de cobertura (resumen)

| Área | IDs (canón. + alias) | Iteraciones dueñas | Archivos legacy mapeados | Casos de test legacy → It. | Huecos sin test legacy (criterios nuevos) |
|---|---|---|---|---|---|
| BLE + SIM | 16 + 0 | 1, 2, 4, 10 | 11/11 | 14: scan_filter 2, rowing_simulator 7 (It. 1); simulated_ble 3, simulator_overlay 2 (It. 2) | FTMS (8 paquetes), máquina de estados del remo, watchdog |
| HRM | 15 + 1 | 1, 2, 11 | 6/6 | 23: hr_parser 6 (1); simulated_hr 3, hr_card 4, hr_provider 3 (2); reconnect_loop 7 (11) | Secuencia real de conexión (HRM-03.4..13) |
| RUT | 18 + 2 | 1, 3, 9 | 6/6 | 4: routine_positions 1, session_stats 3 (It. 1) | RUT §5.3 casos 1-21 (con el oráculo: 1, 9, 14 y parte de 11 y 15; por lectura de código: 7 y 12) |
| WKT + HUD | 22 + 1 | 4, 5, 12 | 7/7 (`workout_screen` 203-990 descartado) | 27: workout_provider_hr 4, step_target 6, step_pill 1, series_panels 1 (It. 4); series_tracker 12, series_panels 3 (It. 5) | Fases, reloj, avance, telemetría, semáforo, formatos |
| SCN | 23 + 0 | 6, 7, 8 | 20/20 + `water.frag` | 77: It. 6 (camera 4, state 3, time 6, env, sky/shore, water 1, view 4*); It. 7 (state 4, stroke 4, boat 3); It. 8 (env, sky/shore, water 1, picker 2, view 12*) | Capturas de aceptación visual |
| HIS + DB | 15 + 8 | 3, 4, 9, 12, 13 | 4/4 + `database_service` repartido | 0 | Formatos, consultas, gráfico, reparación |
| STR + PRF | 18 + 0 | 12, 13 | 9/9 (`strava_secrets.dart` no se lee) | 0 | TCX, metadatos, vencimiento, duplicados, importación |
| APP | 15 + 4 | 1, 2, 3, 4, 10, 12, 14 | `main`, `theme`, l10n, widgets muertos y config de plataformas | 0 (`widget_test` vacío) | Catálogo, permisos, identidad, CI |
| **Total** | **142 + 16 = 158** | 14 (17 con P-03/05/06) | 69 `lib` + shader + 27 `test` | **145 casos**, todos asignados | — |

(*) Corrección E2: `scene_view_test` se reparte 4/12 entre las It. 6 y 8.

**Cobertura de archivos:** todos los archivos de `lib/`, `shaders/` y `test/` aparecen en inventario, mapping y roadmap. Las únicas excepciones son el código generado de l10n y los dos widgets muertos, que el roadmap no nombra (se cierran por F-APP-08: aceptable). Las 5 specs legacy están citadas. `step-timeline-states` solo la cita la auditoría visual, pero F-WKT-09/15 la cubren. La configuración de plataformas, el CI, `codemagic.yaml`, el ícono y la licencia MIT están en `app-shell-platform.md`.

## 8. Riesgos (estado de mitigación)

| Riesgo | Estado | Nota |
|---|---|---|
| BLE nativo en 3 pilas | Mitigado en diseño (IBleAdapter + Fake + lógica en Core) | Falta lo de segundo plano y keepalive (QA-P-06). Spike de Windows sin Unity posible (QA-P-24) |
| IL2CPP / AOT | Mitigado (`link.xml`, `JToken`, callbacks estáticos) | `BigInteger` para `Fmt` es compatible con AOT |
| SQLite | Mitigado (versión 1.3.3 verificada) | Determinismo de los tests asíncronos (QA-P-26) |
| OAuth en 4 plataformas | Mitigado (ASWebAuth, deep link, loopback) | Se verifica en dispositivo en la It. 12 |
| Agua y rendimiento en móvil | Parcial | No verificable sin dispositivo; los presupuestos no coinciden entre docs (QA-P-10, QA-P-12) |
| Mixamo + Animation Rigging | Parcial | `.gitignore` (QA-P-01); Animator con controlador y avatar en runtime (QA-P-29) |
| Verificación sin Editor | Parcial | Oráculo Dart + dotnet para lo puro (QA-P-23). Lo de Unity queda NO VERIFICABLE hasta la It. 2 (QA-P-25) |
| Paridad numérica | **No mitigado todavía** | QA-P-02 con evidencia |

## 9. Condiciones para el spec de la It. 1 (checklist)

1. P-01 ampliada (§4) y `ci-core.yml`.
2. QA-P-01 (`.gitignore`), QA-P-03 (asmdef de soporte de tests) y QA-P-15 (árbol y rutas únicos).
3. QA-P-02 + QA-P-23: algoritmo de `Fmt.Fixed` y `DartMath`, con vectores dorados del oráculo (incluida la tabla de §3.2).
4. QA-P-04: tabla exacta de textos es/en del resumen y las etiquetas; qué hace Q-02 en la It. 1.
5. QA-P-05: `Tokens.cs` como fuente de los colores de paso (F-RUT-01).
6. Bootstrapper mínimo (QA-P-25) y sin niveles de calidad definitivos (QA-P-10).
7. Criterios de la It. 1 del ROADMAP: 19 casos legacy, 8 paquetes FTMS, formatos, RUT §5.3 casos 1-15 y paridad de las 159 claves. Los valores ya los confirmó el oráculo.
