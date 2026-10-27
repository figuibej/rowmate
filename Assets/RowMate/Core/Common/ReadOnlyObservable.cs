using System;

namespace RowMate.Core.Common
{
    // Vista de solo lectura de un ObservableValue<T> (el mismo patrón que LanguageState.ReadOnlyLanguage, It. 1): quien la
    // recibe puede leer y suscribirse, pero no asignar el valor ni siquiera con un cast. La usan los stores y los servicios
    // para exponer sus IReadOnlyObservable<T> (02-tech-design §2.1, §2.3-§2.7).
    public sealed class ReadOnlyObservable<T> : IReadOnlyObservable<T>
    {
        private readonly ObservableValue<T> _inner;

        public ReadOnlyObservable(ObservableValue<T> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public T Value => _inner.Value;

        public IDisposable Subscribe(Action<T> onChange, bool emitCurrent = true)
        {
            return _inner.Subscribe(onChange, emitCurrent);
        }
    }
}
