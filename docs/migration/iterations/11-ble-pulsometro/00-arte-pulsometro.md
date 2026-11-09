# It. 11 — Base de arte: tarjeta del pulsómetro, fuente del pulso en el HUD y avisos breves

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-08 |
| Iteración | 11 · `11-ble-pulsometro` ([ROADMAP §It. 11](../../ROADMAP.md): mejoras EXR "avisos breves y discretos" y "estados en lenguaje humano", Q-42) |
| Entrada | [10a/00-arte-bluetooth](../10a-ble-android-windows/00-arte-bluetooth.md) (sonar, `LinkIndicator`, avisos, alto compacto) y [10a/04-design-notes](../10a-ble-android-windows/04-design-notes.md) (DD10-01 a -08: lo que quedó implementado); [02/04-design-notes §4.6 y §5.3](../02-dispositivo-simulador/04-design-notes.md) (tarjeta y latido); [inventario HRM](../../product/inventory/heart-rate.md) (F-HRM-05, -08, -13, -14); `Presentation/Device/{HeartRateCardView,ScanView}.cs`, `Presentation/Workout/{HudView,HudCard}.cs`, `Presentation/Shell/ToastLayer.cs`, `Styles/Tokens.uss`; la toma [CAP-07b de la 10a](../10a-ble-android-windows/snapshots/CAP-07b-lista-pulsometro-th.png) (m-06) |
| Legacy leído (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/device/heart_rate_card.dart` |
| Modo | Entrega rápida: cada duda se resolvió con el legacy, la 10a o un valor por defecto, en una línea (§0.2). Lo menor va a "Para issues" (§11) |

**Cero colores, fuentes, glifos y tokens nuevos.** Todo sale de `Tokens.uss`, `Typography.uss`, `MotionSpecs`/`BleMotionSpecs` y de los glifos que ya existen (`favorite` relleno y contorno, `heart_broken`, `link_off`, `refresh`, `bluetooth_searching`, `bluetooth_disabled`, `chevron_right`). Los textos son del PO: acá van **propuestas** marcadas (§6); si el PO no las toma, va el texto del legacy en el mismo lugar. Contrastes WCAG 2.x en sRGB con el alfa compuesto, calculados para este documento.

Ventanas: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720 (dp). **Alto compacto**: contenido de Dispositivo < 560 dp (`BleMotionSpecs.CompactHeightDp`; TH y L640).

---

## 0. Resumen

### 0.1 Qué hay que lograr

1. **La tarjeta cuenta qué pasa en una frase humana**: nombre arriba, estado abajo con el mismo indicador del remo (spinner, arco de espera, check, `link_off`) (§1).
2. **El pulso se siente vivo y su ausencia se nota sin alarma**: el corazón late a la frecuencia real (It. 2) y, con la lectura vencida, se queda quieto y aparece "sin señal" en naranja, sin cortar nada (§1.2, estado 6; §2.2).
3. **En horizontal nada queda apretado** (m-06 de la 10a): con alto compacto, una sola columna con un único desplazamiento (L640) o dos columnas remo | pulsómetro (TH) (§3).
4. **En el HUD se sabe de dónde viene el pulso sin sumar elementos**: corazón relleno = pulsómetro, corazón contorno = monitor; y el cambio de fuente no hace parpadear la tarjeta (§4).
5. **Avisos breves y discretos** ("Pulsómetro conectado") en la cápsula del `ToastLayer` de la 10a, solo cuando la tarjeta no está a la vista, y nunca encima de un aviso del remo (§5).
6. **Presupuesto**: HUD sigue en **59** elementos (+0), 0 B por cuadro (§8).

### 0.2 Decisiones tomadas (una línea cada una)

- **D11-01** La línea de estado reutiliza el `LinkIndicator` de la 10a (1 elemento): espera = arco, intento = spinner, éxito = check, no encontrado = `link_off`. Mismo vocabulario que el remo.
- **D11-02** Nombre y estado van **en dos renglones** (`titleS` / `bodyS`), así el `…` corta el nombre y nunca el estado (lección A10-08). El "{nombre} · sufijo" del legacy queda como texto de respaldo.
- **D11-03** **Sin destello** del borde al conectar: lo prohíbe AC-HRM-13-02 ("esta tarjeta no destella"). La confirmación es el check (240) y el corazón que "despierta" con el primer pulso.
- **D11-04** Lectura vencida = conectado + watchdog disparado (F-HRM-05): número `--` (paridad), corazón **pleno y quieto** (AC-HRM-13-02: pleno con el sensor conectado, aun con `--`; manda sobre el "corazón en gris" de F-HRM-05) y un renglón "sin señal" en `state.warning` (6,41:1 sobre la tarjeta). Es aviso, no error: no se usa rojo.
- **D11-05** Reconectando: el corazón del encabezado **respira** (opacidad 0,40 ↔ 0,70, 3 200 ms, `ease.pulse`), como la ilustración del Bluetooth apagado. Espera serena, sin spinner gigante.
- **D11-06** Alto compacto (cambia AC-HRM-13-01, ver Pedido al PO §10): ancho de contenido ≥ 840 → **dos columnas**; si no, **un único `ScrollView`** con la lista a su alto natural. Con alto normal (TV, P360, W1280) queda como hoy (paridad).
- **D11-07** La vista conectada (remo conectado) no cambia: la tarjeta sigue debajo de la grilla y en W1280 queda bajo el pliegue (CAP-04 de la It. 2, paridad). Columna lateral → issue.
- **D11-08** Fuente en el HUD = **variante del glifo del corazón que ya existe** en la tarjeta BPM (`favorite` relleno / contorno). 0 elementos. El color no cambia (`metric.hr.hud`): el pulso es uno solo.
- **D11-09** Antiparpadeo: la tarjeta BPM del HUD se oculta recién después de **1 200 ms** sin pulso > 0; mientras tanto muestra `--`. Nunca se muestra un valor viejo.
- **D11-10** Los avisos del pulsómetro van en la cápsula `.rm-toast--link` del `ToastLayer` (fuera de `#immersive`, DD10-02), con dos glifos nuevos **dibujados** por el `LinkIndicator`: corazón y corazón roto.
- **D11-11** Un aviso del pulsómetro **solo** sale si la pestaña Dispositivo no está a la vista (HUD, Workout, Rutinas, Historial). En Dispositivo, la tarjeta ya lo dice.
- **D11-12** Prioridad: aviso del remo > aviso del pulsómetro > aviso común. Si hay uno del remo a la vista, el del pulsómetro **se descarta** (no se encola; queda en el log).
- **D11-13** Sin háptica nueva: la de `Listo` es del remo (D10-11). Vibrar al reconectar el pulsómetro va a issues.

---

## 1. Tarjeta del pulsómetro por estado (Dispositivo; TV, ancho interior 347 dp)

### 1.1 Anatomía común (cambios sobre la It. 2)

| Pieza | Spec |
|---|---|
| Caja, encabezado, borde y brillo | Sin cambios (It. 2 §4.6): `surface.card`, `r.card` 16, relleno 16, borde superior `metric.hr` @60 % fijo, `favorite` relleno 14 + 6 + `PULSÓMETRO` `label` en `metric.hr.hud` (4,68:1) |
| **Bloque de identidad** (nuevo, en recordado, reconectando y no encontrado) | Fila: `LinkIndicator` 16 (trazo 2) + 10 + columna (nombre `titleS` 16/600 `text.primary`, 1 línea con `…` → 2 → estado `bodyS` 13 `text.tertiary` 5,15:1, hasta 2 líneas, sin `…`). Alto 16 + 2 + 18 = **42** con estado de una línea |
| **Error humano** (Q-42) | `margin-top` 8. Renglón 1: mensaje humano `bodyXs` 12 `state.dangerText` (5,20:1), hasta 2 líneas. Renglón 2 (2 de aire): el detalle técnico `caption` 11 `text.tertiary`, **1 línea con `…`** (el texto completo va al log). Incompatible: solo el renglón 1. Entra con la regla del empuje (alto 0-160, opacidad 60-260) |
| Cambio de cuerpo | `HeightTween` 360 `ease.move` (It. 2) con el contenido: sale 0-120 (`ease.exit`), entra 120-280 (`ease.enter`, y +4 → 0) |

### 1.2 Tabla de estados

| # | Estado (condición de App) | Corazón del encabezado | Cuerpo | Alto aprox. TV |
|---|---|---|---|---|
| 1 | **Sin sensor** (desconectado, sin recordado) | 0,40, quieto | Paridad: `Sin sensor` `titleS` → 4 → pista `bodyXs` `text.tertiary` → 12 → `Buscar` tonal con `bluetooth_searching` 18. Con el remo buscando o conectando, `Buscar` al 38 % (paridad) | 186 |
| 2 | **Buscando** | 0,40, quieto | Paridad en la fila de estado (`Spinner` 16 `accent.primary` + 12 + `Buscando sensores…` `bodyS` `text.tertiary`). Filas: §1.3 | 138 + 56 por fila |
| 3 | **Lista** (buscando con resultados) | igual | Filas `#hrm-device-<id>` de §1.3, a lo sumo 8 a la vista; más, desplazan su contenedor | ídem |
| 4 | **Conectando** (manual) | 0,40, quieto | Paridad: `Spinner` 16 + 12 + `Conectando a {name}…` `bodyM` `text.primary`. Al quedar `connected`: §2.1 | 113 |
| 5 | **Conectado** | 1; late a 60/bpm con pulso > 0 (It. 2 §5.3) | Paridad: nombre `bodyM` `text.secondary` → 4 → `142` `num.xl` `metric.hr` + 6 + `bpm` `bodyM` `metric.hr.hud`, misma línea de base; a la derecha `Desconectar` texto peligro. Hasta la primera lectura, `--` y corazón quieto en 1 | 142 |
| 6 | **Lectura vencida** (conectado + watchdog, D11-04) | 1, **quieto** (sin latidos nuevos; la escala vuelve a 1 en 120 desde donde esté) | Como 5 con `--`, y debajo (4 de aire) el renglón **sin señal**: punto de 6 `state.warning` + 6 + texto `bodyXs` `state.warning` (6,41:1), 1 línea | 142 + 20 = **162** |
| 7 | **Recordado, no encontrado** | 0,40, quieto | Bloque de identidad con `link_off` 16 en `text.tertiary` y el estado "no encontrado" → 12 → acciones (paridad: `Conectar` tonal con `refresh`, `Buscar otro` contorno, `Olvidar` texto) en una fila que ajusta (8 / 4) | 42 + 12 + 44 + 28 ≈ **157** |
| 8 | **Reconectando** (recordado + reintentando: al arrancar o tras caída) | **respira** 0,40 ↔ 0,70 (D11-05) | Bloque de identidad con el `LinkIndicator` en `accent.primary`: **arco** durante la espera (se llena lineal en la espera real: 3 000 ms tras una caída, 10 000 ms entre intentos al arrancar) y **spinner** durante el intento (hasta 15 s). Acciones como en 7, con `Conectar` al 38 % (paridad) | ≈ 157 |
| 9 | **Olvidar / Desconectar** (→ 1) | 1 → 0,40 en 160 | Cambio de cuerpo de §1.1 hacia "Sin sensor". Sin aviso ni confirmación (paridad) | 157 o 142 → 186 |

El error (§1.1) puede sumarse a cualquiera: +8 + 16 (+ 16 + 2 con detalle).

### 1.3 Fila de sensor encontrado (estado 3)

Paridad de la It. 2 (min 56, `favorite` **contorno** 24 `metric.hr` 3,81:1 —ícono, ≥ 3—, título `body` `text.primary` con `…`, subtítulo id `caption` `text.tertiary`, `chevron_right` 18). Nuevo, con los verbos de la tarjeta de remo de la 10a:

| Momento | Spec |
|---|---|
| Entrada (sensor nuevo) | Alto 0 → 56 en 0-160 (`ease.move`, regla del empuje); opacidad 0 → 1 y y −8 → 0 en 60-260 (`ease.enter`). Escalonado cada 35 ms, 8 como máximo |
| Reaparece (mismo id) | Se actualiza en su lugar (nombre con fundido de 160); nunca se reanima ni cambia de orden. Clave estable = id |
| Toque | Capa de estado 8 % (paridad); las otras filas a 0,38 en 160 (como la 10a) hasta que el cuerpo cambia a "Conectando" |
| Fin de la búsqueda sin elegir (10 s, Q-44) | El cuerpo vuelve a 1 o 7 con el cambio de cuerpo de §1.1 (paridad: la lista desaparece) |

---

## 2. Transiciones con números

### 2.1 Llega `connected` (manual o automático, con Dispositivo a la vista)

| t | Qué se ve |
|---|---|
| t₀ | El indicador (spinner de la fila "Conectando" o el del bloque de identidad) pasa a **check** dibujado en `state.connected` (9,65:1): círculo 0-160, tilde 80-240 (`BleMotionSpecs`, el mismo de `Listo`) |
| t₀ + 240 → + 600 | Cambio de cuerpo de §1.1 hacia "Conectado". El corazón va de 0,40 a 1 en 160 (`ease.move`) al empezar el cambio |
| Primer pulso > 0 | Primer latido en ese cuadro (It. 2 §5.3) y `--` → número por `TabularNumber` (sin contar intermedios) |

Retención de presentación de 240 ms **sin demorar** el estado de App (igual que A10-05): HUD y sesión ven el sensor en t₀.

### 2.2 Lectura vencida y vuelta de la señal

- **Vence (10 s sin lectura):** número a `--` en el acto (los números no animan); corazón: sin latidos nuevos y escala → 1 en 120 (la opacidad sigue en 1); el renglón "sin señal" crece 0-160 y funde 60-260.
- **Vuelve:** el renglón sale (opacidad 0-120, alto 80-240); el corazón late desde esa lectura; el número entra con el rodillo de la It. 2 (180 ms).

### 2.3 Caída (conectado → reconectando)

Corazón: escala → 1 en 120, opacidad → 0,40 en 160 y, desde los 160, respira. Cambio de cuerpo hacia 8 con el arco en 0 (la espera de 3 s arranca en el cuadro de la caída). Al intentar: el arco lleno se vuelve spinner en el acto (paridad 10a).

---

## 3. Convivencia con la tarjeta del remo (vista de búsqueda)

| Ventana | Disposición | Números |
|---|---|---|
| TV, P360, W1280 (alto normal) | Paridad: columna con la lista del remo (`flex 1 1 0`) y el slot del pulsómetro (`flex 1 1 0`, tarjeta arriba de su mitad). La banda del sonar de la 10a sigue | TV: 795 de contenido; la tarjeta "Lista" con 3 filas mide 138 + 112 = 250 y entra en su mitad (≈ 330) |
| **L640** (alto compacto, ancho de contenido < 840) | **Un único `ScrollView`** (arregla m-06): [caja del enlace] · `Buscar dispositivos` 48 · ayuda · lista de remos a su **alto natural** (sin desplazamiento propio) · 12 · tarjeta del pulsómetro · relleno inferior de la barra. Sin banda de sonar (D10-02) | 608 de ancho. Con 2 remos: 48 + 12 + 16 + 2 × 72 = 220, y la tarjeta empieza en ≈ 232 |
| **TH** (alto compacto, ancho ≥ 840) | **Dos columnas**: izquierda remo (botón, ayuda, lista; desplazamiento propio); derecha pulsómetro, ancho `clamp(0,4 × ancho, 360, 440)`, desplazamiento propio; 16 entre columnas. La caja del enlace del remo va arriba, a todo el ancho | 882 de ancho → derecha 360, izquierda 506. Alto útil ≈ 411 − barra − pestañas ≈ 250: la tarjeta "Sin sensor" en 360 mide ≈ 170 y entra entera |
| Seguir a la tarjeta | Si en L640 la tarjeta crece (buscar, error) y su borde inferior queda fuera de vista, el `ScrollView` se desplaza lo justo para mostrarla, en 240 (`ease.move`); con reducir movimiento, en el acto | — |

El cambio entre disposiciones (rotar o redimensionar) es en el acto, sin animación (es un cambio de ventana, no de dato).

---

## 4. HUD: fuente del pulso y antiparpadeo (0 elementos nuevos)

| Pieza | Spec |
|---|---|
| Indicador de fuente | El corazón 11 de la tarjeta BPM (`.rm-hud-card__heart`, M6 de la 4b) cambia de **variante**: `favorite` **relleno** = pulsómetro; `favorite` **contorno** = monitor (FTMS). Color `metric.hr.hud` en los dos (4,57:1 en el peor fondo, vidrio sobre cielo blanco). Late en los dos: es un pulso real |
| Cambio de fuente | Opacidad del corazón 1 → 0 en 0-80 (`ease.exit`), cambio de glifo, 0 → 1 en 80-200 (`ease.enter`). El latido sigue su fase. Reducir movimiento: en el acto |
| Antiparpadeo (D11-09) | Con pulso 0 o ausente, la tarjeta muestra `--` y espera **1 200 ms** antes de salir (salida actual de la 4b, `KcalSlideMotion`). Si en ese lapso llega un pulso > 0 (de cualquier fuente), se queda y cambia el glifo si cambió la fuente. Caso típico: cae el sensor y el próximo paquete FTMS (≤ 1 s) repone el del monitor sin que la tarjeta salga y vuelva |
| Sin cambios | Ubicación, tamaño, rótulo `PULSO`, valor `metric.hr.hud`. El aviso de §5 explica el porqué |

---

## 5. Avisos breves del pulsómetro (D11-10 a D11-12)

Misma cápsula que el aviso del remo (`.rm-toast--link`: `glass.fallback`, radio 22, `titleXs` `text.hudPrimary`, contenido centrado), en la posición del `ToastLayer` (sube por encima de los obstáculos inferiores).

| Aviso (texto propuesto, §6) | Cuándo | Indicador (dibujado por `LinkIndicator`, 16) | Dura |
|---|---|---|---|
| `Pulsómetro conectado` | Paso a `connected` con Dispositivo fuera de vista (reconexión al arrancar o tras caída; manual desde otra pestaña no existe) | `favorite` relleno en `metric.hr.hud` (4,57:1) con **un** latido al entrar (golpe principal: 0-90 sube a +0,16, 90-290 baja) | 2 s |
| `Se perdió el pulsómetro · reconectando…` | Caída no pedida (F-HRM-08) con Dispositivo fuera de vista | `heart_broken` en `state.warning` (6,24:1) | 4 s |
| `El pulsómetro dejó de enviar el pulso` | Lectura vencida (F-HRM-05) con Dispositivo fuera de vista | `heart_broken` en `state.warning` | 4 s |

Reglas: entra y sale como el aviso del remo (transición común del `ToastLayer`). **No se repite** el mismo aviso antes de 10 s (un sensor que se cae y vuelve no llena la pantalla); `conectado` siempre sale si antes salió `se perdió`. Se descarta si hay un aviso del remo a la vista (D11-12) y no sale desde que se confirma `Terminar` hasta que cierra el inmersivo (regla de la 10a §4.2). Reemplaza a un aviso común con la regla vigente (DV-14). Reducir movimiento: sin el latido de entrada.

---

## 6. Textos (propuestas para el PO; el arte no inventa claves)

| Lugar | Propuesta es / en | Respaldo legacy |
|---|---|---|
| Estado 7 | `No aparece. Revisá que esté prendido y cerca.` / `Not found. Check it's on and nearby.` | `{name} · no encontrado` |
| Estado 8, tras caída | `Se perdió la señal. Reconectando…` / `Signal lost. Reconnecting…` | `{name} · reconectando...` |
| Estado 8, al arrancar | `Buscando tu pulsómetro…` / `Looking for your sensor…` | `{name} · reconectando...` |
| Estado 6 | `Sin señal del sensor. ¿Está bien puesto?` / `No signal from the sensor. Is it on properly?` | (no existe: `--` solo) |
| Error de búsqueda (Q-42) | `No se pudo buscar sensores.` + detalle | `Error al conectar: {error}` |
| Error de conexión (Q-42) | `No se pudo conectar con {name}. Acercalo y probá de nuevo.` + detalle | `Error al conectar: {error}` |
| Avisos | Los de §5 | (no existen) |

`{name}` siempre llega aparte del texto fijo (para el `…`). Sin nombre: `hrmUnknownSensor` (Q-41).

---

## 7. Reducir movimiento

Filas: fundido de 160 sin crecer. Cambios de cuerpo: `HeightTween` en el acto y cruce de opacidad 120 + 120. Check: entero con fundido de 160. Corazón: escala 1 siempre, sin respiración (0,40 fijo en reconectando). Glifo del HUD: en el acto. Arco y spinner: siguen (indican espera). Aviso: sin latido de entrada. Una captura fija con y sin reducir movimiento sale idéntica.

---

## 8. Presupuesto

| Qué | Tope | Cómo |
|---|---|---|
| HUD | **59** (sin cambio; ≤ 60 con `HudBudgetTests`) | Indicador de fuente = variante del glifo del corazón que ya existe; antiparpadeo = temporizador en `HudView`; avisos en el `ToastLayer`, fuera de `#immersive`. **+0** |
| Tarjeta del pulsómetro | Dispositivo no tiene tope | Bloque de identidad +4 (fila, indicador, columna y un segundo texto); renglón "sin señal" +3 (fila, punto, texto); detalle del error +1 |
| `LinkIndicator` con corazón | 1 elemento | El glifo se dibuja con la fuente de íconos en el mismo `generateVisualContent`; repinta solo durante el latido de entrada (290 ms) y con arco/spinner a la vista |
| Respiración del corazón | 0 elementos | Opacidad del `IconView` existente por `MotionDriver`, solo con la pestaña a la vista |
| Basura por cuadro | **0 B** | FrameAllocations con los marcadores `RowMate.Device.HrmCard` y `RowMate.Toast.LinkNotice`: 10 s reconectando (arco + respiración) y 10 s conectado latiendo, en TV |

---

## 9. Tomas que tienen que verse (adaptador falso, `ROWMATE_DATA_DIR` temporal, reloj de prueba)

Sensor falso con nombre ficticio (p. ej. `HRM Test 01`) y dirección `AA:BB:CC:00:0D:01`; nunca datos de un equipo real.

| Toma (AUX, el PO numera sus CAP) | Qué muestra |
|---|---|
| AUX-11-01 a -09 | Estados 1 a 9 de §1.2 en TV (8 con el arco a mitad: t = 1 500 ms de la espera de 3 s; 6 con el renglón naranja) |
| AUX-11-10 | Error de conexión con mensaje humano + detalle de una línea |
| AUX-11-11 / -12 | L640 con un único desplazamiento (2 remos + tarjeta) y TH en dos columnas (lista + "Lista" del pulsómetro) |
| AUX-11-13 / -14 | HUD TV con BPM del sensor (corazón relleno) y del monitor (contorno), con el contorno de depuración |
| AUX-11-15 | HUD W1280 con el aviso `Pulsómetro conectado` y la evidencia de 59 elementos |

---

## 10. Pedidos

### Al Architect

| ID | Pedido |
|---|---|
| **A11-01** | Exponer en el estado del pulsómetro (App/Core, reloj inyectable, tests de `dotnet` con el adaptador falso): **`IsStale`** (conectado y watchdog disparado, se limpia con la próxima lectura válida); **fase de reconexión** (`Waiting` con el instante del próximo intento y la duración de la espera, o `Attempting`); **motivo** de la reconexión (`Startup` o `Drop`); y el **error separado** en `Kind` (búsqueda, conexión, incompatible) + detalle técnico. La vista no infiere nada por tiempos |
| **A11-02** | `HrmCardRules`/`HrmCardViewModel`: los estados 6, 7 y 8 de §1.2 con nombre y estado **separados**, el tipo de indicador (`Spinner`, `WaitingArc` con progreso, `Check`, `LinkOff`) y los dos renglones del error. Con `dotnet test` |
| **A11-03** | `HeartRateCardView`: bloque de identidad con `LinkIndicator`; renglón "sin señal"; check con retención de 240 ms antes del cambio de cuerpo sin demorar `connected` (como A10-05); respiración del corazón (D11-05); filas con clave estable por id y la entrada de la 10a (0-160 / 60-260, 35 ms) |
| **A11-04** | `LinkIndicator`: dos tipos nuevos que dibujan **glifo** en el mismo elemento (`favorite` relleno con un latido de entrada; `heart_broken`), con 0 B por cuadro |
| **A11-05** | Vista de búsqueda con alto compacto (D11-06): ancho de contenido ≥ 840 → dos columnas (derecha `clamp(0,4 W, 360, 440)`); < 840 → un único `ScrollView` con la lista a su alto natural; seguir a la tarjeta al crecer (240). La regla de disposición en Core, con tests en los cinco perfiles. Cierra m-06 de la 10a |
| **A11-06** | HUD: la **fuente del pulso** (`None`, `Sensor`, `Monitor`) en el estado del HUD a partir de la fusión F-HRM-14 (Core, con tests); `HudCard` cambia la variante del corazón con el cruce de 0-80 / 80-200; antiparpadeo de 1 200 ms antes de ocultar BPM (D11-09). `HudBudgetTests` sigue en 59 |
| **A11-07** | Avisos del pulsómetro (§5): en App, la regla de cuándo salen (Dispositivo fuera de vista, prioridad del remo, 10 s sin repetir, nada durante el cierre), con tests de `dotnet`; en Presentation, `ToastLayer` con los indicadores de A11-04 y las duraciones 2 s / 4 s |
| **A11-08** | Reducir movimiento de §7 y el presupuesto de §8 (marcador `RowMate.Device.HrmCard`), sin tocar `prefs.json` ni las bases del usuario |

### Al PO

| ID | Pedido |
|---|---|
| **P11-01** | Tomar (o no) los textos de §6 y las claves nuevas de los avisos (§5) |
| **P11-02** | Ajustar AC-HRM-13-01 para alto compacto: dos columnas o desplazamiento único en lugar del "área de alto acotado que comparte el alto con el estado vacío" (D11-06; el test legacy de 640 × 360 "no desborda y `Buscar` es alcanzable" se sigue cumpliendo) |

---

## 11. Para issues

- Mini-sonar en el corazón del encabezado mientras busca (un anillo de radio 7 → 15, alfa 0,45 → 0, 1 600 ms).
- Barras de señal (RSSI) en las filas de sensores, con A10-02 de la 10a.
- Columna lateral para la tarjeta del pulsómetro en la vista conectada con ventanas anchas (W1280: hoy queda bajo el pliegue, D11-07).
- Vibración sutil en `Pulsómetro conectado` tras una caída (el teléfono va montado en el remo).
- Pista humana cuando `Buscar` está deshabilitado porque el remo busca ("Esperá a que termine la búsqueda del remo").
- Guía ilustrada para relojes (Wear OS / Apple Watch) desde la pista de "Sin sensor" (mejora EXR de F-HRM-16).
- Descripción accesible de la fuente del pulso en la tarjeta BPM ("pulso del pulsómetro" / "del monitor").
- Zonas de frecuencia cardíaca y batería del sensor (0x180F): fuera de alcance del legacy.
