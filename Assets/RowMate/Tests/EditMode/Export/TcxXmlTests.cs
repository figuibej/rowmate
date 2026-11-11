using System.Linq;
using System.Xml;
using NUnit.Framework;
using RowMate.Core.Export;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Export
{
    // TX · Texto dentro del TCX (AC-STR-09-01, -09; legacy: _escapeXml, tcx_builder.dart:119-126; DV-120).
    [TestFixture]
    public sealed class TcxXmlTests
    {
        private const string TcxNamespace = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";

        [Test, Category("AC-STR-09-09")]
        public void EscapesAmpFirst()
        {
            Assert.That(TcxXml.Escape(TcxSeeds.T1RoutineName), Is.EqualTo("Escalera &amp; &lt;Co&gt; &quot;A&quot; &apos;B&apos;"));
            Assert.That(TcxXml.Escape("&lt;"), Is.EqualTo("&amp;lt;"), "el & de una entidad del texto se escapa (primero)");
            Assert.That(TcxXml.Escape("Ñandú 4×500 m"), Is.EqualTo("Ñandú 4×500 m"), "el resto, tal cual");
        }

        [Test, Category("AC-STR-09-09")]
        public void StripsInvalidXmlChars()
        {
            Assert.That(TcxXml.Escape("a\u0000b\u0001c\u001Fd\u0008e"), Is.EqualTo("abcde"), "controles");
            Assert.That(TcxXml.Escape("t\tn\nr\r"), Is.EqualTo("t\tn\nr\r"), "tab, \\n y \\r quedan");
            Assert.That(TcxXml.Escape("x\uFFFEy\uFFFFz"), Is.EqualTo("xyz"), "U+FFFE y U+FFFF");
            Assert.That(TcxXml.Escape("a\uD800b\uDC00c"), Is.EqualTo("abc"), "sustitutos sueltos");
            Assert.That(TcxXml.Escape("remo \uD83D\uDEA3"), Is.EqualTo("remo \uD83D\uDEA3"), "un par válido queda");
            Assert.That(TcxXml.Escape("\u007F\u0085\uE000\uFFFD"), Is.EqualTo("\u007F\u0085\uE000\uFFFD"), "válidos en XML 1.0");
        }

        [Test, Category("AC-STR-09-01")]
        public void ParsesWithXmlReaderThreeNamespacesOneActivity()
        {
            foreach (var seed in new[] { TcxSeeds.T1, TcxSeeds.T3, TcxSeeds.T6, TcxSeeds.ThreeHours })
            {
                var xml = TcxBuilder.Build(seed.Session with { RoutineName = "a\u0001 & <b> \"c\" 'd' \uFFFF" }, seed.Points,
                    new TcxOptions(TestZones.UtcMinus3, true));
                var document = new XmlDocument();
                document.LoadXml(xml);
                var root = document.DocumentElement!;
                Assert.That(root.LocalName, Is.EqualTo("TrainingCenterDatabase"), seed.Name);
                Assert.That(root.NamespaceURI, Is.EqualTo(TcxNamespace));
                var declared = root.Attributes.Cast<XmlAttribute>().Where(a => a.Name == "xmlns" || a.Prefix == "xmlns").Select(a => a.Name + "=" + a.Value);
                Assert.That(declared, Is.EqualTo(new[]
                {
                    "xmlns=" + TcxNamespace, "xmlns:xsi=http://www.w3.org/2001/XMLSchema-instance",
                    "xmlns:ns3=http://www.garmin.com/xmlschemas/ActivityExtension/v2",
                }), seed.Name + ": exactamente los 3 del legacy");
                var ns = new XmlNamespaceManager(document.NameTable);
                ns.AddNamespace("t", TcxNamespace);
                var activities = root.SelectNodes("t:Activities/t:Activity", ns)!;
                Assert.That(activities.Count, Is.EqualTo(1), seed.Name);
                Assert.That(((XmlElement)activities[0]!).GetAttribute("Sport"), Is.EqualTo("Other"));
                var notes = root.SelectSingleNode("t:Activities/t:Activity/t:Notes", ns)!;
                Assert.That(notes.InnerText, Is.EqualTo("[SIM] a & <b> \"c\" 'd' "), seed.Name + ": Notes vuelve al texto (sin los inválidos)");
                var children = activities[0]!.ChildNodes.Cast<XmlNode>().Select(n => n.LocalName).ToArray();
                Assert.That(children.First(), Is.EqualTo("Id"));
                Assert.That(children.Last(), Is.EqualTo("Creator"), "orden del esquema TCX v2: Id, Lap+, Notes, Creator (It. 15, AC-D15-15)");
                Assert.That(children[children.Length - 2], Is.EqualTo("Notes"));
                var creator = (XmlElement)activities[0]!.LastChild!;
                Assert.That(creator.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance"), Is.EqualTo("Device_t"));
                Assert.That(creator.SelectSingleNode("t:Name", ns)!.InnerText, Is.EqualTo("RowMate"));
            }
        }
    }
}
