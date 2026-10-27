using System.Collections.Generic;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Lista de idiomas del sistema de prueba (01-po-spec §0.4): el test la fija y la cambia sin tocar el equipo.
    public sealed class FakeSystemLocale : ISystemLocale
    {
        private IReadOnlyList<string> _tags;

        public FakeSystemLocale(params string[] tags)
        {
            _tags = tags ?? new string[0];
        }

        public IReadOnlyList<string> PreferredLanguageTags => _tags;

        public void Set(params string[] tags)
        {
            _tags = tags ?? new string[0];
        }
    }
}
