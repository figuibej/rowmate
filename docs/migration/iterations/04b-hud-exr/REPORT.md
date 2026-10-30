# It. 4b — HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema: reporte de cierre

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-30 |
| Estado | **Cerrada, espera tu OK** |
| Veredicto del PO | **ACEPTADA, sin condiciones.** El HUD funcional de la 4a ya está al nivel EXR, y la pantalla inmersiva hace todo lo que hace la del legacy. Lo que trae: arriba, las píldoras de la rutina, que se llenan sin saltos y quedan sólidas o huecas según el resultado; el reloj de pared; en el libre, el chip `EN MARCHA` / `PAUSADO` / `FINALIZADO`. En los últimos 3 s de cada paso aparece la cuenta grande `3`, `2`, `1`, y al cerrar un paso con objetivo, el sello `Objetivo cumplido` / `No alcanzado`. Al terminar la rutina, el diálogo de completado, que espera al remero como en el legacy. Además, la entrada "sumergirse", la salida que nunca pasa por `00:00`, el velo de pausa y, en Android, las barras del sistema ocultas. El motor sigue dando lo mismo que el del legacy, ahora también después del fin. Cumplen 98 de 101 criterios y no falla ninguno. Los 3 que no se pudieron ver son de Android, de Apple y de tu equipo suspendido; ese último lo verificás vos en la demo (c) |
| Veredicto de QA | **APROBADO CON OBSERVACIONES** en la ronda 2 ([05-qa-verification](05-qa-verification.md), "Ronda 2"): 0 problemas graves, 0 mayores y 4 menores. En la ronda 1 habían fallado dos criterios menores, la llave del simulador en la salida y el espaciado de letras del chip. Se corrigieron y QA los volvió a verificar, junto con otros 10 menores. Las dos lentes, [paridad](05-qa-lens-paridad.md) y [motion](05-qa-lens-motion.md), también dieron APROBADO CON OBSERVACIONES |
| Documentos | [01-po-spec](01-po-spec.md) (qué se pidió, con §17 al cerrar) · [02-tech-design](02-tech-design.md) (cómo se hizo, con los cambios de la implementación y de la ronda 1) · [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) (control previo) · [04-design-notes](04-design-notes.md) (aspecto y movimiento, con §15 y §16) · [05-qa-verification](05-qa-verification.md) (verificación en dos rondas), con sus lentes [05-qa-lens-paridad](05-qa-lens-paridad.md) y [05-qa-lens-motion](05-qa-lens-motion.md) · [00-deuda-4a](00-deuda-4a.md) y [00-deuda-diseno](00-deuda-diseno.md) (la deuda de la It. 4a) · [snapshots/INDEX.md](snapshots/INDEX.md) (capturas) |

---

## 1. En pocas palabras

La It. 4b termina el entrenamiento por fuera: todo lo que el legacy muestra mientras remás, ahora con el acabado EXR.

- **La barra de pasos de la rutina.**
  - Arriba van las píldoras, una por paso y todas del mismo ancho, como el legacy (Q-59).
  - Cada una muestra su estado: **pendiente** (tenue), **en curso** (se llena de forma continua, sin saltos de 1 s), **hecha y cumplida** (sólida) o **hecha y no cumplida** (hueca, con su borde).
  - El último paso queda "en curso" y nunca muestra su resultado (Q-52).
  - Debajo, la fila de información de la 4a con el **reloj de pared** (`HH:mm`, siempre de 24 h), pegado al borde derecho junto con la cuenta regresiva.
- **El final de cada paso se siente venir.**
  - En los últimos 5 s, la píldora late con un halo de su color.
  - En los últimos 3 s aparece en grande `3`, `2`, `1`, en un disco con un aro, sin tapar el bote ni las tarjetas.
- **El resultado, en el momento.** Al cerrar un paso con objetivo, durante 2 s se ve el sello `Objetivo cumplido` (verde) o `No alcanzado` (ámbar) con tu promedio contra el objetivo, por ejemplo `150 / 160 W · 24.0 / 22 spm`. El promedio se redondea hacia el lado desfavorable, así nunca contradice el veredicto.
- **El entrenamiento libre** tiene su barra: `Entrenamiento Libre`, el reloj y el chip `EN MARCHA` (con un punto que late), `PAUSADO` o `FINALIZADO`.
- **El diálogo de rutina completada** reemplaza el aviso provisorio de la 4a.
  - Aparece 0,3 s después del último segundo, una sola vez, con `Tiempo`, `Distancia` y `Calorías`.
  - El check se dibuja y los totales cuentan hasta su valor exacto.
  - Queda hasta que tocás `Terminar`: tocar afuera o Escape no lo cierra. Mientras tanto, la sesión espera, como en el legacy.
  - Nunca aparece al terminar a mano (Q-53).
- **Entrar y salir.**
  - Al tocar `Entrenamiento libre` o `Iniciar`, Workout se apaga a negro y el HUD llega en cascada, de arriba abajo, en poco más de un segundo. El reloj de la sesión corre desde el toque.
  - Al salir, el HUD se va con sus últimos valores, sin pasar nunca por un tablero en `00:00`.
  - En Android, las barras del sistema se ocultan al entrar y vuelven al salir. Eso no se pudo ver: falta el teléfono.
- **La pausa se ve.** El fondo se oscurece un poco y aparece `PAUSA` en el lugar de la cuenta grande. Las tarjetas siguen enteras y en vivo.
- **Nada salta.**
  - kcal se desliza cuando aparece o se va el pulso.
  - Los números usan cifras de tablero de ancho fijo.
  - El divisor de la banda se ve en todos los tamaños.
- **El equipo suspendido** (P-17, lo que aceptaste en el gate de la 4a): si Windows suspende la PC con una sesión en marcha, ese tiempo no cuenta, como en el legacy. Lo prueban 87 pruebas, en los dos comportamientos posibles del reloj del sistema. **Falta verlo en tu PC**: es la demo (c).

Se comporta como el legacy:

- las reglas de las píldoras, el último paso en curso, el diálogo una vez por sesión y nunca a mano, el chip, la barra libre y el reloj de pared;
- el caso legacy `step_pill_test` (LT-46), portado con sus 12 afirmaciones;
- el motor: la lente de paridad volvió a correr el motor del legacy desde su código y dio **el mismo archivo, byte a byte**. Ahora también compara lo que pasa después del fin, con 16 guiones.

Lo que el legacy hace mal, corregido a propósito (§6.1, DV-40 a DV-47):

- con más de unos 120 pasos, la fila del legacy queda vacía; acá nunca desaparece (DV-40);
- al salir, el legacy muestra un instante `00:00` y `—` (DV-41);
- en el legacy, tocar `Terminar` del diálogo antes de que termine el guardado pierde el cierre de la sesión (DV-42);
- la pastilla de objetivo ya no corta un número por la mitad (DV-45);
- la hora ya no salta de lugar (DV-46);
- los títulos ya no se salen de su caja (DV-47).

Además:

- **La deuda de la 4a quedó saldada al empezar** ([00-deuda-4a](00-deuda-4a.md)):
  - las **dos condiciones**: el divisor de la banda y el primer Tab con anillo de foco;
  - las cifras de tablero y el kerning;
  - nada que siga animando fuera de pantalla;
  - los 15 guiones nuevos del oráculo del motor;
  - una sola regeneración de las capturas de las It. 2, 3 y 4a.
- Hay **2501 pruebas en dotnet**: 182 de las herramientas y 2319 del resto, contra 2228 al cerrar la 4a. También hay **420 pruebas de la interfaz montada** con el simulador, contra 328 en la 4a.
- Las **43 capturas y los 8 videos** los genera Unity con la interfaz real, y salen iguales byte a byte en dos corridas.
- Ni las pruebas ni las capturas tocaron tus bases ni las del legacy.

Lo que no se pudo ver:

- **Android**: faltan el módulo y el teléfono. Quedan sin ver las barras del sistema ocultas y "Atrás".
- **La suspensión real de tu equipo**: QA no suspende tu PC. Lo verificás vos (§3.1 c).

---

## 2. Qué se entregó, por funcionalidad

"Cumple" quiere decir que pasa todo lo que se pudo verificar. "Con parte pendiente" quiere decir que cumple en el Editor y en la PC, pero falta verlo con un equipo (§5.2).

