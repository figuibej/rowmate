# It. 4a — Entrenar: motor de sesión, guardado incremental, reposo y HUD funcional: reporte de cierre

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-29 |
| Estado | **Cerrada, espera tu OK** |
| Veredicto del PO | **ACEPTADA, con dos condiciones.** Desde Workout arrancás un entrenamiento libre o una de tus rutinas y remás con un HUD que funciona de punta a punta: las métricas grandes, el semáforo de objetivos, el paso en curso con su cuenta regresiva y el aviso del que viene, y pausa, reanudar y terminar con confirmación. La sesión y sus puntos se guardan mientras remás, así que ni un cierre forzado la pierde. El motor da los mismos números que el del legacy. Fallan 2 criterios menores, y esas son las condiciones: se corrigen al empezar la It. 4b, antes de construir encima. Son el divisor de la banda del HUD, que no se ve en el teléfono (QA-V4a-03, AC-WKT-17-04), y el primer Tab después de usar el mouse, que enfoca sin mostrar el anillo (QA-V4a-04, AC-APP-05-07). Hasta entonces, esos dos criterios quedan reabiertos |
| Veredicto de QA | **APROBADO CON OBSERVACIONES** en la ronda 2 ([05-qa-verification.md](05-qa-verification.md)): 0 problemas graves, 0 mayores y 8 menores. Los dos mayores de la ronda 1 se corrigieron y se verificaron otra vez: una sesión que podía quedar sin HUD y dos animaciones que no paraban. Las dos lentes, [paridad](05-qa-lens-paridad.md) y [robustez](05-qa-lens-robustez.md), también dieron APROBADO CON OBSERVACIONES |
| Documentos | [01-po-spec](01-po-spec.md) (qué se pidió) · [02-tech-design](02-tech-design.md) (cómo se hizo) · [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) (control previo) · [04-design-notes](04-design-notes.md) (aspecto y movimiento) · [05-qa-verification](05-qa-verification.md) (verificación final, en dos rondas), con sus lentes [05-qa-lens-paridad](05-qa-lens-paridad.md) y [05-qa-lens-robustez](05-qa-lens-robustez.md) · [00-deuda-it3](00-deuda-it3.md) y [00-deuda-diseno](00-deuda-diseno.md) (la deuda de la It. 3) · [snapshots/INDEX.md](snapshots/INDEX.md) (capturas) |

---

## 1. En pocas palabras

La It. 4a convierte **Workout** en una pestaña de verdad y trae el entrenamiento:

- **Workout en reposo.**
  - Arriba, `Entrenamiento libre`; debajo, tus rutinas, con la misma tarjeta de Rutinas (nombre, descripción, resumen y mini-barra) y un *chevron*.
  - Sin remo, el aviso naranja `Conectá el rower para iniciar un entrenamiento` y el inicio apagado.
  - Tocar una rutina abre `¿Iniciar el entrenamiento?` con su resumen.
  - Una rutina dañada a mano, por ejemplo con una serie ×2 000 000 000, avisa `Esta rutina no se puede iniciar: revisá sus pasos en Rutinas` en lugar de colgar la app.
- **El motor.** Es el del legacy:
  - cuatro fases y un reloj de 1 s;
  - la rutina expandida, con sus progresiones;
  - pasos por tiempo y por distancia;
  - "por terminar" de 1 a 5 s y "a continuación" de 0 a 15 s;
  - el cumplimiento de objetivos de cada paso;
  - el pulso del sensor, que manda sobre el del monitor;
  - un punto cada 5 s y el cierre con el punto final y el P99.

  QA lo comparó con el motor del legacy, corrido fuera del legacy: en los mismos escenarios da los mismos puntos y los mismos totales, salvo dos diferencias conocidas (§4).
- **El HUD funcional.**
  - SPM grande arriba a la izquierda; Split y Watts arriba a la derecha.
  - Abajo a la izquierda, la banda con el reloj y la distancia; abajo a la derecha, kcal y BPM; abajo al centro, `Terminar` y `Pausa`.
  - Con rutina, arriba va el paso: el tipo en su color, la pastilla con los objetivos, `Próximo: …` y la cuenta regresiva.
  - Todo va en vidrio oscuro sobre un fondo provisorio de dos tonos, el cielo y el agua, hasta que llegue la escena en la It. 6a.
- **El semáforo.** Fuera de objetivo, el número pasa a rojo con una flecha (▲ subí, ▼ bajá) y debajo se ve cuánto pide (`obj. 160`). Se evalúa en cada lectura, sin margen, como el legacy.
- **Guardado incremental.**
  - La sesión se crea al tocar y cada punto queda guardado en la base enseguida.
  - Si la app se cierra o se mata a mitad de una sesión, la sesión queda "cortada", con todos sus puntos.
  - Una sesión terminada normalmente queda idéntica a la del legacy, punto por punto.
- **Segundo plano y pantalla.**
  - Minimizada, la sesión sigue: el reloj, los pasos y los puntos avanzan como si estuviera adelante.
  - Mientras la app está abierta, la pantalla no se apaga y el equipo no se suspende.

Se comporta como el legacy:

- los mismos textos en español y en inglés, salvo `Próximo:` (DV-24);
- los mismos formatos: 84 casos de borde sin una sola diferencia;
- las mismas reglas de pasos, objetivos, semáforo y guardado;
- los 11 tests del legacy de esta área, portados uno a uno.

Lo que el legacy hace mal y ya estaba decidido corregir, quedó corregido (§6):

- dos toques rápidos crean una sola sesión (DV-30);
- un error de la base se avisa, y el entrenamiento sigue (DV-29);
- un monitor que se reinicia ya no traba un paso por distancia (DV-26);
- la sesión no pierde segundos si la app estuvo trabada o minimizada (DV-27);
- un paso sin meta muestra `?`, no `nullm` (DV-25), y uno de duración negativa ya no hace fallar la pantalla (DV-37);
- en un teléfono horizontal, las tarjetas ya no se enciman (DV-33).

Encima se sumaron las mejoras EXR pedidas:

- **Números de tablero**, grandes y de ancho fijo, que cambian con un rodillo corto y no bailan.
- **El objetivo, a la vista.** Al salir de objetivo, el número da un solo "empujón", como mucho cada 4 s.
- **El paso que viene.** En los últimos 15 s, `Próximo` late suave; en los últimos 5, la cuenta crece y toma el color del paso.
- **El pulso como signo vital.** El corazón de BPM late a tu pulso.
- **Empezar es inmediato.** Un toque y el reloj corre.
- **Respeto por quien lo pide.** Con "reducir movimiento" hay fundidos y realces fijos, y con letra grande la vista de reposo y los diálogos entran.

Además:

- **La deuda de la It. 3** se resolvió al empezar: las 10 observaciones. La llave del simulador se mudó abajo a la izquierda en toda la app, para no tapar acciones (D-ART-10).
- Hay **2228 pruebas de la lógica** (eran 1599), que también corren dentro de Unity, y **328 pruebas de la interfaz montada** con el simulador (eran 235).
- Las **46 capturas y los 3 videos** de esta iteración los genera Unity solo, con la interfaz real. Incluyen la demo toma por toma, y si se vuelven a generar salen idénticos byte a byte.
- Ninguna prueba ni captura tocó tus bases ni las del legacy.

Lo que no se pudo ver:

- **Android**: faltan el módulo y el teléfono. Quedan sin ver el segundo plano con otra app o con la pantalla bloqueada, y "Atrás".
- **La pantalla siempre encendida, mirada con la herramienta de Windows**: `powercfg /requests` pide una consola de administrador. Las pruebas que se lo piden de verdad al sistema pasan, y vos lo podés ver en 1 minuto (§3.7).

---

## 2. Qué se entregó, por funcionalidad

"Cumple" quiere decir que pasa todo lo que se pudo verificar. "Con parte pendiente" quiere decir que cumple en el Editor y en la PC, pero falta verlo en Android (§5.3). "No verificable" está en §5.2, y "falla", en §5.1.

