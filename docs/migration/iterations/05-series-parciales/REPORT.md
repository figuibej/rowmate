# It. 5 — Series en vivo: parciales de 500 m y repeticiones: reporte de cierre

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-30 |
| Estado | **Cerrada, espera tu OK** |
| Veredicto del PO | **ACEPTADA.** En los pasos de trabajo de una rutina ya ves tus parciales de 500 m: la vuelta en curso y las 4 últimas, la más reciente arriba. Dentro de una serie ves en qué repetición vas, cuántos metros llevás y cómo te fue en las 3 anteriores. Todo va en dos paneles de vidrio debajo de SPM, cerrados por defecto, que se abren con un toque o con el teclado y **recuerdan su estado** aunque cierres la app. Los números son los del legacy, también con un monitor que manda saltos de distancia, y los paneles conviven con la cuenta grande, el sello, `PAUSA` y la llave sin encimarse. Cumplen 71 de 74 criterios y no falla ninguno. **Queda una verificación pendiente, no una falla**: nada de la It. 5 corrió todavía en el build de Windows, porque tu `RowMate.exe` estuvo abierto toda la iteración en `Builds/Windows/simulator` y no se pudo compilar ahí. Esa carpeta todavía tiene el build de la 4b, **sin los paneles**: para ver la demo, primero cerralo y generá el build (§3.1). La medida de fluidez en el build se hace al empezar la 6a, antes de construir encima |
| Veredicto de QA | **APROBADO CON OBSERVACIONES** en la ronda 2 ([05-qa-verification](05-qa-verification.md), "Ronda 2"): 0 problemas graves, 0 mayores y 1 menor (del Designer, no hace fallar ningún criterio). En la ronda 1 había fallado un criterio menor (la llave sobre un chip en L640 con el diálogo), y las dos lentes encontraron dos mayores: una fila de `Series anteriores` que podía mostrar el split de otra serie al volver del segundo plano, y en horizontal una columna desplazada que podía dejar un panel escondido. Los tres se corrigieron, cada uno con una prueba que antes fallaba, y QA los volvió a verificar junto con otros 9 menores. Las dos lentes, [paridad](05-qa-lens-paridad.md) y [motion](05-qa-lens-motion.md), también dieron APROBADO CON OBSERVACIONES |
| Documentos | [01-po-spec](01-po-spec.md) (qué se pidió, con "Al cerrar la iteración") · [02-tech-design](02-tech-design.md) (cómo se hizo, con los cambios de la implementación y de la ronda 1) · [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) (control previo) · [04-design-notes](04-design-notes.md) (aspecto y movimiento, con §15) · [05-qa-verification](05-qa-verification.md) (verificación en dos rondas), con sus lentes [05-qa-lens-paridad](05-qa-lens-paridad.md) y [05-qa-lens-motion](05-qa-lens-motion.md) · [00-deuda-4b](00-deuda-4b.md) y [00-deuda-diseno](00-deuda-diseno.md) (la deuda de la 4b y la base de diseño de los paneles) · [snapshots/INDEX.md](snapshots/INDEX.md) (capturas) |

---

## 1. En pocas palabras

La It. 5 suma a la pantalla de entrenamiento lo último que le faltaba del legacy: los parciales y las repeticiones.

- **Los parciales de 500 m** (`Parciales 500 m`).
  - En cada paso de trabajo, la **vuelta en curso**: su tiempo grande (`0:50.0`) y sus metros a la derecha (`200 m`).
  - Debajo, **las 4 últimas vueltas**, la más reciente arriba (`#5 1:40.0`, `#4 1:40.0`…).
  - El instante de cada 500 m se interpola entre dos lecturas, como el legacy: a 3 m/s, la vuelta da `2:46.7`.
  - Fuera de un trabajo dice `Sin parciales` / `en este paso`.
- **Las repeticiones de la serie.**
  - En una serie, al final del panel de parciales va la línea `Rep 2/3 · 200 m`: la repetición y los metros que llevás en ella, también en el descanso.
  - El panel `Series anteriores` muestra **las 3 repeticiones anteriores de la misma serie**, con su split de trabajo y sus metros (`Rep 1 2:05 680 m`). Otra serie o un paso suelto vacían la lista, y la última repetición nunca se compara, como en el legacy.
- **Los paneles.**
  - Cerrados, son dos chips discretos debajo de SPM: `500 m` en ámbar y `Reps` en celeste. `Reps` aparece solo dentro de una serie.
  - Un toque (o Tab y Enter) los abre o los cierra. **La app recuerda lo que elegiste** con las mismas claves del legacy, también después de cerrarla.
  - Abrir es crecer hacia abajo con un resorte suave, sin cambiar de ancho. Cada vuelta nueva entra arriba con una marca ámbar que se apaga. Solo rueda lo que cambia.
  - Nunca tapan las métricas, ni la cuenta grande, ni `PAUSA`. En horizontal nunca entran en la zona del bote; en vertical, abiertos, solo por su borde de afuera.
  - Nada se corta: `Rep 999/999 · 999.999 m` entra entero, achicado (CAP-25).
- **Los saltos del monitor no rompen nada.** Un paquete sin distancia, un reinicio del monitor, un corte de la conexión o lo remado en pausa no suman metros ni inventan vueltas. Los parciales y el paso por distancia cuentan **los mismos metros**, segundo a segundo (DV-26 de la 4a).
- **Tus preferencias, a salvo.** Se guardan en un archivo propio de la app, `prefs.json`, que comparten el build normal y el de simulador. Nunca se leen las del legacy. Si el archivo falta o se daña, los paneles arrancan cerrados, sin avisos.

Se comporta como el legacy:

- las reglas de las vueltas, la interpolación, el saneamiento de saltos, la pausa, las repeticiones, la comparación de hasta 3, la lista que se vacía y el segundo de atraso en cada cambio de paso (P-01);
- los **15 casos legacy** (LT-47 a LT-61: los 12 de `series_tracker_test` y los 3 que quedaban de `series_panels_test`), portados con su nombre y sus afirmaciones;
- **el oráculo**: el tracker del legacy corre desde su código y da **el mismo archivo**. La lente de paridad lo regeneró y comparó, además, 400 secuencias al azar con saltos, ceros y reinicios: **160 362 estados idénticos**, sin tolerancia. El motor sigue dando exactamente lo mismo que el del legacy.

Lo que se ve distinto del legacy, a propósito (§6.1, DV-48 a DV-51): los paneles muestran el primer paso desde el primer cuadro; nada se corta, se achica; tienen un ancho de 137 fijo para no tapar el bote ni la cuenta grande; y adentro, los textos secundarios son más chicos para que entre todo.

Además:

- **La deuda de la 4b quedó saldada al empezar** ([00-deuda-4b](00-deuda-4b.md)):
  - el **primer cuadro de cada entrada**: el HUD se arma en el negro y el arranque lo precalienta. En el Editor, el toque del primer libre bajó de 54 a 13,7 ms. En el build falta medirlo (§5.1);
  - **la barra libre en un teléfono en español**: `Entrenamiento Libre` ya no queda pegado al reloj;
  - las herramientas que se trababan cuando Unity revienta al cerrar, y el endurecimiento ante una animación o un diálogo que fallan.
- **La demo (c) de la 4b** no fue concluyente: el registro de la app no muestra ningún despertar. Con la app abierta Windows no se duerme solo, y bloquear la sesión o apagar la pantalla no suspende. No hay bug: esta vez se suspende a mano (§3.1 c).
- Hay **2733 pruebas en dotnet** (192 de las herramientas y 2541 del resto), contra 2501 al cerrar la 4b, y **495 pruebas de la interfaz montada** con el simulador, contra 420.
- Las **38 fotos y 9 videos** los genera Unity con la interfaz real y salen iguales byte a byte en dos corridas.
- Ni las pruebas ni las capturas tocaron tus bases, tus preferencias ni las del legacy.

Lo que no se pudo ver:

- **El build de Windows de la 5**: tu `RowMate.exe` estuvo abierto toda la iteración. Falta la medida de fluidez en el build y la prueba a mano en la ventana real (§5.1). Lo hace QA al empezar la 6a.
- **La suspensión real de tu equipo**: lo verificás vos (§3.1 c).
- **Android y IL2CPP**: faltan los módulos y el teléfono. Quedan sin ver el dedo sobre los chips y el arrastre de la columna.

---

## 2. Qué se entregó, por funcionalidad

"Cumple" quiere decir que pasa todo lo que se pudo verificar. "Con parte pendiente" quiere decir que cumple en el Editor, pero falta verlo en el build o con un equipo (§5.2).

