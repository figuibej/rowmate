# It. 10a — Bluetooth real en Android y Windows: spec de producto

| Campo | Valor |
|---|---|
| Iteración | 10a (`10a-ble-android-windows`), [ROADMAP §It. 10a](../../ROADMAP.md) |
| Incluye | F-BLE-04, F-BLE-05, F-BLE-06, F-BLE-08, F-BLE-09, F-BLE-12 ([rower-ble-device-sim](../../product/inventory/rower-ble-device-sim.md)); F-APP-13 ([app-shell-platform](../../product/inventory/app-shell-platform.md)) |
| También toca | F-BLE-02/03 (paquetes y filtro con equipos reales), F-BLE-07, F-BLE-10, F-BLE-11 (tablero con datos reales), F-APP-14 (pantalla encendida en Android), Q-91 (segundo plano en Android y Windows), QA-V4a-L01 (paso por distancia), F-APP-10 (12 claves nuevas: 240 por idioma) |
| Decisiones | Q-01, Q-04, Q-15, Q-29, Q-31, Q-32, Q-34 a Q-39, Q-47, Q-90, Q-91 ([FEATURE_INVENTORY §4](../../product/FEATURE_INVENTORY.md)); D-ANI-06 ([ANIMATION_BIBLE §3.10](../../design/ANIMATION_BIBLE.md)); ADR [009](../../architecture/adr/009-ble-nativo.md) y [017](../../architecture/adr/017-segundo-plano.md) |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`). Leídos enteros: `lib/core/bluetooth/ble_service.dart`, `scan_filter.dart`, `lib/features/device/device_screen.dart`, `device_provider.dart`, `android/app/src/main/AndroidManifest.xml`. No se leyó `lib/core/strava/strava_secrets.dart` |
| Modo | Entrega rápida, sin debates. Ante una duda, el legacy o un valor por defecto, anotado en una línea (§8) |
| Criterios | **43** (§3), 20 capturas con el adaptador falso (§6) y 2 guiones de demo con el remo (§1) |

---

## 0. Cómo leer esta spec

- Tamaños (**TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720, en dp) y estado de prueba (reloj de prueba, `ROWMATE_DATA_DIR` temporal, sin tocar `prefs.json` ni las bases del usuario): los de la [spec de la 6a §0](../06a-escena-lago/01-po-spec.md). Textos entre comillas invertidas: **exactos al carácter**.
- Marcas de verificación:
  - `[dotnet]`: máquina de estados con el **adaptador falso** y el reloj de prueba.
  - `[Editor]`: vistas en el Editor con el adaptador falso; `[Captura]` = captura de §6.
  - `[Device-Win]`: build normal de Windows con el **adaptador real** de la PC, sin remo: estado del adaptador, búsqueda que no rompe, lista vacía si no hay FTMS cerca. **Nunca** se prende ni se apaga la radio, ni se empareja ni se conecta a nada.
  - `[Device-Android]`: build de Android en un teléfono. **NO VERIFICABLE** hasta que el usuario instale "Android Build Support"; queda para su demo.
  - `[BLE-HW]`: con el AMS-670B (u otro remo FTMS). **NO VERIFICABLE** en el workflow; queda para la demo del usuario (§1).
- Fuentes legacy: `bs` = `lib/core/bluetooth/ble_service.dart`, `dp` = `lib/features/device/device_provider.dart`, `ds` = `lib/features/device/device_screen.dart`, `sf` = `lib/core/bluetooth/scan_filter.dart`, `am` = `android/app/src/main/AndroidManifest.xml`.
- **Adaptador falso de esta iteración** (`K-10`): equipos ficticios, con direcciones inventadas. `K-ROW` = `AMS-670B`, `AA:BB:CC:00:06:70`, anuncia FTMS `0x1826`, tiene Rower Data `0x2AD1` y Control Point `0x2AD9`; `K-ANON` = sin nombre, `AA:BB:CC:00:00:02`, FTMS; `K-BIKE` = `Bici FTMS`, `AA:BB:CC:00:00:03`, FTMS sin Rower Data (Q-35); `K-NOCP` = `Remo sin control`, `AA:BB:CC:00:00:04`, FTMS con Rower Data y sin Control Point; `K-HP` = `Auriculares`, `AA:BB:CC:00:00:05`, **no** anuncia FTMS ni `0x180D`. Paquete de Rower Data de prueba: el del oráculo `ftms.json` de la It. 1. Ninguna prueba, captura ni demo muestra un nombre o una dirección de un equipo real.
- Los textos "El monitor dejó de enviar datos" y "Reconectando con {remo}…" **no** están en el legacy: los agregan Q-31 y Q-34. Los tiempos sí están, y se verificaron uno por uno en el código: 10 s de búsqueda (`bs:65`), 5 s de espera del adaptador (`bs:77`), 15 s de conexión (`bs:144`), 300 ms entre `0x00` y `0x07` (`bs:269`), keepalive de 2 s (`bs:280`), reconexión a los 3 s (`bs:168`) con tope de 10 s (`bs:178`), y watchdog de 1 s y ≥ 5 s (`dp:8`, `:71`, `:75`).
- IDs: siguen la numeración de las iteraciones anteriores para no repetir (AC-BLE-05-04 sigue a -03 de la It. 2, AC-BLE-07-05, AC-BLE-08-02, AC-BLE-11-12, AC-APP-14-05, AC-WKT-03-11); AC-BLE-91 y AC-BLE-SEC son nuevos. `DV-96` a `DV-105` siguen a DV-95.

---

## 1. Objetivo y demo

**Objetivo.** Conectar un remo FTMS real en Android y Windows: estado del adaptador y permisos, búsqueda de 10 s con lista en vivo, conexión con toma de control y keepalive, reconexión que reintenta hasta lograrlo, aviso de monitor apagado, aviso de reconexión sobre el HUD y diagnóstico oculto. Se escribe la capa nativa de Android y Windows, que también va a usar el pulsómetro en la It. 11. El simulador (It. 2) no cambia: lo usan los tests y el build `simulator`.

**Lo que el workflow deja verificado**, sin remo: toda la máquina de estados con el adaptador falso ([dotnet]), todas las vistas ([Editor], §6) y, en esta PC, que el build normal de Windows ve el adaptador encendido, busca 10 s sin romper y muestra la lista vacía o solo equipos FTMS ([Device-Win]).

**Demo del usuario en Windows** (build normal, W1280, español, con el AMS-670B encendido):

1. Abrir RowMate. Dispositivo muestra la vista de búsqueda (el Bluetooth de la PC está encendido) y la tarjeta del pulsómetro.
2. Tocar `Buscar dispositivos`. Durante 10 s se ve `Buscando...` con el spinner; el remo aparece en la lista con su nombre y su dirección. No aparece ningún equipo que no sea FTMS.
3. Tocar el remo: se ven `Conectando…`, `Tomando control del monitor…` y `Listo`. El monitor pasa a modo Bluetooth y el tablero muestra split, SPM, vatios y distancia reales al remar.
4. Bajar el tablero 3 veces (o mantener presionada la cabecera del remo) → `Show BLE debug` → el panel muestra `RECEIVING`, el contador que sube y los bytes crudos.
5. Workout → `Entrenamiento libre`, remar 1 min. Minimizar la ventana 30 s y volver: el monitor sigue en modo Bluetooth y la sesión siguió.
6. Corte breve: alejarse del alcance o apagar y prender el Bluetooth de la PC **a mano** durante unos segundos. Sobre el HUD aparece `Reconectando con AMS-670B…`; al volver, `Reconectado con AMS-670B` y los números siguen.
7. Apagar el monitor: a los 5-6 s se ve `El monitor dejó de enviar datos`. `Terminar` y revisar la sesión en Historial.
8. Anotar para QA (Q-15, Q-31, D-ANI-06, QA-V4a-L01): si los contadores del monitor arrancan en cero al conectar, si el monitor notifica durante una pausa del remero, cada cuánto manda la distancia y en qué momento de la palada sube el contador de paladas. El registro de diagnóstico (`Player.log`) tiene los datos; **no se sube al repo**.

**Demo del usuario en Android** (después de instalar "Android Build Support" y generar el APK; teléfono con Android 12 o más nuevo):

1. Instalar y abrir. Con el Bluetooth del teléfono **apagado**, Dispositivo muestra `Bluetooth apagado` y `Activar Bluetooth`; tocarlo muestra el diálogo del sistema; al aceptarlo, la vista pasa sola a la búsqueda.
2. Tocar `Buscar dispositivos`: aparece el diálogo de "Dispositivos cercanos". Aceptarlo: la búsqueda arranca y lista el remo. (Para ver `Permiso de Bluetooth denegado`: rechazarlo y reabrir la app.)
3. Conectar, remar, ver el diagnóstico (pasos 3 y 4 de Windows), con una vibración sutil al quedar `Listo`.
4. Workout → `Entrenamiento libre`. Bloquear el teléfono 60 s y desbloquearlo: el monitor siguió en modo Bluetooth, el reloj y la distancia siguieron y la pantalla no se apagó mientras la app estaba adelante.
5. Corte breve y monitor apagado (pasos 6 y 7 de Windows).

---

## 2. Historias de usuario

- Como remero quiero que la app me diga si el Bluetooth está apagado o sin permiso, y cómo arreglarlo, para no quedarme sin saber por qué no conecta.
- Como remero quiero encontrar mi remo con un toque y que el monitor quede controlado y mandando datos sin tocar nada en el monitor.
- Como remero quiero que un corte breve se reconecte solo, también en pleno entrenamiento, y que la app me avise si apagué el monitor.
- Como soporte quiero un diagnóstico escondido que muestre si el monitor manda datos y qué manda.

---

## 3. Criterios de aceptación

### F-BLE-04 · Estado del adaptador

- **AC-BLE-04-01 · Bluetooth apagado.** *Dado* el adaptador falso en apagado y el remo sin conectar, *entonces* Dispositivo muestra a pantalla completa, centrados: el ícono de Bluetooth tachado de 64 en rojo, `deviceBtOff` (`Bluetooth apagado` / `Bluetooth is off`) en 18 negrita, `deviceBtOffHint` en 14, gris 54 %, centrado, y el botón `deviceBtEnable` (`Activar Bluetooth` / `Enable Bluetooth`) con ícono de Bluetooth. Esa vista reemplaza al botón de búsqueda, a la lista y a la **tarjeta del pulsómetro**; el banner de error, si lo hay, sigue arriba. Con el remo conectado, apagar el Bluetooth corta la conexión y lleva a esta vista. [Editor] · [Captura] CAP-01, CAP-02 · `ds:346`, `:355-394`; `bs:51-53`
- **AC-BLE-04-02 · Sin permiso.** *Dado* el adaptador falso sin autorización, *entonces* es la misma vista con `deviceBtDenied` (`Permiso de Bluetooth denegado`) y `deviceBtDeniedHint`, y el mismo botón. [Editor] · [Captura] CAP-03 · `ds:347`, `:376`, `:381`
- **AC-BLE-04-03 · Los demás estados y la vuelta.** *Entonces* con el adaptador desconocido (al arrancar), encendiendo, apagando o encendido se ve la vista de búsqueda. *Cuando* el adaptador pasa de apagado a encendido, la vista de estado se transforma en la de búsqueda sin salto (con "reducir movimiento", cambia sin animar) y la búsqueda **no** arranca sola. [Editor] · `dp:14`; `ds:367`
- **AC-BLE-04-04 · "Activar Bluetooth" según la plataforma (Q-32).** *Cuando* se toca `Activar Bluetooth`, *entonces* en Android se muestra el pedido del sistema para encender el Bluetooth (con el permiso negado, se vuelve a pedir el permiso y, si el sistema ya no lo muestra, se abren los ajustes de la app); en Windows se abren los ajustes de Bluetooth del sistema. Nunca aparece un error. [Device-Win] (solo que se abren los ajustes; no se toca la radio) · [Device-Android] · `ds:387`
- **AC-BLE-04-05 · Buscar con el adaptador sin listo.** *Dado* el adaptador desconocido o encendiendo, *cuando* se toca `Buscar dispositivos`, *entonces* la app espera **hasta 5 s** a que esté encendido: si se enciende, la búsqueda arranca; si no, el remo pasa a apagado, la búsqueda no arranca y no hay mensaje. `Buscar` sigue habilitado. [dotnet] · `bs:69-84`
- **AC-BLE-04-06 · Equipo sin Bluetooth LE (Q-47).** *Dado* el adaptador falso "no soportado", *entonces* Dispositivo muestra la misma vista de estado con el ícono tachado, `deviceBleUnsupported` y `deviceBleUnsupportedHint`, **sin** botón y sin la tarjeta del pulsómetro. Reemplaza el "Buscar no hace nada" provisorio de AC-BLE-13-05 (It. 2) y del legacy. [dotnet] · [Editor] · [Captura] CAP-04 · `bs:66-67`
- **AC-BLE-04-07 · El adaptador real de la PC.** *Dado* el build normal de Windows en esta PC (Bluetooth LE encendido), *entonces* Dispositivo abre en la vista de búsqueda, no en la de apagado ni en la de sin soporte, y la app no cambia el estado de la radio. [Device-Win]

### F-APP-13 · Permisos y plataformas

- **AC-APP-13-01 · Manifiesto de Android.** *Entonces* el APK declara exactamente: `BLUETOOTH_SCAN` con `neverForLocation` y `BLUETOOTH_CONNECT` (API 31+); `BLUETOOTH`, `BLUETOOTH_ADMIN` y `ACCESS_FINE_LOCATION` con `maxSdkVersion` 30; `WAKE_LOCK`; y `uses-feature android.hardware.bluetooth_le` con `required="true"`. Mínimo Android 8.0 (API 26), solo ARM64 (Q-90). No pide ubicación en Android 12+. [Editor] (revisión del manifiesto que entra al build) · [Device-Android] · `am:4-19`
- **AC-APP-13-02 · Permiso en Android.** *Dado* un Android 12+ sin el permiso, *cuando* se toca `Buscar dispositivos` por primera vez, *entonces* aparece el diálogo de "Dispositivos cercanos" (en Android 8 a 11, el de ubicación). Concedido, la búsqueda arranca sin otro toque; negado, Dispositivo muestra la vista de AC-BLE-04-02 (no un `Error al escanear`). Al volver de los ajustes con el permiso dado, la vista pasa a la de búsqueda. [Device-Android]
- **AC-APP-13-03 · Windows y los dos builds.** *Entonces* en Windows no hay diálogo de permiso. El build normal usa el Bluetooth real; el build `simulator` sigue usando el remo y el pulsómetro simulados, **no** escanea ni abre la radio, y sus criterios de la It. 2 siguen pasando. [Device-Win] · [dotnet]

### F-BLE-05 · Búsqueda

- **AC-BLE-05-04 · 10 s, con estado visible.** *Cuando* se toca `Buscar dispositivos`, *entonces* se borran el error y la lista anterior, el remo pasa a buscando y la búsqueda dura **10 s**, solo de equipos con el servicio FTMS `0x1826`. Mientras busca, el botón está deshabilitado con un spinner blanco de 16 y `deviceSearching` (`Buscando...`), y debajo va `deviceSearchHint` en 12, gris 54 %, centrado. Buscando sin resultados, no se ve el estado vacío. [dotnet] · [Editor] · [Captura] CAP-05 · `dp:104-114`; `bs:65`, `:86`, `:93-96`; `ds:396-417`
- **AC-BLE-05-05 · Lista en vivo y solo FTMS (F-BLE-03, Q-35).** *Dado* `K-10` anunciándose en este orden: `K-HP`, `K-ROW`, `K-ANON`, `K-BIKE`, `K-ROW` otra vez, *entonces* la lista se actualiza a medida que llegan y queda `AMS-670B`, `Dispositivo desconocido` y `Bici FTMS`, una vez cada uno, en el orden del primer descubrimiento; `Auriculares` **nunca** aparece (también en Windows, donde el filtro lo hace la app). Solo cuentan los resultados de esta búsqueda, no los de una anterior ni los del pulsómetro. [dotnet] · `bs:87`, `:97-109`; `sf:12-20`
- **AC-BLE-05-06 · Fin y búsquedas encimadas (Q-39).** *Entonces* a los 10 s el remo vuelve a desconectado, salvo que ya esté conectando o conectado; la lista queda visible hasta la próxima búsqueda, también después de conectar y desconectar. No hay botón para detener la búsqueda. Conectar no cambia la lista. Si una búsqueda nueva empieza mientras termina una vieja, la vieja no pisa ni el estado ni la lista de la nueva. [dotnet] · `bs:88-90`, `:113-123`; `dp:116`
- **AC-BLE-05-07 · Ítem y estado vacío.** *Entonces* cada ítem es una tarjeta con el ícono de remo en `#00B4D8`, el nombre (o `deviceUnknown` si no tiene), la dirección en 11, gris 38 %, y la flecha a la derecha. Sin resultados y sin buscar (también al abrir la app), se ve el ícono tachado de 48 en gris 24 %, `deviceNoResults` y `deviceNoResultsHint` en 12, gris 38 %, centrado. [Editor] · [Captura] CAP-06, CAP-07 · `ds:419-471`
- **AC-BLE-05-08 · Error al buscar.** *Dado* que el adaptador falso falla al empezar la búsqueda, *entonces* aparece arriba el banner rojo (fondo rojo 15 %, radio 12, texto 13 rojo claro) con `deviceScanError` y el detalle del sistema, y el remo vuelve a desconectado para que `Buscar` no quede deshabilitado. [dotnet] · [Editor] · [Captura] CAP-08 · `dp:110-112`; `ds:355-365`; `bs:118-121`
- **AC-BLE-05-09 · Exclusión con el pulsómetro (Q-36).** *Entonces* `Buscar dispositivos` se deshabilita mientras el pulsómetro busca, y `Buscar` y `Reintentar` del pulsómetro se deshabilitan mientras el remo busca o conecta; que el pulsómetro conecte no frena al remo. Se vuelven a correr LT-29 (`heart_rate_card_test.dart:121`), LT-30 (`:142`, L640 sin desborde) y LT-31 (`:161`) con el remo real sobre el adaptador falso. [Editor] · [Captura] CAP-07 · `ds:348`, `:397`
- **AC-BLE-05-10 · Búsqueda real en la PC.** *Dado* el build normal de Windows y ningún equipo FTMS cerca, *cuando* se toca `Buscar dispositivos`, *entonces* se ve `Buscando...` 10 s, la app no se traba ni cierra, al terminar se ve el estado vacío y en la lista nunca aparece un equipo que no anuncie FTMS, aunque haya otros equipos Bluetooth cerca. Se repite 3 veces seguidas con el mismo resultado. [Device-Win]

