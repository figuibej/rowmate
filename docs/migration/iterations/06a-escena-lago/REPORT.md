# It. 6a — Escena: cielo, luz, hora, Lago alpino y agua degradada: reporte de cierre

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-11-06 |
| Estado | **Cerrada, espera tu OK** |
| Veredicto del PO | **ACEPTADA, con dos criterios en FAIL que van primero en la 6b.** Detrás de tus métricas ya no hay un fondo liso: hay un **lago de montaña en 3D** que avanza a la velocidad que remás, con cielo, sol, luna, estrellas, nubes, montañas nevadas, pinos, un muelle y una cabaña **en los mismos lugares que en la app legacy**. La luz sigue la hora real (o la que elegís con `Hora escena` en el simulador) y cambia como un *time-lapse*. Entrar a entrenar es un vuelo corto de la cámara hasta el agua; la pausa le quita color a la escena y el bote se desliza hasta quedar quieto; al terminar una rutina la cámara se eleva. Todo lo que es regla (la cámara, la velocidad, la hora, los colores, los astros, las estrellas, la orilla) sale del código del legacy y se compara con él número por número. Cumplen 98 de 101 criterios. **Fallan 2, chicos y fuera del uso normal**: si en el **primer arranque** entrás a entrenar en los primeros segundos, mientras la escena todavía se prepara, la entrada tiene dos cuadros de ≈ 20 ms (el tope es 16,7; con la escena lista, 13,6 ms), y el agua dibuja una línea de brillo de más, de 1 px y casi transparente, en el borde de abajo. Los dos se corrigen al empezar la 6b, antes de construir encima (§7). El otro que no cumple es el de siempre: los 60 fps en un teléfono Android, que no tenemos |
| Veredicto de QA | **APROBADO CON OBSERVACIONES** en la ronda 3 ([05-qa-verification](05-qa-verification.md), "Ronda 3"). Fueron tres rondas: en la 1, QA y la lente de rendimiento encontraron 13 problemas mayores (la escena que llegaba tarde aparecía de golpe, no había fundido al rotar, la toma aérea quedaba detrás del diálogo, medidas de rendimiento hechas con un build viejo, un armado de la orilla en un cuadro visible, una fuga de memoria nativa…); en la 2 estaban todos corregidos y apareció uno más (el HUD con rutina reservaba memoria en cada cuadro, de la 4b/5), corregido para la 3. En la 3, QA probó a mano el build y encontró el tirón del primer arranque (QA-V6a-R3-01), y quedaba abierto el menor de la línea de brillo que había visto la lente de [paridad](05-qa-lens-paridad.md) (QA-V6a-PAR-01). Las lentes de [paridad](05-qa-lens-paridad.md) y [rendimiento](05-qa-lens-rendimiento.md) quedaron con sus mayores cerrados |
| Documentos | [01-po-spec](01-po-spec.md) (qué se pidió, con "Al cerrar la iteración") · [02-tech-design](02-tech-design.md) (cómo se hizo, con los cambios por implementación y por las rondas 1 y 2) · [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) (control previo) · [04-design-notes](04-design-notes.md) (arte, luz y movimiento, con WP-D1 y WP-D2) · [05-qa-verification](05-qa-verification.md) (tres rondas), con sus lentes [05-qa-lens-paridad](05-qa-lens-paridad.md) y [05-qa-lens-rendimiento](05-qa-lens-rendimiento.md) · [00-deuda-5](00-deuda-5.md) y [00-deuda-diseno](00-deuda-diseno.md) (la deuda de la 5, saldada al empezar) · [snapshots/INDEX.md](snapshots/INDEX.md) (capturas) |

---

## 1. En pocas palabras

La It. 6a reemplaza el fondo provisorio del entrenamiento por la primera escena 3D de la app.

- **El lago.** Un Lago alpino con el agua en su modo "degradado" (un degradé con líneas de brillo, como el agua de respaldo del legacy; el agua con ondas, reflejos y estela es de la 6b). Montañas al fondo, con nieve; pinos en las dos orillas; un muelle con pilotes y una cabaña; pasto, juncos, piedras y bosque detrás. **Nada flota**: todo está apoyado en el terreno.
- **El mismo lago que el legacy.** Cada pino, muelle y cabaña está **en el mismo lugar que en la app legacy a la misma distancia** (Q-61): el generador aleatorio del legacy se portó bit a bit y la orilla se compara con la del legacy en los segmentos 0 a 2 000 (hasta 100 km). En las capturas con marcas (CAP-02, -05, -07), cada objeto está sobre la marca que calcula el código del legacy, a 0 dp.
- **El bote avanza a la velocidad del split**, con inercia: acelera en ≈ 1,5 s y, en pausa, se desliza ≈ 6 m hasta quedar quieto en ≈ 9 s. La distancia de la escena es propia: no se muestra ni se guarda y no toca la sesión. El bote es provisorio: el casco solo, sin remero (llega en la 7).
- **La cámara es la del legacy** (la misma posición y la misma proyección, comprobadas en siete tamaños de pantalla), y además:
  - **respira**: se mece apenas, sin mover el horizonte más de 3 dp;
  - **entra volando**: al tocar `Entrenamiento libre`, desde atrás y arriba hasta el agua, en ≈ 1,8 s, mientras llega el HUD;
  - **sale subiendo** cuando terminás, con el fundido a negro;
  - **se eleva al final de una rutina** (la toma aérea), detrás del diálogo de completado.
- **La hora.**
  - El cielo, la luz, las sombras y el agua siguen la **hora real** de tu PC, con los colores del legacy minuto a minuto.
  - **El sol se pone detrás de las montañas** y la luna sale detrás del paisaje: nada salta en el cielo (en el legacy, a las 19:34 el sol a la derecha se convierte de golpe en la luna a la izquierda). Las líneas de brillo del agua se apagan con el sol y vuelven plateadas con la luna.
  - **80 estrellas** en los lugares del legacy, solo de noche y al amanecer (de 18:00 a 6:12); entran y salen con un fundido.
  - **La luna con la fase del día** (DV-57).
  - Las luces de las cabañas se prenden de a una al anochecer.
  - **`Hora escena`** en la llave del simulador: `+` y `−` cambian la hora de la escena en 1,5 s, como un *time-lapse*; `Hora real` vuelve a la de la PC. El reloj del HUD sigue siempre la hora real, y la hora elegida no sobrevive a cerrar la app.
- **El HUD sobre la escena.** Es el de la 4b y la 5, legible a cualquier hora (con dos degradés suaves detrás de la barra y de los controles, DV-67). La escena no le suma ningún elemento: sigue en 56-57 de 60. El velo de pausa ahora es de la escena: menos color y un poco más oscura.
- **Si la escena no está lista** al entrar (por ejemplo, en el primer arranque), la entrada sigue con el fondo provisorio de la 4a y la escena aparece con un fundido de 0,6 s, ya al día, sin que la sesión espere.
- **Fluidez.** La escena tiene tres niveles de detalle (Alto, Medio y Bajo); la app elige uno sola y, si el equipo no llega, baja detalle antes que fluidez. En Workout la escena queda preparada **sin dibujarse**. En esta PC: remando 5 minutos, ningún cuadro pasa de 5 ms (§4).
- **Todo el arte es gratis y procedural**, detrás de 52 *slots* que hoy usan su versión procedural sin avisos. El Terrain Sample Asset Pack **no hace falta** (P-02). Lo único de terceros que se portó es el generador aleatorio de la VM de Dart (BSD-3): su aviso va en `THIRD_PARTY_NOTICES.md` y al lado del ejecutable.

Se comporta como el legacy:

- la cámara, la velocidad y la distancia, la hora efectiva, la paleta de cada minuto, el astro, las estrellas, las nubes, la orilla, la luz y la niebla de la orilla y el agua degradada salen del código del legacy, y **un oráculo que corre ese código** (con Flutter, sin tocar el legacy) da los valores que las pruebas comparan, sin tolerancia en lo que es exacto;
- los **26 casos legacy** de la escena (LT-62 a LT-87: `scene_camera_test`, `scene_state_test`, `time_of_day_test`, `environment_test`, `sky_shore_painter_test`, `water_painter_test` y `scene_view_test`), portados con su nombre;
- el oráculo también **dibuja con el código del legacy** las imágenes de referencia LEG-01 a LEG-07, para comparar lado a lado (§3.6).

Lo que se ve distinto del legacy, a propósito (§6.1): el sol más chico y brillante, en un arco de 12°; las montañas más bajas en vertical y con más nieve; el cielo con bruma en el horizonte; las estrellas solo de noche; la luna con fase; los degradés detrás del HUD; las montañas que se acercan en lugar de correrse de costado. **Van a tu confirmación en el gate (P-18, §8)**.

Además:

- **La deuda de la 5 quedó saldada al empezar** ([00-deuda-5](00-deuda-5.md)): QA midió en el build de la 5 las tres entradas (ningún cuadro de más de 14,7 ms) y los paneles (p99 ≤ 1,4 ms), e hizo la prueba a mano en la ventana (mouse y teclado, reiniciar con los paneles recordados, `Simular desconexión`, la cuenta grande, el sello, `PAUSA`, matar el proceso): todo bien.
- **El HUD con rutina ya no reserva memoria en cada cuadro** en el build (lo encontró QA en la ronda 2; venía de la 4b/5): de ≈ 840 cuadros con reservas cada 10 s a 49-53, que son los cambios de paso y la lectura de cada segundo.
- Hay **3049 pruebas en dotnet** (más las 192 de las herramientas), contra 2541 al cerrar la 5, y **553 pruebas de la interfaz montada** con el simulador, contra 495.
- Las **34 fotos y 11 videos** los genera Unity con la escena y la interfaz reales y salen iguales byte a byte en dos corridas.
- Ni las pruebas, ni las capturas, ni las corridas en el build tocaron tus bases, tus preferencias ni el legacy.