| ID | Qué quedó listo | Criterios | Dónde se ve |
|---|---|---|---|
| F-WKT-07 | **Los parciales de 500 m**. Solo con rutina y solo en los pasos de trabajo; la lista se vacía en cada paso. Una lectura por segundo, la del segundo, antes de decidir si el paso terminó. El cruce interpolado (`2:46.7` a 3 m/s; `2:05.0` a 4 m/s). La vuelta en curso. Los saltos que no suman: el paquete sin distancia, el reinicio del monitor, el primer salto y el corte. Lo remado en pausa no cuenta. Segundo plano igual que primer plano; el equipo suspendido no suma tiempo. Solo en memoria, por sesión. El oráculo del legacy | 12 de 12 | El panel `Parciales 500 m` |
| F-WKT-08 | **Las repeticiones y la comparación**. "En serie" según la rutina expandida. Los metros de la repetición, que nunca bajan. El resumen de las 3 anteriores, la más reciente primero. La lista que se vacía con otra serie o un paso suelto. El split de trabajo (`—` sin metros de trabajo). La última repetición nunca se compara. Los casos de `Series 600`, `Pirámide 500` (`1.112 m`), `Prueba 4a` y `Corta` | 8 de 8 | La línea `Rep r/R · m` y el panel `Series anteriores` |
| F-WKT-18 | **Los paneles colapsables**. Los chips de 137 × 34; abiertos, el mismo ancho, con su cabecera y el título a una escala por idioma. Lo que muestra cada uno en cada paso, también el primer segundo (DV-48) y el segundo del cambio (P-01). El toque, el arrastre, el teclado y el anillo de foco. Las preferencias (`immersive.lapsExpanded`, `immersive.repsExpanded`), a prueba de un archivo que falta, se daña o no se puede guardar. El lugar en los cinco tamaños, con la columna desplazable en horizontal. La zona del bote, la cuenta grande, `PAUSA`, el sello y la llave. Nada se corta (`S-LARGO`). Abrir, cerrar, la vuelta nueva y los cambios por pieza, también con reducir movimiento. 56-57 elementos en el peor caso (tope 60) y 0 bytes por cuadro. En español y en inglés | 21 de 22 (5 con parte pendiente); 1 no verificable: la fluidez medida en el build | Debajo de SPM, con rutina |
| F-WKT-13 (deuda de la 4b) | **La entrada sin tirón**: el HUD se arma en el primer cuadro negro y el arranque lo precalienta. **Una falla de la presentación no traba la sesión**: una animación que falla sale sola y las demás siguen; si el diálogo de completado no se puede abrir, la sesión se guarda y la app vuelve a reposo | 1 de 2; 1 no verificable: la medida en el build | Al entrar al entrenamiento |
| F-WKT-16 (deuda de la 4b) | **La barra libre en un teléfono en español** (QA-V4b-R2-01): el modo compacto separa `Entrenamiento Libre` del reloj, sin achicar el título | 1 de 1 | CAP-13 de la 4b |
| F-APP-10 (también toca) | **9 claves nuevas** (223 por idioma), con el español idéntico al legacy: `500 m`, `Reps`, `Parciales 500 m`, `Series anteriores`, `Sin parciales en este paso`, `Sin repeticiones completas`, `Rep {rep}/{count} · {meters}`, `Rep {rep}` y `#{number}` | 1 de 1 | CAP-12 y CAP-13 en inglés |
| De iteraciones anteriores (28) | **13 que cambian**: la cascada y la salida con los paneles, la pausa y la cuenta grande junto a los paneles abiertos, la zona del bote, el teclado, reducir movimiento, la llave (con su única excepción) y tus datos, ahora también tus preferencias. **15 que se vuelven a verificar**: la pausa, el segundo plano, el equipo suspendido, el paso por distancia con el mismo saneamiento, los formatos, nada animando al salir, la presentación que no toca el dato, la rotación, los valores extremos, la letra del sistema y `Simular desconexión` | 27 de 28 (4 con parte pendiente); 1 no verificable: la suspensión real | — |

Además:

- **Los 15 casos legacy** (LT-47 a LT-61) pasan en dotnet y en Unity, con y sin simulador.
- **El oráculo de series**: 50 corridas del motor de la app le dan al tracker exactamente las entradas de cada guion, y el tracker da lo mismo que el del legacy con los 8 remos de prueba (con saltos, reinicios, cortes, extremos y parado), pausas y un paso de 0 s.
- **Herramientas para el equipo**:
  - el arnés de capturas `it5`, con 47 tomas, 27 sondas de aspecto y los valores de cada cuadro de los 9 videos;
  - en el build de simulador, `-rowmate-perf-routines` y `-rowmate-perf-series` para medir la fluidez con los paneles (solo en desarrollo);
  - `SnapshotCli.SeedDatabase -seed F5 -prefs laps,reps`, que siembra una base de prueba y su `prefs.json` en una carpeta temporal.
- **Documentos al día**:
  - el ADR [021](../../architecture/adr/021-preferencias.md) (las preferencias) y ARCHITECTURE §9.4 y §20.2;
  - ART_BIBLE y ANIMATION_BIBLE, del Designer;
  - al cerrar: el ROADMAP, el inventario `workout-engine-hud` y FEATURE_INVENTORY ([01-po-spec, "Al cerrar la iteración"](01-po-spec.md)).

---

## 3. Cómo ver la app

### 3.1 La demo en el build de Windows con simulador

**El build.** `Builds/Windows/simulator/RowMate.exe` ya tiene la It. 5: el coordinador lo generó al cierre, cuando se cerró el `RowMate.exe` que estuvo abierto durante la iteración. Si hace falta volver a generarlo, con el Editor de Unity **cerrado** y desde la carpeta del repositorio:

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator
```

**Antes de empezar:**

- **Un solo `RowMate.exe` a la vez.** Si abrís dos, escriben en la misma base y en el mismo archivo de preferencias, y gana el último que guarda.
- **El Firewall.** Si aparece el aviso "Seguridad de Windows", contestá **`Cancelar`**: la app no usa la red.
- **El idioma.** El build sigue la lista de idiomas de Windows, como el legacy. Si Windows está en inglés, el build sale en inglés: por eso el texto en inglés va entre paréntesis.
- **El simulador del build tiene "ruido"**: los valores se mueven un poco alrededor de lo que elegís en el panel. Por eso los números van con "≈" y van a ser parecidos a los de las capturas, no idénticos. A 150 W el simulador rema a ≈ 3,77 m/s: ≈ 4 m por segundo y ≈ `2:13` cada 500 m.

**(a) Parciales y repeticiones** (`Series 600`, 8 minutos)

Primero armá `Series 600` en **Rutinas** (`Routines`):

1. `+` → en `Nombre de la rutina` (`Routine name`), escribí `Series 600`.
2. `+ Paso` (`Step`) → `Calentamiento` (`Warmup`), `Por tiempo` (`By time`), `Seg` (`Sec`) `30` → `Confirmar` (`Confirm`).
3. `Serie` (`Series`): aparece un bloque ×3 con `Trabajo` 2m y `Descanso` 1m. Dejá el ×3.
4. Lápiz de `Trabajo` (`Work`) → `Por distancia` (`By distance`), `Metros` (`Meters`) `600` → `Confirmar`.
5. Lápiz de `Descanso` (`Rest`) → `Min` `0` y `Seg` `20` → `Confirmar`.
6. `+ Paso` → `Enfriamiento` (`Cooldown`), `Seg` `30` → `Confirmar`. Después, `Guardar` (`Save`).

Después, en **Workout**:

1. **La llave**: dejá `Watts` en `150` con `−` o `+`, y cerrá el panel con su `✕`. → DEMO-01
2. **`Series 600` → `Iniciar`** (`Start`). Debajo de SPM llega, junto con ella en la cascada de la entrada, el chip **`500 m`** en ámbar. Durante el calentamiento **no** hay chip `Reps`: todavía no hay serie. → DEMO-02
3. **Tocá `500 m`.** El panel crece hacia abajo con suavidad, sin cambiar de ancho, y dice `Parciales 500 m` (`500 m splits`) y `Sin parciales` / `en este paso` (`No splits` / `in this step`). → DEMO-03
4. **A los `00:31` empieza el trabajo.**
   - El panel muestra la vuelta en curso: `0:01.0` y ≈ `4 m`, `0:02.0` y ≈ `8 m`… El tiempo cambia una vez por segundo, junto con el reloj de la banda, y los metros quedan pegados a la derecha.
   - Debajo, `Rep 1/3 · …` en celeste, y abajo aparece el chip **`Reps`**. → DEMO-04
   - Tocá `Reps`: `Series anteriores` (`Previous reps`) con `Sin repeticiones` / `completas` (`No completed` / `reps`). → DEMO-05
5. **A los ≈ `02:43` se cierran los primeros 500 m.** La lista baja, arriba entra **`#1`** con ≈ `2:12.6` y, a su izquierda, una marca ámbar que se apaga en poco más de un segundo. La vuelta en curso vuelve a `0:00.0`. → DEMO-06
6. **A los ≈ `03:10` termina el trabajo.** La barra pasa a `Descanso`. Durante ese segundo los paneles todavía muestran el trabajo (como el legacy, P-01); después, `Sin parciales en este paso`, y la línea `Rep 1/3 · …` sigue sumando los metros del descanso.
7. **En los últimos 3 s del descanso** (≈ `03:27` a `03:30`), la cuenta grande `3`, `2`, `1`, lejos de los paneles.
8. **A los ≈ `03:31` empieza la repetición 2.** En `Series anteriores` entra **`Rep 1`** con ≈ `2:13` y ≈ `676 m`, con una marca celeste, y la línea pasa a `Rep 2/3 · …`. En la línea solo ruedan las cifras que cambian. → DEMO-07
9. **La llave → `Simular desconexión`.** Durante ≈ 4 s, los metros de la vuelta en curso no avanzan y su tiempo sí. Después siguen desde donde estaban: nada salta, no aparece ninguna vuelta inventada y ningún número se pone negativo. El panel del simulador no tapa Split, Watts ni la barra. Cerralo con su `✕`. → DEMO-08, DEMO-09, VID-05
10. **`⏸ Pausa`.** `PAUSA` aparece en su disco, en su lugar de siempre, y los paneles quedan quietos con los valores del último segundo. Los podés abrir y cerrar también en pausa. **`Reanudar`**.
11. **`Terminar` → `Terminar`** (`Finish`). Los paneles se van con sus últimos valores, nunca en `0:00.0` ni vacíos, y vuelve Workout en reposo. → DEMO-10
12. **Lo recuerda.** Otra vez `Series 600` → `Iniciar`: `Parciales 500 m` ya está **abierto desde el primer cuadro**, y a los `00:31` `Series anteriores` aparece **abierto**, vacío: nada de la sesión anterior. → DEMO-11
13. **También después de cerrar la app.** `Terminar` → `Terminar`, **cerrá la app** y volvé a abrirla. `Series 600` → `Iniciar`: los dos siguen abiertos (`Series anteriores`, desde que empieza la serie).
14. **Y cerrados.** Tocá los dos paneles para cerrarlos, `Terminar` → `Terminar`, cerrá la app y volvé a abrirla. Al iniciar otra vez, los dos están **cerrados**: los chips. → DEMO-12

