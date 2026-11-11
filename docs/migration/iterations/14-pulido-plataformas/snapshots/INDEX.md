# Capturas de la It. 14 (Pulido, plataformas y deuda)

Generadas por el arnés (`tools/unity/capture-snapshots.ps1`, 03-rutinas/02-tech-design §5) en modo edición, con la tubería GammaRt, bases de prueba (TempDatabase) y el reloj de prueba. Detalle de cada toma (sondas, auditoría, elementos) en [manifest.json](manifest.json). CAP-27 (ventana del build normal) no es del arnés: `tools/unity/capture-cap27.ps1`, a pedido.

| ID | Archivo | Perfil | Backend | Estado | Sondas | Auditoría | Comprobaciones |
|---|---|---|---|---|---|---|---|
| CAP-01 | [CAP-01.png](CAP-01.png) | TV | — | Ícono: maestro 1024 y 256 a 16 sobre claro y oscuro | 0/0 | sin desbordes | origen tools/icon (dotnet run --project tools/icon) |
| CAP-02 | [CAP-02.png](CAP-02.png) | TV | — | Adaptativo de Android con las máscaras | 0/0 | sin desbordes | origen tools/icon (dotnet run --project tools/icon) |
| CAP-03 | [CAP-03-arranque-w1280.png](CAP-03-arranque-w1280.png) | W1280 | Prueba | Primer cuadro del arranque: #0D1B2A y la marca centrada | 0/0 | sin desbordes | ok |
| CAP-04 | [CAP-04-arranque-tv.png](CAP-04-arranque-tv.png) | TV | Prueba | Primer cuadro del arranque: #0D1B2A y la marca centrada | 0/0 | sin desbordes | ok |
| CAP-05 | [CAP-05-dispositivo-l640.png](CAP-05-dispositivo-l640.png) | L640 | FakeBle | Ventana mínima 640 × 360: Dispositivo conectado, sin desbordes | 0/0 | sin desbordes | ok |
| CAP-06 | [CAP-06-workout-l640-en.png](CAP-06-workout-l640-en.png) | L640 | R-4 | Ventana mínima: el HUD con el Lago alpino, sin desbordes | 0/0 | sin desbordes | ok |
| CAP-07 | [CAP-07-dispositivo-tv.png](CAP-07-dispositivo-tv.png) | TV | FakeBle | Dispositivo conectado con las métricas tabulares | 0/0 | sin desbordes | ok |
| CAP-08 | [CAP-08-rutinas-th-en.png](CAP-08-rutinas-th-en.png) | TH | R-4 | Rutinas: la lista con los resúmenes en inglés | 0/0 | sin desbordes | ok |
| CAP-09 | [CAP-09-editor-escalera-tv-en.png](CAP-09-editor-escalera-tv-en.png) | TV | Prueba | Editor de Escalera con duraciones y objetivos tabulares | 0/0 | sin desbordes | ok |
| CAP-10 | [CAP-10-hud-rio-tv.png](CAP-10-hud-rio-tv.png) | TV | R-4 | HUD con Río urbano y la barra de pasos | 0/0 | sin desbordes | ok |
| CAP-11 | [CAP-11-hud-costa-th-en.png](CAP-11-hud-costa-th-en.png) | TH | R-4 | HUD con Costa y Up next: | 0/0 | sin desbordes | ok |
| CAP-12 | [CAP-12-historial-tv-en.png](CAP-12-historial-tv-en.png) | TV | Prueba | Historial: tarjetas con fecha, duración y distancia | 0/0 | sin desbordes | ok |
| CAP-13 | [CAP-13-completada-l640-13.png](CAP-13-completada-l640-13.png) | L640 | R-4 | Diálogo de rutina completada centrado (AC-POL-04) | 0/0 | sin desbordes | ok |
| CAP-14 | [CAP-14-hud-regata-th.png](CAP-14-hud-regata-th.png) | TH | R-4 | HUD con Canal de regata | 0/0 | sin desbordes | ok |
| CAP-15 | [CAP-15-reconectando-tv-13.png](CAP-15-reconectando-tv-13.png) | TV | FakeBle | Reconectando con AMS-670B RowMaster 2024… completo (#40) | 0/0 | sin desbordes | ok |
| CAP-16 | [CAP-16-buscando-tv.png](CAP-16-buscando-tv.png) | TV | FakeBle | Buscando... con el indicador blanco (#41) | 0/0 | sin desbordes | ok |
| CAP-17 | [CAP-17-buscando-th.png](CAP-17-buscando-th.png) | TH | FakeBle | Buscando en TH: lista y pulsómetro con un solo desplazamiento (#42) | 0/0 | sin desbordes | ok |
| CAP-18 | [CAP-18-error-w1280.png](CAP-18-error-w1280.png) | W1280 | FakeBle | Error al escanear: error 2 (#44) | 0/0 | sin desbordes | ok |
| CAP-19 | [CAP-19-aviso-tv.png](CAP-19-aviso-tv.png) | TV | Prueba | Guardado en Documentos\RowMate\TCX sobre el gráfico (#69) | 0/0 | sin desbordes | ok |
| CAP-20 | [CAP-20-aviso-hover-w1280-en.png](CAP-20-aviso-hover-w1280-en.png) | W1280 | Prueba | Saved to Documents\RowMate\TCX con el puntero encima (#69) | 0/0 | sin desbordes | ok |
| CAP-21 | [CAP-21-historial-p360-13-en.png](CAP-21-historial-p360-13-en.png) | P360 | Prueba | Historial: columnas achicadas enteras, 4 dp de aire (#36) | 0/0 | sin desbordes | ok |
| CAP-22 | [CAP-22-detalle-tv.png](CAP-22-detalle-tv.png) | TV | Prueba | Detalle de Escalera con el gráfico a la vista | 0/0 | sin desbordes | ok |
| CAP-23 | [CAP-23-detalle-w1280.png](CAP-23-detalle-w1280.png) | W1280 | Prueba | Detalle de Escalera con el gráfico a la vista | 0/0 | sin desbordes | ok |
| CAP-24 | [CAP-24-push-editor-w1280.png](CAP-24-push-editor-w1280.png) | W1280 | Prueba | Cuadro a mitad del push del editor (120 ms): sin cuadro vacío | 0/0 | sin desbordes | ok |
| CAP-25 | [CAP-25-historial-error-tv-en.png](CAP-25-historial-error-tv-en.png) | TV | Prueba | Error del Historial con su ícono de 56 dp (#37) | 0/0 | sin desbordes | ok |
| CAP-26 | [CAP-26-preparando-escena-tv.png](CAP-26-preparando-escena-tv.png) | TV | R-4 | Primer entrenamiento: Preparando escena… con el HUD andando (#21) | 0/0 | sin desbordes | ok |
| CAP-27 | [CAP-27-lector-w1280.png](CAP-27-lector-w1280.png) | W1280 | Prueba | Detalle con el lector forzado: CAP-27-a11y.txt (AC-APP-03-12) | 0/0 | sin desbordes | ok |
| VID-01 | [VID-01-transiciones-w1280.mp4](VID-01-transiciones-w1280.mp4) | W1280 | Prueba | Pestañas, push y pop del editor (6 s) | 0/0 | sin desbordes | ok |
| VID-02 | [VID-02-rotar-tv-th.mp4](VID-02-rotar-tv-th.mp4) | TV | R-4 | Libre a los 20 s: rotar de TV a TH (2 s) | 0/0 | sin desbordes | ok |
