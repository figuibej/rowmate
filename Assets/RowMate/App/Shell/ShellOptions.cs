namespace RowMate.App.Shell
{
    // Opciones del shell: si existe la pestaña Perfil. En la It. 2 la app la deja apagada (su regla real, StravaConfig
    // .IsConfigured, es de la It. 12, D-20); PlayMode y el arnés la prenden con ShellOptions(true) (CAP-25).
    public sealed record ShellOptions(bool ProfileTabEnabled);
}
