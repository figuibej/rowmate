# Estado de la migración

| Campo | Valor |
|---|---|
| Actualizado | 2026-10-30 (rowmate-po, al cerrar la It. 5) |
| Fase | **It. 5 cerrada, esperando el OK del usuario.** PO: **aceptada**, con la verificación en el build pendiente (no es una falla: nada de la 5 corrió todavía en el build de simulador, porque el `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator`, que todavía tiene el build de la 4b). QA: APROBADO CON OBSERVACIONES en la ronda 2 ([05-qa-verification](iterations/05-series-parciales/05-qa-verification.md), "Ronda 2"), con las lentes de [paridad](iterations/05-series-parciales/05-qa-lens-paridad.md) y [motion](iterations/05-series-parciales/05-qa-lens-motion.md) también aprobadas con observaciones: dotnet con 0 avisos y 0 errores, herramientas 192 / 192 y Core 2541 (2507 pasan, 0 fallan, 34 ignoradas); Unity EditMode 2601 / 2563 / 0 / 38 y con simulador 2793 / 2755 / 0 / 38; PlayMode 426 / 426 y con simulador 495 / 495; matriz de 658 IDs con 656 en PASA, 0 en FALLA y 2 ignorados (Android y Apple); 47 tomas de captura (38 fotos y 9 videos) sin problemas e iguales byte a byte entre corridas. De 74 criterios, 71 cumplen, 0 fallan y 3 no son verificables (la medida `[PERF]` del build, en dos criterios, y la suspensión real del equipo, que verifica el usuario); LT-47 a LT-61 pasan. 0 issues bloqueantes, 0 mayores (los dos mayores de las lentes se corrigieron en la ronda 2) y 1 menor (del Designer). Reporte para el usuario: [iterations/05-series-parciales/REPORT.md](iterations/05-series-parciales/REPORT.md) |
| Próximo paso | **Usuario**: cerrar su `RowMate.exe` y generar el build de simulador ([REPORT §3.1](iterations/05-series-parciales/REPORT.md)); probar la demo, sobre todo la parte (c), que suspende el equipo a mano y cierra AC-WKT-02-11; y dar el OK al gate de la It. 5 (abajo). Después, **It. 6a** (`06a-escena-lago`): QA hace la verificación en el build que quedó pendiente ("Al empezar la It. 6a", abajo), el Designer cierra QA-V5-R2-01 y el PO escribe `iterations/06a-escena-lago/01-po-spec.md` |
| Plan | [ROADMAP.md](ROADMAP.md): 17 iteraciones + 1 *spike* opcional (que no se hace), 158 IDs. El usuario decidió mantenerlo: todo con el simulador hasta la It. 9 y el Bluetooth real en la 10a y la 11 |

## Iteraciones

