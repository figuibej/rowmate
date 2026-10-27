using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Common.Json;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Simulator;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // RowingSimulator = lib/core/dev/rowing_simulator.dart:1-83 (F-BLE-14). Los casos LT-01 a LT-07 son los de
    // test/rowing_simulator_test.dart, 1 a 1, con su nombre original (LT-07 está fuera del grupo en el legacy: método aparte).
    // El split exacto usa Math.Pow, que puede diferir en el último bit del pow de Dart (R-T4): se compara con Within(1e-9)
    // y los enteros que salen de él, con el oráculo.
    [TestFixture]
    public sealed class RowingSimulatorTests
    {
        private const double PowTolerance = 1e-9;

        // ---------------------------------------------------------------- AC-BLE-14-01 · valores iniciales

        [Test, Category("AC-BLE-14-01")]
        public void InitialValues()
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false);
            Assert.That(sim.TargetWatts, Is.EqualTo(150));
            Assert.That(sim.TargetSpm, Is.EqualTo(24));
            Assert.That(sim.Rowing, Is.True);
            Assert.That(sim.Noise, Is.False);
            Assert.That(sim.ExactHeartRate, Is.EqualTo(70.0), "el pulso interno arranca en 70");
            Assert.That(sim.HeartRate, Is.EqualTo(70));
            Assert.That(new RowingSimulator().Noise, Is.True, "con ruido por defecto");
        }

        [Test, Category("AC-BLE-14-01"), Category("AC-BASE-03")]
        public void InitialValuesMatchOracle()
        {
            var initial = GoldenVectors.Load("simulator.json").Section("initial").Items[0];
            var sim = new RowingSimulator(new SequenceRandom(), noise: false);
            Assert.That(sim.TargetWatts, Is.EqualTo(initial["targetWatts"].AsLong()));
            Assert.That(sim.TargetSpm, Is.EqualTo(initial["targetSpm"].AsLong()));
            Assert.That(sim.Rowing, Is.EqualTo(initial["rowing"].AsBool()));
            Assert.That(sim.Noise, Is.EqualTo(initial["noise"].AsBool()));
            Assert.That(sim.HeartRate, Is.EqualTo(initial["heartRate"].AsLong()));
            Assert.That(RowingSimulator.MinWatts, Is.EqualTo(initial["minWatts"].AsLong()));
            Assert.That(RowingSimulator.MaxWatts, Is.EqualTo(initial["maxWatts"].AsLong()));
            Assert.That(RowingSimulator.MinSpm, Is.EqualTo(initial["minSpm"].AsLong()));
            Assert.That(RowingSimulator.MaxSpm, Is.EqualTo(initial["maxSpm"].AsLong()));
        }

        // ---------------------------------------------------------------- AC-BLE-14-02 · topes

        [TestCase(9999, 500, Category = "AC-BLE-14-02")]
        [TestCase(0, 30, Category = "AC-BLE-14-02")]
        [TestCase(-5, 30, Category = "AC-BLE-14-02")]
        [TestCase(29, 30, Category = "AC-BLE-14-02")]
        [TestCase(30, 30, Category = "AC-BLE-14-02")]
        [TestCase(203, 203, Category = "AC-BLE-14-02")]
        [TestCase(500, 500, Category = "AC-BLE-14-02")]
        [TestCase(501, 500, Category = "AC-BLE-14-02")]
        [TestCase(int.MinValue, 30, Category = "AC-BLE-14-02")]
        [TestCase(int.MaxValue, 500, Category = "AC-BLE-14-02")]
        public void TargetsAreClamped_Watts(int value, int expected)
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false) { TargetWatts = value };
            Assert.That(sim.TargetWatts, Is.EqualTo(expected));
        }

        [TestCase(1, 14, Category = "AC-BLE-14-02")]
        [TestCase(99, 40, Category = "AC-BLE-14-02")]
        [TestCase(13, 14, Category = "AC-BLE-14-02")]
        [TestCase(14, 14, Category = "AC-BLE-14-02")]
        [TestCase(24, 24, Category = "AC-BLE-14-02")]
        [TestCase(40, 40, Category = "AC-BLE-14-02")]
        [TestCase(41, 40, Category = "AC-BLE-14-02")]
        [TestCase(-5, 14, Category = "AC-BLE-14-02")]
        public void TargetsAreClamped_Spm(int value, int expected)
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false) { TargetSpm = value };
            Assert.That(sim.TargetSpm, Is.EqualTo(expected));
        }

        [Test, Category("AC-BLE-14-02"), Category("AC-BASE-03")]
        public void TargetsAreClamped()
        {
            // Los setters del legacy (int.clamp) con los mismos valores.
            var cases = GoldenVectors.Load("simulator.json").Section("clamp").Items;
            Assert.That(cases.Count, Is.GreaterThan(20));
            foreach (var c in cases)
            {
                var value = (int)c["value"].AsLong();
                var sim = new RowingSimulator(new SequenceRandom(), noise: false);
                var property = c["property"].AsString();
                int actual;
                if (property == "targetWatts")
                {
                    sim.TargetWatts = value;
                    actual = sim.TargetWatts;
                }
                else
                {
                    Assert.That(property, Is.EqualTo("targetSpm"));
                    sim.TargetSpm = value;
                    actual = sim.TargetSpm;
                }

                Assert.That(actual, Is.EqualTo(c["result"].AsLong()), property + " = " + value);
            }
        }

        // legacy: test/rowing_simulator_test.dart:54 'los objetivos se limitan a su rango'
        [TestCase(TestName = "los objetivos se limitan a su rango", Category = "LT-05,AC-BLE-14-02")]
        public void TargetsAreLimitedToTheirRange()
        {
            var sim = new RowingSimulator
            {
                TargetWatts = 9999,
                TargetSpm = 1,
            };
            Assert.That(sim.TargetWatts, Is.EqualTo(RowingSimulator.MaxWatts));
            Assert.That(sim.TargetSpm, Is.EqualTo(RowingSimulator.MinSpm));
        }

        // ---------------------------------------------------------------- AC-BLE-14-03 · split de Concept2

        // Tabla de la spec: split exacto (4 decimales), emitido, su texto y m/s = 500 / exacto.
        [TestCase(30, 226.8031, 227, "03:47", 2.2046, Category = "AC-BLE-14-03")]
        [TestCase(100, 151.8294, 152, "02:32", 3.2932, Category = "AC-BLE-14-03")]
        [TestCase(150, 132.6352, 133, "02:13", 3.7697, Category = "AC-BLE-14-03")]
        [TestCase(180, 124.8145, 125, "02:05", 4.0059, Category = "AC-BLE-14-03")]
        [TestCase(203, 119.9105, 120, "02:00", 4.1698, Category = "AC-BLE-14-03")]
        [TestCase(280, 107.7217, 108, "01:48", 4.6416, Category = "AC-BLE-14-03")]
        [TestCase(300, 105.2727, 105, "01:45", 4.7496, Category = "AC-BLE-14-03")]
        [TestCase(500, 88.7904, 89, "01:29", 5.6312, Category = "AC-BLE-14-03")]
        public void Concept2Split(int watts, double exact, int emitted, string text, double metersPerSecond)
        {
            var pace = RowingSimulator.PaceForWatts(watts);
            Assert.That(pace, Is.EqualTo(exact).Within(0.0000501), "500 · (2,80 / W)^(1/3) con 4 decimales");
            Assert.That(500 / pace, Is.EqualTo(metersPerSecond).Within(0.0000501));

            var sim = new RowingSimulator(new SequenceRandom(), noise: false) { TargetWatts = watts };
            var reading = sim.Tick();
            Assert.That(reading.PowerWatts, Is.EqualTo(watts));
            Assert.That(reading.Pace500mSeconds, Is.EqualTo(emitted), "el split emitido es el redondeo del exacto");
            Assert.That(reading.Pace500mFormatted, Is.EqualTo(text));
            Assert.That(reading.DistanceMeters, Is.EqualTo((int)Math.Floor(500 / pace + 1e-6)));
        }

        // legacy: test/rowing_simulator_test.dart:7 'más watts → pace más rápido'
        [TestCase(TestName = "más watts → pace más rápido", Category = "LT-01,AC-BLE-14-03")]
        public void MoreWattsFasterPace()
        {
            Assert.That(RowingSimulator.PaceForWatts(300), Is.LessThan(RowingSimulator.PaceForWatts(150)));
        }

        // legacy: test/rowing_simulator_test.dart:14 '203 W ≈ 2:00 /500m (fórmula Concept2)'
        [TestCase(TestName = "203 W ≈ 2:00 /500m (fórmula Concept2)", Category = "LT-02,AC-BLE-14-03")]
        public void TwoHundredThreeWattsIsTwoMinutes()
        {
            Assert.That(RowingSimulator.PaceForWatts(203), Is.EqualTo(120).Within(0.5));
        }

        [Test, Category("AC-BLE-14-03"), Category("AC-BLE-14-04"), Category("AC-BASE-03")]
        public void MatchesOracle_Pace()
        {
            // paceForWatts con Within(1e-9) (R-T4) y su redondeo exacto; caloriesPerSecond bit a bit (sin pow). W en [1; 600].
            var bitExact = 0;
            var rows = GoldenVectors.CheckRows("simulator.json", "concept2", row =>
            {
                var watts = (int)row.Int("watts");
                var pace = RowingSimulator.PaceForWatts(watts);
                var expectedPace = row.F64("pace");
                if (BitConverter.DoubleToInt64Bits(pace) == BitConverter.DoubleToInt64Bits(expectedPace))
                {
                    bitExact++;
                }

                if (Math.Abs(pace - expectedPace) > PowTolerance)
                {
                    return "pace esperado " + Fmt.DartToString(expectedPace) + ", obtenido " + Fmt.DartToString(pace);
                }

                if (DartMath.Round(pace) != row.Int("paceRounded"))
                {
                    return "pace redondeado esperado " + row.Int("paceRounded") + ", obtenido " + DartMath.Round(pace);
                }

                var kcal = RowingSimulator.CaloriesPerSecond(watts);
                var expectedKcal = row.F64("kcalPerSecond");
                return BitConverter.DoubleToInt64Bits(kcal) == BitConverter.DoubleToInt64Bits(expectedKcal)
                    ? null
                    : "kcal/s esperado " + Fmt.DartToString(expectedKcal) + ", obtenido " + Fmt.DartToString(kcal);
            });
            Assert.That(rows, Is.EqualTo(600));
            TestContext.Out.WriteLine("R-T4: Math.Pow = pow de Dart bit a bit en " + bitExact + " de " + rows + " potencias");
        }

        // ---------------------------------------------------------------- AC-BLE-14-04 · calorías de Concept2

        [TestCase(30, 403.248, Category = "AC-BLE-14-04")]
        [TestCase(100, 644.16, Category = "AC-BLE-14-04")]
        [TestCase(150, 816.24, Category = "AC-BLE-14-04")]
        [TestCase(180, 919.488, Category = "AC-BLE-14-04")]
        [TestCase(203, 998.6448, Category = "AC-BLE-14-04")]
        [TestCase(280, 1263.648, Category = "AC-BLE-14-04")]
        [TestCase(300, 1332.48, Category = "AC-BLE-14-04")]
        [TestCase(500, 2020.8, Category = "AC-BLE-14-04")]
        public void Concept2Calories(int watts, double kcalPerHour)
        {
            // (W · 4 · 0,8604 + 300) / 3600 kcal por segundo.
            Assert.That(RowingSimulator.CaloriesPerSecond(watts) * 3600, Is.EqualTo(kcalPerHour).Within(1e-9));
            Assert.That(RowingSimulator.CaloriesPerSecond(watts), Is.EqualTo((watts * 4 * 0.8604 + 300) / 3600));
        }

        // ---------------------------------------------------------------- AC-BLE-14-05 · remando sin ruido

        [Test, Category("AC-BLE-14-05"), Category("Demo")]
        public void RowingWithoutNoise()
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false) { TargetWatts = 150, TargetSpm = 24 };
            var heartRates = new List<int>();
            var ticks = new Dictionary<int, RowingData>();
            for (var t = 1; t <= 600; t++)
            {
                var reading = sim.Tick();
                if (t <= 12)
                {
                    heartRates.Add(reading.HeartRate);
                }

                ticks[t] = reading;
            }

            AssertReading(ticks[1], 24.0, 0, 3, 133, 150, 0, 77, 1, "tick 1");
            AssertReading(ticks[60], 24.0, 24, 226, 133, 150, 13, 142, 60, "tick 60");
            AssertReading(ticks[120], 24.0, 48, 452, 133, 150, 27, 142, 120, "tick 120");
            AssertReading(ticks[600], 24.0, 240, 2261, 133, 150, 136, 142, 600, "tick 600");
            Assert.That(heartRates, Is.EqualTo(new[] { 77, 84, 90, 95, 100, 104, 108, 111, 114, 117, 120, 122 }));

            // Línea de la demo (02-tech-design §8.7).
            var d = ticks[60];
            TestContext.Out.WriteLine("[DEMO] Simulador 150 W / 24 spm sin ruido, tick 60 -> spm=" + Fmt.Fixed(d.StrokeRate, 1)
                + " paladas=" + Fmt.Integer(d.StrokeCount)
                + " m=" + Fmt.Integer(d.DistanceMeters)
                + " split=" + Fmt.Integer(d.Pace500mSeconds) + " (" + d.Pace500mFormatted + ")"
                + " W=" + Fmt.Integer(d.PowerWatts)
                + " kcal=" + Fmt.Integer(d.TotalCalories)
                + " bpm=" + Fmt.Integer(d.HeartRate)
                + " t=" + Fmt.Integer(d.ElapsedSeconds) + " (" + d.ElapsedFormatted + ")");
        }

        // legacy: test/rowing_simulator_test.dart:18 'acumula distancia, remadas y tiempo mientras rema'
        [TestCase(TestName = "acumula distancia, remadas y tiempo mientras rema", Category = "LT-03,AC-BLE-14-05")]
        public void AccumulatesWhileRowing()
        {
            var sim = new RowingSimulator(noise: false)
            {
                TargetWatts = 150,
                TargetSpm = 24,
            };
            RowingData d = RowingData.Empty;
            for (var i = 0; i < 60; i++)
            {
                d = sim.Tick();
            }

            Assert.That(d.PowerWatts, Is.EqualTo(150));
            Assert.That(d.StrokeRate, Is.EqualTo(24));
            Assert.That(d.Pace500mSeconds, Is.EqualTo(133)); // 132.6 s redondeado
            Assert.That(d.ElapsedSeconds, Is.EqualTo(60));
            Assert.That(d.StrokeCount, Is.EqualTo(24));
            Assert.That(d.DistanceMeters, Is.EqualTo(226)); // 60 * 500 / 132.6
            Assert.That(d.TotalCalories, Is.EqualTo(13)); // 60 * (150*4*0.8604 + 300) / 3600
            Assert.That(d.HeartRate, Is.GreaterThan(70));
        }

        [Test, Category("AC-BLE-14-05"), Category("AC-BASE-03")]
        public void MatchesOracle_NoNoise600()
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false) { TargetWatts = 150, TargetSpm = 24 };
            var expectedTick = 0;
            var rows = GoldenVectors.CheckRows("simulator.json", "noNoise600", row =>
            {
                expectedTick++;
                if (row.Int("tick") != expectedTick)
                {
                    return "tick fuera de orden";
                }

                return TickDiff(sim.Tick(), sim, row);
            });
            Assert.That(rows, Is.EqualTo(600));
        }

        [Test, Category("AC-BLE-14-05"), Category("AC-BASE-03")]
        public void MatchesOracle_NoNoiseTargets()
        {
            // 30/14, 203/30 y 500/40: 120 ticks cada uno.
            RowingSimulator? sim = null;
            var simulators = 0;
            var rows = GoldenVectors.CheckRows("simulator.json", "noNoiseTargets", row =>
            {
                if (row.Int("tick") == 1)
                {
                    sim = new RowingSimulator(new SequenceRandom(), noise: false)
                    {
                        TargetWatts = (int)row.Int("targetWatts"),
                        TargetSpm = (int)row.Int("targetSpm"),
                    };
                    simulators++;
                }

                if (sim == null)
                {
                    return "la sección no empieza en el tick 1";
                }

                return TickDiff(sim.Tick(), sim, row);
            });
            Assert.That(rows, Is.EqualTo(360));
            Assert.That(simulators, Is.EqualTo(3));
        }

        [TestCase(30, 14, Category = "AC-BLE-14-05")]
        [TestCase(203, 30, Category = "AC-BLE-14-05")]
        [TestCase(500, 40, Category = "AC-BLE-14-05")]
        [TestCase(150, 24, Category = "AC-BLE-14-05")]
        public void WithoutNoiseEmitsExactlyTheTargets(int watts, int spm)
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false) { TargetWatts = watts, TargetSpm = spm };
            for (var t = 1; t <= 120; t++)
            {
                var reading = sim.Tick();
                Assert.That(reading.PowerWatts, Is.EqualTo(watts));
                Assert.That(reading.StrokeRate, Is.EqualTo((double)spm));
                Assert.That(reading.Pace500mSeconds, Is.EqualTo((int)DartMath.Round(RowingSimulator.PaceForWatts(watts))));
                Assert.That(reading.ElapsedSeconds, Is.EqualTo(t));
            }
        }

        // ---------------------------------------------------------------- AC-BLE-14-06 · dejar de remar

        [Test, Category("AC-BLE-14-06")]
        public void StopAndResume()
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false);
            RowingData reading = RowingData.Empty;
            for (var t = 0; t < 11; t++)
            {
                reading = sim.Tick();
            }

            AssertReading(reading, 24.0, 4, 41, 133, 150, 2, 120, 11, "11 ticks remando");

            sim.Rowing = false;
            var stopped = sim.Tick();
            AssertReading(stopped, 0.0, 4, 41, 0, 0, 2, 115, 11, "sin remar");
            Assert.That(stopped.Pace500mFormatted, Is.EqualTo("--:--"));

            sim.Rowing = true;
            var resumed = sim.Tick();
            Assert.That(resumed.DistanceMeters, Is.EqualTo(45));
            Assert.That(resumed.ElapsedSeconds, Is.EqualTo(12));
            Assert.That(resumed.HeartRate, Is.EqualTo(118));
        }

        // legacy: test/rowing_simulator_test.dart:36 'sin remar: 0 spm/watts/pace y totales congelados'
        [TestCase(TestName = "sin remar: 0 spm/watts/pace y totales congelados", Category = "LT-04,AC-BLE-14-06")]
        public void NotRowingFreezesTotals()
        {
            var sim = new RowingSimulator(noise: false);
            for (var i = 0; i < 10; i++)
            {
                sim.Tick();
            }

            var before = sim.Tick();
            sim.Rowing = false;
            var after = sim.Tick();
            Assert.That(after.StrokeRate, Is.EqualTo(0));
            Assert.That(after.PowerWatts, Is.EqualTo(0));
            Assert.That(after.Pace500mSeconds, Is.EqualTo(0));
            Assert.That(after.DistanceMeters, Is.EqualTo(before.DistanceMeters));
            Assert.That(after.StrokeCount, Is.EqualTo(before.StrokeCount));
            Assert.That(after.ElapsedSeconds, Is.EqualTo(before.ElapsedSeconds));
            Assert.That(after.TotalCalories, Is.EqualTo(before.TotalCalories));
            Assert.That(after.HeartRate, Is.LessThan(before.HeartRate));
        }

        [Test, Category("AC-BLE-14-06"), Category("AC-BASE-03")]
        public void MatchesOracle_StopResume()
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false);
            var ticks = GoldenVectors.Load("simulator.json").Section("stopResume").Items;
            Assert.That(ticks.Count, Is.EqualTo(13));
            foreach (var tick in ticks)
            {
                sim.Rowing = tick["rowing"].AsBool();
                var reading = sim.Tick();
                var label = "tick " + tick["tick"].AsLong();
                AssertFields(reading, sim, tick["out"], label);
            }
        }

        // ---------------------------------------------------------------- AC-BLE-14-07 · pulso

        [TestCase(30, 100.5, Category = "AC-BLE-14-07")]
        [TestCase(100, 125.0, Category = "AC-BLE-14-07")]
        [TestCase(150, 142.5, Category = "AC-BLE-14-07")]
        [TestCase(180, 153.0, Category = "AC-BLE-14-07")]
        [TestCase(203, 161.05, Category = "AC-BLE-14-07")]
        [TestCase(280, 188.0, Category = "AC-BLE-14-07")]
        [TestCase(300, 190.0, Category = "AC-BLE-14-07")]
        [TestCase(500, 190.0, Category = "AC-BLE-14-07")]
        public void HeartRateTargetAndApproach(int watts, double steady)
        {
            // Objetivo mín(190; 90 + 0,35 · W) remando y 70 sin remar; cada tick avanza el 10 % de la distancia al objetivo.
            Assert.That(RowingSimulator.HeartRateTarget(true, watts), Is.EqualTo(steady).Within(1e-9));
            Assert.That(RowingSimulator.HeartRateTarget(false, watts), Is.EqualTo(70.0));

            var sim = new RowingSimulator(new SequenceRandom(), noise: false) { TargetWatts = watts };
            for (var t = 1; t <= 400; t++)
            {
                var previous = sim.ExactHeartRate;
                var reading = sim.Tick();
                var expected = previous + (RowingSimulator.HeartRateTarget(true, watts) - previous) * 0.1;
                Assert.That(sim.ExactHeartRate, Is.EqualTo(expected), "tick " + t);
                Assert.That(reading.HeartRate, Is.EqualTo((int)DartMath.Round(expected)), "tick " + t);
            }

            Assert.That(sim.ExactHeartRate, Is.EqualTo(steady).Within(1e-6), "valor estable");

            // Sin remar baja hacia 70 con la misma regla.
            sim.Rowing = false;
            for (var t = 1; t <= 400; t++)
            {
                var previous = sim.ExactHeartRate;
                sim.Tick();
                Assert.That(sim.ExactHeartRate, Is.EqualTo(previous + (70.0 - previous) * 0.1));
            }

            Assert.That(sim.ExactHeartRate, Is.EqualTo(70.0).Within(1e-6));
        }

        [Test, Category("AC-BLE-14-07")]
        public void HeartRateTargetUsesTheNoisyPowerOfThatTick()
        {
            // W = la potencia de ese tick, con ruido: u = -1 da 190 W con objetivo 200 → objetivo de pulso 156,5.
            var sim = new RowingSimulator(new SequenceRandom(0.0, 0.5)) { TargetWatts = 200 };
            var reading = sim.Tick();
            Assert.That(reading.PowerWatts, Is.EqualTo(190));
            Assert.That(sim.ExactHeartRate, Is.EqualTo(70.0 + (RowingSimulator.HeartRateTarget(true, 190) - 70.0) * 0.1));
            Assert.That(RowingSimulator.HeartRateTarget(true, 190), Is.EqualTo(156.5).Within(1e-9));
        }

        // ---------------------------------------------------------------- AC-BLE-14-08 · pulso expuesto

        // legacy: test/rowing_simulator_test.dart:70 'heartRate expone el último pulso calculado por tick()'
        [TestCase(TestName = "heartRate expone el último pulso calculado por tick()", Category = "LT-07,AC-BLE-14-08")]
        public void HeartRateExposesTheLastTickPulse()
        {
            var sim = new RowingSimulator(noise: false);
            var d = sim.Tick();
            Assert.That(sim.HeartRate, Is.EqualTo(d.HeartRate));
            Assert.That(sim.HeartRate, Is.GreaterThan(70));
        }

        [Test, Category("AC-BLE-14-08")]
        public void ExposedHeartRateIsTheReadingOne()
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false);
            var first = sim.Tick();
            Assert.That(first.HeartRate, Is.EqualTo(77));
            Assert.That(sim.HeartRate, Is.EqualTo(77));
            for (var t = 2; t <= 100; t++)
            {
                sim.Rowing = t % 30 < 20;
                var reading = sim.Tick();
                Assert.That(sim.HeartRate, Is.EqualTo(reading.HeartRate), "tick " + t);
            }
        }

        // ---------------------------------------------------------------- AC-BLE-14-09 · ruido

        [Test, Category("AC-BLE-14-09")]
        public void NoiseRanges()
        {
            // Con azar real: objetivo 200 W → [190; 210]; 24 spm → [23.0; 25.0].
            var sim = new RowingSimulator(new SystemRandom(2026)) { TargetWatts = 200, TargetSpm = 24 };
            for (var t = 1; t <= 1000; t++)
            {
                var reading = sim.Tick();
                Assert.That(reading.PowerWatts, Is.InRange(190, 210), "tick " + t);
                Assert.That(reading.StrokeRate, Is.InRange(23.0, 25.0), "tick " + t);
            }

            // Extremos de u: 0 → u = -1; el mayor double menor que 1 → u ≈ +1.
            const double AlmostOne = 0.99999999999999989;
            var low = new RowingSimulator(new SequenceRandom(0.0, 0.0)) { TargetWatts = 200, TargetSpm = 24 }.Tick();
            Assert.That(low.PowerWatts, Is.EqualTo(190));
            Assert.That(low.StrokeRate, Is.EqualTo(23.0));
            var high = new RowingSimulator(new SequenceRandom(AlmostOne, AlmostOne)) { TargetWatts = 200, TargetSpm = 24 }.Tick();
            Assert.That(high.PowerWatts, Is.EqualTo(210));
            Assert.That(high.StrokeRate, Is.EqualTo(25.0));
            var middle = new RowingSimulator(new SequenceRandom(0.5, 0.5)) { TargetWatts = 200, TargetSpm = 24 }.Tick();
            Assert.That(middle.PowerWatts, Is.EqualTo(200), "u = 0: exactamente el objetivo");
            Assert.That(middle.StrokeRate, Is.EqualTo(24.0));
        }

        [Test, Category("AC-BLE-14-09"), Category("AC-BASE-07")]
        public void NoiseCallOrderIsWattsThenSpm()
        {
            // Primero vatios, después spm, un valor independiente para cada uno (rowing_simulator.dart:46-47).
            const double AlmostOne = 0.99999999999999989;
            var random = new SequenceRandom(0.0, AlmostOne, AlmostOne, 0.0);
            var sim = new RowingSimulator(random) { TargetWatts = 200, TargetSpm = 24 };
            var first = sim.Tick();
            Assert.That(random.Calls, Is.EqualTo(2));
            Assert.That(first.PowerWatts, Is.EqualTo(190));
            Assert.That(first.StrokeRate, Is.EqualTo(25.0));
            var second = sim.Tick();
            Assert.That(random.Calls, Is.EqualTo(4));
            Assert.That(second.PowerWatts, Is.EqualTo(210));
            Assert.That(second.StrokeRate, Is.EqualTo(23.0));

            // Sin remar no se consume azar.
            sim.Rowing = false;
            sim.Tick();
            Assert.That(random.Calls, Is.EqualTo(4));
        }

        // legacy: test/rowing_simulator_test.dart:62 'con ruido, los watts quedan dentro de ±5 %'
        [TestCase(TestName = "con ruido, los watts quedan dentro de ±5 %", Category = "LT-06,AC-BLE-14-09")]
        public void NoisyWattsWithinFivePercent()
        {
            var sim = new RowingSimulator { TargetWatts = 200 };
            for (var i = 0; i < 100; i++)
            {
                Assert.That(sim.Tick().PowerWatts, Is.InRange(190, 210));
            }
        }

        [Test, Category("AC-BLE-14-09"), Category("AC-BASE-03"), Category("AC-BASE-07")]
        public void MatchesOracle_NoiseReplay()
        {
            // El oráculo guardó, por tick, los nextDouble() que consumió el legacy (null si no remaba). La misma secuencia en
            // un SequenceRandom tiene que dar los mismos ticks: mismas fórmulas y mismo orden de llamadas.
            var table = GoldenVectors.Load("simulator.json").Table("noiseReplay");
            var byCase = new List<KeyValuePair<string, List<GoldenRow>>>();
            for (var i = 0; i < table.Count; i++)
            {
                var row = table[i];
                var name = row.Text("case");
                if (byCase.Count == 0 || byCase[byCase.Count - 1].Key != name)
                {
                    byCase.Add(new KeyValuePair<string, List<GoldenRow>>(name, new List<GoldenRow>()));
                }

                byCase[byCase.Count - 1].Value.Add(row);
            }

            Assert.That(byCase.Count, Is.EqualTo(3));
            var differences = new List<string>();
            foreach (var group in byCase)
            {
                var values = new List<double>();
                foreach (var row in group.Value)
                {
                    var rWatts = row.Cell("rWatts");
                    var rSpm = row.Cell("rSpm");
                    Assert.That(rWatts.IsNull, Is.EqualTo(row.Int("rowing") == 0), group.Key + " " + row);
                    if (!rWatts.IsNull)
                    {
                        values.Add(rWatts.AsF64());
                        values.Add(rSpm.AsF64());
                    }
                }

                var random = new SequenceRandom(values.ToArray());
                var first = group.Value[0];
                var sim = new RowingSimulator(random) { TargetWatts = (int)first.Int("targetWatts"), TargetSpm = (int)first.Int("targetSpm") };
                foreach (var row in group.Value)
                {
                    sim.Rowing = row.Int("rowing") == 1;
                    var diff = TickDiff(sim.Tick(), sim, row);
                    if (diff != null && differences.Count < 20)
                    {
                        differences.Add(group.Key + " tick " + row.Int("tick") + ": " + diff);
                    }
                }

                Assert.That(random.Remaining, Is.EqualTo(0), group.Key + ": tiene que consumir la secuencia entera");
            }

            Assert.That(differences, Is.Empty, string.Join("\n", differences));
        }

        // ---------------------------------------------------------------- AC-BLE-14-10 · redondeos de la salida

        [Test, Category("AC-BLE-14-10")]
        public void OutputRounding()
        {
            // Ritmo a 1 decimal como Dart: 24,25 → 24.3 (empate exacto, se aleja del cero) y 23,95 → 23.9 (valor binario).
            Assert.That(24 + (0.625 * 2 - 1) * 1.0, Is.EqualTo(24.25), "precondición: u = 0,25");
            var up = new RowingSimulator(new SequenceRandom(0.5, 0.625)).Tick();
            Assert.That(up.StrokeRate, Is.EqualTo(24.3));
            Assert.That(24 + (0.475 * 2 - 1) * 1.0, Is.EqualTo(23.95), "precondición: u = -0,05");
            var down = new RowingSimulator(new SequenceRandom(0.5, 0.475)).Tick();
            Assert.That(down.StrokeRate, Is.EqualTo(23.9));

            // Paladas, distancia y kcal: parte entera con 1e-6 de tolerancia (60 ticks a 24 spm = 23,99999999999998 → 24).
            var raw = 0.0;
            for (var i = 0; i < 60; i++)
            {
                raw += 24.0 / 60;
            }

            Assert.That(raw, Is.LessThan(24.0), "precondición: la suma en double queda debajo de 24");
            Assert.That(Math.Floor(raw), Is.EqualTo(23.0));
            var sim = new RowingSimulator(new SequenceRandom(), noise: false);
            RowingData reading = RowingData.Empty;
            for (var i = 0; i < 60; i++)
            {
                reading = sim.Tick();
            }

            Assert.That(reading.StrokeCount, Is.EqualTo(24));

            // Pulso: al entero más cercano y la mitad se aleja del cero. 1 tick a 100 W → 75,5 → 76; 200 W y después
            // 240 W → 88,5 → 89 (redondear al par daría 88).
            var half = new RowingSimulator(new SequenceRandom(), noise: false) { TargetWatts = 100 };
            var halfReading = half.Tick();
            Assert.That(half.ExactHeartRate, Is.EqualTo(75.5));
            Assert.That(halfReading.HeartRate, Is.EqualTo(76));
            var even = new RowingSimulator(new SequenceRandom(), noise: false) { TargetWatts = 200 };
            even.Tick();
            even.TargetWatts = 240;
            var evenReading = even.Tick();
            Assert.That(even.ExactHeartRate, Is.EqualTo(88.5));
            Assert.That(evenReading.HeartRate, Is.EqualTo(89));
            Assert.That(even.HeartRate, Is.EqualTo(89));

            // Tiempo: los segundos remados (sin remar no suma).
            even.Rowing = false;
            Assert.That(even.Tick().ElapsedSeconds, Is.EqualTo(2));
            even.Rowing = true;
            Assert.That(even.Tick().ElapsedSeconds, Is.EqualTo(3));
        }

        // ---------------------------------------------------------------- AC-BASE-07 · azar controlable

        [Test, Category("AC-BASE-07")]
        public void NoNoiseIsDeterministicAndConsumesNoRandom()
        {
            // Con "sin ruido", AC-BLE-14-05 y AC-BLE-14-06 dan lo mismo en cada corrida y el IRandom no se toca
            // (un SequenceRandom vacío falla en la primera llamada).
            var randomA = new SequenceRandom();
            var randomB = new SequenceRandom();
            var a = new RowingSimulator(randomA, noise: false);
            var b = new RowingSimulator(randomB, noise: false);
            for (var t = 1; t <= 700; t++)
            {
                var rowing = t <= 11 || t > 12;
                a.Rowing = rowing;
                b.Rowing = rowing;
                var ra = a.Tick();
                var rb = b.Tick();
                Assert.That(ra, Is.EqualTo(rb), "tick " + t);
                if (t == 11)
                {
                    AssertReading(ra, 24.0, 4, 41, 133, 150, 2, 120, 11, "tick 11");
                }
                else if (t == 12)
                {
                    AssertReading(ra, 0.0, 4, 41, 0, 0, 2, 115, 11, "tick 12 sin remar");
                }
            }

            Assert.That(randomA.Calls, Is.EqualTo(0));
            Assert.That(randomB.Calls, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------- ayudas

        private static void AssertReading(RowingData d, double spm, int strokes, int meters, int split, int watts, int kcal, int bpm,
            int seconds, string label)
        {
            var expected = new RowingData
            {
                StrokeRate = spm,
                StrokeCount = strokes,
                DistanceMeters = meters,
                Pace500mSeconds = split,
                PowerWatts = watts,
                TotalCalories = kcal,
                HeartRate = bpm,
                ElapsedSeconds = seconds,
            };
            Assert.That(d, Is.EqualTo(expected), label);
        }

        // Diferencia de una lectura contra una fila del oráculo (8 campos, el ritmo por bits, y el pulso expuesto), o null.
        private static string? TickDiff(RowingData d, RowingSimulator sim, GoldenRow row)
        {
            var problems = new List<string>();
            Compare(problems, "strokeRate", BitConverter.DoubleToInt64Bits(d.StrokeRate), BitConverter.DoubleToInt64Bits(row.F64("strokeRate")));
            Compare(problems, "strokeCount", d.StrokeCount, row.Int("strokeCount"));
            Compare(problems, "distanceMeters", d.DistanceMeters, row.Int("distanceMeters"));
            Compare(problems, "pace500mSeconds", d.Pace500mSeconds, row.Int("pace500mSeconds"));
            Compare(problems, "powerWatts", d.PowerWatts, row.Int("powerWatts"));
            Compare(problems, "totalCalories", d.TotalCalories, row.Int("totalCalories"));
            Compare(problems, "heartRate", d.HeartRate, row.Int("heartRate"));
            Compare(problems, "elapsedSeconds", d.ElapsedSeconds, row.Int("elapsedSeconds"));
            Compare(problems, "exposedHeartRate", sim.HeartRate, row.Int("exposedHeartRate"));
            return problems.Count == 0 ? null : string.Join("; ", problems);
        }

        private static void AssertFields(RowingData d, RowingSimulator sim, JsonNode expected, string label)
        {
            Assert.That(BitConverter.DoubleToInt64Bits(d.StrokeRate), Is.EqualTo(BitConverter.DoubleToInt64Bits(expected["strokeRate"].AsF64())),
                label + ": strokeRate");
            Assert.That(d.StrokeCount, Is.EqualTo(expected["strokeCount"].AsLong()), label + ": strokeCount");
            Assert.That(d.DistanceMeters, Is.EqualTo(expected["distanceMeters"].AsLong()), label + ": distanceMeters");
            Assert.That(d.Pace500mSeconds, Is.EqualTo(expected["pace500mSeconds"].AsLong()), label + ": pace500mSeconds");
            Assert.That(d.PowerWatts, Is.EqualTo(expected["powerWatts"].AsLong()), label + ": powerWatts");
            Assert.That(d.TotalCalories, Is.EqualTo(expected["totalCalories"].AsLong()), label + ": totalCalories");
            Assert.That(d.HeartRate, Is.EqualTo(expected["heartRate"].AsLong()), label + ": heartRate");
            Assert.That(d.ElapsedSeconds, Is.EqualTo(expected["elapsedSeconds"].AsLong()), label + ": elapsedSeconds");
            Assert.That(sim.HeartRate, Is.EqualTo(expected["exposedHeartRate"].AsLong()), label + ": exposedHeartRate");
        }

        private static void Compare(List<string> problems, string field, long actual, long expected)
        {
            if (actual != expected)
            {
                problems.Add(field + " esperado " + Fmt.Integer(expected) + ", obtenido " + Fmt.Integer(actual));
            }
        }
    }
}
