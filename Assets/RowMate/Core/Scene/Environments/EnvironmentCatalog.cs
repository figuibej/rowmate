using System;

namespace RowMate.Core.Scene
{
    // Lo que cambia de un escenario a otro fuera del generador (02-tech-design §2.3, §2.5; AC-SCN-06-04, AC-SCN-19-03 a -05):
    // el k del rolido, la ganancia y el tope de la respiración de la cámara, si el agua lleva la ola (WaveAmplitude ≥ 0,5: solo
    // la costa) y la silueta lejana.
    public readonly struct EnvironmentProfile
    {
        // DT8-05 / D8-06: la ola del agua degradada y del casco solo desde esta amplitud (la costa).
        public const double SwellThreshold = 0.5;

        public EnvironmentProfile(EnvironmentId id, double rollK, double breathingGain, double breathingCapDp, bool swell, in HorizonSpec horizon)
        {
            Id = id;
            RollK = rollK;
            BreathingGain = breathingGain;
            BreathingCapDp = breathingCapDp;
            Swell = swell;
            Horizon = horizon;
        }

        public EnvironmentId Id { get; }

        // AC-SCN-06-04: lago 0,5, río 0,8, costa 2,0, regata 0,4.
        public double RollK { get; }

        // La respiración de la cámara × 1 (× 1,8 en la costa) y su tope ± 3 dp (± 5,4 en la costa).
        public double BreathingGain { get; }
        public double BreathingCapDp { get; }

        // El tope suave del cabeceo de DT7-06: el tope duro menos 0,05 dp (2,95 con ± 3; 5,35 con ± 5,4).
        public double SoftPitchCapDp => BreathingCapDp - 0.05;

        public bool Swell { get; }

        public HorizonSpec Horizon { get; }
    }

    // Los cuatro escenarios en el orden del selector (lake, river, coast, regatta; environment.dart:10, :65-70).
    public static class EnvironmentCatalog
    {
        public const double LakeRollK = 0.5;
        public const double RiverRollK = 0.8;
        public const double CoastRollK = 2.0;
        public const double RegattaRollK = 0.4;
        public const double CoastBreathingGain = 1.8;
        public const double BaseBreathingCapDp = 3.0;
        public const double CoastBreathingCapDp = 5.4;

        private static readonly EnvironmentId[] Order = { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };

        public static ReadOnlySpan<EnvironmentId> All => Order;

        // Environment.of (environment.dart:65-70): un generador nuevo por pedido (no guarda estado; la caché es de ShoreSegments).
        public static ShoreEnvironment Create(EnvironmentId id)
        {
            switch (id)
            {
                case EnvironmentId.Lake:
                    return new LakeEnvironment();
                case EnvironmentId.River:
                    return new RiverEnvironment();
                case EnvironmentId.Coast:
                    return new CoastEnvironment();
                case EnvironmentId.Regatta:
                    return new RegattaEnvironment();
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, "EnvironmentCatalog: escenario desconocido.");
            }
        }

        public static EnvironmentProfile Profile(EnvironmentId id)
        {
            switch (id)
            {
                case EnvironmentId.Lake:
                    return new EnvironmentProfile(id, LakeRollK, 1.0, BaseBreathingCapDp, false, HorizonSpec.Lake);
                case EnvironmentId.River:
                    return new EnvironmentProfile(id, RiverRollK, 1.0, BaseBreathingCapDp, false, HorizonSpec.River);
                case EnvironmentId.Coast:
                    return new EnvironmentProfile(id, CoastRollK, CoastBreathingGain, CoastBreathingCapDp, true, HorizonSpec.Coast);
                case EnvironmentId.Regatta:
                    return new EnvironmentProfile(id, RegattaRollK, 1.0, BaseBreathingCapDp, false, HorizonSpec.Regatta);
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, "EnvironmentCatalog: escenario desconocido.");
            }
        }

        // ¿El agua y el casco llevan la ola? (WaveAmplitude ≥ 0,5: solo la costa; el lago, el río y la regata quedan calmos.)
        public static bool HasSwell(ShoreEnvironment environment)
        {
            return environment.WaveAmplitude >= EnvironmentProfile.SwellThreshold;
        }
    }
}