| ID | Qué quedó listo | Criterios | Dónde se ve |
|---|---|---|---|
| F-WKT-15 | **La píldora y sus cuatro estados**. Pendiente al 18 %; en curso, con la pista al 30 % y el relleno sólido con un brillo fino arriba, sin marco; hecha y cumplida (o sin objetivo), sólida al 85 %; hecha y no cumplida, hueca, con un borde de 1,5. En el color de su tipo. Los estados se distinguen por la forma, también con daltonismo. LT-46 portado | 6 de 6 | CAP-01, CAP-04 |
| F-WKT-14 | **La barra de pasos**. La fila de píldoras, del mismo ancho y en el orden de la rutina expandida. El relleno es continuo, nunca adelanta al motor y nunca retrocede. Al cambiar de paso, la píldora se solidifica con un destello o se le dibuja el borde. El último paso queda en curso: lleno si es por tiempo y con la pista sola si es por distancia. Con muchos pasos, una franja con un **pulgar** en el paso actual (200 pasos en CAP-22, hasta 1998 en las pruebas). El halo de los últimos 5 s, la cuenta grande de los últimos 3 s y el reloj de pared pegado a la derecha. Cuando la fila no entra, la pastilla de objetivo pierde fragmentos enteros (`>150W · 24 spm…`) y, si hace falta, se va. Sin interacciones | 13 de 13 (1 con parte pendiente: `Serie larga` en el build) | Arriba, con rutina |
| F-WKT-19 | **El reloj de pared**. `HH:mm` en 24 h, con su ícono. Cambia a más tardar 1 s después del minuto, con un rodillo. Sigue en pausa, con la sesión terminada y con el diálogo abierto. Muestra la hora nueva si cambia la zona o la hora de la PC, sin tocar el reloj de la sesión | 5 de 5 | Las dos barras |
| F-WKT-16 | **La barra del libre y el chip de fase**. Ícono, `Entrenamiento Libre`, el reloj y el chip, de ancho fijo para que el reloj no se corra. `EN MARCHA`, en verde y con un punto que late; `PAUSADO`, en amarillo; `FINALIZADO`, en `#FF7A92` mientras se guarda. En un teléfono en español el título se achica hasta 0,805 para entrar entero | 5 de 5 | Arriba, en libre |
| F-WKT-21 | **El diálogo de rutina completada**. Aparece a los 300 ms del fin, una vez por sesión y nunca a mano. Es modal: tocar afuera o Escape no hacen nada. Trae el check, el título, el mensaje y los totales fijos del fin: `Corta` da `00:30`, `1120 m` y `43`. Los totales cuentan hasta su valor exacto. `Terminar` espera el cierre de la sesión y, si falló, Workout avisa `No se pudo guardar la sesión`. Si la rutina terminó minimizada, al volver ya está abierto. Teclado y letra 1,3× incluidos. Sin Strava hasta la It. 12 | 13 de 13 (1 con parte pendiente: "Atrás" de Android) | Al terminar una rutina |
| F-WKT-13 | **El inmersivo, completo sin la escena**. La entrada "sumergirse" (el negro en 220 ms y la cascada hasta los 1,1 s) y la salida con la última foto, en menos de 1 s. El velo de pausa. Al salir no queda nada animando, y el registro del build quedó con 0 avisos de DPI. Una sesión nueva tiene su propio inmersivo, aunque el anterior esté saliendo. El diálogo, a lo sumo una vez. Ninguna pieza nueva se encima con otra ni entra en la zona del bote. La presentación nunca adelanta ni demora el dato | 9 de 9 (1 con parte pendiente: "Atrás" de Android) | Al entrar, al salir y en pausa |
| F-WKT-17 | **Tarjetas y banda, cerradas sin el desenfoque**. kcal se desliza cuando BPM aparece o se va. Cifras de tablero (RowMate Numeric SC) y kerning en toda la app. El divisor de la banda, a la vista (condición de la 4a) | 2 de 2, más la condición | El HUD |
| F-WKT-09 (también toca) | **El sello de resultado** (D-ANI-05). Dura 2 s, en una tercera fila de la barra que crece sin mover las tarjetas. `Objetivo cumplido` o `No alcanzado`, con un fragmento por objetivo y el redondeo desfavorable (P-03). No aparece en el último paso, ni en un paso sin objetivos, ni en uno cortado con `Terminar` | 2 de 2 | Al cerrar cada paso con objetivo |
| F-WKT-02 (también toca) | **P-17: el equipo suspendido no cuenta**. Al despertar: el reloj, los pasos y los puntos siguen desde donde estaban, sin avisos ni un `No se pudo guardar` falso. La hora del HUD es la actual | 0 de 1: **no verificable** en el equipo (lo verificás vos); en dotnet y PlayMode pasan sus 87 pruebas | §3.1 (c) |
| F-APP-15 (también toca) | **Las barras del sistema**. Se ocultan al entrar y vuelven al salir, también con dos inmersivos a la vez, con el diálogo abierto y al volver del segundo plano. En la PC la ventana no cambia: ni tamaño, ni posición, ni pantalla completa | 2 de 4; 2 no verificables (Android y Apple) | Registro del build: `barras del sistema: ocultas` / `visibles` |
| F-APP-10 (también toca) | **12 claves nuevas** en el catálogo (214 por idioma), con el español del legacy donde el legacy lo tiene fijo | 1 de 1 | — |
| Las dos condiciones de la 4a | **AC-WKT-17-04**: el divisor de la banda se ve en todos los tamaños. **AC-APP-05-07**: después de usar el mouse, el primer Tab ya muestra el anillo de foco, en el HUD, en Workout, en el editor de Rutinas y en el diálogo | 2 de 2 | CAP-13; el teclado |
| De iteraciones anteriores (38) | **23 que cambian**: el fin automático que espera al diálogo, el aviso que se va, el inmersivo que se abre con "sumergirse" y se cierra con la última foto, la barra de pasos de dos filas (tres con el sello), la pastilla por fragmentos, `Próximo` compacto, los diálogos, letra 1,3× y reducir movimiento en las piezas nuevas, la cinta, la llave y el techo del panel del simulador. **15 que se vuelven a verificar** con las piezas nuevas: capas, Escape, tarjetas, rotación con el diálogo, el sello o la cuenta grande a la vista, nada que se encime, la zona del bote, valores extremos y tus bases | 38 de 38 (2 con parte pendiente: Android) | — |

Además:

- **El caso legacy LT-46** (`pendiente, en curso, hecho y no cumplido se pintan distinto`) está portado con sus 12 afirmaciones y pasa en dotnet y en Unity.
- **El oráculo del motor.** El motor del legacy corre fuera del legacy y se compara con el nuevo:
  - 11 escenarios y 16 guiones, entre ellos los de QA-V4a-L02 y `CompletedHoldReset`, que mira lo que pasa después del fin;
  - la lente de paridad lo regeneró desde el legacy y salió idéntico.
- **Herramientas para el equipo**:
  - el arnés de capturas `it4b`, con 51 tomas, 150 sondas de aspecto y valores por cuadro en los 8 videos;
  - en el build de simulador, el registro anota los tiempos de cuadro (`[PERF]`), los avisos de suspensión (`[TIME]`, `[WORKOUT]`) y las barras del sistema (`[UI]`);
  - `SnapshotCli.SeedDatabase -seed F4A`, que siembra una base de prueba en una carpeta temporal.
- **Documentos al día**:
  - el ADR [020](../../architecture/adr/020-reloj-despierto.md) (el reloj "despierto" de P-17);
  - ART_BIBLE, ANIMATION_BIBLE y LEGACY_VISUAL_AUDIT, del Designer;
  - al cerrar: el ROADMAP y los inventarios `workout-engine-hud` y `app-shell-platform` ([01-po-spec §17](01-po-spec.md)).

---

## 3. Cómo ver la app

### 3.1 La demo en el build de Windows con simulador (lo más rápido)

El build está en la carpeta de siempre: **`Builds/Windows/simulator/RowMate.exe`**, con su carpeta `RowMate_Data` al lado. Es el del código final, con los arreglos de la ronda 2: ningún archivo de código cambió después de compilarlo. Si preferís generarlo vos, mirá §3.3.

**Antes de empezar:**

- **Un solo `RowMate.exe` a la vez.** Hoy no hay ninguno abierto. Si abrís otro, los dos escriben en la misma base.
- **El Firewall.** Si aparece el aviso "Seguridad de Windows", contestá **`Cancelar`**: la app no usa la red.
- **El idioma.** El build sigue la lista de idiomas de Windows, como el legacy. Si Windows está en inglés, el build sale en inglés: por eso el texto en inglés va entre paréntesis.
- **El simulador del build tiene "ruido"**: los valores se mueven un poco alrededor de lo que elegís en el panel. Por eso los números del sello y del diálogo van a ser parecidos a los de las capturas, no idénticos.
- **La distancia y las calorías del diálogo son las del monitor**, como en el legacy, y el simulador las viene sumando desde que abriste la app. Si antes hiciste un libre, la distancia del diálogo incluye esos metros: no es un error.
- **Los controles responden desde que aparecen**, a los 0,84 s del toque (P-15). Un toque en `Pausa` durante la cascada no hace nada.

**(a) Entrenamiento libre: sumergirse, la hora, el chip y la pausa** (2 minutos)

1. **Abrí `RowMate.exe`.** Arranca en Dispositivo, con `Remo simulado` · `Conectado` (`Connected`).
   - *Opcional, para ver kcal que se desliza*: en la tarjeta del pulsómetro, `Buscar` (`Search`) → `Pulsómetro simulado` → `Conectar` (`Connect`).
2. **Workout → `▶ Entrenamiento libre`** (`Free workout`).
   - Workout se funde a negro, aparece el fondo y el HUD llega en cascada, de arriba abajo, en poco más de un segundo.
   - El reloj de la banda ya corre (`00:01` llega antes de que termine la cascada).
   - La llave del simulador entra al final. → VID-01
