using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Tests.UiSupport;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Scene
{
    // EBT · El presupuesto por escenario (08-entornos-selector/02-tech-design §5, DA8-17; AC-SCN-01-19): río, costa y regata en su
    // distancia de prueba (E-250, E-500, E-180) a las 12 y a las 22 h, por nivel: dibujos, SetPass y triángulos con el maniquí dentro de
    // los topes de §5 (Medio ≤ 95 / 50 / 220 k; Bajo ≤ 70 / 50 / 180 k; Alto ≤ 150 / 80 / 600 k, los de la 6a), la CPU de la escena
    // (mediana de los p95 de tres tramos, ≤ 1,5 ms como SceneBudgetTests) y 0 B por cuadro con FrameAllocations.WatchScene y su control
    // positivo. El lago lo cubren SceneBudgetTests (sin cambios en la 8).
    [Category("GPU")]
    public sealed class EnvironmentBudgetTests : SceneTestBase
    {
        private const double SceneCpuBudgetMs = 1.5;

        private static readonly EnvironmentId[] Kits = { EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };

        private int _levelAtStart;

        [OneTimeSetUp]
        public void RememberLevel()
        {
            _levelAtStart = QualitySettings.GetQualityLevel();
        }

        [OneTimeTearDown]
        public void LevelIsRestored()
        {
            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(_levelAtStart), "TierScope restauró el nivel del Editor");
        }

        private static double TestDistance(EnvironmentId id)
        {
            return id == EnvironmentId.River ? 250 : id == EnvironmentId.Coast ? 500 : 180;
        }

        private UiTestHost CreateIn(EnvironmentId id, QualityTier tier, DeviceProfile profile)
        {
            var preferences = new MemoryPreferences();
            preferences.SetString(EnvironmentKeys.PreferenceKey, EnvironmentKeys.ToKey(id));
            return CreateScene(profile, o =>
            {
                o.Preferences = preferences;
                o.SceneTier = tier;
                // It. 6b (AC-6B-29): los presupuestos con el agua avanzada.
                o.SceneWater = RowMate.Core.Scene.Water.WaterForce.Auto;
            });
        }

        [UnityTest, Category("AC-SCN-01-19"), Category("Perf")]
        public IEnumerator DrawCountsAndCpuPerEnvironmentWithinTheBudget([ValueSource(nameof(Kits))] EnvironmentId id,
            [Values(QualityTier.High, QualityTier.Medium, QualityTier.Low)] QualityTier tier)
        {
            AssumeGpu();
            using (new TierScope(tier))
            {
                CreateIn(id, tier, DeviceProfile.TV);
                yield return EnterFree();
                Assert.That(SceneHost.KitEnvironment, Is.EqualTo(id), "el kit armado");
                // It. 6b (AC-6B-29; G6b-16): con el agua avanzada (Auto), lista y a la vista antes de medir; el vidrio no corre (se mide aparte).
                yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
                Assert.That(SceneHost.ShowingAdvancedWater, Is.True, "se mide la avanzada");

                Assert.That(SceneHost.Tier, Is.EqualTo(tier));
                // Como en SceneBudgetTests: el panel de la prueba no dibuja mientras se mide (la UI se mide aparte).
                H.Root.style.display = DisplayStyle.None;
                yield return Frames(3);
                var maxDraws = tier == QualityTier.Medium ? 95 : tier == QualityTier.Low ? 70 : 150;
                var maxSetPass = tier == QualityTier.High ? 80 : 50;
                var maxTriangles = tier == QualityTier.Medium ? 220000 : tier == QualityTier.Low ? 180000 : 600000;
                foreach (var hour in new[] { 12, 22 })
                {
                    SceneHost.ForceHourForTest(hour);
                    SceneHost.ForceDistanceForTest(TestDistance(id));
                    using (var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count"))
                    using (var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
                    using (var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count"))
                    {
                        long draws = 0, passes = 0, tris = 0;
                        // Tres tramos de 120 cuadros: el p95 de cada uno y la mediana de los tres (QA-V6a-R2-02 a).
                        var tramos = new List<double>();
                        var cpu = new List<double>();
                        for (var tramo = 0; tramo < 3; tramo++)
                        {
                            cpu.Clear();
                            for (var i = 0; i < 120; i++)
                            {
                                H.Step();
                                yield return null;
                                if (tramo == 0 && i < 30)
                                {
                                    // Los conteos en los primeros 30 cuadros (el regreso a la distancia de prueba), como SceneBudgetTests.
                                    draws = Math.Max(draws, batches.LastValue);
                                    passes = Math.Max(passes, setPass.LastValue);
                                    tris = Math.Max(tris, triangles.LastValue);
                                }

                                cpu.Add(SceneHost.LastCpuMs);
                            }

                            cpu.Sort();
                            tramos.Add(cpu[(int)Math.Ceiling(0.95 * cpu.Count) - 1]);
                        }

                        tramos.Sort();
                        var p95 = tramos[1];
                        var kit = SceneHost.World!.Director!.Kit!;
                        Debug.Log("[EBT] " + id + " " + tier + " " + hour + " h (E-" + TestDistance(id) + "): " + draws + " dibujos, " + passes + " SetPass, " + tris
                                  + " triángulos, " + kit.SegmentsDrawn + " segmentos del kit; CPU de la escena p95 " + p95.ToString("0.00") + " ms ("
                                  + tramos[0].ToString("0.00") + " / " + tramos[1].ToString("0.00") + " / " + tramos[2].ToString("0.00") + ")");
                        Assert.That(kit.SegmentsDrawn, Is.GreaterThan(0), id + " " + hour + " h: los objetos del kit a la vista");
                        Assert.That(draws, Is.LessThanOrEqualTo(maxDraws), id + " " + tier + " " + hour + " h: dibujos");
                        Assert.That(passes, Is.LessThanOrEqualTo(maxSetPass), id + " " + tier + " " + hour + " h: SetPass");
                        Assert.That(tris, Is.LessThanOrEqualTo(maxTriangles), id + " " + tier + " " + hour + " h: triángulos");
                        Assert.That(p95, Is.LessThanOrEqualTo(SceneCpuBudgetMs), id + " " + tier + " " + hour + " h: la CPU de la escena (p95)");
                    }
                }
            }
        }

        // §5: 0 B por cuadro en régimen en los kits (12 y 22 h; la regata en E-180), remando, con el control positivo en la misma corrida.
        [UnityTest, Category("AC-SCN-01-19"), Category("AC-SCN-01-12")]
        public IEnumerator NoAllocationsPerFramePerEnvironment([ValueSource(nameof(Kits))] EnvironmentId id)
        {
            AssumeGpu();
            CreateIn(id, QualityTier.High, DeviceProfile.TV);
            yield return EnterFree();
            var host = SceneHost;
            Assert.That(host.KitEnvironment, Is.EqualTo(id));
            host.ForceDistanceForTest(TestDistance(id));
            // Calentar: que pasen segmentos del kit (sus mallas por LOD) y se armen los lotes.
            for (var i = 0; i < 120; i++)
            {
                H.Step();
                yield return null;
            }

            using (var positive = FrameAllocations.WatchScene(host))
            {
                host.AllocateInTickForTest = true;
                for (var i = 0; i < 60; i++)
                {
                    H.Step();
                    yield return null;
                    positive.Sample();
                }

                host.AllocateInTickForTest = false;
                positive.Stop();
                Assert.That(positive.SceneTicks, Is.EqualTo(60));
                Assert.That(positive.SceneAllocations, Is.GreaterThanOrEqualTo(60), "el control positivo: " + positive);
            }

            foreach (var hour in new[] { 12, 22 })
            {
                H.App.Scene.HourOverride.Value = hour;
                H.Advance(2);
                H.Step();
                yield return null;
                using (var measured = FrameAllocations.WatchScene(host))
                {
                    for (var i = 0; i < 120; i++)
                    {
                        H.Step();
                        yield return null;
                        measured.Sample();
                    }

                    measured.Stop();
                    Debug.Log("[EBT] 0 B " + id + " " + hour + " h: " + measured);
                    Assert.That(measured.SceneTicks, Is.EqualTo(120), id + " " + hour + " h");
                    Assert.That(measured.SceneAllocations, Is.EqualTo(0), id + " " + hour + " h: " + measured);
                    Assert.That(measured.RenderAllocations, Is.EqualTo(0), id + " " + hour + " h: " + measured);
                }
            }
        }
    }
}