| ID | Qué quedó listo | Criterios | Dónde se ve |
|---|---|---|---|
| F-WKT-01 | **Ciclo de vida y fases**. Reposo, en marcha, en pausa y terminada; las lecturas llegan en todas. Al iniciar, libre o con rutina, la sesión se crea en el acto y el reloj arranca a más tardar 100 ms después del toque. Pausar congela los relojes, el muestreo y los objetivos; reanudar pierde la fracción de segundo, como el legacy (Q-58). Terminar guarda y vuelve a reposo. Un toque, una sesión (DV-30). Si la base falla al crear la sesión, el entrenamiento anda igual y avisa (DV-29) | 11 de 11 | El HUD y Workout |
| F-WKT-02 | **Reloj activo**. Un segundo por cada segundo en marcha, en el orden del legacy. El HUD muestra el reloj de la app, no el del monitor. No se pierde ni se duplica un segundo aunque un cuadro llegue tarde (DV-27). Minimizada, la sesión sigue igual; si la rutina termina mientras tanto, al volver se ve el aviso una sola vez | 9 de 10 (1 con parte pendiente: Android); 1 no verificable (Android, AC-WKT-02-10) | La banda del HUD; al minimizar la ventana |
| F-WKT-03 | **Avance de pasos y fin automático**. La rutina expandida con sus progresiones (`Prueba 4a` da 6 pasos). Pasos por tiempo y por distancia, estos con el saneamiento de los parciales (DV-26). Al terminar el último paso, se guarda y vuelve Workout con `¡Entrenamiento completado!`, un aviso provisorio hasta el diálogo de la 4b. El paso sin meta (`?`), el de 0 s y el de duración negativa (DV-25, DV-37). El libre solo termina a mano. Si el remo se cae, la sesión sigue | 10 de 10 | La fila del paso |
| F-WKT-04 | **Estado del paso**. Restante, progreso, "por terminar" de 1 a 5 s y "a continuación" de 0 a 15 s, con los ejemplos de la spec | 7 de 7 | `Próximo` y la cuenta regresiva |
| F-WKT-05 (+ F-HRM-14) | **Fusión del pulso**. El sensor manda sobre el monitor en todas las fases, también en los puntos guardados. Los 4 tests del legacy, portados. Con el pulsómetro simulado, la tarjeta BPM aparece, y desaparece mientras dura la caída simulada | 6 de 6 | Tarjeta BPM |
| F-WKT-06 (+ F-HIS-01, -02, -03) | **Telemetría y guardado**. La sesión se da de alta al iniciar. Un punto cada 5 s en marcha, con los contadores del monitor (Q-15) y el pulso fusionado; el punto es del paso que termina en ese segundo, y en pausa no se toma ninguno. **Cada punto queda guardado a más tardar 1 s después de tomarse** (Q-12). El cierre calcula los totales y el P99 sobre todos los puntos, igual que el legacy: en `Corta`, los dos últimos puntos son iguales, como allá. Una sesión cortada queda con sus puntos y sin hora de fin. Los errores se avisan una vez, y una sesión cerrada siempre tiene todos sus puntos. Con la base lenta, el HUD no se frena. Al minimizar se guarda todo lo pendiente | 15 de 15 | La base, con la herramienta de la demo (§3.1 c) |
| F-WKT-09 | **Cumplimiento de objetivos por paso**. Promedios de vatios, SPM y split, sin contar las lecturas paradas. Los 6 tests del legacy, portados. Se calcula, pero todavía no se ve: lo muestran las píldoras de la 4b | 7 de 7 | — (4b) |
| F-WKT-10 | **Semáforo**. Solo SPM, Split y Watts, y solo con objetivo. SPM y Watts fuera por debajo, Split por encima; los vatios máximos no cuentan; el split 0 y los objetivos 0 o negativos, como el legacy. La etiqueta y el borde en el color de la métrica, el valor en blanco o en rojo `#FF625B` con flecha, y `obj. N` debajo (D-ART-01, D-ART-09) | 9 de 9 | Las tarjetas con objetivo |
| F-WKT-11 (+ F-APP-12) | **Formatos**. Los 15: split, SPM, vatios, distancia, kcal, pulso, reloj, cuenta regresiva, cuenta por distancia, tiempo de vuelta, split de una repetición, metros de los paneles, hora, etiqueta de objetivo y resumen. Iguales en español y en inglés, con cualquier configuración regional | 16 de 16 | Todo el HUD |
| F-WKT-12 | **Workout en reposo**. El título, `Entrenamiento libre` y la lista con la tarjeta de Rutinas y su *chevron* (DV-21). La colección compartida con Rutinas, sin volver a leer la base. Los estados de carga, error con `Reintentar` y vacío (DV-23). El diálogo de inicio y la rutina que no se puede iniciar (DV-28). Todo en un solo desplazamiento (DV-22) | 9 de 9 | Workout |
| F-WKT-20 | **Controles y terminar**. `Terminar` y `Pausa` / `Reanudar`, grandes (128 × 56 y 148 × 56 dp). Pausa y reanudar actúan en el acto. `Terminar` pide confirmación y vuelve a Workout sin resumen, también desde la pausa. Sin dobles. Si la rutina termina sola con el diálogo abierto, el diálogo se cierra sin efecto | 7 de 7 (1 con parte pendiente: "Atrás" de Android) | Los controles del HUD |
| F-WKT-23 | **Pantalla clásica**: no se migra. QA confirmó que en el legacy nada la abre | 1 de 1 | — |
| F-BLE-10 | **Señal "remo conectado"**. El aviso naranja en cualquier estado que no sea "conectado". El inicio apagado: el botón al 38 % y las tarjetas al 50 %, sin responder. Sigue al remo en vivo con `Simular desconexión` y con `Desconectar`. La conexión se mira solo al tocar, y el motor no la mira | 5 de 5 | Workout sin remo |
| F-APP-14 | **Pantalla siempre encendida**: en toda la app desde el arranque, también minimizada, y liberada al cerrar; si el sistema la rechaza, no avisa nada | 1 de 4; 3 no verificables (§5.2) | — |
| F-WKT-13 (también toca) | **El inmersivo**. Se abre en el acto, por encima de todo, con sus capas. Escape no hace nada sin un diálogo abierto. Se cierra solo al volver a reposo. No muestra el nombre de la rutina | 5 de 5 (1 con parte pendiente: "Atrás" de Android) | Al entrenar |
| F-WKT-14 (también toca) | **La fila de información de la barra de pasos**: el tipo del paso; la pastilla de objetivo con el texto en blanco (DV-38); `Próximo:`, que late en su fondo y su borde (DV-24, DV-38); la cuenta regresiva, que crece al final | 6 de 6 | Arriba, con rutina |
| F-WKT-16 (también toca) | **La barra del libre**: `Entrenamiento Libre` con el ícono de remo | 1 de 1 | Arriba, en libre |
| F-WKT-17 (también toca) | **Tarjetas y banda**. El vidrio `glass.fallback` y los tamaños de D-ART-02, escalados con la pantalla. SPM, Split, Watts, la banda, kcal y BPM. Los valores en vivo, también en pausa. Se reacomoda al rotar. Nada se encima en las 4 ventanas de captura, ni con valores extremos, y la zona del bote queda libre | 9 de 10 (**falla** AC-WKT-17-04: el divisor de la banda) | El HUD |
| F-HIS-17, F-HIS-19 (también toca) | Las tablas de sesiones y puntos de la base v8 empiezan a llenarse, sin cambiar el esquema. Lectura de una sesión y de sus puntos | 3 de 3 | La base |
| F-SCN-01 (también toca) | **Fondo provisorio**: quieto, de dos tonos, el cielo y el agua del mediodía del legacy, hasta la escena de la 6a | 1 de 1 | Detrás del HUD |
| F-APP-03, F-APP-10 (también toca) | El inmersivo se apila como el editor. Los diálogos de inicio y de terminar y los avisos se comportan como los de la It. 3. **12 claves nuevas** en el catálogo (202 por idioma) | 4 de 4 | — |
| De iteraciones anteriores (15) | La cinta y la llave sobre el inmersivo (AC-BLE-16-01 a -03, que cambian por D-ART-10). La llave a la izquierda en Rutinas (AC-RUT-10-04, -09). Workout deja de ser provisoria. La paleta en el HUD. Letra 1,3×, teclado y "reducir movimiento" en las pantallas nuevas. La rutina sin pasos. La mini-barra en Workout. El esquema v8. Tus bases intactas. Las rutinas, cargadas al arrancar | 14 de 15 (**falla** AC-APP-05-07: el primer Tab) | — |

Además:

- **Los 11 tests del legacy** de esta área (LT-35 a LT-45: el pulso, los objetivos y los formatos) están portados uno a uno, con el mismo nombre, y pasan.
- **El oráculo del motor.** El `WorkoutProvider` del legacy corre fuera del legacy, y sus 11 vectores se comparan con el motor nuevo punto por punto. La lente de paridad corrió 15 escenarios más: 13 dieron idénticos, 1 distinto a propósito (DV-37) y 1 dejó un riesgo para la 10a (QA-V4a-L01).
- **Herramientas para el equipo**:
  - la herramienta de solo lectura de la demo, que lista las sesiones de una base sin tocarla;
  - el arnés de capturas `it4a`, con 49 tomas, sondas de aspecto y la auditoría de la llave;
  - `build.ps1`, que deja los builds de desarrollo siempre en la misma carpeta, para no disparar avisos nuevos del Firewall.
- **Documentos al día**: ART_BIBLE, ANIMATION_BIBLE, LEGACY_VISUAL_AUDIT y ASSET_SLOTS (Designer) y, al cerrar, el ROADMAP, FEATURE_INVENTORY y el inventario del motor ([01-po-spec §17](01-po-spec.md)).

---

## 3. Cómo ver la app

### 3.1 La demo en el build de Windows con simulador (lo más rápido)

El coordinador te deja el build de simulador en la carpeta de prueba: es `RowMate.exe`, con su carpeta `RowMate_Data` al lado. Es el del código final, con los arreglos de la ronda 2. Si preferís generarlo vos, mirá §3.3.

**Antes de empezar:**

- **Cerrá el `RowMate.exe` que quedó abierto.** Lo abriste desde el Explorador durante la verificación, y es el build de la ronda 1, de antes de los arreglos: ahí todavía puede pasar que una sesión iniciada justo mientras se cierra el inmersivo anterior quede sin HUD (QA-V4a-08). Además, los dos escribirían en la misma base.
- **El Firewall.** Si al abrir el build aparece el aviso "Seguridad de Windows", contestá **`Cancelar`**. La app no usa la red. Pasa solo la primera vez que se abre un build desde una carpeta nueva.
- **El idioma.** El build sigue la lista de idiomas de Windows, como el legacy. Si Windows está en inglés, el build sale en inglés: por eso, en los pasos, el texto en inglés va entre paréntesis.
- **El simulador del build tiene "ruido"**: los valores se mueven un poco alrededor de lo que elegís en el panel; las capturas usan uno sin ruido.
  - Cerca de un objetivo, la tarjeta puede alternar entre blanco y rojo. Es el semáforo evaluando cada lectura sin margen, como el legacy.
  - Para verla fija, alejate un paso más del objetivo.

**(a) Entrenamiento libre** (2 minutos)

