using System;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace RowMate.Presentation.Controls
{
    // Una ranura de texto dibujado (05-series-parciales/02-tech-design §3.1; DT4b-01): lo que las pruebas y el arnés leen de un
    // elemento que dibuja varios textos en su generateVisualContent, como si cada texto fuera un elemento: su nombre, el texto
    // entero (un vacío en dos renglones da el texto del catálogo y sus renglones aparte), el rectángulo en coordenadas del panel
    // de UI Toolkit (dp de ventana), el alfa efectivo (el de su motion por las reglas de la tinta), la escala de ajuste y el color.
    public readonly struct DrawnSlot
    {
        public DrawnSlot(string name, string text, Rect worldRect, float alpha, float scale, Color color, string? line0 = null, string? line1 = null)
        {
            Name = name;
            Text = text;
            WorldRect = worldRect;
            Alpha = alpha;
            Scale = scale;
            Color = color;
            Line0 = line0;
            Line1 = line1;
        }

        public string Name { get; }

        public string Text { get; }

        public Rect WorldRect { get; }

        public float Alpha { get; }

        public float Scale { get; }

        public Color Color { get; }

        // Los dos renglones de un vacío (null en una ranura de un renglón).
        public string? Line0 { get; }

        public string? Line1 { get; }

        // ≈ find.text: una ranura con alfa efectivo ≤ 0,01 no se ve (05 §3.1).
        public bool IsVisible => Alpha > 0.01f && WorldRect.width > 0 && WorldRect.height > 0;

        public override string ToString()
        {
            return Name + " \"" + Text + "\" α " + Alpha.ToString("0.000") + " ×" + Scale.ToString("0.000") + " " + WorldRect;
        }
    }

    // Un elemento que dibuja textos sin Label (los paneles de series): UiQuery.IsTextVisible, UiTestHost.TextOf / IsShown / Bounds y
    // PixelProbe buscan primero un elemento con ese nombre y, si no hay, una ranura con ese nombre (§3.6, §6.2). Las ranuras se
    // arman al pedirlas (las pruebas, el arnés): nunca en el cuadro a cuadro.
    public interface IDrawnTextSource
    {
        int SlotCount { get; }

        DrawnSlot SlotAt(int index);
    }

    // El FontAsset de una clase de tipo y su medida (05 §3.1; R5-01; la sonda S-B5 de WP-B0): un hijo oculto sin caja (como los
    // afijos de TabularNumber: no se dibuja ni cuenta en el presupuesto) resuelve la fuente de la clase y mide una corrida con
    // MeasureTextSize, el generador de TextCore, con su kerning (S-B5: ± 0,5 base contra lo que dibuja DrawText). La corrida con
    // tracking (el rótulo del chip, 0,08 em) se mide y se dibuja carácter por carácter con los avances del FontAsset (DrawText no
    // acepta letter-spacing; S-B5: ± 0,3 base contra MeasureTextSize con letter-spacing 8). Sin panel no mide (devuelve NaN).
    public sealed class DrawnFont
    {
        public const string HolderClass = "rm-hud-series-panel__font";

        private static readonly string[] CharStrings = BuildCharStrings();

        public DrawnFont(VisualElement owner, string typeClass, float sizeDp, float? letterSpacing)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            SizeDp = sizeDp;
            Holder = new Label();
            Holder.AddToClassList(HolderClass);
            Holder.AddToClassList(typeClass);
            Holder.pickingMode = PickingMode.Ignore;
            Holder.focusable = false;
            Holder.enableRichText = false;
            Holder.style.position = Position.Absolute;
            Holder.style.left = 0;
            Holder.style.top = 0;
            Holder.style.width = 0;
            Holder.style.height = 0;
            Holder.style.visibility = Visibility.Hidden;
            Holder.style.fontSize = sizeDp;
            if (letterSpacing.HasValue)
            {
                Holder.style.letterSpacing = letterSpacing.Value;
            }

            owner.Add(Holder);
        }

        public Label Holder { get; }

        // El tamaño de la corrida que mide MeasureTextSize (base).
        public float SizeDp { get; }

        public FontAsset? Font => Holder.resolvedStyle.unityFontDefinition.fontAsset;

        // El ancho de una corrida sin tracking a tamaño sizeDp (base): MeasureTextSize al tamaño del holder, proporcional (el texto
        // SDF escala lineal). NaN sin panel o sin fuente.
        public float Measure(string text, float sizeDp)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            if (Holder.panel == null || Font == null)
            {
                return float.NaN;
            }

            var width = Holder.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            return width * sizeDp / SizeDp;
        }

        // Escala de la fuente a un tamaño (unidades del FontAsset → base).
        public float ScaleAt(float sizeDp)
        {
            var font = Font;
            if (font == null)
            {
                return 0;
            }

            var face = font.faceInfo;
            return face.pointSize > 0 ? sizeDp / face.pointSize * (face.scale > 0 ? face.scale : 1f) : 0;
        }

        // El ascendente (la línea de arriba de DrawText) a un tamaño: DrawText pone la línea de base en pos.y + ascendente.
        public float AscentAt(float sizeDp)
        {
            var font = Font;
            return font == null ? 0 : font.faceInfo.ascentLine * ScaleAt(sizeDp);
        }

        // El avance de un carácter (base); 0,5 em si la fuente no lo tiene.
        public float Advance(char c, float sizeDp)
        {
            var font = Font;
            if (font == null)
            {
                return 0;
            }

            Character character;
            if (!font.characterLookupTable.TryGetValue(c, out character))
            {
                font.TryAddCharacters(CharString(c));
                font.characterLookupTable.TryGetValue(c, out character);
            }

            if (character == null || character.glyph == null)
            {
                return 0.5f * sizeDp;
            }

            return character.glyph.metrics.horizontalAdvance * ScaleAt(sizeDp) * character.glyph.scale;
        }

        // El avance mayor de 0-9 (la celda de un dígito) a un tamaño: lineal con el tamaño, calculado una vez por fuente.
        public float DigitAdvance(float sizeDp)
        {
            var font = Font;
            if (font == null)
            {
                return 0;
            }

            if (!ReferenceEquals(font, _digitFont))
            {
                _digitFont = font;
                _unitDigit = 0;
                for (var c = '0'; c <= '9'; c++)
                {
                    _unitDigit = Mathf.Max(_unitDigit, Advance(c, 1));
                }
            }

            return _unitDigit * sizeDp;
        }

        private FontAsset? _digitFont;
        private float _unitDigit;

        // El centro de la tinta de un glifo sobre la línea de base (para centrar un ícono en su caja).
        public float InkCenterAboveBaseline(char c, float sizeDp)
        {
            var font = Font;
            if (font == null)
            {
                return 0;
            }

            if (!font.characterLookupTable.TryGetValue(c, out var character) || character.glyph == null)
            {
                font.TryAddCharacters(CharString(c));
                if (!font.characterLookupTable.TryGetValue(c, out character) || character.glyph == null)
                {
                    return 0;
                }
            }

            var m = character.glyph.metrics;
            var k = ScaleAt(sizeDp) * character.glyph.scale;
            return (m.horizontalBearingY - m.height / 2f) * k;
        }

        // El ancho de una corrida con tracking (em): los avances más el tracking entre letras.
        public float MeasureTracked(string text, float sizeDp, float trackingEm)
        {
            if (string.IsNullOrEmpty(text) || Font == null)
            {
                return 0;
            }

            var width = 0f;
            for (var i = 0; i < text.Length; i++)
            {
                width += Advance(text[i], sizeDp);
                if (i < text.Length - 1)
                {
                    width += trackingEm * sizeDp;
                }
            }

            return width;
        }

        // Una corrida entera con DrawText (el kerning lo pone el generador): x a la izquierda, la línea de base en baseline.
        // color: el de la ficha (sin MeshColor), con su alfa; alpha lo multiplica.
        public void Draw(MeshGenerationContext context, string text, float x, float baseline, float sizeDp, Color color, float alpha, float pixelsPerPoint)
        {
            var font = Font;
            if (font == null || string.IsNullOrEmpty(text) || alpha <= 0.001f || sizeDp <= 0)
            {
                return;
            }

            var mesh = MeshColor.ForMesh(color);
            mesh.a = color.a * alpha;
            context.DrawText(text, new Vector2(x, baseline - AscentAt(sizeDp)), sizeDp * pixelsPerPoint, mesh, font);
        }

        // Una corrida con tracking, carácter por carácter (las cadenas de un carácter están precalculadas: no asigna).
        public void DrawTracked(MeshGenerationContext context, string text, float x, float baseline, float sizeDp, float trackingEm, Color color,
                                float alpha, float pixelsPerPoint)
        {
            var font = Font;
            if (font == null || string.IsNullOrEmpty(text) || alpha <= 0.001f || sizeDp <= 0)
            {
                return;
            }

            var mesh = MeshColor.ForMesh(color);
            mesh.a = color.a * alpha;
            var top = baseline - AscentAt(sizeDp);
            var size = sizeDp * pixelsPerPoint;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c != ' ')
                {
                    context.DrawText(CharString(c), new Vector2(x, top), size, mesh, font);
                }

                x += Advance(c, sizeDp) + trackingEm * sizeDp;
            }
        }

        // Un glifo solo, con su caja centrada en (centerX, centerY) por su tinta (el ícono de la cabecera).
        public void DrawGlyphCentered(MeshGenerationContext context, string glyph, float centerX, float centerY, float sizeDp, Color color, float alpha,
                                      float pixelsPerPoint)
        {
            var font = Font;
            if (font == null || string.IsNullOrEmpty(glyph) || alpha <= 0.001f)
            {
                return;
            }

            var c = glyph[0];
            var advance = Advance(c, sizeDp);
            var baseline = centerY + InkCenterAboveBaseline(c, sizeDp);
            var mesh = MeshColor.ForMesh(color);
            mesh.a = color.a * alpha;
            context.DrawText(glyph, new Vector2(centerX - advance / 2f, baseline - AscentAt(sizeDp)), sizeDp * pixelsPerPoint, mesh, font);
        }

        public static string CharString(char c)
        {
            return c < CharStrings.Length ? CharStrings[c] : c.ToString();
        }

        private static string[] BuildCharStrings()
        {
            var strings = new string[0x2027];
            for (var i = 0; i < strings.Length; i++)
            {
                strings[i] = ((char)i).ToString();
            }

            return strings;
        }
    }

    // Lo que no depende de una fuente (05 §3.1).
    public static class DrawnText
    {
        public const string Ellipsis = "…";

        // Si el texto tiene letras con descendente (la tinta de abajo de la cabecera: `Reps`, `Series anteriores`).
        public static bool HasDescender(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (var i = 0; i < text!.Length; i++)
            {
                switch (text[i])
                {
                    case 'g':
                    case 'j':
                    case 'p':
                    case 'q':
                    case 'y':
                    case ',':
                    case ';':
                    case 'Q':
                        return true;
                }
            }

            return false;
        }

        // El prefijo más largo de text que, con "…" al final, entra en room (base) a la escala dada; measure mide a escala 1.
        // Lo usan el título por debajo de 0,8 y el split por debajo del piso de 0,6 (AC-WKT-18-03, -17; DT5-16). Asigna: solo
        // cuando cambia el texto.
        public static string Ellipsize(string text, float room, float scale, Func<string, float> measure)
        {
            if (string.IsNullOrEmpty(text) || measure(text) * scale <= room + 0.01f)
            {
                return text;
            }

            for (var length = text.Length - 1; length > 0; length--)
            {
                var candidate = text.Substring(0, length).TrimEnd() + Ellipsis;
                if (measure(candidate) * scale <= room + 0.01f)
                {
                    return candidate;
                }
            }

            return Ellipsis;
        }

        // El alfa de una ficha de color (ColorToken con su alfa) por otro alfa.
        public static Color WithAlpha(ColorToken token, double alpha)
        {
            var color = TokenColors.ToColor(token);
            color.a *= (float)alpha;
            return color;
        }
    }
}
