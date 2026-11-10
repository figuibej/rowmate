using System;
using System.Text.RegularExpressions;

namespace RowMate.Core.Ble
{
    // El detalle de una falla de Bluetooth para mostrar (It. 14, #44, AC-DEBT-04; DT14-27): BleException.Message ya trae
    // "{operación}: {detalle}" y el banner antepone "Error al escanear: " o "Error al conectar: ", así que se leía
    // "Error al escanear: scan: error 2". ForDisplay quita el prefijo técnico de la operación y deja solo el detalle del
    // sistema (sin traducir). Las operaciones son las que usan los servicios y el adaptador nativo (el diseño nombraba scan,
    // connect, discover y subscribe; se suman connection, setNotify, write y ble, que también existen). Los servicios NO cambian
    // sus mensajes (las pruebas que los comparan siguen en verde).
    public static class BleErrorText
    {
        private static readonly Regex OperationPrefix = new Regex(
            @"^(scan|connect|connection|discover|subscribe|setNotify|write|ble): ", RegexOptions.CultureInvariant);

        public static string ForDisplay(string detail)
        {
            if (detail == null)
            {
                throw new ArgumentNullException(nameof(detail));
            }

            var match = OperationPrefix.Match(detail);
            return match.Success ? detail.Substring(match.Length) : detail;
        }
    }
}
