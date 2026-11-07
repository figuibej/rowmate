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

        // It. 4b (02-tech-design §1.2): ningún reloj del sistema en las reglas nuevas de Core/Workout, Core/Design y App/Workout
        // (el tiempo llega por argumento o por el IScheduler), y el reloj despierto (SuspendAccounting, FrameClock, SystemWake)
        // recibe todo por argumento: ni Stopwatch ni la hora del sistema.
        [Test, Category("AC-WKT-02-11"), Category("AC-APP-19-03")]
        public void NoSystemClockInTheFourBRules()
        {
            var patterns = new[]
            {
                new Regex(@"\bDateTime(Offset)?\s*\.\s*(Now|UtcNow|Today)\b"),
                new Regex(@"\bStopwatch\b"),
                new Regex(@"\bEnvironment\s*\.\s*TickCount(64)?\b"),
                new Regex(@"\bTimeProvider\b"),
                new Regex(@"\bTask\s*\.\s*Delay\s*\("),
                new Regex(@"\bnew\s+Timer\s*\("),
            };
            var files = new[]
            {
                "Core/Workout/StepPills.cs", "Core/Workout/StepOutcome.cs", "Core/Workout/StepSeal.cs", "Core/Workout/BigCountdown.cs",
                "Core/Workout/PhaseChip.cs", "Core/Workout/CompletionSummary.cs", "Core/Workout/CloseStatus.cs",
                "Core/Design/PillRowGeometry.cs", "Core/Design/PillShapes.cs", "Core/Design/PillProgress.cs", "Core/Design/InfoRowFit.cs",
                "Core/Design/CountUp.cs", "Core/Design/HudTimelines.cs", "Core/Design/CheckGlyph.cs",
                "Core/Common/SuspendAccounting.cs", "Core/Common/FrameClock.cs", "Core/Common/SystemWake.cs",
                "App/Workout/WallClockTicker.cs", "App/Workout/CompletionDialogRule.cs", "App/Workout/ImmersiveSystemBars.cs",
            };
            var offenders = new List<string>();
            foreach (var relative in files)
            {
                var path = RepoPaths.Combine("Assets/RowMate/" + relative);
                Assert.That(File.Exists(path), Is.True, relative);
                offenders.AddRange(SourceScan.Matches(path, patterns, true));
            }

            Assert.That(offenders, Is.Empty, "reloj del sistema en una regla pura de la It. 4b:\n" + string.Join("\n", offenders));
        }

        // It. 5 (05-series-parciales/02-tech-design §1.2): ningún reloj del sistema en las reglas nuevas (el tiempo llega por
        // argumento: el reloj activo del motor).
        [Test, Category("AC-WKT-07-10"), Category("AC-APP-19-03")]
        public void NoSystemClockInTheFiveRules()
        {
            var patterns = new[]
            {
                new Regex(@"\bDateTime(Offset)?\s*\.\s*(Now|UtcNow|Today)\b"),
                new Regex(@"\bStopwatch\b"),
                new Regex(@"\bEnvironment\s*\.\s*TickCount(64)?\b"),
                new Regex(@"\bTimeProvider\b"),
                new Regex(@"\bTask\s*\.\s*Delay\s*\("),
                new Regex(@"\bnew\s+Timer\s*\("),
                new Regex(@"\bUnityEngine\b"),
            };
            var files = new[]
            {
                "Core/Workout/SeriesTracker.cs", "Core/Workout/SeriesPanels.cs", "Core/Workout/SanitizedDistance.cs", "Core/Design/SeriesPanelGeometry.cs",
                "Core/Design/SeriesTimelines.cs", "Core/Common/MemoryPreferences.cs", "Core/Ports/IPreferences.cs", "App/Workout/SeriesPanelPrefs.cs",
                "App/Workout/SeriesPanelsExpansion.cs", "App/Workout/HudSeriesChange.cs",
            };
            var offenders = new List<string>();
            foreach (var relative in files)
            {
                var path = RepoPaths.Combine("Assets/RowMate/" + relative);
                Assert.That(File.Exists(path), Is.True, relative);
                offenders.AddRange(SourceScan.Matches(path, patterns, true));
            }

            Assert.That(offenders, Is.Empty, "reloj del sistema en una regla pura de la It. 5:\n" + string.Join("\n", offenders));
        }

        // It. 5 (spec §12 punto 2; DV-26, DT5-06; AC-WKT-07-09): UN solo saneamiento de la distancia por sesión, el del
        // SeriesTracker. El motor no tiene otro (ni un SanitizedDistance propio ni la fórmula del salto) y lo alimenta una sola vez
        // por segundo; en todo el código de producción, el único que crea un SanitizedDistance es el tracker.
        [Test, Category("AC-WKT-07-09"), Category("AC-WKT-03-03")]
        public void OneDistanceSanitizerOnly()
        {
            var engine = SourceScan.StripComments(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Core/Workout/WorkoutEngine.cs")));
            Assert.That(engine, Does.Not.Contain("new SanitizedDistance"), "el motor no tiene su propio saneamiento");
            Assert.That(engine, Does.Not.Contain("MaxSpeedMetersPerSecond"), "ni la fórmula del salto");
            Assert.That(engine, Does.Not.Contain("SanitizedDistance"));
            Assert.That(Regex.Matches(engine, @"_series\s*\.\s*Update\s*\(").Count, Is.EqualTo(1), "una sola alimentación por segundo");
            Assert.That(Regex.Matches(engine, @"_series\s*\.\s*Meters\b").Count, Is.GreaterThanOrEqualTo(2), "el paso por distancia lee los metros del tracker");
            var creators = new List<string>();
            var formula = new List<string>();
            foreach (var file in SourceScan.CsFiles(PureProduction))
            {
                var code = SourceScan.StripComments(File.ReadAllText(file));
                if (Regex.IsMatch(code, @"\bnew\s+SanitizedDistance\s*\("))
                {
                    creators.Add(RepoPaths.Relative(file));
                }

                if (Regex.IsMatch(code, @"MaxSpeedMetersPerSecond\s*\*"))
                {
                    formula.Add(RepoPaths.Relative(file));
                }
            }

            Assert.That(creators, Is.EqualTo(new[] { "Assets/RowMate/Core/Workout/SeriesTracker.cs" }));
            Assert.That(formula, Is.EqualTo(new[] { "Assets/RowMate/Core/Workout/SanitizedDistance.cs" }), "la regla del salto, en un solo lugar");
        }

        // ------------------------------------------------------------------ It. 6a (06a-escena-lago/02-tech-design §2, §6.3)

        // It. 7 (07-remero-palada/02-tech-design §1.1, §6.3): la palada es Core puro y determinista. Core/Scene/Stroke sin Unity, sin
        // reloj ni System.Random (solo DartRandom con semilla fija: dos corridas del arnés iguales byte a byte).
        [Test, Category("AC-SCN-08-11"), Category("AC-APP-19-03")]
        public void TheStrokeCoreIsPureAndDeterministic()
        {
            var patterns = new[]
            {
                new Regex(@"\bUnityEngine\b"),
                new Regex(@"\bDateTime(Offset)?\s*\.\s*(Now|UtcNow|Today)\b"),
                new Regex(@"\bStopwatch\b"),
                new Regex(@"\bEnvironment\s*\.\s*TickCount(64)?\b"),
                new Regex(@"\bnew\s+(System\s*\.\s*)?Random\s*\("),
                new Regex(@"\bGuid\s*\.\s*NewGuid\b"),
            };
            var files = new List<string>(SourceScan.CsFiles("Core/Scene/Stroke"));
            Assert.That(files.Count, Is.GreaterThanOrEqualTo(20), "el escaneo encontró Core/Scene/Stroke");
            var offenders = new List<string>();
            foreach (var file in files)
            {
                offenders.AddRange(SourceScan.Matches(file, patterns, true));
            }

            Assert.That(offenders, Is.Empty, "Unity, reloj o azar sin semilla en la palada:\n" + string.Join("\n", offenders));
        }

        // Toda regla de la escena es Core puro (Q-69, AC-SCN-01-14): Core/Scene (con Art y Geometry) y las líneas de tiempo y el
        // contraste de Core/Design, sin reloj del sistema, cronómetro, timers ni Unity. El tiempo llega por SceneTickInputs.
        [Test, Category("AC-SCN-01-14"), Category("AC-APP-19-03")]
        public void NoSystemClockInTheSceneCore()
        {
            var patterns = new[]
            {
                new Regex(@"\bDateTime(Offset)?\s*\.\s*(Now|UtcNow|Today)\b"),
                new Regex(@"\bStopwatch\b"),
                new Regex(@"\bEnvironment\s*\.\s*TickCount(64)?\b"),
                new Regex(@"\bTimeProvider\b"),
                new Regex(@"\bTask\s*\.\s*Delay\s*\("),
                new Regex(@"\bnew\s+Timer\s*\("),
                new Regex(@"\bThread\s*\.\s*Sleep\s*\("),
                new Regex(@"\bUnityEngine\b"),
            };
            var files = new List<string>(SourceScan.CsFiles("Core/Scene"));
            Assert.That(files.Count, Is.GreaterThan(40), "el escaneo encontró Core/Scene");
            files.Add(RepoPaths.Combine("Assets/RowMate/Core/Design/SceneTimelines.cs"));
            files.Add(RepoPaths.Combine("Assets/RowMate/Core/Design/HudContrast.cs"));
            var offenders = new List<string>();
            foreach (var file in files)
            {
                Assert.That(File.Exists(file), Is.True, file);
                offenders.AddRange(SourceScan.Matches(file, patterns, true));
            }

            Assert.That(offenders, Is.Empty, "reloj del sistema o Unity en el Core de la escena:\n" + string.Join("\n", offenders));
        }

        // AC-SCN-02-08: una sola cámara y sin entrada de usuario. Ninguna clase de la escena (Core/Scene y la escena de Unity,
        // Scene/) nombra la entrada: Input, el Input System, Pointer*, Keyboard, Mouse ni Touch.
        [Test, Category("AC-SCN-02-08")]
        public void TheSceneHasNoUserInput()
        {
            var patterns = new[]
            {
                new Regex(@"\bInput\s*\."),
                new Regex(@"\bUnityEngine\s*\.\s*InputSystem\b"),
                new Regex(@"\bInputSystem\b"),
                new Regex(@"\bPointer[A-Z]\w*"),
                new Regex(@"\b(Keyboard|Mouse|Touchscreen|Touch|Gamepad)\b"),
                new Regex(@"\bIPointer\w*Handler\b"),
                new Regex(@"\bOnMouse(Down|Up|Drag|Over)\b"),
            };
            Assert.That(patterns[0].IsMatch("var x = Input.GetAxis(\"a\");"), Is.True, "control positivo");
            Assert.That(patterns[3].IsMatch("RegisterCallback<PointerDownEvent>(OnDown);"), Is.True, "control positivo");
            Assert.That(patterns[0].IsMatch("in SceneTickInputs inputs"), Is.False, "las entradas de la escena no son entrada de usuario");
            AssertNoMatches(new[] { "Core/Scene", "Scene" }, patterns, true, "entrada de usuario en la escena (AC-SCN-02-08)");
        }

        // AC-SCN-03-05, AC-WKT-13-14: la distancia de escena es propia de la escena. Fuera de Core/Scene y de la escena de Unity
        // (Scene/), ningún código de producción nombra el estado, la sesión ni el cuadro de la escena (donde vive la distancia),
        // así el motor, el HUD y los datos nunca la leen. El arnés (Snapshots), el Editor y las pruebas quedan afuera.
        [Test, Category("AC-SCN-03-05"), Category("AC-WKT-13-14")]
        public void TheSceneDistanceStaysInTheScene()
        {
            var patterns = new[]
            {
                new Regex(@"\b(SceneState|SceneSession|SceneFrame|ShaderDistance|ForceDistanceForTest)\b"),
            };
            var folders = new[] { "Core", "App", "Simulator", "Infrastructure", "Presentation", "Platform", "SimulatorPresentation", "Bootstrap" };
            var offenders = new List<string>();
            var scanned = 0;
            foreach (var file in SourceScan.CsFiles(folders))
            {
                if (RepoPaths.Relative(file).StartsWith("Assets/RowMate/Core/Scene/", StringComparison.Ordinal))
                {
                    continue;
                }

                scanned++;
                offenders.AddRange(SourceScan.Matches(file, patterns, true));
            }

            Assert.That(scanned, Is.GreaterThan(200), "el escaneo encontró el código de producción");
            Assert.That(offenders, Is.Empty, "la distancia de escena se lee fuera de la escena:\n" + string.Join("\n", offenders));
            var engine = SourceScan.StripCommentsAndLiterals(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Core/Workout/WorkoutEngine.cs")));
            Assert.That(engine, Does.Not.Contain("RowMate.Core.Scene"), "el motor no conoce la escena");
        }

        // QA-G6aT-05: en el Editor nunca se asigna un asset de URP a la configuración del proyecto ni se cambia la compilación
        // asíncrona de la configuración del Editor. Solo Editor/Bootstrap/UrpProvisioner.cs asigna QualitySettings.renderPipeline y
        // GraphicsSettings.defaultRenderPipeline (el bootstrapper); nadie usa EditorSettings.asyncShaderCompilation (las pruebas y
        // el arnés usan ShaderUtil.allowAsyncCompilation, que no se guarda).
        [Test, Category("AC-APP-19-03")]
        public void OnlyTheProvisionerAssignsTheRenderPipeline()
        {
            var assign = new[]
            {
                new Regex(@"\bQualitySettings\s*\.\s*renderPipeline\s*=(?!=)"),
                new Regex(@"\bGraphicsSettings\s*\.\s*defaultRenderPipeline\s*=(?!=)"),
                new Regex(@"\bGraphicsSettings\s*\.\s*renderPipelineAsset\s*=(?!=)"),
            };
            var asyncSetting = new[] { new Regex(@"\bEditorSettings\s*\.\s*asyncShaderCompilation\b") };
            const string Provisioner = "Assets/RowMate/Editor/Bootstrap/UrpProvisioner.cs";
            var offenders = new List<string>();
            var provisionerAssigns = 0;
            foreach (var file in SourceScan.CsFiles(""))
            {
                var matches = new List<string>(SourceScan.Matches(file, assign, true));
                if (RepoPaths.Relative(file) == Provisioner)
                {
                    provisionerAssigns = matches.Count;
                }
                else
                {
                    offenders.AddRange(matches);
                }

                offenders.AddRange(SourceScan.Matches(file, asyncSetting, true));
            }

            Assert.That(provisionerAssigns, Is.GreaterThanOrEqualTo(2), "el escaneo encontró las asignaciones del bootstrapper");
            Assert.That(assign[0].IsMatch("if (QualitySettings.renderPipeline == x)"), Is.False, "una comparación no es una asignación");
            Assert.That(offenders, Is.Empty, "un asset de URP asignado fuera del bootstrapper o la compilación asíncrona del Editor:\n"
                + string.Join("\n", offenders));
        }

        // QA-G6aT-14: los controles de prueba internal de Core (SceneSession.ForceDistanceForTest, FreezeSceneTimeForTest,
        // ForceHourForTest, BreathingOffForTest; SceneState.ForceForTest) solo se llaman, en la escena de Unity (Scene/), desde
        // miembros internal *ForTest propios: así ningún camino de producción los alcanza (Core/AssemblyInfo.cs da los internos a
        // RowMate.Scene por eso).
        [Test, Category("AC-SCN-01-14"), Category("AC-APP-19-03")]
        public void SceneCallsCoreTestControlsOnlyFromItsOwnTestControls()
        {
            var controls = CoreTestControls();
            Assert.That(controls, Is.SupersetOf(new[] { "ForceDistanceForTest", "FreezeSceneTimeForTest", "ForceHourForTest", "BreathingOffForTest" }));
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles("Scene"))
            {
                offenders.AddRange(TestControlOffenders(RepoPaths.Relative(file), File.ReadAllText(file), controls));
            }

            Assert.That(offenders, Is.Empty, "controles de prueba de Core llamados fuera de un control internal *ForTest:\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-APP-19-03")]
        public void TheTestControlRuleFindsAViolation()
        {
            // Control positivo de la regla anterior: un control llamado desde Tick (público) se detecta; desde un internal *ForTest,
            // no; un comentario no cuenta.
            var controls = new HashSet<string>(StringComparer.Ordinal) { "ForceHourForTest" };
            var sample = "namespace X\n{\n    public sealed class Host\n    {\n        private SceneSession _s;\n"
                + "        public void Tick()\n        {\n            _s.ForceHourForTest(12);\n        }\n\n"
                + "        internal void ForceHourForTest(double? hour)\n        {\n            _s?.ForceHourForTest(hour);\n        }\n\n"
                + "        private void Other()\n        {\n            // _s.ForceHourForTest(3);\n        }\n    }\n}\n";
            var offenders = TestControlOffenders("Host.cs", sample, controls);
            Assert.That(offenders.Count, Is.EqualTo(1), string.Join("\n", offenders));
            Assert.That(offenders[0], Does.StartWith("Host.cs:8:"));
        }

        // AC-SCN-23-03 (AC-BLE-16-11): la hora forzada de la escena no se guarda. Ninguna clave de preferencias (ni ningún otro
        // literal del código de producción) nombra la hora de la escena.
        [Test, Category("AC-BLE-16-11"), Category("AC-BLE-16-07")]
        public void NoPreferenceKeyNamesTheSceneHour()
        {
            // Una clave es un literal sin espacios (un texto de la UI como "Hora escena" no lo es).
            var key = new Regex(@"(?i)""[\w.\-]*(scene[._-]?hour|hour[._-]?override|forced[._-]?hour|hora[._-]?escena)[\w.\-]*""");
            Assert.That(key.IsMatch("\"scene.hourOverride\""), Is.True, "control positivo");
            Assert.That(key.IsMatch("\"simulator.sceneHour\""), Is.True, "control positivo");
            Assert.That(key.IsMatch("\"immersive.lapsExpanded\""), Is.False);
            Assert.That(key.IsMatch("\"Hora escena\""), Is.False, "el rótulo del panel no es una clave");
            var folders = new[] { "Core", "App", "Simulator", "Infrastructure", "Presentation", "Platform", "SimulatorPresentation", "Bootstrap", "Scene" };
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles(folders))
            {
                var code = SourceScan.StripComments(File.ReadAllText(file));
                foreach (Match match in key.Matches(code))
                {
                    offenders.Add(RepoPaths.Relative(file) + ":" + SourceScan.LineOf(code, match.Index) + ": " + match.Value);
                }
            }

            Assert.That(offenders, Is.Empty, "un literal (¿una clave de preferencias?) nombra la hora de la escena:\n" + string.Join("\n", offenders));
        }

        // Los nombres de los controles de prueba internal de Core: internal <tipo> <Nombre>ForTest(.
        private static HashSet<string> CoreTestControls()
        {
            var declaration = new Regex(@"\binternal\s+[\w<>\[\]?,\s]+?\s+(\w+ForTest)\s*\(");
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in SourceScan.CsFiles("Core"))
            {
                foreach (Match match in declaration.Matches(SourceScan.StripCommentsAndLiterals(File.ReadAllText(file))))
                {
                    result.Add(match.Groups[1].Value);
                }
            }

            return result;
        }

        // Cada llamada a un control de Core (x.Nombre( o x?.Nombre() tiene que estar adentro de un miembro internal cuyo nombre
        // termina en ForTest: el miembro es la última declaración con modificador de acceso antes de la llamada.
        private static List<string> TestControlOffenders(string file, string text, ISet<string> controls)
        {
            var code = SourceScan.StripCommentsAndLiterals(text.Replace("\r\n", "\n"));
            var member = new Regex(@"^[ \t]*((?:public|internal|private|protected)(?:\s+(?:static|override|virtual|sealed|async|unsafe|new|readonly|extern|partial))*)\s+[\w<>\[\]?,.\s]+?\s+(\w+)\s*(\(|\{|=>)", RegexOptions.Multiline);
            var members = member.Matches(code);
            var call = new Regex(@"\??\.\s*(\w+ForTest)\s*\(");
            var offenders = new List<string>();
            foreach (Match match in call.Matches(code))
            {
                if (!controls.Contains(match.Groups[1].Value))
                {
                    continue;
                }

                Match? owner = null;
                foreach (Match m in members)
                {
                    if (m.Index < match.Index)
                    {
                        owner = m;
                    }
                }

                var ok = owner != null
                    && Regex.IsMatch(owner.Groups[1].Value, @"\binternal\b")
                    && owner.Groups[2].Value.EndsWith("ForTest", StringComparison.Ordinal);
                if (!ok)
                {
                    offenders.Add(file + ":" + SourceScan.LineOf(code, match.Index) + ": " + match.Groups[1].Value + " en " + (owner == null ? "?" : owner.Groups[2].Value));
                }
            }

            return offenders;
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
