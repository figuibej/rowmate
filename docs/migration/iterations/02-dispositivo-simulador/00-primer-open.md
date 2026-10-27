# It. 2 — Primer open real en Unity y tests dentro de Unity (R-T2)

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-27 |
| Iteración | 2 · `02-dispositivo-simulador` ([ROADMAP §It. 2](../../ROADMAP.md)) |
| Origen | R-T2 (primer open), R-T5 (tests dentro de Unity), R-T12 (`csc.rsp`), [00-deuda-it1 §4](00-deuda-it1.md) (lo que quedó para el primer open), AC-BASE-10 y AC-BASE-08 de [01-po-spec §5](01-po-spec.md) |
| Herramientas | Unity **6000.3.27f1** (`672d562a42a7`, licencia Personal), solo con el módulo Windows Standalone **Mono** (`PlaybackEngines/windowsstandalonesupport`; sin IL2CPP, Android, iOS ni Mac). .NET SDK 8.0.427 |
| Legacy | `git -C rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó nada del legacy en este paso |

---

## 0. Resumen

**El proyecto abre en Unity 6000.3.27f1 sin errores, con el bootstrapper aplicado, y las mismas pruebas pasan en Unity y en dotnet.** El primer open terminó con código 0 y sin `error CS`, pero el bootstrapper no había corrido. Tampoco funcionaban los tests dentro de Unity: la mitad quedaba inválida. Eran cinco defectos de la It. 1, escrita sin Editor, y los cinco están corregidos.

| # | Defecto | Dónde se vio | Corrección | Verificado |
|---|---|---|---|---|
| 1 | El bootstrapper **no corre en batchmode**: el `EditorApplication.delayCall` no llega a ejecutarse antes de `-quit` | Primer open: código 0, sin `[BOOTSTRAP]` ni `RowMateBootstrap.json` | `BootstrapPostprocessor` (`OnPostprocessAllAssets` con `didDomainReload`) → `ProjectBootstrapper.RunInBatchMode` (§3.3) | Open 2: 9 de 9 pasos `ok` |
| 2 | El renderer de URP queda **sin `PostProcessData`** (URP apaga el postprocesado de ese renderer) | `RowMate_Renderer.asset`: `postProcessData: {fileID: 0}` | `UrpProvisioner` asigna el de URP; bootstrapper **versión 2** (§3.4) | Open 3: versión 2 aplicada, `postProcessData` asignado |
| 3 | **Categorías con guiones**: el NUnit 3.5 de Unity deja *NotRunnable* todo test con `[Category("AC-…")]` | EditMode: 885 tests, 446 pasan, **439 fallan** (438 por esto) | `RowMate.Tests.CategoryAttribute` en Tests/Support (§4.1) | 0 por esta causa |
| 4 | `Has.Count` falla en NUnit 3.5 con un arreglo detrás de `IReadOnlyList<T>` | 1 de los 439 | `.Count, Is.EqualTo(n)` y regla en `NUnitCompatibilityTests` (§4.2) | 0 |
| 5 | `[SetCulture]` no tiene efecto en el Unity Test Framework | 2.ª corrida: 1 falla | Cultura del hilo a mano, en `try/finally`, y regla en `NUnitCompatibilityTests` (§4.3) | 0 |
| (6) | El procedimiento QA-V-05 **sale con 2**: Unity cuenta el Inconclusive buscado de `CommittedSettingsDoNotEnableTheSimulator` como rojo | `-Simulator`: 0 fallan, 1 inconcluso, código 2 | `ROWMATE_SIMULATOR_TEST_RUN=1` solo en la corrida de tests; el test pasa dentro de ella (§4.4) | Código 0 |

| Conteos finales | Total | Pasan | Fallan | Ignorados | Inconclusos | Código |
|---|---|---|---|---|---|---|
| dotnet (`run-tests.ps1`) | **961** (892 + 69 del simulador) | 944 | 0 | 17 | 0 | 0 |
| Unity EditMode, sin simulador | **888** | 872 | 0 | 16 | 0 | 0 |
| Unity EditMode, `-Simulator` (QA-V-05) | **957** (888 + 69) | 941 | 0 | 16 | 0 | 0 |

Las diferencias están todas explicadas en §5: hay 4 tests que solo existen en `net8.0` y 1 que solo corre dentro de Unity.

AC-BASE-10: **CUMPLE**. AC-BASE-08: **CUMPLE** en lo que se puede verificar hoy. Quedan ignoradas las pruebas "→ It. 2" que necesitan la UI de esta iteración o un equipo (§6).

---

## 1. Comandos exactos

`<S>` = `<scratch>`. `<U>` = `"C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe"`. Antes de cada corrida se verificó que no hubiera un Editor con el proyecto abierto: solo estaban la Hub y su cliente de licencias, que no se tocaron.

```
# 1) Primer open (Library/ vacía) y los opens siguientes
<U> -batchmode -quit -projectPath <repo> -logFile <S>/unity-first-open.log
<U> -batchmode -quit -projectPath <repo> -logFile <S>/unity-open2.log        # con el fix 1
<U> -batchmode -quit -projectPath <repo> -logFile <S>/unity-open3.log        # con el fix 2 (versión 2)
<U> -batchmode -projectPath <repo> -executeMethod QaRecompileProbe.ForceRecompile -logFile <S>/unity-qav03-probe.log   # QA-V-03, sonda temporal (§3.5)
<U> -batchmode -quit -projectPath <repo> -logFile <S>/unity-open4.log        # sin la sonda
<U> -batchmode -quit -projectPath <repo> -logFile <S>/unity-open-final.log   # final

