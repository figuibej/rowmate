using System;
using System.Collections.Generic;

namespace RowMate.Core.Common
{
    // Observable derivado de otro (02-tech-design §2.1): por ejemplo, el idioma vigente → el Localizer de ese idioma
    // (AppContext.Texts). El valor se calcula al crearlo y se vuelve a calcular cada vez que la fuente avisa; los
    // suscriptores se enteran de cada aviso de la fuente (también de un ForceNotify), con el valor ya recalculado.
    // Misma semántica de entrega que ObservableValue (sincrónica, en el hilo que cambia la fuente; la excepción de un
    // suscriptor va al logger, QA-V-01). La excepción de map sube a quien cambió la fuente, como la de un listener.
    // Dispose se da de baja de la fuente: el valor queda congelado y nadie más recibe avisos.
    public sealed class DerivedObservable<TIn, TOut> : IReadOnlyObservable<TOut>, IDisposable
    {
        private readonly Func<TIn, TOut> _map;
        private readonly ObservableValue<TOut> _value;
        private IDisposable? _sourceSubscription;

        public DerivedObservable(IReadOnlyObservable<TIn> source, Func<TIn, TOut> map, IAppLogger logger)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _map = map ?? throw new ArgumentNullException(nameof(map));
            _value = new ObservableValue<TOut>(map(source.Value), logger, AlwaysDifferent.Instance);
            _sourceSubscription = source.Subscribe(OnSource, false);
        }

        public TOut Value => _value.Value;

        public bool IsDisposed => _sourceSubscription == null;

        public IDisposable Subscribe(Action<TOut> onChange, bool emitCurrent = true)
        {
            return _value.Subscribe(onChange, emitCurrent);
        }

        public void Dispose()
        {
            var subscription = _sourceSubscription;
            _sourceSubscription = null;
            subscription?.Dispose();
        }

        private void OnSource(TIn value)
        {
            if (_sourceSubscription == null)
            {
                return;
            }

            // Con AlwaysDifferent la asignación avisa siempre: un aviso de la fuente = un aviso del derivado.
            _value.Value = _map(value);
        }

        // Comparador que nunca da "igual": el derivado no filtra, repite cada aviso de la fuente.
        private sealed class AlwaysDifferent : IEqualityComparer<TOut>
        {
            public static readonly AlwaysDifferent Instance = new AlwaysDifferent();

            public bool Equals(TOut x, TOut y)
            {
                return false;
            }

            public int GetHashCode(TOut obj)
            {
                return 0;
            }
        }
    }
}
