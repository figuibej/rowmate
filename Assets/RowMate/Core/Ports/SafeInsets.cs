namespace RowMate.Core.Ports
{
    // Márgenes seguros del sistema en dp, medidos desde cada borde de la ventana (Screen.safeArea / densidad): barra de
    // estado, barra de gestos, recortes de pantalla. En Windows y en el Editor son 0. Regla única de uso
    // (02-tech-design §3.1, QA-G2-06): la barra superior consume el superior y los laterales, la de pestañas el inferior y
    // los laterales, el contenido no consume nada y la capa del simulador tampoco.
    public readonly struct SafeInsets
    {
        public static readonly SafeInsets None = new SafeInsets(0, 0, 0, 0);

        public readonly double Left;
        public readonly double Top;
        public readonly double Right;
        public readonly double Bottom;

        public SafeInsets(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public bool IsNone => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;
    }
}
