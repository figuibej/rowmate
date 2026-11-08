# It. 8: Río, Costa, Regata y selector de escenario (reporte de cierre)

| Campo | Valor |
|---|---|
| Estado | **Cerrada, esperando tu OK.** El PO la aceptó sin criterios en FAIL |
| QA | Ronda 1: RECHAZADO (4 mayores). Ronda 2: **APROBADO CON OBSERVACIONES**, sin mayores ([05-qa-verification](05-qa-verification.md), "Ronda 2") |
| Criterios | 40: **39 PASS · 0 FAIL · 1 NO VERIFICABLE** (60 fps en Android, sin teléfono) |
| Casos legacy | LT-99 a LT-138: los 40 portados y pasando. Con esto quedan portados todos los tests de `test/scene/` del legacy |
| Build | `Builds/Windows/simulator` **ya tiene la 8** (lo generó QA en la ronda 2 con el código final) |

---

## 1. Qué se entregó

- **Tres escenarios nuevos** que se suman al Lago alpino, con el mismo paisaje que el legacy en el mismo metro. Usan los mismos generadores, que coinciden bit a bit en 100 km, y cada objeto cae en la misma x de pantalla que en el legacy (diferencia medida: 0 dp).
  - **Río urbano**: muro de piedra, paseo, edificios y clubes de remo, faroles y boyas amarillas. Un puente de piedra cada 800 m (el primero a los 270 m): la cámara pasa por debajo del arco y la luz baja. De noche se encienden las ventanas, los faroles y el skyline.
  - **Costa**: mar abierto a la derecha con oleaje que mueve el bote (arfada, cabeceo y rolido propios). Acantilados con estratos, pasto y espuma al pie, y playas con sombrillas. Un faro cada 1 000 m (el primero a los 530 m), con halo y haz giratorio de noche. Cinco gaviotas cruzan el cielo.
  - **Canal de regata**: 3 carriles con boyas cada 10 m, rojas, amarillas y blancas cada 250 m. Carteles `250 m`, `500 m`… con la distancia de escena. Tribuna con público a los 210 m, torre de llegada a los 610 m, banderas que flamean, álamos, y tribunas y banderas lejanas en el horizonte.
- **Mundo vivo**: con el tiempo de escena (también en pausa y con "reducir movimiento"), las banderas flamean, las boyas cabecean, el público se mueve, el haz del faro gira y las gaviotas aletean.
- **Selector de escenario** en Workout en reposo: título `Escenario` y 4 tarjetas con miniaturas horneadas de cada lugar a las 12:00. Funciona aunque el remo no esté conectado, se desplaza de costado si no entra y se maneja con teclado (Tab, flechas, Enter). El elegido tiene borde celeste.
- **La elección se recuerda**: se guarda en `scene.environment` del archivo de preferencias. Sin la clave, o con un valor desconocido, arranca en el lago. Al reabrir la app aparece elegida desde el primer cuadro. El entrenamiento (libre o rutina) usa el escenario elegido.
- **Rendimiento**: los 4 escenarios respetan el presupuesto en los 3 niveles de calidad y no reservan memoria por cuadro. En el build, el p99 fue ≤ 5,3 ms en todas las corridas (el tope es 16,7 ms).
- Todo el arte nuevo es procedural o propio, detrás de slots. No hay que bajar nada.

---

## 2. Cómo probarlo en `Builds/Windows/simulator`

Abrí un solo `RowMate.exe` a la vez. Si aparece el aviso del Firewall, contestá `Cancelar`. Si la app está en inglés, los nombres son `Scenery`, `Alpine lake`, `City river`, `Coast` y `Regatta course`.

**(a) El selector** (2 min)
1. Abrí `RowMate.exe` y andá a **Workout**. Arriba de `Entrenamiento libre` vas a ver `Escenario` y las 4 tarjetas: `Lago alpino` (elegido, con borde celeste), `Río urbano`, `Costa` y `Canal de regata`. → [DEMO-01](snapshots/DEMO-01-selector-lago.png)
2. Tocá `Río urbano`: el borde celeste pasa a esa tarjeta con una animación corta.
3. Opcional: con el teclado, Tab hasta la fila, ← → para moverte y Enter para elegir.

**(b) Remar en el río** (3 min)
1. Con `Río urbano` elegido, tocá `Entrenamiento libre`, abrí la **llave** del simulador, elegí `Fuerte` y cerrá el panel.
2. Vas a remar entre el muro, los edificios y los faroles. Cerca de los 270 m de la escena la cámara pasa por debajo del puente sin cortes.
3. Llave → `Hora escena` → `22:00`: se encienden las ventanas, los faroles y el skyline. → [DEMO-02](snapshots/DEMO-02-rio-noche.png), [CAP-12](snapshots/CAP-12-river-22-tv.png)
4. `Hora real` → `Terminar` → `Terminar`.

