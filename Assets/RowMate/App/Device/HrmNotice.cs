namespace RowMate.App.Device
{
    // Qué aviso del pulsómetro (It. 11, DT11-12): Connected = una reconexión automática salió bien (AC-HRM-07-05); Lost = corrida
    // nueva de la cadencia de caída (AC-HRM-08-03).
    public enum HrmNoticeKind
    {
        Connected,
        Lost
    }

    // Un aviso del pulsómetro que emite HeartRateStore.Notices; Name es el nombre del sensor ("" o null si no tiene).
    public sealed record HrmNotice(HrmNoticeKind Kind, string? Name);

    // Qué aviso hay a la vista en la capa de avisos cuando llega uno del pulsómetro (QA-G11-03, DT11-13): ninguno, uno común, uno del
    // remo (CurrentLink) o uno del pulsómetro (CurrentHrm).
    public enum ToastSlot
    {
        None,
        Common,
        Rower,
        Hrm
    }
}
