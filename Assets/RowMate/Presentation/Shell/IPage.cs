using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Una pantalla apilada sobre el shell (F-APP-03; 03-rutinas/02-tech-design §4.2): el editor de rutina en la It. 3; el
    // detalle de sesión (It. 9) y el inmersivo (It. 4a-4b) después. La crea quien la abre y la dispone OnPopped.
    public interface IPage
    {
        // Raíz de la página (.rm-page): ocupa todo el panel, con fondo bg.app, y recibe los eventos.
        VisualElement Root { get; }

        // Terminó la entrada.
        void OnPushed();

        // Salió del árbol: se dispone acá (desuscribe y dispone su store).
        void OnPopped();

        // "Atrás" del sistema con la página arriba: el editor hace Pop sin preguntar (AC-RUT-11-08).
        void OnBackRequested();
    }
}
