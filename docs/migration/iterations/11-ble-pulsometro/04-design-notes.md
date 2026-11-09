# It. 11 — Notas de diseño implementadas (WP-D)

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-08 |
| Entrada | [00-arte-pulsometro](00-arte-pulsometro.md), [02-tech-design](02-tech-design.md) §1.3, §4, §8 y §9 (con "Cambios por gate"), [01-po-spec](01-po-spec.md), las capturas de WP-B1 en [snapshots/](snapshots/) |
| Modo | Entrega rápida: lo que el diseño rechazó o la spec no pide va a "Para issues" (§6) |

Cero colores, fuentes, glifos de fuente ni tokens nuevos. Todo sale de `Tokens.uss`, `Typography.uss` y `BleMotionSpecs`.

---

## 1. Qué quedó implementado

| ID | Pieza | Dónde | Números |
|---|---|---|---|
| DD11-01 | `LinkIndicator.Mode.Heart` y `.HeartBroken` (A11-04) | `Presentation/Controls/LinkIndicator.cs`, `Core/Design/HeartGlyph.cs` | El `favorite` de Material (caja 24) dibujado con `Painter2D` en el mismo elemento: sin fuente ni elementos nuevos. Roto: dos mitades separadas 0,9 por lado y cerradas por una grieta en zigzag de la muesca a la punta. `Heart` late **una vez** al entrar (escala 1 → 1,16 en 0-90 `ease.enter`, → 1 en 90-290 `ease.move`) por `style.scale`, sin repintar. Reducir movimiento: sin latido |
| DD11-02 | Avisos del pulsómetro con su glifo | `ToastLayer.ModeFor(HrmNoticeKind)` | `Connected` → `Heart` en `metric.hr.hud` (4,57:1); `Lost` → `HeartBroken` en `state.warning` (6,24:1). Reemplaza el `Check`/`LinkOff` provisorio de WP-B1 |
| DD11-03 | Respiración del corazón del encabezado (D11-05) | `HrmMotion.Breath` | Opacidad 0,40 ↔ 0,70, período 3 200 ms (`ease.pulse`), desde los 160 ms del cambio (después del fundido 1 → 0,40 de `HeartbeatAnimator`) y arrancando en 0,40. Solo con la tarjeta a la vista. Al dejar de respirar vuelve a su reposo en 160 (`ease.move`); si conecta, el fundido a 1 de `HeartbeatAnimator` sale desde la opacidad del momento. Reducir movimiento: 0,40 fijo. Continuo, 0 B por cuadro |
| DD11-04 | Línea "Sin señal del sensor" (§2.2) | `HrmMotion.Line` | Entra: alto y margen 0 → natural en 0-160 (`ease.move`), opacidad 60-260 (`ease.enter`). Sale: opacidad 0-120 (`ease.exit`), alto 80-240 (`ease.move`); el texto se conserva mientras sale. Reducir movimiento: fundido 160 / 120 sin cambio de alto. Al armar el cuerpo conectado, en el acto |
| DD11-05 | Filas de sensores con clave estable (§1.3) | `HeartRateCardView.AppendRows` + `HrmMotion.Row` | Solo entran las filas **nuevas** al final de la lista: alto 0 → 56 (o el de una fila ya medida) y margen 0 → 4 en 0-160; opacidad e y −8 → 0 en 60-260; escalonadas 35 ms en un mismo lote, 8 como máximo. Las que ya estaban no se reaniman ni cambian de lugar (antes, cada anuncio rearmaba el cuerpo entero con su fundido). Reducir movimiento: fundido de 160 |
| DD11-06 | Check y retención de 240 ms (A10-05) | `HeartRateCardView.OnHoldDone` | El cambio de cuerpo y el fundido del corazón arrancan en el **instante exacto** del fin de la retención (`MotionDriver.StartingAt` con el `OverrunMs`) y se ponen al día en ese mismo cuadro. Con un cuadro largo (o el salto del reloj de prueba de CAP-12a) el cuerpo ya no queda vacío a mitad del cruce: la CAP-12a de WP-B1 mostraba la tarjeta sin contenido; regenerada, muestra "Banda de prueba · -- bpm" |
| DD11-07 | Seguir a la tarjeta (L640) | `HrmMotion.Follow`, `ScanView.FollowCard` | Con `SingleScroll`, si la tarjeta crece y su borde inferior sale de la vista, desplaza lo justo en 240 (`ease.move`), nunca hacia arriba ni más allá de su borde superior; si sigue creciendo, retoma desde donde está. No actúa mientras el usuario toca. Reducir movimiento: en el acto |
| DD11-08 | Aspecto (USS) | `Styles/Skin.uss` §It. 11, `Styles/Motion.uss` | Estado del recordado (`· reconectando...` / `· no encontrado`) en `text.tertiary` (5,15:1): el nombre manda. `link_off` del recordado en `text.tertiary` (hecho, no alarma); arco y spinner en `accent.primary`. `__stale` en `text.tertiary` (manda AC-HRM-05-03: sin punto naranja). `__error-human` en `state.dangerText`; `__error-detail` en `text.tertiary`, una línea con `…`. Aire inferior de 12 (L640) y 8 (TH) dentro del slot. `Motion.uss`: ninguna regla nueva (todo por código, regla 2) |
| DD11-09 | Números | `Core/Design/BleMotionSpecs.cs` (sección "Pulsómetro real") | `HrmBreath*`, `HrmGrowEndMs`/`HrmFade*`/`HrmOut*`, `HrmRow*`, `HrmStale*`, `HrmFollowMs`, `HeartBeat*`, y las funciones puras `HrmBreathOpacity`, `HeartBeatScaleAt`, `HrmRowDelayMs`. Tests dotnet: `Tests/EditMode/Design/HrmMotionSpecsTests.cs` |

