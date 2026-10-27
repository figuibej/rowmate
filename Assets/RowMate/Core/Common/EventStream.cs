using System;

namespace RowMate.Core.Common
{
    // Flujo de eventos broadcast sin buffer (= StreamController.broadcast del legacy, ADR 004).
    // Quien se suscribe después de un Emit no recibe ese evento. Suscriptores copy-on-write:
    // emitir no reserva memoria y desuscribirse durante una emisión no rompe la emisión en curso.
    // La excepción de un suscriptor no corta a los demás ni sube a quien emite: se registra en el logger
    // (SubscriberErrors, QA-V-01), como el error de un listener de un Stream broadcast, que va a la zona y no cancela nada.
    // Implementa IEventSource<T> (It. 2): los servicios exponen solo esa interfaz y se quedan con Emit.
    public sealed class EventStream<T> : IObservable<T>, IEventSource<T>
    {
        private const string Source = "EventStream";
        private static readonly Subscription[] NoSubscriptions = new Subscription[0];

        private readonly IAppLogger _logger;
        private Subscription[] _subscriptions = NoSubscriptions;

        // logger: adonde van las excepciones de los suscriptores (el de la app, o RecordingLogger/NullAppLogger en tests).
        public EventStream(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int SubscriberCount => _subscriptions.Length;

        public void Emit(T value)
        {
            var snapshot = _subscriptions;
            for (var i = 0; i < snapshot.Length; i++)
            {
                var subscription = snapshot[i];
                if (subscription.IsDisposed)
                {
                    continue;
                }

                try
                {
                    subscription.Handler(value);
                }
                catch (Exception error)
                {
                    SubscriberErrors.Report(_logger, Source, typeof(T), error);
                }
            }
        }

        public IDisposable Subscribe(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var subscription = new Subscription(this, handler);
            var current = _subscriptions;
            var next = new Subscription[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = subscription;
            _subscriptions = next;
            return subscription;
        }

        // Solo OnNext: un flujo broadcast del legacy no se completa ni falla.
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            return Subscribe(observer.OnNext);
        }

        private void Remove(Subscription subscription)
        {
            var current = _subscriptions;
            var index = Array.IndexOf(current, subscription);
            if (index < 0)
            {
                return;
            }

            if (current.Length == 1)
            {
                _subscriptions = NoSubscriptions;
                return;
            }

            var next = new Subscription[current.Length - 1];
            Array.Copy(current, 0, next, 0, index);
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            _subscriptions = next;
        }

        private sealed class Subscription : IDisposable
        {
            private EventStream<T>? _owner;

            public Subscription(EventStream<T> owner, Action<T> handler)
            {
                _owner = owner;
                Handler = handler;
            }

            public Action<T> Handler { get; }

            public bool IsDisposed => _owner == null;

            public void Dispose()
            {
                var owner = _owner;
                if (owner == null)
                {
                    return;
                }

                _owner = null;
                owner.Remove(this);
            }
        }
    }
}
