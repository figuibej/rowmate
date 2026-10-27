# It. 2 — Deuda técnica de la It. 1 (Architect)

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-27 |
| Iteración | 2 · `02-dispositivo-simulador` ([ROADMAP §It. 2](../../ROADMAP.md)) |
| Origen | [REPORT §7.1 y §9.2](../01-fundaciones/REPORT.md), [05-qa-verification §6](../01-fundaciones/05-qa-verification.md) |
| Alcance | QA-V-01 a QA-V-08 (las del Architect). QA-V-09 es del Designer; QA-V-10 ya estaba resuelto; QA-V-11 es la decisión Q-92 de la It. 3 |
| Herramientas | .NET SDK 8.0.427. **Unity no se abrió en este paso** (pedido del coordinador): lo que depende del Editor queda revisado de forma estática y se verifica en el primer open (§4) |
| Legacy | `git -C rowmate-legacy status --porcelain` vacío al empezar y al terminar. Ver la nota de §5.6 |

---

## 0. Resumen

| ID | Qué pasaba | Qué se hizo | Verificado con dotnet | Falta en Unity |
|---|---|---|---|---|
| QA-V-01 | La excepción de un suscriptor cortaba la notificación a los demás y subía a quien asignó | `ObservableValue` y `EventStream` atrapan la excepción de cada suscriptor, la registran en su `IAppLogger` (`Error`, tag `STATE`) y siguen. Logger obligatorio por constructor | 9 tests nuevos | — (Core puro) |
| QA-V-02 | `.gitignore` solo cubría `StravaSecrets*.cs`; la higiene solo miraba lo versionado | `.gitignore` con `StravaSecrets*` de cualquier extensión; la higiene mira también lo que `git add -A` agregaría y se prueba en un repositorio git temporal | 23 casos nuevos, también con `-Ci` | — |
| QA-V-03 | El *bootstrapper* reintentaba en cada recarga de dominio | Un intento automático por sesión del Editor (`SessionState`); un paso fallido se reintenta en el próximo open | Test estático | Ver que no se repite al recompilar |
| QA-V-04 | `namespace RowMate.Editor` tapaba a `UnityEditor.Editor` | Namespace `RowMate.EditorTools` (el assembly sigue siendo `RowMate.Editor`) + regla general | 2 tests | Compilar |
| QA-V-05 | Los 69 tests del simulador no existen en Unity sin `ROWMATE_SIMULATOR` | `tools/unity/run-unity-tests.ps1 -Simulator`: activa el define solo para la corrida y lo restaura siempre; `SimulatorDefine` (menú y tests) y `SimulatorBuildGuard` | 6 tests estáticos | **La corrida real** |
| QA-V-06 | El test de textos idénticos comparaba el patrón, no el texto visible | Test nuevo que compara `Localizer.Get` (151 claves) y `Localizer.Format` (4 claves) con el legacy | 2 casos (es, en) | — |
| QA-V-07 | `-Demo` imprimía cada `[DEMO]` dos veces | `-Demo` con `verbosity=normal` y `NUnit.ConsoleOut=1` | Test + corrida: 22 líneas, 22 distintas | — |
| QA-V-08 | El texto principal de ADR 011 y 012 contradecía D-06 y D-01 | Texto principal reescrito (y, de paso, el resto de §3 de ADR 012) | Revisión | — |

Pruebas: **912 → 956** (887 generales + 69 del simulador): **938 pasan, 0 fallan, 18 ignoradas** (las mismas 18 "NO VERIFICABLE" de la It. 1). Compilación: 9 proyectos, 0 advertencias, 0 errores (§2).

---

## 1. Cada issue

### 1.1 QA-V-01 — Una vista rota no congela a las otras

**Problema.** `Notify()` y `Emit()` llamaban a los suscriptores sin protección. Si la vista A lanzaba al recibir `connected`, el setter lanzaba, el valor quedaba en `connected` y el servicio B nunca se enteraba (y no se iba a enterar: un `connected` igual no notifica). El comentario prometía "semántica de `ChangeNotifier`", que sí aísla cada *listener*.

