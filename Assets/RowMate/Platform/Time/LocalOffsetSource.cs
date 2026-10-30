using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RowMate.Platform.Time
{
    // El offset de la zona horaria del equipo SIN la caché de TimeZoneInfo (AC-WKT-19-04; 04b-hud-exr/02-tech-design §2.8.3;
    // DT4b-13): en .NET, TimeZoneInfo.Local no ve un cambio de zona hasta ClearCachedData, y el reloj de pared tiene que mostrar la
    // hora nueva antes de 1 s. En Windows (build y Editor) lee GetTimeZoneInformation en un búfer reservado UNA vez
    // (Marshal.AllocHGlobal; sin asignar por lectura): el sesgo actual de la zona, con el de verano si rige. En Android e iOS,
    // TimeZoneInfo.Local (la 10a lo revisa). UnityScheduler lo lee cada 0,5 s.
    public sealed class LocalOffsetSource : IDisposable
    {
        // TIME_ZONE_INFORMATION: LONG Bias; WCHAR StandardName[32]; SYSTEMTIME StandardDate; LONG StandardBias;
        // WCHAR DaylightName[32]; SYSTEMTIME DaylightDate; LONG DaylightBias = 172 bytes.
        public const int StructSize = 172;
        private const int StandardBiasOffset = 4 + 64 + 16;
        private const int DaylightBiasOffset = StandardBiasOffset + 4 + 64 + 16;
        private const uint TimeZoneIdInvalid = 0xFFFFFFFF;
        private const uint TimeZoneIdStandard = 1;
        private const uint TimeZoneIdDaylight = 2;

        private readonly bool _windows;
        private IntPtr _buffer;
        private bool _failed;

        public LocalOffsetSource()
            : this(Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
        }

        public LocalOffsetSource(bool windows)
        {
            _windows = windows;
            if (_windows)
            {
                _buffer = Marshal.AllocHGlobal(StructSize);
            }
        }

        // Lee el sistema con GetTimeZoneInformation (false: TimeZoneInfo.Local).
        public bool UsesWindowsApi => _windows && !_failed;

        // El offset local para esa hora UTC.
        public TimeSpan OffsetFor(DateTimeOffset utc)
        {
            if (!_windows || _failed || _buffer == IntPtr.Zero)
            {
                return TimeZoneInfo.Local.GetUtcOffset(utc);
            }

            try
            {
                var id = GetTimeZoneInformation(_buffer);
                if (id == TimeZoneIdInvalid)
                {
                    return TimeZoneInfo.Local.GetUtcOffset(utc);
                }

                var bias = Marshal.ReadInt32(_buffer, 0);
                if (id == TimeZoneIdDaylight)
                {
                    bias += Marshal.ReadInt32(_buffer, DaylightBiasOffset);
                }
                else if (id == TimeZoneIdStandard)
                {
                    bias += Marshal.ReadInt32(_buffer, StandardBiasOffset);
                }

                // UTC = local + sesgo: el offset es −sesgo, en minutos.
                return TimeSpan.FromMinutes(-bias);
            }
            catch (Exception error) when (error is DllNotFoundException || error is EntryPointNotFoundException)
            {
                _failed = true;
                return TimeZoneInfo.Local.GetUtcOffset(utc);
            }
        }

        public void Dispose()
        {
            if (_buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_buffer);
                _buffer = IntPtr.Zero;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetTimeZoneInformation(IntPtr timeZoneInformation);
    }
}
