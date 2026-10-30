# Inventario de producto: shell de la app, navegación, tema, componentes compartidos, l10n, permisos, wakelock y plataformas/CI

| Campo | Valor |
|---|---|
| Área | **APP** |
| Autor | RowMate PO |
| Fecha | 2026-10-27 |
| Fuente | `rowmate-legacy` (solo lectura). Manda el código cuando difiere de los docs, y cada diferencia está anotada en §6. |
| Alcance | `lib/main.dart`, `lib/shared/**` (`theme.dart`, `widgets/interval_tile.dart`, `widgets/metric_card.dart`), `lib/l10n/app_es.arb`, `lib/l10n/app_en.arb`, `l10n.yaml`, `pubspec.yaml`, `android/app/src/main/AndroidManifest.xml`, `android/app/build.gradle.kts` (más `res/values*/styles.xml`, `res/drawable*`, `src/debug/AndroidManifest.xml`), `ios/Runner/Info.plist` (más `project.pbxproj`, `LaunchScreen.storyboard`, `Assets.xcassets`), `macos/Runner/*` (entitlements, Info.plist, AppInfo.xcconfig, MainMenu.xib), `windows/runner/*`, `codemagic.yaml`, `.github/workflows/build-ios.yml`, `.vscode/launch.json`, `assets/icon/icon.png`. También leí lo necesario de los consumidores (screens/providers) para entender los comportamientos del shell. **No** leí `lib/core/strava/strava_secrets.dart`, que es secreto. |
| Tests legacy | Ninguno cubre el shell de forma directa. `test/widget_test.dart` es un placeholder vacío. Los textos y la composición quedan cubiertos de forma indirecta por `environment_picker_test`, `heart_rate_card_test`, `series_panels_test` y `simulator_overlay_test` (ver §5). |

Este documento es el **contrato de paridad** del área APP: lo que no quede escrito acá se pierde en la migración. Describe el QUÉ y no toma decisiones técnicas. Las rutas `archivo:línea` son relativas a `https://github.com/figuibej/rowmate-legacy`. La marca † señala código muerto, es decir, código que existe pero ninguna pantalla alcanza.

Siglas de otras áreas que se citan: **BLE** (remo FTMS y su conexión), **HRM** (pulsómetro), **SIM** (simulador de desarrollo), **WKT** (motor de entrenamiento), **HUD** (pantalla de entrenamiento inmersiva y paneles), **SCN** (escena 2.5D), **RUT** (modelos, rutinas y editor), **HIS** (historial y detalle), **DB** (persistencia), **STR** (Strava: OAuth, subida, sync), **PRF** (pantalla Perfil).

---

## 0. Glosario

| Término | Significado en el producto |
|---|---|
| **Shell** | El marco de la app: arranque, barra de pestañas inferior y pantallas que se apilan por encima. |
| **Pestaña** | Cada destino de la barra inferior: Dispositivo, Workout, Rutinas, Historial y, si corresponde, Perfil. |
| **Pantalla apilada** | Pantalla que se abre encima del shell y tapa la barra de pestañas (editor de rutina, detalle de sesión, entrenamiento inmersivo). Se cierra con "atrás". |
| **Strava configurado** | El build trae credenciales válidas de Strava (ver F-APP-04). Si no las trae, toda la parte de Strava desaparece de la app. |
| **Modo simulador** | Variante de compilación solo para desarrollo, con un remo y un pulsómetro simulados (ver F-APP-18 y el inventario SIM). |
| **Clave `.arb`** | Identificador de un texto visible traducido a es/en. |
| **Texto hardcodeado** | Texto visible escrito fijo en el código, fuera del catálogo `.arb`. No cambia con el idioma. |

---

## 1. Mapa de funcionalidades

| ID | Nombre | Lógica pura | Tests legacy |
|---|---|---|---|
| F-APP-01 | Arranque de la app y servicios activos desde el inicio | No | ninguno |
| F-APP-02 | Barra de pestañas inferior | No | ninguno |
| F-APP-03 | Navegación secundaria: pantallas apiladas, diálogos y avisos | No | ninguno |
| F-APP-04 | Strava como interruptor de producto (según credenciales del build) | Sí (la regla) | ninguno |
| F-APP-05 | Tema visual oscuro y estilos base | No | ninguno |
| F-APP-06 | Paleta de colores por métrica | Sí (datos) | ninguno |
| F-APP-07 | Colores y nombres por tipo de paso, vistos desde el tema | Sí | indirecto: `step_pill_test` |
| F-APP-08 | Componentes compartidos: tarjeta de métrica y tarjeta de paso (sin uso) | No | ninguno |
| F-APP-09 | Idiomas soportados y elección de idioma | Sí (la regla) | indirecto: `environment_picker_test`, `heart_rate_card_test` |
| F-APP-10 | Catálogo de textos `.arb` (159 claves) | Sí (datos) | indirecto: `environment_picker_test`, `heart_rate_card_test` |
| F-APP-11 | Textos visibles fuera del catálogo (hardcodeados) | Sí (datos) | indirecto: `series_panels_test`, `simulator_overlay_test` |
| F-APP-12 | Convenciones de formato de números, horas y fechas | Sí | `series_panels_test` ("formatos") |
| F-APP-13 | Permisos de Bluetooth y privacidad por plataforma | No | ninguno |
| F-APP-14 | Pantalla siempre encendida | No | ninguno |
| F-APP-15 | Modo inmersivo del sistema, orientación y ventana | No | ninguno |
| F-APP-16 | Identidad de la app: nombre, identificadores, versión, ícono y pantalla de arranque | No | ninguno |
| F-APP-17 | Regreso a la app después del login externo (esquema `rowmate://`) | No | ninguno |
| F-APP-18 | Variante de compilación "simulador" vista desde el shell | No | `simulator_overlay_test` (2 casos) |
| F-APP-19 | Compilación y distribución (CI/CD, firma, configuraciones de ejecución) | No | ninguno |

---

## 2. Fichas

### F-APP-01: Arranque de la app y servicios activos desde el inicio

**Valor para el usuario.** Al abrir la app está todo listo: rutinas e historial cargados, el pulsómetro recordado se reconecta solo, la pantalla no se apaga y el usuario cae directo en la pestaña Dispositivo para conectar el remo.

**Comportamiento observable.**
1. Antes de mostrar nada, en Windows (y en Linux, que no es plataforma objetivo) se habilita el motor de base de datos de escritorio, así que historial y rutinas funcionan igual que en el móvil (`lib/main.dart:37-41`).
2. Se pide mantener la pantalla encendida para toda la vida de la app (F-APP-14). Ocurre una vez, antes de la primera pantalla (`lib/main.dart:42`).
3. Se crean los servicios. Si el build es normal, se usan el remo real y el pulsómetro real. Si es build de simulador, el remo simulado y un pulsómetro simulado que **comparte el mismo atleta simulado** (`lib/main.dart:51-54`). Ninguna pantalla sabe si los servicios son reales o simulados.
4. **El pulsómetro arranca activo apenas abre la app**, aunque ninguna pantalla lo esté mirando, e intenta reconectar al sensor recordado (cadencia de arranque: 6 intentos con 10 s entre ellos, unos 2 minutos en total; el detalle está en HRM). Si esa reconexión falla, el error solo queda en el log de diagnóstico como `[HRM] autoConnect: <error>` y el usuario no ve nada (`lib/main.dart:68-79`).
5. **El remo NO se reconecta solo al abrir la app.** Arranca desconectado y el usuario tiene que tocar "Buscar dispositivos" (BLE). En modo simulador, en cambio, el remo simulado aparece conectado de inmediato (SIM).
6. Como las cinco pestañas se construyen al arrancar (F-APP-02), quedan activos desde el inicio el estado del remo, del pulsómetro, del entrenamiento, de rutinas (que **carga la lista de rutinas al arrancar**), del historial (que **carga la lista de sesiones y sus estadísticas al arrancar**), del escenario elegido (que lee `scene.environment`, ver SCN) y, solo si Strava está configurado, del perfil Strava (F-APP-04, PRF). La pestaña Workout precarga además el agua de la escena para que el entrenamiento arranque con el agua completa y no con la versión de respaldo (`lib/features/workout/workout_screen.dart:42-44`, SCN).
7. La app se identifica como **"RowMate"** ante el sistema (por ejemplo en la lista de apps recientes de Android) y nunca muestra la cinta de "debug" (`lib/main.dart:94-95`).
8. Pantalla inicial: el shell con la pestaña **Dispositivo** (índice 0) seleccionada. La última pestaña usada no se recuerda entre aperturas (`lib/main.dart:124`).
9. Al cerrar la app se liberan, en este orden de declaración, el remo, la base de datos y el pulsómetro (`lib/main.dart:62,66,78`). No hay ningún aviso al usuario.
10. En **iOS**, el primer acceso a Bluetooth ocurre al arrancar, porque el servicio del remo empieza a escuchar el estado del adaptador ni bien se crea. Por eso el **diálogo de permiso de Bluetooth del sistema aparece al abrir la app la primera vez**, no al tocar "Buscar" (`lib/core/bluetooth/ble_service.dart:46-53`; el comentario de `:70` reconoce que la primera llamada dispara el diálogo). Hay que verificarlo en un dispositivo (§8).

**Casos borde y errores.**
- Si la reconexión inicial del pulsómetro falla o no hay sensor recordado, no se muestra nada. La tarjeta del pulsómetro queda en el estado que corresponda (HRM).
- Si el pedido de pantalla encendida falla, no se informa nada (F-APP-14).
- El modo simulador no persiste ningún sensor, así que en ese modo la reconexión inicial no hace nada (SIM).

**UI.** No hay splash propio dentro de la app. El sistema muestra la pantalla de arranque nativa (F-APP-16) y después aparece directo el shell. Mientras rutinas e historial cargan, sus pestañas muestran un indicador circular de carga centrado (RUT, HIS).

**Datos persistidos.** Ninguno propio. Lee al arrancar las preferencias de otras áreas: `hrm.deviceId`, `hrm.deviceName` (HRM), `scene.environment` (SCN), `strava_upload_pref` (PRF), los tokens de Strava (STR) y la base `rower_app.db` o, en simulador, `rower_app_dev.db` (DB). El mapa completo está en §4.

**Fuente legacy.** `lib/main.dart:35-44` (secuencia de arranque), `:50-113` (servicios y estados), `:86-91` (perfil condicionado), `:93-111` (configuración general de la app); `lib/features/routines/routines_provider.dart:12-14`; `lib/features/history/history_provider.dart:12-14`; `lib/features/workout/scene/scene_settings.dart:7-9`.

**Tests legacy.** Ninguno. `test/widget_test.dart:4-6` es un "smoke test" vacío que no verifica nada.

**Dependencias.** F-APP-02, F-APP-04, F-APP-14, F-APP-18; BLE, HRM, DB, RUT, HIS, SCN, PRF, STR, SIM.

**Mejora EXR.** El arranque se siente como entrar a un club de remo premium. La marca aparece sobre el azul noche del agua, con una transición suave (sin destellos blancos ni saltos) a la pantalla principal en uno o dos segundos. Si el pulsómetro recordado se reconecta, un aviso breve y elegante lo confirma ("Pulsómetro conectado"). Si no se encuentra, el usuario no recibe ruido. Todo lo que tarde en cargar muestra un estado de carga con estilo, nunca un vacío o un parpadeo.

---

### F-APP-02: Barra de pestañas inferior

**Valor para el usuario.** Moverse entre las cinco áreas de la app con un toque, sin perder lo que estaba haciendo en cada una.

**Comportamiento observable.**
1. Hay una barra fija abajo, con estos destinos y en este orden (`lib/main.dart:150-177`):

   | Índice | Destino | Clave `.arb` (es / en) | Ícono sin seleccionar | Ícono seleccionado |
   |---|---|---|---|---|
   | 0 | Dispositivo | `navDevice` (Dispositivo / Device) | Bluetooth | Bluetooth conectado |
   | 1 | Workout | `navWorkout` (Workout / Workout) | Cronómetro de contorno | Cronómetro relleno |
   | 2 | Rutinas | `navRoutines` (Rutinas / Routines) | Pesa | Pesa (el mismo) |
   | 3 | Historial | `navHistory` (Historial / History) | Reloj con flecha de historial | El mismo |
   | 4 | Perfil (solo si Strava está configurado, F-APP-04) | `navProfile` (Perfil / Profile) | Persona de contorno | Persona rellena |

