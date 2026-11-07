# It. 7: remero, bote y palada (reporte de cierre)

| | |
|---|---|
| Estado | **Cerrada, esperando tu OK.** El PO la acepta y QA la aprobó con observaciones (ronda 2) |
| Criterios | **37 cumplen, 0 fallan y 1 no se pudo verificar** (de 38) |
| Base | La escena de la 6a con el agua degradada. La 6b sigue postergada (issue #6) |
| Detalle | [01-po-spec](01-po-spec.md) · [02-tech-design](02-tech-design.md) · [04-design-notes](04-design-notes.md) · [05-qa-verification](05-qa-verification.md) · [snapshots/](snapshots/INDEX.md) |

## 1. Qué se entregó

- **Un remero 3D que rema al ritmo del SPM.** Hace la secuencia piernas → tronco → brazos y vuelve. El drive casi no cambia y la recuperación se alarga a pocas paladas: a 20 spm, drive de ≈ 1,0 s y recuperación de ≈ 2,0 s; a 30 spm, 0,82 s y 1,18 s. Mira hacia la cámara y las manos van pegadas al puño del remo. La palada es todo código (IK propio), sin animaciones de Mixamo.
- **El maniquí propio es el remero por defecto** y la app funciona completa sin descargar nada. **Si Y Bot está, el remero es Y Bot**, con la misma palada. Si el archivo falta o no es válido, vuelve el maniquí y queda un único aviso en el registro.
- **Un single completo con medidas reales**: casco, riggers, carro que se mueve, pedalera, remos de 2,88 m y pala celeste con punta blanca. La pala gira suave al salir del agua y se cuadra antes de entrar, y la caña se flexiona más cuando hacés más fuerza.
- **El bote está vivo**: cabecea como en el legacy, sube y baja un poco, rola apenas y "empuja" en cada palada. La cámara lo acompaña sin pasarse de ± 3 dp en el horizonte.
- **El agua responde**: salpicadura en cada ataque (más gotas cuantos más vatios), gotas que caen en la recuperación y un par de remolinos por palada que quedan atrás y se apagan en 4 s, con las reglas del legacy.
- **Una sombra que sigue al sol o a la luna.** En los niveles Medio y Alto es una sombra real; en Bajo, el óvalo del legacy. De noche el bote y el remero se ven oscuros y azulados.
- **Una boya naranja de 500 m** en los trabajos de las rutinas: pasa junto al remero cuando el panel `Parciales 500 m` cierra la vuelta, ± 1 s.
- **Pausa y reposo**: el remero frena en ≈ 0,25 s y el bote desliza. Al rato respira y mira a los costados. `Reanudar` retoma desde donde estaba.
- **No cambia ningún dato**: con la escena o sin ella, los metros, el tiempo, las vueltas y la sesión guardada son idénticos byte a byte. El HUD sigue en ≤ 57 de 60 elementos.

## 2. Cómo probarlo en `Builds/Windows/simulator`

El build **ya tiene la 7** (lo generó QA con el código final) y **usa Y Bot**, porque el archivo está en tu máquina. Abrí un solo `RowMate.exe` a la vez y, si aparece el aviso del Firewall, contestá `Cancelar`.

**(a) Remar** (3 min)
1. Abrí `RowMate.exe`, andá a **Workout** y esperá unos 15 s mientras se prepara la escena. → [DEMO-01](snapshots/DEMO-01-suave.png)
2. Tocá `Entrenamiento libre`, abrí la **llave** y elegí `Suave` (20 spm, 100 W). Cerrá el panel. Vas a ver una palada lenta con una recuperación larga, salpicadura en cada ataque y dos remolinos que quedan atrás.
3. Llave → `Fuerte` (30 spm, 280 W). La palada se acelera sin saltos, hay más salpicadura y el bote cabecea y empuja. → [DEMO-02](snapshots/DEMO-02-fuerte.png)
4. `⏸ Pausa`. El remero frena en un cuarto de segundo, el bote desliza y a los ≈ 2,5 s el remero empieza a respirar. `Reanudar` arranca sin salto. → [DEMO-03](snapshots/DEMO-03-pausa.png)

**(b) La sombra** (2 min). Llave → fila `Hora escena`:
1. Con `09:00` la sombra queda de un lado; con `17:00`, del otro. → [DEMO-04](snapshots/DEMO-04-sombra-17.png)
2. Con `22:00` hay sombra tenue de luna y el bote y el remero se ven azulados. → [CAP-08](snapshots/CAP-08-sombra-22-th.png)
3. `Hora real` → `Terminar` → `Terminar`.

**(c) La boya** (4 min). Necesitás la rutina `Series 600` (la armaste en la It. 5; si no la tenés, está en el REPORT de la 5 §3.1 (a)). Llave → `Watts` en `150` → `Series 600` → `Iniciar`. En el primer trabajo, cerca de los **2:35** del reloj, la boya naranja llega por la derecha y pasa junto al remero justo cuando el panel `Parciales 500 m` cierra la vuelta `#1`. → [DEMO-05](snapshots/DEMO-05-boya.png)

**(d) Maniquí o Y Bot.** El build actual muestra **Y Bot**: es el remero calvo de [DEMO-06](snapshots/DEMO-06-ybot.png) y [CAP-21](snapshots/CAP-21-ybot-tv.png). Para ver el maniquí (con pelo y anteojos, [CAP-20](snapshots/CAP-20-maniqui-tv.png)):
1. Cerrá el Editor de Unity y sacá la carpeta `Assets/ThirdParty/Mixamo/YBot` fuera del proyecto.
2. Regenerá el build con `powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator`.
3. Para volver a Y Bot, devolvé la carpeta a su lugar y regenerá el build otra vez. No hay que tocar nada en Unity: la app elige sola.

### Para qué sirve Y Bot

Es un maniquí humano gratuito de Mixamo, con el esqueleto estándar y la piel bien pesada. **No trae animaciones y no usamos ninguna**: la palada es nuestra. Sirve para tres cosas:
- Comprueba que el IK funciona con un cuerpo hecho por otros: si funciona con Y Bot, funciona con cualquier personaje Humanoid, como un remero realista más adelante.
- Da una referencia de proporciones para el maniquí.
- Hombros, codos y rodillas se deforman mejor.

Ni Y Bot ni sus `.meta` entran al repositorio, y el CI compila sin él. **Ya lo bajaste y está funcionando**, así que no tenés que hacer nada más.

## 3. Resultados (salida real)

| Suite | Resultado |
|---|---|
| dotnet (.NET 8) | 0 avisos y 0 errores. Herramientas 192/192. Core: 3191, de los que pasan 3152, fallan 0 y se omiten 39 |
| Matriz AC → test | 656 PASA, 0 FALLA, 4 ignorados y 149 pendientes de Unity (los cubren las suites de Unity) |
| Unity EditMode | 3290: pasan 3245, fallan 0, se ignoran 45. Con simulador: 3482, pasan 3437, fallan 0, se ignoran 45 |
| Unity PlayMode | 500/500. Con simulador, 573/573 |
| Build de Windows con simulador | `Result: Success` |
| `[PERF]` en el build (Y Bot) | 5 corridas: escena en Alto y en Medio, horas, series y diálogo de completado. Sin excepciones ni GC en régimen, y ningún *shader* subido después de Ready. CPU del hilo principal p99 de 3,4 a 8,8 ms |
| Presupuesto en Medio | Maniquí: 82 dibujos, 50 SetPass y 164 k triángulos. Y Bot: 84 dibujos, 50 SetPass y 267 k triángulos |
| Capturas | 55 tomas (21 fotos, 7 videos y 6 de demo), iguales byte a byte entre dos corridas, con el maniquí y con Y Bot |
| Repo público | Limpio: sin rutas de la PC y nada de Mixamo versionado. El legacy, sin cambios |

Las pruebas usaron siempre una carpeta de datos temporal y los ProjectSettings quedaron sin cambios.

## 4. Criterios

- **Pasan 37**, entre ellos LT-88 a LT-98 del legacy, todos sin cambiar números.
- **AC-SCN-05-08** (las 10 poses del atleta) falló en la ronda 1 y se corrigió: la tabla de poses pasó a ser la hoja medida, con una columna para Y Bot (DV-78). Ahora los 60 valores de cada cuerpo quedan dentro de ± 8°. → [CAP-17](snapshots/CAP-17-poses-maniqui.png), [CAP-18](snapshots/CAP-18-poses-ybot.png)
- **AC-SCN-01-16 no se pudo verificar** (60 fps con el remero). La CPU, las asignaciones y el HUD dan bien, pero esa noche la PC limitaba la presentación a ≈ 13 fps también en Bajo: es un tope externo a la app, con la pantalla apagada. La medida del Architect con la pantalla encendida había dado GPU de 4,4 a 5,6 ms. Se repite con la pantalla encendida en la próxima corrida de QA. Android queda para la It. 14, cuando haya teléfono.

## 5. Capturas

| Qué | Archivo |
|---|---|
| Drive, recuperación y ataque con salpicadura | [CAP-01](snapshots/CAP-01-drive-tv.png), [CAP-02](snapshots/CAP-02-recuperacion-tv.png), [CAP-03](snapshots/CAP-03-ataque-tv.png) |
| Sombra a las 9, 12, 17 y 22 h, y el óvalo en Bajo | [CAP-05](snapshots/CAP-05-sombra-09-th.png), [CAP-06](snapshots/CAP-06-sombra-12-th.png), [CAP-07](snapshots/CAP-07-sombra-17-th.png), [CAP-08](snapshots/CAP-08-sombra-22-th.png), [CAP-09](snapshots/CAP-09-ovalo-bajo-th.png) |
| El HUD con el remero, la llave en L640 y los paneles de series | [CAP-10](snapshots/CAP-10-hud-tv.png), [CAP-11](snapshots/CAP-11-hud-th.png), [CAP-14](snapshots/CAP-14-llave-l640.png), [CAP-12](snapshots/CAP-12-series-c0-tv.png) |
| La boya | [CAP-15](snapshots/CAP-15-boya-150.2-tv.png), [CAP-16](snapshots/CAP-16-boya-155.2-tv.png) |
| Comparación con el legacy | [CAP-19a](snapshots/CAP-19a-catch-v390.png), [CAP-19b](snapshots/CAP-19b-finish-v390.png) |
| Maniquí y Y Bot | [CAP-20](snapshots/CAP-20-maniqui-tv.png), [CAP-21](snapshots/CAP-21-ybot-tv.png) |
| Demo completa | [DEMO-01](snapshots/DEMO-01-suave.png) a [DEMO-06](snapshots/DEMO-06-ybot.png) |
| Videos | [VID-01 Suave](snapshots/VID-01-suave-tv.mp4), [VID-02 Fuerte](snapshots/VID-02-fuerte-tv.mp4), [VID-03 cambio de ritmo](snapshots/VID-03-suave-a-fuerte-tv.mp4), [VID-04 pausa](snapshots/VID-04-pausa-th.mp4), [VID-05 sin paladas](snapshots/VID-05-spm-cero-tv.mp4), [VID-06 vista lateral](snapshots/VID-06-lateral-th.mp4), [VID-07 boya](snapshots/VID-07-boya-tv.mp4) |

## 6. Decisiones por defecto (respondé solo si querés cambiar alguna)

| Decisión | Por defecto | Si la rechazás |
|---|---|---|
| Medidas y proporciones reales (DV-68, DV-74, DV-77): el bote es más fino y el remero se ve más chico que en el legacy | Se aceptan | Se usa la escala del legacy en lo visible |
| El remero cabecea con el casco, el ritmo depende del SPM, el plumeo es continuo y se suman arfada, rolido, empuje, gotas y respiración (DV-69, -70, -75, -76) | Se aceptan | Vuelve lo del legacy en cada uno |
| Sombra real en Medio y Alto, óvalo del legacy en Bajo (DV-72) | Se acepta | Óvalo en todos los niveles |
| Boya naranja de 500 m solo en los trabajos de las rutinas, a +4,5 m y sin texto (DV-73) | Se acepta | Sin boya |
| La tabla de poses es la hoja medida, con los brazos algo doblados en la recuperación (DV-78) | Se acepta | El Designer ajusta la técnica (va en un issue) |
| El maniquí lleva pelo y anteojos, sin gorra (DT7-09) | Se acepta | Gorra puesta |
| Y Bot mide 1,786 m y se usa así: el slot acepta personajes de 1,55 a 2,05 m (QA7-V-06) | Se acepta | Escalar Y Bot a 1,85 m |
| El tope de SetPass en Medio es ≤ 50, el mismo de la 6a (B7-04) | Se acepta | Bajar SetPass en la It. 14 |
| Con "reducir movimiento" el remero sigue remando y solo la cámara queda quieta | Se acepta | El remero también queda quieto |

**Antes del commit de la 7** (no cambia la app): borrar `Assets/New Text Document.txt` y su `.meta`, volver a guardar en UTF-8 dos archivos que tienen tildes rotas y regenerar `snapshots/INDEX.md` entero.

## 7. Deuda: va a issues de GitHub

Son 10 issues agrupados (boya, efectos de agua, agua avanzada, pulido del remero, cámara cercana y LOD, rig y técnica, rendimiento, arnés de capturas, composición y limpieza). No bloquean nada de lo que se entregó.

## 8. Próximo paso

Probá la demo de §2 y dame el OK, o decime qué decisión de §6 querés cambiar. Después sigue la **It. 8: Río, Costa, Regata y selector de escenario**. La 6b sigue postergada (issue #6).
