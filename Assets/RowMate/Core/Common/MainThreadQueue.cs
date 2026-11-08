using System;
using System.Collections.Generic;

namespace RowMate.Core.Common
{
    // Cola de acciones hacia el hilo principal (ADR 005; DT10-10, QA-G10a-17 de la It. 10a): lo que llega de hilos administrados
    // ajenos (la respuesta del diálogo de permisos de Android, el loopback de OAuth de la It. 12) se encola con Post desde
    // cualquier hilo y corre en el hilo principal cuando AppRoot.Update llama Drain, ANTES de la bomba de eventos BLE y del
    // Tick del programador. Drain sin pendientes no toma el lock ni asigna. Una acción que lanza se registra y no corta las
    // demás. Lo que se encola durante un Drain corre en el Drain siguiente (un cuadro después).
    public sealed class MainThreadQueue
    {
        public const string Tag = "MAIN";

        private readonly object _gate = new object();
        private readonly IAppLogger _logger;
        private List<Action> _pending = new List<Action>();
        private List<Action> _running = new List<Action>();
        private volatile int _count;

        public MainThreadQueue(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Acciones esperando el próximo Drain (aproximado fuera del lock).
        public int Pending => _count;

        // Cualquier hilo.
        public void Post(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (_gate)
            {
                _pending.Add(action);
                _count = _pending.Count;
            }
        }

        // Hilo principal. Devuelve cuántas acciones corrió.
        public int Drain()
        {
            if (_count == 0)
            {
                return 0;
            }

            List<Action> batch;
            lock (_gate)
            {
                batch = _pending;
                _pending = _running;
                _running = batch;
                _count = 0;
            }

            var ran = 0;
            for (var i = 0; i < batch.Count; i++)
            {
                try
                {
                    batch[i]();
                }
                catch (Exception error)
                {
                    _logger.Log(LogLevel.Error, Tag, "acción del hilo principal: " + error.Message, error);
                }

                ran++;
            }

            batch.Clear();
            return ran;
        }
    }
}
