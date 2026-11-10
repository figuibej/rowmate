# It. 14 — Base visual: identidad, arranque y pulido EXR (Designer)

Base de arte para la spec del PO y el diseño del Architect. Fuentes: [ART_BIBLE](../../design/ART_BIBLE.md), [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md), `Assets/RowMate/Presentation/Styles/` (`Tokens.uss`, `Typography.uss`, `Motion.uss`) y la ficha F-APP-16 del inventario ([app-shell-platform](../../product/inventory/app-shell-platform.md)). **No hay tokens nuevos**: todo sale de los que ya existen en `Tokens.cs` (138 variables en `Tokens.uss`). Al cerrar la iteración, lo aceptado pasa a las biblias.

Decisiones tomadas sin debate (una línea cada una):

- **D14-01** Ícono **nuevo y propio**: no se copia ni se redibuja el del legacy (remos neón sobre una "R", JPEG de 640 px que no es nuestro). Se conserva su idea (remos cruzados, fondo azul noche) y se suma el pilar de la biblia "amanecer en el agua".
- **D14-02** Sin texto en el ícono ni en el arranque: el nombre "RowMate" lo pone el sistema (la etiqueta del ícono, el título de la ventana). Así no hay texto fuera del catálogo.
- **D14-03** Todo se genera **por código, de forma determinista** (la misma geometría da los mismos bytes), desde una sola descripción de la geometría en Core. Sin herramientas externas, ni Python ni Inkscape.
- **D14-04** El arranque **no agrega espera**: el logo se ve mientras la app se arma de verdad, y la salida dura 420 ms (`dur.page`) solapada con la entrada del shell.
- **D14-05** Ventana mínima en Windows: **640 × 360 dp** de área cliente (L640, el perfil horizontal más chico que ya se verificó). Un ancho menor no está verificado en ningún perfil horizontal.
- **D14-06** Háptica: solo **3 eventos** (los de la biblia), sin permiso `VIBRATE` (`View.performHapticFeedback`, que respeta el ajuste "respuesta táctil" del sistema).

---

## 1. Ícono "RowMate"

### 1.1 Concepto

**"Amanecer de regata"**: dos remos de *scull* cruzados en X delante de un sol que asoma sobre el horizonte, con las palas metidas en el agua. Es el símbolo universal del remo más la hora del día, que es lo que distingue a la app (la escena con la luz real). Es legible a 16 px, porque se reduce a "X blanca sobre disco ámbar en azul noche", y tiene detalle a 1024 px (empuñaduras, dos olas y el resplandor).

### 1.2 Paleta (tokens existentes)

| Pieza | Color | Token |
|---|---|---|
| Fondo, degradé vertical, arriba → abajo | `#1A2E45` → `#0D1B2A` | `surface.card` → `bg.app` |
| Sol, degradé vertical (de su borde de arriba a la base) | `#FFD166` → `#FF9F1C` | `step.warmup` → `metric.split` |
| Resplandor (solo M) | `#FF9F1C`, de alfa 0,20 en el centro a 0 en el borde (`smoothstep`) | `metric.split` |
| Ola 1 | `#00B4D8` | `accent.primary` |
| Ola 2 (solo M) | `#0077B6` | `brand.seed` |
| Remos | `#FFFFFF` | `text.primary` |

Contrastes sobre el fondo, decorativos (umbral de gráfico 3:1): remo blanco ≥ 12:1, sol ≥ 10:1 y ola 1 ≈ 6:1. El blanco sobre el sol da 1,4:1, así que lo separa el **recorte** (§1.3).

### 1.3 Geometría exacta (la "marca", caja de 100 × 100, origen arriba a la izquierda, y hacia abajo)

Se dibuja en este orden; cada pieza tapa a las anteriores:

