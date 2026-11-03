using System.Collections;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // SKT · El teclado con los paneles (05-series-parciales/02-tech-design §3.5; AC-WKT-18-12, AC-APP-05-07; QA-G5T-09; DD5-22): el anillo
    // del inmersivo Terminar → Pausa → parciales → repeticiones (si se ve) → Terminar; Enter (NavigationSubmitEvent) y Espacio (un
    // KeyDownEvent de verdad) abren o cierran una sola vez y guardan una sola vez; el anillo de foco en dp de ventana; el panel
    // enfocado que desaparece; con un diálogo, Tab no llega.
    public sealed class SeriesKeyboardTests : SeriesUiTestBase
    {
        [UnityTest, Category("AC-WKT-18-12"), Category("AC-APP-05-07")]
        public IEnumerator TheRingEnterAndSpace()
        {
            var preferences = new RecordingPreferences();
            CreateSeries(DeviceProfile.W1280, configure: o => o.Preferences = preferences);
            yield return StartAt("Series 600", 60.2);
            H.PressTab();
            Assert.That(H.Focused, Is.SameAs(Hud.Controls.Finish), "el primer Tab, en Terminar");
            H.PressTab();
            Assert.That(H.Focused, Is.SameAs(Hud.Controls.Pause), "Pausa");
            H.PressTab();
            Assert.That(H.Focused, Is.SameAs(Laps), "parciales");
            H.PressTab();
            Assert.That(H.Focused, Is.SameAs(Reps), "repeticiones (se ve: estamos en la serie)");
            H.PressTab();
            Assert.That(H.Focused, Is.SameAs(Hud.Controls.Finish), "vuelve a Terminar");
            H.PressTab(true);
            Assert.That(H.Focused, Is.SameAs(Reps), "Mayús+Tab al revés");

            // Enter: una vez y una escritura.
            H.PressTab(true);
            Assert.That(H.Focused, Is.SameAs(Laps));
            yield return Frames();
            Assert.That(Laps.HasFocusRing, Is.True, "el anillo de foco en el modo teclado");
            var writes = preferences.WriteCount;
            H.PressEnter();
            yield return Frames();
            Assert.That(Column.Expanded.Laps, Is.True, "Enter abre");
            Assert.That(preferences.WriteCount - writes, Is.EqualTo(1), "una sola escritura");

            // Espacio (KeyDownEvent con KeyCode.Space, como RmButton): una vez.
            writes = preferences.WriteCount;
            H.PressKey(KeyCode.Space, ' ');
            yield return Frames();
            Assert.That(Column.Expanded.Laps, Is.False, "Espacio cierra");
            Assert.That(preferences.WriteCount - writes, Is.EqualTo(1), "una sola escritura por tecla");
        }

        [UnityTest, Category("AC-WKT-18-12")]
        public IEnumerator TheFocusRingFollowsTheGlassInWindowDp()
        {
            CreateSeries(DeviceProfile.W1280, laps: true, reps: true);
            yield return StartAt("Series 600", 340.2);
            H.PressTab();
            H.PressTab();
            H.PressTab();
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(Laps));
            Assert.That(Laps.HasFocusRing, Is.True);
            // CAP-26: el anillo blanco de 2 dp por fuera, de −4 a −2 dp del borde visual (x 18,4-190,8 e y 316,2-461,0).
            var glass = VisualRect(Laps);
            Assert.That(glass.xMin - 4, Is.EqualTo(18.4f).Within(0.5f));
            Assert.That(glass.yMin - 4, Is.EqualTo(316.2f).Within(0.5f));
            Assert.That(glass.yMax + 4, Is.EqualTo(461.0f).Within(0.5f));
            // El halo (de −8 a −4) no toca SPM ni el panel de abajo.
            Assert.That(glass.yMin - 8, Is.GreaterThan(H.BoundsOf("hud-spm").yMax), "el halo no toca SPM");
            Assert.That(glass.yMax + 8, Is.LessThan(VisualRect(Reps).yMin), "ni repeticiones");
        }

        // LM5-02 (verificación de la 5, ronda 1): en ColumnScroll (TH, L640) con los dos abiertos (CAP-16), repeticiones empieza
        // entero debajo del piso. El Tab que llega a un panel lo trae a la vista (la columna se desplaza con spring.snappy hasta su
        // cabecera) y el anillo se recorta a la ventana de la columna: nunca baja sobre la banda ni queda un foco invisible.
        [UnityTest, Category("AC-WKT-18-12"), Category("AC-WKT-18-16"), Category("AC-APP-05-07")]
        public IEnumerator InColumnScrollTheFocusedPanelComesIntoView()
        {
            foreach (var profile in new[] { DeviceProfile.TH, DeviceProfile.L640 })
            {
                CreateSeries(profile, laps: true, reps: true);
                yield return StartAt("Series 600", 380.2);
                Assert.That(Column.Column.Rule, Is.EqualTo(RowMate.Core.Design.SeriesFitRule.ColumnScroll), profile.Code);
                var floor = (float)Column.Column.Floor;
                var ceiling = (float)Column.Column.Top(Column.SpmHasTarget);
                var sp = (float)Column.Column.Scale;
                Assert.That(Reps.worldBound.yMin, Is.GreaterThan(floor - 1), profile.Code + ": repeticiones debajo del piso");
                for (var i = 0; i < 4; i++)
                {
                    H.PressTab();
                }

                Assert.That(H.Focused, Is.SameAs(Reps), profile.Code + ": Terminar, Pausa, parciales, repeticiones");
                yield return Settle();
                Assert.That(Reps.HasFocusRing, Is.True, profile.Code);
                var glass = VisualRect(Reps);
                Assert.That(glass.yMin, Is.GreaterThanOrEqualTo(ceiling - 0.5f), profile.Code + ": la cabecera, debajo del techo (" + Describe(Reps) + ")");
                Assert.That(glass.yMin + 34 * sp, Is.LessThanOrEqualTo(floor + 0.5f), profile.Code + ": y arriba del piso (se trajo a la vista)");
                Assert.That(Column.ScrollBase, Is.LessThanOrEqualTo(Column.ScrollMaxBase + 0.05), profile.Code + ": dentro de su tope");
                var band = H.BoundsOf("hud-band");
                var ring = Reps.FocusRingSpanBase;
                var ringTop = Reps.worldBound.yMin + (float)ring.Top * sp - 8;
                var ringBottom = Reps.worldBound.yMin + (float)ring.Bottom * sp + 8;
                Assert.That(ringBottom, Is.LessThanOrEqualTo(floor + 0.5f), profile.Code + ": el anillo y su halo, recortados al piso");
                Assert.That(ringBottom, Is.LessThan(band.yMin), profile.Code + ": sin tocar la banda");
                Assert.That(ringTop, Is.GreaterThanOrEqualTo(ceiling - 0.5f), profile.Code + ": ni pasar el techo");
                Assert.That(ringBottom - ringTop, Is.GreaterThan(34 * sp), profile.Code + ": rodea al menos la cabecera");

                // Mayús+Tab vuelve a parciales y lo trae a la vista.
                H.PressTab(true);
                Assert.That(H.Focused, Is.SameAs(Laps), profile.Code);
                yield return Settle();
                Assert.That(VisualRect(Laps).yMin, Is.GreaterThanOrEqualTo(ceiling - 0.5f), profile.Code + ": parciales, con su cabecera a la vista");
                ring = Laps.FocusRingSpanBase;
                Assert.That(Laps.worldBound.yMin + (float)ring.Bottom * sp + 8, Is.LessThan(band.yMin), profile.Code + ": su anillo tampoco toca la banda");
                DisposeHost();
            }
        }

        [UnityTest, Category("AC-WKT-18-12"), Category("AC-APP-05-07")]
        public IEnumerator TheFocusedRepsPanelThatLeavesSendsTabToFinish()
        {
            CreateSeries(DeviceProfile.TV);
            yield return StartAt("Series 600", 538.0);
            for (var i = 0; i < 4; i++)
            {
                H.PressTab();
            }

            Assert.That(H.Focused, Is.SameAs(Reps), "repeticiones con el foco");
            yield return AdvanceTo(541.2);
            yield return Settle();
            Assert.That(Column.RepsShown, Is.False, "la serie terminó");
            Assert.That(H.Focused, Is.Not.SameAs(Reps), "perdió el foco antes de irse");
            H.PressTab();
            Assert.That(H.Focused, Is.SameAs(Hud.Controls.Finish), "el siguiente Tab, a Terminar");

            // Con un diálogo abierto, Tab no llega a los paneles.
            yield return Tap("hud-finish");
            var focused = H.Focused;
            H.PressTab();
            Assert.That(H.Focused, Is.Not.SameAs(Laps), "con el diálogo, Tab no llega al panel");
        }
    }
}
