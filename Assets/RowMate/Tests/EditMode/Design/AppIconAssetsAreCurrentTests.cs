using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // IA · Los PNG versionados del ícono son exactamente lo que genera tools/icon (It. 14, DT14-06; 02-tech-design §2.2), como
    // Tokens.uss con TokensUssSyncTests: se vuelven a generar en memoria y se comparan byte a byte. Corre en ci-core (ubuntu y
    // windows). AppIconHashes fija el SHA-256 de 16 y 1024 (QA-G14-05): si otra plataforma diera otros bytes, la falla lo dice.
    [TestFixture]
    public sealed class AppIconAssetsAreCurrentTests
    {
        private const string Regenerate = "Regenerá con: \"C:/Program Files/dotnet/dotnet.exe\" run --project tools/dotnet/RowMate.Icon --artifacts-path tools/dotnet/.artifacts";

        private static readonly object Gate = new object();
        private static IReadOnlyList<AppIconOutput>? _outputs;

        public static IReadOnlyList<AppIconOutput> Outputs
        {
            get
            {
                lock (Gate)
                {
                    return _outputs ??= AppIconOutputs.All();
                }
            }
        }

        [Test, Category("AC-APP-16-08"), Category("AC-APP-16-09")]
        public void AppIconAssetsAreCurrent()
        {
            var stale = new List<string>();
            foreach (var output in Outputs)
            {
                var path = RepoPaths.Combine(output.RelativePath);
                if (!File.Exists(path))
                {
                    stale.Add(output.RelativePath + " (no existe)");
                    continue;
                }

                if (!Same(File.ReadAllBytes(path), output.Bytes))
                {
                    stale.Add(output.RelativePath);
                }
            }

            Assert.That(stale, Is.Empty, "PNG del ícono desactualizados:\n" + string.Join("\n", stale) + "\n" + Regenerate);
        }

        [Test, Category("AC-APP-16-08")]
        public void NoStrayPngInIdentity()
        {
            // Art/Identity tiene solo lo que genera la herramienta (nada de terceros ni a mano).
            var expected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var output in Outputs)
            {
                expected.Add(output.RelativePath);
            }

            var stray = new List<string>();
            foreach (var pattern in new[] { "*.png", "*.ico" })
            {
                foreach (var file in RepoPaths.Files(AppIconOutputs.IdentityFolder, pattern))
                {
                    var relative = RepoPaths.Relative(file);
                    if (!expected.Contains(relative))
                    {
                        stray.Add(relative);
                    }
                }
            }

            Assert.That(stray, Is.Empty);
            Assert.That(Outputs.Count, Is.EqualTo(49), "9 Windows + 24 Android + 2 arranque + 1 iOS + 3 launch + 7 macOS + 2 hojas + el .ico (It. 15)");
        }

        [Test, Category("AC-APP-16-08")]
        public void PathsAreRelativeAndUnique()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var output in Outputs)
            {
                Assert.That(Path.IsPathRooted(output.RelativePath), Is.False, output.RelativePath);
                Assert.That(output.RelativePath, Does.Not.Contain("\\"));
                Assert.That(seen.Add(output.RelativePath), Is.True, "repetido: " + output.RelativePath);
                Assert.That(output.RelativePath.StartsWith(AppIconOutputs.IdentityFolder + "/", StringComparison.Ordinal)
                            || output.RelativePath.StartsWith(AppIconOutputs.SnapshotsFolder + "/CAP-0", StringComparison.Ordinal), Is.True, output.RelativePath);
            }
        }

        [Test, Category("AC-APP-16-08")]
        public void GitAttributesKeepThemOutOfLfs()
        {
            // QA-G14-16: los PNG del ícono y CAP-01/CAP-02 van sin LFS (ci-core los compara byte a byte), después de la regla de *.png.
            var text = File.ReadAllText(RepoPaths.Combine(".gitattributes")).Replace("\r\n", "\n");
            var lfs = text.IndexOf("*.png filter=lfs", StringComparison.Ordinal);
            var identity = text.IndexOf("Assets/RowMate/Art/Identity/**/*.png -filter -diff binary", StringComparison.Ordinal);
            var sheets = text.IndexOf("docs/migration/iterations/14-pulido-plataformas/snapshots/CAP-0[12].png -filter -diff binary", StringComparison.Ordinal);
            Assert.That(lfs, Is.GreaterThanOrEqualTo(0));
            Assert.That(identity, Is.GreaterThan(lfs));
            Assert.That(sheets, Is.GreaterThan(lfs));
        }

        public static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(64);
                foreach (var b in hash)
                {
                    builder.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private static bool Same(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }

    // IH · El SHA-256 fijo de icon-16 e icon-1024 (QA-G14-05). Un cambio a propósito del ícono actualiza los dos en el mismo cambio.
    [TestFixture]
    public sealed class AppIconHashesTests
    {
        public const string Icon16 = "0862a327480a041a498eabb59f7360d1ebecbd30fbb1cc506200bb83ea41911a";
        public const string Icon1024 = "93f3eb7db1205503a874f12fb6060c4c1f136d3aef95516d7232cc1c680089b5";

        [Test, Category("AC-APP-16-08")]
        public void FixedHashes()
        {
            Assert.That(AppIconAssetsAreCurrentTests.Sha256(AppIconRaster.Render(AppIconVariant.Plate, 16).ToPng()), Is.EqualTo(Icon16), "icon-16.png");
            Assert.That(AppIconAssetsAreCurrentTests.Sha256(AppIconRaster.Render(AppIconVariant.Plate, 1024).ToPng()), Is.EqualTo(Icon1024), "icon-1024.png");
        }
    }
}
