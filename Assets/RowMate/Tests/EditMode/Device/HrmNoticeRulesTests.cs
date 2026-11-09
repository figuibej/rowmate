using System;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.Device
{
    // Los avisos del pulsómetro (It. 11, DT11-12, DT11-13, QA-G11-03): cuándo salen, ante quién ceden y sus textos.
    [TestFixture]
    public sealed class HrmNoticeRulesTests
    {
        // AC-HRM-08-03 / AC-HRM-07-05: Lost solo con el entrenamiento a la vista; Connected en cualquier pestaña (CAP-12).
        [Test, Category("AC-HRM-08-03"), Category("AC-HRM-07-05")]
        public void LostOnlyDuringWorkout()
        {
            Assert.That(HrmNoticeRules.ShouldShow(HrmNoticeKind.Lost, true), Is.True);
            Assert.That(HrmNoticeRules.ShouldShow(HrmNoticeKind.Lost, false), Is.False);
            Assert.That(HrmNoticeRules.ShouldShow(HrmNoticeKind.Connected, true), Is.True);
            Assert.That(HrmNoticeRules.ShouldShow(HrmNoticeKind.Connected, false), Is.True, "también en Dispositivo");
        }

        // QA-G11-03 (AC-HRM-08-03): el aviso del pulsómetro cede SOLO ante uno del remo (a la vista o fijo pendiente); reemplaza el
        // suyo (Connected pisa a Lost) y uno común.
        [TestCase(ToastSlot.None, false, true)]
        [TestCase(ToastSlot.Common, false, true)]
        [TestCase(ToastSlot.Hrm, false, true)]
        [TestCase(ToastSlot.Rower, false, false)]
        [TestCase(ToastSlot.None, true, false)]
        [TestCase(ToastSlot.Common, true, false)]
        [TestCase(ToastSlot.Hrm, true, false)]
        [TestCase(ToastSlot.Rower, true, false)]
        [Category("AC-HRM-08-03")]
        public void AdmitYieldsOnlyToRower(ToastSlot visible, bool rowerSticky, bool admitted)
        {
            Assert.That(HrmNoticeRules.Admit(visible, rowerSticky), Is.EqualTo(admitted));
        }

        // AC-HRM-07-05 / AC-HRM-08-03 (spec §4, DV-108, DV-109): textos y duraciones, en los dos idiomas.
        [Test, Category("AC-HRM-07-05"), Category("AC-HRM-08-03")]
        public void TextsAndDurations()
        {
            var connected = HrmNoticeText.Toast(new HrmNotice(HrmNoticeKind.Connected, "Banda de prueba"), LocalizationFixture.Es);
            Assert.That(connected.Text, Is.EqualTo("Pulsómetro conectado"));
            Assert.That(connected.Duration, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(connected.Kind, Is.EqualTo(HrmNoticeKind.Connected));
            var lost = HrmNoticeText.Toast(new HrmNotice(HrmNoticeKind.Lost, null), LocalizationFixture.En);
            Assert.That(lost.Text, Is.EqualTo("Heart rate sensor lost, reconnecting…"));
            Assert.That(lost.Duration, Is.EqualTo(TimeSpan.FromSeconds(4)));
            Assert.That(HrmNoticeText.Toast(new HrmNotice(HrmNoticeKind.Connected, null), LocalizationFixture.En).Text,
                Is.EqualTo("Heart rate sensor connected"));
        }
    }
}
