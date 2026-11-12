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
        // It. 15: también AC-D15-nn (letras y cifras en el prefijo). It. 6b: también AC-6B-nn (cifras y letras).
        private static readonly Regex AcPattern = new Regex(@"^AC-(?:[A-Z]+[0-9]*|[0-9]+[A-Z]+)-\d\d(-\d\d)?$");
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

            foreach (var ac in It03Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var ac in It04aIds.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var lt in It04aIds.LegacyCases)
            {
                known.Add(lt.Id);
            }

            foreach (var ac in It04bIds.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var lt in It04bIds.LegacyCases)
            {
                known.Add(lt.Id);
            }

            foreach (var ac in It05Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var lt in It05Ids.LegacyCases)
            {
                known.Add(lt.Id);
            }

            foreach (var ac in It06aIds.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var lt in It06aIds.LegacyCases)
            {
                known.Add(lt.Id);
            }

            foreach (var ac in It07Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var lt in It07Ids.LegacyCases)
            {
                known.Add(lt.Id);
            }

            foreach (var ac in It08Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var lt in It08Ids.LegacyCases)
            {
                known.Add(lt.Id);
            }

            foreach (var ac in It09Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var ac in It10aIds.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var ac in It11Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var lt in It11Ids.LegacyCases)
            {
                known.Add(lt.Id);
            }

            foreach (var ac in It12Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var ac in It14Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var ac in It15Ids.Acceptance)
            {
                known.Add(ac.Id);
            }

            foreach (var ac in It06bIds.Acceptance)
            {
                known.Add(ac.Id);
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

        // ------------------------------------------------------------------ It. 3 (02-tech-design §6.3)

        [Test, Category("AC-BASE-09")]
        public void It03IdsAreComplete()
        {
            // 102 AC = 91 nuevos (RUT 70, HIS 10, APP 11) + 11 de iteraciones anteriores (§5); sin casos legacy (spec §6).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var earlier = new HashSet<string>(It03Ids.FromEarlier, StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var ac in It02Ids.Acceptance)
            {
                before.Add(ac.Id);
            }

            int rut = 0, his = 0, app = 0;
            foreach (var ac in It03Ids.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It03Ids: " + ac.Id);
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                if (earlier.Contains(ac.Id))
                {
                    Assert.That(before.Contains(ac.Id), Is.True, ac.Id + " figura como de una iteración anterior pero no está en It01Ids ni en It02Ids");
                    continue;
                }

                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior: va en FromEarlier");
                rut += ac.Id.StartsWith("AC-RUT-", StringComparison.Ordinal) ? 1 : 0;
                his += ac.Id.StartsWith("AC-HIS-", StringComparison.Ordinal) ? 1 : 0;
                app += ac.Id.StartsWith("AC-APP-", StringComparison.Ordinal) ? 1 : 0;
            }

            Assert.That(It03Ids.Acceptance.Length, Is.EqualTo(102));
            Assert.That(It03Ids.FromEarlier.Length, Is.EqualTo(11));
            Assert.That(rut, Is.EqualTo(70));
            Assert.That(his, Is.EqualTo(10));
            Assert.That(app, Is.EqualTo(11));
            foreach (var id in It03Ids.FromEarlier)
            {
                Assert.That(ids.Contains(id), Is.True, id);
            }

            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            foreach (var ac in It03Ids.Acceptance)
            {
                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            Assert.That(It03Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It03Ids.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(unityOnly.Count, Is.EqualTo(42), "34 nuevos y 8 de §5");
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It03DotnetIdsHaveTests()
        {
            // Lo que el plan verifica con dotnet ([d] o [s] en 02-tech-design §6.3) tiene al menos un test con su categoría en
            // Tests/EditMode o Tests/Simulator (WP-A).
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It03Ids.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " AC [d]/[s] de la It. 3 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It03UnityOnlyIdsHaveAUnityLocation()
        {
            // Como It02UnityOnlyIdsHaveAUnityLocation. Mientras ninguno de los NUEVOS de la It. 3 tenga su prueba de Unity
            // (WP-B no empezó), se ignora con el motivo; desde que aparece uno, se exigen todos.
            var folders = new Dictionary<string, SourceIndex?>(StringComparer.Ordinal)
            {
                ["p"] = ScanFolders("Tests/PlayMode"),
                ["ps"] = ScanFolders("Tests/PlayModeSimulator"),
                ["u"] = ScanFolders("Tests/UnityEditMode"),
                ["cap"] = ScanFolders("Snapshots"),
            };
            var checks = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var ac in It03Ids.Acceptance)
            {
                checks[ac.Id] = ac.Checks;
            }

            var earlier = new HashSet<string>(It03Ids.FromEarlier, StringComparer.Ordinal);
            var missing = new List<string>();
            var newFound = 0;
            foreach (var id in It03Ids.UnityOnlyIds)
            {
                var found = false;
                foreach (var kind in folders.Keys)
                {
                    var index = folders[kind];
                    if (!HasKind(checks[id], kind) || index == null)
                    {
                        continue;
                    }

                    found |= kind == "cap" ? index.MentionsText(id) : index.Categories.ContainsKey(id);
                }

                if (found && !earlier.Contains(id))
                {
                    newFound++;
                }

                if (!found)
                {
                    missing.Add(id + " (" + checks[id] + ")");
                }
            }

            if (newFound == 0)
            {
                Assert.Ignore("pendiente WP-B: todavía no hay pruebas de Unity de la It. 3 (" + missing.Count + " IDs sin su prueba)");
            }

            Assert.That(missing, Is.Empty, "IDs de la It. 3 solo de Unity sin su prueba en la carpeta esperada: " + string.Join(", ", missing));
        }

        // ------------------------------------------------------------------ It. 4a (02-tech-design §8.3, §8.4)

        [Test, Category("AC-BASE-09")]
        public void It04aIdsAreComplete()
        {
            // 162 AC = 147 nuevos (WKT 130, BLE 5, APP 8, HIS 3, SCN 1) + 15 de iteraciones anteriores (§5); 11 LT = LT-35 a
            // LT-45 (01-po-spec §4.0, §5, §6).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var earlier = new HashSet<string>(It04aIds.FromEarlier, StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var ac in It02Ids.Acceptance)
            {
                before.Add(ac.Id);
            }

            foreach (var ac in It03Ids.Acceptance)
            {
                before.Add(ac.Id);
            }

            int wkt = 0, ble = 0, app = 0, his = 0, scn = 0;
            foreach (var ac in It04aIds.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It04aIds: " + ac.Id);
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                if (earlier.Contains(ac.Id))
                {
                    Assert.That(before.Contains(ac.Id), Is.True, ac.Id + " figura como de una iteración anterior pero no está en It01Ids, It02Ids ni It03Ids");
                    continue;
                }

                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior: va en FromEarlier");
                wkt += ac.Id.StartsWith("AC-WKT-", StringComparison.Ordinal) ? 1 : 0;
                ble += ac.Id.StartsWith("AC-BLE-", StringComparison.Ordinal) ? 1 : 0;
                app += ac.Id.StartsWith("AC-APP-", StringComparison.Ordinal) ? 1 : 0;
                his += ac.Id.StartsWith("AC-HIS-", StringComparison.Ordinal) ? 1 : 0;
                scn += ac.Id.StartsWith("AC-SCN-", StringComparison.Ordinal) ? 1 : 0;
            }

            Assert.That(It04aIds.Acceptance.Length, Is.EqualTo(162));
            Assert.That(It04aIds.FromEarlier.Length, Is.EqualTo(15));
            Assert.That(wkt, Is.EqualTo(130));
            Assert.That(ble, Is.EqualTo(5));
            Assert.That(app, Is.EqualTo(8));
            Assert.That(his, Is.EqualTo(3));
            Assert.That(scn, Is.EqualTo(1));
            foreach (var id in It04aIds.FromEarlier)
            {
                Assert.That(ids.Contains(id), Is.True, id);
            }

            Assert.That(It04aIds.LegacyCases.Length, Is.EqualTo(11));
            var byFile = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < It04aIds.LegacyCases.Length; i++)
            {
                var lt = It04aIds.LegacyCases[i];
                Assert.That(lt.Id, Is.EqualTo("LT-" + (i + 35).ToString("00", System.Globalization.CultureInfo.InvariantCulture)));
                Assert.That(LtPattern.IsMatch(lt.Id), Is.True);
                Assert.That(lt.Checks, Is.EqualTo("d"), lt.Id + ": [d]");
                byFile.TryGetValue(lt.File, out var count);
                byFile[lt.File] = count + 1;
            }

            Assert.That(byFile["test/workout_provider_hr_test.dart"], Is.EqualTo(4));
            Assert.That(byFile["test/step_target_tracker_test.dart"], Is.EqualTo(6));
            Assert.That(byFile["test/series_panels_test.dart"], Is.EqualTo(1));

            // UnityOnlyIds y WorkPackageBIds (los lee run-tests.ps1) son exactamente los que salen de la columna de verificación.
            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            foreach (var ac in It04aIds.Acceptance)
            {
                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            Assert.That(It04aIds.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It04aIds.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(unityOnly.Count, Is.EqualTo(54), "44 nuevos y 10 de §5");
            Assert.That(workPackageB, Is.Empty);
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It04aDotnetIdsHaveTests()
        {
            // Lo que el plan verifica con dotnet ([d] o [s] en 02-tech-design §8.4) tiene al menos un test con su categoría en
            // Tests/EditMode o Tests/Simulator (WP-A); también los 11 LT.
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It04aIds.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            foreach (var lt in It04aIds.LegacyCases)
            {
                if (!categories.ContainsKey(lt.Id))
                {
                    missing.Add(lt.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d]/[s] de la It. 4a sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It04aLegacyCasesKeepTheirNames()
        {
            // Convención de ARCHITECTURE §15.3: el nombre del legacy al carácter (TestName), la categoría del LT y el comentario
            // "// legacy: test/…:línea 'nombre'".
            var scan = Scan();
            var problems = new List<string>();
            foreach (var lt in It04aIds.LegacyCases)
            {
                CheckLegacyCase(scan, lt.Id, lt.File, lt.Line, lt.Name, problems);
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It04aUnityOnlyIdsHaveAUnityLocation()
        {
            // Como It03UnityOnlyIdsHaveAUnityLocation. Mientras ninguno de los NUEVOS de la It. 4a tenga su prueba de Unity
            // (WP-B), se ignora con el motivo; desde que aparece uno, se exigen todos.
            var folders = new Dictionary<string, SourceIndex?>(StringComparer.Ordinal)
            {
                ["p"] = ScanFolders("Tests/PlayMode"),
                ["ps"] = ScanFolders("Tests/PlayModeSimulator"),
                ["u"] = ScanFolders("Tests/UnityEditMode"),
                ["cap"] = ScanFolders("Snapshots"),
            };
            var checks = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var ac in It04aIds.Acceptance)
            {
                checks[ac.Id] = ac.Checks;
            }

            var earlier = new HashSet<string>(It04aIds.FromEarlier, StringComparer.Ordinal);
            var missing = new List<string>();
            var newFound = 0;
            foreach (var id in It04aIds.UnityOnlyIds)
            {
                var found = false;
                foreach (var kind in folders.Keys)
                {
                    var index = folders[kind];
                    if (!HasKind(checks[id], kind) || index == null)
                    {
                        continue;
                    }

                    found |= kind == "cap" ? index.MentionsText(id) : index.Categories.ContainsKey(id);
                }

                if (found && !earlier.Contains(id))
                {
                    newFound++;
                }

                if (!found)
                {
                    missing.Add(id + " (" + checks[id] + ")");
                }
            }

            if (newFound == 0)
            {
                Assert.Ignore("pendiente WP-B: todavía no hay pruebas de Unity de la It. 4a (" + missing.Count + " IDs sin su prueba)");
            }

            Assert.That(missing, Is.Empty, "IDs de la It. 4a solo de Unity sin su prueba en la carpeta esperada: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It04aUnityOnlyIdsHaveNoDotnetResult()
        {
            // Un ID solo de Unity con un test de dotnet saldría PASA en la matriz sin su prueba de pantalla: los de la It. 4a no
            // llevan su categoría en Tests/EditMode ni en Tests/Simulator (sí en un [Ignore] de equipo, que deja PENDIENTE UNITY).
            // It. 4b: los que la It. 4b vuelve a verificar con dotnet (§5 de su spec: AC-WKT-14-01, -14-03, -14-04, AC-APP-03-11,
            // AC-APP-05-08…) tienen su prueba de dotnet de la 4b; cuentan para la matriz de la 4b, no como un falso PASA de la 4a.
            var scan = Scan();
            var earlier = new HashSet<string>(It04aIds.FromEarlier, StringComparer.Ordinal);
            foreach (var ac in It04bIds.Acceptance)
            {
                if (HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s"))
                {
                    earlier.Add(ac.Id);
                }
            }

            // It. 5: lo mismo con los que la It. 5 vuelve a verificar con dotnet (AC-WKT-17-09, AC-WKT-13-13…).
            foreach (var ac in It05Ids.Acceptance)
            {
                if (HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s"))
                {
                    earlier.Add(ac.Id);
                }
            }

            // It. 6a: lo mismo con los que la It. 6a vuelve a verificar con dotnet (AC-WKT-13-15, AC-APP-05-08, AC-BLE-15-09…).
            foreach (var ac in It06aIds.Acceptance)
            {
                if (HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s"))
                {
                    earlier.Add(ac.Id);
                }
            }

            var offenders = new List<string>();
            foreach (var id in It04aIds.UnityOnlyIds)
            {
                if (earlier.Contains(id) || !scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    if (!file.EndsWith("WorkoutDeviceChecksTests.cs", StringComparison.Ordinal))
                    {
                        offenders.Add(id + " en " + file);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        // ------------------------------------------------------------------ It. 4b (02-tech-design §6.3, §6.4)

        [Test, Category("AC-BASE-09")]
        public void It04bIdsAreComplete()
        {
            // 101 AC = 61 nuevos (WKT 56, APP 5) + 40 de iteraciones anteriores (§5); LT-46 (01-po-spec §4.0, §5, §6).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var earlier = new HashSet<string>(It04bIds.FromEarlier, StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var ac in It02Ids.Acceptance)
            {
                before.Add(ac.Id);
            }

            foreach (var ac in It03Ids.Acceptance)
            {
                before.Add(ac.Id);
            }

            foreach (var ac in It04aIds.Acceptance)
            {
                before.Add(ac.Id);
            }

            int wkt = 0, app = 0;
            foreach (var ac in It04bIds.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It04bIds: " + ac.Id);
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                if (earlier.Contains(ac.Id))
                {
                    Assert.That(before.Contains(ac.Id), Is.True, ac.Id + " figura como de una iteración anterior pero no está en It01Ids a It04aIds");
                    continue;
                }

                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior: va en FromEarlier");
                wkt += ac.Id.StartsWith("AC-WKT-", StringComparison.Ordinal) ? 1 : 0;
                app += ac.Id.StartsWith("AC-APP-", StringComparison.Ordinal) ? 1 : 0;
            }

            Assert.That(It04bIds.Acceptance.Length, Is.EqualTo(101));
            Assert.That(It04bIds.FromEarlier.Length, Is.EqualTo(40));
            Assert.That(wkt, Is.EqualTo(56));
            Assert.That(app, Is.EqualTo(5));
            foreach (var id in It04bIds.FromEarlier)
            {
                Assert.That(ids.Contains(id), Is.True, id);
            }

            Assert.That(It04bIds.LegacyCases.Length, Is.EqualTo(1));
            Assert.That(It04bIds.LegacyCases[0].Id, Is.EqualTo("LT-46"), "la numeración sigue a la de la 4a (LT-45)");
            Assert.That(It04bIds.LegacyCases[0].File, Is.EqualTo("test/step_pill_test.dart"));
            Assert.That(It04bIds.LegacyCases[0].Checks, Is.EqualTo("d p"));

            // UnityOnlyIds y WorkPackageBIds (los lee run-tests.ps1) son exactamente los que salen de la columna de verificación.
            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            foreach (var ac in It04bIds.Acceptance)
            {
                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            Assert.That(It04bIds.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It04bIds.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(unityOnly.Count, Is.EqualTo(36), "02-tech-design §6.4");
            Assert.That(workPackageB, Is.Empty);
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It04bDotnetIdsHaveTests()
        {
            // Lo que el plan verifica con dotnet ([d] o [s] en 02-tech-design §6.4) tiene al menos un test con su categoría en
            // Tests/EditMode o Tests/Simulator (WP-A); también LT-46.
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It04bIds.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            foreach (var lt in It04bIds.LegacyCases)
            {
                if (!categories.ContainsKey(lt.Id))
                {
                    missing.Add(lt.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d]/[s] de la It. 4b sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It04bLegacyCasesKeepTheirNames()
        {
            var scan = Scan();
            var problems = new List<string>();
            foreach (var lt in It04bIds.LegacyCases)
            {
                CheckLegacyCase(scan, lt.Id, lt.File, lt.Line, lt.Name, problems);
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It04bUnityOnlyIdsHaveAUnityLocation()
        {
            // Como It04aUnityOnlyIdsHaveAUnityLocation. Mientras ninguno de los NUEVOS de la It. 4b tenga su prueba de Unity
            // (WP-B), se ignora con el motivo; desde que aparece uno, se exigen todos.
            var folders = new Dictionary<string, SourceIndex?>(StringComparer.Ordinal)
            {
                ["p"] = ScanFolders("Tests/PlayMode"),
                ["ps"] = ScanFolders("Tests/PlayModeSimulator"),
                ["u"] = ScanFolders("Tests/UnityEditMode"),
                ["cap"] = ScanFolders("Snapshots"),
            };
            var checks = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var ac in It04bIds.Acceptance)
            {
                checks[ac.Id] = ac.Checks;
            }

            var earlier = new HashSet<string>(It04bIds.FromEarlier, StringComparer.Ordinal);
            var missing = new List<string>();
            var newFound = 0;
            foreach (var id in It04bIds.UnityOnlyIds)
            {
                var found = false;
                foreach (var kind in folders.Keys)
                {
                    var index = folders[kind];
                    if (!HasKind(checks[id], kind) || index == null)
                    {
                        continue;
                    }

                    found |= kind == "cap" ? index.MentionsText(id) : index.Categories.ContainsKey(id);
                }

                if (found && !earlier.Contains(id))
                {
                    newFound++;
                }

                if (!found)
                {
                    missing.Add(id + " (" + checks[id] + ")");
                }
            }

            if (newFound == 0)
            {
                Assert.Ignore("pendiente WP-B: todavía no hay pruebas de Unity de la It. 4b (" + missing.Count + " IDs sin su prueba)");
            }

            Assert.That(missing, Is.Empty, "IDs de la It. 4b solo de Unity sin su prueba en la carpeta esperada: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It04bUnityOnlyIdsHaveNoDotnetResult()
        {
            // Los nuevos de la It. 4b solo de Unity no llevan su categoría en Tests/EditMode ni en Tests/Simulator (sí en un
            // [Ignore] de equipo, que deja PENDIENTE UNITY).
            var scan = Scan();
            var earlier = new HashSet<string>(It04bIds.FromEarlier, StringComparer.Ordinal);
            foreach (var ac in It05Ids.Acceptance)
            {
                if (HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s"))
                {
                    earlier.Add(ac.Id);
                }
            }

            // It. 6a: lo mismo con los que la It. 6a vuelve a verificar con dotnet (AC-WKT-13-15, AC-APP-05-08, AC-BLE-15-09…).
            foreach (var ac in It06aIds.Acceptance)
            {
                if (HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s"))
                {
                    earlier.Add(ac.Id);
                }
            }

            var offenders = new List<string>();
            foreach (var id in It04bIds.UnityOnlyIds)
            {
                if (earlier.Contains(id) || !scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    if (!file.EndsWith("WorkoutDeviceChecksTests.cs", StringComparison.Ordinal))
                    {
                        offenders.Add(id + " en " + file);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        // ------------------------------------------------------------------ It. 5 (02-tech-design §6.3, §6.5)

        [Test, Category("AC-BASE-09")]
        public void It05IdsAreComplete()
        {
            // 74 AC = 46 nuevos (WKT 45, APP 1) + 28 de iteraciones anteriores (§5.2 y §5.3); LT-47 a LT-61 (01-po-spec §4.0, §6).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var earlier = new HashSet<string>(It05Ids.FromEarlier, StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var ac in It02Ids.Acceptance)
            {
                before.Add(ac.Id);
            }

            foreach (var ac in It03Ids.Acceptance)
            {
                before.Add(ac.Id);
            }

            foreach (var ac in It04aIds.Acceptance)
            {
                before.Add(ac.Id);
            }

            foreach (var ac in It04bIds.Acceptance)
            {
                before.Add(ac.Id);
            }

            int wkt = 0, app = 0;
            foreach (var ac in It05Ids.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It05Ids: " + ac.Id);
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                if (earlier.Contains(ac.Id))
                {
                    Assert.That(before.Contains(ac.Id), Is.True, ac.Id + " figura como de una iteración anterior pero no está en It01Ids a It04bIds");
                    continue;
                }

                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior: va en FromEarlier");
                wkt += ac.Id.StartsWith("AC-WKT-", StringComparison.Ordinal) ? 1 : 0;
                app += ac.Id.StartsWith("AC-APP-", StringComparison.Ordinal) ? 1 : 0;
            }

            Assert.That(It05Ids.Acceptance.Length, Is.EqualTo(74));
            Assert.That(It05Ids.FromEarlier.Length, Is.EqualTo(28));
            Assert.That(wkt, Is.EqualTo(45));
            Assert.That(app, Is.EqualTo(1));
            foreach (var id in It05Ids.FromEarlier)
            {
                Assert.That(ids.Contains(id), Is.True, id);
            }

            Assert.That(It05Ids.LegacyCases.Length, Is.EqualTo(15));
            for (var i = 0; i < It05Ids.LegacyCases.Length; i++)
            {
                var lt = It05Ids.LegacyCases[i];
                Assert.That(lt.Id, Is.EqualTo("LT-" + (47 + i).ToString("00", System.Globalization.CultureInfo.InvariantCulture)),
                    "la numeración sigue a la de la 4b (LT-46)");
                Assert.That(lt.File, Is.EqualTo(i < 12 ? "test/series_tracker_test.dart" : "test/series_panels_test.dart"));
                CheckKinds(lt.Id, lt.Checks);
            }

            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            foreach (var ac in It05Ids.Acceptance)
            {
                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            Assert.That(It05Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It05Ids.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(unityOnly.Count, Is.EqualTo(20), "02-tech-design §6.5");
            Assert.That(workPackageB, Is.Empty);
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It05DotnetIdsHaveTests()
        {
            // Lo que el plan verifica con dotnet ([d] o [s] en 02-tech-design §6.5) tiene al menos un test con su categoría en
            // Tests/EditMode o Tests/Simulator (WP-A); también LT-47 a LT-61.
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It05Ids.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            foreach (var lt in It05Ids.LegacyCases)
            {
                if (!categories.ContainsKey(lt.Id))
                {
                    missing.Add(lt.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d]/[s] de la It. 5 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It05LegacyCasesKeepTheirNames()
        {
            var scan = Scan();
            var problems = new List<string>();
            foreach (var lt in It05Ids.LegacyCases)
            {
                CheckLegacyCase(scan, lt.Id, lt.File, lt.Line, lt.Name, problems);
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It05UnityOnlyIdsHaveAUnityLocation()
        {
            // Como It04bUnityOnlyIdsHaveAUnityLocation. Mientras ninguno de los de los paneles (AC-WKT-18-*, solo de Unity) tenga su
            // prueba de Unity (WP-B), se ignora con el motivo; desde que aparece uno, se exigen todos. Los de la deuda de la 4b
            // (AC-WKT-13-15, -13-16, -16-07) ya tienen pruebas de la deuda y no abren la puerta.
            var folders = new Dictionary<string, SourceIndex?>(StringComparer.Ordinal)
            {
                ["p"] = ScanFolders("Tests/PlayMode"),
                ["ps"] = ScanFolders("Tests/PlayModeSimulator"),
                ["u"] = ScanFolders("Tests/UnityEditMode"),
                ["cap"] = ScanFolders("Snapshots"),
            };
            var checks = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var ac in It05Ids.Acceptance)
            {
                checks[ac.Id] = ac.Checks;
            }

            var earlier = new HashSet<string>(It05Ids.FromEarlier, StringComparer.Ordinal);
            var missing = new List<string>();
            var newFound = 0;
            foreach (var id in It05Ids.UnityOnlyIds)
            {
                var found = false;
                foreach (var kind in folders.Keys)
                {
                    var index = folders[kind];
                    if (!HasKind(checks[id], kind) || index == null)
                    {
                        continue;
                    }

                    found |= kind == "cap" ? index.MentionsText(id) : index.Categories.ContainsKey(id);
                }

                if (found && !earlier.Contains(id) && id.StartsWith("AC-WKT-18-", StringComparison.Ordinal))
                {
                    newFound++;
                }

                if (!found)
                {
                    missing.Add(id + " (" + checks[id] + ")");
                }
            }

            if (newFound == 0)
            {
                Assert.Ignore("pendiente WP-B: todavía no hay pruebas de Unity de la It. 5 (" + missing.Count + " IDs sin su prueba)");
            }

            Assert.That(missing, Is.Empty, "IDs de la It. 5 solo de Unity sin su prueba en la carpeta esperada: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It05UnityOnlyIdsHaveNoDotnetResult()
        {
            // Los nuevos de la It. 5 solo de Unity no llevan su categoría en Tests/EditMode ni en Tests/Simulator (sí en un [Ignore]
            // de equipo, que deja PENDIENTE UNITY).
            var scan = Scan();
            var earlier = new HashSet<string>(It05Ids.FromEarlier, StringComparer.Ordinal);

            // It. 6a: los que la It. 6a vuelve a verificar con dotnet (AC-WKT-13-15, -13-08, -13-12, -17-07: sus reglas puras de la
            // escena) ya no son solo de Unity.
            foreach (var ac in It06aIds.Acceptance)
            {
                if (HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s"))
                {
                    earlier.Add(ac.Id);
                }
            }

            var offenders = new List<string>();
            foreach (var id in It05Ids.UnityOnlyIds)
            {
                if (earlier.Contains(id) || !scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    if (!file.EndsWith("WorkoutDeviceChecksTests.cs", StringComparison.Ordinal))
                    {
                        offenders.Add(id + " en " + file);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        // ------------------------------------------------------------------ It. 6a (02-tech-design §6.3, §6.5)

        [Test, Category("AC-BASE-09")]
        public void It06aIdsAreComplete()
        {
            // 101 ID = 78 nuevos (SCN 77 por ficha + WKT 1: AC-WKT-17-13) + 23 de iteraciones anteriores (§5.2 y §5.3); LT-62 a
            // LT-87 (01-po-spec, encabezado, §4 y §6).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var earlier = new HashSet<string>(It06aIds.FromEarlier, StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            var byCard = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var ac in It06aIds.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It06aIds: " + ac.Id);
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                if (earlier.Contains(ac.Id))
                {
                    Assert.That(before.Contains(ac.Id), Is.True, ac.Id + " figura como de una iteración anterior pero no está en It01Ids a It05Ids");
                    continue;
                }

                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior: va en FromEarlier");
                var card = ac.Id.Substring(0, ac.Id.LastIndexOf('-'));
                byCard.TryGetValue(card, out var count);
                byCard[card] = count + 1;
            }

            Assert.That(It06aIds.Acceptance.Length, Is.EqualTo(101));
            Assert.That(It06aIds.FromEarlier.Length, Is.EqualTo(23));
            Assert.That(earlier.Count, Is.EqualTo(23), "repetidos en FromEarlier");
            foreach (var id in It06aIds.FromEarlier)
            {
                Assert.That(ids.Contains(id), Is.True, id);
            }

            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["AC-SCN-02"] = 8, ["AC-SCN-03"] = 7, ["AC-SCN-09"] = 6, ["AC-SCN-10"] = 7, ["AC-SCN-11"] = 7, ["AC-SCN-14"] = 7,
                ["AC-SCN-15"] = 8, ["AC-SCN-23"] = 4, ["AC-SCN-01"] = 14, ["AC-SCN-13"] = 4, ["AC-SCN-19"] = 2, ["AC-SCN-20"] = 2,
                ["AC-SCN-21"] = 1, ["AC-WKT-17"] = 1,
            };
            Assert.That(byCard, Is.EquivalentTo(expected), "los 78 nuevos por ficha (encabezado de la spec)");

            Assert.That(It06aIds.LegacyCases.Length, Is.EqualTo(26));
            for (var i = 0; i < It06aIds.LegacyCases.Length; i++)
            {
                var lt = It06aIds.LegacyCases[i];
                Assert.That(lt.Id, Is.EqualTo("LT-" + (62 + i).ToString("00", System.Globalization.CultureInfo.InvariantCulture)),
                    "la numeración sigue a la de la 5 (LT-61)");
                Assert.That(lt.File, Does.StartWith("test/scene/").And.EndWith("_test.dart"), lt.Id);
                CheckKinds(lt.Id, lt.Checks);
            }

            // Sin d ni s: solo de Unity (p, ps, u o cap) o solo de equipo (eq: los [Ignore] de WorkoutDeviceChecksTests, WP-B2).
            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            var deviceOnly = new List<string>();
            foreach (var ac in It06aIds.Acceptance)
            {
                if (ac.Checks == "eq")
                {
                    deviceOnly.Add(ac.Id);
                    continue;
                }

                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            // QA-V6a-PAR-05 y QA-V6a-11 (ronda 1 de la verificación): los LT que se verifican solo en PlayMode (LT-78 a LT-81 y LT-83 a
            // LT-87) también son de Unity: sin -UnityResults, run-tests.ps1 -Matrix los da PENDIENTE UNITY (no SIN TEST).
            foreach (var lt in It06aIds.LegacyCases)
            {
                Classify(lt.Id, lt.Checks, unityOnly, workPackageB);
            }

            Assert.That(It06aIds.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It06aIds.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(It06aIds.DeviceOnlyIds, Is.EquivalentTo(deviceOnly));
            Assert.That(unityOnly.Count, Is.EqualTo(24 + 9), "02-tech-design §6.5: 24 AC y 9 LT de PlayMode");
            Assert.That(deviceOnly, Is.EquivalentTo(new[] { "AC-SCN-01-09", "AC-SCN-01-10" }));
            Assert.That(workPackageB, Is.Empty);
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It06aDotnetIdsHaveTests()
        {
            // Lo que el plan verifica con dotnet ([d] o [s] en 02-tech-design §6.5) tiene al menos un test con su categoría en
            // Tests/EditMode o Tests/Simulator (WP-A); también los LT de dotnet (LT-62 a LT-77 y LT-82).
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It06aIds.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            foreach (var lt in It06aIds.LegacyCases)
            {
                if (HasKind(lt.Checks, "d") && !categories.ContainsKey(lt.Id))
                {
                    missing.Add(lt.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d]/[s] de la It. 6a sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It06aLegacyCasesKeepTheirNames()
        {
            // Los de dotnet, en Tests/EditMode; los de PlayMode (LT-78 a LT-81, LT-83 a LT-87), en Tests/PlayMode desde WP-B1
            // (It06aUnityOnlyIdsHaveAUnityLocation).
            var scan = Scan();
            var problems = new List<string>();
            var dotnet = 0;
            foreach (var lt in It06aIds.LegacyCases)
            {
                if (!HasKind(lt.Checks, "d"))
                {
                    continue;
                }

                dotnet++;
                CheckLegacyCase(scan, lt.Id, lt.File, lt.Line, lt.Name, problems);
            }

            Assert.That(dotnet, Is.EqualTo(17), "LT-62 a LT-77 y LT-82");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It06aUnityOnlyIdsHaveAUnityLocation()
        {
            // Como It05UnityOnlyIdsHaveAUnityLocation. Mientras ninguno de los nuevos de la escena (AC-SCN-*, solo de Unity) tenga su
            // prueba de Unity (WP-B), se ignora con el motivo; desde que aparece uno, se exigen todos, y también los LT de PlayMode
            // con su nombre y su cita.
            var folders = new Dictionary<string, SourceIndex?>(StringComparer.Ordinal)
            {
                ["p"] = ScanFolders("Tests/PlayMode"),
                ["ps"] = ScanFolders("Tests/PlayModeSimulator"),
                ["u"] = ScanFolders("Tests/UnityEditMode"),
                ["cap"] = ScanFolders("Snapshots"),
            };
            var checks = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var ac in It06aIds.Acceptance)
            {
                checks[ac.Id] = ac.Checks;
            }

            // QA-V6a-PAR-05: los LT de PlayMode también están en UnityOnlyIds.
            foreach (var lt in It06aIds.LegacyCases)
            {
                checks[lt.Id] = lt.Checks;
            }

            var earlier = new HashSet<string>(It06aIds.FromEarlier, StringComparer.Ordinal);
            var missing = new List<string>();
            var newFound = 0;
            foreach (var id in It06aIds.UnityOnlyIds)
            {
                var found = false;
                foreach (var kind in folders.Keys)
                {
                    var index = folders[kind];
                    if (!HasKind(checks[id], kind) || index == null)
                    {
                        continue;
                    }

                    found |= kind == "cap" ? index.MentionsText(id) : index.Categories.ContainsKey(id);
                }

                if (found && !earlier.Contains(id) && id.StartsWith("AC-SCN-", StringComparison.Ordinal))
                {
                    newFound++;
                }

                if (!found)
                {
                    missing.Add(id + " (" + checks[id] + ")");
                }
            }

            if (newFound == 0)
            {
                Assert.Ignore("pendiente WP-B: todavía no hay pruebas de Unity de la It. 6a (" + missing.Count + " IDs sin su prueba)");
            }

            var playMode = folders["p"];
            if (playMode != null)
            {
                foreach (var lt in It06aIds.LegacyCases)
                {
                    if (HasKind(lt.Checks, "p"))
                    {
                        CheckLegacyCase(playMode, lt.Id, lt.File, lt.Line, lt.Name, missing);
                    }
                }
            }

            Assert.That(missing, Is.Empty, "IDs de la It. 6a solo de Unity sin su prueba en la carpeta esperada: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It06aUnityOnlyIdsHaveNoDotnetResult()
        {
            // Los nuevos de la It. 6a solo de Unity o de equipo no llevan su categoría en Tests/EditMode ni en Tests/Simulator (sí en
            // un [Ignore] de equipo de WorkoutDeviceChecksTests, que deja PENDIENTE UNITY). Los que una iteración posterior cambia y
            // pasa a verificar con dotnet (It. 8 §4: AC-SCN-21-01) quedan fuera.
            var scan = Scan();
            var earlier = new HashSet<string>(It06aIds.FromEarlier, StringComparer.Ordinal);
            earlier.UnionWith(It08Ids.FromEarlier);
            var offenders = new List<string>();
            var ids = new List<string>(It06aIds.UnityOnlyIds);
            ids.AddRange(It06aIds.DeviceOnlyIds);
            foreach (var id in ids)
            {
                if (earlier.Contains(id) || !scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    if (!file.EndsWith("WorkoutDeviceChecksTests.cs", StringComparison.Ordinal))
                    {
                        offenders.Add(id + " en " + file);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        // ------------------------------------------------------------------ It. 7 (07-remero-palada/02-tech-design §6.3, §6.4)

        [Test, Category("AC-BASE-09")]
        public void It07IdsAreComplete()
        {
            // 38 ID = 36 nuevos (SCN-04: 5, SCN-05: 10, SCN-06: 3, SCN-07: 3, SCN-08: 12, también toca: 3) + 2 que cambian (§4);
            // LT-88 a LT-98 (01-po-spec §5).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var earlier = new HashSet<string>(It07Ids.FromEarlier, StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance, It06aIds.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            var byCard = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var ac in It07Ids.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It07Ids: " + ac.Id);
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                if (earlier.Contains(ac.Id))
                {
                    Assert.That(before.Contains(ac.Id), Is.True, ac.Id + " figura como de una iteración anterior pero no está en It01Ids a It06aIds");
                    continue;
                }

                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior: va en FromEarlier");
                var card = ac.Id.Substring(0, ac.Id.LastIndexOf('-'));
                byCard.TryGetValue(card, out var count);
                byCard[card] = count + 1;
            }

            Assert.That(It07Ids.Acceptance.Length, Is.EqualTo(38));
            Assert.That(It07Ids.FromEarlier.Length, Is.EqualTo(2));
            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["AC-SCN-04"] = 5, ["AC-SCN-05"] = 10, ["AC-SCN-06"] = 3, ["AC-SCN-07"] = 3, ["AC-SCN-08"] = 12, ["AC-WKT-17"] = 1, ["AC-WKT-18"] = 1,
                ["AC-SCN-01"] = 1,
            };
            Assert.That(byCard, Is.EquivalentTo(expected), "los 36 nuevos por ficha (§3)");

            Assert.That(It07Ids.LegacyCases.Length, Is.EqualTo(11));
            for (var i = 0; i < It07Ids.LegacyCases.Length; i++)
            {
                var lt = It07Ids.LegacyCases[i];
                Assert.That(lt.Id, Is.EqualTo("LT-" + (88 + i).ToString("00", System.Globalization.CultureInfo.InvariantCulture)),
                    "la numeración sigue a la de la 6a (LT-87)");
                Assert.That(lt.File, Does.StartWith("test/scene/").And.EndWith("_test.dart"), lt.Id);
                CheckKinds(lt.Id, lt.Checks);
            }

            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            var deviceOnly = new List<string>();
            foreach (var ac in It07Ids.Acceptance)
            {
                if (ac.Checks == "eq")
                {
                    deviceOnly.Add(ac.Id);
                    continue;
                }

                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            foreach (var lt in It07Ids.LegacyCases)
            {
                Classify(lt.Id, lt.Checks, unityOnly, workPackageB);
            }

            Assert.That(It07Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It07Ids.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(It07Ids.DeviceOnlyIds, Is.EquivalentTo(deviceOnly));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It07DotnetIdsHaveTests()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It07Ids.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            foreach (var lt in It07Ids.LegacyCases)
            {
                if (HasKind(lt.Checks, "d") && !categories.ContainsKey(lt.Id))
                {
                    missing.Add(lt.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d] de la It. 7 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It07LegacyCasesKeepTheirNames()
        {
            // Los de dotnet (LT-88 a LT-96), en Tests/EditMode; LT-97 y LT-98 son de PlayMode (RowerSceneTests, WP-B1).
            var scan = Scan();
            var problems = new List<string>();
            var dotnet = 0;
            foreach (var lt in It07Ids.LegacyCases)
            {
                if (!HasKind(lt.Checks, "d"))
                {
                    continue;
                }

                dotnet++;
                CheckLegacyCase(scan, lt.Id, lt.File, lt.Line, lt.Name, problems);
            }

            Assert.That(dotnet, Is.EqualTo(9), "LT-88 a LT-96");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It07UnityOnlyIdsHaveNoDotnetResult()
        {
            var scan = Scan();
            var earlier = new HashSet<string>(It07Ids.FromEarlier, StringComparer.Ordinal);
            var offenders = new List<string>();
            var ids = new List<string>(It07Ids.UnityOnlyIds);
            ids.AddRange(It07Ids.DeviceOnlyIds);
            foreach (var id in ids)
            {
                if (earlier.Contains(id) || !scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    if (!file.EndsWith("WorkoutDeviceChecksTests.cs", StringComparison.Ordinal))
                    {
                        offenders.Add(id + " en " + file);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        // ------------------------------------------------------------------ It. 8 (08-entornos-selector/02-tech-design §7.3, §7.4)

        [Test, Category("AC-BASE-09")]
        public void It08IdsAreComplete()
        {
            // 40 ID = 37 nuevos (SCN-14: 3, SCN-16: 4, SCN-17: 4, SCN-12: 1, SCN-06: 1, SCN-18: 4, SCN-19: 3, SCN-20: 5, SCN-21: 4,
            // SCN-22: 5, SCN-01: 3) + 3 que cambian (§4); LT-99 a LT-138 (01-po-spec §5).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var earlier = new HashSet<string>(It08Ids.FromEarlier, StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance, It06aIds.Acceptance, It07Ids.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            var byCard = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var ac in It08Ids.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It08Ids: " + ac.Id);
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                if (earlier.Contains(ac.Id))
                {
                    Assert.That(before.Contains(ac.Id), Is.True, ac.Id + " figura como de una iteración anterior pero no está en It01Ids a It07Ids");
                    continue;
                }

                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior: va en FromEarlier");
                var card = ac.Id.Substring(0, ac.Id.LastIndexOf('-'));
                byCard.TryGetValue(card, out var count);
                byCard[card] = count + 1;
            }

            Assert.That(It08Ids.Acceptance.Length, Is.EqualTo(40));
            Assert.That(It08Ids.FromEarlier.Length, Is.EqualTo(3));
            foreach (var id in It08Ids.FromEarlier)
            {
                Assert.That(ids.Contains(id), Is.True, id);
            }

            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["AC-SCN-14"] = 3, ["AC-SCN-16"] = 4, ["AC-SCN-17"] = 4, ["AC-SCN-12"] = 1, ["AC-SCN-06"] = 1, ["AC-SCN-18"] = 4,
                ["AC-SCN-19"] = 3, ["AC-SCN-20"] = 5, ["AC-SCN-21"] = 4, ["AC-SCN-22"] = 5, ["AC-SCN-01"] = 3,
            };
            Assert.That(byCard, Is.EquivalentTo(expected), "los 37 nuevos por ficha (§3)");

            Assert.That(It08Ids.LegacyCases.Length, Is.EqualTo(40));
            for (var i = 0; i < It08Ids.LegacyCases.Length; i++)
            {
                var lt = It08Ids.LegacyCases[i];
                Assert.That(lt.Id, Is.EqualTo("LT-" + (99 + i).ToString("00", System.Globalization.CultureInfo.InvariantCulture)),
                    "la numeración sigue a la de la 7 (LT-98)");
                Assert.That(lt.File, Does.StartWith("test/scene/").And.EndWith("_test.dart"), lt.Id);
                CheckKinds(lt.Id, lt.Checks);
            }

            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            var deviceOnly = new List<string>();
            foreach (var ac in It08Ids.Acceptance)
            {
                if (ac.Checks == "eq")
                {
                    deviceOnly.Add(ac.Id);
                    continue;
                }

                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            foreach (var lt in It08Ids.LegacyCases)
            {
                Classify(lt.Id, lt.Checks, unityOnly, workPackageB);
            }

            Assert.That(It08Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It08Ids.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(It08Ids.DeviceOnlyIds, Is.EquivalentTo(deviceOnly));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It08DotnetIdsHaveTests()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It08Ids.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            foreach (var lt in It08Ids.LegacyCases)
            {
                if (HasKind(lt.Checks, "d") && !categories.ContainsKey(lt.Id))
                {
                    missing.Add(lt.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d] de la It. 8 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It08LegacyCasesKeepTheirNames()
        {
            // Los de dotnet (LT-99 a LT-110, LT-124 a LT-126), en Tests/EditMode; LT-111 a LT-123 y LT-127 a LT-138 son de PlayMode
            // (EnvironmentSceneTests, WP-B1).
            var scan = Scan();
            var problems = new List<string>();
            var dotnet = 0;
            foreach (var lt in It08Ids.LegacyCases)
            {
                if (!HasKind(lt.Checks, "d"))
                {
                    continue;
                }

                dotnet++;
                CheckLegacyCase(scan, lt.Id, lt.File, lt.Line, lt.Name, problems);
            }

            Assert.That(dotnet, Is.EqualTo(15), "LT-99 a LT-110 y LT-124 a LT-126");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It08UnityOnlyIdsHaveNoDotnetResult()
        {
            var scan = Scan();
            var earlier = new HashSet<string>(It08Ids.FromEarlier, StringComparer.Ordinal);
            var offenders = new List<string>();
            var ids = new List<string>(It08Ids.UnityOnlyIds);
            ids.AddRange(It08Ids.DeviceOnlyIds);
            foreach (var id in ids)
            {
                if (earlier.Contains(id) || !scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    if (!file.EndsWith("WorkoutDeviceChecksTests.cs", StringComparison.Ordinal))
                    {
                        offenders.Add(id + " en " + file);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        // ------------------------------------------------------------------ It. 9 (09-historial/02-tech-design §8.3)

        [Test, Category("AC-BASE-09")]
        public void It09IdsAreComplete()
        {
            // 43 AC nuevos (§3): HIS-05: 4, HIS-06: 4, HIS-07: 7, HIS-09: 4, HIS-10: 1, HIS-11: 4, HIS-12: 2, HIS-13: 7, HIS-14: 3,
            // HIS-15: 3, RUT-20: 1, HIS-19: 3. Ninguno es de una iteración anterior y no hay casos legacy (spec §0).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance, It06aIds.Acceptance, It07Ids.Acceptance, It08Ids.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            var byCard = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var ac in It09Ids.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It09Ids: " + ac.Id);
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior");
                var card = ac.Id.Substring(0, ac.Id.LastIndexOf('-'));
                byCard.TryGetValue(card, out var count);
                byCard[card] = count + 1;
            }

            Assert.That(It09Ids.Acceptance.Length, Is.EqualTo(43));
            Assert.That(It09Ids.FromEarlier, Is.Empty);
            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["AC-HIS-05"] = 4, ["AC-HIS-06"] = 4, ["AC-HIS-07"] = 7, ["AC-HIS-09"] = 4, ["AC-HIS-10"] = 1, ["AC-HIS-11"] = 4,
                ["AC-HIS-12"] = 2, ["AC-HIS-13"] = 7, ["AC-HIS-14"] = 3, ["AC-HIS-15"] = 3, ["AC-RUT-20"] = 1, ["AC-HIS-19"] = 3,
            };
            Assert.That(byCard, Is.EquivalentTo(expected), "los 43 por ficha (§3)");

            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            var deviceOnly = new List<string>();
            foreach (var ac in It09Ids.Acceptance)
            {
                if (ac.Checks == "eq")
                {
                    deviceOnly.Add(ac.Id);
                    continue;
                }

                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            Assert.That(It09Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It09Ids.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(It09Ids.DeviceOnlyIds, Is.EquivalentTo(deviceOnly));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It09DotnetIdsHaveTests()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It09Ids.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d] de la It. 9 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It09UnityOnlyIdsHaveNoDotnetResult()
        {
            var scan = Scan();
            var offenders = new List<string>();
            var ids = new List<string>(It09Ids.UnityOnlyIds);
            ids.AddRange(It09Ids.DeviceOnlyIds);
            foreach (var id in ids)
            {
                if (!scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    offenders.Add(id + " en " + file);
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test, Category("AC-BASE-09")]
        public void It10aIdsAreComplete()
        {
            // 43 AC nuevos (10a §3): BLE-04: 7, APP-13: 3, BLE-05: 7, BLE-06: 8, BLE-07: 1, BLE-11: 1, BLE-08: 6, BLE-09: 3, BLE-12: 2,
            // BLE-91: 2, APP-14: 1, WKT-03: 1, BLE-SEC: 1. AC-WKT-03-10 (4a) cambia y va en FromEarlier.
            var pattern = new Regex(@"^AC-[A-Z]+(-[A-Z]+)?-\d\d(-\d\d)?$");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance, It06aIds.Acceptance, It07Ids.Acceptance, It08Ids.Acceptance, It09Ids.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            var byCard = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var ac in It10aIds.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It10aIds: " + ac.Id);
                Assert.That(pattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior");
                var card = ac.Id.Substring(0, ac.Id.LastIndexOf('-'));
                byCard.TryGetValue(card, out var count);
                byCard[card] = count + 1;
            }

            Assert.That(It10aIds.Acceptance.Length, Is.EqualTo(43));
            foreach (var id in It10aIds.FromEarlier)
            {
                Assert.That(before.Contains(id), Is.True, id + " figura como de antes pero no está en ninguna lista anterior");
            }

            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["AC-BLE-04"] = 7, ["AC-APP-13"] = 3, ["AC-BLE-05"] = 7, ["AC-BLE-06"] = 8, ["AC-BLE-07"] = 1, ["AC-BLE-11"] = 1, ["AC-BLE-08"] = 6,
                ["AC-BLE-09"] = 3, ["AC-BLE-12"] = 2, ["AC-BLE-91"] = 2, ["AC-APP-14"] = 1, ["AC-WKT-03"] = 1, ["AC-BLE-SEC"] = 1,
            };
            Assert.That(byCard, Is.EquivalentTo(expected), "los 43 por ficha (§3)");

            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            var deviceOnly = new List<string>();
            foreach (var ac in It10aIds.Acceptance)
            {
                if (ac.Checks == "eq")
                {
                    deviceOnly.Add(ac.Id);
                    continue;
                }

                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            Assert.That(It10aIds.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It10aIds.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(It10aIds.DeviceOnlyIds, Is.EquivalentTo(deviceOnly));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It10aDotnetIdsHaveTests()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It10aIds.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d] de la It. 10a sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It10aUnityOnlyIdsHaveNoDotnetResult()
        {
            var scan = Scan();
            var offenders = new List<string>();
            var ids = new List<string>(It10aIds.UnityOnlyIds);
            ids.AddRange(It10aIds.DeviceOnlyIds);
            foreach (var id in ids)
            {
                if (!scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    offenders.Add(id + " en " + file);
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test, Category("AC-BASE-09")]
        public void It11IdsAreComplete()
        {
            // 41 AC (11 §3): HRM-09: 1, HRM-05: 3, HRM-04: 1, HRM-02: 6, HRM-03: 4, HRM-06: 3, APP-01: 1, HRM-07: 5, HRM-08: 3, HRM-10: 2,
            // HRM-11: 3, HRM-16: 2, HRM-13: 2, WKT-05: 1, HRM-15: 1, HRM-SEC: 1, HRM-HW: 2. Todos nuevos; 7 casos legacy (LT-139 a LT-145).
            var pattern = new Regex(@"^AC-[A-Z]+(-[A-Z]+)?-\d\d(-\d\d)?$");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance, It06aIds.Acceptance, It07Ids.Acceptance, It08Ids.Acceptance, It09Ids.Acceptance, It10aIds.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            var byCard = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var ac in It11Ids.Acceptance)
            {
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It11Ids: " + ac.Id);
                Assert.That(pattern.IsMatch(ac.Id), Is.True, ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior");
                var card = ac.Id.Substring(0, ac.Id.LastIndexOf('-'));
                byCard.TryGetValue(card, out var count);
                byCard[card] = count + 1;
            }

            Assert.That(It11Ids.Acceptance.Length, Is.EqualTo(41));
            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["AC-HRM-09"] = 1, ["AC-HRM-05"] = 3, ["AC-HRM-04"] = 1, ["AC-HRM-02"] = 6, ["AC-HRM-03"] = 4, ["AC-HRM-06"] = 3, ["AC-APP-01"] = 1,
                ["AC-HRM-07"] = 5, ["AC-HRM-08"] = 3, ["AC-HRM-10"] = 2, ["AC-HRM-11"] = 3, ["AC-HRM-16"] = 2, ["AC-HRM-13"] = 2, ["AC-WKT-05"] = 1,
                ["AC-HRM-15"] = 1, ["AC-HRM-SEC"] = 1, ["AC-HRM-HW"] = 2,
            };
            Assert.That(byCard, Is.EquivalentTo(expected), "los 41 por ficha (§3)");

            Assert.That(It11Ids.LegacyCases.Length, Is.EqualTo(7));
            for (var i = 0; i < It11Ids.LegacyCases.Length; i++)
            {
                Assert.That(It11Ids.LegacyCases[i].Id, Is.EqualTo("LT-" + (139 + i)));
                CheckKinds(It11Ids.LegacyCases[i].Id, It11Ids.LegacyCases[i].Checks);
            }

            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            var deviceOnly = new List<string>();
            foreach (var ac in It11Ids.Acceptance)
            {
                if (ac.Checks == "eq")
                {
                    deviceOnly.Add(ac.Id);
                    continue;
                }

                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            Assert.That(It11Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It11Ids.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(It11Ids.DeviceOnlyIds, Is.EquivalentTo(deviceOnly));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It11DotnetIdsHaveTests()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It11Ids.Acceptance)
            {
                if ((HasKind(ac.Checks, "d") || HasKind(ac.Checks, "s")) && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d] de la It. 11 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It11UnityOnlyIdsHaveNoDotnetResult()
        {
            var scan = Scan();
            var offenders = new List<string>();
            var ids = new List<string>(It11Ids.UnityOnlyIds);
            ids.AddRange(It11Ids.DeviceOnlyIds);
            foreach (var id in ids)
            {
                if (!scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    offenders.Add(id + " en " + file);
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It11LegacyCasesKeepTheirNames()
        {
            // LT-139 a LT-145 (test/reconnect_loop_test.dart), en Tests/EditMode/Ble (ReconnectLoopTests, StaleWatchdogTests).
            var scan = Scan();
            var problems = new List<string>();
            foreach (var lt in It11Ids.LegacyCases)
            {
                CheckLegacyCase(scan, lt.Id, lt.File, lt.Line, lt.Name, problems);
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-BASE-09")]
        public void It12IdsAreComplete()
        {
            // 28 AC (12 §3): AC-STR-09-01 a -28, todos nuevos y en orden; sin casos legacy (el legacy no tiene tests del TCX).
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance, It06aIds.Acceptance, It07Ids.Acceptance, It08Ids.Acceptance, It09Ids.Acceptance, It10aIds.Acceptance, It11Ids.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            Assert.That(It12Ids.Acceptance.Length, Is.EqualTo(28));
            for (var i = 0; i < It12Ids.Acceptance.Length; i++)
            {
                var ac = It12Ids.Acceptance[i];
                Assert.That(ac.Id, Is.EqualTo("AC-STR-09-" + (i + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture)));
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It12Ids: " + ac.Id);
                CheckKinds(ac.Id, ac.Checks);
                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior");
            }

            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            var deviceOnly = new List<string>();
            foreach (var ac in It12Ids.Acceptance)
            {
                if (ac.Checks == "eq")
                {
                    deviceOnly.Add(ac.Id);
                    continue;
                }

                Classify(ac.Id, ac.Checks, unityOnly, workPackageB);
            }

            Assert.That(It12Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It12Ids.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(It12Ids.DeviceOnlyIds, Is.EquivalentTo(deviceOnly));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It12DotnetIdsHaveTests()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It12Ids.Acceptance)
            {
                if (HasKind(ac.Checks, "d") && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d] de la It. 12 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It12UnityOnlyIdsHaveNoDotnetResult()
        {
            var scan = Scan();
            var offenders = new List<string>();
            var ids = new List<string>(It12Ids.UnityOnlyIds);
            ids.AddRange(It12Ids.DeviceOnlyIds);
            foreach (var id in ids)
            {
                if (!scan.Categories.TryGetValue(id, out var files))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    offenders.Add(id + " en " + file);
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test, Category("AC-BASE-09")]
        public void It14IdsAreComplete()
        {
            // 44 AC (14 §3): 7 de F-APP-16, 6 de F-APP-15, 3 de F-APP-11, 4 de F-APP-03, 7 de pulido, 6 de F-APP-19, 1 de arrastre y 10 de
            // deuda; todos nuevos (la It. 13 se descartó). Las tres listas salen de la columna de verificación: sin d ni dB y con p, u o
            // cap → UnityOnlyIds; sin d y con dB → WorkPackageBIds; sin ninguna automática → DeviceOnlyIds.
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance, It06aIds.Acceptance, It07Ids.Acceptance, It08Ids.Acceptance, It09Ids.Acceptance, It10aIds.Acceptance, It11Ids.Acceptance, It12Ids.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var prefixes = new Dictionary<string, int>(StringComparer.Ordinal);
            var unityOnly = new List<string>();
            var workPackageB = new List<string>();
            var deviceOnly = new List<string>();
            foreach (var ac in It14Ids.Acceptance)
            {
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It14Ids: " + ac.Id);
                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior");
                CheckKinds(ac.Id, ac.Checks);
                var prefix = ac.Id.Substring(0, ac.Id.LastIndexOf('-'));
                prefixes.TryGetValue(prefix, out var count);
                prefixes[prefix] = count + 1;
                if (HasKind(ac.Checks, "d"))
                {
                    continue;
                }

                if (HasKind(ac.Checks, "dB"))
                {
                    workPackageB.Add(ac.Id);
                }
                else if (HasKind(ac.Checks, "p") || HasKind(ac.Checks, "u") || HasKind(ac.Checks, "cap"))
                {
                    unityOnly.Add(ac.Id);
                }
                else
                {
                    deviceOnly.Add(ac.Id);
                }
            }

            Assert.That(It14Ids.Acceptance.Length, Is.EqualTo(44));
            Assert.That(prefixes["AC-APP-16"], Is.EqualTo(7));
            Assert.That(prefixes["AC-APP-15"], Is.EqualTo(6));
            Assert.That(prefixes["AC-APP-11"], Is.EqualTo(3));
            Assert.That(prefixes["AC-APP-03"], Is.EqualTo(4));
            Assert.That(prefixes["AC-POL"], Is.EqualTo(7));
            Assert.That(prefixes["AC-APP-19"], Is.EqualTo(6));
            Assert.That(prefixes["AC-NV"], Is.EqualTo(1));
            Assert.That(prefixes["AC-DEBT"], Is.EqualTo(10));
            Assert.That(It14Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It14Ids.WorkPackageBIds, Is.EquivalentTo(workPackageB));
            Assert.That(It14Ids.DeviceOnlyIds, Is.EquivalentTo(deviceOnly));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It14DotnetIdsHaveTests()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It14Ids.Acceptance)
            {
                if (HasKind(ac.Checks, "d") && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d] de la It. 14 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It14UnityOnlyIdsHaveNoDotnetResult()
        {
            var scan = Scan();
            var offenders = new List<string>();
            var ids = new List<string>(It14Ids.UnityOnlyIds);
            ids.AddRange(It14Ids.DeviceOnlyIds);
            foreach (var id in ids)
            {
                if (scan.Categories.TryGetValue(id, out var files))
                {
                    foreach (var file in files)
                    {
                        offenders.Add(id + " en " + file);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test, Category("AC-BASE-09")]
        public void It15IdsAreComplete()
        {
            // 40 AC de deuda (15 §3), todos nuevos. UnityOnlyIds sale de la columna de verificación (sin d y con p, u o cap); sin
            // WorkPackageBIds (los de dotnet son todos de WP-A); DeviceOnlyIds = los de equipo de 02-tech-design §6 (03, 19 y 37) y el
            // que solo verifica QA leyendo (08).
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance, It06aIds.Acceptance, It07Ids.Acceptance, It08Ids.Acceptance, It09Ids.Acceptance, It10aIds.Acceptance, It11Ids.Acceptance, It12Ids.Acceptance, It14Ids.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var unityOnly = new List<string>();
            var withoutAutomatic = new List<string>();
            for (var i = 0; i < It15Ids.Acceptance.Length; i++)
            {
                var ac = It15Ids.Acceptance[i];
                Assert.That(ac.Id, Is.EqualTo("AC-D15-" + (i + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture)), "en orden");
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It15Ids: " + ac.Id);
                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior");
                CheckKinds(ac.Id, ac.Checks);
                if (HasKind(ac.Checks, "d") || HasKind(ac.Checks, "dB"))
                {
                    continue;
                }

                if (HasKind(ac.Checks, "p") || HasKind(ac.Checks, "u") || HasKind(ac.Checks, "cap"))
                {
                    unityOnly.Add(ac.Id);
                }
                else
                {
                    withoutAutomatic.Add(ac.Id);
                }
            }

            Assert.That(It15Ids.Acceptance.Length, Is.EqualTo(40));
            Assert.That(It15Ids.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It15Ids.WorkPackageBIds, Is.Empty);
            var deviceOnly = new List<string>(withoutAutomatic) { "AC-D15-03", "AC-D15-19" };
            Assert.That(It15Ids.DeviceOnlyIds, Is.EquivalentTo(deviceOnly));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It15DotnetIdsHaveTests()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It15Ids.Acceptance)
            {
                if (HasKind(ac.Checks, "d") && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d] de la It. 15 sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It15UnityOnlyIdsHaveNoDotnetResult()
        {
            var scan = Scan();
            var offenders = new List<string>();
            foreach (var id in It15Ids.UnityOnlyIds)
            {
                if (scan.Categories.TryGetValue(id, out var files))
                {
                    foreach (var file in files)
                    {
                        offenders.Add(id + " en " + file);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join(", ", offenders));
        }

        [Test, Category("AC-BASE-09")]
        public void It06bIdsAreComplete()
        {
            // 38 AC (06b §3), todos nuevos, en orden. UnityOnlyIds sale de la columna de verificación (sin d y con p, u o cap); sin
            // WorkPackageBIds; DeviceOnlyIds = los que no tienen ninguna automática (el Android, NO VERIFICABLE, y el repo limpio de QA).
            var before = new HashSet<string>(It01Ids.AcceptanceIds, StringComparer.Ordinal);
            foreach (var list in new[] { It02Ids.Acceptance, It03Ids.Acceptance, It04aIds.Acceptance, It04bIds.Acceptance, It05Ids.Acceptance, It06aIds.Acceptance, It07Ids.Acceptance, It08Ids.Acceptance, It09Ids.Acceptance, It10aIds.Acceptance, It11Ids.Acceptance, It12Ids.Acceptance, It14Ids.Acceptance, It15Ids.Acceptance })
            {
                foreach (var ac in list)
                {
                    before.Add(ac.Id);
                }
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var unityOnly = new List<string>();
            var withoutAutomatic = new List<string>();
            for (var i = 0; i < It06bIds.Acceptance.Length; i++)
            {
                var ac = It06bIds.Acceptance[i];
                Assert.That(ac.Id, Is.EqualTo("AC-6B-" + (i + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture)), "en orden");
                Assert.That(AcPattern.IsMatch(ac.Id), Is.True, ac.Id);
                Assert.That(ids.Add(ac.Id), Is.True, "repetido en It06bIds: " + ac.Id);
                Assert.That(before.Contains(ac.Id), Is.False, ac.Id + " ya es de una iteración anterior");
                CheckKinds(ac.Id, ac.Checks);
                if (HasKind(ac.Checks, "d") || HasKind(ac.Checks, "dB"))
                {
                    continue;
                }

                if (HasKind(ac.Checks, "p") || HasKind(ac.Checks, "u") || HasKind(ac.Checks, "cap"))
                {
                    unityOnly.Add(ac.Id);
                }
                else
                {
                    withoutAutomatic.Add(ac.Id);
                }
            }

            Assert.That(It06bIds.Acceptance.Length, Is.EqualTo(38));
            Assert.That(It06bIds.UnityOnlyIds, Is.EquivalentTo(unityOnly));
            Assert.That(It06bIds.WorkPackageBIds, Is.Empty);
            Assert.That(It06bIds.DeviceOnlyIds, Is.EquivalentTo(withoutAutomatic));

            // La matriz de run-tests.ps1 reconoce AC-6B-nn (la misma expresión de Get-QuotedIds).
            var script = File.ReadAllText(RepoPaths.Combine("tools/dotnet/run-tests.ps1"));
            var quoted = Regex.Match(script, @"\[regex\]::Matches\(\$Text, '([^']+)'\)");
            Assert.That(quoted.Success, Is.True, "Get-QuotedIds");
            var found = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in new Regex(quoted.Groups[1].Value).Matches(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Tests/EditMode/Traceability/It06bIds.cs"))))
            {
                found.Add(m.Groups[1].Value);
            }

            Assert.That(found.Count, Is.EqualTo(38), "los 38 AC de la 6b entran en la matriz");
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It06bDotnetIdsHaveTests()
        {
            var categories = Scan().Categories;
            var missing = new List<string>();
            foreach (var ac in It06bIds.Acceptance)
            {
                if (HasKind(ac.Checks, "d") && !categories.ContainsKey(ac.Id))
                {
                    missing.Add(ac.Id);
                }
            }

            Assert.That(missing, Is.Empty, missing.Count + " IDs [d] de la It. 6b sin test: " + string.Join(", ", missing));
        }

        [Test, Category("AC-BASE-09"), Category("Integration")]
        public void It06bUnityOnlyIdsHaveNoDotnetResult()
        {
            var scan = Scan();
            var offenders = new List<string>();
            var ids = new List<string>(It06bIds.UnityOnlyIds);
            ids.AddRange(It06bIds.DeviceOnlyIds);
            foreach (var id in ids)
            {
                if (scan.Categories.TryGetValue(id, out var files))
                {
                    foreach (var file in files)
                    {
                        offenders.Add(id + " en " + file);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join(", ", offenders));
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