# 2) Tests EditMode dentro de Unity, sin simulador (sin -quit)
<U> -batchmode -projectPath <repo> -runTests -testPlatform EditMode -testResults <S>/editmode-results.xml  -logFile <S>/editmode.log    # 1.ª
<U> … -testResults <S>/editmode2-results.xml -logFile <S>/editmode2.log        # 2.ª (fixes 3 y 4)
<U> … -testResults <S>/editmode3-results.xml -logFile <S>/editmode3.log        # 3.ª (fix 5)
<U> … -testResults <S>/editmode-final-results.xml -logFile <S>/editmode-final.log   # final

# 3) Tests del simulador con el procedimiento de QA-V-05 (desde la raíz del repositorio)
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Simulator -ArtifactsPath <S>/unity-sim    # 1.ª: código 2 (§4.4)
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Simulator -ArtifactsPath <S>/unity-sim2   # final: código 0

# 4) dotnet, después de cada cambio
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <S>/it2-firstopen
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <S>/it2-firstopen
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Ci -Filter "FullyQualifiedName~RepoHygiene|FullyQualifiedName~SimulatorInUnity|FullyQualifiedName~TestCategory|FullyQualifiedName~Bootstrapper" -ArtifactsPath <S>/it2-firstopen
```

---

## 2. Tiempos

| Corrida | Duración | Código | Notas |
|---|---|---|---|
| Primer open (sin `Library/`) | **75 s** | 0 | Refresh inicial 70,3 s: resolución de paquetes 29,4 s, compilación 15,2 s, 1 recarga de dominio (3,9 s), 2640 importaciones (932 scripts, 1707 no scripts). Muy por debajo de los 10 minutos previstos |
| Open 2 (bootstrapper v1) | 12 s | 0 | Compila `BootstrapPostprocessor` y aplica los 9 pasos |
| Open 3 (bootstrapper v2) | 25 s | 0 | Recompila todo: el cambio a Input System cambia los defines `ENABLE_*` |
| Sonda QA-V-03 | 28 s | 0 | Dos recargas de dominio en la misma sesión |
| Opens siguientes (con caché) | 8 a 15 s | 0 | |
| `-runTests` EditMode | 9 a 14 s | 2, 2, 0, 0 | Los tests en sí tardan 1 a 2 s |
| `run-unity-tests.ps1 -Simulator` | 43 s | 2, 0 | Tres arranques de Unity (activar, tests, restaurar) |
| `run-tests.ps1` (dotnet, incremental) | 5 a 7 s | 0 | |

---

## 3. Primer open

### 3.1 Paquetes

`[Package Manager] Done resolving packages in 29.39 seconds`, `Lock file was created`, 33 paquetes registrados y ningún error. Las versiones del manifest se resolvieron **exactas** (`ManifestAndVersionTests.ManifestHasExactlyTheDecidedPackages` sigue en verde: Unity no tocó `manifest.json`).

| Origen | Paquetes (versión resuelta) |
|---|---|
| Registro `packages.unity.com` | animation.rigging 1.4.1, ide.visualstudio 2.0.28, inputsystem 1.20.1, mobile.android-logcat 1.4.7, nuget.newtonsoft-json 3.2.2. Dependencias: searcher 4.9.5, burst 1.8.31, mathematics 1.3.3, collections 2.7.1, nuget.mono-cecil 1.11.6, test-framework.performance 3.5.0 |
| Integrados en el Editor | render-pipelines.universal 17.3.0, shadergraph 17.3.0, test-framework 1.6.0, ugui 2.0.0, los 13 `com.unity.modules.*` del manifest. Dependencias: render-pipelines.core 17.3.0, universal-config 17.0.3, **ext.nunit 2.0.5** (el test-framework pide 2.0.3; el Editor trae 2.0.5), modules.imageconversion y hierarchycore |
| Git | `com.gilzoide.sqlite-net` `#1.3.3` → `34340bf93526b5143ac404f13cb5c4fefabaa461` |

Lo nuevo es `Packages/packages-lock.json` (33 entradas, se versiona: ARCHITECTURE §16).

### 3.2 Compilación de cada asmdef

Sin `error CS`, sin `warning CS`, sin `Scripts have compiler errors` ni `Compilation failed` en ningún log.

