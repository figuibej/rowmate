# It. 11 — Bluetooth real: pulsómetro (spec de producto)

| Campo | Valor |
|---|---|
| Iteración | 11 (`11-ble-pulsometro`), [ROADMAP §It. 11](../../ROADMAP.md) |
| Incluye | F-HRM-02, F-HRM-03, F-HRM-05 a F-HRM-11, F-HRM-16 ([heart-rate](../../product/inventory/heart-rate.md)) |
| También toca | F-APP-01 (el sensor recordado se reconecta al abrir la app), F-HRM-04 y F-HRM-12/13 (tarjeta con el servicio real), F-HRM-14 / F-WKT-05 (fusión con el sensor real), F-APP-10 (5 claves nuevas: 245 por idioma) |
| Decisiones | Q-33, Q-41, Q-42, Q-43, Q-44 ([FEATURE_INVENTORY §4](../../product/FEATURE_INVENTORY.md)); paridad con Q-39, Q-45, Q-46; DV-96/97/99 de la [10a](../10a-ble-android-windows/01-po-spec.md) como precedente |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`). Leídos enteros: `lib/core/bluetooth/heart_rate_service.dart`, `reconnect_loop.dart`, `lib/features/device/heart_rate_card.dart`, `heart_rate_provider.dart`, `test/reconnect_loop_test.dart`; en parte `lib/main.dart`, `lib/features/device/device_screen.dart`. No se leyó `lib/core/strava/strava_secrets.dart` |
| Modo | Entrega rápida, sin debates. Ante una duda, el legacy o un valor por defecto, anotado en una línea (§9) |
| Criterios | **41** (§3), 7 casos legacy a portar (§5), 16 capturas con el adaptador falso (§7) y 2 guiones de demo (§1) |

---

## 0. Cómo leer esta spec

- Tamaños (**TV** 411 × 914, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720, en dp) y estado de prueba (reloj de prueba, `ROWMATE_DATA_DIR` temporal, sin tocar `prefs.json` ni las bases del usuario): los de la [spec de la 6a §0](../06a-escena-lago/01-po-spec.md). Textos entre comillas invertidas: **exactos al carácter**.
- Marcas de verificación:
  - `[dotnet]`: servicio del pulsómetro, cadencias y watchdog con el **adaptador falso** y el **reloj de prueba** (nada espera tiempo real).
  - `[Editor]`: vistas en el Editor con el adaptador falso; `[Captura]` = captura de §7.
  - `[Device-Win]`: build normal de Windows con el adaptador real de la PC, **sin** pulsómetro ni remo: la búsqueda del pulsómetro dura 10 s, no rompe y solo lista equipos que anuncian `0x180D` (la del remo, solo FTMS `0x1826`). Nunca se empareja, se conecta a algo ni se toca la radio.
  - `[Device-Android]`: teléfono. **NO VERIFICABLE** hasta que el usuario instale "Android Build Support"; queda para su demo.
  - `[BLE-HW]`: con una banda o un reloj real (y el AMS-670B). **NO VERIFICABLE** en el workflow; queda para la demo del usuario (§1).
- Fuentes legacy: `hrs` = `lib/core/bluetooth/heart_rate_service.dart`, `rl` = `lib/core/bluetooth/reconnect_loop.dart`, `rlt` = `test/reconnect_loop_test.dart`, `hc` = `lib/features/device/heart_rate_card.dart`, `hp` = `lib/features/device/heart_rate_provider.dart`, `ds` = `lib/features/device/device_screen.dart`, `sf` = `lib/core/bluetooth/scan_filter.dart`, `m` = `lib/main.dart`.
- **Adaptador falso de esta iteración** (`K-11` = `K-10` de la 10a + estos sensores ficticios, con direcciones inventadas):
  - `K-HRM` = `Banda de prueba`, `AA:BB:CC:00:18:0D`, anuncia `180d` (16 bits), tiene `0x180D` con `0x2A37`.
  - `K-WATCH` = `Reloj de prueba`, `AA:BB:CC:00:18:0E`, anuncia `0x180D` en 128 bits junto a otro servicio, tiene `0x2A37`.
  - `K-HRANON` = sin nombre (como un reloj en Windows), `AA:BB:CC:00:18:0F`, anuncia `180d`, tiene `0x2A37`.
  - `K-NOHR` = `Sensor sin pulso`, `AA:BB:CC:00:18:10`, anuncia `180d` pero **no** tiene `0x2A37` (incompatible).
  - De la 10a: `K-ROW` (`AMS-670B`, FTMS) y `K-HP` (`Auriculares`, sin FTMS ni `0x180D`).
  - Paquetes de prueba: `[0x00, 72]` → 72; `[0x06, 142]` → 142 con contacto; `[0x01, 0x8E, 0x00]` → 142 (16 bits); `[0x04, 0]` → sin contacto (0); `[0x01, 72]` → inválido. Ninguna prueba, captura ni demo versionada muestra un nombre o una dirección de un equipo real.
- Los tiempos se verificaron en el código: búsqueda de 10 s (`hrs:108`), conexión de 15 s sin pedir MTU (`hrs:241-246`), watchdog de 10 s (`hrs:38`), arranque 6 intentos × 10 s (`hrs:40-42`), caída cada 3 s sin límite (`hrs:44-45`); el motor cuenta la espera desde el **fin** del intento fallido (`rl:49-70`).
- IDs: AC-HRM-NN-nn por ficha (las 02, 03, 05 a 11 y 16 son nuevas; AC-HRM-04-06, -13-13, -15-13 y AC-WKT-05-07 siguen a los de las It. 2 y 4a; AC-APP-01-10 sigue a -09). `LT-139` a `LT-145` siguen a LT-138. `DV-106` a `DV-115` siguen a DV-105.

---

## 1. Objetivo y demo

**Objetivo.** Usar un pulsómetro real (banda o reloj que transmita `0x180D`) en Android y Windows: búsqueda, conexión manual con validación del servicio, memoria del último sensor, reconexión al abrir la app y tras una caída, watchdog de lectura vencida, desconectar y olvidar, con prioridad de las acciones del usuario. Reutiliza la capa nativa de la 10a. El pulsómetro simulado (It. 2) no cambia: lo usan los tests y el build `simulator`.

**Lo que el workflow deja verificado**, sin sensor: todo el servicio y sus cadencias con el adaptador falso ([dotnet]), la tarjeta en cada estado y el HUD ([Editor], §7) y, en esta PC, que el build normal busca pulsómetros 10 s con el adaptador real sin romper y sin listar nada que no anuncie `0x180D` ([Device-Win]).

**Demo del usuario en Windows** (build normal, W1280, español; una banda mojada y puesta, o un reloj con una app que transmita el pulso, p. ej. Heart for Bluetooth con los permisos "Sensores corporales" y "Dispositivos cercanos"):

1. Abrir RowMate. Dispositivo muestra la tarjeta `PULSÓMETRO` con `Sin sensor` y la pista del smartwatch.
2. Tocar `Buscar`. Durante 10 s: `Buscando sensores...` y aparece el sensor (un reloj suele salir como `Sensor desconocido` + dirección). `Buscar dispositivos` del remo queda deshabilitado mientras tanto. Nada que no sea pulsómetro aparece.
3. Tocar el sensor: `Conectando a {nombre}...` y luego el nombre con el pulso en vivo; el corazón late a ese ritmo.
4. Sacarse la banda (o pausar la app del reloj) 15 s: a los 10 s se ve `--` con `Sin señal del sensor`; al volver, el pulso real.
5. Cerrar y abrir la app: la tarjeta muestra `{nombre} · reconectando...` y se conecta sola, con el aviso `Pulsómetro conectado`.
6. Con el AMS-670B encendido: `Buscar dispositivos` → conectar el remo. Los dos quedan conectados; la tarjeta del pulsómetro va debajo de la grilla del remo.
7. Workout → `Entrenamiento libre`, remar 2 min. El HUD muestra el pulso del sensor, no el del monitor. Alejarse o apagar la banda 15 s: aviso `Se perdió el pulsómetro, reconectando…`, la tarjeta BPM desaparece si el monitor no manda pulso (el AMS-670B no lo manda) y la sesión sigue; al volver, `Pulsómetro conectado` y el pulso. `Terminar` y abrir el detalle en Historial: pulso promedio/máximo y curva del sensor.
8. Dispositivo → `Desconectar`: vuelve `Sin sensor`. Cerrar y abrir: **no** se reconecta.
9. (Opcional) Con un sensor recordado y apagado, abrir la app: unos 50 s de `· reconectando...` (6 intentos) y luego `· no encontrado` con `Conectar`, `Buscar otro` y `Olvidar`. `Olvidar` → `Sin sensor`.
10. Mandar en privado las líneas `[HRM]` del `Player.log` (nombres y direcciones reales **no** van al repo).

**Demo del usuario en Android** (con "Android Build Support" instalado y el APK generado): los pasos 1 a 8 en el teléfono, más: con la app en segundo plano 60 s durante el entrenamiento, el pulso sigue llegando al volver (sin servicio en primer plano, como la 10a).

---

## 2. Historias de usuario

- Como remero quiero encontrar mi banda o mi reloj con un toque y ver mi pulso en vivo, con un error claro si el equipo no es un pulsómetro.
- Como remero quiero que la app recuerde mi sensor y se conecte sola al abrirla y después de una caída, sin bajarme del remo.
- Como remero quiero que, durante el entrenamiento, el pulso del sensor reemplace al del monitor en el HUD y en la sesión guardada.
- Como remero quiero soltar u olvidar el sensor y que la app no lo vuelva a enganchar sola, y que lo que toco mande siempre sobre lo automático.

---

## 3. Criterios de aceptación

### F-HRM-09 · Motor de reintentos

- **AC-HRM-09-01 · Los 6 casos del motor (LT-139 a LT-144).** *Entonces* el motor de reintentos cumple los 6 casos de §5 con el reloj de prueba: primer intento inmediato; éxito → para; fallo o error → espera el intervalo desde el **fin** del intento y reintenta; con máximo N se rinde y avisa una vez; cancelar o reiniciar descarta el resultado de un intento en vuelo; se puede reiniciar desde el aviso de rendición. *Reintentando* = hay corrida activa, incluidas las esperas. [dotnet] · `rl:8-71`; `rlt:9-153`

### F-HRM-05 · Watchdog de lectura vencida

- **AC-HRM-05-01 · El plazo genérico (LT-145).** *Entonces* con plazo de 10 s: tocar a los 0 y 9 s no dispara hasta los 19 s; dispara **una sola vez**; 30 s más no vuelve a disparar; tocar y detener → no dispara. [dotnet] · `rl:75-91`; `rlt:157`
- **AC-HRM-05-02 · En el servicio.** *Dado* `K-HRM` conectado y una lectura `[0x06, 142]`, *cuando* pasan 10 s sin lectura válida (también si solo llegan `[0x01, 72]` inválidos), *entonces* el pulso pasa a **0 una vez**, la conexión **no** se corta y no arranca ningún reintento; la próxima lectura válida devuelve 142 y rearma el plazo. El plazo se arma al conectar y se detiene al desconectar, al caerse, al abortar un intento y al cerrar la app (no queda temporizador vivo). [dotnet] · `hrs:37-38`, `:86-89`, `:274`, `:321-326`, `:380`, `:396`, `:440`
- **AC-HRM-05-03 · "Sin señal del sensor" (mejora EXR).** *Dado* el sensor conectado y el watchdog disparado, *entonces* la tarjeta muestra `--` y debajo `hrmNoSignal` (`Sin señal del sensor`) en 12 dp `text.tertiary`, y el corazón queda quieto al 40 %. Antes de la primera lectura se ve solo `--` con el corazón quieto en color pleno (paridad, LT-28). Con la próxima lectura válida la leyenda desaparece. [Editor] · [Captura] CAP-06 · `hc:150-196`

### F-HRM-04 · Pulso en vivo con el sensor real

- **AC-HRM-04-06 · Cada notificación cuenta.** *Dado* `K-HRM` conectado, *cuando* llegan `[0x00, 72]`, `[0x00, 72]`, `[0x01, 0x8E, 0x00]`, `[0x04, 0]` y `[0x01, 72]`, *entonces* se publican 72, 72, 142 y 0 (sin contacto → 0) y el inválido se descarta sin rearmar el watchdog; sin promedios. Las lecturas llegan desde el hilo nativo y se publican en el principal; con el sensor conectado no hay asignaciones de memoria por cuadro (medido con el perfilador, como en la 10a). [dotnet] · [Editor] · `hrs:321-331`

### F-HRM-02 · Búsqueda

- **AC-HRM-02-01 · Arranque y fin.** *Cuando* se toca `Buscar` (o `Buscar otro`), *entonces* se cancelan las dos cadencias y cualquier intento en vuelo (que cierra su enlace en silencio), una búsqueda anterior termina ya, el estado pasa a buscando y la lista arranca **vacía**. Dura **10 s**; al vencer sin elección, la lista desaparece (Q-44) y la tarjeta vuelve a `Sin sensor` o a `{recordado} · no encontrado`; la reconexión automática **no** se reanuda (Q-43). [dotnet] · `hrs:108-168`, `:211`
- **AC-HRM-02-02 · Solo pulsómetros, en vivo y sin repetir.** *Dado* `K-11` anunciándose en este orden: `K-HP`, `K-HRM`, `K-ROW`, `K-WATCH`, `K-HRANON`, `K-HRM` otra vez, *entonces* la lista queda `Banda de prueba`, `Reloj de prueba`, `Sensor desconocido`, una vez cada uno, en el orden del primer descubrimiento, con la dirección de subtítulo; `Auriculares` y `AMS-670B` nunca aparecen (en Windows filtra la app: `180d` de 16 bits = UUID de 128 bits). Solo cuentan los resultados de esta búsqueda: ni los de una anterior ni los del remo; y en la lista del remo nunca aparece un pulsómetro. [dotnet] · `hrs:131-145`; `sf:12-20`
- **AC-HRM-02-03 · Errores de búsqueda.** *Entonces* un error nativo durante el escaneo termina la búsqueda **en silencio**; si arrancar la búsqueda falla, el estado vuelve a desconectado y la tarjeta muestra el error de búsqueda de AC-HRM-13-14. [dotnet] · [Editor] · [Captura] CAP-11 · `hrs:147-157`
- **AC-HRM-02-04 · Las búsquedas se excluyen, con los servicios reales.** *Entonces* LT-29 y LT-31 pasan con el remo y el pulsómetro reales sobre `K-11`: con el pulsómetro buscando, `Buscar dispositivos` está deshabilitado; con el remo buscando o conectando, `Buscar`, `Buscar otro` y `Conectar` del pulsómetro están deshabilitados; `Olvidar`, `Desconectar` y las filas, nunca. Nunca hay dos búsquedas a la vez. [Editor] · [Captura] CAP-16 · `ds:348`, `:397`; `hc:22-24`, `:212`, `:218`
- **AC-HRM-02-05 · Diagnóstico en el registro.** *Entonces* al empezar se registra `[HRM] Escaneando sensores 0x180D durante 10s` y, por cada sensor nuevo que pasa el filtro, `[HRM] Encontrado <id> "<nombre>" rssi=<dBm> uuids=[…]`. Solo va al registro local, nunca a lo versionado. [dotnet] · `hrs:130`, `:139-142`
- **AC-HRM-02-06 · En esta PC.** *Dado* el build normal de Windows con el adaptador real, *cuando* se toca `Buscar` del pulsómetro, *entonces* durante 10 s se ve `Buscando sensores...`, después la tarjeta vuelve a `Sin sensor` sin error, y la lista solo pudo mostrar equipos que anuncian `0x180D`. Si aparece un pulsómetro real cercano, **no** se toca: queda solo en el registro local. [Device-Win]

### F-HRM-03 · Conexión manual

- **AC-HRM-03-01 · La secuencia.** *Cuando* se toca la fila de `K-HRM`, *entonces* se cancelan las cadencias, se detiene la búsqueda, la tarjeta muestra `Conectando a Banda de prueba...` y, en este orden (el adaptador falso registra cada paso): conexión directa con tope de **15 s** y sin pedir MTU → búsqueda de `0x180D` y `0x2A37` → activar notificaciones → recordar el sensor → armar la vigilancia de caída → armar el watchdog → estado conectado y registro `[HRM] Conectado a Banda de prueba`. Al quedar conectado se borran el error anterior y el nombre elegido. [dotnet] · `hrs:188-203`, `:229-276`; `hp:23-28`
- **AC-HRM-03-02 · Sensor incompatible.** *Cuando* se toca `K-NOHR`, *entonces* se desconecta, queda desconectado, la tarjeta muestra `hrmIncompatible` y el sensor **no** se recuerda (un recordado anterior sigue). [dotnet] · [Editor] · [Captura] CAP-09 · `hrs:248-253`
- **AC-HRM-03-03 · Fallas.** *Dado* el adaptador falso que no responde (15 s), rechaza la conexión, falla al descubrir servicios o al activar notificaciones, *entonces* en cada caso se cierra el enlace (nada queda colgado), el pulso pasa a 0 si no lo era, el estado queda desconectado, la tarjeta muestra el error de conexión de AC-HRM-13-14 y **no** se programan reintentos; un sensor que ya estaba recordado sigue recordado. [dotnet] · `hrs:294-304`
- **AC-HRM-03-04 · Pedidos que se pisan.** *Entonces* tocar una fila mientras ya hay una conexión en curso se ignora en silencio; durante una conexión manual la tarjeta no ofrece botones (Q-45). [dotnet] · `hrs:189-192`

### F-HRM-06 · Memoria del último sensor

- **AC-HRM-06-01 · Qué se guarda.** *Cuando* `K-HRM` queda conectado, *entonces* las preferencias de la app tienen `hrm.deviceId` = `AA:BB:CC:00:18:0D` y `hrm.deviceName` = `Banda de prueba` (nombre del sistema; si está vacío, el anunciado). *Cuando* después conecta `K-HRANON` (sin nombre), *entonces* `hrm.deviceId` cambia y `hrm.deviceName` **se borra**: la tarjeta muestra `Sensor desconocido`, no el nombre de la banda (Q-41, cambio respecto del legacy). [dotnet] · `hrs:405-412`
- **AC-HRM-06-02 · Qué se lee y cómo se muestra.** *Entonces* la memoria se lee al abrir la app y en cada `Conectar`; el nombre solo vale si hay id (un nombre huérfano se ignora). Nombre recordado = el guardado o `Sensor desconocido`; nombre conectado = el del sistema, si no el recordado, si no `Sensor desconocido` (Q-41). [dotnet] · `hrs:91-98`, `:414-421`
- **AC-HRM-06-03 · Qué nunca se recuerda.** *Entonces* no se recuerda un sensor incompatible, ni uno cuya conexión falló antes de activar notificaciones, ni lo recuerda un intento abortado (un sensor olvidado no vuelve). Hay un solo recordado: `Buscar otro` no olvida hasta que otro conecta. El build `simulator` no escribe estas claves. [dotnet] · `hrs:259`, `:280-293`

### F-HRM-07 y F-APP-01 · Reconexión al abrir la app

- **AC-APP-01-10 · El sensor recordado se busca solo al abrir.** *Dado* el build normal con `hrm.deviceId` guardado, *cuando* se abre la app, *entonces* el pulsómetro arranca su reconexión aunque Dispositivo no esté en pantalla; un error de ese arranque solo va al registro como `[HRM] autoConnect: <error>`. En el build `simulator` no hace nada y la tarjeta abre en `Sin sensor` (AC-APP-01-04). [dotnet] · [Editor] · `m:68-77`
- **AC-HRM-07-01 · Precondiciones y primer intento.** *Entonces* la reconexión solo arranca si hay recordado, el estado es desconectado y no hay una búsqueda del usuario en curso; cancela la cadencia de caída y hace el **primer intento de inmediato**, directo a la dirección recordada, **sin buscar**. [dotnet] · `hrs:207-227`
- **AC-HRM-07-02 · Cadencia de arranque.** *Dado* `K-HRM` recordado y apagado (cada intento falla al instante), *entonces* hay intentos a los 0, 10, 20, 30, 40 y 50 s (**6 en total**) y ninguno más en los 120 s siguientes; con intentos que vencen a los 15 s, el último termina a los **140 s**. Un sensor incompatible cuenta como fallo. Los fallos nunca se muestran en la tarjeta: solo `[HRM] Reintento fallido: …` en el registro. Tras el 6.º, *reintentando* = no y la tarjeta muestra `{nombre} · no encontrado` con `Conectar` habilitado. [dotnet] · [Captura] CAP-08 · `hrs:40-42`, `:76-81`, `:217-227`
- **AC-HRM-07-03 · La tarjeta mientras reintenta.** *Entonces* durante toda la cadencia (intentos y esperas) la tarjeta muestra `{nombre} · reconectando...` con `Conectar` deshabilitado y `Buscar otro` y `Olvidar` habilitados; nunca solo un spinner. [Editor] · [Captura] CAP-07 · `hc:73-78`, `:198-226`
- **AC-HRM-07-04 · "Conectar".** *Cuando* se toca `Conectar` en `· no encontrado`, *entonces* se borra el error, se relee la memoria y arranca **la misma cadencia desde cero** (6 intentos, 10 s). [dotnet] · `hp:72-78`
- **AC-HRM-07-05 · Éxito y aviso (mejora EXR).** *Cuando* un intento automático (al abrir, `Conectar` o tras una caída) conecta, *entonces* la cadencia para, queda armada la vigilancia de caída y aparece `hrmConnectedNotice` (`Pulsómetro conectado`) **2 s** en la capa de avisos (en cualquier pestaña y sobre el HUD). La conexión manual desde la lista no lo muestra (la tarjeta ya lo dice). Si hay un aviso del remo en pantalla, el del pulsómetro no lo reemplaza y se descarta. Con "reducir movimiento", sin animación. [Editor] · [Captura] CAP-12 · `hrs:274-276`

### F-HRM-08 · Reconexión tras una caída

- **AC-HRM-08-01 · Caída y cadencia de 3 s.** *Dado* `K-HRM` conectado con 142, *cuando* el adaptador falso informa una desconexión que el usuario no pidió, *entonces* primero el pulso pasa a 0 y **después** el estado a desconectado; se registra `[HRM] Conexión perdida, reintentando cada 3s`; hay un intento inmediato y luego uno cada **3 s** desde el fin del anterior, **sin límite** (sigue intentando después de 100 fallos). El sensor sigue recordado y la tarjeta muestra `{nombre} · reconectando...`. [dotnet] · `hrs:44-45`, `:263-272`, `:393-401`
- **AC-HRM-08-02 · Cómo termina.** *Entonces* la cadencia de caída termina al reconectar (vuelven las lecturas sin tocar nada), con una acción del usuario o al cerrar la app. `Desconectar` nunca la dispara. Con el Bluetooth apagado durante la caída, los intentos siguen fallando cada 3 s y reconecta al encenderlo. Cada caída nueva relanza la cadencia. [dotnet] · `hrs:265-271`, `:337-349`
- **AC-HRM-08-03 · Durante un entrenamiento (mejora EXR).** *Dado* un entrenamiento en curso, *cuando* el sensor se cae, *entonces* aparece `hrmLostNotice` (`Se perdió el pulsómetro, reconectando…`) **4 s** en el lugar de los avisos del HUD (sin sumar elementos; un aviso del remo tiene prioridad), la tarjeta BPM desaparece **si el monitor no manda pulso** (AMS-670B; si lo manda, muestra el del monitor, AC-WKT-05-07) y la sesión sigue; *cuando* vuelve, `Pulsómetro conectado` 2 s (AC-HRM-07-05) y la tarjeta BPM reaparece. [Editor] · [Captura] CAP-14

### F-HRM-10 · Desconectar y Olvidar

- **AC-HRM-10-01 · Desconectar.** *Cuando* se toca `Desconectar`, *entonces* se cancelan cadencias e intento en vuelo, la desconexión cuenta como pedida (no hay cadencia de caída), el pulso pasa a 0, el enlace se cierra de inmediato (una conexión pendiente se cancela sin esperar los 15 s), se borran `hrm.deviceId` y `hrm.deviceName` y la tarjeta vuelve a `Sin sensor`. Sin confirmación ni deshacer (Q-33). Al reabrir la app, **no** se reconecta. [dotnet] · [Editor] · `hrs:337-349`
- **AC-HRM-10-02 · Olvidar.** *Cuando* se toca `Olvidar` (siempre habilitado en el estado recordado), también durante `· reconectando...`, *entonces* la cadencia se corta en el acto, un intento en vuelo se aborta (desconectado, enlace cerrado en silencio), se borra la memoria y la tarjeta vuelve a `Sin sensor`. Ambas acciones borran el error previo; si fallan, el error queda en la tarjeta y la app sigue andando. [dotnet] · [Editor] · `hrs:353-383`; `hp:80-89`

### F-HRM-11 · Las acciones del usuario mandan

- **AC-HRM-11-01 · Intentos invalidados.** *Dado* el adaptador falso con una conexión que queda pendiente, *cuando* el usuario busca, elige otro sensor, desconecta u olvida (o se cierra la app), *entonces* ese intento: no cambia el estado visible cuando por fin responde, no recuerda nada (tras `Olvidar`, las preferencias siguen vacías), no publica lecturas y cierra su enlace en silencio, **salvo** que la acción nueva apunte al mismo sensor (no le corta el intento nuevo). Una conexión manual reemplazada no muestra error. [dotnet] · `hrs:66-69`, `:229-234`, `:277-293`, `:372-383`
- **AC-HRM-11-02 · Carreras.** *Entonces* si mientras se cerraba un intento fallido arrancó otra acción, ese fallo no toca el estado ni muestra error; mientras hay una búsqueda del usuario, la reconexión automática no hace nada. [dotnet] · `hrs:211`, `:299-301`
- **AC-HRM-11-03 · Cierre limpio.** *Cuando* se cierra la app, *entonces* se detienen búsqueda, intento, cadencias y watchdog, se sueltan los enlaces del pulsómetro y no queda ningún temporizador vivo. [dotnet] · `hrs:435-447`

### F-HRM-16 · Compatibilidad

- **AC-HRM-16-01 · Remo y pulsómetro a la vez.** *Dado* `K-ROW` y `K-HRM` conectados, *entonces* los dos publican a la vez; el keepalive del remo sigue cada 2 s; una caída del pulsómetro no toca al remo, y una caída, el watchdog o `Desconectar` del remo no tocan al pulsómetro. [dotnet]
- **AC-HRM-16-02 · Bluetooth no disponible.** *Dado* el Bluetooth apagado, sin permiso o sin Bluetooth LE, *entonces* la tarjeta no se muestra (vista de la 10a) y las cadencias en curso siguen fallando con su ritmo; al volver el Bluetooth, la tarjeta reaparece en el estado que corresponda. En Android, buscar pulsómetros usa el mismo permiso "Dispositivos cercanos" que el remo. [dotnet] · [Device-Android] · `ds:367`

### Tarjeta, HUD y sesión con el sensor real

- **AC-HRM-13-13 · Cada estado de la tarjeta, con el servicio real.** *Entonces* la tabla de AC-HRM-13-03 se cumple con el pulsómetro real sobre `K-11`, incluidas las filas que en la It. 2 solo se veían con el pulsómetro de prueba: `Conectando a {nombre}...` (manual), `{nombre} · reconectando...` (conectando o esperando con *reintentando*) y `{nombre} · no encontrado`. [Editor] · [Captura] CAP-01 a CAP-08
- **AC-HRM-13-14 · Errores en lenguaje humano (Q-42).** *Entonces* el error de la tarjeta es texto suelto, sin caja ni ícono: una línea en 12 dp `state.dangerText` con `hrmScanFailed` (`No se pudo buscar sensores`), `hrmConnectFailed` (`No se pudo conectar con {name}`) o `hrmIncompatible`, y debajo, salvo en incompatible, el detalle técnico del sistema en 11 dp `text.tertiary`, sin traducir; en total hasta 3 líneas con "…". Los disparadores son los del legacy; reemplaza `hrmConnectError` en la tarjeta. [Editor] · [Captura] CAP-09 a CAP-11 · `hc:52-62`
- **AC-WKT-05-07 · El sensor real manda en el HUD y en la sesión.** *Dado* `K-ROW` mandando 90 bpm por FTMS y `K-HRM` mandando 142, *entonces* la tarjeta BPM del HUD (vertical y horizontal) muestra 142, los puntos guardados tienen 142 y el detalle de la sesión calcula promedio y máximo con el pulso del sensor; en una **caída** el pulso pasa a 0 y el próximo paquete FTMS repone el del monitor (LT-36): si el monitor no manda pulso (AMS-670B), queda en 0, la tarjeta BPM desaparece y esos puntos guardan 0; con el **watchdog** disparado (sensor conectado) el pulso queda en 0 y el FTMS no lo pisa; al volver, otra vez el del sensor (gate QA-G11-01: paridad con `wp:220-237` y LT-36). La grilla del remo en Dispositivo sigue mostrando solo el pulso FTMS. [dotnet] · [Editor] · [Captura] CAP-13 · `workout_provider.dart:220-237`; `ds:257-258`
- **AC-HRM-15-13 · El simulado no cambia.** *Entonces* el build `simulator` sigue usando el pulsómetro simulado (AC-HRM-15-01 a -12 y LT-32 siguen pasando) y nunca abre el Bluetooth real ni escribe `hrm.*`. [dotnet] · [Editor]

### Seguridad y equipo real

- **AC-HRM-SEC-01 · Solo lo que el usuario eligió.** *Entonces* el pulsómetro solo se conecta a un sensor que el usuario tocó en la lista (todos anuncian `0x180D`) o al recordado; nunca empareja, desempareja ni cambia la radio; con `K-HP` nunca hay conexión; y nada versionado (código, docs, logs de prueba, capturas) tiene nombres ni direcciones reales. [dotnet] · revisión de QA
- **AC-HRM-HW-01 · Demo en Windows.** *Entonces* los pasos 1 a 9 de la demo de Windows (§1) se cumplen con un sensor real y el AMS-670B. [BLE-HW] · NO VERIFICABLE en el workflow
- **AC-HRM-HW-02 · Demo en Android.** *Entonces* la demo de Android (§1) se cumple, incluido el pulso que sigue llegando tras 60 s en segundo plano. [Device-Android] · [BLE-HW] · NO VERIFICABLE en el workflow

---

## 4. Textos nuevos (F-APP-10, Q-01)

Cinco claves nuevas: **245 por idioma** (240 + 5). Las existentes `hrm*` no cambian (sus `...` se quedan); las nuevas usan `…` (U+2026). `hrmConnectError` sigue en el catálogo, pero la tarjeta ya no la usa (Q-42).

| Clave | es | en | Uso |
|---|---|---|---|
| `hrmNoSignal` | `Sin señal del sensor` | `No signal from the sensor` | tarjeta, con el watchdog disparado (AC-HRM-05-03) |
| `hrmScanFailed` | `No se pudo buscar sensores` | `Couldn't search for sensors` | error de búsqueda (AC-HRM-13-14) |
| `hrmConnectFailed` | `No se pudo conectar con {name}` | `Couldn't connect to {name}` | error de conexión (AC-HRM-13-14) |
| `hrmConnectedNotice` | `Pulsómetro conectado` | `Heart rate sensor connected` | aviso de 2 s (AC-HRM-07-05) |
| `hrmLostNotice` | `Se perdió el pulsómetro, reconectando…` | `Heart rate sensor lost, reconnecting…` | aviso de 4 s sobre el HUD (AC-HRM-08-03) |

