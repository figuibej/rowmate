# It. 2 — Shell, Dispositivo en vivo y simulador: spec de producto

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-27 |
| Iteración | 2 · `02-dispositivo-simulador` ([ROADMAP §2, It. 2](../../ROADMAP.md)) |
| Estado | Corregida por el gate de armonía de QA ([03-qa-harmony](03-qa-harmony.md): QA-G2-04, -09, -10 y -18; ver §16). Sigue: WP-A / WP-B / WP-D según [02-tech-design](02-tech-design.md) y [04-design-notes](04-design-notes.md) |
| IDs dueños | **16 = 14 canónicos + 2 alias**: F-APP-01, F-APP-02, F-APP-05, F-APP-06, F-APP-08 (+ F-RUT-19, descarte), F-BLE-07, F-BLE-11, F-BLE-13 (+ F-APP-18), F-BLE-15, F-BLE-16, F-HRM-04, F-HRM-12, F-HRM-13, F-HRM-15 |
| También toca | F-BLE-05 (botón "Buscar dispositivos" y estado vacío), F-BLE-08 (caída simulada de 3 s), F-BLE-10 (Dispositivo como primera pestaña y bloqueo cruzado), F-HRM-02/03/08/10 (con el pulsómetro simulado), F-SCN-23 (control "Hora escena"), F-APP-16 (nombre `RowMate`, identificadores y Android mínimo en equipos), F-APP-19 (`ci-unity.yml`, opcional) |
| Criterios | **125** = **113 nuevos** (APP 30: F-APP-01 8, F-APP-02 8, F-APP-05 9, F-APP-06 2, F-APP-08 2, F-APP-19 1; BLE 48: F-BLE-05 3, F-BLE-07 4, F-BLE-08 1, F-BLE-10 2, F-BLE-11 11, F-BLE-13 5, F-BLE-15 10, F-BLE-16 12; HRM 35: F-HRM-04 5, F-HRM-12 6, F-HRM-13 12, F-HRM-15 12) + **12 de la It. 1** que se verifican en pantalla o en equipo (§5). Además: **15 casos legacy** (LT-20 a LT-34, §6) y **25 capturas y 3 videos** (§7) |
| Contrato | `rowmate-legacy` (solo lectura, commit `3ae0f3a`). Cada valor se leyó en `lib/` y `test/`; los textos, en `lib/l10n/app_es.arb` y `app_en.arb`; las capturas de referencia, en `docs/image-01.png` (Dispositivo) y `docs/image-02.png` (Workout) |
| Insumos | [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md); inventarios [rower-ble-device-sim](../../product/inventory/rower-ble-device-sim.md), [heart-rate](../../product/inventory/heart-rate.md) y [app-shell-platform](../../product/inventory/app-shell-platform.md); [REPORT de la It. 1](../01-fundaciones/REPORT.md) §5 y §7; [01-po-spec de la It. 1](../01-fundaciones/01-po-spec.md); [04-design-notes](../01-fundaciones/04-design-notes.md) §5 y §7; [ART_BIBLE](../../design/ART_BIBLE.md) §2-§5 y §10; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §1-§2; [ASSET_SLOTS](../../design/ASSET_SLOTS.md) §3-§4; [ADR 003](../../architecture/adr/003-composition-root-y-simulador.md); [STATUS](../../STATUS.md) |

---

## 0. Cómo leer esta spec

### 0.1 Identificadores

- **`AC-<ÁREA>-<NN>-<nn>`**: criterio `nn` de la ficha `F-<ÁREA>-<NN>`. Los alias se verifican con su canónico: F-APP-18 con F-BLE-13 y F-RUT-19 con F-APP-08.
- Los IDs de "también toca" (F-BLE-05, -08, -10, F-APP-19) numeran sus criterios desde acá; la iteración dueña sigue la numeración (como hizo la It. 1 con F-APP-16).
- **§5** repite IDs de la It. 1 (AC-BLE-01-05, AC-APP-09-04 a -06, AC-APP-16-01 a -04, AC-BASE-01, -05, -08 y -10): son los mismos criterios, con lo que falta verificar en pantalla o en equipo.
- **`LT-nn`**: casos de test legacy portados 1 a 1. La It. 1 usó LT-01 a LT-19; esta iteración, **LT-20 a LT-34** (§6).
- **`CAP-nn`** y **`VID-nn`**: capturas y videos que QA entrega (§7).

### 0.2 Cómo se verifica cada criterio

| Marca | Qué es | Disponible |
|---|---|---|
| **[dotnet]** | `dotnet test` del Core puro, fuera de Unity, con reloj y azar de prueba | Sí |
| **[Editor-EM]** | Unity Test Framework, EditMode, en el Editor 6000.3.27f1 | Sí |
| **[Editor-PM]** | Unity Test Framework, PlayMode: la UI real montada, con el tamaño de ventana, el idioma y los servicios que pide el criterio | Sí |
| **[Captura]** | Imagen renderizada por Unity (no una maqueta), con el tamaño y el estado de §7, revisada por QA y el Designer | Sí |
| **[Device-Win]** | Build de Windows instalado y abierto en la PC | Sí con Mono. IL2CPP, solo si el usuario agregó el módulo |
| **[Device-And]** | Build de Android en un teléfono 8.0+ de 64 bits | Solo si el usuario agregó el módulo de Android y tiene el teléfono |
| **[QA]** | Revisión estática de QA (configuración, build, código) | Sí |
| **[CI]** | Corrida en GitHub | Solo con repositorio remoto y licencia de Unity cargada |

Si un paso necesita el módulo de Windows IL2CPP o el de Android y no está instalado, o no hay teléfono, ese criterio queda **NO VERIFICABLE** con el motivo, y no frena la iteración. Nunca se da por cumplido sin verlo.

### 0.3 Notación

- Textos entre comillas invertidas: **exactos al carácter**. `·` es U+00B7 (punto medio) con un espacio a cada lado; `...` son tres puntos (no el carácter de elipsis); `⏎` es un salto de línea sin espacios alrededor; "…" en prosa es la elipsis que pone la pantalla al cortar un texto.
- Tamaños en **dp** (punto independiente de la densidad: en Android, 1 dp = 1 px × densidad; en Windows, 1 dp = 1 px con la escala de pantalla al 100 %, como el legacy). Cada captura indica dp y px.
- **"Sin desbordes"** quiere decir: ningún elemento se dibuja fuera de su tarjeta ni de la ventana, ningún texto queda cortado sin "…", y lo que no entra se alcanza desplazando.
- Números que ve el usuario, con punto (`24.0`); en la prosa, con coma (24,5 spm).

### 0.4 Estado de prueba

Para que tests y capturas den siempre lo mismo:

- **Simulador sin ruido**: el modo de prueba de la It. 1 (AC-BLE-14-06), con reloj de prueba. A 150 W y 24 spm, cada segundo suma 3,770 m (split exacto 132,62 s).
- **Remo de prueba**: un remo falso, solo para tests, cuyo estado fija el test (desconectado, buscando, conectando, conectado) y que manda las lecturas que el test pide (incluido un pulso por FTMS). Es el equivalente de `_ControllableBleService` de `test/heart_rate_card_test.dart:25-48`.
- **Pulsómetro de prueba**: un pulsómetro falso, solo para tests, con estado, "reintentando", sensor recordado (`Polar H10 7E1A2B3C`), nombre elegido y errores (incompatible o con texto técnico) a pedido. Es el equivalente de `_ThrowingHrm` de `test/heart_rate_provider_test.dart:7-50`.
- **Idioma, escala de letra y "reducir movimiento" de prueba**: el test le da a la app la lista de idiomas del sistema, la escala de letra y el ajuste de movimiento, sin tocar el equipo.
- Ninguno de estos dobles existe en un build que se instala.

---

## 1. Objetivo y demo

**Objetivo.** La primera app usable: abre en Dispositivo con el remo simulado conectado y sus métricas en vivo, la tarjeta del pulsómetro funciona de punta a punta con el sensor simulado, y el panel del simulador controla intensidad, ritmo, hora y fallas desde cualquier pestaña. Quedan fijados el sistema visual (tema oscuro, paleta por métrica, tipografía de tablero) y la barra de pestañas, idénticos en comportamiento al legacy.

**Demo (se le muestra al usuario antes de la It. 3).** Con el modo simulador, en el Editor y en un build de desarrollo de Windows (y en Android, si hay módulo y teléfono):

1. La app abre en **Dispositivo**: `Remo simulado` + `Conectado`, y la grilla `SPLIT 500m` / `SPM` / `VATIOS` / `DISTANCIA` cambia cada segundo (2 columnas en un teléfono vertical y en una ventana de 640 dp; 3 en la ventana de Windows de 1280 × 720 y en un teléfono horizontal).
2. En la tarjeta del pulsómetro: `Buscar` → aparece `Pulsómetro simulado` → tocarlo → `--` → al segundo, el pulso en vivo con el corazón latiendo a ese ritmo → `Desconectar` → `Sin sensor`.
3. Desde el panel (botón de la llave): `Suave`, `Medio` y `Fuerte` cambian los números al segundo siguiente; ± vatios y ritmo con sus topes; `Remando` apagado deja `--:--`, `0.0`, `0`; `Hora escena` ±1 h y `Hora real`; `Simular desconexión` muestra 3 s la vista de búsqueda y vuelve, con la distancia siguiendo desde donde iba; `Simular caída del pulsómetro` muestra `Pulsómetro simulado · reconectando...` 3 s y vuelve.
4. La cinta `SIMULADOR` siempre visible. La barra con **Dispositivo / Workout / Rutinas / Historial**: las tres últimas con contenido provisorio, y todas conservan su estado al ir y volver.
5. Con el sistema en inglés, todo en inglés salvo el simulador (Q-03); en Android, el cambio de idioma con la app abierta se aplica sin reiniciar.
6. El **build normal** de Windows: sin cinta ni panel, la vista de búsqueda vacía, el título de ventana `RowMate`, los datos en `AppData\LocalLow\iguisoft\RowMate`, y la app legacy sigue instalada e intacta.
7. Las pruebas: `dotnet test` en verde con los 15 casos legacy nuevos y todo lo de la It. 1; las mismas pruebas en el Test Runner de Unity; las PlayMode de la UI en verde; y la carpeta de capturas de §7.

Con las fuentes y los íconos descargados (ASSET_SLOTS §3-§4), la tipografía y los íconos son los definitivos; sin ellos, la demo se ve "a medio vestir" (ROADMAP §0).

---

## 2. Alcance

| ID | Funcionalidad | Qué se cierra en la It. 2 | Vuelve en |
|---|---|---|---|
| F-APP-01 | Arranque de la app | Pestaña inicial, servicios por variante, pestañas vivas, nombre, arranque sin destellos, cierre limpio | 3 (rutinas al arrancar), 9 (historial al arrancar), 11 (reconexión real del pulsómetro) |
| F-APP-02 | Barra de pestañas | 4 destinos (5 con Strava, con configuración de prueba), selección, estado conservado, "Atrás" en Android, rotación | 9 (recarga de Historial), 12 (Perfil real) |
| F-APP-05 | Tema oscuro y estilos base | Colores base, tipografía, cifras fijas, escala de letra, texto terciario, foco de teclado, reducir movimiento | 14 (auditoría) |
| F-APP-06 | Paleta por métrica | Colores en pantalla (Dispositivo y pulsómetro) | 4a (HUD), 9 (historial) |
| F-APP-08 (+ F-RUT-19) | `MetricCard` e `IntervalTile` | **Descarte**: no se migran | — |
| F-BLE-07 | Desconexión manual | Botón, sin confirmación, no reconecta | 10a (remo real) |
| F-BLE-11 | Vista conectada | Cabecera, grilla 2/3 columnas, tarjetas, valores, ceros al conectar | 10a (remo real, nombre `Rower`) |
| F-BLE-13 (+ F-APP-18) | Modo simulador | Activación solo en desarrollo, aislamiento, build normal sin simulador | 3 (base separada, F-HIS-20), 12 (bloqueo de Strava, F-STR-18) |
| F-BLE-15 | Remo simulado | Completo | — |
| F-BLE-16 | Panel del simulador | Completo (la hora no tiene efecto visible hasta la 6a) | 6a (F-SCN-23) |
| F-HRM-04 | Pulso en vivo | Publicación, `--`, 0 al cortar, regla de contacto | 11 (sensor real) |
| F-HRM-12 | Estado de presentación | Completo | 11 |
| F-HRM-13 | Tarjeta del pulsómetro | Todos los estados (con el simulado y con el pulsómetro de prueba) | 10a (exclusión con el remo real), 11 |
| F-HRM-15 | Pulsómetro simulado | Completo | — |

---

## 3. Historias de usuario

| ID | Historia |
|---|---|
| H-01 | Como **remero**, quiero abrir la app y ver mi remo conectado con split, ritmo, vatios y distancia en vivo, para confirmar que todo anda antes de entrenar. |
| H-02 | Como **remero con pulsómetro**, quiero buscar mi sensor, conectarlo y ver el pulso en vivo en la misma pantalla, y soltarlo cuando quiero. |
| H-03 | Como **remero**, quiero saber qué pasa cuando el sensor se cae y vuelve, sin tocar nada. |
| H-04 | Como **usuario**, quiero moverme entre pestañas sin perder lo que estaba haciendo en cada una. |
| H-05 | Como **usuario**, quiero la app en mi idioma (español o inglés) sin configurar nada, y que cambie si cambio el idioma del teléfono. |
| H-06 | Como **remero que mira el teléfono desde el remo**, quiero números grandes, de ancho fijo y con el color de cada métrica, para leerlos de un vistazo. |
| H-07 | Como **usuario en horizontal o en una ventana chica**, quiero que nada quede cortado ni tapado. |
| H-08 | Como **desarrollador o tester**, quiero un remo y un pulsómetro simulados que se comporten como los reales, y un panel para cambiar intensidad, ritmo y hora y para provocar caídas, desde cualquier pantalla. |
| H-09 | Como **usuario de la app vieja**, quiero instalar la nueva en la PC y en el teléfono sin que pise la vieja ni sus datos, y que se llame RowMate. |
| H-10 | Como **equipo**, queremos que las pruebas del Core corran también dentro de Unity y que cada pantalla tenga pruebas y capturas, para que nada se rompa sin que se note. |

---

## 4. Criterios de aceptación

### 4.0 Conteo

| Ficha | AC | Ficha | AC | Ficha | AC |
|---|---|---|---|---|---|
| F-APP-01 | 8 | F-BLE-05 | 3 | F-HRM-04 | 5 |
| F-APP-02 | 8 | F-BLE-07 | 4 | F-HRM-12 | 6 |
| F-APP-05 | 9 | F-BLE-08 | 1 | F-HRM-13 | 12 |
| F-APP-06 | 2 | F-BLE-10 | 2 | F-HRM-15 | 12 |
| F-APP-08 | 2 | F-BLE-11 | 11 | | |
| F-APP-19 | 1 | F-BLE-13 | 5 | | |
| | | F-BLE-15 | 10 | | |
| | | F-BLE-16 | 12 | | |
| **APP** | **30** | **BLE** | **48** | **HRM** | **35** |

Total: **113 criterios nuevos** (§4.1-§4.15) + **12 de la It. 1** (§5) = **125 criterios** que se verifican en la It. 2.

### 4.1 F-APP-01 — Arranque de la app

- **AC-APP-01-01 · Pestaña inicial.** *Dado* cualquier build, *cuando* se abre la app, *entonces* se ve el shell con la pestaña **Dispositivo** (la primera, índice 0) elegida. *Dado* que el usuario cerró la app con otra pestaña elegida (por ejemplo, Historial), *cuando* la vuelve a abrir, *entonces* arranca otra vez en Dispositivo: la pestaña no se guarda. "Cerró" quiere decir que la app terminó (ventana cerrada en Windows; en Android, quitada de las recientes o con "Atrás" en Android 11 o anterior); volver de segundo plano no es abrirla (AC-APP-02-06). [Editor-PM] · [Device-Win] · `lib/main.dart:124`
- **AC-APP-01-02 · Servicios según la variante.** *Entonces* el build normal usa los servicios de remo y de pulsómetro "de verdad" (en la It. 2 todavía sin Bluetooth: AC-BLE-13-05) y el build de simulador usa **a la vez** el remo simulado y el pulsómetro simulado, que comparten el mismo atleta: el pulso del sensor simulado sale de la física del remo simulado (AC-HRM-15-09). No existe "remo simulado + pulsómetro real" ni al revés (Q-40). [dotnet] · [QA] · `main.dart:51-54`
- **AC-APP-01-03 · El remo no se conecta solo, salvo el simulado.** *Dado* el build normal, *cuando* abre, *entonces* el remo arranca desconectado y Dispositivo muestra la vista de búsqueda (el legacy no recuerda el remo). *Dado* el build de simulador, *cuando* abre, *entonces* sin tocar nada Dispositivo muestra `Remo simulado` + `Conectado` (AC-BLE-15-02). [Editor-PM] · [Device-Win] · `main.dart:51`; `core/bluetooth/ble_service.dart:43`; `simulated_ble_service.dart:17-21`
- **AC-APP-01-04 · El pulsómetro arranca activo.** *Entonces* el pulsómetro empieza a trabajar al abrir la app, aunque Dispositivo no esté en pantalla, e intenta reconectar al sensor recordado (la cadencia real de 6 intentos es de la It. 11, F-HRM-07). Un fallo de ese intento no se muestra: solo queda en el registro de diagnóstico como `[HRM] autoConnect: <error>`. En el simulador el intento no hace nada y la tarjeta abre en `Sin sensor`. [dotnet] · [Editor-PM] · `main.dart:68-79`; `simulated_heart_rate_service.dart:89-91`
- **AC-APP-01-05 · Pestañas vivas desde el arranque.** *Entonces* las pestañas se arman una sola vez al abrir la app y siguen vivas aunque no se vean. *Dado* el remo simulado conectado, *cuando* el usuario pasa 10 s en Rutinas y vuelve a Dispositivo, *entonces* la grilla muestra la lectura de ese segundo (no la de hace 10 s) y la distancia siguió sumando (unos 38 m más a 150 W). [Editor-PM] · `main.dart:126-143`
- **AC-APP-01-06 · Se presenta como RowMate, sin marcas de depuración.** *Entonces* el nombre de la app ante el sistema es `RowMate` (AC-APP-16-01) y ninguna pantalla muestra una cinta o marca de depuración (el legacy la apaga: `debugShowCheckedModeBanner: false`). En el build de simulador la única marca propia es la cinta `SIMULADOR` (AC-BLE-16-01). Sobre la marca "Development Build" del motor, ver P-01 (§14). [Device-Win] · [Captura] · `main.dart:94-95`
- **AC-APP-01-07 · Arranque sin destellos.** *Cuando* se abre la app, *entonces* después de la pantalla de arranque del sistema aparece directamente el shell: no se ve el logo del motor ni un cuadro blanco intermedio, y el fondo es `#0D1B2A` desde el primer cuadro que dibuja la app. La pantalla de arranque con marca es de la It. 14 (F-APP-16, Q-86). [Device-Win] · [Device-And] · `main.dart:93-107`; `android/app/src/main/res/drawable/launch_background.xml`
- **AC-APP-01-08 · Cierre limpio.** *Cuando* la app se cierra, *entonces* el remo y el pulsómetro (simulados o reales) se detienen sin avisarle nada al usuario y no queda ningún temporizador vivo: ni lecturas, ni caída simulada, ni búsqueda (los LT-20 a LT-25 y LT-32 lo comprueban). [dotnet] · `main.dart:60-79`

