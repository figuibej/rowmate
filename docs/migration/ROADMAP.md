# Roadmap de migración RowMate → Unity

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-27 (revisado tras el gate de QA del plan y al especificar la It. 1) |
| Base | [Inventario maestro](product/FEATURE_INVENTORY.md) (158 IDs = 142 canónicos + 16 alias), [auditoría visual](design/LEGACY_VISUAL_AUDIT.md), [gate de QA del plan](QA_PLAN_REVIEW.md) y propuestas P-01 a P-10 del Architect ([ARCHITECTURE](architecture/ARCHITECTURE.md)) |
| Objetivo | Migrar **todo** `rowmate-legacy` a Unity 6 LTS + URP con paridad funcional exacta y look & feel realista estilo EXR, en **17 iteraciones** verticales (las 14 originales; la 4, la 6 y la 10 divididas en a/b) más un *spike* opcional. Cada una entra en el contexto de un agente |

---

## 0. Reglas del plan

1. **Vertical slices.** Cada iteración termina con algo que el usuario puede ver o probar (la *demo*), y se le muestra **antes** de empezar la siguiente. Ciclo por iteración (ver [CLAUDE.md](../../CLAUDE.md)): PO spec → Architect diseño (+ Designer) → QA gate de armonía → implementación → QA verificación → correcciones → `REPORT.md` → [STATUS.md](STATUS.md). Carpeta: `docs/migration/iterations/NN-slug/`.
2. **Una iteración dueña por ID.** Cada ID del inventario se cierra en **una** iteración (la que verifica todos sus criterios, salvo los [Apple] de la regla 8). Un alias se cierra con su canónico. Si otra iteración necesita una parte antes (por ejemplo, la tarjeta del pulsómetro con el sensor simulado), figura en "También toca" de la matriz (§4) y no cuenta como cierre.
3. **Tamaño.** ≤ ~2 500 líneas legacy por iteración (código vivo + tests a portar; el catálogo `.arb` cuenta como datos). Algunas iteraciones son chicas en líneas legacy pero grandes en trabajo nuevo de arte o plataforma: lo indica la columna "Esfuerzo nuevo".
4. **Primero usable con el simulador.** De la It. 2 a la It. 9 todo se prueba con el remo y el pulsómetro simulados, en cualquier plataforma. El hardware real entra en las It. 10a-11 y Strava en las It. 12-13.
5. **Mejoras EXR junto a lo que tocan.** Cada iteración incluye las mejoras de experiencia de sus funcionalidades (fichas "Mejora EXR" de los inventarios). La It. 14 queda para el pulido transversal.
6. **Decisiones de producto.** Cada iteración aplica las decisiones por defecto de [FEATURE_INVENTORY §4](product/FEATURE_INVENTORY.md) que la afectan y las decisiones de diseño de §6. Las marcadas "Confirmar = Sí" se le muestran al usuario en el gate de esa iteración.
7. **Textos.** Cada iteración pasa los textos de sus pantallas al catálogo es/en (Q-01), con el español idéntico al legacy salvo las correcciones de Q-02, y su `01-po-spec` fija la tabla exacta es/en de cada clave nueva. La It. 14 audita.
8. **Plataformas Apple (hoy no hay Mac, RA-02).** Cada iteración se verifica en Editor, Windows y Android. Lo que solo se puede ver en iOS o macOS se marca **[Apple]** en el spec y se verifica en la **tanda Apple (It. 10b)**, que se hace cuando el usuario tenga una Mac, en cualquier momento después de la 10a. Un ID se cierra en su iteración dueña con sus criterios no-Apple; sus criterios [Apple] quedan en la lista de la 10b (§4.1) o, si la iteración es posterior a la 10b, en la It. 14. Si un criterio [Apple] falla, el ID se reabre en esa tanda. La 10b escribe **todo** el código nativo de Apple de una vez (P-06), así que después la Mac solo hace falta para compilar y verificar.
9. **Rendimiento.** Desde la It. 6a, cada iteración con escena mide el objetivo de R-12 tal como lo fija la **tabla única de rendimiento** de [ARCHITECTURE §20](architecture/ARCHITECTURE.md) (QA-P-10): 60 fps sostenidos durante 30 min en el Android de gama media de referencia (nivel Medio), dentro de los topes de su nivel (§20.1) y de los topes comunes (§20.2). [ART_BIBLE §9](design/ART_BIBLE.md) no es otra tabla: solo reparte el presupuesto del arte dentro de esos topes. Si el usuario todavía no tiene ese teléfono, el criterio queda **NO VERIFICABLE** en el `REPORT.md` de la iteración y se arrastra a la It. 14. Nunca se da por cumplido sin medir.

### Leyenda de verificación

| Marca | Significa | Disponible hoy en la máquina |
|---|---|---|
| **[dotnet]** | `dotnet test` del Core puro fuera de Unity (`C:/Program Files/dotnet/dotnet.exe`) | Sí |
| **[Editor]** | Unity 6 LTS Editor: play mode, Unity Test Framework, capturas | **No** (lo tiene que instalar el usuario) |
| **[Device]** | Build en un dispositivo o equipo real: PC con Windows, teléfono Android de gama media | Parcial (Windows sí; el Android lo aporta el usuario) |
| **[Apple]** | Build y prueba en iPhone y Mac (necesita una Mac con Xcode) | **No** (RA-02); se juntan en la It. 10b o en la It. 14 |
| **[BLE-HW]** | Remo FTMS real (AMS-670B) y un pulsómetro real | Depende del usuario |
| **[Strava]** | Cuenta de Strava y app de API con credenciales | Depende del usuario |

### Prerrequisitos del usuario (bloquean demos, no el Core)

| Desde | Qué hace falta | Para qué | Si falta |
|---|---|---|---|
| It. 1 (opcional) | **Repositorio remoto en GitHub** con el proyecto subido | Que `ci-core.yml` corra en cada cambio | El CI queda NO VERIFICABLE: QA revisa el workflow y corre los mismos comandos en la PC |
| It. 2 | **Instalar Unity 6 LTS** (Hub) con soporte de build Android, iOS, Windows y macOS | Ver cualquier pantalla | Solo se verifica el Core con `dotnet` y la demo se limita a la salida de los tests |
| It. 2 | **Fuentes** Inter, Barlow Semi Condensed y JetBrains Mono (OFL) e **íconos** Material Symbols Rounded (Apache 2.0), con los pasos de [ASSET_SLOTS §3-§4](design/ASSET_SLOTS.md). Se versionan junto con su licencia | Tipografía de tablero y íconos definitivos | Fuente por defecto de Unity e íconos provisorios; la demo se ve "a medio vestir" |
| It. 2 (opcional, P-08) | Activar la licencia Personal de Unity y cargar el archivo `.ulf` como secreto del repositorio | CI de Unity (tests y builds) en cada iteración | Solo corre el CI de `dotnet` (`ci-core.yml`, desde la It. 1) |
| S-1 (opcional, P-07) | Un pulsómetro BLE y la PC con Windows con Bluetooth LE | Spike de Bluetooth nativo en Windows | Se saltea el spike; el riesgo se ve recién en la It. 10a |
| It. 6a | **Terrain Sample Asset Pack** (Asset Store de Unity, gratis, 1,6 GB; [ASSET_SLOTS §5.2](design/ASSET_SLOTS.md)) | Terreno y vegetación de orilla realistas | Texturas procedurales de terreno (digno, menos realista) |
| It. 6a | **Un teléfono Android de gama media** (clase Galaxy A54 / Pixel 6a, Android 8+ ARM64) con depuración USB | Medir 60 fps en las It. 6a-8 (R-12, regla 9); también sirve para el Bluetooth real de la 10a | Rendimiento NO VERIFICABLE hasta que haya teléfono (a más tardar, It. 14) |
| It. 7 (lista en la 6a, P-09) | Descargar de Mixamo los archivos exactos que el Designer fija en la It. 6a | Remero 3D | Maniquí procedural |
| It. 10a | Remo AMS-670B (u otro FTMS), el teléfono Android y la PC con Windows con Bluetooth LE | Bluetooth real en Android y Windows | Sin Bluetooth real |
| It. 10b | Una Mac con Xcode, un iPhone y una cuenta de Apple para firmar | Tanda Apple: iOS y macOS | Los criterios [Apple] quedan NO VERIFICABLE hasta que haya Mac |
| It. 11 | Un pulsómetro BLE (banda o reloj con app que transmita 0x180D) | Pulsómetro real | Sin pulsómetro real |
| It. 12 | Cuenta de Strava y app de API (client id/secret propios, inyectados al compilar) | Login, subida y sincronización | Strava no aparece (estado válido del legacy) |

---

## 1. Vista general

| N | Slug | Iteración | IDs (canónicos + alias) | Depende de | Verificación | Líneas legacy | Esfuerzo nuevo |
|---|---|---|---|---|---|---|---|
| 1 | `01-fundaciones` | Fundaciones: proyecto Unity, Core puro y piezas base | 20 (17 + 3) | — | [dotnet] | ~1 500 | Medio |
| 2 | `02-dispositivo-simulador` | Shell, Dispositivo en vivo y simulador | 16 (14 + 2) | 1 | [dotnet] [Editor] | ~2 050 | Medio |
| S-1 | `s1-spike-ble-windows` | *Spike* opcional: Bluetooth nativo en Windows sin Unity | 0 | 1 | [dotnet] [BLE-HW] | 0 | Bajo |
| 3 | `03-rutinas` | Rutinas: lista, editor y base local | 11 (10 + 1) | 1, 2 | [dotnet] [Editor] | ~1 110 | Medio |
| 4a | `04a-motor-hud-funcional` | Entrenar: motor de sesión, guardado incremental, reposo y HUD funcional | 19 (14 + 5) | 1, 2, 3 | [dotnet] [Editor] | ~1 260 | Medio |
| 4b | `04b-hud-exr` | HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema | 7 | 4a | [dotnet] [Editor] | ~650 | Alto |
| 5 | `05-series-parciales` | Series en vivo: parciales de 500 m y repeticiones | 3 | 4b | [dotnet] [Editor] | ~1 030 | Bajo |
| 6a | `06a-escena-lago` | Escena: Core de escena, cielo, luz, hora, Lago alpino y agua degradada | 8 | 2, 4b | [dotnet] [Editor] [Device] | ~1 000 | Muy alto |
| 6b | `06b-agua-avanzada` | Agua avanzada, reflejos, estela, niebla, vidrio con desenfoque y rendimiento | 3 | 6a | [dotnet] [Editor] [Device] | ~350 | Muy alto |
| 7 | `07-remero-palada` | Remero, bote y palada biomecánica | 5 | 6b | [dotnet] [Editor] [Device] | ~470 | Muy alto |
| 8 | `08-entornos-selector` | Río, Costa, Regata y selector de escenario | 7 | 6b, 7 | [dotnet] [Editor] [Device] | ~650 | Alto |
| 9 | `09-historial` | Historial y detalle con gráficos | 12 | 3, 4a | [dotnet] [Editor] | ~780 | Medio |
| 10a | `10a-ble-android-windows` | Bluetooth real en Android y Windows: capa nativa y remo FTMS | 7 | 2, 4a | [dotnet] [Editor] [Device] [BLE-HW] | ~650 | Alto |
| 10b | `10b-tanda-apple` | Tanda Apple: todo lo nativo de iOS y macOS y los criterios [Apple] | 0 (verifica los [Apple], §4.1) | 10a | [Device] [Apple] [BLE-HW] | ~50 | Alto |
| 11 | `11-ble-pulsometro` | Bluetooth real: pulsómetro | 10 | 10a | [dotnet] [Editor] [Device] [BLE-HW] | ~720 | Medio |
| 12 | `12-strava-subida` | Strava: conexión, subida y TCX | 17 (14 + 3) | 4b, 9 | [dotnet] [Editor] [Device] [Strava] | ~1 190 | Alto |
| 13 | `13-strava-sincronizacion` | Strava: sincronización, importación y Perfil completo | 9 (7 + 2) | 12 | [dotnet] [Editor] [Strava] | ~790 | Medio |
| 14 | `14-pulido-plataformas` | Pulido EXR, identidad, plataformas y CI | 4 | todas | [dotnet] [Editor] [Device] [Apple] | ~300 | Medio |
| | | **Total** | **158 (142 + 16)** | | | **~14 550** | |

