using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Design;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // WP-B0 de la It. 4a, integrado al empezar la 4b (QA-V4a-06; 02-tech-design §6.14 de la 4a; 00-deuda-diseno §3.4):
    // - K1: las 7 caras de texto con m_GetFontFeatures: 0.
    // - K2 y K3: la tabla de kerning de cada .asset que dibuja texto (Inter ×4 y RowMate Numeric SC 700) es EXACTAMENTE su lista
    //   de UI/Fonts/Kerning (tools/fonts/kern_pairs.py): mismos pares, en el mismo orden, con XAdvance = unidades / unidades por
    //   em × tamaño de muestreo y el resto en 0. La lista es la del .ttf versionado (SHA-256) y su juego cubre todo el catálogo.
    // - K4: la derivada 600 y 800 no llevan tabla.
    // - DD4a-07 y DN4a-22: el rol numérico es la derivada, y ni Typography.uss ni ningún .asset, .uss o .tss de Assets/RowMate
    //   nombra los FontAsset de Barlow, que ya no existen.
    [TestFixture]
    public sealed class ItFourAFontTests
    {
        private static readonly Regex Record = new Regex(
            @"- m_FirstAdjustmentRecord:\n\s+m_GlyphIndex: (\d+)\n\s+m_GlyphValueRecord:\n\s+m_XPlacement: (\S+)\n\s+m_YPlacement: (\S+)\n"
            + @"\s+m_XAdvance: (\S+)\n\s+m_YAdvance: (\S+)\n\s+m_SecondAdjustmentRecord:\n\s+m_GlyphIndex: (\d+)\n\s+m_GlyphValueRecord:\n"
            + @"\s+m_XPlacement: (\S+)\n\s+m_YPlacement: (\S+)\n\s+m_XAdvance: (\S+)\n\s+m_YAdvance: (\S+)\n\s+m_FeatureLookupFlags: (\S+)\n");

        [Test, Category("DD4a-06"), Category("QA-V4a-06")]
        public void TextFacesHaveNoRuntimeFontFeatures()
        {
            Assert.That(FontAssetPaths.TextFaces.Count, Is.EqualTo(7), "Inter ×4 y la derivada ×3");
            foreach (var face in FontAssetPaths.TextFaces)
            {
                var relative = FontAssetPaths.For(face.Family, face.Weight);
                Assert.That(ReadAsset(relative), Does.Contain("\n  m_GetFontFeatures: 0\n"), relative + " (K1)");
            }
        }

        [Test, Category("DD4a-06"), Category("QA-V4a-06")]
        public void EachKerningTableIsExactlyItsList()
        {
            var kerned = 0;
            foreach (var face in FontAssetPaths.TextFaces)
            {
                var relative = FontAssetPaths.For(face.Family, face.Weight);
                var text = ReadAsset(relative);
                var records = Records(relative, text);
                var listPath = FontAssetPaths.KerningFor(face.Family, face.Weight);
                if (listPath == null)
                {
                    Assert.That(face.Family, Is.EqualTo(FontFamilyRole.Numeric), "solo las derivadas que dibuja TabularNumber van sin tabla");
                    Assert.That(records, Is.Empty, relative + ": sin tabla (K4)");
                    Assert.That(text, Does.Contain("\n    m_GlyphPairAdjustmentRecords: []\n"), relative + " (K4)");
                    continue;
                }

                kerned++;
                var list = KerningList.Parse(File.ReadAllText(RepoPaths.Combine(listPath)));
                var pointSize = PointSize(relative, text);
                Assert.That(pointSize, Is.EqualTo(face.Family == FontFamilyRole.Numeric ? 90 : 48), relative + ": tamaño de muestreo");
                var unitsPerEm = Regex.Match(text, @"\n    m_UnitsPerEM: (\d+)\n");
                Assert.That(unitsPerEm.Success && int.Parse(unitsPerEm.Groups[1].Value, CultureInfo.InvariantCulture) == list.UnitsPerEm, Is.True,
                    relative + ": las mismas unidades por em que la lista (" + list.UnitsPerEm + ")");
                Assert.That(records.Count, Is.EqualTo(list.Pairs.Count), relative + ": tantos pares como " + listPath);
                var problems = new List<string>();
                for (var i = 0; i < records.Count && problems.Count < 10; i++)
                {
                    var r = records[i];
                    var pair = list.Pairs[i];
                    var expected = list.XAdvance(pair, pointSize);
                    if (r.First != pair.First || r.Second != pair.Second || r.XAdvance != expected || !r.RestIsZero)
                    {
                        problems.Add("par " + i + ": " + r + " en el .asset, [" + pair.First + ", " + pair.Second + ", "
                                     + expected.ToString("R", CultureInfo.InvariantCulture) + "] en la lista");
                    }
                }

                Assert.That(problems, Is.Empty, relative + " contra " + listPath + ":\n" + string.Join("\n", problems));
            }

            Assert.That(kerned, Is.EqualTo(5), "Inter 400, 500, 600 y 700 y RowMate Numeric SC 700 (DN4a-22)");
        }

        [Test, Category("DD4a-06"), Category("QA-V4a-06")]
        public void KerningListsComeFromTheVersionedFonts()
        {
            foreach (var face in FontAssetPaths.TextFaces)
            {
                var listPath = FontAssetPaths.KerningFor(face.Family, face.Weight);
                if (listPath == null)
                {
                    continue;
                }

                var list = KerningList.Parse(File.ReadAllText(RepoPaths.Combine(listPath)));
                var source = FontAssetPaths.SourceFor(face.Family, face.Weight);
                Assert.That(list.Source, Is.EqualTo(source), listPath);
                Assert.That(list.SourceSha256, Is.EqualTo(Sha256(RepoPaths.Combine(source))),
                    listPath + " quedó vieja: el .ttf cambió (volver a correr tools/fonts/kern_pairs.py)");
                Assert.That(list.Pairs.Count, Is.GreaterThan(1000), listPath);
                for (var i = 1; i < list.Pairs.Count; i++)
                {
                    var a = list.Pairs[i - 1];
                    var b = list.Pairs[i];
                    Assert.That(a.First < b.First || (a.First == b.First && a.Second < b.Second), Is.True, listPath + ": ordenados, sin repetir (" + i + ")");
                    Assert.That(b.Units, Is.Not.EqualTo(0), listPath + ": sin ceros");
                }
            }
        }

        // K3: el juego fijo cubre cada carácter de los textos del catálogo (si no, hay que volver a correr la receta).
        [Test, Category("DD4a-06"), Category("QA-V4a-06")]
        public void TheFixedSetCoversTheCatalog()
        {
            var catalog = CatalogCharacters();
            Assert.That(catalog.Count, Is.GreaterThan(60));
            foreach (var face in FontAssetPaths.TextFaces)
            {
                var listPath = FontAssetPaths.KerningFor(face.Family, face.Weight);
                if (listPath == null)
                {
                    continue;
                }

                var list = KerningList.Parse(File.ReadAllText(RepoPaths.Combine(listPath)));
                var missing = new List<string>();
                foreach (var c in catalog)
                {
                    if (!list.Covers(c))
                    {
                        missing.Add("U+" + c.ToString("X4", CultureInfo.InvariantCulture));
                    }
                }

                Assert.That(missing, Is.Empty, listPath + ": caracteres del catálogo fuera del juego (volver a correr tools/fonts/kern_pairs.py)");
                for (var c = 0x20; c <= 0x7E; c++)
                {
                    Assert.That(list.Covers(c), Is.True, listPath + ": el ASCII imprimible entero (U+" + c.ToString("X4", CultureInfo.InvariantCulture) + ")");
                }

                foreach (var c in "áéíóúüñÁÉÍÓÚÜÑ¿¡×±·°")
                {
                    Assert.That(list.Covers(c), Is.True, listPath + ": " + c);
                }
            }
        }

        [Test, Category("DD4a-07"), Category("QA-V4a-06"), Category("AC-WKT-17-12")]
        public void NumbersUseTheDerivedFaceAndNothingNamesBarlowAssets()
        {
            foreach (var weight in new[] { 600, 700, 800 })
            {
                Assert.That(FontAssetPaths.For(FontFamilyRole.Numeric, weight), Does.Contain("/RowMateNumericSC-"), "rol numérico " + weight);
                Assert.That(FontAssetPaths.SourceFor(FontFamilyRole.Numeric, weight), Does.StartWith("Assets/RowMate/UI/Fonts/RowMateNumericSC/"));
            }

            Assert.That(FontAssetPaths.All.Count, Is.EqualTo(10), "7 de texto y número, 2 de íconos y el respaldo de emojis");
            foreach (var retired in FontAssetPaths.RetiredFontAssets)
            {
                Assert.That(File.Exists(RepoPaths.Combine(retired)), Is.False, retired + " ya no existe (WP-B0)");
                Assert.That(File.Exists(RepoPaths.Combine(retired + ".meta")), Is.False, retired + ".meta");
                Assert.That(FontAssetPaths.All, Does.Not.Contain(retired));
            }

            var named = new List<string>();
            foreach (var pattern in new[] { "*.uss", "*.tss", "*.asset", "*.unity", "*.prefab" })
            {
                foreach (var file in RepoPaths.Files("Assets/RowMate", pattern))
                {
                    var content = File.ReadAllText(file);
                    foreach (var retired in FontAssetPaths.RetiredFontAssets)
                    {
                        if (content.IndexOf(Path.GetFileName(retired), StringComparison.Ordinal) >= 0)
                        {
                            named.Add(RepoPaths.Relative(file) + " → " + Path.GetFileName(retired));
                        }
                    }
                }
            }

            Assert.That(named, Is.Empty, string.Join("\n", named));
            var typography = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Presentation/Styles/Typography.uss"));
            Assert.That(typography, Does.Contain("RowMateNumericSC-Bold-SDF.asset"));
            Assert.That(typography, Does.Contain("RowMateNumericSC-ExtraBold-SDF.asset"), "num.hero en 800");
        }

        private static string ReadAsset(string relative)
        {
            var path = RepoPaths.Combine(relative);
            if (!File.Exists(path))
            {
                Assert.Inconclusive(relative + " no existe todavía (lo crea el bootstrapper)");
            }

            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        private static int PointSize(string relative, string text)
        {
            var match = Regex.Match(text, @"\n    m_PointSize: (\d+)\n");
            Assert.That(match.Success, Is.True, relative + ": m_FaceInfo.m_PointSize");
            return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static List<Adjustment> Records(string relative, string text)
        {
            var start = text.IndexOf("\n    m_GlyphPairAdjustmentRecords:", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThan(0), relative + ": m_GlyphPairAdjustmentRecords");
            var end = text.IndexOf("\n    m_MarkToBaseAdjustmentRecords:", start, StringComparison.Ordinal);
            Assert.That(end, Is.GreaterThan(start), relative + ": la tabla termina en m_MarkToBaseAdjustmentRecords");
            var section = text.Substring(start, end - start + 1);
            var list = new List<Adjustment>();
            foreach (Match m in Record.Matches(section))
            {
                list.Add(new Adjustment(m));
            }

            var count = Regex.Matches(section, @"- m_FirstAdjustmentRecord:").Count;
            Assert.That(list.Count, Is.EqualTo(count), relative + ": todos los registros con la forma esperada");
            return list;
        }

        // Los caracteres de los textos del catálogo es/en (las claves que no empiezan con @), como kern_pairs.py.
        private static SortedSet<int> CatalogCharacters()
        {
            var set = new SortedSet<int>();
            foreach (var language in new[] { "es", "en" })
            {
                var root = JsonParser.Parse(File.ReadAllText(RepoPaths.Catalog(language)));
                foreach (var entry in root.Properties)
                {
                    if (entry.Key.StartsWith("@", StringComparison.Ordinal) || entry.Value.Kind != JsonKind.String)
                    {
                        continue;
                    }

                    var value = entry.Value.AsString();
                    for (var i = 0; i < value.Length; i++)
                    {
                        var c = char.ConvertToUtf32(value, i);
                        if (char.IsHighSurrogate(value[i]))
                        {
                            i++;
                        }

                        if (c >= 0x20)
                        {
                            set.Add(c);
                        }
                    }
                }
            }

            return set;
        }

        private static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                var hash = sha.ComputeHash(stream);
                var text = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    text.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                }

                return text.ToString();
            }
        }

        private sealed class Adjustment
        {
            public Adjustment(Match m)
            {
                First = uint.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                XAdvance = (float)double.Parse(m.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                Second = uint.Parse(m.Groups[6].Value, CultureInfo.InvariantCulture);
                var rest = new[] { 2, 3, 5, 7, 8, 9, 10, 11 };
                RestIsZero = true;
                foreach (var g in rest)
                {
                    if (double.Parse(m.Groups[g].Value, NumberStyles.Float, CultureInfo.InvariantCulture) != 0)
                    {
                        RestIsZero = false;
                    }
                }
            }

            public uint First { get; }

            public uint Second { get; }

            public float XAdvance { get; }

            public bool RestIsZero { get; }

            public override string ToString()
            {
                return "[" + First + ", " + Second + ", " + XAdvance.ToString("R", CultureInfo.InvariantCulture) + (RestIsZero ? "" : ", con valores de más") + "]";
            }
        }
    }
}
