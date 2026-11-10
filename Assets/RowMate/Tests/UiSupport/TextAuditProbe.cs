using System.Collections.Generic;
using RowMate.Core.Localization;
using UnityEngine.UIElements;

namespace RowMate.Tests.UiSupport
{
    // La auditoría de textos en vivo (It. 14, AC-APP-11-01; 02-tech-design §4, DT14-25, D-01): junta cada texto visible del panel
    // (TextElement a la vista, cruzando la ventana) salvo los subárboles .rm-audit-exempt (panel y cinta del simulador, diagnóstico BLE)
    // y lo clasifica con VisibleTextClassifier (catálogo del idioma activo, números, datos del usuario de la toma, marcas y
    // excepciones Q-03). Cada Violation vuelve como "{pantalla} · {estado} · {idioma} · \"{texto}\"". La usan las tomas de it14 y las
    // pruebas PlayMode (TextAuditTourTests, LegacyFixedTextsTests, DemoWalkthroughTests): por eso vive en UiSupport y no en Snapshots,
    // que es solo del Editor (QA-V14-04). También junta las ranuras de texto dibujado (IDrawnTextSource: los paneles de series).
    public static class TextAuditProbe
    {
        public const string ExemptClass = "rm-audit-exempt";

        public static List<string> Collect(VisualElement root)
        {
            var texts = new List<string>();
            Walk(root, root.worldBound, texts);
            return texts;
        }

        public static List<string> Violations(UiTestHost host, string screen, string state, params string[] userData)
        {
            var catalog = host.App.Texts.Value;
            var language = catalog.LanguageCode;
            var matcher = new CatalogMatcher(LocalizationCatalog.Parse(language.StartsWith("es") ? UiAssetsLocator.StringsEs() : UiAssetsLocator.StringsEn()));
            var data = new List<string>(userData);
            data.AddRange(FakeDeviceData());
            var context = new AuditContext(matcher, screen, state).WithUserData(data.ToArray()).WithExceptions(RowMate.Tests.Support.AuditExceptions.Simulator);
            var upper = UppercaseCatalog(catalog);
            var problems = new List<string>();
            foreach (var text in Collect(host.Root))
            {
                var raw = RichText.Replace(RowMate.Core.Design.PathBreak.Strip(text), string.Empty);
                var clean = raw.Trim();
                if (clean.Length == 0)
                {
                    continue;
                }

                if (!IsAllowed(raw, clean, context, upper))
                {
                    problems.Add(screen + " · " + state + " · " + language + " · \"" + clean + "\"");
                }
            }

            return problems;
        }

        // Las marcas de texto enriquecido (<color=…>…</color> de las unidades atenuadas del editor) no son texto visible.
        private static readonly System.Text.RegularExpressions.Regex RichText = new System.Text.RegularExpressions.Regex("</?(color|b|i|size|alpha)(=[^>]*)?>");

        // Un texto visible está bien si alguna de sus formas pasa el clasificador (QA-V14-04, lo que encontró el recorrido): tal cual
        // (los mensajes del catálogo con espacios adelante, "  O elegí una rutina:"), recortado, sin el separador " · " con el que la
        // vista pega un mensaje a un nombre (" · reconectando...") o, en mayúsculas, un mensaje del catálogo (la cabecera del pulsómetro).
        private static bool IsAllowed(string raw, string clean, AuditContext context, HashSet<string> upper)
        {
            if (upper.Contains(clean) || VisibleTextClassifier.Classify(clean, context) != VisibleTextKind.Violation
                || VisibleTextClassifier.Classify(raw, context) != VisibleTextKind.Violation)
            {
                return true;
            }

            var unglued = clean.Trim('·', ' ');
            if (unglued.Length > 0 && unglued != clean && VisibleTextClassifier.Classify(unglued, context) != VisibleTextKind.Violation)
            {
                return true;
            }

            // Un subtítulo armado con varios mensajes (el del paso: "2m  ·  140-160W · 18 spm · 02:10/500m ·  SPM +2/rep, W +10/rep",
            // StepSubtitle/WorkoutFormats): vale si cada pedazo entre " · " o ", " pasa por sí solo.
            var parts = clean.Split(new[] { '·', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return false;
            }

            foreach (var part in parts)
            {
                var piece = WithoutGlyphs(part).Trim();
                if (piece.Length > 0 && VisibleTextClassifier.Classify(piece, context) == VisibleTextKind.Violation)
                {
                    return false;
                }
            }

            return true;
        }

        // Sin los glifos de ícono pegados al texto (el 📈 de la tendencia, los de la fuente de íconos en el área de uso privado).
        private static string WithoutGlyphs(string text)
        {
            var builder = new System.Text.StringBuilder(text.Length);
            foreach (var c in text)
            {
                var category = char.GetUnicodeCategory(c);
                if (category != System.Globalization.UnicodeCategory.PrivateUse && category != System.Globalization.UnicodeCategory.Surrogate
                    && category != System.Globalization.UnicodeCategory.OtherSymbol && category != System.Globalization.UnicodeCategory.Format)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        // Los mensajes del catálogo del idioma activo en mayúsculas (las claves de L10nKeys).
        private static HashSet<string> UppercaseCatalog(RowMate.Core.Localization.Localizer texts)
        {
            var set = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var field in typeof(L10nKeys).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string) && texts.Contains((string)field.GetRawConstantValue()))
                {
                    try
                    {
                        set.Add(texts.Get((string)field.GetRawConstantValue()).ToUpperInvariant());
                    }
                    catch (System.Exception)
                    {
                        // Un mensaje con parámetros no se lee sin ellos: no es un rótulo fijo.
                    }
                }
            }

            return set;
        }

        // Los equipos de prueba (FakeBleDevices): nombres e identificadores son datos del usuario (categoría c).
        private static IEnumerable<string> FakeDeviceData()
        {
            foreach (var field in typeof(RowMate.Tests.Support.FakeBleDevices).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    yield return (string)field.GetRawConstantValue();
                }
                else if (field.GetValue(null) is RowMate.Tests.Support.FakeBleDevice device && device.Name != null)
                {
                    yield return device.Name;
                }
            }
        }

        private static void Walk(VisualElement element, UnityEngine.Rect window, List<string> into)
        {
            if (element.ClassListContains(ExemptClass))
            {
                return;
            }

            var style = element.resolvedStyle;
            if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden || style.opacity <= 0.001f)
            {
                return;
            }

            if (element is TextElement text && !string.IsNullOrWhiteSpace(text.text) && element.worldBound.Overlaps(window))
            {
                into.Add(text.text);
            }

            if (element is RowMate.Presentation.Controls.IDrawnTextSource drawn)
            {
                var slots = drawn.SlotCount;
                for (var i = 0; i < slots; i++)
                {
                    var slot = drawn.SlotAt(i);
                    if (slot.IsVisible && !string.IsNullOrWhiteSpace(slot.Text) && slot.WorldRect.Overlaps(window))
                    {
                        into.Add(slot.Text);
                    }
                }
            }

            var count = element.hierarchy.childCount;
            for (var i = 0; i < count; i++)
            {
                Walk(element.hierarchy[i], window, into);
            }
        }
    }
}
