using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Localization
{
    // F-APP-09: idioma vigente sin selector ni memoria (Q-05) y re-resolución cuando cambia el sistema.
    [TestFixture]
    public sealed class LanguageStateTests
    {
        // AC-APP-09-04: no hay ajuste de idioma en la app ni se guarda: la API pública es solo Current (de solo lectura)
        // y Refresh(lista del sistema), y cada arranque (constructor) vuelve a resolver desde el sistema.
        [Test, Category("AC-APP-09-04")]
        public void NoSetterNoPersistence()
        {
            var type = typeof(LanguageState);
            var members = new List<string>();
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                members.Add(member.MemberType + " " + member.Name);
            }

            Assert.That(members, Is.EquivalentTo(new[] { "Constructor .ctor", "Property Current", "Method get_Current", "Method Refresh" }));

            var current = type.GetProperty("Current")!;
            Assert.That(current.CanWrite, Is.False, "Current no tiene setter");
            Assert.That(current.PropertyType, Is.EqualTo(typeof(IReadOnlyObservable<AppLanguage>)));

            // Solo recibe la lista del sistema y el logger de diagnóstico (QA-V-01: adonde va la excepción de una vista
            // suscripta). Ningún parámetro de preferencias guardadas.
            Assert.That(type.GetConstructors().Length, Is.EqualTo(1));
            var constructor = type.GetConstructors()[0].GetParameters();
            Assert.That(constructor.Length, Is.EqualTo(2));
            Assert.That(constructor[0].ParameterType, Is.EqualTo(typeof(IReadOnlyList<string>)), "solo recibe la lista del sistema: nada de preferencias guardadas");
            Assert.That(constructor[1].ParameterType, Is.EqualTo(typeof(IAppLogger)), "el segundo parámetro es solo el log de diagnóstico");

            var refresh = type.GetMethod("Refresh")!.GetParameters();
            Assert.That(refresh.Length, Is.EqualTo(1));
            Assert.That(refresh[0].ParameterType, Is.EqualTo(typeof(IReadOnlyList<string>)));

            // Ni con un cast se puede fijar el idioma.
            var state = new LanguageState(new[] { "es-AR" }, NullAppLogger.Instance);
            Assert.That(state.Current, Is.Not.InstanceOf<ObservableValue<AppLanguage>>());

            // Cada "arranque" resuelve desde el sistema; lo que pasó antes no queda guardado.
            state.Refresh(new[] { "en-US" });
            Assert.That(new LanguageState(new[] { "es-AR" }, NullAppLogger.Instance).Current.Value, Is.EqualTo(AppLanguage.Es));
            Assert.That(new LanguageState(null, NullAppLogger.Instance).Current.Value, Is.EqualTo(AppLanguage.En));
        }

        [Test, Category("AC-APP-09-04")]
        [Ignore("Se verifica en PlayMode con el simulador: LanguageOnScreenTests.NoLanguageSelectorAndNoMemory (It. 2). [Device] Android: QA en equipo (05-qa-verification de la It. 2)")]
        public void NoSelectorOnScreen()
        {
        }

        // AC-APP-09-05: con la app en español, el sistema pasa a [en-US] → al resolver de nuevo da inglés y avisa una vez.
        [Test, Category("AC-APP-09-05")]
        public void RefreshNotifiesChange()
        {
            var state = new LanguageState(new[] { "es-AR" }, NullAppLogger.Instance);
            Assert.That(state.Current.Value, Is.EqualTo(AppLanguage.Es));
            var seen = new List<AppLanguage>();
            using (state.Current.Subscribe(seen.Add, false))
            {
                state.Refresh(new[] { "en-US" });
                Assert.That(state.Current.Value, Is.EqualTo(AppLanguage.En));
                Assert.That(seen, Is.EqualTo(new[] { AppLanguage.En }));

                state.Refresh(new[] { "en-GB", "es" });
                Assert.That(seen, Is.EqualTo(new[] { AppLanguage.En }), "sin cambio de idioma no avisa");

                state.Refresh(new[] { "fr-FR", "es-AR" });
                Assert.That(seen, Is.EqualTo(new[] { AppLanguage.En, AppLanguage.Es }));

                state.Refresh(null);
                Assert.That(seen, Is.EqualTo(new[] { AppLanguage.En, AppLanguage.Es, AppLanguage.En }), "sin lista: inglés");
            }
        }

        // QA-V-01: una vista que falla al recibir el idioma nuevo no deja sin aviso a las demás; el error va al log.
        [Test, Category("AC-APP-09-05"), Category("QA-V-01")]
        public void BrokenViewDoesNotStopTheOthers()
        {
            var logger = new RecordingLogger();
            var state = new LanguageState(new[] { "es-AR" }, logger);
            var seen = new List<AppLanguage>();
            using (state.Current.Subscribe(l => throw new InvalidOperationException("vista rota"), false))
            using (state.Current.Subscribe(seen.Add, false))
            {
                Assert.DoesNotThrow(() => state.Refresh(new[] { "en-US" }));
            }

            Assert.That(state.Current.Value, Is.EqualTo(AppLanguage.En));
            Assert.That(seen, Is.EqualTo(new[] { AppLanguage.En }));
            Assert.That(logger.Lines.Count, Is.EqualTo(1));
            Assert.That(logger.Lines[0], Does.StartWith("[STATE] Error: "));
        }

        [Test, Category("AC-APP-09-05")]
        public void SubscribeEmitsCurrentLanguage()
        {
            var state = new LanguageState(new[] { "es" }, NullAppLogger.Instance);
            AppLanguage? first = null;
            using (state.Current.Subscribe(l => first = l))
            {
                Assert.That(first, Is.EqualTo(AppLanguage.Es));
            }
        }

        [Test, Category("AC-APP-09-05")]
        [Ignore("Se verifica en PlayMode con el simulador: LanguageOnScreenTests.LiveLanguageChangeKeepsEverythingElse (It. 2). [Device] Android: QA en equipo (05-qa-verification de la It. 2)")]
        public void VisibleChangeWithoutRestart()
        {
        }

        // AC-APP-09-07 (diferencia aceptada, P-06): textos del sistema (teclado y menú de copiar y pegar).
        [Test, Category("AC-APP-09-07")]
        [Ignore("NO VERIFICABLE → It. 3: [Device] P-06 aceptada; primer campo de texto: editor de rutinas (teclado y menú de copiar y pegar en el idioma del sistema)")]
        public void SystemTextsFollowSystemLanguage()
        {
        }

        [Test, Category("AC-APP-09-04")]
        public void ConstructorAcceptsTheSameInputsAsTheResolver()
        {
            Assert.That(new LanguageState(new string[0], NullAppLogger.Instance).Current.Value, Is.EqualTo(AppLanguage.En));
            Assert.That(new LanguageState(new[] { "fr-FR, es" }, NullAppLogger.Instance).Current.Value, Is.EqualTo(AppLanguage.En), "una sola etiqueta rara no coincide");
            Assert.Throws<ArgumentNullException>(() => LanguageTag.Parse(null!));
        }
    }
}
