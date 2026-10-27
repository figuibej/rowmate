# It. 1 — Fundaciones: reporte de cierre

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-27 |
| Estado | **Cerrada, espera tu OK** |
| Veredicto del PO | **ACEPTADA.** Lo que la It. 1 tenía que lograr está cumplido. Lo que falta solo se puede ver con Unity Editor o en un equipo, y no la frena |
| Veredicto de QA | **APROBADO CON OBSERVACIONES** ([05-qa-verification.md](05-qa-verification.md)): 0 problemas graves, 0 mayores y 11 menores |
| Documentos | [01-po-spec](01-po-spec.md) (qué se pidió) · [02-tech-design](02-tech-design.md) (cómo se hizo) · [03-qa-harmony](03-qa-harmony.md) (control previo) · [04-design-notes](04-design-notes.md) (colores y contraste) · [05-qa-verification](05-qa-verification.md) (verificación final) |

---

## 1. En pocas palabras

La It. 1 no tiene pantallas. Dejó lista la **base de la app nueva**: todo lo que el remero va a ver más adelante (números del remo y del pulsómetro, física del remo simulado, pasos, rutinas con series y progresiones, sesiones, estadísticas y textos en español e inglés) ya sale de reglas **idénticas a las del legacy**. La verifican 912 pruebas automáticas que corren en tu PC sin Unity.

Las pruebas también comparan esas reglas con **el propio código del legacy**: se corrió el código Dart original, se guardaron sus resultados y la app nueva tiene que dar lo mismo, dígito por dígito y letra por letra.

Además quedó armado el **proyecto Unity**, que todavía no se abrió nunca porque Unity no está instalado: estructura, versión 6000.3.27f1, paquetes, colores en un solo lugar y una preparación inicial automática. Se va a abrir por primera vez al empezar la It. 2.

---

## 2. Qué se entregó, por funcionalidad

"Cumple" quiere decir que la regla está probada y pasa. "No verificable" quiere decir que la parte automática está revisada y pasa, pero falta verlo en Unity o en un equipo real (detalle en §5).