### F-BLE-06 · Conexión y toma de control

- **AC-BLE-06-01 · Conectando.** *Cuando* se toca `K-ROW`, *entonces* el remo pasa a conectando: todos los ítems muestran un spinner de 20 en lugar de la flecha y no responden, y `Buscar` está deshabilitado. El intento tiene un tope de **15 s**. [dotnet] · [Editor] · `bs:136-144`; `ds:397`, `:460-466`
- **AC-BLE-06-02 · Descubrimiento.** *Entonces*, ya conectado, la app identifica Rower Data `0x2AD1` (decodifica cada notificación, F-BLE-02) y Control Point `0x2AD9`, y anota en el registro de diagnóstico los servicios y características con sus propiedades, cada paquete en hex y cada lectura decodificada (`[BLE] Parsed → SPM:… W:… dist:…m elapsed:…s`). *Dado* `K-BIKE`, queda conectado igual, con las métricas en 0 y el watchdog sin disparar (Q-35). [dotnet] · `bs:204-253`, `:291-302`
- **AC-BLE-06-03 · Toma de control.** *Entonces*, con Control Point, la app escribe `0x00` (Request Control), espera **300 ms** y escribe `0x07` (Start or Resume); si alguna escritura falla, sigue igual. *Dado* `K-NOCP`, no escribe nada y no hay keepalive. [dotnet] · `bs:259-276`
- **AC-BLE-06-04 · Keepalive de 2 s.** *Entonces*, después de la toma de control (aunque haya fallado), escribe `0x07` en el Control Point **cada 2 s** mientras dure la conexión; un error solo se anota. Con el reloj de prueba, en 60 s conectado hay 30 escrituras de keepalive (± 1). Se detiene al desconectar por cualquier motivo. [dotnet] · `bs:278-288`, `:315-319`
- **AC-BLE-06-05 · Conectado y datos.** *Entonces* recién después de todo eso el remo pasa a conectado y Dispositivo cambia a la vista conectada, con las métricas en cero (split `--:--`) hasta el primer paquete. Cada paquete de Rower Data se publica para toda la app (Dispositivo, entrenamiento, escena) igual que con el simulador. [dotnet] · `bs:155`, `:291-302`; `dp:38-42`, `:64-67`
- **AC-BLE-06-06 · Error al conectar.** *Dado* que el adaptador falso no responde, *entonces* a los 15 s el remo vuelve a desconectado y aparece el banner con `deviceConnectError` y el detalle; se borra al buscar o conectar otra vez. [dotnet] · [Editor] · [Captura] CAP-12 · `bs:156-159`; `dp:118-127`
- **AC-BLE-06-07 · Pasos visibles (mejora EXR).** *Entonces* el ítem tocado muestra, en lugar de su dirección, el paso en curso: `deviceStepConnecting`, `deviceStepControl` (solo si hay Control Point) y `deviceStepReady`, que dura lo que tarda la transición al tablero (≤ 600 ms, sin demorar el paso a conectado). Al quedar listo, en Android hay una vibración corta y sutil. Con "reducir movimiento", sin animación. [Editor] · [Captura] CAP-09, CAP-10, CAP-11 · [Device-Android]
- **AC-BLE-06-08 · Con el remo real.** *Dado* el AMS-670B, *cuando* se conecta, *entonces* el monitor pasa a modo Bluetooth, el tablero muestra datos reales en menos de 3 s y el monitor sigue en modo Bluetooth durante 10 min de remo sin tocarlo. [BLE-HW]

