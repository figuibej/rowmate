# tools/unity — tests dentro de Unity

Corre las fuentes de test de `Assets/RowMate/Tests/` en el **Unity Test Framework** (NUnit propio de Unity), en *batchmode* y sin abrir la ventana del Editor. Es la otra mitad de AC-BASE-08: las mismas pruebas que `tools/dotnet/run-tests.ps1` corre con .NET 8, ahora dentro de Unity (ADR 013, ARCHITECTURE §15.1). Resuelve QA-V-05 de la It. 1.

## El problema que resuelve

`RowMate.Simulator` y `RowMate.Tests.Simulator` tienen `defineConstraints: ["ROWMATE_SIMULATOR"]`. Sin ese define Unity **no los compila**, así que los 69 tests del simulador (LT-01 a LT-07, AC-BLE-14-01 a -10) no aparecen en el Test Runner. Y el define no puede quedar puesto, porque ningún build de distribución puede llevar el simulador (R-01).

## El comando

Desde la raíz del repositorio, **con el Editor cerrado** (Unity admite una sola instancia por proyecto):

```
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Simulator
```

Con `-Simulator` hace tres corridas de Unity, una detrás de otra:

1. `Unity -batchmode -quit -executeMethod RowMate.EditorTools.SimulatorDefine.EnableForTests`: guarda en `Library/RowMate/simulator-tests.txt` en qué plataformas ya estaba `ROWMATE_SIMULATOR` y lo agrega en Standalone, Android e iOS.
2. `Unity -batchmode -runTests -testPlatform EditMode -testResults …`: con el define, compila los assemblies del simulador y corre **todas** las fuentes de test (las de `Tests/EditMode` y las de `Tests/Simulator`).
3. **Siempre**, aunque el paso 2 falle o se corte (`finally`): `Unity -batchmode -quit -executeMethod RowMate.EditorTools.SimulatorDefine.RestoreAfterTests`, que deja cada plataforma como estaba y borra la marca. Después el script comprueba en `ProjectSettings/ProjectSettings.asset` que el define quedó igual que antes del paso 1.

Al final imprime el resumen del XML (total, pasan, fallan, ignorados, inconclusos) y los primeros tests rojos, y sale con el código de Unity: `0` todo verde, `2` hay tests rojos o inconclusos, otro valor si Unity no pudo correr (por ejemplo, errores de compilación: el script muestra las líneas `error CS…` del log).

## Parámetros

| Parámetro | Qué hace |
|---|---|
| `-Platform EditMode\|PlayMode` | Por defecto `EditMode` (todas las pruebas de la It. 1 son de EditMode) |
| `-Simulator` | Activa `ROWMATE_SIMULATOR` solo para la corrida (pasos 1 a 3). Sin él, una sola corrida sin los tests del simulador |
| `-Filter "<regex>"` | Pasa `-testFilter` a Unity (nombre completo del test) |
| `-Category "<cats>"` | Pasa `-testCategory` a Unity (por ejemplo `AC-BLE-14-05` o `LT-03`) |
| `-Unity <ruta>` | `Unity.exe` a usar. Si no se da: la variable `ROWMATE_UNITY` o `C:\Program Files\Unity\Hub\Editor\<versión de ProjectVersion.txt>\Editor\Unity.exe` |
| `-ArtifactsPath <carpeta>` | Resultados (`<plataforma>[-simulator]-results.xml`) y logs. Por defecto `tools/unity/.artifacts/`, ignorado por git |
| `-RestoreOnly` | Solo el paso 3. Sirve si una corrida se cortó (se cerró la terminal, se apagó la PC) |

Códigos propios del script: `10` el proyecto está abierto en otro Editor (`Temp/UnityLockfile` tomado; no toca nada), `11` el define no quedó como antes (correr `-RestoreOnly`), `12` no encuentra `Unity.exe`.

## Por qué el define no llega a un release

- El script lo restaura siempre (paso 3) y verifica el resultado.
- Mientras exista la marca `Library/RowMate/simulator-tests.txt`, `SimulatorBuildGuard` hace fallar **cualquier** build. Fuera de una corrida de tests, falla todo build con el define que no sea *Development* (ARCHITECTURE §4.2).
- `Library/` no se versiona, y si el define quedara en un `ProjectSettings/*.asset` versionado, el CI falla (`SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator`).
- El *bootstrapper* nunca lo agrega ni lo saca; `SimulatorDefine` es el único que escribe los defines en el Editor (lo controla un test de dotnet; en el CI, `ci-simulator-define.sh` hace lo mismo en el checkout efímero del runner).
- Última barrera (QA-V2-11 de la It. 2): `Simulator/SimulatorReleaseGuard.cs` tiene un `#error` que hace que `RowMate.Simulator` no compile en un player sin *Development*, aunque el define llegue por otro camino (un *Build Profile*, `extraScriptingDefines` a mano). Lo prueba `SimulatorModeTests.SimulatorDoesNotCompileInANonDevelopmentPlayer`.

