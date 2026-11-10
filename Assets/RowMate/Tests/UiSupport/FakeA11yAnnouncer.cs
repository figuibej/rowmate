using System.Collections.Generic;
using RowMate.Presentation.Accessibility;
using UnityEngine.Accessibility;

namespace RowMate.Tests.UiSupport
{
    // El anunciador de prueba del lector de pantalla (It. 14, DT14-21, DT14-22): registra cada anuncio con su tipo, sin hablar.
    public sealed class FakeA11yAnnouncer : IA11yAnnouncer
    {
        public readonly List<string> Announcements = new List<string>();
        public readonly List<string> Screens = new List<string>();
        public int LayoutChanges;

        // Todo lo que se dijo, en orden ("anuncio: …" / "pantalla: …").
        public readonly List<string> All = new List<string>();

        public void Announce(string text)
        {
            Announcements.Add(text);
            All.Add("anuncio: " + text);
        }

        public void ScreenChanged(string title)
        {
            Screens.Add(title);
            All.Add("pantalla: " + title);
        }

        public void LayoutChanged(AccessibilityNode? focus)
        {
            LayoutChanges++;
        }

        public void Clear()
        {
            Announcements.Clear();
            Screens.Clear();
            All.Clear();
            LayoutChanges = 0;
        }
    }
}
