# It. 4b — Deuda de diseño de la It. 4a y base de motion del HUD EXR

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-29 |
| Iteración | 4b · `04b-hud-exr` ([ROADMAP §It. 4b](../../ROADMAP.md)). Se escribió al empezar, en paralelo con la spec del PO y con las condiciones del cierre de la 4a, que está resolviendo el Architect (QA-V4a-03 a -06) |
| Entrada | [REPORT de la It. 4a](../04a-motor-hud-funcional/REPORT.md) (§5.1 y §7 a §9); [05-qa-verification de la 4a](../04a-motor-hud-funcional/05-qa-verification.md) (QA-V4a-03, QA-V4a-06 y O-01); [04a/00-deuda-diseno](../04a-motor-hud-funcional/00-deuda-diseno.md) (§1.6, §1.7 y §3); [04a/04-design-notes](../04a-motor-hud-funcional/04-design-notes.md) (§3, §5, §7, §14 y §17); [STATUS](../../STATUS.md); [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) (§0.1, §1, §2.4 a §2.11 y §7.1); [ART_BIBLE](../../design/ART_BIBLE.md) (§2.3, §3.2, §3.3, §3.5, §3.11, §5.2 y §10); los inventarios de F-WKT-04, -09, -13 a -16, -19 y -21, y de F-APP-15; las capturas de la 4a (CAP-19 y CAP-23) y las medidas que hizo QA sobre ellas<br>Código: `Core/Design/{Tokens,MotionSpecs,HudGeometry,Easing,CriticalSpring,DigitRoll,Contrast,Icons}.cs`, `Core/Workout/{StepProgress,ActiveClock}.cs`, `Simulator/Panel/{SimulatorPanelLayout,WrenchPlacement}.cs`, `SimulatorPresentation/SimulatorLayerView.cs`, `Presentation/Workout/{HudBand,ImmersiveWorkoutPage}.cs`, `Presentation/Motion/PageTransition.cs` y `Presentation/Styles/Base.uss`<br>El trabajo en curso del Architect sobre QA-V4a-03 a -06 se leyó con `git diff`, sin tocarlo |
| Entrega | Este documento, con los pedidos **DD4b-01** a **DD4b-18** (§5); [04a/00-deuda-diseno](../04a-motor-hud-funcional/00-deuda-diseno.md) §1.6 y §1.7, precisados; ART_BIBLE y ANIMATION_BIBLE alineadas (secciones "Cambios al iniciar la It. 4b"). **Ningún token cambia**: `Tokens.cs` sigue en 135 |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy):<br>- `lib/features/workout/step_pill.dart:1-105` y `test/step_pill_test.dart:1-54`;<br>- `lib/features/workout/immersive_workout_screen.dart:21-121` (el modo inmersivo, `:34` y `:39`), `:196-296` (el diálogo de completado), `:303-550` (la barra de pasos, la barra libre y el chip de fase) y `:893-1019` (los controles y el resumen);<br>- `lib/features/workout/series_panels.dart:57-111` (el reloj de pared), `lib/features/workout/workout_screen.dart:169-175` y `lib/core/dev/simulator_overlay.dart:25-100` (la llave y el alto máximo del panel).<br>No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar |
| Verificado | Programas propios, sin red, en `<scratch>/it4b/designer-debt/`:<br>- `rmcore.py`: réplica de `Easing.cs`, `CriticalSpring`, `DigitRoll` y `Contrast.cs`. Reproduce los valores de la 4a: 0,362 / 0,622 / 0,799 de la entrada, 0,066 / 0,248 / 0,500 del latido y 1,0167 / 1,0431 del resorte de 22;<br>- `motion4b.py`: las tablas de §4;<br>- `geo4b.py` y `panel4b.py`: la barra, las píldoras, el lugar de la cuenta grande, el divisor y el panel del simulador, sobre los rectángulos de 04a/04-design-notes §3.8 y el código del panel;<br>- `contrast4b.py`: los contrastes de las piezas nuevas;<br>- `kern_delta.py`, `kern_catalog.py`, `ellipsis.py` y `digits.py`: fontTools 4.68.0 (`uv run --offline`) sobre las fuentes, las listas K2 y los `.asset` del último commit (antes de la integración);<br>- `k2_verify.py`: los `.asset` que escribió después el *bootstrapper* del Architect, contra las listas K2.<br>**Unity no se abrió** y dotnet no se corrió, porque no cambió ningún token ni código |

Convenciones: las de la 4a. Las medidas van en dp. Perfiles: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720. `s` = `hudScale`: 1,054 en TV y TH, 0,923 en P360 y L640, 1,6 en W1280. "Base" = antes de multiplicar por `s`. Los rectángulos van en coordenadas de ventana, con y desde arriba y los márgenes seguros en 0.

- Las capturas de TV y TH son de 2,628 px por dp; las de L640 y W1280, de 1 px por dp.
- "Tick" es el segundo que procesa el motor (`ActiveClock`).
- Todo lo de §4 se anima por código, con el reloj inyectable (`MotionDriver`), salvo que se diga otra cosa.

---

## 0. Resumen

1. **QA-V4a-03, el divisor de la banda: visto bueno** (§1). El arreglo del Architect (`flex-shrink: 0`; la fracción de píxel pasa al relleno de la derecha) y su prueba (`BandDividerKeepsItsWidth`) hacen lo que pide AC-WKT-17-04. Qué tiene que dar en cada perfil:
   - un divisor de 1 × 32 base y un hueco de (2·g + 1)·s entre las columnas;
   - su centro con `00:42`;
   - `#263241` ± 2 en el medio del divisor en las tomas de TV y TH.
2. **QA-V4a-06, las cifras y el kerning: visto bueno** (§2) a la integración en curso, que coincide con DD4a-06, DD4a-07 y DN4a-22, con su lista de comprobaciones. Los `FontAsset` que ya escribió el *bootstrapper* tienen exactamente las tablas K2 (lo verifiqué con fontTools). Hay dos correcciones a lo que se esperaba:
   - **`Terminar entrenamiento` (CAP-26) no cambia**: Inter SemiBold ya tenía el par `Te` (−0,078 em). Del texto, casi nada se ve cambiar (el que más, el tipo de paso `Trabajo`: −0,79 dp a 13). Lo que sí cambia son las cifras, entre un 3,5 y un 4,6 % más angostas y con el `1` tabular.
   - **K2 no arregla el "…" en cian** de la pastilla recortada: a la flecha le faltan 1,98 dp para entrar. Pedido DD4b-03: el "…" siempre en blanco.
3. **04a/00-deuda-diseno §1.6 y §1.7, precisados** (§3):
   - fuera del entrenamiento, el alto máximo del panel es `alto − borde de abajo − (margen superior + 32)`: son los valores de AC-BLE-16-03;
   - con el panel abierto entre una pestaña y una pantalla apilada, **su borde de arriba no salta**, porque el alto máximo se calcula con el borde de abajo de cada cuadro (DD4b-04). Hoy, en L640, el panel sale 32 dp por arriba de la ventana al abrir el editor y baja 64 de golpe al cerrarlo.
4. **La regla de oro, aplicada** (§4.0): cada animación de la 4b dice de qué dato sale y por qué no adelanta ni frena el reloj ni las métricas.
5. **Entrada "sumergirse", sin escena** (§4.1 y §4.2):
   - el fondo funde en 240 ms, y el HUD baja a su lugar en cascada, de arriba hacia abajo: cada 40 ms desde los 120, cada pieza en 240 y subiendo 8 dp. A los 560 todo está quieto;
   - las barras del sistema se van en el mismo cuadro del toque, y **nada se mueve** cuando se van o vuelven: el HUD se arma con los márgenes del inmersivo y las pestañas con los de siempre (DD4b-07).
6. **Salida con la última foto** (O-01, §4.3): el HUD no aplica el estado de reposo, y sale (200) con lo último que mostró.
7. **Píldoras** (§4.4 a §4.7):
   - un solo elemento dibuja todas;
   - el relleno avanza continuo y nunca pasa del valor del tick siguiente (`PillProgress`);
   - en los últimos 5 s, un halo que late con el reloj común de la barra;
   - al cambiar de paso, la cumplida se "solidifica" con un destello (120-340 ms), y la no cumplida se vacía mientras su borde se dibuja alrededor (120-440).
8. **D-ANI-05** (§4.8 y §4.9):
   - **la cuenta grande** es un disco de vidrio de 64·s con la cifra de 52 y un aro del color del paso que se vacía con el tiempo real, centrado arriba de la zona del bote. Entra en los 5 perfiles con 8·s de aire;
   - **el sello** es una tercera fila de la barra de pasos, que crece de 52 a 74 base durante 2 s, con `✓ Objetivo cumplido` o `! No alcanzado` y los promedios contra el objetivo.
9. **El diálogo de completado, el velo de pausa, el chip de fase y el reloj de pared** (§4.10 a §4.12), con reducir movimiento (§4.13) y las constantes (§4.14).
10. **Presupuesto** (§4.15): con la cuenta grande y el sello a la vista, el HUD llega a 67 elementos, y el tope es 60. Propongo dónde juntar 7 sin cambiar lo que se ve (DD4b-16).

---

## 1. QA-V4a-03: el divisor de la banda (visto bueno por especificación)

### 1.1 Qué pasaba y qué hace el arreglo

- **La causa** (QA). `TabularNumber` redondea su caja hacia arriba a píxeles, y la banda mide lo que calcula `HudGeometry` con los anchos sin redondear. Cuando el contenido quedaba apenas más ancho que la banda, el divisor (1 dp, el único hijo con `flex-shrink` 1) cedía esa fracción y quedaba en 0 en TV, TH y L640.
- **El arreglo en curso** (leído con `git diff`, sin tocarlo).
  - `.rm-hud-band__divider { flex-shrink: 0; }` en `Base.uss` y `flexShrink = 0` en `HudBand`. La fracción de píxel cae en el relleno de la derecha (20 o 14 dp).
  - La prueba `HudLayoutTests.BandDividerKeepsItsWidth` mide el divisor (1 × 32 base), su `flex-shrink`, que se dibuje, el hueco entre las columnas y que quede centrado.
  - La corre en todos los perfiles con 5 casos: libre, libre con pulso, rutina con objetivos, los largos de CAP-16 y los extremos de FTMS.
- **Por qué está bien** (ART_BIBLE §3.2 y §3.6). En la banda, "el relleno, el divisor, los íconos y la unidad no se achican", y como en UI Toolkit `flex-shrink` vale 1 por defecto, todo adorno de tamaño fijo lleva `flex-shrink: 0`. Una fracción de píxel menos de relleno no se ve: es menos de 1 px de 20 dp.

### 1.2 Lo que tiene que dar