| Asmdef | Primer open | Con `ROWMATE_SIMULATOR` (corrida QA-V-05) |
|---|---|---|
| RowMate.Core, App, Infrastructure, Platform, Presentation, Scene, Bootstrap, Editor | Compilan | Compilan |
| RowMate.Tests.Support, Tests.EditMode, Tests.PlayMode | Compilan | Compilan |
| RowMate.Simulator, Simulator.Presentation, Tests.Simulator | No se compilan (`defineConstraints`), como se diseñó | Compilan |

Las referencias por nombre a assemblies de paquetes (`Unity.InputSystem`, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime`/`.Editor`, `Unity.Animation.Rigging`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`) resolvieron todas. **R-T12 resuelto**: las respuestas de compilación de Unity (`Library/Bee/artifacts/…/RowMate.*.rsp`) llevan `-nullable:enable` (de `Assets/csc.rsp`) y `-langversion:9.0` en todos los asmdefs, igual que `tools/dotnet`.

### 3.3 Defecto 1: el bootstrapper no corría en batchmode

**Qué pasó.** El primer open salió con código 0, pero el log no tenía ninguna línea `[BOOTSTRAP]` y no había `ProjectSettings/RowMateBootstrap.json`. `ProjectSettings.asset` quedó con los valores de fábrica (`companyName: DefaultCompany`, `AndroidMinSdkVersion: 25`, `activeInputHandler: 0`, color *Gamma*).

**Por qué.** El `[InitializeOnLoad]` agenda `RunOncePerSession` con `EditorApplication.delayCall`. Con `-batchmode -quit`, Unity sale apenas termina el refresh inicial y no da ninguna vuelta del loop del Editor, así que el `delayCall` nunca corre. Con `-executeMethod` pasa lo mismo. Con `-runTests` sí correría, pero en medio de la corrida de tests.

**Corrección** (`Editor/Bootstrap/BootstrapPostprocessor.cs`, nuevo, y `ProjectBootstrapper.cs`):

- `BootstrapPostprocessor : AssetPostprocessor`. `OnPostprocessAllAssets(…, bool didDomainReload)` llega al final del refresh de **cada** recarga de dominio, también en batchmode, y ahí se pueden crear assets. Es el gancho que usa URP 17 para crear su Global Settings (`UniversalRenderPipelineGlobalSettingsPostprocessor`). Si `didDomainReload && Application.isBatchMode`, llama a `ProjectBootstrapper.RunInBatchMode()`.
- `RunInBatchMode()` usa **la misma marca de sesión** (`SessionState`, QA-V-03) y corre en el acto, sin mirar `IsBusy()`: dentro del postprocesado el Editor figura `isUpdating` y reagendar sería no correr nunca.
- El Editor con ventana sigue con el `delayCall`, porque ahí puede mostrar el diálogo de reinicio del Input System. En batchmode, si el `delayCall` llega a correr (con `-runTests`), encuentra la marca y no hace nada.
- `Run(force)` → `Apply(force)`. Cuando la versión ya está aplicada, `Apply` deja **una** línea por sesión: `[BOOTSTRAP] versión 2 ya aplicada (ProjectSettings/RowMateBootstrap.json): sin cambios.`. Así el log muestra que el intento automático corrió.

**Resultado (open 2).** `[BOOTSTRAP] versión 1 aplicada`, con los 9 pasos `ok`. Cada valor se comprobó en el `.asset`:

| Paso | Detalle del estado | Comprobado en `ProjectSettings/` |
|---|---|---|
| `player.identity` | `iguisoft/RowMate 1.0.0 (1); Android com.iguisoft.rowmate; iOS y Standalone com.iguisoft.rowmate.unity` | `companyName: iguisoft`, `productName: RowMate`, `bundleVersion: 1.0.0`, `applicationIdentifier` Android `com.iguisoft.rowmate`, Standalone e iPhone `com.iguisoft.rowmate.unity`, `buildNumber` Standalone/iPhone 1, `AndroidBundleVersionCode: 1`. Windows sin id propio (D-02) |
| `player.scripting` | IL2CPP + .NET Standard 2.1 | `scriptingBackend` Android/Standalone/iPhone = 1 (IL2CPP). `apiCompatibilityLevel: 6` (.NET Standard 2.1): Unity no guarda la entrada por plataforma porque coincide con el valor global |
| `player.android` | minSdk 26, target automático, ARM64, Internet | `AndroidMinSdkVersion: 26`, `AndroidTargetSdkVersion: 0` (automático), `AndroidTargetArchitectures: 2` (ARM64), `ForceInternetPermission: 1` |
| `player.ios` | iOS 15.0 | `iOSTargetOSVersionString: 15.0` |
| `player.rendering` | Linear; Android Vulkan+GLES3; Windows DX11 | `m_ActiveColorSpace: 1`; `m_BuildTargetGraphicsAPIs`: AndroidPlayer `150000000b000000` (Vulkan 21, GLES3 11), WindowsStandaloneSupport `02000000` (D3D11), los dos con `m_Automatic: 0` |
| `player.runtime` | runInBackground y Frame Timing Stats | `runInBackground: 1`, `enableFrameTimingStats: 1` |
| `player.input` | Input System (reiniciar el Editor) | `activeInputHandler: 1`. Advertencia esperada: `[BOOTSTRAP] Manejador de entrada cambiado a Input System: reiniciá el Editor.` En la versión 2: `skipped - ya era Input System` |
| `urp.provisional` | `creado: renderer pipeline; …` | `GraphicsSettings.m_CustomRenderPipeline` y `customRenderPipeline` en los **6** niveles de calidad existentes (Very Low a Ultra; no se crearon ni borraron niveles, QA-P-10) = `RowMate_URP_Default` (`63a45525…`), con `m_RendererDataList` = `RowMate_Renderer` |
| `defines.check` | `ROWMATE_SIMULATOR ausente en Standalone, Android e iOS` | `scriptingDefineSymbols: {}` |