2. Si Strava **no** está configurado hay exactamente 4 pestañas y la de Perfil no existe (`lib/main.dart:133,171-176`).
3. La selección inicial es la pestaña 0. Tocar una pestaña la selecciona al instante. No hay animación de página entre pestañas: el contenido cambia de golpe (`lib/main.dart:143-149`).
4. **Cada pestaña conserva su estado** (scroll, vistas abiertas, datos) al ir y volver, porque todas viven en paralelo desde el arranque y solo se muestra la elegida (`lib/main.dart:143`).
5. **Tocar la pestaña Historial (índice 3) siempre recarga el historial desde la base**, incluso si ya estaba seleccionada. Mientras recarga, el historial muestra el indicador de carga en lugar de la lista, así que se ve un parpadeo de carga en cada toque (`lib/main.dart:146-148`, `lib/features/history/history_screen.dart:21-22`). Las demás pestañas no recargan nada al tocarlas.
6. El ícono "Bluetooth conectado" de Dispositivo indica **solo que la pestaña está seleccionada, no que el remo esté conectado**. La barra no refleja el estado real de conexión.
7. Etiquetas siempre visibles debajo de los íconos. La pestaña seleccionada lleva una "píldora" de fondo detrás del ícono y la etiqueta en negrita (estilo estándar de la barra; ver `docs/image-01.png`, `image-02.png`, `image-06.png`).
8. La barra no aparece en las pantallas apiladas (F-APP-03): el editor de rutina, el detalle de sesión y el entrenamiento inmersivo la tapan.
9. **"Atrás" del sistema (Android) en el shell cierra la app** directamente, sin confirmar y sin volver a la pestaña anterior. No hay manejo especial de "atrás" a nivel shell.
10. Mientras hay un entrenamiento en curso, la pestaña Workout (que queda tapada por el entrenamiento inmersivo) muestra centrado el texto `workoutInProgress` ("Entrenamiento en curso..." / "Workout in progress...") en blanco al 38 % (`lib/features/workout/workout_screen.dart:180-202`). En la práctica no se ve, porque el inmersivo está encima.

**Casos borde y errores.**
- El índice de Historial está fijo en 3. Como Perfil es siempre el último, agregarlo o quitarlo no corre a Historial.
- Volver de un entrenamiento al shell deja seleccionada la pestaña desde la que se inició (Workout). El historial **no** se recarga solo al terminar un entrenamiento: se ve actualizado cuando el usuario toca Historial. Como el historial se cargó al arrancar, mientras no lo toque puede mostrar datos viejos.

**UI.** Colores reales de la barra: fondo gris azulado muy oscuro y píldora de selección gris azulada, con ícono y etiqueta claros. Salen del esquema de color derivado del azul semilla (F-APP-05), **no** de los colores de barra que define el tema (ver §6, D-03). La referencia visual son las capturas `docs/image-01.png`, `image-02.png` y `image-06.png` (barra en inglés, con 5 pestañas).

**Datos persistidos.** Ninguno: la pestaña elegida no se guarda.

**Fuente legacy.** `lib/main.dart:116-181`; `lib/features/workout/workout_screen.dart:19-35,180-202`; `lib/features/history/history_screen.dart:14-22`.

**Tests legacy.** Ninguno.

**Dependencias.** F-APP-04, F-APP-10; HIS (recarga), WKT, BLE, RUT, PRF.

**Mejora EXR.** La barra es una pieza de "vidrio" sobre el agua, con desenfoque del fondo. La selección se desliza suavemente de una pestaña a otra y cada cambio da un toque háptico sutil en el móvil. Los íconos tienen estilo propio, nítidos y deportivos. El cambio de pestaña se acompaña con un fundido breve, sin saltos. *(Propuesta de mejora, a aprobar porque cambia comportamiento:* que el ícono de Dispositivo refleje el estado real del remo, por ejemplo con un pulso suave cuando está conectado y transmitiendo, y que el historial se refresque solo al terminar una sesión, sin parpadeo de carga.)*

---

### F-APP-03: Navegación secundaria: pantallas apiladas, diálogos y avisos

**Valor para el usuario.** Entrar a una tarea concreta (editar una rutina, ver una sesión, remar) en pantalla completa y volver con "atrás" al mismo lugar del shell.

**Comportamiento observable.**
1. Pantallas que se apilan sobre el shell y tapan la barra de pestañas:

   | Pantalla | Se abre desde | Cómo se vuelve | Al volver | Fuente |
   |---|---|---|---|---|
   | Editor de rutina (nueva) | Botón flotante "+" de Rutinas | Atrás, o "Guardar" válido | Nada especial | `lib/features/routines/routines_screen.dart:17-23` |
   | Editor de rutina (existente) | Tocar una rutina, o el menú de la rutina → `routineEdit` | Igual | Nada especial | `routines_screen.dart:76-81,91-96` |
   | Detalle de sesión | Tocar una sesión del historial | Atrás | **Recarga el historial** (por si se subió a Strava) | `lib/features/history/history_screen.dart:44-56` |
   | Entrenamiento inmersivo | Iniciar entrenamiento libre o rutina en Workout | **"Atrás" no sale**: pide confirmar el fin (WKT/HUD). Sale solo cuando el entrenamiento vuelve a reposo | — | `lib/features/workout/workout_screen.dart:168-175`, `immersive_workout_screen.dart:57-70` |

2. La transición al apilar o desapilar es la estándar del sistema de cada plataforma (en iOS, deslizamiento lateral con gesto de borde para volver). No hay transiciones propias.
3. Los **diálogos** de confirmación son modales y centrados, con título, texto y dos acciones: una secundaria de texto (cancelar) y una primaria rellena (confirmar). Una acción destructiva usa la primaria en rojo, por ejemplo "Eliminar" al borrar rutina (`routines_screen.dart:98-115`). Tocar fuera del diálogo lo cierra y equivale a cancelar. La excepción es el diálogo de "rutina completada", que no se cierra tocando afuera (`immersive_workout_screen.dart:209`, HUD).
4. Los **avisos breves** aparecen en la parte inferior durante unos segundos (duración estándar del sistema), sin acción, por ejemplo `editorNameEmpty`, `profileUploaded` o `profileUploadFailed` (RUT, STR, PRF, HUD).
5. Dentro del shell no hay rutas con nombre ni enlaces profundos. La única entrada externa es el regreso del login de Strava (F-APP-17).

**Casos borde y errores.**
- Si el entrenamiento termina, el inmersivo se cierra solo cuando el estado vuelve a reposo (WKT/HUD).
- El panel del simulador flota por encima de todas las pantallas apiladas y diálogos (F-APP-18).

**UI.** Diálogos y avisos usan el tema oscuro (F-APP-05).

**Datos persistidos.** Ninguno.

**Fuente legacy.** Las citadas en la tabla; `lib/features/routines/routine_editor_screen.dart:43-51,72`.

**Tests legacy.** Ninguno.

**Dependencias.** F-APP-02; RUT, HIS, WKT, HUD, STR, PRF.

**Mejora EXR.** Transiciones con continuidad: la tarjeta tocada "se expande" hasta convertirse en el detalle, y el paso a la escena inmersiva se siente como bajar al agua (fundido a la escena, sonido ambiente que entra suave). Diálogos de vidrio esmerilado con un leve rebote al aparecer. Avisos discretos con ícono, que nunca tapan las métricas.

---

### F-APP-04: Strava como interruptor de producto (según credenciales del build)

**Valor para el usuario.** Strava es opcional: la app funciona completa sin conexión ni cuenta. Si el build no trae credenciales, el usuario no ve ninguna opción de Strava rota o a medias.

**Comportamiento observable.**
1. Regla: Strava está **configurado** si el secreto de cliente del build **no está vacío** y **no es** el texto de ejemplo `TU_CLIENT_SECRET` (`lib/core/strava/strava_config.dart:19-21`).
2. Si está configurado, existe la pestaña Perfil (F-APP-02) y su estado arranca con la app (F-APP-01). También aparecen los elementos de Strava en las demás pantallas: ícono de sincronización en el historial, botón "Subir a Strava" en el detalle y preguntas o subidas al terminar un entrenamiento (HIS, HUD, STR).
3. Si **no** está configurado, no hay pestaña Perfil y todos esos elementos se ocultan (`lib/features/history/history_screen.dart:96`, `session_detail_screen.dart:74-76`, `immersive_workout_screen.dart:143,199-202,264`).
4. La regla se evalúa al compilar. Un mismo build no cambia de estado.

**Casos borde y errores.**
- El build de CI de GitHub genera el archivo de credenciales con secretos del repositorio. Si faltan, el archivo queda con valores vacíos y la app sale **sin Strava** (sin pestaña Perfil), sin error de compilación (`.github/workflows/build-ios.yml:45-49`).
- Si el archivo de credenciales no existe, el build no compila (está excluido del control de versiones). Codemagic no lo genera (F-APP-19, §8).

**UI.** Cuatro o cinco pestañas, según el caso.

**Datos persistidos.** Ninguno propio.

**Fuente legacy.** `lib/core/strava/strava_config.dart:1-22`; `lib/main.dart:56,86-91,127-134,171-176`; `.gitignore` (línea "lib/core/strava/strava_secrets.dart").

**Tests legacy.** Ninguno.

**Dependencias.** F-APP-01, F-APP-02, F-APP-19; STR, PRF, HIS, HUD.

**Mejora EXR.** Sin Strava, la app se ve completa y no "recortada". Con Strava, la integración se siente nativa: el logo y los estados de sincronización están bien integrados al estilo visual, nunca como un agregado.

---

### F-APP-05: Tema visual oscuro y estilos base

**Valor para el usuario.** Una estética oscura de "agua de noche" pensada para entrenar con poca luz y frente a una pantalla, coherente en toda la app.

**Comportamiento observable.**
1. **Siempre oscuro**, sin importar el modo claro u oscuro del sistema. No hay tema claro ni selector (`lib/shared/theme.dart:7-29`).
2. El esquema de color se deriva del **azul semilla `#0077B6`** ("azul agua") en variante oscura (`theme.dart:5,9-12`). De ahí salen el color primario (celeste claro), los botones rellenos (fondo celeste claro con texto oscuro, como "Free workout" en `docs/image-02.png`), los campos de texto, los interruptores y la barra de pestañas.
3. Fondo de todas las pantallas: **`#0D1B2A`** (`theme.dart:13`).
4. Tarjetas: color **`#1A2E45`**, esquinas redondeadas de **16**, sin sombra (`theme.dart:14-18`).
5. Barra superior de título: fondo **`#0D1B2A`** (se funde con la pantalla), texto e íconos **blancos**, sin sombra (`theme.dart:19-23`).
6. El tema define colores para una barra inferior clásica (fondo `#12243A`, seleccionado `#00B4D8`, no seleccionado `#607D8B`), pero **no se aplican** porque la app usa otro tipo de barra (§6, D-03). Paridad = lo que se ve hoy: los colores derivados de la semilla.
7. Tipografía: la del sistema en cada plataforma (no hay fuentes propias en el proyecto). Respeta el tamaño de letra del sistema, sin límite.
8. Toda la app está diseñada en estilo "Material 3".

**Casos borde y errores.** Ninguno.

**UI.** Referencia visual en `docs/image-01.png` a `image-06.png`.

**Datos persistidos.** Ninguno.

**Fuente legacy.** `lib/shared/theme.dart:1-29`; `lib/main.dart:96`; `pubspec.yaml:59-60` (sin fuentes propias).

**Tests legacy.** Ninguno.

**Dependencias.** Lo usan todas las áreas.

**Mejora EXR.** Se conserva el ADN (azul noche `#0D1B2A`, tarjetas azul profundo, acentos cian), elevado a un lenguaje premium: tarjetas de vidrio con profundidad, luz y reflejos sutiles del agua, una tipografía deportivo-técnica con **cifras de ancho fijo** para que los números no "bailen" al cambiar, jerarquía clara entre dato principal y secundario y microanimaciones al actualizar valores. La interfaz se siente parte del mundo 3D y no una capa plana encima.

---

### F-APP-06: Paleta de colores por métrica

**Valor para el usuario.** Reconocer cada métrica por su color en cualquier pantalla (Dispositivo, entrenamiento, historial, gráficos).

**Comportamiento observable.** Paleta fija para usar en toda la app (`lib/shared/theme.dart:31-40`):

| Métrica | Color | Descripción |
|---|---|---|
| Split /500 m | `#FF9F1C` | naranja ámbar |
| SPM (paladas por minuto) | `#00B4D8` | cian |
| Vatios | `#FFD60A` | amarillo eléctrico |
| Distancia | `#4CC9F0` | celeste |
| Pulso | `#EF476F` | rosa rojizo (igual que "trabajo") |
| Tiempo | `#E0E0E0` | gris claro |
| Calorías | `#06D6A0` | verde menta (igual que "descanso") |

- Dónde se usa: tarjetas en vivo de Dispositivo (`lib/features/device/device_screen.dart:253-258`), tarjeta del pulsómetro (`heart_rate_card.dart:16`), tarjetas del inmersivo y paneles de series (`immersive_workout_screen.dart:593-726`, `series_panels.dart:167-387`), historial y detalle con sus gráficos (`history_screen.dart:112-120`, `session_detail_screen.dart:163-171,335-339`).
- Cuando un objetivo de paso no se cumple, la tarjeta cambia a su color de alerta (HUD). Ese color no es parte de esta paleta.

