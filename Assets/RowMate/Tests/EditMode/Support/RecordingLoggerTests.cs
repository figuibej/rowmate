using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Support
{
    // RLG: RecordingLogger seguro entre hilos (QA-G3-09): el hilo de la base también registra.
    [TestFixture]
    public sealed class RecordingLoggerTests
    {
        [Test, Category("QA-G3-09")]
        public void ConcurrentLogsAreAllKept()
        {
            var logger = new RecordingLogger();
            var threads = new List<Thread>();
            var failures = 0;
            for (var t = 0; t < 4; t++)
            {
                var id = t;
                threads.Add(new Thread(() =>
                {
                    try
                    {
                        for (var i = 0; i < 1000; i++)
                        {
                            logger.Log(LogLevel.Info, "T" + id, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            if (i % 100 == 0)
                            {
                                Assert.That(logger.Lines.Count, Is.GreaterThan(0), "leer mientras otros escriben");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref failures);
                    }
                }));
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.That(failures, Is.EqualTo(0));
            var lines = logger.Lines;
            Assert.That(lines.Count, Is.EqualTo(4000));
            Assert.That(logger.Errors.Count, Is.EqualTo(4000));
            Assert.That(logger.Levels.Count, Is.EqualTo(4000));
            var next = new int[4];
            foreach (var line in lines)
            {
                var thread = line[2] - '0';
                Assert.That(line, Is.EqualTo("[T" + thread + "] Info: " + next[thread].ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    "cada hilo en su orden");
                next[thread]++;
            }

            logger.Clear();
            Assert.That(logger.Lines, Is.Empty);
        }
    }
}
