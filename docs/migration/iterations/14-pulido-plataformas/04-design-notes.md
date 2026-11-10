# It. 14 — Notas de diseño implementadas (WP-D)

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-10 |
| Entrada | [00-arte-pulido](00-arte-pulido.md), [01-po-spec](01-po-spec.md) (AC-POL-01, -02, -04, AC-APP-15-10, AC-DEBT-05 a -08), [02-tech-design](02-tech-design.md) §3, §6.1, §6.2, §7 y §1.3 (con "Cambios por gate" y B-01 a B-07 de WP-B), las capturas `it14` de WP-B2 en [snapshots/](snapshots/) |
| Modo | Entrega rápida. Manda la spec sobre el arte; lo menor va a "Para issues" (§7) |

Cero tokens, fuentes, glifos ni colores nuevos (`ColorSourceTests` sigue en verde). Todo sale de `Tokens.uss`, `Typography.uss` (generado: no se toca) y de las piezas de siempre.

---

## 1. Qué quedó implementado

| ID | Pieza | Dónde | Números |
|---|---|---|---|
| DD14-01 | Tracking según la caja (A14-08, V14-06) | `Skin.uss` (`.rm-type-label.rm-type-label--mixed`), `Shell/TabBarView.cs` | Las pestañas pasan de 0,08 em (8 en la unidad de TextCore) a **0,04 em (4)**: `Dispositivo` deja de ocupar 71 de sus 97 dp en TV. El HUD queda en 0,08 (QA-G14-17: no se corrieron las sondas de la 4b; va a issues) |
| DD14-02 | TY-01: Rendimiento por paso | `History/StepBreakdownView.cs`, `Skin.uss` | Valores en `num.xs` (RowMate Numeric SC 13/700, cifras tabulares de la fuente) y **3 columnas de ancho fijo por lista** (el máximo de cada columna en todas las filas, `MeasureTextSize`, alineadas a la derecha): de una fila a otra la `W`, `spm` y `/500m` caen en el mismo lugar. La línea 1 (`1:00 · 183m`) en RowMate Numeric SC 600 al tamaño de `caption` (11), `text.secondary`. Recalcula con un dato nuevo o un cambio de letra (`GeometryChangedEvent`), nunca por cuadro; 0 elementos nuevos |
| DD14-03 | TY-02: fecha de las tarjetas | `Skin.uss` (`.rm-history-card__top > .rm-history-card__date`) | RowMate Numeric SC 600 al tamaño de `bodyXs` (12), `text.tertiary`. **Desvío del arte** (TY-02 pedía `TabularCells` con Inter): la fuente tabular del proyecto da lo mismo sin elementos ni dibujo propio, conserva el `Label` del contrato (`DateLabel`) y queda en la familia de los valores de la fila de abajo. AC-POL-02 acepta "RowMate Numeric SC o `TabularNumber`" |
| DD14-04 | #36: columna entera | `History/StatRowView.cs`, `Skin.uss` | Un solo `FitBox` por columna con `.rm-stat__stack` (valor + etiqueta, 2 dp entre ellos): si no entra, se achica **todo junto**; aire de **4 dp** entre columnas (`.rm-stat` 2 + 2). Vale para la tarjeta y el Resumen del Detalle |
| DD14-05 | #32 (vista): ejes sin etiquetas encimadas | `History/ChartAxisLabels.cs` | `AxisTicks.DropCrowded` sobre las posiciones finales (las X de los bordes ya empujadas adentro del marco; las Y con su caja de 14): la intermedia a menos de 4 dp de un borde pasa a `display: none`. `XTexts`/`YTexts` no cambian (son las marcas del eje); `XDropped`/`YDropped` dicen cuáles se ocultaron. Sin asignar por cuadro |
| DD14-06 | #37: extremos redondos | `Core/History/ChartGeometry.cs` (`WriteSeriesCaps`) | Un disco de 2 dp (el ancho de la línea, 16 segmentos con su borde de 1 px) en el primer punto y en el último visible (sigue el corte del revelado). Aparte de `WriteSeriesLine`, que conserva sus 4 vértices por punto; el tope del cuadro suma 2 × 33 vértices |
| DD14-07 | #37: tira de tipo | `Core/History/ChartGeometry.cs` (`WriteStrip`, `Stadium`) | Cada tramo es una cápsula de **radio 1,5 dp** (medio alto de la tira de 3), en abanico con 2 × 9 puntos de contorno y borde suavizado de 1 px (37 vértices). Mismo rectángulo exterior (y 12 a 15, 2 dp de hueco) |
| DD14-08 | #37: ícono del error | `History/HistoryScreenView.cs`, `Skin.uss` | El error del Historial lleva el mismo `history` de **56 dp al 12 %** que el vacío, 12 dp encima del texto (CAP-25) |
| DD14-09 | T-07: la regla de los 400 ms | `Motion/Hud/HudReflowMotion.cs` | `HudReflowMotion` **es** la `LayoutClassEntrance` de 02-tech-design §3 (existe desde la 6a, DT6a-06: 200 ms `ease.enter`, opacidad, en el layout nuevo). Se le suma la regla: otro cambio de clase antes de 400 ms del anterior → en el acto (arrastrar el borde de un lado al otro del cuadrado no repite la entrada). Reducir movimiento: en el acto (02-tech-design §3; el fundido de 120 del arte queda descartado). El +8 dp de traslado **no** se agrega: lo escribiría `ImmersiveWorkoutPage.ApplyReflow` sobre el `translate` de las piezas, que es de la cascada y de `SeriesColumn` (riesgo de pisarse sin AC que lo pida) |
| DD14-10 | T-07: ancla de desplazamiento | `Controls/ScrollAnchor.cs`, `Routines/RoutineListView.cs` | Captura el primer hijo visible y su distancia al borde con cada desplazamiento; con un cambio de **ancho** del contenido la repone (recortada al rango) en los `GeometryChangedEvent` siguientes, hasta que el usuario toca la lista. Sin elementos ni estilos. En Rutinas (02-tech-design §1.3). El Historial ya tiene su ancla de recarga (`HistoryListView.FixAnchor`): no se le suma otra para no pelearse |
| DD14-11 | Arranque y su indicador | `Skin.uss` | `.rm-boot-splash__mark` con `scale-to-fit` (la marca de 384 px nunca se estira) y `.rm-boot-splash__spinner` en `accent.primary`. El fondo y el centrado van en línea en `BootSplashView` (WP-B1) |
| DD14-12 | Lo que WP-B1 ya pintó en línea | — | #41 (`.rm-scan-button--busy`, B-05: fondo `accent.primary` al 38 % sobre `bg.app`, etiqueta blanca al 70 %, indicador blanco) y #21 (`.rm-scene-loading`, B-03: `text.secondary`, arco de 32 dp) quedan en C#: revisados en CAP-16 y en el código, cumplen la spec; no se duplican en USS. El aviso con acción (#69) ya usa `glass.fallback` desde la 12 (`.rm-toast--link`): §3 |

