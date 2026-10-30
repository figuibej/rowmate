using System;
using System.Collections.Generic;
using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // La interfaz del sistema de prueba (spec §0.4 de la It. 4b; 02-tech-design §4.1): anota cada pedido de ocultar o mostrar las
    // barras del sistema con el instante del programador (s, monótono). La leen ImmersiveSystemBarsTests (dotnet) y SystemBarsTests
    // (PlayMode).
    public sealed class FakeSystemUi : ISystemUi
    {
        private readonly IClock? _clock;
        private readonly List<Request> _requests = new List<Request>();

        public FakeSystemUi(IClock? clock = null)
        {
            _clock = clock;
        }

        public IReadOnlyList<Request> Requests => _requests;

        // El último pedido (null sin pedidos).
        public bool? Immersive => _requests.Count == 0 ? (bool?)null : _requests[_requests.Count - 1].Immersive;

        public void SetImmersive(bool immersive)
        {
            _requests.Add(new Request(immersive, _clock?.MonotonicSeconds ?? double.NaN));
        }

        // "true@0.000 false@0.640": los pedidos con su instante, en orden.
        public string Describe(double origin = 0)
        {
            var parts = new string[_requests.Count];
            for (var i = 0; i < parts.Length; i++)
            {
                var at = _requests[i].At - origin;
                parts[i] = (_requests[i].Immersive ? "true" : "false") + "@" + RowMate.Core.Text.Fmt.Fixed(at, 3);
            }

            return string.Join(" ", parts);
        }

        public readonly struct Request
        {
            public Request(bool immersive, double at)
            {
                Immersive = immersive;
                At = at;
            }

            public bool Immersive { get; }

            public double At { get; }

            public override string ToString()
            {
                return (Immersive ? "true" : "false") + "@" + RowMate.Core.Text.Fmt.Fixed(At, 3);
            }
        }
    }
}
