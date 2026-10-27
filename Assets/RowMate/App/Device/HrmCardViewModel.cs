using System.Collections.Generic;

namespace RowMate.App.Device
{
    // Todo lo que dibuja la tarjeta del pulsómetro, con los textos ya resueltos (HrmCardRules.Build; 02-tech-design §2.5).
    // La vista (HeartRateCardView, WP-B) solo lo traduce a elementos: no decide nada.
    // - Title: hrmTitle en mayúsculas (PULSÓMETRO / HEART RATE SENSOR; toUpperCase de Dart no depende del idioma).
    // - HeartFull: pleno con el pulsómetro conectado, con o sin lectura; si no, al 40 % (AC-HRM-13-02).
    // - HeartBeating: conectado y bpm > 0 (AC-HRM-13-12; con "--" queda quieto y pleno).
    // - Body y los textos de cada cuerpo: Connected (nombre, "142" o "--", "bpm"), Connecting ("Conectando a {name}..."),
    //   Scanning (texto y filas), Remembered ("{recordado} · reconectando..." o "· no encontrado"), NoSensor (texto y pista).
    // - SearchEnabled (Buscar y Buscar otro) y ConnectEnabled (Conectar): AC-HRM-13-04. Olvidar, Desconectar y las filas,
    //   siempre habilitados.
    // - ErrorText: hrmIncompatible o hrmConnectError con el mensaje (D-25), o null; la vista lo corta a 3 líneas.
    // Sin Cancelar, sin señal, sin mensaje de lista vacía ni "detener búsqueda" (AC-HRM-13-11).
    public sealed record HrmCardViewModel(
        string Title,
        bool HeartFull,
        bool HeartBeating,
        int Bpm,
        HrmCardBody Body,
        string ConnectedName,
        string BpmText,
        string BpmUnit,
        string ConnectingText,
        string SearchingText,
        IReadOnlyList<HrmDeviceRow> Rows,
        string RememberedLine,
        string NoSensorText,
        string WatchHint,
        bool SearchEnabled,
        bool ConnectEnabled,
        string SearchLabel,
        string SearchOtherLabel,
        string ConnectLabel,
        string ForgetLabel,
        string DisconnectLabel,
        string? ErrorText);
}
