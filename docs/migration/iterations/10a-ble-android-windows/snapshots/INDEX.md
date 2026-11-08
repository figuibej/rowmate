# Capturas de la It. 10a (Bluetooth real: Dispositivo y avisos del enlace)

Generadas por el arnés (`tools/unity/capture-snapshots.ps1`, 03-rutinas/02-tech-design §5) en modo edición, con la tubería GammaRt, bases de prueba (TempDatabase) y el reloj de prueba. Detalle de cada toma (sondas, auditoría, elementos) en [manifest.json](manifest.json). CAP-27 (ventana del build normal) no es del arnés: `tools/unity/capture-cap27.ps1`, a pedido.

| ID | Archivo | Perfil | Backend | Estado | Sondas | Auditoría | Comprobaciones |
|---|---|---|---|---|---|---|---|
| AUX-10a-01 | [AUX-10a-01-hud-tv-peor-caso-aviso.png](AUX-10a-01-hud-tv-peor-caso-aviso.png) | TV | Prueba (4b) | HUD de TV en el peor caso de la 4b + "Reconectando con AMS-670B…" con el contorno de depuración | 0/0 | sin desbordes | ok |
| AUX-10a-02-0 | [AUX-10a-02a-0ms.png](AUX-10a-02a-0ms.png) | TV | Remo real + adaptador falso (K-10) | 0 ms: la vista de estado | 0/0 | sin desbordes | ok |
| AUX-10a-02-240 | [AUX-10a-02b-240ms.png](AUX-10a-02b-240ms.png) | TV | Remo real + adaptador falso (K-10) | 240 ms | 0/0 | sin desbordes | ok |
| AUX-10a-02-480 | [AUX-10a-02c-480ms.png](AUX-10a-02c-480ms.png) | TV | Remo real + adaptador falso (K-10) | 480 ms | 0/0 | sin desbordes | ok |
| AUX-10a-02-720 | [AUX-10a-02d-720ms.png](AUX-10a-02d-720ms.png) | TV | Remo real + adaptador falso (K-10) | 720 ms: la búsqueda | 0/0 | sin desbordes | ok |
| CAP-01a | [CAP-01a-bt-apagado-tv.png](CAP-01a-bt-apagado-tv.png) | TV | Remo real + adaptador falso (K-10) | Adaptador apagado | 0/0 | sin desbordes | ok |
| CAP-01b | [CAP-01b-bt-apagado-w1280.png](CAP-01b-bt-apagado-w1280.png) | W1280 | Remo real + adaptador falso (K-10) | Adaptador apagado en el escritorio | 0/0 | sin desbordes | ok |
| CAP-02 | [CAP-02-bt-apagado-tv-en.png](CAP-02-bt-apagado-tv-en.png) | TV | Remo real + adaptador falso (K-10) | Adaptador apagado, en inglés | 0/0 | sin desbordes | ok |
| CAP-03 | [CAP-03-sin-permiso-tv.png](CAP-03-sin-permiso-tv.png) | TV | Remo real + adaptador falso (K-10) | Sin permiso de Bluetooth | 0/0 | sin desbordes | ok |
| CAP-04 | [CAP-04-sin-ble-tv.png](CAP-04-sin-ble-tv.png) | TV | Remo real + adaptador falso (K-10) | Equipo sin Bluetooth LE | 0/0 | sin desbordes | ok |
| CAP-05 | [CAP-05-buscando-tv.png](CAP-05-buscando-tv.png) | TV | Remo real + adaptador falso (K-10) | Buscar, sin anuncios, a los 1 200 ms | 0/0 | sin desbordes | ok |
| CAP-06a | [CAP-06a-lista-tv.png](CAP-06a-lista-tv.png) | TV | Remo real + adaptador falso (K-10) | Anuncios Hp, Row, Anon, Bike, Row; 10 s vencidos | 0/0 | sin desbordes | ok |
| CAP-06b | [CAP-06b-lista-w1280.png](CAP-06b-lista-w1280.png) | W1280 | Remo real + adaptador falso (K-10) | Anuncios Hp, Row, Anon, Bike, Row; 10 s vencidos | 0/0 | sin desbordes | ok |
| CAP-07a | [CAP-07a-lista-pulsometro-l640.png](CAP-07a-lista-pulsometro-l640.png) | L640 | Remo real + adaptador falso (K-10) | Anuncios Hp, Row, Anon, Bike, Row; 10 s vencidos; tarjeta del pulsómetro de prueba | 0/0 | sin desbordes | ok |
| CAP-07b | [CAP-07b-lista-pulsometro-th.png](CAP-07b-lista-pulsometro-th.png) | TH | Remo real + adaptador falso (K-10) | Anuncios Hp, Row, Anon, Bike, Row; 10 s vencidos; tarjeta del pulsómetro de prueba | 0/0 | sin desbordes | ok |
| CAP-08 | [CAP-08-error-escanear-tv.png](CAP-08-error-escanear-tv.png) | TV | Remo real + adaptador falso (K-10) | FailNextScan(BleException("scan", "error 2")) | 0/0 | sin desbordes | ok |
| CAP-09 | [CAP-09-conectando-tv.png](CAP-09-conectando-tv.png) | TV | Remo real + adaptador falso (K-10) | Tocar AMS-670B: Conectando… (el connect tarda 1 s) | 0/0 | sin desbordes | ok |
| CAP-10 | [CAP-10-tomando-control-tv.png](CAP-10-tomando-control-tv.png) | TV | Remo real + adaptador falso (K-10) | Tomando control (tras 0x00, a los 250 de los 300 ms; el cruce de 200 ya terminó) | 0/0 | sin desbordes | ok |
| CAP-11 | [CAP-11-listo-tv.png](CAP-11-listo-tv.png) | TV | Remo real + adaptador falso (K-10) | Connected + 235 ms de los 240 de "Listo": el check entero | 0/0 | sin desbordes | ok |
| CAP-12 | [CAP-12-error-conectar-tv.png](CAP-12-error-conectar-tv.png) | TV | Remo real + adaptador falso (K-10) | AMS-670B en NeverRespond, 15 s | 0/0 | sin desbordes | ok |
| CAP-13a | [CAP-13a-conectado-tv.png](CAP-13a-conectado-tv.png) | TV | Remo real + adaptador falso (K-10) | AMS-670B conectado con el paquete de ftms.json | 0/0 | sin desbordes | ok |
| CAP-13b | [CAP-13b-conectado-w1280.png](CAP-13b-conectado-w1280.png) | W1280 | Remo real + adaptador falso (K-10) | AMS-670B conectado con el paquete de ftms.json | 0/0 | sin desbordes | ok |
| CAP-14 | [CAP-14-reconectando-tv.png](CAP-14-reconectando-tv.png) | TV | Remo real + adaptador falso (K-10) | DropLink(AMS-670B) y 1 500 ms de la espera | 0/0 | sin desbordes | ok |
| CAP-15a | [CAP-15a-hud-reconectando-w1280.png](CAP-15a-hud-reconectando-w1280.png) | W1280 | Remo real + adaptador falso (K-10) | Libre + DropLink: reconectando sobre el HUD | 0/0 | sin desbordes | ok |
| CAP-15b | [CAP-15b-hud-reconectando-tv.png](CAP-15b-hud-reconectando-tv.png) | TV | Remo real + adaptador falso (K-10) | Libre + DropLink: reconectando sobre el HUD | 0/0 | sin desbordes | ok |
| CAP-16 | [CAP-16-hud-reconectado-w1280-en.png](CAP-16-hud-reconectado-w1280-en.png) | W1280 | Remo real + adaptador falso (K-10) | Respond → Reconnected to AMS-670B (2 s), en inglés | 0/0 | sin desbordes | ok |
| CAP-17 | [CAP-17-monitor-apagado-tv.png](CAP-17-monitor-apagado-tv.png) | TV | Remo real + adaptador falso (K-10) | 6 s sin paquetes tras el primero: Dispositivo | 0/0 | sin desbordes | ok |
| CAP-18 | [CAP-18-hud-monitor-apagado-w1280.png](CAP-18-hud-monitor-apagado-w1280.png) | W1280 | Remo real + adaptador falso (K-10) | 6 s sin paquetes tras el primero: HUD | 0/0 | sin desbordes | ok |
| CAP-19a | [CAP-19a-diagnostico-tv.png](CAP-19a-diagnostico-tv.png) | TV | Remo real + adaptador falso (K-10) | Gesto + panel con AMS-670B y paquetes (presión de la cabecera) | 0/0 | sin desbordes | ok |
| CAP-19b | [CAP-19b-diagnostico-w1280.png](CAP-19b-diagnostico-w1280.png) | W1280 | Remo real + adaptador falso (K-10) | Gesto + panel con AMS-670B y paquetes (presión de la cabecera) | 0/0 | sin desbordes | ok |
| CAP-20 | [CAP-20-diagnostico-sin-datos-tv.png](CAP-20-diagnostico-sin-datos-tv.png) | TV | Remo real + adaptador falso (K-10) | Bici FTMS sin paquetes (presión de la cabecera) | 0/0 | sin desbordes | ok |
