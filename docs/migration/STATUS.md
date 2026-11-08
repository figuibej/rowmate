# Estado de la migración

| Campo | Valor |
|---|---|
| Actualizado | 2026-11-08 (rowmate-po, al cerrar la It. 9) |
| Fase | **It. 9 cerrada con OK del usuario (rigen los valores por defecto de su gate). It. 10a en curso.** PO: **aceptada**, sin criterios en FAIL. QA: APROBADO CON OBSERVACIONES ([05-qa-verification](iterations/09-historial/05-qa-verification.md)), sin bloqueantes ni mayores: dotnet `-Matrix` 3500 (3461 pasan, 0 fallan, 39 omitidos), herramientas 192 / 192, matriz con 737 en PASA, 0 en FALLA, 4 ignorados y 163 pendientes de Unity; Unity EditMode 3603 / 3556 / 0 / 47 y con simulador 3795 / 3748 / 0 / 47; PlayMode 588 / 588 y con simulador 663 / 663; build de simulador con exit 0. De 43 criterios, 42 cumplen y 1 no es verificable (AC-HIS-19-05: el peor cuadro de recarga y los 60 fps en el build, sin sonda). Reporte para el usuario: [iterations/09-historial/REPORT.md](iterations/09-historial/REPORT.md). La deuda va a issues de GitHub |
| Próximo paso | **It. 10a** (`10a-ble-android-windows`: Bluetooth real en Android y Windows), que necesita el remo AMS-670B (u otro FTMS) y la PC con Bluetooth LE. La 6b sigue postergada (issue #6) |
| Plan | [ROADMAP.md](ROADMAP.md): 17 iteraciones + 1 *spike* opcional (que no se hace), 158 IDs. El usuario decidió mantenerlo: todo con el simulador hasta la It. 9 y el Bluetooth real en la 10a y la 11 |

## Iteraciones

| N | Iteración | Estado |
|---|---|---|
| 1 | Fundaciones: proyecto Unity, Core puro y piezas base | **Cerrada** ([reporte](iterations/01-fundaciones/REPORT.md)); el usuario aceptó los valores por defecto de su gate |
| 2 | Shell, Dispositivo en vivo y simulador | **Cerrada** ([reporte](iterations/02-dispositivo-simulador/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su condición (AC-BLE-11-02, QA-V2-01) se cumplió en la It. 3 |
| S-1 | *Spike* opcional de Bluetooth en Windows | **No se hace** (decisión del usuario en el gate de la It. 2; el riesgo se ve en la 10a) |
| 3 | Rutinas: lista, editor y base local | **Cerrada** ([reporte](iterations/03-rutinas/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su deuda se resolvió al empezar la 4a |
| 4a | Motor de sesión, guardado incremental, reposo y HUD funcional | **Cerrada** ([reporte](iterations/04a-motor-hud-funcional/REPORT.md)); el usuario aceptó los valores por defecto de su gate, también P-17 (el tiempo con el equipo suspendido no cuenta). Sus dos condiciones (QA-V4a-03 y QA-V4a-04) y su deuda se resolvieron al empezar la 4b ([00-deuda-4a](iterations/04b-hud-exr/00-deuda-4a.md)): AC-WKT-17-04 y AC-APP-05-07 pasan |
| 4b | HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema | **Cerrada** ([reporte](iterations/04b-hud-exr/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su deuda se resolvió al empezar la 5 ([00-deuda-4b](iterations/05-series-parciales/00-deuda-4b.md)) |
| 5 | Series en vivo: parciales y repeticiones | **Cerrada** ([reporte](iterations/05-series-parciales/REPORT.md)); el usuario aceptó los valores por defecto de su gate (lo registra la [spec de la 6a §11](iterations/06a-escena-lago/01-po-spec.md)). La verificación en el build que quedaba se hizo al empezar la 6a y pasó ([00-deuda-5](iterations/06a-escena-lago/00-deuda-5.md)). Solo sigue la demo (c) del usuario (AC-WKT-02-11) |
| 6a | Escena: cielo, luz, hora, Lago alpino y agua degradada | **Cerrada** ([reporte](iterations/06a-escena-lago/REPORT.md)); el usuario dio el OK. Su deuda quedó en los issues de GitHub #1 a #10 |
| 6b | Agua avanzada, desenfoque del vidrio y rendimiento | **Postergada** por decisión del usuario (issue #6). La 7 se hizo sobre el agua degradada |
| 7 | Remero, bote y palada | **Cerrada** ([reporte](iterations/07-remero-palada/REPORT.md)). Sin FAIL; AC-SCN-01-16 NO VERIFICABLE. El usuario siguió con la 8 sin cambiar nada de su gate: rigen los valores por defecto |
| 8 | Río, Costa, Regata y selector | **Cerrada** ([reporte](iterations/08-entornos-selector/REPORT.md)); el usuario dio el OK (rigen los valores por defecto de su gate). Sin FAIL; AC-SCN-01-19 NO VERIFICABLE solo en Android |
| 9 | Historial y detalle | **Cerrada** ([reporte](iterations/09-historial/REPORT.md)); el usuario dio el OK (rigen los valores por defecto de su gate). Deuda en los issues #32 a #39. Sin FAIL; AC-HIS-19-05 NO VERIFICABLE en parte (cuadro de 50 ms y 60 fps en el build) |
| 10a | Bluetooth real en Android y Windows | **En curso** (la prueba con el remo real queda para la demo del usuario) |
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
- **Gate de la It. 4b**: valores por defecto de D-ANI-05 (la cuenta grande y el sello), DV-40 a DV-47, P-01 a P-16 de la 4b y las decisiones del PO al cerrar. Lo registró la [spec de la 5 §11](iterations/05-series-parciales/01-po-spec.md). La demo (c) de P-17 (AC-WKT-02-11) no fue concluyente ([00-deuda-4b §1](iterations/05-series-parciales/00-deuda-4b.md)).
- **Gate de la It. 5**: valores por defecto de DV-48 a DV-51, P-08 y las decisiones del PO al cerrar ([REPORT de la It. 5 §8](iterations/05-series-parciales/REPORT.md)). Lo registró la [spec de la 6a §11](iterations/06a-escena-lago/01-po-spec.md). La demo (c) de P-17 (AC-WKT-02-11, suspender el equipo a mano) **sigue pendiente**.
- **Gate de la It. 6a**: el usuario dio el OK (valores por defecto de P-18, QA-V6a-12, DV-57 / P-03, P-19, D-ART-08 / P-01, P-02 y AC-WKT-02-11). Postergó la 6b (issue #6) y pasó la deuda a issues de GitHub (#1 a #10). Detalle en [REPORT de la It. 6a §8](iterations/06a-escena-lago/REPORT.md).

- **Gate de la It. 7**: el usuario siguió con la It. 8 sin cambiar nada, así que rigen los valores por defecto de la tabla de abajo (DV-68 a DV-78, DT7-09, QA7-V-06 y B7-04). Detalle en [REPORT de la It. 7 §6](iterations/07-remero-palada/REPORT.md).

- **Gate de la It. 8**: el usuario dio el OK, así que rigen los valores por defecto de la tabla de abajo (DV-79 a DV-89, DT8-10, QA-V8-14, D-ART-08). También pidió borrar el worktree `it08-entornos` y su rama (ya no están: `git worktree list` solo muestra `main`), commitear, pushear y empezar la It. 9.

### Para confirmar en el gate de la It. 9

Si el usuario no dice nada, se aplica "Por defecto". Detalle en [REPORT de la It. 9 §5](iterations/09-historial/REPORT.md) y [01-po-spec §10](iterations/09-historial/01-po-spec.md).

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-90 y DV-91: fecha, etiquetas y ejes en blanco al 54 % y ejes en 11 (legacy: 38 % y 9); tamaños del tablero EXR y área en degradé | Se aceptan | Colores y tamaños del legacy |
| DV-92 / Q-22: la lista no parpadea al recargar y también recarga al terminar una sesión | Se acepta | Indicador en cada recarga, como el legacy |
| DV-93 / Q-24: el split con lo rápido arriba, las paradas en el piso y el objetivo invertido | Se acepta | Split sin invertir, como el legacy |
| DV-94: el tooltip muestra solo el valor de la curva | Se acepta | Agrega el de la línea objetivo cercana |
| DV-95 / Q-13: los errores de carga se avisan con `Reintentar` | Se acepta | Indicador para siempre, como el legacy |
| Tiempo y distancia de la tarjeta = los del monitor (máximo de los puntos), como el legacy: con el simulador sin reconectar, la segunda sesión arranca desde lo acumulado | Paridad | Restar el primer punto (cambio de producto, issue) |
| Etiquetas X de los bordes que se pisan (`3:44` con `5:00`) | Paridad con `fl_chart` (issue con prioridad) | Se ocultan las marcas intermedias que chocan |

**Decididas por el PO al cerrar la It. 9** (no necesitan respuesta): AC-HIS-19-05 queda NO VERIFICABLE en la parte del build (la lista a 0,25 s y el detalle a 0,5 s sí se vieron; falta una sonda `-rowmate-perf-history`); el dato del cursor del §13 del diseño se corrige en el REPORT (asigna en 3 de 60 cuadros, no 0); VID-01 y VID-02 no se miraron por falta de `ffmpeg`, pero sus AC están cubiertos por tests y por el build; los menores de QA, del gate, del diseño y del arte van a issues de GitHub (8, agrupados; lista en el REPORT §7).

### Gate de la It. 8 (valores que rigen)

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-79: tarjetas de 152 × 96 con el nombre en 13 | Se acepta | 120 × 80 y 12, como el legacy |
| DV-80 y QA-P-11: miniatura horneada de la escena 3D a las 12:00 | Se acepta | Miniaturas a la hora actual (4 por escenario, issue) |
| DV-81 y DV-87: siluetas lejanas que se acercan sin deslizarse, con el tope de 9° | Se acepta | Deslizamiento y alto del legacy |
| DV-82: el número del cartel siempre en el tablero | Se acepta | Aparece a los ≈ 54 m |
| DV-83: faro con halo y haz giratorio de 12 s | Se acepta | Solo el halo |
| DV-84: muro, paseo, espuma, arena, taludes, álamos y público agregados | Se acepta | Sin público ni álamos |
| DV-85 y DV-86: rolido por escenario; la costa mueve el bote y muestra el oleaje | Se acepta | Rolido del lago y costa calma hasta la 6b |
| DV-88 y DV-89: boyas que cabecean y gaviotas 3D que aletean | Se acepta | Boyas quietas y gaviotas planas |
| DT8-10: el faro sobre un promontorio de 2,5 m (no 8 m × escala en ese bloque) | Se acepta | Acantilado entero, con la base del faro más alta que la del legacy |
| QA-V8-14: en horizontal, a las 22 h, la linterna del faro y la del farol quedan bajo la barra superior | Se acepta como está (issue) | El Designer cambia el encuadre o la altura |
| D-ART-08: sin texturas CC0 de fachadas | Sigue en No | Texturas CC0 detrás de slots |

**Decididas por el PO al cerrar la It. 8** (no necesitan respuesta): AC-SCN-01-19 queda NO VERIFICABLE solo en Android (en la PC cumple); QA-V8-12 se acepta la medición de la regata remando de 0 a ≈ 250 m (pasa por `E-180` y la tribuna); el issue #9 **no se cierra**: falta una sonda del farol que pase (QA-V8-R2-02), y su resto va con QA-V8-14; el worktree `.claude/worktrees/it08-entornos` y su rama se borran solo cuando el usuario lo diga (ya están en `.gitignore`); el resto de los menores y los "Para issues" de los docs van a issues de GitHub (lista en el REPORT §7).

### Gate de la It. 7 (valores que rigen)

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-68, DV-74 y DV-77: medidas y proporciones reales (bote fino, remero más chico que en el legacy) | Se aceptan | Escala del legacy en lo visible |
| DV-69, DV-70, DV-75 y DV-76: el remero cabecea con el casco, ritmo por SPM, plumeo continuo, arfada, rolido, *surge*, gotas y respiración | Se aceptan | Vuelve lo del legacy en cada uno |
| DV-72: sombra real en Medio y Alto, óvalo del legacy en Bajo | Se acepta | Óvalo en todos los niveles |
| DV-73: boya naranja de 500 m solo en los trabajos de rutina, a x +4,5 m, sin texto | Se acepta | Sin boya |
| DV-78: la tabla de 10 poses es la hoja medida (brazos algo doblados en la recuperación) | Se acepta | El Designer ajusta la técnica (issue) |
| DT7-09: el maniquí con pelo y anteojos, sin gorra | Se acepta | Gorra encendida |
| QA7-V-06: Y Bot mide 1,786 m; vale cualquier personaje del slot entre 1,55 y 2,05 m (el 1,80–1,88 m es del maniquí) | Se acepta | Escalar Y Bot a 1,85 m |
| B7-04: tope de SetPass en Medio ≤ 50 (el de la 6a), no ≤ 40 | Se acepta | Bajar SetPass en la 14 |

**Decididas por el PO al cerrar la It. 7** (no necesitan respuesta): AC-SCN-01-16 queda NO VERIFICABLE (se rehace `-rowmate-perf-scene` con la pantalla encendida en la próxima corrida de QA; Android en la 14); antes del commit se borra `Assets/New Text Document.txt` (y su `.meta`), se vuelven a guardar en UTF-8 `RowerCatalog.cs` y `SceneBudgetTests.cs` y se regenera `snapshots/INDEX.md` entero (QA7-V-04, QA7-V-R2-01); el resto de los menores (QA7-V-02, -03, -05, -07) y los "Para issues" van a issues de GitHub.

## Prerrequisitos del usuario abiertos

| Para | Qué | Estado |
|---|---|---|
| It. 9 (recomendado, 15 minutos) | La demo de [REPORT de la 9 §2](iterations/09-historial/REPORT.md) en `Builds/Windows/simulator` (ya tiene la 9): una libre, la rutina `Demo 9`, el detalle con las 5 métricas y una sesión cortada | Pendiente |
| It. 10a (necesario) | Remo AMS-670B (u otro FTMS) y la PC con Windows con Bluetooth LE; para Android, el teléfono con depuración USB | Pendiente |
| P-17 (recomendado, 5 minutos) | La demo (c) de la 4b: suspender el equipo a mano (Inicio → Encender/apagar → Suspender) con un entrenamiento en marcha ([REPORT de la 6a §3.1 (d)](iterations/06a-escena-lago/REPORT.md)). Cierra AC-WKT-02-11, que QA no puede verificar | Pendiente |
| It. 4a (opcional, 1 minuto) | `powercfg /requests` en una consola de administrador, con la app abierta y después de cerrarla ([REPORT de la 4a §3.7](iterations/04a-motor-hud-funcional/REPORT.md)). Cierra AC-APP-14-01 a -03 | Pendiente |
| Cierre de NV (It. 2 a 6a) y rendimiento de la escena | Módulos **Windows Build Support (IL2CPP)** (con Visual Studio 2022 "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621+) y **Android Build Support** (con OpenJDK y Android SDK & NDK Tools), más **un Android de gama media** (clase Galaxy A54 / Pixel 6a, Android 8+ de 64 bits) con depuración USB. Cierran IL2CPP (AC-BASE-01, R3-01; R5-04), AC-SCN-01-09 (60 fps con la escena, 30 min), la fila de 200 píldoras en el teléfono (R4b-02) y las partes Android de las It. 2 a 6a | Pendiente. Sin él, el rendimiento en el teléfono queda NO VERIFICABLE hasta la 14 (regla 9). **Recomendado para la 6b** (60 fps con el agua avanzada) |
| It. 6a (opcional) | **Terrain Sample Asset Pack** (Asset Store de Unity, gratis, 1,6 GB) | Opcional (P-02): la 6a se ve completa con terreno procedural |
| It. 2 (opcional, 5 minutos en la PC) | Abrir la app legacy después de la nueva (AC-APP-16-03); probar el build con Windows en modo claro (AC-APP-05-01 y, de la It. 3, AC-APP-05-10); los idiomas preferidos `Français (France)`, `Español (Argentina)` (AC-APP-09-06) | Pendiente |
| CI de Unity (opcional) | Licencia Personal de Unity como secreto del repositorio (P-08) | Pendiente. El repositorio está en GitHub, público, y `ci-core` corre en verde |
| It. 10a / 10b / 11 / 12 | Remo AMS-670B, Mac con Xcode + iPhone, pulsómetro, cuenta de Strava | Pendiente |

Resueltos: en la It. 2, Unity Hub con el Editor 6000.3.27f1 y Windows Standalone (Mono), y las fuentes e íconos. Entre la It. 2 y la 3, el repositorio en GitHub ([figuibej/rowmate](https://github.com/figuibej/rowmate), público, con `ci-core` en verde). Entre la It. 3 y la 4a, los avisos del Firewall. Entre la 4a y la 4b, el `RowMate.exe` que había quedado abierto. Al empezar la 6a, el build de la 5 (el `RowMate.exe` ya no estaba abierto: QA verificó la 5 en el build). Para la 7, Y Bot de Mixamo en `Assets/ThirdParty/Mixamo/YBot/` (no se versiona). Al cerrar la 8, el worktree `it08-entornos` y su rama (borrados a pedido del usuario).

## Pendiente de otros responsables

**Desde el OK de la 6a, la deuda vive en los issues de GitHub**: #1 a #10 (de la 6a), los de la It. 7 (hasta el #21) y los de la It. 8, que se crean al cerrarla (lista en el [REPORT de la 8 §7](iterations/08-entornos-selector/REPORT.md)). El #9 sigue abierto con un resto chico (la sonda del farol). Lo de abajo queda como registro del cierre de la 6a.

Issues de [05-qa-verification](iterations/06a-escena-lago/05-qa-verification.md) (ronda 3) y lo que quedó de [02-tech-design](iterations/06a-escena-lago/02-tech-design.md) ("Lo que queda"). Los de "Al empezar la 6b" van antes de construir encima.

| ID | Qué | Quién | Cuándo |
|---|---|---|---|
| **QA-V6a-R3-01** (mayor; AC-WKT-13-15 en FAIL) | Con la precarga de la escena sin terminar (primer arranque, Workout y `Entrenamiento libre` en pocos segundos), la entrada tiene un cuadro de 19,7-20,0 ms en el toque y otro de 20,7-20,8 ms a los ≈ 5,6 s, con movimiento. Partir, hacer en el negro o diferir las unidades largas de la precarga con el inmersivo a la vista; un *driver* `[PERF]` que entre antes de que termine la precarga y su prueba PlayMode | Architect | **Al empezar la 6b** |
| **QA-V6a-PAR-01** (menor; AC-SCN-13-03 en FAIL) | La 11.ª línea de brillo del agua degradada en la fila de abajo (`n` sin tope en el *shader*): tope en 10, una sonda que cuente las líneas y `it6a` regenerado (dos corridas) | Designer (con el Architect) | **Al empezar la 6b** |
| QA-V6a-R2-04 | El margen de la primera entrada del proceso (12,7 a 16,8 ms entre corridas): ejercitar por adelantado lo que corre por primera vez en el toque | Architect | Al empezar la 6b, con R3-01 |
| QA-V6a-12 | El halo de las 18 h y las montañas en vertical y en horizontal (si el usuario no decide otra cosa en el gate) | Designer | Al empezar la 6b |
| `DeviceScreenView.Render` | Corre en cada lectura del remo con el inmersivo arriba (≈ 20 asignaciones por lectura, a 1 Hz): saltarlo mientras no se ve | Architect | Al empezar la 6b |
| QA-V6a-R2-05, R2-02 (b), REN-13, REN-14, `B10G11R11` en Medio | Las unidades de la precarga en el teléfono, la memoria de RT de la escena sola, la GPU por pieza, un tope de dibujos de la UI en el build y el bandeado de Medio | Architect / QA | It. 14, con el teléfono |
| QA-V6a-08 = PAR-02 | Las sondas de la ventana `#FFE082` y de la cabaña en el *manifest* (hoy QA las mide en los PNG) | Architect / QA | It. 8 (cuando se proyecten las mallas de los objetos) |
| P-07 / REN-12 | El diálogo de completado quieto a 30 fps (permitido, no obligatorio) | PO / Architect | It. 14 |
| DT5-13 | Las celdas de los números de los paneles (`TabularCells`) repiten la lógica de `TabularNumber` | Architect | Cuando se toque `TabularNumber` por otro motivo |
| O-R2-01 (4a) | `FlushAndWait` devuelve `true` aunque no encole el cierre | Architect | Cuando se use |
| QA-V4a-L01 | Un monitor que no informa la distancia una vez por segundo puede trabar un paso por distancia y los parciales | Architect y QA | It. 10a, con el AMS-670B |

La deuda de la It. 5 quedó resuelta al empezar la 6a ([00-deuda-5](iterations/06a-escena-lago/00-deuda-5.md), [00-deuda-diseno](iterations/06a-escena-lago/00-deuda-diseno.md)): AC-WKT-13-15 y AC-WKT-18-21 pasaron en el build de la 5, la prueba a mano en la ventana dio bien, O-3, O-4 y O-5 cerrados, y QA-V5-R2-01 lo cerró el Designer. En la 6a se cerraron además, con evidencia, los mayores de las rondas 1 y 2 (QA-V6a-01 a -03, REN-01 a -10, R2-01) y los menores QA-V6a-04, -05, -06, -07, -09, -10, -11, -13 y R2-02 a R2-05.

Otra deuda abierta:

- **Para la 6b.** Lo de la tabla de arriba; el vidrio con desenfoque en el HUD y en el diálogo de completado (P-19); la elección del agua avanzada o la degradada; 60 fps en el teléfono (regla 9; si no hay teléfono, a la 14).
- **Para la It. 7** (hecho en la 7). La boya o marca de 500 m en el agua (P-06); `Series anteriores` abierto en vertical con el remero a la vista (P-03 de la 5); la sombra del bote del legacy, el cabeceo y las medidas reales del bote (D-ART-04); en L640, con el simulador, la llave dentro de la zona del bote (de la 4a).
- **Para la 10a.** QA-V4a-L01; RA-19 (R4a-12) y RA-08 (R4a-10) de Android; un cuadro tarde con el remo real (DT4a-10); la variante completa de ROB-04; el *lock* del *scheduler* para los *callbacks* nativos de Bluetooth; las barras del sistema en Android (R4b-05), el aviso de suspensión con IL2CPP (R4b-06) y el reemplazo atómico de `prefs.json` con IL2CPP (R5-04).
- **Para la It. 12.** El diálogo de completado con Strava; el indicio de que el diálogo a 1,3× en L640 se desplaza (O-01 de la 4b).
- **Para la It. 14.** Lectores de pantalla, DPI con dos monitores, la pantalla encendida solo donde hace falta (BKL-24), la háptica, el cierre de F-APP-15 y lo de la escena en el teléfono (arriba).
- **Deseables sin AC.** De la It. 3: `Guardar` con indicador (DN3-19); el alto que crece y la mini-barra que interpola (DN3-20). De la 5: un indicio de "hay más" en las filas desplazables de W1280, si el usuario lo pide.
- **Con teléfono.** Lo de las It. 3 a 5 (el teclado del sistema, "Atrás", el segundo plano, la pantalla encendida, las barras del sistema, el dedo sobre los chips) y, de la 6a, los 60 fps de la escena, la fila de 200 píldoras, la adaptación térmica y la profundidad en el nivel Bajo con GLES.
- **Backlog.** BKL-26: el aviso de cada vuelta con la diferencia contra la anterior (P-09 de la 5).
- **CI.** `ci-core` corre en GitHub; `ci-unity` espera la licencia (P-08).

Riesgos:

- **Abiertos**:
  - R-T6 / R3-01 (IL2CPP con el SQLite nativo), hasta que esté el módulo.
  - R3-02 y R3-13 (Android), hasta que estén el módulo y el teléfono.
  - R4a-01, R4a-10 / RA-08 y R4a-12 / RA-19, que se miden en la 10a.
  - R4b-01 (P-17 con el modo de espera moderno de Windows): hasta la demo (c) del usuario.
  - R4b-05, R4b-06 y R5-04, hasta que estén los módulos y el teléfono.
  - R4b-02 (la fila de 200 píldoras en el teléfono): pasa en la PC con la escena; en el teléfono, a la 14.
  - R-12 (60 fps en el Android de gama media con la escena): sin teléfono, NO VERIFICABLE hasta la 14.
- **Mitigados y medidos**: R4a-07, R4a-13, R4b-10, R5-11 y R3-03 / R3-09, como en la 5. En la 6a, 0 B por cuadro con la escena, el HUD y los paneles (con el control positivo) y 0 subidas de *shaders* después de `Ready` en el build.
- **Cerrados**: R5-10 (el build tomado por el `RowMate.exe` del usuario: no volvió a pasar en la 6a); R4a-03 (el presupuesto del HUD: la escena no suma elementos, 56-57 de 60); R5-01, R5-02, R5-06 y R5-12; R3-08; R4a-04 y R4a-05; R4b-04, R4b-07, R4b-09, R4b-11 y R4b-12; R-T1, R-T2, R-T5 y R-T12.

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
| 2026-11-02 | — | El usuario aceptó los valores por defecto del gate de la It. 5 (DV-48 a DV-51, P-08); lo registra la [spec de la 6a §11](iterations/06a-escena-lago/01-po-spec.md). Empieza la It. 6a: la deuda de la 5 en el build ([00-deuda-5](iterations/06a-escena-lago/00-deuda-5.md), [00-deuda-diseno](iterations/06a-escena-lago/00-deuda-diseno.md)), spec, diseño técnico, gate de armonía con dos lentes, WP-A, WP-D1, WP-B, WP-D2 y verificación en tres rondas con las lentes de paridad y rendimiento |
| 2026-11-06 | rowmate-po | **Cierre de la It. 6a**: aceptada por el PO, con AC-WKT-13-15 (QA-V6a-R3-01, la entrada con la precarga de la escena sin terminar) y AC-SCN-13-03 (QA-V6a-PAR-01, la 11.ª línea de brillo) en FAIL, como deuda al empezar la 6b. QA: APROBADO CON OBSERVACIONES en la ronda 3 (dotnet: herramientas 192/192 y Core 3010/3049 pasan, 0 fallan; Unity EditMode con simulador 3284/3329 y PlayMode con simulador 553/553; matriz 758 PASA de 762, 0 FALLA; 98 AC PASS, 2 FAIL, 1 NV; LT-62 a LT-87 pasan). Lentes de paridad y rendimiento: sus mayores, cerrados en la ronda 2. Reporte para el usuario en `iterations/06a-escena-lago/REPORT.md`. Decisiones del PO al cerrar: la deuda de la 6b, DT6a-06 registrado en AC-WKT-17-07, P-07 como permiso y QA-V6a-12 al gate con P-18. Pendientes de la spec §15 hechos: ROADMAP ("También toca" de la 6a con F-WKT-14, -17 y F-APP-05; D-ART-05; la boya en la 7; la deuda de la 6a en la 6b), `immersive-scene` (la precarga) y este STATUS. Spec con "Al cerrar la iteración". Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-11-06 | coordinador | El usuario dio el OK a la It. 6a, postergó la 6b (issue #6) y pidió llevar la deuda a issues de GitHub (#1 a #10) y entregar rápido. Empieza la It. 7 sobre el agua degradada, con Y Bot en la máquina de desarrollo |
| 2026-11-06 | rowmate-po | **Cierre de la It. 7**: aceptada por el PO, sin FAIL. QA: APROBADO CON OBSERVACIONES en la ronda 2 (dotnet: herramientas 192/192 y Core 3152/3191 pasan, 0 fallan; Unity EditMode con simulador 3437/3482 y PlayMode con simulador 573/573; matriz 656 PASA, 0 FALLA; 37 AC PASS, 0 FAIL, 1 NV; LT-88 a LT-98 pasan; QA7-V-01, el único mayor, cerrado con DV-78). Reporte para el usuario: `iterations/07-remero-palada/REPORT.md`. Decisiones del PO al cerrar: QA7-V-06 (altura de Y Bot) aclarada en AC-SCN-05-06, AC-SCN-01-16 NO VERIFICABLE, limpieza antes del commit y el resto a issues de GitHub. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-11-07 | coordinador | Empieza la It. 8 con los valores por defecto del gate de la 7. Dos corridas trabajaron en paralelo (una en un worktree local); se fusionaron en el árbol principal (02-tech-design §15) |
| 2026-11-07 | rowmate-po | **Cierre de la It. 8**: aceptada por el PO, sin FAIL. QA: RECHAZADO en la ronda 1 (29 PASS, 9 FAIL, 2 NV; 4 mayores: mundo vivo, 25 casos legacy, paridad en pantalla y tomas con sondas) y APROBADO CON OBSERVACIONES en la ronda 2 (dotnet: herramientas 192/192 y Core 3311/3350 pasan, 0 fallan; Unity EditMode con simulador 3600/3645 y PlayMode con simulador 624/624; matriz 700 PASA, 0 FALLA; 39 AC PASS, 0 FAIL, 1 NV; LT-99 a LT-138 pasan; `EnvironmentParityTests` con 0 dp de diferencia). Reporte para el usuario: `iterations/08-entornos-selector/REPORT.md`. Decisiones del PO al cerrar: AC-SCN-01-19 NV solo en Android, QA-V8-12 aceptado, el #9 sigue abierto con el resto del farol, el worktree espera al usuario y el resto a issues de GitHub. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-11-08 | coordinador | El usuario dio el OK a la It. 8 (rigen los valores por defecto de su gate), pidió borrar el worktree `it08-entornos` y su rama, commitear, pushear y empezar la It. 9 |
| 2026-11-08 | rowmate-po | **Cierre de la It. 9**: aceptada por el PO, sin FAIL. QA: APROBADO CON OBSERVACIONES (dotnet `-Matrix` 3461/3500 pasan, 0 fallan; herramientas 192/192; matriz 737 PASA, 0 FALLA; Unity EditMode con simulador 3748/3795 y PlayMode con simulador 663/663; build de simulador OK; 42 AC PASS, 0 FAIL, 1 NV: AC-HIS-19-05 en el build). Reporte para el usuario: `iterations/09-historial/REPORT.md`. Decisiones del PO al cerrar: AC-HIS-19-05 NV en la parte del build, el dato del cursor corregido en el REPORT, VID-01 y VID-02 cubiertos por tests y los menores a 8 issues de GitHub. Próxima: la 10a (la 6b sigue postergada). Legacy sin cambios (`git status --porcelain` vacío) |