### 4.2 F-APP-02 — Barra de pestañas

- **AC-APP-02-01 · Destinos, orden, textos e íconos.** *Dado* un build sin Strava (todos los de la It. 2), *entonces* la barra inferior tiene **exactamente 4** destinos, en este orden. [Editor-PM] · [Captura] CAP-20 · `main.dart:150-170`

  | Índice | Etiqueta (clave · es / en) | Ícono sin elegir | Ícono elegido |
  |---|---|---|---|
  | 0 | `navDevice` · `Dispositivo` / `Device` | `bluetooth` | `bluetooth_connected` |
  | 1 | `navWorkout` · `Workout` / `Workout` (Q-08) | `timer` sin relleno | `timer` relleno |
  | 2 | `navRoutines` · `Rutinas` / `Routines` | `fitness_center` | `fitness_center` (el mismo) |
  | 3 | `navHistory` · `Historial` / `History` | `history` | `history` (el mismo) |

- **AC-APP-02-02 · Perfil solo con Strava.** *Dado* un build con Strava configurado (en la It. 2, solo con una configuración de prueba en PlayMode; la regla real es F-STR-01, It. 12), *entonces* aparece un quinto destino **al final**, `navProfile` (`Perfil` / `Profile`), con `person` sin relleno y relleno al elegirlo, e Historial sigue siendo el índice 3. Sin Strava, el destino no existe: ni oculto ni deshabilitado. [Editor-PM] · [Captura] CAP-25 · `main.dart:126-134`, `:141`, `:171-176`
- **AC-APP-02-03 · Selección.** *Entonces* al abrir está elegido el índice 0, y tocar un destino lo elige en el acto (tocar el que ya está elegido no cambia nada; que Historial recargue en cada toque es de la It. 9, Q-22). Las etiquetas se ven siempre, en los 4 destinos. El elegido se distingue por una píldora detrás del ícono, la variante "elegida" del ícono y la etiqueta resaltada; los demás van en `text.secondary`. [Editor-PM] · [Captura] CAP-20 · `main.dart:144-149`
- **AC-APP-02-04 · Cada pestaña conserva su estado.** *Dado* Dispositivo desplazado hacia abajo, *cuando* el usuario va a Rutinas y vuelve, *entonces* Dispositivo sigue en la misma posición. *Dado* una búsqueda de pulsómetro en curso, *cuando* el usuario va a Workout y vuelve antes de los 10 s, *entonces* la tarjeta sigue en `Buscando sensores...` con su fila; si vuelve después de los 10 s, la encuentra en `Sin sensor` (la búsqueda venció mientras tanto). Ninguna pestaña se recrea al volver. [Editor-PM] · `main.dart:143`
- **AC-APP-02-05 · El ícono de Dispositivo indica selección, no conexión.** *Dado* el remo desconectado y Dispositivo elegida, *entonces* su ícono es `bluetooth_connected`; *dado* el remo conectado y otra pestaña elegida, *entonces* es `bluetooth`. La barra no refleja el estado del remo (paridad; §13). [Editor-PM] · `main.dart:151-155`
- **AC-APP-02-06 · "Atrás" en Android sale de la app.** *Dado* el shell en cualquier pestaña (por ejemplo, después de ir de Dispositivo a Rutinas), *cuando* el usuario usa "Atrás" del sistema, *entonces* la app sale a la pantalla anterior del sistema (en general, el inicio) sin confirmar y sin volver a la pestaña anterior. En Android 12 o más nuevo queda en segundo plano, como cualquier app del sistema, y al volver sigue donde estaba (misma pestaña, remo y pulsómetro como estaban); en Android 8 a 11 se cierra y la próxima vez arranca en Dispositivo (AC-APP-01-01). Es D-09, con un cambio respecto del legacy en Android 12 a 15 (§11.1, DV-06). [Device-And] · `main.dart:142-179` (el shell no maneja "Atrás")
- **AC-APP-02-07 · Rotar o redimensionar no reinicia nada.** *Dado* el remo y el pulsómetro simulados conectados, *cuando* se rota el teléfono o se cambia el tamaño de la ventana en Windows, *entonces* la pantalla se reacomoda (AC-BLE-11-05) sin reiniciar: el remo sigue conectado, la distancia sigue sumando desde donde iba, la tarjeta del pulsómetro queda en el mismo estado y la pestaña elegida no cambia. [Editor-PM] · [Device-Win] · [Device-And] · `android/app/src/main/AndroidManifest.xml:30` (no recrea la pantalla al rotar)
- **AC-APP-02-08 · Contenido provisorio.** *Entonces* hasta su iteración (Rutinas It. 3, Workout It. 4a, Historial It. 9, Perfil It. 12), cada pestaña muestra su barra superior con el título del catálogo (`workoutTitle` `Entrenamiento` / `Workout`; `routinesTitle` `Rutinas` / `Routines`; `historyTitle` `Historial` / `History`; `profileTitle` `Perfil` / `Profile`) y, centrado, el ícono de su pestaña en grande y apagado (blanco al 24 %), **sin ningún texto nuevo ni acciones**. El catálogo no suma claves (P-05). [Editor-PM] · [Captura] CAP-21 · `workout_screen.dart:53`; `routines_screen.dart:16`; `history_screen.dart:21`

### 4.3 F-APP-05 — Tema visual oscuro y estilos base

- **AC-APP-05-01 · Siempre oscuro.** *Dado* el sistema en modo claro (Android con el tema oscuro apagado; Windows con el modo de apps "Claro"), *entonces* la app se ve igual que con el sistema en modo oscuro: no hay tema claro ni selector. [Device-Win] · [Device-And] · `lib/shared/theme.dart:7-12`
- **AC-APP-05-02 · Colores base.** *Entonces* el fondo de toda pantalla es `#0D1B2A`; las tarjetas (métricas y pulsómetro) son `#1A2E45`, con esquinas de 16 dp y sin sombra; la barra superior tiene el mismo fondo `#0D1B2A` (se funde con la pantalla), título e íconos blancos y ninguna línea ni sombra debajo. En las capturas, el píxel del fondo, de una tarjeta y de la barra superior da el hex exacto (±1 por canal). [Captura] CAP-01 · `theme.dart:13-23`
- **AC-APP-05-03 · Tipografía.** *Dado* que el usuario descargó las fuentes y los íconos (ASSET_SLOTS §3-§4), *entonces* los textos van en Inter, los números del tablero y del pulsómetro en Barlow Semi Condensed, y ningún ícono se ve como un cuadrado vacío o un carácter suelto. Sin las fuentes, la app funciona igual con la fuente por defecto y este criterio queda NO VERIFICABLE ("a medio vestir"). [Captura] · ART_BIBLE §4.1
- **AC-APP-05-04 · Cifras de ancho fijo.** *Entonces* todo número del tablero y de la tarjeta del pulsómetro tiene dígitos de ancho fijo: `111` y `888` ocupan el mismo ancho, igual que `11:11` y `00:00`, y cuando el valor pasa de `149` a `151` los dígitos que no cambian no se mueven. [Editor-PM] (mide anchos y posiciones) · ART_BIBLE §4.2
- **AC-APP-05-05 · Tamaño de letra del sistema.** *Dado* el tamaño de letra del sistema en 1,3× (Android: Ajustes → Pantalla → Tamaño de la fuente), *entonces* los textos de interfaz crecen hasta 1,3× (no más; P-03), nada se corta ni se sale de su tarjeta o de la pantalla, lo que no entra se alcanza desplazando, y los valores grandes (tablero y pulso) se achican para entrar antes que desbordar. [Editor-PM] (escala de prueba) · [Device-And] · [Captura] CAP-24 · `features/device/device_screen.dart:321-323`; `heart_rate_card.dart:163-166`
- **AC-APP-05-06 · Texto terciario más legible (D-ART-03).** *Entonces* los textos que el legacy dibuja en blanco al 38 % sin estar deshabilitados (la pista `hrmWatchHint`, la ayuda `deviceNoResultsHint` y el subtítulo `SIM:HRM` de la fila del sensor) se ven en `text.tertiary` (blanco al 54 %). El 38 % queda solo para lo deshabilitado. [Captura] CAP-08, CAP-10 · `heart_rate_card.dart:103-104`, `:140-141`; `device_screen.dart:433-437`
- **AC-APP-05-07 · Teclado y foco en escritorio.** *Dado* Windows, *entonces* la tecla Tab recorre los controles accionables (destinos de la barra, `Desconectar`, `Buscar dispositivos`, botones y filas de la tarjeta del pulsómetro, botón y controles del simulador) con un anillo de foco visible (`focus.ring`), y Enter o Espacio activan el control enfocado (el legacy lo tiene por defecto en escritorio). [Editor-PM] · [Device-Win] · ART_BIBLE §10.4
- **AC-APP-05-08 · Reducir movimiento.** *Dado* el ajuste del sistema para reducir el movimiento (en Android, la escala de animación en 0), *entonces* el rodillo de dígitos pasa a un fundido de 80 ms, el corazón no late (queda con un realce fijo) y el cambio de pestaña es un fundido sin desplazamiento; los datos y los tiempos de actualización no cambian. [Editor-PM] (ajuste de prueba) · [Device-And] · ART_BIBLE §10.4; ANIMATION_BIBLE §2.4
- **AC-APP-05-09 · Tamaño real en pantalla.** *Entonces* la interfaz tiene en cada equipo el mismo tamaño físico que el legacy: en Windows con la escala de pantalla al 100 %, la ventana de 1280 × 720 px mide 1280 × 720 dp (y el tablero tiene 3 columnas); al 150 %, todo se ve 1,5 veces más grande; en Android, 1 dp = 1 px × la densidad del equipo. [Device-Win] · [Device-And] · `windows/runner/main.cpp:27-33` (ventana escalada según el DPI)

### 4.4 F-APP-06 — Paleta de colores por métrica

- **AC-APP-06-01 · Colores en pantalla.** *Entonces* en el tablero, cada valor, su ícono, su etiqueta y su borde superior van en el color de su métrica, y el número de la tarjeta del pulsómetro va en el color del pulso: split `#FF9F1C`, SPM `#00B4D8`, vatios `#FFD60A`, distancia `#4CC9F0`, pulso `#EF476F`. Medido en la captura sobre el trazo del valor: el hex exacto, ±2 por canal. El **valor** va al 100 %. El **borde superior** va al 60 % en reposo, como el legacy, y al 100 % durante el destello de dato nuevo (AC-BLE-11-06). El **ícono y la etiqueta** los fija el Designer para llegar a AA sin cambiar el matiz: al 100 % (el legacy los pone al 70 %) y, en el pulso (etiqueta e ícono de PULSO y título `PULSÓMETRO`), en `metric.hr.hud` (`#FF5A7E`, el mismo rosa más claro) (§11.1, DV-03). Tiempo `#E0E0E0` y calorías `#06D6A0` no aparecen en Dispositivo: se ven desde la It. 4a. [Captura] CAP-04a, CAP-04b, CAP-06 · `theme.dart:32-40`; `device_screen.dart:253-258`, `:300`, `:310`, `:315`; `heart_rate_card.dart:16`, `:32`, `:45`
- **AC-APP-06-02 · Una sola fuente de colores.** *Entonces* ninguna pantalla define colores propios: todos salen de los tokens de la It. 1 (AC-BASE-04) y un chequeo falla si aparece un hex suelto fuera de esa fuente. [dotnet] · ADR 016

### 4.5 F-APP-08 (+ F-RUT-19) — Componentes sin uso (descarte)

- **AC-APP-08-01 · No se migran.** *Entonces* QA confirma que ninguna pantalla ni test del legacy usa `MetricCard` ni `IntervalTile` (búsqueda de `metric_card`, `interval_tile` y `shared/widgets` en `lib/` y `test/`: 0 resultados fuera de esos dos archivos) y que la app nueva no los porta (Q-57). [QA] · `lib/shared/widgets/metric_card.dart`; `lib/shared/widgets/interval_tile.dart`
- **AC-APP-08-02 · Una sola tarjeta de métrica.** *Entonces* las tarjetas del tablero y la del pulsómetro se ven y se comportan igual (mismo fondo, radio, borde superior del color al 60 %, luz suave bajo el borde y encabezado de ícono + etiqueta): no hay tres versiones distintas como en el legacy (mejora de F-APP-08). La única diferencia es a propósito: el destello de dato nuevo es de las tarjetas del tablero; en la del pulsómetro, el dato vivo lo marca el corazón (AC-HRM-13-12). [QA] · [Captura] CAP-04b

### 4.6 F-BLE-07 — Desconexión manual

- **AC-BLE-07-01 · Botón solo con el remo conectado.** *Dado* el remo conectado, *entonces* la barra superior de Dispositivo muestra a la derecha el botón de texto `deviceDisconnect` (`Desconectar` / `Disconnect`) con el ícono `bluetooth_disabled` de 18 dp, en rojo claro (`state.dangerText`). *Dado* cualquier otro estado, el botón no está. [Editor-PM] · [Captura] CAP-01, CAP-08 · `device_screen.dart:20-31`
- **AC-BLE-07-02 · Desconecta sin preguntar.** *Cuando* el usuario lo toca, *entonces* en el acto y sin confirmación (Q-33) el remo pasa a desconectado, dejan de llegar lecturas y Dispositivo muestra la vista de búsqueda; con el simulador, sin lista: `No se encontraron dispositivos` (AC-BLE-05-02). [dotnet] · [Editor-PM] · `ble_service.dart:304-313`; `simulated_ble_service.dart:74-78`
- **AC-BLE-07-03 · No se reconecta solo.** *Dado* que el usuario desconectó, *cuando* pasan 10 s, *entonces* el remo sigue desconectado y sin nombre (LT-22). Solo vuelve si el usuario toca `Buscar dispositivos` (con el simulador, al instante: AC-BLE-15-04). [dotnet] · [Editor-PM] · `test/simulated_ble_service_test.dart:64`
- **AC-BLE-07-04 · Le gana a una reconexión pendiente.** *Dado* una caída simulada con la vuelta de 3 s pendiente, *cuando* se pide desconectar antes de los 3 s, *entonces* a los 3 s no se reconecta. [dotnet] · `simulated_ble_service.dart:74-78`

### 4.7 F-BLE-11 — Pestaña Dispositivo, vista conectada

- **AC-BLE-11-01 · Qué vista se ve.** *Entonces* Dispositivo muestra la vista conectada si el remo está conectado, y la vista de búsqueda en cualquier otro estado (desconectado, buscando, conectando o apagado). [dotnet] (regla) · [Editor-PM] · `device_screen.dart:33-38`
- **AC-BLE-11-02 · Cabecera.** *Entonces* arriba, centrados: el ícono `rowing` de 32 dp en el color de acento, el nombre del remo (`Remo simulado`) y una insignia verde con el ícono `bluetooth_connected` de 12 dp y `deviceConnected` (`Conectado` / `Connected`) en 12 dp, en verde claro (`state.connected` `#69F0AE` sobre `state.connectedBg`, radio 12). Si el remo conectado no tuviera nombre, el legacy muestra `Rower`; con el simulador no pasa (siempre informa `Remo simulado`) y ese caso se cierra en la It. 10a (§13). [Editor-PM] · [Captura] CAP-01 · `device_screen.dart:92-118`
- **AC-BLE-11-03 · Métricas, orden e íconos.** *Entonces* debajo de la cabecera (a 24 dp) va la grilla, con estas tarjetas y en este orden. Los textos de los valores son los de AC-BLE-01-05. [Editor-PM] · [Captura] CAP-01, CAP-06 · `device_screen.dart:252-259`

  | # | Etiqueta (clave · es / en) | Valor | Color | Ícono |
  |---|---|---|---|---|
  | 1 | `metricSplit` · `SPLIT 500m` / `SPLIT 500m` | `MM:SS` o `--:--` | `#FF9F1C` | `timer` |
  | 2 | `metricSpm` · `SPM` / `SPM` | 1 decimal con punto (`24.0`) | `#00B4D8` | `speed` |
  | 3 | `metricWatts` · `VATIOS` / `WATTS` | entero sin unidad (`150`) | `#FFD60A` | `bolt` |
  | 4 | `metricDistance` · `DISTANCIA` / `DISTANCE` | entero con `m` pegada (`226m`) | `#4CC9F0` | `straighten` |
  | 5 | `metricHeartRate` · `PULSO` / `HR` | entero sin unidad (`140`) | `#EF476F` | `favorite` |

- **AC-BLE-11-04 · PULSO solo con el pulso del propio remo.** *Dado* una lectura del remo con pulso > 0 (el que manda el monitor por FTMS), *entonces* aparece la quinta tarjeta PULSO con ese valor; *dado* pulso 0, no está. El pulso del pulsómetro **nunca** aparece en la grilla (se ve en su tarjeta). Con el remo simulado nunca aparece, porque manda pulso 0 (AC-BLE-15-03); se verifica con el remo de prueba: manda 140 → 5 tarjetas, PULSO `140`; después 0 → vuelven a ser 4. [dotnet] (regla) · [Editor-PM] · [Captura] CAP-06 · `device_screen.dart:257-258`
- **AC-BLE-11-05 · 2 o 3 columnas.** *Entonces* con la ventana de **hasta 640 dp** de ancho la grilla tiene **2 columnas** de tarjetas con proporción ancho / alto **1,3**, y con **más de 640 dp** tiene **3 columnas** con proporción **1,6**; la separación entre tarjetas es de 12 dp en los dos sentidos. Es la regla del legacy: grilla de más de 600 dp de ancho, con 20 dp de margen a cada lado. Al cambiar el ancho en vivo (rotar o redimensionar) cambia la cantidad de columnas sin perder estado (AC-APP-02-07). [dotnet] (regla) · [Editor-PM] · [Captura] CAP-01, CAP-03, CAP-04a, CAP-05 · `device_screen.dart:87-89`, `:261-276`

  | Ancho de la ventana (dp) | 360 | 411 | 640 | 641 | 914 | 1280 |
  |---|---|---|---|---|---|---|
  | Columnas | 2 | 2 | **2** | **3** | 3 | 3 |

