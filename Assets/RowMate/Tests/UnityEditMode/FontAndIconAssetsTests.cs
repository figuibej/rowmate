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
            Assert.That(FontAssetPaths.All.Count, Is.EqualTo(9));
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
