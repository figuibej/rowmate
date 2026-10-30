using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
#endif

namespace RowMate.Platform.SystemUi
{
    // Las barras del sistema de Android (F-APP-15; AC-APP-15-01; legacy immersive_workout_screen.dart:32-41; 04b-hud-exr/02-tech-design
    // §4.2; ARCHITECTURE §13), por JNI desde C# en el hilo de UI de la actividad (runOnUiThread con un AndroidJavaRunnable):
    // - API 30 o más: window.getInsetsController(): hide(WindowInsets.Type.systemBars()) con
    //   BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE (si el usuario las revela deslizando, se vuelven a ocultar solas); mostrar:
    //   show(systemBars()).
    // - API 26 a 29: decorView.setSystemUiVisibility con IMMERSIVE_STICKY | FULLSCREEN | HIDE_NAVIGATION | LAYOUT_STABLE |
    //   LAYOUT_HIDE_NAVIGATION | LAYOUT_FULLSCREEN, y al mostrar solo los LAYOUT_* ("de borde a borde", :39).
    // NO VERIFICABLE sin el módulo de Android y el teléfono ([Device-And]; su [Ignore] en WorkoutDeviceChecksTests).
    public sealed class AndroidSystemUi : ISystemUi
    {
        private const int SystemBarsBehaviorShowTransientBySwipe = 2;
        private const int LayoutStable = 0x00000100;
        private const int LayoutHideNavigation = 0x00000200;
        private const int LayoutFullscreen = 0x00000400;
        private const int HideNavigation = 0x00000002;
        private const int Fullscreen = 0x00000004;
        private const int ImmersiveSticky = 0x00001000;

        private readonly IAppLogger _logger;

        public AndroidSystemUi(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void SetImmersive(bool immersive)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() => Apply(activity, immersive)));
                }
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, "UI", "barras del sistema: " + error.GetType().Name);
            }
#else
            _ = immersive;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void Apply(AndroidJavaObject activity, bool immersive)
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                {
                    var sdk = version.GetStatic<int>("SDK_INT");
                    if (sdk >= 30)
                    {
                        using (var controller = window.Call<AndroidJavaObject>("getInsetsController"))
                        using (var types = new AndroidJavaClass("android.view.WindowInsets$Type"))
                        {
                            var bars = types.CallStatic<int>("systemBars");
                            if (immersive)
                            {
                                controller.Call("setSystemBarsBehavior", SystemBarsBehaviorShowTransientBySwipe);
                                controller.Call("hide", bars);
                            }
                            else
                            {
                                controller.Call("show", bars);
                            }
                        }

                        return;
                    }

                    using (var decor = window.Call<AndroidJavaObject>("getDecorView"))
                    {
                        var layout = LayoutStable | LayoutHideNavigation | LayoutFullscreen;
                        decor.Call("setSystemUiVisibility", immersive ? layout | ImmersiveSticky | Fullscreen | HideNavigation : layout);
                    }
                }
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, "UI", "barras del sistema: " + error.GetType().Name);
            }
            finally
            {
                activity.Dispose();
            }
        }
#endif
    }
}
