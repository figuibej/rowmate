using System.IO;
using NUnit.Framework;
using RowMate.Bootstrap;
using RowMate.Core.Scene;
using RowMate.EditorTools;
using RowMate.Scene.Slots;
using UnityEditor;
using UnityEngine;

namespace RowMate.Tests.UnityEditMode
{
    // Los assets de los cuatro escenarios y de las miniaturas que dejan el bootstrapper v6 y el horneado (08-entornos-selector/02-tech-design
    // §3.1, §3.4, §7.3; AC-SCN-20-07, AC-SCN-22-04, -05): River, Coast y Regatta.asset en SceneWorld con sus slots en Procedural, y las 4
    // miniaturas de 456 × 288 asignadas a UiAssets, sin mipmaps, en Clamp y Bilinear, que juntas pesan ≤ 1 MB en el build.
    public sealed class EnvironmentAssetsTests
    {
        [Test, Category("AC-SCN-20-07")]
        public void TheFourEnvironmentsHaveTheirAssetsWithProceduralSlots()
        {
            var world = AssetDatabase.LoadAssetAtPath<SceneWorldDefinition>(SceneWorldDefinition.AssetPath);
            Assert.That(world, Is.Not.Null);
            foreach (var id in EnvironmentCatalog.All)
            {
                var definition = world!.Environment(id);
                Assert.That(definition, Is.Not.Null, "falta el escenario " + id);
                Assert.That(definition!.Environment, Is.EqualTo(id));
                if (id != EnvironmentId.Lake)
                {
                    Assert.That(AssetDatabase.GetAssetPath(definition), Is.EqualTo(SceneAssetsBuilder.EnvironmentPath(id)));
                    foreach (var slotId in SceneAssetsBuilder.EnvironmentSlotIds(id))
                    {
                        var slot = definition.Find(slotId);
                        Assert.That(slot, Is.Not.Null, id + ": " + slotId);
                        Assert.That(slot!.Source, Is.EqualTo(SlotSource.Procedural), id + ": " + slotId);
                    }
                }
            }
        }

        [Test, Category("AC-SCN-22-04"), Category("AC-SCN-22-05")]
        public void TheBakedThumbnailsAreAssignedAndLight()
        {
            var assets = AssetDatabase.LoadAssetAtPath<UiAssets>(UiAssets.AssetPath);
            Assert.That(assets, Is.Not.Null);
            long bytes = 0;
            foreach (var id in EnvironmentCatalog.All)
            {
                var texture = assets!.Thumbnails[(int)id];
                Assert.That(texture, Is.Not.Null, "falta la miniatura de " + id + " (tools/unity/bake-thumbnails.ps1)");
                Assert.That(AssetDatabase.GetAssetPath(texture), Is.EqualTo(ThumbnailAssets.PathFor(id)));
                Assert.That(texture!.width, Is.EqualTo(ThumbnailRule.Width));
                Assert.That(texture.height, Is.EqualTo(ThumbnailRule.Height));
                var importer = (TextureImporter)AssetImporter.GetAtPath(ThumbnailAssets.PathFor(id));
                Assert.That(importer.mipmapEnabled, Is.False, id + ": sin mipmaps");
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), id.ToString());
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), id.ToString());
                // En el build de escritorio: BC7, 1 byte por píxel (sin mipmaps).
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden && standalone.format == TextureImporterFormat.BC7, Is.True, id + ": BC7 en escritorio");
                bytes += (long)texture.width * texture.height;
            }

            Assert.That(bytes, Is.LessThanOrEqualTo(1024 * 1024), "las 4 miniaturas ≤ 1 MB");
            Assert.That(File.Exists(ThumbnailAssets.PathFor(EnvironmentId.Lake).Replace(".png", "").Replace("lake", "thumbnails") + ".json"), Is.True);
        }
    }
}