### 3.4 Defecto 2: el renderer de URP sin `PostProcessData` (versión 2)

`UrpProvisioner` creaba el renderer con `ScriptableObject.CreateInstance<UniversalRendererData>()`, que deja `postProcessData = null`. Con eso, URP apaga el postprocesado de ese renderer. ART_BIBLE pide bloom, tonemapping ACES y ajustes de color, así que el problema iba a aparecer recién en la It. 6a. El menú de URP (*Create > Rendering > URP Asset (with Universal Renderer)*), que es el respaldo manual del mismo bootstrapper, sí lo asigna: `UniversalRenderPipelineAsset.CreateRendererData` usa `PostProcessData.GetDefaultPostProcessData()`.

**Corrección.** Se asigna `Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset` al crear el renderer. A un renderer de la versión 1 se le agrega sin recrearlo, así conserva su GUID. `CurrentVersion = 2`: subir la versión es el mecanismo para que el próximo open reaplique los pasos, que son idempotentes.

**Resultado (open 3).** `[BOOTSTRAP] versión 2 aplicada`, `urp.provisional: ok - creado: postprocesado; …` (la redacción del detalle se mejoró después: `PostProcessData agregado al renderer existente; …`), `player.input: skipped`, el resto `ok`. `RowMate_Renderer.asset`: `postProcessData: {fileID: 11400000, guid: 41439944d30ece34e96484bdb6645b55}`.

### 3.5 QA-V-03: un solo intento por sesión, verificado en el Editor

Para comprobar que el bootstrapper no se repite en cada recompilación, se usó una **sonda temporal**: `Assets/QaProbeTemp/Editor/QaRecompileProbe.cs`. La sonda cuenta las recargas de dominio con `SessionState`, pide `CompilationPipeline.RequestScriptCompilation` con `-executeMethod` y sale con `EditorApplication.Exit(0)` en la segunda recarga, con un límite de 240 s. El log muestra `[QAPROBE] recarga de dominio 1`, `[BOOTSTRAP] versión 2 ya aplicada …` **una sola vez**, `[QAPROBE] pido recompilación`, `Requested script compilation because: Requested through public api`, `[QAPROBE] recarga de dominio 2` (sin otro `[BOOTSTRAP]`) y `[QAPROBE] fin`. Después se borró la sonda con sus `.meta` y la carpeta, y el open 4 quedó limpio.

**Sigue NO VERIFICABLE en este paso**: el camino del Editor con ventana (el `delayCall` y el diálogo del Input System). Solo se puede ver abriendo el Editor a mano. El camino de un paso que falla (aviso y reintento en el próximo open) lo cubre el test estático, porque ningún paso falló de verdad.

---

## 4. Tests dentro de Unity: errores y correcciones

`RowMate.Tests.UnityApiCheck` compila las fuentes de test contra NUnit 3.5.0, pero **no las corre**. Los tres defectos siguientes compilan igual y solo aparecieron al correr los tests dentro de Unity (R-T5).

### 4.1 Defecto 3: categorías con guiones (438 tests)

La primera corrida dio 885 tests: **446 pasan, 439 fallan** y 0 ignorados. 438 de las fallas tenían `runstate="NotRunnable"`, `label="Invalid"` y el mensaje `Category name must not contain ',', '!', '+' or '-'`. Las ignoradas también quedaban inválidas. La validación está en `CategoryAttribute.ApplyToTest` del NUnit 3.5 de Unity (`com.unity.ext.nunit` 2.0.5). El NUnit 3.14 de dotnet ya no la tiene. Todos los IDs de trazabilidad (`AC-…`, `QA-V-…`, `LT-…`) llevan guiones, y hay 473 `[Category("…")]` en 47 archivos.

