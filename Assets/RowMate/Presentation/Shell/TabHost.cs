using System.Collections.Generic;
using RowMate.App.Shell;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // = IndexedStack del legacy (main.dart:143): las 4/5 pantallas se construyen UNA vez y siguen suscriptas a sus stores
    // aunque no se vean (AC-APP-01-05). Cambiar de pestaña pone display: none en la anterior al final de su salida y flex en
    // la nueva (TabTransition): el desplazamiento y el estado de cada pantalla se conservan (AC-APP-02-04). Ninguna se recrea.
    public sealed class TabHost : VisualElement
    {
        private readonly UiContext _ui;
        private readonly List<VisualElement> _screens = new List<VisualElement>();
        private readonly TabTransition _transition;
        private int _current = -1;

        public TabHost(UiContext ui, ShellStore shell)
        {
            _ui = ui;
            name = "tab-content";
            AddToClassList("rm-tab-content");
            _transition = new TabTransition(ui.Motion);
            Shell = shell;
        }

        public ShellStore Shell { get; }

        public bool Ready { get; private set; }

        public int Current => _current;

        public IReadOnlyList<VisualElement> Screens => _screens;

        public void AddScreen(VisualElement screen)
        {
            screen.style.display = DisplayStyle.None;
            _screens.Add(screen);
            Add(screen);
        }

        // Después de agregar todas: se suscribe a la selección (el estado inicial se aplica sin animar).
        public void Start()
        {
            if (Ready)
            {
                return;
            }

            Ready = true;
            this.Bind(Shell.SelectedIndex, Show);
        }

        private void Show(int index)
        {
            if (index == _current || index < 0 || index >= _screens.Count)
            {
                return;
            }

            var previous = _current >= 0 ? _screens[_current] : null;
            var direction = index > _current ? 1 : -1;
            _current = index;
            if (previous == null || !_ui.Motion.Armed)
            {
                _transition.Finish();
                for (var i = 0; i < _screens.Count; i++)
                {
                    _screens[i].style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;
                    _screens[i].style.opacity = StyleKeyword.Null;
                    _screens[i].style.translate = StyleKeyword.Null;
                }

                return;
            }

            _transition.Play(previous, _screens[index], direction);
        }
    }
}
