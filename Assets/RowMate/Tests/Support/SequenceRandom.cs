using System;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // Azar reemplazable para tests (AC-BASE-07): devuelve los valores en orden; si se agotan, falla.
    // Sin valores, cualquier llamada falla: así se prueba que "sin ruido" el simulador no consume azar.
    public sealed class SequenceRandom : IRandom
    {
        private readonly double[] _values;

        public SequenceRandom(params double[] values)
        {
            _values = values ?? new double[0];
        }

        public int Calls { get; private set; }

        public int Remaining => _values.Length - Calls;

        public double NextDouble()
        {
            if (Calls >= _values.Length)
            {
                throw new InvalidOperationException("SequenceRandom: se pidió el valor " + (Calls + 1) + " y la secuencia tiene "
                    + _values.Length + ".");
            }

            return _values[Calls++];
        }
    }
}
