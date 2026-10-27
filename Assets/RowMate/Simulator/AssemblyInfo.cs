// Atributos del assembly RowMate.Simulator (puro, solo con ROWMATE_SIMULATOR).
// Sus internos se exponen solo a los tests del simulador y a la compilación de compatibilidad con Unity.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RowMate.Tests.Simulator")]
[assembly: InternalsVisibleTo("RowMate.Tests.UnityApiCheck")]