Camino crítico: 1 → 2 → 3 → 4a → 4b → 5 → 6a → 6b → 7 → 8. En paralelo lógico: 4a → 9 → 12 → 13 y 4a → 10a → 11. La 10b es **flotante**: se hace en cuanto haya Mac, después de la 10a y antes de la 14; cuanto más tarde llegue, más criterios [Apple] verifica de una vez. El Bluetooth real (10a → 11) solo depende de 2 y 4a, así que se puede adelantar si el usuario quiere usar su remo antes. El *spike* S-1 es opcional, entre la 2 y la 3.

---

## 2. Iteraciones

### It. 1 — Fundaciones: proyecto Unity, Core puro y piezas base (`01-fundaciones`)

**Objetivo.** Dejar el proyecto Unity 6 LTS + URP estructurado (Core puro separado y testeable fuera de Unity, servicios y presentación) y portar al Core toda la lógica de dominio que no depende de pantallas: lectura del remo y sus formatos, parser FTMS, parser de pulso, filtro de búsqueda, física del simulador, pasos, rutinas con series y progresiones, sesión, telemetría, estadísticas y el catálogo de textos es/en. Suma las piezas base que usan todas las iteraciones siguientes (P-01).

**Incluye.**
- BLE: F-BLE-01 (+ F-RUT-15), F-BLE-02, F-BLE-03, F-BLE-14.
- HRM: F-HRM-01.
- RUT: F-RUT-01 (+ F-APP-07), F-RUT-02, F-RUT-03, F-RUT-04, F-RUT-05, F-RUT-06, F-RUT-07, F-RUT-16, F-RUT-17, F-RUT-18 (+ F-HIS-04).
- APP: F-APP-09, F-APP-10.
- Piezas base sin ID propio (P-01 ampliada por QA, [QA_PLAN_REVIEW §4](QA_PLAN_REVIEW.md) y §9). Son del Architect y se detallan en ARCHITECTURE: reloj programable con su versión falsa para tests, valores observables y eventos, paridad numérica con Dart (decimales, redondeo y fechas, QA-P-02), identificadores y resultados de búsqueda de Bluetooth, generador aleatorio inyectable, registro de diagnóstico, catálogo es/en convertido con plurales, fuente única de colores (QA-P-05), oráculo Dart de vectores dorados (QA-P-23), soporte de tests (QA-P-03) y el CI de `dotnet test` (`ci-core.yml`).
- Proyecto Unity: preparación mínima del proyecto (QA-P-25), sin niveles de calidad definitivos (QA-P-10). Android mínimo 8.0 ARM64 (Q-90) e identificadores provisionales según R-14.

**También toca.** F-APP-16 (nombre "RowMate" e identificadores provisionales), F-APP-19 (`ci-core.yml`).

**Tests legacy a portar (19 casos).** `rowing_simulator_test` (7), `heart_rate_parser_test` (6), `scan_filter_test` (2), `routine_positions_test` (1), `session_stats_test` (3).
**Criterios nuevos (huecos del legacy).**
- La tabla de 8 paquetes FTMS de F-BLE-02.
- Los formatos de F-BLE-01 (`02:13`, `--:--`, `100:00`, `1:01:01`).
- Los casos 1-15 de RUT §5.3: progresiones de SPM, vatios y split; repeticiones vacías y 0; series contiguas; etiquetas de duración y objetivos; resumen; P99; redondeo del pulso. Los casos 9-11 usan los textos exactos es/en de [routines-models §3.1 y F-RUT-03/04/05](product/inventory/routines-models.md) (QA-P-04).
- Catálogo: las 159 claves del legacy existen en es y en con los mismos parámetros; 4 cambian su texto por Q-02 ([FEATURE_INVENTORY §4.1.1](product/FEATURE_INVENTORY.md)); se suman 15 claves nuevas (Q-01), 174 en total; un idioma distinto de es cae en en.
- Vectores dorados del oráculo Dart (formatos de decimales de QA §3.2, etiquetas y P99) que pasan igual en `dotnet`.

**Demo.** La consola muestra `dotnet test` en verde con los 19 casos legacy portados y los criterios nuevos, y el CI (`ci-core.yml`) corre lo mismo (si ya hay repositorio remoto; prerrequisito opcional). El árbol del proyecto Unity (URP, separación Core / servicios / presentación) queda listo para abrirse en cuanto el usuario instale el Editor.

**Spec.** [iterations/01-fundaciones/01-po-spec.md](iterations/01-fundaciones/01-po-spec.md): 139 criterios `AC-…`, los 19 casos legacy uno por uno y el checklist de paridad.

**Verificación.** [dotnet] obligatorio. [Editor] opcional: abrir el proyecto sin errores cuando esté instalado. Lo que depende de Unity (proyecto, paquetes, preparación inicial) queda NO VERIFICABLE hasta la It. 2 y QA lo revisa de forma estática (QA-P-25).

**Tamaño.** ~1 500 líneas: `core/models/rowing_data.dart` 63, `core/bluetooth/ftms_parser.dart` 142, `heart_rate_parser.dart` 41, `scan_filter.dart` 20, `core/dev/rowing_simulator.dart` 83, `core/models/interval_step.dart` 181, `routine.dart` 138, `workout_session.dart` 183, `shared/theme.dart` (tipos de paso, ~30), `l10n/app_es.arb` + `app_en.arb` 416 (datos), tests 203. Las piezas base son trabajo nuevo.

**Mejoras EXR.** No visibles (es Core). Paridad numérica exacta: la base de todo lo que se va a ver.

**Decisiones aplicadas.** Q-01 (resumen y etiquetas), Q-02 (4 claves del catálogo y plurales del resumen), Q-05, Q-07, Q-10 (el `📈` se mantiene en el texto), Q-14 (P99 idéntico), Q-38, Q-48, Q-69, Q-90, R-14.

**Fuera de esta iteración.** Toda pantalla; la lógica pura de la escena (It. 6a-8), del motor (It. 4a-5), del pulsómetro real (It. 11) y de Strava (It. 12-13).

---

### It. 2 — Shell, Dispositivo en vivo y simulador (`02-dispositivo-simulador`)

**Objetivo.** Primera app usable: arranca en la pestaña Dispositivo con el remo simulado conectado y las métricas en vivo, la tarjeta del pulsómetro funciona con el sensor simulado y el panel del simulador controla todo. Se fijan el sistema visual (tema oscuro, paleta por métrica, tipografía) y la barra de pestañas.

**Incluye.**
- APP: F-APP-01, F-APP-02, F-APP-05, F-APP-06, F-APP-08 (+ F-RUT-19, descarte).
- BLE: F-BLE-07, F-BLE-11, F-BLE-13 (+ F-APP-18), F-BLE-15, F-BLE-16.
- HRM: F-HRM-04, F-HRM-12, F-HRM-13, F-HRM-15.

**También toca.** F-BLE-05 (estado vacío y botón "Buscar dispositivos" con el simulador), F-BLE-08 (caída simulada de 3 s), F-BLE-10 (señal de remo conectado), F-HRM-02/03/08/10 (con el sensor simulado), F-SCN-23 (control "Hora escena" del panel, sin escena todavía), F-APP-16 (nombre "RowMate"), F-APP-19 (`ci-unity.yml` si hay licencia, P-08).

**Tests legacy a portar (15 casos).** `simulated_ble_service_test` (3), `simulated_heart_rate_service_test` (3), `simulator_overlay_test` (2), `heart_rate_card_test` (4), `heart_rate_provider_test` (3). Los 4 casos de `heart_rate_card_test` se portan acá con los servicios simulados. Los de :121, :142 y :161 (exclusión de búsquedas y vista de búsqueda en horizontal) se vuelven a correr en la It. 10a contra el remo real con adaptador falso (QA-P-27).

**Demo.** Con el build de simulador: la app abre en Dispositivo con "Remo simulado · Conectado" y la grilla SPLIT/SPM/VATIOS/DISTANCIA actualizándose cada segundo (2 columnas con la ventana de hasta 640 dp de ancho, como un teléfono vertical o una ventana de 640 × 360; 3 si es más ancha, como un teléfono horizontal o la ventana de Windows de 1280 × 720: depende del ancho, no de la orientación). En la tarjeta del pulsómetro: Buscar → "Pulsómetro simulado" → Conectar → bpm en vivo → Desconectar. Desde el panel: presets Suave/Medio/Fuerte, ± vatios y ritmo, "Remando" apagado, "Simular desconexión" (3 s en la vista de búsqueda y vuelve) y "Simular caída del pulsómetro" ("reconectando..."). Cinta "SIMULADOR" visible siempre. Barra con Dispositivo / Workout / Rutinas / Historial (las tres últimas con contenido provisorio). Idioma es/en según el sistema. Con las fuentes y los íconos descargados, la tipografía y los íconos son los definitivos.

**Verificación.** [dotnet] servicios simulados con reloj falso, estado de presentación del pulsómetro y reglas del panel. [Editor] UI, landscape 640×360 sin desbordes, panel en 800×360. Si hay licencia: CI de Unity en verde (P-08).

**Tamaño.** ~2 050 líneas: `main.dart` 181, `shared/theme.dart` 69, `features/device/device_screen.dart` (~250 de 480: vista conectada y estado vacío), `device_provider.dart` (~80 de 141), `heart_rate_card.dart` 238, `heart_rate_provider.dart` 117, `core/bluetooth/simulated_ble_service.dart` 120, `simulated_heart_rate_service.dart` 156, `core/dev/simulator_overlay.dart` 212, `dev_config.dart` 5, tests 612.

**Mejoras EXR.** Tablero de instrumentos con profundidad y cifras tabulares que no "bailan"; cada valor entra con una transición breve (el dato mostrado siempre es el real); el pulso como signo vital (corazón que late a la frecuencia real, `#EF476F`); panel del simulador con la estética de la app; barra de pestañas tipo vidrio con selección que se desliza.

**Decisiones aplicadas.** Q-03, Q-08, Q-33, Q-40, Q-57, D-ART-03 (§6).

**Fuera de esta iteración.** Búsqueda real con lista, conexión, permisos y diagnóstico (It. 10a); memoria y reconexiones reales del pulsómetro (It. 11); Perfil (It. 12-13).

---

### S-1 (opcional) — *Spike* de Bluetooth nativo en Windows (`s1-spike-ble-windows`)

**Objetivo.** Bajar temprano el riesgo de la capa nativa propia (RA-03): leer un pulsómetro real desde la PC con Windows, usando la biblioteca nativa de Windows y una consola de `dotnet`, **sin Unity** (P-07, QA-P-24).

**Incluye.** Ningún ID (es riesgo técnico). Usa F-HRM-01 (parser) y F-BLE-03 (filtro de Windows en la app), ya portados en la It. 1.

**Demo.** La consola lista los pulsómetros cercanos (filtrados por 0x180D) y muestra el bpm en vivo durante un minuto.

**Verificación.** [dotnet] [BLE-HW] (pulsómetro real).

**Cuándo.** Entre la It. 2 y la It. 3, solo si el usuario tiene un pulsómetro y quiere hacerlo. Si no se hace, el riesgo se ve en la It. 10a.

