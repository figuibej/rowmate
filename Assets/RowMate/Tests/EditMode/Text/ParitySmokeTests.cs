using NUnit.Framework;
using RowMate.Core.Text;

namespace RowMate.Tests.EditMode.Text
{
    // ParitySmoke = la tabla de AC-BASE-01 ejecutable en un player (prueba de humo IL2CPP, D-17). En .NET 8 (y en el Editor)
    // tiene que dar 0 diferencias; el player con -rowmate-parity-smoke la escribe en su registro (WP-B).
    [TestFixture]
    public sealed class ParitySmokeTests
    {
        [Test, Category("AC-BASE-01")]
        public void NoDifferences()
        {
            var differences = ParitySmoke.Run();
            Assert.That(differences, Is.Empty, string.Join("\n", differences));
            Assert.That(ParitySmoke.CaseCount, Is.GreaterThanOrEqualTo(20), "la tabla entera de AC-BASE-01 y algunos bordes");
        }
    }
}
