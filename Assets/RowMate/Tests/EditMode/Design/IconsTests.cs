using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // Icons = la tabla de ASSET_SLOTS §4.3 (Material Symbols Rounded, commit 10ecc14). Se verifica contra los archivos
    // versionados: el .codepoints original y el cmap de las dos fuentes recortadas (UI/Icons). Así un ícono nunca se ve como
    // un cuadrado vacío por un codepoint mal copiado (AC-APP-05-03), sin abrir Unity.
    [TestFixture]
    public sealed class IconsTests
    {
        private const string IconsFolder = "Assets/RowMate/UI/Icons";
        private const string CodepointsFile = "MaterialSymbolsRounded[FILL,GRAD,opsz,wght].codepoints";

        [Test, Category("AC-APP-05-03")]
        public void CodepointsMatchThePinnedFile()
        {
            var pinned = ReadCodepoints();
            var problems = new List<string>();
            foreach (var icon in Icons.All)
            {
                if (!pinned.TryGetValue(icon.Name, out var codepoint))
                {
                    problems.Add(icon.Name + ": no está en " + CodepointsFile);
                    continue;
                }

                if (codepoint != icon.Codepoint)
                {
                    problems.Add(icon.Name + ": el archivo dice " + codepoint.ToString("x4", CultureInfo.InvariantCulture) + " y Icons "
                        + icon.Codepoint.ToString("x4", CultureInfo.InvariantCulture));
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-APP-05-03")]
        public void FontsHaveEveryGlyph()
        {
            var outline = ReadCmap(RepoPaths.Combine(IconsFolder, "RowMateIcons-Outline.ttf"));
            var filled = ReadCmap(RepoPaths.Combine(IconsFolder, "RowMateIcons-Filled.ttf"));
            var problems = new List<string>();
            foreach (var icon in Icons.All)
            {
                if (icon.InOutline && !outline.Contains(icon.Codepoint))
                {
                    problems.Add(icon.Name + ": falta en RowMateIcons-Outline.ttf");
                }

                if (icon.InFilled && !filled.Contains(icon.Codepoint))
                {
                    problems.Add(icon.Name + ": falta en RowMateIcons-Filled.ttf");
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-APP-05-03")]
        public void FortyTwoOutlineAndSevenFilled()
        {
            var outline = 0;
            var filled = 0;
            var both = new List<string>();
            foreach (var icon in Icons.All)
            {
                outline += icon.InOutline ? 1 : 0;
                filled += icon.InFilled ? 1 : 0;
                if (icon.InOutline && icon.InFilled)
                {
                    both.Add(icon.Name);
                }
            }

            Assert.That(outline, Is.EqualTo(42));
            Assert.That(filled, Is.EqualTo(7));
            Assert.That(both, Is.EquivalentTo(new[] { "timer", "favorite", "person" }));
            Assert.That(Icons.All.Count, Is.EqualTo(46));

            // Las fuentes no traen glifos de más (fuera de .notdef, que no tiene codepoint).
            Assert.That(ReadCmap(RepoPaths.Combine(IconsFolder, "RowMateIcons-Outline.ttf")).Count, Is.EqualTo(42));
            Assert.That(ReadCmap(RepoPaths.Combine(IconsFolder, "RowMateIcons-Filled.ttf")).Count, Is.EqualTo(7));
        }

        [Test]
        public void NamesAndGlyphsAreUniqueAndConstantsMatchTheTable()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var glyphs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var icon in Icons.All)
            {
                Assert.That(names.Add(icon.Name), Is.True, "nombre repetido: " + icon.Name);
                Assert.That(glyphs.Add(icon.Glyph), Is.True, "glifo repetido: " + icon.Name);
                Assert.That(icon.Codepoint, Is.InRange(0xE000, 0xF8FF), icon.Name + " en el área de uso privado");
            }

            // Los que usa la It. 2 (ASSET_SLOTS §4.3 y 00-deuda-diseno §3.5).
            Assert.That(Icons.Get("rowing").Glyph, Is.EqualTo(Icons.Rowing));
            Assert.That(Icons.Get("timer").Glyph, Is.EqualTo(Icons.Timer));
            Assert.That(Icons.Get("speed").Glyph, Is.EqualTo(Icons.Speed));
            Assert.That(Icons.Get("bolt").Glyph, Is.EqualTo(Icons.Bolt));
            Assert.That(Icons.Get("straighten").Glyph, Is.EqualTo(Icons.Straighten));
            Assert.That(Icons.Get("favorite").Glyph, Is.EqualTo(Icons.Favorite));
            Assert.That(Icons.Get("bluetooth").Glyph, Is.EqualTo(Icons.Bluetooth));
            Assert.That(Icons.Get("bluetooth_connected").Glyph, Is.EqualTo(Icons.BluetoothConnected));
            Assert.That(Icons.Get("bluetooth_disabled").Glyph, Is.EqualTo(Icons.BluetoothDisabled));
            Assert.That(Icons.Get("bluetooth_searching").Glyph, Is.EqualTo(Icons.BluetoothSearching));
            Assert.That(Icons.Get("fitness_center").Glyph, Is.EqualTo(Icons.FitnessCenter));
            Assert.That(Icons.Get("history").Glyph, Is.EqualTo(Icons.History));
            Assert.That(Icons.Get("person").Glyph, Is.EqualTo(Icons.Person));
            Assert.That(Icons.Get("chevron_right").Glyph, Is.EqualTo(Icons.ChevronRight));
            Assert.That(Icons.Get("refresh").Glyph, Is.EqualTo(Icons.Refresh));
            Assert.That(Icons.Get("build").Glyph, Is.EqualTo(Icons.Build));
            Assert.That(Icons.Get("close").Glyph, Is.EqualTo(Icons.Close));
            Assert.That(Icons.Get("add").Glyph, Is.EqualTo(Icons.Add));
            Assert.That(Icons.Get("remove").Glyph, Is.EqualTo(Icons.Remove));
            Assert.That(Icons.Get("link_off").Glyph, Is.EqualTo(Icons.LinkOff));
            Assert.That(Icons.Get("heart_broken").Glyph, Is.EqualTo(Icons.HeartBroken));
            Assert.That(Icons.Get("error").Glyph, Is.EqualTo(Icons.Error));
            Assert.Throws<KeyNotFoundException>(() => Icons.Get("no_existe"));
        }

        // "nombre codepoint" por línea (el formato del repositorio de Material Symbols).
        private static Dictionary<string, int> ReadCodepoints()
        {
            var path = RepoPaths.Combine(IconsFolder, CodepointsFile);
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                var parts = line.Split(' ');
                if (parts.Length != 2)
                {
                    continue;
                }

                map[parts[0]] = int.Parse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return map;
        }

        // Codepoints con glifo en el cmap de una fuente TrueType (subtablas de formato 4 y 12, las que escribe fonttools).
        private static HashSet<int> ReadCmap(string path)
        {
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            var font = File.ReadAllBytes(path);
            var tables = U16(font, 4);
            var cmap = -1;
            for (var i = 0; i < tables; i++)
            {
                var record = 12 + i * 16;
                if (font[record] == 'c' && font[record + 1] == 'm' && font[record + 2] == 'a' && font[record + 3] == 'p')
                {
                    cmap = (int)U32(font, record + 8);
                }
            }

            Assert.That(cmap, Is.GreaterThanOrEqualTo(0), RepoPaths.Relative(path) + " no tiene tabla cmap");
            var result = new HashSet<int>();
            var subtables = U16(font, cmap + 2);
            for (var i = 0; i < subtables; i++)
            {
                var platform = U16(font, cmap + 4 + i * 8);
                var encoding = U16(font, cmap + 6 + i * 8);
                var offset = cmap + (int)U32(font, cmap + 8 + i * 8);
                var unicode = platform == 0 || (platform == 3 && (encoding == 1 || encoding == 10));
                if (!unicode)
                {
                    continue;
                }

                var format = U16(font, offset);
                if (format == 4)
                {
                    ReadFormat4(font, offset, result);
                }
                else if (format == 12)
                {
                    ReadFormat12(font, offset, result);
                }
            }

            return result;
        }

        private static void ReadFormat4(byte[] font, int offset, HashSet<int> result)
        {
            var segments = U16(font, offset + 6) / 2;
            var ends = offset + 14;
            var starts = ends + segments * 2 + 2;
            var deltas = starts + segments * 2;
            var rangeOffsets = deltas + segments * 2;
            for (var s = 0; s < segments; s++)
            {
                var end = U16(font, ends + s * 2);
                var start = U16(font, starts + s * 2);
                var delta = (short)U16(font, deltas + s * 2);
                var rangeOffset = U16(font, rangeOffsets + s * 2);
                for (var c = start; c <= end && c != 0xFFFF; c++)
                {
                    int glyph;
                    if (rangeOffset == 0)
                    {
                        glyph = (c + delta) & 0xFFFF;
                    }
                    else
                    {
                        var at = rangeOffsets + s * 2 + rangeOffset + (c - start) * 2;
                        glyph = U16(font, at);
                        if (glyph != 0)
                        {
                            glyph = (glyph + delta) & 0xFFFF;
                        }
                    }

                    if (glyph != 0)
                    {
                        result.Add(c);
                    }
                }
            }
        }

        private static void ReadFormat12(byte[] font, int offset, HashSet<int> result)
        {
            var groups = U32(font, offset + 12);
            for (var g = 0; g < groups; g++)
            {
                var group = offset + 16 + (int)g * 12;
                var start = U32(font, group);
                var end = U32(font, group + 4);
                var glyph = U32(font, group + 8);
                for (var c = start; c <= end; c++)
                {
                    if (glyph + (c - start) != 0)
                    {
                        result.Add((int)c);
                    }
                }
            }
        }

        private static int U16(byte[] data, int at)
        {
            return (data[at] << 8) | data[at + 1];
        }

        private static uint U32(byte[] data, int at)
        {
            return ((uint)data[at] << 24) | ((uint)data[at + 1] << 16) | ((uint)data[at + 2] << 8) | data[at + 3];
        }
    }
}