---

### It. 3 — Rutinas: lista, editor y base local (`03-rutinas`)

**Objetivo.** El usuario crea, edita y borra rutinas con pasos sueltos y series ×N con progresiones, y quedan guardadas entre aperturas. Nace la base local con las 4 tablas, y la base separada del simulador.

**Incluye.**
- RUT: F-RUT-08 (+ F-HIS-18), F-RUT-09, F-RUT-10, F-RUT-11, F-RUT-12, F-RUT-13, F-RUT-14.
- HIS·DB: F-HIS-17, F-HIS-20.
- APP: F-APP-03.

**También toca.** F-APP-01 (las rutinas se cargan al arrancar).

**Tests legacy a portar.** Ninguno (no hay). **Criterios nuevos:**
- Casos 16-21 de RUT §5.3: nombre vacío → `editorNameEmpty`; sin pasos → `editorNoSteps`; "Serie" agrega Trabajo 120 s + Descanso 60 s ×3; repeticiones "abc"/"0"/"−2"/"5"; agregar paso a una serie; diálogo con campos vacíos; crear arriba, editar sin reordenar, borrar con confirmación.
- Persistencia y reapertura.
- Base `rower_app_dev.db` separada en simulador.
- La base nace ya con el formato propio v8 (P-02).
- Rango de los números del paso y sus progresiones (Q-92, de QA-V-11 de la It. 1): tope por campo, mensaje es/en y ninguna rutina guardada que haga fallar la expansión.

**Demo.** Pestaña Rutinas: crear "Escalera SPM" con calentamiento + serie ×3 (trabajo 300 s 18 SPM +2/rep, descanso 120 s) + enfriamiento; ver su resumen (`1 serie · 1 descanso`); editarla; cerrar y volver a abrir la app y seguir viéndola; borrarla con confirmación.

**Verificación.** [dotnet] repositorio de rutinas sobre la base y reglas del editor que se puedan aislar. [Editor] pantallas y diálogos.

**Tamaño.** ~1 110 líneas: `features/routines/routine_editor_screen.dart` 739, `routines_screen.dart` 120, `routines_provider.dart` 53, `core/database/database_service.dart` (~200 de 340: archivo, esquema, rutinas, base del simulador).

**Mejoras EXR.** Editor visual: bloques de serie con color por tipo de paso, chip ×N claro, transiciones al agregar o quitar pasos; lista de rutinas con tarjetas legibles.

**Decisiones aplicadas.** Q-01 (textos del editor y `×N` en es/en), Q-10, Q-11 (sin importar la base legacy: base propia v8 desde cero, P-02; la importación queda diseñada y sin implementar; R-14), Q-13, Q-17, Q-18, Q-19, Q-25, Q-26, Q-92.

**Fuera de esta iteración.** Mejoras del editor (BKL-12).

---

### It. 4a — Entrenar: motor de sesión, guardado incremental, reposo y HUD funcional (`04a-motor-hud-funcional`)

**Objetivo.** Desde la pestaña Workout el usuario arranca un entrenamiento libre o una rutina y rema con un HUD **funcional**. Ve las métricas con semáforo de objetivos, el paso en curso con su cuenta regresiva y "a continuación", y tiene pausa/reanudar/terminar con confirmación. La sesión y su telemetría se guardan mientras rema. El vidrio usa el respaldo translúcido sin desenfoque (token `glass.fallback`, `#070D16 @86 %`, ART_BIBLE §3.2) y el fondo es provisorio hasta la It. 6a.

**Incluye.**
- WKT: F-WKT-01, F-WKT-02, F-WKT-03, F-WKT-04, F-WKT-05 (+ F-HRM-14), F-WKT-06 (+ F-HIS-01, F-HIS-02, F-HIS-03), F-WKT-09, F-WKT-10, F-WKT-11 (+ F-APP-12), F-WKT-12, F-WKT-20, F-WKT-23 (descarte).
- BLE: F-BLE-10.
- APP: F-APP-14 (criterios [Apple] en la 10b).

**También toca.** F-WKT-13, F-WKT-16, F-WKT-17 (versión funcional del inmersivo, la barra superior y las tarjetas, que cierra la 4b), F-HIS-17 / F-HIS-19 (alta y cierre de sesiones y puntos), F-SCN-01 (capa de fondo provisoria).

**Tests legacy a portar (11 casos).** `workout_provider_hr_test` (4), `step_target_tracker_test` (6), `series_panels_test` caso "formatos" (1).
**Criterios nuevos.**
- Fases y reloj (F-WKT-01/02).
- Avance y fin con progresiones (F-WKT-03): al terminar la rutina, la sesión se guarda y se vuelve a la vista de reposo (el diálogo de completado llega en la 4b).
- Estado del paso con "por terminar" y "a continuación" ≤ 15 s (F-WKT-04).
- Telemetría cada 5 s activos + último punto + P99 (F-WKT-06), guardada en forma incremental (Q-12).
- Semáforo (F-WKT-10) con los colores de §6 (D-ART-01).
- Formatos 1-15 de F-WKT-11.
- Comportamiento en segundo plano según Q-91 (QA-P-06): el spec de la 4a lo fija con criterios verificables.

**Demo.** Con el simulador:
- (a) "Entrenamiento libre": HUD funcional con SPM grande, Split, Watts, kcal, BPM, tiempo y distancia sobre tarjetas con el respaldo del vidrio `glass.fallback` (`#070D16 @86 %`, ART_BIBLE §3.2) y el borde legacy; pausa y reanudación; "Terminar" con confirmación.
- (b) Una rutina de la It. 3: paso en curso con su nombre y cuenta regresiva `M:SS`, "Up next" en los últimos 15 s, y fin automático que guarda la sesión. Fuera de objetivo, el valor de la tarjeta se pone en rojo: `#FF5F59` con flecha ▲/▼ si se aprueba D-ART-01, o `#FF4444` si no. Subir los vatios desde el panel lo corrige.
- (c) Cerrar la app a mitad de una sesión: la sesión queda guardada con sus puntos (se verá en el Historial de la It. 9).
- Sin remo conectado (Simular desconexión), el aviso naranja deshabilita el inicio. La pantalla no se apaga.

**Verificación.** [dotnet] motor completo, trackers, formatos, fusión del pulso y guardado incremental. [Editor] HUD funcional vertical y horizontal, diálogo de terminar. [Device] Windows: pantalla siempre encendida.

**Tamaño.** ~1 260 líneas: `features/workout/workout_provider.dart` 328, `step_target_tracker.dart` 71, `workout_screen.dart` (~200 vivas; 203-990 es código muerto), `immersive_workout_screen.dart` (~350 de 1 019: estructura mínima, tarjetas, banda, controles y diálogo de terminar), `database_service.dart` (~40: sesiones y puntos), tests ~275.

**Mejoras EXR.** HUD legible a 1,5 m con cifras tabulares (escala de D-ART-02); tarjeta de objetivo que conserva su color semántico (Q-60) y muestra el objetivo en chico (D-ART-09).

**Decisiones aplicadas.** Q-01 (textos del HUD en es/en), Q-02 (`workoutStartTitle` y `workoutStartContent` con `¿`, ya corregidos desde la It. 1), Q-06, Q-12 (telemetría incremental), Q-13, Q-15, Q-51, Q-55, Q-56, Q-57, Q-58, Q-60, Q-91, D-ART-01, D-ART-02, D-ART-09 y respaldo del vidrio (QA-P-17), todas en §6. Strava se comporta como "no configurado" hasta la It. 12, que es un estado válido del legacy.

**Fuera de esta iteración.** Barra de pasos con píldoras, reloj de pared, diálogo de completado, transiciones e inmersivo del sistema (It. 4b); parciales y repeticiones (It. 5); escena real (It. 6a); vidrio con desenfoque (It. 6b); selector de escenario en la vista de reposo (It. 8); subida a Strava al terminar (It. 12); aviso de caída del remo (It. 10a).

---

### It. 4b — HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema (`04b-hud-exr`)

**Objetivo.** Llevar el HUD de la 4a al nivel EXR y completar la pantalla inmersiva del legacy: barra de pasos con píldoras animadas, barra superior del entrenamiento libre con chip de fase, reloj de pared, diálogo de rutina completada, entrada y salida del inmersivo como "sumergirse" y las barras del sistema ocultas mientras se rema.

**Incluye.** WKT: F-WKT-13, F-WKT-14, F-WKT-15, F-WKT-16, F-WKT-17, F-WKT-19, F-WKT-21.

**También toca.** F-APP-15 (barras del sistema ocultas al entrar al inmersivo y visibles al salir; se cierra en la It. 14), F-WKT-04 y F-WKT-09 (pulso de la píldora y sello de resultado, D-ANI-05).

**Tests legacy a portar (1 caso).** `step_pill_test` (1).
**Criterios nuevos.**
- Estados visuales de la píldora: pendiente / en curso / hecho / no cumplido.
- El último paso queda "en curso" (Q-52).
- Al terminar a mano nunca aparece el diálogo de completado (Q-53).
- Píldoras del mismo ancho (Q-59).
- Las animaciones nunca adelantan ni frenan el reloj ni las métricas (ANIMATION_BIBLE §0.1).

**Demo.** Con el simulador y una rutina: barra de pasos con píldoras que se llenan, pulso de la píldora en los últimos 5 s y cuenta grande en los últimos 3 s. Al cerrar cada paso, sello "Objetivo cumplido / No alcanzado" (si se aprueba D-ANI-05). Al terminar, el diálogo de rutina completada. En entrenamiento libre, barra superior con chip de fase y reloj de pared. Entrar al inmersivo oculta las barras del sistema con una transición y salir las devuelve.

**Verificación.** [dotnet] reglas de estado de las píldoras y del diálogo. [Editor] HUD completo vertical y horizontal, transiciones, diálogos.

**Tamaño.** ~650 líneas: `features/workout/step_pill.dart` 105, `immersive_workout_screen.dart` (~500: barra de pasos, barra libre, reloj, diálogo de completado, transiciones), tests ~54.

**Mejoras EXR.** Relleno de la píldora y valores interpolados visualmente (sin cambiar el dato); pulso de la píldora en los últimos 5 s; entrada al inmersivo como "sumergirse" (las barras del sistema se desvanecen con la transición).

**Decisiones aplicadas.** Q-01, Q-52, Q-53 (parte del diálogo de completado), Q-59, D-ANI-05 (§6).

**Fuera de esta iteración.** Vidrio con desenfoque (It. 6b); subida a Strava desde el diálogo (It. 12).

---

### It. 5 — Series en vivo: parciales de 500 m y repeticiones (`05-series-parciales`)

**Objetivo.** En los pasos de trabajo el remero ve sus parciales de 500 m (vuelta en curso y las 4 últimas) y, dentro de una serie, la comparación de repeticiones anteriores, en paneles colapsables que recuerdan su estado.

**Incluye.** WKT: F-WKT-07, F-WKT-08, F-WKT-18.

**Tests legacy a portar (15 casos).** `series_tracker_test` (12), `series_panels_test` (los 3 casos restantes: colapsados por defecto, preferencia guardada, sin serie no hay panel de repeticiones).

**Demo.** Con el simulador, una rutina con serie ×3 de trabajo 1 000 m: panel "Parciales 500 m" con la vuelta en curso (`0:55.0 207 m`) y las anteriores (la más reciente primero); al pasar a la repetición 2, "Series anteriores" muestra `Rep 1 2:13 452 m`. Colapsar los paneles, cerrar la app y verlos colapsados al volver (`immersive.lapsExpanded`, `immersive.repsExpanded`). "Simular desconexión" no rompe los parciales (saneamiento de saltos).

**Verificación.** [dotnet] trackers de parciales y repeticiones. [Editor] paneles en vertical y horizontal.

