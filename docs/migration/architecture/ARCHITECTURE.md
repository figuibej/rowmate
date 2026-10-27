# Arquitectura de RowMate en Unity

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-27 |
| Estado | Vigente desde la It. 1, revisada tras el [gate de QA del plan](../QA_PLAN_REVIEW.md) (cambios al final, "Cambios por QA (plan)"). Las decisiones mayores tienen ADR en [adr/](adr/) |
| Insumos | [ROADMAP](../ROADMAP.md) (17 iteraciones + S-1), [FEATURE_INVENTORY](../product/FEATURE_INVENTORY.md), inventarios de [product/inventory/](../product/inventory/), [LEGACY_VISUAL_AUDIT](../design/LEGACY_VISUAL_AUDIT.md), [ART_BIBLE](../design/ART_BIBLE.md), [ANIMATION_BIBLE](../design/ANIMATION_BIBLE.md), [ASSET_SLOTS](../design/ASSET_SLOTS.md), código de `rowmate-legacy/lib` y `test/` |
| Fuente única de | Árbol de carpetas y rutas (§2), assemblies (§3), presupuesto de rendimiento y niveles de calidad (§20). Los documentos de diseño los referencian |
| Mapeo archivo por archivo | [LEGACY_MAPPING.md](LEGACY_MAPPING.md) |

Este documento fija el **CÓMO** transversal de la migración. Cada iteración lo baja a detalle en su `02-tech-design.md`; si una iteración necesita apartarse de algo de acá, lo dice en "Desvíos propuestos" y se actualiza el ADR correspondiente.

---

## 0. Resumen de decisiones

| # | Tema | Decisión | ADR |
|---|---|---|---|
| 1 | Motor | **Unity 6.3 LTS, `6000.3.27f1`** (la LTS vigente; 6.0 LTS termina su soporte en octubre de 2026 y 6.7 LTS sigue en beta). URP 17.3.0 | [001](adr/001-unity-version-y-paquetes.md) |
| 2 | Capas | Core puro (`noEngineReferences`) ← App (estado de pantallas, puro) ← Infrastructure (SQLite, Strava, prefs; sin API de Unity) ← Platform / Presentation / Scene (Unity) ← Bootstrap (composition root). Lo puro compila con `dotnet` | [002](adr/002-capas-y-asmdefs.md) |
| 3 | Simulador | Scripting define `ROWMATE_SIMULATOR` + asmdefs con `defineConstraints` (el código del simulador no existe en un build normal) + guardia de build | [003](adr/003-composition-root-y-simulador.md) |
| 4 | Reactividad | `ObservableValue<T>` / `EventStream<T>` propios (~150 líneas) en Core. Sin UniRx, R3 ni data binding por reflexión en caminos calientes | [004](adr/004-estado-y-reactividad.md) |
| 5 | Asincronía | `Task` + `IScheduler` (reloj inyectable, falso en tests) en lo puro; `Awaitable` solo en el pegamento Unity. Sin UniTask. Todo el dominio corre en el hilo principal | [005](adr/005-hilos-y-asincronia.md) |
| 6 | UI | **UI Toolkit** para toda la UI 2D: pantallas, HUD, diálogos, gráficos (Painter2D). HUD "glass" con UI Shader Graph (6.3) sobre la textura global `_RmHudBlur` de un Renderer Feature de URP; cifras con `TabularNumber` (un solo elemento por número); 60 fps solo mientras algo se anima | [006](adr/006-ui-toolkit.md) |
| 7 | Navegación | Shell con 4/5 pestañas que se ocultan sin destruirse (equivalente a `IndexedStack`), pila de páginas y capas de diálogo, aviso y simulador | [007](adr/007-navegacion.md) |
| 8 | Persistencia | SQLite con **sqlite-net** (paquete `com.gilzoide.sqlite-net` 1.3.3, MIT) en un hilo propio; esquema = v7 del legacy + columna `origin` (v8); preferencias en archivo JSON propio con **las mismas claves**; tokens en almacén seguro nativo | [008](adr/008-persistencia.md) |
| 9 | BLE | Interfaz mínima `IBleAdapter` en Core; la lógica del legacy (remo, pulsómetro, reintentos) se porta sobre ella en Core. Adaptadores nativos propios: Kotlin (Android), Swift/CoreBluetooth (iOS, macOS), C++/WinRT (Windows); eventos con marca de tiempo en un *buffer* nativo que C# drena cada frame (*pull*) | [009](adr/009-ble-nativo.md) |
| 10 | Strava | OAuth con `ASWebAuthenticationSession` (iOS/macOS), navegador + deep link `rowmate://` (Android) y *loopback* `http://127.0.0.1` (Windows, Editor). Credenciales inyectadas al compilar; tokens en Keychain / Keystore / DPAPI; subidas bloqueadas en simulador | [010](adr/010-strava.md) |
| 11 | Textos | Catálogo propio es/en en JSON (mismas claves y placeholders de los `.arb`) + formateador ICU mínimo (placeholders y plurales). Sin el paquete Localization | [011](adr/011-localizacion.md) |
| 12 | Paridad numérica | `Fmt.Fixed` con expansión decimal **exacta** del `double` (desempate alejándose del cero, `-0` como Dart), `DartMath.Round : long`, `IsoDate` con microsegundos, cultura invariante, RNG de Dart portado; vectores dorados del oráculo Dart | [012](adr/012-paridad-numerica-dart.md) |
| 13 | Tests | Mismas fuentes NUnit 3 en `tools/dotnet` (net8.0) y en Unity Test Framework 1.6; asmdef `RowMate.Tests.Support` con los *fakes*; los `.csproj` copian el grafo de asmdefs; `tools/oracle` genera el JSON dorado | [013](adr/013-tests-dotnet-y-unity.md) |
| 14 | Bootstrap sin Editor | `manifest.json` + `ProjectVersion.txt` escritos a mano; script de Editor `[InitializeOnLoad]`: **mínimo** en la It. 1 (Player + un asset de URP), completo en la It. 2; los 3 niveles de calidad llegan en la It. 6a | [014](adr/014-bootstrap-sin-editor.md) |
| 15 | Escena 3D | Coordenadas "cinta", un escenario por escena aditiva, agua con fases en `double`, fase visual de palada, *asset slots* (`AssetSlotDefinition` con subtipos tipados y `SlotIds` en Core), miniaturas horneadas | [015](adr/015-escena-3d.md) |
| 16 | Tokens de diseño | `Core/Design/Tokens.cs` como **única** fuente de colores, radios, tamaños y duraciones; `Tokens.uss` generado por `tools/tokens` con test de sincronía; test de contraste WCAG en `dotnet` | [016](adr/016-tokens-de-diseno.md) |
| 17 | Segundo plano | Paridad Q-91: la sesión sigue. Keepalive FTMS en el plugin nativo mientras la app no está en primer plano, eventos nativos con marca de tiempo en un buffer nativo y **alcance** del reloj por tiempo monótono al volver | [017](adr/017-segundo-plano.md) |
| 18 | Niveles de calidad | Alto/Medio/Bajo como `ScriptableObject` con un asset de URP cada uno; adaptación térmica con `FrameTimingManager` (respaldo: tiempo de CPU) y `renderScale` con cambios poco frecuentes | [018](adr/018-niveles-de-calidad.md) |
| 19 | Canal de color de la UI | El panel de la app se dibuja en una RT `R8G8B8A8_UNorm` con `forceGammaRendering` (composición sRGB como el legacy) y `OverlayCopyPresenter` la copia a una RT sRGB que muestra un panel Overlay; `Overlay` directo queda de respaldo. Verificado en el build de Windows (S-B1) | [019](adr/019-canal-de-color-de-la-ui.md) |

---

## 1. Versiones de Unity y paquetes

Verificado el **2026-10-27** (fuentes en §23).

### 1.1 Editor

| Ítem | Valor | Motivo |
|---|---|---|
| Versión | **Unity 6.3 LTS `6000.3.27f1`** (publicada el 2026-10-20, último parche de la rama 6000.3) | Es la LTS vigente, con soporte hasta diciembre de 2027. Unity 6.0 LTS termina su soporte en octubre de 2026. Unity 6.7 LTS está en beta desde el 2026-09-17: no se usa una beta. **Cumple el pedido del Designer**: UI Shader Graph (vidrio del HUD) existe desde 6.3 y solo con URP, así que el vidrio con desenfoque es posible; el respaldo de vidrio plano (token `glass.fallback`, §7.4) queda para el nivel Bajo y la It. 4a |
| Backend de scripting | IL2CPP en las 4 plataformas | Obligatorio en iOS; en Android y desktop por rendimiento y paridad de comportamiento entre plataformas |
| API Compatibility Level | .NET Standard 2.1 | Coincide con `netstandard2.1` del Core en `dotnet` |
| Lenguaje | C# 9 (el compilador de Unity 6.3) | Ver §18 |
| Color space | Linear | Requisito de la luz realista (URP) |
| Input | Input System (modo "Input System Package (New)") | UI Toolkit y teclado/gamepad modernos; el Input Manager viejo está deprecado |
| Actualización | Se sube de parche (6000.3.x) cuando QA lo apruebe. Se evalúa pasar a **6.7 LTS** cuando salga (trae localización integrada y mejoras de UI Toolkit); no antes de la It. 14 salvo bloqueo | Estabilidad |

`ProjectSettings/ProjectVersion.txt` (lo único de `ProjectSettings/` que se escribe a mano):

```
m_EditorVersion: 6000.3.27f1
```

### 1.2 Paquetes (`Packages/manifest.json`)

| Paquete | Versión | Por qué | Desde |
|---|---|---|---|
| `com.unity.render-pipelines.universal` | 17.3.0 | URP (paquete core, va atado al Editor) | It. 1 |
| `com.unity.shadergraph` | 17.3.0 | UI Shader Graph (glass del HUD) y shaders del Designer | It. 1 |
| `com.unity.inputsystem` | 1.20.1 | Entrada (táctil, mouse, teclado, botón "atrás" de Android) | It. 1 |
| `com.unity.test-framework` | 1.6.0 | Unity Test Framework (usa `com.unity.ext.nunit` 2.0.5, basado en NUnit 3.5) | It. 1 |
| `com.unity.ugui` | 2.0.0 | No se usa para pantallas; queda para `EventSystem` si hiciera falta mezclar entrada (y lo piden algunas herramientas) | It. 1 |
| `com.unity.nuget.newtonsoft-json` | 3.2.2 (Newtonsoft.Json 13.0.2, compatible con AOT) | JSON de Strava y de preferencias, leído con `JToken` (sin reflexión) | It. 1 |
| `com.unity.animation.rigging` | 1.4.1 | IK de manos y pies del remero sobre el rig de Mixamo | It. 1 (se usa en la It. 7) |
| `com.gilzoide.sqlite-net` | `https://github.com/gilzoide/unity-sqlite-net.git#1.3.3` | sqlite-net 1.9.172 + SQLite 3.53.4 (SQLite3 Multiple Ciphers 2.5.1) precompilado para Android (arm64, armv7, x86_64), iOS (estático), macOS (universal) y Windows (x64, arm64); MIT | It. 1 (se usa en la It. 3) |
| `com.unity.ide.visualstudio` | 2.0.28 | Integración con VS Code / Visual Studio | It. 1 |
| `com.unity.mobile.android-logcat` | 1.4.7 | Diagnóstico de BLE en Android (solo Editor) | It. 1 |
| Módulos `com.unity.modules.*` | 1.0.0 | `androidjni`, `animation`, `audio`, `imgui`, `jsonserialize`, `particlesystem`, `physics`, `terrain`, `ui`, `uielements`, `unitywebrequest`, `unitywebrequesttexture` | It. 1 |

Paquetes **descartados** con motivo: `com.unity.localization` 1.5.13 (ver ADR 011), Cinemachine 3.1 (la cámara del legacy es una sola y su matemática está en Core; entra solo si se hacen las cámaras extra de BKL-02, D-ART-07/D-ANI-07), Addressables (los escenarios se cargan como escenas aditivas; no hay contenido descargable), UniTask (ADR 005), UniRx/R3 (ADR 004). Burst, Collections y Mathematics **no** se agregan como dependencias directas, pero Animation Rigging 1.4.1 trae Burst y Mathematics de forma transitiva: quedan en `packages-lock.json` y el código propio no los usa salvo que el Designer lo justifique para agua o vegetación. `com.unity.ui.test-framework` (core 6.3) se agrega en la It. 2, cuando haya Editor para fijar la versión exacta.

