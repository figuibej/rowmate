using System;
using RowMate.Core.Common;

namespace RowMate.Core.Ble
{
    // Filtro de candidatos de la búsqueda por servicio anunciado (F-BLE-03); vale igual para el remo (FTMS) y el
    // pulsómetro (0x180D). En Android, iOS y macOS el filtro lo aplica el sistema; en Windows el plugin lo ignora y
    // entrega todos los equipos cercanos, así que se filtra en la app. En iOS/macOS NO se filtra en la app: el servicio
    // puede viajar en los "overflow UUIDs" y no figurar en la lista anunciada. Linux queda fuera (Q-50).
    // legacy: lib/core/bluetooth/scan_filter.dart:12-20; usos en ble_service.dart:106 y heart_rate_service.dart:136
    public static class ScanFilter
    {
        // = scanFilterIsClientSide (scan_filter.dart:12: Platform.isWindows || Platform.isLinux; Linux fuera).
        public static bool IsClientSide(PlatformKind platform)
        {
            return platform == PlatformKind.Windows;
        }

        // = advertisesService (scan_filter.dart:14-15): algún servicio anunciado es el buscado. Se comparan en su forma
        // de 128 bits (180d ≡ 0000180d-0000-1000-8000-00805f9b34fb), como Guid.== de flutter_blue_plus (AC-BLE-03-04).
        public static bool AdvertisesService(BleScanResult result, BleUuid service)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var uuids = result.ServiceUuids;
            if (uuids == null)
            {
                return false;
            }

            for (var i = 0; i < uuids.Count; i++)
            {
                if (uuids[i] == service)
                {
                    return true;
                }
            }

            return false;
        }

        // = acceptScanResult (scan_filter.dart:19-20) con clientSide explícito: el llamador pasa IsClientSide(plataforma).
        public static bool Accept(BleScanResult result, BleUuid service, bool clientSide)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return !clientSide || AdvertisesService(result, service);
        }
    }
}
