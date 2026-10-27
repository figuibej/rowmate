// Assembly RowMate.Editor: bootstrapper, modo simulador, builds por línea de comandos y menús de desarrollo.
// Namespace RowMate.EditorTools, no RowMate.Editor (QA-V-04 de la It. 1, ARCHITECTURE §17): un namespace
// RowMate.Editor tapa a UnityEditor.Editor dentro de RowMate.* y `class X : Editor` da CS0118. Un solo namespace para
// todo el assembly, sin subnamespaces que tapen a RowMate.Simulator o RowMate.Bootstrap.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RowMate.Tests.UnityEditMode")]

namespace RowMate.EditorTools
{
    internal static class AssemblyMarker
    {
    }
}