**Solución** (`Core/Common/ObservableValue.cs`, `EventStream.cs`, nuevo `SubscriberErrors.cs`):

- Cada llamada a un suscriptor va en su `try/catch`. La excepción se registra con `IAppLogger.Log(LogLevel.Error, "STATE", "un suscriptor de ObservableValue<String> lanzó InvalidOperationException: vista rota", excepción)` y se sigue con el siguiente. Es lo que hace el legacy: `ChangeNotifier.notifyListeners` atrapa, reporta con `FlutterError.reportError` y sigue; un `StreamController.broadcast` manda el error a la zona sin cancelar a los demás.
- El suscriptor que lanzó **sigue suscripto** (como en Flutter) y cada falla se registra.
- La emisión inicial de `Subscribe(…, emitCurrent: true)` también está protegida: la suscripción queda hecha y se devuelve (si no, quien se suscribe no podría darla de baja).
- La semántica anidada no cambia: un `Value =` dentro de un callback que después lanza notifica igual que antes.
- El texto del error se arma solo en el `catch`: emitir sigue sin reservar memoria (`EmittingDoesNotAllocate` sigue en 0 B).

**Logger obligatorio, sin sumidero estático.** QA ofrecía un *hook* estático o un `IAppLogger`. Se eligió el logger **por constructor y obligatorio** porque ARCHITECTURE §4.1 no admite estáticos fuera de `BuildFlags` y `StravaCredentials`, y porque un valor por defecto que descarte haría desaparecer el error en silencio. Quien crea un observable (servicio, *store*, `LanguageState`) ya recibe el logger del *composition root*. Cambios de API en §3.

**Tests** (`[Category("QA-V-01")]`): `ObservableValueTests.ThrowingSubscriberDoesNotStopTheOthers` (el escenario de QA, con el texto exacto del log), `ThrowingSubscriberStaysSubscribedAndEveryFailureIsReported`, `ThrowingSubscriberDuringANestedSetKeepsTheNestedSemantics`, `ThrowingInTheInitialEmissionStillSubscribes`, `LoggerIsRequired`; `EventStreamTests.ThrowingSubscriberDoesNotStopTheOthers`, `ThrowingObserverDoesNotStopTheOthers`, `LoggerIsRequired`; `LanguageStateTests.BrokenViewDoesNotStopTheOthers`.

**Documentado en** ADR 004 (decisión, alternativas y tabla de cambios) y ARCHITECTURE §5.

### 1.2 QA-V-02 — Credenciales: cualquier extensión y antes del commit

**`.gitignore`.** `**/StravaSecrets*.cs` y su `.meta` pasan a `**/[Ss]trava[Ss]ecrets*` (cualquier extensión; el `.meta` empieza igual). También `**/[Ss]trava_[Ss]ecrets*` y `**/strava.local.json*` en cualquier carpeta. Las clases de corchetes cubren las mayúsculas en Linux (en Windows git ya ignora mayúsculas con `core.ignorecase`). Se agregó `/tools/unity/.artifacts/` (QA-V-05).

**`RepoHygieneTests`.**

- `NoSecretsWouldBeAdded` (nuevo): lista `git ls-files --others --exclude-standard`, o sea lo que está **en disco**, sin versionar y sin ignorar, que `git add -A` subiría. Un `StravaSecrets.json` nuevo se detecta en la PC antes del commit y del push.
- `ThirdPartyOnlyMetaTracked` mira lo versionado **y** lo que se agregaría.
- `DetectsSecretFilesOnDiskBeforeTheyAreCommitted` (nuevo) prueba que el control funciona de verdad. Arma un repositorio git temporal fuera del proyecto (así Unity no importa nada) con seis secretos y dos archivos normales. Sin `.gitignore`, los seis aparecen como "se agregarían". Con `git add -f`, uno aparece como versionado. Con el `.gitignore` del proyecto, quedan ignorados y `StravaCredentials.cs` no.
- `GitignoreProtectsSecretsAndThirdParty` suma 14 casos: `StravaSecrets.json`/`.asset`/`.txt` y sus `.meta`, minúsculas, `strava_secrets.dart`, `strava.local.json` fuera de `tools/strava/`, y los vecinos que **sí** se versionan (`Bootstrap/StravaCredentials.cs`, `Core/Strava/StravaConfig.cs`, `tools/strava/write-secrets.ps1`) para que el patrón no sea demasiado amplio.
- `SecretNamesAreRecognized` (7 casos) fija la regla por nombre.

