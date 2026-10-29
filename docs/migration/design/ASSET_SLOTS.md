# Asset slots — RowMate (Unity 6 LTS + URP)

| Campo | Valor |
|---|---|
| Autor | `rowmate-designer` |
| Fecha | 2026-10-27 |
| Base | Decisión del usuario (assets 3D solo gratis, detrás de slots intercambiables, R-08) · [ART_BIBLE.md](ART_BIBLE.md) · [ANIMATION_BIBLE.md](ANIMATION_BIBLE.md) · [ROADMAP](../ROADMAP.md) (iteraciones y prerrequisitos) · [ARCHITECTURE](../architecture/ARCHITECTURE.md) §1.2 (paquetes), §2 (árbol de carpetas) y §14 (slots) |
| Alcance | Todo asset externo o reemplazable de la app: qué es, su reemplazo procedural, el asset gratis recomendado (link y licencia) y los pasos exactos que tiene que hacer el usuario |

---

## 0. Cómo funciona un slot

El modelo y las rutas los fija el Architect: [ARCHITECTURE §14.4](../architecture/ARCHITECTURE.md) (slots) y §2 (árbol, única referencia de rutas). Si algo de este documento no coincide con ARCHITECTURE, **manda ARCHITECTURE** (QA-P-15).

- Un **slot** es un `ScriptableObject`. Hay una base genérica (`AssetSlotDefinition`: `slotId` estable, ruta esperada, presupuesto, validación, placeholder procedural) y **subtipos tipados** (`RowerCharacterSlot`, `BoatPartSlot`, `OarSlot`, `PropSlot`, `TerrainLayerSlot`, `VegetationSlot`, `MaterialSlot`, `TextureSlot`, `FontSlot`, `DataSlot<…>`; `EnvironmentDefinition` agrupa los de un escenario). Ejemplos de reglas de validación: "Humanoid obligatorio", "≤ 30 k triángulos", "textura ≤ 1 024 en móvil".
- **Resolución:** asset asignado y válido → se usa. Falta o no pasa la validación → se usa el placeholder y se escribe **un** aviso `[SLOT]` en el log de diagnóstico. **Nunca** un error que frene el build o la escena. El menú `RowMate/Slots/Validar` lista los slots que están en placeholder y por qué.
- Los ids viven también como constantes en el Core (`SlotIds`) para poder testear con dotnet que cada tipo de objeto de orilla del catálogo legacy (15) tiene su slot.
- **Rutas** (ARCHITECTURE §2): instancias de slots (`*.asset`, una por id) en `Assets/RowMate/Slots/`; sus clases en `Assets/RowMate/Scene/`; fuentes en `Assets/RowMate/UI/Fonts/`; íconos en `Assets/RowMate/UI/Icons/`; marca de Strava en `Assets/RowMate/UI/Brand/Strava/`; shaders en `Assets/RowMate/Art/Shaders/`; texturas generadas en `Assets/RowMate/Art/Textures/`.
- **Terceros y repositorio:** los archivos de Mixamo, del Asset Store y los opcionales CC0 **no se versionan** (sus licencias permiten usarlos en la app, no redistribuirlos sueltos). Van **solo** a `Assets/ThirdParty/`, ignorada por git salvo sus `.meta` (los `.meta` conservan el GUID que referencia el slot y los ajustes de importación; QA-P-01). Cada usuario descarga los archivos con los pasos de este documento. **Cuidado:** si abrís el proyecto en Unity **sin** los archivos descargados, Unity borra los `.meta` huérfanos; antes de copiar los archivos, recuperalos con `git checkout -- Assets/ThirdParty` (ARCHITECTURE §2, RA-17). Las fuentes OFL, los íconos Apache 2.0 y los botones de Strava sí se versionan, junto con su licencia o guía.

### 0.1 Prioridad para el usuario

Coincide con la tabla de prerrequisitos del [ROADMAP](../ROADMAP.md) §0.

| Prioridad | Qué descargar | Cuándo | Sin esto… |
|---|---|---|---|
| 1 | Fuentes Inter, Barlow Semi Condensed y JetBrains Mono (§3) | It. 2 | la UI usa la fuente por defecto de Unity |
| 2 | Íconos: fuente Material Symbols Rounded, recortada a los glifos de la app (§4) | It. 2 | íconos provisorios dibujados por código (§4.4): mismo tamaño y color, nunca un cuadrado vacío |
| 3 | Terrain Sample Asset Pack (§5.2) | It. 6a | texturas procedurales de terreno |
| 4 | Mixamo **Y Bot** (§2; la lista definitiva se confirma en el diseño de la It. 6a, P-09) | antes de la It. 7 | maniquí procedural |
| 5 | Mixamo personaje realista (§2.3) | It. 7 o 14 | Y Bot con mono pintado |
| 6 | Assets oficiales de Strava (§6) | It. 12 | botón propio con el color de Strava |
| Opcional | CC0 y NASA (§7), con OK del usuario (D-ART-08, gate de la It. 6a) | It. 6a–8 | procedurales |

---

## 1. Tabla de slots

Leyenda de "Recomendado": **Proc.** = se queda con el procedural propio (es la mejor opción gratis disponible); **Mixamo**, **Unity AS** (Asset Store gratis de Unity Technologies), **OFL**, **Apache**, **Strava**, **CC0/PD** (opcional, §7).

### 1.1 Remero y bote (It. 7)

| Id | Qué es | Placeholder procedural | Recomendado | Licencia |
|---|---|---|---|---|
| `rower.character` | Personaje Humanoid (malla + rig) | Maniquí `ProceduralRower` con **esqueleto `mixamorig:*` generado** (mismos nombres y proporciones que Y Bot), avatar Humanoid construido en ejecución con `AvatarBuilder` (ANIMATION_BIBLE §7.3), segmentos en cápsulas suavizadas, mono pintado por shader | Mixamo **Y Bot** (calibración) → personaje realista (§2.3) | Mixamo (Adobe) |
| `rower.outfit` | Paleta y materiales del mono, medias, gorra | Shader "RowMate/Fabric" con máscaras por altura y lado del cuerpo: base `#1565C0`, franjas `#00B4D8`, panel `#0D3B6E` | Proc. | propio |
| `rower.cap` | Gorra de remo (opcional) | Media esfera + visera procedural, `#0D3B6E` | Proc. | propio |
| `rower.clip.idleRef` | Clip de reposo sentado, solo referencia de dedos y respiración (opcional) | Pose de agarre propia (§ANIMATION_BIBLE 7.3) | Mixamo "Sitting Idle" (§2.4) | Mixamo |
| `boat.hull` | Casco del single (8,2 m) | **Loft procedural** de 24 secciones (ancho y calado de ART_BIBLE §7.8), cubierta, cockpit, bola de proa, aleta; material gelcoat con línea `#00B4D8` | Proc. (no hay un single realista gratis y verificable; el loft da un resultado limpio) | propio |
| `boat.rigger` | Ala de carbono con chumaceras | Tubos procedurales (perfil ovalado) + chumaceras de caja | Proc. | propio |
| `boat.seat` / `boat.stretcher` | Carro, rieles, pedalera con zapatillas | Procedural | Proc. | propio |
| `boat.oar` | Remo de *sculling* (par) | Caña cónica, puño, manguito, collarín, **pala tipo hacha con concavidad** (loft), cara `#00B4D8`, punta blanca; flexión por vertex shader | Proc. | propio |
| `boat.livery` | Colores del bote y remos (datos) | `ScriptableObject` con los colores de contrato | Proc. | propio |

### 1.2 Agua y cielo (It. 6a; agua avanzada en la 6b)

| Id | Qué es | Placeholder procedural | Recomendado | Licencia |
|---|---|---|---|---|
| `water.profile.lake` / `.river` / `.coast` / `.regatta` | Parámetros del agua por entorno (ART_BIBLE §7.7) | Datos | Proc. | propio |
| `water.normalDetail` | 2 mapas de normal tileables 512² | **Generados en el Editor** por un script propio (suma de ondas + ruido fBm, *tileable*) | Proc. | propio |
| `water.foam` | Textura de espuma 256² | Celdas de Voronoi + ruido, generada en el Editor | Proc. | propio |
| `sky.profile` | Paleta legacy + derivados por hora | Datos (tabla de audit §7.3 y ART_BIBLE §6.3) | Proc. | propio |
| `sky.clouds` | Capa de nubes 1 024×512 + 7 nubes héroe | Ruido fBm con *domain warp* horneado al iniciar | Proc. | propio |
| `sky.moon` | Albedo de la luna 512² | Ruido de cráteres + "mares" oscuros | Opcional: NASA CGI Moon Kit (§7) | PD |
| `sky.stars` | Campo de estrellas | Hash por dirección en el shader del cielo | Proc. | propio |

