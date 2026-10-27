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
        Rt1
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

        public VideoSpec At(double seconds, Action<UiTestHost> action)
        {
            Actions.Add((seconds, action));
            return this;
        }
    }
}