1. **Resplandor (G, solo M).** Disco radial con centro (50; 58) y radio 46.
2. **Sol (S).** Semidisco con centro (50; 58) y radio 22, recortado a y ≤ 58: va de y 36 a 58.
3. **Ola 1 (W1).** Trazo `y = 66 + 2,2·sin(2π·(x − 10)/20)` para x ∈ [10; 90] (4 períodos: empieza y termina en y = 66), de 4,4 de grosor, puntas redondas.
4. **Ola 2 (W2, solo M).** Trazo `y = 76 − 2,2·sin(2π·(x − 25)/20)` para x ∈ [25; 75] (2,5 períodos), de 4,4, puntas redondas.
5. **Remo izquierdo (OL)** y, encima, **remo derecho (OR)**. Cada remo es un segmento de la empuñadura **H** a la punta de la pala **T**:
   - OR: H = (18; 8), T = (84; 90). OL: espejo en x = 50, con H = (82; 8) y T = (16; 90).
   - Largo L = 105,262; dirección d = (±0,62701; 0,77901). Un punto a la distancia s es P(s) = H + s·d.
   - Se cruzan en **(50; 47,76)**, delante del sol.
   - Piezas de cada remo, todas *stadium* (segmento con extremos redondos de radio r):

   | Pieza | Segmento s | r | Extremos visibles |
   |---|---|---|---|
   | Empuñadura (solo M) | [3,5; 12] | 3,5 | s 0 a 15,5 |
   | Caña | [3,5; 80] | 2,5 (trazo 5) | — |
   | Pala | [80; 98,762] | 6,5 | s 73,5 a 105,262 (la punta cae justo en T) |

   - **Recorte (*knock-out*)**: antes de dibujar cada remo se **borra** (alfa → 0) la unión de sus tres piezas con r + 3,0. Separa el remo del sol, de las olas y, en el cruce, OR de OL (OR va "por encima").
   - La pala empieza en y ≈ 65,3, a la altura de W1: las palas entran al agua.

Radio máximo de la marca desde (50; 50): 52,8 (la empuñadura en H). Se usa para el área segura de Android (§1.5).

### 1.4 Tres niveles de detalle (por tamaño final en px; nunca se achica un PNG grande)

| Nivel | Tamaño | Marca / lado del lienzo | Cambios contra §1.3 |
|---|---|---|---|
| **M** | ≥ 49 px | 0,70 | ninguno |
| **S** | 25–48 px | 0,80 | sin G, W2 ni empuñaduras; caña r 3,5; pala r 8; recorte +3,5; W1 de 6 de grosor, amplitud 2,6, x ∈ [12; 88], período 19; sol r 23 |
| **XS** | ≤ 24 px | 0,88 | solo el sol (r 24) y los remos; caña r 4,5; pala segmento [78; 95,762] con r 9,5; recorte +4 |

**Rasterizado**: 8 × 8 muestras por píxel, composición "over" en sRGB con alfa recto. El PNG es RGBA de 8 bits, sin fragmentos de tiempo ni de gamma (bytes estables). La marca se centra en el lienzo con su caja de 100.

### 1.5 Tamaños y formatos por plataforma

| Plataforma | Qué | Tamaños | Forma |
|---|---|---|---|
| **Windows** | Íconos del *standalone* de Unity, que arma el `.ico` del `.exe` | los que devuelve `PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application)` en 6000.3 (se espera 1024/512/256/128/48/32/16) | **placa** cuadrada redondeada: margen `round(lado/16)`, radio `round(lado·0,19)`, con el fondo dentro y transparente afuera. En 16: margen 1 y radio 3 |
| **Android** (API ≥ 26: siempre adaptativo) | Ícono **adaptativo**: dos capas de 108 × 108 dp | las que pide `AndroidPlatformIconKind.Adaptive` (432/324/216/162/108/81 px) | **fondo**: el degradé a sangre, sin marca. **Frente**: la marca de 60 dp centrada (radio máximo 31,7 dp, dentro del círculo seguro de 66 dp), transparente alrededor. Sin G en el frente |
| Android, monocromo (iconos temáticos, 13+) | Silueta del frente | 108 dp | todas las piezas del frente con alfa 1 en un solo color y los recortes como huecos; sin G ni degradés |
| Android, *Round* y *Legacy* (los pide Unity) | Por completitud | los de la API | *Legacy*: placa como la de Windows. *Round*: disco de 0,92 del lado |
| Android 12+, ícono del arranque | La marca sin fondo | lienzo de 288 dp, marca de **160 dp** (radio máximo 84,5 < 96) | fondo de la ventana `#0D1B2A` |
| **iOS** [Apple] | `AppIcon` | los de la API de iOS (1024 de la tienda incluido) | a sangre, **opaco** (sin alfa) y sin esquinas (las pone iOS). Oscuro (iOS 18): la marca sobre transparente. Teñido: la silueta monocroma |
| **macOS** [Apple] | `AppIcon` | 16 a 1024 (@1x/@2x) | lienzo de 1024 con la placa de **824 × 824 en (100; 100) y radio 185**, el degradé dentro, sombra `0 10 28` negra al 30 % y la marca a 0,70 de la placa |

