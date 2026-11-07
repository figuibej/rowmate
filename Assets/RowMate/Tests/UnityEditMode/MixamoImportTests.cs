using System.Globalization;
using NUnit.Framework;
using RowMate.EditorTools;
using RowMate.Scene;
using RowMate.Scene.Slots;
using UnityEditor;
using UnityEngine;

namespace RowMate.Tests.UnityEditMode
{
    // It. 7 · El import de Mixamo y el slot del remero (07-remero-palada/02-tech-design §3.2, §6.3; DT7-04, DT7-13; AC-SCN-08-08, -09):
    // con YBot.fbx en Assets/ThirdParty/Mixamo/YBot/ (lo baja el usuario; nunca se versiona): Humanoid con el avatar propio, sin Optimize
    // Game Objects, legible, sin animaciones ni materiales; valida para el slot (55 320 triángulos, 65 huesos, 30 dedos, alto 1,55–2,05 m)
    // y el binding generado lo apunta. Sin el FBX (el CI, otro clon): Ignore = NO VERIFICABLE. El slot versionado queda en Procedural.
    public sealed class MixamoImportTests
    {
        private static GameObject YBotOrIgnore()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RowerCharacterSlot.YBotPath);
            if (model == null)
            {
                Assert.Ignore("NO VERIFICABLE: falta " + RowerCharacterSlot.YBotPath + " (Y Bot de Mixamo, lo baja el usuario)");
            }

            return model!;
        }

        [Test, Category("AC-SCN-08-08")]
        public void YBotImportsAsHumanoidReadableAndWithoutOptimizeGameObjects()
        {
            YBotOrIgnore();
            var importer = (ModelImporter)AssetImporter.GetAtPath(RowerCharacterSlot.YBotPath);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(importer.avatarSetup, Is.EqualTo(ModelImporterAvatarSetup.CreateFromThisModel));
            Assert.That(importer.optimizeGameObjects, Is.False);
            Assert.That(importer.isReadable, Is.True);
            Assert.That(importer.importAnimation, Is.False);
            Assert.That(importer.materialImportMode, Is.EqualTo(ModelImporterMaterialImportMode.None));
            Assert.That(MixamoImportPostprocessor.IsConfigured(importer), Is.True);
        }

        [Test, Category("AC-SCN-08-08")]
        public void YBotValidatesForTheSlot()
        {
            var model = YBotOrIgnore();
            var ok = RowerCharacterValidation.Validate(model, out var reason, out var stats);
            Debug.Log("[SLOT] Y Bot: Humanoid " + stats.Humanoid + ", dedos " + stats.FingerBones + ", alto " + F(stats.HeightM) + " m, "
                      + stats.Triangles + " triángulos, " + stats.Bones + " huesos, legible " + stats.Readable);
            Assert.That(ok, Is.True, reason);
            Assert.That(stats.Triangles, Is.EqualTo(55320), "QA7-G-01");
            // El FBX tiene 65 huesos; la piel usa 52 (los extremos _End y algunos de la cara no pesan vértices): el tope es 75.
            Assert.That(stats.Bones, Is.LessThanOrEqualTo(65), "QA7-G-01");
            Assert.That(stats.FingerBones, Is.EqualTo(30));
        }

        // El informe de B0 (DT7-03): los largos de hueso de Y Bot en T-pose (RowerRigDriver.MeasureSpec), en el registro de la corrida.
        [Test]
        public void YBotBoneLengthsAreMeasured()
        {
            var model = YBotOrIgnore();
            var instance = Object.Instantiate(model);
            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var bones = RowerBones.Find(instance.transform);
                Assert.That(bones.Missing(), Is.Empty);
                Assert.That(bones.HasFingers, Is.True, "los 15 dedos del agarre por mano");
                var s = RowerRigDriver.MeasureSpec(bones);
                Debug.Log("[SLOT] Y Bot (m): alto " + F(s.Height) + ", cadera ±" + F(s.HipHalfWidth) + ", Hips sobre las articulaciones " + F(s.HipsAboveJoints)
                          + ", pelvis " + F(s.PelvisLength) + ", Spine " + F(s.SpineLength) + ", Spine1 " + F(s.Spine1Length) + ", Spine2 " + F(s.Spine2Length)
                          + ", hombros caen " + F(s.ShoulderDrop) + ", clavícula ±" + F(s.ClavicleHalfWidth) + ", hombros ±" + F(s.ShoulderHalfWidth)
                          + ", cuello " + F(s.NeckLength) + ", cabeza " + F(s.HeadTopAbove) + ", brazo " + F(s.UpperArm) + ", antebrazo " + F(s.Forearm)
                          + ", muñeca-agarre " + F(s.HandToGrip) + ", muslo " + F(s.Thigh) + ", pierna " + F(s.Shin) + ", tobillo " + F(s.AnkleHeight)
                          + ", pie " + F(s.FootLength) + ", pies ±" + F(s.FootHalfSpread) + "; tronco " + F(s.TrunkLength) + ", alcance " + F(s.ArmReach)
                          + "; alto medido de los pies a HeadTop_End " + F(RowerCharacterValidation.Height(instance.transform)));
                Assert.That(s.Height, Is.InRange(RowerCharacterValidation.MinHeight, RowerCharacterValidation.MaxHeight));
                Assert.That(s.UpperArm, Is.GreaterThan(0.2).And.LessThan(0.45));
                Assert.That(s.Thigh, Is.GreaterThan(0.3).And.LessThan(0.6));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test, Category("AC-SCN-08-09")]
        public void TheVersionedSlotStaysProceduralAndTheBindingFollowsTheMachine()
        {
            var slot = AssetDatabase.LoadAssetAtPath<RowerCharacterSlot>(SceneAssetsBuilder.SlotPath("rower.character"));
            Assert.That(slot, Is.Not.Null, "Slots/rower.character.asset (RowerCharacterSlot)");
            Assert.That(slot!.Source, Is.EqualTo(SlotSource.Procedural));
            Assert.That(slot.AssignedObject, Is.Null);
            Assert.That(slot.ExpectedPaths, Is.EqualTo(RowerCharacterSlot.DefaultPaths));
            var binding = AssetDatabase.LoadAssetAtPath<RowerCharacterBinding>(RowerCharacterBinding.AssetPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RowerCharacterSlot.YBotPath)
                        ?? AssetDatabase.LoadAssetAtPath<GameObject>(RowerCharacterSlot.RealisticPath);
            if (model == null)
            {
                Assert.That(binding, Is.Null, "sin FBX de Mixamo no hay binding");
            }
            else
            {
                Assert.That(binding, Is.Not.Null, "con un FBX de Mixamo, el binding generado");
                Assert.That(binding!.Model, Is.Not.Null);
            }
        }

        private static string F(double value)
        {
            return value.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }
}
