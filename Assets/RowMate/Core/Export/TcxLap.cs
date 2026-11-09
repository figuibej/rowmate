namespace RowMate.Core.Export
{
    // Una vuelta del TCX: el índice de su primer punto y cuántos puntos tiene (It. 12; 02-tech-design §2.2). Los puntos son una
    // racha consecutiva de la lista ordenada por el tiempo del monitor.
    public readonly struct TcxLap
    {
        public TcxLap(int start, int count)
        {
            Start = start;
            Count = count;
        }

        public int Start { get; }

        public int Count { get; }

        // Índice del último punto de la vuelta.
        public int Last => Start + Count - 1;
    }
}