- **AC-BLE-11-06 · Tarjeta de métrica.** *Entonces* cada tarjeta tiene fondo `#1A2E45`, esquinas de 16 dp y un borde superior de 3 dp en el color de su métrica **al 60 % en reposo** (como el legacy) y **al 100 % en el destello** de dato nuevo, que dura menos de 300 ms y solo ocurre en la tarjeta cuyo valor cambió (§10.2); arriba, centrados, el ícono de 14 dp y la etiqueta en 12 dp semi-negrita con espaciado entre letras, en el color de la métrica (al 100 %; el legacy, al 70 %: AC-APP-06-01, §11.1 DV-03); en el centro, el valor en 52 dp, en el color de la métrica. Si el valor no entra a lo ancho, se achica hasta entrar: nunca se corta ni se sale (por ejemplo, `123456m` con 2 columnas a 411 dp). [Editor-PM] · [Captura] CAP-07 · `device_screen.dart:288-335`
- **AC-BLE-11-07 · Lo que va debajo y el desplazamiento.** *Entonces* debajo de la grilla, a 12 dp, va la tarjeta del pulsómetro (AC-HRM-13-01), y todo el contenido de la vista conectada se desplaza en vertical como un solo bloque, con 20 dp de margen a los costados, 24 dp sobre la cabecera (16 + 8) y **64 dp debajo de la tarjeta del pulsómetro** hasta el final del contenido (32 + 16 + el margen de 16; en el legacy ese hueco aloja el acceso oculto al diagnóstico, que llega en la It. 10a). Desplazado hasta el final, el final del contenido queda por encima de la barra de pestañas: la barra no tapa nada que no se pueda alcanzar (QA-G2-12). [Editor-PM] · `device_screen.dart:87-136` (`:89`, `:92`, `:123`, `:135`)
- **AC-BLE-11-08 · Lo que no se muestra.** *Entonces* calorías, tiempo y paladas totales **no** aparecen en Dispositivo, aunque el remo los mande (DC-15). [Editor-PM] · `device_screen.dart:252-259`
- **AC-BLE-11-09 · Actualización y ceros al conectar.** *Entonces* la grilla se actualiza con cada lectura (con el simulador, exactamente una por segundo). Cada vez que el remo pasa a conectado, la grilla arranca en ceros (`--:--`, `0.0`, `0`, `0m`) hasta la primera lectura de esa conexión. [dotnet] · [Editor-PM] · `device_provider.dart:38-42`, `:52-55`, `:64-67`
- **AC-BLE-11-10 · Valores de referencia con el simulador.** *Dado* el simulador sin ruido, *entonces* la grilla muestra lo de la tabla (valores del oráculo de la It. 1, AC-BLE-14-03 a -06). [dotnet] (lectura → textos) · [Editor-PM] · [Captura] CAP-01, CAP-19

  | Situación | SPLIT 500m | SPM | VATIOS | DISTANCIA |
  |---|---|---|---|---|
  | 60 s a 150 W y 24 spm desde que abrió la app | `02:13` | `24.0` | `150` | `226m` |
  | Preset `Suave` (100 W, 20 spm), lectura siguiente | `02:32` | `20.0` | `100` | sigue sumando |
  | Preset `Medio` (180 W, 24 spm) | `02:05` | `24.0` | `180` | sigue sumando |
  | Preset `Fuerte` (280 W, 30 spm) | `01:48` | `30.0` | `280` | sigue sumando |
  | 30 W (mínimo) / 500 W (máximo) | `03:47` / `01:29` | según el ritmo | `30` / `500` | sigue sumando |
  | `Remando` apagado | `--:--` | `0.0` | `0` | congelada |

- **AC-BLE-11-11 · El número que se ve es el real.** *Dado* una lectura nueva, *entonces* la transición de entrada (rodillo de los dígitos que cambian, ≤ 180 ms) termina siempre en el valor exacto de esa lectura; nunca muestra valores intermedios inventados (no "cuenta" de 150 a 160), y si llega otra lectura antes de terminar, salta al final y arranca la nueva. [Editor-PM] · [Captura] VID-01 · ANIMATION_BIBLE §2.4

### 4.8 F-BLE-13 (+ F-APP-18) — Modo simulador: activación y aislamiento

- **AC-BLE-13-01 · Solo para desarrollo.** *Entonces* el modo simulador se elige al compilar: hay una forma documentada de abrir la app en modo simulador en el Editor (equivalente a la configuración "RowMate (simulador)" del legacy) y de generar un build de desarrollo con simulador; un build con simulador que no sea de desarrollo no se genera (falla con un mensaje claro), y ningún perfil ni workflow de distribución lo activa (R-01, Q-40). [QA] · [Editor-EM] · `lib/core/dev/dev_config.dart:1-5`; `.vscode/launch.json`
- **AC-BLE-13-02 · Nada del simulador en el build normal.** *Dado* el build normal de Windows, *entonces* no aparecen la cinta, el botón de la llave, el panel ni los nombres `Remo simulado` o `Pulsómetro simulado`, y QA comprueba en el propio build (no solo en la pantalla) que no trae el código del simulador. [Device-Win] · [QA] · [Captura] CAP-22 · `dev_config.dart:3-5`
- **AC-BLE-13-03 · Reemplaza las dos cosas a la vez.** *Entonces* en modo simulador el remo y el pulsómetro son los dos simulados y comparten el mismo atleta (AC-APP-01-02). [dotnet] · `main.dart:51-54`
- **AC-BLE-13-04 · Las pantallas no saben.** *Entonces* ninguna pantalla cambia según el modo: lo único propio del simulador es su capa (cinta, botón y panel). Dispositivo y la tarjeta del pulsómetro se ven y se comportan igual con los servicios simulados que con el remo y el pulsómetro de prueba (R-06). [QA] · [Editor-PM] · `main.dart:51-54`, `:108-110`
- **AC-BLE-13-05 · El build normal de la It. 2, todavía sin Bluetooth.** *Dado* el build normal (el Bluetooth real llega en las It. 10a y 11), *entonces* se comporta como el legacy en un equipo sin Bluetooth LE: Dispositivo abre en la vista de búsqueda con `Buscar dispositivos` habilitado y el estado vacío (AC-BLE-05-02); tocar `Buscar dispositivos` no hace nada (ni estado, ni mensaje, ni error); la tarjeta del pulsómetro está en `Sin sensor` y su `Buscar` tampoco hace nada. Es provisorio (P-04). [Editor-PM] · [Device-Win] · [Captura] CAP-22 · `ble_service.dart:65-67`; `heart_rate_service.dart:113`

### 4.9 F-BLE-15 — Remo simulado "conectado"

- **AC-BLE-15-01 · Nombre.** *Entonces* el remo simulado se llama `Remo simulado` (fijo en español, Q-03) y el nombre existe solo mientras está conectado; en cualquier otro estado no tiene nombre. [dotnet] · `simulated_ble_service.dart:11`, `:59-60`
- **AC-BLE-15-02 · Arranca conectado, una sola vez.** *Cuando* la app empieza a escuchar el estado del remo, *entonces* el remo pasa a conectado sin que nadie lo pida, **una sola vez** en la vida de la app, y el Bluetooth figura siempre encendido: con el simulador nunca se ve "Bluetooth apagado" (LT-20). [dotnet] · `simulated_ble_service.dart:17-26`, `:55`
- **AC-BLE-15-03 · Una lectura por segundo, con pulso 0.** *Dado* conectado, *entonces* manda una lectura por segundo (un paso de la física de la It. 1) con el pulso en 0, como un monitor real: el pulso llega solo por el pulsómetro simulado. En 3 s llegan 3 lecturas, las 3 con pulso 0, y la distancia de la última es mayor que la de la primera (LT-20). [dotnet] · `simulated_ble_service.dart:88-97`
- **AC-BLE-15-04 · Buscar y Conectar reconectan al instante.** *Dado* desconectado, *cuando* se pide buscar o conectar, *entonces* pasa a conectado en el acto: no hay estado "buscando" ni lista de equipos. Detener la búsqueda no hace nada (LT-22). [dotnet] · `simulated_ble_service.dart:62-71`
- **AC-BLE-15-05 · Desconectar.** *Cuando* se desconecta, *entonces* dejan de llegar lecturas, pasa a desconectado y no se reconecta solo (AC-BLE-07-03 y -04). [dotnet] · `simulated_ble_service.dart:74-78`
- **AC-BLE-15-06 · Caída simulada de 3 s.** *Dado* conectado, *cuando* se simula una desconexión, *entonces* pasa a desconectado en el acto y no llegan lecturas: a los 2 s los estados fueron [conectado, desconectado] y no hubo ninguna lectura; a los 3 s vuelve a conectado ([conectado, desconectado, conectado]) (LT-21). *Dado* que no está conectado, simular la desconexión no hace nada. [dotnet] · `simulated_ble_service.dart:81-86`
- **AC-BLE-15-07 · Sin avisos repetidos.** *Dado* conectado, *cuando* se pide conectar otra vez, *entonces* no se repite el aviso de "conectado". [dotnet] · `simulated_ble_service.dart:91`
- **AC-BLE-15-08 · Sin paquetes crudos ni lista.** *Entonces* el remo simulado no manda paquetes crudos ni lista de equipos: el diagnóstico (It. 10a) mostraría `NO DATA` y el vigilante de 5 s del monitor real (F-BLE-09, It. 10a) nunca actúa con el simulador. [dotnet] · `simulated_ble_service.dart:34-35`
- **AC-BLE-15-09 · La física no se reinicia al reconectar.** *Dado* el simulador sin ruido a 150 W con 10 lecturas (`37m`), *cuando* hay una caída simulada y vuelve a los 3 s, *entonces* la primera lectura después de reconectar es `41m` con 11 s de tiempo: no vuelve a 0 ni cuenta los 3 s caído. Distancia, paladas y tiempo suman desde que abrió la app y solo avanzan mientras está conectado. [dotnet] · `simulated_ble_service.dart:88-103`; `core/dev/rowing_simulator.dart:45-54`
- **AC-BLE-15-10 · Lo que se ve en Dispositivo.** *Entonces* al abrir se ve la vista conectada (`Remo simulado` + `Conectado`); con `Simular desconexión`, durante 3 s se ve la vista de búsqueda con `No se encontraron dispositivos` y después vuelve la vista conectada, primero en ceros y al segundo con valores (AC-BLE-11-09). [Editor-PM] · [Captura] CAP-08, VID-03

### 4.10 F-BLE-16 — Panel de control del simulador

- **AC-BLE-16-01 · Cinta SIMULADOR.** *Entonces* en modo simulador hay una cinta diagonal con el texto `SIMULADOR` en la esquina superior izquierda, en naranja profundo (`dev.simulator` `#FF5722`) con texto oscuro (`dev.simulatorText` `#1A0A00`; **cambio**: el legacy lo escribe en blanco, §11.1 DV-01), con la geometría del legacy (franja de 12 dp de alto a 45°, centrada a 24 dp de la esquina), por encima de toda la app: las 4 pestañas y, cuando existan, las pantallas apiladas, los diálogos y el entrenamiento a pantalla completa. Siempre está y no se puede ocultar. [Editor-PM] · [Captura] CAP-01 · `lib/core/dev/simulator_overlay.dart:33-36`; `main.dart:108-110`
- **AC-BLE-16-02 · Botón de la llave.** *Entonces* con el panel cerrado (como arranca siempre) hay un botón flotante chico con el ícono `build` (llave), a 16 dp del borde derecho y por encima de la barra de pestañas, con 16 dp de aire entre los dos (legacy: 96 dp desde abajo, sobre una barra de 80 dp). Tocarlo abre el panel en ese mismo lugar, anclado abajo a la derecha; mientras el panel está abierto, el botón no se ve. [Editor-PM] · [Captura] CAP-01, CAP-18 · `simulator_overlay.dart:29`, `:40-49`
- **AC-BLE-16-03 · Tamaño del panel.** *Entonces* el panel mide **280 dp** de ancho fijo y su alto máximo es: alto de la ventana − 96 − margen seguro superior − 16, con un mínimo de 120 dp. Si el contenido no entra, se desplaza adentro del panel, y el botón de cerrar queda siempre visible. Ejemplos sin margen seguro: ventana de 360 dp → 248 dp; de 914 dp → 802 dp (entra todo); de 200 dp → 120 dp. [dotnet] (regla) · [Editor-PM] · `simulator_overlay.dart:83-95`
- **AC-BLE-16-04 · Contenido y orden.** *Entonces* el panel muestra, en este orden y con estos textos fijos en español en los dos idiomas (Q-03): [Editor-PM] · [Captura] CAP-16, CAP-17, CAP-18 · `simulator_overlay.dart:101-166`

  | # | Control | Texto | Comportamiento |
  |---|---|---|---|
  | 1 | Título y cerrar | `Simulador` + botón con ícono `close` | cierra el panel y vuelve el botón de la llave |
  | 2 | Presets | `Suave` · `Medio` · `Fuerte` | AC-BLE-16-05 |
  | 3 | Vatios | `Watts` · − · `<n> W` · + | AC-BLE-16-06 |
  | 4 | Ritmo | `Ritmo` · − · `<n> spm` · + | AC-BLE-16-06 |
  | 5 | Hora de la escena | `Hora escena` · − · `real` o `<h>:00` · + | AC-BLE-16-07 |
  | 6 | Volver a la hora real | `Hora real` (solo con hora forzada) | AC-BLE-16-07 |
  | 7 | Remar | interruptor `Remando` | AC-BLE-16-08 |
  | 8 | Caída del remo | `Simular desconexión`, ícono `link_off` | AC-BLE-15-06 |
  | 9 | Caída del pulsómetro | `Simular caída del pulsómetro`, ícono `heart_broken` | AC-HRM-15-08 |

- **AC-BLE-16-05 · Presets.** *Cuando* se toca `Suave`, `Medio` o `Fuerte`, *entonces* los objetivos pasan a 100 W y 20 spm, 180 W y 24 spm, o 280 W y 30 spm, y el panel lo muestra (por ejemplo, `280 W` y `30 spm`). Los presets no tocan `Remando` ni la hora (LT-26). [dotnet] · [Editor-PM] · `simulator_overlay.dart:10-14`, `:112-124`
- **AC-BLE-16-06 · Vatios y ritmo.** *Entonces* el panel arranca en `150 W` y `24 spm`; − y + de vatios mueven de a 10 dentro de 30-500, y los de ritmo de a 1 dentro de 14-40, sin pasarse: en `500 W`, + deja `500 W`; en `30 W`, − deja `30 W`; en `40 spm`, + deja `40 spm`; en `14 spm`, − deja `14 spm`. Desde 150, + de vatios muestra `160 W` y + de ritmo deja 25 (LT-26). [dotnet] · [Editor-PM] · `simulator_overlay.dart:125-138`; `rowing_simulator.dart:20-26`
- **AC-BLE-16-07 · Hora de la escena (F-SCN-23).** *Entonces* sin hora forzada el valor dice `real`. Cada − o + mueve 1 h desde la hora forzada o, si no hay, desde la hora del reloj del equipo (solo la hora, sin los minutos), y da la vuelta entre 23 y 0. Con hora forzada se ve `<h>:00` con la hora sin cero a la izquierda, y aparece el botón `Hora real`, que vuelve a `real` y desaparece. Con el reloj a las 13:45: + → `14:00`; − → `12:00`. Desde `23:00`, + → `0:00`; desde `0:00`, − → `23:00`. En la It. 2 no hay escena: el valor se guarda y se ve en el panel; su efecto sobre la luz se verifica en la It. 6a. [dotnet] (con reloj de prueba) · [Editor-PM] · `simulator_overlay.dart:74`, `:139-150`; `features/workout/scene/scene_settings.dart:19-24`
- **AC-BLE-16-08 · Remando.** *Entonces* el interruptor arranca encendido, y tocar el interruptor o su rótulo lo cambia. Apagado: desde la lectura siguiente, vatios 0, ritmo 0 y split `--:--`, con distancia, paladas, tiempo y calorías congelados, y el pulso simulado bajando hacia 70. Encendido otra vez: vuelve a remar con los objetivos actuales (LT-26). [dotnet] · [Editor-PM] · `simulator_overlay.dart:151-156`; `rowing_simulator.dart:41-44`, `:56-57`
- **AC-BLE-16-09 · Fallas sin errores.** *Entonces* `Simular desconexión` con el remo conectado provoca la caída de 3 s (AC-BLE-15-06), y `Simular caída del pulsómetro` con el pulsómetro conectado, la de AC-HRM-15-08. Si el equipo correspondiente no está conectado, el botón no hace nada. Ninguno produce un error ni una excepción (LT-26). [Editor-PM] · `simulator_overlay.dart:157-166`
- **AC-BLE-16-10 · Efecto inmediato.** *Entonces* cada cambio del panel vale en el acto para el simulador y se ve en la lectura siguiente (≤ 1 s), en el tablero y en la tarjeta del pulsómetro. [dotnet] · [Editor-PM] · `simulator_overlay.dart:115-156`
- **AC-BLE-16-11 · Nada se guarda.** *Entonces* objetivos, hora forzada, `Remando` y panel abierto o cerrado duran mientras la app está abierta: cerrar y volver a abrir el panel muestra los valores actuales, y al reiniciar la app todo vuelve a `150 W`, `24 spm`, `real`, `Remando` encendido y panel cerrado. [Editor-PM] · [Device-Win] · `scene_settings.dart:14`; `simulator_overlay.dart:29`
- **AC-BLE-16-12 · Ventana baja.** *Dado* una ventana de 800 × 360 dp, *cuando* se toca la llave, *entonces* el panel abre (se ve `150 W`), el botón de cerrar queda dentro de la ventana y, al tocarlo, el panel se cierra (deja de verse `150 W`) (LT-27). [Editor-PM] · [Captura] CAP-16 · `test/simulator_overlay_test.dart:90`

### 4.11 F-HRM-04 — Pulso en vivo

- **AC-HRM-04-01 · Cada lectura, tal cual.** *Entonces* cada pulso que llega se publica y se muestra, aunque repita el anterior, sin promedios ni suavizado. Con el pulsómetro simulado conectado llegan 3 lecturas en 3 s, y la última es el pulso del simulador (> 0), que es también el valor actual (LT-23). [dotnet] · `heart_rate_service.dart:321-331`; `simulated_heart_rate_service.dart:126-129`
- **AC-HRM-04-02 · Contacto con la piel.** *Dado* una lectura del sensor (AC-HRM-01 de la It. 1), *entonces* si dice "sin contacto" se publica 0, y si dice "con contacto" o "no informa" se publica el pulso leído, aunque sea 0. La regla se verifica ahora; con un sensor real, en la It. 11. [dotnet] · `heart_rate_service.dart:321-326`
- **AC-HRM-04-03 · 0 = sin lectura.** *Entonces* el pulso actual vale 0 al abrir la app, y 0 quiere decir "sin lectura válida": la tarjeta muestra `--`. [dotnet] · [Editor-PM] · `heart_rate_card.dart:171`
- **AC-HRM-04-04 · 0 al cortarse, una sola vez.** *Cuando* la conexión se corta (caída o `Desconectar`), *entonces* se publica 0 si el último valor no era 0; si ya era 0, no se publica otro. [dotnet] · `simulated_heart_rate_service.dart:132-140`
- **AC-HRM-04-05 · No depende del remo.** *Entonces* el pulso llega aunque el remo esté quieto, desconectado o sin sesión. Con el simulador y el remo desconectado, el pulso queda en el último valor y se sigue publicando cada segundo (AC-HRM-15-10). [dotnet] · [Editor-PM] · `simulated_heart_rate_service.dart:126-129`

