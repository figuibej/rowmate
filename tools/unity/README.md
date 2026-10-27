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
- El *bootstrapper* nunca lo agrega ni lo saca; `SimulatorDefine` es el único que escribe los defines (lo controla un test de dotnet).

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