## 2. Inventario de transiciones (AC-POL-01)

Auditado contra ANIMATION_BIBLE §2.1, §2.2, §2.11 a §2.13 sin cambios de código: los tiempos ya eran los del token.

| ID | Pieza | Spec | Estado |
|---|---|---|---|
| T-01 | `BootSplashMotion` (WP-A/B1) | 200 `ease.exit`; 120 lineal | como la spec (A14-04: sin la cascada de 420) |
| T-02 | `TabTransition` | saliente 0-120 `ease.exit` −12; entrante 80-280 `ease.enter` +12; reducido: solo opacidad | ok; `TransitionInventoryTests` lo muestrea cuadro a cuadro |
| T-03 | `PageTransition` | push 240 / pop 200; reducido 120 | ok; muestreado, sin cuadro sin estilos; CAP-24 = cuadro a los 120 |
| T-04 | `DialogTransition`, `ToastTransition`, menús | §2.2 | ok (pruebas de la 3 y la 12) |
| T-05 | Inmersivo (`ImmersiveCascadeMotion`, salida, completado) | §2.11 | ok (4b, 6a) |
| T-06 | `BleMotion` | §2.12, §2.13 | ok (10a, 11) |
| T-07 | `HudReflowMotion` + `ScrollAnchor` | §3.3 (nueva) | DD14-09, DD14-10 |
| T-08 | idioma, letra | en el acto | ok |

## 3. Contrastes (AC-DEBT-05, #69)