El rearme del vigilante de 10 s con cada lectura válida (HRM-04.1) es de F-HRM-05, It. 11.

### 4.12 F-HRM-12 — Estado de presentación del pulsómetro

- **AC-HRM-12-01 · Refleja el servicio.** *Entonces* el estado de la tarjeta refleja el estado del pulsómetro, el pulso y la lista de la búsqueda, y avisa a la tarjeta en cada cambio. Expone: conectado, buscando, conectando, reintentando, tiene recordado, nombre conectado y nombre recordado. Al empezar: desconectado, sin recordado y lista vacía. En el flujo buscar → conectar → caída → vuelta → desconectar avisa más de 3 veces y al final no queda ningún temporizador vivo (LT-32). [dotnet] · `features/device/heart_rate_provider.dart:20-54`
- **AC-HRM-12-02 · Nombre mientras conecta.** *Entonces* mientras conecta se muestra el nombre elegido en la lista; si no hay, el recordado; si tampoco, vacío. El nombre elegido vale solo para ese intento: cualquier estado distinto de "conectando" lo descarta (los reintentos automáticos muestran el recordado). Con el simulado: al tocar la fila, el nombre a mostrar es `Pulsómetro simulado`; ya conectado, el nombre "conectando" queda vacío y el conectado es `Pulsómetro simulado` (LT-32). [dotnet] · `heart_rate_provider.dart:28`, `:54`, `:65-70`
- **AC-HRM-12-03 · Errores.** *Entonces* toda falla de una acción (buscar, conectar, `Conectar`, desconectar, olvidar) se captura y queda visible en la tarjeta; se borra al **empezar** cualquier acción y al llegar a conectado, y **no** se borra al pasar a desconectado. Con un servicio que falla en todo: conectar deja el error "incompatible"; `Conectar` (reintentar) lo borra; desconectar no rompe nada aunque el servicio falle (LT-33). [dotnet] · `heart_rate_provider.dart:25`, `:58-101`
- **AC-HRM-12-04 · Incompatible o no.** *Entonces* el error de sensor incompatible se distingue de cualquier otro: el primero se muestra con `hrmIncompatible` y los demás con `hrmConnectError` (LT-34). [dotnet] · `heart_rate_provider.dart:56`
- **AC-HRM-12-05 · Buscar empieza limpio.** *Cuando* se busca, *entonces* antes de empezar se borran el error y la lista anterior. [dotnet] · `heart_rate_provider.dart:58-63`
- **AC-HRM-12-06 · Tarjeta que ya no existe.** *Dado* una acción lenta (hasta 15 s con un sensor real), *cuando* termina después de que la tarjeta dejó de existir, *entonces* no se intenta actualizarla y no hay error. [dotnet] · `heart_rate_provider.dart:91-116`

### 4.13 F-HRM-13 — Tarjeta "Pulsómetro"

- **AC-HRM-13-01 · Dónde está.** *Entonces* con el remo conectado, la tarjeta va debajo de la grilla (a 12 dp) y se desplaza con toda la vista; con el remo sin conectar, va al pie de la vista de búsqueda, en un área de alto acotado con desplazamiento propio que comparte el alto con el estado vacío, para que en horizontal nada desborde. Con el Bluetooth apagado o sin permiso la tarjeta no se muestra (F-BLE-04, It. 10a; con el simulador no pasa). [Editor-PM] · `device_screen.dart:117-123`, `:367`, `:419-424`, `:472-474`
- **AC-HRM-13-02 · Estructura y encabezado.** *Entonces* la tarjeta ocupa todo el ancho, con fondo `#1A2E45`, radio 16, borde superior de 3 dp en el color del pulso al 60 % (fijo, como el legacy: esta tarjeta no destella) y relleno de 16 dp; arriba, un corazón relleno de 14 dp en el color del pulso y `hrmTitle` **en mayúsculas** (`PULSÓMETRO` / `HEART RATE SENSOR`) en 12 dp semi-negrita en el rosa del pulso (`metric.hr.hud`; el legacy lo pone al 70 %: §11.1 DV-03); debajo, el cuerpo según el estado; al final, si hay error, la línea de error. El corazón va en color pleno con el pulsómetro conectado (con lectura o con `--`) y al 40 % en cualquier otro estado. [Editor-PM] · [Captura] CAP-10 a CAP-15 · `heart_rate_card.dart:26-62`
- **AC-HRM-13-03 · Cuerpo según el estado.** *Entonces* el cuerpo y sus acciones salen de esta tabla, en este orden de prioridad. Con el simulador se ven las filas 1, 4, 5 (reconectando) y 6; las filas 2, 3 y 5 (no encontrado) se verifican con el pulsómetro de prueba. [dotnet] (regla) · [Editor-PM] · [Captura] CAP-10 a CAP-14 · `heart_rate_card.dart:68-94`

  | # | Condición | Cuerpo | Acciones |
  |---|---|---|---|
  | 1 | conectado | nombre conectado + pulso (`142` o `--`) + `bpm` | `Desconectar` |
  | 2 | conectando, reintentando **y** con recordado | igual que la fila 5 | igual que la fila 5 |
  | 3 | conectando (el resto: conexión manual) | spinner de 16 dp + `Conectando a {name}...` (`hrmConnectingTo`) | ninguna |
  | 4 | buscando | spinner de 16 dp + `Buscando sensores...` (`hrmSearching`) + lista de sensores | tocar una fila conecta |
  | 5 | desconectado con recordado | `{recordado} · reconectando...` (`hrmReconnecting`) si reintenta; si no, `{recordado} · no encontrado` (`hrmNotFound`) | `Conectar`, `Buscar otro`, `Olvidar` |
  | 6 | desconectado sin recordado | `Sin sensor` (`hrmNoSensor`) + pista `hrmWatchHint` | `Buscar` |

- **AC-HRM-13-04 · Botones habilitados.** *Entonces* un botón deshabilitado se ve al 38 % y no responde, según esta tabla (LT-31). [dotnet] (regla) · [Editor-PM] · [Captura] CAP-15 · `heart_rate_card.dart:22-24`, `:108`, `:212`, `:218`

  | Botón | Deshabilitado cuando |
  |---|---|
  | `Buscar` (`hrmSearch`) y `Buscar otro` (`hrmSearchOther`) | el remo está buscando o conectando |
  | `Conectar` (`hrmConnect`) | reintentando, o el remo está buscando o conectando |
  | `Olvidar` (`hrmForget`), `Desconectar` (`hrmDisconnect`) y las filas de la lista | nunca |

- **AC-HRM-13-05 · Fila de la lista.** *Entonces* cada fila tiene el ícono `favorite` sin relleno en color pulso; de título, el nombre del sistema, si no el anunciado, y si no `hrmUnknownSensor` (`Sensor desconocido` / `Unknown sensor`); de subtítulo, el id del dispositivo en 11 dp `text.tertiary`; y la flecha `chevron_right`. Tocarla conecta usando ese título como nombre elegido. Con el simulador hay una sola fila: `Pulsómetro simulado` / `SIM:HRM`. [dotnet] · [Editor-PM] · [Captura] CAP-11 · `heart_rate_card.dart:131-145`, `:232-237`
- **AC-HRM-13-06 · Conectado.** *Entonces* se ve el nombre conectado en una línea (con "…" si no entra) en blanco al 70 %; debajo, el pulso como entero sin separadores (`142`) o `--` si es 0, en 52 dp color pulso, con `bpm` (`hrmBpm`) en 14 dp alineado por la línea de base, **en el matiz del pulso** (el legacy: rosa al 70 %; la app, con la luminancia que fije el Designer para AA, por ejemplo `metric.hr.hud`; no en blanco: §11.1 DV-04); con letra grande el número se achica para entrar. A la derecha, `Desconectar` en rojo claro con el ícono `bluetooth_disabled` de 18 dp. [Editor-PM] · [Captura] CAP-12, CAP-04b · `heart_rate_card.dart:150-196`
- **AC-HRM-13-07 · Línea de error.** *Entonces* el error ocupa hasta 3 líneas y lo que sobra termina en "…"; si es incompatible dice `hrmIncompatible` (`No es un pulsómetro compatible (sin servicio Heart Rate)` / `Not a compatible heart rate sensor (no Heart Rate service)`), y si no, `hrmConnectError` con el texto técnico (`Error al conectar: {error}` / `Connection error: {error}`), en 12 dp `state.dangerText`. [Editor-PM] (pulsómetro de prueba) · [Captura] CAP-14 · `heart_rate_card.dart:52-62`
- **AC-HRM-13-08 · Búsquedas que se excluyen.** *Dado* el remo desconectado, *entonces* `Buscar dispositivos` está habilitado; *cuando* el pulsómetro empieza a buscar, se deshabilita; *cuando* esa búsqueda termina (se detiene, vence o conecta), se habilita otra vez (LT-29). *Dado* el remo buscando o conectando (remo de prueba), *entonces* `Buscar` del pulsómetro está deshabilitado, y vuelve a habilitarse con el remo desconectado (LT-31). La regla es asimétrica: que el pulsómetro esté **conectando** no frena al remo. [Editor-PM] · [Captura] CAP-09, CAP-15 · `device_screen.dart:348`, `:397`; `heart_rate_card.dart:22-24`
- **AC-HRM-13-09 · Flujo completo con el simulado.** *Dado* el remo simulado conectado y el idioma en español, *entonces* se ven `PULSÓMETRO`, `Sin sensor` y una pista que contiene `Heart for Bluetooth`, con la tarjeta debajo de la grilla (en una ventana de 800 × 600 dp hay que desplazarse para tocar su botón). *Cuando* se toca `Buscar`, se ven `Buscando sensores...` y `Pulsómetro simulado`; *cuando* se toca la fila, el pulsómetro queda conectado y se ven `Pulsómetro simulado` y `--` (todavía sin lectura); al segundo se ve el pulso que publicó el pulsómetro (> 0) y ya no `--`; *cuando* se toca `Desconectar`, vuelve `Sin sensor` (LT-28). [Editor-PM] · `test/heart_rate_card_test.dart:83`
- **AC-HRM-13-10 · Horizontal 640 × 360.** *Dado* una ventana de 640 × 360 dp con el remo desconectado (vista de búsqueda + tarjeta), *entonces* nada desborda; `Buscar` del pulsómetro se alcanza desplazando el área de la tarjeta; al tocarlo, el pulsómetro queda buscando y la fila `Pulsómetro simulado` existe dentro del área desplazable de la tarjeta (aunque quede fuera de la vista) sin desbordar (LT-30). [Editor-PM] · [Captura] CAP-09 · `test/heart_rate_card_test.dart:142`
- **AC-HRM-13-11 · Lo que la tarjeta no tiene.** *Entonces* la tarjeta no ofrece `Cancelar` durante una conexión manual, ni intensidad de señal, ni mensaje de lista vacía mientras busca, ni botón para detener la búsqueda (paridad: Q-39, Q-45, Q-46). [Editor-PM] · `heart_rate_card.dart:68-148`
- **AC-HRM-13-12 · El corazón late al pulso real.** *Dado* el pulsómetro conectado con 120 bpm, *entonces* el corazón del encabezado late 20 ± 1 veces en 10 s (período = 60 / bpm, con su segundo golpe corto), y el período nuevo se aplica desde el latido siguiente, sin saltos. *Dado* el pulsómetro conectado con `--` (todavía sin lectura), *entonces* queda quieto en color pleno; *dado* el pulsómetro sin conexión (incluido `reconectando...`), queda quieto al 40 % (AC-HRM-13-02). El legacy no anima el corazón; el latido es una mejora EXR (§10.3) y la opacidad es la del legacy. [Editor-PM] · [Captura] VID-01, CAP-12 · ANIMATION_BIBLE §2.9; `heart_rate_card.dart:39-40`

### 4.14 F-HRM-15 — Pulsómetro simulado

- **AC-HRM-15-01 · Solo en simulador, sin memoria.** *Entonces* existe solo en modo simulador y reemplaza al pulsómetro real; no guarda nada y su reconexión al arrancar no hace nada, así que la tarjeta siempre abre en `Sin sensor`. [dotnet] · `simulated_heart_rate_service.dart:9-11`, `:89-91`
- **AC-HRM-15-02 · Buscar.** *Cuando* se busca, *entonces* se cancela una vuelta simulada pendiente, pasa a buscando y la lista llega en el acto con **un único** sensor: nombre `Pulsómetro simulado`, id `SIM:HRM`, señal −50 dBm, anuncia el servicio 0x180D. Si nadie lo elige, a los 9 s sigue buscando y a los 10 s pasa a desconectado: la lista desaparece y vuelve `Sin sensor` (LT-23, LT-24). [dotnet] · [Editor-PM] · `simulated_heart_rate_service.dart:50-77`
- **AC-HRM-15-03 · Detener la búsqueda.** *Entonces* si está buscando, pasa a desconectado; si no, nada. [dotnet] · `simulated_heart_rate_service.dart:79-84`
- **AC-HRM-15-04 · Conectar.** *Cuando* se conecta a cualquier dispositivo, *entonces* pasa **directo** a conectado (sin pasar por "conectando": con el simulador no se ve `Conectando a ...`), publica cada 1 s el pulso del simulador y la primera lectura llega al segundo (hasta entonces la tarjeta muestra `--`). Conectar estando conectado no hace nada. [dotnet] · `simulated_heart_rate_service.dart:86-87`, `:118-130`
- **AC-HRM-15-05 · Nombre.** *Entonces* el nombre conectado es `Pulsómetro simulado` (fijo en español, Q-03), solo mientras está conectado. [dotnet] · `simulated_heart_rate_service.dart:13`, `:41-42`
- **AC-HRM-15-06 · Desconectar.** *Cuando* se desconecta, *entonces* se cancela una vuelta pendiente, deja de publicar, publica 0 si el último no era 0 y pasa a desconectado; 10 s después sigue desconectado y sin nombre conectado (LT-25). [dotnet] · `simulated_heart_rate_service.dart:93-98`, `:132-140`
- **AC-HRM-15-07 · Olvidar.** *Cuando* se olvida, *entonces* se cancela una vuelta pendiente y se vuelve a avisar el estado; una conexión activa no se corta. En la tarjeta: durante `Pulsómetro simulado · reconectando...`, `Olvidar` la deja en `Sin sensor` y a los 3 s **no** se reconecta. [dotnet] · [Editor-PM] · `simulated_heart_rate_service.dart:100-105`
- **AC-HRM-15-08 · Caída simulada.** *Dado* conectado, *cuando* se simula la caída desde el panel, *entonces* primero se publica 0 y después pasa a desconectado, con reintentando = sí y recordado = `Pulsómetro simulado`: la tarjeta muestra `Pulsómetro simulado · reconectando...`, con `Conectar` deshabilitado, `Buscar otro` y `Olvidar` habilitados y el corazón al 40 %. A los 2 s los estados fueron [conectado, desconectado]; a los 3 s vuelve a conectado, sin reintentar y sin recordado. *Dado* que no está conectado, no hace nada (LT-25, LT-26). [dotnet] · [Editor-PM] · [Captura] CAP-13 · `simulated_heart_rate_service.dart:107-116`
- **AC-HRM-15-09 · De dónde sale el pulso.** *Entonces* el pulso es el del simulador del remo: arranca en 70 y cada segundo se acerca un 10 % a su objetivo (AC-BLE-14-07 de la It. 1); el pulsómetro publica el último valor calculado, que puede ir un paso atrás o adelante del remo. Sin ruido, a 150 W y 24 spm desde el arranque, a los 60 s la tarjeta muestra `142`. [dotnet] · `simulated_heart_rate_service.dart:126-129`; `rowing_simulator.dart:31-37`, `:56-57`
- **AC-HRM-15-10 · Con el remo parado.** *Dado* el remo simulado desconectado (no avanza), *entonces* el pulso queda en el último valor y se sigue publicando cada segundo. [dotnet] · `simulated_heart_rate_service.dart:126-129`; `simulated_ble_service.dart:93-96`
- **AC-HRM-15-11 · Sin temporizadores al cerrar.** *Entonces* después de buscar, conectar, caer, volver y desconectar, al cerrar no queda ningún temporizador vivo (LT-23 a LT-25, LT-32). [dotnet] · `simulated_heart_rate_service.dart:147-155`
- **AC-HRM-15-12 · `Buscar otro` durante la caída.** *Dado* `Pulsómetro simulado · reconectando...`, *cuando* se toca `Buscar otro`, *entonces* se cancela la vuelta, la tarjeta pasa a buscando con la fila del sensor simulado y a los 3 s no se reconecta sola. [dotnet] · [Editor-PM] · `simulated_heart_rate_service.dart:66-77`

### 4.15 También toca

**F-BLE-05 (búsqueda de remos; se cierra en la It. 10a).**

- **AC-BLE-05-01 · Botón `Buscar dispositivos`.** *Entonces* la vista de búsqueda tiene arriba, a todo el ancho, el botón relleno `deviceSearch` (`Buscar dispositivos` / `Search devices`) con el ícono `bluetooth_searching`, habilitado salvo con el remo buscando o conectando o con el pulsómetro buscando. Con el remo buscando (remo de prueba) muestra un spinner blanco de 16 dp y `deviceSearching` (`Buscando...` / `Searching...`), y debajo aparece `deviceSearchHint` (`Buscando dispositivos FTMS con servicio 0x1826...` / `Searching for FTMS devices with service 0x1826...`) en 12 dp, centrado. [Editor-PM] · [Captura] CAP-08 · `device_screen.dart:396-417`
- **AC-BLE-05-02 · Estado vacío.** *Dado* el remo sin conectar, sin búsqueda en curso y sin resultados (con el simulador, siempre), *entonces* debajo del botón se ve, centrado, el ícono `bluetooth_disabled` de 48 dp apagado (blanco al 24 %), `deviceNoResults` (`No se encontraron dispositivos` / `No devices found`) en `text.secondary` (el legacy, al 54 %: §11.1 DV-05) y la ayuda `deviceNoResultsHint` en dos líneas (`Asegurate que el rower esté encendido⏎y el Bluetooth activado` / `Make sure the rower is on⏎and Bluetooth is enabled`), en 12 dp y centrada; si no entra, se desplaza. Se ve desde el primer momento en que aparece la vista, sin haber buscado nunca. [Editor-PM] · [Captura] CAP-08, CAP-22 · `device_screen.dart:419-442`
- **AC-BLE-05-03 · Exclusión con el pulsómetro.** Es AC-HRM-13-08 (LT-29, LT-31). La lista de remos, los spinners al conectar y el banner de error son de la It. 10a. [Editor-PM]