### F-BLE-07, F-BLE-10 y F-BLE-11 · Desconexión y vista conectada con el remo real

- **AC-BLE-07-05 · Desconectar.** *Entonces* `deviceDisconnect` está en la barra superior solo con el remo conectado; al tocarlo (sin confirmación), corta, detiene keepalive y notificaciones, **olvida** el remo (no reconecta: a los 10 s sigue desconectado) y vuelve a la vista de búsqueda con la última lista. [dotnet] · [Editor] · `bs:304-313`; `ds:21-30`
- **AC-BLE-11-12 · Cabecera y señal de conectado.** *Entonces* la cabecera muestra el nombre del remo; un remo sin nombre muestra `deviceUnknown` (como en la lista) y, sin remo asociado, `deviceRowerFallback`. Con el remo real conectado, Workout habilita el entrenamiento y el resto de la app lo ve como con el simulador (F-BLE-10). [Editor] · [Captura] CAP-13 · `ds:98`, `:450-452`

### F-BLE-08 · Reconexión (Q-29, Q-34)

- **AC-BLE-08-02 · La primera reconexión.** *Dado* `K-ROW` conectado, *cuando* el enlace se cae solo, *entonces* el remo pasa a desconectado, se limpia todo y a los **3 s** pasa a conectando, con un intento de tope **10 s** que repite descubrimiento, toma de control y keepalive. Si sale bien, conectado, con las métricas en cero hasta el primer paquete. [dotnet] · `bs:148-153`, `:163-192`
- **AC-BLE-08-03 · Reintenta hasta lograrlo (Q-29, cambio).** *Dado* que el remo sigue sin responder, *entonces* hay un intento nuevo **3 s después de cada fallo**, sin límite: con el reloj de prueba, en 60 s sin respuesta los estados alternan desconectado → conectando y, al responder el remo, queda conectado. El legacy hace un solo intento (`bs:193-197`, `:315-319`). [dotnet]
- **AC-BLE-08-04 · Qué la corta.** *Entonces* la reconexión se cancela con `Desconectar`, con `Buscar dispositivos` (cambio de Q-29: en el legacy no la cancela) o al elegir un remo de la lista; la desconexión del watchdog y la manual nunca reconectan. *Dado* el Bluetooth apagado durante la reconexión, *entonces* se ve la vista de apagado y, al encenderlo, el próximo intento reconecta solo. [dotnet] · `bs:136-137`, `:308`
- **AC-BLE-08-05 · En Dispositivo.** *Entonces*, mientras reconecta, la vista de búsqueda muestra arriba un aviso con un indicador de progreso, `deviceReconnecting` con el nombre del remo y el botón `deviceDisconnect`, que corta los reintentos. `Buscar dispositivos` se habilita entre intentos y se deshabilita durante cada intento. [Editor] · [Captura] CAP-14
- **AC-BLE-08-06 · Sobre el HUD (Q-34).** *Dado* un entrenamiento en curso, *entonces* durante la reconexión se ve un aviso discreto sobre el HUD con `deviceReconnecting`, que no tapa los valores de las métricas, y al volver `deviceReconnected` durante 2 s. Las métricas quedan en la última lectura y el reloj sigue, como en el legacy; la sesión no se pausa ni se corta. Reemplaza el "no aparece ningún aviso" de AC-WKT-03-10 (4a); el resto de ese criterio sigue. El HUD está cerca de su tope de elementos: el aviso reutiliza el lugar de los avisos de 4 s (AC-APP-03-05), sin sumar otro. Con "reducir movimiento", sin animación. [Editor] · [Captura] CAP-15, CAP-16 · `lib/features/workout/workout_provider.dart:82-86`
- **AC-BLE-08-07 · Con el remo real.** *Dado* el AMS-670B en un entrenamiento, *cuando* hay un corte breve, *entonces* reconecta solo, el monitor vuelve a modo Bluetooth y la sesión sigue. [BLE-HW]

