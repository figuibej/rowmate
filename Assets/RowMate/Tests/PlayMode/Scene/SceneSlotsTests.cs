using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Scene.Slots;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // SSL · Los slots (06a-escena-lago/02-tech-design §3.7; AC-SCN-15-08; QA-G6aT-04): con todos los slots en el procedural (sin el pack,
    // hoy) la escena arranca y entrena sin un solo [SLOT]; un slot de prueba (una capa de terreno asignada en una copia en memoria del
    // escenario) entra al Texture2DArray del shader de terreno; quitado (la fuente Asset con la referencia vacía, como un GUID de
    // Assets/ThirdParty/ sin su archivo), vuelve el procedural con UN aviso; el registro de avisos es de cada anfitrión.
    [Category("GPU")]
    public sealed class SceneSlotsTests : SceneTestBase
    {
        [UnityTest, Category("AC-SCN-15-08"), Category("AC-SCN-01-05")]
        public IEnumerator AllProceduralMeansNoWarnings()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            Assert.That(SceneHost.SlotLog.Warnings, Is.EqualTo(0));
            foreach (var resolution in SceneHost.World!.Slots)
            {
                Assert.That(resolution.UsesProcedural, Is.True, resolution.SlotId);
                Assert.That(resolution.Reason, Is.Empty, resolution.SlotId + ": el procedural asignado no es una falla");
            }

            Assert.That(SceneHost.World.Terrain!.IsKeywordEnabled("RM_TERRAIN_LAYERS"), Is.False, "sin el pack, el terreno procedural");
            AssertClean(H);
        }

        [UnityTest, Category("AC-SCN-15-08")]
        public IEnumerator TheTestLayerEntersAndLeaves()
        {
            AssumeGpu();
            var world = UiTestHost.LoadSceneWorld();
            var lake = Object.Instantiate(world.Lake!);
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, true) { name = "test-terrain-layer" };
            var layer = new TerrainLayer { diffuseTexture = texture };
            var slot = ScriptableObject.CreateInstance<TerrainLayerSlot>();
            slot.Configure(SlotIds.TerrainGrass, "Assets/ThirdParty/TerrainSampleAssetPack/TerrainLayers/grass.terrainlayer", 2048);
            slot.AssignObject(layer);
            lake.Replace(slot);
            var withLayer = world.WithLake(lake);
            CreateScene(DeviceProfile.TV, o => o.SceneWorld = withLayer);
            yield return EnterFree();
            Assert.That(SceneHost.World!.LayerAlbedo, Is.Not.Null, "la capa entró al Texture2DArray");
            Assert.That(SceneHost.World.Terrain!.IsKeywordEnabled("RM_TERRAIN_LAYERS"), Is.True, "el shader la usa");
            Assert.That(SceneHost.SlotLog.Warnings, Is.EqualTo(0));

            // Quitado: la fuente Asset con la referencia vacía (lo que deja un .meta de Assets/ThirdParty/ sin su archivo).
            slot.AssignObject(null);
            CreateScene(DeviceProfile.TV, o => o.SceneWorld = withLayer);
            yield return EnterFree();
            Assert.That(SceneHost.World!.LayerAlbedo, Is.Null, "el procedural");
            Assert.That(SceneHost.SlotLog.Warnings, Is.EqualTo(1), "un solo aviso");
            Assert.That(SceneHost.SlotLog.Lines[0], Does.StartWith("[SLOT] " + SlotIds.TerrainGrass + ": "));

            // Un SlotLog por anfitrión: otro anfitrión avisa otra vez (no depende del orden de las pruebas).
            CreateScene(DeviceProfile.TV, o => o.SceneWorld = withLayer);
            yield return EnterFree();
            Assert.That(SceneHost.SlotLog.Warnings, Is.EqualTo(1));
            Object.Destroy(withLayer);
            Object.Destroy(lake);
            Object.Destroy(slot);
            Object.Destroy(texture);
        }
    }
}
