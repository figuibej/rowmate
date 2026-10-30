using NUnit.Framework;
using RowMate.Core.Design;
using UnityEditor;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace RowMate.Tests.UnityEditMode
{
    // Fuentes e íconos (AC-APP-05-03; ASSET_SLOTS §3-§4; 02-tech-design §1.3 paso ui.fonts).
    public sealed class FontAndIconAssetsTests
    {
        [Test, Category("AC-APP-05-03")]
        public void FontAssetsExistAndCoverTheirSets()
        {
            Assert.That(FontAssetPaths.All.Count, Is.EqualTo(10), "7 caras de texto, 2 de íconos y Noto Emoji (It. 3, DN3-21)");
            var emoji = AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPaths.EmojiFallback);
            Assert.That(emoji, Is.Not.Null, FontAssetPaths.EmojiFallback);
            Assert.That(emoji.sourceFontFile, Is.Not.Null, "Noto Emoji: dinámico, con su .ttf");
            Assert.That(emoji.HasCharacter(0x1F6A3, false, true), Is.True, "🚣 (U+1F6A3) en Noto Emoji");
            foreach (var face in FontAssetPaths.TextFaces)
            {
                var path = FontAssetPaths.For(face.Family, face.Weight);
                var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
                Assert.That(asset, Is.Not.Null, path);
                Assert.That(asset.sourceFontFile, Is.Not.Null, path + ": dinámico, con su .ttf");
                Assert.That(asset.material, Is.Not.Null, path + ": material como subasset");
                Assert.That(asset.atlasTextures.Length, Is.GreaterThan(0), path);
                var set = face.Family == FontFamilyRole.Numeric ? "0123456789:.-m" : "AaÁáÉéÍíÓóÚúÑñ¿?¡!·…0123456789";
                asset.TryAddCharacters(set, out string missing, false);
                Assert.That(missing, Is.Null.Or.Empty, path + " cubre " + set);
            }
        }

        // DD4a-07 (WP-B0, QA-V4a-06): en cada cara de RowMate Numeric SC las diez cifras son las tabulares de Barlow (tnum,
        // congeladas en el cmap por tools/fonts/freeze_tnum.py): un solo avance, 507, 521 y 535 unidades en 600, 700 y 800 (de
        // 1000), y el `1` con pie, igual de ancho que el resto. TextCore las mide en puntos del tamaño de muestreo (90).
        [Test, Category("AC-APP-05-04"), Category("QA-V4a-06")]
        public void DerivedDigitsShareOneAdvance()
        {
            var expected = new[] { (600, 507), (700, 521), (800, 535) };
            foreach (var (weight, units) in expected)
            {
                var path = FontAssetPaths.For(FontFamilyRole.Numeric, weight);
                Assert.That(path, Does.Contain("/RowMateNumericSC-"), path);
                var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
                Assert.That(asset, Is.Not.Null, path);
                Assert.That(asset.faceInfo.familyName, Is.EqualTo("RowMate Numeric SC"), path + ": la familia de la derivada (OFL 1.1)");
                asset.TryAddCharacters("0123456789", out string missing, false);
                Assert.That(missing, Is.Null.Or.Empty, path);
                var pointSize = (double)asset.faceInfo.pointSize;
                Assert.That(pointSize, Is.EqualTo(90).Within(0.001), path + ": muestreo de 90");
                for (var c = '0'; c <= '9'; c++)
                {
                    var character = asset.characterLookupTable[c];
                    var advance = character.glyph.metrics.horizontalAdvance * character.glyph.scale;
                    Assert.That(advance / pointSize * 1000.0, Is.EqualTo(units).Within(0.5), path + ": avance de '" + c + "'");
                }
            }
        }

        [Test, Category("AC-APP-05-03")]
        public void IconFontHasEveryIconCodepoint()
        {
            var outline = AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPaths.IconsOutline);
            var filled = AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPaths.IconsFilled);
            Assert.That(outline, Is.Not.Null);
            Assert.That(filled, Is.Not.Null);
            foreach (var icon in Icons.All)
            {
                if (icon.InOutline)
                {
                    Assert.That(outline.HasCharacter(icon.Glyph[0], false, true), Is.True, "contorno: " + icon.Name);
                }

                if (icon.InFilled)
                {
                    Assert.That(filled.HasCharacter(icon.Glyph[0], false, true), Is.True, "relleno: " + icon.Name);
                }
            }
        }
    }
}
