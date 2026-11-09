using System;

namespace RowMate.Presentation.Shell
{
    // Un aviso con acción (It. 12; 12-exportar-tcx/02-tech-design §5.2, A12-04; AC-STR-09-19, -21): el título (ajusta en varias
    // líneas, nunca "…"), un segundo renglón opcional en texto secundario (el nombre del archivo), el check dibujado del
    // LinkIndicator (éxito) o nada (error), el texto de la acción ("Abrir carpeta", "Reintentar") con lo que hace al tocarla, y la
    // duración (6 s, ExportMotionSpecs.NoticeMs). Lo muestra ToastLayer.ShowAction con la prioridad de un aviso común.
    // Clase y no record: Presentation no tiene el shim de IsExternalInit.
    public sealed class ActionToast
    {
        public ActionToast(string title, string? detail, bool check, string actionText, Action onAction, TimeSpan duration)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Detail = detail;
            Check = check;
            ActionText = actionText ?? throw new ArgumentNullException(nameof(actionText));
            OnAction = onAction ?? throw new ArgumentNullException(nameof(onAction));
            Duration = duration;
        }

        public string Title { get; }

        public string? Detail { get; }

        public bool Check { get; }

        public string ActionText { get; }

        public Action OnAction { get; }

        public TimeSpan Duration { get; }
    }
}
