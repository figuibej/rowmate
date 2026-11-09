using System;
using RowMate.Core.Common;

namespace RowMate.Core.Ble
{
    // Qué cadencia de reconexión del pulsómetro está corriendo (A11-01): la del arranque (6 × 10 s) o la de la caída (cada 3 s).
    public enum HrmRetryReason
    {
        None,
        Startup,
        Drop
    }

    // Quién conectó el sensor (A11-01, DT11-12): Manual = la fila de la lista; Startup = la cadencia del arranque (al abrir la app o
    // "Conectar"); Drop = la cadencia de la caída. El aviso "Pulsómetro conectado" sale solo con Startup y Drop (AC-HRM-07-05).
    public enum HrmConnectOrigin
    {
        None,
        Manual,
        Startup,
        Drop
    }

    // El estado del enlace con el pulsómetro además de HrmStatus (It. 11, A11-01; 02-tech-design §2.5):
    // - Reconnect: Attempting con el intento en vuelo, Waiting en la espera (NextAttemptAt = hora monótona del IScheduler del
    //   próximo intento; Wait = la espera entera, 10 s o 3 s). La vista arma el arco con NextAttemptAt − Wait.
    // - Reason: la cadencia que corre; Run: el número de corrida de esa cadencia (sube en cada Start: "corrida nueva").
    // - IsStale: el watchdog disparó con el sensor conectado (se limpia con la próxima lectura válida).
    // - ConnectedBy: quién conectó la conexión actual (None si no hay).
    // El simulado y Unavailable no tienen: HrmLinkSource.Idle.
    public sealed record HrmLink(
        ReconnectPhase Reconnect,
        HrmRetryReason Reason,
        int Run,
        double? NextAttemptAt,
        TimeSpan Wait,
        bool IsStale,
        HrmConnectOrigin ConnectedBy)
    {
        public static readonly HrmLink Idle = new HrmLink(ReconnectPhase.None, HrmRetryReason.None, 0, null, TimeSpan.Zero, false,
            HrmConnectOrigin.None);

        public bool IsReconnecting => Reconnect != ReconnectPhase.None;
    }

    // El enlace de quien no tiene (IHeartRateService.Link por defecto): siempre Idle, nunca avisa ni guarda suscriptores.
    public static class HrmLinkSource
    {
        public static readonly IReadOnlyObservable<HrmLink> Idle = new ConstantLink();

        private sealed class ConstantLink : IReadOnlyObservable<HrmLink>
        {
            public HrmLink Value => HrmLink.Idle;

            public IDisposable Subscribe(Action<HrmLink> onChange, bool emitCurrent = true)
            {
                if (onChange == null)
                {
                    throw new ArgumentNullException(nameof(onChange));
                }

                if (emitCurrent)
                {
                    onChange(HrmLink.Idle);
                }

                return Nothing.Instance;
            }
        }

        private sealed class Nothing : IDisposable
        {
            public static readonly Nothing Instance = new Nothing();

            public void Dispose()
            {
            }
        }
    }
}