1. **Abrí `RowMate.exe`.** Arranca en Dispositivo, con `Remo simulado` · `Conectado` (`Connected`).
   - *Opcional, para ver el pulso*: en la tarjeta del pulsómetro, `Buscar` (`Search`) → `Pulsómetro simulado` → `Conectar` (`Connect`).
2. **Workout.** El título `Entrenamiento` (`Workout`), `▶ Entrenamiento libre` (`Free workout`) y, si tenés rutinas, `O elegí una rutina:` (`Or pick a routine:`) con sus tarjetas. La llave está abajo a la izquierda. → DEMO-01
3. **`Entrenamiento libre`.** El inmersivo se abre en el acto:
   - arriba, `Entrenamiento Libre` (`Free Workout`);
   - SPM grande arriba a la izquierda; `Split /500m` y `Watts` arriba a la derecha;
   - abajo a la izquierda, la banda con el reloj (`00:01`, `00:02`…) y la distancia;
   - abajo a la derecha, `kcal` y, con el pulsómetro, `BPM` con el corazón;
   - abajo al centro, `Terminar` (`Finish`) y `⏸ Pausa` (`Pause`).

   La distancia y las kcal no arrancan de 0: son las del monitor, que el simulador viene sumando desde que abriste la app, como en el legacy. → DEMO-02
4. **La llave**, ahora a la derecha de `Pausa` → `+` de `Watts`. El panel se abre hacia arriba y a la izquierda, sin tapar Split ni Watts, y la tarjeta pasa a unos 160 en la lectura siguiente. Cerralo con su `✕`. → DEMO-03
5. **`Pausa`**: el reloj se detiene, pero los valores siguen llegando, y el botón pasa a `▶ Reanudar` (`Resume`), verde. **`Reanudar`**: el reloj sigue. → DEMO-04
6. **`Terminar`** → `Terminar entrenamiento` / `Se guardará la sesión. ¿Continuar?` (`Finish workout` / `The session will be saved. Continue?`). Mientras está abierto, el reloj sigue. **`Cancelar`** (`Cancel`): todo sigue. → DEMO-05
7. **`Terminar` otra vez → `Terminar`**: vuelve Workout en reposo, sin resumen ni aviso. → DEMO-06

**(b) Una rutina hasta el final, con el semáforo** (7 minutos)

Primero armá `Prueba 4a` en **Rutinas** (`Routines`), como en la It. 3:

1. `+` → en `Nombre de la rutina` (`Routine name`), escribí `Prueba 4a`.
2. `+ Paso` (`Step`) → `Calentamiento` (`Warmup`), `Por tiempo` (`By time`), `Seg` (`Sec`) `30` → `Confirmar` (`Confirm`).
3. `Serie` (`Series`): aparece un bloque ×3 con `Trabajo` 2m y `Descanso` 1m.
4. Lápiz de `Trabajo` (`Work`) → `Min` `1`, `W mín` (`W min`) `160`, `SPM objetivo` (`Target SPM`) `22` y, más abajo, `Incremento SPM (+/rep)` (`SPM increment (+/rep)`) `2` → `Confirmar`.
5. Lápiz de `Descanso` (`Rest`) → `Min` `0` y `Seg` `30` → `Confirmar`. Tocá `×3` → `Cantidad` (`Amount`) `2` → `OK`.
6. `+ Paso` → `Enfriamiento` (`Cooldown`), `Seg` `30` → `Confirmar`. Después, `Guardar` (`Save`).

Después, en **Workout**:

7. La tarjeta `Prueba 4a` dice `1 serie · 1 descanso` (`1 interval · 1 rest`) y tiene una mini-barra de 6 tramos: amarillo, rosa, verde, rosa, verde y azul. Tocala → `Prueba 4a` / `1 serie · 1 descanso` / `¿Iniciar el entrenamiento?` (`Start the workout?`) → `Iniciar` (`Start`). → DEMO-07
8. **Calentamiento.** Arriba, `Calentamiento` (`Warmup`) en amarillo y la cuenta `0:30`, `0:29`…
   - En `0:15` aparece `Próximo: Trabajo` (`Up next: Work`), que late suave.
   - En los últimos 5 s, la cuenta crece y se tiñe de amarillo. → DEMO-08 y VID-01
9. **Trabajo, repetición 1.** `Trabajo` en rosa, la pastilla `>160W · 22 spm · ↗ SPM +2/rep` (el `↗` es un ícono) y la cuenta `1:00`.
   - Si dejaste Watts en 160 en la parte (a), bajalo: la llave → `−` de Watts (150).
   - Watts se pone en **rojo, con `▲`**, y debajo dice `obj. 160` (`target 160`).
   - SPM sigue en blanco, con `obj. 22`. → DEMO-09
10. **La llave → `+` de Watts**: Watts vuelve a blanco. Con el ruido, mejor tocalo dos veces (170). → DEMO-10
11. **Repetición 2**, después del descanso de 30 s. La pastilla dice `>160W · 24 spm` y SPM, `obj. 24`.
    - A 24 spm cumple.
    - La llave → `−` de `Ritmo` (23): SPM en rojo con `▲`.
    - `+`: vuelve a blanco. → DEMO-11 y VID-02
12. **Enfriamiento.** Sin pastilla ni semáforo. En los últimos 5 s la cuenta crece y se tiñe, sin `Próximo`, porque es el último paso. → DEMO-12
13. **El final, a los 4 minutos.** La rutina termina sola, se guarda y vuelve Workout con el aviso `¡Entrenamiento completado!` (`Workout Completed!`). El diálogo de rutina completada llega en la 4b. → DEMO-13

**(c) Cerrar la app a mitad de una sesión** (3 minutos)

1. Tocá `Entrenamiento libre` y dejá correr unos 30 s.
2. Cerrá la ventana con la `✕` de Windows, o terminá `RowMate.exe` desde el Administrador de tareas.
3. Abrí `RowMate.exe` otra vez. Arranca en Dispositivo, como siempre, sin ofrecer retomar nada, igual que el legacy.
4. Para ver la sesión guardada (el Historial llega en la It. 9), abrí una consola en la carpeta del repositorio y corré, con .NET 8:

   ```
   dotnet run --project tools/dotnet/RowMate.SessionList -- --variant simulator
   ```

   Si `dotnet` no está en el PATH, usá el de la carpeta de instalación de .NET 8, como en `tools/dotnet/README.md`.

   - La lista va de la más nueva a la más vieja: `id · inicio · fin · rutina · puntos`.
   - Arriba de todo está la sesión cortada, con su hora de inicio, `—` en el fin, `libre` y unos 6 puntos (uno cada 5 s).
   - Debajo están las de (a) y (b), con su hora de fin.
   - La herramienta lee una copia de la base y nunca la modifica.

Si querés probar más, en el mismo build:

- **Sin remo.**
  - Dispositivo → `Desconectar` (`Disconnect`) → Workout: el aviso naranja `Conectá el rower para iniciar un entrenamiento` (`Connect the rower to start a workout`), `Entrenamiento libre` apagado y las tarjetas al 50 %, que no responden. → DEMO-14
  - `Buscar dispositivos` (`Search devices`), en Dispositivo, lo vuelve todo a la normalidad.
  - Con la llave → `Simular desconexión` pasa lo mismo durante 3 s, sin salir de Workout.
- **Minimizar.** En pleno entrenamiento, minimizá la ventana 1 o 2 minutos y volvé. El reloj avanzó ese tiempo, y la sesión siguió guardando puntos. Si era una rutina y terminó mientras tanto, al volver ves Workout con `¡Entrenamiento completado!` una sola vez.
- **Teclado.**
  - En el inmersivo, Tab recorre `Terminar` → `Pausa` → la llave, con un anillo de foco, y Enter o Espacio los activan.
  - Escape no hace nada si no hay un diálogo abierto, como en el legacy; con el diálogo, lo cancela.
  - **Ojo, QA-V4a-04**: el primer Tab después de usar el mouse enfoca sin mostrar el anillo; desde el segundo, se ve. En el editor de Rutinas, ese foco invisible cae en `←`, y un Enter sale sin guardar.
- **Doble toque.** Dos toques rápidos en `Entrenamiento libre` abren un solo entrenamiento.
- **Terminar al final de una rutina.** Si tocás `Terminar` en los últimos segundos y la rutina termina sola con el diálogo abierto, el diálogo se cierra solo y aparece `¡Entrenamiento completado!`.

**Tus datos.** El build de simulador guarda las rutinas y las sesiones en `rower_app_dev.db`, dentro de `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`, y el build normal usa `rower_app.db` en la misma carpeta. Para empezar de cero, cerrá la app y borrá ese archivo, junto con `-wal` y `-shm` si están. La app legacy guarda sus datos en otro lugar y la nueva no los toca.

### 3.2 En Unity, con el simulador

Igual que en la It. 3 ([REPORT de la It. 3 §3.2](../03-rutinas/REPORT.md)). Unity admite una sola instancia por proyecto: antes de abrir el Editor, verificá que no esté corriendo ninguno de los scripts de §3.3 a §3.6.

1. **Abrí el proyecto** en Unity Hub, con el Editor **6000.3.27f1**.
2. **Prendé el modo simulador**: menú **RowMate → Modo simulador**.
3. **Abrí la escena** `Assets/RowMate/Scenes/Boot.unity`.
4. **Elegí el tamaño en la vista *Game***:
   - `1280x720`: la ventana de Windows, como la demo;
   - `411 x 914`: un teléfono vertical, como CAP-12;
   - `914 x 411`: un teléfono horizontal, como CAP-15;
   - `640 x 360`: una ventana baja, como CAP-16.
5. **Dale Play y andá a Workout.** Seguí los pasos de §3.1. Para "cerrar y abrir la app", pará y volvé a darle Play.
6. **Idioma, letra grande y "reducir movimiento"**: están en el menú **RowMate → Simular**.