3. **Arriba**: el ícono de remo, `Entrenamiento Libre` (`Free Workout`), la hora de tu PC en 24 h con su ícono y el chip `● EN MARCHA` (`ACTIVE`), verde, con un punto que late. → DEMO-02
4. **`⏸ Pausa`**.
   - El fondo se oscurece un poco y aparece, en un disco, `PAUSA` (`PAUSED`).
   - El chip pasa a `PAUSADO`, en amarillo y sin punto, y el botón a `▶ Reanudar`, verde.
   - Las tarjetas siguen enteras, con los valores en vivo, y el reloj de la banda se detiene. → DEMO-03, VID-06
5. **`Reanudar`**: todo vuelve.
6. *(Opcional, con el pulsómetro)* **La llave → `Simular caída del pulsómetro`**. BPM se va y kcal baja **deslizándose** a su lugar; a los 3 s BPM vuelve y kcal sube. → VID-07
7. **`Terminar` → `Terminar`** (`Finish`).
   - El chip dice `FINALIZADO` (`FINISHED`) un instante.
   - El HUD se va con sus últimos valores, sin pasar nunca por `00:00`; la pantalla funde a negro y vuelve Workout.
   - No hay diálogo ni aviso. → VID-02, DEMO-04

**(b) Una rutina con píldoras, halo, cuenta grande, sello y diálogo** (`Prueba 4a`, 5 minutos)

Si no tenés `Prueba 4a`, armala en **Rutinas** como en el [REPORT de la 4a §3.1 (b)](../04a-motor-hud-funcional/REPORT.md): calentamiento de 30 s; una serie ×2 de trabajo de 1 min (W mín. 160, SPM 22, +2 SPM por repetición) y descanso de 30 s; enfriamiento de 30 s.

1. **La llave**: dejá `Watts` en `150` con `−` o `+` y `Ritmo` en `24`. Cerrá el panel con su `✕`.
2. **`Prueba 4a` → `Iniciar`** (`Start`). La barra de pasos llega primero. → VID-03
   - Arriba van **6 píldoras del mismo ancho**: amarilla, rosa, verde, rosa, verde y azul.
   - La amarilla se va llenando de a poco, sin saltos.
   - Debajo, `Calentamiento` (`Warmup`), la cuenta `0:30` y la hora, pegadas a la derecha. → DEMO-05
3. **El final del paso.**
   - En `0:15` aparece `Próximo: Trabajo` (`Up next: Work`), que late suave.
   - En `0:05` la píldora late con un halo y la cuenta crece y toma el color del paso.
   - En `0:03` aparecen en grande `3`, `2`, `1`, en un disco con un aro que se vacía, debajo de las tarjetas de arriba. → DEMO-06, VID-04
4. **Trabajo 1, a 150 W.** La pastilla dice `>160W · 22 spm · ↗ SPM +2/rep`, y Watts va en rojo con `▲` y `obj. 160`. Al terminar, a los 1:30 de la sesión:
   - la barra crece una fila y, durante 2 s, se lee `ⓘ No alcanzado · 150 / 160 W · 24.0 / 22 spm` (`Target missed`), en ámbar;
   - la píldora rosa queda **hueca**: tenue, con un borde;
   - la verde del descanso empieza a llenarse. → DEMO-07
5. **En el descanso**: la llave → `+` de Watts dos veces (170) y `+` de `Ritmo` dos veces (26). El panel no tapa Split, Watts ni el lugar del sello. Cerralo. → DEMO-08
6. **Trabajo 2** (`>160W · 24 spm`). Al terminar, a los 3:00, se ve `✓ Objetivo cumplido · 170 / 160 W · 26.0 / 24 spm` (`Target met`), en verde, y la píldora queda **sólida**. → DEMO-09
7. **Enfriamiento.**
   - No hay `Próximo`, porque es el último paso.
   - En los últimos 3 s, la cuenta grande. → DEMO-10
   - A los 4:00 la píldora azul queda llena, con un brillo fino arriba que la distingue de una sólida, y no marca resultado (Q-52).
8. **El diálogo.** 0,3 s después aparece `¡Entrenamiento completado!` (`Workout Completed!`). → DEMO-11, VID-05
   - El check se dibuja.
   - Los totales cuentan hasta su valor: `Tiempo` `04:00`, y la distancia y las calorías del monitor.
   - Detrás del velo, los valores en vivo y la hora siguen.
9. **El diálogo espera.**
   - Un clic afuera o Escape no hacen nada.
   - Tab enfoca `Terminar` con el anillo, y otro Tab no sale del diálogo.
10. **`Terminar`** (o Enter). La llave se va en el primer cuadro, el HUD se va, la pantalla pasa por negro y vuelve Workout en reposo, sin aviso. → DEMO-12

**(c) El equipo suspendido** (P-17, 5 minutos). **Lo verificás vos**: QA no suspende tu equipo. Hacelo con el build, no en el Editor: en el Editor la app no registra el aviso de suspensión de Windows (DT4b-20).

1. `Entrenamiento libre`. Dejá correr el reloj de la banda hasta `01:00`.
2. **Suspendé Windows**: Inicio → Inicio/apagado → **Suspender**, o cerrá la tapa si así está configurada. Esperá **1 minuto** y despertá la PC.
3. **Lo que tiene que pasar**:
   - el reloj de la banda sigue desde `01:00` (a lo sumo `01:01`), **no** desde `02:00`;
   - el chip sigue en `EN MARCHA` y la hora del HUD es la actual;
   - no aparece ningún aviso.
4. Dejá correr hasta `02:00` → `Terminar` → `Terminar`.
5. **Antes de volver a abrir la app**:
   - Abrí `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate\Player.log` con el Bloc de notas y buscá `durmió`. Tiene que estar la línea `[WORKOUT] el equipo durmió 60.0 s: no cuenta (P-17; …)`, con más o menos segundos.
   - Si en cambio dice `[TIME] cuadro de … s sin aviso de suspensión: cuenta`, la señal de Windows no llegó (R4b-01).
   - Hay que mirarlo antes, porque al volver a abrir la app el registro anterior pasa a `Player-prev.log`.
6. **Los puntos.** En una consola abierta en la carpeta del repositorio, corré con .NET 8:

   ```
   dotnet run --project tools/dotnet/RowMate.SessionList -- --variant simulator
   ```

   Si `dotnet` no está en el PATH, usá el de la carpeta de instalación de .NET 8, como en `tools/dotnet/README.md`. La sesión más nueva tiene **unos 25 puntos**: uno cada 5 s de los 2 minutos remados, más el final. Si el tiempo suspendido hubiera contado, tendría unos 37.

**Contanos** qué marcó el reloj al despertar y cuántos puntos tiene la sesión. Si querés, pegá también la línea `[WORKOUT] …` o `[TIME] …`, y solo esa, porque el resto del registro trae rutas de tu PC. Si el reloj saltó, AC-WKT-02-11 se reabre y se corrige al empezar la It. 5, sumando otra señal del sistema.

**(d) Las dos condiciones de la 4a** (1 minuto)

- El divisor entre el reloj y la distancia de la banda se ve en todos los tamaños: mirá CAP-13 (teléfono vertical) o el Editor en `411 x 914`.
- En la PC, después de usar el mouse, **el primer** Tab ya muestra el anillo de foco: en el HUD (`Terminar`), en Workout y en el editor de Rutinas.

Si querés probar más, en el mismo build:

- **Minimizar.** Con `Prueba 4a` en el enfriamiento, minimizá la ventana y restaurala cuando la rutina ya terminó. El diálogo ya está abierto, sin la animación de entrada, con los totales del fin y una sola vez.
- **Terminar a mano en el último paso.** `Terminar` → `Terminar` en el enfriamiento: nunca aparece el diálogo de completado (Q-53).
- **Toques en el negro.** Con dos clics rápidos en `Terminar` del diálogo, o un clic en `Entrenamiento libre` mientras la pantalla está negra, no pasa nada. Desde que Workout empieza a verse, a los 0,4 s, los toques valen.
- **Reducir movimiento.** Windows → Configuración → Accesibilidad → Efectos visuales → apagá **Efectos de animación**. La app lo toma cuando la ventana recupera el foco.
  - La entrada y la salida pasan a ser un fundido corto.
  - El halo y el punto en vivo quedan quietos.
  - El diálogo entra con los totales ya puestos.
- **La ventana.** Entrar y salir del entrenamiento no cambia el tamaño, la posición ni el estilo de la ventana (AC-APP-15-02).

**Tus datos.** El build de simulador guarda las rutinas y las sesiones en `rower_app_dev.db`, dentro de `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`. El build normal usa `rower_app.db` en la misma carpeta. La app legacy guarda sus datos en otro lugar y la nueva no los toca.

### 3.2 En Unity, con el simulador

Igual que en la It. 4a ([REPORT de la 4a §3.2](../04a-motor-hud-funcional/REPORT.md)). Unity admite una sola instancia por proyecto: antes de abrir el Editor, verificá que no esté corriendo ninguno de los scripts de §3.3 a §3.6.

