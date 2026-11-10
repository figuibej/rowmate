using UnityEngine.Accessibility;

namespace RowMate.Presentation.Accessibility
{
    // Los anuncios del lector de pantalla (It. 14, AC-APP-03-14; DT14-21): en el player, UnityA11yAnnouncer sobre
    // AssistiveSupport.notificationDispatcher; en las pruebas, FakeA11yAnnouncer (registra). Qué se anuncia lo decide
    // A11yAnnouncePolicy (Core) en ScreenReaderBridge: el título nuevo al cambiar de pantalla, cada aviso una vez, el cambio de paso y
    // el diálogo de completado; nunca las métricas solas.
    public interface IA11yAnnouncer
    {
        // Un aviso, un cambio de paso o el completado.
        void Announce(string text);

        // Pantalla nueva (pestaña, página o diálogo): el título que se lee.
        void ScreenChanged(string title);

        // El elemento enfocado salió del árbol: el foco va a otro (o null = el primero).
        void LayoutChanged(AccessibilityNode? focus);
    }

    // El de Unity 6.3 (UnityEngine.AccessibilityModule): Narrador en Windows (UIA del UnityPlayer.dll), TalkBack en Android, VoiceOver
    // en la 10b. Solo manda algo con el lector encendido.
    public sealed class UnityA11yAnnouncer : IA11yAnnouncer
    {
        public void Announce(string text)
        {
            if (!AssistiveSupport.isScreenReaderEnabled || string.IsNullOrEmpty(text))
            {
                return;
            }

            AssistiveSupport.notificationDispatcher.SendAnnouncement(text);
        }

        public void ScreenChanged(string title)
        {
            if (!AssistiveSupport.isScreenReaderEnabled || string.IsNullOrEmpty(title))
            {
                return;
            }

            AssistiveSupport.notificationDispatcher.SendAnnouncement(title);
        }

        public void LayoutChanged(AccessibilityNode? focus)
        {
            if (!AssistiveSupport.isScreenReaderEnabled)
            {
                return;
            }

            AssistiveSupport.notificationDispatcher.SendLayoutChanged(focus);
        }
    }
}
