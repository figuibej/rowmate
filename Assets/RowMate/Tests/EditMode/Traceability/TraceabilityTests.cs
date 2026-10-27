using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.EditMode.Project;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Traceability
{
    // AC-BASE-09: cada AC tiene al menos un test marcado con su ID y cada caso legacy conserva su nombre y cita su origen.
    // No usa reflexión (QA-09): lee el TEXTO de Tests/EditMode/** y Tests/Simulator/** (sin comentarios) y extrae los literales
    // de [Category("…")], Category = "…", .SetCategory("…"), TestName = "…" y .SetName("…"). Así ve también los tests del
    // simulador, que EditMode no puede referenciar. El resultado real por AC sale de run-tests.ps1 -Matrix (.trx).
    // It. 2 (QA-G2-16): It02Ids trae los 125 AC y los 15 LT con su verificación; los que son solo de Unity se buscan por texto
    // en Tests/PlayMode*, Tests/UnityEditMode y Snapshots (WP-B), y se ignoran mientras esas carpetas no tengan pruebas.
    [TestFixture]
    public sealed class TraceabilityTests
    {
        private static readonly Regex CategoryAttribute = new Regex(@"\bCategory\s*\(\s*""((?:[^""\\\n]|\\.)*)""\s*\)");
        private static readonly Regex CategoryProperty = new Regex(@"\bCategory\s*=\s*""((?:[^""\\\n]|\\.)*)""");
        private static readonly Regex SetCategory = new Regex(@"\.SetCategory\s*\(\s*""((?:[^""\\\n]|\\.)*)""\s*\)");
        private static readonly Regex TestNameProperty = new Regex(@"\bTestName\s*=\s*""((?:[^""\\\n]|\\.)*)""");
        private static readonly Regex SetName = new Regex(@"\.SetName\s*\(\s*""((?:[^""\\\n]|\\.)*)""\s*\)");
        private static readonly Regex AcPattern = new Regex(@"^AC-[A-Z]+-\d\d(-\d\d)?$");
        private static readonly Regex LtPattern = new Regex(@"^LT-\d\d$");

        [Test, Category("AC-BASE-09")]
        public void It01IdsAreComplete()
        {
            // 139 AC = BLE 37 + HRM 9 + RUT 58 + APP 25 + BASE 10; 19 LT = 7 + 2 + 6 + 1 + 3 (01-po-spec, encabezado y §5).
            var ids = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            Assert.That(It01Ids.AcceptanceIds.Length, Is.EqualTo(139));
            Assert.That(ids.Count, Is.EqualTo(139), "IDs repetidos en It01Ids");
            Assert.That(CountWithPrefix("AC-BLE-"), Is.EqualTo(37));
            Assert.That(CountWithPrefix("AC-HRM-"), Is.EqualTo(9));
            Assert.That(CountWithPrefix("AC-RUT-"), Is.EqualTo(58));
            Assert.That(CountWithPrefix("AC-APP-"), Is.EqualTo(25));
            Assert.That(CountWithPrefix("AC-BASE-"), Is.EqualTo(10));
            foreach (var id in It01Ids.AcceptanceIds)
            {
                Assert.That(AcPattern.IsMatch(id), Is.True, id);
            }

            Assert.That(It01Ids.LegacyCases.Length, Is.EqualTo(19));
            var byFile = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < It01Ids.LegacyCases.Length; i++)
            {
                var lt = It01Ids.LegacyCases[i];
                Assert.That(lt.Id, Is.EqualTo("LT-" + (i + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture)));
                byFile.TryGetValue(lt.File, out var count);
                byFile[lt.File] = count + 1;
            }

            Assert.That(byFile["test/rowing_simulator_test.dart"], Is.EqualTo(7));
            Assert.That(byFile["test/scan_filter_test.dart"], Is.EqualTo(2));
            Assert.That(byFile["test/heart_rate_parser_test.dart"], Is.EqualTo(6));
            Assert.That(byFile["test/routine_positions_test.dart"], Is.EqualTo(1));
            Assert.That(byFile["test/session_stats_test.dart"], Is.EqualTo(3));
            foreach (var id in It01Ids.WorkPackageAIds)
            {
                Assert.That(ids.Contains(id), Is.True, id);
            }
        }

        [Test, Category("AC-BASE-09")]
        public void WorkPackageAIdsHaveTests()
        {
            // Lo que cubre WP-A ya tiene test aunque B y C no hayan terminado.
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var id in It01Ids.WorkPackageAIds)
            {
                if (!categories.ContainsKey(id))
                {
                    missing.Add(id);
                }
            }

            Assert.That(missing, Is.Empty, "AC de WP-A sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09")]
        public void NoUnknownIdsInCategories()
        {
            var known = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var lt in It01Ids.LegacyCases)
            {
                known.Add(lt.Id);
            }

            foreach (var ac in It02Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var lt in It02Ids.LegacyCases)
            {
                known.Add(lt.Id);
            }

            var unknown = new List<string>();
            foreach (var entry in Scan().Categories)
            {
                if ((entry.Key.StartsWith("AC-", StringComparison.Ordinal) || entry.Key.StartsWith("LT-", StringComparison.Ordinal)) && !known.Contains(entry.Key))
                {
                    unknown.Add(entry.Key + " (" + string.Join(", ", entry.Value) + ")");
                }
            }

            Assert.That(unknown, Is.Empty, "categorías con un ID que no está en la spec:\n" + string.Join("\n", unknown));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void EveryAcceptanceIdHasATest()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var id in It01Ids.AcceptanceIds)
            {
                if (!categories.ContainsKey(id))
                {
                    missing.Add(id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " de 139 AC sin ningún test con su categoría: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void EveryLegacyCaseKeepsItsName()
        {
            var scan = Scan();
            var problems = new List<string>();
            foreach (var lt in It01Ids.LegacyCases)
            {
                if (!scan.TestNames.TryGetValue(lt.Name, out var files))
                {
                    problems.Add(lt.Id + ": ningún TestName = \"" + lt.Name + "\"");
                    continue;
                }

                if (!scan.Categories.ContainsKey(lt.Id))
                {
                    problems.Add(lt.Id + ": falta la categoría " + lt.Id);
                }

                var citation = "// legacy: " + lt.File + ":" + lt.Line + " '" + lt.Name + "'";
                var cited = false;
                foreach (var file in files)
                {
                    if (scan.RawText[file].Contains(citation))
                    {
                        cited = true;
                    }
                }

                if (!cited)
                {
                    problems.Add(lt.Id + ": falta el comentario de origen " + citation);
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void DemoCoversSpec()
        {
            // 02-tech-design §8.7 (QA-13): los cuatro tests de la demo llevan Demo y solo ellos escriben líneas de demo.
            var scan = Scan();
            var marker = "[" + "DEMO]";
            var demoFiles = new HashSet<string>(StringComparer.Ordinal);
            var problems = new List<string>();
            foreach (var demo in It01Ids.DemoTests)
            {
                string? found = null;
                foreach (var entry in scan.Code)
                {
                    if (Regex.IsMatch(entry.Value, @"\bclass\s+" + demo.Class + @"\b"))
                    {
                        found = entry.Key;
                    }
                }

                if (found == null)
                {
                    problems.Add(demo.Class + ": no existe");
                    continue;
                }

                demoFiles.Add(found);
                var code = scan.Code[found];
                if (!Regex.IsMatch(code, @"\b" + demo.Method + @"\b"))
                {
                    problems.Add(demo.Class + "." + demo.Method + ": no existe");
                }

                if (!(code.Contains("Category(\"Demo\")") || code.Contains("SetCategory(\"Demo\")") || Regex.IsMatch(code, @"Category\s*=\s*""[^""]*\bDemo\b")))
                {
                    problems.Add(demo.Class + ": falta la categoría Demo");
                }

                if (!code.Contains(marker))
                {
                    problems.Add(demo.Class + ": no escribe ninguna línea " + marker);
                }
            }

            foreach (var entry in scan.Code)
            {
                if (!demoFiles.Contains(entry.Key) && entry.Value.Contains(marker))
                {
                    problems.Add(entry.Key + ": escribe " + marker + " sin ser un test de la demo");
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // ------------------------------------------------------------------ It. 2 (02-tech-design §5.2, QA-G2-16)

        [Test, Category("AC-BASE-09")]
        public void It02IdsAreComplete()
        {
            // 125 AC = 113 nuevos (APP 30, BLE 48, HRM 35) + 12 de la It. 1; 15 LT = LT-20 a LT-34 (01-po-spec, encabezado, §5, §6).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var fromIt01 = new HashSet<string>(It02Ids.FromIt01, StringComparer.Ordinal);
            var it01 = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            int app = 0, ble = 0, hrm = 0;
            foreach (var ac in It02Ids.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It02Ids: " + ac.Id);
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                if (fromIt01.Contains(ac.Id))
                {
                    Assert.That(it01.Contains(ac.Id), Is.True, ac.Id + " figura como de la It. 1 pero no está en It01Ids");
                    continue;
                }

                Assert.That(it01.Contains(ac.Id), Is.False, ac.Id + " ya es de la It. 1: va en FromIt01");
                app += ac.Id.StartsWith("AC-APP-", StringComparison.Ordinal) ? 1 : 0;
                ble += ac.Id.StartsWith("AC-BLE-", StringComparison.Ordinal) ? 1 : 0;
                hrm += ac.Id.StartsWith("AC-HRM-", StringComparison.Ordinal) ? 1 : 0;
            }

            Assert.That(It02Ids.Acceptance.Length, Is.EqualTo(125));
            Assert.That(It02Ids.FromIt01.Length, Is.EqualTo(12));
            Assert.That(app, Is.EqualTo(30));
            Assert.That(ble, Is.EqualTo(48));
            Assert.That(hrm, Is.EqualTo(35));
            foreach (var id in It02Ids.FromIt01)
            {
                Assert.That(ids.Contains(id), Is.True, id);
            }

            Assert.That(It02Ids.LegacyCases.Length, Is.EqualTo(15));
            var byFile = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < It02Ids.LegacyCases.Length; i++)
            {
                var lt = It02Ids.LegacyCases[i];
                Assert.That(lt.Id, Is.EqualTo("LT-" + (i + 20).ToString("00", System.Globalization.CultureInfo.InvariantCulture)));
                Assert.That(LtPattern.IsMatch(lt.Id), Is.True);
                Assert.That(lt.Checks == "s" || lt.Checks == "ps", Is.True, lt.Id + ": [s] o [ps]");
                byFile.TryGetValue(lt.File, out var count);
                byFile[lt.File] = count + 1;
            }

            Assert.That(byFile["test/simulated_ble_service_test.dart"], Is.EqualTo(3));
            Assert.That(byFile["test/simulated_heart_rate_service_test.dart"], Is.EqualTo(3));
            Assert.That(byFile["test/simulator_overlay_test.dart"], Is.EqualTo(2));
            Assert.That(byFile["test/heart_rate_card_test.dart"], Is.EqualTo(4));
            Assert.That(byFile["test/heart_rate_provider_test.dart"], Is.EqualTo(3));

            // UnityOnlyIds y WorkPackageBIds (los lee run-tests.ps1) son exactamente los que salen de la columna de verificación.
            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            foreach (var ac in It02Ids.Acceptance)
            {
                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            foreach (var lt in It02Ids.LegacyCases)
            {
                Classify(lt.Id, lt.Checks, unityOnly, workPackageB);
            }

            Assert.That(It02Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It02Ids.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(unityOnly.Count, Is.EqualTo(36), "30 AC y 6 LT [ps]");
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It02DotnetIdsHaveTests()
        {
            // Lo que el plan verifica con dotnet ([d] o [s] en 02-tech-design §5.2) tiene al menos un test con su categoría.
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It02Ids.Acceptance)
            {
                if (HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s"))
                {
                    if (!categories.ContainsKey(ac.Id))
                    {
                        missing.Add(ac.Id);
                    }
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " AC [d]/[s] de la It. 2 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It02LegacyCasesKeepTheirNames()
        {
            // Los 9 [s] en Tests/Simulator (dotnet); los 6 [ps] en Tests/PlayModeSimulator (WP-B: si no existe, se ignoran).
            var dotnet = Scan();
            var playModeSimulator = ScanFolders("Tests/PlayModeSimulator");
            var problems = new List<string>();
            var skipped = 0;
            foreach (var lt in It02Ids.LegacyCases)
            {
                var index = lt.Checks == "s" ? dotnet : playModeSimulator;
                if (index == null)
                {
                    skipped++;
                    continue;
                }

                CheckLegacyCase(index, lt.Id, lt.File, lt.Line, lt.Name, problems);
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            if (skipped > 0)
            {
                TestContext.Out.WriteLine(skipped + " LT [ps] pendientes de WP-B (todavía no existe Tests/PlayModeSimulator).");
            }
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It02UnityOnlyIdsHaveAUnityLocation()
        {
            // Cada UnityOnlyIds tiene su categoría en la carpeta que dice su verificación (p → Tests/PlayMode, ps →
            // Tests/PlayModeSimulator, u → Tests/UnityEditMode) o figura en el catálogo de capturas (cap → Snapshots, por texto).
            // Una fila cuyas carpetas todavía no existen se ignora con el motivo (pendiente WP-B).
            var folders = new Dictionary<string, SourceIndex?>(StringComparer.Ordinal)
            {
                ["p"] = ScanFolders("Tests/PlayMode"),
                ["ps"] = ScanFolders("Tests/PlayModeSimulator"),
                ["u"] = ScanFolders("Tests/UnityEditMode"),
                ["cap"] = ScanFolders("Snapshots"),
            };
            var checks = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var ac in It02Ids.Acceptance)
            {
                checks[ac.Id] = ac.Checks;
            }

            foreach (var lt in It02Ids.LegacyCases)
            {
                checks[lt.Id] = lt.Checks;
            }

            var missing = new List<string>();
            var pending = new List<string>();
            foreach (var id in It02Ids.UnityOnlyIds)
            {
                var anyFolder = false;
                var found = false;
                foreach (var kind in folders.Keys)
                {
                    var index = folders[kind];
                    if (!HasKind(checks[id], kind) || index == null)
                    {
                        continue;
                    }

                    anyFolder = true;
                    found |= kind == "cap" ? index.MentionsText(id) : index.Categories.ContainsKey(id);
                }

                if (!anyFolder)
                {
                    pending.Add(id);
                }
                else if (!found)
                {
                    missing.Add(id + " (" + checks[id] + ")");
                }
            }

            Assert.That(missing, Is.Empty, "IDs solo de Unity sin su prueba en la carpeta esperada:\n" + string.Join("\n", missing));
            if (pending.Count == It02Ids.UnityOnlyIds.Length)
            {
                Assert.Ignore("pendiente WP-B: todavía no existen Tests/PlayMode*, Tests/UnityEditMode ni Snapshots con pruebas de la It. 2 ("
                    + pending.Count + " IDs)");
            }

            if (pending.Count > 0)
            {
                TestContext.Out.WriteLine("pendientes de WP-B (sin su carpeta todavía): " + string.Join(", ", pending));
            }
        }

        [Test, Category("AC-BASE-09")]
        public void ScannerReadsLiteralCategoriesAndNames()
        {
            var sample = "[Test, Category(\"AC-X-01\"), Category(\"LT-01\")]\n[TestCase(1, Category = \"AC-Y-02,Demo\", TestName = \"a \\\"b\\\" →\")]\n"
                + "x.SetCategory(\"AC-Z-03\").SetName(\"n · m\");\n// [Category(\"AC-COMMENT-99\")]";
            var scan = new SourceIndex();
            scan.Add("sample.cs", sample);
            Assert.That(scan.Categories.Keys, Is.EquivalentTo(new[] { "AC-X-01", "LT-01", "AC-Y-02", "Demo", "AC-Z-03" }));
            Assert.That(scan.TestNames.Keys, Is.EquivalentTo(new[] { "a \"b\" →", "n · m" }));
        }

        private static int CountWithPrefix(string prefix)
        {
            var count = 0;
            foreach (var id in It01Ids.AcceptanceIds)
            {
                if (id.StartsWith(prefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static SourceIndex Scan()
        {
            var index = new SourceIndex();
            foreach (var file in SourceScan.CsFiles("Tests/EditMode", "Tests/Simulator"))
            {
                index.Add(RepoPaths.Relative(file), File.ReadAllText(file));
            }

            return index;
        }

        // Índice de una carpeta de Unity (WP-B), o null si todavía no tiene pruebas: no existe, o (Tests/PlayMode de la It. 1)
        // solo tiene el asmdef y el marcador. Snapshots cuenta con cualquier fuente (el catálogo se mira por texto).
        private static SourceIndex? ScanFolders(string folder)
        {
            var files = SourceScan.CsFiles(folder);
            if (files.Count == 0)
            {
                return null;
            }

            var index = new SourceIndex();
            foreach (var file in files)
            {
                index.Add(RepoPaths.Relative(file), File.ReadAllText(file));
            }

            return folder == "Snapshots" || index.Categories.Count > 0 ? index : null;
        }

        private static readonly HashSet<string> Kinds = new HashSet<string>(StringComparer.Ordinal)
        {
            "d", "dB", "s", "u", "p", "ps", "cap", "eq", "qa", "ci",
        };

        private static void CheckKinds(string id, string checks)
        {
            Assert.That(checks.Trim().Length, Is.GreaterThan(0), id + " sin verificación");
            foreach (var kind in checks.Split(' '))
            {
                Assert.That(Kinds.Contains(kind), Is.True, id + ": verificación desconocida \"" + kind + "\"");
            }
        }

        private static bool HasKind(string checks, string kind)
        {
            foreach (var part in checks.Split(' '))
            {
                if (part == kind)
                {
                    return true;
                }
            }

            return false;
        }

        // Sin d ni s: solo de Unity (si tiene p, ps, u o cap) o de un archivo de dotnet de WP-B (si solo tiene dB).
        private static void Classify(string id, string checks, List<string> unityOnly, List<string> workPackageB)
        {
            if (HasKind(checks, "d") || HasKind(checks, "s"))
            {
                return;
            }

            if (HasKind(checks, "p") || HasKind(checks, "ps") || HasKind(checks, "u") || HasKind(checks, "cap"))
            {
                unityOnly.Add(id);
            }
            else if (HasKind(checks, "dB"))
            {
                workPackageB.Add(id);
            }
            else
            {
                Assert.Fail(id + ": sin ninguna verificación automática (" + checks + ")");
            }
        }

        // TestName exacto, la categoría del LT y el comentario de origen (convención de la It. 1).
        private static void CheckLegacyCase(SourceIndex index, string id, string file, int line, string name, List<string> problems)
        {
            if (!index.TestNames.TryGetValue(name, out var files))
            {
                problems.Add(id + ": ningún TestName = \"" + name + "\"");
                return;
            }

            if (!index.Categories.ContainsKey(id))
            {
                problems.Add(id + ": falta la categoría " + id);
            }

            var citation = "// legacy: " + file + ":" + line.ToString(System.Globalization.CultureInfo.InvariantCulture) + " '" + name + "'";
            var cited = false;
            foreach (var source in files)
            {
                if (index.RawText[source].Contains(citation))
                {
                    cited = true;
                }
            }

            if (!cited)
            {
                problems.Add(id + ": falta el comentario de origen " + citation);
            }
        }

        private static string Unescape(string literal)
        {
            var sb = new StringBuilder(literal.Length);
            for (var i = 0; i < literal.Length; i++)
            {
                var c = literal[i];
                if (c != '\\' || i + 1 >= literal.Length)
                {
                    sb.Append(c);
                    continue;
                }

                var e = literal[++i];
                switch (e)
                {
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'u':
                        if (i + 4 < literal.Length)
                        {
                            sb.Append((char)Convert.ToInt32(literal.Substring(i + 1, 4), 16));
                            i += 4;
                        }

                        break;
                    default:
                        sb.Append(e);
                        break;
                }
            }

            return sb.ToString();
        }

        // Índice de categorías y nombres literales por archivo.
        private sealed class SourceIndex
        {
            public Dictionary<string, List<string>> Categories { get; } = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            public Dictionary<string, List<string>> TestNames { get; } = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            public Dictionary<string, string> Code { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

            public Dictionary<string, string> RawText { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

            // El texto aparece en el código (sin comentarios) de algún archivo (para el catálogo de capturas).
            public bool MentionsText(string text)
            {
                foreach (var code in Code.Values)
                {
                    if (code.Contains(text))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Add(string file, string text)
            {
                var raw = text.Replace("\r\n", "\n");
                var code = SourceScan.StripComments(raw);
                RawText[file] = raw;
                Code[file] = code;
                foreach (var regex in new[] { CategoryAttribute, CategoryProperty, SetCategory })
                {
                    foreach (Match match in regex.Matches(code))
                    {
                        foreach (var part in Unescape(match.Groups[1].Value).Split(','))
                        {
                            var category = part.Trim();
                            if (category.Length > 0)
                            {
                                AddTo(Categories, category, file);
                            }
                        }
                    }
                }

                foreach (var regex in new[] { TestNameProperty, SetName })
                {
                    foreach (Match match in regex.Matches(code))
                    {
                        AddTo(TestNames, Unescape(match.Groups[1].Value), file);
                    }
                }
            }

            private static void AddTo(Dictionary<string, List<string>> map, string key, string file)
            {
                if (!map.TryGetValue(key, out var files))
                {
                    files = new List<string>();
                    map[key] = files;
                }

                if (!files.Contains(file))
                {
                    files.Add(file);
                }
            }
        }
    }
}
