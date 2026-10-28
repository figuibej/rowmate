# It. 3 — Deuda de la It. 2 (Architect)

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-27 |
| Iteración | 3 · `03-rutinas` ([ROADMAP §It. 3](../../ROADMAP.md)), antes del diseño técnico |
| Entrada | [05-qa-verification de la It. 2 §6](../02-dispositivo-simulador/05-qa-verification.md) (QA-V2-01 a -15), [REPORT de la It. 2 §7 y §9.2](../02-dispositivo-simulador/REPORT.md), [STATUS](../../STATUS.md) (decisiones aceptadas por el usuario: QA-V2-08 en esta iteración; QA-V2-09 decidido por el PO: diagnósticos solo en desarrollo), [01-po-spec de la It. 3](01-po-spec.md) (AC-BLE-11-02 reabierto, AC-APP-05-10, AC-APP-05-11) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) `lib/features/device/device_screen.dart:33-38`, `:92-118` (la vista conectada se desmonta en el acto; `'Rower'` solo sin nombre) y `windows/runner/win32_window.cpp:14-27`, `:147`, `:216-217`, `:275-287` (`UpdateTheme`). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío antes y después |
| En paralelo | El Designer saldó QA-V2-12 y alineó las biblias ([00-deuda-diseno.md](00-deuda-diseno.md)): 8 tokens del HUD en `Tokens.cs` / `Tokens.uss`. Esos cambios no son de este documento, pero entran en las corridas y en las capturas regeneradas (§7) |

---

## 0. Resumen

**Los 15 issues de la It. 2 quedan resueltos**: 14 por el Architect y QA-V2-12 por el Designer. **AC-BLE-11-02 vuelve a PASS**, con tres pruebas PlayMode nuevas que fallaban antes del arreglo (`Rower`) y ahora pasan. Todas las suites están en verde (§9): dotnet, Unity EditMode y PlayMode, con y sin simulador, el arnés (41 tomas sin problemas, deterministas) y los builds de Windows.

