using System;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Workout;

namespace RowMate.Tests.EditMode.Design
{
    // CVD: los cuatro aspectos de la píldora se distinguen también sin el matiz (AC-WKT-15-06; 02-tech-design §6.2 de la It. 4b):
    // sobre el vidrio de la barra encima del cielo provisorio (#08162A, 04-design-notes §2.3), pasados por las matrices de
    // deuteranopía y protanopía de Machado, Oliveira y Fernandes (2009, severidad 1, en RGB lineal), la pendiente, la pista de la
    // en curso, la sólida y su relleno quedan separados por luminancia, y la hueca y la en curso se distinguen además por la forma
    // (el borde y el relleno). QA lo mira también en CAP-01 con la simulación.
    [TestFixture]
    public sealed class PillColorVisionTests
    {
        private static readonly double[,] Protanopia =
        {
            { 0.152286, 1.052583, -0.204868 },
            { 0.114503, 0.786281, 0.099216 },
            { -0.003882, -0.048116, 1.051998 },
        };

        private static readonly double[,] Deuteranopia =
        {
            { 0.367322, 0.860646, -0.227968 },
            { 0.280085, 0.672501, 0.047413 },
            { -0.011820, 0.042940, 0.968881 },
        };

        private static readonly StepType[] Types = { StepType.Work, StepType.Rest, StepType.Warmup, StepType.Cooldown };

        [Test, Category("AC-WKT-15-06")]
        public void FourLooksDifferUnderDeuteranopiaAndProtanopia()
        {
            var glass = Contrast.CompositeSrgb(Tokens.GlassFallback, Contrast.Channels(Tokens.BackdropSky.Rgb));
            foreach (var matrix in new[] { Protanopia, Deuteranopia })
            {
                foreach (var type in Types)
                {
                    var color = StepTypeInfo.Color(type).Rgb;
                    var pending = Luminance(Mix(color, StepPills.Paint(StepPillLook.Pending, 0).TrackAlpha, glass), matrix);
                    var track = Luminance(Mix(color, StepPills.Paint(StepPillLook.Current, 0.5).TrackAlpha, glass), matrix);
                    var fill = Luminance(Mix(color, 1, glass), matrix);
                    var solid = Luminance(Mix(color, StepPills.Paint(StepPillLook.Solid, 0).TrackAlpha, glass), matrix);
                    var hollowInterior = Mix(color, StepPills.Paint(StepPillLook.Hollow, 0).TrackAlpha, glass);
                    var hollowBorder = Luminance(Mix(color, StepPills.Paint(StepPillLook.Hollow, 0).BorderAlpha, hollowInterior), matrix);
                    var name = type + (ReferenceEquals(matrix, Protanopia) ? " (protanopía)" : " (deuteranopía)");

                    // Por luminancia: pendiente < pista < sólida, con contraste entre la tenue y la hecha.
                    Assert.That(track, Is.GreaterThan(pending), name + ": la pista sobre la pendiente");
                    Assert.That(solid, Is.GreaterThan(track), name + ": la sólida sobre la pista");
                    Assert.That(Contrast.Ratio(solid, pending), Is.GreaterThanOrEqualTo(2.0), name + ": la hecha contra la que falta");
                    Assert.That(Contrast.Ratio(fill, track), Is.GreaterThanOrEqualTo(1.8), name + ": el relleno contra su pista");
                    Assert.That(Contrast.Ratio(hollowBorder, Luminance(hollowInterior, matrix)), Is.GreaterThanOrEqualTo(2.0),
                        name + ": el borde de la hueca contra su interior");
                }
            }

            // Por forma: solo la hueca tiene borde, solo la en curso tiene relleno.
            Assert.That(StepPills.Paint(StepPillLook.Hollow, 0).HasBorder, Is.True);
            Assert.That(StepPills.Paint(StepPillLook.Solid, 0).HasBorder, Is.False);
            Assert.That(StepPills.Paint(StepPillLook.Current, 0.5).HasFill, Is.True);
            Assert.That(StepPills.Paint(StepPillLook.Pending, 0.5).HasFill, Is.False);
        }

        private static (double R, double G, double B) Mix(Rgb color, double alpha, (double R, double G, double B) under)
        {
            return (alpha * color.R + (1 - alpha) * under.R, alpha * color.G + (1 - alpha) * under.G, alpha * color.B + (1 - alpha) * under.B);
        }

        private static double Luminance((double R, double G, double B) srgb, double[,] m)
        {
            var r = Linear(srgb.R);
            var g = Linear(srgb.G);
            var b = Linear(srgb.B);
            var sr = Clamp(m[0, 0] * r + m[0, 1] * g + m[0, 2] * b);
            var sg = Clamp(m[1, 0] * r + m[1, 1] * g + m[1, 2] * b);
            var sb = Clamp(m[2, 0] * r + m[2, 1] * g + m[2, 2] * b);
            return 0.2126 * sr + 0.7152 * sg + 0.0722 * sb;
        }

        private static double Linear(double channel)
        {
            var c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        private static double Clamp(double x)
        {
            return x < 0 ? 0 : x > 1 ? 1 : x;
        }
    }
}
