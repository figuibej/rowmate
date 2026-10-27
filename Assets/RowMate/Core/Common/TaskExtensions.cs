using System;
using System.Threading.Tasks;

namespace RowMate.Core.Common
{
    public static class TaskExtensions
    {
        // Tarea "disparada y olvidada" (= unawaited(...).catchError del legacy, ARCHITECTURE §6):
        // nunca se pierde una excepción, se registra con el tag dado. Una cancelación se registra como Debug.
        // La continuación vuelve al contexto que llama (sin ConfigureAwait(false)): en Unity, el hilo principal.
        public static void Forget(this Task task, IAppLogger logger, string tag)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (task.IsCompleted)
            {
                LogOutcome(task, logger, tag);
                return;
            }

            _ = ObserveAsync(task, logger, tag);
        }

        private static async Task ObserveAsync(Task task, IAppLogger logger, string tag)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException canceled)
            {
                logger.Log(LogLevel.Debug, tag, "tarea cancelada", canceled);
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, tag, error.Message, error);
            }
        }

        private static void LogOutcome(Task task, IAppLogger logger, string tag)
        {
            if (task.IsCanceled)
            {
                logger.Log(LogLevel.Debug, tag, "tarea cancelada");
                return;
            }

            var aggregate = task.Exception;
            if (task.IsFaulted && aggregate != null)
            {
                var error = aggregate.InnerExceptions.Count == 1 ? aggregate.InnerExceptions[0] : aggregate;
                logger.Log(LogLevel.Error, tag, error.Message, error);
            }
        }
    }
}
