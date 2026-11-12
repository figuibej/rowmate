using RowMate.Core.Scene.Water;

namespace RowMate.Core.Scene
{
    // Una estrella lista para dibujar: la dirección del mundo (unitaria), el radio angular y el alfa (ya con el fundido).
    public readonly struct StarInstance
    {
        public StarInstance(double dirX, double dirY, double dirZ, double radiusRad, double alpha)
        {
            DirX = dirX;
            DirY = dirY;
            DirZ = dirZ;
            RadiusRad = radiusRad;
            Alpha = alpha;
        }

        public double DirX { get; }
        public double DirY { get; }
        public double DirZ { get; }
        public double RadiusRad { get; }
        public double Alpha { get; }
    }

    // Una nube lista para dibujar: la dirección del centro de su base, el tamaño angular (s/focal), la variante del atlas
    // (i mod 4), los alfas del cuerpo y de la sombra (ya con WrapFade) y su punto en la pantalla del legacy.
    public readonly struct CloudInstance
    {
        public CloudInstance(double dirX, double dirY, double dirZ, double sizeRad, int variant, double bodyAlpha, double shadeAlpha, double screenX,
                             double screenY)
        {
            DirX = dirX;
            DirY = dirY;
            DirZ = dirZ;
            SizeRad = sizeRad;
            Variant = variant;
            BodyAlpha = bodyAlpha;
            ShadeAlpha = shadeAlpha;
            ScreenX = screenX;
            ScreenY = screenY;
        }

        public double DirX { get; }
        public double DirY { get; }
        public double DirZ { get; }
        public double SizeRad { get; }
        public int Variant { get; }
        public double BodyAlpha { get; }
        public double ShadeAlpha { get; }
        public double ScreenX { get; }
        public double ScreenY { get; }
    }

    // Una gaviota de la costa lista para dibujar (SeagullFlight): acimut y elevación (rad), aleteo (−1 a 1), tamaño (× la
    // envergadura) y alfa del fundido de la vuelta.
    public readonly struct SeagullInstance
    {
        public SeagullInstance(double azimuthRad, double elevationRad, double flap, double size, double alpha)
        {
            AzimuthRad = azimuthRad;
            ElevationRad = elevationRad;
            Flap = flap;
            Size = size;
            Alpha = alpha;
        }

        public double AzimuthRad { get; }
        public double ElevationRad { get; }
        public double Flap { get; }
        public double Size { get; }
        public double Alpha { get; }
    }

    // Lo que hay que dibujar en un cuadro (06a-escena-lago/02-tech-design §2.15): la cámara (pose y matriz), la paleta, el astro
    // (con los radios del disco y del halo y el espejo del sur), la luz y el post, las 80 estrellas, las 7 nubes, las cordilleras,
    // el agua (los tres colores, las 10 líneas, la fase y la visibilidad del astro), la bruma, los objetos de la orilla visibles con
    // el nivel de sus luces, la distancia (double), la velocidad, el tiempo de escena y la hora visible. Los arreglos se reservan
    // en Create (o al abrir): Tick solo escribe. Draw = false con la escena oculta (lo pone quien dibuja).
    public struct SceneFrame
    {
        public const int TrailingCapacity = 4;

        public bool Draw;

        // Solo el dibujo de calentamiento (§3.4): estrellas, ventanas y nubes a la vista para que el dibujo toque todas las
        // variantes de la sesión.
        public bool WarmUpAll;

        public SceneCamera Window;
        public CameraPose Pose;
        public SceneMatrix4 Projection;
        public CameraMode CameraMode;
        public double BreathingMultiplier;
        public double HorizonDeviationDp;

        public double VisibleHour;
        public ScenePalette Palette;
        public CelestialState Celestial;
        public LegacyLight LegacyLight;
        public MoonPhaseInfo Moon;
        public bool SouthernMirror;
        public double DiskRadiusRad;
        public double HaloRadiusRad;
        public double HaloAlphaScale;
        public double DiskHdr;

        public HourLook Look;
        public PostLook Post;

        public StarInstance[] Stars;
        public double StarFade;
        public CloudInstance[] Clouds;
        public SceneColor CloudBody;

        public double HorizonPeakTan;
        public double HorizonFarScale;
        public double HorizonNearScale;
        public SceneColor HorizonFar;
        public SceneColor HorizonNear;
        public SceneColor Snow;

        public SceneColor WaterTop;
        public SceneColor WaterMid;
        public SceneColor WaterBottom;
        public ShimmerLine[] Lines;
        public double WaterPhase;
        public double ShimmerVisibility;
        public double ShimmerCenterX;

        public double LowMist;
        public double Golden;

        public ShorePropBuffer VisibleProps;

        // El nivel de la luz de cada objeto visible (en el mismo orden): 0-1 para lo que se enciende de noche (cabañas; desde la
        // It. 8 también ventanas de edificios, portón del club, faroles, faro y torre), 0 para lo demás.
        public double[] PropLight;

        // It. 8 (02-tech-design §2.2, §2.3, §2.7): el escenario, los puentes que siguen detrás de la cámara (DT8-08) con su luz,
        // el puente más cercano en cinta (relZ del tablero; NaN sin puente) y su sombra sobre el bote (DT8-07), la ola de la costa
        // (las fases en double, DT8-06) y las gaviotas.
        public EnvironmentId Environment;
        public ShorePropBuffer TrailingProps;
        public double[] TrailingLight;
        public double BridgeStartRelZ;
        public double BridgeEndRelZ;
        public double UnderBridgeAtBoat;
        public bool Swell;
        public double SwellAmplitude;
        public double SwellScale;
        public double SwellPhase1;
        public double SwellPhase2;
        public SeagullInstance[] Seagulls;
        public double SeagullOpacity;

        // It. 7: el remero, el bote y el agua (02-tech-design §2.12).
        public Stroke.RowerFrame Rower;

        // It. 6b (06b-agua-avanzada/02-tech-design §3.3): lo del agua avanzada (fases, estela, anillos, columnas, bandas y casco).
        public WaterFrame Water;

        public double Distance;
        public double Speed;
        public double SceneTime;

        // 200: el tope más alto de los cuatro escenarios (la regata); alcanza para cualquiera.
        public static SceneFrame Create(int maxVisibleProps = 200)
        {
            return new SceneFrame
            {
                TrailingProps = new ShorePropBuffer(TrailingCapacity),
                TrailingLight = new double[TrailingCapacity],
                Seagulls = new SeagullInstance[SeagullFlight.Count],
                BridgeStartRelZ = double.NaN,
                BridgeEndRelZ = double.NaN,
                Stars = new StarInstance[StarField.Count],
                Clouds = new CloudInstance[CloudModel.Count],
                Lines = new ShimmerLine[DegradedWater.LineCount],
                VisibleProps = new ShorePropBuffer(maxVisibleProps),
                PropLight = new double[maxVisibleProps],
                Projection = SceneMatrix4.Identity,
                Rower = Stroke.RowerFrame.Create(),
                Water = WaterFrame.Create(),
            };
        }
    }
}
