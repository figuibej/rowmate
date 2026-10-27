using System;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
#endif

namespace RowMate.Platform.Accessibility
{
    // Accesibilidad de Android por JNI (ARCHITECTURE §13.2): Settings.Global animator_duration_scale == 0 (reducir
    // movimiento, "Quitar animaciones") y Configuration.fontScale (tamaño de letra). NO VERIFICABLE sin el módulo de Android.
    internal static class AndroidAccessibility
    {
        public static bool ReduceMotion()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                using (var global = new AndroidJavaClass("android.provider.Settings$Global"))
                {
                    return global.CallStatic<float>("getFloat", resolver, "animator_duration_scale", 1f) == 0f;
                }
            }
            catch (Exception)
            {
                return false;
            }
#else
            return false;
#endif
        }

        public static double TextScale()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var resources = activity.Call<AndroidJavaObject>("getResources"))
                using (var configuration = resources.Call<AndroidJavaObject>("getConfiguration"))
                {
                    return configuration.Get<float>("fontScale");
                }
            }
            catch (Exception)
            {
                return 1.0;
            }
#else
            return 1.0;
#endif
        }
    }
}
