using System.Text;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // DigitRuns = la línea Rep (y todo texto con rótulo y cifras) partida en tramos, para que solo ruede lo que cambia (QA-V5-02 de
    // la verificación de la It. 5; H5-D2-01; ANIMATION_BIBLE §2.4). Antes la línea entera se alineaba a la derecha y, cuando cambiaba
    // de largo, rodaban todas sus celdas (también "Rep 2/3 · ") y se corría de costado.
    [TestFixture]
    public sealed class DigitRunsTests
    {
        private readonly int[] _starts = new int[DigitRuns.MaxRuns];
        private readonly int[] _lengths = new int[DigitRuns.MaxRuns];
        private readonly bool[] _numeric = new bool[DigitRuns.MaxRuns];
        private readonly int[] _starts2 = new int[DigitRuns.MaxRuns];
        private readonly int[] _lengths2 = new int[DigitRuns.MaxRuns];
        private readonly bool[] _numeric2 = new bool[DigitRuns.MaxRuns];

        [Test, Category("AC-WKT-18-19")]
        public void TheRepLineInRuns()
        {
            // Los separadores entre dígitos son del número (el punto de miles, el reloj); "/" y " · " son texto.
            Assert.That(Runs("Rep 2/3 · 1.112 m"), Is.EqualTo("[Rep ]<2>[/]<3>[ · ]<1.112>[ m]"));
            Assert.That(Runs("Rep 999/999 · 999.999 m"), Is.EqualTo("[Rep ]<999>[/]<999>[ · ]<999.999>[ m]"));
            Assert.That(Runs("0:50.0"), Is.EqualTo("<0:50.0>"));
            Assert.That(Runs("5999:59.9"), Is.EqualTo("<5999:59.9>"));
            Assert.That(Runs("200 m"), Is.EqualTo("<200>[ m]"));
            // Un separador que no está entre dos dígitos es texto.
            Assert.That(Runs("Rep 1. m"), Is.EqualTo("[Rep ]<1>[. m]"));
            Assert.That(Runs("—"), Is.EqualTo("[—]"));
            Assert.That(Runs(""), Is.EqualTo(""));
        }

        [Test, Category("AC-WKT-18-19")]
        public void SameShapeWhenOnlyTheNumbersChange()
        {
            // Los cambios de VID-04: al empezar la repetición y en los 10 m de cada una.
            Assert.That(Same("Rep 1/3 · 680 m", "Rep 2/3 · 4 m"), Is.True);
            Assert.That(Same("Rep 2/3 · 8 m", "Rep 2/3 · 12 m"), Is.True);
            Assert.That(Same("Rep 2/3 · 999 m", "Rep 2/3 · 1.000 m"), Is.True, "el punto de miles es del número");
            // Otro rótulo (el idioma) u otra cantidad de tramos: no.
            Assert.That(Same("Rep 2/3 · 8 m", "Rep 2/3 · 8 ft"), Is.False);
            Assert.That(Same("Rep 2/3 · 8 m", "Rep 2/3 8 m"), Is.False);
            Assert.That(Same("—", "2:05"), Is.False);
        }

        [Test, Category("AC-WKT-18-19")]
        public void InsideARunTheCellsAlignToTheRight()
        {
            // "8" → "12" en "Rep 2/3 · 8 m": la celda de la derecha del tramo compara con el 8; la de la izquierda es nueva.
            var previous = "Rep 2/3 · 8 m";
            var next = "Rep 2/3 · 12 m";
            DigitRuns.Split(previous, _starts, _lengths, _numeric);
            DigitRuns.Split(next, _starts2, _lengths2, _numeric2);
            const int meters = 5;
            Assert.That(DigitRuns.PreviousCell(_starts[meters], _lengths[meters], _lengths2[meters], 1), Is.EqualTo(previous.IndexOf('8')));
            Assert.That(DigitRuns.PreviousCell(_starts[meters], _lengths[meters], _lengths2[meters], 0), Is.EqualTo(-1), "la celda nueva");
            // "680" → "4": la única celda compara con el 0.
            DigitRuns.Split("Rep 1/3 · 680 m", _starts, _lengths, _numeric);
            DigitRuns.Split("Rep 2/3 · 4 m", _starts2, _lengths2, _numeric2);
            Assert.That(DigitRuns.PreviousCell(_starts[meters], _lengths[meters], _lengths2[meters], 0), Is.EqualTo(_starts[meters] + 2));
        }

        [Test]
        public void MoreRunsThanItHolds()
        {
            Assert.That(DigitRuns.Split("1a2b3c4d5", _starts, _lengths, _numeric), Is.EqualTo(-1), "9 tramos");
            Assert.That(DigitRuns.Split("1a2b3c4d", _starts, _lengths, _numeric), Is.EqualTo(8));
        }

        private string Runs(string text)
        {
            var count = DigitRuns.Split(text, _starts, _lengths, _numeric);
            var builder = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                var run = text.Substring(_starts[i], _lengths[i]);
                builder.Append(_numeric[i] ? "<" + run + ">" : "[" + run + "]");
            }

            return builder.ToString();
        }

        private bool Same(string a, string b)
        {
            var countA = DigitRuns.Split(a, _starts, _lengths, _numeric);
            var countB = DigitRuns.Split(b, _starts2, _lengths2, _numeric2);
            return DigitRuns.SameShape(a, countA, _starts, _lengths, _numeric, b, countB, _starts2, _lengths2, _numeric2);
        }
    }
}
