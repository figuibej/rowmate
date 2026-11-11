using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Tests.EditMode.Project;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Localization
{
    // VS · Escaneo estático de textos visibles fuera del catálogo (It. 14, AC-APP-11-01; DT14-26): busca literales con letras en
    // ".text =", "new Label(", "tooltip =", "SetText(", "Title =" (C#) y text="…" (UXML) dentro de Presentation/**, App/** y
    // Bootstrap/**. Quedan afuera las carpetas Simulator*/ (Q-03) y Presentation/Diagnostics/ (Q-04), y lo que está en la lista
    // blanca versionada tools/l10n/visible-text-allowlist.json (file, literal, reason: Q-03, Q-04, brand o format). La capa en
    // vivo (TextAuditProbe, TextAuditTourTests) es de WP-B.
    [TestFixture]
    public sealed class VisibleTextScanTests
    {
        public const string AllowlistPath = "tools/l10n/visible-text-allowlist.json";

        // WP-A suma hidden (una etiqueta de medida invisible) y dev-only (solo en el Editor y los builds de desarrollo).
        private static readonly string[] Reasons = { "Q-03", "Q-04", "brand", "format", "hidden", "dev-only" };

        private const string Literal = @"\$?@?""((?:[^""\\\n]|\\.)*)""";

        private static readonly Regex[] CsPatterns =
        {
            new Regex(@"\btext\s*=\s*" + Literal),
            new Regex(@"\bnew\s+(?:Label|Button|Toggle)\s*\(\s*" + Literal),
            new Regex(@"\btooltip\s*=\s*" + Literal),
            new Regex(@"\bSetText\s*\(\s*" + Literal),
            new Regex(@"\bTitle\s*=\s*" + Literal),
        };

        private static readonly Regex UxmlText = new Regex(@"\btext\s*=\s*""([^""]*)""");

        [Test, Category("AC-APP-11-01")]
        public void NoVisibleLiteralOutsideTheCatalog()
        {
            var allowed = Allowlist();
            var used = new HashSet<string>(StringComparer.Ordinal);
            var offenders = new List<string>();
            foreach (var finding in Findings())
            {
                var key = finding.File + "\n" + finding.Literal;
                if (allowed.ContainsKey(key))
                {
                    used.Add(key);
                    continue;
                }

                offenders.Add(finding.File + ":" + finding.Line + ": \"" + finding.Literal + "\"");
            }

            Assert.That(offenders, Is.Empty, "textos visibles fuera del catálogo (agregalos al catálogo o, si son Q-03, Q-04, marca o formato, a "
                + AllowlistPath + "):\n" + string.Join("\n", offenders));
            var stale = new List<string>();
            foreach (var entry in allowed.Keys)
            {
                if (!used.Contains(entry))
                {
                    stale.Add(entry.Replace("\n", ": "));
                }
            }

            Assert.That(stale, Is.Empty, "entradas de la lista blanca que ya no aparecen:\n" + string.Join("\n", stale));
        }

        [Test, Category("AC-APP-11-01")]
        public void AllowlistEntriesHaveAKnownReason()
        {
            var json = JsonParser.Parse(File.ReadAllText(RepoPaths.Combine(AllowlistPath)));
            foreach (var entry in json["entries"].Items)
            {
                Assert.That(entry["file"].AsString(), Does.Not.Contain("\\").And.Not.StartWith("/"), "ruta relativa con /");
                Assert.That(Array.IndexOf(Reasons, entry["reason"].AsString()), Is.GreaterThanOrEqualTo(0), entry["file"].AsString() + ": " + entry["reason"].AsString());
                Assert.That(entry["literal"].AsString().Length, Is.GreaterThan(0));
            }
        }

        [Test, Category("AC-APP-11-01")]
        public void ScannerFindsTheKnownShapes()
        {
            var sample = "var b = new Button { text = \"Uno\" };\nlabel.text = \"Hola\";\nvar l = new Label(\"Chau\");\nb.tooltip = \"Ayuda\";\nx.SetText(\"123\");\n// x.text = \"comentario\";\ny.text = texts.Get(Key);\nTitle = $\"Hola {n}\";";
            var found = new List<string>();
            foreach (var finding in Scan("sample.cs", sample))
            {
                found.Add(finding.Literal);
            }

            Assert.That(found, Is.EqualTo(new[] { "Uno", "Hola", "Chau", "Ayuda", "Hola {n}" }), "sin números ni comentarios ni llamadas al catálogo");
            var uxml = new List<string>();
            foreach (var finding in Scan("sample.uxml", "<ui:Label text=\"Visible\" /><ui:Label text=\"42\" />"))
            {
                uxml.Add(finding.Literal);
            }

            Assert.That(uxml, Is.EqualTo(new[] { "Visible" }));
        }

        public static IEnumerable<Finding> Findings()
        {
            var files = new List<string>();
            files.AddRange(SourceScan.CsFiles("Presentation", "App", "Bootstrap"));
            files.AddRange(RepoPaths.Files("Assets/RowMate", "*.uxml"));
            foreach (var file in files)
            {
                var relative = RepoPaths.Relative(file);
                if (relative.Contains("/Simulator") || relative.Contains("/Presentation/Diagnostics/"))
                {
                    continue;
                }

                foreach (var finding in Scan(relative, File.ReadAllText(file)))
                {
                    yield return finding;
                }
            }
        }

        public static IEnumerable<Finding> Scan(string relative, string text)
        {
            if (relative.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Match match in UxmlText.Matches(text))
                {
                    if (HasLetter(match.Groups[1].Value))
                    {
                        yield return new Finding(relative, SourceScan.LineOf(text, match.Index), match.Groups[1].Value);
                    }
                }

                yield break;
            }

            var code = SourceScan.StripComments(text);
            foreach (var pattern in CsPatterns)
            {
                foreach (Match match in pattern.Matches(code))
                {
                    if (HasLetter(match.Groups[1].Value))
                    {
                        yield return new Finding(relative, SourceScan.LineOf(code, match.Index), match.Groups[1].Value);
                    }
                }
            }

            // It. 15 (#79, AC-D15-27, DT15-15): el argumento de texto de los controles de la app, por su posición en la firma real
            // (no cualquier literal: el nombre y la variante también son literales con letras).
            foreach (var control in TextArguments)
            {
                foreach (Match match in control.Value.Matches(code))
                {
                    var literal = Argument(code, match.Index + match.Length, control.Key.Position);
                    if (literal != null && HasLetter(literal))
                    {
                        yield return new Finding(relative, SourceScan.LineOf(code, match.Index), literal);
                    }
                }
            }
        }

        // Control → posición (desde 0) del argumento que se ve como texto, sacada de las firmas: RmButton(name, variant,
        // iconCodepoint, label, …) y SwitchToggle(name, label). IconButton y Fab no tienen texto (solo el glifo); ErrorLine, TextClamp
        // y RmTextField reciben el Label o lo arman con el catálogo.
        public static readonly KeyValuePair<(string Control, int Position), Regex>[] TextArguments =
        {
            new KeyValuePair<(string, int), Regex>(("RmButton", 3), new Regex(@"\bnew\s+RmButton\s*\(")),
            new KeyValuePair<(string, int), Regex>(("SwitchToggle", 1), new Regex(@"\bnew\s+SwitchToggle\s*\(")),
        };

        private static readonly Regex LiteralOnly = new Regex(@"^\s*" + Literal + @"\s*$");

        // El argumento n.º position de la llamada que abre en start (justo después del paréntesis), si es un literal; null si no.
        // Respeta paréntesis, llaves y corchetes anidados y las comillas.
        private static string? Argument(string code, int start, int position)
        {
            var depth = 0;
            var index = 0;
            var from = start;
            for (var i = start; i < code.Length; i++)
            {
                var c = code[i];
                if (c == '"')
                {
                    var verbatim = i > 0 && code[i - 1] == '@';
                    for (i++; i < code.Length; i++)
                    {
                        if (!verbatim && code[i] == '\\')
                        {
                            i++;
                            continue;
                        }

                        if (code[i] == '"')
                        {
                            if (verbatim && i + 1 < code.Length && code[i + 1] == '"')
                            {
                                i++;
                                continue;
                            }

                            break;
                        }
                    }

                    continue;
                }

                if (c == '(' || c == '{' || c == '[')
                {
                    depth++;
                }
                else if ((c == ')' || c == '}' || c == ']') && depth > 0)
                {
                    depth--;
                }
                else if ((c == ',' || c == ')') && depth == 0)
                {
                    if (index == position)
                    {
                        var match = LiteralOnly.Match(code.Substring(from, i - from));
                        return match.Success ? match.Groups[1].Value : null;
                    }

                    if (c == ')')
                    {
                        return null;
                    }

                    index++;
                    from = i + 1;
                }
            }

            return null;
        }

        [Test, Category("AC-D15-27")]
        public void FindsTextArgumentOfAppControls()
        {
            // El texto de un botón como literal se encuentra; el nombre y la variante (también literales) no; una clave del catálogo
            // o un texto vacío tampoco; un literal con paréntesis o comas adentro no confunde la posición.
            var sample = "var a = new RmButton(\"save\", \"text\", null, \"Guardar\", Save);\n"
                + "var b = new RmButton(\"retry\", \"tonal\", Icons.Refresh, texts.Get(L10nKeys.Retry), (\n    ) => Retry());\n"
                + "var c = new RmButton(\"x\", \"text\", null, \"\", Go);\n"
                + "var d = new RmButton(Name(\"a, b\"), \"text (raro)\", Icons.Add, \"Agregar paso\", Add, 14, \"rm-type-body-xs\");\n"
                + "var e = new SwitchToggle(\"sim-hr\", \"Pulsómetro simulado\");\n"
                + "// var f = new RmButton(\"no\", \"text\", null, \"Comentario\", F);\n"
                + "var g = new IconButton(\"close\", Icons.Close, Close);";
            var found = new List<string>();
            foreach (var finding in Scan("sample.cs", sample))
            {
                found.Add(finding.Literal);
            }

            Assert.That(found, Is.EqualTo(new[] { "Guardar", "Agregar paso", "Pulsómetro simulado" }));
        }

        private static Dictionary<string, string> Allowlist()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var json = JsonParser.Parse(File.ReadAllText(RepoPaths.Combine(AllowlistPath)));
            foreach (var entry in json["entries"].Items)
            {
                result[entry["file"].AsString() + "\n" + entry["literal"].AsString()] = entry["reason"].AsString();
            }

            return result;
        }

        private static bool HasLetter(string text)
        {
            foreach (var c in text)
            {
                if (char.IsLetter(c))
                {
                    return true;
                }
            }

            return false;
        }

        public sealed class Finding
        {
            public Finding(string file, int line, string literal)
            {
                File = file;
                Line = line;
                Literal = literal;
            }

            public string File { get; }

            public int Line { get; }

            public string Literal { get; }
        }
    }
}