| Perfil | Divisor (dp) | Ancho en la captura | Hueco entre `#hud-time` y `#hud-distance` | Centro del divisor con `00:42` | Alto del divisor, desde el borde de arriba de la banda |
|---|---|---|---|---|---|
| TV | 1,054 × 33,72 | 2,77 px | (16 + 1 + 16)·s = 34,78 | x 139,4 dp (366 px) | de 21,08 a 54,80 |
| P360 | 0,923 × 29,54 | 2,77 px | 30,46 | — | de 18,46 a 48,00 |
| TH (compacta) | 1,054 × 33,72 | 2,77 px | (10 + 1 + 10)·s = 22,13 | x 126,8 dp (333 px) | de 21,08 a 54,80 |
| L640 (compacta) | 0,923 × 29,54 | 0,92 px | 19,38 | x 111,0 | de 18,46 a 48,00 |
| W1280 (compacta) | 1,6 × 51,2 | 1,6 px | 33,60 | x 192,4 | de 32,0 a 83,2 |

- **El color.** Blanco al 12 % sobre el vidrio y el agua da `#263241` (04a/04-design-notes §2.3).
  - En las tomas de TV y TH, el divisor mide 2,77 px: la columna del medio, a media altura de la banda, tiene que dar `#263241` ± 2.
  - En L640 y W1280 (1 px por dp) mide menos de 2 px y no se sondea por color: alcanza con el ancho del layout y una línea que se vea.
- **Con la derivada.** El reloj de la banda se angosta (`00:42` pasa de 84,9 a 81,7 base). En TV el divisor queda 3,4 dp a la izquierda de la columna que midió QA en CAP-12 (x 374-375 px).
- **Con las cifras achicadas.** En CAP-16, con la banda a 0,561, el divisor y su hueco no cambian: solo se achican las cifras.

### 1.3 Visto bueno

**Visto bueno del Designer** al arreglo y a la prueba. Para cerrar QA-V4a-03, además:

1. En la regeneración de §2 (una sola, con dos corridas seguidas), las tomas con la banda muestran el divisor en el lugar y con el color de §1.2, ± 1 px: CAP-12 a -14, -18 a -22, -26, -27a a -27c, -29 y -30 en TV, y CAP-15 y CAP-23 en TH.
2. Una sonda de aspecto en CAP-12 y en CAP-15 (`WithProbe(…, aspect: true)`) en ese punto, así una regresión hace fallar una toma (DD4b-01).

---

## 2. QA-V4a-06: RowMate Numeric SC y las tablas K2 (visto bueno por especificación)

### 2.1 El trabajo en curso

Leído con `git diff`, sin tocarlo:

- `FontAssetPaths` pasa el rol numérico a la derivada: la 600 y la 800 sin tabla, la 700 con la suya. `RetiredFontAssets` lista los tres de Barlow;
- `KerningList`, en Core: una sola lectura de las listas, para el *bootstrapper* y para la prueba;
- `FontAssetBuilder` y `Typography.uss` regenerado, con `.rm-type-num-*` en la derivada;
- `ItFourAFontTests` y `FontAndIconAssetsTests`;
- `ScalesOfTheDesignNotes`, sin el `[Ignore]`;
- los `FontAsset` que ya escribió el *bootstrapper*: los de Inter con las tablas nuevas, los tres de la derivada, y los de Barlow borrados.

**Coincide con DD4a-06, DD4a-07 y DN4a-22.** Lo verifiqué sobre los archivos, con fontTools (`k2_verify.py`):

- la tabla de cada cara es exactamente su lista K2: 1 445, 1 695, 1 695, 1 695 y 1 272 pares, sin pares de menos, de más ni con otro valor;
- `m_GetFontFeatures: 0` en todas, con el muestreo de 48 (Inter) y 90 (la derivada); la derivada 600 y la 800 van sin tabla (K4);
- en cada derivada, las diez cifras son las `*.tf`, con un solo avance: 507, 521 y 535;
- ningún `.uss`, `.uxml` ni `.asset` nombra los `FontAsset` de Barlow.

### 2.2 Lo que tiene que dar

| Dónde | Comprobación |
|---|---|
| dotnet (`ItFourAFontTests`) | La tabla de cada `.asset` es exactamente su lista K2: 1 445 pares en Inter 400; 1 695 en Inter 500, 600 y 700; 1 272 en la derivada 700. `m_GetFontFeatures: 0` en las caras de texto. Ningún `.uss` ni `.asset` nombra los `FontAsset` de Barlow |
| Unity EditMode (`FontAndIconAssetsTests`) | En cada derivada, las diez cifras tienen un mismo avance: 507, 521 y 535 unidades |
| PlayMode (`ScalesOfTheDesignNotes`) | Las escalas de 04a/04-design-notes §3.7, ± 0,005. Por ejemplo: en L640, la banda a 0,776 con valores normales y SPM a 0,962 con objetivo; en CAP-16, la banda a 0,561 |
| PlayMode (ancho de `TabularNumber` a tamaño base, ± 0,5 por el redondeo a píxeles) | `24.0` en 64/800: **123,2** (hoy, 129,2) · `02:13` en 44/700: **105,8** (109,8) · `150`: **68,8** (71,8) · `00:42` en 34: **81,7** (84,9) · `1048`: **70,9** (74,0) · `11` en 32: **33,3** (34,8) · `0:40` en 20: **37,7** (39,0) |
| Capturas, geometría | CAP-15: los rectángulos de la banda y de Split de 04a/04-design-notes §3.8, ± 0,1, sin las comprobaciones flojas que dejó la 4a. CAP-16: la banda a 0,561 ± 0,005. Si la pastilla se corrió, se vuelve a ubicar el punto de la sonda de la tendencia (CAP-19, -23 y -25) |
| Capturas, reproducibles | Dos corridas seguidas de `it2`, `it3` e `it4a` dan los mismos bytes en las fotos y en los videos (QA-V3-06) |

### 2.3 Qué se ve cambiar, y qué no

| Qué | Cambio | Dónde |
|---|---|---|
| Toda cifra de `TabularNumber` (el HUD, el tablero de Dispositivo y la cápsula `×N`) | Entre un 3,5 y un 4,6 % más angosta; el `1` con su pie y sin aire de más (DD4a-07) | Capturas de las It. 2, 3 y 4a |
| `num.stat` del panel del simulador | Cifras tabulares de la derivada 700: `150 W` → `160 W` ya no corre las letras. El `1` es más ancho que el proporcional de Barlow (0,521 contra 0,319 em) | Las tomas con el panel abierto: DEMO-03 de la 4a y las de la It. 2 |
| Tipo de paso, en Inter 700 a 13 | `Trabajo` −0,79 dp; `Work` −0,44; `Warmup` −0,65; `Calentamiento` −0,06; `Descanso` (a 10, en `Próximo`) +0,10 | CAP-19 a -25 y CAP-27c |
| `Reanudar` (16/700) y `Serie` (13/700) | +0,16 y +0,07 dp | CAP-14 y el editor de la It. 3 |
| Inter 600 (títulos, botones y rótulos) | Ya tenía casi todos los pares: el cambio más grande en las tomas es ` SPM +2/rep` (−0,28 dp). **`Terminar entrenamiento` no cambia**: el par `Te` ya estaba | — |
| Inter 500 (`caption`) | Las unidades del HUD tienen una letra (`W`, `m`): no cambian | — |

**Corrección.** El REPORT de la 4a (§7.1) y 04a/04-design-notes §17.6 daban el `Te` de `Terminar entrenamiento` (CAP-26) como un par que faltaba. No faltaba: el título del diálogo es `title` 20/600, y la tabla de Inter SemiBold lo tiene. Los pares que faltaban son de **Inter 700** (`display`, `headline` y el texto en 700, como dice 04a/00-deuda-diseno §0), que en las tomas de hoy casi no aparece. El texto nuevo de la 4b en 700 (el título del sello, en `headerS`) ya sale con la tabla completa.

### 2.4 El "…" de la pastilla recortada (DD4b-03)

04a/04-design-notes §17.6 esperaba que con K2 la flecha `trending_up` entrara en la pastilla recortada de CAP-21 y CAP-27c, y que el "…" saliera en blanco. **No entra**:

- la pastilla mide 128,4 con K2 y la derivada, así que quedan 116,4 dp para el texto;
- `>160W · 22 spm · ` mide 94,96 (con K2 y sin K2), la flecha 12,9 y el "…" de Inter SemiBold a 11, 10,52 (0,956 em): suman 118,38;
- faltan **1,98 dp**, y el "…" sigue saliendo del color del primer carácter que se saca, que es la flecha (cian).

**Pedido:** el "…" de la pastilla va siempre en el color de su texto (`text.primary`), venga de donde venga el recorte. El cómo lo elige el Architect: cortar en código antes del glifo, o evitar que el estilo del glifo pase al "…". Se verifica en CAP-21 y CAP-27c: el "…" en blanco, ± 3 en su trazo.

### 2.5 Visto bueno

**Visto bueno del Designer** a la integración. La parte de los archivos (K3, K4 y las cifras) ya da (§2.1); faltan las pruebas y la regeneración única de `it2`, `it3` e `it4a`, que tienen que dar lo de §2.2 y lo de §1.2. QA lo verifica con estas listas. Si alguna comprobación no da, el visto bueno se reabre y vuelve al Designer antes de cerrar las condiciones.

---

## 3. 04a/00-deuda-diseno §1.6 y §1.7, precisados

Los dos puntos quedaron escritos en su lugar ([04a/00-deuda-diseno](../04a-motor-hud-funcional/00-deuda-diseno.md) §1.6 y §1.7, con la sección "Precisiones al empezar la It. 4b"). Este es el porqué.

### 3.1 §1.7: el alto máximo del panel fuera del entrenamiento

- **Lo que decía.** "Alto de la ventana − borde de abajo − margen seguro superior − 16". Con la llave a 80 dp del borde de abajo (barra de 64 + 16, DV-07), esa fórmula da 264 y 818, no los 248 y 802 de la It. 2 que fija AC-BLE-16-03 (spec de la 4a §15).
- **La regla, precisada.** Fuera del entrenamiento: `alto − borde de abajo del panel − (margen seguro superior + 32)`, con mínimo 120.
  - En una pestaña da 248, 802 y 120 (ventanas de 360, 914 y 200).
  - En una pantalla apilada, con la llave a 16, da 64 más: 312, 866 y 152.
  - En el entrenamiento la reserva es 16, con el techo de Watts en vertical (04a/04-design-notes §3.9).
  - Es lo que ya hace `SimulatorPanelLayout.AppTopReserve`.
