using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Lo que sale no responde (QA-G3-01; 03-rutinas/02-tech-design §4.2; Flutter: la ruta que sale ignora los toques y suelta
    // el foco, <flutter-sdk>/packages/flutter/lib/src/widgets/routes.dart:1153-1156, :1215-1223). Una sola vez, en el cuadro 0
    // de la salida de una página, un diálogo o un menú: cada elemento del árbol pasa a pickingMode = Ignore y focusable = false
    // (pickingMode NO se hereda en UI Toolkit: hay que recorrer el árbol), y si el foco estaba adentro, se quita. No se
    // deshace: lo que sale se dispone al terminar.
    // DT4a-01 (It. 4a; 00-deuda-it3 §11.1, camino a; aceptado por el PO en AC-APP-05-07): el foco vuelve a quien lo tenía antes
    // de abrir SOLO si se estaba navegando con teclado (.rm-keyboard-nav en la raíz, FocusHighlight); con el mouse o un toque
    // se quita y no queda ningún control enfocado, como Flutter de escritorio, que no enfoca el botón con el clic: así un
    // Enter después de Cancelar no vuelve a abrir el diálogo de terminar.
    // Costo: un recorrido del árbol (≈ 1 100 elementos con un editor de 60 filas).
    public static class ExitLock
    {
        public static void Apply(VisualElement root, Focusable? restoreFocusTo)
        {
            if (root == null)
            {
                return;
            }

            var focused = root.panel?.focusController?.focusedElement as VisualElement;
            var inside = focused != null && (ReferenceEquals(focused, root) || root.Contains(focused));
            var keyboard = IsKeyboardNavigation(root);
            Lock(root);
            if (!inside)
            {
                return;
            }

            focused!.Blur();
            if (keyboard && restoreFocusTo is VisualElement target && target.panel != null && !root.Contains(target) && target.canGrabFocus)
            {
                target.Focus();
            }
        }

        // true si root ya está bloqueado (se aplicó Apply).
        public static bool IsLocked(VisualElement root)
        {
            return root.pickingMode == PickingMode.Ignore && !root.focusable;
        }

        // Se está navegando con teclado: algún ancestro (la raíz de la app) tiene .rm-keyboard-nav.
        public static bool IsKeyboardNavigation(VisualElement element)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                if (e.ClassListContains(Controls.FocusHighlight.KeyboardNavClass))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Lock(VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;
            element.focusable = false;
            var count = element.hierarchy.childCount;
            for (var i = 0; i < count; i++)
            {
                Lock(element.hierarchy[i]);
            }
        }
    }
}
