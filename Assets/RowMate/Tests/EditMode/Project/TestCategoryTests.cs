using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // AC-BASE-08 (primer open real, It. 2, 00-primer-open.md §4): las categorías con guiones (AC-…, QA-…, LT-…) corren
    // también en el NUnit 3.5 de Unity, que invalida esos tests con NUnit.Framework.CategoryAttribute. En los archivos de
    // test, [Category("…")] tiene que resolver a RowMate.Tests.CategoryAttribute (Tests/Support).
    [TestFixture]
    public sealed class TestCategoryTests
    {
        // Si este test corre (no queda NotRunnable en Unity), la categoría con guiones se aceptó, y la propiedad llega igual.
        [Test, Category("AC-BASE-08")]
        public void CategoriesWithHyphensRunAndAreReported()
        {
            var categories = TestContext.CurrentContext.Test.Properties["Category"];
            Assert.That(categories, Does.Contain("AC-BASE-08"));
        }

        [Test, Category("AC-BASE-08")]
        public void EveryCategoryAttributeIsTheRowMateOne()
        {
            var offenders = new List<string>();
            var checkedAttributes = 0;
            foreach (var type in typeof(TestCategoryTests).Assembly.GetTypes())
            {
                checkedAttributes += Check(type, type.FullName ?? type.Name, offenders);
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                foreach (var method in type.GetMethods(flags))
                {
                    checkedAttributes += Check(method, type.FullName + "." + method.Name, offenders);
                }
            }

            Assert.That(checkedAttributes, Is.GreaterThan(400), "el recorrido no encontró las categorías");
            Assert.That(offenders, Is.Empty, "categorías con la clase de NUnit (en Unity quedan NotRunnable si llevan guiones):\n"
                + string.Join("\n", offenders));
        }

        // Un using NUnit.Framework dentro del bloque namespace, o la categoría calificada, ganaría la búsqueda de nombres
        // y volvería a la clase de NUnit. También en las fuentes del simulador, que este assembly no carga.
        [Test, Category("AC-BASE-08")]
        public void TestSourcesUseTheRowMateCategory()
        {
            var insideNamespace = new Regex(@"\bnamespace\s+[\w.]+\s*\{[\s\S]*\busing\s+NUnit\.Framework\s*;");
            var qualified = new Regex(@"\bNUnit\.Framework\.Category(Attribute)?\s*\(");
            var outside = new Regex(@"\bnamespace\s+(?!RowMate\.Tests(\.|\s|\{))[\w.]+");
            var offenders = new List<string>();
            var files = SourceScan.CsFiles("Tests");
            foreach (var file in files)
            {
                var code = SourceScan.StripCommentsAndLiterals(File.ReadAllText(file));
                if (insideNamespace.IsMatch(code) || qualified.IsMatch(code) || (code.Contains("Category(") && outside.IsMatch(code)))
                {
                    offenders.Add(RepoPaths.Relative(file));
                }
            }

            Assert.That(files.Count, Is.GreaterThan(40));
            Assert.That(offenders, Is.Empty, "[Category] que no resuelve a RowMate.Tests.CategoryAttribute:\n" + string.Join("\n", offenders));
        }

        private static int Check(MemberInfo member, string name, List<string> offenders)
        {
            var count = 0;
            foreach (var attribute in member.GetCustomAttributes(typeof(NUnit.Framework.CategoryAttribute), false))
            {
                count++;
                if (attribute.GetType() != typeof(RowMate.Tests.CategoryAttribute))
                {
                    offenders.Add(name + ": " + attribute.GetType().FullName);
                }
            }

            return count;
        }
    }
}