### F-BLE-09 · Monitor apagado (Q-31)

- **AC-BLE-09-01 · Watchdog de 5 s.** *Entonces*, conectado, hay un chequeo **cada 1 s** que actúa solo después del **primer** paquete de Rower Data de esa conexión: si pasaron **≥ 5 s enteros** desde el último paquete (entre 5 y 6 s en la práctica), la app hace una desconexión manual, sin reconectar. El chequeo se detiene en cualquier estado que no sea conectado. Sin ningún paquete, nunca dispara. [dotnet] · `dp:8`, `:69-86`
- **AC-BLE-09-02 · El aviso.** *Entonces*, cuando dispara, Dispositivo muestra arriba `deviceMonitorStopped` con el botón `retry` (`Reintentar`), que vuelve a conectar el mismo remo; el aviso se borra al reintentar, buscar o conectar. Si hay un entrenamiento, el mismo texto aparece sobre el HUD durante 4 s y la sesión sigue como en el legacy. [Editor] · [Captura] CAP-17, CAP-18
- **AC-BLE-09-03 · Con el remo real.** *Dado* el AMS-670B, *cuando* se apaga el monitor, *entonces* a los 5-6 s se ve el aviso. Se anota si el monitor notifica durante una pausa del remero de 10 s (si no, el watchdog lo desconecta: issue). [BLE-HW]

