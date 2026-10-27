using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Surface;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RowMate.Snapshots
{
    // Medición de R-T1 (CAP-23, AC-BASE-05; 02-tech-design §4.4). Muestra de 400 × 240 dp a densidad 1: un rectángulo blanco
    // con uno de glass.fallback encima; uno de surface.card con uno de text.secondary encima; un texto text.secondary de 15 dp
    // sobre surface.card. Se renderiza dos veces, con la tubería de la app (GammaRt: RT UNorm + forceGammaRendering) y con la
    // directa (Overlay: RT sRGB, = el backbuffer de un player en Overlay, S-0), se lee el píxel central de cada muestra, se
    // calculan los contrastes contra lo medido y se vuelve a renderizar con los valores escritos al pie. El arnés no decide
    // nada: deja los números (rt1-measurement.json) y QA cierra AC-BASE-05.
    public sealed class Rt1Measurement
    {
        public const int Width = 400;
        public const int Height = 240;

        public string AppGlass = "";
        public string AppSecondary = "";
        public string DirectGlass = "";
        public string DirectSecondary = "";
        public Texture2D? AppImage;
        public Texture2D? DirectImage;

        public IEnumerator Run()
        {
            var app = new Pass(UiColorPipeline.GammaRt);
            yield return app.Measure();
            AppGlass = app.Glass;
            AppSecondary = app.Secondary;
            yield return app.Annotate("App (GammaRt): vidrio " + AppGlass + " · secundario " + AppSecondary);
            AppImage = app.Image;
            app.Dispose();

            var direct = new Pass(UiColorPipeline.Overlay);
            yield return direct.Measure();
            DirectGlass = direct.Glass;
            DirectSecondary = direct.Secondary;
            yield return direct.Annotate("Directa (Overlay): vidrio " + DirectGlass + " · secundario " + DirectSecondary);
            DirectImage = direct.Image;
            direct.Dispose();
        }

        // Las dos mitades lado a lado (800 × 240).
        public Texture2D Compose()
        {
            var image = new Texture2D(Width * 2, Height, TextureFormat.RGBA32, false, true);
            image.SetPixels(0, 0, Width, Height, AppImage!.GetPixels());
            image.SetPixels(Width, 0, Width, Height, DirectImage!.GetPixels());
            image.Apply(false);
            return image;
        }

        public string ToJson()
        {
            var white = (255.0, 255.0, 255.0);
            var card = Contrast.Channels(Tokens.SurfaceCard.Rgb);
            var expectedSrgbGlass = Hex(Contrast.CompositeSrgb(Tokens.GlassFallback, white));
            var expectedSrgbSecondary = Hex(Contrast.CompositeSrgb(Tokens.TextSecondary, card));
            var expectedLinearGlass = Hex(Contrast.CompositeLinear(Tokens.GlassFallback, white));
            var expectedLinearSecondary = Hex(Contrast.CompositeLinear(Tokens.TextSecondary, card));
            var glass = Contrast.Channels(Rgb.Parse(AppGlass));
            var verdict = Distance(AppGlass, expectedSrgbGlass) <= 1 ? "sRGB" : Distance(AppGlass, expectedLinearGlass) <= 2 ? "lineal" : "otra";
            var json = new StringBuilder();
            json.Append("{\n");
            json.Append("  \"app\": { \"pipeline\": \"GammaRt\", \"glassOnWhite\": \"").Append(AppGlass).Append("\", \"textSecondaryOnCard\": \"")
                .Append(AppSecondary).Append("\" },\n");
            json.Append("  \"direct\": { \"pipeline\": \"Overlay\", \"glassOnWhite\": \"").Append(DirectGlass).Append("\", \"textSecondaryOnCard\": \"")
                .Append(DirectSecondary).Append("\" },\n");
            json.Append("  \"expectedSrgb\": { \"glassOnWhite\": \"").Append(expectedSrgbGlass).Append("\", \"textSecondaryOnCard\": \"")
                .Append(expectedSrgbSecondary).Append("\" },\n");
            json.Append("  \"expectedLinear\": { \"glassOnWhite\": \"").Append(expectedLinearGlass).Append("\", \"textSecondaryOnCard\": \"")
                .Append(expectedLinearSecondary).Append("\" },\n");
            json.Append("  \"contrast\": { \"text.hudPrimary/glass\": ").Append(Ratio(Tokens.TextHudPrimary, glass))
                .Append(", \"metric.split.hud/glass\": ").Append(Ratio(Tokens.MetricSplitHud, glass))
                .Append(", \"metric.hr.hud/glass\": ").Append(Ratio(Tokens.MetricHrHud, glass))
                .Append(", \"text.secondary(medido)/surface.card\": ").Append(RatioOf(AppSecondary, card)).Append(" },\n");
            json.Append("  \"verdict\": \"").Append(verdict).Append("\"\n}\n");
            return json.ToString();
        }

        // glass.fallback sobre blanco compuesto en sRGB (≈ #2A2F37): lo que espera AC-BASE-05 con la composición del legacy.
        public static string ExpectedSrgbGlass()
        {
            return Hex(Contrast.CompositeSrgb(Tokens.GlassFallback, (255.0, 255.0, 255.0)));
        }

        private static string Ratio(ColorToken front, (double R, double G, double B) background)
        {
            return Contrast.RatioOver(front, background).ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string RatioOf(string measuredHex, (double R, double G, double B) background)
        {
            var c = Contrast.Channels(Rgb.Parse(measuredHex));
            var ratio = Contrast.Ratio(Contrast.RelativeLuminance(c.R, c.G, c.B), Contrast.RelativeLuminance(background.R, background.G, background.B));
            return ratio.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Hex((double R, double G, double B) c)
        {
            return new Rgb(ToByte(c.R), ToByte(c.G), ToByte(c.B)).ToHex();
        }

        private static byte ToByte(double v)
        {
            var r = (int)Math.Floor(v + 0.5);
            return (byte)(r < 0 ? 0 : r > 255 ? 255 : r);
        }

        private static int Distance(string a, string b)
        {
            return PixelProbe.Distance(a, b);
        }

        // Una pasada con una tubería: documento propio con una copia del panel del proyecto (el tema real), RT de 400 × 240.
        private sealed class Pass : IDisposable
        {
            private readonly UiColorPipeline _pipeline;
            private readonly GameObject _host;
            private readonly PanelSettings _panel;
            private readonly RenderTexture _target;
            private readonly VisualElement _root;

            public Pass(UiColorPipeline pipeline)
            {
                _pipeline = pipeline;
                var format = pipeline == UiColorPipeline.GammaRt ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8B8A8_SRGB;
                _target = UiSurface.CreateTarget(Width, Height, format, "RowMate.Rt1." + pipeline);
                _panel = Object.Instantiate(UiAssetsLocator.Panel());
                _panel.hideFlags = HideFlags.HideAndDontSave;
                _panel.targetTexture = _target;
                _panel.scaleMode = PanelScaleMode.ConstantPixelSize;
                _panel.scale = 1;
                _panel.forceGammaRendering = pipeline == UiColorPipeline.GammaRt;
                _panel.clearColor = true;
                _panel.colorClearValue = UiSurface.ClearValue(pipeline);
                _host = new GameObject("RowMate.Rt1." + pipeline) { hideFlags = HideFlags.DontSave };
                _host.SetActive(false);
                var document = _host.AddComponent<UIDocument>();
                document.panelSettings = _panel;
                _host.SetActive(true);
                _root = document.rootVisualElement;
                _root.style.position = Position.Absolute;
                _root.style.left = 0;
                _root.style.top = 0;
                _root.style.right = 0;
                _root.style.bottom = 0;
                var sample = new VisualElement { name = "rt1-sample" };
                sample.AddToClassList("rm-app");
                _root.Add(sample);
                var white = Box("rt1-white", 16, 16, 176, 120, Color.white);
                white.Add(Box("rt1-glass", 28, 30, 120, 60, TokenColors.ToColor(Tokens.GlassFallback)));
                sample.Add(white);
                var card = Box("rt1-card", 208, 16, 176, 120, TokenColors.ToColor(Tokens.SurfaceCard));
                card.Add(Box("rt1-secondary", 28, 30, 120, 60, TokenColors.ToColor(Tokens.TextSecondary)));
                sample.Add(card);
                var textCard = Box("rt1-text-card", 16, 148, 368, 40, TokenColors.ToColor(Tokens.SurfaceCard));
                var text = new Label("text.secondary 15 dp sobre surface.card");
                text.AddToClassList("rm-type-body");
                text.style.color = TokenColors.ToColor(Tokens.TextSecondary);
                text.style.marginLeft = 12;
                text.style.marginTop = 10;
                textCard.Add(text);
                sample.Add(textCard);
                Caption = new Label { name = "rt1-caption" };
                Caption.AddToClassList("rm-type-caption");
                Caption.style.position = Position.Absolute;
                Caption.style.left = 16;
                Caption.style.top = 200;
                Caption.style.right = 16;
                Caption.style.color = TokenColors.ToColor(Tokens.TextPrimary);
                sample.Add(Caption);
            }

            public Label Caption { get; }

            public string Glass { get; private set; } = "";

            public string Secondary { get; private set; } = "";

            public Texture2D? Image { get; private set; }

            public IEnumerator Measure()
            {
                for (var i = 0; i < 6; i++)
                {
                    yield return null;
                }

                var frame = RtReader.Read(_target);
                Glass = Center(frame, "rt1-glass");
                Secondary = Center(frame, "rt1-secondary");
                Object.DestroyImmediate(frame);
            }

            public IEnumerator Annotate(string caption)
            {
                Caption.text = caption;
                for (var i = 0; i < 6; i++)
                {
                    yield return null;
                }

                Image = RtReader.Read(_target);
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_host);
                _panel.targetTexture = null;
                Object.DestroyImmediate(_panel);
                _target.Release();
                Object.DestroyImmediate(_target);
            }

            private string Center(Texture2D frame, string name)
            {
                var element = _root.Q(name);
                var center = element.worldBound.center;
                Color32 c = RtReader.PixelTopLeft(frame, Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
                return new Rgb(c.r, c.g, c.b).ToHex();
            }

            private static VisualElement Box(string name, float x, float y, float w, float h, Color color)
            {
                var box = new VisualElement { name = name };
                box.style.position = Position.Absolute;
                box.style.left = x;
                box.style.top = y;
                box.style.width = w;
                box.style.height = h;
                box.style.backgroundColor = color;
                return box;
            }
        }
    }
}