**(b) Con el teclado** (1 minuto). En la ventana, con una rutina en marcha y después de usar el mouse:

- Tab recorre `Terminar`, `Pausa`, `500 m`, `Reps` (si se ve) y la llave, y vuelve a `Terminar`. Cada uno muestra el anillo de foco; el de un panel lo sigue cuando cambia de tamaño. → CAP-26
- Enter o Espacio sobre `500 m` lo abre y lo cierra, y lo recuerda, como un toque.
- Si el foco está en `Reps` cuando termina la serie y el panel se va, el Tab siguiente vuelve a `Terminar`.

**(c) El equipo suspendido** (P-17, 5 minutos). **Lo verificás vos**: QA no suspende tu equipo. Hacelo con el build, no en el Editor: en el Editor la app no registra el aviso de suspensión de Windows.

1. `Entrenamiento libre`. Dejá correr el reloj de la banda hasta `01:00`.
2. **Suspendé Windows a mano**: Inicio → **Encender/apagar** → **Suspender**. No sirve bloquear la sesión (Win+L), apagar el monitor ni esperar a que se duerma solo: con la app abierta Windows no se duerme solo (la pantalla siempre encendida de la It. 4a). Esperá **1 minuto** y despertá la PC.
3. **Lo que tiene que pasar**:
   - el reloj de la banda sigue desde `01:00` (a lo sumo `01:01`), **no** desde `02:00`;
   - el chip sigue en `EN MARCHA` y la hora del HUD es la actual;
   - no aparece ningún aviso.
4. Dejá correr hasta `02:00` → `Terminar` → `Terminar`.
5. **Antes de volver a abrir la app** (al abrirla, el registro anterior pasa a `Player-prev.log`):
   - Abrí `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate\Player.log` con el Bloc de notas y buscá `durmió`.
   - Tienen que estar `[TIME] despertar: durmió 60.0 s según la medida del núcleo y 60.4 s según el aviso del sistema; …` y `[WORKOUT] el equipo durmió 60.0 s: no cuenta (P-17; aviso, medida)`, con más o menos segundos. La línea del aviso puede decir `reanuda (2)`: Windows manda dos avisos al despertar.
   - Si dice `sin aviso del sistema` o `sin medida del núcleo`, esa es la señal que faltó (R4b-01). Si no hay ninguna línea `despertar`, el equipo no llegó a suspenderse.
6. **Los puntos.** En una consola abierta en la carpeta del repositorio, corré con .NET 8:

   ```
   dotnet run --project tools/dotnet/RowMate.SessionList -- --variant simulator
   ```

   Si `dotnet` no está en el PATH, usá el de la carpeta de instalación de .NET 8, como en `tools/dotnet/README.md`. La sesión más nueva tiene **unos 25 puntos**: uno cada 5 s de los 2 minutos remados, más el final. Si el tiempo suspendido hubiera contado, tendría unos 37.

**Contanos** qué marcó el reloj al despertar y cuántos puntos tiene la sesión. Si querés, pegá también las líneas `[TIME] despertar …` y `[WORKOUT] …`, y solo esas, porque el resto del registro trae rutas de tu PC. Si el reloj saltó, AC-WKT-02-11 se reabre y se corrige al empezar la 6a.

Si querés probar más, en el mismo build:

- **Minimizar.** Con `Series 600` en el trabajo, minimizá la ventana más de 2 minutos y restaurala. Las vueltas y las repeticiones que se cerraron mientras tanto ya están, sin animar su entrada, y con sus valores de verdad.
- **En inglés.** Con Windows en inglés: `500 m splits`, `Previous reps`, `No splits in this step`, `No completed reps`. Los números llevan el mismo formato que en español (`1.112 m`, P-08). → CAP-12, CAP-13
- **Reducir movimiento.** Windows → Configuración → Accesibilidad → Efectos visuales → apagá **Efectos de animación**. La app lo toma cuando la ventana recupera el foco. Los paneles cambian de tamaño en el acto, con el contenido en un fundido, y la vuelta nueva aparece sin deslizarse, con su marca. → VID-08
- **El diálogo de completado.** Con `Series 600` hasta el final, detrás del diálogo los paneles muestran el enfriamiento (`Sin parciales en este paso`, sin repeticiones) y no responden.

**Tus datos.** El build de simulador guarda las rutinas y las sesiones en `rower_app_dev.db`, y el normal en `rower_app.db`, dentro de `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`. En esa misma carpeta, desde esta iteración, va `prefs.json`, con lo que elegís en los paneles: lo comparten los dos builds, como en el legacy. La app legacy guarda sus datos en otro lugar y la nueva no los lee ni los toca.

### 3.2 En Unity, con el simulador

Igual que en la 4b ([REPORT de la 4b §3.2](../04b-hud-exr/REPORT.md)). Unity admite una sola instancia por proyecto: antes de abrir el Editor, verificá que no esté corriendo ninguno de los scripts de §3.3 a §3.6.

1. **Abrí el proyecto** en Unity Hub, con el Editor **6000.3.27f1**.
2. **Prendé el modo simulador**: menú **RowMate → Modo simulador**.
3. **Abrí la escena** `Assets/RowMate/Scenes/Boot.unity`.
4. **Elegí el tamaño en la vista *Game***: `1280x720` (la ventana de Windows), `411 x 914` (teléfono vertical), `914 x 411` (teléfono horizontal) o `640 x 360` (ventana baja).
5. **Dale Play** y seguí los pasos de §3.1 (a) y (b). La parte (c) no vale en el Editor.
6. **Idioma, letra grande y "reducir movimiento"**: están en el menú **RowMate → Simular**.

Lo que más se luce en el Editor, en el **teléfono vertical** (`411 x 914`), con `Series 600` (2 minutos):

- en los últimos 3 s del primer descanso (≈ `03:27` a `03:30`), con los paneles cerrados, la cuenta grande `3`, `2`, `1` aparece en su lugar de la 4b, a ≈ 8 dp de los chips, sin tocarlos → CAP-19;
- en el descanso de la repetición 2, abrí `500 m` justo cuando aparece la cuenta grande (≈ `06:27`): el panel se abre a su lado, la cuenta grande **sigue en su lugar** y en ningún momento se enciman. La cuenta de la barra igual crece y se tiñe, y la píldora late → CAP-20, VID-09.

En el **teléfono horizontal** (`914 x 411`), lo mismo con los dos paneles abiertos: quedan a la izquierda, lejos de la cuenta grande y del bote. Ahí la columna se desplaza con la rueda o arrastrando; para ver `Series anteriores`, bajala → CAP-16, CAP-21.

**Antes de cerrar Unity, dejá `RowMate → Modo simulador` sin tilde.** Si queda puesto, se guarda en la configuración del proyecto, y el control del CI falla a propósito.

### 3.3 Los builds de Windows

Con el Editor **cerrado**, desde la carpeta del repositorio:

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator
Builds\Windows\simulator\RowMate.exe
```

Es el build de la demo, siempre de desarrollo ("Development Build") y siempre en `Builds/Windows/simulator`. Si tenés abierto un `RowMate.exe` de esa carpeta, cerralo antes: el build pisa sus archivos.

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows
Builds\Windows\normal\RowMate.exe
```

Es el build normal, sin cinta, llave ni marca de desarrollo. **El de `Builds/Windows/normal` que hay hoy es anterior a los arreglos de la ronda 1**: el Architect lo generó en WP-B y salió limpio en el inspector, sin rastros del simulador, de diagnósticos, de pruebas ni de los argumentos de medida. Para verlo con el código final, generalo otra vez con este comando.

El script termina con 0 si el build salió bien, con 2 si el build normal tiene restos del simulador, de diagnósticos o de pruebas, con 3 si falta un módulo (NO VERIFICABLE) y con 5 si se le pide una carpeta nueva para un build de desarrollo. Hoy, `-Backend IL2CPP` y Android terminan con 3.

### 3.4 Las pruebas sin Unity (.NET 8)

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1
```

Al final aparecen dos líneas `Passed!`. Las herramientas, con 192 que pasan, y el resto, con 2541 pruebas, de las que 2507 pasan y 34 quedan ignoradas. Las ignoradas piden un teléfono, una Mac, GitHub, la suspensión real del equipo o el build. Si ninguna línea dice `Failed!`, está todo verde.

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix
```

Escribe `tools\dotnet\.artifacts\TestResults\ac-matrix.md` con los 658 IDs: los 597 de las iteraciones anteriores (28 de ellos se vuelven a verificar en la 5), los 46 criterios nuevos y LT-47 a LT-61.

- Sin los resultados de Unity: 531 PASA, 125 PENDIENTE UNITY y 2 ignorados.
- Si antes corriste §3.5, agregá `-UnityResults tools/unity/.artifacts`: la matriz queda en **656 PASA**, 0 FALLA y 2 IGNORADO (AC-WKT-02-10 de Android y AC-APP-15-03 de Apple).

### 3.5 Las pruebas dentro de Unity

Con el Editor **cerrado** (si está abierto, el script sale con 10 y no toca nada):

```
powershell -NoProfile -File tools/unity/run-unity-tests.ps1
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Simulator
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode -Simulator
```