| ID | Qué quedó listo | Criterios | Se ve en pantalla en |
|---|---|---|---|
| F-BLE-01 (+ F-RUT-15) | Lectura del remo (8 datos), formato del split `02:13` / `--:--` / `100:00`, formato del tiempo `1:01:01` y textos de la grilla de Dispositivo (`24.0`, `149`, `652m`) | 6 de 6 cumplen | It. 2 (Dispositivo), 4a (HUD) |
| F-BLE-02 | Decodificación del paquete del monitor FTMS: los 8 paquetes de referencia, potencia negativa, campos salteados y paquetes cortos o raros sin errores (se probaron más de 400 000) | 16 de 16 | It. 10a (remo real) |
| F-BLE-03 | Filtro de la búsqueda: en Windows solo aparecen remos (o pulsómetros), y un UUID corto vale lo mismo que el largo | 5 de 5 | It. 10a / 11 |
| F-BLE-14 | Física del remo simulado: fórmulas de Concept2, topes 30-500 W y 14-40 spm, pulso, ruido (±5 % en vatios y ±1 spm) y modo sin ruido | 10 de 10 | It. 2 (simulador) |
| F-HRM-01 | Decodificación del pulsómetro estándar: pulso de 8 y 16 bits, contacto con la piel, paquetes vacíos o cortos | 9 de 9 | It. 2 (simulado), 11 (real) |
| F-RUT-01 (+ F-APP-07) | Los 4 tipos de paso, en el orden del selector, con nombre es/en, color e ícono | 5 de 5 | It. 3 (editor) |
| F-RUT-02 | Modelo del paso (duración, objetivos, progresiones), sin validaciones, como el legacy | 4 de 4 | It. 3 |
| F-RUT-03 | Etiqueta de duración (`5m`, `1m 30s`, `500m`) en es y en | 5 de 5 | It. 3 |
| F-RUT-04 | Etiqueta de objetivos y progresiones (`150-170W · 📈 W +10/rep`) en es y en | 6 de 6 | It. 3, 4a |
| F-RUT-05 | Modelo de rutina y resumen con plurales (`1 serie · 1 descanso` / `1 interval · 1 rest`) | 4 de 4 | It. 3, 4a |
| F-RUT-06 | Expansión de series ×N con progresiones de SPM, vatios y split | 13 de 13 | It. 4a, 9, 12 |
| F-RUT-07 | Posición de cada paso dentro de su serie (por ejemplo, repetición 2 de 3) | 5 de 5 | It. 5 |
| F-RUT-16 | Registro de la sesión: "promedio" = P99 al cerrar, duración y fechas | 5 de 5 | It. 4a, 9 |
| F-RUT-17 | Punto de telemetría | 2 de 2 | It. 4a, 9 |
| F-RUT-18 (+ F-HIS-04) | Estadísticas: P99, totales, pulso promedio y máximo sin contar los ceros | 9 de 9 | It. 9, 12 |
| F-APP-09 | Idioma según el sistema, con la regla exacta del legacy y respaldo en inglés | 5 cumplen, 2 no verificables | It. 2 |
| F-APP-10 | Catálogo de textos es/en: 174 claves (las 159 del legacy, 4 corregidas y 15 nuevas), con parámetros y plurales | 9 de 9 | todas |
| F-APP-16 (también toca) | Nombre `RowMate`, empresa `iguisoft`, versión 1.0.0, identificadores provisionales, Android 8.0+ de 64 bits, iOS 15 | 1 cumple, 4 no verificables | It. 2, 10b, 14 |
| F-APP-19 (también toca) | CI de las pruebas (`ci-core.yml`) para Windows y Linux, y controles de higiene del repositorio (secretos, llaves, licencias) | 2 cumplen, 2 no verificables | cada cambio |
| Piezas base (AC-BASE) | Paridad de decimales y redondeo con Dart, independencia de la región del equipo, vectores del legacy, una sola fuente de colores (122 tokens) con control de contraste, tiempo y azar controlables en las pruebas, trazabilidad y proyecto Unity preparado | 10 de 10 | base de todo |

Además, los **19 casos de prueba del legacy** (`rowing_simulator_test` 7, `heart_rate_parser_test` 6, `scan_filter_test` 2, `routine_positions_test` 1, `session_stats_test` 3) están portados uno por uno, con su nombre original y los mismos valores, y pasan.

---

## 3. Cómo ver la demo

Todo corre en tu PC con lo que ya tenés instalado (.NET 8). La primera vez necesita internet para bajar las bibliotecas de prueba; después, no.

Abrí una terminal (PowerShell o cmd) y pará en el proyecto:

```
cd <repo>
```

**1. Todas las pruebas**, el mismo comando que corre el CI:

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1
```

Primero compila los 9 proyectos y después corre las pruebas. Al final tenés que ver dos líneas `Passed!`: 69 pasan (simulador) y 825 pasan con 18 ignoradas (el resto). Si ninguna línea dice `Failed!`, todo está verde.

**2. La demo**: los números que se pueden mostrar:

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Demo
```

Busca las líneas que empiezan con `[DEMO]`:

- Los 8 paquetes de referencia del monitor, decodificados. Por ejemplo, `[DEMO] FTMS 2C 00 30 2A 01 E2 01 00 85 00 96 00 -> spm=24.0 paladas=298 m=482 split=133 (02:13) W=150 …`. El paquete de 1 byte da `sin lectura`.
- El simulador a 150 W y 24 spm, sin ruido, en el segundo 60: `m=226`, `paladas=24`, `kcal=13`, `bpm=142`.
- Los resúmenes de las 9 rutinas de ejemplo en es y en: `1 serie · 1 descanso` / `1 interval · 1 rest`.
- El P99: `W=198 spm=29.8 split=198 (03:18)`.

Hoy cada línea `[DEMO]` sale **dos veces**. Es un detalle de la salida, no un error (QA-V-07).