**Casos borde.** Ninguno.

**UI.** Ver capturas `docs/image-01.png` (Dispositivo) y `docs/image-06.png` (Historial).

**Datos persistidos.** Ninguno.

**Fuente legacy.** `lib/shared/theme.dart:31-40`.

**Tests legacy.** Ninguno.

**Dependencias.** BLE, HRM, HUD, HIS.

**Mejora EXR.** Se mantiene la paleta como identidad, con versiones "luminosas" para el HUD sobre la escena 3D (con brillo suave, legibles de día y de noche en la escena) y contraste verificado para daltonismo, con apoyo de ícono y etiqueta para no depender solo del color.

---

### F-APP-07: Colores y nombres por tipo de paso, vistos desde el tema

**Valor para el usuario.** Distinguir trabajo, descanso, calentamiento y enfriamiento por color y nombre localizado en toda la app.

**Comportamiento observable.** La ficha completa está en **F-RUT-01** (inventario RUT). Acá confirmo que el tema es la fuente única de:
- Nombre localizado: warmup → `stepTypeWarmup`, work → `stepTypeWork`, rest → `stepTypeRest`, cooldown → `stepTypeCooldown` (`lib/shared/theme.dart:42-53`).
- Color: work `#EF476F`, rest `#06D6A0`, warmup `#FFD166`, cooldown `#118AB2` y cualquier otro valor `#607D8B` (`theme.dart:55-69`).
- Lo consumen el editor de rutina (`routine_editor_screen.dart:318,343,578`), la barra de pasos y el banner del inmersivo (`step_pill.dart:36`, `immersive_workout_screen.dart:386-464`) y los gráficos del detalle (`session_detail_screen.dart:371,415`).

**Casos borde.** Un tipo desconocido se ve gris `#607D8B`.

**UI, datos persistidos.** Ver F-RUT-01.

**Fuente legacy.** `lib/shared/theme.dart:42-69`.

**Tests legacy.** `test/step_pill_test.dart` usa el color de "trabajo" de forma indirecta: verifica niveles de opacidad de los estados de la barra de pasos (hecho > 0,8, pendiente < 0,3, activo entre ambos), no el valor del color (HUD).

**Dependencias.** F-APP-10; RUT, HUD, HIS.

**Mejora EXR.** Ver F-RUT-01. El color de cada tipo tiñe sutilmente la atmósfera del HUD (más cálido en trabajo, más calmo en descanso).

---

### F-APP-08: Componentes compartidos: tarjeta de métrica y tarjeta de paso (sin uso)

**Valor para el usuario.** Ninguno hoy: **ninguno de los dos componentes de `lib/shared/widgets/` se usa en ninguna pantalla** (ningún archivo los importa). Se documentan como referencia de diseño, **sin requisito de paridad**.

**Comportamiento (de referencia, sin pantalla que lo muestre).**
- **Tarjeta de métrica** (`lib/shared/widgets/metric_card.dart:4-67`): tarjeta del tema con margen interno de 16 horizontal y 14 vertical. Arriba, una fila con ícono opcional de 14 en el color de la métrica, 6 de separación y la etiqueta en 11 al 60 % de opacidad, con espaciado de letras 0,5. Debajo (separación 6), el valor en 32, negrita 700, en el color de la métrica, y la unidad opcional en 13 al 50 % (separación 4), alineada por la línea base. El color por defecto es el primario del tema. Cada pantalla tiene su propia versión: la de Dispositivo (`device_screen.dart:288-330`) y la tarjeta de vidrio del inmersivo (`immersive_workout_screen.dart:741-838`). Esas sí tienen paridad, en BLE y HUD.
- **Tarjeta de paso** (`lib/shared/widgets/interval_tile.dart:6-75`): ver **F-RUT-19**. El editor de rutina tiene su propia copia visible (RUT).

**Casos borde, UI, datos persistidos.** Ninguno.

**Fuente legacy.** Las citadas arriba. `grep` de `metric_card`, `interval_tile` y `shared/widgets` en `lib/` y `test/` da 0 resultados.

**Tests legacy.** Ninguno.

**Dependencias.** F-APP-05, F-APP-06, F-APP-07; F-RUT-03 y F-RUT-04 (etiquetas de duración y objetivo).

**Mejora EXR.** Que la app Unity tenga **un único** componente de tarjeta de métrica y uno de paso, reutilizados en todas las pantallas, para lograr consistencia visual total (el legacy tiene tres versiones distintas de tarjeta de métrica).

---

### F-APP-09: Idiomas soportados y elección de idioma

**Valor para el usuario.** La app habla el idioma del teléfono, español o inglés, sin configurar nada.

**Comportamiento observable.**
1. Idiomas soportados: **inglés (`en`) y español (`es`)**, en ese orden (`lib/main.dart:103-106`).
2. La elección es **automática según el idioma del sistema**. No hay selector dentro de la app.
3. Cualquier variante regional de español (es-AR, es-ES, es-MX, etc.) usa el español. Cualquier variante de inglés usa el inglés (`lib/l10n/app_localizations.dart`, `isSupported` por código de idioma).
4. **Si el idioma del sistema no es ni español ni inglés** (por ejemplo portugués), la app se muestra **en inglés**, que es el primero de la lista y el idioma plantilla (`l10n.yaml:2`).
5. Los textos del sistema que muestra la app (por ejemplo los menús de copiar y pegar en campos de texto, o los textos de accesibilidad de "atrás") también siguen el idioma elegido (`lib/main.dart:97-102`).
6. Si el usuario cambia el idioma del sistema con la app abierta, la app se actualiza sin reiniciar (en Android la actividad no se recrea, `AndroidManifest.xml:30` incluye `locale`).
7. Tono del español: **rioplatense con voseo** en la mayoría de los textos ("Activá", "Presioná", "Conectá", "Creá", "Agregá", "Habilitá", "Asegurate", "elegí"). Hay excepciones con tuteo o neutro: `workoutCompletedMessage` ("Has completado tu rutina exitosamente."), `profileDisconnectContent` ("Tus sesiones…") y el aviso hardcodeado "Puedes sincronizar de nuevo." (F-APP-11).

**Casos borde.**
- Los textos nativos del sistema operativo (la explicación del permiso de Bluetooth en iOS y macOS) están **solo en español** para todos los usuarios (F-APP-13).
- En iOS el proyecto declara solo "en" como región conocida (`ios/Runner.xcodeproj/project.pbxproj:238-241`). Hay que verificar si eso afecta algún texto nativo del sistema en español (§8).

**UI.** No hay pantalla de idioma.

**Datos persistidos.** Ninguno: el idioma no se guarda.

**Fuente legacy.** `lib/main.dart:97-106`; `l10n.yaml:1-3`; `pubspec.yaml:15-16,31,61`; `lib/l10n/app_localizations.dart` (generado: `supportedLocales`, `isSupported`, `lookupAppLocalizations`).

**Tests legacy.** Indirectos. `test/scene/environment_picker_test.dart:11-36` fuerza el español y verifica que se vea "Lago alpino" y que tocar "Río urbano" cambie el escenario y guarde `river`. `test/heart_rate_card_test.dart:65,87-95,126` fuerza el español y verifica "PULSÓMETRO", "Sin sensor", un texto que contiene "Heart for Bluetooth", "Buscando sensores..." y "Buscar dispositivos". Ningún test cubre el inglés ni la elección automática.

**Dependencias.** F-APP-10, F-APP-11.

**Mejora EXR.** Localización completa e impecable: **cero** textos sueltos en otro idioma (ver F-APP-11) y una voz de marca consistente (voseo rioplatense en todo el español). *(Propuesta a aprobar:* agregar un selector de idioma dentro de la app.)*

---

### F-APP-10: Catálogo de textos `.arb` (159 claves)

**Valor para el usuario.** Todos los textos de la app, traducidos.

**Comportamiento observable.**
1. Hay **159 claves**, idénticas en `app_es.arb` y `app_en.arb`, sin faltantes en ninguno de los dos (verificado por comparación), y todas generadas (`lib/l10n/app_localizations*.dart`).
2. Placeholders: `hrmConnectingTo{name}`, `hrmConnectError{error}`, `workoutStartContent{summary}`, `routineDeleteContent{name}` (texto), y `editorStepsCount{count}`, `profileSyncResult{count}`, `profileUploadResult{count}` (entero). **No hay plurales**: "1 sesiones sincronizadas" o "1 sessions synced" se muestra así.
3. Saltos de línea dentro de los textos (`⏎` en el Anexo A): `deviceNoResultsHint`, `deviceBtOffHint`, `deviceBtDeniedHint`, `workoutStartContent` (doble salto) y `editorEmptySteps`.
4. Espacios iniciales significativos: `workoutOrPickRoutine` empieza con **dos espacios** ("  O elegí una rutina:").
5. Hay textos que se muestran en mayúsculas por código, como `hrmTitle` → "PULSÓMETRO" / "HEART RATE SENSOR" (`lib/features/device/heart_rate_card.dart:42`). Los rótulos `metric*` ya vienen en mayúsculas en el catálogo.
6. Claves **sin uso** (ninguna pantalla las muestra): `workoutStartTitle`, `historyStatWatts`, `historyStatCalories`.
7. Claves que **solo usa código muerto**: `workoutFinishTitle`, `workoutFinishContent`, `workoutPause`, `workoutResume`, `metricTime`, `metricCalories`, `metricFreeLabel`. El entrenamiento inmersivo que sí se ve usa textos hardcodeados equivalentes en español (F-APP-11).
8. El listado completo, con texto es/en, placeholders y dónde se usa cada clave, está en el **Anexo A**.

**Casos borde.**
- `navWorkout` dice "Workout" **también en español** (no "Entrenamiento"), mientras que `workoutTitle` en español es "Entrenamiento".
- Hay términos que no se traducen en ningún idioma: "SPM", "SPLIT 500m", "Split 500m", "kcal", "bpm", "OK".
- El signo de apertura falta en `workoutStartTitle` ("Iniciar entrenamiento?") y en `workoutStartContent` ("Iniciar el entrenamiento?"). La segunda sí se ve, en el diálogo de inicio de rutina.

**UI.** Lo consumen todas las áreas.

**Datos persistidos.** Ninguno.

**Fuente legacy.** `lib/l10n/app_es.arb:1-208`, `lib/l10n/app_en.arb:1-208`, `l10n.yaml`.

**Tests legacy.** Igual que F-APP-09 (solo textos en español de Dispositivo, Pulsómetro y Escenario).

**Dependencias.** Todas las áreas.

**Mejora EXR.** Textos breves y con tono de entrenador, con plurales correctos ("1 sesión sincronizada") y signos de puntuación completos. *(Cambio menor de producto a aprobar.)*

---

### F-APP-11: Textos visibles fuera del catálogo (hardcodeados)

**Valor para el usuario.** Hoy ninguno: son textos que **no se traducen**. Se documentan porque la paridad exige mostrarlos igual. Además es candidato a mejora, porque el usuario en inglés ve frases en español y algún texto en inglés aparece siempre.

**Comportamiento observable.** Estos textos se ven **siempre iguales**, con el idioma que sea. Solo listo lo que alguna pantalla alcanza; lo que está en código muerto va aparte.

