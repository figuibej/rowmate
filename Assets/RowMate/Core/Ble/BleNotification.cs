using System;

namespace RowMate.Core.Ble
{
    // Una notificación (o indicación) de una característica, con la hora del reloj de la app en que llegó (ADR 009, ADR 017):
    // en primer plano, la del cuadro que la drenó; al volver del segundo plano, su instante virtual del alcance (BleEventClock).
    public readonly struct BleNotification
    {
        public BleNotification(BleUuid characteristic, byte[] value, double monotonicSeconds)
        {
            Characteristic = characteristic;
            Value = value ?? throw new ArgumentNullException(nameof(value));
            MonotonicSeconds = monotonicSeconds;
        }

        public BleUuid Characteristic { get; }

        public byte[] Value { get; }

        public double MonotonicSeconds { get; }
    }
}