**3. Las pruebas de un criterio o de un caso del legacy:**

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Filter "TestCategory=AC-BLE-02-02"
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Filter "TestCategory=LT-16"
```

**4. La matriz criterio → prueba → resultado:**

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix
```

Escribe `tools\dotnet\.artifacts\TestResults\ac-matrix.md` con los 158 IDs (139 criterios y 19 casos legacy).

**5. El proyecto Unity**, listo para abrir:

```
type ProjectSettings\ProjectVersion.txt
dir Assets\RowMate
```

El primero muestra `m_EditorVersion: 6000.3.27f1`. El segundo muestra las capas: `Core`, `App`, `Infrastructure` y `Simulator` (lógica pura, la que se prueba sin Unity), `Platform`, `Presentation`, `Scene`, `SimulatorPresentation` y `Bootstrap` (lo que usa Unity), `Editor`, `Localization` (los dos catálogos) y `Tests`.

Si Windows contesta que la ejecución de scripts está deshabilitada, agregá `-ExecutionPolicy Bypass` después de `-NoProfile`. Solo vale para esa corrida.

**El CI en GitHub** no se puede mostrar: el proyecto no tiene repositorio remoto (ni commits). QA revisó que el workflow corre el mismo comando del punto 1 (ver P-03 en §8).

---

## 4. Resultado de las pruebas

Son los números de la corrida real de QA (05-qa-verification §1):

| Qué | Resultado |
|---|---|
| Compilación | 9 proyectos, **0 advertencias, 0 errores** (Debug y Release) |
| Pruebas | **912**: **894 pasan, 0 fallan, 18 ignoradas**. Son 843 generales (825 pasan y 18 ignoradas) y 69 del simulador (69 pasan) |
| Las 18 ignoradas | Son a propósito. Cada una está marcada "NO VERIFICABLE → It. N" porque necesita Unity Editor, un equipo, una Mac o GitHub (por ejemplo `ProjectOpensWithoutCompileErrors`, `InstallsNextToTheLegacyApp`, `RunsOnGitHub`). No son fallas |
| Matriz (158 IDs) | **157 pasan, 0 fallan, 1 ignorado y 0 sin prueba**. El ignorado es AC-APP-09-07 (teclado en el idioma del sistema), que solo se puede ver en un equipo |
| Casos del legacy | **19 de 19** portados con el mismo nombre y los mismos valores |
| Vectores del legacy | QA los volvió a generar con Dart 3.13.5 sobre el legacy y dieron **idénticos byte a byte** a los guardados (8 archivos) |
| Legacy | Intacto: `git status` vacío antes y después. Nadie leyó ni copió el archivo de credenciales de Strava |
| Otras corridas | `-Demo` (las líneas de §3), `-Ci` con los controles de higiene (20 de 20) y el filtro por ID: todo en verde |

---

## 5. Criterios de aceptación

| Área | Criterios | Cumplen | No verificables |
|---|---|---|---|
| BLE (remo, FTMS, filtro, simulador) | 37 | 37 | 0 |
| HRM (pulsómetro) | 9 | 9 | 0 |
| RUT (pasos, rutinas, sesión, estadísticas) | 58 | 58 | 0 |
| APP (idioma, textos, identidad, CI) | 25 | 17 | 8 |
| BASE (piezas base) | 10 | 10 | 0 |
| **Total** | **139** | **131** | **8** |

### 5.1 Los 8 no verificables y por qué

En todos, QA revisó la parte que se puede revisar sin Unity (configuración, textos y reglas) y está bien. Lo que falta es verlo de verdad.

