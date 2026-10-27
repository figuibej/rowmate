// Shim de C# 9 para `record` e `init` en .NET Standard 2.1 (Unity). Interno: uno por assembly de producción
// que declara records (Core, App, Simulator). Los assemblies de test no declaran records (02-tech-design §2.4).
namespace System.Runtime.CompilerServices
{
#if !NET5_0_OR_GREATER
    internal static class IsExternalInit
    {
    }
#endif
}
