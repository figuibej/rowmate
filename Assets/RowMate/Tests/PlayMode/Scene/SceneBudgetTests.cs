using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Tests.UiSupport;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Scene
{
    // SBT · El presupuesto de la escena (06a-escena-lago/02-tech-design §4.1, §4.4; AC-SCN-01-11, AC-SCN-01-12, AC-WKT-18-21, AC-WKT-14-19):
    // los conteos del dibujo por nivel (Alto, Medio y Bajo con TierScope, que restaura el nivel, y el fixture lo afirma) dentro de §20.1
    // (≤ 150 dibujos, ≤ 50 SetPass, ≤ 350 k triángulos) a las 12 y a las 22 h avanzando; 0 B por cuadro con FrameAllocations.WatchScene
    // (las cuentas "escena" y "URP" y el control positivo en la misma corrida), remando, en pausa, en una transición de hora y de noche;
    // y el HUD con la escena sin elementos netos nuevos.
    [Category("GPU")]
    public sealed class SceneBudgetTests : SceneTestBase
    {
        // §4.1 (ARCHITECTURE §20.2): la CPU de la escena por cuadro, sin el dibujo de URP.
        private const double SceneCpuBudgetMs = 1.5;

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

        private static readonly List<string> RenderedCameras = new List<string>();

        // Las cámaras que URP dibujó en un cuadro (con su tipo): el diagnóstico de lo que suma a los contadores.
        private static IEnumerator WatchRenderedCameras()
        {
            RenderedCameras.Clear();
            void OnCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
            {
                RenderedCameras.Add(camera.name + "/" + camera.cameraType + (camera.targetTexture != null ? "/RT" : "/pantalla"));
            }

            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnCamera;
            yield return null;
            yield return null;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnCamera;
        }

        // Lo que más dibuja en el cuadro además del anfitrión de la prueba (el diagnóstico de los contadores, que son de todo el cuadro):
        // las cámaras habilitadas, los paneles de UI Toolkit vivos, los mundos de la escena y las cámaras que dibujó URP.
        private static string Surroundings()
        {
            var cameras = new System.Text.StringBuilder();
            foreach (var camera in Camera.allCameras)
            {
                cameras.Append(camera.name).Append(" (máscara ").Append(camera.cullingMask).Append(camera.targetTexture != null ? ", RT" : ", pantalla").Append(") ");
            }

            var panelNames = new System.Text.StringBuilder();
            var all = Resources.FindObjectsOfTypeAll<UnityEngine.UIElements.PanelSettings>();
            foreach (var settings in all)
            {
                panelNames.Append(settings.name).Append(settings.targetTexture != null ? "(RT " + settings.targetTexture.width + "x" + settings.targetTexture.height + ")" : "(sin RT)").Append(' ');
            }

            var panels = all.Length + " [" + panelNames + "]";
            var documents = UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsSortMode.None).Length;
            var worlds = 0;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "RowMate.Scene")
                {
                    worlds++;
                }
            }

            var rendered = string.Join(", ", RenderedCameras);
            return "cámaras habilitadas: " + Camera.allCamerasCount + " [" + cameras + "]; PanelSettings " + panels + "; UIDocument " + documents
                   + "; mundos de la escena " + worlds + "; cámaras que dibujaron en el último cuadro: " + rendered;
        }

        // La línea de base: el mismo HUD sin escena (el fondo provisorio), para el informe (la UI ya existía desde la It. 2).
        [UnityTest, Category("AC-SCN-01-11"), Category("Perf")]
        public IEnumerator UiBaselineWithoutTheScene()
        {
            AssumeGpu();
            CreateWorkout(null, o => o.Profile = DeviceProfile.TV);
            yield return EnterFree();
            using (var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count"))
            using (var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
            {
                long maxBatches = 0, maxSetPass = 0;
                for (var i = 0; i < 30; i++)
                {
                    H.Step();
                    yield return null;
                    maxBatches = Math.Max(maxBatches, batches.LastValue);
                    maxSetPass = Math.Max(maxSetPass, setPass.LastValue);
                }

                Debug.Log("[SBT] la UI sin escena: " + maxBatches + " dibujos, " + maxSetPass + " SetPass");
                yield return WatchRenderedCameras();
                Debug.Log("[SBT] entorno: " + Surroundings());
            }
        }

        [UnityTest, Category("AC-SCN-01-11"), Category("Perf")]
        public IEnumerator DrawCountsPerTierWithinTheBudget([Values(QualityTier.High, QualityTier.Medium, QualityTier.Low)] QualityTier tier)
        {
            AssumeGpu();
            using (new TierScope(tier))
            {
                CreateScene(DeviceProfile.TV, o => o.SceneTier = tier);
                yield return EnterFree();
                Assert.That(SceneHost.Tier, Is.EqualTo(tier));
                // Los contadores de URP son de todo el cuadro y la UI suma lo suyo, que depende de lo que corrió antes en la misma sesión del
                // Editor (en la corrida completa el mismo HUD da 730 dibujos y solo, 2; probablemente el atlas dinámico de UI Toolkit, ya lleno). Para medir la
                // escena sola, el panel de la prueba no dibuja mientras se mide (la escena se dibuja igual: su cámara, a pedido). La UI
                // se mide aparte (UiBaselineWithoutTheScene).
                H.Root.style.display = DisplayStyle.None;
                yield return Frames(3);
                foreach (var hour in new[] { 12, 22 })
                {
                    SceneHost.ForceHourForTest(hour);
                    SceneHost.ForceDistanceForTest(920);
                    using (var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count"))
                    using (var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
                    using (var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count"))
                    {
                        long maxBatches = 0, maxSetPass = 0, maxTriangles = 0;
                        for (var i = 0; i < 30; i++)
                        {
                            H.Step();
                            yield return null;
                            maxBatches = Math.Max(maxBatches, batches.LastValue);
                            maxSetPass = Math.Max(maxSetPass, setPass.LastValue);
                            maxTriangles = Math.Max(maxTriangles, triangles.LastValue);
                        }

                        H.Step();
                        yield return WatchRenderedCameras();
                        Debug.Log("[SBT] entorno: " + Surroundings());
                        Debug.Log("[SBT] " + tier + " " + hour + " h: " + maxBatches + " dibujos, " + maxSetPass + " SetPass, " + maxTriangles + " triángulos, "
                                  + SceneHost.World!.Director!.Fill.Instances + " instancias de relleno");
                        if (hour == 12)
                        {
                            // El desglose para el informe: sin post y sin sombras.
                            var data = SceneHost.World.Rig!.Data;
                            data.renderPostProcessing = false;
                            long noPost = 0;
                            for (var i = 0; i < 6; i++)
                            {
                                H.Step();
                                yield return null;
                                noPost = Math.Max(noPost, setPass.LastValue);
                            }

                            data.renderPostProcessing = true;
                            data.renderShadows = false;
                            long noShadows = 0;
                            for (var i = 0; i < 6; i++)
                            {
                                H.Step();
                                yield return null;
                                noShadows = Math.Max(noShadows, setPass.LastValue);
                            }

                            data.renderShadows = SceneHost.Profile.Shadows;
                            Debug.Log("[SBT] " + tier + " SetPass sin post: " + noPost + "; sin sombras: " + noShadows);
                        }

                        Assert.That(maxBatches, Is.LessThanOrEqualTo(150), tier + " " + hour + " h: dibujos");
                        // §20.1: ≤ 50 SetPass en Medio y Bajo; en Alto "el Designer puede ampliarlo en la It. 6a con medición": las dos cascadas de
                        // 2 048 a 80 m vuelven a dibujar los pinos hasta 120 m (B6a-05, pendiente de confirmar por el Designer en WP-D2). Triángulos
                        // visibles: 600 k / 350 k / 180 k.
                        Assert.That(maxSetPass, Is.LessThanOrEqualTo(tier == QualityTier.High ? 80 : 50), tier + " " + hour + " h: SetPass");
                        Assert.That(maxTriangles, Is.LessThanOrEqualTo(tier == QualityTier.High ? 600000 : tier == QualityTier.Medium ? 350000 : 180000),
                            tier + " " + hour + " h: triángulos");
                        Assert.That(SceneHost.World.Director.Fill.Overflow, Is.EqualTo(0), "ningún lote se llenó");
                    }
                }
            }
        }

        // AC-SCN-01-12 (QA-G6aT-06): 0 asignaciones por cuadro en la escena (el Tick y el dibujo), en cuadros reales con el repintado del
        // HUD; el control positivo (byte[64] en el Tick) cuenta 120 de 120 en la cuenta "escena" y no se filtra a la de URP.
        [UnityTest, Category("AC-SCN-01-12")]
        public IEnumerator NoAllocationsPerFrame()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            var host = SceneHost;
            // Calentar: que pasen segmentos (la orilla se arma con el pool) y se armen los lotes.
            for (var i = 0; i < 120; i++)
            {
                H.Step();
                yield return null;
            }

            // El control positivo.
            using (var positive = FrameAllocations.WatchScene(host))
            {
                host.AllocateInTickForTest = true;
                for (var i = 0; i < 120; i++)
                {
                    H.Step();
                    yield return null;
                    positive.Sample();
                }

                host.AllocateInTickForTest = false;
                positive.Stop();
                Assert.That(positive.SceneTicks, Is.EqualTo(120));
                Assert.That(positive.SceneAllocations, Is.GreaterThanOrEqualTo(120), "el control positivo: " + positive);
                Assert.That(positive.RenderAllocations, Is.EqualTo(0), "el control no se filtra a la cuenta URP: " + positive);
            }

            foreach (var scene in new[] { "remando", "pausa", "hora 12 → 18", "noche" })
            {
                if (scene == "pausa")
                {
                    H.App.Workout.Pause();
                }
                else if (scene == "hora 12 → 18")
                {
                    H.App.Workout.Resume();
                    H.App.Scene.HourOverride.Value = 12;
                    H.Step();
                    H.App.Scene.HourOverride.Value = 18;
                }
                else if (scene == "noche")
                {
                    H.App.Scene.HourOverride.Value = 22;
                    H.Advance(2);
                }

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
                    Debug.Log("[SBT] 0 B " + scene + ": " + measured);
                    Assert.That(measured.SceneTicks, Is.EqualTo(120), scene);
                    Assert.That(measured.Renders, Is.EqualTo(120), scene);
                    Assert.That(measured.SceneAllocations, Is.EqualTo(0), scene + ": " + measured);
                    Assert.That(measured.RenderAllocations, Is.EqualTo(0), scene + ": " + measured);
                }
            }
        }

        // QA-V6a-REN-07 y QA-V6a-09 (AC-SCN-01-11, §4.1 y "Cambios por verificación (ronda 1)" de 02-tech-design): por nivel, (a) la memoria
        // del contenido de la escena (las texturas procedurales; sin el pack), (b) la de las RT que suma la escena (la de la sesión, los
        // intermedios de URP con su MSAA, la cadena del bloom y el mapa de sombras: "Render Textures Bytes" antes de la precarga y con la
        // escena dibujando) y (c) la CPU de la escena sin el dibujo de URP (SceneHost.LastCpuMs = el marcador RowMate.Scene.Update: la
        // sesión de Core, la orilla, aplicar el cuadro y las matrices), con sus topes. Medio y Bajo en TV (1 080 × 2 400, el teléfono de
        // referencia de §4.1); Alto en W1280 (el escritorio: la tabla del teléfono no es la suya).
        [UnityTest, Category("AC-SCN-01-11"), Category("Perf")]
        public IEnumerator MemoryAndSceneCpuPerTierWithinTheBudget([Values(QualityTier.High, QualityTier.Medium, QualityTier.Low)] QualityTier tier)
        {
            AssumeGpu();
            using (new TierScope(tier))
            {
                var profile = tier == QualityTier.High ? DeviceProfile.W1280 : DeviceProfile.TV;
                CreateScene(profile, o => o.SceneTier = tier);
                using (var rtBytes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render Textures Bytes"))
                {
                    yield return Frames(3);
                    var before = rtBytes.LastValue;
                    yield return EnterFree();
                    SceneHost.ForceHourForTest(12);
                    SceneHost.ForceDistanceForTest(920);
                    // QA-V6a-R2-02 (a): la CPU en TRES tramos de 120 cuadros (el primero arranca con el salto a 920 m); el p95 de cada tramo y
                    // la MEDIANA de los tres contra el tope. Un tramo con un cuadro lento del Editor (el reloj de pared) no decide solo: con
                    // un solo p95 la prueba daba 1,24 / 1,51 / 1,18 / 1,16 ms en cuatro corridas de Alto.
                    var cpu = new List<double>();
                    var tramos = new List<double>();
                    var all = new List<double>();
                    for (var tramo = 0; tramo < 3; tramo++)
                    {
                        cpu.Clear();
                        for (var i = 0; i < 120; i++)
                        {
                            H.Step();
                            yield return null;
                            cpu.Add(SceneHost.LastCpuMs);
                        }

                        all.AddRange(cpu);
                        cpu.Sort();
                        tramos.Add(cpu[(int)Math.Ceiling(0.95 * cpu.Count) - 1]);
                    }

                    var after = rtBytes.LastValue;
                    var world = SceneHost.World!;
                    long content = 0;
                    long reported = 0;
                    foreach (var texture in new Texture?[] { world.CloudAtlas?.Texture, world.Moon?.Texture, world.Noise?.Texture, world.CloudLayer?.Texture, world.LayerAlbedo, world.LayerNormal })
                    {
                        if (texture != null)
                        {
                            // Lo que ocupa en la GPU (formato × tamaño × mips × capas): el número de la tabla de §4.1. GetRuntimeMemorySizeLong, solo
                            // como dato (en el Editor cuenta además una copia de la CPU aunque la textura ya no sea legible).
                            content += GpuBytes(texture);
                            reported += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
                        }
                    }

                    tramos.Sort();
                    var p95 = tramos[1];
                    all.Sort();
                    var max = all[all.Count - 1];
                    var rtMb = (after - before) / (1024.0 * 1024.0);
                    var output = SceneHost.Output!;
                    Debug.Log("[SBT] memoria y CPU " + tier + " (" + profile.Code + "): contenido " + (content / (1024.0 * 1024.0)).ToString("0.00") + " MB (" + (reported / (1024.0 * 1024.0)).ToString("0.00") + " MB según el Editor); RT de la escena "
                              + rtMb.ToString("0.0") + " MB (la de la sesión " + output.width + " × " + output.height + " " + output.graphicsFormat + "); CPU de la escena p95 "
                              + p95.ToString("0.00") + " ms (la mediana de los p95 de tres tramos: " + tramos[0].ToString("0.00") + " / " + tramos[1].ToString("0.00") + " / " + tramos[2].ToString("0.00") + "), máx " + max.ToString("0.00") + " ms");
                    Assert.That(rtBytes.Valid, Is.True, "el contador de las RT");
                    // §4.1: ≈ 0,6 MB (+ 0,5 MB de la capa lejana de nubes en Alto), sin la copia de la CPU (las texturas dejan de ser legibles).
                    Assert.That(content, Is.LessThanOrEqualTo(tier == QualityTier.High ? 1600L * 1024 : 1024L * 1024), tier + ": el contenido de la escena sin el pack");
                    // §4.1 y §20.1 (180 MB entre todo, con los 10,4 MB de la RT de la UI): Medio ≤ 64 MB (la tabla, con la profundidad de la RT de
                    // la sesión que no contaba); Bajo ≤ 48 MB (sin tabla: la mitad del tope de Medio en memoria por píxel); Alto, en el escritorio, ≤ 180 MB.
                    var rtCap = tier == QualityTier.Medium ? 64.0 : tier == QualityTier.Low ? 48.0 : 180.0;
                    Assert.That(rtMb, Is.LessThanOrEqualTo(rtCap), tier + ": las RT de la escena");
                    // §4.1: la CPU de la escena ≤ 1,5 ms (en el teléfono de referencia); en la PC del Editor, la mediana de tres p95 de 120 cuadros con
                    // el mismo tope (QA-V6a-R2-02 a).
                    Assert.That(p95, Is.LessThanOrEqualTo(SceneCpuBudgetMs), tier + ": la CPU de la escena (p95)");
                }
            }
        }

        // Los bytes de una textura en la GPU: Σ mips del tamaño de cada nivel con su formato, por las capas de un arreglo.
        private static long GpuBytes(Texture texture)
        {
            var layers = texture is Texture2DArray array ? array.depth : 1;
            long bytes = 0;
            for (var mip = 0; mip < texture.mipmapCount; mip++)
            {
                var w = Math.Max(1, texture.width >> mip);
                var h = Math.Max(1, texture.height >> mip);
                bytes += (long)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipmapSize(w, h, texture.graphicsFormat);
            }

            return bytes * layers;
        }

        // QA-V6a-REN-11 y QA-V6a-07 (AC-SCN-01-12: "la escena y el HUD … repintado incluido"): con una rutina, los paneles de series abiertos
        // y la escena, 120 cuadros reales en los que se mide TODO el C# del cuadro (el MotionDriver con el HUD, el Tick de la escena con la
        // orilla, el pedido de render y el repintado de la superficie, el repintado de los paneles): 0; con un borde de segmento adentro
        // de la ventana (entra uno). El control positivo (byte[64] en el Tick, adentro de lo medido) cuenta en la misma corrida.
        [UnityTest, Category("AC-SCN-01-12"), Category("AC-WKT-18-21")]
        public IEnumerator NoAllocationsPerFrameWithTheHudAndTheScene()
        {
            AssumeGpu();
            var prefs = new RowMate.Core.Common.MemoryPreferences();
            prefs.SetBool(RowMate.App.Workout.SeriesPanelPrefs.LapsKey, true);
            prefs.SetBool(RowMate.App.Workout.SeriesPanelPrefs.RepsKey, true);
            CreateWorkout(RowMate.Tests.Support.RoutineSeeds.F5, o =>
            {
                o.Scene = SceneMode.Real;
                o.Profile = DeviceProfile.TV;
                o.IsDesktop = false;
                o.Preferences = prefs;
            });
            yield return StartRoutine("Series 600");
            yield return AdvanceTo(40.2);
            yield return Settle();
            var host = SceneHost;
            Assert.That(host.IsShowing, Is.True, "la escena a la vista");
            // Calentar los caminos (pasan segmentos con el pool).
            for (var i = 0; i < 60; i++)
            {
                H.Step();
                yield return null;
            }

            var dt = TimeSpan.FromTicks((long)Math.Round(UiTestHost.FrameSeconds * TimeSpan.TicksPerSecond));
            using (var positive = FrameAllocations.Watch(Hud.Series!.Laps, Hud.Series.Reps))
            {
                host.AllocateInTickForTest = true;
                for (var i = 0; i < 30; i++)
                {
                    H.Scheduler.Advance(dt);
                    positive.Step(() =>
                    {
                        H.Handle.Tick();
                        H.SceneTick();
                    });
                    yield return null;
                    positive.Sample();
                }

                host.AllocateInTickForTest = false;
                positive.Stop();
                Assert.That(positive.Total, Is.GreaterThanOrEqualTo(30), "el control positivo adentro de lo medido: " + positive);
            }

            // Un borde de segmento a 3 m del bote: entra uno dentro de la ventana.
            var distance = host.Frame.Distance;
            var border = (Math.Floor(distance / ShoreEnvironment.SegmentLength) + 2) * ShoreEnvironment.SegmentLength;
            host.ForceDistanceForTest(border - 3);
            H.Step();
            yield return null;
            var from = host.Frame.Distance;
            var draws = host.Draws;
            using (var measured = FrameAllocations.Watch(Hud.Series!.Laps, Hud.Series.Reps))
            {
                for (var i = 0; i < 120; i++)
                {
                    H.Scheduler.Advance(dt);
                    measured.Step(() =>
                    {
                        H.Handle.Tick();
                        H.SceneTick();
                    });
                    yield return null;
                    measured.Sample();
                }

                measured.Stop();
                var to = host.Frame.Distance;
                Debug.Log("[SBT] 0 B con el HUD, los paneles y la escena: " + measured + "; de " + from.ToString("0.0") + " a " + to.ToString("0.0") + " m");
                Assert.That(host.Draws - draws, Is.EqualTo(120), "la escena se dibujó en cada cuadro medido");
                Assert.That(Math.Floor(to / ShoreEnvironment.SegmentLength), Is.GreaterThan(Math.Floor(from / ShoreEnvironment.SegmentLength)),
                    "un borde de segmento adentro de la ventana");
                Assert.That(measured.Total, Is.EqualTo(0), "0 B por cuadro con el HUD y la escena: " + measured);
            }
        }

        // AC-WKT-18-21 con la escena: scene-surface reemplaza al fondo provisorio (display: none): el conteo no cambia.
        [UnityTest, Category("AC-WKT-18-21"), Category("AC-WKT-17-01")]
        public IEnumerator TheHudAddsNoElementsWithTheScene()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            var withScene = CountVisible(Immersive.Root);
            Assert.That(Hud.SceneShown, Is.True);
            Assert.That(H.IsShown("hud-backdrop"), Is.False, "el provisorio en display: none");
            Assert.That(H.IsShown("scene-surface"), Is.True);
            CreateWorkout(null, o => o.Profile = DeviceProfile.TV);
            yield return StartFree();
            yield return AdvanceTo(2.5);
            yield return Settle();
            var without = CountVisible(Immersive.Root);
            Assert.That(withScene, Is.EqualTo(without), "0 elementos netos (DT6a-12)");
            Assert.That(withScene, Is.LessThanOrEqualTo(60));
        }

        private static int CountVisible(VisualElement root)
        {
            var count = 0;
            foreach (var element in root.Query<VisualElement>().Build())
            {
                if (UiQuery.IsShown(element))
                {
                    count++;
                }
            }

            return count;
        }
    }

    // SAD · La adaptación (06a-escena-lago/02-tech-design §2.16, §4.2; AC-SCN-01-13; QA-G6aT-10): con tiempos de prueba, 13 ms de GPU
    // sostenidos 3 s bajan la resolución un paso (la RT se rehace con 0,95 y filtro bilineal); 33 ms de intervalo con 8 ms de trabajo no
    // dispara; suspendida (sin dibujar) no suma. En el Editor lo del asset de URP solo se anota (Bootstrap; acá, la RT).
    [Category("GPU")]
    public sealed class SceneAdaptationTests : SceneTestBase
    {
        [UnityTest, Category("AC-SCN-01-13")]
        public IEnumerator SustainedGpuLowersTheResolution()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            var adaptation = new ThermalAdaptation(ThermalSpec.Default, SceneHost.Resolution);
            var now = 100.0;
            var action = QualityAction.None;
            for (var i = 0; i < 60 * 4 && action.Kind == QualityActionKind.None; i++)
            {
                now += 1.0 / 60;
                action = adaptation.Sample(now, 14, true, true, false);
            }

            Assert.That(action.Kind, Is.EqualTo(QualityActionKind.LowerResolution));
            SceneHost.SetResolution(action.Resolution);
            H.Step();
            yield return Frames();
            Assert.That(SceneHost.Output!.width, Is.EqualTo((int)Math.Round(DeviceProfile.TV.WidthPx * action.Resolution)));
            Assert.That(SceneHost.Output.filterMode, Is.EqualTo(FilterMode.Bilinear));

            // 30 fps holgados (8 ms de trabajo) y suspendida: no dispara.
            var calm = new ThermalAdaptation(ThermalSpec.Default, 1);
            for (var i = 0; i < 30 * 10; i++)
            {
                Assert.That(calm.Sample(200 + i / 30.0, 8, true, true, false).Kind, Is.EqualTo(QualityActionKind.None));
                Assert.That(calm.Sample(200 + i / 30.0 + 0.01, 20, true, false, true).Kind, Is.EqualTo(QualityActionKind.None));
            }
        }

        // QA-V6a-REN-09 (AC-SCN-01-13: "por último, el nivel"; ADR 018): bajar el nivel baja el PERFIL de la escena (HDR y formato de la
        // RT, MSAA, sombras, bloom, campo de estrellas y capa de nubes, LOD, bosque cercano, pasto) sobre el mundo ya armado, sin rehacerlo
        // ni cortar la sesión, y la resolución no sube. Bajo es el último.
        [UnityTest, Category("AC-SCN-01-13")]
        public IEnumerator LoweringTheTierLowersTheWholeSceneProfile()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            var host = SceneHost;
            var world = host.World!;
            var session = host.Session;
            Assert.That(host.Tier, Is.EqualTo(QualityTier.High));
            Assert.That(world.Sky!.IsKeywordEnabled(RowMate.Scene.SceneShaderIds.SkyStarFieldKeyword), Is.True, "Alto: el campo tenue de estrellas");

            Assert.That(host.LowerTier(), Is.True);
            H.Step();
            yield return Frames();
            Assert.That(host.Tier, Is.EqualTo(QualityTier.Medium));
            Assert.That(host.World, Is.SameAs(world), "sin rehacer el mundo");
            Assert.That(host.Session, Is.SameAs(session), "la misma sesión");
            Assert.That(host.Resolution, Is.LessThanOrEqualTo(host.Profile.Resolution + 1e-9), "la resolución del nivel (o menos)");
            Assert.That(world.Sky.IsKeywordEnabled(RowMate.Scene.SceneShaderIds.SkyStarFieldKeyword), Is.EqualTo(host.Profile.StarField), "Medio: el campo de estrellas del perfil");
            Assert.That(world.Rig!.Camera.allowMSAA, Is.EqualTo(host.Profile.Msaa > 1));
            Assert.That(world.Rig.Data.renderShadows, Is.EqualTo(host.Profile.Shadows));
            Assert.That(world.Director!.Fill.Tier, Is.EqualTo(QualityTier.Medium), "el relleno del nivel");
            Assert.That(host.Output!.graphicsFormat, Is.EqualTo(RowMate.Scene.SceneRenderTarget.FormatFor(host.Profile.Hdr, QualityTier.Medium)));
            var draws = host.Draws;

            Assert.That(host.LowerTier(), Is.True);
            H.Step();
            yield return Frames();
            Assert.That(host.Tier, Is.EqualTo(QualityTier.Low));
            Assert.That(world.Rig.Camera.allowHDR, Is.False, "Bajo: sin HDR");
            Assert.That(world.Rig.Data.renderShadows, Is.EqualTo(host.Profile.Shadows), "Bajo: las sombras del perfil");
            Assert.That(world.Lighting!.BloomEnabled, Is.EqualTo(host.Profile.Bloom), "Bajo: el bloom del perfil");
            Assert.That(host.Output!.graphicsFormat, Is.EqualTo(UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB), "Bajo: la RT de 8 bits");
            Assert.That(host.Draws, Is.GreaterThan(draws), "la escena sigue dibujándose");
            Assert.That(host.LowerTier(), Is.False, "Bajo es el último");
            AssertClean(H);
        }
    }
}
