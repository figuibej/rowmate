# It. 10a — Base de arte: búsqueda, conexión, estados del Bluetooth y avisos del enlace

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-08 |
| Iteración | 10a · `10a-ble-android-windows` ([ROADMAP §It. 10a](../../ROADMAP.md), mejoras EXR de F-BLE-04, -05, -06, -08, -09 y -12) |
| Entrada | [01-po-spec](01-po-spec.md) (AC, textos §4, capturas §6, decisiones DV-96 a DV-105); [ART_BIBLE §2, §3.1, §3.10, §4, §5.2, §10](../../design/ART_BIBLE.md); [ANIMATION_BIBLE §1, §2.3, §2.12, §2.13](../../design/ANIMATION_BIBLE.md); `Assets/RowMate/Presentation/Styles/` (`Tokens.uss`, `Typography.uss`, `Motion.uss`); `Presentation/Device/{DeviceScreenView,ScanView,DeviceHeaderView}.cs`; `Core/Design/MotionSpecs.cs`; las tomas de [02-dispositivo-simulador/snapshots](../02-dispositivo-simulador/snapshots/) y de [07-remero-palada/snapshots](../07-remero-palada/snapshots/) (HUD TV y TH) |
| Legacy leído (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/device/device_screen.dart`, `device_provider.dart` |
| Modo | Entrega rápida: cada duda se resolvió con el legacy, con la spec del PO o con un valor por defecto, en una línea (§0.2). Lo menor va a "Para issues" (§12) |

No repite las biblias: fija con números lo que falta para las vistas del Bluetooth real. **Cero colores nuevos, cero fuentes nuevas, cero glifos nuevos** (todo sale de `Tokens.cs` / `Tokens.uss`, de `Typography.uss` y de los 45 glifos de RowMate Icons). **Un solo token de tipo nuevo**: `mono` (§6.1), con JetBrains Mono, que ya está en `UI/Fonts/JetBrainsMono/` y no lo usaba nadie. Los textos y los tiempos de la máquina de estados son del PO ([01-po-spec §3 y §4](01-po-spec.md)); acá solo se dice **cómo se ven y cómo se mueven**. Contrastes en sRGB con el alfa compuesto y cuantizado a 8 bits (WCAG 2.x), calculados para este documento.

Ventanas: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720 (dp). Con **alto compacto** se indica el contenido de Dispositivo de menos de 560 dp de alto (TH y L640).

---

## 0. Resumen

### 0.1 Qué hay que lograr

1. **Buscar se siente como un sonar sereno**: anillos lentos que salen del ícono y tarjetas de remo que entran suaves, empujando lo de abajo sin saltos (§1).
2. **Conectar se lee en tres pasos**: `Conectando…`, `Tomando control del monitor…` y `Listo`, en el renglón de la tarjeta tocada, con un check que se dibuja y una vibración sutil en Android (§2).
3. **El Bluetooth apagado es una ilustración tranquila, no un error seco**: cuando el Bluetooth vuelve, la ilustración "se despierta" y se transforma en la búsqueda (§3).
4. **Los avisos del enlace nunca tapan una cifra**: `Reconectando con {name}…`, `Reconectado con {name}` y `El monitor dejó de enviar datos`, en Dispositivo y sobre el HUD, con **un solo elemento** en el HUD (§4).
5. **El diagnóstico parece una consola técnica prolija**: bytes en mono alineados, sin saltos de alto (§5).
6. **Presupuesto**: 0 B por cuadro con el sonar, el aviso y el diagnóstico a la vista, y el HUD sigue en ≤ 60 elementos (§8).

### 0.2 Decisiones tomadas (una línea cada una)

- **D10-01** El sonar y las barras de señal son **visuales**: no cambian ningún AC. El PO los dejó en "Para issues" ([01-po-spec §9](01-po-spec.md)), pero el pedido de esta iteración los incluye. Quedan como **P1 el sonar** (solo presentación) y **P2 las barras** (necesitan el RSSI en el resultado de búsqueda, A10-02). Si el Architect no llega, el issue lleva esta spec.
- **D10-02** El sonar va en una **banda fija** entre la ayuda y la lista mientras dura la búsqueda. Así el primer remo encontrado entra debajo y nada se corre. Con alto compacto **no hay banda**: queda el spinner del botón, como el legacy (L640 y TH no tienen lugar sin achicar la lista y la tarjeta del pulsómetro, LT-30).
- **D10-03** Los pasos de conexión **reemplazan el renglón de la dirección** de la tarjeta tocada (AC-BLE-06-07). La tarjeta no se agranda: el alto de la lista no cambia mientras se conecta.
- **D10-04** `Listo` dura **600 ms visuales**: el check se dibuja en 0-240 y el "encender" de la It. 2 corre en 240-600. El estado `connected` no espera: la app (Workout) lo ve en el cuadro 0 (AC-BLE-06-07, "≤ 600 ms, sin demorar").
- **D10-05** Un error de conexión es **color e ícono, sin sacudidas** (ANIMATION_BIBLE §2.12). El banner rojo de la It. 2 hace el resto.
- **D10-06** La vista de estado tiene el **ícono de 64 en `state.danger`** (5,45:1 sobre `bg.app`; paridad con el rojo del legacy) sobre un disco con dos anillos quietos. El título usa `title` 20/600: el 18 negrita del legacy no es un token, y ART_BIBLE §3.10 ya fijó 20. La ayuda usa `bodyM` 14 `text.tertiary`, que es el 54 % exacto del AC (5,83:1).
- **D10-07** "Sin Bluetooth LE" (AC-BLE-04-06) usa la misma ilustración con el ícono en **`text.tertiary`** y **sin respiración**: no hay nada que esperar ni que arreglar, así que no se pinta de alarma.
- **D10-08** El aviso del HUD va en una **franja libre que se elige por ventana** con el peor caso, como las demás reglas del HUD: debajo de la barra de arriba si ahí entra sin cruzarse con ninguna tarjeta, y si no, encima de los controles (§4.3). En TV y P360 va abajo, y en TH, L640 y W1280, arriba al centro.
- **D10-09** En el HUD, el aviso es **un único `VisualElement`** que dibuja la cápsula, el indicador y el texto con `MeshGenerationContext`, como `TabularNumber`. El peor caso de la 4b es 59, así que con el aviso queda en 60 (§8).
- **D10-10** El indicador de la reconexión tiene **dos modos**: durante los 3 s de espera, un arco que se llena (lineal, 3 000 ms); durante el intento, el spinner de siempre (una vuelta cada 900 ms). Es el "con progreso" de F-BLE-08 sin números ni contador de intentos, que darían ansiedad.
- **D10-11** La vibración es **`performHapticFeedback(CONFIRM)`** de la vista de Unity (API 30+) y `CONTEXT_CLICK` antes (API 26-29). Respeta el ajuste "respuesta táctil" del sistema y **no necesita el permiso `VIBRATE`**, que el AC-APP-13-01 no lista. Solo en `Listo`. En Windows, nada.
- **D10-12** El ícono de máquina sale del dato de servicio FTMS del anuncio, si está (bit 4 = remo → `rowing`; otro tipo → `fitness_center`). Si no está, va `rowing`, que es la paridad con el legacy. Es P2, junto con las barras.
- **D10-13** En el diagnóstico, la **etiqueta va en Inter y el valor en mono** (`mono` 12): los números y los bytes quedan alineados de renglón a renglón. Los textos fijos en inglés no cambian (Q-04).

---

## 1. Búsqueda (F-BLE-05)

### 1.1 Layout de la vista de búsqueda (TV; sin cambios fuera de la banda)

Relleno 16, como hoy (`ScanView.Padding`). De arriba abajo:

1. [Aviso del enlace (§4.1), si lo hay, con 12 debajo.]
2. `Buscar dispositivos` (primario, 48, a todo el ancho).
3. 12 de aire.
4. Mientras busca: `deviceSearchHint` en `bodyXs` 12 `text.tertiary`, centrado (paridad).
5. **Banda del sonar** (D10-02): 8 + 136 + 8 = **152 dp**, solo mientras busca y sin alto compacto.
6. Lista de remos (§1.3), o el estado vacío de la It. 2 si no busca y no hay resultados.
7. 12 de aire y la tarjeta del pulsómetro (sin cambios).

En TV, el contenido mide 795 dp entre las barras. Quedan ≈ 300 dp para la lista mientras busca, o sea 4 tarjetas a la vista, y ≈ 450 cuando termina.

### 1.2 Sonar (P1; ANIMATION_BIBLE §2.12, con números)

| Pieza | Spec |
|---|---|
| Caja | 136 × 136, centrada en x. Un elemento con malla propia (A10-03) |
| Disco central | Diámetro 56, `accent.tonalBg` sobre `bg.app`. Adentro, `bluetooth_searching` 28 en `accent.onTonal` (es un `IconView` hijo: 2 elementos en total) |
| Anillos | 3, con trazo de 1,5 dp (`stroke.target`) en `accent.primary`. El radio va de 28 (el borde del disco) a 68 con `ease.enter`: r(t) = 28 + 40 · (1 − (1 − t)³). El alfa va de 0,45 a 0 de forma lineal. Período de 1 600 ms y desfase de 533 ms (ANIMATION_BIBLE §2.12). Arrancan escalonados desde t = 0: el primero sale en el cuadro 0 y los otros dos a los 533 y a los 1 066 ms, sin anillos "a mitad de camino" en el primer cuadro |
| Respiración del disco | Escala 1 → 1,03 → 1 con período de 1 600 ms y `ease.pulse`, en fase con el primer anillo |
| Eco al encontrar | Cuando aparece un remo **nuevo** en la lista, sale un 4.º anillo de `accent.onTonal` que va del alfa 0,70 a 0 en 900 ms (`dur.scene`), con la misma curva de radio. A lo sumo uno a la vez: el siguiente lo reinicia. Si llegan varios remos en el mismo cuadro, sale un solo eco |
| Entrada | La banda entra con la búsqueda: el alto va de 0 a 152 en 0-160 y la opacidad de 0 a 1 en 60-260, las dos con `ease.move` (regla del empuje, ANIMATION_BIBLE §2.3: lo cambia el dato). Lo de abajo se corre con el mismo valor |
| Salida (a los 10 s, o al pasar a `connecting`) | Ya no salen anillos nuevos. La opacidad va a 0 en 0-120 (`ease.exit`) y el alto a 0 en 80-240 (`ease.move`). La lista sube junto con la banda. Si no hubo resultados, el estado vacío entra después, en 240-480, con opacidad e y +8 → 0 (`ease.enter`) |
| Contraste | Los anillos son decorativos. El ícono `accent.onTonal` sobre el disco da 7,50:1 |
| Sin foco ni toque | `pickingMode = Ignore` |

### 1.3 Tarjeta de remo encontrado (TV: 379 × 64 dp)

| Pieza | Spec |
|---|---|
| Caja | `surface.card`, `r.card` 16, relleno 12 / 16, `min-height` 64, **8 entre tarjetas** (en el legacy, 4 + 4). Capa de estado `text.primary` al 4 % con *hover* y al 8 % al presionar, sin escala (la de la tarjeta de rutina, `Motion.uss`). Anillo de foco de escritorio de la app |
| Ícono | Disco de 40 en `accent.tonalBg` y `rowing` 24 en `accent.primary` (5,60:1 sobre la tarjeta; D10-12: `fitness_center` si el anuncio dice que no es un remo), y 12 de aire |
| Renglón 1 | El nombre (o `deviceUnknown`) en `titleS` 16/600 `text.primary`, en una línea con `…` |
| Renglón 2 (a 2) | La dirección en `caption` 11/500 `text.tertiary` (5,15:1; el legacy, 11 al 38 %). Durante la conexión, el paso (§2) |
| Barras de señal (P2) | Van a la derecha del texto, antes de la flecha, con 12 de aire. Son 4 barras de 3 × {6, 9, 12, 15} dp, a 2 dp una de otra, apoyadas abajo y con radio 1. Las encendidas van en `text.secondary` (7,53:1) y las apagadas en `border.strong` (2,15:1: es el fondo de la escala; lo que se lee es cuántas hay encendidas). Los niveles del RSSI suavizado (A10-02) son 4 con ≥ −60 dBm, 3 con ≥ −70, 2 con ≥ −80 y 1 si está por debajo; sin RSSI, las barras no se muestran. Un cambio de nivel funde el color de la barra en 160 (`ease.move`). Es un solo elemento que se repinta solo cuando cambia el nivel |
| Derecha | `chevron_right` 20 `text.tertiary`. Durante la conexión, el spinner de 20 en `accent.primary` (paridad: en **todos** los ítems, AC-BLE-06-01) |
| Entrada (remo nuevo en la lista) | Crece: el alto y el margen van de 0 a 64 + 8 en 0-160 (`ease.move`, la regla del empuje: lo trajo el dato); la opacidad de 0 a 1 en 60-260 y la y de −8 a 0 en 60-260 (`ease.enter`), así "baja" desde el sonar. Si llegan varios a la vez, se escalonan cada 35 ms, 8 como máximo (ANIMATION_BIBLE §2.3) |
| Reaparece (el mismo remo) | Se actualiza en su lugar: solo pueden cambiar el nombre (fundido de 160) y las barras. Nunca se reanima ni cambia de orden |
| Lista nueva (otra búsqueda) | La lista anterior colapsa toda junta (opacidad 0-120, alto 80-280) cuando arranca la búsqueda, y el sonar entra en el mismo cuadro. Son verbos de la It. 3 |

Elementos por tarjeta: caja, disco, ícono, columna, nombre, dirección o paso, barras y flecha o spinner, o sea **8**. Con 8 tarjetas son 64 (Dispositivo no tiene el tope del HUD; §8).

### 1.4 Estados que ya existen (sin cambios de arte)

El estado vacío (ART_BIBLE §3.10, It. 2), el banner rojo de error (`Error al escanear: …`, AC-BLE-05-05, con la forma "Error en línea" de §3.10) y el botón buscando (spinner blanco de 16 y `Buscando...`, deshabilitado al 38 %).

---

## 2. Conexión en pasos visibles (F-BLE-06; AC-BLE-06-07)

### 2.1 En la tarjeta tocada (D10-03)

| t | Qué se ve |
|---|---|
| Toque (0) | La capa de presión de §1.3. Las **otras** tarjetas bajan a la opacidad 0,38 en 160 (`ease.move`) y muestran su spinner de 20 (paridad); la tocada queda en 1 con el spinner en `accent.primary`. `Buscar` queda deshabilitado (paridad) |
| 0-200 | El renglón 2 cambia la dirección por `deviceStepConnecting`: el texto viejo funde → 0 en 0-80 y el nuevo entra en 80-200, en `caption` 11/500 `text.secondary` (6,9:1 sobre la tarjeta; el paso en curso se lee más que la dirección) |
| Paso 2 (con Control Point, al escribir `0x00`) | `deviceStepControl` con el mismo cruce de 0-80 / 80-200. Sin Control Point (`K-NOCP`), este paso no aparece |
| `connected` (t₀) | `deviceStepReady` en `state.connected` (9,65:1). En t₀ el spinner se reemplaza por un **check dibujado**: un círculo de 20 con trazo 2 en `state.connected` que se llena en 0-160 y la tilde que se traza en 80-240 (`ease.enter`; ANIMATION_BIBLE §2.12, "trazo 240"). La vibración (§7) va en t₀ |
| t₀ + 240 → + 600 | El "encender" de la It. 2 (ANIMATION_BIBLE §2.13): la búsqueda sale en 0-120 y la cabecera y la grilla entran en 120-360. Con 240 de check, el total es **600 ms** (D10-04) |

El cruce de los textos no espera: si un paso dura menos de 200 ms, el siguiente arranca desde la opacidad que tenga el texto (no se encolan).

### 2.2 Error (D10-05)

- El spinner de la tocada pasa a `error` 20 en `state.dangerText` (5,20:1). El renglón 2 vuelve a la dirección, en el color de siempre, en 160.
- Las otras tarjetas vuelven a la opacidad 1 en 160 y todas recuperan la flecha.
- El banner `Error al conectar: …` crece arriba con los verbos de la It. 3.
- A los 1 200 ms, el ícono de error de la tarjeta vuelve a la flecha (fundido de 160). No hay sacudidas ni rebotes.

### 2.3 Reducir movimiento

Los textos cambian con un fundido de 80. El check aparece entero con un fundido de 160, sin trazo. La opacidad de las otras tarjetas cambia en el acto. El "encender" queda en su versión reducida (It. 2). Los 600 ms no cambian.

---

## 3. Vista de estado: Bluetooth apagado, sin permiso y sin Bluetooth LE (F-BLE-04)

### 3.1 Composición (TV; centrada en los dos ejes entre la barra superior y la de pestañas)

| Pieza | Spec |
|---|---|
| Ilustración | Caja de 176 × 176: dos anillos quietos de radio 64 y 84 con trazo 1,5 en `text.primary` al 8 % y al 5 %, y un disco de 112 en `surface.card`. Adentro, el ícono `bluetooth_disabled` **64** en `state.danger` (D10-06). Un elemento con malla (los anillos y el disco) y el `IconView` hijo |
| Respiración | Solo apagado y sin permiso: la opacidad del ícono va de 1 a 0,72 y vuelve, con período de 3 200 ms y `ease.pulse` (el doble de lento que el sonar: la vista espera). Sin Bluetooth LE no respira (D10-07) |
| Título | 24 de aire y después `deviceBtOff`, `deviceBtDenied` o `deviceBleUnsupported` en `title` 20/600 `text.primary`, centrado, en hasta 2 líneas |
| Ayuda | 8 de aire y después la pista en `bodyM` 14 `text.tertiary` (5,83:1), centrada, con ancho máximo de 320. Los `\n` del catálogo se respetan |
| Botón | 24 de aire y después `deviceBtEnable` con el ícono `bluetooth`, primario de 48, ancho mínimo 220 y centrado. No está en la vista sin Bluetooth LE. Fuera de Android, su descripción accesible es `deviceEnableSettings` |
| Banner | Si hay un error, sigue arriba, como en el AC-BLE-04-01 |
| Alto compacto (L640, TH) | La ilustración baja a 112 (disco 72, ícono 48 y anillos de 44 y 56) y los aires a 16, 8 y 16. Si así tampoco entra, la columna se desplaza (desplazamiento de la app, sin barra en móvil) |

En TV, el bloque mide 176 + 24 + 28 + 8 + 40 + 24 + 48 ≈ **348 dp** y queda centrado en los 795 dp del contenido.

### 3.2 Transformaciones (AC-BLE-04-03: "sin salto")

| Cambio | Coreografía | Reducir movimiento |
|---|---|---|
| Apagado o sin permiso → encendido ("se despierta") | 0-240: el color del ícono va de `state.danger` a `accent.primary` (`ease.move`), y en 80-200 el glifo cruza de `bluetooth_disabled` a `bluetooth`. 0-360: el disco va de escala 1 a 1,06 (`ease.enter`) y sale **un anillo** de sonar (radio 56 → 84, alfa de 0,45 a 0, 900 ms). 360-480: la vista de estado funde → 0 (`ease.exit`). 480-720: entra la búsqueda con la tarjeta del pulsómetro, opacidad 0 → 1 e y +8 → 0 (`shift.s`, `ease.enter`) | El glifo y el color cambian en 0-160, y después hay un cruce de solo opacidad de 120 + 120 |
| Búsqueda → apagado o sin permiso | 0-120: la búsqueda sale (opacidad, `ease.exit`). 120-360: la vista de estado entra, el disco va de escala 0,94 a 1 y la opacidad de 0 a 1 (`ease.enter`), y el ícono llega ya en rojo | Solo opacidad, 120 + 120 |
| Conectada → apagado (se apagó el Bluetooth con el remo conectado) | El "apagar" de la It. 2 con la vista de estado como destino: 0-120 sale la vista conectada (escala 0,985), y 120-360 entra la de estado como en la fila anterior | Lo de la It. 2 |
| Primer estado conocido en los primeros 500 ms desde que se abre la app | En el acto, sin animación: el "desconocido" del arranque no tiene que verse como un cambio | — |
| Cambio en medio de una transición | La transición actual salta a su final y arranca la nueva (regla de la It. 2) | — |

---

## 4. Avisos del enlace: reconectando, reconectado y monitor apagado

### 4.1 En Dispositivo (AC-BLE-08-04, AC-BLE-09-02)

Es una caja arriba de `Buscar dispositivos`, con `r.l` 12, relleno 12 / 12 / 12 / 16, `min-height` 48 y 12 de aire debajo. Crece y colapsa con la regla del empuje (alto en 0-160 y en 80-240, `ease.move`).

| Variante | Fondo | Izquierda | Texto (`bodyM` 14) | Derecha |
|---|---|---|---|---|
| Reconectando | `surface.card` | Indicador de 20 (D10-10): durante la espera, una pista de 20 con trazo 2 en `border.strong` y un arco en `accent.primary` que se llena en 3 000 ms lineales desde arriba, en sentido horario; durante el intento, el spinner de 20 de `accent.primary` | `deviceReconnecting` en `text.primary`, en una línea con `…` en el **nombre**, no en el texto fijo (A10-08) | `deviceDisconnect` como botón de texto en `state.dangerText` (5,20:1), de 36 de alto |
| Monitor apagado | `state.warningBg` | `link_off` 20 `state.warning` | `deviceMonitorStopped` en `state.warning` (6,30:1) | `retry` como botón de texto en `accent.primary` (5,51:1 sobre el fondo naranja) |

El paso de un modo del indicador al otro es en el acto: el arco lleno se vuelve la cabeza del spinner y no hay fundido. Al reconectar, la caja no muestra una confirmación propia: el "encender" de la vista conectada ya lo es.

### 4.2 Sobre el HUD (AC-BLE-08-05, AC-BLE-09-02; D10-08, D10-09)

| Pieza | Spec (en dp base × `hudScale` `s`) |
|---|---|
| Elemento | `#hud-link-notice`: **uno solo**, con `pickingMode = Ignore` (es informativo: en el HUD no tiene acción). Dibuja la cápsula, el indicador y el texto con `MeshGenerationContext` (A10-08) |
| Cápsula | 36·s de alto, `r.full`, `glass.fallback` (el vidrio plano del HUD, **sin desenfoque**, así no suma pasadas de `_RmHudBlur`) y borde de 1 en `border.default`. Relleno de 14·s a los lados, indicador de 16·s, 8·s y el texto |
| Texto | `titleXs` 14/600 × s en `text.hudPrimary` (13,46:1 en el peor fondo, el vidrio sobre cielo blanco). Una línea. Ancho máximo = la franja libre − 16·s; si no entra, el `…` va en el nombre |
| Reconectando | El indicador de D10-10 a 16·s, con trazo de 2·s, en `metric.spm.hud` (7,11:1 en el peor fondo): el cian del HUD |
| Reconectado (2 s, DV-99) | El indicador pasa a un check dibujado en `state.connected` (9,41:1): disco en 0-160 y tilde en 80-240. El texto cruza a `deviceReconnected` (0-80 sale y 80-200 entra). El ancho de la cápsula sigue al texto en 0-240 (`ease.move`, lo cambió el dato) y queda centrado |
| Monitor apagado (4 s, DV-98) | `link_off` 16·s en `state.warning` (6,24:1 en el peor fondo) y `deviceMonitorStopped` en `text.hudPrimary` |
| Entrada | Opacidad de 0 a 1 en 0-200 (`ease.enter`). La y llega desde −8·s si el aviso está arriba (baja de la barra) o desde +8·s si está abajo (sube desde los controles) |
| Salida (al vencer los 2 s o los 4 s) | Opacidad → 0 en 0-160 (`ease.exit`) y y → ∓4·s |
| Reemplazo | Un aviso nuevo con el anterior todavía a la vista cruza en el lugar, sin salir y volver a entrar: el indicador y el texto cruzan como en "Reconectado", y el ancho sigue en 240 |
| Cuándo no se muestra | Desde que se confirma `Terminar` hasta que cierra el inmersivo (spinner del guardado, sello y diálogo de completado). Así no se suma nunca al peor caso de la 4b (§8). La escena se trata en A10-09 |

### 4.3 Dónde va en el HUD (regla por ventana, A10-08)

La regla se decide **una vez por ventana** con el peor caso: la barra en su alto máximo (74·s), la cápsula más ancha (el texto más largo con el nombre de 24 caracteres) y SPM, Split y Watts con objetivo. Así el aviso no cambia de lugar con los datos.

1. **Arriba (A):** se centra en x. El borde de arriba va a 8·s del alto máximo de la barra. Vale si el rectángulo, con 8·s de margen, no se cruza con SPM, Split ni Watts, ni con la columna de series, el disco del centro alto ni la zona del bote.
2. **Abajo (B):** se centra en x. El borde de abajo va a 12·s del borde superior de los controles. Vale si no se cruza con la banda tiempo-distancia, con kcal ni con BPM.
3. Si no entra en ninguna de las dos (no pasa en los cinco perfiles), se usa A, pegado al borde de abajo de la barra, y se anota en el log del arnés.

Medidas sobre las tomas de la It. 7:

| Ventana | Franja | Lugar libre |
|---|---|---|
| TV | **B** | La banda termina en ≈ 765 dp y los controles empiezan en ≈ 838, así que hay 73 dp de alto para 36 + 2 × 12 |
| P360 | **B** | La misma proporción con s = 0,92 |
| TH | **A** | Entre SPM (borde derecho en ≈ 178) y Split (borde izquierdo en ≈ 758), y desde la barra (≈ 50) hasta la zona del bote (≈ 208), hay 580 × 158 dp |
| L640 | **A** | Entre SPM y Split hay ≈ 316 dp libres, contra una cápsula de ≈ 240 |
| W1280 | **A** | Es la franja más amplia |

En B, el aviso queda registrado como **obstáculo inferior** del `ToastLayer`: un aviso emergente que coincida sube por encima con 8 de aire (ART_BIBLE §3.10).

### 4.4 Reducir movimiento

Hay solo opacidad, con los mismos tiempos, y el ancho cambia en el acto. El arco de espera sigue llenándose, porque indica espera, igual que el spinner. El check aparece entero.

---

## 5. Diagnóstico oculto (F-BLE-12; AC-BLE-12-01, -02)

### 5.1 Acceso

- `Show BLE debug` queda como en el legacy (AC-BLE-12-01): `bug_report` 14 y el texto `bodyXs` 12, los dos en `text.primary` con opacidad 0,24 (2,18:1). Es a propósito: es un acceso escondido, igual que el ícono del vacío de la It. 2. Aparece con un fundido de 160.
- **Mantener presionada la cabecera** (DV-104): en 0-500 ms, la capa de estado de la cabecera sube del 0 al 8 % de forma lineal (es el progreso del gesto). A los 500 ms, el panel se abre directamente, sin pasar por el botón. Soltar antes hace volver la capa en 160.

### 5.2 Panel

| Pieza | Spec |
|---|---|
| Caja | `dev.bleDebugBg` (negro al 38 %) sobre `bg.app`, `r.l` 12, borde de 1 en `border.default`, relleno 12 y a todo el ancho. Entra con la regla del alto de la It. 2: en 360 (`ease.move`), con el contenido en 160 |
| Encabezado | `bug_report` 14 `text.tertiary`, 6 de aire y `DEBUG BLE` en `label` 12/600 con 0,08 em de tracking, en `text.tertiary` (6,05:1). Separador e insignia a la derecha |
| Insignia | Relleno 2 / 8, `r.m` 8, `caption` 11/500. `RECEIVING` va en `state.connected` sobre `state.connectedBg` (13,3:1 sin el tinte) y `NO DATA` en `state.dangerText` sobre `state.danger` al 20 % (7,15:1 sin el tinte). Con `RECEIVING`, un punto de 6 a la izquierda del texto **late con cada paquete**: opacidad de 1 a 0,35 en 300 (`ease.pulse`). Es el latido de los datos: con reducir movimiento queda fijo en 1 |
| Separadores | 1 en `border.default`, con 8 arriba y abajo (son los 16 del legacy) |
| Renglones | La etiqueta en `bodyXs` 12 `text.tertiary`, a la izquierda; el valor en `mono` 12 `text.primary`, a la derecha (D10-13). Renglón de 20 |
| Bytes crudos | `Raw bytes (hex):` en `caption` 11 `text.tertiary`, 4 de aire y el hex en `mono` 12 / 18 `dev.bleDebug` (15,2:1). Corta **solo en los espacios**, así nunca parte un byte. Reserva **2 líneas** de alto mínimo, así el panel no salta cuando el largo del paquete cambia: en TV entran 47 caracteres por línea (347 dp / 7,2 dp), o sea 16 bytes, y un Rower Data típico de 20 bytes ocupa 2 líneas. Llega a 3 líneas como máximo. Si hay más, la última termina en `…` |
| Cambios con cada paquete | Sin animación: el texto cambia en el acto, una vez por segundo. El único movimiento es el punto de la insignia |

Son ≈ 22 elementos.

### 5.3 Token `mono` (el único nuevo)

`mono`: JetBrains Mono, 400, 12 / 18, con tracking 0. Va en `Typography.uss` a través de `tools/tokens`, con su `.rm-type-mono` y la escala de letra (`.rm-text-scale-*`), como los demás. Avance: 0,6 em = 7,2 dp a 12. El rol `FontFamilyRole.Mono` ya existe; falta generar su `FontAsset` (A10-10).

---

## 6. Tipografía y color usados (resumen)

### 6.1 Tipo

Se usan `title` 20/600, `titleS` 16/600, `titleXs` 14/600 (el HUD), `bodyM` 14, `bodyXs` 12, `caption` 11/500, `label` 12/600 y el nuevo `mono` 12/18. No hay cifras grandes nuevas: el tablero sigue con `num.xl`.

### 6.2 Color (todo de tokens existentes)

| Uso | Token | Contraste (fondo) |
|---|---|---|
| Sonar, spinner, arco de reconexión en la app | `accent.primary` | 7,06 (`bg.app`), 5,60 (tarjeta) |
| Ícono del sonar | `accent.onTonal` sobre `accent.tonalBg` | 7,50 |
| Ícono del Bluetooth apagado | `state.danger` | 5,45 (`bg.app`) |
| Listo, reconectado, `RECEIVING` | `state.connected` | 9,65 (tarjeta), 9,41 (vidrio sobre blanco), 13,28 (diagnóstico) |
| Monitor apagado | `state.warning` sobre `state.warningBg` (app); sobre el vidrio (HUD) | 6,30; 6,24 |
| Error en la tarjeta, `NO DATA`, `Desconectar` | `state.dangerText` | 5,20 (tarjeta), 7,15 (diagnóstico) |
| Bytes crudos | `dev.bleDebug` sobre `dev.bleDebugBg` | 15,19 |
| Indicador del HUD | `metric.spm.hud` | 7,11 (vidrio sobre blanco) |

---

## 7. Háptica (D10-11)

| Momento | Android | Windows y Editor |
|---|---|---|
| `Listo` (t₀ de §2.1) | `View.performHapticFeedback(HapticFeedbackConstants.CONFIRM)` en API 30+; `CONTEXT_CLICK` en API 26-29. Sin el permiso `VIBRATE`. Si el usuario apagó la respuesta táctil del sistema, no vibra | Nada |
| Todo lo demás | Nada en esta iteración: las pausas y los pasos quedan para la It. 14 (ANIMATION_BIBLE §2.12) | — |

Reducir movimiento **no** apaga la háptica, porque no es movimiento en pantalla. El adaptador falso registra cuántas veces se pidió (A10-06).

---

## 8. Presupuesto

| Qué | Tope | Cómo |
|---|---|---|
| HUD | **≤ 60 elementos** en el peor caso (`HudBudgetTests`) | El aviso suma **1** (D10-09). El peor caso de la 4b es 59 y el aviso no se muestra durante el cierre (§4.2), así que el peor caso con aviso es el de una rutina en marcha: 59 + 1 = **60**. Si `HudBudgetTests` mide más, el plan B es dibujar el indicador **dentro** del mismo elemento que ya cuenta la barra de arriba. **Nunca** se saca una pieza del HUD |
| Sonar | 2 elementos (la malla y el ícono) | 4 anillos como máximo, de 48 segmentos cada uno: 4 × 48 × 2 = 384 vértices y 1 152 índices, en *buffers* reusados (`MeshGenerationContext.Allocate`, como el gráfico de la It. 9). Se repinta una vez por cuadro **solo mientras busca y con la pestaña a la vista**; con la app en segundo plano o en otra pestaña, `MotionDriver` no lo mueve |
| Aviso del HUD | 1 elemento | La malla de la cápsula, el arco y el texto con `DrawText`. El texto se arma una vez por **cambio de estado**, nunca por cuadro, y el arco se repinta por cuadro solo con la reconexión a la vista |
| Barras de señal | 1 elemento por tarjeta | Se repinta solo cuando cambia el nivel |
| Diagnóstico | ≈ 22 elementos | El hex se arma una vez por paquete (≈ 1 Hz) con un `StringBuilder` reusado. Es la única asignación y no es por cuadro. Solo con el panel abierto |
| Basura por cuadro | **0 B** | Con FrameAllocations y marcadores del Profiler `RowMate.Device.Sonar` y `RowMate.Hud.LinkNotice`: 10 s de búsqueda con 3 remos falsos, y 10 s del HUD reconectando (TV y W1280) |
| CPU | UI ≤ 1,0 ms (ART_BIBLE §9) | Solo transformaciones, opacidades y repintado de mallas chicas, sin rearmar el *layout* por cuadro. El alto de la banda y de las tarjetas anima con la regla del empuje: el *layout* cambia en 160-240 ms y después queda quieto |

---

## 9. Tomas que tienen que verse (las CAP del PO, con el adaptador falso `K-10`)

Todas las tomas se hacen con el adaptador falso, `ROWMATE_DATA_DIR` temporal y el reloj de prueba. Ninguna tiene un nombre o una dirección reales.

| CAP del PO | Qué tiene que mostrar el arte |
|---|---|
| CAP-01 a CAP-04 | La ilustración de §3.1 con su ícono: rojo, en reposo de la respiración (opacidad 1), y en la 04 en `text.tertiary` y sin botón |
| CAP-05 | La banda del sonar con los 3 anillos en t = 1 200 ms (radios 67, 60 y 37 con t = 0,75, 0,42 y 0,08; alfas 0,11, 0,26 y 0,41), sin estado vacío |
| CAP-06 | 3 tarjetas de §1.3 en su valor final: `AMS-670B` con `rowing`; `Dispositivo desconocido`; y `Bici FTMS` con `fitness_center` si el falso anuncia el tipo, y barras 4, 3 y 2 si va P2 |
| CAP-07 | L640 y TH sin banda (D10-02) |
| CAP-09 a CAP-11 | La tarjeta tocada con cada paso, las otras al 0,38 con su spinner, y en la 11, el check entero (t₀ + 240) |
| CAP-14 | La caja de §4.1 "Reconectando", con el arco a mitad (t = 1 500 de la espera) |
| CAP-15 y CAP-16 | El HUD con la cápsula en su franja (TV abajo, W1280 arriba), sin cruzarse con ninguna tarjeta: se verifica con el contorno de depuración de la zona del bote (CAP-16 de la It. 4a) |
| CAP-17 y CAP-18 | La caja naranja con `Reintentar`, y la cápsula con `link_off` |
| CAP-19 y CAP-20 | El panel de §5.2: el hex en 2 líneas con el paquete de `ftms.json`, y `NO DATA` |
| **Nuevas del arte** (AUX, no son criterios): AUX-10a-01 | El HUD de TV con el peor caso de la 4b **y** el aviso, con el contorno de depuración. Es la evidencia de los 60 elementos |
| AUX-10a-02 | Los cuadros 0, 240, 480 y 720 de "se despierta" (§3.2), en TV |

---

## 10. Textos

Todos los textos son los de [01-po-spec §4](01-po-spec.md). El arte **no agrega claves**. Lo único que pide es que `{name}` llegue aparte del texto fijo, para que el `…` de ancho corte el nombre y no `Reconectando con` (A10-08).

---

## 11. Pedidos al Architect

| ID | Pedido |
|---|---|
| **A10-01** | Exponer en el estado del remo (App/Core) la **fase de la conexión** (`Connecting`, `TakingControl` solo con Control Point, `Ready`), la **fase de la reconexión** (`Waiting` con el instante del próximo intento, para el arco de 3 s, o `Attempting`) y el **motivo de la última desconexión** (`Manual`, `Watchdog` o `Lost`). Todo con el reloj inyectable y con tests de `dotnet` sobre el adaptador falso. La vista no infiere nada a partir de los tiempos |
| **A10-02** | (P2) El resultado de búsqueda con el **RSSI** (Android `ScanResult.getRssi`, Windows `RawSignalStrengthInDBm`) y, si viene, el **tipo de máquina** del dato de servicio FTMS (AD 0x16, UUID 0x1826: banderas y 2 bytes de tipos, donde el bit 4 es el remo). En Core: el suavizado con EMA α 0,3, la histéresis de 3 dB y los niveles 4/3/2/1 con −60, −70 y −80 dBm, con `dotnet test`. Nada de eso se loguea con el nombre ni la dirección de un equipo real |
| **A10-03** | `SonarView`: un elemento con malla propia, 4 anillos en *buffers* reusados, tiempos con `MotionDriver` y constantes en `MotionSpecs` (1 600, 533, 900, 0,45, 0,70, 28 → 68, 1,03). Arranca con t = 0 al empezar la búsqueda; el eco se dispara con "remo nuevo en la lista"; se detiene fuera de la pestaña y en segundo plano |
| **A10-04** | La lista de remos con **clave estable por dirección** e ítems que crecen con la regla del empuje (0-160 / 60-260, y −8 → 0), escalonados cada 35 ms. Al reaparecer, se actualiza en su lugar; al empezar una búsqueda nueva, colapsa todo junto. La banda del sonar anima su alto con la misma regla y no existe con alto compacto (< 560 dp de contenido) |
| **A10-05** | Los pasos en el renglón 2 de la tarjeta tocada: cruce de 0-80 / 80-200 y check dibujado en 0-240. Una **retención de presentación** de 240 ms antes del "encender" (`ViewSwap`), sin demorar `connected` en el estado de la app: Workout se habilita en t₀ |
| **A10-06** | `IHaptics` en Platform: en Android, `performHapticFeedback(CONFIRM)` en API 30+ y `CONTEXT_CLICK` en API 26-29 sobre la vista de Unity, en el hilo de UI y **sin el permiso `VIBRATE`**. En Windows y el Editor no hace nada. La versión falsa cuenta las llamadas para los tests |
| **A10-07** | `BluetoothStateView`: la ilustración (un elemento con malla y el `IconView`), sus tres variantes y las transformaciones de §3.2, con la regla del primer estado en los primeros 500 ms (en el acto) |
| **A10-08** | `LinkNotice` en dos formas: la **caja de Dispositivo** (§4.1, elementos comunes) y el **elemento único del HUD** (§4.2, cápsula + indicador + texto con `MeshGenerationContext`, como `TabularNumber`). El texto se arma con el nombre **aparte** para cortarlo con `…`. La **regla de la franja** A/B (§4.3) va en Core, junto a `HudZones`, decidida por ventana y con tests de `dotnet` en los cinco perfiles. En B, el aviso se registra como obstáculo del `ToastLayer`. Se oculta desde que se confirma `Terminar`. `HudBudgetTests` suma el caso "peor caso + aviso" |
| **A10-09** | La escena con el **enlace caído** (reconectando, o monitor apagado durante un entrenamiento) toma la inercia de la pausa (ANIMATION_BIBLE §3.9 y §4.7): el bote se desliza y frena en lugar de seguir remando con la última lectura congelada. Las métricas y el reloj no cambian (Q-34). Al volver los datos, retoma como al reanudar. Es solo visual: si el Architect ve que toca el contrato de la sesión, va a "Para issues" |
| **A10-10** | El token de tipo `mono` (12 / 18, JetBrains Mono 400) en `Tokens.cs` → `tools/tokens` → `Typography.uss`, y el `FontAsset` de `FontFamilyRole.Mono` en `FontAssetPaths` y el *bootstrapper* (hoy no se genera). Tiene que convivir con `GeneratedAssetGuard` |
| **A10-11** | El panel del diagnóstico de §5.2 con el hex de alto mínimo de 2 líneas y corte en los espacios. El gesto de mantener presionada la cabecera (500 ms, con la capa como progreso). El punto que late con cada paquete. El `StringBuilder` reusado para el hex |
| **A10-12** | Las variantes con reducir movimiento de §1.2, §1.3, §2.3, §3.2, §4.4 y §5.2. Una captura fija con y sin reducir movimiento sale idéntica (como en la It. 2) |
| **A10-13** | Medir el presupuesto de §8 con FrameAllocations y los marcadores `RowMate.Device.Sonar` y `RowMate.Hud.LinkNotice`, más AUX-10a-01, sin tocar `prefs.json` ni las bases del usuario y sin dejar cambiados los ProjectSettings |

---

## 12. Para issues

- Sonido sutil al quedar `Listo` y al reconectar (F-BLE-06, mejora EXR). Hoy no hay sistema de audio de UI.
- Vibración también en `Reconectado con {name}` (el teléfono va montado en el remo y es una confirmación sin mirar).
- Cierre "Búsqueda terminada" con un último eco del sonar (está en los issues del PO).
- Indicador de paquetes por segundo en el diagnóstico (está en los issues del PO). Iría en `mono`, al lado de `Notifications`.
- Glifos propios para los estados: `lock` (sin permiso), `power_settings_new` (monitor apagado, en lugar de `link_off`) y `signal_cellular_*` (si se prefiere a las barras dibujadas). Cada uno exige volver a recortar RowMate Icons (ASSET_SLOTS §4).
- Guía visual corta por plataforma en la vista de estado: una ilustración de dónde está el interruptor en Android y en Windows (mejora EXR de F-BLE-04; las pistas por plataforma están en los issues del PO).
- La lista **se transforma** en el tablero con un elemento compartido (la tarjeta tocada se vuelve la cabecera), en lugar del "encender" común.
- El "LED" de estado del remo siempre visible en la barra de pestañas (mejora EXR de F-BLE-10).