Los casos de QA (`Assets/RowMate/Config/StravaSecrets.json`, `Assets/RowMate/StravaSecrets.asset`, `tools/strava/StravaSecrets.txt`) dan ahora "ignorado" con `git check-ignore --no-index`. Con `-Ci` (`ROWMATE_CI=1`) pasan los 49 tests de `RepoHygieneTests` (43) y `SimulatorInUnityTests` (6).

### 1.3 QA-V-03 — El *bootstrapper* reintenta en el próximo open

`[InitializeOnLoad]` corre el constructor estático en cada recarga de dominio, o sea en cada recompilación. Ahora (`Editor/Bootstrap/ProjectBootstrapper.cs`):

- El constructor sale enseguida si `SessionState.GetBool("RowMate.Bootstrap.AttemptedThisSession")` es verdadero. `SessionState` sobrevive a las recargas y se borra al cerrar Unity.
- Si no, agenda `RunOncePerSession` con `EditorApplication.delayCall`. Si el Editor está ocupado, se reagenda **sin marcar**. Recién cuando corre de verdad, marca la sesión y llama a `Run(false)`. Si hay una recarga en el medio, el constructor lo vuelve a agendar porque la marca todavía no está.
- Un paso fallido deja la versión sin aplicar: se reintenta en el **próximo open**. El menú `RowMate/Bootstrap/Reaplicar` sigue forzando una corrida (`Run(true)`).

Test estático `BootstrapperStaticTests.RetriesAFailedStepOnlyOnTheNextOpen`: el constructor sale si la sesión ya lo intentó y no corre pasos; `RunOncePerSession` mira primero si el Editor está ocupado, después marca y después corre; el menú fuerza. Coincide con AC-BASE-10 de la spec de la It. 2 ("si uno falla, avisa y no se repite en cada recompilación").

### 1.4 QA-V-04 — Namespace `RowMate.EditorTools`

- Los 5 `.cs` de `Assets/RowMate/Editor/` y los 2 nuevos usan `namespace RowMate.EditorTools`. El asmdef conserva el **nombre** `RowMate.Editor` (las referencias y los documentos que hablan del assembly no cambian) y su `rootNamespace` pasa a `RowMate.EditorTools`.
- Un solo namespace para todo el assembly. Un subnamespace como `RowMate.EditorTools.Simulator` taparía a `RowMate.Simulator` adentro del assembly, por la misma regla de búsqueda de C#.
- Regla general en ARCHITECTURE §17 y dos tests: `AsmdefLintTests.NoNamespaceShadowsUnityNames` (ningún namespace de `Assets/RowMate` tiene un segmento `Editor`, `UnityEditor` o `UnityEngine`; revisa más de 100 declaraciones) y `EditorAssemblyUsesTheEditorToolsNamespace`. `EachFolderHasItsAsmdefAndAssemblyInfo` admite esa única excepción al "rootNamespace = nombre".
- ARCHITECTURE §19 pasa a `-executeMethod RowMate.EditorTools.BuildCli.Build`.

### 1.5 QA-V-05 — Los tests del simulador dentro de Unity, sin activarlo en release

**El problema de fondo.** `RowMate.Simulator` y `RowMate.Tests.Simulator` tienen `defineConstraints: ["ROWMATE_SIMULATOR"]`, así que en Unity solo existen con el define. AC-BASE-08 pide que las mismas pruebas corran en Unity, y R-01 pide que el define nunca llegue a un build de distribución.

**El procedimiento**, automatizable: `tools/unity/run-unity-tests.ps1 -Simulator` (manual completo en [tools/unity/README.md](../../../../tools/unity/README.md)).

