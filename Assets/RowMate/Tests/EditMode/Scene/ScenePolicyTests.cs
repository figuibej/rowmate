using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene
{
    // THA · La adaptación del detalle (06a-escena-lago/02-tech-design §2.16; ARCHITECTURE §20.3; AC-SCN-01-13; QA-G6aT-10).
    [TestFixture]
    public sealed class ThermalAdaptationTests
    {
        // Una serie de cuadros a 60 fps con un tiempo de trabajo fijo; devuelve las acciones.
        private static List<QualityAction> Run(ThermalAdaptation a, ref double now, double seconds, double workMs, bool gpu = true, double fps = 60,
                                                bool drawn = true, bool suspended = false)
        {
            var actions = new List<QualityAction>();
            var frames = (int)(seconds * fps);
            for (var i = 0; i < frames; i++)
            {
                now += 1 / fps;
                var action = a.Sample(now, workMs, gpu, drawn, suspended);
                if (action.Kind != QualityActionKind.None)
                {
                    actions.Add(action);
                }
            }

            return actions;
        }

        [Test, Category("AC-SCN-01-13")]
        public void ThirtyFpsWithEightMsOfWorkNeverTriggers()
        {
            // Un cuadro de 33 ms de intervalo con 8 ms de trabajo es holgado: nunca se mide el intervalo.
            var a = new ThermalAdaptation();
            var now = 0.0;
            Assert.That(Run(a, ref now, 120, 8, fps: 30), Is.Empty);
            Assert.That(a.Resolution, Is.EqualTo(1.0));
        }

        [Test, Category("AC-SCN-01-13")]
        public void ResolutionFirstInSteps()
        {
            var a = new ThermalAdaptation();
            var now = 0.0;
            var actions = Run(a, ref now, 2.9, 14);
            Assert.That(actions, Is.Empty, "13 ms de GPU sostenidos 3 s");
            actions = Run(a, ref now, 0.2, 14);
            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions[0].Kind, Is.EqualTo(QualityActionKind.LowerResolution));
            Assert.That(actions[0].Resolution, Is.EqualTo(0.95));
            actions = Run(a, ref now, 9, 14);
            Assert.That(actions, Is.Empty, "como mucho un cambio de resolución cada 10 s");
            actions = Run(a, ref now, 1.5, 14);
            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions[0].Resolution, Is.EqualTo(0.90));
        }

        // It. 6b (AC-6B-31): resolución → reflejo → sombras → vegetación → nivel → agua degradada (el último, pegado: DV-152). Nunca baja
        // la tasa de cuadros y, después del último paso, nada más.
        [Test, Category("AC-SCN-01-13"), Category("AC-6B-31")]
        public void OrderEndsInDegradedWater()
        {
            var a = new ThermalAdaptation();
            var now = 0.0;
            var kinds = new List<QualityActionKind>();
            foreach (var act in Run(a, ref now, 300, 20))
            {
                kinds.Add(act.Kind);
            }

            Assert.That(kinds, Is.EqualTo(new[]
            {
                QualityActionKind.LowerResolution, QualityActionKind.LowerResolution, QualityActionKind.LowerResolution, QualityActionKind.LowerResolution,
                QualityActionKind.LowerResolution, QualityActionKind.LowerReflection, QualityActionKind.LowerShadowDistance,
                QualityActionKind.LowerVegetationDensity, QualityActionKind.LowerTier, QualityActionKind.DegradeWater,
            }), "resolución hasta 0,75 de a 0,05 → reflejo → sombras → vegetación → nivel → agua degradada");
            Assert.That(a.Resolution, Is.EqualTo(0.75));
            Assert.That(a.ReflectionLowered && a.ShadowsLowered && a.VegetationLowered && a.TierLowered && a.WaterDegraded, Is.True);
            Assert.That(Run(a, ref now, 120, 30), Is.Empty, "después del agua degradada no hay más pasos (y nunca baja la tasa)");
            foreach (var name in Enum.GetNames(typeof(QualityActionKind)))
            {
                Assert.That(name.IndexOf("Frame", StringComparison.Ordinal), Is.LessThan(0), "ninguna acción toca la tasa de cuadros: " + name);
            }
        }

        // Un cambio de resolución como mucho cada 10 s también con los pasos nuevos (AC-6B-31).
        [Test, Category("AC-6B-31")]
        public void ResolutionStillEveryTenSecondsAtMost()
        {
            var a = new ThermalAdaptation();
            var now = 0.0;
            var last = double.NegativeInfinity;
            for (var i = 0; i < 60 * 300; i++)
            {
                now += 1.0 / 60;
                var act = a.Sample(now, 20, true, true, false);
                if (act.Kind == QualityActionKind.LowerResolution || act.Kind == QualityActionKind.RaiseResolution)
                {
                    Assert.That(now - last, Is.GreaterThanOrEqualTo(10 - 1e-9));
                    last = now;
                }
            }
        }

        [Test, Category("AC-SCN-01-13")]
        public void CpuThresholdIsFifteen()
        {
            var a = new ThermalAdaptation();
            var now = 0.0;
            Assert.That(Run(a, ref now, 5, 14, gpu: false), Is.Empty, "14 ms de CPU no dispara");
            Assert.That(Run(a, ref now, 5, 16, gpu: false).Count, Is.EqualTo(1));
        }

        [Test, Category("AC-SCN-01-13")]
        public void RaisingNeedsThirtyRelaxedSeconds()
        {
            var a = new ThermalAdaptation();
            var now = 0.0;
            Run(a, ref now, 3.1, 14);
            Assert.That(a.Resolution, Is.EqualTo(0.95));
            Assert.That(Run(a, ref now, 29, 8), Is.Empty, "29 s holgados no alcanzan");
            var actions = Run(a, ref now, 2, 8);
            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions[0].Kind, Is.EqualTo(QualityActionKind.RaiseResolution));
            Assert.That(a.Resolution, Is.EqualTo(1.0));
            Assert.That(Run(a, ref now, 60, 8), Is.Empty, "no pasa de 1");
            Assert.That(Run(a, ref now, 60, 11), Is.Empty, "entre 10 y 13 ms: ni sube ni baja (histéresis)");
        }

        [Test, Category("AC-SCN-01-13")]
        public void SuspendedItDoesNotCountAndTheWindowRestarts()
        {
            // QA-G6aT-10: con renderFrameInterval 2 (el diálogo quieto), fuera del inmersivo o con la escena oculta no suma, y al volver
            // la ventana de 3 s arranca de cero.
            var a = new ThermalAdaptation();
            var now = 0.0;
            Run(a, ref now, 2.5, 20);
            Assert.That(Run(a, ref now, 10, 20, suspended: true), Is.Empty);
            Assert.That(Run(a, ref now, 10, 20, drawn: false), Is.Empty);
            Assert.That(Run(a, ref now, 2.9, 20), Is.Empty, "la ventana arrancó de cero");
            Assert.That(Run(a, ref now, 0.2, 20).Count, Is.EqualTo(1));
        }

        [Test, Category("AC-SCN-01-13")]
        public void ASingleFastFrameRestartsTheWindow()
        {
            var a = new ThermalAdaptation();
            var now = 0.0;
            Run(a, ref now, 2.5, 20);
            Run(a, ref now, 1.0 / 60, 5);
            Assert.That(Run(a, ref now, 2.9, 20), Is.Empty, "sostenidos de verdad");
        }
    }

    // SWP · La preparación de la escena (06a-escena-lago/02-tech-design §2.16, §3.4; AC-WKT-13-15; QA-G6aT-03).
    [TestFixture]
    public sealed class SceneWarmupPolicyTests
    {
        [Test, Category("AC-WKT-13-15")]
        public void TheSlices()
        {
            Assert.That(SceneWarmupPolicy.SliceMs(false, true, false, 0, false), Is.EqualTo(3), "la pantalla negra y quieta");
            Assert.That(SceneWarmupPolicy.SliceMs(true, false, false, 0, false), Is.EqualTo(3), "Workout en reposo sin nada moviéndose");
            Assert.That(SceneWarmupPolicy.SliceMs(true, false, false, 1, false), Is.EqualTo(0), "Workout con algo moviéndose");
            Assert.That(SceneWarmupPolicy.SliceMs(false, false, true, 0, false), Is.EqualTo(2), "el inmersivo abierto sin movimiento finito");
            Assert.That(SceneWarmupPolicy.SliceMs(false, false, true, 3, false), Is.EqualTo(0), "la cascada, el vuelo, una transición");
            Assert.That(SceneWarmupPolicy.SliceMs(false, false, true, 0, true), Is.EqualTo(0), "el cuadro en que se arma el HUD");
            Assert.That(SceneWarmupPolicy.SliceMs(false, true, true, 0, true), Is.EqualTo(0), "el armado del HUD paga solo");
            Assert.That(SceneWarmupPolicy.SliceMs(false, false, false, 0, false), Is.EqualTo(0), "fuera de todo, nada");
        }

        [Test, Category("AC-WKT-13-15")]
        public void TwoMillisecondsAlsoAfterTheCascade()
        {
            // La escena demorada termina de prepararse por tramos también después de la cascada (DT6a-11 reemplazado).
            for (var frame = 0; frame < 100; frame++)
            {
                Assert.That(SceneWarmupPolicy.SliceMs(false, false, true, 0, false), Is.EqualTo(2));
            }
        }

        // La tabla de cotas del diseño (el objetivo de cada tipo de unidad). NO es la evidencia de AC-WKT-13-15 (QA-V6a-REN-06: compara la
        // tabla consigo misma): el costo real de cada unidad lo mide SceneHost (WorstUnitMs y su fase) y lo informan [PERF] y
        // SceneEntryTests.EachWarmupUnitIsMeasured. Por eso esta prueba no lleva la categoría del criterio.
        // It. 6b: las unidades de la 6a siguen acotadas igual; las nuevas livianas, ≤ 4 ms (VisibleUnitsUnderFourMs) y las de primera
        // vez del proceso son pesadas (HeavyOnlyWhenQuiet).
        [Test]
        public void NoIndivisibleStep()
        {
            var sixA = new[]
            {
                WarmupUnit.Slot, WarmupUnit.TextureRow, WarmupUnit.Mesh, WarmupUnit.Material, WarmupUnit.ShaderVariant, WarmupUnit.SyntheticTick,
                WarmupUnit.WarmupDraw, WarmupUnit.CameraLightVolumeTarget,
            };
            foreach (var unit in sixA)
            {
                Assert.That(SceneWarmupPolicy.UnitCostBoundMs(unit), Is.LessThanOrEqualTo(SceneWarmupPolicy.MaxUnitCostMs), unit + " acotada");
                Assert.That(SceneWarmupPolicy.UnitCostBoundMs(unit), Is.LessThanOrEqualTo(SceneWarmupPolicy.ImmersiveSliceMs), unit + " entra en un tramo");
            }

            Assert.That(SceneWarmupPolicy.UnitCostBoundMs(WarmupUnit.WarmupDraw), Is.EqualTo(1.5), "un cuadro de escena (§4.1)");
            Assert.That(SceneWarmupPolicy.AllowsWarmupDraw(2), Is.True);
            Assert.That(SceneWarmupPolicy.AllowsWarmupDraw(0), Is.False, "nunca durante un movimiento finito");
        }

        [Test, Category("AC-WKT-13-15")]
        public void AtLeastOneUnitPerFrameThenWhileTimeLasts()
        {
            Assert.That(SceneWarmupPolicy.RunAnother(2, 5, 0), Is.True, "al menos una con tramo > 0");
            Assert.That(SceneWarmupPolicy.RunAnother(2, 1.5, 3), Is.True);
            Assert.That(SceneWarmupPolicy.RunAnother(2, 2.0, 3), Is.False);
            Assert.That(SceneWarmupPolicy.RunAnother(0, 0, 0), Is.False, "sin tramo, ninguna");
        }

        // ── It. 6b (#1; 06b-agua-avanzada/02-tech-design §2.1; DT6b-25, G6b-05) ──

        private static readonly WarmupFrame QuietFrame = new WarmupFrame(true, true, false);

        [Test, Category("AC-6B-01")]
        public void HeavyOnlyWhenQuiet()
        {
            // Una pesada (lo de primera vez del proceso) nunca corre con el inmersivo a la vista; una liviana sí, con el tramo de 2 ms.
            var visible = new WarmupFrame(SceneWarmupPolicy.IsQuiet(false, false, true, 0, false), true, false);
            Assert.That(visible.Quiet, Is.False, "el inmersivo a la vista nunca es quieto");
            Assert.That(SceneWarmupPolicy.RunAnother(2, 0, 0, true, visible), Is.False, "pesada con el inmersivo: no");
            Assert.That(SceneWarmupPolicy.RunAnother(2, 0, 0, false, visible), Is.True, "liviana con el inmersivo: sí");
            Assert.That(SceneWarmupPolicy.RunAnother(3, 0, 0, true, QuietFrame), Is.True, "pesada en reposo: sí");
            Assert.That(SceneWarmupPolicy.RunAnother(0, 0, 0, true, QuietFrame), Is.False, "sin tramo, ninguna");
            Assert.That(SceneWarmupPolicy.RunAnother(3, 3.5, 2, true, QuietFrame), Is.False, "también respeta el tramo");

            Assert.That(SceneWarmupPolicy.IsQuiet(true, false, false, 0, false), Is.True, "Workout en reposo");
            Assert.That(SceneWarmupPolicy.IsQuiet(false, true, false, 0, false), Is.True, "negro y quieto");
            Assert.That(SceneWarmupPolicy.IsQuiet(true, false, false, 1, false), Is.False, "con un movimiento finito");
            Assert.That(SceneWarmupPolicy.IsQuiet(false, true, true, 0, true), Is.False, "el cuadro del armado del HUD lo maneja el negro de la entrada");

            foreach (var unit in new[] { WarmupUnit.CameraLightVolumeTarget, WarmupUnit.FirstUrpRequest, WarmupUnit.CoreJitPreamble, WarmupUnit.PostAtSessionSize })
            {
                Assert.That(SceneWarmupPolicy.Classify(unit), Is.EqualTo(UnitClass.Heavy), unit.ToString());
            }
        }

        [Test, Category("AC-6B-01"), Category("AC-6B-02")]
        public void HeavyNeedsPreviousQuietFrameAndNoInput()
        {
            // G6b-05: el cuadro del toque (con la entrada en curso) y el siguiente al primero quieto no corren pesadas, aunque el orden del
            // player loop fuera otro.
            Assert.That(SceneWarmupPolicy.RunAnother(3, 0, 0, true, new WarmupFrame(true, false, false)), Is.False, "el anterior no fue quieto");
            Assert.That(SceneWarmupPolicy.RunAnother(3, 0, 0, true, new WarmupFrame(true, true, true)), Is.False, "un puntero, toque o tecla en curso");
            Assert.That(SceneWarmupPolicy.RunAnother(3, 0, 0, false, new WarmupFrame(true, true, true)), Is.True, "las livianas no dependen de la entrada");
            Assert.That(new WarmupFrame(true, true, false).AllowsHeavy, Is.True);
            Assert.That(default(WarmupFrame).AllowsHeavy, Is.False);

            // Una secuencia de cuadros armada con Next: reposo, reposo, el toque (entrada), el siguiente, el inmersivo (no quieto), el primero
            // quieto después y recién el otro. Ni el cuadro del toque ni el siguiente corren pesadas.
            var quiet = new[] { true, true, true, true, false, true, true };
            var input = new[] { false, false, true, false, false, false, false };
            var frame = new WarmupFrame(true, true, false);
            var heavy = new bool[quiet.Length];
            for (var i = 0; i < quiet.Length; i++)
            {
                frame = frame.Next(quiet[i], input[i]);
                heavy[i] = SceneWarmupPolicy.RunAnother(3, 0, 0, true, frame);
            }

            Assert.That(heavy, Is.EqualTo(new[] { true, true, false, false, false, false, true }));
        }

        [Test, Category("AC-6B-01")]
        public void PreambleBeforeWorld()
        {
            // El preámbulo de primera vez (0a la cámara, la luz y la RT; 0b el primer pedido de URP; 0c el JIT de Core en 5 unidades), después
            // la sesión preparada, el mundo y, después de Ready, el agua avanzada (fase 10).
            var preamble = new[] { WarmupUnit.CameraLightVolumeTarget, WarmupUnit.FirstUrpRequest, WarmupUnit.CoreJitPreamble };
            var world = new[]
            {
                WarmupUnit.Slot, WarmupUnit.TextureRow, WarmupUnit.Mesh, WarmupUnit.Material, WarmupUnit.ShaderVariant, WarmupUnit.SyntheticTick,
                WarmupUnit.WarmupDraw, WarmupUnit.WarmupSubDraw, WarmupUnit.PostAtSessionSize,
            };
            var water = new[] { WarmupUnit.WaterVariant, WarmupUnit.WaterResource, WarmupUnit.WaterSubDraw };
            foreach (var p in preamble)
            {
                Assert.That(SceneWarmupPolicy.PhaseOf(p), Is.EqualTo(SceneWarmupPolicy.PreamblePhase), p.ToString());
                Assert.That(SceneWarmupPolicy.PhaseOf(p), Is.LessThan(SceneWarmupPolicy.PhaseOf(WarmupUnit.NextSession)));
                foreach (var w in world)
                {
                    Assert.That(SceneWarmupPolicy.PhaseOf(p), Is.LessThan(SceneWarmupPolicy.PhaseOf(w)), p + " antes que " + w);
                }
            }

            foreach (var w in world)
            {
                Assert.That(SceneWarmupPolicy.PhaseOf(WarmupUnit.NextSession), Is.LessThan(SceneWarmupPolicy.PhaseOf(w)), "la sesión preparada antes que " + w);
                foreach (var a in water)
                {
                    Assert.That(SceneWarmupPolicy.PhaseOf(w), Is.LessThan(SceneWarmupPolicy.PhaseOf(a)), "el agua avanzada después de Ready");
                }
            }

            Assert.That(SceneWarmupPolicy.CoreJitPreambleUnits, Is.EqualTo(5));
            foreach (WarmupUnit unit in Enum.GetValues(typeof(WarmupUnit)))
            {
                Assert.That(SceneWarmupPolicy.PhaseOf(unit), Is.InRange(SceneWarmupPolicy.PreamblePhase, SceneWarmupPolicy.WaterPhase), unit.ToString());
            }
        }

        [Test, Category("AC-6B-01")]
        public void VisibleUnitsUnderFourMs()
        {
            // Toda liviana (lo que puede caer en un cuadro visible) tiene cota ≤ 4 ms; lo que pasa de 4 ms es pesada.
            Assert.That(SceneWarmupPolicy.VisibleUnitMaxMs, Is.EqualTo(4));
            foreach (WarmupUnit unit in Enum.GetValues(typeof(WarmupUnit)))
            {
                var bound = SceneWarmupPolicy.UnitCostBoundMs(unit);
                if (SceneWarmupPolicy.Classify(unit) == UnitClass.Light)
                {
                    Assert.That(bound, Is.LessThanOrEqualTo(SceneWarmupPolicy.VisibleUnitMaxMs), unit + " liviana");
                }

                if (bound > SceneWarmupPolicy.VisibleUnitMaxMs)
                {
                    Assert.That(SceneWarmupPolicy.Classify(unit), Is.EqualTo(UnitClass.Heavy), unit + " pasa de 4 ms");
                }
            }

            foreach (var unit in new[] { WarmupUnit.NextSession, WarmupUnit.WarmupSubDraw, WarmupUnit.WaterVariant, WarmupUnit.WaterResource, WarmupUnit.WaterSubDraw })
            {
                Assert.That(SceneWarmupPolicy.Classify(unit), Is.EqualTo(UnitClass.Light), unit.ToString());
            }
        }
    }

    // SLT · Los ids de los slots (06a-escena-lago/02-tech-design §2.17; ASSET_SLOTS §1; DD6a-22).
    [TestFixture]
    public sealed class SlotIdsTests
    {
        [Test, Category("AC-SCN-15-08")]
        public void NoDuplicatesAndOnePerPropKind()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in SlotIds.All)
            {
                Assert.That(seen.Add(id), Is.True, "repetido: " + id);
                Assert.That(id, Does.Match(@"^[a-z]+(\.[a-zA-Z]+)+$"));
            }

            foreach (PropKind kind in Enum.GetValues(typeof(PropKind)))
            {
                Assert.That(seen.Contains(SlotIds.ForProp(kind)), Is.True, kind.ToString());
            }

            Assert.That(SlotIds.ForProp(PropKind.Pine), Is.EqualTo("prop.pine"));
            Assert.That(SlotIds.ForProp(PropKind.LaneBuoy), Is.EqualTo("prop.laneBuoy"));
            Assert.That(SlotIds.ForProp(PropKind.DistanceMarker), Is.EqualTo("prop.distanceMarker"));
        }

        [Test, Category("AC-SCN-15-08")]
        public void TheSlotsOfTheLake()
        {
            foreach (var id in new[]
                     {
                         "vegetation.canopyFar", "sky.profile", "sky.clouds", "sky.moon", "horizon.lake", "water.profile.lake", "terrain.layer.grass",
                         "terrain.layer.pebbles", "terrain.layer.soil", "terrain.layer.rock", "vegetation.grass", "vegetation.fern", "vegetation.bush",
                         "vegetation.reeds", "prop.rock", "boat.hull", "prop.cabin", "prop.pier",
                     })
            {
                Assert.That(SlotIds.All, Does.Contain(id));
            }

            // It. 7 (02-tech-design §1.1): el remero, sus efectos y la boya.
            foreach (var id in new[] { "rower.character", "rower.outfit", "rower.cap", "fx.splash", "fx.drips", "fx.ripple", "prop.markerBuoy" })
            {
                Assert.That(SlotIds.All, Does.Contain(id));
            }
        }
    }

    // HCT · El contraste de las píldoras del HUD sobre la escena (06a-escena-lago/02-tech-design §2.18; AC-WKT-17-13; DD6a-18).
    [TestFixture]
    public sealed class HudContrastTests
    {
        [TestCase("work", 3.678, 3.625, 3.829, 3.018, Category = "AC-WKT-17-13")]
        [TestCase("cooldown", 3.759, 3.705, 3.905, 2.973, Category = "AC-WKT-17-13")]
        [TestCase("unknown", 3.745, 3.691, 3.887, 2.934, Category = "AC-WKT-17-13")]
        public void TheHudVariantsReachThreeAgainstTheWorstGlass(string type, double done, double rendered, double border, double interior)
        {
            // Los valores de 00-deuda-diseno §9.3 (pills.py, pills2.py), a 3 decimales.
            var token = Hud(type);
            Assert.That(HudContrast.Done(token, false), Is.EqualTo(done).Within(0.0006), "la hecha");
            Assert.That(HudContrast.Done(token, true), Is.EqualTo(rendered).Within(0.0006), "la hecha contra el vidrio renderizado");
            Assert.That(HudContrast.HollowBorder(token), Is.EqualTo(border).Within(0.0006), "el borde de la hueca");
            Assert.That(HudContrast.HollowBorderOverInterior(token), Is.EqualTo(interior).Within(0.0006), "contra su interior: la excepción contada");
            Assert.That(HudContrast.Done(token, true), Is.GreaterThanOrEqualTo(HudContrast.GraphicsThreshold));
            Assert.That(HudContrast.Fill(token, true), Is.GreaterThanOrEqualTo(HudContrast.GraphicsThreshold), "el relleno");
            Assert.That(HudContrast.HollowBorder(token), Is.GreaterThanOrEqualTo(HudContrast.GraphicsThreshold));
        }

        [TestCase("work", 3.038, Category = "AC-WKT-17-13")]
        [TestCase("cooldown", 2.850, Category = "AC-WKT-17-13")]
        [TestCase("unknown", 2.629, Category = "AC-WKT-17-13")]
        public void WithoutTheHudVariantTheyDidNotReach(string type, double done)
        {
            Assert.That(HudContrast.Done(Base(type), false), Is.EqualTo(done).Within(0.0006));
        }

        [Test, Category("AC-WKT-17-13")]
        public void RestAndWarmupStayAboveFiveAndAHalf()
        {
            foreach (var token in new[] { Tokens.StepRestHud, Tokens.StepWarmupHud })
            {
                Assert.That(HudContrast.Done(token, true), Is.GreaterThanOrEqualTo(5.5));
                Assert.That(HudContrast.HollowBorderOverInterior(token), Is.GreaterThanOrEqualTo(4.0));
            }

            var rendered = HudContrast.RenderedWorstGlass();
            Assert.That(rendered.R, Is.EqualTo(43));
            Assert.That(rendered.G, Is.EqualTo(48));
            Assert.That(rendered.B, Is.EqualTo(56), "#2B3038");
        }

        private static ColorToken Hud(string type)
        {
            return type == "work" ? Tokens.StepWorkHud : (type == "cooldown" ? Tokens.StepCooldownHud : Tokens.StepUnknownHud);
        }

        private static ColorToken Base(string type)
        {
            return type == "work" ? Tokens.StepWork : (type == "cooldown" ? Tokens.StepCooldown : Tokens.StepUnknown);
        }
    }

    // Las líneas de tiempo de la escena en el inmersivo (SceneTimelines; §2.2, §3.9, §7.4).
    [TestFixture]
    public sealed class SceneTimelinesTests
    {
        [Test, Category("AC-SCN-02-05"), Category("AC-WKT-13-06")]
        public void TheFlightIsAnchoredToTheTap()
        {
            Assert.That(SceneTimelines.FlightStartSeconds(100), Is.EqualTo(100.22).Within(1e-12));
            Assert.That(SceneTimelines.FlightEndMs, Is.EqualTo(1820));
            Assert.That(SceneTimelines.BreathingFullMs, Is.EqualTo(2420));
            Assert.That(SceneTimelines.FlightProgress(0), Is.EqualTo(0));
            Assert.That(SceneTimelines.FlightProgress(220), Is.EqualTo(0));
            Assert.That(SceneTimelines.FlightProgress(1020), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(SceneTimelines.FlightProgress(1820), Is.EqualTo(1));
            Assert.That(SceneTimelines.SceneAppear(220, false), Is.EqualTo(0));
            Assert.That(SceneTimelines.SceneAppear(400, false), Is.EqualTo(1));
            Assert.That(SceneTimelines.SceneAppear(0, true), Is.EqualTo(1), "con reducir movimiento la página funde con todo adentro");
        }

        [Test, Category("AC-SCN-02-06"), Category("AC-WKT-13-08")]
        public void TheExitCoversTheSceneAt400()
        {
            Assert.That(SceneTimelines.SceneExit(200, 1, false), Is.EqualTo(1));
            Assert.That(SceneTimelines.SceneExit(400, 1, false), Is.EqualTo(0));
            Assert.That(SceneTimelines.CloseMs(false), Is.EqualTo(400));
            Assert.That(SceneTimelines.CloseMs(true), Is.EqualTo(120));
        }

        [Test, Category("AC-SCN-01-07")]
        public void TheLateSceneFadesInSixTenths()
        {
            Assert.That(SceneTimelines.LateScene(0), Is.EqualTo(0));
            Assert.That(SceneTimelines.LateScene(600), Is.EqualTo(1));
            Assert.That(SceneTimelines.LateScene(300), Is.EqualTo(Easing.Evaluate(Tokens.EaseEnter, 0.5)).Within(1e-12), "ease.enter");
        }

        [Test, Category("AC-WKT-13-12")]
        public void TheVeilWeight()
        {
            Assert.That(SceneTimelines.VeilWeight(0), Is.EqualTo(0));
            Assert.That(SceneTimelines.VeilWeight(0.15), Is.EqualTo(1));
            Assert.That(SceneTimelines.VeilWeight(0.075), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(SceneTimelines.VeilWeight(0.3), Is.EqualTo(1));
        }

        [Test, Category("AC-WKT-17-07")]
        public void TheHudReflowAfterAChangeOfClass()
        {
            Assert.That(SceneTimelines.HudReflow(0, false), Is.EqualTo(0));
            Assert.That(SceneTimelines.HudReflow(200, false), Is.EqualTo(1));
            Assert.That(SceneTimelines.HudReflow(0, true), Is.EqualTo(1), "con reducir movimiento, en el acto");
            Assert.That(MotionSpecs.HudReflowMs, Is.EqualTo(200));
        }

        [Test, Category("AC-D15-32")]
        public void TheHudReflowCascadeInThreeGroups()
        {
            // It. 15 (#82, DT15-11): grupos a 0, 40 y 80 ms, 200 cada uno con ease.enter; traslado de 8 dp × la escala del HUD → 0.
            Assert.That(MotionSpecs.HudReflowGroups, Is.EqualTo(3));
            Assert.That(MotionSpecs.HudReflowGroupDelayMs, Is.EqualTo(40));
            Assert.That(MotionSpecs.HudReflowShiftDp, Is.EqualTo(8));
            Assert.That(SceneTimelines.HudReflowGroup(0, 0, false), Is.EqualTo(0));
            Assert.That(SceneTimelines.HudReflowGroup(1, 40, false), Is.EqualTo(0), "el grupo 1 arranca a los 40");
            Assert.That(SceneTimelines.HudReflowGroup(2, 80, false), Is.EqualTo(0), "el grupo 2 arranca a los 80");
            Assert.That(SceneTimelines.HudReflowGroup(0, 120, false), Is.InRange(0.0, 1.0).And.GreaterThan(SceneTimelines.HudReflowGroup(1, 120, false)));
            Assert.That(SceneTimelines.HudReflowGroup(1, 120, false), Is.GreaterThan(SceneTimelines.HudReflowGroup(2, 120, false)));
            Assert.That(SceneTimelines.HudReflowGroup(0, 200, false), Is.EqualTo(1));
            Assert.That(SceneTimelines.HudReflowGroup(1, 240, false), Is.EqualTo(1));
            Assert.That(SceneTimelines.HudReflowGroup(2, 279, false), Is.LessThan(1));
            Assert.That(SceneTimelines.HudReflowGroup(2, 280, false), Is.EqualTo(1), "todo quieto a los 280 (antes de la regla de los 400)");
            Assert.That(SceneTimelines.HudReflowGroup(2, 0, true), Is.EqualTo(1), "reducir movimiento: en el acto");
            Assert.That(SceneTimelines.HudReflowGroup(1, 140, false), Is.EqualTo(SceneTimelines.HudReflow(100, false)), "la misma curva, corrida");
            Assert.That(SceneTimelines.HudReflowShift(0, 1.3), Is.EqualTo(8 * 1.3).Within(1e-12));
            Assert.That(SceneTimelines.HudReflowShift(1, 1.3), Is.EqualTo(0));
            Assert.That(SceneTimelines.HudReflowShift(0.5, 1), Is.EqualTo(4));
        }

        [Test, Category("AC-SCN-02-07")]
        public void TheAerialShotStartsWithTheDialogAndSettlesInSixSeconds()
        {
            Assert.That(SceneTimelines.AerialStartMsAfterEnd, Is.EqualTo(300));
            Assert.That(SceneTimelines.AerialSettled(5999), Is.False);
            Assert.That(SceneTimelines.AerialSettled(6000), Is.True);
        }
    }
}