**Íconos (QA-P-09).** Decisión, alineada con el Designer (ART_BIBLE §4.1, ASSET_SLOTS §4): los íconos son **glifos de fuente** de "RowMate Icons" (Material Symbols Rounded, Apache 2.0, versión fijada, recortada a los glifos de la app), importada como `FontAsset` SDF en `UI/Icons/` y teñida con el color del token. Las constantes de codepoints viven en `Core/Design/Icons.cs` con un test de `dotnet` contra el `.codepoints` versionado. **Sin** `com.unity.vectorgraphics` (solo existe como *preview* 3.0.0). Dato para QA: Unity 6.3 trae además Vector Graphics como **módulo integrado** del Editor, que importa SVG como *UI Toolkit Vector Image* sin paquete ([manual de Vector Graphics 3.0](https://docs.unity3d.com/Packages/com.unity.vectorgraphics@3.0/manual/index.html)); queda como opción para un ícono multicolor, con aprobación de QA, y no se usa por defecto.

Regla: **todo paquete nuevo entra con versión exacta, motivo y fuente** en esta tabla (o en el diseño de la iteración) y lo aprueba QA.

```json
{
  "dependencies": {
    "com.gilzoide.sqlite-net": "https://github.com/gilzoide/unity-sqlite-net.git#1.3.3",
    "com.unity.animation.rigging": "1.4.1",
    "com.unity.ide.visualstudio": "2.0.28",
    "com.unity.inputsystem": "1.20.1",
    "com.unity.mobile.android-logcat": "1.4.7",
    "com.unity.nuget.newtonsoft-json": "3.2.2",
    "com.unity.render-pipelines.universal": "17.3.0",
    "com.unity.shadergraph": "17.3.0",
    "com.unity.test-framework": "1.6.0",
    "com.unity.ugui": "2.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0"
  }
}
```

La dependencia por URL de git necesita `git` en el `PATH` de la máquina que abre el proyecto (está, por el repo). `Packages/packages-lock.json` lo genera Unity en el primer open y se versiona.

### 1.3 Herramientas fuera de Unity

| Herramienta | Versión | Uso |
|---|---|---|
| .NET SDK | 8.0.427 (instalado en `C:/Program Files/dotnet/dotnet.exe`) | Compilar y testear lo puro (§15) |
| NuGet en `tools/dotnet` | NUnit **3.14.0** (rama 3.x, por compatibilidad con el NUnit 3.5 de Unity), NUnit3TestAdapter 4.6.0, Microsoft.NET.Test.Sdk 17.11.1, sqlite-net-pcl 1.9.172 (misma API que el paquete de Unity), Newtonsoft.Json 13.0.2 (la misma que trae Unity) | Se fijan en la It. 1 con `dotnet list package` y no se suben sin motivo |
| Android | SDK/NDK/JDK que instala Unity Hub con el módulo Android | Build Android y compilación de las fuentes Kotlin |
| Windows | Visual Studio 2022 con "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621+ | IL2CPP Windows y el plugin C++/WinRT |
| Apple | Mac con Xcode 16+ | IL2CPP iOS/macOS, plugin Swift de macOS, firma |

---

## 2. Estructura del repositorio y de `Assets/`

Este árbol es la **única** referencia de rutas del proyecto (QA-P-15). ART_BIBLE, ANIMATION_BIBLE y ASSET_SLOTS lo referencian; si un documento de diseño nombra otra ruta, manda esta.

```
rowmate/
├── Assets/
│   ├── csc.rsp                         # -nullable:enable (mismo contexto de nulabilidad que dotnet)
│   ├── RowMate/
│   │   ├── Core/                       # RowMate.Core (puro)
│   │   │   ├── Common/                 # ObservableValue, EventStream, SubscriberErrors, IScheduler, IClock, IRandom, IAppLogger (D-03), Result, Json/, AppIdentity; Internal/IsExternalInit
│   │   │   ├── Text/                   # Fmt, DartMath, IsoDate (paridad numérica, ADR 012)
│   │   │   ├── Design/                 # Tokens (única fuente de colores, radios, tamaños y duraciones), Icons (codepoints), Contrast (WCAG), Easing (ADR 016)
│   │   │   ├── Models/                 # RowingData, StepType, StepTypeInfo, MetricPalette, IntervalStep, Routine, WorkoutSession, DataPoint, SessionStats
│   │   │   ├── Ble/                    # BleUuid, BleScanResult, IBleAdapter, FtmsParser, HeartRateParser, ScanFilter, RowerService, HeartRateService, ReconnectLoop
│   │   │   ├── Workout/                # WorkoutEngine, StepTargetTracker, SeriesTracker, WorkoutFormats, StrokeCounterEvents
│   │   │   ├── History/                # ChartGeometry, StepPerformance, StepBands
│   │   │   ├── Scene/                  # SceneCamera, SceneState, StrokeCycle, SceneTimeOfDay, HourLighting, CelestialPlacement, MoonPhase,
│   │   │   │   │                       #   Environments, DartRandom, WaterParams, SkyModel, SlotIds
│   │   │   │   └── Stroke/             # StrokeRhythm, VisualStrokeClock, ScullingKinematics, BoatSpec, LegacyBoatGeometry, BoatSurge, BoatMotion,
│   │   │   │                           #   EffortModel, PuddleField, ChaseCameraMotion (ANIMATION_BIBLE §7.2)
│   │   │   ├── Strava/                 # StravaConfig, TcxBuilder, StravaMetadata, UploadPreference, SessionRepair, ImportedTelemetry
│   │   │   ├── Localization/           # Localizer, MessageFormatter, LocaleResolver
│   │   │   └── Ports/                  # IPreferences, ISecureStore, IRoutineRepository, ISessionRepository, IHttpClient, IWakeLock,
│   │   │                               #   IAppLifecycle, ISystemLocale, IAccessibilitySettings…
│   │   ├── App/                        # RowMate.App (puro): stores por pantalla, AppComposer, ISceneHost
│   │   ├── Infrastructure/             # RowMate.Infrastructure: SQLite, preferencias JSON, clientes de Strava
│   │   ├── Simulator/                  # RowMate.Simulator (puro, solo con ROWMATE_SIMULATOR)
│   │   ├── SimulatorPresentation/      # RowMate.Simulator.Presentation (UI del panel, solo con ROWMATE_SIMULATOR)
│   │   ├── Platform/                   # RowMate.Platform: adaptadores BLE, almacén seguro, wakelock, http, OAuth, permisos, scheduler,
│   │   │                               #   idiomas del sistema, accesibilidad, GraphicsTier, ThermalAdapter
│   │   ├── Presentation/               # RowMate.Presentation (código + UXML/USS de la UI)
│   │   │   ├── Uxml/  Styles/  Controls/  Screens/  Motion/
│   │   │   └── Styles/Tokens.uss       # GENERADO por tools/tokens desde Core/Design/Tokens.cs (no se edita a mano)
│   │   ├── Scene/                      # RowMate.Scene (código): director, cámara, agua, cielo, bote, remero, orillas, VFX, AssetSlotDefinition y subtipos
│   │   ├── Bootstrap/                  # RowMate.Bootstrap: AppRoot (composition root), PlatformFactory, StravaCredentials
│   │   │   └── Generated/              # StravaSecrets.g.cs, generado al compilar (IGNORADO por git, R-02)
│   │   ├── Editor/                     # RowMate.Editor (namespace RowMate.EditorTools, §17): bootstrapper, modo simulador y guardia (Simulator/),
│   │   │                               #   build CLI, post-procesos, EnvironmentThumbnailBaker, validadores de slots
│   │   ├── Localization/               # strings.es.json, strings.en.json (datos)
│   │   ├── Art/                        # Designer, sin código: Shaders/ (Water, Sky, Prop, Foliage, Fabric, Skin, Glass, UI), Materials/,
│   │   │                               #   Textures/ (incluye Thumbnails/ horneadas), Models/ (procedurales horneados), Vfx/
│   │   ├── UI/                         # Assets de UI sin código: Fonts/<Familia>/ (+ OFL.txt), Icons/ (fuente "RowMate Icons" + LICENSE), Brand/Strava/
│   │   ├── Slots/                      # Instancias de slots (*.asset), una por id de SlotIds: rower.character.asset, prop.pine.asset…
│   │   ├── Scenes/                     # Boot.unity, World.unity, Environments/{Lake,River,Coast,Regatta}.unity
│   │   ├── Settings/                   # URP/ (RowMate_Renderer + RowMate_URP_{High,Medium,Low}), Quality/ (QualityTierProfile ×3),
│   │   │                               #   UI/ (RowMatePanel.asset); los crea el bootstrapper
│   │   └── Tests/
│   │       ├── Support/                # RowMate.Tests.Support: FakeScheduler, FakeBleAdapter, InMemory repos/prefs, RepoPaths,
│   │       │                           #   TestMainThreadContext, InlineDbExecutor, GoldenVectors
│   │       ├── EditMode/               # RowMate.Tests.EditMode (Core, App, Infrastructure) — compartidos con dotnet
│   │       ├── Simulator/              # RowMate.Tests.Simulator — compartidos con dotnet
│   │       ├── PlayMode/               # RowMate.Tests.PlayMode — solo Unity (UI y escena)
│   │       └── Golden/oracle/          # vectores dorados del oráculo Dart (*.json, ≤ 1 MiB c/u; D-01 de la It. 1)
│   ├── ThirdParty/                     # ÚNICA ruta de terceros, IGNORADA por git salvo los .meta (QA-P-01):
│   │   ├── Mixamo/<Personaje>/         #   YBot/YBot.fbx, XBot/XBot.fbx, RowerRealistic/… (los descarga el usuario, ASSET_SLOTS §2)
│   │   ├── TerrainSampleAssetPack/     #   Asset Store de Unity (ASSET_SLOTS §5.2)
│   │   └── CC0/                        #   opcionales con OK del usuario (D-ART-08)
│   └── Plugins/
│       ├── Android/RowMateNative/      # Kotlin: RowMateBle.kt, SecureStore.kt; AndroidManifest.xml propio
│       ├── iOS/RowMateNative/          # Swift: RowMateBle.swift, Keychain.swift, WebAuth.swift, SystemUi.swift, SystemInfo.swift
│       ├── macOS/RowMateNative.bundle  # precompilado desde native/macos (mismas fuentes Swift que iOS)
│       └── Windows/x86_64/RowMateBle.dll  # precompilado desde native/windows (C++/WinRT)
├── native/
│   ├── macos/                          # proyecto Xcode que compila Assets/Plugins/iOS/RowMateNative/*.swift como .bundle universal
│   └── windows/                        # CMake + C++/WinRT → RowMateBle.dll (API C)
├── Packages/manifest.json
├── ProjectSettings/ProjectVersion.txt
├── tools/
│   ├── dotnet/                         # RowMate.sln, Directory.Build.props, un .csproj por asmdef puro (incluidos los de test, §15)
│   ├── oracle/                         # Oráculo Dart: bin/*.dart + lib/golden.dart + run-oracle.ps1 (package_config propio, legacy en solo lectura);
│   │                                   #   los JSON van a Assets/RowMate/Tests/Golden/oracle/ (D-01 de la It. 1)
│   ├── tokens/                         # Generador dotnet: Core/Design/Tokens.cs → Presentation/Styles/Tokens.uss
│   ├── unity/                          # run-unity-tests.ps1: tests dentro de Unity en batchmode, con -Simulator (QA-V-05; .artifacts/ ignorado)
│   ├── (ci/)                           # no se crea: la higiene del repositorio son tests de dotnet (RepoHygieneTests, PureCodeRulesTests; D-04)
│   ├── spikes/ble-windows/             # S-1: consola net8.0 con P/Invoke a RowMateBle.dll (sin Unity)
│   ├── build/                          # build.ps1 / build.sh (Unity en batchmode)
│   ├── strava/                         # write-secrets.ps1, strava.local.example.json (strava.local.json, ignorado)
│   └── l10n/                           # conversor .arb → .json (one-shot de la It. 1) y verificador
├── .github/workflows/                  # ci-core.yml (dotnet + higiene), ci-unity.yml (desde la It. 2, si hay licencia)
├── .gitattributes                      # LFS para binarios propios (png, psd, exr, wav, dll, bundle, a, ttf)
└── docs/
```

Notas:
- Los `.meta` los genera Unity en el primer open; **se versionan** desde ese commit. Hasta entonces ningún archivo escrito a mano referencia un GUID: los asmdef se referencian **por nombre** y los UXML a sus USS por **ruta relativa**.
- **Terceros** (Mixamo, Asset Store, CC0): solo en `Assets/ThirdParty/`. El `.gitignore` excluye todo su contenido salvo los `.meta` (verificado con `git check-ignore` y `git add` en un repo de prueba: `YBot.fbx` ignorado, `YBot.fbx.meta` versionado). Los `.meta` conservan el GUID que referencia el slot y los ajustes de importación (Humanoid, *Optimize Game Objects* desactivado). Cuidado: si se abre el proyecto **sin** los archivos descargados, Unity borra los `.meta` huérfanos; se recuperan con `git checkout -- Assets/ThirdParty` antes de copiar los archivos (RA-17). Además cada slot guarda la ruta esperada (`expectedPath`) y el validador del Editor vuelve a enlazar por ruta si el GUID se perdió.
- No hay FBX propios: todo `.fbx` versionado fuera de `Assets/ThirdParty/` es un error que detecta el CI (§19). Las mallas propias son procedurales (código) o se hornean a `.asset`/`.mesh` en `Art/Models/`.
- Fuentes OFL e íconos Apache 2.0 **sí** se versionan, con su licencia, en `UI/Fonts/` y `UI/Icons/`.
- Shaders y Shader Graph van en `Art/Shaders/` (Unity no los compila por asmdef, así que su carpeta no cambia de assembly). Los slots (`*.asset`) van en `Slots/`; sus clases (`AssetSlotDefinition` y subtipos) en `Scene/`; sus ids (`SlotIds`) en `Core/Scene/`.
- El catálogo de textos se guarda como `.json` porque Unity no importa `.arb` como `TextAsset`.

---

## 3. Assemblies y regla de dependencias

### 3.1 Tabla de asmdefs

| Assembly | Carpeta | API de Unity | `noEngineReferences` | `defineConstraints` | Referencias | Compila en `dotnet` |
|---|---|---|---|---|---|---|
| `RowMate.Core` | `Core/` | No | **true** | — | — | Sí |
| `RowMate.App` | `App/` | No | **true** | — | Core | Sí |
| `RowMate.Infrastructure` | `Infrastructure/` | No (por regla) | false (\*) | — | Core, `Gilzoide.SqliteNet`, `Newtonsoft.Json.dll` | Sí (con NuGet equivalentes) |
| `RowMate.Simulator` | `Simulator/` | No | **true** | `ROWMATE_SIMULATOR` | Core, App | Sí (con el define) |
| `RowMate.Platform` | `Platform/` | Sí | false | — | Core, `Unity.InputSystem` | No |
| `RowMate.Presentation` | `Presentation/` | Sí (UI Toolkit) | false | — | Core, App, `Unity.InputSystem` | No |
| `RowMate.Scene` | `Scene/` | Sí (URP) | false | — | Core, App, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.Animation.Rigging` | No |
| `RowMate.Simulator.Presentation` | `SimulatorPresentation/` | Sí | false | `ROWMATE_SIMULATOR` | Core, App, Simulator, Presentation | No |
| `RowMate.Bootstrap` | `Bootstrap/` | Sí | false | — | Core, App, Infrastructure, Platform, Presentation, Scene, Simulator, Simulator.Presentation | No |
| `RowMate.Editor` | `Editor/` | Sí (Editor) | false | — (`includePlatforms: ["Editor"]`) | todas + `Unity.RenderPipelines.Universal.Editor` | No |
| `RowMate.Tests.Support` | `Tests/Support/` | No | **true** | `UNITY_INCLUDE_TESTS` | Core, App, Infrastructure; `nunit.framework.dll` (`overrideReferences: true`, para los *helpers* de aserción) | Sí |
| `RowMate.Tests.EditMode` | `Tests/EditMode/` | No (por regla) | false | `UNITY_INCLUDE_TESTS` | Tests.Support, Core, App, Infrastructure, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `nunit.framework.dll`. `includePlatforms: ["Editor"]` | Sí |
| `RowMate.Tests.Simulator` | `Tests/Simulator/` | No (por regla) | false | `UNITY_INCLUDE_TESTS`, `ROWMATE_SIMULATOR` | Tests.Support, Core, App, Simulator + TestRunner; `nunit.framework.dll`. `includePlatforms: ["Editor"]` | Sí |
| `RowMate.Tests.PlayMode` | `Tests/PlayMode/` | Sí | false | `UNITY_INCLUDE_TESTS` | Tests.Support, Presentation, Scene, Bootstrap, App, Core + TestRunner; `nunit.framework.dll` | No |

(\*) `Infrastructure` no lleva `noEngineReferences` porque referencia al asmdef del paquete de SQLite, que sí usa tipos de Unity en algunas firmas (extensiones con `NativeArray`); con `noEngineReferences` el compilador podría pedir `UnityEngine` al resolver esas extensiones. La regla "sin API de Unity" la hace cumplir el build de `dotnet`, donde `UnityEngine` no existe.

**Soporte de tests (QA-P-03).** `Tests/Support/` es un asmdef propio, **hermano** de los de test: un asmdef solo compila los archivos de su carpeta, así que `Tests/EditMode` no podría incluir `Tests/Support`. `RowMate.Tests.Support` no tiene `[Test]`, no usa API de Unity y solo existe en compilaciones con tests (`UNITY_INCLUDE_TESTS`). Lo referencian los tres assemblies de test; **no** referencia `RowMate.Simulator` (sin el define ese assembly no existe y Support dejaría de compilar), así que los *fakes* que usa `SimulatedRowerServiceTests` (`FakeScheduler`) son de Core. El grafo se verifica fuera de Unity: los `.csproj` de `tools/dotnet` copian estas referencias una por una y un test de *lint* de asmdefs compara ambos grafos (§15.1).

### 3.2 Regla de dependencias

```
                 RowMate.Core  (puro: dominio, reglas, puertos)
                   ▲    ▲    ▲
        ┌──────────┘    │    └───────────────┐
   RowMate.App   RowMate.Infrastructure   RowMate.Simulator*
  (stores, puro)   (SQLite, Strava, JSON)   (puro)
     ▲      ▲              ▲                  ▲
     │      └──────┐       │                  │
 Presentation   Scene   Platform              │
     ▲      ▲      ▲       ▲                  │
     │   Simulator.Presentation*              │
     └──────┴──────┴───────┴──────────────────┘
                     RowMate.Bootstrap (composition root)
                     RowMate.Editor (todo)        * = solo con ROWMATE_SIMULATOR
```

1. Las flechas apuntan hacia el Core. **Core no referencia nada** (ni Newtonsoft ni sqlite-net).
2. `Presentation` y `Scene` **no se referencian entre sí** ni referencian `Platform` ni `Infrastructure`: reciben interfaces de Core/App ya construidas. El inmersivo arranca y detiene la escena a través de `App.ISceneHost`, que implementa `Scene`.
3. Nadie fuera de `Bootstrap` (y de los tests y el Editor) referencia `Simulator`. `Bootstrap` lo usa solo dentro de `#if ROWMATE_SIMULATOR`.
4. Toda regla de negocio, fórmula, umbral o máquina de estados vive en Core o App. Los MonoBehaviours y las vistas son delgados: leen estado, dibujan y reenvían gestos.
5. Cómo se hace cumplir: `autoReferenced: false` en todos los asmdefs propios; el build de `dotnet` falla si un archivo puro usa `UnityEngine`; QA revisa las referencias de cada `.asmdef` en cada iteración.

Ejemplo (`Assets/RowMate/Core/RowMate.Core.asmdef`):

```json
{
  "name": "RowMate.Core",
  "rootNamespace": "RowMate.Core",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": true
}
```

---

## 4. Composition root y simulador

### 4.1 Arranque

`Boot.unity` contiene un único `AppRoot : MonoBehaviour` (`[DefaultExecutionOrder(-10000)]`, en `RowMate.Bootstrap`), el `UIDocument` del shell y una cámara de UI. `AppRoot.Awake()`:

1. Fija `CultureInfo.DefaultThreadCurrentCulture` y `CurrentCulture` a `InvariantCulture` (punto decimal siempre, F-APP-12).
2. Crea los servicios de hilo principal: `UnityScheduler` (reloj monótono nativo, timers y alcance, §6), `NativeEventPump` (drena el *buffer* de eventos de los plugins al inicio de cada frame, §10.4) y `MainThreadQueue` (cola lock-free para lo que llega de hilos administrados, como el *loopback* de OAuth).
3. `PlatformFactory.Create()` devuelve `IPlatformServices` según `#if UNITY_ANDROID / UNITY_IOS / UNITY_STANDALONE_WIN / UNITY_STANDALONE_OSX` (el Editor usa la plataforma del sistema anfitrión).
4. Elige el *backend* de dispositivos:

```csharp
// RowMate.Bootstrap/CompositionRoot.cs
#if ROWMATE_SIMULATOR
    var devices = SimulatorBackend.Create(scheduler);           // remo + pulsómetro simulados, comparten RowingSimulator
    const string dbFile = "rower_app_dev.db";
#else
    var devices = HardwareBackend.Create(platform.Ble, scheduler, prefs, platform.Kind);
    const string dbFile = "rower_app.db";
#endif
```

5. `AppComposer.Compose(...)` (en `RowMate.App`, puro y testeable) arma `AppContext`: stores de cada pantalla, motor de entrenamiento, repositorios, localizador, Strava (solo si `StravaConfig.IsConfigured`), capacidades (`CanUploadToStrava = configurado && !simulador`).
6. Pide el wakelock (`IWakeLock.Enable()`), arranca `HeartRateService.AutoConnectAsync()` sin esperar (equivalente al `Provider` *eager* de `main.dart:68-79`; un error va al log) y carga rutinas.
7. Entrega `AppContext` a `AppShellController` (Presentation) y a `SceneDirector` (Scene) **por parámetro**. En simulador, además monta `SimulatorOverlay` en la capa superior.
8. `OnDestroy()` descarta todo en orden inverso (`IDisposable`).

No hay singletons estáticos ni service locator. La única "estática" permitida es `BuildFlags` (constantes de compilación) y `StravaCredentials` (constantes generadas al compilar).

### 4.2 Simulador (equivalente a `kSimulator`)

| Legacy | Unity |
|---|---|
| `const bool kSimulator = bool.fromEnvironment('SIMULATOR')` | Scripting define `ROWMATE_SIMULATOR`; `BuildFlags.IsSimulator` es `const bool` dentro de `#if` |
| Tree-shaking de las clases del simulador | `RowMate.Simulator` y `RowMate.Simulator.Presentation` tienen `defineConstraints: ["ROWMATE_SIMULATOR"]`: sin el define, Unity **no compila ni referencia** esos assemblies |
| `SimulatedBleService implements BleService` | `SimulatedRowerService : IRowerService`; la UI solo ve `IRowerService` (R-06) |
| `rower_app_dev.db` | `dbFile` en el composition root (§9) |
| Strava bloqueado | `StravaApiClient.UploadAsync` devuelve "bloqueado" antes de cualquier request si `BuildFlags.IsSimulator` (como `strava_api_service.dart:81`), y la UI oculta todo botón de subida (`AppCapabilities.CanUploadToStrava`, Q-78) |
| `SimulatorOverlay` en `MaterialApp.builder` | Capa superior del shell, por encima de pilas y diálogos |

Cómo se activa:
- **Editor**: menú `RowMate/Modo simulador` (agrega o quita el define en las plataformas Standalone, Android e iOS con `PlayerSettings.SetScriptingDefineSymbols`). Alternativa equivalente: un *Build Profile* de Unity 6 con el define. Lo implementa `RowMate.EditorTools.SimulatorDefine` (`Editor/Simulator/`), el **único** que escribe los defines.
- **Tests en Unity** (QA-V-05 de la It. 1): `tools/unity/run-unity-tests.ps1 -Simulator` activa el define solo para la corrida (`SimulatorDefine.EnableForTests`, guarda el estado previo en `Library/RowMate/simulator-tests.txt`), corre `-runTests` y **siempre** lo restaura (`RestoreAfterTests`, en `finally`). Mientras exista esa marca, la guardia hace fallar cualquier build. Un define olvidado en `ProjectSettings/*.asset` hace fallar el CI (`SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator`).
- **Build**: `tools/build/build.ps1 -Simulator` pasa `extraScriptingDefines = { "ROWMATE_SIMULATOR" }` y fuerza `BuildOptions.Development`.
- **Guardia (R-01)**: `SimulatorBuildGuard : IPreprocessBuildWithReport` hace fallar cualquier build que tenga el define sin `BuildOptions.Development`. Ningún workflow de distribución pasa el define. QA lo verifica en cada iteración que toque builds.

---

## 5. Estado y reactividad

Equivalente a `ChangeNotifier` + `Stream` del legacy, sin librerías:

```csharp
namespace RowMate.Core.Common
{
    public interface IReadOnlyObservable<T>
    {
        T Value { get; }
        // Llama a onChange en el hilo principal, sincrónicamente, cuando Value cambia.
        IDisposable Subscribe(Action<T> onChange, bool emitCurrent = true);
    }

    public sealed class ObservableValue<T> : IReadOnlyObservable<T>
    {
        // logger: adonde va la excepción de un suscriptor (QA-V-01 de la It. 1)
        public ObservableValue(T initial, IAppLogger logger, IEqualityComparer<T>? comparer = null);
        public T Value { get; set; }            // notifica solo si cambia
        public void ForceNotify();               // como _reemitStatus() del legacy
    }

    public sealed class EventStream<T> : IObservable<T> // broadcast, sin buffer (como StreamController.broadcast)
    {
        public EventStream(IAppLogger logger);
        public void Emit(T value);
        public IDisposable Subscribe(Action<T> handler);
        public IDisposable Subscribe(IObserver<T> observer);
    }
}
```

- **Servicios** (`IRowerService`, `IHeartRateService`, `WorkoutEngine`) exponen `IReadOnlyObservable<TStatus>` para el estado y `EventStream<T>` para flujos (lecturas, bpm, resultados de búsqueda).
- **Stores** (`RowMate.App`, uno por pantalla, puertos de los `*_provider.dart`) combinan servicios y exponen **un estado inmutable** por pantalla (`IReadOnlyObservable<DeviceState>`, `HrmCardState`, `WorkoutHudState`…). Los estados son `sealed record` con valores ya listos para mostrar (texto localizado, clave de color, habilitado o no), así la lógica de presentación del legacy (`heart_rate_provider_test`, `heart_rate_card_test`) se testea en `dotnet`.
- **Vistas** (UI Toolkit) se suscriben en `AttachToPanelEvent` y liberan en `DetachFromPanelEvent`; actualizan solo los elementos que cambian (binding manual con un helper `view.Bind(store.State, Render)`). No se usa el *runtime data binding* de UI Toolkit en el HUD: evita reflexión, sorpresas de AOT/stripping y allocations.
- Suscriptores guardados en arreglos copy-on-write: emitir no reserva memoria.
- **La excepción de un suscriptor no corta a los demás** ni sube a quien asignó o emitió (semántica de `ChangeNotifier.notifyListeners` y de `StreamController.broadcast`; QA-V-01 de la It. 1): se registra en el `IAppLogger` del observable (`Error`, tag `STATE`) y se sigue. Por eso el logger es obligatorio en el constructor; no hay sumidero estático (ADR 004).
- Todo cambio ocurre en el **hilo principal** (§6): no hay locks en Core.

---

## 6. Hilos, asincronía y tiempo

| Contexto | Qué corre | Cómo |
|---|---|---|
| Hilo principal | Todo Core, App, UI, escena | Un solo hilo lógico, como el isolate de Dart. Sin locks |
| Hilo de base de datos | Toda llamada a SQLite | `IDbExecutor`: en producción `SerialExecutor` (un hilo dedicado con cola). Devuelve `Task<T>`; al hacer `await` desde el hilo principal, la continuación vuelve al hilo principal por el `UnitySynchronizationContext`. En Core y App está prohibido `ConfigureAwait(false)` |
| Hilos nativos (BLE, OAuth loopback) | Eventos de Kotlin/Swift/WinRT, socket local | **Nunca** tocan Core: el plugin guarda cada evento **con su marca de tiempo monótona** en un *buffer* nativo y C# lo drena al principio de cada frame (`NativeEventPump`, §10.4). El *loopback* de OAuth encola en `MainThreadQueue` (lock-free) |
| Red (Strava) | `UnityWebRequest` | Asíncrono nativo de Unity; completa en el hilo principal |

**Tiempo inyectable** (equivalente a `fake_async`):

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }     // offset 0
    DateTimeOffset LocalNow { get; }   // hora local con su offset (D-11 de la It. 1: los modelos usan DateTimeOffset)
    double MonotonicSeconds { get; }   // reloj monótono, inmune a cambios de hora
}

