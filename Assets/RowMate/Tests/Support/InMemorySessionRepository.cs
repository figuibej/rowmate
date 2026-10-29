using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Base de sesiones en memoria (02-tech-design §8.1 de la It. 4a): cumple el contrato de ISessionRepository en línea (cada
    // tarea vuelve ya terminada, en el orden en que se llama) y anota cada operación. Para las pruebas del motor y de los
    // stores que no necesitan SQLite; la persistencia real la prueba SqliteSessionRepositoryTests.
    public sealed class InMemorySessionRepository : ISessionRepository
    {
        private readonly List<WorkoutSession> _sessions = new List<WorkoutSession>();
        private readonly List<DataPoint> _points = new List<DataPoint>();
        private readonly List<string> _operations = new List<string>();
        private long _nextId = 1;
        private long _nextPointId = 1;

        // "create", "points:n", "after-create:n", "finish:n" (n = puntos), en orden.
        public IReadOnlyList<string> Operations => _operations.ToArray();

        public IReadOnlyList<WorkoutSession> Sessions => _sessions.ToArray();

        public IReadOnlyList<DataPoint> AllPoints => _points.ToArray();

        public int WaitCalls { get; private set; }

        public WorkoutSession? Session(long id)
        {
            for (var i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].Id == id)
                {
                    return _sessions[i];
                }
            }

            return null;
        }

        public IReadOnlyList<DataPoint> PointsOf(long sessionId)
        {
            var result = new List<DataPoint>();
            for (var i = 0; i < _points.Count; i++)
            {
                if (_points[i].SessionId == sessionId)
                {
                    result.Add(_points[i]);
                }
            }

            return result;
        }

        public Task<long> CreateAsync(WorkoutSession session)
        {
            return Task.FromResult(Create(session));
        }

        public Task AddPointsAsync(IReadOnlyList<DataPoint> points)
        {
            AddPoints(points, null, "points");
            return Task.CompletedTask;
        }

        public Task AddPointsAfterCreateAsync(Task<long> creation, IReadOnlyList<DataPoint> points)
        {
            if (creation == null)
            {
                throw new ArgumentNullException(nameof(creation));
            }

            if (!creation.IsCompleted)
            {
                return Task.FromException(new InvalidOperationException("InMemorySessionRepository: el alta todavía no terminó (FIFO)."));
            }

            if (creation.IsFaulted || creation.IsCanceled)
            {
                _operations.Add("after-create:0");
                return Task.CompletedTask;
            }

            AddPoints(points, creation.Result, "after-create");
            return Task.CompletedTask;
        }

        public Task FinishAsync(IReadOnlyList<DataPoint> points, WorkoutSession closed)
        {
            try
            {
                Finish(points, closed);
                return Task.CompletedTask;
            }
            catch (Exception error)
            {
                return Task.FromException(error);
            }
        }

        public Task<WorkoutSession?> GetByIdAsync(long id)
        {
            return Task.FromResult(Session(id));
        }

        public Task<IReadOnlyList<DataPoint>> GetPointsAsync(long sessionId)
        {
            var points = new List<DataPoint>(PointsOf(sessionId));
            points.Sort((a, b) => a.ElapsedSeconds != b.ElapsedSeconds ? a.ElapsedSeconds.CompareTo(b.ElapsedSeconds) : Nullable.Compare(a.Id, b.Id));
            return Task.FromResult<IReadOnlyList<DataPoint>>(points);
        }

        public Task<IReadOnlyList<SessionListing>> ListAsync()
        {
            var list = new List<SessionListing>();
            for (var i = _sessions.Count - 1; i >= 0; i--)
            {
                var s = _sessions[i];
                list.Add(new SessionListing(s.Id!.Value, s.StartedAt, s.FinishedAt, s.RoutineName, "app", PointsOf(s.Id.Value).Count));
            }

            return Task.FromResult<IReadOnlyList<SessionListing>>(list);
        }

        public bool WaitForPendingWork(TimeSpan timeout)
        {
            WaitCalls++;
            return true;
        }

        // Las operaciones en sí, para ScriptedSessionRepository (que las ejecuta en su orden, con demoras y fallas).
        internal long Create(WorkoutSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var id = _nextId++;
            _sessions.Add(new WorkoutSession(session.StartedAt) { Id = id, RoutineId = session.RoutineId, RoutineName = session.RoutineName });
            _operations.Add("create");
            return id;
        }

        internal void AddPoints(IReadOnlyList<DataPoint> points, long? sessionId, string operation)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                _points.Add(point with { Id = _nextPointId++, SessionId = sessionId ?? point.SessionId });
            }

            _operations.Add(operation + ":" + points.Count);
        }

        internal void Finish(IReadOnlyList<DataPoint> points, WorkoutSession closed)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (closed == null)
            {
                throw new ArgumentNullException(nameof(closed));
            }

            var index = -1;
            for (var i = 0; i < _sessions.Count; i++)
            {
                if (closed.Id.HasValue && _sessions[i].Id == closed.Id)
                {
                    index = i;
                }
            }

            if (index < 0)
            {
                throw new SessionNotFoundException(closed.Id);
            }

            for (var i = 0; i < points.Count; i++)
            {
                _points.Add(points[i] with { Id = _nextPointId++, SessionId = closed.Id!.Value });
            }

            _sessions[index] = closed with { StravaActivityId = null };
            _operations.Add("finish:" + points.Count);
        }
    }
}
