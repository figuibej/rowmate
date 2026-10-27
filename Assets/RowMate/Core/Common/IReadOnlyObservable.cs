using System;

namespace RowMate.Core.Common
{
    // Valor observable de solo lectura (ADR 004, ARCHITECTURE §5). Equivale a un ChangeNotifier del legacy
    // con un único valor: los suscriptores se llaman en forma sincrónica, en el hilo que cambia el valor.
    public interface IReadOnlyObservable<T>
    {
        T Value { get; }

        // emitCurrent = true: llama a onChange en el acto con el valor actual.
        IDisposable Subscribe(Action<T> onChange, bool emitCurrent = true);
    }
}