1. `Unity -batchmode -quit -executeMethod RowMate.EditorTools.SimulatorDefine.EnableForTests`: guarda en `Library/RowMate/simulator-tests.txt` (no versionado) en qué plataformas ya estaba el define y lo agrega en Standalone, Android e iOS.
2. `Unity -batchmode -runTests -testPlatform EditMode -testResults … -logFile …`, sin `-quit`.
3. **Siempre**, en `finally`: `-executeMethod RowMate.EditorTools.SimulatorDefine.RestoreAfterTests`, que deja cada plataforma como estaba y borra la marca. Después el script comprueba en `ProjectSettings.asset` que el define quedó igual que antes (si no, sale con 11).

También tiene `-RestoreOnly` para recuperar una corrida cortada, `-Filter`, `-Category`, `-Platform` y `-Unity`. No arranca si otro Editor tiene el proyecto abierto (`Temp/UnityLockfile` tomado → código 10, sin tocar nada). Al final resume el XML y lista los tests rojos. Es ASCII y compatible con Windows PowerShell 5.1 (lo controla `ScriptsAreAsciiForWindowsPowerShell51`). Usa `Process.Start` + `WaitForExit` porque `Unity.exe` es una app de ventana y `&` no la espera.

**Por qué el define no llega a un release** (cuatro barreras):

| Barrera | Dónde |
|---|---|
| El script restaura siempre y verifica | `run-unity-tests.ps1` (`finally` + `Test-SimulatorInSettings`) |
| Mientras exista la marca, **cualquier** build falla; fuera de una corrida, falla todo build con el define que no sea *Development* (AC-BLE-13-01 de la It. 2) | `Editor/Simulator/SimulatorBuildGuard.cs` (`IPreprocessBuildWithReport`, `BuildFailedException`) |
| Un define olvidado en `ProjectSettings/*.asset` o en un *Build Profile* versionado hace fallar el CI (en la PC queda *Inconclusive* con el aviso, porque el modo simulador del Editor es legítimo mientras se desarrolla; durante la corrida de tests, con la marca, también) | `SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator` |
| `SimulatorDefine` es el **único** código que llama a `SetScriptingDefineSymbols`; el *bootstrapper* nunca lo toca | `SimulatorInUnityTests.OnlySimulatorDefineChangesScriptingDefines`, `BootstrapperStaticTests.NeverChangesScriptingDefines` |

**Desde la ventana del Editor**: menú `RowMate/Modo simulador` (con tilde, deshabilitado durante una corrida de tests), Test Runner y otra vez el menú. El menú y la guardia ya estaban definidos en ARCHITECTURE §4.2 y ADR 003; se implementaron ahora porque el procedimiento los necesita (§5.3).

**Tests estáticos** (`SimulatorInUnityTests`, `[Category("AC-BASE-08")]` y `QA-V-05`): los métodos que el script pasa a `-executeMethod` existen como `public static void` de `RowMate.EditorTools.SimulatorDefine`; el script activa, corre en `try` y restaura en `finally` (también si no pudo activar); la marca está en `Library/` y la restauración vuelve al estado guardado; la guardia frena durante los tests y en *release* con el define; la higiene de `ProjectSettings`. **La corrida real en Unity queda NO VERIFICABLE en este paso** (§4).

### 1.6 QA-V-06 — Lo que ve el usuario

`CatalogParityTests.UnchangedLegacyTextsLookIdenticalOnScreen` (es y en, `[Category("AC-APP-10-02")]` y `QA-V-06`), al lado de la comparación de patrones, que sigue igual:

- **151 claves sin parámetros**: `Localizer.Get(clave)` tiene que ser igual al texto del `.arb`. Flutter lo muestra tal cual: gen-l10n sin `use-escaping`, y sin parámetros no hay sintaxis ICU que interpretar.
- **4 claves con parámetros sin cambios** (`hrmConnectingTo`, `hrmConnectError`, `routineDeleteContent`, `editorStepsCount`, todas simples): `Localizer.Format` con valores de prueba tiene que dar el texto del `.arb` con cada `{parámetro}` reemplazado, como la interpolación `'Conectando a $name...'` que genera gen-l10n. Los valores de texto llevan apóstrofo, llaves y `#` (`Polar H10 'A' {x} #0`) para comprobar que se insertan literales. Los números, `1234`.

