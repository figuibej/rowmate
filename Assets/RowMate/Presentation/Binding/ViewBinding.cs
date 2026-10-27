using System;
using RowMate.Core.Common;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Binding
{
    // Binding manual (ARCHITECTURE §5, 02-tech-design §3.8), sin reflexión: la vista se suscribe al adjuntarse al panel
    // (con el valor actual, emitCurrent: true) y se da de baja al quitarse. Si ya está en un panel, se suscribe en el acto.
    // Como las pantallas ocultas (display: none) siguen en el panel, siguen suscriptas y están al día al volver (AC-APP-01-05).
    // Si un render lanza, ObservableValue lo registra con el tag STATE y los demás suscriptores siguen (QA-V-01).
    public static class ViewBinding
    {
        public static void Bind<T>(this VisualElement view, IReadOnlyObservable<T> source, Action<T> render)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            new ObservableBinding<T>(view, source ?? throw new ArgumentNullException(nameof(source)),
                render ?? throw new ArgumentNullException(nameof(render)));
        }

        public static void Bind<T>(this VisualElement view, IEventSource<T> source, Action<T> onEvent)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            new EventBinding<T>(view, source ?? throw new ArgumentNullException(nameof(source)),
                onEvent ?? throw new ArgumentNullException(nameof(onEvent)));
        }

        private abstract class BindingBase
        {
            private IDisposable? _subscription;

            protected BindingBase(VisualElement view)
            {
                view.RegisterCallback<AttachToPanelEvent>(_ => Attach());
                view.RegisterCallback<DetachFromPanelEvent>(_ => Detach());
                if (view.panel != null)
                {
                    // Se llama desde el constructor de la derivada, después de fijar la fuente (ver Start).
                    AttachedAtCreation = true;
                }
            }

            protected bool AttachedAtCreation { get; }

            protected void Start()
            {
                if (AttachedAtCreation)
                {
                    Attach();
                }
            }

            protected abstract IDisposable Subscribe();

            private void Attach()
            {
                if (_subscription != null)
                {
                    return;
                }

                _subscription = Subscribe();
            }

            private void Detach()
            {
                var subscription = _subscription;
                _subscription = null;
                subscription?.Dispose();
            }
        }

        private sealed class ObservableBinding<T> : BindingBase
        {
            private readonly IReadOnlyObservable<T> _source;
            private readonly Action<T> _render;

            public ObservableBinding(VisualElement view, IReadOnlyObservable<T> source, Action<T> render)
                : base(view)
            {
                _source = source;
                _render = render;
                Start();
            }

            protected override IDisposable Subscribe()
            {
                return _source.Subscribe(_render, true);
            }
        }

        private sealed class EventBinding<T> : BindingBase
        {
            private readonly IEventSource<T> _source;
            private readonly Action<T> _onEvent;

            public EventBinding(VisualElement view, IEventSource<T> source, Action<T> onEvent)
                : base(view)
            {
                _source = source;
                _onEvent = onEvent;
                Start();
            }

            protected override IDisposable Subscribe()
            {
                return _source.Subscribe(_onEvent);
            }
        }
    }
}
