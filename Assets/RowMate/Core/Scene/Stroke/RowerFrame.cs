namespace RowMate.Core.Scene.Stroke
{
    // Lo que se dibuja del remero, el bote y el agua en un cuadro (SceneFrame.Rower; 02-tech-design §2.12): la pose, el movimiento
    // del bote, las fases (p, φ, c, r), el esfuerzo, los eventos del cuadro, la sombra, los charcos visibles, la salpicadura y las
    // gotas, la boya y los hervideros. Los objetos de efectos son los de la sesión (se asignan al abrir; Tick solo escribe).
    public struct RowerFrame
    {
        public ScullingPose Pose;
        public BoatMotionSample Boat;

        // It. 8 (AC-SCN-06-04): la parte de la ola de la costa que ya está sumada en Boat (filtrada, τ 0,3 s); 0 fuera de la costa.
        public double SwellHeaveM;
        public double SwellPitchRad;

        public double ContractPhase;
        public double Phase;
        public double Canonical;
        public double Ratio;
        public double PhaseSpeed;
        public double Effort;
        public double IdleWeight;
        public StrokeEvents Events;
        public BoatShadowMode ShadowMode;
        public double ShadowSide;
        public double ShadowAlpha;
        public PuddleField Puddles;
        public StrokeSpray Spray;
        public BuoyTracker Buoy;
        public bool BoilLeft;
        public bool BoilRight;

        // La posición del bote en z de mundo (distancia de escena + 9,5 + surge): el origen del marco del bote en el agua.
        public double BoatWorldZ;

        public static RowerFrame Create()
        {
            return new RowerFrame { Pose = ScullingPose.Create() };
        }
    }

    // Lo que el nivel de calidad le dice a la palada (lo pone SceneHost al armar y cuando cambia el nivel): si hay mapa de sombras y si
    // es Alto (la fuerza de sombra de la luz), la escala de partículas (1 en Alto y Medio, 0,5 en Bajo) y si hay hervidero (no en Bajo).
    public readonly struct RowerRenderTier
    {
        public static readonly RowerRenderTier High = new RowerRenderTier(true, true, 1, true);
        public static readonly RowerRenderTier Medium = new RowerRenderTier(true, false, 1, true);
        public static readonly RowerRenderTier Low = new RowerRenderTier(false, false, 0.5, false);

        public RowerRenderTier(bool hasShadows, bool highTier, double particleScale, bool boils)
        {
            HasShadows = hasShadows;
            HighTier = highTier;
            ParticleScale = particleScale;
            Boils = boils;
        }

        public bool HasShadows { get; }
        public bool HighTier { get; }
        public double ParticleScale { get; }
        public bool Boils { get; }
    }
}
