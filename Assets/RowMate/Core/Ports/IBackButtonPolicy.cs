namespace RowMate.Core.Ports
{
    // Qué hace "Atrás" del sistema en la raíz del shell (AC-APP-02-06, D-09, QA-G2-17). Lo implementa Platform: Android
    // API >= 31 manda la app al fondo (moveTaskToBack) y < 31 la cierra (Application.Quit); escritorio y Editor, nada.
    // Lo llama ShellHandle.HandleBack (Bootstrap, WP-B), una vez por pulsación.
    public interface IBackButtonPolicy
    {
        void OnBackAtRoot();
    }
}
