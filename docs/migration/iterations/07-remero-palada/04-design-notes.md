# It. 7 — Notas de diseño (WP-D: shaders, materiales y pulido)

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-06 |
| Entrada | [02-tech-design](02-tech-design.md) §7.3, §7.4 y "Cambios por implementación (WP-B)" (B7-01: WP-B1 escribió versiones mínimas de los cinco shaders); [00-arte-remero](00-arte-remero.md) §2.3, §5, §8, §9; las tomas de `it7` ([snapshots/](snapshots/INDEX.md)) |
| Modo | Entrega rápida: cada ajuste en una línea; lo menor va a "Para issues" |

WP-D1 no llegó antes de WP-B1, así que este paquete es D1 + D2 sobre lo que dejó B1: se conservaron el contrato de §7.4 (nombres, colas, propiedades, arreglos que pone `SceneWorldBuilder` desde `RowerArt`) y la lógica de Unity y Core sin tocar. Todo número nuevo vive en `Core/Scene/Art/RowerArt.cs` y el shader lo copia como `kRm*` (lo comprueba `RowerShaderContractTests`).

---

## 1. Qué se vio en las tomas de B2 y qué se cambió

| Toma | Problema | Cambio |
|---|---|---|
| CAP-17 / CAP-18 (cámara lateral) | El mono se veía "plástico": las franjas cian se degradaban a lo largo de una cara entera de la malla (8 cm en el tronco) y el especular era ancho y fuerte | Regiones con borde nítido (§2.1) y especular apagado × 0,25 (§2.2) |
| CAP-20 / CAP-03 (TV) | Bordes de las regiones con halo cian; frente plano | Bordes nítidos; brillo de borde de lycra y terminador cálido en la piel |
| CAP-04 / CAP-20 (TV) | Los charcos se leían como óvalos blancos lisos, sin los remolinos | Par de remolinos contrarrotantes con ojo oscuro y media luna de espuma (§4.1) |
| B2 Para issues | "Hervidero y charcos poco visibles a 47 dp en TV" | Espuma × 1,6 sobre el alfa de Core; hervidero batido blanco (§4.2) |
| CAP-15 / CAP-16 | La boya, naranja mate | Brillo de plástico (suavidad 0,7, §5) |

## 2. `RowMate/Scene/Rower` (DT7-09, DA7-05)

### 2.1 Bordes nítidos con una grilla de celdas

La región viene en el rojo del color de vértice. Interpolar el color degrada cada borde a lo largo de una cara entera; elegir la región con el mayor de nueve pesos cuesta ≈ 60 ALU. Solución (4 ALU): cada región tiene una celda en una grilla de 3 × 3 armada para que las regiones que se tocan en la malla sean vecinas (también en diagonal); el vértice manda la celda, la interpolación va en línea recta y el fragmento redondea.

| | y = 0 | y = 1 | y = 2 |
|---|---|---|---|
| **x = 0** | franjas | mono | panel |
| **x = 1** | medias | piel | articulaciones |
| **x = 2** | anteojos | pelo | gorra |

Los vecinos que importan: mono–franjas, mono–piel (rodilla, cuello, hombros), piel–medias, piel–pelo, piel–anteojos, pelo–anteojos. En un triángulo con tres regiones distintas, el redondeo no sale de las tres celdas para los tríos que da el maniquí (mono–franjas–piel, piel–pelo–anteojos). La franja queda de una cara de ancho (≈ 4 cm en el muslo, ≈ 8 en el tronco).

### 2.2 Luz

`luz = (ambiente + relleno + clave·sombra) · albedo + terminador·sombra + extras + especular`

| Término | Valor (`RowerArt`) | Dónde |
|---|---|---|
| Relleno del frente (ART_BIBLE §7.9) | `RimFill` 0,12 · sunColor · saturate(N·V) (`RmFrontFill` en `RowMateLighting.hlsl`) | vértice |
| Envolvente | tela `ClothWrap` 0,25, piel `SkinWrap` 0,4 (arte §2.3) | vértice |
| Terminador de la piel | `SkinScatter` 0,22 · clave · `#E89A80` en la banda \|N·L − 0,1\| < 1/3 | vértice, × sombra |
| Brillo de borde del mono | `SuitSheen` 0,12 · (1 − N·V)⁴ · T(a)² (blanco @12 % teñido por la hora) | vértice (regiones 0–2) |
| Anteojos | `GlassesReflect` 0,7 · Fresnel de Schlick · cielo del horizonte | vértice (región 5) |
| Especular | Blinn con exponente 2^(1 + 7·suavidad), peso `RowerSpecular` 0,25 · suavidad · luz clave² · visibilidad del astro | fragmento (medio vector en el vértice) |