**Tamaño.** ~1 030 líneas: `features/workout/series_tracker.dart` 183, `series_panels.dart` 392, `immersive_workout_screen.dart` (~170: integración de los paneles), tests ~287.

**Mejoras EXR.** Paneles de vidrio que se despliegan con suavidad, ancho acotado para no tapar el bote; cada vuelta nueva entra con una animación breve.

**Decisiones aplicadas.** Q-01 (textos de los paneles en es/en, español idéntico), Q-06.

---

### It. 6a — Escena: Core de escena, cielo, luz, hora, Lago alpino y agua degradada (`06a-escena-lago`)

**Objetivo.** El fondo provisorio se reemplaza por una escena 3D completa y usable estilo EXR sobre el Lago alpino, con el agua en su modo degradado (R-04). Tiene cielo con hora del día, sol, luna, estrellas y nubes, luz que cambia con la hora real (o la forzada del simulador), y la orilla del lago con terreno, vegetación y objetos en las **mismas posiciones que el legacy** (Q-61). El bote avanza a la velocidad que dicta el split, con un bote provisorio.

**Incluye.** SCN: F-SCN-02, F-SCN-03, F-SCN-09, F-SCN-10, F-SCN-11, F-SCN-14, F-SCN-15, F-SCN-23.

**También toca.** F-SCN-01 (composición de la escena con el agua degradada; se cierra en la 6b), F-SCN-13 (agua degradada; la disponibilidad del agua avanzada se cierra en la 6b), F-SCN-19 y F-SCN-20 (silueta de montañas y objetos del lago), F-SCN-21 (el lago es el escenario por defecto), F-WKT-13 (capa de escena del inmersivo), F-BLE-16 ("Hora escena").

**Tests legacy a portar.** `scene_camera_test` (4), `scene_state_test` (casos de velocidad y distancia :14, :26 y :55; este último se porta como contrato aunque el agua nueva no lo use, QA-P-28), `time_of_day_test` (6), `environment_test` (casos genéricos y lago), `sky_shore_painter_test` (casos de cielo y lago), `water_painter_test` (:11, agua de respaldo), `scene_view_test` (los 4 del lago: 2 tamaños × 12 h y 23 h).
**Criterios nuevos.**
- Vectores dorados del generador aleatorio del legacy, del oráculo: `Random(42)` → `0.15092545597797424`, `0.6041479725361217`, `nextInt(1000) = 77`; semilla del lago con k = 7 → `0.854835531354132`.
- Paisaje del lago idéntico al legacy a la misma distancia (Q-61).
- Estrellas solo con ambiente ≤ 0,85 (Q-62).
- Salida y puesta graduales del sol y la luna (Q-63).
- 60 fps en el Android de gama media con el agua degradada (regla 9).

**Demo.** Entrenamiento libre con el simulador sobre el Lago alpino con el agua degradada. El paisaje es el mismo que en el legacy a la misma distancia. Desde el panel, "Hora escena" a las 6, 12, 18 y 22 h cambia luz, cielo y agua (amanecer, mediodía, atardecer, noche con luna y estrellas) con transiciones graduales. Medición de cuadros por segundo en el teléfono de referencia.

**Verificación.** [dotnet] cámara, velocidad y distancia, hora y paleta, sol y luna, generación determinista de la orilla del lago. [Editor] escena y capturas. [Device] Android de gama media (rendimiento) y Windows.

**Tamaño.** ~1 000 líneas: `scene/scene_camera.dart` 46, `scene_state.dart` (~50 de 87), `time_of_day.dart` 117, `environment.dart` 129, `environments/lake_environment.dart` 86, `scene_settings.dart` (~15), `painters/sky_painter.dart` 124, `painters/shore_painter.dart` (~150 de 289), `water_painter.dart` (~40: respaldo), tests ~260.

**Mejoras EXR.** Orilla con terreno y vegetación (nada "flota" en el agua); cámara de persecución con la misma posición del legacy y una "respiración" sutil; horizonte que se acerca en lugar de deslizarse de costado (Q-65); nubes que se mueven con el viento y no de costado al avanzar (D-ART-06).

**Decisiones aplicadas.** Q-61 (revisada: paisaje idéntico), Q-62, Q-63, Q-65, Q-67, Q-69, D-ART-05, D-ART-06, D-ART-08 (§6). El Designer entrega en el diseño de esta iteración la lista exacta de archivos de Mixamo para la It. 7 (P-09).

**Fuera de esta iteración.** Agua avanzada y vidrio con desenfoque (It. 6b); remero y palada (It. 7); los otros 3 escenarios y el selector (It. 8).

---

### It. 6b — Agua avanzada, reflejos, estela, niebla, vidrio con desenfoque y rendimiento (`06b-agua-avanzada`)

**Objetivo.** El agua pasa a ser la protagonista: ondas, reflejo del cielo y de la orilla, brillo del sol, estela detrás del bote y niebla por distancia. Si el equipo no la soporta, la app usa sola el agua degradada de la 6a y la sesión sigue usable. El vidrio del HUD suma el desenfoque de la escena (P-04). Se cierra la composición de la escena y su presupuesto de rendimiento.

**Incluye.** SCN: F-SCN-01, F-SCN-12, F-SCN-13.

**También toca.** F-WKT-17 (tarjetas con desenfoque de la escena), F-SCN-03 (velocidad que alimenta la estela).

**Tests legacy a portar.** `scene_view_test` (los 4 del lago otra vez, ahora con el agua avanzada).
**Criterios nuevos.**
- Elección del agua avanzada o de la degradada según el equipo, y caída al degradado sin cortar la sesión (F-SCN-13).
- El patrón de olas no salta a los 4096 m (Q-68).
- Desenfoque del vidrio ≤ 0,6 ms en el nivel Medio (tabla única de ARCHITECTURE §20.1).
- 60 fps sostenidos durante 30 min en el Android de gama media (R-12, regla 9).
- Captura de aceptación visual: lago × 6, 12, 18 y 22 h.

**Demo.** Entrenamiento libre sobre el lago: el agua refleja el cielo y la orilla, el sol brilla en el agua, la estela queda detrás del bote y la niebla esfuma lo lejano. Las tarjetas del HUD dejan ver la escena desenfocada detrás. Forzar el modo degradado (o usar un equipo sin agua avanzada) muestra el agua de la 6a y la sesión sigue. Captura lago × 4 horas y medición de rendimiento en el teléfono de referencia.

**Verificación.** [dotnet] parámetros del agua y reglas de disponibilidad. [Editor] escena, capturas. [Device] Android de gama media (rendimiento) y Windows.

**Tamaño.** ~350 líneas: `water_painter.dart` (~68), `water_shader.dart` 23, `shaders/water.frag` 98, `scene_view.dart` 125, tests ~40. Esfuerzo nuevo muy alto (agua, reflejos, desenfoque).

**Mejoras EXR.** Agua protagonista con reflejos del cielo y de la orilla; niebla por distancia y recorte suave de los objetos cercanos; vidrio real sobre la escena.

**Decisiones aplicadas.** Q-67, Q-68.

**Fuera de esta iteración.** Remero y palada (It. 7); los otros 3 escenarios y el selector (It. 8).

---

### It. 7 — Remero, bote y palada biomecánica (`07-remero-palada`)

**Objetivo.** Un remero 3D articulado rema en un single al ritmo real del SPM, con la secuencia biomecánica piernas → tronco → brazos, remos con giro de pala, salpicadura en el ataque, charcos de pala, cabeceo del bote y sombra según el astro.

**Incluye.** SCN: F-SCN-04, F-SCN-05, F-SCN-06, F-SCN-07, F-SCN-08.

**Tests legacy a portar.** `scene_state_test` (casos de fase de palada y charcos :37, :46, :60, :69), `stroke_cycle_test` (4), `boat_painter_test` (1 + 2 tamaños).
**Criterios nuevos.** Las reglas legacy de los charcos se mantienen con D-ANI-02: 2 por palada, el izquierdo primero, 4 s de vida, fijos en el mundo y ninguno en pausa. 60 fps con remero en el Android de gama media (regla 9).

**Demo.** Con el simulador: a 20 spm la palada es lenta y con recuperación larga; con el preset Fuerte (30 spm) se acelera, y siempre que el drive mueve el bote, la escena acompaña. Al pausar, la palada se congela y el bote desacelera. Charcos en pares que se desvanecen, salpicadura en cada ataque, sombra que cambia con la hora.

**Verificación.** [dotnet] fase de palada, secuencia biomecánica canónica, cabeceo, charcos y el mapeo de ritmo por SPM. [Editor] animación, slots del remero (Mixamo) con el reemplazo procedural. [Device] Android de gama media (rendimiento).

**Tamaño.** ~470 líneas: `scene/scene_state.dart` (~37), `stroke_cycle.dart` 51, `painters/boat_painter.dart` 268, tests ~116. Esfuerzo nuevo muy alto (rig, IK, materiales).

**Mejoras EXR.** Ritmo drive/recuperación según el SPM (Q-70), ángulos de remo de sculler, giro de pala continuo (sin saltos), *surge* visual del bote dentro de la palada, gotas que caen de la pala en la recuperación, manos sobre el mango por IK. Nada cambia la distancia ni las métricas.

**Decisiones aplicadas.** Q-66 (cámaras extra y personalización quedan en backlog), Q-70, D-ART-04, D-ANI-01, D-ANI-02, D-ANI-03, D-ANI-04 (§6).

---

### It. 8 — Río, Costa, Regata y selector de escenario (`08-entornos-selector`)

**Objetivo.** Completar los 4 escenarios del legacy con look realista (Río urbano, Costa, Canal de regata) con sus siluetas lejanas y todo el catálogo de objetos de orilla, y el selector con miniaturas en la vista de reposo de Workout, que recuerda la elección.

**Incluye.** SCN: F-SCN-16, F-SCN-17, F-SCN-18, F-SCN-19, F-SCN-20, F-SCN-21, F-SCN-22.

**También toca.** F-WKT-12 (sección "Escenario" de la vista de reposo), F-SCN-12 (oleaje de la costa), F-SCN-14 (orillas de los otros escenarios).

**Tests legacy a portar.** `environment_test` (casos de río, costa y regata), `sky_shore_painter_test` (casos restantes), `water_painter_test` (:27, oleaje de la costa 0,8), `environment_picker_test` (2), `scene_view_test` (los 12 restantes: río, costa y regata × 2 tamaños × 12 h y 23 h, como criterio de F-SCN-16/17/18).
**Criterios nuevos.** Paisaje idéntico al legacy en los 4 escenarios (Q-61). 60 fps en el escenario más cargado en el Android de gama media (regla 9).

**Demo.** En Workout aparecen 4 miniaturas (Lago alpino, Río urbano, Costa, Canal de regata). Al elegir "Río urbano", el entrenamiento rema bajo puentes entre edificios; la Costa tiene oleaje, faros y playa; la Regata, carteles cada 250 m con su número, boyas y tribunas. La elección sobrevive a un reinicio. Captura de aceptación visual: 4 escenarios × 6, 12, 18 y 22 h (la prueba manual del legacy).

**Verificación.** [dotnet] generación determinista de cada escenario, topes e hitos en metros exactos, preferencia. [Editor] escenas, miniaturas, capturas. [Device] Android de gama media (rendimiento).

**Tamaño.** ~650 líneas: `environments/river_environment.dart` 72, `coast_environment.dart` 89, `regatta_environment.dart` 72, `painters/shore_painter.dart` (~139), `thumbnail_painter.dart` 44, `environment_picker.dart` 66, `scene_settings.dart` (~28), tests ~140. Esfuerzo nuevo alto (3 entornos y props).

