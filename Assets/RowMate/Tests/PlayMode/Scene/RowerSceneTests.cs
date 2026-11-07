using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Scene;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // It. 7 · El remero en la escena (07-remero-palada/02-tech-design §3.1, §6.3): LT-97/98 (todas las fases con 3 charcos, en vertical y
    // en horizontal, sin excepciones ni avisos, con el maniquí y con Y Bot si está), las piezas del bote (la jerarquía, el remero bajo el
    // marco del bote, los remos con la pala en su lugar), el modo de sombra por hora y nivel (DT7-08) y la sesión que no cambia con la
    // escena (AC-SCN-08-11: la misma foto del motor con y sin escena).
    public sealed class RowerSceneTests : SceneTestBase
    {
        private static readonly double[] LegacyPhases = { 0, 0.05, 0.2, 0.36, 0.39, 0.5, 0.8, 0.99 };

        [UnityTest, Category("AC-SCN-08-07"), Category("LT-97"), Category("LT-98")]
        public IEnumerator EveryPhaseDrawsWithThreePuddles([Values(390, 844)] int width,
                                                           [Values(RowerCharacterKind.Mannequin, RowerCharacterKind.Slot)] RowerCharacterKind kind)
        {
            AssumeGpu();
            var profile = width == 390 ? DeviceProfile.V390 : DeviceProfile.H844;
            CreateScene(profile, o => o.SceneCharacter = kind);
            yield return EnterFree();
            var host = SceneHost;
            if (kind == RowerCharacterKind.Slot && !host.World!.Rower!.FromSlot)
            {
                Assert.Ignore("NO VERIFICABLE: esta máquina no tiene el FBX de Mixamo (sin binding)");
            }

            yield return SceneAlone(0, 12);
            host.InjectPuddleForTest(-3.0, 6.3, 1);
            host.InjectPuddleForTest(3.0, 6.3, 3.5);
            host.InjectPuddleForTest(3.0, 0.2, 2);
            foreach (var c in LegacyPhases)
            {
                host.ForceCanonicalForTest(c);
                H.Step();
                yield return null;
                var frame = Read();
                Dump(frame, "lt97-" + width + "-" + kind + "-" + c);
                Destroy(frame);
                Assert.That(host.Frame.Rower.Puddles.Count, Is.GreaterThanOrEqualTo(3), "los 3 charcos inyectados");
            }

            // El de z 0,2 queda detrás de la cámara (relZ ≤ 0,5): no se dibuja; los otros dos sí (más los de las paladas de la entrada).
            Assert.That(host.World!.Director!.WaterMarks!.Puddles, Is.GreaterThanOrEqualTo(2));
            host.ForceCanonicalForTest(null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Category("AC-SCN-08-01"), Category("AC-SCN-06-01")]
        public IEnumerator TheBoatIsAHierarchyAroundThePins()
        {
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            yield return AdvanceTo(10);
            var world = SceneHost.World!;
            var boat = world.Boat!;
            Assert.That(boat.Root.name, Is.EqualTo("scene-boat-root"));
            Assert.That(boat.Pitch.parent, Is.SameAs(boat.Root));
            Assert.That(world.Rower!.Root.transform.parent, Is.SameAs(boat.Pitch), "el remero cabecea con el bote");
            Assert.That(boat.Hull.transform.parent, Is.SameAs(boat.Pitch));
            Assert.That(boat.Shadow.transform.parent, Is.SameAs(boat.Root), "el óvalo no cabecea");
            Assert.That(world.Fittings!.vertexCount, Is.GreaterThan(0));
            var rower = SceneHost.Frame.Rower;
            Assert.That(boat.Root.localPosition.z, Is.EqualTo((float)(SceneCamera.BoatZ + rower.Boat.SurgeM)).Within(1e-4f));
            // La pala del remo de la izquierda, en el centro de pala de Core (± 3 cm: el cruce corre el puño, no el barrido).
            var oars = world.Director!.Oars!;
            foreach (var (oar, state) in new[] { (oars.Left, rower.Pose.Left), (oars.Right, rower.Pose.Right) })
            {
                var blade = boat.Pitch.InverseTransformPoint(oar.TransformPoint(new Vector3((float)BoatSpec.Real.BladeFromPin, 0, 0)));
                var core = new Vector3((float)state.BladeCenter.X, (float)state.BladeCenter.Y, (float)state.BladeCenter.Z);
                Assert.That(Vector3.Distance(blade, core), Is.LessThan(0.03f), oar.name + ": pala en " + blade + ", Core " + core);
            }

            // El carro en SeatPosition.
            var seat = rower.Pose.SeatPosition;
            Assert.That(Vector3.Distance(boat.Seat.transform.localPosition, new Vector3((float)seat.X, (float)seat.Y, (float)seat.Z)), Is.LessThan(1e-4f));
        }

        [UnityTest, Category("AC-SCN-08-06")]
        public IEnumerator TheShadowIsTheMapOrTheOvalNeverBoth([Values(QualityTier.High, QualityTier.Medium, QualityTier.Low)] QualityTier tier)
        {
            using (new TierScope(tier))
            {
                CreateScene(DeviceProfile.TH, o => o.SceneTier = tier);
                yield return EnterFree();
                foreach (var hour in new[] { 9, 12, 17, 22 })
                {
                    SceneHost.ForceHourForTest(hour);
                    H.Step();
                    H.Step();
                    yield return null;
                    var rower = SceneHost.Frame.Rower;
                    var oval = SceneHost.World!.Boat!.Shadow.enabled;
                    Assert.That(oval, Is.EqualTo(rower.ShadowMode == BoatShadowMode.Oval), tier + " " + hour + " h: el óvalo solo sin mapa");
                    if (tier == QualityTier.Low)
                    {
                        Assert.That(rower.ShadowMode, Is.EqualTo(BoatShadowMode.Oval), "Bajo: siempre el óvalo");
                    }

                    if (tier == QualityTier.Medium && hour == 22)
                    {
                        Assert.That(rower.ShadowMode, Is.EqualTo(BoatShadowMode.Oval), "Medio de noche: sin fuerza de sombra, el óvalo");
                    }

                    if (tier == QualityTier.High && hour == 12)
                    {
                        Assert.That(rower.ShadowMode, Is.EqualTo(BoatShadowMode.Map), "Alto al mediodía: el mapa");
                    }

                    Assert.That(Math.Abs(rower.ShadowSide), Is.LessThanOrEqualTo(BoatShadowRule.OvalOffset + 1e-9));
                }
            }
        }

        // AC-SCN-08-11: la sesión R-4 da la misma foto del motor con la escena real que sin escena (el remero, el surge y los efectos son
        // de dibujo).
        [UnityTest, Category("AC-SCN-08-11")]
        public IEnumerator TheSessionIsTheSameWithAndWithoutTheScene()
        {
            CreateScene(DeviceProfile.TV);
            yield return StartFree();
            yield return AdvanceTo(30.2);
            var with = Describe(H.App.Workout.State.Value);
            Assert.That(SceneHost.Frame.Rower.Spray, Is.Not.Null, "la escena corrió con el remero");
            CreateWorkout(null, o => o.Profile = DeviceProfile.TV);
            yield return StartFree();
            yield return AdvanceTo(30.2);
            var without = Describe(H.App.Workout.State.Value);
            Assert.That(H.Scene, Is.Null);
            Assert.That(with, Is.EqualTo(without));
        }

        private static string Describe(RowMate.Core.Workout.WorkoutSnapshot snapshot)
        {
            return snapshot.Phase + " " + snapshot.TotalElapsedSeconds + " s " + snapshot.Data;
        }
    }
}
