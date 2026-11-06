# ADR 022: La escena en el panel (RT propia, pedido de render y `scene-surface`)

- **Estado:** Aceptado (2026-11-03), al implementar WP-B de la It. 6a (DT6a-05; sondas S6a-1 a S6a-7 de [02-tech-design §3.2](../../iterations/06a-escena-lago/02-tech-design.md))
- **Ámbito:** Scene (`SceneHost`, `SceneRenderer`, `SceneRenderTarget`, `ChaseCameraRig`), Presentation (`Workout/IImmersiveScene`, `Workout/SceneSurface`, `Workout/HudView`), Bootstrap (`SceneBridge`, `AppRoot`), arnés (`UiTestHost` con `SceneMode.Real`, `SceneCatalog`, `SceneMarks`)
- **Relacionados:** [ADR 015](015-escena-3d.md) (la escena 3D), [ADR 018](018-niveles-de-calidad.md) (niveles), [ADR 019](019-canal-de-color-de-la-ui.md) (la UI en una RT UNorm con `forceGammaRendering`)

## Contexto

La UI se compone en sRGB en una RT `R8G8B8A8_UNorm` con `forceGammaRendering` que el presentador copia a la pantalla (ADR 019). Si la escena 3D se dibujara en la pantalla con la UI transparente encima, el vidrio del HUD se compondría en lineal (`#6A6A6C` en lugar de `#2A2F37`, R-T1) y el contraste de las píldoras contra la escena (AC-WKT-17-13) se rompería. Además, el arnés lee la RT de la UI: lo que no esté ahí no sale en las capturas.

## Decisión

1. **La cámara de la escena nunca se dibuja sola** (`enabled = false`). En cada cuadro, `SceneHost.Tick` (desde el `LateUpdate` de `AppRoot`, o el `Tick` de `UiTestHost`) aplica el `SceneFrame` de Core y `SceneRenderer.Render` pide el dibujo con `RenderPipeline.SubmitRenderRequest(camera, UniversalRenderPipeline.SingleCameraRequest { destination = rt })`, con el objeto del pedido reutilizado. Si el pipeline no acepta el pedido, el respaldo es `Camera.Render()` (no se usó: S6a-2).
2. **La RT es nuestra** (`SceneRenderTarget`): del tamaño de la ventana en píxeles por la resolución del nivel (Alto 1, Medio 0,85, Bajo 0,75, y lo que baje la adaptación). Los assets de URP quedan con `renderScale` 1. Filtro de punto a tamaño completo y bilineal escalada. Se rehace solo al cambiar la ventana, la resolución o el formato (`OutputChanged`), sin reiniciar la escena.
3. **El formato de la RT sigue al nivel** (cambio por implementación, B6a-14): `R16G16B16A16_SFloat` en Alto y Medio (los que tienen HDR) y `R8G8B8A8_SRGB` en Bajo. URP arma la intermedia de una cámara que dibuja en una RT **con el formato de esa RT**: con una de 8 bits no hay HDR aunque el asset y la cámara lo permitan, y el disco del sol en `3 + 5e` (DN6a-21) quedaba recortado en 1 antes del *tonemapping* (salía gris, `#BEBEBE`). Con la de media precisión el *tonemapping* recibe el HDR y el *bloom* tiene de dónde salir. Los dos formatos dan el valor lineal al muestrear: el panel y la sonda S6a-1 no cambian.
4. **`scene-surface`** (`SceneSurface`, el primer hijo de `HudView`, a pantalla completa, `pickingMode = Ignore`) muestra la RT con `Background.FromRenderTexture`. Solo muestra: el ciclo de vida lo llevan `WorkoutScreenView` (la precarga en reposo) e `ImmersiveWorkoutPage` (abrir, vuelo, velo, salida, toma aérea, cerrar). Se suscribe a `OutputChanged` y a `Drawn` al entrar al panel y se desuscribe al salir y en `Dispose`. En cada cuadro en que la escena se dibujó pide `MarkDirtyRepaint()`: sin eso, el panel podía mostrar la RT del cuadro anterior en los cuadros sin cambios del HUD (S6a-7).
5. **El orden de un cuadro**: el `Tick` y el dibujo de la escena → la UI (*offscreen*, muestrea la RT recién dibujada) → el presentador. Cero cuadros de atraso entre la escena y el HUD. Con `OnDemandRendering.willCurrentFrameRender == false` no se pide el dibujo.
6. **La capa `RowMateScene`** (la crea `UrpProvisioner`): la cámara de la escena solo ve esa capa y la de la UI no la ve.
7. **El arnés usa la escena real** (`UiHostOptions.Scene = SceneMode.Real`, nivel Alto): la compilación de *shaders* sincrónica mientras dura el anfitrión y durante toda la corrida de capturas (`ShaderUtil.allowAsyncCompilation`, que no se guarda en `ProjectSettings`), la orilla armada en el acto y la sesión en pasos de 1/60 s (`SceneSession.Advance`). El *dithering* de la cámara apagado, sin grano y sin TAA.

