using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RowMate.Tests.EditMode.Localization
{
    // MN · Un nombre por métrica (It. 15, #75, AC-D15-22, DV-140; gate QA-G15-04, DT15-21): en los rótulos de métrica de las familias
    // metric* (Dispositivo), sessionChart* (selector del Detalle), historyStat* (tarjeta y resumen) y hudWatts/hudSplit/hudSpm, cada
    // métrica se llama igual sin distinguir mayúsculas. Excepciones cerradas: historyStatSplit ("Split", columnas angostas), hudBpm
    // (el pulso del HUD como unidad), las calorías (kcal es unidad) y editor* (fuera de las familias). Una clave nueva de esas familias
    // tiene que entrar en la tabla o en las excepciones (así la prueba no queda vacía).
    [TestFixture]
    public sealed class MetricNamesTests
    {
        // métrica → (es, en) y las claves que la nombran.
        private static readonly (string Metric, string Es, string En, string[] Keys)[] Names =
        {
            ("power", "Vatios", "Watts", new[] { "metricWatts", "historyStatWatts", "historyStatPower", "sessionChartWatts", "hudWatts" }),
            ("split", "Split 500m", "Split 500m", new[] { "metricSplit", "sessionChartSplit", "hudSplit" }),
            ("spm", "SPM", "SPM", new[] { "metricSpm", "historyStatSpm", "sessionChartSpm", "hudSpm" }),
            ("heartRate", "Pulso", "HR", new[] { "metricHeartRate", "historyStatHeartRate", "sessionChartHr" }),
            ("distance", "Distancia", "Distance", new[] { "metricDistance", "historyStatDistance", "sessionChartDistance" }),
            ("time", "Tiempo", "Time", new[] { "metricTime", "historyStatTime" }),
        };

        // Las excepciones cerradas de la spec (AC-D15-22) y las claves de esas familias que no son rótulo de métrica.
        private static readonly HashSet<string> Exceptions = new HashSet<string>(StringComparer.Ordinal)
        {
            "historyStatSplit", "hudBpm", "metricCalories", "historyStatCalories", "hudKcal",
        };

        private static readonly HashSet<string> NotMetricLabels = new HashSet<string>(StringComparer.Ordinal) { "metricFreeLabel" };

        [TestCase("es")]
        [TestCase("en")]
        [Category("AC-D15-22")]
        public void OneNamePerMetric(string language)
        {
            var catalog = LocalizationFixture.Catalog(language);
            var problems = new List<string>();
            foreach (var name in Names)
            {
                var expected = language == "es" ? name.Es : name.En;
                foreach (var key in name.Keys)
                {
                    if (!catalog.Contains(key))
                    {
                        problems.Add(key + ": no está en el catálogo");
                        continue;
                    }

                    var text = catalog.Message(key);
                    if (!string.Equals(text, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        problems.Add(language + "/" + key + " (" + name.Metric + "): \"" + text + "\", se esperaba \"" + expected + "\"");
                    }
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(catalog.Message("historyStatSplit"), Is.EqualTo("Split"), "la única abreviatura (legacy history_screen.dart:115)");
            Assert.That(catalog.Message("hudBpm"), Is.EqualTo("BPM"));
            Assert.That(catalog.Message("metricWatts"), Is.EqualTo(language == "es" ? "VATIOS" : "WATTS"), "Dispositivo en mayúsculas, sin cambios");
        }

        [Test, Category("AC-D15-22")]
        public void NoUnlistedMetricKey()
        {
            var listed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in Names)
            {
                foreach (var key in name.Keys)
                {
                    Assert.That(listed.Add(key), Is.True, "clave repetida en la tabla: " + key);
                }
            }

            var catalog = LocalizationFixture.Catalog("es");
            var unlisted = new List<string>();
            var inFamilies = 0;
            foreach (var key in catalog.Keys)
            {
                if (!InFamilies(key))
                {
                    continue;
                }

                inFamilies++;
                if (listed.Contains(key) || Exceptions.Contains(key) || NotMetricLabels.Contains(key) || catalog.Message(key).IndexOf('{') >= 0)
                {
                    continue;
                }

                unlisted.Add(key);
            }

            Assert.That(unlisted, Is.Empty, "claves de las familias de métricas sin tabla ni excepción: " + string.Join(", ", unlisted));
            Assert.That(inFamilies, Is.GreaterThanOrEqualTo(listed.Count + Exceptions.Count), "la prueba ve todas las claves de la tabla");
            foreach (var key in listed)
            {
                Assert.That(InFamilies(key), Is.True, key + " es de una familia");
            }
        }

        private static bool InFamilies(string key)
        {
            return key.StartsWith("metric", StringComparison.Ordinal) || key.StartsWith("sessionChart", StringComparison.Ordinal)
                || key.StartsWith("historyStat", StringComparison.Ordinal) || key == "hudWatts" || key == "hudSplit" || key == "hudSpm"
                || key == "hudBpm" || key == "hudKcal";
        }
    }
}
