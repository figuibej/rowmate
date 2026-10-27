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

        // Pesos que existen por familia (los de los tokens tipográficos: Inter 400/500/600/700, Barlow SC 600/700/800).
        private static readonly (FontFamilyRole Family, int Weight, string Folder, string Stem)[] Faces =
        {
            (FontFamilyRole.Ui, 400, "Inter", "Inter-Regular"),
            (FontFamilyRole.Ui, 500, "Inter", "Inter-Medium"),
            (FontFamilyRole.Ui, 600, "Inter", "Inter-SemiBold"),
            (FontFamilyRole.Ui, 700, "Inter", "Inter-Bold"),
            (FontFamilyRole.Numeric, 600, "BarlowSemiCondensed", "BarlowSemiCondensed-SemiBold"),
            (FontFamilyRole.Numeric, 700, "BarlowSemiCondensed", "BarlowSemiCondensed-Bold"),
            (FontFamilyRole.Numeric, 800, "BarlowSemiCondensed", "BarlowSemiCondensed-ExtraBold"),
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

        // Los 9 FontAsset que crea el bootstrapper: 7 de texto y número, y los 2 de íconos.
        public static IReadOnlyList<string> All
        {
            get
            {
                var all = new List<string>(Faces.Length + 2);
                for (var i = 0; i < Faces.Length; i++)
                {
                    all.Add(For(Faces[i].Family, Faces[i].Weight));
                }

                all.Add(IconsOutline);
                all.Add(IconsFilled);
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

        private static (FontFamilyRole Family, int Weight, string Folder, string Stem) Face(FontFamilyRole family, int weight)
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
