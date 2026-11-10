using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // It. 14 (AC-APP-19-09, -10; 14-pulido-plataformas/02-tech-design §8.3): TODOS los workflows se leen con un parser YAML (MiniYaml) y
    // cumplen las reglas: versiones fijas en cada uses, permissions.contents: read en la raíz, timeout-minutes en cada job, Unity fijado
    // en la versión de ProjectVersion.txt, secrets solo en env o with (nunca en if ni en run) y sin rutas de la PC. Corre en la PC y en
    // ci-core (run-tests.ps1).
    [TestFixture]
    public sealed class WorkflowYamlTests
    {
        private static readonly Regex PinnedUses = new Regex(@"^[\w.-]+/[\w.-]+@v\d+(\.\d+){0,2}$");

        private static IEnumerable<string> Workflows()
        {
            var files = Directory.GetFiles(RepoPaths.Combine(".github/workflows"), "*.yml");
            Array.Sort(files, StringComparer.Ordinal);
            Assert.That(files.Length, Is.GreaterThanOrEqualTo(2));
            return files;
        }

        private static string UnityVersion()
        {
            var line = File.ReadAllLines(RepoPaths.Combine("ProjectSettings/ProjectVersion.txt"))[0];
            return line.Substring(line.IndexOf(':') + 1).Trim();
        }

        [Test, Category("AC-APP-19-10")]
        public void EveryWorkflowParses()
        {
            foreach (var file in Workflows())
            {
                var root = MiniYaml.Parse(File.ReadAllText(file));
                Assert.That(root["name"]?.Value, Is.Not.Null.And.Not.Empty, RepoPaths.Relative(file));
                Assert.That(root["on"], Is.Not.Null, RepoPaths.Relative(file));
                Assert.That(root["jobs"]!.Entries.Count, Is.GreaterThan(0), RepoPaths.Relative(file));
            }
        }

        [Test, Category("AC-APP-19-10")]
        public void PinnedActionsReadPermissionsAndTimeouts()
        {
            foreach (var file in Workflows())
            {
                var name = RepoPaths.Relative(file);
                var root = MiniYaml.Parse(File.ReadAllText(file));
                Assert.That(root.At("permissions", "contents")?.Value, Is.EqualTo("read"), name + ": permissions.contents: read");
                foreach (var job in root["jobs"]!.Entries)
                {
                    Assert.That(job.Value["timeout-minutes"]?.Value, Does.Match(@"^\d+$"), name + ": " + job.Key + " sin timeout-minutes");
                }

                foreach (var scalar in root.Scalars())
                {
                    if (scalar.Key.EndsWith(".uses", StringComparison.Ordinal))
                    {
                        Assert.That(PinnedUses.IsMatch(scalar.Value.Value ?? ""), Is.True, name + ": " + scalar.Key + " = " + scalar.Value.Value);
                    }
                }
            }
        }

        [Test, Category("AC-APP-19-10")]
        public void UnityPinnedToProjectVersion()
        {
            var version = UnityVersion();
            var found = 0;
            foreach (var file in Workflows())
            {
                foreach (var scalar in MiniYaml.Parse(File.ReadAllText(file)).Scalars())
                {
                    if (scalar.Key.EndsWith(".unityVersion", StringComparison.Ordinal))
                    {
                        found++;
                        Assert.That(scalar.Value.Value, Is.EqualTo(version), RepoPaths.Relative(file) + ": " + scalar.Key);
                    }
                }
            }

            Assert.That(found, Is.GreaterThanOrEqualTo(5), "tests y los builds de Windows, Android, iOS y macOS");
        }

        [Test, Category("AC-APP-19-09")]
        public void SecretsOnlyInEnvOrWith()
        {
            foreach (var file in Workflows())
            {
                foreach (var scalar in MiniYaml.Parse(File.ReadAllText(file)).Scalars())
                {
                    if (!(scalar.Value.Value ?? "").Contains("secrets."))
                    {
                        continue;
                    }

                    Assert.That(scalar.Key, Does.Match(@"\.(env|with)\.[A-Za-z0-9_-]+$"), RepoPaths.Relative(file) + ": " + scalar.Key);
                }
            }
        }

        [Test, Category("AC-APP-19-09")]
        public void NoMachinePathsNorCredentials()
        {
            foreach (var file in Workflows())
            {
                var text = File.ReadAllText(file);
                foreach (var needle in new[] { "C:\\", "C:/", "/Users/", "/home/", "-----BEGIN", ".ulf\"", "password:" })
                {
                    Assert.That(text, Does.Not.Contain(needle), RepoPaths.Relative(file));
                }
            }
        }
    }
}
