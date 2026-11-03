# It. 5 — Series en vivo: parciales de 500 m y repeticiones: spec de producto

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-30 |
| Iteración | 5 · `05-series-parciales` ([ROADMAP §2, It. 5](../../ROADMAP.md)) |
| Estado | **Spec del PO corregida por el gate de armonía de QA** ([03-qa-harmony-paridad](03-qa-harmony-paridad.md), [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md)). Decide SD-01 a SD-19 (§11.3) y suma DV-50 y DV-51 para el usuario (§11.1). Lo que cambió está en "Cambios por gate QA", al final. **Precisada después por la verificación de QA** ([05-qa-verification](05-qa-verification.md), [05-qa-lens-paridad](05-qa-lens-paridad.md)): AC-BLE-16-02, AC-WKT-18-11, -14, -16 y AC-WKT-17-09 ("Cambios por verificación (ronda 1)", al final). Antes de construir encima va la deuda "al empezar la It. 5" de la 4b (§5.1, §12 punto 1). **Cerrada por el PO** el 2026-10-30: aceptada, con la verificación en el build pendiente ("Al cerrar la iteración", al final; [REPORT](REPORT.md)) |
| IDs dueños | **3 canónicos**: F-WKT-07, F-WKT-08, F-WKT-18 |
| También toca | F-WKT-13, F-WKT-14 y F-WKT-17 (la cascada, la salida, la cuenta grande, `PAUSA`, la zona del bote y "nada se encima" con los paneles); F-WKT-16 (QA-V4b-R2-01); F-WKT-01, -02, -03 y -11 (la pausa, el segundo plano, el equipo suspendido, el paso por distancia y los formatos, que los paneles usan); F-APP-05 (teclado y reducir movimiento en los paneles); F-APP-10 y F-APP-11 (9 claves nuevas: los textos fijos de los paneles pasan al catálogo); F-BLE-15 y F-BLE-16 (`Simular desconexión`, la llave y el panel del simulador); F-HIS-20 (los datos del usuario, ahora también sus preferencias) |
| Criterios | **74** = **46 nuevos** (WKT 45: F-WKT-07 12, -08 8, -18 22, -13 2, -16 1; APP 1: F-APP-10 1) + **28 de iteraciones anteriores** (§5.2: 13 que cambian; §5.3: 15 que se vuelven a verificar). Además: **15 casos de test legacy** (LT-47 a LT-61, §6) y **26 capturas, 12 tomas de la demo y 9 videos** (§7) |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`). Las rutas `lib/...:línea`, `test/...` y `docs/...` de esta spec son relativas a ese repositorio. Leídos: `lib/features/workout/series_tracker.dart` y `series_panels.dart` (enteros), `workout_provider.dart:40-328`, `immersive_workout_screen.dart:90-115` y `:556-690` (la integración de los paneles), `lib/core/models/routine.dart:20-106`, `lib/core/bluetooth/simulated_ble_service.dart:81-103`, `lib/core/dev/rowing_simulator.dart:34-77`, `lib/l10n/app_es.arb` y `app_en.arb` (no hay claves de los paneles: sus textos están fijos en el código), `test/series_tracker_test.dart`, `test/series_panels_test.dart`, `test/routine_positions_test.dart`, `docs/superpowers/specs/2026-10-02-series-splits-design.md` y `docs/image-03.png` (el HUD con los dos paneles abiertos). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Insumos | [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) §3 (DC-35) y §4 (Q-01, Q-06, Q-11, Q-15, Q-55, Q-91); inventario [workout-engine-hud](../../product/inventory/workout-engine-hud.md) (F-WKT-07, -08, -17, -18); [spec de la It. 4a](../04a-motor-hud-funcional/01-po-spec.md) (AC-WKT-03-03, AC-WKT-11-09 a -11, DV-26, §0.4); [spec de la It. 4b](../04b-hud-exr/01-po-spec.md) (todo lo que se cita por ID) y su [REPORT](../04b-hud-exr/REPORT.md) §7 y §9; [04b/00-deuda-diseno](../04b-hud-exr/00-deuda-diseno.md) §4.8; [04b/02-tech-design](../04b-hud-exr/02-tech-design.md) §3.11 (el presupuesto); [ART_BIBLE](../../design/ART_BIBLE.md) §3.4, §5.2, §10.4; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §0.1, §2.3, §2.10; [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §9.4 (preferencias) y §20.2 (topes); [STATUS](../../STATUS.md) |

---

## 0. Cómo leer esta spec

### 0.1 Identificadores

- **`AC-<ÁREA>-<NN>-<nn>`**: criterio `nn` de la ficha `F-<ÁREA>-<NN>`. F-WKT-07, F-WKT-08 y F-WKT-18 empiezan en 01. Los de fichas que ya tienen criterios siguen su numeración: AC-WKT-13-15 sigue a los 14 de la 4b, AC-WKT-16-07 a los 6 y AC-APP-10-12 a los 11.
- **§5** repite IDs de iteraciones anteriores: los que **cambian** (valen los textos de §5.2, que reemplazan a los anteriores) y los que se **vuelven a verificar** con los paneles puestos (§5.3). Si fallan, se reabren acá.
- **`LT-nn`**: casos de test legacy, con la numeración que viene de la 4b (terminó en LT-46).
- **`CAP-nn`**, **`DEMO-nn`** y **`VID-nn`**: capturas, tomas de la demo y videos de esta iteración (§7), en `docs/migration/iterations/05-series-parciales/snapshots/`. Las de la 4b se citan como "CAP-nn de la 4b".
- **`DV-nn`**: diferencias visibles con el legacy, a propósito (§11.1). Siguen la numeración de la 4b (terminó en DV-47).
- **`P-nn`**: preguntas abiertas de esta spec (§14).
- **`SD-nn`**: las diferencias entre esta spec y el diseño ([02-tech-design §8.2](02-tech-design.md), [04-design-notes §11.1](04-design-notes.md)) que decidió el gate de armonía (§11.3).
- **`F-*`**, **`R-*`** y **`S-*`**: bases, remos y estados de prueba (§0.4).

### 0.2 Cómo se verifica cada criterio

Las marcas de la 4b ([01-po-spec §0.2](../04b-hud-exr/01-po-spec.md)): **[dotnet]**, **[Editor-EM]**, **[Editor-PM]**, **[Captura]**, **[Device-Win]**, **[Device-And]** y **[QA]**. Si falta el módulo de Android o el teléfono, la parte de equipo queda **NO VERIFICABLE** con el motivo y no frena la iteración. Nunca se da por cumplido sin verlo.

### 0.3 Notación

La de la 4b ([01-po-spec §0.3](../04b-hud-exr/01-po-spec.md)):

- Textos entre comillas invertidas: **exactos al carácter**. `·` es U+00B7 con un espacio a cada lado; `—` es U+2014; `…` es U+2026; `#` es U+0023.
- Tamaños en dp. En el HUD, toda medida del legacy se multiplica por la **escala del HUD** (`clamp(lado corto / 390; 0,9; 1,6)`): 1,054 en TV y TH, 0,92 en P360 y L640, 1,6 en W1280. **"Base"** es la medida antes de multiplicarla, y `8·s` quiere decir 8 base por la escala. Los paneles de series van con **su escala, `sp`**: la del HUD, salvo en W1280, donde es 1,20 (AC-WKT-18-13); `8·sp` es 8 base por ella.
- "Segundo" del motor = un paso del reloj activo de 1 s. "En marcha", "en pausa", "terminada" y "reposo" son las fases del motor.
- **"Parciales"** es el panel de vueltas de 500 m (`500 m` / `Parciales 500 m`); **"repeticiones"** es el panel `Reps` / `Series anteriores`. **"Cerrado"** = colapsado (el chip); **"abierto"** = expandido.
- Los tiempos de los ejemplos son los del reloj activo de la sesión (los de la banda), y "a los 170 s" quiere decir "después del segundo 170 del motor".

### 0.4 Estado de prueba

**Lo de la 4a y la 4b** ([4a §0.4](../04a-motor-hud-funcional/01-po-spec.md), [4b §0.4](../04b-hud-exr/01-po-spec.md)), sin cambios: base, reloj y planificador de prueba (la sesión se crea en el instante 0); el ciclo de vida de prueba, con segundo plano, foco y suspensión del equipo; la zona horaria UTC−03:00 (el reloj de pared marca `10:00`); el remo **`R-4`** (la lectura `L(k)` llega en el instante `k − 0,5`: distancia `1000 + 4k`, split 125, 150 W, SPM 24.0; `L(0)` llega antes de iniciar, así que la rutina arranca con el monitor en 1000 m y en el segundo `k` los metros de la rutina son `4k`) y sus variantes **`R-4R`** (desde `L(10)` la distancia es `4 (k − 10)`: el monitor se reinicia), **`R-4Z`** (`L(10)` llega con distancia 0), **`R-4P`** (parado) y **`R-4X`** (los extremos de FTMS); el simulador sin ruido (150 W y 24 spm dan split `02:13`, 3,77 m/s); idioma, letra y "reducir movimiento" de prueba. Ningún doble existe en un build que se instala.

**Nuevo en la It. 5:**

- **Preferencias de prueba**: cada prueba, captura y video usa su propio archivo de preferencias, temporal. **Nunca** el del usuario (AC-HIS-20-04, §5.2). "Sin preferencias" = el archivo no existe (los paneles arrancan cerrados). Las tomas dicen cuándo arrancan con `immersive.lapsExpanded` o `immersive.repsExpanded` en verdadero.
- **Base `F-5`** (en v8), con estas rutinas, de la más nueva a la más vieja:
  - **`Series 600`** (la de la demo): calentamiento 30 s; serie ×3 [trabajo 600 m; descanso 20 s]; enfriamiento 30 s. 8 pasos. Con `R-4`: calentamiento 1-30 s; rep 1, trabajo 31-180 (vuelta 1 a los 155) y descanso 181-200; rep 2, trabajo 201-350 (vuelta 1 a los 325) y descanso 351-370; rep 3, trabajo 371-520 (vuelta 1 a los 495) y descanso 521-540; enfriamiento 541-570.
  - **`Trabajo 1000`**: un trabajo de 1000 m, suelto.
  - **`Fondo 3000`**: un trabajo de 3000 m, suelto.
  - **`Cinco reps`**: serie ×5 [trabajo 500 m; enfriamiento 10 s]. Con `R-4` cada repetición son 125 s de trabajo y 10 de enfriamiento (540 m); la rep `r` empieza a los `1 + 135 (r − 1)` s.
  - **`Dos series`**: serie A ×2 [trabajo 60 s]; serie B ×2 [trabajo 60 s]; descanso 30 s, suelto. Con `R-4`: A 1-120, B 121-240, descanso 241-270.
  - Copias de **`Pirámide 500`** (de `F-LISTA`), **`Prueba 4a`** y **`Corta`** (de `F-4A`).
- **Remos de prueba nuevos**:
  - **`R-3`**: `R-4` con la distancia `1000 + 3k` (3 m/s) y split 167.
  - **`R-4A`** (acelera): `R-4` hasta `L(250)`; desde `L(251)`, distancia `2000 + 5 (k − 250)` (5 m/s) y split 100.
  - **`R-4D`** (corte): `R-4` sin `L(101)`, `L(102)` ni `L(103)`. El monitor siguió contando, pero esas lecturas no llegan, como en un corte de la conexión; del 101 al 103 el motor sigue con `L(100)`.
  - **`R-4S`** (el monitor sigue contando con el equipo suspendido): `R-4` con el número de la lectura tomado del reloj crudo, que sigue corriendo mientras el equipo duerme. Al despertar, la primera lectura trae todo lo que el monitor contó durante el sueño. Lo usa AC-WKT-07-10. El `R-4` del ciclo de vida de prueba, en cambio, no manda nada con el equipo dormido y al despertar sigue sin salto. Cómo se arma es del Architect (QA-G5T-06).
- **Estado de prueba `S-LARGO`** (armado a mano, sin correr el motor, como la fila de 1998 píldoras de la 4b): en un trabajo de una serie, la vuelta en curso en `5999:59.9` y `499 m`; las vueltas `#1999`, `#1998`, `#1997` y `#1996`, cada una con `5999:59.9`; la repetición `999` de `999` con `999.999 m`; y las repeticiones anteriores `999`, `998` y `997`, cada una con split `5999:59` y `999.999 m`.

---

## 1. Objetivo y demo

**Objetivo.** En los pasos de trabajo de una rutina, el remero ve sus parciales de 500 m: la vuelta en curso y las 4 últimas, de la más reciente a la más vieja. Dentro de una serie ve en qué repetición va, cuántos metros lleva en ella y cómo le fue en las 3 repeticiones anteriores. Todo va en dos paneles de vidrio debajo de SPM, cerrados por defecto, que se abren con un toque (o con el teclado) y recuerdan su estado entre sesiones. Los números son los del legacy, también cuando el monitor manda saltos de distancia, y los paneles conviven con la cuenta grande, el sello, la pausa y la llave sin encimarse.

**Demo (se le muestra al usuario antes de la It. 6a).** Con el build de simulador de Windows (o el Editor con el modo simulador), en la ventana de 1280 × 720. Si Windows está en inglés, el build sale en inglés: el texto en inglés va entre paréntesis. Los valores llevan "≈" porque el simulador del build tiene ruido.

**(a) Parciales y repeticiones** (`Series 600`, 6 minutos). Si la rutina no está, se crea en Rutinas: `+`, nombre `Series 600`; un paso de calentamiento de 30 s; una serie ×3 con un trabajo de 600 m y un descanso de 20 s; un paso de enfriamiento de 30 s; `Guardar`.

1. Con Watts en 150 (llave, `−` o `+`), Workout → `Series 600` → `Iniciar`. Debajo de SPM aparece, junto con ella en la cascada de la entrada, el chip `500 m` en ámbar. Durante el calentamiento no hay chip `Reps`: todavía no hay serie.
2. Tocar `500 m`: el panel se abre con suavidad y dice `Parciales 500 m` (`500 m splits`) y `Sin parciales en este paso` (`No splits in this step`).
3. A los `00:31` empieza el trabajo. El panel muestra la vuelta en curso contando (`0:01.0` y ≈ `4 m`, `0:02.0` y ≈ `8 m`…) y, debajo, `Rep 1/3 · …`. Debajo del panel aparece el chip `Reps` en celeste. Tocarlo: `Series anteriores` (`Previous reps`) con `Sin repeticiones completas` (`No completed reps`).
4. A los ≈ `02:43` se cierran los primeros 500 m: la lista baja, arriba entra `#1` con ≈ `2:12.6` y, a su izquierda, una marca ámbar que se apaga, y la vuelta en curso vuelve a contar desde `0:00.0`.
5. A los ≈ `03:10` termina el trabajo. Durante ese segundo el panel todavía muestra el trabajo; después dice `Sin parciales en este paso`, y la línea `Rep 1/3 · …` sigue sumando los metros del descanso.
6. A los ≈ `03:31` empieza la repetición 2. En `Series anteriores` entra `Rep 1` con ≈ `2:13` y ≈ `676 m`, y la línea pasa a `Rep 2/3 · …`.
7. Llave → `Simular desconexión`. Durante ≈ 4 s los metros de la vuelta en curso no avanzan y su tiempo sí; después siguen desde donde estaban. Nada salta, no aparece ninguna vuelta inventada y ningún número se pone negativo.
8. `Terminar` → `Terminar`. Volver a iniciar `Series 600`: `Parciales 500 m` ya está abierto desde el primer cuadro, y `Series anteriores` aparece abierto, vacío, cuando empieza la serie. Cerrar los dos, terminar, **cerrar la app** y volver a abrirla: al iniciar otra vez, los paneles están cerrados.

**(b) En el Editor, en el teléfono vertical (`411 x 914`)** (2 minutos). Con `Series 600`:
- en los últimos 3 s del primer descanso (≈ `03:27` a `03:30`), con los paneles cerrados, aparece la cuenta grande `3`, `2`, `1` en su lugar de la 4b, a ≈ 8 dp de los chips, sin tocarlos;
- en el descanso de la rep 2, abrir `500 m` justo cuando aparece la cuenta grande (≈ `06:27`): el panel se abre a su lado, la cuenta grande **sigue en su lugar** y en ningún momento se enciman (DV-50). La cuenta de la barra igual crece y se tiñe, y la píldora late.

En el teléfono horizontal (`914 x 411`), lo mismo con los dos paneles abiertos: quedan a la izquierda, lejos de la cuenta grande y del bote.

**(c) Con el teclado** (1 minuto). En la ventana, Tab recorre `Terminar`, `Pausa`, `500 m`, `Reps` y la llave, con el anillo de foco; Enter sobre `500 m` lo abre y lo cierra.

**(d) Las pruebas.** `dotnet test` y las de Unity en verde, con LT-47 a LT-61. La carpeta de capturas trae CAP-01 a CAP-26, la secuencia DEMO-01 a DEMO-12 y VID-01 a VID-09. Las tomas con rutina de la 4b quedan regeneradas con los chips debajo de SPM; las del HUD de la 4a ya estaban reemplazadas por las de la 4b (§7.1).

---

## 2. Alcance

| ID | Funcionalidad | Qué se cierra en la It. 5 | Vuelve en |
|---|---|---|---|
| F-WKT-07 | Parciales de 500 m | Completo: vueltas, interpolación, vuelta en curso, saneamiento compartido con el paso por distancia, pausa, segundo plano, equipo suspendido y oráculo | 10a (el remo real que no manda la distancia una vez por segundo: QA-V4a-L01) |
| F-WKT-08 | Repeticiones de la serie y comparación | Completo: en serie, metros de la repetición, resumen de las 3 anteriores, split de trabajo | — |
| F-WKT-18 | Paneles colapsables | Completo: los dos paneles, su contenido, el toque y el teclado, las preferencias, el lugar, la zona del bote, la convivencia con la cuenta grande, `PAUSA`, el sello y la llave, valores largos, animaciones, presupuesto, es/en | 6a / 7 (con el bote a la vista, se revisa P-03, los paneles abiertos en vertical); 6b (vidrio con desenfoque); 14 (lectores de pantalla) |
| F-WKT-13, -14, -16, -17 (también tocan) | Inmersivo, barra, barra libre y tarjetas | La cascada y la salida con los paneles, la cuenta grande y `PAUSA` contra los paneles abiertos, la zona del bote, la deuda de la 4b (QA-V4b-06, -R2-01, O-4, O-5) | 6a |
| F-APP-10, F-APP-11 (también tocan) | Catálogo y textos fijos | 9 claves nuevas (223 por idioma): los textos de los paneles, fijos en español en el legacy, pasan al catálogo con el mismo español | 14 (auditoría de F-APP-11) |

---

## 3. Historias de usuario

| ID | Historia |
|---|---|
| H-01 | Como **remero en un trabajo largo**, quiero ver cuánto tardé en cada 500 m y cómo viene la vuelta en curso, para sostener el ritmo sin frenar. |
| H-02 | Como **remero en una serie**, quiero saber en qué repetición voy y cuántos metros llevo en ella, para dosificarme. |
| H-03 | Como **remero en una serie**, quiero comparar con mis últimas repeticiones (split y metros), para saber si voy mejor o peor que hace un rato. |
| H-04 | Como **remero que no quiere ruido en la pantalla**, quiero que los paneles estén cerrados hasta que los abra, y que la app recuerde lo que elegí. |
| H-05 | Como **remero**, quiero que abrir un panel no tape las métricas ni el bote (en horizontal) y que todo se acomode sin saltos. |
| H-06 | Como **remero con un monitor que falla** (paquetes sin distancia, reinicios, cortes), quiero que los parciales no muestren números absurdos. |
| H-07 | Como **usuario de la PC**, quiero abrir y cerrar los paneles con el teclado. |
| H-08 | Como **usuario en inglés**, quiero los paneles en inglés. |

---

## 4. Criterios de aceptación

### 4.0 Conteo

| Ficha | AC nuevos | Ficha | AC nuevos |
|---|---|---|---|
| F-WKT-07 | 12 | F-WKT-13 (deuda de la 4b, §5.1) | 2 |
| F-WKT-08 | 8 | F-WKT-16 (deuda de la 4b, §5.1) | 1 |
| F-WKT-18 | 22 | F-APP-10 | 1 |

Total: **46 nuevos** (§4.1-§4.4 y §5.1) + **28 de iteraciones anteriores** (§5.2: 13; §5.3: 15) = **74**.

### 4.1 F-WKT-07 — Parciales de 500 m

