# It. 12 — Base de arte: Exportar TCX

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-09 |
| Iteración | 12 · `12-exportar-tcx` (reemplaza a la vieja It. 12 de Strava, descartada el 2026-11-09) |
| Entrada | [ART_BIBLE](../../design/ART_BIBLE.md), [ANIMATION_BIBLE §2](../../design/ANIMATION_BIBLE.md), [ASSET_SLOTS §4](../../design/ASSET_SLOTS.md); `Presentation/Styles/` (`Tokens.uss`, `Motion.uss`, `Skin.uss`); [09/00-arte-historial](../09-historial/00-arte-historial.md) §2 y §6; [11/00-arte-pulsometro](../11-ble-pulsometro/00-arte-pulsometro.md) §5; `Presentation/History/SessionDetailPage.cs`, `Shell/ToastLayer.cs`, `Controls/{RmButton,LinkIndicator}.cs` |
| Modo | Entrega rápida: cada duda resuelta con un valor por defecto, en una línea |

**Cero tokens nuevos, cero fuentes nuevas y cero glifos nuevos** en esta iteración: todo sale de `Tokens.uss`, `Typography.uss`, la variante `--tonal` del `RmButton`, el `Spinner` y el check dibujado del `LinkIndicator`. Contrastes en sRGB con el alfa compuesto (WCAG 2.x). Ventanas: **TV** 411 × 914, **TH** 914 × 411, **W1280** 1280 × 720.

## 0. Decisiones (una línea cada una)

- **D12-01** El botón va en la **cabecera de la tarjeta Resumen**, a la derecha de `sessionSummary`: se ve al abrir el detalle sin desplazar, en las tres ventanas (el detalle es de una columna en todos los tamaños, 09/02-tech-design §10.2). No va en la AppBar: ahí le roba ancho al nombre de la rutina.
- **D12-02** Jerarquía **secundaria**: `RmButton` variante `--tonal`, tamaño chico (`.rm-type-button-s`). Nunca primario: exportar es una acción ocasional, no la tarea de la pantalla.
- **D12-03** **Sin ícono** en esta iteración. El glifo correcto (`ios_share`, `E6B8`) no está en el recorte, y agregarlo exige bajar la fuente variable (ASSET_SLOTS §4.1), cosa que este workflow no puede hacer porque no tiene red. `cloud_upload` queda descartado porque sugiere nube y era de Strava. Va a "Para issues".
- **D12-04** El **éxito** usa la cápsula del aviso de enlace (`.rm-toast--link`) con el **check dibujado** del `LinkIndicator` (`Check`, `state.success`) más una **acción**. Es el mismo lenguaje que "Listo" y "Reconectado" de la 10a.
- **D12-05** La ruta se muestra **abreviada y sin datos del equipo**: nunca la unidad, `Users\<nombre>` ni la ruta absoluta (ver §3).
- **D12-06** Android con hoja de compartir: **no hay aviso de éxito**. La hoja del sistema ya es la confirmación, y cancelarla no muestra nada.
- **D12-07** El error es un aviso con la acción "Reintentar". Va sin rojo ni sacudida: es un contratiempo, no un peligro.
- **D12-08** Las sesiones del simulador se ven igual, porque la marca va solo en el nombre del archivo (es cosa del PO).

## 1. Botón "Exportar TCX" (Detalle, tarjeta Resumen)

| Pieza | Spec |
|---|---|
| Cabecera | Fila `row`, `align-items: center`, `justify-content: space-between`, alto mínimo 36. A la izquierda va `sessionSummary` `titleS` 16/600 `text.primary` (sin cambios) y a la derecha el botón. Debajo, los 12 de aire de siempre hasta la `StatRowView` |
| Botón | `.rm-button.rm-button--tonal`, `min-height` 36, relleno horizontal 12, radio `r.full`, texto `exportTcx` en `.rm-type-button-s` 12/600 `accent.onTonal` sobre `accent.tonalBg` |
| Contraste | `#38CBEF` sobre `tonalBg` compuesto con `surface.card` (≈ `#173E57`): **5,9:1** (AA) |
| Área táctil | 48 dp de alto: la cabecera tiene 36, así que se extiende 6 hacia arriba y 6 hacia abajo dentro del relleno de 16 de la tarjeta (A12-02). En escritorio el foco usa el anillo `.rm-focus-ring` de siempre |
| Letra grande (1,3×) | Si el título y el botón no entran en una fila, el botón baja a su propio renglón, alineado a la derecha y con 8 de aire. Nunca se muestra "…" en el botón |
| Deshabilitado | Solo si la sesión no se puede exportar (lo decide el PO, por ejemplo una sesión sin puntos). Toma la opacidad 0,38 de `:disabled` y queda visible, para que se entienda que la acción existe |
| Vertical / horizontal | El mismo lugar y el mismo tamaño en TV, TH y W1280. En TH el resumen ya queda arriba sin desplazar |