Costo del fragmento en Medio (D3D11, sin sombras de URP): **26 ALU** (tope 30), 0 texturas. Vértices: 6 k (maniquí) o 55 k (Y Bot) con *skinning*: sin cambio de costo relevante.

## 3. `RowMate/Scene/Oar` (DA7-06, DA7-07)

Sin cambios de forma (la flexión y el ancho mínimo de 1,2 dp de B1 se ven bien en CAP-17 y CAP-20). El medio vector pasa al vértice: **19 ALU** (tope 20). `kRmOarTip` 2,0 m = `RowerArt.OarTipFromPin` (centro de pala 1,76 + media pala).

## 4. El agua que responde

### 4.1 Charcos (`WaterMarks`, tipo 0; arte §8.3)

- **Par de remolinos** a ±`SwirlSeparation`/2 = 0,225 m del centro, en `x` local (el eje de la pala en el finish queda a ≈ 40° de `x`: Para issues). El espejo en `x` da la contrarrotación sin cuentas.
- **Ojo** liso y oscuro (`waterNear` × 0,6, alfa `MarkEyeAlpha` 0,55): agua sin ondas que refleja menos cielo. Radio `SwirlEye` 0,10 + `SwirlEyeGrowth` 0,035·edad m.
- **Media luna de espuma** en el borde del ojo, orientada por el giro de Core (`_RmMarks.y`).
- **Borde del charco**: anillo tenue (× 0,35) en el radio de Core (0,3 → 0,9 m).
- **Alfa**: el de Core es el de la mancha media; la espuma llega a × `MarkFoamGain` 1,6 porque desde la cámara del contrato el quad se ve de canto (a 10 m, 0,9 m de radio son ≈ 3 dp de alto).

### 4.2 Hervidero (tipo 1) y anillo de la boya (tipo 2)

- Hervidero: espuma blanca (cielo × blanco 60 %), densa en el centro y batida con dos senos que se mueven con `_RmSceneTime` (determinista), × (0,8 + 0,4·esfuerzo).
- Anillo de la boya: una línea de espuma en el 85 % del radio.

Costo: **11 ALU** fuera de las ramas + **28** en la rama del tipo (tope 20 + 32, `[branch]`: cada instancia es de un tipo y cubre una parte chica de la pantalla), 0 texturas, 1 *draw call* para las 16 instancias.

### 4.3 Salpicadura y gotas (`Spray`)

La gota llega a opaca en el 50 % del radio (`DropCore` 2): con el tamaño mínimo de 1,5 dp (1,0 los trazos) ya no se ve como un disco gris difuso. **15 ALU** (tope 16).

## 5. La boya (`Buoy`, arte §9)

Brillo de plástico: Blinn con suavidad 0,7 (`BuoyGloss` = 2^5,9 ≈ 59,7), peso `BuoySpecular` 0,45, franja `BuoyWhite` 0,94 (lineal). **23 ALU** (tope 25).

## 6. Sombra y agua (DA7-04, DT7-08)

- `WaterDegraded` (B1): recibe la sombra de la luz principal; en sombra plena oscurece `kRmShadowDarken` 0,10 en "gamma 2" (≈ 19 % en lineal). En CAP-07 (17 h) la sombra del remero se estira hacia la cámara, como pide el arte §7. Sin cambios: la medida de la 6a (24 ALU sin sombras) sigue.
- `BoatShadow` (B1): el lado como uniform (`_RmShadowOffset.x` con signo, de `ShadowSideFader`). Sin cambios.
- `Hull`: los herrajes y el carro ya entran por color de vértice con el shader del casco (B1); sin cambios.

## 7. Contrato y pruebas

- `ShaderArt.Rower` (5 especificaciones: nombre, cola, presupuesto, propiedades) y `ShaderArt.RowerConstants()` (16 constantes con su fuente en `RowerArt`).
- `Tests/EditMode/Scene/Stroke/RowerShaderContractTests.cs` (dotnet y Unity): nombre y cola, sin el reloj de Unity, `RowMateScene.hlsl` y target 3.5, propiedades del material y de instancia, instanciado o SRP Batcher, lo opaco proyecta, lo transparente después del agua y sin profundidad, cada `kRm*` igual a `RowerArt`.
- `SceneShaderTests.TheRowerFragmentsFitTheirBudget` (Unity): mide los cinco en D3D11 contra `ShaderArt.Rower`.

