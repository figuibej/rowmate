using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Tests.UiSupport
{
    // Sonda de color sobre una toma (02-tech-design §4.2): Point = un píxel del fondo de un elemento en un punto relativo
    // (0..1); Dominant = el color más frecuente que no es el fondo dentro del elemento (el trazo de un número: AC-APP-06-01
    // "medido sobre el trazo del valor"). Tolerancia en unidades por canal (0-255).
    public sealed class PixelProbe
    {
        private PixelProbe(string name, string element, bool dominant, Vector2 relative, string expectedHex, int tolerance)
        {
            Name = name;
            Element = element;
            IsDominant = dominant;
            Relative = relative;
            ExpectedHex = expectedHex;
            Tolerance = tolerance;
        }

        public string Name { get; }

        public string Element { get; }

        public bool IsDominant { get; }

        public Vector2 Relative { get; }

        public string ExpectedHex { get; }

        public int Tolerance { get; }

        public static PixelProbe Point(string name, string element, Vector2 relative, string expectedHex, int tolerance = 1)
        {
            return new PixelProbe(name, element, false, relative, expectedHex, tolerance);
        }

        public static PixelProbe Dominant(string name, string element, string expectedHex, int tolerance = 2)
        {
            return new PixelProbe(name, element, true, Vector2.zero, expectedHex, tolerance);
        }

        // Color esperado de un token translúcido sobre un fondo opaco, compuesto en sRGB (la tubería GammaRt).
        public static string Composite(ColorToken over, ColorToken under)
        {
            var c = Contrast.CompositeSrgb(over, Contrast.Channels(under.Rgb));
            return new Rgb(ToByte(c.R), ToByte(c.G), ToByte(c.B)).ToHex();
        }

        public ProbeResult Measure(VisualElement root, Texture2D frame, double density)
        {
            var element = UiQuery.Resolve(root, Element);
            if (element == null)
            {
                return new ProbeResult(this, "", 255, "no existe #" + Element);
            }

            var bounds = element.worldBound;
            if (!(bounds.width > 0) || !(bounds.height > 0))
            {
                return new ProbeResult(this, "", 255, "#" + Element + " sin tamaño");
            }

            Color32 actual;
            if (!IsDominant)
            {
                var x = Mathf.RoundToInt((float)((bounds.x + bounds.width * Relative.x) * density));
                var y = Mathf.RoundToInt((float)((bounds.y + bounds.height * Relative.y) * density));
                actual = RtReader.PixelTopLeft(frame, x, y);
            }
            else
            {
                actual = DominantStroke(frame, RtReader.ToPixels(bounds, density));
            }

            var hex = new Rgb(actual.r, actual.g, actual.b).ToHex();
            return new ProbeResult(this, hex, Distance(hex, ExpectedHex), null);
        }

        // El color más frecuente que difiere del más frecuente (el fondo) en más de 24 por canal.
        public static Color32 DominantStroke(Texture2D frame, RectInt rectTopLeft)
        {
            var rect = RtReader.Clamp(rectTopLeft, frame.width, frame.height);
            var counts = new Dictionary<int, int>();
            for (var y = rect.y; y < rect.yMax; y++)
            {
                for (var x = rect.x; x < rect.xMax; x++)
                {
                    Color32 c = RtReader.PixelTopLeft(frame, x, y);
                    var key = (c.r << 16) | (c.g << 8) | c.b;
                    counts.TryGetValue(key, out var n);
                    counts[key] = n + 1;
                }
            }

            var background = -1;
            var best = 0;
            foreach (var pair in counts)
            {
                if (pair.Value > best)
                {
                    best = pair.Value;
                    background = pair.Key;
                }
            }

            var stroke = background;
            best = 0;
            foreach (var pair in counts)
            {
                if (pair.Key == background || ChannelDistance(pair.Key, background) <= 24)
                {
                    continue;
                }

                if (pair.Value > best)
                {
                    best = pair.Value;
                    stroke = pair.Key;
                }
            }

            return new Color32((byte)(stroke >> 16), (byte)(stroke >> 8), (byte)stroke, 255);
        }

        public static int Distance(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return 255;
            }

            var x = Rgb.Parse(a);
            var y = Rgb.Parse(b);
            return Math.Max(Math.Abs(x.R - y.R), Math.Max(Math.Abs(x.G - y.G), Math.Abs(x.B - y.B)));
        }

        private static int ChannelDistance(int a, int b)
        {
            return Math.Max(Math.Abs(((a >> 16) & 255) - ((b >> 16) & 255)),
                Math.Max(Math.Abs(((a >> 8) & 255) - ((b >> 8) & 255)), Math.Abs((a & 255) - (b & 255))));
        }

        private static byte ToByte(double channel)
        {
            var rounded = (int)Math.Floor(channel + 0.5);
            return (byte)(rounded < 0 ? 0 : rounded > 255 ? 255 : rounded);
        }
    }

    public sealed class ProbeResult
    {
        public ProbeResult(PixelProbe probe, string actual, int difference, string? error)
        {
            Probe = probe;
            Actual = actual;
            Difference = difference;
            Error = error;
        }

        public PixelProbe Probe { get; }

        public string Actual { get; }

        public int Difference { get; }

        public string? Error { get; }

        public bool Pass => Error == null && Difference <= Probe.Tolerance;

        public override string ToString()
        {
            return Probe.Name + " (#" + Probe.Element + "): " + (Error ?? Actual) + " vs " + Probe.ExpectedHex + " ±" + Probe.Tolerance
                   + (Pass ? " ok" : " FALLA");
        }
    }
}
