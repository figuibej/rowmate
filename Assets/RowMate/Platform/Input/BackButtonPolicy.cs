using System;
using RowMate.Core.Ports;
using UnityEngine;

namespace RowMate.Platform.Input
{
    // "Atrás" del sistema en la raíz del shell (AC-APP-02-06, D-09, DV-06). Android API >= 31: la app va a segundo plano
    // (moveTaskToBack(true), lo que hace el sistema con la pantalla raíz de cualquier app desde Android 12); API < 31:
    // Application.Quit() (el legacy termina la app en Android 8-15). Escritorio y Editor: nada (como el legacy).
    public sealed class BackButtonPolicy : IBackButtonPolicy
    {
        public const int MoveTaskToBackFromApi = 31;

        private readonly bool _android;
        private readonly int _apiLevel;

        public BackButtonPolicy(bool android, int apiLevel)
        {
            _android = android;
            _apiLevel = apiLevel;
        }

        public enum BackAction
        {
            Nothing,
            MoveTaskToBack,
            Quit
        }

        public int OnBackCalls { get; private set; }

        // La regla, sin efectos (la prueba [u] SMT.BackButtonPolicyByApiLevel).
        public static BackAction ActionFor(bool android, int apiLevel)
        {
            if (!android)
            {
                return BackAction.Nothing;
            }

            return apiLevel >= MoveTaskToBackFromApi ? BackAction.MoveTaskToBack : BackAction.Quit;
        }

        public static BackButtonPolicy ForCurrentPlatform()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new BackButtonPolicy(true, AndroidApiLevel());
#else
            return new BackButtonPolicy(false, 0);
#endif
        }

        public void OnBackAtRoot()
        {
            OnBackCalls++;
            switch (ActionFor(_android, _apiLevel))
            {
                case BackAction.MoveTaskToBack:
                    MoveTaskToBack();
                    break;
                case BackAction.Quit:
                    Application.Quit();
                    break;
            }
        }

        private static void MoveTaskToBack()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call<bool>("moveTaskToBack", true);
                }
            }
            catch (Exception)
            {
                Application.Quit();
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static int AndroidApiLevel()
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    return version.GetStatic<int>("SDK_INT");
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }
#endif
    }
}
