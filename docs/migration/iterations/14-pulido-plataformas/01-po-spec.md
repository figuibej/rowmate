# It. 14 — Pulido EXR, identidad, plataformas y CI (spec de producto)

| Campo | Valor |
|---|---|
| Iteración | 14 (`14-pulido-plataformas`), [ROADMAP §It. 14](../../ROADMAP.md). **La última del plan** |
| Incluye | F-APP-11, F-APP-15, F-APP-16, F-APP-19 ([app-shell-platform](../../product/inventory/app-shell-platform.md)) |
| También toca | F-APP-03 (lectores de pantalla: la parte NO VERIFICABLE de AC-APP-03-06, DT-05 de la It. 3), F-APP-05 (tipografía, transiciones), F-APP-10 (4 claves nuevas: 252 → 256 por idioma), F-APP-12 (cifras), F-WKT-13 (indicador de carga de la escena), F-HIS-12/13 (gráfico), F-BLE-05/08/09 (Dispositivo) |
| Decisiones aplicadas | Q-01 (auditoría), Q-02, Q-03, Q-04, Q-85 (definitivos), Q-86, Q-88 (parcial, DV-129), Q-90 (verificación), P-10 (DV-138), R-12 y R-14 |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura). Leídos: `android/app/src/main/AndroidManifest.xml`, `android/app/src/main/res/values-v31/styles.xml`, `ios/Runner/Info.plist:25-60`, `macos/Runner/Base.lproj/MainMenu.xib:335`, `macos/Runner/Configs/AppInfo.xcconfig`, `windows/runner/main.cpp:20-40`, `pubspec.yaml:4`, `.github/workflows/build-ios.yml`, `codemagic.yaml` (vía el inventario). **No** se leyó `lib/core/strava/strava_secrets.dart` |
| Unity verificado | 6000.3.27f1 instalado: `UnityEngine.Accessibility` (`AssistiveSupport`, `AccessibilityHierarchy`, `AccessibilityNode` con `label`, `hint`, `value`, `role`, `state`, `notificationDispatcher.SendAnnouncement`, `SendScreenChanged`, `screenReaderStatusOverride`). El player de Windows del editor instalado trae UI Automation (`UiaHostProviderFromHwnd`, `UiaRaiseNotificationEvent`): **Narrador es alcanzable**. TalkBack: soportado por el motor desde 2023.2. Solo está el módulo de Windows (sin Android ni Apple) |
| Modo | Entrega rápida, sin debates. Ante una duda, el legacy o un valor por defecto, anotado en una línea (§8) |
| Criterios | **44** (§3): 34 de producto + 10 de deuda (issues, §6). 27 capturas (§7) |

---

## 0. Cómo leer esta spec

- Tamaños (**TV** 411 × 914, **TH** 914 × 411, **L640** 640 × 360, **P360** 360 × 800, **W1280** 1280 × 720, en dp) y estado de prueba (reloj de prueba, `ROWMATE_DATA_DIR` temporal, nada en `prefs.json` ni en las bases del usuario): los de la [spec de la 6a §0](../06a-escena-lago/01-po-spec.md). Textos entre comillas invertidas: **exactos al carácter**.
- Marcas: `[dotnet]` Core y herramientas con `dotnet test`; `[Editor]` EditMode/PlayMode, capturas (§7) y revisión estática de `ProjectSettings`; `[Device-Win]` build de desarrollo con `ROWMATE_DATA_DIR` temporal (QA) y build normal (demo del usuario); `[Device-Android]` **NO VERIFICABLE** sin "Android Build Support" y sin teléfono (QA solo revisa la configuración y el Kotlin con `native/android/check.ps1`); `[Apple]` **NO VERIFICABLE**, a la 10b; `[CI]` workflows **validados localmente** (estructura, sintaxis, versiones fijas); la corrida real queda para cuando el usuario cargue los secretos.
- Ninguna prueba toca la radio Bluetooth, el registro de Windows ni los datos reales del usuario. Si un build necesita Bluetooth, `-BleSmoke` tal como está.
- IDs: `AC-APP-nn-mm` siguen a los existentes (APP-03 hasta 11, APP-15 hasta 04, APP-16 hasta 05, APP-19 hasta 05). `AC-POL-nn` = pulido transversal, `AC-NV-01` = arrastre de lo no verificable, `AC-DEBT-nn` = issues (§6). `DV-128` a `DV-139` siguen a DV-127.

---

## 1. Objetivo y demo final de la migración

**Objetivo.** Que RowMate se vea y se comporte como un producto terminado: nombre, ícono propio y arranque oscuro en Windows y Android (y listo para iOS/macOS), ventana de escritorio cómoda (mínimo, redimensionado, pantalla completa a pedido), rotación fluida, ningún texto fuera del catálogo es/en, lector de pantalla del sistema que lee la app, tipografía tabular coherente, háptica sutil en Android, transiciones sin cortes y un CI que compila Windows y Android sin credenciales versionadas.

**Qué cambia a la vista** (frente a la It. 12):

