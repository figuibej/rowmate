using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Densidad y área segura de prueba (QA-G2-06): FakeDisplayMetrics(2.625, new SafeInsets(0, 24, 0, 48)) = un teléfono
    // vertical con barra de estado y barra de gestos. Los tests y el arnés los cambian en vivo (rotar, redimensionar).
    // It. 4b (§4.3): ImmersiveSafeArea, los márgenes del inmersivo (solo el recorte); sin fijarlos, los mismos que SafeArea.
    public sealed class FakeDisplayMetrics : IDisplayMetrics
    {
        private SafeInsets? _immersive;

        public FakeDisplayMetrics(double dpScale = 1.0, SafeInsets safeArea = default)
        {
            DpScale = dpScale;
            SafeArea = safeArea;
        }

        public double DpScale { get; set; }

        public SafeInsets SafeArea { get; set; }

        public SafeInsets ImmersiveSafeArea
        {
            get => _immersive ?? SafeArea;
            set => _immersive = value;
        }
    }
}
