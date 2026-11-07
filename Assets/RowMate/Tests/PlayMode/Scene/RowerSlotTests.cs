using System.Collections;
using NUnit.Framework;
using RowMate.Scene;
using RowMate.Scene.Slots;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // It. 7 · El slot rower.character (07-remero-palada/02-tech-design §3.2, §6.3; DT7-13; AC-SCN-08-08): sin binding, el maniquí y 0
    // avisos; con Y Bot (si la máquina lo tiene), Y Bot con sus 55 320 triángulos dentro del tope de 60 000; un binding sin modelo (el FBX
    // se borró) o un modelo inválido (sin avatar): el maniquí y UN aviso [SLOT], nunca un error.
    public sealed class RowerSlotTests : SceneTestBase
    {
        [UnityTest, Category("AC-SCN-08-08")]
        public IEnumerator WithoutABindingTheMannequinAndNoWarning()
        {
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            var host = SceneHost;
            var world = host.World!;
            var log = new SlotLog(null);
            var character = RowerCharacterResolver.Resolve(RowerCharacterKind.Auto, null, world.Boat!.Pitch, world.Layer, world.RowerMaterial!, log, false);
            try
            {
                Assert.That(character.FromSlot, Is.False);
                Assert.That(character.Source, Is.EqualTo("maniquí"));
                Assert.That(log.Warnings, Is.EqualTo(0));
                Assert.That(character.Triangles, Is.LessThanOrEqualTo(6000), "el maniquí: ≤ 6 000 triángulos");
            }
            finally
            {
                character.Dispose();
            }

            Assert.That(world.Rower!.FromSlot, Is.False, "el anfitrión de pruebas arma el maniquí por defecto");
            Assert.That(host.SlotLog.Warnings, Is.EqualTo(0));
        }

        [UnityTest, Category("AC-SCN-08-08")]
        public IEnumerator WithYBotTheSlotCharacter()
        {
            CreateScene(DeviceProfile.TV, o => o.SceneCharacter = RowerCharacterKind.Slot);
            yield return EnterFree();
            var rower = SceneHost.World!.Rower!;
            if (!rower.FromSlot)
            {
                Assert.Ignore("NO VERIFICABLE: esta máquina no tiene el FBX de Mixamo (sin binding)");
            }

            Assert.That(rower.Source, Does.EndWith(".fbx"));
            Assert.That(rower.Triangles, Is.LessThanOrEqualTo(RowerCharacterValidation.MaxTriangles));
            Assert.That(rower.Bones.HasFingers, Is.True);
            Assert.That(rower.Spec.Height, Is.InRange(1.55, 2.05));
            Assert.That(SceneHost.SlotLog.Warnings, Is.EqualTo(0));
            Assert.That(SceneHost.Session!.Rower.Height, Is.EqualTo(rower.Spec.Height).Within(1e-9), "la sesión con los largos del rig");
        }

        [UnityTest, Category("AC-SCN-08-08")]
        public IEnumerator ABindingWithoutItsModelFallsBackWithOneWarning()
        {
            var binding = ScriptableObject.CreateInstance<RowerCharacterBinding>();
            binding.Set(null, RowerCharacterSlot.YBotPath);
            try
            {
                CreateScene(DeviceProfile.TV, o => o.SceneCharacter = RowerCharacterKind.Slot);
                SceneHost.Options.CharacterBinding = binding;
                yield return EnterFree();
                SceneHost.ForceCharacterForTest(RowerCharacterKind.Slot);
                H.Step();
                yield return null;
                Assert.That(SceneHost.World!.Rower!.FromSlot, Is.False);
                Assert.That(SceneHost.SlotLog.Warnings, Is.EqualTo(1), string.Join("\n", SceneHost.SlotLog.Lines));
                Assert.That(SceneHost.SlotLog.Lines[0], Does.StartWith("[SLOT] rower.character: "));
            }
            finally
            {
                Object.Destroy(binding);
            }
        }

        [UnityTest, Category("AC-SCN-08-08")]
        public IEnumerator AnInvalidModelFallsBackWithOneWarning()
        {
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            SceneHost.ForceCharacterForTest(RowerCharacterKind.InvalidForTest);
            H.Step();
            yield return null;
            SceneHost.ForceCharacterForTest(RowerCharacterKind.InvalidForTest);
            H.Step();
            yield return null;
            Assert.That(SceneHost.World!.Rower!.FromSlot, Is.False);
            Assert.That(SceneHost.SlotLog.Warnings, Is.EqualTo(1), "un solo aviso por anfitrión: " + string.Join("\n", SceneHost.SlotLog.Lines));
            Assert.That(SceneHost.SlotLog.Lines[0], Does.Contain("Humanoid"));
            Assert.That(SceneHost.Frame.Rower.Pose.Joints[0].Y, Is.GreaterThan(0), "la escena sigue con el maniquí");
        }
    }
}
