using System;
using System.IO;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // It. 14 (AC-APP-19-06 a -09; 14-pulido-plataformas/02-tech-design §8.1): la forma de ci-unity.yml, leída con MiniYaml. La corrida real
    // queda NO VERIFICABLE hasta que el usuario cargue los secretos (P-08).
    [TestFixture]
    public sealed class CiUnityWorkflowTests
    {
        private static YamlNode Workflow => MiniYaml.Parse(File.ReadAllText(RepoPaths.Combine(".github/workflows/ci-unity.yml")));

        private static string Text => File.ReadAllText(RepoPaths.Combine(".github/workflows/ci-unity.yml")).Replace("\r\n", "\n");

        [Test, Category("AC-APP-19-06")]
        public void JobsAndTriggers()
        {
            var root = Workflow;
            Assert.That(root.At("on", "push", "branches")!.Items[0].Value, Is.EqualTo("main"));
            Assert.That(root.At("on", "workflow_dispatch", "inputs", "apple", "default")?.Value, Is.EqualTo("false"));
            foreach (var job in new[] { "gate", "unity-tests", "build-windows", "build-android", "build-apple" })
            {
                Assert.That(root.At("jobs", job), Is.Not.Null, job);
                if (job != "gate")
                {
                    Assert.That(root.At("jobs", job, "needs")?.Value, Is.EqualTo("gate"), job);
                    Assert.That(root.At("jobs", job, "if")?.Value, Does.Contain("needs.gate.outputs.unity == 'true'"), job);
                }
            }
        }

        [Test, Category("AC-APP-19-07")]
        public void GateSkipsWithWarning()
        {
            var gate = Workflow.At("jobs", "gate")!;
            Assert.That(gate.At("outputs", "unity")?.Value, Is.EqualTo("${{ steps.check.outputs.unity }}"));
            Assert.That(gate.At("outputs", "android-signed")?.Value, Is.EqualTo("${{ steps.check.outputs.android-signed }}"));
            var step = gate["steps"]!.Items[0];
            foreach (var secret in new[] { "UNITY_LICENSE", "UNITY_EMAIL", "UNITY_PASSWORD" })
            {
                Assert.That(step.At("env", secret)?.Value, Is.EqualTo("${{ secrets." + secret + " }}"), secret);
            }

            var run = step["run"]!.Value!;
            Assert.That(run, Does.Contain("::warning::Falta el secreto $name"));
            Assert.That(run, Does.Contain("tools/unity/README.md"));
            Assert.That(run, Does.Not.Contain("exit 1"), "sin secretos el workflow queda en verde");
        }

        [Test, Category("AC-APP-19-06")]
        public void BuildsAndArtifacts()
        {
            var root = Workflow;
            AssertBuild(root, "build-windows", "StandaloneWindows64", "-rowmateTarget Windows", "RowMate-Windows");
            AssertBuild(root, "build-android", "Android", "-rowmateTarget Android -rowmateBackend IL2CPP", null);
            foreach (var job in root["jobs"]!.Entries)
            {
                foreach (var scalar in job.Value.Scalars())
                {
                    if (scalar.Key.EndsWith(".retention-days", StringComparison.Ordinal))
                    {
                        Assert.That(scalar.Value.Value, Is.EqualTo("14"), job.Key);
                    }
                }
            }
        }

        [Test, Category("AC-APP-19-06")]
        public void ArtifactPathsOnlyPlayer()
        {
            foreach (var job in new[] { "build-windows", "build-android", "build-apple" })
            {
                foreach (var scalar in Workflow.At("jobs", job)!.Scalars())
                {
                    if (scalar.Key.EndsWith(".with.path", StringComparison.Ordinal) && !scalar.Key.Contains("cache"))
                    {
                        var path = scalar.Value.Value ?? "";
                        if (path == "Library")
                        {
                            continue;
                        }

                        Assert.That(path, Does.StartWith("build/RowMate-"), job + ": " + path);
                        Assert.That(path, Does.Not.Contain("*").And.Not.Contain(".log").And.Not.Contain("keystore"), job);
                    }
                }
            }

            Assert.That(AllText(Workflow.At("jobs", "build-android")!), Does.Contain("build/RowMate-Android/RowMate.apk"));
        }

        [Test, Category("AC-APP-19-09")]
        public void DebugSignedSuffix()
        {
            var text = Text;
            Assert.That(text, Does.Contain("name: ${{ needs.gate.outputs.android-signed == 'true' && 'RowMate-Android' || 'RowMate-Android-debugsigned' }}"));
            foreach (var secret in new[] { "ANDROID_KEYSTORE_BASE64", "ANDROID_KEYSTORE_PASS", "ANDROID_KEYALIAS_NAME", "ANDROID_KEYALIAS_PASS" })
            {
                Assert.That(text, Does.Contain("${{ secrets." + secret + " }}"), secret);
            }

            var signing = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Editor/Build/AndroidSigning.cs"));
            Assert.That(signing, Does.Contain("\"-androidKeystoreName\"").And.Contain("\"-androidKeyaliasPass\""));
            var cli = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Editor/Build/BuildCli.cs"));
            Assert.That(cli, Does.Contain("previousSigning?.Restore();"), "la firma vuelve a como estaba (en memoria)");
        }

        [Test, Category("AC-APP-19-08")]
        public void AppleManualUnsigned()
        {
            var apple = Workflow.At("jobs", "build-apple")!;
            Assert.That(apple["if"]!.Value, Does.Contain("github.event_name == 'workflow_dispatch' && inputs.apple"));
            Assert.That(apple["runs-on"]!.Value, Does.StartWith("macos-"));
            var text = AllText(apple);
            Assert.That(text, Does.Contain("CODE_SIGNING_ALLOWED=NO"));
            Assert.That(text, Does.Contain("RowMate-iOS-unsigned").And.Contain("RowMate-macOS-unsigned"));
            Assert.That(text, Does.Contain("-rowmateTarget iOS").And.Contain("-rowmateTarget macOS"));
        }

        // Todos los escalares de un nodo, uno por línea.
        private static string AllText(YamlNode node)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var scalar in node.Scalars())
            {
                builder.Append(scalar.Value.Value).AppendLine();
            }

            return builder.ToString();
        }

        private static void AssertBuild(YamlNode root, string job, string platform, string parameters, string? artifact)
        {
            var steps = root.At("jobs", job, "steps")!.Items;
            var builder = false;
            foreach (var step in steps)
            {
                if ((step["uses"]?.Value ?? "").StartsWith("game-ci/unity-builder@v4", StringComparison.Ordinal))
                {
                    builder = true;
                    Assert.That(step.At("with", "targetPlatform")!.Value, Is.EqualTo(platform), job);
                    Assert.That(step.At("with", "buildMethod")!.Value, Is.EqualTo("RowMate.EditorTools.BuildCli.Build"), job);
                    Assert.That(step.At("with", "customParameters")!.Value, Does.Contain(parameters), job);
                }

                if (artifact != null && (step["uses"]?.Value ?? "").StartsWith("actions/upload-artifact@v4", StringComparison.Ordinal))
                {
                    Assert.That(step.At("with", "name")!.Value, Is.EqualTo(artifact), job);
                }
            }

            Assert.That(builder, Is.True, job);
        }
    }
}