| Criterio | Qué falta ver | Por qué no se pudo | Cuándo |
|---|---|---|---|
| AC-APP-09-06 | Que el idioma se elija con la lista de idiomas que da el sistema del equipo | Necesita un equipo con la app instalada | It. 2 (Windows/Android); iOS/macOS en la 10b |
| AC-APP-09-07 | Teclado y menú de copiar y pegar en el idioma del sistema (diferencia aceptada, ver §6) | El primer campo de texto llega con el editor de rutinas | It. 3 |
| AC-APP-16-01 | Que el equipo muestre el nombre `RowMate`, versión 1.0.0 | Necesita Unity y un build | It. 2; Apple en la 10b |
| AC-APP-16-03 | Que la app nueva se instale **al lado** del legacy sin pisarlo ni tocar sus datos | Necesita un build y un equipo con el legacy instalado | It. 2 (Windows/Android); Apple en la 10b |
| AC-APP-16-04 | Que en Android instale desde 8.0 y solo en 64 bits | Necesita un build y un teléfono | It. 2 |
| AC-APP-16-05 | iOS 15 como mínimo | Necesita una Mac | It. 10b |
| AC-APP-19-01 | Que el CI corra en GitHub en cada cambio, en Windows y Linux | No hay repositorio remoto (P-03) | Cuando subas el proyecto |
| AC-APP-19-02 | Que el CI quede en rojo si una prueba falla | Ídem | Cuando subas el proyecto |

### 5.2 Cumplen, pero una parte se ve más adelante

Pasan en la It. 1, y su parte visual o "dentro de Unity" se comprueba después. Si ahí falla, el criterio se reabre en esa iteración.

| Criterio | Parte pendiente | Cuándo |
|---|---|---|
| AC-BLE-01-05 | Los textos en la grilla de Dispositivo | It. 2 |
| AC-APP-09-04, -05 | Idioma en pantalla y cambio sin reiniciar | It. 2 |
| AC-APP-16-02 | Carpeta de datos de Windows y convivencia con el legacy | It. 2 |
| AC-BASE-01, -08 | Las mismas pruebas dentro de Unity (incluidas las del simulador, ver QA-V-05) | It. 2 |
| AC-BASE-10 | Abrir el proyecto sin errores y que la preparación automática se aplique | It. 2 |
| AC-RUT-01-04, AC-RUT-04-06 | Íconos y subtítulos en el editor de rutinas | It. 3 |
| AC-RUT-05-03 | El diálogo de inicio de rutina | It. 4a |
| AC-BASE-05 | Contraste del vidrio del HUD como lo dibuja Unity de verdad (riesgo R-T1, §7.3) | Se mide en la It. 2 y se decide antes de la 4a |
| AC-BLE-03-01 | El filtro con Bluetooth real | It. 10a / 11; Apple en la 10b |

---

## 6. Desvíos aceptados

Diferencias con el plan o con el legacy que se aceptaron con motivo. Ninguna cambia un número ni un texto del legacy.

| ID | Qué cambia | Por qué | Efecto para vos |
|---|---|---|---|
| D-02 | En Windows la app **no tiene identificador** tipo `com.iguisoft.rowmate`. Es "RowMate" de "iguisoft" y guarda sus datos en `AppData\LocalLow\iguisoft\RowMate` | Unity no permite uno propio para Windows. `com.iguisoft.rowmate` queda reservado para el instalador MSIX de la It. 14 | Ninguno: igual convive con el legacy, que usa otras carpetas |
| P-06 | El teclado y su menú de copiar y pegar salen en el idioma **del sistema**, no en el de la app | Así funciona Unity: esos textos los pone el sistema | Solo se nota con el sistema en un idioma que no es español ni inglés. Lo confirmás en el gate de la It. 2 (alternativa: una barra de edición propia en la It. 3) |
| D-06 | El idioma se elige con la regla exacta de Flutter, incluidos dos casos raros (`fr-FR, es-AR, en` → inglés) | Paridad | Ninguno (ver P-01 en §8 si preferís simplificarla) |
| D-12 | El punto de telemetría exige el tiempo transcurrido | Paridad con el legacy | Ninguno |
| D-09 | Los íconos (como dibujo) y la paleta por métrica pasan a la It. 2. En la It. 1 el ícono de cada tipo de paso queda como dato | La fuente de íconos se descarga en la It. 2 | Ninguno |
| Diseño | El rojo de los textos de error pasa de `#FF6B6B` a `#FF7272` (mismo tono) | No llegaba al contraste mínimo sobre los diálogos | Más legible. No toca ningún color del legacy |
| P-04 (agendada) | Windows mínimo 10 21H1 y macOS mínimo 12, por Unity 6.3 | Lo impone el motor | Se pierden Windows 10 anteriores a 21H1 y macOS 10.15 y 11. Se decide en el gate de la It. 2 (Windows) y de la 10b (macOS) |