### F-BLE-12 · Diagnóstico oculto (Q-04, Q-37)

- **AC-BLE-12-01 · Acceso.** *Entonces* en la vista conectada aparece `Show BLE debug` (ícono de bicho, 12, gris 24 %) después de 3 desplazamientos hacia abajo de más de 20 cada uno, **o** al mantener presionada la cabecera del remo ≥ 0,5 s (Q-37, para pantallas sin desplazamiento). Tocarlo abre el panel, que sigue abierto mientras dure la conexión; al reconectar hay que repetir el gesto. [Editor] · `ds:52-74`, `:124-134`
- **AC-BLE-12-02 · Contenido, fijo en inglés.** *Entonces* el panel `DEBUG BLE` (fondo negro 38 %, radio 12, borde blanco 12 %) muestra: `RECEIVING` en verde si llegó al menos 1 paquete, si no `NO DATA` en rojo; `Notifications` con el contador acumulado de toda la vida de la app (no se reinicia al reconectar); `Last received` con la hora `HH:MM:SS` del último paquete de esta conexión o `--:--:--`; `Raw bytes (hex):` con el último paquete en hex minúscula de 2 dígitos separados por espacio, en cian monoespaciado, o `(sin datos)` (no se borra al reconectar); y `Parsed SPM` (1 decimal), `Parsed Watts`, `Parsed Distance` (`{n}m`) y `Parsed Split`. Valores FTMS "no disponible" y potencias negativas se muestran crudos (Q-38). [dotnet] · [Editor] · [Captura] CAP-19, CAP-20 · `dp:20-22`, `:56-61`, `:98-102`; `ds:142-218`

