# Estado de la migración

| Campo | Valor |
|---|---|
| Actualizado | 2026-10-27 (rowmate-po, al cerrar la It. 2) |
| Fase | **It. 2 cerrada, esperando el OK del usuario.** PO: **aceptada con una condición** (QA-V2-01 se corrige al empezar la It. 3 y reabre AC-BLE-11-02 hasta entonces). QA: APROBADO CON OBSERVACIONES ([05-qa-verification.md](iterations/02-dispositivo-simulador/05-qa-verification.md)): dotnet 1264 pruebas (1247 pasan, 0 fallan, 17 ignoradas); Unity EditMode 1132 / 1116 / 0 / 16 y con simulador 1281 / 1265 / 0 / 16; PlayMode 53 / 53 y con simulador 85 / 85; 41 tomas de captura sin problemas. 118 AC cumplen, 1 falla (menor), 6 no verificables; 15 de 15 casos legacy; 15 issues menores. Reporte para el usuario: [iterations/02-dispositivo-simulador/REPORT.md](iterations/02-dispositivo-simulador/REPORT.md) |
| Próximo paso | **Usuario**: ver la demo (REPORT §3) y dar el OK a las decisiones del gate de la It. 2 (abajo). Después, **It. 3** (`03-rutinas`): el Architect empieza por QA-V2-01 y la deuda de "Al empezar la It. 3" (abajo), y el PO escribe `iterations/03-rutinas/01-po-spec.md` |
| Plan | [ROADMAP.md](ROADMAP.md): 17 iteraciones + 1 *spike* opcional, 158 IDs |

## Iteraciones

| N | Iteración | Estado |
|---|---|---|
| 1 | Fundaciones: proyecto Unity, Core puro y piezas base | **Cerrada** ([reporte](iterations/01-fundaciones/REPORT.md)); el usuario aceptó los valores por defecto de su gate |
| 2 | Shell, Dispositivo en vivo y simulador | **Cerrada, esperando el OK del usuario** ([reporte](iterations/02-dispositivo-simulador/REPORT.md)) |
| S-1 | *Spike* opcional de Bluetooth en Windows | Opcional (decide el usuario: por defecto no se hace) |
| 3 | Rutinas: lista, editor y base local | **Próxima** (espera el OK de la It. 2) |
| 4a | Motor de sesión, guardado incremental, reposo y HUD funcional | Pendiente |
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

Valores por defecto de Q-01, Q-14, Q-90, R-14 / Q-85, Q-92 (gate de la It. 3, adelantada), P-01 (regla de idioma de Flutter), P-04 (Windows 10 21H1 o más nuevo) y P-06 (teclado y copiar y pegar en el idioma del sistema). Detalle en [REPORT de la It. 1 §8](iterations/01-fundaciones/REPORT.md).

### Para confirmar en el gate de la It. 2

Si el usuario no dice nada, se aplica "Por defecto". Detalle en [REPORT de la It. 2 §8](iterations/02-dispositivo-simulador/REPORT.md).

| Decisión | Por defecto | Fuente |
|---|---|---|
| P-03: tope de 1,3× a la letra del sistema (el legacy no tiene tope) | Se adopta | [01-po-spec §14](iterations/02-dispositivo-simulador/01-po-spec.md) |
| DV-01 a DV-08: diferencias visibles con el legacy, a propósito (cinta con texto oscuro, borde que destella, etiquetas al 100 %, `bpm` rosa, `deviceNoResults` al 70 %, "Atrás" en Android 12+ deja la app en segundo plano, barra de 64 dp, sin pantalla siempre encendida hasta la 4a) | Se mantienen | [01-po-spec §11.1](iterations/02-dispositivo-simulador/01-po-spec.md) |
| QA-V2-08: barra de título de Windows clara con el sistema en modo oscuro (el legacy la pone oscura) | Paridad: se corrige al empezar la It. 3 (alternativa: dejarla para la It. 14) | [05-qa-verification §6](iterations/02-dispositivo-simulador/05-qa-verification.md) |
| S-1: *spike* de Bluetooth en Windows con un pulsómetro real, entre la 2 y la 3 | No se hace (el riesgo se ve en la 10a) | ROADMAP §2 |

**Decididas por el PO al cerrar la It. 2** (no necesitan respuesta): D-26 (alto máximo del panel descontando el margen inferior) confirmada; D-25 y D-27 aceptadas; QA-V2-05 (CAP-17 sin el renglón `14:00` en la foto) aceptada, con la comprobación del valor en el catálogo; QA-V2-09: los diagnósticos por línea de comandos (`-rowmate-color-probe`, `-rowmate-ui-pipeline`) solo en builds de desarrollo, nunca en el normal; la marca "Development Build" solo en los builds con simulador (P-01 de la spec).

## Prerrequisitos del usuario abiertos

