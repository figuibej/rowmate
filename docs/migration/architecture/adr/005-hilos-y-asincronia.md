# ADR 005: Hilo principal, Task + IScheduler, Awaitable solo en Unity, sin UniTask

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** Core, Infrastructure, Platform

## Contexto

El legacy corre todo en el isolate de Dart con `Timer`, `Future.delayed` y `Stream`; sus tests usan `fake_async`. En Unity, BLE y red llegan desde hilos nativos, y el Core tiene que compilar como `netstandard2.1`.

## Decisión

- **Un hilo lógico**: Core, App, UI y escena corren en el hilo principal; sin locks.
- Los plugins nativos guardan cada evento con su marca de tiempo monótona en un *buffer* nativo que C# drena al principio de cada frame (ADR 009); lo que llega de hilos administrados (*loopback* de OAuth) va a `MainThreadQueue`.
- SQLite corre detrás de `IDbExecutor`: en producción un `SerialExecutor` (un hilo propio); la continuación del `await` vuelve al hilo principal por el `UnitySynchronizationContext`. Prohibido `ConfigureAwait(false)` en Core y App.
- Tiempo inyectable: `IClock` + `IScheduler` (`Schedule`, `SchedulePeriodic`, `Delay`, `IsCatchingUp`). `UnityScheduler` usa el reloj monótono nativo y avanza en `Update` (a lo sumo un disparo por frame en primer plano); al volver de una pausa larga hace **alcance** en tiempo virtual, intercalando timers y eventos guardados (ADR 017). `FakeScheduler.Advance(...)` en tests, con continuaciones en línea.
- En `dotnet` no hay `UnitySynchronizationContext`: `Tests/Support` trae `TestMainThreadContext` (contexto de un solo hilo bombeado por el test) e `InlineDbExecutor`, así las continuaciones nunca caen en el *thread pool* y los tests son deterministas.
- `Task` en lo puro; `Awaitable` de Unity 6 solo para esperar frames o volver al hilo principal en el pegamento Unity.
- Sin `async void` salvo handlers de UI (con captura y log); "dispara y olvidá" siempre por `Forget(logger)`.

## Alternativas descartadas

- **UniTask**: muy buena en Unity, pero no compila en el Core puro sin atarlo a Unity, y no hay caminos asíncronos por frame que justifiquen su ahorro de allocations.
- **`System.Threading.Timer`**: dispara en otro hilo y rompe el modelo de un solo hilo.

## Consecuencias

- Tests deterministas de timers, reintentos y watchdogs con los mismos tiempos del legacy.
- Segundo plano: la premisa anterior ("los timers se detienen como en Dart") era falsa; en Android e iOS Dart sigue corriendo. La sesión sigue según ADR 017 (keepalive nativo y alcance del reloj); se verifica en dispositivo en las It. 10a y 10b.

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-06 | Reloj monótono nativo, alcance al volver, `IsCatchingUp`; consecuencia de segundo plano corregida |
| QA-P-26 | `TestMainThreadContext` e `InlineDbExecutor` para tests deterministas en `dotnet`; `IDbExecutor` |