**Antes de cerrar Unity, dejá `RowMate → Modo simulador` sin tilde.** Si queda puesto, se guarda en la configuración del proyecto, y el control del CI falla a propósito.

### 3.3 Los builds de Windows

Con el Editor **cerrado**, desde la carpeta del repositorio:

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator
Builds\Windows\simulator\RowMate.exe
```

Es el build de la demo:

- Con simulador, el build siempre es de desarrollo: por eso lleva la marca "Development Build".
- Va siempre a `Builds/Windows/simulator`. Con otra carpeta (`-Out`), el script sale con 5 sin tocar nada, porque cada carpeta nueva dispara un aviso del Firewall; `-AllowNewFolder` lo fuerza.
- Si tenés abierto un `RowMate.exe` de esa carpeta, cerralo antes: el build pisa sus archivos (QA-V4a-08).

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows
Builds\Windows\normal\RowMate.exe
```

Es el build normal. No tiene cinta, llave ni marca "Development Build", y nunca dispara el aviso del Firewall. Usa `rower_app.db`, y el inspector de la salida confirma que no lleva nada del simulador, de diagnósticos ni de pruebas.

El script termina con 0 si el build salió bien. Termina con 2 si el build normal tiene restos del simulador, de diagnósticos o de pruebas, con 3 si falta un módulo (NO VERIFICABLE) y con 5 si se le pide una carpeta nueva para un build de desarrollo. Hoy, `-Backend IL2CPP` y Android terminan con 3.

### 3.4 Las pruebas sin Unity (.NET 8)

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1
```

Al final aparecen dos líneas `Passed!`: el simulador, con 173 que pasan, y el resto, con 2055 pruebas, 2033 que pasan y 22 ignoradas. Si ninguna dice `Failed!`, está todo verde. Las pruebas del motor usan un reloj de prueba, y las del guardado, SQLite real sobre archivos temporales: nunca tocan tus bases.

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix
```

Escribe `tools\dotnet\.artifacts\TestResults\ac-matrix.md` con los 535 IDs: 158 de la It. 1, 128 de la It. 2, 91 de la It. 3 y 158 nuevos de la 4a (147 criterios y 11 casos legacy).

- Sin los resultados de Unity, 109 quedan "PENDIENTE UNITY".
- Si antes corriste §3.5, agregá `-UnityResults tools/unity/.artifacts`: la matriz queda en **534 PASA**, 0 FALLA y 1 IGNORADO (AC-WKT-02-10, que es de Android).

Para correr un solo criterio: `-Filter "TestCategory=AC-WKT-06-05"`.

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
| EditMode | 2093 | 2067 | 0 | 26 |
| EditMode `-Simulator` | 2266 | 2240 | 0 | 26 |
| PlayMode | 264 | 263 | 0 | 1 |
| PlayMode `-Simulator` | 328 | 327 | 0 | 1 |

- Las PlayMode montan la app real con una base de prueba: Workout en reposo, el inmersivo, el HUD libre y con rutina, los controles y los diálogos, los errores de la base y la base lenta, el segundo plano, el pulsómetro simulado y la llave.
- Las ignoradas de EditMode necesitan un teléfono, una Mac o GitHub. Entre ellas están las 5 de la herramienta de la demo, que dentro de Unity no se compila y en dotnet pasa.
- La de PlayMode es `HudLayoutTests.ScalesOfTheDesignNotes`, que espera las fuentes nuevas (QA-V4a-06).
- Con `-Simulator`, el script prende el modo simulador solo durante la corrida y lo deja como estaba.

### 3.6 Las capturas

Están en [snapshots/](snapshots/). La tabla completa, con el estado, las sondas y la auditoría de cada toma, está en [snapshots/INDEX.md](snapshots/INDEX.md). Las renderiza Unity con la interfaz real, una base de prueba, el reloj de prueba y el simulador sin ruido; no son maquetas. Para regenerarlas, con el Editor cerrado (alrededor de un minuto):

```
powershell -NoProfile -File tools/unity/capture-snapshots.ps1 -Set it4a
```

Fijate en dos cosas:

- La cinta `SIMULADOR` tapa en parte el comienzo de la barra de arriba, como en el legacy (`docs/image-03.png`).
- **En la banda falta el divisor** entre el reloj y la distancia en CAP-16, -18, -20, -24, -26, -27a, -27b y -30 (QA-V4a-03, §5.1). En las tomas de 1280 × 720 se ve.

**La demo, toma por toma** (ventana de 1280 × 720):

| DEMO-01 · Workout con `Prueba 4a` | DEMO-02 · libre a los 5 s |
|---|---|
| ![DEMO-01](snapshots/DEMO-01-workout.png) | ![DEMO-02](snapshots/DEMO-02-libre.png) |

| DEMO-03 · el panel abierto desde la llave, con Watts en 160 | DEMO-04 · en pausa |
|---|---|
| ![DEMO-03](snapshots/DEMO-03-panel-en-el-hud.png) | ![DEMO-04](snapshots/DEMO-04-pausa.png) |

| DEMO-05 · `Terminar entrenamiento` | DEMO-07 · el diálogo de inicio de `Prueba 4a` |
|---|---|
| ![DEMO-05](snapshots/DEMO-05-terminar.png) | ![DEMO-07](snapshots/DEMO-07-inicio-prueba-4a.png) |

| DEMO-08 · calentamiento con `Próximo: Trabajo` | DEMO-09 · Watts fuera de objetivo |
|---|---|
| ![DEMO-08](snapshots/DEMO-08-calentamiento-proximo.png) | ![DEMO-09](snapshots/DEMO-09-watts-fuera.png) |

| DEMO-10 · Watts cumple | DEMO-11 · repetición 2: SPM fuera, con `obj. 24` |
|---|---|
| ![DEMO-10](snapshots/DEMO-10-watts-cumple.png) | ![DEMO-11](snapshots/DEMO-11-spm-fuera.png) |

| DEMO-12 · el enfriamiento por terminar | DEMO-13 · `¡Entrenamiento completado!` |
|---|---|
| ![DEMO-12](snapshots/DEMO-12-por-terminar.png) | ![DEMO-13](snapshots/DEMO-13-completada.png) |

![DEMO-14](snapshots/DEMO-14-sin-remo.png)

DEMO-14: sin remo, con el aviso naranja y todo apagado. [DEMO-06](snapshots/DEMO-06-reposo.png), la vuelta a reposo después de terminar a mano, no se repite: es idéntica byte a byte a DEMO-01.

**Workout en reposo** (teléfono vertical):

| CAP-01 · la lista | CAP-04 · en inglés | CAP-02 · sin remo |
|---|---|---|
| ![CAP-01](snapshots/CAP-01-workout-lista.png) | ![CAP-04](snapshots/CAP-04-workout-lista-en.png) | ![CAP-02](snapshots/CAP-02-workout-sin-remo.png) |

| CAP-03 · sin rutinas | CAP-06 · cargando | CAP-07 · error de carga |
|---|---|---|
| ![CAP-03](snapshots/CAP-03-workout-sin-rutinas.png) | ![CAP-06](snapshots/CAP-06-workout-cargando.png) | ![CAP-07](snapshots/CAP-07-workout-error.png) |

| CAP-08 · el diálogo de inicio | CAP-09 · una rutina que no se puede iniciar | CAP-11 · letra 1,3× |
|---|---|---|
| ![CAP-08](snapshots/CAP-08-dialogo-de-inicio.png) | ![CAP-09](snapshots/CAP-09-rutina-que-no-inicia.png) | ![CAP-11](snapshots/CAP-11-workout-letra-130.png) |

| CAP-28 · `Corta` terminada sola |
|---|
| ![CAP-28](snapshots/CAP-28-rutina-completada.png) |

**Workout en la ventana** (1280 × 720, comparable con `docs/image-02.png` del legacy, sin el selector de escenario: DV-34) y en la ventana baja (640 × 360, con 30 rutinas, letra 1,3× y sin remo, desplazada al final):

![CAP-10](snapshots/CAP-10-workout-w1280.png)

![CAP-05](snapshots/CAP-05-workout-l640-final.png)

**El HUD del entrenamiento libre** (teléfono vertical):

| CAP-12 · a los 42 s | CAP-13 · con el pulsómetro | CAP-14 · en pausa |
|---|---|---|
| ![CAP-12](snapshots/CAP-12-libre-tv.png) | ![CAP-13](snapshots/CAP-13-libre-con-pulso.png) | ![CAP-14](snapshots/CAP-14-pausa.png) |

| CAP-26 · `Terminar entrenamiento` | CAP-27a · en inglés | CAP-27b · `Finish workout` |
|---|---|---|
| ![CAP-26](snapshots/CAP-26-dialogo-terminar.png) | ![CAP-27a](snapshots/CAP-27a-libre-en.png) | ![CAP-27b](snapshots/CAP-27b-terminar-en.png) |

| CAP-29 · la base que falla: `No se pudo guardar la sesión` |
|---|
| ![CAP-29](snapshots/CAP-29-no-se-pudo-guardar.png) |

**El HUD libre en horizontal**: el teléfono (914 × 411), la ventana de 1280 × 720, comparable con `docs/image-03.png` del legacy, y la ventana baja con valores largos y el contorno de la zona del bote:

![CAP-15](snapshots/CAP-15-libre-th.png)

![CAP-17](snapshots/CAP-17-libre-w1280.png)

![CAP-16](snapshots/CAP-16-valores-largos-l640.png)

**El HUD con rutina** (teléfono vertical):

