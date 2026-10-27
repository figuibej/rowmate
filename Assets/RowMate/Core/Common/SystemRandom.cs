using System;

namespace RowMate.Core.Common
{
    // Azar real de producción. Es el ÚNICO archivo del proyecto que usa System.Random (PureCodeRulesTests):
    // el resto recibe un IRandom. Sus valores no tienen que coincidir con los de Dart (AC-BLE-14-09).
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
