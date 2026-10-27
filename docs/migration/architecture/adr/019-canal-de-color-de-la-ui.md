# ADR 019: Canal de color de la UI (composición sRGB con GammaRt)

- **Estado:** Aceptado (2026-10-27), al implementar WP-B de la It. 2 (D-01 confirmada por el Designer y el PO; R-T1)
- **Ámbito:** Presentation (`Surface/UiSurface`, `OverlayCopyPresenter`, `BlitPresenter`, `Controls/MeshColor`), Bootstrap (`AppRoot`, `ColorProbe`), arnés de capturas (`UiTestHost`, `Rt1Measurement`)

## Contexto

El proyecto es Linear (URP). Unity compone la UI de UI Toolkit en espacio **lineal**: los blancos translúcidos se ven más claros que en el legacy, que compone en sRGB como Flutter/Skia (R-T1). Medido en el *spike* S-0 y repetido por el arnés (CAP-23, `snapshots/rt1-measurement.json`):

| Camino | `glass.fallback` sobre blanco | `text.secondary` sobre `surface.card` | Composición |
|---|---|---|---|
| *Screen Space Overlay* (player de Windows) / RT `R8G8B8A8_SRGB` | `#6A6A6C` | `#DADBDC` | lineal |
| RT `R8G8B8A8_UNorm` + `PanelSettings.forceGammaRendering = true` | **`#2A2F37`** | **`#BBC1C8`** | sRGB (legacy: `#2A2F37` / `#BAC0C7`) |

`forceGammaRendering` no tiene efecto en *Screen Space Overlay*.

## Decisión

- **`UiColorPipeline.GammaRt` por defecto** (opción A de la It. 1): el panel de la app se dibuja en una RT `R8G8B8A8_UNorm` del tamaño de la pantalla con `forceGammaRendering`, y un presentador la lleva al *backbuffer* sin volver a convertir los colores. Respaldo: `Overlay` (`-rowmate-ui-pipeline Overlay`), con la composición lineal y el respaldo de tokens de 04-design-notes §8.3.
- **Presentador: `OverlayCopyPresenter`** (solo UI Toolkit, sin *shader*): en `RenderPipelineManager.beginContextRendering`, `Graphics.CopyTexture` de la RT UNorm a una RT sRGB del mismo tamaño y un segundo panel *Overlay* (`sortingOrder` 30000, `pickingMode Ignore`) con esa RT de fondo a pantalla completa. Su `GameObject` va en la **raíz de la escena**: un `UIDocument` hijo del `GameObject` de otro `UIDocument` se anida en el panel del padre e ignora su `PanelSettings` (ver S-B1). `BlitPresenter` queda escrito como alternativa (necesitaría `GammaPresent.shader`, que no se agrega: S-B1 no lo pidió).
- **Las RT llevan stencil** (`D24_UNorm_S8_UInt`): UI Toolkit enmascara con stencil los `overflow: hidden` con esquinas redondeadas (el panel del simulador, el cuerpo de la tarjeta mientras anima). Sin stencil la máscara sale como un rectángulo blanco (hallazgo del arnés, CAP-16).
- **Limpieza a `bg.app`** al crear cada RT (`GL.Clear`): en la UNorm con el valor del token; en la sRGB, con el color en lineal (si no, la RT recién creada da `#405B71`; prueba `THM.SurfaceRtsStartCleared`). Lo mismo para el `colorClearValue` del panel con `Overlay` (`UiSurface.ClearValue`): el panel limpia un destino sRGB.
- **Lo que se dibuja con `DrawText` se compensa** (`Controls/MeshColor`): en un proyecto Linear `MeshGenerationContext.DrawText` deja el color con una conversión a lineal de más **en las dos tuberías** (el trazo de `#FF9F1C` salía `#FF5802` con GammaRt en el arnés y con Overlay en el build, mientras la etiqueta de al lado, con el mismo color por USS, salía bien): se le pasa `color.gamma`. `DrawText` toma además el tamaño en píxeles físicos (se multiplica por `scaledPixelsPerPoint`). `Painter2D` no necesita compensación (el *spinner* va con el color resuelto). El shell marca el panel gamma con `.rm-gamma-panel` (`ShellHostOptions.Pipeline`).
- **El arnés usa la misma tubería** que la app: `UiTestHost` crea la RT UNorm + `forceGammaRendering` (o sRGB con `Overlay`), así los bytes leídos son los de la pantalla.

