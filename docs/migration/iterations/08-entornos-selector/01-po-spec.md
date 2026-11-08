# It. 8 — Río, Costa, Regata y selector de escenario: spec de producto

| Campo | Valor |
|---|---|
| Iteración | 8 (`08-entornos-selector`), [ROADMAP §It. 8](../../ROADMAP.md) |
| Incluye | F-SCN-16, F-SCN-17, F-SCN-18, F-SCN-19, F-SCN-20, F-SCN-21, F-SCN-22 ([immersive-scene](../../product/inventory/immersive-scene.md)) |
| También toca | F-WKT-12 (sección `Escenario` de la vista de reposo), F-SCN-12 (oleaje de la costa), F-SCN-14 (orillas y topes de los otros escenarios), F-SCN-06 (rolido por escenario), F-SCN-01 (rendimiento y ciclo de vida con 4 escenarios) |
| Base | La escena de la 6a (agua **degradada**) y el remero de la 7. La 6b sigue postergada (issue #6): nada de esta spec depende del agua avanzada |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`); `scene/` = `lib/features/workout/scene/`. Leídos enteros: `scene/environment.dart`, `environments/river_environment.dart`, `coast_environment.dart`, `regatta_environment.dart`, `painters/shore_painter.dart`, `painters/thumbnail_painter.dart`, `environment_picker.dart`, `scene_settings.dart`, `features/workout/workout_screen.dart:60-100`, `l10n/app_es.arb` y `app_en.arb:56-60` y los tests `environment_test`, `environment_picker_test`, `water_painter_test`, `sky_shore_painter_test`, `scene_view_test`. `git -C ../rowmate-legacy status --porcelain` vacío |
| Modo | Entrega rápida, sin debates. Ante una duda, el legacy o el valor por defecto, anotado en una línea (§12). Lo menor va a "Para issues" (§14) |
| Criterios | **40** = 37 nuevos (§3) + 3 que cambian (§4). **40 casos legacy** (LT-99 a LT-138, §5). **46 capturas, 3 LEG, 6 videos y 5 tomas de demo** (§6) |

---

## 0. Cómo leer esta spec

- Notación, tamaños (**V390**, **H844**, **TV**, **P360**, **TH**, **L640**, **W1280**), marcas (`[dotnet]`, `[Editor-EM]`, `[Editor-PM]`, `[Captura]`, `[Device-Win]`, `[Device-And]`, `[QA]`, `[PERF]`) y estados de prueba (`R-4`, `F-5`, hora de prueba, carpeta de datos temporal `ROWMATE_DATA_DIR`, escena sola, respiración en 0, tiempo de escena 3,2 s, escena demorada): los de la [spec de la 6a §0](../06a-escena-lago/01-po-spec.md) y la [de la 7 §0](../07-remero-palada/01-po-spec.md), sin cambios. `u` = un `nextDouble()` del generador del legacy (AC-SCN-14-02).
- IDs nuevos: `AC-SCN-nn-mm` siguen a los de la 6a y la 7 en cada ficha; `LT-99` a `LT-138` siguen a LT-98; `DV-79` a `DV-89` siguen a DV-78.
- **Nuevos estados de prueba** (no existen en un build que se instala):
  - **Escenario forzado**: el `prefs.json` de la carpeta temporal arranca con `scene.environment` = `lake`, `river`, `coast` o `regatta` (o sin la clave, o con `"marte"`).
  - **Distancias forzadas** (además de `E-0`, `E-920`, `E-1234` y `E-42K` de la 6a): **`E-180`** (regata: tribuna a 30 m, cartel `250 m` a 70 m, cambio de color de boyas), **`E-220`** (regata: cartel `250 m` a 30 m, como `sky_shore_painter_test:57`), **`E-250`** (río: puente a 20 m), **`E-266`** (río: el puente a 4 m de la cámara), **`E-500`** (costa: faro a 30 m, playa a 100–150 m), **`E-720`** (regata: cartel `750 m` a 30 m). "En régimen desde `E-x`" = la escena arranca en esa distancia ya a 4,0 m/s con `R-4`.

---

## 1. Objetivo y demo

**Objetivo.** Los 4 escenarios del legacy, con el mismo paisaje a la misma distancia (Q-61) y un look realista: **Río urbano** (edificios, clubes, faroles, puentes de piedra), **Costa** (acantilados, playas, faro, gaviotas y oleaje) y **Canal de regata** (boyas cada 10 m, carteles cada 250 m, tribunas, torre y banderas). En Workout en reposo, la sección `Escenario` con 4 miniaturas para elegir; la elección sobrevive al reinicio.

**Demo** (build de simulador, W1280, español, hora real):

1. Workout en reposo: arriba de `Entrenamiento libre`, el título `Escenario` y 4 tarjetas: `Lago alpino` (elegida, borde celeste), `Río urbano`, `Costa`, `Canal de regata`, cada una con su miniatura del mediodía.
2. Elegir `Río urbano` → `Entrenamiento libre` → `Fuerte`: se rema entre edificios y faroles; a los 270 m (distancia de escena) la cámara pasa por debajo de un puente de piedra. `Hora escena` a `22:00`: ventanas y faroles encendidos, skyline con ventanas.
3. Salir, elegir `Costa`: mar abierto a la derecha con oleaje que mueve el bote; acantilados, playas con sombrillas y, a los 530 m, el faro; 5 gaviotas cruzan el cielo.
4. Elegir `Canal de regata`: 3 carriles con boyas cada 10 m, rojas hasta los 250 m y amarillas después; el cartel `250 m` pasa a la izquierda; tribuna a los 210 m, torre de llegada a los 610 m, banderas que flamean.
5. Cerrar la app con `Canal de regata` elegido y abrirla: Workout muestra `Canal de regata` elegido desde el primer cuadro, y el entrenamiento rema en la regata.

**Lo que depende del usuario:** nada que bajar (todo el arte nuevo es procedural o propio). Sin el teléfono Android, los 60 fps en Android quedan NO VERIFICABLE (regla 9).

---

## 2. Historias de usuario

| ID | Historia |
|---|---|
| H-01 | Como **remero**, quiero elegir dónde remar (lago, río, costa o regata) antes de empezar, viendo cómo es cada lugar, para variar mis sesiones. |
| H-02 | Como **remero**, quiero que la app recuerde mi escenario, para no elegirlo cada vez. |
| H-03 | Como **remero de competencia**, quiero una pista con carriles y carteles cada 250 m, para sentir una regata. |
| H-04 | Como **remero habitual**, quiero reconocer "mi" recorrido en cada escenario (los mismos edificios, el mismo faro en el mismo metro), sesión tras sesión. |

---

## 3. Criterios de aceptación

### 3.1 F-SCN-14 — Orilla de los otros escenarios

- **AC-SCN-14-08 · El mismo generador para los 4 (Q-61).** *Entonces* río, costa y regata generan sus segmentos de 50 m con la semilla `índice·1 000 003 + k·7 919 + 17` (río 1, costa 2, regata 3) y el generador del legacy de AC-SCN-14-02, consumiendo los valores en el orden del legacy:
  - **río**, por cada orilla (izquierda, después derecha): `u` del z inicial (`z₀ + u·8`); por cada edificio, `u` del tipo (`< 0,3` club), `u` de x, `u` de escala, `nextInt(1000)` de variante y `u` del paso; faroles, boya y puente no consumen;
  - **costa**: en playa, por sombrilla `u` de x, `u` de z y `nextInt(1000)`; en acantilado, por cada uno `u` de escala y `nextInt(1000)`; el faro no consume;
  - **regata**: no consume (todo es fijo).

  Los segmentos 0 a 2 000 de cada uno (100 km) coinciden **bit a bit** con el oráculo; las listas visibles a 0, 1, 49,5, 50, 920, 1234, 5000 y 42 195 m también (a igual z, el orden no importa). Determinista, dentro del segmento, en la orilla (`|x| ≥ 9` salvo puente y boyas), de lejos a cerca y con z en `[d + 1; d + 400]` (LT-99 a LT-107). [dotnet] · `environment.dart:72-114`; ADR 012
- **AC-SCN-14-09 · Topes y prioridad de hitos.** *Entonces* el tope visible es **60** en la costa y **200** en la regata (120 en lago y río); al superarlo se aplica la regla de AC-SCN-14-05 (hitos primero, después las boyas más cercanas, después se descartan los hitos más lejanos). En la regata, desde 0 m se ve al menos una tribuna, torre o cartel con z > 150 (LT-109). Un escenario de prueba al tope da lo mismo que el oráculo. [dotnet] · `coast_environment.dart:25`; `regatta_environment.dart:20`
- **AC-SCN-14-10 · El mismo paisaje que el legacy, en pantalla (Q-61).** *Dado* el mismo tamaño y la misma distancia de escena, con la respiración en 0, *entonces* en río, costa y regata están los mismos objetos que en el legacy y **el eje de cada uno cae en la misma x de pantalla que en el legacy, ± 2 dp**, en V390, H844 y los cinco tamaños de la app; la base solo puede quedar más arriba por el terreno (≤ 2,5 m de mundo; en el río, el muro de 1,2 m). CAP-44 a CAP-46 lado a lado con LEG-01 a LEG-03, con una marca en la base de cada objeto del legacy. [dotnet] · [Editor-PM] · [Captura]

### 3.2 F-SCN-16 — Río urbano

- **AC-SCN-16-01 · Agua y edificios.** *Entonces* el río tiene amplitud **0,35** y crestas cada **2,0 m**, sin tinte. En cada orilla, edificios (70 %) y clubes de remo (30 %): el primero en `z₀ + u·8` y los siguientes cada `15 + u·15` m mientras `z < z₀ + 50`; `|x| = 13 + u·4` (13 a 17); escala `0,6 + u·0,4`; variante 0 a 999 (pisos `3 + variante mod 4`, tono `variante mod 3`). [dotnet] · `river_environment.dart:14-35`
- **AC-SCN-16-02 · Faroles y boyas.** *Entonces* hay faroles en `x = ±9,5` a `z₀ + 12,5` y `z₀ + 37,5` (cada 25 m: 12,5; 37,5; 62,5… m) y un par de boyas de carril **amarillas** (`#FDD835`, variante 1) en `x = ±3` a `z₀ + 25` (25, 75, 125… m). [dotnet] · `river_environment.dart:36-39`
- **AC-SCN-16-03 · Puente cada 800 m.** *Entonces* hay un puente de piedra en `x = 0` a `z₀ + 20` cuando `k mod 16 = 5`: a **270, 1 070, 1 870…** m; en los segmentos 0 a 31, exactamente el 5 y el 21 (LT-110). Cruza todo el canal (arco de 17 m de luz con la clave a ≈ 5 m, pilares de 1 × 1,5 m, tablero de 24 m entre 6 y 7,2 m, parapeto hasta 7,7 m) y la cámara y el bote pasan **por debajo del arco**: el puente no se trama ni desaparece mientras está a la vista (no le aplica el tramado de 2 a 1 m de DV-62) y sale por detrás de la cámara. Debajo, la luz baja un 40 % [EXR]. [dotnet] (los metros) · [Editor-PM] · [Captura] CAP-43, VID-02 · `river_environment.dart:41`; `shore_painter.dart:163-182`
- **AC-SCN-16-04 · Una orilla urbana de verdad [VISUAL con medidas].** *Entonces* muro de contención de piedra en `|x| = 9` (1,2 m sobre el agua), paseo de 9 a 12,5 m y fachadas con el eje en `|x|` 13 a 17 m (el del legacy; el frente del volumen queda desde ≈ 10 m y nunca más adentro de 10,1), con el catálogo de AC-SCN-20-03: edificio de 6 m de ancho y 3 m por piso con 3 ventanas por piso; club de 8 × 4 m con techo a 6 m, portón de 3 × 2,8 m, franja `#1565C0` y rampa al agua que corta el muro; farol de 4 m. Nada flota; sin costuras entre segmentos; todo lo agregado es determinista por distancia; ningún volumen se cruza con otro ni con el muro. ART_BIBLE §7.3. [dotnet] (sin cruces) · [Editor-PM] · [Captura] CAP-09 a CAP-16
  - *Motivo del cambio (gate QA-G8-01):* "fachadas desde 13 m" no era posible con el eje del legacy en 13–17 y 6·s u 8·s de ancho; las reglas de colocación están en [02-tech-design](02-tech-design.md) §2.4.1.

### 3.3 F-SCN-17 — Costa

- **AC-SCN-17-01 · Agua del mar.** *Entonces* la costa tiene amplitud **0,8** y crestas cada **4,0 m**, y sus dos colores de agua (waterNear y waterFar) se mezclan al **30 %** con `#0E6B7A`, también en el agua degradada (los tres tramos y la franja de arriba con fog). [dotnet] (los colores por hora) · `coast_environment.dart:16-21`; `water_painter.dart:80-81`
- **AC-SCN-17-02 · Acantilados y playas.** *Entonces* solo hay orilla izquierda (a la derecha, mar abierto sin objetos hasta el horizonte). Cuando `k mod 5 = 2` el segmento es **playa** (100–150, 350–400, 600–650… m): 3 sombrillas en `x = −13 + u·3` (−13 a −10), a `z₀ + 8 + i·14 + u·6` (i = 0, 1, 2), color `variante mod 3` (`#F06292`, `#4FC3F7`, `#FFD54F`). En el resto, 2 **acantilados** en `x = −14` a `z₀` y `z₀ + 25`, escala `0,8 + u·0,5`, forma del borde según su variante. [dotnet] · `coast_environment.dart:31-45`
- **AC-SCN-17-03 · El faro cada 1 000 m.** *Entonces* hay un faro en `x = −14` a `z₀ + 30` cuando `k mod 20 = 10`: a **530, 1 530, 2 530…** m, siempre sobre un tramo de acantilado. Torre `#ECEFF1` de 2,2 × 9 m con 3 franjas `#D32F2F`, linterna `#37474F` hasta 10,2 m. De noche, halo `#FFF59D @35 %` de radio 3 m y **haz giratorio** que da una vuelta cada 12 s [EXR, DV-83]. [dotnet] (los metros) · [Captura] CAP-20, CAP-24, VID-03 · `coast_environment.dart:46`; `shore_painter.dart:217-227`
- **AC-SCN-17-04 · Una costa de verdad [VISUAL con medidas].** *Entonces* acantilados de 8 m × escala con estratos, 7 m de fondo, pasto `#556B2F` arriba, rocas y espuma en la base (x ≈ −12,5); en las playas, arena `#D8C3A0` desde x = −9,5 (pendiente suave) hasta −20, con transición de 10 m entre tramos; sombrillas de 2,2 m con copa de 1,8 m y toalla `#FFF3E0`. Nada flota; determinista por distancia. ART_BIBLE §7.4. [Editor-PM] · [Captura] CAP-17 a CAP-24, CAP-42

### 3.4 F-SCN-12 y F-SCN-06 — Oleaje de la costa y rolido por escenario

- **AC-SCN-12-01 · El oleaje de la costa, en el agua degradada.** *Entonces* el agua de la costa ondula con la ola del legacy sin el ruido: `h = sin(zMundo/4,0 + 0,6t)·0,6 + sin((0,7·zMundo + 1,3x)/4,0 + 0,9t)·0,4`, que aclara y oscurece el color `× (1 + h·0,8·0,25)` (± 20 %): franjas de oleaje que avanzan con el tiempo de escena y pasan con la distancia (también en pausa). Lago, río y regata siguen con el agua degradada calma de la 6a (sus parámetros quedan para la 6b). Además, el contrato de entradas del agua del legacy existe con su test: en H844, velocidad 2, costa, 12 h → 28 valores, [0] 844, [1] 390, [2] horizonte, [7] **0,8**, [9] ancho/2, [26] 0,5, [27] 5,3 (LT-124). [dotnet] · [Captura] CAP-42, VID-05 · `water.frag:45-53, 67-68`; `water_painter.dart:29-60`
- **AC-SCN-06-04 · Rolido por escenario y mar de fondo (DV-85).** *Entonces* el rolido de AC-SCN-06-02 usa `k` = lago 0,5, río 0,8, **costa 2,0**, regata 0,4. En la costa, el casco toma la altura de la ola de AC-SCN-12-01 (`h·0,15` m, ± 0,12 m) en proa (13,5), centro (9,5) y popa (5,3): arfada = promedio y cabeceo extra `atan((h_proa − h_popa)/8,2)`, suavizados τ 0,3 s; la respiración de la cámara va × 1,8 y su tope pasa de ± 3 a **± 5,4 dp**. Nada de esto cambia la distancia ni los datos (AC-SCN-08-11). Con reducir movimiento, la cámara quieta (el bote sigue la ola). [dotnet] · [Captura] VID-03, VID-05 · ANIMATION_BIBLE §4.6, §5

### 3.5 F-SCN-18 — Canal de regata

- **AC-SCN-18-01 · Agua y boyas de carril.** *Entonces* la regata tiene amplitud **0,3** y crestas cada **1,5 m**, sin tinte. Boyas cada **10 m** (`z₀`, `z₀ + 10` … `z₀ + 40`) en **4 líneas** (`x = −9, −3, +3, +9`): 3 carriles, el bote en el del centro; 20 por segmento; el segmento 3 tiene 5 z distintas separadas 10 m (LT-108). Color por tramo de 250 m, `(k div 5) mod 3`: **rojo** `#E53935` de 0 a 249 m, **amarillo** `#FDD835` de 250 a 499, **blanco** `#ECEFF1` de 500 a 749, rojo de nuevo de 750 a 999… Esfera de 0,25 m que cabecea `0,08·sin(1,7t + variante)` rad (DV-88). [dotnet] · [Captura] CAP-25 a CAP-32 · `regatta_environment.dart:14-32`; `shore_painter.dart:184-189`
- **AC-SCN-18-02 · Carteles cada 250 m (Q-64).** *Entonces* hay un cartel en `x = −10,5` a `z₀` cuando `k mod 5 = 0`, con el texto `<metros> m` (metros = 50k: `250 m`, `500 m`, `750 m`, `1000 m`…, sin separador de miles, como el legacy), en negro al 87 % y negrita sobre tablero `#FAFAFA` de 1,8 × 1,0 m (de 1,5 a 2,5 m de alto) con poste `#9E9E9E`; cifras de 0,55 m centradas a 2,0 m. El número es la **distancia de escena** (F-SCN-03), no la del HUD: el cartel `250 m` pasa la cámara cuando la escena lleva 250 m, aunque la banda diga otra cosa. El de `0 m` existe y nunca se ve. El número está siempre en el tablero (DV-82) y se lee sin ambigüedad a 30 m en TV y en TH, de día y de noche (CAP-40, CAP-41). [dotnet] · [Captura] · `regatta_environment.dart:33-35`; `shore_painter.dart:266-285`
- **AC-SCN-18-03 · Tribunas, torre y banderas.** *Entonces* tribuna en `x = +12` a `z₀ + 10` cuando `k mod 20 = 4` (**210, 1 210…** m); torre de llegada en `x = −12` a `z₀ + 10` cuando `k mod 20 = 12` (**610, 1 610…** m), con reloj real; en cada segmento una bandera a `z₀ + 30`, a la derecha (`x = +10,5`) si k es par y a la izquierda (`−10,5`) si es impar, color `k mod 4`: rojo `#E53935`, ámbar `#FFB300`, azul `#1E88E5`, verde `#43A047` (30 m derecha roja, 80 m izquierda ámbar, 130 m derecha azul, 180 m izquierda verde…). [dotnet] · `regatta_environment.dart:36-38`
- **AC-SCN-18-04 · Una pista de verdad [VISUAL con medidas].** *Entonces* taludes de pasto de 25° que suben 1,0 m desde `|x| = 9,8`; tribuna de 4 gradas de 12 × 1,2 m (`#8D9DAE` / `#6E7B8C`) con techo `#37474F` a 5,3 m y **público** estático con movimiento sutil [EXR]; torre de 3 × 10 m `#CFD8DC` con banda `#263238`, reloj de radio 0,6 m y antena de 1,5 m; banderas de 5 m con paño de 1,6 m que flamea; álamos en `|x|` 30–40 m. ART_BIBLE §7.5. [Editor-PM] · [Captura] CAP-25 a CAP-32

### 3.6 F-SCN-19 — Siluetas lejanas

Común: color `mezcla(tono; fog; 0,35 + 0,4·profundidad)` con la paleta de la hora; alto máximo del legacy con el tope de 9° sobre el horizonte de DV-65 (DV-87); se acercan al 4 % de la velocidad del bote y **no se deslizan de costado** (Q-65, DV-81); quietas con el bote quieto.

- **AC-SCN-19-03 · Río: skyline.** *Entonces* bloques de ciudad de alto 30 a 100 % del 12 % del alto de pantalla y ancho 60 a 100 % de su paso, tono `#3A4660` con profundidad 0,8, cada bloque con su forma fija; con ambiente < 0,6, ventanas `#FFE08A @85 %` encendidas al 50 %, fijas por bloque (no titilan). [dotnet] (color, umbral) · [Captura] CAP-12, CAP-16 · `river_environment.dart:46-71`
- **AC-SCN-19-04 · Costa: acantilado lejano y gaviotas.** *Entonces* un acantilado lejano en el 35 % izquierdo del ancho, de alto máximo 8 % de la pantalla, que se desvanece hacia el centro, tono `#4A3F3A` con profundidad 0,9; a la derecha, solo mar. **5 gaviotas** `#2B2B2B` con opacidad `0,5 + 0,4·ambiente` cruzan el cielo de izquierda a derecha a velocidades proporcionales a 8, 11, 14, 17 y 20 px/s del legacy, a 35, 45, 55, 65 y 75 % de la altura del horizonte, de 5 a 9 px de envergadura en V390, y reaparecen por la izquierda; planean con aleteos (DV-89). [dotnet] · [Captura] VID-03 · `coast_environment.dart:51-88`
- **AC-SCN-19-05 · Regata: tribunas lejanas.** *Entonces* franja de tierra `#55704A` (profundidad 0,9) de 1,5 % del alto; repetidas a lo largo del horizonte, tribunas `#6E7B8C` y torres `#B8C0CC` (profundidad 0,8) y grupos de 4 banderas rojo, ámbar, azul y verde (profundidad 0,5) que flamean. [dotnet] (colores) · [Captura] CAP-25 a CAP-32 · `regatta_environment.dart:43-71`

### 3.7 F-SCN-20 — Objetos de orilla

- **AC-SCN-20-03 · El catálogo completo [VISUAL con medidas].** *Entonces* los 12 tipos que faltan (edificio, club, puente, boya, farol, acantilado, faro, sombrilla, tribuna, torre, bandera, cartel) existen en 3D con las medidas de referencia (± 10 %) y los colores del catálogo del legacy ([immersive-scene F-SCN-20](../../product/inventory/immersive-scene.md)) como tonos dominantes antes de la luz de la hora, incluidas las variantes: edificio pisos y tono por variante, sombrilla y boya `mod 3`, bandera `mod 4`. Todos reciben la luz y la niebla de AC-SCN-20-01. [dotnet] (paletas por variante) · [Editor-PM] (medidas) · `shore_painter.dart:54-270`
- **AC-SCN-20-04 · Las luces de la noche.** *Entonces* con ambiente < 0,6 (≈ 19:26 a 4:43) se encienden, sin tinte ni niebla y con el encendido escalonado de DV-60: las ventanas de cada edificio (`#FFE082`; las mismas ventanas que el legacy: `Random(variante)`, una tirada por ventana, piso por piso, encendida si `< 0,6`), los faroles (lámpara `#FFF8E1` y halo `#FFE082 @35 %` de 1,5 m; de día lámpara `#90A4AE`), el faro (AC-SCN-17-03) y las ventanas del skyline (AC-SCN-19-03). [dotnet] · [Captura] CAP-12, CAP-16, CAP-24 · `shore_painter.dart:26, 132-147, 191-199`
- **AC-SCN-20-05 · Se dibuja sin errores (LT-111 a LT-123).** *Entonces* río, costa y regata, en V390 y H844, a las 12 y a las 23 h, a 0 y a 1 234 m con tiempo 3,2 s, se dibujan sin excepciones ni avisos; y un puente a relZ 0,3 (no se dibuja), uno a relZ 1,0 (encima de la cámara), el cartel `750 m` a 30 m y los 15 tipos juntos en `x = 12` a 20 m, variante 3, a las 22 h, también. [Editor-PM] · `sky_shore_painter_test.dart:16, 57`
- **AC-SCN-20-06 · Mundo vivo.** *Entonces* con el tiempo de escena (también en pausa): las banderas de orilla flamean con la punta en `sin(4t + variante)·0,2` m (tela con onda), las del horizonte con `sin(3t + i)`, las boyas cabecean (AC-SCN-18-01), el público se mueve apenas, el haz del faro gira y las gaviotas vuelan. Con reducir movimiento todo eso sigue (es el mundo, no la cámara). [Captura] VID-01 a VID-04 · ANIMATION_BIBLE §6
- **AC-SCN-20-07 · Arte gratis, detrás de slots, sin marcas.** *Entonces* todo el arte nuevo es procedural o propio, o entra por un **slot** (`EnvironmentDefinition` de cada escenario y un `PropSlot` por tipo); sin nada descargado, los 4 escenarios se ven completos y dignos; un archivo asignado que falta o no valida vuelve al procedural con **un** aviso `[SLOT]`; nada de terceros se versiona (`Assets/ThirdParty/` ignorado) y el CI compila y pasa sin él. Ningún nombre, logo, cartel ni silueta reconocible de un lugar o marca real (inspiración Bled, Henley/Charles, Bosbaan, sin copiarlos). [Editor-PM] · [QA] (`git ls-files`, CI) · ASSET_SLOTS §0

### 3.8 F-SCN-21 — Preferencia de escenario

- **AC-SCN-21-02 · Clave, valores y por defecto.** *Entonces* la preferencia vive en el archivo de preferencias de la app con la clave **`scene.environment`** y el valor `lake`, `river`, `coast` o `regatta` (en ese orden del selector). Sin la clave o con un valor desconocido (`"marte"`), el escenario es el **lago** y el archivo no se toca al arrancar. No se importa nada del legacy (Q-11). [dotnet] · [Editor-PM] · `scene_settings.dart:11-13, 26-33`
- **AC-SCN-21-03 · Elegir aplica y guarda.** *Cuando* se toca otra tarjeta, *entonces* la selección cambia en el acto y después se guarda `scene.environment` (LT-125: sin preferencias arranca en lago y se ve `Lago alpino`; al tocar `Río urbano` queda río y se guarda `"river"`). Tocar la ya elegida no cambia nada ni escribe. Si la escritura falla, la elección vale para esta sesión, sin mensaje, con un aviso en el registro. [dotnet] · [Editor-PM] · `scene_settings.dart:36-41`
- **AC-SCN-21-04 · Sobrevive al reinicio.** *Dado* `scene.environment = "coast"`, *cuando* arranca la app, *entonces* el escenario es costa (LT-126: a los 50 ms) y Workout la muestra elegida **desde el primer cuadro en que se ve**, sin mostrar antes el lago (mejora EXR). Si la lectura todavía no terminó cuando el usuario toca una tarjeta, gana el toque (también si toca el lago) y lo leído se ignora. Cerrar con cada uno de los 4 elegido y volver a abrir lo conserva (en el build, 4 de 4). [dotnet] · [Editor-PM] · [Device-Win] · `scene_settings.dart:15, 26-37`
- **AC-SCN-21-05 · El entrenamiento usa el escenario elegido.** *Entonces* el inmersivo (libre y rutinas) usa el escenario elegido al entrar; durante el entrenamiento no se puede cambiar (el selector solo está en reposo). La sesión guardada no registra el escenario (como el legacy). La hora forzada del simulador sigue sin guardarse (AC-SCN-23-03). [Editor-PM] · `immersive_workout_screen.dart:81-86`

### 3.9 F-SCN-22 — Selector con miniaturas

- **AC-SCN-22-01 · Dónde está.** *Entonces* en Workout en reposo, de arriba abajo: el aviso sin remo (si aparece), el título `workoutScene` (`Escenario` / `Scenery`) en blanco al 54 % y 13, 8 de separación, el selector, 16 y `Entrenamiento libre`. Está **habilitado aunque el remo no esté conectado**. Se desplaza con el resto de Workout (AC-WKT-12-09). [Editor-PM] · [Captura] CAP-37 a CAP-39 · `workout_screen.dart:79-83`
- **AC-SCN-22-02 · La fila de tarjetas (DV-79).** *Entonces* una fila horizontal de **4 tarjetas** en el orden `sceneLake`, `sceneRiver`, `sceneCoast`, `sceneRegatta` (`Lago alpino`, `Río urbano`, `Costa`, `Canal de regata` / `Alpine lake`, `City river`, `Coast`, `Regatta course`), separadas 10; miniatura de **152 × 96** con radio 12 y, 6 más abajo, el nombre en 13. Elegida: borde `#00B4D8` de 2,5 y nombre blanco en 700. No elegida: borde blanco al 12 % de 1 y nombre blanco al 70 % en 400. Si no entran (TV, P360, L640), la fila se desplaza de costado con el dedo, la rueda y el teclado, sin mover la página; en W1280 entran las 4. Ningún texto se corta con letra 1,3×. No hay claves nuevas del catálogo. [Editor-PM] · [Captura] · `environment_picker.dart:23-58`
- **AC-SCN-22-03 · Tocar y teclado.** *Entonces* tocar la miniatura o el nombre elige ese escenario (AC-SCN-21-03). Con teclado, Tab entra a la fila, las flechas ← → mueven el foco entre tarjetas (la fila se corre para mostrarla), Enter o Espacio la eligen, con el anillo de foco del sistema visual (F-APP-05). El cambio se anima: borde y ancho con `spring.snappy`, miniatura 1 → 1,03 en 240 ms, fundido del peso del nombre; con reducir movimiento, en el acto. [Editor-PM] · [Captura] CAP-39 · ANIMATION_BIBLE §2 (tabla de componentes)
- **AC-SCN-22-04 · La miniatura (QA-P-11).** *Entonces* cada miniatura es la escena real de su escenario a las **12:00**, con la cámara a **20 m** de distancia de escena, **sin bote ni remero ni HUD**, con el agua degradada, sin animación (tiempo 0) y solo con los objetos de orilla a menos de 150 m (`z < 170`); cielo, terreno y silueta lejana sí. Es una imagen preparada de antemano (no carga ni dibuja escenas en Workout) y no cambia con la hora real ni con `Hora escena`. [Editor-PM] · [Captura] CAP-33 a CAP-36 · `thumbnail_painter.dart:10-43`; ART_BIBLE §7.6
- **AC-SCN-22-05 · Workout sigue liviana.** *Entonces* con el selector, Workout en reposo no dibuja la escena 3D (0 dibujos de escena, AC-SCN-01-06), no reserva memoria por cuadro y el desplazamiento de la fila va a 60 fps en W1280; las 4 miniaturas suman ≤ 1 MB en el build. [Editor-PM] · [Device-Win] (`[PERF]`)

### 3.10 F-SCN-01 — Cuatro escenarios en la misma app

- **AC-SCN-01-17 · Cada escenario anima y se libera sin errores (LT-127 a LT-138).** *Entonces* río, costa y regata animan 6 cuadros de 100 ms con SPM 24, split 110 s, 300 m en el monitor, en marcha y con la hora forzada, en V390 y H844, a las 12 y a las 23 h, sin excepciones, y se cierran sin excepciones ni avisos. [Editor-PM] · `scene_view_test.dart:11`
- **AC-SCN-01-18 · Cambiar de escenario no rompe nada.** *Entonces* elegir otro escenario en reposo y entrar enseguida al inmersivo no tironea: si el escenario nuevo no está listo, aplica la escena demorada (AC-SCN-01-07). Alternar 10 veces entre los 4 escenarios entrando y saliendo del inmersivo no deja avisos de objetos destruidos y la memoria total vuelve a ± 5 % de la del primer recorrido. [Editor-PM] · [Device-Win]
- **AC-SCN-01-19 · Rendimiento en los 4 escenarios (`[PERF]`).** *Dado* el build de simulador en la PC (W1280), *entonces* en cada escenario, libre con `Fuerte`, a las 12 y a las 22 h, y en la regata en el tramo más cargado (`E-180`, con público): p99 del cuadro ≤ **16,7 ms**, ningún cuadro > **33 ms**, **0 bytes** por cuadro en régimen (helper `FrameAllocations` con su control positivo y Profiler del build de desarrollo), topes de ARCHITECTURE §20.1 por nivel en el Editor, y el HUD en ≤ 57 de 60 elementos. Android de gama media, 60 fps en el escenario más cargado: **NO VERIFICABLE** sin el teléfono (regla 9). [Device-Win] · [Editor-PM]

---

## 4. Criterios anteriores que cambian o se vuelven a verificar

| ID | Qué pasa |
|---|---|
| **AC-WKT-12-02** (cambia) | El orden pasa a ser: aviso sin remo, `Escenario`, 8, selector, 16, `Entrenamiento libre`, 24, lista de rutinas (AC-SCN-22-01). Se cierra DV-34 en esta parte; la prueba de la 4a que exige que no se vea `Escenario` se invierte |
| **AC-SCN-21-01** (cambia) | Deja de ser "siempre el lago": la reemplazan AC-SCN-21-02 a -05 |
| **AC-SCN-14-05** (cambia) | Además del escenario de prueba, se verifica con los topes reales de costa (60) y regata (200) y LT-109 |
| Se vuelven a verificar en los 4 escenarios | AC-SCN-14-04, -06, -07; AC-SCN-15-05 (lago, sin cambios); AC-SCN-20-01, -02; AC-SCN-01-05, -06, -07, -12, -15 (`E-42K` contra el oráculo); AC-SCN-02-03; AC-SCN-08-06 (sombra), AC-SCN-08-10 (la boya naranja de 500 m también en río y regata, entre las boyas de carril); AC-WKT-13-15; AC-WKT-17-09, -13 (HUD legible sobre la nieve, la arena, el muro, el cielo del mar); AC-WKT-18-14. LT-62 a LT-98 siguen pasando. Si alguno falla, se reabre acá |

---

## 5. Casos de test legacy (cada uno → AC)

| LT | Test legacy | Qué afirma | AC |
|---|---|---|---|
| LT-99, LT-100, LT-101 | `environment_test.dart:10, 17, 30` (`river`) | determinista (7 ≠ 8); segmentos 0–29 no vacíos, dentro, `\|x\| ≥ 9`; a 1234 ≤ 120, z decreciente en [1235; 1634] | AC-SCN-14-08 |
| LT-102, LT-103, LT-104 | ídem (`coast`) | ídem con tope 60 | AC-SCN-14-08, -09 |
| LT-105, LT-106, LT-107 | ídem (`regatta`) | ídem con tope 200 | AC-SCN-14-08, -09 |
| LT-108 | `environment_test.dart:42` | regata: segmento 3 con 5 z de boyas separadas 10 m | AC-SCN-18-01 |
| LT-109 | `:54` | regata desde 0: tribuna, torre o cartel con z > 150 | AC-SCN-14-09 |
| LT-110 | `:60` | río: puente en los segmentos [5, 21] de 0–31 | AC-SCN-16-03 |
| LT-111 a LT-114 | `sky_shore_painter_test.dart:16` (`river` × V390/H844 × 12/23 h) | cielo y orilla a 0 y 1234 m sin excepciones | AC-SCN-20-05 |
| LT-115 a LT-118 | ídem (`coast`) | ídem | AC-SCN-20-05 |
| LT-119 a LT-122 | ídem (`regatta`) | ídem | AC-SCN-20-05 |
| LT-123 | `:57` | puente a relZ 0,3 y 1,0, cartel 750 m, 15 tipos a las 22 h | AC-SCN-20-05, AC-SCN-16-03 |
| LT-124 | `water_painter_test.dart:27` | 28 valores; [7] = 0,8 en la costa; [9] = ancho/2; [26] = 0,5; [27] = 5,3 | AC-SCN-12-01, AC-SCN-17-01 |
| LT-125 | `environment_picker_test.dart:11` | lago por defecto, se ve `Lago alpino`; tocar `Río urbano` → río y `"river"` guardado | AC-SCN-21-03, AC-SCN-22-02 |
| LT-126 | `:38` | con `"coast"` guardado, a los 50 ms es costa | AC-SCN-21-04 |
| LT-127 a LT-130 | `scene_view_test.dart:11` (`river` × V390/H844 × 12/23 h) | 6 cuadros de 100 ms y desmontar sin excepciones | AC-SCN-01-17 |
| LT-131 a LT-134 | ídem (`coast`) | ídem | AC-SCN-01-17 |
| LT-135 a LT-138 | ídem (`regatta`) | ídem | AC-SCN-01-17 |

Todos se portan **sin cambiar números**. Con esto quedan portados todos los tests de `test/scene/` del legacy.

---

## 6. Capturas y videos

**Reglas:** las de la 6a §7.1 y la 7 §6 (español, simulador sin ruido, reloj y fecha de prueba, carpeta de datos temporal, `R-4`, dos corridas iguales byte a byte, `manifest.json` e `INDEX.md` sin rutas de la PC), en `docs/migration/iterations/08-entornos-selector/snapshots/`. Ninguna toma toca `prefs.json` ni las bases del usuario ni deja cambiados los ProjectSettings. Personaje: el maniquí. El `manifest.json` lleva **sondas** de color de las luces nocturnas (ventana y frente de la cabaña, ventanas de edificio, farol, faro, ventanas del skyline): cierra el issue #9 (QA-V6a-08). "Matriz" = libre con `R-4`, HUD visible, tiempo de escena 3,2 s, fase c = 0,18, hora fijada con el control de prueba y la distancia del escenario: lago `E-920`, río `E-250`, costa `E-500`, regata `E-180`.

| ID | Qué | Qué tiene que verse |
|---|---|---|
| **CAP-01 a CAP-08** | Matriz, **lago**: 6, 12, 18 y 22 h en TV (01–04) y en TH (05–08) | El lago de la 6a con el remero, igual que antes |
| **CAP-09 a CAP-16** | Matriz, **río**: ídem (09–12 TV, 13–16 TH) | Muro, paseo, fachadas, faroles, boyas amarillas, el puente a 20 m; a las 22 h ventanas, faroles y skyline encendidos |
| **CAP-17 a CAP-24** | Matriz, **costa**: ídem (17–20 TV, 21–24 TH) | Agua teñida con oleaje, acantilados, faro a 30 m, playa adelante, mar abierto a la derecha; a las 22 h el halo y el haz del faro |
| **CAP-25 a CAP-32** | Matriz, **regata**: ídem (25–28 TV, 29–32 TH) | 3 carriles, boyas rojas y amarillas desde 250 m, tribuna con público, cartel `250 m`, banderas |
| **CAP-33 a CAP-36** | Las 4 miniaturas, 1:1 | Lago, río, costa, regata al mediodía, a 20 m, sin bote |
| **CAP-37** | Workout en reposo, TV, remo desconectado, lago elegido | Aviso, `Escenario`, la fila (las 2 primeras enteras y la 3.ª asomando), `Entrenamiento libre` deshabilitado |
| **CAP-38** | Workout en reposo, W1280, remo conectado, río elegido | Las 4 tarjetas a la vista, río con borde celeste |
| **CAP-39** | Workout en reposo, TV, inglés, costa elegida, foco de teclado en `Regatta course` | `Scenery`, los nombres en inglés, anillo de foco |
| **CAP-40** | Regata, escena sola, TV, 12 h, `E-220` | Cartel `250 m` a 30 m, legible |
| **CAP-41** | Regata, escena sola, TH, 22 h, `E-720` | Cartel `750 m` a 30 m, legible de noche |
| **CAP-42** | Costa, escena sola, TH, 12 h, `E-500` | Las franjas del oleaje; el bote inclinado por la ola |
| **CAP-43** | Río, escena sola, TV, 12 h, `E-266` | El arco del puente encima, la luz más baja debajo |
| **CAP-44 a CAP-46** | Río `E-250`, costa `E-500`, regata `E-180`; escena sola, V390, 12 h, respiración en 0, con marcas | Cada objeto sobre la marca de **LEG-01 a LEG-03** (el legacy dibujado por el oráculo a la misma distancia) |

| ID | Tamaño y duración | Qué se hace | Qué tiene que verse |
|---|---|---|---|
| **VID-01** | TV, 20 s | Lago, `R-4` en régimen desde `E-900` | Remar en el lago (referencia) |
| **VID-02** | TH, 20 s | Río, en régimen desde `E-200` | Edificios y faroles que pasan; a los ≈ 17,5 s la cámara pasa bajo el puente sin cortes |
| **VID-03** | TV, 20 s | Costa, en régimen desde `E-470` | Oleaje, bote que sube y rola; el faro pasa a los ≈ 15 s; gaviotas |
| **VID-04** | TH, 20 s | Regata, en régimen desde `E-190` | Tribuna a los ≈ 5 s; cartel `250 m` y boyas que pasan de rojo a amarillo a los ≈ 15 s; banderas y público |
| **VID-05** | TH, 10 s | Costa, escena sola, bote quieto y en pausa | El mar se mueve y el bote sube y baja con la ola |
| **VID-06** | W1280, ≈ 30 s | Elegir `Costa` → cerrar la app → abrirla → `Entrenamiento libre` | `Costa` elegida desde el primer cuadro y el entrenamiento en la costa |

**Demo** (`DEMO-01` a `DEMO-05`, W1280): los pasos 1 a 5 de §1, una toma por paso.

---

## 7. Qué se verifica dónde

| Dónde | Qué |
|---|---|
| [dotnet] | LT-99 a LT-110, LT-124 a LT-126; generadores bit a bit (0–2 000) y listas contra el oráculo; metros de cada hito; colores por variante y por hora; tinte de la costa; ola y mar de fondo; rolido; siluetas (colores, umbral); luces nocturnas; preferencia (lectura, escritura, toque antes de la lectura, valor desconocido) |
| [Editor-PM] | LT-111 a LT-123, LT-127 a LT-138; x de pantalla contra el legacy; medidas; nada flota; puente encima; selector (orden, estados, toque, teclado, desplazamiento, letra 1,3×); reinicio con cada escenario; slots; alternar escenarios; topes por nivel |
| [Captura] | §6 |
| [Device-Win] | `[PERF]` de AC-SCN-01-19 y AC-SCN-22-05; reinicio en el build; demo en `Builds/Windows/simulator`. Si el `RowMate.exe` del usuario está abierto, se reintenta cada 2 min hasta 20; si no, NO VERIFICABLE |
| [Device-And] | 60 fps en el escenario más cargado: NO VERIFICABLE sin teléfono |
| [QA] | AC-SCN-20-07 (repositorio y CI sin terceros) |

---

## 8. Checklist de paridad

| # | Comportamiento legacy | Fuente | Test | AC |
|---|---|---|---|---|
| 1 | Semilla por escenario y segmento; generación por orden | `environment.dart:72-82` | `:10` | AC-SCN-14-08 |
| 2 | Visibles `[d + 1; d + 400]`, de lejos a cerca, topes 120/60/200 con prioridad de hitos | `:89-114`; entornos | `:30`, `:54` | AC-SCN-14-08, -09 |
| 3 | Río: edificios/clubes 30 %, x 13–17, escala 0,6–1,0, paso 15–30 | `river_environment.dart:22-35` | `:17` | AC-SCN-16-01 |
| 4 | Río: faroles ±9,5 cada 25 m; boyas amarillas ±3 cada 50 m | `:36-39` | — | AC-SCN-16-02 |
| 5 | Río: puente k mod 16 = 5 (270, 1 070 m) | `:41` | `:60` | AC-SCN-16-03 |
| 6 | Costa: 0,8 / 4,0 y tinte `#0E6B7A` 30 % | `coast_environment.dart:16-21` | `water_painter:27` | AC-SCN-17-01, AC-SCN-12-01 |
| 7 | Costa: playa k mod 5 = 2 con 3 sombrillas; si no, 2 acantilados | `:31-45` | `:17` | AC-SCN-17-02 |
| 8 | Costa: faro k mod 20 = 10 (530 m) | `:46` | — | AC-SCN-17-03 |
| 9 | Regata: boyas cada 10 m en 4 líneas; color cada 250 m | `regatta_environment.dart:26-32` | `:42` | AC-SCN-18-01 |
| 10 | Regata: cartel `<m> m` cada 250 m con distancia de escena | `:33-35` | `sky_shore:57` | AC-SCN-18-02 |
| 11 | Regata: tribuna 210, torre 610, banderas alternadas `k mod 4` | `:36-38` | `:54` | AC-SCN-18-03 |
| 12 | Siluetas de río, costa y regata; color por profundidad | entornos `paintHorizon`; `environment.dart:127-129` | `sky_shore:16` | AC-SCN-19-03 a -05 |
| 13 | Catálogo de 15 tipos, luz y niebla, noche < 0,6 | `shore_painter.dart` | `:16`, `:57` | AC-SCN-20-03, -04, -05 |
| 14 | Banderas flamean con el tiempo, también en pausa | `shore_painter.dart:255` | — | AC-SCN-20-06 |
| 15 | `scene.environment`, por defecto lake, desconocido → lake, toque gana | `scene_settings.dart` | picker `:11`, `:38` | AC-SCN-21-02 a -04 |
| 16 | Selector en reposo arriba de `Entrenamiento libre`, 4 tarjetas, estados | `workout_screen.dart:79-83`; `environment_picker.dart` | picker `:11` | AC-SCN-22-01 a -03 |
| 17 | Miniatura 12:00, 20 m, sin bote, objetos < 150 m | `thumbnail_painter.dart` | — | AC-SCN-22-04 |
| 18 | Cada escenario anima y se desmonta sin errores | `scene_view.dart` | `scene_view:11` | AC-SCN-01-17 |

---

## 9. Mejoras EXR (cómo se tiene que sentir)

Cuatro lugares distintos y creíbles. El río se siente urbano y encajonado: muros de piedra, el eco visual de pasar bajo un puente, la ciudad que se enciende de noche. La costa es abierta y viva: el mar mueve el bote, la espuma rompe en las rocas, las gaviotas planean y el faro barre el agua de noche. La regata es ordenada y competitiva: carriles perfectos, agua planchada, carteles grandes que marcan el avance, público y banderas. Elegir escenario es inmediato y agradable: tarjetas con una foto real del lugar que responden al toque, y la app recuerda la elección.

---

## 10. Decisiones aplicadas y diferencias visibles

Aplicadas (ROADMAP §6): **Q-61** (paisaje idéntico, vectores dorados), **Q-64** (carteles con la distancia de escena), **Q-65** (horizonte que se acerca), **QA-P-11** (miniaturas a las 12:00), **Q-11** (no se importa la preferencia del legacy), **D-ART-08** sigue en No (sin texturas CC0 hasta que el usuario diga que sí).

| DV | Legacy | App | Si el usuario lo rechaza |
|---|---|---|---|
| DV-79 | Tarjetas 120 × 80, nombre 12 | 152 × 96, nombre 13 (ART_BIBLE §3) | 120 × 80 y 12 |
| DV-80 | Miniatura dibujada en vivo (2D) | Render horneado de la escena 3D a las 12:00 | — (el agregado "hora actual", ver §12) |
| DV-81 | Skyline y tribunas lejanas se deslizan a la izquierda (0,24 y 0,36 dp/m); el acantilado lejano cambia de forma con la distancia | Se acercan al 4 % sin deslizarse ni cambiar de forma (Q-65) | Deslizamiento del legacy |
| DV-82 | El número del cartel no se dibuja bajo 4 px (≈ 54 m en V390) | Siempre en el tablero; se pierde con la distancia y la niebla | Aparece a los ≈ 54 m |
| DV-83 | Faro con halo fijo | Halo + haz giratorio de 12 s | Solo el halo |
| DV-84 | Solo los objetos del catálogo | Muro y paseo (río), espuma y arena (costa), taludes, álamos y público (regata); en `\|x\| < 18 m` solo objetos del legacy, suelo y vegetación baja (P-09 de la 6a) | Sin público ni álamos |
| DV-85 | Sin rolido ni mar de fondo | Rolido por escenario; la costa mueve el bote; cámara × 1,8 con tope ± 5,4 dp en la costa | Rolido del lago en todos y sin mar de fondo |
| DV-86 | Agua sin shader: sin oleaje en ningún escenario | La costa muestra las franjas de la ola del legacy (sin ruido) en el agua degradada | Costa calma hasta la 6b |
| DV-87 | Siluetas al 12 / 8 / 6 % del alto | Con el tope de 9° de DV-65 (en vertical, el skyline y el acantilado más bajos) | Alto del legacy |
| DV-88 | Boyas quietas | Cabecean `0,08·sin(1,7t + variante)` rad | Quietas |
| DV-89 | 5 gaviotas en "M" de 1,5 px | 5 gaviotas 3D que planean y aletean, en las mismas alturas, sentido y velocidades | "M" planas |

---

## 11. Restricciones de producto para el Architect

1. Los generadores y los LT se portan sin cambiar un número; lo visual se apoya encima.
2. Un escenario por `EnvironmentDefinition`, sin escenas aditivas para el selector; miniaturas horneadas.
3. Arte procedural o propio, detrás de slots con fallback que funciona sin nada descargado; `Assets/ThirdParty/` no se versiona; el CI compila sin él.
4. 0 bytes por cuadro en régimen; el HUD no suma elementos (≤ 57 de 60).
5. La escena no escribe en la sesión, el motor ni la telemetría; el escenario no se guarda en la sesión.
6. Pruebas y capturas con `ROWMATE_DATA_DIR` temporal; nunca `prefs.json` ni las bases del usuario; ProjectSettings sin cambios; textos en UTF-8.
7. Builds solo a `Builds/Windows/simulator`; nunca cerrar el `RowMate.exe` del usuario.
8. Repo público: ninguna ruta de la PC en lo que se versiona.

---

## 12. Fuera de alcance y decisiones del PO

**Fuera de alcance:** agua avanzada, reflejos de orilla, luces y bote, estela, columnas de luz de los faroles en el agua, vidrio con desenfoque (6b, issue #6); cambiar de escenario durante el entrenamiento (tampoco en el legacy); botes rivales o fantasma, línea de llegada, sonido, clima y cámaras extra (Q-66, backlog); miniaturas animadas; 60 fps en Android (It. 14); la deuda de los issues #1 a #20 salvo el #9 (que entra por las sondas, §6).

**Decisiones del PO al especificar (sin preguntas abiertas):**

- Miniaturas a las **12:00** (paridad, QA-P-11). En el gate se ofrece el agregado: hornear 6, 12, 18 y 22 h y mostrar la más cercana a la hora de escena, con fundido de 240 ms.
- El oleaje visible es solo el de la costa (ROADMAP: F-SCN-12 oleaje de la costa); la altura física de la ola es `h·0,15` m (± 0,12 m), valor por defecto razonable para que se sienta sin marear.
- El puente no se trama al pasar por encima de la cámara (el legacy lo dibuja gigante hasta relZ 0,5).
- Si la escritura de la preferencia falla, la elección vale para la sesión y no se muestra error (el legacy no muestra nada).
- La boya naranja de 500 m de la 7 sigue igual en los 4 escenarios (x = +4,5 m).
- El cartel muestra `1000 m`, `1250 m`… sin separador de miles, como el legacy.

---

## 13. Discrepancias encontradas

- Ninguna entre el inventario (F-SCN-12, -14, -16 a -22) y el código del legacy en los números usados acá.
- `ART_BIBLE §7.6` pedía 304 × 192 px para la miniatura horneada: es el ×2 de la tarjeta de 152 × 96 (DV-79), coherente.
- `environment_test:17` exime a las boyas de `|x| ≥ 9`, pero las de la regata en ±9 también cumplen; las del río (±3) son las que lo necesitan.

---

## 14. Para issues

- Miniaturas a la hora actual (4 horneadas por escenario), si el usuario no lo pide en el gate.
- Columnas de luz de faroles y ventanas sobre el agua, y reflejo del puente (6b).
- Racks con botes de colores frente a los clubes del río (ART_BIBLE §7.3, opcional).
- Mar de fondo con espuma en las crestas y boyas que siguen la ola en la costa cuando llegue el agua avanzada (6b).
- Descripción breve de cada lugar debajo del nombre en el selector (mejora opcional del inventario).
- Rolido y respiración de la cámara por escenario configurables desde un perfil de datos (afinado del Designer).
- 60 fps en el escenario más cargado en el Android de gama media (It. 14).