- **AC-WKT-07-01 · Solo en los pasos de trabajo de una rutina.** *Entonces* hay parciales solo con rutina y solo mientras el paso es de tipo trabajo. En calentamiento, descanso y enfriamiento no hay vueltas, y la vuelta en curso vale 0 m y 0 s. En el libre no hay parciales ni repeticiones, ni paneles (AC-WKT-18-01). [dotnet] · `lib/features/workout/series_tracker.dart:73-78`, `:129`, `:178`; `immersive_workout_screen.dart:105-106`
- **AC-WKT-07-02 · La vuelta.** *Entonces* una vuelta son 500 m completos remados dentro de un paso de trabajo, contados desde el inicio del paso.
  - Se numeran 1, 2, 3… dentro de cada paso, y la lista se vacía al empezar cada paso, también si el siguiente es otro trabajo.
  - Un trabajo en el que se remaron menos de 500 m no tiene vueltas (LT-49), y un trabajo por tiempo también tiene vueltas: no depende del largo del paso.
  - Varias vueltas en un mismo paso: LT-48.
  - Con `Pirámide 500` y `R-4`, el trabajo de 500 m tiene `#1` con `2:05.0`, y el de 250 m que le sigue no tiene ninguna.

  [dotnet] · `series_tracker.dart:40`, `:133-140`, `:179-181`
- **AC-WKT-07-03 · Una lectura por segundo, la del segundo.** *Entonces* parciales y repeticiones se alimentan una vez por segundo del motor, con la última lectura del monitor y el reloj activo de ese segundo, **antes** de evaluar si el paso terminó. La lectura del segundo en que termina un paso es de ese paso, y el paso siguiente recibe su primera lectura en el segundo siguiente. No se alimentan en pausa, en el libre ni después del fin de la rutina. Con `Series 600` y `R-4`, el trabajo de la rep 1 recibe las lecturas de los segundos 31 a 180, y su descanso, las de 181 a 200. [dotnet] · `workout_provider.dart:151-186`; `series_tracker.dart:118`
- **AC-WKT-07-04 · El instante del cruce.** *Entonces* el instante en que se cruza cada marca de 500 m se interpola entre la lectura anterior y la que la cruza: `t = t_anterior + (marca − d_anterior) / Δd × Δt`.
  - El tiempo de la vuelta es ese instante menos el final de la vuelta anterior, o menos el inicio del paso (el reloj activo de la lectura anterior a la primera del paso).
  - Si una lectura cruza varias marcas, cierra varias vueltas.
  - A 3 m/s, con 498 m en el segundo 166 y 501 m en el 167, la vuelta da 166,67 s: `2:46.7` (LT-47; `Trabajo 1000` con `R-3`).
  - A 4 m/s (`R-4`), cada vuelta es exactamente `2:05.0`.

  [dotnet] · `series_tracker.dart:132-140`
- **AC-WKT-07-05 · La vuelta en curso.** *Entonces*, en un paso de trabajo, la vuelta en curso tiene `⌊metros del paso − 500 × vueltas⌋` metros (de 0 a 499) y `reloj activo − final de la última vuelta (o inicio del paso)` segundos. Fuera de un trabajo, 0 m y 0 s. Ejemplos:
  - LT-47: 10 m y 3,33 s.
  - `Series 600` con `R-4`, a los 170 s: 60 m y 15,0 s.
  - a los 180 s, el último segundo del trabajo: 100 m y 25,0 s.

  [dotnet] · `series_tracker.dart:72-78`
- **AC-WKT-07-06 · Los saltos de distancia no suman (Q-55).** *Entonces* en cada segundo suma lo que avanzó el monitor desde la lectura anterior, salvo que sea negativo o más de 10 m por segundo de reloj activo (`10 × máx(Δt; 1)`). En ese caso, ese segundo no suma metros, el tiempo sí corre y esa lectura pasa a ser la referencia. Cubre el paquete sin distancia (que llega como 0), el reinicio del monitor, el primer salto al empezar y un corte de lecturas. Nunca hay metros negativos, vueltas falsas ni metros dobles. Con `Trabajo 1000`:
  - `R-4`: vueltas de `2:05.0`, en los segundos 125 y 250.
  - `R-4R` (el monitor vuelve a 0 en `L(10)`): se pierden 4 m; `#1` con `2:06.0` (segundo 126) y `#2` con `2:05.0` (251).
  - `R-4Z` (`L(10)` llega sin distancia): se pierden 8 m, los de esa lectura y la siguiente; `#1` con `2:07.0` (127) y `#2` con `2:05.0` (252).
  - `R-4D` (no llegan `L(101)` a `L(103)`): del segundo 101 al 103 no suma, y en el 104 el salto de 16 m no cuenta; se pierden 16 m; `#1` con `2:09.0` (129) y `#2` con `2:05.0` (254).
  - Los casos legacy LT-55, LT-56 y LT-57.

  [dotnet] · `series_tracker.dart:43-46`, `:120-127`
- **AC-WKT-07-07 · Lo remado en pausa no cuenta.** *Cuando* se reanuda, *entonces* la distancia del monitor en ese instante pasa a ser la referencia, y el salto no se cuenta (LT-58). Es lo mismo que en el paso por distancia (AC-WKT-01-05 de la 4a). Con `Trabajo 1000` y `R-4`, `Pausa` a los 20,7 s y `Reanudar` a los 50,7 s:
  - durante la pausa, la vuelta en curso queda en `0:20.0` y `80 m`;
  - el primer segundo después de reanudar marca `0:21.0` y `84 m`;
  - `#1` cierra en el segundo 125 de reloj activo, con `2:05.0`, como sin la pausa: lo remado en pausa no suma (las lecturas de `L(20)`, 1080 m, a `L(51)`, 1204 m: 124 m, por la fracción que pierde la pausa, Q-58, y el medio segundo de las lecturas).

  [dotnet] · `workout_provider.dart:245-250`; `series_tracker.dart:104-108`
- **AC-WKT-07-08 · Desde el inicio de la rutina.** *Entonces* los metros se cuentan desde la distancia que tenía el monitor al iniciar la rutina, y el tiempo desde el reloj activo al iniciar (0). Los totales del monitor no vuelven a 0 (Q-15): con `R-4`, el monitor trae 1000 m y la rutina arranca en 0. Si al iniciar todavía no llegó ninguna lectura, la primera, con todo lo que el monitor traía acumulado, es un salto que no suma (LT-56). [dotnet] · `workout_provider.dart:128`; `series_tracker.dart:86-102`
- **AC-WKT-07-09 · Los mismos metros que el paso por distancia (Q-55, DV-26).** *Entonces* los metros de un paso que cuentan los parciales y los que deciden el fin de un paso por distancia (AC-WKT-03-03 de la 4a) son los mismos, segundo a segundo y con cualquier secuencia de lecturas.
  - Un trabajo de `500 × n` m cierra su vuelta `n` en el mismo segundo en que termina: `Trabajo 1000` con `R-4`, `R-4R`, `R-4Z` y `R-4D` termina en los segundos 250, 251, 252 y 254.
  - Un trabajo de 600 m termina con la vuelta en curso en 100 m, o en algo más si la última lectura se pasó.

  La 10a mantiene la regla con el remo real (QA-V4a-L01). [dotnet]
- **AC-WKT-07-10 · Segundo plano y equipo suspendido.** *Entonces*:
  - con la app en segundo plano, minimizada o sin foco (Q-91), los segundos que faltan se procesan en orden, cada uno con su lectura (AC-WKT-02-04), así que vueltas y repeticiones dan lo mismo que en primer plano;
  - con el equipo suspendido (P-17 de la 4a), el lapso no se procesa y no suma tiempo a la vuelta en curso. Al despertar, un salto de distancia de más de 10 m por segundo no suma (AC-WKT-02-11): la vuelta en curso sigue desde donde estaba, el primer segundo sin sumar metros, y desde el siguiente suma de nuevo.

  Se prueba con `Series 600`:
  - con `R-4` y segundo plano de los 100 a los 400 s: da lo mismo que en primer plano;
  - con `R-4S` (§0.4) y una suspensión de 600 s a los 250,4 s: la vuelta en curso sigue desde `0:50.0` y `200 m`; a los 251, `0:51.0` y `200 m` (el salto de 2404 m no suma); a los 252, `0:52.0` y `204 m`.

  [dotnet] (ciclo de vida de prueba)
- **AC-WKT-07-11 · Solo en memoria, por sesión.** *Entonces* parciales y repeticiones existen solo durante la sesión:
  - se reinician al iniciar cada rutina y al volver a reposo;
  - no se guardan en la base, ni en los puntos de telemetría, ni en el historial (paridad; el historial con parciales es BKL-16);
  - una sesión nueva nunca muestra vueltas ni repeticiones de la anterior;
  - cerrar la app a mitad las pierde, pero la sesión y sus puntos quedan (Q-12).

  [dotnet] · `workout_provider.dart:128`, `:315`; `docs/superpowers/specs/2026-10-02-series-splits-design.md` ("Fuera de alcance")
- **AC-WKT-07-12 · Igual que el legacy (oráculo).** *Entonces*, con la misma secuencia de pasos y lecturas, el estado de parciales y repeticiones después de cada segundo es idéntico al del `SeriesTracker` del legacy corrido desde su código. Se comparan:
  - las vueltas (número y segundos) y si el paso es de trabajo;
  - la vuelta en curso (metros y segundos);
  - si está en serie, la repetición, la cantidad de repeticiones y sus metros;
  - la lista de anteriores (repetición, total, trabajo, segundos y split).

  Los segundos, con una tolerancia de 1e-9. Guiones:
  - los 12 casos legacy;
  - `Series 600`, `Cinco reps`, `Dos series`, `Pirámide 500` y `Corta`, con `R-4`, `R-3`, `R-4A`, `R-4R`, `R-4Z`, `R-4D`, `R-4P` y `R-4X`;
  - pausas cortas (1 s) y largas, también una que cruza un cambio de paso;
  - un paso de 0 s.

  El tracker se alimenta con la secuencia de pasos de la app, así que DV-26 no lo cambia. [dotnet] (oráculo, `tools/oracle/run-oracle.ps1`)

### 4.2 F-WKT-08 — Repeticiones de la serie y comparación

- **AC-WKT-08-01 · En serie.** *Entonces* la sesión está "en serie" cuando el paso del que vienen las lecturas pertenece a una serie. Su posición (serie, repetición desde 1 y cantidad de repeticiones) es la de la rutina expandida (AC-RUT-07-01, LT-16). Un paso suelto es (sin serie, 1, 1) y no está en serie. LT-50: en serie, rep 1 de 5. [dotnet] · `series_tracker.dart:80-82`; `lib/core/models/routine.dart:74-97`
- **AC-WKT-08-02 · Metros de la repetición.** *Entonces* son los metros saneados (AC-WKT-07-06) desde que empezó la repetición, con `⌊ ⌋`, sumando todos sus pasos (trabajo, descanso, enfriamiento…). Nunca bajan dentro de una repetición (LT-57).
  - LT-50: 620 m (500 de trabajo y 120 de enfriamiento).
  - `Series 600` con `R-4`: 600 al terminar el trabajo de la rep 1 (180 s) y 680 al terminar su descanso (200 s).

  [dotnet] · `series_tracker.dart:83`
- **AC-WKT-08-03 · Cambio de repetición.** *Entonces* empieza una repetición nueva cuando, al entrar a un paso, cambia la serie o el número de repetición respecto del paso anterior. Dos pasos sueltos seguidos no son un cambio; no se ve, porque fuera de una serie no hay línea de repetición. [dotnet] · `series_tracker.dart:147-152`
- **AC-WKT-08-04 · El resumen, la más reciente primero, hasta 3.** *Entonces*, si la repetición que termina es de la misma serie que la nueva, su resumen va al frente de la lista: repetición, metros totales (`⌊ ⌋`), metros de trabajo (`⌊ ⌋`) y segundos de trabajo. Si quedan más de 3, se va la más vieja.
  - LT-51: rep 1, 620 m, 500 m, 100 s y split 100.
  - LT-52: con 5 repeticiones, [4, 3, 2].
  - `Cinco reps` con `R-4`, a los 541 s: reps 4, 3 y 2, cada una con 540 m y `2:05`.

  [dotnet] · `series_tracker.dart:41`, `:153-166`
- **AC-WKT-08-05 · Otra serie o un paso suelto vacían la lista.** *Entonces* al pasar a otra serie o a un paso suelto, la lista se vacía: la comparación es dentro de la serie en curso (LT-53). Con `Dos series` y `R-4`:
  - a los 120 s, la lista tiene `Rep 1` de la serie A (`2:05`, 240 m);
  - a los 121, está vacía (serie B, rep 1);
  - a los 181, tiene `Rep 1` de la B;
  - a los 241, en el descanso suelto, la sesión ya no está en serie.

  [dotnet] · `series_tracker.dart:167-170`
- **AC-WKT-08-06 · El split de trabajo.** *Entonces* el split de una repetición es `segundos de trabajo / metros de trabajo × 500`, con los segundos y los metros de las lecturas de sus pasos de trabajo (sin pausas). Los segundos de un salto que no suma cuentan; sus metros, no. Sin metros de trabajo no hay split (LT-54) y el panel muestra `—`.
  - `Series 600` con `R-4`: 150 s y 600 m dan 125 s: `2:05`.
  - Con `R-4D`: 154 s y 600 m dan 128,3 s: `2:08`.
  - `Corta` con `R-4P` (parado): `—`.

  [dotnet] · `series_tracker.dart:31-33`, `:129-131`
- **AC-WKT-08-07 · La última repetición no se compara.** *Entonces* la última repetición de una serie nunca entra en la lista: al salir de la serie la lista se vacía, y si la rutina termina dentro de la serie, no se agrega. En una serie ×1 siempre se ve `Rep 1/1`, con la lista vacía. [dotnet] · `series_tracker.dart:153-174`
- **AC-WKT-08-08 · Casos de la app.** *Entonces*, con `R-4`:
  - `Series 600`: a los 201 s, `Rep 1` con 680 m, 600 de trabajo, 150 s y `2:05`; a los 371, [`Rep 2`, `Rep 1`], iguales; a los 541 (el enfriamiento, suelto), fuera de la serie y con la lista vacía;
  - `Pirámide 500`: a los 879 s, `Rep 1` con 1112 m (se ve `1.112 m`), 752 de trabajo, 188 s y `2:05`;
  - `Prueba 4a`: a los 121 s, `Rep 1` con 360 m y `2:05`;
  - `Corta`: a los 16 s, `Rep 1` con 60 m y `2:05`; la rutina termina a los 30 s sin agregar la rep 2.

  [dotnet]

### 4.3 F-WKT-18 — Paneles colapsables de parciales y repeticiones

- **AC-WKT-18-01 · Cuándo se ven.** *Dado* una rutina, *entonces*:
  - debajo de SPM va siempre el panel de parciales, en todos los pasos;
  - debajo de él va el de repeticiones, **solo** mientras la sesión está en serie (AC-WKT-08-01; LT-61);
  - en el libre no hay paneles ni lugar reservado: SPM queda sola, como en la 4b;
  - desde el primer cuadro de la sesión, los paneles corresponden al primer paso: si es de trabajo, la vuelta en curso en `0:00.0` y `0 m`; si está en serie, `Rep 1/N · 0 m` y el panel de repeticiones (**DV-48**). El legacy muestra durante el primer segundo `Sin parciales en este paso` y no muestra el panel de repeticiones;
  - al salir de una serie, el panel de repeticiones se va (en `Series 600`, a los 541 s). Vuelve con la serie siguiente, en el estado que tenía (abierto o cerrado).

  [dotnet] (la regla) · [Editor-PM] · [Captura] CAP-01, CAP-07, CAP-08 · `immersive_workout_screen.dart:105-106`; `series_panels.dart:160-196`
- **AC-WKT-18-02 · Cerrado: un chip.** *Entonces* cerrado, cada panel es un chip de vidrio de **137 × 34** base, el mismo ancho que abierto (SD-01, DV-50), con 10 de relleno a los lados y 8 arriba y abajo. En su renglón van:
  - el ícono de 14, en el color del panel al 80 %;
  - a 4, el texto corto con el estilo de los rótulos de las tarjetas de la app: 13/600, con 0,08 em de espaciado, en el color pleno (SD-17, DV-51);
  - pegada al borde derecho del contenido, la flecha "expandir", dibujada, en blanco al 54 % (`text.tertiary`). Entre el texto y la flecha queda vidrio vacío (SD-18, DV-50).

  Por panel:
  - **parciales**: ícono de cronómetro y `hudLapsChip` (`500 m`), en `metric.split` `#FF9F1C`;
  - **repeticiones**: ícono de historial y `hudRepsChip` (`Reps`), en `metric.distance` `#4CC9F0`.

  Llevan el vidrio de las tarjetas (AC-WKT-17-01): `glass.fallback`, borde de 1 al 10 % y radio 14, sin reflejo. Todo va por la escala de los paneles (`sp`, §0.3): en TV miden 144,4 × 35,8 dp. El legacy: un chip del ancho de su contenido, de 130 como mínimo (el de su tarjeta SPM), con el rótulo al 80 % y la flecha a 6 del texto. Sin preferencias, los dos arrancan cerrados (LT-59). [Editor-PM] · [Captura] CAP-01, CAP-15 · `series_panels.dart:28-39`, `:165-192`, `:223-229`, `:249-259`
- **AC-WKT-18-03 · Abierto: la cabecera y el ancho.** *Entonces* abierto, el panel conserva el ancho del chip, **137 base fijo** (SD-02, DV-50): abrir y cerrar cambia solo el alto. Lleva una cabecera, en el mismo renglón del chip, y a 6 su contenido. La cabecera tiene:
  - el mismo ícono;
  - a 4, el título en una línea: `hudLapsTitle` (`Parciales 500 m`) o `hudRepsTitle` (`Series anteriores`), en 11/600, sin espaciado de letras y en el color pleno del panel (SD-15, DV-51). El legacy usa el estilo del chip (13/600, 0,8 de espaciado, al 80 %), que en 137 no entra;
  - la flecha "contraer", en el lugar de la del chip (es la misma, girada).

  Los dos títulos van a **una misma escala por idioma**, la que necesita el más largo, así no cambian de tamaño cuando aparece o se va el panel de repeticiones: en español, 0,949 (por `Series anteriores`; lo mide Unity, R5-02); en inglés, 1. En un idioma en que no entrara, el título se achica hasta 0,8 y, solo debajo, se recorta con `…` al final. [Editor-PM] · [Captura] CAP-02, CAP-12 · `series_panels.dart:225-248`, `:260-263`
- **AC-WKT-18-04 · Parciales en un paso de trabajo.** *Entonces* el contenido es:
  1. **la vuelta en curso**, en una línea: el tiempo en `M:SS.d` (AC-WKT-11-09), en `num.lap` (28) y `metric.split.hud`, desde el borde izquierdo; y `{metros} m` (`hudUnitMeters`), en 13 y `metric.split.hud` al 80 % (la regla de las unidades del HUD, DV-31 de la 4a), **pegado al borde derecho** del contenido, en la misma línea de base y con al menos 6 de aire. Así no se corre cuando el tiempo cambia de ancho (SD-03, SD-16, DV-51). El legacy lo pone a 6 del tiempo, en 14/500 al 55 %;
  2. a 4, **las últimas 4 vueltas, de la más reciente a la más vieja**: una fila de 20 por vuelta, con 2 entre filas (SD-19; el legacy deja 2 arriba y 2 abajo de cada fila, 4 entre filas). Cada fila lleva `hudLapNumber` (`#3`) en 12/600 y `text.secondary`, en una columna del ancho de `#99` (o del rótulo más ancho a la vista), y a 4 el tiempo de la vuelta en `M:SS.d`, en `num.row` (20) ámbar (SD-03, DV-51). El legacy: `#3` en 15/600 al 60 % en una columna de 52 y el tiempo en 22/800.

  Con más de 4 vueltas, las más viejas no se ven. Sin vueltas, la línea de la repetición sigue a la vuelta en curso sin el aire de 4 (el legacy deja los 4 también sin vueltas, `:305`; SD-19).
  - `Fondo 3000` con `R-4A`, a los 600 s: `0:50.0` `250 m`; `#5 1:40.0`, `#4 1:40.0`, `#3 1:40.0` y `#2 2:05.0`; `#1` no se ve.
  - `Trabajo 1000` con `R-3`, a los 170 s: `0:03.3` `10 m` y `#1 2:46.7`.

  [dotnet] (qué filas y qué textos) · [Editor-PM] · [Captura] CAP-02, CAP-08, CAP-09 · `series_panels.dart:41-53`, `:279-311`, `:351-390`
- **AC-WKT-18-05 · Fuera de un paso de trabajo.** *Entonces*, en calentamiento, descanso y enfriamiento, en lugar de la vuelta en curso y la lista va `hudLapsEmpty` (`Sin parciales en este paso`), de 15, en `text.tertiary` (blanco al 54 %, como el legacy), siempre en dos renglones, cortado donde las dos líneas quedan más parejas: `Sin parciales` / `en este paso` (`No splits` / `in this step`) (SD-13). El texto no cambia; el legacy no lo limita a una línea. `Series 600` a los 190 s (CAP-05). [Editor-PM] · `series_panels.dart:55`, `:286-287`
- **AC-WKT-18-06 · La línea de la repetición.** *Entonces*, si está en serie, al final del panel de parciales, a 6 y también fuera de un trabajo, va `hudRepProgress` (`Rep {rep}/{count} · {meters}`), con los metros de la repetición en el formato de AC-WKT-11-11 (`1.112 m`), en 16/700 y `metric.distance.hud`. Fuera de una serie, no está.
  - LT-59: `Rep 1/3 · 300 m`.
  - `Series 600` con `R-4`: `Rep 1/3 · 560 m` a los 170 s, `Rep 1/3 · 640 m` a los 190 y `Rep 2/3 · 200 m` a los 250.

  [dotnet] · [Editor-PM] · [Captura] CAP-02 a CAP-05 · `series_panels.dart:313-317`