**Corrección: `Assets/RowMate/Tests/Support/CategoryAttribute.cs`.** `RowMate.Tests.CategoryAttribute : NUnit.Framework.CategoryAttribute, IApplyToTest` re-implementa `ApplyToTest` sin la validación y agrega la misma propiedad `Category`.

- Está en el namespace `RowMate.Tests` **a propósito**. En los archivos de test (namespace `RowMate.Tests.*`), C# busca `Category` primero en los namespaces que contienen al código, y recién después en los `using` del archivo, por donde entra `NUnit.Framework`. Así ninguna fuente de test cambia, y tampoco `TraceabilityTests`, que lee `Category("…")` en el texto, ni `run-tests.ps1 -Matrix`, que lee la propiedad `Category` del XML. Los filtros `-testCategory` de Unity y `--filter TestCategory=…` de dotnet siguen iguales.
- Se eligió esto antes que renombrar 473 atributos o cambiar los IDs, que son los del PO y los documentos. El riesgo de un sombreado así es que alguien no lo vea. Por eso hay tres controles nuevos en `Tests/EditMode/Project/TestCategoryTests.cs` (`AC-BASE-08`):
  - `CategoriesWithHyphensRunAndAreReported`: corre en Unity y la propiedad `Category` llega.
  - `EveryCategoryAttributeIsTheRowMateOne`: por reflexión, las más de 400 categorías del assembly usan la clase de RowMate.
  - `TestSourcesUseTheRowMateCategory`: en el texto de `Tests/**`, también del simulador, no hay ningún `using NUnit.Framework` dentro del bloque `namespace`, ninguna categoría calificada y ninguna categoría fuera de `RowMate.Tests.*`.
- `[TestCase(…, Category = "…")]` y `TestCaseData.SetCategory("…")` no validan en NUnit 3.5 y no necesitaron cambios.

### 4.2 Defecto 4: `Has.Count` (1 test)

`JsonParserTests.ParsesEveryKindOfValue` falló con `System.ArgumentException : Property Count was not found`. En NUnit 3.5, `Has.Count` busca por reflexión una propiedad pública `Count` en el tipo real, y `JsonNode.Items` es un arreglo detrás de `IReadOnlyList<T>`: el arreglo solo tiene `Length`. Se cambiaron los 3 usos del proyecto por `.Count, Is.EqualTo(n)`: el que fallaba y dos de `RoutinePositionsTests`, que pasaban porque es una `List<T>`. `NUnitCompatibilityTests.NoUnsupportedNUnitApis` ahora prohíbe `Has.Count`.

### 4.3 Defecto 5: `[SetCulture]` (1 test)

Con las categorías corregidas quedó una sola falla, `CultureIndependenceTests.SameTextsWithSetCultureAttribute`: `Expected: ","  But was: "."`. La precondición del test (el separador decimal de `de-DE`) no se cumplía porque el Unity Test Framework no aplica `[SetCulture]`. `UnityTestExecutionContext.CurrentCulture` es una propiedad simple que no toca el hilo, a diferencia del `TestExecutionContext` de NUnit. El test pasa a llamarse `SameTextsWithTheThreadCulture` y fija a mano `Thread.CurrentThread.CurrentCulture`/`CurrentUICulture` en `de-DE` dentro de `try/finally`, que es lo mismo que hace el atributo en NUnit. Conserva la precondición, así que en Unity también prueba que la cultura de Mono da `","`. `NUnitCompatibilityTests` ahora prohíbe `SetCulture(`/`SetUICulture(`.

### 4.4 (6) El procedimiento QA-V-05 salía con 2

En la primera corrida con `-Simulator`, Unity dio 957 tests, 940 pasan, **0 fallan**, 16 ignorados y 1 inconcluso: `CommittedSettingsDoNotEnableTheSimulator`, Inconclusive a propósito según 00-deuda-it1 §4.3. Aun así, el log decía `Test run completed. Exiting with code 2 (Failed). One or more tests failed.`: el Unity Test Framework cuenta un Inconclusive como rojo. El script prometía `0` = todo verde.

**Corrección.** `tools/unity/run-unity-tests.ps1` pone `ROWMATE_SIMULATOR_TEST_RUN=1` en el entorno **solo** alrededor de `-runTests` y la saca en el `finally`, antes de restaurar. Unity la hereda. El test distingue tres casos:

- Marca y variable: es la corrida en curso → `Assert.Pass`.
- Marca sin variable: la corrida quedó cortada → sigue Inconclusive y pide `-RestoreOnly`.
- Sin marca: en el CI falla y en la PC queda Inconclusive, como antes.

R-01 no se debilita. Fuera de la corrida no hay variable, la guardia de builds sigue mirando la marca y el CI no tiene `Library/`. `SimulatorInUnityTests.ScriptEnablesRunsAndAlwaysRestores` controla que la variable se pone una sola vez, justo antes de `try`, y se saca primero en el `finally`.

