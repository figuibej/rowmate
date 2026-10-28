using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Lo que sale no responde (QA-G3-01; 03-rutinas/02-tech-design §4.2; Flutter: la ruta que sale ignora los toques y suelta
    // el foco, <flutter-sdk>/packages/flutter/lib/src/widgets/routes.dart:1153-1156, :1215-1223). Una sola vez, en el cuadro 0
    // de la salida de una página, un diálogo o un menú: cada elemento del árbol pasa a pickingMode = Ignore y focusable = false
    // (pickingMode NO se hereda en UI Toolkit: hay que recorrer el árbol), y si el foco estaba adentro, vuelve a quien lo tenía
    // antes de abrir (si sigue en el árbol y se puede enfocar) o se quita. No se deshace: lo que sale se dispone al terminar.
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
            Lock(root);
            if (!inside)
            {
                return;
            }

            focused!.Blur();
            if (restoreFocusTo is VisualElement target && target.panel != null && !root.Contains(target) && target.canGrabFocus)
            {
                target.Focus();
            }
        }

        // true si root ya está bloqueado (se aplicó Apply).
        public static bool IsLocked(VisualElement root)
        {
            return root.pickingMode == PickingMode.Ignore && !root.focusable;
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
