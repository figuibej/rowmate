# It. 12 — Notas de diseño implementadas (WP-D)

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-09 |
| Entrada | [00-arte-exportar](00-arte-exportar.md), [02-tech-design](02-tech-design.md) §1.3, §5, §8 y §9 (con "Cambios por gate" y los desvíos B-01 a B-03 de WP-B), [01-po-spec](01-po-spec.md), [03-qa-gate](03-qa-gate.md) m-01, las capturas de WP-B2 en [snapshots/](snapshots/) |
| Modo | Entrega rápida: lo que la spec no pide va a "Para issues" (§6) |

Cero tokens, fuentes, glifos de fuente ni transiciones de USS nuevos. Todo sale de `Tokens.uss`, `Typography.uss`, `ExportMotionSpecs` y de las piezas de siempre (`IconButton`, `Spinner`, `LinkIndicator`, la cápsula de vidrio del `ToastLayer`).

**Manda la spec sobre la base del arte (m-01, 02-tech-design §9.2).** De [00-arte-exportar](00-arte-exportar.md) queda **descartado**: el botón tonal con texto en la cabecera del Resumen (D12-01/-02, A12-01/-02), la decisión "sin ícono" (D12-03), el antiparpadeo de 250/400 ms (A12-03; vale `MinBusyMs` = 300), el título `TCX guardado` con la carpeta `Exportaciones`, el recorte de la ruta en el medio (A12-06) y las claves `export*`/`folderDocuments` (§5). Valen D12-04 (éxito en la cápsula del enlace con el check), D12-05 (sin datos del equipo), D12-06 (Android sin aviso de éxito), D12-07 (error sin rojo) y D12-08 (el simulador se ve igual).

