// Conversión ÚNICA .arb del legacy → catálogos de la app nueva (02-tech-design §3.3, D-05).
//
//   dart tools/l10n/arb_to_catalog.dart --legacy <rowmate-legacy> --out <Assets/RowMate/Localization> --changes <it01-changes.json>
//
// Lo corre tools/oracle/run-oracle.ps1 -L10n, dentro de la guardia del legacy (§6.1). Solo importa dart:*:
// LEE <legacy>/lib/l10n/app_es.arb y app_en.arb (solo lectura), aplica it01-changes.json (las 4 claves que
// cambian y las 15 nuevas, con los tipos de sus parámetros) y ESCRIBE strings.es.json y strings.en.json.
// Después de la It. 1 los catálogos se mantienen a mano (ADR 011); la verificación no depende de este script:
// la hacen los tests de dotnet contra Assets/RowMate/Tests/Golden/oracle/l10n_legacy.json.
//
// Formato de salida (el de los .arb): "@@locale" primero; las claves del legacy en su orden y las nuevas al
// final; "@clave": {"placeholders": {"p": {"type": "String" | "int"}}} justo después de cada clave que tiene
// parámetros (y de ninguna otra). UTF-8 sin BOM, LF, sangría de 2 espacios, salto de línea final.
import 'dart:convert';
import 'dart:io';

const _locales = ['es', 'en'];
const _types = {'String', 'int'};

void main(List<String> args) {
  String? legacy;
  String? out;
  String? changesPath;
  for (var i = 0; i < args.length; i++) {
    final a = args[i];
    if (a == '--legacy' && i + 1 < args.length) {
      legacy = args[++i];
    } else if (a == '--out' && i + 1 < args.length) {
      out = args[++i];
    } else if (a == '--changes' && i + 1 < args.length) {
      changesPath = args[++i];
    } else {
      _fail('argumento desconocido: $a');
    }
  }
  if (legacy == null || out == null || changesPath == null) {
    _fail('uso: arb_to_catalog.dart --legacy <ruta> --out <carpeta> --changes <it01-changes.json>');
  }

  final changes = jsonDecode(File(changesPath!).readAsStringSync()) as Map<String, dynamic>;
  final changed = (changes['changed'] as List).cast<Map<String, dynamic>>();
  final added = (changes['added'] as List).cast<Map<String, dynamic>>();

  for (final locale in _locales) {
    final arbFile = File('${legacy!}/lib/l10n/app_$locale.arb');
    final arb = jsonDecode(arbFile.readAsStringSync()) as Map<String, dynamic>;
    if (arb['@@locale'] != locale) {
      _fail('${arbFile.path}: @@locale es ${arb['@@locale']}, se esperaba $locale');
    }

    // Mensajes (en el orden del archivo) y metadatos (que en el .arb pueden estar en cualquier lugar).
    final messages = <String, String>{};
    final metadata = <String, Map<String, String>>{};
    arb.forEach((key, value) {
      if (key.startsWith('@@')) return;
      if (key.startsWith('@')) {
        metadata[key.substring(1)] = _placeholders(key, value);
      } else {
        messages[key] = value as String;
      }
    });
    for (final key in metadata.keys) {
      if (!messages.containsKey(key)) _fail('$locale: metadatos @$key sin mensaje');
    }

    // Las 4 claves que cambian: el texto legacy tiene que ser el esperado (si el legacy cambió, se aborta).
    for (final c in changed) {
      final key = c['key'] as String;
      final edit = c[locale] as Map<String, dynamic>?;
      if (edit == null) continue;
      if (messages[key] != edit['from']) {
        _fail('$locale/$key: el legacy dice ${jsonEncode(messages[key])} y se esperaba ${jsonEncode(edit['from'])}');
      }
      messages[key] = edit['to'] as String;
    }

    // Las 15 claves nuevas, al final.
    for (final a in added) {
      final key = a['key'] as String;
      if (messages.containsKey(key)) _fail('$locale/$key: la clave nueva ya existe en el legacy');
      messages[key] = a[locale] as String;
      final ph = (a['placeholders'] as Map<String, dynamic>).cast<String, String>();
      for (final type in ph.values) {
        if (!_types.contains(type)) _fail('$locale/$key: tipo de parámetro no admitido: $type');
      }
      if (ph.isNotEmpty) metadata[key] = ph;
    }

    final output = <String, Object?>{'@@locale': locale};
    messages.forEach((key, text) {
      output[key] = text;
      final ph = metadata[key];
      if (ph != null && ph.isNotEmpty) {
        output['@$key'] = {
          'placeholders': {for (final e in ph.entries) e.key: {'type': e.value}},
        };
      }
    });

    final text = '${const JsonEncoder.withIndent('  ').convert(output)}\n';
    final target = File('$out/strings.$locale.json');
    target.parent.createSync(recursive: true);
    target.writeAsBytesSync(utf8.encode(text));
    stdout.writeln('[l10n] ${target.path}: ${messages.length} claves, ${metadata.length} con parámetros');
  }
}

Map<String, String> _placeholders(String key, Object? value) {
  final meta = value as Map<String, dynamic>;
  final result = <String, String>{};
  final ph = meta['placeholders'] as Map<String, dynamic>?;
  if (ph == null) return result;
  ph.forEach((name, info) {
    final type = (info as Map<String, dynamic>)['type'] as String?;
    if (type == null || !_types.contains(type)) _fail('$key/$name: tipo de parámetro no admitido: $type');
    result[name] = type!;
  });
  return result;
}

Never _fail(String message) {
  stderr.writeln('[l10n] ERROR: $message');
  exit(1);
}