public interface IScheduler : IClock
{
    IDisposable Schedule(TimeSpan delay, Action callback);          // Timer(...)
    IDisposable SchedulePeriodic(TimeSpan period, Action callback); // Timer.periodic(...)
    Task Delay(TimeSpan delay, CancellationToken ct = default);     // Future.delayed(...)
}
```

- `IClock.MonotonicSeconds` en Unity lo da el reloj monótono **nativo** que también sella los eventos BLE (Android `SystemClock.elapsedRealtimeNanos`, que incluye el sueño profundo; Apple `mach_continuous_time`; Windows `QueryPerformanceCounter`; Editor `Stopwatch`). Así eventos y timers comparten una sola línea de tiempo.
- `UnityScheduler` (Platform) avanza en `Update` con ese reloj. En primer plano un timer periódico dispara **a lo sumo una vez por frame**, como hoy. Al volver de una pausa larga (segundo plano, §6.1) hace **alcance** (*catch-up*): avanza en tiempo virtual desde la pausa hasta ahora, intercalando en orden los timers vencidos y los eventos nativos guardados según su marca de tiempo, igual que `FakeScheduler.Advance`. `IScheduler.IsCatchingUp` avisa a quien escribe al hardware (el keepalive no escribe en el pasado).
- `FakeScheduler` (Tests/Support) con `Advance(TimeSpan)`: ejecuta en orden los timers vencidos y completa los `Delay` **en línea**, así las continuaciones corren dentro de `Advance` y los tests son deterministas. `FakeBleAdapter.EnqueueAt(t, evento)` simula eventos guardados durante el segundo plano.
- **Asincronía determinista en `dotnet` (QA-P-26).** Fuera de Unity no hay `UnitySynchronizationContext`: sin cuidado, la continuación de un `await` sobre el `SerialExecutor` correría en el *thread pool*. `Tests/Support` trae `TestMainThreadContext` (un `SynchronizationContext` de un solo hilo que el test bombea con `RunUntilIdle()` o desde `FakeScheduler.Advance`) e `InlineDbExecutor` (ejecuta el trabajo de base en el hilo que llama). Los tests de stores y motor usan los dos; los de integración con SQLite real usan `SerialExecutor` dentro de `TestMainThreadContext.Run(async () => …)`, así las continuaciones vuelven siempre al hilo del test.
- **Async**: `Task` en todo lo puro (netstandard2.1). `Awaitable` (Unity 6) solo en el pegamento Unity (esperar un frame, volver al hilo principal). **No se usa UniTask** (ADR 005): evita una dependencia en el Core, `Task` alcanza porque ningún camino por frame es asíncrono, y `Awaitable` cubre lo que Unity necesita.
- Prohibido `async void` (salvo handlers de eventos de Unity/UI, que capturan y registran toda excepción). Toda tarea "disparada y olvidada" pasa por `TaskExtensions.Forget(logger)`, que registra el error (como `unawaited(...).catchError` del legacy).
- Cancelación: los "intentos superados" del pulsómetro se modelan con el **contador de generación** del legacy (`_connectGen`), no con `CancellationToken`, para portar la semántica exacta (DC-26).

### 6.1 Segundo plano (Q-91, QA-P-06; ADR 017)

**Corrección de la premisa anterior.** Este documento decía que Unity pausado era "igual que los timers de Dart suspendidos". Es falso: en Android el isolate de Dart **sigue corriendo** con la app en segundo plano, y en iOS con `bluetooth-central` corre cada vez que llega una notificación. En el legacy el keepalive `0x07` cada 2 s (`ble_service.dart:278-284`) y el reloj de 1 s (`workout_provider.dart:147`) siguen; en Unity, con el *player loop* pausado, se cortarían: el monitor saldría del modo Bluetooth y el reloj activo se frenaría. El PO fijó la paridad (Q-91): **la sesión sigue**.

| Plataforma | Qué hace Unity en segundo plano | Diseño |
|---|---|---|
| Windows y macOS (ventana minimizada o sin foco) | Con `PlayerSettings.runInBackground = true` el loop sigue | Todo sigue igual que en primer plano. Mientras no hay foco, `targetFrameRate = 10` y la escena no se dibuja (`OnDemandRendering.renderFrameInterval` alto) |
| Android | La actividad pausa el *player loop*; el proceso y el código Kotlin siguen vivos | Keepalive **nativo**, eventos guardados en el *buffer* nativo y alcance al volver |
| iOS (`bluetooth-central`, R-07) | El loop se detiene; el sistema despierta la app en cada notificación BLE y corre el código Swift | Keepalive nativo atado a las notificaciones (si pasaron ≥ 2 s desde la última escritura, escribe `0x07`) más un temporizador mientras haya tiempo de ejecución; eventos guardados y alcance al volver |

Reglas:
1. **El dueño del keepalive sigue siendo Core.** `RowerService` hace el keepalive con `IScheduler` en primer plano (testeable, mismos 2 s). Con `IAppLifecycle.Paused` llama a `IBlePeripheral.StartNativeKeepAlive(service, characteristic, [0x07], 2 s)` y cancela su timer; con `Resumed`, `StopNativeKeepAlive()` y vuelve su timer. `FakeBleAdapter` registra las dos llamadas.
2. **Reloj por tiempo monótono.** `WorkoutEngine` no cuenta "disparos": guarda el instante monótono de inicio de cada tramo activo y, en cada disparo, procesa todos los segundos enteros vencidos (`while (dueTicks > processedTicks) Tick()`). En primer plano es 1 por segundo, como el legacy; al volver procesa los que faltan, cada uno con los datos que había en ese instante virtual.
3. **Eventos con su hora.** Durante el segundo plano el *buffer* nativo guarda notificaciones FTMS, pulso y cambios de conexión con su marca de tiempo. Al volver, `UnityScheduler` hace alcance: reproduce eventos y timers en orden, así la telemetría cada 5 s, los parciales y los watchdogs (≥ 5 s sin datos, 10 s de pulso vencido) dan lo mismo que si la app hubiera seguido en primer plano. Tope del *buffer*: 6 h de eventos a 2 Hz por dispositivo (unos 2 MB); si se llena, descarta los más viejos y lo registra.
4. **Guardado.** Con `Paused`, el motor guarda en el acto lo pendiente (Q-12). En Android, si el sistema mata el proceso en segundo plano, se pierde lo que pasó desde esa pausa hasta la muerte (el legacy, que sigue corriendo, lo habría guardado). Es el riesgo residual RA-08; si la medición de la It. 10a lo muestra en la práctica, se propone al PO un servicio en primer plano de Android con notificación "Entrenamiento en curso" (agregado, no paridad).
5. **Verificación.** It. 4a [dotnet]: alcance con `FakeScheduler` + eventos guardados (reloj, telemetría, watchdogs, keepalive delegado). It. 10a [Device] Android y Windows: el monitor no sale del modo Bluetooth con la app 5 min en segundo plano y la sesión sigue; QA mide el legacy en el mismo teléfono como referencia (incluido qué pasa con la pantalla bloqueada, que define si el reloj cuenta el sueño profundo). It. 10b [Apple] iOS y macOS.

---

## 7. Interfaz de usuario

### 7.1 Tecnología: UI Toolkit (decidido)

| Criterio | UI Toolkit | uGUI |
|---|---|---|
| Tema y tokens del Designer | USS con variables (`--color-metric-watts`), hojas por tema | Prefabs y componentes, sin cascada |
| Layout vertical/horizontal (2 o 3 columnas, landscape 640×360) | Flexbox (Yoga) | RectTransform + LayoutGroups, más frágil |
| Gráficos del historial | `generateVisualContent` + `Painter2D` (líneas, rellenos, arcos) | Mallas a mano o librería externa |
| Glass sobre la escena | Material propio en `VisualElement` con **UI Shader Graph** (nuevo en 6.3, solo URP) | Material en `Image` |
| Texto | TextCore SDF, fuentes dinámicas | TextMeshPro |
| Costo | Retenido, batching agresivo; hay que cuidar la cantidad de elementos en gama baja | Rebuild de canvas costoso con cambios frecuentes |

Se elige **UI Toolkit para toda la UI 2D**. uGUI no se usa para pantallas.

### 7.2 Capas del shell (un solo `UIDocument`, `PanelSettings` en *Screen Space Overlay*)

| Orden | Capa | Contenido |
|---|---|---|
| 0 | `TabContent` | Las 4/5 pantallas de pestaña (§8) |
| 1 | `TabBar` | Barra inferior tipo vidrio |
| 2 | `PageStack` | Páginas apiladas (editor de rutina, detalle de sesión, entrenamiento inmersivo). Tapan la barra, como el `Navigator` del legacy |
| 3 | `DialogLayer` | Diálogos modales con scrim |
| 4 | `ToastLayer` | Avisos breves (equivalente a `SnackBar`) |
| 5 | `SimulatorLayer` | Cinta "SIMULADOR" y panel 🛠 (solo con `ROWMATE_SIMULATOR`) |

### 7.3 Escala y área segura

- Unidad de diseño = **dp lógico del legacy** (así los anchos de los tests 640×360 y 800×360 siguen teniendo sentido): **1 px de USS = 1 dp**, como pide el Designer (ART_BIBLE §2.4). Móvil: `PanelSettings.scaleMode = ConstantPhysicalSize`, `referenceDpi = 160` (1 dp = 1/160"). Escritorio: `ConstantPixelSize` con `scale = Screen.dpi / 96` (factor de escala del sistema; `Screen.dpi` de escritorio no es confiable para tamaño físico). Lo aplica `PanelScaleController` y se recalcula al cambiar de monitor o DPI.
- `Screen.safeArea` se aplica como padding del raíz (muescas, barra de gestos).
- Teclado en móvil (editor de rutina): `KeyboardAvoider` desplaza el contenido según `TouchScreenKeyboard.area`, equivalente a `adjustResize` (F-APP-15.4). Se verifica en la It. 3.

### 7.4 HUD glass sobre la escena 3D

1. `HudBlurFeature` (Renderer Feature de URP con la API Render Graph de URP 17): después del post-proceso copia el color de la cámara de la escena a **1/4** de resolución, aplica **3 pasadas Kawase** y publica el resultado como textura global **`_RmHudBlur`** (nombre y pasadas del Designer, ART_BIBLE §3.2; costo ≤ 0,6 ms en el nivel Medio, §20).
2. **La textura tiene que llegar viva a la UI** (QA-P-16). La UI va en *Screen Space Overlay* y se dibuja después de la cámara de la escena, fuera del grafo de esa cámara: una textura transitoria de Render Graph ya no existiría. Por eso el destino es un `RTHandle` **persistente** que crea y libera el feature (se realoca solo si cambia la resolución), importado al grafo con `renderGraph.ImportTexture(...)`; la última pasada publica la global con `builder.SetGlobalTextureAfterPass(handle, _RmHudBlur)`. Prototipo y medición en la **It. 6b** (P-04): si el orden no se sostiene en algún backend, la pasada se mueve a `RenderPassEvent.AfterRendering` de la última cámara y se publica con `Shader.SetGlobalTexture` desde `RenderPipelineManager.endCameraRendering`.
3. Las tarjetas del HUD usan un material de **UI Shader Graph** (`RowMate/UI/Glass`, en `Art/Shaders/UI/`) que muestrea `_RmHudBlur` en coordenadas de pantalla y aplica el tinte con alfa adaptativo, el borde y el brillo del Designer.
4. **Respaldo** (nivel Bajo, It. 4a a 6a, o si el feature no está disponible): vidrio plano sin desenfoque con el borde y el radio del legacy, con el tinte del token `glass.fallback` de `Tokens` (ART_BIBLE §3.2: hoy **`#070D16 @86 %`** fijo, el alfa máximo del vidrio adaptativo, porque `@80 %` no garantiza D-ART-01 sobre un cielo blanco; reemplaza al `#000000 @55 %` del legacy). El test de contraste de §7.7 lo cubre. Lo decide el nivel de calidad (§20).
5. El blur se calcula solo mientras haya un elemento de vidrio visible sobre la escena.
6. La barra de pestañas **no** lleva blur (no hay escena detrás y UI Toolkit no desenfoca lo que queda debajo): vidrio por translucidez, como corrige QA-P-22 para el Designer.

### 7.5 Gráficos del historial

`MetricChartElement : VisualElement` dibuja con `Painter2D`: serie, bandas por tipo de paso, separadores, líneas objetivo punteadas (segmentos), ejes y tooltip. La geometría (escalas, inversión del split, bandas, objetivos, Q-24) la calcula `Core.History.ChartGeometry`, pura y testeada en `dotnet`. La animación de entrada es una interpolación del recorte en la vista (sin cambiar datos).

### 7.6 Texto y tipografía

Fuentes OFL elegidas por el Designer (Inter, Barlow Semi Condensed, JetBrains Mono; en `UI/Fonts/`), importadas como `FontAsset` dinámico. **Cifras tabulares**: en el HUD y el tablero las garantiza `TabularNumber` (§7.8), sin depender de la fuente; en listas y ejes, el derivado "RowMate Numeric" con `tnum` congelado (ASSET_SLOTS §3). Todo número visible se formatea con `Fmt` (§18) y los textos salen del catálogo (§12).

### 7.7 Tokens de diseño: una sola fuente (QA-P-05, ADR 016)

- **Fuente única**: `Core/Design/Tokens.cs` (puro) con todos los tokens de ART_BIBLE §2 y ANIMATION_BIBLE §1: colores (hex base del legacy y variantes `*.hud`), alfas, radios, espaciados, tamaños de tipografía y duraciones/curvas de movimiento. Cada token tiene su nombre estable (`metric.watts`, `step.work`, `metric.spm.hud`, `r.glass`, `dur.base`…).
- **Consumidores**: `StepTypeInfo` y `MetricPalette` (Core/Models, F-RUT-01 y F-APP-06, se cierran en la It. 1) leen sus hex de `Tokens`; no hay hex sueltos en otro archivo. La escena, los gráficos y `TabularNumber` usan `Tokens` directo.
- **USS generado**: `tools/tokens` (consola `dotnet` que referencia `RowMate.Core`) escribe `Presentation/Styles/Tokens.uss` con variables `--rm-*` y la marca "GENERADO, no editar". Un test de `dotnet` regenera en memoria y compara con el archivo versionado: si alguien cambia `Tokens.cs` sin regenerar (o edita el USS a mano), falla.
- **Contraste**: `Core/Design/Contrast` (luminancia relativa y relación WCAG 2.x, composición alfa) y un test de `dotnet` que verifica ART_BIBLE §10.1 contra el **peor fondo del vidrio**: `#070D16 @86 %` sobre blanco = `#2A2F37` (L = 0,028, verificado por QA): ≥ 7:1 en blanco y métricas no rojas, ≥ 4,5:1 en la familia roja (D-ART-01), ≥ 4,5:1 en el texto de interfaz sobre `surface.card`. Un cambio de token que rompa un umbral rompe el build.
- Lo que se cierra en la It. 1 es el contrato de datos (hex del legacy, F-RUT-01); el tema completo y el USS se usan desde la It. 2.

### 7.8 `TabularNumber`: un solo elemento por número (QA-P-08)

El Designer pide celdas de ancho fijo por dígito con rodillo (ART_BIBLE §4.2, ANIMATION_BIBLE §2.4). Armado con un `Label` por celda, el HUD (unas 9 cifras más los paneles, 40-80 celdas, el doble durante el rodillo) rompería el tope de ≤ 60 elementos visibles (§20). Diseño:

- `TabularNumber : VisualElement` **es un solo elemento** por número. Dibuja sus glifos en `generateVisualContent` con `MeshGenerationContext.DrawText` (Unity 6), un carácter por celda: ancho de celda = avance máximo de los dígitos 0-9 de esa fuente y tamaño (leído del `FontAsset` una vez); `:` y `.` en celdas angostas de 0,32 em.
- El texto llega como `ReadOnlySpan<char>` desde `Fmt.TryFormatFixed`/`WorkoutFormats` a un *buffer* propio de `char`: los glifos salen de cadenas de un carácter precalculadas ("0"…"9", ":", ".", "-"), así que actualizar un valor **no reserva memoria**.
- La animación (dígitos que cambian, `DigitRoll` de Core) mueve cada glifo con un desplazamiento vertical y un alfa por celda; mientras dura (180 ms) el elemento pide `MarkDirtyRepaint()` por cuadro, y solo se regenera la malla de ese elemento (sin *layout*: su tamaño no cambia). El recorte lo da el contenedor con `overflow: hidden`.
- Si `DrawText` no sirve en 6000.3 (API no verificada sin Editor, RA-18), el respaldo es el mismo elemento dibujando *quads* del atlas SDF del `FontAsset` con `MeshGenerationContext.Allocate` y el material de texto.
- Verificación: test PlayMode del HUD completo que cuenta elementos visibles (≤ 60) y mide 0 B/cuadro con el `Profiler` (`GC.Alloc`) durante 10 s de valores cambiando a 1 Hz con rodillos (It. 2 para el tablero, It. 4a para el HUD).

---

## 8. Navegación: 5 pestañas que preservan estado

- `AppShellController` construye las pestañas **una sola vez**: Dispositivo, Workout, Rutinas, Historial y, solo si `StravaConfig.IsConfigured`, Perfil (F-APP-02). La pestaña inicial es Dispositivo.
- Cambiar de pestaña pone `style.display = None` en la anterior y `Flex` en la nueva: los elementos, el scroll y el estado de cada pantalla **se conservan** (equivalente a `IndexedStack`). Las pantallas ocultas siguen suscriptas a sus stores (el estado no se pierde) pero no se dibujan.
- Tocar Historial dispara `HistoryStore.Reload()` (igual que `main.dart:147`), sin parpadeo (Q-22): la lista sigue visible mientras recarga.
- `PageStack.Push(page)` / `Pop()` para pantallas apiladas; cada página implementa `IPage` (`OnPushed`, `OnPopped`, `OnBackRequested`). El entrenamiento inmersivo es una página que además pide `ISceneHost.Enter()` / `Exit()` y `ISystemUi.SetImmersive(bool)`.
- Botón "atrás" (Android, Escape en escritorio) vía Input System: cierra el diálogo superior; si no hay, `PageStack.OnBackRequested()`; en la raíz, Android manda la app al fondo (`moveTaskToBack`), como Flutter.
- Diálogos: `DialogService.ShowAsync<TResult>(DialogSpec)` devuelve `Task<TResult>`; el diálogo "preguntar" de Strava queda abierto hasta que el usuario responde (Q-53).

---

## 9. Persistencia

### 9.1 SQLite

