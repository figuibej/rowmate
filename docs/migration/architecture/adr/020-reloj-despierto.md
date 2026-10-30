# ADR 020: El reloj de la app no cuenta el sueño del equipo (reloj despierto)

- **Estado:** Aceptado (2026-10-29), al implementar WP-A de la It. 4b (P-17 de la 4a, que el usuario aceptó en el gate de la 4a: el tiempo con el equipo suspendido no cuenta; DT4b-06 y DT4b-20 de [04b/02-tech-design](../../iterations/04b-hud-exr/02-tech-design.md) §2.9)
- **Ámbito:** Core.Common (`FrameClock`, `SystemWake`, `SuspendAccounting`, `FrameScheduler`), Core.Ports (`IAppLifecycle.Woke`), Core.Workout (`WorkoutEngine`), App (`WallClockTicker`); Platform (`UnityScheduler`, `AppLifecycle`, `SystemSleepSource`) y Bootstrap (`AppRoot`) en WP-B2 de la It. 4b; ADR 005 y 017

## Contexto

El legacy cuenta disparos de un `Timer.periodic(1 s)` ([rowmate-legacy](https://github.com/figuibej/rowmate-legacy) `lib/features/workout/workout_provider.dart:147`, `:247`) que no corre con el equipo dormido: para él, el lapso de una suspensión no existe. La app cuenta tiempo monótono (`ActiveClock`, ADR 017), y el `Stopwatch` de Windows puede seguir contando la suspensión o no: no está medido (QA-V4a-ROB-05). Sin una decisión, una PC que se suspende con una sesión en marcha y despierta 10 minutos después mostraría 10 minutos de remo que no pasaron, con sus puntos y sus lecturas de objetivos, o un `workoutSaveError` falso por el tope de 15 s del cierre (AC-WKT-02-11).

## Decisión

1. **El monótono del programador es el despierto.** Entre el monótono crudo (el `Stopwatch`) y `FrameScheduler` va `SuspendAccounting` (Core, puro: recibe todo por argumento), que le resta al crudo lo dormido que el crudo contó. Como todo lo que mide tiempo en la app usa ese reloj (el motor, el remo simulado, los topes de espera como `ForegroundTimeout`, el periódico del reloj de pared y el `MotionDriver`), **el lapso dormido no existe para la app**: no hay segundos, ni pasos, ni puntos, ni lecturas de objetivos, ni demora del cierre. La hora de pared (`UtcNow`, `LocalNow`) sigue siendo la real.
2. **Lo que recibe por cuadro** (`FrameClock`): el crudo, la hora UTC, el offset local, lo dormido según el sistema desde el arranque (NaN sin medida) y los contadores de avisos de "se suspende" y de "reanuda". El mismo `Awake` corre en cada cuadro (`FrameScheduler.Tick(in FrameClock)`) y en la vuelta del segundo plano (`ResumeFromBackground(in FrameClock)`), cuyo alcance llega **hasta el monótono despierto** (QA-G4bT-05 (a)).
3. **La regla**, con Δ = lo de este cuadro menos lo del anterior:
   - **con medida** (`ΔAsleep ≥ 0,5 s`): si el crudo avanzó al menos el 90 % de lo dormido, lo contó y se restan `min(ΔAsleep; ΔRaw)`; si no, no lo contó y no se resta nada;
   - **sin medida, con un "se suspende" pendiente**: todo cuadro de `ΔRaw ≥ 1 s` se descuenta (menos un cuadro nominal) y el aviso no se consume con ese cuadro, así un tirón entre el aviso y el sueño de verdad no deja contar el sueño (QA-G4bT-05 (b)); un salto de la hora de pared con un `ΔRaw` normal quiere decir que el crudo no contó; el primer cuadro con un "reanuda" nuevo cierra el pendiente, llegue antes o después del primer cuadro despierto; un aviso sin sueño ni "reanuda" en 30 s despiertos se descarta y se anota;
   - **sin medida ni aviso**: nada. Un cuadro tarde es un cuadro tarde (AC-WKT-02-04, la conducta de la 4a) y un salto de la hora de pared es un cambio de hora (AC-WKT-19-04).

   El monótono despierto nunca baja.
4. **El despertar se avisa.** Si hubo despertar, `FrameScheduler.Woke` sale después de correr lo vencido; `UnityScheduler` lo pasa a `AppLifecycle.SignalWake` y de ahí a `IAppLifecycle.Woke` (`IEventSource<SystemWake>`; por defecto nunca avisa). El motor publica una foto `Instant` (el primer cuadro ya muestra el presente, sin rodillos ni destellos de lo que no pasó) y anota `[WORKOUT] el equipo durmió N s: no cuenta (P-17; …)`; el reloj de pared publica la hora actual sin rodillo.
5. **Las señales por plataforma** (WP-B2):
   - **Windows, build**: los avisos de `PowerRegisterSuspendResumeNotification` (`PBT_APMSUSPEND`; `PBT_APMRESUMEAUTOMATIC` o `PBT_APMRESUMESUSPEND`), cuyo *callback* solo incrementa dos contadores, y la medida del núcleo `GetTickCount64 − QueryUnbiasedInterruptTime`, que sube solo mientras el equipo duerme;
   - **Windows, Editor**: solo la medida; el aviso no se registra, porque una recarga de dominio dejaría al sistema llamando a código descargado (DT4b-20);
   - **macOS**: la misma forma en la 10b;
   - **Android e iOS**: sin fuente (`NullSleepSource`): la pantalla bloqueada es segundo plano y la sesión sigue (Q-91, ADR 017); el reloj del teléfono con el sueño profundo es RA-19, de la 10a.

## Alternativas descartadas

- **Enseñarle a cada parte qué es una suspensión** (el motor, el remo simulado, cada tope): repartiría la regla y dejaría huecos (el reloj de pared, el motion). Un solo reloj despierto la deja en un lugar, probada en `dotnet`.
- **Usar solo la hora de pared**: no distingue una suspensión de un cambio de hora a mano o de zona (AC-WKT-19-04).
- **Tratar todo cuadro largo como suspensión**: rompería la conducta de la 4a con un cuadro tarde de verdad (AC-WKT-02-04), que procesa los segundos vencidos.

## Consecuencias

- `Tick(double, DateTimeOffset, TimeSpan)` y `ResumeFromBackground(double, …)` siguen y son las de `FrameClock` sin medida ni avisos: las pruebas de la 4a no cambian.
- **Si no llega ninguna señal**, el lapso cuenta como un cuadro tarde (la 4a) y se anota `[TIME] cuadro de N s sin aviso de suspensión: cuenta (AC-WKT-02-04)` desde 30 s. Con el modo de espera moderno (S0) el núcleo puede no contarlo como sueño: quedan los avisos (R4b-01).
- Las pruebas de `dotnet` lo cubren con `SuspendAccountingTests` y con `LifecycleHarness.Suspend` / `Wake` en los dos casos del monótono (`WorkoutSuspendTests`: 25 puntos en vez de 145). La suspensión real del equipo la verifica el usuario con el build (demo (c) de la 4b; `[Ignore]` en `WorkoutDeviceChecksTests`).