**(c) Remar en la costa** (3 min)
1. En Workout elegí `Costa` → `Entrenamiento libre` → llave → `Fuerte`.
2. El mar abierto queda a la derecha, con franjas de oleaje, y el bote sube, baja y rola. A la izquierda hay acantilados con espuma al pie y playas con sombrillas, y se ven gaviotas en el cielo. → [DEMO-03](snapshots/DEMO-03-costa.png)
3. El faro pasa cerca de los 530 m. Con `Hora escena` `22:00` se ven el halo y el haz giratorio. → [CAP-20](snapshots/CAP-20-coast-22-tv.png)
4. `⏸ Pausa`: el mar sigue moviéndose y el bote sigue la ola.

**(d) Remar en la regata** (3 min)
1. Elegí `Canal de regata` → `Entrenamiento libre` → llave → `Fuerte`.
2. Vas por el carril del centro, entre boyas rojas. Cerca de los 210 m pasa la tribuna con público a la derecha. A los 250 m pasa el cartel `250 m` a la izquierda y las boyas se vuelven amarillas. Las banderas flamean. → [DEMO-04](snapshots/DEMO-04-regata.png), [CAP-26](snapshots/CAP-26-regatta-12-tv.png)
3. La torre de llegada aparece a los 610 m.

**(e) Reiniciar** (1 min)
1. Con `Canal de regata` elegido, cerrá la app y volvé a abrirla.
2. Workout muestra `Canal de regata` elegido desde el primer cuadro, sin pasar por el lago. `Entrenamiento libre` va directo a la regata. → [DEMO-05](snapshots/DEMO-05-reabierta-regata.png), [VID-06](snapshots/VID-06-reinicio-costa-w1280.mp4)

Nota: si entrás al libre apenas abrís Workout, la primera vez la escena puede tardar en aparecer (QA vio el fondo provisorio liso hasta unos 20 s). Va a issues.

---

## 3. Resultados (salida real, ronda 2 de QA)

| Qué | Resultado |
|---|---|
| dotnet (.NET 8) | Build con 0 avisos y 0 errores. Herramientas **192 / 192**. Core **3350: 3311 pasan, 0 fallan, 39 omitidos** |
| Matriz AC → test | PASA 700, FALLA 0, IGNORADO 4, PENDIENTE UNITY 157 (los cubren las suites de Unity), SIN TEST 0 |
| Unity EditMode | **3453: 3408 pasan, 0 fallan, 45 ignorados**. Con simulador: **3645: 3600 pasan, 0 fallan, 45 ignorados** |
| Unity PlayMode | **551 / 551**. Con simulador: **624 / 624**. Incluyen `EnvironmentParityTests` 3 / 3 (máx 0 dp), `EnvironmentBudgetTests` 12 / 12, `SceneBudgetTests` 10 / 10, los 25 casos LT-111 a -123 y LT-127 a -138, y `TenSwitchesKeepTheMemoryFlat` (memoria −0,01 % en 10 cambios) |
| Tomas | 57 (CAP-01 a -46, VID-01 a -06, DEMO-01 a -05), iguales byte a byte entre dos corridas |
| Build y `[PERF]` (W1280, Alto) | p99 del cuadro ≤ 5,3 ms en las 13 corridas, 0 asignaciones de GC en régimen y 0 subidas de shaders después de `Ready` en 12 de 13. Hubo 3 tirones aislados (> 33 ms) que no se repitieron en 5 corridas más → issue |
| Demo con clics reales | Los 4 escenarios elegidos, remados y reabiertos: 4 de 4 |
| Higiene | ProjectSettings sin cambios, repo sin rutas de la PC, nada de terceros versionado, UTF-8 limpio y legacy sin cambios |

---

## 4. Criterios que no pasan o no se pueden verificar

| AC | Estado | Por qué |
|---|---|---|
| AC-SCN-01-19 (rendimiento) | **NO VERIFICABLE solo en Android** | En la PC cumple. Los 60 fps en un Android de gama media se miden cuando haya teléfono (It. 14) |

No hay criterios en FAIL. Lo que quedó menor va a issues (§7).

---

## 5. Capturas

Índice completo: [snapshots/INDEX.md](snapshots/INDEX.md) (sondas y valores en `manifest.json`).

