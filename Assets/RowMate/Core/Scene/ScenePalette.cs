namespace RowMate.Core.Scene
{
    // Los colores de la escena para una hora (scene/time_of_day.dart:4-38). Ambient es el multiplicador de la luz de la orilla y
    // del bote (1 = día, 0,35 = noche). Una sola paleta manda (AC-SCN-09-04): todo color de la escena sale de acá.
    public readonly struct ScenePalette
    {
        public ScenePalette(in SceneColor skyTop, in SceneColor skyHorizon, in SceneColor waterNear, in SceneColor waterFar,
                            in SceneColor sunColor, in SceneColor sunGlow, in SceneColor fog, double ambient)
        {
            SkyTop = skyTop;
            SkyHorizon = skyHorizon;
            WaterNear = waterNear;
            WaterFar = waterFar;
            SunColor = sunColor;
            SunGlow = sunGlow;
            Fog = fog;
            Ambient = ambient;
        }

        public SceneColor SkyTop { get; }

        public SceneColor SkyHorizon { get; }

        public SceneColor WaterNear { get; }

        public SceneColor WaterFar { get; }

        public SceneColor SunColor { get; }

        public SceneColor SunGlow { get; }

        public SceneColor Fog { get; }

        public double Ambient { get; }

        // time_of_day.dart:29-38: los colores con Color.lerp (recortado) y el ambiente con a + (b − a)·t.
        public static ScenePalette Lerp(in ScenePalette a, in ScenePalette b, double t)
        {
            return new ScenePalette(
                SceneColor.Lerp(a.SkyTop, b.SkyTop, t),
                SceneColor.Lerp(a.SkyHorizon, b.SkyHorizon, t),
                SceneColor.Lerp(a.WaterNear, b.WaterNear, t),
                SceneColor.Lerp(a.WaterFar, b.WaterFar, t),
                SceneColor.Lerp(a.SunColor, b.SunColor, t),
                SceneColor.Lerp(a.SunGlow, b.SunGlow, t),
                SceneColor.Lerp(a.Fog, b.Fog, t),
                a.Ambient + (b.Ambient - a.Ambient) * t);
        }
    }
}
