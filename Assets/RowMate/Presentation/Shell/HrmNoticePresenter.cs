using System;
using RowMate.App.Device;
using RowMate.Core.Common;

namespace RowMate.Presentation.Shell
{
    // Los avisos breves del pulsómetro (It. 11, 02-tech-design §4.3; DT11-12, DT11-13; AC-HRM-07-05, AC-HRM-08-03): escucha
    // HeartRateStore.Notices y los pasa al ToastLayer (ShowHrm).
    // - Connected ("Pulsómetro conectado", 2 s): en cualquier pestaña, también Dispositivo y sobre el HUD (CAP-12).
    // - Lost ("Se perdió el pulsómetro, reconectando…", 4 s): solo con el entrenamiento a la vista (workoutNoticesVisible, el mismo
    //   predicado que el aviso del remo: WorkoutScreenView.ShowsLinkNotices).
    // Con el aviso sobre el HUD, la banda del inmersivo pasa a ser obstáculo del aviso (onHud, como el del remo). Lo descartado (fuera del
    // entrenamiento o porque hay un aviso del remo) va solo al registro: "[HRM] aviso descartado: …".
    public sealed class HrmNoticePresenter : IDisposable
    {
        public const string Tag = "HRM";

        private readonly UiContext _ui;
        private readonly ToastLayer _toasts;
        private readonly Func<bool> _workoutNoticesVisible;
        private readonly Action? _onHud;
        private IDisposable? _subscription;

        public HrmNoticePresenter(UiContext ui, ToastLayer toasts, Func<bool> workoutNoticesVisible, Action? onHud = null)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
            _workoutNoticesVisible = workoutNoticesVisible ?? throw new ArgumentNullException(nameof(workoutNoticesVisible));
            _onHud = onHud;
            _subscription = ui.App.HeartRate.Notices.Subscribe(OnNotice);
        }

        // Avisos mostrados y descartados (pruebas).
        public int Shown { get; private set; }

        public int Discarded { get; private set; }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void OnNotice(HrmNotice notice)
        {
            if (_subscription == null)
            {
                return;
            }

            var overHud = _workoutNoticesVisible();
            if (!HrmNoticeRules.ShouldShow(notice.Kind, overHud))
            {
                Discard(notice, "fuera del entrenamiento");
                return;
            }

            if (overHud)
            {
                _onHud?.Invoke();
            }

            if (!_toasts.ShowHrm(HrmNoticeText.Toast(notice, _ui.Texts)))
            {
                Discard(notice, "hay un aviso del remo");
                return;
            }

            Shown++;
        }

        private void Discard(HrmNotice notice, string why)
        {
            Discarded++;
            _ui.Logger.Log(LogLevel.Info, Tag, "aviso descartado: " + notice.Kind + " (" + why + ")");
        }
    }
}