El aviso con acción sigue en `glass.fallback` (#070D16 al 86 %), como pide la spec. Compuesto sobre la línea más clara del gráfico (`metric.watts` #FFD60A, el peor caso) da ≈ (42; 41; 20): `text.hudPrimary` ≈ 14,7:1; el detalle (`text.secondary`) y la acción (`accent.onTonal`) ya medían 8,2:1 y 8,0:1 en el peor fondo de la 12 (el cian de la pestaña): todos ≥ 4,5:1. La línea se adivina detrás (14 % de un amarillo saturado): es el vidrio de la biblia (§3.10), no un problema de lectura. Si el usuario lo quiere opaco, es un cambio de spec (para issues).

## 4. Capturas `it14` revisadas

Se revisaron las 14 tomas de WP-B2 (CAP-03 a -05, -07, -12, -15 a -20, -22, -23, -27) antes de WP-D, y se volvieron a tomar después (§5).

| Toma | Antes de WP-D | Después |
|---|---|---|
| CAP-03, -04 | arranque `#0D1B2A` con la marca centrada, sin texto: ok | igual |
| CAP-05, -07 | Dispositivo: tablero tabular; pestañas con el tracking de mayúsculas | pestañas con 0,04 em |
| CAP-12 | fechas proporcionales (`05/11/2026  18:40` bailaba entre tarjetas); pestañas abiertas | fechas tabulares; pestañas |
| CAP-15 a -18 | #40, #41, #42, #44 (WP-B1): ok | igual |
| CAP-19, -20 | el aviso de vidrio sobre el gráfico: legible (§3) | igual |
| CAP-22, -23 | filas por paso con las columnas corridas ≈ 10 dp; tira de tipo de esquinas vivas; en CAP-23 los ejes no se ven (la toma es de arriba) | columnas fijas, cifras tabulares; tira redondeada |
| CAP-27 | volcado del lector: ok | igual |

**Capturas de iteraciones anteriores afectadas y no regeneradas** (a propósito, como pide la tarea): las pestañas (todas las tomas con barra de pestañas de la 2, 3, 9, 10a, 11 y 12), las tarjetas del Historial y el Detalle (9 y 12: fecha, columnas del Resumen y de la tarjeta, filas por paso, tira de tipo, extremos de la línea, ejes con marcas ocultas) y el error del Historial (9, CAP-17). No cambia nada del HUD ni de la escena.

## 5. Verificación

Corridas de WP-D (2026-11-10, en la PC):

| Corrida | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1` | verde: 4 015 pruebas, 3 976 pasan, 39 ignoradas (más 194 del otro proyecto, todas pasan) |
| Unity EditMode | 4 139: 4 085 pasan, 54 ignoradas, 0 fallan; compilación sin errores ni advertencias nuevas |
| Unity EditMode `-Simulator` | 4 333: 4 279 pasan, 54 ignoradas, 0 fallan |
| Unity PlayMode | 682: 682 pasan |
| Unity PlayMode `-Simulator` | 757: 757 pasan |
| `capture-snapshots.ps1 -Set it14` | 14 tomas, 0 con problemas, 0 sondas que fallan; escribe solo en la carpeta de la 14; guardia de `ProjectSettings` sin cambios |

Pruebas nuevas o cambiadas: `Tests/PlayMode/It14/It14DesignTests.cs` (`TransitionInventoryTests`, `TypographyAuditTests` = TabularAudit + FontAudit, `HistoryScaleTests`, `ChartArtTests`, `CompletionDialogScaleTests`, `ScrollAnchorTests`), `ChartGeometryTests.BandsAndStrip` (la cápsula) y `.RoundCapsAtBothEnds` (nueva), y `SceneMotionTests.AnotherClassChangeWithinFourHundredIsAtOnce` (reemplaza `AnotherClassChangeInTheMiddleStartsFromWhereItIs` de la 6a por la regla de los 400 ms; ningún conteo baja).

## 6. Desvíos frente al arte y al diseño

| ID | Desvío | Por qué |
|---|---|---|
| WD-01 | TY-02 con RowMate Numeric SC 600 en lugar de `TabularCells` con Inter | DD14-03 |
| WD-02 | `LayoutClassEntrance` = `HudReflowMotion` + la regla de los 400 ms, sin el +8 dp | DD14-09 |
| WD-03 | `SplitRange.Widen` (#32, rango mínimo de 10 s del split) **no** está cableado: cambia el sistema de coordenadas del split de `ChartSeries` (DT9-08, Core, lógica de WP-A) y las pruebas de la 9 que lo fijan | para issues; WP-D no toca lógica |
| WD-04 | El tracking de 0,04 em solo en las pestañas | QA-G14-17: el HUD necesita las sondas de la 4b |
| WD-05 | El ancla de desplazamiento solo en Rutinas | DD14-10 |

## 7. Para issues

- Tracking de 0,04 em en los rótulos del HUD (`Split /500m`, `Watts`, `kcal`) con las sondas de la 4b (V14-07).
- `SplitRange.Widen` cableado en `ChartSeries` (WD-03).
- +8 dp de traslado en la entrada del HUD con cambio de clase y la cascada corta del arte (A14-05).
- Ancla de desplazamiento en Historial, Detalle y Dispositivo (hoy solo Rutinas).
- CAP-22/-23: tomar también el gráfico entero (desplazado al final) para que la toma muestre el `0`, el eje X y los extremos redondos.
- Aviso con acción opaco si el usuario lo prefiere al vidrio (§3).
- Lo de 00-arte-pulido §6 que sigue vigente: animación de marca en el arranque, recordar tamaño y posición de la ventana, cinta `SIMULADOR` sobre el título, "RowMate Numeric" de Inter cuando haya Python, parche de la estela, `.ico` versionado.