`{name}` = el nombre elegido en la lista, o el recordado, o `hrmUnknownSensor`. El detalle técnico va sin traducir. Los registros `[HRM] …` quedan fijos en español, como el legacy.

---

## 5. Casos legacy a portar (`test/reconnect_loop_test.dart`)

| LT | Línea | Caso | Qué comprueba (reloj de prueba) | AC |
|---|---|---|---|---|
| LT-139 | `rlt:9` | intenta de inmediato, reintenta cada intervalo y para al tener éxito | 3 s, éxito al 3.º: 1 intento a los 0 s, 2 a los 3 s, 3 a los 6 s y para; 30 s después siguen 3 | AC-HRM-09-01 |
| LT-140 | `rlt:32` | se rinde tras maxAttempts y avisa | 10 s, máximo 3: en 25 s, 3 intentos, avisó y paró; 60 s más, siguen 3 | AC-HRM-09-01 |
| LT-141 | `rlt:55` | cancel frena los reintentos y una excepción cuenta como fallo | 1 s, siempre error: 3 intentos en 2 s; tras cancelar, ninguno en 10 s | AC-HRM-09-01 |
| LT-142 | `rlt:75` | start() durante un intento en vuelo descarta ese resultado y arranca de cero | el reinicio intenta ya (contador 1); el éxito tardío del viejo no para la corrida nueva; tras un fallo, a los 3 s hay un 3.er intento (contador 2) | AC-HRM-09-01, AC-HRM-11-01 |
| LT-143 | `rlt:105` | cancel() durante un intento en vuelo no programa otro intento | 1 intento en 10 s y no corre | AC-HRM-09-01, AC-HRM-11-01 |
| LT-144 | `rlt:126` | start() desde onGiveUp arranca una corrida nueva | máximo 2, 1 s: al rendirse reinicia (3 intentos, corre); al rendirse otra vez (4) para | AC-HRM-09-01 |
| LT-145 | `rlt:157` | StaleWatchdog: dispara tras el timeout sin touch, y touch lo rearma | 10 s: no dispara hasta los 19 s; una sola vez; tocar y detener → no dispara | AC-HRM-05-01 |