- **AC-WKT-18-07 · Repeticiones anteriores.** *Entonces* el contenido del panel de repeticiones es:
  - sin repeticiones completas, `hudRepsEmpty` (`Sin repeticiones completas`), como el texto vacío de parciales, en dos renglones: `Sin repeticiones` / `completas` (`No completed` / `reps`) (SD-13);
  - si no, una fila de 20 por repetición de la lista (hasta 3, la más reciente arriba: AC-WKT-08-04), con 2 entre filas (SD-19). Cada fila lleva `hudRepLabel` (`Rep 2`) en 12/600 y `text.secondary`, en una columna del ancho del `Rep n` más ancho a la vista; a 4, el split de trabajo en `M:SS` (AC-WKT-11-10) o `—`, en `num.row` (20) ámbar; y, pegados al borde derecho, los metros totales (AC-WKT-11-11) en 13/700 y `metric.distance.hud`, con al menos 6 de aire (SD-04, DV-51). Van en columnas, sin `·`, como en el legacy. Las filas a la vista comparten una misma escala, así las columnas quedan alineadas cuando una no entra (AC-WKT-18-17): `Rep 1 2:05 1.112 m` va a ≈ 0,97. El legacy: `Rep 2` en 15/600 al 60 % en una columna de 52, el split en 22/800 y los metros en 16/800.

  Ejemplos: `Series 600` a los 380 s, `Rep 2 2:05 680 m` y `Rep 1 2:05 680 m`; `Pirámide 500` a los 880 s, `Rep 1 2:05 1.112 m`; `Corta` con `R-4P` a los 16 s, `Rep 1 — 0 m`; LT-59, `Sin repeticiones completas`. [dotnet] · [Editor-PM] · [Captura] CAP-04, CAP-10, CAP-11 · `series_panels.dart:323-347`, `:351-390`
- **AC-WKT-18-08 · Cuándo cambian.** *Entonces* los valores de los paneles cambian una vez por segundo del motor, en el mismo cuadro en que cambia el reloj de la banda (AC-WKT-13-14 de la 4b). No cambian con cada lectura del monitor, ni en pausa, ni entre segundos: la vuelta en curso no cuenta décimas en vivo, que serían valores inventados (ANIMATION_BIBLE §0.1).
  - En el segundo en que termina un paso, los paneles todavía muestran ese paso, con la vuelta que se haya cerrado en ese segundo, aunque la barra ya muestre el siguiente.
  - Desde el segundo siguiente muestran el nuevo, y recién ahí la repetición que terminó entra en la lista (paridad; P-01).
  - `Series 600` a los 180 s: la barra en `Descanso` y los paneles con `0:25.0` `100 m`, `#1 2:05.0` y `Rep 1/3 · 600 m`; a los 181, `Sin parciales en este paso`.

  [dotnet] · [Editor-PM] · [Captura] CAP-06, VID-03, VID-04 · `workout_provider.dart:176-186`
- **AC-WKT-18-09 · Un toque abre o cierra.** *Cuando* se toca cualquier parte de un panel (el chip, la cabecera o el contenido), *entonces* ese panel pasa de cerrado a abierto o al revés, y la elección se guarda en el acto (LT-59).
  - Cada panel es independiente. Un toque en uno nunca activa el otro, aunque estén a 8: sus áreas táctiles (ART_BIBLE §10.4) no se superponen. Cerrado, el área es el chip agrandado hacia arriba y hacia abajo hasta 56 dp de alto (48 como mínimo), y cada uno toma la mitad del aire entre los dos; abierto, el panel entero.
  - Arrastrar para desplazar la columna (AC-WKT-18-13) no abre ni cierra.
  - Responde también en pausa. No responde con un diálogo abierto (el de terminar o el de completado), durante la salida, ni antes de empezar a aparecer en la entrada (AC-WKT-13-07).
  - Abrir o cerrar no cambia nada del motor, de la sesión ni de la telemetría (AC-WKT-13-14).

  [Editor-PM] · `series_panels.dart:171-175`, `:186-190`, `:220-222`
- **AC-WKT-18-10 · Se recuerda.** *Entonces* el estado de cada panel se guarda en las preferencias de la app con las claves del legacy, `immersive.lapsExpanded` e `immersive.repsExpanded` (booleanas), y vale para todas las rutinas y las sesiones siguientes, también después de cerrar y abrir la app.
  - Sin clave, cerrado (LT-59).
  - Con la clave en verdadero, el panel arranca abierto (LT-60) desde el primer cuadro en que se ve, sin pasar por cerrado (§11.1.1).
  - Si el usuario toca el panel antes de que se termine de leer la preferencia, gana lo que eligió.
  - El de repeticiones conserva su estado aunque no se vea.

  [dotnet] (la regla, con un archivo temporal) · [Editor-PM] · [Device-Win] (cerrar y abrir el build) · `series_panels.dart:120-156`
- **AC-WKT-18-11 · Las preferencias, a salvo.** *Entonces*:
  - el archivo de preferencias está en la carpeta de datos de la app y lo comparten el build de simulador y el normal (ARCHITECTURE §9.4, como el legacy). Nunca se leen ni se importan las preferencias del legacy (Q-11);
  - si el archivo no existe, está vacío o dañado, los paneles arrancan cerrados, sin aviso ni falla, y el próximo toque guarda un archivo válido;
  - si una de las dos claves tiene otro tipo (por ejemplo, `immersive.lapsExpanded` con un texto en lugar de un booleano), **ese panel** arranca cerrado, sin aviso ni falla, y **el otro arranca con su clave**. El próximo toque de ese panel guarda su clave como booleana, y las demás claves del archivo se conservan. El legacy, con `immersive.lapsExpanded` de otro tipo, no lee ninguna de las dos (§11.1.1);
  - si guardar falla, el panel cambia igual y no hay aviso. Las sesiones siguientes de la misma ejecución abren como lo dejó el usuario; después de cerrar y abrir la app, como quedó guardado la última vez. Es lo que hace el legacy: `setBool` cambia la copia en memoria de `shared_preferences` antes de escribir (`series_panels.dart:153-156`; `shared_preferences` 2.5.4, `lib/src/shared_preferences_legacy.dart:177-187`);
  - el archivo se guarda entero o no se guarda: un corte en medio del guardado no lo deja a medias;
  - **ninguna prueba, captura ni video lee ni escribe el archivo de preferencias del usuario**: cada uno usa el suyo, temporal (AC-HIS-20-04, §5.2).

  [dotnet] · [QA]
- **AC-WKT-18-12 · Con el teclado.** *Entonces*, en la PC, los paneles se enfocan con Tab y muestran el anillo de foco (`focus.ring`), que los sigue cuando cambian de tamaño. Enter o Espacio los abren o cierran, y guardan, como un toque.
  - El orden en el inmersivo es `Terminar` → `Pausa` / `Reanudar` → parciales → repeticiones (si se ve) → la llave (con el simulador) → `Terminar` (P-06).
  - Si el panel enfocado desaparece (el de repeticiones, al salir de la serie), no queda un foco invisible: el Tab siguiente va a `Terminar`.
  - Con un diálogo abierto, Tab no llega a los paneles.

  El legacy no los deja enfocar (§11.1.1). [Editor-PM] · [Device-Win] · [Captura] CAP-26
- **AC-WKT-18-13 · El lugar y el espacio.** *Entonces*:
  - van debajo de SPM, a **8·s** (SD-05, DV-50; el legacy, a 10), alineados a su izquierda: parciales arriba y repeticiones 8·sp debajo (paridad). La columna sigue el borde de abajo de SPM cuando SPM gana o pierde su objetivo en un cambio de paso, como Watts debajo de Split;
  - la columna termina 8·s arriba de la banda, en vertical y en horizontal (el legacy: margen inferior + 200 en vertical, y + 16 + 66 + 8 en horizontal);
  - **la escala de los paneles** es la del HUD, salvo en W1280, donde es **1,20** y no 1,6: la mayor con la que entra sin desplazar lo que pide el punto de abajo. Nunca baja de 0,9, la escala más chica del HUD. Las tarjetas no cambian;
  - lo que no entra se **desplaza**, con el dedo o con la rueda del mouse, sin barra a la vista y empezando arriba. En W1280 se desplazan las filas de cada panel abierto, dentro de él, y quedan fijas su cabecera, la vuelta en curso y la línea de la repetición; en TH y L640 se desplaza la columna entera, como el legacy. Cuando cambia el contenido, lo que se ve no salta;
  - en los cinco perfiles, los dos chips cerrados se ven enteros y se tocan sin desplazar, también en TH y L640 con SPM con objetivo (76 de 77,1 base);
  - en TV, P360 y W1280, con los dos paneles abiertos y la serie en marcha, se ven sin desplazar la vuelta en curso, la vuelta más reciente, la línea de la repetición y la repetición más reciente, como en `docs/image-03.png` del legacy (CAP-18), también con 4 vueltas y 3 repeticiones;
  - en TH y L640 (debajo de SPM entran 77,1 base con objetivo en SPM y 93,9 sin, contra ≈ 133 y ≈ 82 dp en el legacy: §15), con parciales abierto se ven sin desplazar su cabecera y la vuelta en curso; el resto se desplaza.

  [Editor-PM] · [Captura] CAP-14 a CAP-18 · `immersive_workout_screen.dart:611-633`
- **AC-WKT-18-14 · La zona del bote.** *Entonces*:
  - **en horizontal** (TH, L640 y W1280), ningún panel entra en la zona del bote (AC-WKT-17-09), y ninguno se angosta: con el ancho de 137 (DV-50), el borde derecho queda a 96,8 dp de la zona en TH, a 39,8 en L640 y a 171,6 en W1280. El legacy, con 200, entra 18 dp en la zona en L640;
  - **en vertical** (TV, P360), con los dos paneles cerrados los chips no entran (en TV terminan 15 dp arriba de la zona con objetivo en SPM, y 32,7 sin). Los paneles **abiertos** sí pueden entrar, porque los abre el usuario, como en el legacy, pero **solo en la franja de afuera** de la zona, nunca en su columna del centro: la del disco, el casco y el remero, desde `ancho / 2 − 44·s` (SD-06). En TV entran de 115,1 a 159,1 dp, el 24 % del ancho de la zona; el legacy, con 200, llega a 225,6 dp (el 61 %) y pasa el centro del remero. Se revisa con el bote a la vista en la 6a o en la 7 (P-03);
  - **el chip que baja con un panel abierto cuenta como la columna abierta**: con parciales abierto, el chip `Reps` cerrado baja con él (8·sp debajo, AC-WKT-18-13) y puede entrar en la zona, con la misma regla: solo en la franja de afuera, nunca en la columna del centro. En TV queda de 339,8 a 375,7 dp de alto en CAP-02 y de 331,4 a 367,2 en CAP-20, con la zona desde 323,8, y su borde derecho en 159,1, como el de parciales. Un chip cerrado en su lugar, sin un panel abierto arriba, nunca entra.

  [Captura] (con el contorno de la zona) CAP-03, CAP-14, CAP-16 a CAP-18; el chip que baja, CAP-02 y CAP-20 (con los rectángulos del `manifest.json` y la auditoría de las tomas, sin contorno)
- **AC-WKT-18-15 · Con la cuenta grande y `PAUSA`.** *Entonces* la cuenta grande (AC-WKT-14-18) y `PAUSA` (AC-WKT-13-12) nunca se enciman con un panel, en ningún cuadro, y **se ven siempre en su lugar de la 4b**, con los paneles cerrados o abiertos, en los cinco perfiles (SD-07, DV-50, P-04):
  - los paneles terminan 8·s antes del disco: en TV, a 8,4 dp; en P360, a 7,4 (CAP-19, CAP-20, CAP-22). En horizontal quedan lejos (CAP-21, CAP-23);
  - abrir o cerrar un panel con la cuenta grande o `PAUSA` a la vista no las mueve, no las esconde y no se encima con ellas en ningún cuadro (VID-09);
  - si el usuario eligiera los 200 del legacy (DV-50), vale la regla de reserva: en vertical, si un panel abierto ocupa su lugar, esa vez no se muestran. La información sigue a la vista: la cuenta de la barra crece y se tiñe, la píldora late y, en pausa, se ven el velo, `Reanudar` y el reloj quieto. La regla queda probada aunque con 137 no se use.

  `Series 600`, en el descanso de la rep 1, a los 197,6 s: se ve `3`, con los paneles cerrados (CAP-19) y con `500 m` abierto (CAP-20). [dotnet] (la regla de lugar) · [Editor-PM] · [Captura] CAP-19 a CAP-23, VID-09
- **AC-WKT-18-16 · Nada se encima.** *Entonces*, en TV, P360, TH, L640 y W1280, cerrados y abiertos, con pulsómetro, con el sello (`Prueba 4a` a los 90,6 s, CAP-24) y con los valores largos, los paneles no se enciman con la barra (tampoco con el sello), ninguna tarjeta, la banda, kcal y BPM, los controles, la llave (salvo su única excepción, en L640 con el diálogo de completado: AC-BLE-16-02), la cuenta grande ni `PAUSA`, ni salen de la pantalla. La cinta `SIMULADOR` no los toca. Las tarjetas no se mueven cuando los paneles se abren o se cierran. El panel del simulador abierto puede taparlos en parte: es la herramienta que abrió el usuario (AC-BLE-16-03, §5.2). [Editor-PM] · [Captura] CAP-01 a CAP-26
- **AC-WKT-18-17 · Nada se corta.** *Entonces* ningún número ni texto de los paneles se corta, se parte en dos líneas o sale de su panel: lo que no entra en el ancho se achica, como las cifras del HUD (ART_BIBLE §5.2). Las únicas excepciones son los dos textos vacíos, que van siempre en dos renglones (AC-WKT-18-05, -07), y los títulos, con su regla (AC-WKT-18-03). Las filas de un panel se achican juntas, con una escala común.
  - Con `S-LARGO` (§0.4), en es y en, todo entra entero en TV, P360, TH, L640 y W1280: `5999:59.9` `499 m`; `#1999` a `#1996` con `5999:59.9`; `Rep 999/999 · 999.999 m`; `Rep 999`, `Rep 998` y `Rep 997` con `5999:59` y `999.999 m`.
  - Con `R-4X` (los extremos de FTMS), los paneles no se cuelgan ni muestran valores negativos.
  - Valores más largos que los de `S-LARGO` (por ejemplo, un split de días con un metro de trabajo) se achican igual y, en el extremo, se recortan con `…` al final: nunca rompen el HUD.

  El legacy corta con `…` la vuelta en curso y los valores de las filas, y parte en dos líneas `Rep 100` y la línea de la repetición (**DV-49**). [Editor-PM] · [Captura] CAP-25
- **AC-WKT-18-18 · Abrir y cerrar se siente (mejora EXR).** *Entonces*, al abrir o cerrar:
  - el panel cambia de alto con un resorte suave (`spring.smooth`, ANIMATION_BIBLE §2.10). El ancho no cambia (SD-08);
  - al abrir, el contenido entra con un fundido entre los 80 y los 240 ms, a medida que el vidrio lo descubre, y nunca se dibuja fuera del vidrio. Al cerrar, el contenido se va primero (0 a 120 ms) y el alto empieza a bajar a los 80. El texto corto del chip y el título se cruzan con un fundido, y la flecha gira 180° en 240;
  - el panel de abajo se desliza con él, siempre a 8·sp, sin saltos, y nada más del HUD se mueve;
  - un toque durante la animación la invierte desde donde está.

  Con reducir movimiento, el tamaño cambia en el acto y el contenido funde (AC-APP-05-08). Nada de esto demora el guardado ni los valores. [Editor-PM] · [Captura] VID-01, VID-08
- **AC-WKT-18-19 · Una vuelta nueva se nota (mejora EXR).** *Cuando* se cierra una vuelta, *entonces*, desde el mismo cuadro del segundo que la cierra (SD-09):
  - la lista baja una fila (de 0 a 160 ms) y la vuelta nueva entra arriba (de 80 a 240 ms), con un fundido y deslizándose desde −8·sp. A los 200 ms se lee entera (al 98 % o más);
  - a su izquierda, dentro del relleno, una **marca** ámbar (`metric.split`) de 3 de ancho y el alto de la fila, que se apaga entre los 600 y los 1400 ms. Reemplaza al fondo ámbar al 20 % de ANIMATION_BIBLE §2.10: debajo del valor, ese fondo lo deja en 4,857:1 de contraste, por debajo del 7:1 de los valores del HUD (DT5-03);
  - la vuelta en curso vuelve a contar desde `0:00.0`, con el rodillo de sus cifras;
  - si ya había 4, la más vieja se va con un fundido, sin pasar sobre la línea de la repetición y sin que el panel salte.

  Una repetición nueva en `Series anteriores` entra igual, con la marca en `metric.distance`. Los demás cambios de contenido tampoco saltan, y cambia solo lo que cambia (SD-12):
  - de la vuelta en curso a `Sin parciales en este paso`, o al revés, un fundido cruzado;
  - de un trabajo a otro trabajo, la vuelta en curso cambia con su rodillo; entre dos pasos que no son de trabajo, nada se mueve;
  - la línea de la repetición cambia con su rodillo;
  - el alto que cambia por un dato se acomoda con el **empuje** de la lista (crece de 0 a 160 ms y se achica de 80 a 240), junto con lo que está debajo. El resorte queda para abrir y cerrar;
  - con el panel cerrado no se anima nada: al abrirlo, la lista ya está, sin marcas;
  - al volver del segundo plano o al rotar, no se anima lo que ya pasó (AC-WKT-02-08).

  Con reducir movimiento, fundidos sin deslizamiento, y la marca igual (es opacidad, no movimiento). `Series 600` a los 325 s (VID-02). [Editor-PM] · [Captura] VID-02 a VID-04, VID-08
- **AC-WKT-18-20 · La pausa, la rotación y el fin.** *Entonces*:
  - en pausa, los paneles quedan quietos con los valores del último segundo y se pueden abrir y cerrar;
  - al rotar o cambiar el tamaño de la ventana, conservan su estado y sus valores y se reacomodan, sin reiniciarse (AC-WKT-17-07);
  - con la rutina terminada, detrás del diálogo de completado, muestran el último segundo (en `Series 600`, el enfriamiento: `Sin parciales en este paso` y sin repeticiones) y no responden;
  - al terminar a mano, salen con la última foto (AC-WKT-13-08).

  [Editor-PM] · [Captura] CAP-22, VID-07
- **AC-WKT-18-21 · Fluidez y presupuesto (R4a-03).** *Dado* el peor caso de la It. 5, *entonces* el HUD:
  - entra en el tope de elementos visibles de ARCHITECTURE §20.2 (60), o en un tope nuevo que el Architect mida y deje escrito ahí, con la medición que lo justifica. Cada panel suma un solo elemento, en cualquier estado, y se juntan otros dos del HUD sin cambiar lo que se ve (SD-10);
  - no reserva memoria por cuadro en régimen, medido como en la 4b (con el registro de asignaciones de Unity: el contador del hilo siempre da 0 en Mono);
  - en el build de simulador, en la PC, con `Fondo 3000` y `Series 600` y los paneles abriéndose y cerrándose, no tiene ningún cuadro de más de 33 ms de CPU del hilo principal y su p99 no pasa de 16,7 ms (`[PERF]`).

  El peor caso es el de la 4b (los tres objetivos fuera, BPM, `Próximo`, la cuenta grande y el sello) más los dos paneles abiertos, con la vuelta en curso, 4 vueltas, la línea de la repetición, 3 repeticiones y una vuelta nueva entrando. [Editor-PM] (medición) · [Device-Win]
- **AC-WKT-18-22 · En inglés.** *Entonces*, con la app en inglés, los paneles dicen `500 m`, `Reps`, `500 m splits`, `Previous reps`, `No splits in this step`, `No completed reps`, `Rep 2/3 · 200 m`, `Rep 1` y `#1`. Los números llevan los mismos formatos que en español (Q-06): el punto decimal y el punto de miles de AC-WKT-11-11, como `1.112 m` (P-08). Nada se corta (AC-WKT-18-17). [dotnet] (catálogo) · [Editor-PM] · [Captura] CAP-12, CAP-13

### 4.4 También toca

**F-APP-10 (catálogo; se cerró en la It. 1).**

