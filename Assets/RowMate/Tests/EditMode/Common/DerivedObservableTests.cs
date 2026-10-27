using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // DerivedObservable (02-tech-design §2.1): el idioma vigente → su Localizer (AppContext.Texts), y ReadOnlyObservable, la
    // vista de solo lectura que exponen los stores y los servicios.
    [TestFixture]
    public sealed class DerivedObservableTests
    {
        [Test]
        public void MapsTheCurrentValueAndEveryNotification()
        {
            var source = new ObservableValue<int>(2, NullAppLogger.Instance);
            var derived = new DerivedObservable<int, string>(source, v => "x" + v, NullAppLogger.Instance);
            Assert.That(derived.Value, Is.EqualTo("x2"));
            var seen = new List<string>();
            derived.Subscribe(seen.Add);
            source.Value = 3;
            source.Value = 3; // la fuente no avisa: el derivado tampoco
            source.ForceNotify(); // la fuente reavisa: el derivado también
            Assert.That(seen, Is.EqualTo(new[] { "x2", "x3", "x3" }));
            Assert.That(derived.Value, Is.EqualTo("x3"));
        }

        [Test]
        public void DisposeStopsListeningAndFreezesTheValue()
        {
            var source = new ObservableValue<int>(1, NullAppLogger.Instance);
            var derived = new DerivedObservable<int, int>(source, v => v * 10, NullAppLogger.Instance);
            var seen = new List<int>();
            derived.Subscribe(seen.Add, false);
            derived.Dispose();
            Assert.That(derived.IsDisposed, Is.True);
            Assert.That(source.SubscriberCount, Is.EqualTo(0), "se dio de baja de la fuente");
            source.Value = 2;
            Assert.That(seen, Is.Empty);
            Assert.That(derived.Value, Is.EqualTo(10));
            Assert.DoesNotThrow(() => derived.Dispose());
        }

        [Test]
        public void LanguageToLocalizer()
        {
            var language = new LanguageState(new[] { "es-AR" }, NullAppLogger.Instance);
            var texts = new DerivedObservable<AppLanguage, Localizer>(language.Current, LocalizationFixture.Set.For, NullAppLogger.Instance);
            Assert.That(texts.Value.Get(L10nKeys.NavDevice), Is.EqualTo("Dispositivo"));
            language.Refresh(new[] { "en-US" });
            Assert.That(texts.Value.Get(L10nKeys.NavDevice), Is.EqualTo("Device"));
            texts.Dispose();
        }

        [Test]
        public void SubscriberFailureGoesToTheLogger()
        {
            var logger = new RecordingLogger();
            var source = new ObservableValue<int>(0, NullAppLogger.Instance);
            var derived = new DerivedObservable<int, int>(source, v => v, logger);
            var after = 0;
            derived.Subscribe(_ => throw new InvalidOperationException("vista rota"), false);
            derived.Subscribe(_ => after++, false);
            source.Value = 1;
            Assert.That(after, Is.EqualTo(1), "los demás se enteran");
            Assert.That(logger.Lines.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReadOnlyObservableCannotBeAssigned()
        {
            var inner = new ObservableValue<int>(5, NullAppLogger.Instance);
            IReadOnlyObservable<int> view = new ReadOnlyObservable<int>(inner);
            Assert.That(view, Is.Not.InstanceOf<ObservableValue<int>>(), "ni con un cast");
            var seen = new List<int>();
            view.Subscribe(seen.Add);
            inner.Value = 6;
            Assert.That(seen, Is.EqualTo(new[] { 5, 6 }));
            Assert.That(view.Value, Is.EqualTo(6));
            Assert.Throws<ArgumentNullException>(() => new ReadOnlyObservable<int>(null!));
        }

        [Test]
        public void EventStreamIsAnEventSource()
        {
            var stream = new EventStream<int>(NullAppLogger.Instance);
            IEventSource<int> source = stream;
            var seen = new List<int>();
            using (source.Subscribe(seen.Add))
            {
                stream.Emit(1);
            }

            stream.Emit(2);
            Assert.That(seen, Is.EqualTo(new[] { 1 }));
        }
    }
}
