using System;
using System.IO;
using UnityEngine;

namespace RowMate.Platform.Storage
{
    // Carpeta PRIVADA de la base local por plataforma (F-HIS-17, AC-HIS-17-01; 03-rutinas/02-tech-design §2.4):
    // - Windows (build y Editor), macOS e iOS: Application.persistentDataPath (en Windows,
    //   %USERPROFILE%\AppData\LocalLow\iguisoft\RowMate: empresa y producto de AC-APP-16-02).
    // - Android: el almacenamiento INTERNO de la app, getDatabasePath("x").getParentFile() por JNI
    //   (/data/user/0/<paquete>/databases, el mismo lugar que usa sqflite en el legacy). Application.persistentDataPath no
    //   sirve: Unity lo resuelve con getExternalFilesDir, que se ve desde la PC por USB. Si la carpeta no existe, se crea.
    // La ruta se resuelve en el hilo principal (JNI) y llega a Infrastructure como texto. Ninguna ruta va al registro.
    public sealed class AppDataLocation
    {
        // El nombre que se le pide a getDatabasePath: solo se usa su carpeta.
        public const string AndroidProbeName = "rowmate";

        public string DatabaseDirectory()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var folder = AndroidDatabasesDirectory();
#else
            var folder = Application.persistentDataPath;
#endif
            Directory.CreateDirectory(folder);
            return folder;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // context.getDatabasePath("rowmate").getParentFile().getAbsolutePath(), con el contexto de la actividad de Unity.
        private static string AndroidDatabasesDirectory()
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var database = context.Call<AndroidJavaObject>("getDatabasePath", AndroidProbeName))
            using (var parent = database.Call<AndroidJavaObject>("getParentFile"))
            {
                var path = parent.Call<string>("getAbsolutePath");
                if (string.IsNullOrEmpty(path))
                {
                    throw new InvalidOperationException("getDatabasePath no devolvió carpeta");
                }

                return path;
            }
        }
#endif
    }
}
