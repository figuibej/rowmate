using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using RowMate.Core.Common;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RowMate.EditorTools
{
    // La versión y el nombre del RowMate.exe (It. 14, 14-pulido-plataformas/02-tech-design DT14-31, QA-G14-01; AC-APP-16-06): el player
    // de Unity sale con el VS_VERSIONINFO del motor; este post-proceso lo reemplaza, al final de todo build de Windows, por el que arma
    // VersionInfoBlock (Core) con los valores de PlayerSettings (productName, companyName, bundleVersion), con BeginUpdateResourceW /
    // UpdateResourceW(RT_VERSION, 1, idioma) / EndUpdateResourceW en cada idioma que traiga (hoy 0x0409). El .exe de 6000.3.27f1 no
    // viene firmado: no se invalida ninguna firma. Si falla, BuildFailedException (build rojo). En un editor que no es de Windows (el CI
    // de Linux con GameCI) no hay UpdateResource: un aviso "::warning::VERSIONINFO sin reescribir" y el build sigue (R14-13, issue).
    // build.ps1 lo verifica después de cada build de Windows (código 9). UnityPlayer.dll queda con la versión del motor.
    public sealed class WindowsVersionInfoPostprocess : IPostprocessBuildWithReport
    {
        public const string SkippedWarning = "::warning::VERSIONINFO sin reescribir (editor que no es de Windows; R14-13)";

        private const int RtVersion = 16;
        private const int VersionId = 1;
        private const uint LoadLibraryAsDatafile = 0x00000002;
        private const uint LoadLibraryAsImageResource = 0x00000020;

        // Al final: después de NoticesPostprocessor y de lo que sume Unity.
        public int callbackOrder => 1000;

        public void OnPostprocessBuild(BuildReport report)
        {
            var platform = report.summary.platform;
            if (platform != BuildTarget.StandaloneWindows64 && platform != BuildTarget.StandaloneWindows)
            {
                return;
            }

            if (report.summary.result == BuildResult.Failed || report.summary.result == BuildResult.Cancelled)
            {
                return;
            }

            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Debug.LogWarning(SkippedWarning);
                Console.WriteLine(SkippedWarning);
                return;
            }

            var exe = report.summary.outputPath;
            var block = VersionInfoBlock.Build(PlayerSettings.productName, PlayerSettings.companyName, PlayerSettings.bundleVersion);
            try
            {
                Write(exe, block);
            }
            catch (Exception error)
            {
                throw new BuildFailedException("[BUILD] no se pudo escribir VS_VERSIONINFO en " + Path.GetFileName(exe) + ": " + error.Message);
            }

            Debug.Log("[BUILD] VS_VERSIONINFO de " + Path.GetFileName(exe) + ": " + PlayerSettings.productName + " " + PlayerSettings.bundleVersion
                + " (" + PlayerSettings.companyName + ")");
        }

        // Reemplaza el RT_VERSION #1 del ejecutable en cada idioma que traiga (si no trae, en 0x0409).
        public static void Write(string exe, byte[] block)
        {
            if (!File.Exists(exe))
            {
                throw new FileNotFoundException("no está el ejecutable", Path.GetFileName(exe));
            }

            var languages = Languages(exe);
            if (languages.Count == 0)
            {
                languages.Add(VersionInfoBlock.Language);
            }

            var handle = BeginUpdateResourceW(exe, false);
            if (handle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var ok = false;
            try
            {
                for (var i = 0; i < languages.Count; i++)
                {
                    if (!UpdateResourceW(handle, (IntPtr)RtVersion, (IntPtr)VersionId, languages[i], block, (uint)block.Length))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }

                ok = true;
            }
            finally
            {
                if (!EndUpdateResourceW(handle, !ok) && ok)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        // Los idiomas del RT_VERSION #1 (EnumResourceLanguagesW sobre el ejecutable cargado como datos).
        public static List<ushort> Languages(string exe)
        {
            var result = new List<ushort>();
            var module = LoadLibraryExW(exe, IntPtr.Zero, LoadLibraryAsDatafile | LoadLibraryAsImageResource);
            if (module == IntPtr.Zero)
            {
                return result;
            }

            try
            {
                EnumResLangProc callback = (hModule, type, name, language, param) =>
                {
                    result.Add(language);
                    return true;
                };
                EnumResourceLanguagesW(module, (IntPtr)RtVersion, (IntPtr)VersionId, callback, IntPtr.Zero);
                GC.KeepAlive(callback);
            }
            finally
            {
                FreeLibrary(module);
            }

            return result;
        }

        private delegate bool EnumResLangProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr BeginUpdateResourceW(string pFileName, [MarshalAs(UnmanagedType.Bool)] bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UpdateResourceW(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, uint cb);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EndUpdateResourceW(IntPtr hUpdate, [MarshalAs(UnmanagedType.Bool)] bool fDiscard);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryExW(string lpLibFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hLibModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumResourceLanguagesW(IntPtr hModule, IntPtr lpType, IntPtr lpName, EnumResLangProc lpEnumFunc, IntPtr lParam);
    }
}
