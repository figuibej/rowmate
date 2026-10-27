// Instantánea de los catálogos del legacy (WP-C, 02-tech-design §6.3) -> Assets/RowMate/Tests/Golden/oracle/l10n_legacy.json.
// Lee lib/l10n/app_es.arb y app_en.arb del legacy (SOLO LECTURA, con dart:io + jsonDecode; la ruta sale del
// package_config del oráculo: package:rowmate/ = <legacy>/lib/). Es la referencia "carácter por carácter" de
// AC-APP-10-02/-04/-08 en el CI, que no tiene el legacy.
// Secciones (tablas, 02-tech-design §6.2): "es" y "en" = [key, text] en el orden del archivo;
// "placeholders" = [locale, key, name, type] de los metadatos "@clave".
// Se corre con tools/oracle/run-oracle.ps1 -Only l10n_vectors (guardia del legacy de §6.1).
import 'dart:convert';
import 'dart:io';
import 'dart:isolate';

import '../lib/golden.dart';

const generator = 'tools/oracle/bin/l10n_vectors.dart';

Future<void> main(List<String> args) async {
  final a = GoldenArgs.parse(args);
  final body = <String, Object?>{'header': header(generator, a.legacyCommit)};
  final placeholders = <List<Object?>>[];
  for (final locale in ['es', 'en']) {
    final uri = await Isolate.resolvePackageUri(Uri.parse('package:rowmate/l10n/app_$locale.arb'));
    if (uri == null) {
      throw StateError('no se pudo resolver package:rowmate/l10n/app_$locale.arb (falta --packages)');
    }
    final arb = jsonDecode(File.fromUri(uri).readAsStringSync()) as Map<String, dynamic>;
    if (arb['@@locale'] != locale) {
      throw StateError('app_$locale.arb: @@locale = ${arb['@@locale']}');
    }
    final rows = <List<Object?>>[];
    arb.forEach((key, value) {
      if (key.startsWith('@@')) return;
      if (key.startsWith('@')) {
        final ph = (value as Map<String, dynamic>)['placeholders'] as Map<String, dynamic>?;
        if (ph == null) return;
        ph.forEach((name, info) {
          placeholders.add([locale, key.substring(1), name, (info as Map<String, dynamic>)['type'] as String]);
        });
      } else {
        rows.add([key, value as String]);
      }
    });
    body[locale] = table(['key', 'text'], rows);
  }
  body['placeholders'] = table(['locale', 'key', 'name', 'type'], placeholders);
  writeGolden('l10n_legacy.json', body, args);
}
