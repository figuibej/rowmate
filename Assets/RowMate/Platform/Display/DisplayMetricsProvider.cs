using System;
using System.Runtime.InteropServices;
using RowMate.Core.Ports;
using UnityEngine;

namespace RowMate.Platform.Display
{
    // IDisplayMetrics de la app (02-tech-design §3.1, D-03, AC-APP-05-09): px por dp y área segura en dp.
    // - Densidad: Android DisplayMetrics.density (JNI); Windows GetDpiForWindow(GetActiveWindow()) / 96 con respaldo
    //   Screen.dpi / 96; Editor y resto Screen.dpi / 96 (1 si Unity no informa dpi).
    // - Área segura: Screen.safeArea tiene el origen abajo a la izquierda; Top = (alto − yMax) / escala, Bottom = y / escala,
    //   Left = x / escala, Right = (ancho − xMax) / escala. En Windows y en el Editor es la pantalla entera (márgenes 0).
    // Refresh() vuelve a leer todo; lo llama UiSurface cuando cambian el tamaño, los dpi o el área segura.
    public sealed class DisplayMetricsProvider : IDisplayMetrics
    {
        public DisplayMetricsProvider()
        {
            Refresh();
        }

        public double DpScale { get; private set; } = 1;

        public SafeInsets SafeArea { get; private set; }

        public void Refresh()
        {
            DpScale = ReadDensity();
            SafeArea = InsetsFor(Screen.width, Screen.height, Screen.safeArea, DpScale);
        }

        public static SafeInsets InsetsFor(int screenWidth, int screenHeight, Rect safe, double scale)
        {
            if (!(scale > 0) || screenWidth <= 0 || screenHeight <= 0)
            {
                return SafeInsets.None;
            }

            return new SafeInsets(
                Math.Max(0, safe.x) / scale,
                Math.Max(0, screenHeight - safe.yMax) / scale,
                Math.Max(0, screenWidth - safe.xMax) / scale,
                Math.Max(0, safe.y) / scale);
        }

        private static double ReadDensity()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var resources = activity.Call<AndroidJavaObject>("getResources"))
                using (var metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
                {
                    var density = metrics.Get<float>("density");
                    if (density > 0)
                    {
                        return density;
                    }
                }
            }
            catch (Exception)
            {
                // cae al respaldo de Screen.dpi
            }
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                var dpi = GetDpiForWindow(GetActiveWindow());
                if (dpi > 0)
                {
                    return dpi / 96.0;
                }
            }
            catch (Exception)
            {
                // Windows sin GetDpiForWindow o sin ventana activa: respaldo
            }
#endif
            return Screen.dpi > 0 ? Screen.dpi / 96.0 : 1.0;
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr window);
#endif
    }
}
