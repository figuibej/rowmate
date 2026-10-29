using System.Collections.Generic;

namespace RowMate.Core.Design
{
    // Los caracteres que el arnés de capturas precarga en los atlas dinámicos de las fuentes antes de la primera toma
    // (QA-V3-06 de la It. 3; Snapshots/FontAtlasWarmup): con todos los glifos ya en el atlas, en este orden fijo, ninguna toma
    // agrega glifos y los bytes no dependen de lo que se dibujó antes (otra corrida, otro conjunto, -Filter o el orden en que
    // UI Toolkit genera el texto). ItThreeDebtTests.AtlasCharactersCoverEveryText comprueba que cubren los textos de los dos
    // idiomas y lo que escriben las tomas.
    public static class AtlasCharacters
    {
        // Puntuación tipográfica de los textos (además de ASCII y Latin-1): – — ‘ ’ “ ” • … ← → − ≈ ≤ ≥, y las flechas del
        // semáforo del HUD ▲ ▼ (U+25B2, U+25BC; It. 4a, AC-WKT-10-05): las dibuja el código, no salen del catálogo (QA-V3-06).
        private static readonly uint[] Punctuation =
        {
            0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2026, 0x2190, 0x2192, 0x2212, 0x2248, 0x2264, 0x2265,
            0x25B2, 0x25BC,
        };

        // ASCII imprimible (U+0020-U+007E), Latin-1 imprimible (U+00A0-U+00FF: acentos, ñ, ¿, ¡, ·, ×, ±, °) y Punctuation,
        // en ese orden. Un arreglo nuevo en cada llamada.
        public static uint[] Text()
        {
            var list = new List<uint>(95 + 96 + Punctuation.Length);
            for (uint c = 0x20; c <= 0x7E; c++)
            {
                list.Add(c);
            }

            for (uint c = 0xA0; c <= 0xFF; c++)
            {
                list.Add(c);
            }

            list.AddRange(Punctuation);
            return list.ToArray();
        }

        // Los glifos de una de las dos fuentes de íconos (Icons.All, en el orden de ASSET_SLOTS §4.3).
        public static uint[] IconGlyphs(bool filled)
        {
            var list = new List<uint>();
            foreach (var icon in Icons.All)
            {
                if (filled ? icon.InFilled : icon.InOutline)
                {
                    list.Add(icon.Glyph[0]);
                }
            }

            return list.ToArray();
        }

        public static bool CoversText(int codepoint)
        {
            if ((codepoint >= 0x20 && codepoint <= 0x7E) || (codepoint >= 0xA0 && codepoint <= 0xFF))
            {
                return true;
            }

            for (var i = 0; i < Punctuation.Length; i++)
            {
                if (Punctuation[i] == (uint)codepoint)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
