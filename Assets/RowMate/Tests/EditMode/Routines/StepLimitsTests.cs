using System;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // SLT: topes de Q-92 (AC-RUT-13-11, -12, AC-RUT-14-06, AC-RUT-11-05) y la primera repetición fuera de tope en forma
    // cerrada (sin un bucle por repetición: ×2 000 000 000 cuesta lo mismo que ×2).
    [TestFixture]
    public sealed class StepLimitsTests
    {
        [Test, Category("AC-RUT-13-11")]
        public void FieldCaps()
        {
            Assert.That(StepLimits.MaxDurationSeconds, Is.EqualTo(359999), "99:59:59");
            Assert.That(StepLimits.MaxDistanceMeters, Is.EqualTo(999999));
            Assert.That(StepLimits.MaxWatts, Is.EqualTo(9999));
            Assert.That(StepLimits.MaxSpm, Is.EqualTo(999));
            Assert.That(StepLimits.MaxSplitSeconds, Is.EqualTo(5999), "99:59");
            Assert.That(StepLimits.MaxProgressionSpm, Is.EqualTo(999));
            Assert.That(StepLimits.MaxProgressionWatts, Is.EqualTo(9999));
            Assert.That(StepLimits.MaxProgressionSplitSeconds, Is.EqualTo(5999));
            Assert.That(StepLimits.MaxRepeatCount, Is.EqualTo(999), "P-01");
            Assert.That(StepLimits.DurationMaxText, Is.EqualTo("99:59:59"));
            Assert.That(StepLimits.DistanceMaxText, Is.EqualTo("999999"));
            Assert.That(StepLimits.WattsMaxText, Is.EqualTo("9999"));
            Assert.That(StepLimits.SpmMaxText, Is.EqualTo("999"));
            Assert.That(StepLimits.SplitMaxText, Is.EqualTo("99:59"));
            Assert.That(StepLimits.ProgressionSpmMaxText, Is.EqualTo("999"));
            Assert.That(StepLimits.ProgressionWattsMaxText, Is.EqualTo("9999"));
            Assert.That(StepLimits.ProgressionSplitMaxText, Is.EqualTo("5999"));
            Assert.That(99 * 3600 + 59 * 60 + 59, Is.EqualTo(StepLimits.MaxDurationSeconds));
            Assert.That(99 * 60 + 59, Is.EqualTo(StepLimits.MaxSplitSeconds));
        }

        // AC-RUT-13-12 y AC-RUT-14-06, con los ejemplos de la spec.
        [Test, Category("AC-RUT-13-12"), Category("AC-RUT-14-06")]
        public void FirstRepetitionClosedForm()
        {
            Assert.That(StepLimits.FirstRepetitionOutOfRange(18, 200, 10, 999), Is.EqualTo(6), "18 + 200 × 5 = 1018");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(9990, 10, 10, 9999), Is.EqualTo(2), "9990 + 10 = 10000");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(18, 100, 10, 999), Is.Null, "la 10.ª da 918");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(18, 2, 491, 999), Is.Null, "Escalera SPM ×491: la última da 998");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(18, 2, 492, 999), Is.EqualTo(492), "×492 da 1000");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(18, 2, 999, 999), Is.EqualTo(492));
            Assert.That(StepLimits.FirstRepetitionOutOfRange(125, -2, 3064, 5999), Is.EqualTo(3064), "bajando: 125 − 2 × 3063 = −6001");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(125, -2, 3063, 5999), Is.Null, "125 − 2 × 3062 = −5999");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(999, 1, 1, 999), Is.Null, "con ×1 solo cuenta el objetivo");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(999, 1, 2, 999), Is.EqualTo(2));
            Assert.That(StepLimits.FirstRepetitionOutOfRange(1000, 0, 5, 999), Is.EqualTo(1), "el objetivo ya se pasa");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(-1000, 0, 5, 999), Is.EqualTo(1));
            Assert.That(StepLimits.FirstRepetitionOutOfRange(500, 0, 5, 999), Is.Null, "sin progresión no cambia");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(500, 600, 0, 999), Is.Null, "×0: ninguna repetición");
            Assert.That(StepLimits.FirstRepetitionOutOfRange(500, 600, -3, 999), Is.Null, "×−3: ninguna repetición");
        }

        // Equivale al recorrido repetición por repetición (que es lo que hace la expansión, routine.dart:50-63).
        [Test, Category("AC-RUT-13-12")]
        public void ClosedFormMatchesTheLoop()
        {
            var checkedCases = 0;
            for (var target = -40; target <= 40; target += 7)
            {
                for (var progression = -9; progression <= 9; progression++)
                {
                    for (var repeat = 0; repeat <= 30; repeat += 3)
                    {
                        int? expected = null;
                        for (var r = 1; r <= repeat; r++)
                        {
                            if (Math.Abs(target + (long)progression * (r - 1)) > 35)
                            {
                                expected = r;
                                break;
                            }
                        }

                        Assert.That(StepLimits.FirstRepetitionOutOfRange(target, progression, repeat, 35), Is.EqualTo(expected),
                            target + " " + progression + " ×" + repeat);
                        checkedCases++;
                    }
                }
            }

            Assert.That(checkedCases, Is.GreaterThan(1000));
        }

        // spec §12.3: ×2 000 000 000 sin bucles y sin desbordes (la cuenta no se hace multiplicando).
        [Test, Category("AC-RUT-13-12"), Category("AC-RUT-10-11")]
        public void TwoBillionRepeatsWithoutLoops()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Assert.That(StepLimits.FirstRepetitionOutOfRange(18, 1, 2000000000, 999), Is.EqualTo(983));
            Assert.That(StepLimits.FirstRepetitionOutOfRange(0, 0, 2000000000, 999), Is.Null);
            Assert.That(StepLimits.FirstRepetitionOutOfRange(int.MaxValue, int.MaxValue, int.MaxValue, 999), Is.EqualTo(1));
            Assert.That(StepLimits.FirstRepetitionOutOfRange(-999, int.MinValue, int.MaxValue, 999), Is.EqualTo(2));
            Assert.That(StepLimits.FirstRepetitionOutOfRange(999, long.MaxValue, long.MaxValue, 999), Is.EqualTo(2));
            Assert.That(StepLimits.FirstRepetitionOutOfRange(-999, long.MinValue, long.MaxValue, 999), Is.EqualTo(2));
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(1000));
        }

        // Validación 3 de AC-RUT-11-05 sobre lo guardado, paso por paso.
        [Test, Category("AC-RUT-11-05")]
        public void StoredStepCaps()
        {
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 359999, wMin: -9999, wMax: 9999, spm: 999, split: 5999), 1, false), Is.True);
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 360000), 1, false), Is.False);
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dist: -1000000), 1, false), Is.False);
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, wMin: 50000), 1, false), Is.False, "F-RARA 50 000 W");
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, spm: 1000), 1, false), Is.False);
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, split: 6000), 1, false), Is.False);
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, split: -12000), 1, false), Is.True, "≤ 0 = sin split");
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, spm: 18, pSpm: 5000), 1, false), Is.True,
                "las progresiones de un paso suelto no se controlan (QA-G3-13 c)");
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, spm: 18, pSpm: 5000), 1, true), Is.False, "en una serie, sí");
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, spm: 18, pSpm: 2), 491, true), Is.True);
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, spm: 18, pSpm: 2), 492, true), Is.False);
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, wMax: 9990, pW: 10), 2, true), Is.False, "W máx progresado");
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, split: 125, pSplit: -2), 3064, true), Is.False);
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, pSpm: 900), 999, true), Is.True, "progresión sin objetivo");
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60), 1000, true), Is.False, "más de 999 repeticiones");
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, spm: 18, pSpm: 900), 0, true), Is.True, "×0 no se controla (DT-13)");
            Assert.That(StepLimitCheck.StepIsWithinLimits(Step(StepType.Work, dur: 60, spm: 18, pSpm: 900), -3, true), Is.True, "×−3 tampoco");
        }

        private static IntervalStep Step(StepType type, int? dur = null, int? dist = null, int? wMin = null, int? wMax = null, int? spm = null,
            int? split = null, int? pSpm = null, int? pW = null, int? pSplit = null)
        {
            return RoutineSeeds.Step(type, dur, dist, wMin, wMax, spm, split, null, null, pSpm, pW, pSplit);
        }
    }
}