| N | Iteración | Estado |
|---|---|---|
| 1 | Fundaciones: proyecto Unity, Core puro y piezas base | **Cerrada** ([reporte](iterations/01-fundaciones/REPORT.md)); el usuario aceptó los valores por defecto de su gate |
| 2 | Shell, Dispositivo en vivo y simulador | **Cerrada** ([reporte](iterations/02-dispositivo-simulador/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su condición (AC-BLE-11-02, QA-V2-01) se cumplió en la It. 3 |
| S-1 | *Spike* opcional de Bluetooth en Windows | **No se hace** (decisión del usuario en el gate de la It. 2; el riesgo se ve en la 10a) |
| 3 | Rutinas: lista, editor y base local | **Cerrada** ([reporte](iterations/03-rutinas/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su deuda se resolvió al empezar la 4a |
| 4a | Motor de sesión, guardado incremental, reposo y HUD funcional | **Cerrada** ([reporte](iterations/04a-motor-hud-funcional/REPORT.md)); el usuario aceptó los valores por defecto de su gate, también P-17 (el tiempo con el equipo suspendido no cuenta). Sus dos condiciones (QA-V4a-03 y QA-V4a-04) y su deuda se resolvieron al empezar la 4b ([00-deuda-4a](iterations/04b-hud-exr/00-deuda-4a.md)): AC-WKT-17-04 y AC-APP-05-07 pasan |
| 4b | HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema | **Cerrada** ([reporte](iterations/04b-hud-exr/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su deuda se resolvió al empezar la 5 ([00-deuda-4b](iterations/05-series-parciales/00-deuda-4b.md)), salvo la medida `[PERF]` de la entrada en el build (AC-WKT-13-15, pendiente con la de la 5) |
| 5 | Series en vivo: parciales y repeticiones | **Cerrada, esperando el OK del usuario** ([reporte](iterations/05-series-parciales/REPORT.md)). Con la verificación en el build pendiente, para el comienzo de la 6a |
| 6a | Escena: cielo, luz, hora, Lago alpino y agua degradada | **Próxima** (espera el OK de la It. 5) |
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
- **Gate de la It. 4b**: valores por defecto de D-ANI-05 (la cuenta grande y el sello), DV-40 a DV-47, P-01 a P-16 de la 4b y las decisiones del PO al cerrar. Lo registró la [spec de la 5 §11](iterations/05-series-parciales/01-po-spec.md). La demo (c) de P-17 (AC-WKT-02-11) no fue concluyente: el registro de la app no muestra ningún despertar, y con la app abierta Windows no se duerme solo ([00-deuda-4b §1](iterations/05-series-parciales/00-deuda-4b.md)). Sin bug; se repite al cierre de la 5, suspendiendo a mano.

### Para confirmar en el gate de la It. 5

Si el usuario no dice nada, se aplica "Por defecto". Detalle en [REPORT de la It. 5 §8](iterations/05-series-parciales/REPORT.md).

| Decisión | Por defecto | Fuente |
|---|---|---|
| **DV-48 / P-02**: desde el primer cuadro, los paneles muestran el primer paso (`0:00.0` `0 m`, `Rep 1/N · 0 m` y el panel de repeticiones); el legacy dice un segundo `Sin parciales en este paso` y el chip `Reps` aparece de golpe (VID-06) | Se mantiene | AC-WKT-18-01, AC-WKT-13-06 |
| **DV-49 / P-13**: en los paneles nada se corta ni se parte: se achica (`Rep 100`, `5999:59.9`, `999.999 m`; CAP-25) | Se mantiene | AC-WKT-18-17 |
| **DV-50 / P-03 / P-04**: paneles de 137 base fijos (cerrados y abiertos), a 8·s de SPM; la cuenta grande y `PAUSA` siempre a la vista en su lugar; abiertos en vertical, solo por la franja de afuera de la zona del bote; en horizontal nunca la tocan (CAP-03, CAP-18 a CAP-22). El legacy: 130 / 200 a 10 | Se mantiene. Si no: 130 / 200 y 10 (constantes), y en vertical la cuenta grande y `PAUSA` se esconden esa vez si un panel abierto ocupa su lugar | [01-po-spec §11.1](iterations/05-series-parciales/01-po-spec.md) |
| **DV-51 / P-14**: adentro de los paneles, rótulos, metros y títulos más chicos que en el legacy, con los metros a la derecha y los vacíos en dos renglones; lo que se lee de lejos (la vuelta en curso, los tiempos de las filas) queda como en el legacy (CAP-03 contra `docs/image-03.png` del legacy) | Se mantiene. Si no: los tamaños del legacy por la escala, con las filas de repeticiones más achicadas | Ídem |
| **P-08**: en inglés, `1.112 m` con punto de miles, como el legacy (Q-06) | Se mantiene. La alternativa es `1,112 m` en inglés | AC-WKT-18-22, AC-WKT-11-11 |
| **AC-WKT-02-11** (P-17): la demo (c), suspendiendo el equipo a mano con una sesión en marcha | Si el reloj salta el tiempo suspendido, el criterio se reabre y se corrige al empezar la 6a | [REPORT §3.1 (c)](iterations/05-series-parciales/REPORT.md) |

**Decididas por el PO al cerrar la It. 5** (no necesitan respuesta; detalle en [01-po-spec, "Al cerrar la iteración"](iterations/05-series-parciales/01-po-spec.md)):

- La verificación en el build (AC-WKT-13-15, la ventana `[PERF]` de AC-WKT-18-21 y la prueba a mano en el build) queda NO VERIFICABLE en la 5 (riesgo aceptado en el gate, QA-G5T-14) y se hace al empezar la 6a, antes de construir encima. Si algo falla, se reabre y va primero.
- QA-V5-R2-01 (DD5-27 en la columna desplazable de TH y L640): el criterio de producto se cumple; decide el Designer al empezar la 6a, por defecto que la regla valga solo para W1280 y las notas lo digan.
- La llave del simulador en L640 con el diálogo de completado tapa parte de la columna inerte, debajo del velo (AC-BLE-16-02 con su única excepción, QA-V5-01): no es una DV; va contada en el REPORT.
- La clave de preferencias de otro tipo (QA-V5-P02): solo ese panel arranca cerrado; informativa (§11.1.1 de la spec).
- DT5-D1 y DT5-D2 (el HUD armado en el negro y el precalentado en el arranque): aceptados.
- Las precisiones del modo compacto de la barra libre en AC-WKT-16-02, -03, -07 y AC-WKT-19-01.
- Pendientes del PO de la spec §15: hechos (ROADMAP, `workout-engine-hud`, FEATURE_INVENTORY con BKL-26 y este STATUS).

## Prerrequisitos del usuario abiertos

| Para | Qué | Estado |
|---|---|---|
| It. 5 (necesario para ver la demo) | Generar el build de simulador con `tools/build/build.ps1` ([REPORT §3.1](iterations/05-series-parciales/REPORT.md)). También destraba la medida `[PERF]` de QA | Hecho: `Builds/Windows/simulator` tiene la It. 5 (la medida `[PERF]` queda para el comienzo de la 6a) |
| It. 5 (recomendado, 10 minutos) | La demo de [REPORT §3.1](iterations/05-series-parciales/REPORT.md) (a) y (b) en ese build | Pendiente |
| It. 5 (recomendado, 5 minutos) | La parte (c): suspender el equipo a mano (Inicio → Encender/apagar → Suspender) con un entrenamiento en marcha. Cierra AC-WKT-02-11, que QA no puede verificar | Pendiente |
| It. 4a (opcional, 1 minuto) | `powercfg /requests` en una consola de administrador, con la app abierta y después de cerrarla ([REPORT de la 4a §3.7](iterations/04a-motor-hud-funcional/REPORT.md)). Cierra AC-APP-14-01 a -03 | Pendiente |
| Cierre de NV (It. 2 a 5) | Módulos **Windows Build Support (IL2CPP)** (con Visual Studio 2022 "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621+) y **Android Build Support** (con OpenJDK y Android SDK & NDK Tools), más un Android 8.0+ de 64 bits con depuración USB. Cierran IL2CPP (AC-BASE-01, R3-01; en la 5, el reemplazo atómico de `prefs.json`, R5-04), AC-APP-16-04, AC-APP-02-06, AC-RUT-11-10, AC-WKT-02-10 y AC-APP-15-01 (las barras del sistema ocultas en el inmersivo), la parte Android de AC-APP-14-02 y las partes Android de otros criterios (10 de la It. 2, 6 de la It. 3, 3 de la 4a, 2 de la 4b y, en la 5, el arrastre con el dedo, el área de toque de los chips y el primer cuadro al volver) | Pendiente. **El teléfono hace falta para la 6a** (60 fps) |
| It. 2 (opcional, 5 minutos en la PC) | Abrir la app legacy después de la nueva (AC-APP-16-03); probar el build con Windows en modo claro (AC-APP-05-01 y, de la It. 3, AC-APP-05-10); los idiomas preferidos `Français (France)`, `Español (Argentina)` (AC-APP-09-06) | Pendiente |
| CI de Unity (opcional) | Licencia Personal de Unity como secreto del repositorio (P-08) | Pendiente. El repositorio está en GitHub, público, y `ci-core` corre en verde |
| It. 6a | **Terrain Sample Asset Pack** (Asset Store de Unity, gratis, 1,6 GB) y **un Android de gama media** (clase Galaxy A54 / Pixel 6a, Android 8+ ARM64) con depuración USB, con el módulo de Android | Pendiente. Sin el pack, terreno procedural; sin el teléfono, los 60 fps quedan NO VERIFICABLE hasta la 14 |
| It. 7 (la lista llega en la 6a) | Descargar de Mixamo los archivos que fije el Designer en la 6a (P-09) | Pendiente |
| It. 10a / 10b / 11 / 12 | Remo AMS-670B, Mac con Xcode + iPhone, pulsómetro, cuenta de Strava | Pendiente |

Resueltos: en la It. 2, Unity Hub con el Editor 6000.3.27f1 y Windows Standalone (Mono), y las fuentes e íconos. Entre la It. 2 y la 3, el repositorio en GitHub ([figuibej/rowmate](https://github.com/figuibej/rowmate), público, con `ci-core` en verde). Entre la It. 3 y la 4a, los avisos del Firewall que dejaron los builds de la It. 3. Entre la 4a y la 4b, el `RowMate.exe` que había quedado abierto (QA-V4a-08). La demo (c) de la 4b se hizo, pero no fue concluyente: se repite en la 5, suspendiendo a mano.

## Pendiente de otros responsables (del cierre de la It. 5)

Issues de [05-qa-verification](iterations/05-series-parciales/05-qa-verification.md) (ronda 2) y lo que quedó sin hacer. Los de "Al empezar la It. 6a" van antes de construir encima.

| ID | Qué | Quién | Cuándo |
|---|---|---|---|
| Verificación en el build (AC-WKT-13-15, AC-WKT-18-21) | Con el `RowMate.exe` del usuario cerrado: generar el build de simulador; `[PERF]` de las tres entradas (libre, `Prueba 4a` y `Cinco reps` con los dos paneles abiertos) y la ventana con los paneles (`Fondo 3000` y `Series 600`); la prueba a mano de 05-qa-verification §R2.6 (mouse y teclado, reiniciar la app con los paneles recordados, `Simular desconexión`, la cuenta grande, el sello y `PAUSA`, matar el proceso a mitad). Siempre con `ROWMATE_DATA_DIR` en una carpeta temporal | QA (o el Architect) | **Al empezar la It. 6a**. Si algo falla, se reabre y va primero |
| QA-V5-R2-01 | DD5-27 en la columna desplazable (TH, L640): 04-design-notes §5.2 dice que una fila nueva con la vista desplazada no mueve lo que se ve; el código lo hace solo en W1280. En TH y L640 lo que se ve baja con el empuje, sin saltos (AC-WKT-18-13 se cumple) | Designer | Al empezar la It. 6a. Por defecto, la opción (a): la regla vale solo para W1280 y las notas lo dicen |
| DT5-13 | Las celdas de los números de los paneles (`TabularCells`) repiten la lógica de `TabularNumber` | Architect | Cuando se toque `TabularNumber` por otro motivo |
| O-R2-01 (4a) | `FlushAndWait` devuelve `true` aunque no encole el cierre: distinguirlo si algún día se usa ese valor | Architect | Cuando se use |
| QA-V4a-L01 | Un monitor que no informa la distancia una vez por segundo puede trabar un paso por distancia (Q-55); con la 5, también los parciales (comparten los metros) | PO (decidido: P-04 ampliada) · Architect y QA | It. 10a, con el AMS-670B |

La deuda de la It. 4b quedó resuelta al empezar la 5 ([00-deuda-4b](iterations/05-series-parciales/00-deuda-4b.md), [00-deuda-diseno](iterations/05-series-parciales/00-deuda-diseno.md)): QA-V4b-06 (el HUD se arma en el negro y el arranque lo precalienta; falta solo la medida del build), QA-V4b-R2-01 (el modo compacto de la barra libre), QA-V4b-R2-03 (los *scripts* siguen si Unity revienta al cerrar: pasó 3 veces en 16 cambios del modo simulador en la ronda 2 de QA y siguieron solos), O-4, O-5, O-1, O-P01, O-P02 y O-R2-01 de la 4b. QA la verificó en las dos rondas. Los dos mayores de las lentes de la 5 (QA-V5-P01 y LM5-01) y los menores QA-V5-01 a -05, QA-V5-P02 y LM5-02 a -05 quedaron cerrados en la ronda 2.

Otra deuda abierta:

- **Para la 6a.** La verificación en el build de la 5 (arriba); la fila con 200 píldoras en el teléfono (R4b-02); 60 fps con el diálogo de completado abierto (O-2 de la lente de motion de la 4b); el velo de pausa pasa a la escena y el negro de la entrada, al vuelo de cámara; la boya de 500 m en el agua (6a o 7). El presupuesto del HUD queda en 56-57 de 60 elementos: la 6a tiene 3 de margen (el corte de la fila de 200 píldoras suma 1).
- **Para la It. 7.** En vertical, `Series anteriores` abierto tapa la pala izquierda mientras está abierto: se revisa con el bote a la vista (P-03; la alternativa medida es ponerlo debajo de Watts). En L640, con el simulador, la llave queda dentro de la zona del bote (de la 4a).
- **Para la 10a.**
  - QA-V4a-L01.
  - RA-19 (R4a-12): el reloj monótono de Android con la pantalla bloqueada.
  - RA-08 (R4a-10): Android puede cerrar la app en segundo plano.
  - Un cuadro tarde con el remo real (DT4a-10).
  - La variante completa de ROB-04: encolar el cierre en el acto.
  - El *lock* del *scheduler* para los *callbacks* nativos de Bluetooth (de la It. 3).
  - Las barras del sistema en Android y sus márgenes (R4b-05), el aviso de suspensión con IL2CPP (R4b-06) y el reemplazo atómico de `prefs.json` con IL2CPP (R5-04), con el módulo y el teléfono.
- **Para la It. 12.** El diálogo de completado con Strava (dos botones y la subida); el indicio de que el diálogo a 1,3× en L640 se desplaza (O-01 de QA de la 4b).
- **Para la It. 14.** Lectores de pantalla (DT-05; también los paneles de series), DPI con dos monitores, la pantalla encendida solo en Dispositivo y en el entrenamiento (BKL-24), la háptica (también al cerrar una vuelta) y el cierre de F-APP-15.
- **Deseables sin AC.** De la It. 3 (P2): `Guardar` con indicador si tarda más de 300 ms (DN3-19); el alto que crece de una a dos líneas y la mini-barra que interpola (DN3-20). De la 5: un indicio de "hay más" en las filas desplazables de W1280, en la cabecera, si el usuario lo pide (04-design-notes §13).
- **Con teléfono.** De la It. 3: el teclado del sistema y el menú de texto (AC-RUT-11-10; la variante de P-06 si Android no muestra su menú, R3-13), `:hover` con el dedo, el costo de la copia de pantalla y la barra de estado. De la 4a: "Atrás" en el inmersivo, el segundo plano y el primer cuadro al volver, y la pantalla encendida. De la 4b: las barras del sistema ocultas y "Atrás" con el diálogo de completado. De la 5: el arrastre de la columna con el dedo, el área de toque de los chips y el primer cuadro al volver con los paneles.
- **Backlog.** BKL-26: el aviso de cada vuelta con la diferencia contra la anterior y las filas coloreadas (P-09).
- **CI.** `ci-core` corre en GitHub; `ci-unity` espera la licencia (P-08).

Riesgos:

- **Abiertos**:
  - R-T6 / R3-01 (IL2CPP con el SQLite nativo), hasta que esté el módulo.
  - R3-02 y R3-13 (Android), hasta que estén el módulo y el teléfono.
  - R4a-01 (el alcance del segundo plano en Android, sin teléfono), R4a-10 / RA-08 y R4a-12 / RA-19, que se miden en la 10a.
  - R4b-01 (P-17 con el modo de espera moderno de Windows): hasta la demo (c) del usuario.
  - R4b-05 (las barras de Android), R4b-06 (el aviso de suspensión con IL2CPP) y R5-04 (el reemplazo atómico de `prefs.json` con IL2CPP), hasta que estén los módulos y el teléfono.
  - R5-10 (el build tomado por el `RowMate.exe` del usuario): pasó en toda la 5; se cierra con la verificación en el build al empezar la 6a.
  - R4b-02 (la fila de 200 píldoras en el teléfono), para la 6a.
- **Mitigados y medidos**:
  - R4a-07: el pedido de pantalla encendida se libera en el mismo hilo que lo pidió (probado).
  - R4a-13: un alcance de 8 h toma menos de 500 ms y publica una sola foto.
  - R4b-10: la salida de 640 ms con dos inmersivos (QA-V4a-01), probada; los toques durante el negro ya no llegan a Workout.
  - R5-11 (asignaciones del texto dibujado): 0 B por cuadro en las diez escenas medidas, con el control positivo en la misma corrida.
  - R3-03 y R3-09 (It. 3).
- **Cerrados**: R4a-03 (el presupuesto del HUD: el peor caso de la 5 da 56-57 de 60, sin juntar más elementos); R5-01 y R5-12 (un elemento que dibuja todo y su área de toque: se probaron en el proyecto); R5-02 (las medidas, tomadas en Unity); R5-06 (el oráculo del tracker sale idéntico al legacy); R3-08 (It. 4a); R4a-04 y R4a-05; R4b-04, R4b-07, R4b-09, R4b-11 y R4b-12 (It. 4b); R-T1, R-T2, R-T5 y R-T12 (It. 2).

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
| 2026-10-30 | — | El usuario aceptó los valores por defecto del gate de la It. 4b (D-ANI-05, DV-40 a DV-47, P-01 a P-16 de la 4b); lo registra la [spec de la 5 §11](iterations/05-series-parciales/01-po-spec.md). La demo (c) no fue concluyente ([00-deuda-4b §1](iterations/05-series-parciales/00-deuda-4b.md)). Empieza la It. 5: la deuda de la 4b ([00-deuda-4b](iterations/05-series-parciales/00-deuda-4b.md), [00-deuda-diseno](iterations/05-series-parciales/00-deuda-diseno.md)), spec, diseño técnico, gate de armonía con dos lentes, WP-A, WP-D1, WP-B, WP-D2 y verificación en dos rondas |
| 2026-10-30 | rowmate-po | **Cierre de la It. 5**: aceptada por el PO, con la verificación en el build pendiente para el comienzo de la 6a (el `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator`; la carpeta tiene el build de la 4b). QA: APROBADO CON OBSERVACIONES en la ronda 2 (dotnet: herramientas 192/192 y Core 2507/2541 pasan, 0 fallan; Unity EditMode con simulador 2755/2793 y PlayMode con simulador 495/495; matriz 656 PASA de 658, 0 FALLA; 71 AC PASS, 0 FAIL, 3 NV; LT-47 a LT-61 pasan; 1 issue menor, del Designer). Lentes de paridad y motion: APROBADO CON OBSERVACIONES; sus dos mayores (QA-V5-P01 y LM5-01) se corrigieron en la ronda 2. Reporte para el usuario en `iterations/05-series-parciales/REPORT.md`. Decisiones del PO al cerrar: la verificación en el build al empezar la 6a, QA-V5-R2-01 al Designer, la excepción de la llave contada en el REPORT, DT5-D1 y DT5-D2 aceptados y las precisiones del modo compacto. Pendientes de la spec §15 hechos: ROADMAP (la demo con `Series 600`, "También toca" de la 5, BKL-26), `workout-engine-hud` (F-WKT-07 y -18, el mapa) y FEATURE_INVENTORY (BKL-26). Spec con "Al cerrar la iteración". Legacy sin cambios (`git status --porcelain` vacío) |