**Resultado final.** Las tres corridas salen con 0 y el script termina con 0. `[SIMULATOR] ROWMATE_SIMULATOR activado para los tests en Standalone, Android e iOS.` → tests → `[SIMULATOR] defines restaurados; ROWMATE_SIMULATOR queda en: ninguna plataforma.` Al final, `ProjectSettings.asset` no tiene el define y `Library/RowMate/simulator-tests.txt` no existe.

---

## 5. Resultados: Unity contra dotnet

| | dotnet | Unity sin simulador | Unity `-Simulator` |
|---|---|---|---|
| Generales (`RowMate.Tests.EditMode`) | 892 (875 + 17 ign.) | 888 (872 + 16 ign.) | 888 (872 + 16 ign.) |
| Simulador (`RowMate.Tests.Simulator`) | 69 (69) | — (no existe sin el define) | 69 (69) |
| **Total** | **961: 944 pasan, 17 ignorados** | **888: 872 pasan, 16 ignorados** | **957: 941 pasan, 16 ignorados** |
| Fallan / inconclusos | 0 / 0 | 0 / 0 | 0 / 0 |

**Por qué los números difieren** (las dos diferencias estaban previstas):

1. **4 tests existen solo en `net8.0`** (`#if NET5_0_OR_GREATER`, ausentes en el XML de Unity): `EventStreamTests.EmittingDoesNotAllocate` y `ObservableValueTests.EmittingDoesNotAllocate` (medición de memoria con `GC.GetAllocatedBytesForCurrentThread`), `DartMathTests.RoundToFixedEqualsParsingTheFixedText` (usa el parser de .NET 8, que redondea correctamente) y `FmtFixedTests.TryFormatFixedDoesNotAllocateOnTheFastPath`. 892 − 4 = 888.
2. **`ManifestAndVersionTests.ProjectOpensWithoutCompileErrors` corre solo en Unity**. En dotnet sigue ignorado (`#if !UNITY_EDITOR [Ignore]`). En Unity pasa y comprueba que están cargados los 11 assemblies de RowMate, y los 2 del simulador cuando está el define. Por eso los ignorados son 17 en dotnet y 16 en Unity, y los que pasan, 875 − 4 + 1 = 872.

Los 16 ignorados de Unity son los "NO VERIFICABLE" que necesitan UI, equipo, Mac o GitHub (§6). Fuera de los nombres para mostrar, que NUnit 3.5 y 3.14 formatean distinto en los `double` de `[TestCase]` (`-0.0d` contra `0.0d`, `4.5035996273705E+15d`), es el mismo conjunto de tests con el mismo resultado.

**Contra la It. 1 y la deuda.** 912 (It. 1) → 956 (deuda, [00-deuda-it1 §2](00-deuda-it1.md)) → **961**: 5 tests nuevos y ninguno borrado.

- `BootstrapperStaticTests`: `RunsInBatchModeAfterTheDomainReload` (fix 1) y `RendererHasTheDefaultPostProcessData` (fix 2).
- Los 3 de `TestCategoryTests` (fix 3).
- `BootstrapperStaticTests.RunsOnFirstOpen`, que estaba ignorado, pasa a ser `RanOnTheFirstOpenAndLeftItsState`. Corre en dotnet y en Unity y lee el `RowMateBootstrap.json` versionado: versión = `CurrentVersion`, editor `6000.3.27f1`, los 9 pasos en orden y `ok`/`skipped`, y el simulador ausente. Por eso los ignorados de dotnet bajan de 18 a 17.
- `SameTextsWithSetCultureAttribute` pasa a llamarse `SameTextsWithTheThreadCulture`.

Matriz AC → test (`-Matrix`): **PASA 157, FALLA 0, IGNORADO 1, SIN TEST 0**, igual que antes (el ignorado sigue siendo AC-APP-09-07). Con `-Ci`, `RepoHygiene`, `SimulatorInUnity`, `TestCategory` y `Bootstrapper` dan 63 de 63.

---

## 6. Criterios de aceptación

**AC-BASE-10 · El proyecto abre sin errores: CUMPLE.**

| Lo que pide | Evidencia |
|---|---|
| Abre en 6000.3.27f1 sin `error CS`, `Scripts have compiler errors` ni `Compilation failed` | Ninguno en los 6 logs de open ni en los de tests (§3.2) |
| La apertura en batch termina con código 0 | 0 en todos los opens, incluido el primero y el final (`unity-open-final.log`: `return code 0`) |
| `ProjectVersion.txt` sigue en `6000.3.27f1` | `m_EditorVersion: 6000.3.27f1` y la línea que agregó Unity, `m_EditorVersionWithRevision: 6000.3.27f1 (672d562a42a7)`. `ProjectVersionIs6000_3_27f1` sigue en verde |
| La preparación automática aplica sus pasos una vez | Open 2: versión 1, 9 pasos. Open 3: versión 2. Opens siguientes: `ya aplicada: sin cambios`. Estado en `RowMateBootstrap.json` (`RanOnTheFirstOpenAndLeftItsState`) |
| Si uno falla, avisa y no se repite en cada recompilación (QA-V-03) | Sonda: dos recargas en la sesión, un solo `[BOOTSTRAP]` (§3.5). El aviso de un paso fallido lo cubre el test estático; no hubo fallas reales |
| No activa el modo simulador | `defines.check: ok - ROWMATE_SIMULATOR ausente…`, `scriptingDefineSymbols: {}` |

