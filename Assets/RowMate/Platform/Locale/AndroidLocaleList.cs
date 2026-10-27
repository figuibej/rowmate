using System;
using System.Collections.Generic;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
#endif

namespace RowMate.Platform.Locale
{
    // Lista de idiomas de Android en orden: LocaleList.getDefault().toLanguageTags() ("es-AR,en-US") por JNI (API 24+;
    // la app pide 26). Es la lista que Flutter le pasa a la app (AC-APP-09-06). NO VERIFICABLE sin el módulo de Android.
    internal static class AndroidLocaleList
    {
        public static IReadOnlyList<string> Read()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var localeList = new AndroidJavaClass("android.os.LocaleList"))
                using (var current = localeList.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    return Parse(current.Call<string>("toLanguageTags"));
                }
            }
            catch (Exception)
            {
                return new string[0];
            }
#else
            return new string[0];
#endif
        }

        internal static IReadOnlyList<string> Parse(string? joined)
        {
            var tags = new List<string>();
            if (string.IsNullOrEmpty(joined))
            {
                return tags;
            }

            var parts = joined!.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                var tag = parts[i].Trim();
                if (tag.Length > 0)
                {
                    tags.Add(tag);
                }
            }

            return tags;
        }
    }
}
