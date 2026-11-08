namespace RowMate.Core.Ble.Native
{
    // Los nombres y descriptores JNI de RmBle (Kotlin, Android) que usa AndroidBleApi (10a 02-tech-design §4.3; QA-G10a-05). Es la
    // ÚNICA fuente de los literales JNI: Platform no escribe ninguno suelto. JniSignatureTests (dotnet, WP-C) compara esta tabla con
    // native/android/golden/jni-signatures.txt, y native/android/check.ps1 regenera ese archivo con javap -s de RmBle y falla con
    // código 4 si difiere. Los métodos son @JvmStatic de un object Kotlin. Si WP-C cambia una firma, cambia acá y en el golden.
    public static class RmBleJniSignatures
    {
        public const string ClassName = "com/rowmate/ble/RmBle";

        public static readonly (string Name, string Signature)[] All =
        {
            (Init.Name, Init.Signature),
            (Shutdown.Name, Shutdown.Signature),
            (ApiVersion.Name, ApiVersion.Signature),
            (MonotonicSeconds.Name, MonotonicSeconds.Signature),
            (AdapterState.Name, AdapterState.Signature),
            (StartScan.Name, StartScan.Signature),
            (StopScan.Name, StopScan.Signature),
            (Connect.Name, Connect.Signature),
            (Disconnect.Name, Disconnect.Signature),
            (Discover.Name, Discover.Signature),
            (SetNotify.Name, SetNotify.Signature),
            (Write.Name, Write.Signature),
            (KeepAliveStart.Name, KeepAliveStart.Signature),
            (KeepAliveStop.Name, KeepAliveStop.Signature),
            (Pending.Name, Pending.Signature),
            (Drain.Name, Drain.Signature),
            (RequestEnable.Name, RequestEnable.Signature),
            (HasScanPermission.Name, HasScanPermission.Signature),
            (ShouldShowRationale.Name, ShouldShowRationale.Signature),
            (WasDenied.Name, WasDenied.Signature),
            (SetDenied.Name, SetDenied.Signature),
            (OpenAppSettings.Name, OpenAppSettings.Signature),
            (Haptic.Name, Haptic.Signature),
            (SdkInt.Name, SdkInt.Signature),
        };

        public static class Init
        {
            public const string Name = "init";
            public const string Signature = "(Landroid/app/Activity;)I";
        }

        public static class Shutdown
        {
            public const string Name = "shutdown";
            public const string Signature = "()V";
        }

        public static class ApiVersion
        {
            public const string Name = "apiVersion";
            public const string Signature = "()I";
        }

        public static class MonotonicSeconds
        {
            public const string Name = "monotonicSeconds";
            public const string Signature = "()D";
        }

        public static class AdapterState
        {
            public const string Name = "adapterState";
            public const string Signature = "()I";
        }

        public static class StartScan
        {
            public const string Name = "startScan";
            public const string Signature = "(Ljava/lang/String;)I";
        }

        public static class StopScan
        {
            public const string Name = "stopScan";
            public const string Signature = "()I";
        }

        public static class Connect
        {
            public const string Name = "connect";
            public const string Signature = "(Ljava/lang/String;Z)I";
        }

        public static class Disconnect
        {
            public const string Name = "disconnect";
            public const string Signature = "(Ljava/lang/String;)I";
        }

        public static class Discover
        {
            public const string Name = "discover";
            public const string Signature = "(Ljava/lang/String;)I";
        }

        public static class SetNotify
        {
            public const string Name = "setNotify";
            public const string Signature = "(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;Z)I";
        }

        public static class Write
        {
            public const string Name = "write";
            public const string Signature = "(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;[BZ)I";
        }

        public static class KeepAliveStart
        {
            public const string Name = "keepAliveStart";
            public const string Signature = "(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;[BI)I";
        }

        public static class KeepAliveStop
        {
            public const string Name = "keepAliveStop";
            public const string Signature = "(Ljava/lang/String;)I";
        }

        public static class Pending
        {
            public const string Name = "pending";
            public const string Signature = "()I";
        }

        public static class Drain
        {
            public const string Name = "drain";
            public const string Signature = "()[B";
        }

        public static class RequestEnable
        {
            public const string Name = "requestEnable";
            public const string Signature = "()I";
        }

        public static class HasScanPermission
        {
            public const string Name = "hasScanPermission";
            public const string Signature = "()Z";
        }

        public static class ShouldShowRationale
        {
            public const string Name = "shouldShowRationale";
            public const string Signature = "(Ljava/lang/String;)Z";
        }

        public static class WasDenied
        {
            public const string Name = "wasDenied";
            public const string Signature = "()Z";
        }

        public static class SetDenied
        {
            public const string Name = "setDenied";
            public const string Signature = "(Z)V";
        }

        public static class OpenAppSettings
        {
            public const string Name = "openAppSettings";
            public const string Signature = "()V";
        }

        public static class Haptic
        {
            public const string Name = "haptic";
            public const string Signature = "()V";
        }

        public static class SdkInt
        {
            public const string Name = "sdkInt";
            public const string Signature = "()I";
        }
    }
}
