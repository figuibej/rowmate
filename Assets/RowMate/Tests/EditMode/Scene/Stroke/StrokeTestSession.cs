using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // Una sesión de escena para las pruebas de la palada: el lago, la hora de prueba (UTC−03:00, 2026-10-28) y pasos de 1/60 s.
    internal sealed class StrokeTestSession
    {
        public static readonly DateTimeOffset Local = new DateTimeOffset(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(-3));

        public StrokeTestSession(double width = 411, double height = 914)
        {
            Width = width;
            Height = height;
            Session = new SceneSession(new LakeEnvironment(), SceneSessionOptions.Default);
            Session.Open(0);
            Frame = SceneFrame.Create();
        }

        public SceneSession Session { get; }

        public SceneFrame Frame;

        public double Now { get; private set; }

        // El paso que vio la sesión en el último cuadro (para avanzar un SceneState de control con el mismo dt).
        public double LastDt { get; private set; }

        private long _frames;

        public double Width { get; }

        public double Height { get; }

        public int? ForcedHour { get; set; } = 12;

        public bool ReduceMotion { get; set; }

        public static SceneInputs Rowing(double spm, int watts = 150, double pace = 125) => new SceneInputs(pace, spm, true, watts, BuoyInputs.None);

        public static SceneInputs Pause(int watts = 150) => new SceneInputs(125, 24, false, watts, BuoyInputs.None);

        public ref RowerFrame Rower => ref Frame.Rower;

        public void Step(in SceneInputs w)
        {
            _frames++;
            var now = _frames / 60.0;
            LastDt = now - Now;
            Now = now;
            Session.Tick(new SceneTickInputs(Now, Local, Local.ToUniversalTime(), w, ForcedHour, ReduceMotion, Width, Height, 0), ref Frame);
        }

        public void Run(double seconds, in SceneInputs w)
        {
            var n = (int)Math.Round(seconds * 60);
            for (var i = 0; i < n; i++)
            {
                Step(w);
            }
        }
    }
}
