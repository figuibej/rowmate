using System;
using RowMate.App.Export;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Export;
using RowMate.Core.Localization;

namespace RowMate.Presentation.Shell
{
    // Los avisos de "Exportar TCX" (It. 12; 12-exportar-tcx/02-tech-design §5.2, DT12-13; AC-STR-09-19 a -22): escucha
    // TcxExportService.Notices (uno por app: el aviso llega aunque se haya salido del Detalle) y arma el ActionToast con el idioma
    // vigente, en la cápsula común del ToastLayer (ShowAction, prioridad de aviso común), por 6 s (ExportMotionSpecs.NoticeMs):
    // - Saved: "Guardado en {carpeta abreviada}" (ExportFolderLabel: nunca la ruta ni el usuario), el nombre del archivo en el
    //   segundo renglón, el check y "Abrir carpeta" (Reveal; el aviso sale en el acto).
    // - Failed: "No se pudo exportar la sesión", sin detalle ni indicador, y "Reintentar" (vuelve a exportar la misma sesión).
    // Share (Android) no avisa el éxito: el servicio no publica Saved.
    public sealed class ExportNoticePresenter : IDisposable
    {
        private readonly UiContext _ui;
        private readonly ToastLayer _toasts;
        private readonly TcxExportService _export;
        private IDisposable? _subscription;

        public ExportNoticePresenter(UiContext ui, ToastLayer toasts, TcxExportService export)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
            _export = export ?? throw new ArgumentNullException(nameof(export));
            _subscription = export.Notices.Subscribe(OnNotice);
        }

        // Avisos mostrados (pruebas).
        public int Shown { get; private set; }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        // El aviso de un resultado, con los textos del idioma vigente.
        public static ActionToast ToastFor(TcxExportNotice notice, Localizer texts, Action onAction)
        {
            if (notice == null)
            {
                throw new ArgumentNullException(nameof(notice));
            }

            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            var duration = TimeSpan.FromMilliseconds(ExportMotionSpecs.NoticeMs);
            if (notice.Kind == TcxExportNoticeKind.Saved && notice.File != null)
            {
                var folder = ExportFolderLabel.For(notice.File.Location, texts);
                return new ActionToast(texts.Format(L10nKeys.ExportTcxSaved, MessageArg.Text("folder", folder)), notice.File.FileName, true,
                    texts.Get(L10nKeys.ExportTcxOpenFolder), onAction, duration);
            }

            return new ActionToast(texts.Get(L10nKeys.ExportTcxFailed), null, false, texts.Get(L10nKeys.Retry), onAction, duration);
        }

        private void OnNotice(TcxExportNotice notice)
        {
            if (_subscription == null)
            {
                return;
            }

            Action onAction;
            if (notice.Kind == TcxExportNoticeKind.Saved && notice.File != null)
            {
                var file = notice.File;
                onAction = () => _export.Reveal(file);
            }
            else
            {
                var session = notice.Session;
                onAction = () => _export.ExportAsync(session).Forget(_ui.Logger, ExportLog.Tag);
            }

            _toasts.ShowAction(ToastFor(notice, _ui.Texts, onAction));
            Shown++;
        }
    }
}
