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

namespace RowMate.Presentation.Shell
{
    // It. 9 (AC-HIS-09-01): una página que también se cierra con Escape en el escritorio (el detalle de sesión). Las demás siguen
    // la regla P-08 (Escape no cierra pantallas). BackNavigation la consulta después de los menús y los diálogos: una pulsación
    // sigue cerrando un solo nivel.
    public interface IClosesOnEscape
    {
    }
}