| Shader | ALU (tope) | Rama (tope) | Texturas |
|---|---|---|---|
| Rower | 26 (30) | — | 0 |
| Oar | 19 (20) | — | 0 |
| Spray | 15 (16) | — | 0 |
| WaterMarks | 11 (20) | 28 (32) | 0 |
| Buoy | 23 (25) | — | 0 |

## 8. Biblias actualizadas

- ART_BIBLE §7.8 (carro, pedalera, sombra, reflejo postergado, presupuesto real) y §7.9 (un shader con regiones, pelo y anteojos, maniquí sin avatar, presupuesto del slot).
- ANIMATION_BIBLE §0.3 (nota DT7-01) y §7.3 reescrita: solver propio, `Animator` deshabilitado, `GripPose` (DT7-07).
- ASSET_SLOTS §2.5 reescrita: el usuario solo copia el FBX; `MixamoImportPostprocessor` fija los ajustes (Read/Write ✓, materiales `None`; DT7-04) y el *binding* reemplaza al arrastre.

## Cambios por verificación

| ID | Qué se cambió | Prueba |
|---|---|---|
| QA7-V-01 | **La tabla de 10 poses de ANIMATION_BIBLE §3.4 es la hoja medida de A7-01** (DV-78 en la spec; AC-SCN-05-08 dice "los 60 valores, cada cuerpo con su columna"). Rodilla, cadera, hombro y codo, redondeados de lo que da Core con las medidas reales y las manos en el puño del remo real; tronco, carro, remo, pala y plumeo, los de diseño. **Y Bot tiene columna propia**: con sus largos medidos (B7-02, brazo 0,274 m) el finish da hombro −43° y codo 54° contra −9° y 70° del maniquí (34° y 16° de diferencia: ninguna tabla única cierra a ± 8° con los dos). Sin cambios de código de la app: el rig ya sigue a Core (≤ 1,6° con Y Bot, ≤ 0,02° con el maniquí; CAP-17, CAP-18, `RowerRigTests`) | `ScullingKinematicsTests.TheTenPosesOfTheTable("maniqui"/"ybot")`: borrado `Checked`, exige los 60 valores (± 8°) con cada cuerpo. **Rojo antes** (maniquí 21 fuera, p. ej. c 0,09 rodilla 75,9 contra 112, c 0,60 codo 128,1 contra 178; Y Bot igual), verde después |

## Para issues

- CAP-04 en TV no muestra los charcos: con `R-4` el par está a `x` ±2,1 m y `relZ` ≈ 1 m de la cámara (sonda del `manifest`), fuera del cuadro vertical. Tomarlo en TH o con la escena sola y fase forzada.
- Los remolinos van sobre `x` local; orientarlos por el eje de la pala en el finish (≈ 40°) necesita el giro del quad desde `WaterInteractionRenderer`.
- Franja de una cara de ancho (8 cm en el tronco del maniquí, el arte pide 3): una columna de vértices más en `MannequinMesh` o antialias del borde (`fwidth`) si titila en movimiento.
- La coronilla del maniquí sale en piel vista desde la cámara del contrato (la regla de pelo de `MannequinMesh` mira `n.Z`): subir la línea del pelo.
- Y Bot: las manos con la región de articulaciones se ven "manchadas" a 47 dp; pintar la mano entera de piel en `RowerOutfitBaker`.
- Normal de tejido de la caña y del mono, brillo de sudor por tiempo activo, tarjetas de pelo (ART_BIBLE §7.9): no se ven a 47 dp.
- LOD de Y Bot para móvil (55 k con *skinning*).
- Recuperación con los brazos doblados (QA7-V-01): en "manos afuera" (c 0,60) el codo queda en 128° (114° con Y Bot) porque las manos siguen al puño del remo real, que vuelve más lento que los brazos. La técnica pide brazos estirados antes de pivotar: adelantar el barrido de la recuperación (contrato del remo) o separar el puño visual del contrato.
- Y Bot mide 1,786 m (fuera de 1,80–1,88): escalarlo a 1,85 m (WP-A') acercaría su columna a la del maniquí.
