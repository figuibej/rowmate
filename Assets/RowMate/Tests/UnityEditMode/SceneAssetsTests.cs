using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.EditorTools;
using RowMate.Scene;
using RowMate.Scene.Slots;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RowMate.Tests.UnityEditMode
{
    // Los assets de la escena que deja el bootstrapper v4 (06a-escena-lago/02-tech-design §1.1, §3.7, §4.2, §6.3 "Unity EditMode solo";
    // QA-G6aT-04, -05, -10, -14): cada SlotIds con su .asset en Slots/ con la fuente Procedural (sin aviso); los tres niveles, los únicos,
    // cada uno con SU asset de URP del proyecto (no una copia) y renderScale 1; el asset por defecto también del proyecto; la capa
    // RowMateScene; Frame Timing Stats; SceneWorld.asset completo; y el contrato de nombres de SceneShaderIds con ShaderArt.
    public sealed class SceneAssetsTests
    {
        [Test, Category("AC-SCN-15-08")]
        public void EverySlotHasItsAssetWithTheProceduralAssigned()
        {
            foreach (var id in SlotIds.All)
            {
                var slot = AssetDatabase.LoadAssetAtPath<AssetSlotDefinition>(SceneAssetsBuilder.SlotPath(id));
                Assert.That(slot, Is.Not.Null, "falta Slots/" + id + ".asset");
                Assert.That(slot!.SlotId, Is.EqualTo(id));
                Assert.That(slot.GetType(), Is.EqualTo(SceneAssetsBuilder.SlotTypeFor(id)), id);
                Assert.That(slot.Source, Is.EqualTo(SlotSource.Procedural), id + ": el procedural asignado (sin el pack)");
                Assert.That(slot.AssignedObject, Is.Null, id);
            }
        }

        [Test, Category("AC-SCN-01-11"), Category("AC-SCN-01-13")]
        public void ThreeTiersEachWithItsProjectUrpAsset()
        {
            Assert.That(QualitySettings.names, Is.EqualTo(new[] { "Low", "Medium", "High" }), "los tres niveles, los únicos");
            var tiers = new[] { QualityTier.Low, QualityTier.Medium, QualityTier.High };
            for (var i = 0; i < tiers.Length; i++)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                Assert.That(asset, Is.Not.Null, tiers[i].ToString());
                Assert.That(AssetDatabase.GetAssetPath(asset), Is.EqualTo(UrpProvisioner.PipelinePath(tiers[i])), "su asset del proyecto, no una copia");
                Assert.That(AssetDatabase.Contains(asset), Is.True);
                Assert.That(asset!.renderScale, Is.EqualTo(1f), "la resolución va en la RT de la escena (DT6a-16)");
            }

            var high = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpProvisioner.PipelinePath(QualityTier.High));
            Assert.That(high.msaaSampleCount, Is.EqualTo(4));
            Assert.That(high.shadowDistance, Is.EqualTo(80f));
            Assert.That(high.shadowCascadeCount, Is.EqualTo(2));
            var medium = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpProvisioner.PipelinePath(QualityTier.Medium));
            Assert.That(medium.msaaSampleCount, Is.EqualTo(2));
            Assert.That(medium.shadowDistance, Is.EqualTo(40f));
            var low = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpProvisioner.PipelinePath(QualityTier.Low));
            Assert.That(low.supportsMainLightShadows, Is.False, "Bajo: sin mapa (el óvalo del bote)");
            Assert.That(low.supportsHDR, Is.False);
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(high), "el asset por defecto: el de Alto, del proyecto");
            Assert.That(AssetDatabase.Contains(GraphicsSettings.defaultRenderPipeline), Is.True);
            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(2), "el Editor (Windows) en Alto");
        }

        [Test, Category("AC-SCN-01-13")]
        public void SceneLayerAndFrameTimingStats()
        {
            var layer = LayerMask.NameToLayer(SceneShaderIds.LayerName);
            Assert.That(layer, Is.GreaterThanOrEqualTo(8), "la capa RowMateScene");
            Assert.That(SceneWorldBuilder.SceneLayer(), Is.EqualTo(layer));
            Assert.That(PlayerSettings.enableFrameTimingStats, Is.True, "FrameTimingManager (ThermalAdapter, [PERF])");
        }

        [Test, Category("AC-SCN-15-08"), Category("AC-SCN-21-01")]
        public void SceneWorldIsComplete()
        {
            var world = AssetDatabase.LoadAssetAtPath<SceneWorldDefinition>(SceneWorldDefinition.AssetPath);
            Assert.That(world, Is.Not.Null);
            Assert.That(world!.HasSlotsAndTiers, Is.True);
            Assert.That(world.Lake!.Environment, Is.EqualTo(EnvironmentId.Lake), "en la 6a, siempre el lago");
            foreach (var id in SceneAssetsBuilder.LakeSlotIds())
            {
                Assert.That(world.Lake.Find(id), Is.Not.Null, "Lake: " + id);
            }

            foreach (var id in SceneAssetsBuilder.WorldSlotIds())
            {
                Assert.That(world.Find(id), Is.Not.Null, "SceneWorld: " + id);
            }

            foreach (QualityTier tier in Enum.GetValues(typeof(QualityTier)))
            {
                var profile = world.Profile(tier);
                Assert.That(profile, Is.Not.Null, tier.ToString());
                Assert.That(profile!.Tier, Is.EqualTo(tier));
                Assert.That(AssetDatabase.GetAssetPath(profile.UrpAsset), Is.EqualTo(UrpProvisioner.PipelinePath(tier)));
            }

            Assert.That(world.Profile(QualityTier.High)!.Resolution, Is.EqualTo(1).Within(1e-6));
            Assert.That(world.Profile(QualityTier.Medium)!.Resolution, Is.EqualTo(0.85).Within(1e-6));
            Assert.That(world.Profile(QualityTier.Low)!.Resolution, Is.EqualTo(0.75).Within(1e-6));
            Assert.That(world.Profile(QualityTier.Low)!.GrassEnd, Is.EqualTo(MaterialArt.GrassEndLow).Within(1e-6));
        }

        // QA-G6aT-01: SceneShaderIds tiene TODO el contrato de 02-tech-design §7.5 (con lo que precisó WP-D1): los mismos nombres que
        // ShaderArt (los que SceneShaderContractTests compara con los .shader).
        [Test, Category("AC-SCN-01-14")]
        public void ShaderIdsAreTheContract()
        {
            Assert.That(SceneShaderIds.ShaderNames, Is.EquivalentTo(ShaderArt.Scene.Select(s => s.FullName)));
            Assert.That(SceneShaderIds.GlobalNames, Is.EquivalentTo(ShaderArt.Globals));
            Assert.That(SceneShaderIds.Keywords, Is.EquivalentTo(ShaderArt.Keywords));
            var material = new HashSet<string>(ShaderArt.Scene.SelectMany(s => s.Properties));
            Assert.That(SceneShaderIds.MaterialPropertyNames, Is.EquivalentTo(material));
            var instance = new HashSet<string>(ShaderArt.Scene.SelectMany(s => s.InstanceProperties));
            Assert.That(SceneShaderIds.InstancePropertyNames, Is.EquivalentTo(instance));
        }
    }

    // Los materiales y la colección de variantes de la escena (06a-escena-lago/02-tech-design §3.4, §6.3; QA-G6aT-07, -14): los 13
    // shaders del contrato encontrados, sin errores y con su cola; los 15 materiales de Art/Materials/Scene con su shader, el instanciado
    // donde corresponde y RM_WATER_EXTRAS en el agua; la colección cubre los 13. (La compilación para Direct3D 11, GLCore, Vulkan y GLES 3
    // la afirma SceneShaderTests de WP-D1.)
    public sealed class SceneMaterialsTests
    {
        [Test, Category("AC-SCN-01-14")]
        public void TheThirteenShadersWithTheirQueues()
        {
            foreach (var spec in ShaderArt.Scene)
            {
                var shader = Shader.Find(spec.FullName);
                Assert.That(shader, Is.Not.Null, spec.FullName);
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False, spec.FullName);
                Assert.That(shader.renderQueue, Is.EqualTo(spec.Queue), spec.FullName);
            }
        }

        [Test, Category("AC-SCN-01-14")]
        public void TheMaterialsOfTheScene()
        {
            var world = AssetDatabase.LoadAssetAtPath<SceneWorldDefinition>(SceneWorldDefinition.AssetPath);
            Assert.That(world!.Materials.IsComplete, Is.True);
            Assert.That(world.IsComplete, Is.True);
            var m = world.Materials;
            Assert.That(m.Sky!.shader.name, Is.EqualTo(SceneShaderIds.Sky));
            Assert.That(m.Stars!.enableInstancing, Is.True);
            Assert.That(m.Clouds!.enableInstancing, Is.True);
            Assert.That(m.Foliage!.enableInstancing, Is.True);
            Assert.That(m.Grass!.enableInstancing, Is.True);
            Assert.That(m.Reeds!.enableInstancing, Is.True);
            Assert.That(m.Prop!.enableInstancing, Is.True);
            Assert.That(m.Glow!.shader.name, Is.EqualTo(SceneShaderIds.Glow), "It. 8");
            Assert.That(m.Glow.enableInstancing, Is.True);
            Assert.That(m.Foam!.shader.name, Is.EqualTo(SceneShaderIds.Foam), "It. 8");
            Assert.That(m.Foam.enableInstancing, Is.True);
            Assert.That(m.Water!.IsKeywordEnabled(SceneShaderIds.WaterExtrasKeyword), Is.True, "DT6a-07 aceptado");
            Assert.That(m.Sky.IsKeywordEnabled(SceneShaderIds.SkyStarFieldKeyword), Is.False, "el .mat sin lo de Alto: lo prende la copia del nivel");
            Assert.That(m.PropWindow!.GetFloat(SceneShaderIds.WindowMode), Is.EqualTo(1f));
            Assert.That(m.HorizonNear!.GetFloat(SceneShaderIds.SnowEnabled), Is.EqualTo(0f));
            Assert.That(m.HorizonNear.GetVector(SceneShaderIds.HorizonScale).z, Is.EqualTo((float)LakeArt.NearHeightScale).Within(1e-6));
            foreach (var material in m.All())
            {
                Assert.That(AssetDatabase.GetAssetPath(material), Does.StartWith(SceneMaterialsBuilder.MaterialsFolder + "/"));
            }
        }

        [Test, Category("AC-SCN-01-14")]
        public void TheVariantCollectionCoversTheThirteen()
        {
            var world = AssetDatabase.LoadAssetAtPath<SceneWorldDefinition>(SceneWorldDefinition.AssetPath);
            var variants = world!.Variants;
            Assert.That(variants, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(variants), Is.EqualTo(SceneMaterialsBuilder.VariantsPath));
            // It. 7: más los 5 del remero (Rower, Oar, Spray, WaterMarks, Buoy; SceneShaderIds.RowerShaderNames); It. 8: Glow.
            Assert.That(variants!.shaderCount, Is.EqualTo(ShaderArt.Scene.Length + RowMate.Scene.SceneShaderIds.RowerShaderNames.Length
                + RowMate.Scene.SceneShaderIds.EnvironmentShaderNames.Length + RowMate.Scene.SceneShaderIds.WaterShaderNames.Length),
                "los 13, los 5 de la 7, los de la 8 y los de la 6b");
            Assert.That(variants.variantCount, Is.GreaterThanOrEqualTo(ShaderArt.Scene.Length * 2));
        }
    }
}