- **AC-APP-10-12 · 9 claves nuevas.** *Entonces* el catálogo pasa de 214 a **223** claves en es y en, con las 9 de §11.2 (textos y parámetros exactos). El control de la cantidad pasa a 223 y ninguna clave queda en un solo idioma. Los textos que el legacy tiene fijos en español en los paneles salen del catálogo con el mismo español (Q-01); los números y los formatos (`:`, `.`, `—`, `m` y los dígitos) no son claves. [dotnet] · FEATURE_INVENTORY Q-01

---

## 5. Criterios de iteraciones anteriores que se verifican acá

### 5.1 La deuda de la 4b, primero (antes de construir encima)

Es la de [REPORT de la 4b §9.2](../04b-hud-exr/REPORT.md). QA la verifica antes de que se sumen los paneles, salvo la medida en el build de AC-WKT-13-15, que puede quedar para el cierre (abajo). Son 3 criterios nuevos, que cuentan en §4.0.

- **AC-WKT-13-15 · La entrada, sin tirón (QA-V4b-06).** *Entonces*, en el build de simulador en la PC, mientras algo se mueve en pantalla durante la entrada (el fundido a negro de Workout, la aparición del fondo y la cascada), ningún cuadro pasa de 16,7 ms de CPU del hilo principal. Un cuadro más largo solo se admite con la pantalla entera en negro y quieta. Se mide en tres entradas seguidas: libre, `Prueba 4a` y `Cinco reps` con los dos paneles abiertos (preferencias en verdadero; empieza con la serie, así los dos se ven desde la entrada). La entrada no cambia: los tiempos de AC-WKT-13-06 y el reloj que corre desde el toque (AC-WKT-13-01).

  **Riesgo aceptado (QA-G5T-14).** Si el `RowMate.exe` del usuario sigue abierto y el build no se puede rehacer (§12 punto 11), WP-B1 sigue igual: el orden de la entrada se verifica antes en el Editor, y la medida en el build se hace al cierre de la iteración, con las tres entradas y los paneles ya construidos. Antes del cierre, el coordinador le pide al usuario que cierre su `RowMate.exe`. Si al cierre sigue abierto, la parte `[PERF]` queda NO VERIFICABLE, con el motivo. [Device-Win] (`[PERF]`) · [Editor-PM] (el orden de la entrada)
- **AC-WKT-13-16 · Una falla de la presentación no traba la sesión (O-4 y O-5 de la lente de motion de la 4b).** *Entonces*:
  - si una animación del HUD falla, las demás siguen y la salida termina igual, sin dejar nada animando;
  - si el diálogo de completado no se puede mostrar, la sesión se cierra igual (guardada), el inmersivo sale y Workout queda en reposo, con `workoutSaveError` solo si el guardado falló.

  El error queda en el registro. Se prueba provocando la falla. [Editor-PM]
- **AC-WKT-16-07 · El título del libre, separado del reloj (QA-V4b-R2-01).** *Entonces*, en TV y P360, en español, entre el final de `Entrenamiento Libre` y el ícono del reloj queda al menos el aire que fije el Designer, que nunca es menor que el espacio entre `Entrenamiento` y `Libre`. El título queda a 0,8 de su tamaño o más (AC-WKT-16-06), y el resto de la barra no cambia. CAP-13 a CAP-15 de la 4b se regeneran, con dos corridas iguales. [Captura] · [Editor-PM]

Además, sin criterio propio: **QA-V4b-R2-03** (Unity que revienta al cerrar un `-batchmode` que cambió los *defines*: reintentar y anotarlo en `tools/unity/README.md`) y **O-1** de la lente de motion (dos resortes de la 4a que siguen después de salir; lo mira AC-WKT-13-09, §5.3). **AC-WKT-02-11** (P-17 de la 4a) sigue sin verificar para QA y queda para el usuario. Si él lo prueba y el reloj salta el tiempo suspendido, se reabre y va primero.

### 5.2 Cambian por esta iteración

Valen estos textos, que reemplazan a los de la 4a y la 4b.

- **AC-WKT-13-06 · Entrada (cambia: los paneles).** *Entonces* lo de la 4b, con los paneles en la cascada **junto con SPM**: a los 520 ms, en 280 y con la misma subida corta (8 base; en W1280, 8·sp = 9,6 dp, porque los paneles van con su escala: DT5-10). Llegan ya en su estado guardado y con el primer paso (AC-WKT-18-01), así que nada aparece después de la cascada. En el legacy, el chip `Reps` aparece en el primer segundo. Con reducir movimiento, el fundido de 120 de la 4b. [Editor-PM] · [Captura] VID-06
- **AC-WKT-13-08 · Salida, con la última foto (cambia).** *Entonces* lo de la 4b; los paneles también salen con la última foto: su estado y sus valores del último segundo. Nunca quedan vacíos, en `0:00.0`, con un `Sin parciales en este paso` que no tenían, ni desaparecen antes que el resto. [Editor-PM] · [Captura] VID-07
- **AC-WKT-13-12 · El velo de pausa (cambia).** *Entonces* lo de la 4b; con los paneles abiertos, también en vertical, `PAUSA` sigue en su lugar, sin encimarse (AC-WKT-18-15, DV-50). [Editor-PM] · [Captura] CAP-22, CAP-23
- **AC-WKT-13-13 · Nada se encima (cambia).** *Entonces* lo de la 4b, con los paneles cerrados y abiertos (AC-WKT-18-16), también en P360. [Editor-PM] · [Captura]
- **AC-WKT-14-18 · La cuenta grande (cambia).** *Entonces* lo de la 4b y, además, nunca se encima con un panel: se ve en su lugar también con los paneles abiertos, en vertical y en horizontal (AC-WKT-18-15, DV-50). Los instantes de `Estados` y `Final por distancia` no cambian. [dotnet] · [Editor-PM] · [Captura] CAP-19 a CAP-21
- **AC-WKT-17-06 · Valores en vivo (cambia).** *Entonces* lo de la 4a, salvo que "El HUD no tiene otras interacciones que los controles" pasa a "que los controles y los paneles de series" (AC-WKT-18-09). [Editor-PM]
- **AC-WKT-17-09 · La zona del bote (cambia).** *Entonces* ninguna pieza del HUD entra en la zona del bote, salvo, **en vertical**, la columna de series con un panel abierto: el panel abierto y el chip cerrado que baja con él (`Reps` debajo de parciales abierto). Entran solo en la franja de afuera de la zona, nunca en su columna del centro (AC-WKT-18-14, P-03). Un chip cerrado sin un panel abierto arriba nunca entra. [Captura]
- **AC-WKT-02-08 · Al volver, el presente (cambia).** *Entonces* lo de la 4b, también en los paneles: las vueltas y las repeticiones que se cerraron en segundo plano están en el primer cuadro, sin animar su entrada. [Editor-PM] (ciclo de vida de prueba) · [Device-And]
- **AC-APP-05-07 · Teclado y foco (cambia).** *Entonces* lo de la 4b; el orden del inmersivo suma los paneles (AC-WKT-18-12). El primer Tab sigue enfocando `Terminar`, con su anillo. [Editor-PM] · [Device-Win]
- **AC-APP-05-08 · Reducir movimiento (cambia).** *Entonces* lo de la 4b y, además:
  - los paneles cambian de tamaño en el acto (al abrir, en el cuadro del toque; al cerrar, cuando su contenido ya se fue), con su contenido en un fundido, y el panel de abajo se corre en el mismo cuadro;
  - la vuelta nueva aparece con un fundido, sin deslizarse, y su marca se apaga igual que sin reducir movimiento (AC-WKT-18-19);
  - los cambios de contenido son fundidos, sin subir ni bajar, y el alto cambia en el acto.

  Nada de esto cambia los datos ni cuándo pasan. [Editor-PM] · [Captura] VID-08
- **AC-BLE-16-02 · La llave (cambia).** *Entonces* lo de la 4b; tampoco tapa los paneles, cerrados ni abiertos, en los cinco perfiles, con **una sola excepción**:
  - **con el diálogo de completado abierto**, la llave busca primero un lugar que no toque la columna de series (en TV y P360, debajo del diálogo, como en la 4b; en TH, a la derecha de la columna: CAP-09 de la 4b). Si en la ventana no queda ninguno, queda en su lugar de la 4b, aunque tape parte de la columna (de los cinco perfiles, solo pasa en **L640**: el diálogo ocupa el centro, SPM, la columna y la banda llenan el costado izquierdo, y Split, Watts y kcal el derecho; en CAP-10 de la 4b tapa el `Re` de `Reps`);
  - se admite porque ahí la columna está debajo del velo y no responde (AC-WKT-18-20), y cualquier otro lugar taparía otra pieza del HUD, también debajo del velo. La llave sigue arriba del velo y se puede usar, y nunca tapa el diálogo ni `Terminar`;
  - sin el diálogo de completado, o con un lugar libre, no hay excepción.

  La auditoría de la llave corre en toda toma con simulador y acepta solo esa excepción. [dotnet] (la regla) · [Editor-PM] · [Captura] (CAP-09 y CAP-10 de la 4b) · 02-tech-design B5-10
- **AC-BLE-16-03 · El panel del simulador (cambia).** *Entonces* lo de la 4b: Split, Watts, el sello y el lugar de la cuenta grande siguen a la vista y sus altos máximos no cambian. Abierto, puede tapar en parte los paneles de series; la parte que se ve sigue respondiendo. [Editor-PM] · [Captura] DEMO-08
- **AC-HIS-20-04 · Las pruebas no tocan los datos del usuario (cambia).** *Entonces* lo de la 4b y, además, el archivo de preferencias de la app: ninguna prueba, captura ni video lo abre ni lo escribe. QA compara su contenido y su fecha (o que siga sin existir) antes y después de todo. Si QA usa el build de simulador a mano, deja el archivo como lo encontró (si no existía, lo borra). [QA]

### 5.3 Se vuelven a verificar, sin cambios

| Criterio | Qué se mira otra vez |
|---|---|
| AC-WKT-01-05 · Reanudar | La referencia nueva vale para los parciales y para el paso por distancia a la vez (AC-WKT-07-07) |
| AC-WKT-02-04 · No se pierden ni se duplican segundos | Cada segundo procesado alimenta los parciales con su lectura (AC-WKT-07-10) |
| AC-WKT-02-11 · El equipo suspendido no cuenta | Tampoco suma tiempo a la vuelta en curso (AC-WKT-07-10). Lo verifica el usuario en equipo (§5.1) |
| AC-WKT-03-03 · Paso por distancia, con el saneamiento | Mismos metros que los parciales, segundo a segundo (AC-WKT-07-09) |
| AC-WKT-11-09, -10, -11 · Formatos de los paneles (LT-45) | Ahora a la vista: `M:SS.d`, `M:SS` o `—` y `1.245 m` |
| AC-WKT-13-07 · Los controles durante la entrada | Los paneles responden desde que empiezan a aparecer, como los controles |
| AC-WKT-13-09 · Nada queda animando al salir | Tampoco el resorte de un panel, el empuje de la lista, la entrada de una vuelta ni su marca, ni los dos resortes de la 4a (O-1) |
| AC-WKT-13-14 · La presentación no toca el dato | La secuencia del motor es idéntica con los paneles abiertos o cerrados, abriéndose y cerrándose, con reducir movimiento y sin HUD |
| AC-WKT-17-01 · Vidrio y tamaños | Los paneles con el mismo vidrio; no siguen la letra del sistema |
| AC-WKT-17-07 · Orientación | Al rotar con los paneles abiertos, se conservan su estado y sus valores (AC-WKT-18-20) |
| AC-WKT-17-10 · Valores extremos | Con `R-4X` y los paneles abiertos (AC-WKT-18-17) |
| AC-APP-05-05 · Letra del sistema 1,3× | Los paneles, como el HUD, no la siguen |
| AC-BLE-15-09 · La física no se reinicia al reconectar | Con `Simular desconexión` en un trabajo, la vuelta en curso no suma metros durante el corte (≈ 4 s, hasta la primera lectura nueva) y sigue desde los mismos metros; ninguna vuelta inventada ni números negativos (VID-05) |

---

## 6. Casos de test legacy

Los 15 de ROADMAP §5 para la It. 5. Se portan uno a uno, con el mismo nombre y las mismas afirmaciones. Se numeran por línea; el archivo registra primero los cuatro de `_hardening()`, y el orden no cambia nada.

| LT | Archivo:línea | Caso (nombre exacto) | Qué afirma | Dónde | AC |
|---|---|---|---|---|---|
| LT-47 | `test/series_tracker_test.dart:46` | `interpola el tiempo de la vuelta entre lecturas` | 3 m/s durante 170 s en un trabajo de 1000 m: 1 vuelta, número 1, de 166,67 ± 0,01 s; vuelta en curso de 10 m y 3,33 ± 0,01 s; es trabajo | [dotnet] | AC-WKT-07-04, -05 |
| LT-48 | `:59` | `dos vueltas dentro del mismo paso` | 5 m/s durante 220 s en un trabajo de 2000 m: vueltas [100, 100] | [dotnet] | AC-WKT-07-02, -04 |
| LT-49 | `:65` | `un paso de trabajo de menos de 500 m no genera vueltas` | 5 m/s durante 80 s en un trabajo de 400 m: ninguna vuelta | [dotnet] | AC-WKT-07-02 |
| LT-50 | `:71` | `el enfriamiento no genera vueltas pero suma metros a la repetición` | Trabajo de 500 m a 5 m/s durante 100 s y enfriamiento a 2 m/s durante 60 s: no es trabajo, sin vueltas, vuelta en curso de 0 m, en serie, rep 1 de 5, 620 m | [dotnet] | AC-WKT-07-01, -05, AC-WKT-08-01, -02 |
| LT-51 | `:85` | `al cambiar de repetición guarda el resumen de la anterior` | Lo anterior y 10 s de la rep 2 a 5 m/s: rep 2 con 50 m; 1 resumen: rep 1, 620 m, 500 m de trabajo, 100 s y split 100 ± 0,001 | [dotnet] | AC-WKT-08-03, -04, -06 |
| LT-52 | `:102` | `guarda solo las últimas 3 repeticiones, la más reciente primero` | 5 repeticiones (trabajo de 500 m a 5 m/s durante 100 s y enfriamiento de 10 s a 2 m/s): rep 5; lista [4, 3, 2] | [dotnet] | AC-WKT-08-04 |
| LT-53 | `:112` | `una serie nueva o un paso suelto vacían la comparación` | g1 rep 1 y 10 s de la rep 2: 1 resumen; al entrar a g2, vacía; al entrar a un paso suelto, no está en serie y sigue vacía | [dotnet] | AC-WKT-08-05 |
| LT-54 | `:126` | `una repetición sin metros de trabajo no tiene split` | Resumen (1, 100, 0, 0): sin split | [dotnet] | AC-WKT-08-06 |
| LT-55 | `:155` | `una lectura de 0 dentro del paso no genera metros negativos ni dobles` | 250 lecturas de +4, una de 0 y de 1004 a 1100 de a 4: vuelta en curso y metros de la repetición nunca negativos; 2 vueltas de 125 ± 2 s; al pasar a la rep 2 (lectura 1104), los metros de trabajo de la rep 1 quedan entre 1060 y 1110 | [dotnet] | AC-WKT-07-06, AC-WKT-08-04 |
| LT-56 | `:180` | `un primer salto desde 0 no crea vueltas falsas` | Primera lectura en 5000: sin vueltas; después de 140 lecturas de +4, 1 vuelta de 125 ± 2 s | [dotnet] | AC-WKT-07-06, -08 |
| LT-57 | `:191` | `un reinicio del monitor no genera valores negativos` | 200 lecturas de +4; el monitor vuelve a 0 y sube de a 4 (61 lecturas): los metros de la repetición nunca bajan, la vuelta en curso nunca es negativa, hay al menos una vuelta y la primera es de 125 ± 2 s | [dotnet] | AC-WKT-07-06, AC-WKT-08-02 |
| LT-58 | `:211` | `rebase no cuenta el salto al reanudar` | 5 lecturas de +4: 20 m en la repetición; con la referencia nueva en 9000 y una lectura de 9004, 24 m | [dotnet] | AC-WKT-07-07 |
| LT-59 | `test/series_panels_test.dart:39` | `arrancan colapsados y al tocar se expanden y se recuerda` | Tracker a 300 m en el trabajo de la rep 1 de 3, sin preferencias: no se ven `Parciales 500 m` ni `Series anteriores`, y se ven `500 m` y `Reps`. Tocar `500 m`: se ven `Parciales 500 m` y `Rep 1/3 · 300 m`, e `immersive.lapsExpanded` queda en verdadero. Tocar `Reps`: `Sin repeticiones completas` | [Editor-PM] (los paneles montados, con un archivo de preferencias temporal) · [dotnet] (la regla) | AC-WKT-18-02, -03, -06, -07, -09, -10 |
| LT-60 | `:62` | `respeta la preferencia guardada` | Con `immersive.lapsExpanded` en verdadero: se ve `Parciales 500 m` | [Editor-PM] · [dotnet] | AC-WKT-18-10 |
| LT-61 | `:68` | `sin serie no aparece el panel de repeticiones` | Sin grupo: se ve `500 m` y no se ve `Reps` | [Editor-PM] · [dotnet] | AC-WKT-18-01 |

El caso restante de `series_panels_test`, `formatos` (`:32`), ya está portado como LT-45 en la 4a (AC-WKT-11-09 a -11).

---

## 7. Capturas, demo y videos

### 7.1 Reglas

Las de la 4b ([01-po-spec §7.1](../04b-hud-exr/01-po-spec.md)), y además:

- Las tomas van en `docs/migration/iterations/05-series-parciales/snapshots/`, con `manifest.json` e `INDEX.md` **sin rutas de la PC**.
- Salvo que la toma diga otra cosa: español, simulador sin ruido, reloj y zona de prueba, `F-5`, **sin preferencias** (los paneles cerrados) y el pulsómetro sin conectar.
- Ninguna toma cae en un segundo entero del motor: van a los "+0,2 s" (o "+0,6 s", con el reloj común en `p` = 1). Cada toma usa su propio archivo de preferencias temporal.
- En las tomas con un panel recién abierto o cerrado, el resorte ya terminó: el toque es al menos **0,82 s** antes (SD-11: a los 0,6 s al resorte le falta 0,9 base), o el resorte ya está quieto cuando se toma (la regla de toma lo comprueba: DEMO-03 y DEMO-05), o la toma arranca con la preferencia en verdadero.
- Ninguna toma fija cae en los 1400 ms que siguen a una vuelta o una repetición nueva (su marca, AC-WKT-18-19), salvo las que la muestran a propósito. Por eso CAP-11 va a los 881,2 s y DEMO-11 a los 31,6 s (DT5-04).
- Las tomas con desplazamiento, con el desplazamiento en 0 (arriba).
- **Al empezar**, las tomas con rutina de `it4b` se regeneran **una vez**, con dos corridas iguales: ahora muestran los chips debajo de SPM. QA explica cada diferencia que no sea esa o la de AC-WKT-16-07. Las del HUD de `it4a` ya están reemplazadas por las de `it4b` desde la 4b, así que de `it4a` no queda ninguna con rutina para regenerar (DT5-08); las suyas sin rutina tienen que dar los mismos bytes.
- El contorno de la zona del bote va en las tomas que lo piden (AC-WKT-18-14).

### 7.2 Tamaños

**TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720. Escala del HUD: 1,054, 0,92, 1,054, 0,92 y 1,6. La de los paneles (`sp`) es la misma, salvo en W1280: 1,20.

### 7.3 Lista

Todas con `R-4`, salvo que se diga otra cosa.

