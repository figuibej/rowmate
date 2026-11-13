# Ronda 16 — Tests intermitentes de asignaciones (diseño técnico)

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-13 |
| Ronda | 16 · `16-tests-intermitentes`, fuera del plan de paridad: que la CI de Unity no salga roja sin motivo antes de cargar los secretos |
| Issues | #78 (resto: AC-D15-39) y #99 (`BleAllocationTests` en la corrida completa) |
| Alcance | Solo el arnés de pruebas (`Tests/UiSupport`) y una prueba nueva. **La app no cambia**: no asignaba |
| Fuera | `SceneBudgetTests(High)` y `HudBudgetTests.SeriesWorstCaseWork` (fallan solo con carga externa documentada; no se tocaron) |

Rutas relativas a `Assets/RowMate/`.

## 1. Causa

**La app no asignaba. La medida contaba asignaciones de otro hilo.**

`GcAllocations.During` y `FrameAllocations` contaban las muestras `GC.Alloc` con el `Recorder` de `UnityEngine.Profiling`, filtrado con `FilterToCurrentThread()`. Es el mismo patrón que usa `AllocatingGCMemoryConstraint` del Unity Test Framework. Ese filtro no es exacto: si otro hilo asigna justo cuando la medida se prende o se apaga, el `Recorder` a veces se lleva **una** muestra de ese hilo.

Se reprodujo de forma determinista con una sonda en PlayMode, en el Editor 6000.3.27f1. El hilo principal hace medidas vacías de 200 µs, sin asignar, mientras otros hilos asignan:

| Otro hilo | `Recorder` + `FilterToCurrentThread` (antes) | `ProfilerRecorder` + `CollectOnlyOnCurrentThread` (ahora) |
|---|---|---|
| Ninguno | 0 de más en 1000 medidas | 0 |
| Un `Thread` que asigna sin parar | **177 de más en 177 de 1000** | 0 |
| 4 del `ThreadPool` que asignan sin parar | **29 en 28 de 1000** | 0 |
| Objetos con finalizador que asigna (el hilo de los finalizadores) | **1 en 500** | 0 |
| Un `Thread` que asigna cada 50 µs | 0 en 1000 | 0 |
| Con 1 asignación propia por medida (control positivo) | 1 por medida (+ las ajenas) | **exactamente 1 por medida** |

El ajeno suma 1 por medida y no cientos, aunque el otro hilo asigne miles de veces en 200 µs. Eso indica una carrera al prender o apagar el filtro, no un filtro apagado. No depende de que haya un GC en la medida: 0 de las medidas con muestras de más tuvieron un GC.

Esto explica lo que se vio en las It. 6b, 11, 12, 14 y 15. Las 12 fallas guardadas de 10 corridas en `<scratch>` (`*/playmode*-results.xml`) muestran siempre **1 o 2** asignaciones. Caen en el tramo que más tiempo ocupa en cada cuadro: el dibujo que se repinta (`device-sonar`, `device-link-indicator`), el paso o el reloj de prueba. Fallaron las 5 medidas de `BleAllocationTests`, no una en particular. Nunca cayeron en un elemento que no se dibujaba.

**Por qué solo en la corrida completa.** `BleAllocationTests` son las **4 primeras** pruebas de la corrida de PlayMode (orden alfabético). Por eso no influye el estado que deje otra prueba de RowMate. Lo que cambia es el proceso: justo después de entrar en Play con el árbol de ~800 pruebas, hay hilos que asignan en segundo plano (el de los finalizadores y el `ThreadPool` del Editor y del corredor). Con `-Filter` ese trabajo es mucho menor y la carrera casi nunca cae en una medida. No se pudo poner nombre al hilo con el Profiler: la grabación con pilas (`AllocationProfile`) no muestra muestras de otros hilos en esas ventanas, porque esos hilos de Mono no están registrados en el Profiler. Sin embargo, el `Recorder` se las lleva igual, como muestra la sonda con el hilo de los finalizadores.

La primera causa del #78 (el `Settle` que llegaba al intento de reconexión) ya estaba corregida en la It. 15 y no cambia.

## 2. Arreglo (arnés, con control positivo)