| Qué | Tomas |
|---|---|
| Selector | [CAP-37 vertical](snapshots/CAP-37-selector-tv.png), [CAP-38 W1280](snapshots/CAP-38-selector-w1280.png), [CAP-39 inglés con foco](snapshots/CAP-39-selector-tv-en.png) |
| Miniaturas | [Lago](snapshots/CAP-33-miniatura-lake.png), [Río](snapshots/CAP-34-miniatura-river.png), [Costa](snapshots/CAP-35-miniatura-coast.png), [Regata](snapshots/CAP-36-miniatura-regatta.png) |
| Río | [12 h](snapshots/CAP-10-river-12-tv.png), [22 h](snapshots/CAP-12-river-22-tv.png), [bajo el puente](snapshots/CAP-43-rio-puente-tv.png), [VID-02](snapshots/VID-02-rio-th.mp4) |
| Costa | [12 h](snapshots/CAP-18-coast-12-tv.png), [22 h](snapshots/CAP-20-coast-22-tv.png), [la ola](snapshots/CAP-42-costa-ola-th.png), [VID-03](snapshots/VID-03-costa-tv.mp4), [VID-05 en pausa](snapshots/VID-05-costa-pausa-th.mp4) |
| Regata | [12 h](snapshots/CAP-26-regatta-12-tv.png), [22 h](snapshots/CAP-32-regatta-22-th.png), [cartel 250 m](snapshots/CAP-40-regata-cartel-tv.png), [cartel de noche](snapshots/CAP-41-regata-noche-th.png), [VID-04](snapshots/VID-04-regata-th.mp4) |
| Contra el legacy | [CAP-44 río](snapshots/CAP-44-rio-marcas-v390.png), [CAP-45 costa](snapshots/CAP-45-costa-marcas-v390.png), [CAP-46 regata](snapshots/CAP-46-regata-marcas-v390.png), al lado de [LEG-01](snapshots/legacy/LEG-01-marcas.png), [LEG-02](snapshots/legacy/LEG-02-marcas.png) y [LEG-03](snapshots/legacy/LEG-03-marcas.png) |
| Lago (sin cambios) | [CAP-02](snapshots/CAP-02-lake-12-tv.png), [CAP-04](snapshots/CAP-04-lake-22-tv.png) |
| Demo | [DEMO-01](snapshots/DEMO-01-selector-lago.png) a [DEMO-05](snapshots/DEMO-05-reabierta-regata.png) |

Lo que se ve mal (menor): en horizontal, a las 22 h, la linterna del faro y la del farol quedan debajo de la barra superior del HUD y solo se ve el haz ([CAP-24](snapshots/CAP-24-coast-22-th.png), [CAP-16](snapshots/CAP-16-river-22-th.png)). El público de la tribuna son figuras simples (cajas con movimiento).

---

## 6. Decisiones por defecto (respondé solo si querés cambiar alguna)

| Decisión | Por defecto | Si la rechazás |
|---|---|---|
| DV-79: tarjetas de 152 × 96 con el nombre en 13 | Se acepta | 120 × 80 y 12, como el legacy |
| DV-80: miniatura horneada a las 12:00 | Se acepta | Miniaturas a la hora actual (4 por escenario) |
| DV-81 y DV-87: las siluetas lejanas se acercan sin deslizarse, con el tope de 9° | Se acepta | Como el legacy |
| DV-82: el número del cartel siempre en el tablero | Se acepta | Aparece recién a los ≈ 54 m |
| DV-83: faro con halo y haz giratorio | Se acepta | Solo el halo |
| DV-84: muro, paseo, espuma, arena, taludes, álamos y público | Se acepta | Sin público ni álamos |
| DV-85 y DV-86: rolido por escenario, la costa mueve el bote y muestra el oleaje | Se acepta | Rolido del lago en todos y costa calma |
| DV-88 y DV-89: boyas que cabecean y gaviotas 3D | Se acepta | Boyas quietas y gaviotas planas |
| DT8-10: faro sobre un promontorio de 2,5 m | Se acepta | Acantilado entero (la base del faro queda más alta que en el legacy) |
| QA-V8-14: la linterna bajo la barra en horizontal de noche | Se acepta como está (issue) | El Designer ajusta el encuadre ya |
| D-ART-08: sin texturas CC0 de fachadas | Sigue en No | Texturas CC0 detrás de slots |

**Decididas por el PO al cerrar** (no necesitan respuesta):
- AC-SCN-01-19 queda NO VERIFICABLE solo en Android.
- La regata se acepta medida remando de 0 a ≈ 250 m (QA-V8-12).
- El **issue #9 sigue abierto**. Ya pasan las sondas de la cabaña, las ventanas, el club, el skyline y el faro en vertical, pero el farol todavía no tiene una sonda que pase. Va a un issue, junto con el encuadre.
- El worktree `.claude/worktrees/it08-entornos` y su rama quedan hasta que digas que se borren (ya están ignorados por git).

---

## 7. Deuda: va a issues de GitHub

1. Bajar el costo del shader de espuma (Foam) de 62 a ≤ 30 ALU.
2. Resto del #9: sonda del farol y linterna bajo la barra en horizontal.
3. Tirones aislados en el build de simulador, más un argumento para arrancar en una distancia.
4. La escena demorada tarda en aparecer si se entra enseguida al libre.
5. Pruebas pendientes de la It. 8.
6. Pulido del arte de los escenarios (público con poses, sombra de gaviotas, reloj de la torre, ventanas del skyline, muro).
7. Selector y textos: hover, curva en Core y panel del simulador en inglés.
8. Limpieza del repo: worktree de la It. 8 y fin de línea LF.
9. Backlog de escenarios (mejoras opcionales).
10. Para la 6b: reflejos, columnas de luz y espuma de crestas.

---

## 8. Próximo paso

1. **Vos**: probar §2 (unos 15 minutos) y dar el OK o cambiar alguna decisión de §6. Decir también si se borra el worktree.
2. Después, la **It. 9: Historial y detalle con gráficos** (`09-historial`). La 6b sigue postergada (issue #6).
