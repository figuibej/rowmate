# ADR 023: Un escenario es un kit sobre la base común

- **Estado:** Aceptado (2026-11-07), al implementar WP-B de la It. 8 (DT8-01 de [02-tech-design](../../iterations/08-entornos-selector/02-tech-design.md))
- **Ámbito:** Scene (`SceneWorld`, `SceneWorldBuilder`, `SceneHost`, `SegmentStore`, `KitPropRenderer`, `HorizonRenderer`), Core (`KitSegmentMesh`, `EnvironmentHorizonStrip`, `EnvironmentCatalog`), App (`SceneEnvironmentChoice`), Presentation (`EnvironmentPicker`)
- **Relacionados:** [ADR 015](015-escena-3d.md) (la escena 3D), [ADR 018](018-niveles-de-calidad.md) (niveles), [ADR 022](022-escena-en-el-panel.md) (la escena en el panel)

## Contexto

La It. 8 suma Río, Costa y Regata al Lago de la 6a. La spec pide un solo inmersivo, sin escenas aditivas (§11.2), cambiar de escenario en Workout en reposo sin trabar la UI y que el escenario elegido se use al entrar. El cielo, el agua, el bote, el remero, la luz, el post y los *shaders* son iguales en los cuatro; cambian la orilla, el terreno, el relleno y la silueta lejana.

## Decisión

1. **Base común + kit.** `SceneWorld` se arma una vez (lo de la 6a y la 7). El kit del escenario es el `SegmentStore` de ese escenario (los objetos del legacy con su colocación, el terreno, el relleno y sus mallas por segmento) y la tira de su silueta lejana. **Un kit en memoria**: `SceneWorld.SetEnvironment(id)` descarta el anterior (sus mallas) y arma el nuevo vacío; los *streamers* (`TerrainStreamer`, `ShoreStreamer`, `FillStreamer`, `KitPropRenderer`) se vuelven a apuntar (`Rebind`) sin crear objetos.
2. **El cambio va por tramos.** `SceneHost.SwitchKit` deja `Readiness = Loading` y retoma el armado desde la orilla de 920 m (fase 2): los `Tick` sintéticos, el dibujo de calentamiento y la orilla de 0 m corren con el escenario nuevo antes de `Ready`, con el tramo de la 6a (0 ms mientras algo anima). Lo dispara `Preload` (Workout en reposo, también al cambiar `SceneEnvironmentChoice.Selected`) o, si el toque llega antes, `Open` (la escena demorada de la 6a, AC-SCN-01-07).
3. **La sesión es del escenario elegido al entrar**: `SceneSession(EnvironmentCatalog.Create(elegido))`; la sesión preparada en reposo se usa solo si es del mismo escenario.
4. **Los objetos de río, costa y regata se dibujan por segmento** (respaldo mientras faltan los *shaders* `Facade`, `Glow`, `Crowd` y `MarkerText` de WP-D1): `KitSegmentMesh` (Core) junta los objetos del segmento, el muro, el relleno, las ventanas de la máscara del legacy y las cifras del cartel en 7 segmentos en una malla por LOD (cercano hasta 120 m), con lo que se enciende de noche en una submalla aparte. `KitPropRenderer` dibuja 1 malla por segmento y LOD (≤ 2 dibujos por segmento) con `Prop` y, de noche, la submalla con el material de ventana.
5. **Las miniaturas del selector se hornean** con la escena real (`ThumbnailRule`) por el arnés (conjunto `thumbs`) y se versionan con su huella (DT8-16). En Workout son texturas: nunca se arma una escena para el selector.

## Consecuencias

- Cambiar de escenario no rehace el cielo, el agua, el remero ni los materiales: solo el almacén de la orilla (≈ 16 segmentos) y una tira. La memoria queda en un kit.
- El fundido de aparición y el tramado cercano del legacy (DV-62) son por segmento, no por objeto, en los kits; el lago sigue con los suyos.
- Las variantes de *shaders* de los 4 escenarios son las mismas (todo usa `Prop`, `Terrain`, `Horizon` y `WaterDegraded`): cambiar de escenario no compila nada.
- Cuando lleguen los *shaders* de WP-D1, el kit pasa a instanciado con `float4` por instancia (02-tech-design §3.3) sin cambiar la API de `SceneWorld.SetEnvironment` ni la del selector.
