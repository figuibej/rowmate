# Ronda 16 — Tests intermitentes de asignaciones: reporte

**Estado:** cerrada. QA: **APROBADO CON OBSERVACIONES**. 8 PASS · 0 FAIL · 1 NO VERIFICABLE (R16-07, del entorno). Ronda corta, fuera del plan de paridad, pedida por el usuario para que la CI de Unity sea confiable.

## 1. Qué pasaba

`BleAllocationTests` (sonar, aviso de reconexión, pulsómetro conectado, aviso del HUD) salía rojo de vez en cuando en las corridas completas de PlayMode, siempre con 1 o 2 asignaciones (#99, y el resto de #78 / AC-D15-39).

## 2. La causa

La app **no** asignaba. El arnés contaba con `UnityEngine.Profiling.Recorder` + `FilterToCurrentThread`, el mismo patrón que `AllocatingGCMemoryConstraint`. Ese `Recorder` a veces se lleva una asignación de **otro hilo** si ese hilo asigna justo al prender o apagar la medida. Es una carrera, no un filtro apagado:

- Sonda determinista con otro hilo asignando: el `Recorder` de antes contó entre 85 y 177 asignaciones ajenas en 1000 medidas vacías; el contador nuevo, 0.
- Solo pasaba en la corrida completa porque `BleAllocationTests` son las primeras pruebas, cuando arrancan más hilos en segundo plano.

## 3. El arreglo (solo en el arnés de pruebas)

- `GcAllocCounter` (en `Tests/UiSupport/GcAllocations.cs`): `ProfilerRecorder` del marcador `GC.Alloc` con `CollectOnlyOnCurrentThread`. Lo usan `GcAllocations.During` y todos los tramos de `FrameAllocations`.
- El `Recorder` de antes queda solo como testigo (`Foreign`, en el `ToString`) y no entra en ninguna cuenta.
- Prueba nueva `FrameAllocationsTests.OtherThreadsAllocatingAreNotCounted`: con un hilo que asigna sin parar, las medidas vacías dan 0 y el control positivo da exacto (300/300 con `byte[64]` y con `mesh.vertices`; uno por dibujo y uno por paso en cuadros reales).
- No se subieron topes, ni hay reintentos, `Ignore` o exclusiones de la CI. La app no cambia.

## 4. Resultados

| Corrida | Architect | QA |
|---|---|---|
| PlayMode ×3 | 797/797, 796/797\*, 797/797 | 797/797 ×3 |
| PlayMode `-Simulator` ×3 | 878/878 ×3 | 878/878 ×3 |
| `BleAllocationTests` | 6/6 | 6/6 (48/48 casos sumando las 12 corridas) |
| dotnet `-Matrix` | 194/194 · 4143 pasan, 0 fallan | igual; matriz PASA 919, FALLA 0 |
| EditMode (±`-Simulator`) | 1 falla de entorno | 1 falla de entorno |

\* `HudBudgetTests.SeriesWorstCaseWork` (1,056 ms) con carga externa de hasta 82 %; fuera de alcance.

En 2 de las 6 corridas del QA y en 4 de las 6 del Architect, el testigo muestra que el arnés viejo habría dado rojo en `BleAllocationTests`; el contador nuevo dio 0 en esas mismas ventanas.

**Fallas de EditMode (entorno):** desde que esta PC tiene el módulo iOS Build Support, Unity agrega 97 líneas de íconos de iPhone a `ProjectSettings.asset`, y fallan `ApplyIsIdempotent` y `AndroidPlatformIcons`. La CI no tiene el módulo.

## 5. Issues

- Resuelve **#99** y el resto de **#78** (AC-D15-39).
- Deuda nueva: QA-V16-01 a 04 (liberar los contadores del pool, testigo compartido, `Dispose` en `finally`, íconos de iPhone en `ProjectSettings`), `LocalOffsetSourceTests` con el mismo patrón y la limitación de `AllocationProfile` con hilos de Mono.

Detalle: [02-tech-design.md](02-tech-design.md) · [05-qa-verification.md](05-qa-verification.md).