Se vuelven a correr con el servicio real sobre `K-11`: LT-28 a LT-31 (tarjeta y exclusión) y LT-35 a LT-38 (fusión).

---

## 6. Checklist de paridad

| Comportamiento legacy | Fuente | AC |
|---|---|---|
| Búsqueda de 10 s, solo `0x180D`, sin duplicados, solo esta búsqueda; fin a desconectado | `hrs:108-183`; `sf:12-20` | AC-HRM-02-01 a -03 |
| Exclusión de búsquedas con el remo | `ds:348`, `:397`; `hc:22-24` | AC-HRM-02-04 |
| Registro `[HRM]` de búsqueda, conexión, caída y reintentos | `hrs:130`, `:139`, `:224`, `:267`, `:276`; `m:74` | AC-HRM-02-05, -03-01, -07-02, -08-01, AC-APP-01-10 |
| Conexión directa 15 s, sin MTU, `0x180D`/`0x2A37`, notificaciones, recordar, vigilancia, watchdog | `hrs:229-276` | AC-HRM-03-01 |
| Incompatible y fallas: cerrar enlace, error, sin reintentos | `hrs:248-253`, `:294-304` | AC-HRM-03-02, -03 |
| Conectar mientras conecta se ignora | `hrs:189-192` | AC-HRM-03-04 |
| `hrm.deviceId` / `hrm.deviceName`, nombre solo con id | `hrs:34-35`, `:405-421` | AC-HRM-06-01 a -03 |
| Arranque: 6 intentos × 10 s, sin buscar, fallos solo al registro | `hrs:40-42`, `:207-227`; `m:68-77` | AC-HRM-07-01 a -04, AC-APP-01-10 |
| Caída: pulso 0 antes del estado, cada 3 s sin límite | `hrs:44-45`, `:263-272` | AC-HRM-08-01, -02 |
| Watchdog de 10 s, una vez, sin cortar | `hrs:38`, `:86-89`; `rl:75-91` | AC-HRM-05-01, -02 |
| Desconectar olvida; Olvidar aborta y borra | `hrs:337-363` | AC-HRM-10-01, -02 |
| Invalidación de intentos en vuelo y cierre | `hrs:66-69`, `:277-301`, `:372-383`, `:435-447` | AC-HRM-11-01 a -03 |
| Tarjeta: reconectando con botones, no encontrado, conectando manual | `hc:68-94`, `:198-226` | AC-HRM-07-03, AC-HRM-13-13 |
| El sensor manda sobre el FTMS en el entrenamiento | `workout_provider.dart:220-237` | AC-WKT-05-07 |