**Hoja de verificación** (sin Unity): la misma herramienta escribe `ICON-01-hoja.png` con todos los tamaños y niveles sobre fondo claro (`#F2F2F2`) y oscuro (`#000000`), las 4 máscaras de Android (círculo, *squircle*, cuadrado redondeado y gota) y el monocromo teñido.

---

## 2. Arranque oscuro

**Regla**: desde que el usuario toca el ícono hasta que el shell responde, ningún cuadro tiene otro fondo que `#0D1B2A` (o negro antes del primer cuadro del sistema, en Windows). Nunca blanco, gris ni el logo de Unity.

### 2.1 Por plataforma

| Plataforma | Antes del primer cuadro de Unity | Primer cuadro de la app |
|---|---|---|
| Windows | splash de Unity apagado (`m_ShowUnitySplashScreen: 0`, ya está) y color de fondo del splash `#0D1B2A` (ya está). La ventana del player va de negro a nuestro primer cuadro | la vista de arranque (§2.2) |
| Android 8–11 | imagen de arranque estática de Unity: la marca de 160 dp centrada sobre `#0D1B2A`, escala "centrar" | ídem |
| Android 12+ | arranque del sistema con fondo `#0D1B2A` y el ícono de §1.5 | ídem, con la marca **en el mismo lugar y del mismo tamaño** (160 dp, centrada en la ventana): el relevo no se nota |
| iOS [Apple] | *Launch screen* con fondo `#0D1B2A` y la marca de 160 pt centrada (corrige D-10 del legacy, que mostraba una pantalla blanca) | ídem |
| macOS [Apple] | sin arranque del sistema | ídem |

### 2.2 Vista de arranque y transición al shell

- **Cuadro 0**: una capa encima de todo con fondo `bg.app` y la marca sin fondo de **160 dp** centrada en la ventana (sin área segura, igual que el sistema), sin texto. El shell se arma debajo, invisible.
- **Espera**: hasta que la primera pestaña quedó maquetada y con sus fuentes (no un tiempo fijo). Si pasan 1 500 ms, aparece debajo de la marca, a 32 dp, el indicador de carga de 32 de la app, con fundido de 160.
- **Salida (0–420 ms, desde que el shell está listo):**

| t (ms) | Qué |
|---|---|
| 0–200 | la marca: opacidad 1 → 0 y escala 1 → 0,94 (`ease.exit`) |
| 120–320 | la capa: opacidad 1 → 0 (`ease.exit`). Es el mismo color del shell, así que no se ve un corte |
| 160–420 | contenido de la pestaña y barra superior: opacidad 0 → 1 e y +8 → 0 (`shift.s`, `ease.enter`). Barra de pestañas: solo opacidad. Todo responde desde los 160 |

- **Reducir movimiento**: fundido cruzado lineal de 120, sin escala ni desplazamiento.
- Si el shell está listo en el cuadro 0, igual corre la salida. El arranque nunca demora más que la carga real.
- La animación de marca de la ficha F-APP-16 (remos que se cruzan con un destello de agua) queda **para issues**: suma tiempo o riesgo y no es paridad.

---

## 3. Pasada transversal EXR

### 3.1 Transiciones entre pantallas: inventario único

Regla: **nada aparece ni se va de golpe**, salvo lo marcado "en el acto". Todas usan los tokens `dur.*` y `ease.*` existentes.