| ID | Estado | Qué cambió | Prueba / evidencia |
|---|---|---|---|
| QA-V2-01 | **Cerrado** (AC-BLE-11-02 → PASS) | La vista conectada solo se dibuja con el remo conectado; al salir queda congelada (§1) | PlayMode `DeviceScreenTests.OutgoingConnectedViewIsFrozen`; con simulador `DeviceSimulatorTests.DisconnectKeepsTheNameWhileTheConnectedViewLeaves` y `.SimulatedDropKeepsTheNameWhileTheConnectedViewLeaves`. Rojas antes (`But was: "Rower"`), verdes después |
| QA-V2-02 | Cerrado | `PerformanceTestRunCleaner` (preproceso de build, `callbackOrder` 100) borra los `PerformanceTestRun*.json` y la carpeta vacía; `.gitignore`; agujas en el inspector (§2.1) | Build normal: `PerformanceTestRunCleaner: 4 entradas …`, inspector limpio, sin `Assets/Resources` después; UnityEditMode `PerformanceTestRunFilesAreRemovedBeforeTheBuild`; dotnet `ItTwoDebtTests.PerformanceTestRunFilesNeverReachTheBuildOrTheRepository` |
| QA-V2-03 | Cerrado | `BuildCli` comprueba `Variations/*il2cpp*` antes de compilar (y mapea el error del reporte); log por backend (§2.2) | `build.ps1 -Backend IL2CPP` → **3** `NO VERIFICABLE: falta el módulo IL2CPP`; logs `build-windows-normal-mono.log` / `-il2cpp.log`; UnityEditMode `Il2CppIsDetectedFromTheVariationsFolder` |
| QA-V2-04 | Cerrado | `INDEX.md` marca con ⚠ las sondas de aspecto que fallan y el arnés las cuenta; CAP-04b sin la sonda de `bpm` a densidad 1 (§6) | Arnés: `0 sondas de aspecto que fallan`; CAP-04b `1/1` |
| QA-V2-05 | Cerrado | CAP-17 comprueba `14:00`, `Hora real`, `Remando` y las dos fallas | Arnés: CAP-17 `ok` |
| QA-V2-06 | Cerrado | 10 motivos de `[Ignore]` y 2 comentarios de cabecera al día (§8) | dotnet: mismos 17 ignorados, con el motivo nuevo |
| QA-V2-07 | Cerrado | `GeneratedAssetGuard` (`AssetModificationProcessor`): nadie guarda los 9 `FontAsset` generados (salvo el bootstrapper) ni los assets de URP en batchmode. Hallazgo: la tabla de kerning versionada es parte del aspecto y se conserva (§3) | 0 archivos reescritos (mtime igual) después del arnés, 2 builds, las 4 suites; capturas idénticas byte a byte entre corridas; dotnet `GeneratedFontAssetsAreCommittedWithoutDynamicData`, `OnlyTheBootstrapperWritesGeneratedAssets` |
| QA-V2-08 | Cerrado (AC-APP-05-10) | `Platform/Display/WindowsTitleBar`: la regla de `UpdateTheme` del legacy al arrancar, al recuperar el foco y con un sondeo de 2 s (§4) | Build normal con Windows en oscuro: barra `#201F25` (antes clara) y `[PLATFORM] barra de título oscura (AppsUseLightTheme)` en `Player.log`; UnityEditMode `WindowsTitleBarTests` |
| QA-V2-09 | Cerrado | `-rowmate-color-probe`, `-rowmate-ui-pipeline` y `ColorProbe` solo con `UNITY_EDITOR \|\| DEVELOPMENT_BUILD` (§2.3) | Build normal con los dos argumentos: no escribe el JSON y la UI es la normal; el inspector no encuentra los argumentos; build de desarrollo: S-B1 sigue pasando (`#2A2F37`, latencia 12/12, clic) |
| QA-V2-10 | Cerrado | El inspector busca también `Remo simulado`, `Pulsómetro simulado` y `SIMULADOR`, y mira `*.assets` y los archivos sin extensión (§2.1) | UnityEditMode `BuildOutputInspectorFindsTextsDiagnosticsAndTestMetadata`; dotnet `InspectorLooksForTheTextsTheSimulatorShows` (toma los textos del código del simulador) |
| QA-V2-11 | Cerrado | `Simulator/SimulatorReleaseGuard.cs`: `#error` en un player sin Development (§2.4) | UnityEditMode `SimulatorDoesNotCompileInANonDevelopmentPlayer`: compilado como player, `error CS1029: #error … (R-01)`; con Development compila |
| QA-V2-12 | Cerrado por el Designer | [00-deuda-diseno §2](00-deuda-diseno.md) | — |
| QA-V2-13 | Cerrado (AC-APP-05-11 en lo de la It. 2) | `ScrollPolicy`: barra vertical oculta en el teléfono, `Auto` en escritorio; las capturas TV/TH se arman como teléfono (§5) | PlayMode `DeviceScreenTests.VerticalScrollbarOnlyOnDesktop`; CAP-05 sin la barra (solo cambia la franja x 2384-2394) |
| QA-V2-14 | Cerrado en el código; corrida en GitHub **NO VERIFICABLE** (sin licencia en los secretos) | `ci-unity.yml` con la matriz `simulator: [false, true]` y `tools/unity/ci-simulator-define.sh` (§8.2) | YAML validado con un parser; el script probado en un checkout de prueba (enable idempotente, restore deja el archivo como en el commit); dotnet `CiUnityRunsTheSimulatorSuitesWithoutCommittingTheDefine` |
| QA-V2-15 | Cerrado | VID-03 toca `Simular desconexión` en el panel (con *hit-test*) y lo cierra en el mismo cuadro | Arnés: VID-03 `ok` |

---

## 1. QA-V2-01 · `Rower` durante la salida de la vista conectada

**Causa.** `DeviceScreenView.Render()` dibujaba la cabecera y la grilla con cada estado nuevo. Al desconectar, `ConnectedDeviceName` ya es `null` y `DeviceScreenRules.DeviceName` devuelve `"Rower"` (`device_screen.dart:98`), mientras `ViewSwap` mantiene visible la vista conectada 120 ms.

**Arreglo** (`Presentation/Device/DeviceScreenView.cs`): la cabecera y la grilla solo se dibujan si `ViewFor(status) == Connected`. Al desconectar, la vista que sale queda congelada con lo último que mostró, que es lo que ve el usuario del legacy (`device_screen.dart:33-38`: la vista se desmonta en el acto y nunca se dibuja sin remo conectado). Al reconectar, el primer estado `Connected` la vuelve a dibujar antes de que entre, con los ceros de AC-BLE-11-09 y en el idioma actual.

**Pruebas (escritas antes del arreglo).**

