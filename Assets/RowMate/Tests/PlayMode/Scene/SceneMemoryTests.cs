using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // SMT · La memoria con el agua avanzada (06b-agua-avanzada/02-tech-design §6; AC-6B-30): 10 vueltas de escenario en reposo (lago, río,
    // costa, regata…) con el agua avanzada lista en cada kit; al volver al lago la memoria (la de Unity y la de los gráficos) queda a ± 5 %
    // de la del primer lago. El tope de 1,5 GB del escritorio y el contenido de Medio están en SceneBudgetTests y en [PERF] memoria.
    [Category("GPU")]
    public sealed class SceneMemoryTests : SceneTestBase
    {
        private static readonly EnvironmentId[] Rounds = { EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta, EnvironmentId.Lake };

        private IEnumerator UntilReady()
        {
            for (var i = 0; i < 60 * 30 && SceneHost.Readiness.Value != SceneReadiness.Ready; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(SceneReadiness.Ready));
            SceneHost.CompleteWaterForTest();
            yield return Frames(3);
        }

        private static long Measure()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return Profiler.GetTotalAllocatedMemoryLong() + Profiler.GetAllocatedMemoryForGraphicsDriver();
        }

        [UnityTest, Category("AC-6B-30"), Category("Perf")]
        public IEnumerator TenEnvironmentRoundsReturnWithinFivePercent()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV, o =>
            {
                o.SceneTier = QualityTier.Medium;
                o.SceneWater = WaterForce.Auto;
            });
            yield return GoToWorkout();
            yield return UntilReady();
            yield return Resources.UnloadUnusedAssets();
            var first = Measure();
            for (var round = 0; round < 10; round++)
            {
                var environment = round == 9 ? EnvironmentId.Lake : Rounds[round % Rounds.Length];
                SceneHost.ForceEnvironmentForTest(environment);
                SceneHost.Preload();
                yield return UntilReady();
                Assert.That(SceneHost.KitEnvironment, Is.EqualTo(environment));
            }

            yield return Resources.UnloadUnusedAssets();
            var last = Measure();
            var change = (double)(last - first) / first;
            Debug.Log("[SMT] memoria: lago " + (first / 1048576.0).ToString("0.0") + " MB → después de 10 vueltas " + (last / 1048576.0).ToString("0.0")
                      + " MB (" + (100 * change).ToString("+0.0;-0.0") + " %); cambios de kit " + SceneHost.KitSwitches);
            Assert.That(Math.Abs(change), Is.LessThanOrEqualTo(0.05), "± 5 % después de 10 vueltas");
            AssertClean(H);
        }
    }
}
