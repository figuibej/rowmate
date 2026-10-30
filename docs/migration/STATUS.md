# Estado de la migración

| Campo | Valor |
|---|---|
| Actualizado | 2026-10-30 (rowmate-po, al cerrar la It. 4b) |
| Fase | **It. 4b cerrada, esperando el OK del usuario.** PO: **aceptada, sin condiciones**. QA: APROBADO CON OBSERVACIONES en la ronda 2 ([05-qa-verification](iterations/04b-hud-exr/05-qa-verification.md), "Ronda 2"), con las lentes de [paridad](iterations/04b-hud-exr/05-qa-lens-paridad.md) y [motion](iterations/04b-hud-exr/05-qa-lens-motion.md) también aprobadas con observaciones: dotnet con 0 avisos y 0 errores, herramientas 182 / 182 y Core 2319 (2292 pasan, 0 fallan, 27 ignoradas); Unity EditMode 2368 / 2337 / 0 / 31 y con simulador 2550 / 2519 / 0 / 31; PlayMode 354 / 354 y con simulador 420 / 420; matriz de 597 IDs con 595 en PASA, 0 en FALLA y 2 ignorados (Android y Apple); 51 tomas de captura (43 fotos y 8 videos) sin problemas e iguales byte a byte entre corridas. De 101 criterios, 98 cumplen, 0 fallan y 3 no son verificables (Android, Apple y la suspensión real del equipo, que verifica el usuario); LT-46 pasa. Las dos condiciones de la 4a se cumplen. 0 issues bloqueantes, 0 mayores (los dos FAIL menores de la ronda 1 se corrigieron) y 4 menores. Reporte para el usuario: [iterations/04b-hud-exr/REPORT.md](iterations/04b-hud-exr/REPORT.md) |
| Próximo paso | **Usuario**: probar la demo en el build de simulador ([REPORT §3.1](iterations/04b-hud-exr/REPORT.md)), sobre todo la parte (c), que suspende el equipo y cierra AC-WKT-02-11, y dar el OK al gate de la It. 4b (abajo). Después, **It. 5** (`05-series-parciales`): el Architect y el Designer empiezan por la deuda "Al empezar la It. 5" (abajo) y el PO escribe `iterations/05-series-parciales/01-po-spec.md` |
| Plan | [ROADMAP.md](ROADMAP.md): 17 iteraciones + 1 *spike* opcional (que no se hace), 158 IDs. El usuario decidió mantenerlo: todo con el simulador hasta la It. 9 y el Bluetooth real en la 10a y la 11 |

## Iteraciones