- `DeviceScreenTests.OutgoingConnectedViewIsFrozen` (PlayMode, remo de prueba): desconecta y además manda una lectura en ceros. En cada cuadro de 1/60 s de la salida, el nombre sigue en `Remo de prueba`, la insignia en `Conectado` y la grilla en `149`.
- `DeviceSimulatorTests.DisconnectKeepsTheNameWhileTheConnectedViewLeaves` (botón `Desconectar`) y `.SimulatedDropKeepsTheNameWhileTheConnectedViewLeaves` (`Simular desconexión` del panel, con *hit-test*). Es la prueba que propuso QA: a los 60 ms la vista conectada todavía se ve (opacidad entre 0 y 1) y dice `Remo simulado` + `Conectado`. Después revisa cada cuadro hasta que se oculta y que ningún texto diga `Rower`.
- Antes del arreglo las tres fallaron con `Expected: "Remo simulado" / But was: "Rower"`, lo mismo que reportó QA. Después del arreglo pasan.

---

## 2. Builds: lo que el build normal no puede llevar

### 2.1 QA-V2-02 y QA-V2-10 · Metadatos de tests y textos del simulador

- `com.unity.test-framework.performance` escribe `Assets/Resources/PerformanceTestRun{Info,Settings}.json` en su `OnPreprocessBuild` (`callbackOrder` 0) y solo los borra en el postproceso. `Editor/Build/PerformanceTestRunCleaner` corre después (`callbackOrder` 100), antes de que Unity junte los activos. Borra los dos archivos, sus `.meta` si existen y `Assets/Resources` si quedó vacía (en el build: `4 entradas`, los dos `.json`, la carpeta y su `.meta`). Con un build fallido ya no queda nada, y `.gitignore` ignora `/Assets/Resources/PerformanceTestRun*` por si un build se corta a mano.
- `BuildOutputInspector.FindForbiddenTraces` (lo que usa `BuildCli`) busca en UTF-8 y UTF-16 tres grupos de agujas:
  - **simulador**: los tipos de antes más `Remo simulado`, `Pulsómetro simulado` y `SIMULADOR`;
  - **diagnósticos**: `-rowmate-color-probe` y `-rowmate-ui-pipeline`;
  - **tests**: `PerformanceTestRunInfo` y `PerformanceTestRunSettings`.
  Además de `*.dll`, `*.dat` y `*.json`, ahora revisa `*.assets` (ahí estaba el `TextAsset` de QA-V2-02) y los archivos sin extensión (`level0`, `globalgamemanagers`). `FindSimulatorTraces` sigue existiendo con solo el primer grupo. El build normal recién compilado da `sin rastros del simulador, de diagnósticos ni de tests`.

### 2.2 QA-V2-03 · IL2CPP sin módulo

`BuildCli.Run` comprueba `Variations/*il2cpp*` en `BuildPipeline.GetPlaybackEngineDirectory` antes de tocar nada. Sin el módulo sale con **3** (`NO VERIFICABLE: falta el módulo IL2CPP de Windows en este Editor.`), y como respaldo mapea a 3 el mensaje del reporte "scripting backend … is not installed". `build.ps1` nombra el log con el backend: `build-<destino>-<variante>-<mono|il2cpp>.log`. Medido: `Windows normal IL2CPP -> 3` en unos segundos, sin compilar.

### 2.3 QA-V2-09 · Diagnósticos solo en desarrollo

En `Bootstrap/AppRoot.cs`, las constantes de los dos argumentos, la lectura de `-rowmate-ui-pipeline` y el arranque de `ColorProbe` están bajo `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. `Bootstrap/ColorProbe.cs` entero también. En el build normal no se leen ni quedan sus nombres en `RowMate.Bootstrap.dll` (el inspector lo comprueba en cada build). Se probó en la PC:

- el build normal abierto con `-rowmate-color-probe <json> -rowmate-ui-pipeline Overlay` no escribe el JSON y muestra la app normal;
- el build de desarrollo (con simulador) sigue corriendo S-B1 igual que en la It. 2: `glassOnWhite #2A2F37`, primer cuadro sin píxeles distintos de `bg.app`, latencia 12 de 12 y clic en `probe-button`.

ADR 019 dice ahora que el respaldo `Overlay` es solo de desarrollo.

### 2.4 QA-V2-11 · Freno en el propio ensamblado del simulador

`Simulator/SimulatorReleaseGuard.cs`:

```
#if UNITY_5_3_OR_NEWER && ROWMATE_SIMULATOR && !UNITY_EDITOR && !DEVELOPMENT_BUILD
#error RowMate.Simulator en un build sin Development (R-01): …
#endif
```

