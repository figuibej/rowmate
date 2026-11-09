using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Export;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.App.Export
{
    // "Exportar TCX" (F-STR-09; It. 12, 02-tech-design §4, DT12-06, -11, -12, -13, -19). Uno por app (AppContext.Export): salir
    // del Detalle no cancela y el aviso igual llega (AC-STR-09-18).
    // 1. Una exportación en curso por sesión: un segundo pedido devuelve la misma tarea.
    // 2. Busy suma el id; relee los PUNTOS por el repositorio (hilo de la base) y usa la sesión que recibe (la del Detalle, ya
    //    releída por la It. 9). Solo lee: nunca llama a una escritura del repositorio ni a las preferencias (AC-STR-09-11).
    // 3. Sin datos (TcxExportRules.IsExportable) → falla. Si no: zona del equipo AL EXPORTAR (una vez), nombre con historyFree
    //    del idioma vigente y [SIM] en el build del simulador, y el pedido al exportador con la FUNCIÓN que arma los bytes (el
    //    exportador real la corre fuera del hilo principal, DT12-08).
    // 4. El indicador dura al menos 300 ms (ExportMotionSpecs.MinBusyMs, con IScheduler.Delay del resto).
    // 5. Busy saca el id, registra ([EXPORT], sin rutas del equipo: ExportLog) y avisa: Saved solo con SaveToFolder, Failed
    //    siempre. ExportAsync nunca lanza. Dispose corta los avisos pendientes (la tarea del exportador termina sola).
    public sealed class TcxExportService : IDisposable
    {
        private static readonly long[] NoIds = new long[0];

        private readonly ISessionRepository _sessions;
        private readonly IFileExporter _exporter;
        private readonly IScheduler _scheduler;
        private readonly Func<TimeZoneInfo> _zone;
        private readonly IReadOnlyObservable<Localizer> _texts;
        private readonly bool _simulator;
        private readonly IAppLogger _logger;
        private readonly string _userProfile;
        private readonly ObservableValue<IReadOnlyCollection<long>> _busy;
        private readonly EventStream<TcxExportNotice> _notices;
        private readonly Dictionary<long, Task> _running = new Dictionary<long, Task>();
        private bool _disposed;

        public TcxExportService(ISessionRepository sessions, IFileExporter exporter, IScheduler scheduler, Func<TimeZoneInfo> zone,
                                IReadOnlyObservable<Localizer> texts, bool simulator, IAppLogger logger)
        {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _zone = zone ?? throw new ArgumentNullException(nameof(zone));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _simulator = simulator;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // La carpeta del usuario se cambia por %USERPROFILE% en el registro (DT12-19).
            _userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? "";
            _busy = new ObservableValue<IReadOnlyCollection<long>>(NoIds, logger);
            Busy = new ReadOnlyObservable<IReadOnlyCollection<long>>(_busy);
            _notices = new EventStream<TcxExportNotice>(logger);
        }

        public ExportDelivery Delivery => _exporter.Delivery;

        // Los ids en curso (cambia dos veces por exportación).
        public IReadOnlyObservable<IReadOnlyCollection<long>> Busy { get; }

        public IEventSource<TcxExportNotice> Notices => _notices;

        public bool IsBusy(long sessionId)
        {
            return Array.IndexOf((long[])_busy.Value, sessionId) >= 0;
        }

        public Task ExportAsync(WorkoutSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (!session.Id.HasValue)
            {
                // El botón no aparece sin id (DT12-17): solo queda en el registro.
                _logger.Log(LogLevel.Warning, ExportLog.Tag, "no se pudo exportar: la sesión no tiene id");
                return Task.CompletedTask;
            }

            var id = session.Id.Value;
            if (_running.TryGetValue(id, out var running))
            {
                return running;
            }

            var task = RunAsync(session, id);
            if (!task.IsCompleted)
            {
                _running[id] = task;
            }

            return task;
        }

        public void Reveal(ExportedFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (_disposed)
            {
                return;
            }

            try
            {
                _exporter.Reveal(file);
            }
            catch (Exception error)
            {
                // Una falla de "Abrir carpeta" solo queda en el registro (spec §10.4).
                _logger.Log(LogLevel.Warning, ExportLog.Tag, ExportLog.Failed(error, Replacements()));
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private async Task RunAsync(WorkoutSession session, long id)
        {
            SetBusy(id, true);
            var started = _scheduler.MonotonicSeconds;
            ExportedFile? file = null;
            Exception? failure = null;
            try
            {
                var points = await _sessions.GetPointsAsync(id);
                if (points == null || !TcxExportRules.IsExportable(SessionStats.Compute(points), points.Count))
                {
                    throw new InvalidOperationException("la sesión no tiene datos para exportar");
                }

                var zone = _zone();
                var texts = _texts.Value;
                var options = new TcxOptions(zone, _simulator);
                var baseName = TcxFileName.Base(session.StartedAt, zone, session.RoutineName, texts.Get(L10nKeys.HistoryFree), _simulator);
                var request = new ExportRequest(baseName, TcxDocument.Extension, TcxDocument.MimeType, texts.Get(L10nKeys.ExportTcxShareTitle),
                    () => TcxDocument.Encode(TcxBuilder.Build(session, points, options)));
                file = await _exporter.ExportAsync(request);
                if (file == null)
                {
                    throw new InvalidOperationException("el exportador no devolvió el archivo");
                }
            }
            catch (Exception error)
            {
                failure = error;
            }

            try
            {
                var remainingMs = ExportMotionSpecs.MinBusyMs - (_scheduler.MonotonicSeconds - started) * 1000.0;
                if (remainingMs > 0 && !_disposed)
                {
                    await _scheduler.Delay(TimeSpan.FromMilliseconds(remainingMs));
                }
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Debug, ExportLog.Tag, "espera mínima del indicador: " + error.Message);
            }

            _running.Remove(id);
            SetBusy(id, false);
            if (failure != null || file == null)
            {
                _logger.Log(LogLevel.Warning, ExportLog.Tag,
                    ExportLog.Failed(failure ?? new InvalidOperationException("sin archivo"), Replacements()));
                Notify(new TcxExportNotice(TcxExportNoticeKind.Failed, session, null));
                return;
            }

            _logger.Log(LogLevel.Info, ExportLog.Tag, ExportLog.Saved(file.FileName, FolderLabel(file.Location)));
            if (_exporter.Delivery == ExportDelivery.SaveToFolder)
            {
                Notify(new TcxExportNotice(TcxExportNoticeKind.Saved, session, file));
            }
        }

        private void Notify(TcxExportNotice notice)
        {
            if (!_disposed)
            {
                _notices.Emit(notice);
            }
        }

        private void SetBusy(long id, bool busy)
        {
            var current = (long[])_busy.Value;
            var index = Array.IndexOf(current, id);
            if (busy == (index >= 0))
            {
                return;
            }

            long[] next;
            if (busy)
            {
                next = new long[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = id;
            }
            else if (current.Length == 1)
            {
                next = NoIds;
            }
            else
            {
                next = new long[current.Length - 1];
                Array.Copy(current, 0, next, 0, index);
                Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            }

            _busy.Value = next;
        }

        private string FolderLabel(ExportLocation location)
        {
            return ExportFolderLabel.For(location, _texts.Value);
        }

        // La carpeta real → su etiqueta; la carpeta del usuario → %USERPROFILE% (ExportLog.Scrub ordena del más largo al más corto).
        private IReadOnlyList<KeyValuePair<string, string>> Replacements()
        {
            var list = new List<KeyValuePair<string, string>>(2);
            var location = _exporter.Location;
            if (location != null && !string.IsNullOrEmpty(location.Directory))
            {
                list.Add(new KeyValuePair<string, string>(location.Directory, FolderLabel(location)));
            }

            if (_userProfile.Length > 0)
            {
                list.Add(new KeyValuePair<string, string>(_userProfile, ExportLog.UserProfileLabel));
            }

            return list;
        }
    }
}
