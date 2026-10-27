# ADR 011: Localización con catálogo JSON propio y formateador ICU mínimo

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** Core.Localization, datos en `Assets/RowMate/Localization/` (F-APP-09, F-APP-10, Q-01, Q-02)

## Contexto

El legacy tiene 159 claves `.arb` es/en con placeholders, elige el idioma por el sistema y cae en inglés. La It. 1 tiene que verificar con `dotnet` que las claves existen en ambos idiomas con los mismos parámetros. Cada iteración agrega claves nuevas (Q-01) y hacen falta plurales (Q-02). No hay Editor.

## Decisión

- Catálogos `strings.es.json` y `strings.en.json` con las mismas claves, placeholders y metadatos `@clave` que los `.arb` (convertidos una vez con `tools/l10n`).
- `Localizer` + `MessageFormatter` en Core: `{name}` y `{count, plural, =0{…} one{…} other{…}}`.
- `LocaleResolver.Resolve(lista)`: lo mismo que hace Flutter con `supportedLocales: [en, es]` (`main.dart:103-105`) sobre la **lista** de idiomas preferidos del sistema, es decir, el algoritmo `basicLocaleListResolution` de Flutter 3.47.6 (`widgets/app.dart:146-238`), no "el primero soportado" (D-06). Recorre la lista en orden: una coincidencia **perfecta** (`es` o `en` sin región ni escritura) gana en el acto. Una coincidencia **solo por idioma** (`es-AR`, `en-GB`) gana enseguida si es la **primera** preferencia (salvo que la segunda sea del mismo idioma); si aparece más adelante, se guarda y gana solo si la preferencia siguiente no coincide de forma perfecta. Por eso `[es-AR, en]` → `es` y `[fr-FR, es-AR, pt-PT, en]` → `es`, pero `[fr-FR, es-AR, en]` → `en` y `[fr-FR, en-GB, es]` → `es` (los casos raros de P-01, que se mantienen por paridad). Si nada coincide, o la lista es nula o vacía, `en`. Las 17 filas de AC-APP-09-02 están en `LocaleResolverTests`. La lista la da `ISystemLocale` (Android `LocaleList` por JNI, Windows `GetUserPreferredUILanguages`, Apple `Locale.preferredLanguages` en la It. 10b), porque `Application.systemLanguage` da solo el principal. Se vuelve a leer al recuperar el foco.
- Test de paridad del catálogo (mismas claves y parámetros en es y en) en `dotnet` y en Unity.
- Textos del simulador y del diagnóstico BLE quedan fijos (Q-03, Q-04).

## Alternativas descartadas

- **`com.unity.localization` 1.5.13**: las tablas se crean en el Editor (no hay), no se pueden verificar en `dotnet`, suma Addressables y Smart Strings para un catálogo chico.
- **Leer los `.arb` directamente**: Unity no importa esa extensión como `TextAsset`.

## Consecuencias

- Agregar un texto = agregar la clave en los dos JSON (el test lo exige).
- Si Unity 6.7 LTS trae localización integrada adecuada, se reevalúa en la It. 14.

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-20 | Resolución sobre la lista de idiomas preferidos, con lectura nativa por plataforma |

## Cambios de la It. 1 (diseño técnico)

| ID | Qué cambió |
|---|---|
| D-06 | `LocaleResolver` = algoritmo `basicLocaleListResolution` de Flutter 3.47.6 (`widgets/app.dart:146-238`) con `supportedLocales = [en, es]`, no "el primero soportado": una coincidencia perfecta (`es`/`en` sin región ni escritura) se revisa antes de devolver una coincidencia solo por idioma anterior (`[fr-FR, es-AR, en]` → `en`). Lista nula o vacía → `en` |
| D-05 | La conversión única `.arb` → catálogos es un script Dart (`tools/l10n/arb_to_catalog.dart`, corrido por `tools/oracle/run-oracle.ps1 -L10n` bajo la guardia del legacy); `L10nKeys` se mantiene a mano con un test de sincronía con los dos catálogos |
| D-08 | El catálogo se lee con el `JsonParser` estricto de Core (sin Newtonsoft) |

## Cambios de la It. 2 (deuda de la It. 1)

| ID | Qué cambió |
|---|---|
| QA-V-08 | El texto principal de la decisión (`LocaleResolver`) dice lo mismo que D-06: algoritmo `basicLocaleListResolution`, no "el primero soportado" |
| QA-V-06 | El test de textos idénticos al legacy compara además lo que ve el usuario (`Localizer.Get` y `Format`), no solo el patrón guardado (`CatalogParityTests.UnchangedLegacyTextsLookIdenticalOnScreen`) |
