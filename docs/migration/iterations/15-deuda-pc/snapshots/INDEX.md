# Capturas de la It. 15 (Deuda resoluble en la PC)

Generadas por el arnés (`tools/unity/capture-snapshots.ps1`, 03-rutinas/02-tech-design §5) en modo edición, con la tubería GammaRt, bases de prueba (TempDatabase) y el reloj de prueba. Detalle de cada toma (sondas, auditoría, elementos) en [manifest.json](manifest.json). CAP-27 (ventana del build normal) no es del arnés: `tools/unity/capture-cap27.ps1`, a pedido.

| ID | Archivo | Perfil | Backend | Estado | Sondas | Auditoría | Comprobaciones |
|---|---|---|---|---|---|---|---|
| CAP-01 | [CAP-01-historial-exportar-tv.png](CAP-01-historial-exportar-tv.png) | TV | Prueba | Historial: botón de exportar, Vatios y split m:ss | 0/0 | sin desbordes | ok |
| CAP-02 | [CAP-02-tarjeta-guardado-w1280-en.png](CAP-02-tarjeta-guardado-w1280-en.png) | W1280 | Prueba | Exportar desde la tarjeta: Saved to … sobre la barra | 0/0 | sin desbordes | ok |
| CAP-03 | [CAP-03-paso-marcado-tv.png](CAP-03-paso-marcado-tv.png) | TV | Prueba | Detalle de Escalera: fila 2 marcada y sus franjas al 22 % | 0/0 | sin desbordes | ok |
| CAP-04 | [CAP-04-split-cursor-w1280.png](CAP-04-split-cursor-w1280.png) | W1280 | Prueba | Split 500m sin picos, resumen m:ss y la sombra del tooltip | 0/0 | sin desbordes | ok |
| CAP-05 | [CAP-05-detalle-glifo-w1280.png](CAP-05-detalle-glifo-w1280.png) | W1280 | Prueba | Barra del Detalle con el glifo download de la fuente | 0/0 | sin desbordes | ok |
| CAP-06 | [CAP-06-abrir-carpeta-fallo-w1280.png](CAP-06-abrir-carpeta-fallo-w1280.png) | W1280 | Prueba | Abrir carpeta falla: No se pudo abrir la carpeta | 0/0 | sin desbordes | ok |
| CAP-07 | [CAP-07-hud-l640-13.png](CAP-07-hud-l640-13.png) | L640 | R-4 | HUD: Vatios, Split 500m y kcal con 0,04 em, letra 1,3× | 0/0 | sin desbordes | ok |
| CAP-08 | [CAP-08-hud-tv-en.png](CAP-08-hud-tv-en.png) | TV | R-4 | HUD en inglés: Watts, Split 500m | 0/0 | sin desbordes | ok |
| CAP-09 | [CAP-09-rutinas-simulador-w1280.png](CAP-09-rutinas-simulador-w1280.png) | W1280 | Sim | Rutinas con el simulador: la cinta no tapa el título | 0/0 | sin desbordes | ok |
| CAP-10 | [CAP-10-lago-w1280.png](CAP-10-lago-w1280.png) | W1280 | R-4 | Lago W1280, nivel Alto, como hoy (referencia de AUX-15-01) | 0/0 | sin desbordes | ok |

## Tomas auxiliares (diagnóstico, fuera del índice de arte)

| ID | Archivo | Perfil | Backend | Estado | Sondas | Auditoría | Comprobaciones |
|---|---|---|---|---|---|---|---|
| AUX-15-01 | [AUX-15-01-lago-sin-casco-w1280.png](AUX-15-01-lago-sin-casco-w1280.png) | W1280 | R-4 | Diagnóstico: el cuadro de CAP-10 con el casco oculto | 0/0 | sin desbordes | ok |
