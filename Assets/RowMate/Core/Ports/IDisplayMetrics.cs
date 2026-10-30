namespace RowMate.Core.Ports
{
    // Densidad de pantalla y área segura (02-tech-design §3.1, D-03, AC-APP-05-09). Lo implementa Platform (Android
    // DisplayMetrics.density por JNI; Windows GetDpiForWindow / 96; Editor Screen.dpi / 96); en los tests, FakeDisplayMetrics.
    public interface IDisplayMetrics
    {
        // px por dp (1 en Windows al 100 %, 2,625 en un teléfono típico).
        double DpScale { get; }

        // Márgenes seguros en dp (SafeInsets): los de la app, CON las barras del sistema aunque el inmersivo las oculte, así
        // Workout y las pestañas no se mueven cuando las barras van y vienen (It. 4b, §4.3; DD4b-07).
        SafeInsets SafeArea { get; }

        // It. 4b (AC-APP-15-01; 02-tech-design §4.3): los márgenes del inmersivo, solo el recorte de la pantalla (Screen.cutouts),
        // estables con las barras ocultas, visibles o reveladas un momento por el deslizamiento. Los usa el HUD desde el cuadro
        // del push (HudZones, por ShellLayout.ImmersiveInsets). En escritorio son los mismos que SafeArea: es la implementación
        // por defecto (DisplayMetricsProvider la reemplaza en Android en WP-B2).
        SafeInsets ImmersiveSafeArea => SafeArea;
    }
}
