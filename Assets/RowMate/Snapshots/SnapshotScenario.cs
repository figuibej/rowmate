using System;
using System.Collections;
using System.Collections.Generic;
using RowMate.Tests.UiSupport;

namespace RowMate.Snapshots
{
    public enum SnapshotKind
    {
        Still,
        Video,
        Rt1,

        // Una sola armada con varias tomas (DEMO de la It. 3): cada ScenarioScript.Shot es una foto fija con su registro.
        Sequence
    }

    // Una toma del catálogo (02-tech-design §4.3): una captura fija, un video o la medición de R-T1. Cada toma arma su propio
    // UiTestHost desde cero (las tomas "a", "b"… de una captura son escenarios distintos).
    public sealed class SnapshotScenario
    {
        public SnapshotScenario(string id, string fileStem, UiHostOptions host, string state, Func<ScenarioScript, IEnumerator>? arrange)
        {
            Id = id;
            FileStem = fileStem;
            Host = host;
            State = state;
            Arrange = arrange;
        }

        public string Id { get; }

        // Nombre del archivo sin extensión ("CAP-01-dispositivo-tv").
        public string FileStem { get; }

        public UiHostOptions Host { get; }

        // Texto de la spec (§7.3): cómo se llega.
        public string State { get; }

        public string BackendName { get; set; } = "";

        // Acciones con el reloj de prueba (yield return null = un EditorApplication.update).
        public Func<ScenarioScript, IEnumerator>? Arrange { get; }

        public SnapshotKind Kind { get; set; } = SnapshotKind.Still;

        // null = la ventana entera; si no, el recorte del elemento (worldBound × densidad + 8 dp de margen).
        public string? CropElement { get; set; }

        public List<PixelProbe> Probes { get; } = new List<PixelProbe>();

        // Sondas que dependen del aspecto de WP-D (Skin.uss): se anotan pero no hacen fallar la corrida.
        public HashSet<string> AspectProbes { get; } = new HashSet<string>(StringComparer.Ordinal);

        public VideoSpec? Video { get; set; }

        // Comprobaciones propias del escenario (textos, columnas…): devuelven los problemas encontrados.
        public Func<UiTestHost, IEnumerable<string>>? Checks { get; set; }

        // Toma sin la regla de la ventana [N + 0,55; N + 1) (escenarios sin lecturas periódicas que vencen a mitad).
        public bool SkipReadingWindow { get; set; }

        public string FileName => FileStem + (Kind == SnapshotKind.Video ? ".mp4" : ".png");

        // It. 4b (04b-hud-exr/02-tech-design §5.6; DT4b-07): una toma de it4a reemplazada por las de it4b ("it4b" y los IDs que la
        // reemplazan). El arnés no la vuelve a tomar, conserva el archivo de la 4a y la lista en el manifest como superseded.
        public string? SupersededBy { get; set; }

        public string SupersededIds { get; set; } = "";

        // It. 4b (§5.2, DT4b-19): la regla de toma de la 4b. La toma se hace EXACTAMENTE en el instante del armado (nunca en un
        // segundo entero del motor) y FALLA si algo de la lista de bloqueos corre (SnapshotTakeRule): nunca espera.
        public bool ExactInstant { get; set; }

        // Sequence: las tomas que tiene que hacer, en orden (id, archivo sin extensión). Las que no se lleguen a tomar quedan en
        // el manifest como fallas.
        public List<(string Id, string Stem, string State)> SequenceShots { get; } = new List<(string, string, string)>();

        public SnapshotScenario Shot(string id, string stem, string state)
        {
            SequenceShots.Add((id, stem, state));
            return this;
        }

        public SnapshotScenario WithProbe(PixelProbe probe, bool aspect = false)
        {
            Probes.Add(probe);
            if (aspect)
            {
                AspectProbes.Add(probe.Name);
            }

            return this;
        }
    }

    // Un video (VID-01 a VID-03): segundos, cuadros por segundo y acciones en instantes relativos al comienzo.
    public sealed class VideoSpec
    {
        public VideoSpec(double seconds, int fps = 30)
        {
            Seconds = seconds;
            Fps = fps;
        }

        public double Seconds { get; }

        public int Fps { get; }

        public List<(double At, Action<UiTestHost> Action)> Actions { get; } = new List<(double, Action<UiTestHost>)>();

        // It. 4b (§5.5): los valores de cada cuadro (los que devuelven los IMotion de WP-D1) para el manifest, y la comparación con
        // las líneas de tiempo puras (± 0,01): devuelve un problema o null. t = segundos desde el comienzo del video.
        public Func<UiTestHost, double, string?>? FrameValues { get; set; }

        public Func<UiTestHost, double, string?>? FrameCheck { get; set; }

        public VideoSpec At(double seconds, Action<UiTestHost> action)
        {
            Actions.Add((seconds, action));
            return this;
        }
    }
}
