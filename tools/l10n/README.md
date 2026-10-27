# tools/l10n — conversión única de los catálogos

Los textos de la app viven en `Assets/RowMate/Localization/strings.es.json` y `strings.en.json` (F-APP-10, ADR 011). En la It. 1 se generaron **una sola vez** desde los `.arb` del legacy; desde entonces **se mantienen a mano** (cada iteración agrega sus claves en los dos archivos y en `Assets/RowMate/Core/Localization/L10nKeys.cs`).

## Qué hay acá

| Archivo | Qué es |
|---|---|
| `arb_to_catalog.dart` | El conversor (Dart, solo `dart:*`, sin importar código del legacy). Lee `lib/l10n/app_es.arb` y `app_en.arb` del legacy en solo lectura, aplica `it01-changes.json` y escribe los dos catálogos |
| `it01-changes.json` | Los cambios de la It. 1: las 4 claves que cambian (01-po-spec §4.17.1, Q-02) con el texto legacy esperado (`from`) y el nuevo (`to`), y las 15 claves nuevas (§4.17.2, Q-01 + Q-02) con sus textos es/en y el **tipo** de cada parámetro (02-tech-design §3.3, QA-07) |

## Cómo se corre

Siempre a través del oráculo, para que la lectura del legacy quede dentro de su guardia (02-tech-design §6.1: `git status --porcelain` vacío, listado `--ignored` y metadatos de archivos iguales antes y después):

```
powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Only l10n_vectors -L10n
```

`-L10n` invoca `arb_to_catalog.dart --legacy <rowmate-legacy> --out Assets/RowMate/Localization --changes tools/l10n/it01-changes.json`. Si el texto legacy de una clave que cambia no es el `from` esperado, o una clave nueva ya existe en el legacy, el script aborta sin escribir.

> Volver a correrlo **pisa** los catálogos. Después de la It. 1 solo tiene sentido para reconstruirlos desde cero; los cambios normales se hacen a mano en los JSON.

## Formato de salida

El de los `.arb`, para que la diferencia con el legacy se lea a simple vista:

```json
{
  "@@locale": "es",
  "hrmConnectingTo": "Conectando a {name}...",
  "@hrmConnectingTo": {
    "placeholders": {
      "name": {
        "type": "String"
      }
    }
  },
  "routineSummarySteps": "{count, plural, one{# paso} other{# pasos}}",
  ...
}
```

- `@@locale` primero; las 159 claves del legacy en su orden y las 15 nuevas al final.
- `@clave` con `placeholders` (tipos `String` o `int`) **justo después** de cada clave con parámetros, y de ninguna otra (en el `.arb` legacy `@profileSyncResult` estaba más abajo: acá se normaliza).
- UTF-8 sin BOM, LF, 2 espacios de sangría, salto de línea final.
- Sintaxis de mensajes: la de `MessageFormatter` (`{x}`, `{x, plural, =N{…} one{…} other{…}}` con `#`; sin escape con apóstrofos).

## Quién lo verifica

No el script: los tests de `dotnet` (`Tests/EditMode/Localization/`), que comparan los catálogos con la instantánea `Assets/RowMate/Tests/Golden/oracle/l10n_legacy.json` (generada por `tools/oracle/bin/l10n_vectors.dart`) y con los textos y tipos de la spec:

- `CatalogParityTests`: 174 claves iguales en es y en; las 155 sin cambio idénticas al legacy carácter por carácter; las 4 que cambian; las 21 claves con parámetros (nombre y tipo); las 15 nuevas; ninguna clave legacy perdida.
- `L10nKeysTests`: cada constante de `L10nKeys` existe en los dos catálogos y cada clave tiene su constante.
- `LocalizationSet.Load` (y `LocalizationCatalog.Parse`) fallan al cargar si un mensaje no se puede analizar, si un argumento no tiene metadato (o al revés), si un plural no es `int` o si los dos idiomas no tienen las mismas claves y parámetros.

## Nota sobre la guardia R-02

`run-oracle.ps1` aborta si algún `.dart` de `tools/oracle/` o `tools/l10n/` menciona el nombre del servicio de subida de actividades (el del módulo de credenciales del legacy). Los scripts de esta carpeta no lo nombran: leen y escriben los textos como datos.
