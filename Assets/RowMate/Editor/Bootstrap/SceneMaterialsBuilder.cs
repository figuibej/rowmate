using System;
using System.Collections.Generic;
using RowMate.Scene;
using RowMate.Scene.Slots;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.EditorTools
{
    // Paso scene.materials del bootstrapper v4 (06a-escena-lago/02-tech-design §1.1, §3.4, §7.2 B1; QA-G6aT-07, -14): los materiales
    // de la escena con los 13 shaders de WP-D1 (Art/Materials/Scene/*.mat: los valores por defecto de los shaders ya son los de Art,
    // SceneShaderContractTests) y la colección de variantes de la sesión (Settings/Scene/SceneShaderVariants.shadervariants): por shader
    // y por pasada (UniversalForward, ShadowCaster, DepthOnly), las palabras clave de cada nivel (las sombras de la luz principal con
    // sus cascadas y su suavizado, el instanciado, RM_WATER_EXTRAS, y en Alto RM_SKY_STARFIELD y RM_CLOUD_LAYER). Los asigna a
    // SceneWorld.asset. Va aparte de SceneAssetsBuilder: B0 no depende de los shaders (QA-G6aT-14). Idempotente.
    internal static class SceneMaterialsBuilder
    {
        public const string MaterialsFolder = "Assets/RowMate/Art/Materials/Scene";
        public const string VariantsFolder = "Assets/RowMate/Settings/Scene";
        public const string VariantsPath = VariantsFolder + "/SceneShaderVariants.shadervariants";

        // Los materiales: nombre del archivo, shader y si se instancia.
        private static readonly (string File, string Shader, bool Instanced)[] Specs =
        {
            ("Sky", SceneShaderIds.Sky, false),
            ("Stars", SceneShaderIds.Stars, true),
            ("Clouds", SceneShaderIds.Clouds, true),
            ("HorizonFar", SceneShaderIds.Horizon, false),
            ("HorizonNear", SceneShaderIds.Horizon, false),
            ("WaterDegraded", SceneShaderIds.WaterDegraded, false),
            ("Terrain", SceneShaderIds.Terrain, false),
            ("Prop", SceneShaderIds.Prop, true),
            ("PropWindow", SceneShaderIds.Prop, true),
            ("Foliage", SceneShaderIds.Foliage, true),
            ("Grass", SceneShaderIds.Grass, true),
            ("Reeds", SceneShaderIds.Reeds, true),
            ("Hull", SceneShaderIds.Hull, false),
            ("BoatShadow", SceneShaderIds.BoatShadow, false),
            ("ScreenOverlay", SceneShaderIds.ScreenOverlay, false),
            // It. 7 (07-remero-palada/02-tech-design §1.3, §7.4).
            ("Rower", SceneShaderIds.Rower, false),
            ("Oar", SceneShaderIds.Oar, false),
            ("Spray", SceneShaderIds.Spray, true),
            ("WaterMarks", SceneShaderIds.WaterMarks, true),
            ("Buoy", SceneShaderIds.Buoy, false),
        };

        public static string MaterialPath(string file)
        {
            return MaterialsFolder + "/" + file + ".mat";
        }

        public static BootstrapState.Step Apply(string id)
        {
            FontAssetBuilder.EnsureFolder(MaterialsFolder);
            FontAssetBuilder.EnsureFolder(VariantsFolder);
            var world = AssetDatabase.LoadAssetAtPath<SceneWorldDefinition>(SceneWorldDefinition.AssetPath);
            if (world == null)
            {
                throw new InvalidOperationException("falta " + SceneWorldDefinition.AssetPath + " (paso scene.assets)");
            }

            var created = 0;
            var materials = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var spec in Specs)
            {
                var shader = Shader.Find(spec.Shader);
                if (shader == null)
                {
                    throw new InvalidOperationException("no se encontró el shader " + spec.Shader + " (WP-D1)");
                }

                var path = MaterialPath(spec.File);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = spec.File };
                    AssetDatabase.CreateAsset(material, path);
                    created++;
                }
                else if (material.shader != shader)
                {
                    material.shader = shader;
                }

                Configure(spec.File, material, spec.Instanced);
                EditorUtility.SetDirty(material);
                materials[spec.File] = material;
            }

            var set = new SceneMaterialSet
            {
                Sky = materials["Sky"],
                Stars = materials["Stars"],
                Clouds = materials["Clouds"],
                HorizonFar = materials["HorizonFar"],
                HorizonNear = materials["HorizonNear"],
                Water = materials["WaterDegraded"],
                Terrain = materials["Terrain"],
                Prop = materials["Prop"],
                PropWindow = materials["PropWindow"],
                Foliage = materials["Foliage"],
                Grass = materials["Grass"],
                Reeds = materials["Reeds"],
                Hull = materials["Hull"],
                BoatShadow = materials["BoatShadow"],
                ScreenOverlay = materials["ScreenOverlay"],
                Rower = materials["Rower"],
                Oar = materials["Oar"],
                Spray = materials["Spray"],
                WaterMarks = materials["WaterMarks"],
                Buoy = materials["Buoy"],
            };

            var variants = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(VariantsPath);
            if (variants == null)
            {
                variants = new ShaderVariantCollection { name = "SceneShaderVariants" };
                AssetDatabase.CreateAsset(variants, VariantsPath);
                created++;
            }

            var count = FillVariants(variants);
            EditorUtility.SetDirty(variants);
            world.SetMaterials(set, variants);
            EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssets();
            return BootstrapState.Step.Ok(id, created + " asset(s) nuevos; " + Specs.Length + " materiales; " + count + " variantes en la colección");
        }

        // Lo propio de cada material (lo demás son los valores por defecto del shader, que son los de Art).
        private static void Configure(string file, Material material, bool instanced)
        {
            material.enableInstancing = instanced;
            switch (file)
            {
                case "HorizonNear":
                    // La cercana: profundidad 0,3, sin nieve, 0,7 del alto (lake_environment.dart:62; HorizonModel).
                    material.SetFloat(SceneShaderIds.HorizonDepth, (float)RowMate.Core.Scene.Art.LakeArt.NearDepth);
                    material.SetFloat(SceneShaderIds.SnowEnabled, 0f);
                    var scale = material.GetVector(SceneShaderIds.HorizonScale);
                    material.SetVector(SceneShaderIds.HorizonScale, new Vector4(scale.x, scale.y, (float)RowMate.Core.Scene.Art.LakeArt.NearHeightScale, 0));
                    break;
                case "HorizonFar":
                    material.SetFloat(SceneShaderIds.HorizonDepth, (float)RowMate.Core.Scene.Art.LakeArt.FarDepth);
                    material.SetFloat(SceneShaderIds.SnowEnabled, 1f);
                    var far = material.GetVector(SceneShaderIds.HorizonScale);
                    material.SetVector(SceneShaderIds.HorizonScale, new Vector4(far.x, far.y, 1f, 0));
                    break;
                case "WaterDegraded":
                    material.EnableKeyword(SceneShaderIds.WaterExtrasKeyword);
                    break;
                case "Prop":
                    material.SetFloat(SceneShaderIds.WindowMode, 0f);
                    break;
                case "PropWindow":
                    material.SetFloat(SceneShaderIds.WindowMode, 1f);
                    break;
                case "Sky":
                    material.DisableKeyword(SceneShaderIds.SkyStarFieldKeyword);
                    break;
                case "Clouds":
                    material.DisableKeyword(SceneShaderIds.CloudLayerKeyword);
                    break;
            }
        }

        // Las combinaciones de las palabras clave de URP que pueden estar prendidas en la sesión (constantes por nivel, QA-G6aT-07).
        private static readonly string[][] ShadowSets =
        {
            new string[0],
            new[] { "_MAIN_LIGHT_SHADOWS" },
            new[] { "_MAIN_LIGHT_SHADOWS_CASCADE" },
            new[] { "_MAIN_LIGHT_SHADOWS", "_SHADOWS_SOFT" },
            new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT" },
            new[] { "_MAIN_LIGHT_SHADOWS", "_SHADOWS_SOFT_LOW" },
            new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT_LOW" },
            new[] { "_MAIN_LIGHT_SHADOWS", "_SHADOWS_SOFT_MEDIUM" },
            new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT_MEDIUM" },
            new[] { "_MAIN_LIGHT_SHADOWS", "_SHADOWS_SOFT_HIGH" },
            new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT_HIGH" },
        };

        private static int FillVariants(ShaderVariantCollection collection)
        {
            collection.Clear();
            var added = 0;
            var names = new List<string>(SceneShaderIds.ShaderNames);
            names.AddRange(SceneShaderIds.RowerShaderNames);
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    continue;
                }

                var own = OwnKeywordSets(name);
                foreach (var ownSet in own)
                {
                    foreach (var shadows in ShadowSets)
                    {
                        foreach (var instancing in new[] { false, true })
                        {
                            var keywords = new List<string>(ownSet);
                            keywords.AddRange(shadows);
                            if (instancing)
                            {
                                keywords.Add("INSTANCING_ON");
                            }

                            added += TryAdd(collection, shader, PassType.ScriptableRenderPipeline, keywords);
                            added += TryAdd(collection, shader, PassType.ScriptableRenderPipelineDefaultUnlit, keywords);
                            if (shadows.Length == 0)
                            {
                                added += TryAdd(collection, shader, PassType.ShadowCaster, keywords);
                            }
                        }
                    }
                }
            }

            return added;
        }

        private static IEnumerable<string[]> OwnKeywordSets(string shader)
        {
            if (shader == SceneShaderIds.WaterDegraded)
            {
                return new[] { new[] { SceneShaderIds.WaterExtrasKeyword } };
            }

            if (shader == SceneShaderIds.Sky)
            {
                return new[] { new string[0], new[] { SceneShaderIds.SkyStarFieldKeyword } };
            }

            if (shader == SceneShaderIds.Clouds)
            {
                return new[] { new string[0], new[] { SceneShaderIds.CloudLayerKeyword } };
            }

            if (shader == SceneShaderIds.Terrain)
            {
                return new[] { new string[0], new[] { SceneShaderIds.TerrainLayersKeyword } };
            }

            return new[] { new string[0] };
        }

        private static int TryAdd(ShaderVariantCollection collection, Shader shader, PassType pass, List<string> keywords)
        {
            try
            {
                var variant = new ShaderVariantCollection.ShaderVariant(shader, pass, keywords.ToArray());
                return collection.Add(variant) ? 1 : 0;
            }
            catch (ArgumentException)
            {
                // Esa combinación no existe para esa pasada (un shader sin sombras, sin instanciado o sin esa pasada).
                return 0;
            }
        }
    }
}
