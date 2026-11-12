# Estado de la migración

| Campo | Valor |
|---|---|
| Actualizado | 2026-11-12 (coordinador, OK de la It. 6b con la ronda del salto) |
| Fase | **Plan completo**, salvo la 10b (flotante hasta que haya Mac). La 6b (agua avanzada) quedó **cerrada** con el OK del usuario ([reporte](iterations/06b-agua-avanzada/REPORT.md), [01-po-spec §10](iterations/06b-agua-avanzada/01-po-spec.md)). La It. 15 quedó cerrada con el OK del usuario |
| Próximo paso | A pedido del usuario: cargar los secretos de Unity (cierra #10), instalar Android Build Support y probar en un teléfono (#7, #46), la 10b con una Mac y las demos con hardware (10a, 11, 12, P-17) |
| Plan | [ROADMAP.md](ROADMAP.md): 17 iteraciones + 1 *spike* opcional (que no se hace), 158 IDs; desde el 2026-11-09, la 13 descartada y 25 IDs de Strava descartados (133 vivos). El usuario decidió mantenerlo: todo con el simulador hasta la It. 9 y el Bluetooth real en la 10a y la 11 |

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
| 6b | Agua avanzada, desenfoque del vidrio y rendimiento | **Cerrada** con el OK del usuario, después de la ronda del salto (AC-6B-20 b pasa: VID-01 1,41 ×; QA APROBADO CON OBSERVACIONES). Quedan 2 FAIL (AC-6B-01 y -02 → #1). Deuda en los issues #91 a #99 ([reporte](iterations/06b-agua-avanzada/REPORT.md), [spec §10](iterations/06b-agua-avanzada/01-po-spec.md), [verificación](iterations/06b-agua-avanzada/05-qa-verification.md)). QA: RECHAZADO en las rondas 1 y 2; el PO la acepta con **3 FAIL** de causa medida (AC-6B-01 y -02 → #1; AC-6B-20 b, el agua salta cada 16 m → issue nuevo). 31 PASS, 3 FAIL, 4 NO VERIFICABLE (-15, -26, -29 en la CPU de Alto con carga externa, -32 Android). Resuelve #2, #3, #12, #13, #22 y #85; en parte #1, #6 y #31. Decisiones DV-149 a DV-153 |
| 7 | Remero, bote y palada | **Cerrada** ([reporte](iterations/07-remero-palada/REPORT.md)). Sin FAIL; AC-SCN-01-16 NO VERIFICABLE. El usuario siguió con la 8 sin cambiar nada de su gate: rigen los valores por defecto |
| 8 | Río, Costa, Regata y selector | **Cerrada** ([reporte](iterations/08-entornos-selector/REPORT.md)); el usuario dio el OK (rigen los valores por defecto de su gate). Sin FAIL; AC-SCN-01-19 NO VERIFICABLE solo en Android |
| 9 | Historial y detalle | **Cerrada** ([reporte](iterations/09-historial/REPORT.md)); el usuario dio el OK ("pushea y pasa a la 10"): rigen los valores por defecto de su gate. Deuda en los issues #32 a #39. Sin FAIL; AC-HIS-19-05 NO VERIFICABLE en parte (cuadro de 50 ms y 60 fps en el build) |
| 10a | Bluetooth real en Android y Windows | **Cerrada** ([reporte](iterations/10a-ble-android-windows/REPORT.md)); el usuario dio el OK (rigen los valores por defecto de su gate). Deuda en los issues #40 a #56. La demo con el remo queda pendiente. Sin FAIL; 37 PASS y 6 NO VERIFICABLE: AC-BLE-06-08, -08-07, -09-03 y -91-02 esperan el AMS-670B; AC-APP-13-02 y -14-05, el módulo "Android Build Support" y un teléfono |
| 10b | Tanda Apple (iOS y macOS) | Flotante: cuando haya Mac |
| 11 | Pulsómetro real | **Cerrada** ([reporte](iterations/11-ble-pulsometro/REPORT.md)); el usuario dio el OK ("dale"): rigen los valores por defecto de su gate. Deuda en los issues #57 a #65. La demo con el pulsómetro queda pendiente. Sin FAIL; 39 PASS y 2 NO VERIFICABLE: AC-HRM-HW-01 (sensor real y AMS-670B) y AC-HRM-HW-02 (teléfono y "Android Build Support") |
| 12 | Exportar TCX (reemplaza a "Strava: conexión y subida") | **Cerrada** ([reporte](iterations/12-exportar-tcx/REPORT.md)); el usuario dio el OK ("dale"): rigen los valores por defecto de su gate (DV-116 a DV-127). Deuda en los issues #66 a #73. Sin FAIL; 25 PASS y 3 NO VERIFICABLE: AC-STR-09-24 (Android), -25 (Apple, 10b) y -27 (subida a Strava, demo del usuario) |
| 13 | ~~Strava: sincronización y Perfil~~ | **Descartada** (Strava pago: decisión del usuario del 2026-11-09) |
| 14 | Pulido, identidad, plataformas y CI | **Cerrada** ([reporte](iterations/14-pulido-plataformas/REPORT.md)); el usuario dio el OK (rigen los valores por defecto de su gate). QA: APROBADO CON OBSERVACIONES en la ronda 2. Sin FAIL; 38 PASS y 6 NO VERIFICABLE (DPI con dos monitores, 60 fps con la pantalla normal, la corrida real del CI y el arrastre de Android/Apple). Cerró los issues #21, #36, #40, #41, #42, #44 y #69; deuda en los issues #74 a #83 |
| 15 | Deuda resoluble en la PC (fuera del plan de paridad, pedida por el usuario) | **Cerrada** ([reporte](iterations/15-deuda-pc/REPORT.md)); el usuario dio el OK (rigen los valores por defecto de su gate, DV-140 a DV-148). Deuda nueva en los issues #84 a #90 ([spec](iterations/15-deuda-pc/01-po-spec.md), [verificación](iterations/15-deuda-pc/05-qa-verification.md)). QA: RECHAZADO en las rondas 1 y 2; el PO la acepta con 2 FAIL con la causa medida (AC-D15-04 → #34, AC-D15-39 → #78). 38 PASS, 2 FAIL, 0 NO VERIFICABLE. Resuelve #32, #35, #37, #38, #39, #66, #71, #72, #75, #76, #79 y #82; en parte #33, #34, #67, #70, #73, #74, #78, #80 y #81; #77 fuera |

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

- **Gate de la It. 9**: el usuario dio el OK ("pushea y pasa a la 10"), así que rigen los valores por defecto de la tabla "Gate de la It. 9" de abajo (DV-90 a DV-95, tiempo y distancia del monitor, etiquetas X como `fl_chart` con issue prioritario).
- **Gates de la It. 10a y la It. 11**: el usuario dio el OK; rigen los valores por defecto de sus tablas (abajo).

### Para confirmar en el gate de la It. 6b

Si el usuario no dice nada, se aplica "Por defecto". Detalle en [01-po-spec §7 y §10](iterations/06b-agua-avanzada/01-po-spec.md).

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-149: el agua refleja el cielo real y la orilla (en Alto, también el bote y el remero) con el peso del legacy | Se acepta | Color plano `skyHorizon` con el peso del legacy |
| DV-150: oleaje con carácter por escenario | Se acepta | La fórmula del legacy en los 4 |
| DV-151: la estela persiste 12 s y se abre | Se acepta | La V del legacy, sin memoria |
| DV-152: la degradada también como último paso de la adaptación, sin volver hasta reiniciar | Se acepta | Solo si el agua avanzada no carga |
| DV-153: umbrales de las sondas (1,3:1; 30 %; 15 %; 10 %; 1,15:1; 8 %) | Se acepta | Otros que proponga el Designer |
| Popa (#85): rige la cubierta 0,80-0,95 de la 6a (media 0,897) | Se acepta | Tope ≤ 0,82 y otra vuelta de arte |
| Cierre: AC-6B-01 y -02 a #1; AC-6B-20 b a un issue nuevo | Se acepta | Otra ronda de corrección antes del commit |

### Gate de la It. 15 (valores que rigen)

El usuario dio el OK ("ok"). Detalle en [01-po-spec §6 y §9](iterations/15-deuda-pc/01-po-spec.md).

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-140: un nombre por métrica (`Vatios`/`Watts`, `Split 500m`); `Split` solo en columnas angostas | Se acepta | Nombres del legacy por pantalla |
| DV-141: split `m:ss` en Historial y Detalle; `mm:ss` en Dispositivo y HUD | Se acepta | `mm:ss` en todos lados |
| DV-142: la curva no se pasa entre dos puntos | Se acepta | La cúbica de `fl_chart` con su pico |
| DV-143: botón de exportar también en la tarjeta | Se acepta | Solo en el Detalle |
| DV-144: vuelta con distancia negativa → `0`; `<Creator>` RowMate | Se acepta | El negativo del legacy; sin `Creator` |
| DV-145: la ventana recuerda lugar, tamaño y maximizado (no la pantalla completa) | Se acepta | Siempre centrada en 1280 × 720 |
| DV-146: tocar una fila de `Rendimiento por paso` resalta sus franjas al 22 % | Se acepta | Sin vínculo fila ↔ franja |
| DV-147 / DV-148: `Show BLE debug` sigue en inglés; no se hace FIT | Se acepta | Pasa al catálogo; FIT en otra iteración |
| Cierre: AC-D15-04 y AC-D15-39 FAIL con la causa medida, a #34 y #78 | Se acepta | Otra ronda de corrección antes del commit |

### Gate de la It. 14 (valores que rigen)

Si el usuario no dice nada, se aplica "Por defecto". Detalle en [01-po-spec §8 y §12](iterations/14-pulido-plataformas/01-po-spec.md).

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-128: ventana mínima 640 × 360 en Windows y macOS | Se acepta | Sin mínimo, como el legacy |
| DV-129: pantalla completa con `F11` o `Alt+Enter`, sin recordarla | Se acepta | Sin pantalla completa |
| DV-130: ventana inicial centrada | Se acepta | En (10, 10), como el legacy |
| DV-131: Android `com.iguisoft.rowmate`; iOS/macOS `com.iguisoft.rowmate.unity` | Se acepta | iOS/macOS reemplazan al legacy |
| DV-132: Windows sin MSIX (la carpeta del build comprimida) | Se acepta | MSIX con certificado (issue) |
| DV-133 / DV-134: ícono propio redibujado; arranque con logo fijo y fundido de 200 ms | Se acepta | Ícono del legacy; animación de marca (issue) |
| DV-135: Android en las 4 orientaciones, respetando el bloqueo | Se acepta | Sin vertical invertida en teléfonos |
| DV-136: el lector lee textos, botones, estados, avisos, cambio de paso y resumen del gráfico; no anuncia métricas solo | Se acepta | Anunciar métricas cada N s (issue) |
| DV-137: #32, se oculta la marca que choca con un borde y el split abre su eje a ±5 s | Se acepta | Etiquetas encimadas, como `fl_chart` |
| DV-138: sigue Unity 6000.3.27f1 | Se acepta | Evaluar una LTS más nueva |
| DV-139: APK del CI con la llave de depuración si faltan los secretos de firma | Se acepta | No publicar el APK sin firma |
| DV-V14-03: el arranque oscuro cuenta "desde el primer código de la app" (queda ≤ 0,1 s blanco de Windows) | Se acepta | *Launcher* nativo (issue) |
| QA-V14-11: el aviso de exportación sigue de vidrio al 86 % (contraste ≈ 14,7:1) | Se acepta | Aviso opaco (issue) |

**Decididas por el PO al cerrar la It. 14** (no necesitan respuesta): los 6 NO VERIFICABLE quedan para la demo, el teléfono, la Mac y la primera corrida del CI; `BleAllocationTests.TheReconnectBannerDoesNotAllocatePerFrame` (intermitente, It. 11) se arregla antes de cargar `UNITY_LICENSE`; los menores de QA, del gate, del diseño y del arte van a 10 issues de GitHub (agrupados), sin corregirlos antes del commit; #32, #37 y #51 siguen abiertos con su resto.

### Gate de la It. 12 (valores que rigen)

El usuario dio el OK ("dale"): rigen los valores por defecto. Detalle en [01-po-spec §8](iterations/12-exportar-tcx/01-po-spec.md) y el [reporte](iterations/12-exportar-tcx/REPORT.md).

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-116 / DV-117: sin metadatos de Strava; `<Notes>` con el nombre de la rutina, después de la última vuelta | Se acepta | Sin `Notes`, o antes de la primera vuelta como el legacy |
| DV-118: las vueltas salen de los índices de paso de los puntos, sin depender de la rutina | Se acepta | — (el legacy fallaba) |
| DV-119: sesión cortada con tiempo y calorías de los puntos, sin tocar la base; tiempo 0 → 0 kcal | Se acepta | Reparar y guardar en la base |
| DV-120: se quitan los caracteres que XML 1.0 no admite | Se acepta | Escapar solo los 5 del legacy |
| DV-121: Windows guarda directo en `Documentos\RowMate\TCX`, sin diálogo | Se acepta | Diálogo "Guardar como" (issue) |
| DV-122: Android abre la hoja de compartir | Se acepta | Guardar en Descargas (issue) |
| DV-123: el simulador exporta con `_SIM` en el nombre y `[SIM]` en `Notes` | Se acepta | No exporta, como el legacy |
| DV-124 / DV-126: solo desde el Detalle, y el botón solo con una sesión exportable | Se acepta | Acción en la tarjeta del Historial (issue); botón deshabilitado |
| DV-125 / DV-127: `Sport="Other"` (el remo se elige en Strava) y la distancia del monitor tal cual | Paridad | FIT (issue); restar el primer punto |

**Decididas por el PO al cerrar la It. 12** (no necesitan respuesta): AC-STR-09-24, -25 y -27 quedan para la demo del usuario (teléfono, Mac, subida manual); los 6 menores de QA (QA-V12-01 a -06), los menores del gate y los "Para issues" de la spec, el diseño y el arte van a issues de GitHub (8, agrupados), sin corregirlos antes del commit, salvo QA-V12-03 y -04, que son de documentos: la ROADMAP ya no atribuye Strava a la 12 ni a la 13, y "sobre la barra de pestañas" se lee como "al pie, sin tapar la barra superior" (el Detalle no tiene pestañas). El set `it9` de capturas no se regenera ahora (mostraría el botón nuevo).

### Gate de la It. 11 (valores que rigen)

El usuario dio el OK ("dale"): rigen los valores por defecto. Detalle en [01-po-spec §9](iterations/11-ble-pulsometro/01-po-spec.md) y [02-tech-design](iterations/11-ble-pulsometro/02-tech-design.md).

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-106 / Q-41: un sensor nuevo sin nombre borra el nombre guardado y se ve `Sensor desconocido` | Se acepta | El legacy: queda el nombre del anterior o la dirección |
| DV-107 / Q-42: error en lenguaje humano (`No se pudo buscar sensores`, `No se pudo conectar con {name}`) y el detalle técnico debajo | Se acepta | `Error al conectar: …` del legacy |
| DV-108: `Pulsómetro conectado` 2 s, solo en reconexiones automáticas | Se acepta | Sin aviso, como el legacy |
| DV-109: `Se perdió el pulsómetro, reconectando…` 4 s sobre el HUD; los avisos del remo tienen prioridad | Se acepta | Sin aviso, como el legacy |
| DV-110: lectura vencida = `--` + `Sin señal del sensor` y el corazón atenuado | Se acepta | Solo `--`, como el legacy |
| DV-111: con el Bluetooth apagado durante una caída, sigue intentando cada 3 s y reconecta al encenderlo | Se acepta (paridad) | Deja de intentar |
| DV-113: la reconexión va directo a la dirección recordada, sin buscar, también en Windows | Se acepta (paridad; R-01 se mira en la demo) | Buscar antes de reconectar en Windows |
| DV-114: Android sin servicio en primer plano | Se acepta | Servicio con notificación ya |
| DT11-15: si `Desconectar` u `Olvidar` fallan, la tarjeta dice `No se pudo conectar con {name}` | Se acepta | Texto propio para esas acciones |
| DT11-10: en el `Player.log` las direcciones van enmascaradas (`AA:BB:…:0D`) | Se acepta | Dirección completa, como el legacy |

**Decididas por el PO al cerrar la It. 11** (no necesitan respuesta): AC-HRM-HW-01 y -HW-02 quedan para la demo del usuario; los 3 menores de QA (m-01 a m-03), los menores del gate y los "Para issues" de la spec, el diseño y el arte van a issues de GitHub (agrupados, 10), sin corregirlos antes del commit; las capturas versionadas son de antes del último pulido (los avisos muestran check y `link_off` en vez del corazón): el coordinador las regeneró con el arte final antes del commit, junto con `Builds/Windows/normal` y `simulator` (no se versionan).

### Gate de la It. 10a (valores que rigen)

El usuario dio el OK ("pushea y arranca la proxima iteracion"): rigen los valores por defecto. Detalle en el reporte de la 10a (§6) y en [01-po-spec §8](iterations/10a-ble-android-windows/01-po-spec.md).

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-96 / Q-29: la reconexión reintenta 3 s después de cada fallo, sin límite, y `Buscar dispositivos` también la cancela | Se acepta | Un solo intento, como el legacy |
| DV-97: con el Bluetooth apagado durante la reconexión, reconecta solo al encenderlo | Se acepta | Deja de reconectar, como el legacy |
| DV-98 / Q-31: `El monitor dejó de enviar datos` queda en Dispositivo con `Reintentar` y 4 s sobre el HUD; la sesión sigue | Se acepta | Sin aviso, como el legacy |
| DV-99 / Q-34 y DD10-02: `Reconectando con {remo}…` sobre el HUD y `Reconectado con {remo}` 2 s, en el lugar de los avisos (cápsula de vidrio) | Se acepta | Sin aviso sobre el HUD, como el legacy |
| DV-100 y DT10-15: en Android, un permiso negado muestra la vista "sin permiso" (y se recuerda al reabrir), no `Error al escanear` | Se acepta | El banner de error del legacy |
| DV-101: `Dispositivo desconocido` traducido, también en la cabecera | Se acepta | `Unknown device` fijo |
| DV-102: `Activar Bluetooth` en todas las plataformas; en Windows abre los ajustes de Bluetooth; las pistas siguen hablando de iOS | Se acepta (issue para pistas por plataforma) | Pistas por plataforma ya |
| DV-103 / QA-V4a-L01: tope de 10 m por segundo desde el último cambio de distancia, en el paso y los parciales | Se acepta | Tope de 10 m por lectura |
| DV-104 / Q-37: el diagnóstico también se abre manteniendo presionada la cabecera del remo 0,5 s | Se acepta | Solo los 3 desplazamientos |
| DT10-12: sin servicio en primer plano en Android hasta que la demo muestre que el sistema mata el proceso | Se acepta | Servicio con notificación ya |
| DT10-18 / RA-19: el reloj de la app sigue sin contar el sueño profundo; se mide en el teléfono | Se acepta | `elapsedRealtime` en la app y en el plugin |
| `RowMateBle.dll` compilada por script y versionada en el repo, sin LFS (reproducible, sin rutas) | Se acepta | Se compila en cada máquina |

**Decididas por el PO al cerrar la It. 10a** (no necesitan respuesta): los 6 NO VERIFICABLE quedan para la demo del usuario (4 con el remo, 2 con Android); AC-BLE-04-04 pasa en Windows por código (no se apagó la radio, regla del workflow); QA-G10a-06: AC-APP-13-01 se lee como "los permisos de Bluetooth y ubicación, exactamente estos" (Unity puede sumar otros, como `INTERNET`), y se revisa con `aapt2` en la demo de Android; los 6 menores de QA (m-01 a m-06) y los "Para issues" de la spec, el diseño, el arte y el gate van a issues de GitHub (agrupados), sin corregirlos antes del commit. Ya quedaron resueltos en el código y **no** van a issue: `.gitattributes` (la DLL sin LFS), los constructores de WP-C, QA-G10a-07, -08, -10, -11, -12, -13 (búfer de 4 MB) y -14, y lo principal de WP-D (sonar, ilustración, check, token mono).

### Gate de la It. 9 (valores que rigen)

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
| It. 9 (opcional, 15 minutos) | La demo de [REPORT de la 9 §2](iterations/09-historial/REPORT.md) en `Builds/Windows/simulator` (ya tiene la 9): una libre, la rutina `Demo 9`, el detalle con las 5 métricas y una sesión cortada | Pendiente |
| It. 10a (demo, recomendado) | Con el AMS-670B: la demo de Windows del reporte de la 10a en `Builds/Windows/normal` (Bluetooth apagado, buscar, conectar, diagnóstico, entrenar, minimizar, corte breve, monitor apagado) y anotar la cadencia (`[BLE] Cadencia` del `Player.log`, que no se sube). Cierra AC-BLE-06-08, -08-07, -09-03 y la parte de remo de -91-02 | Pendiente |
| It. 10a, Android | Módulo **Android Build Support** (con OpenJDK y Android SDK & NDK Tools), `build.ps1 -Target Android -Backend IL2CPP` y un teléfono con Android 12+ y depuración USB ([tools/unity/README.md](../../tools/unity/README.md) §It. 10a). Cierra AC-APP-13-01 (APK), -13-02, -14-05, la vibración y Android de AC-BLE-04-04 y -91-02 | Pendiente |
| It. 11 (demo, recomendado) | Con un pulsómetro BLE (banda o reloj que transmita `0x180D`) y, si se puede, el AMS-670B: la demo de Windows del reporte de la 11 en `Builds/Windows/normal` (regenerado) y las líneas `[HRM]` del `Player.log` (no se suben). Cierra AC-HRM-HW-01; con el teléfono, AC-HRM-HW-02 | Pendiente |
| P-17 (recomendado, 5 minutos) | La demo (c) de la 4b: suspender el equipo a mano (Inicio → Encender/apagar → Suspender) con un entrenamiento en marcha ([REPORT de la 6a §3.1 (d)](iterations/06a-escena-lago/REPORT.md)). Cierra AC-WKT-02-11, que QA no puede verificar | Pendiente |
| It. 4a (opcional, 1 minuto) | `powercfg /requests` en una consola de administrador, con la app abierta y después de cerrarla ([REPORT de la 4a §3.7](iterations/04a-motor-hud-funcional/REPORT.md)). Cierra AC-APP-14-01 a -03 | Pendiente |
| Cierre de NV (It. 2 a 6a) y rendimiento de la escena | Módulos **Windows Build Support (IL2CPP)** (con Visual Studio 2022 "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621+) y **Android Build Support** (con OpenJDK y Android SDK & NDK Tools), más **un Android de gama media** (clase Galaxy A54 / Pixel 6a, Android 8+ de 64 bits) con depuración USB. Cierran IL2CPP (AC-BASE-01, R3-01; R5-04), AC-SCN-01-09 (60 fps con la escena, 30 min), la fila de 200 píldoras en el teléfono (R4b-02) y las partes Android de las It. 2 a 6a | Pendiente. Sin él, el rendimiento en el teléfono queda NO VERIFICABLE hasta la 14 (regla 9). **Recomendado para la 6b** (60 fps con el agua avanzada) |
| It. 6a (opcional) | **Terrain Sample Asset Pack** (Asset Store de Unity, gratis, 1,6 GB) | Opcional (P-02): la 6a se ve completa con terreno procedural |
| It. 2 (opcional, 5 minutos en la PC) | Abrir la app legacy después de la nueva (AC-APP-16-03); probar el build con Windows en modo claro (AC-APP-05-01 y, de la It. 3, AC-APP-05-10); los idiomas preferidos `Français (France)`, `Español (Argentina)` (AC-APP-09-06) | Pendiente |
| CI de Unity (opcional) | Los secretos `UNITY_LICENSE`, `UNITY_EMAIL` y `UNITY_PASSWORD` (y, opcionales, los 4 de firma de Android), según [tools/unity/README.md](../../tools/unity/README.md) §It. 14: CI. El intermitente de la It. 11 tiene su causa original corregida en la 15, pero falló 1 vez en 4 corridas completas por otra causa (#78): en el CI puede salir rojo de vez en cuando | Pendiente (P-08, #10). `ci-core` corre en verde; `ci-unity` se saltea en verde sin los secretos |
| It. 10a / 10b / 11 | Remo AMS-670B, Mac con Xcode + iPhone, pulsómetro | Pendiente |
| It. 12 (demo, opcional) | Una cuenta gratis de Strava (u otro servicio que acepte TCX) para subir a mano el archivo exportado ([spec §1](iterations/12-exportar-tcx/01-po-spec.md)). Ya no hacen falta credenciales de la API | Pendiente |

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
- **Para la It. 14** (antes "Para la It. 12"). El indicio de que el diálogo de completado a 1,3× en L640 se desplaza (O-01 de la 4b). El diálogo de completado con Strava se descartó con Strava (2026-11-09).
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
| 2026-11-08 | coordinador | El usuario dio el OK a la It. 9 ("pushea y pasa a la 10"): rigen los valores por defecto de su gate. Empieza la It. 10a |
| 2026-11-08 | rowmate-po | **Cierre de la It. 10a**: aceptada por el PO, sin FAIL; esperando OK y demo del usuario con el remo. QA: APROBADO CON OBSERVACIONES (dotnet `-Matrix` 3593/3632 pasan, 0 fallan; herramientas 192/192; matriz 772 PASA, 0 FALLA; Unity EditMode con simulador 3880/3928 y PlayMode con simulador 697/697; builds normal, `-BleSmoke` y `-Simulator` OK; Kotlin y DLL verificados; 37 AC PASS, 0 FAIL, 6 NV: 4 [BLE-HW] y 2 [Device-Android]). Decisiones del PO al cerrar: los NV a la demo, QA-G10a-06 precisado, m-01 a m-06 y los "Para issues" a issues de GitHub. Próxima: la It. 11 (la 6b sigue postergada y la 10b, flotante). Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-11-08 | coordinador | El usuario dio el OK a la It. 10a ("pushea y arranca la proxima iteracion"): rigen los valores por defecto de su gate. Empieza la It. 11 |
| 2026-11-09 | rowmate-po | **Cierre de la It. 11**: aceptada por el PO, sin FAIL; esperando OK y demo del usuario con el pulsómetro. QA: APROBADO CON OBSERVACIONES (dotnet `-Matrix` 3737/3776 pasan, 0 fallan; herramientas 194/194; matriz 1003 PASA, 0 FALLA; LT-139 a LT-145 pasan; Unity EditMode con simulador 4026/4074 y PlayMode con simulador 713/713; builds `-BleSmoke` y `-Simulator` OK; plugins sin cambios; 39 AC PASS, 0 FAIL, 2 NV: [BLE-HW] y [Device-Android]). Decisiones del PO al cerrar: los NV a la demo, capturas y build normal a regenerar, menores y "Para issues" a 10 issues de GitHub. Próxima: la It. 12 (la 6b sigue postergada y la 10b, flotante). Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-11-09 | coordinador | Capturas y builds `normal`/`simulator` de la It. 11 regenerados con el arte final; deuda en los issues #57 a #65. El usuario dio el OK a la It. 11 ("dale"): rigen los valores por defecto de su gate. Empieza la It. 12 con Strava simulado (sin credenciales de la API todavía) |
| 2026-11-09 | rowmate-po | **Strava descartado** por decisión del usuario (la API exige suscripción paga desde junio de 2026): ROADMAP con las viejas It. 12 y 13 DESCARTADAS y la nueva It. 12 "Exportar TCX" (`12-exportar-tcx`, F-STR-09); inventarios con 25 IDs descartados; spec de la It. 12 escrita (`iterations/12-exportar-tcx/01-po-spec.md`). Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-11-09 | coordinador | Deuda de la It. 12 en los issues #66 a #73. El usuario dio el OK a la It. 12 ("dale"): rigen los valores por defecto de su gate. Empieza la It. 14 |
| 2026-11-10 | rowmate-po | **Cierre de la It. 14**, la última del plan: aceptada por el PO, sin FAIL; esperando el OK del usuario. QA: RECHAZADO en la ronda 1 (7 mayores: cierre con la X, barra de título fuera de la pantalla, cuadro blanco al arrancar y 4 AC sin prueba) y APROBADO CON OBSERVACIONES en la ronda 2 (dotnet `-Matrix` 3984/4023 pasan, 0 fallan; herramientas 194/194; matriz 867 PASA, 0 FALLA; Unity EditMode 4098/4152 y con simulador 4292/4346, 0 fallan; PlayMode 690/690 y con simulador 767/767 en la segunda corrida (1 intermitente de la It. 11 en la primera); builds normal, simulador, `-CloseSmoke`, `-BootSmoke` y `-BleSmoke` OK; 38 AC PASS, 0 FAIL, 6 NV). Decisiones del PO al cerrar: DV-V14-03, el vidrio del aviso (QA-V14-11), AC-POL-06 NV y la deuda a 10 issues ([01-po-spec §12](iterations/14-pulido-plataformas/01-po-spec.md)). Cierra #21, #36, #40, #41, #42, #44 y #69. Plan completo salvo la 6b (postergada) y la 10b (flotante). Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-11-10 | coordinador | Deuda de la It. 14 en los issues #74 a #83. El usuario dio el OK a la It. 14 ("ok"): rigen los valores por defecto de su gate (DV-128 a DV-139, DV-V14-03 y el aviso de vidrio de QA-V14-11). Se cerraron en GitHub #21, #36, #40, #41, #42, #44 y #69. Plan completo salvo la 6b (postergada) y la 10b (flotante) |
| 2026-11-10 | rowmate-po | **Cierre de la It. 15** (deuda resoluble en la PC, fuera del plan): aceptada por el PO con 2 FAIL con la causa medida; esperando el OK del usuario. QA: RECHAZADO en la ronda 1 (5 mayores) y en la ronda 2 (2 mayores: AC-D15-04 y el intermitente del banner con otra causa) (dotnet `-Matrix` 4045/4084 pasan, 0 fallan; matriz 890 PASA, 0 FALLA, 18 EN EQUIPO; Unity EditMode 4163/4217 y con simulador 4357/4411, 0 fallan; PlayMode 731/732 y con simulador 811-812/813, con los topes de la escena fallando por la VM de WSL; builds normal, simulador y humos de desarrollo OK; 38 AC PASS, 2 FAIL, 0 NV). Cierra #32, #35, #37, #38, #39, #66, #71, #72, #75, #76, #79 y #82; el resto, a issues agrupados. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-11-11 | coordinador | Deuda de la It. 15 en los issues #84 a #90. El usuario dio el OK a la It. 15 ("ok"): rigen los valores por defecto de su gate (DV-140 a DV-148 y los 2 FAIL a #34 y #78). Se cerraron en GitHub #32, #35, #37, #38, #39, #66, #71, #72, #75, #76, #79 y #82; los 9 en parte tienen un comentario con lo que queda. Empieza la It. 6b |
| 2026-11-11 | rowmate-po | **Spec de la It. 6b** ([01-po-spec](iterations/06b-agua-avanzada/01-po-spec.md)): 38 AC; §3.0 la deuda de la 6a primero (AC-WKT-13-15 y AC-SCN-13-03 vuelven a PASA, QA-V6a-R2-04, QA-V6a-12 y Dispositivo bajo el inmersivo); agua avanzada y degradada en los 4 escenarios con la regla de elección por equipo y la caída sin cortar la sesión; olas sin salto a los 4096 m; vidrio con desenfoque ≤ 0,6 ms en Medio y el diálogo de completado con el bote detrás; set `it6b` (32 capturas, 5 videos). Issues: #1, #2, #3, #12, #13, #22, #31 y #85 enteros; #6 en parte (AC-6B-32, Android, a #7). DV-149 a DV-153 |
| 2026-11-11 | rowmate-po | **Cierre de la It. 6b**: aceptada por el PO con 3 FAIL de causa medida; esperando el OK del usuario. QA: RECHAZADO en la ronda 1 (7 mayores) y en la ronda 2 (3 mayores: AC-6B-01, -02 y -20 b) (dotnet `-Matrix` 4138/4177 pasan, 0 fallan; matriz 919 PASA, 0 FALLA; Unity EditMode 4265/4320 y con simulador 4459/4514, 0 fallan; PlayMode 795/796 y con simulador 876/877 dos veces, la única falla es `SceneBudgetTests(High)` a 1,515-1,80 ms con 25-31 % de carga externa; builds normal, simulador y `-BleSmoke` OK; pantalla en su modo normal; 31 AC PASS, 3 FAIL, 4 NV). Decisiones del PO al cerrar: la popa con la cubierta de la 6a, #13 entero, `INDEX.md` de la 6b y de la 7 repuestos, spec corregida (QA-V6b-16, G6b-06) y la deuda a 8 issues agrupados ([01-po-spec §10](iterations/06b-agua-avanzada/01-po-spec.md)). Resuelve #2, #3, #12, #13, #22 y #85; en parte #1, #6 y #31. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-11-12 | coordinador | Deuda de la 6b en los issues #91 a #98. El usuario pidió una ronda para el salto del agua cada 16 m antes del commit: la malla queda quieta y se apagan las ondas que el paso no alcanza (DT6b-27); QA APROBADO CON OBSERVACIONES (VID-01 1,41 ×, sonda de 400 m sin saltos, dotnet 4143/4182 y 0 fallan, EditMode 0 fallan, PlayMode 796/796 y con simulador 877/877). Restos menores en #99. El usuario dio el OK: rigen DV-149 a DV-153. Se cerraron #2, #3, #12, #13, #22, #85 y #91; #1, #6 y #31 siguen abiertos con su resto |
