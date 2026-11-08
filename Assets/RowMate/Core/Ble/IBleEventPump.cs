namespace RowMate.Core.Ble
{
    // La bomba de eventos nativos (ADR 009, modelo pull; 02-tech-design §5.2). AppRoot la llama en cada cuadro ANTES de
    // scheduler.Tick() (resuming: false) y al volver del segundo plano ANTES de ResumeFromBackground() (resuming: true). Sin
    // eventos pendientes no asigna memoria.
    public interface IBleEventPump
    {
        void Drain(bool resuming);
    }
}
