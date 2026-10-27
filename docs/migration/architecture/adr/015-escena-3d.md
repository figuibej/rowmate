# ADR 015: Escena 3D con coordenadas "cinta", escenarios aditivos y asset slots

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** Core.Scene, RowMate.Scene, Designer (It. 6a, 6b, 7 y 8)

## Contexto

El legacy dibuja una escena 2.5D con cámara de persecución, paisaje determinista por distancia (segmentos de 50 m), 4 escenarios, hora del día y agua por shader con respaldo. En Unity se pasa a 3D realista (URP) con assets gratuitos detrás de slots, 60 fps en móvil y sesiones que pueden superar los 10 km.

## Decisión

- **Coordenadas "cinta"** (origen flotante): el bote queda en el origen y el mundo se desplaza por la distancia de escena (en `double` en Core). La fase de cada ola y el desplazamiento de cada mapa de normal se calculan en `double` en la CPU y se acotan (módulo 2π o su período) antes de ir al shader: sin pérdida de precisión ni salto cada 4096 m (Q-68). `SceneState.ShaderDistance` (`% 4096`) se porta como contrato con su test y el agua nueva no lo usa (QA-P-28).
- **Escenas**: `World.unity` (agua, cielo, luz, bote, remero, cámara, `SceneDirector`) + una escena aditiva por escenario, cargada al entrar al inmersivo y descargada al salir. Fuera del inmersivo la cámara 3D se apaga.
- **Lógica pura en Core** (cámara, estado, palada, hora, `CelestialPlacement`, `HourLighting`, `MoonPhase`, generación de orillas con `DartRandom`, visibilidad, `WaterParams`, y en `Core/Scene/Stroke` `VisualStrokeClock`, `ScullingKinematics`, `BoatSpec`, `LegacyBoatGeometry`, `PuddleField`, `BoatSurge`); Unity (`RowMate.Scene`) solo aplica a transforms y materiales en un único `Update`, sin asignaciones.
- **Decisiones del Designer** (ARCHITECTURE §14.3): fase visual que sigue a la del contrato sin tocar `SceneState` (D-ANI-01), medidas reales con la geometría legacy conservada para sus tests (D-ART-04), charcos en `BladeExit` en la punta real con las reglas legacy (D-ANI-02), remero y remos hijos del pivote de cabeceo del casco (D-ANI-03), dos direcciones de sol con acimut común (D-ART-05).
- **Textura de interacción** RGBA8 de 256×200 sin estado, redibujada cada cuadro con *quads* instanciados.
- **Asset slots**: base `AssetSlotDefinition` (id de `SlotIds` en Core, ruta esperada, presupuesto, validación, *placeholder* y **un** aviso, nunca un error) + `AssetSlot<T>` con subtipos tipados (`RowerCharacterSlot`, `BoatPartSlot`, `OarSlot`, `PropSlot`, `TerrainLayerSlot`, `VegetationSlot`, `MaterialSlot`, `TextureSlot`, `FontSlot`, `DataSlot<T>`), agrupados por `EnvironmentDefinition`. Instancias en `Assets/RowMate/Slots/`; terceros en `Assets/ThirdParty/` (ignorado salvo `.meta`). Mixamo: Humanoid con *Optimize Game Objects* desactivado; maniquí con `AvatarBuilder` sobre el mismo esqueleto.
- **Agua degradada** siempre disponible (R-04) como `SubShader`/`Fallback`, elegida por nivel de calidad (ADR 018) y soporte del shader; llega en la It. 6a.
- **Miniaturas horneadas** en el Editor desde la escena real, a las 12:00 por defecto (paridad); si el usuario aprueba "hora actual", 4 por escenario (6, 12, 18, 22 h) y se muestra la más cercana. Nunca se renderizan en ejecución.

## Alternativas descartadas

- **Mover el bote por el mundo**: precisión de `float` y jitter en sesiones largas.
- **Addressables para los escenarios**: no hay contenido remoto; las escenas aditivas alcanzan.
- **Cinemachine**: una sola cámara del legacy, cuya matemática ya está en Core; se reconsidera con cámaras extra (BKL-02).

## Consecuencias

- El paisaje depende solo de la distancia y del escenario: testeable en `dotnet`.
- El Designer trabaja en las escenas aditivas y en los slots sin tocar código.

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-11 | Miniaturas horneadas, 12:00 por defecto; variante de 4 horas |
| QA-P-14 | Agua degradada en la It. 6a |
| QA-P-15 | Un solo modelo de slots (base genérica + subtipos tipados, `SlotIds` en Core) y rutas únicas |
| QA-P-28 | `ShaderDistance` como contrato, sin uso en el agua nueva |
| Pedidos del Designer | D-ANI-01/02/03, D-ART-04/05, agua y textura de interacción |