1. **Abrí el proyecto** en Unity Hub, con el Editor **6000.3.27f1**.
2. **Prendé el modo simulador**: menú **RowMate → Modo simulador**.
3. **Abrí la escena** `Assets/RowMate/Scenes/Boot.unity`.
4. **Elegí el tamaño en la vista *Game***: `1280x720` (la ventana de Windows), `411 x 914` (teléfono vertical), `914 x 411` (teléfono horizontal) o `640 x 360` (ventana baja).
5. **Dale Play** y seguí los pasos de §3.1 (a), (b) y (d). La parte (c) no vale en el Editor.
6. **Idioma, letra grande y "reducir movimiento"**: están en el menú **RowMate → Simular**.

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

Es el build normal, sin cinta, llave ni marca de desarrollo. **El de `Builds/Windows/normal` que hay hoy es anterior a los arreglos de la ronda 1**: el Architect lo generó en WP-B y salió limpio en el inspector, sin rastros del simulador, de diagnósticos ni de pruebas. Para verlo con el código final, generalo otra vez con este comando.

El script termina con 0 si el build salió bien, con 2 si el build normal tiene restos del simulador, de diagnósticos o de pruebas, con 3 si falta un módulo (NO VERIFICABLE) y con 5 si se le pide una carpeta nueva para un build de desarrollo. Hoy, `-Backend IL2CPP` y Android terminan con 3.

### 3.4 Las pruebas sin Unity (.NET 8)

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1
```

Al final aparecen dos líneas `Passed!`. Las herramientas, con 182 que pasan, y el resto, con 2319 pruebas, de las que 2292 pasan y 27 quedan ignoradas. Las ignoradas piden un teléfono, una Mac, GitHub o la suspensión real del equipo. Si ninguna línea dice `Failed!`, está todo verde.

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix
```

Escribe `tools\dotnet\.artifacts\TestResults\ac-matrix.md` con los 597 IDs: los 535 de las iteraciones anteriores, los 61 criterios nuevos y LT-46.

- Sin los resultados de Unity: 478 PASA, 117 PENDIENTE UNITY y 2 ignorados.
- Si antes corriste §3.5, agregá `-UnityResults tools/unity/.artifacts`: la matriz queda en **595 PASA**, 0 FALLA y 2 IGNORADO (AC-WKT-02-10 de Android y AC-APP-15-03 de Apple).

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
| EditMode | 2368 | 2337 | 0 | 31 |
| EditMode `-Simulator` | 2550 | 2519 | 0 | 31 |
| PlayMode | 354 | 354 | 0 | 0 |
| PlayMode `-Simulator` | 420 | 420 | 0 | 0 |

- Las PlayMode montan la app real con una base de prueba. Cubren:
  - las píldoras;
  - la cuenta grande, el sello y la pausa;
  - la barra libre;
  - el diálogo, con la base lenta, la que falla y el segundo plano;
  - la entrada y la salida;
  - la rotación;
  - las asignaciones de memoria por cuadro (0);
  - el primer Tab con anillo;
  - la cinta y la llave.
- **Ojo (QA-V4b-R2-03)**: a veces el Unity que restaura el modo simulador sale con `-1073741819` después de escribir `[SIMULATOR] defines restaurados`. Las pruebas ya habían pasado y el modo queda bien restaurado: repetí la corrida. Se arregla al empezar la It. 5.

### 3.6 Las capturas

Están en [snapshots/](snapshots/). La tabla completa, con las sondas y la auditoría de cada toma, está en [snapshots/INDEX.md](snapshots/INDEX.md).

- Las renderiza Unity con la interfaz real, con una base de prueba, el reloj de prueba con la zona en UTC−03:00 (el reloj de pared marca `10:00`) y el simulador sin ruido. No son maquetas.
- Para regenerarlas, con el Editor cerrado:

  ```
  powershell -NoProfile -File tools/unity/capture-snapshots.ps1 -Set it4b
  ```

- La cinta `SIMULADOR` tapa en parte el comienzo de la barra de arriba y de la primera píldora, como en el legacy (`docs/image-03.png`).

**La demo, toma por toma** (ventana de 1280 × 720):

| DEMO-01 · Workout en reposo | DEMO-02 · libre a los 3,2 s: la barra libre |
|---|---|
| ![DEMO-01](snapshots/DEMO-01-workout.png) | ![DEMO-02](snapshots/DEMO-02-libre.png) |

| DEMO-03 · la pausa: velo, `PAUSA` y `PAUSADO` | DEMO-05 · `Prueba 4a` a los 18,2 s: 6 píldoras |
|---|---|
| ![DEMO-03](snapshots/DEMO-03-pausa.png) | ![DEMO-05](snapshots/DEMO-05-prueba-4a.png) |

| DEMO-06 · el halo y la cuenta grande `3` | DEMO-07 · `No alcanzado` · `150 / 160 W · 24.0 / 22 spm` |
|---|---|
| ![DEMO-06](snapshots/DEMO-06-cuenta-grande.png) | ![DEMO-07](snapshots/DEMO-07-sello.png) |

| DEMO-08 · el panel abierto, sin tapar Split, Watts ni la barra | DEMO-09 · `Objetivo cumplido` · `170 / 160 W · 26.0 / 24 spm` |
|---|---|
| ![DEMO-08](snapshots/DEMO-08-panel.png) | ![DEMO-09](snapshots/DEMO-09-cumplido.png) |

| DEMO-10 · la cuenta grande `1` del último paso | DEMO-11 · el diálogo: `04:00`, `957 m`, `58` |
|---|---|
| ![DEMO-10](snapshots/DEMO-10-cuenta-final.png) | ![DEMO-11](snapshots/DEMO-11-completado.png) |

[DEMO-04](snapshots/DEMO-04-terminado.png) y [DEMO-12](snapshots/DEMO-12-reposo.png) son la vuelta a reposo después de terminar a mano y después del diálogo, respectivamente. Son idénticas byte a byte a DEMO-01: sin diálogo ni aviso.

**Las píldoras, la cuenta grande y el sello** (teléfono vertical, rutina `Estados`):

| CAP-01 · los cuatro estados | CAP-02 · el halo y la cuenta grande | CAP-03 · sello cumplido |
|---|---|---|
| ![CAP-01](snapshots/CAP-01-pildoras-tv.png) | ![CAP-02](snapshots/CAP-02-cuenta-grande-y-halo.png) | ![CAP-03](snapshots/CAP-03-sello-cumplido.png) |

| CAP-04 · sello no alcanzado y la píldora hueca | CAP-05 · `Pirámide 500`: 14 píldoras y el sello largo | CAP-06 · la última en curso, llena |
|---|---|---|
| ![CAP-04](snapshots/CAP-04-sello-no-alcanzado.png) | ![CAP-05](snapshots/CAP-05-sello-piramide.png) | ![CAP-06](snapshots/CAP-06-ultima-llena.png) |

| CAP-12 · la última por distancia, con la pista sola | CAP-22 · 200 pasos: la franja y el pulgar | CAP-27 · la pausa con el paso por terminar |
|---|---|---|
| ![CAP-12](snapshots/CAP-12-final-por-distancia.png) | ![CAP-22](snapshots/CAP-22-serie-larga.png) | ![CAP-27](snapshots/CAP-27-pausa-con-la-cuenta.png) |

En horizontal y en la ventana baja (CAP-25, CAP-26, CAP-29, CAP-30):

![CAP-25](snapshots/CAP-25-cuenta-grande-th.png)

![CAP-26](snapshots/CAP-26-sello-th.png)

| CAP-29 · la cuenta grande en 640 × 360 | CAP-30 · el sello en 640 × 360 |
|---|---|
| ![CAP-29](snapshots/CAP-29-cuenta-grande-l640.png) | ![CAP-30](snapshots/CAP-30-sello-l640.png) |

**La fila de información cuando no entra** (teléfono vertical, `Texto largo`):

| CAP-31 · la pastilla por fragmentos, con el `…` blanco | CAP-19 · la fila más larga, sin pastilla | CAP-20 · en inglés |
|---|---|---|
| ![CAP-31](snapshots/CAP-31-pastilla-por-fragmentos.png) | ![CAP-19](snapshots/CAP-19-fila-mas-larga.png) | ![CAP-20](snapshots/CAP-20-fila-mas-larga-en.png) |

**La barra con rutina en horizontal**: el teléfono, la ventana baja con SPM y Watts fuera, y la ventana de 1280 × 720, comparable con `docs/image-03.png` del legacy:

![CAP-23](snapshots/CAP-23-prueba-4a-th.png)

![CAP-21](snapshots/CAP-21-prueba-4a-l640.png)

![CAP-24](snapshots/CAP-24-prueba-4a-w1280.png)

**La barra del libre** (teléfono vertical):

| CAP-13 · `EN MARCHA` | CAP-14 · la pausa | CAP-15 · `FINALIZADO` mientras guarda | CAP-18 · en inglés |
|---|---|---|---|
| ![CAP-13](snapshots/CAP-13-barra-libre-tv.png) | ![CAP-14](snapshots/CAP-14-pausa-libre.png) | ![CAP-15](snapshots/CAP-15-finalizado-guardando.png) | ![CAP-18](snapshots/CAP-18-barra-libre-en.png) |

