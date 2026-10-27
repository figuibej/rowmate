# ADR 003: Composition root único y modo simulador por scripting define

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** arranque, simulador, builds (R-01, R-06)

## Contexto

El legacy elige en `main.dart` entre servicios reales y simulados con la constante de compilación `kSimulator` (`--dart-define=SIMULATOR=true`), y el tree-shaking saca el simulador de los builds normales. El simulador usa otra base (`rower_app_dev.db`) y bloquea toda subida a Strava. Nunca puede llegar a un build de distribución.

## Decisión

- Un único `AppRoot` (MonoBehaviour en `Boot.unity`) es el composition root. Construye todo y lo pasa por parámetro; no hay singletons ni service locator.
- `#if ROWMATE_SIMULATOR` en `CompositionRoot.cs` elige el backend de dispositivos y el nombre de la base. `BuildFlags.IsSimulator` es `const`.
- `RowMate.Simulator` y `RowMate.Simulator.Presentation` tienen `defineConstraints: ["ROWMATE_SIMULATOR"]`: sin el define no se compilan.
- Activación: menú de Editor `RowMate/Modo simulador` o `build.ps1 -Simulator` (`extraScriptingDefines`). `SimulatorBuildGuard` hace fallar todo build con el define que no sea `Development`. Ningún workflow de distribución lo pasa.
- La UI solo ve `IRowerService` / `IHeartRateService`.

## Alternativas descartadas

- **Interruptor en tiempo de ejecución** (ajuste oculto): el código del simulador quedaría en el binario de distribución; viola R-01.
- **Registro automático con `[RuntimeInitializeOnLoadMethod]`**: esconde la composición; el `#if` explícito es más claro y equivalente a `kSimulator ? … : …`.

## Consecuencias

- Paridad exacta con el aislamiento del legacy.
- Cambiar de modo en el Editor recompila (unos segundos).
- QA revisa en cada iteración con builds que el define no aparezca en los perfiles de distribución.
- Los tests del simulador (`RowMate.Tests.Simulator`) tampoco existen en Unity sin el define: se corren con `tools/unity/run-unity-tests.ps1 -Simulator`, que lo activa solo para esa corrida y lo restaura siempre (ADR 013).

## Cambios de la It. 2 (deuda de la It. 1)

| ID | Qué cambió |
|---|---|
| QA-V-05 | Implementados el menú `RowMate/Modo simulador` y `SimulatorBuildGuard` (`Assets/RowMate/Editor/Simulator/`, namespace `RowMate.EditorTools`), más `SimulatorDefine.EnableForTests`/`RestoreAfterTests` para la corrida de tests en Unity. `SimulatorDefine` es el único que llama a `SetScriptingDefineSymbols` (Standalone, Android e iOS). La guardia además hace fallar **cualquier** build mientras haya una corrida de tests sin restaurar (`Library/RowMate/simulator-tests.txt`). NO VERIFICABLE hasta el primer open de la It. 2 |