`UNITY_5_3_OR_NEWER` deja afuera a dotnet, que compila el simulador con `ROWMATE_SIMULATOR` y no es un player. La prueba `SimulatorModeTests.SimulatorDoesNotCompileInANonDevelopmentPlayer` compila una copia con `AssemblyBuilder`, como player (sin `UNITY_EDITOR`) y con `ROWMATE_SIMULATOR`. Sin `DevelopmentBuild` da `error CS1029: #error: 'RowMate.Simulator en un build sin Development (R-01) …'`, y con `DevelopmentBuild` compila sin errores. El freno ya no depende de cómo se arme el build: `BuildCli`, un *Build Profile* o `extraScriptingDefines` a mano.

---

## 3. QA-V2-07 · Fuentes y URP que se reescribían en cada corrida

**Qué pasaba.** Cada build, corrida de tests o del arnés llamaba a `AssetDatabase.SaveAssets()` (`BuildCli`, `SimulatorDefine`, el cierre del Editor) y volvía a escribir los 9 `FontAsset` dinámicos y los assets de URP. Con los textos de la It. 2 el resultado coincidía con lo versionado (lo medí: `mtime` nuevo, mismos bytes). Pero el contenido dependía de lo que la corrida dibujaba: con pantallas nuevas (It. 3) cada corrida iba a dejar MB de diferencias en git.

**Arreglo.** `Editor/Bootstrap/GeneratedAssetGuard` es un `AssetModificationProcessor`. Su `OnWillSaveAssets` saca de cada guardado:

- los 9 `FontAsset` de `FontAssetPaths.All`, siempre;
- los `*.asset` de `Assets/RowMate/Settings/URP/`, solo en batchmode. En la ventana del Editor se pueden seguir ajustando.

Solo escribe esos assets quien abre `GeneratedAssetGuard.AllowWrites()`: hoy, `ProjectBootstrapper.Apply` (crea los que faltan; lo controla un test de dotnet). Cada corrida arranca del mismo estado versionado, y por eso lo que dibuja es determinista. En el log queda `[ASSETS] no se guarda … (QA-V2-07)` una vez por archivo.

**Hallazgo: la tabla de kerning versionada es parte del aspecto.** Primero normalicé los `FontAsset` a un estado "vacío": sin glifos, sin caracteres y sin los registros de `m_FontFeatureTable`, que son 16 000 pares de kerning GPOS (hasta 6,8 MB en Inter Regular y SemiBold). Con ese estado el arnés cambió 4 tomas: la cabecera de CAP-06 y CAP-07 (hasta 242 por canal), una etiqueta de AUX-02 y la barra de CAP-25. **AUX-02 dejó de ser igual a CAP-01 dentro de la misma corrida.** La causa: TextCore agrega pares de kerning solo para los glifos que se suman al atlas (`UpdateFontFeaturesForNewlyAddedGlyphs`). Así, el kerning de un texto depende del orden en que aparecieron sus letras, y la tabla versionada es la que lo deja estable. Además, `FontAsset.ClearFontAssetData` también vacía esa tabla. Por eso:

- los `FontAsset` quedan **tal cual están versionados**: 0 glifos, 0 caracteres, atlas de 1 × 1 y la tabla de kerning de la It. 2. No se tocó ningún byte;
- ningún código llama a `ClearFontAssetData` (lo controla un test de dotnet);
- `ItTwoDebtTests.GeneratedFontAssetsAreCommittedWithoutDynamicData` comprueba sobre los `.asset` versionados que no haya datos dinámicos (tablas de glifos y caracteres vacías, atlas de 1 × 1) y que esté la tabla de kerning. Si algo vuelve a escribirlos con lo que dibujó, falla en `ci-core`.

**Evidencia.** El `mtime` de las 9 fuentes y los 4 assets de URP no cambió después de:

- el arnés, dos veces;
- los builds normal, IL2CPP (que sale con 3) y de simulador;
- las 4 suites de Unity.

Las capturas de dos corridas del arnés separadas por builds y tests son **idénticas byte a byte** (41 tomas; solo cambian las duraciones de `manifest.json`), y AUX-02 vuelve a ser igual a CAP-01.

