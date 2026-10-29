using System;
using System.Collections.Generic;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Text;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // El estado de prueba de la spec §0.4 para el motor, con FakeScheduler (el alcance es implícito: Advance dispara todo en
    // orden). La hora de pared del instante 0 es 2026-10-28T13:00:00.000Z; el reloj monótono 0 es el instante −0,5 (la
    // primera lectura de R-4, que manda el remo de prueba al construirse). La sesión se crea en el instante 0 (StartFree /
    // StartWithRoutine después de At(0)) y su segundo k cae en el instante k.
    internal sealed class WorkoutHarness : IDisposable
    {
        public static readonly DateTimeOffset SessionStart = new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero);

        // sessionsFor: una base que necesita el programador del arnés (ScriptedSessionRepository, con sus demoras).
        public WorkoutHarness(Func<int, RowingData>? reading = null, ISessionRepository? sessions = null, bool startRower = true,
                              Func<FakeScheduler, ISessionRepository>? sessionsFor = null)
        {
            Logger = new RecordingLogger();
            Scheduler = new FakeScheduler(SessionStart.AddSeconds(-0.5));
            Lifecycle = new FakeAppLifecycle(Logger);
            Rower = new ScriptedRower(Scheduler, reading ?? RowerScripts.R4, Logger);
            HeartRate = new ScriptedHeartRateService(Logger);
            if (sessions == null && sessionsFor != null)
            {
                sessions = sessionsFor(Scheduler);
            }

            Memory = sessions as InMemorySessionRepository;
            Sessions = sessions ?? (Memory = new InMemorySessionRepository());
            Engine = new WorkoutEngine(Rower, HeartRate, Sessions, Scheduler, Lifecycle, Logger);
            Notices = new StateRecorder<WorkoutNotice>(Engine.Notices);
            if (startRower)
            {
                Rower.Start();
            }
        }

        public RecordingLogger Logger { get; }

        public FakeScheduler Scheduler { get; }

        public FakeAppLifecycle Lifecycle { get; }

        public ScriptedRower Rower { get; }

        public ScriptedHeartRateService HeartRate { get; }

        public ISessionRepository Sessions { get; }

        // La base en memoria (si la prueba no pasó otra).
        public InMemorySessionRepository? Memory { get; private set; }

        public WorkoutEngine Engine { get; }

        public StateRecorder<WorkoutNotice> Notices { get; }

        public WorkoutSnapshot State => Engine.State.Value;

        // El instante actual de la spec.
        public double Now => Scheduler.MonotonicSeconds - 0.5;

        // Avanza hasta el instante de la spec (en ticks exactos).
        public void At(double instant)
        {
            var target = (long)DartMath.Round((instant + 0.5) * TimeSpan.TicksPerSecond);
            var delta = target - Scheduler.Elapsed.Ticks;
            if (delta < 0)
            {
                throw new InvalidOperationException("WorkoutHarness: el tiempo no retrocede (" + instant + ").");
            }

            Scheduler.Advance(TimeSpan.FromTicks(delta));
        }

        // Inicio libre en el instante 0.
        public WorkoutHarness Free()
        {
            At(0);
            Engine.StartFree();
            return this;
        }

        // Inicio con rutina en el instante 0.
        public WorkoutHarness Routine(Routine template)
        {
            At(0);
            var outcome = Engine.StartWithRoutine(template);
            if (outcome != StartOutcome.Started)
            {
                throw new InvalidOperationException("WorkoutHarness: la rutina no arrancó (" + outcome + ").");
            }

            return this;
        }

        // Con ids, como si estuvieran guardadas (la sesión copia el id de la plantilla).
        public static Routine Saved(Routine template, long id)
        {
            return template with { Id = id };
        }

        public IReadOnlyList<DataPoint> Points()
        {
            if (Memory == null)
            {
                throw new InvalidOperationException("WorkoutHarness: la base no es la de memoria.");
            }

            return Memory.AllPoints;
        }

        public WorkoutSession? Session(long id = 1)
        {
            return Memory?.Session(id);
        }

        public void Dispose()
        {
            Notices.Dispose();
            Engine.Dispose();
            Rower.Dispose();
        }

        // Los puntos como (tiempo del monitor, distancia, kcal, índice, tipo), la forma del Anexo A del diseño.
        public static string Describe(IReadOnlyList<DataPoint> points)
        {
            var parts = new List<string>();
            foreach (var p in points)
            {
                parts.Add(Fmt.Integer(p.ElapsedSeconds) + "/" + Fmt.Integer(p.DistanceMeters) + "/" + Fmt.Integer(p.Calories) + "/"
                    + (p.StepIndex.HasValue ? Fmt.Integer(p.StepIndex.Value) : "-") + "/" + (p.StepTypeName ?? "-"));
            }

            return string.Join(" ", parts);
        }
    }
}
