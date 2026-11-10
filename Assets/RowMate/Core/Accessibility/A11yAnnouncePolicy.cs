namespace RowMate.Core.Accessibility
{
    // Qué se anuncia solo (It. 14, AC-APP-03-14; DT14-21, DV-136):
    // - el título nuevo al cambiar de pestaña o de página apilada (no si es el mismo que el último anunciado);
    // - cada aviso UNA vez por Show (un mismo aviso repintado, reacomodado o que vuelve del segundo plano no se repite);
    // - el cambio de paso (una vez por paso y sesión) y el diálogo de rutina completada (una vez por sesión);
    // - las métricas NUNCA se anuncian solas: se leen al llegar a ellas.
    public sealed class A11yAnnouncePolicy
    {
        private string? _lastTitle;
        private long _lastToast = long.MinValue;
        private long _stepSession = long.MinValue;
        private int _stepIndex = -1;
        private long _completedSession = long.MinValue;

        public bool Title(string title)
        {
            if (string.IsNullOrEmpty(title) || title == _lastTitle)
            {
                return false;
            }

            _lastTitle = title;
            return true;
        }

        // showId: el número del Show del aviso (sube con cada aviso nuevo).
        public bool Toast(long showId)
        {
            if (showId == _lastToast)
            {
                return false;
            }

            _lastToast = showId;
            return true;
        }

        public bool Step(long session, int stepIndex)
        {
            if (session == _stepSession && stepIndex == _stepIndex)
            {
                return false;
            }

            _stepSession = session;
            _stepIndex = stepIndex;
            return true;
        }

        public bool Completion(long session)
        {
            if (session == _completedSession)
            {
                return false;
            }

            _completedSession = session;
            return true;
        }

        public bool Metric()
        {
            return false;
        }
    }
}