**Mejoras EXR.** Entornos inspirados en lugares reales (lago tipo Bled, río tipo Henley/Charles, canal tipo Bosbaan) sin copiar marcas; mundo con vida (banderas, boyas, público estático en la regata); miniaturas renderizadas de la escena real.

**Decisiones aplicadas.** Q-61, Q-64, Q-65 y la hora de las miniaturas (QA-P-11, §6).

---

### It. 9 — Historial y detalle con gráficos (`09-historial`)

**Objetivo.** El usuario ve la lista de sus sesiones y, en el detalle, el resumen, el rendimiento por paso y el gráfico de cada métrica con bandas de pasos y líneas objetivo.

**Incluye.**
- HIS: F-HIS-05, F-HIS-06, F-HIS-07, F-HIS-09, F-HIS-10, F-HIS-11, F-HIS-12, F-HIS-13, F-HIS-14, F-HIS-15, F-HIS-19.
- RUT: F-RUT-20.

**También toca.** F-APP-02 (tocar Historial recarga), F-APP-01 (el historial se carga al arrancar).

**Tests legacy a portar.** Ninguno directo (`session_stats_test` ya está en la It. 1). **Criterios nuevos:** tabla de formatos de F-HIS-05, cálculos de rendimiento por paso, bandas, líneas objetivo y escala del gráfico (split invertido). Una sesión cortada a mitad (It. 4a) aparece con sus puntos.

**Demo.** Después de remar con el simulador una sesión libre y una rutina: la pestaña Historial muestra las tarjetas con fecha `dd/MM/yyyy  HH:mm`, distancia, duración, split, SPM, vatios y pulso `{prom}/{máx}`. En el detalle: resumen, "Rendimiento por paso" (`2:00 · 433m / 150W 24.1 spm 2:13/500m`) y el gráfico con selector Watts/SPM/Split/Pulso, franjas por tipo de paso y objetivos punteados.

**Verificación.** [dotnet] estadísticas, rendimiento por paso, bandas, líneas objetivo, formatos y consultas. [Editor] lista, detalle y gráfico.

**Tamaño.** ~780 líneas: `features/history/history_provider.dart` 35, `history_screen.dart` 159, `session_detail_screen.dart` (~520 de 581; el botón de subida es de la It. 12), `database_service.dart` (~60: consultas).

**Mejoras EXR.** Tipografía numérica de tablero de regata, gráfico con animación de entrada y tooltip fluido, sin parpadeo al recargar (Q-22).

**Decisiones aplicadas.** Q-01, Q-06, Q-09, Q-13, Q-16, Q-21, Q-22, Q-23, Q-24, Q-28, D-ART-03 (ejes de 11 px).

**Fuera de esta iteración.** Indicadores y subida a Strava (It. 12); sesiones importadas y reparación (It. 13).

---

### It. 10a — Bluetooth real en Android y Windows: capa nativa y remo FTMS (`10a-ble-android-windows`)

**Objetivo.** Conectar un remo FTMS real en Android y Windows: estado del adaptador y permisos, búsqueda con lista en vivo, conexión con toma de control y keepalive, reconexión automática, detección de monitor apagado y diagnóstico oculto. Se construye la capa nativa propia de Android y Windows, que también usa el pulsómetro en la It. 11.

**Incluye.**
- BLE: F-BLE-04, F-BLE-05, F-BLE-06, F-BLE-08, F-BLE-09, F-BLE-12.
- APP: F-APP-13.
- Los criterios [Apple] de estos IDs se verifican en la 10b (§4.1).

**También toca.** F-BLE-02/03 (paquetes y filtro con el remo real), F-BLE-07 y F-BLE-11 (desconexión y tablero con datos reales), F-BLE-10, F-APP-14 (pantalla encendida en Android), Q-91 (segundo plano en Android y Windows).

**Tests legacy a portar.** Ninguno nuevo (la conexión real no tiene tests en el legacy). Se vuelven a correr `heart_rate_card_test` :121, :142 y :161 contra el remo real con adaptador falso (QA-P-27). **Criterios nuevos:** máquina de estados del remo con un adaptador falso (10 s de búsqueda, 15 s de conexión, Request Control → 300 ms → Start, keepalive cada 2 s, reconexión a los 3 s), watchdog de ≥ 5 s, exclusión de búsquedas, vistas "Bluetooth apagado" y "sin permiso", y el keepalive que sigue con la app en segundo plano (Q-91).

**Demo.** Con el AMS-670B, en el teléfono Android y en la PC con Windows: Bluetooth apagado muestra su vista y "Activar Bluetooth"; al encenderlo, "Buscar dispositivos" lista el remo; tocarlo conecta y el tablero muestra los datos reales. Apagar el monitor desconecta a los 5-6 s con el mensaje "El monitor dejó de enviar datos". Un corte breve reconecta solo, también en pleno entrenamiento, con el aviso "Reconectando con {remo}…" sobre el HUD. El diagnóstico muestra RECEIVING, contador y bytes crudos. Con la app en segundo plano durante un entrenamiento, el monitor sigue en modo Bluetooth y la sesión sigue.

**Verificación.** [dotnet] estado del remo con adaptador falso, watchdog, filtro. [Editor] vistas. [Device] Android y Windows. [BLE-HW] AMS-670B.

**Tamaño.** ~650 líneas legacy: `core/bluetooth/ble_service.dart` 345, `features/device/device_screen.dart` (~230: búsqueda, Bluetooth apagado, diagnóstico), `device_provider.dart` (~60: watchdog y diagnóstico), manifiesto de Android (~20). Más el código nativo nuevo de Android y Windows.

**Mejoras EXR.** Búsqueda como un "sonar" sereno con tarjetas que entran suaves; conexión en pasos visibles ("Conectando…", "Tomando control del monitor…", "Listo") con vibración sutil; vista de estado ilustrada que se transforma al volver el Bluetooth.

**Decisiones aplicadas.** Q-04, Q-15 (verificar contadores del AMS-670B), Q-29, Q-31, Q-32, Q-34, Q-35, Q-36, Q-37, Q-38, Q-39, Q-47, Q-91, D-ANI-06 (§6).

---

### It. 10b — Tanda Apple: iOS y macOS (`10b-tanda-apple`)

**Objetivo.** Llevar a iOS y macOS todo lo que ya funciona en Android y Windows, escribiendo de una sola vez **todo** el código nativo de Apple (P-06): Bluetooth del remo y del pulsómetro, Bluetooth en segundo plano en iOS (R-07), pantalla siempre encendida en macOS, barra de estado de iOS, almacenamiento seguro y login del sistema para Strava. Además se verifican todos los criterios [Apple] de las iteraciones ya hechas (§4.1).

**Cuándo.** En cuanto el usuario tenga una Mac con Xcode, en cualquier momento después de la 10a (regla 8). Si llega antes de la It. 12, el almacenamiento seguro y el login se prueban con una verificación técnica mínima: guardar y leer un valor, y abrir el login del sistema contra una dirección de prueba y volver a la app por `rowmate://`. El cierre de producto de esas piezas queda en la It. 12.

**Incluye.** No es dueña de ningún ID: verifica los criterios [Apple] de §4.1 y escribe el código nativo que necesitan.

**Demo.** En un iPhone y en una Mac: el remo AMS-670B se conecta y muestra datos reales. Al primer arranque aparece el diálogo de permiso de Bluetooth, con textos es/en que mencionan el remo y el pulsómetro (Q-87, Q-89). En iOS, con la app en segundo plano, la conexión sigue y reconecta (R-07). En la Mac, la pantalla no se apaga. Si ya pasaron las It. 11-12, también el pulsómetro real y el login de Strava.

**Verificación.** [Device] [Apple] iPhone y Mac. [BLE-HW] AMS-670B (y pulsómetro si ya pasó la It. 11).

**Tamaño.** ~50 líneas legacy (`Info.plist`, permisos y *entitlements* de iOS y macOS, partes de `main.dart`). Más el código nativo nuevo de Apple (Bluetooth, almacenamiento seguro, login, pantalla encendida, barra de estado).

**Decisiones aplicadas.** Q-80 (parte nativa del login en macOS), Q-87, Q-89, Q-91, R-07, R-14 (identificador provisional de Apple distinto del legacy).

---

### It. 11 — Bluetooth real: pulsómetro (`11-ble-pulsometro`)

**Objetivo.** Usar un pulsómetro real: búsqueda, conexión manual con validación del servicio, memoria del último sensor, reconexión automática al arrancar y tras una caída, watchdog de lectura vencida, desconectar y olvidar, con prioridad de las acciones del usuario.

**Incluye.** HRM: F-HRM-02, F-HRM-03, F-HRM-05, F-HRM-06, F-HRM-07, F-HRM-08, F-HRM-09, F-HRM-10, F-HRM-11, F-HRM-16. Los criterios [Apple] se verifican en la 10b (si viene después) o en la It. 14.

**También toca.** F-APP-01 (el pulsómetro recordado se reconecta al abrir la app).

**Tests legacy a portar (7 casos).** `reconnect_loop_test` (ReconnectLoop 6 + StaleWatchdog 1).
**Criterios nuevos.** Secuencia real de conexión (HRM-03.4 a 03.13), cadencias 6 × 10 s y 3 s sin límite, invalidación de intentos en vuelo, preferencias `hrm.deviceId`/`hrm.deviceName`.

**Demo.** Con una banda o un reloj: Buscar → elegir → bpm en vivo. Cerrar y abrir la app: el sensor se reconecta solo y aparece "recordado". Alejarse hasta perder la señal: "reconectando..." y vuelve solo. Durante un entrenamiento, el pulso del sensor reemplaza al del monitor en el HUD y en la sesión guardada. Olvidar borra la memoria.

**Verificación.** [dotnet] motor de reintentos, watchdog y servicio del pulsómetro con adaptador falso. [Device] Android y Windows. [BLE-HW] pulsómetro real (y remo, para la exclusión de búsquedas).

**Tamaño.** ~720 líneas: `core/bluetooth/heart_rate_service.dart` 448, `reconnect_loop.dart` 91, tests 177.

**Mejoras EXR.** Avisos breves y discretos ("Pulsómetro conectado"); estados explicados en lenguaje humano (Q-42).

**Decisiones aplicadas.** Q-33, Q-41, Q-42, Q-43, Q-44.

---

### It. 12 — Strava: conexión, subida y TCX (`12-strava-subida`)

**Objetivo.** Con un build que trae credenciales aparece la pestaña Perfil. El usuario conecta su cuenta (regresando a la app con `rowmate://`), elige cómo subir (automático, preguntar, manual) y cada sesión sube como TCX con vueltas por paso, pulso, cadencia, potencia y metadatos ocultos de la rutina. Historial y Detalle muestran qué está sincronizado, y el Detalle permite subir a mano. En simulador no se sube nada.

**Incluye.**
- STR: F-STR-01 (+ F-APP-04), F-STR-02, F-STR-03, F-STR-04, F-STR-05, F-STR-06 (+ F-WKT-22), F-STR-07, F-STR-08, F-STR-09, F-STR-10, F-STR-17 (+ F-HIS-08), F-STR-18.
- HIS: F-HIS-16.
- APP: F-APP-17.
- Login y almacenamiento seguro en iOS/macOS: criterios [Apple], con el código nativo de la 10b.

**También toca.** F-STR-16 (tarjetas de conexión y preferencia del Perfil), F-WKT-20/21 (disparadores al terminar), F-APP-02 (quinta pestaña), F-APP-13 (permiso de internet).

**Tests legacy a portar.** Ninguno (no hay). **Criterios nuevos (STR §5):**
- Vencimiento de credenciales: 939 no vence, 940 sí, con V = 1000.
- Preferencia por defecto `auto`.
- TCX: desfases horarios, vueltas por paso, calorías, omisión de ceros y escape de caracteres.
- Metadatos: nivel 3 con 5 pasos → `{"v":3,"c":5}`; límites de tamaño.
- Duplicado "duplicate of activity 123456" → éxito con 123456.