| ID | De → a | Spec | Reducir movimiento |
|---|---|---|---|
| T-01 | Arranque → shell | §2.2 (nueva) | fundido de 120 |
| T-02 | Pestaña → pestaña | ANIMATION_BIBLE §2.1 | solo opacidad |
| T-03 | Pila: editor y detalle de sesión | §2.2 (*push* 240, *pop* 200) | fundido de 120 |
| T-04 | Diálogos, menús y avisos | §2.2 | solo opacidad |
| T-05 | Workout ↔ inmersivo y completado | §2.11 | fundido de 120 |
| T-06 | Dispositivo: búsqueda ↔ conectada, pasos de conexión | §2.13 y §2.12 (10a) | solo opacidad |
| T-07 | Cambio de clase de ventana (rotación, redimensionado, pantalla completa) | §3.3 (nueva) | fundido de 120 |
| T-08 | Idioma, letra del sistema | en el acto (ya es así) | — |

QA lo verifica con **VID-01**, el recorrido completo de la demo de la It. 14. Cualquier cuadro con un cambio de más del 30 % del área sin transición (fuera de T-08) es un defecto.

### 3.2 Coherencia tipográfica: cifras tabulares en todo número

**Regla** (ART_BIBLE §4.2, ampliada): todo número que **cambia en su lugar** o que **se alinea en filas o columnas** usa cifras tabulares, con `TabularNumber` o una clase `.rm-type-num-*`. **Exentos**: los números dentro de una frase (`1 serie · 1 descanso`), los identificadores (MAC, nombre de archivo), los campos de texto que el usuario escribe, el simulador y el diagnóstico.

Faltan, según las capturas:

| ID | Dónde | Hoy | Cambio |
|---|---|---|---|
| TY-01 | Detalle de sesión, "Rendimiento por paso" (CAP-06 de la It. 12) | Inter proporcional: en `100W  20.0 spm` y `60W  18.0 spm` las columnas se corren ≈ 10 dp de una fila a otra | cada valor con su unidad es un `TabularNumber` en `num.stat` (16/700) blanco, con la unidad en `bodyS` sobre la línea de base (A9-05); 3 columnas de **ancho fijo por lista** (el máximo de la columna en todas las filas). La línea de arriba (`1:00 · 183m`) va igual, en `text.tertiary` |
| TY-02 | Fecha y hora de las tarjetas del historial (CAP-01 de la It. 9) | Inter proporcional alineado a la derecha: las cifras no caen en el mismo lugar entre tarjetas | `TabularNumber` con Inter en su tamaño y color actuales, en celdas del ancho máximo de los dígitos 0–9 de Inter (sin fuente nueva: ahora no se puede derivar "RowMate Numeric" porque no hay Python) |
| TY-03 | Resto de la app | — | auditoría por código (A14-07); lo que aparezca entra con la regla de arriba |

**Etiquetas con tracking**: el 0,08 em de `label` está pensado para MAYÚSCULAS. Hoy también lo llevan textos con mayúsculas y minúsculas, como las pestañas (`Dispositivo` ocupa 71 de sus 97 dp en TV y a 1,3× se aprieta) y los rótulos del HUD (`Split /500m`, `Watts`, `kcal`). Desde ahora, **0,08 em solo si el texto es todo mayúsculas** y **0,04 em** si lleva minúsculas (el M3 de la barra del legacy: 0,5 px a 12). Se resuelve con una clase modificadora en el USS, no con un token.

### 3.3 Rotación y cambio de ventana fluidos (T-07)