**F-BLE-08 (reconexión del remo; se cierra en la It. 10a).**

- **AC-BLE-08-01 · La caída simulada vista desde Dispositivo.** *Dado* una caída simulada del remo (AC-BLE-15-06), *entonces* durante los 3 s Dispositivo muestra la vista de búsqueda sin ningún texto de "reconectando" para el remo (como el legacy, BLE-08.5), `Buscar dispositivos` está habilitado y tocarlo reconecta en el acto (cancela la espera); si nadie toca nada, a los 3 s vuelve la vista conectada. La reconexión real (Q-29) es de la It. 10a. [dotnet] · [Editor-PM] · `simulated_ble_service.dart:81-91`

**F-BLE-10 (señal "remo conectado"; se cierra en la It. 4a).**

- **AC-BLE-10-01 · Dispositivo, primera pestaña.** *Entonces* Dispositivo es el destino 0 de la barra, con los íconos y la etiqueta de AC-APP-02-01, y conserva su estado al cambiar de pestaña (AC-APP-02-04). [Editor-PM] · `main.dart:124`, `:151-155`
- **AC-BLE-10-02 · El remo ocupado frena la búsqueda del pulsómetro.** Es AC-HRM-13-08, segunda parte (LT-31). El aviso de Workout sin remo y el bloqueo del inicio del entrenamiento son de la It. 4a. [Editor-PM]

**F-HRM-02, F-HRM-03, F-HRM-08 y F-HRM-10 (se cierran en la It. 11).** Con el pulsómetro simulado quedan cubiertos por AC-HRM-15-02 (búsqueda), AC-HRM-15-04 y AC-HRM-13-05 (conexión desde la lista), AC-HRM-15-08 (caída y vuelta) y AC-HRM-15-06, -07 y -12 (desconectar, olvidar, buscar otro). Sin criterios propios en esta iteración.

**F-SCN-23 (hora de la escena; se cierra en la It. 6a).** Es AC-BLE-16-07.

**F-APP-19 (compilación y CI; se cierra en la It. 14).**

- **AC-APP-19-05 · CI de Unity (opcional, P-08).** *Dado* que el usuario cargó la licencia de Unity como secreto y hay repositorio remoto, *entonces* `ci-unity.yml` corre los tests EditMode y PlayMode de la It. 2 en cada cambio y queda en verde, o en rojo nombrando el test que falla. Sin licencia o sin remoto, queda NO VERIFICABLE y no frena la iteración. [CI]

---

## 5. Criterios de la It. 1 que se verifican en la It. 2

La It. 1 los cerró con su parte de reglas ([REPORT §5](../01-fundaciones/REPORT.md)). Acá se verifica lo que faltaba ver; si falla, el criterio se reabre en esta iteración.

- **AC-BLE-01-05 · Textos de la grilla, en pantalla.** *Dado* el remo de prueba con 23,5 spm, 149 W y 652 m (los valores de `docs/image-01.png`), *entonces* la grilla muestra `23.5`, `149` y `652m`; con el simulador sin ruido, `24.0`, `150` y `226m` a los 60 s. Con la región del sistema en `es-AR` o `de-DE` (coma decimal) se ve igual, siempre con punto (AC-BASE-02). [Editor-PM] · [Captura] CAP-01, CAP-04a
- **AC-APP-09-04 · Sin selector ni memoria, en pantalla.** *Entonces* no hay ningún ajuste de idioma en la app (tampoco en el panel del simulador) y el idioma no se guarda: *dado* el sistema en español, la app abre en español; se cierra, el sistema pasa a inglés y la app abre en inglés. [Editor-PM] (idioma de prueba) · [Device-Win] · [Device-And]
- **AC-APP-09-05 · Cambio con la app abierta, en pantalla.** *Dado* la app abierta en español con el remo y el pulsómetro simulados conectados, *cuando* la lista del sistema pasa a `[en-US]`, *entonces* sin reiniciar la app los textos cambian a inglés (`Dispositivo` → `Device`, `PULSÓMETRO` → `HEART RATE SENSOR`, `Conectado` → `Connected`, las etiquetas de la barra) y nada más cambia: el remo sigue conectado, la distancia sigue desde donde iba (si la app se hubiera reiniciado, arrancaría en `0m`, AC-BLE-15-09) y la tarjeta queda en el mismo estado. Los textos del simulador siguen en español (Q-03). [Editor-PM] · [Device-And] (cambiar el idioma en Ajustes con la app abierta y volver). En Windows, cambiar el idioma de la pantalla pide cerrar la sesión: ahí vale AC-APP-09-04.
- **AC-APP-09-06 · Lista completa del sistema, en equipo.** *Dado* el sistema con la lista `Français (France)`, `Español (Argentina)` (en ese orden, sin inglés), *entonces* la app abre en **español**: toma la lista entera, no solo el primer idioma. *Dado* solo `Português (Brasil)`, abre en inglés. [Device-And] · [Device-Win] (Configuración → Hora e idioma → Idioma y región → idiomas preferidos)
- **AC-APP-16-01 · Nombre y versión, en equipo.** *Entonces* en Windows el ejecutable es `RowMate.exe`, el título de la ventana es `RowMate` y el producto es `RowMate` de `iguisoft`; en Android, la etiqueta en el lanzador y en Ajustes → Apps es `RowMate`. La versión `1.0.0` (compilación 1) se ve en Ajustes → Apps en Android y, en Windows, en las propiedades del ejecutable o, si el motor no la escribe ahí, en la primera línea del registro de diagnóstico de la app. En ningún lado aparece `rower_app` ni `com.example`. [Device-Win] · [Device-And] · DC-63
- **AC-APP-16-02 · Identificadores y carpeta de datos, en equipo.** *Entonces* en Windows, después del primer arranque, los datos de la app nueva están en `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate` y en ninguna carpeta del legacy; en Android, el paquete instalado es `com.iguisoft.rowmate`. [Device-Win] · [Device-And]
- **AC-APP-16-03 · Convivencia con el legacy, en equipo.** *Dado* la PC (y el teléfono, si hay) con la app legacy instalada y con datos (rutinas, historial, pulsómetro recordado), *cuando* se instala y abre la app nueva, *entonces* la legacy sigue instalada, abre y conserva todo, y la app nueva no abrió ni modificó la base ni las preferencias del legacy (en Windows, `AppData\Roaming\com.example\rower_app` y `rower_app.db`). [Device-Win] · [Device-And]
- **AC-APP-16-04 · Android mínimo, en equipo.** *Entonces* el paquete de Android declara como mínimo Android 8.0 (API 26) y trae solo código de 64 bits (`arm64-v8a`): QA lo ve en el paquete generado y lo instala en un Android 8+ de 64 bits. Sin un Android 7 o de 32 bits a mano, el rechazo se da por verificado con el paquete. [Device-And]
- **AC-BASE-01 · Paridad numérica dentro de Unity y en un build.** *Entonces* la tabla de AC-BASE-01 (`0.25` → `0.3`, `24.45` → `24.4`, `2.5` → `3`, −0,05 → 0, etc.) da lo mismo en el Editor y en un build de prueba de Windows y de Android con IL2CPP (R-T5, R-T6). Sin el módulo IL2CPP de Windows o el de Android, esa parte queda NO VERIFICABLE. [Editor-EM] · [Device-Win] · [Device-And]
- **AC-BASE-05 · Medición del vidrio (R-T1).** Con D-01 aceptada (§11), la app compone los colores translúcidos como el legacy **desde la It. 2** (la opción A de [04-design-notes §5 de la It. 1](../01-fundaciones/04-design-notes.md), adelantada). *Entonces* QA mide en CAP-23, con las dos composiciones lado a lado, el color de `glass.fallback` sobre blanco y el de `text.secondary` sobre `surface.card`: **la de la app** tiene que dar `#2A2F37` y `#BAC0C7` (±1 por canal), como el legacy, y así el chequeo de contraste describe lo que se ve; **la directa** da ≈ `#6A6A6C` y ≈ `#DADBDC` (espacio lineal: R-T1, ya medido en el *spike* S-0 y en el gate, [03-qa-harmony §1.2](03-qa-harmony.md)) y queda solo como referencia. Todas las capturas de §7 se toman con la composición de la app. *Dado* un build de Windows, QA repite la lectura en la pantalla (S-B1 de 02-tech-design): si da lo mismo, el criterio pasa; si en el build la app no puede usar su composición y vuelve a la directa, el Designer aplica el respaldo de [04-design-notes §8.3](04-design-notes.md), se repiten las capturas con translucidez (CAP-01, CAP-04a, CAP-04b, CAP-10, CAP-20) y la opción A queda pendiente para antes de la It. 4a, como estaba previsto. La It. 2 no se rechaza por el resultado, sino por no medirlo. [Captura] CAP-23 · [Device-Win]
- **AC-BASE-08 · Las pruebas del Core dentro de Unity.** *Entonces* todas las pruebas del Core (las 912 de la It. 1 y las nuevas de la It. 2) compilan y corren dentro de Unity, en EditMode, con el mismo resultado que en `dotnet`: 0 fallan. Las del simulador corren con el procedimiento documentado para activar el modo simulador en los tests (QA-V-05), y las que estaban ignoradas por "NO VERIFICABLE → It. 2" y se pueden correr en el Editor dejan de estar ignoradas. [Editor-EM]
- **AC-BASE-10 · El proyecto abre sin errores.** *Entonces* el proyecto abre en Unity **6000.3.27f1** sin errores de compilación (el registro no tiene `error CS`, `Scripts have compiler errors` ni `Compilation failed`, y la apertura en modo batch termina con código 0); `ProjectSettings/ProjectVersion.txt` sigue diciendo `6000.3.27f1`; la preparación automática aplica sus pasos una sola vez (si uno falla, avisa y no se repite en cada recompilación, QA-V-03) y no activa el modo simulador. [Editor-EM]

---

## 6. Los 15 casos de test legacy que se portan

Cada caso se porta 1 a 1, con su nombre original y el comentario de origen (`archivo:línea`, la línea de la declaración `test(` o `testWidgets(`). LT-29, LT-30 y LT-31 se vuelven a correr en la It. 10a contra el remo real con un adaptador falso (QA-P-27).

| LT | Archivo:línea | Caso (nombre exacto) | Qué afirma | Dónde | AC |
|---|---|---|---|---|---|
| LT-20 | `test/simulated_ble_service_test.dart:13` | `emite connected y adapter on al primer listener, y datos cada segundo` | Al primer oyente: estados [conectado], adaptador [encendido], estado conectado y nombre `Remo simulado`; en 3 s, 3 lecturas, todas con pulso 0 ("el pulso llega solo por el pulsómetro simulado"), y la última distancia mayor que la primera | [dotnet] | AC-BLE-15-01, -02, -03 |
| LT-21 | `test/simulated_ble_service_test.dart:39` | `simulateDisconnect emite disconnected y reconecta a los 3 s` | A los 2 s: estados [conectado, desconectado] y ninguna lectura; a los 3 s: [conectado, desconectado, conectado] | [dotnet] | AC-BLE-15-06 |
| LT-22 | `test/simulated_ble_service_test.dart:64` | `disconnect no reconecta solo; startScan reconecta` | Después de desconectar y 10 s: desconectado y sin nombre; buscar → conectado y el último estado avisado es conectado | [dotnet] | AC-BLE-07-03, AC-BLE-15-04, -05 |
| LT-23 | `test/simulated_heart_rate_service_test.dart:10` | `startScan emite el sensor sintético y connect emite bpm cada segundo` | Al inicio: desconectado, sin recordado. Buscar: estados [buscando] y un único resultado con nombre `Pulsómetro simulado` e id `SIM:HRM`. Conectar: conectado con ese nombre. Con 2 pasos del simulador y 3 s: 3 pulsos, el último = pulso del simulador (> 0) = valor actual. Al cerrar, sin temporizadores | [dotnet] | AC-HRM-04-01, AC-HRM-15-02, -04, -05, -11 |
| LT-24 | `test/simulated_heart_rate_service_test.dart:49` | `el escaneo expira a disconnected si no se conecta` | A los 9 s sigue buscando; a los 10 s, desconectado; al cerrar, sin temporizadores | [dotnet] | AC-HRM-15-02 |
| LT-25 | `test/simulated_heart_rate_service_test.dart:62` | `simulateDisconnect cae y reconecta a los 3 s; disconnect no reconecta` | Conectado, 1 pulso en 1 s. Caída: reintentando = sí, recordado = `Pulsómetro simulado` ("mientras reconecta la tarjeta muestra 'reconectando…'"), último pulso 0. A los 2 s: estados [conectado, desconectado]; a los 3 s: conectado, sin reintentar, sin recordado. Desconectar y 10 s: desconectado, sin nombre. Sin temporizadores | [dotnet] | AC-HRM-04-04, AC-HRM-15-06, -08, -11 |
| LT-26 | `test/simulator_overlay_test.dart:17` | `el panel se abre fuera del Navigator y + sube los watts` | Con la capa montada por encima de la navegación, como en la app: antes de abrir no se ve `150 W`; al tocar la llave se ven `150 W` y `24 spm`; + vatios → objetivo 160 y `160 W`; + ritmo → 25; + hora → aparece `Hora real`; `Simular desconexión` sin error; con el pulsómetro simulado conectado, `Simular caída del pulsómetro` sin error, lo deja desconectado y reintentando; `Fuerte` → 280 W y 30 spm; tocar `Remando` lo apaga; cerrar → ya no se ve `280 W` | [Editor-PM] | AC-BLE-16-02, -04 a -09, AC-HRM-15-08 |
| LT-27 | `test/simulator_overlay_test.dart:90` | `en una ventana baja el botón de cerrar sigue accesible` | En 800 × 360 a densidad 1: el panel abre (se ve `150 W`) y su botón de cerrar lo cierra (deja de verse) | [Editor-PM] | AC-BLE-16-03, -12 |
| LT-28 | `test/heart_rate_card_test.dart:83` | `sin sensor → buscar → conectar → bpm → desconectar` | Remo simulado conectado, idioma es: `PULSÓMETRO`, `Sin sensor`, pista con `Heart for Bluetooth`; hay que desplazarse hasta `Buscar`; buscar → `Buscando sensores...` y `Pulsómetro simulado`; tocar la fila → conectado, `Pulsómetro simulado` y `--` ("todavía sin lectura"); 1 s → se ve el pulso publicado (> 0) y no `--`; `Desconectar` → `Sin sensor` | [Editor-PM] | AC-HRM-13-09, AC-BLE-11-07 |
| LT-29 | `test/heart_rate_card_test.dart:121` | `los escaneos del remo y del pulsómetro se excluyen` | Con el remo desconectado (vista de búsqueda), `Buscar dispositivos` habilitado; al buscar pulsómetro, deshabilitado ("mientras el pulsómetro escanea no se puede escanear el remo"); al detener esa búsqueda, habilitado | [Editor-PM] | AC-HRM-13-08, AC-BLE-05-03 |
| LT-30 | `test/heart_rate_card_test.dart:142` | `en landscape la vista de escaneo no desborda` | En 640 × 360 a densidad 1, con el remo desconectado: sin desbordes; `Buscar` del pulsómetro alcanzable desplazando; al buscar, estado buscando y la fila `Pulsómetro simulado` existe dentro del área desplazable de la tarjeta, sin desbordes ("la lista de sensores tampoco desborda") | [Editor-PM] | AC-HRM-13-10, AC-HRM-13-01 |
| LT-31 | `test/heart_rate_card_test.dart:161` | `mientras el remo escanea no se puede buscar pulsómetro` | Con el remo de prueba: `Buscar` del pulsómetro habilitado; con el remo buscando, deshabilitado; conectando, deshabilitado; desconectado, habilitado | [Editor-PM] | AC-HRM-13-04, -08, AC-BLE-10-02 |
| LT-32 | `test/heart_rate_provider_test.dart:54` | `refleja escaneo, conexión, bpm y desconexión del servicio` | Inicio: desconectado, sin recordado, sin resultados. Buscar: buscando y 1 resultado. Conectar: nombre "conectando" = `Pulsómetro simulado` ("mientras conecta se muestra el nombre elegido"); conectado, nombre conectado = `Pulsómetro simulado`, nombre "conectando" vacío. 1 s: pulso = simulador (> 0). Caída: no conectado, reintentando, recordado; 3 s: conectado, sin reintentar. Desconectar: no conectado, pulso 0; más de 3 avisos; sin temporizadores | [dotnet] | AC-HRM-12-01, -02, AC-HRM-15-08 |
| LT-33 | `test/heart_rate_provider_test.dart:107` | `una excepción del servicio queda en error y se limpia al reintentar` | Con un servicio que falla: conectar → error incompatible, reconocido como tal; `Conectar` (reintentar) → sin error; desconectar no lanza aunque el servicio falle | [dotnet] | AC-HRM-12-03 |
| LT-34 | `test/heart_rate_provider_test.dart:122` | `isIncompatible distingue el sensor incompatible` | El error incompatible → sí; otro error → no | [dotnet] | AC-HRM-12-04 |

Total: `simulated_ble_service_test` 3 + `simulated_heart_rate_service_test` 3 + `simulator_overlay_test` 2 + `heart_rate_card_test` 4 + `heart_rate_provider_test` 3 = **15**. Los 9 de [dotnet] también corren en Unity EditMode (AC-BASE-08).

---

## 7. Capturas y videos

### 7.1 Reglas

- Las renderiza **Unity** en el Editor con la UI real; no son maquetas. El arnés de capturas trabaja en modo edición, con la misma UI, la misma configuración de pantalla y la misma composición de colores que la app; las pruebas de UI corren en PlayMode (D-24, aceptada). Condición: una prueba PlayMode confirma que el árbol y la geometría de una pantalla coinciden con su captura (riesgo R2-09 de 02-tech-design).
- Se guardan en `docs/migration/iterations/02-dispositivo-simulador/snapshots/` como `CAP-nn-<tema>.png` (las tomas de una misma captura, con letra: `CAP-04a-…`, `CAP-12b-…`) y `VID-nn-<tema>.mp4`, junto con `manifest.json` (datos de cada toma: tamaño, densidad, idioma, estado, sondas y auditoría) e `INDEX.md` (la misma información en una tabla); el REPORT de la iteración enlaza `INDEX.md` (D-06, aceptada).
- Cada captura **representa el tamaño en dp** que indica, con su densidad, no la del monitor de la PC: en la de 411 × 914 dp la grilla tiene que tener 2 columnas.
- Salvo que diga otra cosa: idioma español, modo simulador, simulador sin ruido, reloj de prueba, fuentes e íconos definitivos si el usuario los descargó (si no, se aclara "a medio vestir").
- QA verifica en cada una lo de la columna "Qué tiene que verse" y el Designer revisa el aspecto; los píxeles de color se leen en el centro de los trazos.