| Texto exacto (siempre) | Dónde se ve | Área | Fuente |
|---|---|---|---|
| "Terminar entrenamiento" / "Se guardará la sesión. ¿Continuar?" / "Cancelar" / "Terminar" | Diálogo de fin de entrenamiento (inmersivo) | HUD | `immersive_workout_screen.dart:129-137` |
| "Terminar", "Reanudar", "Pausa" | Botones de control del inmersivo | HUD | `immersive_workout_screen.dart:917,927,940` |
| "Up next: " | Próximo paso en la barra del inmersivo (en inglés siempre) | HUD | `immersive_workout_screen.dart:435` |
| "Entrenamiento Libre" | Barra superior del entrenamiento libre (con L mayúscula) | HUD | `immersive_workout_screen.dart:510` |
| "EN MARCHA" / "PAUSADO" / "FINALIZADO" | Chip de estado del entrenamiento | HUD | `immersive_workout_screen.dart:531-533` |
| "SPM", "Split /500m", "Watts", "kcal", "BPM", unidades "W", "m" | Tarjetas del inmersivo | HUD | `immersive_workout_screen.dart:624-725` |
| "Tiempo", "Distancia", "Calorías" | Resumen del diálogo de rutina completada | HUD | `immersive_workout_screen.dart:981-991` |
| "Parciales 500 m", "500 m", "Series anteriores", "Reps", "Sin parciales en este paso", "Sin repeticiones completas", "Rep {n}/{N} · {metros}", "Rep {n}", "#{n}" | Paneles de series | HUD | `series_panels.dart:169-338` |
| "{n} series · {m} descansos" / "{n} pasos" | Resumen de rutina: lista de Workout, lista de Rutinas y diálogo de inicio (ver `docs/image-02.png`, "1 series · 0 descansos" con la app en inglés) | RUT, WKT | `lib/core/models/routine.dart:100-105` |
| "Watts", "SPM", "Split" | Rótulos de estadísticas en historial y detalle (mezclados con claves `.arb`) | HIS | `history_screen.dart:113-115`, `session_detail_screen.dart:164-166` |
| "{W}W", "{spm} spm", "{split}/500m", "{dur}  ·  {dist}m" | Fila de rendimiento por paso del detalle | HIS | `session_detail_screen.dart:254-261` |
| "Rower" | Nombre del remo conectado cuando no informa nombre | BLE | `device_screen.dart:98` |
| "Unknown device" | Remo encontrado sin nombre, en la lista de búsqueda (en inglés siempre) | BLE | `device_screen.dart:452` |
| "Error al escanear: {e}" / "Error al conectar: {e}" | Caja roja de error de la vista de búsqueda del remo | BLE | `device_provider.dart:111,124` |
| "Show BLE debug", "DEBUG BLE", "RECEIVING", "NO DATA", "Notifications", "Last received", "Raw bytes (hex):", "Parsed SPM/Watts/Distance/Split", "(sin datos)" | Panel de diagnóstico BLE de Dispositivo | BLE | `device_screen.dart:128-213`, `device_provider.dart:99` |
| "Strava", "Mantenimiento", "Limpiar sesiones de Strava" y su diálogo ("Esto eliminará todas las sesiones descargadas de Strava. Podrás volver a sincronizarlas después.⏎⏎¿Continuar?", "Cancelar", "Eliminar") y su aviso ("Sesiones de Strava eliminadas. Puedes sincronizar de nuevo.") | Pantalla Perfil | PRF | `profile_screen.dart:54,422-460` |
| "Upload" (texto de ayuda de un botón) | Perfil, subidas pendientes | PRF | `profile_screen.dart:400` |
| Errores de Strava: "Failed to connect to Strava", "Session not found", "Upload failed – check console for details", "Strava error: …", "No authorization code received. Callback: …", "OAuth error: …", "Token exchange failed: …", "Token exchange error: …" | Texto de error en Perfil (`lastError`) | STR, PRF | `profile_provider.dart:65,98,125`, `strava_auth_service.dart:70-117`, `profile_screen.dart:106-108` |
| "×{N}" | Contador de repeticiones de una serie en el editor | RUT | `routine_editor_screen.dart:403` |
| "Remo simulado", "Pulsómetro simulado", "SIMULADOR", "Simulador", "Watts", "Ritmo", "Hora escena", "Hora real", "Remando", "Simular desconexión", "Simular caída del pulsómetro", "Suave", "Medio", "Fuerte" | Modo simulador (solo builds de desarrollo) | SIM | `simulated_ble_service.dart:11`, `simulated_heart_rate_service.dart:13`, `simulator_overlay.dart:11-164` |

- Textos con respaldo fijo en español por si falta la traducción: `workoutCompleted ?? '¡Entrenamiento completado!'` y `workoutCompletedMessage ?? 'Has completado…'` (`immersive_workout_screen.dart:217,224`). La traducción nunca falta, así que siempre se ve la clave.
- Código muerto † con más textos hardcodeados: `workout_screen.dart:203-990` (pantalla de entrenamiento anterior, a la que ya no se navega) y `StepType.label` en `interval_step.dart:3-15` (nombres de tipo en español, sin uso).
- Los textos de protocolo también son fijos, pero ya están en el catálogo: `deviceSearchHint` con "0x1826".

**Casos borde.** Con la app en inglés, el usuario ve mezclas como "Or pick a routine: … 1 series · 0 descansos" o "Terminar / Pausa" en el entrenamiento inmersivo.

**UI.** Las de cada área.

**Datos persistidos.** Ninguno.

**Fuente legacy.** Las de la tabla.

**Tests legacy.** `test/series_panels_test.dart:39-73` (sin idioma configurado) verifica "500 m", "Reps", "Parciales 500 m", "Rep 1/3 · 300 m", "Sin repeticiones completas" y que sin serie no aparezca "Reps". `test/simulator_overlay_test.dart:56-76` verifica "Hora real", "Simular desconexión", "Simular caída del pulsómetro", "Fuerte" y "Remando". Esos tests fijan el texto en español sin depender del idioma.

**Dependencias.** F-APP-09, F-APP-10; HUD, RUT, HIS, BLE, PRF, STR, SIM.

**Mejora EXR.** *(Cambio de producto propuesto, a aprobar):* en Unity **todo texto visible sale del catálogo**. En español se usa exactamente el texto actual del legacy. En inglés se agrega una traducción equivalente ("Finish workout", "Up next:", "{n} series · {m} rests", etc.). Los textos de desarrollo (SIM, diagnóstico BLE) pueden quedar en un idioma fijo. Ver §8, P-02.

---

### F-APP-12: Convenciones de formato de números, horas y fechas

**Valor para el usuario.** Lectura uniforme de los números en toda la app.

**Comportamiento observable** (son reglas transversales y cada área es dueña de su uso):
1. **Separador decimal: siempre punto**, sin importar el idioma, por ejemplo SPM "23.5" también en español (todos los decimales usan formato fijo con punto, por ejemplo `device_screen.dart:254`, `immersive_workout_screen.dart:625`, `history_screen.dart:114`).
2. **Separador de miles: punto, fijo en estilo español**, solo en los metros de los paneles de series: "1.245 m" también en inglés (`series_panels.dart:24`). El resto de las distancias se muestra sin separador de miles ("1657m" en el historial).
3. **Hora del día: 24 h "HH:mm"** fija, sin importar el idioma ni la configuración del sistema (reloj de pared del inmersivo, `series_panels.dart:98`).
4. **Fecha y hora de sesión: "dd/MM/yyyy  HH:mm"** (dos espacios entre fecha y hora) fija para cualquier idioma (`history_screen.dart:74`).
5. Tiempos de cronómetro: "mm:ss" con ceros a la izquierda, o "h:mm:ss" desde una hora (`immersive_workout_screen.dart:570-576,965-971`). Los parciales usan "m:ss.d" y el split "m:ss" (`series_panels.dart:10-19`). El detalle está en HUD, HIS y RUT.

**Casos borde.** Ninguno propio de esta ficha.

**Fuente legacy.** Las citadas.

**Tests legacy.** `test/series_panels_test.dart:32-37` ("formatos"): `formatLapTime(166.667)` = "2:46.7", `formatLapTime(65)` = "1:05.0", `formatSplit(114.4)` = "1:54", `formatMeters(1245)` = "1.245 m".

**Dependencias.** F-APP-09; HUD, HIS, BLE, RUT.

**Mejora EXR.** Cifras de ancho fijo y alineadas, con unidades en tamaño menor y peso más liviano. *(Propuesta a aprobar:* formato de fecha según el idioma, "dd/MM/yyyy" en español y "MM/dd/yyyy" en inglés, y separadores según el idioma.)* Hasta que se apruebe, rige la paridad exacta de arriba.

---

### F-APP-13: Permisos de Bluetooth y privacidad por plataforma

**Valor para el usuario.** Poder conectar el remo y el pulsómetro con los permisos mínimos y entender qué hacer si el Bluetooth está apagado o el permiso fue rechazado.

**Comportamiento observable.**
1. **Android, permisos declarados** (`android/app/src/main/AndroidManifest.xml:4-19`):
   - Android 12 o superior (API 31+): "Buscar dispositivos cercanos", declarado **sin uso de ubicación**, y "Conectar con dispositivos cercanos".
   - Android 11 o inferior (API ≤ 30): Bluetooth, administración de Bluetooth y **ubicación precisa** (en esas versiones el sistema exige ubicación para buscar dispositivos BLE).
   - Mantener la pantalla encendida (F-APP-14).
   - **Bluetooth LE obligatorio en el hardware**: la app se declara incompatible con equipos sin BLE, y la tienda no la ofrece en esos equipos.
   - **Internet no figura en el manifiesto principal**. Solo está en los manifiestos de debug y profile (`android/app/src/debug/AndroidManifest.xml:6`). Hay que verificar si Strava funciona en el APK release (§8, P-07).
2. **iOS** (`ios/Runner/Info.plist:44-51`): explicación del uso de Bluetooth (dos claves, mismo texto) **"Se necesita Bluetooth para conectar con el monitor del rower."**, solo en español y sin mencionar el pulsómetro. Modo en segundo plano **"Bluetooth central"** habilitado, lo que permite seguir conectado al remo y al pulsómetro con la app en segundo plano.
3. **macOS** (`macos/Runner/Release.entitlements`, `DebugProfile.entitlements`, `Info.plist`): app aislada en "sandbox" con permiso de Bluetooth y de **conexiones de red salientes** (Strava). En debug también se permiten conexiones entrantes (herramientas de desarrollo). Explicación de Bluetooth: el mismo texto en español que en iOS.
4. **Windows**: no declara permisos. El Bluetooth se usa directamente (el filtrado de dispositivos está en BLE).
5. **No hay pantalla propia de permisos ni de onboarding**. El pedido lo dispara el sistema la primera vez que la app usa Bluetooth: en iOS al abrir la app (F-APP-01, punto 10) y en Android al buscar o conectar. Hay que verificarlo en un dispositivo.
6. **Bluetooth apagado o permiso denegado** (estado del adaptador "apagado" o "no autorizado"): la pestaña Dispositivo completa muestra un ícono de Bluetooth tachado de 64 en rojo, el título `deviceBtOff` ("Bluetooth apagado") o `deviceBtDenied` ("Permiso de Bluetooth denegado"), la pista `deviceBtOffHint` o `deviceBtDeniedHint` y un botón `deviceBtEnable` ("Activar Bluetooth") que pide al sistema encenderlo. Esa vista tapa la tarjeta del pulsómetro (`device_screen.dart:346-393`; el detalle está en BLE y HRM).
7. Si el usuario toca "Buscar" con el adaptador todavía sin estar listo, la app espera **hasta 5 s** a que se encienda. Si no se enciende, pasa a "apagado" (`ble_service.dart:67-81`, BLE).

**Casos borde y errores.**
- Las pistas hablan de "Centro de Control" y "Ajustes > Privacidad > Bluetooth", que son rutas de iOS, y se muestran igual en Android, Windows y macOS.
- El botón "Activar Bluetooth" solo tiene efecto en Android. En las demás plataformas el sistema no permite encender el Bluetooth desde una app: hay que verificar si falla en silencio o genera un error no manejado (§8, P-11).
- En Android, un permiso rechazado probablemente no se informa como "no autorizado" sino como error al buscar ("Error al escanear: …", texto rojo hardcodeado, F-APP-11). Hay que verificarlo.
- En Android ≤ 11, si la ubicación del sistema está apagada, la búsqueda puede no encontrar nada, y no hay ningún aviso específico.

**UI.** La del punto 6 (BLE).

**Datos persistidos.** Ninguno.

**Fuente legacy.** Las citadas; `CLAUDE.md` "Platform Notes"; `README.md` "Platform Notes".

**Tests legacy.** Ninguno.

**Dependencias.** F-APP-01, F-APP-09; BLE, HRM, STR (red).

**Mejora EXR.** Antes del pedido del sistema, una pantalla de bienvenida explica con una ilustración (remo, pulsómetro y teléfono) por qué se necesita Bluetooth. Si el permiso está denegado o el Bluetooth apagado, se ofrece un camino claro y propio de cada plataforma (botón que abre los ajustes correctos y pasos con capturas). Los textos del sistema se traducen y mencionan también el pulsómetro.

---

### F-APP-14: Pantalla siempre encendida

**Valor para el usuario.** La pantalla no se apaga mientras rema con las manos ocupadas.

**Comportamiento observable.**
1. La app pide mantener la pantalla encendida **una sola vez, al arrancar, antes de la primera pantalla**, y **nunca la libera** (`lib/main.dart:42`). En la práctica, la pantalla no se apaga **en ninguna pestaña** mientras la app está en primer plano: no solo durante el entrenamiento, también en Rutinas, Historial, Perfil, etc.
2. Aplica a todas las plataformas: Android (con el permiso declarado, F-APP-13), iOS, macOS y Windows.

**Casos borde y errores.**
- Si el pedido falla, no se avisa nada.
- El legacy no pide nada nuevo al volver del segundo plano. Hay que verificar en cada plataforma que el sistema conserve el pedido (§8, P-01).

**UI.** Ninguna.

**Datos persistidos.** Ninguno.

**Fuente legacy.** `lib/main.dart:9,42`; `AndroidManifest.xml:16-17`; `pubspec.yaml:32`.

**Tests legacy.** Ninguno.

