using System;
using RowMate.Core.Localization;

namespace RowMate.App.Device
{
    // Los textos y duraciones de los avisos del pulsómetro (It. 11, spec §4; DV-108, DV-109): Connected 2 s, Lost 4 s. El texto es
    // fijo, sin el nombre del sensor.
    public static class HrmNoticeText
    {
        public static readonly TimeSpan ConnectedDuration = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan LostDuration = TimeSpan.FromSeconds(4);

        public static HrmToast Toast(HrmNotice notice, Localizer texts)
        {
            if (notice == null)
            {
                throw new ArgumentNullException(nameof(notice));
            }

            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            return notice.Kind == HrmNoticeKind.Connected
                ? new HrmToast(notice.Kind, texts.Get(L10nKeys.HrmConnectedNotice), ConnectedDuration)
                : new HrmToast(notice.Kind, texts.Get(L10nKeys.HrmLostNotice), LostDuration);
        }
    }

    // Lo que dibuja ToastLayer.ShowHrm (WP-B1): el tipo (para el indicador Heart o HeartBroken), el texto y cuánto dura.
    public sealed record HrmToast(HrmNoticeKind Kind, string Text, TimeSpan Duration);
}
