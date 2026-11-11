using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // Icons = la tabla de ASSET_SLOTS §4.3 (Material Symbols Rounded, commit 10ecc14). Se verifica contra los archivos
    // versionados: el .codepoints original y el cmap de las dos fuentes recortadas (UI/Icons). Así un ícono nunca se ve como
    // un cuadrado vacío por un codepoint mal copiado (AC-APP-05-03), sin abrir Unity.
    // It. 15 (#66, AC-D15-11, -12; DT15-14): + download e ios_share, − cloud_upload, cloud_download, cloud_done y link. El recorte nuevo
    // lo hace WP-D (A15-11); mientras la fuente versionada sea la de la It. 3 (tiene cloud_upload y no download), las pruebas que
    // comparan con la fuente quedan ignoradas con el motivo (WP-B no cierra la iteración con esa guarda).
    [TestFixture]
    public sealed class IconsTests
    {
        private const string IconsFolder = "Assets/RowMate/UI/Icons";
        private const string CodepointsFile = "MaterialSymbolsRounded[FILL,GRAD,opsz,wght].codepoints";
        private const int CloudUpload = 0xE2C3;
        private const int DownloadCodepoint = 0xF090;

        // Íconos del recorte que hoy ninguna pantalla usa y no son de Strava: AC-D15-12 solo saca los de Strava (DT15-14), así que
        // quedan en una lista cerrada (va a "Para issues": decidir si salen del recorte).
        private static readonly HashSet<string> ReservedWithoutUse = new HashSet<string>(StringComparer.Ordinal)
        {
            "DeleteSweep", "ExpandMore", "ExpandLess", "Landscape", "ArrowDropUp", "ArrowDropDown",
        };

        // AC-D15-11: los contornos y avances de los glifos que ya estaban, por codepoint (SHA-256 de los puntos decodificados del glyf,
        // con su bandera de "sobre la curva", los finales de contorno y el avance del hmtx; los 16 primeros hex). Calculado sobre las
        // fuentes de la It. 3 (commit 8f4073c); un recorte nuevo con la misma receta los deja iguales aunque cambien los índices.
        private static readonly Dictionary<int, string> OutlineHashes = new Dictionary<int, string>
        {
            { 0xE040, "d45ac1aab9486635" }, { 0xE145, "f8d942251b888ce1" }, { 0xE15B, "2f8b1ce6d5211ac4" },
            { 0xE16C, "698a5eaeebf22f16" }, { 0xE16F, "7de5a09cc6b81b9d" }, { 0xE1A2, "6306378e43ef9149" },
            { 0xE1A7, "71b5d42670873a03" }, { 0xE1A8, "0469b0bd39c5e033" }, { 0xE1A9, "08d665f37f13be4f" },
            { 0xE41C, "ed1bcc0471f4e232" }, { 0xE425, "c36c43acc0aa2df2" }, { 0xE564, "045cae01876fe9ab" },
            { 0xE5C4, "b795a6ad2bd92cc7" }, { 0xE5C5, "fd36d1807d0d7079" }, { 0xE5C7, "cc141f3132f2e4b8" },
            { 0xE5CC, "3bb45383c33fe2f9" }, { 0xE5CD, "5dbb788681960173" }, { 0xE5CE, "b0bf395ff4719760" },
            { 0xE5CF, "0870e2a90318cc85" }, { 0xE5D4, "346f151ff0961bfc" }, { 0xE5D5, "a314c5ee3155a102" },
            { 0xE60F, "4c85e5ae7b345083" }, { 0xE80E, "5c38c3005570a65a" }, { 0xE868, "4991290a987f9adf" },
            { 0xE87E, "113be5dee6793629" }, { 0xE8B3, "b03b2936b0dc4421" }, { 0xE8E5, "88685c935c3f4618" },
            { 0xE921, "85c15b87658d2002" }, { 0xE92E, "94492b4e7499b4e9" }, { 0xE9E4, "3266cf98fe6edea0" },
            { 0xEA0B, "fe976ed66eb46a8a" }, { 0xEAC2, "87c323fb3c37cdf2" }, { 0xEB3B, "8668c47f4aff9c7d" },
            { 0xEB43, "650583e01e4f266f" }, { 0xEF55, "46e4631f72b1d872" }, { 0xEFD6, "f0b0e7dcfc761e78" },
            { 0xF083, "2bd03bb04c0662ec" }, { 0xF097, "84e15404c33bc0c1" }, { 0xF0D3, "267be2dc56a88f5f" },
            { 0xF8B6, "23bdecf60918778e" }, { 0xF8CD, "c2c3ac6f1abe01e4" },
        };

        private static readonly Dictionary<int, string> FilledHashes = new Dictionary<int, string>
        {
            { 0xE034, "fef61e46c3cdbc18" }, { 0xE037, "362a3e48a6094630" }, { 0xE425, "c4a4ba147f048d3a" },
            { 0xE87E, "701bd2ac844cddf8" }, { 0xF0BE, "81b6cd62f128bc18" }, { 0xF0D3, "19ac914e401dd382" },
        };

        private static string OutlinePath => RepoPaths.Combine(IconsFolder, "RowMateIcons-Outline.ttf");

        private static string FilledPath => RepoPaths.Combine(IconsFolder, "RowMateIcons-Filled.ttf");

        // La fuente versionada todavía es la de la It. 3: el recorte de la 15 es de WP-D.
        private static void IgnoreUntilTheNewSubset()
        {
            var outline = ReadCmap(OutlinePath);
            if (outline.ContainsKey(CloudUpload) && !outline.ContainsKey(DownloadCodepoint))
            {
                Assert.Ignore("pendiente WP-D (A15-11): RowMateIcons-*.ttf todavía es el recorte de la It. 3 (sin download ni ios_share)");
            }
        }

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
            IgnoreUntilTheNewSubset();
            var outline = ReadCmap(OutlinePath);
            var filled = ReadCmap(FilledPath);
            var problems = new List<string>();
            foreach (var icon in Icons.All)
            {
                if (icon.InOutline && !outline.ContainsKey(icon.Codepoint))
                {
                    problems.Add(icon.Name + ": falta en RowMateIcons-Outline.ttf");
                }

                if (icon.InFilled && !filled.ContainsKey(icon.Codepoint))
                {
                    problems.Add(icon.Name + ": falta en RowMateIcons-Filled.ttf");
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-APP-05-03"), Category("AC-D15-12")]
        public void FortyThreeOutlineAndSixFilled()
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

            Assert.That(outline, Is.EqualTo(43), "It. 3: + arrow_back y more_vert; It. 15: + download e ios_share, − cloud_upload, cloud_download y link");
            Assert.That(filled, Is.EqualTo(6), "It. 15: − cloud_done");
            Assert.That(both, Is.EquivalentTo(new[] { "timer", "favorite", "person" }));
            Assert.That(Icons.All.Count, Is.EqualTo(46));
            foreach (var gone in new[] { "cloud_upload", "cloud_download", "cloud_done", "link" })
            {
                Assert.Throws<KeyNotFoundException>(() => Icons.Get(gone), gone + " salió del recorte (AC-D15-12)");
            }

            // Las fuentes no traen glifos de más (fuera de .notdef, que no tiene codepoint).
            IgnoreUntilTheNewSubset();
            Assert.That(ReadCmap(OutlinePath).Count, Is.EqualTo(43));
            Assert.That(ReadCmap(FilledPath).Count, Is.EqualTo(6));
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

        [Test, Category("AC-D15-11"), Category("AC-D15-18")]
        public void ExportGlyphsInSubset()
        {
            // Los dos glifos de exportar se escribieron por script con escape \uXXXX (AC-D15-18): el código fuente no lleva el carácter
            // del área privada y el valor es el del .codepoints.
            Assert.That(Icons.Download, Is.EqualTo(char.ConvertFromUtf32(0xF090)));
            Assert.That(Icons.IosShare, Is.EqualTo(char.ConvertFromUtf32(0xE6B8)));
            Assert.That(Icons.Get("download").InOutline && !Icons.Get("download").InFilled, Is.True);
            Assert.That(Icons.Get("ios_share").InOutline && !Icons.Get("ios_share").InFilled, Is.True);
            var source = File.ReadAllText(RepoPaths.Combine("Assets", "RowMate", "Core", "Design", "Icons.cs"));
            Assert.That(source, Does.Contain("Download = \"\\uF090\";").And.Contain("IosShare = \"\\uE6B8\";"), "escritos con escape");
            IgnoreUntilTheNewSubset();
            var outline = ReadCmap(OutlinePath);
            Assert.That(outline.ContainsKey(0xF090), Is.True, "download en el recorte de contorno");
            Assert.That(outline.ContainsKey(0xE6B8), Is.True, "ios_share en el recorte de contorno");
        }

        [Test, Category("AC-D15-11")]
        public void ExistingGlyphsUnchanged()
        {
            var problems = new List<string>();
            Compare(OutlinePath, OutlineHashes, problems);
            Compare(FilledPath, FilledHashes, problems);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(OutlineHashes.Count, Is.EqualTo(41), "los 41 de contorno que quedan de la It. 3");
            Assert.That(FilledHashes.Count, Is.EqualTo(6), "los 6 rellenos que quedan");
            foreach (var gone in new[] { 0xE2C3, 0xE2C0, 0xE250 })
            {
                Assert.That(OutlineHashes.ContainsKey(gone), Is.False, "salió del recorte");
            }

            Assert.That(FilledHashes.ContainsKey(0xE2BF), Is.False, "cloud_done salió del recorte");
        }

        [Test, Category("AC-D15-12")]
        public void EveryGlyphHasAUseAndEveryUseIsInSubset()
        {
            // Cada constante de Icons se usa como Icons.<Nombre> en el código de la app (sin Tests/) salvo la lista cerrada de
            // reservados, y cada Icons.<Nombre> que aparece es una constante de la tabla (con su glifo en el recorte).
            var constants = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in typeof(Icons).GetFields())
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    constants[field.Name] = (string)field.GetValue(null)!;
                }
            }

            var inTable = new HashSet<string>(StringComparer.Ordinal);
            foreach (var icon in Icons.All)
            {
                inTable.Add(icon.Glyph);
            }

            var used = new HashSet<string>(StringComparer.Ordinal);
            var unknown = new List<string>();
            var reference = new Regex(@"\bIcons\.([A-Z][A-Za-z0-9]*)\b");
            var root = RepoPaths.Combine("Assets", "RowMate");
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var relative = RepoPaths.Relative(file).Replace('\\', '/');
                if (relative.Contains("/Tests/") || relative.EndsWith("/Core/Design/Icons.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (Match m in reference.Matches(File.ReadAllText(file)))
                {
                    var name = m.Groups[1].Value;
                    if (name == "All" || name == "Get")
                    {
                        continue;
                    }

                    if (!constants.ContainsKey(name))
                    {
                        unknown.Add(relative + ": Icons." + name);
                        continue;
                    }

                    used.Add(name);
                }
            }

            var unused = new List<string>();
            foreach (var constant in constants)
            {
                Assert.That(inTable.Contains(constant.Value), Is.True, constant.Key + " está en la tabla");
                if (!used.Contains(constant.Key) && !ReservedWithoutUse.Contains(constant.Key))
                {
                    unused.Add(constant.Key);
                }
            }

            // El botón de exportar pasa al glifo de la fuente en WP-B (ExportTcxButton), que también borra ExportGlyph: hasta entonces
            // download e ios_share no tienen uso.
            var exportPending = File.Exists(RepoPaths.Combine("Assets", "RowMate", "Core", "Design", "ExportGlyph.cs"));
            if (exportPending)
            {
                unused.Remove(nameof(Icons.Download));
                unused.Remove(nameof(Icons.IosShare));
            }

            Assert.That(unknown, Is.Empty, "usos sin constante: " + string.Join(", ", unknown));
            Assert.That(unused, Is.Empty, "glifos del recorte sin uso (fuera de la lista de reservados): " + string.Join(", ", unused));
            foreach (var reserved in ReservedWithoutUse)
            {
                Assert.That(constants.ContainsKey(reserved), Is.True, "reservado que ya no existe: " + reserved);
                Assert.That(used.Contains(reserved), Is.False, reserved + " ya se usa: sacalo de la lista de reservados");
            }

            Assert.That(used, Does.Contain("Person").And.Contain("LinkOff"), "person (Perfil) y link_off (simulador) se quedan porque se usan");
            if (exportPending)
            {
                Assert.Ignore("pendiente WP-B: ExportTcxButton todavía dibuja ExportGlyph (download e ios_share sin uso); el resto pasó");
            }
        }

        // Los hash por codepoint de una fuente (para regenerar las tablas: correr la prueba con una tabla vacía muestra los valores).
        private static void Compare(string path, Dictionary<int, string> expected, List<string> problems)
        {
            var actual = GlyphHashes(path);
            foreach (var entry in expected)
            {
                if (!actual.TryGetValue(entry.Key, out var hash))
                {
                    problems.Add(RepoPaths.Relative(path) + ": falta U+" + entry.Key.ToString("X4", CultureInfo.InvariantCulture));
                    continue;
                }

                if (hash != entry.Value)
                {
                    problems.Add(RepoPaths.Relative(path) + ": U+" + entry.Key.ToString("X4", CultureInfo.InvariantCulture) + " cambió (" + hash + ")");
                }
            }

            if (expected.Count == 0)
            {
                var sorted = new List<int>(actual.Keys);
                sorted.Sort();
                foreach (var codepoint in sorted)
                {
                    problems.Add("{ 0x" + codepoint.ToString("X4", CultureInfo.InvariantCulture) + ", \"" + actual[codepoint] + "\" },");
                }
            }
        }

        // codepoint → hash del glifo (contornos decodificados + avance).
        private static Dictionary<int, string> GlyphHashes(string path)
        {
            var font = File.ReadAllBytes(path);
            var head = Table(font, "head");
            var maxp = Table(font, "maxp");
            var hhea = Table(font, "hhea");
            var hmtx = Table(font, "hmtx");
            var loca = Table(font, "loca");
            var glyf = Table(font, "glyf");
            var longLoca = (short)U16(font, head + 50) != 0;
            var glyphCount = U16(font, maxp + 4);
            var metrics = U16(font, hhea + 34);
            var result = new Dictionary<int, string>();
            using (var sha = SHA256.Create())
            {
                foreach (var entry in ReadCmap(path))
                {
                    var glyph = entry.Value;
                    Assert.That(glyph, Is.LessThan(glyphCount));
                    var start = longLoca ? (int)U32(font, loca + glyph * 4) : U16(font, loca + glyph * 2) * 2;
                    var end = longLoca ? (int)U32(font, loca + (glyph + 1) * 4) : U16(font, loca + (glyph + 1) * 2) * 2;
                    var advance = U16(font, hmtx + 4 * Math.Min(glyph, metrics - 1));
                    var text = Outline(font, glyf + start, end - start) + "|adv " + advance.ToString(CultureInfo.InvariantCulture);
                    var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                    var hex = new StringBuilder();
                    for (var i = 0; i < 8; i++)
                    {
                        hex.Append(digest[i].ToString("x2", CultureInfo.InvariantCulture));
                    }

                    result[entry.Key] = hex.ToString();
                }
            }

            return result;
        }

        // Un glifo simple del glyf como texto: los finales de contorno y cada punto (x, y absolutos, 1 = sobre la curva). Sin las
        // instrucciones ni el relleno de alineación (dependen de la herramienta, no del dibujo).
        private static string Outline(byte[] font, int at, int length)
        {
            if (length == 0)
            {
                return "vacío";
            }

            var contours = (short)U16(font, at);
            Assert.That(contours, Is.GreaterThanOrEqualTo(0), "glifo compuesto: la prueba solo decodifica glifos simples");
            var text = new StringBuilder();
            var p = at + 10;
            var points = 0;
            for (var c = 0; c < contours; c++)
            {
                var endPoint = U16(font, p);
                p += 2;
                text.Append(endPoint.ToString(CultureInfo.InvariantCulture)).Append(',');
                points = endPoint + 1;
            }

            var instructions = U16(font, p);
            p += 2 + instructions;
            var flags = new byte[points];
            for (var i = 0; i < points;)
            {
                var flag = font[p++];
                flags[i++] = flag;
                if ((flag & 0x08) != 0)
                {
                    var repeat = font[p++];
                    for (var r = 0; r < repeat; r++)
                    {
                        flags[i++] = flag;
                    }
                }
            }

            var xs = new int[points];
            var x = 0;
            for (var i = 0; i < points; i++)
            {
                var flag = flags[i];
                if ((flag & 0x02) != 0)
                {
                    var dx = font[p++];
                    x += (flag & 0x10) != 0 ? dx : -dx;
                }
                else if ((flag & 0x10) == 0)
                {
                    x += (short)U16(font, p);
                    p += 2;
                }

                xs[i] = x;
            }

            var y = 0;
            text.Append('|');
            for (var i = 0; i < points; i++)
            {
                var flag = flags[i];
                if ((flag & 0x04) != 0)
                {
                    var dy = font[p++];
                    y += (flag & 0x20) != 0 ? dy : -dy;
                }
                else if ((flag & 0x20) == 0)
                {
                    y += (short)U16(font, p);
                    p += 2;
                }

                text.Append(xs[i].ToString(CultureInfo.InvariantCulture)).Append(' ').Append(y.ToString(CultureInfo.InvariantCulture))
                    .Append(' ').Append((flags[i] & 0x01) != 0 ? '1' : '0').Append(';');
            }

            return text.ToString();
        }

        private static int Table(byte[] font, string tag)
        {
            var tables = U16(font, 4);
            for (var i = 0; i < tables; i++)
            {
                var record = 12 + i * 16;
                if (font[record] == tag[0] && font[record + 1] == tag[1] && font[record + 2] == tag[2] && font[record + 3] == tag[3])
                {
                    return (int)U32(font, record + 8);
                }
            }

            Assert.Fail("la fuente no tiene la tabla " + tag);
            return -1;
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

        // Codepoint → glifo en el cmap de una fuente TrueType (subtablas de formato 4 y 12, las que escribe fonttools).
        private static Dictionary<int, int> ReadCmap(string path)
        {
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            var font = File.ReadAllBytes(path);
            var cmap = Table(font, "cmap");
            var result = new Dictionary<int, int>();
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

        private static void ReadFormat4(byte[] font, int offset, Dictionary<int, int> result)
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
                        result[c] = glyph;
                    }
                }
            }
        }

        private static void ReadFormat12(byte[] font, int offset, Dictionary<int, int> result)
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
                        result[(int)c] = (int)(glyph + (c - start));
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
