using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // PIL: la regla de cada píldora (02-tech-design §2.1, §6.3 de la It. 4b; F-WKT-15): el aspecto por índice y resultado, si se
    // llena y su pintura sobre el color base del tipo. legacy: lib/features/workout/step_pill.dart:6, :30-76;
    // immersive_workout_screen.dart:363-375; workout_provider.dart:31-36.
    [TestFixture]
    public sealed class StepPillsTests
    {
        // legacy: test/step_pill_test.dart:7 (const _work = IntervalStep(type: work, durationSeconds: 60)).
        private static readonly IntervalStep Work = RoutineSeeds.Step(StepType.Work, dur: 60);

        // legacy: test/step_pill_test.dart:19 'pendiente, en curso, hecho y no cumplido se pintan distinto'
        // QA-G4b-P08: el puerto no elige aspectos. Arma las cinco píldoras de :20-26 con LookOf(índice, en curso = 3, resultado)
        // —la regla "hecha sin resultado = cumplida" del switch de step_pill.dart:43-55 vive ahí— y compara LO QUE SE PINTA: el
        // color final (el base de StepTypeInfo.Color(Work) con el alfa de Paint, como color.withValues(alpha:)), el borde y el
        // relleno. Mapeo: p = LookOf(4, 3, null); a = LookOf(3, 3, null) con progreso 0,5; d = LookOf(0, 3, Met);
        // m = LookOf(1, 3, Missed); n = LookOf(2, 3, null).
        [TestCase(TestName = "pendiente, en curso, hecho y no cumplido se pintan distinto",
            Category = "LT-46,AC-WKT-15-02,AC-WKT-15-03,AC-WKT-15-04,AC-WKT-15-05")]
        public void PendingActiveDoneAndMissedPaintDifferently()
        {
            var p = Pill(StepPills.LookOf(4, 3, null), 0);
            var a = Pill(StepPills.LookOf(3, 3, null), 0.5);
            var d = Pill(StepPills.LookOf(0, 3, StepResult.Met), 0);
            var m = Pill(StepPills.LookOf(1, 3, StepResult.Missed), 0);
            var n = Pill(StepPills.LookOf(2, 3, null), 0);

            // :35 la hecha cumplida con alfa > 0,8.
            Assert.That(d.Background.A, Is.GreaterThan(0.8));
            // :36 la hecha sin resultado IGUAL a la cumplida (el color entero, RGBA, y el borde).
            Assert.That(n.Background, Is.EqualTo(d.Background));
            Assert.That(n.Paint.BorderAlpha, Is.EqualTo(d.Paint.BorderAlpha));
            Assert.That(n.Paint.BorderWidthBase, Is.EqualTo(d.Paint.BorderWidthBase));
            // :37 la pendiente con alfa < 0,3.
            Assert.That(p.Background.A, Is.LessThan(0.3));
            // :40 el interior de la no cumplida con el alfa de la pendiente.
            Assert.That(m.Background.A, Is.EqualTo(p.Background.A));
            // :41-42 el borde de la no cumplida con alfa > 0,8.
            Assert.That(m.Paint.HasBorder, Is.True);
            Assert.That(m.Border.A, Is.GreaterThan(0.8));
            // :43 la pendiente sin borde.
            Assert.That(p.Paint.HasBorder, Is.False);
            // :44 la hecha sin borde.
            Assert.That(d.Paint.HasBorder, Is.False);
            // :47 la en curso con alfa mayor que la pendiente.
            Assert.That(a.Background.A, Is.GreaterThan(p.Background.A));
            // :48 y menor que la hecha.
            Assert.That(a.Background.A, Is.LessThan(d.Background.A));
            // :49-50 el relleno de la en curso en 0,5.
            Assert.That(a.Paint.FillFraction, Is.EqualTo(0.5));
            // :51 la en curso sin borde.
            Assert.That(a.Paint.HasBorder, Is.False);
            // :52 la pendiente sin relleno.
            Assert.That(p.Paint.HasFill, Is.False);

            // El resultado de un paso que no se hizo no se ve.
            Assert.That(StepPills.LookOf(4, 3, StepResult.Met), Is.EqualTo(StepPillLook.Pending));
        }

        [Test, Category("AC-WKT-14-08")]
        public void LooksByIndex()
        {
            // immersive_workout_screen.dart:367-371: antes del en curso, hecha con su resultado; el en curso; después, pendiente.
            Assert.That(StepPills.LookOf(0, 2, StepResult.Met), Is.EqualTo(StepPillLook.Solid));
            Assert.That(StepPills.LookOf(1, 2, StepResult.Missed), Is.EqualTo(StepPillLook.Hollow));
            Assert.That(StepPills.LookOf(1, 2, null), Is.EqualTo(StepPillLook.Solid), "sin objetivo: sólida (:52-54)");
            Assert.That(StepPills.LookOf(2, 2, null), Is.EqualTo(StepPillLook.Current));
            Assert.That(StepPills.LookOf(2, 2, StepResult.Missed), Is.EqualTo(StepPillLook.Current), "el en curso nunca muestra su resultado");
            Assert.That(StepPills.LookOf(3, 2, null), Is.EqualTo(StepPillLook.Pending));
        }

        [Test, Category("AC-WKT-14-13")]
        public void LastStepStaysCurrent()
        {
            // Q-52: con la rutina terminada el índice queda en el último (workout_provider.dart:203-216): el último sigue en curso
            // y su resultado, que el motor calculó, no se ve.
            Assert.That(StepPills.LookOf(4, 4, StepResult.Met), Is.EqualTo(StepPillLook.Current));
            Assert.That(StepPills.LookOf(4, 4, StepResult.Missed), Is.EqualTo(StepPillLook.Current));
            var full = StepPills.Paint(StepPillLook.Current, 1.0);
            Assert.That(full.FillFraction, Is.EqualTo(1), "llena");
            Assert.That(full.HighlightAlpha, Is.EqualTo(0.25), "con su brillo");
            Assert.That(full.TrackAlpha, Is.EqualTo(0.30), "sin solidificarse");
        }

        [Test, Category("AC-WKT-14-09")]
        public void OnlyTimedStepsFill()
        {
            // workout_provider.dart:31-36 (Q-51): solo se llena un paso por tiempo con duración > 0.
            Assert.That(StepPills.Fills(RoutineSeeds.Step(StepType.Work, dur: 60)), Is.True);
            Assert.That(StepPills.Fills(RoutineSeeds.Step(StepType.Work, dist: 500)), Is.False, "por distancia: la pista sola");
            Assert.That(StepPills.Fills(RoutineSeeds.Step(StepType.Work)), Is.False, "sin meta");
            Assert.That(StepPills.Fills(RoutineSeeds.Step(StepType.Work, dur: 0)), Is.False, "0 s");
            Assert.That(StepPills.Fills(RoutineSeeds.Step(StepType.Work, dur: -5)), Is.False, "duración negativa (DV-37)");
            Assert.That(StepPills.Paint(StepPillLook.Current, 0).HasFill, Is.False, "con 0, nada (:71)");
        }

        [Test, Category("AC-WKT-15-01")]
        public void ColorIsTheBaseOfTheStepType()
        {
            // AC-WKT-15-01: el color base del tipo (theme.dart:56-69); el de un tipo desconocido no se llega a ver.
            Assert.That(StepTypeInfo.Color(StepType.Work).Rgb.ToHex(), Is.EqualTo("#EF476F"));
            Assert.That(StepTypeInfo.Color(StepType.Rest).Rgb.ToHex(), Is.EqualTo("#06D6A0"));
            Assert.That(StepTypeInfo.Color(StepType.Warmup).Rgb.ToHex(), Is.EqualTo("#FFD166"));
            Assert.That(StepTypeInfo.Color(StepType.Cooldown).Rgb.ToHex(), Is.EqualTo("#118AB2"));
            Assert.That(PillRowGeometry.Height, Is.EqualTo(8), "8 de alto (step_pill.dart:59)");
            Assert.That(PillRowGeometry.Radius, Is.EqualTo(4), "radio 4 (:57)");
        }

        [Test, Category("AC-WKT-15-02")]
        public void PendingIsEighteenPercent()
        {
            var pending = StepPills.Paint(StepPillLook.Pending, 0.7);
            Assert.That(pending.TrackAlpha, Is.EqualTo(0.18));
            Assert.That(pending.HasBorder, Is.False);
            Assert.That(pending.HasFill, Is.False, "el progreso solo cuenta para la en curso");
        }

        [Test, Category("AC-WKT-15-03")]
        public void CurrentTrackAndFill()
        {
            var current = StepPills.Paint(StepPillLook.Current, 0.51);
            Assert.That(current.TrackAlpha, Is.EqualTo(0.30));
            Assert.That(current.FillFraction, Is.EqualTo(0.51));
            Assert.That(current.HasBorder, Is.False, "sin marco (commit 3ae0f3a)");
            Assert.That(StepPills.Paint(StepPillLook.Current, 1.7).FillFraction, Is.EqualTo(1), "clamp(progress; 0; 1) (:73)");
            Assert.That(StepPills.Paint(StepPillLook.Current, -0.2).FillFraction, Is.EqualTo(0));
            Assert.That(StepPills.Paint(StepPillLook.Current, double.NaN).FillFraction, Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-15-03")]
        public void HighlightOnlyWithFill()
        {
            // DN4b-19: el brillo de 1·s al 25 % solo en la en curso con relleno; distingue la llena de la hecha (CAP-06).
            Assert.That(StepPills.Paint(StepPillLook.Current, 0.2).HighlightAlpha, Is.EqualTo(0.25));
            Assert.That(StepPills.Paint(StepPillLook.Current, 0).HighlightAlpha, Is.EqualTo(0));
            Assert.That(StepPills.Paint(StepPillLook.Solid, 0).HighlightAlpha, Is.EqualTo(0));
            Assert.That(StepPills.Paint(StepPillLook.Hollow, 0).HighlightAlpha, Is.EqualTo(0));
            Assert.That(StepPills.Paint(StepPillLook.Pending, 0).HighlightAlpha, Is.EqualTo(0));
            Assert.That(MotionSpecs.PillHighlightAlpha, Is.EqualTo(StepPills.HighlightAlpha));
        }

        [Test, Category("AC-WKT-15-04")]
        public void SolidAndNoTargetAreIdentical()
        {
            Assert.That(StepPills.Paint(StepPills.LookOf(0, 1, StepResult.Met), 0), Is.EqualTo(StepPills.Paint(StepPills.LookOf(0, 1, null), 0)));
            Assert.That(StepPills.Paint(StepPillLook.Solid, 0).TrackAlpha, Is.EqualTo(0.85));
        }

        [Test, Category("AC-WKT-15-05")]
        public void HollowInteriorAndBorder()
        {
            var hollow = StepPills.Paint(StepPillLook.Hollow, 0);
            Assert.That(hollow.TrackAlpha, Is.EqualTo(0.18), "el interior como la pendiente");
            Assert.That(hollow.BorderAlpha, Is.EqualTo(0.85));
            Assert.That(hollow.BorderWidthBase, Is.EqualTo(1.5), "1,5 × s, por dentro (:51)");
            Assert.That(hollow.HasFill, Is.False);
        }

        private static Painted Pill(StepPillLook look, double progress)
        {
            var paint = StepPills.Paint(look, progress);
            var color = StepTypeInfo.Color(Work.Type).Rgb;
            return new Painted(paint, new Rgba(color.R, color.G, color.B, paint.TrackAlpha), new Rgba(color.R, color.G, color.B, paint.BorderAlpha));
        }

        private readonly struct Rgba : System.IEquatable<Rgba>
        {
            public Rgba(byte r, byte g, byte b, double a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }

            public byte R { get; }

            public byte G { get; }

            public byte B { get; }

            public double A { get; }

            public bool Equals(Rgba other)
            {
                return R == other.R && G == other.G && B == other.B && A.Equals(other.A);
            }

            public override bool Equals(object? obj)
            {
                return obj is Rgba other && Equals(other);
            }

            public override int GetHashCode()
            {
                return System.HashCode.Combine(R, G, B, A);
            }

            public override string ToString()
            {
                return "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2") + "@" + RowMate.Core.Text.Fmt.Fixed(A, 2);
            }
        }

        private readonly struct Painted
        {
            public Painted(PillPaint paint, Rgba background, Rgba border)
            {
                Paint = paint;
                Background = background;
                Border = border;
            }

            public PillPaint Paint { get; }

            public Rgba Background { get; }

            public Rgba Border { get; }
        }
    }
}