**Riesgo para arte (no es de esta deuda).** Los textos nuevos de la It. 3 tienen letras que ya están en la tabla, así que su kerning es estable. Un glifo que no esté (un nombre de rutina con caracteres poco comunes) se kerna según el orden en que aparece, igual que con cualquier fuente dinámica de TextCore. Si el Designer quiere el kerning completo de un juego de caracteres fijo, tiene que generarlo de forma deliberada dentro de `AllowWrites`, aceptando el tamaño. Queda anotado para 04-design-notes de la It. 3.

---

## 4. QA-V2-08 · Barra de título oscura (AC-APP-05-10)

`Platform/Display/WindowsTitleBar` porta `UpdateTheme` (`win32_window.cpp:275-287`): lee `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`. Si lo pudo leer, pone `DWMWA_USE_IMMERSIVE_DARK_MODE` (20) en la ventana del player: oscura si el valor es 0. Si el valor no está, no toca nada. La ventana es la de clase `UnityWndClass` del hilo principal (`EnumThreadWindows`), con respaldo en `GetActiveWindow`. Solo hace algo en el player de Windows; en el Editor no toca la ventana del Editor.

Cuándo se aplica. El legacy lo hace al crear la ventana (`:147`) y con `WM_DWMCOLORIZATIONCOLORCHANGED` (`:216`). Unity no expone el `WndProc` del player, así que acá se aplica:

- en `AppRoot.Awake`, desde el primer cuadro;
- en `PlatformServices.Refresh`, al recuperar el foco (cambiar el tema en Configuración saca el foco de la app);
- en un sondeo cada 2 s desde `AppRoot.Update`, para los cambios con la app enfocada (modo automático). El sondeo solo lee el registro, sin asignaciones, y llama a DWM solo si el valor cambió. El cambio llega en 2 s como máximo, contra el instante del legacy: diferencia mínima, anotada para el PO (§10).

Medido en la PC (Windows en modo oscuro, `AppsUseLightTheme = 0`; no cambié la configuración del usuario):

- build normal nuevo: barra de título `#201F25` / `#211E27` (en la It. 2 era clara, `CAP-22-ventana-qa.png` de QA), contenido `#0D1B2A`;
- en `Player.log`: `[PLATFORM] barra de título oscura (AppsUseLightTheme)`.

El modo claro y el cambio en vivo quedan para QA en equipo: no cambio el tema del usuario.

---

## 5. QA-V2-13 · Barra de desplazamiento en el teléfono

`Presentation/Controls/ScrollPolicy.Apply(scroll, ui.IsDesktop)`:

- la barra horizontal, nunca;
- la vertical, `Auto` en escritorio (como la `Scrollbar` que Flutter agrega en Windows, macOS y Linux) y `Hidden` en el teléfono. Flutter en Android e iOS no dibuja barra: `MaterialScrollBehavior.buildScrollbar` devuelve el hijo solo.

Se aplica en las 4 vistas desplazables: `device-connected-scroll`, `device-empty-area`, `hrm-card-slot-scan` y `sim-panel-scroll`. `Hidden` solo oculta la barra: el dedo, la rueda y `ScrollTo` siguen funcionando. Las pantallas de la It. 3 (lista, editor, diálogos; AC-APP-05-11) usan la misma política.

- `DeviceProfile` tiene `IsPhone` (TV y TH), y el arnés arma esas capturas con `IsDesktop = false`, como un teléfono: sin barra ni `:hover`. Las pruebas PlayMode siguen usando escritorio por defecto. `EditModeCaptureMatchesPlayModeTree` se arma como la captura (teléfono); la geometría de CAP-01, CAP-03 y CAP-05 no cambió.
- `DeviceScreenTests.VerticalScrollbarOnlyOnDesktop` (TH, teléfono y escritorio): visibilidad de las 3 vistas de Dispositivo, barra real `display: none` solo en el teléfono, y el desplazamiento llega al final en los dos casos.

---

## 6. Arnés: QA-V2-04, -05 y -15

- **QA-V2-04.** La columna *Sondas* de `INDEX.md` agrega `⚠ aspecto: <sonda> <medido> vs <esperado>` cuando falla una sonda de aspecto. El resumen dice `… N sondas de aspecto que fallan` y avisa en la consola. En CAP-04b saqué la sonda `metric.hr.hud:bpm`: a densidad 1, el trazo de Inter 14/400 no tiene píxeles de cobertura plena y el color dominante es un borde. Ese color se mide en CAP-12b (2,625), donde da exacto. Elegí esto en lugar de una sonda por "píxel más saturado", que a 1 dp/px tampoco garantiza cobertura plena.
- **QA-V2-05.** CAP-17 comprueba `sim-hour-value == 14:00`, `Hora real`, `Remando` y las dos fallas visibles. La foto sigue como la aceptó el PO.
- **QA-V2-15.** VID-03 abre el panel antes de grabar y, en el segundo 1, toca `sim-drop-rower` con *hit-test* y cierra el panel en el mismo cuadro (cerrar lo oculta en el acto, DN-14), para que el "apagar" de 120 ms se vea entero.