---

## 7. Capturas (todas con el adaptador falso `K-11`)

Español salvo que diga `en`. Ningún nombre ni dirección real.

| ID | Qué | Tamaños |
|---|---|---|
| CAP-01 | Sin sensor (remo desconectado: vista de búsqueda + tarjeta) | TV, W1280 |
| CAP-02 | Buscando con `Banda de prueba`, `Reloj de prueba` y `Sensor desconocido` | TV, W1280 |
| CAP-03 | Lista del pulsómetro en horizontal, sin desborde | L640, TH |
| CAP-04 | `Conectando a Banda de prueba...` | TV |
| CAP-05 | Conectado con `142` bpm; también `en` | TV, W1280; TV `en` |
| CAP-06 | Lectura vencida: `--` + `Sin señal del sensor` | TV |
| CAP-07 | `Banda de prueba · reconectando...` con los tres botones; también `en` | TV; TV `en` |
| CAP-08 | `Banda de prueba · no encontrado` | TV, TH |
| CAP-09 | Error incompatible (`K-NOHR`) | TV |
| CAP-10 | Error de conexión con detalle técnico | TV |
| CAP-11 | Error de búsqueda con detalle técnico | TV |
| CAP-12 | Aviso `Pulsómetro conectado` en Dispositivo y sobre el HUD | TV; W1280 |
| CAP-13 | HUD con el pulso del sensor (`142`) con el remo conectado | W1280, TV, TH |
| CAP-14 | HUD con `Se perdió el pulsómetro, reconectando…` | W1280, TV |
| CAP-15 | Remo y pulsómetro conectados (tarjeta bajo la grilla) | TV, W1280 |
| CAP-16 | Remo buscando: `Buscar` del pulsómetro deshabilitado | TV |

