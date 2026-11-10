using System;

namespace RowMate.EditorTools
{
    // La firma de Android para la tienda (It. 14, 14-pulido-plataformas/02-tech-design §8.1; AC-APP-19-09, DV-139): llega SOLO por
    // argumentos de la línea de comandos (los que arma game-ci/unity-builder con los secretos opcionales ANDROID_KEYSTORE_*): ruta del
    // keystore decodificado, su contraseña, el alias y su contraseña. Si falta alguno o viene vacío, null: Unity firma con la llave de
    // depuración (como el legacy, build.gradle.kts:33-38) y el artefacto se llama RowMate-Android-debugsigned. BuildCli la aplica en
    // memoria y la restaura en finally; nunca se escribe en ProjectSettings ni en el registro del build.
    public sealed class AndroidSigning
    {
        public const string KeystoreArgument = "-androidKeystoreName";
        public const string KeystorePassArgument = "-androidKeystorePass";
        public const string AliasArgument = "-androidKeyaliasName";
        public const string AliasPassArgument = "-androidKeyaliasPass";

        public AndroidSigning(string keystore, string keystorePass, string alias, string aliasPass)
        {
            Keystore = keystore;
            KeystorePass = keystorePass;
            Alias = alias;
            AliasPass = aliasPass;
        }

        public string Keystore { get; }

        public string KeystorePass { get; }

        public string Alias { get; }

        public string AliasPass { get; }

        public static AndroidSigning? FromCommandLine(string[] args)
        {
            var keystore = BuildRequest.Value(args, KeystoreArgument);
            var keystorePass = BuildRequest.Value(args, KeystorePassArgument);
            var alias = BuildRequest.Value(args, AliasArgument);
            var aliasPass = BuildRequest.Value(args, AliasPassArgument);
            if (Blank(keystore) || Blank(keystorePass) || Blank(alias) || Blank(aliasPass))
            {
                return null;
            }

            return new AndroidSigning(keystore!, keystorePass!, alias!, aliasPass!);
        }

        private static bool Blank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) || value!.StartsWith("-", StringComparison.Ordinal);
        }
    }
}