| CAP-18 · calentamiento | CAP-19 · trabajo con objetivos | CAP-20 · `Pirámide 500`: Split y Watts fuera |
|---|---|---|
| ![CAP-18](snapshots/CAP-18-calentamiento.png) | ![CAP-19](snapshots/CAP-19-trabajo-con-objetivos.png) | ![CAP-20](snapshots/CAP-20-piramide-500.png) |

| CAP-21 · `Próximo: Descanso` | CAP-22 · por terminar | CAP-27c · en inglés |
|---|---|---|
| ![CAP-21](snapshots/CAP-21-proximo.png) | ![CAP-22](snapshots/CAP-22-por-terminar.png) | ![CAP-27c](snapshots/CAP-27c-rutina-en.png) |

| CAP-30 · un paso sin meta |
|---|
| ![CAP-30](snapshots/CAP-30-paso-sin-meta.png) |

**El HUD con rutina en horizontal**: el teléfono, la ventana baja con SPM y Watts fuera, y la ventana de 1280 × 720:

![CAP-23](snapshots/CAP-23-trabajo-th.png)

![CAP-24](snapshots/CAP-24-dos-fuera-l640.png)

![CAP-25](snapshots/CAP-25-trabajo-w1280.png)

**Videos** (30 cuadros por segundo):

- [VID-01 · `Próximo`, la cuenta por terminar y el cambio de paso (17 s)](snapshots/VID-01-proximo-y-cambio-de-paso.mp4). `Próximo: Trabajo` entra y late; en `0:05` la cuenta crece y se tiñe; en el cambio, las tarjetas con objetivo toman su borde y su `obj.` sin saltos.
- [VID-02 · fuera de objetivo (10 s)](snapshots/VID-02-fuera-de-objetivo.mp4). SPM pasa a rojo con la flecha y vuelve a blanco; el empujón sale solo en el primer cruce.
- [VID-03 · pausa y terminar (10 s)](snapshots/VID-03-pausa-y-terminar.mp4). El botón cambia en el acto, el diálogo entra y sale, y el inmersivo se funde sobre Workout.

### 3.7 Lo que podés verificar vos (opcional)

Cierra lo que QA no puede ver sin tocar tu configuración:

1. **AC-APP-14-01 a -03: la pantalla siempre encendida** (1 minuto).
   - Con el build abierto, en cualquier pestaña, abrí una consola **como administrador** y corré `powercfg /requests`. Tiene que aparecer `RowMate.exe` en `DISPLAY` y en `SYSTEM`.
   - Minimizá la ventana y correlo otra vez: sigue apareciendo.
   - Cerrá la app y correlo de nuevo: ya no aparece.
   - La salida trae la ruta completa del `.exe`: no hace falta copiarla; alcanza con contar qué viste.
   - Si tu plan de energía apaga la pantalla a los pocos minutos, también podés dejar la app quieta ese tiempo: la pantalla no se apaga.
2. **Lo que quedaba de las It. 2 y 3**: el modo claro, la app legacy después de la nueva y los idiomas preferidos. Siguen en el [REPORT de la It. 3 §3.7](../03-rutinas/REPORT.md).

---

## 4. Resultado de las pruebas

Son los números de la ronda 2 de QA ([05-qa-verification, "Ronda 2"](05-qa-verification.md)):

| Qué | Resultado |
|---|---|
| Compilación (dotnet) | **0 advertencias, 0 errores** |
| Pruebas dotnet | **2228: 2206 pasan, 0 fallan, 22 ignoradas**. El simulador, 173 de 173; el resto, 2055, con 2033 que pasan y 22 ignoradas. Al cerrar la It. 3 eran 1599 |
| Unity EditMode | **2093: 2067 pasan, 0 fallan, 26 ignoradas** |
| Unity EditMode con simulador | **2266: 2240 pasan, 0 fallan, 26 ignoradas, 0 inconclusas**. El modo simulador quedó como estaba |
| Unity PlayMode (interfaz montada) | **264: 263 pasan, 0 fallan, 1 ignorada**; con simulador, **328: 327 pasan, 0 fallan, 1 ignorada**. En la It. 3 eran 188 y 235 |
| Las ignoradas | Son a propósito: necesitan un teléfono, una Mac, GitHub o las fuentes de la 4b. No son fallas |
| Matriz (535 IDs) | **534 pasan**, 0 fallan, 1 ignorado (AC-WKT-02-10, Android) y 0 sin prueba. Las pruebas de los dos criterios que fallan pasan porque no miran el ancho del divisor ni el anillo del primer Tab: QA vio las fallas en las capturas, con una sonda en Unity y en la PC (§5.1) |
| Casos legacy | **11 de 11** (LT-35 a LT-45), portados uno a uno |
| Paridad con el motor del legacy | El `WorkoutProvider` del legacy corrió fuera del legacy con los mismos remos de prueba. Dan idénticos los 11 vectores del oráculo y 13 de los 15 escenarios extra de la lente: los puntos, los totales, los cambios de paso, los objetivos y el instante del fin. De los otros 2, uno es DV-37 (a propósito) y el otro es QA-V4a-L01 (un riesgo para la 10a). Formatos: 84 casos, 0 diferencias |
| Rendimiento | El HUD no reserva memoria por cuadro mientras late y tiene como mucho 60 elementos a la vista. Un alcance de 8 h de segundo plano se procesa en menos de 500 ms y publica una sola foto. Al salir del inmersivo la app vuelve a 30 cuadros por segundo en reposo: antes del arreglo de QA-V4a-02 quedaba en 60 |
| Capturas | **49 tomas** (46 PNG y 3 MP4): **0 con problemas, 0 desbordes**, las sondas de aspecto dentro de tolerancia y la auditoría de la llave en verde en todas. Iguales byte a byte en dos corridas seguidas y en las dos rondas. Las de las It. 2 y 3 se regeneraron una vez, por D-ART-10 (la llave a la izquierda), y QA explicó cada diferencia. QA y el Designer miraron las 46 fotos, y los videos cuadro por cuadro; el PO revisó las 46 al cerrar |
| Prueba en la PC (build de simulador) | Funcionaron: el libre con pausa y terminar; `Corta` y `Prueba 4a` hasta el final, con el semáforo corregido desde el panel; la rutina que termina con el diálogo de terminar abierto, que se cerró solo; y sin remo. **Minimizada 2 min**: el reloj marcó `02:10` a los 130,9 s, y la base sumó los 24 puntos de ese lapso. **Con el proceso matado a mitad**, la sesión quedó cortada con sus 31 puntos y la base, íntegra; al reabrir, Dispositivo, sin ofrecer nada. En las sesiones cerradas, los totales guardados son iguales a los recalculados de sus puntos. Los íconos ▶ y ⏸ se ven bien |
| Esquema v8 | Sin cambios. Las tablas de sesiones y puntos se llenan con las columnas del legacy, con `origin` = `app` y 0 puntos huérfanos |
| Builds de Windows (Mono) | Los dos salen bien. El normal no tiene rastros del simulador, de diagnósticos ni de pruebas. Ningún aviso nuevo del Firewall: 12 reglas antes y después |
| Pantalla siempre encendida | Pasan las pruebas que se lo piden de verdad a Windows y lo liberan en el mismo hilo. En la PC no se pudo mirar con `powercfg`, que pide una consola de administrador (§5.2) |
| IL2CPP y Android | **No verificables**: faltan los módulos (`build.ps1` sale con 3, "NO VERIFICABLE") |
| Tus datos y los del legacy | Ninguna prueba, captura ni corrida en la PC usó tus bases: todas usaron carpetas temporales. Tu `rower_app_dev.db` cambió durante la verificación, pero lo escribió el `RowMate.exe` que tenías abierto vos: lo muestran su registro y quién tenía abiertos los archivos. El repositorio legacy está intacto (`git status` vacío), y nadie leyó el archivo de credenciales de Strava |
| Repositorio público | 0 rutas de la PC en lo versionado y en lo nuevo |

---

## 5. Criterios de aceptación

| Área | Criterios | Cumplen | Con parte de equipo pendiente (entre los que cumplen) | Fallan | No verificables |
|---|---|---|---|---|---|
| WKT (motor, Workout, inmersivo y HUD) | 130 | 128 | 3 | 1 | 1 |
| BLE (remo conectado) | 5 | 5 | 0 | 0 | 0 |
| APP (pantalla encendida, navegación y catálogo) | 8 | 5 | 0 | 0 | 3 |
| HIS y SCN (la base y el fondo) | 4 | 4 | 0 | 0 | 0 |
| De iteraciones anteriores (BLE 3, APP 6, RUT 4, HIS 2) | 15 | 14 | 0 | 1 | 0 |
| **Total** | **162** | **156** | **3** | **2** | **4** |

### 5.1 Los dos que fallan: las condiciones del cierre

Los dos son menores y se corrigen al empezar la 4b, antes de construir encima. Hasta que QA los vea pasar, quedan reabiertos.

| Criterio | Qué pasa | Por qué no frena el cierre | Quién y cuándo |
|---|---|---|---|
| AC-WKT-17-04 · la banda del HUD | El divisor vertical entre el reloj y la distancia (1 × 32 dp, blanco al 12 %) queda con ancho 0 en el teléfono y en la ventana baja, así que no se ve: CAP-16, -18, -20, -24, -26, -27a, -27b y -30; en CAP-12 se ve apenas una línea. En la ventana de 1280 × 720 se ve. El reloj, la distancia y el resto de la banda están bien | Es una línea de adorno: no cambia ningún dato ni ningún lugar. El arreglo es una regla (que el divisor no se achique), una prueba del ancho y una regeneración de capturas | QA-V4a-03: el Architect, con el visto bueno del Designer, al empezar la 4b |
| AC-APP-05-07 · el teclado | Después de usar el mouse, el primer Tab enfoca el primer control sin mostrar el anillo; desde el segundo Tab se ve. Un Enter activa lo enfocado aunque no se vea: en el HUD abre el diálogo de terminar, y en el editor de Rutinas, con el foco en `←`, sale sin guardar (el editor sale sin confirmar, como el legacy). El resto del criterio pasa: el orden de Tab, Enter y Espacio, Escape, y que después de un clic el foco no vuelva | Pasa solo al mezclar mouse y teclado. En el entrenamiento no se pierde nada, porque terminar pide confirmación. En el editor sí se puede perder lo que no se guardó, y por eso es condición | QA-V4a-04: el Architect, al empezar la 4b |

