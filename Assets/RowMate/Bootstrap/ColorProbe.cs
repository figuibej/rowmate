// QA-V2-09 (decisión del PO al cerrar la It. 2): la sonda de color solo existe en el Editor y en los builds de desarrollo.
// En un build normal este archivo no compila nada (AppRoot tampoco lee -rowmate-color-probe ni -rowmate-ui-pipeline).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Surface;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace RowMate.Bootstrap
{
    // Verificación S-B1 (02-tech-design §3.1, §6.3 B.2) en un build de desarrollo: RowMate.exe -rowmate-color-probe <json>
    // [-rowmate-ui-pipeline GammaRt|Overlay]. En lugar del shell dibuja la muestra de CAP-23 en el panel de la app (la misma
    // superficie y el mismo presentador) y lee el backbuffer al final de cada cuadro (ReadPixels después de WaitForEndOfFrame):
    //   (1) vidrio sobre blanco = #2A2F37 ± 1 (y text.secondary sobre surface.card);
    //   (2) un clic sintético del Input System sobre un botón de la muestra llega a ese botón (mapeo pantalla → panel);
    //   (3) costo de CPU de la presentación (ms por cuadro, promedio y máximo);
    //   (4) el primer cuadro presentado es bg.app ± 1 en todo el cuadro;
    //   (5) latencia 0: en Update del cuadro k la muestra pinta el canal rojo = k mod 256 y el backbuffer de ese cuadro lo muestra.
    // Escribe el JSON y sale. No es parte de la app: solo corre con el argumento.
    public sealed class ColorProbe : MonoBehaviour
    {
        private const int LatencyFrames = 12;
        private const int WatermarkWidth = 160;
        private const int WatermarkHeight = 36;

        private VisualElement? _root;
        private UiSurface? _surface;
        private string _output = "";
        private IAppLogger? _logger;
        private VisualElement? _frameSwatch;
        private Button? _button;
        private int _clicks;
        private int _frame;

        public void Run(VisualElement root, UiSurface surface, string outputPath, IAppLogger logger)
        {
            _root = root;
            _surface = surface;
            _output = outputPath;
            _logger = logger;
            StartCoroutine(Probe());
        }

        private void Update()
        {
            _frame++;
            if (_frameSwatch != null)
            {
                Color color = Color.black;
                color.r = (_frame % 256) / 255f;
                _frameSwatch.style.backgroundColor = color;
            }
        }

        private IEnumerator Probe()
        {
            var json = new StringBuilder();
            json.Append("{\n  \"pipeline\": \"").Append(_surface!.Pipeline).Append("\",\n");
            json.Append("  \"screen\": \"").Append(Screen.width.ToString(CultureInfo.InvariantCulture)).Append("x")
                .Append(Screen.height.ToString(CultureInfo.InvariantCulture)).Append("\",\n");
            json.Append("  \"graphicsDevice\": \"").Append(SystemInfo.graphicsDeviceType).Append("\",\n");

            // (4) Primer cuadro presentado: el panel vacío (la muestra todavía no existe).
            yield return new WaitForEndOfFrame();
            var first = ReadBackbuffer();
            var firstBad = CountDifferent(first, Tokens.BgApp.Rgb, 1, Debug.isDebugBuild ? WatermarkWidth : 0, Debug.isDebugBuild ? WatermarkHeight : 0);
            json.Append("  \"firstFrame\": { \"pixels\": ").Append(first.width * first.height).Append(", \"notBgApp\": ").Append(firstBad)
                .Append(", \"sample\": \"").Append(Hex(first.GetPixel(first.width / 2, first.height / 2))).Append("\", \"pass\": ")
                .Append(firstBad == 0 ? "true" : "false").Append(" },\n");
            Destroy(first);

            BuildSample();
            for (var i = 0; i < 6; i++)
            {
                yield return null;
            }

            // (1) Colores en pantalla.
            yield return new WaitForEndOfFrame();
            var shot = ReadBackbuffer();
            var glass = Sample(shot, "probe-glass");
            var secondary = Sample(shot, "probe-secondary");
            var card = Hex(Pixel(shot, "probe-card", 0.1f, 0.1f));
            json.Append("  \"glassOnWhite\": \"").Append(glass).Append("\",\n");
            json.Append("  \"textSecondaryOnCard\": \"").Append(secondary).Append("\",\n");
            json.Append("  \"surfaceCard\": \"").Append(card).Append("\",\n");
            var expectedGlass = ExpectedGlass();
            json.Append("  \"expectedGlassOnWhite\": \"").Append(expectedGlass).Append("\",\n");
            var glassPass = Distance(glass, expectedGlass) <= 1;
            json.Append("  \"colorPass\": ").Append(glassPass ? "true" : "false").Append(",\n");
            Destroy(shot);

            // Diagnóstico: lo mismo leído de la RT de la UI y de la copia del presentador (si la pantalla no lo muestra, dice
            // en qué paso se pierde).
            json.Append("  \"diagnostics\": { \"uiTarget\": \"").Append(ReadTarget(_surface.Target, "probe-glass")).Append("\"");
            if (_surface.Presenter is OverlayCopyPresenter copyPresenter)
            {
                json.Append(", \"presenterOutput\": \"").Append(ReadTarget(copyPresenter.Output, "probe-glass")).Append("\", \"copies\": ")
                    .Append(copyPresenter.CopiesDone.ToString(CultureInfo.InvariantCulture));
            }

            json.Append(", \"panelTargetIsSurface\": ").Append(_surface.Panel.targetTexture == _surface.Target ? "true" : "false")
                .Append(", \"forceGamma\": ").Append(_surface.Panel.forceGammaRendering ? "true" : "false").Append(" },\n");

            // (5) Latencia: el rojo de la muestra = número de cuadro del mismo cuadro.
            var latencyOk = 0;
            var latencyLines = new StringBuilder();
            for (var i = 0; i < LatencyFrames; i++)
            {
                yield return new WaitForEndOfFrame();
                var frameShot = ReadBackbuffer();
                var read = Pixel(frameShot, "probe-frame");
                var expected = _frame % 256;
                var red = Mathf.RoundToInt(read.r * 255f);
                if (Mathf.Abs(red - expected) <= 1)
                {
                    latencyOk++;
                }

                latencyLines.Append(latencyLines.Length > 0 ? ", " : "").Append("[").Append(expected).Append(", ").Append(red).Append("]");
                Destroy(frameShot);
            }

            json.Append("  \"latency\": { \"framesMatching\": ").Append(latencyOk).Append(", \"of\": ").Append(LatencyFrames)
                .Append(", \"expectedVsRead\": [").Append(latencyLines).Append("], \"pass\": ").Append(latencyOk == LatencyFrames ? "true" : "false")
                .Append(" },\n");

            // (3) Costo.
            var presenter = _surface.Presenter;
            json.Append("  \"presentCostMs\": ").Append(presenter == null ? "0" : presenter.LastCostMs.ToString("0.###", CultureInfo.InvariantCulture))
                .Append(",\n");

            // (2) Clic sintético del Input System en el centro del botón (coordenadas de pantalla con origen abajo a la izquierda).
            var clickResult = "no-button";
            if (_button != null)
            {
                var scale = _surface.Panel.scale;
                var center = _button.worldBound.center;
                var screen = new Vector2(center.x * scale, Screen.height - center.y * scale);
                var picked = _root!.panel.Pick(RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(screen.x, Screen.height - screen.y)));
                var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 0 });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 1 });
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 0 });
                for (var i = 0; i < 4; i++)
                {
                    yield return null;
                }

                clickResult = "clicks=" + _clicks + " pick=" + (picked == null ? "null" : picked.name);
                json.Append("  \"click\": { \"clicks\": ").Append(_clicks).Append(", \"pickedFromScreen\": \"").Append(picked == null ? "" : picked.name)
                    .Append("\", \"pass\": ").Append(_clicks == 1 ? "true" : "false").Append(" },\n");
            }

            json.Append("  \"done\": true\n}\n");
            File.WriteAllText(_output, json.ToString());
            _logger?.Log(LogLevel.Info, "PROBE", "S-B1: vidrio=" + glass + " secundario=" + secondary + " latencia=" + latencyOk + "/" + LatencyFrames
                + " " + clickResult + " → " + _output);
            Application.Quit(0);
        }

        // Muestra de CAP-23 (§4.4): blanco con vidrio encima; tarjeta con text.secondary encima; un botón; el número de cuadro.
        private void BuildSample()
        {
            var sample = new VisualElement { name = "probe" };
            sample.style.position = Position.Absolute;
            sample.style.left = 0;
            sample.style.top = 0;
            sample.style.right = 0;
            sample.style.bottom = 0;
            sample.style.backgroundColor = TokenColors.ToColor(Tokens.BgApp);
            var white = Box("probe-white", 20, 20, 160, 100, Color.white);
            white.Add(Box("probe-glass", 30, 25, 100, 50, TokenColors.ToColor(Tokens.GlassFallback)));
            sample.Add(white);
            var card = Box("probe-card", 200, 20, 160, 100, TokenColors.ToColor(Tokens.SurfaceCard));
            card.Add(Box("probe-secondary", 30, 25, 100, 50, TokenColors.ToColor(Tokens.TextSecondary)));
            sample.Add(card);
            _frameSwatch = Box("probe-frame", 20, 140, 60, 60, Color.black);
            sample.Add(_frameSwatch);
            _button = new Button(() => _clicks++) { name = "probe-button", text = "probe" };
            _button.style.position = Position.Absolute;
            _button.style.left = 120;
            _button.style.top = 140;
            _button.style.width = 160;
            _button.style.height = 60;
            sample.Add(_button);
            _root!.Add(sample);
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

        private Texture2D ReadBackbuffer()
        {
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false, true);
            RenderTexture.active = null;
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
            texture.Apply(false);
            return texture;
        }

        private string ReadTarget(RenderTexture? target, string elementName)
        {
            if (target == null || !target.IsCreated())
            {
                return "none";
            }

            var active = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, true);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
            texture.Apply(false);
            RenderTexture.active = active;
            var result = Sample(texture, elementName);
            Destroy(texture);
            return result;
        }

        private string Sample(Texture2D shot, string elementName)
        {
            return Hex(Pixel(shot, elementName));
        }

        private Color Pixel(Texture2D shot, string elementName)
        {
            return Pixel(shot, elementName, 0.5f, 0.5f);
        }

        // Un punto del elemento (fx, fy en 0..1 de su caja; 0,5/0,5 = el centro).
        private Color Pixel(Texture2D shot, string elementName, float fx, float fy)
        {
            var element = _root!.Q(elementName);
            if (element == null)
            {
                return Color.clear;
            }

            var scale = _surface!.Panel.scale;
            var bound = element.worldBound;
            var center = new Vector2(bound.x + bound.width * fx, bound.y + bound.height * fy);
            var x = Mathf.Clamp(Mathf.RoundToInt(center.x * scale), 0, shot.width - 1);
            var y = Mathf.Clamp(shot.height - 1 - Mathf.RoundToInt(center.y * scale), 0, shot.height - 1);
            return shot.GetPixel(x, y);
        }

        // Sin contar la esquina inferior derecha de excludeW × excludeH píxeles: ahí Unity escribe "Development Build" en los
        // builds de desarrollo, por encima de todo (no es de la app).
        private static int CountDifferent(Texture2D shot, Rgb expected, int tolerance, int excludeW, int excludeH)
        {
            var pixels = shot.GetPixels32();
            var count = 0;
            for (var i = 0; i < pixels.Length; i++)
            {
                if (excludeW > 0 && i % shot.width >= shot.width - excludeW && i / shot.width < excludeH)
                {
                    continue;
                }

                var p = pixels[i];
                if (Mathf.Abs(p.r - expected.R) > tolerance || Mathf.Abs(p.g - expected.G) > tolerance || Mathf.Abs(p.b - expected.B) > tolerance)
                {
                    count++;
                }
            }

            return count;
        }

        private static string Hex(Color color)
        {
            Color32 c = color;
            return new Rgb(c.r, c.g, c.b).ToHex();
        }

        // glass.fallback sobre blanco compuesto en sRGB (Contrast.WorstGlassBackground, ≈ #2A2F37): lo que tiene que verse.
        private static string ExpectedGlass()
        {
            var c = Contrast.WorstGlassBackground();
            return new Rgb((byte)Mathf.RoundToInt((float)c.R), (byte)Mathf.RoundToInt((float)c.G), (byte)Mathf.RoundToInt((float)c.B)).ToHex();
        }

        private static int Distance(string a, string b)
        {
            var x = Rgb.Parse(a);
            var y = Rgb.Parse(b);
            return Mathf.Max(Mathf.Abs(x.R - y.R), Mathf.Max(Mathf.Abs(x.G - y.G), Mathf.Abs(x.B - y.B)));
        }
    }
}
#endif
