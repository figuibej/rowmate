# Capturas de la It. 9 (Historial y detalle con gráficos)

Generadas por el arnés (`tools/unity/capture-snapshots.ps1`, 03-rutinas/02-tech-design §5) en modo edición, con la tubería GammaRt, bases de prueba (TempDatabase) y el reloj de prueba. Detalle de cada toma (sondas, auditoría, elementos) en [manifest.json](manifest.json). CAP-27 (ventana del build normal) no es del arnés: `tools/unity/capture-cap27.ps1`, a pedido.

| ID | Archivo | Perfil | Backend | Estado | Sondas | Auditoría | Comprobaciones |
|---|---|---|---|---|---|---|---|
| CAP-01 | [CAP-01-lista-tv.png](CAP-01-lista-tv.png) | TV | Prueba | F-9, Historial | 0/2 ⚠ aspecto: metric.watts #000000 vs #FFD60A; metric.hr #000000 vs #EF476F | sin desbordes | ok |
| CAP-02 | [CAP-02-lista-th.png](CAP-02-lista-th.png) | TH | Prueba | F-9, Historial en el teléfono horizontal | 0/0 | sin desbordes | ok |
| CAP-03 | [CAP-03-lista-p360-en-130.png](CAP-03-lista-p360-en-130.png) | P360 | Prueba | F-9, Historial en inglés con letra 1,3× | 0/0 | sin desbordes | ok |
| CAP-04 | [CAP-04-lista-w1280.png](CAP-04-lista-w1280.png) | W1280 | Prueba | F-9, Historial en el escritorio | 0/0 | sin desbordes | ok |
| CAP-05 | [CAP-05-vacia-tv.png](CAP-05-vacia-tv.png) | TV | Prueba | F-9V, Historial | 0/0 | sin desbordes | ok |
| CAP-06 | [CAP-06-detalle-libre-tv.png](CAP-06-detalle-libre-tv.png) | TV | Prueba | F-9 n.º 2 (libre), Vatios | 0/0 | sin desbordes | ok |
| CAP-07 | [CAP-07-detalle-escalera-vatios-tv.png](CAP-07-detalle-escalera-vatios-tv.png) | TV | Prueba | F-9 n.º 1 (Escalera), Vatios | 1/1 | sin desbordes | ok |
| CAP-08 | [CAP-08-detalle-spm-tv.png](CAP-08-detalle-spm-tv.png) | TV | Prueba | F-9 n.º 1, SPM, desplazado hasta el gráfico | 0/0 | sin desbordes | ok |
| CAP-09 | [CAP-09-detalle-split-tv.png](CAP-09-detalle-split-tv.png) | TV | Prueba | F-9 n.º 1, Split 500m, desplazado hasta el gráfico | 0/0 | sin desbordes | ok |
| CAP-10 | [CAP-10-detalle-pulso-tv.png](CAP-10-detalle-pulso-tv.png) | TV | Prueba | F-9 n.º 1, Pulso, desplazado hasta el gráfico | 0/0 | sin desbordes | ok |
| CAP-11 | [CAP-11-detalle-distancia-tv.png](CAP-11-detalle-distancia-tv.png) | TV | Prueba | F-9 n.º 1, Distancia, desplazado hasta el gráfico | 0/0 | sin desbordes | ok |
| CAP-12 | [CAP-12-detalle-cortada-tv.png](CAP-12-detalle-cortada-tv.png) | TV | Prueba | F-9 n.º 3 (cortada), Vatios | 0/0 | sin desbordes | ok |
| CAP-13 | [CAP-13-detalle-sin-datos-tv.png](CAP-13-detalle-sin-datos-tv.png) | TV | Prueba | F-9 n.º 4 (sin puntos) | 0/0 | sin desbordes | ok |
| CAP-14 | [CAP-14-detalle-th.png](CAP-14-detalle-th.png) | TH | Prueba | F-9 n.º 1, Vatios, desplazado hasta el gráfico | 0/0 | sin desbordes | ok |
| CAP-15 | [CAP-15-cursor-w1280.png](CAP-15-cursor-w1280.png) | W1280 | Prueba | F-9 n.º 1, Split 500m, cursor en 300 s | 0/0 | sin desbordes | ok |
| CAP-16 | [CAP-16-detalle-rutina-borrada-tv.png](CAP-16-detalle-rutina-borrada-tv.png) | TV | Prueba | F-9 n.º 6 (Rutina borrada), Vatios | 0/0 | sin desbordes | ok |
| CAP-17 | [CAP-17-error-tv.png](CAP-17-error-tv.png) | TV | Prueba | F-ROTA, Historial | 0/0 | sin desbordes | ok |
| DEMO-01 | [DEMO-01-libre-arriba.png](DEMO-01-libre-arriba.png) | W1280 | Sim | Libre ≈ 2 min y Terminar; Historial: arriba, 6 columnas | 0/0 | sin desbordes | ok |
| DEMO-02 | [DEMO-02-demo9-remada.png](DEMO-02-demo9-remada.png) | W1280 | Sim | La rutina Demo 9 (calentamiento 1:00; trabajo 2:00 140-160 W, 24 SPM, 2:13; enfriamiento 1:00) remada entera | 0/0 | sin desbordes | ok |
| DEMO-03 | [DEMO-03-detalle-vatios.png](DEMO-03-detalle-vatios.png) | W1280 | Sim | Detalle de Demo 9: resumen, 3 filas y Vatios con franjas y objetivos 140/160 | 0/0 | sin desbordes | ok |
| DEMO-04 | [DEMO-04-split-cursor.png](DEMO-04-split-cursor.png) | W1280 | Sim | Split 500m con el cursor en el trabajo | 0/0 | sin desbordes | ok |
| DEMO-05 | [DEMO-05-cortada.png](DEMO-05-cortada.png) | W1280 | Sim | Demo 9 otra vez, cerrar la app a mitad del trabajo y volver a abrir: la cortada en Historial | 0/0 | sin desbordes | ok |
| VID-01 | [VID-01-recarga-sin-parpadeo.mp4](VID-01-recarga-sin-parpadeo.mp4) | TV | Prueba | F-9: Historial → una sesión terminada (el cierre recarga) → dos toques en Historial (10 s) | 0/0 | sin desbordes | ok |
| VID-02 | [VID-02-metricas.mp4](VID-02-metricas.mp4) | TV | Prueba | F-9 n.º 1: abrir y pasar por las 5 métricas cada 1,2 s (8 s) | 0/0 | sin desbordes | ok |
