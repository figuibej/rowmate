using System;
using RowMate.Core.Text;

namespace RowMate.Core.Workout
{
    // Reloj activo por tiempo monótono (ADR 017, decisión 4; ARCHITECTURE §6.1, regla 2; 02-tech-design §2.2 de la It. 4a).
    // Reemplaza al Timer.periodic(1 s) del legacy (workout_provider.dart:147, :247): no cuenta disparos, cuenta los segundos
    // enteros vencidos desde el inicio del TRAMO activo. Un cuadro tarde procesa todos los segundos vencidos, en orden, y no
    // se pierde ni se duplica ninguno (AC-WKT-02-04, DV-27). Pausar cierra el tramo y descarta la fracción; reanudar abre uno
    // nuevo (Q-58). En ticks de 100 ns (long): el borde del segundo no depende de la coma flotante.
    public sealed class ActiveClock
    {
        public const long TicksPerSecond = TimeSpan.TicksPerSecond;

        private long _segmentStart;

        public bool IsRunning { get; private set; }

        // Segundos ya procesados del tramo.
        public int SegmentSeconds { get; private set; }

        // DueAtTicks(SegmentSeconds + 1).
        public long NextDueTicks => DueAtTicks(SegmentSeconds + 1);

        // Abre un tramo: su segundo 1 vence en now + 1 s.
        public void Start(long nowTicks)
        {
            _segmentStart = nowTicks;
            SegmentSeconds = 0;
            IsRunning = true;
        }

        // Cierra el tramo; lo que corría del segundo se pierde (Q-58).
        public void Stop()
        {
            IsRunning = false;
            SegmentSeconds = 0;
        }

        // Segundos vencidos sin procesar; 0 si no corre.
        public int Due(long nowTicks)
        {
            if (!IsRunning || nowTicks <= _segmentStart)
            {
                return 0;
            }

            var due = (nowTicks - _segmentStart) / TicksPerSecond - SegmentSeconds;
            return due <= 0 ? 0 : due > int.MaxValue ? int.MaxValue : (int)due;
        }

        // Instante monótono del segundo n del tramo (n ≥ 1).
        public long DueAtTicks(int segmentSecond)
        {
            return _segmentStart + segmentSecond * TicksPerSecond;
        }

        public void MarkProcessed()
        {
            SegmentSeconds++;
        }

        // s · 10⁷ redondeado (DartMath.Round: Math.Round no se usa en Core, ADR 012).
        public static long ToTicks(double monotonicSeconds)
        {
            return DartMath.Round(monotonicSeconds * TicksPerSecond);
        }
    }
}
