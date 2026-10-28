using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Menú emergente propio (03-rutinas/02-tech-design §4.6, DT-15; 04-design-notes §4.4): el ⋮ de la tarjeta de rutina y el
    // menú del clic derecho de los campos. No es GenericDropdownMenu: ese se cuelga de la raíz del panel, por encima de la
    // capa del simulador (S3-b). Lo muestra PopupLayer, encima de diálogos y avisos y debajo del simulador.
    //   .rm-menu [--dense]
    //   └── #<nombre> .rm-menu__item [--danger]   Button; .rm-button__state, .rm-menu__label .rm-type-body-m, anillo interior
    public sealed class PopupMenu : VisualElement
    {
        public const string UssClass = "rm-menu";

        private readonly List<Button> _items = new List<Button>();

        public PopupMenu(bool dense)
        {
            AddToClassList(UssClass);
            EnableInClassList(UssClass + "--dense", dense);
            IsDense = dense;
        }

        public bool IsDense { get; }

        public IReadOnlyList<Button> Items => _items;

        // Lo que hace el ítem lo decide quien arma el menú; PopupLayer cierra el menú en el cuadro 0 y después llama a la acción.
        internal Action<Action>? OnChoose { get; set; }

        public Button AddItem(string name, string label, bool danger, Action action, bool focusable = true)
        {
            var item = new Button { name = name, text = "" };
            RmButton.StripUnityChrome(item);
            item.AddToClassList(UssClass + "__item");
            item.EnableInClassList(UssClass + "__item--danger", danger);
            item.focusable = focusable;
            item.Add(RmButton.StateLayer());
            var text = new Label(label);
            text.AddToClassList(UssClass + "__label");
            text.AddToClassList("rm-type-body-m");
            text.pickingMode = PickingMode.Ignore;
            item.Add(text);
            var ring = new VisualElement();
            ring.AddToClassList("rm-focus-ring");
            ring.AddToClassList("rm-focus-ring--inset");
            ring.pickingMode = PickingMode.Ignore;
            item.Add(ring);
            item.clicked += () =>
            {
                if (OnChoose != null)
                {
                    OnChoose(action);
                }
                else
                {
                    action();
                }
            };
            _items.Add(item);
            Add(item);
            return item;
        }
    }
}