En CAP-13 a CAP-15, `Entrenamiento Libre` queda pegado al ícono del reloj (QA-V4b-R2-01, §7.1): se ajusta al empezar la It. 5.

En horizontal y en la ventana de 1280 × 720, con el pulsómetro:

![CAP-16](snapshots/CAP-16-barra-libre-th.png)

![CAP-17](snapshots/CAP-17-barra-libre-w1280.png)

**El diálogo de rutina completada** (`Corta` a los 32 s):

| CAP-07 · en español | CAP-08 · en inglés | CAP-28 · el cierre que falla: el aviso en Workout |
|---|---|---|
| ![CAP-07](snapshots/CAP-07-dialogo-completado.png) | ![CAP-08](snapshots/CAP-08-dialogo-completado-en.png) | ![CAP-28](snapshots/CAP-28-cierre-que-falla.png) |

![CAP-09](snapshots/CAP-09-dialogo-completado-th.png)

![CAP-11](snapshots/CAP-11-dialogo-completado-w1280.png)

![CAP-10](snapshots/CAP-10-dialogo-completado-l640-letra.png)

CAP-10 es la ventana baja con letra 1,3×. El contenido se desplaza dentro del diálogo y `Terminar` queda a la vista. Todavía no hay un indicio de que hay más para desplazar (O-01, §7.1).

**Videos** (30 cuadros por segundo; los valores de cada cuadro están en [manifest.json](snapshots/manifest.json)):

- [VID-01 · la entrada "sumergirse" (2,5 s)](snapshots/VID-01-entrada-sumergirse.mp4): el negro, el fondo, la cascada, el reloj que ya corre y la llave al final.
- [VID-02 · la salida con la última foto (2,5 s)](snapshots/VID-02-salida-ultima-foto.mp4): `00:12` y `FINALIZADO` hasta el final, nunca `00:00` ni `—`.
- [VID-03 · la entrada con rutina (2,5 s)](snapshots/VID-03-entrada-con-rutina.mp4): la barra de pasos llega primero.
- [VID-04 · las píldoras, la cuenta y el sello (18,5 s)](snapshots/VID-04-pildoras-cuenta-y-sello.mp4): el halo, `3` / `2` / `1` en el mismo cuadro que la barra, el destello, el borde que se dibuja y los dos sellos.
- [VID-05 · el fin y el diálogo (8 s)](snapshots/VID-05-fin-y-dialogo.mp4): la última píldora que se llena, el check, el conteo y la salida. La llave se va en el primer cuadro.
- [VID-06 · la pausa y el velo (5 s)](snapshots/VID-06-pausa-y-velo.mp4).
- [VID-07 · kcal que se desliza (6 s)](snapshots/VID-07-kcal-que-se-desliza.mp4).
- [VID-08 · la entrada con reducir movimiento (2 s)](snapshots/VID-08-entrada-reducir-movimiento.mp4): un fundido corto. El primer medio segundo muestra `0.0` y `--:--` hasta la primera lectura, como en la 4a.

### 3.7 Lo que podés verificar vos (opcional, pero recomendado)

1. **AC-WKT-02-11: el equipo suspendido** (5 minutos): §3.1 (c). Es el único criterio de la 4b que solo podés cerrar vos.
2. **De la It. 4a, la pantalla siempre encendida** (1 minuto): `powercfg /requests` en una consola de administrador ([REPORT de la 4a §3.7](../04a-motor-hud-funcional/REPORT.md)).
3. **De las It. 2 y 3**: el modo claro, la app legacy después de la nueva y los idiomas preferidos ([REPORT de la It. 3 §3.7](../03-rutinas/REPORT.md)).

---

## 4. Resultado de las pruebas

Son los números de la ronda 2 de QA ([05-qa-verification, "Ronda 2"](05-qa-verification.md)):

| Qué | Resultado |
|---|---|
| Compilación (dotnet) | **0 advertencias, 0 errores** |
| Pruebas dotnet | **Herramientas 182 / 182; el resto, 2319: 2292 pasan, 0 fallan, 27 ignoradas**. Al cerrar la 4a eran 2228 en total |
| Unity EditMode | **2368: 2337 pasan, 0 fallan, 31 ignoradas, 0 inconclusas** |
| Unity EditMode con simulador | **2550: 2519 pasan, 0 fallan, 31 ignoradas, 0 inconclusas**, corrida dos veces. El modo simulador quedó como estaba |
| Unity PlayMode (interfaz montada) | **354 de 354**; con simulador, **420 de 420**. En la 4a eran 264 y 328 |
| Las ignoradas | Son a propósito: necesitan un teléfono, una Mac, GitHub o la suspensión real del equipo |
| Matriz (597 IDs) | **595 pasan**, 0 fallan, 2 ignorados (AC-WKT-02-10, Android; AC-APP-15-03, Apple) y 0 sin prueba. AC-WKT-02-11 y AC-APP-15-01 figuran en PASA porque sus pruebas, con el ciclo de vida y la interfaz del sistema de prueba, pasan; la parte de equipo es NO VERIFICABLE (§5.1) |
| Caso legacy | **LT-46**, portado con sus 12 afirmaciones, en dotnet y en Unity |
| Paridad con el motor del legacy | La lente de paridad regeneró el oráculo desde el legacy y salió idéntico byte a byte. Pasan los 11 escenarios, los 16 guiones y la espera del diálogo después del fin (`CompletedHoldReset`). Con sondas propias se confirmaron Q-52 (la última píldora en curso) y Q-53 (nunca el diálogo a mano, ni con la base lenta y el remo mandando lecturas) |
| Rendimiento | El HUD no reserva memoria por cuadro: lo mide un registrador de Unity, y un control positivo de QA confirmó que esa medida ve las asignaciones. Con 200 píldoras y con 1998, el trabajo del HUD queda en 16 ms o menos por cuadro. El peor caso de la 4b queda dentro del tope de 60 elementos visibles. En el build, las ventanas con cambio de paso, halo, sello y cuenta grande dan como máximo 9,3 ms de CPU (p99 ≤ 1,4 ms). **La primera ventana de cada inmersivo da de 31 a 69 ms** (QA-V4b-06, §7.1) |
| Capturas | **51 tomas** (43 PNG y 8 MP4): **0 con problemas**; 150 de 150 sondas de aspecto dentro de tolerancia; la llave del simulador, auditada en todas. Iguales byte a byte en dos corridas seguidas. Las de las It. 2, 3 y 4a se regeneraron una sola vez, al empezar, y QA explicó cada diferencia (las cifras de tablero y el kerning). Las tomas del HUD de la 4a quedan reemplazadas por las de la 4b. QA y el Designer miraron cada foto y los videos cuadro por cuadro; el PO revisó las 43 fotos al cerrar |
| Prueba en la PC (build de simulador) | Se probó en dos rondas, unos 10 minutos de ventana, con Windows en inglés. **Funcionó todo esto**: el libre con su barra, la pausa, el teclado y la salida; `Corta` hasta el final, con el halo, el `3` grande, `Target met`, la última llena y el diálogo; un clic afuera y Escape, que no hacen nada; Tab y Enter en el diálogo; terminar a mano en el último paso, sin diálogo; la ventana minimizada con la rutina que termina, con el diálogo ya abierto al restaurar; el doble toque y los toques en el negro, sin efecto; la ventana, sin cambios en tamaño, posición ni estilo; y el proceso matado, que dejó la sesión cortada con sus puntos. En el registro: las barras ocultas y visibles en cada entrada y salida, **0 avisos de DPI** y ningún error |
| Builds de Windows (Mono) | El de simulador sale bien, en su carpeta de siempre. El normal (WP-B) salió sin rastros del simulador, de diagnósticos ni de pruebas |
| IL2CPP y Android | **No verificables**: faltan los módulos (`build.ps1` sale con 3, "NO VERIFICABLE") |
| Tus datos y los del legacy | Ninguna prueba, captura ni corrida en la PC usó tus bases: todas usaron una carpeta temporal. Tu `rower_app_dev.db` tiene la misma fecha y el mismo md5 antes y después. Las preferencias de la ventana del build, que las corridas cambian, se exportaron antes y se restauraron byte a byte. El repositorio legacy está intacto (`git status` vacío), y nadie leyó el archivo de credenciales de Strava |
| Repositorio público | 0 rutas de la PC en lo versionado y en lo nuevo |

---

## 5. Criterios de aceptación

| Área | Criterios | Cumplen | Con parte de equipo pendiente (entre los que cumplen) | Fallan | No verificables |
|---|---|---|---|---|---|
| WKT nuevos (inmersivo, barra de pasos, píldora, barra libre, tarjetas, reloj de pared, diálogo, sello y P-17) | 56 | 55 | 3 | 0 | 1 |
| APP nuevos (barras del sistema y catálogo) | 5 | 3 | 0 | 0 | 2 |
| Las dos condiciones de la 4a | 2 | 2 | 0 | 0 | 0 |
| De iteraciones anteriores, que cambian (WKT 15, APP 5, BLE 3) | 23 | 23 | 1 | 0 | 0 |
| De iteraciones anteriores, que se vuelven a verificar (WKT 14, HIS 1) | 15 | 15 | 1 | 0 | 0 |
| **Total** | **101** | **98** | **5** | **0** | **3** |

