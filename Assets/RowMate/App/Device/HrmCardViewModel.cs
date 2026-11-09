using System;
using System.Collections.Generic;

namespace RowMate.App.Device
{
    // Todo lo que dibuja la tarjeta del pulsómetro, con los textos ya resueltos (HrmCardRules.Build; 02-tech-design §2.5).
    // La vista (HeartRateCardView, WP-B) solo lo traduce a elementos: no decide nada.
    // - Title: hrmTitle en mayúsculas (PULSÓMETRO / HEART RATE SENSOR; toUpperCase de Dart no depende del idioma).
    // - HeartFull: pleno con el pulsómetro conectado, con o sin lectura, salvo la lectura vencida (It. 11: al 40 %, AC-HRM-05-03);
    //   si no, al 40 % (AC-HRM-13-02).
    // - HeartBeating: conectado, bpm > 0 y sin lectura vencida (AC-HRM-13-12; con "--" queda quieto).
    // - HeartBreathing: recordado reintentando (D11-05).
    // - StaleText: hrmNoSignal con la lectura vencida (AC-HRM-05-03), si no null.
    // - Indicator, ArcStartAt (hora monótona del inicio de la espera) y ArcDuration: el indicador del recordado (A11-02).
    // - Body y los textos de cada cuerpo: Connected (nombre, "142" o "--", "bpm"), Connecting ("Conectando a {name}..."),
    //   Scanning (texto y filas), Remembered (RememberedName + RememberedSuffix " · reconectando..." o " · no encontrado", en dos
    //   etiquetas de una fila: el … corta solo el nombre), NoSensor (texto y pista).
    // - SearchEnabled (Buscar y Buscar otro) y ConnectEnabled (Conectar): AC-HRM-13-04. Olvidar, Desconectar y las filas,
    //   siempre habilitados.
    // - ErrorHuman (hrmScanFailed, hrmConnectFailed o hrmIncompatible) y ErrorDetail (el detalle técnico sin traducir; null en
    //   incompatible), o los dos null (AC-HRM-13-14).
    // Sin Cancelar, sin intensidad de señal, sin mensaje de lista vacía ni "detener búsqueda" (AC-HRM-13-11).
    public sealed record HrmCardViewModel(
        string Title,
        bool HeartFull,
        bool HeartBeating,
        bool HeartBreathing,
        int Bpm,
        HrmCardBody Body,
        string ConnectedName,
        string BpmText,
        string BpmUnit,
        string? StaleText,
        string ConnectingText,
        string SearchingText,
        IReadOnlyList<HrmDeviceRow> Rows,
        HrmIndicator Indicator,
        double? ArcStartAt,
        TimeSpan ArcDuration,
        string RememberedName,
        string RememberedSuffix,
        string NoSensorText,
        string WatchHint,
        bool SearchEnabled,
        bool ConnectEnabled,
        string SearchLabel,
        string SearchOtherLabel,
        string ConnectLabel,
        string ForgetLabel,
        string DisconnectLabel,
        string? ErrorHuman,
        string? ErrorDetail)
    {
        // La fila completa del recordado: "{nombre} · reconectando..." (hc:199-203). La vista de la It. 2 la usa hasta WP-B1.
        public string RememberedLine => RememberedName + RememberedSuffix;

        // El error en un solo texto (la frase y, debajo, el detalle). La vista de la It. 2 lo usa hasta WP-B1.
        public string? ErrorText => ErrorHuman == null ? null : ErrorDetail == null ? ErrorHuman : ErrorHuman + "\n" + ErrorDetail;
    }
}
