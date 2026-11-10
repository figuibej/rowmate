# It. 14 — Pulido EXR, identidad, plataformas y CI (diseño técnico)

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-09 |
| Iteración | 14 · `14-pulido-plataformas` ([ROADMAP §It. 14](../../ROADMAP.md)), la última del plan |
| Entrada | [01-po-spec](01-po-spec.md) (44 AC, CAP-01 a -27, DV-128 a -139); [00-arte-pulido](00-arte-pulido.md) (D14-01 a -06, A14-01 a -12). **Si el arte contradice la spec, manda la spec** (§0.2) |
| Legacy | No se volvió a leer: alcanza con lo que cita la spec (§5). No se tocó el módulo de credenciales de Strava |
| Código leído (este repo) | `Editor/Bootstrap/{PlayerSettingsStep,ProjectBootstrapper}.cs`, `Editor/Build/{BuildCli,BuildRequest}.cs`, `Core/Common/AppIdentity.cs`, `Core/Ports/IHaptics.cs`, `Platform/Haptics/*`, `Platform/Display/WindowsTitleBar.cs`, `Assets/Plugins/Android/RowMateBle/RmBle.kt` (`haptic`), `Core/History/AxisScale.cs`, `App/Device/DeviceStore.cs` (reconexión), `Presentation/Styles/*.uss` (`letter-spacing`), `ProjectSettings/ProjectSettings.asset`, `.github/workflows/*.yml`, `tools/unity/{README.md,ci-simulator-define.sh,capture-snapshots.ps1}`, `tools/build/build.ps1`, `tools/dotnet/*` (proyectos), `Tests/EditMode/Project/{CiWorkflowTests,AppIdentityTests}.cs` |
| API de Unity verificada en el editor instalado (6000.3.27f1) | `UnityEngine.AccessibilityModule`: `AssistiveSupport` (`activeHierarchy`, `isScreenReaderEnabled`, `screenReaderStatusOverride`, `notificationDispatcher`, eventos `screenReaderStatusChanged` y `nodeFocusChanged`), `AccessibilityHierarchy` (`AddNode`, `RemoveNode`, `Clear`, `TryGetNodeAt`, `RefreshNodeFrames`), `AccessibilityNode` (`label`, `hint`, `value`, `role`, `state`, `frame`, `frameGetter`, `isActive`, eventos `invoked`, `focusChanged`, `scrolled`, `dismissed`, `incremented`, `decremented`), `AccessibilityRole` (Button, TabBar, TabButton, Header, StaticText, TextField, Toggle, ScrollView, Container, Image…), `AccessibilityState` (Selected, Disabled, Expanded), `IAccessibilityNotificationDispatcher` (`SendAnnouncement`, `SendScreenChanged`, `SendLayoutChanged`, `SendPageScrolledAnnouncement`). **No hay puente de UI Toolkit** (`UIElementsModule` no menciona accesibilidad): el árbol lo arma la app. El `UnityPlayer.dll` de Windows (mono, la única variante instalada) importa `UiaHostProviderFromHwnd`, `UiaRaiseNotificationEvent` y `UiaRaiseStructureChangedEvent`, así que **Narrador es alcanzable**. `UnityEngine.CoreModule` **no tiene tamaño mínimo de ventana en Windows** (solo `androidMinimumWindowWidth/Height`). Sí tiene `Screen.mainWindowDisplayInfo`, `Screen.MoveMainWindowTo` y `Screen.SetResolution`. En `UnityEditor.CoreModule` están `PlayerSettings.SetIcons` y `GetIconSizes(NamedBuildTarget, IconKind)`, `PlayerSettings.iOS.SetiPhoneLaunchScreenType` y `SetiPadLaunchScreenType` con `iOSLaunchScreenType.CustomStoryboard`, y `AndroidSplashScreenScale`. `AndroidPlatformIconKind` es del módulo de Android, que **no está instalado** |
| Estado | Diseño compacto, **sin código**, corregido según el gate ([03-qa-gate](03-qa-gate.md); ver "Cambios por gate" al final). Orden: **WP-A** → (**WP-B1** ∥ **WP-D**) → **WP-B2** (§11) |

Convenciones: las rutas son relativas a `Assets/RowMate/`, salvo `Assets/Plugins/`, `ProjectSettings/`, `native/`, `tools/`, `.github/` y `docs/`. **N** = nuevo, **C** = cambia.

---

## 0. Resumen

1. **El ícono sale del código, sin ningún archivo de terceros.** La geometría vive en Core como datos, con un rasterizador puro (8 × 8 muestras) y un PNG propio con **deflate propio**. No se usa `DeflateStream`, porque su zlib cambia entre .NET 8, Mono y la versión del runtime, y así los bytes son los mismos en la PC, en el CI y en Unity. `tools/icon` (dotnet) escribe todos los tamaños y las hojas CAP-01 y CAP-02. Un test de dotnet los vuelve a generar en memoria y los compara con los versionados, igual que se hace hoy con `Tokens.uss`.
2. **Identidad en `ProjectSettings` por un paso nuevo del bootstrapper (`identity`, versión 7).** Windows usa la API (`SetIcons`). Android (íconos adaptativos y arranque) se escribe con `SerializedObject`, porque falta el módulo; queda [Device-Android]. iOS usa un *storyboard* propio más un post-proceso de archivos; queda [Apple]. La lista exacta de campos está en §2.4.
3. **Arranque oscuro.** Una capa `BootSplashView` encima del shell muestra el fondo `bg.app` y la marca de 96 dp desde el primer cuadro. Cuando el shell está maquetado, se funde en 200 ms con `ease.exit` (120 ms lineal si está activo reducir movimiento). No agrega espera.
4. **Ventana de Windows desde C#, sin DLL nueva.** Una subclase del `WndProc` del player (`SetWindowLongPtrW`, sin asignar memoria) aplica el mínimo de 640 × 360 lógicos (`WM_GETMINMAXINFO`) y se traga el pitido de `Alt+Enter`. `F11` y `Alt+Enter` los maneja `WindowModeController`, con `allowFullscreenSwitch` en 0. En cada arranque la ventana se coloca centrada en 1280 × 720 lógicos.
5. **Lector de pantalla con la API real de 6.3.** `ScreenReaderBridge` arma un `AccessibilityHierarchy` recorriendo el árbol visual con **reglas por clase USS** (`A11yRules`). No toca los controles ni agrega `VisualElement`, y con el lector apagado no corre nada. El orden, los textos y la política de anuncios son Core puro.
6. **La auditoría de textos tiene dos capas.** Un escaneo estático de literales en C# y UXML (dotnet, con lista blanca versionada) y una auditoría en vivo del árbol visual: un recorrido PlayMode en `es` y `en`, más un gancho en cada toma del arnés. El clasificador es Core puro.
7. **Para el CI**, `ci-unity.yml` suma un job compuerta que lee los secretos, los tests, los builds de Windows y Android con GameCI y `BuildCli`, y un job manual de Apple sin firmar. Sin secretos, todo se saltea con un aviso y el workflow queda verde. Un parser YAML propio (`MiniYaml`) valida todos los workflows en `ci-core`.

### 0.1 Decisiones del Architect (una línea cada una)