También se aceptaron desvíos internos sin efecto visible: D-01 (dónde se guardan los vectores), D-03 (nombre del registro de diagnóstico), D-04 y D-10 (controles como pruebas), D-05 (conversión de textos), D-07 y D-08 (dependencias que llegan en la It. 3), D-11 (fechas con zona horaria), D-13 y D-14 (02-tech-design §10.2).

---

## 7. Issues abiertos y deuda

### 7.1 Issues de QA (todos menores)

| ID | Qué pasa, en simple | Impacto | Quién | Cuándo |
|---|---|---|---|---|
| QA-V-01 | Si una parte de la app falla al recibir un cambio de estado (por ejemplo "conectado"), las demás partes que esperaban ese aviso no se enteran | Ninguno hoy. En la It. 2, una vista rota podría dejar a otra congelada | Architect | **Al empezar la It. 2**, antes de usarlo |
| QA-V-02 | La protección contra subir credenciales de Strava cubre solo un tipo de archivo, y el control del CI avisa recién **después** de subir | Ninguno hoy (no hay credenciales ni repositorio remoto) | Architect | **Antes del primer push a GitHub**, y a más tardar en la It. 12 |
| QA-V-03 | Si un paso de la preparación automática del proyecto falla, se repite cada vez que Unity recompila, en lugar de una vez | Molestia en el primer open | Architect | Al empezar la It. 2 |
| QA-V-04 | Un nombre interno del código del Editor choca con uno de Unity y va a dar error cuando se agreguen herramientas de Editor | Ninguno hoy | Architect | Al empezar la It. 2 |
| QA-V-05 | Dentro de Unity, las 69 pruebas del simulador no aparecen por defecto (el simulador está apagado a propósito). Falta el procedimiento para correrlas | Hace falta para AC-BASE-08 | Architect | It. 2 |
| QA-V-06 | La prueba de "textos idénticos al legacy" compara el texto guardado y no el que se muestra. Hoy coinciden | Protección más débil a futuro | Architect | It. 2, antes de mostrar textos |
| QA-V-07 | La demo imprime cada línea `[DEMO]` dos veces | Cosmético | Architect | Cuando convenga |
| QA-V-08 | Dos documentos de arquitectura (ADR 011 y 012) conservan en su texto principal una versión vieja que contradice su propia tabla de cambios | Solo documental | Architect | Al empezar la It. 2 |
| QA-V-09 | La biblia de arte (§9) conserva una "diferencia abierta" que ya está resuelta | Solo documental | Designer | Al empezar la It. 2 |
| QA-V-10 | El inventario decía `com.iguisoft.rowmate` "en Android y Windows" | — | PO | **Resuelto al cerrar:** FEATURE_INVENTORY Q-85 y R-14 ya dicen lo mismo que AC-APP-16-02 |
| QA-V-11 | El legacy acepta números gigantes en los pasos (miles de millones de vatios) y la app nueva calcula hasta 2 147 483 647. Con valores absurdos, la expansión de la rutina falla | Ninguno en un uso real | PO | Decisión nueva **Q-92** (§8). Se fija en la spec de la It. 3 |

### 7.2 Deuda documental que encontró el PO al cerrar