### 5.2 Los no verificables y por qué

| Criterio | Qué falta ver | Por qué no se pudo | Cuándo |
|---|---|---|---|
| AC-WKT-02-10 | En Android, pasar a otra app o bloquear la pantalla 2 min con una sesión en marcha | Faltan el módulo de Android y un teléfono. En dotnet, el mismo motor con el ciclo de vida de prueba da lo mismo que en primer plano | Con el módulo y el teléfono; con el remo real, en la 10a |
| AC-APP-14-01 | Que con la app abierta la pantalla no se apague ni el equipo se suspenda, mirado con `powercfg /requests` | `powercfg /requests` pide una consola de administrador, y QA corre sin elevación; tampoco cambia tu configuración de energía. Pasan las pruebas que se lo piden de verdad a Windows y lo liberan | Vos, en 1 minuto (§3.7) |
| AC-APP-14-02 | Lo mismo con la ventana minimizada y al restaurarla; en Android, al volver de otra app | Lo mismo, y en Android falta el teléfono | Vos (Windows, §3.7) y el teléfono |
| AC-APP-14-03 | Que al cerrar la app el pedido se libere | Lo mismo | Vos, §3.7 |

### 5.3 Cumplen, pero falta una parte de equipo

Pasan en el Editor y en la PC. La parte que se nombra queda no verificable, y si falla al verla, el criterio se reabre.

| Criterio | Parte pendiente | Quién y cuándo |
|---|---|---|
| AC-WKT-02-08 | En Android, que el primer cuadro al volver del segundo plano ya muestre el presente | El teléfono |
| AC-WKT-13-03, AC-WKT-20-03 | "Atrás" de Android abre la confirmación de terminar. En la PC ya se vio que Escape sin diálogo no hace nada y con el diálogo lo cancela | El teléfono |

### 5.4 Cumplen, y se completan más adelante

| Criterio | Qué falta | Cuándo |
|---|---|---|
| AC-WKT-03-05, AC-WKT-02-06 | El aviso `¡Entrenamiento completado!` es provisorio: lo reemplaza el diálogo de rutina completada | It. 4b |
| AC-WKT-09-01 a -07 | El cumplimiento de objetivos se calcula; lo muestran las píldoras | It. 4b |
| AC-WKT-13-01, AC-WKT-14-01, AC-WKT-16-01 | Las barras del sistema de Android, todavía visibles; la barra de pasos sin píldoras; la barra libre sin reloj de pared ni chip de fase | It. 4b |
| AC-WKT-11-09 a -12, -15 | Los formatos de los parciales, de la hora y del resumen final ya están, sin una pantalla que los use todavía | It. 4b y 5 |
| AC-SCN-01-01, AC-WKT-17-01 | El fondo provisorio y el vidrio sin desenfoque | It. 6a y 6b |
| AC-WKT-06-10, AC-HIS-19-01, -02 | Las sesiones, también las cortadas, se ven en el Historial | It. 9 |
| AC-WKT-03-03, AC-WKT-03-10, AC-BLE-10-07 | Con el remo real: el saneamiento de la distancia con la cadencia del AMS-670B (QA-V4a-L01) y la caída con el aviso "Reconectando…" | It. 10a |
| AC-WKT-06-11 | "La última sesión terminada" la usa la subida a Strava | It. 12 |
| AC-APP-14-04 | La pantalla encendida en iOS y macOS | It. 10b |

---

## 6. Desvíos aceptados

### 6.1 Diferencias visibles con el legacy, a propósito

Las que cambian una regla del legacy son correcciones que ya estaban decididas: DV-26 (Q-55), DV-27 (Q-91), DV-28 (R3-08 y Q-92), DV-29 (Q-13) y DV-30 (como Q-19). Las demás no cambian ningún número ni ninguna regla. DV-01 a DV-20 ya las aceptaste en las It. 2 y 3. Si alguna de estas no te gusta, decilo (§8).

| ID | Qué cambia | Por qué |
|---|---|---|
| DV-21 | En Workout, la tarjeta de rutina es la de Rutinas, con la descripción y la mini-barra, y un *chevron*. El legacy muestra solo el nombre y el resumen | Una sola tarjeta de rutina en la app; la descripción y la silueta ayudan a elegir (P-06) |
| DV-22 | En Workout se desplaza todo junto; en el legacy, solo la lista | En una ventana baja, con el aviso y letra grande, la parte fija no dejaba lugar ni para una tarjeta |
| DV-23 | Mientras cargan las rutinas se ve el indicador, y si la carga falla, el error con `Reintentar`. El legacy muestra "sin rutinas" en los dos casos | Q-13 |
| DV-24 | `Próximo:` en español; el legacy dice `Up next:` en los dos idiomas | Era el único texto en inglés del HUD en español (P-01). `Watts`, `SPM`, `kcal` y `BPM` quedan como en el legacy |
| DV-25 | Un paso sin meta muestra `?`; el legacy, `nullm` | Defecto evidente |
| DV-26 | El paso por distancia cuenta los metros con el saneamiento de los parciales. Un reinicio del monitor ya no lo traba (con el reinicio de prueba, termina en el segundo 26 y no en el 285) y lo remado en pausa no cuenta. Un paquete sin distancia hace perder unos 2 s de metros, y un monitor que no informa la distancia cada segundo podría trabarlo: se resuelve en la 10a (P-04) | Q-55: el paso y los parciales de la It. 5 cuentan los mismos metros |
| DV-27 | Si la app estuvo trabada o minimizada, los segundos se procesan al volver, en orden; el legacy los pierde | Q-91: la sesión sigue en segundo plano |
| DV-28 | Una rutina que el editor no dejaría guardar (tocada a mano en la base) no se inicia: avisa `Esta rutina no se puede iniciar: revisá sus pasos en Rutinas`. En el legacy, una serie ×2 000 000 000 traba la app y una ×0 hace fallar la pantalla | R3-08 |
| DV-29 | Si la base falla, el entrenamiento anda igual y avisa `No se pudo guardar la sesión`. En el legacy, el reloj queda en `00:00` o nadie se entera | Q-13 |
| DV-30 | Dos toques rápidos crean una sola sesión; en el legacy, pueden ser dos | Como Q-19 en el editor |
| DV-31 | El vidrio `#070D16 @86 %`. Los valores en el tono claro de su métrica, con las etiquetas enteras y las unidades al 80 %. Los tamaños de D-ART-02. Botones de 128 × 56 y 148 × 56. El legacy: negro al 45-55 %, 52 / 36 / 28 dp sin escala y botones de 120 × 48 y 140 × 48 | Contraste sobre cualquier fondo, y botones para manos transpiradas (QA-P-17, R-10, D-ART-01, D-ART-02) |
| DV-32 | Con objetivo, la etiqueta y el borde conservan el color de la métrica; el valor va en blanco o en rojo `#FF625B` con flecha, y `obj. N` debajo. El legacy pone todo en blanco o en `#FF4444` | Q-60, D-ART-01, D-ART-09: el significado no depende solo del color |
| DV-33 | En horizontal, kcal y BPM van en fila a la altura de los controles, y la banda es compacta. En el legacy, en un teléfono horizontal kcal se encima con Watts | Defecto de diseño del legacy |
| DV-34 | (Provisorio) Todavía no están el selector de escenario, la escena, las píldoras, el reloj de pared, el chip de fase ni el diálogo de completado. Hay un fondo provisorio, y al terminar, el aviso `¡Entrenamiento completado!` | Llegan en la 4b, la 6a y la 8 |
| DV-35 | El `📈` de la pastilla se dibuja como un ícono, como en el editor (DV-10) | El emoji depende de la fuente del sistema |
| DV-36 | `Creá una rutina en la pestaña "Rutinas"` y `Entrenamiento en curso...` más legibles: al 54 %, no al 38 % | Contraste mínimo (D-ART-03) |
| DV-37 | Un paso de duración negativa muestra `0:00` y termina en el primer segundo. En el legacy, la pantalla falla mientras dura | Defecto evidente; el motor no cambia |
| DV-38 | En la barra de pasos, el texto de las dos pastillas va en blanco y, en `Próximo`, laten solo el fondo y el borde. En el legacy, el texto va en el color del tipo y late todo | Contraste: 3,5 a 4,9:1 con los colores del legacy; 8:1 o más en blanco (P-15) |
| DV-39 | La llave del simulador va abajo a la izquierda y, en el entrenamiento, sobre kcal y BPM o junto a `Pausa`; el panel se abre desde la llave. Reemplaza a DV-13 de la It. 3 | Ya no tapa acciones que se repiten en cada fila, ni `Serie` (QA-V3-09) |

Diferencias temporales que encontró QA: QA-V4a-03 (la banda sin divisor) y QA-V4a-04 (el primer Tab sin anillo), en §5.1.

### 6.2 Decisiones técnicas con efecto que se puede notar

