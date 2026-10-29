# Estado de la migración

| Campo | Valor |
|---|---|
| Actualizado | 2026-10-29 (rowmate-po, al cerrar la It. 4a) |
| Fase | **It. 4a cerrada, esperando el OK del usuario.** PO: **aceptada, con dos condiciones**: QA-V4a-03 (el divisor de la banda del HUD) y QA-V4a-04 (el primer Tab sin anillo de foco) se corrigen al empezar la It. 4b, y hasta entonces AC-WKT-17-04 y AC-APP-05-07 quedan reabiertos. QA: APROBADO CON OBSERVACIONES en la ronda 2 ([05-qa-verification](iterations/04a-motor-hud-funcional/05-qa-verification.md)): dotnet 2228 pruebas (2206 pasan, 0 fallan, 22 ignoradas); Unity EditMode 2093 / 2067 / 0 / 26 y con simulador 2266 / 2240 / 0 / 26; PlayMode 264 / 263 / 0 / 1 y con simulador 328 / 327 / 0 / 1; matriz de 535 IDs con 534 en PASA, 0 en FALLA y 1 ignorado (Android); 49 tomas de captura sin problemas e iguales byte a byte entre corridas. 156 AC cumplen, 2 fallan (menores) y 4 no son verificables; 0 issues bloqueantes, 0 mayores (los dos de la ronda 1 se corrigieron) y 8 menores. Reporte para el usuario: [iterations/04a-motor-hud-funcional/REPORT.md](iterations/04a-motor-hud-funcional/REPORT.md) |
| Próximo paso | **Usuario**: cerrar el `RowMate.exe` que quedó abierto (QA-V4a-08), probar la demo en el build de simulador (REPORT §3.1) y dar el OK al gate de la It. 4a (abajo). Después, **It. 4b** (`04b-hud-exr`): el Architect empieza por las dos condiciones y la deuda "Al empezar la It. 4b" (abajo), y el PO escribe `iterations/04b-hud-exr/01-po-spec.md` |
| Plan | [ROADMAP.md](ROADMAP.md): 17 iteraciones + 1 *spike* opcional (que no se hace), 158 IDs. El usuario decidió mantenerlo: todo con el simulador hasta la It. 9 y el Bluetooth real en la 10a y la 11 |

## Iteraciones