Si mañana el formateador cambia lo visible (escape con apóstrofos, `#` fuera de plural), este test falla aunque el catálogo no cambie.

### 1.7 QA-V-07 — `[DEMO]` una sola vez

Con `verbosity=detailed`, el logger de consola repetía la salida de cada test en "Standard Output Messages" después del eco en vivo del adaptador de NUnit. `-Demo` ahora deja `verbosity=normal` y pasa `-- NUnit.ConsoleOut=1`, que es el eco en vivo, explícito para no depender del valor por defecto. Los *runsettings* de `-Demo` y `-Matrix` se juntan en un solo `--`. Corrida real: **22 líneas `[DEMO]`, 22 distintas** (antes, 44). Test `CiWorkflowTests.DemoPrintsEachLineOnce`; README de `tools/dotnet` actualizado.

### 1.8 QA-V-08 — ADR 011 y 012

- **ADR 011**: la decisión describe el algoritmo de D-06 (`basicLocaleListResolution` de Flutter 3.47.6 con `[en, es]`), no "el primero soportado". Una coincidencia perfecta gana en el acto. Una coincidencia solo por idioma gana enseguida si es la **primera** preferencia (salvo que la segunda sea del mismo idioma). Más adelante, se guarda y gana solo si la siguiente no coincide perfecto. Los ejemplos salen de las 17 filas de `LocaleResolverTests`: `[es-AR, en]` → `es`, `[fr-FR, es-AR, en]` → `en`, `[fr-FR, en-GB, es]` → `es`.
- **ADR 012 §4**: los vectores van a `Assets/RowMate/Tests/Golden/oracle/` por medio de `run-oracle.ps1`, con su `package_config` propio y la guardia del legacy (D-01, QA-14).
- **De paso, ADR 012 §3**: tenía otras tres filas que contradecían su propia tabla. `toString()` pasa al algoritmo de QA-04 (`BignumDtoa` SHORTEST), las fechas al `IsoDate` de D-11 y la cultura a la regla de "Implementación". Esa regla dice que Core no depende de la cultura y que `AppRoot` igual fija `InvariantCulture`, como dice ARCHITECTURE §4.1.
- **De paso, LEGACY_MAPPING** (`main.dart`) decía "el primero soportado": alineado con D-06.

---

## 2. Corrida real

Todo con `--artifacts-path …/scratchpad/it2-debt`.

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath …/scratchpad/it2-debt
  Build succeeded.  0 Warning(s)  0 Error(s)                (9 proyectos, Release, incluye UnityApiCheck contra NUnit 3.5.0)
  RowMate.Tests.Simulator.dll  Total tests: 69   Passed: 69
  RowMate.Tests.EditMode.dll   Total tests: 887  Passed: 869  Skipped: 18
  exit=0

