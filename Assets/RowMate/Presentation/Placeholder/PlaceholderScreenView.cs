using RowMate.App.Shell;
using RowMate.Core.Localization;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Placeholder
{
    // Pestañas provisorias de la It. 2: Workout, Rutinas, Historial y Perfil (AC-APP-02-08, P-05; 02-tech-design §3.6).
    // Barra superior con el título del catálogo (workoutTitle, routinesTitle, historyTitle, profileTitle) y, centrado entre las
    // dos barras, el glifo SIN elegir de su pestaña a 56 dp, blanco al 24 % (opacidad 0,24, Base.uss). Ningún otro texto ni
    // acción; el catálogo no suma claves.
    public sealed class PlaceholderScreenView : VisualElement
    {
        public PlaceholderScreenView(UiContext ui, ShellLayout layout, TabDestination destination)
        {
            var id = ScreenId(destination.Id);
            name = "screen-" + id;
            AddToClassList("rm-screen");
            AddToClassList("rm-placeholder");
            Destination = destination;
            AppBar = new AppBarView(id + "-appbar", layout);
            Add(AppBar);
            var body = new VisualElement();
            body.AddToClassList("rm-placeholder__body");
            body.pickingMode = PickingMode.Ignore;
            layout.RegisterBottomPadding(body, 0);
            Icon = new IconView(destination.IconFor(false), destination.FilledFor(false), 56);
            Icon.AddToClassList("rm-placeholder__icon");
            body.Add(Icon);
            Add(body);
            this.Bind(ui.App.Texts, OnTexts);
        }

        public TabDestination Destination { get; }

        public AppBarView AppBar { get; }

        public IconView Icon { get; }

        public static string ScreenId(TabDestinationId id)
        {
            switch (id)
            {
                case TabDestinationId.Device:
                    return "device";
                case TabDestinationId.Workout:
                    return "workout";
                case TabDestinationId.Routines:
                    return "routines";
                case TabDestinationId.History:
                    return "history";
                default:
                    return "profile";
            }
        }

        private void OnTexts(Localizer texts)
        {
            AppBar.SetTitle(texts.Get(Destination.TitleKey));
        }
    }
}