| N | Iteración | Estado |
|---|---|---|
| 1 | Fundaciones: proyecto Unity, Core puro y piezas base | **Cerrada** ([reporte](iterations/01-fundaciones/REPORT.md)); el usuario aceptó los valores por defecto de su gate |
| 2 | Shell, Dispositivo en vivo y simulador | **Cerrada** ([reporte](iterations/02-dispositivo-simulador/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su condición (AC-BLE-11-02, QA-V2-01) se cumplió en la It. 3 |
| S-1 | *Spike* opcional de Bluetooth en Windows | **No se hace** (decisión del usuario en el gate de la It. 2; el riesgo se ve en la 10a) |
| 3 | Rutinas: lista, editor y base local | **Cerrada** ([reporte](iterations/03-rutinas/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su deuda se resolvió al empezar la 4a |
| 4a | Motor de sesión, guardado incremental, reposo y HUD funcional | **Cerrada, esperando el OK del usuario** ([reporte](iterations/04a-motor-hud-funcional/REPORT.md)). Condiciones para la 4b: QA-V4a-03 y QA-V4a-04 |
| 4b | HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema | **Próxima** (espera el OK de la It. 4a) |
| 5 | Series en vivo: parciales y repeticiones | Pendiente |
| 6a | Escena: cielo, luz, hora, Lago alpino y agua degradada | Pendiente |
| 6b | Agua avanzada, desenfoque del vidrio y rendimiento | Pendiente |
| 7 | Remero, bote y palada | Pendiente |
| 8 | Río, Costa, Regata y selector | Pendiente |
| 9 | Historial y detalle | Pendiente |
| 10a | Bluetooth real en Android y Windows | Pendiente |
| 10b | Tanda Apple (iOS y macOS) | Flotante: cuando haya Mac |
| 11 | Pulsómetro real | Pendiente |
| 12 | Strava: conexión y subida | Pendiente |
| 13 | Strava: sincronización y Perfil | Pendiente |
| 14 | Pulido, identidad, plataformas y CI | Pendiente |

## Decisiones

### Ya aceptadas por el usuario

- **Gate de la It. 1**: valores por defecto de Q-01, Q-14, Q-90, R-14 / Q-85, Q-92 (el criterio del tope por campo), P-01 (regla de idioma de Flutter), P-04 (Windows 10 21H1 o más nuevo) y P-06 (teclado y copiar y pegar en el idioma del sistema). Detalle en [REPORT de la It. 1 §8](iterations/01-fundaciones/REPORT.md).
- **Gate de la It. 2**: valores por defecto de P-03 (tope de 1,3× a la letra del sistema), DV-01 a DV-08, QA-V2-08 (barra de título de Windows oscura, corregida en la It. 3) y S-1 (no se hace). Además, mantener el roadmap y seguir con el simulador. Detalle en [REPORT de la It. 2 §8](iterations/02-dispositivo-simulador/REPORT.md).
- **Gate de la It. 3**: valores por defecto de Q-11 / R-14 (no se importan las rutinas ni el historial del legacy), P-01 (repeticiones de 1 a 999), P-02 (los topes de Q-92), DV-09 a DV-20 y DT-05 (los lectores de pantalla, en la It. 14). Lo registró [00-deuda-it3](iterations/04a-motor-hud-funcional/00-deuda-it3.md) al empezar la 4a. Detalle en [REPORT de la It. 3 §8](iterations/03-rutinas/REPORT.md).

### Para confirmar en el gate de la It. 4a

Si el usuario no dice nada, se aplica "Por defecto". Detalle en [REPORT de la It. 4a §8](iterations/04a-motor-hud-funcional/REPORT.md).

| Decisión | Por defecto | Fuente |
|---|---|---|
| Q-12: telemetría incremental. Cada punto queda guardado mientras se rema; una sesión cortada conserva sus puntos y una terminada normalmente queda igual que en el legacy | Se adopta | [01-po-spec §11](iterations/04a-motor-hud-funcional/01-po-spec.md), AC-WKT-06-05, -07, -10 |
| D-ART-01: fuera de objetivo, el valor en `#FF625B` con una flecha ▲ / ▼, y contraste de 7:1 en blanco y en las métricas no rojas | Se adopta (si no: `#FF4444` sin flecha, como el legacy) | AC-WKT-10-05 |
| D-ART-02 / P-05: el HUD un 15 a 25 % más grande y escalado con la pantalla hasta 1,6×, sin seguir la letra del sistema | Se adopta (si no: los tamaños del legacy, sin escala) | AC-WKT-17-01 |
| D-ART-10 / P-12 (DV-39): la llave del simulador abajo a la izquierda y, en el entrenamiento, sobre kcal y BPM (vertical) o junto a `Pausa` (horizontal); el panel se abre desde la llave y deja a la vista Split y Watts | Se adopta | AC-BLE-16-02, -03 |
| DV-21 a DV-39: diferencias visibles con el legacy, a propósito. Las que más se notan: `Próximo:` en español (DV-24, P-01), las pastillas en blanco (DV-38, P-15), la tarjeta de Workout con descripción y mini-barra (DV-21, P-06), un toque, una sesión (DV-30) y la rutina que no se puede iniciar (DV-28) | Se mantienen | [REPORT §6.1](iterations/04a-motor-hud-funcional/REPORT.md); [01-po-spec §11.1](iterations/04a-motor-hud-funcional/01-po-spec.md) |
| P-02 (informativo): hasta la 4b, al terminar sola la rutina se avisa `¡Entrenamiento completado!` | Sí; la 4b pone el diálogo | AC-WKT-03-05 |
| P-03 (sin urgencia): cerrar la ventana a mitad de una sesión la deja cortada, como un corte de luz | No se termina sola | [01-po-spec §14](iterations/04a-motor-hud-funcional/01-po-spec.md) |
| **P-17** (nueva al cerrar, QA-V4a-ROB-05): si Windows suspende el equipo con una sesión en marcha, ¿el tiempo suspendido cuenta? | No cuenta, como en el legacy; el criterio va en la 4b | [01-po-spec §14](iterations/04a-motor-hud-funcional/01-po-spec.md) |

**Decididas por el PO al cerrar la It. 4a** (no necesitan respuesta):

- DT4a-17: si el reintento del cierre recupera un lote que falló, el aviso del final es el de un guardado bueno (AC-WKT-06-12, precisado).
- QA-V4a-L01: P-04 y DV-26 suman el monitor que no informa la distancia cada segundo; requisito para la 10a: un paso por distancia nunca queda trabado si el monitor informa avance.
- QA-V4a-03 y QA-V4a-04 son las condiciones del cierre. QA-V4a-04 también afecta al editor de Rutinas; el editor sigue saliendo sin confirmar, como el legacy.
- QA-V4a-05, -06 (WP-B0) y L02 pasan a la 4b; QA-V4a-08 es del coordinador.
- Las observaciones de QA (O-01 a O-05, O-R2-01) se aceptan para la 4a; O-01 se revisa con la salida del inmersivo de la 4b, y la llave dentro de la zona del bote en L640, en la It. 7.
- Pendientes del PO de la spec §15: hechos (ROADMAP, FEATURE_INVENTORY y el inventario `workout-engine-hud`; [01-po-spec §17](iterations/04a-motor-hud-funcional/01-po-spec.md)). R3-08 quedó resuelta (AC-WKT-12-08, DV-28).

## Prerrequisitos del usuario abiertos

| Para | Qué | Estado |
|---|---|---|
| Ahora (antes de la demo) | Cerrar el `RowMate.exe` que quedó abierto desde el Explorador durante la verificación. Es el build de la ronda 1, sin los arreglos, y mientras está abierto no se puede recompilar `Builds/Windows/simulator` (QA-V4a-08) | Pendiente |
| It. 4a (recomendado, 10 minutos) | La demo de [REPORT §3.1](iterations/04a-motor-hud-funcional/REPORT.md) en el build de simulador. La parte (b) arma una rutina en Rutinas, así que cubre también lo de la ventana real que quedaba de la It. 3 | Pendiente |
| It. 4a (opcional, 1 minuto) | `powercfg /requests` en una consola de administrador, con la app abierta y después de cerrarla (REPORT §3.7). Cierra AC-APP-14-01 a -03 | Pendiente |
| Cierre de NV (It. 2, 3 y 4a) | Módulos **Windows Build Support (IL2CPP)** (con Visual Studio 2022 "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621+) y **Android Build Support** (con OpenJDK y Android SDK & NDK Tools), más un Android 8.0+ de 64 bits con depuración USB. Cierran IL2CPP (AC-BASE-01, R3-01), AC-APP-16-04, AC-APP-02-06, AC-RUT-11-10 y AC-WKT-02-10, la parte Android de AC-APP-14-02 y las partes Android de otros criterios (10 de la It. 2, 6 de la It. 3 y 3 de la 4a: AC-WKT-02-08, -13-03 y -20-03) | Pendiente. Sin ellos no se frena ninguna iteración hasta la 6a. En la 4b, las barras del sistema ocultas (F-APP-15) quedarían NO VERIFICABLE |
| It. 2 (opcional, 5 minutos en la PC) | Abrir la app legacy después de la nueva (AC-APP-16-03); probar el build con Windows en modo claro (AC-APP-05-01 y, de la It. 3, AC-APP-05-10); los idiomas preferidos `Français (France)`, `Español (Argentina)` (AC-APP-09-06) | Pendiente |
| CI de Unity (opcional) | Licencia Personal de Unity como secreto del repositorio (P-08) | Pendiente. El repositorio está en GitHub, público, y `ci-core` corre en verde |
| It. 4b | Nada obligatorio | — |
| It. 6a | Terrain Sample Asset Pack y un Android de gama media | Pendiente |
| It. 10a / 10b / 11 / 12 | Remo AMS-670B, Mac con Xcode + iPhone, pulsómetro, cuenta de Strava | Pendiente |

Resueltos: en la It. 2, Unity Hub con el Editor 6000.3.27f1 y Windows Standalone (Mono), y las fuentes e íconos. Entre la It. 2 y la 3, el repositorio en GitHub ([figuibej/rowmate](https://github.com/figuibej/rowmate), público, con `ci-core` en verde). Entre la It. 3 y la 4a, los avisos del Firewall que dejaron los builds de la It. 3: tienen su regla y la It. 4a no dejó ninguno nuevo (12 reglas antes y después).

## Pendiente de otros responsables (del cierre de la It. 4a)

Issues de [05-qa-verification](iterations/04a-motor-hud-funcional/05-qa-verification.md) (ronda 2) y de las lentes. Los de "Al empezar la It. 4b" se resuelven antes de construir encima; las dos condiciones van primero.

| ID | Qué | Quién | Cuándo |
|---|---|---|---|
| QA-V4a-03 | El divisor de la banda del HUD mide 0 de ancho en TV, TH y L640, así que no se ve (AC-WKT-17-04 falla). `flex-shrink: 0` en el divisor, una afirmación de ancho en `ImmersiveHudTests.BandVitalsAndLiveValues` y regenerar `it4a` con dos corridas | Architect, con el visto bueno del Designer | **Condición: al empezar la It. 4b** |
| QA-V4a-04 | El primer Tab después de usar el mouse enfoca sin anillo (AC-APP-05-07 falla): pasa en el HUD, en Workout y en el editor de Rutinas, donde un Enter con el foco invisible en `←` sale sin guardar. `FocusHighlight` en la raíz del panel (o la clase en un `FocusInEvent` que no venga del puntero) y una PlayMode "sin foco, un Tab enfoca con anillo" en el HUD y en el editor | Architect | **Condición: al empezar la It. 4b** |
| QA-V4a-05 | `TabularNumber` sigue animando cifras que ya salieron del panel: avisos de DPI en el registro del player (88 en la ronda 2) | Architect | Al empezar la It. 4b |
| QA-V4a-06 | WP-B0 sin integrar: el kerning K2 y RowMate Numeric SC. Arrastra el `[Ignore]` de `HudLayoutTests.ScalesOfTheDesignNotes`, dos comprobaciones flojas de CAP-15 y CAP-16, el punto de la sonda de la tendencia y la regeneración única de `it2`, `it3` e `it4a` | Architect (Designer: visto bueno) | Al empezar la It. 4b, con QA-V4a-03, en una sola regeneración de dos corridas |
| QA-V4a-08 | `Builds/Windows/simulator` tiene el build de la ronda 1, sin los arreglos de ROB-01 y ROB-02: recompilarlo con `tools/build/build.ps1 -Target Windows -Simulator`, sin `-Out` | Coordinador o Architect | Cuando el usuario cierre su `RowMate.exe` |
| QA-V4a-L02 | Faltan los guiones nuevos del oráculo del motor (pasos de 0 s y negativos, sin meta con `Terminar`, terminar en pausa, último paso por distancia, pulso del sensor, pausas que cruzan un cambio de paso, dos sesiones seguidas, entre otros): `run-oracle.ps1 -Engine` con Flutter | Architect | It. 4b, con el diálogo de completado (DT4a-15) |
| QA-V4a-L01 | Un monitor que no informa la distancia una vez por segundo puede trabar un paso por distancia (Q-55) | PO (decidido: P-04 ampliada) · Architect y QA | It. 10a, con el AMS-670B |
| P-17 (QA-V4a-ROB-05) | El criterio de la suspensión del equipo, según la respuesta del gate | PO (spec de la 4b) · Architect | It. 4b |
| 04-design-notes §17.6 | El FLIP de kcal cuando BPM aparece o se va; el "…" en cian de la pastilla recortada (puede resolverse con K2) | Architect, con el Designer | It. 4b, después de K2 |
| 00-deuda-diseno §1.6 y §1.7 | Precisar el panel que sigue a la llave entre una pestaña y una pantalla apilada, y su alto máximo en las pestañas (AC-BLE-16-03 fija 248 y 802) | Designer | Diseño de la It. 4b |
| O-01 | Durante el fundido de salida (200 ms), el HUD muestra la foto de reposo (la banda en `00:00`) | Designer | Diseño de la salida del inmersivo (4b) |
| O-R2-01 | `FlushAndWait` devuelve `true` aunque no encole el cierre: distinguirlo si algún día se usa ese valor | Architect | Cuando se use |

La deuda de la It. 3 (QA-V3-02 a -12) quedó resuelta al empezar la It. 4a ([00-deuda-it3](iterations/04a-motor-hud-funcional/00-deuda-it3.md), [00-deuda-diseno](iterations/04a-motor-hud-funcional/00-deuda-diseno.md)), y QA la comprobó en las dos rondas. QA-V3-09 se resolvió con D-ART-10. R3-08 quedó resuelta (AC-WKT-12-08). Los mayores de la verificación de la 4a (QA-V4a-01 y -02, iguales a ROB-01 y -02) y los menores QA-V4a-07, ROB-03, ROB-04 y ROB-05 quedaron cerrados en la ronda 2.

Otra deuda abierta:

- **Para la It. 4b.**
  - R4a-03: el HUD ya está en 60 elementos en el peor caso, sin margen para las píldoras. La 4b tiene que juntar elementos.
  - DT4a-15: el motor tiene que esperar al diálogo de completado antes de borrar los objetivos (O-02 de la lente de paridad).
- **Para la It. 7.** En L640, con el simulador, la llave queda dentro de la zona del bote (04-design-notes §3.9). Hoy es fondo liso; se revisa cuando el bote se vea (Designer).
- **Para la 10a.**
  - QA-V4a-L01.
  - RA-19 (R4a-12): el reloj monótono de Android con la pantalla bloqueada.
  - RA-08 (R4a-10): Android puede cerrar la app en segundo plano.
  - Un cuadro tarde con el remo real (DT4a-10).
  - La variante completa de ROB-04: encolar el cierre en el acto.
  - El *lock* del *scheduler* para los *callbacks* nativos de Bluetooth (de la It. 3).
- **Para la It. 14.** Lectores de pantalla (DT-05), DPI con dos monitores y la pantalla encendida solo en Dispositivo y en el entrenamiento (BKL-24).
- **Deseables sin AC** (P2, de la It. 3): `Guardar` con indicador si tarda más de 300 ms (DN3-19); el alto que crece de una a dos líneas y la mini-barra que interpola (DN3-20).
- **Con teléfono.** De la It. 3: el teclado del sistema y el menú de texto (AC-RUT-11-10; la variante de P-06 si Android no muestra su menú, R3-13), `:hover` con el dedo, el costo de la copia de pantalla y la barra de estado. De la 4a: "Atrás" en el inmersivo, el segundo plano y el primer cuadro al volver, y la pantalla encendida.
- **CI.** `ci-core` corre en GitHub; `ci-unity` espera la licencia (P-08).

Riesgos:

- **Abiertos**:
  - R-T6 / R3-01 (IL2CPP con el SQLite nativo), hasta que esté el módulo.
  - R3-02 y R3-13 (Android), hasta que estén el módulo y el teléfono.
  - R4a-01 (el alcance del segundo plano en Android, sin teléfono), R4a-10 / RA-08 y R4a-12 / RA-19, que se miden en la 10a.
  - R4a-03 (el presupuesto del HUD), para la 4b.
- **Mitigados y medidos**:
  - R4a-07: el pedido de pantalla encendida se libera en el mismo hilo que lo pidió (probado).
  - R4a-13: un alcance de 8 h toma menos de 500 ms y publica una sola foto.
  - R3-03 y R3-09 (It. 3).
- **Cerrados**: R3-08 (It. 4a); R4a-04 y R4a-05 (los resolvió el diseño); R-T1, R-T2, R-T5 y R-T12 (It. 2).

---

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-30 | Documento creado al cerrar el gate del plan (estado: It. 1 por especificar) |

## Historial

| Fecha | Quién | Qué |
|---|---|---|
| 2026-10-27 | rowmate-po | Spec de la It. 1 escrita; la It. 1 pasa a "En diseño". ROADMAP: respaldo del vidrio = token `glass.fallback` (`#070D16 @86 %`), regla 9 alineada con la tabla única de ARCHITECTURE §20 y prerrequisito opcional del repositorio remoto |
| 2026-10-27 | rowmate-po | Spec de la It. 1 corregida por el gate de armonía (QA-02, QA-11, QA-12): Windows sin identificador (D-02 aceptada), punto de telemetría con tiempo obligatorio, P-06 aceptada como diferencia. ROADMAP §6: fila R-14 / Q-85 con D-02 y mínimos de escritorio (P-04) y textos del sistema (P-06) agendados en los gates de la It. 2 y la 10b |
| 2026-10-27 | rowmate-po | **Cierre de la It. 1**: aceptada por el PO (QA: APROBADO CON OBSERVACIONES, 894/912 pasan, 0 fallan, 131 AC PASS, 8 NV). Reporte para el usuario en `iterations/01-fundaciones/REPORT.md`. FEATURE_INVENTORY: Q-85 y R-14 alineadas (QA-V-10) y nueva Q-92 (QA-V-11). ROADMAP: Q-92 en la It. 3 y en §6. Prerrequisitos de la It. 2 detallados. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-10-27 | coordinador | El usuario aceptó los valores por defecto de Q-01, Q-14, Q-90, R-14 / Q-85, Q-92, P-01, P-04 y P-06. Empieza la It. 2 |
| 2026-10-27 | rowmate-po | **Cierre de la It. 2**: aceptada por el PO con una condición (QA-V2-01 al empezar la It. 3; AC-BLE-11-02 reabierto). QA: APROBADO CON OBSERVACIONES (dotnet 1247/1264 pasan y 0 fallan; Unity EditMode con simulador 1265/1281 y PlayMode con simulador 85/85; 118 AC PASS, 1 FAIL menor, 6 NV; 15/15 casos legacy; 15 issues menores). Reporte para el usuario en `iterations/02-dispositivo-simulador/REPORT.md`. Decisiones del PO al cerrar: D-26, D-25, D-27, QA-V2-05 y QA-V2-09. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-10-27 | rowmate-po | Registro de lo que informó el coordinador: el usuario aceptó los valores por defecto del gate de la It. 2 (P-03, DV-01 a DV-08, QA-V2-08 en la It. 3 y S-1, que no se hace) y decidió mantener el roadmap; el repositorio quedó en GitHub, público, con `ci-core` en verde. Empieza la It. 3 |
| 2026-10-28 | rowmate-po | **Cierre de la It. 3**: aceptada por el PO, sin condiciones. QA: APROBADO CON OBSERVACIONES en la ronda 3 (dotnet 1582/1599 pasan y 0 fallan; Unity EditMode con simulador 1614/1630 y PlayMode con simulador 235/235; matriz 377/377; 101 AC PASS, 0 FAIL, 1 NV; 10 issues menores). Reporte para el usuario en `iterations/03-rutinas/REPORT.md`. Decisiones del PO al cerrar: DV-20 (QA-V3-07, al gate), QA-V3-09, DT-21, CAP-27 a pedido y QA-V3-02 a -04 en la 4a. Pendientes de la spec §15 hechos: ROADMAP (It. 3: `-2` y `−2`; It. 4a: rutina fuera de topes, R3-08; It. 14: lectores de pantalla), routines-models (F-RUT-13 y caso 18) y FEATURE_INVENTORY (Q-26 y Q-92). Spec con §17. Legacy sin cambios (`git status --porcelain` vacío) |
| 2026-10-28 | — | El usuario aceptó los valores por defecto del gate de la It. 3 (lo registra [00-deuda-it3](iterations/04a-motor-hud-funcional/00-deuda-it3.md)). Empieza la It. 4a: deuda de la It. 3, spec, diseño técnico, gate de armonía con dos lentes, WP-A, WP-B, WP-D y verificación en dos rondas |
| 2026-10-29 | rowmate-po | **Cierre de la It. 4a**: aceptada por el PO con dos condiciones (QA-V4a-03 y QA-V4a-04 al empezar la It. 4b; AC-WKT-17-04 y AC-APP-05-07 reabiertos). QA: APROBADO CON OBSERVACIONES en la ronda 2 (dotnet 2206/2228 pasan y 0 fallan; Unity EditMode con simulador 2240/2266 y PlayMode con simulador 327/328; matriz 534 PASA de 535; 156 AC PASS, 2 FAIL menores, 4 NV; 11/11 casos legacy; 8 issues menores). Lentes de paridad y robustez: APROBADO CON OBSERVACIONES. Reporte para el usuario en `iterations/04a-motor-hud-funcional/REPORT.md`. Decisiones del PO al cerrar: DT4a-17 (AC-WKT-06-12), QA-V4a-L01 (P-04 y DV-26 ampliadas), P-17 nueva para el gate (QA-V4a-ROB-05) y las observaciones de QA. Pendientes de la spec §15 hechos: ROADMAP (F-WKT-14, `#FF625B`, D-ART-10 y P-17 en §6; P-17 en la 4b; QA-V4a-L01 en la 10a), FEATURE_INVENTORY (Q-55 y Q-60) y workout-engine-hud. Spec con §17. Legacy sin cambios (`git status --porcelain` vacío) |