| ID | Qué | Efecto para vos |
|---|---|---|
| DT4a-01 | Al cerrarse un diálogo, el foco vuelve a quien lo abrió solo si venías usando el teclado; con el mouse, no queda nada enfocado | Un Enter después de `Cancelar` no vuelve a abrir el diálogo de terminar, como el legacy en la PC. Su efecto lateral es QA-V4a-04 |
| DT4a-02 | El reloj arranca cuando la base confirma la sesión o a los 100 ms del toque, lo que pase primero | Con una base lenta, el HUD no espera |
| DT4a-07 | El punto final y el cierre se guardan en una sola escritura | Un corte justo al terminar nunca deja una sesión a medias |
| DT4a-09 | En Windows se pide también que el equipo no se suspenda; el legacy solo pide la pantalla | Con la app abierta, ni la pantalla se apaga ni la PC se duerme por inactividad |
| DT4a-10 | Con la ventana minimizada, la app sigue andando a su ritmo. El tope de 10 cuadros por segundo minimizada llega en la 6a | Minimizada, la sesión sigue en tiempo real |
| DT4a-13 | Si la base no confirma el cierre en 15 s de primer plano, se vuelve a reposo con `No se pudo guardar la sesión`; si confirma más tarde, la sesión queda bien | Nunca un indicador eterno |
| DT4a-15 | El motor vuelve a reposo solo, cuando termina el guardado | El inmersivo se cierra por estado. La 4b suma la espera del diálogo de completado |
| DT4a-16 | Una progresión que se pasaría del límite de los enteros satura en lugar de fallar | Solo pasa con una base tocada a mano |
| DT4a-17 | Si un lote de puntos falló y el reintento del cierre lo recupera, el aviso final es el de un guardado bueno. **El PO lo acepta** | No aparece un error falso cuando todo quedó guardado |

También se aplicaron DT4a-03 a -06, -08 y -14, sin efecto visible, y DT4a-11 y -12, que pasaron a DV-39 y DV-37. Detalle en [02-tech-design §10.1](02-tech-design.md).

---

## 7. Issues abiertos y deuda

### 7.1 Issues de QA (todos menores)

Los dos mayores de la verificación quedaron cerrados en la ronda 2:

- **QA-V4a-01**: si tocabas `Entrenamiento libre` durante los 200 ms en que se cerraba el inmersivo anterior, la sesión nueva arrancaba sin HUD, sin `Pausa` ni `Terminar`. Ahora cada sesión abre su propio inmersivo en el mismo toque, como el legacy.
- **QA-V4a-02**: al salir del inmersivo seguían corriendo el latido del corazón y el pulso de la fila del paso, y la app quedaba a 60 cuadros por segundo hasta cerrarla. Ahora paran al salir.

También se cerraron QA-V4a-07, ROB-03, ROB-04 y el arreglo de ROB-05. Quedan 8 menores:

| ID | Qué pasa, en simple | Impacto | Quién | Cuándo |
|---|---|---|---|---|
| QA-V4a-03 | El divisor de la banda no se ve en el teléfono ni en la ventana baja | Una línea de adorno. Hace fallar AC-WKT-17-04 | Architect, con el visto bueno del Designer | **Condición: al empezar la 4b** |
| QA-V4a-04 | El primer Tab después de usar el mouse enfoca sin anillo; en el editor, un Enter con ese foco sale sin guardar | Teclado. Hace fallar AC-APP-05-07 | Architect | **Condición: al empezar la 4b** |
| QA-V4a-05 | Al cerrar el inmersivo con números rodando, Unity anota avisos de "DPI" en el registro (88 en la PC) | Solo el registro, y un poco de trabajo de más que termina solo | Architect | Al empezar la 4b |
| QA-V4a-06 | Todavía no se integraron las cifras de tablero (RowMate Numeric SC) ni el kerning fino de los títulos y del HUD. Hoy el `1` tiene un poco de aire de más y algún par de letras se ve suelto (`Te` de `Terminar entrenamiento`) | Aspecto. Ningún criterio lo pide | Architect, con el visto bueno del Designer | Al empezar la 4b, con QA-V4a-03, en una sola regeneración de capturas |
| QA-V4a-08 | `Builds/Windows/simulator` tiene el build de antes de los arreglos, porque tu `RowMate.exe` lo tenía abierto | Quien abra ese build todavía puede ver los dos mayores, que en el código ya están corregidos. El build de la carpeta de prueba es el bueno | Coordinador | Cuando cierres tu `RowMate.exe` |
| QA-V4a-L01 | Un monitor que no informa la distancia una vez por segundo podría dejar un paso por distancia sin terminar nunca | Con el simulador no pasa. **El PO lo decidió** (§8.2) | Architect y QA | It. 10a, con el AMS-670B |
| QA-V4a-L02 | El oráculo del motor todavía no compara contra el legacy algunos caminos que la lente probó a mano, y que hoy dan iguales: pasos de 0 s y negativos, terminar en pausa, dos sesiones seguidas, entre otros | Protección contra regresiones | Architect | It. 4b, con el diálogo de completado |
| QA-V4a-ROB-05 (pregunta) | Si Windows suspende el equipo con una sesión en marcha, ¿el tiempo suspendido cuenta como tiempo de sesión? | **Es P-17**, para tu gate (§8.1) | Vos; después, el PO y el Architect | Gate de la 4a; el criterio, en la 4b |

### 7.2 Otra deuda

- **Para la It. 4b**:
  - el HUD ya está en 60 elementos en el peor caso, sin margen para las píldoras: la 4b tiene que juntar elementos (R4a-03);
  - el motor tiene que esperar al diálogo de completado antes de borrar los objetivos (DT4a-15);
  - kcal salta de lugar, en vez de deslizarse, cuando aparece o se va BPM; y el "…" de la pastilla recortada sale en cian ([04-design-notes §17.6](04-design-notes.md));
  - durante el fundido de salida (200 ms), el HUD muestra la foto de reposo, con la banda en `00:00` (O-01): se revisa con la salida del inmersivo;
  - el Designer precisa el panel del simulador en las pestañas ([00-deuda-diseno §1.6 y §1.7](00-deuda-diseno.md)).
- **Para la It. 7**: en la ventana baja, con el simulador, la llave queda dentro de la zona del bote. Hoy es fondo liso; se revisa cuando el bote se vea.
- **Para la 10a**:
  - QA-V4a-L01;
  - el reloj de Android con la pantalla bloqueada (RA-19) y que Android cierre la app en segundo plano (RA-08);
  - un cuadro tarde con el remo real (DT4a-10);
  - el cierre de la sesión encolado en el acto, la variante completa de ROB-04;
  - el *lock* del reloj de la app para los avisos del Bluetooth real (viene de la It. 2).
- **Para la It. 14**: los lectores de pantalla (DT-05), la escala de Windows con dos monitores y la pantalla encendida solo en Dispositivo y en el entrenamiento (BKL-24).
- **Deseables, sin criterio (P2, de la It. 3)**: `Guardar` con un indicador si tarda más de 300 ms; un subtítulo que pasa a dos líneas crece animado; la mini-barra interpola sus tramos.
- **Con teléfono**: de la It. 3, el teclado del sistema y el menú de texto; de la 4a, "Atrás" en el inmersivo, el segundo plano y el primer cuadro al volver, y la pantalla encendida.
- **CI**: `ci-core` corre en GitHub; `ci-unity` espera la licencia (P-08).

### 7.3 Riesgos

| ID | Estado |
|---|---|
| R3-01 / R-T6 (IL2CPP con el SQLite nativo) | **Abierto** hasta que esté el módulo |
| R3-02 y R3-13 (Android: carpeta, teclado, "Atrás" y menú de texto) | **Abiertos** hasta que estén el módulo y el teléfono |
| R4a-01, R4a-10 / RA-08 y R4a-12 / RA-19 (el segundo plano en Android) | **Abiertos**: se miden en la 10a, con el teléfono |
| R4a-03 (el presupuesto de elementos del HUD) | **Abierto** para la 4b |
| R4a-07 (el pedido de pantalla encendida, por hilo) | **Mitigado**: se libera en el mismo hilo que lo pidió, y está probado |
| R4a-13 (un alcance muy largo al volver) | **Mitigado y medido**: 8 h en menos de 500 ms, con una sola foto |
| R3-08 (iniciar una rutina imposible) | **Cerrado**: se decide sin expandirla (AC-WKT-12-08) |

---

## 8. Decisiones que tenés que confirmar

Si no decís nada, se aplica lo que dice "Por defecto". Alcanza con un "OK a todo", o con decir cuál querés cambiar. Las decisiones de los gates de las It. 1 a 3 ya las aceptaste.

### 8.1 Gate de la It. 4a