| ID | Decisión |
|---|---|
| DT14-01 | `Core/Design/AppIcon*`: geometría como datos (§2.1), rasterizador `AppIconRaster` (8 × 8 muestras por píxel, "over" sobre sRGB con alfa recto, coordenadas en `double` y redondeo `DartMath.Round` a 8 bits) y `PngEncoder` (IHDR, IDAT y IEND; sin `tIME`, `gAMA` ni `tEXt`; filtro Paeth fijo en todas las filas). **Sin funciones trascendentes de la plataforma** (QA-G14-05): la ola usa `IconMath.Sin` (propio: reducción a [−π, π] con `Math.Round`, plegado a [−π/2, π/2] y polinomio impar de grado 11 por Horner, solo `+` y `*` en `double`, error < 1e-9; sin `Math.FusedMultiplyAdd`, y ni RyuJIT ni Mono fusionan solos). En `Core/Design/AppIcon*` e `IconMath` solo se permiten `Math.Sqrt`, `Math.Round`, `Math.Abs`, `Math.Min`, `Math.Max` y `Math.Floor`, que IEEE 754 fija bit a bit; lo exige `AppIconSourceScanTests` (dotnet) |
| DT14-02 | `DeflateEncoder` propio: bloques de Huffman fijo (BTYPE 01), LZ77 voraz con ventana de 32 KiB, hash de 3 bytes y cadena máxima de 64, Adler-32 a mano. Es determinista por construcción. Se valida descomprimiendo con `ZLibStream` (solo en `net8.0`) y con `Texture2D.LoadImage` (EditMode de Unity) |
| DT14-03 | **Colores: manda la spec.** Fondo **liso `#0D1B2A`** (`bg.app`) en todas las placas y en la capa de fondo adaptativa. Los remos en `accent.primary` `#00B4D8`. Del arte quedan el sol ámbar (degradé `step.warmup` → `metric.split`), las olas (W1 `accent.primary`, W2 `brand.seed`), el resplandor y los recortes. El degradé de fondo `#1A2E45` → `#0D1B2A` del arte se descarta |
| DT14-04 | **Nivel XS (≤ 24 px), corregido para cumplir "nada menor a 2 px a 16" (AC-APP-16-08):** solo los dos remos, sin sol, sin olas y sin recorte entre ellos (la cruz queda en una pieza). Caña r 7,5 y pala r 11 sobre la caja de 100, marca a 0,88: a 16 px la caña mide 2,1 px y la pala 3,1 px. Los niveles S y M quedan como en el arte §1.4 |
| DT14-05 | Tamaños en una tabla versionada `AppIconSizes` (Core). Un test de EditMode de Unity la compara con `GetIconSizes(Standalone, Application)`. Se generan además 24 y 64 para Windows (los pide la spec, CAP-01), aunque Unity no los meta en el `.ico` |
| DT14-06 | `tools/icon` (proyecto `tools/dotnet/RowMate.Icon`, como `RowMate.Tokens`): sin argumentos escribe; con `--check` sale con 1 si algo difiere. La comprobación de CI la hace el test `AppIconAssetsAreCurrent`, que corre en `ci-core` |
| DT14-07 | Paso del bootstrapper `identity` (v7, después de `player.window` y antes de `defines.check`), con 3 subpasos. `identity.import` fija los `TextureImporter` (Default, sRGB, `alphaIsTransparency`, sin mipmaps, sin compresión, `npotScale` None, `maxTextureSize` 2048). `identity.standalone` usa `SetIcons`. `identity.mobile` escribe Android e iOS con `SerializedObject` o con la API de core (§2.4). Es idempotente |
| DT14-08 | En macOS se usa la placa de Mac (arte §1.5) solo **al compilar para macOS**: `BuildCli` cambia los íconos de Standalone en memoria y los restaura en `finally` sin `SaveAssets`. Solo pasa en el job de Apple del CI ([Apple]) |
| DT14-09 | iOS: `iOSLaunchScreenType.CustomStoryboard` (API de core) con `Art/Identity/Apple/LaunchScreen.storyboard`, que tiene el fondo `#0D1B2A` y la imagen `RowMateLaunchMark` de 96 pt centrada. `ApplePostprocess` (`IPostprocessBuildWithReport`, solo con `BuildTarget.iOS`, sin el módulo de Xcode) copia `AppIcon.appiconset` (un único 1024 **RGB sin alfa**, el formato universal de Xcode 14 en adelante) y `RowMateLaunchMark.imageset`, y en `Info.plist` (System.Xml) deja iPhone sin vertical invertida y iPad con las 4 orientaciones |
| DT14-10 | El arranque usa `BootSplashView` (`Presentation/Shell`), que es la **última** hija de `AppShellView` (QA-G14-02; D2-05: en UI Toolkit el hijo de después se dibuja arriba): se agrega al final del constructor, después de `TopLayer`. Mientras viva, `AppShellView` le hace `BringToFront()` después de cualquier `Add` posterior (capas que se sumen más adelante), así nunca queda tapada. La textura es `Art/Identity/boot-mark.png` (384 px = 96 dp × 4, nivel M sin placa, escala 0,94 para que el radio máximo quede dentro de la caja). La referencia se carga en `UiAssets.BootMark` |
| DT14-11 | "Shell listo" significa el primer `GeometryChangedEvent` del contenido de la pestaña activa con ancho > 0, más un cuadro (para que el texto quede generado). Desde ahí corre `BootSplashMotion` (Core; 200 ms `ease.exit`, o 120 lineal con reducir movimiento). Al terminar, la capa sale de la jerarquía. No hay ningún tiempo mínimo |
| DT14-12 | La medición del arranque se hace en el **mismo** build de desarrollo con y sin capa (`-rowmate-boot-splash off`, solo en desarrollo): `[PERF] arranque: primer cuadro del shell {ms}`. `build.ps1 -BootSmoke` toma la mediana de 5 corridas de cada uno y exige una diferencia ≤ 100 ms (código 8). Ver el desvío D-02. **Aislamiento (QA-G14-07), la misma receta que `-ExportSmoke`:** build de DESARROLLO de la variante normal (`Builds/Windows/dev-smoke`, el build completo de la 14: QA-G14-20), una carpeta temporal propia `<temp>/RowMate.Tests/boot-smoke-<id>` como `ROWMATE_DATA_DIR` de las 10 corridas (borrada en `finally`), `ROWMATE_BLE=off` (sin adaptador ni reconexión) y `-screen-fullscreen 0 -screen-width 800 -screen-height 480`. Con `-rowmate-perf-boot`, el player escribe la línea y sale solo con `Application.Quit()` 1 s después; cada corrida tiene que terminar con código 0 en ≤ 30 s y sin carpeta nueva en `Crashes` (también cubre QA-G14-04) |
| DT14-13 | Ventana: lo puro va en `Core/Common/WindowGeometry` (rectángulo inicial centrado y recortado al área de trabajo, tamaño mínimo de marco con DPI e inserciones) y `WindowModeState` (ventana ↔ pantalla completa, rectángulo guardado, teclas). Lo de Unity va en `Platform/Display/WindowsWindowFrame` (P/Invoke) y en `WindowModeController` |
| DT14-14 | Subclase del `WndProc` con `SetWindowLongPtrW(GWLP_WNDPROC)` y `CallWindowProcW`. El delegado queda en un campo estático, con `[MonoPInvokeCallback]` por si algún día se usa IL2CPP. Atiende `WM_GETMINMAXINFO` (0x0024) llamando **primero** al original y después pisando solo `ptMinTrackSize` con `Marshal.WriteInt32` en los desplazamientos 24 y 28, sin asignar (QA-G14-14: así se conserva el máximo que calcula Unity), y `WM_SYSCHAR` con `VK_RETURN` (devuelve 0 para que no suene el pitido). El resto de los mensajes van al original. **Restauración (QA-G14-04):** `Uninstall()` vuelve a poner el original con `SetWindowLongPtrW(GWLP_WNDPROC, original)` solo si el procedimiento actual sigue siendo el nuestro (`GetWindowLongPtrW`), es idempotente y se llama desde `Application.quitting` (suscrito al instalar) y desde `AppRoot.OnDestroy`, siempre antes de que se apague el dominio de Mono; después, el manejador solo delega. La lógica de instalar y restaurar va sobre `IWndProcApi` (P/Invoke en el player, falso en EditMode). Solo corre con `UNITY_STANDALONE_WIN && !UNITY_EDITOR` |
| DT14-15 | La pantalla completa es `FullScreenMode.FullScreenWindow` con el tamaño de `Screen.mainWindowDisplayInfo`, que es el monitor donde está la ventana. Al volver: `SetResolution(w, h, Windowed)` y, en el cuadro siguiente, `SetWindowPos` al rectángulo guardado. `allowFullscreenSwitch` pasa a **0** para que Unity no tenga otro camino con `Alt+Enter`. `Esc` no se toca |
| DT14-16 | Ventana inicial (AC-APP-15-05): en `AppRoot.Awake`, en Windows, si el modo no es `Windowed` o el tamaño no es 1280 × 720 × escala, se aplica `WindowGeometry.Initial` (área de trabajo del monitor principal y `GetDpiForWindow`). Unity recuerda el último tamaño y el modo en su propio almacenamiento, pero nosotros **no leemos ni escribimos el registro**: lo pisamos al arrancar, y si queda un cuadro intermedio lo tapa la capa de arranque. **Excepción (QA-G14-07):** si la línea de comandos trae `-screen-width`, `-screen-height`, `-screen-fullscreen` o `-popupwindow`, no se reubica nada (lo usan los humos de `build.ps1` y QA). La decisión es pura: `WindowGeometry.ShouldPlaceInitial(args)` (dotnet). En macOS es 800 × 600 lógicos, con la misma excepción |
| DT14-17 | Accesibilidad: `Core/Accessibility/{A11yRole,A11yState,A11yItem,A11yOrder,A11yText,A11yAnnouncePolicy,A11yTreeDump}` es puro. `Presentation/Accessibility/{A11yRules,ScreenReaderBridge,IA11yAnnouncer,UnityA11yAnnouncer}` arma el `AccessibilityHierarchy` de Unity |
| DT14-18 | Reglas por clase, en un solo archivo: `.rm-button`, `.rm-icon-button`, `.rm-tab`, `.rm-appbar__title`, `.rm-metric-tile`, `.rm-hud-card`, `.rm-routine-card`, `.rm-history-card`, `.rm-scan-card`, `.rm-text-field`, `.rm-switch`, `.rm-toast`, `.rm-dialog__title` y `.rm-session-chart`, cada una con su rol, la fuente del nombre (texto, *tooltip* o etiqueta + valor) y su estado (`:disabled`, `.--selected`, `.--expanded`). Lo que no tiene regla no se lee: la escena y lo decorativo quedan afuera solos |
| DT14-19 | Solo con el lector encendido, y con **dos niveles de "sucio"** (QA-G14-06). (1) **Estructural** (navegación, apertura o cierre de diálogos, cambio de clase de ventana, elementos con regla que entran o salen o cambian de orden): se **reconcilia**, no se borra. `A11yReconciler` (Core, puro) compara la lista nueva con la anterior por clave estable (la identidad del `VisualElement`, en `Dictionary<VisualElement, AccessibilityNode>`) y produce `Keep`, `Insert(index)`, `Move(index)` y `Remove`, que el bridge aplica con `InsertNode`, `MoveNode` y `RemoveNode` (verificados en el editor instalado; `Clear()` solo al cambiar de raíz: diálogo ↔ shell). Si el nodo enfocado (`focusChanged` / `isFocused`) se conserva, sigue siendo **el mismo objeto** y no se manda nada; si su elemento salió, `SendLayoutChanged` al vecino más cercano en el orden. Como mucho una reconciliación cada 250 ms. (2) **Solo geometría** (`GeometryChangedEvent`, desplazamiento de una lista, el HUD que cambia valores): `RefreshNodeFrames()`, sin tocar el árbol ni asignar (QA-G14-21). Con un diálogo abierto, el árbol tiene **solo** su subárbol (el lector no sale del diálogo); al cerrarlo, `SendLayoutChanged(nodo que lo abrió)`. Los marcos se calculan con `frameGetter` (`worldBound` × escala del panel → píxeles de pantalla); la convención de ejes la confirma un test con `TryGetNodeAt` |
| DT14-20 | `invoked` → la misma acción del clic (se manda un `NavigationSubmitEvent` al elemento, que ya tiene ese manejador desde la It. 3). Las métricas actualizan `label` = `a11yLabelValue` una vez por segundo, solo con el lector encendido. **Nunca se anuncian solas** |
| DT14-21 | Anuncios con `IA11yAnnouncer` (en el player, `UnityA11yAnnouncer` sobre `notificationDispatcher`; en las pruebas, un falso que registra). Título nuevo al cambiar de pestaña o de página (`SendScreenChanged`); avisos una sola vez por `Show` (`A11yAnnouncePolicy` filtra los repetidos); cambio de paso (`a11yStepProgress`) y diálogo de completado. El gráfico del Detalle se lee con `a11yChartSummary` |
| DT14-22 | Pruebas del lector: `AssistiveSupport.screenReaderStatusOverride = ForceEnabled` y comparación del volcado `A11yTreeDump` (que es el mismo modelo que alimenta la jerarquía de Unity) y, donde Unity lo permite, de `activeHierarchy.rootNodes`. La restauración a `OSDriven` va en `TearDown` |
| DT14-23 | Háptica: `IHaptics.Play(HapticKind)`, con `Confirm` (lo de hoy), `Light`, `Medium` y `Success`. Mantengo `Confirm()` como atajo para no romper la It. 10a. `Core/Design/HapticPolicy` aplica el mínimo de 250 ms entre una y otra, `Success` tapa a `Medium` y no hay háptica en segundo plano. `App/Workout/WorkoutHaptics` escucha pausa, reanudar, cambio de paso (no el primero) y completado |
| DT14-24 | Android: `Assets/Plugins/Android/RowMateUi/RmHaptics.kt`, sin manifiesto ni permisos. `bind(activity)` se llama una vez con `UnityPlayer.currentActivity` desde C#, y `perform(kind)` en el hilo de UI: `CLOCK_TICK`, `VIRTUAL_KEY`, `CONFIRM` (API 30+) o, antes de API 30, `VIRTUAL_KEY` + 120 ms + `VIRTUAL_KEY`. `AndroidHaptics` pasa a usar `RmHaptics` también para `Confirm`, así que ya no depende de que el Bluetooth esté iniciado. `RmBle.haptic` queda como está, por su *golden* |
| DT14-25 | Auditoría de textos: `Core/Localization/VisibleTextClassifier` + `CatalogMatcher` (una regex por mensaje del idioma activo: literales escapados, `{param}` → `.+?` y variantes ICU expandidas). Las categorías son las (a) a (e) de AC-APP-11-01. Los subárboles con `.rm-audit-exempt` (panel y cinta del simulador, `BleDebugPanel`) no se miran |
| DT14-26 | Escaneo estático (`VisibleTextScanTests`, dotnet): busca literales con letras en `.text =`, `new Label(`, `tooltip =`, `SetText(`, `text="` (UXML) y `Title =` dentro de `Presentation/**`, `App/**` y `Bootstrap/**`. Las excepciones son las carpetas `Simulator*/` y `Presentation/Diagnostics/`, más la lista `tools/l10n/visible-text-allowlist.json` (`file`, `literal`, `reason`: `Q-03`, `Q-04`, `brand` o `format`) |
| DT14-27 | #44: `Core/Ble/BleErrorText.ForDisplay(detail)` quita un prefijo `^(scan|connect|discover|subscribe): ` antes de armar `Error al escanear: {detalle}`. Los servicios **no cambian sus mensajes**, así que siguen en verde las pruebas que los comparan (`HeartRateServiceScanTests` y las demás) |
| DT14-28 | #32 en `AxisTicks`: `DropCrowded(ticks, labelWidthsDp, minGapDp = 4)` saca la marca intermedia que queda a menos de 4 dp de la etiqueta de un borde. `SplitRange.Widen(min, max)`: si el rango es < 10 s, se abre a ±5 s del valor. Es Core y se prueba con dotnet |
| DT14-29 | #69: `Core/Design/ToastClock` (puro) acumula el tiempo restante y se pausa con `Hover` o `Focus`. `PathBreak.Insert(text)` mete un espacio de ancho cero (U+200B) después de cada `\` y nunca deja una sola letra al final de un renglón (en ese caso no rompe en el último `\`) |
| DT14-30 | #21: `Core/Design/SceneLoadingSpecs` define 120 ms de gracia, 160 ms de fundido y un indicador de 32 dp. **Corregido por QA-G14-03:** el indicador es **un solo** `Spinner(32)` con la clase `.rm-scene-loading` (posición absoluta, centrado en el área de la escena), hermano de `SceneSurface` dentro de `HudView`: se agrega justo **después** de `SceneSurface` (encima del fondo provisorio y de la escena, debajo del contenido del HUD). `SceneSurface` está en `display: none` mientras la escena no está lista, así que no puede ir adentro. Vive bajo `#immersive` y **sí cuenta**: `display: none` por defecto (0 elementos), +1 mientras se ve. `Workout/SceneLoadingController` (sin elementos) lo muestra solo si la escena no estaba `Ready` al entrar, a los 120 ms, y lo saca con un fundido de 160 ms cuando lo está. El peor caso del tope es +2 (el indicador que se funde y el fondo provisorio del fundido de 0,6 s de la escena demorada); lo mide `HudBudgetTests` (§10). La cámara arranca su entrada en ese momento, completa |
| DT14-31 | **Versión del `.exe` (QA-G14-01).** `Core/Common/VersionInfoBlock` (puro) arma el binario `VS_VERSIONINFO`: `VS_FIXEDFILEINFO` 1.0.0.0 (de `bundleVersion` "1.0.0" + 0), `StringFileInfo` `040904B0` con `CompanyName` iguisoft, `FileDescription` RowMate, `FileVersion` 1.0.0, `InternalName` RowMate, `LegalCopyright` © iguisoft, `OriginalFilename` RowMate.exe, `ProductName` RowMate y `ProductVersion` 1.0.0, y `VarFileInfo` `Translation` 0x0409/1200; alineación a 4 bytes y claves en UTF-16. `Editor/Build/WindowsVersionInfoPostprocess` (`IPostprocessBuildWithReport`, solo `StandaloneWindows64`, al final) lo escribe en `RowMate.exe` con `BeginUpdateResourceW` / `UpdateResourceW(RT_VERSION, 1, idioma)` / `EndUpdateResourceW`, reemplazando el recurso en cada idioma que traiga (`EnumResourceLanguagesW`; hoy 0x0409). Los valores salen de `PlayerSettings` (`productName`, `companyName`, `bundleVersion`), no se escriben a mano. Solo corre si el editor es de Windows; si falla, `BuildFailedException` (build rojo). En el CI de Linux (GameCI) no hay `UpdateResource`: se saltea con `::warning::` "VERSIONINFO sin reescribir" y va a issue (escritor PE puro). El `.exe` de Unity 6000.3.27f1 no viene firmado (comprobado), así que no se invalida ninguna firma. Lo comprueba `build.ps1` después de **todo** build de Windows (normal, simulator y desarrollo): lee `(Get-Item RowMate.exe).VersionInfo` y exige `ProductName` RowMate, `CompanyName` iguisoft, `FileVersion` y `ProductVersion` 1.0.0, o sale con código 9. `UnityPlayer.dll` queda con la versión del motor |

### 0.2 Pedidos del arte (A14-xx): una línea cada uno