**AC-BASE-08 · Las pruebas del Core dentro de Unity: CUMPLE en lo que se puede verificar hoy.**

| Lo que pide | Evidencia |
|---|---|
| Todas las pruebas del Core compilan y corren en Unity EditMode con el mismo resultado que en dotnet: 0 fallan | 888 y 957 tests, 0 fallan, 0 inconclusos; diferencias explicadas en §5 |
| Las del simulador corren con el procedimiento de QA-V-05 | `run-unity-tests.ps1 -Simulator`: 69 de 69 y el define restaurado (§4.4) |
| Las ignoradas "NO VERIFICABLE → It. 2" que se pueden correr en el Editor dejan de estar ignoradas | Las dos de AC-BASE-10 ya corren (§5). **Siguen ignoradas**, porque necesitan algo que todavía no existe: las de **UI de esta iteración** (`LanguageStateTests.NoSelectorOnScreen` y `VisibleChangeWithoutRestart`, `RowingFormatsTests.DeviceGridShowsTheTextsOnScreen`), que se activan cuando estén el shell y Dispositivo, en PlayMode; las de **equipo** (`LocaleResolverTests.NativeListOnDevices`, `AppIdentityTests` AC-APP-16-01 a -04), y las de otras iteraciones (It. 3, 4a, 10a/10b, CI) |

---

## 7. Lo que generó Unity

Todo lo que sigue se versiona, salvo lo marcado como ignorado. No se borró nada generado por Unity.

| Qué | Detalle |
|---|---|
| `.meta` | **262** en `Assets/` (213 de archivos y 49 de carpetas), uno por cada archivo y carpeta propios, incluidas las fuentes `.ttf` e íconos de `UI/` y `csc.rsp.meta`. Los de la sonda temporal se borraron |
| `ProjectSettings/` | 21 `.asset` nuevos: AudioManager, ClusterInputManager, DynamicsManager, EditorBuildSettings, EditorSettings, GraphicsSettings, InputManager, MemorySettings, MultiplayerManager, NavMeshAreas, Physics2DSettings, PresetManager, ProjectSettings, QualitySettings, ShaderGraphSettings, TagManager, TimeManager, **URPProjectSettings**, UnityConnectSettings, VFXManager y VersionControlSettings. Además, `RowMateBootstrap.json` (bootstrapper, versión 2) y la segunda línea de `ProjectVersion.txt` |
| `Packages/packages-lock.json` | 33 paquetes (§3.1) |
| URP (bootstrapper) | `Assets/RowMate/Settings/URP/RowMate_Renderer.asset` (Universal Renderer, Forward, con `PostProcessData`) y `RowMate_URP_Default.asset`, con sus `.meta` y los de las carpetas `Settings/` y `Settings/URP/` |
| URP (el propio paquete, en la raíz de `Assets/`) | `Assets/UniversalRenderPipelineGlobalSettings.asset` (*"URP Global Settings Asset has been created for you"*) y `Assets/DefaultVolumeProfile.asset`, que ese asset referencia. Ver §8, punto 3 |
| Raíz del repositorio | `.vsconfig` (carga de trabajo de Visual Studio para Unity; se versiona, como en las plantillas de Unity). `rowmate.slnx` y un `*.csproj` por assembly (15 con los del simulador) de `com.unity.ide.visualstudio`: los `.csproj` ya estaban ignorados, y para `*.slnx` se agregó una regla a `.gitignore` (`!/tools/**/*.slnx` queda permitido) |
| Ignorados | `Library/` (incluida la carpeta vacía `Library/RowMate/` que deja `SimulatorDefine`), `Temp/`, `Logs/` y `UserSettings/`. Unity también guarda una copia de los resultados en `AppData/LocalLow/iguisoft/RowMate/TestResults.xml`: ya usa la carpeta de AC-APP-16-02 |

---

## 8. Advertencias y pendientes

1. **NO VERIFICABLE por módulos.** Solo está el módulo Windows Standalone **Mono**: sin IL2CPP de Windows, Android, iOS ni Mac. Ninguno de esos builds se intentó (AC-BASE-01 con IL2CPP y los AC de equipo). Los ajustes ya quedaron puestos: el backend IL2CPP en Standalone hace que un build de Windows falle hasta que se instale el módulo, y eso es lo esperado.
2. **Advertencias del log sin acción**:
   - En la primera importación, `WARNING: Shader Unsupported: 'Universal Render Pipeline/Terrain/Lit' - All subshaders removed` y `dependency … not found`, de los shaders de Terrain y SpeedTree del paquete URP. Salen mientras se importan antes de que haya pipeline asignado y no vuelven en los opens siguientes.
   - `Native extension for WindowsStandalone target not found`.
   - `Curl error 6: Could not resolve host: config.uca.cloud.unity3d.com / cdp.cloud.unity3d.com`, de los servicios de nube de Unity. El registro de paquetes sí respondió.
   - `d3d12: failed to query info queue interface`.
