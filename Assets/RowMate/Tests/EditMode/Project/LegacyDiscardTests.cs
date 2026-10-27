using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // AC-APP-08-01 (Q-57, F-RUT-19): MetricCard e IntervalTile del legacy (lib/shared/widgets/metric_card.dart,
    // interval_tile.dart) no tienen uso y no se migran. Acá se verifica la parte de la app nueva: ningún tipo, archivo ni clase
    // de USS los porta. Que el legacy no los usa lo confirma QA (búsqueda en lib/ y test/, 01-po-spec §4.5).
    // Una sola tarjeta de métrica: la del tablero (.rm-metric-tile) y la del pulsómetro comparten .rm-card (AC-APP-08-02, PlayMode).
    [TestFixture]
    public sealed class LegacyDiscardTests
    {
        private static readonly Regex Identifier = new Regex(@"\b(MetricCard|IntervalTile)\b");
        private static readonly Regex FileName = new Regex(@"(?i)(metric_?card|interval_?tile)");
        private static readonly Regex UssClass = new Regex(@"(?i)\.rm-(metric-card|interval-tile)\b");

        [Test, Category("AC-APP-08-01")]
        public void NoMetricCardNorIntervalTilePort()
        {
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles(""))
            {
                var relative = RepoPaths.Relative(file);
                if (FileName.IsMatch(Path.GetFileName(file)))
                {
                    offenders.Add(relative + ": nombre de archivo");
                }

                // Sin comentarios ni literales: las menciones en texto (como las de este test) no cuentan.
                offenders.AddRange(SourceScan.Matches(file, new[] { Identifier }, true));
            }

            foreach (var pattern in new[] { "*.uss", "*.tss", "*.uxml" })
            {
                foreach (var file in RepoPaths.Files("Assets", pattern))
                {
                    if (FileName.IsMatch(Path.GetFileName(file)))
                    {
                        offenders.Add(RepoPaths.Relative(file) + ": nombre de archivo");
                    }

                    var text = SourceScan.StripComments(File.ReadAllText(file));
                    foreach (Match match in UssClass.Matches(text))
                    {
                        offenders.Add(RepoPaths.Relative(file) + ":" + SourceScan.LineOf(text, match.Index) + ": " + match.Value);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, "port de MetricCard / IntervalTile (descartados, Q-57):\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-APP-08-01")]
        public void TheCheckRecognizesAPort()
        {
            Assert.That(Identifier.IsMatch("public sealed class MetricCard : VisualElement"), Is.True);
            Assert.That(Identifier.IsMatch("var tile = new IntervalTile();"), Is.True);
            Assert.That(Identifier.IsMatch("public sealed record MetricTileSpec"), Is.False, "la tarjeta del tablero sí existe");
            Assert.That(FileName.IsMatch("metric_card.dart"), Is.True);
            Assert.That(FileName.IsMatch("IntervalTile.cs"), Is.True);
            Assert.That(FileName.IsMatch("MetricTileView.cs"), Is.False);
            Assert.That(UssClass.IsMatch(".rm-metric-card { }"), Is.True);
            Assert.That(UssClass.IsMatch(".rm-metric-tile { }"), Is.False);
            Assert.That(SourceScan.StripCommentsAndLiterals("var s = \"MetricCard\"; // IntervalTile").Contains("MetricCard"), Is.False);
            Assert.That(String.IsNullOrEmpty(SourceScan.StripComments("x")), Is.False);
        }
    }
}