- **ART_BIBLE**: además de QA-V-09, todavía no tiene los cambios de [04-design-notes §6](04-design-notes.md): `state.dangerText` `#FF7272`, `num.band`, `focus.ringHalo`, `state.connectedBg` / `warningBg`, `dev.simulatorText` / `bleDebugBg` y los nombres de espaciado. La fuente que manda (`Tokens.cs`) ya los tiene, así que no afecta las pruebas. Responsable: Designer, al empezar la It. 2.
- **Íconos**: sigue abierto el punto de [ASSET_SLOTS §4](../../design/ASSET_SLOTS.md) (glifos de fuente o SVG). Lo cierran el Architect y QA antes del diseño de la It. 2.

### 7.3 Riesgos que pasan a la It. 2

| ID | Riesgo | Qué se hace |
|---|---|---|
| R-T1 | Unity puede mezclar la transparencia del vidrio del HUD de una forma que baja el contraste por debajo de lo prometido (R-10) | En la It. 2 se mide con una captura. Si pasa, se aplica la opción que recomienda el Designer antes de la It. 4a |
| R-T2 | La configuración del proyecto Unity está escrita a mano y nunca se abrió | Primer open al empezar la It. 2. Cada paso de la preparación tiene su respaldo manual |
| R-T5 / R-T6 | Las pruebas podrían comportarse distinto dentro de Unity o en el build del teléfono | Se corren en el Editor y en un build de prueba en la It. 2 |
| R-T12 | Unity podría no aplicar un ajuste de avisos del compilador | Solo avisos. Se corrige en la It. 2 si pasa |

---

## 8. Decisiones que tenés que confirmar

Si no decís nada, se aplica lo que dice "Por defecto". Alcanza con un "OK a todo", o con decir cuál querés cambiar.

### 8.1 Gate de la It. 1

| Decisión | Qué significa | Por defecto |
|---|---|---|
| **Q-01** | Los resúmenes y las etiquetas de rutina, que en el legacy salen en español aunque la app esté en inglés, pasan al catálogo. En español quedan **idénticos** y en inglés se traducen (15 textos nuevos) | Sí |
| **Q-14** | Lo que la app llama "promedio" de vatios, SPM y split no es un promedio: es el percentil 99 (casi el máximo de vatios y, en el split, casi el más lento), como en el legacy. Así el historial y Strava dan los mismos números que hoy. Se puede revisar después de la paridad | Sí (igual que el legacy) |
| **Q-90** | Android mínimo 8.0 y solo teléfonos de 64 bits. Se pierden Android 7.x y los equipos de 32 bits, que el legacy sí soporta | Sí |
| **R-14 / Q-85** | Identificadores provisionales para que la app nueva conviva con la vieja: Android `com.iguisoft.rowmate`; iOS y macOS `com.iguisoft.rowmate.unity` (el legacy usa `com.iguisoft.rowmate` en Apple y se pisarían); Windows sin identificador (D-02), con datos en `LocalLow\iguisoft\RowMate`. Los definitivos se deciden en la It. 14 | Sí |

### 8.2 Nuevas o sin urgencia

| Decisión | Qué significa | Por defecto |
|---|---|---|
| **Q-92** (nueva, de QA-V-11) | Tope para los números de un paso y sus progresiones. El editor de la It. 3 no deja guardar valores absurdos que la app no puede calcular (miles de millones de vatios) y avisa con un mensaje. Debajo del tope todo es igual al legacy, sin otras validaciones | Sí. Alternativa: el mismo rango que el legacy. Se cierra en el gate de la It. 3 |
| **P-01** | La regla de idioma del legacy tiene dos casos raros: con el sistema en `francés, español (Argentina), inglés`, la app sale en inglés y no en español. ¿Se mantiene o se simplifica a "el primer idioma soportado de la lista"? | Se mantiene (paridad) |
| **P-03** | ¿Creás un repositorio en GitHub y subís el proyecto? Sin eso, el CI no corre (AC-APP-19-01 y -02 quedan no verificables) | Opcional. Antes de subirlo conviene resolver QA-V-02 |

### 8.3 Para ir pensando (se deciden en el gate de la It. 2)

