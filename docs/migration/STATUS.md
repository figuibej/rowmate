# Estado de la migración

| Campo | Valor |
|---|---|
| Actualizado | 2026-10-28 (rowmate-po, al cerrar la It. 3) |
| Fase | **It. 3 cerrada, esperando el OK del usuario.** PO: **aceptada**, sin condiciones; 10 issues menores para el comienzo de la It. 4a. QA: APROBADO CON OBSERVACIONES en la ronda 3 ([05-qa-verification.md](iterations/03-rutinas/05-qa-verification.md)): dotnet 1599 pruebas (1582 pasan, 0 fallan, 17 ignoradas); Unity EditMode 1479 / 1463 / 0 / 16 y con simulador 1630 / 1614 / 0 / 16; PlayMode 188 / 188 y con simulador 235 / 235; matriz con 377 de 377 IDs en PASA; 47 tomas de captura sin problemas e iguales byte a byte entre corridas. 101 AC cumplen (13 con una parte de equipo pendiente), 0 fallan, 1 no verificable (Android); 0 issues bloqueantes, 0 mayores y 10 menores. Reporte para el usuario: [iterations/03-rutinas/REPORT.md](iterations/03-rutinas/REPORT.md) |
| Próximo paso | **Usuario**: contestar con `Cancelar` los avisos del Firewall de Windows que quedaron en pantalla, probar la demo en el build de simulador (REPORT §3.1) y dar el OK al gate de la It. 3 (abajo). Después, **It. 4a** (`04a-motor-hud-funcional`): el Architect empieza por la deuda "Al empezar la It. 4a" (abajo) y el PO escribe `iterations/04a-motor-hud-funcional/01-po-spec.md` |
| Plan | [ROADMAP.md](ROADMAP.md): 17 iteraciones + 1 *spike* opcional, 158 IDs. El usuario decidió mantenerlo: todo con el simulador hasta la It. 9 y el Bluetooth real en la 10a y la 11 |

## Iteraciones

| N | Iteración | Estado |
|---|---|---|
| 1 | Fundaciones: proyecto Unity, Core puro y piezas base | **Cerrada** ([reporte](iterations/01-fundaciones/REPORT.md)); el usuario aceptó los valores por defecto de su gate |
| 2 | Shell, Dispositivo en vivo y simulador | **Cerrada** ([reporte](iterations/02-dispositivo-simulador/REPORT.md)); el usuario aceptó los valores por defecto de su gate. Su condición (AC-BLE-11-02, QA-V2-01) se cumplió en la It. 3 |
| S-1 | *Spike* opcional de Bluetooth en Windows | **No se hace** (decisión del usuario en el gate de la It. 2; el riesgo se ve en la 10a) |
| 3 | Rutinas: lista, editor y base local | **Cerrada, esperando el OK del usuario** ([reporte](iterations/03-rutinas/REPORT.md)) |
| 4a | Motor de sesión, guardado incremental, reposo y HUD funcional | **Próxima** (espera el OK de la It. 3) |
| 4b | HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema | Pendiente |
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

### Para confirmar en el gate de la It. 3

Si el usuario no dice nada, se aplica "Por defecto". Detalle en [REPORT de la It. 3 §8](iterations/03-rutinas/REPORT.md).

| Decisión | Por defecto | Fuente |
|---|---|---|
| Q-11 / R-14: no se importan las rutinas ni el historial del legacy; la app arranca con su base propia v8 vacía y las dos apps conviven sin tocarse (la importación queda diseñada, sin implementar) | No se importa | [01-po-spec §11](iterations/03-rutinas/01-po-spec.md) |
| P-01 (Q-26 ajustada por Q-92): repeticiones de 1 a 999 (el legacy no tiene tope y una serie ×2 000 000 000 traba el inicio) | Se adopta | [01-po-spec §14](iterations/03-rutinas/01-po-spec.md) |
| P-02 (Q-92): los topes. Duración 99:59:59, distancia 999 999 m, vatios ±9 999, SPM ±999 y split 99:59; progresiones ±999 / ±9 999 / ±5 999, también en cada repetición | Se adoptan | [01-po-spec §14](iterations/03-rutinas/01-po-spec.md), AC-RUT-13-11, -12, AC-RUT-14-06 |
| DV-09 a DV-20: diferencias visibles con el legacy, a propósito. Son los mosaicos de tipo, el ícono de tendencia, los textos tenues legibles, las medidas y los colores legibles de tipo, la llave sobre el `+`, los avisos que se reemplazan, el teclado numérico con signo, los errores visibles, los topes, el desplazamiento único del editor y las ayudas de progresión sin foco. **DV-20** es nueva al cerrar: los campos de una línea quitan también `\r` y `\t` | Se mantienen | [REPORT §6.1](iterations/03-rutinas/REPORT.md); [01-po-spec §11.1](iterations/03-rutinas/01-po-spec.md) |
| DT-05 (informativo): los lectores de pantalla del sistema todavía no leen la app (el legacy sí) | Pasa a la It. 14 (ya es criterio en el ROADMAP) | [01-po-spec §11, §13](iterations/03-rutinas/01-po-spec.md) |