### 1.3 Terreno, vegetación y horizonte (It. 6a y 8)

| Id | Qué es | Placeholder procedural | Recomendado | Licencia |
|---|---|---|---|---|
| `terrain.layer.grass` / `.sand` / `.soil` / `.pebbles` / `.rock` / `.snow` | Capas de terreno de orilla (albedo + normal + máscara) | Color por ruido + normal por ruido, triplanar | **Unity AS: Terrain Sample Asset Pack** (§5) | Unity Asset Store EULA |
| `vegetation.grass` / `.fern` / `.bush` | Vegetación chica de orilla | Matas de tarjetas cruzadas con textura procedural | Unity AS: Terrain Sample Asset Pack | Unity Asset Store EULA |
| `vegetation.reeds` | Juncos (lago) | Hojas largas procedurales con viento | Proc. | propio |
| `vegetation.poplar` | Álamos de fondo (regata) | Columna de copas elipsoides + impostor | Proc. | propio |
| `horizon.lake` | 2 cordilleras (lejana con nieve, cercana boscosa) | Malla de perfil del legacy (48 tramos) extruida en 3D con ruido y nieve por altura | Proc. | propio |
| `horizon.river` | Skyline en bloques con ventanas nocturnas | Bloques procedurales con grilla de ventanas por shader (50 % encendidas fijas) | Proc. | propio |
| `horizon.coast` | Acantilado lejano | Perfil legacy extruido | Proc. | propio |
| `horizon.regatta` | Tribunas, torre, banderas y línea de árboles lejanas | Bloques procedurales | Proc. | propio |

### 1.4 Objetos de orilla (catálogo legacy de 15 tipos + agregados; It. 6a lago, It. 8 resto)

Todos tienen **3 LOD + impostor**, apoyan en terreno o muro (nada flota) y respetan medidas y colores de audit §7.6 como referencia.

| Id | Placeholder procedural (es el recomendado salvo que se indique) | Texturas sugeridas |
|---|---|---|
| `prop.pine` | Conífera de 3 a 6 pisos de "faldas" con ruido de borde, tronco cónico, 3 paletas de verde del legacy; LOD 1 200 / 400 / 120 tris / billboard | corteza procedural |
| `prop.cabin` | Chalet de madera con techo a dos aguas, alero, puerta, ventana emisiva de noche | madera procedural o CC0 |
| `prop.pier` | Muelle de tablones con pilotes y bitas | madera |
| `prop.building` | Fachada de 6 m, 3–6 pisos, cornisa, grilla de ventanas por shader (atlas), 3 tonos del legacy | ladrillo/piedra procedural o CC0 |
| `prop.boathouse` | Club de remo con portón, franja `#1565C0`, rampa al agua | — |
| `prop.bridge` | Puente de piedra con arco de 17 m, sillares por shader, pilares, parapeto | `terrain.layer.rock` |
| `prop.laneBuoy` | Boya esférica (lago/río) o Albano (regata), plástico brillante | — |
| `prop.lamp` | Farol de hierro con globo emisivo y halo | — |
| `prop.cliff` | Acantilado de capas: caja desplazada por ruido con estratos, triplanar | `terrain.layer.rock` |
| `prop.lighthouse` | Torre cilíndrica con 3 franjas rojas, linterna, haz giratorio de noche | — |
| `prop.beachUmbrella` | Sombrilla de 8 gajos con tela que flamea, toalla | — |
| `prop.grandstand` | Tribuna de 4 gradas con techo; asientos por shader | — |
| `prop.finishTower` | Torre de 10 m con banda y **reloj que marca la hora real** | — |
| `prop.flag` | Mástil + paño triangular con tela por vertex shader | — |
| `prop.distanceMarker` | Tablero blanco con número en Barlow SC 700 (texto real, no textura) | — |
| `prop.quayWall` (nuevo) | Muro de contención de piedra con remate, en tramos de 50 m | `terrain.layer.rock` |
| `prop.rock` (nuevo) | Rocas sueltas de orilla (5 variantes por ruido) | `terrain.layer.rock` |
| `prop.crowd` (nuevo) | Público: impostores instanciados, 3 variantes de ropa, 2–3 cuadros de pose | proc. |
| `prop.seagull` (nuevo) | Gaviota low-poly (≤ 300 tris) con aleteo por vertex shader | proc. |
| `prop.boatRack` (nuevo, opcional) | Racks con botes de colores frente a los clubes | proc. |

### 1.5 Efectos (It. 7)

| Id | Qué es | Placeholder |
|---|---|---|
| `fx.splash` | Gotas y corona del catch | ParticleSystem + textura de gota suave generada |
| `fx.drips` | Hilo de gotas de la recuperación | ParticleSystem |
| `fx.ripple` | Anillos y remolinos para la textura de interacción | quads con shader propio |
| `fx.lampGlow` / `fx.lighthouseBeam` | Halos y haz nocturnos | billboards y cono aditivo |

### 1.6 UI y marca

