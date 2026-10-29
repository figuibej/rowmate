# ADR 017: La sesión sigue en segundo plano (keepalive nativo, eventos con hora y alcance del reloj)

- **Estado:** Aceptado (2026-10-27), por el gate de QA del plan (QA-P-06) y la decisión del PO Q-91 (paridad: la sesión sigue)
- **Ámbito:** Core.Ble, Core.Workout, Platform (scheduler, plugins nativos), ADR 005 y 009; It. 4a (diseño y tests), 10a y 10b (dispositivo)

## Contexto

La arquitectura anterior suponía que Unity pausado en segundo plano equivalía a los timers de Dart suspendidos. Es falso: en Android el isolate de Dart sigue corriendo y en iOS con `bluetooth-central` corre en cada notificación BLE. En el legacy el keepalive `0x07` cada 2 s (`ble_service.dart:278-284`) y el reloj de 1 s (`workout_provider.dart:147`) siguen en segundo plano. En Unity el *player loop* se detiene en Android e iOS: sin cambios, el monitor saldría del modo Bluetooth y el reloj activo se frenaría.

## Decisión

1. **Keepalive**: lo decide Core (`RowerService`). En primer plano, con `IScheduler` (testeable). Con `IAppLifecycle.Paused`, `IBlePeripheral.StartNativeKeepAlive(...)` delega la escritura al plugin (Android: `Handler`; iOS: temporizador y, además, escritura al recibir una notificación si pasaron ≥ 2 s; Windows y macOS no lo necesitan porque el loop sigue con `runInBackground`). Con `Resumed`, `StopNativeKeepAlive()`.
2. **Eventos con hora**: los plugins guardan todo evento con la marca del reloj monótono nativo en un *buffer* circular (6 h a 2 Hz por dispositivo) y C# lo drena por frame (modelo *pull*, ADR 009).
3. **Alcance (*catch-up*)**: al volver, `UnityScheduler` avanza en tiempo virtual desde la pausa hasta ahora, intercalando timers vencidos y eventos guardados en orden; `IsCatchingUp` evita escrituras al hardware en el pasado.
4. **Reloj por tiempo monótono**: `WorkoutEngine` procesa todos los segundos enteros vencidos desde el inicio del tramo activo; en primer plano es 1 por segundo (como el legacy) y al volver procesa los que faltan con los datos de cada instante.
5. **Guardado** inmediato de lo pendiente al pausar (Q-12).
6. **Escritorio**: `runInBackground = true`; minimizado, 10 fps de *loop* sin dibujar la escena.

## Alternativas descartadas

- **Mantener Unity corriendo en segundo plano en Android** (no llamar a `pause()` del *player*): no está soportado y la superficie gráfica se destruye.
- **Mover toda la lógica de sesión al nativo**: triplicaría el motor y lo sacaría de los tests de `dotnet`.
- **Servicio en primer plano de Android desde el inicio**: es un agregado visible (notificación) que el legacy no tiene; queda como propuesta si la medición de la It. 10a muestra que el sistema mata el proceso.

## Consecuencias

- Telemetría, parciales y watchdogs dan lo mismo que si la app hubiera seguido en primer plano, porque se reproducen con su hora.
- Riesgo residual (RA-08): si Android mata el proceso en segundo plano, se pierde lo posterior a la pausa.
- La fuente del reloj monótono en Android (con o sin sueño profundo) se elige midiendo el legacy en el mismo teléfono (RA-19).
- Tests de `dotnet` en la It. 4a con `FakeScheduler` y `FakeBleAdapter.EnqueueAt`; verificación en dispositivo en las It. 10a y 10b.

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-06 | ADR nuevo: corrige la premisa y define keepalive nativo, eventos con hora, alcance y reloj monótono |

## Cambios de la It. 4a

Detalle en [04a-motor-hud-funcional/02-tech-design](../../iterations/04a-motor-hud-funcional/02-tech-design.md) §2.2, §4 y "Cambios por gate QA".

- **El programador pasa a Core** (decisión 3): `FrameScheduler` (Core/Common, puro) tiene la cola, los periódicos sin reanclar durante el alcance, la hora virtual de cada disparo, `IsCatchingUp` y la hora del cuadro; el mismo código corre en dotnet. `UnityScheduler` (Platform/Time) queda como adaptador: le pasa el `Stopwatch`, la hora UTC y el offset local en cada cuadro, y expone `EnterBackground` y `ResumeFromBackground`. Volver sin haberse ido no hace nada (Unity llama `OnApplicationPause(false)` al arrancar). Los timers de tiempo real de la base (`RealtimeTimerQueue.CatchUp`) y el tope que no cuenta el segundo plano (`ForegroundTimeout`) corren en el mismo alcance (QA-G4aT-03).
- **Ciclo de vida**: `IAppLifecycle` queda en `InBackground`, `HasFocus` e `IsUserFacing` (en lugar de `Paused`, `Resumed` y `FocusChanged`). `AppRoot` lo mueve con `Platform/Lifecycle/AppLifecycle` en este orden: al irse, `SetBackground(true)` y `EnterBackground()`; al volver, el alcance completo con `IsCatchingUp = true` y recién después `SetBackground(false)` (QA-G4aT-07). Sin el usuario delante (segundo plano, ventana minimizada o sin foco) los avisos del motor se retienen y al volver se muestra una vez solo el último (AC-WKT-02-06, -08, -09).
- **Una sola foto al volver** (decisión 4): durante el alcance el motor procesa cada segundo con sus datos pero no publica fotos intermedias; al terminar publica una con `Instant`, que la vista aplica sin animar (sin rodillos ni empujón; los pulsos con origen en el primer cuadro real). Pruebas: `WBG.*`, `FSC.*`, `WEL.OneSnapshotPerReturn` (dotnet), `USC.*` (Unity EditMode), `WBU.*` (PlayMode).
- **Pantalla encendida** (decisión 6 y ARCHITECTURE §13): `ScreenKeepAwake` (Core/Common) la pide al arrancar y otra vez al volver o al recuperar el foco; `WindowsWakeLock` pide y libera en el mismo hilo (un `Disable` desde otro hilo no llama al sistema y queda como Warning: `WLK.ReleaseRunsOnTheRequestingThread`).
- **Sin cambio en el alcance de la 4a**: el keepalive nativo y el *buffer* de eventos con hora siguen siendo de la It. 10a (Android) y 10b (iOS, macOS); en Windows el loop sigue con `runInBackground`.