1. Ícono RowMate en el ejecutable, la barra de tareas, la barra de título y el lanzador de Android (hoy el de Unity por defecto).
2. Al abrir: fondo `#0D1B2A` con el logo y un fundido al shell; nunca un cuadro blanco ni el logo de Unity.
3. Windows: la ventana no se achica por debajo de 640 × 360; `F11` o `Alt+Enter` pasan a pantalla completa y vuelven.
4. Al rotar o redimensionar, el HUD entra en su *layout* nuevo sin saltos y nada se pierde (texto del editor, desplazamiento, sesión).
5. El primer entrenamiento muestra `Preparando escena…` en lugar de una escena trabada (#21).
6. Cifras tabulares también en el Historial (fechas, duraciones, ejes) y ejes del gráfico sin etiquetas encimadas (#32, #37).
7. Arreglos chicos de Dispositivo y del aviso de exportación (#40, #41, #42, #44, #69) y del Historial a 1,3× (#36).
8. Con Narrador encendido, la app se lee entera: pestañas, botones, títulos, métricas, avisos y el cambio de paso.

**Demo del usuario en Windows** (build normal, W1280; 25 minutos; con el remo o con el build `simulator`):

1. Abrir `RowMate.exe`: arranque oscuro con el logo; ícono en la barra de tareas. Achicar la ventana al mínimo (se frena en 640 × 360) y agrandarla; `F11` → pantalla completa, `F11` → vuelve al tamaño anterior.
2. **Dispositivo**: buscar, conectar (o el remo simulado), ver las métricas en vivo.
3. **Rutinas**: abrir `Escalera` en el editor, volver. **Workout**: iniciar `Escalera` → primer entrenamiento con `Preparando escena…` → escena → remar 1 minuto → `Terminar`.
4. Repetir un entrenamiento libre corto en cada escenario: `Lago alpino`, `Río urbano`, `Costa`, `Canal de regata`.
5. **Historial** → la sesión → gráfico (cambiar de métrica) → `Exportar TCX` → `Abrir carpeta`.
6. Cambiar Windows a inglés (Configuración → Hora e idioma → Idioma de visualización) o usar el atajo de idioma que ya documenta la It. 2, y repetir 2 a 5 sin ver un solo texto en español fuera del simulador y del diagnóstico.
7. Encender Narrador (`Win+Ctrl+Enter`), recorrer con `Tab` y las flechas: pestañas, botones, tarjetas de métricas, lista de rutinas, Historial y Detalle; durante un entrenamiento, escuchar el cambio de paso; al exportar, escuchar el aviso. Apagar Narrador.

**Demo en Android** (cuando haya módulo y teléfono): lo mismo en vertical y horizontal, con TalkBack y la háptica (§3, AC-POL-03). **Apple**: en la 10b.

**Lo que el workflow deja verificado:** catálogo, auditoría de textos, árbol de accesibilidad (forzando el lector encendido), capturas, `ProjectSettings`, builds de Windows (normal, `simulator`, humos) y YAML del CI. Android, Apple, el Narrador real y el DPI con dos monitores quedan para la demo del usuario (§3, §9).

---

## 2. Historias de usuario

- Como remero quiero reconocer RowMate por su nombre e ícono en mi PC y mi teléfono, y que arranque prolijo, sin destellos blancos.
- Como remero en la PC quiero achicar, agrandar o poner a pantalla completa la ventana sin que se rompa nada.
- Como remero quiero rotar el teléfono en pleno entrenamiento sin perder nada y sin cortes feos.
- Como remero que usa la app en inglés quiero no ver nunca un texto en español (y al revés).
- Como remero con baja visión quiero que el lector de pantalla del sistema me lea la app.
- Como mantenedor quiero que el CI compile Windows y Android en cada cambio sin versionar credenciales, y saber qué secretos cargar.

---

## 3. Criterios de aceptación

### Identidad (F-APP-16, Q-85, Q-86, Q-90)

- **AC-APP-16-06 · Nombre y versión.** *Entonces* en Windows: ejecutable `RowMate.exe`, título de ventana `RowMate`, producto `RowMate`, empresa `iguisoft`, versión de archivo y de producto `1.0.0`; en Android: etiqueta `RowMate`, `versionName` `1.0.0`, `versionCode` `1`; en iOS/macOS: `CFBundleName`/`CFBundleDisplayName` `RowMate`, versión `1.0.0` (1). Legacy: `pubspec.yaml:4` (`1.0.0+1`), `AndroidManifest.xml` (`android:label="RowMate"`), `AppInfo.xcconfig` (`PRODUCT_NAME = RowMate`); en Windows el legacy decía `rower_app` (DC-63, se corrige). [Editor] (ProjectSettings) · [Device-Win] · [Device-Android] · [Apple]
- **AC-APP-16-07 · Identificadores definitivos (Q-85, DV-131).** *Entonces* Android `com.iguisoft.rowmate`; iOS y macOS `com.iguisoft.rowmate.unity` (conviven con el legacy, que usa `com.iguisoft.rowmate`, R-14); Windows sin identificador del motor, datos en `AppData/LocalLow/iguisoft/RowMate` (AC-APP-16-02 de la It. 2, sin cambios) y sin MSIX (DV-132). Ningún `com.example` en ninguna plataforma. [Editor] · [Apple]
- **AC-APP-16-08 · Ícono propio (DV-133).** *Entonces* existe una **fuente vectorial o procedural propia** versionada (sin assets de terceros, sin el JPEG del legacy, licencia del repo) y un comando reproducible que genera todos los tamaños con los mismos bytes en dos corridas. Idea del legacy redibujada (`assets/icon/icon.png`: remos cruzados en cian sobre azul pizarra muy oscuro): fondo `#0D1B2A`, figura en `accent.primary`. Salidas: maestro 1024; Windows 16, 24, 32, 48, 64, 128, 256 (el `.exe` y la ventana lo muestran en la barra de tareas, la de título y Alt+Tab, **no** el de Unity); iOS 1024 **opaco, sin canal alfa**; macOS 16 a 1024. A **16 px** la silueta se distingue (sin texto ni detalles de menos de 2 px a ese tamaño; CAP-01). [dotnet] (generador) · [Editor] · [Device-Win] · [Apple]
- **AC-APP-16-09 · Ícono adaptativo de Android.** *Entonces* capa de fondo `#0D1B2A` lisa y capa de figura con todo el dibujo dentro de la **zona segura de 66 dp sobre 108 dp**; también las versiones redonda y heredada (Android 8+). Con máscara círculo, *squircle* y cuadrado redondeado la figura no se corta (CAP-02). [Editor] · [Device-Android]
- **AC-APP-16-10 · Arranque en Windows (Q-86).** *Dado* el build normal, *cuando* se abre, *entonces* desde el primer cuadro que dibuja la app el fondo es `#0D1B2A` con el logo centrado (ancho 96 dp) hasta que el shell está listo, y el paso al shell es un fundido de **200 ms** (`ease.exit`; con reducir movimiento, 120 ms lineal). Nunca se ve un cuadro blanco, gris claro ni el logo o el texto "Made with Unity". No se agrega espera: el tiempo hasta el primer cuadro del shell no crece más de **100 ms** frente al build de la It. 12, medido en el build de desarrollo (mediana de 5 arranques). El legacy no tenía arranque en Windows (`main.cpp`): es el agregado de Q-86. [Device-Win] · CAP-03, CAP-04
- **AC-APP-16-11 · Arranque en Android e iOS.** *Entonces* Android 12+: pantalla de arranque del sistema con fondo **`#0D1B2A`** y el ícono RowMate (legacy `values-v31/styles.xml`: `windowSplashScreenBackground` `#0D1B2A`); Android 8-11: fondo `#0D1B2A` con el logo centrado (legacy `launch_background.xml`); igual en modo claro y oscuro; después, el mismo paso de AC-APP-16-10, sin cuadro blanco ni negro de más de 1 cuadro. iOS: arranque **oscuro** `#0D1B2A` con el logo (el legacy lo tenía blanco, D-10, Q-86). [Editor] (configuración) · [Device-Android] · [Apple]
- **AC-APP-16-12 · Android mínimo (Q-90).** *Entonces* `ProjectSettings` declara API mínima **26** y solo **ARM64**; en el APK, `minSdkVersion` 26 y solo `lib/arm64-v8a`; los permisos del APK son exactamente los del legacy (`AndroidManifest.xml:4-19`: `BLUETOOTH_SCAN` con `neverForLocation`, `BLUETOOTH_CONNECT`, `BLUETOOTH`/`BLUETOOTH_ADMIN`/`ACCESS_FINE_LOCATION` hasta API 30, `WAKE_LOCK`, `bluetooth_le` requerido) más los que agrega el motor (`INTERNET`, QA-G10a-06); sin `VIBRATE` (D10-11). [Editor] · [Device-Android]

### Ventana y orientación (F-APP-15, Q-88)

- **AC-APP-15-05 · Ventana inicial en Windows (DV-130).** *Entonces* cada arranque abre **en ventana** (nunca en pantalla completa, aunque se haya cerrado así), de **1280 × 720** lógicos escalados por el DPI del monitor (legacy `main.cpp:28-29`), centrada en el área de trabajo del monitor principal y recortada a esa área si no entra; redimensionable y con el botón maximizar; cerrar la ventana cierra la app (`SetQuitOnClose(true)`). Ni el tamaño ni la posición se recuerdan (Q-88, paridad). [Device-Win]
- **AC-APP-15-06 · Tamaño mínimo (DV-128).** *Entonces* el área de contenido no baja de **640 × 360** lógicos (L640, el menor *layout* horizontal verificado desde la It. 2; legacy sin mínimo). Al arrastrar el borde, la app se reacomoda en vivo, sin desbordes ni textos cortados en Dispositivo, Rutinas, editor, Workout (con el HUD), Historial y Detalle, y sin perder el estado. [Editor] (L640, P800, W1280) · [Device-Win]
- **AC-APP-15-07 · Pantalla completa a pedido (DV-129).** *Dado* Windows, *cuando* se aprieta `F11` o `Alt+Enter`, *entonces* la ventana pasa a pantalla completa sin bordes en el monitor donde está, y con la misma tecla vuelve al tamaño y la posición de antes. `Esc` no la cambia (sigue siendo "Atrás" / cerrar diálogo). Entrar o salir del inmersivo no cambia la ventana (AC-APP-15-02 sigue vigente). No se recuerda entre arranques (AC-APP-15-05). [Device-Win]
- **AC-APP-15-08 · DPI y dos monitores.** *Dado* dos monitores con escalas distintas (p. ej. 100 % y 150 %), *cuando* se arrastra la ventana de uno a otro, *entonces* en menos de 1 s la interfaz se redibuja a la escala nueva, nítida (sin texto borroso por estirado) y sin romper el *layout*; el tamaño lógico se conserva. [Device-Win] (demo del usuario si QA tiene un solo monitor; QA no cambia la escala del sistema)
- **AC-APP-15-09 · Orientaciones (DV-135).** *Entonces* Android: vertical, horizontal izquierda y derecha y vertical invertida, siguiendo el sensor **y respetando el bloqueo de rotación del sistema**; rotar no reinicia la app (legacy `configChanges`, `AndroidManifest.xml`); admite pantalla dividida. iPhone: vertical y las dos horizontales, **sin** vertical invertida; iPad: las cuatro y multitarea (legacy `Info.plist:31-43`). macOS: ventana inicial **800 × 600** (`MainMenu.xib:335`), redimensionable, mínimo 640 × 360. [Editor] (configuración) · [Device-Android] · [Apple]
- **AC-APP-15-10 · Rotación y redimensionado fluidos (EXR).** *Dado* cualquier pantalla, *cuando* rota el teléfono o la ventana cruza de clase (TV ↔ TH, W1280 ↔ L640), *entonces* la escena se adapta en el primer cuadro sin reiniciar distancia ni tiempo y el HUD entra en **200 ms** (`ease.enter`) en el *layout* nuevo; dentro de la misma clase, en el acto (ANIMATION_BIBLE §2.12, DT6a-06). Se conservan: la sesión y su reloj, el desplazamiento de las listas (elemento de arriba a la vista), el texto y el foco del editor, un diálogo o panel abierto y la pestaña. Nunca se ve un cuadro con el *layout* viejo estirado. Con reducir movimiento, en el acto. [Editor] (cambio de tamaño en PlayMode) · [Device-Android]

### Textos (F-APP-11, Q-01 a Q-04)

- **AC-APP-11-01 · Auditoría automática.** *Dado* cada pantalla y estado de los conjuntos de capturas `it2` a `it14`, en `es` y en `en`, *entonces* todo texto visible es: (a) un mensaje del catálogo (con sus parámetros), (b) un número, hora, fecha o duración con los formatos de F-APP-12, (c) un dato del usuario (nombre de rutina, de remo o pulsómetro, nombre de archivo o carpeta), (d) una marca o sigla fija (`RowMate`, `TCX`, `FTMS`), o (e) un texto de las excepciones Q-03 (simulador: `Remo simulado`, `Pulsómetro simulado`, `SIMULADOR`, panel del simulador, `Hora escena`, `Hora real`) o Q-04 (diagnóstico BLE). Cualquier otro texto hace fallar la prueba nombrando la pantalla, el estado, el idioma y el texto. [Editor]
- **AC-APP-11-02 · Los fijos del legacy, localizados.** *Dado* `en`, *entonces* ninguno de los textos de la tabla de F-APP-11 que alcanza una pantalla viva se ve en español (p. ej. `Terminar entrenamiento`, `Entrenamiento Libre`, `EN MARCHA`, `Parciales 500 m`, `1 series · 0 descansos`, `Rower`, `×{N}` con su clave); *dado* `es`, *entonces* no aparecen `Up next:` ni `Unknown device` (DC-14). El texto en español es idéntico al legacy salvo Q-02. Excepción conocida: el panel del simulador queda en español también en inglés (Q-03, cierra esa parte de #28 sin cambios). [Editor]
- **AC-APP-11-03 · Catálogo.** *Entonces* `es` y `en` tienen las **mismas 256 claves** (252 + las 4 de §4), con los mismos parámetros y plural ICU donde hay conteo; un idioma distinto de `es` cae en `en`. [dotnet]

### Lectores de pantalla (F-APP-03, DT-05; DV-136)

- **AC-APP-03-12 · Se lee la app.** *Dado* el lector del sistema encendido (Narrador en Windows; TalkBack en Android; VoiceOver en la 10b), *entonces* al recorrer con el lector se alcanzan, en orden visual (arriba-abajo, izquierda-derecha), las pestañas, los títulos, los botones, las tarjetas de métricas, las filas de las listas, los campos del editor, los diálogos y los avisos de Dispositivo, Workout, Rutinas, editor, Historial, Detalle y el HUD. Cada uno dice su **nombre** (el texto del catálogo que se ve; los botones de solo ícono, su ayuda: `Atrás`, `Mostrar menú`, `Eliminar serie`, `Exportar TCX`, AC-APP-03-06), su **rol** (botón, pestaña, encabezado, texto, campo, interruptor) y su **estado** (elegida, deshabilitada, desplegado). Las métricas se leen como `{nombre}: {valor}` (`a11yLabelValue`) con la unidad. La escena 3D y lo decorativo no se leen. [Editor] (árbol con el lector forzado encendido) · [Device-Win] · [Device-Android] · [Apple]
- **AC-APP-03-13 · Se maneja con el lector.** *Entonces* la acción del lector (Narrador + `Enter`; doble toque en TalkBack) hace lo mismo que el toque o el clic; en un diálogo el recorrido queda dentro del diálogo y, al cerrarlo, vuelve al elemento que lo abrió; las listas se desplazan con los gestos o teclas del lector. [Editor] · [Device-Win] · [Device-Android]
- **AC-APP-03-14 · Anuncios.** *Entonces* al cambiar de pestaña o de pantalla apilada se anuncia el título nuevo; cada aviso (exportado, reconectado, errores) se anuncia **una vez**; en el entrenamiento se anuncia el cambio de paso (`a11yStepProgress`: `Paso 2 de 5: Trabajo`) y el diálogo de rutina completada; las métricas del HUD **no** se anuncian solas cada segundo (se leen al llegar a ellas). El gráfico del Detalle se lee como un resumen (`a11yChartSummary`: `Watts: de 98 a 212`). [Editor] · [Device-Win]
- **AC-APP-03-15 · Sin costo con el lector apagado.** *Dado* el lector apagado, *entonces* no se arma el árbol de accesibilidad, el HUD y Dispositivo siguen en **0 B por cuadro** como en la It. 12 y el HUD no suma elementos visuales (tope 60, hoy 56-57). *Dado* el lector encendido, *entonces* el HUD sigue a 60 fps en W1280 en la PC. [Editor] · [Device-Win]

### Pulido transversal (EXR)

- **AC-POL-01 · Transiciones entre pantallas.** *Entonces* cambio de pestaña, *push*/*pop* (editor, Detalle), diálogos, avisos, Dispositivo búsqueda ↔ conectado y entrada/salida del inmersivo siguen los tiempos de ANIMATION_BIBLE §2.1, §2.2, §2.13 y §2.11, con su variante de reducir movimiento; ninguna pantalla aparece por primera vez con un cuadro sin estilos, en blanco o con el contenido saltando de lugar. [Editor] (muestreo de cuadros, CAP-24)
- **AC-POL-02 · Tipografía coherente.** *Entonces* ningún texto visible usa la fuente por defecto del motor; el texto de interfaz es Inter y **todo número** (Dispositivo, HUD, duraciones y objetivos de Rutinas y del editor, tarjetas del Historial con fecha, hora, duración y distancia, filas y ejes del Detalle, cursor del gráfico) usa cifras tabulares (RowMate Numeric SC o `TabularNumber`, ART_BIBLE §4.2): al cambiar los dígitos, el ancho del número no cambia y lo de al lado no se mueve. [Editor]
- **AC-POL-03 · Háptica sutil en Android.** *Entonces* Android da respuesta táctil **leve** al pausar y al reanudar, **media** al cambiar de paso y de **éxito** al completar la rutina (ANIMATION_BIBLE §2.12), además de la de `Listo` (It. 10a) y la de reconexión (AC-DEBT-10); respeta el ajuste "respuesta táctil" del sistema y no pide permisos (D10-11). En Windows, nada. [Editor] (puerto falso que registra los pedidos) · [Device-Android]
- **AC-POL-04 · Diálogo de completado a 1,3×.** *Dado* L640 con letra 1,3×, *entonces* el diálogo de rutina completada queda centrado y quieto, sin desplazarse ni cortarse (O-01 de la 4b). [Editor] · CAP-13
- **AC-POL-05 · Recorrido completo.** *Dado* el simulador, *entonces* una prueba PlayMode hace el recorrido de la demo (§1, pasos 2 a 5) en `es` y en `en` con el destino de exportación falso, con los 4 escenarios, sin errores ni excepciones en el registro; y todas las pruebas existentes siguen en verde (`dotnet` y Unity, ninguna cuenta baja frente a la It. 12). [dotnet] · [Editor]
- **AC-POL-06 · Rendimiento en la PC sin regresión.** *Entonces* en el build de desarrollo, W1280, nivel Alto: 0 B por cuadro en el HUD con escena y 60 fps sostenidos 2 min en cada escenario, con las mismas sondas de las It. 6a-8. [Device-Win]
- **AC-POL-07 · ProjectSettings bajo control.** *Entonces* el diff de `ProjectSettings` solo trae lo que pide esta spec (íconos, arranque, orientaciones, ventana, versión, identificadores), documentado en el diseño; el define del simulador nunca queda guardado; `HorizonFar.mat` y `HorizonNear.mat` no cambian (#55); la guardia `ProjectSettingsGuard` pasa. [Editor]

### CI (F-APP-19)

- **AC-APP-19-06 · Builds en el CI.** *Dado* el secreto de licencia cargado, *cuando* hay un push a `main` o se lanza a mano, *entonces* el CI compila la variante **normal** de Windows 64 bits y de Android (APK, ARM64), y los publica como artefactos `RowMate-Windows` (la carpeta del build comprimida) y `RowMate-Android` (`.apk`), con **14 días** de retención. El build normal pasa la inspección de rastros del simulador (AC-BLE-13-02). [CI]
- **AC-APP-19-07 · Sin secretos, no se rompe.** *Dado* que faltan los secretos de Unity, *entonces* los jobs de Unity (tests y builds) **se saltean** con un aviso que nombra el secreto que falta y remite a `tools/unity/README.md`, el workflow queda en verde y `ci-core` corre igual en cada push. [CI]
- **AC-APP-19-08 · Job de Apple, opcional.** *Entonces* existe un job **solo manual** (una opción al lanzarlo) en un *runner* de macOS que genera el proyecto de Xcode de iOS y lo compila **sin firmar**, y compila la app de macOS sin firmar; artefactos `RowMate-iOS-unsigned` y `RowMate-macOS-unsigned`. Sin licencia se saltea como en AC-APP-19-07. Documentado como "listo para cuando haya licencia" (la firma queda para la 10b). [CI] · [Apple]
- **AC-APP-19-09 · Sin credenciales versionadas.** *Entonces* ningún workflow ni archivo versionado trae licencias, contraseñas, llaves de firma ni rutas de la PC; los secretos entran solo por `secrets.*`. Android se firma para tienda solo si están los secretos opcionales de firma; si no, con la llave de depuración (como el legacy, `build.gradle.kts:33-38`) y el artefacto lo dice en su nombre (`-debugsigned`). La higiene de AC-APP-19-03 sigue pasando. [dotnet] · [CI]
- **AC-APP-19-10 · YAML validado localmente.** *Entonces* cada `.github/workflows/*.yml` se lee sin errores con un parser YAML; todo `uses:` tiene versión fija (etiqueta de versión; nunca `@main` ni `@master`); `permissions: contents: read` salvo que un job necesite más; cada job tiene `timeout-minutes`; Unity fijado en `6000.3.27f1`. Lo comprueba una prueba o un script que corre en la PC y en `ci-core`. [dotnet] · [CI]
- **AC-APP-19-11 · Documentado.** *Entonces* `tools/unity/README.md` tiene una sección "It. 14: CI" con la tabla de secretos (nombre, para qué, obligatorio u opcional, cómo obtenerlo), cómo lanzar los jobs y qué artefactos salen, y los pasos para Android y Apple (§9); sin rutas de la PC ni datos del usuario. Cierra el pendiente de documentación del issue #10 (que se cierra cuando el usuario cargue los secretos). [CI]

### Lo no verificable acumulado

- **AC-NV-01 · Arrastre explícito.** *Entonces* el `REPORT.md` de la 14 lista, uno por uno, como **NO VERIFICABLE** con su issue: [Device-Android] R-12 (60 fps 30 min, nivel Medio), AC-SCN-01-09, -01-16 y -01-19 (Android), R4b-02 (200 píldoras), AC-APP-13-01 (permisos con `aapt2`), -13-02, -14-05, AC-HRM-HW-02, AC-STR-09-24, AC-APP-16-04/-12, TalkBack, háptica, splash y rotación de esta spec (issues #7, #8, #46, #68); [BLE-HW] AC-BLE-06-08, -08-07, -09-03, -91-02, AC-HRM-HW-01; [Apple] todo ROADMAP §4.1 más los [Apple] de esta spec (a la 10b); demos del usuario P-17 (#5) y `powercfg` (AC-APP-14-01 a -03). Ninguno se da por cumplido sin medir. [Device-Android] · [Apple]

### Deuda elegida (issues, §6)

- **AC-DEBT-01 · #40 Nombre del remo en la reconexión.** *Dado* TV con letra 1,0× y 1,3× y un remo de hasta 24 caracteres (`AMS-670B RowMaster 2024`), *entonces* `Reconectando con {name}…` muestra el nombre **completo** en hasta 2 líneas, y `Desconectar` no se achica (si no entra al lado, baja debajo). Solo los nombres de más de 24 caracteres se cortan con `…` (DT10-14). [Editor] · CAP-15
- **AC-DEBT-02 · #41 Botón buscando.** *Dado* una búsqueda en curso, *entonces* el botón `Buscando...` muestra el indicador **blanco de 16 dp** (legacy `device_screen.dart:398-402`) y la etiqueta blanca al **70 %**, con contraste ≥ **4,5:1** sobre el fondo del botón; el estilo deshabilitado genérico no cambia. [Editor] · CAP-16
- **AC-DEBT-03 · #42 Dispositivo en horizontal de teléfono.** *Dado* TH (914 × 411) buscando, *entonces* la lista de resultados y la tarjeta del pulsómetro comparten **un solo desplazamiento**, sin alto fijo de la lista, y el botón `Buscar` del pulsómetro se alcanza desplazando (no queda tapado por la barra de pestañas). [Editor] · CAP-17
- **AC-DEBT-04 · #44 Error sin la operación repetida.** *Dado* una falla de búsqueda o de conexión, *entonces* la caja dice `Error al escanear: {detalle}` / `Scan error: {detail}` (y `Error al conectar: …`) con solo el detalle del sistema, **sin** el prefijo técnico (`scan:`, `connect:`): p. ej. `Error al escanear: error 2`, no `Error al escanear: scan: error 2`. [dotnet] · [Editor] · CAP-18
- **AC-DEBT-05 · #69 Aviso de exportación.** *Entonces* el aviso con acción usa fondo `glass.fallback` (`#070D16 @86 %`), sin dejar ver el gráfico detrás (contraste del texto ≥ 4,5:1 en el peor caso); en TV la ruta `Documentos\RowMate\TCX` puede partirse después de `\` y no deja un renglón con una sola letra; el reloj del aviso **se detiene** mientras el puntero o el foco están encima y sigue al salir (A12-04). [Editor] · CAP-19, CAP-20
- **AC-DEBT-06 · #36 Historial a 1,3× y TV.** *Entonces* en P360 a 1,3× (CAP-03 de la 9) cada columna de la tarjeta se achica **entera** (valor y etiqueta juntos) con un aire mínimo de **4 dp** entre columnas; en TV (CAP-07 de la 9) el gráfico del Detalle se ve con el `0` del eje Y y el eje X, desplazándose como en CAP-08; en W1280 con el simulador, la llave no tapa la primera etiqueta X y la cinta `SIMULADOR` no tapa la flecha atrás ni el título. [Editor] · CAP-21, CAP-22
- **AC-DEBT-07 · #32 Ejes sin etiquetas encimadas (DV-137).** *Entonces* una marca intermedia cuya etiqueta quedaría a menos de **4 dp** de la etiqueta de un borde no se dibuja (X e Y; p. ej. nunca `3:44` pegado a `5:00`, ni `25` con `26`); con un split constante (rango < 10 s), el eje Y del split se abre a **±5 s** del valor. El suavizado de la curva queda abierto en #32. [dotnet] · [Editor] · CAP-23
- **AC-DEBT-08 · #37 Arte del Historial.** *Entonces* la línea de la serie tiene extremos redondos, la tira de tipo de paso radio **1,5 dp** (arte de la 9 §4 y §7.1), el error del Historial muestra un ícono de **56 dp al 12 %** como el vacío (CAP-17 de la 9), y fecha, hora y ejes usan cifras tabulares (AC-POL-02). La sombra del *tooltip* queda abierta; el sello de Strava se descarta con Strava. [Editor] · CAP-12, CAP-25
- **AC-DEBT-09 · #21 Carga de la escena en el primer entrenamiento.** *Dado* el primer entrenamiento desde que se abrió la app con la escena sin preparar, *entonces* el HUD, el reloj y la sesión arrancan en el toque como siempre (nada toca el dato) y sobre el fondo de la escena se ve `workoutSceneLoading` (`Preparando escena…` / `Preparing scene…`) con el indicador de 32 dp (gracia de 120 ms, fundido de 160, ART_BIBLE §3.10); con la escena lista, el indicador se va y la entrada de la cámara hacia el remero se ve **completa** de principio a fin. Las entradas siguientes siguen inmediatas, sin indicador. [Editor] · [Device-Win] · CAP-26
- **AC-DEBT-10 · #51 Háptica al reconectar.** *Entonces* en Android `Reconectado con {name}` da la misma respuesta táctil corta que `Listo` (It. 10a), una vez por reconexión. El sonido queda abierto (no hay sistema de audio de interfaz; BKL-03). [Editor] · [Device-Android]

---

## 4. Textos nuevos (F-APP-10: 252 → 256 por idioma)

| Clave | es | en | Uso |
|---|---|---|---|
| `workoutSceneLoading` | `Preparando escena…` | `Preparing scene…` | AC-DEBT-09 |
| `a11yLabelValue` | `{label}: {value}` | `{label}: {value}` | AC-APP-03-12 (solo lector) |
| `a11yStepProgress` | `Paso {current} de {total}: {type}` | `Step {current} of {total}: {type}` | AC-APP-03-14 (solo lector) |
| `a11yChartSummary` | `{metric}: de {min} a {max}` | `{metric}: from {min} to {max}` | AC-APP-03-14 (solo lector) |

`{type}` y `{metric}` usan las claves existentes (tipos de paso, métricas del gráfico); `{value}`, `{min}` y `{max}`, los formatos de F-APP-12 con la unidad. Si el Architect encuentra un control de solo ícono sin nombre, reusa la clave de su ayuda; si no hay, la agrega en el diseño con su par es/en (y sube el total).

---

## 5. Checklist de paridad

| Comportamiento legacy | Fuente | AC |
|---|---|---|
| Nombre `RowMate` (Android, iOS, macOS) | `AndroidManifest.xml` `android:label`; `Info.plist:7-16`; `AppInfo.xcconfig:8` | AC-APP-16-06 |
| Versión 1.0.0 (1) | `pubspec.yaml:4` | AC-APP-16-06 |
| Ids iOS/macOS `com.iguisoft.rowmate` (aquí `.unity`, R-14) | `AppInfo.xcconfig:11`; `project.pbxproj:485` | AC-APP-16-07 |
| Ícono: remos cruzados cian sobre azul oscuro | `assets/icon/icon.png`; `pubspec.yaml:49-57` | AC-APP-16-08, -09 |
| Arranque `#0D1B2A` con logo en Android (12+ y anterior) | `values-v31/styles.xml`; `drawable*/launch_background.xml` | AC-APP-16-11 |
| Permisos Bluetooth, ubicación ≤ 30 y `WAKE_LOCK` | `AndroidManifest.xml:4-19` | AC-APP-16-12 |
| Ventana 1280 × 720, redimensionable, cerrar = salir | `windows/runner/main.cpp:27-33` | AC-APP-15-05 |
| Orientación libre en Android, sin reinicio; iPhone 3, iPad 4 | `AndroidManifest.xml` `configChanges`; `Info.plist:31-43` | AC-APP-15-09 |
| macOS 800 × 600 | `MainMenu.xib:335` | AC-APP-15-09 |
| Textos fijos de la tabla F-APP-11 | `immersive_workout_screen.dart`, `series_panels.dart`, `routine.dart:100-105`, `history_screen.dart:113-115`, `device_screen.dart:98,452` | AC-APP-11-01, -02 |
| Flutter expone textos, botones y ayudas a los lectores | `tooltip.dart:392-394` (AC-APP-03-06) | AC-APP-03-12 a -15 |
| CI de iOS sin firmar (artefacto descargable) | `.github/workflows/build-ios.yml`; `codemagic.yaml` | AC-APP-19-08 |
| Android firmado con la llave de depuración | `build.gradle.kts:33-38` | AC-APP-19-09 |

Correcciones al legacy (no paridad): Windows `rower_app` y `com.example` (DC-63), arranque blanco de iOS (D-10), ícono de Flutter en Windows, CI sin tests (D-09).

---

## 6. Issues elegidos (deuda)

Criterio: menores, visuales o de pulido, que caen en las pantallas que esta pasada ya recorre. El resto queda abierto.

| Issue | Título | AC | Alcance |
|---|---|---|---|
| #40 | Reconnect banner truncates the rower name | AC-DEBT-01 | Completo |
| #41 | Searching button: white spinner and readable label | AC-DEBT-02 | Completo |
| #42 | Device in phone landscape: cramped list and cut card | AC-DEBT-03 | Completo |
| #44 | Scan/connect error banner repeats the operation | AC-DEBT-04 | Completo |
| #69 | Aviso de exportación: contraste, título y pausa | AC-DEBT-05 | Completo |
| #36 | Historial: layout a 1,3×, CAP-07 en TV y llave | AC-DEBT-06 | Completo |
| #32 | Historial: etiquetas de ejes encimadas | AC-DEBT-07 | Parcial: el suavizado queda abierto |
| #37 | Historial: pulido de arte | AC-DEBT-08 | Parcial: la sombra del *tooltip* queda abierta |
| #21 | Primer entrenamiento sin indicador de carga | AC-DEBT-09 | Completo (el tirón de #1 sigue en la 6b) |
| #51 | Link feedback: sound and haptics | AC-DEBT-10 | Parcial: el sonido queda abierto |

También se cierran con esta iteración, sin AC propio: #28 en su punto "panel del simulador en inglés" (queda así por Q-03; AC-APP-11-02) y la parte de documentación de #10 (AC-APP-19-11).

---

## 7. Capturas (set `it14`)

**Reglas:** las de la 6a §7.1 (reloj de prueba, `ROWMATE_DATA_DIR` temporal, dos corridas iguales byte a byte, sin rutas de la PC), en `docs/migration/iterations/14-pulido-plataformas/snapshots/` **y no en otra carpeta**; `capture-snapshots.ps1 -Set it14`. Base `F-9` de la It. 9, destino de exportación falso, simulador donde se lo nombra.

| ID | Tamaño | Idioma | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** | — | — | Ícono: maestro 1024 y 256, 64, 48, 32, 24, 16 sobre fondo claro y oscuro; a 16 px la silueta se distingue |
| **CAP-02** | — | — | Adaptativo de Android: figura y fondo, con máscaras círculo, *squircle* y cuadrado redondeado y la zona segura marcada |
| **CAP-03** | W1280 | — | Primer cuadro del arranque: `#0D1B2A` y logo centrado |
| **CAP-04** | TV | — | Igual en vertical |
| **CAP-05** | L640 (ventana mínima) | es | Dispositivo conectado, sin desbordes |
| **CAP-06** | L640 (ventana mínima) | en | Workout con HUD y escena `Lago alpino`, sin desbordes |
| **CAP-07** | TV | es | Dispositivo conectado con las métricas tabulares |
| **CAP-08** | TH | en | Rutinas: lista con resúmenes en inglés |
| **CAP-09** | TV | en | Editor de `Escalera` con duraciones y objetivos tabulares |
| **CAP-10** | TV | es | HUD con `Río urbano` y la barra de pasos |
| **CAP-11** | TH | en | HUD con `Costa`, `Up next:` |
| **CAP-12** | TV | en | Historial: tarjetas con fecha, duración y distancia tabulares |
| **CAP-13** | L640, 1,3× | es | Diálogo de rutina completada centrado (AC-POL-04) |
| **CAP-14** | TH | es | HUD con `Canal de regata` |
| **CAP-15** | TV, 1,3× | es | `Reconectando con AMS-670B RowMaster 2024…` completo (#40) |
| **CAP-16** | TV | es | `Buscando...` con indicador blanco (#41) |
| **CAP-17** | TH | es | Dispositivo buscando: lista y pulsómetro con un solo desplazamiento (#42) |
| **CAP-18** | W1280 | es | Caja `Error al escanear: error 2` (#44) |
| **CAP-19** | TV | es | Aviso `Guardado en Documentos\RowMate\TCX` sobre el gráfico, opaco (#69) |
| **CAP-20** | W1280 | en | Aviso `Saved to Documents\RowMate\TCX` con el puntero encima |
| **CAP-21** | P360, 1,3× | en | Historial: columnas achicadas enteras, 4 dp de aire (#36) |
| **CAP-22** | TV | es | Detalle: gráfico con el `0` del eje Y y el eje X a la vista (#36) |
| **CAP-23** | W1280 | es | Detalle con ejes sin etiquetas encimadas, extremos redondos (#32, #37) |
| **CAP-24** | W1280 | es | Cuadro a mitad del *push* del editor (AC-POL-01): sin cuadro vacío |
| **CAP-25** | TV | en | Error del Historial con su ícono de 56 dp (#37) |
| **CAP-26** | TV | es | Primer entrenamiento: `Preparando escena…` con el HUD funcionando (#21) |
| **CAP-27** | W1280 | es | Árbol de accesibilidad del Detalle volcado como texto junto a la captura (nombres y roles; AC-APP-03-12) |

Fuera del set (las toma QA o el usuario en el build): ícono en la barra de tareas y en Alt+Tab, pantalla completa con `F11`, Narrador leyendo.

---

## 8. Decisiones (valen por defecto; van al gate)

| ID | Decisión | Por defecto | Si se rechaza |
|---|---|---|---|
| DV-128 | Ventana mínima 640 × 360 en Windows y macOS | Se acepta (el legacy no tiene mínimo) | Sin mínimo, como el legacy |
| DV-129 | Pantalla completa con `F11` o `Alt+Enter`, sin recordarla; adelanta una parte de BKL-18 | Se acepta | Sin pantalla completa (Q-88 tal cual) |
| DV-130 | Ventana inicial centrada | Se acepta (el legacy la pone en 10, 10, valor de la plantilla) | En (10, 10) |
| DV-131 | Q-85: Android `com.iguisoft.rowmate`; iOS/macOS `com.iguisoft.rowmate.unity` (conviven con el legacy) | Se acepta | iOS/macOS `com.iguisoft.rowmate`: reemplaza al legacy y su historial queda inaccesible (en la 10b) |
| DV-132 | Windows sin MSIX: se distribuye la carpeta del build comprimida | Se acepta | MSIX con `com.iguisoft.rowmate` (issue; necesita certificado) |
| DV-133 | Ícono propio redibujado con la idea del legacy | Se acepta | Usar el `icon.png` del legacy (es JPEG de 640 px: no da 1024 ni adaptativo) |
| DV-134 | Arranque con logo fijo y fundido de 200 ms, sin animación de marca | Se acepta | Animación de remos que se cruzan (backlog) |
| DV-135 | Android: las 4 orientaciones respetando el bloqueo del sistema | Se acepta (el legacy deja decidir al sistema) | Sin vertical invertida en teléfonos |
| DV-136 | Alcance del lector: textos, botones, estados, avisos, cambio de paso y resumen del gráfico; la escena no; las métricas sin anuncio continuo | Se acepta | Anunciar métricas cada N s (issue) |
| DV-137 | #32: ocultar la marca que choca con un borde y rango mínimo de 10 s en el split | Se acepta (cambio frente a fl_chart, AC-HIS-13-05) | Paridad con las etiquetas encimadas |
| DV-138 | P-10: se queda en Unity 6000.3.27f1 (instalar otro editor está fuera de las reglas del workflow) | Se acepta | Evaluar 6.x LTS más nuevo con el usuario (issue) |
| DV-139 | Android en el CI firmado con la llave de depuración si faltan los secretos de firma | Se acepta (como el legacy) | No publicar el APK sin firma de tienda |

---

## 9. Qué tiene que hacer el usuario

**CI (cierra #10).** GitHub → el repositorio → Settings → Secrets and variables → Actions → New repository secret:

| Secreto | Obligatorio | Para qué | Cómo se obtiene |
|---|---|---|---|
| `UNITY_LICENSE` | Sí, para todo job de Unity | Licencia Personal de Unity | Contenido del archivo de licencia `Unity_lic.ulf` que Unity Hub deja en la PC al activar la licencia Personal (la ubicación exacta la documenta `tools/unity/README.md`) |
| `UNITY_EMAIL` | Sí | Cuenta de Unity | El correo de la cuenta |
| `UNITY_PASSWORD` | Sí | Cuenta de Unity | La contraseña de la cuenta |
| `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS` | No | Firmar el APK para tienda | Un *keystore* propio, que **nunca** se versiona |

Después: Actions → `ci-unity` → Run workflow (o un push a `main`). El job de Apple se lanza a mano con su opción.

**Android.** Unity Hub → 6000.3.27f1 → Add modules → **Android Build Support** (con OpenJDK y Android SDK & NDK Tools); un teléfono de gama media (clase Galaxy A54 / Pixel 6a, Android 8+, 64 bits) con depuración USB; `tools/build/build.ps1 -Target Android -Backend IL2CPP` e instalar el APK. Recorrer AC-NV-01 (lista Android), con TalkBack y la háptica.

**Apple (10b).** Una Mac con Xcode, Unity 6000.3.27f1 con iOS y Mac Build Support, un iPhone y una cuenta de Apple (gratis para probar en el propio equipo; paga para TestFlight). El job manual de macOS sirve para compilar sin firmar antes de tener la Mac.

**Demo de Windows.** §1, con Narrador y, si tiene, dos monitores con escalas distintas (AC-APP-15-08).

---

## 10. Fuera de alcance

- Backlog post-paridad (BKL-01 a BKL-26), incluidos recordar tamaño y posición de la ventana (resto de BKL-18) y sonido de interfaz (BKL-03).
- La 6b (agua avanzada, issue #6) y el tirón de entrada de #1.
- Código nativo de Apple y firma (10b); MSIX y publicación en tiendas.
- Cambiar de versión de Unity (DV-138).
- Íconos temáticos (monocromo) de Android 13.
- Strava (descartado el 2026-11-09).

---

## 11. Para issues

- MSIX de Windows con `com.iguisoft.rowmate` y certificado (DV-132).
- Recordar tamaño, posición y pantalla completa de la ventana (resto de BKL-18).
- Ícono temático monocromo de Android 13 y animación de marca en el arranque (DV-134).
- Gráfico del Detalle navegable punto a punto con el lector (DV-136).
- Builds firmados para TestFlight y prueba interna de Google Play (F-APP-19, mejora EXR).
- Evaluar la próxima LTS de Unity 6 con el usuario (DV-138, P-10).
- Restos de #32 (suavizado monótono), #37 (sombra del *tooltip*) y #51 (sonido) siguen en sus issues.
- #57 (Dispositivo en L640 y pantallas anchas), #66 (íconos de exportar en la fuente, necesita re-recortar la fuente de íconos) y #28 (hover de la miniatura) quedan abiertos para después de la paridad.

---

## 12. Al cerrar la iteración (PO, 2026-11-10)

Aceptada sobre la ronda 2 de [05-qa-verification](05-qa-verification.md): **APROBADO CON OBSERVACIONES**, 38 PASS, 0 FAIL, 6 NO VERIFICABLE (AC-APP-15-08, AC-POL-06, AC-APP-19-06, -07, -08 y AC-NV-01, que se resuelve en el reporte).

| ID | Decisión del PO |
|---|---|
| DV-V14-03 (QA-V14-16) | AC-APP-16-10 se lee "**desde el primer código de la app**": lo que queda (≤ 1 cuadro blanco, ≤ 0,1 s, al aparecer la ventana) es de Windows, antes de que corra nada nuestro. Sacarlo pide un *launcher* nativo: a issue |
| QA-V14-11 (#69) | El aviso con acción **se queda con el vidrio** `glass.fallback` (`#070D16 @86 %`), como la biblia de arte §3.10; se borra "sin dejar ver el gráfico detrás" de AC-DEBT-05. Lo que manda es el contraste ≥ 4,5:1 en el peor caso, y se cumple (≈ 14,7:1). La variante opaca va a issue, a elección del usuario |
| QA-V14-14 (AC-POL-06) | Queda NO VERIFICABLE: la pantalla de QA estaba en el modo de reserva (GPU ≈ 80 ms en todo, incluso en la entrada). CPU y memoria iguales a la It. 8. Se repite con el monitor normal |
| QA-V14-15 | El test intermitente de la It. 11 no bloquea el cierre, pero se arregla **antes** de cargar `UNITY_LICENSE` (si no, `ci-unity` quedaría rojo sin motivo) |
| QA-V14-08 a -13, -17 | A issues, agrupados |
| Issues | Se cierran #40, #41, #42, #44, #69, #36 y #21. Quedan abiertos con su resto: #32 (suavizado), #37 (sombra del *tooltip*), #51 (sonido), #10 (se cierra cuando el usuario cargue los secretos) y #28 (*hover* de la miniatura) |
