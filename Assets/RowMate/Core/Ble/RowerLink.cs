using System;
using RowMate.Core.Common;

namespace RowMate.Core.Ble
{
    // Fase de la conexión que eligió el usuario (A10-01; AC-BLE-06-07): la tarjeta tocada muestra el paso en curso.
    public enum ConnectPhase
    {
        None,
        Connecting,
        // Solo con Control Point (K-NOCP no pasa por acá).
        TakingControl,
        Ready
    }

    // Fase de la reconexión automática (A10-01; AC-BLE-08-02 a -05).
    public enum ReconnectPhase
    {
        None,
        // Esperando el próximo intento (NextAttemptAt): el arco de 3 s de la vista.
        Waiting,
        // Intento en curso (connect con tope de 10 s, descubrimiento y toma de control).
        Attempting
    }

    // Motivo de la última desconexión (A10-01; AC-BLE-09-02 lo usa para el aviso de "monitor apagado").
    public enum DisconnectReason
    {
        None,
        Manual,
        Watchdog,
        Lost,
        ConnectFailed
    }

    // El estado del enlace con el remo además de BleStatus (A10-01): el equipo, la fase, la reconexión y el motivo. Lo publica
    // RowerService; el simulador y el remo sin Bluetooth no tienen (RowerLinkSource.Idle). NextAttemptAt es la hora monótona del
    // programador (IScheduler.MonotonicSeconds) del próximo intento, o null.
    public sealed record RowerLink(
        string? DeviceId,
        string? DeviceName,
        ConnectPhase Phase,
        ReconnectPhase Reconnect,
        double? NextAttemptAt,
        DisconnectReason LastDisconnect)
    {
        public static readonly RowerLink Idle = new RowerLink(null, null, ConnectPhase.None, ReconnectPhase.None, null, DisconnectReason.None);

        public bool IsReconnecting => Reconnect != ReconnectPhase.None;
    }

    // El enlace de quien no tiene (IRowerService.Link por defecto): siempre Idle, nunca avisa ni guarda suscriptores.
    public static class RowerLinkSource
    {
        public static readonly IReadOnlyObservable<RowerLink> Idle = new ConstantLink();

        private sealed class ConstantLink : IReadOnlyObservable<RowerLink>
        {
            public RowerLink Value => RowerLink.Idle;

            public IDisposable Subscribe(Action<RowerLink> onChange, bool emitCurrent = true)
            {
                if (onChange == null)
                {
                    throw new ArgumentNullException(nameof(onChange));
                }

                if (emitCurrent)
                {
                    onChange(RowerLink.Idle);
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
