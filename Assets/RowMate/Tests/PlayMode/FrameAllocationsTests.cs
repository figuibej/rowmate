using System.Collections;
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
    }
}
