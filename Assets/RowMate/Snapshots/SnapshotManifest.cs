using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Evidencia de cada toma (02-tech-design §4.5): manifest.json (lo lee SnapshotEvidenceTests en dotnet y
    // ShellTests.EditModeCaptureMatchesPlayModeTree en PlayMode) e INDEX.md (la misma información en una tabla).
    public sealed class SnapshotManifest
    {
        private readonly List<ShotRecord> _shots = new List<ShotRecord>();

        public IReadOnlyList<ShotRecord> Shots => _shots;

        public void Add(ShotRecord shot)
        {
            _shots.Add(shot);
        }

        // El conjunto del INDEX.md ("It. 2", "It. 3 (Rutinas)").
        public string Title { get; set; } = "It. 2";

        public void Write(string folder, string pipeline)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "manifest.json"), ToJson(pipeline), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(folder, "INDEX.md"), ToMarkdown(pipeline), new UTF8Encoding(false));
        }

        public string ToJson(string pipeline)
        {
            var json = new StringBuilder();
            json.Append("{\n  \"generator\": \"RowMate.Snapshots (02-tech-design §4)\",\n  \"pipeline\": ").Append(Q(pipeline))
                .Append(",\n  \"shots\": [\n");
            for (var i = 0; i < _shots.Count; i++)
            {
                _shots[i].AppendJson(json);
                json.Append(i < _shots.Count - 1 ? ",\n" : "\n");
            }

            json.Append("  ]\n}\n");
            return json.ToString();
        }

        public string ToMarkdown(string pipeline)
        {
            var md = new StringBuilder();
            md.Append("# Capturas de la ").Append(Title).Append("\n\n");
            if (Title == "It. 2")
            {
                md.Append("Generadas por el arnés (`tools/unity/capture-snapshots.ps1 -Set it2`, 02-tech-design §4) en modo edición, con la tubería ")
                    .Append(pipeline).Append(". Detalle de cada toma (sondas, auditoría, elementos) en [manifest.json](manifest.json); la medición de R-T1 en ")
                    .Append("[rt1-measurement.json](rt1-measurement.json).\n\n");
            }
            else
            {
                md.Append("Generadas por el arnés (`tools/unity/capture-snapshots.ps1`, 03-rutinas/02-tech-design §5) en modo edición, con la tubería ")
                    .Append(pipeline).Append(", bases de prueba (TempDatabase) y el reloj de prueba. Detalle de cada toma (sondas, auditoría, elementos) en ")
                    .Append("[manifest.json](manifest.json). CAP-27 (ventana del build normal) no es del arnés: `tools/unity/capture-cap27.ps1`, a pedido.\n\n");
            }
            md.Append("| ID | Archivo | Perfil | Backend | Estado | Sondas | Auditoría | Comprobaciones |\n|---|---|---|---|---|---|---|---|\n");
            foreach (var shot in _shots)
            {
                md.Append("| ").Append(shot.Id).Append(" | [").Append(shot.File).Append("](").Append(shot.File).Append(") | ")
                    .Append(shot.Profile).Append(" | ").Append(shot.Backend).Append(" | ").Append(Escape(shot.State)).Append(" | ")
                    .Append(Escape(ProbeCell(shot))).Append(" | ")
                    .Append(shot.Overflows.Count == 0 && shot.ClippedTexts.Count == 0 ? "sin desbordes" : Escape(string.Join("; ", shot.Overflows) + " " + string.Join("; ", shot.ClippedTexts)))
                    .Append(" | ").Append(shot.Superseded != null ? "reemplazada por " + shot.Superseded + " (" + Escape(shot.SupersededIds ?? "") + ")"
                        : shot.Problems.Count == 0 ? (shot.Error == null ? "ok" : "") : Escape(string.Join("; ", shot.Problems)))
                    .Append(shot.Error == null ? "" : " ERROR: " + Escape(shot.Error)).Append(" |\n");
            }

            return md.ToString();
        }

        public static string Q(string? value)
        {
            if (value == null)
            {
                return "null";
            }

            var sb = new StringBuilder("\"");
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.Append('"').ToString();
        }

        public static string N(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        // "pasan/total" y, si una sonda de aspecto falla (no hace fallar la toma), un aviso con su nombre y lo medido
        // (QA-V2-04: antes quedaba solo "1/2" y la toma decía "ok").
        public static string ProbeCell(ShotRecord shot)
        {
            var passed = 0;
            var warnings = new List<string>();
            foreach (var probe in shot.Probes)
            {
                if (probe.Pass)
                {
                    passed++;
                }
                else if (probe.Aspect)
                {
                    warnings.Add(probe.Name + " " + probe.Actual + " vs " + probe.Expected);
                }
            }

            var cell = passed.ToString(CultureInfo.InvariantCulture) + "/" + shot.Probes.Count.ToString(CultureInfo.InvariantCulture);
            return warnings.Count == 0 ? cell : cell + " ⚠ aspecto: " + string.Join("; ", warnings);
        }

        public static int FailingAspectProbes(IReadOnlyList<ShotRecord> shots)
        {
            var count = 0;
            foreach (var shot in shots)
            {
                foreach (var probe in shot.Probes)
                {
                    if (!probe.Pass && probe.Aspect)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static string Escape(string text)
        {
            return text.Replace("|", "\\|").Replace("\n", " ");
        }
    }

    public sealed class ShotRecord
    {
        public string Id = "";
        public string File = "";
        public string Profile = "";
        public int WidthPx;
        public int HeightPx;
        public double WidthDp;
        public double HeightDp;
        public double Density;
        public string Language = "";
        public double TextScale = 1;
        public bool ReduceMotion;
        public string Pipeline = "";
        public string Backend = "";
        public string State = "";
        public double TakenAtSeconds;
        public double DurationMs;
        public string? Error;
        public List<string> Overflows = new List<string>();
        public List<string> ClippedTexts = new List<string>();
        public List<string> Problems = new List<string>();
        public List<ProbeRecord> Probes = new List<ProbeRecord>();

        // It. 4b (§5.2): lo que corría en el instante de la toma (con su progreso) y, si la toma de it4a quedó reemplazada, por qué
        // conjunto. null en las tomas de antes (el manifest de it2 e it3 no cambia).
        public List<string>? InFlight;
        public string? Superseded;
        public string? SupersededIds;
        public List<string>? Values;
        public List<(string Name, Rect Bounds)> Elements = new List<(string, Rect)>();

        public bool Passed
        {
            get
            {
                if (Error != null || Overflows.Count > 0 || ClippedTexts.Count > 0 || Problems.Count > 0)
                {
                    return false;
                }

                foreach (var probe in Probes)
                {
                    if (!probe.Pass && !probe.Aspect)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // Nombres de contrato visibles con su worldBound en dp (condición de D-24 / R2-09).
        public void CaptureElements(VisualElement root)
        {
            var stack = new Stack<VisualElement>();
            stack.Push(root);
            var window = root.worldBound;
            while (stack.Count > 0)
            {
                var element = stack.Pop();
                if (element.resolvedStyle.display == DisplayStyle.None)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(element.name) && !element.name.StartsWith("unity-", StringComparison.Ordinal) && UiQuery.IsInside(element, window))
                {
                    Elements.Add((element.name, element.worldBound));
                }

                for (var i = element.hierarchy.childCount - 1; i >= 0; i--)
                {
                    stack.Push(element.hierarchy[i]);
                }
            }
        }

        public void AppendJson(StringBuilder json)
        {
            json.Append("    {\n");
            json.Append("      \"id\": ").Append(SnapshotManifest.Q(Id)).Append(",\n");
            json.Append("      \"file\": ").Append(SnapshotManifest.Q(File)).Append(",\n");
            json.Append("      \"profile\": ").Append(SnapshotManifest.Q(Profile)).Append(",\n");
            json.Append("      \"px\": [").Append(WidthPx).Append(", ").Append(HeightPx).Append("],\n");
            json.Append("      \"dp\": [").Append(SnapshotManifest.N(WidthDp)).Append(", ").Append(SnapshotManifest.N(HeightDp)).Append("],\n");
            json.Append("      \"density\": ").Append(Density.ToString("0.###", CultureInfo.InvariantCulture)).Append(",\n");
            json.Append("      \"language\": ").Append(SnapshotManifest.Q(Language)).Append(",\n");
            json.Append("      \"textScale\": ").Append(SnapshotManifest.N(TextScale)).Append(",\n");
            json.Append("      \"reduceMotion\": ").Append(ReduceMotion ? "true" : "false").Append(",\n");
            json.Append("      \"pipeline\": ").Append(SnapshotManifest.Q(Pipeline)).Append(",\n");
            json.Append("      \"backend\": ").Append(SnapshotManifest.Q(Backend)).Append(",\n");
            json.Append("      \"state\": ").Append(SnapshotManifest.Q(State)).Append(",\n");
            json.Append("      \"takenAtSeconds\": ").Append(SnapshotManifest.N(TakenAtSeconds)).Append(",\n");
            json.Append("      \"durationMs\": ").Append(SnapshotManifest.N(DurationMs)).Append(",\n");
            json.Append("      \"error\": ").Append(SnapshotManifest.Q(Error)).Append(",\n");
            json.Append("      \"passed\": ").Append(Passed ? "true" : "false").Append(",\n");
            json.Append("      \"audit\": { \"overflows\": ").Append(List(Overflows)).Append(", \"clippedTexts\": ").Append(List(ClippedTexts)).Append(" },\n");
            json.Append("      \"checks\": ").Append(List(Problems)).Append(",\n");
            if (Superseded != null)
            {
                json.Append("      \"superseded\": ").Append(SnapshotManifest.Q(Superseded)).Append(",\n");
                json.Append("      \"supersededBy\": ").Append(SnapshotManifest.Q(SupersededIds)).Append(",\n");
            }

            if (InFlight != null)
            {
                json.Append("      \"inFlight\": ").Append(List(InFlight)).Append(",\n");
            }

            if (Values != null)
            {
                json.Append("      \"values\": ").Append(List(Values)).Append(",\n");
            }
            json.Append("      \"probes\": [");
            for (var i = 0; i < Probes.Count; i++)
            {
                var p = Probes[i];
                json.Append(i == 0 ? "\n" : ",\n").Append("        { \"name\": ").Append(SnapshotManifest.Q(p.Name)).Append(", \"element\": ")
                    .Append(SnapshotManifest.Q(p.Element)).Append(", \"expected\": ").Append(SnapshotManifest.Q(p.Expected)).Append(", \"actual\": ")
                    .Append(SnapshotManifest.Q(p.Actual)).Append(", \"difference\": ").Append(p.Difference).Append(", \"tolerance\": ").Append(p.Tolerance)
                    .Append(", \"aspect\": ").Append(p.Aspect ? "true" : "false").Append(", \"pass\": ").Append(p.Pass ? "true" : "false").Append(" }");
            }

            json.Append(Probes.Count > 0 ? "\n      ],\n" : "],\n");
            json.Append("      \"elements\": [");
            for (var i = 0; i < Elements.Count; i++)
            {
                var e = Elements[i];
                json.Append(i == 0 ? "\n" : ",\n").Append("        { \"name\": ").Append(SnapshotManifest.Q(e.Name)).Append(", \"x\": ").Append(SnapshotManifest.N(e.Bounds.x))
                    .Append(", \"y\": ").Append(SnapshotManifest.N(e.Bounds.y)).Append(", \"w\": ").Append(SnapshotManifest.N(e.Bounds.width)).Append(", \"h\": ")
                    .Append(SnapshotManifest.N(e.Bounds.height)).Append(" }");
            }

            json.Append(Elements.Count > 0 ? "\n      ]\n" : "]\n");
            json.Append("    }");
        }

        private static string List(List<string> items)
        {
            var sb = new StringBuilder("[");
            for (var i = 0; i < items.Count; i++)
            {
                sb.Append(i == 0 ? "" : ", ").Append(SnapshotManifest.Q(items[i]));
            }

            return sb.Append(']').ToString();
        }
    }

    public sealed class ProbeRecord
    {
        public string Name = "";
        public string Element = "";
        public string Expected = "";
        public string Actual = "";
        public int Difference;
        public int Tolerance;
        public bool Aspect;
        public bool Pass;
    }
}
