using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using RowMate.Core.Common.Json;
using RowMate.Presentation.Surface;
using RowMate.Tests.UiSupport;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace RowMate.Snapshots
{
    // Máquina de estados del arnés (02-tech-design §4.1): corre en EditorApplication.update, en modo edición (sin Play). Un
    // escenario a la vez: arma el UiTestHost, corre su Arrange (yield return null = un update), aplica la regla de toma de la
    // foto fija (DN-21) o graba el video cuadro a cuadro, lee la RT, recorta, guarda el PNG, audita el layout, mide las sondas
    // y anota todo en manifest.json e INDEX.md. Un escenario que falla queda con su error y la corrida sigue.
    // It. 3 (03-rutinas/02-tech-design §5.1): secuencias (una armada, varias tomas: DEMO), el título del INDEX por conjunto y la
    // regla de toma con "ninguna transición de capa en curso" (AppShellView.IsSettled).
    // It. 4a (QA-V3-06): antes de la primera toma precarga los atlas de las fuentes (FontAtlasWarmup), así los bytes no dependen
    // de qué se dibujó antes: dos corridas, la de un conjunto después del otro o una con -Filter dan lo mismo. En cada foto fija
    // corren además WrenchAudit (con simulador) y HudOverlap (en el inmersivo): DD4a-04, 02-tech-design §7.1 de la 4a.
    // Códigos de salida: 0 todo bien; 1 alguna toma falló (error, sonda que no es de aspecto, auditoría o comprobación);
    // 3 venció el tope de 15 min.
    public sealed class SnapshotRunner
    {
        public const double TimeLimitSeconds = 15 * 60;
        private const float CropMarginDp = 8;

        private readonly List<SnapshotScenario> _scenarios;
        private readonly string _output;
        private readonly UiColorPipeline _pipeline;
        private readonly bool _partial;
        private readonly Action<int> _exit;
        private readonly SnapshotManifest _manifest = new SnapshotManifest();
        private readonly Stack<IEnumerator> _stack = new Stack<IEnumerator>();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private int _index = -1;
        private UiTestHost? _host;
        private ShotRecord? _record;
        private Stopwatch _shotClock = new Stopwatch();
        private int _transitions;
        private bool _finished;
        private Rt1Measurement? _rt1;

        public SnapshotRunner(IReadOnlyList<SnapshotScenario> scenarios, string outputFolder, UiColorPipeline pipeline, bool partial, Action<int> exit,
                              string title = "It. 2")
        {
            _scenarios = new List<SnapshotScenario>(scenarios);
            _output = outputFolder;
            _pipeline = pipeline;
            _partial = partial;
            _exit = exit;
            _manifest.Title = title;
        }

        public void Start()
        {
            Directory.CreateDirectory(_output);
            Debug.Log("[SNAPSHOT] " + _scenarios.Count + " tomas → " + _output + " (" + _pipeline + ")");
            // Antes de la primera toma: los atlas de las fuentes en un estado fijo (QA-V3-06; FontAtlasWarmup).
            Debug.Log(FontAtlasWarmup.Run());
            EditorApplication.update += Update;
        }

        private void Update()
        {
            if (_finished)
            {
                return;
            }

            if (_clock.Elapsed.TotalSeconds > TimeLimitSeconds)
            {
                Fail("venció el tope de 15 min");
                Finish(3);
                return;
            }

            try
            {
                if (_stack.Count == 0)
                {
                    EndScenario();
                    if (!BeginNext())
                    {
                        Finish(WriteOutputs());
                        return;
                    }
                }

                Step();
            }
            catch (Exception error)
            {
                Fail(error.GetType().Name + ": " + error.Message);
                Debug.LogException(error);
                _stack.Clear();
            }
        }

        // Un update: avanza el enumerador de arriba hasta el próximo yield null (los anidados se apilan en el acto).
        private void Step()
        {
            var guard = 0;
            while (_stack.Count > 0 && guard++ < 10000)
            {
                var top = _stack.Peek();
                if (!top.MoveNext())
                {
                    _stack.Pop();
                    continue;
                }

                if (top.Current is IEnumerator nested)
                {
                    _stack.Push(nested);
                    continue;
                }

                return;
            }
        }

        private bool BeginNext()
        {
            _index++;
            if (_index >= _scenarios.Count)
            {
                return false;
            }

            var scenario = _scenarios[_index];
            _shotClock = Stopwatch.StartNew();
            _record = NewRecord(scenario);
            Debug.Log("[SNAPSHOT] " + scenario.Id + " (" + scenario.FileName + ")");
            if (scenario.Kind == SnapshotKind.Rt1)
            {
                _stack.Push(Rt1Routine(scenario));
                return true;
            }

            _host = UiTestHost.Create(scenario.Host);
            _transitions = 0;
            _host.Root.RegisterCallback<TransitionRunEvent>(_ => _transitions++, TrickleDown.TrickleDown);
            _host.Root.RegisterCallback<TransitionEndEvent>(_ => _transitions--, TrickleDown.TrickleDown);
            _host.Root.RegisterCallback<TransitionCancelEvent>(_ => _transitions--, TrickleDown.TrickleDown);
            if (scenario.Kind == SnapshotKind.Sequence)
            {
                // La secuencia no tiene registro propio: cada Shot lleva el suyo.
                _record = null;
                _sequence = scenario;
                _sequenceTaken.Clear();
                _stack.Push(SequenceRoutine(scenario, _host));
                return true;
            }

            _stack.Push(scenario.Kind == SnapshotKind.Video ? VideoRoutine(scenario, _host) : StillRoutine(scenario, _host));
            return true;
        }

        private SnapshotScenario? _sequence;
        private readonly HashSet<string> _sequenceTaken = new HashSet<string>(StringComparer.Ordinal);

        private IEnumerator SequenceRoutine(SnapshotScenario scenario, UiTestHost host)
        {
            var script = new ScenarioScript(host) { OnShot = (id, checks) => ShotRoutine(scenario, host, id, checks) };
            yield return script.Frames(3);
            if (scenario.Arrange != null)
            {
                yield return scenario.Arrange(script);
            }
        }

        // Una toma de la secuencia: registro propio, regla de toma, captura y al manifest.
        private IEnumerator ShotRoutine(SnapshotScenario scenario, UiTestHost host, string id, Func<UiTestHost, IEnumerable<string>>? checks)
        {
            var spec = FindShot(scenario, id);
            var record = NewRecord(scenario);
            record.Id = id;
            record.File = spec.Stem + ".png";
            record.State = spec.State;
            _record = record;
            var clock = Stopwatch.StartNew();
            Debug.Log("[SNAPSHOT] " + id + " (" + record.File + ")");
            yield return TakeRule(scenario, host);
            yield return new ScenarioScript(host).Frames(3);
            Capture(scenario, host, record.File, null, new List<PixelProbe>(), checks);
            record.DurationMs = clock.Elapsed.TotalMilliseconds;
            _manifest.Add(record);
            _sequenceTaken.Add(id);
            Debug.Log("[SNAPSHOT] " + id + ": " + (record.Passed ? "ok" : "FALLA " + Summary(record)));
            _record = null;
        }

        private static (string Id, string Stem, string State) FindShot(SnapshotScenario scenario, string id)
        {
            foreach (var shot in scenario.SequenceShots)
            {
                if (shot.Id == id)
                {
                    return shot;
                }
            }

            throw new InvalidOperationException("la secuencia " + scenario.Id + " no declara la toma " + id);
        }

        // Lo que una secuencia cortada no llegó a tomar queda en el manifest como falla (nunca falta en silencio).
        private void CloseSequence(string? error)
        {
            var scenario = _sequence;
            _sequence = null;
            if (scenario == null)
            {
                return;
            }

            foreach (var shot in scenario.SequenceShots)
            {
                if (_sequenceTaken.Contains(shot.Id))
                {
                    continue;
                }

                var record = NewRecord(scenario);
                record.Id = shot.Id;
                record.File = shot.Stem + ".png";
                record.State = shot.State;
                record.Error = "la secuencia " + scenario.Id + " no llegó a esta toma" + (error != null ? " (" + error + ")" : "");
                _manifest.Add(record);
            }
        }

        private void EndScenario()
        {
            if (_sequence != null)
            {
                if (_record != null)
                {
                    // Una toma que falló a mitad: su registro ya tiene el error.
                    _record.DurationMs = _shotClock.Elapsed.TotalMilliseconds;
                    _manifest.Add(_record);
                    _sequenceTaken.Add(_record.Id);
                    _record = null;
                }

                CloseSequence(null);
            }

            if (_record != null)
            {
                _record.DurationMs = _shotClock.Elapsed.TotalMilliseconds;
                _manifest.Add(_record);
                Debug.Log("[SNAPSHOT] " + _record.Id + ": " + (_record.Passed ? "ok" : "FALLA " + Summary(_record)));
                _record = null;
            }

            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }
        }

        private IEnumerator StillRoutine(SnapshotScenario scenario, UiTestHost host)
        {
            var script = new ScenarioScript(host);
            yield return script.Frames(3);
            if (scenario.Arrange != null)
            {
                yield return scenario.Arrange(script);
            }

            yield return TakeRule(scenario, host);
            yield return script.Frames(3);
            Capture(scenario, host, scenario.FileName, scenario.CropElement, scenario.Probes, scenario.Checks);
        }

        // Regla de toma de una foto fija (DN-21; 04-design-notes §7.1): sin animaciones finitas, corazón en reposo y sin
        // transiciones de USS, avanzando de a 1/60 s sin cruzar la lectura siguiente: con una lectura en N s, la toma cae en
        // [N + 0,55; N + 1). Si no se cumple, se prueba UNA vez la ventana de la lectura siguiente; si tampoco, la toma falla (no se
        // toma "lo que haya").
        private IEnumerator TakeRule(SnapshotScenario scenario, UiTestHost host)
        {
            var now = host.Scheduler.MonotonicSeconds;
            double windowEnd;
            if (scenario.SkipReadingWindow)
            {
                windowEnd = now + 1;
            }
            else
            {
                var n = Math.Floor(now);
                if (now - n > 0.85)
                {
                    n += 1;
                }

                var start = n + 0.55;
                if (now < start)
                {
                    host.Advance(start - now);
                    yield return null;
                    yield return null;
                }

                windowEnd = n + 1;
            }

            var retried = false;
            while (true)
            {
                if (!host.HasFiniteMotions && host.IsHeartAtRest() && _transitions <= 0 && host.Shell.IsSettled)
                {
                    break;
                }

                if (host.Scheduler.MonotonicSeconds + UiTestHost.FrameSeconds >= windowEnd)
                {
                    // Una sola vez: la ventana de la lectura siguiente ([N + 1,55; N + 2)). Nunca se toma "lo que haya".
                    if (!scenario.SkipReadingWindow && !retried)
                    {
                        retried = true;
                        Debug.Log("[SNAPSHOT] " + (_record?.Id ?? scenario.Id) + ": la ventana de la lectura no alcanzó (animaciones finitas: "
                                  + host.Handle.Motion.DescribeFinite() + "; corazón en reposo: " + host.IsHeartAtRest() + "; transiciones USS: " + _transitions
                                  + "; capas quietas: " + host.Shell.IsSettled + PulseState(host) + "); se prueba la siguiente");
                        var next = windowEnd + 0.55;
                        host.Advance(next - host.Scheduler.MonotonicSeconds);
                        windowEnd += 1;
                        yield return null;
                        yield return null;
                        continue;
                    }

                    throw new InvalidOperationException("regla de toma: no quedó quieta antes de " + windowEnd.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                        + " s (animaciones finitas: " + host.Handle.Motion.DescribeFinite() + "; corazón en reposo: " + host.IsHeartAtRest() + ")");
                }

                host.Step();
                yield return null;
            }

            _record!.TakenAtSeconds = host.Scheduler.MonotonicSeconds;
        }

        // El estado del pulso de la fila del paso (para el registro de la regla de toma).
        private static string PulseState(UiTestHost host)
        {
            var pulse = host.Immersive?.View.Steps?.Pulse;
            if (pulse == null)
            {
                return "";
            }

            return "; pulso: finito " + pulse.IsFinite + ", rampa " + pulse.Ramp.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ", escala "
                   + pulse.Scale.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + ", ahora " + host.Scheduler.MonotonicSeconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void Capture(SnapshotScenario scenario, UiTestHost host, string fileName, string? cropElement, List<PixelProbe> probes,
                             Func<UiTestHost, IEnumerable<string>>? checks)
        {
            var record = _record!;
            var frame = host.ReadPixels();
            try
            {
                Texture2D image = frame;
                if (cropElement != null)
                {
                    var element = host.Find(cropElement) ?? throw new InvalidOperationException("recorte: no existe #" + cropElement);
                    var crop = RtReader.ToPixels(element.worldBound, host.Profile.Density, CropMarginDp);
                    image = RtReader.Read(host.Target, crop);
                }

                File.WriteAllBytes(Path.Combine(_output, fileName), image.EncodeToPNG());
                if (!ReferenceEquals(image, frame))
                {
                    Object.DestroyImmediate(image);
                }

                foreach (var probe in probes)
                {
                    var result = probe.Measure(host.Root, frame, host.Profile.Density);
                    record.Probes.Add(new ProbeRecord
                    {
                        Name = probe.Name,
                        Element = probe.Element,
                        Expected = probe.ExpectedHex,
                        Actual = string.IsNullOrEmpty(result.Actual) ? new RowMate.Core.Design.Rgb(0, 0, 0).ToHex() : result.Actual,
                        Difference = result.Difference,
                        Tolerance = probe.Tolerance,
                        Aspect = scenario.AspectProbes.Contains(probe.Name),
                        Pass = result.Pass
                    });
                }
            }
            finally
            {
                Object.DestroyImmediate(frame);
            }

            var audit = host.Audit();
            record.Overflows.AddRange(audit.Overflows);
            record.ClippedTexts.AddRange(audit.ClippedTexts);
            if (checks != null)
            {
                record.Problems.AddRange(checks(host));
            }

            // It. 4a (DD4a-04; 04a-motor-hud-funcional/02-tech-design §7.1): la auditoría de la llave en toda toma con simulador y
            // HudOverlap en toda toma del inmersivo. Lo que encuentran hace fallar la toma (no son sondas de aspecto).
            record.Problems.AddRange(WrenchAudit.Run(host));
            record.Problems.AddRange(WrenchAudit.HudOverlap(host));

            if (host.HasErrors())
            {
                record.Problems.Add("el registro tiene errores: " + string.Join(" | ", host.Logger.Lines));
            }

            record.CaptureElements(host.Root);
        }

        private IEnumerator VideoRoutine(SnapshotScenario scenario, UiTestHost host)
        {
            var script = new ScenarioScript(host);
            yield return script.Frames(3);
            if (scenario.Arrange != null)
            {
                yield return scenario.Arrange(script);
            }

            var spec = scenario.Video!;
            var frames = (int)Math.Round(spec.Seconds * spec.Fps);
            var format = scenario.Host.Pipeline == UiColorPipeline.GammaRt ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8B8A8_SRGB;
            var path = Path.Combine(_output, scenario.FileName);
            var actions = new List<(double At, Action<UiTestHost> Action)>(spec.Actions);
            actions.Sort((a, b) => a.At.CompareTo(b.At));
            var next = 0;
            using (var recorder = new VideoRecorder(path, host.Target.width, host.Target.height, spec.Fps, format))
            {
                yield return script.Frames(2);
                for (var i = 0; i < frames; i++)
                {
                    var t = i / (double)spec.Fps;
                    while (next < actions.Count && actions[next].At <= t + 1e-9)
                    {
                        actions[next].Action(host);
                        next++;
                    }

                    host.Advance(1.0 / spec.Fps);
                    yield return null;
                    yield return null;
                    recorder.AddFrame(host.Target);
                }

                _record!.TakenAtSeconds = host.Scheduler.MonotonicSeconds;
                if (recorder.FallbackFolder != null)
                {
                    _record.Problems.Add("MediaEncoder no disponible: cuadros en " + recorder.FallbackFolder);
                }

                _record.WidthPx = recorder.Width;
                _record.HeightPx = recorder.Height;
            }

            if (host.HasErrors())
            {
                _record.Problems.Add("el registro tiene errores: " + string.Join(" | ", host.Logger.Lines));
            }
        }

        private IEnumerator Rt1Routine(SnapshotScenario scenario)
        {
            _rt1 = new Rt1Measurement();
            yield return _rt1.Run();
            var image = _rt1.Compose();
            File.WriteAllBytes(Path.Combine(_output, scenario.FileName), image.EncodeToPNG());
            Object.DestroyImmediate(image);
            File.WriteAllText(Path.Combine(_output, "rt1-measurement.json"), _rt1.ToJson(), new UTF8Encoding(false));
            var record = _record!;
            record.WidthPx = Rt1Measurement.Width * 2;
            record.Pipeline = "GammaRt + Overlay";
            record.Probes.Add(new ProbeRecord
            {
                Name = "glass.fallback/white (app)", Element = "rt1-glass", Expected = Rt1Measurement.ExpectedSrgbGlass(), Actual = _rt1.AppGlass,
                Difference = PixelProbe.Distance(_rt1.AppGlass, Rt1Measurement.ExpectedSrgbGlass()), Tolerance = 1, Aspect = true,
                Pass = PixelProbe.Distance(_rt1.AppGlass, Rt1Measurement.ExpectedSrgbGlass()) <= 1
            });
            Debug.Log("[SNAPSHOT] R-T1: app " + _rt1.AppGlass + " / " + _rt1.AppSecondary + " · directa " + _rt1.DirectGlass + " / " + _rt1.DirectSecondary);
            if (_rt1.AppImage != null)
            {
                Object.DestroyImmediate(_rt1.AppImage);
            }

            if (_rt1.DirectImage != null)
            {
                Object.DestroyImmediate(_rt1.DirectImage);
            }
        }

        private ShotRecord NewRecord(SnapshotScenario scenario)
        {
            var host = scenario.Host;
            return new ShotRecord
            {
                Id = scenario.Id,
                File = scenario.FileName,
                Profile = host.Profile.Code,
                WidthPx = host.Profile.WidthPx,
                HeightPx = host.Profile.HeightPx,
                WidthDp = host.Profile.WidthDp,
                HeightDp = host.Profile.HeightDp,
                Density = host.Profile.Density,
                Language = host.LanguageTags.Count > 0 ? host.LanguageTags[0] : "",
                TextScale = host.TextScale,
                ReduceMotion = host.ReduceMotion,
                Pipeline = host.Pipeline.ToString(),
                Backend = scenario.BackendName,
                State = scenario.State
            };
        }

        private void Fail(string message)
        {
            if (_record != null && _record.Error == null)
            {
                _record.Error = message;
            }

            Debug.LogError("[SNAPSHOT] " + (_record != null ? _record.Id + ": " : "") + message);
        }

        private int WriteOutputs()
        {
            var merged = _partial ? MergeWithExisting() : null;
            if (merged == null)
            {
                _manifest.Write(_output, _pipeline.ToString());
            }
            else
            {
                File.WriteAllText(Path.Combine(_output, "manifest.json"), merged, new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(_output, "INDEX.md"), _manifest.ToMarkdown(_pipeline.ToString())
                    + "\n_Corrida parcial: el manifest conserva las demás tomas de la corrida anterior._\n", new UTF8Encoding(false));
            }

            var failed = 0;
            foreach (var shot in _manifest.Shots)
            {
                if (!shot.Passed)
                {
                    failed++;
                }
            }

            // Las sondas de aspecto no hacen fallar la corrida, pero se cuentan y se avisan (QA-V2-04).
            var aspect = SnapshotManifest.FailingAspectProbes(_manifest.Shots);
            if (aspect > 0)
            {
                Debug.LogWarning("[SNAPSHOT] " + aspect + " sondas de aspecto fallan (ver la columna Sondas de INDEX.md)");
            }

            Debug.Log("[SNAPSHOT] listo: " + _manifest.Shots.Count + " tomas, " + failed + " con problemas, " + aspect + " sondas de aspecto que fallan → " + _output);
            return failed == 0 ? 0 : 1;
        }

        // Corrida con filtro: las tomas nuevas reemplazan a las del manifest anterior con el mismo id; el resto se conserva.
        private string? MergeWithExisting()
        {
            var path = Path.Combine(_output, "manifest.json");
            if (!File.Exists(path))
            {
                return null;
            }

            JsonNode existing;
            try
            {
                existing = JsonParser.Parse(File.ReadAllText(path));
            }
            catch (Exception)
            {
                return null;
            }

            var fresh = new HashSet<string>(StringComparer.Ordinal);
            foreach (var shot in _manifest.Shots)
            {
                fresh.Add(shot.Id);
            }

            var chunks = new List<(string Id, string Json)>();
            foreach (var shot in existing["shots"].Items)
            {
                var id = shot["id"].AsString();
                if (!fresh.Contains(id))
                {
                    chunks.Add((id, "    " + JsonText(shot, "    ")));
                }
            }

            foreach (var shot in _manifest.Shots)
            {
                var sb = new StringBuilder();
                shot.AppendJson(sb);
                chunks.Add((shot.Id, sb.ToString()));
            }

            chunks.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var json = new StringBuilder();
            json.Append("{\n  \"generator\": \"RowMate.Snapshots (02-tech-design §4)\",\n  \"pipeline\": ").Append(SnapshotManifest.Q(_pipeline.ToString()))
                .Append(",\n  \"shots\": [\n");
            for (var i = 0; i < chunks.Count; i++)
            {
                json.Append(chunks[i].Json).Append(i < chunks.Count - 1 ? ",\n" : "\n");
            }

            json.Append("  ]\n}\n");
            return json.ToString();
        }

        private static string JsonText(JsonNode node, string indent)
        {
            switch (node.Kind)
            {
                case JsonKind.Null:
                    return "null";
                case JsonKind.Bool:
                    return node.AsBool() ? "true" : "false";
                case JsonKind.Number:
                    return node.RawNumber;
                case JsonKind.String:
                    return SnapshotManifest.Q(node.AsString());
                case JsonKind.Array:
                {
                    var sb = new StringBuilder("[");
                    for (var i = 0; i < node.Items.Count; i++)
                    {
                        sb.Append(i == 0 ? "" : ", ").Append(JsonText(node.Items[i], indent + "  "));
                    }

                    return sb.Append(']').ToString();
                }

                default:
                {
                    var sb = new StringBuilder("{");
                    var first = true;
                    foreach (var pair in node.Properties)
                    {
                        sb.Append(first ? " " : ", ").Append(SnapshotManifest.Q(pair.Key)).Append(": ").Append(JsonText(pair.Value, indent + "  "));
                        first = false;
                    }

                    return sb.Append(" }").ToString();
                }
            }
        }

        private static string Summary(ShotRecord record)
        {
            var parts = new List<string>();
            if (record.Error != null)
            {
                parts.Add(record.Error);
            }

            parts.AddRange(record.Problems);
            parts.AddRange(record.Overflows);
            parts.AddRange(record.ClippedTexts);
            foreach (var probe in record.Probes)
            {
                if (!probe.Pass)
                {
                    parts.Add("sonda " + probe.Name + ": " + probe.Actual + " vs " + probe.Expected + (probe.Aspect ? " (aspecto)" : ""));
                }
            }

            return string.Join(" | ", parts);
        }

        private void Finish(int code)
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            EditorApplication.update -= Update;
            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }

            _exit(code);
        }
    }
}
