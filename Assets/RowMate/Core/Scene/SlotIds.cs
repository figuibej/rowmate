using System;
using System.Collections.Generic;

namespace RowMate.Core.Scene
{
    // Los ids de los asset slots de la escena (06a-escena-lago/02-tech-design §2.17; ASSET_SLOTS §1.1 a §1.4 sin el remero, que es de
    // la It. 7) más vegetation.canopyFar (DD6a-22). Un id por PropKind (prop.<nombre del legacy>). Cada uno tiene su .asset en
    // Slots/ con el procedural asignado (WP-B0).
    public static class SlotIds
    {
        public const string WaterProfileLake = "water.profile.lake";
        public const string WaterProfileRiver = "water.profile.river";
        public const string WaterProfileCoast = "water.profile.coast";
        public const string WaterProfileRegatta = "water.profile.regatta";
        public const string WaterNormalDetail = "water.normalDetail";
        public const string WaterFoam = "water.foam";

        public const string SkyProfile = "sky.profile";
        public const string SkyClouds = "sky.clouds";
        public const string SkyMoon = "sky.moon";
        public const string SkyStars = "sky.stars";

        public const string TerrainGrass = "terrain.layer.grass";
        public const string TerrainSand = "terrain.layer.sand";
        public const string TerrainSoil = "terrain.layer.soil";
        public const string TerrainPebbles = "terrain.layer.pebbles";
        public const string TerrainRock = "terrain.layer.rock";
        public const string TerrainSnow = "terrain.layer.snow";

        public const string VegetationGrass = "vegetation.grass";
        public const string VegetationFern = "vegetation.fern";
        public const string VegetationBush = "vegetation.bush";
        public const string VegetationReeds = "vegetation.reeds";
        public const string VegetationPoplar = "vegetation.poplar";
        public const string VegetationCanopyFar = "vegetation.canopyFar";

        public const string HorizonLake = "horizon.lake";
        public const string HorizonRiver = "horizon.river";
        public const string HorizonCoast = "horizon.coast";
        public const string HorizonRegatta = "horizon.regatta";

        public const string PropQuayWall = "prop.quayWall";
        public const string PropRock = "prop.rock";
        public const string PropCrowd = "prop.crowd";
        public const string PropSeagull = "prop.seagull";
        public const string PropBoatRack = "prop.boatRack";

        public const string BoatHull = "boat.hull";
        public const string BoatRigger = "boat.rigger";
        public const string BoatSeat = "boat.seat";
        public const string BoatStretcher = "boat.stretcher";
        public const string BoatOar = "boat.oar";
        public const string BoatLivery = "boat.livery";

        private static readonly string[] Props = BuildProps();

        private static readonly string[] Others =
        {
            WaterProfileLake, WaterProfileRiver, WaterProfileCoast, WaterProfileRegatta, WaterNormalDetail, WaterFoam,
            SkyProfile, SkyClouds, SkyMoon, SkyStars,
            TerrainGrass, TerrainSand, TerrainSoil, TerrainPebbles, TerrainRock, TerrainSnow,
            VegetationGrass, VegetationFern, VegetationBush, VegetationReeds, VegetationPoplar, VegetationCanopyFar,
            HorizonLake, HorizonRiver, HorizonCoast, HorizonRegatta,
            PropQuayWall, PropRock, PropCrowd, PropSeagull, PropBoatRack,
            BoatHull, BoatRigger, BoatSeat, BoatStretcher, BoatOar, BoatLivery,
        };

        // Todos los ids, en un orden estable (los 15 PropKind primero).
        public static IReadOnlyList<string> All { get; } = Concat(Props, Others);

        // prop.pine, prop.laneBuoy… (el nombre del legacy, environment.dart:12-15).
        public static string ForProp(PropKind kind)
        {
            return Props[(int)kind];
        }

        private static string[] BuildProps()
        {
            var kinds = (PropKind[])Enum.GetValues(typeof(PropKind));
            var result = new string[kinds.Length];
            foreach (var kind in kinds)
            {
                var name = kind.ToString();
                result[(int)kind] = "prop." + char.ToLowerInvariant(name[0]) + name.Substring(1);
            }

            return result;
        }

        private static string[] Concat(string[] a, string[] b)
        {
            var result = new string[a.Length + b.Length];
            Array.Copy(a, result, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
        }
    }
}