**Demo.** Conectar con Strava desde Perfil (nombre y avatar del atleta). Con "Automático", terminar una sesión la sube y aparece en Strava con sus vueltas; con "Preguntar", el diálogo espera la respuesta; con "Manual", el Detalle ofrece "Subir a Strava". El ícono naranja `#FC4C02` aparece en las sincronizadas. Un build sin credenciales vuelve a tener 4 pestañas; un build de simulador no muestra botones de subida.

**Verificación.** [dotnet] TCX, metadatos, vencimiento, lectura del duplicado. [Editor] Perfil y diálogos. [Device] login en Android y Windows. [Strava] cuenta real (verificar que la descripción guarde el comentario ROWMATE, Q-74).

**Tamaño.** ~1 190 líneas: `core/strava/strava_auth_service.dart` 182, `strava_api_service.dart` (~200 de 303), `strava_config.dart` 22, `strava_metadata.dart` (~120 de 205), `tcx_builder.dart` 189, `features/profile/profile_provider.dart` (~120 de 431), `profile_screen.dart` (~220 de 467), `session_detail_screen.dart` (~60), disparadores del inmersivo (~50), configuración de plataformas (~20). **Nunca** se lee `strava_secrets.dart`.

**Mejoras EXR.** Conexión con la identidad de Strava respetada (color `#FC4C02`), avisos de subida claros y no intrusivos, estado sincronizado visible de un vistazo.

**Decisiones aplicadas.** Q-01, Q-11 (R-11: ids externos que no chocan con el legacy), Q-53, Q-54, Q-72, Q-74, Q-78, Q-79, Q-80, Q-83, Q-84.

---

### It. 13 — Strava: sincronización, importación y Perfil completo (`13-strava-sincronizacion`)

**Objetivo.** Completar el Perfil: sincronizar a Strava todo lo pendiente (reparando antes las sesiones mal guardadas), importar las actividades de remo de Strava al historial con su telemetría reconstruida, ver y subir las pendientes y limpiar las sesiones importadas.

**Incluye.**
- STR: F-STR-11, F-STR-12, F-STR-13 (+ F-HIS-23), F-STR-14, F-STR-15 (+ F-HIS-22), F-STR-16.
- HIS: F-HIS-21.

**También toca.** F-HIS-17 (la base distingue las sesiones importadas desde la It. 3, P-02).

**Tests legacy a portar.** Ninguno directo. **Criterios nuevos (STR §5):**
- Ritmo importado: Δt 1 / Δd 4 → 125; serie sin distancia → 120; suavizado de ventana 15 recortada en los bordes.
- Vueltas que comparten índice de borde.
- Reparación: sin puntos se saltea; fin = inicio + tiempo máximo.
- Importación: solo Rowing y VirtualRow; saltea los ids que ya están.
- Avisos con plural (`profileSyncResult`, `profileUploadResult`, FEATURE_INVENTORY §4.1.1).

**Demo.** "Sincronizar desde Strava" trae al historial las actividades de remo de la cuenta, con gráfico y bandas por vuelta. "Sincronizar a Strava" sube las pendientes, incluida una sesión cortada a mitad (gracias al guardado incremental de la It. 4a). "Subidas pendientes" lista lo que falta. "Limpiar sesiones de Strava" borra solo las importadas y deja intactas las grabadas en la app.

**Verificación.** [dotnet] reconstrucción del ritmo, vueltas y tipo de paso, reparación, filtros de importación. [Editor] Perfil completo. [Strava] cuenta real con actividades de remo.

**Tamaño.** ~790 líneas: `features/profile/profile_provider.dart` (~311), `profile_screen.dart` (~247), `core/strava/strava_api_service.dart` (~103: actividades, streams, vueltas), `strava_metadata.dart` (~85: lectura), `database_service.dart` (~40: limpieza y pendientes).

**Mejoras EXR.** Las sesiones externas se integran sin costuras, con marca de origen; las acciones destructivas dicen exactamente qué y cuántas sesiones borran.

**Decisiones aplicadas.** Q-02 (plural de "{count} sesiones", ya en el catálogo desde la It. 1), Q-71, Q-73, Q-75, Q-76, Q-77, Q-82.

---

### It. 14 — Pulido EXR, identidad, plataformas y CI (`14-pulido-plataformas`)

**Objetivo.** Cerrar la paridad de plataforma y llevar el conjunto al nivel EXR. Incluye identidad "RowMate" en las 4 plataformas, ícono y pantalla de arranque oscura, orientación y ventanas, builds reproducibles con tests automáticos, auditoría final de textos y una pasada de pulido de transiciones, háptica, tipografía y rendimiento. Es la última vuelta [Apple] para lo que se hizo después de la 10b.

**Incluye.** APP: F-APP-11, F-APP-15, F-APP-16, F-APP-19.

**Tests legacy a portar.** Ninguno. **Criterios nuevos:**
- Ningún texto visible fuera del catálogo es/en, salvo el simulador y el diagnóstico (Q-03/Q-04).
- Orientaciones y ventanas de F-APP-15.
- 60 fps en un teléfono de gama media durante el entrenamiento (R-12), incluido todo lo que haya quedado NO VERIFICABLE en las It. 6a-8.
- Pipeline que compila las 4 plataformas y corre `dotnet test` sin credenciales versionadas.
- Todos los criterios [Apple] pendientes (§4.1).

**Demo.** Builds instalables de Android, iOS, Windows y macOS con nombre, ícono y arranque "RowMate"; recorrido completo de la app (Dispositivo → rutina → entrenamiento en los 4 escenarios → historial → Strava) en es y en, con transiciones fluidas y sin textos fijos.

**Verificación.** [dotnet] en el pipeline. [Editor] auditoría visual. [Device] Android y Windows. [Apple] iOS y macOS.

**Tamaño.** ~300 líneas legacy (configuración de `android/`, `ios/`, `macos/`, `windows/`, `.github/`, `codemagic.yaml`) más la auditoría de textos de todas las pantallas.

**Mejoras EXR.** Pasada transversal: transiciones entre pantallas, háptica sutil en móvil, coherencia tipográfica (fuente propia OFL con cifras tabulares), rotación fluida de escena y HUD.

**Decisiones aplicadas.** Q-01 (auditoría), Q-02, Q-85 (identificadores definitivos; en iOS y macOS, reemplazar o convivir con el legacy, R-14), Q-86, Q-88, Q-90 (verificación del mínimo de Android), P-10 (§7).

---

## 3. Backlog post-paridad

Las mejoras que no existen en el legacy (botes fantasma, cámaras extra, sonido, clima, minimapa, personalización, zonas de FC, mejoras del editor, borrado de sesiones, etc.) están en [FEATURE_INVENTORY §6.2](product/FEATURE_INVENTORY.md) (BKL-01 a BKL-25). No entran en estas iteraciones; se priorizan con el usuario al terminar la paridad. Las que más acercan la app a EXR son BKL-01 (otros botes), BKL-02 (cámaras, con D-ART-07 y D-ANI-07 ya especificadas por el Designer), BKL-03 (sonido) y BKL-05 (minimapa).

---

## 4. Matriz de trazabilidad ID → iteración

Cada uno de los 158 IDs aparece **una sola vez**. "Tipo": C = canónico; "→ X" = alias que se cierra con X. "También toca" = iteraciones que usan una parte antes o después sin cerrarla. La 10b figura en "También toca" de los IDs cuyo código nativo de Apple se escribe ahí; el resto de los criterios [Apple] está en §4.1.