## Desde la ventana del Editor

1. Menú **RowMate → Modo simulador** (queda con tilde): Unity recompila con el define.
2. **Window → General → Test Runner → EditMode → Run All**: aparecen también los tests de `RowMate.Tests.Simulator`.
3. Otra vez **RowMate → Modo simulador** para sacarlo. Antes de commitear, el menú tiene que quedar sin tilde.

## Estado

Verificado en el primer open real de la It. 2 con Unity 6000.3.27f1 ([00-primer-open.md](../../docs/migration/iterations/02-dispositivo-simulador/00-primer-open.md)). El código de Editor del procedimiento lo sigue revisando `SimulatorInUnityTests` en dotnet.

- Sin `-Simulator`: 888 tests en EditMode, 872 pasan, 0 fallan, 16 ignorados; Unity sale con 0.
- Con `-Simulator`: 957 tests (los 888 y los 69 del simulador), 941 pasan, 0 fallan, 16 ignorados, 0 inconclusos. Las tres corridas salen con 0 y al final `ProjectSettings/ProjectSettings.asset` no tiene `ROWMATE_SIMULATOR` y la marca no existe. Tarda unos 45 s con la caché de `Library/`.

El Unity Test Framework sale con `2` también si hay un test **Inconclusive**. Por eso el paso 2 corre con `ROWMATE_SIMULATOR_TEST_RUN=1` (solo esa corrida). Si `SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator` ve la marca y la variable, pasa. Si ve la marca sin la variable, la corrida quedó cortada y sale Inconclusive (correr `-RestoreOnly`).

Diferencias esperadas con `tools/dotnet/run-tests.ps1` (961 tests: 892 generales y 69 del simulador): 4 tests existen solo en `net8.0` (`#if NET5_0_OR_GREATER`: medición de memoria y parser de .NET 8), y `ManifestAndVersionTests.ProjectOpensWithoutCompileErrors` corre solo dentro de Unity (en dotnet queda ignorado).

## En el CI de Unity (QA-V2-14 de la It. 2)

`.github/workflows/ci-unity.yml` corre las cuatro combinaciones (EditMode y PlayMode, sin y con simulador) con GameCI, a pedido (`workflow_dispatch`) y solo con la licencia de Unity en los secretos. GameCI no deja correr `-executeMethod` antes de los tests, así que el paso 1 lo hace `tools/unity/ci-simulator-define.sh enable` sin abrir Unity, en el checkout del runner: agrega `ROWMATE_SIMULATOR` a `scriptingDefineSymbols` de `ProjectSettings/ProjectSettings.asset` y deja la misma marca `Library/RowMate/simulator-tests.txt`. Como GameCI tampoco pasa variables de entorno al contenedor, la corrida avisa con el argumento `-rowmateSimulatorTestRun` (`customParameters`), que `SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator` acepta igual que `ROWMATE_SIMULATOR_TEST_RUN=1`. Al final, siempre, `ci-simulator-define.sh restore` deja `ProjectSettings.asset` como en el commit y borra la marca antes de que `actions/cache` guarde `Library/` (cada combinación tiene su propia caché). El workflow no compila players.

## Assets que las corridas no vuelven a escribir (QA-V2-07 de la It. 2)

Los 9 `FontAsset` de `Assets/RowMate/UI/Fonts/Generated` e `UI/Icons/Generated` (atlas dinámicos) no se guardan nunca fuera del bootstrapper, y los assets de `Assets/RowMate/Settings/URP` no se guardan en batchmode (tests, capturas, builds). Lo hace `Editor/Bootstrap/GeneratedAssetGuard` (un `AssetModificationProcessor`): en el log aparece `[ASSETS] no se guarda …` una vez por archivo. Si después de una corrida `git status` muestra cambios en esos archivos, algo los escribió por otro camino: es un error. En la ventana del Editor los assets de URP se pueden ajustar y guardar como siempre.

## It. 3: carpeta de datos, capturas por conjunto y CAP-27

