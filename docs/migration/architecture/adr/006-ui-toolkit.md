# ADR 006: UI Toolkit para toda la UI, glass con UI Shader Graph y gráficos con Painter2D

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** Presentation, Scene (blur), Designer

## Contexto

La app tiene 5 pantallas con layouts vertical/horizontal, un HUD translúcido sobre la escena 3D que tiene que leerse a 1,5 m, diálogos y gráficos del historial. El Designer trabaja con tokens y USS. Unity 6.3 permite materiales propios en `VisualElement` con UI Shader Graph (solo URP).

## Decisión

- **UI Toolkit** para todo lo 2D: shell, pantallas, HUD, diálogos, panel del simulador.
- Escala en dp lógicos del legacy (`ConstantPhysicalSize` a 160 dpi en móvil; `ConstantPixelSize` × escala del sistema en escritorio) y área segura aplicada al raíz.
- **Glass**: un Renderer Feature de URP (`HudBlurFeature`, Render Graph) publica la escena a 1/4 de resolución con 3 pasadas Kawase en la textura global **`_RmHudBlur`**, escrita en un `RTHandle` **persistente** (no transitorio de Render Graph) para que llegue viva a la UI en *Screen Space Overlay*; el material `RowMate/UI/Glass` (UI Shader Graph) lo muestrea en coordenadas de pantalla. Respaldo: vidrio plano con el token `glass.fallback` del Designer (QA-P-17; hoy `#070D16 @86 %`, ART_BIBLE §3.2) en el nivel Bajo y hasta la It. 6b.
- **Cifras**: `TabularNumber` es **un** `VisualElement` por número que dibuja sus glifos en celdas de ancho fijo con `generateVisualContent` y anima el rodillo por transformaciones, sin asignar memoria (ARCHITECTURE §7.8).
- **Frecuencia**: 60 fps mientras haya una animación (transición, rodillo, corazón visible, desplazamiento); 30 fps solo en reposo (ARCHITECTURE §13.1).
- **Tokens**: `Tokens.uss` lo genera `tools/tokens` desde `Core/Design/Tokens.cs` (ADR 016).
- **Gráficos**: `MetricChartElement` con `generateVisualContent` + `Painter2D`; la geometría la calcula `Core.History.ChartGeometry`.

## Alternativas descartadas

- **uGUI + TextMeshPro**: layout responsivo más frágil, sin cascada de estilos, rebuilds de canvas costosos con números que cambian seguido.
- **Librería de gráficos externa**: dependencia para tres tipos de trazo; Painter2D alcanza.
- **Filtros de UI Toolkit para el blur**: se aplican al contenido del propio elemento, no al fondo.

## Consecuencias

- Hay que vigilar la cantidad de elementos del HUD en Android de gama baja (presupuesto en ARCHITECTURE §20) y perfilar en dispositivo desde la It. 4a.
- El blur se prototipa en la It. 6b, cuando existe la escena (propuesta P-04).
- Las cifras tabulares del HUD no dependen de la fuente (`TabularNumber`); las de listas y ejes usan el derivado "RowMate Numeric".
- `MeshGenerationContext.DrawText` no se pudo verificar sin Editor: si no sirve, `TabularNumber` dibuja *quads* del atlas SDF (RA-18).

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-07 | Política de 60 fps mientras algo se anima |
| QA-P-08 | `TabularNumber` como un solo elemento |
| QA-P-16 | Nombre `_RmHudBlur`, 3 pasadas, `RTHandle` persistente; prototipo en la 6b |
| QA-P-05 | `Tokens.uss` generado (ADR 016) |

## Cambios de la It. 2 (WP-B)

| ID | Qué cambió |
|---|---|
| D-02 | Las vistas se construyen en C# con nombres y clases de contrato (02-tech-design §3.12); sin UXML. El arnés de capturas y las pruebas PlayMode arman exactamente la misma UI (`ShellHost`) |
| D-03 | Escala: `ConstantPixelSize` con `scale` = densidad del sistema en todas las plataformas (Android `DisplayMetrics.density`, Windows `GetDpiForWindow / 96`), no `ConstantPhysicalSize`. Área segura con una sola regla (`ShellLayout`, QA-G2-06) en lugar de rellenar la raíz |
| D-14 | Motion por código con un reloj inyectable (`MotionDriver` + `IClock`): rodillo, destello, latido, píldora, transiciones de pestaña y de vista; USS (`Motion.uss`, WP-D) solo para *hover*, presión, foco e interruptor |
| D-01 | Canal de color `GammaRt` (ADR 019) |
| RA-18 | `MeshGenerationContext.DrawText` sirve para `TabularNumber`: toma el tamaño en píxeles físicos (× `scaledPixelsPerPoint`) y, en un proyecto Linear, el color en gamma con las dos tuberías (`MeshColor`, ADR 019) |
| — | Los botones de la app sacan la clase `.unity-button` (`RmButton.StripUnityChrome`): el aspecto por defecto de Unity tiene reglas `:hover`/`:active` más específicas que las de la app. Las etiquetas de botón van en una línea con "…" (el redondeo del layout a 2,625 partía "Desconectar" en dos líneas) |
