namespace RowMate.Core.Workout
{
    // El estado del cierre de la sesión (02-tech-design §2.6 de la It. 4b): None fuera de Finished; Pending con el cierre en
    // vuelo; Saved o Failed cuando volvió (o venció su tope de 15 s de primer plano: Failed). Con la rutina terminada sola el
    // motor queda en Finished hasta AcknowledgeCompletion (DT4a-15), así que la foto dice si el cierre ya volvió.
    public enum CloseStatus
    {
        None,
        Pending,
        Saved,
        Failed,
    }
}