| Ítem | Decisión |
|---|---|
| Librería | `com.gilzoide.sqlite-net` 1.3.3 (sqlite-net 1.9.172 + SQLite 3.53.4) en Unity; `sqlite-net-pcl` 1.9.172 (misma API pública, `namespace SQLite`) en `dotnet` |
| Por qué | Binarios nativos ya compilados para las 4 plataformas, compatible con IL2CPP (los atributos de columna heredan `PreserveAttribute`), MIT, sin plugins pagos; y el mismo código de repositorio compila y se testea en `dotnet` |
| Plataformas | Android (arm64/armv7/x86_64 `.so`), iOS (estática, `__Internal`), macOS (universal), Windows (x64/arm64 `.dll`). En Android no se puede usar el SQLite del sistema (es privado desde Android 7): por eso un paquete con su propio binario |
| Archivo | `Path.Combine(Application.persistentDataPath, dbFile)`; `dbFile` = `rower_app.db` o `rower_app_dev.db` (simulador, F-HIS-20). La ruta se resuelve en el hilo principal y se pasa a Infrastructure |
| Hilo | Una conexión sincrónica, usada solo desde el `SerialExecutor` (§6) |
| Pragmas | `journal_mode=WAL`, `synchronous=NORMAL`, `foreign_keys=OFF` (paridad: en el legacy nunca se activan, DC-06) |
| Versión | `PRAGMA user_version` (lo mismo que usa `sqflite`) |
| Mapeo | DTOs de fila con `[Table]`/`[Column("snake_case")]` + `link.xml` que preserva `RowMate.Infrastructure`. El DDL se ejecuta con el texto exacto del legacy (no `CreateTable<T>`, que generaría otro esquema) |

### 9.2 Esquema

Esquema = **v7 del legacy** (`database_service.dart:75-139`, mismas tablas, columnas, tipos y defaults) **+ dos cambios invisibles**, con `user_version = 8`:

1. `workout_sessions.origin TEXT NOT NULL DEFAULT 'app'` (`'app'` | `'strava'`). Hace falta para Q-71 ("Limpiar sesiones de Strava" borra **solo las importadas**): en el esquema legacy no hay forma de distinguir una sesión importada de una grabada y subida (ambas tienen `strava_activity_id`). Se agrega desde la It. 3 para no migrar después.
2. `CREATE INDEX idx_data_points_session ON data_points(session_id, elapsed_seconds)` (rendimiento del historial; no cambia resultados).

Fechas: `started_at`/`finished_at` en ISO-8601 UTC con `IsoDate.Format`, que emula `toUtc().toIso8601String()` de Dart: milisegundos siempre y microsegundos **solo si no son 0** (oráculo: `2026-10-27T09:37:38.628537Z`, `…38.628Z`, `…38.000Z`; ADR 012). `routines.created_at` también en UTC con `Z` (Q-18: el orden "la más nueva arriba" se mantiene). El lector (`IsoDate.Parse`) acepta de 0 a 7 decimales, con `Z`, con offset o sin zona (formato local del legacy, `routine.dart:111`). Al borrar una rutina se borran también sus pasos (Q-17, sin efecto visible).

Las migraciones v1→v7 del legacy (`_onUpgrade`) se portan **solo** si el PO pide importar la base legacy (§9.3); la base nueva nace directamente en v8.

### 9.3 Importación de la base legacy (`rower_app.db`)

Por defecto **no** se importa (Q-11, "Confirmar = Sí" en la It. 3). La arquitectura lo deja preparado, sin costo:
- `LegacyDatabaseImporter` (Infrastructure, puro): abre un `rower_app.db` de versión 1-7 en solo lectura, aplica sobre una copia las mismas migraciones del legacy hasta v7 (incluido el borrado de sesiones de la v4) y agrega v8 marcando `origin = 'strava'` las sesiones que parecen importadas (heurística a validar por el PO: tienen `strava_activity_id` y sus puntos están a ~1 s entre sí, mientras que las grabadas en la app van cada 5 s, DC-48) y `'app'` el resto.
- Acceso al archivo: en escritorio, selector de archivo; en iOS solo si la app Unity se instalara como actualización con el mismo bundle id `com.iguisoft.rowmate` (el sandbox se conserva), lo que R-14 prohíbe hasta que Q-85 lo decida en la It. 14 (los builds de Apple usan `com.iguisoft.rowmate.unity`, §16); en Android no es posible si cambia el application id (`com.example.rower_app`). R-14: la app nueva nunca abre ni modifica la base del legacy en uso; el importador solo trabaja sobre una **copia** elegida por el usuario.
- R-11: el `external_id` de Strava de la app nueva usa otro prefijo (§11.5), así que una sesión nueva nunca choca con una legacy.

### 9.4 Preferencias (mismas claves que el legacy)

| Clave | Tipo | Default | Dueño |
|---|---|---|---|
| `hrm.deviceId` | string | — | HRM (It. 11) |
| `hrm.deviceName` | string | — | HRM (It. 11) |
| `scene.environment` | string (`lake`/`river`/`coast`/`regatta`) | `lake` | SCN (It. 8) |
| `immersive.lapsExpanded` | bool | `false` | HUD (It. 5) |
| `immersive.repsExpanded` | bool | `false` | HUD (It. 5) |
| `strava_upload_pref` | string (`auto`/`ask`/`manual`) | `auto` | PRF (It. 12) |
| `app.installId` (nueva) | string (8 hex) | se genera | Strava `external_id` (It. 12) |

- Implementación: `JsonFilePreferences : IPreferences` (Infrastructure) sobre `persistentDataPath/prefs.json`, con escritura atómica (archivo temporal + reemplazo) y escritura inmediata en cada `Set`/`Remove` (como `SharedPreferences`). Se descartó `PlayerPrefs` porque guarda en el registro en Windows, no se puede testear en `dotnet` y difiere los guardados.
- La interfaz distingue "no existe" de "vacío" (`TryGetString`), como `prefs.getString(...) ?? default`.
- El simulador comparte el archivo de preferencias con el build normal (igual que el legacy: misma app, base distinta).

### 9.5 Almacenamiento seguro (tokens de Strava)

`ISecureStore` (Core) con las **mismas claves** del legacy: `strava_access_token`, `strava_refresh_token`, `strava_expires_at`, `strava_athlete_name`, `strava_athlete_avatar`.

| Plataforma | Implementación |
|---|---|
| Android | Kotlin: clave AES-GCM en Android Keystore; el texto cifrado va a `SharedPreferences` privadas |
| iOS | Swift: Keychain (`kSecClassGenericPassword`, servicio `rowmate.strava`, `kSecAttrAccessibleAfterFirstUnlock`) |
| macOS | Swift: Keychain clásico (sin *data protection keychain*, como `useDataProtectionKeyChain: false` del legacy) |
| Windows | DPAPI (`CryptProtectData`, ámbito usuario) por P/Invoke a `crypt32.dll`; archivo en `persistentDataPath` |
| Editor | La implementación de la plataforma anfitriona |

---

## 10. Bluetooth LE

### 10.1 Capas

```
UI (Device, HUD)  →  App: DeviceStore, HeartRateStore, WorkoutEngine
                         │ IRowerService / IHeartRateService   (Core; los implementa también el simulador)
                         ▼
Core: RowerService (puerto de ble_service.dart)   HeartRateService (puerto de heart_rate_service.dart)
      FtmsParser · HeartRateParser · ScanFilter · ReconnectLoop · StaleWatchdog · RowerWatchdog
                         │ IBleAdapter   (Core: GATT central mínimo, asíncrono, sin lógica de producto)
                         ▼
Platform: AndroidBleAdapter (Kotlin)  AppleBleAdapter (Swift, iOS+macOS)  WindowsBleAdapter (C++/WinRT)
                         │ buffer nativo (eventos con marca de tiempo) → NativeEventPump → hilo principal
```

La lógica de producto (tiempos, reintentos, handshake FTMS, generaciones, memoria del sensor, watchdogs) **queda en Core** y se testea en `dotnet` con `FakeBleAdapter` + `FakeScheduler`. Los adaptadores nativos solo traducen el GATT de cada sistema.

### 10.2 Interfaz en Core

```csharp
public enum BleAdapterState { Unknown, Unsupported, Unauthorized, Off, TurningOn, On, TurningOff }

public readonly struct BleUuid : IEquatable<BleUuid>   // normaliza "180D", "0000180d-…", Guid
{
    public static BleUuid FromShort(ushort value);
    public static BleUuid Parse(string text);
}

public sealed record BleScanResult(string DeviceId, string? PlatformName, string? AdvName,
                                   int Rssi, IReadOnlyList<BleUuid> ServiceUuids);

public interface IBleAdapter : IDisposable
{
    bool IsSupported { get; }
    IReadOnlyObservable<BleAdapterState> State { get; }
    bool IsScanning { get; }
    EventStream<BleScanResult> ScanResults { get; }
    EventStream<int> ScanFailed { get; }                          // código nativo

    Task StartScanAsync(IReadOnlyList<BleUuid> osServiceFilter);  // reemplaza el escaneo en curso (como FBP)
    Task StopScanAsync();
    Task RequestEnableAsync();                                    // Android: pide encender; otros: abre ajustes (Q-32)

    Task<IBlePeripheral> ConnectAsync(string deviceId, bool requestHighMtu);  // por id, sin escanear
}

public interface IBlePeripheral
{
    string DeviceId { get; }
    string? PlatformName { get; }
    IReadOnlyObservable<bool> IsConnected { get; }
    Task<IReadOnlyList<BleService>> DiscoverServicesAsync();      // servicios + características + propiedades
    Task SetNotifyAsync(BleUuid service, BleUuid characteristic, bool enable);
    Task WriteAsync(BleUuid service, BleUuid characteristic, byte[] data, bool withResponse);
    EventStream<BleNotification> Notifications { get; }
    Task DisconnectAsync(bool skipQueue);                         // skipQueue = disconnect(queue:false) de FBP

    // Segundo plano (§6.1): el plugin repite la escritura mientras Unity está pausado. Core decide cuándo.
    void StartNativeKeepAlive(BleUuid service, BleUuid characteristic, byte[] data, TimeSpan period);
    void StopNativeKeepAlive();
}

// Todo evento nativo trae la marca del reloj monótono nativo (la misma de IClock.MonotonicSeconds).
public readonly struct BleNotification
{
    public readonly BleUuid Characteristic; public readonly byte[] Value; public readonly double MonotonicSeconds;
}
```

**Contador de paladas (D-ANI-06).** `RowerService` expone además `EventStream<StrokeCountIncrement>` (Core/Workout): se emite cuando el `strokeCount` FTMS de `RowingData` sube (con vuelta de `uint16` en 65 535), con la marca de tiempo del paquete. Lo usa la sincronía fina de la palada visual (ANIMATION_BIBLE §3.10), **apagada** hasta verificar en la It. 10a con el AMS-670B en qué fase incrementa. No cambia ningún dato.

Los timeouts (15 s de conexión manual, 10 s de reconexión, 10 s de búsqueda, 5 s de espera del adaptador) los aplica **Core** con `IScheduler`, con los mismos valores del legacy; al vencer, Core llama a `DisconnectAsync(skipQueue: true)`.

### 10.3 Comportamientos del legacy que se conservan

- **Filtro de búsqueda (F-BLE-03)**: Android, iOS y macOS reciben el UUID de servicio como filtro del sistema y **no** se filtra en la app (en iOS/macOS un servicio puede venir en los *overflow UUIDs*). Windows escanea sin filtro y `ScanFilter.Accept(result, service, clientSide: platform == Windows)` descarta en la app. Ambos servicios (remo y pulsómetro) pasan por `ScanFilter`.
- **Dos conexiones GATT simultáneas**: el adaptador mantiene un `IBlePeripheral` por `DeviceId`; remo y pulsómetro son independientes.
- **Un solo escaneo a la vez**: `StartScanAsync` reemplaza el anterior (como FBP); `RowerService` y `HeartRateService` solo detienen el escaneo si es el suyo, y la UI deshabilita una búsqueda mientras corre la otra (F-BLE-05, `heart_rate_card_test:121`).
- **Suscripción después de iniciar el escaneo**: el adaptador no reemite resultados cacheados de un escaneo anterior.
- **Remo (F-BLE-06)**: descubrir servicios, suscribirse a **todas** las características con notify/indicate (diagnóstico), parsear `0x2AD1`, handshake en `0x2AD9`: `0x00` → 300 ms → `0x07`, keepalive `0x07` cada 2 s; reconexión a los 3 s (Q-29: reintenta hasta reconectar o hasta que el usuario actúe); watchdog de monitor apagado ≥ 5 s.
- **MTU**: el remo pide MTU alto en Android (FBP pide 512 por defecto al conectar; sin eso los paquetes FTMS de más de 20 bytes se cortarían). El pulsómetro **no** pide MTU (`mtu: null`, algunos relojes lo rechazan).
- **Pulsómetro**: generaciones de conexión, `ReconnectLoop` (6 intentos cada 10 s al arrancar; cada 3 s sin límite tras una caída), `StaleWatchdog` de 10 s, memoria `hrm.deviceId`/`hrm.deviceName`, validación del servicio `0x180D` (`HrmIncompatibleException`).
- **Conexión por id recordado**: Android `getRemoteDevice(address)`, Apple `retrievePeripherals(withIdentifiers:)`, Windows `BluetoothLEDevice.FromBluetoothAddressAsync`. El id es opaco (MAC en Android/Windows, UUID por app en Apple), igual que `remoteId` de FBP.

### 10.4 Puente nativo por plataforma

API nativa común (C ABI en Apple y Windows; métodos con los mismos nombres en el objeto Kotlin):

```
rm_ble_init()                           rm_ble_adapter_state() -> int        rm_ble_request_enable()
rm_ble_start_scan(uuidsCsv, osFilter)   rm_ble_stop_scan()
rm_ble_connect(deviceId, highMtu) -> reqId      rm_ble_disconnect(deviceId, skipQueue)
rm_ble_discover(deviceId) -> reqId      rm_ble_set_notify(deviceId, svc, chr, on) -> reqId
rm_ble_write(deviceId, svc, chr, bytes, len, withResponse) -> reqId
rm_ble_keepalive_start(deviceId, svc, chr, bytes, len, periodMs)   rm_ble_keepalive_stop(deviceId)
rm_clock_monotonic() -> double          rm_ble_drain(buffer, capacity) -> bytesWritten
eventos (en el buffer nativo, cada uno con su marca monótona):
           adapter_state(state) · scan_result(id, name, advName, rssi, uuidsCsv) · scan_failed(code)
           connection(id, connected, code) · request_done(reqId, status, payloadJson) · notify(id, chr, bytes, len)
```

**Modelo de entrega: *pull*.** Los plugins no llaman a C#: guardan cada evento en un *buffer* circular nativo (con tope, §6.1) y `NativeEventPump` (Platform) lo drena al principio de cada frame con `rm_ble_drain` en un arreglo reutilizado (sin asignaciones). Ventajas: los eventos sobreviven con Unity pausado (segundo plano), no hay que entrar a código administrado desde hilos nativos (`AndroidJavaProxy` con el *player* pausado, punteros a función en IL2CPP) y se agrupan las llamadas JNI (una por frame en Android).

| Plataforma | Implementación | Hilos y permisos |
|---|---|---|
| Android | Kotlin en `Assets/Plugins/Android/RowMateNative/` (Unity 6 compila fuentes Kotlin en el build Gradle, sin `.aar` aparte). `BluetoothLeScanner` + `ScanFilter` por servicio, `connectGatt(…, autoConnect=false, TRANSPORT_LE)`, **cola de operaciones GATT por dispositivo** (Android admite una a la vez), `requestMtu(512)` si `highMtu`. Keepalive nativo con `Handler` (§6.1). C# drena el *buffer* con `AndroidJavaClass` una vez por frame. Entrada de aplicación: *Activity* clásica (máxima compatibilidad de plugins) | Callbacks del sistema en hilos binder → *buffer* nativo. Permisos en tiempo de ejecución con `UnityEngine.Android.Permission`: API ≥ 31 `BLUETOOTH_SCAN` (`neverForLocation`) y `BLUETOOTH_CONNECT`; API ≤ 30 `ACCESS_FINE_LOCATION`. Rechazo → `Unauthorized` (vista `deviceBtDenied`). Manifest propio: permisos, `uses-feature bluetooth_le required=true`, `INTERNET`, `WAKE_LOCK`, intent-filter `rowmate://` |
| iOS | Swift en `Assets/Plugins/iOS/RowMateNative/` (Xcode lo compila; funciones `@_cdecl`), P/Invoke a `__Internal`. `CBCentralManager` con cola serie propia, creado al arrancar (el diálogo de permiso aparece al abrir la app, como el legacy, Q-87) | Callbacks en la cola de CoreBluetooth → *buffer* nativo (sigue recibiendo en segundo plano con `bluetooth-central`; el keepalive se dispara también desde las notificaciones, §6.1). Post-proceso de Xcode: `NSBluetoothAlwaysUsageDescription` y `NSBluetoothPeripheralUsageDescription` localizados es/en (Q-89), `UIBackgroundModes = bluetooth-central` (R-07) |
| macOS | Las **mismas fuentes Swift**, compiladas como `RowMateNative.bundle` universal por `native/macos` (requiere Mac) | Entitlements `com.apple.security.device.bluetooth` y `network.client`; `NSBluetoothAlwaysUsageDescription` en el `Info.plist` del `.app` (post-proceso) |
| Windows | DLL C++/WinRT `native/windows` → `RowMateBle.dll` (x86_64). `BluetoothLEAdvertisementWatcher` (escaneo activo, sin filtro: lo filtra Core), `BluetoothLEDevice`, `GattSession.MaintainConnection`, `GetGattServicesAsync(Uncached)`, `ValueChanged`; estado del adaptador con `BluetoothAdapter`/`Radio` | Callbacks en el *thread pool* de WinRT → *buffer* nativo. Sin permisos declarados. Funciona en el Editor de Windows (se puede probar el pulsómetro en la PC, como el legacy) |

Reglas del puente: el plugin **copia** los bytes al *buffer* antes de volver del callback del sistema; nunca deja pasar excepciones; el *buffer* tiene tope (descarta primero los eventos más viejos y lo registra); los `reqId` se resuelven con `TaskCompletionSource` en el hilo principal. En Android, `rm_*` son métodos estáticos del objeto Kotlin llamados con `AndroidJavaClass`; en Apple y Windows, funciones C por P/Invoke.

**Segundo plano**: ver §6.1 y ADR 017 (keepalive nativo, eventos con marca de tiempo y alcance al volver). La afirmación anterior ("igual que los timers de Dart suspendidos") era incorrecta (QA-P-06).

---

## 11. Strava

### 11.1 Disponibilidad y credenciales (F-STR-01, R-02)

- `StravaCredentials` (Bootstrap) es `static partial` con `static partial void Load(ref string id, ref string secret);`. La implementación vive en `Bootstrap/Generated/StravaSecrets.g.cs`, que **genera el build** desde las variables `STRAVA_CLIENT_ID` y `STRAVA_CLIENT_SECRET` (`tools/strava/write-secrets.ps1` en local, paso de CI en los workflows). El archivo está en `.gitignore`. Si no existe, el método parcial no se compila y las credenciales quedan vacías.
- `StravaConfig.IsConfigured(id, secret)` (Core): falso si están vacías o si el secreto es un marcador de ejemplo (`TU_CLIENT_SECRET`, `YOUR_SECRET`, `YOUR_CLIENT_SECRET`, Q-84). Sin Strava, 4 pestañas.
- Nadie lee `lib/core/strava/strava_secrets.dart`.

### 11.2 OAuth por plataforma (F-STR-02, F-APP-17, Q-80)

| Plataforma | Mecanismo | `redirect_uri` |
|---|---|---|
| iOS | `ASWebAuthenticationSession` (Swift), `prefersEphemeralWebBrowserSession = false` (como el legacy), esquema de callback `rowmate` | `rowmate://rowmate.app/callback` |
| macOS | `ASWebAuthenticationSession` (macOS 10.15+). Resuelve DC-66 sin registrar el esquema | `rowmate://rowmate.app/callback` |
| Android | `Application.OpenURL` (navegador) + intent-filter `rowmate://rowmate.app/callback` en la actividad (`singleTask`) → `Application.deepLinkActivated` / `Application.absoluteURL` | `rowmate://rowmate.app/callback` |
| Windows y Editor | Servidor *loopback* mínimo sobre `TcpListener` en `127.0.0.1:<puerto libre>` (no `HttpListener`, por compatibilidad con IL2CPP) + `Application.OpenURL`. Unity no soporta deep links en Windows Standalone; Strava acepta `localhost`/`127.0.0.1` sin configurarlo | `http://127.0.0.1:<puerto>/callback` |