| ID | Tamaño | Estado (cómo se llega) | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** | TV | `Series 600` a los 60,2 s | **Los chips**: `500 m` (ámbar) y, 8·sp debajo, `Reps` (celeste), cerrados, 8·s debajo de SPM, del mismo ancho (144,4 × 35,8 dp; `Series 600` no tiene objetivos, así que empiezan en y 210,9), con la flecha "expandir" pegada a la derecha y vidrio vacío en el medio. Lo demás como en la 4b: la barra con 8 píldoras, `Trabajo` y `600m`. Nada tapado |
| **CAP-02** | TV | `Series 600` a los 170,2 s, con `immersive.lapsExpanded` en verdadero | `Parciales 500 m` con `0:15.0` `60 m`, `#1 2:05.0` y `Rep 1/3 · 560 m`; debajo, el chip `Reps` cerrado |
| **CAP-03** | TV | `Series 600` a los 250,2 s, con los dos abiertos | **Como `docs/image-03.png`**, con el ancho y el aspecto de la app (DV-50, DV-51): `0:50.0` con `200 m` pegado a la derecha, sin vueltas, y `Rep 2/3 · 200 m`; `Series anteriores` con `Rep 1 2:05 680 m`, los metros pegados a la derecha. Con el contorno de la zona del bote: `Series anteriores` entra en ella solo hasta x 159,1, nunca en su columna del centro (P-03) |
| **CAP-04** | TV | `Series 600` a los 380,2 s, con los dos abiertos | `0:10.0` `40 m`, `Rep 3/3 · 40 m`; `Rep 2 2:05 680 m` arriba y `Rep 1 2:05 680 m` abajo |
| **CAP-05** | TV | `Series 600` a los 190,2 s, con los dos abiertos | En el descanso: `Sin parciales` / `en este paso`, en dos renglones, y `Rep 1/3 · 640 m`; `Sin repeticiones` / `completas`; la barra en `Descanso` |
| **CAP-06** | TV | `Series 600` a los 180,6 s, con los dos abiertos | **El segundo del cambio** (P-01): la barra ya en `Descanso` y `0:20`; los paneles todavía con el trabajo: `0:25.0` `100 m`, `#1 2:05.0` y `Rep 1/3 · 600 m`; `Sin repeticiones completas` |
| **CAP-07** | TV | `Series 600` a los 545,2 s, con las dos preferencias en verdadero | En el enfriamiento: `Sin parciales en este paso`, **sin** la línea de la repetición, y **sin** el panel de repeticiones |
| **CAP-08** | TV | `Fondo 3000` con `R-4A`, a los 600,2 s, con parciales abierto | **Las 4 últimas**: `0:50.0` `250 m`; `#5 1:40.0`, `#4 1:40.0`, `#3 1:40.0` y `#2 2:05.0`, de arriba abajo; sin `#1`, sin la línea de la repetición y sin chip `Reps` |
| **CAP-09** | TV | `Trabajo 1000` con `R-3`, a los 170,2 s, con parciales abierto | **La interpolación** (LT-47): `0:03.3` `10 m` y `#1 2:46.7` |
| **CAP-10** | TV | `Cinco reps` a los 545,2 s, con los dos abiertos | **El tope de 3**: `0:05.0` `20 m`, `Rep 5/5 · 20 m`; `Rep 4`, `Rep 3` y `Rep 2`, cada una con `2:05` y `540 m` |
| **CAP-11** | TV | `Pirámide 500` a los **881,2 s**, con los dos abiertos | **El punto de miles**: `0:03.0` `12 m`, `Rep 2/4 · 12 m` y `Rep 1 2:05 1.112 m`, la fila a su escala (≈ 0,97); arriba, el sello no está (pasó a los 788). A los 880,2 la marca de `Rep 1`, que entró a los 879, todavía se apagaba (DT5-04) |
| **CAP-12** | TV | CAP-03 en inglés | `500 m splits`, `0:50.0` `200 m`, `Rep 2/3 · 200 m`; `Previous reps` con `Rep 1 2:05 680 m`; los títulos enteros |
| **CAP-13** | TV | CAP-05 en inglés | `No splits` / `in this step`, `Rep 1/3 · 640 m` y `No completed` / `reps`, enteros, en dos renglones |
| **CAP-14** | P360 | CAP-04 en un teléfono de 360 | Los paneles abiertos (x 12,9-139,4) no invaden la columna de Split y Watts; nada cortado; los chips y el contenido enteros |
| **CAP-15** | TH | CAP-01 en horizontal | Los dos chips enteros y sin desplazar, entre SPM y la banda; sin fundido de desplazamiento (no hay nada escondido); la zona del bote libre (contorno) |
| **CAP-16** | TH | CAP-04 en horizontal | La columna entera, con el desplazamiento en 0: se ven la cabecera y la vuelta en curso de parciales; abajo, el fundido del borde de la columna; el resto se desplaza. Nada se encima con la banda ni con los controles; la zona del bote libre (contorno) |
| **CAP-17** | L640 | CAP-04 en la ventana baja | Lo mismo en la ventana baja; el panel abierto **no se angosta**: termina 39,8 dp antes de la zona del bote (contorno) |
| **CAP-18** | W1280 | `Series 600` a los 340,2 s, con los dos abiertos | **Comparable con `docs/image-03.png`**: los paneles a la escala 1,20 (164,4 dp de ancho); sin desplazar se ven `0:15.0` `60 m`, `#1 2:05.0`, `Rep 2/3 · 560 m` y `Rep 1 2:05 680 m` (AC-WKT-18-13); la zona del bote libre (contorno) |
| **CAP-19** | TV | `Series 600` a los 197,6 s | **La cuenta grande con los chips**: `3` en su disco, a 8,4 dp de los chips, sin tocar `500 m` ni `Reps`; `Próximo: Trabajo` y el halo de la píldora |
| **CAP-20** | TV | CAP-19 con parciales abierto | **La cuenta grande en su lugar**, a 8,4 dp del panel abierto (DV-50); la cuenta `0:03` de la barra agrandada y teñida; el halo; `Sin parciales` / `en este paso` y `Rep 1/3 · 668 m`; el chip `Reps` debajo |
| **CAP-21** | TH | CAP-19 con los dos abiertos, en horizontal | La cuenta grande `3` en su lugar, con los paneles abiertos a la izquierda; nada se encima |
| **CAP-22** | TV | `Series 600`: `Pausa` a los 250,2 s y la toma a los 252,2 s, con los dos abiertos | El velo; `Reanudar`; los paneles quietos con `0:50.0` `200 m`; **`PAUSA` en su lugar**, a 8,4 dp de los paneles (DV-50) |
| **CAP-23** | TH | CAP-22 en horizontal | El velo y `PAUSA` en su lugar, con los paneles abiertos a la izquierda |
| **CAP-24** | TV | `Prueba 4a` a los 90,6 s, con los dos abiertos | **El sello con los paneles**: `No alcanzado` · `150 / 160 W · 24.0 / 22 spm` en la tercera fila de la barra; las tarjetas no se movieron; `1:00.0` `240 m`, `Rep 1/2 · 240 m` y `Sin repeticiones completas` |
| **CAP-25** | TV | `S-LARGO`, con los dos abiertos | Todos los valores de `S-LARGO` enteros, achicados si hace falta, dentro del panel; nada cortado, partido ni encimado |
| **CAP-26** | W1280 | CAP-18 sin foco; Tab tres veces (`Terminar`, `Pausa`, parciales) | El anillo de foco alrededor de `Parciales 500 m`, entero y visible sobre el vidrio |

### 7.4 Secuencia de la demo

W1280, español, simulador sin ruido a 150 W, `F-5`, sin preferencias. Cada toma sigue a la anterior sin rearmar nada. Los números son los que da el motor con las lecturas del simulador. Van con "≈" porque dependen de cómo caen sus lecturas en los segundos del motor; el arnés anota los exactos en `manifest.json` y QA los compara con el Core.

| ID | Paso | Qué tiene que verse |
|---|---|---|
| **DEMO-01** | Workout | Reposo con `Series 600` primera en la lista |
| **DEMO-02** | `Series 600` → `Iniciar`, a los 10,2 s | El chip `500 m` solo (calentamiento), cerrado, debajo de SPM |
| **DEMO-03** | Tocar `500 m` a los 12 s; la toma, a los 12,8 | `Parciales 500 m` · `Sin parciales en este paso`; sin la línea de la repetición |
| **DEMO-04** | A los 60,2 s | El trabajo de la rep 1: `0:30.0` y ≈ `113 m`, y `Rep 1/3 · ≈ 113 m`; el chip `Reps` debajo |
| **DEMO-05** | Tocar `Reps` a los 61 s; la toma, a los 61,8 | `Series anteriores` · `Sin repeticiones completas` |
| **DEMO-06** | A los 170,2 s | `#1` con ≈ `2:12.6` y la vuelta en curso con pocos metros |
| **DEMO-07** | A los 230,2 s | La rep 2: `Rep 1` con `2:13` y ≈ `676 m`; `Rep 2/3 · …` |
| **DEMO-08** | Llave → `Simular desconexión` a los 240 s; la toma, a los 242,2 con el panel del simulador abierto | La vuelta en curso con el tiempo avanzando y los metros quietos; el panel del simulador no tapa Split, Watts ni la barra |
| **DEMO-09** | Cerrar el panel del simulador; la toma, a los 250,2 s | Los metros siguen desde donde estaban, sin salto; ninguna vuelta nueva inventada |
| **DEMO-10** | `Terminar` → `Terminar` | Workout en reposo, sin diálogo ni aviso |
| **DEMO-11** | `Series 600` → `Iniciar`; la toma, a los **31,6 s** | **La preferencia y la sesión nueva**: los dos abiertos desde que se ven (`Series anteriores` apareció a los 31 s, vacío; a los 31,2 todavía estaba apareciendo: DT5-04); `0:01.0` y ≈ `3 m`; `Rep 1/3 · ≈ 3 m`; nada de la sesión anterior |
| **DEMO-12** | Cerrar los dos; `Terminar` → `Terminar` | Workout en reposo; las preferencias quedan en falso |

### 7.5 Videos

| ID | Tamaño y duración | Qué se hace | Qué tiene que verse |
|---|---|---|---|
| **VID-01** | TV, 3 s | `Series 600` a los 250 s, con repeticiones abierto y parciales cerrado: tocar `500 m` a los 250,3 s y otra vez a los 251,8 | **Abrir y cerrar**: el alto con el resorte (el ancho no cambia), el contenido con su fundido, sin tinta fuera del vidrio; la flecha que gira; `Series anteriores` se desliza hacia abajo y vuelve, siempre a 8·sp, sin saltos; nada más se mueve; el valor del segundo 251 cambia en su cuadro |
| **VID-02** | TV, 4 s | `Series 600` de los 323,5 a los 327,5 s, con los dos abiertos | **La vuelta nueva**: a los 325 la lista baja y `#1 2:05.0` entra arriba deslizándose, con su marca ámbar a la izquierda, que se apaga a los 1,4 s; el alto crece con el empuje, y la línea de la repetición y `Series anteriores` bajan con él; la vuelta en curso pasa a `0:00.0` `0 m` y después a `0:01.0` `4 m`; la banda y el panel cambian en el mismo cuadro |
| **VID-03** | TV, 6 s | `Series 600` de los 178,5 a los 184,5 s, con los dos abiertos | **Del trabajo al descanso**: a los 180, la barra cambia y los paneles siguen con el trabajo un segundo; a los 181 pasan a `Sin parciales` / `en este paso` con un fundido cruzado, con el alto que se acomoda con el empuje, sin saltos, y la línea de la repetición que cambia con su rodillo |
| **VID-04** | TV, 5 s | `Series 600` de los 198,5 a los 203,5 s, con los dos abiertos | **El cambio de repetición**: a los 200, la barra pasa al trabajo de la rep 2; a los 201, `Rep 1 2:05 680 m` entra en `Series anteriores`, la línea pasa a `Rep 2/3 · 4 m` y vuelve la vuelta en curso `0:01.0` `4 m` |
| **VID-05** | W1280, 10 s | Simulador sin ruido, `Series 600` de los 238 a los 248 s, con parciales abierto: `Simular desconexión` a los 240 | Durante el corte (≈ 4 s) la vuelta en curso no suma metros y su tiempo sí; después sigue desde los mismos metros; nada salta, ninguna vuelta inventada (AC-BLE-15-09). Los valores de cada cuadro van en `manifest.json` |
| **VID-06** | TV, 2,5 s | `Cinco reps` → `Iniciar`, con las dos preferencias en verdadero | **La entrada con los paneles** (DV-48): llegan junto con SPM (a los 520 ms) con `0:00.0` `0 m`, `Rep 1/5 · 0 m` y `Sin repeticiones completas`; al segundo 1, `0:01.0` `4 m`; nada aparece de golpe después de la cascada |
| **VID-07** | TV, 2,5 s | `Series 600` a los 252,2 s, con los dos abiertos: `Terminar` → `Terminar`, los dos toques antes del segundo 253 | **La salida con la última foto**: los paneles se van con `0:52.0` `208 m`, `Rep 2/3 · 208 m` y `Rep 1 2:05 680 m`, nunca vacíos ni en cero |
| **VID-08** | TV, 4 s | VID-02 con reducir movimiento, con **parciales cerrado y repeticiones abierto al empezar**, y un toque en `500 m` a los 323,8 s, que lo abre (SD-14) | El panel cambia de tamaño en el acto, con el contenido en un fundido, y `Series anteriores` se corre en el mismo cuadro; a los 325, la vuelta nueva aparece con un fundido, sin deslizarse, con su marca como en VID-02 |
| **VID-09** | TV, 3 s | `Series 600` de los 196,5 a los 199,5 s, con los paneles cerrados: tocar `500 m` a los 197,3 | La cuenta grande a la vista; al tocar, el panel se abre a 8,4 dp de ella y la cuenta grande **sigue en su lugar**, sin moverse ni esconderse (DV-50); en ningún cuadro se enciman; la cuenta de la barra sigue |

---

## 8. Qué se verifica dónde

| Dónde | Qué |
|---|---|
| **[dotnet]** | LT-47 a LT-58. Los parciales y las repeticiones con los casos de la app y los remos de prueba (AC-WKT-07-01 a -11, AC-WKT-08-01 a -08). El oráculo contra el `SeriesTracker` del legacy (AC-WKT-07-12). Los mismos metros que el paso por distancia (AC-WKT-07-09). La pausa, el segundo plano y la suspensión con el ciclo de vida de prueba (AC-WKT-07-07, -10). La regla de qué muestra cada panel en cada estado, también DV-48 y el segundo del cambio (AC-WKT-18-01, -04 a -08). Las preferencias con un archivo temporal: por defecto, guardada, toque antes de leer, archivo que falta, dañado o con otro tipo, guardado que falla y guardado entero (AC-WKT-18-10, -11). La regla del lugar de la cuenta grande y de `PAUSA` con los paneles (AC-WKT-18-15). La regla de la llave (AC-BLE-16-02). El catálogo de 223 claves (AC-APP-10-12). LT-59 a LT-61, en su parte de reglas |
| **[Editor-EM]** | Las mismas pruebas dentro de Unity, con y sin simulador |
| **[Editor-PM]** | LT-59 a LT-61 con los paneles montados. Los paneles en los cinco perfiles: chips, cabecera, contenido, la línea, las filas y los textos vacíos; el toque, el arrastre, el teclado, el orden de Tab y el anillo; las preferencias con el almacén real en una carpeta temporal; el lugar, la columna y el desplazamiento; la zona del bote; nada se encima, con la cuenta grande, `PAUSA`, el sello y la llave; nada se corta con `S-LARGO` y `R-4X`; las animaciones (estados, no tiempos); la pausa, la rotación, el segundo plano, los diálogos, la entrada y la salida; la secuencia del motor con y sin paneles; el presupuesto de elementos y 0 B por cuadro; `Simular desconexión` con `-Simulator`. La deuda de §5.1 (la falla provocada, el orden de la entrada, el título del libre) |
| **[Captura]** | CAP-01 a CAP-26, DEMO-01 a DEMO-12 y VID-01 a VID-09 (§7), la regeneración única de las tomas con rutina de `it4a` e `it4b`, y CAP-13 a CAP-15 de la 4b (AC-WKT-16-07) |
| **[Device-Win]** | Build de simulador: la demo en la ventana real; la preferencia que sobrevive a cerrar y abrir la app (AC-WKT-18-10); el teclado (AC-WKT-18-12); `[PERF]` en la entrada (AC-WKT-13-15) y con los paneles (AC-WKT-18-21); `Simular desconexión`. Si el usuario tiene abierto su `RowMate.exe`, se espera y se reintenta (§12 punto 11); la medida de AC-WKT-13-15 puede quedar para el cierre (riesgo aceptado, §5.1) |
| **[Device-And]** | Solo con el módulo y el teléfono: el desplazamiento con el dedo, el área táctil de los chips y el primer cuadro al volver (AC-WKT-02-08). Si no están, NO VERIFICABLE |
| **[QA]** | Las bases y el archivo de preferencias del usuario, intactos (AC-HIS-20-04); el mismo archivo de preferencias para los dos builds (AC-WKT-18-11); el repositorio legacy sin cambios; sin rutas de la PC en lo versionado (también en `manifest.json`, `INDEX.md` y los registros que se citen) |

---

## 9. Checklist de paridad

| # | Comportamiento del legacy | Legacy | Test legacy | AC |
|---|---|---|---|---|
| 1 | Parciales solo con rutina y solo en pasos de trabajo | `lib/features/workout/series_tracker.dart:73-78`, `:129`, `:178`; `immersive_workout_screen.dart:105-106` | LT-50 | AC-WKT-07-01, AC-WKT-18-01 |
| 2 | Vuelta = 500 m desde el inicio del paso, numeradas por paso, lista vacía en cada paso | `series_tracker.dart:40`, `:133-138`, `:179-181` | LT-47, LT-48 | AC-WKT-07-02 |
| 3 | Menos de 500 m no da vueltas; un trabajo por tiempo también las da | `series_tracker.dart:133` | LT-49 | AC-WKT-07-02 |
| 4 | El cruce se interpola; varias vueltas por lectura | `series_tracker.dart:132-140` | LT-47 | AC-WKT-07-04 |
| 5 | Vuelta en curso: `⌊metros⌋` y segundos; 0 fuera de un trabajo | `series_tracker.dart:72-78` | LT-47, LT-50 | AC-WKT-07-05 |
| 6 | Saneamiento: negativo o más de `10 × máx(Δt; 1)` no suma, el tiempo sí, referencia nueva | `series_tracker.dart:43-46`, `:120-127` | LT-55, LT-56, LT-57 | AC-WKT-07-06 |
| 7 | Al reanudar, referencia nueva | `workout_provider.dart:245-250`; `series_tracker.dart:104-108` | LT-58 | AC-WKT-07-07 |
| 8 | Arranca con la distancia del monitor y el reloj activo | `workout_provider.dart:128`; `series_tracker.dart:86-102` | LT-56 | AC-WKT-07-08 |
| 9 | Una lectura por segundo, antes de evaluar el fin del paso | `workout_provider.dart:176-186` | — | AC-WKT-07-03, AC-WKT-18-08 |
| 10 | Nada en pausa ni en el libre | `workout_provider.dart:151-152`, `:176` | — | AC-WKT-07-03 |
| 11 | Se reinicia al iniciar la rutina y al volver a reposo | `workout_provider.dart:128`, `:315` | — | AC-WKT-07-11 |
| 12 | Nada se guarda | `docs/superpowers/specs/2026-10-02-series-splits-design.md` ("Fuera de alcance") | — | AC-WKT-07-11 |
| 13 | Posición de cada paso; suelto = (sin serie, 1, 1) | `lib/core/models/routine.dart:74-97` | LT-16 (It. 1) | AC-WKT-08-01 |
| 14 | En serie, repetición, cantidad y metros de la repetición con `⌊ ⌋` | `series_tracker.dart:80-83` | LT-50 | AC-WKT-08-01, -02 |
| 15 | Repetición nueva si cambia la serie o el número | `series_tracker.dart:149-151` | LT-51 | AC-WKT-08-03 |
| 16 | Resumen al frente si es la misma serie; hasta 3 | `series_tracker.dart:41`, `:153-166` | LT-51, LT-52 | AC-WKT-08-04 |
| 17 | Otra serie o un paso suelto vacían la lista | `series_tracker.dart:167-170` | LT-53 | AC-WKT-08-05 |
| 18 | Metros de trabajo (los que suman) y segundos de trabajo (todos los Δt) | `series_tracker.dart:129-131` | LT-51, LT-55 | AC-WKT-08-06 |
| 19 | Split = segundos / metros × 500; sin metros, nulo | `series_tracker.dart:31-33` | LT-51, LT-54 | AC-WKT-08-06 |
| 20 | La última repetición nunca entra en la lista | `series_tracker.dart:153-174` | — | AC-WKT-08-07 |
| 21 | Paneles solo con rutina, debajo de SPM, a 10 | `immersive_workout_screen.dart:105-106`, `:630-633` | — | AC-WKT-18-01, -13 (a 8·s: SD-05, DV-50) |
| 22 | Repeticiones solo en serie, 8 debajo | `series_panels.dart:178-193` | LT-61 | AC-WKT-18-01 |
| 23 | Chip: ícono 14 al 80 %, 4, rótulo 13/600 con 0,8 de espaciado en el color al 80 %, 6, flecha 18 al 54 % | `series_panels.dart:34-39`, `:249-259` | LT-59 | AC-WKT-18-02 (el rótulo en el color pleno: SD-17, DV-51; la flecha pegada a la derecha: SD-18, DV-50) |
| 24 | Parciales: `timer_outlined`, `#FF9F1C`, `500 m`, `Parciales 500 m` | `series_panels.dart:165-170` | LT-59 | AC-WKT-18-02, -03 |
| 25 | Repeticiones: `history`, `#4CC9F0`, `Reps`, `Series anteriores` | `series_panels.dart:180-185` | LT-59 | AC-WKT-18-02, -03 |
| 26 | Vidrio negro al 55 %, radio 14, borde al 10 %, relleno 14 × 10 | `series_panels.dart:28-32`, `:228-229` | — | AC-WKT-18-02 (el vidrio de AC-WKT-17-01: DV-31 de la 4a) |
| 27 | Ancho mínimo 130; abierto, fijo en 200 | `series_panels.dart:225`, `:227` | — | AC-WKT-18-02, -03 (137 base fijo, cerrado y abierto: SD-01, SD-02, DV-50) |
| 28 | Abierto: ícono, 4, título en una línea con `…` y el estilo del chip, `expand_less` 18 al 54 %; contenido a 6 | `series_panels.dart:235-248`, `:260-263` | LT-59 | AC-WKT-18-03 (el título en 11/600, a una escala por idioma: SD-15, DV-51) |
| 29 | Todo el panel responde; alterna y guarda en el acto | `series_panels.dart:171-175`, `:186-190`, `:220-222` | LT-59 | AC-WKT-18-09 |
| 30 | Claves `immersive.lapsExpanded` / `repsExpanded`; cerrados por defecto; el toque antes de leer gana | `series_panels.dart:120-156` | LT-59, LT-60 | AC-WKT-18-10 |
| 31 | Fuera de un trabajo: `Sin parciales en este paso`, blanco al 54 %, 15, sin límite de líneas | `series_panels.dart:55`, `:286-287` | — | AC-WKT-18-05 (en dos renglones parejos: SD-13) |
| 32 | Vuelta en curso: `M:SS.d` 30/800 ámbar, una línea, con `…`; a 6, `{n} m` 14/500 al 55 %; por línea de base | `series_panels.dart:41-53`, `:290-304` | — | AC-WKT-18-04 (sin cortar: DV-49; en 28, con los metros a la derecha en 13 al 80 %: SD-03, SD-16, DV-51) |
| 33 | Las últimas 4, la más reciente primero, a 4 (los 4, también sin vueltas) | `series_panels.dart:279-282`, `:305-311` | — | AC-WKT-18-04 (el aire de 4, solo con vueltas: SD-19) |
| 34 | Fila: 2 arriba y abajo (4 entre filas); rótulo en 52, blanco al 60 %, 15/600; valor 22/800 con `…`; a la derecha, 16/800 celeste | `series_panels.dart:351-390` | — | AC-WKT-18-04, -07 (sin cortar: DV-49; 2 entre filas, el rótulo 12/600 en la columna del más ancho, el valor en 20 y los metros en 13: SD-03, SD-04, SD-19, DV-51) |
| 35 | `Rep r/R · N m` a 6, también fuera de un trabajo, 16/800 celeste | `series_panels.dart:313-317` | LT-59 | AC-WKT-18-06 |
| 36 | Sin repeticiones: `Sin repeticiones completas` | `series_panels.dart:330-333` | LT-59 | AC-WKT-18-07 |
| 37 | Filas `Rep n`, split o `—`, metros totales | `series_panels.dart:336-344` | — | AC-WKT-18-07 |
| 38 | Formatos de vuelta, split y metros (miles con punto, siempre) | `series_panels.dart:8-24` | LT-45 (4a) | AC-WKT-11-09 a -11, AC-WKT-18-22 |
| 39 | La columna termina arriba de la banda; lo que no entra se desplaza | `immersive_workout_screen.dart:611-633` | — | AC-WKT-18-13 |
| 40 | Cifras tabulares en los valores | `series_panels.dart:46` | — | AC-WKT-17-12 (4b) |
| 41 | Textos fijos en español | `series_panels.dart:168-169`, `:183-184`, `:287`, `:315`, `:332`, `:338` | LT-59 a LT-61 | AC-APP-10-12, AC-WKT-18-22 (Q-01) |
| 42 | El primer segundo de la rutina: `Sin parciales en este paso` y sin repeticiones | `series_tracker.dart:56`, `:80`; `series_panels.dart:286`; `workout_provider.dart:176` | — | AC-WKT-18-01 (**cambia: DV-48**) |
| 43 | Un segundo de atraso en cada cambio de paso | `workout_provider.dart:176-186`; `series_tracker.dart:118` | — | AC-WKT-18-08 (P-01) |
| 44 | El panel de repeticiones conserva su estado mientras no se ve | `series_panels.dart:128-129` | — | AC-WKT-18-01, -10 |
| 45 | Los valores cambian solo en el segundo, aunque el HUD se redibuja con cada lectura | `workout_provider.dart:151-186`, `:220-224` | — | AC-WKT-18-08 |
| 46 | `Simular desconexión`: 3 s sin lecturas y la física quieta | `lib/core/bluetooth/simulated_ble_service.dart:81-103` | `simulated_ble_service_test` (It. 2) | AC-BLE-15-09, AC-WKT-07-06 |

