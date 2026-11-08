using System;
using RowMate.Core.Localization;

namespace RowMate.App.Device
{
    // Los textos de los avisos del enlace con el nombre APARTE (A10-08, DT10-14): {name} se corta a 24 caracteres con … y el texto
    // fijo nunca se corta. Un remo sin nombre es deviceUnknown (spec §4).
    public static class LinkNoticeText
    {
        public const int MaxNameChars = 24;

        public static readonly TimeSpan ReconnectedDuration = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan MonitorStoppedDuration = TimeSpan.FromSeconds(4);

        // El nombre que va en {name}: el del remo cortado a 24 con …, o deviceUnknown.
        public static string Name(string? name, Localizer texts)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            if (string.IsNullOrEmpty(name))
            {
                return texts.Get(L10nKeys.DeviceUnknown);
            }

            return name!.Length <= MaxNameChars ? name : name.Substring(0, MaxNameChars - 1) + "…";
        }

        public static string Reconnecting(string? name, Localizer texts)
        {
            return texts.Format(L10nKeys.DeviceReconnecting, MessageArg.Text("name", Name(name, texts)));
        }

        public static string Reconnected(string? name, Localizer texts)
        {
            return texts.Format(L10nKeys.DeviceReconnected, MessageArg.Text("name", Name(name, texts)));
        }

        // El aviso sobre el HUD (AC-BLE-08-06, AC-BLE-09-02): Reconnecting fijo (Duration null), Reconnected 2 s, MonitorStopped 4 s;
        // Cleared → null (se quita el fijo).
        public static LinkToast? Toast(LinkNotice notice, Localizer texts)
        {
            if (notice == null)
            {
                throw new ArgumentNullException(nameof(notice));
            }

            switch (notice.Kind)
            {
                case LinkNoticeKind.Reconnecting:
                    return new LinkToast(notice.Kind, Reconnecting(notice.Name, texts), null);
                case LinkNoticeKind.Reconnected:
                    return new LinkToast(notice.Kind, Reconnected(notice.Name, texts), ReconnectedDuration);
                case LinkNoticeKind.MonitorStopped:
                    return new LinkToast(notice.Kind, texts.Get(L10nKeys.DeviceMonitorStopped), MonitorStoppedDuration);
                default:
                    return null;
            }
        }
    }

    // Lo que dibuja ToastLayer.ShowLink (WP-B1): el tipo (para el indicador), el texto y cuánto dura (null = fijo).
    public sealed record LinkToast(LinkNoticeKind Kind, string Text, TimeSpan? Duration);
}