### Q-91 y F-APP-14 · Segundo plano y pantalla

- **AC-BLE-91-01 · El keepalive sigue en segundo plano.** *Dado* `K-ROW` conectado y un entrenamiento, *cuando* la app pasa a segundo plano 60 s con el ciclo de vida de prueba, *entonces* el keepalive sigue cada 2 s (30 escrituras ± 1), los paquetes recibidos se aplican con su hora al volver (ADR 017) y el watchdog **no** dispara si los paquetes siguieron llegando; si el monitor dejó de mandar ≥ 5 s durante el segundo plano, al volver desconecta con el aviso de AC-BLE-09-02. [dotnet]
- **AC-BLE-91-02 · En el equipo.** *Entonces* en Windows, con la ventana minimizada 30 s, y en Android, con el teléfono bloqueado 60 s, el monitor sigue en modo Bluetooth y la sesión sigue (AC-WKT-02-05). [Device-Win] (con `K-10` no aplica: solo que la app no se cuelga al minimizar buscando o conectada al falso) · [Device-Android] · [BLE-HW]
- **AC-APP-14-05 · Pantalla encendida en Android.** *Entonces* en Android la pantalla no se apaga en ninguna pestaña mientras la app está adelante, también al volver del segundo plano. [Device-Android] · `lib/main.dart:42`

### QA-V4a-L01 · El paso por distancia nunca queda trabado