**Dependencias.** F-APP-01, F-APP-13; WKT y HUD (donde más importa).

**Mejora EXR.** La pantalla jamás se apaga durante una sesión. *(Propuesta a aprobar:* limitarlo a Dispositivo conectado y entrenamiento, para ahorrar batería en el resto. Por defecto se mantiene la paridad, siempre encendida.)*

---

### F-APP-15: Modo inmersivo del sistema, orientación y ventana

**Valor para el usuario.** Remar con la escena a pantalla completa, en vertical u horizontal, y usar la app en escritorio con una ventana cómoda.

**Comportamiento observable.**
1. **Al entrar al entrenamiento inmersivo** se ocultan las barras del sistema (estado y navegación) en modo "inmersivo persistente": si el usuario las revela deslizando desde el borde, vuelven a ocultarse solas. Pasa cuando se crea la página, al empezar su transición de entrada (`immersive_workout_screen.dart:34`). **Al salir del inmersivo**, cuando la página se destruye al terminar su salida (`:39`), las barras vuelven a estar visibles y la app dibuja de borde a borde (`immersive_workout_screen.dart:32-40`; el detalle está en HUD).
2. Fuera del entrenamiento, la barra de estado está visible (`Info.plist:56-57`).
3. **Orientación: la app no se bloquea.** En iPhone: vertical, horizontal izquierda y horizontal derecha (no vertical invertida). En iPad: las cuatro (`Info.plist:31-43`). En Android sigue la rotación del sistema sin restricción y **sin reiniciar la pantalla** al rotar, así que se conserva el estado (`AndroidManifest.xml:30`). Las pantallas se acomodan a la orientación (cada área).
4. **Teclado en Android**: el contenido se achica para dejar lugar al teclado, en lugar de quedar tapado (`AndroidManifest.xml:32`), por ejemplo en el editor de rutina.
5. **Pantallas con muesca**: durante la pantalla de arranque, el contenido usa también el área de la muesca (`styles.xml`, "shortEdges").
6. **iOS de alta frecuencia**: la app puede dibujar a la frecuencia máxima de pantallas de 120 Hz (`Info.plist:52-53`). También admite entradas indirectas, como mouse o trackpad en iPad (`Info.plist:54-55`).
7. **iPad**: apto para iPhone y iPad (`project.pbxproj`, familia de dispositivos "1,2"). No exige pantalla completa, así que admite multitarea en pantalla dividida.
8. **Windows**: ventana inicial de **1280×720** (escalada según el DPI del monitor), ubicada en (10, 10) desde la esquina superior izquierda, redimensionable, con título **"rower_app"**. Cerrar la ventana cierra la app (`windows/runner/main.cpp:27-33`; compatibilidad de DPI por monitor en `runner.exe.manifest`).
9. **macOS**: ventana inicial de **800×600** (`macos/Runner/Base.lproj/MainMenu.xib:333-335`), redimensionable. Hay que verificar el título visible (§8).

**Casos borde.**
- Antes del primer entrenamiento, el modo de las barras es el que viene por defecto. Después del primer entrenamiento queda "de borde a borde" para el resto de la sesión de uso.
- En escritorio, "inmersivo" no oculta la barra de tareas ni pone la ventana en pantalla completa.

**UI.** Ver HUD para el inmersivo.

**Datos persistidos.** Ninguno: ni el tamaño ni la posición de la ventana se recuerdan.

**Fuente legacy.** Las citadas.

**Tests legacy.** Ninguno directo. Hay tests de otras áreas que verifican que las vistas no desborden en ventanas apaisadas o bajas: `heart_rate_card_test.dart:142-157` (640×360) y `simulator_overlay_test.dart:89-123` (800×360).

**Dependencias.** HUD, SCN, RUT.

**Mejora EXR.** Entrar al inmersivo se siente como "sumergirse": las barras del sistema se desvanecen junto con la transición a la escena. Al rotar, la escena y el HUD se reacomodan con una animación fluida, sin cortes. En escritorio, opción de pantalla completa real y una ventana que recuerde su tamaño y posición *(mejora a aprobar)*. El título de la ventana dice "RowMate" (§8, P-06).

---

### F-APP-16: Identidad de la app: nombre, identificadores, versión, ícono y pantalla de arranque

**Valor para el usuario.** Reconocer la app en el teléfono o la computadora y tener un arranque prolijo.

**Comportamiento observable.**
1. **Nombre visible: "RowMate"** en Android (etiqueta del ícono, `AndroidManifest.xml:22`), iOS (`Info.plist:7-8,15-16`), macOS (`AppInfo.xcconfig`, `PRODUCT_NAME = RowMate`) y como título general de la app (`lib/main.dart:94`). En **Windows** el ejecutable es `rower_app.exe`, y el título de ventana y el nombre de producto son "rower_app" (`windows/runner/main.cpp:29`, `Runner.rc:92-98`).
2. **Identificadores**:

   | Plataforma | Identificador | Fuente |
   |---|---|---|
   | Android | `com.example.rower_app` (es el valor por defecto de la plantilla) | `android/app/build.gradle.kts:9,24` |
   | iOS | `com.iguisoft.rowmate` | `ios/Runner.xcodeproj/project.pbxproj:485,672,696` |
   | macOS | `com.iguisoft.rowmate` | `macos/Runner/Configs/AppInfo.xcconfig` |
   | Windows | Empresa "com.example", producto "rower_app" | `windows/runner/Runner.rc:92-98` |

3. **Versión**: 1.0.0, compilación 1 (`pubspec.yaml:4`), en todas las plataformas.
4. **Requisitos mínimos**: iOS 15.0 (`project.pbxproj:461,595,646`), macOS 10.15 (`macos/.../project.pbxproj:560`), Android = el mínimo por defecto del kit de Flutter (`flutter.minSdkVersion` = API 24, Android 7.0, con el Flutter 3.47.6 instalado; los docs dicen API 21, §6; en la app nueva, Android 8.0 ARM64 por Q-90) y Windows 10 o superior.
5. **Derechos**: macOS "Copyright © 2026 iguisoft. All rights reserved."; Windows "Copyright (C) 2026 com.example. All rights reserved."; licencia MIT © 2026 iguisoft (`README.md`, `LICENSE`).
6. **Descripción del proyecto**: "RowMate — Monitor de rower AMS-670B con rutinas de entrenamiento" (`pubspec.yaml:2`).
7. **Ícono** (`assets/icon/icon.png`, 640×640; el contenido es en realidad JPEG aunque la extensión diga PNG): fondo azul pizarra muy oscuro, con dos remos cruzados en línea neón cian y azul brillante, con resplandor, sobre una letra "R" de trazo fino del mismo color. Se genera para Android, iOS (sin transparencia) y macOS (`pubspec.yaml:49-57`). **Windows usa el ícono por defecto de Flutter**, porque no está incluido en la generación (`windows/runner/resources/app_icon.ico`, 33 772 bytes, que es el tamaño del ícono por defecto).
8. **Pantalla de arranque nativa**:
   - Android anterior a 12: fondo completo **`#0D1B2A`** con el logo (la misma ilustración del ícono) centrado. Es igual en modo claro y oscuro (`res/drawable*/launch_background.xml`, `background.png` = `#0D1B2A`, `splash.png` de 480 px en xxhdpi).
   - Android 12 o superior: pantalla de arranque del sistema con fondo **`#0D1B2A`** y el ícono `android12splash` (`res/values-v31/styles.xml`, `values-night-v31/styles.xml`).
   - **iOS: pantalla blanca**. El storyboard tiene fondo blanco y una imagen transparente de 1×1. Las imágenes oscuras con logo existen en el proyecto pero ninguna configuración las usa (`LaunchScreen.storyboard:16-22`, `LaunchImage.imageset/Contents.json`). Es un defecto del legacy (§6, D-10).
   - macOS y Windows: sin pantalla de arranque.
   - Después de la pantalla de arranque, el fondo de la app también es `#0D1B2A` (F-APP-05), así que en Android no hay cambio de color.

**Casos borde.** En Windows y en el identificador de Android quedaron nombres de plantilla. Cambiarlos en la app nueva tiene impacto (§8, P-05).

**UI.** La descrita.

**Datos persistidos.** Ninguno.

**Fuente legacy.** Las citadas.

**Tests legacy.** Ninguno.

**Dependencias.** F-APP-05, F-APP-19.

**Mejora EXR.** Un ícono profesional que se lea bien a 16 px y a 1024 px, con versión adaptable en Android y en todas las plataformas, incluida Windows. Una pantalla de arranque oscura con el logo en todas las plataformas, que también en iOS da paso a una animación breve de marca (por ejemplo, los remos que se cruzan con un destello de agua). Nombres e identificadores coherentes con "RowMate".

---

### F-APP-17: Regreso a la app después del login externo (esquema `rowmate://`)

**Valor para el usuario.** Después de autorizar Strava en el navegador, volver solo a RowMate ya conectado.

**Comportamiento observable.**
1. La app registra el esquema de enlace **`rowmate`**. Strava redirige a **`rowmate://rowmate.app/callback`** (`lib/core/strava/strava_config.dart:9-10`).
2. Android: una pantalla receptora acepta los enlaces `rowmate://` que vienen del navegador y devuelve el resultado al login en curso. La pantalla principal está en modo "instancia única" para que el regreso no abra una segunda copia de la app (`AndroidManifest.xml:28,41-51`).
3. iOS: esquema `rowmate` registrado (`Info.plist:58-68`).
4. macOS: **no** registra el esquema en su Info.plist. Hay que verificar si el login funciona igual (§8, P-08).
5. Windows: no hay registro de esquema. Hay que verificar si el login de Strava funciona en Windows (§8, P-08).
6. El flujo de login completo (permisos pedidos, tokens, errores) está en STR.

**Casos borde.** Un enlace `rowmate://` abierto sin un login en curso no tiene efecto visible.

**UI.** Ver STR y PRF.

**Datos persistidos.** Ninguno propio (los tokens son de STR).

**Fuente legacy.** Las citadas.

**Tests legacy.** Ninguno.

**Dependencias.** F-APP-04; STR, PRF.

**Mejora EXR.** Login de Strava dentro de una hoja integrada que vuelve con una confirmación visual ("Conectado a Strava" con el avatar del atleta), sin que el usuario sienta que salió de la app.

---

### F-APP-18: Variante de compilación "simulador" vista desde el shell

**Valor para el usuario (desarrollador y QA).** Probar toda la app sin remo ni pulsómetro, sin ensuciar el historial real y sin subir nada a Strava. **Nunca llega a usuarios finales.**

**Comportamiento observable** (el detalle funcional está en el inventario SIM):
1. Se activa **solo al compilar**, con el indicador `SIMULATOR=true`. Sin él, los builds normales y los de release no incluyen nada del simulador (`lib/core/dev/dev_config.dart:5`). Ningún script de CI lo activa, y eso tiene que seguir así (`CLAUDE.md`, "Dev Mode").
2. Hay dos configuraciones de ejecución en el editor: **"RowMate"** (normal) y **"RowMate (simulador)"** (con el indicador) (`.vscode/launch.json:4-16`).
3. Efectos a nivel app:
   - Remo y pulsómetro simulados, que comparten el mismo atleta simulado (F-APP-01, punto 3).
   - **Base de datos separada**: `rower_app_dev.db` en lugar de `rower_app.db` (`lib/core/database/database_service.dart:11`).
   - **Nada se sube a Strava**. El login y la importación sí funcionan (`strava_api_service.dart:81`, `immersive_workout_screen.dart:151,199`).
   - **Capa del simulador por encima de todo**: cinta naranja "SIMULADOR" en la esquina superior izquierda y botón flotante con llave (🛠) abajo a la derecha, visibles en **todas** las pantallas, incluidas las apiladas y los diálogos (`lib/main.dart:108-110`; capturas `docs/image-01.png`, `image-02.png`, `image-04.png`, `image-06.png`).
   - La capa vive fuera del sistema de navegación, así que dentro de ella no hay menús desplegables, hojas inferiores ni textos de ayuda emergentes (SIM).

**Casos borde.** No hay modo "remo simulado y pulsómetro real" (HRM, SIM).

**UI.** Ver SIM.

**Datos persistidos.** Base separada. El simulador no persiste nada más (SIM).

**Fuente legacy.** Las citadas; `docs/superpowers/specs/2026-10-01-dev-simulator-design.md`.

**Tests legacy.**
- `test/simulator_overlay_test.dart:17-86` ("el panel se abre fuera del Navigator y + sube los watts"). Con la capa montada **igual que en la app**, por encima de la navegación, verifica lo siguiente. Antes de abrir, "150 W" no se ve. Al tocar la llave aparecen "150 W" y "24 spm". "+" de vatios lleva el objetivo a 160 y muestra "160 W". "+" de SPM lo lleva a 25. "+" de hora muestra "Hora real". "Simular desconexión" no genera errores. Con el pulsómetro simulado conectado, "Simular caída del pulsómetro" lo deja desconectado y reintentando. "Fuerte" fija 280 W y 30 spm. "Remando" apaga el remado. Cerrar oculta el panel.
- `test/simulator_overlay_test.dart:89-123` ("en una ventana baja el botón de cerrar sigue accesible"). En una ventana de 800×360 el panel se abre y se cierra con su botón.

