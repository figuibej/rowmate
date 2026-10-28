using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Pantallas apiladas (F-APP-03; 03-rutinas/02-tech-design §4.2; legacy Navigator.push/pop con MaterialPageRoute):
    //   #page-stack .rm-layer .rm-page-stack
    //   ├── .rm-page-stack__dim    oscurecido de lo de atrás (overlay.scrim, opacidad por código 0 → 0,08), sin eventos
    //   └── las páginas (.rm-page)  ocupan todo el panel, con fondo bg.app, y reciben los eventos
    // - Push: la página entra (PageTransition). Al TERMINAR la entrada, #tab-content y #tab-bar pasan a display: none
    //   (CoversShellChanged(true)): no se dibujan, no se tocan y Tab no llega a ellos; display: none conserva el
    //   desplazamiento de un ScrollView, así la lista vuelve donde estaba (AC-RUT-10-09). Un segundo Push mientras entra
    //   otra se ignora (doble toque en +, DT-18).
    // - Pop: en el cuadro 0 de la salida, ExitLock deja inerte la página entera y el foco vuelve a quien lo tenía antes de
    //   abrir (QA-G3-01), pestañas y barra vuelven a flex (CoversShellChanged(false)) y la página sale; al terminar sale del
    //   árbol y se dispone (OnPopped). Un segundo Pop de la misma página no hace nada; "Atrás" saltea lo que sale.
    // - Settled: avisa cuando termina una transición y no queda ninguna en curso (la lista hace crecer la tarjeta nueva o
    //   resalta la editada recién ahí, 04-design-notes §5.5).
    public sealed class PageStack : VisualElement, IDisposable
    {
        private readonly UiContext _ui;
        private readonly VisualElement _dim;
        private readonly List<Entry> _pages = new List<Entry>();
        private VisualElement? _tabContent;
        private VisualElement? _tabBar;
        private bool _coversShell;
        private bool _disposed;

        public PageStack(UiContext ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            name = "page-stack";
            AddToClassList("rm-layer");
            AddToClassList("rm-page-stack");
            pickingMode = PickingMode.Ignore;
            _dim = new VisualElement();
            _dim.AddToClassList("rm-page-stack__dim");
            _dim.pickingMode = PickingMode.Ignore;
            _dim.style.backgroundColor = TokenColors.ToColor(Tokens.OverlayScrim);
            _dim.style.opacity = 0;
            _dim.style.display = DisplayStyle.None;
            Add(_dim);
        }

        // La de arriba que NO está saliendo (la que sale ya no cuenta para "Atrás" ni para Push).
        public IPage? Top
        {
            get
            {
                for (var i = _pages.Count - 1; i >= 0; i--)
                {
                    if (!_pages[i].Exiting)
                    {
                        return _pages[i].Page;
                    }
                }

                return null;
            }
        }

        public bool HasPages => Top != null;

        // Alguna transición de página en curso (para la regla de toma y las pruebas).
        public bool IsTransitioning
        {
            get
            {
                for (var i = 0; i < _pages.Count; i++)
                {
                    if (_pages[i].Transition.IsRunning)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // Pestañas y barra en display: none (terminó la entrada de una página).
        public bool CoversShell => _coversShell;

        public VisualElement Dim => _dim;

        public event Action<bool>? CoversShellChanged;

        public event Action? Settled;

        // Lo de atrás de la primera página (lo arma AppShellView).
        public void AttachShell(VisualElement tabContent, VisualElement tabBar)
        {
            _tabContent = tabContent;
            _tabBar = tabBar;
        }

        public bool IsExiting(IPage page)
        {
            var entry = Find(page);
            return entry != null && entry.Exiting;
        }

        public bool HasPage(IPage page)
        {
            return Find(page) != null;
        }

        public void Push(IPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            if (_disposed || Find(page) != null)
            {
                return;
            }

            for (var i = 0; i < _pages.Count; i++)
            {
                if (!_pages[i].Exiting && _pages[i].Transition.IsEntering)
                {
                    return; // DT-18: una sola página por toque
                }
            }

            var focused = panel?.focusController?.focusedElement;
            var entry = new Entry(page, new PageTransition(_ui.Motion), focused);
            _pages.Add(entry);
            var root = page.Root;
            root.AddToClassList("rm-page");
            root.style.opacity = 0;
            Add(root);
            _dim.style.display = DisplayStyle.Flex;
            entry.Transition.PlayIn(root, _tabContent, _tabBar, _dim, () => OnEntered(entry));
        }

        public void Pop(IPage page)
        {
            var entry = Find(page);
            if (_disposed || entry == null || entry.Exiting || !ReferenceEquals(Top, page))
            {
                return;
            }

            entry.Exiting = true;
            ExitLock.Apply(page.Root, entry.FocusBefore);
            SetCovers(false);
            entry.Transition.PlayOut(page.Root, _tabContent, _tabBar, _dim, () => OnExited(entry));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (var i = _pages.Count - 1; i >= 0; i--)
            {
                var entry = _pages[i];
                entry.Transition.Finish();
                entry.Page.Root.RemoveFromHierarchy();
                entry.Page.OnPopped();
            }

            _pages.Clear();
        }

        private void OnEntered(Entry entry)
        {
            if (_disposed || entry.Exiting)
            {
                return;
            }

            entry.Page.Root.style.opacity = StyleKeyword.Null;
            entry.Page.Root.style.translate = StyleKeyword.Null;
            SetCovers(true);
            entry.Page.OnPushed();
            Settled?.Invoke();
        }

        private void OnExited(Entry entry)
        {
            _pages.Remove(entry);
            entry.Page.Root.RemoveFromHierarchy();
            if (_tabContent != null)
            {
                _tabContent.style.translate = StyleKeyword.Null;
            }

            if (_tabBar != null)
            {
                _tabBar.style.translate = StyleKeyword.Null;
            }

            if (_pages.Count == 0)
            {
                _dim.style.opacity = 0;
                _dim.style.display = DisplayStyle.None;
            }

            entry.Page.OnPopped();
            if (!_disposed)
            {
                Settled?.Invoke();
            }
        }

        private void SetCovers(bool covers)
        {
            if (_coversShell == covers)
            {
                return;
            }

            _coversShell = covers;
            var display = covers ? DisplayStyle.None : DisplayStyle.Flex;
            if (_tabContent != null)
            {
                _tabContent.style.display = display;
                if (covers)
                {
                    _tabContent.style.translate = StyleKeyword.Null;
                }
            }

            if (_tabBar != null)
            {
                _tabBar.style.display = display;
                if (covers)
                {
                    _tabBar.style.translate = StyleKeyword.Null;
                }
            }

            CoversShellChanged?.Invoke(covers);
        }

        private Entry? Find(IPage page)
        {
            for (var i = 0; i < _pages.Count; i++)
            {
                if (ReferenceEquals(_pages[i].Page, page))
                {
                    return _pages[i];
                }
            }

            return null;
        }

        private sealed class Entry
        {
            public Entry(IPage page, PageTransition transition, Focusable? focusBefore)
            {
                Page = page;
                Transition = transition;
                FocusBefore = focusBefore;
            }

            public IPage Page { get; }

            public PageTransition Transition { get; }

            public Focusable? FocusBefore { get; }

            public bool Exiting { get; set; }
        }
    }
}