| Para | Qué | Estado |
|---|---|---|
| It. 2 (cierre de NV) | Módulos **Windows Build Support (IL2CPP)** (con Visual Studio 2022 "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621+) y **Android Build Support** (con OpenJDK y Android SDK & NDK Tools), más un Android 8.0+ de 64 bits con depuración USB: cierran AC-BASE-01 en IL2CPP, AC-APP-16-04, AC-APP-02-06 y las partes Android de otros 10 criterios | Pendiente (el usuario los está agregando). Sin ellos no se frena ninguna iteración hasta la 6a |
| It. 2 (opcional, 5 minutos en la PC) | Abrir la app legacy después de instalar la nueva (AC-APP-16-03); probar el build con Windows en modo claro (AC-APP-05-01) y con los idiomas preferidos `Français (France)`, `Español (Argentina)` (AC-APP-09-06) | Pendiente |
| It. 1 / It. 2 (opcional) | Repositorio remoto en GitHub (P-03 de la It. 1) y licencia Personal de Unity como secreto (P-08). Antes, el Architect corrige `ci-unity.yml` (QA-V2-14) | Pendiente |
| It. 3 | Nada obligatorio (la base local usa `com.gilzoide.sqlite-net`, ya instalado) | — |
| It. 6a | Terrain Sample Asset Pack y un Android de gama media | Pendiente |
| It. 10a / 10b / 11 / 12 | Remo AMS-670B, Mac con Xcode + iPhone, pulsómetro, cuenta de Strava | Pendiente |

Resueltos en la It. 2: Unity Hub con el Editor 6000.3.27f1 y Windows Standalone (Mono); fuentes e íconos descargados (Inter, Barlow Semi Condensed, JetBrains Mono y "RowMate Icons").

## Pendiente de otros responsables (del cierre de la It. 2)

Issues menores de [05-qa-verification §6](iterations/02-dispositivo-simulador/05-qa-verification.md). Ninguno bloqueó el cierre. Los marcados "Al empezar la It. 3" se resuelven antes de construir encima.

| ID | Qué | Quién | Cuándo |
|---|---|---|---|
| QA-V2-01 | `Rower` + `Conectado` visibles 120 ms en la vista que sale al desconectar el remo (único AC que falla: AC-BLE-11-02, reabierto) | Architect | **Al empezar la It. 3, primera tarea**, con el test PlayMode que propone QA |
| QA-V2-02 | El build normal lleva `PerformanceTestRunInfo` / `Settings` (paquete de tests de rendimiento); un build fallido los deja en `Assets/Resources/` | Architect | Al empezar la It. 3 |
| QA-V2-03 | `build.ps1` sale con 1 (y no con 3, NO VERIFICABLE) sin el módulo IL2CPP; los logs de Mono e IL2CPP se pisan | Architect | It. 3 |
| QA-V2-04 | La sonda de `bpm` de CAP-04b falla en silencio e `INDEX.md` marca la toma como "ok" | Architect | It. 3 |
| QA-V2-05 | CAP-17: el renglón `14:00` no entra con el final del panel en P800 | PO: aceptado. Architect: comprobación `sim-hour-value == 14:00` en el catálogo | It. 3 |
| QA-V2-06 | Motivos de `[Ignore]` viejos ("NO VERIFICABLE → It. 2") | Architect | It. 3 |
| QA-V2-07 | Los `FontAsset` dinámicos (hasta 6,8 MB) y los assets de URP se vuelven a serializar en cada build o corrida | Architect | Al empezar la It. 3, antes del primer commit |
| QA-V2-08 | Barra de título clara con Windows en modo oscuro | Architect (Platform) | It. 3, si el usuario acepta el valor por defecto |
| QA-V2-09 | `-rowmate-color-probe` y `-rowmate-ui-pipeline` funcionan en el build normal | Architect | Al empezar la It. 3 (decisión del PO: solo en desarrollo) |
| QA-V2-10 | `BuildOutputInspector` no busca los textos visibles del simulador | Architect | It. 3 |
| QA-V2-11 | R-01 depende de cómo se arme el build: falta el freno en el propio código del simulador | Architect | It. 3 |
| QA-V2-12 | Contraste de `metric.hr.hud` sobre el vidrio: 4,50, justo en el umbral AA | Designer | Antes de la It. 4a |
| QA-V2-13 | Barra de desplazamiento vertical siempre visible en los perfiles de teléfono | Architect + Designer | It. 3 |
| QA-V2-14 | `ci-unity.yml` inválido, solo manual y sin las suites del simulador | Architect | Antes de activar el CI (P-03 / P-08); a más tardar la It. 14 |
| QA-V2-15 | VID-03 provoca la caída por API y no tocando el botón del panel | Architect | It. 3 |

Otra deuda abierta: alinear ART_BIBLE y ANIMATION_BIBLE con [04-design-notes §11 y §15.6](iterations/02-dispositivo-simulador/04-design-notes.md) (Designer, al empezar la It. 3); sumar al catálogo del arnés las sondas de aspecto del Designer y que la regla de toma detecte transiciones congeladas (Architect, It. 3); *lock* del *scheduler* para los *callbacks* nativos de Bluetooth (Architect, diseño de la 10a); DPI con dos monitores (It. 14); `:hover` con el dedo en Android, costo de la copia de pantalla en Android de gama baja y barra de estado de Android (cuando haya módulo y teléfono). La deuda de la It. 1 (QA-V-01 a QA-V-09) quedó resuelta en la It. 2 ([00-deuda-it1](iterations/02-dispositivo-simulador/00-deuda-it1.md), [00-deuda-diseno](iterations/02-dispositivo-simulador/00-deuda-diseno.md)); QA-V-11 se cerró con Q-92.

Riesgos: R-T1 cerrado (los colores translúcidos salen como en el legacy, medido en el arnés y en un build de Windows, ADR 019); R-T2, R-T5 y R-T12 cerrados en el primer open ([00-primer-open](iterations/02-dispositivo-simulador/00-primer-open.md)); R-T6 (IL2CPP) sigue abierto hasta que esté el módulo.

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