**Dependencias.** F-APP-01, F-APP-19; SIM, BLE, HRM, DB, STR.

**Mejora EXR.** La capa de desarrollo es discreta, se puede ocultar y nunca tapa información clave del HUD. Sigue siendo imposible que llegue a un build de usuario.

---

### F-APP-19: Compilación y distribución (CI/CD, firma, configuraciones de ejecución)

**Valor para el usuario.** Recibir builds instalables, hoy solo para iPhone y iPad por instalación manual (sideload).

**Comportamiento observable.**
1. **GitHub Actions "Build iOS App"** (`.github/workflows/build-ios.yml`): corre en cada push a `main` o `master` y a mano. Prepara el kit estable, genera el archivo de credenciales de Strava a partir de los secretos `STRAVA_CLIENT_ID` y `STRAVA_CLIENT_SECRET`, compila iOS release **sin firmar**, empaqueta un **`RowMate.ipa`** y lo publica como artefacto descargable **"RowMate-App"**.
2. **Codemagic "ios-unsigned"** ("iOS sin firmar para sideload", `codemagic.yaml`): Mac mini M2, tope de 60 minutos y kit estable. Compila iOS release sin firmar y empaqueta **`app.ipa`** (artefacto `build/ios/iphoneos/app.ipa`). **No genera el archivo de credenciales de Strava** (§8, P-09).
3. No hay CI para Android, Windows ni macOS, y **ningún CI corre tests ni análisis de código** (§6, D-09).
4. **Android release se firma con la clave de debug** (`android/app/build.gradle.kts:33-38`), así que no es apto para tiendas.
5. No hay publicación en tiendas. La distribución es por IPA sin firmar (sideload) y por builds locales (README "Quick Start"). Existe un script local de instalación en iPad que está fuera del control de versiones (`.gitignore`: `install_ipad.sh`).
6. Requisitos de desarrollo en Windows documentados: Visual Studio con C++ de escritorio y el "Modo desarrollador" de Windows (`README.md`, `CLAUDE.md`).
7. Configuraciones de ejecución: F-APP-18, punto 2.

**Casos borde.** Ver F-APP-04 (build sin secretos = sin Strava).

**UI.** Ninguna.

**Datos persistidos.** Ninguno.

**Fuente legacy.** Las citadas; `analysis_options.yaml` (reglas de estilo; excluye `build/` y las carpetas de plataforma).

**Tests legacy.** Ninguno.

**Dependencias.** F-APP-04, F-APP-16, F-APP-18; STR.

**Mejora EXR.** Builds reproducibles para las cuatro plataformas, con tests automáticos en cada cambio y builds de prueba firmadas (TestFlight o prueba interna en Android), para que el usuario pruebe cada iteración sin pasos manuales. *(Es una restricción de producto para el Unity Architect, no una decisión técnica.)*

---

## 3. Anexo A: catálogo completo de textos `.arb`

Leyenda: "⏎" es un salto de línea dentro del texto. † indica uso en código muerto (`lib/features/workout/workout_screen.dart:203-990`). Los nombres de tipo de paso se muestran a través del nombre localizado del tema (`shared/theme.dart:42-53`), que usan `routine_editor_screen.dart:343,578`, `immersive_workout_screen.dart:386,439`, `workout_screen.dart:672`† e `interval_tile.dart:50` (sin uso).

