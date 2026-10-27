namespace RowMate.Core.Ports
{
    // Densidad de pantalla y área segura (02-tech-design §3.1, D-03, AC-APP-05-09). Lo implementa Platform (Android
    // DisplayMetrics.density por JNI; Windows GetDpiForWindow / 96; Editor Screen.dpi / 96); en los tests, FakeDisplayMetrics.
    public interface IDisplayMetrics
    {
        // px por dp (1 en Windows al 100 %, 2,625 en un teléfono típico).
        double DpScale { get; }

        // Márgenes seguros en dp (SafeInsets).
        SafeInsets SafeArea { get; }
    }
}
