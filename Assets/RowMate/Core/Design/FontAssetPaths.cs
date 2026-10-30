using System;
using System.Collections.Generic;
using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // Rutas de los FontAsset de la UI (02-tech-design §1.3): una sola fuente para el generador de Typography.uss
    // (TypographyUssWriter) y para el bootstrapper que los crea (FontAssetBuilder, WP-B), así no pueden divergir.
    // Sin espacios en los nombres: las rutas van en url() de USS. Fuentes de ASSET_SLOTS §3-§4 (versionadas en UI/).
    public static class FontAssetPaths
    {
        public const string GeneratedFonts = "Assets/RowMate/UI/Fonts/Generated";
        public const string GeneratedIcons = "Assets/RowMate/UI/Icons/Generated";

        public const string IconsOutline = GeneratedIcons + "/RowMateIcons-Outline-SDF.asset";
        public const string IconsFilled = GeneratedIcons + "/RowMateIcons-Filled-SDF.asset";

        public const string IconsOutlineSource = "Assets/RowMate/UI/Icons/RowMateIcons-Outline.ttf";
        public const string IconsFilledSource = "Assets/RowMate/UI/Icons/RowMateIcons-Filled.ttf";

        // Respaldo de emojis del texto del usuario (P-09, DN3-21; 03-rutinas/04-design-notes §4.15): Noto Emoji monocroma,
        // instancia estática de 500 (el peso de los íconos), SIL OFL 1.1. Va último en los respaldos del panel
        // (RuntimePanelFactory): Inter → íconos de contorno → Noto Emoji.
        public const string EmojiFallback = GeneratedFonts + "/NotoEmoji-Medium-SDF.asset";
        public const string EmojiFallbackSource = "Assets/RowMate/UI/Fonts/NotoEmoji/NotoEmoji-Medium.ttf";

        // Las listas de kerning K2 (DD4a-06; tools/fonts/kern_pairs.py), una por cara que dibuja texto: <Stem>.pairs.json.
        public const string KerningFolder = "Assets/RowMate/UI/Fonts/Kerning";

        // Los FontAsset de Barlow Semi Condensed que había hasta la It. 4a (WP-B0, DN4a-22): el rol numérico pasó a la derivada
        // "RowMate Numeric SC" y nada los nombra. El bootstrapper los borra si quedaron; los .ttf de Barlow siguen versionados
        // como entrada de la receta (tools/fonts/freeze_tnum.py, ASSET_SLOTS §3.7).
        public static readonly IReadOnlyList<string> RetiredFontAssets = new[]
        {
            GeneratedFonts + "/BarlowSemiCondensed-SemiBold-SDF.asset",
            GeneratedFonts + "/BarlowSemiCondensed-Bold-SDF.asset",
            GeneratedFonts + "/BarlowSemiCondensed-ExtraBold-SDF.asset",
        };

        // Pesos que existen por familia (los de los tokens tipográficos: Inter 400/500/600/700; números 600/700/800). Desde la
        // It. 4b (WP-B0 de la 4a, DD4a-07) el rol numérico es RowMate Numeric SC: Barlow Semi Condensed con las cifras tabulares
        // (tnum) congeladas en el cmap. Kerned = la cara dibuja texto y lleva su tabla K2 (las Inter y la derivada 700, la de
        // num.stat del panel del simulador, DN4a-22); la derivada 600 y 800 solo las dibuja TabularNumber, celda por celda, sin
        // tabla (K4).
        private static readonly (FontFamilyRole Family, int Weight, string Folder, string Stem, bool Kerned)[] Faces =
        {
            (FontFamilyRole.Ui, 400, "Inter", "Inter-Regular", true),
            (FontFamilyRole.Ui, 500, "Inter", "Inter-Medium", true),
            (FontFamilyRole.Ui, 600, "Inter", "Inter-SemiBold", true),
            (FontFamilyRole.Ui, 700, "Inter", "Inter-Bold", true),
            (FontFamilyRole.Numeric, 600, "RowMateNumericSC", "RowMateNumericSC-SemiBold", false),
            (FontFamilyRole.Numeric, 700, "RowMateNumericSC", "RowMateNumericSC-Bold", true),
            (FontFamilyRole.Numeric, 800, "RowMateNumericSC", "RowMateNumericSC-ExtraBold", false),
        };

        // FontAsset de una familia y un peso ("Assets/RowMate/UI/Fonts/Generated/Inter-SemiBold-SDF.asset").
        // ArgumentOutOfRangeException si esa combinación no se genera (por ejemplo, Mono: ningún token la usa todavía).
        public static string For(FontFamilyRole family, int weight)
        {
            return GeneratedFonts + "/" + Face(family, weight).Stem + "-SDF.asset";
        }

        // Archivo .ttf versionado del que sale ese FontAsset ("Assets/RowMate/UI/Fonts/Inter/Inter-SemiBold.ttf").
        public static string SourceFor(FontFamilyRole family, int weight)
        {
            var face = Face(family, weight);
            return "Assets/RowMate/UI/Fonts/" + face.Folder + "/" + face.Stem + ".ttf";
        }

        // La lista K2 de esa cara ("Assets/RowMate/UI/Fonts/Kerning/Inter-SemiBold.pairs.json"), o null si la cara no lleva tabla
        // (K4: la derivada 600 y 800).
        public static string? KerningFor(FontFamilyRole family, int weight)
        {
            var face = Face(family, weight);
            return face.Kerned ? KerningFolder + "/" + face.Stem + ".pairs.json" : null;
        }

        // Los 10 FontAsset que crea el bootstrapper: 7 de texto y número, los 2 de íconos y el respaldo de emojis (It. 3,
        // QA-G3-12: así GeneratedAssetGuard también lo protege).
        public static IReadOnlyList<string> All
        {
            get
            {
                var all = new List<string>(Faces.Length + 3);
                for (var i = 0; i < Faces.Length; i++)
                {
                    all.Add(For(Faces[i].Family, Faces[i].Weight));
                }

                all.Add(IconsOutline);
                all.Add(IconsFilled);
                all.Add(EmojiFallback);
                return all;
            }
        }

        // Familia y peso de cada FontAsset de texto y número (para FontAssetBuilder: fuente y ajustes de §1.3).
        public static IReadOnlyList<(FontFamilyRole Family, int Weight)> TextFaces
        {
            get
            {
                var faces = new List<(FontFamilyRole, int)>(Faces.Length);
                for (var i = 0; i < Faces.Length; i++)
                {
                    faces.Add((Faces[i].Family, Faces[i].Weight));
                }

                return faces;
            }
        }

        private static (FontFamilyRole Family, int Weight, string Folder, string Stem, bool Kerned) Face(FontFamilyRole family, int weight)
        {
            for (var i = 0; i < Faces.Length; i++)
            {
                if (Faces[i].Family == family && Faces[i].Weight == weight)
                {
                    return Faces[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(weight), weight,
                "FontAssetPaths: no hay FontAsset para " + family + " " + Fmt.Integer(weight) + " (02-tech-design §1.3).");
        }
    }
}