| N | Iteración | Estado |
|---|---|---|
| 1 | Fundaciones: proyecto Unity, Core puro y piezas base | **Cerrada** ([reporte](iterations/01-fundaciones/REPORT.md)); el usuario aceptó los valores por defecto de su gate |
| 2 | Shell, Dispositivo en vivo y simulador | **Cerrada** ([reporte](iterations/02-dispositivo-simulador/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su condición (AC-BLE-11-02, QA-V2-01) se cumplió en la It. 3 |
| S-1 | *Spike* opcional de Bluetooth en Windows | **No se hace** (decisión del usuario en el gate de la It. 2; el riesgo se ve en la 10a) |
| 3 | Rutinas: lista, editor y base local | **Cerrada** ([reporte](iterations/03-rutinas/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su deuda se resolvió al empezar la 4a |
| 4a | Motor de sesión, guardado incremental, reposo y HUD funcional | **Cerrada** ([reporte](iterations/04a-motor-hud-funcional/REPORT.md)); el usuario aceptó los valores por defecto de su gate, también P-17 (el tiempo con el equipo suspendido no cuenta). Sus dos condiciones (QA-V4a-03 y QA-V4a-04) y su deuda se resolvieron al empezar la 4b ([00-deuda-4a](iterations/04b-hud-exr/00-deuda-4a.md)): AC-WKT-17-04 y AC-APP-05-07 pasan |
| 4b | HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema | **Cerrada, esperando el OK del usuario** ([reporte](iterations/04b-hud-exr/REPORT.md)). Sin condiciones |
| 5 | Series en vivo: parciales y repeticiones | **Próxima** (espera el OK de la It. 4b) |
| 6a | Escena: cielo, luz, hora, Lago alpino y agua degradada | Pendiente |
| 6b | Agua avanzada, desenfoque del vidrio y rendimiento | Pendiente |
| 7 | Remero, bote y palada | Pendiente |
| 8 | Río, Costa, Regata y selector | Pendiente |
| 9 | Historial y detalle | Pendiente |
| 10a | Bluetooth real en Android y Windows | Pendiente |
| 10b | Tanda Apple (iOS y macOS) | Flotante: cuando haya Mac |
| 11 | Pulsómetro real | Pendiente |
| 12 | Strava: conexión y subida | Pendiente |
| 13 | Strava: sincronización y Perfil | Pendiente |
| 14 | Pulido, identidad, plataformas y CI | Pendiente |

## Decisiones

### Ya aceptadas por el usuario

- **Gate de la It. 1**: valores por defecto de Q-01, Q-14, Q-90, R-14 / Q-85, Q-92 (el criterio del tope por campo), P-01 (regla de idioma de Flutter), P-04 (Windows 10 21H1 o más nuevo) y P-06 (teclado y copiar y pegar en el idioma del sistema). Detalle en [REPORT de la It. 1 §8](iterations/01-fundaciones/REPORT.md).
- **Gate de la It. 2**: valores por defecto de P-03 (tope de 1,3× a la letra del sistema), DV-01 a DV-08, QA-V2-08 (barra de título de Windows oscura, corregida en la It. 3) y S-1 (no se hace). Además, mantener el roadmap y seguir con el simulador. Detalle en [REPORT de la It. 2 §8](iterations/02-dispositivo-simulador/REPORT.md).
- **Gate de la It. 3**: valores por defecto de Q-11 / R-14 (no se importan las rutinas ni el historial del legacy), P-01 (repeticiones de 1 a 999), P-02 (los topes de Q-92), DV-09 a DV-20 y DT-05 (los lectores de pantalla, en la It. 14). Lo registró [00-deuda-it3](iterations/04a-motor-hud-funcional/00-deuda-it3.md) al empezar la 4a. Detalle en [REPORT de la It. 3 §8](iterations/03-rutinas/REPORT.md).
- **Gate de la It. 4a**: valores por defecto de Q-12 (telemetría incremental), D-ART-01 (`#FF625B` con flecha ▲ / ▼), D-ART-02 / P-05 (el HUD más grande y escalado), D-ART-10 / P-12 (la llave del simulador, DV-39), DV-21 a DV-39, P-02 (el diálogo de completado en la 4b), P-03 (cerrar la ventana deja la sesión cortada) y **P-17** (el tiempo con el equipo suspendido no cuenta, como el legacy). Lo registró la [spec de la 4b §11](iterations/04b-hud-exr/01-po-spec.md). Detalle en [REPORT de la It. 4a §8](iterations/04a-motor-hud-funcional/REPORT.md).

### Para confirmar en el gate de la It. 4b

Si el usuario no dice nada, se aplica "Por defecto". Detalle en [REPORT de la It. 4b §8](iterations/04b-hud-exr/REPORT.md).

| Decisión | Por defecto | Fuente |
|---|---|---|
| **D-ANI-05 / P-01**: la cuenta grande `3`, `2`, `1` de los últimos 3 s de cada paso por tiempo y el sello de 2 s `Objetivo cumplido` / `No alcanzado` con el promedio contra el objetivo (CAP-02, CAP-03, CAP-04, DEMO-06, DEMO-07, DEMO-09) | Se adopta (si no: la cuenta de la barra, como hoy, y el resultado solo en la píldora) | AC-WKT-14-18, AC-WKT-09-08, -09 |
| **DV-40 / P-05**: con muchos pasos, nunca desaparecen; la fila se vuelve una franja y la en curso es un pulgar (CAP-22) | Se mantiene | [01-po-spec §11.1](iterations/04b-hud-exr/01-po-spec.md) |
| **DV-41 / P-10**: la salida conserva la última foto de la sesión, nunca `00:00` ni `—` (VID-02) | Se mantiene | AC-WKT-13-08 |
| **DV-42 / P-11**: `Terminar` del diálogo de completado espera el cierre de la sesión; si falla, avisa | Se mantiene | AC-WKT-21-06 |
| **DV-43 / P-13**: los totales del diálogo son siempre los del instante del fin | Se mantiene | AC-WKT-21-04 |
| **DV-44 a DV-47**: `FINALIZADO` en `#FF7A92`; la pastilla de objetivo cortada por fragmentos enteros; la cuenta y el reloj siempre pegados a la derecha; el título libre que se achica hasta 0,8 y el del diálogo en dos líneas | Se mantienen | [REPORT §6.1](iterations/04b-hud-exr/REPORT.md) |
| **AC-WKT-02-11** (P-17): verlo con la demo (c), suspendiendo el equipo con una sesión en marcha | Si el reloj salta el tiempo suspendido, el criterio se reabre y se corrige al empezar la It. 5 | [REPORT §3.1 (c)](iterations/04b-hud-exr/REPORT.md) |

**Decididas por el PO al cerrar la It. 4b** (no necesitan respuesta; detalle en [01-po-spec §17](iterations/04b-hud-exr/01-po-spec.md)):

- QA-V4b-06 (el primer cuadro de cada inmersivo tarda 31-69 ms de CPU) se acepta como desvío hasta el comienzo de la It. 5: no toca ningún dato.
- QA-V4b-R2-01 (el título libre pegado al ícono del reloj en un teléfono vertical en español): lo ajusta el Designer al empezar la It. 5, sin bajar el título de 0,8.
- QA-V4b-R2-02: hecho, la spec dice ≈ 0,8 (0,805).
- QA-V4b-R2-03 (Unity que revienta al cerrar en las herramientas): el Architect, al empezar la It. 5.
- Observaciones: 60 fps con el diálogo abierto (O-2 de la lente de motion), en la 6a; el indicio de desplazamiento del diálogo a 1,3× en L640 (O-01), en la It. 12; el endurecimiento ante excepciones (O-4 y O-5 de la lente de motion), al empezar la It. 5; los ceros del primer medio segundo de VID-08 (O-03), se aceptan.
- Pendientes del PO de la spec §15: hechos (ROADMAP, los inventarios `workout-engine-hud` y `app-shell-platform` y este STATUS).

## Prerrequisitos del usuario abiertos

| Para | Qué | Estado |
|---|---|---|
| It. 4b (recomendado, 10 minutos) | La demo de [REPORT §3.1](iterations/04b-hud-exr/REPORT.md) en el build de simulador de `Builds/Windows/simulator`, que es el del código final | Pendiente |
| It. 4b (recomendado, 5 minutos) | La parte (c) de esa demo: suspender el equipo con un entrenamiento en marcha. Cierra AC-WKT-02-11, que QA no puede verificar | Pendiente |
| It. 4a (opcional, 1 minuto) | `powercfg /requests` en una consola de administrador, con la app abierta y después de cerrarla ([REPORT de la 4a §3.7](iterations/04a-motor-hud-funcional/REPORT.md)). Cierra AC-APP-14-01 a -03 | Pendiente |
| Cierre de NV (It. 2 a 4b) | Módulos **Windows Build Support (IL2CPP)** (con Visual Studio 2022 "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621+) y **Android Build Support** (con OpenJDK y Android SDK & NDK Tools), más un Android 8.0+ de 64 bits con depuración USB. Cierran IL2CPP (AC-BASE-01, R3-01), AC-APP-16-04, AC-APP-02-06, AC-RUT-11-10, AC-WKT-02-10 y AC-APP-15-01 (las barras del sistema ocultas en el inmersivo), la parte Android de AC-APP-14-02 y las partes Android de otros criterios (10 de la It. 2, 6 de la It. 3, 3 de la 4a y 2 de la 4b: "Atrás" en la entrada y con el diálogo de completado) | Pendiente. Sin ellos no se frena ninguna iteración hasta la 6a |
| It. 2 (opcional, 5 minutos en la PC) | Abrir la app legacy después de la nueva (AC-APP-16-03); probar el build con Windows en modo claro (AC-APP-05-01 y, de la It. 3, AC-APP-05-10); los idiomas preferidos `Français (France)`, `Español (Argentina)` (AC-APP-09-06) | Pendiente |
| CI de Unity (opcional) | Licencia Personal de Unity como secreto del repositorio (P-08) | Pendiente. El repositorio está en GitHub, público, y `ci-core` corre en verde |
| It. 5 | Nada obligatorio: se prueba con el simulador | — |
| It. 6a | Terrain Sample Asset Pack y un Android de gama media | Pendiente |
| It. 10a / 10b / 11 / 12 | Remo AMS-670B, Mac con Xcode + iPhone, pulsómetro, cuenta de Strava | Pendiente |

Resueltos: en la It. 2, Unity Hub con el Editor 6000.3.27f1 y Windows Standalone (Mono), y las fuentes e íconos. Entre la It. 2 y la 3, el repositorio en GitHub ([figuibej/rowmate](https://github.com/figuibej/rowmate), público, con `ci-core` en verde). Entre la It. 3 y la 4a, los avisos del Firewall que dejaron los builds de la It. 3. Entre la 4a y la 4b, el `RowMate.exe` que había quedado abierto (QA-V4a-08): `Builds/Windows/simulator` se recompiló con el código final de la 4b.

## Pendiente de otros responsables (del cierre de la It. 4b)

Issues de [05-qa-verification](iterations/04b-hud-exr/05-qa-verification.md) (ronda 2) y de las lentes. Los de "Al empezar la It. 5" van antes de construir encima.

| ID | Qué | Quién | Cuándo |
|---|---|---|---|
| QA-V4b-06 | El primer cuadro de cada inmersivo tarda 31-69 ms de CPU, porque arma el HUD en el cuadro del toque: un tirón al empezar el fundido a negro (las demás ventanas del build, ≤ 9,3 ms). Armar el HUD en un cuadro del negro o tenerlo listo al abrir Workout, y medirlo con `FrameTimeProbe` en el build (02-tech-design V1-06) | Architect | Al empezar la It. 5 |
| QA-V4b-R2-01 | En TV y P360, en español, `Entrenamiento Libre` queda a ≈ 4 dp del ícono del reloj, menos que el espacio entre sus dos palabras: la barra pierde jerarquía. Ajustar sin bajar el título de 0,8 (por ejemplo, el relleno horizontal del chip de 8 a 6) y regenerar CAP-13 a CAP-15 en dos corridas | Designer (con el Architect) | Al empezar la It. 5 |
| QA-V4b-R2-03 | Unity a veces revienta (0xC0000005) al cerrar un `-batchmode -quit -executeMethod` que cambió los *defines*; los *scripts* restauran el *define*, pero la corrida queda marcada como fallida. Esperar la compilación o reintentar una vez, y anotarlo en `tools/unity/README.md` | Architect | Al empezar la It. 5 |
| Lente de motion O-4 y O-5 | El `MotionDriver` no aísla las excepciones por animación, y si fallara el armado del diálogo de completado la sesión quedaría en `Finished`. No se encontró ningún camino que falle hoy | Architect | Al empezar la It. 5, con QA-V4b-06 |
| Lente de motion O-1 | Dos resortes de la 4a (el ancho de `Próximo` y el cambio de paso) pueden seguir unos cientos de ms después de salir, sin medir ni dar avisos | Architect | Por prolijidad, con QA-V4b-06 |
| Lente de paridad O-P01 y O-P02 | Dos comentarios viejos en `WorkoutGoldenTests` y una afirmación posible durante la espera del diálogo | Architect | Por prolijidad |
| O-R2-01 (4b) | El Unity Test Framework deja un `TestResults.xml` en la carpeta de datos de la app del Editor en cada PlayMode (no es una base) | Architect | Si se quiere esa carpeta limpia |
| O-R2-01 (4a) | `FlushAndWait` devuelve `true` aunque no encole el cierre: distinguirlo si algún día se usa ese valor | Architect | Cuando se use |
| QA-V4a-L01 | Un monitor que no informa la distancia una vez por segundo puede trabar un paso por distancia (Q-55) | PO (decidido: P-04 ampliada) · Architect y QA | It. 10a, con el AMS-670B |

La deuda de la It. 4a quedó resuelta al empezar la 4b ([00-deuda-4a](iterations/04b-hud-exr/00-deuda-4a.md), [00-deuda-diseno](iterations/04b-hud-exr/00-deuda-diseno.md)): las dos condiciones, QA-V4a-05, -06 (las cifras de tablero y el kerning) y -L02 (los guiones del oráculo), con una sola regeneración de `it2`, `it3` e `it4a`; además, el "…" blanco, kcal que se desliza, la última foto en la salida (O-01), la espera del diálogo (DT4a-15) y el presupuesto del HUD. QA la comprobó en las dos rondas. Los FAIL de la ronda 1 de la 4b (QA-V4b-01 y -02) y los menores QA-V4b-03, -04, -05, -07, -P02 y -MOT-01 a -05 quedaron cerrados en la ronda 2.

Otra deuda abierta:

- **Para la It. 5.**
  - El presupuesto del HUD (R4a-03): el peor caso de la 4b queda en el tope de 60 elementos. Los paneles de parciales y de repeticiones tienen que entrar juntando elementos o con un tope nuevo medido.
  - Los paneles de series expandidos contra el lugar de la cuenta grande en vertical: colapsados entran; expandidos se cruzan con el disco. Por defecto, los paneles expandidos son obstáculo y, si no entra, la cuenta grande no se muestra esa vez ([00-deuda-diseno §4.8](iterations/04b-hud-exr/00-deuda-diseno.md)). Lo decide la spec de la 5.
  - La cascada de la entrada suma los paneles con SPM.
- **Para la 6a.** La fila con 200 píldoras en el teléfono (R4b-02); 60 fps con el diálogo de completado abierto (O-2 de la lente de motion); el velo de pausa pasa a la escena y el negro de la entrada, al vuelo de cámara.
- **Para la It. 7.** En L640, con el simulador, la llave queda dentro de la zona del bote (04-design-notes de la 4a §3.9). Hoy es fondo liso; se revisa cuando el bote se vea (Designer).
- **Para la 10a.**
  - QA-V4a-L01.
  - RA-19 (R4a-12): el reloj monótono de Android con la pantalla bloqueada.
  - RA-08 (R4a-10): Android puede cerrar la app en segundo plano.
  - Un cuadro tarde con el remo real (DT4a-10).
  - La variante completa de ROB-04: encolar el cierre en el acto.
  - El *lock* del *scheduler* para los *callbacks* nativos de Bluetooth (de la It. 3).
  - Las barras del sistema en Android y sus márgenes (R4b-05), y el aviso de suspensión con IL2CPP (R4b-06), con el módulo y el teléfono.
- **Para la It. 12.** El diálogo de completado con Strava (dos botones y la subida); el indicio de que el diálogo a 1,3× en L640 se desplaza (O-01 de QA).
- **Para la It. 14.** Lectores de pantalla (DT-05), DPI con dos monitores, la pantalla encendida solo en Dispositivo y en el entrenamiento (BKL-24), la háptica y el cierre de F-APP-15.
- **Deseables sin AC** (P2, de la It. 3): `Guardar` con indicador si tarda más de 300 ms (DN3-19); el alto que crece de una a dos líneas y la mini-barra que interpola (DN3-20).
- **Con teléfono.** De la It. 3: el teclado del sistema y el menú de texto (AC-RUT-11-10; la variante de P-06 si Android no muestra su menú, R3-13), `:hover` con el dedo, el costo de la copia de pantalla y la barra de estado. De la 4a: "Atrás" en el inmersivo, el segundo plano y el primer cuadro al volver, y la pantalla encendida. De la 4b: las barras del sistema ocultas y "Atrás" con el diálogo de completado.
- **CI.** `ci-core` corre en GitHub; `ci-unity` espera la licencia (P-08).

Riesgos:

- **Abiertos**:
  - R-T6 / R3-01 (IL2CPP con el SQLite nativo), hasta que esté el módulo.
  - R3-02 y R3-13 (Android), hasta que estén el módulo y el teléfono.
  - R4a-01 (el alcance del segundo plano en Android, sin teléfono), R4a-10 / RA-08 y R4a-12 / RA-19, que se miden en la 10a.
  - R4b-01 (P-17 con el modo de espera moderno de Windows): hasta la demo (c) del usuario.
  - R4b-05 (las barras de Android) y R4b-06 (el aviso de suspensión con IL2CPP), hasta que estén los módulos y el teléfono.
  - R4a-03 (el presupuesto del HUD), ahora para la It. 5.
  - R4b-02 (la fila de 200 píldoras en el teléfono), para la 6a.
- **Mitigados y medidos**:
  - R4a-07: el pedido de pantalla encendida se libera en el mismo hilo que lo pidió (probado).
  - R4a-13: un alcance de 8 h toma menos de 500 ms y publica una sola foto.
  - R4b-10: la salida de 640 ms con dos inmersivos (QA-V4a-01), probada; los toques durante el negro ya no llegan a Workout.
  - R3-03 y R3-09 (It. 3).
- **Cerrados**: R3-08 (It. 4a); R4a-04 y R4a-05 (los resolvió el diseño); R4b-04 y R4b-12 (las piezas de UI Toolkit se probaron en el proyecto: 02-tech-design B-01); R4b-07 (el oráculo se regeneró desde el legacy y dio idéntico); R4b-09 (las medidas, tomadas en Unity); R4b-11 (los controles durante la entrada, como pide P-15); R-T1, R-T2, R-T5 y R-T12 (It. 2).

---

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-30 | Documento creado al cerrar el gate del plan (estado: It. 1 por especificar) |

## Historial

| Fecha | Quién | Qué |
|---|---|---|
| 2026-10-27 | rowmate-po | Spec de la It. 1 escrita; la It. 1 pasa a "En diseño". ROADMAP: respaldo del vidrio = token `glass.fallback` (`#070D16 @86 %`), regla 9 alineada con la tabla única de ARCHITECTURE §20 y prerrequisito opcional del repositorio remoto |
| 2026-10-27 | rowmate-po | Spec de la It. 1 corregida por el gate de armonía (QA-02, QA-11, QA-12): Windows sin identificador (D-02 aceptada), punto de telemetría con tiempo obligatorio, P-06 aceptada como diferencia. ROADMAP §6: fila R-14 / Q-85 con D-02 y mínimos de escritorio (P-04) y textos del sistema (P-06) agendados en los gates de la It. 2 y la 10b |
| 2026-10-27 | rowmate-po | **Cierre de la It. 1**: aceptada por el PO (QA: APROBADO CON OBSERVACIONES, 894/912 pasan, 0 fallan, 131 AC PASS, 8 NV). Reporte para el usuario en `iterations/01-fundaciones/REPORT.md`. FEATURE_INVENTORY: Q-85 y R-14 alineadas (QA-V-10) y nueva Q-92 (QA-V-11). ROADMAP: Q-92 en la It. 3 y en §6. Prerrequisitos de la It. 2 detallados. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-10-27 | coordinador | El usuario aceptó los valores por defecto de Q-01, Q-14, Q-90, R-14 / Q-85, Q-92, P-01, P-04 y P-06. Empieza la It. 2 |
| 2026-10-27 | rowmate-po | **Cierre de la It. 2**: aceptada por el PO con una condición (QA-V2-01 al empezar la It. 3; AC-BLE-11-02 reabierto). QA: APROBADO CON OBSERVACIONES (dotnet 1247/1264 pasan y 0 fallan; Unity EditMode con simulador 1265/1281 y PlayMode con simulador 85/85; 118 AC PASS, 1 FAIL menor, 6 NV; 15/15 casos legacy; 15 issues menores). Reporte para el usuario en `iterations/02-dispositivo-simulador/REPORT.md`. Decisiones del PO al cerrar: D-26, D-25, D-27, QA-V2-05 y QA-V2-09. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-10-27 | rowmate-po | Registro de lo que informó el coordinador: el usuario aceptó los valores por defecto del gate de la It. 2 (P-03, DV-01 a DV-08, QA-V2-08 en la It. 3 y S-1, que no se hace) y decidió mantener el roadmap; el repositorio quedó en GitHub, público, con `ci-core` en verde. Empieza la It. 3 |
| 2026-10-28 | rowmate-po | **Cierre de la It. 3**: aceptada por el PO, sin condiciones. QA: APROBADO CON OBSERVACIONES en la ronda 3 (dotnet 1582/1599 pasan y 0 fallan; Unity EditMode con simulador 1614/1630 y PlayMode con simulador 235/235; matriz 377/377; 101 AC PASS, 0 FAIL, 1 NV; 10 issues menores). Reporte para el usuario en `iterations/03-rutinas/REPORT.md`. Decisiones del PO al cerrar: DV-20 (QA-V3-07, al gate), QA-V3-09, DT-21, CAP-27 a pedido y QA-V3-02 a -04 en la 4a. Pendientes de la spec §15 hechos: ROADMAP (It. 3: `-2` y `−2`; It. 4a: rutina fuera de topes, R3-08; It. 14: lectores de pantalla), routines-models (F-RUT-13 y caso 18) y FEATURE_INVENTORY (Q-26 y Q-92). Spec con §17. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-10-28 | — | El usuario aceptó los valores por defecto del gate de la It. 3 (lo registra [00-deuda-it3](iterations/04a-motor-hud-funcional/00-deuda-it3.md)). Empieza la It. 4a: deuda de la It. 3, spec, diseño técnico, gate de armonía con dos lentes, WP-A, WP-B, WP-D y verificación en dos rondas |
| 2026-10-29 | rowmate-po | **Cierre de la It. 4a**: aceptada por el PO con dos condiciones (QA-V4a-03 y QA-V4a-04 al empezar la It. 4b; AC-WKT-17-04 y AC-APP-05-07 reabiertos). QA: APROBADO CON OBSERVACIONES en la ronda 2 (dotnet 2206/2228 pasan y 0 fallan; Unity EditMode con simulador 2240/2266 y PlayMode con simulador 327/328; matriz 534 PASA de 535; 156 AC PASS, 2 FAIL menores, 4 NV; 11/11 casos legacy; 8 issues menores). Lentes de paridad y robustez: APROBADO CON OBSERVACIONES. Reporte para el usuario en `iterations/04a-motor-hud-funcional/REPORT.md`. Decisiones del PO al cerrar: DT4a-17 (AC-WKT-06-12), QA-V4a-L01 (P-04 y DV-26 ampliadas), P-17 nueva para el gate (QA-V4a-ROB-05) y las observaciones de QA. Pendientes de la spec §15 hechos: ROADMAP (F-WKT-14, `#FF625B`, D-ART-10 y P-17 en §6; P-17 en la 4b; QA-V4a-L01 en la 10a), FEATURE_INVENTORY (Q-55 y Q-60) y workout-engine-hud. Spec con §17. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-10-29 | — | El usuario aceptó los valores por defecto del gate de la It. 4a, también P-17 (el tiempo con el equipo suspendido no cuenta); lo registra la [spec de la 4b §11](iterations/04b-hud-exr/01-po-spec.md). Empieza la It. 4b: las dos condiciones y la deuda de la 4a ([00-deuda-4a](iterations/04b-hud-exr/00-deuda-4a.md)), spec, diseño técnico, gate de armonía con dos lentes, WP-A, WP-B, WP-D y verificación en dos rondas |
| 2026-10-30 | rowmate-po | **Cierre de la It. 4b**: aceptada por el PO, sin condiciones. QA: APROBADO CON OBSERVACIONES en la ronda 2 (dotnet: herramientas 182/182 y Core 2292/2319 pasan, 0 fallan; Unity EditMode con simulador 2519/2550 y PlayMode con simulador 420/420; matriz 595 PASA de 597, 0 FALLA; 98 AC PASS, 0 FAIL, 3 NV; LT-46 pasa; 4 issues menores). Lentes de paridad y motion: APROBADO CON OBSERVACIONES. Las dos condiciones de la 4a, cumplidas. Reporte para el usuario en `iterations/04b-hud-exr/REPORT.md`. Decisiones del PO al cerrar: QA-V4b-06 como desvío hasta la It. 5, QA-V4b-R2-01 y -R2-03 al empezar la 5, QA-V4b-R2-02 hecho en la spec y las observaciones de QA y de las lentes. Pendientes de la spec §15 hechos: ROADMAP (P-17 aceptada; "También toca" de la 4b en §2 y §4), `workout-engine-hud` (F-WKT-13, -14, -16 y -21) y `app-shell-platform` (F-APP-15). Spec con §17. Legacy sin cambios (`git status --porcelain` vacío) |