---

## 7. Capturas de la It. 2 regeneradas

Regeneré `../02-dispositivo-simulador/snapshots/` con el código final: 41 tomas, 0 con problemas, 0 sondas de aspecto que fallan. Ese `manifest.json` lo leen pruebas (`SnapshotEvidenceTests`, `EditModeCaptureMatchesPlayModeTree`), así que tiene que ir con el código. Cambiaron 31 archivos y comparé cada uno con su versión anterior:

| Por qué | Tomas |
|---|---|
| QA-V2-13 (sin barra en el teléfono) | CAP-05: solo la franja de la derecha (x 2384-2394 px) |
| QA-V2-15 (panel en VID-03) | VID-03 |
| QA-V2-04 / -05 | `INDEX.md` (columna de sondas; CAP-04b `1/1`), `manifest.json` (también duraciones) |
| Tokens del HUD del Designer (QA-V2-12, [00-deuda-diseno §2](00-deuda-diseno.md)) | Las que muestran la etiqueta de la tarjeta del pulsómetro o `bpm`: AUX-01, AUX-02, CAP-01, -02, -04a, -04b, -06, -07, -08, -10 a -15, -18, -19, -22, -24, -25, VID-01, VID-02 (± 3 por canal, solo en esa etiqueta) y `rt1-measurement.json` (contrastes) |

Las tomas de escritorio sin la tarjeta (CAP-03, -09, -16, -17, -20, -21, -23) quedaron iguales byte a byte.

---

## 8. QA-V2-06 y QA-V2-14

### 8.1 Motivos de `[Ignore]` (QA-V2-06)

Ya no queda ninguno que diga "NO VERIFICABLE → It. 2". Cada uno dice ahora dónde se verifica:

- en PlayMode: `LanguageOnScreenTests.NoLanguageSelectorAndNoMemory`, `.LiveLanguageChangeKeepsEverythingElse` y `DeviceScreenTests.GridTextsLikeImage01`;
- en Unity EditMode y en la PC: `SystemLocaleProviderTests.WindowsListIsBcp47AndNotEmpty` y las cuatro de `AppIdentityTests`, con el detalle de 05-qa-verification de la It. 2;
- en GitHub Actions: las dos de `CiWorkflowTests` (el repositorio ya es público y `ci-core` corre).

También se actualizaron los comentarios de cabecera de `AppIdentityTests` y `CiWorkflowTests`. Los que apuntan a la It. 3 (AC-APP-09-07, subtítulos e íconos del editor), la 4a, la 10a y la 10b siguen como estaban.

### 8.2 CI de Unity con las suites del simulador (QA-V2-14)

El coordinador ya había dejado `ci-unity.yml` válido. Faltaba que corriera las suites del simulador:

- La matriz es `testMode × simulator` (4 jobs), y cada combinación tiene su caché de `Library/`.
- GameCI no permite `-executeMethod` antes de los tests. Por eso `tools/unity/ci-simulator-define.sh enable` hace lo mismo que `SimulatorDefine.EnableForTests`, sin abrir Unity: agrega `ROWMATE_SIMULATOR` a los tres `scriptingDefineSymbols` de `ProjectSettings.asset` y deja la misma marca en `Library/RowMate/`. Es solo ASCII, con fin de línea LF y `set -euo pipefail`.
- GameCI tampoco pasa variables de entorno al contenedor. La corrida avisa con `customParameters: -rowmateSimulatorTestRun`, y `SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator` lo acepta igual que `ROWMATE_SIMULATOR_TEST_RUN=1`.
- Al final, siempre, `restore` deja `ProjectSettings.asset` como en el commit y borra la marca antes de que se guarde la caché.
- El workflow no compila players, y `secrets` nunca va en un `if` de *job*.
- Sigue a pedido (`workflow_dispatch`): sin la licencia en los secretos (P-08) no puede correr.

Probado en la PC:

