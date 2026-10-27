namespace RowMate.Core.Common
{
    // Fuente de azar inyectable (AC-BASE-07). Con "sin ruido" el simulador no la consume.
    public interface IRandom
    {
        // Un double uniforme en [0; 1), como Random.nextDouble() de Dart.
        double NextDouble();
    }
}
