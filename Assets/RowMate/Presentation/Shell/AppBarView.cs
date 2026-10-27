using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Barra superior de cada pantalla (AC-APP-05-02; legacy AppBar con kToolbarHeight 56): .rm-appbar con el título
    // .rm-appbar__title .rm-type-title y las acciones .rm-appbar__actions a la derecha; fondo bg.app sin línea ni sombra
    // (Base.uss). El área segura superior y las laterales las pone ShellLayout (alto 56 + Top).
    public sealed class AppBarView : VisualElement
    {
        public const string UssClass = "rm-appbar";

        public AppBarView(string name, ShellLayout layout)
        {
            this.name = name;
            AddToClassList(UssClass);
            Title = new Label();
            Title.AddToClassList(UssClass + "__title");
            Title.AddToClassList("rm-type-title");
            Title.pickingMode = PickingMode.Ignore;
            Add(Title);
            Actions = new VisualElement();
            Actions.AddToClassList(UssClass + "__actions");
            Actions.pickingMode = PickingMode.Ignore;
            Add(Actions);
            layout.RegisterAppBar(this);
        }

        public Label Title { get; }

        public VisualElement Actions { get; }

        public void SetTitle(string title)
        {
            if (Title.text != title)
            {
                Title.text = title;
            }
        }
    }
}
