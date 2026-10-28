# Capturas de la It. 2

Generadas por el arnés (`tools/unity/capture-snapshots.ps1 -Set it2`, 02-tech-design §4) en modo edición, con la tubería GammaRt. Detalle de cada toma (sondas, auditoría, elementos) en [manifest.json](manifest.json); la medición de R-T1 en [rt1-measurement.json](rt1-measurement.json).

| ID | Archivo | Perfil | Backend | Estado | Sondas | Auditoría | Comprobaciones |
|---|---|---|---|---|---|---|---|
| AUX-01 | [AUX-01-tarjeta-conectada-en.png](AUX-01-tarjeta-conectada-en.png) | TV | Sim | La tarjeta con -- en inglés | 0/0 | sin desbordes | ok |
| AUX-02 | [AUX-02-dispositivo-tv-reducir-movimiento.png](AUX-02-dispositivo-tv-reducir-movimiento.png) | TV | Sim | CAP-01 con reducir movimiento (debe verse igual) | 0/0 | sin desbordes | ok |
| CAP-01 | [CAP-01-dispositivo-tv.png](CAP-01-dispositivo-tv.png) | TV | Sim | Dispositivo; remo simulado conectado; 60 s a 150 W / 24 spm; pulsómetro sin conectar | 8/8 | sin desbordes | ok |
| CAP-02 | [CAP-02-dispositivo-tv-en.png](CAP-02-dispositivo-tv-en.png) | TV | Sim | Igual que CAP-01, con el sistema en inglés | 0/0 | sin desbordes | ok |
| CAP-03 | [CAP-03-dispositivo-l640.png](CAP-03-dispositivo-l640.png) | L640 | Sim | Igual que CAP-01 en 640 × 360 | 1/1 | sin desbordes | ok |
| CAP-04a | [CAP-04a-dispositivo-w1280.png](CAP-04a-dispositivo-w1280.png) | W1280 | Sim | Pulsómetro conectado (60 s); Dispositivo sin desplazar | 0/0 | sin desbordes | ok |
| CAP-04b | [CAP-04b-dispositivo-w1280-hrm.png](CAP-04b-dispositivo-w1280-hrm.png) | W1280 | Sim | Pulsómetro conectado (60 s); Dispositivo desplazada hasta el final | 1/1 | sin desbordes | ok |
| CAP-05 | [CAP-05-dispositivo-th.png](CAP-05-dispositivo-th.png) | TH | Sim | Igual que CAP-01 en el teléfono horizontal | 0/0 | sin desbordes | ok |
| CAP-06 | [CAP-06-grilla-pulso.png](CAP-06-grilla-pulso.png) | TV | Prueba | Remo de prueba conectado que manda 140 de pulso por FTMS | 2/2 | sin desbordes | ok |
| CAP-07 | [CAP-07-valores-largos.png](CAP-07-valores-largos.png) | TV | Prueba | Remo de prueba: split 6001 s, 40,5 spm, 1999 W, 123 456 m | 0/0 | sin desbordes | ok |
| CAP-08 | [CAP-08-busqueda-tv.png](CAP-08-busqueda-tv.png) | TV | Sim | Remo desconectado con Desconectar | 1/1 | sin desbordes | ok |
| CAP-09 | [CAP-09-busqueda-l640-hrm.png](CAP-09-busqueda-l640-hrm.png) | L640 | Sim | Remo desconectado; pulsómetro buscando | 0/0 | sin desbordes | ok |
| CAP-10 | [CAP-10-tarjeta-sin-sensor.png](CAP-10-tarjeta-sin-sensor.png) | TV | Sim | Pulsómetro sin conectar | 1/1 | sin desbordes | ok |
| CAP-11 | [CAP-11-tarjeta-buscando.png](CAP-11-tarjeta-buscando.png) | TV | Sim | Pulsómetro buscando | 1/1 | sin desbordes | ok |
| CAP-12a | [CAP-12a-tarjeta-conectada.png](CAP-12a-tarjeta-conectada.png) | TV | Sim | Pulsómetro recién conectado | 0/0 | sin desbordes | ok |
| CAP-12b | [CAP-12b-tarjeta-conectada-60s.png](CAP-12b-tarjeta-conectada-60s.png) | TV | Sim | Pulsómetro conectado, 60 s después | 2/2 | sin desbordes | ok |
| CAP-13 | [CAP-13-tarjeta-reconectando.png](CAP-13-tarjeta-reconectando.png) | TV | Sim | Caída simulada del pulsómetro (dentro de los 3 s) | 0/0 | sin desbordes | ok |
| CAP-14a | [CAP-14a-tarjeta-conectando.png](CAP-14a-tarjeta-conectando.png) | TV | Prueba | Conectando al recordado (queda conectando) | 0/0 | sin desbordes | ok |
| CAP-14b | [CAP-14b-tarjeta-no-encontrado.png](CAP-14b-tarjeta-no-encontrado.png) | TV | Prueba | Recordado, desconectado, sin reintentar | 0/0 | sin desbordes | ok |
| CAP-14c | [CAP-14c-tarjeta-incompatible.png](CAP-14c-tarjeta-incompatible.png) | TV | Prueba | Conectar lanza HrmIncompatibleException | 1/1 | sin desbordes | ok |
| CAP-14d | [CAP-14d-tarjeta-error-largo.png](CAP-14d-tarjeta-error-largo.png) | TV | Prueba | Conectar lanza un error de 300 caracteres | 0/0 | sin desbordes | ok |
| CAP-15 | [CAP-15-tarjeta-remo-buscando.png](CAP-15-tarjeta-remo-buscando.png) | TV | Prueba | Remo de prueba buscando; pulsómetro sin conectar | 0/0 | sin desbordes | ok |
| CAP-16 | [CAP-16-panel-p800.png](CAP-16-panel-p800.png) | P800 | Sim | Panel recién abierto sobre Dispositivo | 1/1 | sin desbordes | ok |
| CAP-17 | [CAP-17-panel-p800-final.png](CAP-17-panel-p800-final.png) | P800 | Sim | Panel desplazado al final, con la hora forzada en 14:00 | 0/0 | sin desbordes | ok |
| CAP-18 | [CAP-18-panel-tv.png](CAP-18-panel-tv.png) | TV | Sim | Panel abierto sobre Dispositivo | 0/0 | sin desbordes | ok |
| CAP-19 | [CAP-19-panel-limites.png](CAP-19-panel-limites.png) | TV | Sim | Panel con 500 W, 40 spm, hora 0:00 y Remando apagado | 0/0 | sin desbordes | ok |
| CAP-20a | [CAP-20a-barra-dispositivo.png](CAP-20a-barra-dispositivo.png) | TV | Sim | Destino elegido: dispositivo | 0/0 | sin desbordes | ok |
| CAP-20b | [CAP-20b-barra-workout.png](CAP-20b-barra-workout.png) | TV | Sim | Destino elegido: workout | 0/0 | sin desbordes | ok |
| CAP-20c | [CAP-20c-barra-rutinas.png](CAP-20c-barra-rutinas.png) | TV | Sim | Destino elegido: rutinas | 0/0 | sin desbordes | ok |
| CAP-20d | [CAP-20d-barra-historial.png](CAP-20d-barra-historial.png) | TV | Sim | Destino elegido: historial | 0/0 | sin desbordes | ok |
| CAP-20e | [CAP-20e-barra-en.png](CAP-20e-barra-en.png) | TV | Sim | Barra en inglés, Dispositivo elegido | 0/0 | sin desbordes | ok |
| CAP-21a | [CAP-21a-workout.png](CAP-21a-workout.png) | TV | Sim | Pestaña provisoria: Entrenamiento | 1/1 | sin desbordes | ok |
| CAP-21b | [CAP-21b-rutinas.png](CAP-21b-rutinas.png) | TV | Sim | Rutinas (desde la It. 3 la pantalla real, base vacía) | 0/0 | sin desbordes | ok |
| CAP-21c | [CAP-21c-historial.png](CAP-21c-historial.png) | TV | Sim | Pestaña provisoria: Historial | 1/1 | sin desbordes | ok |
| CAP-22 | [CAP-22-build-normal-w1280.png](CAP-22-build-normal-w1280.png) | W1280 | Unavailable | Build normal (sin simulador): equipo sin Bluetooth LE, vista de búsqueda vacía | 1/1 | sin desbordes | ok |
| CAP-23 | [CAP-23-medicion-rt1.png](CAP-23-medicion-rt1.png) | 400x240 | — | Muestra de medición de R-T1 (AC-BASE-05): las dos composiciones lado a lado | 1/1 | sin desbordes | ok |
| CAP-24 | [CAP-24-letra-130.png](CAP-24-letra-130.png) | TV | Sim | Letra del sistema en 1,3×, remo y pulsómetro conectados | 0/0 | sin desbordes | ok |
| CAP-25 | [CAP-25-strava-5-destinos.png](CAP-25-strava-5-destinos.png) | TV | Sim | Configuración de prueba con Strava | 0/0 | sin desbordes | ok |
| VID-01 | [VID-01-tablero.mp4](VID-01-tablero.mp4) | TV | Sim | Remo y pulsómetro conectados, a 150 W y 24 spm, desde los 60 s; 10 s | 0/0 | sin desbordes | ok |
| VID-02 | [VID-02-pestanas.mp4](VID-02-pestanas.mp4) | TV | Sim | Recorrido por las 4 pestañas; 10 s | 0/0 | sin desbordes | ok |
| VID-03 | [VID-03-caida-remo.mp4](VID-03-caida-remo.mp4) | TV | Sim | Simular desconexión (desde el panel) y vuelta; 8 s | 0/0 | sin desbordes | ok |
