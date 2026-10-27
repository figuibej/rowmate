# ADR 018: Tres niveles de calidad y adaptación térmica

- **Estado:** Aceptado (2026-10-27), por el gate de QA del plan (QA-P-10) y el pedido del Designer (ART_BIBLE §9)
- **Ámbito:** Platform (`GraphicsTier`, `ThermalAdapter`), Settings, Scene, Editor (bootstrapper); It. 6a (niveles), 6b (adaptación y medición)

## Contexto

Había dos presupuestos y dos esquemas de calidad: ARCHITECTURE §20 ("Móvil/Escritorio", GPU 14 ms, 300 k triángulos, SetPass 40, sombras de 2 cascadas a 60 m, 200 MB de texturas, dos assets de URP Mobile/Desktop) contra ART_BIBLE §9 (Alto/Medio/Bajo, GPU 12 ms, 350 k, SetPass 50, 1 cascada a 40 m, 180 MB). La adaptación térmica no tenía diseño, la resolución dinámica con `ScalableBufferManager` no funciona en GLES3 ni DX11, y el tiempo de GPU de `FrameTimingManager` no está en todos los Android.

## Decisión

- **Una sola tabla**, en ARCHITECTURE §20, con los números del Designer y tres niveles: **Alto** (gama alta y escritorio), **Medio** (paridad, Galaxy A54 / Pixel 6a / iPhone 11) y **Bajo** (Mali-G52, Android 8+ ARM64). ART_BIBLE §9 la referencia y conserva el reparto del arte.
- Cada nivel es un `QualityTierProfile` (`ScriptableObject`) con su `UniversalRenderPipelineAsset` (`RowMate_URP_{High,Medium,Low}`): resolución, MSAA, sombras, reflejo, agua, vidrio, post y LOD.
- `GraphicsTier` elige el nivel inicial con `SystemInfo` y una lista de GPU conocidas.
- `ThermalAdapter`: `FrameTimingManager` con *Frame Timing Stats*; si no hay tiempo de GPU, usa el de CPU (hilo principal + *render thread*). Disparo: > 13 ms de GPU (o > 15 ms de CPU) durante 3 s. Orden: resolución dinámica (`renderScale`, pasos de 0,05 entre 0,75 y 1,0, como mucho un cambio cada 10 s, subida tras 30 s por debajo de 10 ms) → reflejo → distancia de sombras → densidad de vegetación → nivel. Nunca se baja la tasa de cuadros antes que el detalle.
- El bootstrapper de la It. 1 crea **un** asset de URP provisorio y no fija niveles; los tres llegan en la It. 6a.

## Alternativas descartadas

- **`ScalableBufferManager`/resolución dinámica de Unity**: no anda en GLES3 ni DX11.
- **Dos niveles Mobile/Desktop**: no cubre el rango de Android de gama baja que pide R-12 sin castigar a la gama media.
- **Ajuste de calidad visible al usuario**: el legacy no lo tiene; se puede sumar en el backlog.

## Consecuencias

- Cambiar `renderScale` realoca *buffers*: por eso los cambios son poco frecuentes y con histéresis.
- Todo es NO VERIFICABLE sin el Android de gama media (ROADMAP regla 9).

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-10 | ADR nuevo: tabla única, 3 niveles como `ScriptableObject`, adaptación térmica con respaldo por CPU y `renderScale` |
| QA-P-18 | Nivel Bajo para Android 8+ ARM64 (Q-90) |
