# ADR 012: Paridad numérica Dart → C# (redondeo, formato, módulo, RNG, fechas, cultura)

- **Estado:** Aceptado (2026-10-27). Revisado por el gate de QA del plan (QA-P-02, QA-P-23).
- **Ámbito:** todo Core y App (R-13)

## Contexto

Los números del legacy son el contrato. Dart y C# difieren en detalles que cambian lo que se ve y lo que se calcula:

- `x.toStringAsFixed(n)` de Dart redondea el **valor binario exacto** del `double` y, si cae justo en la mitad, desempata **alejándose del cero**. Ni `ToString("F1")` ni `Math.Round(x, MidpointRounding.AwayFromZero)` de .NET 8 lo reproducen (QA §3.2): `0.25 → "0.3"` en Dart y `"0.2"` en .NET; `0.35 → "0.3"` en Dart y `0.4` con `Math.Round`; `2.5 → "3"` en Dart y `"2"` en .NET; `24.45 → "24.4"` en Dart y `24.5` con `Math.Round`. Además el formateo de Mono/IL2CPP no es idéntico al de .NET 8, donde corren los tests.
- `x.round()` devuelve un **`int`** (nunca `-0`); `%` da un resultado no negativo; `int` es de 64 bits.
- El valor emitido por el simulador ya pasa por texto: `double.parse(spm.toStringAsFixed(1))` (`lib/core/dev/rowing_simulator.dart:60`).
- `math.Random(seed)` es otro algoritmo que `System.Random`; `toIso8601String()` emite microsegundos; el formato decimal depende de la cultura del dispositivo.

## Decisión

### 1. `Fmt.Fixed(double x, int n)` = `toStringAsFixed(n)` de Dart

1. `n` en `[0; 20]`; fuera de rango, `ArgumentOutOfRangeException` (como el `RangeError` de Dart).
2. `NaN` → `"NaN"`, `+∞` → `"Infinity"`, `−∞` → `"-Infinity"`. Con `|x| ≥ 1e21` Dart devuelve `toString()` (notación exponencial): se delega en `Fmt.DartToString(x)` (§3). Ningún valor de la app llega ahí.
3. **Descomposición exacta** del `double` (bits IEEE 754 con `BitConverter.DoubleToInt64Bits`): signo `s`, mantisa entera `m` (con el bit implícito) y exponente `e`, de modo que `|x| = m·2^e` **sin error**.
4. **Escalado exacto**: `N = m·10^n·2^e`. Si `e ≥ 0`, `N` es entero. Si `e < 0`, `q = ⌊m·10^n / 2^(−e)⌋` y resto `r`; si `2r ≥ 2^(−e)`, `q = q + 1`. Decide el valor binario exacto, no su texto, y el empate exacto sube (se aleja del cero porque se trabaja con `|x|`).
5. **Camino rápido sin memoria**: si `m·10^n` entra en `ulong` y `−e ≤ 63`, todo se hace con `ulong` y desplazamientos (cubre todos los números de la app: `n ≤ 3` y magnitudes de métricas). Si no, `System.Numerics.BigInteger` (está en .NET Standard 2.1 y es compatible con IL2CPP/AOT).
6. **Texto**: dígitos de `q` en cultura invariante, con ceros a la izquierda hasta `n + 1` dígitos, punto antes de los últimos `n` (si `n > 0`) y `-` adelante si el **bit de signo** está prendido. Así `-0.04 → "-0.0"`, `-0.0 → "-0"` (n = 0) y `-0.05 → "-0.1"`, igual que Dart.
7. **Sobrecarga sin asignaciones**: `bool Fmt.TryFormatFixed(double x, int n, Span<char> destination, out int charsWritten)`. La usan `TabularNumber` (ARCHITECTURE §7.8) y los formatos del HUD: 0 B por cuadro.

### 2. `DartMath.Round(double x) : long` = `double.round()`

- `NaN` o `±∞` → `ArgumentException` (Dart lanza `UnsupportedError`).
- `|x| ≥ 2^52` → `(long)x` (ya es entero).
- Si no: `t = Math.Truncate(x)`, `f = x − t` (resta exacta para `|x| < 2^52`); si `|f| ≥ 0,5`, `t + sign(x)`. Devuelve `long`: nunca hay `-0` (`(-0.05).round() == 0`).
- `DartMath.RoundToFixed(double x, int n) : double` = `double.parse(x.toStringAsFixed(n))`: `±q / 10^n` con el `q` del paso 1.4. Es una división IEEE de dos valores exactos (`q < 2^53`, `n ≤ 22`), así que da el mismo `double` que el parseo correctamente redondeado, sin pasar por texto.
- **Prohibido** `Math.Round` y `ToString("F…")` en `Core` y `App` (regla de QA y chequeo del CI, ARCHITECTURE §19).

### 3. Resto de la paridad