- **Por qué 32.** Son los 16 de aire del legacy más los 16 que la llave bajó con la barra de 64. El legacy tenía la llave a 96 (`lib/core/dev/simulator_overlay.dart:42`) y la fórmula `alto − 96 − margen − 16` (`:83-86`).
  - Así, abierto al máximo, el panel tiene el borde de arriba en `margen superior + 32` en toda la app, igual que en la It. 2.
  - Y la cinta `SIMULADOR` no llega a su parte visible. La franja de la cinta termina en x + y = 56,5. La esquina del panel está en (16; 32), y su arco de radio 16, en el punto más cercano (a 45°), está en x + y = 57,37: queda a 0,87 dp.

### 3.2 §1.6: el panel abierto entre una pestaña y una pantalla apilada

- **Lo que pasa hoy** (`SimulatorLayerView`, leído sin tocarlo). Mientras la llave se mueve (ω 22), el borde de abajo del panel la sigue, pero el alto máximo es **el del destino**.
  - Si el contenido no entra (en L640 el panel mide unos 409 dp), al abrir el editor el borde de arriba salta a −32: sale 32 dp por arriba de la ventana, con el botón de cerrar, y vuelve a 32 en ≈ 330 ms.
  - Al cerrar el editor, salta de 32 a 96 y sube otra vez a 32.
  - AC-BLE-16-02 pide que el panel "pase al lugar nuevo sin saltos". Ninguna toma ni prueba abre el editor con el panel abierto, por eso no apareció antes.
- **La regla, precisada.** El panel va con la llave, anclado a su borde de abajo, con el mismo resorte. Su alto máximo se calcula **en cada cuadro**, con el borde de abajo de ese cuadro (§3.1).
  - Si el contenido no entra, el borde de arriba queda quieto en `margen superior + 32`, y el panel se estira o se encoge.
  - Si entra, se traslada entero.
  - Conserva su desplazamiento interno, acotado si el panel se achica.
  - Nunca sale de la ventana ni salta.

| t (ms) | Llave (desde abajo) | L640 hoy: borde de arriba | L640 precisado: alto · borde de arriba | TV precisado (contenido de 409): borde de arriba |
|---|---|---|---|---|
| 0 | 80,00 | −32,0 | 248,0 · 32 | 425,0 |
| 33,3 | 69,30 | −21,3 | 258,7 · 32 | 435,7 |
| 66,7 | 52,40 | −4,4 | 275,6 · 32 | 452,6 |
| 100 | 38,69 | 9,3 | 289,3 · 32 | 466,3 |
| 200 | 20,24 | 27,8 | 307,8 · 32 | 484,8 |
| 333,3 | 16,35 | 31,7 | 311,7 · 32 | 488,7 |
| 500 | 16,01 | 32,0 | 312,0 · 32 | 489,0 |

Con reducir movimiento, la llave y el panel pasan al lugar nuevo en el acto. Entre pestañas no se mueve nada. Al entrar o salir del entrenamiento, el panel cambia de carril con la llave (04a/04-design-notes §5.9). En la 4b eso pasa al terminar la cascada de §4.1, no a los 240.

---

## 4. Base de motion de la 4b

### 4.0 La regla de oro, aplicada (ANIMATION_BIBLE §0.1)

"La animación nunca adelanta, demora ni frena el reloj ni las métricas": el estado cambia en el instante del dato y se anima solo cómo se ve. Así se cumple en cada pieza de la 4b:

| Animación | De qué dato sale | Por qué no adelanta ni frena |
|---|---|---|
| Entrada en cascada (§4.1) | La sesión, que corre desde el toque (AC-WKT-13-01) | Solo cambian la opacidad y la posición de piezas que ya muestran el valor del momento. Los controles responden desde el cuadro 0, como en la 4a |
| Barras del sistema (§4.2) | — | No tocan el motor, y no mueven el layout |
| Salida con la última foto (§4.3) | El último estado antes del reposo (`Finished`, con el reloj ya quieto) | Muestra los valores finales. Pasar a reposo es interno |
| Relleno de la píldora (§4.5) | El tick del motor y el tiempo activo desde ese tick, con el reloj del motor | Nunca pasa del valor del tick siguiente. En pausa queda donde está y, dentro de un paso, nunca vuelve atrás |
| Halo de los últimos 5 s (§4.6) | El "por terminar" del motor (restante de 1 a 5) | Empieza y termina en el tick que lo cambia |
| Cambio de paso (§4.7) | El paso nuevo, que ya está en el estado | Todo empieza en el cuadro del cambio, después del dato |
| Cuenta grande (§4.8) | El restante del motor: 3, 2, 1 | Aparece en el tick del 3 y se va en el del cambio. El aro usa el mismo relleno que la píldora, así que nunca se vacía antes del tick |
| Sello (§4.9) | El resultado que calcula el motor al cerrar el paso (F-WKT-09) | Entra a los 120 ms del cambio, con el resultado ya calculado. Dura 2 s del reloj de la interfaz, aunque se pause |
| Diálogo de completado (§4.10) | La fase `Finished` | Entra a los 300 ms, con la sesión ya terminada y el reloj quieto. Los totales cuentan hasta el valor exacto: son los únicos números que cuentan (ANIMATION_BIBLE §2.5) |
| Velo de pausa (§4.11) | La fase | Oscurece solo el fondo |
| Reloj de pared (§4.12) | La hora del sistema, por el reloj inyectable | El texto nuevo está desde el cuadro del cambio de minuto |
| Volver del segundo plano o despertar el equipo (P-17) | El estado del presente (`Instant`) | Todo en el acto: ni rodillos, ni destellos, ni cuenta, ni sello de lo que pasó mientras tanto (04a/04-design-notes §5.10) |

### 4.1 Entrada "sumergirse" (sin escena)

La 4a abría el inmersivo con un fundido de 240 (`ease.enter`). La 4b le suma lo que la It. 6a va a sincronizar con el vuelo de la cámara: el HUD que baja a su lugar en cascada mientras las barras del sistema se van (§4.2).

| t (ms) | Qué pasa |
|---|---|
| 0 | El toque en `Entrenamiento libre` o en `Iniciar`. El motor ya corre (AC-WKT-13-01). La página entra con opacidad 0. Debajo, Workout muestra "en curso" sin su barra superior (04a/04-design-notes §4.1.5). Las barras del sistema se ocultan. La llave sale (opacidad 1 → 0 en 90) |
| 0-240 | La página, con su fondo, funde de 0 a 1 (`ease.enter`), como en la 4a |
| 120-560 | Las piezas del HUD entran en cascada, de arriba hacia abajo y cada 40 ms: la barra superior a los 120, SPM a los 160 (en la It. 5, junto con los paneles de series), Split y Watts a los 200, la banda a los 240, kcal y BPM a los 280 y los controles a los 320. Cada pieza, en 240 (`dur.base`, `ease.enter`), funde de 0 a 1 y sube de +8 a 0 (`shift.s`) |
| 560 | Todo quieto. Terminó la transición: las pestañas pasan a `display: none` (lo que hacía la 4a a los 240) y la llave entra en su lugar del HUD (opacidad 0 → 1 y escala 0,85 → 1 en 160). Si el panel estaba abierto, se abre en el lugar nuevo |

Opacidad que se ve (la de la pieza por la de la página) y subida, a 30 cuadros por segundo:

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 | 333 | 367 | 400 | 433 | 467 | 500 | 533 | 567 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Fondo (la página) | 0 | 0,362 | 0,622 | 0,799 | 0,910 | 0,970 | 0,995 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Barra superior | 0 | 0 | 0 | 0 | 0,144 | 0,463 | 0,698 | 0,850 | 0,939 | 0,983 | 0,998 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| SPM | 0 | 0 | 0 | 0 | 0 | 0,080 | 0,420 | 0,663 | 0,826 | 0,925 | 0,977 | 0,997 | 1 | 1 | 1 | 1 | 1 | 1 |
| Split y Watts | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,362 | 0,622 | 0,799 | 0,910 | 0,970 | 0,995 | 1 | 1 | 1 | 1 | 1 |
| Banda | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,299 | 0,577 | 0,769 | 0,892 | 0,961 | 0,992 | 1 | 1 | 1 | 1 |
| kcal y BPM | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,231 | 0,529 | 0,737 | 0,872 | 0,951 | 0,988 | 0,999 | 1 | 1 |
| Controles | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,158 | 0,478 | 0,702 | 0,850 | 0,939 | 0,983 | 0,998 | 1 |
| Subida de la barra (dp base) | +8 | +8 | +8 | +8 | +6,73 | +4,18 | +2,39 | +1,20 | +0,49 | +0,13 | +0,01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Subida de los controles | +8 | +8 | +8 | +8 | +8 | +8 | +8 | +8 | +8 | +8 | +6,73 | +4,18 | +2,39 | +1,20 | +0,49 | +0,13 | +0,01 | 0 |

- **El orden** es el de ANIMATION_BIBLE §2.11 y lee la pantalla como la lee el remero: primero dónde está (la barra), después el ritmo (SPM) y al final los controles. La subida va por `translate`, nunca por layout.
- **Por qué empieza a los 120.** El fondo ya está a 0,875. En Android, las barras del sistema ya se fueron casi del todo: con la animación controlada de §4.2, queda un 13 %. La barra superior no aparece debajo de la barra de estado.
- **Los controles** responden desde el cuadro 0, como en la 4a (la sesión está viva), aunque empiezan a verse a los 320. Es lo mismo que pasaba con el fundido de la 4a: la animación no demora nada.
- **Desde el diálogo de iniciar.** El diálogo sale (150) mientras entra el fondo.
- **Interrupciones.**
  - Si la sesión pasa a reposo antes de los 560 (una base que falla en el acto, o un `Terminar` muy rápido), la cascada se detiene donde está y la salida (§4.3) arranca desde la opacidad que tiene la página. Las piezas que no habían empezado no se ven.
  - Si se rota la ventana o cambia su tamaño, el HUD se reacomoda en el acto (04a, AC-WKT-17-07) y la cascada sigue con sus tiempos.
- **Reducir movimiento.** Todo junto, sin cascada ni subida: la página funde lineal de 0 a 1 en 120 (`ReducedImmersiveFadeMs`, como en la 4a). La llave entra a los 120, solo con opacidad.
- **En la 6a** el vuelo de cámara de 1,6 s reemplaza al fundido del fondo, y la cascada conserva el orden y los verbos con sus propios tiempos (ANIMATION_BIBLE §2.11). Por eso los tiempos de la cascada son constantes (§4.14) y no números escritos en el código.

### 4.2 Barras del sistema (F-APP-15)

