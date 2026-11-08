# It. 9 — Notas de diseño (WP-D)

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-08 |
| Iteración | 9 · `09-historial` |
| Entrada | [00-arte-historial](00-arte-historial.md) (base de arte), [02-tech-design](02-tech-design.md) §1.3, §5.6, §10.2 y §13 (estado de WP-B), [01-po-spec](01-po-spec.md), las tomas de [snapshots/](snapshots/) (24, de WP-B2) |
| Archivos | `Presentation/Styles/Skin.uss` (C), `Presentation/Styles/Motion.uss` (C), `Presentation/Motion/History/HistoryMotion.cs` (N) y dos llamadas en `Presentation/History/{HistoryListView,SessionDetailPage}.cs` (la extracción que WP-B dejó anotada en §13) |
| Modo | Entrega rápida: manda la spec sobre el arte (§10.2 del diseño); lo que no entra acá va a "Para issues" (§5) |

Rutas relativas a `Assets/RowMate/`.

---

## 1. Qué se revisó en las tomas

Se miraron CAP-01, -03, -06, -07, -09, -14, -15 y -17. Con las decisiones de §10.2 del diseño (colores base, fila única, una columna y 220 de alto), el resultado ya se lee como una bitácora EXR:

- **Lista (CAP-01, -03)**: las cifras van en RowMate Numeric SC con el color de su métrica y las etiquetas son discretas. La tarjeta mide 104 a 1,0× y respeta el margen de 12 y los 10 entre tarjetas. A 1,3× en P360 las cifras se achican, pero no se cortan.
- **Detalle (CAP-07, -09)**: el resumen, las filas de paso y el selector con su tilde van en la jerarquía de la spec. El gráfico tiene franjas al 10 %, la tira de tipo de 3 dp, los separadores [4,4], los objetivos [8,5] al 78 %, la grilla al 8 % y el degradé del área 24 → 0 %. El split va invertido, con las paradas al piso.
- **Cursor (CAP-15)**: la guía va al 40 %, el punto de 8 lleva el anillo `bg.sunken` y el tooltip va sobre `glass.fallback` con la cifra `metric.*.hud` y la unidad en `text.secondary`.
- **Entrada y cambio de métrica**: los tiempos de `ChartSpecs` coinciden con §5.1 y §5.5 del arte (marco 0–200, revelado 100–700, área 150–450; tooltip 90 de entrada y 160 de salida). El cambio de métrica es el fundido cruzado de 220 con el reescalado de 300 (DT9-11).

No hubo que cambiar ninguna medida ni ningún color de contrato. Los ajustes son los de §2 y §3, y no se ven en las tomas: el arnés toca con `NavigationSubmitEvent`, sin puntero ni anillo de foco. **WP-B3 no necesita volver a capturar.**

## 2. Skin.uss

| ID | Ajuste | Motivo |
|---|---|---|
| DN9-01 | Tarjeta del historial: capa de estado al 4 % con *hover* (solo escritorio) y al 8 % al presionar (`:hover:active`), sin escala | Arte §1.2. Es la regla de la tarjeta de rutina. WP-B había dejado la capa sin reglas, así que en escritorio la tarjeta no respondía al puntero (AC-HIS-07-07) |
| DN9-02 | Selector de métrica: el anillo de foco de las opciones del medio va con las 4 esquinas de 2 dp, y solo la última (`#session-metric-hr`) se redondea a la derecha (17 = 19 − 2) | Las reglas de la It. 3 suponen 2 opciones: con 5, cada opción con `--divided` se redondeaba a la derecha y el anillo de SPM, Split o Distancia se salía de la forma de la opción |

El anillo blanco de la opción elegida y la capa de estado del selector (4 % y 8 %) ya los daban las reglas de `.rm-segmented` de la It. 3.

## 3. Motion.uss y HistoryMotion

- **Motion.uss**: la capa de la tarjeta va hacia el estado en `dur.micro` con `ease.enter` y vuelve en `dur.fast` con `ease.exit` (la de `.rm-button__state`). No se agregó ninguna transición más. La entrada del gráfico, el cambio de métrica, el cursor, el tooltip, el cambio de opción elegida, la tarjeta nueva y la cascada van por código, con el reloj del `MotionDriver`. Así las tomas siguen siendo deterministas.
- **`Presentation/Motion/History/HistoryMotion.cs`** (namespace `RowMate.Presentation.Motion.History`) junta la coreografía que WP-B tenía dentro de las vistas, con los mismos números y la misma conducta:
  - `FirstLoadCount` y `PlayFirstLoadCard`: la primera carga escalona 35 ms hasta 8 tarjetas, cada una en 240 `ease.enter` con y +12 → 0.
  - `PlayTopInsert(GrowMotion)`: la tarjeta nueva crece en su slot con `spring.smooth` ω 12 y funde de 60 a 260.
  - `PlayDetailCascade`: con los datos después del *push*, las secciones entran cada 35 ms. Si llegaron durante el *push*, no hay cascada.
  - `PlayFirstLoad`: la variante de lista completa del contrato.
  - Con reducir movimiento rige lo de `EnterMotion` y `GrowMotion`: un fundido de 160 junto, sin desplazamiento.
- **Cambio del contrato de §5.6** (una línea): las funciones reciben los movimientos que son de la vista (`EnterMotion`, `GrowMotion`) en lugar del `MotionDriver` solo. La tarjeta nueva crece en su slot (`GrowMotion`, arte §1.4) en lugar de correr el contenido desde −k·alto. Es lo que WP-B ya hacía, y lo cubren `HistoryReloadTests`.

## 4. Verificación

- dotnet (`run-tests.ps1`): 3 500 pruebas, 3 461 pasan, 39 omitidas y 0 fallan.
- Unity: compila limpio. Sin `-Simulator`, EditMode da 3 603 (3 556 pasan, 47 ignoradas, 0 fallan) y PlayMode 588 de 588. Con `-Simulator`, EditMode da 3 795 (3 748 pasan, 47 ignoradas, 0 fallan) y PlayMode 663 de 663. La guardia de `ProjectSettings` no registró cambios.

## 5. Para issues

- **Etiquetas X encimadas**: `3:44` y `5:00` en CAP-06, `10:00` y `11:00` en CAP-09. La regla de §4 del arte (no dibujar la marca intermedia que choca) quedó afuera por paridad (AC-HIS-13-05). Es lo que más se nota en las tomas.
- **Fila de números a 1,3×** (CAP-03): el valor y la etiqueta se achican por separado. `Distance` queda mucho más chica que `Time`, y las cifras de dos columnas vecinas quedan a 2 dp. Habría que achicar la columna entera, con un aire mínimo de 4.
- **Sonda de aspecto de CAP-01** (`HistoryCatalog`): busca `history-card-0-stat-2 .rm-stat__value` como nombre de elemento y lee `#000000`. Los colores de la toma son los correctos. Hay que darle nombre al `Label` del valor o hacer que la sonda acepte un selector.
- **Error del historial sin ícono** (CAP-17): el estado de error de Rutinas tiene la misma forma. Se podría sumar un ícono `cloud_off` o `error` 56 @12 % como el vacío.
- **Tooltip sin sombra**: UI Toolkit no tiene `box-shadow`. Una sombra de malla (9-slice) lo separaría de la curva.
- Lo que ya está en la lista de §12 del diseño y en §10 del arte: el contraste del pulso base, la unidad más chica, el resumen con jerarquía, la fila de paso teñida, el conteo, las dos columnas, los huecos del split, la transformación entre métricas y el pico de la cúbica en cada escalón.