---

## 10. Mejoras EXR (cómo se tiene que sentir)

Son agregados de presentación. Ninguno cambia un dato, un umbral, un orden ni una regla del legacy, y ninguno adelanta ni frena el reloj (AC-WKT-13-14).

1. **El detalle, a pedido.** Cerrados, los paneles son dos chips discretos que no compiten con SPM. Abrirlos es como desplegar una pieza de vidrio: crece hacia abajo desde el chip, sin cambiar de ancho, con un resorte suave; el contenido aparece cuando ya hay lugar y lo de abajo se corre sin saltar. Cerrarlo es el camino inverso.
2. **Cada 500 m se siente.** Al cerrar una vuelta, la lista baja y su tiempo entra arriba, con una marca ámbar breve a su izquierda, y la vuelta en curso vuelve a cero. El remero lo percibe con el rabillo del ojo, sin leer.
3. **Regatear contra uno mismo.** `Series anteriores` se lee de un vistazo: la repetición, el split y los metros, en columnas, con la más reciente arriba. En una serie, el remero sabe si va mejor o peor que en la repetición anterior.
4. **Lo que importa, primero.** Si falta lugar, arriba quedan la vuelta en curso y la más reciente; lo viejo es lo que se desplaza.
5. **El bote es el protagonista.** Los paneles tienen un ancho acotado (ROADMAP, mejora EXR de la It. 5). En horizontal nunca tapan el bote. En vertical, solo si el remero los abre, y solo por el borde de afuera: nunca el casco ni el remero. La cuenta grande y `PAUSA` nunca tienen que apartarse, porque los paneles no llegan a su lugar.
6. **Nada salta.** Los cambios de paso y de repetición se acomodan con el empuje de la lista, y solo cambia lo que cambia; la cascada de la entrada trae los paneles ya armados, con SPM; la salida se los lleva con su última foto.
7. **Respeto por quien lo pide.** Con reducir movimiento, fundidos sin resortes ni deslizamientos; con teclado, Tab llega a los paneles y Enter los abre.

Se comprueba con AC-WKT-18-18, -19, AC-WKT-13-06, -08, AC-APP-05-08 y los videos VID-01 a VID-09, que revisan QA y el Designer.

---

## 11. Decisiones aplicadas y gate de la It. 5

El gate de la 4b quedó aceptado con los valores por defecto: D-ANI-05 (la cuenta grande y el sello), DV-40 a DV-47, P-01 a P-16 de la 4b y las decisiones del PO al cerrar ([REPORT de la 4b §8](../04b-hud-exr/REPORT.md)).

| Decisión | Qué se aplica | Confirmar en el gate |
|---|---|---|
| Q-01 | **9 claves nuevas** (§11.2), 223 en total; el español idéntico al legacy | No (ya aceptada en la It. 1) |
| Q-06 | Formatos fijos: punto decimal y punto de miles en los paneles, también en inglés (AC-WKT-11-11) | No; ver P-08 |
| Q-55 / DV-26 | Parciales y paso por distancia cuentan los mismos metros saneados (AC-WKT-07-09) | No (ya aceptada en la 4a) |
| Q-11 | No se importan las preferencias del legacy (AC-WKT-18-11) | No (ya aceptada) |
| Q-15 | Los parciales cuentan desde la distancia del monitor al iniciar (AC-WKT-07-08) | No |
| Q-91 / P-17 de la 4a | Segundo plano: los segundos se procesan; el equipo suspendido no cuenta (AC-WKT-07-10) | No (ya aceptadas) |
| D-ANI-05 | La cuenta grande y `PAUSA` conviven con los paneles: siempre en su lugar de la 4b (AC-WKT-18-15) | Sí, con DV-50 (P-04) |
| **DV-48** | Los paneles muestran el primer paso desde el primer cuadro | **Sí** (P-02) |
| **DV-49** | En los paneles nada se corta ni se parte: se achica | **Sí** (P-13) |
| **DV-50** | Paneles de 137 base fijos, a 8·s de SPM; la cuenta grande y `PAUSA` siempre a la vista; abiertos en vertical, solo por el borde de afuera de la zona del bote | **Sí** (P-03, P-04) |
| **DV-51** | Adentro de los paneles, textos secundarios más chicos que el legacy y otra disposición | **Sí** (P-14) |
| SD-01 a SD-19 | Las diferencias entre esta spec y el diseño, decididas por el PO en el gate de armonía (§11.3) | Solo las que van en DV-50 y DV-51 |
| R4a-03 | El presupuesto con los paneles: el tope actual o uno nuevo medido (AC-WKT-18-21) | No (técnica) |
| P-01 a P-14 | Preguntas abiertas con su decisión por defecto (§14) | Las marcadas "Usuario" |

### 11.1 Diferencias visibles con el legacy (a propósito)

Siguen la numeración de la 4b. No incluyen las mejoras EXR de §10 ni el sistema visual (§11.1.1).

| ID | Legacy (verificado) | App | Motivo | AC |
|---|---|---|---|---|
| DV-48 | Hasta el primer segundo de la rutina, el tracker todavía no entró a ningún paso: el panel de parciales dice `Sin parciales en este paso` aunque el primer paso sea de trabajo, y no hay panel de repeticiones aunque la rutina empiece con una serie. Al primer segundo, el chip `Reps` aparece de golpe, en medio de la entrada (`series_tracker.dart:54-56`, `:80`; `series_panels.dart:178`, `:286`) | Desde el primer cuadro, los paneles muestran el primer paso: `0:00.0` `0 m` si es de trabajo; `Rep 1/N · 0 m` y el panel de repeticiones si está en serie. Los números del primer segundo en adelante no cambian (son los mismos: ceros) | Nada aparece de golpe en la cascada (AC-WKT-13-06) y el panel no contradice a la barra. No cambia ningún número | AC-WKT-18-01, AC-WKT-13-06 |
| DV-49 | La vuelta en curso y los valores de las filas se cortan con `…` si no entran en los 200 de ancho (`series_panels.dart:294-299`, `:380-385`); el rótulo `Rep n` va en una caja de 52 sin límite de líneas, y desde `Rep 100` se parte en dos líneas (`:372-379`); la línea `Rep r/R · N m` no tiene límite de líneas y, con números largos, se parte (`:315-316`) | Ningún número ni texto de los paneles se corta ni se parte: lo que no entra se achica (como las cifras del HUD). Solo más allá de `S-LARGO` se recorta con `…` al final | Un número cortado informa mal (como DV-45 de la 4b); el editor deja hasta 999 repeticiones (Q-26, P-01 de la It. 3) | AC-WKT-18-17 |
| DV-50 | El chip mide lo de su contenido, con 130 como mínimo (el ancho de su tarjeta SPM), y abierto pasa a **200 fijos** (`series_panels.dart:225-227`). La columna va a 10 debajo de SPM (`immersive_workout_screen.dart:631`). En un teléfono vertical, el panel abierto llega a 225,6 dp: pasa el borde del disco de la cuenta grande (167,6) y el centro del remero (205,5); en L640 entra 18 dp en la zona del bote | **137 base fijos, cerrado y abierto** (en TV 144,4 dp; en P360 126,5; en W1280, a la escala 1,20, 164,4): abrir cambia solo el alto. El chip lleva la flecha pegada a la derecha, con vidrio vacío en el medio. La columna va a **8·s** de SPM, el aire entre las tarjetas apiladas. Los paneles terminan 8·s antes del disco: la cuenta grande y `PAUSA` se ven **siempre** en su lugar de la 4b. En vertical, abiertos, entran solo en la franja de afuera de la zona del bote (en TV, hasta 159,1 dp: el 24 %); en horizontal, nunca, sin angostarse (SD-01, SD-02, SD-05 a SD-07, SD-18) | La mejora EXR del ROADMAP para esta iteración pide "ancho acotado para no tapar el bote". Con 200, en vertical la cuenta grande y `PAUSA` tendrían que esconderse con un panel abierto, y el panel taparía al remero. Con 8, los dos chips entran sin desplazar en los cinco perfiles; con 10, en TH y L640 con objetivo en SPM, no (76 contra 75,05 base). Con un solo ancho, abrir es solo crecer, sin reacomodar el texto. **Si el usuario lo rechaza:** 130 / 200 y 10 de SPM (son constantes, R5-07); en vertical, con un panel abierto que ocupe su lugar, la cuenta grande y `PAUSA` no se muestran esa vez (la regla de reserva de AC-WKT-18-15), y el ancho que cambia al abrir suma trabajo de motion | AC-WKT-18-02, -03, -13 a -15, AC-WKT-13-12, AC-WKT-14-18, AC-WKT-17-09 |
| DV-51 | Tamaños fijos, que siguen la letra del sistema: la vuelta en curso en 30/800 y sus metros en 14/500 al 55 %, a 6 del tiempo; `#n` y `Rep n` en 15/600 al 60 %, en una columna de 52; los tiempos y splits de las filas en 22/800; los metros de las repeticiones en 16/800; 2 arriba y 2 abajo de cada fila; el rótulo del chip y el título en 13/600 con 0,8 de espaciado, al 80 % del color; los textos vacíos sin límite de líneas (`series_panels.dart:34-55`, `:240-245`, `:286-304`, `:351-390`) | Por la escala de los paneles y sin seguir la letra del sistema: la vuelta en curso en **28** y sus metros en **13 al 80 %, pegados a la derecha**; `#n` y `Rep n` en **12/600** `text.secondary`, en una columna del ancho del rótulo; los tiempos y splits en **20**; los metros de las repeticiones en **13**; 2 entre filas; el rótulo del chip en 13/600 con 0,08 em, en el color pleno; el título en **11/600**, sin espaciado, a una escala por idioma (0,949 en español); los vacíos en dos renglones parejos. `Rep r/R · m` (16) y los vacíos (15) quedan como el legacy (SD-03, SD-04, SD-13, SD-15 a SD-17, SD-19) | Es lo que entra en 137 sin cortar nada (DV-49). Lo que se lee de lejos queda como en el legacy (la tabla de abajo): en TV, la vuelta en curso mide 29,5 dp (legacy 30) y los tiempos de las filas 21,1 (22). Lo que se achica (rótulos, metros, títulos) se lee de cerca, como los rótulos de kcal y BPM, y de lejos cada panel se reconoce por su color y su ícono. Los metros al 55 % no llegan al contraste mínimo (3,003:1 sobre el peor vidrio); al 80 % dan 5,118. Los metros a la derecha no se corren cuando el tiempo cambia de ancho. **Si el usuario lo rechaza:** los tamaños del legacy por la escala (constantes), con las filas de repeticiones de 4 cifras de metros achicadas a ≈ 0,82 | AC-WKT-18-02 a -05, -07 |
| DV-26 (de la 4a, ya aceptada) | El paso por distancia termina con la distancia cruda del monitor (`workout_provider.dart:198-201`), y los parciales cuentan la saneada. Con un monitor que falla, el trabajo de 1000 m termina antes o después de su segunda vuelta: con `R-4Z` y `R-4D`, a los 250 s, con solo `#1` y `492 m` o `484 m` en la vuelta en curso; con `R-4R`, recién a los 510 s, con 4 vueltas | El paso y los parciales cuentan los mismos metros (AC-WKT-07-09): el trabajo de 1000 m termina con `#1` y `#2`, a los 251 s (`R-4R`), 252 (`R-4Z`) y 254 (`R-4D`) | Se decidió en la 4a (Q-55). Se anota acá porque en la 5 el usuario la ve en los paneles. No se vuelve a confirmar | AC-WKT-07-09 |

**Para mostrarle al usuario con DV-50 y DV-51** (dp de pantalla; el legacy no escala; legibilidad a 1,5 m en TV, contra los 5′ de arco de una lectura cómoda, de [04-design-notes §8](04-design-notes.md) y [00-deuda-diseno §3.6](00-deuda-diseno.md)):

| Pieza | Legacy | App en TV | App en W1280 | A 1,5 m (TV) |
|---|---|---|---|---|
| Ancho del panel abierto · cerrado | 200 · ≥ 130 | 144,4 · 144,4 | 164,4 · 164,4 | — |
| Vuelta en curso | 30 | 29,5 | 33,6 | 7,5′: de lejos (1,5×) |
| Tiempos de las vueltas y splits de las repeticiones | 22 | 21,1 | 24,0 | 5,4′: de lejos (1,1×) |
| `Rep r/R · m` | 16 | 16,9 | 19,2 | 4,3′: de cerca |
| Metros de la vuelta en curso · de las repeticiones | 14 · 16 | 13,7 · 13,7 | 15,6 · 15,6 | 3,5′: de cerca |
| Rótulo del chip (`500 m`, `Reps`) | 13 | 13,7 | 15,6 | 3,6′: de cerca; de lejos, el color y el ícono |
| `#n` · `Rep n` | 15 | 12,6 | 14,4 | 3,4′: de cerca |
| Título (`Parciales 500 m`, `Series anteriores`) | 13 | 11,0 en es · 11,6 en en | 12,5 · 13,2 | 2,9′ (es): de cerca |

En P360 todo el HUD va a 0,923 (D-ART-02 de la 4a), los paneles también. Las capturas para mostrarle: CAP-03 (contra `docs/image-03.png` del legacy), CAP-18, CAP-19, CAP-20 y CAP-22.

#### 11.1.1 Del sistema visual (informativas)

No van al gate: cambian el aspecto o un caso borde que no se provoca desde la app (una preferencia de otro tipo), no datos, textos, orden ni significados.

| Legacy (verificado) | App | Dónde |
|---|---|---|
| Negro al 55 %, radio 14, borde al 10 % | El vidrio de las tarjetas del HUD (`glass.fallback`, borde de 1 al 10 %, radio 14, sin reflejo: DV-31 de la 4a). Los tamaños y el ancho son DV-50 y DV-51 | AC-WKT-18-02, -03 |
| Los colores base en los valores, y al 80 % en los rótulos (como sus tarjetas) | Las variantes `.hud` en los valores (DV-31 de la 4a); los rótulos del chip y los títulos en el color pleno, como los rótulos de las tarjetas de la app; `#n` y `Rep n` en `text.secondary` (DV-51) | AC-WKT-18-02 a -04, -06, -07 |
| La flecha es el ícono `expand_more` / `expand_less` de 18 | Un trazo dibujado con la misma forma, que gira 180° al abrir y al cerrar | AC-WKT-18-02, -03, -18 |
| Los paneles no se pueden enfocar con el teclado | Tab, anillo de foco y Enter o Espacio, como todo control de la app (AC-APP-05-07) | AC-WKT-18-12 |
| La preferencia se lee después del primer dibujo: con la clave en verdadero, el panel puede verse cerrado un instante y abrirse de golpe (`series_panels.dart:134-151`) | Desde el primer cuadro en que se ve, en su estado guardado | AC-WKT-18-10 |
| Una clave de otro tipo: `getBool` lanza un error (`shared_preferences` 2.5.4, `lib/src/shared_preferences_legacy.dart:117`) dentro de la lectura de las dos claves (`series_panels.dart:140-151`). Con `immersive.lapsExpanded` de otro tipo, **ninguno** lee su clave: los dos arrancan cerrados aunque `immersive.repsExpanded` esté en verdadero. Con `immersive.repsExpanded` de otro tipo, parciales ya tomó su clave y la muestra en el redibujo siguiente; repeticiones, cerrado. En los dos casos queda un error sin atrapar en el registro | Solo el panel de la clave de otro tipo arranca cerrado; el otro, con su clave, desde el primer cuadro. Sin errores (QA-V5-P02) | AC-WKT-18-11 |
| Abrir y cerrar es instantáneo; una vuelta nueva aparece sin transición | El resorte al abrir y cerrar, los fundidos, el empuje de la lista, la entrada de la vuelta con su marca y el cambio de contenido por pieza (§10; SD-08, SD-09, SD-12) | AC-WKT-18-18, -19 |
| En L640, el panel abierto (200 de ancho) entra en la zona del bote | Con 137 no llega: termina 39,8 dp antes (DV-50) | AC-WKT-18-14 |

### 11.2 Claves nuevas del catálogo (Q-01)

Español idéntico al legacy, y su inglés. Con estas, el catálogo pasa de 214 a **223** claves por idioma.

