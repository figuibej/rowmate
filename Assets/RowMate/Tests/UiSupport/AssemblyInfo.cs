// Assembly RowMate.Tests.UiSupport: host de UI determinista sobre una RT (UiTestHost) y sus utilidades, compartido por las
// pruebas PlayMode y el arnés de capturas (02-tech-design §4.2). Sin [Test] ni nunit.framework.dll; solo con el test
// framework (UNITY_INCLUDE_TESTS): nunca llega a un build. Referencia RowMate.Simulator* por nombre: sin el define no
// existen y Unity descarta la referencia; el código que los usa va en #if ROWMATE_SIMULATOR.
namespace RowMate.Tests.UiSupport
{
    internal static class AssemblyMarker
    {
    }
}