### 7.2 Tamaños

| Nombre | px | Densidad | dp | Para qué |
|---|---|---|---|---|
| **TV** teléfono vertical | 1080 × 2400 | 2,625 | 411 × 914 | caso típico (2 columnas) |
| **TH** teléfono horizontal | 2400 × 1080 | 2,625 | 914 × 411 | 3 columnas en el teléfono |
| **L640** horizontal chico | 640 × 360 | 1 | 640 × 360 | el del test legacy (2 columnas, sin desbordes) |
| **P800** ventana baja | 800 × 360 | 1 | 800 × 360 | el del test legacy del panel |
| **W1280** ventana de Windows | 1280 × 720 | 1 | 1280 × 720 | ventana inicial de Windows (3 columnas) |

### 7.3 Lista

| ID | Tamaño | Estado (cómo se llega) | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** | TV | Dispositivo; remo simulado conectado; 60 s a 150 W / 24 spm; pulsómetro sin conectar | Barra superior `Dispositivo` y, a la derecha, `Desconectar` en rojo claro con ícono de Bluetooth tachado. Cabecera centrada: ícono de remo, `Remo simulado`, insignia verde `Conectado`. Grilla de **2 columnas**: `SPLIT 500m` `02:13` (naranja), `SPM` `24.0` (cian), `VATIOS` `150` (amarillo), `DISTANCIA` `226m` (celeste), cada una con su ícono y borde superior de color; **sin** tarjeta PULSO. Debajo, tarjeta `PULSÓMETRO` en `Sin sensor` con la pista y `Buscar`. Cinta `SIMULADOR` arriba a la izquierda; botón de la llave abajo a la derecha, sobre la barra. Barra con 4 destinos, Dispositivo elegido (píldora, `bluetooth_connected`). Fondo `#0D1B2A`, tarjetas `#1A2E45` |
| **CAP-02** | TV | Igual que CAP-01, con el sistema en inglés | `Device`, `Disconnect`, `Remo simulado` (fijo), `Connected`, `SPLIT 500m`, `SPM`, `WATTS`, `DISTANCE`, `HEART RATE SENSOR`, `No sensor`, la pista en inglés con `Heart for Bluetooth on Wear OS.`, `Search`; barra `Device` · `Workout` · `Routines` · `History`. La cinta sigue diciendo `SIMULADOR` |
| **CAP-03** | L640 | Igual que CAP-01 | **2 columnas** (proporción 1,3); tarjetas enteras, sin cortes; el contenido se desplaza (la tarjeta del pulsómetro queda abajo, fuera de la vista inicial); nada desborda; cinta y llave visibles |
| **CAP-04** (2 tomas) | W1280 | Igual que CAP-01, con el pulsómetro simulado conectado (60 s). a) Dispositivo sin desplazar; b) Dispositivo desplazada hasta el final | En 1280 × 720 la tarjeta del pulsómetro empieza en y ≈ 667 dp (56 + 16 + 8 + 32 + 24 + 2 × 253,3 + 12 + 12) y la barra de pestañas ocupa 656-720: no entra en la primera vista (en el legacy, con barra de 80, tampoco). **a)** **3 columnas** (1,6): cabecera; `SPLIT 500m` `02:13`, `SPM` `24.0`, `VATIOS` `150` en la primera fila y `DISTANCIA` `226m` sola a la izquierda en la segunda; la tarjeta del pulsómetro queda debajo de la barra (fuera de la primera vista); cinta, llave y barra. **b)** `DISTANCIA` entera arriba y, debajo, la tarjeta del pulsómetro **entera y por encima de la barra**, a todo el ancho: `PULSÓMETRO`, corazón pleno, `Pulsómetro simulado`, `142` en `#EF476F` con `bpm` a su lado en el matiz del pulso, en la misma línea de base, y `Desconectar` a la derecha; debajo, el aire de 64 dp (AC-BLE-11-07). Las dos juntas son comparables con `docs/image-01.png` del legacy (tomada en una ventana de 1600 × 1080 px, donde todo entra) (DN-22) |
| **CAP-05** | TH | Igual que CAP-01 | **3 columnas** en el teléfono horizontal; nada desborda; la barra de pestañas abajo |
| **CAP-06** | TV | Remo de prueba conectado que manda 140 de pulso por FTMS | **5 tarjetas** en 2 columnas: `SPLIT 500m`, `SPM` / `VATIOS`, `DISTANCIA` / `PULSO` `140` en `#EF476F` con ícono de corazón |
| **CAP-07** | TV | Remo de prueba: split 6001 s, 40,5 spm, 1999 W, 123 456 m | `100:01`, `40.5`, `1999`, `123456m`: los que no entran, **achicados**; ninguno cortado ni fuera de su tarjeta |
| **CAP-08** | TV | Remo desconectado con `Desconectar` (es la misma vista que durante una caída simulada) | Barra superior **sin** `Desconectar`; botón `Buscar dispositivos` habilitado con su ícono; estado vacío: ícono tachado apagado, `No se encontraron dispositivos` y la ayuda en dos líneas al 54 %; tarjeta del pulsómetro al pie |
| **CAP-09** | L640 | Remo desconectado; pulsómetro buscando | Sin desbordes. `Buscar dispositivos` **deshabilitado** (38 %) porque el pulsómetro busca; la tarjeta en su área desplazable con `Buscando sensores...` y la fila `Pulsómetro simulado` (visible desplazando el área de la tarjeta) |
| **CAP-10** | TV (recorte de la tarjeta) | Pulsómetro sin conectar | Corazón al 40 %; `PULSÓMETRO`; `Sin sensor`; la pista completa (`Un smartwatch (Wear OS, Apple Watch) necesita una app que transmita el pulso por Bluetooth, p. ej. Heart for Bluetooth en Wear OS.`) al 54 %; `Buscar` tonal con ícono |
| **CAP-11** | TV (recorte) | Pulsómetro buscando | Spinner + `Buscando sensores...`; una fila: corazón sin relleno rosa, `Pulsómetro simulado`, `SIM:HRM` chico, flecha a la derecha; sin botones |
| **CAP-12** | TV (2 recortes) | Pulsómetro recién conectado y 60 s después | a) corazón pleno, `Pulsómetro simulado`, `--` `bpm`, `Desconectar`; b) igual con `142` |
| **CAP-13** | TV (recorte) | Caída simulada del pulsómetro (dentro de los 3 s) | `Pulsómetro simulado · reconectando...`; `Conectar` deshabilitado, `Buscar otro` y `Olvidar` habilitados; corazón al 40 % |
| **CAP-14** | TV (4 recortes) | Pulsómetro de prueba | a) `Conectando a Polar H10 7E1A2B3C...` con spinner y sin botones; b) `Polar H10 7E1A2B3C · no encontrado` con `Conectar` habilitado; c) `Sin sensor` + `No es un pulsómetro compatible (sin servicio Heart Rate)` en rojo; d) `Sin sensor` + `Error al conectar: …` con un texto técnico largo, cortado en 3 líneas con "…" |
| **CAP-15** | TV (recorte) | Remo de prueba buscando; pulsómetro sin conectar | `Buscar` del pulsómetro deshabilitado (38 %) |
| **CAP-16** | P800 | Panel recién abierto sobre Dispositivo | Panel de 280 dp anclado abajo a la derecha; `Simulador` y la X **dentro** de la ventana; `Suave` `Medio` `Fuerte`; `Watts` − `150 W` +; `Ritmo` − `24 spm` +; `Hora escena` − `real` +; el resto alcanzable desplazando dentro del panel; el botón de la llave no se ve |
| **CAP-17** | P800 | Panel desplazado al final, con la hora forzada en `14:00` | `Hora escena` `14:00`, botón `Hora real`, interruptor `Remando` encendido, `Simular desconexión` (ícono de enlace roto) y `Simular caída del pulsómetro` (ícono de corazón roto) |
| **CAP-18** | TV | Panel abierto sobre Dispositivo | El panel entra entero, sin desplazar; cinta visible; estética de la app (superficie, tipografía e íconos de la app, no controles genéricos) |
| **CAP-19** | TV | Panel con `500 W`, `40 spm`, hora `0:00` y `Remando` apagado; Dispositivo detrás | Panel: `500 W`, `40 spm`, `0:00`, `Remando` apagado. Grilla detrás: `--:--`, `0.0`, `0` y la distancia congelada |
| **CAP-20** | TV (4 recortes de la barra) + 1 en inglés | Cada destino elegido | Píldora detrás del ícono elegido; Dispositivo con `bluetooth_connected`, Workout con `timer` relleno, Rutinas y Historial con su mismo ícono; etiquetas siempre visibles; el elegido resaltado; barra translúcida sobre el fondo |
| **CAP-21** | TV (3) | Workout, Rutinas e Historial | Título `Entrenamiento`, `Rutinas`, `Historial`; en el centro, el ícono de la pestaña grande y apagado; **ningún** otro texto |
| **CAP-22** | W1280 | **Build normal** (sin simulador) | Vista de búsqueda con el estado vacío; tarjeta en `Sin sensor`; **sin** cinta ni llave; título de ventana `RowMate` (captura de la ventana entera) |
| **CAP-23** | cualquiera | Muestra de medición (AC-BASE-05) | Con las dos composiciones lado a lado (la de la app y la directa): un rectángulo de `glass.fallback` sobre blanco y otro de `text.secondary` sobre `surface.card`, con el valor del píxel leído anotado al pie de cada uno |
| **CAP-24** | TV | Letra del sistema en 1,3×, remo y pulsómetro conectados | Textos más grandes, nada cortado ni fuera de su tarjeta; los valores, achicados si hace falta |
| **CAP-25** | TV | Configuración de prueba con Strava | Barra con **5** destinos; `Perfil` al final con `person`; Historial sigue cuarto |
| **VID-01** | TV, 10 s | Remo y pulsómetro conectados, a 150 W y 24 spm, desde los 60 s | En cada lectura, **solo en las tarjetas cuyo valor cambió**: el rodillo de los dígitos que cambian (se asienta antes de 180 ms) y el borde superior que se enciende del 60 % al 100 % y vuelve (antes de 300 ms). Con el simulador sin ruido eso es `DISTANCIA` cada segundo; `02:13`, `24.0` y `150` quedan quietos y sin brillo; el número del pulsómetro rueda solo si cambia, y su borde nunca destella. Corazón latiendo a 142 por minuto (≈ 23-24 latidos en 10 s), sin tirones en el segundo en que llega el dato; ningún número intermedio inventado |
| **VID-02** | TV, 10 s | Recorrido por las 4 pestañas | La píldora se desliza al destino; el contenido entra con un fundido corto; la respuesta al toque es inmediata |
| **VID-03** | TV, 8 s | `Simular desconexión` y vuelta | La vista conectada pasa a la de búsqueda y vuelve con transiciones suaves, sin saltos; al volver, ceros y al segundo los valores, con la distancia siguiendo |

---

## 8. Qué se verifica dónde

| Dónde | Qué |
|---|---|
| **[dotnet]** | Servicios simulados con reloj de prueba (AC-BLE-15, AC-HRM-15, AC-HRM-04, AC-BLE-07-02 a -04, AC-BLE-08-01); estado de presentación del pulsómetro (AC-HRM-12); reglas del panel: presets, topes, hora y alto máximo (AC-BLE-16-03, -05 a -08, -10); reglas de pantalla puras: qué vista se ve, columnas y proporción por ancho, lista de tarjetas con PULSO, cuerpo y botones de la tarjeta del pulsómetro (AC-BLE-11-01, -04, -05, AC-HRM-13-03 a -05); lectura → textos (AC-BLE-11-09, -10); composición de servicios por variante (AC-APP-01-02, AC-BLE-13-03); colores de una sola fuente (AC-APP-06-02); los 9 casos legacy de servicios y estado (LT-20 a LT-25, LT-32 a LT-34) |
| **[Editor-EM]** | Abrir el proyecto (AC-BASE-10); todas las pruebas del Core dentro de Unity, incluidas las del simulador (AC-BASE-08, AC-BASE-01 en el Editor); activación del modo simulador y el rechazo de un build de distribución con simulador (AC-BLE-13-01) |
| **[Editor-PM]** | La UI montada: shell y barra (AC-APP-01, AC-APP-02), tema y accesibilidad (AC-APP-05-04, -05, -07, -08), Dispositivo (AC-BLE-07, AC-BLE-11, AC-BLE-05), tarjeta del pulsómetro (AC-HRM-13), panel (AC-BLE-16), idioma en pantalla (AC-APP-09-04, -05), build normal sin Bluetooth (AC-BLE-13-05); los 6 casos legacy de UI (LT-26 a LT-31) |
| **[Captura]** | CAP-01 a CAP-25 y VID-01 a VID-03 (§7); colores exactos (AC-APP-05-02, AC-APP-06-01); medición R-T1 (AC-BASE-05) |
| **[Device-Win]** | Build normal y build de desarrollo con simulador: nombre, título, versión, carpeta `LocalLow`, convivencia con el legacy, sin simulador en el normal, siempre oscuro, foco de teclado, tamaño con la escala de Windows, nada se guarda al reiniciar, colores translúcidos en la pantalla del build (AC-APP-01-01, -03, -06, -07, AC-APP-05-01, -07, -09, AC-BLE-13-02, -05, AC-BLE-16-11, AC-APP-09-04, -06, AC-APP-16-01 a -03, AC-BASE-05). IL2CPP (AC-BASE-01), solo con el módulo |
| **[Device-And]** | Solo con el módulo de Android y el teléfono: etiqueta, versión, paquete, mínimo 8.0 y 64 bits, convivencia, "Atrás" cierra, rotación, idioma en vivo y lista completa, letra 1,3×, reducir movimiento, siempre oscuro (AC-APP-02-06, -07, AC-APP-05-01, -05, -08, -09, AC-APP-09-04 a -06, AC-APP-16-01 a -04, AC-BASE-01) |
| **[QA]** | Descarte de `MetricCard` / `IntervalTile` (AC-APP-08-01); una sola tarjeta (AC-APP-08-02); perfiles de distribución sin simulador y build normal sin su código (AC-BLE-13-01, -02); las pantallas no saben del modo (AC-BLE-13-04) |
| **[CI]** | `ci-unity.yml` (AC-APP-19-05), opcional |

---

## 9. Checklist de paridad