| Archivo | Cambio |
|---|---|
| C `Tests/UiSupport/GcAllocations.cs` | `During` cuenta con **`GcAllocCounter`** (nuevo, en el mismo archivo). Es un `ProfilerRecorder` del marcador `GC.Alloc` con `CollectOnlyOnCurrentThread`, creado en el hilo que mide, con capacidad para 4096 muestras (si se pasa, `Saturated`; todos los topes de las pruebas son 0 o unas decenas). Usa un contador por medida en curso y por hilo (`[ThreadStatic]`), así una medida adentro de otra no se pisa. Arma su contador antes de `Begin`, así que nada de eso entra en la cuenta |
| C `Tests/UiSupport/FrameAllocations.cs` | Cada tramo usa `GcAllocCounter`: los dibujos, el panel, la escena y URP comparten uno, y el paso y el reloj usan otro (`MeasureAction`), para que un dibujo dentro del paso no lo corte. `Stop` libera los dos. El `Recorder` de antes queda prendido al lado **solo como testigo**: `Foreign` es lo que habría contado de más. No entra en `Total` ni en ninguna afirmación, y sale en el `ToString` ("ajenas que el Recorder de antes habría contado: N") |
| C `Tests/PlayMode/FrameAllocationsTests.cs` | + `OtherThreadsAllocatingAreNotCounted` (ver abajo) |

Lo que **no** se hizo: no se subió ninguna tolerancia, no se agregaron reintentos, `Ignore` ni `Explicit`, y nada sale de la CI. Los topes de `BleAllocationTests` siguen iguales (0 en dibujos, pasos y reloj, y ≤ 3 cuadros a 1 Hz con el reloj del motor).

**Control positivo y negativo:** `FrameAllocationsTests.OtherThreadsAllocatingAreNotCounted`, con un hilo que asigna sin parar durante toda la prueba:

- 1000 medidas vacías de 200 µs con `GcAllocations.During`: **0** (afirmado). En el registro queda lo que da en las mismas medidas el `Recorder` de antes. Es un comportamiento de Unity, así que se registra pero no se afirma: dio **119** en la primera corrida.
- 300 medidas con un `byte[64]` propio: **exactamente 300**. 300 con `mesh.vertices` (una asignación que hace una API de Unity en este hilo): **300 de 300**.
- `FrameAllocations` en cuadros reales: 60 cuadros sin asignar dan **0**, con 10 ajenas que el `Recorder` habría contado. 60 cuadros con un `byte[64]` por dibujo y otro por paso dan **60 en 60 dibujos y 60 en los pasos, exacto**, con 7 ajenas que el `Recorder` habría contado.
- `ThePositiveControlIsSeenInRealFrames` (It. 5) sigue igual: 30 en 30 dibujos y 30 en 30 pasos. El testigo contó 1 ajena en su ventana limpia: el `Recorder` de antes también habría dado rojo ahí.

Con el código de antes, esta prueba falla: el `Recorder` de antes es exactamente `LegacyRecorderDuring`, que da 119 ≠ 0. Los demás controles positivos de la casa también pasan con el contador nuevo (`HudBudgetTests` con su `control-positivo`, `SeriesMotionTests`) en las corridas completas de §3.

## 3. Corridas

Artefactos en `<scratch>/it16/`. "Carga" es la de los otros procesos, medida con `-LogLoad` (antes / media / máximo). No se tocó ningún proceso ajeno.

| Corrida | Resultado | Carga | Testigo: medidas de `BleAllocationTests` que el `Recorder` de antes habría dado en rojo |
|---|---|---|---|
| Línea de base, arnés **viejo**, PlayMode con `ROWMATE_ALLOC_PROFILE` | 796: 795 pasan; falla `SceneBudgetTests(High)` (fuera de alcance). `BleAllocationTests` 4/4 (esta vez no cayó la carrera) | 73 / 26,9 / 96 % | — |
| Sonda (`-Filter`, sin la VM ocupada) | Tabla de §1 | — | — |
| `-Filter "FrameAllocationsTests|BleAllocationTests|ChartAllocationTests"` con el arreglo | 7/7 | — | 0 |
| PlayMode 1 | **797/797** (con `SceneBudgetTests(High)`) | 28 / 12,8 / 76 % | 0 |
| PlayMode `-Simulator` 1 | **878/878** | 7 / 7,4 / 32 % | **1** (caja del enlace: `TheReconnectBannerDoesNotAllocatePerFrame`) |
| PlayMode 2 | 797: **796 pasan**. Falla `HudBudgetTests.SeriesWorstCaseWork` (mediana 1,056 ms ≤ 1,0), con carga externa (media 34 %, máx. 82 %): fuera de alcance, el tope no se tocó | 10 / 34 / 82 % | **1** (sonar). Además, 3 en `[HPF] cursor` |
| PlayMode `-Simulator` 2 | **878/878** | 17 / 16,1 / 40 % | **2** (aviso del HUD y caja del enlace) |
| PlayMode 3 | **797/797** | 12 / 13,9 / 23 % | 0 |
| PlayMode `-Simulator` 3 | **878/878** | 12 / 17,1 / 34 % | **1** (caja del enlace) |
| EditMode | 4325: 4269 pasan, **1 falla**, 55 ignorados. Falla `IdentityStepTests.ApplyIsIdempotent`, por el entorno (§4) | 22 / 13,4 / 20 % | — |
| EditMode `-Simulator` | 4519: 4463 pasan, **1 falla**, 55 ignorados. Falla `ProjectSettingsIt14Tests.AndroidPlatformIcons`, por el entorno (§4) | 14 / 13,9 / 19 % | — |
| dotnet (`tools/dotnet/run-tests.ps1`), con `ProjectSettings.asset` como en el commit | **194/194 y 4182: 4143 pasan, 0 fallan**, 39 omitidos. Mientras Unity tenía el archivo modificado (§4), `AndroidPlatformIcons` falló: espera 6 y había 10 | — | — |
| EditMode, otra vez al final | Lo mismo: 1 falla, `IdentityStepTests.ApplyIsIdempotent`, por el entorno (§4). El archivo se revirtió | 55 / 26 / 77 % | — |