1. **Sin cuadro viejo estirado.** El primer cuadro presentado después del cambio de tamaño ya tiene el *layout* nuevo y la cámara con su aspecto, horizonte y focal nuevos. Lo que falta pintar se tapa con `bg.app` y nunca con la imagen anterior escalada. En Android, la animación de rotación la hace el sistema (Unity maneja los `configChanges`).
2. **Escena**: en el acto, como en la It. 6a (sin reiniciar la distancia ni el tiempo).
3. **HUD con cambio de clase** (vertical ↔ horizontal): en lugar de un fundido único de 200, una **cascada corta**: barra de pasos 0–200, tarjetas de métricas 40–240 y fila de abajo y controles 80–280, cada una con opacidad 0 → 1 y +8·s → 0 (`ease.enter`). Dentro de la misma clase, en el acto.
4. **Redimensionado en vivo en Windows** (arrastrar el borde): en el acto en cada cuadro. Si la clase cambia otra vez antes de 400 ms, la cascada no se repite: queda en el acto hasta soltar.
5. **Pantallas que no son el HUD**: en el acto, conservando el **ancla de desplazamiento** (el primer elemento visible queda arriba, a la misma distancia del borde). La pastilla de la pestaña va al lugar nuevo sin resorte.
6. **Pantalla completa en Windows** (F11 y Alt+Enter, el `allowFullscreenSwitch` de Unity): pasa a ventana sin bordes a pantalla completa y vuelve al tamaño y la posición anteriores, con las reglas 1 a 5. Esc **no** sale de la pantalla completa: ya es "Atrás".
7. **Reducir movimiento**: la cascada pasa a un fundido de 120 de todo el HUD.

### 3.4 Háptica sutil (Android)

Todas con `View.performHapticFeedback`: no piden permiso y el usuario las apaga con el ajuste del sistema.

| ID | Evento | Constante | Respaldo antes de API 30 |
|---|---|---|---|
| H-01 | `Pausa` y `Reanudar` tocados por el usuario | `CLOCK_TICK` (leve) | — |
| H-02 | Cambio de paso de una rutina (incluido el automático; no al empezar la sesión) | `VIRTUAL_KEY` (medio) | — |
| H-03 | Rutina completada, en el cuadro en que entra su diálogo (t = 300) | `CONFIRM` (éxito, API 30+) | `VIRTUAL_KEY`, 120 ms y otra vez `VIRTUAL_KEY` |

- Como mucho **una cada 250 ms**. Si coinciden H-02 y H-03, va solo la H-03. Con la app en segundo plano, nada.
- Reducir movimiento no la apaga (no es movimiento).
- Windows y el Editor no hacen nada. iOS [Apple] queda mapeado para la 10b: `UIImpactFeedbackGenerator` *light* y *medium* y `UINotificationFeedbackGenerator.success`.
- La háptica de conexión y pérdida del remo es del issue #51 (no entra acá).

### 3.5 Ventana (F-APP-15)

- Inicial de 1280 × 720 (ya está).
- Mínima de **640 × 360 dp** de área cliente, por la escala de DPI (D14-05).
- Redimensionable y con el título **"RowMate"** (`productName`).

En pantallas anchas que no son el HUD, el contenido va con **un máximo de 960 dp, centrado** (ART_BIBLE §5.1, "no se aplica todavía") en Historial (lista y detalle), Rutinas (lista y editor) y el selector de Workout. Dispositivo conserva su grilla de contrato (`ancho − 40`), y su tarjeta de pulsómetro en pantallas anchas es el issue #57. Si el PO no lo acepta, pasa a issues.

### 3.6 Lectores de pantalla (lo visual)

Nada que se vea cambia. Lo único que pide el arte es que el rectángulo que informa cada elemento accesible coincida con su caja visible: el Narrador y TalkBack dibujan su propio marco de foco. Para `TabularNumber` y los elementos únicos del HUD que se dibujan con `MeshGenerationContext`, la caja es la del elemento entero, no la de cada cifra.

---

## 4. Inconsistencias visuales en las capturas

Revisé las últimas tomas de las It. 2, 3, 4b, 7, 8, 9, 10a, 11 y 12. "PO" significa texto o paridad: no se cambia sin su decisión.