| Clave | es | en | Placeholders | Uso en código (`lib/...:línea`) |
|---|---|---|---|---|
| `navDevice` | Dispositivo | Device |  | main.dart:154 |
| `navWorkout` | Workout | Workout |  | main.dart:159 |
| `navRoutines` | Rutinas | Routines |  | main.dart:164 |
| `navHistory` | Historial | History |  | main.dart:169 |
| `navProfile` | Perfil | Profile |  | main.dart:175 |
| `deviceTitle` | Dispositivo | Device |  | features/device/device_screen.dart:19 |
| `deviceDisconnect` | Desconectar | Disconnect |  | features/device/device_screen.dart:26 |
| `deviceConnected` | Conectado | Connected |  | features/device/device_screen.dart:112 |
| `deviceSearching` | Buscando... | Searching... |  | features/device/device_screen.dart:404 |
| `deviceSearch` | Buscar dispositivos | Search devices |  | features/device/device_screen.dart:404 |
| `deviceSearchHint` | Buscando dispositivos FTMS con servicio 0x1826... | Searching for FTMS devices with service 0x1826... |  | features/device/device_screen.dart:413 |
| `deviceNoResults` | No se encontraron dispositivos | No devices found |  | features/device/device_screen.dart:430 |
| `deviceNoResultsHint` | Asegurate que el rower esté encendido ⏎ y el Bluetooth activado | Make sure the rower is on ⏎ and Bluetooth is enabled |  | features/device/device_screen.dart:434 |
| `deviceBtOff` | Bluetooth apagado | Bluetooth is off |  | features/device/device_screen.dart:376 |
| `deviceBtOffHint` | Activá el Bluetooth desde el Centro de Control ⏎ o Ajustes para buscar el rower | Turn on Bluetooth from Control Center ⏎ or Settings to search for the rower |  | features/device/device_screen.dart:381 |
| `deviceBtDenied` | Permiso de Bluetooth denegado | Bluetooth permission denied |  | features/device/device_screen.dart:376 |
| `deviceBtDeniedHint` | Habilitá el permiso de Bluetooth en ⏎ Ajustes > Privacidad > Bluetooth | Enable Bluetooth permission in ⏎ Settings > Privacy > Bluetooth |  | features/device/device_screen.dart:381 |
| `deviceBtEnable` | Activar Bluetooth | Enable Bluetooth |  | features/device/device_screen.dart:389 |
| `hrmTitle` | Pulsómetro | Heart rate sensor |  | features/device/heart_rate_card.dart:42 (se muestra en MAYÚSCULAS) |
| `hrmNoSensor` | Sin sensor | No sensor |  | features/device/heart_rate_card.dart:100 |
| `hrmWatchHint` | Un smartwatch (Wear OS, Apple Watch) necesita una app que transmita el pulso por Bluetooth, p. ej. Heart for Bluetooth en Wear OS. | A smartwatch (Wear OS, Apple Watch) needs an app that broadcasts heart rate over Bluetooth, e.g. Heart for Bluetooth on Wear OS. |  | features/device/heart_rate_card.dart:103 |
| `hrmSearch` | Buscar | Search |  | features/device/heart_rate_card.dart:110 |
| `hrmSearchOther` | Buscar otro | Search another |  | features/device/heart_rate_card.dart:219 |
| `hrmSearching` | Buscando sensores... | Searching for sensors... |  | features/device/heart_rate_card.dart:125 |
| `hrmConnectingTo` | Conectando a {name}... | Connecting to {name}... | {name}: String | features/device/heart_rate_card.dart:84 |
| `hrmConnect` | Conectar | Connect |  | features/device/heart_rate_card.dart:214 |
| `hrmDisconnect` | Desconectar | Disconnect |  | features/device/heart_rate_card.dart:191 |
| `hrmForget` | Olvidar | Forget |  | features/device/heart_rate_card.dart:224 |
| `hrmReconnecting` | reconectando... | reconnecting... |  | features/device/heart_rate_card.dart:199 |
| `hrmNotFound` | no encontrado | not found |  | features/device/heart_rate_card.dart:199 |
| `hrmUnknownSensor` | Sensor desconocido | Unknown sensor |  | features/device/heart_rate_card.dart:236 |
| `hrmIncompatible` | No es un pulsómetro compatible (sin servicio Heart Rate) | Not a compatible heart rate sensor (no Heart Rate service) |  | features/device/heart_rate_card.dart:56 |
| `hrmConnectError` | Error al conectar: {error} | Connection error: {error} | {error}: String | features/device/heart_rate_card.dart:57 |
| `hrmBpm` | bpm | bpm |  | features/device/heart_rate_card.dart:178 |
| `workoutTitle` | Entrenamiento | Workout |  | features/workout/workout_screen.dart:53 |
| `workoutFree` | Entrenamiento libre | Free workout |  | features/history/session_detail_screen.dart:87, features/workout/workout_screen.dart:93 |
| `workoutRowerNotConnected` | Conectá el rower para iniciar un entrenamiento | Connect the rower to start a workout |  | features/workout/workout_screen.dart:72 |
| `workoutOrPickRoutine` | ␣␣O elegí una rutina: | ␣␣Or pick a routine: |  | features/workout/workout_screen.dart:99 (empieza con 2 espacios) |
| `workoutNoRoutines` | Creá una rutina en la pestaña "Rutinas" | Create a routine in the "Routines" tab |  | features/workout/workout_screen.dart:133 |
| `workoutInProgress` | Entrenamiento en curso... | Workout in progress... |  | features/workout/workout_screen.dart:198 |
| `workoutScene` | Escenario | Scenery |  | features/workout/workout_screen.dart:79 |
| `sceneLake` | Lago alpino | Alpine lake |  | features/workout/scene/environment_picker.dart:9 |
| `sceneRiver` | Río urbano | City river |  | features/workout/scene/environment_picker.dart:10 |
| `sceneCoast` | Costa | Coast |  | features/workout/scene/environment_picker.dart:11 |
| `sceneRegatta` | Canal de regata | Regatta course |  | features/workout/scene/environment_picker.dart:12 |
| `workoutFinish` | Terminar | Finish |  | features/workout/immersive_workout_screen.dart:237, :289; workout_screen.dart:353†, :399†, :525†, :545† |
| `workoutFinishTitle` | Terminar entrenamiento | Finish workout |  | **solo código muerto**: workout_screen.dart:537† |
| `workoutFinishContent` | Se guardará la sesión. ¿Continuar? | The session will be saved. Continue? |  | **solo código muerto**: workout_screen.dart:538† |
| `workoutPause` | Pausa | Pause |  | **solo código muerto**: workout_screen.dart:942† |
| `workoutResume` | Reanudar | Resume |  | **solo código muerto**: workout_screen.dart:933† |
| `workoutStartTitle` | Iniciar entrenamiento? | Start workout? |  | **sin uso** |
| `workoutStartContent` | {summary} ⏎ ⏎ Iniciar el entrenamiento? | {summary} ⏎ ⏎ Start the workout? | {summary}: String | features/workout/workout_screen.dart:152 |
| `workoutStart` | Iniciar | Start |  | features/workout/workout_screen.dart:159 |
| `workoutCompleted` | ¡Entrenamiento completado! | Workout Completed! |  | features/workout/immersive_workout_screen.dart:217; workout_screen.dart:333† |
| `workoutCompletedMessage` | Has completado tu rutina exitosamente. | You have successfully completed your routine. |  | features/workout/immersive_workout_screen.dart:224; workout_screen.dart:340† |
| `metricTime` | TIEMPO | TIME |  | **solo código muerto**: workout_screen.dart:773† |
| `metricSplit` | SPLIT 500m | SPLIT 500m |  | features/device/device_screen.dart:253; workout_screen.dart:774† |
| `metricDistance` | DISTANCIA | DISTANCE |  | features/device/device_screen.dart:256; workout_screen.dart:775† |
| `metricWatts` | VATIOS | WATTS |  | features/device/device_screen.dart:255; workout_screen.dart:776† |
| `metricSpm` | SPM | SPM |  | features/device/device_screen.dart:254; workout_screen.dart:777† |
| `metricCalories` | CALORÍAS | CALORIES |  | **solo código muerto**: workout_screen.dart:778† |
| `metricHeartRate` | PULSO | HR |  | features/device/device_screen.dart:258; workout_screen.dart:780† |
| `metricFreeLabel` | Entrenamiento libre | Free workout |  | **solo código muerto**: workout_screen.dart:509† |
| `routinesTitle` | Rutinas | Routines |  | features/routines/routines_screen.dart:16 |
| `routinesEmpty` | No hay rutinas todavía | No routines yet |  | features/routines/routines_screen.dart:33 |
| `routinesEmptyHint` | Presioná + para crear una | Press + to create one |  | features/routines/routines_screen.dart:36 |
| `routineEdit` | Editar | Edit |  | features/routines/routines_screen.dart:69 |
| `routineDelete` | Eliminar | Delete |  | features/routines/routines_screen.dart:72 |
| `routineDeleteTitle` | Eliminar rutina | Delete routine |  | features/routines/routines_screen.dart:101 |
| `routineDeleteContent` | ¿Eliminar "{name}"? | Delete "{name}"? | {name}: String | features/routines/routines_screen.dart:102 |
| `historyTitle` | Historial | History |  | features/history/history_screen.dart:21 |
| `historyEmpty` | No hay sesiones guardadas | No saved sessions |  | features/history/history_screen.dart:31 |
| `historyFree` | Libre | Free |  | features/history/history_screen.dart:91, features/profile/profile_screen.dart:390 |
| `historyStatTime` | Tiempo | Time |  | features/history/history_screen.dart:111, features/history/session_detail_screen.dart:162 |
| `historyStatDistance` | Distancia | Distance |  | features/history/history_screen.dart:112, features/history/session_detail_screen.dart:163 |
| `historyStatWatts` | Vatios | Watts |  | **sin uso** (se muestra "Watts" hardcodeado) |
| `historyStatCalories` | kcal | kcal |  | **sin uso** |
| `historyStatHeartRate` | Pulso | HR |  | features/history/history_screen.dart:118, features/history/session_detail_screen.dart:169 |
| `sessionNoTelemetry` | No hay datos de telemetría | No telemetry data |  | features/history/session_detail_screen.dart:126 |
| `sessionSummary` | Resumen | Summary |  | features/history/session_detail_screen.dart:157 |
| `sessionStepBreakdown` | Rendimiento por paso | Step Performance |  | features/history/session_detail_screen.dart:201 |
| `sessionChartWatts` | Vatios | Watts |  | features/history/session_detail_screen.dart:68 |
| `sessionChartSpm` | SPM | SPM |  | features/history/session_detail_screen.dart:69 |
| `sessionChartSplit` | Split 500m | Split 500m |  | features/history/session_detail_screen.dart:70 |
| `sessionChartDistance` | Distancia | Distance |  | features/history/session_detail_screen.dart:71 |
| `sessionChartHr` | Pulso | HR |  | features/history/session_detail_screen.dart:72 |
| `stepTypeWarmup` | Calentamiento | Warmup |  | shared/theme.dart:45 |
| `stepTypeWork` | Trabajo | Work |  | shared/theme.dart:47 |
| `stepTypeRest` | Descanso | Rest |  | shared/theme.dart:49 |
| `stepTypeCooldown` | Enfriamiento | Cooldown |  | shared/theme.dart:51 |
| `cancel` | Cancelar | Cancel |  | features/profile/profile_screen.dart:126, features/routines/routines_screen.dart:106, routine_editor_screen.dart:171, :732, features/workout/immersive_workout_screen.dart:179, workout_screen.dart:156; workout_screen.dart:476†, :542†, :585† |
| `delete` | Eliminar | Delete |  | features/routines/routines_screen.dart:111 |
| `ok` | OK | OK |  | features/routines/routine_editor_screen.dart:174 |
| `confirm` | Confirmar | Confirm |  | features/routines/routine_editor_screen.dart:735 |
| `save` | Guardar | Save |  | features/routines/routine_editor_screen.dart:209 |
| `editorNewRoutine` | Nueva rutina | New routine |  | features/routines/routine_editor_screen.dart:207 |
| `editorEditRoutine` | Editar rutina | Edit routine |  | features/routines/routine_editor_screen.dart:207 |
| `editorRoutineName` | Nombre de la rutina | Routine name |  | features/routines/routine_editor_screen.dart:221 |
| `editorDescription` | Descripción (opcional) | Description (optional) |  | features/routines/routine_editor_screen.dart:230 |
| `editorStepsCount` | Pasos ({count}) | Steps ({count}) | {count}: int | features/routines/routine_editor_screen.dart:244 |
| `editorStep` | Paso | Step |  | features/routines/routine_editor_screen.dart:250 |
| `editorSeries` | Serie | Series |  | features/routines/routine_editor_screen.dart:256, :388 |
| `editorEmptySteps` | Todavía no hay pasos. ⏎ Presioná "Paso" o "Serie". | No steps yet. ⏎ Press "Step" or "Series". |  | features/routines/routine_editor_screen.dart:265 |
| `editorDeleteSeries` | Eliminar serie | Delete series |  | features/routines/routine_editor_screen.dart:416 |
| `editorAddStepToSeries` | Agregar paso a serie | Add step to series |  | features/routines/routine_editor_screen.dart:437 |
| `editorRepetitions` | Repeticiones | Repetitions |  | features/routines/routine_editor_screen.dart:158 |
| `editorAmount` | Cantidad | Amount |  | features/routines/routine_editor_screen.dart:164 |
| `editorNameEmpty` | El nombre no puede estar vacío | Name cannot be empty |  | features/routines/routine_editor_screen.dart:44 |
| `editorNoSteps` | Agregá al menos un paso | Add at least one step |  | features/routines/routine_editor_screen.dart:50 |
| `editorNewStep` | Nuevo paso | New step |  | features/routines/routine_editor_screen.dart:568 |
| `editorEditStep` | Editar paso | Edit step |  | features/routines/routine_editor_screen.dart:568 |
| `editorStepType` | Tipo | Type |  | features/routines/routine_editor_screen.dart:576 |
| `editorByTime` | Por tiempo | By time |  | features/routines/routine_editor_screen.dart:587 |
| `editorByDistance` | Por distancia | By distance |  | features/routines/routine_editor_screen.dart:588 |
| `editorMinutes` | Min | Min |  | features/routines/routine_editor_screen.dart:604 |
| `editorSeconds` | Seg | Sec |  | features/routines/routine_editor_screen.dart:613 |
| `editorMeters` | Metros | Meters |  | features/routines/routine_editor_screen.dart:624 |
| `editorOptionalTargets` | Objetivos opcionales | Optional targets |  | features/routines/routine_editor_screen.dart:629 |
| `editorWattsMin` | W mín | W min |  | features/routines/routine_editor_screen.dart:640 |
| `editorWattsMax` | W máx | W max |  | features/routines/routine_editor_screen.dart:649 |
| `editorTargetSpm` | SPM objetivo | Target SPM |  | features/routines/routine_editor_screen.dart:661 |
| `editorSplitMin` | Split min | Split min |  | features/routines/routine_editor_screen.dart:673 |
| `editorSplitSec` | Split seg | Split sec |  | features/routines/routine_editor_screen.dart:682 |
| `editorProgressions` | Progresiones (solo aplica en series) | Progressions (only applies to series) |  | features/routines/routine_editor_screen.dart:691 |
| `editorProgressionSpm` | Incremento SPM (+/rep) | SPM increment (+/rep) |  | features/routines/routine_editor_screen.dart:699 |
| `editorProgressionSpmHint` | Ej: 2 | e.g: 2 |  | features/routines/routine_editor_screen.dart:701 |
| `editorProgressionWatts` | Incremento Watts (+/rep) | Watts increment (+/rep) |  | features/routines/routine_editor_screen.dart:710 |
| `editorProgressionWattsHint` | Ej: 5 | e.g: 5 |  | features/routines/routine_editor_screen.dart:712 |
| `editorProgressionSplit` | Cambio Split segundos (+/-/rep) | Split change in seconds (+/-/rep) |  | features/routines/routine_editor_screen.dart:721 |
| `editorProgressionSplitHint` | Ej: -2 (más rápido) | e.g: -2 (faster) |  | features/routines/routine_editor_screen.dart:723 |
| `profileTitle` | Perfil | Profile |  | features/profile/profile_screen.dart:18 |
| `profileConnectStrava` | Conectar con Strava | Connect with Strava |  | features/profile/profile_screen.dart:97 |
| `profileConnected` | Conectado | Connected |  | features/profile/profile_screen.dart:75 |
| `profileConnectedToStrava` | Conectado a Strava | Connected to Strava |  | features/profile/profile_screen.dart:78 |
| `profileDisconnect` | Desconectar | Disconnect |  | features/profile/profile_screen.dart:86, :129 |
| `profileDisconnectTitle` | Desconectar Strava | Disconnect Strava |  | features/profile/profile_screen.dart:121 |
| `profileDisconnectContent` | Tus sesiones no se subirán automáticamente. ¿Continuar? | Your sessions won't be uploaded automatically. Continue? |  | features/profile/profile_screen.dart:122 |
| `profileUploadSettings` | Configuración de subida | Upload Settings |  | features/profile/profile_screen.dart:152 |
| `profileAutoUpload` | Subida automática | Auto-upload |  | features/profile/profile_screen.dart:156 |
| `profileAutoUploadDesc` | Subir sesiones a Strava automáticamente al terminar | Upload sessions to Strava automatically after finishing |  | features/profile/profile_screen.dart:157 |
| `profileAskUpload` | Preguntar cada vez | Ask me each time |  | features/profile/profile_screen.dart:163 |
| `profileAskUploadDesc` | Mostrar un diálogo después de cada entrenamiento | Show a dialog after each workout |  | features/profile/profile_screen.dart:164 |
| `profileManualUpload` | Solo manual | Manual only |  | features/profile/profile_screen.dart:170 |
| `profileManualUploadDesc` | Subir sesiones manualmente desde el Historial | Upload sessions manually from History |  | features/profile/profile_screen.dart:171 |
| `profileSync` | Sincronización | Sync |  | features/profile/profile_screen.dart:228 |
| `profileSyncDesc` | Descargar tus actividades de remo desde Strava | Download your rowing activities from Strava |  | features/profile/profile_screen.dart:268 |
| `profileSyncFromStrava` | Sincronizar desde Strava | Sync from Strava |  | features/profile/profile_screen.dart:296 |
| `profileSyncToStrava` | Sincronizar a Strava | Sync to Strava |  | features/profile/profile_screen.dart:260 |
| `profileSyncing` | Sincronizando... | Syncing... |  | features/profile/profile_screen.dart:295 |
| `profileUploading` | Subiendo... | Uploading... |  | features/profile/profile_screen.dart:259 |
| `profileSyncResult` | {count} sesiones sincronizadas | {count} sessions synced | {count}: int | features/profile/profile_screen.dart:282 |
| `profileUploadResult` | {count} sesiones subidas | {count} sessions uploaded | {count}: int | features/profile/profile_screen.dart:245 |
| `profileSyncToDesc` | Subir todas las sesiones locales sin sincronizar a Strava | Upload all unsynced local sessions to Strava |  | features/profile/profile_screen.dart:231 |
| `profilePendingUploads` | Subidas pendientes | Pending Uploads |  | features/profile/profile_screen.dart:350 |
| `profileUploadToStrava` | Subir a Strava | Upload to Strava |  | features/history/session_detail_screen.dart:103, features/workout/immersive_workout_screen.dart:174, :185, :258; workout_screen.dart:371†, :471†, :482†, :580†, :591† |
| `profileUploadingToStrava` | Subiendo a Strava... | Uploading to Strava... |  | features/workout/immersive_workout_screen.dart:160, :273; workout_screen.dart:386†, :458†, :567† |
| `profileUploaded` | Subido a Strava | Uploaded to Strava |  | features/history/session_detail_screen.dart:113, features/profile/profile_screen.dart:361, features/workout/immersive_workout_screen.dart:166, :249, :280; workout_screen.dart:363†, :391†, :463†, :572† |
| `profileUploadFailed` | Error al subir | Upload failed |  | features/history/session_detail_screen.dart:114, features/workout/immersive_workout_screen.dart:166, :250, :281; workout_screen.dart:363†, :391†, :463†, :572† |
| `profileAskUploadDialog` | ¿Subir esta sesión a Strava? | Upload this session to Strava? |  | features/workout/immersive_workout_screen.dart:175; workout_screen.dart:472†, :581† |

---

## 4. Datos persistidos de toda la app (mapa global, por dueño)

El área APP no persiste nada propio. Este mapa sirve para que el shell sepa qué estado sobrevive a un reinicio:

| Almacén | Clave o tabla | Contenido | Dueño |
|---|---|---|---|
| Preferencias | `hrm.deviceId`, `hrm.deviceName` | Pulsómetro recordado | HRM |
| Preferencias | `scene.environment` | Escenario elegido (`lake`, `river`, `coast` o `regatta`; por defecto `lake`) | SCN |
| Preferencias | `immersive.lapsExpanded`, `immersive.repsExpanded` | Paneles de series expandidos (por defecto colapsados) | HUD |
| Preferencias | `strava_upload_pref` | Modo de subida a Strava | PRF |
| Almacenamiento seguro | `strava_access_token`, `strava_refresh_token`, `strava_expires_at`, `strava_athlete_name`, `strava_athlete_avatar` | Sesión de Strava (en macOS va en el llavero clásico, sin llavero de protección de datos) | STR |
| Base local | `rower_app.db` (`rower_app_dev.db` en simulador): tablas `routines`, `interval_steps`, `workout_sessions`, `data_points` | Rutinas, sesiones y telemetría | DB |
| Sin persistir | Pestaña elegida, idioma, tamaño de ventana, hora forzada del simulador | — | APP, SIM |

---

## 5. Tests legacy relacionados y qué afirman