Más LT-46, que pasa. En la ronda 1 fallaban dos criterios, AC-BLE-16-02 (la llave quedaba sobre el negro al salir desde el diálogo) y AC-WKT-16-03 (el espaciado de letras del chip). Los dos se corrigieron, cada uno con una prueba que antes fallaba, y QA los volvió a verificar.

### 5.1 Los no verificables y por qué

| Criterio | Qué falta ver | Por qué no se pudo | Cuándo |
|---|---|---|---|
| AC-WKT-02-11 | Que, con Windows suspendido de verdad, el tiempo suspendido no cuente | QA no suspende tu equipo. En dotnet y PlayMode pasan sus 87 pruebas, en los dos casos del reloj del sistema, y el build registra los avisos de Windows (`[TIME] avisos de suspensión registrados (P-17)`) | Vos, con la demo (c) |
| AC-APP-15-01 | En Android, que las barras de estado y de navegación se vayan al entrar, se vuelvan a ir si las revelás con un gesto y vuelvan al salir | Faltan el módulo de Android y un teléfono. La regla, con la interfaz del sistema de prueba, pasa | Con el módulo y el teléfono; a más tardar en la 6a |
| AC-APP-15-03 | La barra de estado de iOS y la ventana de macOS | [Apple] | It. 10b |

### 5.2 Cumplen, pero falta una parte de equipo

Pasan en el Editor y en la PC. La parte que se nombra queda no verificable, y si falla al verla, el criterio se reabre.

| Criterio | Parte pendiente | Quién y cuándo |
|---|---|---|
| AC-WKT-13-07, AC-WKT-13-03 | "Atrás" de Android abre la confirmación de terminar, desde el primer cuadro de la entrada | El teléfono |
| AC-WKT-21-02 | "Atrás" de Android no cierra el diálogo de completado. En la PC, Escape y el clic afuera no hacen nada | El teléfono |
| AC-WKT-02-08 | En Android, que el primer cuadro al volver del segundo plano ya muestre el presente | El teléfono |
| AC-WKT-14-19 | `Serie larga` en el build: la base que se siembra para el build no la trae. En el build se midió con `Corta` (≤ 9,3 ms fuera del primer cuadro) y, en PlayMode, con 200 y 1998 pasos | Cuando se pruebe en el teléfono (6a) |

### 5.3 Cumplen, y se completan más adelante

| Criterio | Qué falta | Cuándo |
|---|---|---|
| AC-WKT-13-06, AC-WKT-13-12 | La entrada sin el vuelo de cámara; el velo sin desaturar la escena ni el bote que frena | It. 6a |
| AC-WKT-17-01 (y el diálogo de completado) | El vidrio sin desenfoque | It. 6b |
| AC-WKT-19-03 | La hora real que conduce la luz de la escena | It. 6a |
| AC-WKT-21-10 | El diálogo con Strava: dos botones y la subida | It. 12 |
| AC-WKT-06-10 y las sesiones de la demo | Se ven en el Historial | It. 9 |
| F-WKT-07, F-WKT-08, F-WKT-18 | Los paneles de parciales y repeticiones | **It. 5** |
| F-APP-15 | El cierre: orientación por plataforma y ventana de escritorio | It. 14 |

---

## 6. Desvíos aceptados

### 6.1 Diferencias visibles con el legacy, a propósito

Ninguna cambia un número ni una regla del motor. DV-01 a DV-39 ya las aceptaste en las It. 2 a 4a. Si alguna de estas no te gusta, decilo (§8).

| ID | Qué cambia | Por qué |
|---|---|---|
| DV-40 | Con muchos pasos, nunca desaparecen. La separación se achica y, en el extremo, la fila es una franja continua donde cada tramo conserva su color y su estado. Con píldoras de menos de 6 base (en un teléfono, desde 50 pasos), la en curso es un **pulgar** más alto y más ancho, y la hueca lleva dos rieles. En el legacy, con más de ≈ 120 pasos en un teléfono, cada píldora mide 0 y la fila queda vacía (CAP-22) | El editor deja guardar hasta 999 repeticiones |
| DV-41 | Al salir, el HUD conserva la última foto de la sesión. El legacy se redibuja en reposo mientras sale: la banda en `00:00`, la barra de una rutina pasa a ser la del libre y el chip dice `—` (VID-02) | Defecto visual (O-01 de la 4a) |
| DV-42 | `Terminar` del diálogo espera el cierre de la sesión, con un indicador si tarda más de 300 ms; si el cierre falla, Workout avisa `No se pudo guardar la sesión`. En el legacy, un toque antes de que termine el guardado deja la sesión sin hora de fin ni totales, sin aviso | Q-13 |
| DV-43 | Los totales del diálogo son siempre los del instante en que terminó la rutina, también si terminó en segundo plano. El legacy los lee al armar el diálogo, un cuadro después; en primer plano se ve lo mismo, y cambia solo en Android, si la rutina terminó en segundo plano | El resumen es el de la sesión que se guardó |
| DV-44 | `FINALIZADO` en `#FF7A92`, la variante legible del mismo matiz. El legacy usa `#EF476F`, que no llega al contraste del texto chico | D-ART-01 |
| DV-45 | La pastilla de objetivo puede usar todo el espacio libre y se corta por **fragmentos enteros**, con un `…` blanco (`>150W · 24 spm…`). Antes de cortarla, se achican los aires de la fila; si ni así entra su primer fragmento, no se muestra. En el legacy recibe a lo sumo la mitad del espacio, se corta por carácter (`>15…` se leería "más de 15") y nunca se oculta. Los objetivos siguen en las tarjetas (`obj.`) (CAP-31, CAP-19) | Un número cortado informa mal |
| DV-46 | La cuenta y el reloj van siempre pegados al borde derecho y no saltan cuando entra `Próximo`. En el legacy, con pastilla, quedan corridos hacia el centro y se mueven 43 dp cuando entra `Up next` (CAP-24) | Nada salta |
| DV-47 | El título de la barra libre se achica hasta 0,8 (0,805 en un teléfono en español) y, solo si ni así entra, se recorta con `…`. El del diálogo se parte en dos líneas junto al check. En el legacy los dos se salen de su caja: en 390 y 360 dp, el título libre empuja afuera el reloj y el chip | Nada sale de su caja |

**Del sistema visual** (informativas, no cambian datos ni textos):

- el halo va solo por fuera de la píldora (en el legacy también tiñe la pista y pasa por debajo de la fila de información);
- el chip va en peso 700 con su espaciado de letras, de ancho fijo para que el reloj no se corra;
- el relleno de la en curso lleva un brillo fino arriba;
- el check del diálogo mide 32 y se dibuja;
- los valores del diálogo usan las cifras de tablero.

Detalle en [01-po-spec §11.1.1](01-po-spec.md).

### 6.2 Decisiones técnicas con efecto que se puede notar

| ID | Qué | Efecto para vos |
|---|---|---|
| DT4b-10 | Los controles responden desde que empieza su tramo de la cascada, a los 0,84 s. "Atrás" responde desde el primer cuadro | Un toque en `Pausa` en el primer instante no hace nada (P-15: "desde que empiezan a aparecer"). Con reducir movimiento, responden desde el cuadro 0 |
| V1-10 | Mientras la salida está en negro, de 0 a 400 ms, los toques no llegan a Workout | Un doble toque en `Terminar` del diálogo no inicia otra rutina por debajo. Desde que Workout se ve, el toque vale |
| DT4b-06, DT4b-20 | P-17 usa dos señales de Windows: el aviso de suspensión y la cuenta de sueño del sistema. Si no llega ninguna, el lapso cuenta como un cuadro tarde, igual que en la 4a, y el registro lo dice. En el Editor el aviso no se registra | La demo (c) se hace con el build |
| DT4b-09 | El tope de 15 s para el cierre cuenta desde el fin de la rutina, no desde `Terminar`. Un cierre tardío que vuelve antes del toque cuenta como guardado | Nunca un indicador eterno ni un error falso |
| DT4b-11 | El chip de fase tiene un ancho fijo por idioma | El reloj no se corre al pausar |
| DT4b-04 | La cuenta grande lleva la cifra de SPM en un disco con un aro que se vacía | Se lee de lejos sin tapar el bote |
| DT4b-16 | Los valores del diálogo son de 20, con las cifras de tablero, y crecen con la letra del sistema | Con letra 1,3× en la ventana baja, el diálogo se desplaza |
| DT4b-21 | Con el tope del editor (1998 pasos), la franja se dibuja por columnas de píxel con el color promedio | La fila cuesta lo mismo con 200 que con 1998 pasos |
| V1-02 | El espaciado de letras del chip y de `PAUSA` es 0,08 em, el de las notas del Designer; el legacy usa 0,073 | Prácticamente igual a la vista |
| B-06 | Los avisos del final aparecen cuando Workout empieza a verse, nunca sobre el negro ni con el diálogo abierto | `No se pudo guardar la sesión` sale al volver a Workout |
| **QA-V4b-06** (desvío) | El primer cuadro de cada inmersivo tarda de 31 a 69 ms de CPU, porque arma todo el HUD en el cuadro del toque | Un tirón posible justo al empezar el fundido a negro. No toca datos: la sesión y el reloj corren desde el toque. Se resuelve al empezar la It. 5 |

