using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Barras de desplazamiento de las vistas desplazables (QA-V2-13 de la It. 2). La horizontal nunca se ve. La vertical:
    // - en escritorio (Windows, macOS, el Editor), Auto: aparece cuando el contenido no entra, como la Scrollbar que Flutter
    //   agrega en escritorio (MaterialScrollBehavior.buildScrollbar: linux, macOS, windows);
    // - en el teléfono, Hidden: Flutter en Android e iOS no dibuja barra (el mismo buildScrollbar devuelve el hijo sin
    //   Scrollbar), y el legacy no la muestra en reposo.
    // Hidden solo oculta la barra: el desplazamiento con el dedo, la rueda y ScrollTo sigue igual.
    public static class ScrollPolicy
    {
        public static void Apply(ScrollView scroll, bool isDesktop)
        {
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = VerticalFor(isDesktop);
        }

        public static ScrollerVisibility VerticalFor(bool isDesktop)
        {
            return isDesktop ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
        }
    }
}
