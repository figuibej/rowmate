using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Platform.Locale;
using UnityEngine;

namespace RowMate.Tests.UnityEditMode
{
    // Lista de idiomas del sistema real (AC-APP-09-06, QA-G2-17): en el Editor de Windows, GetUserPreferredUILanguages da una
    // lista BCP 47 no vacía. En otras plataformas se ignora con el motivo; en el teléfono lo mira QA [Device-And].
    public sealed class SystemLocaleProviderTests
    {
        [Test, Category("AC-APP-09-06")]
        public void WindowsListIsBcp47AndNotEmpty()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("solo en el Editor de Windows");
            }

            var tags = WindowsUiLanguages.Read();
            Assert.That(tags.Count, Is.GreaterThan(0));
            foreach (var tag in tags)
            {
                Assert.That(Regex.IsMatch(tag, "^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$"), Is.True, tag);
            }
        }

        [Test, Category("AC-APP-09-06")]
        public void AndroidListIsParsedInOrder()
        {
            Assert.That(AndroidLocaleList.Parse("es-AR,en-US, pt-BR"), Is.EqualTo(new[] { "es-AR", "en-US", "pt-BR" }));
            Assert.That(AndroidLocaleList.Parse(""), Is.Empty);
            Assert.That(WindowsUiLanguages.Split(new[] { 'e', 's', '-', 'A', 'R', '\0', 'e', 'n', '\0', '\0' }), Is.EqualTo(new[] { "es-AR", "en" }));
        }
    }
}