- **Cuándo.** Se ocultan en el cuadro del toque (el del *push* del inmersivo) y vuelven en el cuadro 0 de la salida (§4.3). El modo es "inmersivo persistente": si el usuario las revela deslizando desde el borde, vuelven a ocultarse solas (paridad, `immersive_workout_screen.dart:34`, `:39`).
- **Cómo se van.** Si la plataforma deja controlar su animación (Android 11 o más nuevo, con `WindowInsetsController.controlWindowInsetsAnimation`), se van en 240 (`ease.enter`, la curva del fondo) y vuelven en 200 (`ease.exit`, la de la salida). Si no, con la animación del sistema, que dura parecido. Sin teléfono, esto queda **NO VERIFICABLE**.
- **Nada se mueve.** Hay dos juegos de márgenes, y ninguno cambia cuando las barras se ocultan, vuelven o aparecen un momento por el deslizamiento:
  - **los de la app**: con las barras, aunque no se vean (barras y recorte de la pantalla). Los usan las pestañas, las barras superiores y Workout debajo del inmersivo;
  - **los del inmersivo**: solo el recorte de la pantalla (*display cutout*). El HUD los usa desde el cuadro 0 del *push*.

  Si el HUD usara los márgenes de las barras, la barra superior saltaría 24 a 48 dp hacia arriba en el medio de la cascada, cuando la barra de estado termina de irse. Y si las pestañas usaran los del inmersivo, al salir Workout bajaría de golpe detrás de la página que funde. Las barras que aparecen un momento quedan encima del HUD, que no se mueve.
- **Windows, macOS y el Editor** no tienen barras: no hacen nada. El escritorio no pasa a pantalla completa (F-APP-15, casos borde).
- **iOS** (It. 10b): `prefersStatusBarHidden` y `prefersHomeIndicatorAutoHidden`, con el fundido del sistema en los mismos 240 y 200.
- **Los íconos de la barra de estado** quedan claros, como hoy, porque al salir vuelven sobre Workout.

### 4.3 Salida con la última foto (O-01)

**La regla.** Desde el primer estado de reposo, **el HUD no aplica datos**.

- Los valores, los colores, la fila del paso (tipo, pastilla, cuenta y `Próximo`), las píldoras y el sello quedan como en el último estado anterior al reposo, que es `Finished`, con el reloj ya quieto: la última foto.
- Los controles siguen al 0,38 desde `Finished`.
- Lo finito que estaba corriendo termina con su reloj: el rodillo (180), los colores (150), el aro.
- Lo que se repite (el latido, el halo, el punto "en vivo") sigue hasta que la página sale del árbol. Ahí se detiene, como corrigió QA-V4a-02.
- Desde `Finished` no empieza nada nuevo: la cuenta grande sale (200) y no entra ningún sello.

Hoy, a los 120 ms de la salida, la banda ya dice `00:00` (O-01, foto 09a de QA). Con la regla, en todos los cuadros de la salida la banda muestra el reloj y la distancia finales.

| t (ms) | Qué pasa |
|---|---|
| 0 | La página empieza a fundir. Debajo vuelven las pestañas y la barra de Workout, ya en reposo. El aviso del fin, si hay uno, entra sobre la barra (220). Las barras del sistema vuelven. La llave sale (90) |
| 0-200 | La página funde de 1 a 0 (`ease.exit`): 0,995 / 0,961 / 0,872 / 0,702 / 0,421 / 0 a los 33 / 67 / 100 / 133 / 167 / 200, igual que en la 4a |
| 200 | La página sale del árbol. La llave entra en el carril izquierdo |

- **Con el diálogo de completado.** `Finalizar` hace salir el diálogo (150, escala 0,98, con su velo) y la página (200) en el mismo cuadro: el motor pasa a reposo cuando el diálogo se cierra (DT4a-15). Nunca queda un diálogo sobre Workout en reposo. El aviso del fin no aparece, porque el diálogo ya lo dijo (P-02 de la 4a). Si el guardado falló, entra `No se pudo guardar la sesión` (DV-29).
- **Sin transición.** Si al volver del segundo plano la sesión ya está en reposo, el inmersivo sale sin fundido, como en la 4a.
- **Reducir movimiento.** La página funde lineal en 120, con la misma última foto.
- **En la 6a** la cámara sube y se aleja (ANIMATION_BIBLE §2.11).

### 4.4 Píldoras: estados y geometría mínima

Las medidas son las del legacy [CONTRATO] (`step_pill.dart`; `immersive_workout_screen.dart:358-377`):

- **La fila.** Relleno de 10 / 8 / 10 / 4 (izquierda, arriba, derecha y abajo), píldoras de 8 de alto, radio 4 (`r.xs`) y 3 de separación. Todas del mismo ancho (Q-59).
- **La barra.** Con la fila de píldoras arriba, mide 1 + 8 + 8 + 4 + 22 + 8 + 1 = **52 base**. La fila de información de la 4a queda igual, ahora sin su relleno de arriba (el legacy usa `fromLTRB(12, 0, 12, 8)`). La barra libre sigue en 40. Las tarjetas están a `90·s` del borde de arriba en los dos casos, así que no se mueven.

| Perfil | Ancho útil de la fila (base) | Con 6 pasos | Con 22 pasos | Separación 3 (píldoras de 6 base o más) | Separación 1 (de 2 base o más) |
|---|---|---|---|---|---|
| TV · P360 | 344,0 | 54,8 (57,8 / 50,6 dp) | 12,8 | hasta 38 pasos | hasta 115 |
| TH | 821,3 | 134,4 | 34,5 | hasta 91 | hasta 274 |
| L640 | 647,3 | 105,4 | 26,6 | hasta 72 | hasta 216 |
| W1280 | 754,0 | 123,2 | 31,4 | hasta 84 | hasta 251 |

**Con muchos pasos** la fila nunca se recorta ni se desplaza (paridad):

- primero la separación baja de 3 a 1;
- con más pasos todavía (en TV, más de 115: una serie ×58 de dos pasos, que permite Q-92), la fila pasa a ser una tira continua: cada paso es un tramo de `ancho útil / n`, sin separación, y solo la tira tiene los extremos redondeados;
- en la tira, la píldora en curso se dibuja con al menos 2 base, centrada en su tramo y por encima de las vecinas, para que se vea dónde va. Es el único caso en que una píldora no mide lo mismo que las demás (Q-59), y el legacy tampoco lo dibuja bien: la fila desborda. Lo decide el PO (§6).

**Estados** (contrato, `step_pill.dart:30-55`). El color es el base del tipo de paso, porque las píldoras son gráficos (ART_BIBLE §2.3):

| Estado | Cómo se ve |
|---|---|
| Pendiente | Color al 18 % |
| En curso | Pista al 30 % y relleno al 100 % proporcional al avance (§4.5). [EXR] Un brillo de 1 px `#FFFFFF @25 %` en el borde de arriba del relleno (ART_BIBLE §3.3). En un paso por distancia, solo la pista: el avance vale 0 (Q-51) |
| Hecha, cumplida o sin objetivo | Color al 85 % |
| Hecha, no cumplida | Interior al 18 % y borde de 1,5 al 85 % |
| El último paso | Queda "en curso" (Q-52) |

- **Un solo elemento.** `generateVisualContent` dibuja todas las píldoras, el relleno, el brillo, el destello, el trazo y el halo (con el `GlowTexture` de la 4a en 9 partes). Todo va recortado a la fila: el halo nunca pasa debajo del texto de la fila de información (ART_BIBLE §2.3: ningún texto del HUD se apoya sobre un *glow*).
- **Contraste** (un gráfico pide 3:1).
  - Sobre el vidrio del fondo provisorio (`#08162A`), las hechas dan 3,3:1 o más en los 5 tipos.
  - Contra el peor vidrio, con una nube blanca detrás (desde la 6a), la hecha de enfriamiento da 2,850 y la de otro tipo 2,629. La de trabajo, 3,038. La en curso al 100 % da 3,086 o más en los cinco.
  - El estado también se codifica por la forma (tenue, llenándose, sólida o hueca; ART_BIBLE §10.3).
  - **Riesgo para la 6a**: si con la escena se ven nubes detrás de la barra, la variante `.hud` de esos dos tipos da 3,759 y 3,745. La 4b no lo cambia.
- **La cinta `SIMULADOR`** tapa el extremo izquierdo de la primera píldora (en TV, en x + y = 42), como tapa la esquina de los títulos en las otras pantallas. El tipo de paso baja y sale de la cinta (04a/04-design-notes §3.3).

### 4.5 El relleno continuo (`PillProgress`)

ANIMATION_BIBLE §2.6 pide un relleno que avance continuo entre los ticks, sin adelantarse al reloj. Precisado con el reloj de la 4a (`ActiveClock`, Q-58: pausar descarta la fracción del segundo y reanudar abre un tramo nuevo):

- `objetivo = (transcurrido + clamp(ahora − tick; 0; 1 s)) / D`, donde:
  - `transcurrido` son los segundos enteros que contó el motor en el paso;
  - `tick` es el instante del último segundo procesado, o el comienzo del tramo después de reanudar;
  - `D` es la duración del paso.
- `relleno = max(relleno anterior; min(objetivo; (transcurrido + 1) / D))`:
  - **nunca pasa del valor del tick siguiente**;
  - **dentro de un paso nunca vuelve atrás**.
- En pausa queda donde estaba. Puede estar hasta 1 s adelante del segundo contado, pero nunca del siguiente. Al reanudar espera a que el motor lo alcance, y así devuelve la fracción que Q-58 descarta.
- En el primer estado después del segundo plano o del sueño (`Instant`), y en el cambio de paso, el relleno pasa a `transcurrido / D` en el acto.
- Con `D ≤ 0` y en los pasos por distancia, no hay relleno.
- Es puro: va en Core (`PillProgress`, ANIMATION_BIBLE §7.1) con pruebas de dotnet (DD4b-09).

Un ejemplo con D = 30 s: ticks a los 1,000, 2,000 y 3,040 s (este último tarde); pausa a los 3,600, reanuda a los 5,000 y el tick siguiente llega a los 6,000.

| t (s) | 0,5 | 1,0 | 1,5 | 3,0 | 3,04 | 3,3 | 3,6 (pausa) | 4,5 | 5,3 | 5,6 | 5,9 | 6,0 | 7,0 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tick | 0 | 1 | 1 | 2 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 4 | 5 |
| Relleno (s de 30) | 0,500 | 1,000 | 1,500 | 3,000 (tope) | 3,000 | 3,260 | 3,555 | 3,555 | 3,555 | 3,600 | 3,900 | 4,000 | 5,000 |

- **Cuánto se mueve.** En un paso de 60 s y una píldora de 57,8 dp (TV, 6 pasos), el relleno avanza 0,96 dp por segundo, unos 2,5 px por segundo en un teléfono.
- **Cuándo se repinta.** Basta con repintar cuando el borde del relleno avanza un cuarto de píxel, o mientras corre una animación de la fila. Sin reservar memoria por cuadro (`HudBudgetTests`).

### 4.6 El halo de los últimos 5 s

