using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Presentation.Controls;
using RowMate.Presentation.History;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // TAT · El aviso con acción de "Exportar TCX" (12-exportar-tcx/02-tech-design §5.2; DT12-13, DT12-14; AC-STR-09-19 a -22): dos
    // renglones, 6 s, "Abrir carpeta" (registrada por el FakeFileExporter: nunca el Explorador), "Reintentar" tras un error, la
    // prioridad de un aviso común y "reducir movimiento". Sin rutas de la PC ni el usuario en lo que se ve ni en el registro.
    public sealed class ToastActionTests : ExportUiTestBase
    {
        [UnityTest, Category("AC-STR-09-19")]
        public IEnumerator SuccessTwoLinesSixSecondsWithAction()
        {
            yield return OpenSession(0);
            yield return ExportNow();
            var layer = H.Shell.Toasts;
            var toast = layer.CurrentAction!;
            Assert.That(toast, Is.Not.Null, "el aviso de éxito");
            Assert.That(layer.Text.text, Is.EqualTo(SavedTitle));
            Assert.That(layer.Detail.text, Is.EqualTo(FileName), "el nombre en el segundo renglón");
            Assert.That(layer.Detail.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(layer.Action.Label.text, Is.EqualTo("Abrir carpeta"));
            Assert.That(layer.Indicator.Current, Is.EqualTo(LinkIndicator.Mode.Check), "el check (D12-04)");
            Assert.That(layer.Detail.worldBound.yMin, Is.GreaterThanOrEqualTo(layer.Text.worldBound.yMax - 0.5f), "debajo del título");
            Assert.That(layer.Action.worldBound.xMin, Is.GreaterThanOrEqualTo(layer.Detail.worldBound.xMax - 0.5f), "la acción a la derecha");
            Assert.That(layer.Toast.worldBound.width, Is.InRange(344f - 0.5f, 560f + 0.5f), "clamp 344…560 en el escritorio");
            H.Advance(5.5);
            yield return Frames();
            Assert.That(layer.IsVisible, Is.True, "sigue a los 5,7 s");
            H.Advance(0.5);
            yield return Settle();
            Assert.That(layer.IsVisible, Is.False, "se fue a los 6 s");
        }

        [UnityTest, Category("AC-STR-09-19")]
        public IEnumerator NeverFullPathOrUserName()
        {
            yield return OpenSession(0);
            yield return ExportNow();
            var layer = H.Shell.Toasts;
            var folder = Exporter.Location.Directory;
            var user = Environment.UserName;
            foreach (var text in new[] { layer.Text.text, layer.Detail.text, layer.Action.Label.text })
            {
                Assert.That(text, Does.Not.Contain(":\\").And.Not.Contain(folder));
                Assert.That(text, Does.Not.Contain(Path.GetTempPath().TrimEnd('\\')));
                if (user.Length > 2)
                {
                    Assert.That(text, Does.Not.Contain(user));
                }
            }

            foreach (var line in H.Logger.Lines)
            {
                Assert.That(line, Does.Not.Contain(folder), "el registro tampoco lleva la ruta");
            }

            Assert.That(H.Logger.Lines, Has.Some.Contains("TCX guardado: " + FileName + " en Documentos\\RowMate\\TCX"));
        }

        [UnityTest, Category("AC-STR-09-19")]
        public IEnumerator WrapsNeverEllipsis()
        {
            yield return OpenSession(0, DeviceProfile.TV, o => o.TextScale = 1.3);
            yield return ExportNow();
            var layer = H.Shell.Toasts;
            Assert.That(layer.Toast.worldBound.xMin, Is.EqualTo(16).Within(0.6f), "de 16 a 16 en el teléfono");
            Assert.That(H.Root.worldBound.xMax - layer.Toast.worldBound.xMax, Is.EqualTo(16).Within(0.6f));
            foreach (var label in new[] { layer.Text, layer.Detail })
            {
                Assert.That(label.resolvedStyle.whiteSpace, Is.EqualTo(WhiteSpace.Normal), label.name + " ajusta");
                Assert.That(label.resolvedStyle.textOverflow, Is.Not.EqualTo(TextOverflow.Ellipsis), label.name + " sin …");
                var natural = label.MeasureTextSize(label.text, label.worldBound.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Assert.That(label.worldBound.height, Is.GreaterThanOrEqualTo(natural.y - 0.5f), label.name + " entero");
            }

            Assert.That(layer.Toast.worldBound.yMax, Is.LessThanOrEqualTo(H.Root.worldBound.yMax), "a la vista");
        }

        [UnityTest, Category("AC-STR-09-20")]
        public IEnumerator OpenFolderRevealsAndHides()
        {
            yield return OpenSession(0);
            yield return ExportNow();
            yield return Tap("toast-action");
            Assert.That(Exporter.Reveals.Count, Is.EqualTo(1), "Abrir carpeta (registrado: nunca el Explorador)");
            Assert.That(Exporter.Reveals[0].FileName, Is.EqualTo(FileName));
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "el aviso se cierra");
        }

        [UnityTest, Category("AC-STR-09-21")]
        public IEnumerator RetryExportsAgain()
        {
            yield return OpenSession(0);
            Exporter.FailNext(new IOException("disco lleno en " + Exporter.Location.Directory));
            yield return ExportNow();
            var layer = H.Shell.Toasts;
            Assert.That(layer.Text.text, Is.EqualTo("No se pudo exportar la sesión"));
            Assert.That(layer.Detail.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "sin detalle");
            Assert.That(layer.Indicator.Current, Is.EqualTo(LinkIndicator.Mode.None), "sin indicador (D12-07)");
            Assert.That(layer.Action.Label.text, Is.EqualTo("Reintentar"));
            Assert.That(Exporter.Files.Count, Is.EqualTo(0));
            Assert.That(H.Logger.Lines, Has.Some.Contains("no se pudo exportar: IOException: disco lleno en Documentos\\RowMate\\TCX"),
                "el detalle técnico al registro, con la ruta abreviada");
            H.Tap("toast-action");
            yield return WaitUntil(() => Exporter.Requests.Count == 2);
            yield return Frames();
            yield return FinishBusy();
            Assert.That(Exporter.Files.Count, Is.EqualTo(1), "Reintentar vuelve a exportar");
            Assert.That(layer.CurrentAction?.Title, Is.EqualTo(SavedTitle));
        }

        [UnityTest, Category("AC-STR-09-21")]
        public IEnumerator ButtonBackToIcon()
        {
            yield return OpenSession(0);
            Exporter.FailNext(new IOException("sin acceso"));
            yield return ExportNow();
            Assert.That(Button.IsBusy, Is.False);
            Assert.That(Button.GlyphView.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "el botón vuelve a su ícono");
            Assert.That(Button.Button.enabledSelf, Is.True);
        }

        [UnityTest, Category("AC-STR-09-22")]
        public IEnumerator RowerHrmAndCommonReplaceExportNotice()
        {
            yield return OpenSession(0);
            yield return ExportNow();
            var layer = H.Shell.Toasts;
            Assert.That(layer.VisibleSlot, Is.EqualTo(ToastSlot.Common), "prioridad de un aviso común");
            layer.Show("Aviso común");
            yield return Settle();
            Assert.That(layer.CurrentAction, Is.Null, "un aviso común lo reemplaza");
            Assert.That(layer.CurrentText, Is.EqualTo("Aviso común"));
            Assert.That(layer.Action.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            yield return ExportNow(1);
            Assert.That(layer.CurrentAction, Is.Not.Null);
            Assert.That(layer.ShowHrm(new HrmToast(HrmNoticeKind.Connected, "Pulsómetro conectado", TimeSpan.FromSeconds(2))), Is.True, "el del pulsómetro lo reemplaza");
            yield return Settle();
            Assert.That(layer.CurrentAction, Is.Null);
            Assert.That(layer.CurrentHrm, Is.Not.Null);
            yield return ExportNow(2);
            layer.ShowLink(new LinkToast(LinkNoticeKind.MonitorStopped, "El monitor dejó de enviar datos", TimeSpan.FromSeconds(4)));
            yield return Settle();
            Assert.That(layer.CurrentAction, Is.Null, "el del remo lo reemplaza");
            Assert.That(layer.CurrentLink, Is.Not.Null);
        }

        [UnityTest, Category("AC-STR-09-22")]
        public IEnumerator StickyReturnsAfterSixSeconds()
        {
            yield return OpenSession(0);
            var layer = H.Shell.Toasts;
            var sticky = new LinkToast(LinkNoticeKind.Reconnecting, "Reconectando…", null);
            layer.ShowLink(sticky);
            yield return Settle();
            yield return ExportNow();
            Assert.That(layer.CurrentAction, Is.Not.Null, "el aviso de la exportación tapa al fijo");
            Assert.That(layer.StickyLink, Is.SameAs(sticky));
            H.Advance(5.5);
            yield return Frames();
            Assert.That(layer.CurrentAction, Is.Not.Null);
            H.Advance(0.6);
            yield return Settle();
            Assert.That(layer.CurrentLink, Is.SameAs(sticky), "a los 6 s vuelve el fijo");
            Assert.That(layer.Action.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(layer.Detail.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest, Category("AC-STR-09-22")]
        public IEnumerator ReducedMotionNoMovement()
        {
            yield return OpenSession(0, DeviceProfile.W1280, o => o.ReduceMotion = true);
            H.Tap(ExportTcxButton.ElementName);
            yield return WaitUntil(() => Exporter.Requests.Count == 1);
            yield return Frames();
            H.Advance(0.3);
            yield return WaitUntil(() => H.Shell.Toasts.IsVisible);
            var toast = H.Shell.Toasts.Toast;
            for (var i = 0; i < 6; i++)
            {
                H.Step();
                yield return null;
                Assert.That(toast.resolvedStyle.translate.y, Is.EqualTo(0).Within(0.01f), "entra sin moverse");
            }

            yield return Settle();
            H.Advance(6.05);
            for (var i = 0; i < 6; i++)
            {
                H.Step();
                yield return null;
                Assert.That(toast.resolvedStyle.translate.y, Is.EqualTo(0).Within(0.01f), "sale sin moverse");
            }
        }
    }
}
