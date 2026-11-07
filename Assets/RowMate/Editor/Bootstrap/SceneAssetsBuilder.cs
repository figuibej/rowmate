using System;
using System.Collections.Generic;
using RowMate.Core.Scene;
using RowMate.Scene.Quality;
using RowMate.Scene.Slots;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RowMate.EditorTools
{
    // Paso scene.assets del bootstrapper v4 (06a-escena-lago/02-tech-design §1.1, §3.7, §4.2, §7.2 B0; QA-G6aT-04, -14): los assets de
    // la escena que NO dependen de los shaders de WP-D1 (los materiales y la colección de variantes son de SceneMaterialsBuilder, B1):
    //   Settings/Quality/{High,Medium,Low}.asset   QualityTierProfile con su RowMate_URP_<nivel> (UrpProvisioner)
    //   Slots/<id>.asset                           un slot por id de SlotIds, con la fuente Procedural asignada (sin aviso)
    //   Scenes/Environments/Lake.asset             los slots del Lago alpino
    //   Scenes/SceneWorld.asset                    el escenario, los slots del cielo y del bote y los tres niveles
    // Idempotente: crea lo que falta y vuelve a poner los valores; nunca pisa la fuente Asset que el usuario asignó a un slot.
    internal static class SceneAssetsBuilder
    {
        public const string QualityFolder = "Assets/RowMate/Settings/Quality";
        public const string SlotsFolder = "Assets/RowMate/Slots";
        public const string EnvironmentsFolder = "Assets/RowMate/Scenes/Environments";
        public const string LakePath = EnvironmentsFolder + "/Lake.asset";
        public const string ThirdPartyPack = "Assets/ThirdParty/TerrainSampleAssetPack/";

        public static string TierPath(QualityTier tier)
        {
            return QualityFolder + "/" + tier + ".asset";
        }

        public static string SlotPath(string slotId)
        {
            return SlotsFolder + "/" + slotId + ".asset";
        }

        public static BootstrapState.Step Apply(string id)
        {
            FontAssetBuilder.EnsureFolder(QualityFolder);
            FontAssetBuilder.EnsureFolder(SlotsFolder);
            FontAssetBuilder.EnsureFolder(EnvironmentsFolder);
            var created = 0;

            var tiers = new Dictionary<QualityTier, QualityTierProfile>();
            foreach (QualityTier tier in Enum.GetValues(typeof(QualityTier)))
            {
                var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpProvisioner.PipelinePath(tier));
                if (urp == null)
                {
                    throw new InvalidOperationException("falta " + UrpProvisioner.PipelinePath(tier) + " (paso urp.tiers)");
                }

                var profile = LoadOrCreate<QualityTierProfile>(TierPath(tier), ref created);
                profile.SetDefaults(tier, urp, UrpProvisioner.LevelNames[UrpProvisioner.LevelIndex(tier)]);
                EditorUtility.SetDirty(profile);
                tiers[tier] = profile;
            }

            var slots = new Dictionary<string, AssetSlotDefinition>(StringComparer.Ordinal);
            var userAssigned = 0;
            foreach (var slotId in SlotIds.All)
            {
                var path = SlotPath(slotId);
                var type = SlotTypeFor(slotId);
                var slot = AssetDatabase.LoadAssetAtPath<AssetSlotDefinition>(path);
                if (slot != null && slot.GetType() != type)
                {
                    // Un tipo viejo: se rehace (no puede tener una asignación válida del tipo nuevo).
                    AssetDatabase.DeleteAsset(path);
                    slot = null;
                }

                if (slot == null)
                {
                    slot = (AssetSlotDefinition)ScriptableObject.CreateInstance(type);
                    slot.name = slotId;
                    slot.Configure(slotId, ExpectedPathFor(slotId), BudgetFor(slotId));
                    slot.SetSource(SlotSource.Procedural);
                    AssetDatabase.CreateAsset(slot, path);
                    created++;
                }
                else
                {
                    slot.Configure(slotId, ExpectedPathFor(slotId), BudgetFor(slotId));
                    if (slot.Source == SlotSource.Asset)
                    {
                        userAssigned++;
                    }

                    EditorUtility.SetDirty(slot);
                }

                if (slot is RowerCharacterSlot rower)
                {
                    // DT7-13 (QA7-G-04): el slot del remero se versiona SIEMPRE en Procedural, con sus rutas esperadas; la referencia al FBX
                    // de Mixamo vive en el binding generado (RowerCharacterBindingBuilder, más abajo).
                    rower.SetSource(SlotSource.Procedural);
                    rower.SetExpectedPaths(RowerCharacterSlot.DefaultPaths);
                    EditorUtility.SetDirty(rower);
                }

                slots[slotId] = slot;
            }

            var lake = LoadOrCreate<EnvironmentDefinition>(LakePath, ref created);
            var lakeSlots = new List<AssetSlotDefinition>();
            foreach (var slotId in LakeSlotIds())
            {
                lakeSlots.Add(slots[slotId]);
            }

            lake.Set(EnvironmentId.Lake, lakeSlots);
            EditorUtility.SetDirty(lake);

            var world = LoadOrCreate<SceneWorldDefinition>(SceneWorldDefinition.AssetPath, ref created);
            var worldSlots = new List<AssetSlotDefinition>();
            foreach (var slotId in WorldSlotIds())
            {
                worldSlots.Add(slots[slotId]);
            }

            world.SetSlots(lake, worldSlots, tiers[QualityTier.High], tiers[QualityTier.Medium], tiers[QualityTier.Low]);
            EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssets();

            // It. 7 (DT7-04, DT7-13): los FBX de Mixamo que se importaron antes del postprocesador, otra vez; y el binding del remero.
            var reimported = MixamoImportPostprocessor.ReimportAll();
            var binding = RowerCharacterBindingBuilder.Refresh();
            return BootstrapState.Step.Ok(id, created + " asset(s) nuevos; " + SlotIds.All.Count + " slots (" + userAssigned
                + " con un asset del usuario, el resto procedurales); 3 niveles; Lake y SceneWorld; Mixamo: " + reimported + " reimportado(s), " + binding);
        }

        // El tipo de cada slot (ASSET_SLOTS §1; It. 7: el remero, su ropa y los efectos).
        public static Type SlotTypeFor(string slotId)
        {
            if (slotId == SlotIds.RowerCharacter)
            {
                return typeof(RowerCharacterSlot);
            }

            if (slotId == SlotIds.RowerOutfit)
            {
                return typeof(MaterialSlot);
            }

            if (slotId.StartsWith("fx.", StringComparison.Ordinal))
            {
                return typeof(TextureSlot);
            }

            if (slotId.StartsWith("terrain.layer.", StringComparison.Ordinal))
            {
                return typeof(TerrainLayerSlot);
            }

            if (slotId.StartsWith("vegetation.", StringComparison.Ordinal))
            {
                return typeof(VegetationSlot);
            }

            if (slotId.StartsWith("horizon.", StringComparison.Ordinal))
            {
                return typeof(HorizonProfileSlot);
            }

            if (slotId.StartsWith("water.profile.", StringComparison.Ordinal))
            {
                return typeof(WaterProfileSlot);
            }

            if (slotId == SlotIds.SkyProfile)
            {
                return typeof(SkyProfileSlot);
            }

            if (slotId.StartsWith("sky.", StringComparison.Ordinal) || slotId.StartsWith("water.", StringComparison.Ordinal))
            {
                return typeof(TextureSlot);
            }

            if (slotId == SlotIds.BoatLivery)
            {
                return typeof(MaterialSlot);
            }

            return typeof(PropSlot);
        }

        // Dónde iría el asset de terceros (re-enlace por ruta, RA-17): solo lo del Terrain Sample Asset Pack.
        public static string ExpectedPathFor(string slotId)
        {
            if (slotId == SlotIds.RowerCharacter)
            {
                return RowerCharacterSlot.RealisticPath;
            }

            if (slotId.StartsWith("terrain.layer.", StringComparison.Ordinal))
            {
                return ThirdPartyPack + "TerrainLayers/" + slotId.Substring("terrain.layer.".Length) + ".terrainlayer";
            }

            if (slotId == SlotIds.VegetationGrass || slotId == SlotIds.VegetationFern || slotId == SlotIds.VegetationBush)
            {
                return ThirdPartyPack + "Vegetation/" + slotId.Substring("vegetation.".Length) + ".prefab";
            }

            return "";
        }

        // El presupuesto: triángulos del LOD 0 (props, vegetación, bote) o el lado máximo en px (texturas, capas).
        public static int BudgetFor(string slotId)
        {
            if (slotId.StartsWith("terrain.layer.", StringComparison.Ordinal))
            {
                return 2048;
            }

            switch (slotId)
            {
                case SlotIds.SkyClouds:
                    return 1024;
                case SlotIds.SkyMoon:
                case SlotIds.WaterNormalDetail:
                    return 512;
                case SlotIds.WaterFoam:
                    return 256;
                case SlotIds.RowerCharacter:
                    return RowerCharacterValidation.MaxTriangles;
                case SlotIds.PropMarkerBuoy:
                    return 400;
                case SlotIds.BoatHull:
                    return 6000;
                case SlotIds.VegetationGrass:
                case SlotIds.VegetationFern:
                case SlotIds.VegetationReeds:
                    return 600;
                case SlotIds.VegetationBush:
                    return 1500;
            }

            return slotId.StartsWith("prop.", StringComparison.Ordinal) ? 1200 : 0;
        }

        // Los slots del lago (EnvironmentDefinition) y los del mundo (el cielo, el agua compartida y el bote).
        public static IEnumerable<string> LakeSlotIds()
        {
            yield return SlotIds.ForProp(PropKind.Pine);
            yield return SlotIds.ForProp(PropKind.Cabin);
            yield return SlotIds.ForProp(PropKind.Pier);
            yield return SlotIds.PropRock;
            yield return SlotIds.TerrainGrass;
            yield return SlotIds.TerrainSand;
            yield return SlotIds.TerrainSoil;
            yield return SlotIds.TerrainPebbles;
            yield return SlotIds.TerrainRock;
            yield return SlotIds.TerrainSnow;
            yield return SlotIds.VegetationGrass;
            yield return SlotIds.VegetationFern;
            yield return SlotIds.VegetationBush;
            yield return SlotIds.VegetationReeds;
            yield return SlotIds.VegetationCanopyFar;
            yield return SlotIds.HorizonLake;
            yield return SlotIds.WaterProfileLake;
        }

        public static IEnumerable<string> WorldSlotIds()
        {
            yield return SlotIds.SkyProfile;
            yield return SlotIds.SkyClouds;
            yield return SlotIds.SkyMoon;
            yield return SlotIds.SkyStars;
            yield return SlotIds.WaterNormalDetail;
            yield return SlotIds.WaterFoam;
            yield return SlotIds.BoatHull;
            yield return SlotIds.BoatRigger;
            yield return SlotIds.BoatSeat;
            yield return SlotIds.BoatStretcher;
            yield return SlotIds.BoatOar;
            yield return SlotIds.BoatLivery;
            yield return SlotIds.RowerCharacter;
            yield return SlotIds.RowerOutfit;
            yield return SlotIds.RowerCap;
            yield return SlotIds.FxSplash;
            yield return SlotIds.FxDrips;
            yield return SlotIds.FxRipple;
            yield return SlotIds.PropMarkerBuoy;
        }

        private static T LoadOrCreate<T>(string path, ref int created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            created++;
            return asset;
        }
    }
}
