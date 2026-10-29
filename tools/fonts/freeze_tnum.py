# RowMate Numeric SC (DD4a-07, 04a-motor-hud-funcional/00-deuda-diseno §3.3 y §3.6): Barlow Semi Condensed con las cifras
# tabulares de la propia fuente (`tnum`, dibujadas para eso: zero.tf a nine.tf) congeladas en el cmap. UI Toolkit no aplica
# features de OpenType, así que la única forma de dibujar las cifras tabulares de verdad es que el cmap ya apunte a ellas.
#
# - Las entradas del cmap cuyo glifo tiene sustitución `tnum` (GSUB, sustitución simple) pasan al glifo sustituto: las diez
#   cifras quedan con un solo avance (507, 521 y 535 unidades en 600, 700 y 800). Ninguna otra tabla cambia.
# - La familia cambia de nombre en los registros 1, 3, 4, 6, 16 y 21: la SIL OFL 1.1 pide otro nombre para una versión
#   modificada (Barlow no declara un nombre reservado; ASSET_SLOTS §3.7 la renombra igual).
# - Equivale a `pyftfeatfreeze -f tnum -R "Barlow Semi Condensed/RowMate Numeric SC"` (opentype-feature-freezer), con
#   fontTools solo.
# - Reproducible: sin fecha nueva (recalcTimestamp=False), así dos corridas dan los mismos bytes.
#
# Uso, desde la raíz del repo (sin red, con la caché de uv):
#   uvx --offline --from fonttools==4.68.0 python tools/fonts/freeze_tnum.py
# Corre ANTES que kern_pairs.py, que lee la derivada 700 (DN4a-22).
import sys
from pathlib import Path

from fontTools.ttLib import TTFont

REPO = Path(__file__).resolve().parents[2]
FONTS = REPO / "Assets" / "RowMate" / "UI" / "Fonts"
SOURCE = FONTS / "BarlowSemiCondensed"
TARGET = FONTS / "RowMateNumericSC"

OLD_FAMILY = "Barlow Semi Condensed"
NEW_FAMILY = "RowMate Numeric SC"
WEIGHTS = (("SemiBold", 507), ("Bold", 521), ("ExtraBold", 535))   # peso y avance esperado de las cifras (unidades)
NAME_IDS = (1, 3, 4, 6, 16, 21)
DIGITS = "0123456789"

LICENSE_NOTE = (
    "RowMate Numeric SC is a modified version of Barlow Semi Condensed: the tabular figures of its `tnum` feature are\n"
    "frozen into the cmap and the family is renamed (tools/fonts/freeze_tnum.py). It is distributed under the same\n"
    "license.\n"
)


def tnum_map(font):
    gsub = font["GSUB"].table
    mapping = {}
    for record in gsub.FeatureList.FeatureRecord:
        if record.FeatureTag != "tnum":
            continue
        for index in record.Feature.LookupListIndex:
            lookup = gsub.LookupList.Lookup[index]
            for subtable in lookup.SubTable:
                if lookup.LookupType == 7:      # Extension
                    subtable = subtable.ExtSubTable
                if hasattr(subtable, "mapping"):
                    mapping.update(subtable.mapping)
    return mapping


def rename(font):
    for record in font["name"].names:
        if record.nameID not in NAME_IDS:
            continue
        text = record.toUnicode()
        renamed = text.replace(OLD_FAMILY, NEW_FAMILY).replace(OLD_FAMILY.replace(" ", ""), NEW_FAMILY.replace(" ", ""))
        if renamed != text:
            record.string = renamed


def freeze(weight, expected_advance):
    source = SOURCE / f"BarlowSemiCondensed-{weight}.ttf"
    target = TARGET / f"RowMateNumericSC-{weight}.ttf"
    font = TTFont(source, recalcTimestamp=False)
    mapping = tnum_map(font)
    changed = 0
    for table in font["cmap"].tables:
        if not table.isUnicode():
            continue
        for codepoint, glyph in list(table.cmap.items()):
            if glyph in mapping:
                table.cmap[codepoint] = mapping[glyph]
                changed += 1
    rename(font)
    font.save(target)

    check = TTFont(target)
    cmap = check.getBestCmap()
    glyphs = [cmap[ord(c)] for c in DIGITS]
    advances = sorted({check["hmtx"][g][0] for g in glyphs})
    family = [r.toUnicode() for r in check["name"].names if r.nameID == 1 and r.platformID == 3]
    problems = []
    if any(not g.endswith(".tf") for g in glyphs):
        problems.append("el cmap de las cifras no apunta a los .tf: " + ", ".join(glyphs))
    if advances != [expected_advance]:
        problems.append(f"las cifras no tienen un solo avance de {expected_advance}: {advances}")
    for record in check["name"].names:
        if record.nameID in NAME_IDS and OLD_FAMILY.replace(" ", "") in record.toUnicode().replace(" ", ""):
            problems.append(f"el nombre {record.nameID} sigue diciendo Barlow: {record.toUnicode()}")
    print(f"{target.relative_to(REPO).as_posix()}: {changed} entradas del cmap; cifras {glyphs[0]}…{glyphs[-1]}, "
          f"avance {advances}; familia {family}")
    return problems


def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    problems = []
    for weight, advance in WEIGHTS:
        problems += freeze(weight, advance)
    license_text = (SOURCE / "OFL.txt").read_text(encoding="utf-8").replace("\r\n", "\n")
    head, _, rest = license_text.partition("\n")
    (TARGET / "OFL.txt").write_bytes((head + "\n\n" + LICENSE_NOTE + rest).encode("utf-8"))
    print(f"{(TARGET / 'OFL.txt').relative_to(REPO).as_posix()}: la licencia de Barlow con la nota de la versión modificada")
    if problems:
        print("\n".join("FALLA: " + p for p in problems), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
