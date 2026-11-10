using System;

namespace RowMate.Core.Accessibility
{
    // El rol que el lector de pantalla anuncia (It. 14, AC-APP-03-12; DT14-17, DT14-18). Espeja los de AccessibilityRole de
    // Unity 6.3 que usa la app; el puente de Presentation los traduce uno a uno.
    public enum A11yRole
    {
        None,
        Button,
        TabBar,
        TabButton,
        Header,
        StaticText,
        TextField,
        Toggle,
        ScrollView,
        Container,
        Image,
    }

    // Estados (AccessibilityState de Unity): elegida, deshabilitada y desplegado.
    [Flags]
    public enum A11yState
    {
        None = 0,
        Selected = 1,
        Disabled = 2,
        Expanded = 4,
    }
}