Lo que no se pudo ver:

- **60 fps en un Android de gama media** (AC-SCN-01-09): no hay módulo de Android ni teléfono. Queda para la It. 14 (o antes, si conseguís el teléfono).
- **La suspensión real de tu equipo** (AC-WKT-02-11, de la 4a): la demo (c) de la 4b sigue pendiente. Esta vez, en §3.1 (d).

---

## 2. Qué se entregó, por funcionalidad

"Cumple" quiere decir que pasa todo lo que se pudo verificar.

| ID | Qué quedó listo | Criterios | Dónde se ve |
|---|---|---|---|
| F-SCN-02 | **La cámara de persecución** del legacy (posición, proyección y su inversa, en siete tamaños), el recorte de lo cercano (lo que se acerca a menos de 2 m se desvanece), la respiración (rolido de 0,15°, el horizonte dentro de ± 3 dp), el vuelo de entrada (1,8 s), la salida y la toma aérea del final. La escena no recibe toques | 8 de 8 | VID-01, VID-02, VID-07, VID-08, VID-10, CAP-22 |
| F-SCN-03 | **La velocidad y la distancia de la escena**: la velocidad por el split, la inercia, la frenada en pausa o con SPM 0, el paso acotado, una distancia propia que no se muestra ni se guarda. Durante un corte de lecturas el bote sigue (como el legacy, P-16) | 7 de 7 | VID-03, VID-04 |
| F-SCN-09 | **La hora y la paleta**: la hora real o la forzada; los colores del legacy en cada minuto del día (contra el oráculo, minuto a minuto); la luz de cada hora; las luces de la noche, que se encienden de a una | 6 de 6 | CAP-01 a CAP-04, VID-05, VID-06 |
| F-SCN-10 | **El sol, la luna y la luz**: el día y la noche del legacy; el astro en la misma zona del cielo y con la misma progresión (DV-52); la puesta y la salida detrás del paisaje, sin saltos (como mucho 0,3° por minuto); la luz y las sombras; **la luna con fase**, también en el hemisferio sur | 7 de 7 | CAP-04, CAP-08 a CAP-11, VID-05 |
| F-SCN-11 | **El cielo**: el degradé con los dos colores del legacy en sus lugares (con bruma en el horizonte, DV-66), las 80 estrellas de `Random(42)` solo de 18:00 a 6:12 y con fundido, el sol con su brillo y su halo, la luna, y las 7 nubes del legacy, que se mueven con el viento | 7 de 7 | CAP-02, CAP-04, CAP-11, `it6a-brillo` |
| F-SCN-14 | **La orilla determinista**: segmentos de 50 m con la semilla y el generador del legacy (vectores dorados del oráculo), lo visible de 1 a 400 m, el orden, el tope y la prioridad de los hitos, el olvido de lo que quedó atrás. Lo lejano aparece con un fundido entre 400 y 360 m | 7 de 7 | VID-03, CAP-12 |
| F-SCN-15 | **El Lago alpino**: pinos, muelles y cabañas con sus medidas y colores, el tope, el terreno de la orilla, los objetos sobre sus marcas del legacy, lo que se suma (pasto, juncos, piedras, bosque) sin tapar el paisaje del legacy y los *slots* con su versión procedural, sin avisos | 8 de 8 | CAP-02, CAP-05, CAP-07 contra LEG |
| F-SCN-23 | **`Hora escena`**: cambia cielo, luz y agua en 1,5 s por el camino más corto del reloj (en el acto con reducir movimiento), no se guarda y el reloj de pared no la sigue | 4 de 4 | DEMO-04 a DEMO-08, VID-05, VID-06 |
| F-SCN-01 (también toca) | **La composición y el ciclo de vida**: la escena detrás del HUD, de cero en cada entrenamiento, sin reinicios al pausar, al ir a segundo plano o al rotar; apagada fuera del inmersivo (en Workout no se dibuja); la escena que llega tarde con su fundido; el bote dentro de su zona; tres niveles de detalle y la adaptación; 0 bytes por cuadro; sin saltos a 42 km; las reglas en lógica pura probada con dotnet | 13 de 14; 1 no verificable (60 fps en Android) | CAP-12, CAP-13 a CAP-17, CAP-24, VID-09, VID-11 |
| F-SCN-13 (también toca) | **El agua degradada**: los tres tramos del legacy y las líneas de brillo bajo el astro, que se apagan con él. **Falla AC-SCN-13-03 en pantalla**: dibuja una 11.ª línea en la fila de abajo (el dato tiene las 10 del legacy, bit a bit) | 3 de 4; **1 falla** (QA-V6a-PAR-01) | CAP-01 a CAP-07 |
| F-SCN-19, -20 y -21 (también tocan) | Las cordilleras (quietas de costado, se acercan apenas; nieve), el pino, la cabaña y el muelle con la luz, la niebla y la noche del legacy, y el lago como escenario por defecto y único (el selector es de la 8) | 5 de 5 | CAP-01 a CAP-07 |
| F-WKT-17 (también toca) | **Las píldoras legibles con la escena detrás** (AC-WKT-17-13): las hechas, la hueca y la fila de información, con una nube pasando por detrás. Tonos `.hud` un poco más claros para el contraste | 1 de 1 | CAP-19 |
| De iteraciones anteriores (23) | **11 que cambian**: la entrada con el vuelo y la salida con la escena (AC-WKT-13-06, -13-08), el velo de pausa sobre la escena (-13-12), la fila de 200 píldoras con la escena (-14-19), el fundido al rotar (-17-07), la zona del bote (-17-09), `Hora escena` (AC-BLE-16-07), el fondo provisorio que espera a la escena (AC-SCN-01-01), reducir movimiento con la escena (AC-APP-05-08), el HUD sobre la escena (AC-WKT-13-02) y la entrada sin tirones (AC-WKT-13-15). **12 que se vuelven a verificar** con la escena: nada sigue al salir, el dato que la presentación no toca, el segundo plano, los paneles, la llave, `Simular desconexión`, tus datos y el diálogo de completado | 22 de 23; **1 falla**: AC-WKT-13-15 en el primer arranque si se entra en seguida (QA-V6a-R3-01) | — |

Además:

- **Los 26 casos legacy** (LT-62 a LT-87) pasan: 17 en dotnet y en Unity, 9 en la interfaz montada.
- **El oráculo de escena**: corre el código del legacy y escribe seis archivos de valores (la paleta de cada minuto, la luz, el astro, las estrellas, las nubes, el generador, la orilla del lago y el agua) y las diez imágenes LEG. La lente de paridad lo regeneró desde el legacy y salió idéntico.
- **Herramientas para el equipo**:
  - el arnés de capturas `it6a`, con 45 tomas y los valores de cada cuadro de los 11 videos;
  - en el build de simulador, `-rowmate-perf-scene`, `-rowmate-perf-hours`, `-rowmate-perf-completion`, `-rowmate-perf-long-series` y `-rowmate-scene-delay` para medir la fluidez con la escena (solo en desarrollo), y `tools/build/perf-log-check.ps1`, que cuenta los *shaders* que se compilan después de que la escena quedó lista (tienen que ser 0);
  - `SnapshotCli.SeedDatabase -seed F4B` (la base con `Serie larga`).
- **Documentos al día**:
  - el ADR 022 (cómo llega la escena a la pantalla) y los cambios de ARCHITECTURE, ADR 015 y LEGACY_MAPPING;
  - ART_BIBLE, ANIMATION_BIBLE y ASSET_SLOTS, del Designer, con la lista definitiva de Mixamo (§9.2);
  - al cerrar: el ROADMAP y el inventario `immersive-scene` ([01-po-spec, "Al cerrar la iteración"](01-po-spec.md)).

---

## 3. Cómo ver la app

### 3.1 La demo en el build de Windows con simulador