- **Ninguna corrida abre las bases del usuario** (AC-HIS-20-04). `run-unity-tests.ps1` y `capture-snapshots.ps1` ponen `ROWMATE_DATA_DIR` en una carpeta temporal propia (`<temp>/RowMate.Tests/unity-<id>` o `capture-<id>`), que Unity hereda y que se borra en `finally`. En el Editor y en los builds de desarrollo, `Bootstrap/DataLocation` usa esa carpeta en lugar de `LocalLow/iguisoft/RowMate`; `BootSceneTests` y `SimulatorBootSceneProbe` además ponen la suya, así también se puede correr desde la ventana del Test Runner. Las pruebas de UI y el arnés usan bases de prueba (`TempDatabase`, con su guardia).
- **Capturas por conjunto**: `capture-snapshots.ps1 [-Set it3|it2]`. `it3` (por defecto) es el catálogo de Rutinas (`Snapshots/RoutinesCatalog.cs`: 42 IDs, 47 archivos) y escribe en `docs/migration/iterations/03-rutinas/snapshots`; `it2`, el de Dispositivo, en `02-dispositivo-simulador/snapshots`. Cada conjunto tiene su `manifest.json` e `INDEX.md`, sin rutas de la PC (`ItThreeToolingTests.NoMachinePathsInScriptsOrManifests`).
- **CAP-27** (la ventana del build normal, con su barra de título) **no es del arnés**: `capture-cap27.ps1 -ReplaceUserDatabase`, solo a pedido. Sin el switch no toca nada; si Windows no está en modo Oscuro sale con 3 (NO VERIFICABLE). Se niega con RowMate o un Editor de Unity abiertos, o con la base en uso. El Editor se reconoce por la ruta `…\Editor\Unity.exe` o por `Temp/UnityLockfile` tomado; el `unity.exe serve` de la Hub no cuenta. Siembra la base sintética como `cap27.db` y la copia como `rower_app.db`. Resguarda y verifica antes de reemplazar, toca la pestaña Rutinas (`0.625,0.98` de la ventana) y restaura y verifica en `finally` (códigos en el encabezado del script; 03-rutinas/02-tech-design §5.5). `capture-window.ps1` solo hace clic y captura si la ventana del build es la que está adelante; si otra la tapa, sale con 3 sin clic ni captura.
- **Ensayo de CAP-27**: `capture-cap27.ps1 -Rehearsal` corre de verdad la semilla, el resguardo, el reemplazo y la restauración sobre una carpeta temporal con archivos de prueba. También corre la llamada de la toma real a `capture-window.ps1` (`Invoke-WindowCapture`, la misma línea), pero con un ejecutable de prueba sin ventana en lugar del build, y espera `el build no abrio ninguna ventana` (1). No abre el build, no toca el mouse ni la pantalla y no nombra la carpeta del usuario. Los procesos que frenarían la toma real solo se informan; este proyecto abierto en un Editor sí lo frena, porque la semilla necesita Unity. Sirve para probar la herramienta antes de la toma real (QA-V3-01, QA-V3-11).
- **`capture-window.ps1`, arranque (QA-V3-11)**: sin `-Arguments` no pasa `-ArgumentList`, porque `Start-Process` de Windows PowerShell 5.1 no acepta `''`. Espera la ventana hasta `WaitSeconds` + 20 s o hasta que el proceso termine: un build que se cierra sin ventana sale con 1 enseguida. Códigos: 0 bien; 1 no apareció la ventana (o el build se cerró antes de la captura); 2 no existe el ejecutable; 3 otra ventana la tapa; 4 error inesperado, con el mensaje en la consola (el build que se haya abierto se cierra igual). `ItThreeToolingTests.WindowCaptureStartsTheExeForReal` corre el arranque de verdad, en dotnet y en Unity sobre Windows, con y sin `-Arguments`.
- **`new-windowless-exe.ps1 -Out <archivo>`**: compila con Windows PowerShell 5.1 un ejecutable de prueba sin ventana (subsistema gráfico y `Main` vacío: termina enseguida sin mostrar nada). Lo usan el ensayo y la prueba, siempre en una carpeta temporal que después se borra. No entra al repo ni a ningún build.
- **Humo de la base en un player**: `tools/build/build.ps1 -DbSmoke` (build de desarrollo y `-rowmate-db-smoke` sobre una carpeta temporal). Para la ida y vuelta entre dos procesos: `RowMate.exe -rowmate-db-roundtrip` dos veces con `ROWMATE_DATA_DIR` en una carpeta temporal (la primera guarda "Ida y vuelta", la segunda la encuentra).