**Decididas por el PO al cerrar la It. 3** (no necesitan respuesta):

- QA-V3-07 se propone como DV-20 (arriba) y el Architect corrige el comentario de `RmTextField`.
- QA-V3-09 se acepta en esta iteración: la llave tapa en reposo alguna acción del editor en W1280, TV y L640, solo con el simulador, como en el legacy. El Designer lo revisa en el diseño de la 4a, junto con el HUD.
- DT-21 se acepta: lo que se cierra se traga los toques durante 150 ms.
- CAP-27 no se toma en esta iteración. AC-APP-05-10 queda cumplido, con el modo claro pendiente, y la foto se toma solo a pedido, después de QA-V3-12.
- QA-V3-02 a -04 se corrigen al empezar la 4a, con una sola regeneración de capturas.

## Prerrequisitos del usuario abiertos

| Para | Qué | Estado |
|---|---|---|
| Ahora (antes de la demo) | Contestar con **`Cancelar`** los tres avisos del Firewall de Windows ("Seguridad de Windows") que dejaron los builds de desarrollo de QA el 2026-10-28 (03:56, 03:57 y 05:20), y el que aparezca al abrir el build de prueba. Oscurecen la pantalla y retienen el mouse y el teclado. La app no usa la red (QA-V3-08) | Pendiente |
| It. 3 (recomendado, 5 minutos) | La demo de [REPORT §3.1](iterations/03-rutinas/REPORT.md) en el build de simulador: cierra la parte "ventana real" de 6 criterios (REPORT §5.3) | Pendiente |
| Cierre de NV (It. 2 y 3) | Módulos **Windows Build Support (IL2CPP)** (con Visual Studio 2022 "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621+) y **Android Build Support** (con OpenJDK y Android SDK & NDK Tools), más un Android 8.0+ de 64 bits con depuración USB. Cierran IL2CPP (AC-BASE-01, R3-01), AC-APP-16-04, AC-APP-02-06 y AC-RUT-11-10, y las partes Android de otros 16 criterios (10 de la It. 2 y 6 de la It. 3) | Pendiente. Sin ellos no se frena ninguna iteración hasta la 6a |
| It. 2 (opcional, 5 minutos en la PC) | Abrir la app legacy después de la nueva (AC-APP-16-03); probar el build con Windows en modo claro (AC-APP-05-01 y, de la It. 3, AC-APP-05-10); los idiomas preferidos `Français (France)`, `Español (Argentina)` (AC-APP-09-06) | Pendiente |
| CI de Unity (opcional) | Licencia Personal de Unity como secreto del repositorio (P-08) | Pendiente. El repositorio ya está en GitHub, público, y `ci-core` corre en verde |
| It. 4a | Nada obligatorio | — |
| It. 6a | Terrain Sample Asset Pack y un Android de gama media | Pendiente |
| It. 10a / 10b / 11 / 12 | Remo AMS-670B, Mac con Xcode + iPhone, pulsómetro, cuenta de Strava | Pendiente |

Resueltos: en la It. 2, Unity Hub con el Editor 6000.3.27f1 y Windows Standalone (Mono), y las fuentes e íconos. Entre la It. 2 y la 3, el repositorio en GitHub ([figuibej/rowmate](https://github.com/figuibej/rowmate), público, con `ci-core` en verde).

## Pendiente de otros responsables (del cierre de la It. 3)

Issues menores de [05-qa-verification](iterations/03-rutinas/05-qa-verification.md) (rondas 1 a 3). Ninguno afecta un criterio. Los de "Al empezar la It. 4a" se resuelven antes de construir encima. Los dos mayores de la verificación (QA-V3-01 y QA-V3-11, de la herramienta de CAP-27) quedaron cerrados.

| ID | Qué | Quién | Cuándo |
|---|---|---|---|
| QA-V3-02 | En VID-01, el último bloque (o el último paso) desaparece sin colapsar ni fundirse, y el estado vacío aparece entero. Siguen los otros puntos de motion de [04-design-notes §16.6](iterations/03-rutinas/04-design-notes.md): las hojas de la serie, el cruce del color del tipo y la línea de error | Architect, con el visto bueno del Designer | Al empezar la It. 4a |
| QA-V3-03 | DEMO-05 no muestra las progresiones: el contenido del diálogo queda arriba | Architect | Al empezar la It. 4a, con -02 y -04 (una sola regeneración, dos corridas seguidas por QA-V3-06) |
| QA-V3-04 | Arnés: `Type` deja el cursor en 0 y `Tap` no mueve el foco (en DEMO-04 y -06 el nombre sale seleccionado). Solo del arnés | Architect | Ídem |
| QA-V3-05 | Tres `[Ignore]` con el motivo viejo "NO VERIFICABLE → It. 3" | Architect | It. 4a |
| QA-V3-06 | Una vez (ronda 1), el arnés no dio los mismos bytes justo después de correr el otro conjunto: documentarlo o comparar con tolerancia | Architect | It. 4a |
| QA-V3-07 | `\r` y `\t` en los campos de una línea (el legacy solo quita `\n`) | PO: DV-20, al gate. Architect: el comentario de `RmTextField` | Gate de la It. 3 / It. 4a |
| QA-V3-08 | Cada build de desarrollo nuevo dispara el aviso del Firewall: usar siempre la misma carpeta de salida y explicar en `tools/unity/README.md` qué contestar | Architect (el usuario, los tres avisos pendientes) | Al empezar la It. 4a |
| QA-V3-09 | Con el simulador, la llave tapa en reposo la cruz de la 4.ª fila en W1280, la de la fila 8 en TV y `Serie` en L640 | Designer (PO: aceptado en la It. 3) | Diseño de la It. 4a |
| QA-V3-10 | `Guardar`, `Eliminar` y `Reintentar` lanzan la tarea con `_ =`: una excepción inesperada que no venga de la base quedaría sin registro | Architect | Al empezar la It. 4a |
| QA-V3-12 | La foto de CAP-27 saldría con franjas de unos 8 px del escritorio y se escribe por defecto en `docs/`, que es público | Architect | Antes de cualquier toma de CAP-27 |

La deuda de la It. 2 (QA-V2-01 a -15) quedó resuelta al empezar la It. 3 ([00-deuda-it2](iterations/03-rutinas/00-deuda-it2.md), [00-deuda-diseno](iterations/03-rutinas/00-deuda-diseno.md)). QA-V2-14 se cerró en el código; la corrida de `ci-unity` en GitHub sigue NO VERIFICABLE hasta que esté la licencia (P-08). Las biblias de diseño, ASSET_SLOTS, ARCHITECTURE y los ADR 007, 008 y 019 quedaron al día con la It. 3.

Otra deuda abierta:

- **Para la It. 4a.** R3-08: antes de expandir una rutina hay que comprobar los topes con `StepLimitCheck`, porque una base tocada a mano con ×2 000 000 000 agotaría la memoria. El spec de la 4a fija qué ve el usuario (Architect y PO). Además, el kerning de los títulos `display` y `headline` y del HUD, antes de sus capturas ([04-design-notes §13](iterations/03-rutinas/04-design-notes.md); Designer).
- **Deseables sin AC** (P2): `Guardar` con indicador si tarda más de 300 ms (DN3-19), y el alto que crece de una a dos líneas y la mini-barra que interpola (DN3-20).
- **Para la 10a.** El *lock* del *scheduler* para los *callbacks* nativos de Bluetooth (Architect).
- **Para la It. 14.** Lectores de pantalla (DT-05) y DPI con dos monitores.
- **Con teléfono.** El teclado del sistema y el menú de texto (AC-RUT-11-10; la variante de P-06 si Android no muestra su menú, R3-13). También `:hover` con el dedo, el costo de la copia de pantalla en un Android de gama baja y la barra de estado.
- **CI.** `ITT.WindowCaptureStartsTheExeForReal` se ve en `ci-core` de GitHub en el próximo *push* (en `ubuntu-latest` suma un ignorado).

Riesgos:

- **Abiertos**: R-T6 / R3-01 (IL2CPP con el SQLite nativo), hasta que esté el módulo; R3-02 (Android: carpeta privada, teclado y "Atrás") y R3-13 (menú de texto en Android), hasta que estén el módulo y el teléfono.
- **Pasa al diseño de la 4a**: R3-08.
- **Mitigados y medidos**: R3-03 (hilos de la base) y R3-09 (lista larga: 54 rutinas en 197 ms, 7,9 ms por cuadro como máximo).
- **Cerrados**: R-T1, R-T2, R-T5 y R-T12 (It. 2).

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