| Dart | C# |
|---|---|
| `a % b` | `DartMath.Mod(a, b)`: `r = a % b; si r < 0 → r + abs(b)` (oráculo: `(-7.5) % 4096.0 = 4088.5`, `(-7) % 3 = 2`) |
| `a ~/ b` | `DartMath.TruncDiv(a, b) : long` (`/` entero de C# ya trunca) |
| `'$x'` / `x.toString()` con `double` | `Fmt.DartToString(x)`: entero con `abs(x) < 1e21` → `"24.0"`; si no, el texto más corto que vuelve al mismo `double`, calculado como `BignumDtoa` de Dart en modo SHORTEST (QA-04: para cada cantidad de cifras, piso y techo; los que entran en el intervalo; el más cercano; empate exacto → par), normalizado a las reglas de Dart (`1e-7`, `0.000001`, `1e+21`, `-0.0`) |
| `toUtc().toIso8601String()` | `IsoDate.FormatUtc`: `yyyy-MM-ddTHH:mm:ss.mmm` + `uuu` **solo si los microsegundos no son 0** + `Z`; `IsoDate.FormatLocal`, la hora de pared sin zona. `IsoDate.Parse(texto, zona)` sigue la gramática de `DateTime.parse` de Dart (0 a 7 decimales, con o sin `Z` u offset; D-11) |
| cultura | Core no depende de la cultura del dispositivo: todo texto numérico sale de `Fmt` o usa `InvariantCulture` explícito (lo verifican `CultureIndependenceTests` y `PureCodeRulesTests`). Además, `AppRoot` fija `InvariantCulture` al arrancar (ARCHITECTURE §4.1) como defensa para el código de Unity que no es Core |
| `int` | `long` para epoch en ms y acumulados que puedan pasar 2³¹ |
| `math.Random(seed)` | `DartRandom`: puerto del algoritmo de la VM de Dart (BSD-3, con atribución) donde el resultado se ve (estrellas, orillas). Vectores de la It. 6a. `IRandom` inyectable en el simulador desde la It. 1 |
| `pow`, `sin`, `cbrt` | Tolerancia `Within(1e-9)` salvo donde el legacy compara exacto |

### 4. Vectores dorados con el oráculo Dart (QA-P-23)

- `tools/oracle/` contiene scripts Dart (`bin/*.dart` + `lib/golden.dart`) que se corren con el SDK instalado (`<flutter-sdk>/bin/cache/dart-sdk/bin/dart.exe`, Dart 3.13.5) a través de `tools/oracle/run-oracle.ps1`. Importan **por ruta relativa y en solo lectura** los archivos puros del legacy (los que solo importan `dart:`: `core/models/*`, `ftms_parser`, `heart_rate_parser`, `rowing_simulator`) y escriben `Assets/RowMate/Tests/Golden/oracle/*.json` (versionado, junto a los tests que lo usan; D-01), con un encabezado `{ dartVersion, legacyCommit, generatedAt }`.
- Nunca se corre `pub get` ni nada dentro del legacy: `run-oracle.ps1` arma un `package_config` propio en `tools/oracle/.dart_tool/` (sin `pubspec`). Antes y después de cada generación vigila el legacy: `git status --porcelain` vacío, listado `--ignored --untracked-files=all` idéntico y metadatos de archivos sin abrirlos (QA-14).
- Los tests de `dotnet` (y los de EditMode en Unity) leen el JSON con `RepoPaths`. El CI no necesita Dart.
- Lo que depende de Flutter (`shared/theme.dart`, `series_panels.dart`) necesita `flutter test` en un paquete aparte con dependencia por ruta; se agrega cuando una iteración lo necesite (la It. 4a, para los formatos de los paneles).
- Vectores de la It. 1: la tabla de abajo, la tabla de QA §3.2, etiquetas de duración y objetivos, resúmenes de rutina, P99 y pulso promedio, los 8 paquetes FTMS, los formatos de `RowingData` y el simulador sin ruido.

### 5. Vectores verificados con el oráculo (Dart 3.13.5, 2026-10-27)

| Entrada | Dart |
|---|---|
| `toStringAsFixed`: `0.15`/1, `0.25`/1, `0.35`/1, `0.45`/1, `24.45`/1, `99.95`/1 | `0.1`, `0.3`, `0.3`, `0.5`, `24.4`, `100.0` |
| `toStringAsFixed`: `2.5`/0, `0.5`/0, `1.5`/0, `-2.5`/0, `1.005`/2, `123.456`/2 | `3`, `1`, `2`, `-3`, `1.00`, `123.46` |
| `toStringAsFixed`: `-0.05`/1, `-0.04`/1, `-0.0`/1, `-0.0`/0, `0.0001`/3, `5e-324`/3 | `-0.1`, `-0.0`, `-0.0`, `-0`, `0.000`, `0.000` |
| `toStringAsFixed`: `1e20`/2, `1e21`/2, `NaN`/1, `Infinity`/1 | `100000000000000000000.00`, `1e+21`, `NaN`, `Infinity` |
| `round()`: `0.5`, `1.5`, `2.5`, `-0.5`, `-1.5`, `-0.05`, `0.49999999999999994`, `4503599627370495.5`, `-0.0` | `1`, `2`, `3`, `-1`, `-2`, `0`, `0`, `4503599627370496`, `0` (todos `int`) |
| `toString()`: `24.0`, `0.1`, `1e21`, `1e-7`, `0.000001`, `-0.0`, `100/3` | `24.0`, `0.1`, `1e+21`, `1e-7`, `0.000001`, `-0.0`, `33.333333333333336` |
| `double.parse(x.toStringAsFixed(1))`: `24.25`, `24.35` | `24.3`, `24.4` |
| `toIso8601String()` UTC con 628 ms + 537 µs, con 628 ms, con 0 ms | `…38.628537Z`, `…38.628Z`, `…38.000Z` |
| `Random(42)`: `nextDouble()` × 2, `nextInt(1000)` | `0.15092545597797424`, `0.6041479725361217`, `77` |

## Alternativas descartadas

- **`Math.Round` y `ToString("F1")`**: fallan en los vectores de arriba (QA §3.2) y además difieren entre .NET 8 y Mono/IL2CPP.
- **`decimal`**: la conversión `(decimal)x` redondea a 15 cifras y pierde el valor binario exacto (`0.35` pasaría a `0.35m` y daría `"0.4"`).
- **Portar Grisu/Ryu para `toString()`**: demasiado código para el único uso (interpolaciones de `double`); la verificación de ida y vuelta alcanza y los vectores dorados la controlan.
- **RNG propio cualquiera** (permitido por Q-61): válido, pero `DartRandom` deja el paisaje idéntico (Q-61 revisada).

## Consecuencias

- Regla de QA: ningún formateo visible fuera de `Fmt`/`WorkoutFormats`; `Math.Round` y `ToString("F` prohibidos en `Core` y `App` (chequeo del CI).
- El camino `BigInteger` reserva memoria; nunca se usa en el HUD (camino rápido).
- Los mismos vectores corren en `dotnet` y, con Editor, en EditMode; el *smoke test* IL2CPP de la It. 2 los corre también en un build.

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-02 | Algoritmo exacto de `Fmt.Fixed` (expansión del `double` con `ulong`/`BigInteger`, desempate alejándose del cero, `-0` como Dart), `DartMath.Round` devuelve `long`, `RoundToFixed` para `double.parse(toStringAsFixed)`, sobrecarga sin asignaciones, prohibición en el CI |
| QA-P-23 | `tools/oracle` y JSON dorado desde la It. 1 (antes: captura en la It. 6). Vectores verificados con el oráculo en §5 |
| QA-P-19 (E3) | `IsoDate.Format` emite microsegundos como Dart; `Parse` acepta 0 a 7 decimales |

## Cambios de la It. 1 (implementación, WP-A)

| ID | Qué cambió |
|---|---|
| D-01 | §4: los JSON dorados van a `Assets/RowMate/Tests/Golden/oracle/` (no a `tools/oracle/golden/`). `tools/oracle/run-oracle.ps1` arma un `package_config` propio en `tools/oracle/.dart_tool/` (sin `pubspec` ni `pub get`) y vigila el legacy antes y después: `status --porcelain` vacío, listado `--ignored --untracked-files=all` y metadatos de archivos sin abrirlos (QA-14) |
| QA-04 | `Fmt.DartToString` = `BignumDtoa` en modo SHORTEST: para cada cantidad de cifras, piso y techo; los que entran en el intervalo (asimétrico solo si `f == 0 && be > 1`); el más cercano; empate exacto → par. Reemplaza la "verificación de ida y vuelta" de §3. Verificado contra las 2 098 potencias de 2 y 500 empates de `fmt.json` |
| QA-06 | `fmt.json` en formato de tabla (`double` por bits): 683 549 bytes con `toStringAsFixed`, `round`, `parseFixed`, `toString`, `toStringPow2`, `toStringTies`, `mod`, `modInt`, `truncDiv`, `truncDivF64`, `iso8601`, `iso8601Local`, `isoParse`, `isoParseLocal`, `isoParseInvalid` |
| D-11 | `IsoDate` trabaja con `DateTimeOffset`: `FormatUtc` (con `Z`), `FormatLocal` (hora de pared, sin zona) y `Parse(texto, zona)` con la gramática exacta de `DateTime.parse` de Dart (incluye normalización de campos fuera de rango y truncado a microsegundos). Años fuera de 1-9999 → `FormatException` (Dart acepta el año 0; la app no los genera) |
| Implementación | Cultura: no se fuerza `DefaultThreadCurrentCulture`; todo texto numérico de Core usa `Fmt`/`InvariantCulture` explícito (lo verifican `CultureIndependenceTests` y `PureCodeRulesTests`) |

## Cambios de la It. 2 (deuda de la It. 1)

| ID | Qué cambió |
|---|---|
| QA-V-08 | El texto principal dice lo mismo que la tabla de cambios: §4 con la ruta de D-01 (`Assets/RowMate/Tests/Golden/oracle/`) y la guardia de `run-oracle.ps1`; §3 con el algoritmo de QA-04 para `toString()`, el `IsoDate` de D-11 y la regla de cultura de "Implementación" (Core no depende de la cultura; `AppRoot` igual fija `InvariantCulture`, ARCHITECTURE §4.1) |