- Se agrega el parámetro `state` aleatorio (anti-CSRF) y se valida al volver; no cambia nada visible.
- Intercambio y renovación (`/oauth/token`), margen de renovación `expires_at − 60 s` (F-STR-03, regla pura `StravaTokenPolicy`), logout que borra las 5 claves: en `Infrastructure.StravaAuthClient`, testeado con `FakeHttpClient`.
- Los textos de error de Strava pasan al catálogo (Q-01).

### 11.3 HTTP

`IHttpClient` (Core) con requests simples y multipart (`HttpRequestSpec`, `HttpResponseSpec`). Implementación `UnityWebRequestHttpClient` (Platform): TLS del sistema en las 4 plataformas, sin `HttpClient` de .NET (evita problemas de certificados y de stripping en IL2CPP). El avatar se baja con `UnityWebRequestTexture` en Presentation. Android: Internet Access = *Require* (Q-83).

### 11.4 TCX, metadatos e importación

Puros en Core (`TcxBuilder`, `StravaMetadata`, `ImportedTelemetryBuilder`, `SessionRepair`): misma salida byte a byte que el legacy salvo las correcciones aprobadas (Q-72, Q-73, Q-75). JSON de Strava leído con `JToken` (Newtonsoft) en Infrastructure.

### 11.5 Simulador y duplicados

- Subidas bloqueadas en todo camino (R-01, F-STR-18): `StravaApiClient.UploadAsync` corta antes de cualquier request si `BuildFlags.IsSimulator`; la UI oculta los botones (Q-78). Login e importación funcionan.
- `external_id` nuevo: `rowmate-u_{installId}_{sessionId}` (y el `.tcx` con el mismo nombre), así una sesión nueva nunca coincide con un `rowmate_<id>` del legacy (R-11). La lectura de `duplicate of activity N` se conserva (F-STR-08).
- El secreto del cliente queda embebido en el binario, igual que en el legacy (Strava no ofrece PKCE para apps públicas). Riesgo aceptado (§21).

---

## 12. Localización y formatos

- Catálogos `Assets/RowMate/Localization/strings.es.json` y `strings.en.json`: **mismas 159 claves y placeholders** que `app_es.arb`/`app_en.arb` (incluidos `@clave` con metadatos), más las claves nuevas de Q-01 que agregue cada iteración. Se generan una vez desde los `.arb` en la It. 1 (`tools/l10n`). Los textos en español quedan idénticos al legacy (salvo Q-02).
- `Localizer` (Core): carga los JSON (texto pasado por el composition root desde `TextAsset`; en tests, leído de disco), resuelve `Get(key)` y `Format(key, args)` con un `MessageFormatter` ICU mínimo: `{name}` y `{count, plural, =0{…} one{…} other{…}}` (para los plurales de Q-02). Sin claves faltantes en tiempo de ejecución: un test exige que es y en tengan el mismo conjunto de claves y de parámetros.
- Elección del idioma (F-APP-09, QA-P-20): Flutter recibe la **lista** de idiomas preferidos del sistema y elige el primero que la app soporta (`main.dart:103-105`, `supportedLocales: [en, es]`, resolución por defecto de Flutter); si ninguno coincide, el primero de la lista soportada (`en`). Ejemplo: sistema `[fr-FR, es-AR]` → `es`. `Application.systemLanguage` de Unity da solo el principal (`French` → `en`): no alcanza. Diseño:
  - `LocaleResolver.Resolve(IReadOnlyList<string> preferredTags)` (Core, It. 1): es el algoritmo `basicLocaleListResolution` de Flutter 3.47.6 (`widgets/app.dart:146-238`) con `supportedLocales = [en, es]` (D-06 de la It. 1), no "el primero soportado": una coincidencia perfecta (`es`/`en` sin región ni escritura) gana sobre una coincidencia solo por idioma que apareció antes (`[fr-FR, es-AR, en]` → `en`). Lista nula o vacía → `en`. Las 17 filas de AC-APP-09-02 se testean en `dotnet`.
  - `ISystemLocale.PreferredLanguageTags` (Platform): Android `LocaleList.getDefault().toLanguageTags()` por JNI desde C# (sin Kotlin); Windows `GetUserPreferredUILanguages` por P/Invoke a `kernel32`; iOS y macOS `Locale.preferredLanguages` en Swift (It. 10b; hasta entonces, respaldo con `Application.systemLanguage`, criterio [Apple]); Editor: la del sistema anfitrión.
  - Se vuelve a leer al recuperar el foco (cambio de idioma con la app abierta) y la UI se redibuja.
- Textos de desarrollo (panel del simulador, diagnóstico BLE) quedan fijos (Q-03, Q-04), en constantes dentro de su assembly.
- **Formatos** fijos del legacy (Q-06): punto decimal, miles con punto solo en paneles, `HH:mm`, `dd/MM/yyyy  HH:mm`. Todo pasa por `Fmt` y `WorkoutFormats` (§18) con cultura invariante.
- Se descartó `com.unity.localization` (ADR 011): exige crear tablas en el Editor (no hay Editor), no se puede testear el catálogo en `dotnet`, suma Addressables y la It. 1 necesita verificar el catálogo fuera de Unity. Si 6.7 LTS trae localización integrada adecuada, se reevalúa en la It. 14.

---

## 13. Wakelock, ciclo de vida y sistema

| Tema | Decisión |
|---|---|
| Pantalla siempre encendida (F-APP-14, Q-56) | `IWakeLock.Enable()` al arrancar y de nuevo en cada `OnApplicationFocus(true)`/`OnApplicationPause(false)`. Android/iOS: `Screen.sleepTimeout = SleepTimeout.NeverSleep`. Windows: `SetThreadExecutionState(ES_CONTINUOUS \| ES_DISPLAY_REQUIRED \| ES_SYSTEM_REQUIRED)` por P/Invoke. macOS: `IOPMAssertionCreateWithName(kIOPMAssertionTypeNoDisplaySleep)` en el bundle Swift |
| Ciclo de vida | `IAppLifecycle` (puerto en Core, implementación en Platform) expone `Paused`, `Resumed`, `FocusChanged`, `Quitting`. Al salir se cierran conexiones y la base. Segundo plano: §6.1. Escritorio: `runInBackground = true` |
| Modo inmersivo (F-APP-15) | `ISystemUi.SetImmersive(bool)`: Android oculta barras con modo inmersivo *sticky* (helper Kotlin, con *edge-to-edge*); iOS oculta la barra de estado con el helper Swift (`prefersStatusBarHidden` + `setNeedsStatusBarAppearanceUpdate`); escritorio: sin efecto (paridad) |
| Orientación | `Screen.orientation = AutoRotation`; iPhone sin vertical invertida; iPad las 4; Android sin reiniciar al rotar (Unity no recrea la actividad) |
| Ventanas de escritorio | Windows 1280×720 redimensionable, macOS 800×600 (`PlayerSettings` + resolución inicial en `AppRoot`), título "RowMate" (Q-85) |
| Frecuencia | `FramePacing` (§13.1): 60 fps en el inmersivo y **mientras algo se anima**; 30 fps solo en reposo |
| Reducir movimiento | `IAccessibilitySettings.ReduceMotion` (§13.2) |

### 13.1 Frecuencia de cuadros (QA-P-07)

El Designer pide animaciones de UI "super suaves" a 60 fps (ANIMATION_BIBLE §0.1, principio 5): cambio de pestaña, rodillo de dígitos, corazón que late. 30 fps fijos fuera del inmersivo las cortaría a la mitad. Diseño:

- `Application.targetFrameRate = 60` siempre (el *loop* y la entrada corren a 60). El ahorro fuera del inmersivo se hace con `OnDemandRendering.renderFrameInterval`: **1** (60 fps) o **2** (30 fps).
- `FramePacing` (Presentation) lleva un contador de **demandas de animación**. Pide 60 mientras haya: una transición de pantalla, pestaña, diálogo o aviso en curso; un rodillo de `TabularNumber` activo; el corazón visible (tarjeta del pulsómetro en pantalla con pulso conectado); un desplazamiento con inercia o un dedo apoyado (y 500 ms después de soltar). Cada animación pide al empezar y libera al terminar (`using var _ = pacing.Request()`), sin asignar memoria.
- Sin demandas durante 1 s → 30 fps (reposo). El inmersivo pide 60 todo el tiempo. En iOS de 120 Hz se queda en 60 salvo pedido explícito del Designer.
- Escritorio igual. En segundo plano o sin foco: ver §6.1.
- Verificación: test PlayMode de la política (qué estado da 60 y cuál 30) en la It. 2 y medición en dispositivo en la It. 14.

### 13.2 Reducir movimiento (QA-P-21)

`IAccessibilitySettings` (puerto en Core, Platform) con `bool ReduceMotion` y el evento `Changed` (se vuelve a leer al recuperar el foco). Por defecto **no reducir**. Con `true`, Presentation y Scene apagan lo que pide ART_BIBLE §10.4: respiración de cámara, vuelos de entrada, pulsos (rodillo → fundido de 80 ms); la remada y el agua siguen (son el dato).

| Plataforma | Lectura | Iteración |
|---|---|---|
| Android | JNI desde C# (sin Kotlin): `Settings.Global.getFloat(resolver, "animator_duration_scale", 1) == 0` | 2 |
| Windows | `SystemParametersInfo(SPI_GETCLIENTAREAANIMATION)` por P/Invoke: `false` = reducir | 2 |
| iOS | Swift: `UIAccessibility.isReduceMotionEnabled` | 10b |
| macOS | Swift: `NSWorkspace.shared.accessibilityDisplayShouldReduceMotion` | 10b |
| Editor | Menú `RowMate/Simular reducir movimiento` (para probar) | 2 |

---

## 14. Escena 3D (marco para las It. 6a-8)

### 14.1 Marco general

- **Coordenadas "cinta" (origen flotante)**: el bote queda en `z = 0` y el mundo se mueve `−distanciaEscena`; los objetos de orilla se ubican en `zProp − distancia` (en `double` en Core, convertido a `float` relativo al bote). No hay pérdida de precisión en sesiones largas.
- **Escenas**: `World.unity` (agua, cielo, luz, bote, remero, cámara, director) y una escena aditiva por escenario (`Environments/Lake.unity`, `River`, `Coast`, `Regatta`) con terreno, vegetación y props. Se cargan con `LoadSceneAsync(Additive)` al entrar al inmersivo (o en precarga desde la pestaña Workout) y se descargan al salir. Fuera del inmersivo la cámara 3D se apaga; queda la cámara de UI.
- **Lógica pura en Core** (Q-69): `SceneCamera`, `SceneState` (velocidad `500/split`, τ = 1,5 s, dt acotado a 0,1 s), `StrokeCycle` + `StrokeRhythm` (ritmo por SPM, Q-70), `SceneTimeOfDay` (paleta legacy), generación determinista de orillas por segmento de 50 m, visibilidad y topes. **RNG**: `DartRandom` porta `math.Random(seed)` de la VM de Dart (BSD-3): el mismo `seed` da el mismo paisaje que el legacy (Q-61 revisada) y las mismas 80 estrellas (`Random(42)`, `sky_painter.dart:59`).
- **Unity solo dibuja** (assembly `RowMate.Scene`, no Presentation): `SceneDirector` (un único `Update` que llama a Core y aplica a transforms y materiales, sin asignaciones), `ChaseCameraRig`, `WaterSurface`, `WaterInteractionRenderer`, `SkyController`, `SunMoonLights`, `ShoreStreamer` (pool por `PropKind`), `BoatVisual`, `RowerRigDriver`, `OarRig`, `StrokeVfx`. Nombres unificados con ANIMATION_BIBLE §7.1.

### 14.2 Agua (pedido del Designer; Q-68, R-04)

- **Fases en `double` en la CPU**: `WaterParams` (Core) calcula por cuadro la fase de cada ola de Gerstner (`k·d − ω·t`, con la distancia de escena y el tiempo en `double`) **módulo 2π** y el desplazamiento de cada mapa de normal **módulo su período de repetición**, y los pasa al shader como `float` ya acotados. No hay salto cada 4096 m ni a ninguna distancia (Q-68), y el shader no recibe la distancia cruda.
- **Contrato legacy de `shaderDistance`** (QA-P-28): `SceneState.ShaderDistance` (= `distance % 4096`, `scene_state.dart:34`) se porta igual, con su test `scene_state_test:55`, como contrato del legacy. El agua nueva **no** lo usa; queda documentado en el código.
- **Textura de interacción sin estado**: `RenderTexture` RGBA8 de 256×200 texeles en coordenadas del bote (cubre el área de remos, estela y charcos), **redibujada entera cada cuadro** con *quads* instanciados (`Graphics.RenderMeshInstanced` / `CommandBuffer.DrawMeshInstanced`) desde las listas de Core: charcos (`PuddleField`), hervidero del drive, micro-anillos de gotas y estela. Canales: perturbación de normal (RG), espuma (B), calma (A). Como no acumula, no hay simulación que pueda divergir ni que dependa de la tasa de cuadros, y los charcos quedan "fijos en el mundo" porque Core ya los guarda en coordenadas de mundo.
- **Agua degradada obligatoria (R-04)**: el shader del agua avanzada declara un `SubShader` de respaldo (y `Fallback`) con el degradé de 3 tramos del legacy. `WaterQualitySelector` elige `Advanced` / `Degraded` por nivel de calidad (§20) y `Shader.isSupported`; si el avanzado falla en ejecución, cae al degradado sin cortar la sesión (F-SCN-13). El degradado llega en la **It. 6a** (QA-P-14); el avanzado, reflejos y estela en la 6b.

### 14.3 Remada, bote y luz (decisiones del Designer)

| Decisión | Diseño | Gate |
|---|---|---|
| D-ANI-01: fase visual | `Core/Scene/Stroke/VisualStrokeClock` (ANIMATION_BIBLE §3.1 y §7.2) sigue a `SceneState.strokePhase` con el seguidor suave y emite `StrokeEvents` (`Catch`, `BladeEntry`, `BladeExit`, `Finish`). **No toca** `SceneState` ni sus tests: los de `scene_state_test` y `stroke_cycle_test` se portan sin cambios sobre la fase del contrato | 7 (Architect: se adopta) |
| D-ART-04: medidas reales | `BoatSpec` con pernos a ±0,80 m, eje del remo a 0,31 m, remo de 2,88 m con 0,88 m de interior, manga 0,32 m, asiento a 0,14 m, recorrido 0,62 m. La geometría exagerada del legacy (remo 2,9 + 0,9 m, semimanga 0,28 m, `boat_painter.dart:28-30`, `:135`) se porta tal cual a `LegacyBoatGeometry` (Core), solo para `boat_painter_test` | 7 (Architect + PO; por defecto se adopta) |
| D-ANI-02: charcos | `PuddleField` crea los charcos en `BladeExit` (fase canónica 0,38 del reloj visual) en la **posición real de la punta** de cada pala (`ScullingKinematics`), con las reglas legacy: 2 por palada, izquierdo primero, 4 s, radio 0,3→0,9 m, fijos en el mundo, ninguno en pausa (ANIMATION_BIBLE §7.5). La lista de charcos de `SceneState` queda para sus tests (`scene_state_test:69` pasa igual a 24 spm) | 7 (Architect + PO; por defecto se adopta) |
| D-ANI-03: cabeceo común | Jerarquía de transforms: `BoatPitchPivot` (cabeceo, arfada, rolido de `BoatMotion`) → casco, carro, remero y remos. Remero y remos cabecean con el casco (±1,15°); el legacy no los inclina | 7 (Architect: se adopta) |
| D-ART-05: dos soles | `Core/Scene/CelestialPlacement`: acimut común `(xFrac − 0,5)·70°`; dirección **de pantalla** (disco, halo, brillo en el agua; elevación ≤ 17°) y dirección **de luz** (luz direccional y sombras; elevación ≤ 55°), con puesta y salida graduales (Q-63). `HourLighting` (derivados por hora, ART_BIBLE §6.3) y `MoonPhase` en Core. La paleta legacy (`SceneTimeOfDay`) no cambia; `water_painter_test` sigue pasando (`uSunX` centrado al mediodía, acimut 0) | 6a (Architect: se adopta) |
| D-ANI-06: sincronía fina | Eventos `StrokeCountIncrement` (§10.2) disponibles; apagada hasta la It. 10a | 10a |

### 14.4 Asset slots (QA-P-15)

Un solo modelo, que junta la propuesta del Designer (ASSET_SLOTS §0) con los slots tipados:

```csharp
// RowMate.Scene (Unity). Los ids viven en Core para testearlos con dotnet.
public abstract class AssetSlotDefinition : ScriptableObject
{
    [SerializeField] private string slotId;          // una constante de RowMate.Core.Scene.SlotIds ("rower.character", "prop.pine"…)
    [SerializeField] private string expectedPath;    // p. ej. "Assets/ThirdParty/Mixamo/YBot/YBot.fbx" (re-enlace por ruta, §2)
    [SerializeField] private SlotBudget budget;      // triángulos LOD0, tamaño de textura por plataforma, materiales
    public string SlotId => slotId;
    public abstract SlotResolution Resolve(ISlotLog log);   // asset válido → asset; si no → placeholder + UN aviso
    protected abstract SlotValidation Validate();            // reglas propias del subtipo
}
public abstract class AssetSlot<T> : AssetSlotDefinition where T : UnityEngine.Object
{
    [SerializeField] private T? asset;               // opcional
    protected abstract T BuildPlaceholder();         // procedural, siempre disponible
}
```

- **Subtipos tipados**: `RowerCharacterSlot : AssetSlot<GameObject>` (valida Humanoid, huesos de dedos, altura 1,55-2,05 m, ≤ 30 k triángulos; *placeholder*: maniquí con esqueleto `mixamorig:*`), `BoatPartSlot`, `OarSlot`, `PropSlot` (con 3 LOD + impostor), `TerrainLayerSlot : AssetSlot<TerrainLayer>`, `VegetationSlot`, `MaterialSlot`, `TextureSlot`, `FontSlot : AssetSlot<FontAsset>`, `DataSlot<TProfile>` (perfiles de agua, cielo y librea). `EnvironmentDefinition` agrupa los slots de un escenario (id, escena, `PropCatalog`, perfiles, miniatura).
- **Resolución**: asset asignado y válido → se usa. Falta o es inválido → *placeholder* y **un** aviso en el log de diagnóstico (`[SLOT]`). **Nunca** un error que frene la escena ni el build. El validador del Editor (`RowMate/Slots/Validar`) lista los slots en *placeholder* y su motivo.
- **`SlotIds` en Core**: constantes de texto, una por slot de ASSET_SLOTS §1. Test de `dotnet`: cada `PropKind` del catálogo legacy (15) tiene su `SlotIds.Prop*`, y no hay ids repetidos. Test de PlayMode: cada `SlotIds` tiene su `.asset` en `Assets/RowMate/Slots/`.
- **Rutas**: instancias en `Assets/RowMate/Slots/`; terceros en `Assets/ThirdParty/` (§2).
- **Import de Mixamo**: Humanoid, *Avatar* "Create From This Model" y **Optimize Game Objects desactivado** (Animation Rigging necesita los huesos como transforms), con los ajustes de ASSET_SLOTS §2.5; quedan guardados en los `.meta` versionados. El remero usa un `Animator` con un controlador mínimo (capa base con la pose de agarre) bajo el `RigBuilder`; el maniquí procedural construye su avatar en ejecución con `AvatarBuilder.BuildHumanAvatar` sobre el mismo esqueleto `mixamorig:*`, así el IK es el mismo con o sin Mixamo.
- **Paquetes**: Animation Rigging y Shader Graph están en el manifest desde la It. 1; los íconos son glifos de fuente, sin paquete (§1.2); Cinemachine solo si se hacen las cámaras de BKL-02.

### 14.5 Miniaturas del selector (F-SCN-22, QA-P-11)

