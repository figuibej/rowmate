namespace RowMate.App.Shell
{
    // Un destino de la barra de pestañas (AC-APP-02-01): etiqueta (clave nav*), título de su barra superior (clave *Title,
    // AC-APP-02-08), ícono sin elegir y elegido (codepoints de Icons) y nombre de contrato del elemento ("tab-device"…).
    // El ícono depende solo de la selección, nunca del remo (AC-APP-02-05). Los íconos sin elegir van en contorno; el
    // elegido puede ir relleno (SelectedFilled: Workout y Perfil).
    // legacy: lib/main.dart:150-176 (NavigationDestination)
    public sealed record TabDestination(TabDestinationId Id, string LabelKey, string TitleKey,
        string Icon, string SelectedIcon, bool SelectedFilled, string ElementName)
    {
        public string IconFor(bool selected)
        {
            return selected ? SelectedIcon : Icon;
        }

        public bool FilledFor(bool selected)
        {
            return selected && SelectedFilled;
        }
    }
}
