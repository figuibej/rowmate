using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // Reglas del código puro como tests (02-tech-design §7.2; AC-APP-19-03, AC-BASE-08, AC-BASE-04): corren en el mismo
    // comando local y en el CI. Se revisa el texto SIN comentarios (y, donde corresponde, sin el contenido de los literales).
    [TestFixture]
    public sealed class PureCodeRulesTests
    {
        private static readonly string[] PureProduction = { "Core", "App", "Simulator" };

        [Test, Category("AC-APP-19-03")]
        public void NoForbiddenRoundingOrFormatting()
        {
            // ADR 012: ningún Math.Round, MidpointRounding, ToString("F…"/"N…") ni formato {x:F1} en Core, App y Simulator.
            var patterns = new[]
            {
                new Regex(@"\bMath\.Round\s*\("),
                new Regex(@"\bMidpointRounding\b"),
                new Regex(@"\.ToString\s*\(\s*""[FfNn]\d*"""),
                new Regex(@"\{[^{}""]*:[FfNn]\d*\}"),
                new Regex(@"string\.Format\s*\([^;]*\{\d+:[FfNn]\d*\}"),
            };
            AssertNoMatches(PureProduction, patterns, false, "formato o redondeo de .NET (usar Fmt/DartMath, ADR 012)");
        }

        [Test, Category("AC-BASE-08"), Category("AC-APP-19-03")]
        public void NoEngineInPureCode()
        {
            // La lógica pura no depende del motor (R-09). También el soporte y las fuentes de test compartidas con dotnet.
            var folders = new[] { "Core", "App", "Simulator", "Infrastructure", "Tests/Support", "Tests/EditMode", "Tests/Simulator" };
            var patterns = new[]
            {
                new Regex(@"\busing\s+UnityEngine\b"),
                new Regex(@"\busing\s+UnityEditor\b"),
                new Regex(@"\bUnityEngine\s*\."),
                new Regex(@"\bUnityEditor\s*\."),
            };
            AssertNoMatches(folders, patterns, true, "API de Unity en código puro");
        }

        [Test, Category("AC-APP-19-03")]
        public void NoAsyncVoidNorConfigureAwaitFalse()
        {
            // ARCHITECTURE §6: las continuaciones vuelven al hilo principal y ninguna excepción se pierde.
            var patterns = new[]
            {
                new Regex(@"\basync\s+void\b"),
                new Regex(@"\.ConfigureAwait\s*\(\s*false\s*\)"),
            };
            // It. 3 (02-tech-design §2.6): también Infrastructure, cuyas tareas vuelven al hilo principal por el contexto.
            AssertNoMatches(new[] { "Core", "App", "Simulator", "Infrastructure" }, patterns, true, "async void o ConfigureAwait(false)");
        }

        // QA-G3-07 (02-tech-design §1.2): ningún archivo fuera de Infrastructure usa tipos de sqlite-net. En dotnet sqlite-net-pcl
        // llega en forma transitiva a todo proyecto que referencia Infrastructure, así que un test que los nombrara compilaría
        // con dotnet y rompería RowMate.Tests.EditMode en Unity (su asmdef no nombra Gilzoide.SqliteNet). Los tests leen la base
        // con RowMateDatabase.QueryRawForTestsAsync (tipos primitivos). Sobre el código sin comentarios ni literales.
        [Test, Category("AC-BASE-08"), Category("AC-HIS-17-02")]
        public void SqliteOnlyInInfrastructure()
        {
            var patterns = new[]
            {
                new Regex(@"\busing\s+(static\s+)?SQLite\b"),
                new Regex(@"\bSQLiteConnection\b"),
                new Regex(@"\bSQLiteException\b"),
                new Regex(@"\bSQLite3\b"),
                new Regex(@"\bSQLiteOpenFlags\b"),
                new Regex(@"\bSQLiteConnectionString\b"),
            };
            var offenders = new List<string>();
            var inside = 0;
            foreach (var file in SourceScan.CsFiles(""))
            {
                if (RepoPaths.Relative(file).StartsWith("Assets/RowMate/Infrastructure/", StringComparison.Ordinal))
                {
                    inside++;
                    continue;
                }

                offenders.AddRange(SourceScan.Matches(file, patterns, true));
            }

            Assert.That(inside, Is.GreaterThan(5), "el escaneo no encontró Infrastructure");
            Assert.That(offenders, Is.Empty, "tipos de sqlite-net fuera de Infrastructure:\n" + string.Join("\n", offenders));
        }

        // QA-G3-08: el SQLite de Unity compila con SQLITE_DQS 0 (un literal entre comillas dobles es un error) y el de dotnet no:
        // todo literal de C# con SQL lleva sus textos entre comillas simples (ningún \" ni "" adentro).
        [Test, Category("AC-HIS-17-02")]
        public void SqlLiteralsUseSingleQuotes()
        {
            var offenders = new List<string>();
            var sqlLiterals = 0;
            foreach (var file in SourceScan.CsFiles("Infrastructure", "Tests/Support", "Tests/EditMode/Infrastructure"))
            {
                var code = SourceScan.StripComments(File.ReadAllText(file));
                foreach (Match literal in StringLiteral.Matches(code))
                {
                    if (!SqlKeyword.IsMatch(literal.Value))
                    {
                        continue;
                    }

                    sqlLiterals++;
                    var body = literal.Value.Substring(literal.Value.IndexOf('"') + 1);
                    body = body.Substring(0, body.Length - 1);
                    if (body.Contains("\\\"") || body.Contains("\"\""))
                    {
                        offenders.Add(RepoPaths.Relative(file) + ":" + SourceScan.LineOf(code, literal.Index) + ": " + literal.Value);
                    }
                }
            }

            Assert.That(sqlLiterals, Is.GreaterThan(20), "el escaneo no encontró el SQL");
            Assert.That(offenders, Is.Empty, "SQL con comillas dobles (usar comillas simples):\n" + string.Join("\n", offenders));
            Assert.That(SqlKeyword.IsMatch("\"SELECT 1\""), Is.True);
            Assert.That(SqlKeyword.IsMatch("\"Seleccionar todo\""), Is.False);
        }

        // Literales de texto normales y textuales (@"…" con "" adentro), sobre código sin comentarios.
        private static readonly Regex StringLiteral = new Regex(@"@""(?:[^""]|"""")*""|""(?:[^""\\\n]|\\.)*""");

        // Una palabra clave de SQL en mayúsculas al principio del texto o después de un espacio.
        private static readonly Regex SqlKeyword = new Regex(@"(^|[\s""(])(SELECT|INSERT|UPDATE|DELETE|CREATE|PRAGMA|DROP|ALTER|VALUES|WHERE)\b");

        [Test, Category("AC-APP-19-03")]
        public void NoSystemRandomOutsideSystemRandom()
        {
            // AC-BASE-07: el azar es inyectable (IRandom); System.Random solo en Core/Common/SystemRandom.cs.
            var patterns = new[]
            {
                new Regex(@"\bnew\s+(System\.)?Random\s*\("),
                new Regex(@"\bSystem\.Random\b"),
                new Regex(@"\bRandom\.Shared\b"),
            };
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles(""))
            {
                if (RepoPaths.Relative(file) == "Assets/RowMate/Core/Common/SystemRandom.cs")
                {
                    continue;
                }

                offenders.AddRange(SourceScan.Matches(file, patterns, true));
            }

            Assert.That(offenders, Is.Empty, "System.Random fuera de SystemRandom.cs:\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-APP-19-03")]
        public void NoWallClockInPureCode()
        {
            // ADR 005: el reloj es inyectable (IClock/IScheduler).
            var patterns = new[] { new Regex(@"\bDateTime(Offset)?\s*\.\s*(Now|UtcNow|Today)\b") };
            AssertNoMatches(PureProduction, patterns, true, "reloj del sistema en código puro (usar IClock)");
        }

        // It. 4a (02-tech-design §2.2, §4.1): el motor y sus stores cuentan el tiempo SOLO con el programador (la hora del cuadro
        // y el reloj monótono de IScheduler, ADR 017): ni la hora del sistema, ni un cronómetro, ni timers o esperas propias.
        // Así el alcance del segundo plano y las pruebas con FakeScheduler y LifecycleHarness son deterministas.
        [Test, Category("AC-WKT-02-04"), Category("AC-APP-19-03")]
        public void NoSystemClockInTheWorkoutEngine()
        {
            var patterns = new[]
            {
                new Regex(@"\bDateTime(Offset)?\s*\.\s*(Now|UtcNow|Today)\b"),
                new Regex(@"\bStopwatch\b"),
                new Regex(@"\bEnvironment\s*\.\s*TickCount(64)?\b"),
                new Regex(@"\bTimeProvider\b"),
                new Regex(@"\bSystem\s*\.\s*Threading\s*\.\s*Timer\b"),
                new Regex(@"\bnew\s+Timer\s*\("),
                new Regex(@"\bTask\s*\.\s*Delay\s*\("),
                new Regex(@"\bThread\s*\.\s*Sleep\s*\("),
            };
            AssertNoMatches(new[] { "Core/Workout", "App/Workout" }, patterns, true, "reloj o timer del sistema en el motor (usar IScheduler)");
            Assert.That(SourceScan.CsFiles("Core/Workout").Count, Is.GreaterThan(10), "el escaneo encontró el motor");
        }

        // AC-WKT-06-14 (QA-G4aT-09): los parciales, las repeticiones y el resultado de objetivos de cada paso no se guardan
        // (paridad, workout_provider.dart:57-61). Infrastructure no conoce el motor (Core/Workout) y el SQL de las sesiones no
        // nombra objetivos, parciales, vueltas, series ni resultados: solo las columnas de workout_sessions y data_points (v8).
        [Test, Category("AC-WKT-06-14")]
        public void InfrastructureDoesNotKnowTargetsOrSplits()
        {
            var types = new[]
            {
                new Regex(@"\busing\s+(static\s+)?RowMate\.Core\.Workout\b"),
                new Regex(@"\bRowMate\s*\.\s*Core\s*\.\s*Workout\s*\."),
                new Regex(@"\b(StepTargetTracker|StepResult|TargetLight|TargetLights|SeriesTracker|SessionRecorder|WorkoutEngine|StepProgress)\b"),
            };
            AssertNoMatches(new[] { "Infrastructure" }, types, true, "Infrastructure conoce el motor");
            var offenders = new List<string>();
            var sqlLiterals = 0;
            // Como palabra (en snake_case el guion bajo separa): "elapsed_seconds" no es una vuelta.
            var forbidden = new Regex(@"(?i)(^|[^a-z])(targets?|splits?|laps?|repetitions?|reps?|series|results?)([^a-z]|$)");
            Assert.That(forbidden.IsMatch("\"step_target_met\""), Is.True);
            Assert.That(forbidden.IsMatch("\"SELECT elapsed_seconds FROM data_points\""), Is.False);
            foreach (var file in SourceScan.CsFiles("Infrastructure/Sessions"))
            {
                var code = SourceScan.StripComments(File.ReadAllText(file));
                foreach (Match literal in StringLiteral.Matches(code))
                {
                    if (forbidden.IsMatch(literal.Value))
                    {
                        offenders.Add(RepoPaths.Relative(file) + ":" + SourceScan.LineOf(code, literal.Index) + ": " + literal.Value);
                    }

                    sqlLiterals += SqlKeyword.IsMatch(literal.Value) ? 1 : 0;
                }
            }

            Assert.That(sqlLiterals, Is.GreaterThan(4), "el escaneo encontró el SQL de las sesiones");
            Assert.That(offenders, Is.Empty, "el SQL de las sesiones nombra objetivos o parciales:\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-BASE-04"), Category("AC-APP-19-03")]
        public void NoColorLiteralsOutsideTokens()
        {
            // ADR 016: el hex de un color existe solo en Core/Design/Tokens.cs; en USS, solo en Tokens.uss (generado).
            var hexLiteral = new[] { new Regex(@"""#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?""") };
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles("Core", "App", "Simulator", "Presentation"))
            {
                if (RepoPaths.Relative(file) == "Assets/RowMate/Core/Design/Tokens.cs")
                {
                    continue;
                }

                offenders.AddRange(SourceScan.Matches(file, hexLiteral, false));
            }

            var ussHex = new Regex(@"#[0-9A-Fa-f]{3,8}\b");
            foreach (var file in RepoPaths.Files("Assets", "*.uss"))
            {
                if (RepoPaths.Relative(file) == "Assets/RowMate/Presentation/Styles/Tokens.uss")
                {
                    continue;
                }

                var text = SourceScan.StripComments(File.ReadAllText(file));
                foreach (Match match in ussHex.Matches(text))
                {
                    offenders.Add(RepoPaths.Relative(file) + ": " + match.Value);
                }
            }

            Assert.That(offenders, Is.Empty, "literales de color fuera de Tokens.cs/Tokens.uss:\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-APP-19-03")]
        public void NoFbxOutsideThirdParty()
        {
            // ARCHITECTURE §2: no hay FBX propios; los de terceros viven solo en Assets/ThirdParty/ (y no se versionan).
            var offenders = new List<string>();
            foreach (var file in SourceScan.RepositoryFiles("*.fbx"))
            {
                var relative = RepoPaths.Relative(file);
                if (!relative.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal))
                {
                    offenders.Add(relative);
                }
            }

            Assert.That(offenders, Is.Empty, "FBX fuera de Assets/ThirdParty/:\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-BASE-08")]
        public void SourceScanStripsCommentsAndLiterals()
        {
            // Control del lector de fuentes que usan estas reglas, la compatibilidad con NUnit 3.5 y la trazabilidad.
            var source = "var a = \"Math.Round(\"; // Math.Round(\n/* async void */ var b = @\"x\"\"y\"; var c = $\"{(d ? \"e\" : \"f\")}\"; var g = '\\'';";
            var noComments = SourceScan.StripComments(source);
            Assert.That(noComments, Does.Contain("\"Math.Round(\""));
            Assert.That(noComments, Does.Not.Contain("async void"));
            var code = SourceScan.StripCommentsAndLiterals(source);
            Assert.That(code, Does.Not.Contain("Math.Round"));
            Assert.That(code, Does.Contain("var c = $\""));
            Assert.That(code, Does.Contain("d ?"));
            Assert.That(code, Does.Contain("var g ="));
            Assert.That(code.Split('\n').Length, Is.EqualTo(source.Split('\n').Length));
        }

        private static void AssertNoMatches(string[] folders, Regex[] patterns, bool blankLiterals, string what)
        {
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles(folders))
            {
                offenders.AddRange(SourceScan.Matches(file, patterns, blankLiterals));
            }

            Assert.That(offenders, Is.Empty, what + ":\n" + string.Join("\n", offenders));
        }
    }

    // Lector de fuentes C# para las reglas estáticas: quita comentarios y, si se pide, el contenido de los literales de
    // texto y de carácter (conserva las comillas y los saltos de línea, así los números de línea siguen valiendo).
    // Soporta "…", @"…", $"…" con huecos {…} anidados, $@"…" y '…'. C# 9 no tiene raw strings.
    internal static class SourceScan
    {
        // Archivos .cs bajo Assets/RowMate/<carpeta> ("" = todo Assets/RowMate), sin repetidos, en orden ordinal.
        public static IReadOnlyList<string> CsFiles(params string[] folders)
        {
            var result = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var folder in folders)
            {
                var relative = folder.Length == 0 ? "Assets/RowMate" : "Assets/RowMate/" + folder;
                foreach (var file in RepoPaths.Files(relative, "*.cs"))
                {
                    result.Add(file);
                }
            }

            return new List<string>(result);
        }

        // Archivos del repositorio con un patrón, sin entrar en .git, Library, Temp ni salidas de compilación.
        public static IReadOnlyList<string> RepositoryFiles(string searchPattern)
        {
            var result = new List<string>();
            Collect(new DirectoryInfo(RepoPaths.Root), searchPattern, result);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        // "<ruta>:<línea>: <coincidencia>" por cada patrón que aparece en el archivo.
        public static IEnumerable<string> Matches(string file, Regex[] patterns, bool blankLiterals)
        {
            var text = File.ReadAllText(file);
            var code = blankLiterals ? StripCommentsAndLiterals(text) : StripComments(text);
            foreach (var pattern in patterns)
            {
                foreach (Match match in pattern.Matches(code))
                {
                    yield return RepoPaths.Relative(file) + ":" + LineOf(code, match.Index) + ": " + match.Value.Trim();
                }
            }
        }

        public static string StripComments(string source)
        {
            return Scan(source, false);
        }

        public static string StripCommentsAndLiterals(string source)
        {
            return Scan(source, true);
        }

        public static int LineOf(string text, int index)
        {
            var line = 1;
            for (var i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                }
            }

            return line;
        }

        private static void Collect(DirectoryInfo directory, string pattern, List<string> result)
        {
            foreach (var file in directory.GetFiles(pattern))
            {
                result.Add(file.FullName);
            }

            foreach (var child in directory.GetDirectories())
            {
                var name = child.Name;
                if (name == ".git" || name == ".artifacts" || name == "Library" || name == "Temp" || name == "obj" || name == "bin"
                    || name == "Logs" || name == "node_modules" || name == ".dart_tool")
                {
                    continue;
                }

                Collect(child, pattern, result);
            }
        }

        private static string Scan(string source, bool blankLiterals)
        {
            var text = source.Replace("\r\n", "\n");
            var sb = new StringBuilder(text.Length);
            var i = 0;
            ScanCode(text, ref i, sb, blankLiterals, false);
            return sb.ToString();
        }

        // Copia código hasta el final, o (inHole) hasta la '}' que cierra un hueco de interpolación, sin consumirla.
        private static void ScanCode(string s, ref int i, StringBuilder sb, bool blank, bool inHole)
        {
            var depth = 0;
            while (i < s.Length)
            {
                var c = s[i];
                var next = i + 1 < s.Length ? s[i + 1] : '\0';
                if (c == '/' && next == '/')
                {
                    while (i < s.Length && s[i] != '\n')
                    {
                        sb.Append(' ');
                        i++;
                    }

                    continue;
                }

                if (c == '/' && next == '*')
                {
                    sb.Append("  ");
                    i += 2;
                    while (i < s.Length && !(s[i] == '*' && i + 1 < s.Length && s[i + 1] == '/'))
                    {
                        sb.Append(s[i] == '\n' ? '\n' : ' ');
                        i++;
                    }

                    if (i < s.Length)
                    {
                        sb.Append("  ");
                        i += 2;
                    }

                    continue;
                }

                if (c == '\'')
                {
                    ScanChar(s, ref i, sb, blank);
                    continue;
                }

                if (c == '"' || (c == '@' && next == '"') || (c == '$' && (next == '"' || next == '@')) || (c == '@' && next == '$'))
                {
                    ScanString(s, ref i, sb, blank);
                    continue;
                }

                if (inHole)
                {
                    if (c == '{')
                    {
                        depth++;
                    }
                    else if (c == '}')
                    {
                        if (depth == 0)
                        {
                            return;
                        }

                        depth--;
                    }
                }

                sb.Append(c);
                i++;
            }
        }

        private static void ScanChar(string s, ref int i, StringBuilder sb, bool blank)
        {
            sb.Append('\'');
            i++;
            while (i < s.Length && s[i] != '\'' && s[i] != '\n')
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    sb.Append(blank ? "  " : s.Substring(i, 2));
                    i += 2;
                    continue;
                }

                sb.Append(blank ? ' ' : s[i]);
                i++;
            }

            if (i < s.Length && s[i] == '\'')
            {
                sb.Append('\'');
                i++;
            }
        }

        private static void ScanString(string s, ref int i, StringBuilder sb, bool blank)
        {
            var verbatim = false;
            var interpolated = false;
            while (s[i] == '@' || s[i] == '$')
            {
                verbatim |= s[i] == '@';
                interpolated |= s[i] == '$';
                sb.Append(s[i]);
                i++;
            }

            sb.Append('"');
            i++;
            while (i < s.Length)
            {
                var c = s[i];
                if (verbatim && c == '"' && i + 1 < s.Length && s[i + 1] == '"')
                {
                    sb.Append(blank ? "  " : "\"\"");
                    i += 2;
                    continue;
                }

                if (c == '"')
                {
                    sb.Append('"');
                    i++;
                    return;
                }

                if (!verbatim && c == '\\' && i + 1 < s.Length)
                {
                    sb.Append(blank ? "  " : s.Substring(i, 2));
                    i += 2;
                    continue;
                }

                if (!verbatim && c == '\n')
                {
                    return; // texto sin cerrar: C# no lo compilaría
                }

                if (interpolated && c == '{')
                {
                    if (i + 1 < s.Length && s[i + 1] == '{')
                    {
                        sb.Append(blank ? "  " : "{{");
                        i += 2;
                        continue;
                    }

                    sb.Append('{');
                    i++;
                    ScanCode(s, ref i, sb, blank, true);
                    if (i < s.Length)
                    {
                        sb.Append('}');
                        i++;
                    }

                    continue;
                }

                if (interpolated && c == '}' && i + 1 < s.Length && s[i + 1] == '}')
                {
                    sb.Append(blank ? "  " : "}}");
                    i += 2;
                    continue;
                }

                sb.Append(blank && c != '\n' ? ' ' : c);
                i++;
            }
        }
    }
}