## 2. Estados y movimiento (tiempos de los tokens; nada en USS nuevo)

| Estado | Qué se ve | Motion |
|---|---|---|
| Reposo | Tonal | Presión: las reglas `.rm-button` de `Motion.uss` (escala 0,97, 90 a la ida y 240 a la vuelta; la capa de estado en `dur.micro` y `dur.fast`) |
| Exportando | El texto funde a 0 y un `Spinner` 16/2 `accent.onTonal` queda centrado en el botón. **El ancho no cambia**, porque se mide con el texto. El botón no responde y la ayuda accesible dice `exportBusy` | **Antiparpadeo**: el spinner aparece solo si la exportación pasa los **250 ms**. Si aparece, dura al menos **400 ms**. Texto 1 → 0 en 80 `ease.exit` y spinner 0 → 1 en 120 `ease.enter` (desde los 40) |
| Éxito (Windows) | El botón vuelve a reposo y sale el aviso de §3 | Texto 0 → 1 en 160 `ease.enter`. El aviso entra con la transición común del `ToastLayer` (sube 16 y funde en 220) y el check se traza en 0 a 240 |
| Éxito (Android, hoja) | El botón vuelve a reposo cuando abre la hoja | Lo que se ve es la animación del sistema. RowMate no anima nada encima |
| Error | El botón vuelve a reposo y sale el aviso de §4 | Las mismas transiciones del `ToastLayer` |
| Reducir movimiento | Igual | Sin escala al presionar (la regla vigente). El spinner sigue, porque indica espera. Aviso: fundido de 160 sin subir. Check entero con fundido de 160 |

Todo esto va **por código** con el reloj del `MotionDriver` (regla 2 de `Motion.uss`). No hay ninguna transición de USS nueva: el `--tonal` ya toma las reglas de `.rm-button`.

## 3. Aviso de éxito con acción (Windows)

```
┌───────────────────────────────────────────────────────────────┐
│ (✓)  TCX guardado                                Abrir carpeta │
│      Documentos\RowMate\Exportaciones                          │
└───────────────────────────────────────────────────────────────┘
```

