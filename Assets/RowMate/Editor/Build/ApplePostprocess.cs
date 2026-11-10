using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RowMate.EditorTools
{
    // El proyecto de Xcode de iOS con la identidad de RowMate (It. 14, 14-pulido-plataformas/02-tech-design DT14-09; AC-APP-16-08,
    // -11, AC-APP-15-09). Solo con BuildTarget.iOS y sin el módulo de Xcode (System.Xml):
    // - Images.xcassets/AppIcon.appiconset: el único 1024 RGB sin alfa de tools/icon (el formato universal de Xcode 14 en adelante; D-03).
    // - Images.xcassets/RowMateLaunchMark.imageset: la marca de 96 pt del storyboard (LaunchScreen.storyboard, CustomStoryboard).
    // - Info.plist: iPhone sin vertical invertida (vertical y las dos horizontales) e iPad con las cuatro, con multitarea
    //   (UIRequiresFullScreen = false). Como UnityAppController manda sobre Info.plist, AppRoot además apaga la vertical invertida en
    //   un iPhone (QA-G14-10).
    // [Apple] NO VERIFICABLE hasta la 10b; ApplePostprocessTests lo corre sobre carpetas temporales.
    public sealed class ApplePostprocess : IPostprocessBuildWithReport
    {
        public const string IdentityFolder = "Assets/RowMate/Art/Identity/Apple";
        public const string AppIconSet = "AppIcon.appiconset";
        public const string LaunchMarkSet = "RowMateLaunchMark.imageset";

        public static readonly string[] IPhoneOrientations =
        {
            "UIInterfaceOrientationPortrait", "UIInterfaceOrientationLandscapeLeft", "UIInterfaceOrientationLandscapeRight",
        };

        public static readonly string[] IPadOrientations =
        {
            "UIInterfaceOrientationPortrait", "UIInterfaceOrientationPortraitUpsideDown", "UIInterfaceOrientationLandscapeLeft",
            "UIInterfaceOrientationLandscapeRight",
        };

        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            var source = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, IdentityFolder);
            Apply(report.summary.outputPath, source);
            Debug.Log("[BUILD] iOS: ícono 1024 sin alfa, marca del arranque y orientaciones en Info.plist (ApplePostprocess)");
        }

        // xcodeProject: la carpeta del proyecto de Xcode que generó Unity; identityFolder: Art/Identity/Apple.
        public static void Apply(string xcodeProject, string identityFolder)
        {
            if (string.IsNullOrEmpty(xcodeProject) || !Directory.Exists(xcodeProject))
            {
                throw new BuildFailedException("ApplePostprocess: no está el proyecto de Xcode");
            }

            var catalog = FindCatalog(xcodeProject);
            ReplaceFolder(Path.Combine(identityFolder, AppIconSet), Path.Combine(catalog, AppIconSet));
            ReplaceFolder(Path.Combine(identityFolder, LaunchMarkSet), Path.Combine(catalog, LaunchMarkSet));
            var plist = Path.Combine(xcodeProject, "Info.plist");
            if (!File.Exists(plist))
            {
                throw new BuildFailedException("ApplePostprocess: no está Info.plist");
            }

            File.WriteAllText(plist, PatchInfoPlist(File.ReadAllText(plist)), new System.Text.UTF8Encoding(false));
        }

        // Unity-iPhone/Images.xcassets (el catálogo del proyecto de Unity); si no está, el primero que haya.
        private static string FindCatalog(string xcodeProject)
        {
            var preferred = Path.Combine(xcodeProject, "Unity-iPhone", "Images.xcassets");
            if (Directory.Exists(preferred))
            {
                return preferred;
            }

            var found = Directory.GetDirectories(xcodeProject, "Images.xcassets", SearchOption.AllDirectories);
            if (found.Length == 0)
            {
                throw new BuildFailedException("ApplePostprocess: el proyecto de Xcode no tiene Images.xcassets");
            }

            Array.Sort(found, StringComparer.Ordinal);
            return found[0];
        }

        // Copia los PNG y el Contents.json (no los .meta de Unity), borrando lo que hubiera.
        private static void ReplaceFolder(string from, string to)
        {
            if (!Directory.Exists(from))
            {
                throw new BuildFailedException("ApplePostprocess: no está " + Path.GetFileName(from) + " (correr tools/icon)");
            }

            if (Directory.Exists(to))
            {
                Directory.Delete(to, true);
            }

            Directory.CreateDirectory(to);
            var files = Directory.GetFiles(from);
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            }
        }

        // Info.plist con las orientaciones de iPhone (sin vertical invertida) y de iPad (las cuatro) y UIRequiresFullScreen = false.
        // Las demás claves quedan como estaban.
        public static string PatchInfoPlist(string xml)
        {
            var document = new XmlDocument { PreserveWhitespace = false, XmlResolver = null };
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = null };
            using (var reader = XmlReader.Create(new StringReader(xml), settings))
            {
                document.Load(reader);
            }

            var dict = document.SelectSingleNode("/plist/dict") ?? throw new BuildFailedException("ApplePostprocess: Info.plist sin <dict>");
            SetArray(document, dict, "UISupportedInterfaceOrientations", IPhoneOrientations);
            SetArray(document, dict, "UISupportedInterfaceOrientations~ipad", IPadOrientations);
            SetBool(document, dict, "UIRequiresFullScreen", false);

            var output = new StringWriter();
            var writerSettings = new XmlWriterSettings { Indent = true, IndentChars = "\t", Encoding = new System.Text.UTF8Encoding(false), NewLineChars = "\n" };
            using (var writer = XmlWriter.Create(output, writerSettings))
            {
                document.Save(writer);
            }

            return output.ToString().Replace("encoding=\"utf-16\"", "encoding=\"UTF-8\"");
        }

        // Los valores de una clave de tipo <array> de <string> (para las pruebas).
        public static IReadOnlyList<string> ReadArray(string xml, string key)
        {
            var document = new XmlDocument { XmlResolver = null };
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = null };
            using (var reader = XmlReader.Create(new StringReader(xml), settings))
            {
                document.Load(reader);
            }

            var result = new List<string>();
            var value = ValueOf(document.SelectSingleNode("/plist/dict")!, key);
            if (value == null)
            {
                return result;
            }

            foreach (XmlNode item in value.ChildNodes)
            {
                if (item.Name == "string")
                {
                    result.Add(item.InnerText);
                }
            }

            return result;
        }

        private static void SetArray(XmlDocument document, XmlNode dict, string key, IReadOnlyList<string> values)
        {
            var array = document.CreateElement("array");
            for (var i = 0; i < values.Count; i++)
            {
                var item = document.CreateElement("string");
                item.InnerText = values[i];
                array.AppendChild(item);
            }

            Replace(document, dict, key, array);
        }

        private static void SetBool(XmlDocument document, XmlNode dict, string key, bool value)
        {
            Replace(document, dict, key, document.CreateElement(value ? "true" : "false"));
        }

        private static void Replace(XmlDocument document, XmlNode dict, string key, XmlNode value)
        {
            var old = ValueOf(dict, key);
            if (old != null)
            {
                dict.ReplaceChild(value, old);
                return;
            }

            var keyNode = document.CreateElement("key");
            keyNode.InnerText = key;
            dict.AppendChild(keyNode);
            dict.AppendChild(value);
        }

        // El nodo de valor que sigue a <key>key</key> (o null).
        private static XmlNode? ValueOf(XmlNode dict, string key)
        {
            var children = dict.ChildNodes;
            for (var i = 0; i < children.Count; i++)
            {
                var node = children[i];
                if (node.NodeType == XmlNodeType.Element && node.Name == "key" && node.InnerText == key)
                {
                    for (var j = i + 1; j < children.Count; j++)
                    {
                        if (children[j].NodeType == XmlNodeType.Element)
                        {
                            return children[j];
                        }
                    }

                    return null;
                }
            }

            return null;
        }
    }
}