run-tests.ps1 -Matrix …   → matriz: PASA 157, FALLA 0, IGNORADO 1, SIN TEST 0 (de 158 IDs; el ignorado es AC-APP-09-07, igual que en la It. 1)
run-tests.ps1 -Demo …     → 22 líneas "[DEMO]" al comienzo de línea, 22 distintas (antes 44); exit=0
run-tests.ps1 -Ci -Filter "FullyQualifiedName~RepoHygiene|FullyQualifiedName~SimulatorInUnity" …  → Total 49, Passed 49; exit=0
run-unity-tests.ps1 -Simulator -Unity C:/no/existe/Unity.exe   → "no existe …", exit=12 (sin abrir Unity; parser de PowerShell: 0 errores)
```

| | It. 1 | Ahora | Diferencia |
|---|---|---|---|
| Generales (EditMode) | 843 (825 + 18 ign.) | 887 (869 + 18 ign.) | +44 |
| Simulador | 69 | 69 | 0 |
| **Total** | **912** | **956** | **+44** |

Las 44 nuevas: QA-V-01 (9), QA-V-02 (23: 14 casos de `.gitignore`, 7 de nombres, `NoSecretsWouldBeAdded` y la prueba en repositorio temporal), QA-V-03 (1), QA-V-04 (2), QA-V-05 (6), QA-V-06 (2), QA-V-07 (1). Ninguna se ignora.

---

## 3. Cambios de API (para quien construye la It. 2)

| Antes | Ahora |
|---|---|
| `new ObservableValue<T>(initial, comparer?)` | `new ObservableValue<T>(initial, IAppLogger logger, comparer?)` |
| `new EventStream<T>()` | `new EventStream<T>(IAppLogger logger)` |
| `new LanguageState(tags)` | `new LanguageState(tags, IAppLogger logger)`. El test de AC-APP-09-04 (`NoSetterNoPersistence`) ahora pide dos parámetros: la lista del sistema y el logger de diagnóstico. Sigue sin aceptar nada de preferencias guardadas |
| `namespace RowMate.Editor` | `namespace RowMate.EditorTools` (assembly `RowMate.Editor` sin cambios) |
| — | `RowMate.Core.Common.SubscriberErrors.Tag` = `"STATE"` |
| — | `RowMate.EditorTools.SimulatorDefine`: `Symbol`, `MenuPath`, `TestMarkerPath`, `IsEnabled(NamedBuildTarget)`, `IsEnabledAnywhere()`, `TestRunInProgress`, `EnableForTests()`, `RestoreAfterTests()`; menú `RowMate/Modo simulador` |
| — | `RowMate.EditorTools.SimulatorBuildGuard : IPreprocessBuildWithReport` (`callbackOrder` −1000) |

---

## 4. Para el primer open de la It. 2 (NO VERIFICABLE en este paso)

Unity 6000.3.27f1 está instalado, pero el coordinador pidió no abrirlo acá. Lo que tiene que comprobar el paso que lo abra:

1. **Compila** el código de Editor nuevo o tocado (`SimulatorDefine.cs`, `SimulatorBuildGuard.cs`, `ProjectBootstrapper.cs`, el namespace `RowMate.EditorTools`). El log no tiene `error CS`. APIs usadas, todas de Unity 6: `PlayerSettings.Get/SetScriptingDefineSymbols(NamedBuildTarget, string)`, `NamedBuildTarget.TargetName`/`FromBuildTargetGroup`, `Menu.SetChecked`, `MenuItem(path, isValidate, priority)`, `SessionState.GetBool/SetBool`, `IPreprocessBuildWithReport`, `BuildReport.summary.platformGroup`/`.options`, `BuildFailedException`.
2. **QA-V-03**: en el log del primer open aparece una sola vez el resumen `[BOOTSTRAP] versión 1 …`. Después de forzar una recompilación (tocar un `.cs`), no se repite.
3. **QA-V-05**: desde la raíz y con el Editor cerrado, `powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Simulator`. Se espera exit 0 y, en EditMode, **952 tests** (los 956 de dotnet menos los 4 `EmittingDoesNotAllocate`/medición de memoria que existen solo en `net8.0`, bajo `#if NET5_0_OR_GREATER`), 0 rojos y las 18 ignoradas, menos las que AC-BASE-08 de la It. 2 decida des-ignorar. Al terminar, `ProjectSettings/ProjectSettings.asset` no tiene `ROWMATE_SIMULATOR` y `Library/RowMate/simulator-tests.txt` no existe. Dentro de esa corrida, `CommittedSettingsDoNotEnableTheSimulator` sale *Inconclusive* a propósito (el define está puesto por la corrida).
4. **Guardia**: opcional, con el menú `RowMate/Modo simulador` activo, un build *Release* falla con el mensaje `[SIMULATOR] ROWMATE_SIMULATOR está activo en …`.
5. Si `-executeMethod … -quit` no llegara a guardar los defines en `ProjectSettings.asset` antes de salir (riesgo bajo: se llama a `AssetDatabase.SaveAssets()`), el script lo detecta en el paso 3 (código 11) y en la corrida de tests no aparecerían los 69 del simulador.

---

## 5. Decisiones y desvíos