| Decisión | Qué significa | Por defecto |
|---|---|---|
| **Q-12** | La telemetría se guarda mientras remás, punto por punto. Una sesión cortada conserva sus puntos, y una terminada normalmente queda igual que en el legacy. El legacy la tiene en memoria hasta terminar, y un cierre la pierde entera | Se adopta |
| **D-ART-01** | Fuera de objetivo, el número va en `#FF625B` con una flecha ▲ o ▼, y el contraste del HUD es de 7:1 en blanco y en las métricas no rojas (CAP-19, CAP-20, CAP-24) | Se adopta. Si no: `#FF4444` sin flecha, como el legacy |
| **D-ART-02 / P-05** | El HUD es un 15 a 25 % más grande y crece con la pantalla hasta 1,6×: en la ventana de 1280 × 720 el SPM mide 102 dp, contra 52 en el legacy. No sigue la letra del sistema (CAP-17) | Se adopta. Si no: los tamaños del legacy, sin escala |
| **D-ART-10 / P-12 (DV-39)** | La llave del simulador va abajo a la izquierda y, en el entrenamiento, sobre kcal y BPM en vertical o junto a `Pausa` en horizontal. El panel se abre desde la llave y deja a la vista Split y Watts (DEMO-03) | Se adopta |
| **DV-21 a DV-39** | Las diferencias visibles de §6.1. Las que más se notan: `Próximo:` en español (DV-24, P-01), las pastillas con el texto en blanco (DV-38, P-15), la tarjeta de Workout con descripción y mini-barra (DV-21, P-06), un toque, una sesión (DV-30) y la rutina que no se puede iniciar (DV-28) | Se mantienen todas |
| **P-02** (informativo) | Hasta la 4b, al terminar sola la rutina se avisa `¡Entrenamiento completado!`. En la 4b lo reemplaza el diálogo de rutina completada, que queda hasta que lo tocás, como el legacy | Sí |
| **P-03** (sin urgencia) | Cerrar la ventana a mitad de una sesión la deja cortada, como un corte de luz, y no la termina como `Terminar`. En Android y iOS no hay un aviso confiable de cierre; la It. 13 repara las cortadas | Queda cortada |
| **P-17** (nueva al cerrar) | Si suspendés el equipo con una sesión en marcha (a mano o cerrando la tapa; por inactividad no se suspende mientras la app está abierta), ¿el tiempo suspendido cuenta como tiempo de sesión? Si contara, al despertar la sesión sumaría ese tiempo, con un punto cada 5 s de la última lectura, sin nadie remando. El legacy pierde esos segundos. Hoy no está medido qué hace la app. El criterio va en la 4b y lo verificás vos en esa demo, porque QA no suspende tu equipo | **No cuenta**: al despertar, la sesión sigue desde donde quedó, como en el legacy |

### 8.2 Decididas por el PO al cerrar (no necesitan tu respuesta)

- **DT4a-17**: se acepta. Si el reintento del cierre recupera un lote de puntos que falló, el aviso final es el de un guardado bueno. AC-WKT-06-12 quedó precisado.
- **QA-V4a-L01**: P-04 y DV-26 suman el caso. Queda un requisito de producto para la 10a: un paso por distancia nunca queda trabado si el monitor informa avance. La 10a mide la cadencia del AMS-670B y, si hace falta, cambia el saneamiento en el paso y en los parciales a la vez.
- **QA-V4a-03 y QA-V4a-04**: son las condiciones del cierre (§5.1). En el editor de Rutinas no se agrega una confirmación al salir, porque el legacy tampoco la tiene: se corrige el anillo.
- **QA-V4a-05, QA-V4a-06 (las cifras y el kerning) y QA-V4a-L02**: pasan a la 4b. QA-V4a-08 es del coordinador.
- **Las observaciones de QA**:
  - con el ruido del simulador, a 160 W contra un objetivo de 160 la tarjeta alterna: es la regla sin margen del legacy;
  - el panel del simulador está en español con la app en inglés, como en la It. 2: es una herramienta de desarrollo;
  - con el aviso sin remo, la llave pisa la esquina de la última tarjeta, apagada, sin tapar ninguna acción;
  - el "…" de la pastilla recortada sale en cian;
  - la foto de reposo durante el fundido de salida (O-01) se revisa en la 4b, y la llave dentro de la zona del bote en la ventana baja, en la It. 7.
- **AC-HIS-20-04 (tus bases)**: se da por cumplido. La comparación de antes y después no sirvió porque tu propio `RowMate.exe` escribió en `rower_app_dev.db`, pero ninguna corrida de QA usó esa carpeta.
- **Pendientes del PO de la spec (§15)**: hechos.
  - En el ROADMAP: F-WKT-14 en la 4a, `#FF625B` en la demo y en D-ART-01, y D-ART-10 y P-17 en la tabla de decisiones; además, P-17 como criterio de la 4b y el paso por distancia que nunca se traba, en la 10a.
  - En FEATURE_INVENTORY, Q-55 y Q-60; en el inventario del motor, seis precisiones del legacy.
  - El detalle está en [01-po-spec §17](01-po-spec.md).

### 8.3 Opcional, sin urgencia

- **Licencia de Unity (P-08)**: ¿cargás tu licencia Personal de Unity como secreto del repositorio? Hace falta para que corra `ci-unity`.
- **CAP-27 de la It. 3** (la ventana del build normal con la barra de título oscura): sigue disponible a pedido, como explica el [REPORT de la It. 3 §8.3](../03-rutinas/REPORT.md).

---

## 9. Prerrequisitos para la It. 4b

### 9.1 De tu lado

| Qué | Para qué | ¿Obligatorio? |
|---|---|---|
| Tu OK a §8.1 | Cerrar el gate | Sí (alcanza con "OK a todo"). Conviene que contestes P-17, porque define un criterio de la 4b |
| Cerrar el `RowMate.exe` que quedó abierto | Que se pueda recompilar `Builds/Windows/simulator` (QA-V4a-08) | Sí |
| Los 10 minutos de §3.1 | Ver la iteración en la ventana real | Recomendado |
| El minuto de §3.7 | Cerrar AC-APP-14-01 a -03 | No |
| Módulos **Windows Build Support (IL2CPP)** y **Android Build Support**, y un Android 8.0+ de 64 bits con depuración USB | Cerrar AC-WKT-02-10 y las partes Android. En la 4b, ver las barras del sistema ocultas en el inmersivo (F-APP-15), que sin teléfono quedan NO VERIFICABLE | No para la 4b. El teléfono hace falta a más tardar en la 6a |
| Licencia de Unity como secreto (P-08) | Que corra `ci-unity` | No |

La It. 4b no necesita nada nuevo de tu lado: se prueba con el simulador. En su gate te vamos a mostrar D-ANI-05 (la cuenta grande de los últimos 3 s y el sello "Objetivo cumplido / No alcanzado") y la entrada al inmersivo como "sumergirse".

### 9.2 Del equipo, al empezar la It. 4b

1. **Architect**:
   - primero, las dos condiciones (QA-V4a-03 y QA-V4a-04), cada una con su prueba;
   - QA-V4a-06 (las cifras y el kerning) junto con QA-V4a-03, en una sola regeneración de las capturas de las It. 2, 3 y 4a, con dos corridas seguidas;
   - QA-V4a-05, y QA-V4a-08 en cuanto cierres tu `RowMate.exe`;
   - en el diseño de la 4b: los guiones del oráculo (QA-V4a-L02), la espera del diálogo de completado en el motor (DT4a-15) y el presupuesto de elementos del HUD para las píldoras (R4a-03).
2. **Designer**:
   - el visto bueno del divisor y de las cifras nuevas;
   - [00-deuda-diseno §1.6 y §1.7](00-deuda-diseno.md);
   - la salida del inmersivo (O-01), la entrada "sumergirse" y las píldoras.
3. **PO**: escribir la spec de la 4b (`iterations/04b-hud-exr/01-po-spec.md`), con:
   - las píldoras y sus estados (F-WKT-14, F-WKT-15; Q-52, Q-59);
   - el reloj de pared (F-WKT-19) y el chip de fase de la barra libre (F-WKT-16);
   - el diálogo de rutina completada (F-WKT-21), que reemplaza el aviso provisorio (DV-34, P-02, Q-53);
   - las transiciones, el velo de pausa y las barras del sistema ocultas (F-APP-15);
   - D-ANI-05;
   - P-17, según lo que contestes;
   - AC-WKT-17-04 y AC-APP-05-07, otra vez.
4. **QA**: verificar las condiciones y la regeneración antes de construir encima.

---

## 10. Por qué el PO acepta la It. 4a

- **El objetivo se cumple.**
  - Desde Workout se arranca un entrenamiento libre o una rutina, y se rema con un HUD funcional: las métricas grandes, el semáforo con flecha y `obj.`, el paso con su cuenta regresiva y `Próximo`, y pausa, reanudar y terminar con confirmación.
  - La sesión y su telemetría se guardan mientras se rema: en el build real, una sesión matada a mitad quedó con sus 31 puntos.
  - El entrenamiento sigue minimizado (2 minutos, 24 puntos), y la app le pide a Windows que no apague la pantalla: las pruebas que lo piden de verdad pasan, y en la PC falta mirarlo con `powercfg` (§5.2).
  - La demo de la spec se ve toma por toma en DEMO-01 a DEMO-14, con la interfaz real y SQLite real, y QA la repitió en la PC con el build de simulador.
- **La paridad está probada contra el legacy.**
  - El motor del legacy corrió fuera del legacy y el nuevo da los mismos puntos, totales, cambios de paso y objetivos en sus 11 vectores y en 13 de los 15 escenarios extra; los otros dos son una diferencia a propósito y un riesgo con fecha.
  - Los 84 casos de formatos dan el mismo texto, y los 11 tests del legacy están portados y en verde.
  - Cada una de las 78 filas de la checklist de paridad tiene su criterio.
- **La base es confiable.**
  - Las mismas pruebas pasan en .NET y dentro de Unity, con simulador y sin él, y la interfaz se prueba montada (328 pruebas).
  - Las capturas son deterministas, y el build normal no lleva nada del simulador.
  - Los dos mayores que encontró QA se corrigieron, cada uno con una prueba que antes fallaba, y QA los volvió a verificar con sondas propias en Unity y, el segundo, también en la PC.
  - Tus datos y los del legacy no se tocaron.
- **Lo que falla es menor y tiene fecha.**
  - Son dos criterios de 162: una línea de la banda que no se ve en el teléfono, y el anillo del primer Tab después de usar el mouse.
  - Ninguno cambia un dato ni un número, y los dos se corrigen al empezar la 4b, antes de construir encima.
  - Lo demás son 6 observaciones menores, con dueño y fecha.
- **Lo único que falta es tu OK** a §8.1, la respuesta a P-17 si querés otra cosa, y, si podés, los 10 minutos de §3.1.