- el YAML pasó por un parser (4 jobs, los `if` y `customParameters` como se esperaba);
- el script corrió sobre una copia de `ProjectSettings.asset` en un repo de prueba: `enable` dos veces deja el mismo resultado y `restore` vuelve al commit sin la marca.

La corrida real en GitHub es **NO VERIFICABLE** hasta que esté la licencia. El README de `tools/unity` tiene la explicación.

---

## 9. Corridas (evidencia)

Carpetas propias en `<scratch>/it3/` (dotnet, Unity, builds, capturas). Una sola instancia de Unity por vez, siempre con `Temp/UnityLockfile` libre: el Designer no abrió Unity. El define siempre se restauró: `ProjectSettings.asset` sin cambios.

| Corrida | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -ArtifactsPath <scratch>/it3/debt`) | Build sin errores; **1273 pruebas: 1256 pasan, 0 fallan, 17 ignoradas** (simulador 149/149; EditMode 1124: 1107 + 17). Línea base al empezar: 1264 / 1247 / 0 / 17. La matriz con los XML de Unity, en §9.1 |
| Unity (4 suites) | Todas en verde, ver §9.1 |
| Arnés (`capture-snapshots.ps1`) | `listo: 41 tomas, 0 con problemas, 0 sondas de aspecto que fallan`; determinista (§3, §7) |
| Build normal Windows Mono | `Success`; `PerformanceTestRunCleaner` borró 4 entradas (los dos `.json`, `Assets/Resources` y su `.meta`); `BuildOutputInspector: sin rastros del simulador, de diagnósticos ni de tests`; sin `Assets/Resources` después |
| Build normal Windows IL2CPP | **3**: `NO VERIFICABLE: falta el módulo IL2CPP de Windows en este Editor.` |
| Build de simulador Windows Mono (Development) | `Success` (`PerformanceTestRunCleaner: 4 entradas …`); S-B1 con `-rowmate-color-probe`: `#2A2F37`, latencia 12/12, clic |
| Ventana del build normal (`capture-window.ps1`) | `RowMate` 1296 × 759, barra de título oscura `#201F25`; con los argumentos de diagnóstico, igual y sin JSON |

### 9.1 Corrida final de Unity (código definitivo)

`tools/unity/run-unity-tests.ps1` con Unity 6000.3.27f1, en serie, después del último cambio de código:

| Suite | Total | Pasan | Fallan | Ignorados | Antes (It. 2, QA) |
|---|---|---|---|---|---|
| EditMode | 1149 | 1133 | 0 | 16 | 1132 / 1116 / 0 / 16 |
| EditMode + simulador | 1298 | 1282 | 0 | 16 | 1281 / 1265 / 0 / 16 |
| PlayMode | 55 | 55 | 0 | 0 | 53 / 53 |
| PlayMode + simulador | 89 | 89 | 0 | 0 | 85 / 85 |

En todas las corridas con simulador, Enable y Restore salen con 0 y `ProjectSettings.asset` queda sin el define.

Las diferencias con la It. 2 son las pruebas nuevas:

- EditMode +17: 8 de `ItTwoDebtTests`, 5 de `SimulatorModeTests`, 3 de `WindowsTitleBarTests` y 1 nueva del Designer en `ContrastTests`;
- PlayMode +2: `OutgoingConnectedViewIsFrozen` y `VerticalScrollbarOnlyOnDesktop`;
- PlayMode + simulador +4: esas 2 y las 2 de QA-V2-01 con simulador.

**Matriz AC** (`run-tests.ps1 -Matrix -UnityResults <scratch>/it3/unity-final`): **PASA 285, FALLA 0, IGNORADO 1, PENDIENTE UNITY 0, SIN TEST 0** de 286 IDs. El ignorado es AC-APP-09-07, el primer campo de texto de esta iteración. **AC-BLE-11-02: PASA** (6 pruebas). AC-APP-05-01 suma las de `WindowsTitleBarTests`.

**Sin reescrituras.** Desde que existe el guardián, el `mtime` de las 9 fuentes y los 4 assets de URP no cambió con el arnés (dos veces), los 3 builds ni las 8 corridas de Unity. `git status` no muestra cambios en `Assets/RowMate/UI`, `Assets/RowMate/Settings` ni `ProjectSettings`, y no queda `Assets/Resources`.

---

## 10. Desvíos y decisiones (para el PO y QA)

