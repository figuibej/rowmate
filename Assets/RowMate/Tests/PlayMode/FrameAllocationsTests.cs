using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // FAT · El control positivo de FrameAllocations (05-series-parciales/02-tech-design §3.6; QA-G5T-03; DT5-18): en cuadros reales, la
    // medida ve lo que asigna un generateVisualContent (un byte[64] por repintado: una asignación por dibujo, ≥ 30 en 30 cuadros) y lo
    // que asigna el paso del cuadro, y no ve nada del mismo elemento sin asignar. Es la medida que usa HBT.SeriesNoAllocationsPerFrame
    // con los paneles (por SeriesPanelView.DrawObserver).
    public sealed class FrameAllocationsTests : UiTestBase
    {
        private sealed class Allocating : VisualElement
        {
            public bool Allocate;
            public int Draws;
            public byte[]? Last;
            public FrameAllocations? Observer;

            public Allocating()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.width = 40;
                style.height = 40;
                generateVisualContent += context =>
                {
                    Observer?.BeginDraw();
                    Draws++;
                    if (Allocate)
                    {
                        Last = new byte[64];
                    }

                    var painter = context.painter2D;
                    painter.fillColor = Color.gray;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(0, 0));
                    painter.LineTo(new Vector2(40, 0));
                    painter.LineTo(new Vector2(40, 40));
                    painter.ClosePath();
                    painter.Fill();
                    Observer?.EndDraw();
                };
            }
        }

        private static IEnumerator Window(FrameAllocations allocations, Allocating element, int frames, bool allocateInStep)
        {
            element.Observer = allocations;
            for (var i = 0; i < frames; i++)
            {
                element.MarkDirtyRepaint();
                allocations.Step(() =>
                {
                    if (allocateInStep)
                    {
                        element.Last = new byte[64];
                    }
                });
                yield return null;
                allocations.Sample();
            }

            element.Observer = null;
            allocations.Stop();
        }

        [UnityTest, Category("AC-WKT-18-21")]
        public IEnumerator ThePositiveControlIsSeenInRealFrames()
        {
            Create(new UiHostOptions());
            yield return Frames(3);
            var element = new Allocating();
            H.Root.Add(element);
            yield return Frames(5);

            var clean = FrameAllocations.Start();
            yield return Window(clean, element, 30, false);
            element.Allocate = true;
            var drawing = FrameAllocations.Start();
            yield return Window(drawing, element, 30, false);
            element.Allocate = false;
            var stepping = FrameAllocations.Start();
            yield return Window(stepping, element, 30, true);
            Debug.Log("[FAT] sin asignar: " + clean + "; un byte[64] por repintado: " + drawing + "; uno por paso: " + stepping);
            Assert.That(clean.Draws, Is.GreaterThanOrEqualTo(30), "repinta en cada cuadro real");
            Assert.That(clean.Total, Is.EqualTo(0), "sin asignar, nada: " + clean);
            Assert.That(drawing.DrawAllocations, Is.GreaterThanOrEqualTo(30), "el control positivo en el dibujo se ve: " + drawing);
            Assert.That(stepping.StepAllocations, Is.GreaterThanOrEqualTo(30), "y en el paso: " + stepping);
            clean.Dispose();
            drawing.Dispose();
            stepping.Dispose();
        }

        // It. 16 (#78, #99; 16-tests-intermitentes/02-tech-design): con OTRO hilo asignando sin parar (como el de los finalizadores o el
        // ThreadPool al empezar una corrida grande), la medida del hilo principal no cuenta nada ajeno y sigue viendo lo propio, exacto:
        // GcAllocations.During y FrameAllocations (paso y dibujo, en cuadros reales). Con el Recorder de antes (FilterToCurrentThread)
        // la misma sonda contaba una asignación ajena en 177 de 1000 medidas de 200 µs: se deja en el registro como testigo, sin afirmar
        // (es un comportamiento de Unity, no de RowMate).
        [UnityTest, Category("AC-WKT-18-21")]
        public IEnumerator OtherThreadsAllocatingAreNotCounted()
        {
            Create(new UiHostOptions());
            yield return Frames(3);
            var element = new Allocating();
            H.Root.Add(element);
            yield return Frames(5);

            var flag = new StopFlag();
            var thread = new Thread(() =>
            {
                while (!flag.Stop)
                {
                    _foreign = new byte[32];
                }
            }) { IsBackground = true, Name = "RowMate.Test.ForeignAllocations" };
            thread.Start();
            try
            {
                var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.one, Vector3.up } };
                var clean = 0;
                var cleanWindows = 0;
                var legacy = 0;
                var own = 0;
                var unityApi = 0;
                for (var i = 0; i < 1000; i++)
                {
                    var n = GcAllocations.During(() => Spin(200));
                    clean += n;
                    cleanWindows += n > 0 ? 1 : 0;
                    legacy += LegacyRecorderDuring(() => Spin(200));
                }

                for (var i = 0; i < 300; i++)
                {
                    own += GcAllocations.During(() =>
                    {
                        Spin(200);
                        element.Last = new byte[64];
                    });
                    unityApi += GcAllocations.During(() => _foreign = mesh.vertices) > 0 ? 1 : 0;
                }

                UnityEngine.Object.Destroy(mesh);
                var quiet = FrameAllocations.Start();
                yield return Window(quiet, element, 60, false);
                element.Allocate = true;
                var drawing = FrameAllocations.Start();
                yield return Window(drawing, element, 60, true);
                element.Allocate = false;
                Debug.Log("[FAT] con otro hilo asignando: " + clean + " ajenas en 1000 medidas vacías (" + cleanWindows + " con alguna); el Recorder de antes, "
                          + legacy + "; un byte[64] por medida, " + own + " en 300; mesh.vertices, " + unityApi + " de 300; cuadros sin asignar: " + quiet
                          + "; uno por dibujo y uno por paso: " + drawing);
                Assert.That(clean, Is.EqualTo(0), "nada de otro hilo en 1000 medidas vacías");
                Assert.That(own, Is.EqualTo(300), "el control positivo, exacto: uno por medida");
                Assert.That(unityApi, Is.EqualTo(300), "lo que asigna una API de Unity en este hilo se ve");
                Assert.That(quiet.Total, Is.EqualTo(0), "cuadros sin asignar: " + quiet);
                Assert.That(quiet.Draws, Is.GreaterThanOrEqualTo(60), "repinta en cada cuadro real");
                Assert.That(drawing.DrawAllocations, Is.EqualTo(drawing.Draws), "uno por dibujo, exacto: " + drawing);
                Assert.That(drawing.StepAllocations, Is.EqualTo(60), "uno por paso, exacto: " + drawing);
                quiet.Dispose();
                drawing.Dispose();
            }
            finally
            {
                flag.Stop = true;
                thread.Join();
            }
        }

        private static volatile object? _foreign;

        private sealed class StopFlag
        {
            public volatile bool Stop;
        }

        private static void Spin(double microseconds)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            var ticks = (long)(microseconds * System.Diagnostics.Stopwatch.Frequency / 1e6);
            while (System.Diagnostics.Stopwatch.GetTimestamp() - start < ticks)
            {
            }
        }

        // La medida de antes de la It. 16 (y de AllocatingGCMemoryConstraint): el Recorder de UnityEngine.Profiling con FilterToCurrentThread.
        private static int LegacyRecorderDuring(Action action)
        {
            var recorder = UnityEngine.Profiling.Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                action();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }

            return recorder.sampleBlockCount;
        }
    }
}