- **Horneadas en el Editor** por `EnvironmentThumbnailBaker` desde la escena real (cámara de persecución a 20 m de escena, sin bote, nivel Alto, 304×192 px): texturas en `Art/Textures/Thumbnails/`, incluidas en el build. Mostrarlas no carga ninguna escena aditiva.
- **Por defecto, a las 12:00** (paridad con el legacy, decisión del PO en ROADMAP §6).
- **Si el usuario aprueba "hora actual"** en el gate de la It. 8: se hornean 4 por escenario (6, 12, 18 y 22 h) y el selector muestra la más cercana a la hora de escena, con fundido al cambiar. Costo: 16 texturas (≈ 3 MB), sin render en ejecución. Se descarta el render en ejecución a la hora actual (ART_BIBLE §7.6 en su versión anterior), porque obliga a cargar las 4 escenas en Workout.

---

## 15. Estrategia de tests

### 15.1 Dos ejecutores, una sola fuente

```
tools/dotnet/
├── RowMate.sln
├── Directory.Build.props        # LangVersion 9, Nullable enable, Deterministic, RepoRoot, TreatWarningsAsErrors (Core/App)
├── RowMate.Core/RowMate.Core.csproj                     # netstandard2.1; <Compile Include="$(RepoRoot)Assets/RowMate/Core/**/*.cs" />
├── RowMate.App/RowMate.App.csproj                       # netstandard2.1; ref Core
├── RowMate.Simulator/RowMate.Simulator.csproj           # netstandard2.1; DefineConstants ROWMATE_SIMULATOR
├── RowMate.Infrastructure/RowMate.Infrastructure.csproj # netstandard2.1; sqlite-net-pcl 1.9.172, Newtonsoft.Json 13.0.2
├── RowMate.Tests.Support/RowMate.Tests.Support.csproj   # netstandard2.1; ref Core, App, Infrastructure, NUnit 3.14.0 (= asmdef Tests.Support)
├── RowMate.Tests.EditMode/RowMate.Tests.EditMode.csproj # net8.0; ref Tests.Support, Core, App, Infrastructure (= asmdef EditMode)
├── RowMate.Tests.Simulator/RowMate.Tests.Simulator.csproj # net8.0; ref Tests.Support, Core, App, Simulator; ROWMATE_SIMULATOR
├── RowMate.Tokens/RowMate.Tokens.csproj                 # net8.0; consola tools/tokens (ref Core): genera Tokens.uss
├── RowMate.Tests.UnityApiCheck/…csproj                  # netstandard2.1 + NUnit 3.5.0; solo compila Support + EditMode + Simulator (por glob)
└── run-tests.ps1                                        # dotnet build de la SOLUCIÓN + dotnet test --no-build (QA-01 de la It. 1)
```

- **El comando** (It. 1, QA-01): `run-tests.ps1` corre `dotnet build tools/dotnet/RowMate.sln --configuration Release --artifacts-path tools/dotnet/.artifacts` y después `dotnet test` de la misma solución con `--no-build` y el mismo `--artifacts-path`, y propaga el primer código distinto de 0. `dotnet test` solo no alcanza: compila únicamente los proyectos de test y sus dependencias, y dejaría sin compilar `RowMate.Tests.UnityApiCheck` y `RowMate.Tokens`. Parámetros `-Filter`, `-Ci`, `-Demo`, `-Matrix` (matriz AC → test → resultado desde el XML de NUnit) y `-ArtifactsPath` (tools/dotnet/README.md).
- **Compatibilidad con Unity verificada por el compilador** (QA-10 de la It. 1): `RowMate.Tests.UnityApiCheck` compila las fuentes de `Tests/Support` (por glob, no por `ProjectReference`), `Tests/EditMode` y `Tests/Simulator` contra `netstandard2.1` y el paquete **NUnit 3.5.0**: una API posterior a 3.5 o fuera de .NET Standard 2.1 rompe el paso 1 del comando.
- **Sin `record` ni `init` en las fuentes de test** (QA-03 de la It. 1): en Unity esos assemblies ven por IVT varios *shims* internos de `IsExternalInit` y declarar un `record` da `CS0518`. Datos de test con `class`/`struct` y constructor; usar los records de Core está permitido. Lo controlan `NUnitCompatibilityTests` y la compilación de `UnityApiCheck`.

- `EnableDefaultCompileItems=false` y `Compile Include` apuntando a `Assets/`: **las fuentes no se copian**.
- **Un `.csproj` por asmdef, con las mismas referencias** (QA-P-03): así `dotnet build` falla si un test de EditMode usa un tipo del simulador o si a Support le falta una referencia, igual que fallaría en Unity. Antes un solo proyecto de test compilaba todo junto y escondía esos errores.
- **Lint de asmdefs** (QA-P-25, en `RowMate.Tests.EditMode`): lee todos los `.asmdef` de `Assets/` y verifica que el JSON es válido, que cada referencia por nombre existe, que el grafo coincide con el de los `.csproj`, la regla de dependencias de §3.2, `noEngineReferences` en los puros, `defineConstraints` del simulador y de los tests, e `includePlatforms: ["Editor"]` en EditMode y Simulator. También valida `Packages/manifest.json` (JSON y versiones exactas) y `ProjectSettings/ProjectVersion.txt`. Es la revisión estática de lo que no se puede compilar sin Editor.
- **Vectores dorados** (QA-P-23, ADR 012 §4): `tools/oracle/bin/*.dart` generan `Assets/RowMate/Tests/Golden/oracle/*.json` (D-01 de la It. 1: los datos junto a los tests que los usan) con Dart 3.13.5. `tools/oracle/run-oracle.ps1` arma un `package_config` propio en `tools/oracle/.dart_tool/` (sin `pubspec` ni `pub get`) y vigila el legacy antes y después (`status --porcelain` vacío, listado `--ignored` archivo por archivo y metadatos sin abrir archivos). `GoldenVectors` (Tests/Support) los lee con `RepoPaths` y los tests de `dotnet` los consumen desde la It. 1. Formato (QA-06 de la It. 1): secciones chicas como arreglo de objetos; toda sección de más de 200 entradas como tabla (`columns`/`rows`, una fila por línea, `double` como 16 hex de sus bits); **tope de 1 MiB por archivo**. Regenerarlos necesita Dart y el legacy al lado; correr los tests, no.
- Los mismos archivos de test los compila Unity con los asmdefs `RowMate.Tests.EditMode` y `RowMate.Tests.Simulator` (Unity Test Framework 1.6, NUnit 3.5).
- **En Unity** (QA-V-05 de la It. 1): `tools/unity/run-unity-tests.ps1 [-Platform EditMode|PlayMode] [-Simulator]` corre `Unity -batchmode -runTests` y resume el XML de resultados (en `tools/unity/.artifacts/`, ignorado). `RowMate.Tests.Simulator` solo existe con `ROWMATE_SIMULATOR`: `-Simulator` lo activa solo para la corrida y lo restaura siempre (§4.2). No arranca si otro Editor tiene el proyecto abierto (`Temp/UnityLockfile`). Detalle en `tools/unity/README.md`.
- **API de NUnit permitida**: la intersección de NUnit 3.5 (Unity) y 3.14 (dotnet): `[Test]`, `[TestCase]`, `[TestCaseSource]`, `[SetUp]`, `[TearDown]`, `Assert.That` con `Is.*`/`Has.*`/`Does.*`, `.Within(...)`, `Assert.Throws`. No usar `Assert.Multiple`, `ClassicAssert` ni nada de NUnit 4. Tests sincrónicos con `FakeScheduler` siempre que se pueda. **Lo que mostró el primer open (It. 2):** `[Category("AC-…")]` resuelve a `RowMate.Tests.CategoryAttribute` (Tests/Support), porque el NUnit 3.5 de Unity deja *NotRunnable* todo test con una categoría con `-` (todas las de AC, QA y LT); no usar `[SetCulture]`/`[SetUICulture]` (el Unity Test Framework no los aplica: fijar la cultura a mano en `try/finally`) ni `Has.Count` (en 3.5 falla con un arreglo detrás de `IReadOnlyList<T>`). Lo controlan `TestCategoryTests` y `NUnitCompatibilityTests`.
- Archivos de datos (catálogos, fixtures FTMS): `RepoPaths.Root` sube desde el directorio actual hasta encontrar `ProjectSettings/ProjectVersion.txt` (funciona en `dotnet test` y en el Editor, cuyo directorio actual es la raíz del proyecto).

### 15.2 Qué se testea dónde

| Nivel | Dónde | Ejemplos |
|---|---|---|
| Reglas puras | `dotnet` + Unity EditMode | Parsers FTMS/HR, simulador físico, rutinas y progresiones, `SessionStats` (P99), formatos, `ScanFilter`, `ReconnectLoop`, `WorkoutEngine`, `SeriesTracker`, escena (cámara, estado, hora, orillas), TCX, metadatos |
| Estado de pantallas | `dotnet` + Unity EditMode | Stores con servicios falsos: `HeartRateStore` (`heart_rate_provider_test`), reglas de la tarjeta (`heart_rate_card_test`), panel del simulador (`simulator_overlay_test`) |
| Persistencia | `dotnet` (SQLite real en archivo temporal) + Unity EditMode | Esquema v8, CRUD de rutinas, sesiones y puntos, estadísticas, limpieza de importadas |
| Servicios BLE | `dotnet` | `RowerService`/`HeartRateService` con `FakeBleAdapter` (secuencias de conexión, timeouts, generaciones, reintentos) |
| UI | Unity PlayMode / UI Test Framework | Textos visibles, desbordes en 640×360 y 800×360, navegación, diálogos |
| Escena | Unity PlayMode | "Dibuja sin excepciones" (equivalente a los tests de *painters*); `scene_view_test` = 4 escenarios × 2 tamaños × 2 horas (12 h y 23 h): los 4 del lago en la It. 6a (y otra vez en la 6b) y los 12 restantes en la It. 8; capturas de aceptación visual |
| Plataforma | Dispositivo | BLE real, permisos, OAuth, wakelock, segundo plano |

### 15.3 Trazabilidad

Cada test portado lleva en un comentario el origen: `// legacy: test/rowing_simulator_test.dart:12 'nombre del caso'`. Las clases de test se llaman como el archivo legacy en PascalCase + `Tests`. Cada `AC-…` del PO tiene al menos un test con `[Category("AC-RUT-03")]` (o el ID que use el spec) para que QA arme la matriz AC → test → resultado.

---

## 16. Bootstrap del proyecto sin Editor

Se escribe a mano solo lo que es texto estable; todo lo demás lo crea Unity o el bootstrapper en el primer open.

| Se escribe a mano | Lo crea Unity / el bootstrapper |
|---|---|
| `Packages/manifest.json` (§1.2) | `Packages/packages-lock.json` |
| `ProjectSettings/ProjectVersion.txt` (solo `m_EditorVersion`) | Resto de `ProjectSettings/*.asset` |
| `.asmdef`, `.cs`, `.uxml`, `.uss`, `.tss`, `.shader`/`.hlsl`, `.json`, `link.xml`, `csc.rsp`, fuentes nativas | `.meta`, escenas, assets de URP, `PanelSettings`, `FontAsset`, materiales |

`RowMate.EditorTools.ProjectBootstrapper` (assembly `RowMate.Editor`; `[InitializeOnLoad]`, idempotente, versión en `ProjectSettings/RowMateBootstrap.json`). En `EditorApplication.delayCall` aplica los pasos de su versión, **una vez por sesión del Editor** (`SessionState`, QA-V-03 de la It. 1): un paso fallido se reintenta en el próximo open, no en cada recompilación; el menú `RowMate/Bootstrap/Reaplicar` fuerza una corrida. En batchmode (`-quit`, `-executeMethod`, `-runTests`) el `delayCall` no llega a correr: ahí lo dispara `BootstrapPostprocessor` (`OnPostprocessAllAssets` con `didDomainReload`, el mismo gancho que usa URP para su Global Settings), con la misma marca de sesión (primer open real, It. 2). Subir `CurrentVersion` hace que el próximo open reaplique todos los pasos (versión 2: `PostProcessData` de URP en el renderer). **Se escribe en dos etapas** (QA-P-25): lo mínimo en la It. 1, lo completo en la It. 2, cuando ya hay Editor para probarlo.

**Bootstrapper mínimo (It. 1, NO VERIFICABLE hasta que haya Editor):**

1. **Player**: compañía "iguisoft", producto "RowMate", versión 1.0.0 / build 1. **Ids provisionales (R-14, Q-85):** `com.iguisoft.rowmate` en Android; **`com.iguisoft.rowmate.unity`** en iOS y macOS (y como identificador único de Standalone), porque el legacy ya usa `com.iguisoft.rowmate` en Apple y un build con ese id lo pisaría al instalarse. **Windows no tiene identificador propio en Unity** (D-02 de la It. 1, aceptada por el PO en AC-APP-16-02): su identidad es el producto `RowMate` de `iguisoft` (datos en `AppData/LocalLow/iguisoft/RowMate`, distinta de las del legacy) y `com.iguisoft.rowmate` queda reservado para el MSIX de la It. 14; ningún paso lo aplica. Todas las constantes salen de `RowMate.Core.Common.AppIdentity`. IL2CPP, .NET Standard 2.1, color space Linear, Android minSdk 26 (8.0, Q-90) / target el más alto instalado / solo ARM64, iOS 15.0, Internet *Require*, gráficos (Vulkan + GLES3 de respaldo en Android, Metal en Apple, DX11 en Windows), `runInBackground = true` en Standalone (§6.1), *Frame Timing Stats* activado (lo usa la adaptación térmica, §20), entrada = Input System (requiere reiniciar el Editor; el script lo avisa).
2. **URP provisorio**: crea `Settings/URP/RowMate_Renderer.asset` (Universal Renderer, Forward) y **un solo** `RowMate_URP_Default.asset` (`UniversalRenderPipelineAsset.Create(rendererData)`) y lo asigna en Graphics y en todos los niveles de calidad existentes. **No** fija niveles de calidad definitivos (QA-P-10).
3. **Defines**: verifica que `ROWMATE_SIMULATOR` no esté puesto salvo que el usuario lo haya pedido con el menú.
4. Registra lo hecho en la consola y en el archivo de estado. Menú `RowMate/Bootstrap/Reaplicar`.

**Bootstrapper completo (It. 2):** suma `Settings/UI/RowMatePanel.asset` (`PanelSettings` con el tema `Presentation/Styles/RowMateTheme.tss`, 160 DPI), `Scenes/Boot.unity` con `AppRoot`, `UIDocument` y cámara de UI registrada en Build Settings, y el *smoke test* IL2CPP. **Niveles de calidad (It. 6a):** reemplaza el asset provisorio por `RowMate_URP_{High,Medium,Low}` y los 3 `QualityTierProfile` (§20, ADR 018).

Si algún paso de URP falla por API, el script deja instrucciones de 3 pasos en la consola (crear el asset de URP desde el menú, asignarlo en Graphics y en Quality) y QA lo registra.

**Qué es verificable en la It. 1 sin Editor:** el *lint* de asmdefs, manifest y `ProjectVersion.txt` (§15.1) y la revisión estática de QA (nombres de asmdef, referencias por nombre, JSON válido). Versiones verificadas en la web: `com.gilzoide.sqlite-net` 1.3.3 e Input System 1.20.1; el resto queda **NO VERIFICABLE** hasta el primer open (RA-01, RA-13).

---

## 17. Convenciones de código

- Namespaces = carpeta (`RowMate.Core.Ble`, `RowMate.App.Device`, `RowMate.Presentation.Workout`). Bloques `namespace X { }` (no *file-scoped*).
- **Excepción: el assembly `RowMate.Editor` usa el namespace `RowMate.EditorTools`**, uno solo para todas sus carpetas (QA-V-04 de la It. 1). Ningún namespace propio tiene un segmento `Editor`, `UnityEditor` ni `UnityEngine`: `namespace RowMate.Editor` haría que, dentro de `RowMate.*`, `Editor` resuelva al namespace antes que a `UnityEditor.Editor` (`class X : Editor` → CS0118). Tampoco subnamespaces como `RowMate.EditorTools.Simulator`, que taparían a `RowMate.Simulator` dentro del assembly. Lo controla `AsmdefLintTests.NoNamespaceShadowsUnityNames`.
- Un tipo público por archivo; el archivo se llama como el tipo. Tests: `<Tipo>Tests.cs`.
- Identificadores en inglés; **comentarios en español** (como el legacy). Cada puerto cita su fuente: `// legacy: lib/core/bluetooth/ble_service.dart:259-289`.
- Clases `sealed` por defecto. Modelos de valor como `sealed record` con propiedades `init`; `CopyWith` explícito cuando el legacy usa `copyWith` con la semántica "null = conservar".
- Nulabilidad habilitada (`csc.rsp` + `Nullable` en dotnet).
- Nombres de pasos, métricas y estados idénticos a los del legacy cuando tienen valor persistido (`warmup`, `work`, `rest`, `cooldown`; `auto`, `ask`, `manual`; `lake`…).
- Sin LINQ, `foreach` sobre interfaces ni closures en caminos por frame (`Update`, `generateVisualContent`, callbacks de notificación).
- MonoBehaviours: `[SerializeField] private` + propiedades de solo lectura; nada de `FindObjectOfType` en runtime; inicialización explícita `Initialize(AppContext)`.
- Logs con prefijos del legacy (`[BLE]`, `[HRM]`, `[STRAVA_AUTH]`) a través de `IAppLogger` (Core; no `ILogger`, que choca con `UnityEngine.ILogger`, D-03 de la It. 1) implementado con `Debug.Log` en Unity y `RecordingLogger` en tests.
- Formato: `.editorconfig` (4 espacios, llaves en línea nueva, `var` cuando el tipo es evidente).

---

## 18. Restricciones de C# de Unity y paridad Dart → C#

### 18.1 Lenguaje (C# 9, .NET Standard 2.1)