| # | Comportamiento del legacy | Legacy | Test legacy | AC |
|---|---|---|---|---|
| 1 | Abre en Dispositivo (índice 0); la pestaña no se recuerda | `lib/main.dart:124` | — | AC-APP-01-01 |
| 2 | Servicios reales o simulados; los simulados comparten el atleta | `main.dart:51-54` | — | AC-APP-01-02, AC-BLE-13-03 |
| 3 | El remo real no se reconecta al abrir; el simulado arranca conectado | `main.dart:51`; `ble_service.dart:43`; `simulated_ble_service.dart:17-21` | `simulated_ble_service_test.dart:13` | AC-APP-01-03, AC-BLE-15-02 |
| 4 | El pulsómetro arranca activo; su fallo va solo al registro | `main.dart:68-79` | — | AC-APP-01-04 |
| 5 | Las pestañas viven en paralelo y conservan su estado | `main.dart:126-143` | — | AC-APP-01-05, AC-APP-02-04 |
| 6 | Título `RowMate`, sin cinta de depuración | `main.dart:94-95` | — | AC-APP-01-06 |
| 7 | Al cerrar se liberan los servicios, sin temporizadores vivos | `main.dart:60-79` | `simulated_*_service_test.dart` (los 6), `heart_rate_provider_test.dart:54` | AC-APP-01-08, AC-HRM-15-11 |
| 8 | 4 destinos en orden, etiquetas e íconos (elegidos y no) | `main.dart:150-170` | — | AC-APP-02-01 |
| 9 | Perfil solo con Strava; Historial fijo en el índice 3 | `main.dart:126-134`, `:141`, `:171-176` | — | AC-APP-02-02 |
| 10 | Selección inmediata; etiquetas siempre visibles; píldora | `main.dart:144-149` | — | AC-APP-02-03 |
| 11 | El ícono de Dispositivo indica selección, no conexión | `main.dart:151-155` | — | AC-APP-02-05 |
| 12 | "Atrás" en el shell cierra la app | `main.dart:142-179` | — | AC-APP-02-06 |
| 13 | Rotar no reinicia la pantalla | `android/app/src/main/AndroidManifest.xml:30` | — | AC-APP-02-07 |
| 14 | Siempre oscuro | `lib/shared/theme.dart:7-12` | — | AC-APP-05-01 |
| 15 | Fondo `#0D1B2A`, tarjetas `#1A2E45` radio 16 sin sombra, barra superior blanca sin sombra | `theme.dart:13-23` | — | AC-APP-05-02 |
| 16 | Letra del sistema; los valores grandes se achican | `device_screen.dart:321-323`; `heart_rate_card.dart:163-166` | — | AC-APP-05-05 |
| 17 | Foco y activación por teclado en escritorio | (por defecto en escritorio) | — | AC-APP-05-07 |
| 18 | Tamaño físico según la escala del sistema | `windows/runner/main.cpp:27-33` | — | AC-APP-05-09 |
| 19 | Paleta de métricas | `theme.dart:32-40` | — | AC-APP-06-01 |
| 20 | `MetricCard` e `IntervalTile` sin uso | `lib/shared/widgets/*` | — | AC-APP-08-01 |
| 21 | `Desconectar` solo con el remo conectado, rojo claro, ícono de 18 | `device_screen.dart:20-31` | — | AC-BLE-07-01 |
| 22 | Desconectar sin confirmar; no se reconecta | `ble_service.dart:304-313`; `simulated_ble_service.dart:74-78` | `simulated_ble_service_test.dart:64` | AC-BLE-07-02 a -04 |
| 23 | Vista conectada solo con el remo conectado | `device_screen.dart:33-38` | — | AC-BLE-11-01 |
| 24 | Cabecera: ícono 32, nombre (`Rower` si no hay), insignia verde | `device_screen.dart:92-118` | `heart_rate_card_test.dart:83` | AC-BLE-11-02 |
| 25 | Métricas en orden, con etiqueta, ícono y color | `device_screen.dart:252-259` | — | AC-BLE-11-03 |
| 26 | PULSO solo con el pulso FTMS > 0; el del sensor nunca en la grilla | `device_screen.dart:257-258` | — | AC-BLE-11-04 |
| 27 | 2/3 columnas (grilla > 600 dp), proporción 1,3/1,6, separación 12 | `device_screen.dart:87-89`, `:261-276` | — | AC-BLE-11-05 |
| 28 | Tarjeta de métrica: borde superior de 3, valor de 52 que se achica | `device_screen.dart:288-335` | — | AC-BLE-11-06 |
| 29 | Debajo, la tarjeta del pulsómetro; todo se desplaza | `device_screen.dart:87-136` | `heart_rate_card_test.dart:83` | AC-BLE-11-07 |
| 30 | Sin calorías, tiempo ni paladas | `device_screen.dart:252-259` | — | AC-BLE-11-08 |
| 31 | Ceros al conectar hasta la primera lectura | `device_provider.dart:38-42`, `:64-67` | — | AC-BLE-11-09 |
| 32 | Textos de la grilla (`24.0`, `149`, `652m`) | `device_screen.dart:253-258` | — | AC-BLE-01-05, AC-BLE-11-10 |
| 33 | Simulador solo con el flag de compilación; configuración de arranque propia | `lib/core/dev/dev_config.dart:1-5`; `.vscode/launch.json` | — | AC-BLE-13-01, -02 |
| 34 | Las pantallas no saben si es simulado; capa encima de todo | `main.dart:51-54`, `:108-110` | `simulator_overlay_test.dart:17` | AC-BLE-13-04 |
| 35 | Sin Bluetooth LE, "Buscar" no hace nada | `ble_service.dart:65-67`; `heart_rate_service.dart:113` | — | AC-BLE-13-05 |
| 36 | `Remo simulado`, nombre solo conectado | `simulated_ble_service.dart:11`, `:59-60` | `simulated_ble_service_test.dart:13` | AC-BLE-15-01 |
| 37 | Adaptador siempre encendido | `simulated_ble_service.dart:22-26`, `:55` | `simulated_ble_service_test.dart:13` | AC-BLE-15-02 |
| 38 | Una lectura por segundo con pulso 0 | `simulated_ble_service.dart:88-97` | `simulated_ble_service_test.dart:13` | AC-BLE-15-03 |
| 39 | Buscar / Conectar reconectan al instante | `simulated_ble_service.dart:62-71` | `simulated_ble_service_test.dart:64` | AC-BLE-15-04 |
| 40 | Caída simulada de 3 s | `simulated_ble_service.dart:81-86` | `simulated_ble_service_test.dart:39` | AC-BLE-15-06 |
| 41 | Sin avisos repetidos | `simulated_ble_service.dart:91` | — | AC-BLE-15-07 |
| 42 | Sin paquetes crudos ni lista | `simulated_ble_service.dart:34-35` | — | AC-BLE-15-08 |
| 43 | La física no se reinicia al reconectar | `rowing_simulator.dart:45-54`; `simulated_ble_service.dart:93-96` | — | AC-BLE-15-09 |
| 44 | Durante la caída: vista de búsqueda vacía, sin "reconectando" del remo | `device_screen.dart:419-442` | — | AC-BLE-15-10, AC-BLE-08-01 |
| 45 | Cinta `SIMULADOR` naranja, arriba a la izquierda, encima de todo | `simulator_overlay.dart:33-36`; `main.dart:108-110` | `simulator_overlay_test.dart:17` | AC-BLE-16-01 |
| 46 | Llave a 16 de la derecha y 96 de abajo; el panel abre ahí | `simulator_overlay.dart:40-49` | `simulator_overlay_test.dart:17` | AC-BLE-16-02 |
| 47 | Panel de 280, alto máximo con mínimo 120 y desplazamiento | `simulator_overlay.dart:83-95` | `simulator_overlay_test.dart:90` | AC-BLE-16-03, -12 |
| 48 | Contenido y orden del panel | `simulator_overlay.dart:101-166` | `simulator_overlay_test.dart:17` | AC-BLE-16-04 |
| 49 | Presets 100/20, 180/24, 280/30 | `simulator_overlay.dart:10-14` | `simulator_overlay_test.dart:17` | AC-BLE-16-05 |
| 50 | Vatios ±10 en 30-500; ritmo ±1 en 14-40 | `simulator_overlay.dart:125-138`; `rowing_simulator.dart:20-26` | `simulator_overlay_test.dart:17`; `rowing_simulator_test.dart:54` | AC-BLE-16-06 |
| 51 | Hora escena `real` / `<h>:00`, ±1 h con vuelta, `Hora real` | `simulator_overlay.dart:74`, `:139-150`; `scene_settings.dart:19-24` | `simulator_overlay_test.dart:17` | AC-BLE-16-07 |
| 52 | `Remando` | `simulator_overlay.dart:151-156` | `simulator_overlay_test.dart:17` | AC-BLE-16-08 |
| 53 | Simular desconexión y caída del pulsómetro | `simulator_overlay.dart:157-166` | `simulator_overlay_test.dart:17` | AC-BLE-16-09 |
| 54 | Nada del panel se guarda | `scene_settings.dart:14`; `simulator_overlay.dart:29` | — | AC-BLE-16-11 |
| 55 | Pulso por notificación, sin suavizado | `heart_rate_service.dart:321-331` | `simulated_heart_rate_service_test.dart:10` | AC-HRM-04-01 |
| 56 | "Sin contacto" → 0 | `heart_rate_service.dart:321-326` | — | AC-HRM-04-02 |
| 57 | 0 se ve `--` | `heart_rate_card.dart:171` | `heart_rate_card_test.dart:83` | AC-HRM-04-03 |
| 58 | 0 al cortarse, una sola vez | `simulated_heart_rate_service.dart:132-140` | `simulated_heart_rate_service_test.dart:62` | AC-HRM-04-04 |
| 59 | El pulso no depende del remo | `simulated_heart_rate_service.dart:126-129` | — | AC-HRM-04-05, AC-HRM-15-10 |
| 60 | Estado de presentación y derivados | `heart_rate_provider.dart:20-54` | `heart_rate_provider_test.dart:54` | AC-HRM-12-01 |
| 61 | Nombre mientras conecta | `heart_rate_provider.dart:28`, `:54`, `:65-70` | `heart_rate_provider_test.dart:54` | AC-HRM-12-02 |
| 62 | Errores: se capturan, se borran al empezar y al conectar | `heart_rate_provider.dart:25`, `:58-101` | `heart_rate_provider_test.dart:107` | AC-HRM-12-03 |
| 63 | Incompatible distinto de otros errores | `heart_rate_provider.dart:56` | `heart_rate_provider_test.dart:122` | AC-HRM-12-04 |
| 64 | Buscar borra error y lista | `heart_rate_provider.dart:58-63` | — | AC-HRM-12-05 |
| 65 | Sin actualizar una tarjeta descartada | `heart_rate_provider.dart:91-116` | — | AC-HRM-12-06 |
| 66 | Ubicación de la tarjeta en cada vista | `device_screen.dart:117-123`, `:367`, `:472-474` | `heart_rate_card_test.dart:83`, `:142` | AC-HRM-13-01 |
| 67 | Título en mayúsculas; corazón pleno solo conectado | `heart_rate_card.dart:26-62` | `heart_rate_card_test.dart:83` | AC-HRM-13-02 |
| 68 | Cuerpo por estado (6 casos) | `heart_rate_card.dart:68-94` | `heart_rate_card_test.dart:83` | AC-HRM-13-03 |
| 69 | Botones deshabilitados por el remo o por reintentar | `heart_rate_card.dart:22-24`, `:108`, `:212`, `:218` | `heart_rate_card_test.dart:161` | AC-HRM-13-04 |
| 70 | Fila: nombre del sistema / anunciado / `Sensor desconocido` | `heart_rate_card.dart:131-145`, `:232-237` | `heart_rate_card_test.dart:83` | AC-HRM-13-05 |
| 71 | Conectado: nombre, número de 52, `bpm`, `Desconectar` | `heart_rate_card.dart:150-196` | `heart_rate_card_test.dart:83` | AC-HRM-13-06 |
| 72 | Error en hasta 3 líneas | `heart_rate_card.dart:52-62` | — | AC-HRM-13-07 |
| 73 | Exclusión de búsquedas remo / pulsómetro | `device_screen.dart:348`, `:397`; `heart_rate_card.dart:22-24` | `heart_rate_card_test.dart:121`, `:161` | AC-HRM-13-08 |
| 74 | En 640 × 360 nada desborda | `device_screen.dart:419-424`, `:472-474` | `heart_rate_card_test.dart:142` | AC-HRM-13-10 |
| 75 | Sin `Cancelar`, sin señal, sin detener búsqueda | `heart_rate_card.dart:68-148` | — | AC-HRM-13-11 |
| 76 | Pulsómetro simulado sin memoria; arranca en `Sin sensor` | `simulated_heart_rate_service.dart:9-11`, `:89-91` | `simulated_heart_rate_service_test.dart:10` | AC-HRM-15-01 |
| 77 | Búsqueda simulada: una fila inmediata, vence a los 10 s | `simulated_heart_rate_service.dart:50-77` | `simulated_heart_rate_service_test.dart:10`, `:49` | AC-HRM-15-02 |
| 78 | Detener la búsqueda simulada | `simulated_heart_rate_service.dart:79-84` | `heart_rate_card_test.dart:121` | AC-HRM-15-03 |
| 79 | Conexión directa, primera lectura al segundo | `simulated_heart_rate_service.dart:118-130` | `simulated_heart_rate_service_test.dart:10` | AC-HRM-15-04 |
| 80 | `Pulsómetro simulado` / `SIM:HRM` / −50 dBm | `simulated_heart_rate_service.dart:13-14`, `:50-63` | `simulated_heart_rate_service_test.dart:10` | AC-HRM-15-02, -05 |
| 81 | Desconectar el simulado | `simulated_heart_rate_service.dart:93-98` | `simulated_heart_rate_service_test.dart:62` | AC-HRM-15-06 |
| 82 | Olvidar no corta una conexión activa | `simulated_heart_rate_service.dart:100-105` | — | AC-HRM-15-07 |
| 83 | Caída simulada del pulsómetro: 0, desconectado, "reconectando", 3 s | `simulated_heart_rate_service.dart:107-116` | `simulated_heart_rate_service_test.dart:62`; `simulator_overlay_test.dart:17`; `heart_rate_provider_test.dart:54` | AC-HRM-15-08 |
| 84 | El pulso sale del simulador del remo | `simulated_heart_rate_service.dart:126-129`; `rowing_simulator.dart:31-37`, `:56-57` | `simulated_heart_rate_service_test.dart:10` | AC-HRM-15-09 |
| 85 | `Buscar otro` cancela la vuelta simulada | `simulated_heart_rate_service.dart:66-77` | — | AC-HRM-15-12 |
| 86 | Botón `Buscar dispositivos`: estados y ayuda | `device_screen.dart:396-417` | `heart_rate_card_test.dart:121` | AC-BLE-05-01 |
| 87 | Estado vacío desde el primer momento | `device_screen.dart:419-442` | — | AC-BLE-05-02 |

---

## 10. Mejoras EXR (cómo se tiene que sentir)

Son agregados: ninguno cambia un dato, un texto ni una regla del legacy. El número que se ve es siempre el real.

1. **Un tablero de instrumentos, no una grilla.** Las tarjetas tienen profundidad (escalones de superficie y una luz suave que baja desde el borde de color), y los números son grandes, de ancho fijo, con la tipografía de tablero deportivo: un `149` que pasa a `151` no hace "bailar" nada.
2. **Cada dato nuevo se nota sin distraer.** Solo los dígitos que cambian ruedan un instante (para arriba si el valor sube, para abajo si baja) y el borde de color de **esa** tarjeta se enciende un momento (del 60 % de reposo al color pleno, y vuelve). Una tarjeta cuyo valor no cambió no se mueve ni brilla. Nunca se muestra un número que no vino del remo, y la animación nunca atrasa el dato.
3. **El pulso es un signo vital.** El corazón de la tarjeta late a la frecuencia real, con su doble golpe, en el rosa del pulso; conectado pero todavía sin lectura (`--`), se queda quieto en color pleno; sin el sensor conectado, quieto y apagado (40 %). Cuando el sensor cae y vuelve, el estado "reconectando" se ve sereno, sin alarmas.
4. **Nada salta.** Pasar de `Sin sensor` a `Buscando`, a `Conectado` o a `reconectando` acomoda la tarjeta con una transición corta, sin saltos de alto. Desconectar el remo "apaga" el tablero y la vuelta lo "enciende".
5. **Una barra de pestañas de vidrio.** Translúcida sobre el fondo, con la píldora de selección que se desliza al destino tocado y un fundido corto del contenido; el toque responde en el acto. Los íconos son nítidos y deportivos, del mismo juego en toda la app.
6. **Un panel de simulador que pertenece a la app.** Con las superficies, la tipografía y los íconos de la app, discreto, sin tapar la información clave; cada ajuste se ve en el tablero y en el pulso al segundo siguiente.
7. **Un arranque prolijo.** Del arranque del sistema al tablero sin destellos blancos ni logos ajenos.
8. **Respeto por quien lo pide.** Con "reducir movimiento", los rodillos son fundidos cortos y el corazón no late; con letra grande, todo entra.
9. **Fluidez.** Mientras algo se mueve, se mueve a la frecuencia completa de la pantalla; la llegada del dato de cada segundo no produce tirones.

Se comprueba con AC-BLE-11-11, AC-HRM-13-12, AC-APP-05-04, -05 y -08, y con VID-01 a VID-03, que revisan QA y el Designer.

---

## 11. Decisiones aplicadas y gate de la It. 2