3. **Assets de URP en la raíz de `Assets/`.** URP crea `UniversalRenderPipelineGlobalSettings.asset` y `DefaultVolumeProfile.asset` en la raíz. Funcionan donde están: URP los encuentra por GUID desde `GraphicsSettings.m_RenderPipelineGlobalSettingsMap`. Propuesta para el bootstrapper completo de la It. 2: moverlos con `AssetDatabase.MoveAsset` a `Assets/RowMate/Settings/URP/`, que conserva el GUID, para que todo lo propio quede bajo `Assets/RowMate`. Lo decide el diseño técnico.
4. **Marca del motor (P-01).** `m_ShowUnitySplashScreen: 1`. El bootstrapper mínimo no lo toca; le corresponde al bootstrapper completo de la It. 2 (01-po-spec §12, punto 12).
5. **El Editor ahora usa D3D11.** Antes del bootstrap usaba D3D12; con la lista de Windows en D3D11 solo (ARCHITECTURE §16), el Editor arrancó con `Direct3D 11.0`. Tenerlo en cuenta para la medición de R-T1 (vidrio) en el Editor.
6. **Input System solo (`activeInputHandler: 1`).** Ya está aplicado. El primer open con ventana ya arranca con el Input System, sin pedir reinicio. Cualquier `UnityEngine.Input` del manejador viejo lanza una excepción.
7. **Camino con ventana del bootstrapper** (`delayCall` y diálogo): NO VERIFICABLE en batchmode (§3.5). Para verlo, abrir el proyecto en el Editor y usar el menú `RowMate/Bootstrap/Reaplicar`.
8. **`UnityApiCheck` solo compila.** Los defectos 3 a 5 compilan contra NUnit 3.5.0 y solo se ven al correr. La red de seguridad real es correr `tools/unity/run-unity-tests.ps1` (y `-Simulator`) en cada iteración y, si se habilita, en `ci-unity.yml` (AC-APP-19-05, opcional). Las reglas nuevas de `NUnitCompatibilityTests` y `TestCategoryTests` atrapan en dotnet los casos ya conocidos.
9. **`RowMateBootstrap.json` lleva `appliedAtUtc`.** Cambia en cada reaplicación forzada, y eso genera un diff que hay que versionar (es lo que se busca: queda registro de cuándo se aplicó).
10. **PlayMode.** `RowMate.Tests.PlayMode` compila pero no tiene tests todavía. No se corrió `-testPlatform PlayMode`; los primeros llegan con la UI de la It. 2.

---

## 9. Archivos tocados

**Editor (solo Unity):** `Assets/RowMate/Editor/Bootstrap/BootstrapPostprocessor.cs` (nuevo), `ProjectBootstrapper.cs` (`RunInBatchMode`, `Apply`, línea "ya aplicada", `CurrentVersion = 2`) y `UrpProvisioner.cs` (`PostProcessData`).

**Tests:**
- Nuevos: `Assets/RowMate/Tests/Support/CategoryAttribute.cs` y `Tests/EditMode/Project/TestCategoryTests.cs`.
- Cambiados: `Project/BootstrapperStaticTests.cs` (3 tests nuevos o activados; `CurrentVersion = 2`; excepción para las rutas `Packages/com.unity.*` en el control de literales), `Project/ManifestAndVersionTests.cs` (`ProjectOpensWithoutCompileErrors` en Unity; comentario de R-T12), `Project/NUnitCompatibilityTests.cs` (prohíbe `Has.Count` y `SetCulture`), `Project/SimulatorInUnityTests.cs` (variable de la corrida en curso), `Project/AsmdefLintTests.cs` (comentario), `Text/CultureIndependenceTests.cs`, `Common/JsonParserTests.cs` y `Models/RoutinePositionsTests.cs`.

**Herramientas:** `tools/unity/run-unity-tests.ps1` (`ROWMATE_SIMULATOR_TEST_RUN`; sigue siendo solo ASCII), `tools/unity/README.md` (estado y código 2) y `.gitignore` (`*.slnx`).

**Generado por Unity:** §7.

**Documentos:** `architecture/ARCHITECTURE.md` (§15.1, §16 y "Cambios de la It. 2 (primer open real)"), ADR 013 y ADR 014 ("Cambios de la It. 2 (primer open real)"). No se tocaron `STATUS.md` (del PO), los documentos cerrados de la It. 1, `00-deuda-it1.md` (su §4 queda como registro de lo que se esperaba; este documento lo cierra) ni `rowmate-legacy`.
