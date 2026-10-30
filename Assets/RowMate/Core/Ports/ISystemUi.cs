namespace RowMate.Core.Ports
{
    // Las barras del sistema (F-APP-15; ARCHITECTURE §13: ISystemUi.SetImmersive; 02-tech-design §4 de la It. 4b). Android:
    // barras ocultas en modo inmersivo persistente; iOS: la barra de estado (10b); escritorio y Editor: sin efecto (paridad:
    // immersive_workout_screen.dart:32-41 no hace nada en escritorio). Lo pide ImmersiveSystemBars (App), con cuenta de
    // inmersivos; en las pruebas, FakeSystemUi anota cada pedido con su instante.
    public interface ISystemUi
    {
        void SetImmersive(bool immersive);
    }
}
