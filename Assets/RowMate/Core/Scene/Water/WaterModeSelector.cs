namespace RowMate.Core.Scene.Water
{
    // Qué agua se dibuja: la degradada de la 6a (el respaldo, R-04) o la avanzada en la variante del nivel.
    public enum WaterKind
    {
        Degraded,
        Advanced,
    }

    // -rowmate-water auto|advanced|degraded (solo en desarrollo, DT6b-14) y el agua fija de las tomas viejas (DT6b-24).
    public enum WaterForce
    {
        Auto,
        Advanced,
        Degraded,
    }

    // La elección de un cuadro: el agua elegida, la variante del nivel (Bajo = Lite), la que se ve (la degradada mientras la avanzada
    // se prepara) y el motivo de la degradada (null si no hay).
    public readonly struct WaterChoice
    {
        public WaterChoice(WaterKind kind, QualityTier variant, WaterKind showing, string? reason)
        {
            Kind = kind;
            Variant = variant;
            Showing = showing;
            Reason = reason;
        }

        public WaterKind Kind { get; }
        public QualityTier Variant { get; }
        public WaterKind Showing { get; }
        public string? Reason { get; }

        public override string ToString()
        {
            return Kind + "/" + Variant + " (se ve " + Showing + (Reason == null ? "" : "; " + Reason) + ")";
        }
    }

    // Todo lo que decide, sin Unity: el nivel, si el shader avanzado está soportado (DT6b-22), si falló, si la adaptación llegó al
    // último paso (DV-152), lo forzado y si se permite forzar (desarrollo), si es una miniatura y si la avanzada ya está lista.
    public struct WaterSelectorInputs
    {
        public QualityTier Tier;
        public bool Supported;
        public bool Failed;
        public bool ThermalDegraded;
        public WaterForce Force;
        public bool ForceAllowed;
        public bool Thumbnail;
        public bool AdvancedReady;
        public string? Reason;

        // Lo de un equipo que puede, en Auto y sin nada pegado.
        public static WaterSelectorInputs For(QualityTier tier, bool advancedReady)
        {
            return new WaterSelectorInputs { Tier = tier, Supported = true, AdvancedReady = advancedReady };
        }
    }

    // La regla de AC-6B-06 (F-SCN-13; water_shader.dart:12-22), una fila por línea de la tabla de la spec:
    // - miniatura del selector: siempre la degradada (F-SCN-22);
    // - forzada (solo con ForceAllowed): la forzada; "advanced" sin soporte o con falla cae a la degradada con su motivo;
    // - sin soporte o con falla de carga: la degradada (pegada por la sesión de la app, sin reintento);
    // - la adaptación térmica agotada: la degradada (pegada, DV-152);
    // - si no, la avanzada en la variante del nivel, y mientras no esté lista se ve la degradada (AC-6B-07).
    public static class WaterModeSelector
    {
        public const string UnsupportedReason = "sin SubShader avanzado";
        public const string FailureReason = "falla de carga";
        public const string ThermalReason = "adaptación térmica";
        public const string ForcedReason = "forzada";
        public const string ThumbnailReason = "miniatura";

        public static WaterChoice Choose(in WaterSelectorInputs inputs)
        {
            var tier = inputs.Tier;
            if (inputs.Thumbnail)
            {
                return Degraded(tier, ThumbnailReason);
            }

            var forced = inputs.ForceAllowed ? inputs.Force : WaterForce.Auto;
            if (forced == WaterForce.Degraded)
            {
                return Degraded(tier, ForcedReason);
            }

            if (!inputs.Supported)
            {
                return Degraded(tier, inputs.Reason ?? UnsupportedReason);
            }

            if (inputs.Failed)
            {
                return Degraded(tier, inputs.Reason ?? FailureReason);
            }

            // Forzada la avanzada (con soporte): gana sobre el escalón térmico (con el nivel forzado la adaptación está apagada).
            if (inputs.ThermalDegraded && forced != WaterForce.Advanced)
            {
                return Degraded(tier, ThermalReason);
            }

            return new WaterChoice(WaterKind.Advanced, tier, inputs.AdvancedReady ? WaterKind.Advanced : WaterKind.Degraded, null);
        }

        private static WaterChoice Degraded(QualityTier tier, string reason)
        {
            return new WaterChoice(WaterKind.Degraded, tier, WaterKind.Degraded, reason);
        }
    }
}