| Decisión | Qué se aplica | Confirmar en el gate |
|---|---|---|
| Q-03 | Panel del simulador, `Remo simulado`, `Pulsómetro simulado`, `Hora escena`, `Hora real` y `SIMULADOR`: fijos en español en los dos idiomas | No |
| Q-08 | La pestaña `Workout` se llama así también en español | No |
| Q-33 | Sin confirmación al desconectar el remo o el pulsómetro | No |
| Q-40 / R-01 | Simulador nunca activable en distribución; reemplaza remo y pulsómetro a la vez | No |
| Q-57 | `MetricCard` e `IntervalTile` no se migran | No |
| D-ART-03 | Texto terciario al 54 % (legacy 38 %) | No |
| Q-01 | Esta iteración **no suma claves al catálogo**: todos sus textos ya están en las 174 claves o son del simulador (Q-03). `Rower`, `Unknown device` y los errores de búsqueda y conexión, que con el simulador no se ven, se localizan en la It. 10a | No |
| Q-05 | Sin selector de idioma; sigue al sistema | No |
| P-04 (It. 1) | Windows 10 21H1 (build 19043) o más nuevo | **Ya aceptada por el usuario** |
| P-06 (It. 1) | Teclado y menú de copiar y pegar en el idioma del sistema | **Ya aceptada por el usuario** |
| P-01 (It. 1) | Regla de idioma de Flutter con sus dos casos raros | **Ya aceptada por el usuario** |
| P-01 a P-07 de esta spec (§14) | Ver §14 | Solo P-03 (tope de la letra en 1,3×), sin urgencia |
| D-01 (02-tech-design §7.1) | Los colores translúcidos se componen como el legacy desde la It. 2 (opción A adelantada), con respaldo si el build no lo permite (AC-BASE-05, §12.10) | **Confirmada por el PO** en el gate (QA-G2-18). Evidencia: S-0 y las sondas del gate con la configuración real del proyecto (03-qa-harmony §1.2, §9). Falta S-B1 en el build |
| D-06 | Capturas en `snapshots/` con `manifest.json` e `INDEX.md` (§7.1) | **Confirmada** |
| D-09 | "Atrás" en la raíz: Android 12+ deja la app en segundo plano; Android 8-11 la cierra (AC-APP-02-06) | **Confirmada** como cambio respecto del legacy en Android 12-15 (§11.1 DV-06; el motivo del diseño técnico se corrige en §15) |
| D-24 | El arnés de capturas renderiza en el Editor en modo edición; las pruebas de UI, en PlayMode (§7.1) | **Aceptada**, con la condición de R2-09 (§7.1) |
| Aclaración del corazón (02-tech-design §7.1) | Conectado con `--`: pleno y quieto; conectado con pulso: pleno y latiendo; sin conexión: quieto al 40 % (AC-HRM-13-02, -12; §10.3) | **Confirmada**: es el legacy (`heart_rate_card.dart:39-40`) más el latido |
| Color del pulso en etiquetas | Etiqueta e ícono de PULSO y título `PULSÓMETRO` en `metric.hr.hud` (`#FF5A7E`); el número en `metric.hr` (`#EF476F`) (AC-APP-06-01; 03-qa-harmony §7 #8) | **Confirmada** |
| DN-22 | CAP-04 en dos tomas: sin desplazar y al final del desplazamiento (§7.3) | **Adoptada** (QA-G2-04) |
| DV-01 a DV-08 | Diferencias visibles con el legacy, a propósito (§11.1) | **Aceptadas por el PO** (QA-G2-09); DV-04 la aplica el Designer (QA-G2-08) |

### 11.1 Diferencias visibles con el legacy (a propósito)

Paridad primero: estas son las diferencias de color, comportamiento o medida respecto del legacy que la app hace a propósito en la It. 2, cada una con su motivo. No incluye las mejoras EXR de §10, que agregan sin quitar (rodillo, destello, latido, transiciones, profundidad de las tarjetas, barra de vidrio), ni el sistema visual de la app (tipografía Inter y Barlow, juego de íconos; ART_BIBLE), que no cambian datos, textos, colores de métrica ni reglas.

| ID | Legacy (verificado) | App | Motivo | AC |
|---|---|---|---|---|
| DV-01 | Cinta `SIMULADOR` con texto **blanco** `#FFFFFF`, 10,2 dp, peso 900 y una sombra suave (`Banner` de Flutter por defecto: `flutter/packages/flutter/lib/src/widgets/banner.dart:22-30`; `simulator_overlay.dart:33-36`) | Texto oscuro `dev.simulatorText` (`#1A0A00`), sin sombra; tamaño y peso del Designer (04-design-notes §4.8). Misma geometría, mismo naranja y misma ubicación | Blanco sobre `#FF5722` da 3,2:1 (no llega a AA); el oscuro, 6,1:1 | AC-BLE-16-01 |
| DV-02 | Borde superior de las tarjetas en el color de la métrica **al 60 %**, fijo (`device_screen.dart:300`; `heart_rate_card.dart:32`) | Igual en reposo; en las tarjetas del tablero, al 100 % durante el destello de dato nuevo (< 300 ms, solo si el valor cambió). La del pulsómetro, fija al 60 % | Mejora EXR §10.2 ("cada dato nuevo se nota") | AC-BLE-11-06, AC-APP-06-01, AC-HRM-13-02 |
| DV-03 | Ícono y etiqueta de las tarjetas del tablero **al 70 %** del color de la métrica (`device_screen.dart:310`, `:315`); título `PULSÓMETRO` al 70 % del rosa (`heart_rate_card.dart:45`) | Al 100 % del color de la métrica; en el pulso (etiqueta e ícono de PULSO, título `PULSÓMETRO`), `metric.hr.hud` (`#FF5A7E`, el mismo matiz más claro) | Son textos de 12 dp: al 70 % no llegan a 4,5:1 (el split da ≈ 4,0:1 sobre la tarjeta; el pulso, menos). El matiz no cambia | AC-BLE-11-06, AC-APP-06-01, AC-HRM-13-02 |
| DV-04 | Unidad `bpm` en el rosa del pulso **al 70 %** (`heart_rate_card.dart:178-180`) | En el matiz del pulso, con la luminancia que fije el Designer para AA (`metric.hr.hud` da 4,61:1). **No** en blanco | Al 70 % da ≈ 2,5:1. El PO no acepta pasarla a blanco (respuesta a QA-G2-08): la unidad se lee como parte del pulso, como en `docs/image-01.png` | AC-HRM-13-06 |
| DV-05 | `deviceNoResults` en blanco **al 54 %** (`device_screen.dart:430-431`) | `text.secondary` (blanco al 70 %) | Con D-ART-03 la ayuda de abajo sube del 38 % al 54 %: el título sube un escalón para no quedar igual que la ayuda. Respuesta del PO a QA-G2-11: se acepta como cambio | AC-BLE-05-02 |
| DV-06 | "Atrás" en la raíz **termina la app** en Android 8 a 15 (`MainActivity` es una `FlutterActivity`; sin nada que cerrar, Flutter llama a `SystemNavigator.pop`, que termina la actividad: SDK local 3.47.6, `engine/…/io/flutter/plugin/platform/PlatformPlugin.java:614-625`); en Android 16, con su `targetSdk` 36, el "atrás predictivo" queda activo por defecto y el sistema la deja en segundo plano (comportamiento documentado de Android 16; no probado en equipo) | Android 8-11: se cierra; Android 12+: segundo plano (D-09) | Es lo que el sistema hace con la pantalla raíz de cualquier app desde Android 12 y lo que el propio legacy ya hace en Android 16; reabrir es inmediato y vuelve donde estaba. En la It. 2 no hay datos en juego. En la It. 10a se revisa el remo real conectado en segundo plano (pasa lo mismo con el botón de inicio, hoy y en el legacy) | AC-APP-02-06, AC-APP-01-01 |
| DV-07 | Barra de pestañas de 80 dp (M3 `NavigationBar`); llave a 96 dp de abajo | 64 dp; llave a 16 dp sobre la barra (D-12) | ART_BIBLE §3.11; los 16 dp de aire del legacy se conservan | AC-BLE-16-02 |
| DV-08 | Pantalla siempre encendida desde el arranque (`main.dart:42`) | Sin pantalla siempre encendida en la It. 2: se apaga sola (D-08) | Provisorio: F-APP-14 llega en la It. 4a (§13) | — |

Además: texto terciario al 54 % (D-ART-03, arriba), máximo 3 columnas (P-02) y tope de letra en 1,3× (P-03, §14).

---

## 12. Restricciones de producto para el Architect

1. **Determinismo.** Tests y capturas necesitan el simulador sin ruido, el reloj de prueba, el remo y el pulsómetro de prueba (§0.4) y una fuente de prueba para el idioma del sistema, la escala de letra y "reducir movimiento". Ninguno de esos dobles puede llegar a un build instalable.
2. **Capturas en dp.** Cada captura representa el tamaño en dp de §7.2, con su densidad, sin depender del monitor de la PC. Lo mismo para las pruebas PlayMode de tamaño (640 × 360, 800 × 360, 800 × 600).
3. **Tamaño físico como el legacy** (AC-APP-05-09): en Windows, 1 dp = 1 px con la escala al 100 % y escala con la configuración de Windows; en Android, con la densidad del equipo.
4. **R-01.** El modo simulador no se activa en ningún perfil de distribución; un build con simulador que no sea de desarrollo falla. QA lo revisa en los builds.
5. **R-06.** La UI no sabe si los servicios son simulados; lo único propio del simulador es su capa, que queda por encima de todo (también, desde la It. 3 y la 4a, de pantallas apiladas, diálogos y del entrenamiento) y funciona sin depender de ninguna pantalla.
6. **Una vista rota no congela a las otras.** Si una pantalla falla al recibir un cambio de estado, las demás siguen enterándose (QA-V-01, antes de construir la UI).
7. **Las pruebas del Core corren dentro de Unity** (AC-BASE-08), también las del simulador (QA-V-05), y un build IL2CPP da los mismos números (AC-BASE-01).
8. **Identificadores estables para las pruebas**, equivalentes a los del legacy: `hrm-search` (`Buscar` y `Buscar otro`), `hrm-retry`, `hrm-forget`, `hrm-disconnect`, `hrm-device-<id>`, `sim-watts-minus` / `-plus`, `sim-spm-minus` / `-plus`, `sim-hour-minus` / `-plus`, `sim-close`.
9. **Sin claves nuevas en el catálogo** en esta iteración (§11, Q-01); los textos del simulador quedan fijos.
10. **Composición de colores.** El producto espera que los colores translúcidos se vean como en el legacy (composición sRGB) **desde la It. 2** (D-01, aceptada), en la pantalla de la app y en las capturas. Se mide en CAP-23 y en la pantalla de un build de Windows (AC-BASE-05); si el build no lo permite, vale el respaldo de 04-design-notes §8.3 y se corrige antes de la 4a (R-T1).
11. **La app funciona sin las fuentes descargadas** (con la fuente por defecto) y con ellas no cambia ningún comportamiento.
12. **Sin marcas del motor** (P-01): ni logo de arranque ni la marca de build de desarrollo, si el motor lo permite.

---

## 13. Fuera de alcance

- **Remo real** (It. 10a): búsqueda de 10 s con lista, conexión y toma de control, keepalive, reconexión (Q-29), vigilante de 5 s, vistas de Bluetooth apagado y sin permiso (F-BLE-04), banner de error (`Error al escanear` / `Error al conectar`), `Unknown device`, el nombre `Rower` de la cabecera y el diagnóstico oculto (`Show BLE debug`, F-BLE-12). Con el simulador nada de esto se alcanza.
- **Pulsómetro real** (It. 11): memoria `hrm.deviceId` / `hrm.deviceName`, reconexión al arrancar (6 intentos, 10 s), reconexión tras caída cada 3 s, vigilante de 10 s, validación del servicio 0x180D y compatibilidad de sensores.
- **Contenido real** de Workout (It. 4a: aviso de remo no conectado, selector de escenario, rutinas, inicio), Rutinas (It. 3), Historial (It. 9, incluida la recarga al tocar la pestaña, Q-22) y Perfil (It. 12-13). Hasta entonces, el contenido provisorio de AC-APP-02-08.
- Pantallas apiladas, diálogos y avisos (F-APP-03, It. 3).
- Pantalla siempre encendida (F-APP-14, It. 4a).
- Modo inmersivo, orientación por plataforma, tamaño y posición de la ventana de escritorio (F-APP-15, It. 14). La rotación sin reiniciar se verifica igual (AC-APP-02-07).
- Escena: la hora forzada no tiene efecto visible hasta la It. 6a (F-SCN-23).
- Base separada del simulador (F-HIS-20, It. 3) y bloqueo de subidas a Strava (F-STR-18, It. 12).
- Ícono, pantalla de arranque con marca e identificadores definitivos (F-APP-16, It. 14); auditoría de textos fijos (F-APP-11, It. 14).
- Háptica (It. 14).
- Propuestas de los inventarios que cambian comportamiento y quedan para después de la paridad: que el ícono de Dispositivo refleje el estado real del remo; un remero en miniatura en el tablero; un indicador de "sin contacto" del pulsómetro (BKL-11); `Cancelar` en la conexión manual (BKL-10); el modo "remo simulado + pulsómetro real" (BKL-20).

---

## 14. Preguntas abiertas

| ID | Pregunta | Por defecto | Para |
|---|---|---|---|
| P-01 | Un build de desarrollo de Unity puede mostrar la marca "Development Build" y la consola de errores del motor. El legacy nunca muestra marcas de depuración (`debugShowCheckedModeBanner: false`), y el build con simulador tiene que ser de desarrollo (ADR 003) | Se ocultan si el motor lo permite. Si no se puede, se acepta **solo** en los builds de desarrollo con simulador, que nunca se distribuyen; en el build normal no aparece nunca | Architect |
| P-02 | ART_BIBLE §3.1 y §5.1 proponen 4 columnas en tablets (≥ 840 dp). El legacy nunca pasa de 3 | **Paridad**: como máximo 3 columnas (AC-BLE-11-05). Las 4 columnas quedan como propuesta para después de la paridad | Designer |
| P-03 | ART_BIBLE §10.4 pone un tope de 1,3× a la letra del sistema; el legacy no tiene tope | Se adopta el tope de 1,3× (con letra más grande, el resto del diseño no entra), sin perder nada a 1,3× (AC-APP-05-05) | Usuario (sin urgencia) |
| P-04 | ¿Qué hace el build normal en la It. 2, si el Bluetooth real llega en la 10a y la 11? | Se comporta como el legacy en un equipo sin Bluetooth LE (AC-BLE-13-05). Es provisorio | — (decidida por el PO) |
| P-05 | ¿Qué muestran Workout, Rutinas e Historial hasta su iteración? | Título del catálogo + ícono apagado, sin texto nuevo (AC-APP-02-08), para no agregar al catálogo claves que después se borran | — (decidida por el PO) |
| P-06 | ¿Y si Unity no puede sacar la versión `1.0.0` en las propiedades del ejecutable de Windows? | Alcanza con que la app la informe en la primera línea de su registro de diagnóstico (AC-APP-16-01) | Architect |
| P-07 | En Windows, ¿la app tiene que enterarse de un cambio de idioma con la app abierta? | No: Windows pide cerrar la sesión para cambiar el idioma de la pantalla. Se verifica en Android y en el Editor (AC-APP-09-05) | — |

---

## 15. Discrepancias encontradas al especificar

| Dónde | Qué dice | Qué es (verificado en el legacy) | Acción |
|---|---|---|---|
| ROADMAP §2, It. 2 (demo) | "2 columnas en vertical, 3 en horizontal" | Depende del ancho, no de la orientación: más de 640 dp de ventana (grilla > 600 con 20 de margen). En 640 × 360 (el del test legacy) hay **2** columnas; en un teléfono horizontal típico (914 dp), 3 | Fijado en AC-BLE-11-05. **Resuelto en el gate** (QA-G2-18): frase del ROADMAP corregida y anotada en su §8 |
| `rower-ble-device-sim.md` BLE-11.5 | "Con ancho > 600 px la grilla tiene 3 columnas" | Es el ancho de la grilla (ventana − 40 dp), no el de la ventana (`device_screen.dart:87-89`, `:263-264`) | **Resuelto en el gate**: BLE-11.5 corregido |
| ASSET_SLOTS §4.3 | `speed` para "split / SPM (tablero)" | El split del tablero usa `timer_outlined` (`device_screen.dart:253`); el SPM, `speed` (`:254`) | AC-BLE-11-03 usa `timer` para el split. **Pendiente** (Designer) |
| ASSET_SLOTS §4.3 | `favorite`: "relleno con lectura; contorno `@40 %` sin conexión" | El corazón del encabezado es siempre el relleno, pleno si el sensor está **conectado** (con o sin lectura) y al 40 % en otro estado (`heart_rate_card.dart:39-40`); el contorno es el de las filas de la lista (`:138`) | AC-HRM-13-02 y -05. **Pendiente** (Designer) |
| ART_BIBLE §3.1 y §5.1 | Tablet: 3-4 columnas, 4 desde 840 dp | El legacy tiene como máximo 3 | P-02 (paridad) |
| ART_BIBLE §10.4 | Letra del sistema hasta 1,3× | El legacy no tiene tope (app-shell F-APP-05.7) | P-03 |
| ART_BIBLE §2.2 | `state.dangerText` `#FF6B6B` | Ya es `#FF7272` en `Tokens.cs` (04-design-notes §6, QA-V-09) | **Pendiente** (Designer, QA-V-09) |
| `heart_rate_card_test.dart:83` | "La tarjeta queda bajo la rejilla: hay que desplazarse" | Vale para la ventana de prueba de Flutter (800 × 600); en un teléfono vertical de 914 dp de alto puede entrar sin desplazar | AC-HRM-13-09 fija la ventana de 800 × 600 dp para ese paso |
| STATUS.md | It. 1 "esperando el OK del usuario"; P-04 y P-06 "para el gate de la It. 2" | Según el coordinador, el usuario ya aceptó los valores por defecto de Q-01, Q-14, Q-90, R-14/Q-85, Q-92, P-01, P-04 y P-06 | §11. **Pendiente** (quien actualice STATUS) |
| Esta spec, primera versión (AC-BLE-16-01, AC-BLE-11-06, AC-APP-06-01, AC-HRM-13-02) | Cinta "con texto oscuro"; borde, ícono, etiqueta y título "en el color de la métrica", como si fuera paridad | El legacy escribe la cinta en blanco (`Banner`), pone el borde al 60 % y el ícono, la etiqueta y el título al 70 % | **Resuelto en el gate** (QA-G2-09): marcados como diferencias en §11.1 (DV-01 a DV-03) |
| 02-tech-design §7.1, D-09 | "Es lo que el sistema hace con la actividad raíz del legacy en cada versión (Android 12+ no la termina)" | El legacy **sí** termina la app con "Atrás" en Android 8 a 15: `FlutterActivity` maneja el botón y, sin nada que cerrar, termina la actividad; solo en Android 16 (atrás predictivo con `targetSdk` 36) queda en segundo plano | La propuesta se acepta igual, como cambio con motivo (§11.1 DV-06). **Pendiente** (Architect): corregir el "Por qué" de D-09 y ADR 007 |
| 02-tech-design §4.3, fila CAP-04 | Una sola toma sin desplazar, con `142` `bpm` y la sonda del pulso | La tarjeta del pulsómetro arranca en y ≈ 667 dp, bajo la barra (656-720) | CAP-04a y CAP-04b (§7.3, DN-22). **Pendiente** (Architect): partir la fila en el catálogo de capturas |

`git -C ../rowmate-legacy status --porcelain` quedó vacío antes y después de escribir esta spec y antes y después de corregirla por el gate. No se leyó `lib/core/strava/strava_secrets.dart`.

---

## 16. Cambios por gate QA

Correcciones por el gate de armonía de QA ([03-qa-harmony](03-qa-harmony.md), APROBADO CON OBSERVACIONES), 2026-10-27. Cada valor nuevo se volvió a leer en el legacy (`device_screen.dart:87-136`, `:280-335`, `:396-442`; `heart_rate_card.dart:14-66`, `:150-196`; `simulator_overlay.dart:25-50`; `main.dart:38-44`, `:100-112`, `:120-181`; `docs/image-01.png`; `android/app/src/main/AndroidManifest.xml`, `MainActivity.kt`, `build.gradle.kts`) y en el SDK local de Flutter (`widgets/banner.dart:18-30`; motor: `PlatformPlugin.java:614-625`, `FlutterActivity.java:632-660`, `:730-735`, `:940`). No se disputa ningún issue; en D-09 se corrige el motivo, no la decisión.

| Issue | Sev. | Qué cambió | Dónde |
|---|---|---|---|
| QA-G2-04 | mayor | CAP-04 pasa a dos tomas (DN-22): **a)** sin desplazar, para las 3 columnas; **b)** al final del desplazamiento, con la tarjeta del pulsómetro entera sobre la barra. Geometría recalculada: la tarjeta empieza en y ≈ 666,7 dp y la barra ocupa 656-720 (en el legacy, con barra de 80, lo visible termina en 640). La toma b se definió como "al final del desplazamiento" (y no como "desplazar hasta la tarjeta") para que la tarjeta quede siempre por encima de la barra; con la separación de 64 dp bajo la tarjeta (AC-BLE-11-07), en b se ven enteras `DISTANCIA` y la tarjeta del pulsómetro. Referencias a CAP-04 repartidas entre a y b. Falta del Architect: partir la fila CAP-04 de 02-tech-design §4.3 (§15) | §7.3; AC-APP-06-01, AC-APP-08-02, AC-BLE-11-05, AC-BLE-01-05, AC-HRM-13-06, AC-BASE-05 |
| QA-G2-09 | menor | Nueva §11.1 con las diferencias visibles a propósito: cinta con texto oscuro (DV-01), borde al 60 % en reposo y 100 % en el destello (DV-02), ícono, etiqueta y título al 100 % o `metric.hr.hud` en vez del 70 % (DV-03); además `bpm` (DV-04), `deviceNoResults` (DV-05), "Atrás" (DV-06), barra de 64 (DV-07) y pantalla siempre encendida (DV-08). AC-BLE-11-06 dice "al 60 % en reposo y al 100 % en el destello" | §11, §11.1; AC-BLE-16-01, AC-BLE-11-06, AC-APP-06-01, AC-APP-08-02, AC-HRM-13-02, AC-BLE-05-02; §15 |
| QA-G2-10 | menor | VID-01: rodillo y destello **solo en las tarjetas cuyo valor cambió** (con el simulador sin ruido, `DISTANCIA`); `02:13`, `24.0` y `150` quietos y sin brillo; la tarjeta del pulsómetro no destella. §10.2 dice lo mismo | §7.3 VID-01; §10.2 |
| QA-G2-18 | menor | Decididas y reflejadas: **D-01** confirmada (colores como el legacy desde la It. 2; AC-BASE-05 reescrito con las dos composiciones, la lectura en el build y el respaldo; §12.10; CAP-23); **D-06** (`snapshots/`, `manifest.json`, `INDEX.md`, §7.1); **D-09** (AC-APP-02-06 y AC-APP-01-01 reescritos; DV-06; el motivo del diseño técnico se corrige en §15); **D-24** (§7.1, con la condición de R2-09); **aclaración del corazón** (AC-HRM-13-02, -12 y §10.3: conectado con `--`, pleno y quieto); **color del pulso en etiquetas** (`metric.hr.hud`, 03-qa-harmony §7 #8); **frase del ROADMAP** corregida (y BLE-11.5 del inventario) | §7.1, §7.3, §10.3, §11, §12, §15; AC-BASE-05, AC-APP-02-06, AC-APP-01-01, AC-HRM-13-02, -12; `ROADMAP.md` §2 It. 2 y §8; `product/inventory/rower-ble-device-sim.md` BLE-11.5 |
| Alineación (issues de otros) | — | Respuestas del PO donde el issue lo pedía: **QA-G2-08** (Designer): `bpm` va en el matiz del pulso, no en blanco (DV-04); **QA-G2-11** (Designer): `deviceNoResults` al 70 % se acepta (DV-05); **QA-G2-12** (Architect): AC-BLE-11-07 fija los espacios del legacy (24 dp sobre la cabecera, 64 dp bajo la tarjeta del pulsómetro) | AC-HRM-13-06, AC-BLE-05-02, AC-BLE-11-07; §11.1 |

Sin cambios en el alcance ni en los conteos: siguen 125 criterios, 15 casos legacy y 25 capturas (CAP-04 con dos tomas, como CAP-12 y CAP-14) más 3 videos. AC-BASE-05 suma la marca [Device-Win] (§8).