También se aplicaron DT4b-01, -02, -03, -05, -07, -08, -13, -14 y -17 a -19, sin efecto visible o ya incluidas en los criterios. Detalle en [02-tech-design §8.1](02-tech-design.md).

---

## 7. Issues abiertos y deuda

### 7.1 Issues de QA (todos menores)

En la ronda 2 se cerraron, con evidencia, los dos FAIL de la ronda 1 y otros 10 menores:

- **QA-V4b-01**: la llave sobre el negro.
- **QA-V4b-02 = QA-V4b-P01**: el espaciado de letras del chip y de `PAUSA`.
- **QA-V4b-03**: la pista del pulgar, que dejaba ver la franja.
- **QA-V4b-04**: un píxel suelto en el relleno.
- **QA-V4b-05**: seis pruebas que faltaban.
- **QA-V4b-07**: valores de los videos sin `NaN`.
- **QA-V4b-P02**: la última píldora, que podía quedar al 99 %.
- **QA-V4b-MOT-01**: el conteo del diálogo, que asignaba memoria.
- **QA-V4b-MOT-02**: los toques en el negro.
- **QA-V4b-MOT-03**: el punto en vivo con reducir movimiento.
- **QA-V4b-MOT-04**: el registro de la vuelta del segundo plano.
- **QA-V4b-MOT-05**: la prueba de las cinco salidas.

Quedan 4:

| ID | Qué pasa, en simple | Impacto | Quién | Cuándo |
|---|---|---|---|---|
| QA-V4b-06 | El primer cuadro de cada inmersivo tarda de 31 a 69 ms de CPU (en el build: 47,7, 31,5, 69,0 y 69,1). Arma el HUD en el cuadro del toque | Un tirón posible al empezar "sumergirse". Las demás ventanas, ≤ 9,3 ms. **Desvío aceptado** por el PO | Architect | **Al empezar la It. 5**: armar el HUD en un cuadro del negro o tenerlo listo al abrir Workout, y medirlo en el build |
| QA-V4b-R2-01 | En un teléfono vertical en español, `Entrenamiento Libre` queda a ≈ 4 dp del ícono del reloj, menos que el espacio entre sus dos palabras: se lee `Libre ⏱ 10:00` como un grupo (CAP-13 a CAP-15) | Aspecto. AC-WKT-16-06 se cumple (nada se encima ni se corta), pero la barra pierde jerarquía | Designer, con el Architect | **Al empezar la It. 5**, sin bajar el título de 0,8 (por ejemplo, el relleno del chip de 8 a 6), con CAP-13 a CAP-15 regeneradas |
| QA-V4b-R2-02 | La spec decía "≈ 0,82" para el título libre, y el valor es 0,805 | Documento | PO | **Hecho** al cerrar ([01-po-spec §17](01-po-spec.md)) |
| QA-V4b-R2-03 | Unity a veces revienta al cerrar un `-batchmode` que cambió el modo simulador. Los *scripts* siempre lo restauran, pero la corrida queda marcada como fallida o el arnés no corre | Solo las herramientas: hay que repetir la corrida | Architect | **Al empezar la It. 5** (esperar la compilación o reintentar una vez, y anotarlo en `tools/unity/README.md`) |

**Observaciones, sin issue:**

- **O-01** (QA): en CAP-10, el diálogo a 1,3× en la ventana baja se desplaza sin un indicio de que hay más. El criterio lo permite. → el Designer, en la It. 12, cuando el diálogo sume los botones de Strava.
- **O-03** (QA): en VID-08 el HUD muestra `0.0` y `--:--` durante el primer medio segundo, hasta la primera lectura del remo. Es la conducta de la 4a. → se acepta.
- **O-R2-01** (QA): Unity deja un `TestResults.xml` en la carpeta de datos del Editor en cada PlayMode. No es una base. → el Architect, si se quiere esa carpeta limpia.
- **O-2** (lente de motion): con el diálogo de completado abierto, la app sigue a 60 cuadros por segundo. → se decide en la 6a, con el teléfono.
- **O-4 y O-5** (lente de motion): una excepción en una animación o al armar el diálogo podría trabar la salida o dejar la sesión terminada sin volver a reposo. No se encontró ningún camino que falle. → endurecimiento del Architect al empezar la It. 5, junto con QA-V4b-06.
- **O-1** (lente de motion): dos resortes de la 4a siguen unos cientos de ms después de salir, sin efecto. → prolijidad, con QA-V4b-06.
- **O-P01 y O-P02** (lente de paridad): dos comentarios viejos en las pruebas del oráculo y una afirmación posible durante la espera del diálogo. → prolijidad. **O-P03** quedó resuelta con QA-V4b-02.

### 7.2 Otra deuda

- **Para la It. 5**:
  - **El presupuesto del HUD** (R4a-03): el peor caso de la 4b queda en el tope de 60 elementos visibles. Los paneles de parciales y repeticiones tienen que entrar juntando elementos o con un tope nuevo medido.
  - **Los paneles de series expandidos y la cuenta grande**: en un teléfono vertical, colapsados entran; expandidos se cruzan con el disco de la cuenta grande. Por defecto, los paneles expandidos son obstáculo y, si no entra, la cuenta grande no se muestra esa vez. Lo decide la spec de la 5 ([00-deuda-diseno §4.8](00-deuda-diseno.md)).
  - La cascada de la entrada suma los paneles, junto con SPM.
- **Para la 6a**:
  - la fila con 200 píldoras en el teléfono (R4b-02);
  - los 60 cuadros por segundo con el diálogo abierto (O-2);
  - el velo de pausa pasa a la escena y el negro de la entrada, al vuelo de cámara.
- **Para la It. 7**: en la ventana baja, la llave queda dentro de la zona del bote (de la 4a).
- **Para la 10a**:
  - QA-V4a-L01;
  - el reloj de Android con la pantalla bloqueada (RA-19) y que Android cierre la app en segundo plano (RA-08);
  - un cuadro tarde con el remo real;
  - la variante completa de ROB-04;
  - el *lock* para los avisos del Bluetooth real;
  - las barras de Android y sus márgenes (R4b-05);
  - el aviso de suspensión con IL2CPP (R4b-06).
- **Para la It. 12**: Strava en el diálogo de completado y el indicio de desplazamiento de O-01.
- **Para la It. 14**: los lectores de pantalla, la escala de Windows con dos monitores, la pantalla encendida solo donde hace falta, la háptica y el cierre de F-APP-15.
- **Deseables, sin criterio** (P2, de la It. 3): `Guardar` con un indicador si tarda más de 300 ms; un subtítulo que pasa a dos líneas crece animado; la mini-barra interpola sus tramos.
- **Con teléfono**:
  - de la It. 3: el teclado del sistema y el menú de texto;
  - de la 4a: "Atrás", el segundo plano, el primer cuadro al volver y la pantalla encendida;
  - de la 4b: las barras del sistema ocultas y "Atrás" con el diálogo.
- **CI**: `ci-core` corre en GitHub; `ci-unity` espera la licencia (P-08).

### 7.3 Riesgos

| ID | Estado |
|---|---|
| R4b-01 (P-17 con el modo de espera moderno de Windows) | **Abierto** hasta tu demo (c). Si falla en tu PC, se suma otra señal del sistema |
| R4b-05 (las barras de Android) y R4b-06 (el aviso de suspensión con IL2CPP) | **Abiertos** hasta que estén los módulos y el teléfono |
| R4a-03 (el presupuesto de elementos del HUD) | **Abierto**, ahora para la It. 5 |
| R4b-02 (la fila de 200 píldoras en el teléfono) | **Abierto** para la 6a. El corte previsto suma un solo elemento |
| R3-01 / R-T6 (IL2CPP con el SQLite nativo), R3-02 y R3-13 (Android), R4a-01, R4a-10 / RA-08 y R4a-12 / RA-19 | **Abiertos**, como en la 4a |
| R4b-10 (la salida más larga, con dos inmersivos a la vez) | **Mitigado y probado**; los toques en el negro ya no llegan a Workout |
| R4b-04 y R4b-12 (las piezas de UI Toolkit y el presupuesto sin ellas) | **Cerrados**: se probaron en el proyecto |
| R4b-07 (regenerar el oráculo) | **Cerrado**: la lente lo regeneró desde el legacy y salió idéntico |
| R4b-09 (medidas de texto aproximadas) | **Cerrado**: se midieron en Unity |
| R4b-11 (los controles durante la entrada) | **Cerrado**: es lo que pide P-15 |

---