**Lectura.** `BleAllocationTests` pasó en las **6 corridas completas** (3 normales y 3 con `-Simulator`) y `FrameAllocationsTests` 2/2 en las 6. En **4 de esas 6** corridas, el `Recorder` de antes se habría llevado una muestra ajena en alguna medida de `BleAllocationTests`: con el arnés viejo, esas 4 corridas habrían salido rojas. Con el contador nuevo, esas mismas ventanas dieron 0. En `FrameAllocationsTests.OtherThreadsAllocatingAreNotCounted`, el `Recorder` de antes contó 104 a 132 ajenas en las 1000 medidas vacías de cada corrida; el contador nuevo contó 0 en las 6.

Las 2 fallas que quedan en PlayMode están fuera de esta ronda y tienen la carga externa documentada (`SceneBudgetTests(High)` en la línea de base y `SeriesWorstCaseWork` en la corrida 2). Los topes no se tocaron.

## 4. Entorno de esta ronda

- **Pantalla:** el monitor estaba en su modo normal, no en el de reserva. No se midió la GPU: esta ronda no tiene métricas de cuadro.
- **Unity Hub:** no estaba abierto, y la primera corrida salió con 198. Se abrió la Hub (es una app empaquetada, con `shell:AppsFolder`). Al arrancar, la Hub retomó una instalación que había quedado en cola de una sesión anterior ("Checking for persisted install-queued items from previous session") y apareció instalado el módulo **iOS Build Support** (`PlaybackEngines/iOSSupport`). Esta ronda no lo pidió y no lo desinstaló: lo decide el usuario.
- **Efecto del módulo iOS:** con el módulo, cada apertura de Unity agrega a `ProjectSettings/ProjectSettings.asset` los íconos por defecto de iPhone (97 líneas). Por eso la guardia de `run-unity-tests.ps1` salió con 13 en cada corrida. Se revirtió con `git checkout` después de cada una. Mientras el archivo está así, fallan `AndroidPlatformIcons` (que cuenta las entradas de íconos) en dotnet y en EditMode, y `IdentityStepTests.ApplyIsIdempotent` en EditMode (Unity reescribe el archivo). No tienen relación con este cambio: esta ronda no toca `ProjectSettings` ni esos assemblies.
- **Otro proceso en el proyecto:** al final de la ronda, a las 11:32 hora local, después de la última corrida de esta ronda, apareció un `Unity.exe -batchmode … BuildCli.Build -rowmateTarget iOS -rowmateSimulator` que esta ronda no lanzó, y volvió a modificar `ProjectSettings.asset`. No se tocó ni ese proceso ni el archivo mientras corría. El `RowMate.exe` del build del simulador que estaba abierto desde antes tampoco se tocó.

## Para issues

- `Tests/UnityEditMode/LocalOffsetSourceTests` mide con `AllocatingGCMemoryConstraint` del Unity Test Framework, que usa el mismo `Recorder` con `FilterToCurrentThread`, así que está expuesto a la misma carrera. No se lo vio fallar. Se puede pasar a `GcAllocCounter` (habría que sumar la referencia a `RowMate.Tests.UiSupport`, o mover el contador a un assembly de pruebas que ya tenga acceso al motor).
- Con el módulo iOS Build Support instalado, Unity agrega los íconos de iPhone a `ProjectSettings.asset` cada vez que se abre, y `AndroidPlatformIcons` e `IdentityStepTests.ApplyIsIdempotent` fallan en esta PC. Hay que decidir si se versionan esas entradas, si se ajusta la prueba para que cuente solo Android, o si se desinstala el módulo. La CI no se ve afectada: no tiene el módulo.
- `AllocationProfile` (It. 15) graba solo los hilos registrados en el Profiler, así que no ve las asignaciones de los hilos de Mono que causaban esto. Conviene anotarlo en su comentario si se vuelve a usar para buscar intermitentes.
