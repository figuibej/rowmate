# ADR 027: Agua avanzada con caída a la degradada y vidrio desenfocado sobre la RT de la escena

- **Estado:** Aceptado (2026-11-11), al implementar WP-B de la It. 6b ([02-tech-design](../../iterations/06b-agua-avanzada/02-tech-design.md) DT6b-01 a -26)
- **Ámbito:** Core (`Scene/Water/*`, `SceneWarmupPolicy`, `ThermalAdaptation`, `Design/GlassTint`), Scene (`SceneHost`, `Water/{WaterSurfaceController, AdvancedWaterSurface, PlanarReflection, WakeRenderer}`, `HudBlurPass`), Presentation (`Workout/GlassSurface`, `IImmersiveScene`), Bootstrap (`SceneBridge`, `AppRoot`), shaders (`WaterAdvanced`, `RowMateWater.hlsl`, `WaterMarks`, `UI/HudBlur`)
- **Relacionados:** [ADR 022](022-escena-en-el-panel.md) (la escena a pedido en su RT), [ADR 023](023-kits-de-escenario.md) (kits)

## Contexto

El legacy elige el agua con un *shader* (`water.frag`) y cae a la pintada (`water_painter.dart`) si no carga, sin reintento. La 6a dejó solo la degradada. La 6b suma la avanzada en los 4 escenarios, que tiene que caer a la degradada sin cortar la sesión, con un fundido y una línea en el registro, y un vidrio desenfocado en el HUD que no sume elementos ni pases a la escena.

## Decisión

1. **Dos *renderers*, nunca los dos a la vez** (`WaterSurfaceController`): la degradada de la 6a (la grilla, el respaldo obligatorio) y la avanzada (la malla en anillos de Core). `WaterAdvanced` no tiene `Fallback` y lleva el tag `RmWater`=`Advanced`: hay soporte solo si `Shader.isSupported` y el SubShader activo es el avanzado.
2. **El agua es estado de la sesión de la app** (`WaterAvailability` en `SceneHost`): un equipo sin soporte, una falla (excepción, RT del reflejo perdida, `-rowmate-water-fail`) o el último paso térmico la dejan degradada hasta reiniciar, con una sola línea `[Scene] Shader de agua no disponible, usando fallback: <motivo>`.
3. **El fundido de 0,6 s es un solo dibujo**: `WaterAdvanced` sabe dibujar la degradada adentro (`_RmDegradedBlend`), así el cambio en los dos sentidos no pide dos opacos coplanares. Solo una excepción del dibujo corta en el acto.
4. **La avanzada se prepara después de `Ready`** (fase 13, unidades livianas): la escena nunca espera al agua; si el usuario ya entró, la avanzada aparece con su fundido.
5. **La estela y las columnas de luz son tipos de `WaterMarks`** (el mismo material y variante, cola 3050, mezcla premultiplicada): 0 SetPass nuevos en Medio, que está en su tope.
6. **El vidrio no es un *renderer feature***: `HudBlurPass` corre un `CommandBuffer` (bajada, 3 Kawase y composición con α(L) por píxel en el espacio del panel) sobre la RT de la escena después de dibujarla, y `GlassSurface` lo muestra como `background-image` de las piezas de vidrio que ya existen, alineado con `scene-surface`. Debajo queda el respaldo plano de hoy (peso 0 en Bajo).
7. **El reflejo planar de Alto** es una cámara espejo deshabilitada con matriz de reflexión y proyección oblicua, dibujada a pedido con `SubmitRenderRequest` y `GL.invertCulling`, sin post ni sombras.

## Consecuencias

- La degradada sigue siendo la de la 6a y la evidencia vieja no cambia de agua (`UiHostOptions.SceneWater` y `SnapshotCli.WaterFor` la fijan en la degradada).
- Volver a la avanzada cuando el equipo se enfría pide reiniciar (spec §9: va a issues).
- El reflejo planar solo ve los *renderers* (cielo, horizonte, terreno, bote y remero): la orilla instanciada no se encola para el espejo.
- El desenfoque suma 3 SetPass aparte del tope de la escena y no corre con el panel escondido ni en Bajo.
