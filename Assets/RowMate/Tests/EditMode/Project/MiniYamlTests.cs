using System.IO;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // MY · El parser YAML propio de los workflows (It. 14, AC-APP-19-10; 02-tech-design §8.3): casos válidos, inválidos y los
    // workflows reales del repositorio. Las reglas sobre los workflows (versiones fijas, permisos, timeouts, secretos) son de
    // WorkflowYamlTests (WP-B2).
    [TestFixture]
    public sealed class MiniYamlTests
    {
        [Test, Category("AC-APP-19-10")]
        public void MapsListsAndScalars()
        {
            var yaml = "# comentario\nname: ci\non:\n  push: { branches: [main] }\n  workflow_dispatch:\npermissions:\n  contents: read\njobs:\n"
                       + "  core:\n    runs-on: ubuntu-latest   # al final\n    timeout-minutes: 20\n    steps:\n      - uses: actions/checkout@v4\n"
                       + "        with:\n          lfs: true\n      - name: \"Con: dos puntos\"\n        run: |\n          echo uno\n          # no es comentario\n\n          echo dos\n"
                       + "      - 'simple'\n      - name: plegado\n        run: >-\n          a\n          b\n";
            var root = MiniYaml.Parse(yaml);
            Assert.That(root.At("name")!.Value, Is.EqualTo("ci"));
            Assert.That(root.At("on", "push", "branches")!.Items[0].Value, Is.EqualTo("main"));
            Assert.That(root.At("on", "workflow_dispatch")!.Kind, Is.EqualTo(YamlKind.Null));
            Assert.That(root.At("permissions", "contents")!.Value, Is.EqualTo("read"));
            var job = root.At("jobs", "core")!;
            Assert.That(job["runs-on"]!.Value, Is.EqualTo("ubuntu-latest"));
            Assert.That(job["timeout-minutes"]!.Value, Is.EqualTo("20"));
            var steps = job["steps"]!.Items;
            Assert.That(steps.Count, Is.EqualTo(4));
            Assert.That(steps[0]["uses"]!.Value, Is.EqualTo("actions/checkout@v4"));
            Assert.That(steps[0].At("with", "lfs")!.Value, Is.EqualTo("true"));
            Assert.That(steps[1]["name"]!.Value, Is.EqualTo("Con: dos puntos"));
            Assert.That(steps[1]["run"]!.Value, Is.EqualTo("echo uno\n# no es comentario\n\necho dos\n"));
            Assert.That(steps[2].Value, Is.EqualTo("simple"));
            Assert.That(steps[3]["run"]!.Value, Is.EqualTo("a b"));
        }

        [Test, Category("AC-APP-19-10")]
        public void ExpressionsAndQuotes()
        {
            var yaml = "a: ${{ matrix.simulator && ' + simulador' || '' }}\nb: Library-${{ hashFiles('x') }}\nc: 'it''s'\nd: \"q\\\"q\"\n"
                       + "e: { x: ${{ inputs.a }}, y: [1, \"2, 3\"] }\nf: -rowmate ${{ x }} -testCategory \"!GPU\"\n";
            var root = MiniYaml.Parse(yaml);
            Assert.That(root["a"]!.Value, Is.EqualTo("${{ matrix.simulator && ' + simulador' || '' }}"));
            Assert.That(root["b"]!.Value, Is.EqualTo("Library-${{ hashFiles('x') }}"));
            Assert.That(root["c"]!.Value, Is.EqualTo("it's"));
            Assert.That(root["d"]!.Value, Is.EqualTo("q\"q"));
            Assert.That(root.At("e", "x")!.Value, Is.EqualTo("${{ inputs.a }}"));
            Assert.That(root.At("e", "y")!.Items[1].Value, Is.EqualTo("2, 3"));
            Assert.That(root["f"]!.Value, Is.EqualTo("-rowmate ${{ x }} -testCategory \"!GPU\""));
        }

        [TestCase("a: 1\n\tb: 2\n", "tabulación")]
        [TestCase("a:\n  b: 1\n   c: 2\n", "sangría")]
        [TestCase("a: 1\na: 2\n", "clave repetida")]
        [TestCase("a: [1, 2\n", "sin cerrar")]
        [TestCase("a: \"abc\n", "comillas")]
        [TestCase("a: &ancla 1\n", "anclas")]
        [TestCase("  a: 1\n", "columna 0")]
        [TestCase("a: 1\nsuelto\n", "clave: valor")]
        [Category("AC-APP-19-10")]
        public void InvalidYamlFailsWithTheLine(string yaml, string expected)
        {
            var error = Assert.Throws<MiniYamlException>(() => MiniYaml.Parse(yaml));
            Assert.That(error!.Message, Does.Contain(expected));
            Assert.That(error.LineNumber, Is.GreaterThan(0));
        }

        [Test, Category("AC-APP-19-10")]
        public void TheRepositoryWorkflowsParse()
        {
            var files = RepoPaths.Files(".github/workflows", "*.yml");
            Assert.That(files.Count, Is.GreaterThanOrEqualTo(2));
            foreach (var file in files)
            {
                var root = MiniYaml.Parse(File.ReadAllText(file));
                Assert.That(root.IsMap, Is.True, RepoPaths.Relative(file));
                Assert.That(root["jobs"], Is.Not.Null, RepoPaths.Relative(file));
                foreach (var job in root["jobs"]!.Entries)
                {
                    Assert.That(job.Value["runs-on"], Is.Not.Null, RepoPaths.Relative(file) + ": " + job.Key);
                }
            }

            // Lo lee como corresponde: el paso de GameCI de ci-unity con su versión de Unity.
            var unity = MiniYaml.Parse(File.ReadAllText(RepoPaths.Combine(".github/workflows/ci-unity.yml")));
            Assert.That(unity.At("jobs", "unity-tests", "strategy", "matrix", "testMode")!.Items.Count, Is.EqualTo(2));
            var found = false;
            foreach (var step in unity.At("jobs", "unity-tests", "steps")!.Items)
            {
                if (step["uses"]?.Value == "game-ci/unity-test-runner@v4")
                {
                    found = true;
                    Assert.That(step.At("with", "unityVersion")!.Value, Is.EqualTo("6000.3.27f1"));
                }
            }

            Assert.That(found, Is.True, "el paso de unity-test-runner");
        }
    }
}