| Comando | Total | Pasan | Fallan | Ignoradas |
|---|---|---|---|---|
| EditMode | 2601 | 2563 | 0 | 38 |
| EditMode `-Simulator` | 2793 | 2755 | 0 | 38 |
| PlayMode | 426 | 426 | 0 | 0 |
| PlayMode `-Simulator` | 495 | 495 | 0 | 0 |

- Las PlayMode montan la app real con una base y unas preferencias de prueba. Cubren:
  - los paneles en los cinco tamaños: chips, cabecera, contenido, filas y textos vacíos;
  - el toque por la selección real, el arrastre, el teclado y el anillo de foco;
  - las preferencias con el archivo real en una carpeta temporal, también dos arranques seguidos de la app;
  - la columna, el desplazamiento y la zona del bote;
  - la cuenta grande, `PAUSA`, el sello y la llave;
  - `S-LARGO` y los valores extremos;
  - las animaciones, la pausa, la rotación, el segundo plano, los diálogos, la entrada y la salida;
  - las asignaciones de memoria por cuadro (0) y el presupuesto de elementos;
  - `Simular desconexión`, con `-Simulator`.
- **Si Unity revienta al cerrar** después de cambiar el modo simulador (`-1073741819`), el script ahora lo dice ("Unity revento al cerrar despues de cambiar el define, pero quedo como se pidio: se sigue") y sigue solo. En la ronda 2 de QA pasó 3 veces en 16 cambios, y las 3 siguió.

### 3.6 Las capturas

Están en [snapshots/](snapshots/). La tabla completa, con las sondas y la auditoría de cada toma, está en [snapshots/INDEX.md](snapshots/INDEX.md).

- Las renderiza Unity con la interfaz real, con una base de prueba, unas preferencias de prueba en memoria por toma (nunca tu `prefs.json`), el reloj de prueba con la zona en UTC−03:00 (el reloj de pared marca `10:0x`) y el simulador sin ruido. No son maquetas.
- Para regenerarlas, con el Editor cerrado:

  ```
  powershell -NoProfile -File tools/unity/capture-snapshots.ps1 -Set it5
  ```

- La cinta `SIMULADOR` tapa en parte el comienzo de la barra de arriba, como en el legacy.
- Las de la 4b con rutina se regeneraron **una sola vez**: cambian solo en el lugar de los chips (y CAP-09 en la llave). CAP-13 a CAP-15 de la 4b muestran la barra libre en el modo compacto.

**La demo, toma por toma** (ventana de 1280 × 720, simulador sin ruido a 150 W):

| DEMO-01 · Workout con `Series 600` | DEMO-02 · a los 10,2 s: el chip `500 m`, sin `Reps` |
|---|---|
| ![DEMO-01](snapshots/DEMO-01-workout.png) | ![DEMO-02](snapshots/DEMO-02-chip.png) |

| DEMO-03 · `Parciales 500 m` en el calentamiento | DEMO-04 · el trabajo: `0:30.0` `113 m`, `Rep 1/3 · 113 m` |
|---|---|
| ![DEMO-03](snapshots/DEMO-03-parciales.png) | ![DEMO-04](snapshots/DEMO-04-serie.png) |

| DEMO-05 · `Series anteriores` vacío | DEMO-06 · `#1 2:12.5` |
|---|---|
| ![DEMO-05](snapshots/DEMO-05-repeticiones.png) | ![DEMO-06](snapshots/DEMO-06-primera-vuelta.png) |

| DEMO-07 · la repetición 2: `Rep 1 2:13 675 m` | DEMO-08 · `Simular desconexión`, con el panel abierto |
|---|---|
| ![DEMO-07](snapshots/DEMO-07-repeticion.png) | ![DEMO-08](snapshots/DEMO-08-desconexion.png) |

| DEMO-09 · los metros siguen, sin salto | DEMO-11 · otra sesión: los dos abiertos desde que se ven, `0:01.0` `4 m` |
|---|---|
| ![DEMO-09](snapshots/DEMO-09-sigue.png) | ![DEMO-11](snapshots/DEMO-11-recordados.png) |

[DEMO-10](snapshots/DEMO-10-reposo.png) y [DEMO-12](snapshots/DEMO-12-cerrados.png) son la vuelta a reposo después de cada `Terminar`, sin diálogo ni aviso. Son idénticas byte a byte a DEMO-01.

**Los chips y los paneles** (teléfono vertical, `Series 600`):

| CAP-01 · los chips cerrados | CAP-02 · parciales abierto | CAP-03 · los dos abiertos, con el contorno de la zona del bote |
|---|---|---|
| ![CAP-01](snapshots/CAP-01-chips-tv.png) | ![CAP-02](snapshots/CAP-02-parciales-abierto.png) | ![CAP-03](snapshots/CAP-03-los-dos-abiertos.png) |

