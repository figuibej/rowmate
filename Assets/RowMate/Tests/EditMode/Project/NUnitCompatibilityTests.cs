using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace RowMate.Tests.EditMode.Project
{
    // AC-BASE-08: las fuentes de test compilan y corren igual en dotnet (NUnit 3.14) y en Unity (NUnit 3.5 de com.unity.ext.nunit).
    // La fuente de verdad es el compilador: RowMate.Tests.UnityApiCheck compila Tests/Support, Tests/EditMode y Tests/Simulator
    // contra NUnit 3.5.0 y netstandard2.1 (QA-10). Este test cubre lo que COMPILA en 3.5 pero no se admite, y da un mensaje
    // claro y temprano para los casos frecuentes (02-tech-design §2.4).
    [TestFixture]
    public sealed class NUnitCompatibilityTests
    {
        private static readonly string[] TestFolders = { "Tests" };

        // Declaración de record (QA-03) y accesor init, sobre el texto sin comentarios ni literales (02-tech-design §2.4).
        private static readonly Regex RecordDeclaration = new Regex(
            @"(?m)(^\s*|\b(?:public|internal|private|protected|sealed|abstract|partial)\s+)record\s+(?!with\b)(?:class\s+|struct\s+)?[A-Za-z_]\w*\s*[({:<]");

        private static readonly Regex InitAccessor = new Regex(@"[{;]\s*init\s*([;{]|=>)");

        [Test, Category("AC-BASE-08")]
        public void NoUnsupportedNUnitApis()
        {
            var forbidden = new[]
            {
                new Regex(@"\[\s*(NUnit\.Framework\.)?Parallelizable\b"),
                new Regex(@"\bParallelizable\s*\("),
                new Regex(@"\[\s*NonParallelizable\b"),
                new Regex(@"\bNonParallelizable\b"),
                new Regex(@"\bDefaultFloatingPointTolerance\b"),
                new Regex(@"\bAssert\s*\.\s*Multiple\b"),
                new Regex(@"\bAssert\s*\.\s*ThrowsAsync\b"),
                new Regex(@"\bAssert\s*\.\s*CatchAsync\b"),
                new Regex(@"\bClassicAssert\b"),
                new Regex(@"\bWarn\s*\."),
                new Regex(@"\bSetArgDisplayNames\b"),
                new Regex(@"\bAddTestAttachment\b"),
                new Regex(@"\bIs\s*\.\s*Not\s*\.\s*Default\b"),
                new Regex(@"\bIs\s*\.\s*Default\b"),
                new Regex(@"\bIs\s*\.\s*AnyOf\b"),
                // Primer open real (It. 2): en NUnit 3.5 Has.Count busca una propiedad pública "Count" por reflexión y un
                // arreglo detrás de IReadOnlyList<T> no la tiene ("Property Count was not found"). Usar .Count, Is.EqualTo(n).
                new Regex(@"\bHas\s*\.\s*Count\b"),
                // Primer open real (It. 2): el Unity Test Framework no aplica [SetCulture]/[SetUICulture] (compila, pero
                // UnityTestExecutionContext.CurrentCulture no toca el hilo). Fijar la cultura a mano en try/finally.
                new Regex(@"\bSet(UI)?Culture\s*\("),
                // Métodos de test asincrónicos: los tests son sincrónicos (FakeScheduler, TestMainThreadContext.Run).
                new Regex(@"\[[^\]]*\b(Test|TestCase|TestCaseSource|UnityTest)\b[^\]]*\]\s*(\[[^\]]*\]\s*)*(public|internal|private|protected)?\s*(static\s+)?async\b"),
            };
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles(TestFolders))
            {
                offenders.AddRange(SourceScan.Matches(file, forbidden, true));
            }

            Assert.That(offenders, Is.Empty, "API de NUnit no admitida (02-tech-design §2.4):\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-BASE-08")]
        public void NoRecordOrInitInTestSources()
        {
            // QA-03: en Unity los assemblies de test ven por IVT dos o más shims internos de IsExternalInit y declarar un
            // record da CS0518. Usar los records de Core ({ … } y with) sí está permitido.
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles(TestFolders))
            {
                offenders.AddRange(SourceScan.Matches(file, new[] { RecordDeclaration, InitAccessor }, true));
            }

            Assert.That(offenders, Is.Empty, "record o init en fuentes de test (usar class/struct con constructor):\n" + string.Join("\n", offenders));
        }

        [TestCase("public sealed record Case(int A);")]
        [TestCase("record Row(string X);")]
        [TestCase("internal record class Thing { }")]
        [TestCase("    record Foo<T>(T Value);")]
        [TestCase("public int X { get; init; }")]
        [TestCase("public string Y { get; init => y = value; }")]
        [TestCase("private record Pair : Base { }")]
        [TestCase("public partial record struct P(int X);")]
        [TestCase("int Z { init; get; }")]
        public void RecordRulesCatchDeclarations(string code)
        {
            var text = SourceScan.StripCommentsAndLiterals(code);
            Assert.That(RecordDeclaration.IsMatch(text) || InitAccessor.IsMatch(text), Is.True, code);
        }

        [TestCase("var record = Load();")]
        [TestCase("return record with { Id = 2 };")]
        [TestCase("x = init;")]
        [TestCase("var r = original with { Name = \"record Foo(\" };")]
        [TestCase("// public sealed record Case(int A);")]
        [TestCase("var initial = init + 1;")]
        [TestCase("Assert.That(record.Id, Is.EqualTo(1));")]
        [TestCase("record.Value = 3;")]
        [TestCase("var text = \"{ get; init; }\";")]
        public void RecordRulesIgnoreUsages(string code)
        {
            var text = SourceScan.StripCommentsAndLiterals(code);
            Assert.That(RecordDeclaration.IsMatch(text) || InitAccessor.IsMatch(text), Is.False, code);
        }
    }
}