## 8. Decisiones que tenés que confirmar

Si no decís nada, se aplica lo que dice "Por defecto". Alcanza con un "OK a todo", o con decir cuál querés cambiar. Las decisiones de los gates de las It. 1 a 4a ya las aceptaste, P-17 incluida.

### 8.1 Gate de la It. 4b

| Decisión | Qué significa | Por defecto |
|---|---|---|
| **D-ANI-05 / P-01** | La cuenta grande `3`, `2`, `1` en los últimos 3 s de cada paso por tiempo (no en pausa, ni en pasos por distancia, ni en el libre) y el sello de 2 s `Objetivo cumplido` / `No alcanzado`, con el promedio contra el objetivo, al cerrar cada paso con objetivo (CAP-02, CAP-03, CAP-04, DEMO-06, DEMO-07, DEMO-09) | **Se adopta**. Si no: queda la cuenta de la barra, como hoy, y el resultado se ve solo en la píldora |
| **DV-40 / P-05** | Con muchos pasos, nunca desaparecen: franja y pulgar (CAP-22) | Se mantiene |
| **DV-41 / P-10** | La salida conserva la última foto: nunca `00:00` ni `—` (VID-02) | Se mantiene |
| **DV-42 / P-11** | `Terminar` del diálogo espera el cierre de la sesión y, si falla, avisa | Se mantiene |
| **DV-43 / P-13** | Los totales del diálogo, siempre los del fin | Se mantiene |
| **DV-44 a DV-47** | `FINALIZADO` en `#FF7A92`; la pastilla por fragmentos con `…` blanco; la cuenta y el reloj pegados a la derecha; el título libre que se achica hasta 0,8 y el del diálogo en dos líneas (§6.1) | Se mantienen |
| **AC-WKT-02-11** (P-17, verificación) | Hacé la demo (c) y contanos qué marcó el reloj al despertar y cuántos puntos quedaron | Si el reloj salta, el criterio se reabre y se corrige al empezar la It. 5, antes de construir encima |

### 8.2 Decididas por el PO al cerrar (no necesitan tu respuesta)

- **QA-V4b-06**: se acepta como desvío hasta el comienzo de la It. 5. Es un tirón posible al empezar el fundido a negro, sin efecto en ningún dato. Lo corrige el Architect antes de construir encima, con la medida del build.
- **QA-V4b-R2-01**: la barra libre en un teléfono en español se ajusta al empezar la It. 5 (Designer). AC-WKT-16-06 se cumple hoy, pero la jerarquía título / reloj y chip importa.
- **QA-V4b-R2-02**: hecho. La spec dice ≈ 0,8 (0,805).
- **QA-V4b-R2-03**: el Architect lo resuelve en las herramientas, al empezar la 5.
- **Las observaciones**: O-01 en la It. 12; O-03 se acepta; O-2 en la 6a; O-4, O-5 y O-1 al empezar la 5; O-P01 y O-P02 por prolijidad (§7.1).
- **P-02 a P-16 de la spec**: quedan como estaban por defecto. Entre ellas: el último paso sin sello (P-02); en pausa, `PAUSA` en lugar de la cuenta grande (P-04); el diálogo opaco hasta la 6b (P-08); ningún aviso con el diálogo abierto (P-16).
- **Pendientes del PO de la spec (§15)**: hechos.
  - En el ROADMAP, P-17 aceptada y "También toca" de la 4b al día.
  - En los inventarios `workout-engine-hud` y `app-shell-platform`, las precisiones del legacy (F-WKT-13, -14, -16, -21 y F-APP-15).
  - En el STATUS, el estado al día.
  - El detalle está en [01-po-spec §17](01-po-spec.md).

### 8.3 Opcional, sin urgencia

- **Licencia de Unity (P-08)**: ¿la cargás como secreto del repositorio? Hace falta para que corra `ci-unity`.
- **CAP-27 de la It. 3** (la ventana del build normal con la barra de título oscura): sigue disponible a pedido.

---

## 9. Prerrequisitos para la It. 5 (series en vivo: parciales de 500 m y repeticiones)

### 9.1 De tu lado

| Qué | Para qué | ¿Obligatorio? |
|---|---|---|
| Tu OK a §8.1 | Cerrar el gate | Sí (alcanza con "OK a todo") |
| La demo (c) de §3.1: suspender el equipo con un entrenamiento en marcha (5 minutos) | Cerrar AC-WKT-02-11; si falla, se corrige al empezar la 5 | Recomendado |
| Los 10 minutos de §3.1 (a) y (b) | Ver la iteración en la ventana real | Recomendado |
| Módulos **Windows Build Support (IL2CPP)** y **Android Build Support**, y un Android 8.0+ de 64 bits con depuración USB | Cerrar AC-APP-15-01, las partes Android y los NV de IL2CPP | No para la 5. El teléfono hace falta a más tardar en la 6a |
| Licencia de Unity como secreto (P-08) | Que corra `ci-unity` | No |

La It. 5 no necesita nada nuevo de tu lado: se prueba con el simulador.

### 9.2 Del equipo, al empezar la It. 5

1. **Architect**, antes de construir encima:
   - QA-V4b-06: armar el HUD fuera del cuadro del toque y medir la entrada en el build;
   - QA-V4b-R2-03, en las herramientas;
   - el endurecimiento de O-4 y O-5, y O-1;
   - si tu demo (c) falla: AC-WKT-02-11, con otra señal del sistema.
2. **Designer**:
   - QA-V4b-R2-01, la barra libre en un teléfono en español, con CAP-13 a CAP-15 regeneradas en dos corridas;
   - en el diseño de la 5: los paneles de vidrio colapsables, que no tapen el bote ni el lugar de la cuenta grande; la entrada de cada vuelta nueva; los paneles en la cascada.
3. **PO**: escribir la spec de la 5 (`iterations/05-series-parciales/01-po-spec.md`), con:
   - F-WKT-07 (parciales de 500 m: la vuelta en curso y las 4 últimas), F-WKT-08 (repeticiones de la serie y la comparación) y F-WKT-18 (paneles colapsables que recuerdan su estado en `immersive.lapsExpanded` e `immersive.repsExpanded`);
   - los 15 casos legacy: `series_tracker_test` (12) y los 3 que quedan de `series_panels_test`;
   - el saneamiento de la distancia compartido con el paso por distancia (Q-55, DV-26);
   - los paneles expandidos contra el lugar de la cuenta grande (por defecto, la cuenta grande no se muestra esa vez si no entra);
   - el presupuesto de elementos del HUD con los paneles (R4a-03);
   - los textos de los paneles en es y en (Q-01), con el español idéntico al legacy.
4. **QA**: verificar la deuda de arriba antes de construir encima, y la demo (c) si la contestaste.

---

## 10. Por qué el PO acepta la It. 4b

- **El objetivo se cumple.**
  - El HUD de la 4a pasa al nivel EXR. Llegan las píldoras con sus cuatro estados y el relleno continuo, el halo, la cuenta grande y el sello, el reloj de pared, la barra del libre con su chip y el diálogo de completado, que espera al remero.
  - Llegan también la entrada "sumergirse", la salida sin `00:00`, el velo de pausa y la regla de las barras del sistema.
  - La demo de la spec se ve toma por toma en DEMO-01 a DEMO-12 y en los 8 videos, con la interfaz real. QA la repitió en la PC con el build de simulador, en dos rondas.
- **La paridad está probada contra el legacy.**
  - Las reglas de las píldoras, el último paso en curso (Q-52), el diálogo una vez y nunca a mano (Q-53), el mismo ancho (Q-59), el chip, la barra libre y el reloj coinciden con el código del legacy.
  - LT-46 está portado con sus 12 afirmaciones.
  - El oráculo del motor, regenerado desde el legacy, da el mismo archivo byte a byte y ahora compara también lo que pasa después del fin.
  - Cada una de las 56 filas de la checklist de paridad tiene su criterio.
- **Nada de la presentación toca el dato.** El motor da la misma secuencia con el HUD, sin él y con reducir movimiento. El reloj de la banda, la cuenta y la cuenta grande cambian en el mismo cuadro, y el relleno nunca adelanta al motor.
- **La base es confiable.**
  - Las mismas pruebas pasan en .NET y dentro de Unity, con simulador y sin él, con la interfaz montada (420 pruebas).
  - Las capturas son deterministas.
  - Los dos FAIL de la ronda 1 se corrigieron, cada uno con una prueba que antes fallaba.
  - QA encontró que la medida de "0 bytes por cuadro" no medía nada en Unity. Se reemplazó por una que sí mide, y QA lo confirmó con un control positivo.
  - Tus datos y los del legacy no se tocaron.
- **Lo pendiente es menor y tiene fecha.**
  - Ningún criterio falla.
  - Los 3 no verificables dependen de un teléfono, de una Mac o de tu PC suspendida.
  - Los 4 menores no cambian un dato. Uno ya está hecho, y los otros tres, y el desvío del primer cuadro, se resuelven al empezar la It. 5, antes de construir encima.
- **Lo único que falta es tu OK** a §8.1 y, si podés, los 5 minutos de la demo (c).