- Prohibido: *file-scoped namespaces*, `global using`, `required`, *raw string literals*, `record struct` (C# 10), patrones de listas, `static abstract` en interfaces, APIs fuera de .NET Standard 2.1 (`System.Text.Json`, `DateOnly`, `TimeOnly`, `Random.Shared`, `ArgumentNullException.ThrowIfNull`). Sí están, por ejemplo, `HashCode`, `Math.Clamp`, `MathF`, `Span<T>`.
- `record` e `init` requieren el *shim* `IsExternalInit` en Core (`#if !NET5_0_OR_GREATER`).
- IL2CPP/AOT: nada de `System.Reflection.Emit`, `dynamic` ni `Expression.Compile`; genéricos sobre `struct` usados por reflexión se instancian explícitamente; `link.xml` preserva `RowMate.Infrastructure` (DTOs de sqlite-net) y lo que se deserialice.
- Eventos nativos: modelo *pull* (§10.4), sin callbacks de nativo a C#. Si algún plugin necesitara un callback, se usa un método `static` con `[AOT.MonoPInvokeCallback(typeof(TDelegate))]` y el delegado en un campo estático para que el GC no lo recolecte.

### 18.2 Paridad numérica (ADR 012)

| Dart | Trampa en C# | Regla |
|---|---|---|
| `x.round()` (mitad hacia afuera, devuelve `int`) | `Math.Round` usa redondeo bancario y `AwayFromZero` sobre el `double` no coincide con Dart (QA §3.2) | `DartMath.Round(x) : long` exacto (truncado + fracción, ADR 012 §2); nunca `-0` |
| `x.toStringAsFixed(n)` | Ni `ToString("F")` ni `Math.Round(AwayFromZero)` reproducen a Dart (`0.25`, `0.35`, `2.5`, `24.45`) | `Fmt.Fixed(x, n)`: expansión decimal **exacta** del `double` (`ulong` o `BigInteger`), desempate alejándose del cero, `-0` como Dart; `Fmt.TryFormatFixed` sin asignaciones (ADR 012 §1). **Prohibido** `ToString("F…")` y `Math.Round` en Core y App (chequeo del CI) |
| `double.parse(x.toStringAsFixed(n))` (`rowing_simulator.dart:60`) | Pasar por texto con la cultura del equipo | `DartMath.RoundToFixed(x, n)`: `±q/10^n` exacto, sin texto |
| `a % b` (resultado no negativo) | `%` de C# puede dar negativo | `DartMath.Mod(a, b)` |
| `a ~/ b` (trunca) | `/` entero también trunca | Igual, pero en `double` usar `DartMath.TruncDiv` |
| `'$x'` con `double` | `ToString()` da `"24"` y no `"24.0"` | `Fmt.DartToString(x)` |
| `int` de 64 bits | `int` de 32 bits | `long` para epoch en ms y acumulados que puedan pasar 2³¹ |
| `math.Random(seed)` | `System.Random` es otro algoritmo | `DartRandom` (puerto del algoritmo de la VM de Dart) donde el resultado se ve (escena); `IRandom` inyectable en el simulador |
| `DateTime.parse`/`toIso8601String` | Formatos, zonas y microsegundos | `IsoDate.Format/Parse` propios (microsegundos solo si no son 0, como Dart), con cultura invariante |
| `pow`, `sin`, `cbrt` | Puede diferir en el último bit entre runtimes | Tests con tolerancia (`Within(1e-9)`) salvo donde el legacy compara exacto |

Todos estos helpers viven en `RowMate.Core.Text` y tienen tests propios con los valores de los tests del legacy y los vectores dorados del oráculo Dart (ADR 012 §4-§5).

---

## 19. Build y CI por plataforma

| Plataforma | Dónde compila | Salida | Notas |
|---|---|---|---|
| Android | Windows (local) o Linux (CI, GameCI) | APK (sideload) y AAB | Firma con keystore propio desde secretos de CI (el legacy firmaba release con la clave de debug). minSdk 26, ARM64 |
| iOS | Unity genera el proyecto Xcode (cualquier SO) → `xcodebuild` en macOS | IPA sin firmar (paridad con el artefacto "RowMate-App") y, cuando haya cuenta, TestFlight | Post-proceso de `Info.plist`, background mode, Swift |
| macOS | Mac (IL2CPP necesita macOS) | `.app` firmado con entitlements (BLE, red) y, si se desea, notarizado | Necesita `RowMateNative.bundle` precompilado |
| Windows | Windows con VS 2022 C++ (IL2CPP) | Carpeta zip `RowMate-Windows` (x86_64) | Necesita `RowMateBle.dll` precompilado |

- **Build por CLI**: `Unity -batchmode -quit -projectPath . -executeMethod RowMate.EditorTools.BuildCli.Build -rowmateTarget <Android|iOS|Windows|macOS> [-rowmateSimulator]`. `BuildCli` arma `BuildPlayerOptions` (escenas desde Build Settings, `extraScriptingDefines`), genera `StravaSecrets.g.cs` si hay variables de entorno y lo borra al terminar en CI.
- **Plugins nativos**: `native/windows` (CMake + MSVC, job en `windows-latest`) y `native/macos` (`xcodebuild`, job en `macos-latest`) producen los binarios que se versionan con Git LFS en `Assets/Plugins/...`; el job verifica que el binario versionado coincide con el compilado.
- **CI** (GitHub Actions):
  - `ci-core.yml` (desde la It. 1): `./tools/dotnet/run-tests.ps1 -Ci` (el mismo comando que en la PC: `dotnet build` de la solución y `dotnet test --no-build`, §15.1) en `ubuntu-latest` y `windows-latest`, en cada push y PR, con `actions/setup-dotnet` y `global.json` (8.0.427). La **higiene del repositorio** son tests de `dotnet` dentro de ese mismo comando (`RepoHygieneTests`, `PureCodeRulesTests`, `TokensUssSyncTests`; D-04 de la It. 1, en lugar de `tools/ci/check-repo-hygiene`). No necesita Unity, licencia, Dart ni secretos.
  - Higiene del repositorio (falla el job si algo no se cumple; con `ROWMATE_CI=1` los tests que necesitan git fallan en vez de quedar inconclusos):
    1. `git ls-files` no contiene ningún `StravaSecrets*`, `strava_secrets.*`, `strava.local.json`, `*.keystore`, `*.jks`, `*.p12`, `*.mobileprovision` ni `*.ulf` (R-02); **tampoco lo que `git add -A` agregaría** (`git ls-files --others --exclude-standard`: en disco, sin versionar y sin ignorar), así en la PC se detecta antes del commit y del push (QA-V-02 de la It. 1). La detección se prueba en un repositorio git temporal.
    2. `git check-ignore` confirma que `Assets/RowMate/Bootstrap/Generated/StravaSecrets.g.cs`, cualquier `StravaSecrets*` (`.json`, `.asset`, `.txt`… y su `.meta`) y `Assets/ThirdParty/Mixamo/YBot/YBot.fbx` están ignorados y que `Assets/ThirdParty/Mixamo/YBot/YBot.fbx.meta`, `Bootstrap/StravaCredentials.cs` y `Core/Strava/StravaConfig.cs` **no** lo están (protege el `.gitignore` de cambios accidentales).
    3. Bajo `Assets/ThirdParty/` solo hay `.meta` versionados (o por versionar); ningún `.fbx` en todo el repo.
    4. Ningún `Math.Round(` ni `ToString("F` en `Assets/RowMate/Core/` y `Assets/RowMate/App/` (ADR 012); ningún `using UnityEngine` en los assemblies puros (lo confirma además el build de `dotnet`).
    5. `Presentation/Styles/Tokens.uss` coincide con lo que genera `tools/tokens` (también lo verifica un test, ADR 016).
    6. Ningún `ProjectSettings/*.asset` ni *Build Profile* versionado tiene `ROWMATE_SIMULATOR` (R-01, QA-V-05 de la It. 1).
  - `ci-unity.yml` (desde la It. 2, cuando el usuario cargue la licencia de Unity Personal en los secretos `UNITY_LICENSE`/`UNITY_EMAIL`/`UNITY_PASSWORD`): tests EditMode y PlayMode con GameCI, y builds de las 4 plataformas (Android/iOS-proyecto en Linux, Windows en `windows-latest`, macOS e IPA en `macos-latest`).
  - Ningún job de distribución define `ROWMATE_SIMULATOR`; un job aparte "dev" puede generar builds de simulador (siempre `Development`, con sufijo `-sim` en el artefacto).
  - Secretos: `STRAVA_CLIENT_ID`, `STRAVA_CLIENT_SECRET`, keystore de Android, certificados de Apple. Nunca en el repo.

---

## 20. Presupuesto de rendimiento

**Fuente única** (QA-P-10): esta tabla reemplaza a la anterior de este documento ("Móvil/Escritorio") y es la que referencia ART_BIBLE §9, que conserva solo el **reparto** del arte dentro de estos topes. Los números son los del Designer para el nivel Medio, que QA considera verosímiles para un Galaxy A54 / Pixel 6a pero **NO VERIFICABLES** sin dispositivo (ROADMAP regla 9).

Objetivo R-12: **60 fps sostenidos durante 30 min** durante el entrenamiento en el teléfono de referencia del nivel Medio, con margen térmico. Cuadro de 16,6 ms.

### 20.1 Niveles de calidad

| | **Alto** | **Medio** (paridad, referencia de R-12) | **Bajo** |
|---|---|---|---|
| Equipos de referencia | iPhone 13+, Pixel 8, Galaxy S22+; todo escritorio con GPU dedicada (por defecto en Windows y macOS) | Galaxy A54 / Pixel 6a / iPhone 11 | Galaxy A14/A15 (Mali-G52), **Android 8+ ARM64** (Q-90, minSdk 26), 4 GB, GLES 3.2 |
| GPU / hilo principal | ≤ 12 ms / ≤ 8 ms | ≤ 12 ms / ≤ 8 ms | ≤ 12 ms / ≤ 8 ms |
| Resolución (`renderScale`) | 1,0 | 0,85 (dinámica 0,75-1,0) | 0,75 |
| Antialiasing | MSAA 4× | MSAA 2× | FXAA |
| Draw calls / SetPass (SRP Batcher) | ≤ 150 / ≤ 50 (el Designer puede ampliarlo en la It. 6a con medición) | **≤ 150 / ≤ 50** | ≤ 150 / ≤ 50 |
| Triángulos visibles | ≤ 600 k | **≤ 350 k** | ≤ 180 k |
| Texturas | ≤ 2048², BC7 (escritorio) / ASTC 6×6 | ≤ 1024², ASTC 6×6; **≤ 180 MB** | ≤ 1024², ASTC 6×6; ≤ 180 MB |
| Sombras | 2 cascadas, 2048, 80 m | **1 cascada, 1024, 40 m**, solo bote, remero, remos y props cercanos | Óvalo del legacy (sin mapa de sombras) |
| Reflejo del agua | Planar a 1/4 | Analítico de orilla + cubemap | Cubemap |
| Agua | Avanzada | Avanzada, ≤ 70 ALU / 5 tex por fragmento | Avanzada si entra en el presupuesto; si no, degradada (R-04) |
| Vidrio del HUD | `_RmHudBlur` a 1/4, 3 pasadas Kawase | Igual, **≤ 0,6 ms** | Vidrio plano `glass.fallback` (`#070D16 @86 %`, sin blur) |
| Post-proceso | ≤ 1,5 ms: bloom a media resolución, tonemapping, color por hora, viñeta | Igual | Tonemapping y color por hora |
| Vegetación y LOD | Distancias de LOD × 1,0 | × 1,0, sobre-dibujo ≤ 2,5× | Distancias de LOD × 0,7 (ART_BIBLE §9) |

### 20.2 Topes comunes (todos los niveles)

| Recurso | Tope |
|---|---|
| CPU de escena / Animation Rigging / UI | ≤ 1,5 ms / ≤ 0,3 ms / ≤ 1,0 ms (sin *relayout* por cuadro: los números animan con transformaciones, §7.8) |
| UI Toolkit | HUD ≤ 60 elementos visibles; sin máscaras anidadas; un `TabularNumber` por número |
| GC | **0 B/cuadro** en régimen durante el entrenamiento (verificado con el Profiler); los eventos de 1 Hz pueden reservar poco |
| Partículas | ≤ 400 vivas; VFX ≤ 0,4 ms |
| Memoria total | ≤ 600 MB en móvil, ≤ 1,5 GB en escritorio |
| Arranque | ≤ 3 s hasta la primera pantalla en el teléfono de referencia (≤ 2 s en escritorio) |
| Batería | Escena apagada fuera del inmersivo; 30 fps en reposo y 60 mientras algo se anima (§13.1) |

### 20.3 Elección del nivel y adaptación térmica (ADR 018)

- `QualityTierProfile` (`ScriptableObject`, uno por nivel en `Settings/Quality/`) con todo lo de §20.1 y su asset de URP (`RowMate_URP_{High,Medium,Low}`). Los crea el bootstrapper en la **It. 6a**; el de la It. 1 no fija niveles (§16).
- `GraphicsTier` (Platform) elige el nivel inicial al arrancar con `SystemInfo` (nivel de shaders, memoria de GPU y del sistema, núcleos, API gráfica) y una lista de GPU conocidas; el usuario no lo ve (no hay ajuste de calidad en el legacy).
- `ThermalAdapter` (Platform) mide con `FrameTimingManager` (*Frame Timing Stats* activado en el Player). Si el tiempo de **GPU** no está disponible (pasa en algunos Android y backends), usa el tiempo de **CPU** del cuadro (hilo principal + *render thread*) como respaldo. Disparo: > 13 ms de GPU (o > 15 ms de CPU) sostenidos 3 s.
- Orden al degradar (pedido del Designer): **resolución dinámica → reflejo → distancia de sombras → densidad de vegetación → nivel**. Nunca se baja la tasa de cuadros antes que el detalle. La "resolución dinámica" es `UniversalRenderPipelineAsset.renderScale` (0,05 por paso, entre 0,75 y 1,0) y **no** `ScalableBufferManager`, que no funciona en GLES3 ni DX11. Como cambiar `renderScale` realoca *buffers*, se cambia **como mucho una vez cada 10 s**, y para volver a subir hacen falta 30 s por debajo de 10 ms (histéresis).
- Verificación: It. 6a-8 [Device] en el Android de gama media (regla 9); sin teléfono, NO VERIFICABLE hasta la It. 14.

---

## 21. Riesgos y mitigaciones

| ID | Riesgo | Impacto | Mitigación |
|---|---|---|---|
| RA-01 | No hay Unity Editor en la máquina: el código Unity no se compila hasta que el usuario lo instale | Errores tardíos en MonoBehaviours, asmdefs, UXML | Capa Unity delgada; todo lo demás en `dotnet`; revisión estática de QA; prerrequisito de Editor para la It. 2 |
| RA-02 | No hay Mac: iOS y macOS (Swift, IL2CPP, firma) no se pueden verificar | BLE, OAuth, Keychain y wakelock de Apple sin probar | Juntar todo lo nativo de Apple en una tanda (propuesta P-06); jobs `macos-latest` en CI; misma fuente Swift para iOS y macOS |
| RA-03 | Tres pilas BLE nativas propias | Es la pieza más difícil del proyecto | `IBleAdapter` mínimo; toda la lógica en Core con `FakeBleAdapter`; empezar por Windows (se prueba en la PC con el pulsómetro) y Android |
| RA-04 | Diferencias numéricas Dart → C# y entre .NET 8 y Mono/IL2CPP | Números distintos a los del legacy | ADR 012, helpers `DartMath`/`Fmt`, cultura invariante, tests corridos en ambos runtimes |
| RA-05 | Stripping/AOT de IL2CPP (sqlite-net por reflexión, Newtonsoft) | Crash en dispositivo, no en Editor | `link.xml`, `JToken` en lugar de deserialización genérica, smoke test en un build IL2CPP por iteración que toque persistencia |
| RA-06 | Rendimiento de UI Toolkit en Android de gama baja | HUD que tironea | Presupuesto de elementos, `TabularNumber` de un solo elemento (§7.8), perfilar en dispositivo desde la It. 4a, respaldo sin blur |
| RA-07 | Blur de fondo: UI Shader Graph y Render Graph son APIs nuevas; la textura tiene que sobrevivir hasta la UI en *Screen Space Overlay* | Glass sin efecto | `RTHandle` persistente + global `_RmHudBlur` (§7.4); prototipo en la It. 6b (P-04); respaldo plano con el token `glass.fallback` (`#070D16 @86 %`, P-08 de la It. 1) siempre disponible |
| RA-08 | Unity pausa el loop en segundo plano en Android e iOS, mientras que Dart sigue corriendo (QA-P-06) | El monitor sale del modo Bluetooth, el reloj se frena; en Android, si el sistema mata el proceso en segundo plano, se pierde lo posterior a la pausa | Keepalive nativo, eventos con marca de tiempo y alcance al volver (§6.1, ADR 017); guardado inmediato al pausar; medición del legacy como referencia en la It. 10a; servicio en primer plano como propuesta si hiciera falta |
| RA-09 | `TimeZoneInfo.Local` en Android con IL2CPP | Hora del reloj, de la escena y del historial corrida | `IClock.LocalNow` aislado; test en dispositivo; respaldo con el offset nativo si falla |
| RA-10 | Secreto de Strava embebido en el binario | Extraíble, como en el legacy | Inyección al compilar (no se versiona); riesgo aceptado; un proxy de tokens queda para backlog |
| RA-11 | Paquete de SQLite de un tercero | Mantenimiento | Aislado en Infrastructure; alternativa SQLitePCLRaw con los mismos repositorios |
| RA-12 | Soporte de 6.3 LTS hasta diciembre de 2027 y 6.7 LTS en camino | Upgrade futuro | APIs estándar; evaluación de 6.7 LTS en la It. 14 |
| RA-13 | Bootstrapper de URP por script (APIs internas o cambiantes) | Primer open con pasos manuales | Script idempotente con instrucciones de respaldo; QA lo valida al instalar el Editor |
| RA-14 | Mixamo y Asset Store: términos de redistribución | Repo con FBX o texturas de terceros | Ruta única `Assets/ThirdParty/` ignorada por git salvo los `.meta` (verificado con `git check-ignore`); chequeo del CI (§19); el usuario los descarga |
| RA-17 | Abrir el proyecto sin los archivos de terceros: Unity borra los `.meta` huérfanos de `Assets/ThirdParty/` | Se pierden GUID y ajustes de importación; el slot cae al *placeholder* | `git checkout -- Assets/ThirdParty` antes de copiar los archivos (ASSET_SLOTS lo indica); `expectedPath` en el slot y re-enlace por ruta en el validador del Editor |
| RA-18 | `MeshGenerationContext.DrawText` (base de `TabularNumber`) no verificado en 6000.3 sin Editor | Rodillo de dígitos o tope de elementos del HUD | Respaldo con *quads* del atlas SDF (§7.8); test PlayMode de elementos y 0 B/cuadro en la It. 2 |
| RA-19 | Reloj monótono nativo distinto en cada plataforma (sueño profundo en Android) | El reloj activo cuenta distinto que el legacy con la pantalla bloqueada | Se mide el legacy en el mismo teléfono en la It. 10a y se elige la fuente que da el mismo resultado (`elapsedRealtime` o `uptimeMillis`) |
| RA-15 | Deep link de Android con *singleTask* y arranque en frío | Login de Strava que no vuelve | `Application.absoluteURL` en arranque en frío + `deepLinkActivated` en caliente; test en dispositivo (It. 12) |
| RA-16 | Teclado en móvil sin `adjustResize` | Campos tapados en el editor | `KeyboardAvoider` (It. 3) |

---

## 22. Propuestas al roadmap

El PO las decidió en [ROADMAP §7](../ROADMAP.md) (todas adoptadas). Esta tabla queda como registro, con el estado final.

| ID | Propuesta | Estado tras el PO y QA |
|---|---|---|
| P-01 | **It. 1** suma las piezas base que usan todas las iteraciones | **Adoptada y ampliada** (QA §4): `IScheduler` + `FakeScheduler`, `ObservableValue`/`EventStream`, `DartMath`/`Fmt`/`IsoDate` con vectores dorados, `BleUuid` y `BleScanResult`, `IRandom`, `ILogger`, `IsExternalInit`, `RepoPaths`, `Localizer` con el catálogo convertido (plurales de Q-02), `Core/Design/Tokens` (QA-P-05), `tools/oracle` (QA-P-23), `RowMate.Tests.Support` (QA-P-03), *lint* de asmdefs y `ci-core.yml` con la higiene del repositorio |
| P-02 | **It. 3** crea la base en v8 (v7 + `workout_sessions.origin` + índice de `data_points`) | Adoptada |
| P-03 | **Dividir la It. 4** en 4a y 4b | Adoptada con el corte de QA (4a: motor y HUD funcional; 4b: HUD EXR) |
| P-04 | Mover el **blur del glass** a la escena | Adoptada: desenfoque en la **6b**; la 4a usa el respaldo plano del Designer (QA-P-17; hoy `#070D16 @86 %`, ART_BIBLE §3.2) |
| P-05 | **Dividir la It. 6** en 6a y 6b | Adoptada con el ajuste de QA-P-14: el **agua degradada** (R-04) pasa a la 6a, que queda con una escena completa y usable; la 6b es agua avanzada, reflejos, estela, niebla, desenfoque y rendimiento |
| P-06 | **Dividir la It. 10** en 10a (Android + Windows) y 10b (Apple, con todo lo nativo de Apple) | Adoptada con regla de cierre [Apple] (ROADMAP regla 8) |
| P-07 | *Spike* opcional de BLE en Windows | Adoptada como S-1, **sin Unity** (QA-P-24): `native/windows` se compila con VS Community 2022 17.14 (herramientas C++ instaladas, CMake incluido) y `tools/spikes/ble-windows` es una consola `net8.0` que usa `RowMateBle.dll` por P/Invoke con la misma API C de §10.4 (`rm_ble_*`, drenaje *pull*), más `HeartRateParser` y `ScanFilter` de Core, contra el pulsómetro real. Lo que se aprende va directo al adaptador de la It. 10a |
| P-08 | `ci-unity.yml` desde la It. 2 | Adoptada, condicionada a la licencia |
| P-09 | El Designer especifica los archivos exactos de Mixamo | Adoptada (en la 6a, para la 7) |
| P-10 | Evaluar Unity 6.7 LTS en la It. 14 | Adoptada como evaluación; el cambio necesita el OK del usuario |

### Desvíos y aclaraciones para el PO

- **Q-61**: se porta el RNG de Dart (`DartRandom`); el paisaje queda **idéntico** al legacy (Q-61 revisada por el PO). Vectores en la It. 6a.
- **Q-11** (import de la base legacy): queda diseñado (§9.3) y sin implementar. En Android no es posible si cambia el application id; en iOS tampoco mientras rija R-14.
- **Q-85 / R-14** (identificadores): provisionales `com.iguisoft.rowmate` en Android y **`com.iguisoft.rowmate.unity`** en iOS y macOS (también el identificador único de Standalone del motor), para no pisar la app legacy (§16). Windows no tiene identificador propio en Unity (D-02 de la It. 1, aceptada por el PO en AC-APP-16-02): su identidad es el producto `RowMate` de `iguisoft` (datos en `AppData/LocalLow/iguisoft/RowMate`) y `com.iguisoft.rowmate` queda **reservado** para el paquete MSIX de la It. 14 (`AppIdentity.WindowsApplicationId`, que el bootstrapper no aplica). Q-85 decide los definitivos en la It. 14.
- **Q-91** (segundo plano): paridad, la sesión sigue (§6.1, ADR 017). Lo que no se puede igualar sin un servicio en primer plano de Android queda como riesgo residual RA-08 y, si la medición lo muestra, como propuesta de agregado.
- **QA-P-11** (miniaturas): por defecto a las 12:00, horneadas; la variante "hora actual" también horneada (§14.5).

### 22.1 Pedidos del Designer: dónde quedan cubiertos

| Pedido (ART_BIBLE, ANIMATION_BIBLE, ASSET_SLOTS) | Respuesta del Architect | Dónde |
|---|---|---|
| Versión con UI Shader Graph (6.3+ y URP) y Renderer Feature con la escena a 1/4 en `_RmHudBlur`; si no, vidrio plano | Confirmada: `6000.3.27f1` + URP 17.3. `HudBlurFeature` con `RTHandle` persistente; respaldo plano con el token `glass.fallback` (el Designer lo subió a `@86 %` por contraste) | §1.1, §7.4 |
| Fuente única de tokens + generador de `Tokens.uss` + test WCAG contra `#070D16 @86 %` sobre blanco | Adoptado | §7.7, ADR 016 |
| `VisualStrokeClock` sin tocar `SceneState` (D-ANI-01) | Adoptado | §14.3 |
| D-ANI-02 y D-ART-04: charcos en `BladeExit` (0,38) en la punta real; medidas reales; geometría legacy en Core para sus tests | Adoptado; el PO lo confirma en el gate de la It. 7 (por defecto, sí) | §14.3, ROADMAP §6 |
| D-ANI-03: remero y remos cabecean con el casco | Adoptado | §14.3 |
| D-ART-05: dos direcciones de sol con acimut común; `CelestialPlacement`, `HourLighting`, `MoonPhase` en Core | Adoptado (decide el Architect, gate de la 6a) | §14.3 |
| Agua: origen flotante, fases y desplazamientos de normal en `double`, textura de interacción RGBA8 256×200 sin estado, respaldo degradado | Adoptado | §14.2 |
| Asset slots con placeholder, validación y presupuesto; `SlotIds` en Core; `Assets/ThirdParty/` ignorado | Adoptado, con subtipos tipados sobre la base genérica | §2, §14.4, `.gitignore` |
| Import de Mixamo con *Optimize Game Objects* desactivado; Animation Rigging, Shader Graph y Vector Graphics; Cinemachine solo para BKL-02 | AR y SG en el manifest; íconos como glifos de "RowMate Icons" (lo que fijó después el Designer), sin paquete de Vector Graphics; Cinemachine fuera hasta BKL-02 | §1.2, §14.4 |
| Niveles Alto/Medio/Bajo como `ScriptableObject` + adaptación térmica con `FrameTimingManager` y el orden de degradación | Adoptado; resolución dinámica por `renderScale` con cambios poco frecuentes y respaldo por CPU | §20, ADR 018 |
| `TabularNumber` con celdas fijas y rodillo; PanelSettings a 160 DPI (1 px de USS = 1 dp) | Adoptado como **un** elemento por número | §7.3, §7.8 |
| Consulta nativa de "reducir movimiento" | Adoptado (`IAccessibilitySettings`) | §13.2 |
| D-ANI-06: eventos de incremento del contador de paladas; verificar con el AMS-670B | Adoptado; apagado hasta la It. 10a | §10.2 |
| Propuestas para el gate del PO: D-ART-01, D-ART-02, D-ART-03, D-ART-06, D-ART-07/D-ANI-07, D-ANI-04, D-ANI-05, D-ART-08 | Son del QUÉ: el PO las agendó en ROADMAP §6. Viabilidad técnica: todas entran en la arquitectura (D-ART-01 con las variantes `*.hud` de `Tokens`; D-ART-02 y D-ANI-05 dentro del tope de 60 elementos gracias a `TabularNumber`; D-ART-06 conserva la función legacy de las nubes en `SkyModel` para su test; D-ART-08 usa `Assets/ThirdParty/CC0/`; D-ART-07/D-ANI-07 suman Cinemachine recién en BKL-02) | ROADMAP §6 |

---

## 23. Fuentes

- Unity 6 releases y soporte (6.3 LTS hasta diciembre de 2027; 6.0 LTS hasta octubre de 2026): <https://unity.com/releases/unity-6-releases>
- Anuncio de Unity 6.3 LTS (2025-12-04): <https://unity.com/blog/unity-6-3-lts-is-now-available>
- Unity 6000.3.27f1 (publicada el 2026-10-20; última de la rama 6000.3): <https://unityreleases.com/releases/6000.3.27f1>
- Unity 6.7 Beta (2026-09-17, aún no LTS): <https://discussions.unity.com/t/unity-6-7-beta-is-now-available/1736830>
- Paquetes core de 6000.3 (URP 17.3, Shader Graph 17.3, Test Framework 1.6, uGUI 2.0, UI Test Framework 6.3): <https://docs.unity3d.com/6000.3/Documentation/Manual/pack-core.html>
- Paquetes liberados de 6000.3 (Input System 1.20, Localization 1.5, Animation Rigging 1.4, …): <https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html>
- Changelogs: Input System 1.20.1 (2026-10-05) <https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/changelog/CHANGELOG.html> · Localization 1.5.13 (2026-09-02) <https://docs.unity3d.com/Packages/com.unity.localization@1.5/changelog/CHANGELOG.html> · Animation Rigging 1.4.1 (2026-01-16) <https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.4/changelog/CHANGELOG.html> · Test Framework 1.6.0 <https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/changelog/CHANGELOG.html> · Newtonsoft 3.2.2 <https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/changelog/CHANGELOG.html> · VS Editor 2.0.28 <https://docs.unity3d.com/Packages/com.unity.ide.visualstudio@2.0/changelog/CHANGELOG.html> · Android Logcat 1.4.7 <https://docs.unity3d.com/Packages/com.unity.mobile.android-logcat@1.4/changelog/CHANGELOG.html>
- NUnit de Unity (`com.unity.ext.nunit` 2.0.5, basado en NUnit 3.5): <https://docs.unity3d.com/Packages/com.unity.ext.nunit@2.0/manual/index.html>
- UI Shader Graph (Unity 6.3, solo URP): <https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/ui-shader-graph.html>
- Deep linking en Unity 6.3 (iOS, Android, UWP, macOS, Web; no Windows Standalone): <https://docs.unity3d.com/6000.3/Documentation/Manual/deep-linking.html>
- Autenticación de Strava (`localhost` y `127.0.0.1` habilitados): <https://developers.strava.com/docs/authentication/>
- SQLite-net para Unity (1.3.3, MIT, binarios por plataforma): <https://github.com/gilzoide/unity-sqlite-net>
- Vector Graphics integrado en Unity 6.3 (SVG como *UI Toolkit Vector Image* sin paquete; el paquete 3.0 solo para *sprites* y uGUI): <https://docs.unity3d.com/Packages/com.unity.vectorgraphics@3.0/manual/index.html>
- `MeshGenerationContext` en 6000.3 (base de `TabularNumber`; `DrawText` a confirmar con Editor, RA-18): <https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/uielements/meshgenerationcontext>
- Oráculo Dart: Dart SDK 3.13.5 de `<flutter-sdk>` (vectores de ADR 012 §5, corridos el 2026-10-27 sin tocar el legacy; `git status` limpio)

---

## Cambios por QA (plan)

Correcciones por el [gate de armonía del plan](../QA_PLAN_REVIEW.md), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-P-01 | `.gitignore`: `**/StravaSecrets*.cs` (+ `.meta`), carpeta `Bootstrap/Generated/`, ruta única `Assets/ThirdParty/**` con sus `.meta` versionados (verificado con `git check-ignore` y `git add`), llaves y licencias. Higiene del repositorio en `ci-core.yml` (§19). RA-14 y RA-17 |
| QA-P-02 | §18.2 y ADR 012: `Fmt.Fixed` exacto, `DartMath.Round : long`, `RoundToFixed`, `DartToString`, prohibición de `Math.Round`/`ToString("F` en Core y App |
| QA-P-03 | §3.1: asmdef `RowMate.Tests.Support` (hermano, `UNITY_INCLUDE_TESTS`, sin API de Unity) referenciado por EditMode, Simulator y PlayMode; EditMode y Simulator con `includePlatforms: ["Editor"]`; §15.1: un `.csproj` por asmdef |
| QA-P-05 | §7.7 y ADR 016: `Core/Design/Tokens.cs` única fuente; `StepTypeInfo`/`MetricPalette` lo consumen; `tools/tokens` genera `Tokens.uss` con test de sincronía; test WCAG en `dotnet` |
| QA-P-06 | §6.1 y ADR 017: premisa corregida; keepalive nativo en segundo plano, eventos con marca de tiempo en *buffer* nativo (§10.4, modelo *pull*), reloj por tiempo monótono con alcance, guardado al pausar, `runInBackground` en escritorio; verificación en 4a/10a/10b. RA-08 y RA-19 |
| QA-P-07 | §13.1: `FramePacing`, 60 fps mientras hay transición, rodillo, corazón visible o dedo apoyado; 30 fps solo en reposo |
| QA-P-08 | §7.8: `TabularNumber` como un solo `VisualElement` con `generateVisualContent`; 0 B/cuadro con `TryFormatFixed`; test PlayMode de elementos y memoria. RA-18 |
| QA-P-10 | §20: una sola tabla con 3 niveles (Alto/Medio/Bajo) y los números del Designer; `QualityTierProfile`; adaptación térmica con `FrameTimingManager`, respaldo por CPU, `renderScale` con cambios cada ≥ 10 s; §16: el bootstrapper de la It. 1 no fija niveles. Nivel Bajo con Android 8+ (Q-90, QA-P-18) |
| QA-P-11 | §14.5: miniaturas horneadas; por defecto 12:00 (decisión del PO); "hora actual" = 4 horneadas (6/12/18/22 h) y la más cercana |
| QA-P-14 | §22: agua degradada en la 6a |
| QA-P-15 | §2: árbol único (terceros en `Assets/ThirdParty/`, slots en `Assets/RowMate/Slots/`, fuentes e íconos en `UI/`, shaders en `Art/Shaders/`); §14.4: `AssetSlotDefinition` + `AssetSlot<T>` con subtipos tipados y `SlotIds` en Core |
| QA-P-16 | §7.4: un nombre (`_RmHudBlur`), 3 pasadas Kawase, `RTHandle` persistente + global; prototipo en la 6b. Barra de pestañas sin blur |
| QA-P-19 | §9.2: fechas con microsegundos como Dart (E3); §15.2: `scene_view_test` = 4 × 2 × 2, repartido 4/12 entre las It. 6a y 8 (E2) |
| QA-P-20 | §12: `LocaleResolver` sobre la lista de idiomas preferidos; `ISystemLocale` nativo por plataforma |
| QA-P-21 | §13.2: `IAccessibilitySettings` (JNI, `SPI_GETCLIENTAREAANIMATION`, Swift en la 10b) |
| QA-P-23 | §15.1 y ADR 012 §4: `tools/oracle` y JSON dorado desde la It. 1 |
| QA-P-24 | §22: S-1 sin Unity, consola `net8.0` con P/Invoke a `RowMateBle.dll` |
| QA-P-25 | §16: bootstrapper mínimo en la It. 1 y completo en la It. 2; *lint* de asmdefs y manifest; lo de Unity marcado NO VERIFICABLE |
| QA-P-26 | §6: `TestMainThreadContext` e `InlineDbExecutor` en `Tests/Support`; sin `ConfigureAwait(false)` en Core/App |
| QA-P-28 | §14.2: `SceneState.ShaderDistance` portado como contrato con su test; el agua nueva no lo usa |
| Pedidos del Designer | §22.1 (cobertura completa) |
| R-14 (pedido del PO) | §16 y §9.3: ids provisionales de Apple `com.iguisoft.rowmate.unity` |
| QA-P-09 (alineación con el Designer) | §1.2: íconos como glifos de "RowMate Icons" con `Core/Design/Icons.cs`, sin `com.unity.vectorgraphics`; el módulo SVG integrado de 6.3 queda solo como opción con aprobación de QA |
| QA-P-17 (alineación con el Designer) | §7.4 y §20.1: el respaldo plano es el token `glass.fallback`, hoy `#070D16 @86 %` (ART_BIBLE §3.2); ya no se repite el valor `@80 %` |
| QA-P-29 (parte Architect, adelantada) | §1.2: Burst y Mathematics llegan transitivos con Animation Rigging; §14.1/§14.4: clases de escena en `RowMate.Scene`, nombres unificados con ANIMATION_BIBLE, `Animator` con pose de agarre y `AvatarBuilder` para el maniquí. El resto se detalla en el diseño de la It. 7 |

## Cambios de la It. 1 (diseño técnico e implementación de WP-A)

Decisiones de [02-tech-design](../iterations/01-fundaciones/02-tech-design.md) §10.2 y del gate de armonía, reflejadas en este documento.

| ID | Qué cambió |
|---|---|
| D-01 | §2 y §15.1: vectores dorados en `Assets/RowMate/Tests/Golden/oracle/`; `run-oracle.ps1` con `package_config` propio y guardia del legacy (porcelain, `--ignored` y metadatos) |
| D-02 | §16 (punto 1) y §22 (Q-85 / R-14): Windows sin identificador en el motor; `com.iguisoft.rowmate` reservado para el MSIX de la It. 14 (igual que AC-APP-16-02) |
| D-03 | §2 y §17: `IAppLogger` en lugar de `ILogger` |
| D-04 | §2 y §19: la higiene del repositorio son tests de `dotnet` en el mismo comando; no hay `tools/ci/` |
| D-06 | §12: `LocaleResolver` = `basicLocaleListResolution` de Flutter con `[en, es]` |
| D-11 | §6: `IClock.UtcNow`/`LocalNow` son `DateTimeOffset` |
| P-08 | RA-07: respaldo del vidrio = token `glass.fallback` (`#070D16 @86 %`) |
| QA-01, QA-03, QA-06, QA-10 | §15.1: `run-tests.ps1` = `dotnet build` de la solución + `dotnet test --no-build`; `RowMate.Tests.UnityApiCheck` contra NUnit 3.5.0 con Support por glob; fuentes de test sin `record`/`init`; formato de tabla y tope de 1 MiB de los vectores |

## Cambios de la It. 2 (deuda de la It. 1)

Issues de [05-qa-verification §6](../iterations/01-fundaciones/05-qa-verification.md) resueltos antes de construir la It. 2. Detalle en [00-deuda-it1.md](../iterations/02-dispositivo-simulador/00-deuda-it1.md).

| ID | Qué cambió |
|---|---|
| QA-V-01 | §5: la excepción de un suscriptor se registra y no corta a los demás; `ObservableValue` y `EventStream` reciben el `IAppLogger` por constructor (ADR 004) |
| QA-V-02 | §19 (higiene 1 y 2): `.gitignore` con `StravaSecrets*` de cualquier extensión; `RepoHygieneTests` mira también lo que `git add -A` agregaría |
| QA-V-03 | §16: un intento automático del *bootstrapper* por sesión del Editor (`SessionState`) |
| QA-V-04 | §2 y §17: el assembly `RowMate.Editor` usa el namespace `RowMate.EditorTools`; ningún namespace con segmento `Editor`; §19: `-executeMethod RowMate.EditorTools.BuildCli.Build` |
| QA-V-05 | §2, §4.2, §15.1 y §19 (higiene 6): `tools/unity/run-unity-tests.ps1 -Simulator`, `SimulatorDefine` (menú y tests) y `SimulatorBuildGuard` |

## Cambios de la It. 2 (primer open real)

Primer open del proyecto en Unity 6000.3.27f1 (R-T2). Detalle, comandos y logs en [00-primer-open.md](../iterations/02-dispositivo-simulador/00-primer-open.md).

| ID | Qué cambió |
|---|---|
| R-T2 | §16: en batchmode (`-quit`, `-executeMethod`, `-runTests`) el `delayCall` no corre antes de salir; el *bootstrapper* lo dispara `BootstrapPostprocessor` (`OnPostprocessAllAssets` con `didDomainReload`), con la misma marca de sesión. Versión 2: el renderer de URP lleva el `PostProcessData` de URP (sin él, URP apaga el postprocesado) |
| R-T5 | §15.1: `[Category("AC-…")]` resuelve a `RowMate.Tests.CategoryAttribute` (Tests/Support): el NUnit 3.5 de Unity invalida las categorías con guiones. Sin `[SetCulture]` (el Unity Test Framework no lo aplica) ni `Has.Count` (en 3.5 falla con arreglos detrás de `IReadOnlyList<T>`) |
| R-T12 | §15.1: Unity aplica `Assets/csc.rsp` (`-nullable:enable`) a todos los asmdefs, con `-langversion:9.0` |
| QA-V-05 | §15.1: `run-unity-tests.ps1 -Simulator` pone `ROWMATE_SIMULATOR_TEST_RUN=1` solo para la corrida de tests; así `CommittedSettingsDoNotEnableTheSimulator` pasa dentro de ella (Unity sale con 2 si hay un Inconclusive) |

## Cambios de la It. 2 (WP-B, implementación)

Detalle en [02-tech-design](../iterations/02-dispositivo-simulador/02-tech-design.md) §1-§4 y ADR 006, 007 y 019.

| Sección | Qué cambió |
|---|---|
| §2 | Asmdefs nuevos solo de Unity: `RowMate.Tests.UiSupport` (host de UI determinista, sin NUnit), `RowMate.Tests.PlayMode.Simulator` (con el define), `RowMate.Tests.UnityEditMode` y `RowMate.Snapshots` (arnés de capturas, Editor). `RowMate.Bootstrap` suma `Unity.InputSystem` ("Atrás") |
| §4.1 | `AppRoot` configura en `Awake` (cultura, registro, *scheduler*, plataforma, *backend* por `DeviceBackendFactory`, `AppComposer`, superficie con una copia del panel) y arma la UI en `Start` (`ShellHost`); `OnDestroy` dispone en orden inverso. Sin wakelock (D-08) |
| §7.2-§7.3 | UI en C# sin UXML; `ConstantPixelSize` × densidad; regla única de área segura; canal de color `GammaRt` (ADR 019) |
| §8 | `ViewBinding.Bind` (adjuntar/quitar del panel) sobre los *stores*; las pantallas ocultas siguen suscriptas |
| §13 | `FramePacer` aplica `FramePacingPolicy` a `OnDemandRendering.renderFrameInterval` (60/30 fps); `MotionDriver` arranca desarmado (nada se anima antes del primer cuadro presentado) |
| §16 | *Bootstrapper* v3: `urp.tidy`, `player.window`, `ui.fonts` (9 `FontAsset`), `ui.panel` (`RowMatePanel.asset`, `UiAssets.asset`), `scene.boot` (`Boot.unity`, única escena del build); `ProjectBootstrapper.ApplyFromCommandLine` para aplicarlo fuera del postprocesado de importación |
| §19 | `tools/build/build.ps1` → `BuildCli` (simulador por `extraScriptingDefines` y siempre Development; Mono temporal, D-16) y `BuildOutputInspector` en los builds normales. Arnés: `tools/unity/capture-snapshots.ps1` |
