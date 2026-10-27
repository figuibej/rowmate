using System;
using System.Collections.Generic;

namespace RowMate.Core.Common
{
    // Valor observable (ADR 004). Notifica solo si el valor cambia según el comparador.
    // Suscriptores en un arreglo copy-on-write: emitir no reserva memoria.
    // Semántica de ChangeNotifier (Flutter):
    // - un `Value =` dentro de un callback notifica en el acto (anidado);
    // - cada suscriptor recibe el valor vigente en el momento de llamarlo (como un listener que lee `value`);
    // - desuscribirse durante una emisión no rompe la emisión en curso; el suscriptor dado de baja ya no se llama;
    // - la excepción de un suscriptor no corta a los demás ni sube a quien asignó el valor: se registra en el logger
    //   (SubscriberErrors, QA-V-01) y se sigue con el siguiente. El suscriptor que lanzó sigue suscripto, como en Flutter.
    //   Lo mismo con emitCurrent en Subscribe: la suscripción queda hecha y se devuelve.
    public sealed class ObservableValue<T> : IReadOnlyObservable<T>
    {
        private const string Source = "ObservableValue";
        private static readonly Subscription[] NoSubscriptions = new Subscription[0];

        private readonly IEqualityComparer<T> _comparer;
        private readonly IAppLogger _logger;
        private Subscription[] _subscriptions = NoSubscriptions;
        private T _value;

        // logger: adonde van las excepciones de los suscriptores (el de la app, o RecordingLogger/NullAppLogger en tests).
        public ObservableValue(T initial, IAppLogger logger, IEqualityComparer<T>? comparer = null)
        {
            _value = initial;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public T Value
        {
            get => _value;
            set
            {
                if (_comparer.Equals(_value, value))
                {
                    return;
                }

                _value = value;
                Notify();
            }
        }

        public int SubscriberCount => _subscriptions.Length;

        // = _reemitStatus() del legacy: avisa aunque el valor no haya cambiado.
        public void ForceNotify()
        {
            Notify();
        }

        public IDisposable Subscribe(Action<T> onChange, bool emitCurrent = true)
        {
            if (onChange == null)
            {
                throw new ArgumentNullException(nameof(onChange));
            }

            var subscription = new Subscription(this, onChange);
            var current = _subscriptions;
            var next = new Subscription[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = subscription;
            _subscriptions = next;

            if (emitCurrent)
            {
                Deliver(onChange);
            }

            return subscription;
        }

        private void Notify()
        {
            var snapshot = _subscriptions;
            for (var i = 0; i < snapshot.Length; i++)
            {
                var subscription = snapshot[i];
                if (!subscription.IsDisposed)
                {
                    Deliver(subscription.Handler);
                }
            }
        }

        // Un suscriptor que lanza no corta la emisión (= el try/catch por listener de ChangeNotifier.notifyListeners).
        private void Deliver(Action<T> handler)
        {
            try
            {
                handler(_value);
            }
            catch (Exception error)
            {
                SubscriberErrors.Report(_logger, Source, typeof(T), error);
            }
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
            private ObservableValue<T>? _owner;

            public Subscription(ObservableValue<T> owner, Action<T> handler)
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