## Resultado de S-B1 (build de desarrollo de Windows, Mono)

`tools/build/build.ps1 -Target Windows -Backend Mono -Simulator` y `RowMate.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 800 -rowmate-color-probe <json> [-rowmate-ui-pipeline GammaRt|Overlay]` (`Bootstrap/ColorProbe`: dibuja la muestra de CAP-23 en el panel de la app, con la misma superficie y el mismo presentador, y lee el *backbuffer* con `ReadPixels` después de `WaitForEndOfFrame`). Direct3D 11, NVIDIA GPU de escritorio, 2026-10-27:

| Criterio | GammaRt (`OverlayCopyPresenter`) | Overlay (respaldo) |
|---|---|---|
| (1) `glass.fallback` sobre blanco = `#2A2F37` ± 1 | **`#2A2F37`** ✔ (`text.secondary` sobre `surface.card`: `#BAC1C8`; legacy `#BAC0C7`) | `#6A6A6C` ✘ (`#DADBDC`) |
| (2) clic sintético del Input System llega al botón | 1 clic, `probe-button` ✔ | 1 clic ✔ |
| (3) costo de CPU de la presentación | 0,002 ms por cuadro (≤ 1 ms) ✔ | — |
| (4) primer cuadro = `bg.app` ± 1 | 0 píxeles distintos ✔ (sin contar la marca "Development Build" de Unity) | `#405B71` ✘ → corregido con `UiSurface.ClearValue` |
| (5) latencia 0 (cuadro k pinta rojo = k, se lee k) | 12 de 12 ✔ | 12 de 12 |

**Decisión: GammaRt con `OverlayCopyPresenter`** (sin *shader*). Overlay queda solo como respaldo por línea de comandos. IL2CPP y Android: NO VERIFICABLE en esta máquina (sin los módulos).

Hallazgos de S-B1 que cambiaron el código:
- La primera corrida dio `#0D1B2A` en todo el *backbuffer* aunque la RT de la UI y la copia tenían `#2A2F37` (diagnóstico del JSON): el `GameObject` del presentador colgaba de `AppRoot`, que tiene el `UIDocument` de la app, y Unity lo anidó en ese panel (la copia se dibujaba dentro de la propia RT; la pantalla mostraba el fondo de la cámara). Va en la raíz de la escena. El arnés no lo podía ver (no usa `AppRoot`).
- Con Overlay, `DrawText` también salía con el color oscurecido: la compensación de `MeshColor` vale para las dos tuberías.
- `TabularNumber` avisaba "[SLOT] ui.font.numeric" con el `FontAsset` puesto (miraba la fuente al adjuntarse, antes de resolver los estilos): decide después del primer layout.

## Consecuencias

- Los colores translúcidos de la It. 2 se ven como en el legacy en las capturas y en la app; las capturas no hay que repetirlas en la 4a.
- Una copia de pantalla completa por cuadro dibujado (≈ 10 MB a 1080 × 2400; R2-02): se mide en equipo en la 4a / 14. En GLES 3.0 `CopyTexture` UNorm → sRGB puede no estar: ahí, `BlitPresenter`.
- Todo control que dibuje con `DrawText` tiene que pasar por `MeshColor` (lo hace `TabularNumber`).
- El mapeo de la entrada (pantalla → panel) con `targetTexture` usa el de Unity por defecto (la RT cubre la pantalla); `SetScreenToPanelSpaceFunction` queda como respaldo.