| ID | Qué | Por qué | Quién decide |
|---|---|---|---|
| DD-01 | La barra de título sigue al tema del sistema al arrancar, al volver el foco y con un sondeo de 2 s; el legacy lo hace en el instante del aviso del sistema | Unity no expone el `WndProc` del player; un *subclassing* de la ventana sería frágil (IL2CPP, la ventana del motor). El caso real (cambiar el tema en Configuración) sale y vuelve el foco: se aplica en el acto | PO (AC-APP-05-10: "se actualiza sin reiniciar" se cumple; la demora es ≤ 2 s) |
| DD-02 | Los `FontAsset` generados se versionan con su tabla de kerning (hasta 6,8 MB) y no "vacíos" como sugería QA | Vaciarla cambia el aspecto y lo vuelve dependiente del orden (§3, medido). Lo que QA pedía (sin reescrituras, estado determinista y versionable) se cumple con el guardián | QA (aceptar la alternativa); Designer (kerning de textos nuevos, §3) |
| DD-03 | CAP-04b pierde la sonda de `bpm`; se mide en CAP-12b | A densidad 1 la sonda no puede medir un trazo fino (QA-V2-04, segunda opción que sugirió QA) | QA |
| DD-04 | Las capturas TV/TH se arman como teléfono (`IsDesktop = false`) | Es lo que dicen los perfiles; sin eso la regla de QA-V2-13 no se ve en las capturas | — |
| DD-05 | `ci-unity.yml` sigue a pedido (`workflow_dispatch`) | Sin la licencia en los secretos no corre; QA-V2-14 pedía además "en cada cambio": se activa al cargar la licencia (P-08) | El usuario (P-08) |

## 11. Archivos

Nuevos:

- `Assets/RowMate/Editor/Bootstrap/GeneratedAssetGuard.cs`
- `Assets/RowMate/Editor/Build/PerformanceTestRunCleaner.cs`
- `Assets/RowMate/Platform/Display/WindowsTitleBar.cs`
- `Assets/RowMate/Presentation/Controls/ScrollPolicy.cs`
- `Assets/RowMate/Simulator/SimulatorReleaseGuard.cs`
- `Assets/RowMate/Tests/EditMode/Project/ItTwoDebtTests.cs`
- `Assets/RowMate/Tests/UnityEditMode/WindowsTitleBarTests.cs`
- `tools/unity/ci-simulator-define.sh`
- los `.meta` de cada uno

Modificados:

- **App y Editor**: `Bootstrap/AppRoot.cs`, `Bootstrap/ColorProbe.cs`, `Editor/Bootstrap/{FontAssetBuilder,ProjectBootstrapper}.cs`, `Editor/Build/{BuildCli,BuildOutputInspector}.cs`, `Platform/{PlatformFactory,PlatformServices}.cs`, `Presentation/Device/{ConnectedView,DeviceScreenView,ScanView}.cs` y `SimulatorPresentation/SimulatorPanelView.cs`.
- **Arnés**: `Snapshots/{SnapshotCatalog,SnapshotManifest,SnapshotRunner}.cs` y `Tests/UiSupport/DeviceProfile.cs`.
- **Pruebas**: `DeviceScreenTests`, `DeviceSimulatorTests`, `SimulatorModeTests`, `SimulatorInUnityTests`, `ItTwoToolingTests`, `LanguageStateTests`, `LocaleResolverTests`, `RowingFormatsTests`, `AppIdentityTests` y `CiWorkflowTests`.
- **Herramientas y repo**: `.github/workflows/ci-unity.yml`, `.gitignore`, `tools/build/build.ps1` y `tools/unity/README.md`.
- **Documentación**: `docs/migration/architecture/adr/019-canal-de-color-de-la-ui.md`. El cambio refleja QA-V2-09, y además le saqué un dato del equipo que no va en un repositorio público.
- **Evidencia**: `docs/migration/iterations/02-dispositivo-simulador/snapshots/*`, regenerada (§7).

## 12. Lo que no entra acá (sigue abierto)

Es deuda de [REPORT de la It. 2 §7.2](../02-dispositivo-simulador/REPORT.md) que no estaba en esta tarea:

- **Arnés (Architect, durante la It. 3):**
  - sumar al catálogo las sondas de aspecto que mide el script del Designer;
  - que la toma falle si una transición queda congelada.
- **Diseño de la 10a:** el *lock* del *scheduler* para los *callbacks* nativos de Bluetooth.
- **Con teléfono:**
  - `:hover` con el dedo;
  - el costo de la copia de pantalla en Android de gama baja;
  - el color de la barra de estado de Android.
