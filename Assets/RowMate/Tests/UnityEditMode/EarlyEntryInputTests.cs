using System;
using NUnit.Framework;
using RowMate.Bootstrap;
using RowMate.Core.Scene;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevel;

namespace RowMate.Tests.UnityEditMode
{
    // EEI · La entrada temprana y el cuadro del toque (#1; 06b-agua-avanzada/02-tech-design §2.1 (2) y (6); DT6b-25, G6b-05; AC-6B-01, -02),
    // con el Input System de verdad (InputTestFixture, como It15GapTests):
    // - el orden del player loop de Unity 6.3: el despacho de UI Toolkit (PreLateUpdate.UIElementsUpdatePanels) va ANTES de
    //   PreLateUpdate.ScriptRunBehaviourLateUpdate (AppRoot.LateUpdate → SceneBridge.Tick); se lee el player loop y se anota;
    // - una unidad pesada nunca comparte el cuadro del toque ni el siguiente: con el mouse o con Enter apretados (y soltados en la misma
    //   actualización) SceneBridge.InputActive da true y el WarmupFrame que arma SceneBridge (Next) no deja correr pesadas ni en ese
    //   cuadro ni en el que sigue, aunque los dos sean quietos. La regla no depende del orden del player loop.
    public sealed class EarlyEntryInputTests
    {
        [Test, Category("AC-6B-02")]
        public void PlayerLoopDispatchesUiBeforeLateUpdate()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            int ui = -1, late = -1;
            foreach (var phase in loop.subSystemList ?? Array.Empty<PlayerLoopSystem>())
            {
                if (phase.type == null || phase.type.Name != "PreLateUpdate")
                {
                    continue;
                }

                var list = phase.subSystemList ?? Array.Empty<PlayerLoopSystem>();
                for (var i = 0; i < list.Length; i++)
                {
                    var name = list[i].type?.Name ?? "";
                    if (name == "UIElementsUpdatePanels")
                    {
                        ui = i;
                    }
                    else if (name == "ScriptRunBehaviourLateUpdate")
                    {
                        late = i;
                    }
                }
            }

            Debug.Log("[EEI] PreLateUpdate: UIElementsUpdatePanels en " + ui + ", ScriptRunBehaviourLateUpdate en " + late);
            Assert.That(ui, Is.GreaterThanOrEqualTo(0), "UIElementsUpdatePanels en PreLateUpdate");
            Assert.That(late, Is.GreaterThanOrEqualTo(0), "ScriptRunBehaviourLateUpdate en PreLateUpdate");
            Assert.That(ui, Is.LessThan(late), "el despacho de la UI va antes del LateUpdate que decide el tramo");
        }

        [Test, Category("AC-6B-01"), Category("AC-6B-02")]
        public void HeavyNeverSharesTheTapFrame()
        {
            var input = new InputTestFixture();
            input.Setup();
            try
            {
                var mouse = InputSystem.AddDevice<Mouse>();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                InputSystem.Update();

                // Workout en reposo, quieto desde hace un cuadro y sin entrada: una pesada puede correr.
                var frame = default(WarmupFrame).Next(true, SceneBridge.InputActive());
                frame = frame.Next(true, SceneBridge.InputActive());
                Assert.That(SceneBridge.InputActive(), Is.False, "sin entrada");
                Assert.That(frame.AllowsHeavy, Is.True, "quieto y sin entrada");

                // (a) Bajar y soltar el mouse en la misma actualización (el toque de Entrenamiento libre).
                input.Press(mouse.leftButton, queueEventOnly: true);
                input.Release(mouse.leftButton, queueEventOnly: true);
                InputSystem.Update();
                var tap = frame.Next(true, SceneBridge.InputActive());
                Assert.That(tap.InputActive, Is.True, "el toque se ve en el cuadro del toque");
                Assert.That(tap.AllowsHeavy, Is.False, "ninguna pesada en el cuadro del toque");
                Assert.That(SceneWarmupPolicy.RunAnother(3, 0, 0, true, tap), Is.False);
                InputSystem.Update();
                var after = tap.Next(true, SceneBridge.InputActive());
                Assert.That(after.AllowsHeavy, Is.False, "ni en el siguiente");
                var later = after.Next(true, SceneBridge.InputActive());
                Assert.That(later.AllowsHeavy, Is.True, "dos cuadros quietos sin entrada después, sí");

                // (b) Enter sobre el botón con foco.
                input.Press(keyboard.enterKey);
                var enter = later.Next(true, SceneBridge.InputActive());
                Assert.That(enter.AllowsHeavy, Is.False, "Enter apretado");
                input.Release(keyboard.enterKey);
                var afterEnter = enter.Next(true, SceneBridge.InputActive());
                Assert.That(afterEnter.AllowsHeavy, Is.False, "el cuadro siguiente a Enter");
            }
            finally
            {
                input.TearDown();
            }
        }
    }
}