- **P-04 (Windows)**: Windows 10 21H1 o más nuevo. Por defecto se adopta, porque lo impone Unity.
- **P-06**: teclado y copiar y pegar en el idioma del sistema. Por defecto se acepta. La alternativa es una barra de edición propia en la It. 3.

---

## 9. Prerrequisitos para la It. 2

### 9.1 De tu lado

| Qué | Para qué | ¿Obligatorio? |
|---|---|---|
| **Unity Hub** con el Editor **6000.3.27f1** exacto (Unity 6.3 LTS) y los módulos *Android Build Support* (con *OpenJDK* y *Android SDK & NDK Tools*), *Windows Build Support (IL2CPP)*, *iOS Build Support* y *Mac Build Support* | Ver cualquier pantalla y abrir el proyecto por primera vez. Con otra versión, Unity puede reescribir la configuración | **Sí** |
| **Visual Studio 2022** con "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621 o más nuevo | Compilar el build de prueba de Windows (IL2CPP) | Sí, para el build de prueba |
| **git** disponible en la terminal | Unity baja un paquete (la base de datos) desde git en el primer open | Sí (ya está instalado) |
| **Fuentes** Inter, Barlow Semi Condensed y JetBrains Mono, e **íconos** Material Symbols Rounded, con los pasos de [ASSET_SLOTS §3-§4](../../design/ASSET_SLOTS.md). Para los íconos hacen falta Python 3 y `fonttools` | Tipografía e íconos definitivos | No: sin ellas la demo se ve "a medio vestir" |
| Un **teléfono Android 8.0 o más nuevo, de 64 bits**, con depuración USB | Ver el nombre, el mínimo de Android y la convivencia en un teléfono (AC-APP-16-01, -03 y -04) | No: sin teléfono, esos criterios siguen no verificables hasta que haya uno (a más tardar en la It. 6a, donde ya hace falta) |
| La **app legacy instalada y con datos** en esa PC (y en el teléfono, si hay) | Comprobar que la nueva se instala al lado sin tocarla (AC-APP-16-03) | Recomendado |
| Repositorio en **GitHub** (P-03) y licencia Personal de Unity cargada como secreto | CI de las pruebas y, con la licencia, CI de Unity | No |

### 9.2 Del equipo, al empezar la It. 2

1. **Architect**: resolver QA-V-01, QA-V-03, QA-V-04, QA-V-05 y QA-V-06 antes de construir encima. Corregir el texto de ADR 011 y 012 (QA-V-08). QA-V-02, antes del primer push.
2. **Designer**: alinear ART_BIBLE (QA-V-09 y §7.2) y, con el Architect y QA, cerrar íconos (fuente o SVG).
3. **Primer open del proyecto** en 6000.3.27f1: abre sin errores, la preparación automática aplica sus pasos y las 912 pruebas corren en el Test Runner de Unity, incluidas las del simulador.
4. **PO**: spec de la It. 2 (`iterations/02-dispositivo-simulador/01-po-spec.md`), con los criterios que la It. 1 dejó para verificar en pantalla y en equipo (§5).

---

## 10. Por qué el PO acepta la It. 1

- Lo que la It. 1 tenía que lograr se cumple: cada regla del legacy que entraba (lectura del remo y del pulsómetro, filtro, física del simulador, pasos, rutinas, series, sesión, telemetría, estadísticas y catálogo es/en) está reproducida y probada contra el legacy mismo. Ninguna prueba falla, los 139 criterios tienen prueba y los 19 casos del legacy pasan con sus valores originales.
- Los 8 no verificables dependen de Unity Editor, de un equipo, de una Mac o de GitHub. La spec ya preveía que en la It. 1 se revisaran de forma estática, y QA no encontró defectos. Se verifican en las iteraciones indicadas en §5.
- Los 11 issues son menores y ninguno cambia un número ni un texto que vaya a ver el remero. Los que pueden afectar a la It. 2 (QA-V-01, -03, -04, -05 y -06) quedan como primera tarea de esa iteración.
- **Lo único que falta para cerrar del todo es tu OK** a las decisiones de §8.1.
