using System;

namespace RowMate.Core.Common
{
    // Fuente de eventos de solo suscripción (02-tech-design §2.1): lo que exponen los puertos de servicios (IRowerService,
    // IHeartRateService), para que nadie de afuera pueda emitir. La implementa EventStream<T>, con su misma semántica
    // (broadcast sin buffer, = Stream.broadcast del legacy).
    public interface IEventSource<T>
    {
        // Disponer el resultado da de baja la suscripción.
        IDisposable Subscribe(Action<T> handler);
    }
}
