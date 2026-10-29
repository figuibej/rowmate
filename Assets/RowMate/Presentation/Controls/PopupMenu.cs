using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Menú emergente propio (03-rutinas/02-tech-design §4.6, DT-15; 04-design-notes §4.4): el ⋮ de la tarjeta de rutina y el
    // menú del clic derecho de los campos. No es GenericDropdownMenu: ese se cuelga de la raíz del panel, por encima de la
    // capa del simulador (S3-b). Lo muestra PopupLayer, encima de diálogos y avisos y debajo del simulador.
    //   .rm-menu [--dense]
    //   └── #<nombre> .rm-menu__item [--danger]   Button; .rm-button__state, .rm-menu__label .rm-type-body-m, anillo interior
    // It. 4a (D-ART-10, R6; DD4a-02): el menú trata la caja de la llave del simulador, agrandada 8 dp, como un borde: si se
    // cruzaría con ella, se abre del otro lado del ancla (o del puntero) y, si tampoco entra, se corre en x (AvoidWrench, que
    // llama PopupLayer al ubicarlo).
    public sealed class PopupMenu : VisualElement
    {
        public const string UssClass = "rm-menu";

        // El aire de la llave (00-deuda-diseno §1.2: "tapar" = la llave agrandada 8 dp se cruza con el control).
        public const float WrenchMarginDp = 8;

        // La caja del menú (en coordenadas del panel) que no se cruza con la llave agrandada: la que se pidió si no la cruza; si
        // no, la del otro lado en y (alternateTop); si tampoco, corrida en x a un lado de la llave, dentro de la ventana.
        // flipped = se usó el otro lado en y.
        public static Rect AvoidWrench(Rect menu, Rect? wrench, float alternateTop, Rect window, float edge, out bool flipped)
        {
            flipped = false;
            if (!wrench.HasValue)
            {
                return menu;
            }

            var w = wrench.Value;
            var grown = new Rect(w.xMin - WrenchMarginDp, w.yMin - WrenchMarginDp, w.width + 2 * WrenchMarginDp, w.height + 2 * WrenchMarginDp);
            if (!menu.Overlaps(grown))
            {
                return menu;
            }

            var top = Mathf.Clamp(alternateTop, window.yMin + edge, Mathf.Max(window.yMin + edge, window.yMax - edge - menu.height));
            var other = new Rect(menu.x, top, menu.width, menu.height);
            if (!other.Overlaps(grown))
            {
                flipped = true;
                return other;
            }

            // Corrido en x: a la derecha de la llave si entra; si no, a su izquierda.
            var right = grown.xMax;
            if (right + menu.width <= window.xMax - edge)
            {
                return new Rect(right, menu.y, menu.width, menu.height);
            }

            var left = grown.xMin - menu.width;
            return new Rect(Mathf.Max(window.xMin + edge, left), menu.y, menu.width, menu.height);
        }

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
