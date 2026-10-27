using UnityEngine;

namespace RowMate.Presentation.Controls
{
    // Colores y tamaños para lo que se dibuja a mano (MeshGenerationContext.DrawText, Painter2D: TabularNumber, Spinner, el
    // ícono genérico). Medido en un proyecto Linear (It. 2):
    // - DrawText deja el color una conversión a lineal "de más", en las DOS tuberías: con GammaRt (arnés, CAP-01) y con
    //   Overlay (build de Windows, S-B1) el trazo de #FF9F1C salía #FF5802 mientras la etiqueta de al lado, con el mismo
    //   color por USS, salía bien. Se compensa pasando color.gamma.
    // - DrawText toma además el tamaño de letra en píxeles físicos: se multiplica por scaledPixelsPerPoint (a 2,625 el número
    //   salía 2,6 veces más chico).
    // - Painter2D NO necesita compensación (el spinner salía más claro con ella): ahí va el color resuelto tal cual.
    public static class MeshColor
    {
        // Marca del shell que dibuja en un panel con forceGammaRendering (AppShellView, ShellHostOptions.Pipeline).
        public const string GammaPanelClass = "rm-gamma-panel";

        public static Color ForMesh(Color color)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
            {
                return color;
            }

            var converted = color.gamma;
            converted.a = color.a;
            return converted;
        }
    }
}
