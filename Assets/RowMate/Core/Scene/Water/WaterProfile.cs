using System;

namespace RowMate.Core.Scene.Water
{
    // El carácter del agua de cada escenario (DV-150; ART_BIBLE §7.7): lago Espejo, río Corriente, costa Mar y regata Pista.
    public enum WaterLook
    {
        Mirror,
        Current,
        Sea,
        Track,
    }

    // Una banda de ondas del mapa de normales (B2 chop o B3 rizo): tile (m), deriva (m/s), θ (grados, hacia donde viaja la onda,
    // desde +z y positivo hacia +x) y fuerza.
    public readonly struct WaterBand
    {
        public WaterBand(double tile, double drift, double directionDeg, double strength)
        {
            Tile = tile;
            Drift = drift;
            DirectionDeg = directionDeg;
            Strength = strength;
        }

        public double Tile { get; }
        public double Drift { get; }
        public double DirectionDeg { get; }
        public double Strength { get; }

        // La dirección de viaje en (x; z).
        public double DirX => Math.Sin(SceneMath.DegToRad(DirectionDeg));
        public double DirZ => Math.Cos(SceneMath.DegToRad(DirectionDeg));
    }

    // La banda de orilla de un lado para el reflejo analítico de Medio (00-arte-agua §4.2): el rayo reflejado cruza |x| = X a una
    // distancia t y, si su elevación es menor que atan(Height/t), toma el tono (× ToneScale × la luz de la hora). El tramo bajo
    // (el muro del río o el talud de la regata) es otra banda más cerca. Present = false: sin orilla de ese lado (la costa a la
    // derecha, mar abierto).
    public readonly struct ShoreBand
    {
        public ShoreBand(double x, double height, uint tone, double toneScale, double lowX = double.NaN, double lowHeight = 0, uint lowTone = 0)
        {
            X = x;
            Height = height;
            Tone = tone;
            ToneScale = toneScale;
            LowX = lowX;
            LowHeight = lowHeight;
            LowTone = lowTone;
        }

        public static ShoreBand None => new ShoreBand(double.NaN, 0, 0, 0);

        public double X { get; }
        public double Height { get; }
        public uint Tone { get; }
        public double ToneScale { get; }
        public double LowX { get; }
        public double LowHeight { get; }
        public uint LowTone { get; }

        public bool Present => !double.IsNaN(X);
        public bool HasLow => !double.IsNaN(LowX);

        // El color de la banda (sRGB del legacy) antes de la luz de la hora.
        public SceneColor Color => Present ? Scaled(SceneColor.FromArgb(Tone), ToneScale) : SceneColor.Black;

        // ¿El reflejo de un rayo con esa elevación (grados, sobre el horizonte) a esa distancia horizontal cae sobre la banda?
        public bool Covers(double elevationDeg, double horizontalDistance)
        {
            if (!Present || horizontalDistance <= 0)
            {
                return false;
            }

            return elevationDeg < SceneMath.RadToDeg(Math.Atan(Height / horizontalDistance));
        }

        private static SceneColor Scaled(in SceneColor c, double k)
        {
            return new SceneColor(c.R * k, c.G * k, c.B * k);
        }
    }

    // El perfil del agua de un escenario (06b-agua-avanzada/02-tech-design §3.1; 00-arte-agua §3.2): la amplitud y la escala son las
    // del legacy (ShoreEnvironment: 0,25/2,5; 0,35/2,0; 0,8/4,0; 0,3/1,5) y lo visual sale de WaterLookArt.
    public readonly struct WaterProfile
    {
        public WaterProfile(EnvironmentId environment, WaterLook look, double amplitude, double scale, double currentMps, bool swell, double b1Slope,
                            in WaterBand b2, in WaterBand b3, double calmMin, double roughness, double distortion, in ShoreBand left, in ShoreBand right)
        {
            Environment = environment;
            Look = look;
            Amplitude = amplitude;
            Scale = scale;
            CurrentMps = currentMps;
            Swell = swell;
            B1Slope = b1Slope;
            B2 = b2;
            B3 = b3;
            CalmMin = calmMin;
            Roughness = roughness;
            Distortion = distortion;
            Left = left;
            Right = right;
        }

        public EnvironmentId Environment { get; }
        public WaterLook Look { get; }
        public double Amplitude { get; }
        public double Scale { get; }

        // La corriente hacia la cámara (m/s): 0,25 en el río, 0 en los demás.
        public double CurrentMps { get; }

        // B0, el mar de fondo con desplazamiento (solo la costa).
        public bool Swell { get; }

        public double B1Slope { get; }
        public WaterBand B2 { get; }
        public WaterBand B3 { get; }
        public double CalmMin { get; }
        public double Roughness { get; }
        public double Distortion { get; }
        public ShoreBand Left { get; }
        public ShoreBand Right { get; }

        // La longitud de onda de las dos componentes de B1: 2π·s y 2π·s/0,7 (lago: 15,7 y 22,4 m a lo largo de z).
        public double B1Wavelength => 2 * Math.PI * Scale;

        // La velocidad de la cresta de B1 hacia la cámara: 0,6·s m/s (la fase avanza 0,6·t).
        public double B1CrestSpeed => 0.6 * Scale;
    }
}
