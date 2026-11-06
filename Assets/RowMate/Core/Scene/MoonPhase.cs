using System;
using RowMate.Core.Text;

namespace RowMate.Core.Scene
{
    // La fase de la luna de una fecha: edad en días, ángulo de fase φ (radianes) e iluminación (0 nueva, 1 llena).
    public readonly struct MoonPhaseInfo
    {
        public MoonPhaseInfo(double ageDays, double phaseAngle, double illumination)
        {
            AgeDays = ageDays;
            PhaseAngle = phaseAngle;
            Illumination = illumination;
        }

        public double AgeDays { get; }

        public double PhaseAngle { get; }

        public double Illumination { get; }

        // Menguante: después de la llena (φ > π).
        public bool IsWaning => PhaseAngle > Math.PI;
    }

    // El modelo sinódico de la luna (DV-57; ART_BIBLE §6.3; 00-deuda-diseno §4.3): luna nueva de referencia 2000-01-06 18:14 UTC,
    // edad = Mod(t − ref; mes), φ = 2π·edad/mes, iluminación = (1 − cos φ)/2. La fecha es la del reloj del equipo (IClock.UtcNow),
    // también con hora forzada. El lado iluminado se espeja en el hemisferio sur (la región del sistema; sin región, el norte).
    public static class MoonPhase
    {
        public const double SynodicDays = 29.530588853;

        public static readonly DateTimeOffset ReferenceNewMoon = new DateTimeOffset(2000, 1, 6, 18, 14, 0, TimeSpan.Zero);

        // Los países del hemisferio sur (P2 del Designer): ISO 3166-1 alfa-2.
        private static readonly string[] Southern =
        {
            "AR", "AU", "BO", "BR", "CL", "LS", "MG", "MZ", "NA", "NZ", "PE", "PY", "SZ", "UY", "ZA", "ZW",
        };

        public static MoonPhaseInfo At(DateTimeOffset utc)
        {
            var days = (utc - ReferenceNewMoon).TotalDays;
            var age = DartMath.Mod(days, SynodicDays);
            var phi = 2 * Math.PI * age / SynodicDays;
            return new MoonPhaseInfo(age, phi, (1 - Math.Cos(phi)) / 2);
        }

        public static bool IsSouthernRegion(string? regionCode)
        {
            if (string.IsNullOrEmpty(regionCode))
            {
                return false;
            }

            var code = regionCode!.Trim().ToUpperInvariant();
            for (var i = 0; i < Southern.Length; i++)
            {
                if (Southern[i] == code)
                {
                    return true;
                }
            }

            return false;
        }

        // El factor del halo de la luna (DV-57): 0,4 + 0,6·iluminación.
        public static double HaloFactor(double illumination)
        {
            return Art.SkyArt.MoonHaloBase + Art.SkyArt.MoonHaloPerIllumination * illumination;
        }
    }
}