- **Qué.** Mientras el paso está "por terminar" (restante de 1 a 5 s; solo en pasos por tiempo, F-WKT-04), la píldora en curso lleva un halo de su color.
  - Intensidad: `a × (0,30 + 0,40 × p)`, que va de 0,3 a 0,7 como el legacy.
  - Forma: desenfoque 8, expansión 2 y radio 5 (`step_pill.dart:82-101`), con el `GlowTexture` en 9 partes.
- **`p`** es el reloj común de la barra (04a/04-design-notes §5.4): el mismo que mueve `Próximo` y la cuenta, así laten juntos.
- **`a`** es una rampa de 0 a 1 en 150 (`ease.move`) al empezar, y de vuelta a 0 en 150 en el cambio de paso o al terminar. Con el reloj común a mitad de ciclo, el halo no salta.
- **En pausa** sigue latiendo, por paridad (F-WKT-14, punto 9).
- **La píldora** no cambia de tamaño.
- **Reducir movimiento.** Realce fijo en 0,70, que entra y sale con la rampa (es un fundido).

### 4.7 El cambio de paso en la barra

`t = 0` en el segundo en que el motor cambia de paso. La fila de información hace lo de la 4a: el tipo y la pastilla en 0-120 y 120-260, la cuenta y `Próximo` (04a/04-design-notes §5.5). Las píldoras:

| t (ms) | 0 | 33 | 67 | 100 | 120 | 133 | 167 | 200 | 233 | 267 | 300 | 340 | 367 | 400 | 440 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Relleno que faltaba de la que termina (0-120, `ease.move`) | 0 | 0,092 | 0,652 | 0,973 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Cumplida: destello blanco, 0,40 × (1 − `ease.enter`) en 120-340 | 0 | 0 | 0 | 0 | 0,400 | 0,331 | 0,196 | 0,104 | 0,047 | 0,015 | 0,003 | 0 | 0 | 0 | 0 |
| Cumplida: opacidad de 1 a 0,85 (120-340, `ease.move`) | 1 | 1 | 1 | 1 | 1 | 1 | 0,993 | 0,971 | 0,919 | 0,872 | 0,855 | 0,850 | 0,850 | 0,850 | 0,850 |
| No cumplida: interior de 1 a 0,18 (120-440, `ease.move`) | 1 | 1 | 1 | 1 | 1 | 0,999 | 0,983 | 0,942 | 0,855 | 0,685 | 0,453 | 0,283 | 0,227 | 0,192 | 0,180 |
| No cumplida: el trazo del borde (120-440, `ease.move`) | 0 | 0 | 0 | 0 | 0 | 0,001 | 0,020 | 0,071 | 0,176 | 0,385 | 0,668 | 0,875 | 0,943 | 0,986 | 1 |
| La nueva: pista de 0,18 a 0,30 (0-150, `ease.move`) | 0,180 | 0,186 | 0,222 | 0,282 | 0,295 | 0,299 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 |
| Halo de la que termina: rampa `a` (0-150) | 1 | 0,947 | 0,650 | 0,149 | 0,042 | 0,011 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

- **"Solidificar"**: el paso cumplido o sin objetivo se ve lleno y, a los 120, un destello lo asienta en su 85 %. Es el "sólido = cumplido" del legacy, con un tiempo que se lee.
- **El trazo** del borde de la no cumplida recorre el perímetro (2·ancho + 9,1 base: 118,7 en una píldora de 54,8) en sentido horario, desde el centro de su extremo izquierdo, mientras el interior se vacía. Se lee "lo intentó y no llegó".
- **La nueva** se enciende de pendiente a pista. Su relleno arranca de 0 con el primer segundo. No hay marcador que viaje: el legacy sacó el marco de la píldora en curso (`3ae0f3a`), y basta con la pista y el relleno.
- **El último paso** completa su relleno (0-120) y queda "en curso", sin solidificar (Q-52).
- **Un paso cortado con `Terminar`** no tiene resultado: queda en curso, con su relleno, en la última foto.
- **Dos cambios seguidos** (pasos de 1 s): lo que corre salta a su final y empieza lo nuevo.
- **Reducir movimiento.** Sin trazo: el borde de la no cumplida funde de 0 a 1 en 160. El destello, el interior y las opacidades quedan igual, porque son fundidos.

### 4.8 La cuenta grande de los últimos 3 s (D-ANI-05)

**Cuándo.** En los pasos por tiempo, con restante 3, 2 y 1: desde el tick que lo deja en 3, o desde que empieza un paso más corto. No aparece en pasos por distancia, en libre ni después de `Finished`.

**Dónde.** En un lugar fijo por ventana, "centro alto", que calcula `HudGeometry`:

- centrado en x en la ventana;
- en y, centrado entre lo que tiene arriba más 8·s y el borde de arriba de la zona del bote menos 8·s. Lo de arriba es la barra en su alto máximo (74, con el sello), o SPM, Split o Watts con objetivo, si se cruzan con él en x (agrandados 8·s);
- nunca toca una pieza, no se mueve con los valores y, al rotar, cambia de lugar en el acto.

| Perfil | Disco (64·s) | x | y | Lugar en y (para el disco) | Aire a los costados (pide 8·s) |
|---|---|---|---|---|---|
| TV | 67,4 | 171,8-239,2 | 238,2-305,7 | 86,6 (debajo de SPM con objetivo, hasta la zona del bote) | Watts 14,9 |
| P360 | 59,1 | 150,5-209,5 | 208,6-267,7 | 75,7 | Watts 13,1 |
| TH | 67,4 | 423,3-490,7 | 112,5-180,0 | 102,8 (debajo de la barra con el sello) | SPM 212,6, Split 267,6 |
| L640 | 59,1 | 290,5-349,5 | 98,6-157,7 | 90,1 | SPM 111,3, Split 154,1 |
| W1280 | 102,4 | 588,8-691,2 | 195,0-297,4 | 204,3 | SPM 268,9, Split 352,4 |

**Qué.**

- **El disco**: vidrio (`glass.fallback`), de 64·s.
- **El aro**, en su borde: 3 base, con la pista al 20 % y el arco al 100 % del color base del paso.
  - El arco ocupa la fracción del paso que falta para el cambio, desde las 12 en sentido horario, y se acorta por su punta.
  - Contra el peor vidrio, el color base da de 3,086 (otro tipo) a 9,358 (calentamiento): pasa el 3:1 de un gráfico.
- **La cifra**: `num.xl` 52/700 en blanco (13,493:1), centrada por la banda de las mayúsculas.
  - A 1,5 m, en TV, mide 54,8 dp y su mayúscula 38,4 dp (6,1 mm): 14′, 2,8 veces la agudeza.
  - Es el segundo número más grande del HUD, después de SPM.
- **Es discreta** porque no tiene texto, se apoya en el vidrio y está fuera del bote: se ve cuando importa y se va.

**Movimiento.**

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 |
|---|---|---|---|---|---|---|---|
| Entra (tick del 3): opacidad, `ease.enter` | 0 | 0,421 | 0,702 | 0,872 | 0,961 | 0,995 | 1 |
| Entra: escala de 0,85 a 1, `ease.enter` | 0,850 | 0,913 | 0,955 | 0,981 | 0,994 | 0,999 | 1 |
| Sale (tick del cambio): opacidad, `ease.exit` | 1 | 0,995 | 0,961 | 0,872 | 0,702 | 0,421 | 0 |
| Sale: escala de 1 a 1,10, `ease.enter` | 1 | 1,042 | 1,070 | 1,087 | 1,096 | 1,100 | 1,100 |

- **En cada tick** la cifra rueda (`DigitRoll`, 180, hacia abajo porque el valor baja): la nueva baja desde 0,35 em más arriba y la vieja sigue hacia abajo y se apaga a los 108.
- **El aro** es `restante visual / 3 s`, con el mismo relleno de §4.5. Está lleno en el tick del 3, en 2/3 en el del 2, en 1/3 en el del 1 y vacío en el cambio. Nunca se vacía antes del tick.
- **Al salir** el disco "se suelta": crece y se apaga en el cuadro en que empieza el paso nuevo, con el aro ya vacío.
- **En el último paso** sale en `Finished` (0-200), y el diálogo entra a los 300 (§4.10).
- **En pausa** queda a la vista, con la cifra y el aro quietos. Los pulsos de la barra siguen.
- **Al volver del segundo plano** o al despertar, se ve en el acto como corresponde, sin la entrada.
- **Reducir movimiento.** Solo opacidad: entra en 160 (`ease.enter`) y sale en 120 (`ease.exit`). La cifra cambia con el fundido de 80. El aro sigue continuo, porque es avance y no adorno.
- **Para la It. 5.** Con los paneles de series colapsados (140 base desde 14·s), quedan 9,5 dp de aire en TV y 8,3 en P360: entra. Expandidos (200 base), se cruzarían con el disco. Decide la It. 5. Por defecto, los paneles expandidos pasan a ser obstáculos del lugar y, si no entra, la cuenta grande no se muestra esa vez.
- **El panel del simulador** abierto en vertical tapa los 6 dp de abajo del disco (en TV, el panel empieza en y 299,5). Es la herramienta que abrió el usuario (04a/00-deuda-diseno §1.2).
- **Elementos:** 2. El disco y su aro (por `generateVisualContent`) y un `TabularNumber`.

### 4.9 El sello "Objetivo cumplido / No alcanzado" (D-ANI-05)

**Cuándo.** Al cerrarse solo un paso con objetivos (F-WKT-09), cumplido o no. No aparece en los pasos sin objetivo, en el paso cortado con `Terminar`, en el último paso (Q-52) ni en libre.

**Dónde.** En una tercera fila dentro de la barra de pasos: la barra crece de 52 a **74 base** (5 de aire y una fila de 17), anclada por arriba, hacia abajo.

- Siempre queda a 8·s de las tarjetas: en TV la barra termina en 86,4 y las tarjetas empiezan en 94,8; en P360, a 7,4; en W1280, a 12,8.
- Es la misma pieza de vidrio, que da una noticia, y no tapa nada: un sello flotante debajo de la barra quedaba apretado contra los rótulos de las tarjetas.
- El halo de las tarjetas con objetivo (12·s por fuera) pasa por debajo de los 4·s de abajo de la barra, y el vidrio lo tapa.

**Qué.**

- Un ícono de 14: `check_circle` relleno en `state.success`, o `error` en `state.warning`.
- A 5, un título en `headerS` 13/700, del mismo color: `Objetivo cumplido` o `No alcanzado`.
- Un detalle en `buttonS` 12/600 `text.secondary` con los promedios contra el objetivo, por ejemplo `· 182 / 180 W · 23.6 / 22 spm`. El contenido lo fija el PO (§6).
- Contraste contra el peor vidrio: 7,154 y 6,260 los títulos, 7,448 el detalle.
- **Por qué `state.warning` y no `phase.paused`**: `#FFD166` también es el color del calentamiento, que puede ser el tipo del paso siguiente en la misma barra. `#FF9800` es el ámbar de aviso de la app (6,260:1).

