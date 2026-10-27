using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common.Json;

namespace RowMate.Tests.Support
{
    // Acceso a los vectores dorados del oráculo Dart (AC-BASE-03). Los tests los leen sin necesitar Dart.
    // - Cases: para [TestCaseSource] con secciones chicas y con nombre (arreglo de objetos).
    // - Rows: para barridos grandes; un solo test recorre las filas, junta las diferencias y falla con las
    //   primeras 20 (así la salida de dotnet test y la matriz .trx siguen siendo legibles).
    public static class GoldenVectors
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, GoldenFile> Cache = new Dictionary<string, GoldenFile>(StringComparer.Ordinal);

        // Cacheado; valida schema == 1, legacyCommit de 40 hex y dartVersion "3.".
        public static GoldenFile Load(string fileName)
        {
            lock (Gate)
            {
                if (Cache.TryGetValue(fileName, out var cached))
                {
                    return cached;
                }

                var file = GoldenFile.Read(RepoPaths.Golden(fileName));
                Cache[fileName] = file;
                return file;
            }
        }

        // Un TestCaseData por elemento de una sección chica (arreglo de objetos).
        public static IEnumerable<TestCaseData> Cases(string fileName, string section, Func<JsonNode, TestCaseData> map)
        {
            var node = Load(fileName).Section(section);
            if (node.Kind != JsonKind.Array)
            {
                throw new InvalidOperationException("Golden " + fileName + "/" + section + ": se esperaba un arreglo de casos.");
            }

            var items = node.Items;
            for (var i = 0; i < items.Count; i++)
            {
                yield return map(items[i]);
            }
        }

        // Las filas de una sección en formato de tabla.
        public static IEnumerable<GoldenRow> Rows(string fileName, string section)
        {
            var table = Load(fileName).Table(section);
            for (var i = 0; i < table.Count; i++)
            {
                yield return table[i];
            }
        }

        // Barrido en UN test: check devuelve null si la fila coincide, o la diferencia ("esperado …, obtenido …").
        // Una excepción cuenta como diferencia. Falla con el total y las primeras 20 (entrada, esperado, obtenido).
        // Devuelve la cantidad de filas revisadas (falla si la sección está vacía).
        public static int CheckRows(string fileName, string section, Func<GoldenRow, string?> check)
        {
            if (check == null)
            {
                throw new ArgumentNullException(nameof(check));
            }

            var total = 0;
            var failed = 0;
            var first = new List<string>();
            foreach (var row in Rows(fileName, section))
            {
                total++;
                string? problem;
                try
                {
                    problem = check(row);
                }
                catch (Exception e)
                {
                    problem = "excepción " + e.GetType().Name + ": " + e.Message;
                }

                if (problem != null)
                {
                    failed++;
                    if (first.Count < 20)
                    {
                        first.Add("  fila " + row.Index + " " + row + ": " + problem);
                    }
                }
            }

            if (total == 0)
            {
                Assert.Fail("Golden " + fileName + "/" + section + ": la sección no tiene filas.");
            }

            if (failed > 0)
            {
                Assert.Fail("Golden " + fileName + "/" + section + ": " + failed + " de " + total
                    + " filas no coinciden con el oráculo. Primeras:\n" + string.Join("\n", first));
            }

            return total;
        }
    }
}
