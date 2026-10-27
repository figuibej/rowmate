# ADR 002: Capas, asmdefs y regla de dependencias

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** estructura de código de todas las iteraciones

## Contexto

No hay Unity Editor en la máquina de desarrollo y la paridad con el legacy se verifica con tests. Hace falta que la mayor parte posible del código compile y se pruebe con .NET 8 fuera de Unity, y que la capa dependiente de Unity sea delgada.

## Decisión

Assemblies (detalle en ARCHITECTURE §3):

| Assembly | Puro | Contiene |
|---|---|---|
| `RowMate.Core` | Sí (`noEngineReferences`) | Dominio, reglas, parsers, máquinas de estado de BLE, motor de entrenamiento, matemática de escena, TCX, l10n, puertos (interfaces) |
| `RowMate.App` | Sí | Stores por pantalla (puertos de los providers), `AppComposer`, `ISceneHost` |
| `RowMate.Infrastructure` | Sin API de Unity (por regla) | SQLite, preferencias JSON, clientes HTTP de Strava |
| `RowMate.Simulator` | Sí, con `ROWMATE_SIMULATOR` | Simulador físico y servicios simulados |
| `RowMate.Platform` / `Presentation` / `Scene` / `Simulator.Presentation` | No | Adaptadores nativos, UI Toolkit, URP |
| `RowMate.Bootstrap` | No | Composition root |
| `RowMate.Tests.Support` | Sí (`UNITY_INCLUDE_TESTS`) | Fakes y ayudas de test (`FakeScheduler`, `FakeBleAdapter`, `TestMainThreadContext`, `RepoPaths`, `GoldenVectors`); lo referencian los tres assemblies de test |
| `RowMate.Editor` | No | Herramientas de Editor y build |

Regla: las dependencias apuntan hacia Core. Presentation y Scene no se conocen entre sí ni conocen Platform/Infrastructure. Solo Bootstrap conoce todo. `autoReferenced: false` en todos.

## Alternativas descartadas

- **Un solo assembly**: no permitiría compilar lo puro con `dotnet` ni detectar dependencias indebidas.
- **Infrastructure con `noEngineReferences: true`**: el asmdef del paquete de SQLite expone tipos de Unity en algunas firmas y el compilador podría exigir `UnityEngine`; la regla "sin API de Unity" la hace cumplir el build de `dotnet`.

## Consecuencias

- El build de `dotnet` es la red de seguridad: si un archivo puro usa `UnityEngine`, falla.
- Más asmdefs que mantener, a cambio de compilación incremental más rápida en el Editor.
- El grafo de asmdefs se copia en `tools/dotnet` (un `.csproj` por asmdef, con las mismas referencias) y un test de *lint* compara los dos grafos (ARCHITECTURE §15.1).

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-03 | Assembly `RowMate.Tests.Support`, hermano de los de test; `.csproj` por asmdef y *lint* del grafo |
