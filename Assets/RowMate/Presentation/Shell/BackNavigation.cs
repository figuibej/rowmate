using System;
using RowMate.Core.Ports;

namespace RowMate.Presentation.Shell
{
    // De dónde viene el "Atrás" (03-rutinas/02-tech-design §4.2): la tecla Atrás de Android (llega como Escape del Input
    // System; AppRoot la manda como SystemBack en Android) o Escape en escritorio.
    public enum BackSource
    {
        SystemBack,
        Escape
    }

    // "Atrás" del sistema (AC-APP-02-06, AC-APP-03-02, AC-RUT-11-08; P-08): cierra un solo nivel por pulsación, lo de arriba,
    // salteando lo que ya está saliendo (como Flutter, donde la ruta que sale ya no es la actual):
    //   1. menú o ayuda abiertos → se cierran (= tocar afuera)
    //   2. diálogo abierto → se cancela
    //   3. Escape: nada más (P-08: Escape no cierra pantallas; en el legacy solo cierra lo que se cierra tocando afuera)
    //   4. página arriba → OnBackRequested() (el editor cierra sin preguntar)
    //   5. raíz → policy.OnBackAtRoot() (Android: al fondo o salir; escritorio: nada), una vez por pulsación, sin cambiar de
    //      pestaña
    // Escape llega por un solo camino (AppRoot.Update → ShellHandle.HandleBack): ningún control registra
    // NavigationCancelEvent para cerrar algo, así una pulsación nunca cierra dos niveles.
    public sealed class BackNavigation
    {
        private readonly IBackButtonPolicy _policy;
        private readonly PopupLayer? _popups;
        private readonly DialogLayer? _dialogs;
        private readonly PageStack? _pages;

        public BackNavigation(IBackButtonPolicy policy, PopupLayer? popups = null, DialogLayer? dialogs = null, PageStack? pages = null)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _popups = popups;
            _dialogs = dialogs;
            _pages = pages;
        }

        public bool IsAtRoot => (_popups == null || !_popups.HasOpenMenu) && (_dialogs == null || !_dialogs.HasOpen) && (_pages == null || !_pages.HasPages);

        public void HandleBack()
        {
            HandleBack(BackSource.SystemBack);
        }

        public void HandleBack(BackSource source)
        {
            if (_popups != null && _popups.CloseForBack())
            {
                return;
            }

            if (_dialogs != null && _dialogs.CancelTop())
            {
                return;
            }

            if (source == BackSource.Escape)
            {
                return;
            }

            var top = _pages?.Top;
            if (top != null)
            {
                top.OnBackRequested();
                return;
            }

            _policy.OnBackAtRoot();
        }
    }
}