> **Corregido en la It. 15 (m-01, #72; AC-D15-18).** Estas notas siguen el diseño [§9.2](02-tech-design.md), no la base del arte: donde [00-arte-exportar](00-arte-exportar.md) dice otra cosa (botón tonal con texto, sin ícono, carpeta `Exportaciones`, claves `export*`/`folderDocuments`, antiparpadeo de 250/400 ms, recorte de la ruta en el medio), manda §9.2. Además, desde la It. 15 el botón usa los glifos `download`/`ios_share` **de la fuente** (ASSET_SLOTS §4.3; #66): DD12-01 y §2 quedan como historia, `ExportGlyph` y `ExportGlyphView` se retiraron, y los puntos 1, 3 (la acción en la tarjeta; el sello no) y 4 de §6 se resolvieron ([15-deuda-pc/04-design-notes](../15-deuda-pc/04-design-notes.md)).

---

## 1. Qué quedó implementado

| ID | Pieza | Dónde | Números |
|---|---|---|---|
| DD12-01 | Glifos `download` e `ios_share` dibujados (D12-03 según §9.2). **Retirado en la It. 15**: glifos de la fuente | `Core/Design/ExportGlyph.cs`, `Presentation/Controls/ExportGlyphView.cs` | Material Symbols **Rounded**, la familia de la fuente de la app, con la receta de [ASSET_SLOTS §4.4](../../design/ASSET_SLOTS.md): trazo 2,25 (peso 500, opsz 24), remates y uniones redondos, esquinas exteriores de radio 2 (§2). Reemplaza el `download` de Material Icons clásico de WP-B (B-01): relleno, de esquinas vivas y con un eje de 6, se veía más pesado que la flecha de volver de la misma AppBar |
| DD12-02 | Botón de la AppBar | `Styles/Skin.uss` §It. 12 (colores), `Styles/Base.uss` §It. 12 (estructura) | La caja de 48 de `IconButton` con su presión, su capa de estado (hover al 4 % en escritorio, CAP-12; presionado al 8 %) y su anillo de foco. Glifo de 24 en `text.primary` (`.rm-icon-button__icon`). Exportando: `Spinner` 18/2 en `text.primary`, el color del glifo al que reemplaza (el botón cambia de forma, no de color), sin el 0,38 de `:disabled` (`.rm-export-button--busy`) |
| DD12-03 | Aviso con acción | `Styles/Skin.uss` §It. 12 | Cápsula de vidrio del enlace (`glass.fallback`, radio 22, borde `border.default`). Título `body-m` en `text.hudPrimary` (≥ 15:1); segundo renglón (el nombre del archivo) en `caption` 11/500 `text.secondary`: 9,4:1 sobre el vidrio con una tarjeta detrás y 8,2:1 en el peor fondo (el cian de la pestaña elegida detrás); acción de texto `button-s` en `accent.onTonal`: 9,8:1 y 8,0:1. Check del `LinkIndicator` en `state.connected` solo en el éxito; el error, sin indicador ni rojo (D12-07) |
| DD12-04 | Colores fuera de `Base.uss` | `Styles/Base.uss`, `Styles/Skin.uss` | Los tres colores que WP-B había puesto en `Base.uss` (B-02) pasan a `Skin.uss`, que se importa después (`RowMateTheme.tss`): estructura en Base, aspecto en Skin, como en las iteraciones anteriores. El resultado en pantalla no cambia |

## 2. Los glifos (caja de 24, y hacia abajo)

| Glifo | Pieza | Geometría (líneas centrales, trazo 2,25) |
|---|---|---|
| `download` | Flecha | Eje de (12; 4,5) a la punta (12; 14,5); brazos a 45° hasta (8; 10,5) y (16; 10,5). Remates redondos; la punta, unión redonda (borde exterior en y 15,625) |
| | Bandeja | U entre x 4 y 20 (bordes exteriores) con fondo en y 20; laterales hasta un remate redondo centrado en y 15,875; esquinas exteriores de radio 2 e interiores vivas, como el original |
| `ios_share` | Flecha | Punta en (12; 3,25), brazos a 45° hasta (9; 6,25) y (15; 6,25), eje hasta (12; 15,75). Remates y punta redondos |
| | Caja | De x 4 a 20 y de y 8,75 a 23 (bordes exteriores), abierta arriba entre x 9,125 y 14,875 (remates redondos de los bordes en x 8 y 16), esquinas exteriores de radio 2 |

- **Un contorno simple por pieza** (arcos de 4 tramos por cada 90°), sin piezas encimadas y todas con el mismo giro: con un color con alfa no aparecen zonas más claras (regla de §4.4). Los arma un constructor estático una sola vez; `ExportGlyphView` recorre arreglos fijos: **0 B al dibujar**.
- Aire entre piezas (como en el original): punta de `download` a la bandeja 2,1; brazos de `ios_share` al borde de la caja 1,4; eje a los bordes 1,75.
- Pruebas (dotnet y Unity), `Tests/EditMode/Design/ExportGlyphTests.cs`: coordenadas en 0-24 y sin repetir el primer vértice; punta abajo en `download` y arriba en `ios_share`; ejes de 2,25; contornos simples, horarios, que no se cruzan ni quedan uno dentro de otro; los arreglos son siempre los mismos.

## 3. Estados del botón y del aviso (sin cambios de tiempos)

| Estado | Qué se ve | Motion |
|---|---|---|
| Reposo | Glifo de 24 en `text.primary` | Presión y capa de `.rm-icon-button` (`Motion.uss`) |
| Hover / foco (escritorio) | Capa blanca al 4 % (círculo de 40) y la ayuda `Exportar TCX` debajo (CAP-12); con Tab, el anillo de foco | Los de siempre |
| Exportando | `Spinner` 18/2 en el lugar del glifo; botón deshabilitado sin atenuar; ayuda `Exportando…` | Glifo → spinner en el acto (manda la spec, A12-03); el spinner dura ≥ 300 ms (`ExportMotionSpecs.MinBusyMs`) |
| Guardado (Windows) | Aviso: check · `Guardado en Documentos\RowMate\TCX` / nombre del archivo · `Abrir carpeta` | Entrada y salida comunes del `ToastLayer`; 6 s (`NoticeMs`) |
| Error | Aviso: `No se pudo exportar la sesión` · `Reintentar`, sin indicador ni rojo | Ídem |
| Reducir movimiento | Igual | Sin escala al presionar; el spinner sigue (indica espera); aviso con el fundido de siempre |

## 4. Revisión de las capturas (CAP-01 a -12)

Regeneradas con el glifo nuevo (`capture-snapshots.ps1 -Set it12`), en [snapshots/](snapshots/).

| CAP | Veredicto |
|---|---|
| 01-03 | El `download` Rounded tiene el mismo peso visual que la flecha de volver: los dos extremos de la AppBar se leen como un par. El título no se corre |
| 04 | El spinner de 18/2 queda centrado en la caja de 48, en blanco y sin atenuar |
| 05, 10, 11 | Cápsula centrada sobre la barra de pestañas; título, archivo y acción alineados; la acción no baja de renglón (también a 1,3× en inglés) |
| 06 | En TV el título ajusta en dos renglones (la ruta no se parte en `\`) y el archivo va debajo; el check queda centrado en el bloque. Legible, sin `…` |
| 07, 08 | Error en la misma cápsula, sin indicador: no alarma. En TH la acción va a la derecha y el texto a la izquierda |
| 09 | Sin botón en la sesión sin telemetría: la AppBar queda limpia |
| 12 | Capa de hover y la ayuda debajo del botón, dentro de la ventana |

## 5. Presupuesto

- Botón: 0 elementos nuevos respecto de WP-B (glifo, spinner, capa y anillo ya estaban). Aviso: los de WP-B (cuerpo, detalle y acción), creados una vez.
- Dibujo del glifo: 2 contornos de 40-60 vértices cada uno, rellenos con `Painter2D` solo al repintar (no por cuadro); 0 B.

## 6. Para issues

1. Pasar `download` e `ios_share` al recorte de la fuente (ASSET_SLOTS §4.1, necesita red) y sacar `ExportGlyph` (R-08 del diseño).
2. Pausar el reloj del aviso con acción mientras el puntero o el foco están encima (A12-04).
3. Acción "Exportar TCX" en la tarjeta del Historial y un sello sutil de "exportada" (00-arte §8).
4. Glifos `cloud_upload`, `cloud_download`, `cloud_done`, `person` y `link` sin uso tras descartar Strava: revisar si se quitan del recorte.