| Clave | es | en | Parámetros | Dónde |
|---|---|---|---|---|
| `hudLapsChip` | `500 m` | `500 m` | — | chip de parciales (AC-WKT-18-02) |
| `hudLapsTitle` | `Parciales 500 m` | `500 m splits` | — | cabecera de parciales (AC-WKT-18-03) |
| `hudLapsEmpty` | `Sin parciales en este paso` | `No splits in this step` | — | parciales fuera de un trabajo (AC-WKT-18-05) |
| `hudLapNumber` | `#{number}` | `#{number}` | `number` (texto: `3`) | rótulo de cada vuelta (AC-WKT-18-04) |
| `hudRepProgress` | `Rep {rep}/{count} · {meters}` | `Rep {rep}/{count} · {meters}` | `rep`, `count` (texto: `2`, `3`); `meters` (texto con el formato de AC-WKT-11-11: `1.112 m`) | línea de la repetición (AC-WKT-18-06) |
| `hudRepsChip` | `Reps` | `Reps` | — | chip de repeticiones (AC-WKT-18-02) |
| `hudRepsTitle` | `Series anteriores` | `Previous reps` | — | cabecera de repeticiones (AC-WKT-18-03) |
| `hudRepsEmpty` | `Sin repeticiones completas` | `No completed reps` | — | repeticiones sin datos (AC-WKT-18-07) |
| `hudRepLabel` | `Rep {rep}` | `Rep {rep}` | `rep` (texto: `2`) | rótulo de cada repetición (AC-WKT-18-07) |

Se reusan, sin cambios: `hudUnitMeters` (`m`, los metros de la vuelta en curso). Los formatos de AC-WKT-11-09 a -11 (`M:SS.d`, `M:SS`, `—` y `1.245 m`) no son claves.

### 11.3 Decisiones del gate de armonía: SD-01 a SD-19

La spec y el diseño no coincidían en estos puntos: SD-01 a SD-11 de [02-tech-design §8.2](02-tech-design.md), SD-12 a SD-14 de [04-design-notes §11.1](04-design-notes.md), y cinco que el gate encontró sin lista (QA-G5P-02, QA-G5T-02). SD-15 a SD-17 llevan los números que propuso QA; SD-18 y SD-19 los numera el PO. El Designer los suma a su tabla con estos mismos números.

**El PO adopta la propuesta del Designer en todos.** Es lo que cumple las garantías de AC-WKT-18-13 a -16 en los cinco perfiles con el peor contenido, sin cortar nada (DV-49), sin tapar el bote ni la cuenta grande, y es la mejora EXR que pide el ROADMAP. Ningún dato, regla, texto u orden del legacy cambia.

- Lo que se ve distinto del legacy va al usuario con **DV-50** y **DV-51** (§11.1).
- Lo demás distingue la spec del diseño, no del legacy: el legacy no anima nada, y la regla de toma, el video y el presupuesto son de cómo se prueba y se construye.
- Si el usuario rechaza DV-50 o DV-51, cambian constantes (R5-07), así que no frena WP-B1. El usuario las confirma con las demás DV en el REPORT de la iteración, como en la 4b. Si el coordinador prefiere mostrárselas antes de WP-B1, la tabla de §11.1 sirve tal cual.

| SD | Tema | La spec decía | Queda | Dónde | Va en |
|---|---|---|---|---|---|
| SD-01 | El chip | relleno 14 × 10, mínimo 140 | 137 × 34, relleno 10 × 8 | AC-WKT-18-02 | DV-50 |
| SD-02 | El ancho abierto | 200 fijo | 137 fijo, igual que cerrado | AC-WKT-18-03 | DV-50 |
| SD-03 | La vuelta en curso y las filas de vueltas | `num.lap` 34; metros al 55 %; `#n` 15/600 al 60 % en 52; `num.row` 24 | `num.lap` 28; metros 13 al 80 %; `#n` 12/600 `text.secondary` en la columna de `#99`; `num.row` 20 | AC-WKT-18-04 | DV-51 |
| SD-04 | Las filas de repeticiones | metros 16/700; columna de 52 | metros 13/700; columna del `Rep n` más ancho; una escala común | AC-WKT-18-07 | DV-51 |
| SD-05 | El lugar | 10·s de SPM; en L640, el segundo chip se desplaza | 8·s; los dos chips entran en los cinco perfiles; en W1280, `sp` 1,20 y las filas desplazables | AC-WKT-18-13 | DV-50 |
| SD-06 | La zona del bote | en vertical, abiertos hasta 225,6 dp en TV; en L640, angostar | en vertical, abiertos solo en la franja de afuera (hasta 159,1 en TV); en horizontal nunca, sin angostar | AC-WKT-18-14, AC-WKT-17-09 | DV-50 (P-03) |
| SD-07 | La cuenta grande y `PAUSA` | en vertical, con un panel abierto, no se muestran | siempre en su lugar | AC-WKT-18-15, AC-WKT-13-12, AC-WKT-14-18, demo (b), CAP-20, CAP-22, VID-09 | DV-50 (P-04) |
| SD-08 | Abrir y cerrar | alto y ancho; el contenido en 160, demorado 80 | solo el alto; el contenido entra en 80-240 y se va en 0-120 | AC-WKT-18-18, VID-01 | informativa |
| SD-09 | La vuelta nueva | crece de 0 en 200 desde −8 dp, con fondo ámbar al 20 % que se apaga en 1,2 s | la lista baja en 0-160; la fila entra en 80-240 desde −8·sp; una marca que se apaga en 600-1400 (DT5-03) | AC-WKT-18-19, AC-APP-05-08, VID-02 | informativa |
| SD-10 | El presupuesto | el tope de 60 o uno nuevo | un elemento por panel y dos juntas en el HUD | AC-WKT-18-21 | técnica |
| SD-11 | La regla de toma | el toque, 0,6 s antes | 0,82 s, o el resorte quieto | §7.1 | de prueba |
| SD-12 | El alto por un dato y el cambio de contenido | el alto con el resorte; todo funde | el alto con el empuje; cambia solo lo que cambia, por pieza | AC-WKT-18-19, VID-02 a VID-04 | informativa |
| SD-13 | Los textos vacíos | sin decir dónde se cortan | dos renglones parejos | AC-WKT-18-05, -07, -17, CAP-05, CAP-13 | DV-51 |
| SD-14 | VID-08 | con los dos abiertos (el toque cerraba parciales) | parciales cerrado al empezar | §7.5 | de prueba |
| SD-15 | El título | el estilo del chip (13/600, 0,8 de espaciado, al 80 %) | 11/600, sin espaciado, en el color pleno, a una escala por idioma | AC-WKT-18-03 | DV-51 |
| SD-16 | Los metros de la vuelta en curso | "al lado" del tiempo (el legacy, a 6) | pegados al borde derecho, con al menos 6 de aire | AC-WKT-18-04 | DV-51 |
| SD-17 | El rótulo del chip | al 80 % del color (el legacy) | en el color pleno, como los rótulos de las tarjetas de la app | AC-WKT-18-02 | DV-51 |
| SD-18 | La flecha del chip | a 6 del texto | pegada al borde derecho, con vidrio vacío en el medio | AC-WKT-18-02 | DV-50 |
| SD-19 | El aire entre filas | "2 entre filas" (el legacy: 2 arriba y 2 abajo, 4 entre filas) | 2 entre filas; el aire de 4 debajo de la vuelta en curso, solo con vueltas | AC-WKT-18-04, -07 | DV-51 |

Además, de 02-tech-design §8.1: **DT5-03** (la marca) se acepta, con SD-09; **DT5-04** (CAP-11 a los 881,2 s y DEMO-11 a los 31,6) y **DT5-08** (no queda nada de `it4a` con rutina para regenerar) están en §7; **DT5-10** (la subida de la cascada en W1280, 9,6 dp) está en AC-WKT-13-06.

Los anchos y las escalas que da esta spec (0,949, ≈ 0,97 y los de `S-LARGO`) los midió el Designer con las tablas de las fuentes; los vuelve a medir Unity (R5-02), y las pruebas usan lo medido.

---

## 12. Restricciones de producto para el Architect

1. **Primero la deuda de la 4b** (§5.1): QA-V4b-06 (armar el HUD fuera de los cuadros en que algo se mueve, medido con `[PERF]` en el build), O-4 y O-5 (una falla de la presentación no traba la sesión), O-1, QA-V4b-R2-03 (en las herramientas) y, con el Designer, QA-V4b-R2-01. Si la demo (c) del usuario falló, también AC-WKT-02-11. QA lo verifica antes de que se sumen los paneles, salvo la medida de AC-WKT-13-15 en el build, que puede quedar para el cierre si el `RowMate.exe` del usuario sigue abierto (riesgo aceptado, §5.1).
2. **Un solo saneamiento.** Los parciales y el paso por distancia cuentan los mismos metros, con la misma regla y la misma fuente, no con dos copias que puedan divergir (AC-WKT-07-09; DV-26). El tracker del legacy solo depende de los modelos, así que el oráculo lo puede correr como a los demás vectores (AC-WKT-07-12).
3. **El motor, aparte; la presentación, aparte.** Parciales y repeticiones son lógica pura que alimenta solo el segundo del motor. Los paneles solo muestran. Abrir, cerrar, animar o desplazar nunca toca el motor, la sesión ni la telemetría, y ninguna animación demora ni adelanta un valor (AC-WKT-13-14, AC-WKT-18-08).
4. **Las preferencias llegan en esta iteración.** Es la primera que usa el archivo de preferencias de ARCHITECTURE §9.4: las mismas claves del legacy, guardado en el acto y entero, compartido por los dos builds, a prueba de un archivo que falta o está dañado, y reemplazable en las pruebas por uno temporal. **Nunca** el del usuario en una prueba, una captura o un video (AC-WKT-18-11, AC-HIS-20-04). No se importa nada del legacy (Q-11).
5. **El presupuesto del HUD (R4a-03).** El peor caso de la 4b ya está en 58 de 60. Los paneles, armados con un elemento por texto, sumarían unos 28. Hay que juntar elementos sin cambiar lo que se ve (como la fila de píldoras de la 4b, que es un elemento), o subir el tope con una medición en el equipo, escrita en ARCHITECTURE §20.2. Sin reservar memoria por cuadro, medido con el registro de asignaciones de Unity (lección de la 4b) (AC-WKT-18-21).
6. **El lugar de la cuenta grande y de `PAUSA`** tiene en cuenta los paneles abiertos (AC-WKT-18-15). Con el ancho de DV-50 nunca se cruzan y se ven siempre. La regla de reserva (esconderlas si un panel ocupa su lugar) es de geometría, no de animación, queda probada para el caso de 200 y, si se usara, decide en cada cuadro.
7. **Determinismo y aislamiento**, como en la 4b, con lo nuevo de §0.4: `F-5`, `R-3`, `R-4A`, `R-4D`, `R-4S`, `S-LARGO` y las preferencias de prueba. Ningún doble llega a un build instalable.
8. **Identificadores estables para las pruebas**, por ejemplo `hud-laps-panel`, `hud-laps-current`, `hud-laps-row-<n>`, `hud-rep-progress`, `hud-reps-panel` y `hud-reps-row-<n>`. Si las filas se dibujan juntas, las pruebas las leen por índice en la vista, como las píldoras (DT4b-01). Los nombres finales los fija el Architect.
9. **Textos**: las 9 claves de §11.2. Ningún texto visible nuevo queda fuera del catálogo, salvo los formatos.
10. **Capturas**: la regeneración única de las tomas con rutina de `it4a` e `it4b` (§7.1), con dos corridas iguales.
11. **Builds de desarrollo**: solo en la carpeta de salida por defecto (`Builds/Windows/simulator`). Si el usuario tiene abierto su `RowMate.exe`, no se lo cierra: se reintenta cada 2 minutos hasta 20 minutos y, si sigue abierto, esa parte queda NO VERIFICABLE.
12. **Unity**: una sola instancia por proyecto, y nunca se cierra la del usuario. Si Unity revienta al cerrar un `-batchmode` que cambió los *defines* (0xC0000005), se repite la corrida (QA-V4b-R2-03).
13. **Repositorio público**: sin rutas de la PC en ningún documento, `manifest.json`, `INDEX.md` ni registro citado. La ubicación del archivo de preferencias se describe sin rutas absolutas.
14. **Strava**: nada en esta iteración.

---

## 13. Fuera de alcance

- **It. 6a / 7**: la escena y el bote; con el bote a la vista, se revisan los paneles abiertos en vertical (P-03). La boya o marca de 500 m en el agua.
- **It. 6b**: el vidrio con desenfoque de los paneles.
- **It. 9 y backlog**: parciales, repeticiones y cumplimiento de objetivos en el historial (BKL-16). Comparar contra sesiones anteriores de la misma rutina (fuera de alcance también en el legacy).
- **It. 10a**: el remo real que no manda la distancia una vez por segundo (QA-V4a-L01) y el aviso "Reconectando…" (Q-34).
- **It. 14**: lectores de pantalla (DT-05) y la háptica al cerrar una vuelta.
- **Backlog** (fuera de la paridad):
  - un aviso breve al cerrar cada vuelta, con la diferencia contra la anterior, y las filas coloreadas según la diferencia (inventario F-WKT-07 y F-WKT-18, "Mejora EXR"; P-09; se propone como BKL-26);
  - la referencia de ritmo de la repetición anterior en el agua, o el bote fantasma (BKL-01);
  - el sonido al cerrar una vuelta (BKL-03).

---

## 14. Preguntas abiertas

| ID | Pregunta | Por defecto | Para |
|---|---|---|---|
| P-01 | ¿Los paneles pasan al paso nuevo en el mismo cuadro en que cambia la barra? | **No** (paridad): durante el segundo del cambio siguen mostrando el paso que terminó, y así se ve un segundo la última vuelta que cerró en ese segundo, que si no nunca se vería. Los números serían los mismos en los dos casos (AC-WKT-18-08) | — (PO) |
| P-02 | ¿Al iniciar, los paneles ya muestran el primer paso? | **Sí** (DV-48) | Usuario (gate, con las DV) |
| P-03 | En vertical, ¿los paneles abiertos pueden entrar en la zona del bote? | **Sí, solo los abiertos, y solo en la franja de afuera de la zona**, nunca en su columna del centro (el disco, el casco y el remero): en TV, de 115,1 a 159,1 dp, el 24 % (DV-50). El chip `Reps` cerrado que baja con parciales abierto sigue la misma regla (AC-WKT-18-14). El legacy, con 200, entra hasta 225,6 dp, el 61 %. Los abre el usuario. `Series anteriores` abierto tapa la pala izquierda mientras está abierto; se revisa en la 7 con el bote a la vista, y la alternativa medida es ponerlo debajo de Watts, solo en vertical ([00-deuda-diseno §2.2](00-deuda-diseno.md)). Angostarlos hasta la zona los dejaría en ≈ 90 dp en TV: no entra ni la vuelta en curso | Usuario (gate, con DV-50) |
| P-04 | En vertical, con un panel abierto, ¿qué pasa con la cuenta grande y `PAUSA`? | **Se ven siempre en su lugar de la 4b**: con 137, los paneles terminan 8·s antes del disco (DV-50). No hace falta un segundo lugar. Si el usuario elige los 200 del legacy, esa vez no se muestran (la regla de reserva de AC-WKT-18-15, la que dejó propuesta la 4b en [00-deuda-diseno §4.8](../04b-hud-exr/00-deuda-diseno.md)) | Usuario (gate, con DV-50) |
| P-05 | Con la escala del HUD, debajo de SPM entra menos que en el legacy (TH ≈ 82 dp, L640 ≈ 73, W1280 ≈ 218: §15). ¿Cómo entran los paneles? | **Resuelta por el Designer** ([00-deuda-diseno §2.3 y §2.4](00-deuda-diseno.md), [04-design-notes §5](04-design-notes.md)): en la zona del legacy, con las reglas de AC-WKT-18-13: en TV y P360 entra todo; en W1280, los paneles a 1,20 y las filas desplazables adentro; en TH y L640, la columna desplazable, como el legacy. La zona no cambia | — (resuelta) |
| P-06 | ¿En qué orden recorre Tab los paneles? | `Terminar` → `Pausa` / `Reanudar` → parciales → repeticiones → la llave. El primer Tab sigue en `Terminar`, como en la 4b | — (PO) |
| P-07 | ¿Qué textos en inglés llevan los paneles? | Los de §11.2: `500 m splits`, `Previous reps`, `No splits in this step`, `No completed reps`; `500 m`, `Reps`, `Rep` y `#` como en español | — (PO; se ven en CAP-12 y CAP-13) |
| P-08 | En inglés, `1.112 m` con punto de miles se puede leer como 1,1 m. ¿Se cambia? | **No**: Q-06 fija los formatos del legacy en los dos idiomas, y AC-WKT-11-11 ya está cerrado. La alternativa es `1,112 m` en inglés | Usuario (gate, opcional) |
| P-09 | ¿Se agregan el aviso de cada vuelta con la diferencia contra la anterior y las filas coloreadas por la diferencia (mejoras EXR del inventario)? | **No** en la It. 5: cambian lo que se ve, suman elementos a un HUD que está en su tope y no son del legacy. Se proponen como BKL-26 | — (PO) |
| P-10 | ¿Se guardan las vueltas y las repeticiones en la base o en el historial? | **No** (paridad; BKL-16) | — (PO) |
| P-11 | ¿La preferencia de los paneles es la misma en el build normal y en el de simulador? | **Sí**: un solo archivo de preferencias, como el legacy (ARCHITECTURE §9.4) | — (PO) |
| P-12 | ¿Qué pasa si no se puede leer o guardar la preferencia? | Los paneles arrancan cerrados o cambian igual, sin aviso (AC-WKT-18-11). En la It. 11 (el pulsómetro recordado) se revisa si un archivo dañado debe avisar, porque ahí se perdería algo más que dos interruptores | — (PO) |
| P-13 | ¿`Rep 100` y los valores largos se achican en lugar de cortarse o partirse? | **Sí** (DV-49) | Usuario (gate, con las DV) |
| P-14 | ¿Adentro de los paneles, los rótulos, los metros y los títulos van más chicos que en el legacy, con los metros a la derecha y los vacíos en dos renglones? | **Sí** (DV-51): es lo que entra en 137 sin cortar nada, y lo que se lee de lejos (la vuelta en curso, los tiempos de las filas) queda como en el legacy. Si no, los tamaños del legacy por la escala, con las filas de repeticiones más achicadas (≈ 0,82) | Usuario (gate, con DV-51) |

---

## 15. Discrepancias encontradas al especificar

