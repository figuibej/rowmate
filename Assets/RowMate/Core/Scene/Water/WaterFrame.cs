namespace RowMate.Core.Scene.Water
{
    // Lo del agua avanzada en un cuadro (06b-agua-avanzada/02-tech-design §3.3): el perfil del escenario, las fases en double, el
    // astro (la x de la columna y su fuerza), las bandas de orilla con la luz de la hora, la huella del casco (la línea de contacto),
    // la estela empaquetada, los micro-anillos y las columnas de luz. Lo llena SceneSession.Tick sin asignar (los arreglos se reservan
    // en Create). La elección del agua y el fundido (Choice, DegradedBlend) los pone SceneHost (WP-B): la sesión no sabe del equipo.
    public struct WaterFrame
    {
        public WaterProfile Profile;
        public WaterPhaseSet Phases;

        public WaterChoice Choice;
        public double DegradedBlend;

        // El astro que se ve: la x de pantalla de la columna (la de las líneas, DV-59), la fuerza del legacy × la visibilidad.
        public double ColumnCenterX;
        public double ColumnStrength;

        // Las bandas de orilla (Medio) con el tono × la luz de la hora.
        public SceneColor LeftBand;
        public SceneColor RightBand;
        public SceneColor LeftLowBand;
        public SceneColor RightLowBand;

        // La línea de contacto del casco (00-arte-agua §4.3): de la popa a la proa en cinta (con el surge) y el semiancho + 0,06 m.
        public double HullStartRelZ;
        public double HullEndRelZ;
        public double HullHalfWidth;

        // La estela: intensidad de la popa en este cuadro y la historia empaquetada (WakeTrail.CopyTo: 48 × 4 floats).
        public double WakeIntensity;
        public float[] WakePacked;
        public int WakeCount;

        // La boya de la costa sobre el mar de fondo (0 fuera de la costa).
        public double BuoyHeave;

        public MicroRingField Rings;
        public LightColumns Columns;

        public static WaterFrame Create()
        {
            return new WaterFrame
            {
                WakePacked = new float[WakeTrail.Capacity * WakeTrail.PackedStride],
                DegradedBlend = 1,
                Choice = new WaterChoice(WaterKind.Degraded, QualityTier.Medium, WaterKind.Degraded, null),
            };
        }
    }
}