## 2. Estados de la tarjeta, cómo se ven (TV, 347 dp de ancho interior)

| # | Estado | Corazón | Indicador | Lo nuevo de WP-D |
|---|---|---|---|---|
| 1 | Sin sensor | 0,40 quieto | — | — |
| 2-3 | Buscando / lista | 0,40 quieto | spinner 16 | filas que entran de a una (DD11-05) |
| 4 | Conectando | 0,40 | spinner → check 240 | el cuerpo cambia al terminar el check (DD11-06) |
| 5 | Conectado | 1, late a 60/bpm | — | — |
| 6 | Lectura vencida | 0,40 quieto (manda la spec) | — | línea "Sin señal del sensor" que crece y se va (DD11-04) |
| 7 | Recordado, no encontrado | 0,40 quieto | `link_off` `text.tertiary` | estado en `text.tertiary` |
| 8 | Reconectando | **respira** 0,40 ↔ 0,70 | arco de la espera (10 s / 3 s) y spinner en el intento | DD11-03 |
| — | Error | — | — | dos renglones: humano en `state.dangerText`, detalle de una línea en `text.tertiary` |

## 3. Reducir movimiento y capturas fijas

Todo lo **finito** (línea sin señal, filas, check, latido del aviso, seguir a la tarjeta) termina en el mismo estado fijo con y sin reducir movimiento: una toma asentada sale igual. Lo **continuo** (respiración, arco, spinner) es la excepción declarada: con reducir movimiento la respiración queda en 0,40 y el arco y el spinner siguen (indican espera, §7 del arte). Por eso CAP-07 (t = 5 s, respirando) no es comparable entre los dos modos; CAP-08 sí (sin reintentos no respira).

## 4. Presupuesto

| Qué | Resultado |
|---|---|
| HUD | **+0** elementos: el aviso va en la cápsula del `ToastLayer`, fuera de `#immersive`; el glifo lo dibuja el `LinkIndicator` que ya estaba |
| Tarjeta | +0 sobre la estructura de WP-B1 (los glifos son malla del mismo elemento) |
| Basura por cuadro | Respiración, arco, latido y seguimiento escriben `StyleFloat`/`Scale`/`Vector2` (structs) y recorren arreglos estáticos: 0 B. La entrada de una fila crea su `HrmMotion.Row` **al descubrir** el sensor (una vez), no por cuadro. Medición con `BleAllocationTests` (marcadores `RowMate.Device.HrmCard` y `RowMate.Toast.LinkNotice`) |
| Repintado | `Heart` y `HeartBroken` se pintan una vez por cambio de modo; el latido es escala |

## 5. Desvíos sobre el arte (una línea cada uno)

- Fuente del pulso en el HUD (corazón relleno/contorno) y antiparpadeo de 1 200 ms: **no** (A11-06 rechazado por el Architect; a issues).
- Aviso de lectura vencida y "no repetir en 10 s": no (A11-07 parcial; a issues).
- Estado 6 con el corazón al 40 % y sin punto naranja: manda AC-HRM-05-03 sobre D11-04.
- Nombre y estado del recordado en una fila (`{nombre} · sufijo`), no en dos renglones: manda la spec (A11-02 adaptado).
- Las otras filas a 0,38 al tocar una: no se implementó; el cuerpo cambia a "Conectando" en el mismo cuadro del toque, así que no se vería.
- `Buscar dispositivos` queda fuera del desplazamiento único en L640 (estructura de WP-B1): la lista y la tarjeta desplazan debajo del botón. Se lee bien; no se tocó.

## 6. Para issues

- Mini-sonar en el corazón del encabezado mientras busca (arte §11).
- Fundido de 160 del nombre de una fila que se actualiza en su lugar (hoy cambia en el acto).
- Fuente del pulso en el HUD y antiparpadeo de 1 200 ms (A11-06).
- Aviso "El pulsómetro dejó de enviar el pulso" y regla de no repetir en 10 s (A11-07).
- Respiración en fase fija para que CAP-07 sea idéntica con y sin reducir movimiento (hoy solo lo son las tomas asentadas).
