using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Water
{
    // Los cuatro perfiles (AC-6B-11; DV-150): la amplitud y la escala del generador del legacy de cada escenario y el resto de la
    // tabla de WaterLookArt. Sin asignar después de la primera llamada (los perfiles se arman una vez).
    public static class WaterProfiles
    {
        private static readonly WaterProfile Lake = Build(EnvironmentId.Lake);
        private static readonly WaterProfile River = Build(EnvironmentId.River);
        private static readonly WaterProfile Coast = Build(EnvironmentId.Coast);
        private static readonly WaterProfile Regatta = Build(EnvironmentId.Regatta);

        public static WaterProfile For(EnvironmentId id)
        {
            switch (id)
            {
                case EnvironmentId.Lake:
                    return Lake;
                case EnvironmentId.River:
                    return River;
                case EnvironmentId.Coast:
                    return Coast;
                case EnvironmentId.Regatta:
                    return Regatta;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, "WaterProfiles: escenario desconocido.");
            }
        }

        private static WaterProfile Build(EnvironmentId id)
        {
            var env = EnvironmentCatalog.Create(id);
            var swell = EnvironmentCatalog.HasSwell(env);
            switch (id)
            {
                case EnvironmentId.Lake:
                    return new WaterProfile(id, WaterLook.Mirror, env.WaveAmplitude, env.WaveScale, 0, swell, WaterLookArt.LakeB1Slope,
                        new WaterBand(WaterLookArt.LakeB2Tile, WaterLookArt.LakeB2Drift, WaterLookArt.LakeB2Deg, WaterLookArt.LakeB2Strength),
                        new WaterBand(WaterLookArt.LakeB3Tile, WaterLookArt.LakeB3Drift, WaterLookArt.LakeB3Deg, WaterLookArt.LakeB3Strength),
                        WaterLookArt.LakeCalmMin, WaterLookArt.LakeRoughness, WaterLookArt.LakeDistortion,
                        new ShoreBand(WaterLookArt.LakeLeftX, WaterLookArt.LakeLeftHeight, WaterLookArt.LakeLeftTone, 1),
                        new ShoreBand(WaterLookArt.LakeRightX, WaterLookArt.LakeRightHeight, WaterLookArt.LakeRightTone, WaterLookArt.LakeRightScale));
                case EnvironmentId.River:
                {
                    var band = new ShoreBand(WaterLookArt.RiverBandX, WaterLookArt.RiverBandHeight, WaterLookArt.RiverBandTone, WaterLookArt.RiverBandScale,
                        WaterLookArt.RiverWallX, WaterLookArt.RiverWallHeight, WaterLookArt.RiverWallTone);
                    return new WaterProfile(id, WaterLook.Current, env.WaveAmplitude, env.WaveScale, WaterLookArt.RiverCurrent, swell,
                        WaterLookArt.RiverB1Slope,
                        new WaterBand(WaterLookArt.RiverB2Tile, WaterLookArt.RiverB2Drift, WaterLookArt.RiverB2Deg, WaterLookArt.RiverB2Strength),
                        new WaterBand(WaterLookArt.RiverB3Tile, WaterLookArt.RiverB3Drift, WaterLookArt.RiverB3Deg, WaterLookArt.RiverB3Strength),
                        WaterLookArt.RiverCalmMin, WaterLookArt.RiverRoughness, WaterLookArt.RiverDistortion, band, band);
                }

                case EnvironmentId.Coast:
                    return new WaterProfile(id, WaterLook.Sea, env.WaveAmplitude, env.WaveScale, 0, swell, WaterLookArt.CoastB1Slope,
                        new WaterBand(WaterLookArt.CoastB2Tile, WaterLookArt.CoastB2Drift, WaterLookArt.CoastB2Deg, WaterLookArt.CoastB2Strength),
                        new WaterBand(WaterLookArt.CoastB3Tile, WaterLookArt.CoastB3Drift, WaterLookArt.CoastB3Deg, WaterLookArt.CoastB3Strength),
                        WaterLookArt.CoastCalmMin, WaterLookArt.CoastRoughness, WaterLookArt.CoastDistortion,
                        new ShoreBand(WaterLookArt.CoastLeftX, WaterLookArt.CoastLeftHeight, WaterLookArt.CoastLeftTone, 1), ShoreBand.None);
                default:
                {
                    var band = new ShoreBand(WaterLookArt.RegattaBandX, WaterLookArt.RegattaBandHeight, WaterLookArt.RegattaBandTone, 1,
                        WaterLookArt.RegattaSlopeX, WaterLookArt.RegattaSlopeHeight, WaterLookArt.RegattaSlopeTone);
                    return new WaterProfile(id, WaterLook.Track, env.WaveAmplitude, env.WaveScale, 0, swell, WaterLookArt.RegattaB1Slope,
                        new WaterBand(WaterLookArt.RegattaB2Tile, WaterLookArt.RegattaB2Drift, WaterLookArt.RegattaB2Deg, WaterLookArt.RegattaB2Strength),
                        new WaterBand(WaterLookArt.RegattaB3Tile, WaterLookArt.RegattaB3Drift, WaterLookArt.RegattaB3Deg, WaterLookArt.RegattaB3Strength),
                        WaterLookArt.RegattaCalmMin, WaterLookArt.RegattaRoughness, WaterLookArt.RegattaDistortion, band, band);
                }
            }
        }
    }
}