| Pieza | Spec |
|---|---|
| Cápsula | `.rm-toast--link`: `glass.fallback`, radio 22, misma posición y ancho que el `ToastLayer` (TV de 16 a 16; desde 600 dp, `clamp(contenido + 32; 344; 560)` centrado) |
| Indicador | `LinkIndicator` en modo `Check`, 24, `state.success`, con 16 de aire hasta el texto (igual que en la 10a) |
| Título | `exportSaved` `titleXs` 14/600 `text.hudPrimary`, en una línea |
| Ruta | A 2 del título, en `caption` 11/500 `text.secondary` y una línea. Sobre el vidrio da ≥ 9:1 |
| Ruta abreviada | Si la carpeta está dentro de Documentos: `<Documentos localizado>\RowMate\Exportaciones`. Si se redirigió (`ROWMATE_DATA_DIR`, override de pruebas): `…\` + los **2 últimos segmentos**. Si no entra, se recorta **en el medio** y se conservan el primer y el último segmento. Va sin el nombre del archivo, porque el archivo ya queda seleccionado al abrir la carpeta |
| Acción | `.rm-button--text` `.rm-type-button-s` `exportOpenFolder` en `accent.onTonal` (≥ 8:1 sobre el vidrio), `min-height` 36 y 8 de aire a la izquierda. Toma la presión y la capa de `.rm-button`. Con teclado entra en el orden de foco y se activa con Enter |
| Duración | **6 s** (4 s del aviso común + 2 por la acción). El reloj se pausa con el puntero encima o con el foco en la acción, y sigue al salir |
| Tocar la acción | Abre el Explorador con el archivo seleccionado (A12-05) y el aviso sale en el acto (salida común, 160) |
| Convivencia | Es un aviso común con acción: lo reemplaza un aviso del remo o del pulsómetro, como manda DV-14 y D11-12 |
| TH / W1280 | La misma cápsula centrada. En TV, si título + acción no entran, la ruta toma el ancho que queda y se recorta en el medio. La acción no baja de renglón |

## 4. Aviso de error

Usa la misma cápsula con acción, **sin** indicador. El título es `exportFailed` en `titleXs` `text.hudPrimary` y no lleva ruta. La acción es `exportRetry`, que vuelve a disparar la exportación (el botón pasa a "Exportando…") y dura 6 s. Va sin color de peligro (D12-07). Si el motivo se conoce (falta espacio o falta permiso), el PO decide el texto en un segundo renglón `caption` `text.secondary`.

## 5. Textos propuestos (para el PO; el arte no inventa claves)

| Clave | es | en |
|---|---|---|
| `exportTcx` | Exportar TCX | Export TCX |
| `exportBusy` (ayuda) | Exportando… | Exporting… |
| `exportSaved` | TCX guardado | TCX saved |
| `exportOpenFolder` | Abrir carpeta | Open folder |
| `exportFailed` | No se pudo exportar la sesión | Couldn't export the session |
| `exportRetry` | Reintentar | Retry |
| `folderDocuments` | Documentos | Documents |

## 6. Presupuesto y tomas

- **Elementos**: el botón suma unos 5 (botón, capa, texto, spinner diferido y foco). El aviso con acción suma unos 5, creados la primera vez y después reusados. **0 B por cuadro** con el spinner y el check a la vista (FrameAllocations; el marcador `RowMate.Toast.LinkNotice` ya existe).
- **Tomas del set `it12`** (exportador falso, `ROWMATE_DATA_DIR` temporal, reloj de prueba, ruta de ejemplo genérica): detalle TV en reposo, TV exportando, TV con el aviso de éxito, TH y W1280 con el aviso de éxito, TV con el aviso de error, TV a 1,3× (botón en su propio renglón) y TV con reducir movimiento. Ninguna toma abre el Explorador ni escribe en Documentos.

## 7. Pedidos al Architect

| ID | Pedido |
|---|---|
| **A12-01** | En `SessionDetailPage`, una cabecera `#session-summary-header` con el título y `#session-export` (`RmButton` `--tonal`, sin ícono y con `.rm-type-button-s`) según §1, más la regla de 1,3× (pasa a su propio renglón y nunca muestra "…") |
| **A12-02** | Área táctil de 48 dp en un botón de 36 dentro de la tarjeta, sin mover el layout (márgenes negativos o una capa de toque hermana) |
| **A12-03** | Estado ocupado **con el ancho fijo** (spinner centrado, texto en opacidad 0) y el antiparpadeo de 250 / 400 ms con el reloj inyectable. Las constantes van en Core (`ExportMotionSpecs`, como `BleMotionSpecs`) con `dotnet test` |
| **A12-04** | `ToastLayer.ShowAction(título, detalle?, indicador?, textoAcción, alTocar)`: la cápsula `.rm-toast--link` con un segundo renglón y un `.rm-button--text`, 6 s, pausa con el puntero o el foco, y salida en el acto al tocar la acción. Elementos reusados, sin asignaciones por cuadro |
| **A12-05** | "Abrir carpeta" detrás de una interfaz (`IFolderRevealer`): en Windows, Explorador con el archivo seleccionado; en pruebas y tomas, una implementación falsa que **solo registra** la llamada |
| **A12-06** | La abreviatura de la ruta de §3 como función pura en Core (Documentos localizado, `…\` + 2 segmentos y recorte en el medio por ancho medido), con pruebas que garanticen que nunca sale la unidad ni el nombre del usuario |
| **A12-07** | Android: el botón vuelve a reposo al abrir la hoja y no muestra aviso de éxito. El error usa §4 |
| **A12-08** | El set `it12` de `capture-snapshots.ps1` con las tomas de §6, escrito en `12-exportar-tcx/snapshots` |

## 8. Para issues

- Sumar el glifo `ios_share` (`E6B8`) al recorte (ASSET_SLOTS §4.1, necesita red) y ponerlo de 16 en el botón (D12-03).
- Acción "Exportar TCX" en el Historial (menú ⋮ o pulsación larga en la tarjeta), con el mismo aviso.
- Exportar varias sesiones de una vez.
- Variante Android de guardar en Descargas (MediaStore): el aviso de §3 sin acción y con `Descargas\RowMate`.
- Un sello sutil de "exportada" en la tarjeta del Historial, como reemplazo EXR del ícono de sincronizado de Strava.
- Los glifos `cloud_upload`, `cloud_download`, `cloud_done`, `person`, `link` y `link_off` (este último lo dibuja el `LinkIndicator`) quedan sin uso en la UI tras descartar Strava. Revisar si se quitan del recorte.