| Id | Qué es | Recomendado | Licencia | It. |
|---|---|---|---|---|
| `ui.font.text` | Inter | [Google Fonts](https://fonts.google.com/specimen/Inter) · [GitHub](https://github.com/rsms/inter) | OFL 1.1 | 2 |
| `ui.font.numeric` | **RowMate Numeric SC** 600, 700 y 800: Barlow Semi Condensed con sus cifras tabulares (`tnum`) congeladas en el `cmap` y la familia renombrada (OFL 1.1). Dibuja todo número (`.rm-type-num-*`, también `num.stat`). Los `.ttf` de Barlow quedan como entrada de la receta (§3, punto 7). Desde la It. 4a | [Google Fonts](https://fonts.google.com/specimen/Barlow+Semi+Condensed) · [GitHub](https://github.com/jpt/barlow) | OFL 1.1 | 2 (Barlow) · 4a (la derivada) |
| `ui.font.mono` | JetBrains Mono | [Google Fonts](https://fonts.google.com/specimen/JetBrains+Mono) | OFL 1.1 | 10 |
| `ui.font.emoji` | Noto Emoji **monocroma**, instancia estática Medium (500, el peso de los íconos): último respaldo del texto, para emojis en nombres y descripciones (P-09). Se dibuja en el color del texto, del alto de la línea. En el repo: `Assets/RowMate/UI/Fonts/NotoEmoji/NotoEmoji-Medium.ttf` con su `OFL.txt` (§3, punto 6). Sin el slot, Windows dibuja el emoji de color del sistema y otras plataformas `□`; el resto del texto no se corre | [Google Fonts](https://fonts.google.com/noto/specimen/Noto+Emoji) | OFL 1.1 | 3 |
| `ui.icons` | Fuente de íconos "RowMate Icons": Material Symbols Rounded recortada a los glifos de la app (§4). **Glifos de fuente, no SVG** (QA-P-09, cerrado al iniciar la It. 2). Placeholder: íconos provisorios dibujados por código (§4.4) | [GitHub (versión fijada)](https://github.com/google/material-design-icons/tree/10ecc14790612ca8fd164e3f4ffe9c16fc50d5b7/variablefont) · [catálogo](https://fonts.google.com/icons) | Apache 2.0 | 2 |
| `ui.thumbnail.<entorno>` | Miniaturas del selector | **Horneadas en el Editor** por `EnvironmentThumbnailBaker` (ARCHITECTURE §14.5) desde la escena real, en `Assets/RowMate/Art/Textures/Thumbnails/`, a las 12:00 (paridad F-SCN-22). Opcional, si se aprueba en el gate de la It. 8: 6, 12, 18 y 22 h (ART_BIBLE §7.6) | propio | 8 |
| `brand.strava` | Botón "Connect with Strava" y logo "Powered by Strava" | [Guía de marca de Strava](https://developers.strava.com/guidelines/), en **PNG @2x** | Guía de marca de Strava (usar sin modificar) | 12 |
| `brand.appIcon` / `brand.splash` | Ícono y arranque "RowMate" | lo diseña `rowmate-designer`: original vectorial fuera de Unity, exportado a PNG en los tamaños de cada plataforma (en Unity no entra ningún SVG) | propio | 14 |
| `audio.*` | Reservado (BKL-03, fuera de paridad) | — | — | — |

---

## 2. Mixamo (remero)

**Licencia:** los personajes y animaciones de Mixamo se pueden usar sin regalías en proyectos personales, comerciales y sin fines de lucro; no se pueden redistribuir como archivos sueltos ([preguntas frecuentes de Mixamo](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html)). Por eso van a `Assets/ThirdParty/Mixamo/`, fuera del repositorio (§0).

Esta es la lista preliminar. La definitiva (archivos exactos y nombres) la confirma el Designer en el diseño de la It. 6a (P-09, ROADMAP §6), para que el usuario descargue antes de la It. 7.

### 2.1 Cuenta

1. Entrá a [www.mixamo.com](https://www.mixamo.com) y tocá **Log in**. Usá (o creá gratis) una cuenta de Adobe.

### 2.2 Personaje de calibración: Y Bot (obligatorio para It. 7)

Se usa para calibrar el IK y `RiggingFit`: rig limpio, proporciones neutras, pesado perfecto.

1. Pestaña **Characters** → buscá **`Y Bot`** → hacé clic en la tarjeta (aparece en el visor en T-pose).
2. Botón **DOWNLOAD** (arriba a la derecha). En el diálogo:
   - **Format:** `FBX for Unity(.fbx)`
   - **Pose:** `T-pose`
3. **DOWNLOAD**. Guardá el archivo como `YBot.fbx`.
4. (Opcional, remera mujer) Repetí con **`X Bot`** → `XBot.fbx`.

### 2.3 Personaje realista (It. 7 si está listo; si no, It. 14)

No pude verificar desde acá el vestuario de cada personaje (Mixamo requiere sesión). Elegilo en la vista previa con esta **lista de control**; el primero que la cumpla gana:

| Criterio | Por qué |
|---|---|
| Ropa **ajustada y deportiva** (musculosa o top + calza o short, o enterito) | El mono se pinta encima; ropa holgada atraviesa piernas y remos |
| Sin abrigo, capucha, falda, capa, mochila, cinturón con objetos ni accesorios grandes | Se cruzan con el tronco al inclinarse ±28° |
| Pelo corto o recogido | El pelo suelto atraviesa hombros en el finish |
| Manos con los 5 dedos riggeados | El agarre del puño usa los dedos |
| Proporciones adultas realistas (no estilizado/caricatura) | Look EXR y `RiggingFit` |
| Sin texturas de "tela suelta" en piernas | Las piernas se ven completas en el catch |

**Primer candidato a revisar: `Kaya`.** Si no cumple, recorrer la lista de personajes con el buscador vacío y aplicar la tabla. Descarga con **los mismos ajustes que Y Bot** (§2.2) y nombre `RowerRealistic.fbx`. Si ninguno cumple, se queda Y Bot con el mono pintado (`rower.outfit`): se ve como atleta con traje integral.

Alternativa opcional que requiere OK del usuario: generar un atleta con **MakeHuman** (software libre; sus modelos exportados son CC0) con ropa ajustada y subirlo al *auto-rigger* de Mixamo (botón **UPLOAD CHARACTER**), que devuelve el mismo rig.

### 2.4 Clip opcional de referencia (no se usa para remar)

1. Con el personaje elegido, pestaña **Animations** → buscá **`Sitting Idle`** (si no aparece con ese nombre, buscá "sitting idle" y elegí uno con el torso quieto y las manos sobre los muslos).
2. **DOWNLOAD** con:
   - **Format:** `FBX for Unity(.fbx)`
   - **Skin:** `Without Skin`
   - **Frames per Second:** `30`
   - **Keyframe Reduction:** `none`
3. Guardalo como `RowerRealistic@SittingIdle.fbx`.

### 2.5 Importar en Unity (cada FBX de personaje)

1. Copiá los FBX a `Assets/ThirdParty/Mixamo/<Personaje>/` (p. ej. `Assets/ThirdParty/Mixamo/YBot/YBot.fbx`).
2. Seleccioná el FBX → **Inspector**:

| Pestaña | Opción | Valor |
|---|---|---|
| Model | Scale Factor | `1` |
| | Convert Units | ✓ |
| | Import BlendShapes | ✓ |
| | Import Visibility / Cameras / Lights | ✗ |
| | Mesh Compression | `Off` |
| | Read/Write | ✗ |
| | Optimize Mesh | `Everything` |
| | Normals / Tangents | `Import` / `Calculate Mikktspace` |
| Rig | Animation Type | **`Humanoid`** |
| | Avatar Definition | **`Create From This Model`** |
| | Skin Weights | `Standard (4 Bones)` |
| | **Optimize Game Objects** | **✗ (desactivado: Animation Rigging necesita los huesos como transforms)** |
| Animation | Import Animation | ✗ |
| Materials | Material Creation Mode / Location | `Standard` / `Use Embedded Materials`; después **Extract Textures…** → carpeta `Textures/` y **Extract Materials…** → carpeta `Materials/` |

3. En **Rig** tocá **Apply** y después **Configure…**: todos los huesos obligatorios tienen que estar en verde, incluidos los dedos (pestaña *Left Hand* / *Right Hand*). Menú **Pose → Enforce T-Pose** → **Apply** → **Done**.
4. Texturas extraídas: en cada una, override de **Android** e **iOS**: Max Size `1024`, Format `ASTC 6x6`; **Default/Standalone**: Max Size `2048`. Las de normal: Texture Type `Normal map`.
5. Materiales: **Window → Rendering → Render Pipeline Converter** → `Built-in to URP` → marcá **Material Upgrade** → **Initialize And Convert**. (El slot después los reemplaza por los materiales RowMate/Skin, RowMate/Fabric y RowMate/Hair usando estas texturas.)
6. Asignación: abrí `Assets/RowMate/Slots/rower.character.asset` (tipo `RowerCharacterSlot`) y arrastrá el FBX al campo **Asset**. Al dar Play, el slot valida (Humanoid, huesos de dedos, altura 1,55–2,05 m, ≤ 30 k triángulos en LOD0); si algo falla, vuelve al maniquí y lo avisa en la consola. El slot le pone al personaje el controlador de pose `RowerPose` (ANIMATION_BIBLE §7.3): no hace falta configurar el `Animator` a mano.

### 2.6 Importar el clip opcional

Seleccioná `RowerRealistic@SittingIdle.fbx` → **Rig**: `Humanoid`, Avatar Definition **`Copy From Other Avatar`** → elegí el avatar del personaje → Apply. **Animation**: Loop Time ✓; Root Transform Rotation, Position (Y) y Position (XZ): **Bake Into Pose** ✓ con Based Upon `Original`. Asignalo a `rower.clip.idleRef`.

---

## 3. Fuentes (OFL 1.1)

1. Descargá:
   - **Inter:** [Google Fonts](https://fonts.google.com/specimen/Inter) → **Get font** → **Download all**. Usá los `.ttf` **estáticos** Regular, Medium, SemiBold y Bold (si el ZIP trae tamaños ópticos, los de 18 pt para texto y los de 28 pt para títulos).
   - **Barlow Semi Condensed:** [Google Fonts](https://fonts.google.com/specimen/Barlow+Semi+Condensed) → SemiBold, Bold y ExtraBold.
   - **JetBrains Mono:** [Google Fonts](https://fonts.google.com/specimen/JetBrains+Mono) → Regular.
2. Copialos a `Assets/RowMate/UI/Fonts/<Familia>/` **junto con su `OFL.txt`** (la licencia pide acompañar el aviso; estas sí se versionan).
3. En Unity, clic derecho en cada `.ttf` → **Create → Text → Font Asset** (en algunas versiones el menú se llama **Create → TextCore → Font Asset**). Ajustes del Font Asset:

| Uso | Atlas Population Mode | Atlas | Render Mode | Sampling Point Size | Padding |
|---|---|---|---|---|---|
| Inter (texto) | `Dynamic` | 1024×1024 | `SDFAA` | 48 | 10 % |
| RowMate Numeric SC (números; hasta la It. 3, Barlow SC) | `Dynamic` (o `Static` con el set de abajo) | 1024×1024 | `SDFAA` | 90 | 10 % |

   Set estático para números: `0123456789:.,-–—/+%▲▼× mWkcalBPMSPMSplitRep#·`. Los `FontAsset` de la derivada los crea el bootstrapper (WP-B0 de la It. 4a), vacíos y dinámicos como los demás.
4. En el *Text Settings* de los PanelSettings: fuente por defecto Inter Regular. Los íconos **no** dependen del *fallback*: cada ícono usa la clase USS `.rm-icon`, que fija la fuente de íconos (§4).
   - No uses las Inter que trae el Editor de Unity (`Editor/Data/Resources/Fonts/` de la instalación): son recursos del Editor, no entran en el build y no vienen con su `OFL.txt`. Siempre la copia de Google Fonts.
   - Sin las fuentes descargadas, la UI usa la fuente por defecto del tema de Unity y los tamaños de `Tokens.cs` no cambian ([00-deuda-diseno §4.3](../iterations/02-dispositivo-simulador/00-deuda-diseno.md)).
5. **Kerning (It. 3, R3-06 decidido; [03-rutinas/04-design-notes §4.16](../iterations/03-rutinas/04-design-notes.md)):** las 7 caras de texto (`FontAssetPaths.TextFaces`: Inter 400 a 700 y Barlow SC 600 a 800) van con **`getFontFeatures = false`**: el kerning es el de la tabla que quedó versionada en cada `FontAsset` y TextCore no busca pares nuevos mientras dibuja, así el texto queda en el mismo lugar en cualquier orden y en una corrida parcial del arnés (el atlas dinámico puede variar en 1 el borde de un glifo: medido en la It. 3, 04-design-notes §16.5). Lo deja el bootstrapper dentro de `GeneratedAssetGuard.AllowWrites()` y lo controla `ItThreeToolingTests` (`m_GetFontFeatures: 0`). Los íconos y Noto Emoji quedan como los crea Unity. **Desde la It. 4a (K2, DD4a-06):** la tabla de cada cara que dibuja texto (Inter 400 a 700 y RowMate Numeric SC 700) es exactamente la lista de `Assets/RowMate/UI/Fonts/Kerning/<cara>.pairs.json`: **todos** los pares GPOS `kern` distintos de 0 entre los caracteres del juego fijo (el catálogo es/en, el ASCII imprimible, `áéíóúüñÁÉÍÓÚÜÑ¿¡` y `×±·°`), y solo esos. La escribe `tools/fonts/kern_pairs.py` (fontTools 4.68.0 fijado, sin red; [tools/fonts/README.md](../../tools/fonts/README.md)) y el bootstrapper la vuelca en `m_GlyphPairAdjustmentRecords` (`XAdvance` = unidades / `unitsPerEm` × tamaño de muestreo). Se vuelve a correr cuando cambia el catálogo (K3). La derivada 600 y 800 no llevan tabla: solo las dibuja `TabularNumber`, celda por celda (K4). Un carácter fuera del juego va sin kerning (K6).
6. **Noto Emoji (It. 3, slot `ui.font.emoji`):** de [Google Fonts](https://fonts.google.com/noto/specimen/Noto+Emoji) → **Get font** → **Download all**; del ZIP, el `.ttf` **estático** `NotoEmoji-Medium.ttf` (si el ZIP trae solo la variable: `uvx --from fonttools==4.67.0 fonttools varLib.instancer NotoEmoji[wght].ttf wght=500 -o NotoEmoji-Medium.ttf`) y su `OFL.txt`, a `Assets/RowMate/UI/Fonts/NotoEmoji/`. El `FontAsset` dinámico (`NotoEmoji-Medium-SDF`, en `FontAssetPaths.All`) lo crea el bootstrapper, protegido por `GeneratedAssetGuard` como los otros 9. Orden de respaldos del panel (lo arma `RuntimePanelFactory`, la única fábrica: app, `UiTestHost` y arnés): Inter (por la clase) → RowMate Icons contorno → Noto Emoji. Visto bueno del Designer en la It. 3: con la fuente, `Remo 🚣 ñ` sale con el glifo monocromo en el color del texto, en la línea y sin correr la `ñ` (sonda S-B3a, corrida 1).
7. **RowMate Numeric SC (It. 4a, DD4a-07; slot `ui.font.numeric`).** Barlow Semi Condensed **sí trae `tnum`**, con cifras tabulares dibujadas para eso (`zero.tf` a `nine.tf`: un solo avance de 0,507 / 0,521 / 0,535 em en 600 / 700 / 800 y un `1` con pie), pero UI Toolkit no aplica features de OpenType. La derivada las congela en el `cmap`:
   1. Desde la raíz del repo, sin red: `uvx --offline --from fonttools==4.68.0 python tools/fonts/freeze_tnum.py`. Lee los `.ttf` de `UI/Fonts/BarlowSemiCondensed/` y escribe `UI/Fonts/RowMateNumericSC/RowMateNumericSC-{SemiBold,Bold,ExtraBold}.ttf` con su `OFL.txt` (el de Barlow, con la nota de la versión modificada). Las diez cifras apuntan a los `*.tf`; los nombres 1, 3, 4, 6, 16 y 21 pasan a "RowMate Numeric SC" (la OFL exige **otro nombre** para una versión modificada; Barlow no declara un nombre reservado y se renombra igual). Ninguna otra tabla cambia y la fecha es la de Barlow: dos corridas dan los mismos bytes. Equivale a `pyftfeatfreeze -f tnum -R "Barlow Semi Condensed/RowMate Numeric SC"` (opentype-feature-freezer, que no hace falta instalar).
   2. Después, `tools/fonts/kern_pairs.py` (punto 5): la lista K2 de la derivada 700 se calcula sobre la derivada.
   3. Integración (WP-B0, 02-tech-design de la It. 4a §6.14): los tres `FontAsset` dinámicos de la derivada (muestreo 90, como Barlow), el rol numérico de `FontAssetPaths` apuntando a ellos y `Typography.uss` regenerado con `tools/tokens`. `TabularNumber` no cambia: sus celdas pasan a medir todas lo mismo. Los `FontAsset` de Barlow salen de `FontAssetPaths.All`; los `.ttf` de Barlow quedan como entrada de esta receta.
   4. **Listas y ejes** (Historial, It. 9): si hace falta un derivado tabular de Inter, la misma receta con fontTools (`tnum` congelado y otra familia, "RowMate Numeric"); hasta entonces no se genera.

---

## 4. Íconos: glifos de Material Symbols Rounded (Apache 2.0)

**Decisión (cerrada al iniciar la It. 2; detalle y comparación en [00-deuda-diseno §3](../iterations/02-dispositivo-simulador/00-deuda-diseno.md)): los íconos son glifos de una fuente, no SVG ni PNG.** Coincide con ARCHITECTURE §1.2 y ADR 001, que ya eligen glifos: no queda diferencia entre documentos.

- El paquete `com.unity.vectorgraphics` no se usa: en el registro de Unity solo existe como *preview* (última: `3.0.0-preview.7`) y no está en el manifest (ARCHITECTURE §1.2).
- El Editor 6000.3.27f1 **sí** trae un módulo integrado, `com.unity.modules.vectorgraphics` 1.0.0, que importa SVG como `UnityEngine.UIElements.VectorImage` sin paquete (verificado en un proyecto de prueba, 00-deuda-diseno §7). **Tampoco se agrega al manifest**: la decisión no depende de que exista, sino de lo que sigue.
- Un glifo SDF se ve nítido en cualquier tamaño y con cualquier `hudScale`. Se tiñe con `color` de USS, así el color semántico sale de los tokens, y el fundido de color de 160 ms (ANIMATION_BIBLE §2.1) es una transición de USS común.
- Todos los íconos comparten un solo atlas, quedan alineados con el texto y cada uno es un `Label`, que cuenta como un solo elemento en el tope del HUD.
- No hace falta ningún paquete ni módulo, la fuente recortada pesa 9 KB (contorno) + 3 KB (relleno) y se versiona, y los codepoints se verifican con dotnet antes de tener Editor. Con SVG serían 49 archivos sueltos, exportados a mano uno por uno, sin forma de verificarlos con dotnet.
- SVG solo entraría para un ícono **multicolor**, que en la paridad no existe, con aprobación de QA. El botón de Strava sigue en PNG (§6) y el ícono de la app no entra a Unity como SVG.

### 4.1 Preparar la fuente (una vez, It. 2)

Versión fijada: commit [`10ecc14`](https://github.com/google/material-design-icons/tree/10ecc14790612ca8fd164e3f4ffe9c16fc50d5b7/variablefont) del repositorio oficial (2026-10-23). Los codepoints de §4.3 salen de **ese** archivo `.codepoints`: entre versiones de la fuente pueden cambiar.

1. Instalá Python 3 y ejecutá `pip install fonttools` (si ya instalaste `opentype-feature-freezer` en el §3, fonttools ya está). **Sin Python instalado:** si tenés [`uv`](https://docs.astral.sh/uv/), anteponé `uvx --from fonttools==4.67.0` a cada comando (`uvx --from fonttools==4.67.0 fonttools varLib.instancer …` y `uvx --from fonttools==4.67.0 pyftsubset …`): `uv` baja Python y fonttools solo, sin instalar nada en el sistema. **Fijá la versión**: sin `==4.67.0`, `uvx` toma la última (4.68.0 en octubre de 2026). Así se verificó esta receta (fonttools 4.67.0, 00-deuda-diseno §7; en la It. 3, con los 44 codepoints: 03-rutinas/04-design-notes §14.6).
2. Descargá los 3 archivos de la versión fijada:
   - [`MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf`](https://raw.githubusercontent.com/google/material-design-icons/10ecc14790612ca8fd164e3f4ffe9c16fc50d5b7/variablefont/MaterialSymbolsRounded%5BFILL%2CGRAD%2Copsz%2Cwght%5D.ttf) (15 MB, fuente variable)
   - [`MaterialSymbolsRounded[FILL,GRAD,opsz,wght].codepoints`](https://raw.githubusercontent.com/google/material-design-icons/10ecc14790612ca8fd164e3f4ffe9c16fc50d5b7/variablefont/MaterialSymbolsRounded%5BFILL%2CGRAD%2Copsz%2Cwght%5D.codepoints)
   - [`LICENSE`](https://raw.githubusercontent.com/google/material-design-icons/10ecc14790612ca8fd164e3f4ffe9c16fc50d5b7/LICENSE) (Apache 2.0)
3. Generá dos instancias **estáticas** con Weight 500, Grade 0 y Optical size 24: una sin relleno y otra con relleno. El *Font Asset* de Unity no elige ejes de una fuente variable: toma la instancia por defecto, que es de Weight 400.

   ```
   fonttools varLib.instancer "MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf" FILL=0 GRAD=0 opsz=24 wght=500 -o msr-outline.ttf
   fonttools varLib.instancer "MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf" FILL=1 GRAD=0 opsz=24 wght=500 -o msr-filled.ttf
   ```

4. Recortá cada una a sus glifos (§4.3). Sin ligaduras: la app usa los codepoints.

   ```
   pyftsubset msr-outline.ttf --unicodes="U+E921,U+EB43,U+E80E,U+E1A2,U+EB3B,U+E040,U+E425,U+E41C,U+EA0B,U+E9E4,U+EF55,U+EFD6,U+E8B3,U+E8E5,U+E1A7,U+E1A8,U+E1A9,U+E60F,U+E868,U+E87E,U+EAC2,U+E5D5,U+F0D3,U+E2C3,U+E2C0,U+E16C,U+E250,U+E16F,U+F083,U+F8B6,U+E5CC,U+E5CF,U+E5CE,U+E145,U+E15B,U+E5CD,U+F097,U+E92E,U+F8CD,U+E564,U+E5C7,U+E5C5,U+E5C4,U+E5D4" --layout-features="" --no-hinting --output-file=RowMateIcons-Outline.ttf
   pyftsubset msr-filled.ttf --unicodes="U+E87E,U+E037,U+E034,U+F0BE,U+E2BF,U+E425,U+F0D3" --layout-features="" --no-hinting --output-file=RowMateIcons-Filled.ttf
   ```

   Resultado esperado (verificado): `RowMateIcons-Outline.ttf` ≈ 9 KB con 45 glifos (44 + `.notdef`; 9 304 B desde la It. 3, que sumó `arrow_back` y `more_vert` con los 42 de antes idénticos) y `RowMateIcons-Filled.ttf` ≈ 3 KB con 8 (7 + `.notdef`), los dos con Weight 500, 960 unidades por em y avance de 960 en todos los glifos. Si un codepoint no aparece en el resultado, la versión descargada no es la fijada.

5. Copiá a `Assets/RowMate/UI/Icons/` (se versionan): `RowMateIcons-Outline.ttf`, `RowMateIcons-Filled.ttf`, el `LICENSE`, el `.codepoints` original y un `NOTICE.md` que diga: *"Derivado de Material Symbols Rounded (Google, Apache License 2.0), commit 10ecc14. Modificaciones: instancias estáticas FILL 0 y 1, wght 500, GRAD 0, opsz 24, y recorte a los glifos de RowMate."* Apache 2.0 §4 pide avisar qué se cambió. La fuente variable de 15 MB **no** se versiona.

### 4.2 Importar y usar (It. 2, con Editor)

1. Clic derecho en cada `.ttf` → **Create → Text → Font Asset** (o **Create → TextCore → Font Asset**). Ajustes: Atlas Population Mode `Dynamic` (la fuente ya está recortada: no puede entrar ningún glifo ajeno), Render Mode `SDFAA`, Sampling Point Size 64, Padding 10 %. Atlas: **512×512** para el contorno (los 42 glifos de la It. 2 ocupaban alrededor del 60 %; los 44 de la It. 3 entran con lugar para unos 18 más) y **256×256** para el relleno (7 glifos). Verificado con esos ajustes en 6000.3.27f1: entran los 42 y los 7 codepoints y el atlas SDF sale bien (00-deuda-diseno §7).
2. USS: clase `.rm-icon` con `-unity-font-definition` apuntando al Font Asset de contorno, `-unity-text-align: middle-center`, `white-space: nowrap`, sin `letter-spacing`; el tamaño del ícono es su `font-size` (14, 16, 18, 20, 22, 24, 32, 56 o 64 dp, ART_BIBLE §3) y la caja del elemento es un cuadrado de ese lado. La variante `.rm-icon--filled` cambia a la fuente rellena. Métricas de la fuente: 960 unidades por em, ascendente 1 056 y descendente 96, o sea una línea de 1,2 em **simétrica** respecto del centro del ícono (el dibujo va de 0 a 960): se centra en vertical y en horizontal sin ajustes a mano.
3. El texto del `Label` es el codepoint, tomado de constantes (por ejemplo `Icons.Rowing = "\uE921"`). Propuesta al Architect, junto con el generador de tokens (QA-P-05): `Core/Design/Icons.cs`, generado desde la tabla de §4.3, con un test de dotnet que verifique cada nombre y su codepoint contra el `.codepoints` versionado.
4. Íconos que cambian de glifo o de relleno (pestaña seleccionada): se cambia la clase y el glifo en el mismo cuadro, con el fundido de color de ANIMATION_BIBLE §2.1. El corazón del pulsómetro es relleno siempre (como el legacy) y solo cambia de opacidad (`@40 %` sin conexión).
5. Ningún ícono muestra nunca el cuadrado de "glifo faltante": si falta la fuente o un codepoint, se dibuja el ícono provisorio (§4.4).

**Respaldo de último recurso** (solo si la captura del primer open con las fuentes muestra glifos mal dibujados, cosa que la prueba del atlas no anticipa): PNG blancos exportados desde [fonts.google.com/icons](https://fonts.google.com/icons) con los mismos ajustes (Rounded, 500, Grade 0, Optical size 24), a 96 px para los íconos de hasta 32 dp y a 192 px para los de 56-64 dp. Van como `background-image` con mipmaps y se tiñen con `-unity-background-image-tint-color`. **SVG** no es respaldo: ver la decisión al principio de §4.

### 4.3 Lista de íconos y codepoints

Nombres del catálogo de Material Symbols; codepoints del `.codepoints` del commit `10ecc14`. "Legacy" es el `Icons.*` de Flutter que reemplaza (los `*_outline`/`*_border` del legacy son el mismo glifo sin relleno).

| Ícono | Codepoint | Fuente | Uso | Legacy |
|---|---|---|---|---|
| `rowing` | `E921` | contorno | remo: Dispositivo, barra libre, rutinas, sesiones | `rowing` |
| `fitness_center` | `EB43` | contorno | paso de trabajo [CONTRATO]; pestaña Rutinas | `fitness_center` |
| `whatshot` | `E80E` | contorno | calentamiento [CONTRATO] | `whatshot` |
| `pause_circle` | `E1A2` | contorno | descanso [CONTRATO] | `pause_circle_outline` |
| `ac_unit` | `EB3B` | contorno | enfriamiento [CONTRATO] | `ac_unit` |
| `repeat` | `E040` | contorno | serie (editor) | `repeat` |
| `timer` | `E425` | contorno y relleno | split del tablero de Dispositivo; tiempo; pestaña Workout (relleno = seleccionada) | `timer_outlined` / `timer` |
| `straighten` | `E41C` | contorno | distancia | `straighten` |
| `bolt` | `EA0B` | contorno | vatios | `bolt` |
| `speed` | `E9E4` | contorno | SPM del tablero (el split del tablero usa `timer`, como el legacy: `device_screen.dart:253-254`) | `speed` |
| `local_fire_department` | `EF55` | contorno | kcal | `local_fire_department` |
| `schedule` | `EFD6` | contorno | reloj de pared, parciales | `schedule` |
| `history` | `E8B3` | contorno | pestaña Historial, series anteriores | `history` |
| `trending_up` | `E8E5` | contorno | progresiones (reemplaza el emoji 📈, Q-10) | — |
| `bluetooth` | `E1A7` | contorno | pestaña Dispositivo (no seleccionada) | `bluetooth` |
| `bluetooth_connected` | `E1A8` | contorno | pestaña Dispositivo (seleccionada), conectado | `bluetooth_connected` |
| `bluetooth_disabled` | `E1A9` | contorno | sin conexión, Bluetooth apagado | `bluetooth_disabled` |
| `bluetooth_searching` | `E60F` | contorno | buscando | `bluetooth_searching` |
| `bug_report` | `E868` | contorno | diagnóstico BLE | `bug_report` |
| `favorite` | `E87E` | contorno y relleno | pulso. Relleno: encabezado del pulsómetro y tablero, `@40 %` sin conexión (como el legacy, `heart_rate_card.dart:39`). Contorno: filas de pulsómetros encontrados (`:138`) | `favorite` / `favorite_border` |
| `heart_broken` | `EAC2` | contorno | panel del simulador: caída del pulsómetro | `heart_broken` |
| `refresh` | `E5D5` | contorno | reintentar (pulsómetro) | `refresh` |
| `person` | `F0D3` | contorno y relleno | pestaña Perfil (relleno = seleccionada; la pestaña aparece en la It. 12-13, con Strava configurado) | `person_outline` / `person` |
| `cloud_upload` | `E2C3` | contorno | subir a Strava | `cloud_upload` / `cloud_upload_outlined` |
| `cloud_done` | `E2BF` | relleno | sesión sincronizada con Strava | `cloud_done` |
| `cloud_download` | `E2C0` | contorno | importar de Strava | `cloud_download` |
| `delete_sweep` | `E16C` | contorno | limpiar sesiones de Strava | `delete_sweep` |
| `link` | `E250` | contorno | conectar Strava | `link` |
| `link_off` | `E16F` | contorno | panel del simulador: simular desconexión | `link_off` |
| `warning` | `F083` | contorno | remo no conectado | `warning_amber` |
| `error` | `F8B6` | contorno | errores en línea y de pantalla (ART_BIBLE §3.10) | — |
| `play_arrow` | `E037` | relleno | iniciar, reanudar | `play_arrow` |
| `pause` | `E034` | relleno | pausa | `pause` |
| `check_circle` | `F0BE` | relleno | completado, conectado | `check_circle` |
| `chevron_right` | `E5CC` | contorno | filas que abren detalle | `chevron_right` |
| `expand_more` / `expand_less` | `E5CF` / `E5CE` | contorno | paneles de series | `expand_more` / `expand_less` |
| `add` / `remove` | `E145` / `E15B` | contorno | agregar; ± del simulador | `add` / `remove` |
| `close` | `E5CD` | contorno | cerrar panel o diálogo | `close` |
| `edit` / `delete` | `F097` / `E92E` | contorno | editar y borrar (editor, rutinas) | `edit` / `delete_outline` |
| `build` | `F8CD` | contorno | botón del simulador | `build` |
| `landscape` | `E564` | contorno | sección "Escenario" | — |
| `arrow_drop_up` / `arrow_drop_down` | `E5C7` / `E5C5` | contorno | flecha de fuera de objetivo (D-ART-01) | — |
| `arrow_back` | `E5C4` | contorno | volver del editor de rutinas (It. 3; a 4 dp del borde, 24 `text.primary`) | `arrow_back` (del `AppBar`) |
| `more_vert` | `E5D4` | contorno | menú `⋮` de la tarjeta de rutina (It. 3; 24 `text.secondary`) | `more_vert` |

Total: **44** glifos en la fuente de contorno (42 de la It. 2 + `arrow_back` y `more_vert` de la It. 3, verificados contra el mismo `.codepoints`: `e5c4`, `e5d4`) y 7 en la rellena. Agregar un ícono = sumarlo a esta tabla, repetir el §4.1 paso 4, regenerar las constantes y sumar su receta provisoria (§4.4). Los 42 + 7 codepoints se verificaron contra el `.codepoints` del commit `10ecc14` al iniciar la It. 2: coinciden todos. Los que usa la It. 2, con tamaño, color y lugar: [00-deuda-diseno §3.5](../iterations/02-dispositivo-simulador/00-deuda-diseno.md).

### 4.4 Íconos provisorios (sin la fuente o sin un codepoint)

Placeholder del slot `ui.icons`. Regla: **mismo elemento, misma caja, mismo color**. La app se ve "a medio vestir" pero nunca rota, y el layout no se mueve cuando llega la fuente.

- **Cuándo.** Si el slot `ui.icons` no resuelve (falta el Font Asset), se usan para todos los íconos. Si falta un codepoint en el Font Asset, solo para ese. Un solo aviso `[SLOT] ui.icons` por sesión (§0), no uno por ícono.
- **Dibujo.** Líneas sobre la grilla de 24 × 24 de Material Symbols (2 de margen), escaladas al `font-size` del ícono. Trazo de 2,25 unidades (equivale al peso 500 de la fuente con *optical size* 24), extremos y uniones redondeados, como Rounded. Un relleno es **un solo contorno cerrado**: dos formas encimadas se verían más claras donde se cruzan cuando el color tiene alfa (`text.secondary` es `@70 %`, el corazón sin conexión `@40 %`). Por lo mismo, las recetas evitan los trazos encimados, salvo los cruces propios del dibujo (la runa de Bluetooth, la cruz de `add` y `close`).
- **Color y movimiento.** El mismo `color` de USS que tendría el glifo, con sus opacidades y su fundido de 160 ms. Las animaciones (latido, escala de la pestaña) son del elemento, así que no cambian.
- **Sin receta.** Un cuadrado redondeado de 14 × 14 (de 5 a 19), radio 3, mismo trazo y color. Nunca el "□" de glifo faltante ni un texto adentro de la caja.
- **Implementación** (propuesta al Architect, sin código). Las recetas son **datos** en Core: primitivas con números, testeables con dotnet (cada ícono de la iteración tiene receta y sus coordenadas caen en 0-24). Presentation dibuja cada receta una vez con un `Painter2D` suelto y la guarda como `VectorImage` con `Painter2D.SaveToVectorImage`, que existe en `UnityEngine.UIElementsModule` de 6000.3.27f1 sin el módulo de Vector Graphics. El elemento la usa como `background-image` teñida con `-unity-background-image-tint-color`. Alternativa: `generateVisualContent` con `resolvedStyle.color`. No verificable sin capturas todavía: que el tinte y su transición se apliquen bien a un `VectorImage` (se mira en la It. 2).
- **Costo.** Una malla de menos de 200 triángulos por ícono, generada una vez al arrancar y reutilizada, sin texturas.

Notación: `L(x,y …)` línea quebrada; `C(cx,cy,r)` circunferencia; `D(cx,cy,r)` disco relleno; `A(cx,cy,r,a0→a1)` arco en grados, 0° a la derecha y en sentido horario (y hacia abajo, como `Painter2D`); `R(x,y,ancho,alto,radio)` rectángulo redondeado; `Z(…)` contorno cerrado; `[3]` trazo de 3; `T(dx,dy)` desplazamiento; `+` otra figura. Decimales con coma. Se pueden correr ±1 unidad para que se lean mejor: la aceptación es la captura.

| Ícono | Receta |
|---|---|
| `bluetooth` | `L(7,7 17,17 12,22 12,2 17,7 7,17)` |
| `bluetooth_connected` | `bluetooth` + `D(4,12,1,5)` + `D(20,12,1,5)` |
| `bluetooth_disabled` | `bluetooth` + `L(3,3 21,21)` |
| `bluetooth_searching` | `bluetooth` con `T(−3,0)` + `A(14,12,4,−45→45)` + `A(14,12,7,−45→45)` |
| `timer` (contorno) | `C(12,13,8)` + `L(10,2 14,2)` + `L(12,13 12,8)` |
| `timer` (relleno) | `D(12,13,8)` + `L(10,2 14,2)` |
| `fitness_center` | `[3] L(6,7 6,17)` + `[3] L(18,7 18,17)` + `L(7,5,12 16,5,12)` + `L(2,5,9,5 2,5,14,5)` + `L(21,5,9,5 21,5,14,5)` |
| `history` | `A(13,12,8,200→510)` + punta `L(3,8 5,5,10,5 8,8)` + agujas `L(13,8 13,12 16,14)` |
| `rowing` | casco `L(2,15 5,18 19,18 22,15)` + remo `L(5,6 14,15)` + cabeza `D(15,8,2,2)` |
| `speed` | `A(12,15,8,180→360)` + aguja `L(12,15 16,10)` |
| `bolt` | `Z(13,2 5,13,5 11,13,5 10,22 19,10 13,10)` con trazo |
| `straighten` | `R(2,8,20,8,1,5)` + marcas `L(6,10 6,12)` `L(10,10 10,13)` `L(14,10 14,12)` `L(18,10 18,13)` |
| `favorite` | `Z` corazón = desde (12,20) a (4,6;11,25), `A(8,5,9,4,5,150→321)`, `A(15,5,9,4,5,219→390)` y vuelta a (12,20). Relleno: ese contorno relleno. Contorno: el mismo, con trazo |
| `heart_broken` | `favorite` con trazo + grieta `L(12,6,5 10,5,11 13,5,13,5 11,5,18)` |
| `chevron_right` | `L(9,6 15,12 9,18)` |
| `refresh` | `A(12,12,7,20→315)` + punta `L(19,4,5 19,9,5 14,5,9,5)` |
| `build` | anillo `A(15,5,8,5,4,5,0→270)` + mango `[3] L(12,3,11,7 4,5,19,5)` |
| `close` | `L(6,6 18,18)` + `L(18,6 6,18)` |
| `add` / `remove` | `L(12,5 12,19)` + `L(5,12 19,12)` / solo `L(5,12 19,12)` |
| `link_off` | `R(2,8,8,8,4)` + `R(14,8,8,8,4)` + `L(3,3 21,21)` |
| `error` | `C(12,12,9)` + `L(12,7 12,13)` + `D(12,16,5,1,25)` |
| (sin receta) | `R(5,5,14,14,3)` |

Los demás íconos de §4.3 usan el genérico hasta que la iteración que los necesite sume su receta.

---

## 5. Unity: paquetes y Asset Store gratis

### 5.1 Paquetes del Package Manager (registro de Unity, gratis)

Versiones exactas y motivos: [ARCHITECTURE §1.2](../architecture/ARCHITECTURE.md) (manda). El arte necesita estos:

| Paquete | Para qué | Estado en el manifest |
|---|---|---|
| Universal RP (`com.unity.render-pipelines.universal` 17.3.0) | render | desde la It. 1 |
| Shader Graph (`com.unity.shadergraph` 17.3.0) | shaders de tela, piel y vidrio de UI (UI Shader Graph) | desde la It. 1 |
| Animation Rigging (`com.unity.animation.rigging` 1.4.1) | IK del remero (It. 7) | desde la It. 1. Trae como dependencias `com.unity.burst` (≥ 1.4.1; Unity resuelve la versión verificada del Editor) y, a través de Burst, `com.unity.mathematics`. El código de RowMate **no** los referencia: no se escriben restricciones propias (ANIMATION_BIBLE §7.3, QA-P-29) |
| ~~Vector Graphics (`com.unity.vectorgraphics`)~~ y módulo `com.unity.modules.vectorgraphics` | **No se usan** (QA-P-09, cerrado al iniciar la It. 2). El paquete solo existe como *preview*. El módulo integrado 1.0.0 existe en el Editor 6000.3.27f1 e importa SVG como `VectorImage` (verificado), pero los íconos son glifos de fuente (§4) y los provisorios se dibujan con `Painter2D`, que no lo necesita (§4.4) | no se agregan |
| Cinemachine (`com.unity.cinemachine`) | descartado por ARCHITECTURE §1.2; la cámara de persecución es propia. Se reevalúa con las cámaras extra (BKL-02) | no se agrega |

### 5.2 Terrain Sample Asset Pack (Unity Technologies, gratis)

Contenido: capas de terreno (pasto, arena, tierra, canto rodado, nieve, roca), 20 prefabs de vegetación (pasto, helechos, arbustos) y Shader Graph de vegetación compatibles con URP. **Pesa 1,6 GB.** Licencia: EULA estándar del Asset Store de Unity (uso en la app; no se redistribuye el contenido).

1. Abrí [Terrain Sample Asset Pack en el Asset Store](https://assetstore.unity.com/packages/3d/environments/landscapes/terrain-sample-asset-pack-145808) con tu cuenta de Unity → **Add to My Assets** (gratis).
2. En Unity: **Window → Package Manager** → desplegable **My Assets** → `Terrain Sample Asset Pack` → **Download** → **Import**.
3. En la ventana de importación **destildá todo** y tildá solo: las *Terrain Layers* de pasto, arena, tierra, canto rodado, roca y nieve con sus texturas; los prefabs de vegetación de pasto, helecho y arbusto con sus materiales; los Shader Graph de vegetación para URP. **No** importes heightmaps ni escenas de demo.
4. Mové lo importado a `Assets/ThirdParty/TerrainSampleAssetPack/` (§0).
5. Texturas: override **Android/iOS** Max Size `1024` (capas) y `512` (vegetación), Format `ASTC 6x6`.
6. Asigná cada capa a su slot `terrain.layer.*` y cada prefab a `vegetation.*` (`Assets/RowMate/Slots/`).

---

## 6. Strava (It. 12)

1. Entrá a la [guía de marca de Strava](https://developers.strava.com/guidelines/) y descargá el paquete de botones **"Connect with Strava"** (versión naranja y blanca) y el logo **"Powered by Strava"**, en **PNG @2x** (no SVG: Unity no usa SVG en este proyecto, §4.2).
2. Copialos a `Assets/RowMate/UI/Brand/Strava/` (ARCHITECTURE §2) sin modificar colores ni proporciones (lo exige la guía). Import: Texture Type `Default`, sin compresión con pérdida (`RGBA 32 bit`), con mipmaps.
3. Asignalos al slot `brand.strava`. Sin estos archivos se dibuja un botón propio `#FC4C02` con el texto del catálogo.

---

## 7. Opcionales CC0 y dominio público [requieren OK del usuario, D-ART-08]

| Fuente | Qué aporta | Licencia | Cómo |
|---|---|---|---|
| [ambientCG](https://ambientcg.com) | texturas PBR de ladrillo, piedra, madera de muelle, roca sedimentaria, grava | CC0 | buscar "bricks", "planks", "rock", "gravel" → descargar **1K-JPG** → `Assets/ThirdParty/CC0/` → asignar a los materiales de `prop.*` |
| [Poly Haven](https://polyhaven.com/textures) | ídem (alternativa) | CC0 | descargar 1K → mismo destino |
| [NASA SVS — CGI Moon Kit](https://svs.gsfc.nasa.gov/4720) | mapa de color de la luna real | dominio público (crédito "NASA's Scientific Visualization Studio") | descargar el mapa de color más chico (≥ 1K), reducir a 512 → `sky.moon` |

Prohibido: assets pagos, licencias "no comercial" (CC BY-NC), modelos extraídos de juegos, cualquier cosa sin licencia explícita.

---

## 8. Calidad mínima de los placeholders

Un placeholder tiene que poder mostrarse en una demo sin pedir disculpas:

| Placeholder | Criterio |
|---|---|
| Maniquí | mismo esqueleto e IK que el personaje real; cápsulas con uniones suaves; mono pintado con los colores de contrato; se lee "atleta en un single" a 9,5 m |
| Casco, riggers, remos | proporciones reales (ART_BIBLE §7.8), gelcoat con reflejos, pala con concavidad visible al plumear |
| Pino | silueta irregular (no conos perfectos), 3 paletas legacy con variación por instancia, viento |
| Edificios y clubes | grilla de ventanas con variación, cornisas, luces nocturnas fijas por edificio |
| Terreno procedural | sin costuras entre segmentos de 50 m; transición agua-orilla con franja húmeda |
| Texturas de agua | tileables sin repetición evidente a 20 m (dos escalas cruzadas) |
| Íconos provisorios (§4.4) | mismo tamaño, color y animación que el glifo; se reconoce cada ícono de la It. 2 en una captura a 24 dp; nunca "□" ni texto en la caja; el layout no se mueve al llegar la fuente |
| Texto sin Inter ni Barlow | fuente por defecto del tema de Unity con los tamaños de `Tokens.cs`; los números siguen tabulares (las celdas de `TabularNumber` se miden en la fuente que haya) |

QA verifica cada placeholder con la captura de aceptación de su iteración (4 entornos × 4 horas en It. 8).

---

## Cambios por QA (plan)

Correcciones por el gate de armonía del plan ([QA_PLAN_REVIEW.md](../QA_PLAN_REVIEW.md)), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-P-09 | Los íconos pasan a ser **glifos de fuente**: Material Symbols Rounded, con la versión fijada (commit `10ecc14`), 2 instancias estáticas (sin relleno y con relleno, Weight 500) y recorte a 42 + 7 glifos con fonttools (§4.1). Se agregan la importación y el uso por clase USS `.rm-icon` (§4.2) y la tabla de íconos con sus codepoints verificados contra el `.codepoints` de esa versión, que ahora cubre todos los `Icons.*` del legacy (§4.3). Se saca `com.unity.vectorgraphics` de §5.1 (solo existe como *preview* 3.0.0). Respaldo: PNG teñidos; SVG (módulo integrado de 6.3) solo con aprobación de QA. Queda abierta la diferencia con ARCHITECTURE §1.2, que hoy elige SVG con el módulo integrado y deja los glifos como respaldo: §4 sirve en los dos casos. Strava y el ícono de la app no usan SVG dentro de Unity |
| QA-P-29 (parte Designer) | §5.1: Animation Rigging 1.4.1 trae `com.unity.burst` y `com.unity.mathematics` como dependencias; el código propio no los referencia. `rower.character`: el maniquí arma su avatar con `AvatarBuilder`, y el slot le pone el controlador de pose `RowerPose` (§1.1, §2.5) |
| QA-P-15 (alineación con ARCHITECTURE) | §0: modelo de slots de ARCHITECTURE §14.4 (base `AssetSlotDefinition` + subtipos tipados como `RowerCharacterSlot` + `SlotIds` en Core) y rutas del árbol único de ARCHITECTURE §2: `Slots/`, `UI/Fonts/`, `UI/Icons/`, `UI/Brand/Strava/`, `Art/Shaders/`, `Art/Textures/Thumbnails/`. Los terceros van solo a `Assets/ThirdParty/` (ignorada salvo los `.meta`, QA-P-01), con el aviso de los `.meta` huérfanos (RA-17) |
| QA-P-11 (alineación con ROADMAP §6) | `ui.thumbnail`: miniaturas horneadas en el Editor a las 12:00 (paridad). La opción de 6, 12, 18 y 22 h se ofrece en el gate de la It. 8 |
| ROADMAP (iteraciones nuevas) | Referencias a las It. 6a/6b; prioridades alineadas con los prerrequisitos del ROADMAP §0; lista de Mixamo preliminar, que se confirma en la It. 6a (P-09) |

---

## Cambios al iniciar la It. 2

Decisión de íconos y alineación con lo que se verificó con el Editor instalado. Detalle, comparación y verificación en [iterations/02-dispositivo-simulador/00-deuda-diseno.md](../iterations/02-dispositivo-simulador/00-deuda-diseno.md), 2026-10-27.

| Dónde | Qué cambió |
|---|---|
| §4 (QA-P-09) | **Cerrado** el punto abierto con ARCHITECTURE §1.2: los íconos son glifos de "RowMate Icons". ARCHITECTURE §1.2 y ADR 001 ya decían lo mismo; el texto "hoy elige SVG" había quedado viejo. Dato nuevo: el Editor 6000.3.27f1 trae el módulo integrado `com.unity.modules.vectorgraphics` 1.0.0, que importa SVG como `VectorImage` sin paquete (verificado). Igual no se usa: 49 archivos sueltos sin verificación en dotnet contra 2 fuentes de 12 KB con test de codepoints. SVG solo para un futuro ícono multicolor, con aprobación de QA. SVG deja de figurar como respaldo |
| §4.1 | Receta verificada de punta a punta (versión fijada, fonttools 4.67.0): 9 KB / 43 glifos y 3 KB / 8 glifos. Se agrega la variante sin Python instalado, con `uvx --from fonttools` |
| §4.2 | Atlas 512×512 (contorno) y 256×256 (relleno), verificados en 6000.3.27f1. Métricas de la fuente (línea de 1,2 em, simétrica respecto del ícono). El corazón del pulsómetro es relleno siempre, como el legacy. Nunca "□": se cae al provisorio. Los PNG quedan como último recurso |
| §4.3 | Los 42 + 7 codepoints se revisaron contra el `.codepoints` de `10ecc14`: coinciden todos. `timer` también es el split del tablero y `speed` solo el SPM, como el legacy (`device_screen.dart:253-254`). `favorite`: relleno en el encabezado y el tablero, contorno en las filas encontradas. `person`: la pestaña Perfil aparece en la It. 12-13 |
| §4.4 (nuevo) | Íconos provisorios dibujados por código, con sus recetas para los íconos de la It. 2 y un genérico para el resto |
| §0.1, §1.6, §3, §5.1, §8 | Placeholder de íconos; no usar las Inter que trae el Editor; qué pasa sin las fuentes; el módulo integrado de SVG no se agrega; criterios de calidad de los placeholders de texto e íconos |

---

## Cambios de la It. 3

Rutinas, editor y base local. Fuente: [03-rutinas/04-design-notes](../iterations/03-rutinas/04-design-notes.md) (§4.15, §4.16, §10, §14.6 y §16) y [02-tech-design §7.1](../iterations/03-rutinas/02-tech-design.md) (WP-D0), 2026-10-28.

| Dónde | Qué cambió |
|---|---|
| §1.6 | Slot nuevo `ui.font.emoji`: Noto Emoji monocroma Medium (OFL 1.1), último respaldo del texto para emojis del usuario (P-09) |
| §3 | Punto 5: kerning de la tabla versionada (`getFontFeatures = false` en las 7 caras de texto, DN3-24) y cómo se completa la tabla cuando haga falta. Punto 6: Noto Emoji, su `FontAsset` dinámico y el orden de respaldos de `RuntimePanelFactory`, con el visto bueno del Designer |
| §4.1 | `uvx --from fonttools==4.67.0` con la versión fijada (QA-G3-18); el recorte de contorno pasa a 44 codepoints (+ `U+E5C4`, `U+E5D4`): 45 glifos y 9 304 B, con los 42 de antes idénticos (contornos y avances comparados con fontTools) |
| §4.2 | El atlas de 512 × 512 alcanza para los 44 glifos |
| §4.3 | `arrow_back` (`E5C4`) y `more_vert` (`E5D4`); total 44 + 7 |

---

## Cambios de la It. 4a

Cifras tabulares y kerning del HUD (WP-D0). Fuente: [04a-motor-hud-funcional/00-deuda-diseno §3](../iterations/04a-motor-hud-funcional/00-deuda-diseno.md) (DD4a-06, DD4a-07, K1 a K6), [04-design-notes §4.9](../iterations/04a-motor-hud-funcional/04-design-notes.md) (DN4a-12, DN4a-22) y [02-tech-design §6.14 y §9.1](../iterations/04a-motor-hud-funcional/02-tech-design.md), 2026-10-28.

| Dónde | Qué cambió |
|---|---|
| §1.6 | `ui.font.numeric` es **RowMate Numeric SC** 600, 700 y 800 (Barlow Semi Condensed con `tnum` congelado), que dibuja todo número, también `num.stat`. Los `.ttf` de Barlow quedan como entrada de la receta |
| §3, punto 3 | La fila de números pasa a la derivada (mismos ajustes que Barlow: dinámica, muestreo 90); sus `FontAsset` los crea el bootstrapper (WP-B0) |
| §3, punto 5 | K2: la tabla de cada cara de texto (Inter ×4 y la derivada 700) es la lista de `UI/Fonts/Kerning/<cara>.pairs.json` que escribe `tools/fonts/kern_pairs.py`. Sale el camino de `TryAddCharacters` (DD4a-06, camino a) |
| §3, punto 7 | La derivada integrada con su receta reproducible (`tools/fonts/freeze_tnum.py`, fontTools 4.68.0 sin red). Sale la rama "si Barlow no trae `tnum`": la trae. El derivado de Inter para listas y ejes queda para la It. 9, si hace falta |

Verificado al generarla: las diez cifras de cada derivada en `zero.tf` … `nine.tf` con un solo avance (507, 521 y 535 unidades); las tablas de la derivada idénticas a las de la receta verificada en 00-deuda-diseno salvo la fecha de `head` (ahora la de Barlow, para que dos corridas den los mismos bytes); las listas K2 con 1 445, 1 695, 1 695, 1 695 y 1 272 pares (juego de 116 caracteres, 115 con glifo) y los 2 882 pares que ya tenían las tablas de TextCore, con el mismo valor (0 diferencias).
