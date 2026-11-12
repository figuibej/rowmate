# It. 6b — Notas de diseño (WP-D: shaders del agua, estela, popa, vidrio y ronda de ajuste)

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-11 |
| Entrada | [02-tech-design](02-tech-design.md) (§4, §7 WP-D, §15 y §16: los *shaders* provisorios de WP-B), [01-po-spec](01-po-spec.md), [00-arte-agua](00-arte-agua.md) |
| Legacy | `shaders/water.frag` de [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura) |
| Capturas | Set `it6b` regenerado dos veces en [snapshots/](snapshots/) (§6); el detalle por toma, en [snapshots/manifest.json](snapshots/manifest.json) |

Modo rápido: ante la duda, el legacy o un valor razonable, anotado en una línea (DD6b-nn).

---

## 1. Qué entregó WP-D

| Archivo | Qué |
|---|---|
| `Art/Shaders/Scene/WaterAdvanced.shader` | Reescrito sobre el provisorio de WP-B: absorción, turbidez del río, parches de calma, sombreado de facetas, banda de orilla reflejada, astro sobre la normal perturbada en Alto y Medio, costura del horizonte, línea de contacto con el huso del casco y B3 girado 37° (§2) |
| `Art/Shaders/Include/RowMateWater.hlsl` | Sin cambios sobre el de WP-B (la ola, `RmSwellHeight`, el tope `n ≤ 9` de #2 y el peso del legacy); queda como la versión de WP-D |
| `Art/Shaders/Scene/WaterMarks.shader` | La estela (tipo 5): apagado del legacy hacia la cámara, cabeza de 0,5 s, espuma cortada y batida en la z de mundo, centro × 0,6 y luz de la hora (§3). Solo el vértice: el fragmento no cambia (13 + 28 ALU) |
| `Art/Shaders/Scene/Hull.shader` | #85: el Fresnel y el especular del gelcoat en la cubierta recortados a 0,15 (§4) |
| `Art/Shaders/UI/HudBlur.shader` | El tramado pasa al Bayer 4 × 4 de la escena (`RowMateDither`, ± 0,5/255), como pide el arte §8 (§5) |
| `Presentation/Styles/Skin.uss` | `.rm-dialog--glass`: el respaldo `glass.fallback` y el borde en `border.default`. Sin elementos ni cambios de tamaño (§5) |
| `Core/Scene/Art/{WaterLookArt, MaterialArt, ShaderArt}.cs` | Los números nuevos de las capas, de la estela y de la cubierta, con su `kRm*` en `ShaderArt` (los contratos de dotnet los comparan con los `.shader`). La ronda de ajuste del río y la costa (§2.2) |
| `Scene/Water/AdvancedWaterSurface.cs`, `Scene/SceneShaderIds.cs` | **Pegamento** (fuera de la lista de WP-D, DD6b-01): la banda de orilla y el carácter del escenario que Core ya calculaba (`WaterFrame.LeftBand`…, `WaterProfile.CalmMin`) no llegaban al material. 7 `Set*` por cuadro, sin asignar |
| `Snapshots/WaterProbes.cs` (N), `Snapshots/WaterCatalog.cs` | Las sondas de color que WP-B dejó para la ronda de WP-D (`WaterCatalog`: "las sondas de color de la spec las cierra WP-D"): costura, 10 líneas, camino de brillo y popa (§6) |
| `Art/Textures/Thumbnails/*` | Horneadas de nuevo (IA6b-14: la huella cambia con `ShaderArt` y los `.shader`). La imagen sigue con la degradada (DT6b-19) |

**No cambian**: `Foam.shader` (el provisorio de WP-B, 23 ALU con tope 30, #22: revisado en CAP-09 a -12 y CAP-19, se ve igual que en la 8), `Glow`, `WaterDegraded` (ya tenía el tope de #2 por `RowMateWater.hlsl`), `Base.uss`, `LakeArt` y `SkyArt` (los valores de #3 de WP-A quedan: §6.3).

## 2. El agua avanzada

### 2.1 Capas (orden del arte §2) y costo

| Capa | Dónde | Alto | Medio | Lite |
|---|---|---|---|---|
| Color base del contrato, ola del legacy, peso de Fresnel del legacy (DT6b-03), niebla, puente | vértice | ✓ | ✓ | ✓ |
| Absorción bajo la cámara `col·(1 − 0,18·(1 − F)·(1 − S(3; 25; z)))` (arte §3.4) | vértice | ✓ | ✓ | ✓ |
| Turbidez del río `lerp(col; #3E5A3A·√ambiente; 0,20)` | vértice | ✓ | ✓ | ✓ |
| Parches de calma: el ruido de 256 m de Core (6 y 11 vueltas, ≈ 43 y 23 m) × `CalmMin` del escenario sobre B2/B3 | vértice | ✓ | ✓ | ✓ |
| Normal: B2 + B3 (girado 37°) + B4 | fragmento | B2-B4 | B2-B3 | B2 |
| Sombreado de facetas `base·(1 + 0,8·(pendiente · astro.xz))` | fragmento | ✓ | ✓ | — |
| Reflejo: planar ½ × ½ | fragmento | ✓ | — | — |
| Reflejo: silueta del horizonte (perfil 256 × 1) | fragmento | — | ✓ | — |
| Reflejo: banda de orilla analítica (el alto del rayo reflejado en `\|x\| = X`, borde de 2 m) | fragmento | alta + baja | alta | — |
| Columna del legacy con destellos (piso, DT6b-04) | fragmento | ✓ | ✓ | ✓ |
| Astro sobre la normal: `pow(r·L; 1/α²)·(1 + 2·máscara)` (+ velo `pow(r·L; 0,06/α²)·0,10` en Alto) | fragmento | ✓ | ✓ | — |
| Costura: los últimos 3 dp hacia `skyHorizon` | fragmento | ✓ | ✓ | ✓ |
| Espuma de crestas (costa, ≤ 0,35) · contacto · salto degradado | ramas | ✓ | ✓ | ✓ |
| **Fragmento D3D11** (`SceneShaderTests`) | | **81 / 4 tex** (110/6) | **68 / 3** (70/5) | **32 / 1** (35/2) |
| Ramas | | 40 (40) | 40 | 40 |

- **DD6b-02** El vector reflejado se **normaliza**. Con el provisorio sin normalizar, cerca de la cámara las pendientes llegan a 0,3, `|r|` pasa de 1 y `r·L` se saturaba: el astro se "veía" en manchas blancas por toda el agua (toma de prueba `r1`, no versionada).
- **DD6b-03** El exponente `1/α²` va en el vértice (`skyH.w`) y el velo ancho solo en Alto: así Medio entra en 70 (68). El recorte del brillo en 1 del arte sale (el *bloom* de la hora hace el resplandor y la RT es de 8 bits).
- **DD6b-04** B3 va girado 37° respecto de B2: las dos tramas del mapa son periódicas y, alineadas, se leían como una grilla de rombos entre 20 y 60 m (CAP-03 de WP-B). La deriva de B3 queda girada lo mismo (imperceptible).
- **DD6b-05** La línea de contacto sigue el huso del casco: semiancho × `√(1,6·4t(1 − t))` de popa a proa. Con el semiancho constante se veían dos rieles oscuros que se abrían del casco en la popa.

### 2.2 Ronda de ajuste por escenario (`WaterLookArt`)

| Valor | Antes (arte) | Ahora | Por qué |
|---|---|---|---|
| Río B2 / B3, fuerza | 0,45 / 0,30 | **0,38 / 0,26** | Las pendientes cerca llegaban a 0,5 y el astro brillaba en casi toda el agua del canal a las 12 h |
| Costa B2 / B3, fuerza | 0,55 / 0,35 | **0,42 / 0,28** | Ídem: una alfombra de destellos en TH (CAP-19) en lugar del camino bajo el sol |
| Río / costa, α | 0,10 / 0,14 | **0,09 / 0,11** | Con 1/α² = 51 el lóbulo de la costa abría el brillo sobre todo el mar |
| `GlintGain` | — | **2** | Con 3, el camino del lago a las 12 h se recortaba en blanco en placas |

El orden de `WaterProfilesTests` (regata ≤ lago < río < costa, en fuerza y en α) se mantiene.

### 2.3 El planar de Alto no refleja las instancias (DD6b-06)

`InstanceBatch.Draw` encola cada dibujo instanciado **solo para la cámara principal** (`FillStreamer`, `KitPropRenderer`): el espejo no dibuja los pinos, la cabaña, las fachadas ni las tribunas (toma de prueba con el planar puro: cielo, terreno, bote, remos y remero). Arreglarlo es lógica de WP-B (y SetPass del espejo: Alto mide 71/72 de 80). **Mitigación de WP-D**: en Alto también va la banda de orilla analítica de Medio (con el tramo bajo), encima del planar, así el bosque, las fachadas y los álamos se reflejan como tono. Va a desvíos e issues.

## 3. La estela (`WaterMarks` tipo 5)

- **Apagado del legacy hacia la cámara**: alfa × `S(0; 5,3; z − z_cámara)` (`water.frag:88`, `fade = 1 − S(0; sternZ; back)`). Con la cámara del contrato se ve como en el legacy (nace en la popa y se apaga hacia la cámara); en el vuelo y la toma aérea, la V entera. Sin esto, los brazos llegaban al borde de abajo como dos haces de luz (CAP-02 de WP-B).
- **Cabeza**: el primer tramo nace con el 35 % y llega a pleno en 0,5 s: sin el borde recto en la popa.
- **Espuma cortada y batida en la z de MUNDO**: el ruido de 256 m de Core (`_RmWaterOffsets2.z`, Q-68) con 97 vueltas (alfa por punto de 0,55 a 1) y 171 vueltas (la fase del batido, ≈ 1,5 m). Con la z de cinta, la textura se deslizaba con el bote.
- **Centro × 0,6** (`WakeCenterGain`): se leía como una franja translúcida.
- **Luz de la hora**: la espuma × `_RmAmbient` además del ambiente de las marcas: de noche ≈ 0,25 de luminancia (arte §6.1).
- `WakeRenderer.WakeShaderGain` (0,45, de WP-B) queda: con el apagado y el corte, la estela a 4 m/s llega a ≈ 0,25 de alfa en la popa, como el 0,35 del legacy con el ruido.

## 4. La popa (#85)

`Hull.shader`: en la cubierta (`N.y` de 0,7 a 0,9) el Fresnel y el especular del gelcoat se recortan a **0,15** (`MaterialArt.HullDeckGloss`). A 22° desde la cámara, el sol bajo de enfrente se reflejaba entero en la cubierta (el medio vector queda casi vertical): esa era la "tabla blanca". Con el degradé y el afinado de `HullMesh` (WP-A), la sonda de CAP-31 da la variación de punta a punta de §6. La media de la cubierta queda en ≈ 0,9 (informativa ≤ 0,82): bajarla más choca con `MaterialArtTests.TheHullDeckStaysWhiteBelowClippingAtNoon` de la 6a (cubierta 0,80-0,95 a las 12 h), que manda (DD6b-07).

## 5. Vidrio

- `HudBlur`: tramado Bayer 4 × 4 (± 0,5/255) en la composición, fijo en la pantalla (dos tomas del mismo cuadro, iguales). La cadena (bajada, 3 Kawase de 1,5/2,5/3,5 texeles, composición con `α(L)` en sRGB) es la de WP-B y se ve bien en CAP-28 y CAP-29: las formas grandes (bote, orilla, sol) se leen y las cifras de atrás no.
- `.rm-dialog--glass` (Skin.uss): el respaldo `glass.fallback` y el borde `border.default` (12 %, el token más cercano al 10 % del arte: sin tokens nuevos). El reflejo interior lo dibuja `GlassSurface` (WP-B) en `generateVisualContent`. Las piezas del HUD no suman borde: 1 px movería el contenido y las zonas de la 4b (DD6b-08).

## 6. Sondas y capturas

`WaterProbes` lee la RT de la escena del mismo cuadro (con el post, sin la UI) y proyecta con la cámara real. Resultados de las dos corridas publicadas (iguales):

| Sonda | Tomas | Resultado | Tipo |
|---|---|---|---|
| Costura del horizonte (p95 por canal, tope 6/255) | CAP-09 a -12 (costa, mar abierto) | **1/255** en 497-534 columnas | aspecto: pasa |
| ídem | CAP-01 a -08, -13 a -16 (canal) | 18-81/255 en **11-13 columnas**: no concluyente (< 32) | aspecto (§6.1) |
| Líneas de la degradada: sin la 11.ª en el borde de abajo (#2) | CAP-21 a -24 | **ausente** en las 4 | contrato: pasa |
| Líneas de la degradada: vistas + tapadas | CAP-21 a -24 | 10 en CAP-21; 9 en CAP-22 a -24 (la fase está cerca de 0,1 y la 10.ª cae en las últimas filas, contra el borde) | aspecto |
| Camino de brillo a ± 3 m del bote (≥ 30 px con L > 0,85) | CAP-02, -06, -10, -14, -17 a -20, -25 | 4 270 a 59 369 px | aspecto: pasa |
| Popa: variación de punta a punta (≥ 8 %, AC-6B-21) | CAP-31 | **13,1 %** (cockpit 0,944, punta 0,82) | contrato: pasa |
| Popa: media (≤ 0,82, informativa) | CAP-31 | 0,897 | aspecto: no llega (DD6b-07) |

Las dos corridas dieron los mismos PNG byte a byte (md5) y las mismas sondas. Ninguna toma con agua distinta de la pedida (`water:` en el manifiesto). Ninguna sonda de contrato falla; las 16 que el INDEX marca son de aspecto (13 costuras del canal no concluyentes, 3 cuentas de líneas y la media de la popa).

### 6.1 Lo que no se mide (y por qué)

- **Costura en el lago, el río y la regata**: el canal toca el horizonte en ≈ 13 columnas (a 1 400 m mide ± 8,4 m); con menos de 32 la sonda es **no concluyente** (diseño §8: hace falta la pasada sin siluetas, con `HorizonRenderer` y la orilla lejana apagados). En la costa (mar abierto) pasa. A issues.
- **`fogRow`** (CAP-28, -30, -31): las dos pasadas de prueba con `_RmWaterFogScale` no existen todavía (globales solo de prueba). La niebla a 400 m la prueba dotnet (`WaterModelTests.FogReachesFogColorAt400`). A issues.
- **Halo (CAP-03) y silueta (CAP-17)** de #3: a las 18 h el sol queda detrás de los pinos de la derecha en TV, así que la sonda del halo no tiene disco; las montañas de CAP-17 se ven nevadas contra el cielo, con la ladera izquierda baja (a ojo, > 1,3:1). Los valores de WP-A de `LakeArt`/`SkyArt` quedan (DV-153 no pide cambiarlos sin una sonda que falle).
- **CAP-27** es la misma toma que CAP-25 (Medio, lago, sin `Series 600` ni los dos paneles): el catálogo de WP-B no arma el paso de trabajo. A desvíos.

### 6.2 Revisión de las 32 tomas

- **Lago** (CAP-01 a -04, -17, -25, -26, -30, -31): el agua se lee por el reflejo, el brillo y la estela, no por la altura de la ola (EXR). A las 12 h, el camino de destellos rodea el bote (el punto espejo cae en ≈ 10 m); a las 22 h, el camino plateado de la luna; a las 6 y 18 h, el cielo cálido reflejado lejos. Las calmas cortan el rizo en parches. Medio (CAP-25) casi igual a Alto; Bajo (CAP-26) sin destellos, con la columna del legacy.
- **Río** (CAP-05 a -08, -18, -28): chop corto y más turbio; el puente y las fachadas como tono reflejado por la banda; de noche, el brillo bajo las luces. El destello de las 12 h sigue denso (es un canal estrecho con el sol de frente), pero ya no satura.
- **Costa** (CAP-09 a -12, -19): el mar de fondo con crestas y espuma ≤ 0,35, el camino del sol ancho a las 12 h y el de la luna a las 22 h. Costura limpia (1/255).
- **Regata** (CAP-13 a -16, -20): planchada, casi un espejo; el camino corre entre los carriles sin líneas que se confundan con ellos.
- **Degradada** (CAP-21 a -24): la de la 6a con el tope de 10 líneas.
- **Vidrio** (CAP-27 a -29): las tarjetas y el diálogo con el fondo desenfocado y el tinte por luminancia; el contraste es el del respaldo o mejor.
- **Charcos** (CAP-32): el par de remolinos, el hervidero y los micro-anillos se leen en TH.
- **Estela** (todas las tomas con remada): nace en la popa, se abre en V y se apaga hacia la cámara como en el legacy, cortada y batida; de noche, tenue.
- **Popa**: con el gelcoat recortado y el degradé, se lee como casco afinado y no como tabla; sigue clara a mediodía (DD6b-07).

## 7. Lo que cambia en sets viejos (no se regeneran)

- **Hull** (#85): toda toma con el bote a la vista (6a, 7, 8, 14, 15 y los inmersivos de 4a/4b/5/11): la cubierta con sol bajo de frente se ve menos blanca. Con la degradada fija (DT6b-24) el agua no cambia.
- **`.rm-dialog--glass`**: el diálogo de completado dentro del inmersivo pasa de `surface.overlay` a `glass.fallback` + la imagen del vidrio (WP-B ya le ponía la clase). Afecta las tomas del diálogo de completado de 4a/4b.
- Las miniaturas del selector (horneadas de nuevo): la huella cambia; la imagen, solo por #2/#3/#85 de la degradada.

## 8. Para issues

- El planar de Alto sin las instancias (pinos, cabaña, fachadas, tribunas, boyas): encolar `InstanceBatch` también para la cámara del espejo en LOD1, medido contra el tope de 80 (DD6b-06).
- La sonda de la costura con la pasada sin siluetas (lago, río y regata) y `fogRow` con sus dos pasadas de prueba.
- CAP-27 con `Series 600` y los dos paneles (la toma hoy repite CAP-25).
- La sonda del halo de #3 con el disco a la vista (otra hora u otra cámara) y la de la silueta de CAP-17.
- El tramo bajo de la orilla (muro del río, talud de la regata) en Medio: no entra en el tope de 70 ALU.
- Borde de 1 px `#FFFFFF @10 %` en las piezas de vidrio del HUD sin mover el contenido (un `generateVisualContent` o un borde dentro del *padding*).
- La media de la popa ≤ 0,82 contra la cubierta 0,80-0,95 de la 6a: decidir cuál manda (PO).

## 9. Pruebas (estado final de WP-D)

| Corrida | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1` | 4 165: 4 126 pasan, 0 fallan, 39 ignorados |
| EditMode | 4 308: 4 253 pasan, 0 fallan, 55 ignorados |
| EditMode `-Simulator` | 4 502: 4 447 pasan, 0 fallan, 55 ignorados |
| PlayMode | 767: 767 pasan |
| PlayMode `-Simulator` | 848: 848 pasan |
| `SceneShaderTests` (D3D11) | WaterAdvanced Alto 81/4, Medio 68/3, Lite 32/1, ramas 40; WaterMarks 13 + 28; Foam 23; Hull 21 |

Carga de los otros procesos del equipo durante las corridas de Unity: 15-35 % de la CPU total (no se tocó ningún proceso). `ProjectSettings` sin cambios (guardia de cada corrida); `HorizonFar.mat`/`HorizonNear.mat` sin tocar; el legacy sin cambios.

---

## 10. Cambios por verificación (ronda 1, designer)

Contra [05-qa-verification](05-qa-verification.md): QA-V6b-02, -03, -04, -05 y -07 (y de paso QA-V6b-15 en la toma aérea). Primero la prueba que reproduce, después el arreglo. Corridas en `<scratch>/it6b/des-v/`, `ROWMATE_DATA_DIR` temporal, una sola instancia de Unity, sin tocar procesos ajenos. Pantalla: además del monitor en modo de respaldo ahora el monitor está en su modo normal; esta ronda no mide GPU ni tiempos del build (no los pide ningún ítem).

| Ítem | Reproducción | Arreglo (una línea por decisión) | Resultado |
|---|---|---|---|
| QA-V6b-02 · AC-6B-14 (estela real) | N `PlayMode/Scene/WakeRealHistoryTests` (R-4 a los 30 s, sin `PrimeWakeForTest`): la cinta arrancaba en 4,998 m con la popa en 5,309 (el punto más nuevo se emite cada 0,25 s) y el brazo a 2,8 m conservaba el 77 % del contraste de 4 m. dotnet `TheHeadIsTheDrawnSternEveryFrame`, `TheHeadFadesInFromTheStern` | **DD6b-09** `WakeTrail.CopyTo`: el lugar 0 es la **cabeza viva** (la popa que se dibuja en el cuadro, con el *surge*, edad 0); los guardados van del 1 al 47. **DD6b-10** La rampa de la cabeza pasa a Core por distancia a la popa que se dibuja (`ShapedAlpha` = alfa × S(0; 1,2 m; retroceso), `WaterLookArt.WakeHeadLength`): alfa 0 en la popa, sin borde recto (antes era por índice de vértice en el *shader*). **DD6b-11** El apagado hacia la cámara del legacy, S(0; 5,3; z), va **al cuadrado** (`WaterMarks`): en TV los brazos salen por los costados a z ≈ 2,5 m, donde S valía 0,45 | La prueba pasa: `_RmWake[0]` = la popa, alfa 0; a 2,8 m ≤ 50 % del contraste de 4 m. **VID-03** (sin precarga, de la entrada a la pausa) con su comprobación por cuadro: 0 problemas |
| QA-V6b-03 · AC-6B-17 (#12) | Sondas nuevas en CAP-32 y en la CAP-04 de `it7` (`WaterSpecProbes.Puddles`/`Boil`): adentro 1,07-1,10:1, hervidero 1,14:1 | **DD6b-12** Con la avanzada (`_RmMarkTint.x` = 1 en el charco; con la degradada, el charco de la 7 igual) el charco **aplana**: un disco liso del agua cercana × (1 − 0,6) con alfa pleno, los ojos × (1 − 0,85) y el borde de espuma + 0,25; alfa mínimo hasta los 2 s 0,35 → 0,65 (tapa los destellos de adentro). **DD6b-13** Hervidero: alfa × 1,5 (era 1,25) y 0,2 m a popa del centro de la pala (era 0,1: quedaba debajo de la pala desde la cámara de atrás). **DD6b-14** CAP-32 con micro-anillos inyectados (`InjectRingForTest`, 2 de gota y 2 de ataque: con la palada congelada no nacen). **DD6b-15** La CAP-04 de la 7 se toma en la salida de pala (antes 1 s después: el charco quedaba a 1,1 m, debajo del cuadro) | CAP-32: charcos adentro **1,31 y 1,26:1**, borde 1,38 y 1,39:1, remolinos a **39,6° y 39,4°**, hervidero **1,28 y 1,17:1**; 2 charcos, 2 hervideros y 4 anillos en el manifiesto. CAP-04 de la 7: charcos de 0,8 s a 5,1 m: adentro **1,16 y 1,17:1**, borde 1,51:1, remolinos a **39,2° y 41,1°**. El ángulo es el eje principal del 6 % más oscuro del charco en el plano del agua; con el charco cortado por el borde del cuadro es no concluyente (aspecto) |
| QA-V6b-04 · AC-6B-27 | CAP-29 con `Corta` de verdad hasta los 36,3 s y la toma aérea (sin `OpenCompletionForTest` ni datos inventados) y la sonda `SternUnderDialog`: +5,6 % (la escena sola: popa 0,81 contra agua 0,45) | **DD6b-16** El α del vidrio con el L de la **zona** para lo más claro que la zona (± 8 texeles en diagonal, `GlassTint.RegionalAlpha`): L_α = máx(L_zona; L − 0,25); lo más oscuro sigue con su α. Con el α por píxel el compuesto era casi plano entre L 0,4 y 0,7 (el α sube con L y se come la diferencia). El compuesto nunca pasa el del respaldo sobre blanco (dotnet `RegionalCompositeNeverPassesTheWorstCase`): AC-6B-24 no cambia. La sonda se lee a 1 m de la punta (después del afinado de 0,6 m de #85) | CAP-29: popa bajo el diálogo **+18,0 %** (L 0,107 contra 0,091; antes +5,6 %), cámara `Aerial`, el diálogo con los datos de la sesión (00:30, 1120 m, 43). CAP-27 (AC-6B-23): variación dentro de cada pieza **6,3-21,2 %** de la de sin pieza (tope 25 %), con `Series 600` y los dos paneles; CAP-28 sin cambios de sonda |
| QA-V6b-05 · AC-6B-04 (#3) | Sondas (a), (b) y (c) en CAP-02, CAP-17 y CAP-03: el centro 1,14:1 | **DD6b-17** Contraluz de la cordillera lejana (solo el lago, la de nieve): tono y nieve × (1 − 0,6·exp(−(Δacimut/12°)²)·S(sol alto)) (`LakeArt.Backlight`, `BacklightLobeDeg`). Con un factor parejo (0,3 y 0,45) los costados empeoraban (1,08:1): ahí la nieve clara ya contrasta con el azul más oscuro y oscurecerla la igualaba al cielo; el lóbulo sigue al resplandor del cielo alrededor del sol. Los números del legacy (DV-65, DV-64) no cambian. **DD6b-18** (b) se mide en la imagen: de los acimuts con la cresta lejana a la vista y sin la cordillera cercana delante, los que tienen la orilla verde encima. **DD6b-19** (c) arriba del disco (en TV el disco de las 18 h queda detrás de los pinos); toma de evidencia **CAP-33** (lago 18 h en TH, con el disco a la vista) | (a) CAP-02 (TV): izquierda **1,34:1**, centro **1,55:1** (la derecha, tapada por los pinos); CAP-17 (TH): **1,81 / 1,38 / 1,32:1**. (b) CAP-17: **28,3 %** (30 de 106 acimuts). (c) CAP-03: el cielo a 2 diámetros **+76 %** sobre el de 6; CAP-33: **+135 %**. El disco de las 18 h no se ve en ningún tamaño (en TV, detrás de los pinos; en TH, detrás de la loma y la cabaña): la sonda mide arriba del disco, que es cielo |
| QA-V6b-07 · AC-6B-33 | — | CAP-27 = `Series 600` a los 50,2 s con los dos paneles (Alto). CAP-29 (arriba). VID-01 a -05 con comprobaciones por cuadro (`WaterVideoChecks`). `WaterSpecProbes`: -04 (a, b, c), -11 (desvío 2-20 m y el orden en CAP-14), -12 (al pie de la orilla, mediana), -13 (centro del camino a ± 40 dp; "no aplica" si el astro cae fuera del agua; la luna más tenue que el sol de las 12 h), -17, -18 (de día 0 columnas; de noche, cada columna medible más clara que sus costados), -19 (espuma de crestas, informativa), -23 (variación en cada pieza de vidrio), -27. **DD6b-20** Las columnas de luz (AC-6B-18) van **bajo la x de pantalla** de la luz (x = X·z/zF de 3 m al pie, ancho constante en pantalla, alfa de 0 cerca a pleno en el pie, cortadas en franjas de 0,9 m que corren con el agua) y sus alfas × 2: con la x de mundo las de las fachadas y los faroles caían sobre el muelle (la sonda daba 4 de 19) | -11: regata 0,077 ≤ lago 0,089 < río 0,172 < costa 0,208. -12: lago **36,5 %**, río **10,6 %** más oscura. -13: a ± 40 dp donde aplica (lago y río 12 h ≤ 4,2 dp; costa 6, 12 y 18 h 39,8, 24,4 y −20,4 dp; regata 12 h 20,6 dp); a las 6 y 18 h en el canal el sol cae sobre la orilla (no aplica); la luna, más tenue que el sol en los 4. -18: 0 columnas de día; de noche, río 20 de 20 y lago 1 de 1 más claras que sus costados (costa: la del faro no es medible; regata: sin luces). -19: 15,3 % (informativa). VID-01 a -05 en el set |
| QA-V6b-15 (de paso) | En la toma aérea el huso oscuro de la línea de contacto quedaba corrido detrás de la popa (se veía bajo el diálogo de CAP-29) | **DD6b-21** `WaterAdvanced`: la huella del casco se compara en z de cinta (la z desde la cámara + la de la cámara); con la cámara del contrato es lo mismo | Los rieles de CAP-04 de la 7 y el óvalo de CAP-09/-11 siguen para QA |

### 10.1 Lo que destapó VID-01 (AC-6B-20 b): un salto del agua cada 16 m

VID-01 cruza 4096 m y su comprobación **falla**: la diferencia entre cuadros del agua de 16 a 60 m (bloques de 9 × 9 px) tiene un pico de **4,1 ×** la mediana justo en el cruce. No es 4096: corrido a 4112 m da **5,7 ×** en ese cruce. Es el ancla de la malla en anillos (`WavePhases.GridAnchorZ` = Mod(D; 16), WP-B): cada 16 m la malla salta 16 m y los bordes de los anillos (paso 1, 2, 4… m) cambian de lugar, así que lo que el vértice calcula con frecuencia alta (la pendiente de B1, λ ≈ 16 m en el lago, y la cresta de la columna) se re-muestrea con otra densidad. Es de arquitectura (la malla de WP-B) y de presupuesto (llevar B1 al fragmento cuesta ≈ 3 ALU y Medio está en 70/70): va a **issues** con el dato. VID-01 queda versionado con la falla anotada en el manifiesto.

### 10.2 Capturas

- Set `it6b` regenerado entero (38 tomas: CAP-01 a -33 y VID-01 a -05) y una segunda corrida a `<scratch>`: **38/38 PNG y MP4 iguales** byte a byte y las mismas sondas y valores en el manifiesto. No escribe en otras carpetas. CAP-04 de `it7` regenerada sola (el manifiesto de la 7 se fusiona).
- Sondas: 85 en el manifiesto: 69 pasan, **0 de contrato fallan**, 16 de aspecto fallan (las de antes: 13 costuras del canal no concluyentes, 3 cuentas de líneas de la degradada y la media de la popa). VID-01 falla su comprobación (§10.1).
- Miniaturas horneadas de nuevo (la huella cambia con `ShaderArt` y los `.shader`; la imagen, por el contraluz del lago).
- **Sets viejos** (no se regeneran): el contraluz cambia la cordillera del lago a las horas de sol en toda toma del lago (6a, 7, 8, 14, 15); el vidrio (`HudBlur`) cambia en las tomas con la escena real en Alto o Medio y el HUD inmersivo. Los charcos, el hervidero y las columnas solo cambian con la avanzada (los sets viejos van con la degradada, DT6b-24), salvo la CAP-04 de la 7 (regenerada).

### 10.3 Pruebas

| Corrida | Resultado | Carga de otros procesos |
|---|---|---|
| `tools/dotnet/run-tests.ps1` | **4177: 4138 pasan, 0 fallan, 39 ignorados** (+194/194) | — |
| Unity EditMode | **4320: 4265 pasan, 0 fallan, 55 ignorados**. `SceneShaderTests`: WaterAdvanced Alto 83/110, **Medio 70/70**, Lite 33/35 (ramas 40/40); WaterMarks 15/20 + **40/40** en ramas | 20 % antes; 27 % de media, 37 % máx |
| Unity EditMode `-Simulator` | **4514: 4459 pasan, 0 fallan, 55 ignorados** | 22 % antes; 32 % de media, 72 % máx |
| Unity PlayMode | **796: 796 pasan** (con `WakeRealHistoryTests`) | 29 % antes; 25 % de media, 38 % máx |
| Unity PlayMode `-Simulator` | **877: 876 pasan, 1 falla**: `SceneBudgetTests.MemoryAndSceneCpuPerTierWithinTheBudget(High)`, CPU de la escena p95 **1,518 ms** (tope 1,5) a mitad de la corrida larga. La clase sola: **10/10, p95 0,97 ms** (0,96 / 0,97 / 1,22). No se relaja el tope; lo que esta ronda suma por cuadro es una rampa por punto de la estela (48 `SmoothStep`) | 16 % antes; 21 % de media, 34 % máx (la clase sola: 31 % de media, 48 % máx) |
| `capture-snapshots.ps1 -Set it6b` ×2 | 38/38 iguales; 0 sondas de contrato fallan; VID-01 falla su comprobación (§10.1) | — |

Builds regenerados: `normal` (release: 0, sin rastros del simulador, `RowMateBle.dll`, VERSIONINFO 1.0.0) y `simulator` (0). `ProjectSettings` sin cambios en todas las corridas (la guardia); `HorizonFar.mat`/`HorizonNear.mat` sin cambios; el legacy sin cambios. Medio queda en 70/70 ALU y las ramas de WaterMarks en 40/40: sin margen (la huella en z de cinta suma 1 en el fragmento; si hace falta, pasa al vértice).

### 10.4 Issues elegidos (lo que cambia)

| Issue | Estado | Qué queda |
|---|---|---|
| #3 | **Resuelto** | — (las tres sondas en el manifiesto) |
| #12 | **Resuelto** | — |
| #13 | **En parte** | El espejo de Alto sin las instancias (DD6b-06) |
| #31 | **En parte** | El espejo sin las instancias; la espuma de crestas medida como cobertura (informativa) |

### 10.5 Para issues

- El salto del agua cada 16 m (§10.1; AC-6B-20 b): la malla en anillos con el ancla de 16 m y lo que el vértice calcula con frecuencia alta.
- AC-6B-15 `fogRow` y la costura del canal sin siluetas (QA-V6b-08) siguen sin la pasada de prueba.
- La popa media 0,897 contra 0,82 (DD6b-07, PO).
- Los remolinos se leen todavía como "carita" (dos medias lunas y un borde redondo): probar medias lunas abiertas en espiral.
- La comprobación de -13 a las 6 y 18 h no aplica en el canal (el sol cae sobre la orilla en TV); la costa la cumple.
