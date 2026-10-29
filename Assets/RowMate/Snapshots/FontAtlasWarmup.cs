using System.Globalization;
using RowMate.Core.Design;
using UnityEditor;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace RowMate.Snapshots
{
    // QA-V3-06 de la It. 3: el arnés deja los atlas dinámicos de las fuentes en un estado fijo ANTES de la primera toma. Los
    // FontAsset de la UI (FontAssetPaths: 7 de texto y número y los 2 de íconos) son dinámicos y se versionan vacíos; TextCore
    // agrega cada glifo al atlas la primera vez que se dibuja, en el lugar libre que encuentra. El lugar cambia con el orden y
    // con él el muestreo del SDF en el borde de los glifos (± 1-2 por canal, 03-rutinas/04-design-notes §16.5): una corrida de
    // un conjunto con otras tomas antes, una corrida con -Filter, o el orden en que UI Toolkit genera el texto de un cuadro (lo
    // hace en paralelo) daban bytes distintos en el borde de las letras. Con todos los glifos que dibujan las capturas ya en
    // el atlas, en un orden fijo (AtlasCharacters), ninguna toma agrega glifos y el resultado no depende de lo que se dibujó
    // antes. Solo el arnés: las pruebas y la app no lo necesitan. Sin las características de la fuente (includeFontFeatures
    // false): el kerning es el de la tabla versionada (DN3-24) y no se toca. GeneratedAssetGuard sigue impidiendo que se guarden.
    public static class FontAtlasWarmup
    {
        // Precarga los atlas en el orden de FontAssetPaths.All (Noto Emoji no: los emojis son del texto del usuario y ninguna
        // toma los dibuja). Devuelve la línea del registro: cuántos glifos había antes (en un proceso nuevo, 0: los assets se
        // versionan vacíos) y cuántos quedaron.
        public static string Run()
        {
            var before = 0;
            var after = 0;
            var fonts = 0;
            var missing = 0;
            foreach (var path in FontAssetPaths.All)
            {
                if (path == FontAssetPaths.EmojiFallback)
                {
                    continue;
                }

                var font = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
                if (font == null)
                {
                    continue;
                }

                fonts++;
                before += font.glyphTable.Count;
                var characters = path == FontAssetPaths.IconsOutline ? AtlasCharacters.IconGlyphs(false)
                    : path == FontAssetPaths.IconsFilled ? AtlasCharacters.IconGlyphs(true)
                    : AtlasCharacters.Text();
                font.TryAddCharacters(characters, out var notFound, false);
                missing += notFound?.Length ?? 0;
                after += font.glyphTable.Count;
            }

            return "[SNAPSHOT] atlas de fuentes precargados (QA-V3-06): " + fonts.ToString(CultureInfo.InvariantCulture) + " FontAsset, "
                + before.ToString(CultureInfo.InvariantCulture) + " glifos antes, " + after.ToString(CultureInfo.InvariantCulture) + " después ("
                + missing.ToString(CultureInfo.InvariantCulture) + " caracteres sin glifo en su fuente)";
        }
    }
}