| Test | Caso | Qué afirma | Ficha |
|---|---|---|---|
| `test/widget_test.dart` | "App smoke test" | Nada: está vacío (placeholder) | F-APP-01 |
| `test/scene/environment_picker_test.dart` | "tocar una tarjeta cambia el escenario y lo persiste" | Con idioma español se ve "Lago alpino". Al tocar "Río urbano" el escenario pasa a `river` y se guarda `scene.environment = river` | F-APP-09, F-APP-10 (textos); SCN |
| `test/scene/environment_picker_test.dart` | "arranca con el escenario guardado" | Si estaba guardado `coast`, a los 50 ms el escenario es `coast` | SCN (contexto F-APP-01) |
| `test/heart_rate_card_test.dart` | "sin sensor → buscar → conectar → bpm → desconectar" | En español: "PULSÓMETRO" (título en mayúsculas), "Sin sensor", texto con "Heart for Bluetooth", "Buscando sensores...", "--" sin lectura | F-APP-10; HRM |
| `test/heart_rate_card_test.dart` | "los escaneos del remo y del pulsómetro se excluyen" | El botón "Buscar dispositivos" (en español) se desactiva mientras escanea el pulsómetro | F-APP-10; BLE, HRM |
| `test/heart_rate_card_test.dart` | "en landscape la vista de escaneo no desborda" | En 640×360 no hay desbordes y la lista del pulsómetro queda en su propio scroll | F-APP-15; HRM |
| `test/series_panels_test.dart` | "formatos" | "2:46.7", "1:05.0", "1:54", "1.245 m" | F-APP-12; HUD |
| `test/series_panels_test.dart` | "arrancan colapsados…", "respeta la preferencia guardada", "sin serie no aparece el panel de repeticiones" | Textos fijos en español sin idioma configurado ("500 m", "Reps", "Parciales 500 m", "Rep 1/3 · 300 m", "Sin repeticiones completas") | F-APP-11; HUD |
| `test/simulator_overlay_test.dart` | "el panel se abre fuera del Navigator y + sube los watts" | La capa funciona montada por encima de la navegación (detalle en F-APP-18) | F-APP-18; SIM |
| `test/simulator_overlay_test.dart` | "en una ventana baja el botón de cerrar sigue accesible" | En 800×360 el panel se abre y se cierra | F-APP-15, F-APP-18 |

Lo que hoy no cubre ningún test y la app Unity tiene que cubrir con criterios de aceptación propios: la composición y el orden de las pestañas, Perfil condicionado a Strava, la recarga del historial al tocar su pestaña, la conservación del estado entre pestañas, la elección de idioma y el respaldo en inglés, la pantalla siempre encendida, el modo inmersivo al entrar y salir, y la regla de Strava configurado.

---

## 6. Diferencias entre docs y código (manda el código)

| ID | Doc dice | Código hace |
|---|---|---|
| D-01 | `README.md` ("Screen always-on during workouts") y `CLAUDE.md` ("Screen always-on during workouts via wakelock_plus") | La pantalla queda encendida **siempre**, en toda la app, desde el arranque y sin liberarse nunca (`lib/main.dart:42`). |
| D-02 | `CLAUDE.md`: "navigation via `go_router`" | `go_router` está declarado en `pubspec.yaml:27` pero **no se usa** en ningún lado. La navegación es la barra de pestañas más pantallas apiladas. |
| D-03 | El tema define colores de barra inferior (`#12243A`, `#00B4D8`, `#607D8B`) | La app usa otro tipo de barra, que **no lee esos colores**. La barra visible usa los colores derivados del azul semilla (`lib/shared/theme.dart:24-28` contra `lib/main.dart:144`). |
| D-04 | `CLAUDE.md` y `README.md`: "`shared/` — Theme and reusable widgets" | `metric_card.dart` e `interval_tile.dart` **no se usan** en ninguna pantalla. |
| D-05 | `CLAUDE.md` ("`minSdkVersion` 21") y `README.md` ("Android (API 21+)") | `build.gradle.kts:28` usa el mínimo por defecto del kit de Flutter, sin fijarlo. El 21 solo figura en la configuración de íconos (`pubspec.yaml:56`). |
| D-06 | La app se presenta como bilingüe es/en | Hay muchos textos visibles fuera del catálogo y fijos en español (o en inglés, como "Up next: " y "Unknown device"). Ver F-APP-11. |
| D-07 | El catálogo tiene `workoutFinishTitle`, `workoutFinishContent`, `workoutPause`, `workoutResume` | El inmersivo, que es lo que se ve, usa sus equivalentes hardcodeados en español. Esas claves solo las usa código muerto. `workoutStartTitle`, `historyStatWatts` y `historyStatCalories` no se usan. |
| D-08 | `CLAUDE.md`, "Platform Notes": iOS declara `NSBluetoothAlwaysUsageDescription` | También está `NSBluetoothPeripheralUsageDescription`, con el mismo texto. Los dos textos están solo en español y no mencionan el pulsómetro. |
| D-09 | `CONTRIBUTING.md`: "We follow `flutter_lints` rules (automatically checked in CI)" | Ningún CI corre análisis ni tests. Los dos CI solo compilan iOS. |
| D-10 | El proyecto tiene imágenes de arranque con logo para iOS (`LaunchImageDark*.png`, `LaunchBackground`) | La pantalla de arranque de iOS es **blanca**: el storyboard usa fondo blanco e imagen transparente de 1×1, y las imágenes con logo no están referenciadas. |
| D-11 | Nombre de producto "RowMate" | En Windows el ejecutable y el título de ventana son "rower_app", la empresa es "com.example" y el ícono es el de Flutter. En Android el identificador es `com.example.rower_app`. |
| D-12 | `assets/icon/icon.png` | El archivo es un JPEG con extensión `.png`. |
| D-13 | `CLAUDE.md`: "features/ One folder per screen (device, workout, routines, history)" | También existe `features/profile/` (pestaña Perfil, condicionada a Strava). |
| D-14 | `README.md`: "macOS — Works out of the box, no extra configuration needed" | macOS no registra el esquema `rowmate://` para el regreso del login de Strava. Hay que verificar si el login funciona (P-08). |
| D-15 | `CLAUDE.md`, "Data Flow" y pestañas: nada sobre recargas | Tocar la pestaña Historial siempre recarga desde la base, con parpadeo de carga, y volver del detalle de sesión también recarga. |
| D-16 | `CLAUDE.md`: "HeartRateService … `autoConnect()`, fired from the eager `Provider` in `main.dart`" | Coincide, pero un fallo de esa reconexión solo va al log (`[HRM] autoConnect: …`) y el usuario no lo ve. |
| D-17 | Codemagic y GitHub Actions como equivalentes para compilar iOS | Solo GitHub genera el archivo de credenciales de Strava. Codemagic no lo genera y, si el archivo no existe, la compilación falla. |
| D-18 | `main.dart` prepara la base de datos para Linux | No hay carpeta de plataforma Linux, así que Linux no es plataforma del producto. |

---

## 7. Código muerto o no alcanzable (sin requisito de paridad)

- `lib/shared/widgets/metric_card.dart` y `lib/shared/widgets/interval_tile.dart` (F-APP-08).
- `lib/features/workout/workout_screen.dart:203-990`: la pantalla de entrenamiento anterior y sus partes, a las que ya no se navega (`CLAUDE.md`). Incluye textos hardcodeados y el uso de 7 claves `.arb` (F-APP-10).
- `StepType.label` (`lib/core/models/interval_step.dart:3-15`): nombres de tipo en español sin uso (F-RUT-01).
- El respaldo `?? '…'` de `workoutCompleted` y `workoutCompletedMessage`, que nunca se alcanza.
- Colores de barra inferior del tema (D-03).
- La dependencia `go_router` (D-02).

---

## 8. Preguntas abiertas (por defecto se hace lo que hace el legacy)

| ID | Pregunta | Por defecto (paridad) |
|---|---|---|
| P-01 | ¿La pantalla siempre encendida sigue siendo global o se limita a Dispositivo conectado y entrenamiento? ¿Hay que volver a pedirla al regresar del segundo plano? | Global, desde el arranque, sin liberar. Verificar en cada plataforma que no se pierda al volver del segundo plano. |
| P-02 | ¿Se localizan los textos hardcodeados (F-APP-11) agregando la versión en inglés? | Paridad estricta = texto fijo. **Recomiendo aprobar el cambio**: español idéntico al legacy y una traducción al inglés nueva, sin tocar números ni formatos. |
| P-03 | ¿Se unifica la voz del español en voseo ("Completaste tu rutina", "Podés sincronizar de nuevo")? ¿Se corrigen "Iniciar entrenamiento?" (sin ¿) y los plurales ("1 sesiones")? | Textos exactos del legacy. |
| P-04 | ¿La pestaña Workout se llama "Workout" también en español? | Sí: `navWorkout` = "Workout". |
| P-05 | Identidad: ¿se unifican los identificadores (Android `com.example.rower_app`, iOS y macOS `com.iguisoft.rowmate`)? Cambiar el de Android instala una app nueva al lado de la vieja, sin sus datos. | Hay que decidirlo con el usuario. ¿Hay que **migrar datos** (rutinas, historial, pulsómetro recordado, sesión de Strava) desde la app Flutter instalada a la app Unity? |
| P-06 | ¿Windows muestra "RowMate" como título y nombre, con el ícono de la app, en lugar de "rower_app" y el ícono de Flutter? | Recomiendo "RowMate", como corrección de identidad. Paridad literal = "rower_app". |
| P-07 | ¿Strava funciona en el APK release de Android sin el permiso de internet en el manifiesto principal? | Requisito de producto: Strava tiene que funcionar en release en todas las plataformas. |
| P-08 | ¿El login de Strava funciona en macOS (sin esquema registrado) y en Windows? | Verificar en el legacy. En Unity, Strava tiene que funcionar en las 4 plataformas. |
| P-09 | ¿Cómo obtiene Codemagic las credenciales de Strava? | Fuera del producto Unity. Lo resuelve el Architect para el CI nuevo. |
| P-10 | ¿Se traducen los textos de permiso del sistema (iOS y macOS) y se menciona el pulsómetro? | Texto actual, solo en español. Recomiendo localizar. |
| P-11 | ¿Qué hace "Activar Bluetooth" en iOS, macOS y Windows, donde una app no puede encender el Bluetooth? | Paridad: el botón existe en todas las plataformas. Recomiendo reemplazarlo fuera de Android por "Abrir Ajustes" o por instrucciones. |
| P-12 | ¿Se mantiene el parpadeo de carga al tocar Historial y la falta de refresco automático al terminar un entrenamiento? | Recarga en cada toque de la pestaña. Mejora propuesta: refresco sin parpadeo y también al terminar una sesión. |
| P-13 | ¿Se agrega un selector de idioma dentro de la app? | No: sigue al sistema, con respaldo en inglés. |
| P-14 | ¿Los formatos (decimal con punto, miles con punto solo en los paneles de series, fecha `dd/MM/yyyy  HH:mm`, 24 h) pasan a depender del idioma? | Formatos fijos del legacy. |
| P-15 | iOS: ¿la pantalla de arranque pasa a ser oscura con el logo, como en Android? | Recomiendo sí (corrección de defecto). Paridad literal = blanca. |
| P-16 | ¿El diálogo de permiso de Bluetooth de iOS aparece al abrir la app la primera vez (antes de tocar "Buscar")? | Verificar en un dispositivo. Si se confirma, en Unity se puede adelantar con la pantalla explicativa de F-APP-13 (mejora EXR). |
| P-17 | En escritorio, ¿se recuerdan el tamaño y la posición de la ventana y se ofrece pantalla completa en el inmersivo? | No (paridad). Es una mejora a aprobar. |
| P-18 | ¿Cuál es el título real de la ventana en macOS? (El archivo de interfaz dice "APP_NAME".) | Verificar. En Unity: "RowMate". |

---

## Cambios por QA (plan)

Correcciones por el gate de armonía del plan ([QA_PLAN_REVIEW.md](../../QA_PLAN_REVIEW.md)), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-P-18 | Requisitos mínimos: el mínimo real de Android del legacy es API 24 (Android 7.0, el valor por defecto de Flutter 3.47.6). La app nueva pide Android 8.0 (API 26) y ARM64 (Q-90 de FEATURE_INVENTORY) |

## Cambios al cerrar la It. 4b

Pendiente del PO de la [spec de la It. 4b §15](../../iterations/04b-hud-exr/01-po-spec.md), releído en el legacy (`3ae0f3a`, `lib/features/workout/immersive_workout_screen.dart:27-41`), 2026-10-30.

| Dónde | Qué cambió |
|---|---|
| F-APP-15, punto 1 | Las barras se ocultan al crearse la página del inmersivo (al empezar su entrada) y vuelven al destruirse (al terminar su salida), no "al entrar" y "al cerrarse" en general (AC-APP-15-01, -04 de la It. 4b) |