| Pedido | Decisión |
|---|---|
| A14-01 | **Adoptado, con un cambio**: se usa el deflate propio de DT14-02 en lugar de `DeflateStream` (el zlib cambia entre runtimes). Los colores son los de DT14-03 y el XS el de DT14-04 (manda la spec) |
| A14-02 | **Adoptado** (DT14-05 y -06). Las hojas pasan a ser **CAP-01 y CAP-02** de la spec, en lugar de `ICON-01` |
| A14-03 | **Adoptado, en parte**: Standalone por API; Android (adaptativo, redondo, *legacy* y la imagen de arranque con "centrar") por `SerializedObject`; iOS con *storyboard* y post-proceso. La capa **monocroma se rechaza** (spec §10). El fondo del arranque de Android 12+ no tiene API de core: queda el color de `SplashScreen.backgroundColor` (ya es `#0D1B2A`) y es [Device-Android] |
| A14-04 | **Adoptado con los números de la spec**: marca de 96 dp (no 160) y una sola salida de 200 ms `ease.exit` (no la cascada de 420). El indicador a los 1 500 ms se mantiene (no agrega espera) |
| A14-05 | **Adoptado, salvo la cascada**: el HUD entra en un solo bloque de 200 ms `ease.enter` (manda AC-APP-15-10). También van: ningún cuadro estirado, la regla de 400 ms, el ancla de desplazamiento, F11 y el mínimo con `WM_GETMINMAXINFO` hecho desde C# (DT14-14) |
| A14-06 | **Adoptado** (DT14-23 y -24), con la háptica de reconexión de #51 (AC-DEBT-10), que el arte dejaba afuera |
| A14-07 | **Adoptado**: `TabularAuditTests` (AC-POL-02), TY-01 y TY-02 en WP-D |
| A14-08 | **Adoptado**: `.rm-type-label--mixed` (`letter-spacing: 4px`, el equivalente de 0,04 em en UI Toolkit) **por defecto solo en las pestañas**; en los rótulos del HUD únicamente si las sondas de la 4b siguen en verde sin mover nada más de 1 dp (QA-G14-17) |
| A14-09 | **Rechazado, a issues**: no tiene AC y toca la geometría del HUD, que ya está verificada |
| A14-10 | **Rechazado, a issues**: la spec no lo incluye (§11: #57) |
| A14-11 | **Adoptado como conjunto `it14` con los IDs de la spec** (CAP-01 a -27), más VID-01 (transiciones) y VID-02 (rotación TV → TH) como evidencia de AC-POL-01 y AC-APP-15-10 |
| A14-12 | **Adoptado** (WP-D, `ASSET_SLOTS.md`) |

### 0.3 Desvíos propuestos (al gate)

| ID | Desvío | Por qué |
|---|---|---|
| D-01 | AC-APP-11-01: el gancho de auditoría corre en **toda** toma de **todo** conjunto, pero QA corre `it2`, `it3`, `it9`, `it10a`, `it11`, `it12` e `it14`. Las pantallas del HUD y de la escena de `it4b` a `it8` las cubre el recorrido PlayMode `TextAuditTourTests`, **ampliado** con `EnvironmentPicker`, `SeriesPanelView`, `CenterCallouts`/`Próximo:` y los diálogos de inicio y fin con sus variantes (§4), y `TextAuditProbe` también corre en `DemoWalkthroughTests` (los 4 escenarios). **Aceptado por el gate con esa condición (QA-G14-08)** | Volver a tomar `it6a` a `it8` cuesta más de 2 h de escena real sin agregar ningún texto nuevo |
| D-02 | AC-APP-16-10: "no crece más de 100 ms frente a la It. 12" se mide como la diferencia con y sin la capa, en el mismo build (DT14-12) | No hay un build de la It. 12 guardado; lo único que agrega la iteración es la capa |
| D-03 | AC-APP-16-08, iOS: un único 1024 universal en lugar de toda la grilla de tamaños | Xcode 14 en adelante lo acepta y los genera |
| D-04 | AC-APP-15-09, macOS "mínimo 640 × 360": queda NO VERIFICABLE y pasa a la 10b (código nativo Swift) | Unity no tiene API para eso y no hay Mac |

---

## 1. Árbol de archivos

### 1.1 WP-A: puro (dotnet)

```
Core/Design/AppIconGeometry.cs  AppIconLevel.cs  AppIconRaster.cs  AppIconSizes.cs  N  marca, niveles M/S/XS, placas (§2.1)
Core/Design/PngEncoder.cs  DeflateEncoder.cs  Adler32.cs  Crc32.cs  IconMath.cs  N  PNG determinista; seno propio (DT14-01/02; QA-G14-05)
Core/Common/VersionInfoBlock.cs                                                  N  VS_VERSIONINFO del .exe (DT14-31)
Core/Accessibility/A11yReconciler.cs                                             N  diff por clave estable (DT14-19)
Core/Design/BootSplashMotion.cs  SceneLoadingSpecs.cs  HapticPolicy.cs  ToastClock.cs  PathBreak.cs  N
Core/Ports/IHaptics.cs  HapticKind.cs                                            C/N  Play(kind) + Confirm()
Core/Common/WindowGeometry.cs  WindowModeState.cs                                N  (§3)
Core/Accessibility/A11yRole.cs  A11yState.cs  A11yItem.cs  A11yOrder.cs  A11yText.cs  A11yAnnouncePolicy.cs  A11yTreeDump.cs  N  (§5)
Core/Localization/VisibleTextClassifier.cs  CatalogMatcher.cs  AuditContext.cs   N  (§4)
Core/Localization/L10nKeys.cs   Localization/strings.{es,en}.json               C  +4 claves (256)
Core/Ble/BleErrorText.cs                                                         N  #44
Core/History/AxisScale.cs                                                        C  DropCrowded, SplitRange.Widen (#32)
App/Workout/WorkoutHaptics.cs                                                    N  pausa, reanudar, paso, completado
App/Device/DeviceStore.cs                                                        C  Confirm al reconectar (#51)
App/Composition/AppComposer.cs  AppContext.cs                                    C  WorkoutHaptics
tools/icon/Program.cs   tools/dotnet/RowMate.Icon/RowMate.Icon.csproj   tools/dotnet/RowMate.sln  N/C
Art/Identity/**/*.png   docs/…/14-pulido-plataformas/snapshots/CAP-01.png  CAP-02.png  N  (salida de tools/icon, §2.2)
tools/l10n/visible-text-allowlist.json                                           N
Tests/Support/MiniYaml.cs  FakeHaptics.cs (C: registra HapticKind)               N/C
Tests/EditMode/Design/{AppIconGeometry,AppIconRaster,PngEncoder,DeflateEncoder,AppIconAssetsAreCurrent,AppIconHashes,AppIconSourceScan,IconMath,BootSplashMotion,SceneLoadingSpecs,HapticPolicy,ToastClock,PathBreak}Tests.cs  N
Tests/EditMode/Common/{WindowGeometry,WindowModeState,VersionInfoBlock}Tests.cs  N
Tests/EditMode/Accessibility/{A11yOrder,A11yText,A11yAnnouncePolicy,A11yTreeDump,A11yReconciler}Tests.cs  N
Tests/EditMode/Localization/{VisibleTextClassifier,CatalogMatcher,VisibleTextScan}Tests.cs  N;  catálogo existente C (256)
Tests/EditMode/Ble/BleErrorTextTests.cs   Tests/EditMode/History/AxisCrowdingTests.cs  N
Tests/EditMode/Workout/WorkoutHapticsTests.cs   Tests/EditMode/Device/DeviceStoreReconnectHapticTests.cs  N
Tests/EditMode/Project/MiniYamlTests.cs                                         N  el parser (casos válidos e inválidos)
Tests/EditMode/Traceability/It14Ids.cs                                           N
```

### 1.2 WP-B: Unity (B1: identidad, plataforma, lector y deuda; B2: CI, arnés y builds)

```
B1  Editor/Bootstrap/IdentityStep.cs                       N  identity.import/.standalone/.mobile (§2.4)
B1  Editor/Bootstrap/ProjectBootstrapper.cs  PlayerSettingsStep.cs  C  v7; allowFullscreenSwitch 0; requiresFullScreen false
B1  Editor/Build/ApplePostprocess.cs                       N  appiconset, launch mark, Info.plist (DT14-09)
B1  Editor/Build/WindowsVersionInfoPostprocess.cs          N  VS_VERSIONINFO en RowMate.exe (DT14-31)
B1  Art/Identity/Apple/LaunchScreen.storyboard  AppIcon.appiconset/Contents.json  RowMateLaunchMark.imageset/Contents.json  N
B1  Bootstrap/UiAssets.cs  AppRoot.cs                      C  BootMark; WindowPlacement; WindowModeController; ScreenReaderBridge; -rowmate-boot-splash, -rowmate-perf-boot
B1  Platform/Display/WindowsWindowFrame.cs  WindowModeController.cs  N  (§3)
B1  Platform/Haptics/AndroidHaptics.cs  NoHaptics.cs  HapticsFactory.cs  C  Play(kind) con RmHaptics
B1  Assets/Plugins/Android/RowMateUi/RmHaptics.kt          N
B1  native/android/check.ps1   native/android/golden/ui-signatures.txt  C/N
B1  Presentation/Shell/BootSplashView.cs  AppShellView.cs  N/C  capa, señal de "listo" (DT14-10/11)
B1  Presentation/Accessibility/A11yRules.cs  ScreenReaderBridge.cs  IA11yAnnouncer.cs  UnityA11yAnnouncer.cs  N  (§5)
B1  Presentation/Shell/ToastLayer.cs  ActionToast.cs        C  ToastClock (pausa con hover/foco, #69), PathBreak, anuncio
B1  Presentation/Device/LinkBanner.cs  ScanView.cs  DeviceScreenView.cs  ScanResultList.cs  ErrorBanner.cs  C  #40 #41(C#) #42 #44
B1  Presentation/Workout/SceneLoadingController.cs  ImmersiveWorkoutPage.cs  N/C  #21; anuncio de paso y de completado
B1  Presentation/Workout/HudView.cs                        C  SOLO el bloque del constructor que agrega el Spinner de #21 después de SceneSurface (DT14-30); B1 lo toca antes que WP-D, nunca a la vez
B1  Tests/UiSupport/UiHostOptions.cs  UiTestHost.cs  FakeA11yAnnouncer.cs  C/N  lector forzado, anuncios, háptica falsa, tamaño en vivo
B1  Tests/PlayMode/It14/{BootSplash,ScreenReaderTree,ScreenReaderAction,ScreenReaderAnnouncement,ScreenReaderCost,TextAuditTour,LegacyFixedTexts,WindowMode,MinWindowLayout,ResizeContinuity,HapticEvents,LinkBannerName,SearchingButton,DeviceLandscapeScroll,ScanErrorText,ExportToastPolish,SceneLoading,DemoWalkthrough}Tests.cs  N
B1  Tests/UnityEditMode/{IdentityStep,AppIconImport,WindowsWindowFrame,ApplePostprocess}Tests.cs  N
B1  Tests/EditMode/Project/ProjectSettingsIt14Tests.cs     N  valores de §2.4 leídos del YAML (dotnet)
B2  .github/workflows/ci-unity.yml                         C  (§8)
B2  Editor/Build/BuildCli.cs  BuildRequest.cs  BuildOutputInspector.cs  C  iOS/macOS; firma de Android por argumentos; ícono de Mac; argumentos nuevos de desarrollo
B2  Tests/EditMode/Project/WorkflowYamlTests.cs  CiUnityWorkflowTests.cs  N  YAML genérico sobre todos los workflows (§8.3); gate, builds, artefactos, retención (dotnet). Van en B2 porque el ci-unity.yml de hoy no tiene permissions ni timeout-minutes
B2  Tests/EditMode/Project/ItFourteenToolingTests.cs       N  README "It. 14: CI", carpeta del set it14, -BootSmoke, sin rutas de la PC (dotnet)
B2  Tests/EditMode/Project/CiWorkflowTests.cs              C  lo de ci-core que siga vigente
B2  Tests/EditMode/Project/ItTwoDebtTests.cs  ItTwoToolingTests.cs  C  R-01 reescrita, no borrada (§8.4; QA-G14-09)
B2  Tests/EditMode/Project/{ItFourAToolingTests,ItFourBToolingTests,ItSixAToolingTests}.cs  C  ValidateSet y $Set por defecto → it14 (QA-G14-12)
B2  Snapshots/PolishCatalog.cs  TextAuditProbe.cs  SnapshotRunner.cs  SnapshotCatalog.cs  N/C  set it14 y gancho de auditoría
B2  tools/unity/capture-snapshots.ps1  tools/unity/README.md  tools/build/build.ps1  C  -Set it14 (por defecto); "It. 14: CI"; -BootSmoke
B2  docs/migration/architecture/adr/025-identidad-ventana-y-ci.md  026-lector-de-pantalla.md  ARCHITECTURE.md  N/C
```

### 1.3 WP-D: Designer

```
Presentation/Styles/Skin.uss  Base.uss  Motion.uss  RowMateTheme.tss    C  TODA regla USS de la iteración (#41, #69, #36, #37, tracking, splash, indicador)
Presentation/Motion/{TabTransition,PageTransition,PopTransition,DialogTransition,ToastTransition,BleMotion}.cs  C  inventario T-01 a T-07 (§6.1)
Presentation/Motion/LayoutClassEntrance.cs  Presentation/Controls/ScrollAnchor.cs  N  HUD 200 ms; ancla de desplazamiento
Presentation/Workout/HudView.cs                                        C  usa LayoutClassEntrance (regla de los 400 ms)
Presentation/Shell/TabBarView.cs                                       C  .rm-type-label--mixed
Presentation/History/{StepBreakdownView,HistoryCardView,HistoryListView,HistoryScreenView,SessionChartView,ChartAxisLabels,SessionDetailPage,StatRowView}.cs  C  TY-01, TY-02, #36, #32 (vista), #37
Presentation/Routines/RoutineListView.cs                               C  ScrollAnchor
Tests/PlayMode/It14/{TransitionInventory,TabularAudit,FontAudit,CompletionDialogScale,HistoryScale,ChartArt}Tests.cs  N
docs/migration/design/ART_BIBLE.md  ANIMATION_BIBLE.md  ASSET_SLOTS.md   C  lo aceptado; slot app.icon
docs/…/14-pulido-plataformas/04-design-notes.md                       N  y revisión de capturas
```

**Archivos disjuntos.** WP-B no escribe USS: cuando necesita estilo, pide la clase a WP-D (§7). WP-D no toca `Device/*`, `Shell/{ToastLayer,ActionToast,AppShellView,BootSplashView}` ni `Workout/` salvo `HudView`. Los tests de cada paquete van en archivos propios.

---

## 2. Identidad

### 2.1 Geometría (Core, la caja de 100 del arte §1.3)

- Primitivas: `Stadium(Hx, Hy, Tx, Ty, s0, s1, r)`, `HalfDisc(cx, cy, r, yMax)`, `SineStroke(y0, amp, x0, x1, period, phase, width)`, `RadialGlow(cx, cy, r, alpha0)` y `RoundedPlate(margin, radius)`. Cada pieza lleva su color (`Rgb` de `Tokens`) y la operación `Paint` o `KnockOut`.
- `AppIconGeometry.Mark(level)` devuelve la lista ordenada de piezas: el arte §1.3 para M, §1.4 para S y DT14-04 para XS. `Variant`: `Plate` (Windows/*legacy*), `Bleed` (iOS, opaco), `AdaptiveBackground`, `AdaptiveForeground` (marca de 60 dp centrada en 108, sin resplandor), `Round` (disco de 0,92), `MacPlate` (824 en (100;100), radio 185, sombra `0 10 28` negra al 30 %) y `BootMark` (sin placa, escala 0,94).
- `AppIconLevel.For(px)`: M si es ≥ 49, S entre 25 y 48, XS si es ≤ 24.
- Datos que se prueban: cruce en (50; 47,76) ± 0,01; radio máximo de M ≤ 52,8; el frente adaptativo entra en el círculo de 33 dp; a 16 px ninguna caña, pala ni hueco mide menos de 2 px.

### 2.2 Salidas de `tools/icon`

```
Art/Identity/Standalone/icon-{16,24,32,48,64,128,256,512,1024}.png     placa (RGBA)
Art/Identity/Android/adaptive-bg-{432,324,216,162,108,81}.png          liso #0D1B2A
Art/Identity/Android/adaptive-fg-{432,324,216,162,108,81}.png          marca transparente
Art/Identity/Android/round-{192,144,96,72,48,36}.png   legacy-{192,…,36}.png
Art/Identity/Android/splash-mark.png                                    384 px, marca sin fondo (arranque estático de 8 a 11)
Art/Identity/boot-mark.png                                              384 px (DT14-10)
Art/Identity/Apple/AppIcon.appiconset/icon-1024.png                     RGB, sin alfa
Art/Identity/Apple/RowMateLaunchMark.imageset/mark@{1,2,3}x.png         96 / 192 / 288 px
Art/Identity/Apple/macOS/icon-{16,32,64,128,256,512,1024}.png           placa de Mac
docs/…/14-pulido-plataformas/snapshots/CAP-01.png                      maestro 1024 y 256…16 sobre #F2F2F2 y #000000
docs/…/14-pulido-plataformas/snapshots/CAP-02.png                      fondo + frente con máscaras círculo, squircle y cuadrado redondeado, y la zona segura de 66 dp marcada
```

`AppIconAssetsAreCurrent` vuelve a generar todo en memoria y compara byte a byte. Con dos corridas seguidas de `tools/icon` no hay diff. **`AppIconHashesTests`** (QA-G14-05) fija en el código el SHA-256 de `icon-16.png` y de `icon-1024.png` (`System.Security.Cryptography.SHA256`, netstandard2.1): si una plataforma del CI (ubuntu o windows) da otros bytes, la falla lo dice con nombre. Un cambio a propósito del ícono actualiza los dos hashes en el mismo cambio. El repo lleva los PNG **sin LFS** (son chicos, pasa lo mismo con la DLL en `.gitattributes`; `ci-core` no baja LFS). WP-A agrega las reglas `Assets/RowMate/Art/Identity/**/*.png -filter -diff binary` y `docs/migration/iterations/14-pulido-plataformas/snapshots/CAP-0[12].png -filter -diff binary`, después de la de `*.png` (QA-G14-16: el test compara también CAP-01 y CAP-02), y se versionan los `.meta` de `Art/Identity`, porque `ProjectSettings` referencia sus GUID.

### 2.3 Arranque

| Plataforma | Antes del primer cuadro | Primer cuadro de la app |
|---|---|---|
| Windows | Sin el splash de Unity (`m_ShowUnitySplashScreen: 0`, ya está). La ventana del player arranca en negro y en el cuadro 0 la cámara de `Boot` limpia con `bg.app` (AC-APP-01-07) | `BootSplashView`: fondo `bg.app` y marca de 96 dp centrada (sin área segura); después `BootSplashMotion` |
| Android 8–11 | Imagen estática de Unity: `splash-mark.png`, escala "centrar", sobre `#0D1B2A` | ídem |
| Android 12+ | Arranque del sistema: el ícono adaptativo y el fondo de `SplashScreen.backgroundColor` | ídem ([Device-Android]) |
| iOS | `LaunchScreen.storyboard` (DT14-09) | ídem ([Apple]) |
| macOS | — | ídem ([Apple]) |

`BootSplashMotion` (Core): `Opacity(tMs, reduceMotion)` = 1 → 0 con `ease.exit` en 200 ms, o lineal en 120 ms. Si pasan 1 500 ms sin que el shell esté listo, aparece el `Spinner(32)` 32 dp debajo de la marca, con un fundido de 160. La capa bloquea la entrada (`pickingMode` Position) hasta que empieza la salida. Es la última hija del shell (DT14-10): `BootSplashTests` comprueba en el cuadro 0 que el elemento que resuelve `panel.Pick` en el centro es la capa (o un hijo) y que el píxel central de la captura es el de la marca sobre `#0D1B2A`, no el del shell.

### 2.4 `ProjectSettings`: los cambios exactos (campo → valor)

Los escribe **solo** `identity` y `player.window` (bootstrapper v7). Cualquier otra diferencia en `git diff ProjectSettings/` es un defecto (AC-POL-07).

| Plataforma | Campo (`ProjectSettings.asset`) | Antes → después | Cómo |
|---|---|---|---|
| Windows y macOS | `m_BuildTargetIcons` | `[]` → entrada `Standalone` con un ícono por cada tamaño de `GetIconSizes(Standalone, Application)` (se esperan 1024, 512, 256, 128, 48, 32 y 16) | `PlayerSettings.SetIcons` |
| Windows | `allowFullscreenSwitch` | `1` → `0` (DT14-15) | API |
| Windows | `defaultScreenWidth/Height`, `resizableWindow`, `fullscreenMode` | sin cambios: 1280, 720, 1, 3 (Windowed) | se verifican |
| Android | `m_BuildTargetPlatformIcons` | `[]` → `m_BuildTarget: Android` con: `m_Kind: 2` (Adaptativo) en 432, 324, 216, 162, 108 y 81, con 2 texturas (fondo y frente); `m_Kind: 1` (Redondo) y `m_Kind: 0` (*Legacy*) en 192, 144, 96, 72, 48 y 36 | `SerializedObject` (sin módulo; R14-05) |
| Android | `androidSplashScreen` | `{fileID: 0}` → GUID de `splash-mark.png` | `SerializedObject` |
| Android | `AndroidSplashScreenScale` | `0` (centrar), sin cambios | se verifica |
| Android | `AndroidMinSdkVersion` 26, `AndroidTargetArchitectures` 2, `androidAutoRotationBehavior` 1 (respeta el bloqueo), `androidResizeableActivity` 1, `allowedAutorotateTo*` 1 (los 4), `defaultScreenOrientation` 4, `applicationIdentifier.Android` `com.iguisoft.rowmate`, `AndroidBundleVersionCode` 1 | sin cambios | se verifican |
| iOS | `iOSLaunchScreenType` / `iOSLaunchScreeniPadType` | `0` → `CustomStoryboard` | `PlayerSettings.iOS.SetiPhoneLaunchScreenType` / `SetiPadLaunchScreenType` |
| iOS | `iOSLaunchScreenCustomStoryboardPath` y `iOSLaunchScreeniPadCustomStoryboardPath` | vacío → `Assets/RowMate/Art/Identity/Apple/LaunchScreen.storyboard` | `SerializedObject` |
| iOS | `uIRequiresFullScreen` | `1` → `0` (multitarea en iPad, AC-APP-15-09) | `PlayerSettings.iOS.requiresFullScreen` |
| iOS | `applicationIdentifier.iPhone` `com.iguisoft.rowmate.unity`, `buildNumber.iPhone` 1, `bundleVersion` `1.0.0` | sin cambios | se verifican |
| Todas | `productName` RowMate, `companyName` iguisoft, `m_SplashScreenBackgroundColor` `#0D1B2A`, `m_ShowUnitySplashScreen` 0, `scriptingDefineSymbols` sin `ROWMATE_SIMULATOR` | sin cambios | se verifican |
| — | `ProjectSettings/RowMateBootstrap.json` | `version` 6 → 7 | bootstrapper |

Después de `Reaplicar`: si `HorizonFar.mat` o `HorizonNear.mat` cambiaron, `git checkout` (#55) y se anota. `ProjectSettingsGuard` sigue en todos los scripts.

---

## 3. Ventana y orientaciones

| Qué | Cómo |
|---|---|
| Inicial (AC-APP-15-05) | `WindowGeometry.Initial(workArea, dpiScale, 1280, 720)`: tamaño lógico × escala, centrado en el área de trabajo del monitor principal y recortado a ella. `WindowsWindowFrame.Place(rect)` usa `AdjustWindowRectExForDpi` + `SetWindowPos`. No se aplica si llegan `-screen-*` o `-popupwindow` (`ShouldPlaceInitial`, DT14-16). En macOS, `Screen.SetResolution(800, 600, Windowed)`. Cerrar = salir (lo que Unity ya hace), con el `WndProc` original restaurado antes (DT14-14) |
| Mínimo (AC-APP-15-06) | `WindowGeometry.MinTrack(dpi, frameInsets)` = 640 × 360 lógicos más el marco. El `WndProc` escribe `ptMinTrackSize` (DT14-14). El DPI se lee en cada mensaje con `GetDpiForWindow`, que es barato y no asigna |
| Pantalla completa (AC-APP-15-07) | `WindowModeController.Update()` (en `AppRoot.Update`, solo en el player de Windows): `Keyboard.current.f11Key.wasPressedThisFrame` o `enterKey` + `altKey` → `WindowModeState.Toggle(rectActual)` → la acción de DT14-15. No se guarda nada entre arranques |
| DPI con dos monitores (AC-APP-15-08) | Lo maneja Unity (`WM_DPICHANGED`). `DisplayMetricsProvider` ya relee el DPI al cambiar el tamaño. Lo verifica el usuario en la demo |
| Orientaciones (AC-APP-15-09) | Android: los campos de §2.4 (sin cambios). iPhone sin vertical invertida e iPad con 4 orientaciones y multitarea: `ApplePostprocess` en `Info.plist` y `uIRequiresFullScreen` 0; además, como `UnityAppController` manda sobre `Info.plist`, `AppRoot.Awake` pone `Screen.autorotateToPortraitUpsideDown = false` en iOS si `UnityEngine.iOS.Device.generation` es un iPhone (QA-G14-10, [Apple]). macOS: mínimo a la 10b (D-04) |
| Fluido (AC-APP-15-10) | `PanelScaleController` mira el tamaño de `Screen` **antes** del `Update` de UI Toolkit (orden de ejecución -1000), así que el cuadro que se presenta ya tiene el *layout* nuevo y la cámara su aspecto nuevo (la escena ya se adapta en el acto desde la 6a). Con un cambio de clase, el HUD usa `LayoutClassEntrance`: 200 ms `ease.enter`, opacidad 0 → 1 y +8 dp → 0, sin elementos nuevos (solo `style.opacity` y `translate`). Si hay otro cambio de clase antes de 400 ms, se aplica en el acto. Fuera del HUD, `ScrollAnchor` guarda el índice y el desvío del primer hijo visible antes del cambio y los repone en el `GeometryChangedEvent`. Con reducir movimiento, todo en el acto |

---

## 4. Textos (F-APP-11)

- **Catálogo (AC-APP-11-03).** Las 4 claves de la spec §4 llevan las dos traducciones idénticas a la tabla. `CatalogParityTests` (`:225-227`) y `L10nKeysTests` (`:69`) pasan de 252 a 256 (QA-G14-11). Si WP-B encuentra un control de solo ícono sin nombre, reusa la clave de su ayuda. Si no hay ninguna, la agrega con su par es/en y anota el total nuevo en el `REPORT`.
- **Clasificador (Core).** `VisibleTextClassifier.Classify(text, AuditContext)` devuelve `Catalog`, `Number`, `UserData`, `Brand`, `Exception` o `Violation`. `Number`: sin letras, o letras que son solo de las unidades y abreviaturas que salen del catálogo (las saca `CatalogMatcher.UnitTokens()`). `UserData`: lo carga cada toma o prueba (nombres de rutinas, de equipos y de archivos de sus semillas). `Brand`: `RowMate`, `TCX` y `FTMS`. `Exception`: Q-03 (los 6 textos exactos) y Q-04 por subárbol `.rm-audit-exempt`.
- **En vivo.** `TextAuditProbe.Collect(panel)` (Snapshots) junta cada `TextElement` visible (`display`, `visibility`, `opacity` > 0, cruzando la ventana) más el texto de `DrawnText`, `TabularNumber` y `TabularCells` (su propiedad `AuditText`, que ya existe o se agrega sin asignar en el cuadro). Una `Violation` hace fallar la toma o la prueba con `{pantalla} · {estado} · {idioma} · "{texto}"`.
- **Recorrido.** `TextAuditTourTests` corre en `es` y `en` con el simulador y los servicios falsos de la It. 10a a 12. Pasa por: Dispositivo (sin Bluetooth, buscando, resultados, conectando, conectado, error, reconectando y las 4 tarjetas del pulsómetro), Workout (lista, inicio, HUD libre y con rutina, pausa, completado, terminar), Rutinas (lista, editor, diálogos de paso, repetir y borrar), Historial (vacío, error y lista), Detalle (gráfico, métricas, exportando y los avisos de guardado y de error). **Ampliado por QA-G14-08** (lo que dejan de cubrir `it4b` a `it8`): `EnvironmentPicker` abierto, con cada uno de los 4 escenarios elegido; `SeriesPanelView` con y sin series, colapsado y expandido; `CenterCallouts` en cada aviso central que exponga el componente y con `Próximo:`; `StartWorkoutDialog` y `FinishWorkoutDialog` en todas sus variantes. WP-B1 enumera estados y variantes leyendo esas 5 clases y los deja listados en el test (un `TestCaseSource` por pantalla), en `es` y en `en`. Además, `DemoWalkthroughTests` (los 4 escenarios, es/en) pasa `TextAuditProbe` en cada pantalla que recorre. AC-APP-11-02 se cubre en `LegacyFixedTextsTests` con la tabla de F-APP-11.
- **Estático.** DT14-26. La lista blanca nace con lo que encuentre la primera corrida. Cada entrada nueva lleva su `reason` y QA la revisa.

---

## 5. Lector de pantalla (F-APP-03)

```
AppRoot.Update ─► ScreenReaderBridge.Tick(now)
   │  (si !AssistiveSupport.isScreenReaderEnabled → return; 0 B)
   ├─ sucio ESTRUCTURAL? (navegación, diálogo, clase de ventana, elementos con regla que entran/salen; ≥ 250 ms desde el último)
   │     └─ A11yRules.Collect(panel.visualTree, modalRoot) → List<A11yItem>  (reutilizada)
   │        A11yOrder.Sort(items)   (filas con tolerancia de 8 dp, arriba-abajo e izquierda-derecha)
   │        A11yReconciler.Diff(anterior, nueva) por clave estable (VisualElement)
   │          → InsertNode / MoveNode / RemoveNode; los que siguen conservan su AccessibilityNode (y el foco)
   │          → Clear() solo al cambiar de raíz (shell ↔ diálogo); foco perdido → SendLayoutChanged(vecino)
   │        role, state, hint, value, frameGetter, invoked solo en los nodos nuevos o cambiados
   │        AssistiveSupport.activeHierarchy = hierarchy (una vez)
   ├─ sucio de GEOMETRÍA? (GeometryChanged, desplazamiento, HUD) → hierarchy.RefreshNodeFrames()  (sin Clear, 0 B)
   ├─ cada 1 s: métricas → node.label = A11yText.LabelValue(…)
   └─ IA11yAnnouncer ◄─ TabHost/PageStack (título), ToastLayer (aviso), WorkoutStore (paso, completado)
```

- **Nombres.** El texto visible del catálogo. Si el botón es solo un ícono, su *tooltip* (`Atrás`, `Mostrar menú`, `Eliminar serie`, `Exportar TCX`). Las métricas usan `a11yLabelValue` con la unidad. El gráfico, `a11yChartSummary` con mínimo y máximo de la serie visible.
- **Roles.** Button, TabButton (dentro de TabBar), Header, StaticText, TextField, Toggle y ScrollView para las listas. `scrolled` desplaza un paso con `ScrollPolicy`.
- **Estados.** `Selected` (pestaña, segmento), `Disabled` (`:disabled`), `Expanded` (paneles de series y menús).
- **Costo (AC-APP-03-15).** Con el lector apagado no hay suscripciones a `GeometryChangedEvent` ni árbol, y el `Tick` hace una sola lectura de un `bool` estático. Las anotaciones no agregan ningún `VisualElement`. Con el lector encendido, la reconciliación asigna memoria solo ante un cambio estructural; la geometría del HUD y el desplazamiento solo llaman a `RefreshNodeFrames()`, y la `label` de las métricas se actualiza una vez por segundo (QA-G14-21).
- **Foco estable (QA-G14-06).** El bridge guarda el elemento del nodo enfocado (`focusChanged`). Un desplazamiento o una geometría nueva no cambian el árbol; un cambio estructural conserva el mismo objeto `AccessibilityNode` para cada elemento que sigue. Para la prueba, el bridge expone `internal SimulateFocus(VisualElement)` (lo que haría el lector) y un contador de `Clear()`.
- **Plataformas.** En Windows lo maneja el `UnityPlayer.dll` vía UIA (Narrador); en Android, Unity con TalkBack. VoiceOver es [Apple]. En el Editor, el árbol se prueba forzado (DT14-22). Si Narrador no lee algo en la demo, queda NO VERIFICABLE con su issue (R14-01).

---

## 6. Pulido transversal

### 6.1 Transiciones (AC-POL-01, WP-D)

Se usa el inventario T-01 a T-08 del arte §3.1 con los tiempos de ANIMATION_BIBLE. T-01 es `BootSplashMotion` y T-07 es `LayoutClassEntrance`. El resto ya existe y WP-D solo corrige lo que falle. `TransitionInventoryTests` muestrea cada transición cuadro a cuadro con el reloj de prueba: la duración y la curva tienen que ser las del token, la variante de reducir movimiento tiene que estar, y ningún cuadro puede quedar sin estilos (`resolvedStyle` con la fuente o el fondo por defecto) ni con un hueco de más del 30 % del área (comparación con `bg.app`). CAP-24 y VID-01.

### 6.2 Tipografía (AC-POL-02, WP-D)

- TY-01 (filas por paso del Detalle) y TY-02 (fecha y hora de las tarjetas del Historial) como en el arte §3.2. TY-02 usa `TabularCells` con Inter, en celdas del ancho máximo de 0-9.
- `TabularAuditTests`: recorre las pantallas del recorrido y marca todo `Label` cuyo texto salga de `TimeFormats`, `HistoryFormats` o `Fmt` (se reconoce porque el texto es solo cifras con separadores y unidad) y que no sea `TabularNumber`, `TabularCells` ni `.rm-type-num-*`. Los exentos del arte (números dentro de frases, identificadores, campos, simulador y diagnóstico) van en una lista versionada dentro del test.
- `FontAuditTests`: el `resolvedStyle.unityFontDefinition` de todo `TextElement` visible es uno de `FontAssetPaths.All`.
- Tracking: A14-08.

### 6.3 Háptica (AC-POL-03, AC-DEBT-10)

| Evento | `HapticKind` | Origen |
|---|---|---|
| `Pausa` / `Reanudar` tocados | Light | `WorkoutHaptics` (comando del usuario, no la pausa automática) |
| Cambio de paso (no al empezar) | Medium | `WorkoutHaptics` |
| Rutina completada (al entrar el diálogo) | Success | `WorkoutHaptics` |
| `Listo` (10a) y `Reconectado con {name}` | Confirm | `DeviceStore` |

`HapticPolicy`: 250 ms como mínimo entre dos, `Success` gana sobre `Medium` dentro de esa ventana y no se emite en segundo plano (`AppLifecycle`). Un `Confirm` de reconexión que cae dentro de los 250 ms de un cambio de paso se pierde: se acepta y queda escrito en `HapticPolicyTests` (QA-G14-23). En Windows y en el Editor, `NoHaptics`. Sin el permiso `VIBRATE` (ya lo controla el test del manifiesto de la 10a).

---

## 7. Deuda elegida

| Issue | WP | Qué se hace | USS (lo escribe WP-D) |
|---|---|---|---|
| #40 | B1 | En `LinkBanner`, el texto va con `white-space: normal` y 2 líneas como máximo, y el botón con `flex-shrink: 0`. Si no entran juntos, el botón pasa debajo (clase `--stacked` según el ancho medido). Solo se recorta un nombre de más de 24 caracteres (DT10-14) | `.rm-link-banner--stacked` |
| #41 | B1 + D | `Spinner(16, color: text.primary)` en el botón `Buscando...` y la etiqueta en blanco al 70 % | `.rm-scan-button--busy` |
| #42 | B1 | En TH, `DeviceScreenView` pone la lista y la tarjeta del pulsómetro dentro de **un** `ScrollView`, sin alto fijo en la lista y con el margen inferior de `BottomObstacles` | `.rm-device--landscape` |
| #44 | A + B1 | `BleErrorText.ForDisplay` (DT14-27) en la **vista**: `ErrorBanner` y la tarjeta del pulsómetro. `HrmCardRules` no cambia, porque `HrmCardRulesRealTests:117-124` exige el detalle sin tocar (QA-G14-13) | — |
| #69 | A + B1 + D | `ToastClock` y `PathBreak` (DT14-29). `ActionToast` pausa con `PointerEnter` o `FocusIn` y sigue con `PointerLeave` o `FocusOut`. `ExportToastPolishTests` mide el contraste sobre la composición del peor caso (el `glass.fallback` al 86 % sobre la línea más clara del gráfico), no sobre el color del token (QA-G14-18) | `.rm-toast--action` con fondo `glass.fallback` |
| #36 | D | En P360 a 1,3×, cada columna de la tarjeta es un `FitBox` entero (valor y etiqueta juntos) con 4 dp de aire. En TV, el gráfico del Detalle con el `0` en Y y el eje X a la vista. La llave y la cinta, auditadas con `WrenchAudit` | sí |
| #32 | A + D | `AxisTicks.DropCrowded` y `SplitRange.Widen` (DT14-28), usados por `ChartAxisLabels` | — |
| #37 | D | Extremos redondos en `SessionChartView` (`LineCap.Round`), la tira de paso con radio 1,5 dp y el ícono de error de 56 dp al 12 % | sí |
| #21 | A + B1 | DT14-30 | `.rm-scene-loading` |
| #51 | A | `DeviceStore` llama a `Confirm()` al reconectar, una vez por reconexión | — |

---

## 8. CI (F-APP-19)

### 8.1 `ci-unity.yml` (C)

```yaml
on:
  push: { branches: [main] }
  workflow_dispatch:
    inputs:
      tests:  { type: boolean, default: true }
      builds: { type: boolean, default: true }
      apple:  { type: boolean, default: false }   # solo manual (AC-APP-19-08)
permissions: { contents: read }
concurrency: { group: ci-unity-${{ github.ref }}, cancel-in-progress: true }
jobs:
  gate:            # ubuntu-latest, timeout 5. Lee secrets.* SOLO en env de un paso; outputs: unity, android-signed
                   # sin UNITY_LICENSE/EMAIL/PASSWORD → ::warning:: "Falta el secreto … ver tools/unity/README.md (It. 14: CI)" y unity=false; el job pasa
  unity-tests:     # needs gate; if unity=='true' && (push || inputs.tests); la matriz de hoy (4), timeout 60
  build-windows:   # needs gate; if unity=='true' && (push || inputs.builds); ubuntu-latest (imagen windows-mono de GameCI), timeout 90
                   # game-ci/unity-builder@v4: unityVersion 6000.3.27f1, targetPlatform StandaloneWindows64,
                   # buildMethod RowMate.EditorTools.BuildCli.Build, customParameters -rowmateTarget Windows -rowmateBackend Mono -rowmateOutput build/RowMate-Windows
                   # actions/upload-artifact@v4: name RowMate-Windows, path build/RowMate-Windows, retention-days 14
  build-android:   # ídem con targetPlatform Android, -rowmateTarget Android -rowmateBackend IL2CPP -rowmateOutput build/RowMate-Android
                   # androidKeystoreBase64/Pass/AliasName/AliasPass desde secrets (vacíos = llave de depuración)
                   # artefacto: needs.gate.outputs.android-signed == 'true' && 'RowMate-Android' || 'RowMate-Android-debugsigned' (solo el .apk)
  build-apple:     # needs gate; if unity=='true' && github.event_name=='workflow_dispatch' && inputs.apple; macos-14, timeout 120
                   # unity-builder iOS (proyecto Xcode) → xcodebuild … -sdk iphoneos CODE_SIGNING_ALLOWED=NO → zip .app → RowMate-iOS-unsigned
                   # unity-builder StandaloneOSX → zip .app → RowMate-macOS-unsigned; retention-days 14
```

- `upload-artifact` sube **solo** la carpeta del player (Windows, Apple) o el `.apk` (Android): ningún `*.log`, `*.keystore` ni `Library/` (el *keystore* que decodifica GameCI queda en el *workspace*). Lo exige `CiUnityWorkflowTests.ArtifactPathsOnlyPlayer` (QA-G14-15).
- En `build-windows` (Linux), el post-proceso de DT14-31 no puede reescribir la versión: el registro deja `::warning::` y el artefacto sale igual. La reescritura en Linux es un issue.
- Todas las acciones van fijadas a su etiqueta: `actions/checkout@v4`, `actions/cache@v4`, `actions/upload-artifact@v4`, `game-ci/unity-test-runner@v4` y `game-ci/unity-builder@v4`. Nada de `@main` ni `@master`. Cada build tiene su caché de `Library` por plataforma.
- `BuildCli`: `-rowmateTarget iOS|macOS` (`BuildRequest`), sin Development. Lee `-androidKeystoreName`, `-androidKeystorePass`, `-androidKeyaliasName` y `-androidKeyaliasPass` si GameCI los pasa y no vienen vacíos. Los aplica en memoria (`useCustomKeystore`) y los restaura en `finally`. El build normal sigue pasando por `BuildOutputInspector` (rastros del simulador, AC-BLE-13-02): un rastro es la salida 2 y el job queda rojo.
- `ci-core.yml` no cambia. La validación de YAML corre dentro de `run-tests.ps1` (§8.3).

### 8.2 Secretos (en `tools/unity/README.md`, sección "It. 14: CI")

`UNITY_LICENSE`, `UNITY_EMAIL` y `UNITY_PASSWORD` son obligatorios para cualquier job de Unity. `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` y `ANDROID_KEYALIAS_PASS` son opcionales. La tabla, cómo lanzar los jobs, los artefactos y los pasos de Android y Apple siguen la spec §9, sin rutas de la PC (el `.ulf` se nombra por la carpeta de datos de programa de Unity, sin usuario). El README dice además que en la primera corrida real se comprueba que existan las imágenes de GameCI `6000.3.27f1` (windows-mono y android) y que la activación Personal funcione en el *runner* de macOS; lo que falle va a issue (QA-G14-22).

### 8.3 Cómo se valida en la PC

- **`MiniYaml`** (`Tests/Support`, netstandard2.1, compila también en Unity): el subconjunto de YAML de bloque que usan los workflows (mapas, listas, listas en flujo `[a, b]`, mapas en flujo `{a: b}`, escalares con y sin comillas, `|` y `>`, comentarios y `${{ }}`). Falla con tabulaciones, sangría inconsistente y claves repetidas.
- **`WorkflowYamlTests`** (WP-B2, junto con el workflow nuevo, sobre todos los `.github/workflows/*.yml`): se parsea sin errores; todo `uses:` cumple `^[\w.-]+/[\w.-]+@v\d+(\.\d+){0,2}$`; `permissions.contents: read` en la raíz; `timeout-minutes` en cada job; todo `unityVersion` es igual a `ProjectVersion.txt` (`6000.3.27f1`); `secrets.` aparece solo en `env:` o `with:` (nunca en `if:` ni en `run:`); no hay rutas absolutas (`C:\`, `/Users/`, `/home/`).
- **`CiUnityWorkflowTests`** (WP-B2): los jobs de §8.1, `needs: gate` y su `if:`, el aviso con `tools/unity/README.md`, los nombres de los artefactos, `retention-days: 14`, la lógica de `-debugsigned`, que el job de Apple es solo manual, y la higiene (`RepoHygieneTests` sigue en verde).
- Todo corre con `tools/dotnet/run-tests.ps1` en la PC y en `ci-core`. No se hace push ni se lanza ningún workflow.

### 8.4 Pruebas vigentes que el workflow nuevo toca (QA-G14-09)

- `ItTwoDebtTests.CiUnityRunsTheSimulatorSuitesWithoutCommittingTheDefine` (`:166-178`): la aserción `Does.Not.Contain("unity-builder")` ("no compila players", R-01 de la It. 2) se **reescribe, no se borra**: todo job que usa `game-ci/unity-builder` tiene un id `build-*`; ningún `build-*` menciona `ci-simulator-define.sh`, `ROWMATE_SIMULATOR` ni el argumento de prueba del simulador; y su caché de `Library` tiene una clave propia por plataforma (`build-<plataforma>-…`), distinta de la de tests (`sim-${{ matrix.simulator }}-…`). El resto de la prueba queda igual: la matriz `        simulator: [false, true]` y los pasos `enable`/`restore` del job `unity-tests` se mantienen con su texto y su sangría exactos.
- `ItTwoToolingTests.CiUnityRunsEditAndPlayMode` (`:90-101`): sin cambios; el workflow nuevo conserva `testMode: [EditMode, PlayMode]`, `workflow_dispatch`, `UNITY_LICENSE` y no contiene `ROWMATE_SIMULATOR` en ningún lugar (tampoco en los builds ni en el job compuerta).
- El `REPORT.md` anota que R-01 queda reemplazada por AC-APP-19-06.

---

## 9. Arnés: conjunto `it14`

`capture-snapshots.ps1 -Set it14` pasa a ser el valor por defecto y escribe en `docs/migration/iterations/14-pulido-plataformas/snapshots` (ninguna otra carpeta: lo prueba `ItFourteenToolingTests`). El catálogo es `Snapshots/PolishCatalog.cs`, con las reglas de la 6a §7.1 (reloj de prueba, `ROWMATE_DATA_DIR` temporal, F-9, `FakeFileExporter`, dos corridas iguales byte a byte; en `manifest.json` solo cambia `durationMs`).

| ID | Origen y estado |
|---|---|
| CAP-01, CAP-02 | Las escribe `tools/icon` (§2.2). El catálogo solo las lista en el *manifest* (`"source": "tools/icon"`) |
| CAP-03, CAP-04 | Cuadro 0 de `UiTestHost` con `BootSplash = true` (W1280, TV) |
| CAP-05, CAP-06 | Ventana de 640 × 360, `es` Dispositivo conectado (falso K-10); `en` Workout con HUD y escena `Lago alpino` (escena real en Alto, sin `-nographics`) |
| CAP-07 a CAP-12, CAP-14 | Los estados de la spec §7, con `LayoutAudit` y, cuando hay simulador, `WrenchAudit` |
| CAP-13 | L640, letra 1,3×, `Corta` completada; el diálogo quieto (regla de toma de la 4b) |
| CAP-15 | TV 1,3×, remo real sobre `FakeBle` con el nombre `AMS-670B RowMaster 2024`, reconectando |
| CAP-16 a CAP-18 | Dispositivo buscando (TV y TH) y la falla de búsqueda inyectada con `scan: error 2` |
| CAP-19, CAP-20 | El aviso de exportación sobre el gráfico; CAP-20 con `ScenarioScript.Hover` |
| CAP-21 a CAP-23, CAP-25 | Historial y Detalle (F-9; el error con la base falsa que falla) |
| CAP-24 | Cuadro en t = 120 ms del *push* del editor |
| CAP-26 | Primera entrada con la escena demorada (`UiHostOptions.SceneDelay`) |
| CAP-27 | Detalle con el lector forzado: `CAP-27.png` + `CAP-27-a11y.txt` (`A11yTreeDump`: orden, rol, nombre y estado) |
| VID-01, VID-02 | El recorrido de transiciones (W1280) y la rotación TV → TH con el HUD |

Toda toma pasa por `TextAuditProbe` (D-01). El tope del arnés queda en 45 min.

---

## 10. Tests: matriz AC → test

| AC | Test (marca) |
|---|---|
| AC-APP-16-06 | `AppIdentityTests` (dotnet) + `ProjectSettingsIt14Tests.NameAndVersion` (dotnet, lectura del YAML) + `VersionInfoBlockTests` (dotnet: estructura, alineación, claves y valores leídos de vuelta) + `ItFourteenToolingTests.ExeVersionInfoChecked` (dotnet: `build.ps1` verifica `VersionInfo` en todo build de Windows, código 9) + la verificación misma de `build.ps1` en `normal`, `simulator` y `dev-smoke` + QA: propiedades del `.exe` y título [Device-Win]; [Device-Android] y [Apple] NV (DT14-31) |
| AC-APP-16-07 | `ProjectSettingsIt14Tests.Identifiers`, `AppIdentityTests` (el caso `ProvisionalIdsAndNoComExample` pasa a "definitivos", Q-85; QA-G14-11) (dotnet) |
| AC-APP-16-08 | `AppIconGeometryTests`, `AppIconRasterTests` (16 px ≥ 2 px, opaco en iOS), `PngEncoderTests`, `DeflateEncoderTests`, `AppIconAssetsAreCurrent`, `AppIconHashesTests` (SHA-256 de 16 y 1024), `IconMathTests` (contra `Math.Sin` con tolerancia 1e-9, y valores fijos bit a bit), `AppIconSourceScanTests` (sin `Math.Sin/Cos/Tan/Exp/Pow/Log/Atan*` en `AppIcon*`) (dotnet, en ubuntu y windows); `AppIconImportTests`, `IdentityStepTests.StandaloneIconsForEverySize` (EditMode de Unity); CAP-01; QA: barra de tareas, título y Alt+Tab [Device-Win] |
| AC-APP-16-09 | `AppIconGeometryTests.AdaptiveForegroundInsideSafeZone` (dotnet); `ProjectSettingsIt14Tests.AndroidPlatformIcons` (dotnet); CAP-02; [Device-Android] NV |
| AC-APP-16-10 | `BootSplashMotionTests` (dotnet); `BootSplashTests` (PlayMode: la capa es la última hija del shell y sigue última tras un `Add` posterior; cuadro 0 con `panel.Pick` del centro en la capa y el píxel central de la marca sobre `#0D1B2A`; marca de 96 dp centrada; salida de 200/120; la capa sale de la jerarquía); CAP-03, CAP-04; `build.ps1 -BootSmoke` (aislado: `ItFourteenToolingTests.BootSmokeIsIsolated` exige `ROWMATE_DATA_DIR` temporal borrado en `finally`, `ROWMATE_BLE = 'off'`, build de desarrollo y `-screen-*`) [Device-Win] |
| AC-APP-16-11 | `ProjectSettingsIt14Tests.SplashAndLaunchScreen` (dotnet); `ApplePostprocessTests` (EditMode: carpetas temporales); [Device-Android] y [Apple] NV |
| AC-APP-16-12 | `ProjectSettingsIt14Tests.AndroidMinimum` + `Ble/AndroidManifestTests` (permisos del manifiesto, sin `VIBRATE`; QA-G14-11) (dotnet); `aapt2` [Device-Android] NV |
| AC-APP-15-05 | `WindowGeometryTests.Initial*` y `.ShouldPlaceInitial` (con `-screen-*` o `-popupwindow` no se reubica) (dotnet); `WindowsWindowFrameTests` (EditMode con `IWndProcApi` falso: instalar guarda el original; `Uninstall` lo restaura solo si el actual es el nuestro, dos veces no hace nada; se engancha a `Application.quitting`; `WM_GETMINMAXINFO` llama primero al original; en el Editor no hace nada); `-BootSmoke` (10 salidas limpias, código 0, sin `Crashes`); QA [Device-Win]: cerrar con la X, con Alt+F4 y en pantalla completa, 5 veces cada una, sin crash ni `crash.dmp` |
| AC-APP-15-06 | `WindowGeometryTests.MinTrack*` (dotnet); `MinWindowLayoutTests` (PlayMode: L640 en las 6 pantallas con `LayoutAudit`, estado conservado); CAP-05, CAP-06; QA arrastrando el borde [Device-Win] |
| AC-APP-15-07 | `WindowModeStateTests` (dotnet: F11, Alt+Enter, que `Esc` no haga nada, restauración); `WindowModeTests` (PlayMode con `IWindowFrame` falso); QA [Device-Win] |
| AC-APP-15-08 | Demo del usuario [Device-Win] |
| AC-APP-15-09 | `ProjectSettingsIt14Tests.Orientations` (dotnet); `ApplePostprocessTests.InfoPlistOrientations` (EditMode); `WindowGeometryTests.Mac800x600`; [Device-Android] y [Apple] NV |
| AC-APP-15-10 | `ResizeContinuityTests` (PlayMode: TV ↔ TH y W1280 ↔ L640; reloj de la sesión, ancla de desplazamiento, texto y foco del editor, diálogo, pestaña; el primer cuadro con el *layout* nuevo; 200 ms `ease.enter`; regla de los 400 ms; reducir movimiento); VID-02; [Device-Android] NV |
| AC-APP-11-01 | `VisibleTextClassifierTests`, `CatalogMatcherTests`, `VisibleTextScanTests` (dotnet); `TextAuditTourTests` (PlayMode, es/en, con `EnvironmentPicker`, `SeriesPanelView`, `CenterCallouts`/`Próximo:` y los diálogos de inicio y fin; QA-G14-08); `TextAuditProbe` en cada toma y en `DemoWalkthroughTests` (D-01) |
| AC-APP-11-02 | `LegacyFixedTextsTests` (PlayMode, es/en) |
| AC-APP-11-03 | `CatalogParityTests`, `L10nKeysTests` y las demás (dotnet, 256 claves, paridad de parámetros e ICU) |
| AC-APP-03-12 | `A11yOrderTests`, `A11yTextTests`, `A11yTreeDumpTests`, `A11yReconcilerTests` (dotnet: `Keep`/`Insert`/`Move`/`Remove` por clave, sin cambios → diff vacío); `ScreenReaderTreeTests` (PlayMode, forzado: las 7 pantallas y el HUD, con nombre, rol y estado; la escena afuera; convención de marcos con `TryGetNodeAt`); CAP-27; Narrador [Device-Win]; TalkBack NV |
| AC-APP-03-13 | `ScreenReaderActionTests` (PlayMode: `invoked` = clic; el lector no sale del diálogo; vuelve al que lo abrió; `scrolled`; **foco estable** (QA-G14-06): foco en la fila 5 del Historial, desplazar la lista → mismo `AccessibilityNode`, el bridge sigue con la fila 5 como enfocada, 0 `Clear()` y 0 `SendLayoutChanged`; entra una fila nueva → la fila 5 conserva su nodo); [Device-Win] |
| AC-APP-03-14 | `A11yAnnouncePolicyTests` (dotnet); `ScreenReaderAnnouncementTests` (PlayMode con `FakeA11yAnnouncer`: título, aviso una vez, `Paso 2 de 5: Trabajo`, completado, métricas sin anuncio, resumen del gráfico); [Device-Win] |
| AC-APP-03-15 | `ScreenReaderCostTests` (PlayMode: apagado → sin jerarquía, `FrameAllocations` 0 B en HUD y Dispositivo, conteo del HUD sin cambios ≤ 60; encendido con el HUD latiendo → solo `RefreshNodeFrames`, 0 reconciliaciones y `label` de métricas 1 vez por segundo); `HudBudgetTests` (existente); QA 60 fps con Narrador [Device-Win] |
| AC-POL-01 | `TransitionInventoryTests` (PlayMode); CAP-24; VID-01 |
| AC-POL-02 | `TabularAuditTests`, `FontAuditTests` (PlayMode); CAP-07, -09, -12 |
| AC-POL-03 | `HapticPolicyTests`, `WorkoutHapticsTests` (dotnet); `HapticEventsTests` (PlayMode con `FakeHaptics`); `check.ps1` + `ui-signatures.txt`; [Device-Android] NV |
| AC-POL-04 | `CompletionDialogScaleTests` (PlayMode); CAP-13 |
| AC-POL-05 | `DemoWalkthroughTests` (PlayMode con simulador, es/en, los 4 escenarios, exportador falso, registro sin errores, `TextAuditProbe`) + todos los conteos de dotnet y Unity ≥ los de la It. 12 (base de dotnet del gate: 3886 / 3847 / 39 ignoradas) + las pruebas vigentes actualizadas sin bajar ninguna cuenta: `ItTwoDebtTests` (R-01 reescrita, §8.4), `ItFourAToolingTests:30`, `ItFourBToolingTests:35-36`, `ItSixAToolingTests:190` (`ValidateSet` y `$Set` → `it14`) y `ProjectBootstrapper.CurrentVersion` 6 → 7 (QA-G14-09, -12) |
| AC-POL-06 | QA con `-rowmate-perf-scene` y `-rowmate-perf-env` × 4, 2 min, nivel Alto [Device-Win] |
| AC-POL-07 | `ProjectSettingsIt14Tests` (dotnet, valores de §2.4) + `SimulatorInUnityTests` + `ProjectSettingsGuard` + revisión de QA de `git diff ProjectSettings/` y de los `.mat` del horizonte |
| AC-APP-19-06 | `CiUnityWorkflowTests.BuildsAndArtifacts` y `.ArtifactPathsOnlyPlayer` (dotnet); `ItTwoDebtTests` con R-01 reescrita (§8.4); la corrida real NV hasta que estén los secretos |
| AC-APP-19-07 | `CiUnityWorkflowTests.GateSkipsWithWarning` (dotnet) |
| AC-APP-19-08 | `CiUnityWorkflowTests.AppleManualUnsigned` (dotnet); [Apple] NV |
| AC-APP-19-09 | `WorkflowYamlTests.SecretsOnlyInEnvOrWith`, `RepoHygieneTests`, `CiUnityWorkflowTests.DebugSignedSuffix` (dotnet) |
| AC-APP-19-10 | `WorkflowYamlTests` (dotnet, en la PC y en `ci-core`) |
| AC-APP-19-11 | `ItFourteenToolingTests.ReadmeCiSection` (WP-B2, dotnet: título, los 7 secretos, sin rutas) |
| AC-NV-01 | Lista en `REPORT.md` (QA) |
| AC-DEBT-01 | `LinkBannerNameTests` (PlayMode, TV 1,0× y 1,3×); CAP-15 |
| AC-DEBT-02 | `SearchingButtonTests` (PlayMode: color del indicador, 16 dp, alfa de 0,7 y contraste ≥ 4,5 con `Contrast`); CAP-16 |
| AC-DEBT-03 | `DeviceLandscapeScrollTests` (PlayMode: un solo `ScrollView` y `Buscar` alcanzable); CAP-17 |
| AC-DEBT-04 | `BleErrorTextTests` (dotnet); `ScanErrorTextTests` (PlayMode); CAP-18 |
| AC-DEBT-05 | `ToastClockTests`, `PathBreakTests` (dotnet); `ExportToastPolishTests` (PlayMode: opaco, sin renglón de una letra, pausa con hover y foco); CAP-19, CAP-20 |
| AC-DEBT-06 | `HistoryScaleTests` (PlayMode); CAP-21, CAP-22 |
| AC-DEBT-07 | `AxisCrowdingTests` (dotnet); CAP-23 |
| AC-DEBT-08 | `ChartArtTests` (PlayMode); CAP-12, CAP-25 |
| AC-DEBT-09 | `SceneLoadingSpecsTests` (dotnet); `SceneLoadingTests` (PlayMode: el reloj corre desde el toque, el indicador **a la vista** a los 120 ms con `SceneSurface` en `display: none`, entrada completa, las siguientes sin indicador); `HudBudgetTests.AtMostSixtyVisibleElementsWithDelayedScene` (nuevo, QA-G14-03: rutina `Budget` con objetivos, BPM y `Próximo`, escena demorada con `UiHostOptions.SceneDelay`; se cuenta cuadro a cuadro durante la entrada y el fundido de 0,6 s, y se exige que el peor caso 4b medido + los extra observados (indicador +1, fondo provisorio +1) sea ≤ 60, listando los elementos si falla); CAP-26 |
| AC-DEBT-10 | `DeviceStoreReconnectHapticTests` (dotnet); `HapticEventsTests` (PlayMode); [Device-Android] NV |

Las pruebas nuevas llevan `Category("AC-…")`, y `It14Ids` exige que cada AC tenga al menos una prueba o una marca NV explícita.

---

## 11. Paquetes y orden

| Paquete | Contenido | Sale cuando |
|---|---|---|
| **WP-A** (puro) | §1.1: ícono y PNG, `tools/icon` y sus salidas, movimientos y políticas, ventana pura, accesibilidad pura, clasificador y escaneo de textos, 4 claves, #44, #32, #51, `WorkoutHaptics`, `MiniYaml` y `MiniYamlTests` | `run-tests.ps1` en verde; `tools/icon --check` en 0; dos corridas de `tools/icon` sin diff |
| **WP-B1** (Unity) | §1.2 B1: bootstrapper v7 e importación, `ProjectSettings` de §2.4, `ApplePostprocess`, arranque, ventana, F11, lector, háptica de Android, deuda #40, #41 (C#), #42, #44 (vista), #69 (lógica) y #21, y sus pruebas PlayMode y EditMode | EditMode y PlayMode en verde (también con `-Simulator`); `check.ps1 -Golden`; `git diff ProjectSettings/` = §2.4 |
| **WP-D** (Designer) | §1.3: USS, transiciones, `LayoutClassEntrance`, `ScrollAnchor`, TY-01, TY-02, tracking, #36, #37 y #32 (vista), biblias, slot `app.icon`, `04-design-notes` | En paralelo con B1 (archivos disjuntos); sus pruebas PlayMode en verde |
| **WP-B2** (Unity) | §1.2 B2: CI, `BuildCli` (iOS/macOS, firma, ícono de Mac), arnés `it14` (dos corridas), `-BootSmoke`, builds `normal`, `simulator` y los humos (`-SmokeTest`, `-DbSmoke`, `-BleSmoke` tal como están, `-ExportSmoke`, `-BootSmoke`), README y ADR | Después de B1 y D; capturas revisadas por WP-D; `VersionInfo` del `.exe` verificado en los 3 builds (DT14-31); `-BootSmoke` con 10 salidas limpias |

Una sola instancia de Unity. No se editan fuentes mientras corre, y los builds van solo bajo `Builds/Windows/`.

---

## 12. Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| R14-01 | En el Editor, `activeHierarchy` puede no hacer nada (`isSupportedPlatform`), y Narrador puede leer menos de lo que expone Unity 6.3 por UIA | Las pruebas usan el modelo propio (`A11yTreeDump`) y, cuando se puede, el de Unity. Narrador va a la demo; lo que no lea queda NV con su issue |
| R14-02 | La subclase del `WndProc` desde C# puede tener reentradas con `Screen.SetResolution`, y al cerrar pueden llegar mensajes (`WM_DESTROY`, `WM_NCDESTROY`) a un delegado administrado después de apagar Mono (QA-G14-04) | El manejador no llama a Unity; solo escribe memoria y delega. `Uninstall()` restaura el original en `Application.quitting` y en `AppRoot.OnDestroy`, antes del apagado del dominio (DT14-14). QA cierra con la X, Alt+F4 y en pantalla completa, 5 veces cada una. Si igual falla, se pasa a una DLL nativa chica (mismo contrato) |
| R14-03 | Unity restaura el último tamaño o la pantalla completa antes de `Awake` | Se pisa en `Awake` y la capa de arranque tapa el cuadro intermedio. No se toca el registro |
| R14-04 | `Alt+Enter` puede seguir pasando por el manejo nativo de Unity aunque `allowFullscreenSwitch` esté en 0 | QA lo prueba en el build. Si pasa, solo queda `F11` y va a issue |
| R14-05 | Los íconos y el arranque de Android se escriben por `SerializedObject` sin el módulo (`m_Kind` 0/1/2 sin verificar) | Queda [Device-Android] NV. Cuando el usuario instale el módulo, `IdentityStepTests.AndroidKindsMatchModule` (ignorado hoy) los compara con `AndroidPlatformIconKind` |
| R14-06 | El fondo del arranque de Android 12+ depende del tema que arme Unity | NV. Si no es `#0D1B2A`, va a issue con un `.androidlib` de tema y la plantilla del *launcher* |
| R14-07 | GameCI: la activación de la licencia Personal, la imagen windows-mono en Linux, el disco en Android y las versiones del runner de macOS | El job compuerta deja el CI en verde sin secretos. La primera corrida real la hace el usuario y lo que falle va a issue |
| R14-08 | Un error en el deflate propio, o bytes distintos según la plataforma (libm de UCRT o glibc, QA-G14-05) | Se descomprime con `ZLibStream` (.NET 8) y con `Texture2D.LoadImage` (Unity), con un test de ida y vuelta sobre datos aleatorios con semilla fija. El rasterizado no usa funciones trascendentes de la plataforma (`IconMath.Sin`, `AppIconSourceScanTests`) y `AppIconHashesTests` fija el SHA-256 de 16 y 1024 en ubuntu y windows |
| R14-09 | Falsos positivos en la auditoría de textos (unidades, datos del usuario) | Categorías explícitas y lista blanca con motivo. La falla nombra la pantalla y el texto |
| R14-10 | El HUD está cerca de su tope de elementos (hoy 56-57 en el peor caso) | El lector no agrega `VisualElement` y `LayoutClassEntrance` solo cambia estilos. El indicador de #21 **sí cuenta** (QA-G14-03): es un solo `Spinner` hermano de `SceneSurface`, `display: none` salvo en la entrada demorada, y el peor caso (+2 con el fondo del fundido) lo mide `HudBudgetTests.AtMostSixtyVisibleElementsWithDelayedScene`. Si pasara de 60, el indicador va fuera de `#immersive` (sobre la página, debajo del HUD) y se anota |
| R14-13 | `UpdateResource` no existe en el CI de Linux, así que el `.exe` del artefacto de GameCI sale con la versión del motor (QA-G14-01) | En la PC, el post-proceso es obligatorio y `build.ps1` lo verifica (código 9). En Linux, `::warning::` y issue (escritor PE puro) |
| R14-11 | El bootstrapper puede cambiar otros campos o los `.mat` del horizonte | `ProjectSettingsGuard`, la lista de §2.4 y `git checkout` de `HorizonFar.mat` y `HorizonNear.mat` |
| R14-12 | Algunas pruebas existentes esperan el comportamiento de antes (letter-spacing de las pestañas, entrada del HUD, `Confirm` vía `RmBle`, "no compila players" de R-01 en `ItTwoDebtTests:166-178`, el `$Set` por defecto en `ItFourA/FourB/SixAToolingTests`, `CurrentVersion` 6 del bootstrapper) | Cada paquete actualiza las que le corresponden y lo anota en su informe; R-01 se reescribe según §8.4, no se borra. Ningún conteo baja |

---

## 13. Para issues

- Animación de marca en el arranque (DV-134), ícono monocromo de Android 13 y un `.ico` versionado para un instalador.
- Mínimo de ventana y pantalla completa en macOS (Swift, 10b; D-04).
- T6, Split y Watts con el mismo ancho (A14-09); contenido de 960 dp como máximo (A14-10); borde de la barra al desplazar (V14-11); sangrado de `Escenario` (V14-05); parche de la estela (V14-12); cinta `SIMULADOR` sobre el título (V14-13).
- Recorrer el gráfico punto a punto con el lector (DV-136).
- Si Narrador no alcanza algún rol en la demo: mapeo propio a UIA.
- Tema de arranque de Android 12+ con `.androidlib` si `SplashScreen.backgroundColor` no alcanza (R14-06).
- `VS_VERSIONINFO` del `.exe` compilado en el CI de Linux: escritor PE puro (R14-13, QA-G14-01).

---

## Cambios por gate

Gate: [03-qa-gate](03-qa-gate.md), aprobado con observaciones. Se corrigen los 9 mayores en este documento; la spec y el arte no cambian (ningún mayor toca el QUÉ ni el arte: QA-G14-01 se resuelve implementando, así que AC-APP-16-06 queda como está).

| ID | Corrección | Dónde |
|---|---|---|
| QA-G14-01 | Post-proceso de Windows que reescribe `VS_VERSIONINFO` con `UpdateResourceW` (en la PC), armado por `VersionInfoBlock` (Core, dotnet) con los valores de `PlayerSettings`. `build.ps1` verifica `VersionInfo` en todo build de Windows (código 9). En el CI de Linux: aviso e issue. AC-APP-16-06 **no** se baja | DT14-31, §1, §8.1, §10, R14-13, §13 |
| QA-G14-02 | `BootSplashView` es la **última** hija del shell (después de `TopLayer`), con `BringToFront()` tras cualquier `Add` posterior. `BootSplashTests` comprueba `panel.Pick` y el píxel central en el cuadro 0 | DT14-10, §2.3, §10 |
| QA-G14-03 | El indicador de #21 es un solo `Spinner` hermano de `SceneSurface` (no adentro, porque está en `display: none`), bajo `#immersive`, y **cuenta**. `HudBudgetTests` suma el caso de la escena demorada (≤ 60). R14-10 corregido. `HudView`: B1 toca solo ese bloque y antes que WP-D | DT14-30, §1.2, §10, R14-10 |
| QA-G14-04 | `Uninstall()` del `WndProc`: restaura `GWLP_WNDPROC` en `Application.quitting` y en `AppRoot.OnDestroy`, idempotente y solo si el actual es el nuestro; pruebas con `IWndProcApi` falso, 10 salidas limpias en `-BootSmoke` y QA de cierre (X, Alt+F4, pantalla completa) | DT14-14, §3, §10, R14-02 |
| QA-G14-05 | `IconMath.Sin` propio (Horner con `+` y `*`); solo `Sqrt`, `Round` y funciones exactas; `AppIconSourceScanTests` lo exige y `AppIconHashesTests` fija el SHA-256 de 16 y 1024 | DT14-01, §1.1, §2.2, §10, R14-08 |
| QA-G14-06 | Dos niveles de "sucio": estructural → reconciliación por clave estable (`A11yReconciler` + `InsertNode`/`MoveNode`/`RemoveNode`, verificados en el editor), conservando el nodo y el foco; geometría → `RefreshNodeFrames()`. Caso nuevo de foco estable al desplazar | DT14-19, §5, §10 |
| QA-G14-07 | `-BootSmoke` con la receta de `-ExportSmoke` (datos temporales borrados en `finally`, `ROWMATE_BLE=off`, build de desarrollo, `-screen-*`); DT14-16 no reubica si llegan `-screen-*` o `-popupwindow` (`ShouldPlaceInitial`). `ItFourteenToolingTests.BootSmokeIsIsolated` | DT14-12, DT14-16, §3, §10 |
| QA-G14-08 | D-01 queda aceptado con la condición: el recorrido suma `EnvironmentPicker`, `SeriesPanelView`, `CenterCallouts`/`Próximo:` y los diálogos de inicio y fin con sus variantes, en es y en; `TextAuditProbe` también en `DemoWalkthroughTests` | D-01, §4, §10 |
| QA-G14-09 | Nueva §8.4: R-01 de `ItTwoDebtTests` se reescribe ("los builds solo en `build-*`, sin el define del simulador y con caché separada"), sin borrarla; la matriz y los pasos del simulador quedan literales; `ItTwoToolingTests` sin cambios; el REPORT anota R-01 → AC-APP-19-06 | §1.2, §8.4, §10, R14-12 |

Menores del architect, adoptados en el mismo cambio: QA-G14-10 (`autorotateToPortraitUpsideDown` en iPhone, §3), -11 (nombres de pruebas en §4 y §10), -12 (pruebas a actualizar, §1.2 y §10), -13 (#44 en la vista, §7), -14 (original primero en `WM_GETMINMAXINFO`, DT14-14), -15 (artefactos solo del player, §8.1), -16 (`.gitattributes` y `.meta`, §2.2), -18 (contraste sobre la composición, §7), -20 (`-BootSmoke` con el build completo de la 14, DT14-12), -21 (§5 y §10), -22 (README, §8.2) y -23 (`HapticPolicyTests`, §6.3). QA-G14-17 es del Designer (A14-08 ya dice "solo pestañas salvo sondas en verde") y QA-G14-19 ya lo aceptó el gate: no cambian este documento.

## Cambios por implementación de WP-B

| ID | Cambio | Por qué |
|---|---|---|
| B-01 | `Packages/manifest.json` suma `com.unity.modules.accessibility` (`ManifestAndVersionTests` actualizado) | Sin el módulo integrado, `AccessibilityHierarchy` no compila (CS1069) |
| B-02 | Las excepciones Q-03 salen de `Core` a `Tests/Support/AuditExceptions` y entran por `AuditContext.WithExceptions` | Con la lista en `Core`, `BuildOutputInspector` encontraba "Remo simulado" y "SIMULADOR" en el build normal (AC-BLE-13-02) |
| B-03 | El indicador de #21 es un solo `Label` (`SceneLoadingIndicator`) que dibuja su arco de 32 dp, en lugar de `Spinner` + texto | El texto `workoutSceneLoading` es parte del AC y el HUD solo admite +1 elemento |
| B-04 | #42: en TH (`TwoColumns`) el único desplazamiento es el slot de la tarjeta, con la lista y la tarjeta en fila adentro; la disposición de la It. 11 se conserva | AC-DEBT-03 sin romper `LT30_RealService` |
| B-05 | #41: el botón buscando se pinta en línea (fondo `accent.primary` al 38 % sobre `bg.app`, opacidad 1, etiqueta blanca al 70 %, indicador blanco) | El deshabilitado genérico (opacidad 0,38 sobre todo el botón) dejaba la etiqueta con contraste < 4,5 |
| B-06 | `A11yRules` suma `.rm-session-card__title`, `.rm-segmented__option`, `.rm-stat` y `.rm-step-perf`; no lee los glifos `.rm-icon` | El Detalle (CAP-27) se leía solo con 4 elementos |
| B-07 | Conjunto `it14`: 14 tomas (CAP-03, -04, -05, -07, -12, -15 a -20, -22, -23 y -27 con `CAP-27-a11y.txt`); la auditoría de textos corre solo en `it14` | Las tomas con escena real, CAP-08, -09, -13, -21, -24, -25 y VID-01/02 quedan para issues; correr el gancho en los conjuntos viejos queda para cuando se regeneren |

## Cambios por verificación

Ronda 1 de [05-qa-verification](05-qa-verification.md): se corrigen los 7 mayores del architect. Cada uno tiene primero la prueba que lo reproduce y después el arreglo. Evidencia en `<scratch>/it14/arch/`.

| ID | Causa | Arreglo | Prueba |
|---|---|---|---|
| QA-V14-01 | Unity apaga el player mientras atiende `WM_CLOSE`. Nuestro *thunk* administrado seguía en la cadena (y en la pila, porque el original se llamaba desde el manejador) cuando se apagaba el dominio → `0xC0000005` (reproducido 1 de 1 con el `dev-smoke` anterior) | `WindowsWindowFrame`: con `WM_CLOSE` restaura el `WndProc` original y vuelve a encolar el mismo `WM_CLOSE` (`PostMessageW`), que llega directo al de Unity sin nada administrado en el medio; el nuestro devuelve 0. Si otra subclase quedó encima, delega como antes y queda `NotOurs`. `WM_DESTROY`/`WM_NCDESTROY` restauran antes de delegar. `LastUninstall` queda en el registro al salir (`subclase de la ventana retirada: WndProc original restaurado`). No hizo falta pasar a `SetWindowSubclass`: con comctl32 el manejador administrado también queda en la pila | `WindowsWindowFrameTests.CloseRestoresTheOriginalAndRepostsIt`, `.CloseWithAnotherSubclassOnTopDelegatesAndSaysSo`, `.DestroyRestoresBeforeDelegating`, `.UninstallSaysWhatHappened` (EditMode). `AltEnterWithoutBeep` pasa de `0x0010` (que ahora es `WM_CLOSE`) a `WM_PAINT`. Humo nuevo `build.ps1 -CloseSmoke` (código 11; `ItFourteenToolingTests.CloseSmokeIsIsolated`): **7 de 7 salen con 0** (5 en ventana sin `-screen-*` y 2 en pantalla completa), las 7 con la línea de restauración, sin carpetas nuevas en `Crashes` |
| QA-V14-02 | `WindowGeometry.Initial` recortaba el contenido y `PlaceContent` le sumaba el marco después | `Initial(workArea, escala, WindowFrameEdges, …)`: el que se recorta y se centra es el **exterior** (contenido + marco con el DPI, `WindowsWndProcApi.FrameEdges`, con el estilo de ventana normal si Unity abrió en pantalla completa) | `WindowGeometryTests.TheOuterWindowFitsTheWorkArea1024` (contenido 1008 × 681 en (8, 31)), `…1366` (1280 × 689), `…At150` (1920 × 1032 al 150 %) y `WhenItFitsTheOuterIsCentered` (dotnet). En el build, con el monitor de 1024 × 768: contenido **1008 × 681**, la barra de título adentro |
| QA-V14-03 | Entre que aparece la ventana y el primer `Present` (≈ 0,7 s: `AppRoot.Awake` arma la base, el shell y el HUD precalentado) Windows muestra el área de contenido sin pintar, en blanco | `WindowsBootBackground`: en `RuntimeInitializeLoadType.SubsystemRegistration` (lo primero administrado, antes de la escena) pone el pincel `bg.app` en la clase de la ventana y pinta el área de contenido con GDI; `AppRoot` repinta después de ubicar la ventana inicial. No se usó el *splash* de Unity: cada logo dura 2 s como mínimo y alarga el arranque. El registro dice cuándo pintó (≈ 440 ms del arranque del proceso) | `WindowsWindowFrameTests.BootBackgroundIsBgAppAsColorRef` (EditMode). Capturas cada ≈ 75 ms, 3 corridas, el mismo monitor de QA: antes **0,7 s** de blanco (9 tomas seguidas); ahora **0 o 1 toma** (≤ 0,1 s) justo al aparecer la ventana y después `#0D1B2A` sin cortes hasta el fundido. Ese resto ocurre antes de que corra cualquier código de la app: **desvío propuesto al PO (DV-V14-03)**: AC-APP-16-10 dice "desde el primer código de la app (≈ 0,1 s después de que aparece la ventana) nunca un cuadro blanco". Repetirlo con el monitor normal (QA-V14-14) |
| QA-V14-04 | B-07 dejó la auditoría en vivo solo en las 14 tomas, y `TextAuditProbe` vivía en `Snapshots` (solo Editor), así que ninguna prueba PlayMode podía usarla | `TextAuditProbe` pasa a `Tests/UiSupport` (mismo `.meta`). Ahora junta también las ranuras de texto dibujado (`IDrawnTextSource`: paneles de series), saca las marcas de texto enriquecido y acepta los mensajes del catálogo pegados por la vista (espacios adelante, `" · reconectando..."`, subtítulos de paso armados con varios mensajes y el glifo de tendencia). Ninguno de los hallazgos del recorrido era un texto fuera del catálogo | `TextAuditTourTests` (es y en; ≥ 50 paradas: Dispositivo con 9 estados, pulsómetro con 6 cuerpos, `EnvironmentPicker` con los 4 escenarios, el diálogo de inicio de las 5 rutinas de F-4B, HUD con rutina, cuenta grande y `Próximo`, pausa, terminar, completado, libre, paneles de series abiertos, cerrados y con repeticiones, Rutinas con menú, borrar, editor, paso, serie y repeticiones, Historial vacío, error y lista, Detalle en las 5 métricas, exportando, guardado y error) y `LegacyFixedTextsTests` (en en, ninguno de los fijos en español de la tabla de F-APP-11; en es, los textos del legacy y ni `Up next` ni `Unknown device`) |
| QA-V14-05 | No existía | `DemoWalkthroughTests` (PlayModeSimulator, es y en): Dispositivo con el remo simulado, Escalera en el editor, Escalera 1 minuto y Terminar, un libre en cada uno de los 4 escenarios, Historial → Detalle → Split → Exportar TCX con el exportador falso. `TextAuditProbe` en cada pantalla y el registro sin errores | La prueba misma |
| QA-V14-06 | No existía | `ResizeContinuityTests`: TV → TH con el mismo inmersivo, el reloj y la fase intactos, el layout horizontal y los controles en su lugar nuevo desde el primer cuadro, opacidad 0 → a mitad a los 100 ms → 1 a los 220 ms, ida y vuelta antes de 400 ms en el acto, reducir movimiento en el acto; W1280 ↔ L640 con el mismo editor, el texto y el foco del nombre, el diálogo de paso abierto y la pestaña. VID-02 sigue en "Para issues" | La prueba misma (3 casos) |
| QA-V14-07 | `SplitRange.Widen` no estaba cableado (WD-03) | `SessionChartData.PaceReference`: si el rango de los ritmos > 0 es menor de 10 s, la referencia "lenta" del eje es el borde lento del rango abierto (±5 s, sin bajar de 0) y el rango entero entra en el eje. Curva, etiquetas y líneas objetivo la usan; `SlowestPace` sigue siendo el ritmo real. Ninguna prueba de la It. 9 fijaba un rango menor de 10 s (todas siguen igual) | `ChartSeriesTests.ConstantSplitOpensTheAxisToTenSeconds`, `.NarrowSplitNearZeroDoesNotGoBelowZero`, `.WideSplitKeepsTheSlowestAsReference` (dotnet); `SplitRangeChartTests.ConstantSplitOpensTheAxis` (PlayMode: la sesión libre de 133 s, eje de 2:18 hacia arriba, `YMax` 11) |

**Corridas** (después de los arreglos; guardia de `ProjectSettings` "sin cambios" en todas, `Horizon*.mat` sin tocar, legacy limpio):

| Corrida | Resultado | Frente a la ronda 1 |
|---|---|---|
| `run-tests.ps1 -Matrix` | **4023: 3984 pasan, 39 ignoradas, 0 fallan**; herramientas 194/194 | 4015/3976 → sube |
| Unity EditMode | **4152: 4098 pasan, 54 ignoradas, 0 fallan** | 4139/4085 → sube |
| Unity EditMode `-Simulator` | **4346: 4292 pasan, 54 ignoradas, 0 fallan** | 4333/4279 → sube |
| Unity PlayMode | **690/690** | 682 → sube |
| Unity PlayMode `-Simulator` | **767: 766 pasan**; `BleAllocationTests.TheReconnectBannerDoesNotAllocatePerFrame` dio 1 asignación en 120 dibujos una vez y pasó al repetirla (4/4) y en la corrida sin simulador: intermitente, a issue | 757 → sube |
| Capturas `it14` | 14 tomas, 0 con problemas (la sonda ahora vive en `UiSupport`) | igual |
| `build.ps1 -Target Windows` (normal) | 0; `BuildOutputInspector` limpio; `VersionInfo` RowMate 1.0.0 | igual |
| `build.ps1 -Target Windows -Simulator` | 0; misma `VersionInfo` | igual |
| `build.ps1 -Target Windows -BootSmoke` | 0: mediana con la capa 1389 ms, sin la capa 1399 ms; 10 salidas limpias | igual |
| `build.ps1 -Target Windows -CloseSmoke` (nuevo) | 0: 7 cierres con `WM_CLOSE` (5 en ventana, 2 en pantalla completa), salida 0, sin `Crashes` | antes: 6 de 6 con `0xC0000005` |

**Para issues** (menores, sin bloquear): `+8 dp` en la entrada del HUD al rotar (solo opacidad hoy); el ≤ 0,1 s antes del primer código de la app si el PO no acepta DV-V14-03 (haría falta un *launcher* nativo); los menores de QA (QA-V14-08 a -13) siguen como están en 05-qa-verification; `BleAllocationTests.TheReconnectBannerDoesNotAllocatePerFrame` intermitente en la corrida larga con el simulador.
