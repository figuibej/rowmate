# Capturas de la It. 5 (Series en vivo)

Generadas por el arnés (`tools/unity/capture-snapshots.ps1`, 03-rutinas/02-tech-design §5) en modo edición, con la tubería GammaRt, bases de prueba (TempDatabase) y el reloj de prueba. Detalle de cada toma (sondas, auditoría, elementos) en [manifest.json](manifest.json). CAP-27 (ventana del build normal) no es del arnés: `tools/unity/capture-cap27.ps1`, a pedido.

| ID | Archivo | Perfil | Backend | Estado | Sondas | Auditoría | Comprobaciones |
|---|---|---|---|---|---|---|---|
| CAP-01 | [CAP-01-chips-tv.png](CAP-01-chips-tv.png) | TV | R-4 | Series 600 a los 60,2 s, cerrados | 0/0 | sin desbordes | ok |
| CAP-02 | [CAP-02-parciales-abierto.png](CAP-02-parciales-abierto.png) | TV | R-4 | 170,2 s, parciales abierto | 3/3 | sin desbordes | ok |
| CAP-03 | [CAP-03-los-dos-abiertos.png](CAP-03-los-dos-abiertos.png) | TV | R-4 | 250,2 s, los dos abiertos, contorno | 5/5 | sin desbordes | ok |
| CAP-04 | [CAP-04-dos-repeticiones.png](CAP-04-dos-repeticiones.png) | TV | R-4 | 380,2 s, los dos | 2/2 | sin desbordes | ok |
| CAP-05 | [CAP-05-vacios-parejos.png](CAP-05-vacios-parejos.png) | TV | R-4 | 190,2 s, los dos (Descanso) | 2/2 | sin desbordes | ok |
| CAP-06 | [CAP-06-segundo-de-atraso.png](CAP-06-segundo-de-atraso.png) | TV | R-4 | 180,6 s, los dos | 0/0 | sin desbordes | ok |
| CAP-07 | [CAP-07-fuera-de-la-serie.png](CAP-07-fuera-de-la-serie.png) | TV | R-4 | 545,2 s, los dos en verdadero | 0/0 | sin desbordes | ok |
| CAP-08 | [CAP-08-cuatro-vueltas.png](CAP-08-cuatro-vueltas.png) | TV | R-4 | Fondo 3000 con R-4A a los 600,2 s, parciales | 0/0 | sin desbordes | ok |
| CAP-09 | [CAP-09-interpolada.png](CAP-09-interpolada.png) | TV | R-4 | Trabajo 1000 con R-3 a los 170,2 s, parciales | 0/0 | sin desbordes | ok |
| CAP-10 | [CAP-10-tres-repeticiones.png](CAP-10-tres-repeticiones.png) | TV | R-4 | Cinco reps a los 545,2 s, los dos | 0/0 | sin desbordes | ok |
| CAP-11 | [CAP-11-escala-comun.png](CAP-11-escala-comun.png) | TV | R-4 | Pirámide 500 a los 881,2 s, los dos (DT5-04) | 0/0 | sin desbordes | ok |
| CAP-12 | [CAP-12-en-ingles.png](CAP-12-en-ingles.png) | TV | R-4 | En inglés, CAP-03 | 0/0 | sin desbordes | ok |
| CAP-13 | [CAP-13-vacios-en-ingles.png](CAP-13-vacios-en-ingles.png) | TV | R-4 | En inglés, CAP-05 | 0/0 | sin desbordes | ok |
| CAP-14 | [CAP-14-p360.png](CAP-14-p360.png) | P360 | R-4 | CAP-04 en P360, contorno | 1/1 | sin desbordes | ok |
| CAP-15 | [CAP-15-th-chips.png](CAP-15-th-chips.png) | TH | R-4 | CAP-01 en TH, contorno | 0/0 | sin desbordes | ok |
| CAP-16 | [CAP-16-th-mascara.png](CAP-16-th-mascara.png) | TH | R-4 | CAP-04 en TH, contorno | 2/2 | sin desbordes | ok |
| CAP-17 | [CAP-17-l640-mascara.png](CAP-17-l640-mascara.png) | L640 | R-4 | CAP-04 en L640, contorno | 0/0 | sin desbordes | ok |
| CAP-18 | [CAP-18-w1280.png](CAP-18-w1280.png) | W1280 | R-4 | W1280 a los 340,2 s, los dos, contorno | 3/3 | sin desbordes | ok |
| CAP-19 | [CAP-19-cuenta-grande-con-chips.png](CAP-19-cuenta-grande-con-chips.png) | TV | R-4 | 197,6 s | 3/3 | sin desbordes | ok |
| CAP-20 | [CAP-20-cuenta-grande-con-parciales.png](CAP-20-cuenta-grande-con-parciales.png) | TV | R-4 | 197,6 s, parciales | 3/3 | sin desbordes | ok |
| CAP-21 | [CAP-21-th-cuenta-grande.png](CAP-21-th-cuenta-grande.png) | TH | R-4 | CAP-19 en TH, los dos | 1/1 | sin desbordes | ok |
| CAP-22 | [CAP-22-pausa.png](CAP-22-pausa.png) | TV | R-4 | Pausa a los 250,2 s, toma a los 252,2 | 0/0 | sin desbordes | ok |
| CAP-23 | [CAP-23-th-pausa.png](CAP-23-th-pausa.png) | TH | R-4 | CAP-22 en TH | 0/0 | sin desbordes | ok |
| CAP-24 | [CAP-24-sello-y-columna.png](CAP-24-sello-y-columna.png) | TV | R-4 | Prueba 4a a los 90,6 s, los dos | 0/0 | sin desbordes | ok |
| CAP-25 | [CAP-25-s-largo.png](CAP-25-s-largo.png) | TV | R-4 | S-LARGO (OverrideSeries), los dos | 0/0 | sin desbordes | ok |
| CAP-26 | [CAP-26-anillo-de-foco.png](CAP-26-anillo-de-foco.png) | W1280 | R-4 | CAP-18 sin foco y Tab × 3 | 2/2 | sin desbordes | ok |
| DEMO-01 | [DEMO-01-workout.png](DEMO-01-workout.png) | W1280 | Sim | Workout en reposo; Series 600 primera | 0/0 | sin desbordes | ok |
| DEMO-02 | [DEMO-02-chip.png](DEMO-02-chip.png) | W1280 | Sim | Series 600 a los 10,2 s | 0/0 | sin desbordes | ok |
| DEMO-03 | [DEMO-03-parciales.png](DEMO-03-parciales.png) | W1280 | Sim | 500 m a los 12 s; toma a los 12,8 | 0/0 | sin desbordes | ok |
| DEMO-04 | [DEMO-04-serie.png](DEMO-04-serie.png) | W1280 | Sim | A los 60,2 s | 0/0 | sin desbordes | ok |
| DEMO-05 | [DEMO-05-repeticiones.png](DEMO-05-repeticiones.png) | W1280 | Sim | Reps a los 61; toma a los 61,8 | 0/0 | sin desbordes | ok |
| DEMO-06 | [DEMO-06-primera-vuelta.png](DEMO-06-primera-vuelta.png) | W1280 | Sim | A los 170,2 s | 0/0 | sin desbordes | ok |
| DEMO-07 | [DEMO-07-repeticion.png](DEMO-07-repeticion.png) | W1280 | Sim | A los 230,2 s | 0/0 | sin desbordes | ok |
| DEMO-08 | [DEMO-08-desconexion.png](DEMO-08-desconexion.png) | W1280 | Sim | Simular desconexión a los 240; toma a los 242,2 | 0/0 | sin desbordes | ok |
| DEMO-09 | [DEMO-09-sigue.png](DEMO-09-sigue.png) | W1280 | Sim | Panel cerrado; a los 250,2 s | 0/0 | sin desbordes | ok |
| DEMO-10 | [DEMO-10-reposo.png](DEMO-10-reposo.png) | W1280 | Sim | Terminar → Terminar; 800 ms de la salida | 0/0 | sin desbordes | ok |
| DEMO-11 | [DEMO-11-recordados.png](DEMO-11-recordados.png) | W1280 | Sim | Series 600 otra vez; a los 31,6 s | 0/0 | sin desbordes | ok |
| DEMO-12 | [DEMO-12-cerrados.png](DEMO-12-cerrados.png) | W1280 | Sim | Cerrar los dos; Terminar → Terminar; 800 ms de la salida | 0/0 | sin desbordes | ok |
| VID-01 | [VID-01-abrir-y-cerrar.mp4](VID-01-abrir-y-cerrar.mp4) | TV | R-4 | Series 600 a los 250 s, repeticiones abierto; 500 m a los 250,3 y a los 251,8 (3 s) | 0/0 | sin desbordes | ok |
| VID-02 | [VID-02-vuelta-nueva.mp4](VID-02-vuelta-nueva.mp4) | TV | R-4 | Series 600 de 323,5 a 327,5 s, los dos (la vuelta a los 325) | 0/0 | sin desbordes | ok |
| VID-03 | [VID-03-cambio-a-descanso.mp4](VID-03-cambio-a-descanso.mp4) | TV | R-4 | Series 600 de 178,5 a 184,5 s, los dos | 0/0 | sin desbordes | ok |
| VID-04 | [VID-04-cambio-a-trabajo.mp4](VID-04-cambio-a-trabajo.mp4) | TV | R-4 | Series 600 de 198,5 a 203,5 s, los dos | 0/0 | sin desbordes | ok |
| VID-05 | [VID-05-desconexion.mp4](VID-05-desconexion.mp4) | W1280 | Sim | Simulador sin ruido de 238 a 248 s, parciales; Simular desconexión a los 240 | 0/0 | sin desbordes | ok |
| VID-06 | [VID-06-entrada-con-paneles.mp4](VID-06-entrada-con-paneles.mp4) | TV | R-4 | Cinco reps → Iniciar con los dos en verdadero (2,5 s) | 0/0 | sin desbordes | ok |
| VID-07 | [VID-07-salida-con-paneles.mp4](VID-07-salida-con-paneles.mp4) | TV | R-4 | 252,2 s, los dos; Terminar → Terminar antes del 253 (2,5 s) | 0/0 | sin desbordes | ok |
| VID-08 | [VID-08-reducir-movimiento.mp4](VID-08-reducir-movimiento.mp4) | TV | R-4 | VID-02 con reducir movimiento; parciales cerrado y repeticiones abierto; 500 m a los 323,8 | 0/0 | sin desbordes | ok |
| VID-09 | [VID-09-cuenta-grande-y-panel.mp4](VID-09-cuenta-grande-y-panel.mp4) | TV | R-4 | 196,5 a 199,5 s, cerrados; 500 m a los 197,3 | 0/0 | sin desbordes | ok |