**El build.** `Builds/Windows/simulator/RowMate.exe` **ya tiene la It. 6a**: QA lo generó en la ronda 3 con el código final. Si hace falta volver a generarlo, con el Editor de Unity **cerrado** y desde la carpeta del repositorio:

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator
```

**Antes de empezar:**

- **Un solo `RowMate.exe` a la vez** (comparten la base y las preferencias).
- **El Firewall.** Si aparece el aviso "Seguridad de Windows", contestá **`Cancelar`**: la app no usa la red.
- **El idioma.** El HUD sigue el idioma de Windows; el texto en inglés va entre paréntesis. El panel del simulador (la llave) está siempre en español, y la escena no tiene textos.
- **El simulador del build tiene "ruido"**: los números van con "≈".
- **La primera vez que abrís la app**, esperá **unos 15 segundos en Workout** antes de tocar `Entrenamiento libre`: la escena termina de prepararse en ese tiempo. Si entrás antes, todo funciona, pero ves el fondo azul provisorio unos segundos hasta que la escena aparece con un fundido, y hay un pequeño tirón (es QA-V6a-R3-01, §7). Si querés verlo, hacé la prueba (e).

**(a) Remar en el lago** (3 minutos)

1. Abrí `RowMate.exe`, andá a **Workout** y esperá unos 15 segundos. → DEMO-01
2. **`Entrenamiento libre`** (`Free workout`). Workout funde a negro y la cámara baja volando desde atrás y arriba hasta quedar detrás del bote, a ras del agua, mientras el HUD llega en cascada (≈ 1,8 s). El reloj de la banda corre desde el toque. → DEMO-02, VID-01
3. **Mirá el lago**: la luz es la de la hora de tu PC. Montañas al fondo con nieve, pinos en las dos orillas, nubes, el agua con sus líneas de brillo bajo el sol (o la luna). La cámara se mece apenas.
4. **La llave → `Fuerte`**, y cerrá el panel con su `✕`. El bote acelera en ≈ 1,5 s (split ≈ `1:48`, ≈ 280 W) y el paisaje viene hacia la cámara: los pinos pasan por los costados; **el muelle con sus pilotes aparece a la izquierda a los ≈ 34 s** desde que tocaste `Entrenamiento libre`, sale del cuadro a los ≈ 38 s; la cabaña pasa por la derecha a los ≈ 79 s. Las montañas no se corren de costado y las nubes se mueven solo con el viento. → DEMO-03, VID-03
5. **`⏸ Pausa`** (`Pause`). La escena pierde color y se oscurece un poco, `PAUSA` aparece en su disco y el bote se desliza ≈ 6 m hasta quedar quieto en ≈ 9 s; las nubes siguen. **`Reanudar`** (`Resume`): el velo se va y el bote vuelve a tomar velocidad. → VID-04

**(b) La hora** (3 minutos). Sin salir del entrenamiento, abrí la **llave**. En la fila **`Hora escena`** dice `real`. Cada `+` suma una hora (desde `real`, arranca en la hora de tu PC) y cada `−` resta una:

1. Llevala a **`18:00`**. En 1,5 s el cielo pasa al atardecer: luz cálida y baja, el sol a la derecha casi en el horizonte, y las primeras estrellas, tenues, con un fundido. **El reloj del HUD sigue en la hora real.** → DEMO-04
2. **`19:00`** y **`20:00`**: el sol se esconde detrás del paisaje, sin saltar, y los brillos del agua se apagan con él; la luna sale por la izquierda, detrás del paisaje, y los brillos vuelven plateados; la ventana de la cabaña se enciende (si está a la vista). → DEMO-05, VID-05
3. **`22:00`**: noche con la luna (con la fase de hoy, con la parte oscura apenas visible) y 80 estrellas; las nubes casi no se ven. → DEMO-06
4. **`⏸ Pausa`** de noche: el velo sobre la noche. **`Reanudar`**. → DEMO-07
5. Seguí con `+` hasta **`6:00`** (pasa por `23:00` y `0:00`): amanecer violeta y naranja, la luna se pone detrás del paisaje, sale el sol, bruma baja sobre el agua y las estrellas se van. → VID-06, CAP-01
6. **`12:00`**: mediodía azul, el sol centrado y alto, la nieve brillando. → CAP-02
7. **`Hora real`**: vuelve a la hora de tu PC en 1,5 s. Cerrá el panel.
8. **`Terminar` → `Terminar`** (`Finish`). El HUD se va con su última foto, la cámara empieza a subir, funde a negro y vuelve Workout. → DEMO-08, VID-07

**(c) La rutina y la toma aérea** (2 minutos)

Si no la tenés, armá en **Rutinas** (`Routines`) una rutina corta:

1. `+` → en `Nombre de la rutina` (`Routine name`), escribí `Lago 1 min`.
2. `+ Paso` (`Step`) → `Calentamiento` (`Warmup`), `Por tiempo` (`By time`), `Min` `0` y `Seg` (`Sec`) `20` → `Confirmar` (`Confirm`).
3. `+ Paso` → `Trabajo` (`Work`), `Por tiempo`, `Min` `0` y `Seg` `40` → `Confirmar`. Después, `Guardar` (`Save`).

Después, en **Workout**: **`Lago 1 min` → `Iniciar`** (`Start`) y dejala correr sin tocar nada.

- A los 60 s termina sola y aparece en el centro el diálogo `¡Entrenamiento completado!`. Alrededor, la cámara **se eleva y retrocede despacio durante 6 s**: el horizonte sube, el lago se abre y el paisaje se va deteniendo mientras el bote frena.
- **El diálogo es opaco y tapa el bote** (en la ventana de 1280 × 720 asoma la popa debajo). Es a propósito hasta la 6b: con el vidrio con desenfoque, el bote se va a ver frenando detrás del diálogo (P-19, §8). → CAP-22, VID-08
- `Terminar` del diálogo → Workout.

**(d) El equipo suspendido** (P-17 de la 4a, 5 minutos). **Sigue pendiente desde la 4b y lo verificás vos**: QA no suspende tu equipo. Hacelo con el build, no en el Editor.

1. `Entrenamiento libre`. Dejá correr el reloj de la banda hasta `01:00`.
2. **Suspendé Windows a mano**: Inicio → **Encender/apagar** → **Suspender**. No sirve bloquear la sesión (Win+L), apagar el monitor ni esperar a que se duerma solo: con la app abierta Windows no se duerme solo. Esperá **1 minuto** y despertá la PC.
3. **Lo que tiene que pasar**:
   - el reloj de la banda sigue desde `01:00` (a lo sumo `01:01`), **no** desde `02:00`;
   - el chip sigue en `EN MARCHA`, la hora del HUD es la actual y la escena sigue donde estaba;
   - no aparece ningún aviso.
4. Dejá correr hasta `02:00` → `Terminar` → `Terminar`.
5. **Antes de volver a abrir la app** (al abrirla, el registro anterior pasa a `Player-prev.log`):
   - Abrí `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate\Player.log` con el Bloc de notas y buscá `durmió`.
   - Tienen que estar `[TIME] despertar: durmió 60.0 s según la medida del núcleo y 60.4 s según el aviso del sistema; …` y `[WORKOUT] el equipo durmió 60.0 s: no cuenta (P-17; aviso, medida)`, con más o menos segundos. La línea puede decir `reanuda (2)`: Windows manda dos avisos al despertar.
   - Si dice `sin aviso del sistema` o `sin medida del núcleo`, esa es la señal que faltó. Si no hay ninguna línea `despertar`, el equipo no llegó a suspenderse.
6. **Los puntos.** En una consola abierta en la carpeta del repositorio, con .NET 8:

   ```
   dotnet run --project tools/dotnet/RowMate.SessionList -- --variant simulator
   ```

   La sesión más nueva tiene **unos 25 puntos** (uno cada 5 s de los 2 minutos remados, más el final). Si el tiempo suspendido hubiera contado, tendría unos 37.

**Contanos** qué marcó el reloj al despertar y cuántos puntos tiene la sesión. Si querés, pegá también las líneas `[TIME] despertar …` y `[WORKOUT] …`, y solo esas: el resto del registro trae rutas de tu PC. Si el reloj saltó, AC-WKT-02-11 se reabre y va primero en la 6b.

**(e) Opcional: el primer arranque apurado** (1 minuto). Cerrá la app, abrila, andá a **Workout** y tocá `Entrenamiento libre` **enseguida** (en menos de 5 s). La entrada sigue sobre el fondo azul provisorio con el HUD y el reloj corriendo, y a los ≈ 5-8 s **la escena aparece con su fundido**, ya al día. Es correcto salvo dos tirones chicos (uno en el toque y otro cuando llega la escena) que QA midió en ≈ 20 ms: es lo que se corrige al empezar la 6b (QA-V6a-R3-01). → CAP-24, VID-11

Si querés probar más, en el mismo build:

- **La hora no se guarda.** Con `Hora escena` en `22:00`, cerrá la app y volvé a abrirla: la llave dice `real`.
- **Rotar o cambiar la ventana** entre vertical y horizontal (si tu monitor gira) o achicarla hasta la ventana baja: el HUD entra con un fundido de 200 ms en su lugar nuevo y la escena sigue sin reiniciarse. → VID-09
- **Reducir movimiento.** Windows → Configuración → Accesibilidad → Efectos visuales → apagá **Efectos de animación**. La entrada pasa a ser un fundido de 120 ms con la cámara ya en su lugar, sin vuelo; la cámara no respira; los cambios de hora llegan en el acto. → VID-02
- **En inglés.** Con Windows en inglés el HUD sale en inglés; la escena es la misma.

**Tus datos.** El build de simulador guarda las rutinas y las sesiones en `rower_app_dev.db`, y el normal en `rower_app.db`, dentro de `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`, junto con `prefs.json`. La escena no guarda nada. La app legacy guarda sus datos en otro lugar y la nueva no los lee ni los toca.

### 3.2 En Unity, con el simulador

Igual que en la 5 ([REPORT de la 5 §3.2](../05-series-parciales/REPORT.md)): abrí el proyecto con el Editor **6000.3.27f1**, menú **RowMate → Modo simulador**, la escena `Assets/RowMate/Scenes/Boot.unity`, el tamaño en la vista *Game* (`1280x720`, `411 x 914`, `914 x 411` o `640 x 360`) y Play. Seguí §3.1 (a) a (c); la (d) no vale en el Editor. Idioma, letra grande y "reducir movimiento" están en **RowMate → Simular**. Lo que más se luce en el Editor: el **teléfono vertical** (`411 x 914`) de noche, con la luna y las estrellas sobre el HUD (CAP-23 es la ventana), y el **horizontal** (`914 x 411`) al atardecer.

Unity admite una sola instancia por proyecto. **Antes de cerrar Unity, dejá `RowMate → Modo simulador` sin tilde**: si queda puesto, el control del CI falla a propósito.

### 3.3 Los builds de Windows

Con el Editor **cerrado**, desde la carpeta del repositorio:

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator
Builds\Windows\simulator\RowMate.exe
```

Es el build de la demo, siempre de desarrollo y en `Builds/Windows/simulator`. Si tenés abierto un `RowMate.exe` de esa carpeta, cerralo antes: el build pisa sus archivos.

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows
Builds\Windows\normal\RowMate.exe
```

Es el build normal, sin cinta, llave ni marca de desarrollo. En la 6a no se volvió a generar: para verlo con la escena, generalo con este comando. Los dos llevan `THIRD_PARTY_NOTICES.md` al lado del ejecutable (el aviso BSD-3 del generador de Dart).

El script termina con 0 si salió bien, con 2 si el build normal tiene restos del simulador, de diagnósticos o de pruebas, con 3 si falta un módulo (hoy, `-Backend IL2CPP` y Android) y con 5 si se le pide una carpeta nueva para un build de desarrollo.

### 3.4 Las pruebas sin Unity (.NET 8)

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1
```

