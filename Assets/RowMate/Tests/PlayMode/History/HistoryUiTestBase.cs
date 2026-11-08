using System;
using System.Collections;
using System.Collections.Generic;
using RowMate.Presentation.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;

namespace RowMate.Tests.PlayMode.History
{
    // Base de las pruebas del historial (09-historial/02-tech-design §8.2): un UiTestHost con la base temporal sembrada POR CÓDIGO
    // con HistorySeeds (F-9, F-9V, F-9P) a través de los repositorios reales, en la zona UTC−03:00 (UiHostOptions.LocalZone), o con
    // sesiones de guion (fallas a pedido). Nunca las bases del usuario (TempDatabase con su guardia).
    public abstract class HistoryUiTestBase : UiTestBase
    {
        protected HistorySeedResult? Seed { get; private set; }

        protected UiTestHost CreateHistory(string set, DeviceProfile? profile = null, Action<UiHostOptions>? configure = null)
        {
            var options = new UiHostOptions { Profile = profile ?? DeviceProfile.TV };
            options.UseHistory(set, result => Seed = result);
            configure?.Invoke(options);
            return Create(options);
        }

        // Sesiones de guion (en memoria) sembradas con F-9 y fallas a pedido.
        protected UiTestHost CreateScriptedHistory(Action<ScriptedSessionRepository> configure, DeviceProfile? profile = null)
        {
            var options = new UiHostOptions
            {
                Profile = profile ?? DeviceProfile.TV,
                Sessions = (scheduler, logger, routines) =>
                {
                    var repository = new ScriptedSessionRepository(scheduler);
                    Seed = HistorySeeds.Apply(routines.Repository, repository.Store, HistorySeeds.F9);
                    configure(repository);
                    return repository;
                },
            };
            return Create(options);
        }

        protected HistoryScreenView Screen => H.Shell.HistoryScreen ?? throw new InvalidOperationException("sin la pantalla Historial");

        protected SessionDetailPage Detail => H.Shell.Pages.Top as SessionDetailPage ?? throw new InvalidOperationException("no hay un detalle abierto");

        protected SessionDetailPage? DetailOrNull => H.Shell.Pages.Top as SessionDetailPage;

        protected HistoryCardView Card(int index)
        {
            return Screen.List.CardAt(index) ?? throw new InvalidOperationException("no hay tarjeta " + index);
        }

        protected IEnumerator GoToHistory()
        {
            yield return Tap("tab-history");
        }

        // Abre la tarjeta n (la más nueva es la 0) y deja la entrada del gráfico en su final.
        protected IEnumerator OpenCard(int index)
        {
            yield return Frames();
            H.Tap("history-card-" + index);
            yield return Settle();
            if (DetailOrNull != null)
            {
                Detail.Chart.FinishEntry();
            }

            yield return Frames();
        }

        protected static string[] Texts(StatRowView row)
        {
            var texts = new List<string>();
            for (var i = 0; i < row.Count; i++)
            {
                texts.Add(row.ValueAt(i).text);
            }

            return texts.ToArray();
        }
    }
}
