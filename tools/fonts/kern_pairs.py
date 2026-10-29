# Tablas de kerning K2 (DD4a-06, 04a-motor-hud-funcional/00-deuda-diseno §3.4; 04-design-notes §4.9 y DN4a-22): por cada
# cara que dibuja TEXTO, la lista de TODOS los pares GPOS `kern` distintos de 0 entre los caracteres del juego fijo, y solo
# esos. El bootstrapper (FontAssetBuilder, WP-B0) la vuelca en m_GlyphPairAdjustmentRecords; con getFontFeatures = false (K1)
# no se aplica ningún otro par.
#
# - Caras: Inter 400, 500, 600 y 700 y RowMate Numeric SC 700 (la derivada, en lugar de Barlow 700: num.stat la usa desde la
#   4a). La derivada 600 y 800 no llevan tabla (K4: solo las dibuja TabularNumber, celda por celda).
# - Juego (K2): los textos del catálogo es/en, el ASCII imprimible (U+0020 a U+007E), áéíóúüñÁÉÍÓÚÜÑ¿¡ y ×±·°. Se recalcula
#   con el catálogo (K3): si cambia un texto, se vuelve a correr.
# - Método (el de 03-rutinas/04-design-notes §14.7, validado contra las tablas que había escrito TextCore): la feature `kern`
#   de GPOS, PairPos de formatos 1 y 2 (también dentro de Extension); dentro de cada lookup gana el primer subtable que cubre
#   el primer glifo y los lookups se suman. Valor = XAdvance del primero + XPlacement del segundo, en unidades de la fuente.
# - Salida: Assets/RowMate/UI/Fonts/Kerning/<cara>.pairs.json, pares [primero, segundo, unidades] por índice de glifo,
#   ordenados por (primero, segundo); el bootstrapper usa XAdvance = unidades / unitsPerEm × tamaño de muestreo (48 en Inter,
#   90 en la derivada) y deja lo demás en 0. Reproducible: mismos bytes en cada corrida.
#
# Uso, desde la raíz del repo (sin red, con la caché de uv), DESPUÉS de freeze_tnum.py:
#   uvx --offline --from fonttools==4.68.0 python tools/fonts/kern_pairs.py
import hashlib
import json
import sys
from pathlib import Path

from fontTools.ttLib import TTFont

REPO = Path(__file__).resolve().parents[2]
ASSETS = REPO / "Assets" / "RowMate"
FONTS = ASSETS / "UI" / "Fonts"
CATALOGS = (ASSETS / "Localization" / "strings.es.json", ASSETS / "Localization" / "strings.en.json")
OUTPUT = FONTS / "Kerning"

FACES = (
    ("Inter-Regular", "Inter/Inter-Regular.ttf"),
    ("Inter-Medium", "Inter/Inter-Medium.ttf"),
    ("Inter-SemiBold", "Inter/Inter-SemiBold.ttf"),
    ("Inter-Bold", "Inter/Inter-Bold.ttf"),
    ("RowMateNumericSC-Bold", "RowMateNumericSC/RowMateNumericSC-Bold.ttf"),
)
EXTRA = "áéíóúüñÁÉÍÓÚÜÑ¿¡×±·°"
RECIPE = "tools/fonts/kern_pairs.py (fontTools 4.68.0)"


def charset():
    chars = {chr(c) for c in range(0x20, 0x7F)}
    chars.update(EXTRA)
    for path in CATALOGS:
        data = json.loads(path.read_text(encoding="utf-8"))
        for key, value in data.items():
            if key.startswith("@") or not isinstance(value, str):
                continue
            chars.update(value)
    return sorted(c for c in chars if ord(c) >= 0x20)   # sin saltos de línea ni otros de control


def kern_lookups(font):
    if "GPOS" not in font:
        return []
    gpos = font["GPOS"].table
    indices = set()
    for record in gpos.FeatureList.FeatureRecord:
        if record.FeatureTag == "kern":
            indices.update(record.Feature.LookupListIndex)
    lookups = []
    for index in sorted(indices):
        lookup = gpos.LookupList.Lookup[index]
        subtables = []
        for subtable in lookup.SubTable:
            if lookup.LookupType == 9:          # Extension
                subtable = subtable.ExtSubTable
            if subtable.__class__.__name__ == "PairPos":
                subtables.append(subtable)
        lookups.append(subtables)
    return lookups


def value_of(record_first, record_second):
    value = 0
    if record_first is not None:
        value += getattr(record_first, "XAdvance", 0) or 0
    if record_second is not None:
        value += getattr(record_second, "XPlacement", 0) or 0
    return value


def pair_value(lookups, first, second):
    total = 0
    for subtables in lookups:
        for subtable in subtables:
            coverage = subtable.Coverage.glyphs
            if first not in coverage:
                continue
            if subtable.Format == 1:
                pair_set = subtable.PairSet[coverage.index(first)]
                hit = next((r for r in pair_set.PairValueRecord if r.SecondGlyph == second), None)
                if hit is None:
                    continue                    # formato 1 sin ese segundo: prueba el subtable siguiente
                total += value_of(hit.Value1, getattr(hit, "Value2", None))
                break
            if subtable.Format == 2:
                class1 = subtable.ClassDef1.classDefs.get(first, 0)
                class2 = subtable.ClassDef2.classDefs.get(second, 0)
                record = subtable.Class1Record[class1].Class2Record[class2]
                total += value_of(record.Value1, getattr(record, "Value2", None))
                break
    return total


def write_json(path, face, source, font, characters, pairs, glyph_count):
    digest = hashlib.sha256(source.read_bytes()).hexdigest()
    lines = [
        "{",
        f'  "face": {json.dumps(face)},',
        f'  "source": {json.dumps(source.relative_to(REPO).as_posix())},',
        f'  "sourceSha256": "{digest}",',
        f'  "unitsPerEm": {font["head"].unitsPerEm},',
        f'  "recipe": {json.dumps(RECIPE)},',
        f'  "glyphs": {glyph_count},',
        '  "characters": [' + ", ".join(str(ord(c)) for c in characters) + "],",
        '  "pairs": [',
    ]
    for i, (first, second, value) in enumerate(pairs):
        lines.append(f"    [{first}, {second}, {value}]" + ("," if i < len(pairs) - 1 else ""))
    lines.append("  ]")
    lines.append("}")
    path.write_bytes(("\n".join(lines) + "\n").encode("utf-8"))


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    characters = charset()
    print(f"juego: {len(characters)} caracteres")
    for face, relative in FACES:
        source = FONTS / relative
        if not source.exists():
            print(f"FALLA: falta {source.relative_to(REPO).as_posix()} (¿corriste freeze_tnum.py?)", file=sys.stderr)
            return 1
        font = TTFont(source)
        cmap = font.getBestCmap()
        order = {name: index for index, name in enumerate(font.getGlyphOrder())}
        glyphs = sorted({cmap[ord(c)] for c in characters if ord(c) in cmap}, key=lambda g: order[g])
        missing = "".join(c for c in characters if ord(c) not in cmap)
        lookups = kern_lookups(font)
        pairs = []
        for first in glyphs:
            for second in glyphs:
                value = pair_value(lookups, first, second)
                if value != 0:
                    pairs.append((order[first], order[second], value))
        pairs.sort()
        path = OUTPUT / f"{face}.pairs.json"
        write_json(path, face, source, font, characters, pairs, len(glyphs))
        print(f"{path.relative_to(REPO).as_posix()}: {len(glyphs)} glifos, {len(pairs)} pares"
              + (f"; sin glifo: {ascii(missing)}" if missing else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