- **AC-WKT-03-11 · Monitor que no informa cada segundo.** *Entonces* en el paso por distancia (AC-WKT-03-03) y en los parciales (F-WKT-07) a la vez, un avance de la distancia del monitor cuenta si no es negativo y no supera **10 m por cada segundo en marcha desde la última vez que la distancia cambió** (o desde que se fijó la referencia). Con `Distancia 100`: una lectura nueva cada 2 s con +11 m termina el paso en el segundo 20; dos lecturas en el mismo segundo suman las dos; +50 m después de 5 s sin cambio suman; un reinicio a 0 no suma y pasa a ser la referencia; +200 m en 1 s no suma. Con una lectura por segundo, el resultado es igual al de hoy: los tests de la 4a y la 5 siguen pasando sin cambios. [dotnet]

### Seguridad del Bluetooth

- **AC-BLE-SEC-01 · Solo lectura con lo ajeno.** *Entonces* la app solo se conecta a equipos que el usuario tocó en la lista (todos FTMS) o al último remo que tuvo conectado; nunca empareja, desempareja ni cambia el estado de la radio; y nada de lo que se versiona (código, docs, logs de prueba, capturas) tiene nombres ni direcciones de equipos reales. [dotnet] (con `K-HP` nunca hay conexión) · revisión de QA

---

## 4. Textos nuevos (F-APP-10, Q-01)

Doce claves nuevas: **240 por idioma** (228 + 12). El diagnóstico queda fijo en inglés (Q-04) y `(sin datos)` en español, como el legacy. Los `...` de las claves existentes no se tocan; las nuevas usan `…` (U+2026).

| Clave | es | en | Uso |
|---|---|---|---|
| `deviceUnknown` | `Dispositivo desconocido` | `Unknown device` | ítem y cabecera sin nombre (legacy: `Unknown device` fijo, `ds:452`) |
| `deviceRowerFallback` | `Rower` | `Rower` | cabecera sin remo asociado (`ds:98`) |
| `deviceScanError` | `Error al escanear: {error}` | `Scan error: {error}` | banner (`dp:111`) |
| `deviceConnectError` | `Error al conectar: {error}` | `Connection error: {error}` | banner (`dp:125`) |
| `deviceStepConnecting` | `Conectando…` | `Connecting…` | paso 1 |
| `deviceStepControl` | `Tomando control del monitor…` | `Taking control of the monitor…` | paso 2 |
| `deviceStepReady` | `Listo` | `Ready` | paso 3 |
| `deviceReconnecting` | `Reconectando con {name}…` | `Reconnecting to {name}…` | Dispositivo y HUD (Q-34) |
| `deviceReconnected` | `Reconectado con {name}` | `Reconnected to {name}` | HUD, 2 s (Q-34) |
| `deviceMonitorStopped` | `El monitor dejó de enviar datos` | `The monitor stopped sending data` | Dispositivo y HUD (Q-31) |
| `deviceBleUnsupported` | `Este equipo no tiene Bluetooth LE` | `This device doesn't have Bluetooth LE` | vista sin soporte (Q-47) |
| `deviceBleUnsupportedHint` | `RowMate necesita Bluetooth LE para conectar\nel remo y el pulsómetro` | `RowMate needs Bluetooth LE to connect\nthe rower and the heart rate monitor` | vista sin soporte |

`{name}` es el nombre del remo, o `deviceUnknown` si no tiene. `{error}` es el detalle técnico del sistema, sin traducir.

---

## 5. Checklist de paridad

| Comportamiento legacy | Fuente | AC |
|---|---|---|
| Vistas apagado / sin permiso y su botón | `ds:346-394` | AC-BLE-04-01, -02, -04 |
| Espera de 5 s del adaptador | `bs:69-84` | AC-BLE-04-05 |
| Búsqueda de 10 s FTMS, en vivo, sin duplicados, solo esta búsqueda | `bs:65-123` | AC-BLE-05-04 a -06 |
| Filtro en la app en Windows | `sf:12-20` | AC-BLE-05-05 |
| Banner de error al buscar y al conectar | `dp:104-127` | AC-BLE-05-08, AC-BLE-06-06 |
| Exclusión con el pulsómetro (LT-29 a LT-31) | `ds:348,397`; `heart_rate_card_test.dart:121,142,161` | AC-BLE-05-09 |
| Conexión de 15 s, descubrimiento, 0x00 → 300 ms → 0x07, keepalive 2 s | `bs:135-160`, `:204-289` | AC-BLE-06-01 a -05 |
| Desconexión manual que olvida el remo | `bs:304-313` | AC-BLE-07-05 |
| Reconexión a los 3 s con tope de 10 s | `bs:163-199` | AC-BLE-08-02 (y -03, cambio Q-29) |
| Watchdog cada 1 s, ≥ 5 s, solo tras el primer paquete | `dp:8`, `:64-86` | AC-BLE-09-01 |
| Diagnóstico oculto y su contenido | `ds:52-74`, `:124-218`; `dp:20-22`, `:56-61`, `:98-102` | AC-BLE-12-01, -02 |
| Permisos de Android y BLE obligatorio | `am:4-19` | AC-APP-13-01 |
| Pantalla siempre encendida | `lib/main.dart:42` | AC-APP-14-05 |

