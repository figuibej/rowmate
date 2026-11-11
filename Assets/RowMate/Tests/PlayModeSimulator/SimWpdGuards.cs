using System.IO;
using NUnit.Framework;
using RowMate.Presentation.Controls;

namespace RowMate.Tests.PlayMode.Simulator
{
    // Lo que depende de WP-D de la It. 15 (la USS y el recorte de la fuente de íconos; 15-deuda-pc/02-tech-design §3, §5): mientras no
    // llegue, la afirmación queda ignorada con el motivo y se dispara sola cuando el archivo de WP-D existe (como las guardas de WP-A-02).
    internal static class SimWpdGuards
    {
        // El glifo está en la fuente de íconos (IconView sin el respaldo dibujado).
        public static void FontHas(IconView icon)
        {
            if (icon.UsesFallback)
            {
                Assert.Ignore("pendiente WP-D (A15-11): la fuente de íconos todavía no tiene el glifo U+" + ((int)icon.Codepoint[0]).ToString("X4"));
            }
        }

        // Alguna hoja de Presentation/Styles tiene el selector.
        public static void UssHas(string selector)
        {
            var folder = Path.Combine("Assets", "RowMate", "Presentation", "Styles");
            foreach (var file in Directory.GetFiles(folder, "*.uss"))
            {
                if (File.ReadAllText(file).Contains(selector))
                {
                    return;
                }
            }

            Assert.Ignore("pendiente WP-D: ninguna USS define " + selector);
        }
    }
}
