using System;

namespace RowMate.Core.Design
{
    // Qué celdas de un número cambian entre dos lecturas y hacia dónde ruedan (ANIMATION_BIBLE §2.4; 04-design-notes §5.1,
    // DN-08). Las celdas se alinean a la derecha: "9m" → "13m" compara 'm' con 'm' y '3' con '9', y el '1' es una celda nueva
    // (cuenta como cambiada). Struct sin asignaciones: guarda las dos cadenas que ya existen.
    public readonly struct DigitRollPlan
    {
        private readonly string _previous;
        private readonly string _next;

        public DigitRollPlan(string previous, string next, int direction)
        {
            _previous = previous ?? throw new ArgumentNullException(nameof(previous));
            _next = next ?? throw new ArgumentNullException(nameof(next));
            Direction = direction > 0 ? 1 : direction < 0 ? -1 : 0;
        }

        // Largo del texto nuevo (las celdas que se dibujan al terminar).
        public int Length => _next == null ? 0 : _next.Length;

        // Largo del texto anterior: si difiere, el ancho del número interpola en 180 ms (DigitRoll.WidthAt, DN-08 c).
        public int PreviousLength => _previous == null ? 0 : _previous.Length;

        // +1 el valor subió (los dígitos suben), −1 bajó, 0 igual. Un marcador sin dígitos ("--", "--:--") vale 0: al
        // apagarse los dígitos bajan y al encenderse suben.
        public int Direction { get; }

        public bool HasChanges
        {
            get
            {
                if (Length != PreviousLength)
                {
                    return true;
                }

                for (var i = 0; i < Length; i++)
                {
                    if (IsChanged(i))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // Celda i del texto nuevo (0 = la de la izquierda): cambió si es nueva o si su carácter difiere del de la misma
        // posición contando desde la derecha en el texto anterior.
        public bool IsChanged(int cell)
        {
            if (cell < 0 || cell >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(cell), cell, "DigitRollPlan: celda fuera del número.");
            }

            var previousCell = PreviousCellFor(cell);
            return previousCell < 0 || _previous[previousCell] != _next[cell];
        }

        // Índice en el texto anterior de la celda que ocupaba el lugar de la celda nueva, o −1 si la celda es nueva.
        public int PreviousCellFor(int cell)
        {
            var previous = cell - (Length - PreviousLength);
            return previous >= 0 && previous < PreviousLength ? previous : -1;
        }

        // Carácter que sale de la celda nueva (el anterior en esa posición), o '\0' si la celda es nueva.
        public char OutgoingAt(int cell)
        {
            var previousCell = PreviousCellFor(cell);
            return previousCell < 0 ? '\0' : _previous[previousCell];
        }

        public char IncomingAt(int cell)
        {
            return _next[cell];
        }

        // Celdas del texto anterior que ya no existen (largo menor): solo salen, a la izquierda del número.
        public int RemovedCells => PreviousLength > Length ? PreviousLength - Length : 0;
    }

    // Rodillo de dígitos (ANIMATION_BIBLE §2.4; curvas por celda de 04-design-notes §5.1, DN-08 d). Solo se animan la
    // posición y la opacidad de los glifos: el texto nuevo es el de la lectura desde el cuadro 0, nunca se dibuja un dígito que
    // no sea el viejo o el nuevo, y en u ≥ 1 se ve exactamente el valor de la lectura (AC-BLE-11-11). Si llega otra lectura
    // antes de terminar, quien dibuja salta al final y arranca un plan nuevo.
    public static class DigitRoll
    {
        // ease.enter (ANIMATION_BIBLE §2.4).
        public const double DurationMs = 180;

        // previousValue / nextValue: el valor numérico de cada texto (segundos del split, spm, W, m, bpm). Un texto sin
        // dígitos vale 0, sea cual sea el número que se pase.
        public static DigitRollPlan Plan(string previous, string next, double previousValue, double nextValue)
        {
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var from = HasDigits(previous) ? previousValue : 0;
            var to = HasDigits(next) ? nextValue : 0;
            var direction = to > from ? 1 : to < from ? -1 : 0;
            return new DigitRollPlan(previous, next, direction);
        }

        // u = t / 180, E = ease.enter.
        //   saliente: y = −dir·0,35·E(u) em, alfa = 1 − E(min(1, u / 0,6))
        //   entrante: y = +dir·0,35·(1 − E(u)) em, alfa = E(clamp((u − 0,15) / 0,85))
        // Reducir movimiento: fundido cruzado lineal de 80 ms en el lugar (saliente 1 − u, entrante u, sin desplazamiento).
        // y positivo = hacia abajo (como el eje de UI Toolkit): un valor que sube hace entrar el dígito nuevo desde abajo.
        public static void Evaluate(double elapsedMs, int direction, bool reduceMotion,
                                    out double outOffsetEm, out double outAlpha, out double inOffsetEm, out double inAlpha)
        {
            var dir = direction > 0 ? 1 : direction < 0 ? -1 : 0;
            if (reduceMotion)
            {
                var r = Clamp01(elapsedMs / MotionSpecs.ReducedFadeMs);
                outOffsetEm = 0;
                inOffsetEm = 0;
                outAlpha = 1 - r;
                inAlpha = r;
                return;
            }

            var u = Clamp01(elapsedMs / DurationMs);
            var e = Easing.Evaluate(Tokens.EaseEnter, u);
            outOffsetEm = -dir * MotionSpecs.RollShiftEm * e;
            inOffsetEm = dir * MotionSpecs.RollShiftEm * (1 - e);
            outAlpha = 1 - Easing.Evaluate(Tokens.EaseEnter, Math.Min(1, u / MotionSpecs.RollOutFadeEnd));
            inAlpha = Easing.Evaluate(Tokens.EaseEnter, Clamp01((u - MotionSpecs.RollInFadeStart) / (1 - MotionSpecs.RollInFadeStart)));
            if (u >= 1)
            {
                // Exacto al final: el entrante en su lugar y opaco, el saliente invisible.
                inOffsetEm = 0;
                inAlpha = 1;
                outAlpha = 0;
            }
        }

        // Ancho del número mientras cambia de largo (DN-08 c): interpola con ease.enter en 180 ms; las celdas quedan pegadas
        // a la derecha.
        public static double WidthAt(double elapsedMs, double fromWidth, double toWidth)
        {
            var u = Clamp01(elapsedMs / DurationMs);
            return fromWidth + (toWidth - fromWidth) * Easing.Evaluate(Tokens.EaseEnter, u);
        }

        public static bool IsFinished(double elapsedMs, bool reduceMotion)
        {
            return elapsedMs >= (reduceMotion ? MotionSpecs.ReducedFadeMs : DurationMs);
        }

        private static bool HasDigits(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] >= '0' && text[i] <= '9')
                {
                    return true;
                }
            }

            return false;
        }

        private static double Clamp01(double value)
        {
            if (double.IsNaN(value) || value <= 0)
            {
                return 0;
            }

            return value >= 1 ? 1 : value;
        }
    }
}
