using System;
using System.Collections.Generic;
using RowMate.Core.Common.Json;

namespace RowMate.Core.Design
{
    // Un par de kerning de la lista K2 (00-deuda-diseno §3.4 de la It. 4a): los índices de glifo de la fuente (los de TextCore)
    // y el ajuste en unidades de la fuente, que va en el XAdvance del primero.
    public readonly struct KerningPair
    {
        public KerningPair(uint first, uint second, int units)
        {
            First = first;
            Second = second;
            Units = units;
        }

        public uint First { get; }

        public uint Second { get; }

        public int Units { get; }
    }

    // La lista K2 de una cara (DD4a-06; formato en tools/fonts/README.md): la escribe tools/fonts/kern_pairs.py en
    // Assets/RowMate/UI/Fonts/Kerning/<cara>.pairs.json y el bootstrapper (FontAssetBuilder, WP-B0) la vuelca tal cual en
    // m_FontFeatureTable.m_GlyphPairAdjustmentRecords del FontAsset. Una sola lectura y un solo cálculo del XAdvance para el
    // bootstrapper y para la prueba que compara cada .asset con su lista (ItFourAFontTests, K3).
    public sealed class KerningList
    {
        private KerningList(string face, string source, string sourceSha256, int unitsPerEm, IReadOnlyList<int> characters,
                            IReadOnlyList<KerningPair> pairs)
        {
            Face = face;
            Source = source;
            SourceSha256 = sourceSha256;
            UnitsPerEm = unitsPerEm;
            Characters = characters;
            Pairs = pairs;
        }

        public string Face { get; }

        // El .ttf con el que se calculó (ruta del repo) y su SHA-256 en hexadecimal: si la fuente cambia, la lista quedó vieja.
        public string Source { get; }

        public string SourceSha256 { get; }

        public int UnitsPerEm { get; }

        // El juego fijo (puntos de código ordenados): el catálogo es/en, el ASCII imprimible, las letras y signos de K2.
        public IReadOnlyList<int> Characters { get; }

        // Ordenados por (primero, segundo), sin ceros.
        public IReadOnlyList<KerningPair> Pairs { get; }

        public static KerningList Parse(string json)
        {
            var root = JsonParser.Parse(json ?? throw new ArgumentNullException(nameof(json)));
            var unitsPerEm = (int)root["unitsPerEm"].AsLong();
            if (unitsPerEm <= 0)
            {
                throw new FormatException("KerningList: unitsPerEm tiene que ser positivo.");
            }

            var characters = new List<int>();
            foreach (var c in root["characters"].Items)
            {
                characters.Add((int)c.AsLong());
            }

            var pairs = new List<KerningPair>();
            foreach (var p in root["pairs"].Items)
            {
                var items = p.Items;
                if (items.Count != 3)
                {
                    throw new FormatException("KerningList: cada par es [primero, segundo, unidades].");
                }

                pairs.Add(new KerningPair((uint)items[0].AsLong(), (uint)items[1].AsLong(), (int)items[2].AsLong()));
            }

            return new KerningList(root["face"].AsString(), root["source"].AsString(), root["sourceSha256"].AsString(), unitsPerEm,
                characters, pairs);
        }

        // XAdvance del primero en el FontAsset: unidades / unidades por em × tamaño de muestreo (48 en Inter, 90 en la derivada),
        // en float como lo guarda TextCore (DD4a-06).
        public float XAdvance(KerningPair pair, int samplingPointSize)
        {
            return (float)(pair.Units / (double)UnitsPerEm * samplingPointSize);
        }

        public bool Covers(int codePoint)
        {
            var lo = 0;
            var hi = Characters.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;
                var c = Characters[mid];
                if (c == codePoint)
                {
                    return true;
                }

                if (c < codePoint)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return false;
        }
    }
}