## Resultado de las sondas (Editor, Direct3D 11, *batchmode* sin `-nographics`)

| Sonda | Resultado |
|---|---|
| **S6a-1** El color | Los cuatro *quads* sin luz (`#0B4FA8`, `#FFFFFF`, `#7F7F7F`, `#06122A`) ± 1 por canal en la RT de la UI y en la de la escena; también con la RT de media precisión ✔ |
| **S6a-2** El dibujo a pedido | `SubmitRenderRequest` dibuja en modo edición y en *Play* (`LastUsedSubmit`), el `Volume` cambia la imagen y las 80 estrellas instanciadas (`Graphics.RenderMeshInstanced` con `RenderParams.camera` y arreglos por instancia) llegan al pedido ✔ |
| **S6a-3** 0 B | 0 B por cuadro de lo nuestro y del dibujo de URP en 120 cuadros reales (remando, en pausa, la hora de 12 a 18 y de noche), con el control positivo (`byte[64]` en el `Tick`) contado en la misma corrida ✔ |
| **S6a-4** Determinismo | Dos corridas completas del conjunto `it6a` (45 archivos: 34 PNG y 11 MP4, con la escena real en Alto) **iguales byte a byte**; el `manifest.json`, igual salvo `durationMs` ✔ |
| **S6a-5** RT escalada | Con 0,85 la RT mide 0,85 × la ventana, se rehace una sola vez y `scene-surface` la estira con bilineal (el borde pasa de un tono al otro sin escalones de 2 px) ✔ |
| **S6a-6** La matriz | Con `Camera.projectionMatrix` = la de Core, `WorldToViewportPoint` × el tamaño de la RT da el píxel del legacy ± 1 dp en los siete tamaños; en las capturas con marcas (CAP-02, -05, -07) el peor `|Δx|` es 0 dp ✔ |
| **S6a-7** Cero cuadros de atraso | 0 cuadros de 120 con la escena vieja en la UI (con `MarkDirtyRepaint` en cada cuadro dibujado) ✔ |

## Consecuencias

- La escena cuesta una RT del tamaño de la ventana (en Alto y Medio, 8 bytes por píxel: en TV, ≈ 20 MB a resolución 1 y ≈ 15 MB a 0,85; en Bajo, 4 bytes) más la intermedia de URP. Entra en el presupuesto de memoria de §4.1 del diseño; si el teléfono pide menos, Medio puede volver a 8 bits a cambio del brillo del sol (decisión del Designer).
- El HUD sigue componiéndose en sRGB sobre la escena: el vidrio y el contraste de las píldoras se miden contra la escena renderizada (AC-WKT-17-13).
- Las pruebas que leen la RT de la escena usan `RtReader.Read`, que pasa una RT de media precisión a bytes sRGB (lo mismo que se ve en el panel).
- Sin escenas aditivas (`World.unity`, `Environments/Lake.unity`): el mundo lo arma `SceneWorldBuilder` bajo `scene-root` en `Boot.unity` (DT6a-04; ADR 015 corregido).

## Cambios por la verificación de la It. 6a (ronda 1)

- **El orden de los hijos de `HudView`** (QA-V6a-01, D2-05): el fondo provisorio primero y `scene-surface` **justo encima** (en UI Toolkit el hijo de después se dibuja arriba). El punto 4 decía "el primer hijo": así el provisorio, opaco, tapaba el fundido de 0,6 s de la escena demorada. Con la escena a la vista el provisorio sigue en `display: none`.
- **El formato de la RT** (punto 3; QA-V6a-REN-07): Medio pasa a `B10G11R11_UFloatPack32` (`SceneRenderTarget.FormatFor`); Alto sigue en `R16G16B16A16_SFloat` y Bajo en `R8G8B8A8_SRGB`.
- **La RT existe en `Ready`** (QA-V6a-REN-05): nace en la precarga y el dibujo de calentamiento va a ella (fuera de pantalla), con dos pedidos de URP antes (la máscara de la cámara en 0: a una RT chica del mismo formato y a la de la sesión) para el post y los intermedios.