---

## 8. Fuera de alcance

- iOS y macOS (It. 10b): Bluetooth del pulsómetro y sus criterios [Apple].
- Indicador explícito de "sin contacto" (Q-46: `--` y la tarjeta BPM desaparece), intensidad de señal y botón para detener la búsqueda (Q-39), `Cancelar` durante la conexión manual (Q-45), deshacer al desconectar (Q-33).
- Zonas de frecuencia cardíaca, batería del sensor (`0x180F`), más de un sensor a la vez, pulso del sensor en la grilla del remo (HRM-16.12).
- Guía ilustrada de emparejamiento de smartwatches; recordar el remo (Q-30); servicio en primer plano de Android.
- Importar las preferencias de la app Flutter: hay que elegir el sensor una vez.

---

## 9. Decisiones del PO (una línea cada una)

| ID | Decisión |
|---|---|
| DV-106 | Q-41: un sensor nuevo sin nombre borra `hrm.deviceName` y se muestra `Sensor desconocido` (ni el id ni el nombre del anterior). |
| DV-107 | Q-42: error = frase humana por situación + detalle técnico debajo; el error de búsqueda ya no dice "Error al conectar". |
| DV-108 | `Pulsómetro conectado` dura 2 s y solo sale con reconexiones automáticas (al abrir, `Conectar`, tras una caída). |
| DV-109 | Caída en entrenamiento: `Se perdió el pulsómetro, reconectando…` 4 s (no fijo, para no ocupar el lugar del remo); los avisos del remo tienen prioridad y un aviso del pulsómetro que choca se descarta. |
| DV-110 | Watchdog disparado: `--` + `Sin señal del sensor` y corazón al 40 %; antes de la primera lectura, solo `--` (paridad). |
| DV-111 | Bluetooth apagado durante una caída: sigue intentando cada 3 s y reconecta al encenderlo (como DV-97 del remo; el legacy hace lo mismo por no tener límite). |
| DV-112 | Paridad en Q-33, Q-43 y Q-44: sin confirmación al desconectar, `Buscar otro` sin elección no reanuda la reconexión y la lista desaparece al vencer. |
| DV-113 | La reconexión va directo a la dirección recordada, sin buscar, también en Windows; si el sistema no la encuentra, cuenta como intento fallido sin error visible. |
| DV-114 | Android en segundo plano: sin servicio en primer plano (como la 10a), hasta ver en la demo si el sistema corta el enlace. |
| DV-115 | Al abrir la app con el estado del adaptador todavía desconocido, el primer intento sale igual (paridad); si falla, cuenta en la cadencia. |