| ID | Captura | Qué se ve | Propuesta |
|---|---|---|---|
| V14-01 | 11/CAP-13b (TV), 10a/CAP-15a (W1280), 7/CAP-11 (TH), 8/CAP-27 | En la columna derecha del HUD, Split y Watts tienen anchos distintos: sus bordes izquierdos quedan **16·s dp** desparejos (TV 242,7 contra 258,7 dp; W1280 1044 contra 1070 px) | regla **T6**, gemela de T5: Split y Watts miden lo de la mayor, alineadas a la derecha. No cambia ni la zona ni el orden |
| V14-02 | 12/CAP-06, CAP-01 | Filas por paso con cifras proporcionales, columnas que bailan | TY-01 |
| V14-03 | 9/CAP-01 | Fechas proporcionales entre tarjetas | TY-02 |
| V14-04 | 12/CAP-06 | El aviso de exportación deja ver los rótulos del eje (`100`, `50`) a través de su fondo | fondo opaco `surface.overlay`, como el aviso de ART_BIBLE §3.10 (va con el issue #69) |
| V14-05 | 8/CAP-37 | `Escenario` empieza a 16 dp y `O elegí una rutina:` a ≈ 22,5 dp: dos rótulos de sección desalineados en la misma pantalla | los dos a 16, salvo que el legacy tenga ese sangrado (si lo tiene, queda) |
| V14-06 | 2/CAP-01, 9/CAP-01, 10a/CAP-06a | Pestañas con el tracking de mayúsculas sobre texto en minúsculas | §3.2, etiquetas con tracking |
| V14-07 | HUD (todas) | Rótulos `SPM` y `BPM` en mayúsculas junto a `Split /500m`, `Watts` y `kcal`, todos con 0,08 em | tracking según §3.2. El uso de mayúsculas es del catálogo: PO |
| V14-08 | 2/CAP-01, 9/CAP-01, 11/CAP-13b, 12/CAP-06 | Una métrica, cuatro nombres: `SPLIT 500m`, `Split /500m`, `Split` y `Split 500m`; también `VATIOS`, `Watts` y `Vatios` (el chip y el resumen de la misma pantalla) | PO (catálogo, Q-02). Arte: sin cambio |
| V14-09 | 9/CAP-01, 12/CAP-06 | Formato del split desparejo en la misma pantalla: `03:00` en el resumen y `3:00/500m` en las filas | PO: verificar contra el legacy |
| V14-10 | 12/CAP-01, 11/CAP-15b | En W1280 las filas del detalle miden 1 232 dp con el contenido en los primeros 260, y la acción de la tarjeta del pulsómetro queda a 1 100 dp de su nombre | ancho máximo de 960 (§3.5) |
| V14-11 | 11/CAP-15b | Al desplazar, el contenido se corta en seco bajo la barra superior, que es del mismo color | cuando hay desplazamiento > 0, la barra muestra un borde inferior de 1 px `border.subtle` (fundido de 160) |
| V14-12 | 7/CAP-11, 8/CAP-27 | La estela es un rectángulo claro y opaco de ancho constante detrás del bote: se lee como una tabla | es de la 6b (#6, #13). Parche opcional: alfa 0,5 → 0 a lo largo y un 30 % más angosta al final |
| V14-13 | 3/CAP-04, 2/CAP-01, 4b/CAP-07 | La cinta `SIMULADOR` tapa las primeras letras del título (`utinas`, `ispositivo`) | solo en el build con simulador: para issues |
| V14-14 | 10a/CAP-06a | El mismo ícono de remo para `Bici FTMS` y `Dispositivo desconocido` | va con el issue #53 |

---

## 5. Pedidos al Architect

| ID | Pedido |
|---|---|
| **A14-01** | Geometría del ícono en Core (`RowMate.Core.Design.AppIconGeometry`): las piezas de §1.3-§1.4 como datos (*stadium*, semidisco, trazo senoidal, degradés) más un rasterizador puro (8 × 8 muestras, "over" sRGB) y un escritor de PNG sin metadatos (zlib con `DeflateStream` y Adler-32 a mano, que compila igual en .NET 8 y en Unity). Tests de dotnet: el cruce está en (50; 47,76), el radio máximo es ≤ 52,8, el frente de Android entra en el círculo de 33 dp y dos corridas dan los mismos bytes |
| **A14-02** | Generador `tools/icon` (dotnet, como `tools/tokens`, con `--check`): escribe los PNG por plataforma y nivel en `Assets/RowMate/Art/Identity/` y la hoja `ICON-01` en `snapshots/` de esta iteración. Los tamaños los toma de una tabla versionada; un test de Editor la compara con la API de 6000.3 |
| **A14-03** | Asignación en ProjectSettings por un paso del *bootstrapper* (documentado y en el diff final): íconos de Standalone, Android (adaptativo, redondo y *legacy*) e iOS, la imagen de arranque de Android (escala "centrar") y el color y la imagen del *launch screen* de iOS. Investigá en 6000.3 si existe la capa **monocroma** de Android y cómo fijar el fondo del arranque de Android 12+ (`windowSplashScreenBackground`). Si hace falta un recurso en un `.androidlib` o no se puede verificar sin el módulo de Android, queda NO VERIFICABLE o va a issues |
| **A14-04** | Vista de arranque §2.2: una capa en el primer cuadro, la señal "shell listo" (primera pestaña maquetada y con sus fuentes) y la salida `BootSplashMotion` con su reloj inyectable y la variante de reducir movimiento. La marca va como textura de 640 px generada por A14-02 |
| **A14-05** | T-07 (§3.3): ningún cuadro estirado, la cascada del HUD con cambio de clase (0/40/80 + 200), la regla de los 400 ms para el redimensionado en vivo y el ancla de desplazamiento fuera del HUD. Más F11 y la ventana mínima de 640 × 360 dp: si 6000.3 no la trae, con `WM_GETMINMAXINFO` en el plugin de Windows; si no se puede, va a issues |
| **A14-06** | Háptica §3.4: `HapticSpecs` en Core (evento → tipo, una cada 250 ms y la H-03 tapa a la H-02, con tests de dotnet), `IHaptics` con la versión de Android en Kotlin (`performHapticFeedback` sobre la vista de Unity) y una que no hace nada en los demás destinos. Sin permiso nuevo en el manifiesto |
| **A14-07** | Auditoría de cifras (§3.2): un test que recorra las vistas y marque todo `Label` cuyo texto salga de un formateador numérico (duración, split, distancia, vatios, fecha) y que no sea `TabularNumber` ni `.rm-type-num-*`, con la lista de exentos versionada. Implementar TY-01 y TY-02 |
| **A14-08** | Tracking según la caja (§3.2): una clase modificadora para las etiquetas con minúsculas (`letter-spacing: 4px`), aplicada a las pestañas y a los rótulos del HUD. En el HUD, verificar con las sondas que los rótulos no cambien de lugar más de 1 dp |
| **A14-09** | Regla T6 (V14-01): Split y Watts con el mismo ancho, en el cálculo del HUD; sin elementos nuevos (el HUD está cerca de su tope) |
| **A14-10** | Contenido de 960 dp como máximo, centrado (§3.5, V14-10), y el borde de la barra al desplazar (V14-11). Si el PO no los acepta, a issues |
| **A14-11** | Conjunto de capturas `it14` en `capture-snapshots.ps1`, con la carpeta de la 14: CAP-01 arranque TV (cuadro 0), CAP-02 arranque W1280, CAP-03 a CAP-05 TY-01 y TY-02 en TV y W1280, CAP-06 HUD con T6 en TV, TH y W1280, CAP-07 ventana mínima de 640 × 360, CAP-08 pestañas a 1,3×, VID-01 recorrido de transiciones y VID-02 rotación TV → TH con el HUD. Más `ICON-01` desde A14-02 |
| **A14-12** | Agregar el slot `app.icon` a ASSET_SLOTS como "generado (procedural)", con su origen (A14-01/02), sin asset de terceros |

## 6. Para issues

- Animación de marca en el arranque (remos que se cruzan con un destello de agua; F-APP-16, Mejora EXR).
- Recordar el tamaño y la posición de la ventana en Windows (mejora a aprobar de F-APP-15).
- Cinta `SIMULADOR` sobre el título de la página (V14-13).
- Derivar "RowMate Numeric" (Inter con `tnum` congelado) cuando haya Python, para reemplazar las celdas de TY-02.
- Parche de la estela (V14-12) si la 6b sigue postergada.
- Ícono `.ico` versionado aparte del que arma Unity (para un instalador futuro).
