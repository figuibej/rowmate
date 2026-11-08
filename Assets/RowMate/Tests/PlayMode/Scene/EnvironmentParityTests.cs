using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // EPT · El mismo paisaje que el legacy, en pantalla (QA-V8-03; AC-SCN-14-10; 08-entornos-selector/02-tech-design §7.3): en río,
    // costa y regata, con la respiración en 0, el ancla 3D de cada objeto de la lista del cuadro (ShoreStreamer.TryAnchor: el pie del
    // objeto en el kit) proyectada por la cámara real cae en la x de pantalla del legacy (SceneCamera con y = 0) ± 2 dp, en V390, H844
    // y los cinco tamaños de la app, a 0 m, en el lugar de cada escenario y a 1 234 m; la base solo sube por el terreno (≤ 2,5 m; en el
    // río, el muro de 1,2 m). Es la prueba de las marcas de CAP-44 a CAP-46 (Snapshots/EnvironmentMarks) sobre todos los tamaños.
    [Category("GPU")]
    public sealed class EnvironmentParityTests : SceneTestBase
    {
        private static readonly EnvironmentId[] Kits = { EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };

        private const double ToleranceDp = 2.0;
        private const double MaxFootRise = 2.5;
        private const double RiverWallRise = 1.2;

        [UnityTest, Category("AC-SCN-14-10")]
        public IEnumerator EachObjectSitsOnTheLegacyX([ValueSource(nameof(Kits))] EnvironmentId id)
        {
            AssumeGpu();
            var preferences = new MemoryPreferences();
            preferences.SetString(EnvironmentKeys.PreferenceKey, EnvironmentKeys.ToKey(id));
            CreateScene(DeviceProfile.TV, o => o.Preferences = preferences);
            yield return EnterFree();
            var host = SceneHost;
            Assert.That(host.KitEnvironment, Is.EqualTo(id));
            host.BreathingOffForTest(true);
            var place = id == EnvironmentId.River ? 250.0 : id == EnvironmentId.Coast ? 500.0 : 180.0;
            var checkedProps = 0;
            var worst = 0.0;
            var worstRise = 0.0;
            foreach (var profile in new[] { DeviceProfile.V390, DeviceProfile.H844, DeviceProfile.TV, DeviceProfile.P360, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280 })
            {
                H.Resize(profile);
                foreach (var distance in new[] { 0.0, place, 1234.0 })
                {
                    host.ForceDistanceForTest(distance);
                    host.FreezeSceneTimeForTest(3.2);
                    H.Step();
                    yield return Frames(1);
                    var camera = host.World!.Rig!.Camera;
                    var cam = new SceneCamera(profile.WidthDp, profile.HeightDp);
                    var props = host.Frame.VisibleProps;
                    var d = host.Frame.Distance;
                    for (var i = 0; i < props.Count; i++)
                    {
                        var p = props[i];
                        var relZ = p.Z - d;
                        if (!cam.TryProject(p.X, 0, relZ, out var lx, out _))
                        {
                            continue;
                        }

                        Assert.That(host.World.Director!.Shore.TryAnchor(p, out var ax, out var ay, out _), Is.True,
                            profile.Code + " " + distance + ": " + p + " sin colocar");
                        var v = camera.WorldToViewportPoint(new Vector3((float)ax, (float)ay, (float)relZ));
                        if (v.z <= 0)
                        {
                            continue;
                        }

                        var dx = Math.Abs(v.x * profile.WidthDp - lx);
                        worst = Math.Max(worst, dx);
                        Assert.That(dx, Is.LessThanOrEqualTo(ToleranceDp), id + " " + profile.Code + " " + distance + ": " + p);
                        var rise = PropPlacement.FootY(id, p);
                        worstRise = Math.Max(worstRise, rise);
                        // En el río, lo de la orilla sube con el muro de 1,2 m (dentro del tope de 2,5 del terreno).
                        var limit = MaxFootRise;
                        Assert.That(rise, Is.LessThanOrEqualTo(limit + 1e-9), id + ": la base de " + p + " sube " + rise + " m");
                        checkedProps++;
                    }
                }
            }

            TestContext.WriteLine(id + ": " + checkedProps + " objetos; máx |Δx| " + worst.ToString("0.###") + " dp; base máx " + worstRise.ToString("0.##") + " m");
            Assert.That(checkedProps, Is.GreaterThan(100), "objetos medidos");
            LogAssert.NoUnexpectedReceived();
            AssertClean(H);
        }
    }
}