| ID | Tipo | It. | También toca |
|---|---|---|---|
| F-BLE-01 | C | 1 | — |
| F-BLE-02 | C | 1 | 10a, 10b |
| F-BLE-03 | C | 1 | S-1, 10a, 10b, 11 |
| F-BLE-04 | C | 10a | 10b |
| F-BLE-05 | C | 10a | 2, 10b |
| F-BLE-06 | C | 10a | 10b |
| F-BLE-07 | C | 2 | 10a |
| F-BLE-08 | C | 10a | 2, 10b |
| F-BLE-09 | C | 10a | 10b |
| F-BLE-10 | C | 4a | 2, 10a |
| F-BLE-11 | C | 2 | 10a |
| F-BLE-12 | C | 10a | 10b |
| F-BLE-13 | C | 2 | 3, 12 |
| F-BLE-14 | C | 1 | 2 |
| F-BLE-15 | C | 2 | — |
| F-BLE-16 | C | 2 | 6a |
| F-HRM-01 | C | 1 | S-1, 11 |
| F-HRM-02 | C | 11 | 2, 10b |
| F-HRM-03 | C | 11 | 2, 10b |
| F-HRM-04 | C | 2 | 11 |
| F-HRM-05 | C | 11 | — |
| F-HRM-06 | C | 11 | — |
| F-HRM-07 | C | 11 | — |
| F-HRM-08 | C | 11 | 2 |
| F-HRM-09 | C | 11 | — |
| F-HRM-10 | C | 11 | 2 |
| F-HRM-11 | C | 11 | — |
| F-HRM-12 | C | 2 | 11 |
| F-HRM-13 | C | 2 | 11 |
| F-HRM-14 | → F-WKT-05 | 4a | — |
| F-HRM-15 | C | 2 | — |
| F-HRM-16 | C | 11 | 10b |
| F-RUT-01 | C | 1 | 3, 4a, 4b |
| F-RUT-02 | C | 1 | — |
| F-RUT-03 | C | 1 | 3 |
| F-RUT-04 | C | 1 | 3, 4a |
| F-RUT-05 | C | 1 | 3, 4a |
| F-RUT-06 | C | 1 | 4a, 9 |
| F-RUT-07 | C | 1 | 4a |
| F-RUT-08 | C | 3 | — |
| F-RUT-09 | C | 3 | 4a |
| F-RUT-10 | C | 3 | — |
| F-RUT-11 | C | 3 | — |
| F-RUT-12 | C | 3 | — |
| F-RUT-13 | C | 3 | — |
| F-RUT-14 | C | 3 | — |
| F-RUT-15 | → F-BLE-01 | 1 | — |
| F-RUT-16 | C | 1 | 4a |
| F-RUT-17 | C | 1 | 4a |
| F-RUT-18 | C | 1 | 4a, 9 |
| F-RUT-19 | → F-APP-08 | 2 | — |
| F-RUT-20 | C | 9 | 12 |
| F-WKT-01 | C | 4a | — |
| F-WKT-02 | C | 4a | — |
| F-WKT-03 | C | 4a | — |
| F-WKT-04 | C | 4a | 4b |
| F-WKT-05 | C | 4a | 11 |
| F-WKT-06 | C | 4a | 9 |
| F-WKT-07 | C | 5 | — |
| F-WKT-08 | C | 5 | — |
| F-WKT-09 | C | 4a | 4b |
| F-WKT-10 | C | 4a | — |
| F-WKT-11 | C | 4a | 5 |
| F-WKT-12 | C | 4a | 8 |
| F-WKT-13 | C | 4b | 4a, 6a, 6b |
| F-WKT-14 | C | 4b | — |
| F-WKT-15 | C | 4b | — |
| F-WKT-16 | C | 4b | 4a |
| F-WKT-17 | C | 4b | 4a, 6b |
| F-WKT-18 | C | 5 | — |
| F-WKT-19 | C | 4b | — |
| F-WKT-20 | C | 4a | 12 |
| F-WKT-21 | C | 4b | 12 |
| F-WKT-22 | → F-STR-06 | 12 | — |
| F-WKT-23 | C | 4a | — |
| F-SCN-01 | C | 6b | 4a, 6a |
| F-SCN-02 | C | 6a | — |
| F-SCN-03 | C | 6a | 6b |
| F-SCN-04 | C | 7 | — |
| F-SCN-05 | C | 7 | — |
| F-SCN-06 | C | 7 | — |
| F-SCN-07 | C | 7 | — |
| F-SCN-08 | C | 7 | — |
| F-SCN-09 | C | 6a | — |
| F-SCN-10 | C | 6a | 7 |
| F-SCN-11 | C | 6a | — |
| F-SCN-12 | C | 6b | 8 |
| F-SCN-13 | C | 6b | 6a |
| F-SCN-14 | C | 6a | 8 |
| F-SCN-15 | C | 6a | — |
| F-SCN-16 | C | 8 | — |
| F-SCN-17 | C | 8 | — |
| F-SCN-18 | C | 8 | — |
| F-SCN-19 | C | 8 | 6a |
| F-SCN-20 | C | 8 | 6a |
| F-SCN-21 | C | 8 | 6a |
| F-SCN-22 | C | 8 | — |
| F-SCN-23 | C | 6a | 2 |
| F-HIS-01 | → F-WKT-06 | 4a | — |
| F-HIS-02 | → F-WKT-06 | 4a | — |
| F-HIS-03 | → F-WKT-06 | 4a | — |
| F-HIS-04 | → F-RUT-18 | 1 | — |
| F-HIS-05 | C | 9 | 13 |
| F-HIS-06 | C | 9 | 12, 13 |
| F-HIS-07 | C | 9 | 12 |
| F-HIS-08 | → F-STR-17 | 12 | — |
| F-HIS-09 | C | 9 | 12 |
| F-HIS-10 | C | 9 | — |
| F-HIS-11 | C | 9 | 13 |
| F-HIS-12 | C | 9 | — |
| F-HIS-13 | C | 9 | — |
| F-HIS-14 | C | 9 | 13 |
| F-HIS-15 | C | 9 | — |
| F-HIS-16 | C | 12 | — |
| F-HIS-17 | C | 3 | 4a, 13 |
| F-HIS-18 | → F-RUT-08 | 3 | — |
| F-HIS-19 | C | 9 | 4a, 12, 13 |
| F-HIS-20 | C | 3 | — |
| F-HIS-21 | C | 13 | — |
| F-HIS-22 | → F-STR-15 | 13 | — |
| F-HIS-23 | → F-STR-13 | 13 | — |
| F-STR-01 | C | 12 | — |
| F-STR-02 | C | 12 | 10b |
| F-STR-03 | C | 12 | 10b, 13 |
| F-STR-04 | C | 12 | — |
| F-STR-05 | C | 12 | — |
| F-STR-06 | C | 12 | — |
| F-STR-07 | C | 12 | 13 |
| F-STR-08 | C | 12 | 13 |
| F-STR-09 | C | 12 | — |
| F-STR-10 | C | 12 | 13 |
| F-STR-11 | C | 13 | — |
| F-STR-12 | C | 13 | — |
| F-STR-13 | C | 13 | — |
| F-STR-14 | C | 13 | — |
| F-STR-15 | C | 13 | — |
| F-STR-16 | C | 13 | 12 |
| F-STR-17 | C | 12 | — |
| F-STR-18 | C | 12 | — |
| F-APP-01 | C | 2 | 3, 9, 11 |
| F-APP-02 | C | 2 | 9, 12 |
| F-APP-03 | C | 3 | 4a, 9 |
| F-APP-04 | → F-STR-01 | 12 | — |
| F-APP-05 | C | 2 | 14 |
| F-APP-06 | C | 2 | 4a, 9 |
| F-APP-07 | → F-RUT-01 | 1 | — |
| F-APP-08 | C | 2 | — |
| F-APP-09 | C | 1 | 2 |
| F-APP-10 | C | 1 | todas |
| F-APP-11 | C | 14 | todas |
| F-APP-12 | → F-WKT-11 | 4a | — |
| F-APP-13 | C | 10a | 10b, 11, 12 |
| F-APP-14 | C | 4a | 10a, 10b |
| F-APP-15 | C | 14 | 4b, 10b |
| F-APP-16 | C | 14 | 1, 2, 10b |
| F-APP-17 | C | 12 | 10b |
| F-APP-18 | → F-BLE-13 | 2 | — |
| F-APP-19 | C | 14 | 1, 2 |

Recuento por iteración dueña: It. 1 = 20, 2 = 16, 3 = 11, 4a = 19, 4b = 7, 5 = 3, 6a = 8, 6b = 3, 7 = 5, 8 = 7, 9 = 12, 10a = 7, 10b = 0, 11 = 10, 12 = 17, 13 = 9, 14 = 4. Total 158.

### 4.1 Criterios [Apple]

Se verifican en la tanda Apple (10b) si la iteración dueña ya pasó; si no, en la It. 14 (regla 8).

| ID / decisión | Qué hay que ver en iOS y macOS | Código nativo |
|---|---|---|
| F-BLE-04, F-APP-13, Q-87, Q-89 | Estado del Bluetooth y permisos; diálogo de permiso al primer arranque en iOS; textos de permiso es/en que mencionan el remo y el pulsómetro | 10b |
| F-BLE-05, F-BLE-06, F-BLE-08, F-BLE-09, F-BLE-12 | Búsqueda, conexión con toma de control y keepalive, reconexión, watchdog y diagnóstico con el AMS-670B | 10b |
| R-07, Q-91 | En iOS la conexión y la sesión siguen con la app en segundo plano; en macOS, con la ventana minimizada | 10b |
| F-APP-14 | Pantalla siempre encendida en iOS y macOS | 10b |
| F-APP-15 | Barra de estado de iOS oculta en el inmersivo; ventana inicial 800×600 en macOS | 10b |
| F-HRM-02, F-HRM-03, F-HRM-05..11, F-HRM-16 | Pulsómetro real: búsqueda, conexión, memoria, reconexiones y watchdog | 10b |
| F-STR-02, F-STR-03, F-APP-17, Q-80 | Login con regreso a la app (`rowmate://`), credenciales guardadas en el almacenamiento seguro y renovación | 10b |
| F-APP-16, F-APP-19, Q-85, R-14 | Nombre, ícono, arranque oscuro, identificador (distinto del legacy hasta Q-85) y build firmado | 10b (id), 14 |

---

## 5. Tests legacy → iteración en que se portan

| Archivo de test | Casos | Iteración |
|---|---|---|
| `rowing_simulator_test.dart` | 7 | 1 |
| `heart_rate_parser_test.dart` | 6 | 1 |
| `scan_filter_test.dart` | 2 | 1 |
| `routine_positions_test.dart` | 1 | 1 |
| `session_stats_test.dart` | 3 | 1 |
| `simulated_ble_service_test.dart` | 3 | 2 |
| `simulated_heart_rate_service_test.dart` | 3 | 2 |
| `simulator_overlay_test.dart` | 2 | 2 |
| `heart_rate_card_test.dart` | 4 | 2 (:121, :142 y :161 se vuelven a correr en la 10a, QA-P-27) |
| `heart_rate_provider_test.dart` | 3 | 2 |
| `workout_provider_hr_test.dart` | 4 | 4a |
| `step_target_tracker_test.dart` | 6 | 4a |
| `step_pill_test.dart` | 1 | 4b |
| `series_panels_test.dart` | 4 | 4a ("formatos") y 5 (los otros 3) |
| `series_tracker_test.dart` | 12 | 5 |
| `scene/scene_camera_test.dart` | 4 | 6a |
| `scene/scene_state_test.dart` | 7 | 6a (:14, :26, :55) y 7 (:37, :46, :60, :69) |
| `scene/time_of_day_test.dart` | 6 | 6a |
| `scene/environment_test.dart` | 3 × 4 + 3 | 6a (genéricos y lago) y 8 (río, costa, regata) |
| `scene/sky_shore_painter_test.dart` | 16 + 2 | 6a (cielo y lago) y 8 (resto) |
| `scene/water_painter_test.dart` | 2 | 6a (:11, respaldo) y 8 (:27) |
| `scene/scene_view_test.dart` | 16 = 4 escenarios × 2 tamaños × 2 horas | 6a (los 4 del lago, que se vuelven a correr en la 6b) y 8 (los 12 de río, costa y regata) |
| `scene/stroke_cycle_test.dart` | 4 | 7 |
| `scene/boat_painter_test.dart` | 1 + 2 | 7 |
| `scene/environment_picker_test.dart` | 2 | 8 |
| `reconnect_loop_test.dart` | 7 | 11 |
| `widget_test.dart` | 0 (vacío) | No se porta |

---

## 6. Decisiones de diseño y producto por gate (QA-P-13)

Las propuestas del Designer ([ART_BIBLE §0.2](design/ART_BIBLE.md), [ANIMATION_BIBLE §0.2](design/ANIMATION_BIBLE.md)) y las decisiones abiertas del gate del plan se deciden en el gate de la iteración indicada. "Por defecto" es lo que se implementa si el usuario no responde. "Confirmar = Sí" se le muestra al usuario en ese gate.

