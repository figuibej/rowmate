using System;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // Reloj de la fecha de creación de las rutinas en pruebas y capturas (spec §0.4): 2026-10-27T13:00:00.000Z en la primera
    // lectura y un minuto más en cada lectura siguiente. Lo lee SOLO RoutinesStore.CreateAsync, una vez por alta, así "un
    // minuto más por lectura" es "un minuto más por cada rutina creada" (QA-G3-19; un alta que falla y se reintenta toma el
    // minuto siguiente). Los ids de serie y todo lo demás usan el FakeScheduler.
    public sealed class SequenceClock : IClock
    {
        public static readonly DateTimeOffset DefaultStart = new DateTimeOffset(2026, 10, 27, 13, 0, 0, TimeSpan.Zero);

        private readonly DateTimeOffset _start;
        private readonly TimeSpan _step;
        private readonly TimeSpan _localOffset;

        // localOffset: el de LocalNow (por defecto UTC−3, el del FakeScheduler).
        public SequenceClock(DateTimeOffset? start = null, TimeSpan? step = null, TimeSpan? localOffset = null)
        {
            _start = (start ?? DefaultStart).ToUniversalTime();
            _step = step ?? TimeSpan.FromMinutes(1);
            _localOffset = localOffset ?? TimeSpan.FromHours(-3);
        }

        // Lecturas hechas (UtcNow y LocalNow cuentan igual).
        public int Reads { get; private set; }

        // La próxima lectura, sin leerla.
        public DateTimeOffset Next => _start.AddTicks(_step.Ticks * Reads);

        public DateTimeOffset UtcNow
        {
            get
            {
                var value = Next;
                Reads++;
                return value;
            }
        }

        public DateTimeOffset LocalNow => UtcNow.ToOffset(_localOffset);

        public double MonotonicSeconds => _step.TotalSeconds * Reads;
    }
}
