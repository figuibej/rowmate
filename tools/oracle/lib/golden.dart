// Infraestructura común de los vectores dorados (02-tech-design §6.2). Solo importa dart:*.
//
// Formato: un objeto JSON con "header" y una entrada por sección.
// - Sección chica (casos con nombre, <= 200 entradas): arreglo de objetos legible; un double va como
//   f64(x) = {"v": <toString() o "NaN"/"Infinity">, "bits": "<16 hex>"}.
// - Sección tabla (barridos, aleatorios, fuzz: toda sección de más de 200 entradas): table(columns, rows),
//   que se escribe con una fila por línea y sin sangría; celdas escalares, un double como f64bits(x).
// Los tests de C# leen todo double por sus bits (GoldenJsonExtensions.AsF64, GoldenRow.F64).
// Salida: LF, salto de línea final, UTF-8 sin BOM.
import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

/// Argumentos comunes de los scripts: --out <carpeta> --legacy-commit <sha>.
class GoldenArgs {
  final String outDir;
  final String legacyCommit;

  GoldenArgs(this.outDir, this.legacyCommit);

  static GoldenArgs parse(List<String> args) {
    String? out;
    String? commit;
    for (var i = 0; i < args.length; i++) {
      if (args[i] == '--out' && i + 1 < args.length) {
        out = args[++i];
      } else if (args[i] == '--legacy-commit' && i + 1 < args.length) {
        commit = args[++i];
      } else {
        throw ArgumentError('argumento desconocido: ${args[i]}');
      }
    }
    if (out == null || commit == null) {
      throw ArgumentError('uso: <script> --out <carpeta> --legacy-commit <sha>');
    }
    if (!RegExp(r'^[0-9a-f]{40}$').hasMatch(commit)) {
      throw ArgumentError('legacy-commit no es un sha de 40 hex: $commit');
    }
    return GoldenArgs(out, commit);
  }
}

/// Sección en formato de tabla.
class GoldenTableData {
  final List<String> columns;
  final List<List<Object?>> rows;

  GoldenTableData(this.columns, this.rows) {
    for (var i = 0; i < rows.length; i++) {
      if (rows[i].length != columns.length) {
        throw ArgumentError('fila $i: ${rows[i].length} celdas y ${columns.length} columnas');
      }
      for (final cell in rows[i]) {
        if (!(cell == null || cell is String || cell is int || cell is bool || (cell is double && cell.isFinite))) {
          throw ArgumentError('fila $i: celda no escalar o double no finito ($cell); usá f64bits');
        }
      }
    }
  }
}

final ByteData _bits = ByteData(8);

/// Encabezado común.
Map<String, Object?> header(String generator, String legacyCommit) => {
      'schema': 1,
      'generator': generator,
      'dartVersion': Platform.version,
      'legacyCommit': legacyCommit,
      'generatedAt': DateTime.now().toUtc().toIso8601String(),
    };

/// Bits IEEE-754 de un double: 16 hex en minúsculas, big-endian.
String f64bits(double x) {
  _bits.setFloat64(0, x);
  final sb = StringBuffer();
  for (var i = 0; i < 8; i++) {
    sb.write(_bits.getUint8(i).toRadixString(16).padLeft(2, '0'));
  }
  return sb.toString();
}

/// Un double legible y exacto: {"v": ..., "bits": "<16 hex>"}.
Map<String, Object?> f64(double x) => {'v': x.isFinite ? x : x.toString(), 'bits': f64bits(x)};

/// Bytes como en la spec: "2C 00 30".
String bytesHex(List<int> bytes) =>
    bytes.map((b) => (b & 0xFF).toRadixString(16).padLeft(2, '0').toUpperCase()).join(' ');

/// Sección de tabla.
GoldenTableData table(List<String> columns, List<List<Object?>> rows) => GoldenTableData(columns, rows);

/// Escribe <out>/<fileName>. body incluye "header" (ver header()).
void writeGolden(String fileName, Map<String, Object?> body, List<String> args) {
  final a = GoldenArgs.parse(args);
  final sb = StringBuffer();
  _write(sb, body, 0);
  sb.write('\n');
  final file = File('${a.outDir}/$fileName');
  file.parent.createSync(recursive: true);
  file.writeAsBytesSync(utf8.encode(sb.toString()));
  stdout.writeln('[oracle] $fileName: ${file.lengthSync()} bytes');
}

bool _isScalar(Object? v) => v == null || v is String || v is num || v is bool;

void _write(StringBuffer sb, Object? value, int indent) {
  final pad = ' ' * indent;
  final inner = ' ' * (indent + 2);
  if (value is GoldenTableData) {
    sb.write('{\n');
    sb.write('$inner"columns": ${jsonEncode(value.columns)},\n');
    if (value.rows.isEmpty) {
      sb.write('$inner"rows": []\n');
    } else {
      sb.write('$inner"rows": [\n');
      for (var i = 0; i < value.rows.length; i++) {
        sb.write(jsonEncode(value.rows[i]));
        sb.write(i + 1 < value.rows.length ? ',\n' : '\n');
      }
      sb.write('$inner]\n');
    }
    sb.write('$pad}');
  } else if (value is Map) {
    if (value.isEmpty) {
      sb.write('{}');
      return;
    }
    sb.write('{\n');
    var i = 0;
    for (final entry in value.entries) {
      sb.write('$inner${jsonEncode(entry.key.toString())}: ');
      _write(sb, entry.value, indent + 2);
      sb.write(++i < value.length ? ',\n' : '\n');
    }
    sb.write('$pad}');
  } else if (value is List) {
    if (value.isEmpty) {
      sb.write('[]');
      return;
    }
    if (value.every(_isScalar)) {
      sb.write(jsonEncode(value));
      return;
    }
    sb.write('[\n');
    for (var i = 0; i < value.length; i++) {
      sb.write(inner);
      _write(sb, value[i], indent + 2);
      sb.write(i + 1 < value.length ? ',\n' : '\n');
    }
    sb.write('$pad]');
  } else {
    sb.write(jsonEncode(value));
  }
}