CAP-03 es la comparable con `docs/image-03.png` del [legacy](https://github.com/figuibej/rowmate-legacy): la misma lectura (título con ícono y flecha, el tiempo grande con los metros, `Rep r/R · m`; `Series anteriores` con `Rep 1`, el split y los metros en columnas), más angosta (DV-50) y con los metros a la derecha (DV-51). `Series anteriores` entra en la zona del bote solo por su franja de afuera, nunca en la columna del remero.

| CAP-04 · dos repeticiones anteriores | CAP-05 · en el descanso: los vacíos en dos renglones | CAP-06 · el segundo del cambio: la barra ya en `Descanso` |
|---|---|---|
| ![CAP-04](snapshots/CAP-04-dos-repeticiones.png) | ![CAP-05](snapshots/CAP-05-vacios-parejos.png) | ![CAP-06](snapshots/CAP-06-segundo-de-atraso.png) |

| CAP-07 · en el enfriamiento, fuera de la serie | CAP-08 · las 4 últimas vueltas (`#1` ya no se ve) | CAP-09 · la vuelta interpolada: `#1 2:46.7` |
|---|---|---|
| ![CAP-07](snapshots/CAP-07-fuera-de-la-serie.png) | ![CAP-08](snapshots/CAP-08-cuatro-vueltas.png) | ![CAP-09](snapshots/CAP-09-interpolada.png) |

| CAP-10 · el tope de 3 repeticiones | CAP-11 · el punto de miles: `Rep 1 2:05 1.112 m` | CAP-25 · `S-LARGO`: todo entero, achicado |
|---|---|---|
| ![CAP-10](snapshots/CAP-10-tres-repeticiones.png) | ![CAP-11](snapshots/CAP-11-escala-comun.png) | ![CAP-25](snapshots/CAP-25-s-largo.png) |

| CAP-12 · en inglés | CAP-13 · los vacíos en inglés | CAP-14 · en un teléfono de 360 |
|---|---|---|
| ![CAP-12](snapshots/CAP-12-en-ingles.png) | ![CAP-13](snapshots/CAP-13-vacios-en-ingles.png) | ![CAP-14](snapshots/CAP-14-p360.png) |

**Con la cuenta grande, `PAUSA` y el sello** (teléfono vertical):

| CAP-19 · la cuenta grande con los chips | CAP-20 · la cuenta grande con parciales abierto | CAP-22 · `PAUSA` con los dos abiertos | CAP-24 · el sello con los paneles |
|---|---|---|---|
| ![CAP-19](snapshots/CAP-19-cuenta-grande-con-chips.png) | ![CAP-20](snapshots/CAP-20-cuenta-grande-con-parciales.png) | ![CAP-22](snapshots/CAP-22-pausa.png) | ![CAP-24](snapshots/CAP-24-sello-y-columna.png) |

**En horizontal y en la ventana**: los chips en el teléfono horizontal (CAP-15), la columna con los dos abiertos, que se desplaza y se apaga en su borde de abajo (CAP-16 y CAP-17), la cuenta grande y `PAUSA` (CAP-21 y CAP-23), la ventana de 1280 × 720 (CAP-18, comparable con `docs/image-03.png` del legacy) y el anillo de foco (CAP-26):

![CAP-15](snapshots/CAP-15-th-chips.png)

![CAP-16](snapshots/CAP-16-th-mascara.png)

![CAP-21](snapshots/CAP-21-th-cuenta-grande.png)

![CAP-23](snapshots/CAP-23-th-pausa.png)

| CAP-17 · la ventana baja, 640 × 360 | CAP-18 · la ventana de 1280 × 720 | CAP-26 · el anillo de foco en `Parciales 500 m` |
|---|---|---|
| ![CAP-17](snapshots/CAP-17-l640-mascara.png) | ![CAP-18](snapshots/CAP-18-w1280.png) | ![CAP-26](snapshots/CAP-26-anillo-de-foco.png) |

**De la 4b, lo que cambió por esta iteración:**

| CAP-13 de la 4b · la barra libre en el modo compacto | CAP-10 de la 4b · la única excepción de la llave (§6.2) |
|---|---|
| ![CAP-13 de la 4b](../04b-hud-exr/snapshots/CAP-13-barra-libre-tv.png) | ![CAP-10 de la 4b](../04b-hud-exr/snapshots/CAP-10-dialogo-completado-l640-letra.png) |

**Videos** (30 cuadros por segundo; los valores de cada cuadro están en [manifest.json](snapshots/manifest.json)):

- [VID-01 · abrir y cerrar (3 s)](snapshots/VID-01-abrir-y-cerrar.mp4): el alto con el resorte, el contenido que entra cuando el vidrio lo descubre, la flecha que gira y `Series anteriores` que baja y sube con él.
- [VID-02 · la vuelta nueva (4 s)](snapshots/VID-02-vuelta-nueva.mp4): la lista baja, `#1 2:05.0` entra arriba con su marca ámbar, y la banda y el panel cambian en el mismo cuadro.
- [VID-03 · del trabajo al descanso (6 s)](snapshots/VID-03-cambio-a-descanso.mp4): el segundo de atraso y el cambio por pieza.
- [VID-04 · el cambio de repetición (5 s)](snapshots/VID-04-cambio-a-trabajo.mp4): `Rep 1 2:05 680 m` entra en `Series anteriores` y en la línea `Rep` solo ruedan las cifras que cambian.
- [VID-05 · `Simular desconexión` (10 s)](snapshots/VID-05-desconexion.mp4): los metros quietos durante el corte y después, sin salto.
- [VID-06 · la entrada con los paneles (2,5 s)](snapshots/VID-06-entrada-con-paneles.mp4): llegan con SPM, ya armados; nada aparece de golpe después.
- [VID-07 · la salida con la última foto (2,5 s)](snapshots/VID-07-salida-con-paneles.mp4).
- [VID-08 · reducir movimiento (4 s)](snapshots/VID-08-reducir-movimiento.mp4).
- [VID-09 · la cuenta grande y el panel (3 s)](snapshots/VID-09-cuenta-grande-y-panel.mp4): el panel se abre al lado de la cuenta grande, que sigue en su lugar.

### 3.7 Lo que podés verificar vos (opcional, pero recomendado)

1. **El build de la 5 y su demo** (10 minutos): §3.1, empezando por cerrar tu `RowMate.exe` y generar el build.
2. **AC-WKT-02-11: el equipo suspendido** (5 minutos): §3.1 (c). Es el único criterio que solo podés cerrar vos.
3. **De la It. 4a, la pantalla siempre encendida** (1 minuto): `powercfg /requests` en una consola de administrador ([REPORT de la 4a §3.7](../04a-motor-hud-funcional/REPORT.md)).
4. **De las It. 2 y 3**: el modo claro, la app legacy después de la nueva y los idiomas preferidos ([REPORT de la It. 3 §3.7](../03-rutinas/REPORT.md)).

---

## 4. Resultado de las pruebas

Son los números de la ronda 2 de QA ([05-qa-verification, "Ronda 2"](05-qa-verification.md)):

| Qué | Resultado |
|---|---|
| Compilación (dotnet) | **0 advertencias, 0 errores** |
| Pruebas dotnet | **Herramientas 192 / 192; el resto, 2541: 2507 pasan, 0 fallan, 34 ignoradas**. Al cerrar la 4b eran 182 y 2319 |
| Unity EditMode | **2601: 2563 pasan, 0 fallan, 38 ignoradas, 0 inconclusas** |
| Unity EditMode con simulador | **2793: 2755 pasan, 0 fallan, 38 ignoradas, 0 inconclusas**. El modo simulador quedó como estaba |
| Unity PlayMode (interfaz montada) | **426 de 426**; con simulador, **495 de 495**. En la 4b eran 354 y 420 |
| Las ignoradas | Son a propósito: necesitan un teléfono, una Mac, GitHub, la suspensión real del equipo o el build |
| Matriz (658 IDs) | **656 pasan**, 0 fallan, 2 ignorados (AC-WKT-02-10, Android; AC-APP-15-03, Apple) y 0 sin prueba. Los 89 IDs de la 5 (74 criterios y LT-47 a LT-61), todos en PASA. AC-WKT-13-15, AC-WKT-18-21 y AC-WKT-02-11 figuran en PASA porque sus pruebas del Editor pasan; la parte del build o del equipo es NO VERIFICABLE (§5.1) |
| Casos legacy | **LT-47 a LT-61**, portados con su nombre y sus afirmaciones, en dotnet y en Unity |
| Paridad con el legacy | El tracker es el del legacy, línea por línea. La lente de paridad regeneró el oráculo de series desde el legacy y salió **idéntico** (salvo la fecha de generación). 400 secuencias al azar dieron 160 362 estados iguales, con un control que sí detecta una lectura alterada en 1 m. El motor sigue idéntico: el oráculo del motor no cambió desde la 4b y pasa entero |
| Rendimiento | En el peor caso de la 5 (todo lo de la 4b más los dos paneles abiertos con 4 vueltas y 3 repeticiones), el HUD tiene **56 elementos visibles en vertical y 57 en horizontal, con un tope de 60**: le quedan 3 a la 6a. **0 bytes por cuadro** en las diez escenas medidas (abrir, cerrar, la vuelta nueva, la repetición nueva, la marca, la serie que empieza, el anillo de foco, la rueda y la columna desplazable), con el control positivo en la misma corrida. El trabajo de los paneles, mediana 0,82 ms y peor 1,08 ms por cuadro en el Editor. La entrada, en el Editor: el primer toque bajó de 54 a 13,7 ms, y el armado del HUD (10,1 ms) cae en un cuadro negro y quieto. **En el build, sin medir** (§5.1) |
| Capturas | **47 tomas** (38 PNG y 9 MP4): **0 con problemas**; 27 de 27 sondas de aspecto dentro de tolerancia; la llave del simulador y "nada se encima", auditadas en todas. Iguales byte a byte en dos corridas seguidas. Las de la 4b con rutina se regeneraron una sola vez y cambian solo en el lugar de los chips; las de las It. 2, 3 y 4a dieron los mismos bytes. QA y el Designer miraron cada foto y los videos cuadro por cuadro; el PO revisó al cerrar 19 fotos de la 5 y las dos de la 4b de §3.6 |
| Prueba en la PC (build de simulador) | **NO VERIFICABLE**: tu `RowMate.exe` estuvo abierto en `Builds/Windows/simulator` durante toda la iteración. Cuatro tareas esperaron 20 minutos cada una, mirando cada 2 minutos, sin tocar tu programa. Lo cubren en el Editor las pruebas de la interfaz montada (con la selección y el teclado reales de UI Toolkit, y dos arranques seguidos de la app) |
| Builds de Windows (Mono) | El normal (WP-B, antes de los arreglos de la ronda 1) salió sin rastros del simulador, de diagnósticos, de pruebas ni de los argumentos de medida. El de simulador, sin compilar |
| IL2CPP y Android | **No verificables**: faltan los módulos (`build.ps1` sale con 3, "NO VERIFICABLE") |
| Tus datos y los del legacy | Ninguna prueba, captura ni corrida usó tus bases ni tus preferencias: todas usaron carpetas temporales y preferencias en memoria. En tu carpeta de datos, `prefs.json` no existía antes y sigue sin existir, y los demás archivos tienen el mismo tamaño y la misma fecha. El repositorio legacy está intacto (`git status` vacío), y nadie leyó el archivo de credenciales de Strava |
| Repositorio público | 0 rutas de la PC en lo versionado y en lo nuevo |

---

## 5. Criterios de aceptación

| Área | Criterios | Cumplen | Con parte pendiente (entre los que cumplen) | Fallan | No verificables |
|---|---|---|---|---|---|
| WKT nuevos (parciales, repeticiones, paneles y la deuda de la 4b) | 45 | 43 | 5 | 0 | 2 |
| APP nuevos (catálogo) | 1 | 1 | 0 | 0 | 0 |
| De iteraciones anteriores, que cambian (WKT 8, APP 2, BLE 2, HIS 1) | 13 | 13 | 2 | 0 | 0 |
| De iteraciones anteriores, que se vuelven a verificar (WKT 13, APP 1, BLE 1) | 15 | 14 | 2 | 0 | 1 |
| **Total** | **74** | **71** | **9** | **0** | **3** |

Más LT-47 a LT-61, que pasan.

- En la ronda 1 fallaba un criterio menor, AC-BLE-16-02: en la ventana baja, con el diálogo de completado, la llave tapa el chip `Reps`. No hay ningún lugar libre, así que el PO precisó el criterio con esa única excepción (§6.2), y QA lo volvió a verificar.
- Las lentes encontraron dos casos que hacían fallar criterios que el resto de las pruebas daba por buenos:
  - **QA-V5-P01** (AC-WKT-02-08 y -18-07): al volver del segundo plano, una fila de `Series anteriores` podía quedar con el split de otra serie;
  - **LM5-01** (AC-WKT-18-13): en horizontal, con la columna desplazada, al terminar la serie un panel podía quedar escondido arriba y sin forma de volver.

  Los dos se corrigieron, cada uno con una prueba que antes fallaba.

### 5.1 Los no verificables y por qué

| Criterio | Qué falta ver | Por qué no se pudo | Cuándo |
|---|---|---|---|
| AC-WKT-13-15 | Que la entrada no tenga tirones en el build: en tres entradas seguidas (libre, `Prueba 4a` y `Cinco reps` con los dos paneles abiertos), ningún cuadro que se mueve pase de 16,7 ms | Tu `RowMate.exe` abierto en `Builds/Windows/simulator` (riesgo aceptado en el gate). En el Editor pasa el orden de la entrada: el HUD se arma en el primer cuadro negro, y el primer toque bajó de 54 a 13,7 ms | QA, al empezar la 6a, con el build. Si no da, se reabre y va primero |
| AC-WKT-18-21 (su parte de build) | En el build, con `Fondo 3000` y `Series 600` y los paneles abriéndose y cerrándose, ningún cuadro de más de 33 ms y el p99 en 16,7 ms o menos | Ídem. Las otras dos partes pasan: 56-57 elementos (tope 60) y 0 bytes por cuadro | Ídem |
| AC-WKT-02-11 | Que, con Windows suspendido de verdad, el tiempo suspendido no cuente, tampoco en la vuelta en curso | QA no suspende tu equipo, y la demo (c) de la 4b no fue concluyente. Sus pruebas pasan en dotnet y en Unity (124 resultados en la matriz), también con un monitor que sigue contando mientras el equipo duerme | Vos, con la demo (c) |

### 5.2 Cumplen, pero falta una parte en el build o con un equipo

Pasan en el Editor. La parte que se nombra queda pendiente, y si falla al verla, el criterio se reabre.

| Criterio | Parte pendiente | Quién y cuándo |
|---|---|---|
| AC-WKT-18-10 | Cerrar y abrir el build y ver los paneles como quedaron. En el Editor pasa con dos arranques de la app sobre la misma carpeta | Vos con la demo (a), o QA al empezar la 6a |
| AC-WKT-18-12, AC-APP-05-07 | Tab, Enter y Espacio en la ventana real | Vos con la demo (b), o QA al empezar la 6a |
| AC-BLE-15-09 | `Simular desconexión` en el build | Vos con la demo (a), o QA al empezar la 6a |
| AC-WKT-18-09, AC-WKT-18-13 | El dedo: el área de toque de los chips y el arrastre de la columna en el teléfono | El teléfono (6a) |
| AC-WKT-02-08 | En Android, que el primer cuadro al volver del segundo plano ya muestre las vueltas cerradas | El teléfono |
| AC-WKT-18-11 | Que el archivo de preferencias se reemplace entero con IL2CPP (R5-04) | El módulo IL2CPP |
| AC-WKT-13-07 | "Atrás" de Android desde el primer cuadro de la entrada (de la 4b) | El teléfono |

### 5.3 Cumplen, y se completan más adelante

| Criterio | Qué falta | Cuándo |
|---|---|---|
| AC-WKT-18-14, AC-WKT-17-09 (P-03) | Los paneles abiertos en vertical con el bote a la vista: `Series anteriores` abierto tapa la pala izquierda | It. 7 (la alternativa medida es ponerlo debajo de Watts) |
| AC-WKT-17-01 (los paneles) | El vidrio con desenfoque | It. 6b |
| AC-WKT-07-09 con el remo real | Un monitor que no manda la distancia una vez por segundo (QA-V4a-L01) | It. 10a |
| AC-WKT-07-11 | Parciales y repeticiones en el historial (BKL-16, fuera de la paridad) | Backlog |
| AC-WKT-18-12 | Los lectores de pantalla | It. 14 |
| De la 4b: AC-WKT-13-06, -13-12, -19-03, -21-10 y F-APP-15 | La entrada con el vuelo de cámara, el velo sobre la escena, la luz con la hora real, Strava en el diálogo y el cierre de las barras del sistema | It. 6a, 12 y 14 |

---

## 6. Desvíos aceptados

### 6.1 Diferencias visibles con el legacy, a propósito

Ninguna cambia un número ni una regla del legacy. DV-01 a DV-47 ya las aceptaste en las It. 2 a 4b. Si alguna de estas no te gusta, decilo (§8).

| ID | Qué cambia | Por qué |
|---|---|---|
| DV-48 | **Desde el primer cuadro, los paneles muestran el primer paso**: `0:00.0` `0 m` si es de trabajo; `Rep 1/N · 0 m` y el panel de repeticiones si empieza con una serie. El legacy, durante el primer segundo, dice `Sin parciales en este paso` aunque el paso sea de trabajo, y el chip `Reps` aparece de golpe en medio de la entrada (VID-06) | Nada aparece de golpe en la cascada y el panel no contradice a la barra. Los números son los mismos (ceros) |
| DV-49 | **En los paneles nada se corta ni se parte: se achica**, como las cifras del HUD (`Rep 999/999 · 999.999 m`, `5999:59.9`; CAP-25). Solo más allá de eso se recorta con `…` al final. El legacy corta con `…` la vuelta en curso y los valores de las filas, y desde `Rep 100` parte el rótulo en dos líneas | Un número cortado informa mal (como DV-45 de la 4b), y el editor deja hasta 999 repeticiones |
| DV-50 | **El ancho y el lugar.** Los paneles miden **137 base fijos, cerrados y abiertos** (en un teléfono, 144 dp; en la ventana de 1280 × 720, 164), a 8·s de SPM; abrir cambia solo el alto. Terminan antes del disco de la cuenta grande: la cuenta grande y `PAUSA` se ven **siempre** en su lugar. En vertical, abiertos, entran en la zona del bote solo por su franja de afuera (hasta 159 dp en un teléfono, el 24 % de la zona); en horizontal nunca la tocan. El legacy: un chip de 130 como mínimo que abierto pasa a 200 fijos, a 10 de SPM; en un teléfono vertical llega a 225,6 dp, pasa el disco de la cuenta grande y el centro del remero, y en la ventana baja entra 18 dp en la zona del bote (CAP-03, CAP-18 a CAP-22) | La mejora EXR del ROADMAP pide "ancho acotado para no tapar el bote". Con 200, en vertical la cuenta grande y `PAUSA` tendrían que esconderse con un panel abierto, y el panel taparía al remero. Con 8, los dos chips entran sin desplazar en los cinco tamaños |
| DV-51 | **Los tamaños de adentro.** La vuelta en curso en 28 y sus metros en 13 al 80 %, pegados a la derecha; `#n` y `Rep n` en 12, en una columna del ancho del rótulo; los tiempos de las filas en 20; los metros de las repeticiones en 13; 2 entre filas; el título en 11, a una escala por idioma; los vacíos en dos renglones parejos. `Rep r/R · m` y los vacíos quedan como el legacy. El legacy: la vuelta en curso en 30, sus metros en 14 al 55 %, `#n` y `Rep n` en 15 en una columna de 52, los tiempos en 22, los metros de las repeticiones en 16, el título con el estilo del chip | Es lo que entra en 137 sin cortar nada (DV-49). Lo que se lee de lejos queda como en el legacy (la tabla de abajo); lo que se achica (rótulos, metros, títulos) se lee de cerca, como los rótulos de kcal y BPM. Los metros al 55 % no llegan al contraste mínimo; al 80 %, sí |
| DV-26 (de la 4a, ya aceptada) | Con un monitor que falla, el paso por distancia y los parciales cuentan los mismos metros saneados: el trabajo de 1000 m termina con `#1` y `#2`. El legacy, con un paquete sin distancia o un corte, lo termina con solo `#1` y `492 m` o `484 m` en la vuelta en curso; con un reinicio del monitor, recién a los 510 s, con 4 vueltas | Se decidió en la 4a (Q-55). Se anota porque en la 5 se ve en los paneles. No se vuelve a confirmar |

**Para comparar DV-50 y DV-51** (dp de pantalla; el legacy no escala; la legibilidad a 1,5 m en un teléfono vertical):

| Pieza | Legacy | App en un teléfono | App en 1280 × 720 | A 1,5 m |
|---|---|---|---|---|
| Ancho del panel abierto · cerrado | 200 · ≥ 130 | 144,4 · 144,4 | 164,4 · 164,4 | — |
| Vuelta en curso | 30 | 29,5 | 33,6 | De lejos (1,5×) |
| Tiempos de las vueltas y splits de las repeticiones | 22 | 21,1 | 24,0 | De lejos (1,1×) |
| `Rep r/R · m` | 16 | 16,9 | 19,2 | De cerca |
| Metros de la vuelta en curso · de las repeticiones | 14 · 16 | 13,7 · 13,7 | 15,6 · 15,6 | De cerca |
| Rótulo del chip (`500 m`, `Reps`) | 13 | 13,7 | 15,6 | De cerca; de lejos, el color y el ícono |
| `#n` · `Rep n` | 15 | 12,6 | 14,4 | De cerca |
| Título (`Parciales 500 m`, `Series anteriores`) | 13 | 11,0 en es · 11,6 en en | 12,5 · 13,2 | De cerca |

**Del sistema visual** (informativas, no cambian datos ni textos):

- el vidrio de las tarjetas del HUD (DV-31 de la 4a), sin el reflejo, que llega con la 6b para todas las piezas;
- los rótulos del chip y los títulos en el color pleno del panel, como los rótulos de las tarjetas; `#n` y `Rep n` en el gris secundario;
- la flecha dibujada, que gira 180° al abrir y al cerrar;
- los paneles se enfocan con Tab y se abren con Enter o Espacio (el legacy no los deja enfocar);
- con la preferencia en verdadero, el panel se ve abierto desde el primer cuadro (el legacy la lee después del primer dibujo y puede verse cerrado un instante);
- con una clave de preferencias de otro tipo (un archivo editado a mano), solo ese panel arranca cerrado; el legacy no lee ninguna de las dos y deja un error en el registro;
- el resorte, los fundidos, el empuje de la lista, la entrada de la vuelta con su marca y el cambio por pieza (el legacy no anima nada).

Detalle en [01-po-spec §11.1.1](01-po-spec.md).

### 6.2 Decisiones técnicas con efecto que se puede notar

| ID | Qué | Efecto para vos |
|---|---|---|
| AC-BLE-16-02 (B5-10) | **La llave con el diálogo de completado.** La llave busca un lugar que no toque los paneles: en vertical, debajo del diálogo; en el teléfono horizontal, a la derecha de la columna. En la ventana baja (640 × 360) no queda ninguno, y la llave queda en su lugar de la 4b, sobre el `Re` de `Reps`, que está debajo del velo y no responde (CAP-10 de la 4b, en §3.6) | No es una diferencia con el legacy (ahí la llave va fija y tapa lo que haya): precisa la regla de la migración. Mover la llave a otro lugar taparía otra pieza, también debajo del velo |
| La columna desplazable (DV-50) | En el teléfono horizontal y en la ventana baja se desplaza **la columna entera**, como el legacy, con la rueda o arrastrando, y se apaga en sus bordes. En la ventana de 1280 × 720 se desplazan **las filas** de cada panel abierto, y quedan fijas su cabecera, la vuelta en curso y la línea `Rep` | En horizontal, para ver `Series anteriores` con los dos abiertos, hay que bajar la columna. Si el contenido se achica, la columna se acomoda sola, sin saltos (LM5-01) |
| DT5-D1 | El HUD se arma en el primer cuadro negro de la entrada (a los ≈ 230 ms), no en el del toque | El tirón del primer cuadro de la 4b baja a 13,7 + 10,1 ms en el Editor, el segundo en un cuadro negro y quieto. La sesión y el reloj siguen corriendo desde el toque |
| DT5-D2 | El arranque de la app precalienta el HUD (≈ 65 ms más en los dos primeros cuadros, medido en el Editor) | Un arranque apenas más largo, sin verse |
| DT5-03 | La vuelta nueva lleva una **marca** ámbar de 3 a la izquierda, no un fondo ámbar | El fondo bajaba el contraste del valor a 4,9:1; la marca no lo toca |
| DT5-05 / P-11 | `prefs.json` va en la carpeta de la base y lo comparten el build normal y el de simulador; se guarda entero o no se guarda, fuera del hilo principal | Lo que elegís en un build vale en el otro. Las preferencias del legacy nunca se leen |
| DT5-12 | Los paneles responden desde los 520 ms de la entrada, cuando empiezan a aparecer, como los controles | Un toque en un chip en el primer medio segundo no hace nada |
| DT5-16 | Las cifras se achican hasta 0,6 de su tamaño; más allá, la pieza que no entra se recorta con `…` al final | No pasa con valores reales: `S-LARGO` llega a 0,638 |
| V5-09 | En la línea `Rep r/R · m` ruedan solo las cifras que cambian (`1`→`2`, `680`→`4`); el texto queda quieto | VID-04 |
| QA-V4b-R2-01 | El modo compacto de la barra libre: solo en un teléfono vertical en español, el reloj y el chip se acomodan para que `Entrenamiento Libre` quede separado del reloj, entero a 0,805 | CAP-13 de la 4b, en §3.6 |

También se aplicaron DT5-01, -02, -04, -06 a -09, -11, -13 a -15, -17 y -18, sin efecto visible o ya incluidas en los criterios. Detalle en [02-tech-design §8.1](02-tech-design.md).

---

## 7. Issues abiertos y deuda

### 7.1 Issues de QA

En la ronda 2 se cerraron, con evidencia:

- **QA-V5-P01** (mayor, lente de paridad): el split de otra serie en `Series anteriores` al volver del segundo plano.
- **LM5-01** (mayor, lente de motion): la columna desplazada que dejaba un panel escondido, y los recortes que saltaban.
- **QA-V5-01**: la llave sobre `Reps` en la ventana baja con el diálogo (el criterio, precisado).
- **QA-V5-02**: la línea `Rep` que rodaba entera.
- **QA-V5-03 y QA-V5-04**: los valores y la duración de VID-06 y VID-07.
- **QA-V5-05 y QA-V5-P02**: la redacción de la zona del bote y de la clave de otro tipo.
- **LM5-02 a LM5-05**: el anillo de foco en la columna desplazada, una animación que fallaba y quedaba colgada, las escenas de 0 bytes que faltaban medir y la baja del HUD al salir.

Queda 1:

| ID | Qué pasa, en simple | Impacto | Quién | Cuándo |
|---|---|---|---|---|
| QA-V5-R2-01 | Las notas de diseño dicen que, con la vista desplazada, una vuelta nueva no mueve lo que se ve. El código lo hace en la ventana de 1280 × 720, pero no en la columna desplazable del teléfono horizontal y la ventana baja: ahí lo que se ve baja con la lista, suave y sin saltos | Documento. El criterio ("lo que se ve no salta") se cumple | Designer | Al empezar la 6a. Por defecto, que la regla valga solo para la ventana de 1280 × 720 y las notas lo digan, sin cambiar código ni tomas |

**Observaciones, sin issue:**

- **O-1** (lente de motion): si se abren a la vez dos `RowMate.exe`, o el build y el Editor en Play, los dos escriben el mismo `prefs.json` y gana el último. Es lo que hace el legacy (P-11). → un solo `RowMate.exe` a la vez (§3.1).
- **O-6** (lente de motion): al despertar, Windows manda dos avisos, y el registro dice `reanuda (2)`. → ya está en la demo (c).
- **O-3** (lente de motion): con el mouse quieto sobre un panel y el diálogo de terminar abierto con el teclado, el brillo de *hover* puede quedar debajo del velo. → QA lo mira con la prueba en el build, al empezar la 6a.
- **O-4 y O-5** (lente de motion): unos 300 KB fijos de memoria para el texto dibujado y una medida de los textos vacíos que se rehace una vez por segundo. Ninguno asigna por cuadro. → prolijidad.
- La capa de *hover* de los paneles no entró en la medida de 0 bytes (el puntero de prueba no la prende). → la mide QA si se agrega un puntero de prueba.
- Unity reventó al cerrar 3 veces en 16 cambios del modo simulador, y los scripts siguieron solos (QA-V4b-R2-03, cerrado).

### 7.2 Otra deuda

- **Al empezar la 6a, antes de construir encima**: la verificación en el build de la 5 (§5.1 y §5.2), con tu `RowMate.exe` cerrado.
- **Para la 6a**:
  - la fila con 200 píldoras en el teléfono (R4b-02);
  - los 60 cuadros por segundo con el diálogo de completado abierto (O-2 de la 4b);
  - el velo de pausa pasa a la escena y el negro de la entrada, al vuelo de cámara;
  - el presupuesto del HUD queda en 56-57 de 60: la 6a tiene 3 de margen.
- **Para la It. 7**: `Series anteriores` abierto en vertical, con el bote a la vista (P-03); la llave dentro de la zona del bote en la ventana baja (de la 4a); la boya de 500 m en el agua (6a o 7).
- **Para la 10a**: QA-V4a-L01 (ahora también para los parciales, que comparten los metros con el paso por distancia); el reloj de Android con la pantalla bloqueada y que Android cierre la app en segundo plano; un cuadro tarde con el remo real; la variante completa de ROB-04; el *lock* para los avisos del Bluetooth real; las barras de Android, el aviso de suspensión con IL2CPP y el reemplazo de `prefs.json` con IL2CPP.
- **Para la It. 12**: Strava en el diálogo de completado y el indicio de desplazamiento del diálogo (O-01 de la 4b).
- **Para la It. 14**: los lectores de pantalla (también en los paneles), la escala de Windows con dos monitores, la pantalla encendida solo donde hace falta, la háptica (también al cerrar una vuelta) y el cierre de F-APP-15.
- **Deseables, sin criterio**: de la It. 3, `Guardar` con un indicador y la mini-barra que interpola; de la 5, un indicio de "hay más" en las filas desplazables de la ventana de 1280 × 720, en la cabecera, si lo pedís.
- **Backlog**: BKL-26, el aviso de cada vuelta con la diferencia contra la anterior y las filas coloreadas por esa diferencia (P-09; FEATURE_INVENTORY §6.2).
- **DT5-13**: los números de los paneles repiten la lógica de los del HUD; se unifican cuando se toque esa pieza por otro motivo.
- **Con teléfono**: de la It. 3, el teclado del sistema y el menú de texto; de la 4a, "Atrás", el segundo plano, el primer cuadro al volver y la pantalla encendida; de la 4b, las barras del sistema ocultas y "Atrás" con el diálogo; de la 5, el dedo sobre los chips y el arrastre de la columna.
- **CI**: `ci-core` corre en GitHub; `ci-unity` espera la licencia (P-08).

### 7.3 Riesgos

| ID | Estado |
|---|---|
| R5-10 (el build tomado por tu `RowMate.exe`) | **Pasó en toda la iteración.** Se cierra con la verificación en el build al empezar la 6a |
| R4b-01 (P-17 con el modo de espera moderno de Windows) | **Abierto** hasta tu demo (c). Si falla en tu PC, se suma otra señal del sistema |
| R5-04 (el reemplazo de `prefs.json` con IL2CPP), R4b-05 y R4b-06 | **Abiertos** hasta que estén los módulos y el teléfono. Si falla, el próximo arranque lee el archivo anterior, nunca uno a medias |
| R4b-02 (la fila de 200 píldoras en el teléfono) | **Abierto** para la 6a |
| R3-01 / R-T6 (IL2CPP con el SQLite nativo), R3-02 y R3-13 (Android), R4a-01, R4a-10 / RA-08 y R4a-12 / RA-19 | **Abiertos**, como en la 4b |
| R4a-03 (el presupuesto de elementos del HUD) | **Cerrado**: 56-57 de 60 con los dos paneles abiertos, sin juntar más elementos |
| R5-11 (memoria del texto dibujado) | **Mitigado y medido**: 0 bytes por cuadro en las diez escenas, con el control positivo |
| R5-01 y R5-12 (un elemento que dibuja todo, y su área de toque) | **Cerrados**: se probaron en el proyecto antes de construir |
| R5-02 (medidas de texto aproximadas) y R5-06 (el modelo del oráculo) | **Cerrados**: medidas en Unity; el oráculo sale idéntico al legacy |

---

## 8. Decisiones que tenés que confirmar

Si no decís nada, se aplica lo que dice "Por defecto". Alcanza con un "OK a todo", o con decir cuál querés cambiar. Las decisiones de los gates de las It. 1 a 4b ya las aceptaste.

### 8.1 Gate de la It. 5

| Decisión | Qué significa | Por defecto |
|---|---|---|
| **DV-48 / P-02** | Los paneles muestran el primer paso desde el primer cuadro, sin el segundo de `Sin parciales en este paso` ni el chip `Reps` que aparece de golpe (VID-06) | **Se mantiene** |
| **DV-49 / P-13** | En los paneles nada se corta ni se parte: se achica (CAP-25) | **Se mantiene** |
| **DV-50 / P-03 / P-04** | Paneles de 137 base fijos a 8·s de SPM; la cuenta grande y `PAUSA` siempre a la vista; abiertos en vertical, solo por el borde de afuera de la zona del bote; en horizontal, nunca (CAP-03, CAP-18 a CAP-22) | **Se mantiene**. Si no: 130 / 200 y 10, como el legacy, y en vertical la cuenta grande y `PAUSA` no se muestran esa vez si un panel abierto ocupa su lugar. Son constantes: el cambio es chico |
| **DV-51 / P-14** | Adentro de los paneles, rótulos, metros y títulos más chicos que en el legacy, los metros a la derecha y los vacíos en dos renglones; lo que se lee de lejos, como el legacy (la tabla de §6.1; CAP-03 contra `docs/image-03.png` del legacy) | **Se mantiene**. Si no: los tamaños del legacy por la escala, con las filas de repeticiones más achicadas |
| **P-08** | En inglés, `1.112 m` con punto de miles, como el legacy en los dos idiomas (Q-06). Se puede leer como "1,1 m" | **Se mantiene**. La alternativa es `1,112 m` en inglés |
| **AC-WKT-02-11** (P-17, verificación) | Hacé la demo (c), suspendiendo a mano, y contanos qué marcó el reloj al despertar y cuántos puntos quedaron | Si el reloj salta, el criterio se reabre y se corrige al empezar la 6a, antes de construir encima |

### 8.2 Decididas por el PO al cerrar (no necesitan tu respuesta)

- **La verificación en el build** (AC-WKT-13-15, la fluidez de AC-WKT-18-21 y la prueba a mano en la ventana): queda pendiente para el comienzo de la 6a, antes de construir encima. Era un riesgo aceptado en el gate de la 5. Si algo falla, se reabre y va primero.
- **QA-V5-R2-01**: lo decide el Designer al empezar la 6a. Por defecto, la regla de la fila nueva con la vista desplazada vale solo para la ventana de 1280 × 720, y las notas lo dicen.
- **La excepción de la llave** en la ventana baja con el diálogo (AC-BLE-16-02): no es una DV, pero se ve (§6.2).
- **DT5-D1 y DT5-D2** (el HUD armado en el negro y el precalentado en el arranque): se aceptan. No cambian el QUÉ.
- **Las precisiones del modo compacto** de la barra libre en AC-WKT-16-02, -03, -07 y AC-WKT-19-01: los aires del reloj y del chip en un teléfono vertical en español ([01-po-spec, "Al cerrar la iteración"](01-po-spec.md)).
- **P-01, P-06, P-07 y P-09 a P-12 de la spec**: quedan como estaban. Entre ellas: los paneles pasan al paso nuevo un segundo después de la barra, como el legacy (P-01); el orden de Tab (P-06); el aviso de cada vuelta, al backlog como BKL-26 (P-09); las vueltas no se guardan en el historial (P-10); un solo archivo de preferencias para los dos builds (P-11).
- **Pendientes del PO de la spec (§15)**: hechos.
  - En el ROADMAP, la demo de la 5 con `Series 600` (la que decía el plan tenía los números de una captura del legacy, imposibles con 1 000 m de trabajo), "También toca" de la 5 y BKL-26.
  - En el inventario `workout-engine-hud`, las precisiones del legacy (F-WKT-07 y F-WKT-18).
  - En FEATURE_INVENTORY, BKL-26.
  - En el STATUS, el estado al día.

### 8.3 Opcional, sin urgencia

- **Licencia de Unity (P-08 del plan)**: ¿la cargás como secreto del repositorio? Hace falta para que corra `ci-unity`.
- **CAP-27 de la It. 3** (la ventana del build normal con la barra de título oscura): sigue disponible a pedido.

---

## 9. Prerrequisitos para la It. 6a (escena: cielo, luz, hora, Lago alpino y agua degradada)

### 9.1 De tu lado

| Qué | Para qué | ¿Obligatorio? |
|---|---|---|
| Tu OK a §8.1 | Cerrar el gate | Sí (alcanza con "OK a todo") |
| **Cerrar tu `RowMate.exe`** de `Builds/Windows/simulator` (y, si querés, generar el build, §3.1) | Ver la demo de la 5, y que QA haga la verificación en el build antes de construir encima | **Sí**, al empezar la 6a |
| La demo (c) de §3.1: suspender el equipo a mano con un entrenamiento en marcha (5 minutos) | Cerrar AC-WKT-02-11; si falla, se corrige al empezar la 6a | Recomendado |
| Los 10 minutos de §3.1 (a) y (b) | Ver la iteración en la ventana real | Recomendado |
| **Terrain Sample Asset Pack** (Asset Store de Unity, gratis, 1,6 GB) | El terreno y la vegetación de la orilla del lago | Recomendado. Sin él, terreno procedural (digno, menos realista) |
| **Un Android de gama media** (clase Galaxy A54 / Pixel 6a, Android 8+ de 64 bits) con depuración USB, y los módulos **Android Build Support** y **Windows Build Support (IL2CPP)** | Medir 60 fps con la escena (regla 9 del plan); además cierra los NV de Android e IL2CPP de las It. 2 a 5 | Recomendado. Sin él, el rendimiento de la 6a queda NO VERIFICABLE hasta la 14 |
| Licencia de Unity como secreto (P-08) | Que corra `ci-unity` | No |

En el gate de la 6a vas a decidir D-ART-08 (texturas CC0 opcionales y la luna de la NASA; por defecto, no). La lista de Mixamo para la It. 7 la entrega el Designer en la 6a.

### 9.2 Del equipo, al empezar la It. 6a

1. **QA** (o el Architect), antes de construir encima, con tu `RowMate.exe` cerrado:
   - el build de simulador;
   - `[PERF]` de las tres entradas (AC-WKT-13-15) y la ventana con los paneles (AC-WKT-18-21);
   - la prueba a mano en la ventana: abrir y cerrar con mouse y teclado, reiniciar la app con los paneles recordados, `Simular desconexión`, la cuenta grande, el sello y `PAUSA`, y matar el proceso a mitad de sesión;
   - siempre con una carpeta de datos temporal, sin tocar tus bases ni tus preferencias;
   - si tu demo (c) falló: AC-WKT-02-11, con otra señal del sistema.
2. **Designer**: QA-V5-R2-01, y en el diseño de la 6a, la lista de Mixamo para la 7 (P-09), el agua degradada y el cielo, y el velo de pausa sobre la escena.
3. **PO**: escribir la spec de la 6a (`iterations/06a-escena-lago/01-po-spec.md`), con:
   - F-SCN-02, -03, -09, -10, -11, -14, -15 y -23, y lo que también toca (F-SCN-01, -13, -19, -20, -21, F-WKT-13 y F-BLE-16 con "Hora escena");
   - los casos legacy de `scene_camera_test`, `scene_state_test`, `time_of_day_test`, `environment_test`, `sky_shore_painter_test`, `water_painter_test` y `scene_view_test` (los del lago);
   - el paisaje idéntico al legacy (Q-61) y los vectores dorados del generador aleatorio;
   - la entrada con el vuelo de cámara y la luz con la hora real (AC-WKT-13-06, -13-12 y -19-03 de la 4b);
   - el rendimiento con la tabla única (60 fps en el Android de gama media) y lo que quedó para la 6a: la fila de 200 píldoras (R4b-02) y los 60 fps con el diálogo abierto.
4. **Architect**: el presupuesto del HUD con la escena: quedan 3 elementos de margen (56-57 de 60).

---

## 10. Por qué el PO acepta la It. 5

- **El objetivo se cumple.**
  - En los pasos de trabajo de una rutina el remero ve la vuelta en curso y las 4 últimas; en una serie, en qué repetición va, cuántos metros lleva y cómo le fue en las 3 anteriores.
  - Todo en dos paneles que se abren a pedido, recuerdan su estado entre sesiones y después de cerrar la app, y no se enciman con nada en los cinco tamaños.
  - La demo de la spec se ve toma por toma en DEMO-01 a DEMO-12 y en los 9 videos, con la interfaz real.
- **La paridad está probada contra el legacy, con más de lo que pedía la spec.**
  - El tracker es el del legacy línea por línea; el oráculo, regenerado desde el legacy, sale idéntico; y 400 secuencias al azar dan los mismos 160 362 estados.
  - Los 15 casos legacy están portados con su nombre y sus afirmaciones, y cada una de las 46 filas de la checklist de paridad tiene su criterio.
  - Los 9 textos son el español exacto del legacy.
  - Los parciales y el paso por distancia cuentan los mismos metros, segundo a segundo, y el motor sigue dando lo mismo que el del legacy.
- **Nada de la presentación toca el dato.** Abrir, cerrar, animar o desplazar no cambia el motor, la sesión ni la telemetría. Los valores de los paneles cambian en el mismo cuadro que el reloj de la banda, y la salida se los lleva con su última foto.
- **La base es confiable.**
  - Las mismas pruebas pasan en .NET y dentro de Unity, con simulador y sin él, con la interfaz montada (495 pruebas).
  - Las capturas son deterministas.
  - Las lentes encontraron dos casos que el resto de las pruebas no veía. Los dos se corrigieron con una prueba que antes fallaba.
  - El HUD entra en su tope de elementos sin juntar más piezas y no reserva memoria por cuadro, medido con control positivo.
  - Tus datos, tus preferencias y los del legacy no se tocaron.
- **Lo pendiente es una verificación, no una falla, y tiene fecha.**
  - Ningún criterio falla.
  - Los 3 no verificables son la medida en el build (porque tu `RowMate.exe` estuvo abierto) y tu PC suspendida. La medida se hace al empezar la 6a, antes de construir encima, y si algo no da, se reabre y va primero.
  - El único issue abierto es de documentación.
- **Lo único que falta de tu lado** es tu OK a §8.1 y cerrar tu `RowMate.exe` para ver la demo; si podés, también los 5 minutos de la demo (c).