---

## 10. Para issues

- Indicador "sin contacto" en lenguaje humano ("Ajustá la banda") en la tarjeta y el HUD (Q-46).
- Búsqueda del pulsómetro con "radar", tiempo restante, intensidad de señal y "No encontramos pulsómetros cerca" al vencer sin resultados.
- Guía de emparejamiento para smartwatches (Heart for Bluetooth, HeartCast) desde la tarjeta.
- Verificar con un reloj real si las direcciones privadas rotativas impiden reconectar (HRM-16.6) y, si pasa, sugerir `Buscar otro` en `· no encontrado`.
- Recordar el nombre elegido en la lista cuando el sistema no da nombre (hoy queda `Sensor desconocido`).
- `Cancelar` durante una conexión manual de hasta 15 s (Q-45).
- Sonido o vibración sutil al conectar el pulsómetro.

---

## 11. Cambios por gate ([03-qa-gate](03-qa-gate.md))

| Issue | Cambio |
|---|---|
| QA-G11-01 | AC-WKT-05-07: en una caída el pulso pasa a 0 y el próximo paquete FTMS repone el del monitor (LT-36); con el watchdog queda en 0 y el FTMS no lo pisa. AC-HRM-08-03 y el paso 7 de la demo: la tarjeta BPM desaparece **si el monitor no manda pulso**. Motivo: la spec contradecía LT-36, que ella misma manda volver a correr |