---

## 6. Capturas (todas con el adaptador falso `K-10`)

Español salvo que diga `en`. Ningún nombre ni dirección real.

| ID | Qué | Tamaños |
|---|---|---|
| CAP-01 | Bluetooth apagado | TV, W1280 |
| CAP-02 | Bluetooth apagado, `en` | TV |
| CAP-03 | Sin permiso | TV |
| CAP-04 | Sin Bluetooth LE | TV |
| CAP-05 | Buscando, sin resultados | TV |
| CAP-06 | Lista con `AMS-670B`, `Dispositivo desconocido` y `Bici FTMS` | TV, W1280 |
| CAP-07 | Lista + tarjeta del pulsómetro en horizontal, sin desborde | L640, TH |
| CAP-08 | Banner `Error al escanear` | TV |
| CAP-09 | Conectando: `Conectando…` | TV |
| CAP-10 | Conectando: `Tomando control del monitor…` | TV |
| CAP-11 | Conectando: `Listo` | TV |
| CAP-12 | Banner `Error al conectar` | TV |
| CAP-13 | Conectado con datos del paquete de prueba (2 y 3 columnas) | TV, W1280 |
| CAP-14 | Reconectando en Dispositivo | TV |
| CAP-15 | Reconectando sobre el HUD | W1280, TV |
| CAP-16 | `Reconectado con AMS-670B` sobre el HUD, `en` en W1280 | W1280 |
| CAP-17 | Monitor apagado en Dispositivo | TV |
| CAP-18 | Monitor apagado sobre el HUD | W1280 |
| CAP-19 | Diagnóstico con `RECEIVING`, contador y bytes crudos | TV, W1280 |
| CAP-20 | Diagnóstico con `NO DATA` (`K-BIKE`) | TV |

---

## 7. Fuera de alcance

- Pulsómetro real: búsqueda, memoria, reconexión y watchdog (It. 11). La tarjeta del pulsómetro sigue como en la It. 2, salvo que se oculta con el Bluetooth apagado, sin permiso o sin soporte.
- iOS y macOS: Bluetooth, permisos, `bluetooth-central` y sus criterios [Apple] (It. 10b).
- Recordar el último remo y conectarse solo al arrancar (Q-30, backlog).
- Sincronía fina con el contador de paladas (D-ANI-06): queda **apagada**; en esta iteración solo se mide en la demo.
- Servicio en primer plano de Android con notificación (ADR 017): solo si la demo muestra que el sistema mata el proceso.
- Pantalla de bienvenida previa al permiso y guías por plataforma (mejora EXR de F-APP-13).

---

## 8. Decisiones del PO (una línea cada una)

| ID | Decisión |
|---|---|
| DV-96 | Q-29: reintenta 3 s después de cada fallo, sin límite; `Buscar` también cancela la reconexión (en el legacy, no). |
| DV-97 | Con el Bluetooth apagado durante la reconexión, los intentos siguen y reconecta al encenderlo (el legacy ya no reconectaba). |
| DV-98 | Q-31: el aviso del watchdog queda en Dispositivo hasta reintentar, buscar o conectar, y 4 s sobre el HUD. |
| DV-99 | Q-34: `Reconectado con {name}` dura 2 s; el aviso no tapa los valores de las métricas. |
| DV-100 | Android: un permiso negado muestra la vista "sin permiso" en lugar de `Error al escanear`. |
| DV-101 | `Unknown device` se localiza (`Dispositivo desconocido`); un remo conectado sin nombre muestra lo mismo en la cabecera (el legacy, vacío). |
| DV-102 | Q-32: la etiqueta sigue siendo `Activar Bluetooth` en todas las plataformas; las pistas siguen hablando de rutas de iOS, como el legacy. |
| DV-103 | QA-V4a-L01: el tope de 10 m se mide por segundo desde el último cambio de distancia, en el paso y en los parciales a la vez; se aplica ya, sin esperar la medición del AMS-670B, porque con una lectura por segundo da lo mismo. |
| DV-104 | Q-37: el gesto alternativo es mantener presionada la cabecera ≥ 0,5 s. |
| DV-105 | Conectar no detiene la búsqueda visible (paridad); si la plataforma necesita cortar el escaneo por dentro, no se nota en la lista ni en el estado. |

---

## 9. Para issues

- Pistas de "apagado" y "sin permiso" propias de cada plataforma (hoy hablan de iOS en Android y Windows).
- Intensidad de señal en cada ítem de la lista y el "sonar" animado de la búsqueda (mejora EXR de F-BLE-05).
- Cierre explícito "Búsqueda terminada" al vencer los 10 s.
- Sonido sutil al quedar `Listo` (solo hay vibración en Android).
- En Android 11 o menos, aviso si la ubicación del sistema está apagada (la búsqueda no encuentra nada).
- Si el AMS-670B no notifica en las pausas, distinguir pausa de monitor apagado (Q-31).
- Si el AMS-670B no reinicia sus contadores al conectar, revisar los totales de la sesión (Q-15).
- Indicador de paquetes por segundo en el diagnóstico (mejora EXR de F-BLE-12).
