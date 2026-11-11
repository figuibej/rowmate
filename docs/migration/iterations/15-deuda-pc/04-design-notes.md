# It. 15 — Notas de diseño implementadas (WP-D)

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-10 |
| Entrada | [02-tech-design](02-tech-design.md) §3 (WP-D), DT15-04, -10, -13, -14, -22 y "Cambios por gate"; [01-po-spec](01-po-spec.md) (manda sobre el arte); [00-arte-deuda](00-arte-deuda.md) solo en lo que sigue vigente (D15-01, D15-05, 82-c); las tomas de WP-B en [snapshots/](snapshots/) |
| Modo | Entrega rápida. Lo menor va a "Para issues" (§5) |

Cero tokens, colores, fuentes ni shaders nuevos (gate QA-G15-01 y -02: sin estela, sin `WakeArt`, sin `freeze_tnum.py` ni `RowMate Numeric` de Inter). Lo visual que depende de un dato (el color del tipo de paso, la sombra 9-slice del *tooltip*, la cascada del HUD) va en C# y es de WP-B; acá, la USS, el recorte de la fuente de íconos y los documentos.

## 1. Qué quedó implementado

| ID | Pieza | Dónde | Números |
|---|---|---|---|
| DD15-01 | Recorte nuevo de la fuente de íconos (#66, AC-D15-11, -12; DT15-14) | `UI/Icons/RowMateIcons-{Outline,Filled}.ttf`, `NOTICE.md`, [ASSET_SLOTS](../../design/ASSET_SLOTS.md) §4.1, §4.3 | Receta de §4.1 con `uvx --from fonttools==4.67.0` sobre la fuente variable del commit `10ecc14` (bajada a `<scratch>`, no se versiona). Contorno: 43 glifos (+ `download` `F090`, `ios_share` `E6B8`; − `cloud_upload`, `cloud_download`, `link`), 9 008 B. Relleno: 6 (− `cloud_done`), 2 464 B. Quedan `person` y `link_off` (se usan). Los 41 + 6 que ya estaban, idénticos (`IconsTests.ExistingGlyphsUnchanged`) |
| DD15-02 | Recorte reproducible | ídem | `SOURCE_DATE_EPOCH=1792440351` (la fecha de `head.modified` de la fuente variable): dos corridas, mismos bytes (SHA-256 `fd75c721…` y `b45808f4…`). Sin esa variable lo único que cambia es la fecha de `head` y su suma: con la fecha de la It. 3 la receta reproduce **byte a byte** el recorte de la It. 3 (verificado). Los `FontAsset` de `UI/Icons/Generated` son dinámicos con las tablas vacías: no hay índices de glifo viejos que regenerar |
| DD15-03 | Botón de exportar de la tarjeta (#67, AC-D15-09; DT15-04) | `Skin.uss` §It. 15 (`.rm-export-button--card`) | Área de toque de 40 con −11 dp arriba y abajo: el renglón mide lo que el glifo (18) y la tarjeta no crece. Glifo `text.secondary` (el mismo gris del *chevron*: los dos son secundarios al nombre). Capa de estado en un círculo de 32. Los estilos en línea de WP-B pasaron a la USS (`ExportTcxButton` solo pone la clase) |
| DD15-04 | Fila marcada de `Rendimiento por paso` (#33, AC-D15-02) | `Skin.uss` (`.rm-step-perf--selected`) | Borde de 1 dp y relleno 7/9 (el 8/10 de siempre menos el borde: el contenido no se mueve). El color del borde, el del tipo (`StepBandPalette`), sigue en línea. Sin transición de USS: la marca cambia sin puntero y las capturas tienen que salir iguales (Motion.uss, regla 2); las franjas animan por código (160 ms `ease.enter`) |
| DD15-05 | *Tracking* del HUD por contenido (#76, AC-D15-24; DT15-10) | `Skin.uss` (`.rm-hud-card__label.rm-hud-card__label--mixed`) | 4 en TextCore = 0,04 em con alguna minúscula (`Split 500m`, `Vatios`/`Watts`, `kcal`); `SPM` y `BPM` siguen en 8 (`.rm-type-label`). Dos clases, para ganarle a `.rm-type-label` de `Typography.uss` sin depender del orden. Sin elementos nuevos |
| DD15-06 | Cifras de los ejes (#37/#76, AC-D15-25; DT15-22) | `Skin.uss` (`.rm-chart__axis.rm-chart-axis__num`) | RowMate Numeric SC 600 a 11 dp en `text.tertiary`, sin *tracking*. **Hallazgo**: `.rm-chart__axis` (Base.uss, It. 9) ya pedía esa cara; los ejes no estaban en Inter (el diseño §2.1 decía otra cosa). Lo que cambia de verdad en la 15 es que `AxisCrowding` mide con los anchos de la SC (WP-A-09). La fecha de la tarjeta sigue igual (TY-02) |
| DD15-07 | Cinta `SIMULADOR` (#82, AC-D15-35; DT15-13) | `Skin.uss` (`.rm-has-sim-ribbon .rm-appbar__title`, `__back`) | El contenido de la AppBar se corre 28 dp: el título de una pestaña arranca en 44 (margen 16 → 44) y el glifo de la flecha de volver queda en 44-68 (su caja de 48, en 32-80; el título que la sigue mantiene sus 20). **Desvío de DT15-13**: no va en el relleno de `.rm-appbar`, porque ese relleno es el área segura y `ShellLayout.ApplyAppBar` lo pone en línea (la regla de USS no tenía efecto: medido). La franja de la cinta es `x + y ∈ [39,5; 56,5]`: el título (44 + 16) y el glifo (44 + 16) quedan afuera. `SimRibbonTitleTests` y `RoutinesSimulatorLayerTests.BackArrowUnderTheRibbonWorks` miden ahora el **glifo** de la flecha (la AC habla de la flecha; la caja de 48 puede pasar bajo la cinta, que no recibe toques). El build normal no cambia |
| DD15-09 | Prueba de las cifras de los ejes (AC-D15-25, faltaba la parte [Editor]) | `Tests/PlayMode/It15/ChartAxisFontTests.cs` (N) | Las etiquetas X e Y con `.rm-chart-axis__num`, la cara `RowMateNumericSC-SemiBold-SDF`, 11 dp y `text.tertiary`; `0000` … `9999` con el mismo ancho, igual a `AxisDigitAdvanceEm` × 11 (± 0,3); la fecha de la tarjeta sigue en la SC y sin la clase de los ejes |
| DD15-08 | Sombra del *tooltip* (#37, AC-D15-06) | — (WP-B-06, `ShadowTexture` + `SessionChartCursor`) | Revisada en CAP-04: negro al 35 %, 8 dp, +2 dp. Se lee como un apoyo suave sobre la curva, sin halo. No hace falta USS (el 9-slice y el tinte van en línea porque la textura se genera) |

## 2. Revisión de las capturas del set `it15` (las de WP-B, antes de WP-D)

| Toma | Qué se veía | Qué cambia con WP-D |
|---|---|---|
| CAP-01, -02 | El botón de la tarjeta con el **genérico** de `[SLOT] ui.icons` (cuadrado redondeado blanco): la fuente no tenía `download` | El glifo `download` de la fuente, 18 `text.secondary` |
| CAP-03 | Fila 2 con borde rosa (trabajo) de 1 dp; franjas del paso 2 al 22 % | Igual (el borde ahora sale de la USS) |
| CAP-04 | Escalones sin pico en `Split 500m`, resumen `3:00`, *tooltip* con sombra | Igual |
| CAP-05, -06 | Barra del Detalle con el genérico en lugar de `download` | El glifo de la fuente, 24, el peso de la flecha de volver |
| CAP-07, -08 | `Vatios`, `Split 500m`, `kcal` todavía a 0,08 em (se veían separados al lado de `SPM`) | 0,04 em |
| CAP-09 | La cinta tapaba la `R` de `Rutinas` | El título a 44: entero a la derecha de la cinta |
| CAP-10 / AUX-15-01 | El "rectángulo" claro detrás del bote desaparece con el casco oculto: es la popa (D15-05 confirmado) | Nada (va a issues) |

El set `it15` se regeneró después de WP-D (`capture-snapshots.ps1 -Set it15`, solo en esta carpeta): 11 tomas, 0 con problemas, 0 sondas de aspecto que fallan. Revisadas: `download` de la fuente en la tarjeta (18, gris secundario, alineado con la fecha y el *chevron*) y en la barra del Detalle (24, el peso de la flecha); `Vatios`/`Split 500m`/`kcal` a 0,04 em al lado de `SPM` a 0,08; `Rutinas` entero a la derecha de la cinta.

## 3. Lo que afecta capturas de otras iteraciones (no se regeneran acá)

- `it9` (CAP-01 a -04 de la lista y las del Detalle) y `it14` (CAP-12, -19, -20, -22, -23, -27): el botón de exportar pasa del genérico al glifo de la fuente. Se vuelven a tomar con su set.
- Los sets del HUD de la 4a, 4b, 5, 6a y 7: rótulos con 0,04 em (ya estaba anotado por la spec §8).
- Los sets con simulador (2, 3, 4a…): la AppBar corrida 28 dp a la derecha.

## 4. Presupuesto

- 0 elementos nuevos (todo son clases sobre elementos de WP-B). 0 transiciones de USS nuevas.
- Fuente de íconos: −296 B (contorno) y −236 B (relleno); el atlas de 512 × 512 sigue con lugar (43 glifos).

## 5. Para issues

1. El aviso `No se pudo abrir la carpeta` (CAP-06) sale sin ícono; los demás avisos de exportar llevan el check o el `error`. Sumar `error` en `text.secondary` (sin rojo, como D12-07) le da la misma anatomía.
2. Los 6 glifos reservados sin uso (`delete_sweep`, `expand_more`, `expand_less`, `landscape`, `arrow_drop_up`, `arrow_drop_down`): decidir si salen del recorte (WP-A-04).
3. La fila marcada podría sumar un velo del color del tipo al 6 % (como la franja) para leerse mejor a 1,5 m; necesita el color en línea (código) y el OK del PO.
4. Las del arte que siguen fuera: popa del casco (degradé y afinado de los últimos 0,6 m), V14-10 (960 dp de ancho máximo), columnas parejas entre tarjetas, resto de #33 y sello de #67.