| Decisión | Qué propone | Decide | Gate | Por defecto | Confirmar |
|---|---|---|---|---|---|
| Q-90 | Android mínimo 8.0 (API 26) y ARM64 | PO | 1 | Se adopta (FEATURE_INVENTORY Q-90) | Sí |
| R-14 / Q-85 | Identificadores provisionales | PO | 1 (provisorios), 14 (definitivos) | `com.iguisoft.rowmate` en Android; `com.iguisoft.rowmate.unity` en iOS y macOS (distinto del legacy). Windows no tiene identificador en el motor (D-02 de la It. 1): producto `RowMate` de `iguisoft`, datos en `LocalLow/iguisoft/RowMate`, sin `com.example`, y `com.iguisoft.rowmate` reservado para el MSIX de la It. 14 ([01-po-spec AC-APP-16-02](iterations/01-fundaciones/01-po-spec.md)) | Sí |
| D-ART-03 | Texto terciario mínimo `@54 %` (legacy `@38 %`) y ejes de gráfico de 11 px (legacy 9) | PO | 2 | Se adopta (corrección de contraste AA; también rige en el historial de la It. 9) | No |
| It. 1 · P-04 (Windows) | Windows mínimo del motor: Windows 10 21H1 (build 19043) o más nuevo. Se pierden los Windows 10 anteriores, que el legacy soporta | PO | 2 | Se adopta: lo impone Unity 6.3 (ADR 001) | Sí |
| It. 1 · P-06 | Teclado y menú de copiar y pegar en el idioma del sistema; el legacy los muestra en el de la app (AC-APP-09-07) | PO | 2 | Se acepta como diferencia conocida. Alternativa: barra de edición propia en la It. 3 | Sí |
| Q-92 | Tope de los números del paso y sus progresiones (QA-V-11 de la It. 1): el legacy suma en 64 bits y la app nueva calcula hasta 2 147 483 647 | PO | 3 | Tope por campo muy por encima de cualquier uso real; el editor no deja guardar lo que no entra (mensaje es/en); debajo del tope, idéntico al legacy (FEATURE_INVENTORY Q-92) | Sí |
| D-ART-01 | Contraste del HUD: ≥ 7:1 en blanco y métricas no rojas; familia roja ≥ 4,5:1 con sus variantes del mismo matiz (`#FF5A7E` pulso, `#FF5F59` fuera de objetivo) y flecha ▲/▼ + borde fuera de objetivo | PO + QA | 4a | Se adopta. R-10 ya está redactada así. El contrato `#FF4444` sigue siendo el color base | Sí |
| D-ART-02 | HUD +15-25 % (SPM 64, Split/Watts 44, kcal/BPM 32, banda 34) en las mismas zonas y orden | PO | 4a | Se adopta. QA verifica en 640×360 que no tape el bote | Sí |
| D-ART-09 | Objetivo en chico debajo del valor ("obj. 26") | PO | 4a | Se adopta (es la mejora EXR de F-WKT-10) | No |
| QA-P-17 | Respaldo del vidrio sin desenfoque | PO + Designer | 4a | Token `glass.fallback` del Designer (`#070D16 @86 %`, ART_BIBLE §3.2), con el borde y el radio del legacy: garantiza el contraste en el peor caso. Reemplaza al `#000000 @55 %` del legacy | No |
| Q-91 | Sesión en segundo plano | PO | 4a | Paridad: la sesión sigue (FEATURE_INVENTORY Q-91) | No |
| D-ANI-05 | Cuenta grande centrada en los últimos 3 s del paso y sello "Objetivo cumplido / No alcanzado" de 2 s | PO | 4b | Se adopta (agregado). Si el usuario lo rechaza, queda la cuenta regresiva normal | Sí |
| D-ART-05 | Sol "de pantalla" separado del sol "de luz", con el mismo acimut | Architect | 6a | Se adopta: el sol se ve donde lo ubica el legacy y las sombras son coherentes | No |
| D-ART-06 | Las nubes conservan la deriva por viento y pierden la deriva lateral por distancia | PO | 6a | Se adopta (coherente con Q-65) | No |
| D-ART-08 | Texturas CC0 opcionales (Poly Haven, ambientCG) y luna de la NASA | Usuario | 6a | **No**, hasta que el usuario diga que sí: procedurales + Terrain Sample Asset Pack | Sí |
| P-09 | Lista exacta de archivos de Mixamo | Designer | 6a | El Designer la entrega en el diseño de la 6a; el usuario descarga antes de la 7 | — |
| D-ART-04 | Bote y remos con medidas reales (manga 0,32 m, remo 2,88 m) | Architect + PO | 7 | Se adopta (es el realismo que pidió el usuario). La geometría legacy queda para sus tests | No |
| D-ANI-01 | Fase visual que sigue a la del contrato con un seguidor suave | Architect | 7 | Se adopta; la fase del contrato y sus tests no cambian | No |
| D-ANI-02 | Charcos al salir la pala visible, en la punta real de la pala | Architect + PO | 7 | Se adopta, con las reglas legacy intactas (It. 7, criterios nuevos) | No |
| D-ANI-03 | Remero y remos cabecean con el casco | Architect | 7 | Se adopta | No |
| D-ANI-04 | Arfada, rolido leve, flexión de la caña, gotas y respiración en reposo | PO | 7 | Se adopta (agregados que no cambian datos) | No |
| QA-P-11 | Hora de las miniaturas del selector | PO | 8 | Paridad: las miniaturas muestran la escena a las 12:00, como el legacy (F-SCN-22). Mostrarlas a la hora actual es un agregado que se le ofrece al usuario | Sí |
| D-ANI-06 | Sincronía fina con el contador de paladas del monitor | Architect + QA | 10a | Apagada hasta verificar con el AMS-670B cuándo incrementa | No |
| It. 1 · P-04 (macOS) | macOS mínimo del motor: 12 Monterey o más nuevo. Se pierden macOS 10.15 y 11, que el legacy soporta | PO | 10b | Se adopta: lo impone Unity 6.3 (ADR 001) | Sí |
| P-10 | Pasar a Unity 6.7 LTS si ya salió | Usuario | 14 | Se evalúa; solo se cambia con el OK del usuario (sigue siendo Unity 6 LTS) | Sí |
| D-ART-07, D-ANI-07 | Cámaras extra (lateral, dron, popa) y director automático | PO | Backlog | Fuera de la paridad (Q-66, BKL-02). Quedan especificadas para priorizarlas con el usuario al terminar | Sí (al terminar la paridad) |

---

## 7. Propuestas del Architect: cuáles se adoptan

Decisión del PO como dueño del QUÉ y del orden, con la recomendación de QA ([QA_PLAN_REVIEW §4](QA_PLAN_REVIEW.md)).

| ID | Propuesta | Decisión | Cómo queda en el plan |
|---|---|---|---|
| P-01 | Piezas base en la It. 1 y `ci-core.yml` | **Adoptada, ampliada** como pide QA | It. 1: "Piezas base"; F-APP-19 también toca la 1 |
| P-02 | Base v8 desde la It. 3 (origen de la sesión + índice) | **Adoptada** | It. 3; F-HIS-17 también toca la 4a y la 13. Sin cambio visible; habilita Q-71 |
| P-03 | Dividir la It. 4 en 4a y 4b | **Adoptada** con el corte de QA | It. 4a (motor y HUD funcional, 19 IDs) y 4b (HUD EXR, 7 IDs). F-WKT-23 va en la 4a. F-WKT-21 va entero en la 4b: hasta entonces, el fin de la rutina guarda y vuelve a reposo |
| P-04 | Desenfoque del vidrio en la It. 6 | **Adoptada** | Desenfoque en la 6b. La 4a usa el respaldo plano del Designer, token `glass.fallback` (`#070D16 @86 %`, ART_BIBLE §3.2), y no el negro `@55 %` del legacy (QA-P-17, §6) |
| P-05 | Dividir la It. 6 en 6a y 6b | **Adoptada con el ajuste de QA** (QA-P-14) | El agua degradada pasa a la 6a para que tenga una escena completa y usable; la 6b queda para el agua avanzada, el desenfoque y el rendimiento |
| P-06 | Dividir la It. 10 en 10a (Android + Windows) y 10b (iOS + macOS) con todo lo nativo de Apple en la 10b | **Adoptada, con una regla de cierre** | Los IDs de Bluetooth se cierran en la 10a con Android y Windows; la 10b es la tanda Apple flotante (regla 8, §4.1). Motivo: hoy no hay Mac (RA-02) y la paridad del remo real no puede quedar trabada por eso. Después de la 10b la Mac solo hace falta para compilar y verificar, no para escribir código nativo |
| P-07 | *Spike* de Bluetooth en Windows | **Adoptada, opcional** | S-1 entre la 2 y la 3, sin Unity (QA-P-24). No cierra IDs |
| P-08 | `ci-unity.yml` desde la It. 2 | **Adoptada, condicionada** a que el usuario active la licencia y cargue el `.ulf` | Prerrequisito opcional de la It. 2. Mientras tanto, `ci-core.yml` |
| P-09 | El Designer fija los archivos de Mixamo en la It. 6 | **Adoptada** | Prerrequisito de la 7 y decisión de la 6a (§6) |
| P-10 | Evaluar Unity 6.7 LTS en la It. 14 | **Adoptada como evaluación** | El cambio de versión necesita el OK del usuario (§6), porque Unity 6 LTS es una decisión fija suya |
| Aclaración `DartRandom` | El paisaje puede quedar idéntico al legacy | **Adoptada**: es más paridad | Q-61 revisada (Paridad); vectores dorados en la 6a |
| Aclaración Q-11 | La importación de la base legacy queda diseñada y sin implementar; en Android no es posible si cambia el identificador | **De acuerdo** | Q-11 actualizada |
| Aclaración Q-85 | Ids provisionales `com.iguisoft.rowmate` hasta confirmar Q-85 | **Adoptada solo para Android y Windows** | En iOS y macOS el legacy ya usa `com.iguisoft.rowmate`: un build nuevo con ese id reemplazaría la app legacy del usuario en su equipo. Por eso en Apple el provisional tiene que ser distinto (R-14). Lo cambia el Architect |

**Rechazadas:** ninguna. Dos se adoptan con condición (P-06: regla de cierre [Apple]; aclaración de ids: solo Android y Windows) y una solo como evaluación (P-10).

---

## 8. Cambios por QA (plan)

Correcciones por el gate de armonía del plan ([QA_PLAN_REVIEW.md](QA_PLAN_REVIEW.md)), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-P-12 | Prerrequisitos nuevos: fuentes e íconos (It. 2), licencia de Unity para CI (It. 2, opcional), Terrain Sample Asset Pack y Android de gama media (It. 6a), lista de Mixamo (6a → 7), Mac y iPhone (10b). [Device] con Android de gama media en las It. 6a, 6b, 7 y 8. Regla 9: rendimiento NO VERIFICABLE si falta el teléfono, arrastrado a la It. 14 |
| QA-P-13 | Nueva §6, tabla decisión → gate: D-ART-03 en la 2; D-ART-01/02/09 en la 4a; D-ANI-05 en la 4b (la animación es de la 4b); D-ART-05/06/08 en la 6a; D-ART-04 y D-ANI-01..04 en la 7; D-ANI-06 en la 10a; D-ART-07 y D-ANI-07 al backlog. Demo de la 4a con `#FF5F59` + flecha o `#FF4444`, según D-ART-01. R-10 actualizada en FEATURE_INVENTORY |
| QA-P-27 | `heart_rate_card_test`: los 4 casos se portan en la It. 2 con servicios simulados; :121, :142 y :161 se vuelven a correr en la 10a |
| QA-P-30 | Se creó [STATUS.md](STATUS.md) |
| QA-P-04 | It. 1: los casos 9-11 y el catálogo apuntan a las tablas exactas es/en (routines-models §3.1, FEATURE_INVENTORY §4.1.1) |
| QA-P-18 | Q-90 en el gate de la It. 1 y verificación en la 14 |
| QA-P-19 (E2) | `scene_view_test` = 4 escenarios × 2 tamaños × 2 horas: 4 casos en la 6a (y otra vez en la 6b) y 12 en la 8 |
| QA-P-06 (parte PO) | Q-91 (segundo plano) en la 4a; verificación en la 10a y la 10b |
| QA-P-11 (parte PO) | Por defecto, miniaturas a las 12:00 (paridad); la hora actual se ofrece en el gate de la 8 |
| QA-P-17 (parte PO) | Aprobado el respaldo plano del Designer para la 4a (token `glass.fallback`, hoy `#070D16 @86 %`; ver la fila "QA-P-17 (cierre)") |
| P-01 a P-10 y aclaraciones | §7: todas adoptadas (P-06 con regla de cierre [Apple], P-10 como evaluación, ids provisionales solo en Android y Windows). Nuevas iteraciones 4a/4b, 6a/6b, 10a/10b y S-1; matriz §4 y tabla de tests §5 actualizadas (158 IDs, 145 casos) |
| QA-P-17 (cierre, al especificar la It. 1) | Todas las menciones al respaldo del vidrio con el alfa anterior (objetivo y demo de la 4a, §6, §7 P-04 y la fila de arriba) pasan al token `glass.fallback` (`#070D16 @86 %`, ART_BIBLE §3.2), el mismo que usan ARCHITECTURE §7.4/§20.1 y ADR 016 |
| QA-P-10 (regla 9, al especificar la It. 1) | La regla 9 y el criterio de desenfoque de la 6b remiten a la tabla única de rendimiento de ARCHITECTURE §20 (objetivo de R-12, topes por nivel y comunes); ART_BIBLE §9 queda como reparto del arte |
| It. 1 (PO) | Prerrequisito opcional nuevo: repositorio remoto en GitHub para que corra `ci-core.yml`; sin él, el CI queda NO VERIFICABLE. Enlace a la spec de la It. 1 |
| It. 1 (cierre, PO) | Q-92 nueva (QA-V-11): tope de los números del paso y sus progresiones, en la It. 3 (criterio nuevo y "Decisiones aplicadas") y en la tabla de §6, gate 3 |
| It. 2 (gate, PO) | Demo de la It. 2: la cantidad de columnas depende del ancho de la ventana (hasta 640 dp, 2; más, 3), no de la orientación ([spec de la It. 2](iterations/02-dispositivo-simulador/01-po-spec.md) AC-BLE-11-05 y §15; QA-G2-18) |