**Movimiento.** Son los verbos de la It. 3, "crecer" y "colapsar", con el ícono que se estampa:

| t (ms) | 120 | 150 | 180 | 200 | 233 | 267 | 300 | 333 | 380 | 420 | 500 | 600 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Alto de la barra (base; ω 12 desde los 120) | 52,0 | 53,1 | 55,6 | 57,5 | 60,7 | 63,6 | 66,0 | 67,9 | 70,0 | 71,2 | 72,7 | 73,5 |
| Contenido: opacidad (180-380, `ease.enter`) | 0 | 0 | 0 | 0,272 | 0,604 | 0,815 | 0,934 | 0,986 | 1 | 1 | 1 | 1 |
| Ícono: escala de 0,6 a 1 (180-420, `ease.emphasis`) | 0,600 | 0,600 | 0,600 | 0,738 | 0,902 | 0,997 | 1,035 | 1,036 | 1,013 | 1 | 1 | 1 |

| Salida, t (ms) | 2000 | 2040 | 2080 | 2120 | 2160 | 2200 | 2240 | 2280 |
|---|---|---|---|---|---|---|---|---|
| Contenido (2000-2120, `ease.exit`) | 1 | 0,961 | 0,702 | 0 | 0 | 0 | 0 | 0 |
| Alto de la barra (2080-2280, `ease.move`) | 74,0 | 74,0 | 74,0 | 73,1 | 68,5 | 57,5 | 52,9 | 52,0 |

- El resorte de ω 12 llega al 98 % a los 610 ms. El sello dura 2 s desde el cambio de paso: a los 2000 empieza a irse.
- **Si se cierra otro paso con objetivos** mientras se ve (pasos de 2 s o menos), el contenido se reemplaza (el viejo funde en 0-80 y el nuevo entra en 80-200, con el ícono que se estampa otra vez), los 2 s vuelven a empezar y la barra sigue abierta. Un paso sin objetivos no lo toca. Desde `Finished` sigue su reloj, y la última foto puede llevarlo.
- **En pausa** el sello sigue sus 2 s, que son del reloj de la interfaz.
- **Reducir movimiento.** El alto de la barra cambia en el acto, al entrar y al salir. El contenido funde en 160 y en 120, y el ícono no escala.
- **`HudOverlap`** mide la barra con su rectángulo de ese momento.
- **Elementos:** 3. El ícono, una etiqueta con el título y el detalle en texto enriquecido, y la fila.
- **Del motor** hace falta el resultado de cada paso con sus promedios por objetivo (DD4b-12).

### 4.10 El diálogo de rutina completada (F-WKT-21)

Aparece solo cuando la rutina termina sola, una sola vez (F-WKT-21; al terminar a mano, nunca: Q-53). `t = 0` en el cuadro en que la fase pasa a `Finished`:

- **En el acto:** los controles al 0,38;
- **0-120:** la cuenta rueda a `0:00` y el último paso completa su relleno;
- **0-200:** la cuenta grande sale;
- **"Próximo" y "por terminar"** terminan (04a/04-design-notes §5.4);
- el último paso no tiene sello.

El motor espera al diálogo (DT4a-15). A los 300, con la barra ya completa y quieta, entra el diálogo:

| t (ms) | 300 | 333 | 367 | 400 | 433 | 467 | 500 | 520 | 600 | 700 | 800 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Velo de 0 a 0,6 (`ease.move`, 200) | 0 | 0,016 | 0,090 | 0,300 | 0,510 | 0,584 | 0,600 | 0,600 | 0,600 | 0,600 | 0,600 |
| Diálogo: opacidad (`ease.enter`, 220) | 0 | 0,389 | 0,660 | 0,835 | 0,937 | 0,985 | 0,999 | 1 | 1 | 1 | 1 |
| Diálogo: escala de 0,96 a 1 | 0,960 | 0,976 | 0,986 | 0,993 | 0,997 | 0,999 | 1 | 1 | 1 | 1 | 1 |
| Check: el disco de 0,6 a 1 (360-600, `ease.emphasis`) | 0,600 | 0,600 | 0,650 | 0,846 | 0,966 | 1,025 | 1,039 | 1,034 | 1 | 1 | 1 |
| Check: el trazo (480-800, `ease.move`) | 0 | 0 | 0 | 0 | 0 | 0 | 0,003 | 0,014 | 0,208 | 0,875 | 1 |

- **El check** es un solo elemento dibujado por código, de 32 base (el legacy usa 28). El disco `state.success` crece y, encima, se dibuja el trazo del check, recortado del color del diálogo. Quieto, es el `check_circle` del legacy. Si el Architect prefiere el glifo, alcanza con la escala, sin trazo.
- **Los totales** (Tiempo, Distancia y Calorías) cuentan de 0 a su valor exacto en 700 (`ease.enter`), escalonados 120: 520-1220, 640-1340 y 760-1460. Cada cuadro muestra el valor intermedio con el formato del legacy, y el último, el exacto (ANIMATION_BIBLE §2.5):

  | t (ms) | 600 | 700 | 800 | 900 | 1000 | 1100 | 1220 | 1340 | 1460 |
  |---|---|---|---|---|---|---|---|---|---|
  | Tiempo | 0,306 | 0,589 | 0,781 | 0,902 | 0,967 | 0,995 | 1 | 1 | 1 |
  | Distancia | 0 | 0,237 | 0,540 | 0,749 | 0,883 | 0,958 | 0,995 | 1 | 1 |
  | Calorías | 0 | 0 | 0,163 | 0,488 | 0,714 | 0,861 | 0,958 | 0,995 | 1 |

- **Opaco** (`surface.overlay`), como el diálogo de terminar de la 4a, hasta que llegue el desenfoque (6b): `@92 %` sin desenfoque deja ver el HUD fantasma detrás del velo.
- **Contraste**, opaco: el check 6,626; los íconos `accent.primary` sobre la caja del resumen 4,501 (gráfico); los rótulos 6,361; los valores 11,093; el cuerpo 6,984.
- **Es modal**: no se cierra tocando afuera, con Escape ni con "Atrás" (paridad, `:209-211`).
- **La salida** es la de §4.3.
- **Reducir movimiento.** El diálogo entra solo con opacidad (It. 3), el check aparece completo con él y los totales, en su valor, sin contar.
- **La anatomía** (medidas, textos y botones) va en las 04-design-notes de la 4b, con la spec.

### 4.11 El velo de pausa

- **Al pausar**, el fondo se oscurece un 15 % en 300 (`ease.move`), como negro al 15 % encima: el cielo pasa de `#0B4FA8` a `#09438F` y el agua de `#0E4F8F` a `#0C437A`.
- **Al reanudar**, vuelve en 240.
- **El HUD no se apaga** (ART_BIBLE §3.2): el vidrio sobre el fondo oscurecido pasa de `#08162A` a `#071527` (cielo) y de `#081627` a `#081524` (agua). El contraste solo sube.
- **Sin elementos nuevos**: es el tinte de la textura del fondo, o lo que el Architect prefiera, siempre que dé esos colores.
- **En la 6a** lo reemplaza el *Volume* de URP: desatura un 30 % y oscurece un 15 % (ANIMATION_BIBLE §2.11).
- **Reducir movimiento:** igual, porque es un fundido.
- **QA:** cambian las sondas del fondo de CAP-14 (pausa).

### 4.12 La barra libre: el chip de fase y el reloj de pared (F-WKT-16 y F-WKT-19)

**El chip de fase.**

- **Ancho fijo**, el de la más larga de sus tres etiquetas en el idioma de la app: así el reloj nunca se corre cuando cambia la fase.
- Tinte de la fase al 15 %, borde al 50 %, `r.m`, relleno de 8 × 3. Texto de 11 en Inter 700, con tracking de 0,08 em. El legacy usa 800, que no existe en el repo.
- **Texto en blanco** (propuesta, como DV-38):

  | Fase | Texto en su color | Texto en blanco |
  |---|---|---|
  | `EN MARCHA` | 5,324 | 10,041 |
  | `PAUSADO` | 6,432 | 9,275 |
  | `FINALIZADO` | 3,210 (3,942 con `.hud`) | 11,621 |

  En su color, `FINALIZADO` no llega a 4,5. En blanco, el color de la fase queda en el tinte, el borde y el punto.
- **Un punto "en vivo"** de 6 base en el color de la fase. En `EN MARCHA` late entre 0,35 y 1 (800 + 800 ms, `ease.pulse`). En `PAUSADO` y `FINALIZADO` queda fijo en 1.
- **Al cambiar la fase**, los colores cruzan en 150 (`ease.move`) y la etiqueta se reemplaza (0-80 y 80-200, como el texto de un aviso).
- **Reducir movimiento:** el punto, fijo.

**El reloj de pared**, en las dos barras.

- `schedule` 16 al 70 %, 5 de aire y la hora `HH:mm` en `num.s` 20/700 tabular: el mismo tamaño que la cuenta, como pide el legacy (`series_panels.dart:99`).
- En blanco, por paridad. Optativo: al 70 %, para que quede debajo de la cuenta (§6).
- Al cambiar el minuto, rueda (`DigitRoll` 180, hacia arriba; a medianoche, hacia abajo). Si para el presupuesto se arma como una sola etiqueta (§4.15), cambia con un fundido de 80: es una vez por minuto y no se pierde nada que se lea.
- Lee el reloj inyectable con la zona horaria del sistema. En el arnés, con una zona fija.
- Sigue en pausa.
- **En la barra de pasos ocupa unos 79 base de la fila** (10 + 16 + 5 + 48,1). En TV, con `Próximo` a la vista, la pastilla de objetivo baja de 128,4 a unos 49 base y se lee `>160W · …`. Es la paridad (el legacy hace lo mismo en un teléfono de 400 dp), pero se lee peor. Si se prefiere, el reloj puede ceder su lugar a `Próximo` mientras `Próximo` se ve. Lo decide el PO (§6).

### 4.13 Reducir movimiento (resumen)

| Qué | Con reducir movimiento |
|---|---|
| Entrada | Todo junto: fundido lineal de 120, sin cascada ni subida |
| Salida | Fundido lineal de 120, con la última foto |
| Barras del sistema | Igual (las mueve el sistema) |
| Relleno de la píldora y aro de la cuenta | Igual: son avance |
| Halo de los últimos 5 s | Fijo en 0,70, con la rampa de 150 |
| Píldora que termina | Destello y opacidades iguales. El borde de la no cumplida funde en 160, sin trazo |
| Cuenta grande | Entra en 160 y sale en 120, solo opacidad. La cifra, con el fundido de 80 |
| Sello | El alto, en el acto. El contenido funde en 160 y en 120. El ícono no escala |
| Diálogo de completado | Solo opacidad. El check, completo. Los totales, sin contar |
| Velo de pausa | Igual (es un fundido) |
| Chip de fase | El punto, fijo. Los colores y la etiqueta, iguales |
| Reloj de pared | Fundido de 80 |

