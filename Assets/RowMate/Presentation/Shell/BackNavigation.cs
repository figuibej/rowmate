using System;
using RowMate.Core.Ports;

namespace RowMate.Presentation.Shell
{
    // "Atrás" del sistema (AC-APP-02-06, 02-tech-design §3.10, QA-G2-17). En la It. 2 siempre se está en la raíz (sin
    // páginas apiladas ni diálogos): llama a IBackButtonPolicy.OnBackAtRoot() una vez por pulsación y no cambia de pestaña.
    // Desde la It. 3, primero cierra el diálogo o la página de arriba.
    public sealed class BackNavigation
    {
        private readonly IBackButtonPolicy _policy;

        public BackNavigation(IBackButtonPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public bool IsAtRoot => true;

        public void HandleBack()
        {
            if (IsAtRoot)
            {
                _policy.OnBackAtRoot();
            }
        }
    }
}
