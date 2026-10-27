using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Design;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // Lee la evidencia que deja el arnés de capturas (02-tech-design §4.5) y verifica las sondas de color de la spec: colores
    // base (AC-APP-05-02), textos terciarios (AC-APP-05-06), color de cada métrica sobre el trazo del valor (AC-APP-06-01) y la
    // medición del vidrio (AC-BASE-05). Sin capturas (antes de WP-B, o en un clon sin correr capture-snapshots.ps1) se ignora.
    //
    // Contrato con el arnés (SnapshotManifest, WP-B), en docs/migration/iterations/02-dispositivo-simulador/snapshots/:
    //   manifest.json        { "shots": [ { "id": "CAP-01", "file": "CAP-01-dispositivo-tv.png",
    //                          "probes": [ { "name": "bg.app", "expected": "#0D1B2A", "actual": "#0D1B2A", "tolerance": 1,
    //                                        "pass": true }, … ] }, … ] }
    //                        "id" es el de la spec con su toma ("CAP-04a"); "name" de cada sonda: el token que mide
    //                        ("bg.app", "surface.card", "appbar", "metric.split" … "metric.hr", "text.tertiary:<elemento>").
    //   rt1-measurement.json { "app": { "glassOnWhite": "#2A2F37", "textSecondaryOnCard": "#BAC0C7" },
    //                          "direct": { "glassOnWhite": "#6A6A6C", "textSecondaryOnCard": "#DADBDC" } }
    [TestFixture]
    public sealed class SnapshotEvidenceTests
    {
        private const string SnapshotsFolder = "docs/migration/iterations/02-dispositivo-simulador/snapshots";

        [Test, Category("AC-APP-05-02")]
        public void BaseColors()
        {
            // Fondo #0D1B2A, tarjeta #1A2E45 y barra superior #0D1B2A en CAP-01 (±1 por canal).
            var probes = ProbesOf(Shot(Manifest(), "CAP-01"));
            Check(probes, "bg.app", Tokens.BgApp.Rgb.ToHex(), 1);
            Check(probes, "surface.card", Tokens.SurfaceCard.Rgb.ToHex(), 1);
            Check(probes, "appbar", Tokens.BgApp.Rgb.ToHex(), 1);
        }

        [Test, Category("AC-APP-05-06")]
        public void TertiaryTexts()
        {
            // La pista, la ayuda y el subtítulo de la fila en text.tertiary (blanco al 54 %), no al 38 % del legacy.
            var manifest = Manifest();
            foreach (var id in new[] { "CAP-08", "CAP-10" })
            {
                var found = 0;
                foreach (var probe in ProbesOf(Shot(manifest, id)))
                {
                    if (probe.Name.StartsWith("text.tertiary", StringComparison.Ordinal))
                    {
                        found++;
                        AssertProbe(id, probe, probe.Tolerance);
                    }
                }

                Assert.That(found, Is.GreaterThan(0), id + " sin sondas text.tertiary");
            }
        }

        [Test, Category("AC-APP-06-01")]
        public void MetricColorsOnValueStrokes()
        {
            // El trazo de cada valor en el color de su métrica, ±2 por canal (CAP-01; PULSO en CAP-06).
            var manifest = Manifest();
            var probes = ProbesOf(Shot(manifest, "CAP-01"));
            Check(probes, "metric.split", Tokens.MetricSplit.Rgb.ToHex(), 2);
            Check(probes, "metric.spm", Tokens.MetricSpm.Rgb.ToHex(), 2);
            Check(probes, "metric.watts", Tokens.MetricWatts.Rgb.ToHex(), 2);
            Check(probes, "metric.distance", Tokens.MetricDistance.Rgb.ToHex(), 2);
            Check(ProbesOf(Shot(manifest, "CAP-06")), "metric.hr", Tokens.MetricHr.Rgb.ToHex(), 2);
        }

        [Test, Category("AC-BASE-05")]
        public void Rt1MeasurementPresent()
        {
            // Con la composición de la app (D-01): glass.fallback sobre blanco #2A2F37 y text.secondary sobre surface.card
            // #BAC0C7, ±1 por canal (01-po-spec AC-BASE-05). La directa queda solo como referencia.
            var path = RepoPaths.Combine(SnapshotsFolder, "rt1-measurement.json");
            if (!File.Exists(path))
            {
                Assert.Ignore("sin " + RepoPaths.Relative(path) + ": la produce capture-snapshots.ps1 (WP-B, CAP-23)");
            }

            var root = JsonParser.Parse(File.ReadAllText(path));
            var app = root["app"];
            AssertClose("app.glassOnWhite", app["glassOnWhite"].AsString(), "#2A2F37", 1);
            AssertClose("app.textSecondaryOnCard", app["textSecondaryOnCard"].AsString(), "#BAC0C7", 1);
            Assert.That(root.TryGet("direct", out _), Is.True, "la composición directa, como referencia");
        }

        [Test]
        public void ProbeComparisonIsPerChannel()
        {
            Assert.That(Distance("#0D1B2A", "#0E1A2B"), Is.EqualTo(1));
            Assert.That(Distance("#FF9F1C", "#FD9F1E"), Is.EqualTo(2));
            Assert.That(Distance("#000000", "#FFFFFF"), Is.EqualTo(255));
        }

        private static JsonNode Manifest()
        {
            var path = RepoPaths.Combine(SnapshotsFolder, "manifest.json");
            if (!File.Exists(path))
            {
                Assert.Ignore("sin capturas todavía: " + RepoPaths.Relative(path) + " lo escribe capture-snapshots.ps1 (WP-B)");
            }

            return JsonParser.Parse(File.ReadAllText(path));
        }

        private static JsonNode Shot(JsonNode manifest, string id)
        {
            foreach (var shot in manifest["shots"].Items)
            {
                if (shot["id"].AsString() == id)
                {
                    return shot;
                }
            }

            Assert.Ignore("el manifest no tiene " + id + " (corrida parcial del arnés)");
            throw new InvalidOperationException("inalcanzable");
        }

        private static List<Probe> ProbesOf(JsonNode shot)
        {
            var probes = new List<Probe>();
            foreach (var item in shot["probes"].Items)
            {
                probes.Add(new Probe(item["name"].AsString(), item["expected"].AsString(), item["actual"].AsString(),
                    (int)item["tolerance"].AsLong(), item["pass"].AsBool()));
            }

            return probes;
        }

        private static void Check(List<Probe> probes, string name, string tokenHex, int tolerance)
        {
            foreach (var probe in probes)
            {
                if (probe.Name == name)
                {
                    Assert.That(probe.Expected.ToUpperInvariant(), Is.EqualTo(tokenHex.ToUpperInvariant()), name + ": el esperado es el token");
                    AssertProbe(name, probe, tolerance);
                    return;
                }
            }

            Assert.Fail("falta la sonda " + name);
        }

        private static void AssertProbe(string where, Probe probe, int tolerance)
        {
            AssertClose(where + " " + probe.Name, probe.Actual, probe.Expected, tolerance);
            Assert.That(probe.Pass, Is.True, where + " " + probe.Name + ": el arnés la dio por fallida");
        }

        private static void AssertClose(string what, string actual, string expected, int tolerance)
        {
            Assert.That(Distance(actual, expected), Is.LessThanOrEqualTo(tolerance), what + ": " + actual + " vs " + expected);
        }

        // Diferencia máxima por canal entre dos #RRGGBB.
        private static int Distance(string a, string b)
        {
            var x = Rgb.Parse(a);
            var y = Rgb.Parse(b);
            return Math.Max(Math.Abs(x.R - y.R), Math.Max(Math.Abs(x.G - y.G), Math.Abs(x.B - y.B)));
        }

        private sealed class Probe
        {
            public Probe(string name, string expected, string actual, int tolerance, bool pass)
            {
                Name = name;
                Expected = expected;
                Actual = actual;
                Tolerance = tolerance;
                Pass = pass;
            }

            public string Name { get; }

            public string Expected { get; }

            public string Actual { get; }

            public int Tolerance { get; }

            public bool Pass { get; }
        }
    }
}
