using System;
using System.Collections.Generic;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // Graba la secuencia de valores de un observable o de una fuente de eventos (= las listas statuses.add, bpm.add de los
    // tests legacy). Con entrega sincrónica (D-04) cada aviso queda en el orden exacto en que ocurrió; la aserción intermedia
    // "antes del microtask" de un test legacy se porta mirando la secuencia (LT-32).
    public sealed class StateRecorder<T> : IDisposable
    {
        private readonly List<T> _values = new List<T>();
        private IDisposable? _subscription;

        // emitCurrent = true graba también el valor actual al suscribirse (un observable constante solo se graba así: LT-20).
        public StateRecorder(IReadOnlyObservable<T> source, bool emitCurrent = false)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _subscription = source.Subscribe(_values.Add, emitCurrent);
        }

        public StateRecorder(IEventSource<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _subscription = source.Subscribe(_values.Add);
        }

        public IReadOnlyList<T> Values => _values;

        public int Count => _values.Count;

        public T First
        {
            get
            {
                if (_values.Count == 0)
                {
                    throw new InvalidOperationException("StateRecorder: no se grabó ningún valor.");
                }

                return _values[0];
            }
        }

        public T Last
        {
            get
            {
                if (_values.Count == 0)
                {
                    throw new InvalidOperationException("StateRecorder: no se grabó ningún valor.");
                }

                return _values[_values.Count - 1];
            }
        }

        public void Clear()
        {
            _values.Clear();
        }

        public void Dispose()
        {
            var subscription = _subscription;
            _subscription = null;
            subscription?.Dispose();
        }
    }
}