1. **Logger obligatorio** (QA-V-01) en vez de opcional o estático. Rompe la firma de tres constructores (§3); el costo es bajo porque todavía no hay servicios ni *stores*. Se cambió una aserción del test de AC-APP-09-04 (dos parámetros en lugar de uno), sin cambiar lo que el AC pide.
2. **Solo se renombra el namespace** (QA-V-04), no el assembly ni la carpeta: `Editor/` con su asmdef es la carpeta estándar de Unity y el nombre `RowMate.Editor` aparece en ARCHITECTURE §3, ADR 002 y ANIMATION_BIBLE como nombre de assembly, que no da problemas.
3. **Menú `RowMate/Modo simulador` y `SimulatorBuildGuard` implementados ahora**, antes del diseño técnico de la It. 2. Ya estaban fijados en ARCHITECTURE §4.2 y ADR 003, y el procedimiento de QA-V-05 los necesita. El diseño de la It. 2 los toma como hechos. La forma de "abrir la app en modo simulador en el Editor" de AC-BLE-13-01 es ese menú. Lo que falta de AC-BLE-13-01 (`build.ps1 -Simulator`, que fuerza *Development*) es de la It. 2.
4. **Higiene de `ProjectSettings`**: en la PC, un define presente sin la marca da *Inconclusive* y no *Failed*. Si no, `run-tests.ps1` quedaría rojo cada vez que alguien desarrolla en modo simulador. En el CI (`ROWMATE_CI=1`) falla.
5. **Fuera de QA-V-08, alineados de paso**: ADR 012 §3 (tres filas), ADR 014 (decía `com.iguisoft.rowmate` "en Android y Windows", contra D-02) y una fila de LEGACY_MAPPING. No se tocaron `STATUS.md` (del PO), QA-V-09 (del Designer) ni los documentos cerrados de la It. 1. Por eso REPORT §3 de la It. 1 todavía dice que las líneas `[DEMO]` salen dos veces.
6. **Legacy**: al principio del paso, un `grep -r` sobre `rowmate-legacy/lib` para contar las etiquetas de log (`[BLE]`, `[HRM]`…) recorrió también la carpeta del archivo de credenciales de Strava. La salida fue solo el conteo de etiquetas con la forma `[MAYÚSCULAS]`, agregado de todos los archivos: no se mostró, copió ni guardó contenido de ese archivo. Las búsquedas siguientes lo excluyeron. El legacy no se modificó (`status --porcelain` vacío).
7. **Trabajo en paralelo**: mientras se hacía este paso, otros agentes agregaron fuentes e íconos en `Assets/RowMate/UI/`, `00-deuda-diseno.md`, `01-po-spec.md` y cambios en `design/`. No se tocaron. La corrida final de §2 incluye esos archivos y está en verde.

---

## 6. Archivos

**Código (Core):** `Assets/RowMate/Core/Common/ObservableValue.cs`, `EventStream.cs`, `SubscriberErrors.cs` (nuevo), `Core/Localization/LanguageState.cs`.

**Código (Editor, solo Unity):** `Assets/RowMate/Editor/AssemblyInfo.cs`, `RowMate.Editor.asmdef` (`rootNamespace`), `Bootstrap/{ProjectBootstrapper,PlayerSettingsStep,BootstrapState,UrpProvisioner}.cs` (namespace; QA-V-03 en `ProjectBootstrapper`), `Simulator/SimulatorDefine.cs` y `Simulator/SimulatorBuildGuard.cs` (nuevos).

**Tests:** `Tests/EditMode/Common/{ObservableValueTests,EventStreamTests}.cs`, `Localization/{LanguageStateTests,LocaleResolverTests,CatalogParityTests}.cs`, `Project/{RepoHygieneTests,AsmdefLintTests,BootstrapperStaticTests,CiWorkflowTests}.cs`, `Project/SimulatorInUnityTests.cs` (nuevo).

**Herramientas:** `.gitignore`, `tools/dotnet/run-tests.ps1`, `tools/dotnet/README.md`, `tools/unity/run-unity-tests.ps1` y `tools/unity/README.md` (nuevos).

**Documentos:** `architecture/ARCHITECTURE.md` (§2, §4.2, §5, §15.1, §16, §17, §19 y "Cambios de la It. 2"), `architecture/LEGACY_MAPPING.md`, ADR 003, 004, 011, 012, 013 y 014.