| Dónde | Qué dice | Qué es (verificado) | Acción |
|---|---|---|---|
| ROADMAP §2, It. 5, demo | "Una rutina con serie ×3 de trabajo 1 000 m: … `0:55.0 207 m` … `Rep 1 2:13 452 m`" | Esos valores son los de `docs/image-03.png` del legacy: una serie de trabajos de 2 min a 150 W (55 s a 3,77 m/s dan 207 m; 120 s, 452 m y `2:13`), sin vueltas completas. Con 1000 m de trabajo, una repetición no puede sumar 452 m | La demo usa `Series 600` (§1), que muestra vueltas y repeticiones en 4 minutos. **Pendiente** (PO, al cerrar): ajustar la demo del ROADMAP |
| workout-engine-hud F-WKT-07, casos borde | "Durante el primer segundo de un paso nuevo, hasta la primera lectura, el 'es paso de trabajo' todavía refleja el paso anterior" | Es así, y además: al iniciar la rutina, durante el primer segundo, el panel dice `Sin parciales en este paso` aunque el primer paso sea de trabajo, y no hay panel de repeticiones aunque empiece con una serie | DV-48. **Pendiente** (PO, al cerrar): precisar el inventario |
| workout-engine-hud F-WKT-18, puntos 6 y 7 | Las filas y la vuelta en curso "en una sola línea" | La vuelta en curso y los valores de las filas se cortan con `…`; `Rep n` va en una caja de 52 sin límite de líneas (desde `Rep 100` se parte en dos), y la línea de la repetición tampoco tiene límite (`series_panels.dart:294-299`, `:315-316`, `:372-385`) | DV-49. **Pendiente** (PO): precisar el inventario |
| workout-engine-hud F-WKT-18, punto 8 | "Si el usuario toca un panel antes de que termine de leerse la preferencia, gana lo que eligió" | Es así; además, con la clave en verdadero el panel puede verse cerrado hasta que se lee (se lee después del primer dibujo, `:134-151`) | §11.1.1. **Pendiente** (PO): sumarlo |
| workout-engine-hud, mapa y "Tests legacy" de F-WKT-07 y -08 | `series_tracker_test` (8) y (6) | El archivo tiene 12 casos; 8 + 6 cuenta dos veces los que tocan las dos fichas (§6: LT-47 a LT-58) | **Pendiente** (PO): aclararlo en el inventario |
| workout-engine-hud F-WKT-17, punto 3 | El límite de la columna: "margen inferior + 200" en vertical y "+ 16 + 66 + 8" en horizontal | Son los números del legacy para "8 arriba de la banda". En la app la banda está escalada y en horizontal es compacta (DV-33), así que la regla es "8·s arriba de la banda" | AC-WKT-18-13. Informativa |
| 04b/00-deuda-diseno §4.8 | "Con los paneles de series colapsados (140 base desde 14·s), quedan 9,5 dp de aire en TV": se calculó con el disco de 64·s | El disco quedó de 72·s (ART_BIBLE §5.2: 167,6-243,4 en TV). Con los chips de 140 base (hasta 162,3), el aire es ≈ 5 dp, menos que los 8·s de la regla del lugar. No se enciman | **Resuelto** (Designer): con 137 base, el aire es 8,4 dp, exactamente 8·s ([00-deuda-diseno §8.3](00-deuda-diseno.md); CAP-19, DV-50) |
| Espacio debajo de SPM (medido sobre las capturas de la 4b, aproximado) | El legacy da ≈ 526 dp en TV, ≈ 133 en TH, ≈ 82 en L640 y ≈ 442 en W1280 | Con la escala del HUD (D-ART-02), la app da ≈ 453, ≈ 82, ≈ 73 y ≈ 218. En W1280, los dos paneles abiertos al tamaño de la escala (1,6) no entran ni con una vuelta | **Resuelto** (Designer, [00-deuda-diseno §2.3 y §2.4](00-deuda-diseno.md)): las reglas de AC-WKT-18-13 (en W1280, `sp` 1,20); en TH y L640 entran 77,1 base con objetivo en SPM y 93,9 sin ([04-design-notes §5.1](04-design-notes.md)) |
| L640 y la zona del bote | — | El panel abierto (200 base = 184 dp desde 12,9) llega a 196,9, y la zona del bote empieza en 179,2: se encima 18 dp | Con 137 base (DV-50) no se encima: termina en 139,4, 39,8 dp antes. No hace falta angostarlo (AC-WKT-18-14) |
| ART_BIBLE §3.4 | "Expandido fijo 200 (en tablet 240)" | No dice qué perfil es "tablet"; W1280 es una ventana de escritorio | **Resuelto** (Designer): no hay variante de tablet, es un solo ancho, 137 × `sp` ([00-deuda-diseno §8.3](00-deuda-diseno.md)) |
| ART_BIBLE §3.2, §3.4, §4.3 y §5.2; ANIMATION_BIBLE §2.3, §2.10 y §2.11 | 140 ↔ 200, `num.lap` 34, `num.row` 24, el reflejo del vidrio, el fondo ámbar al 20 % de la vuelta nueva | Lo que decidió el gate (§11.3: SD-01 a SD-19, DV-50, DV-51) | **Pendiente** (Designer, WP-D2): alinear las biblias después del gate ([04-design-notes §12](04-design-notes.md)) |
| FEATURE_INVENTORY §6.2 | — | Falta el aviso de vuelta con la diferencia y las filas coloreadas (P-09) | **Pendiente** (PO, al cerrar): sumar BKL-26 |

---

## Cambios por gate QA

Correcciones por [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md). Se verificaron contra el legacy (`3ae0f3a`, solo lectura): `series_panels.dart` entero, `immersive_workout_screen.dart:96-112`, `:600-640` y `:788-805` (el rótulo de las tarjetas, al 80 %), `workout_provider.dart:190-205`, y `shared_preferences` 2.5.4 (la versión de `pubspec.lock`), `shared_preferences_legacy.dart:177-187`, del caché de paquetes. No se leyó `lib/core/strava/strava_secrets.dart`, y `git -C ../rowmate-legacy status --porcelain` quedó vacío. No se disputa ningún issue. El conteo de criterios no cambia (74).

| Issue | Qué cambió | Dónde |
|---|---|---|
| **QA-G5P-01** (mayor) | SD-01 a SD-14 decididos: se adopta la propuesta del Designer. Lo que queda por debajo del legacy o se ve distinto va al usuario como **DV-50** (el ancho, el lugar, la zona del bote y la cuenta grande) y **DV-51** (los tamaños y la disposición de adentro), con la tabla de tamaños en dp y de legibilidad a 1,5 m. AC-WKT-18-02, -03, -04, -07 y -13 reescritos con los números nuevos. §11.1.1 ya no promete los tamaños de ART_BIBLE: describe solo lo informativo | §11.1, §11.1.1, §11.3, §4.3, §14 (P-03, P-04, P-05, P-14) |
| **QA-G5T-02** (mayor) | Lo mismo, y además: los cinco puntos sin lista entran como **SD-15** (el título en 11/600), **SD-16** (los metros de la vuelta en curso a la derecha), **SD-17** (el rótulo del chip en el color pleno), **SD-18** (la flecha pegada a la derecha) y **SD-19** (2 entre filas). Reescritos AC-WKT-18-14, -15, -17, -18 y -19, AC-WKT-13-12, AC-WKT-14-18, AC-WKT-17-09, AC-APP-05-08, la demo (b) (la cuenta grande aparece siempre), CAP-20, CAP-22, VID-08, VID-09 y la regla de toma de §7.1 (0,82 s). También CAP-01, -03, -05, -13 a -19, VID-01 a -03, el checklist de paridad (#21, #23, #27, #28, #31 a #34), §10 (puntos 1, 2, 5 y 6), §12 punto 6 y §15 | §1, §4.3, §5.2, §7, §9, §10, §11.3, §12, §15 |
| QA-G5P-06 | AC-WKT-07-07: lo remado en pausa que no suma son **124 m**, de `L(20)` (1080 m) a `L(51)` (1204 m), por la fracción que pierde la pausa (Q-58) y el medio segundo de las lecturas. Los otros números del AC no cambian | §4.1 |
| QA-G5P-07, QA-G5T-08 | AC-WKT-18-11: si guardar falla, las sesiones siguientes **de la misma ejecución** abren como lo dejó el usuario, y después de cerrar y abrir la app, como quedó guardado (el caché de `shared_preferences` cambia antes de escribir; verificado en 2.5.4). Además, con una clave de otro tipo se conservan las demás claves del archivo (el lado de producto de QA-G5T-07) | §4.3 |
| QA-G5P-08 | El efecto visible de DV-26 en los paneles, con un monitor que falla, como fila de §11.1 (de la 4a, no se vuelve a confirmar): el legacy termina el trabajo de 1000 m a los 250 s con solo `#1` y `492 m` / `484 m` (`R-4Z`, `R-4D`) o a los 510 s con 4 vueltas (`R-4R`); la app, con `#1` y `#2`, a los 251, 252 y 254 s | §11.1 |
| QA-G5P-09 | El aire entre filas: el legacy deja 2 arriba y 2 abajo (4 entre filas), y 4 debajo de la vuelta en curso aun sin vueltas. Queda 2 entre filas, y el 4 solo con vueltas, como parte de DV-51 (SD-19) | §4.3 (AC-WKT-18-04, -07), §9 #33 y #34 |
| QA-G5P-10 | CAP-11 a los **881,2 s** (`0:03.0` `12 m`, `Rep 2/4 · 12 m`), DEMO-11 a los **31,6 s** (DT5-04), VID-08 con parciales cerrado al empezar (SD-14) y la demo (b) con la cuenta grande siempre a la vista (SD-07). §7.1 suma la regla de la marca (1400 ms) y DT5-08 (de `it4a` no queda nada con rutina para regenerar) | §1, §7.1, §7.3 a §7.5 |
| QA-G5T-14 | Se acepta como riesgo: WP-B1 sigue aunque la medida de AC-WKT-13-15 en el build quede pendiente; se mide al cierre, con las tres entradas y los paneles ya construidos. Antes del cierre, el coordinador le pide al usuario que cierre su `RowMate.exe`; si sigue abierto, esa parte queda NO VERIFICABLE | §5.1, §8, §12 punto 1 |
| QA-G5T-06 (del Architect; lo que toca al PO) | §0.4 suma el remo **`R-4S`** (el monitor sigue contando con el equipo dormido), y AC-WKT-07-10 da los números de la suspensión con él: a los 251, `0:51.0` `200 m` (el salto de 2404 m no suma); a los 252, `0:52.0` `204 m`. Cómo se arma es del Architect | §0.4, §4.1 |

**Otros ajustes, para alinear con 02-tech-design y 04-design-notes:** la escala de los paneles (`sp`, 1,20 en W1280) en §0.3 y §7.2; DT5-03 (la marca), DT5-04, DT5-08 y DT5-10 (la subida de la cascada en W1280, en AC-WKT-13-06) aceptados en §11.3; el área de toque de los chips (48 a 56 dp) en AC-WKT-18-09; la columna que sigue a SPM en AC-WKT-18-13; P-05 resuelta; §15 con lo que resolvió el Designer y, como pendiente suyo, alinear las biblias en WP-D2.

**Para los otros roles** (no los hace el PO):

- **Designer**: sumar SD-15 a SD-19 a [04-design-notes §11.1](04-design-notes.md) con estos números (QA-G5P-02), y en WP-D2 las biblias con lo decidido.
- **Architect**: integrar las notas del Designer (QA-G5T-01) y estas decisiones en 02-tech-design (la tabla de §8.2 pasa de "para el gate" a "decidido"; las comprobaciones de CAP-01, -05, -11, -13, -16, -17, DEMO-11 y VID-08 con los textos de §7); el remo `R-4S` (QA-G5T-06).
- **Coordinador**: mostrarle al usuario DV-50 y DV-51 (con la tabla de §11.1 y las capturas que nombra) junto con las demás DV en el REPORT, o antes de WP-B1 si lo prefiere; y pedirle que cierre su `RowMate.exe` antes del cierre (QA-G5T-14).

---

## Cambios por verificación (ronda 1)

Correcciones por [05-qa-verification](05-qa-verification.md) (QA-V5-01, QA-V5-05) y [05-qa-lens-paridad](05-qa-lens-paridad.md) (QA-V5-P02), los tres menores y del PO. Se verificaron contra el legacy (`3ae0f3a`, solo lectura: `series_panels.dart:115-160`) y contra `shared_preferences` 2.5.4 (la de `pubspec.lock`; `lib/src/shared_preferences_legacy.dart:117`, del caché de paquetes); contra la app, con las tomas versionadas (CAP-10 de la 4b; el `manifest.json` de CAP-02 y CAP-20 de la 5) y lo que la verificación cita. No se leyó `lib/core/strava/strava_secrets.dart`, y `git -C ../rowmate-legacy status --porcelain` quedó vacío. **Ninguno pide cambiar código, tomas ni pruebas**: los tres precisan el texto para que diga lo que la app ya hace y QA verificó. No se disputa ningún issue. El conteo de criterios no cambia (74), y no hay DV nuevas.

| Issue | Decisión | Qué cambió | Dónde |
|---|---|---|---|
| **QA-V5-01** (menor; AC-BLE-16-02 en FAIL) | **(a) La excepción, en el AC.** En L640, con el diálogo de completado abierto, no queda ningún lugar libre en la ventana (02-tech-design B5-10): cualquier otro lugar taparía otra pieza del HUD (la barra con las píldoras y el reloj, SPM, la banda, Split, Watts o kcal), también debajo del velo (y la fila de píldoras y el reloj ya los protege el texto de la 4b). Tapar parte de la columna de series es lo de menos: está inerte (AC-WKT-18-20) y el inmersivo sale al cerrar el diálogo, así que el usuario ya no la necesita en esa sesión. La llave sigue arriba del velo, se puede usar y no tapa el diálogo ni `Terminar`. No es una diferencia con el legacy (allí la llave va a 16 de la derecha y 96 de abajo y tapa lo que haya): precisa la regla de la migración (D-ART-10). Se descarta (b), mover la llave a otro lugar en ese caso: cambia una pieza tapada por otra y suma un movimiento | AC-BLE-16-02: "tampoco tapa los paneles" con **una sola excepción**, con el diálogo de completado y sin lugar libre (de los cinco perfiles, solo L640), en el lugar de la 4b, y el orden en que la llave busca lugar (TV y P360 debajo del diálogo; TH a la derecha de la columna). AC-WKT-18-16 remite a esa excepción en "la llave" | §5.2, §4.3 |
| **QA-V5-05** (menor) | La redacción: con parciales abierto, el chip `Reps` cerrado baja con él y entra en la franja de afuera de la zona del bote (02-tech-design B5-06). Es el lugar que pide la spec (8·sp debajo, AC-WKT-18-13) y lo que muestran CAP-02 y CAP-20 | AC-WKT-18-14: "con los dos paneles cerrados los chips no entran", y una viñeta nueva: el chip que baja con un panel abierto **cuenta como la columna abierta** (solo la franja de afuera, nunca la columna del centro), con sus números en TV (339,8-375,7 dp en CAP-02, 331,4-367,2 en CAP-20, la zona desde 323,8, el borde derecho en 159,1). AC-WKT-17-09 y P-03, igual. DV-50 no cambia: la franja es la misma | §4.3, §5.2, §14 |
| **QA-V5-P02** (menor) | La app hace lo más sensato y se queda así: con una clave de otro tipo, solo ese panel arranca cerrado y el otro lee la suya. El legacy, con `immersive.lapsExpanded` de otro tipo, no lee ninguna (los dos cerrados, con un error sin atrapar en el registro); con `immersive.repsExpanded` de otro tipo, coincide con la app salvo que parciales se ve recién en el redibujo siguiente. No se provoca desde la app (hay que editar el archivo a mano) | AC-WKT-18-11 separa "archivo que falta, vacío o dañado" (los dos cerrados) de "una clave de otro tipo" (ese panel cerrado; el otro, con su clave; el toque la guarda como booleana; las demás claves se conservan). Una fila informativa en §11.1.1 con lo que hace el legacy, y su encabezado lo admite | §4.3, §11.1.1 |

**Para los otros roles** (no los hace el PO):

- **QA**: volver a mirar AC-BLE-16-02 con el texto nuevo (pasa a PASS si la auditoría de la llave sigue aceptando solo el caso del diálogo de completado sin lugar libre, como dice B5-10). AC-WKT-18-11, -14, -16 y AC-WKT-17-09 siguen en PASS: cambió el texto, no lo que se verifica.
- **Coordinador**: en el REPORT, contarle al usuario la excepción de la llave en L640 con el diálogo (CAP-10 de la 4b), junto con las DV; no es una DV, pero se ve.

---

## Al cerrar la iteración (PO)

2026-10-30, con la ronda 2 de [05-qa-verification](05-qa-verification.md) (APROBADO CON OBSERVACIONES: 71 PASS, 0 FAIL, 3 NO VERIFICABLE; LT-47 a LT-61 en verde) y sus lentes de [paridad](05-qa-lens-paridad.md) y [motion](05-qa-lens-motion.md). Legacy (`3ae0f3a`, solo lectura) releído para los pendientes: `lib/features/workout/series_panels.dart:115-160`, `:286-320`, `:351-392`. No se leyó `lib/core/strava/strava_secrets.dart`, y `git -C ../rowmate-legacy status --porcelain` quedó vacío. El conteo de criterios no cambia (74) y no hay DV nuevas. El reporte para el usuario es [REPORT.md](REPORT.md).

**Veredicto: aceptada.** El QUÉ se cumple: parciales de 500 m y repeticiones con los números del legacy (el tracker es el del legacy línea por línea y el oráculo, regenerado desde el legacy, sale idéntico), en dos paneles que se abren con un toque o con el teclado y recuerdan su estado, sin encimarse con nada. Queda **una verificación pendiente, no una falla**: nada de la It. 5 se corrió todavía en el build de simulador, porque el `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator` en todas las tareas que tenían que compilar (la deuda de la 4b, WP-B y las dos rondas de QA; la carpeta todavía tiene el build de la 4b). Se hace al empezar la 6a, antes de construir encima (abajo).

### Pendientes de §15, hechos

| Pendiente | Dónde quedó |
|---|---|
| La demo de la It. 5 en el ROADMAP | [ROADMAP §2, It. 5](../../ROADMAP.md): la demo con `Series 600`, "También toca", las decisiones aplicadas y las del gate; la matriz §4 y la fila "It. 5 (cierre, PO)" de §8 |
| F-WKT-07, casos borde (el primer segundo de la rutina) | [workout-engine-hud](../../product/inventory/workout-engine-hud.md), F-WKT-07 y "Cambios al cerrar la It. 5" |
| F-WKT-18, puntos 6 y 7 (lo que se corta o se parte) | Ídem, F-WKT-18 |
| F-WKT-18, punto 8 (la preferencia leída después del primer dibujo) | Ídem; además, en casos borde, la clave de otro tipo (QA-V5-P02) y el guardado que falla |
| El mapa y los tests de F-WKT-07 y -08 (8 + 6 de 12) | Ídem, mapa y "Cobertura de tests legacy" |
| BKL-26 | [FEATURE_INVENTORY §6.2](../../product/FEATURE_INVENTORY.md) y "Cambios al cerrar la It. 5"; ROADMAP §3 dice BKL-01 a BKL-26 |
| Las biblias (del Designer, WP-D2) | Hechas: 04-design-notes §15.1 |

### Precisiones del modo compacto de la barra libre (pedidas por el Designer, 00-deuda-diseno §1.5)

Son de aspecto, no cambian datos ni textos, y ya están en el código y en CAP-13 a CAP-15 de la 4b, regeneradas. En TV y P360 en español, cuando el título libre no entra a su tamaño con las medidas normales, la barra libre pasa al **modo compacto** (00-deuda-diseno §1.3). Valen estos textos:

- **AC-WKT-16-07**: "… y el resto de la barra no cambia, **salvo, en el modo compacto, los aires del reloj y del chip** (00-deuda-diseno §1.3)".
- **AC-WKT-16-02** (de la 4b), punto 5: "10 de separación" pasa a "10 (8 en el modo compacto)".
- **AC-WKT-16-03** (de la 4b): "relleno 8 × 3" pasa a "8 × 3 (6 × 3 en el modo compacto)".
- **AC-WKT-19-01** (de la 4b): "5 de separación" pasa a "5 (3 en la barra libre en el modo compacto)". En la barra de pasos sigue en 5.

QA ya verificó la barra con estos valores (05-qa-verification §2.4 y §3.4; `FBT.CompactBarSeparatesTheTitleFromTheClock`, `FBT.EnglishAndLandscapeKeepTheNormalBar`): no cambia ningún resultado.

### Decisiones del PO al cerrar

| Tema | Decisión |
|---|---|
| **La verificación en el build** (AC-WKT-13-15, la ventana `[PERF]` de AC-WKT-18-21 y la prueba en equipo de 05-qa-verification §R2.6) | Riesgo ya aceptado en el gate (QA-G5T-14). Queda NO VERIFICABLE en esta iteración y se hace **al empezar la It. 6a, antes de construir encima**, en cuanto el usuario cierre su `RowMate.exe`: el build de simulador, las tres entradas con `[PERF]`, la ventana con los paneles y la prueba a mano (abrir y cerrar con mouse y teclado, reiniciar la app con los paneles recordados, `Simular desconexión`, la cuenta grande, el sello y `PAUSA`, matar el proceso a mitad). Si algo falla, el criterio se reabre y va primero |
| **QA-V5-R2-01** (DD5-27 en `ColumnScroll`, del Designer) | El criterio de producto se cumple: en TH y L640, con la columna desplazada, una vuelta nueva baja lo que se ve con el empuje, sin saltos (AC-WKT-18-13, "lo que se ve no salta"). Decide el Designer al empezar la 6a: por defecto, la opción (a) de QA, que DD5-27 valga solo para las filas de W1280 y 04-design-notes §5.2 lo diga (sin cambiar código ni tomas); la (b), compensar cuando la fila entra arriba del techo, queda a su criterio |
| **DT5-D1 y DT5-D2** (00-deuda-4b §2.6: el HUD se arma en el primer cuadro negro de la entrada y el arranque lo precalienta) | Se aceptan: no cambian el QUÉ. La sesión y el reloj siguen corriendo desde el toque, "Atrás" responde desde el cuadro 0 y el HUD no se ve antes de los 220 ms |
| **La demo (c) de P-17** | Se repite al cierre de esta iteración con los pasos de 00-deuda-4b §1.5, suspendiendo a mano: la de la 4b no fue concluyente (con la app abierta, Windows no se duerme solo, y bloquear la sesión o apagar la pantalla no suspende). La línea del aviso puede decir `reanuda (2)`: Windows manda dos avisos al despertar (O-6 de la lente de motion) |
| **O-1 de la lente de motion** (dos procesos con el mismo `prefs.json`) | Se acepta: es lo que hace el legacy (P-11). En el REPORT, "un solo `RowMate.exe` a la vez" |
| **P-01 a P-14** | P-01, P-06, P-07, P-09 a P-12 quedan como estaban; P-05 resuelta. Van al usuario con su valor por defecto: P-02 (DV-48), P-03 y P-04 (DV-50), P-08, P-13 (DV-49) y P-14 (DV-51) |
| **La excepción de la llave** (AC-BLE-16-02, QA-V5-01) | No es una DV; se le cuenta al usuario en el REPORT, con CAP-10 de la 4b |
