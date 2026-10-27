# ADR 016: Tokens de diseño con una sola fuente en Core

- **Estado:** Aceptado (2026-10-27), por el gate de QA del plan (QA-P-05) y el pedido del Designer (ART_BIBLE §2 y §11)
- **Ámbito:** Core.Design, Core.Models, Presentation (USS), Scene, `tools/tokens`; F-RUT-01 y F-APP-06

## Contexto

Los colores estaban repartidos en tres lugares: `StepTypeInfo`/`MetricPalette` en `Core/Models` (mapeo de `shared/theme.dart`), `Tokens.uss` y una clase `ThemeColors` en C# (ARCHITECTURE anterior), mientras el Designer pedía `Core/Design/Tokens.cs` con un generador. Tres copias se desincronizan, y F-RUT-01 (colores de paso) se cierra ya en la It. 1. Además, el contraste del HUD (R-10, D-ART-01) tiene que poder verificarse con `dotnet`.

## Decisión

- **`RowMate.Core.Design.Tokens`** (puro) es la única fuente de colores (hex base del legacy y variantes `*.hud`), alfas, radios, espaciados, tamaños de tipografía y duraciones/curvas de movimiento, con los nombres de ART_BIBLE §2 y ANIMATION_BIBLE §1.
- `StepTypeInfo` y `MetricPalette` (Core/Models) leen de `Tokens`. No se crea `ThemeColors`.
- `Core/Design/Icons.cs`: constantes de codepoints de la fuente "RowMate Icons" (ASSET_SLOTS §4), con un test de `dotnet` contra el `.codepoints` versionado. El respaldo plano del vidrio es un token más (`glass.fallback`, hoy `#070D16 @86 %`).
- **`tools/tokens`** (consola `dotnet` que referencia Core) genera `Assets/RowMate/Presentation/Styles/Tokens.uss` (variables `--rm-*`, encabezado "GENERADO, no editar"). Un test regenera en memoria y compara con el archivo versionado; el CI también lo chequea.
- **`Core.Design.Contrast`**: luminancia relativa, relación WCAG 2.x y composición alfa. Test de `dotnet` con los umbrales de ART_BIBLE §10.1 contra el peor fondo del vidrio (`#070D16 @86 %` sobre blanco = `#2A2F37`, L = 0,028, verificado por QA): ≥ 7:1 en blanco y métricas no rojas, ≥ 4,5:1 en la familia roja (D-ART-01), ≥ 4,5:1 en texto de interfaz sobre `surface.card`. Otro test fija los hex de contrato del legacy (F-RUT-01, F-APP-06).

## Alternativas descartadas

- **USS como fuente y C# leyendo el USS**: no se puede testear en `dotnet` sin un parser de USS, y la escena y los gráficos necesitan los valores en C#.
- **Tokens en un JSON leído por ambos**: suma carga en tiempo de ejecución y pierde el chequeo del compilador sobre los nombres.

## Consecuencias

- Cambiar un color = editar `Tokens.cs` y correr `tools/tokens`; el test de sincronía y el de contraste avisan si algo queda mal.
- En la It. 1 se cierra el contrato de datos (hex del legacy); el tema completo se usa desde la It. 2.

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-05 | ADR nuevo: fuente única, generador con test de sincronía y test WCAG |

## Cambios de la It. 1 (implementación, WP-A)

| ID | Qué cambió |
|---|---|
| API | `Rgb`, `ColorToken` (`Contract`/`Hex`/`HexAlpha`, alfa en porcentaje entero sin redondear a byte), `LengthToken.Dp`, `DurationToken.Ms`, `EasingToken.CubicBezier` (solo x se limita a [0; 1]; la palabra clave de USS manda), `TypeScaleToken.Of`, `TokenCatalog` (reflexión solo para herramientas y tests), `UssTokenWriter`, `Contrast` (02-tech-design §4.2). `Tokens.cs` lo mantiene el Designer (122 tokens en la It. 1) |
| Roles | `ColorRole`: `UiText` ≥ 4,5 y `UiTextPrimary` ≥ 7 sobre `surface.card`; `HudValue` ≥ 7, `HudValueRed` y `HudText` ≥ 4,5 contra el peor vidrio. `ContrastTests.DesignerPairs` verifica además los 13 grupos de pares de 04-design-notes §4.3 (QA-08) |
| P-05 | Método: composición sRGB con el alfa sin redondear y canales en `double`; luminancia con umbral 0,04045; relación sin redondear. Peor vidrio (41,72; 46,88; 54,62) ≈ `#2A2F37`. Márgenes ≤ 0,02 en cuatro tokens del HUD: cualquier cambio de tono rompe el test |
| R-T1 | El test imprime también la mezcla lineal (lo que haría Unity con la UI en un proyecto Linear): split 2,82, blanco 5,43. Se mide en la It. 2 y se decide antes de la 4a (Designer: `forceGammaRendering`) |
| D-09 | `Icons.cs` y `MetricPalette` pasan a la It. 2 (la fuente de íconos se descarga ahí); en la It. 1 el ícono del tipo de paso es su nombre legacy como dato |
| Generador | `Tokens.uss`: orden ordinal, `#RRGGBB` o `rgba(R, G, B, α)`, `px`, `ms`, curvas solo con palabra clave, tipografía solo `--rm-<token>-size`; enteros sin decimales y el resto con `Fmt.DartToString`. Se regenera con `dotnet tools/dotnet/.artifacts/bin/RowMate.Tokens/release/RowMate.Tokens.dll` después del build (`--check` para solo verificar) |
