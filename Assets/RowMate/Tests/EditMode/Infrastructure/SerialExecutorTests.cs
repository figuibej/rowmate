using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.Infrastructure.Threading;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // SET: el hilo de la base (02-tech-design §2.6; ADR 005). Corre dentro de TestMainThreadContext.Run, que hace de
    // UnitySynchronizationContext: la continuación de cada await vuelve al hilo del test.
    [TestFixture]
    public sealed class SerialExecutorTests
    {
        // AC-HIS-17-06 (regla): el trabajo corre en otro hilo y la continuación vuelve al que llamó.
        [Test, Category("AC-HIS-17-06")]
        public void WorkRunsOffTheCallingThread()
        {
            var logger = new RecordingLogger();
            TestMainThreadContext.Run(async () =>
            {
                var caller = Environment.CurrentManagedThreadId;
                using (var executor = new SerialExecutor("RowMate.Db.Test", logger))
                {
                    var worker = await executor.Run(() => Environment.CurrentManagedThreadId);
                    Assert.That(worker, Is.EqualTo(executor.ThreadId));
                    Assert.That(worker, Is.Not.EqualTo(caller), "fuera del hilo principal");
                    Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(caller), "la continuación vuelve al hilo que llamó");
                    await executor.Run(() => { });
                    Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(caller));
                }
            });
            Assert.That(logger.Lines, Is.Empty);
        }

        // FIFO: el orden de llegada es el de ejecución (cargar y después crear nunca se cruzan).
        [Test, Category("AC-HIS-17-06")]
        public void OrderIsFirstInFirstOut()
        {
            var logger = new RecordingLogger();
            var seen = new List<int>();
            TestMainThreadContext.Run(async () =>
            {
                using (var executor = new SerialExecutor("RowMate.Db.Test", logger))
                {
                    var tasks = new List<Task>();
                    for (var i = 0; i < 200; i++)
                    {
                        var n = i;
                        tasks.Add(executor.Run(() =>
                        {
                            if (n == 0)
                            {
                                Thread.Sleep(20);
                            }

                            seen.Add(n);
                        }));
                    }

                    await Task.WhenAll(tasks);
                }
            });
            Assert.That(seen.Count, Is.EqualTo(200));
            for (var i = 0; i < seen.Count; i++)
            {
                Assert.That(seen[i], Is.EqualTo(i));
            }
        }

        // La excepción del trabajo viaja en la tarea (y la continuación vuelve igual al hilo que llamó).
        [Test, Category("AC-HIS-17-06")]
        public void ExceptionTravelsInTheTask()
        {
            var logger = new RecordingLogger();
            TestMainThreadContext.Run(async () =>
            {
                var caller = Environment.CurrentManagedThreadId;
                using (var executor = new SerialExecutor("RowMate.Db.Test", logger))
                {
                    try
                    {
                        await executor.Run<int>(() => throw new InvalidOperationException("falla de la base"));
                        Assert.Fail("tenía que fallar");
                    }
                    catch (InvalidOperationException error)
                    {
                        Assert.That(error.Message, Is.EqualTo("falla de la base"));
                    }

                    Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(caller));
                    Assert.That(await executor.Run(() => 7), Is.EqualTo(7), "el hilo sigue vivo después de una falla");
                }
            });
        }

        // Dispose termina lo encolado (un guardado en vuelo se completa) y no acepta más.
        [Test, Category("AC-HIS-17-06")]
        public void DisposeFinishesWhatIsQueued()
        {
            var logger = new RecordingLogger();
            var executor = new SerialExecutor("RowMate.Db.Test", logger);
            var slow = executor.Run(() =>
            {
                Thread.Sleep(50);
                return 1;
            });
            var next = executor.Run(() => 2);
            executor.Dispose();
            Assert.That(slow.IsCompleted && next.IsCompleted, Is.True, "Dispose espera lo encolado");
            Assert.That(slow.Result + next.Result, Is.EqualTo(3));
            Assert.That(logger.Lines, Is.Empty);
        }

        // QA-G3-09: un Run después de Dispose devuelve una tarea fallada (no lanza en línea); Dispose es idempotente.
        [Test, Category("AC-HIS-17-06")]
        public void RunAfterDisposeFailsTheTask()
        {
            var executor = new SerialExecutor("RowMate.Db.Test", new RecordingLogger());
            executor.Dispose();
            Task<int> task = null!;
            Assert.DoesNotThrow(() => task = executor.Run(() => 1));
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception!.InnerException, Is.InstanceOf<ObjectDisposedException>());
            Assert.DoesNotThrow(() => executor.Dispose());
        }

        // QA-G3-09: se suscribe a la descarga del dominio al crearse, se desuscribe en Dispose, y la descarga lo dispone.
        [Test, Category("AC-HIS-17-06")]
        public void DisposeUnsubscribesTheUnloadHook()
        {
            var hook = new CountingHook();
            var executor = new SerialExecutor("RowMate.Db.Test", new RecordingLogger(), hook);
            Assert.That(hook.Subscribed, Is.EqualTo(1));
            Assert.That(hook.Unsubscribed, Is.EqualTo(0));
            executor.Dispose();
            executor.Dispose();
            Assert.That(hook.Unsubscribed, Is.EqualTo(1), "una vez, aunque se disponga dos veces");
            var unloaded = new CountingHook();
            var other = new SerialExecutor("RowMate.Db.Test", new RecordingLogger(), unloaded);
            unloaded.Raise();
            Assert.That(other.Run(() => 1).IsFaulted, Is.True, "la descarga del dominio lo dispuso");
            Assert.That(unloaded.Unsubscribed, Is.EqualTo(1));
        }

        private sealed class CountingHook : IUnloadHook
        {
            private EventHandler? _handler;

            public int Subscribed { get; private set; }

            public int Unsubscribed { get; private set; }

            public void Subscribe(EventHandler handler)
            {
                Subscribed++;
                _handler = handler;
            }

            public void Unsubscribe(EventHandler handler)
            {
                Unsubscribed++;
                if (_handler == handler)
                {
                    _handler = null;
                }
            }

            public void Raise()
            {
                _handler?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
