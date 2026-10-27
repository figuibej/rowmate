using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // AC-BASE-03 (QA-P-23): los vectores del oráculo Dart existen, con versión de Dart y commit del legacy, y cumplen el
    // formato de 02-tech-design §6.2 (QA-04, QA-06). Categoría Integration: necesita los archivos de los tres WP.
    [TestFixture, Category("Integration")]
    public sealed class GoldenCoverageTests
    {
        public const long MaxBytes = 1048576; // 1 MiB por archivo (R-T11)
        public const int MaxSmallSection = 200;

        private static readonly string[] GoldenFiles =
        {
            "fmt.json", "rowing_data.json", "ftms.json", "heart_rate.json", "simulator.json", "routines.json", "session.json", "l10n_legacy.json",
        };

        private static readonly string[] FmtSections =
        {
            "toStringAsFixed", "round", "parseFixed", "toString", "toStringPow2", "toStringTies", "mod", "modInt", "truncDiv",
            "iso8601", "isoParse",
        };

        [Test, Category("AC-BASE-03")]
        public void AllGoldenFilesPresentWithHeader()
        {
            string? commit = null;
            var missing = new List<string>();
            foreach (var name in GoldenFiles)
            {
                if (!File.Exists(RepoPaths.Golden(name)))
                {
                    missing.Add(name);
                    continue;
                }

                var file = GoldenVectors.Load(name);
                if (commit == null)
                {
                    commit = file.Header.LegacyCommit;
                }

                Assert.That(file.Header.LegacyCommit, Is.EqualTo(commit), name + ": todos los vectores salen del mismo commit del legacy");
                Assert.That(file.Header.Generator, Does.StartWith("tools/oracle/bin/").And.EndWith(".dart"), name + ": generator");
                Assert.That(File.Exists(RepoPaths.Combine(file.Header.Generator)), Is.True, name + ": no existe " + file.Header.Generator);
                var sections = 0;
                foreach (var section in file.Body.Properties)
                {
                    if (section.Key == "header")
                    {
                        continue;
                    }

                    sections++;
                    Assert.That(Entries(section.Value), Is.GreaterThan(0), name + "/" + section.Key + " no tiene casos");
                }

                Assert.That(sections, Is.GreaterThan(0), name + " no tiene secciones");
            }

            Assert.That(missing, Is.Empty, "faltan vectores dorados (tools/oracle/run-oracle.ps1): " + string.Join(", ", missing));
            var fmt = GoldenVectors.Load("fmt.json");
            foreach (var section in FmtSections)
            {
                Assert.That(Entries(fmt.Section(section)), Is.GreaterThan(0), "fmt.json/" + section);
            }
        }

        [Test, Category("AC-BASE-03")]
        public void FilesWithinSizeCap()
        {
            foreach (var file in RepoPaths.Files("Assets/RowMate/Tests/Golden", "*.json"))
            {
                Assert.That(new FileInfo(file).Length, Is.LessThanOrEqualTo(MaxBytes), RepoPaths.Relative(file) + " supera 1 MiB: achicar el barrido (§6.2)");
            }
        }

        [Test, Category("AC-BASE-03")]
        public void BulkSectionsAreTables()
        {
            foreach (var path in RepoPaths.Files("Assets/RowMate/Tests/Golden/oracle", "*.json"))
            {
                var name = Path.GetFileName(path);
                var file = GoldenVectors.Load(name);
                var lines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
                foreach (var section in file.Body.Properties)
                {
                    if (section.Key == "header")
                    {
                        continue;
                    }

                    var node = section.Value;
                    if (GoldenTable.IsTable(node))
                    {
                        var table = GoldenTable.From(name + "/" + section.Key, node);
                        for (var r = 0; r < table.Count; r++)
                        {
                            foreach (var column in table.Columns)
                            {
                                var kind = table[r].Cell(column).Kind;
                                Assert.That(kind != JsonKind.Array && kind != JsonKind.Object, Is.True,
                                    name + "/" + section.Key + " fila " + r + ": celda no escalar en " + column);
                            }
                        }
                    }
                    else
                    {
                        Assert.That(Entries(node), Is.LessThanOrEqualTo(MaxSmallSection),
                            name + "/" + section.Key + ": más de " + MaxSmallSection + " entradas tienen que ir en formato de tabla (QA-06)");
                    }
                }

                // Una fila por línea: ninguna línea de una tabla empieza con sangría seguida de una celda suelta.
                foreach (var line in lines)
                {
                    Assert.That(line.Length, Is.LessThan(20000), name + ": línea demasiado larga (¿tabla sin una fila por línea?)");
                }
            }
        }

        [Test, Category("AC-BASE-03")]
        public void FmtHasEveryPowerOfTwoAndTheTies()
        {
            // QA-04: las 2 098 potencias de 2 por bits (f == 0 con todos los exponentes normales, más los 52 subnormales 2^j).
            var seen = new HashSet<long>();
            foreach (var row in GoldenVectors.Rows("fmt.json", "toStringPow2"))
            {
                var bits = BitConverter.DoubleToInt64Bits(row.F64("x"));
                var be = (int)((bits >> 52) & 0x7FF);
                var f = bits & 0xF_FFFF_FFFF_FFFFL;
                if (be == 0)
                {
                    Assert.That(f != 0 && (f & (f - 1)) == 0, Is.True, "subnormal que no es potencia de 2: " + row);
                }
                else
                {
                    Assert.That(f, Is.EqualTo(0L), "normal que no es potencia de 2: " + row);
                    Assert.That(be, Is.InRange(1, 2046), row.ToString());
                }

                Assert.That(seen.Add(bits), Is.True, "repetida: " + row);
            }

            Assert.That(seen.Count, Is.EqualTo(2098));
            Assert.That(GoldenVectors.Load("fmt.json").Table("toStringTies").Count, Is.GreaterThanOrEqualTo(500));
        }

        private static int Entries(JsonNode section)
        {
            if (GoldenTable.IsTable(section))
            {
                return section["rows"].Items.Count;
            }

            if (section.Kind == JsonKind.Array)
            {
                return section.Items.Count;
            }

            if (section.Kind == JsonKind.Object)
            {
                return section.Properties.Count;
            }

            return 1;
        }
    }
}
