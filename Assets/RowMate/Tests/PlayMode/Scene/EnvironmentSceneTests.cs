using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Geometry;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // ENV · Los cuatro escenarios en la escena real (08-entornos-selector/02-tech-design §3, §7.3; AC-SCN-21-05, AC-SCN-01-18, AC-SCN-20-07):
    // con scene.environment forzado, el kit y la sesión son de ese escenario, la escena se dibuja con los objetos del kit, sin un solo
    // [SLOT] (todo en Procedural) y sin errores; cambiar de escenario en reposo rehace solo el kit (el mundo común es el mismo) y la
    // sesión siguiente usa el nuevo; la ola de la costa mueve el bote y la distancia no cambia.
    [Category("GPU")]
    public sealed class EnvironmentSceneTests : SceneTestBase
    {
        private static readonly EnvironmentId[] Environments = { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };

        private UiTestHost CreateIn(EnvironmentId id, DeviceProfile? profile = null)
        {
            var preferences = new MemoryPreferences();
            if (id != EnvironmentId.Lake)
            {
                preferences.SetString(EnvironmentKeys.PreferenceKey, EnvironmentKeys.ToKey(id));
            }

            return CreateScene(profile ?? DeviceProfile.TV, o => o.Preferences = preferences);
        }

        [UnityTest, Category("AC-SCN-21-05"), Category("AC-SCN-20-07")]
        public IEnumerator EachEnvironmentBuildsAndDraws([ValueSource(nameof(Environments))] EnvironmentId id)
        {
            AssumeGpu();
            CreateIn(id);
            yield return EnterFree();
            Assert.That(SceneHost.KitEnvironment, Is.EqualTo(id), "el kit armado");
            Assert.That(SceneHost.Session!.Environment.Id, Is.EqualTo(id), "la sesión del inmersivo");
            Assert.That(SceneHost.IsShowing, Is.True);
            Assert.That(SceneHost.SlotLog.Warnings, Is.EqualTo(0), "todos los slots en Procedural");
            yield return SceneAlone(id == EnvironmentId.Lake ? 920 : id == EnvironmentId.River ? 250 : id == EnvironmentId.Coast ? 500 : 180, 12);
            if (id != EnvironmentId.Lake)
            {
                Assert.That(SceneHost.World!.Director!.Kit!.SegmentsDrawn, Is.GreaterThan(0), "los objetos del kit a la vista");
                Assert.That(SceneHost.World.HorizonStrip, Is.Not.Null, "la silueta lejana del escenario");
            }

            Assert.That(SceneHost.Frame.Environment, Is.EqualTo(id));
            AssertClean(H);
        }

        [UnityTest, Category("AC-SCN-01-18"), Category("AC-SCN-21-05")]
        public IEnumerator SwitchingInRestRebuildsOnlyTheKit()
        {
            AssumeGpu();
            CreateIn(EnvironmentId.Lake);
            yield return GoToWorkout();
            SceneHost.CompleteLoadingForTest(H.Scheduler.MonotonicSeconds);
            yield return Frames(2);
            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(RowMate.Scene.SceneReadiness.Ready));
            var world = SceneHost.World;
            H.App.SceneEnvironment.Select(EnvironmentId.Coast);
            yield return Frames(2);
            SceneHost.CompleteLoadingForTest(H.Scheduler.MonotonicSeconds);
            yield return Frames(2);
            Assert.That(SceneHost.KitEnvironment, Is.EqualTo(EnvironmentId.Coast));
            Assert.That(SceneHost.KitSwitches, Is.EqualTo(1));
            Assert.That(SceneHost.World, Is.SameAs(world), "la base común no se rehace");
            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(RowMate.Scene.SceneReadiness.Ready));
            yield return EnterFree();
            Assert.That(SceneHost.Session!.Environment.Id, Is.EqualTo(EnvironmentId.Coast));
            AssertClean(H);
        }

        // QA-V8-08 (AC-SCN-01-18): 10 cambios de escenario en reposo (río → costa → regata → lago …) dejan la memoria como después
        // del primero ± 5 %: las mallas vivas (Unity) y la memoria administrada; el mundo común sigue siendo el mismo.
        [UnityTest, Category("AC-SCN-01-18")]
        public IEnumerator TenSwitchesKeepTheMemoryFlat()
        {
            AssumeGpu();
            CreateIn(EnvironmentId.Lake);
            yield return GoToWorkout();
            SceneHost.CompleteLoadingForTest(H.Scheduler.MonotonicSeconds);
            yield return Frames(2);
            var world = SceneHost.World;
            var cycle = new[] { EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta, EnvironmentId.Lake };
            long baseManaged = 0;
            var baseMeshes = 0;
            for (var i = 0; i < 10; i++)
            {
                H.App.SceneEnvironment.Select(cycle[i % cycle.Length]);
                yield return Frames(2);
                SceneHost.CompleteLoadingForTest(H.Scheduler.MonotonicSeconds);
                yield return Frames(2);
                Assert.That(SceneHost.KitEnvironment, Is.EqualTo(cycle[i % cycle.Length]));
                if (i == 3)
                {
                    // Después de la primera vuelta completa (los cuatro armados una vez): la referencia.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    baseManaged = GC.GetTotalMemory(true);
                    baseMeshes = Resources.FindObjectsOfTypeAll<Mesh>().Length;
                }
            }

            // 10 cambios: termina en la costa (índice 9 → Coast); se vuelve al lago para comparar con la referencia (también en el lago).
            H.App.SceneEnvironment.Select(EnvironmentId.Lake);
            yield return Frames(2);
            SceneHost.CompleteLoadingForTest(H.Scheduler.MonotonicSeconds);
            yield return Frames(2);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var managed = GC.GetTotalMemory(true);
            var meshes = Resources.FindObjectsOfTypeAll<Mesh>().Length;
            TestContext.WriteLine("memoria administrada " + baseManaged + " → " + managed + " B; mallas " + baseMeshes + " → " + meshes);
            Assert.That(SceneHost.World, Is.SameAs(world), "la base común no se rehace");
            Assert.That(meshes, Is.InRange((int)Math.Floor(baseMeshes * 0.95), (int)Math.Ceiling(baseMeshes * 1.05)), "las mallas vivas ± 5 %");
            Assert.That((double)managed, Is.InRange(baseManaged * 0.95, baseManaged * 1.05), "la memoria administrada ± 5 %");
            yield return Settle();   // la animación de la tarjeta elegida (el anillo crece y vuelve) termina antes de la auditoría
            AssertClean(H);
        }

        [UnityTest, Category("AC-SCN-06-04"), Category("AC-SCN-12-01")]
        public IEnumerator TheCoastSwellMovesTheBoatButNotTheDistance()
        {
            AssumeGpu();
            CreateIn(EnvironmentId.Coast);
            yield return EnterFree();
            var before = SceneHost.Frame.Distance;
            H.App.Workout.Pause();
            for (var i = 0; i < 60; i++)
            {
                H.Step();
            }

            yield return Frames(2);
            var f = SceneHost.Frame;
            Assert.That(f.Swell, Is.True);
            Assert.That(System.Math.Abs(f.Rower.SwellHeaveM) + System.Math.Abs(f.Rower.SwellPitchRad), Is.GreaterThan(0), "la ola sobre el casco");
            Assert.That(f.Distance, Is.GreaterThanOrEqualTo(before), "la distancia no retrocede");
            AssertClean(H);
        }

        // ── QA-V8-02: los 25 casos del legacy (01-po-spec §5; AC-SCN-20-05, AC-SCN-01-17) ──

        // LT-111 a LT-122 (test/scene/sky_shore_painter_test.dart:16) y LT-127 a LT-138 (test/scene/scene_view_test.dart:11): río,
        // costa y regata a 390 × 844 y 844 × 390, a las 12 y a las 23 h.
        public static IEnumerable<TestCaseData> Paints()
        {
            yield return new TestCaseData(EnvironmentId.River, 390, 844, 12).Returns(null).SetName("EnvironmentId.river 390x844 12.0h pinta sin excepciones")
                .SetCategory("LT-111").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.River, 390, 844, 23).Returns(null).SetName("EnvironmentId.river 390x844 23.0h pinta sin excepciones")
                .SetCategory("LT-112").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.River, 844, 390, 12).Returns(null).SetName("EnvironmentId.river 844x390 12.0h pinta sin excepciones")
                .SetCategory("LT-113").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.River, 844, 390, 23).Returns(null).SetName("EnvironmentId.river 844x390 23.0h pinta sin excepciones")
                .SetCategory("LT-114").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.Coast, 390, 844, 12).Returns(null).SetName("EnvironmentId.coast 390x844 12.0h pinta sin excepciones")
                .SetCategory("LT-115").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.Coast, 390, 844, 23).Returns(null).SetName("EnvironmentId.coast 390x844 23.0h pinta sin excepciones")
                .SetCategory("LT-116").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.Coast, 844, 390, 12).Returns(null).SetName("EnvironmentId.coast 844x390 12.0h pinta sin excepciones")
                .SetCategory("LT-117").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.Coast, 844, 390, 23).Returns(null).SetName("EnvironmentId.coast 844x390 23.0h pinta sin excepciones")
                .SetCategory("LT-118").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.Regatta, 390, 844, 12).Returns(null).SetName("EnvironmentId.regatta 390x844 12.0h pinta sin excepciones")
                .SetCategory("LT-119").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.Regatta, 390, 844, 23).Returns(null).SetName("EnvironmentId.regatta 390x844 23.0h pinta sin excepciones")
                .SetCategory("LT-120").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.Regatta, 844, 390, 12).Returns(null).SetName("EnvironmentId.regatta 844x390 12.0h pinta sin excepciones")
                .SetCategory("LT-121").SetCategory("AC-SCN-20-05");
            yield return new TestCaseData(EnvironmentId.Regatta, 844, 390, 23).Returns(null).SetName("EnvironmentId.regatta 844x390 23.0h pinta sin excepciones")
                .SetCategory("LT-122").SetCategory("AC-SCN-20-05");
        }

        public static IEnumerable<TestCaseData> Animates()
        {
            yield return new TestCaseData(EnvironmentId.River, 390, 844, 12).Returns(null).SetName("EnvironmentId.river 390x844 12.0h anima sin excepciones")
                .SetCategory("LT-127").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.River, 390, 844, 23).Returns(null).SetName("EnvironmentId.river 390x844 23.0h anima sin excepciones")
                .SetCategory("LT-128").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.River, 844, 390, 12).Returns(null).SetName("EnvironmentId.river 844x390 12.0h anima sin excepciones")
                .SetCategory("LT-129").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.River, 844, 390, 23).Returns(null).SetName("EnvironmentId.river 844x390 23.0h anima sin excepciones")
                .SetCategory("LT-130").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.Coast, 390, 844, 12).Returns(null).SetName("EnvironmentId.coast 390x844 12.0h anima sin excepciones")
                .SetCategory("LT-131").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.Coast, 390, 844, 23).Returns(null).SetName("EnvironmentId.coast 390x844 23.0h anima sin excepciones")
                .SetCategory("LT-132").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.Coast, 844, 390, 12).Returns(null).SetName("EnvironmentId.coast 844x390 12.0h anima sin excepciones")
                .SetCategory("LT-133").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.Coast, 844, 390, 23).Returns(null).SetName("EnvironmentId.coast 844x390 23.0h anima sin excepciones")
                .SetCategory("LT-134").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.Regatta, 390, 844, 12).Returns(null).SetName("EnvironmentId.regatta 390x844 12.0h anima sin excepciones")
                .SetCategory("LT-135").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.Regatta, 390, 844, 23).Returns(null).SetName("EnvironmentId.regatta 390x844 23.0h anima sin excepciones")
                .SetCategory("LT-136").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.Regatta, 844, 390, 12).Returns(null).SetName("EnvironmentId.regatta 844x390 12.0h anima sin excepciones")
                .SetCategory("LT-137").SetCategory("AC-SCN-01-17");
            yield return new TestCaseData(EnvironmentId.Regatta, 844, 390, 23).Returns(null).SetName("EnvironmentId.regatta 844x390 23.0h anima sin excepciones")
                .SetCategory("LT-138").SetCategory("AC-SCN-01-17");
        }

        public static IEnumerable<TestCaseData> BehindAndAbove()
        {
            yield return new TestCaseData().Returns(null).SetName("objetos detrás de la cámara se omiten y un puente encima no rompe")
                .SetCategory("LT-123").SetCategory("AC-SCN-20-05").SetCategory("AC-SCN-16-03");
        }

        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.river 390x844 12.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.river 390x844 23.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.river 844x390 12.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.river 844x390 23.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.coast 390x844 12.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.coast 390x844 23.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.coast 844x390 12.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.coast 844x390 23.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.regatta 390x844 12.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.regatta 390x844 23.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.regatta 844x390 12.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.regatta 844x390 23.0h pinta sin excepciones'
        // El cielo y la orilla del escenario a 0 y a 1 234 m con el tiempo de escena 3,2 se dibujan sin excepciones ni errores en el
        // registro (LogAssert: cualquier error o aviso no esperado hace fallar la prueba), con el kit y la sesión del escenario, y la
        // escena no queda vacía.
        [TestCaseSource(nameof(Paints))]
        public IEnumerator EnvironmentPaintsWithoutExceptions(EnvironmentId id, int width, int height, int hour)
        {
            AssumeGpu();
            CreateIn(id, width < height ? DeviceProfile.V390 : DeviceProfile.H844);
            yield return EnterFree();
            Assert.That(SceneHost.KitEnvironment, Is.EqualTo(id));
            foreach (var distance in new[] { 0.0, 1234.0 })
            {
                var draws = SceneHost.Draws;
                yield return SceneAlone(distance, hour);
                Assert.That(SceneHost.Draws, Is.GreaterThan(draws), "se dibujó a " + distance + " m");
                Assert.That(SceneHost.Frame.Environment, Is.EqualTo(id));
                var frame = Read();
                var top = At(frame, frame.width / 2, 4);
                Assert.That(top.r + top.g + top.b, Is.GreaterThan(0), "el cielo arriba no es negro");
                Destroy(frame);
            }

            LogAssert.NoUnexpectedReceived();
            AssertClean(H);
        }

        // legacy: test/scene/sky_shore_painter_test.dart:57 'objetos detrás de la cámara se omiten y un puente encima no rompe'
        // En la escena real a las 22 h: el puente del río a relZ 0,3 (pasa por detrás: no se dibuja delante) y a relZ 1,0 (encima de la
        // cámara), los 15 tipos juntos en x = 12 a 20 m con la variante 3 (la malla del segmento de cada kit con todos, armada y dibujada
        // con el material de los objetos) y el cartel de 750 m de la regata a 30 m: sin excepciones ni avisos.
        [TestCaseSource(nameof(BehindAndAbove))]
        public IEnumerator BehindTheCameraAndABridgeAboveDoNotBreak()
        {
            AssumeGpu();
            CreateIn(EnvironmentId.River, DeviceProfile.V390);
            yield return EnterFree();
            foreach (var relZ in new[] { 0.3, 1.0 })
            {
                yield return SceneAlone(BridgeZ - relZ, 22);
                // El puente pegado a la cámara (relZ 0,3 y 1,0) se dibuja o se omite, pero no rompe; la escena está ahí.
                Assert.That(SceneHost.Frame.Distance, Is.EqualTo(BridgeZ - relZ).Within(1.0), "la escena en el puente");
            }

            // Los 15 tipos en un segmento de cada kit, a 20 m de la cámara.
            var world = SceneHost.World!;
            var camera = world.Rig!.Camera;
            var layer = 0;
            while (layer < 31 && (camera.cullingMask & (1 << layer)) == 0)
            {
                layer++;
            }

            var all = new List<ShoreProp>();
            foreach (PropKind kind in Enum.GetValues(typeof(PropKind)))
            {
                all.Add(new ShoreProp(12, 120, kind, 1.0, 3));
            }

            Assert.That(all.Count, Is.EqualTo(15));
            var props = all.ToArray();
            var meshes = new List<Mesh>();
            foreach (var id in new[] { EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta })
            {
                meshes.Add(SegmentWithAllKinds(id, props));
            }

            for (var f = 0; f < 3; f++)
            {
                // El segmento 2 empieza en z = 100 y la escena va con z relativo: a 100 m, los objetos (z = 120) quedan a 20 m.
                foreach (var mesh in meshes)
                {
                    Graphics.RenderMesh(new RenderParams(world.Prop!) { camera = camera, layer = layer }, mesh, 0, Matrix4x4.Translate(new Vector3(0, 0, 0)));
                }

                yield return SceneAlone(100, 22);
            }

            foreach (var mesh in meshes)
            {
                Destroy(mesh);
            }

            // El cartel de 750 m de la regata a 30 m.
            CreateIn(EnvironmentId.Regatta, DeviceProfile.V390);
            yield return EnterFree();
            yield return SceneAlone(720, 22);
            Assert.That(HasProp(PropKind.DistanceMarker, 750), Is.True, "el cartel de 750 m a la vista");
            LogAssert.NoUnexpectedReceived();
            AssertClean(H);
        }

        // La malla del segmento 2 de un kit con los objetos dados (fuera del iterador: los Span no cruzan un yield).
        private static Mesh SegmentWithAllKinds(EnvironmentId id, ShoreProp[] props)
        {
            var feet = new double[props.Length];
            for (var i = 0; i < props.Length; i++)
            {
                feet[i] = PropPlacement.FootY(id, props[i]);
            }

            var data = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
            new KitSegmentMesh(id).Build(2, props, feet, ReadOnlySpan<HouseLayout>.Empty, ReadOnlySpan<FillItem>.Empty, 0, data);
            Assert.That(data.TriangleCount, Is.GreaterThan(0), id.ToString());
            var mesh = MeshUpload.NewMesh("lt123-" + id);
            using (var upload = new MeshUpload())
            {
                upload.Upload(data, mesh);
            }

            return mesh;
        }

        // El primer puente del río (RiverEnvironment: k = 5, z₀ + 20).
        private const double BridgeZ = 270;

        // ¿Está en la lista del cuadro un objeto de ese tipo (y esa semilla; −1 = cualquiera)?
        private bool HasProp(PropKind kind, int seed)
        {
            var props = SceneHost.Frame.VisibleProps;
            for (var i = 0; i < props.Count; i++)
            {
                if (props[i].Kind == kind && (seed < 0 || props[i].Seed == seed))
                {
                    return true;
                }
            }

            return false;
        }

        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.river 390x844 12.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.river 390x844 23.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.river 844x390 12.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.river 844x390 23.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.coast 390x844 12.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.coast 390x844 23.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.coast 844x390 12.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.coast 844x390 23.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.regatta 390x844 12.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.regatta 390x844 23.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.regatta 844x390 12.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.regatta 844x390 23.0h anima sin excepciones'
        // SPM 24, split 110, 300 m, en marcha y con la hora forzada: 6 cuadros de 100 ms con un dibujo por cuadro; después se cierra el
        // anfitrión (desmontar) y el registro queda sin excepciones ni avisos.
        [TestCaseSource(nameof(Animates))]
        public IEnumerator EnvironmentAnimatesAndTearsDownWithoutExceptions(EnvironmentId id, int width, int height, int hour)
        {
            AssumeGpu();
            CreateIn(id, width < height ? DeviceProfile.V390 : DeviceProfile.H844);
            H.App.Scene.HourOverride.Value = hour;
            yield return EnterFree();
            SceneHost.ForceDistanceForTest(300);
            var before = SceneHost.Draws;
            for (var i = 0; i < 6; i++)
            {
                H.Advance(0.1);
                yield return null;
            }

            Assert.That(SceneHost.Draws - before, Is.GreaterThanOrEqualTo(6), "un dibujo por cuadro");
            Assert.That(SceneHost.Frame.Speed, Is.GreaterThan(0), "remando, el bote avanza");
            Assert.That(SceneHost.Frame.Environment, Is.EqualTo(id));
            AssertClean(H);
            DisposeHost();
            yield return Frames(2);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