Lo que no cambia: los datos, cuándo cambian, el *spinner* y los fundidos de color. Una toma fija con y sin reducir movimiento sale igual, salvo los realces fijos: el halo en 0,70 y los de la 4a.

### 4.14 Constantes (para `MotionSpecs` y `HudGeometry`)

Las que ya son tokens o constantes de la 4a se reusan: `dur.base`, `shift.s`, `DigitRoll.DurationMs`, `HudColorMs`, `FabInScaleFrom`, `GrowOmega` y las de `CollapseMotion`, `ToastTextOutMs` / `ToastTextInEndMs`, las del diálogo (`DialogInMs`…) y `ReducedImmersiveFadeMs`. Los nombres que siguen son sugeridos.

| Constante | Valor | Constante | Valor |
|---|---|---|---|
| `ImmersiveCascadeStartMs` / `ImmersiveCascadeStaggerMs` | 120 / 40 | `ImmersivePieceInMs` / `ImmersivePieceShiftDp` | 240 (`dur.base`) / 8 (`shift.s`) |
| `SystemBarsOutMs` / `SystemBarsInMs` | 240 (`ease.enter`) / 200 (`ease.exit`) | `ImmersiveEntryEndMs` | 560 (la llave y el panel entran ahí) |
| `PillFillCompleteMs` | 120 (`ease.move`) | `PillSolidifyStartMs` / `PillSolidifyEndMs` / `PillFlashPeak` | 120 / 340 / 0,40 |
| `PillMissedStartMs` / `PillMissedEndMs` | 120 / 440 (`ease.move`) | `PillTrackInMs` | 150 |
| `PillGlowMin` / `PillGlowMax` / `PillGlowRampMs` | 0,30 / 0,70 / 150 | `PillGlowBlurDp` / `PillGlowSpreadDp` / `PillGlowRadiusDp` | 8 / 2 / 5 |
| `ReducedPillBorderFadeMs` | 160 | `PillRepaintPx` | 0,25 |
| `BigCountFromSeconds` | 3 | `BigCountInMs` / `BigCountInScaleFrom` | 200 / 0,85 |
| `BigCountOutMs` / `BigCountOutScaleTo` | 200 / 1,10 | `ReducedBigCountInMs` / `ReducedBigCountOutMs` | 160 / 120 |
| `SealStartMs` / `SealContentInStartMs` / `SealContentInEndMs` | 120 / 180 / 380 | `SealIconScaleFrom` / `SealIconEndMs` | 0,6 / 420 (`ease.emphasis`) |
| `SealOutStartMs` | 2000 (y después `CollapseFadeMs` 120 y `CollapseStartMs` / `CollapseEndMs` 80 / 280) | `SealSwapOutMs` / `SealSwapInEndMs` | 80 / 200 |
| `CompletedDialogDelayMs` | 300 | `CompletedCheckScaleFrom` | 0,6 |
| `CompletedCheckScaleStartMs` / `EndMs` (desde que entra el diálogo) | 60 / 300 | `CompletedCheckStrokeStartMs` / `EndMs` | 180 / 500 |
| `TotalsCountMs` / `TotalsStaggerMs` / `TotalsStartMs` (desde que entra el diálogo) | 700 / 120 / 220 | `PauseVeilAlpha` / `PauseVeilInMs` / `PauseVeilOutMs` | 0,15 / 300 / 240 |
| `LiveDotHalfMs` / `LiveDotMinOpacity` | 800 / 0,35 | `PhaseChipColorMs` | 150 (= `HudColorMs`) |

En `HudGeometry`:

- **La barra:** `TopBarHeightSteps` 52, `TopBarHeightSeal` 74 (`SealRowGap` 5, `SealRowHeight` 17) y `TopBarHeightFree` 40.
- **Las píldoras:** `PillRowPad` 10 / 8 / 10 / 4, `PillHeight` 8, `PillRadius` 4, `PillGap` 3, que baja a 1 y a 0, y `PillMinWidth` 6 / 2.
- **La cuenta grande:** `BigCountDiameter` 64, `BigCountRingStroke` 3 y `BigCountGap` 8, todos × s.

### 4.15 Presupuesto de elementos (R4a-03)

| Caso (rutina con objetivos en SPM y Watts, BPM y `Próximo`) | 4a | 4b, sin juntar nada |
|---|---|---|
| En marcha | 59 | 59 + 1 (píldoras) + 2 (reloj de pared) = 62. Con la cuenta grande, 64. Con la cuenta y el sello a la vista, **67** |
| Guardando, con el *spinner* | 60 | 63 (sin cuenta ni sello del último paso). Si se termina a mano con un sello a la vista, **66** |

Hay que juntar 7. Mi propuesta es esta, sin que cambie lo que se ve (las sondas de 04a/04-design-notes §8.2 tienen que dar lo mismo):

| Qué | Ahorro | Cómo |
|---|---|---|
| La flecha ▲/▼ la dibuja su `TabularNumber`, como prefijo con su fuente, su color, su opacidad y su desplazamiento | −3 (SPM, Split, Watts) | Es la salida de R4a-03 |
| Las unidades `W` y `m` las dibuja su `TabularNumber`, como sufijo con su fuente, su color al 80 % y su línea de base | −2 | — |
| `Próximo:` y el tipo siguiente, en una sola etiqueta con texto enriquecido | −1 | El fondo que late sigue aparte (DV-38) |
| El reloj de pared, en una sola etiqueta (el ícono con la fuente de íconos y la hora con la derivada, que ya es tabular) | −1 | Pierde el rodillo del minuto: cambia con un fundido de 80, una vez por minuto |

Con eso, el peor caso queda en **60** (en marcha, con la cuenta y el sello) y en **59** guardando. Si el Architect mide que el tope puede subir sin costo, que lo proponga con la medición. Mientras tanto, rige el de 60 (ARCHITECTURE §7.8).

### 4.16 Capturas y videos

**Reglas de toma**, además de las de 04a/04-design-notes §8.1:

1. Sin transiciones en curso: ni la cascada, ni el cambio de paso de las píldoras (destello o trazo), ni la entrada o salida de la cuenta grande, ni el sello creciendo o colapsando, ni la entrada del diálogo, el check o los totales, ni el velo.
2. **El relleno y el aro dependen del instante**, que fija el reloj de prueba. La toma espera el valor de `PillProgress` en ese instante: con `AtSecond(n)`, entre 0,45 y 0,95 s después del tick, el relleno incluye esa fracción.
3. La cuenta grande se fotografía entre ticks, con el rodillo quieto (por ejemplo, con restante 2, a +0,5 s: el aro en 0,5). El sello, a +1 s del cambio. El diálogo, después de los 1460 ms y de `Settle`.

**Videos que propongo** (los ID los fija la spec):

| Video | Qué tiene que verse |
|---|---|
| Entrada | 1 s a 30 cuadros por segundo con los valores de §4.1 |
| Cambio de paso | Unos 8 s alrededor de dos cambios, uno cumplido y uno no: el halo de los últimos 5 s, la cuenta grande del 3 al 1, el destello o el trazo, la pista nueva y el sello que crece y se va. Valores de §4.6 a §4.9 |
| Completada | Los últimos 3 s del último paso, el diálogo con el check y los totales, `Finalizar` y la salida con la última foto (§4.3 y §4.10) |

**Ninguna caja salta entre dos cuadros**: la barra crece y colapsa con el sello, y el resto no se mueve.

---

## 5. Pedidos al Architect

Ninguno cambia el comportamiento del producto. Los marcados **(D-ANI-05)** dependen del gate, y si el usuario rechaza D-ANI-05 queda la cuenta regresiva normal (ROADMAP §6).

