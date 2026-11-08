using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Ble.Native;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // Las firmas JNI de RmBle (10a 02-tech-design §4.3, §4.5; QA-G10a-05): la tabla de Core (la única fuente de los literales JNI
    // de AndroidBleApi) es igual a native/android/golden/jni-signatures.txt, que native/android/check.ps1 regenera con javap -s
    // de la clase compilada y compara (código 4 si difiere). Acá, sin JDK: la tabla contra el archivo y contra las funciones
    // @JvmStatic del object Kotlin.
    [TestFixture]
    public sealed class JniSignatureTests
    {
        private const string GoldenFile = "native/android/golden/jni-signatures.txt";
        private const string KotlinFile = "Assets/Plugins/Android/RowMateBle/RmBle.kt";

        private static List<string> Table()
        {
            return RmBleJniSignatures.All.Select(e => e.Name + " " + e.Signature).OrderBy(s => s, StringComparer.Ordinal).ToList();
        }

        [Test]
        public void TheTableMatchesTheJavapGolden()
        {
            var path = RepoPaths.Combine(GoldenFile);
            Assert.That(File.Exists(path), Is.True, GoldenFile + " no está (native/android/check.ps1 -Update)");
            var golden = File.ReadAllLines(path)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal))
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            Assert.That(Table(), Is.EqualTo(golden));
        }

        [Test]
        public void NamesAreUniqueAndTheClassIsTheKotlinObject()
        {
            var names = RmBleJniSignatures.All.Select(e => e.Name).ToList();
            Assert.That(names, Is.Unique);
            Assert.That(names.Count, Is.EqualTo(24));
            Assert.That(RmBleJniSignatures.ClassName, Is.EqualTo("com/rowmate/ble/RmBle"));

            var kotlin = File.ReadAllText(RepoPaths.Combine(KotlinFile));
            Assert.That(kotlin, Does.Contain("package com.rowmate.ble"));
            Assert.That(kotlin, Does.Contain("object RmBle"));
        }

        [Test]
        public void EveryJvmStaticFunctionIsInTheTable()
        {
            // RmBle no puede tener otra función pública: javap la mostraría y check.ps1 fallaría.
            var kotlin = File.ReadAllText(RepoPaths.Combine(KotlinFile));
            var declared = Regex.Matches(kotlin, @"@JvmStatic\s+fun\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            var table = RmBleJniSignatures.All.Select(e => e.Name).OrderBy(s => s, StringComparer.Ordinal).ToList();
            Assert.That(declared, Is.EqualTo(table));
            var publicFunctions = Regex.Matches(kotlin, @"(?m)^\s*(public\s+)?fun\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(").Count;
            Assert.That(publicFunctions, Is.EqualTo(declared.Count), "RmBle.kt tiene una función pública sin @JvmStatic");
            Assert.That(Regex.IsMatch(kotlin, @"(?m)^\s*internal\s+(fun|val|var)\s"), Is.False, "un miembro internal sale público en javap");
        }
    }
}