Al final aparecen dos líneas `Passed!`: las herramientas, con 192 que pasan, y el resto, con **3049 pruebas, de las que 3010 pasan y 39 quedan ignoradas**. Las ignoradas piden un teléfono, una Mac, GitHub, la suspensión real del equipo, el build o Unity. Si ninguna línea dice `Failed!`, está todo verde.

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix
```

Escribe `tools\dotnet\.artifacts\TestResults\ac-matrix.md` con los **762 IDs**: los de las iteraciones anteriores, los 101 criterios de la 6a y LT-62 a LT-87.

- Sin los resultados de Unity: 615 PASA, 143 PENDIENTE UNITY y 4 ignorados.
- Si antes corriste §3.5, agregá `-UnityResults tools/unity/.artifacts`: **758 PASA**, 0 FALLA y 4 IGNORADO (AC-WKT-02-10 y AC-SCN-01-09 de Android, AC-APP-15-03 de Apple y AC-SCN-01-10, que se mide en el build). La matriz automática no ve los dos FAIL de la 6a: uno sale de la prueba a mano en el build y el otro de mirar la imagen (§5).

### 3.5 Las pruebas dentro de Unity

Con el Editor **cerrado** (si está abierto, el script sale con 10 y no toca nada) y **Unity Hub abierto** (si no, Unity sale con 198 por la licencia):

```
powershell -NoProfile -File tools/unity/run-unity-tests.ps1
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Simulator
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode -Simulator
```

| Comando | Total | Pasan | Fallan | Ignoradas |
|---|---|---|---|---|
| EditMode | 3137 | 3092 | 0 | 45 |
| EditMode `-Simulator` | 3329 | 3284 | 0 | 45 |
| PlayMode | 481 | 481 | 0 | 0 |
| PlayMode `-Simulator` | 553 | 553 | 0 | 0 |

- Las PlayMode montan la app real con la escena, una base y unas preferencias de prueba. Cubren la escena en los siete tamaños (las marcas del legacy con la cámara real, nada flota, el bote en su zona), el ciclo de vida (de cero, pausa, segundo plano, rotación, cierre, la escena demorada, cortar un armado a mitad sin dejar memoria nativa), la entrada, la salida, la toma aérea, el velo y reducir movimiento, los niveles y la adaptación, 0 bytes por cuadro con la escena, el HUD y los paneles (con su control positivo), y `Hora escena` con `-Simulator`.
- **Una prueba de tiempo con margen fino**: la CPU de la escena en el nivel Alto da una mediana de 1,31-1,39 ms contra un tope de 1,5 en esta PC. Si alguna vez falla sola, es esa (§7.1).
- Si Unity revienta al cerrar después de cambiar el modo simulador, el script lo dice y sigue solo.

### 3.6 Las capturas

Están en [snapshots/](snapshots/). La tabla completa, con las sondas y la auditoría de cada toma, está en [snapshots/INDEX.md](snapshots/INDEX.md).

- Las renderiza Unity con la escena y la interfaz reales, con una base de prueba, preferencias en memoria, el reloj de prueba (el reloj de pared marca `10:0x`), la fecha de prueba (28 de octubre de 2026: la luna está gibosa, ≈ 94 %) y el simulador sin ruido. No son maquetas.
- Las de "escena sola" (CAP-01 a CAP-12) van sin HUD, con la cámara quieta, como las dibuja el legacy.
- **Las LEG las dibuja el propio código del legacy** con el oráculo (sin tocar el legacy), al mismo tamaño, hora y distancia. Las de "marcas" llevan un punto naranja (en la app) o magenta (en el legacy) en la base de cada objeto del legacy.
- Para regenerarlas, con el Editor cerrado: `powershell -NoProfile -File tools/unity/capture-snapshots.ps1 -Set it6a`.

**La app contra el legacy** (escena sola, 390 × 844 y 844 × 390, en `E-920`: la cabaña a 40 m a la derecha):

| Hora | App | Legacy |
|---|---|---|
| 6 h, el amanecer (CAP-01) | ![CAP-01](snapshots/CAP-01-amanecer-v390.png) | ![LEG-01](snapshots/legacy/LEG-01.png) |
| 12 h, con las marcas (CAP-02) | ![CAP-02](snapshots/CAP-02-mediodia-v390-marcas.png) | ![LEG-02](snapshots/legacy/LEG-02-marcas.png) |
| 18 h, el atardecer (CAP-03) | ![CAP-03](snapshots/CAP-03-atardecer-v390.png) | ![LEG-03](snapshots/legacy/LEG-03.png) |
| 22 h, la noche (CAP-04) | ![CAP-04](snapshots/CAP-04-noche-v390.png) | ![LEG-04](snapshots/legacy/LEG-04.png) |

Qué mirar:

- **Los objetos están en el mismo lugar**: cada pino, el muelle (abajo a la izquierda, chico) y la cabaña sobre su marca, a 0 dp. Los colores de cada hora tienen el mismo tono dominante.
- **Lo que cambia a propósito** (§6.1): el sol más chico, más bajo y brillante (DV-52, DV-63); las montañas más bajas en vertical y con más nieve (DV-64, DV-65); la bruma en el horizonte (DV-66); la luna con su fase (DV-57); sin estrellas de día.
- **Lo que no quedó bien todavía** (QA-V6a-12, §8): a las 18 h el halo del sol detrás de la cabaña casi no se lee, y en vertical las montañas se ven lavadas comparadas con el legacy, donde son la silueta dominante. Por defecto, el Designer lo ajusta al empezar la 6b.
- Las cimas del cielo: en la imagen final, el ajuste de color de cada hora corre el color de arriba del cielo un poco (hasta 17/255 por canal a las 6 h; 0 a las 22 h); antes de ese ajuste, el color es el del legacy exacto.
- La 11.ª línea de brillo (QA-V6a-PAR-01): la más baja de todas, de 1 px y casi transparente, en el borde de abajo. Hay que ampliar mucho para verla.

**En horizontal** (CAP-05 contra LEG-05, con las marcas): las montañas tienen el alto exacto del legacy, pero la ladera cercana con bosque las tapa casi todas (QA-V6a-12).

![CAP-05](snapshots/CAP-05-mediodia-h844-marcas.png)

![LEG-05](snapshots/legacy/LEG-05-marcas.png)

Las otras comparaciones: [CAP-06](snapshots/CAP-06-noche-h844.png) contra [LEG-06](snapshots/legacy/LEG-06.png) (la noche en horizontal) y [CAP-07](snapshots/CAP-07-arranque-v390-marcas.png) contra [LEG-07](snapshots/legacy/LEG-07-marcas.png) (el arranque de toda sesión, a 0 m).

**La hora** (escena sola):

| CAP-08 · 19 h: el sol detrás de las montañas | CAP-09 · 20 h: la luna saliendo | CAP-10 · 0 h: la luna alta | CAP-11 · 15 h: sin estrellas |
|---|---|---|---|
| ![CAP-08](snapshots/CAP-08-puesta-th.png) | ![CAP-09](snapshots/CAP-09-luna-saliendo-th.png) | ![CAP-10](snapshots/CAP-10-medianoche-tv.png) | ![CAP-11](snapshots/CAP-11-tarde-tv.png) |

[CAP-12](snapshots/CAP-12-maraton-w1280.png) es el lago a 42 195 m: igual que a 0 m, sin temblores ni costuras. [it6a-low](snapshots/it6a-low-w1280.png) es el nivel Bajo y [it6a-brillo](snapshots/it6a-brillo-tv.png), la medida del brillo del sol.

**La demo, toma por toma** (ventana de 1280 × 720, simulador sin ruido, hora real de prueba `10:00`):

| DEMO-02 · la entrada: la cámara ya detrás del bote | DEMO-03 · `Fuerte`, a los 36 s: el muelle a la izquierda |
|---|---|
| ![DEMO-02](snapshots/DEMO-02-entrada.png) | ![DEMO-03](snapshots/DEMO-03-muelle.png) |

| DEMO-04 · `Hora escena` `18:00` | DEMO-05 · `20:00`: la luna saliendo |
|---|---|
| ![DEMO-04](snapshots/DEMO-04-atardecer.png) | ![DEMO-05](snapshots/DEMO-05-luna-saliendo.png) |

| DEMO-06 · `22:00`: la noche (el reloj sigue en `10:00`) | DEMO-07 · la pausa de noche |
|---|---|
| ![DEMO-06](snapshots/DEMO-06-noche.png) | ![DEMO-07](snapshots/DEMO-07-pausa-noche.png) |

[DEMO-01](snapshots/DEMO-01-workout.png) y [DEMO-08](snapshots/DEMO-08-salida.png) son Workout en reposo, antes y después: la escena preparada y sin dibujarse.

**El HUD sobre la escena** (a las 10 h, con el contorno naranja de la zona del bote):

| CAP-13 · teléfono vertical | CAP-17 · la ventana de 1280 × 720 |
|---|---|
| ![CAP-13](snapshots/CAP-13-hud-lago-tv.png) | ![CAP-17](snapshots/CAP-17-hud-lago-w1280.png) |

La banda dice `1240` `m` (el monitor) y la escena recorrió ≈ 234 m: la distancia de la escena es propia. Los otros tamaños: [CAP-14](snapshots/CAP-14-hud-lago-th.png) (horizontal), [CAP-15](snapshots/CAP-15-hud-lago-l640.png) (ventana baja), [CAP-16](snapshots/CAP-16-hud-lago-p360.png) (teléfono de 360). CAP-17 es la comparable con `docs/image-03.png` del [legacy](https://github.com/figuibej/rowmate-legacy): el HUD es el de la 4b y la vista del casco desde atrás es la misma geometría; el remero llega en la 7.

| CAP-18 · los paneles al atardecer | CAP-19 · las píldoras con una nube detrás | CAP-20 · el velo de pausa | CAP-22 · la toma aérea detrás del diálogo |
|---|---|---|---|
| ![CAP-18](snapshots/CAP-18-series-atardecer-tv.png) | ![CAP-19](snapshots/CAP-19-pildoras-nube-tv.png) | ![CAP-20](snapshots/CAP-20-velo-tv.png) | ![CAP-22](snapshots/CAP-22-toma-aerea-tv.png) |

[CAP-21](snapshots/CAP-21-velo-th.png) es el velo en horizontal, [CAP-23](snapshots/CAP-23-noche-w1280.png) la noche en la ventana y [CAP-24](snapshots/CAP-24-escena-demorada-tv.png) la escena que todavía no llegó (el fondo provisorio con el HUD).

**Videos** (30 cuadros por segundo; los valores de cada cuadro están en [manifest.json](snapshots/manifest.json)):

- [VID-01 · la entrada (2,5 s)](snapshots/VID-01-entrada.mp4): negro, la cámara que baja volando como un dron, la cascada del HUD, la pose a los 1,8 s sin salto.
- [VID-02 · la entrada con reducir movimiento (1 s)](snapshots/VID-02-entrada-reducida.mp4).
- [VID-03 · el bote avanzando (12 s)](snapshots/VID-03-bote-avanzando-w1280.mp4): los pinos y el muelle que pasan, las montañas quietas.
- [VID-04 · la pausa (14 s)](snapshots/VID-04-pausa-th.mp4): el velo en 300 ms y el bote que se desliza 5,9 m y para.
- [VID-05 · el anochecer (14 s)](snapshots/VID-05-anochecer-th.mp4) y [VID-06 · el amanecer (10 s)](snapshots/VID-06-amanecer-th.mp4): los más lindos de ver.
- [VID-07 · la salida (1,2 s)](snapshots/VID-07-salida-tv.mp4).
- [VID-08 · la toma aérea (9 s)](snapshots/VID-08-final-aereo-tv.mp4).
- [VID-09 · rotar (2 s)](snapshots/VID-09-rotar-tv-th.mp4): el HUD con su fundido; la escena sigue.
- [VID-10 · la respiración (20 s)](snapshots/VID-10-respiracion-tv.mp4), con la línea del horizonte de contrato.
- [VID-11 · la escena que llega tarde (3 s)](snapshots/VID-11-escena-demorada-tv.mp4): el fundido de 0,6 s sobre el fondo provisorio.

### 3.7 Lo que podés verificar vos (opcional, pero recomendado)

1. **La demo de la 6a** (10 minutos): §3.1 (a) a (c).
2. **AC-WKT-02-11: el equipo suspendido** (5 minutos): §3.1 (d). Es el único criterio que solo podés cerrar vos.
3. **De la It. 4a, la pantalla siempre encendida** (1 minuto): `powercfg /requests` en una consola de administrador ([REPORT de la 4a §3.7](../04a-motor-hud-funcional/REPORT.md)).
4. **De las It. 2 y 3**: el modo claro, la app legacy después de la nueva y los idiomas preferidos ([REPORT de la It. 3 §3.7](../03-rutinas/REPORT.md)).

---

## 4. Resultado de las pruebas

Son los números de la ronda 3 de QA ([05-qa-verification, "Ronda 3"](05-qa-verification.md)):

| Qué | Resultado |
|---|---|
| Compilación (dotnet) | **0 advertencias, 0 errores** |
| Pruebas dotnet | **Herramientas 192 / 192; el resto, 3049: 3010 pasan, 0 fallan, 39 ignoradas** (dos corridas). Al cerrar la 5 eran 192 y 2541 |
| Unity EditMode | **3137: 3092 pasan, 0 fallan, 45 ignoradas**; con simulador, **3329: 3284 pasan, 0 fallan, 45 ignoradas**. El modo simulador y los `ProjectSettings` quedaron como estaban |
| Unity PlayMode (interfaz montada) | **481 de 481**; con simulador, **553 de 553**. En la 5 eran 426 y 495. La repetición de las pruebas de presupuesto de la escena y del HUD, 21 de 21 |
| Matriz (762 IDs) | **758 pasan**, 0 fallan en la matriz, 4 ignorados, 0 sin prueba. Los dos FAIL de la 6a no los ve la matriz automática (§5) |
| Casos legacy | **LT-62 a LT-87**, portados con su nombre y sus afirmaciones, en verde |
| Paridad con el legacy | El oráculo corre el código del legacy y la lente de paridad lo **regeneró desde el legacy: idéntico** (los seis archivos de valores y las diez imágenes, byte a byte). Lo que el oráculo copia de lo privado del legacy se comparó línea por línea con su código. La paleta, cada minuto del día; el astro, cada minuto y en siete tamaños; la orilla, segmentos 0 a 2 000, bit a bit; los vectores dorados del generador (`Random(42)` → `0.15092545597797424`…). La 11.ª línea del agua es lo único que no coincide en pantalla (QA-V6a-PAR-01) |
| Rendimiento en esta PC (build de simulador, 1280 × 720, nivel Alto) | Remando 5 minutos con `Fuerte`: **29 ventanas de 10 s con el peor cuadro entre 3,0 y 5,0 ms** (el tope es 16,7), p99 de 2,6 a 2,9 ms, GPU ≤ 4,9 ms, memoria estable en 123-124 MB. `Serie larga` (200 pasos) con la escena: peor cuadro 7,8 ms. El diálogo de completado con la toma aérea: ≤ 13,4 ms. Ningún cuadro de régimen de más de 16,7 ms y **ninguno de más de 33 ms en ninguna corrida**. **0 *shaders* compilados después de que la escena quedó lista**. En Workout, la GPU del proceso trabaja la mitad que en el entrenamiento (la escena no se dibuja). Una corrida posterior, con otra aplicación pesada abierta en la PC, dio tiempos más altos y no se tomó en cuenta |
| La entrada (AC-WKT-13-15) | **Con la escena lista**: el primer toque del proceso 13,6 ms por el camino del usuario (14,0 en el *driver*), las siguientes ≤ 9,4 ms; las salidas ≤ 10,6 ms; la escena que llega tarde a propósito, preparada por tramos (peor cuadro 7,2 ms). **Con la precarga sin terminar** (primer arranque y entrar enseguida): **19,7-20,0 ms en el toque y 20,7-20,8 ms cuando llega la escena**, con movimiento: FAIL (QA-V6a-R3-01) |
| Presupuesto de la escena (Editor) | Por nivel: **Alto 89-90 dibujos / 60-61 SetPass / 215 k triángulos; Medio 69-70 / 45-46 / 155 k; Bajo 56-57 / 32-33 / 106 k**, dentro de los topes. CPU de la escena: mediana de 1,31 a 1,39 ms en Alto (tope 1,5; margen fino), ≈ 1,06 en Medio y ≈ 0,7 en Bajo. **0 bytes por cuadro** con la escena remando, en pausa, en un cambio de hora y de noche, y con el HUD, los paneles y la escena juntos, con el control positivo en la misma corrida. El HUD sigue en **56-57 elementos de 60** |
| Capturas | **45 tomas (34 PNG y 11 MP4) + 10 LEG**: 0 con problemas, 0 sondas que fallan; iguales byte a byte en dos corridas. Las de la 4b y la 5 se regeneraron una vez y cambian solo en la franja de la fila de píldoras (los tonos `.hud` más claros, AC-WKT-17-13); las de las It. 2, 3 y 4a, iguales. QA, la lente de paridad y el Designer miraron cada foto y los videos cuadro por cuadro; el PO revisó al cerrar 12 fotos de la 6a y las LEG de §3.6 |
| Prueba a mano en el build | Hecha en las tres rondas con una carpeta de datos temporal: el vuelo de entrada, `Fuerte`, `Hora escena` a las 6, 12, 18 y 22 h y `Hora real`, la pausa con el velo, la salida, una rutina hasta el diálogo con la toma aérea, matar el proceso a mitad (la sesión queda cortada y consistente) y reabrir (la hora vuelve a `real`). En los registros, 0 avisos, 0 errores y 0 líneas `[SLOT]` (sin el Terrain Sample Asset Pack). Todo bien salvo QA-V6a-R3-01 |
| Builds de Windows (Mono) | El de simulador, generado por QA con el código final, con `THIRD_PARTY_NOTICES.md` al lado, igual al del repositorio |
| IL2CPP y Android | **No verificables**: faltan los módulos (`build.ps1` sale con 3) y el teléfono |
| Tus datos y los del legacy | Ninguna prueba, captura ni corrida usó tus bases ni tus preferencias: todas usaron carpetas temporales. Las preferencias de la ventana del build se resguardaron y se restauraron iguales. El repositorio legacy está intacto (`git status` vacío, también después del oráculo), y nadie leyó el archivo de credenciales de Strava |
| Repositorio público | 0 rutas de la PC ni datos del equipo en lo versionado y en lo nuevo. Nada de `Assets/ThirdParty/` versionado |

---

## 5. Criterios de aceptación

| Área | Criterios | Cumplen | Fallan | No verificables |
|---|---|---|---|---|
| SCN nuevos, de la escena (F-SCN-02, -03, -09, -10, -11, -14, -15, -23) | 54 | 54 | 0 | 0 |
| SCN nuevos que también tocan (F-SCN-01, -13, -19, -20, -21) | 23 | 21 | 1 (AC-SCN-13-03) | 1 (AC-SCN-01-09) |
| WKT nuevo (AC-WKT-17-13, las píldoras sobre la escena) | 1 | 1 | 0 | 0 |
| De iteraciones anteriores, que cambian o se vuelven a verificar | 23 | 22 | 1 (AC-WKT-13-15) | 0 |
| **Total** | **101** | **98** | **2** | **1** |

Más LT-62 a LT-87, que pasan. Entre los que cumplen, 4 tienen una parte de Android que no se pudo ver (entre ellos AC-SCN-01-04, la escena al volver del segundo plano; AC-SCN-01-13, la adaptación térmica; y AC-WKT-14-19, la fila de 200 píldoras en el teléfono).

Cómo llegó cada ronda: en la ronda 1, 96 cumplían y 4 fallaban (la escena que llegaba tarde aparecía de golpe, la misma parte de reducir movimiento, sin fundido al rotar, el bote tapado en la toma aérea); en la ronda 2, los 4 cumplían; en la ronda 3, la prueba a mano encontró AC-WKT-13-15 y QA sumó AC-SCN-13-03, que la lente de paridad había visto y ninguna ronda había tomado.

### 5.1 Los que fallan

| Criterio | Qué pasa | Por qué no rompe nada | Cuándo |
|---|---|---|---|
| **AC-WKT-13-15** (la entrada sin tirones) | Con la escena todavía preparándose (en el **primer arranque**, si entrás a entrenar en los primeros ≈ 5-13 s), dos cuadros con movimiento tardan ≈ 20 ms (tope 16,7): el del toque y el de cuando llega la escena. Son pasos de la preparación que corren por primera vez (la primera vez de URP y de la sesión de escena), que se pensaron para Workout quieto. 2 de 2 arranques. Con la escena lista, 13,6 ms | Es un tirón chico, por debajo de 33 ms, una sola vez por arranque y solo si entrás rápido. La sesión, el reloj y la escena llegan bien (AC-SCN-01-07 pasa) | **Primero en la 6b** (QA-V6a-R3-01): partir esos pasos o hacerlos en el negro de la entrada, y una medida que entre antes de que termine la preparación |
| **AC-SCN-13-03** (las 10 líneas de brillo del legacy) | El agua dibuja una 11.ª línea en la fila de abajo de la pantalla: 1 px con 5 % de opacidad | El dato (las 10 del legacy) coincide bit a bit con el oráculo; la línea de más casi no se ve | **Primero en la 6b** (QA-V6a-PAR-01): un tope en el *shader*, una sonda que cuente las líneas y las capturas regeneradas |

### 5.2 El no verificable

| Criterio | Qué falta ver | Por qué | Cuándo |
|---|---|---|---|
| AC-SCN-01-09 | 60 fps sostenidos durante 30 minutos con la escena en un Android de gama media, también con `Serie larga` y con el diálogo de completado | No hay módulo de Android ni teléfono (regla 9 del ROADMAP: no frena la iteración) | Cuando haya teléfono; si no, It. 14 |

### 5.3 Cumplen, y se completan más adelante

| Criterio | Qué falta | Cuándo |
|---|---|---|
| AC-SCN-02-07 (la toma aérea) | Ver el bote frenando detrás del diálogo: con el vidrio con desenfoque (P-19) | 6b |
| F-SCN-01 y F-SCN-13 | El agua avanzada, la elección entre las dos aguas y el cierre del presupuesto | 6b |
| F-SCN-19, -20 y -21 | Los otros escenarios, sus siluetas y objetos, y el selector | 8 |
| El bote | El remero, los remos, el cabeceo, la sombra del bote del legacy y la boya de 500 m (P-06) | 7 |
| AC-SCN-01-04, -01-13, AC-WKT-14-19 | Su parte de Android | Con el teléfono |
| AC-WKT-02-11 (de la 4a) | La suspensión real de tu equipo | Vos, con §3.1 (d) |

---

## 6. Desvíos aceptados

### 6.1 Diferencias visibles con el legacy, a propósito

Ninguna cambia un número de la sesión. DV-01 a DV-51 ya las aceptaste en las It. 2 a 5. Las que tienen "Confirmás" van a tu gate (§8); las demás salen de decisiones del plan que ya aceptaste.

| ID | Legacy | App | Por qué | Confirmás |
|---|---|---|---|---|
| DV-52 | El sol y la luna en coordenadas de pantalla; al mediodía, casi en el borde de arriba | En una dirección del mundo, en la misma zona del cielo y con la misma progresión (izquierda, centro, derecha), en un arco de **12°** | En 3D el sol del legacy quedaría fuera de cuadro en horizontal; con 12° queda debajo de la barra en los cinco tamaños (CAP-02, -05) | Sí (P-18, el arco) |
| DV-53 | Estrellas casi invisibles a toda hora salvo las 12 | Sin estrellas de 6:13 a 17:59; entran y salen con un fundido de 0,9 s (CAP-11) | Q-62: estrellas de día no son creíbles | Sí (P-18, el fundido) |
| DV-54 | A las 19:34 el sol a la derecha salta a la luna a la izquierda | El sol se pone y la luna sale detrás del paisaje (VID-05, VID-06) | Q-63 | — |
| DV-55 | Las montañas se corren de costado al remar | Quietas de costado; se acercan apenas (al 4 % de la velocidad del bote) | Q-65: una cámara que avanza no ve el horizonte correrse | — |
| DV-56 | Las nubes se corren también con la distancia | Solo con el viento; en la vuelta, se desvanecen y vuelven con un fundido | D-ART-06 | — |
| DV-57 | La luna es un disco entero con el halo a pleno | **La fase real del día**, con la parte oscura apenas visible; el halo según la parte iluminada; espejada en el hemisferio sur (CAP-04, CAP-10) | Realismo; la spec del legacy pedía "disco con sombra" | **Sí (P-03)** |
| DV-58 | `Hora escena` cambia en el acto | En 1,5 s, por el camino más corto del reloj (en el acto con reducir movimiento) | Ver la transición en las demos | — |
| DV-59 | Las líneas del agua siempre visibles, y a las 19:34 saltan de lado | Bajo el astro que se ve y apagándose con él (CAP-08, CAP-09) | Sin astro a la vista no hay brillo; tampoco saltan | Sí (P-18) |
| DV-60 | Todas las luces de la noche se prenden juntas | De a una, en los 20 minutos alrededor del umbral del legacy | Un anochecer creíble | — |
| DV-61 | El bote con remero y remos | Hasta la 7, el casco solo | El remero es de la 7 | — |
| DV-62 | Lo cercano y lo lejano aparecen y desaparecen en seco | Lo cercano se desvanece entre 2 y 1 m; lo lejano aparece con un fundido entre 400 y 360 m. La lista de objetos es la misma | Nada aparece de golpe | — |
| DV-63 | Un sol plano de 44 dp con un halo de 176 dp | Un disco de 1,6° que brilla (≈ 40 dp con su brillo en un teléfono), con el halo de 4 discos y los mismos alfas; la luna, 0,8 del sol | Un disco plano se ve "de dibujo" en 3D (CAP-02, `it6a-brillo`) | **Sí (P-18)** |
| DV-64 | Nieve en un solo pico (con el perfil quieto) | Nieve en todo lo que pasa el 58 % del perfil lejano, con borde suave; rosada en la hora dorada | Con las montañas quietas, la regla del legacy deja un solo pico nevado, siempre el mismo y tapado por las tarjetas en vertical | **Sí (P-18)** |
| DV-65 | Las montañas miden el 12 % del alto de la pantalla en las dos orientaciones | En horizontal, igual; **en vertical, como mucho 9°** (≈ 60 % del alto del legacy) | Con el sol a 12°, el macizo taparía el sol de la mañana hasta las ≈ 10:50 y la luna de las 22 h quedaría apoyada en la cresta | **Sí (P-18)** |
| DV-66 | El cielo es un degradé lineal | Los mismos dos colores en sus lugares, con la luz más junta cerca del horizonte, una franja de bruma y un resplandor dorado alrededor del sol en la hora dorada | Un cielo real se aclara cerca del horizonte, y la bruma evita la costura con el paisaje | **Sí (P-18)** |
| DV-67 | Entre la escena y las métricas no hay nada | Dos degradés suaves de azul muy oscuro detrás de la barra y de los controles (CAP-13) | Aire para leer la barra con un cielo claro o la nieve | **Sí (P-18)** |

Si rechazás una, vuelve lo del legacy (o el valor anterior), sin tocar lógica: son constantes de arte. El detalle de "si se rechaza" de cada una está en [01-po-spec §11.1](01-po-spec.md).

**Del sistema visual** (informativas, no cambian datos ni textos): el vuelo de entrada, la respiración de la cámara, la toma aérea, el terreno y la vegetación de la orilla, el velo de pausa sobre la escena, la luz de cada hora (la tabla final de [04-design-notes, WP-D2 §2](04-design-notes.md): sin *tonemapping*, porque la paleta del legacy ya es un color de pantalla), el campo tenue de estrellas solo en el nivel Alto y los tonos `.hud` de las píldoras, un poco más claros.

### 6.2 Decisiones técnicas con efecto que se puede notar

| ID | Qué | Efecto para vos |
|---|---|---|
| DT6a-01 | El rolido de la respiración es 0,15° (no 0,18°) | El horizonte nunca se aparta más de 3 dp, tampoco en la ventana de 1280 × 720 |
| DT6a-02 | El sol y la luna se mueven como mucho 0,3° por minuto | Ningún salto del astro, también en los cambios de hora |
| DT6a-06 | **Al rotar**, el HUD entra con un fundido de 200 ms en su lugar nuevo, sin una foto del anterior; dentro de la misma clase de ventana (arrastrar el borde) se reacomoda en el acto. El PO lo registró al cerrar en AC-WKT-17-07 (decía "fundido cruzado") | VID-09. Un fundido cruzado pediría una foto del HUD sin la escena que no existe con un solo panel |
| DT6a-08 | La cabaña nunca se corre de su lugar del legacy: si choca con un pino, el pino se poda | La cabaña siempre sobre su marca |
| DT6a-09 | Cerca del canal (a menos de 18 m del eje), solo los objetos del legacy y vegetación baja (≤ 1,2 m) | El paisaje del legacy se reconoce; el bosque va más atrás |
| DT6a-11 / DT6a-12 | La escena que llega tarde se prepara por tramos chicos; durante su fundido de 0,6 s el HUD tiene un elemento más | La entrada sigue fluida con la escena lista (pero ver QA-V6a-R3-01) |
| DT6a-14 | Los pinos lejanos son mallas simples, sin impostores | Ninguno, hasta los 400 m. Si la 6b lo pide, se suman |
| DT6a-19 | De noche las sombras bajan su fuerza a 0 en lugar de apagarse | Ningún tirón en el cambio de hora; cuesta lo mismo que de día |
| P-14 | La escena se prepara en Workout, sin dibujarse | La entrada es inmediata. El arranque de la app tarda un poco más en dejar todo listo (unos segundos en esta PC) |
| P-07 | Con el diálogo de completado quieto, la app **puede** bajar a 30 fps; en la PC queda a 60 | Más fluido, sin costo visible. En el teléfono (batería) se decide en la 14 |

También se aplicaron DT6a-03 a -05, -07, -10, -13, -15 a -18 y los cambios de las rondas (V1 y V2), sin efecto visible o ya incluidos en los criterios. Detalle en [02-tech-design §8.1](02-tech-design.md).

---

## 7. Issues abiertos y deuda

### 7.1 Issues de QA

Se cerraron con evidencia, en tres rondas:

- **Ronda 1 → 2** (los mayores): la escena demorada que aparecía de golpe (QA-V6a-01 = REN-10), sin fundido al rotar (QA-V6a-02), la toma aérea detrás del diálogo (QA-V6a-03, con tu decisión P-19 pendiente), el detector de *shaders* que no podía contar (REN-01), la sonda que no ubicaba el cuadro de la entrada (REN-02), la medida con un build viejo (REN-03), el armado de la orilla en un cuadro visible (REN-04), la RT en el primer cuadro (REN-05), las unidades de la preparación sin tope (REN-06, en parte), el presupuesto de memoria (REN-07), la fuga de memoria nativa al cortar un armado (REN-08) y el nivel que bajaba a medias (REN-09); y 9 menores.
- **Ronda 2 → 3**: el HUD con rutina que reservaba memoria en cada cuadro (QA-V6a-R2-01, mayor, de la 4b/5), DEMO-03 sin el muelle, la prueba de CPU inestable y la sonda de la entrada.

Quedan:

| ID | Qué pasa, en simple | Impacto | Quién | Cuándo |
|---|---|---|---|---|
| **QA-V6a-R3-01** (mayor) | En el primer arranque, si entrás a entrenar antes de que la escena termine de prepararse, dos cuadros de ≈ 20 ms | AC-WKT-13-15 en FAIL. Un tirón chico, una vez, sin efecto en la sesión | Architect | **Al empezar la 6b**, primero |
| **QA-V6a-PAR-01** (menor) | Una 11.ª línea de brillo, de 1 px y casi transparente, en el borde de abajo del agua | AC-SCN-13-03 en FAIL en pantalla | Designer | **Al empezar la 6b**, primero |
| QA-V6a-12 (menor, aspecto) | El halo de las 18 h casi no se lee; las montañas, lavadas en vertical y tapadas por la ladera en horizontal | Ningún criterio; lo ves en §3.6 | Designer | Al empezar la 6b, salvo que decidas otra cosa (§8) |
| QA-V6a-R2-04 (menor) | El margen del primer toque del proceso varía entre corridas (12,7 a 16,8 ms) | Va con R3-01 | Architect | 6b |
| QA-V6a-R2-02 (b), R2-05, REN-13, REN-14 | La memoria de la escena sola, la preparación en el teléfono, la GPU por pieza, un tope de dibujos de la UI en el build | Medidas que piden el teléfono o el *profiler* de memoria | Architect / QA | It. 14 |
| QA-V6a-08 = PAR-02 | Las sondas de la ventana de la cabaña y de su frente no están en el `manifest.json` | QA las mide en las fotos y pasan | Architect / QA | It. 8 |
| P-07 / REN-12 | El diálogo quieto sigue a 60 fps | Permitido (§6.2) | PO / Architect | It. 14 |

**Observaciones, sin issue:**

- La prueba de CPU de la escena en Alto tiene ≈ 7 % de margen en esta PC: si falla sola alguna vez, pasa a la categoría de rendimiento.
- Visto con el *profiler*: la pantalla de Dispositivo, debajo del entrenamiento, se redibuja en cada lectura del remo (1 vez por segundo). → 6b.
- Para las ayudas de QA: un clic en la llave sin mover antes el puntero no la abre (UI Toolkit no recibe el *hover*). No es un error de la app.

### 7.2 Otra deuda

- **Al empezar la 6b, antes de construir encima**: QA-V6a-R3-01 y R2-04 (con una medida que entre antes de que termine la preparación), QA-V6a-PAR-01, QA-V6a-12 y la pantalla de Dispositivo con el inmersivo arriba. Si tu demo (d) falla, también AC-WKT-02-11.
- **Para la 6b**: el agua avanzada, el vidrio con desenfoque (también en el diálogo de completado, P-19) y 60 fps en el teléfono (o la 14).
- **Para la It. 7**: el remero (con la lista de Mixamo, §9.2), los remos, el cabeceo, la sombra del bote del legacy, las medidas reales del bote, la boya de 500 m en el agua (P-06), `Series anteriores` abierto en vertical con el remero a la vista (P-03 de la 5) y la llave en la ventana baja dentro de la zona del bote (de la 4a).
- **Para la It. 8**: los otros escenarios y el selector; las sondas de la ventana y de la cabaña; D-ART-08 se vuelve a ofrecer.
- **Para la 10a, la 12 y la 14**: como en la 5 ([REPORT de la 5 §7.2](../05-series-parciales/REPORT.md)), más lo de la escena en el teléfono (60 fps, la adaptación térmica, la profundidad en el nivel Bajo con GLES).
- **CI**: `ci-core` corre en GitHub; `ci-unity` espera la licencia (P-08). Las pruebas que piden GPU se saltan en un CI sin GPU real.

### 7.3 Riesgos

| ID | Estado |
|---|---|
| R-12 (60 fps en el Android de gama media con la escena) | **Abierto** hasta que haya teléfono. En la PC sobra margen; en el nivel Medio el presupuesto medido entra en los topes (con 45-46 SetPass contra 50: poco margen) |
| R5-10 (el build tomado por tu `RowMate.exe`) | **Cerrado**: en la 6a no volvió a pasar |
| R4b-01 (P-17 con el modo de espera moderno de Windows) | **Abierto** hasta tu demo (d) |
| R4b-02 (la fila de 200 píldoras en el teléfono) | Pasa en la PC con la escena; **abierto** en el teléfono |
| R4a-03 (el presupuesto del HUD) | **Cerrado**: la escena no suma elementos (56-57 de 60) |
| R-T6 / R3-01, R3-02, R3-13, R4a-01, R4a-10, R4a-12, R4b-05, R4b-06, R5-04 | **Abiertos**, como en la 5 (módulos y teléfono) |

---

## 8. Decisiones que tenés que confirmar

Si no decís nada, se aplica lo que dice "Por defecto". Alcanza con un "OK a todo", o con decir cuál querés cambiar. Las decisiones de los gates de las It. 1 a 5 ya las aceptaste.

### 8.1 Gate de la It. 6a

| Decisión | Qué significa | Por defecto |
|---|---|---|
| **P-18** (DV-63 a DV-67 y las ampliaciones de DV-52, -53 y -59) | Las diferencias de arte de §6.1: el sol más chico y brillante en un arco de 12°, la nieve, las montañas más bajas en vertical, el cielo con bruma, los degradés detrás del HUD, el fundido de las estrellas y las líneas del agua que se apagan con el astro. Miralas en §3.6 (CAP-01 a CAP-05 contra LEG) y en VID-05 | **Se aceptan todas.** Podés rechazar cualquiera por separado: vuelve lo del legacy |
| **QA-V6a-12** (cómo se leen las montañas y el atardecer) | En vertical las montañas se ven lavadas y en horizontal la ladera cercana las tapa; a las 18 h el halo del sol casi no se lee (CAP-03, CAP-05 contra LEG-03, LEG-05) | **El Designer lo ajusta al empezar la 6b**: más contraste en la silueta lejana, la ladera cercana más baja o más lejos donde tapa el horizonte y un poco más de halo, sin cambiar DV-65 ni los números del legacy. Si te gustan así, decilo y quedan |
| **DV-57 / P-03** (la luna con fase) | La luna muestra la fase real del día, con la parte oscura apenas visible; en luna nueva se va a ver apenas el disco (CAP-04, CAP-10) | **Sí.** Si no: disco entero y halo a pleno siempre, como el legacy |
| **P-19** (la toma aérea detrás del diálogo) | Al terminar una rutina, el diálogo de completado (opaco y centrado, como el legacy) tapa el bote mientras la cámara sube (CAP-22) | **Se ve detrás del vidrio con desenfoque en la 6b**. Si lo querés nítido, el Designer propone otra ubicación del diálogo para la 6b (arriba en vertical, a un costado en horizontal), sin cambiar su contenido ni cuándo aparece |
| **D-ART-08 / P-01** (texturas CC0 y la luna de la NASA) | Sumar texturas libres (ambientCG, Poly Haven) y la foto de la luna de la NASA (dominio público) | **No.** En el lago la cercanía la mejora más el Terrain Sample Asset Pack, y la luna mide 8 a 16 dp. Se vuelve a ofrecer en el gate de la 8 (fachadas del río) |
| **P-02** (el Terrain Sample Asset Pack) | Un paquete gratis de Unity (Asset Store, 1,6 GB) con terreno y vegetación más realistas | **Opcional, no hace falta**: la 6a se ve completa en procedural. Si lo instalás, va a `Assets/ThirdParty/`, entra por los *slots* sin tocar lógica y no se versiona; el CI compila sin él |
| **QA-V6a-R3-01 y QA-V6a-PAR-01 como deuda** | Los dos criterios en FAIL (§5.1) se corrigen primero en la 6b, en lugar de una ronda corta ahora | **Deuda de la 6b, primero.** Si preferís que se corrijan antes de dar la 6a por cerrada, decilo: es una ronda corta del Architect y el Designer y otra de QA |
| **AC-WKT-02-11** (P-17, verificación) | Hacé la demo (d), suspendiendo a mano, y contanos qué marcó el reloj al despertar y cuántos puntos quedaron | Si el reloj salta, se reabre y va primero en la 6b |

### 8.2 Decididas por el PO al cerrar (no necesitan tu respuesta)

- **QA-V6a-R3-01 y QA-V6a-PAR-01** como deuda de la 6b, primero, con sus criterios en FAIL hasta entonces (salvo que digas otra cosa en §8.1).
- **DT6a-06**: AC-WKT-17-07 dice ahora que el HUD entra con un fundido de 200 ms en su lugar nuevo al rotar (§6.2).
- **P-07**: el diálogo quieto puede bajar a 30 fps, no está obligado; en la PC queda a 60. Se mide en el teléfono (It. 14).
- **P-17 de la spec** (los valores de luz de cada hora): adoptados con la tabla final del Designer, sin *tonemapping*.
- **QA-V6a-R2-04 y la pantalla de Dispositivo**, a la 6b; **R2-02 (b), R2-05, REN-13, REN-14** y el formato de color del nivel Medio, a la 14; las sondas de la ventana y de la cabaña, a la 8 (mientras tanto, QA las mide en las fotos).
- **Pendientes del PO de la spec (§15)**: hechos.
  - En el ROADMAP, "También toca" de la 6a con F-WKT-14, F-WKT-17 y F-APP-05; D-ART-05 con la redacción de DV-52; la boya de 500 m en la 7 (P-06), con `Series anteriores` en vertical, la sombra del bote y la lista de Mixamo; la deuda de la 6a al principio de la 6b.
  - En el inventario `immersive-scene`, la precarga del legacy y la de la app.
  - En el STATUS, el estado al día.

### 8.3 Opcional, sin urgencia

- **Licencia de Unity (P-08 del plan)**: ¿la cargás como secreto del repositorio? Hace falta para que corra `ci-unity`.
- **CAP-27 de la It. 3** (la ventana del build normal): sigue disponible a pedido.

---

## 9. Prerrequisitos

### 9.1 Para la It. 6b (agua avanzada, reflejos, estela, vidrio con desenfoque y rendimiento)

**De tu lado:**

| Qué | Para qué | ¿Obligatorio? |
|---|---|---|
| Tu OK a §8.1 | Cerrar el gate | Sí (alcanza con "OK a todo") |
| La demo de §3.1 (a) a (c), 10 minutos | Ver la iteración en la ventana real | Recomendado |
| La demo (d) de §3.1: suspender el equipo a mano con un entrenamiento en marcha (5 minutos) | Cerrar AC-WKT-02-11; si falla, se corrige primero en la 6b | Recomendado |
| **Un Android de gama media** (clase Galaxy A54 / Pixel 6a, Android 8+ de 64 bits) con depuración USB, y los módulos **Android Build Support** y **Windows Build Support (IL2CPP)** | La 6b cierra el presupuesto de rendimiento de la escena: 60 fps durante 30 minutos con el agua avanzada (y la degradada de la 6a); además cierra los NV de Android e IL2CPP de las It. 2 a 6a | Recomendado. Sin él, el rendimiento en el teléfono queda NO VERIFICABLE hasta la 14 |
| Terrain Sample Asset Pack | Terreno y vegetación más realistas en la orilla | No (P-02) |
| Licencia de Unity como secreto (P-08) | Que corra `ci-unity` | No |

**Del equipo, al empezar la 6b:**

1. **Architect**, antes de construir encima: QA-V6a-R3-01 (la preparación de la escena con el entrenamiento a la vista: partir los pasos largos, hacerlos en el negro de la entrada o esperar un cuadro quieto) con una medida `[PERF]` que entre antes de que termine la preparación y su prueba; R2-04 (el primer toque del proceso); la pantalla de Dispositivo que no se redibuje debajo del entrenamiento. QA lo verifica en el build.
2. **Designer**: QA-V6a-PAR-01 (el tope de 10 líneas, una sonda y las capturas regeneradas) y QA-V6a-12 (las montañas y el halo), según tu decisión en §8.1; y en el diseño de la 6b, el agua avanzada, el vidrio con desenfoque y, si elegís el bote nítido en P-19, la ubicación del diálogo.
3. **PO**: la spec de la 6b (`iterations/06b-agua-avanzada/01-po-spec.md`), con F-SCN-01, -12 y -13, `scene_view_test` con el agua avanzada, el patrón de olas sin saltos a los 4 096 m (Q-68), el desenfoque ≤ 0,6 ms en Medio, la caída al agua degradada sin cortar la sesión, el diálogo de completado con vidrio (P-19) y la deuda de arriba.

### 9.2 Para la It. 7: la lista de Mixamo (P-09)

**No hace falta ninguna animación de Mixamo para remar**: la remada se hace por código (cinemática inversa), al ritmo de tu SPM. De Mixamo se usa **el personaje**. La lista es la definitiva del Designer ([04-design-notes, WP-D2 §7](04-design-notes.md); ASSET_SLOTS §2).

| # | Archivo | Qué bajar | Opciones de descarga | ¿Obligatorio? |
|---|---|---|---|---|
| 1 | `YBot/YBot.fbx` | **Characters** → `Y Bot` | **Format** `FBX for Unity(.fbx)` · **Pose** `T-pose` | **Sí**, antes de la 7 |
| 2 | `XBot/XBot.fbx` | **Characters** → `X Bot` | ídem | Recomendado (prueba que el ajuste del cuerpo funciona con otras proporciones) |
| 3 | `RowerRealistic/RowerRealistic.fbx` | **Characters** → el primer personaje con ropa deportiva ajustada, sin accesorios, pelo corto o recogido y manos con 5 dedos (el primero a revisar: `Kaya`) | ídem | Opcional (el aspecto final del remero; si ninguno sirve, queda Y Bot pintado) |
| 4 | `YBot/YBot@SittingIdle.fbx` | **Animations** → `Sitting Idle`, con Y Bot elegido | **Format** `FBX for Unity(.fbx)` · **Skin** `Without Skin` · **Frames per Second** `30` · **Keyframe Reduction** `none` | Opcional (solo de referencia; nunca se reproduce) |

Cómo:

1. Entrá a [mixamo.com](https://www.mixamo.com) con una cuenta de Adobe (gratis).
2. En **Characters**, buscá `Y Bot`, elegilo y tocá **Download** con las opciones de la tabla. Repetí con los demás que quieras.
3. Guardá cada archivo con el nombre de la tabla dentro de la carpeta del repositorio, en `Assets/ThirdParty/Mixamo/` (por ejemplo `Assets/ThirdParty/Mixamo/YBot/YBot.fbx`). Esa carpeta **no se versiona** (la licencia de Mixamo deja usarlo en la app, no redistribuirlo suelto) y el CI compila sin ella.
4. Si abrís Unity, se importa como **Humanoid**, *Create From This Model* y con **Optimize Game Objects** apagado (ASSET_SLOTS §2.5). Si no, lo deja así el equipo al empezar la 7.

Si falta un archivo o no sirve (sin rig Humanoid, sin dedos, fuera de 1,55 a 2,05 m de alto o con más de 30 000 triángulos), la 7 usa un maniquí procedural y avisa una vez.

---

## 10. Por qué el PO acepta la It. 6a

- **El objetivo se cumple.**
  - El fondo provisorio se reemplazó por un lago 3D que avanza al ritmo del split, con cielo, sol, luna, estrellas, nubes, montañas, orilla, muelle y cabaña, y luz que sigue la hora real o la del simulador.
  - Las cuatro partes de la demo se ven en el build y toma por toma en DEMO-01 a DEMO-08, CAP-01 a CAP-24 y los 11 videos, con la escena y la interfaz reales.
- **La paridad está probada contra el código del legacy**, no contra un documento: el oráculo corre el legacy y la lente de paridad lo regeneró idéntico; los objetos caen sobre sus marcas a 0 dp; la paleta y el astro coinciden minuto a minuto; los 26 casos legacy pasan. Lo que se ve distinto es a propósito, está explicado y va a tu gate.
- **Nada de la escena toca el dato.** La escena lee la sesión y la última lectura; nunca escribe en el motor, la sesión ni la telemetría, y su distancia no se muestra ni se guarda. Matar el proceso a mitad deja la sesión cortada y consistente.
- **La base es confiable.**
  - Las mismas reglas pasan en .NET y dentro de Unity, con simulador y sin él; 553 pruebas con la interfaz montada.
  - Las capturas son deterministas, también las 3D.
  - Tres rondas de QA y dos lentes encontraron 15 problemas mayores: 14 están cerrados con evidencia en las pruebas, en las tomas y en el build, y el que queda (QA-V6a-R3-01, el resto de REN-06) tiene fecha, igual que el menor de la línea de brillo.
  - 0 bytes por cuadro con la escena, el HUD y los paneles; 0 *shaders* compilados a la vista; la escena no se dibuja fuera del entrenamiento.
  - El arte es gratis, procedural y detrás de *slots*; nada de terceros se versiona; la licencia del código portado va con la app.
  - Tus datos, tus preferencias y el legacy no se tocaron.
- **Lo que falla es chico, está entendido y tiene fecha.**
  - Los dos FAIL no rompen la sesión ni el dato y no se ven en el uso normal: un tirón de dos cuadros de ≈ 20 ms solo si entrás enseguida en el primer arranque, y una línea de 1 px casi transparente. Van primero en la 6b, antes de construir encima.
  - El no verificable es el de siempre: el teléfono.
- **Lo único que falta de tu lado** es tu OK a §8.1; si podés, la demo (10 minutos), la demo (d) de suspensión (5 minutos) y bajar Y Bot de Mixamo antes de la 7.
