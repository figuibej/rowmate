using System;
using System.IO;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // SGT · Los vectores del oráculo de la escena (06a-escena-lago/02-tech-design §6.2; AC-SCN-01-14): tools/oracle/run-oracle.ps1
    // -Scene corre el código de la escena del legacy con Flutter (tools/oracle/engine/test/scene_vectors_test.dart) y escribe los
    // seis scene_*.json; estas pruebas no necesitan Flutter ni Dart.
    //
    // El comparador de cada valor (§2, "Bit a bit o con tolerancia"; QA-6a-PAR-05): BIT A BIT la aritmética pura (el generador,
    // los segmentos y su FNV-1a, la paleta y el ambiente, las listas visibles, el _c de la orilla, las líneas del agua con la
    // fuerza leída del archivo, las estrellas, las nubes y horizonColor); Within(1e-12) sunElevation (guardado en bits), la fuerza
    // del astro y el perfil de las cordilleras; Within(1e-9) lightFor en dp. Las listas visibles se comparan con los tramos de z
    // iguales ordenados por (tipo, x, semilla) en los dos lados (QA-G6aT-15 a). Cada prueba de DRN, STD, STR, CLD, HZN, SHS, SSH y
    // DWT nombra su comparador.
    [TestFixture]
    public sealed class SceneGoldenTests
    {
        public static readonly string[] Files =
        {
            "scene_random.json", "scene_palette.json", "scene_light.json", "scene_sky.json", "scene_lake.json", "scene_water.json",
        };

        public const string Generator = "tools/oracle/engine/test/scene_vectors_test.dart";

        [Test, Category("AC-SCN-01-14"), Category("AC-SCN-14-02")]
        public void TheSixFilesComeFromTheLegacyScene()
        {
            var commit = GoldenVectors.Load("session.json").Header.LegacyCommit;
            foreach (var name in Files)
            {
                var file = GoldenVectors.Load(name);
                Assert.That(file.Header.Generator, Is.EqualTo(Generator), name);
                Assert.That(File.Exists(RepoPaths.Combine(Generator)), Is.True, "el generador está en el repo");
                Assert.That(file.Header.LegacyCommit, Is.EqualTo(commit), name + ": el mismo legacy que el resto");
                Assert.That(file.SizeBytes, Is.LessThanOrEqualTo(1048576), name + " ≤ 1 MiB");
            }
        }

        [TestCase("scene_random.json", "sequences", 45, Category = "AC-SCN-14-02")]
        [TestCase("scene_palette.json", "minutes", 1440, Category = "AC-SCN-09-02")]
        [TestCase("scene_light.json", "minutes", 1440, Category = "AC-SCN-10-02")]
        [TestCase("scene_sky.json", "stars", 560, Category = "AC-SCN-11-03")]
        [TestCase("scene_sky.json", "clouds", 196, Category = "AC-SCN-11-06")]
        [TestCase("scene_sky.json", "cloudColors", 25, Category = "AC-SCN-11-06")]
        [TestCase("scene_sky.json", "horizon", 25, Category = "AC-SCN-19-01")]
        [TestCase("scene_sky.json", "profile", 502, Category = "AC-SCN-19-01")]
        [TestCase("scene_sky.json", "snowPeaks", 21, Category = "AC-SCN-19-01")]
        [TestCase("scene_sky.json", "shore", 1350, Category = "AC-SCN-20-01")]
        [TestCase("scene_lake.json", "digests", 2001, Category = "AC-SCN-14-02")]
        [TestCase("scene_lake.json", "capVisible", 240, Category = "AC-SCN-14-05")]
        [TestCase("scene_water.json", "bands", 25, Category = "AC-SCN-13-02")]
        [TestCase("scene_water.json", "lines", 480, Category = "AC-SCN-13-03")]
        public void EachSectionIsATableWithItsRows(string file, string section, int rows)
        {
            var table = GoldenVectors.Load(file).Table(section);
            Assert.That(table.Count, Is.EqualTo(rows));
        }

        [Test, Category("AC-SCN-14-04")]
        public void TheLakeHasTheSegmentsAndTheEightVisibleLists()
        {
            var lake = GoldenVectors.Load("scene_lake.json");
            Assert.That(lake.Table("segments").Count, Is.GreaterThan(1000), "los segmentos 0 a 99 enteros");
            var distances = new System.Collections.Generic.HashSet<long>();
            foreach (var row in GoldenVectors.Rows("scene_lake.json", "visible"))
            {
                distances.Add(BitConverter.DoubleToInt64Bits(row.F64("distance")));
            }

            Assert.That(distances.Count, Is.EqualTo(8), "0, 1, 49,5, 50, 920, 1 234, 5 000 y 42 195 m");
        }

        [Test, Category("AC-SCN-01-14")]
        public void TheLegacyReferencesExist()
        {
            // LEG-01 a LEG-07 (y las con marcas: 02, 05 y 07), dibujadas por el oráculo con el código del legacy.
            const string Folder = "docs/migration/iterations/06a-escena-lago/snapshots/legacy";
            for (var i = 1; i <= 7; i++)
            {
                var name = "LEG-0" + i + ".png";
                Assert.That(File.Exists(RepoPaths.Combine(Folder + "/" + name)), Is.True, name);
            }

            foreach (var i in new[] { 2, 5, 7 })
            {
                Assert.That(File.Exists(RepoPaths.Combine(Folder + "/LEG-0" + i + "-marcas.png")), Is.True);
            }
        }
    }
}