| ID | Qué | Cómo se verifica |
|---|---|---|
| DD4b-01 | **El divisor de la banda** (QA-V4a-03): el arreglo en curso, con la sonda de aspecto de §1.3 en CAP-12 y en CAP-15. Además, la regla general: todo adorno de tamaño fijo del HUD lleva `flex-shrink: 0` (ART_BIBLE §3.6) | `BandDividerKeepsItsWidth` (en curso); las tomas de §1.2 y §1.3 |
| DD4b-02 | **Las cifras y K2** (QA-V4a-06): la integración en curso | La lista de §2.2 |
| DD4b-03 | **El "…" de la pastilla, en el color de su texto** (§2.4) | CAP-21 y CAP-27c: el "…" en blanco, ± 3 |
| DD4b-04 | **El panel abierto sigue a la llave sin saltos** (§3.2): en `SimulatorLayerView.ApplyPanel(bottom)`, el alto máximo sale de `SimulatorPanelLayout.MaxHeight(alto, márgenes, bottom, reserva, techo)` con el borde de abajo del cuadro, no con el del destino | PlayMode (`-Simulator`), en L640: con el panel abierto en Rutinas, abrir el editor y volver; en cada cuadro del resorte, el borde de arriba del panel queda en 32 ± 0,5 (nunca por encima de la ventana), y al final el alto máximo es 312 y 248. En TV, el panel se traslada con su alto (409 ± 0,5) |
| DD4b-05 | **La última foto** (O-01, §4.3): desde el primer estado de reposo, `ImmersiveWorkoutPage` no le pasa datos al HUD. Termina lo finito, sigue lo que se repite hasta que la página sale y no empieza nada nuevo | PlayMode: al confirmar `Terminar` con la banda en `00:50`, durante toda la salida `#hud-time-value` dice `00:50` y la distancia, la final (hoy dice `00:00` a los 120 ms). Lo mismo con `Finalizar` en el diálogo de completado |
| DD4b-06 | **La entrada "sumergirse"** (§4.1): la cascada por zonas (opacidad y `translate`, nunca layout), con las constantes de §4.14; la transición de la página termina a los 560 y ahí entran la llave y el panel abierto; el corte si la sesión se cierra en el medio; reducir movimiento | PlayMode con el reloj inyectado: los valores de la tabla de §4.1 (± 0,01 y ± 0,1 dp). Video de la entrada |
| DD4b-07 | **Las barras del sistema y los dos juegos de márgenes** (§4.2): un puerto en Core (por ejemplo, `ISystemBars.SetHidden`), su implementación en Android (inmersivo persistente, con la animación controlada si la hay) y la de Windows, que no hace nada; los márgenes de la app y los del inmersivo, que no cambian con la visibilidad de las barras; el HUD con los del inmersivo desde el *push* | dotnet: la regla (ocultas si y solo si arriba está el inmersivo y no está saliendo). PlayMode con un doble del puerto: se ocultan en el cuadro del *push* y vuelven en el cuadro 0 de la salida; si el doble cambia los márgenes de las barras, ni el HUD ni las pestañas se mueven. [Device-And]: NO VERIFICABLE sin teléfono |
| DD4b-08 | **Las píldoras en un solo elemento** (§4.4): la geometría (`HudGeometry`: barra de 52 y 74, fila 10 / 8 / 10 / 4, píldora de 8 con radio 4, separación de 3 a 1 y a 0, y la tira continua), los estados del contrato con el brillo de 1 px, el halo recortado a la fila y la cinta. `step_pill_test` portado | dotnet: el caso del legacy, los anchos de §4.4 y la regla de muchos pasos. PlayMode: un solo `VisualElement` para la fila, con cualquier cantidad de pasos; 0 B por cuadro |
| DD4b-09 | **`PillProgress`** (§4.5), en `Core/Design` | dotnet: nunca pasa de `(transcurrido + 1) / D`; nunca baja dentro de un paso; vale `transcurrido / D` en cada tick que llega a tiempo; quieto en pausa; la tabla de §4.5 (± 0,001 s); `Instant` en el acto; 0 en los pasos por distancia o con `D ≤ 0` |
| DD4b-10 | **El cambio de paso de las píldoras y el halo de los últimos 5 s** (§4.6 y §4.7), con el reloj común de la 4a y reducir movimiento | PlayMode cuadro a cuadro con los valores de las tablas (± 0,01). Video del cambio de paso |
| DD4b-11 | **La cuenta grande** (D-ANI-05, §4.8): el lugar "centro alto" en `HudGeometry` (por ventana, con los obstáculos de §4.8), el disco con su aro por `generateVisualContent`, la cifra con `TabularNumber` y los tiempos | dotnet: los rectángulos de §4.8 (± 0,1) y el aire de 8·s. PlayMode: entra en el tick del 3 y sale en el del cambio; el aro igual a `PillProgress`; `HudOverlap` en verde |
| DD4b-12 | **El sello** (D-ANI-05, §4.9): la fila dentro de la barra, el crecimiento y el colapso, el reemplazo y los íconos `check_circle` y `error`, que ya están en `Icons.cs`. **Del motor:** el resultado de cada paso con el promedio de cada objetivo (W, SPM y split), cuando el paso se cierra | dotnet: el resultado con sus promedios (los casos de `step_target_tracker_test`). PlayMode: los valores de §4.9; la barra a 8·s de las tarjetas; sin sello en el último paso, en un paso sin objetivo ni al terminar a mano |
| DD4b-13 | **El diálogo de completado** (§4.10): la espera de 300 y la del motor (DT4a-15), el check dibujado, los totales que cuentan y la salida junto con la página | PlayMode: los valores de §4.10; nunca aparece al terminar a mano (Q-53); con `Finalizar`, el diálogo y la página salen en el mismo cuadro |
| DD4b-14 | **El velo de pausa** (§4.11), sin elementos nuevos | CAP-14: el cielo `#09438F` y el agua `#0C437A`, ± 1 |
| DD4b-15 | **El chip de fase y el reloj de pared** (§4.12): el ancho fijo, el punto, el texto (en blanco, si el PO lo acepta) y el reloj con el reloj inyectable y la zona fija en el arnés | PlayMode: el reloj no se mueve cuando cambia la fase; el minuto cambia en su cuadro |
| DD4b-16 | **El presupuesto** (§4.15): juntar 7 elementos sin cambiar lo que se ve | `HudBudgetTests` en el peor caso de la 4b (en marcha con la cuenta y el sello, y guardando con un sello) ≤ 60; las sondas de la 4a, iguales |
| DD4b-17 | **Las constantes** de §4.14 en `MotionSpecs` y `HudGeometry` | dotnet: que las tablas de §4 salgan de las constantes |
| DD4b-18 | **El arnés**: las reglas de toma de §4.16, el relleno y el aro esperados en cada toma, y los tres videos | Dos corridas seguidas con los mismos bytes |

**Prioridad:** primero DD4b-01 a DD4b-04, que son la deuda de la 4a y las condiciones. Después DD4b-05 a DD4b-07, porque son la transición y DD4b-05 cierra O-01. Después DD4b-08 a DD4b-10 y DD4b-16, porque sin el presupuesto no entran las píldoras. Al final, DD4b-11 a DD4b-15, DD4b-17 y DD4b-18.

---

## 6. Para el PO y para QA

| Para | Qué | Propuesta |
|---|---|---|
| PO · D-ANI-05 | Lo que se muestra en el gate | La cuenta grande (§4.8) y el sello (§4.9) como están acá. Textos nuevos del sello: `Objetivo cumplido` / `Target met` y `No alcanzado` / `Target missed`. Detalle: con el objetivo cumplido, todos los promedios (`182 / 180 W · 23.6 / 22 spm`); sin cumplir, solo los que fallaron, en el orden W, SPM y split. Los números, con los formatos del HUD: W entero, SPM con un decimal y split en `m:ss`. Si el detalle no entra, se recorta con "…"; el título nunca |
| PO · DV nuevas (propuestas) | Diferencias visibles con el legacy | El velo de pausa (§4.11). El chip de fase con el texto en blanco y el punto "en vivo" (§4.12; es la misma razón que DV-38). La entrada en cascada y las barras que se desvanecen (§4.1 y §4.2, la mejora EXR de F-APP-15). El destello y el trazo de las píldoras, y el relleno continuo (la mejora EXR de F-WKT-15). En el diálogo de completado: opaco hasta la 6b, el check de 32 (el legacy usa 28) y los totales que cuentan (la mejora EXR de F-WKT-21) |
| PO · decisiones chicas | Con valor por defecto | **La tira continua** con más de 115 pasos en TV (§4.4): por defecto, sí. **El reloj de pared** en blanco (por defecto, paridad) o al 70 % (§4.12). **Con `Próximo` a la vista en una ventana angosta**, el reloj se queda (por defecto, paridad) o le cede su lugar (§4.12) |
| PO · spec | Para los criterios | AC-WKT-17-04 y AC-APP-05-07 se vuelven a pedir. La salida con la última foto cierra O-01. AC-BLE-16-02 ya pide "sin saltos" y no cambia: DD4b-04 lo cumple. P-17: al despertar, todo en el acto (§4.0) |
| PO · It. 5 | Los paneles de series y la cuenta grande | Colapsados, entran (§4.8). Para los expandidos, por defecto la cuenta grande no se muestra si no entra. Decide la It. 5 |
| QA | Las condiciones | La lista de §1.2 y §2.2 sobre la regeneración única; el "…" en blanco (§2.4); la PlayMode de DD4b-04; las reglas de toma de §4.16; las sondas nuevas de CAP-14 (§4.11) |
| QA | Riesgo para la 6a | La píldora hecha de enfriamiento y la de otro tipo, contra una nube (§4.4) |
| Coordinador | Los programas | Están en `<scratch>/it4b/designer-debt/` y no se versionan. `uv run --offline --with fonttools==4.68.0` funciona sin red con la caché de esta PC |

---

## 7. Alineación de las biblias

| Biblia | Dónde | Qué cambió |
|---|---|---|
| ANIMATION_BIBLE | §0.2 | D-ANI-05 remite a §4.8 y §4.9 de este documento |
| ANIMATION_BIBLE | §1.2 | `ease.emphasis`: el ícono del sello y el check del diálogo. La píldora se solidifica con un destello, no con rebote |
| ANIMATION_BIBLE | §2.6 | El relleno con `PillProgress` (el tope del tick siguiente, la pausa y Q-58); el halo con el reloj común y su rampa; la cuenta grande de los últimos 3 s |
| ANIMATION_BIBLE | §2.7 | Las píldoras en el cambio de paso con los tiempos de §4.7, sin marcador que viaje; el sello dentro de la barra (§4.9), no "bajo la barra" |
| ANIMATION_BIBLE | §2.11 | La cascada de la 6a con +8 (`shift.s`); la entrada "sumergirse" de la 4b (§4.1) y las barras del sistema (§4.2); la salida con la última foto (§4.3); el diálogo de completado (§4.10) y el velo de pausa de la 4b (§4.11) |
| ANIMATION_BIBLE | §2.14 | El panel abierto sigue a la llave sin saltar (§3.2); la llave cambia de carril al terminar la cascada |
| ANIMATION_BIBLE | §7.5 | Las pruebas de `PillProgress` |
| ART_BIBLE | §3.3 | La barra de 52 (y 74 con el sello), la regla de muchos pasos, la cuenta grande y el sello; el chip de fase en 700, con el texto en blanco si el PO lo acepta; el reloj de pared en `num.s` |
| ART_BIBLE | §3.5 | El diálogo de completado, opaco hasta la 6b, con el título en 600 como los demás diálogos |
| ART_BIBLE | §5.2 | El lugar "centro alto" de la cuenta grande |
| ART_BIBLE | §10.1 | El riesgo de contraste de las píldoras para la 6a |
| Las dos | Al final | La sección "Cambios al iniciar la It. 4b" |

---

## 8. Verificación hecha

1. **Legacy, solo lectura.** Se leyeron los archivos del encabezado. `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar, y no se leyó `strava_secrets.dart`.
2. **El trabajo en curso del Architect** (QA-V4a-03 a -06), leído con `git diff` sin tocarlo: `HudBand.cs`, `Base.uss`, `HudLayoutTests.cs`, `ImmersiveHudTests.cs`, `FontAssetPaths.cs`, `KerningList.cs`, `TabularNumber.cs` y `Typography.uss`, y los `FontAsset` generados (solo lectura).
3. **Las medidas**, con los programas del encabezado:
   - las curvas, con las cubic-bezier de `Tokens.cs` y el método de `Easing.cs`, y el resorte con la solución exacta de `CriticalSpring`;
   - los contrastes, con el método de `Contrast.cs` (mezcla sRGB sin redondear, contra `#2A2F37` y `#2B3038`);
   - el kerning, con fontTools 4.68.0 sobre las listas K2 y los `.asset` del último commit: los pares que les faltaban eran 534, 1 026, 761 y 1 506 en Inter 400, 500, 600 y 700, y ninguno tenía otro valor. Sobre los `.asset` que escribió después el *bootstrapper*: cada tabla es exactamente su lista (§2.1);
   - las cifras, con los `.ttf` de Barlow y de la derivada.
4. **Capturas miradas:** CAP-19 (TV) y CAP-23 (TH) de la 4a: el lugar libre de la cuenta grande, entre las tarjetas y arriba de la zona del bote, y el divisor de la banda, que en TV casi no se ve y en TH se ve apenas.
5. **El repositorio.** Se escribieron este documento, 04a/00-deuda-diseno (§1.6, §1.7 y su sección "Precisiones al empezar la It. 4b") y las dos biblias. No se tocó ningún token, código, asset ni captura. Unity no se abrió y dotnet no se corrió.
