using System;

namespace RowMate.Core.Ble
{
    // Falla de una operación Bluetooth (02-tech-design §2.1): la operación ("scan", "connect", "discover", "write"…), el código
    // de la plataforma (0 si no hay) y el detalle. Message = "{op}: {detalle}": es lo que muestra el banner después de
    // "Error al escanear: " / "Error al conectar: " (dp:111, :125; el detalle técnico no se traduce, spec §4).
    public class BleException : Exception
    {
        public BleException(string operation, string detail, int code = 0, Exception? inner = null)
            : base((operation ?? "ble") + ": " + (detail ?? ""), inner)
        {
            Operation = operation ?? "ble";
            Detail = detail ?? "";
            Code = code;
        }

        public string Operation { get; }

        public string Detail { get; }

        public int Code { get; }
    }
}
