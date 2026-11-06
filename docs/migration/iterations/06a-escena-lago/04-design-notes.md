# It. 6a — Escena: Core de escena, cielo, luz, hora, Lago alpino y agua degradada: notas de diseño (arte, luz y motion)

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-02; corregidas por el gate el 2026-11-03 |
| Iteración | 6a · `06a-escena-lago` ([ROADMAP §It. 6a](../../ROADMAP.md)). La primera iteración 3D |
| Estado | **Notas de WP-D corregidas por el gate de armonía de QA** (ver "Cambios por gate QA" al final), antes de WP-D1. **WP-D1 y WP-D2 implementados** ("Cambios por implementación de WP-D1" y "Cambios por implementación de WP-D2", al final: lo que cambió con las tomas manda sobre el cuerpo). Sin código. Integran la base de arte ([00-deuda-diseno](00-deuda-diseno.md)) con la spec ([01-po-spec](01-po-spec.md), ya corregida por el gate) y el diseño técnico ([02-tech-design](02-tech-design.md)): donde se contradicen, **esta nota dice qué construye WP-D** y qué pide cambiar (§20 y §21) |
| Entrada | [01-po-spec](01-po-spec.md) (los 101 ID, §7 tomas, §11 DV-52 a DV-67, §14 P-01 a P-18, "Cambios por gate QA"); [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) (el gate); [02-tech-design](02-tech-design.md) (§2.2, §2.6 a §2.9, §2.12 a §2.14, §3.2 a §3.9, §4, §5, §7.3 a §7.6, §8.1 y §8.2: los 19 puntos C-01 a C-19); [00-deuda-diseno](00-deuda-diseno.md) (la base; DD6a-01 a DD6a-26); [00-deuda-5](00-deuda-5.md) §5; [ART_BIBLE](../../design/ART_BIBLE.md) §3.2, §5.2, §6, §7.1, §7.2, §7.7, §7.8, §8, §9, §10; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §1, §2.11, §2.12, §5, §6; [ASSET_SLOTS](../../design/ASSET_SLOTS.md) §1.2 a §1.4, §2, §5.2, §8; [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §20; las tomas de la 5 ([CAP-03](../05-series-parciales/snapshots/CAP-03-los-dos-abiertos.png), [CAP-18](../05-series-parciales/snapshots/CAP-18-w1280.png), [CAP-22](../05-series-parciales/snapshots/CAP-22-pausa.png), [CAP-23](../05-series-parciales/snapshots/CAP-23-th-pausa.png)) para la geometría del HUD; `docs/image-03.png` del legacy |
| Legacy leído (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/scene/painters/sky_painter.dart` entero (forma de las nubes, disco, halo, estrellas), `painters/shore_painter.dart:1-140` (pino, cabaña, muelle, tinte y niebla), `painters/water_painter.dart:60-110` (respaldo), `environments/lake_environment.dart:40-86` (cordilleras), `docs/image-03.png`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Verificado | Programas propios en `<scratch>/it6a/designer-notes/` (Python 3.14, sin red): `compose.py` (cámara del contrato en los siete tamaños, cordilleras por ventana, nubes y estrellas del legacy desproyectadas, sol y luna), `options.py` (cuatro combinaciones de arco del sol y alto de las cordilleras, con la hora en que el disco sale entero detrás de la cresta), `palette.py` (la paleta del legacy con `Color.lerp` en `double` y sus derivados por hora), `captures.py` (sol, luna, hitos y bote en cada toma; la toma aérea y el vuelo). Los vectores del generador y el paisaje del lago son los de [00-deuda-diseno §17](00-deuda-diseno.md). Para el gate, otros en `<scratch>/it6a/designer-gate/` (Python 3.14, sin red): `mountains.py` y `macizo.py` (las cordilleras con el alto del legacy y el sol y la luna contra la cresta), `stars.py` (el generador del legacy y las 80 estrellas contra los parches de las sondas), `window.py`, `window2.py`, `hue.py` y `hue18.py` (un modelo del LUT HDR de URP con *Neutral* para la ventana y las sondas de matiz). **Unity no se abrió y dotnet no se corrió** |
| Entrega | Este documento. La lista de Mixamo para la It. 7 (§18, P-09). Los pedidos **DN6a-01 a DN6a-29** (§20; del 25 al 29, del gate) y los cambios de texto de AC que van al PO (§21; las precisiones del gate en §21.4). Las biblias se alinean en WP-D2, después del gate (§22) |

Convenciones: las de la 5 (hex `#RRGGBB`, transparencia `@NN %`, 1 px de USS = 1 dp). Perfiles **V390** 390 × 844 y **H844** 844 × 390 (los del legacy), **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720; `s` = `hudScale`. Mundo como el legacy: `x` lateral (+ derecha), `y` altura sobre el agua, `z` hacia adelante desde la cámara del contrato (m). **Elevación** de un punto del cielo = ángulo sobre el horizonte; **acimut** = respecto del sentido de avance, + a la derecha. **τ** = la tangente "de pantalla" de una dirección, `dir.y / dir.z` en el sistema de la cámara del contrato (con ella, `y_pantalla = horizonte − focal·τ`, como el legacy). `d` = distancia de escena. `S(a; b; x)` = `smoothstep`. Prioridades: **P0** (sin esto la 6a no cumple), **P1** (lo que pide la Mejora EXR), **P2** (deseable; puede pasar a la 6b).

---

## 0. Resumen

1. **Se construye la spec.** De los 19 puntos de [02-tech-design §8.2](02-tech-design.md) acepto la spec en **C-03, C-04, C-05, C-08 a C-16** y sostengo **C-01** (sol a 12°), **C-02** (luz 0 en el cambio de astro), **C-06** (P-11, que es mío: cordilleras con el alto del legacy **y un tope de 9°**), **C-07** (nieve sobre el 58 % del perfil), **C-17** (rolido de 0,15°), **C-18** (los *scrims* de pantalla, que ya estaban en ART_BIBLE §3.2: capa del HUD, DV-67) y **C-19** (hemisferio de la luna). Acepto DT6a-01 a DT6a-15 (§1). El gate los aceptó todos (DV-63 a DV-67; P-18) con las correcciones de "Cambios por gate QA".
2. **El mundo y el telón** (§2): el terreno, los objetos, el agua, el bote, la luz y la dirección del astro son mundo 3D. El degradé del cielo, las estrellas, las nubes y el alto de las cordilleras son un **telón por ventana** armado con las fórmulas de pantalla del legacy. Así cada ventana se compone como el legacy y nada del telón tiene paralaje.
3. **El cielo** (§3) va de `skyHorizon` a `skyTop` con `t = 1 − (1 − q)²` sobre la fracción de cielo visible, mezclado en sRGB como el legacy: los extremos son los del legacy y la luz se junta en el horizonte. Una banda de bruma de 1,5° cose el cielo con el paisaje. En la hora dorada hay un resplandor alrededor del sol. La paleta y sus curvas son las del contrato. La tabla de §3.1 da todos los colores por hora.
4. **Sol, luna y estrellas** (§4):
   - el sol llega a **12°** al mediodía y sale y se pone detrás del paisaje;
   - a las 6 h está detrás del macizo de la izquierda (se ve el resplandor) y sale entero a las 7:31 en horizontal y a las 7:59 en vertical; a las 18 h se pone en el collado de la derecha;
   - el disco mide 1,6° de radio, en HDR con *bloom*; la luna, 1,28° y con su fase;
   - las 80 estrellas son las del legacy, en su píxel, blancas, y entran y salen con un fundido de 0,9 s en lugar de aparecer de golpe (DN6a-05).
5. **Nubes** (§5): las 7 del legacy en su lugar, con la forma de sus cinco manchas y bordes de fBm, encendidas por abajo en la hora dorada. En la vuelta (± 0,1·ancho) se desvanecen y vuelven con un fundido, con la `x` del legacy (DV-56, DN6a-26). El viento sopla hacia la derecha para todo: nubes, copas, juncos y pasto.
6. **Montañas** (§6):
   - el perfil del legacy por acimut, quieto de costado, que se acerca al 4 % de la velocidad;
   - alto: **el del legacy (`0,12·alto de pantalla`) con tope de 9°**: en horizontal, 6,84°, igual que el legacy; en vertical, 9° (con el del legacy, 14,9° en TV, el sol de la mañana quedaba detrás del macizo hasta las 10:49);
   - nieve sobre el 58 % del perfil, con *alpenglow*.
7. **La orilla** (§7): el corte de la base (playa de canto rodado, talud, ladera boscosa a la izquierda, pradera a la derecha) con la envolvente del bosque bajada a `2,2 + 0,20·|x|` para que no tape la nieve en horizontal. Pino, cabaña y muelle con las medidas del legacy. El pasto, los helechos y los juncos son mallas de hojas opacas, sin *alpha test*. El bosque cercano va hasta 120 m y después pasa a una copa de malla. Cada pieza tiene sus LOD y sus distancias.
8. **El agua degradada** (§8): los tres tramos y las 10 líneas del legacy sin moverlos. Encima van tres agregados en el presupuesto de 24 ALU: un reflejo leve del cielo hacia lo lejos, la franja somera y la bruma baja de la mañana. Las líneas se apagan con la visibilidad del astro, así no saltan en el cambio de sol a luna (DN6a-06). La sombra del bote es una pieza aparte.
9. **El bote** (§9) es el casco solo (spec): gelcoat, línea de borda, *cockpit* y bola de proa.
10. **La luz** (§10): un modelo propio normalizado, en el que el tinte del legacy es el promedio y la luz clave modula alrededor. La tabla de cada hora (exposición, balance, saturación, *bloom*, sombras) va con *Neutral*. Las sondas de color son de dos clases: las de contrato, en una pasada sin el ajuste de la hora (los colores de la paleta y la ventana `#FFE082`), y las de aspecto, en la toma (el matiz de la hora).
11. **La cámara** (§11):
    - la del contrato, con la respiración a 0,15°;
    - el vuelo de 1,6 s con la focal que empieza en ×1,25 y no en 50° (en vertical, 50° era un zoom de ×2,38), y con la curva de seno, que deja el pico en 17 m/s en lugar de 32,5;
    - la salida de la spec;
    - la toma aérea con **cabeceo de −4°** y la respiración que se apaga (DN6a-07): con la cámara nivelada, la popa queda en el borde de la zona.
12. **El HUD sobre la escena** (§12): el vidrio `@86 %` y las píldoras `.hud` mantienen los contrastes de la 4b contra el peor fondo, a 1,5 m. La composición se da por perfil (qué queda detrás de cada pieza). Los degradés de pantalla son de la capa del HUD: no van en la escena sola (DV-67). El velo va en el `Volume` de la hora, con su sonda. Con reducir movimiento (§13) se apaga lo que es cámara y sigue lo que es mundo.
13. **Shaders** (§14): 13 propios y 4 *includes*, cada uno con sus parámetros, su costo y sus variantes por nivel. **Presupuesto** (§15): ≈ 83 *draw calls*, ≈ 135 k triángulos y ≈ 10,6 ms de GPU en el nivel Medio; sin teléfono queda NO VERIFICABLE (regla 9).
14. **Tomas** (§16): qué tiene que verse en CAP-01 a CAP-24, DEMO-01 a DEMO-08 y VID-01 a VID-11, con las posiciones en dp. **Mixamo** (§18): Y Bot obligatorio, y ninguna animación para remar. **D-ART-08** (§19): no.

---

## 1. Cómo se integra con la spec y el diseño

### 1.1 Los 19 puntos de 02-tech-design §8.2

| # | Tema | Spec (lo que se construye por defecto) | **Designer, final** | Por qué | ¿Cambia un AC? |
|---|---|---|---|---|---|
| C-01 | Altura del sol | arco 15,5° (17° al mediodía) | **12°** (arco 10,5°) | Con 17°, el sol del mediodía queda detrás de la barra de pasos en TH (y 47,0 contra 54,8), L640 (41,1 contra 48,0) y W1280 (82,3 contra 83,2). Con 12° queda debajo en los cinco perfiles (TH 85,3, con 4 dp tapados solo mientras está el sello). Con el arco de 12° el astro corre a 0,257°/min, que entra en el tope de DT6a-02 | Sí: AC-SCN-10-04, -10-05 y CAP-04, -10, -11 (§21) |
| C-02 | Luz en el cambio de astro | mínimo 0,05 | **0** | Una luz a 6° de un sol ya puesto da sombras de un astro que no está | Sí: AC-SCN-10-05 |
| C-03 | Estrellas al atardecer | aparecen con ambiente ≤ 0,85 | **la spec**, con un fundido de 0,9 s en el tiempo cuando cruzan el umbral (DN6a-05) | En régimen da lo de la spec (CAP-01 y CAP-03 las describen así). El fundido evita que 80 estrellas aparezcan en un cuadro a las 18:00 | No (precisión, §21) |
| C-04 | Posición de las estrellas | la del legacy, ± 1 dp | **la spec** | Es [EXACTO]. Salen la extinción cerca del horizonte de la base y el mapeo por acimut | No |
| C-05 | Posición de las nubes | `x` e `y` del legacy | **la spec** | Lo pide el arnés: CAP-19 necesita una nube detrás de la barra en TV. En horizontal el legacy ya las pone a 17-20° (mi banda era 10,5-18°). En vertical van a 33-37°, entre la barra y las tarjetas, como en el legacy. La banda de la base queda descartada | No |
| C-06 | Alto de las cordilleras (P-11) | ≈ 30 % del cielo visible en las dos orientaciones | **el del legacy (`tan Θ = 0,12·alto/focal`) con tope de 9°** (QA-6a-PAR-03) | Ver §6.2: con el alto del legacy en vertical (14,9° en TV) y el sol a 12°, el disco sale entero recién a las 10:49 y la luna de las 22 h queda apoyada en la cresta (−0,15°). Con el tope, a las 7:59 y con 3,66° de aire. En horizontal, 6,84°: el del legacy | Sí: AC-SCN-19-01 (DV-65; ya aplicado por el PO) |
| C-07 | Nieve | picos sobre el 75 % | **todo lo que pasa el 58 % del perfil**, con borde de ± 0,03 y ruido | Con el perfil fijo, el 75 % deja un solo pico nevado, a −25,4°, que en vertical queda detrás de los paneles. Con el 58 % hay nieve en el macizo y en el pico central, y en horizontal en el flanco del pico de la derecha (45 % del ancho en TV y 33 % en TH) | Sí: AC-SCN-19-01 |
| C-08 | Resolución de la hora real | minuto | **la spec**, con una interpolación de 1 s lineal en el cambio de minuto (DN6a-10, P2) | El astro salta hasta 1,9 dp por minuto en TV; con 1 s de transición no se ve. Si no entra, se acepta el salto | No |
| C-09 | Transición de `Hora escena` | 1,5 s fijos; en el acto con reducir movimiento | **la spec** | Más simple; se ve el time-lapse | No |
| C-10 | Escena demorada | fundido de 0,6 s | **la spec** | — | No |
| C-11 | Salida | +1,5 m y −4 m en 1,2 s (`ease.move`) desde el cuadro 0 | **la spec** | Hasta los 300 ms es apenas un indicio: el remero baja ≈ 4 dp en TV y achica 2,6 %. Después lo cubre el negro. Alcanza | No |
| C-12 | Toma aérea | en la 6a | **la spec**, con cabeceo de −4° y la respiración que se apaga (DN6a-07) | Nivelada, la popa termina en y 588,7 de TV (la zona termina en 598) y en 395,7 de TH (de 411). Con −4°, en 552,9 y 359,9 | No (el AC no pide nivelada) |
| C-13 | Bote provisorio | el casco solo | **la spec** | P-05; los *riggers* y los remos llegan con el remero | No |
| C-14 | Brillo del agua | las 10 líneas del legacy | **la spec** + los agregados de §8, con las líneas por la visibilidad del astro (DN6a-06) | El camino con destellos es del agua avanzada (6b) | Precisión de AC-SCN-13-03 (§21) |
| C-15 | Costo del agua | ≤ 12 ALU | **≤ 24 ALU** (DT6a-07) | §8.1: lo de la spec cuesta ≈ 15 y los agregados ≈ 9 | Sí: AC-SCN-13-04 |
| C-16 | Juncos | ≤ 1,2 m en `\|x\| < 18` | **la spec** (DT6a-09) | — | No |
| C-17 | Rolido | 0,18° | **0,15°** (DT6a-01) | Con 0,18° el horizonte se aparta 3,267 dp en W1280; con 0,15°, 2,932 | Sí: AC-SCN-02-04 |
| C-18 | Degradés de pantalla | (la spec no los nombra) | **se suman, como capa del HUD** (DV-67) | Ya están en ART_BIBLE §3.2 ("scrims de pantalla", detrás del HUD y delante de la escena), así que no son nuevos. Los dibuja la cámara de la escena al final, con 0 elementos de UI, pero se prenden y se apagan con el HUD: **no van en la escena sola** (QA-6a-PAR-02; §12.3) | DV-67 (PO, en el gate) |
| C-19 | Hemisferio de la luna | (no lo nombra) | **se suma** | En Argentina la luna gibosa menguante tiene la luz del otro lado | Precisión de DV-57 |

### 1.2 Los desvíos del Architect (02-tech-design §8.1)

| ID | Posición |
|---|---|
| DT6a-01 | De acuerdo (es mi R6a-05) |
| DT6a-02 | De acuerdo, con el tope en **0,3°/min**: con el arco de 12° se mide 0,257°/min, y el sol real se mueve a 0,25°/min |
| DT6a-03, -04, -05, -11, -12, -13, -15 | Técnicos; sin objeciones de arte. DT6a-05 (la escena por pedido y compuesta en el panel) es lo que garantiza que el vidrio dé `#2A2F37` sobre blanco: lo necesito |
| DT6a-06 | De acuerdo. Un fundido de entrada de 200 del HUD en el *layout* nuevo se ve mejor que un fundido cruzado con la foto de otra orientación. Solo en un cambio de clase |
| DT6a-07 | De acuerdo; la receta de 24 ALU está en §8.1 |
| DT6a-08 | De acuerdo: la cabaña con el ancla fija, profundidad de 4, 3 o 2,5 m hacia `+z` y, si todavía choca, la poda del pino. Es mejor que mi corrimiento |
| DT6a-09 | De acuerdo: juncos de 0,6 a 1,2 m en `\|x\| < 18` |
| DT6a-10 | De acuerdo: el velo compuesto en Core con la luz de la hora, en un solo `Volume` |
| DT6a-14 | De acuerdo: sin impostores en la 6a; el LOD2 de 120 triángulos llega a 400 m |

### 1.3 Lo que esta nota cambia de la base (00-deuda-diseno)

Manda esta nota. La base queda como registro.

| Base | Ahora | Dónde |
|---|---|---|
| §4.2 degradé por elevación hasta 24° | por fracción de cielo visible, `t = 1 − (1 − q)²` (el telón) | §3.3 |
| §4.4 estrellas por acimut y elevación propios, extinción con `S(2°; 8°)` | las del legacy en su píxel, sin extinción; fundido de 0,9 s en el umbral | §4.5 |
| §4.5 nubes en una banda de 10,5° a 18° | las del legacy en su píxel; núcleo con el alfa del legacy (sale el ×1,5) | §5 |
| §4.7 transición `clamp(0,25 s·\|Δh\|; 0,6; 2)` | 1,5 s (spec) | §3.4 |
| §5 cordilleras a 9° fijos; nieve sobre 0,55 | el alto del legacy (12 % del alto de pantalla) con tope de 9°; nieve sobre 0,58 | §6 |
| §6.2 envolvente del bosque `2,2 + 0,25·\|x\|` | `2,2 + 0,20·\|x\|` | §7.1 |
| §3.3 salida de 640 con 0,4 m arriba y 1,2 m atrás | la de la spec (1,5 m y 4 m en 1,2 s) | §11.4 |
| §3.3 toma aérea en la 6b | en la 6a, con cabeceo de −4° | §11.5 |
| §3.3 vuelo con FOV de 50° al empezar y `ease.move` | focal del contrato ×1,25 al empezar; posición y orientación con `ease-in-out-sine` | §11.3 |
| §7 agua con camino de brillo y destellos, sin las líneas | las 10 líneas del legacy + los agregados; el camino es de la 6b | §8 |
| §8 casco, *riggers*, carro y remos en reposo | el casco solo | §9 |
| §9.4 fundido de 240 de la escena demorada | 0,6 s (spec) | §11.6 |
| §6.4 bosque cercano en `\|x\|` 18-60 con disco de Poisson de 4,5 m | árboles sueltos hasta `z` 120 y `\|x\|` 45, Poisson de 6 m; más allá, la copa de malla | §7.4 |
| §6.4 pasto en tarjetas cruzadas | mallas de hojas opacas | §7.4 |
| §4.6 sonda "el cielo a 30° = `#0B4FA8`" | sondas de contrato (arriba y a media altura, sin el ajuste de la hora) y de matiz (en la toma) | §10.4 |
| §3.5 *scrims* en la escena | capa del HUD: no van en la escena sola (DV-67) | §12.3 |
| §10 presupuesto | reconciliado con 02-tech-design §4.1 | §15 |

QA-V5-R2-01 (la deuda de diseño de la 5) quedó decidida en [00-deuda-diseno §1](00-deuda-diseno.md), opción (a), sin cambios desde entonces.

---

## 2. El mundo y el telón

La escena del legacy es 2,5D: todo lo del cielo se dibuja en coordenadas de pantalla. Una escena 3D con la cámara del contrato no puede poner el mismo objeto en el mismo píxel en vertical y en horizontal. Por eso hay dos clases de cosas:

| Clase | Qué | Regla | Con la respiración | Al cambiar de ventana |
|---|---|---|---|---|
| **Mundo** | terreno, objetos del legacy y agregados, agua, bote, luz clave y sombras, la dirección del astro (acimut y elevación del disco en grados) | coordenadas de mundo en metros; la cámara del contrato | se mueven en pantalla con la cámara | nada cambia en el mundo; la cámara toma el horizonte y la focal nuevos |
| **Telón** | el degradé del cielo, la bruma del horizonte, las 80 estrellas, las 7 nubes, el alto de las cordilleras (Θ por ventana; el perfil es un anillo de elevación por acimut, §6.2) | las fórmulas de pantalla del legacy, **desproyectadas con la cámara del contrato** a direcciones del cielo (02-tech-design §2.8) o escaladas con `τ` | puntos en el infinito: giran con la cámara y no se trasladan | se rehacen en el cuadro del cambio, con el HUD entrando en 200 (DT6a-06) |

- **El disco del astro es mundo**: su acimut ubica la luz y las sombras, y las líneas del agua van debajo (DV-59). Su elevación es fija en grados (arco de 12°, §4.1). Por eso su lugar de pantalla sí cambia con la ventana, como en cualquier cámara.
- **Por qué el telón**: es la manera de que CAP-01 a CAP-07 se comparen con LEG-01 a LEG-07 en cada tamaño (estrellas ± 1 dp, nubes en su `y`, montañas con el alto del legacy) sin que haya paralaje en el cielo. El legacy ya se comportaba así.

---

## 3. Cielo por hora

### 3.1 La paleta y sus derivados (contrato; `palette.py`)

La paleta es la del legacy, interpolada como `Color.lerp` (canales en `double`, sin redondear). Los hex de la tabla son esos valores redondeados para mostrarlos. **Los derivados se calculan en Core en cada cuadro con la misma paleta** (AC-SCN-09-04: una sola paleta manda).

**(a) Cielo y agua**

| Hora | ambient | `skyTop` | `skyHorizon` | bruma (`lerp(skyHorizon; fog; 0,5)`) | `fog` | `waterNear` | `waterFar` | agua arriba (`lerp(waterFar; fog; 0,5)`) |
|---|---|---|---|---|---|---|---|---|
| 0:00 a 4:00 | 0,350 | `#050A1E` | `#121C3A` | `#101A35` | `#0F1730` | `#06122A` | `#0E1E3C` | `#0E1A36` |
| 5:00 | 0,700 | `#2A1B5E` | `#FF9A5C` | `#E4926B` | `#C98A7A` | `#1A2B4A` | `#3A2F55` | `#825C68` |
| **6:00** | 0,825 | `#324B9A` | `#FFBA7E` | `#E9B99B` | `#D2B9B8` | `#1C4274` | `#4B5F8F` | `#8F8CA3` |
| 7:00 | 0,950 | `#3A7BD5` | `#FFD9A0` | `#EEE0CB` | `#DCE8F5` | `#1E5A9E` | `#5C8FC9` | `#9CBCDF` |
| 9:00 | 0,970 | `#2769C3` | `#D2D7C6` | `#D4E0E0` | `#D7E8F9` | `#185698` | `#4A8FCE` | `#90BCE4` |
| **12:00** | 1,000 | `#0B4FA8` | `#8FD3FF` | `#AFDEFF` | `#CFE9FF` | `#0E4F8F` | `#2F8FD6` | `#7FBCEB` |
| 15:00 | 0,925 | `#1C469A` | `#C7B0AC` | `#CFBAB8` | `#D8C5C4` | `#12447A` | `#5676A8` | `#969EB6` |
| **18:00** | 0,850 | `#2E3C8C` | `#FF8C5A` | `#F09772` | `#E0A28A` | `#163A66` | `#7C5C7A` | `#AE7F82` |
| 19:00 | 0,675 | `#1E2B6B` | `#A4636A` | `#96676E` | `#886A72` | `#102C50` | `#534368` | `#6E576D` |
| 19:34 | 0,575 | `#142158` | `#714C73` | `#634B6C` | `#564B64` | `#0D2443` | `#3C355D` | `#494061` |
| 20:00 | 0,500 | `#0D1A4A` | `#4A3A7A` | `#3D376A` | `#30335A` | `#0B1E3A` | `#2A2A55` | `#2D2F58` |
| 21:00 | 0,425 | `#091234` | `#2E2B5A` | `#272850` | `#202545` | `#081832` | `#1C2448` | `#1E2447` |
| **22:00** | 0,350 | `#050A1E` | `#121C3A` | `#101A35` | `#0F1730` | `#06122A` | `#0E1E3C` | `#0E1A36` |

**(b) Astro, luz, nubes, montañas y niebla baja**

| Hora | `sunColor` / `sunGlow` | Disco: acimut, elevación (vis) | Luz: elevación, intensidad | Fuerza del legacy | Nube: cuerpo, alfa | Cordillera lejana / cercana / nieve | Estrellas (oscuridad) | Bruma baja `m` | Dorado `g` |
|---|---|---|---|---|---|---|---|---|---|
| 0:00 | `#E6EEFF` / `#A6B8FF` | luna 0,0°, 12,0° (1) | 50°, 0,153 | 0,45 | `#C4C6CE` 0,14 | `#222D46` / `#203235` / `#AEB3BD` | 1,00 | 0 | 0 |
| 4:00 | ídem | luna +11,7°, +1,0° (0,38) | 30°, 0,058 | 0,45 | ídem | ídem | 1,00 | 0 | 0 |
| 5:00 | `#FFB070` / `#FF7A3D` | sol −20,4°, −0,4° (0,54) | 6°, 0,30 | 0,20 | `#FFE6D6` 0,32 | `#AE837E` / `#776858` / `#E6D6D4` | 0,46 | 0 | 0,54 |
| **6:00** | `#FFCA88` / `#FF9F54` | sol −17,5°, +1,5° (1) | 6°, 0,90 | 0,20 | `#FFEEDF` 0,42 | `#B5A6AC` / `#7C7E75` / `#E9E4E6` | 0,27 | 0,55 | 1 |
| 7:00 | `#FFE4A0` / `#FFC46B` | sol −14,6°, +4,2° (1) | 18,7°, 1,25 | 0,31 | `#FFF6E7` 0,53 | `#BCCADA` / `#809492` / `#EBF2F8` | 0 | 0,55 | 0,70 |
| 9:00 | `#FFEBB5` / `#FFD383` | sol −8,75°, +8,9° (1) | 40,6°, 1,45 | 0,85 | `#F4F5F1` 0,55 | `#B8CADD` / `#7E9494` / `#EAF2FA` | 0 | 0,06 | ≈ 0 |
| **12:00** | `#FFF6D5` / `#FFE9A6` | sol 0,0°, +12,0° (1) | 55°, 1,75 | 1,00 | `#E3F4FF` 0,58 | `#B2CAE2` / `#7A9597` / `#E7F2FB` | 0 | 0 | 0 |
| 15:00 | `#FFD4A0` / `#FFAA72` | sol +8,75°, +8,9° (1) | 40,6°, 1,30 | 0,85 | `#F1EBEA` 0,51 | `#B9B0B6` / `#7E847B` / `#EAE7EA` | 0 | 0 | ≈ 0 |
| **18:00** | `#FFB36B` / `#FF6B3D` | sol +17,5°, +1,5° (1) | 6°, 0,85 | 0,20 | `#FFE2D6` 0,44 | `#BF958A` / `#827360` / `#EDDDD8` | 0,23 | 0 | 1 |
| 19:00 | `#EECCB5` / `#C7879E` | sol +20,4°, −0,4° (0,54) | 6°, 0,46 | 0,20 | `#E8D8DA` 0,31 | `#7D6B78` / `#595954` / `#D2CCD1` | 0,50 | 0 | 0,54 |
| 19:34 | `#E4DBDF` / `#A797D5` | los dos a −2,5° (0) | — , **0** | **0,20** (todavía de día; 0,45 desde las 19:35) | `#DBD2DC` 0,25 | `#57546E` / `#414A4E` / `#C3C3CD` | 0,66 | 0 | 0 |
| 20:00 | `#DDE6FF` / `#8FA3FF` | luna −11,7°, +1,0° (0,38) | 30°, 0,058 | 0,45 | `#D2CEDE` 0,21 | `#3B4266` / `#2F3F49` / `#B8BCCA` | 0,77 | 0 | 0 |
| 21:00 | `#E2EAFF` / `#9BAEFF` | luna −8,75°, +8,9° (1) | 38,3°, 0,153 | 0,45 | `#CBCAD6` 0,17 | `#2F3756` / `#28393F` / `#B3B7C4` | 0,88 | 0 | 0 |
| **22:00** | `#E6EEFF` / `#A6B8FF` | luna −5,83°, +10,6° (1) | 44,6°, 0,153 | 0,45 | `#C4C6CE` 0,14 | `#222D46` / `#203235` / `#AEB3BD` | 1,00 | 0 | 0 |

- La intensidad de la luna se calcula con la iluminación de la fecha de prueba (≈ 0,94): `(0,04 + 0,12·0,94)·vis`.
- Fuerza del legacy (`time_of_day.dart:64`): de noche, 0,45; de día, `clamp(1,2·e; 0,2; 1)`. La noche del legacy empieza a las 19:34:19 y, con la hora al minuto, a las **19:35** (`isNight(19:34)` es falso): a las 19:34 la fuerza es 0,20 (QA-6a-PAR-07). La columna es ilustrativa: las pruebas la toman del oráculo.
- Nube: el cuerpo es `lerp(blanco; skyHorizon; 0,25)` con alfa `0,08 + 0,5·a²` (contrato).
- Cordilleras: `lerp(tono; fog; 0,35 + 0,4·profundidad)`, con lejana `#5C6E8A` (profundidad 1) y cercana `#2F4A3A` (0,3). La nieve es `lerp(#F2F6FA; fog; 0,3)`. Todo antes de la luz y del post.
- En negrita, las horas de la demo y de CAP-01 a CAP-04.

### 3.2 Las curvas de transición

Todo es función de la hora visible (la real o la del time-lapse, §3.4) y de nada más. Las funciones son puras, de Core.

| Magnitud | Curva | Dueño |
|---|---|---|
| Paleta (8 campos) | lineal entre los keyframes 5, 7, 12, 18, 20, 22, 28 (= 4) y 29 (= 5); noche cerrada de 22 a 4 h (contrato) | `SceneTimeOfDay` |
| Post (exposición, balance, saturación, contraste, *bloom*) y sombras | lineal entre los mismos keyframes (§10.2) | `HourLighting` |
| Visibilidad del sol | `S(4,45; 5,50; h)·(1 − S(18,50; 19,55; h))` | `CelestialPlacement` |
| Visibilidad de la luna | `S(19,5719; 20,6; h)` a la tarde; `S(4,4281; 3,40; h)` a la mañana | ídem |
| Elevación del disco | `−2,5° + (1,5° + 10,5°·e + 2,5°)·vis` | ídem |
| Intensidad de la luz | sol `K(h)·vis` con K = 0,55 (≤ 5 h), 1,25 (7 h), 1,75 (12 h), 0,85 (≥ 18 h), lineal entre ellos; luna `(0,04 + 0,12·iluminación)·vis`; **0 en el cambio** | ídem |
| Dorado `g` (resplandor, nubes, nieve, brillo) | `(1 − S(1,5°; 9°; elevación del sol))·vis_sol`; 0 de noche | `HourLighting` |
| Estrellas | `oscuridad = clamp((1 − a)/0,65)`, 0 con a > 0,85 (spec); en el tiempo, un fundido de 0,9 s `ease.move` cuando cambia el estado (DN6a-05) | `StarField` + fundido |
| Luces de la noche | a < 0,6 (19:26 a 4:43), cada luz en su minuto dentro de ± 10 min, fundido de 0,4 s (DV-60) | `NightLights` |
| Bruma baja `m(h)` | 0 hasta las 5 h; `0,55·S(5; 6; h)`; 0,55 de 6 a 7 h; `0,55·(1 − S(7; 9,5; h))`; 0 desde las 9:30 | `HourLighting.LowMist` |

### 3.3 El degradé del cielo, la bruma del horizonte y el resplandor (shader `Sky`)

Para una dirección de vista con `τ` (§ convenciones) y `τ_top = horizonte/focal` del contrato de la ventana (TV 0,889: 41,7°; TH 0,420: 22,8°):

1. **Fracción de cielo**: `q = clamp(τ/τ_top; 0; 1)`.
2. **Degradé**: `cielo = lerp(skyHorizon; skyTop; 1 − (1 − q)²)`.
   - Es `SkyBackdrop.Gradient(q)` de Core (QA-6a-PAR-10), copiado en el *shader*.
   - **Se interpola en sRGB**, como `Color.lerp` y el degradé del legacy (los canales de 0 a 1, sin linealizar), y el resultado pasa a lineal al final del paso 3 (`SRGBToLinear`). Interpolado en lineal, la mitad del cielo de las 12 h daría `#4B82C2` en lugar de `#2C70BE`.
   - En el borde de arriba es `skyTop` y en el horizonte `skyHorizon`, como el legacy (AC-SCN-11-02).
   - La luz se junta abajo: con `q` = 0,25 va al 44 % y con 0,5 al 75 %.
   - En TV la mitad de arriba es casi `skyTop`: el fondo más oscuro queda detrás de las tarjetas.
3. **Bruma del horizonte**: `bruma = lerp(skyHorizon; fog; 0,5)`; `cielo = lerp(bruma; cielo; S(0; tan 1,5°; τ))`. Debajo del horizonte (lo que no tapa nada) va `bruma`.
   - El agua lejana (agua arriba) y el terreno lejano (que la niebla extendida lleva a `fog`) llegan a tonos vecinos. El cielo, el paisaje y el agua se unen sin costura (AC-SCN-11-02).
4. **Resplandor de la hora dorada**: `+ sunGlow·0,35·g·exp(−Δ/14°)·(1 − S(0; tan 20°; τ))`, con Δ la distancia angular al disco, **aunque esté detrás del paisaje**. Es lo que se ve a las 6 h sobre el macizo y a las 19 h sobre el collado.
5. **Halo**: `sunGlow` con los alfas del legacy (0,7 en el centro, 0,25 a la mitad y 0 en el borde, lineal entre ellos) hasta **6,4°** (4 veces el disco), × `vis`. Para la luna, hasta 5,1° y × `(0,4 + 0,6·iluminación)·vis`.
6. **Disco**: §4.2.
7. **Fondo de estrellas** (solo Alto, P-10): §4.5.
8. **Tramado** (DN6a-29; QA-G6aT-12): `± 0,5/255` con *Bayer* 4 × 4 fijo en la pantalla, al final, contra el bandeado de la noche (de `#050A1E` a `#121C3A` hay ≈ 13 niveles por canal en 337 dp). Sin tiempo: las tomas se repiten byte a byte. El *dithering* de la cámara de URP queda apagado (rota su ruido en cada cuadro).

Los pasos 2 y 3 (el telón y la bruma) se mezclan en sRGB; el resplandor, el halo (mezclado con los alfas del legacy) y el disco se componen después, en lineal. El tramado va último.

Costo: ≤ 41 ALU (35 más 3 de pasar a lineal y 3 del tramado), 1 textura (la de la luna, solo dentro del disco), 1 *draw call* (un triángulo de pantalla en la cola 1000).

### 3.4 Las transiciones de hora

| Caso | Qué pasa |
|---|---|
| Hora real | La de minuto (spec), armada como el legacy (`hora + minuto/60`). En el cambio de minuto, la hora visible va de la vieja a la nueva en **1 s lineal** (DN6a-10, P2): el sol corre como mucho 0,26° (1,9 dp en TV) y así no salta. Si el pedido no entra, el salto se acepta |
| `Hora escena` (−, +, `Hora real`) | 1,5 s con `ease.move`, por el camino más corto del reloj (spec). Todo pasa por las horas intermedias, como un time-lapse: la paleta, el astro, la luz, las estrellas (con su fundido), las luces de a una, la bruma. De 22 a 10 h (DEMO-08) va hacia adelante y pasa por la noche y el amanecer en 1,5 s |
| Con reducir movimiento | `Hora escena` en el acto (spec); el minuto real, en el acto |
| Al abrir, al volver del segundo plano, `ForceHourForTest` | en el acto, y los fundidos (estrellas, luces) se ponen en su valor final en ese cuadro, sin animar |

---

## 4. Sol, luna y estrellas

### 4.1 Dónde

- **Acimut**: el del legacy, `(xFrac − 0,5)·70°`: −17,5° a las 6 h, 0° a las 12 h, +17,5° a las 18 h. La luna usa la hora espejada.
- **Elevación del disco**: `1,5° + 10,5°·e` antes de ponerse: 12° al mediodía y a medianoche. Sale y se pone desde −2,5°, siempre detrás del paisaje (§3.2).
- **Velocidad**: el disco nunca se corre más de **0,3°/min**; lo más rápido medido es 0,257°/min (DT6a-02).
- **Luz**: viene del mismo acimut, con su propia elevación: sol `6° + 49°·e` (55° al mediodía) y luna `10° + 40°·e(h')` (D-ART-05). Las sombras caen hacia la cámara y del lado opuesto al astro. La luz no la tapa la cordillera (§4.4).

Posiciones en las tomas (`captures.py`; `(x; y)` del centro del disco en dp, radio en dp):

| Hora | V390 | H844 | TV | TH | W1280 |
|---|---|---|---|---|---|
| 12:00 sol (r 10,9 / 10,9 / 11,5 / 11,5 / 20,1) | (195,0; 254,7) | (422,0; 80,9) | (205,5; 278,2) | (457,0; 85,3) | (640,0; 149,4) |
| 18:00 sol | (318,0; 326,9) | (545,0; 153,1) | (335,1; 354,3) | (586,6; 161,3) | (867,0; 282,6) |
| 22:00 luna (r 8,7 / 8,7 / 9,2 / 9,2 / 16,1) | (155,2; 264,3) | (382,2; 90,5) | (163,5; 288,3) | (415,0; 95,4) | (566,4; 167,0) |
| 0:00 luna | — | — | (205,5; 278,2) | — | — |
| 15:00 sol | — | — | (268,8; 300,3) | — | — |
| 10:00 sol (CAP-13 a -17) | — | — | (163,5; 288,3) | (415,0; 95,4) | (566,4; 167,0) |

### 4.2 Cómo se ve el sol

- **Disco**: radio **1,6°**, con borde suave de 0,12° y un oscurecimiento al limbo de 1 → 0,85.
  - Color `sunColor × (3 + 5·e)·vis` en HDR (Alto y Medio): el *bloom* lo agranda. A pleno sol se lee como un disco de ≈ 40 dp en TV (el núcleo de 23 dp más el *bloom*).
  - En Bajo (sin HDR ni *bloom*), `sunColor` pleno; el halo hace el brillo.
- **Por qué no 22 dp de radio en V390** (AC-SCN-11-05): el legacy lo dibuja plano y sin *bloom*. En 3D, con un núcleo de 44 dp más el *bloom*, queda un sol de dibujo animado. El AC está marcado [VISUAL], y las proporciones (el halo, 4 veces el disco) se conservan (§21).
- **Detrás de las nubes**: las nubes pasan delante del disco y lo atenúan con su alfa (AC-SCN-11-01).

### 4.3 La luna y su fase (DV-57, C-19)

- **Disco**: radio **1,28°** (0,8 del sol), color `sunColor × 1,6·vis` en HDR (Alto y Medio).
- **Textura**: albedo del slot `sky.moon`, 256² procedural, con cuatro "mares" (manchas de 0,25 a 0,45 de luminancia) y cráteres por ruido. Va multiplicada por el color.
- **Fase**:
  - normal del disco `n = (u; v; √(1 − u² − v²))` y luz `L = (sin φ; 0; −cos φ)`, con `φ = 2π·edad/29,530588853`;
  - lo iluminado es `S(−0,06; 0,06; n·L)`;
  - **en el hemisferio sur, `u → −u`** (C-19, región del sistema).
- **Lado oscuro** (luz cenicienta): `0,06·sunColor` sumado sobre el cielo, así el disco se ubica también en luna nueva.
- **Halo**: §3.3, × `(0,4 + 0,6·iluminación)`.
- **Con la fecha de prueba** (28 de octubre de 2026): gibosa menguante, ≈ 94 %. En el norte la parte oscura queda a la derecha; con región del sur, a la izquierda.

### 4.4 Detrás del paisaje

Orden de dibujo (AC-SCN-11-01): degradé → estrellas → disco y halo → nubes → cordilleras → todo lo demás. Con el arco de 12° y las cordilleras de §6:

| Momento | Qué se ve |
|---|---|
| **6:00** | El sol a −17,5° está detrás del macizo de la izquierda (cresta a 6,2° en vertical y a 4,7° en horizontal). Arriba de la cresta se ve el resplandor dorado y el borde del halo. Los pinos de la orilla izquierda (a ≈ 40 m en ese acimut) pasan por delante |
| Sale | el borde de arriba asoma sobre la cresta a las **6:28** en horizontal y a las **6:53** en vertical; el disco entero, a las **7:31** y a las **7:59** |
| 12:00 | Libre: la cresta central está a 5,8° (vertical) o 4,4° (horizontal) y el disco empieza a 10,4° |
| **18:00** | El sol en el **collado** de la derecha (la cresta por debajo de 0,5° de +13° a +20°; las dos cordilleras en 0 de +14,6° a +18,2°). Lo único que tiene abajo es la pradera, a 0,4-1,1°, así que se ve entero, apoyado en el horizonte. Los pinos y la cabaña de la orilla derecha pueden pasar por delante: es el sol entre los árboles |
| 19:00 | El centro del disco ya está debajo del horizonte (−0,4°, vis 0,54): sobre la cercana (0,7° en TH) apenas asoma el borde de arriba, con el resplandor encima |
| 19:34 | El sol y la luna a −2,5°: el cambio no se ve y la luz vale 0 |
| 20:00 a 20:25 | La luna sale por la izquierda (acimut −11,7°) detrás de la cresta (3,9° en horizontal, 5,1° en vertical): primero el halo; el borde asoma a las 20:07 en horizontal y a las 20:12 en vertical, y el disco está entero a las 20:17 y a las 20:23 |
| 22:00 | La luna a 10,6°, con 3,7° de aire sobre la cresta en vertical y 5,0° en horizontal |

- **La luz no se tapa con la cordillera.** El alto de las cordilleras depende de la ventana (§6.2): si las montañas taparan la luz, la luz de la escena cambiaría al rotar. A las 6 h la luz es dorada y rasante aunque el disco esté detrás del macizo (CAP-01 lo pide así).
- **Borde encendido** (DN6a-12, P2): mientras el disco está detrás de la cresta en su acimut, los últimos 0,35° de la cresta en ± 8° del sol suman `sunGlow·0,6·g·exp(−(Δacimut/6°)²)`. Es el contraluz de las montañas al amanecer.

### 4.5 Estrellas (Q-62, P-10)

- **Las 80 del legacy**: `Random(42)`, tres tiradas por estrella (x, y, radio). Se desproyectan con la cámara del contrato (02-tech-design §2.8).
  - En vertical cubren de 7,6° a 41,7°; en horizontal, de 3,6° a 22,8°.
  - Radio de 0,6 a 1,6 dp; en pantalla es un disco suave de ese radio con 0,5 dp de borde.
  - **Blancas**, como el legacy (`Colors.white`, `sky_painter.dart:66`): de la paleta sale solo su opacidad (AC-SCN-09-04).
- **Brillo** (spec): `(0,4 + 0,5·(0,5 + 0,5·sin(2t + i)))·oscuridad`, 0 con ambiente > 0,85. Titilan también en pausa.
- **Fundido** (DN6a-05): cuando el conjunto pasa de visible a invisible (o al revés), su alfa sigue un fundido de 0,9 s (`dur.scene`, `ease.move`). En régimen no cambia nada; a las 18:00 entran en 0,9 s en lugar de un cuadro. Con reducir movimiento sigue (es un fundido).
- **Tapadas** (AC-SCN-11-01; QA-G6aT-08): el disco y el halo van en `Sky` (cola 1000), debajo de las estrellas (1001). Por eso cada estrella lleva en su alfa de Core `× (1 − alfa del halo en su dirección)` y **0 adentro del disco** (del sol o de la luna, también en su lado oscuro). Es lo que hace el legacy al pintar el astro encima de ellas, sin pasada ni ALU de más (DN6a-28). Las nubes (1002) y el paisaje (1003) las tapan por orden. Si el Architect prefiere la pasada propia del astro (estrellas 1001, astro 1002, nubes 1003, horizonte 1004), esto no hace falta y el disco y el halo salen de `Sky` a esa pasada (1 *draw call* más).
- **Fondo tenue** (P-10, solo Alto):
  - un *hash* por celda de 3° × 3° en (acimut; τ) con una estrella por celda;
  - radio de 0,5 a 0,9 dp, alfa de 0,12 a 0,30 × el brillo de las 80;
  - titileo con fase propia; solo con `τ > tan 3°`;
  - nunca más brillante que las 80 (su máximo, 0,30, contra 0,40 de la más tenue de las 80).

---

## 5. Nubes y viento

### 5.1 Las 7 del legacy (contrato)

- **Posición** (spec, D-ART-06): `x = ((x₀ + t·0,004·v) mod 1,2)·ancho − 0,1·ancho` e `y = horizonte·(0,12 + y_rel·0,6)`, desproyectadas.
  - En vertical quedan a 33,6-37,1° (TV, entre la barra y las tarjetas); en horizontal, a 17,4-19,6° (TH, en buena parte detrás de la barra, como en `image-03` del legacy).
  - Solo el viento las mueve, con el tiempo de escena; siguen en pausa.
- **Tamaño**: el del legacy en dp, `s` de 22 a 40 dp. Ocupan de `x − 1,3·s` a `x + 1,3·s` (la base, de 2,6·s de ancho, `sky_painter.dart:97`) y de `y − 0,8·s` a `y + 0,65·s`. En ángulo, `s/focal` rad: en W1280 son más chicas que en TV, como en el legacy.
- **La vuelta** (DV-56; QA-6a-PAR-08; DN6a-26). El legacy las envuelve en ± 0,1·ancho: en V390, TV y P360 las de 34, 36 y 40 dp (en P360 también la de 30) todavía asoman en el salto (0,1·ancho = 36 a 41 dp contra `1,3·s` = 39 a 52 dp) y un pedazo desaparece de un lado y aparece del otro en un cuadro. Acá **la `x` es la del legacy y cambia solo el alfa**: con la fase `φ = (x₀ + t·0,004·v) mod 1,2` (`x = φ·ancho − 0,1·ancho`), el cuerpo y la sombra se multiplican por
  `V(φ) = S(0; 1,3·s/ancho; φ)·(1 − S(1,2 − 1,3·s/ancho; 1,2; φ))`.
  - La nube se apaga en su último `1,3·s` de recorrido antes del salto (mientras se ve lo que queda de su lado izquierdo) y se prende en su primer `1,3·s` del otro lado. En el salto vale 0 de los dos lados.
  - Se nota como una nube que se disipa en el borde, en 25 a 42 s (la de 40 dp en V390, 41,7 s). En los horizontales y en W1280 (0,1·ancho ≥ 64 dp, más que `1,3·40`) la vuelta pasa fuera del cuadro y `V` no se ve.
  - Lo da Core en el alfa de cada nube (`CloudModel`); el dato del legacy (la `x`, `0,08 + 0,5·a²`) no cambia. Entre dos cuadros de 1/60 s el alfa cambia ≤ 0,01.

### 5.2 Forma (slot `sky.clouds`, horneado al precargar)

- **Atlas** de 512 × 256 (RG8): cuatro celdas de 256 × 128, una variante por celda; la nube `i` usa la variante `i mod 4`.
- **Cada variante** es la estructura de manchas del legacy (`sky_painter.dart:97-101`):
  - la base, una elipse de 2,6·s × 0,8·s centrada en `y + 0,25·s`;
  - tres copos, de radio 0,5·s, 0,65·s y 0,45·s, en `(x − 0,6·s; y)`, `(x; y − 0,15·s)` y `(x + 0,7·s; y + 0,05·s)`;
  - la sombra, una elipse de 2,0·s × 0,3·s en `y + 0,5·s`.
- **Cómo se hornea**: cada mancha opaca hasta el 55 % de su radio. El borde se come con fBm de 3 octavas (amplitud 0,18 del radio, una semilla por variante) y en los copos se suman 2 o 3 sub-copos de 0,25-0,35·s, así no son círculos.
- **Canales**: R = densidad; G = "altura" dentro de la nube (1 arriba, 0 abajo), para la luz.

### 5.3 Color (shader `Clouds`)

- **Cuerpo**: `lerp(blanco; skyHorizon; 0,25)` con el alfa del legacy, `0,08 + 0,5·a²` × la densidad. El máximo nunca pasa el alfa del legacy (sale el ×1,5 de la base).
- **Luz interna**:
  - arriba (G > 0,6), `+0,10·sunColor` de día;
  - el lado del sol, `+0,12·sunColor·max(0; cos Δacimut)`;
  - el 40 % de abajo, `lerp(cuerpo; sunGlow; 0,45·g)`: se encienden por abajo en la hora dorada.
- **Sombra** del legacy: `skyTop` al `0,12·a` en la elipse de abajo.
- **De noche** quedan casi invisibles (alfa 0,14), con un borde plateado de `0,15·sunColor·vis_luna` si la luna está a menos de 25°.

### 5.4 La capa lejana (solo Alto, P2)

- **Qué es**: una tira de nubes delgadas entre **τ = tan 2° y tan 9°**, delante de las estrellas y detrás de las 7 y de las cordilleras, con cobertura 0,35.
- **Textura y movimiento**: fBm de 1024 × 512 (R8) horneado al precargar (segunda textura de `sky.clouds`), periódico en el acimut (sin costura al dar la vuelta). Deriva con el viento a la mitad de la velocidad de las 7.
- **Color y alfa**: el alfa es el de la nube × 0,6; los colores, los de §5.3.
- Tapa el sol de 18 h a ratos (eso se ve bien), pero nunca el del mediodía, que está a 12°.
- En vertical se ve sobre todo en el collado; en horizontal, sobre las montañas. Es lo que llena el cielo de W1280, donde las 7 quedan casi todas detrás de la barra.

### 5.5 Viento (uno solo, hacia la derecha)

| Qué | Movimiento | Peso |
|---|---|---|
| Nubes | la deriva del legacy (+x de pantalla) | — |
| Copas de pino (legacy y bosque) | rotación `0,03·sin(1,1t + x·0,13)` rad + ráfaga `0,012·n(t/9 s)` hacia +x | altura², desde la base de la copa |
| Juncos | `0,06·sin(1,7t + z·0,4 + semilla)` rad + la misma ráfaga ×2 | altura² |
| Pasto y helechos | `0,08·sin(2,3t + x·0,9 + z·0,7)` rad | altura² |
| Matas de la pradera | como las copas, ×1,5 | altura² |

`t` es el tiempo de escena y `n` un ruido de valor en `[−1; 1]`. Todo va en el *vertex shader*, con `_RmSceneTime` y `_RmWindPhase` por instancia. Sigue en pausa y con reducir movimiento (es el mundo, §13).

---

## 6. Montañas

### 6.1 El perfil por acimut (contrato del legacy)

- **Fórmulas**: lejana `p_F = 0,45 + 0,35·sin(6,3u) + 0,2·sin(15,1u)` con `u = acimut/70° + 0,5`; cercana `p_N = 0,45 + 0,35·sin(6,3u + 3,1) + 0,2·sin(15,1u + 6,2)` con `u = 1,6·(acimut/70° + 0,5)`, a 0,7 del alto. De −65° a +65°.
- **Lo que forma**:
  - el **macizo** de la izquierda, con su pico a −25,4° (p 0,892);
  - el **pico central**, a −2,3° (0,658);
  - el **collado** de la derecha: la cresta por debajo de 0,5° de +13° a +20°, y las dos en 0 de +14,6° a +18,2° (el sol de las 18 h, a +17,5°);
  - el **pico de la derecha**, a +58° (0,942), fuera del cuadro en todos los tamaños: en TH y H844 entra apenas su flanco nevado (desde +47°).

### 6.2 El alto (P-11; DV-65): el del legacy, con tope de 9°

`tan Θ = mín(0,12·alto/focal; tan 9°)` (QA-6a-PAR-03). Es el `hMax = 0,12·alto` del legacy (`lake_environment.dart:46`) pasado a ángulo con la focal del contrato, con el tope de 9°. En horizontal, `alto = focal` y `tan Θ = 0,12` exacto (6,84°) en los cuatro tamaños; en vertical, 0,26 a 0,27 (14,6° a 14,9°), y manda el tope.

**Pantalla y perspectiva**: el perfil es un anillo de elevación fija por acimut (como el disco del astro, que es mundo): `tan(elevación) = tan Θ·p(acimut)`, con la escala `k(d)` de §6.3 encima. En la columna del centro el alto en pantalla es el del legacy (`0,12·alto·p`). Hacia los costados la perspectiva de un anillo lo sube `1/cos(acimut)`, como a cualquier cosa del mundo a la misma elevación: ×1,107 en el macizo.

| Perfil | Θ | Del cielo visible (columna del centro) | Macizo (−25,4°; p 0,892) | Pico central (−2,3°; p 0,658) |
|---|---|---|---|---|
| V390 | 9,0° | 18,3 % | (9,6; 276,6) | (179,4; 296,9) |
| TV | 9,0° | 17,8 % | (10,2; 301,3) | (189,1; 322,7) |
| P360 | 9,0° | 17,8 % | (8,9; 263,7) | (165,6; 282,4) |
| H844 | 6,84° | 28,6 % | (236,6; 117,6) | (406,4; 133,0) |
| TH | 6,84° | 28,6 % | (261,7; 123,9) | (440,6; 140,1) |
| L640 | 6,84° | 28,6 % | (148,9; 108,6) | (305,6; 122,7) |
| W1280 | 6,84° | 28,6 % | (297,8; 217,1) | (611,2; 245,5) |

- **El macizo**, el pico más alto que se ve del lejano, llega a 0,892 del alto: **6,11° en horizontal y 8,04° en vertical**. En pantalla, en su columna, es el 28,2 % del cielo en horizontal y el 17,6 % (TV, P360) o el 18,1 % (V390) en vertical. Sin la perspectiva (la `τ` del legacy) serían el 25,5 % y el 15,9 %, que son los "≈ 25 %" y "≈ 16 %" de AC-SCN-19-01 (§21.4).
- En el legacy el perfil se dibuja en 48 pasos y el macizo llega a 0,8896 (QA-6a-PAR-03); acá, con 260 columnas, a 0,892.

**Las cuatro combinaciones** (`options.py`, rehecho en `mountains.py` con el alto del legacy):

| Combinación | Sol de las 12 h en TH (barra 54,8 / con sello 78) | Disco entero a la mañana, vertical (TV) / horizontal | Luna de las 22 h sobre la cresta, vertical (TV) |
|---|---|---|---|
| A: la spec original (alto del legacy, sol a 17°) | y 47,0: **detrás de la barra** | 8:16 / 7:03 | +4,2° |
| B: la recomendación de 02-tech-design (alto del legacy, sol a 12°) | y 85,3 | **10:49** / 7:31 | **−0,15°** (apoyada en la cresta) |
| C: la base (9° fijos, sol a 12°) | y 85,3 | 7:59 / 7:59 | +3,7° |
| **E: la spec del gate (alto del legacy con tope de 9°, sol a 12°)** | **y 85,3** | **7:59 / 7:31** | **+3,7°** |

- **E es el alto del legacy en horizontal** (6,84°; el sol asoma a las 6:28 y sale entero a las 7:31; la luna de las 22 h queda con 5,0° de aire).
- **En vertical las montañas son más bajas que en el legacy**: 18 % del cielo en lugar de 30 % (≈ 60 % del alto). Con el del legacy (14,9° en TV), el macizo de la izquierda dejaría el sol de la mañana detrás de la cresta hasta casi las 11 h, y la luna de las 22 h, apoyada en ella.
- **La forma no cambia**; el pico central sigue asomando arriba de la zona del bote en TV (y 322,7; la zona empieza en 324).

### 6.3 Se acercan, no se corren (Q-65)

`k(d) = D₀/D(d)`, con `D(d) = D_mín + (D₀ − D_mín)·exp(−0,04·d/(D₀ − D_mín))`; lejana D₀ 3500 y D_mín 2500, cercana 1200 y 600 (m virtuales). Al empezar se acercan al 4 % de la velocidad del bote; a 42,2 km miden ×1,304 y ×1,887; con el bote quieto, quietas. La escala afecta el alto y nunca el acimut.

### 6.4 Color, flancos y base (shader `Horizon`)

- **Color base**: el del contrato (§3.1 b), exacto en Core y pasado al shader en lineal.
- **Flancos**: de día, el que mira al sol `+12 %` y el otro `−10 %`, según el signo de `dp/dacimut` y de qué lado está el sol (`× vis_sol`). Les da volumen sin sacarlas del tono.
- **Base**: el 25 % de abajo de cada cordón va 0,15 más hacia `fog`, así se apoyan en la bruma.
- **Relieve fino** (Alto, P2): `+0,05·tan Θ·ridged(acimut/1,7°)` donde `p > 0,3`.

### 6.5 Nieve y *alpenglow*

- **Dónde**: en la lejana, donde `p ≥ 0,58`, con borde suave de ± 0,03 y un ruido de ± 0,04 en `p` con período de 1,7° (lenguas de nieve en las canaletas). Cubre el macizo (de −33,2° a −13,7°), el pico central (de −8,7° a +2,1°) y, en horizontal, el flanco del pico de la derecha (desde +47,1°): 45 % del ancho de la lejana en TV y 33 % en TH.
- **Color**: `lerp(#F2F6FA; fog; 0,3)` (legacy). En la hora dorada `+ lerp(·; sunGlow; 0,35·g)` (*alpenglow*, rosado a las 6 y a las 18). De noche, `× (0,55 + 0,45·iluminación·vis_luna)`: con luna llena la nieve brilla plateada.

### 6.6 Malla

Dos tiras de 260 columnas de −65° a +65°, centradas en la cámara (sin paralaje lateral), en la cola 1003 y sin escribir profundidad.

- **Atributos por vértice**: `p`, la pendiente y la semilla del ruido de nieve.
- **Escala**: el alto se escala por ventana (`tan Θ`) y por `k(d)`.
- **Costo**: 2 *draw calls*, ≈ 1 k triángulos, ≤ 15 ALU.

---

## 7. La orilla del lago

### 7.1 El corte transversal

Medidas en metros; la orilla en `|x| = 9` (contrato) serpentea solo hacia afuera: `|x_orilla(z)| = 9,0 + 0,5·n(z_mundo/41 m)`.

| Tramo (`\|x\|` desde la orilla) | Altura `y` | Qué es |
|---|---|---|
| 7,0 → 9,0 | −0,55 → 0 | fondo bajo el agua (15°): con el agua opaca no se ve, pero la intersección con el agua queda limpia |
| 9,0 → 10,0 | 0 → 0,27 | playa de canto rodado (15°); franja húmeda de 0 a 0,35 m sobre el agua |
| 10,0 → 16,0 | 0,27 → 2,50 | talud de pasto y roca (20°), con ruido de ± 0,25 m en 6 m: acá van los pinos del legacy |
| **Izquierda** > 16 | `2,5 + 0,12·(\|x\| − 16)` | ladera boscosa: 5,4 m a 40 y 36,6 m a 300; hasta 1,5 km, en la niebla extendida |
| **Derecha** > 16 | `2,5 + 0,02·(x − 16)` | pradera alta: 4,2 m a 100 y 8,2 m a 300 |

**Envolventes** (lo agregado no las pasa nunca; los objetos del legacy no tienen envolvente):

- **izquierda**: copa a ≤ **`2,2 + 0,20·|x|`** desde `|x|` = 18. Deja lugar para árboles de 3,1 m a 18, 4,0 m a 30, 6,4 m a 60 y 9,6 m a 100: un bosque que crece ladera arriba;
- **derecha**: ≤ `2,2 + 0,06·x` desde x = 30; antes, solo matas de ≤ 1,2 m.

**Por qué 0,20 y no 0,25**: la base de la nieve está en el 58 % del alto, a 3,98° en horizontal (58 % de 6,84°) y a 5,25° en vertical. La silueta del bosque (la envolvente vista desde la cámara, a 2,2 m) está a una elevación de `atan(0,20·sin|acimut|)`: 1,0° a −5°, 2,0° a −10°, 3,0° a −15° y 3,7° a −19°. Con 0,20 queda debajo de la base de la nieve hasta −20,4° en horizontal y en todo el ancho en vertical (hasta −27,3°); con 0,25, solo hasta −16,2° en horizontal, y tapaba el flanco derecho de la nieve del macizo (que va de −33,2° a −13,7°). Más allá de −20° mandan los pinos del legacy de la orilla, que a ≈ 30 m miden 15° en pantalla.

**La franja del legacy** (P-09): en `|x| < 18` solo van los objetos del legacy y lo bajo (≤ 1,2 m: pasto, helechos, juncos, rocas).

### 7.2 Nada flota

Las reglas de [00-deuda-diseno §6.3](00-deuda-diseno.md) con DT6a-08:

| Objeto | Regla |
|---|---|
| Pino (legacy) | `x`, `z`, escala y variante del legacy, sin tocar; pie en el mínimo del terreno bajo el tronco (radio `0,15·escala`) menos 0,20 m |
| Cabaña (legacy) | el ancla `(13; z₀ + 10)` es el centro del frente y nunca se mueve; la profundidad es de 4, 3 o 2,5 m hacia `+z`, la primera que no choca con un tronco (huella + 0,3 m); si choca igual, ese pino arranca sus gradas a 3,0 m (sobre el alero de 2,6). Piso 0,15 m sobre la esquina más alta de la huella y zócalo de piedra hasta el terreno (≈ 1,5 m del lado del agua) |
| Muelle (legacy) | tablero a 0,40 m de `x` −6 hasta donde la playa llega a 0,30 m (≈ −10,1); los pilotes en pares en `x` −8,7, −7,5 y −6,3 (los del legacy), hasta el fondo |
| Juncos, rocas, matas, pasto | §7.4; las rocas se hunden el 30 % de su alto |

`TerrainProfile.Height` es la única verdad de la altura (Core, `double`): la usan la malla y la colocación.

### 7.3 Los objetos del legacy

**Pino** (`prop.pine`; `PineMesh`). Las medidas son las del legacy (`shore_painter.dart:89-104`) en 3D:

| Parte | Legacy (2D, × escala) | 3D |
|---|---|---|
| Tronco | 0,30 × 1,0 m, `#5D4037` | cono de radio 0,15 → 0,05 hasta la punta; visible hasta 1,0 m; corteza con *smoothness* 0,15 y una grieta vertical por ruido |
| Gradas | 3 triángulos con base en 0,8, 2,3 y 3,8 m, punta en 3,0, 4,5 y 6,0 m y medio ancho de 1,3, 1,0 y 0,7 m | 3 "faldas" cónicas de 2,2 m de alto con 9 a 12 lóbulos cada una; radio del lóbulo = el medio ancho del legacy × (0,85 a 1,15) por ruido de la semilla, con la punta del lóbulo 0,25 m caída; nunca dos lóbulos vecinos iguales |
| Colores | `greens[0]` abajo, `[1]` en el medio, `[2]` arriba; la paleta según `semilla mod 3` | los mismos tonos como albedo de cada falda; oclusión por color de vértice de 0,55 (adentro y abajo) a 1 (las puntas); inclinación ≤ 2° por semilla |
| Alto y ancho | 6,0 m y 2,6 m × escala | ídem, ± 10 % (AC-SCN-15-06) |

- **Variación**: 3 formas (`(semilla / 3) mod 3`) × 3 paletas (`semilla mod 3`, por instancia con `_RmVariant`) = 9 combinaciones con 3 mallas.
- **LOD**: 1 200 / 400 / 120 triángulos.
- **Luz**: envolvente 0,35 y viento (§5.5).

**Cabaña** (`prop.cabin`; `CabinMesh`). Las medidas son las del legacy (`shore_painter.dart:106-118`):

- **Cuerpo**: 4 m de frente × 2,6 m de pared; la profundidad la da DT6a-08. Madera `#6D4C41` en tablas horizontales de 0,18 m: una línea oscura de 1 cm por *shader*, en `y` de mundo.
- **Techo**: a dos aguas hasta 4,2 m, `#4E342E`, con alero de 0,4 m a los costados (el legacy va de −2,4 a 2,4) y 0,5 m al frente, y tejuelas en hileras de 0,25 m.
- **Puerta**: `#3E2723`, 0,8 × 1,4 m, centrada.
- **Ventana**: 0,8 × 0,7 m, de `x` +0,7 a +1,5 y de `y` 1,3 a 2,0 (la del legacy), con marco claro `#D7CCC8` de 4 cm.
  - De día, `#90CAF9 × (0,6 + 0,4·Fresnel hacia skyHorizon)`.
  - De noche, `#FFE082` (el color de contrato, antes del ajuste de color de la hora: AC-SCN-09-06, AC-SCN-20-02) × la ganancia global `_RmEmissiveGain` (**1,6**, en HDR, para el *bloom*; 1 en la pasada de contrato de §10.4), con el fundido de 0,4 s de DV-60. No se tiñe, no recibe luz ni entra en la niebla. WP-D2 puede bajar la ganancia hasta 1,25 si la sonda de aspecto de §10.4 lo pide; el color no se toca. **WP-D2 la dejó en 1** (sin tonemapping, cualquier ganancia recorta el rojo de `#FFE082`: "Cambios por implementación de WP-D2" §1).
- **Zócalo** de piedra `#6B6A66`. El frente, con la puerta y la ventana, mira a la cámara.
- **LOD**: 1 200 / 300 triángulos; la ventana va en una submalla aparte.

**Muelle** (`prop.pier`; `PierMesh`):

- **Tablero**: `#8D6E63`, de 1,6 m de ancho, en tablones de 0,20 m con juntas de 2 cm (por *shader*), de `x` −6 a la playa.
- **Pilotes**: `#5D4037`, de radio 0,10 m, en los pares de §7.2.
- **LOD**: 600 / 150 triángulos.

### 7.4 Lo que se suma (determinista, con sus propias semillas)

Semilla `SeedFor(lago; k) + sal·104 729`, sal 1 a 6 (juncos, rocas, bosque, pradera, pasto y helechos, serpenteo). Nunca usa el generador del legacy.

| Capa (slot) | Dónde | Forma | Densidad | Alto | LOD (triángulos) y fin |
|---|---|---|---|---|---|
| Juncos (`vegetation.reeds`) | `\|x\|` de 8,6 a 9,6, raíz con `y ≤ 0,05` | mata de 10 a 14 hojas largas (cintas de 2 triángulos, curvadas), opacas | 1 cada 0,8 m, con huecos de 2 a 6 m cada 10 a 25 m; nunca a menos de 3 m de un muelle ni frente a una cabaña (± 4 m en `z`) | 0,6 a 1,2 m (DT6a-09) | 24 hasta 30 m, 8 hasta 120 m, tramado de 100 a 120 m |
| Rocas (`prop.rock`) | playa y pie del talud | 5 variantes de icosaedro deformado por ruido, `#6B6A66` ↔ `#7A746B` | 1 cada 7 m por orilla | 0,2 a 0,7 m | 80 hasta 40 m, 24 hasta 200 m, tramado de 180 a 200 m |
| Bosque cercano (`prop.pine`, receta "bosque") | izquierda, `\|x\|` de 18 a 45 y `z` relativo ≤ 120 m | el pino con 3 a 5 gradas más angostas (radio × 0,8), paletas del legacy × 0,85 de luminancia (así los del legacy se leen delante) | disco de Poisson de 6 m | `mín(envolvente − terreno; 6 + 0,12·(\|x\| − 18))` | 400 hasta 40 m, 120 hasta 120 m; más allá pasa a la copa |
| Copa lejana (`vegetation.canopyFar`) | izquierda, `\|x\|` > 45 o `z` > 120 | el terreno levantado hasta la envolvente menos 1 a 3 m de ruido (bultos de 6 a 10 m), material de bosque `#2A4A2C` con oclusión en los valles del ruido | continua | hasta la envolvente | parte del trozo de terreno |
| Matas de la pradera | derecha, `x` de 30 a 120 | grupos de 3 a 9: coníferas chicas (el pino a escala 0,4-0,7) y arbustos de lóbulos (`#4F6B35`, de 1 a 1,5 m) | un grupo cada 90 a 150 m en `z` | bajo la envolvente | 400 hasta 60 m, 120 hasta 400 m |
| Pasto (`vegetation.grass`) | talud (`\|x\|` 10-16) y pradera | mata de 6 hojas (1 triángulo cada una) opaca, color del pasto del terreno ± 8 % | 4 matas/m² junto a la cámara, que bajan linealmente a 0 en el fin | 0,2 a 0,6 m | 6 hasta 20 m, 3 hasta el fin: **40 m** en Medio, 60 m en Alto, 30 m con la mitad en Bajo; tramado en el último 20 % |
| Helechos (`vegetation.fern`) | pie del bosque (izquierda, `\|x\|` de 16 a 20) | 5 frondas de 4 triángulos | 1 mata/m² | 0,4 a 0,8 m | igual que el pasto |

**Sin *alpha test***: el pasto, los helechos y los juncos son geometría opaca. En un móvil, el *alpha test* rompe el *early-z* y el sobre-dibujo de la franja de vegetación pasaría de 2,5×. El fin del pasto se esconde porque el terreno lleva el mismo color del pasto.

### 7.5 Materiales del terreno (shader `Terrain`, procedural)

| Zona | Color | *Smoothness* | Cómo se decide |
|---|---|---|---|
| Canto rodado | `#7A746B`, con piedras por ruido (celdas de 8 a 15 cm, ± 12 % de luminancia) | 0,2 | `\|x\|` de 9 a 10 |
| Franja húmeda | `#4E4A45` | 0,55 (refleja un poco de `skyHorizon` por Fresnel) | `y` de 0 a 0,35 m, con borde de ruido de ± 5 cm |
| **Línea de contacto** | `#C9D2D4` al `20 %·ambient` | — | `y` de 0 a 0,04 m: marca dónde termina el agua, el "nada flota" de la orilla |
| Pasto | `#4F7A3A` ↔ `#6B8A3E` por ruido de 4 m | 0,1 | talud y pradera |
| Suelo de bosque | `#4A3B2C` con agujas `#5A4632` | 0,1 | izquierda, desde `\|x\|` 18 |
| Roca | `#6B6A66` (triplanar, solo ella) | 0,25 | pendiente > 35° |

- **Ruido**: una textura de 256² (RGBA8, *tileable*) con dos lecturas a escalas cruzadas (1,3 m y 7 m), nunca un *hash* iterado.
- Cada pixel mezcla como mucho dos zonas.
- **Sin costuras**: el trozo de 50 m comparte los bordes con el vecino (`TerrainChunkMesh`), y el ruido va en coordenadas de cinta con la fase de `d` en `double` desde Core, como el agua.

### 7.6 LOD, distancias, aparición y tramado

| Pieza | LOD0 | LOD1 | LOD2 | Fin |
|---|---|---|---|---|
| Pino del legacy | 0-40 m | 40-120 m | 120-400 m | aparece con tramado de 400 a 360 m (DV-62) |
| Cabaña, muelle | 0-60 m | 60-400 m | — | ídem |
| Bosque cercano | 0-40 m | 40-120 m | → copa | 120 m (tramado de 110 a 120) |
| Terreno (trozos) | 0-150 m: 1 m en `z`, columnas de 0,5 m en la playa y el talud y de 2 a 8 m afuera | 150-450 m: la mitad | lejano de 450 a 1 500 m: 8 m | — |

- **Cambio de LOD**: tramado complementario en el 10 % del límite (36-44 m alrededor de 40), con *Bayer* 4 × 4 fijo en la pantalla (sin tiempo: las tomas se repiten byte a byte).
- **Cerca de la cámara** (DV-62, corregida en el gate): tramado de **2 a 1 m** en `z`. El legacy saca lo de la orilla de la lista al pasar 1 m (`zMin = d + 1`), así que el tramado sale de la misma lista del contrato `[d + 1; d + 400]`, sin banda propia. Casi nunca se ve: la orilla sale del cuadro a 8,1 m en TH, 8,3 en H844, 10,1 en L640 y W1280 y 18 m en vertical, y la punta del muelle a 5,4 m en TH.
- **Nivel Bajo**: todas las distancias × 0,7.
- **Cobertura del vuelo y de la toma aérea** (DN6a-11): el terreno y el relleno se arman desde `d − 25 m`. La lista de objetos del legacy sigue siendo la del contrato (`d + 1` a `d + 400`).

### 7.7 Con el Terrain Sample Asset Pack (opcional)

Lo que entra por slot y lo que mejora está en [00-deuda-diseno §6.6](00-deuda-diseno.md). Para que el *shader* de terreno no cambie de costo:

- **Capas en arreglos**: las capas `terrain.layer.*` del pack se copian al precargar a dos `Texture2DArray` (albedo con *smoothness*, normal con oclusión), con `Graphics.CopyTexture` en la GPU. Requisito: el mismo tamaño, formato y cantidad de *mips*; si una capa no lo cumple, esa capa vuelve al procedural con un aviso `[SLOT]` (DN6a-15).
- **Costo**: cada pixel lee **las dos capas con más peso**, 4 lecturas más la del ruido, ≤ 40 ALU. En el nivel Bajo, solo la dominante.
- **Vegetación**: los *prefabs* del pack (`vegetation.grass`, `.fern`, `.bush`) entran con su malla y su material de URP, instanciados. Si traen *alpha test*, se limitan a 30 m en Medio.
- **Sin el pack** (hoy), lo de §7.5 y §7.4: la demo se ve completa (ASSET_SLOTS §8).

---

## 8. Agua degradada por hora

### 8.1 La receta (shader `WaterDegraded`; DT6a-07)

Plano de 1,6 × 1,5 km centrado en la cámara (cinta), grilla de 16 × 16, cola 2450. Con `y_s` = el `y` de pantalla del píxel, `H` = alto de pantalla − horizonte del cuadro, `t_p = (y_s − horizonte)/H`, la posición de mundo y los parámetros de Core:

| Paso | Qué | Fórmula | ALU |
|---|---|---|---|
| 1 | **Tres tramos** (contrato) | de arriba a abajo `lerp(waterFar; fog; 0,5)` → `waterFar` → `waterNear`, en `t_p` 0, 0,5 y 1 | 5 |
| 2 | **Las 10 líneas** (contrato) | la línea más cercana al píxel, en O(1): `t* = fase + round((√t_p − fase)/0,1)·0,1` con `fase = Mod(d·0,02; 0,1)` de Core; en `y = horizonte + H·t*²`, grosor 1,5 dp con 0,5 dp de borde suave; medio ancho `ancho·(0,15 + 0,35·t*)` centrado en la `x` del disco; color `sunColor` con alfa `0,05 + 0,1·(1 − t*)·fuerza` | 10 |
| 2b | **× la visibilidad del astro** (DN6a-06) | el alfa de las líneas × `vis` del disco que se ve. Core sigue dando el alfa del legacy (contrato del oráculo); el *shader* multiplica | (en el 2) |
| 3 | **Reflejo leve del cielo** (agregado) | `lerp(col; skyHorizon; 0,25·S(30; 400; z))` | 3 |
| 4 | **Franja somera** (agregado) | `lerp(col; #5A6656·ambient; 0,25·S(8,4; 9,0; \|x\|))`: el agua a menos de 0,6 m de la orilla mínima (la orilla solo serpentea hacia afuera) | 3 |
| 5 | **Bruma baja** (agregado, AC-SCN-15-07) | `lerp(col; color_bruma; 0,6·m(h)·S(10; 150; z))` con `color_bruma = lerp(fog; sunGlow; 0,25·g)` | 3 |

- **Total ≤ 24 ALU, 0 texturas.** Los agregados van con la palabra clave `RM_WATER_EXTRAS`. Si el gate no acepta DT6a-07, sin ella queda el *port* directo en ≈ 15 ALU.
- **Lo que no tiene**: niebla propia, olas, reflejo de la orilla ni estela (AC-SCN-13-01). La sombra del bote es otro *shader* (§8.3).
- **Precisión**: la fase y la `x` del disco las da Core en `double`: a 4 096 m, a 10 km y a 42 195 m nada salta (Q-68).
- **Las líneas se ven solo sobre el agua**: cerca del horizonte las cortan las orillas, que convergen, como en un agua real.

### 8.2 Por hora

| Hora | Arriba / medio / abajo | Líneas: color, alfa de la línea del horizonte (× vis) | Agregados |
|---|---|---|---|
| **6:00** | `#8F8CA3` / `#4B5F8F` / `#1C4274` | `#FFCA88`, 0,07; centradas en `x` del sol (−17,5°), que está detrás del macizo pero con vis 1 | bruma baja 0,55, dorada; reflejo hacia `#FFBA7E` |
| **12:00** | `#7FBCEB` / `#2F8FD6` / `#0E4F8F` | `#FFF6D5`, 0,15; centradas | reflejo hacia `#8FD3FF` |
| **18:00** | `#AE7F82` / `#7C5C7A` / `#163A66` | `#FFB36B`, 0,07; a la derecha (+17,5°) | reflejo hacia `#FF8C5A`: el agua lejana se calienta |
| 19:34 | `#494061` / `#3C355D` / `#0D2443` | **ninguna** en pantalla (vis 0): no saltan. El dato del legacy sigue: `#E4DBDF`, 0,07 (todavía es de día: fuerza 0,20) | — |
| **22:00** | `#0E1A36` / `#0E1E3C` / `#06122A` | `#E6EEFF`, 0,095; plateadas bajo la luna (−5,8°) | reflejo hacia `#121C3A` |

### 8.3 La sombra del bote (shader `BoatShadow`)

- **Forma**: un *quad* sobre el agua (cola 2451), óvalo suave de 8,6 × 0,9 m bajo el casco (el largo del casco con un margen; en `image-03` del legacy es un óvalo ancho al costado del bote).
- **Lado**: corrido 0,35 m al lado opuesto del astro (el signo de su acimut), contrato del legacy.
- **Alfa**: `0,08·ambient` en el centro, a 0 en el borde con `exp(−r²·4)`.
- En la 6a va en todos los niveles: el agua no recibe el mapa de sombras.

---

## 9. El bote provisorio (AC-SCN-01-08)

- **Casco** (`boat.hull`, `HullMesh`):
  - *loft* de 24 secciones de `z` 5,3 (popa) a 13,5 (proa), centrado en `x` 0;
  - manga de 0,29 m en la flotación y 0,33 m en la borda;
  - francobordo de 0,10 m; proa afilada con **bola de proa blanca** de 4 cm de radio; popa con aleta.
- **Colores**:
  - `#F5F5F5` arriba, que pasa a `#B0BEC5` en la obra viva (degradé en `y` de 0,10 a −0,05);
  - **línea de borda `#00B4D8`** de 2 cm;
  - **cockpit `#263238`**, un rebaje oscuro de 0,9 m alrededor de `z` 9,5, con los rieles del carro apenas insinuados (dos líneas `#37474F`);
  - *splash box* (la "V" de cubierta delante del *cockpit*) en el mismo blanco.
- **Material** (shader `Hull`): gelcoat con *smoothness* 0,85. WP-D2 suma la exposición del casco (× 0,78) y la mitad del croma del cielo en su ambiente, y la cubierta blanca de borda a borda ("Cambios por implementación de WP-D2" §1).
  - Especular de la luz clave (Blinn, exponente 64, × `vis`).
  - Fresnel hacia `skyHorizon` al 30 % en los costados: el casco se ve brillante y blanco a cualquier hora, teñido por la luz.
- **Quieto** respecto de la cámara del contrato. Con la respiración se mueve en pantalla lo que la cámara (≤ 2,2 dp en TV).
- **En pantalla**:
  - TV: popa en y 528,4, proa en 429,5 y punto del remero (205,5; 460,8);
  - TH: 335,5, 236,6 y (457,0; 267,8);
  - W1280: 587,7, 414,4 y (640,0; 469,1).

  Siempre dentro de la zona del bote.
- **Costo**: ≤ 6 k triángulos, 1 *draw call* (el casco con colores por vértice) + 1 (la sombra).

---

## 10. Luz y post por hora

### 10.1 El modelo de luz (`RowMateLighting.hlsl`; DN6a-13)

En la escena manda el tinte del legacy. La luz clave y el ambiente modulan alrededor de él: el promedio de una superficie de la orilla es el tinte del legacy, y la dirección del sol la vuelve "fotográfica".

```
T(a)    = (a; a; mín(1; a + 0,12))                                   // el tinte del legacy (shore_painter.dart:47)
Hemi(N) = lerp(norm(waterNear); lerp(norm(skyHorizon); norm(skyTop); máx(N.y; 0)); 0,5 + 0,5·N.y)
          // norm(c) = c / luminancia(c): solo el matiz del cielo, con luminancia 1
Kn      = (sunColor / máx(sunColor)) · (intensidad de la luz / 1,75)  // relativa al mediodía
luz(N)  = T(a) ⊙ [ 0,65·Hemi(N) + 0,55·Kn·envolvente(N·L; w)·sombra ]
          // envolvente(x; w) = máx(0; (x + w)/(1 + w)); w = 0,35 en el follaje y 0 en lo demás
color   = albedo ⊙ luz(N) ⊙ oclusión     → niebla (§10.3) → post
```

**Comprobación** (una cara iluminada y una a la sombra, contra el tinte del legacy):

| Hora | Iluminada | A la sombra | Tinte del legacy |
|---|---|---|---|
| 12 h | ≈ 1,2 | ≈ 0,65 | 1 |
| 18 h | ≈ 0,78 | ≈ 0,55 | 0,85 |
| 22 h | ≈ 0,25 | ≈ 0,23 | 0,35 |

Con la exposición de la noche (+0,6 EV), lo oscuro de la noche llega a ≈ 0,35: "apagada y azulada", como el legacy (AC-SCN-20-01).

**URP para lo de terceros** (los *prefabs* del pack con URP Lit o Shader Graph):

- la luz direccional con color `T(a) ⊙ sunColor` e intensidad `0,55·K/1,75·π`;
- el ambiente *Trilight* con `T(a) ⊙ (skyTop; skyHorizon; waterNear)·0,65`, con la luminancia de cada uno llevada a 1 por la misma normalización.

Así un *prefab* del pack queda dentro de ± 15 % de lo nuestro. `SceneLighting` lo pone en cada cuadro, solo si cambió más de 1/1 024 (02-tech-design §3.6).

### 10.2 La tabla de la hora (`HourLookTable`, P-17; lineal entre keyframes)

> **WP-D2 ajustó esta tabla con las tomas** (sin tonemapping, la exposición en 0, el contraste a la mitad, el *bloom* de las 12 h en 1,30 / 1,4) y el modelo de luz de §10.1 (0,80 / 0,45 y el 20 % del croma del cielo): la tabla final está en "Cambios por implementación de WP-D2" §2. Esta queda como registro.

| Keyframe | Intensidad de la luz (§3.2) | Sombras (Medio / Alto) | Exposición | Balance (temp / tinte) | Saturación / contraste | *Bloom* (intensidad / umbral) |
|---|---|---|---|---|---|---|
| 5 h | 0,55·vis | 0,55 / 0,55 | −0,20 EV | +15 / +5 | 0 / +2 | 0,70 / 1,0 |
| 7 h | 1,25 | 0,75 / 0,75 | 0 | +8 / +2 | +2 / +4 | 0,45 / 1,1 |
| 12 h | 1,75 | 0,85 / 0,85 | −0,15 | 0 / 0 | +4 / +8 | 0,35 / 1,2 |
| 18 h | 0,85 | 0,65 / 0,65 | +0,10 | **+15 / +4** | +6 / +4 | 0,75 / 1,0 |
| 20 h (luna) | luna | 0 / 0,35 | +0,35 | **−5 / −1** | **−5** / 0 | 0,85 / 0,9 |
| 22-4 h (luna) | luna | 0 / 0,25 | +0,60 | **−10 / −2** | **−10** / −5 | 0,95 / 0,8 |

- **Tonemapping *Neutral*** (R6a-04): ACES corre el matiz de los azules y naranjas del contrato. Viñeta 0,18 con suavidad 0,4 a toda hora. Sin grano.
- **La niebla** no va por hora: es la del contrato (80-400 m, 80 %), con la extendida de 400 a 1 200 m para el terreno lejano (§10.3). Salen las distancias por hora de ART_BIBLE §6.3, que contradecían AC-SCN-20-01.
- **La noche se pone azul con la paleta y el tinte del legacy, no con el balance** (cambio del gate, QA-G6aT-13). Con el balance de ART_BIBLE §6.3 (−25 / −4) y −20 de saturación, la ventana encendida (`#FFE082`, croma OKLCH 0,119) se veía casi blanca en pantalla (`#E5E3CB`, 0,032): el balance frío neutraliza las luces cálidas, como una cámara puesta en "tungsteno". Con −10 / −2 y −10 queda cálida (≈ `#E9E3BD`, 0,050), y el cielo y la orilla siguen azules (el matiz de `skyTop` se mueve 3,4°). Estimado con un modelo del LUT HDR de URP con *Neutral*, sin *bloom* (`window2.py`).
- **El balance de la tarde** baja de +20 / +6 a **+15 / +4**: con +20 / +6, el cielo a media altura de las 18 h (`#625080`) corría su matiz 8,7° hacia el magenta, más que la sonda de §10.4; con +15 / +4, 6,5° (`hue18.py`). El horizonte sigue naranja (`#FF8C5A` → ≈ `#D98A4D`). Si la toma real todavía pasa de 8°, es el primero que se baja otra vez; la paleta no se toca.
- **Nivel Bajo**: sin HDR ni *bloom*; quedan la exposición, el balance, la saturación y el contraste (ARCHITECTURE §20.1).

### 10.3 Sombras y niebla

- **Sombras**:
  - Medio: 1 cascada de 1 024 a 40 m (el bote, los pinos, la cabaña y el muelle cercanos);
  - Alto: 2 cascadas de 2 048 a 80 m;
  - Bajo: sin mapa;
  - borde suave (PCF de 4 muestras), *bias* 0,4 / normal 0,6. De noche, solo en Alto (0,25).
  - **Largo**: con la luz a 6° (6 y 18 h), 9,5 veces el alto (un pino de 6 m da 57 m de sombra hacia la cámara); a las 7 h, 2,95; a las 9 y a las 15 h, 1,17; al mediodía, 0,70.
- **Niebla** (`RowMateFog.hlsl`), para el terreno, los objetos y la vegetación:
  - `f = 0,8·clamp((z − 80)/320) + 0,2·clamp((z − 400)/800)`. Hasta 400 m es el `_c` del legacy exacto (AC-SCN-20-01).
  - **bruma baja**: `f = máx(f; m(h)·exp(−máx(y; 0)/1,2)·S(10; 150; z))`;
  - color `fog`, y la bruma baja con `color_bruma` (§8.1).
  - Las luces de la noche no entran en la niebla (AC-SCN-09-06).

### 10.4 Sondas de color para QA (en las tomas `it6a`; QA-6a-PAR-02, QA-G6aT-13)

Hay dos clases de sonda. Salvo la del velo (CAP-20, con el HUD), se leen en la **escena sola**, sin el HUD ni los degradés de pantalla (DV-67; §12.3):

- **De contrato**: los colores del contrato **antes del ajuste de color de la hora** (AC-SCN-09-06, AC-SCN-11-02, AC-SCN-20-02, como los de referencia de AC-SCN-15-06). Se leen en la **pasada de contrato** (DN6a-27): el mismo cuadro de la toma, con el post de la cámara apagado (sin exposición, balance, saturación, contraste, viñeta, *bloom* ni *tonemapping*) y `_RmEmissiveGain` en 1. No se publica como imagen: sus valores van al `manifest.json`. La viñeta sola oscurecía un 14 % el borde de arriba, y el *bloom* y el HDR saturaban la ventana: por eso no se miden en la toma.
- **De aspecto**: en la toma publicada (con el ajuste de la hora), lo que el ajuste puede mover y lo que no: el matiz.

Cada sonda lee la **mediana de un parche de 5 × 5 dp** (3 × 3 en la ventana), así un píxel de tramado o una estrella no la mueven. Ninguna de las 80 estrellas cae en los parches del cielo en V390 (`stars.py`), y a esas horas tampoco el halo, el resplandor ni las nubes (que en V390 van de y 22,7 a 102).

| Sonda | Clase | Dónde | Pasa si |
|---|---|---|---|
| Cielo de arriba | contrato | CAP-02 (V390, 12 h), (195; 8): `q` 0,976 | `#0B4FA8` ± 3 por canal (`SkyBackdrop.Gradient(q)`: `t` 0,9994) |
| Cielo a media altura | contrato | CAP-02, (195; 168,8): `q` 0,5 | `#2C70BE` ± 3 (`t` 0,75, interpolado en sRGB como `Color.lerp`; en lineal daría `#4B82C2`) |
| Ventana encendida | contrato | CAP-04 (V390, 22 h), el centro de la ventana (mide ≈ 8 × 7 dp a 40 m) | `#FFE082` ± 3 por canal: sin tinte, sin luz y sin niebla |
| Luz de la cabaña (DN6a-13) | contrato | CAP-02, -03 y -04, la cabaña de 40 m: un faldón del techo (al astro) y el frente (a la sombra: el astro está adelante) | dentro de ± 25 % de `T(a)·{1,2; 0,65}` × el albedo |
| Matiz de la hora | aspecto | CAP-01, -03 y -04 (6, 18 y 22 h; a las 12 h el ajuste es casi neutro y el *bloom* del sol llega a la mitad del cielo), los dos parches del cielo | el matiz OKLCH a ≤ 8° del de Core para esa hora y ese `q` (estimado: ≤ 6,5° con la tabla de §10.2) |
| Ventana, aspecto | aspecto | CAP-04, el centro de la ventana | matiz OKLCH de 85° a 110° y croma ≥ 0,04: se lee cálida, no blanca (`#FFE082` es 91,7° y 0,119; estimado ≈ 100° y 0,050) |
| Velo de pausa | aspecto, con el HUD | CAP-20 (TV), el cielo a 30° de elevación, (205; 128) | el canal máximo en sRGB × 0,85 ± 0,03 y el croma OKLCH × 0,70 ± 0,05, contra el mismo cuadro sin pausa (§12.4). Los dos cuadros llevan los degradés (van antes del post y son iguales en los dos) |

- **WP-D2 no ajusta la exposición para pasar sondas** ni compensa con ella los degradés de pantalla (QA-6a-PAR-02): las de contrato no dependen del ajuste. Si falla una de contrato, es un error de *shader* o de espacio de color. Si falla una de matiz, se baja el balance de esa hora (primero el de la tarde), nunca la paleta. Si falla la de la ventana, se baja `_RmEmissiveGain` (hasta 1,25) o la saturación de la noche, nunca `#FFE082`.
- La sonda del brillo del sol (AC-SCN-11-05, DN6a-21) es de aspecto y va en CAP-02 (§21.4).

---

## 11. Cámara

### 11.1 La pose del contrato (02-tech-design §2.1)

| Perfil | Horizonte | Focal | FOV vertical × horizontal | Arriba / abajo | La orilla sale del cuadro a |
|---|---|---|---|---|---|
| TV | 365,6 | 411 | 96,1° × 53,1° | 41,7° / −53,2° | z 18,0 m |
| P360 | 320,0 | 360 | 96,0° × 53,1° | 41,6° / −53,1° | z 18,0 m |
| TH | 172,6 | 411 | 53,1° × 96,1° | 22,8° / −30,1° | z 8,1 m |
| L640 | 151,2 | 360 | 53,1° × 83,3° | 22,8° / −30,1° | z 10,1 m |
| W1280 | 302,4 | 720 | 53,1° × 83,3° | 22,8° / −30,1° | z 10,1 m |

La matriz la calcula Core (`ChaseCameraProjection`); el signo del `lensShift` deja de ser un riesgo.

### 11.2 Respiración (ANIMATION_BIBLE §5.2 sin los términos del bote)

| Componente | Fórmula |
|---|---|
| Lateral | `0,035·sin(2π·0,071t) + 0,015·sin(2π·0,173t + 1,7)` m |
| Vertical | `0,025·sin(2π·0,093t + 0,4)` m |
| Rolido | **`0,15°`**`·sin(2π·0,061t + 2,1)` |
| Cabeceo | `0,10°·sin(2π·0,083t + 0,9)` |
| Multiplicador | ×1; en pausa ×0,6 (300 ms `ease.move` al pausar, 240 al reanudar, con el velo); durante la toma aérea baja a 0 (§11.5); con reducir movimiento, ×0 |

- **El horizonte** se aparta como mucho 1,26 dp en TV, 1,92 en TH y 2,93 en W1280.
- **La zona del bote**: el remero queda adentro en todo momento.
- Se siente como estar sobre agua calma: los períodos de 11 a 16 s no se ven como un ciclo.

### 11.3 Entrada: el vuelo (AC-SCN-02-05, AC-WKT-13-06)

| ms desde el toque | Qué pasa |
|---|---|
| 0-220 | Workout funde a negro (4b). La sesión, el reloj y la escena corren |
| 220-400 | La escena aparece desde el negro (`ease.enter`) **con la cámara ya volando** |
| 220-1 820 | El vuelo (1,6 s) |
| 400-1 120 | La cascada del HUD (la de la 4b y la 5), encima del vuelo |
| 1 820 | La pose del contrato, exacta; empieza la respiración desde su valor en ese tiempo de escena, multiplicada por `S(0; 0,6 s)` desde ahí, así no hay un salto |

**El camino** (00-deuda-diseno §3.3, ya en 02-tech-design §2.2):

- **Posición**: Bézier cúbica `P0 (3; 9; −14)`, `P1 (2,4; 7,2; −8,5)`, `P2 (0,6; 3,4; −2,5)`, `P3 (0; 2,2; 0)`. Mide 15,9 m.
  - **Curva** (DN6a-08): **`ease-in-out-sine`**, no `ease.move`. Con `ease.move` (cúbica) la mitad del vuelo pasa por 32,5 m/s, un tirón de 0,2 s en medio de la bajada; con la de seno, el pico es 17,0 m/s. Arranca y llega igual de suave: una bajada de dron.
- **Orientación**: `slerp` desde mirar al bote (guiñada −7,3°, cabeceo −20°) hasta quedar nivelada, con la misma curva.
- **Focal** (DN6a-08): desde **×1,25 la del contrato** hasta la del contrato, con `ease.move`.
  - Con FOV de 50° (02-tech-design) la focal empezaba en ×2,38 en vertical (un zoom que se abre como un *dolly zoom*) y en ×1,07 en horizontal.
  - Con ×1,25 empieza en 83,3° en TV y 43,6° en TH: la misma sensación en las dos orientaciones.
- **Horizonte de la proyección**: desde el centro de la pantalla hasta el del contrato, entre 0,6 y 1 del recorrido.
- **Lo que se ve**:
  - **TV**: el bote empieza en el centro (y 457) y termina en 460,8, casi quieto mientras la cámara baja. El horizonte empieza en y 270,5 y baja a 365,6.
  - **TH**: el horizonte empieza en y 19 (casi sin cielo) y baja a 172,6: el cielo "se abre" mientras se baja al agua. El bote baja de 205,5 a 267,8.
- **No atraviesa nada**: `y ≥ 2,2`, `|x| ≤ 3` y `z ≤ 0` en todo el camino. El terreno y el relleno cubren desde `d − 25 m` (DN6a-11).

### 11.4 Salida (AC-SCN-02-06; spec)

- **Movimiento**: +1,5 m arriba y −4 m atrás en 1,2 s (`ease.move`) desde la pose del cuadro, mientras el HUD se va en 0-200 y la escena funde a negro en 200-400. A los 400, `Close`.
- **Lo que se ve**: a los 300 ms la cámara recorrió el 6 %: el remero baja ≈ 4 dp en TV y achica un 2,6 %. Es un indicio de "levantar la vista" que el negro corta, y alcanza.
- **Con reducir movimiento**, la cámara no se mueve.

### 11.5 La toma aérea del completado (AC-SCN-02-07; DN6a-07)

- **Cuándo**: con el diálogo de rutina completada (300 ms después del fin), una sola vez.
- **Movimiento**: desde la pose de ese cuadro, la cámara sube 1,70 m y retrocede 1,70 m (2,4 m: 0,4 m/s de promedio en 6 s) con `ease.move`. Al mismo tiempo **cabecea −4°** y la **respiración baja a 0** con la misma curva. Después, quieta (P-07: se puede bajar a 30 fps).
- **Por qué el cabeceo**: nivelada, la popa termina en y 588,7 de TV (la zona termina en 598) y en 395,7 de TH, debajo de los controles. Con −4° el bote queda bien adentro:

| Perfil | Horizonte | Remero | Popa | Proa |
|---|---|---|---|---|
| TV | 336,9 | 477,3 | 552,9 | 438,3 |
| TH | 143,9 | 284,3 | 359,9 | 245,4 |

  El horizonte sube 28,7 dp: el lago "se abre" y se ve el bote frenando.
- **`Terminar`** del diálogo hace salir desde donde está la cámara (§11.4).
- **Con reducir movimiento**, nada.

### 11.6 La escena demorada (AC-SCN-01-07)

El fondo provisorio teñido por la hora (DD6a-23) recibe la entrada de la 4b. Cuando la escena está lista (el primer dibujo espera al fin de la cascada, DT6a-11), `scene-surface` entra con **0,6 s `ease.enter`**, en la pose del contrato, sin vuelo y ya al día (distancia, velocidad y hora). Al terminar, el provisorio pasa a `display: none`. Sigue igual con reducir movimiento (es un fundido).

### 11.7 Rotar y cambiar la ventana (AC-WKT-17-07)

- **La escena**: se adapta en el acto (horizonte, focal y telón nuevos en el primer cuadro), sin reiniciar la distancia ni el tiempo.
- **El HUD**: cuando cambia la clase de ventana, entra en 200 (`ease.enter`) en el *layout* nuevo (DT6a-06); dentro de la misma clase, en el acto.

---

## 12. El HUD y el velo sobre la escena

### 12.1 Qué queda detrás de cada pieza (a distancia 0, a las 12 h y a las 22 h)

| Perfil | Barra (0-52·s; 74·s con sello) | Tarjetas de arriba | Disco del centro (cuenta grande y `PAUSA`) | Cielo libre | Zona del bote |
|---|---|---|---|---|---|
| TV | 0-54,8 / 78,0: detrás, el cielo casi `skyTop` y la parte de arriba de las nubes (centros en 54,8-92,1) | SPM 16-178 × 95-202; Split 256-396 × 95-180; Watts 273-396 × 190-273; paneles 16-158 × 212-377: detrás, cielo oscuro, nubes y el macizo (x 10) | 167,6-243,4 × 234-310: detrás, el sol de las 12 h (205,5; 278,2) y la luna de las 22 h (163,5; 288,3) | la columna central, x 178-256 entre las tarjetas y x 115-296 de 273 a 324: el sol, el pico central (y 322,7) | 115-296 × 324-598: el agua, el bote, las orillas que convergen |
| TH | 0-54,8 / 78,0: detrás, casi todas las nubes (centros 25,9-43,5) | SPM 16-177 × 96-201; Split 760-898 × 95-180; Watts 760-898 × 190-273: detrás, cordilleras y orilla | 419,6-494,5 × 108,8-183,7: detrás, el pico central (440,6; 140,1) | x 178-760 × 55-336: el sol de las 12 h (457; 85,3) libre entre la barra y el disco, el macizo (261,7; 123,9) | 256-658 × 206-329 |
| W1280 | 0-83,2 / 118,4: detrás, las nubes | las cuatro tarjetas en los costados | centrado, 182-297 | el sol (640; 149,4), la luna (566,4; 167,0), las cordilleras (y 217-302) | 358-922 × 361-577 |

- **En vertical** la escena se lee por la columna del centro: el sol o la luna, el pico nevado sobre la zona, las orillas que convergen, el bote.
- **En horizontal** el cielo libre es más ancho: el sol, el macizo, el collado y la capa lejana (Alto).
- **Ningún elemento del HUD se corre por la escena** (la escena no suma elementos; 56-57 de 60).

### 12.2 Contraste a 1,5 m

- **Sin cambios en el texto**: el vidrio sigue en `glass.fallback` `#070D16 @86 %` hasta la 6b (AC-WKT-17-01). La composición sigue en sRGB sobre la escena (DT6a-05). El peor fondo sigue siendo el blanco: una nube a mediodía, el disco del sol después del *tonemapping* (no pasa de blanco) o la nieve.
- **Valores contra el vidrio sobre blanco** (`#2A2F37`):
  - blanco 13,5:1;
  - métricas `.hud` ≥ 7,0:1;
  - familia roja ≥ 4,5:1 (ART_BIBLE §3.2).
- **A 1,5 m** se leen los mismos niveles de ART_BIBLE §10.2: SPM 3,4× el umbral de agudeza en TV, Split y Watts 2,4×, la cuenta grande 3,4×.
- **Lo que la escena agrega es textura**, no contraste: detrás del 14 % que deja pasar el vidrio, una nube o un tronco modulan ≤ 14 % el fondo de la cifra. En la 6b el desenfoque lo alisa.
- **Las píldoras** (AC-WKT-17-13, P6a-01 adoptado en 02-tech-design §2.18): en el HUD, el relleno, la hecha, el borde de la hueca y el pulgar van con `step.*.hud`. Con el peor vidrio renderizado:
  - trabajo 3,625;
  - enfriamiento 3,705;
  - otro 3,691;
  - descanso y calentamiento ≥ 5,5.

  **Excepción contada**: el borde de la hueca contra su interior al 18 % da 2,93 a 3,02, como la pista al 30 %.
- **Lo que se lee de lejos** sobre la escena: los valores, el aro amarillo de `PAUSA` (más fuerte sobre la escena desaturada) y la cuenta grande. De noche, el HUD se ve más claro que la escena; el legacy no lo atenuaba y la spec no lo pide.

### 12.3 Degradés de pantalla (*scrims*; C-18, DV-67; ART_BIBLE §3.2)

Son de la **capa del HUD** (DV-67; ART_BIBLE §3.2: "detrás del HUD, delante de la escena"): existen cuando el HUD está a la vista y **nunca en la escena sola** (QA-6a-PAR-02).

- **Qué**:
  - arriba, `#06122A` del 38 % a 0 en el 24 % alto de la pantalla;
  - abajo, del 32 % a 0 en el 18 % bajo.
- **Cómo se dibujan**: un *quad* de la cámara de la escena (shader `ScreenOverlay`, cola 3500), lo último de la escena y antes del post. Así suman 0 elementos de UI y los alcanza el velo de pausa (AC-SCN-01-02).
- **Cuándo** (DN6a-25): los prende y los apaga el HUD, no la escena.
  - Con `HideForCapture` (escena sola: CAP-01 a -12, VID-05, -06 y -10, y la pasada de contrato de §10.4) **no se dibujan**. Si se quedaran, en (205; 8) de TV pondrían el 36,6 % de `#06122A` sobre `skyTop`: `#09397A`, a ΔE OK 0,096 (QA), y la comparación con LEG-01 a -07 tendría el 24 % de arriba y el 18 % de abajo oscurecidos, cosa que el legacy no tiene.
  - Con el HUD, siempre a pleno y sin animación propia: en la entrada están desde el primer cuadro de la escena (debajo del vuelo y de la cascada), en la salida se van con el negro y en la escena demorada entran con su fundido de 0,6 s. A la vista nunca cambian de golpe.
- **No se compensan**: la tabla de la hora y las sondas de §10.4 se ajustan y se miden sin ellos. WP-D2 no toca la exposición por los degradés.

Detalles:

- **No tocan el bote**: el de abajo empieza en 0,82 del alto y la zona termina en 0,654 (vertical) y 0,802 (horizontal).
- **El sol del mediodía en TH** (y 85,3) queda en la cola del de arriba (98,6): lo oscurece un 5 %, que no se ve.
- Le dan aire al HUD y oscurecen lo que queda detrás de la barra y de los controles.

### 12.4 El velo de pausa (AC-WKT-13-12)

- **Qué hace**: la escena **pierde 30 % de saturación y se oscurece 15 %**, en el mismo `Volume` de la hora (`HourLighting.Compose`; DT6a-10): saturación × `(1 − 0,3·w)` y exposición `+ w·VeilEv`. El peso `w` sale de `PauseVeilMotion` (0 → 1 en 300 `ease.move`; vuelta en 240).
- **`VeilEv`**: arranca en **−0,50 EV**. WP-D2 lo ajusta con la sonda de §10.4 (el canal máximo × 0,85 ± 0,03). **Quedó en −0,17** (con la saturación × 0,7 el canal máximo del cielo ya baja ≈ 9 %): "Cambios por implementación de WP-D2" §3.
- **Qué no toca**: la UI, que se compone después. `PAUSA` en su disco, las tarjetas, la barra, el chip y los paneles quedan a pleno.
- **Qué sigue**: el tiempo de escena (las nubes, las estrellas, el viento, la bruma), y el bote se desliza ≈ 6 m y queda quieto a los ≈ 9 s. La respiración pasa a ×0,6.
- **Cómo se siente**: el mundo "se congela" en color mientras sigue vivo. El amarillo del aro de `PAUSA` es lo más saturado de la pantalla.
- **Con el fondo provisorio** (escena demorada), el velo es el de la 4b: el tinte del fondo, 15 % más oscuro.
- **Con reducir movimiento**, el velo igual (es un fundido) y `PAUSA` sin escala.

### 12.5 El fondo de carga

El elemento de la 4a, teñido por código con `skyTop` arriba y `waterNear` abajo **de la hora** (DD6a-23):

- a las 22 h, `#050A1E` / `#06122A`;
- a las 10 h, `#1E61BA` / `#145395` (interpolado).

Se ve solo hasta que entra la escena (CAP-24, VID-11).

---

## 13. Reducir movimiento (AC-APP-05-08)

| Qué | Con reducir movimiento |
|---|---|
| Vuelo de entrada | no: la cámara ya en su lugar; el fundido de 120 ms lineal de la 4b para todo (P-15) |
| Respiración | ×0 |
| Salida | la cámara no se mueve |
| Toma aérea | no |
| `Hora escena` | en el acto |
| Minuto de la hora real | en el acto (sin la interpolación de 1 s) |
| HUD al rotar | en el acto |
| Velo de pausa, escena demorada (0,6 s), luces de la noche (0,4 s), estrellas (0,9 s) | **siguen** (son fundidos, no movimiento) |
| El avance del bote, las nubes, el titileo, el viento, las líneas del agua, la bruma | **siguen**: son el mundo y el dato (ART_BIBLE §10.4: "la remada y el agua no se apagan") |

---

## 14. Shaders URP propios

Los nombres y los globales son los del contrato de 02-tech-design §7.5. Ningún *shader* usa `_Time`: todo va con `_RmSceneTime` y las fases de Core. Todos tienen SRP Batcher (las propiedades en `UnityPerMaterial`) y no tienen lazos por píxel.

### 14.1 *Includes*

| Archivo | Qué da |
|---|---|
| `RowMateScene.hlsl` | los globales de la paleta (en lineal) y los tres del telón **en sRGB** para mezclar como `Color.lerp` (`_RmSkyTopSrgb`, `_RmSkyHorizonSrgb`, `_RmHazeSrgb`; con `SetGlobalVector`, que no convierte, y no con `SetGlobalColor`, que en espacio lineal los pasaría a lineal), `_RmEmissiveGain` (1,6; 1 en la pasada de contrato), `τ` y `q` de una dirección (con `_RmHorizonY`, `_RmFocalDp`, `_RmViewDp`), el disco (`_RmDiskDir`, `_RmDiskParams`: visibilidad, es luna, iluminación, ángulo de fase), el dorado `_RmGolden` |
| `RowMateLighting.hlsl` | §10.1: `T(a)`, `Hemi(N)`, `Kn`, la envolvente, la sombra de URP (`MainLightShadow`), la oclusión |
| `RowMateFog.hlsl` | §10.3: la del legacy, la extendida y la bruma baja (`_RmFogParams` = 80, 320, 0,8, 1 200; `_RmLowMist`); las luces de la noche la saltean |
| `RowMateDither.hlsl` | *Bayer* 4 × 4 fijo en la pantalla; la aparición (400 → 360 m), el tramado cercano (2 → 1 m, DV-62) y el cambio de LOD complementario, con `_RmFade` por instancia |

### 14.2 Los 13 *shaders*

| *Shader* | Cola | Entradas propias (además de los globales) | Qué hace | Costo (fragmento) | Por nivel |
|---|---|---|---|---|---|
| `Sky` | 1000, sin profundidad | `_RmMoonTex` (`sky.moon`); `_RmDiskRadius` (1,6° / 1,28° en rad), `_RmHaloRadius` (6,4° / 5,1°), `_RmGlowParams` (0,35; 14°; 20°), `_RmStarField` (densidad, alfa mín. y máx.) | §3.3 y §4.2 a §4.5: degradé de telón (en sRGB), bruma, resplandor, halo, disco con la fase y el hemisferio, fondo de estrellas, tramado *Bayer* | ≤ 41 ALU, 1 tex | sin fondo de estrellas en Medio y Bajo; sin HDR en Bajo |
| `Stars` | 1001, aditivo | por instancia: dirección, radio en rad (radio dp / focal), alfa (Core: con el fundido y tapada por el disco y el halo, DN6a-28) | 80 *quads* instanciados, disco suave, blancas | ≤ 4 ALU | igual en todos |
| `Clouds` | 1002, alfa | `_RmCloudAtlas` (`sky.clouds`), `_RmCloudLayer` (la capa lejana); por instancia: dirección, tamaño, variante, alfa del cuerpo, alfa de la sombra (Core: con la vuelta `V(φ)`, DN6a-26) | §5.3 y §5.4 | ≤ 12 ALU, 1-2 tex | la capa lejana solo en Alto |
| `Horizon` | 1003, sin profundidad | por material: tono, profundidad, ¿nieve?; por vértice: `p`, pendiente, semilla; `_RmHorizonScale` (`tan Θ·k(d)`), `_RmSnowParams` (0,58; 0,03; 0,04; 1,7°) | §6.4 y §6.5: color del contrato, flancos, base, nieve, *alpenglow*, borde encendido (P2) | ≤ 15 ALU | sin relieve fino en Medio y Bajo |
| `WaterDegraded` | 2450 | `_RmWaterBands` (3 colores), `_RmShimmer` (fase, `x` del disco, fuerza, `vis`), `_RmShimmerColor`, `_RmWaterExtras` (reflejo 0,25 y 30-400 m; somera 0,25 y 8,4-9,0 m; bruma 0,6); `RM_WATER_EXTRAS` | §8.1 | ≤ 24 ALU, 0 tex | igual en todos |
| `Terrain` | 2000 | `_RmNoiseTex` (256²), los colores de §7.5, `_RmZonePhase` (fase de cinta en `double` desde Core); con el pack, `_RmLayerAlbedo` y `_RmLayerNormal` (`Texture2DArray`) y `RM_TERRAIN_LAYERS` | §7.5 y §7.7, con la línea de contacto, la franja húmeda y la copa lejana (por color de vértice) | ≤ 35 ALU, 2 lecturas (sin el pack); ≤ 40 ALU, 5 lecturas (con el pack) | Bajo: 1 capa del pack, sin sombras |
| `Prop` | 2000 | color por vértice y por submalla, `_RmPlankParams` (tablas de 0,18 m o tablones de 0,20 m), `_RmWindowDay`, `_RmWindowNight` (`#FFE082`, sin HDR) × el global `_RmEmissiveGain`; por instancia: `_RmFade`, `_RmLit` | §7.3 (cabaña, muelle, rocas): madera por *shader*, ventana, tramado | ≤ 30 ALU | igual |
| `Foliage` | 2000 | `_RmPalettes` (los 3 × 3 verdes del legacy y los del bosque); por instancia: `_RmVariant`, `_RmWindPhase`, `_RmFade`; por vértice: grada, oclusión, peso del viento | §7.3 (pino) y §7.4 (bosque, matas): paleta, envolvente 0,35, viento en el vértice, tramado | ≤ 25 ALU | igual (LOD × 0,7 en Bajo) |
| `Grass` | 2000 | `_RmGrassColors`, `_RmGrassEnd` (fin y tramado); por instancia: color ± 8 %, fase | §7.4 (pasto, helechos): opaco, viento, envolvente 0,2 | ≤ 18 ALU | fin 60 / 40 / 30 m |
| `Reeds` | 2000 | `_RmReedColors` (`#6E7F4A` → `#A39E6A` en las puntas); por instancia: fase | §7.4 (juncos) | ≤ 18 ALU | 1 cada 1,6 m en Bajo |
| `Hull` | 2000 | `_RmHullColors` (4 colores de §9), `_RmGelcoat` (0,85; 64; 0,3) | §9 | ≤ 25 ALU | igual |
| `BoatShadow` | 2451, alfa | `_RmShadowOffset` (± 0,35 m), `_RmShadowAlpha` (`0,08·ambient`) | §8.3 | ≤ 5 ALU | igual |
| `ScreenOverlay` | 3500 | `_RmScrimTop` (`#06122A`, 0,38, 0,24), `_RmScrimBottom` (0,32, 0,18) | §12.3: los degradés de la capa del HUD; no se dibuja en la escena sola (DN6a-25) | ≤ 4 ALU | igual |

**Lo que verifica WP-D1 antes de entregar**:

- cada *shader* compila en Direct3D 11, Vulkan, GLES 3.2 y Metal (el de Metal, sin Mac: NO VERIFICABLE);
- el conteo de ALU es el del compilador de Mali (*Mali Offline Compiler*, si está) o el del *Compiled code* de Unity;
- ninguno lee `_Time`;
- se ven bien con los *placeholders* (ASSET_SLOTS §8).

---

## 15. Presupuesto visual para 60 fps (regla 9; ARCHITECTURE §20)

### 15.1 Reparto en el nivel Medio (Galaxy A54 / Pixel 6a; `renderScale` 0,85)

| Pieza | *Draw calls* | Triángulos (peor tramo) | Texturas | GPU estimada |
|---|---|---|---|---|
| Cielo (degradé, bruma, resplandor, disco, halo) | 1 | 0 | luna 256² RG8 (128 KB) | 0,4 ms |
| Estrellas y nubes | 2 | < 0,2 k | atlas 512 × 256 RG8 (256 KB) | 0,2 |
| Cordilleras | 2 | 1 k | — | 0,2 |
| Agua degradada | 1 | 0,5 k | — | 0,6 |
| Terreno (9 + 9 trozos + lejano, con la copa) | ≤ 19 | 40 k | ruido 256² RGBA8 (256 KB) | 1,6 |
| Pinos del legacy (3 formas × 3 LOD) | ≤ 9 | 32 k (12 / 22 / 73 pinos por banda, de `lodcount.py`) | — | 1,0 |
| Cabañas y muelles | 3 | 3 k | — | 0,2 |
| Bosque cercano y matas | 4 | 23 k | — | 1,0 |
| Pasto, helechos, juncos y rocas | 4 | 29 k | — | 1,4 |
| Bote y sombra | 2 | 6 k | — | 0,3 |
| Sombras (1 cascada de 1 024 a 40 m) | ≤ 15 | (≈ 25 k vueltos a dibujar) | 4 MB | 1,2 |
| Post (*bloom* a media resolución, color, *Neutral*, viñeta, velo) y *scrims* | ≤ 9 | — | RT de *bloom* ≈ 4 MB | 1,5 |
| UI (HUD, 56-57 elementos) | ≤ 12 | — | — | 1,0 |
| **Total** | **≈ 83** (tope 150; SetPass ≈ 30, tope 50) | **≈ 135 k** (tope 350 k) | **≈ 0,6 MB de contenido** (+ ≈ 2 MB con el pack) y ≈ 20 MB de RT | **≈ 10,6 ms** (tope 12) |

- **CPU de la escena** ≤ 1,5 ms: el `Tick` de Core (~100 objetos visibles, 80 estrellas, 7 nubes), aplicar el cuadro y armar las matrices. Un trozo de terreno cada ~12 s se arma en un cuadro sin movimiento finito o en tramos de ≤ 2 ms.
- **Sobre-dibujo** ≤ 2,5× en la franja de vegetación (pasto y juncos opacos, copas opacas).
- **0 B por cuadro** en régimen, medido en cuadros reales con `FrameAllocations` y su control positivo (AC-SCN-01-12).

### 15.2 Lo que cambia por nivel

| | Alto (escritorio, tomas) | Medio | Bajo |
|---|---|---|---|
| Resolución y AA | 1,0, MSAA 4× | 0,85 (0,75-1), MSAA 2× | 0,75, FXAA |
| HDR, *bloom* | sí | sí | no (el disco plano y el halo) |
| Sombras | 2 cascadas de 2 048 a 80 m; de noche, 0,25 | 1 de 1 024 a 40 m; de noche, sin sombras | sin mapa (el óvalo del bote) |
| Cielo | + fondo de estrellas + capa lejana de nubes | — | — |
| Cordilleras | + relieve fino | — | — |
| Bruma baja | sí | sí | no |
| Vegetación | pasto a 60 m | pasto a 40 m | pasto a 30 m con la mitad; juncos 1 cada 1,6 m; sin bosque cercano (la copa desde `\|x\|` 18) |
| LOD | × 1 | × 1 | × 0,7 |
| Terreno con el pack | 2 capas por píxel | 2 capas | 1 capa |

### 15.3 Adaptación (AC-SCN-01-13; ARCHITECTURE §20.3)

Se baja en este orden:

1. resolución (de a 0,05, entre 0,75 y 1, como mucho uno cada 10 s; vuelve a subir con 30 s holgados);
2. reflejo (no hay en la 6a);
3. distancia de sombras (40 → 25 → 0 m: el óvalo);
4. densidad de vegetación (pasto × 0,5 y fin × 0,75);
5. el nivel.

La tasa de cuadros nunca baja antes que el detalle. Lo que más se nota al bajar es el pasto: por eso va cuarto.

### 15.4 Lo que queda para medir

Los 60 fps durante 30 min, la fila de 200 píldoras (R4b-02) y el diálogo de completado en el teléfono: **NO VERIFICABLE** sin el teléfono ni el módulo de Android (regla 9, a la It. 14). En Windows, el `[PERF]` de 02-tech-design §4.5. En el Editor, `SceneBudgetTests` por nivel con el lago a las 12 y a las 22 h.

---

## 16. Qué tiene que verse en cada toma

Las condiciones son las de la spec §7 (escena sola: sin HUD ni los degradés de pantalla del HUD (DV-67), respiración en 0, tiempo de escena 3,2 s, `E-920`, salvo lo dicho). Las sondas de color de cada toma, de contrato y de aspecto, están en §10.4. Las posiciones salen de `captures.py`, en dp de la ventana, para que QA las mida con sondas (± 2 dp).

### 16.1 Escena sola, contra el legacy

| Toma | Tiene que verse | Contra LEG |
|---|---|---|
| **CAP-01** V390, 6 h | Cielo de `#324B9A` arriba a `#FFBA7E` abajo, con el resplandor dorado sobre el macizo de la izquierda. **El sol detrás del macizo** (centro en (72,0; 326,9), la cresta a 6,2°): se ven el halo y el borde encendido, no el disco. Las 80 estrellas tenues (oscuridad 0,27). Bruma baja sobre el agua (`m` 0,55) que esfuma la orilla desde los 10 m. Luz dorada y rasante (6°) desde la izquierda, con sombras de los pinos de 9,5 veces su alto hacia la cámara y a la derecha. *Alpenglow* rosado en la nieve. El muelle a 55 m a la izquierda (base en x 131,2) y la cabaña a 40 m a la derecha (x 321,8), con la ventana apagada. Las líneas del agua doradas, centradas en x 72 | el mismo tono violeta y naranja; en el legacy el sol se ve en (97,5; 337,6) porque no hay montañas delante |
| **CAP-02** V390, 12 h, con marcas | Cielo de `#0B4FA8` a `#8FD3FF`. **El sol en (195,0; 254,7)**, r 10,9, con *bloom*, sobre el pico central (y 296,9). Sin estrellas. Las 7 nubes en su `x` del legacy. Luz blanca y alta, con sombras cortas. **Cada pino, el muelle y la cabaña sobre su marca** (muelle x 131,2; cabaña x 321,8; ± 2 dp en `x`). Nada flota: los pies en el terreno, el muelle en sus pilotes, la línea de contacto en la playa | el mismo paisaje; el sol más abajo (en el legacy, y 33,8) y las montañas quietas |
| **CAP-03** V390, 18 h | Cielo de `#2E3C8C` a `#FF8C5A`. **El sol en (318,0; 326,9), en el collado**, pero con `E-920` la cabaña a 40 m (de x 298 a 345 con los aleros, hasta y ≈ 296) queda delante: **se ve la cabaña en contraluz** con el halo y el *bloom* alrededor del techo. Estrellas apenas (0,23, ya con el fundido terminado). Luz cálida y rasante (6°) desde la derecha. Nubes encendidas por abajo. *Alpenglow* en la nieve. El camino de líneas a la derecha, cálido | en el legacy la cabaña también tapa parte del sol (x 292,5): la misma lectura |
| **CAP-04** V390, 22 h | Noche: `#050A1E` a `#121C3A`. **La luna en (155,2; 264,3)**, r 8,7, gibosa menguante (≈ 94 %, con el lado oscuro apenas visible), con 3,7° de aire sobre la cresta. Las 80 estrellas plenas. **La ventana de la cabaña encendida**, cálida, con *bloom* y sin niebla (`#FFE082` en la pasada de contrato; en la toma, matiz ≈ 100° y croma ≥ 0,04, §10.4). Nubes casi invisibles (alfa 0,14) con un borde plateado cerca de la luna. Agua oscura con las líneas plateadas bajo la luna (x 155) | en el legacy, la luna más arriba y llena |
| **CAP-05** H844, 12 h, con marcas | CAP-02 en horizontal: el sol en (422,0; 80,9). Más orilla a los costados: la ladera boscosa a la izquierda y la pradera a la derecha. El macizo con nieve en (236,6; 117,6), con el alto del legacy (6,84°; DV-65). Cada objeto sobre su marca | ídem |
| **CAP-06** H844, 22 h | CAP-04 en horizontal: la luna en (382,2; 90,5) | ídem |
| **CAP-07** V390, `E-0`, 12 h, con marcas | El arranque de toda sesión: el muelle a 175 m a la izquierda (x 174,9), con niebla 0,24; la cabaña a 360 m (x 209,1), casi perdida (0,70); pinos desde la cámara hasta 400 m, apareciendo en la niebla. Cada objeto sobre su marca | ídem |
| **CAP-08** TH, 19 h | **El sol detrás de la cercana y de la cabaña** (centro en (610,0; 175,4), vis 0,54): el halo asoma sobre el collado. Las 80 estrellas (0,50). La ventana todavía apagada (ambiente 0,675). Las líneas a la mitad (× 0,54) | — |
| **CAP-09** TH, 20 h | **La luna saliendo a la izquierda**, detrás de la cresta (centro en (372,1; 165,5), a 1,0°; la cresta a 3,9°): se ve solo su halo. Estrellas (0,77). **La ventana encendida** (ambiente 0,5). Las líneas plateadas, tenues (× 0,38) | — |
| **CAP-10** TV, 0 h | La luna centrada en (205,5; 278,2), r 9,2, con su fase. Las 80 estrellas plenas: la primera en su lugar del legacy. Las líneas plateadas centradas | — |
| **CAP-11** TV, 15 h | **Sin estrellas** (Q-62). El sol en (268,8; 300,3), sobre la cercana (3,7°). Cielo de `#1C469A` a `#C7B0AC`, con luz cálida de la tarde | — |
| **CAP-12** W1280, `E-42K`, 12 h | Lo mismo que a 0 m: la cabaña a 165 m (niebla 0,21) y el muelle a 380 m (0,75), los del oráculo; las montañas × 1,30 y × 1,89, más cerca pero en el mismo acimut; sin temblores ni costuras | — |

### 16.2 El HUD sobre la escena (hora real de prueba, 10 h)

A las 10 h el sol está en acimut −5,8° y a 10,6°.

| Toma | Tiene que verse |
|---|---|
| **CAP-13** TV, 60,2 s | El HUD de la 4b sobre la luz de las 10 h, con los degradés de pantalla detrás (arriba y abajo; DV-67). El sol en (163,5; 288,3), en el lugar del disco del centro, que no está. La banda dice `1240` `m` y la escena recorrió ≈ 234 m: la cabaña de los 360 m a 126 m a la derecha (x 247,9), el muelle de los 575 m a 341 m en la niebla. El bote en la zona, con el remero en (205,5; 460,8) ± 2 dp |
| **CAP-14** TH | Ídem: el sol en (415,0; 95,4), libre entre la barra y el lugar del disco; el remero en (457,0; 267,8) |
| **CAP-15** L640 | Ídem: el sol en (283,2; 83,5) |
| **CAP-16** P360 | Ídem: el sol en (143,2; 252,3) |
| **CAP-17** W1280 | Ídem: el sol en (566,4; 167,0), r 20,1; el remero en (640,0; 469,1) |
| **CAP-18** TV, 18 h, los dos paneles abiertos | El atardecer detrás de los paneles. El sol en (335,1; 354,3), en el collado, a la derecha de la zona. El casco a la vista en el centro. Todo legible |
| **CAP-19** TV, 12 h, una nube detrás de la barra | La nube de `x₀` 0,42 (la mayor, `s` 40, centro en y 59,2) pasa detrás de la barra. Las píldoras hechas y la hueca se leen con su `.hud`; sonda ≥ 3 sobre el vidrio renderizado |
| **CAP-20** TV, pausa, 62,2 s | El velo: el cielo con el canal máximo × 0,85 y el croma × 0,70 (§10.4). `PAUSA` con su aro amarillo, a pleno. El HUD a pleno. El bote todavía deslizándose (≈ 1,05 m/s) |
| **CAP-21** TH | Ídem |
| **CAP-22** TV, `Corta`, 36,5 s | El final de la toma aérea: la cámara 1,70 m más alta y 1,70 m más atrás, con cabeceo de −4° y quieta (el horizonte en 336,9). El bote entero adentro de la zona (popa 552,9, proa 438,3). El diálogo opaco encima |
| **CAP-23** W1280, 22 h | El HUD de noche: la luna en (566,4; 167,0), las 80 estrellas en el cielo libre, la ventana de la cabaña (a 126 m, x 714,3), el vidrio sobre el cielo casi negro |
| **CAP-24** TV, escena demorada, 0,6 s | El fondo provisorio teñido por las 10 h (`#1E61BA` / `#145395`), con el HUD llegando |

### 16.3 La demo (W1280, `F-4A`, 10 h real de prueba)

| Toma | Tiene que verse |
|---|---|
| DEMO-01 | Workout en reposo; la escena no se dibuja |
| DEMO-02 | 2,0 s: la cámara ya en su lugar, el HUD entero, la luz de las 10 h (el sol en (566,4; 167,0)) |
| DEMO-03 | ≈ 39 s: el muelle pasando por la izquierda, con sus pilotes en el agua; las montañas en el mismo lugar que en DEMO-02 |
| DEMO-04 | `18:00`: el sol en (867,0; 282,6), r 20,1, en el collado, apoyado en la pradera; nubes encendidas por abajo; las líneas doradas a la derecha; el reloj de pared en la hora real |
| DEMO-05 | `19:00` → `20:00`: el sol se esconde detrás de la cercana; la luna asoma su halo a la izquierda (x ≈ 491); estrellas; la ventana de una cabaña, si está a la vista |
| DEMO-06 | `22:00`: la luna en (566,4; 167,0), r 16,1, con su fase; las 80 estrellas |
| DEMO-07 | `Pausa`: el velo sobre la noche (se nota más en lo claro: la luna y la ventana) |
| DEMO-08 | `Hora real`: de 22 a 10 h por el camino de adelante, pasando por la noche y el amanecer en 1,5 s; la salida; Workout |

### 16.4 Los videos

| Video | Tiene que verse |
|---|---|
| **VID-01** TV, la entrada | Negro en 0-220. Desde los 220, la escena aparece con la cámara alta y atrás, mirando al bote (horizonte en y 270,5), y **baja como un dron** hasta el agua, sin tirón en el medio (pico de 17 m/s). El cielo se abre y la focal pasa de ×1,25 a ×1. La cascada del HUD llega encima (400-1 120). A los 1 820 la cámara se apoya en la pose del contrato sin salto: el último paso es < 1 % del recorrido y la respiración entra en 0,6 s |
| **VID-02** TV, con reducir movimiento | El fundido de 120 lineal con la cámara ya en su lugar; sin negro, sin cascada, sin vuelo ni respiración |
| **VID-03** W1280, `Fuerte`, 30-42 s | Los pinos vienen hacia la cámara y salen por los costados. El muelle pasa por la izquierda a los ≈ 39 s. Las montañas no se corren de costado. Las nubes y la capa lejana se mueven solo con el viento. **Las líneas del agua avanzan** (un ciclo cada 5 m: ≈ 0,9 por segundo). Lo lejano nace en la niebla, con su tramado (ningún objeto pasa de alfa 0 a 1 en un cuadro). Las copas se mecen. El horizonte dentro de ± 3 dp |
| **VID-04** TH, pausa y reanudar | El velo en 300 ms. El bote se desliza ≈ 6 m y queda quieto a los ≈ 9 s; las líneas se frenan con él; las nubes y el viento siguen. Al reanudar, el velo se va en 240 y el bote acelera |
| **VID-05** TH, 17 → 23 h | Cada cambio en 1,5 s. **El sol baja al collado y se esconde detrás de la cercana y de la pradera, sin saltar**; su halo y el resplandor quedan un rato. A las 18:00 las estrellas entran en 0,9 s y crecen. Las líneas se apagan con el sol y, después del cambio (invisible), vuelven plateadas con la luna. La luna asoma por la izquierda detrás de la cresta. **La ventana de la cabaña se enciende con su fundido de 0,4 s.** La nieve pasa de rosada a gris y a plateada |
| **VID-06** TH, 3 → 7 h | La luna baja detrás de la cresta de la derecha. El amanecer violeta y naranja. El resplandor sobre el macizo de la izquierda; el sol sigue detrás. Las estrellas se apagan con su fundido (las últimas, a las 6:12). La ventana se apaga. Aparece la bruma baja, y a las 7 h todavía está |
| **VID-07** TV, la salida | El HUD se va con la última foto en 0-200. La cámara empieza a levantarse (apenas). Negro a los 400. Workout a los 640. 0 dibujos desde los 400 |
| **VID-08** TV, `Corta` | A los 30,3 s el diálogo y la toma aérea: la cámara sube y retrocede 6 s, cabeceando −4°, con el bote frenando adentro de la zona, y se queda quieta |
| **VID-09** TV → TH | El HUD entra en 200 en el *layout* nuevo. La escena sigue sin reiniciarse: la distancia, las nubes y el tiempo siguen; el telón se rehace en el cuadro |
| **VID-10** TV, 20 s, escena sola | La respiración: un vaivén lento, sin mareo. El horizonte dentro de ± 3 dp de la línea del contrato (marcada); como mucho 1,26 dp en TV |
| **VID-11** TV, escena demorada | El fondo provisorio teñido por la hora con la entrada normal; la escena entra en 0,6 s, al día, sin vuelo |

---

## 17. Los números de arte para `Core/Scene/Art` (dueño: WP-D desde WP-D1)

> Los valores que cambió WP-D2 están en "Cambios por implementación de WP-D2" §4 (y en el código, con sus pruebas).

WP-A los crea con estos valores; WP-D2 los ajusta con las tomas y deja esta tabla al día.

| Archivo | Qué guarda | Dónde está el valor |
|---|---|---|
| `SkyArt` | arco del sol 10,5°, base 1,5°, oculto −2,5°, ventanas del sol y de la luna, radio del disco 1,6° y de la luna 1,28°, halos 6,4° y 5,1°, intensidad HDR `3 + 5e` y 1,6, resplandor (0,35; 14°; 20°), bruma del horizonte 1,5°, exponente del telón 2 (mezcla en sRGB), tramado ± 0,5/255, fundido de las estrellas 0,9 s, fondo de estrellas (celda 3°, alfa 0,12-0,30, radio 0,5-0,9 dp), nubes (atlas, fBm 0,18, luz interna, la vuelta en `1,3·s`), capa lejana (τ 2°-9°, cobertura 0,35, alfa × 0,6), ganancia de lo emisivo 1,6 | §3.3, §4, §5, §7.3 |
| `HorizonArt` (en `LakeArt`) | `PeakScreenFraction` **0,12** (del alto de pantalla: el `hMax` del legacy), tope 9°, D₀ y D_mín, flancos +12 % / −10 %, base 0,15, nieve 0,58 ± 0,03 con ruido de ± 0,04 y 1,7°, *alpenglow* 0,35, nieve de noche (0,55 + 0,45·il) | §6 |
| `LakeArt` | el corte de §7.1, las envolventes (0,20 y 0,06), las recetas de pino, bosque, cabaña, muelle, roca, juncos, pasto y helechos, las densidades y los LOD, los colores del terreno | §7 |
| `HourLookTable` | la tabla de §10.2 (con el balance de las 18 h en +15 / +4 y la noche en −10 / −2 y −10 de saturación, del gate), `VeilEv` −0,50, la bruma baja `m(h)` | §10, §12.4 |
| `WaterArt` | los agregados (reflejo 0,25 en 30-400 m; somera `#5A6656`, 0,25 en 8,4-9,0 m; bruma 0,6 en 10-150 m) | §8 |
| `MotionSpecs` (bloque 6a) | vuelo (puntos, focal ×1,25, horizonte de 0,6 a 1), entrada de la respiración en 0,6 s, toma aérea (1,70 / 1,70 / −4° / 6 s), rampas de la respiración en la pausa (300 / 240), minuto real 1 s | §11 |

---

## 18. Mixamo para la It. 7 (P-09)

**No hace falta ninguna animación de Mixamo para remar.** La remada es IK procedural sobre el rig Humanoid (ANIMATION_BIBLE §0.3). Un clip tiene un ritmo fijo (Q-70 lo pide variable con el SPM), despega las manos del puño, no se prueba con dotnet, y Mixamo no tiene *sculling* con carro. De Mixamo se usa **el personaje**.

**Cómo se baja y se importa**:

- Desde [mixamo.com](https://www.mixamo.com), con una cuenta de Adobe gratis.
- Cada archivo va a `Assets/ThirdParty/Mixamo/<Carpeta>/`, que no se versiona: la licencia permite usarlo en la app, no redistribuirlo suelto (ASSET_SLOTS §2).
- Se importa como dice ASSET_SLOTS §2.5: **Humanoid**, *Create From This Model* y **Optimize Game Objects apagado**.

| # | Archivo | En Mixamo | Opciones de descarga | Para qué | ¿Obligatorio? |
|---|---|---|---|---|---|
| 1 | `YBot/YBot.fbx` | **Characters** → `Y Bot` | **Format** `FBX for Unity(.fbx)` · **Pose** `T-pose` | Calibrar el IK y `RiggingFit` (ANIMATION_BIBLE §7.4): rig limpio, proporciones neutras y pesado parejo. Con él se fijan los números de la pose de agarre (`RowerPoseClipBuilder`) | **Sí**, antes de la 7 |
| 2 | `XBot/XBot.fbx` | **Characters** → `X Bot` | ídem | Un segundo rig con otras proporciones, para probar que `RiggingFit` ajusta solo la pedalera y el carro | Recomendado |
| 3 | `RowerRealistic/RowerRealistic.fbx` | **Characters** → el primero que pase la lista de control de ASSET_SLOTS §2.3 (ropa ajustada y deportiva, sin accesorios, pelo corto o recogido, 5 dedos con rig, proporciones adultas). El primero a revisar: `Kaya` | ídem | El aspecto final del remero. Si ninguno pasa, queda Y Bot con el mono pintado | Opcional (7 o 14) |
| 4 | `YBot/YBot@SittingIdle.fbx` | **Animations** → `Sitting Idle`, con Y Bot elegido (si no está con ese nombre, uno con el torso quieto y las manos sobre los muslos) | **Format** `FBX for Unity(.fbx)` · **Skin** `Without Skin` · **Frames per Second** `30` · **Keyframe Reduction** `none` | **Solo de referencia**, para comparar la respiración procedural y los dedos con un movimiento capturado. Nunca se reproduce (`rower.clip.idleRef`) | Opcional |

En la 7, el slot `rower.character` acepta el 1 o el 3. Si falta o no valida (Humanoid, dedos, alto de 1,55 a 2,05 m, ≤ 30 k triángulos), vuelve al maniquí procedural. La lista reemplaza a la "preliminar" de ASSET_SLOTS §2 en WP-D2.

---

## 19. D-ART-08: texturas CC0 y la luna de la NASA

**Por defecto, no** (P-01). El detalle y las licencias leídas están en [00-deuda-diseno §13](00-deuda-diseno.md).

- En el lago, la cercanía la mejora más el Terrain Sample Asset Pack que el CC0.
- La luna mide de 8 a 16 dp de radio y la procedural alcanza a ese tamaño.
- Sumar fuentes suma pasos y créditos.

Recomiendo volver a ofrecerlo en el gate de la 8, para las fachadas del río y los clubes. Si el usuario dice que sí, entra por `MaterialSlot` y `TextureSlot` sin tocar lógica, en `Assets/ThirdParty/CC0/`.

---

## 20. Pedidos al Architect

Los que siguen de la base ([00-deuda-diseno §14](00-deuda-diseno.md)) **sin cambios**: DD6a-01, -03, -04, -06, -08 a -12, -14, -16, -18 (adoptado), -21 a -25.

**Cambiados por esta nota**:

- DD6a-02 (el arco de 12° y el tope de 0,3°/min);
- DD6a-05 (las estrellas y las nubes del legacy, con el fundido y la vuelta de las nubes);
- DD6a-07 (el alto del legacy con tope de 9° y la nieve de §6);
- DD6a-17 (los degradés de pantalla, como capa del HUD: DN6a-25);
- DD6a-13 (la receta de §8);
- DD6a-15 (la salida de la spec, la escena demorada de 0,6 s);
- DD6a-19 (1,5 s);
- DD6a-20 (el casco solo);
- DD6a-26 (la bruma baja pasa a P1 porque la pide AC-SCN-15-07; la capa lejana, solo en Alto).

**Pedidos nuevos**:

| ID | Prioridad | Pedido | Verificación | ¿Cambia un AC? |
|---|---|---|---|---|
| **DN6a-01** | P0 | `HorizonSpec`: `PeakScreenFraction` 0,12 (del alto de pantalla, el `hMax` del legacy) con `MaxPeakDeg` 9: `tan Θ = mín(0,12·alto/focal; tan 9°)` (P-11, DV-65; §6.2; QA-6a-PAR-03). El perfil, como anillo de elevación: `tan(elevación) = tan Θ·p·k(d)` | dotnet: Θ = 9° en V390, TV y P360 y `tan Θ` = 0,12 (6,84°) en H844, TH, L640 y W1280; el macizo a 6,11° y 8,04°; la forma no depende de la ventana ni de `d` | AC-SCN-19-01 (ya aplicado; precisión en §21.4) |
| **DN6a-02** | P0 | `HorizonSpec.SnowThreshold` 0,58 con borde 0,03 y ruido de 0,04 cada 1,7° (§6.5) | dotnet: cobertura 45 % ± 3 en TV y 33 % ± 3 en TH | AC-SCN-19-01 |
| **DN6a-03** | P0 | `CelestialSpec.ArcDeg` 10,5 y el tope de velocidad del astro en 0,3°/min (C-01, DT6a-02) | dotnet: los valores de §4.1; ≤ 0,3°/min en el barrido | AC-SCN-10-04, -05 |
| **DN6a-04** | P0 | `LightMinIntensity` 0 (C-02) | dotnet: luz 0 a las 19:34:19 y a las 4:25:41 | AC-SCN-10-05 |
| **DN6a-05** | P1 | El fundido de las estrellas: `StarFader` (como `NightLightFader`), 0,9 s `ease.move` cuando `Visibility` pasa por 0, en el tiempo real del cuadro; se pone en su valor final al abrir, con `ForceHourForTest` y al volver del segundo plano | dotnet: en régimen, el alfa de la spec; a las 18:00, de 0 al valor en 0,9 s | No (precisión) |
| **DN6a-06** | P1 | `_RmShimmer.w` = la visibilidad del disco que se ve; el *shader* multiplica el alfa de las líneas. `DegradedWater.Line` no cambia (oráculo) | PlayMode: a las 19:34, 0 píxeles de línea; a las 12 y a las 22 h, igual que sin el factor | Precisión de AC-SCN-13-03 |
| **DN6a-07** | P1 | `ChaseCameraMotion.Aerial`: además, cabeceo de −4° con la misma curva, y el multiplicador de la respiración de 1 a 0 con `ease.move` en los 6 s | dotnet: la pose final (+1,70; −1,70; −4°) y la respiración en 0; CAP-22 con el bote en la zona | No |
| **DN6a-08** | P1 | `ChaseCameraMotion.Flight`: la focal arranca en ×1,25 la del contrato (no en la de 50°); la posición y la orientación con `ease-in-out-sine` (pico de 17,0 m/s en lugar de 32,5); la focal y el horizonte de la proyección siguen con `ease.move` | dotnet: la focal inicial, la pose final exacta y la velocidad máxima ≤ 17,5 m/s en 1 000 muestras | No |
| **DN6a-09** | P0 | El cielo de telón (§3.3): `q = τ/τ_top` con `τ_top = HorizonY/Focal` (ya son globales), `t = 1 − (1 − q)²`, **mezclado en sRGB** como `SkyBackdrop.Gradient(q)` de Core (QA-6a-PAR-10), con los tres globales del telón en sRGB (§14.1) | sondas de contrato de §10.4 (arriba `#0B4FA8` y a media altura `#2C70BE`, ± 3) | No |
| **DN6a-10** | P2 | `SceneHourTransition`: el cambio de minuto de la hora real va en 1 s lineal (no en el acto); con reducir movimiento, en el acto | dotnet: a los 0,5 s del cambio, la hora visible en la mitad | No |
| **DN6a-11** | P1 | `TerrainStreamer` y `FillStreamer` desde `d − 25 m` (vuelo y toma aérea); los objetos del legacy, igual que el contrato | PlayMode: sin huecos en el primer cuadro del vuelo | No |
| **DN6a-12** | P2 | El borde encendido de la cresta (§4.4): `HorizonModel.RidgeAt(acimut)` y `_RmRim` | toma a las 6 h | No |
| **DN6a-13** | P0 | `RowMateLighting.hlsl` con el modelo de §10.1 y `SceneLighting` con la luz y el ambiente de URP que lo igualan para lo de terceros | sonda de contrato (§10.4): un faldón del techo al astro y el frente a la sombra de la cabaña a las 12, 18 y 22 h, dentro de ± 25 % de `T(a)·{1,2; 0,65}` | No |
| **DN6a-14** | P0 | La envolvente del bosque de la izquierda con 0,20 (§7.1), en `LakeArt` | dotnet: nada del relleno fuera de la envolvente | No |
| **DN6a-15** | P1 | Las capas del pack en `Texture2DArray` al precargar, con `Graphics.CopyTexture`; validación de tamaño, formato y *mips* por capa | PlayMode con el slot de prueba: entra; con uno que no valida, el procedural y un `[SLOT]` | No |
| **DN6a-16** | P1 | Pasto, helechos y juncos como mallas opacas (§7.4), con el fin y la densidad del nivel | `SceneBudgetTests`: sobre-dibujo ≤ 2,5× en la franja | No |
| **DN6a-17** | P1 | Bosque cercano hasta `z` 120 y `\|x\|` 45 con Poisson de 6 m; más allá, la copa en la malla del terreno | `SceneBudgetTests`: triángulos ≤ 160 k en el peor tramo | No |
| **DN6a-18** | P1 | El atlas de nubes con la estructura de manchas del legacy (§5.2), horneado al precargar por filas | toma: CAP-02 con las 7 en su `x` y su `y` ± 1 dp (el centro de la base) | No |
| **DN6a-19** | P0 | `VeilEv` −0,50 y la sonda del velo de §10.4 (canal máximo y croma) | CAP-20 | Precisión de AC-WKT-13-12 |
| **DN6a-20** | P0 | La entrada de la respiración en 0,6 s al terminar el vuelo y después de la escena demorada (`S(0; 0,6 s)` sobre el multiplicador) | dotnet: sin salto de pose en el cuadro de los 1 820 | No |
| **DN6a-21** | P1 | El disco del sol en HDR (`3 + 5e`) y la luna (1,6), con *bloom* en Alto y Medio; en Bajo, plano | toma: el área con luminancia > 0,9 del sol de las 12 h ≥ 28 dp de diámetro en CAP-02 (V390, Alto), que son los 30 dp de TV con la focal de V390 (§21.4) | AC-SCN-11-05 (precisión, §21.4) |
| **DN6a-22** | P1 | La capa lejana de nubes en Alto (§5.4), segunda textura de `sky.clouds` | toma W1280 a las 12 y a las 18 h | No |
| **DN6a-23** | P0 | Los *shaders* de §14 con sus nombres, colas y palabras clave; `Prop` con la submalla de la ventana y `_RmLit` | `SceneAssetsTests`: los 13 encontrados; compilan en D3D11 y Vulkan | No |
| **DN6a-24** | P1 | Los tramados con *Bayer* fijo y el cambio de LOD complementario en el 10 % del límite (§7.6); el cercano de 2 a 1 m, de la lista del contrato (DV-62) | VID-03: ningún objeto cambia de golpe; dos corridas iguales byte a byte | No |

**Pedidos nuevos del gate** (QA-6a-PAR-02, -08, QA-G6aT-08, -12, -13):

| ID | Prioridad | Pedido | Verificación | ¿Cambia un AC? |
|---|---|---|---|---|
| **DN6a-25** | P0 | Los degradés de pantalla son de la capa del HUD (§12.3; DV-67): `HudView.HideForCapture` (o un interruptor propio del arnés) apaga también `ScreenOverlay`; con el HUD, a pleno, sin animación. La pasada de contrato (DN6a-27) tampoco los dibuja | PlayMode: en la escena sola, 0 dibujos del material de `ScreenOverlay` y el píxel (205; 8) de TV igual byte a byte al de un cuadro sin el *quad*; con el HUD, 1 dibujo | No (DV-67 ya lo dice) |
| **DN6a-26** | P0 | `CloudModel`: el alfa del cuerpo y de la sombra de cada nube × `V(φ) = S(0; 1,3·s/ancho; φ)·(1 − S(1,2 − 1,3·s/ancho; 1,2; φ))`, con `φ = (x₀ + t·0,004·v) mod 1,2` (§5.1). La `x` no cambia | dotnet: `V` = 0 en `φ` = 0 y en `φ` → 1,2; `V` = 1 lejos de la vuelta; en los siete tamaños, ningún cambio de alfa > 0,01 entre dos cuadros de 1/60 s; la `x` igual al oráculo | No (AC-SCN-11-06 y DV-56 ya lo dicen) |
| **DN6a-27** | P0 | **La pasada de contrato** del arnés (§10.4): el mismo cuadro de la toma con el post de la cámara apagado (sin el `Volume` de la hora: ni exposición, balance, saturación, contraste, viñeta, *bloom* ni *tonemapping*), `_RmEmissiveGain` en 1 y sin los degradés; las sondas de contrato al `manifest.json`. Solo en el arnés (`internal`); nunca en un build | el arnés: las sondas de contrato de §10.4 en `manifest.json`, iguales en dos corridas | No (AC-SCN-09-06, -11-02 y -20-02 ya dicen "antes del ajuste de color de la hora") |
| **DN6a-28** | P1 | Las estrellas tapadas por el astro en Core (QA-G6aT-08, si el Architect toma esa opción): `StarField` multiplica el alfa de cada estrella por `1 − alfa del halo` en su dirección y lo pone en 0 adentro del disco del sol o de la luna (§4.5) | dotnet: una estrella adentro del disco de la luna de las 0 h en TV → 0; a la mitad del radio del halo del sol, × 0,75 (el alfa 0,25 del legacy); fuera del halo, sin cambio | No |
| **DN6a-29** | P1 | `Sky` con el tramado *Bayer* fijo de ± 0,5/255 (§3.3, paso 8) y el *dithering* de la cámara de URP apagado (QA-G6aT-12) | dos corridas de CAP-04 iguales byte a byte; las sondas de contrato pasan con el tramado | No |

---

## 21. Para el PO y QA

### 21.1 Textos de AC que cambian si el gate acepta esta nota

**El PO ya los aplicó en el gate** ([01-po-spec](01-po-spec.md), "Cambios por gate QA" §1), con el alto de las cordilleras del legacy en lugar del 30 % (QA-6a-PAR-03): la fila de AC-SCN-19-01 quedó como dice §6.2. La tabla queda como registro; lo que falta está en §21.4.

| AC | Antes del gate | Con esta nota (aplicado) |
|---|---|---|
| AC-SCN-02-04 | rolido `0,18°` | `0,15°` (DT6a-01) |
| AC-SCN-10-04 | `1,5° + 15,5°·elevación`; 9:00 a 12,5°; 12:00 a 17° | `1,5° + 10,5°·elevación`; 9:00 a 8,92°; **12:00 a 12°**; 22:00 a 10,59°; 0:00 a 12° |
| AC-SCN-10-05 | "no se corre más de 0,2°"; luz con mínimo 0,05 | "no se corre más de **0,3°**" (DT6a-02); la luz vale **0** cuando cambia de dirección |
| AC-SCN-11-05 | el sol de ≈ 44 dp de diámetro y el halo de ≈ 176 en V390 | el disco de 1,6° de radio (21,8 dp en V390, 23 en TV) en HDR con *bloom*, que se lee de ≈ 40 dp; el halo, 4 veces el disco (6,4°), con los alfas del legacy. Las proporciones son las del legacy |
| AC-SCN-13-03 | alfa `0,05 + 0,1·(1 − t)·fuerza` | ídem en el dato; en pantalla, × la visibilidad del astro que se ve (las líneas no saltan en el cambio de sol a luna) |
| AC-SCN-13-04 | ≤ 12 ALU | ≤ 24 ALU (DT6a-07) |
| AC-SCN-19-01 | nieve en los picos sobre el 75 % (hasta el 82 % de la altura de cada pico); el pico más alto ≈ 30 % del cielo visible en vertical y en horizontal | nieve en todo lo que pasa el **58 %** del perfil lejano, con borde suave; el alto máximo del perfil, **el del legacy (12 % del alto de pantalla) con un tope de 9°**: en horizontal, 6,84° (como el legacy); en vertical, 9° (≈ 18 % del cielo) |
| AC-SCN-11-04 | aparecen a las 18:00 | ídem, entrando en 0,9 s (y saliendo igual a las 6:12): en régimen, la fórmula de la spec |
| CAP-03 (§7.3) | "el sol a la derecha (+17,5°) en el horizonte" | ídem; con `E-920` la cabaña a 40 m queda delante del disco: se ve en contraluz, con el halo |
| CAP-04, CAP-10, CAP-11 | la luna a ≈ 15°; a 17°; el sol a 12,5° | 10,6°; 12°; 8,9° |
| CAP-01 | "detrás o apenas sobre las montañas" | detrás del macizo (el disco sale entero a las 7:59 en vertical) |

### 21.2 Para el gate

- **P-11 decidido** (§6.2; DV-65).
- **P-10**: el fondo tenue de estrellas, solo en Alto.
- **P-17**: la tabla de §10.2, que ajusta WP-D2 (el gate ya bajó el balance de la tarde y el de la noche, §10.2).
- **C-18** (los *scrims*, DV-67, capa del HUD) y **C-19** (hemisferio sur): confirmados por el PO; el usuario los confirma con P-18.
- **D-ART-08**: no (§19).
- **DV-57** (la fase de la luna): se mantiene.

### 21.3 Para QA

- Las sondas de color (de contrato y de aspecto, §10.4) y de posición (§16).
- La excepción contada de las píldoras (§12.2).
- Las tomas de la 4b y la 5 que cambian solo por las píldoras `.hud` (02-tech-design §5.1).
- Los 60 fps en el teléfono quedan NO VERIFICABLE (§15.4).

### 21.4 Precisiones de texto que deja el gate (para el PO; menores, sin cambio de producto)

| AC | Hoy (spec del gate) | Pedido | Por qué |
|---|---|---|---|
| AC-SCN-19-01 | "el macizo … llega a ≈ 0,89 de ese alto: ≈ 25 % del cielo visible en horizontal y ≈ 16 % en vertical" | decirlo en ángulo: "≈ 0,89 de ese alto: ≈ 6,1° en horizontal y ≈ 8,0° en vertical" | El perfil es un anillo de elevación (§6.2): en la columna del macizo la perspectiva lo sube `1/cos 25,4°` y en pantalla mide el ≈ 28 % y el ≈ 18 % del cielo. El 25 % y el 16 % son la `τ` sin perspectiva: una sonda en dp no los encontraría |
| AC-SCN-11-02 | "[Captura] … con las sondas de color de arriba y del horizonte" | "con las sondas de contrato de arriba y de media altura (04-design-notes §10.4); el extremo del horizonte, en dotnet" | Cerca del horizonte el cielo libre depende de la orilla de cada distancia (en `E-920` la cabaña de 40 m tapa casi todo el collado hasta ≈ 5°, y en el resto del ancho están las montañas): la sonda a 1,6° no tenía un lugar fijo. La de media altura (`q` 0,5) prueba la curva y el espacio de color del *shader* contra Core; los extremos ya son [dotnet] (QA-6a-PAR-10) |
| AC-SCN-11-05 | "lo que pasa de 0,9 de luminancia mide ≥ 30 dp de diámetro en Alto, a las 12 h" (medido en TV) | "≥ 28 dp en CAP-02 (V390)" | No hay toma de escena sola en TV a las 12 h; CAP-02 ya es la del sol (V390, focal 390 contra 411 de TV: 30 × 390/411 = 28,5) |

---

## 22. Biblias: qué se alinea en WP-D2 (después del gate)

| Documento | Dónde | Qué |
|---|---|---|
| ART_BIBLE | §0.2 | D-ART-05, -06 y -08 resueltos |
| | §2.1 | `backdrop.*`: el fondo de carga teñido por la hora |
| | §3.3 | las píldoras con `.hud` en el HUD |
| | §3.2 | los *scrims* como capa del HUD, fuera de la escena sola (DV-67) |
| | §6.1 a §6.3 | §3, §4 y §10 de esta nota: el telón (en sRGB, con tramado), el arco de 12°, la luz 0 en el cambio, *Neutral*, la tabla de la hora sin la niebla por hora y con el balance de la tarde y de la noche del gate, el modelo de luz, las sondas de contrato y de aspecto |
| | §7.1 y §7.2 | la niebla común, el tramado cercano de 2 a 1 m, el corte del lago con la envolvente de 0,20, las cordilleras con el alto del legacy y tope de 9°, la nieve sobre el 58 %, el relleno |
| | §7.7 | el agua degradada de §8 (≤ 24 ALU) |
| | §8.1 y §8.2 | la matriz de Core, la respiración de 0,15° |
| | §9 | el reparto de §15 |
| | §10.1 | el riesgo de las píldoras, cerrado |
| ANIMATION_BIBLE | §2.11 | la entrada con el vuelo, la salida de la spec, el velo en el `Volume`, la toma aérea con cabeceo |
| | §2.12 | las estrellas con su fundido; la rotación (fundido de 200 en el *layout* nuevo) |
| | §5.2 y §5.3 | el rolido de 0,15°, el vuelo (puntos y focal ×1,25), la salida, la toma aérea; reducir movimiento con 120 ms (P-15) |
| | §6 | el viento de §5.5; las nubes (con la vuelta) y las estrellas |
| ASSET_SLOTS | §1.2 a §1.4 | `vegetation.canopyFar`, la capa lejana en `sky.clouds`, las capas del pack en arreglos |
| | §2 | la lista definitiva de §18 |
| | §7 | la recomendación de §19 |

---

## 23. Verificación hecha

1. **Legacy, solo lectura**: lo de la cabecera. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó `strava_secrets.dart`.
2. **Los números** de §3 a §16 salen de los programas de la cabecera, en `<scratch>/it6a/designer-notes/`:
   - la paleta con `Color.lerp` en `double`;
   - la cámara del contrato en los siete tamaños;
   - el perfil del legacy por acimut y las cuatro combinaciones de §6.2;
   - las posiciones de cada toma.

   Las fórmulas del legacy se copiaron de su código (`sky_painter.dart`, `shore_painter.dart`, `water_painter.dart`, `lake_environment.dart`). Los del generador y el paisaje del lago (92 visibles a 0 m, 102 en 100 km, los pinos por banda) son los de [00-deuda-diseno §17](00-deuda-diseno.md). Los de la respiración (2,932 dp) y la velocidad del astro (0,257°/min) son los de 02-tech-design.
3. **Lo que no se verificó**: el costo real de los *shaders* (se estimó; lo mide WP-D1 con el compilador) y el aspecto final. Ninguno de los dos se puede ver sin abrir Unity, y esta tarea es sin Unity. Los 60 fps quedan para el teléfono (regla 9).
4. **El repositorio**: se escribió solo este documento. No se tocó código, *tokens*, tomas, biblias, preferencias ni bases del usuario. Rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) o `../rowmate-legacy`, el scratch como `<scratch>`. Sin *commits*.
5. **En la corrección del gate** (2026-11-03): se volvió a leer el legacy en `3ae0f3a` (`sky_painter.dart:88-103`, `lake_environment.dart:40-50`, `time_of_day.dart:50-68`, `shore_painter.dart:106-124` y `:186-198`); `git -C ../rowmate-legacy status --porcelain` vacío antes y después. Los números nuevos salen de `<scratch>/it6a/designer-gate/` (cabecera): el generador de Dart reproduce los vectores dorados (`0.15092545597797424`, `0.6041479725361217`, `77`) y la primera estrella de V390 (58,8609; 173,3663); las cordilleras con `0,12·alto/focal` reproducen las cifras de la spec del gate (6:28 y 7:31 en horizontal; 6:53 y 7:59 en vertical; 3,7° y 5,0° de aire de la luna). Lo del ajuste de color de la hora (la ventana y el matiz) es un **modelo** del LUT HDR de URP con *Neutral*, sin *bloom*: orienta la tabla de §10.2; lo confirma WP-D2 con las tomas. Unity no se abrió y dotnet no se corrió.

---

## Cambios por gate QA

Gate de armonía: [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) (los dos, APROBADO CON OBSERVACIONES). Fecha: 2026-11-03. La spec ya estaba corregida por el PO ([01-po-spec](01-po-spec.md), "Cambios por gate QA", con lo que tocaba a estas notas en su §6): estas notas se alinean con ella. **No se disputa ningún issue.** En QA-G6aT-13 se toma la opción que eligió el PO (medir antes del ajuste) y no la de compensar la ventana por el post, y se explica por qué (punto 5).

### 1. QA-6a-PAR-02 (mayor): los *scrims* fuera de la escena sola

- **Qué estaba mal**: los degradés de pantalla (C-18) iban en la cámara de la escena y quedaban en las tomas de escena sola: oscurecían el 24 % de arriba y el 18 % de abajo de lo que se compara con LEG-01 a -07, y la sonda "Cielo de arriba" de §10.4 fallaba por construcción (en (205; 8) de TV, `#09397A`, ΔE OK 0,096 contra 0,05). Lo verifiqué: el *scrim* en `y` 8 vale `0,38·(1 − 8/219,36)` = 0,366, como dice QA.
- **Qué cambia**: son de la **capa del HUD** (DV-67, ya en la spec; ART_BIBLE §3.2). Los sigue dibujando la cámara de la escena al final, antes del post (0 elementos de UI; los alcanza el velo), pero los prende y los apaga el HUD: con `HideForCapture` y en la pasada de contrato **no se dibujan** (DN6a-25). Con el HUD, siempre a pleno y sin animación, así nunca cambian de golpe a la vista. Las sondas y la tabla de la hora se miden sin ellos y WP-D2 no los compensa con la exposición.
- **Además**: la sonda "Cielo de arriba" tenía otro problema que el gate no nombró. Medida en la toma, la viñeta de 0,18 oscurece ≈ 14 % el borde de arriba (en V390, ×0,864 en lineal) y con el ajuste de las 12 h la sonda quedaba en ΔE OK ≈ 0,043, al borde (`window.py`). Por eso las sondas de color pasan a ser de **contrato** (en una pasada sin el ajuste de la hora, ± 3 por canal) y de **aspecto** (el matiz, en la toma) (§10.4; DN6a-27).
- **La sonda del horizonte** (a 1,6° en el collado, TV) no tenía un lugar fijo: en `E-920` la cabaña de 40 m tapa casi todo el collado, y no hay toma de escena sola en TV a las 12 h. Pasa a la de **media altura** (CAP-02, `q` 0,5, `#2C70BE`), que prueba la curva y el espacio de color contra Core; el extremo del horizonte queda en dotnet (QA-6a-PAR-10). Para eso el telón se mezcla **en sRGB**, como `Color.lerp` y el degradé del legacy (§3.3; en lineal daría `#4B82C2`), con los tres colores del telón como globales en sRGB (§14.1).
- **Dónde**: §0 (puntos 10 y 12), §1.1 (C-18), §1.3, §3.3, §10.4 (reescrita), §12.3 (reescrita), §14.1, §14.2 (`Sky`, `ScreenOverlay`), §16 (condiciones), §20 (DN6a-09, -25, -27), §21.4 (AC-SCN-11-02), §22.

### 2. QA-6a-PAR-03 (menor): el alto de las cordilleras, el del legacy

- **Qué estaba mal**: `tan Θ = mín(0,30·horizonte/focal; tan 9°)` daba 7,18° en horizontal contra los 6,84° del legacy (`hMax = 0,12·alto`, `lake_environment.dart:46`; +5 %), y "el pico más alto ≈ 30 %" no era el del legacy (el perfil quieto llega a 0,89).
- **Qué cambia**: `tan Θ = mín(0,12·alto/focal; tan 9°)` (`PeakScreenFraction` 0,12; DN6a-01). En vertical no cambia nada (manda el tope). En horizontal, 6,84° exacto: recalculé §6.2 (tabla y combinaciones), §4.4 (el sol asoma a las 6:28 y sale entero a las 7:31; la cresta de las 6 h a 4,7°, la del centro a 4,4°; la luna de las 20 h detrás de 3,9°, entera a las 20:17; la de las 22 h con 5,0° de aire), §7.1 (la base de la nieve a 3,98° y la silueta del bosque, ahora en elevación: debajo de la nieve hasta −20,4° en horizontal), §12.1, §16.1 (CAP-05, CAP-09) y §17. Coincide con los números del PO (su punto 1.3).
- **Precisión de la perspectiva** (nueva, §6.2): el perfil es un anillo de elevación por acimut, como el disco del astro. En la columna del centro mide en pantalla lo del legacy; en la del macizo, ×1/cos 25,4°. El macizo queda a 6,11° y a 8,04°; en pantalla, el ≈ 28 % y el ≈ 18 % del cielo de su columna. El "≈ 25 %" y "≈ 16 %" de AC-SCN-19-01 son la `τ` sin perspectiva: pido decirlo en ángulo (§21.4).
- **De paso**: el collado (las dos cordilleras en 0) va de +14,6° a +18,2° y la cresta queda por debajo de 0,5° de +13° a +20° (decía "+13° a +21°" y "+12° a +22°"); el pico de la derecha (+58°) queda fuera del cuadro en todos los tamaños y en horizontal entra solo su flanco nevado (§6.1, §6.5, C-07).

### 3. QA-6a-PAR-07 (menor): la fuerza de las 19:34

- **Qué estaba mal**: §3.1 (b) decía 0,45 a las 19:34; al minuto el legacy todavía es de día (`isNight(19:34)` es falso, `time_of_day.dart:55`): 0,20.
- **Qué cambia**: la fila dice 0,20 (0,45 desde las 19:35) y una nota debajo de la tabla explica el umbral (19:34:19; al minuto, 19:35) y que las pruebas toman la fuerza del oráculo. §8.2 suma el dato del legacy a las 19:34 (0,07), que en pantalla sigue en 0 por la visibilidad del astro. Revisé las otras filas de la columna contra `time_of_day.dart:64`: 4 h, 0,45 (noche); 5 h y 19 h, 0,20 (día); 20 h, 0,45: bien.

### 4. QA-6a-PAR-08 (menor): las nubes en la vuelta

- **Qué estaba mal**: un hueco: las grandes asoman en el salto de ± 0,1·ancho y desaparecen y aparecen de golpe en V390, TV y P360. Además, §5.1 daba el extremo derecho en `x + 1,15·s`: es `x + 1,3·s` (la base mide 2,6·s, `sky_painter.dart:97`), y el tercer copo va en `y + 0,05·s`.
- **Qué cambia**: la `x` sigue siendo la del legacy y cambia el alfa: el cuerpo y la sombra × `V(φ) = S(0; 1,3·s/ancho; φ)·(1 − S(1,2 − 1,3·s/ancho; 1,2; φ))` (§5.1; DN6a-26 en `CloudModel`, con su prueba). La nube se disipa en el borde en 25 a 42 s; en los horizontales la vuelta pasa fuera del cuadro. Es la opción que eligió el PO (AC-SCN-11-06, DV-56). La capa lejana (Alto) queda periódica en el acimut, sin costura (§5.4).

### 5. QA-G6aT-13 (menor): la ventana encendida y el ajuste de la noche

- **Qué estaba mal**: la sonda (`#FFE082` ± 3 en la toma, sin *bloom*) no se sostenía con el post de la noche. Lo estimé con un modelo del LUT de URP: con el balance −25 / −4, la saturación −20, +0,6 EV y *Neutral*, la ventana sale `#E5E3CB` (croma OKLCH 0,032 contra 0,119 de `#FFE082`), y aun sin balance ni saturación, el *tonemapping* de un HDR × 1,6 la lleva a `#F0E6AD`.
- **Qué cambia**:
  - **La sonda de contrato** se lee en la pasada sin el ajuste de la hora y con la ganancia de lo emisivo en 1 (`_RmEmissiveGain`; DN6a-27): `#FFE082` ± 3, como dice la spec del gate (AC-SCN-09-06, -20-02: "su color antes del ajuste de color de la hora"). La ventana es `_RmWindowNight` (`#FFE082`, sin HDR) × la ganancia 1,6 (§7.3, §14.1, §14.2).
  - **Una sonda de aspecto** cuida lo que se ve: en CAP-04, matiz de 85° a 110° y croma ≥ 0,04 ("cálida, no blanca").
  - **La noche se pone azul con la paleta, no con el balance** (P-17): el balance de las 22-4 h pasa de −25 / −4 a **−10 / −2** y la saturación de −20 a **−10**; el de las 20 h, a −5 / −1 y −5. La ventana queda ≈ `#E9E3BD` (0,050) y el cielo y la orilla siguen azules (el matiz de `skyTop` se mueve 3,4°). Y el de la tarde baja de +20 / +6 a **+15 / +4**, porque la sonda de matiz nueva (media altura) se corría 8,7° a las 18 h (§10.2).
- **Por qué no la otra opción de QA** (compensar la ventana por el post para que dé `#FFE082` en pantalla): el PO ya fijó en la spec que `#FFE082` es el color antes del ajuste, que vale para toda la imagen como los colores de AC-SCN-15-06. Compensarla cambiaría ese contrato y haría depender un color de la tabla de la hora. La sonda de aspecto y la tabla nueva dan el mismo resultado visual (una ventana cálida en una noche azul) sin tocarlo.

### 6. Alineación con los otros documentos

- **Con la spec del gate** (su §6, lo que pedía a estas notas): §6.2 y §4.4 con la fórmula del legacy (punto 2); §7.6 con el tramado cercano **de 2 a 1 m**, de la misma lista del legacy `[d + 1; d + 400]` (DV-62 corregida; también §14.1 y DN6a-24); §12.3 sin los degradés en la escena sola (punto 1); §5.1 con el fundido en la vuelta (punto 4); §10.4 con la ventana antes del ajuste (punto 5); §3.1 (b) con la fuerza de las 19:34 (punto 3). También: las estrellas **blancas** (AC-SCN-09-04; §4.5), la hora al minuto armada como `hora + minuto/60` (§3.4), los 101 ID y DV-63 a DV-67 en la cabecera, y §21.1 marcada como aplicada.
- **Con el gate técnico, en lo que toca al arte** (los dueños son del Architect; acá queda lo que necesita WP-D):
  - QA-G6aT-08 (las estrellas sobre la luna): recomiendo taparlas en Core por estrella (DN6a-28; §4.5), sin pasada ni ALU de más; si el Architect elige la pasada propia del astro, §4.5 dice cómo cambian las colas.
  - QA-G6aT-12 (el bandeado de la noche): el tramado *Bayer* fijo va en `Sky` (§3.3, paso 8; DN6a-29) y el *dithering* de la cámara, apagado. `Sky` pasa de ≤ 35 a ≤ 41 ALU (§14.2); la estimación de GPU de §15.1 no cambia (0,4 ms).
- **Para el PO**, tres precisiones de texto menores (§21.4): AC-SCN-19-01 en ángulo, AC-SCN-11-02 con la sonda de media altura y AC-SCN-11-05 con la sonda del brillo en CAP-02 (≥ 28 dp en V390).
- **Para el Architect** (integra los "Pedidos al Architect" que no contradigan la spec y actualiza la lista de archivos por WP): DN6a-01, -09, -13, -21 y -24 cambiaron; DN6a-25 a -29 son nuevos (§20). Tocan `HudView`/`SceneHost` o el arnés (los degradés y la pasada de contrato), `Core/Scene` (`CloudModel`, `StarField`, `HorizonSpec`, `SkyBackdrop`), `Core/Scene/Art` (`HourLookTable`, `SkyArt`, `HorizonArt`) y los *shaders* `Sky`, `Prop` y `ScreenOverlay` con el *include* `RowMateScene.hlsl` (WP-D1).

---

## Cambios por implementación de WP-D1

WP-D1 quedó implementado con los archivos de [02-tech-design §7.3](02-tech-design.md): los cuatro *includes* (`Art/Shaders/Include/RowMate{Scene,Fog,Dither,Lighting}.hlsl`), los 13 *shaders* (`Art/Shaders/Scene/*.shader`), `Core/Scene/Art/**` (nuevos `MaterialArt`, `SkyTextures` y `ShaderArt`; cambian `SkyArt`, `LakeArt` y `WaterArt`), `Presentation/Motion/Hud/{SceneFadeMotion,HudReflowMotion}.cs` y `PauseVeilMotion.cs` (el peso de la escena). Pruebas: `Tests/EditMode/Scene/{SceneShaderContractTests,MaterialArtTests}.cs` (SSC, MAT y STX; corren en dotnet y en Unity) y `Tests/UnityEditMode/{SceneShaderTests,SceneMotionTests,SceneSkyProbeTests}.cs` (SSH, SMO y SKP). Rutas relativas a `Assets/RowMate/`. Las sondas de WP-B0 (S6a-1 a -7) todavía no corrieron: lo que depende de ellas está en el punto 8.

### 1. Lo que cuesta cada *shader* (medido, nivel Medio)

Instrucciones de cálculo del fragmento de la pasada `UniversalForward` compilada para Direct3D 11, contadas en su desensamblado (`D3DDisassemble`: Unity le saca la tabla STAT al DXBC), con lo que se usa en la sesión (instanciado, `RM_WATER_EXTRAS`) y sin las sombras de URP. Lo que está adentro de un bloque condicional (solo lo pagan los píxeles que entran) va aparte. `SceneShaderTests.EachFragmentFitsItsBudget` lo afirma contra `ShaderArt.Scene` y lo deja en el registro de la corrida (`[SHADER] …`).

| *Shader* | Cálculo | Bloque condicional | Texturas | Tope |
|---|---|---|---|---|
| `Sky` | 40 | 43 (el halo y el disco: solo a menos de 6,4° del astro) | 1 (la luna, adentro del bloque) | 41 (§14.2 corregida por el gate; 02-tech-design §7.5 todavía dice 35: lo alinea el Architect); el bloque, ≤ 48 |
| `Stars` | 7 | — | 0 | 8 (con la máscara del astro, DT6a-17) |
| `Clouds` | 8 | — | 1 | 12 |
| `Horizon` | 13 | — | 0 | 15 (con el borde encendido, P2) |
| `WaterDegraded` | **24** | — | 0 | **24** (DT6a-07) |
| `Terrain` | 27 | — | 2 | 35 |
| `Prop` | 20 | — | 0 | 30 |
| `Foliage` | 12 | — | 0 | 25 |
| `Grass` | 6 | — | 0 | 18 |
| `Reeds` | 12 | — | 0 | 18 |
| `Hull` | 21 | — | 0 | 25 |
| `BoatShadow` | 5 | — | 0 | 5 |
| `ScreenOverlay` | 4 | — | 0 | 4 |

Solo Alto (no entra en el tope de Medio, que es el de la tabla de 02-tech-design §4.1): `Sky` con el fondo tenue de estrellas, 82 + 43; `Clouds` con la capa lejana, 9 + 5 y 2 texturas. El Alto es el escritorio; si WP-D2 lo quiere más barato, el fondo tenue es lo primero.

**Cómo entran en el presupuesto** (la regla para todos): la luz, la niebla, la bruma baja, el viento, los colores de las zonas del terreno y los agregados del agua se calculan **en el vértice** (son suaves) y el fragmento solo mezcla. El agua dobla sus tres agregados en un solo `c·k + C` pesado por vértice (por eso la grilla del plano, `WaterArt.GridX` × `GridZ`, tiene columnas en `|x|` de 8,4 a 9,0 y filas en 10, 30, 150 y 400 m); el terreno arma sus zonas en cinco coeficientes por vértice (`c = A0 + A1·cantos + A2·pasto + A3·agujas + A4·copa`); las nubes reciben la luz de cada borde del quad hecha en el vértice; el tramado pasa a un par por vértice y el fragmento hace un producto y el `clip`.

### 2. El modelo de luz y los espacios de color

- **La luz es un multiplicador del espacio del legacy** (§10.1): `T(a) ⊙ [0,65·Hemi(N) + 0,55·Kn·envolvente·sombra]`, con `_RmAmbient*` = `HourLook.Ambient*` (ya con `T(a)` y el 0,65) y la clave `T(a) ⊙ Kn·0,55` armada en el vértice desde `_RmPaletteSunColor` y la intensidad `I` (`_RmLightDir.w`). Pasa al lineal del *pipeline* con la potencia 2,2.
- **Se sombrea en "gamma 2"** (√ del lineal y al cuadrado una vez al final): un producto y un `lerp` ahí se parecen a los del legacy (el tinte y `Color.lerp` de la niebla, en sRGB) a ± 1 nivel en los tonos medios, por una instrucción. La niebla, la bruma baja, el agua y los objetos mezclan así.
- **El cielo es exacto**: el telón se mezcla en sRGB (`_RmSkyTopSrgb`, `_RmSkyHorizonSrgb`, `_RmHazeSrgb`) y pasa a lineal con la EOTF sRGB completa: las sondas de contrato de §10.4 lo piden (con gamma 2 la de media altura daba + 5 en el rojo).
- **Las mezclas con alfa sobre la escena** (la sombra del bote y los degradés de pantalla) se componen en lineal: su alfa pasa a `1 − (1 − α)^2,2` en el vértice, que oscurece como el mismo alfa en sRGB (el del legacy y el del HUD).
- **La sonda del cielo, en la GPU** (SKP): `Sky` dibujado con las matrices de Core en V390 a las 12 h da arriba `#0B4FA8` ± 3 y a media altura `#2C70BE` ± 3, y la bruma debajo del horizonte. Control positivo en la misma tanda: con el telón lineal del legacy (`1 − r` en lugar de `1 − r²`) la sonda de media altura falla (rojo 77 contra 44); el *include* se restauró byte a byte.

### 3. Precisiones del contrato de §7.5 (para `SceneShaderIds` de WP-B0 y para WP-B1)

| Qué | Cómo quedó | Por qué |
|---|---|---|
| Globales nuevos | `_RmSkyTopSrgb`, `_RmSkyHorizonSrgb`, `_RmHazeSrgb` (sRGB, con `SetGlobalVector`) y `_RmEmissiveGain` (1,6; 1 en la pasada de contrato) | Ya estaban en §14.1; faltaban en la tabla de 02-tech-design §7.5 |
| Semántica de los globales | `_RmPalette*` en lineal (`SetGlobalColor`); `_RmAmbient{Sky,Equator,Ground}` = `HourLook.Ambient*` sin convertir (`SetGlobalVector`); `_RmLightDir` = (dirección hacia la luz; `w` = `CelestialState.LightIntensity`); `_RmDiskShape.w` = `CelestialPlacement.DiskHdr` (1 en Bajo: sin HDR); `_RmZonePhase` = (`frac(d/1,3)`; `frac(d/7)`; −; −) calculada en `double`; `_RmDensity` sin poner (0) cuenta como 1 | El modelo de luz y el ruido de cinta los necesitan; así no hace falta otro global |
| Palabras clave nuevas | `RM_SKY_STARFIELD` (`Sky`) y `RM_CLOUD_LAYER` (`Clouds`), `shader_feature_local`, prendidas solo en Alto por `SceneQualityApplier`, fijas en la sesión como las de sombras (QA-G6aT-07). La colección de variantes de WP-B1 las suma en Alto | El fondo tenue y la capa lejana son solo de Alto (§15.2): con un `if` los pagaba también Medio |
| Terreno | nueve colores con nombre (`_RmTerrainPebbles`, `…Wet`, `…Contact`, `…Grass`, `…GrassAlt`, `…Forest`, `…Needles`, `…Rock`, `…Canopy`) en lugar del arreglo `_RmTerrainColors` | Un arreglo del material no se guarda en el `.mat` y sin código queda negro; con propiedades, el material recién creado ya tiene los colores de Art |
| Follaje | los nueve verdes del legacy como constantes del *shader* (`kRmPineGreens`, iguales a `LakeArt.PineGreens`: SSC lo compara) en lugar de `_RmPalettes`; el tronco y los arbustos como propiedades; `_RmVariant` = (paleta 0 a 2; luminancia: 1 el legacy, 0,85 el bosque; arbusto 0/1; −) | Son colores del contrato: no se ajustan por material |
| Pasto y juncos | `_RmGrassColorA`/`B`, `_RmFernColor` (`#3F6A35`, nuevo en `LakeArt.Fern`), `_RmGrassEnd` = (fin; 0,2; −; −); `_RmReedBase` y `_RmReedTip`; `_RmTint` = (brillo ± 8 %; verde A → B; helecho 0/1; número fijo de la mata para la densidad); `_RmWind` del material = (amplitud; frecuencia; ráfaga; período), los de `MaterialArt` | El nombre del contrato no decía la forma |
| Casco | los cuatro colores van en el color de vértice de `HullMesh` (WP-A): sin `_RmHullColors` | La malla ya los trae |
| Cabaña | la ventana es la submalla 1 de `CabinMesh`, dibujada con otro material del mismo *shader* con `_RmWindowMode` = 1 | Un *shader* para los dos, sin otra palabra clave |
| Cordilleras | `_RmHorizonScale` = (tan Θ; k(d); alto relativo: 1 el lejano, 0,7 el cercano; −) por material; `_RmSnowTone` (`#F2F6FA`); `_RmHorizonTone` = `HorizonModel.FarColor` o `NearColor` de la hora (lineal) | La tira de Core guarda el perfil sin el 0,7 |
| Agua | `_RmShimmer` = (fase de Core; x del disco; fuerza; vis): el *shader* centra las líneas en la x del disco **del cuadro** (`_RmDiskDir` proyectada), que con la cámara del contrato es `_RmShimmer.y`; el horizonte también sale de las matrices del cuadro, así en el vuelo y en la toma aérea las líneas siguen al agua. `_RmWaterExtras` = (0,25; 0,25; 0,6; −); las distancias, constantes de `WaterArt` | §8.1 no decía contra qué horizonte en el vuelo |
| Sombra del bote | `_RmShadowOffset` = (0,35; 8,6; 0,9; 4) y `_RmShadowAlpha` = 0,08; el lado sale de `_RmDiskDir` y el ambiente de `_RmAmbient` | WP-B1 no pone nada por cuadro |
| Degradés | `_RmScrimColor` (`#06122A`) y `_RmScrimTop`/`_RmScrimBottom` = (alfa; fracción del alto; −; −) | Un `float4` no lleva el color y dos números |
| Por instancia | `_RmStarDir` = (dirección; radio en rad) y `_RmStarAlpha` (el de `StarInstance`); `_RmCloudDir` = (dirección; s en rad), `_RmCloudVariant` (0 a 3; < 0 la capa lejana) y `_RmCloudAlpha` = (cuerpo; sombra; −) **ya con `WrapFade`**, como los da `CloudInstance`; `_RmFade` = `MaterialArt.InstanceFade(relZ)` × el par de `LodFadePair` (el signo se conserva); `_RmLit` = el nivel de la luz de esa cabaña (`SceneFrame.PropLight`); `_RmWindPhase` = (fase en rad en [0; 2π) de `MaterialArt.*WindPhase`; escala de la amplitud: 1, o 1,5 las matas) | — |

### 4. Lo que WP-B1 tiene que saber para armar la escena

- **Mallas**: `Sky` y `ScreenOverlay` arman el clip con la UV0 (de 0 a 1): sirve el Quad de Unity, con *bounds* que el *culling* no recorte. `Stars`, `Clouds` y `BoatShadow` usan el Quad de Unity (la sombra la arma el vértice alrededor del centro del casco, a ras del agua). El agua, la grilla `WaterArt.GridX` × `GridZ` centrada en la cámara.
- **`TerrainChunkMesh` (WP-A) escribe la normal (0; 1; 0)**: WP-B1 tiene que recalcular las normales al subir el trozo (`Mesh.RecalculateNormals`), si no el talud no se ilumina y la roca no aparece por la pendiente.
- **Texturas**: las recetas de `SkyTextures`, puras y por filas: el atlas RG8 (R el cuerpo, G la sombra; la celda de la variante `k` en `(k mod 2; k / 2)` contada desde arriba: el texel `(x; y)` va en la fila `256 − 1 − y` de la `Texture2D`), la luna (el albedo en R, `v` hacia arriba), el ruido RGBA sin costura y la capa lejana R8 (solo Alto).
- **Materiales** (`SceneMaterialsBuilder`): los valores por defecto de los *shaders* ya son los de Art (SSC lo afirma); `RM_WATER_EXTRAS` prendida en los tres niveles; en Alto, `RM_SKY_STARFIELD` y `RM_CLOUD_LAYER`; la capa lejana es una octava instancia de las nubes con `_RmCloudVariant` = −1.
- **Pasadas**: `UniversalForward` en los 13; `ShadowCaster` en `Prop`, `Foliage` y `Hull` (el terreno, el pasto y los juncos no proyectan); `DepthOnly` en los opacos. Las sombras, con `_MAIN_LIGHT_SHADOWS`/`_CASCADE` y `_SHADOWS_SOFT*` (sin la de pantalla).

### 5. Lo que cambió del arte de estas notas al implementarlo

| Dónde | Qué | Por qué |
|---|---|---|
| §5.3 nubes | La luz de arriba y el brillo dorado de abajo son un degradé vertical del quad (armado en el vértice) en lugar de los escalones en el 0,6 y el 0,4 del alto | Los escalones costaban 8 instrucciones por píxel; el degradé se ve más suave |
| §5.4 capa lejana | Un plano de ± 65° con la textura en `tan(acimut)·0,5`: se estira hacia los costados, como un cirro | Un plano instanciado entra en el mismo *draw call* de las nubes |
| §4.4 borde encendido (P2) | Un degradé por columna, más fuerte en la cresta, cerca del acimut del sol en la hora dorada, en lugar de "los últimos 0,35°" | Entra en los 15 de `Horizon`; el exacto queda para WP-D2 si la toma de las 6 h lo pide |
| §6.4 relieve fino (Alto, P2) | No se hizo | Pasa a la 6b |
| §6.5 nieve | La máscara de Core por columna (la que mide la cobertura de 45 % y 33 %) × una línea de nieve local en el 58 % con lenguas de un ruido del *shader* (± 0,04 cada 1,7°) | El ruido de Core (SplitMix64, de 64 bits) no corre en GLES 3; la cobertura sigue siendo la de Core |
| §7.3 tablas | El borde de la línea mide un píxel (con la distancia, en el vértice) y la línea se apaga cuando mide menos de medio píxel | `fwidth` y el promedio costaban 6 instrucciones más |
| §7.4 pasto | El fin y la densidad de la adaptación lo achican en el vértice, sin `clip` | Sin *alpha test* ni `discard` en la franja con más sobre-dibujo |
| §7.5 terreno | La franja húmeda refleja `skyHorizon` con 0,15·(0,2 + 0,8·Fresnel); las demás *smoothness* de la tabla no tienen especular | El modelo de §10.1 es difuso |
| §8.1 agua | Lo del legacy y los agregados se mezclan en gamma 2, no en sRGB: los tramos y las líneas quedan a uno o dos niveles de los del legacy | El paso a lineal exacto no entra en los 24 |
| §14.2 `Stars` | La máscara del astro se evalúa en el vértice (la estrella mide menos que el borde del disco) | 7 de 8 |
| §14.2 `ScreenOverlay` | El alfa ya en lineal por vértice; el fragmento es un `max` | 4 de 4 |

### 6. Los `IMotion` de la escena (02-tech-design §7.4)

- `SceneFadeMotion(driver, apply, finished)`: `Play()` (o `Play(true)`, en el acto) lleva la opacidad de `scene-surface` de 0 a 1 en 600 ms con `ease.enter` (`SceneTimelines.LateScene`) y avisa una vez al terminar. Con reducir movimiento, igual (es un fundido).
- `HudReflowMotion(driver, apply)`: `OnOrientationClassChanged()` lleva la opacidad común del HUD de 0 a 1 en 200 ms (`SceneTimelines.HudReflow`), desde donde esté si llega otro cambio; con reducir movimiento o `Instant`, en el acto. Solo para un cambio de clase (DT6a-06; el PO todavía no lo registró en AC-WKT-17-07).
- `PauseVeilMotion(driver, veilAlpha, sceneWeight)`: el segundo *callback* (opcional) recibe el peso de la escena, `alfa/0,15`, en el mismo cuadro; `SceneWeight` lo da en cualquier momento. Sin el segundo *callback*, igual que en la 4b.
- Los tres heredan `HudMotion`: `Stop()` los saca del `MotionDriver` en el acto, sin avisar ni volver a aplicar nada (ROB-02: SMO lo afirma con un segundo de cuadros después de `Stop`). Sin asignaciones por cuadro.

### 7. Verificación

Las corridas (dotnet con el *wrapper* y Unity EditMode y PlayMode, con y sin simulador) están en el informe al coordinador. Lo propio de WP-D1: los 13 *shaders* sin errores y con su cola; compilan para Direct3D 11, OpenGL Core, Vulkan, GLES 3 y Metal (el compilador cruzado del Editor de Windows) con sus variantes (la base, la instanciada, la de sombras y las propias); el costo de §1; la sonda del cielo en la GPU con su control positivo; los `IMotion`.

**NO VERIFICABLE**: el costo con el *Mali Offline Compiler* (no está en el equipo: se midió con el código de Direct3D 11, como admite §14.2); el dibujo en GLES y en Metal en un equipo (sin teléfono ni Mac); cómo se ve la escena armada (es de WP-B1 y de las tomas de WP-B3).

### 8. Lo que depende de las sondas de WP-B0

- **S6a-1 (el color)**: los *shaders* escriben en lineal y dejan la conversión a sRGB a la RT `R8G8B8A8_SRGB`. Si la sonda manda el respaldo (RT `UNorm` con la conversión en el último pase), los *shaders* no cambian: la conversión va en esa pasada.
- **S6a-2 (el dibujo a pedido)**: las propiedades por instancia van en `UNITY_INSTANCING_BUFFER` con `multi_compile_instancing`; con el respaldo (`CommandBuffer.DrawMeshInstanced` en un pase propio) es lo mismo.
- **S6a-6 (la matriz)**: `Sky` y `ScreenOverlay` arman el clip con la UV y `_ProjectionParams.x`, y `Sky` y `WaterDegraded` leen `UNITY_MATRIX_I_VP`, `UNITY_MATRIX_VP` y `UNITY_MATRIX_V`: con `Camera.projectionMatrix` = la de Core, URP los pone solos. SKP lo prueba con las matrices de Core sin la vuelta de la RT; la de URP (`_ProjectionParams.x` = −1) la cubre la sonda.
- **S6a-4 (el determinismo)**: ningún *shader* lee el tiempo de Unity; los tramados son Bayer fijos; el fondo tenue y las lenguas de nieve usan un *hash* entero (sin seno): iguales en dos corridas.

---

## Cambios por implementación de WP-D2

WP-D2 (el pulido) quedó implementado con lo que pide [02-tech-design §7.3](02-tech-design.md): los ajustes con las tomas de `it6a`, las biblias ([ART_BIBLE](../../design/ART_BIBLE.md), [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) y [ASSET_SLOTS](../../design/ASSET_SLOTS.md), cada una con su "Cambios de la It. 6a") y la lista de Mixamo para la 7 (§7 de esta sección). Una primera corrida de WP-D2 se cortó por un error del servidor a mitad del trabajo, con cambios sin versionar; se revisaron uno por uno contra una copia de los originales (en `<scratch>`), **se conservaron todos** y se siguió desde ahí. Rutas relativas a `Assets/RowMate/`.

Archivos: `Core/Scene/Art/{HourLookTable,SkyArt,SkyTextures,MaterialArt,LakeArt,ShaderArt}.cs`, `Core/Scene/HourLighting.cs`, `Core/Scene/Geometry/PropMeshes.cs` (la cubierta del casco, D2-02), `Art/Shaders/Include/{RowMateScene,RowMateLighting}.hlsl`, `Art/Shaders/Scene/{Sky,Clouds,Horizon,Hull}.shader`, `Scene/SceneLighting.cs` (el modo de tonemapping de Art), `Scene/SceneRenderer.cs` (D2-01), las pruebas `Tests/EditMode/Scene/{HourLightingTests,MaterialArtTests,SceneGeometryTests,SceneSessionTests}.cs`, las tomas de `snapshots/` (regeneradas), las tres biblias y este documento.

### 1. Lo que mostraban las tomas (B6a-22) y lo que se hizo

| # | Qué se veía | Qué se cambió | Cómo quedó (corrida publicada) |
|---|---|---|---|
| 1 | La imagen más oscura y menos saturada que LEG-01 a -07 | **Sin tonemapping** (`HourLookTable.NeutralTonemapping = false`; `SceneLighting` lo lee): la paleta del legacy ya es un color de pantalla y *Neutral* la comprimía (el horizonte de las 12 h `#8FD3FF` salía `#90BED4`). La exposición vuelve a 0 a toda hora (−0,10 a las 5) y el contraste a la mitad (con la paleta sin comprimir, +8 recortaba el horizonte) | Sondas de contrato del cielo de CAP-02, diferencia 0; el agua de CAP-02 `#228BD7` / `#105496` contra `#2E8DD3` / `#145A9B` de LEG-02; la cabaña `#493835` contra `#4E342E` |
| 1b | La orilla a la mitad del legacy | El modelo de luz (§10.1) con **0,80** de ambiente y **0,45** de clave: el astro está siempre adelante (acimut ± 35°) y lo que mira a la cámara casi siempre está a la sombra; con 0,65 la orilla salía a la mitad. El ambiente con el **20 %** del croma del cielo (`AmbientHue`): con el matiz entero un cielo saturado multiplicaba el azul por 2,4 y el rojo por 0,16 (la cabaña azul marino, los pinos verde petróleo). La envolvente del follaje en **0,50** | Una cara al astro ≈ 1,25 del tinte, una a la sombra ≈ 0,80 (`MaterialArtTests`, `HourLightingTests.TheAmbientKeepsTheColorOfWhatItLights`) |
| 2 | Las nubes grises, más grandes que en el legacy y **claras de noche** | El *quad* de cada nube en el plano de pantalla de la cámara del contrato (mide en dp lo del legacy a cualquier elevación; tangente al cielo se estiraba × 1,22 de ancho y × 1,49 de alto a los 35°). El alfa del legacy, que se mezclaba en sRGB, pasa al alfa lineal equivalente contra el telón de su vértice (`RmDisplayAlpha` en `RowMateScene.hlsl`): de noche la nube del 14 % pesa el 2 %, como en el legacy. El halo del astro se mezcla en gamma 2 (en lineal su borde era un disco gris de borde duro) | CAP-02 `#B6C4DF` contra `#B8D4ED` de LEG-02; CAP-04 `#242835` contra `#2A2F43` de LEG-04 |
| 3 | Las montañas bajas y lavadas | El color ya es el del contrato exacto (`#B3CDE7` contra `#B2CAE2` de LEG-02); el alto en vertical es el del gate (9°, DV-65). Dos arreglos del *shader* `Horizon`: los flancos cambian de lado en **± 6°** alrededor del acimut del sol (`LakeArt.FlankRampDeg`; con el signo seco quedaba una costura vertical de 4 niveles debajo del sol de las 12 h) y el borde encendido **lleva el color hacia `sunGlow`** en lugar de sumarse (sumado, la nieve con el *alpenglow* pasaba de 1 y el macizo de las 6 h era una mancha blanca). 14 de 15 ALU | Sin costura en CAP-02, CAP-05 y CAP-07; el macizo de CAP-01 con la cresta dorada y sin recorte |
| 4 | El brillo del sol de las 12 h: 22,9 dp (mínimo 30) | El *bloom* de las 12 h en **1,30 / 1,4** (sin tonemapping el disco HDR se recorta en 1 y el *bloom* hace el resto) | 38 dp en CAP-02 (mínimo 28, §21.4) y 30,5 dp en `it6a-brillo` (TV) |
| 5 | El casco como un palo celeste (la cubierta con el color de la franja) y, sin tonemapping, una silueta blanca plana al mediodía | La cubierta con sus propios vértices en las dos bordas, blancos (o del *cockpit*): la línea de borda queda en el costado (D2-02). El casco con **× 0,78** de exposición (en gamma 2, en el vértice) y la **mitad** del croma del cielo en su ambiente (`MaterialArt.HullExposure`, `HullAmbientChroma`) | A las 12 h la cubierta queda en 0,80-0,95 (`MaterialArtTests.TheHullDeckStaysWhiteBelowClippingAtNoon`): `#E1F1FE` (un blanco apenas frío, con el especular encima); a las 6 h, `#8F8C9F`, un gris azulado (era lavanda, `#B0AEE0`) |
| 6 | El agua apagada contra el degradé de LEG | Lo resolvió el punto 1 | Ver 1 |
| — | La ventana de CAP-04 verde amarilla (matiz 114°, la sonda pide 85°-110°) | **La ganancia de lo emisivo en 1** (era 1,6 en estas notas y 1,25 en la primera corrida): sin tonemapping el rojo de `#FFE082` ya está en 1 y cualquier ganancia lo recorta antes del balance frío; con 1 pasa el umbral del *bloom* de la noche (0,8) (`MaterialArtTests.TheLitWindowDoesNotClipItsHue`) | `#EDE09A`: 98,9°, croma 0,090 |
| — | La luna de CAP-04 se leía como una cara (mares de 0,25 a 0,45 en 8,7 dp de radio) | Mares claros de **0,68 a 0,82** sobre tierras altas de **0,94** | Un disco claro con manchas |

### 2. La tabla de la hora final (`HourLookTable.Default`; reemplaza la de §10.2)

| Keyframe | Sombras (Medio / Alto) | Exposición | Balance (temp / tinte) | Saturación / contraste | *Bloom* (intensidad / umbral) |
|---|---|---|---|---|---|
| 5 h | 0,55 / 0,55 | −0,10 EV | +15 / +5 | 0 / 0 | 0,70 / 1,0 |
| 7 h | 0,75 / 0,75 | 0 | +8 / +2 | +2 / +2 | 0,45 / 1,1 |
| 12 h | 0,85 / 0,85 | 0 | 0 / 0 | +4 / +4 | **1,30 / 1,4** |
| 18 h | 0,65 / 0,65 | 0 | +15 / +4 | +6 / +2 | 0,75 / 1,0 |
| 20 h (luna) | 0 / 0,35 | 0 | −5 / −1 | −5 / 0 | 0,85 / 0,9 |
| 22-4 h (luna) | 0 / 0,25 | 0 | −10 / −2 | −10 / −5 | 0,95 / 0,8 |

Tonemapping: ninguno. Viñeta 0,18 / 0,4. **`VeilEv` −0,17**, saturación del velo × `(1 − 0,30·w)`. La intensidad de la luz sale del astro (§3.2), sin cambios.

### 3. Las sondas (04-design-notes §10.4, medidas en la corrida publicada)

| Sonda | Clase | Medido | Pasa si | |
|---|---|---|---|---|
| Cielo de arriba, CAP-02 (195; 8) | contrato | `#0B4FA8` (diferencia 0, en el *manifest*) | ± 3 | sí |
| Cielo a media altura, CAP-02 (195; 168,8) | contrato | `#2C70BE` (diferencia 0) | ± 3 | sí |
| Matiz de la hora, CAP-01 (6 h) | aspecto | arriba `#30448A` 268,7° contra 267,5° de Core; media altura `#646189` 287,4° contra 282,8° | ≤ 8° | sí (1,2° y 4,6°) |
| Matiz de la hora, CAP-03 (18 h) | aspecto | arriba `#2A3780` 271,4° contra 271,4°; media altura `#634C79` 307,1° contra 300,7° | ≤ 8° | sí (0° y 6,4°) |
| Matiz de la hora, CAP-04 (22 h) | aspecto | arriba `#050A1E` 268,3° contra 268,3°; media altura `#0A1027` 270,1° contra 269,6° | ≤ 8° | sí (0° y 0,5°) |
| Ventana, CAP-04 | aspecto | `#EDE09A`: 98,9°, croma 0,090 | 85°-110°, ≥ 0,04 | sí |
| Velo, CAP-20 (TV, (205; 128)) contra CAP-13 (el mismo cielo sin pausa) | aspecto | canal máximo 161 → 137 (× 0,851); croma 0,127 → 0,092 (× 0,724) | × 0,85 ± 0,03; × 0,70 ± 0,05 | sí |
| Velo, CAP-21 (TH, (457; 60)) contra CAP-14 | aspecto | 163 → 139 (× 0,853); 0,128 → 0,093 (× 0,727) | ídem | sí |
| Brillo del sol, CAP-02 (V390) | aspecto | 38 dp con luminancia > 0,9 | ≥ 28 | sí |
| Brillo del sol, `it6a-brillo` (TV) | aspecto | 30,5 dp (en el *manifest*) | ≥ 30 | sí |
| Luz de la cabaña, CAP-02 (el frente, a la sombra) | contrato | **no medida en la pasada de contrato** (D2-03); en la toma, `#493835` contra el albedo `#6D4C41` × 0,80 = `#573D34`: −16 % / −8 % / +2 % | ± 25 % | sí, en la toma |
| Ventana `#FFE082`, CAP-04 | contrato | **no medida** (D2-03): `SceneMarks.Contract` solo tiene las del cielo de CAP-02 | ± 3 | — |

Las de aspecto las mide WP-D2 en los PNG publicados con una herramienta propia en `<scratch>` (la mediana de un parche de 5 × 5 dp, como §10.4; el matiz de Core es `SkyBackdrop.Gradient(q)` de la paleta de esa hora); el arnés deja en el *manifest* solo las de contrato del cielo y el brillo de `it6a-brillo` (D2-03).

### 4. Lo que cambió de los números de arte (§17)

| Constante | En estas notas | Final | Dónde |
|---|---|---|---|
| Tonemapping (R6a-04) | *Neutral* | ninguno | `HourLookTable.NeutralTonemapping` |
| Exposición, contraste, *bloom* de las 12 h | §10.2 | §2 de esta sección | `HourLookTable.Default` |
| Pesos del modelo de luz | 0,65 / 0,55 | **0,80 / 0,45** | `MaterialArt.HemiWeight`, `KeyWeight`; `HourLookTable.AmbientScale`, `KeyLightScale`; `kRmKeyWeight` |
| Croma del cielo en el ambiente | 1 (entero) | **0,2** | `HourLookTable.AmbientHue` |
| Envolvente del follaje | 0,35 | **0,50** | `MaterialArt.FoliageWrap`; `kRmFoliageWrap` |
| `VeilEv` | −0,50 | **−0,17** | `HourLookTable.Default` |
| Ganancia de lo emisivo | 1,6 (hasta 1,25) | **1** | `SkyArt.EmissiveGain` |
| Mares de la luna / tierras altas | 0,25-0,45 / 0,86 | **0,68-0,82 / 0,94** | `SkyArt.MoonMaria*`, `MoonHighlands`; `SkyTextures` |
| Flancos de las cordilleras | signo seco | **rampa de ± 6°** | `LakeArt.FlankRampDeg`; `kRmFlankRampRad` |
| Borde encendido (P2) | sumado | **mezcla hacia `sunGlow`** con la misma fuerza (0,6) | `Horizon.shader` |
| Casco | — | **exposición × 0,78; croma del ambiente × 0,5** | `MaterialArt.HullExposure`, `HullAmbientChroma`; `kRmHullExposure`, `kRmHullAmbientChroma` |
| Nubes | *quad* tangente al cielo; alfa lineal | *quad* en el plano de pantalla; alfa equivalente al del legacy | `Clouds.shader`, `RmDisplayAlpha` |

Lo demás de §17 (el arco, los discos y halos, el telón, las estrellas, las cordilleras, la orilla, el agua, los tramados, las líneas de tiempo de la cámara) **no cambió**: las tomas y los videos lo confirman. Las constantes de `WrapFade` tampoco: la vuelta de las nubes se ve como una nube que se disipa en el borde.

### 5. Los videos, cuadro por cuadro

Hojas de contacto de los once MP4 (cuadros a 30 fps, y cada cuadro en las ventanas de interés) y los valores por cuadro del *manifest*:

| Video | Lo que se vio |
|---|---|
| VID-01 | Negro hasta los 220; la escena aparece con la cámara alta y atrás y **baja como un dron** sin tirón; la cascada del HUD encima; a los 1 820 la pose del contrato sin salto |
| VID-02 | El fundido de 120 lineal con la cámara en su lugar; sin negro, sin cascada ni vuelo |
| VID-03 | Los pinos vienen y salen por los costados; el muelle pasa por la izquierda a los ≈ 39 s con sus pilotes; las montañas quietas de costado; las líneas del agua avanzan; ningún objeto aparece de golpe. El horizonte se aparta como mucho **2,93 dp** (W1280; tope 3) |
| VID-04 | El velo en 300 y la vuelta en 240; el bote se desliza **5,8 m** y queda quieto a los ≈ 10 s (velocidad 4 → 0); las nubes y el viento siguen |
| VID-05 y VID-06 | Cada cambio de hora en 1,5 s; el sol baja al collado y se esconde detrás de la cercana sin saltar; las estrellas con su fundido; las líneas se apagan con el sol y vuelven plateadas con la luna; la ventana se enciende y se apaga con su fundido; la bruma de la mañana |
| VID-07 | El HUD se va con la última foto en 0-200; la escena funde a negro en 200-400; Workout a los 400-640 |
| VID-08 | La toma aérea: la cámara sube, retrocede y cabecea en 6 s con el bote adentro de la zona, y se queda quieta |
| VID-09 | La escena se rehace en el cuadro del cambio, sin reinicio (la distancia sigue). **El HUD aparece entero en el primer cuadro de TH** (33 ms después del cambio), sin la entrada de 200 (D2-04) |
| VID-10 | La respiración: un vaivén lento; el horizonte se aparta como mucho **1,25 dp** (tope 1,26 en TV) |
| VID-11 | La opacidad de `scene-surface` sube de 0 a 1 en 0,6 s (el *manifest*), pero **en la imagen la escena aparece de golpe** al final del fundido: el fondo provisorio, opaco, queda **encima** de `scene-surface` mientras se ven los dos (D2-05) |

### 6. Presupuesto

- **El costo de los *shaders*** (Medio, D3D11, `SceneShaderTests`): `Horizon` 13 → **14** de 15 (la mezcla del borde encendido); `Sky` 40 de 41 y su bloque condicional 43 → **45** de 48 (el halo en gamma 2); `Clouds` 8 de 12; `Hull` 21 de 25 (la exposición y el croma van en el vértice). El resto, como en "Cambios por implementación de WP-D1" §1.
- **La tabla de §15.1 no cambia.** Lo medido por WP-B (B6a-04, B6a-20): 71 / 46 *draw calls* / SetPass en Medio (estimado ≈ 83 / ≈ 30), 91 / 61 en Alto y 58 / 33 en Bajo; en el build de Windows, ≤ 5,7 ms de GPU en Medio y ≤ 7,3 en Alto. **Confirmo el tope de SetPass de Alto en 80** (escritorio y tomas: el fondo tenue de estrellas y la capa lejana son solo de Alto); Medio y Bajo, 50. Los 60 fps en el teléfono siguen NO VERIFICABLE (It. 14).
- **0 B por cuadro**: `SceneBudgetTests` (PlayMode, con la actualización de los *Volume* de D2-01 adentro de lo medido) en verde.

### 7. Mixamo para la It. 7 (P-09): la lista definitiva

Se confirma §18 sin cambios y pasa a [ASSET_SLOTS §2](../../design/ASSET_SLOTS.md) como la definitiva (reemplaza a la "preliminar"). **Ninguna animación de Mixamo para remar** (la remada es IK procedural). Para que el usuario la descargue antes de la 7, desde [mixamo.com](https://www.mixamo.com) con una cuenta de Adobe gratis, a `Assets/ThirdParty/Mixamo/` (no se versiona):

| # | Archivo | Qué bajar | Opciones | ¿Obligatorio? |
|---|---|---|---|---|
| 1 | `YBot/YBot.fbx` | **Characters** → `Y Bot` | `FBX for Unity(.fbx)`, `T-pose` | **Sí** |
| 2 | `XBot/XBot.fbx` | **Characters** → `X Bot` | ídem | Recomendado |
| 3 | `RowerRealistic/RowerRealistic.fbx` | el primer personaje que pase la lista de control de ASSET_SLOTS §2.3 (primero: `Kaya`) | ídem | Opcional |
| 4 | `YBot/YBot@SittingIdle.fbx` | **Animations** → `Sitting Idle` con Y Bot elegido | `Without Skin`, 30 fps, `Keyframe Reduction` `none` | Opcional (solo referencia) |

Importación: ASSET_SLOTS §2.5 (**Humanoid**, *Create From This Model*, **Optimize Game Objects apagado**). En la 7 el slot `rower.character` acepta el 1 o el 3; si falta o no valida (Humanoid, dedos, alto de 1,55 a 2,05 m, ≤ 30 k triángulos), queda el maniquí procedural.

### 8. Desvíos y hallazgos (para el Architect, QA y el PO)

| ID | Dueño | Qué | Estado |
|---|---|---|---|
| **D2-01** | WP-B (bug de lógica) | `RenderPipeline.SubmitRenderRequest` **no actualiza los `Volume`**: en URP 17 `ProcessRenderRequests` va directo a `RenderSingleCameraInternal` y solo `RenderCameraStack` llama a `UpdateVolumeFramework`. El post leía la última pila que había armado otra cámara (en el arnés, la de un `Tick` sintético con el velo): la hora y el velo de pausa no llegaban a la imagen. `SceneRenderer` actualiza la pila principal con la máscara de la cámara de la escena antes de cada pedido (`ResetMainStack` + `Update`), como hace URP con cada cámara del juego | **Hecho** (lo hizo la primera corrida de WP-D2 y se conservó: sin él no hay tabla de la hora que ajustar). Las pruebas PlayMode, también las de 0 B por cuadro, en verde. Para que WP-B lo revise y sume una prueba propia |
| **D2-02** | WP-A (geometría de Core) | `HullMesh` (`Core/Scene/Geometry/PropMeshes.cs`): la cubierta con vértices propios en las bordas (blancos o del *cockpit*), así la línea de borda `#00B4D8` no tiñe la cubierta; `VertexCapacity` pasa a `Secciones × (Perfil + 4)`. Es un cambio de arte en un archivo de WP-A | **Hecho**, con `SceneGeometryTests.TheDeckIsWhiteAndTheStripeStaysOnTheSide` |
| **D2-03** | WP-B3 (arnés) | B6a-17 da como valores del *manifest* las sondas de aspecto (el matiz, la ventana, el velo, la luz de la cabaña) y la de contrato de la ventana, pero `SceneMarks.Contract` solo tiene las dos del cielo de CAP-02 y no hay ninguna de aspecto. WP-D2 las midió en los PNG (§3); la ventana `#FFE082` en la pasada de contrato y la cabaña ± 25 % quedan **sin medir** | Pendiente para WP-B3 / QA |
| **D2-04** | WP-B2 (integración) | VID-09: el HUD aparece entero en el primer cuadro de TH, sin la entrada de 200 de `HudReflowMotion` (DT6a-06). `HudLayoutTests` (PlayMode) sí ve el fundido: puede ser el cambio de tamaño del arnés (`UiTestHost.Resize`) o que la disposición nueva reemplaza las piezas después de aplicar la opacidad | Pendiente para WP-B2 / QA |
| **D2-05** | WP-B2 (integración) | VID-11: `scene-surface` es el primer hijo del HUD y el fondo provisorio queda **encima, opaco**, también durante el fundido de 0,6 s de la escena demorada (`SetSceneShown(true, keepBackdrop: true)`): el fundido no se ve y la escena aparece de golpe cuando el provisorio pasa a `display: none`. Para que se vea, `scene-surface` tiene que quedar encima del provisorio mientras se ven los dos (o el provisorio debajo) | Pendiente para WP-B2 (02-tech-design §3.3 dice "el primero") |
| **D2-06** | Proceso | La corrida cortada de WP-D2 dejó `ROWMATE_SIMULATOR` puesto en `ProjectSettings/ProjectSettings.asset`. La primera captura de esta corrida lo devolvió a como está en el último *commit* (vacío); el *script* salió con 11 porque comparó contra ese estado viejo. `git diff ProjectSettings/ProjectSettings.asset` vacío | Resuelto |
| **D2-07** | WP-A (prueba) | `SceneSessionTests.TheVeilGoesIntoTheSameVolume` leía el −0,5 como literal: ahora lee `HourLookTable.Default.VeilEv` (la regla de 02-tech-design §7.0) | Hecho |
| **D2-08** | WP-B (avisos) | La compilación de Unity deja tres avisos CS8625 (un `null` en una referencia no anulable) que ya estaban: `Scene/SceneLighting.cs` (190) y `Scene/SceneRenderer.cs` (78 y 90). El que sumaba D2-01 (`VolumeManager.Update(null, …)`) quedó como `null!` | Pendiente para WP-B |
| — | Designer | Dos decisiones de estas notas cambian con las tomas: sin tonemapping (R6a-04 pedía *Neutral*) y la ganancia de lo emisivo por debajo del piso de 1,25 de §7.3. El contrato no cambia (`#FFE082` y la paleta son "antes del ajuste de color de la hora") | Registrado acá y en ART_BIBLE §6.3 |

### 9. Verificación

1. **Legacy, solo lectura**: `lib/features/workout/scene/painters/sky_painter.dart` (las nubes y el halo, para la mezcla del alfa). `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó `lib/core/strava/strava_secrets.dart`.
2. **Pruebas primero** (TDD): `MaterialArtTests.TheHullDeckStaysWhiteBelowClippingAtNoon` y `TheLitWindowDoesNotClipItsHue`, y las constantes nuevas en `ShaderArt` (`TheShaderConstantsAreTheArtOnes`), se vieron fallar (3 rojas) antes de cambiar los valores y los *shaders*.
3. **dotnet** (`tools/dotnet/run-tests.ps1`): compilación con 0 avisos y 0 errores; simulador 192 / 192; Core 3045 / 3006 pasan / 0 fallan / 39 ignoradas.
4. **Unity 6000.3.27f1** (`tools/unity/run-unity-tests.ps1`, *batchmode*): compilación limpia; **EditMode** total 3130, pasan 3085, fallan 0, ignorados 45; **EditMode `-Simulator`** total 3322, pasan 3277, fallan 0, ignorados 45; **PlayMode** total 468, pasan 468; **PlayMode `-Simulator`** total 540, pasan 540. La guardia de `ProjectSettings`, sin cambios en cada una.
5. **Arnés** (`tools/unity/capture-snapshots.ps1`, `-Set it6a`): dos corridas completas, la A en `snapshots/` y la B en `<scratch>`, **iguales byte a byte** (34 PNG, 11 MP4 e `INDEX.md`; `manifest.json` igual salvo `durationMs`); 45 tomas, 0 con problemas, 0 sondas que fallan; `manifest.json` e `INDEX.md` sin rutas de la PC. `it4b` e `it5` no usan la escena real: no cambian. Las sondas de §3 y las hojas de los videos de §5 se hicieron con herramientas propias en `<scratch>` (Python con Pillow y PyAV, sin red).
6. **El repositorio**: sin *commits*; rutas relativas, el legacy por su URL o `../rowmate-legacy`, `<scratch>`. Ni las pruebas ni las capturas tocaron `prefs.json` ni las bases del usuario (`ROWMATE_DATA_DIR` temporal).
