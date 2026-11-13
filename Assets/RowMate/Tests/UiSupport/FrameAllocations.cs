using System;
using System.Collections.Generic;
using RowMate.Presentation.Workout;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace RowMate.Tests.UiSupport
{
    // Las asignaciones del GC en CUADROS REALES, con el repintado adentro (05-series-parciales/02-tech-design §3.6, §4.5; QA-G5T-03;
    // DT5-18). GcAllocations.During mide una acción sincrónica y no ve el generateVisualContent, que Unity corre en su loop entre dos
    // `yield return null`: con los paneles de la It. 5 casi todo el trabajo de cada cuadro es repintado. Esto mide, en cada cuadro
    // real, las dos partes de lo que hace la app:
    // - Step: el C# del cuadro (el programador, el MotionDriver, Apply, los callbacks), como GcAllocations.During;
    // - Draws: el generateVisualContent de cada elemento observado (los paneles, por SeriesPanelView.DrawObserver; un elemento de
    //   prueba, con BeginDraw/EndDraw), con el Recorder de "GC.Alloc" del hilo principal prendido solo durante el dibujo.
    // Lo que midió la sonda S-B5 de WP-B0 y la primera corrida de HBT: un contador por cuadro entero (el Recorder entre dos `yield`, o
    // "GC Allocated In Frame") no sirve en batchmode: el corredor de pruebas y lo que dejan las pruebas anteriores asignan miles de
    // veces por cuadro y el control positivo (un byte[64] por repintado) se pierde en esa variación. Medido solo adentro del dibujo y
    // del paso, el control positivo da 1 por repintado y lo limpio da 0, sin línea de base que restar (FrameAllocationsTests).
    // Bytes ("GC Allocated In Frame", todos los hilos) queda solo como dato.
    // It. 16 (#78, #99; 16-tests-intermitentes/02-tech-design): cada tramo se cuenta con GcAllocCounter (un ProfilerRecorder de "GC.Alloc"
    // con CollectOnlyOnCurrentThread), no con el Recorder de UnityEngine.Profiling con FilterToCurrentThread, que a veces se llevaba UNA
    // muestra de otro hilo que asignaba justo al prender o apagar la medida (el intermitente de BleAllocationTests). El Recorder de antes
    // sigue al lado solo como testigo (Foreign: lo que contó de más), sin entrar en ninguna cuenta.
    public sealed class FrameAllocations : IDisposable
    {
        private readonly GcAllocCounter _counter;
        private readonly GcAllocCounter _actionCounter;
        private readonly Recorder _legacy;
        private readonly List<SeriesPanelView> _panels = new List<SeriesPanelView>();
        private readonly Action<bool> _observer;
        private ProfilerRecorder _bytes;
        private bool _stopped;
        private bool _inDraw;

        private FrameAllocations()
        {
            _counter = new GcAllocCounter();
            _actionCounter = new GcAllocCounter();
            _legacy = Recorder.Get("GC.Alloc");
            _legacy.enabled = false;
            _observer = begin =>
            {
                if (begin)
                {
                    BeginDraw();
                }
                else
                {
                    EndDraw();
                }
            };
            _bytes = StartBytes();
        }

        // Cuadros muestreados (llamadas a Sample).
        public int Frames { get; private set; }

        // Asignaciones adentro de los dibujos observados y cuántos dibujos hubo.
        public int DrawAllocations { get; private set; }

        public int Draws { get; private set; }

        // Asignaciones adentro de los pasos (Step).
        public int StepAllocations { get; private set; }

        public int Total => DrawAllocations + StepAllocations + PanelAllocations;

        // QA-V6a-R2-01: lo que UI Toolkit hace por el panel en el cuadro FUERA de los dibujos (el recálculo de estilos con los estilos
        // en línea, el layout, los eventos de geometría y el armado de las mallas en las páginas de UIR), con PanelStep. Es lo que el
        // Profiler del build mostró asignando en cada cuadro (UIElements.UpdateStyle → InlineStyleAccess.ApplyInlineStyles y
        // UIElements.UpdateRenderData → UIRenderDevice.Allocate) y que ni el paso ni los dibujos veían.
        public int PanelAllocations { get; private set; }

        public int PanelSteps { get; private set; }

        public int PanelFramesWithAllocations { get; private set; }

        private Action? _panelStep;
        private bool _inPanel;

        // El Update del panel (estilos, layout, eventos) y su UpdateForRepaint (el repintado con los generateVisualContent y las
        // mallas de UIR), adentro de la medida; el cuadro real después no encuentra nada sucio. Los delegados se arman una vez con
        // reflexión (son internos de UI Toolkit): sin ellos, PanelStep no hace nada y PanelSteps queda en 0 (la prueba lo afirma).
        private void BindPanel(IPanel? panel)
        {
            if (panel == null)
            {
                return;
            }

            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            Action? update = null;
            Action? repaint = null;
            for (var type = panel.GetType(); type != null; type = type.BaseType)
            {
                foreach (var method in type.GetMethods(flags | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    if (method.GetParameters().Length != 0 || method.ReturnType != typeof(void) || method.IsAbstract)
                    {
                        continue;
                    }

                    if (update == null && method.Name == "Update")
                    {
                        update = (Action)Delegate.CreateDelegate(typeof(Action), panel, method);
                    }
                    else if (repaint == null && method.Name == "UpdateForRepaint")
                    {
                        repaint = (Action)Delegate.CreateDelegate(typeof(Action), panel, method);
                    }
                }
            }

            if (update != null && repaint != null)
            {
                _panelStep = update + repaint;
            }
        }

        // Después del paso del cuadro: el trabajo de UI Toolkit del panel, medido (los dibujos observados, aparte).
        public void PanelStep()
        {
            var step = _panelStep;
            if (step == null)
            {
                return;
            }

            if (_stopped)
            {
                step();
                return;
            }

            var before = PanelAllocations;
            _inPanel = true;
            StartRecorder();
            AllocationProfile.Measured.Begin();
            try
            {
                step();
            }
            finally
            {
                AllocationProfile.Measured.End();
                if (!_inDraw)
                {
                    PanelAllocations += StopRecorder();
                }

                _inPanel = false;
            }

            PanelSteps++;
            if (PanelAllocations > before)
            {
                PanelFramesWithAllocations++;
            }
        }

        // It. 6a (06a-escena-lago/02-tech-design §4.4; QA-G6aT-06): el Tick de la escena (la sesión de Core, SceneDirector, los streamers
        // y las matrices: la cuenta "escena") y el pedido de render (SubmitRenderRequest, que dibuja adentro de la llamada: la cuenta
        // "URP"), con WatchScene. AC-SCN-01-12 pide 0 en las dos.
        public int SceneAllocations { get; private set; }

        public int RenderAllocations { get; private set; }

        public int SceneTicks { get; private set; }

        public int Renders { get; private set; }

        private RowMate.Scene.SceneHost? _scene;
        private bool _inScene;

        // Observa el Tick y el dibujo de la escena hasta Stop (el host tiene que estar Ready: el dibujo es del mundo armado).
        public static FrameAllocations WatchScene(RowMate.Scene.SceneHost host)
        {
            var allocations = new FrameAllocations { _scene = host };
            host.TickObserver = allocations.OnSceneTick;
            var renderer = host.World?.Renderer;
            if (renderer != null)
            {
                renderer.BeginRender = allocations.OnBeginRender;
                renderer.EndRender = allocations.OnEndRender;
            }

            return allocations;
        }

        private void OnSceneTick(bool begin)
        {
            if (_stopped)
            {
                return;
            }

            if (begin)
            {
                _inScene = true;
                StartRecorder();
                return;
            }

            SceneAllocations += StopRecorder();
            SceneTicks++;
            _inScene = false;
        }

        private void OnBeginRender()
        {
            if (_stopped)
            {
                return;
            }

            if (_inScene)
            {
                SceneAllocations += StopRecorder();
            }

            StartRecorder();
        }

        private void OnEndRender()
        {
            if (_stopped)
            {
                return;
            }

            RenderAllocations += StopRecorder();
            Renders++;
            if (_inScene)
            {
                StartRecorder();
            }
        }

        private void StartRecorder()
        {
            _legacy.enabled = false;
            _legacy.FilterToCurrentThread();
            _legacy.enabled = true;
            _counter.Begin();
        }

        private int StopRecorder()
        {
            var count = _counter.End();
            _legacy.enabled = false;
            _legacy.CollectFromAllThreads();
            var legacy = _legacy.sampleBlockCount;
            if (legacy > count)
            {
                Foreign += legacy - count;
            }

            return count;
        }

        // El paso y el reloj (como GcAllocations.During), con su propio contador: un dibujo que corriera adentro del paso no lo corta.
        private int MeasureAction(Action action)
        {
            var count = 0;
            _legacy.enabled = false;
            _legacy.FilterToCurrentThread();
            _legacy.enabled = true;
            _actionCounter.Begin();
            try
            {
                action();
            }
            finally
            {
                count = _actionCounter.End();
                _legacy.enabled = false;
                _legacy.CollectFromAllThreads();
                var legacy = _legacy.sampleBlockCount;
                if (legacy > count)
                {
                    Foreign += legacy - count;
                }
            }

            return count;
        }

        // Testigo (It. 16): las muestras que el Recorder de antes contó de más en los tramos de esta medida (asignaciones de otro hilo).
        // No entra en Total: queda en ToString para ver en el registro que el intermitente de antes habría fallado.
        public int Foreign { get; private set; }

        // Bytes de "GC Allocated In Frame" sumados por cuadro (solo como dato: es ruido del Editor y del corredor).
        public long Bytes { get; private set; }

        public static FrameAllocations Start()
        {
            return new FrameAllocations();
        }

        // Observa el dibujo de los paneles (SeriesPanelView.DrawObserver) hasta Stop.
        public static FrameAllocations Watch(params SeriesPanelView[] panels)
        {
            var allocations = new FrameAllocations();
            foreach (var panel in panels)
            {
                panel.DrawObserver = allocations._observer;
                allocations._panels.Add(panel);
            }

            return allocations;
        }

        // QA-V6a-R2-01 (06a-escena-lago/02-tech-design, "Cambios por verificación (ronda 2)"): TODOS los generateVisualContent del árbol
        // (el HUD entero: la fila de píldoras, la cuenta grande con su placa, Próximo, las tarjetas, los paneles…), cada uno envuelto
        // entre BeginDraw y EndDraw, con la cuenta por elemento (AllocatingElements dice cuál asignó). Los envoltorios se arman acá
        // (fuera de la medida) y Stop devuelve los delegados de antes.
        public static FrameAllocations WatchTree(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var allocations = new FrameAllocations();
            var found = new List<VisualElement>();
            root.Query<VisualElement>().ForEach(element =>
            {
                if (element.generateVisualContent != null)
                {
                    found.Add(element);
                }
            });
            allocations._treeElements = found.ToArray();
            allocations._treeOriginals = new Action<MeshGenerationContext>[found.Count];
            allocations._treeWrappers = new Action<MeshGenerationContext>[found.Count];
            allocations._treeAllocations = new int[found.Count];
            allocations._treeDraws = new int[found.Count];
            for (var i = 0; i < found.Count; i++)
            {
                var index = i;
                var original = found[i].generateVisualContent;
                allocations._treeOriginals[i] = original;
                Action<MeshGenerationContext> wrapper = context =>
                {
                    var before = allocations.DrawAllocations;
                    allocations.BeginDraw();
                    AllocationProfile.Measured.Begin();
                    try
                    {
                        original(context);
                    }
                    finally
                    {
                        AllocationProfile.Measured.End();
                        allocations.EndDraw();
                        allocations._treeAllocations[index] += allocations.DrawAllocations - before;
                        allocations._treeDraws[index]++;
                    }
                };
                allocations._treeWrappers[i] = wrapper;
                found[i].generateVisualContent = wrapper;
            }

            allocations.BindPanel(root.panel);
            return allocations;
        }

        private VisualElement[]? _treeElements;
        private Action<MeshGenerationContext>[]? _treeOriginals;
        private Action<MeshGenerationContext>[]? _treeWrappers;
        private int[]? _treeAllocations;
        private int[]? _treeDraws;

        // Los elementos envueltos por WatchTree (0 sin WatchTree).
        public int WatchedElements => _treeElements?.Length ?? 0;

        // Los dibujos de los elementos envueltos con ese nombre (-1 si no hay ninguno).
        public int DrawsOf(string elementName)
        {
            if (_treeElements == null || _treeDraws == null)
            {
                return -1;
            }

            var draws = -1;
            for (var i = 0; i < _treeElements.Length; i++)
            {
                if (_treeElements[i].name == elementName)
                {
                    draws = Math.Max(draws, 0) + _treeDraws[i];
                }
            }

            return draws;
        }

        // Los elementos que asignaron en su dibujo, con su cuenta y sus dibujos ("" si ninguno).
        public string AllocatingElements()
        {
            if (_treeElements == null || _treeAllocations == null || _treeDraws == null)
            {
                return "";
            }

            var text = new System.Text.StringBuilder();
            for (var i = 0; i < _treeElements.Length; i++)
            {
                if (_treeAllocations[i] > 0)
                {
                    var element = _treeElements[i];
                    text.Append(string.IsNullOrEmpty(element.name) ? element.GetType().Name : element.name).Append(": ").Append(_treeAllocations[i])
                        .Append(" en ").Append(_treeDraws[i]).Append(" dibujos; ");
                }
            }

            return text.ToString();
        }

        // El reloj de prueba (las lecturas del remo y el segundo del motor: lo que se arma a 1 Hz, que §20.2 permite) medido aparte:
        // cuántos cuadros asignaron en él (lo de 1 Hz da uno o dos por segundo; algo por cuadro, casi todos).
        public void Clock(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_stopped)
            {
                action();
                return;
            }

            _clockUsed = true;
            int count;
            AllocationProfile.Measured.Begin();
            try
            {
                count = MeasureAction(action);
            }
            finally
            {
                AllocationProfile.Measured.End();
            }

            ClockAllocations += count;
            if (count > 0)
            {
                ClockFramesWithAllocations++;
            }
        }

        public int ClockAllocations { get; private set; }

        public int ClockFramesWithAllocations { get; private set; }

        private bool _clockUsed;

        // El dibujo de un elemento de prueba (el control positivo): BeginDraw al empezar su generateVisualContent, EndDraw al final.
        public void BeginDraw()
        {
            if (_stopped || _inDraw)
            {
                return;
            }

            if (_inPanel)
            {
                PanelAllocations += StopRecorder();
            }

            _inDraw = true;
            StartRecorder();
        }

        public void EndDraw()
        {
            if (!_inDraw)
            {
                return;
            }

            _inDraw = false;
            DrawAllocations += StopRecorder();
            Draws++;
            if (_inPanel)
            {
                StartRecorder();
            }
        }

        // El C# del cuadro (H.Step()), como GcAllocations.During.
        public void Step(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_stopped)
            {
                action();
                return;
            }

            AllocationProfile.Measured.Begin();
            try
            {
                StepAllocations += MeasureAction(action);
            }
            finally
            {
                AllocationProfile.Measured.End();
            }
        }

        // Después de cada `yield return null`: el cuadro que terminó.
        public void Sample()
        {
            if (_stopped)
            {
                return;
            }

            Frames++;
            if (_bytes.Valid)
            {
                Bytes += _bytes.LastValue;
            }
        }

        public void Stop()
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            if (_scene != null)
            {
                _scene.TickObserver = null;
                var renderer = _scene.World?.Renderer;
                if (renderer != null)
                {
                    renderer.BeginRender = null;
                    renderer.EndRender = null;
                }
            }

            if (_treeElements != null && _treeOriginals != null && _treeWrappers != null)
            {
                for (var i = 0; i < _treeElements.Length; i++)
                {
                    if (_treeElements[i].generateVisualContent == _treeWrappers[i])
                    {
                        _treeElements[i].generateVisualContent = _treeOriginals[i];
                    }
                }
            }

            foreach (var panel in _panels)
            {
                if (panel.DrawObserver == _observer)
                {
                    panel.DrawObserver = null;
                }
            }

            if (_bytes.Valid)
            {
                _bytes.Stop();
            }

            _counter.Dispose();
            _actionCounter.Dispose();
        }

        public void Dispose()
        {
            Stop();
            _bytes.Dispose();
        }

        public override string ToString()
        {
            return Total + " asignaciones (" + DrawAllocations + " en " + Draws + " dibujos y " + StepAllocations + " en los pasos; escena " + SceneAllocations
                   + " en " + SceneTicks + " Tick, URP " + RenderAllocations + " en " + Renders + " dibujos) en " + Frames
                   + " cuadros (" + Bytes + " B de GC Allocated In Frame, todo el proceso)"
                   + (_treeElements != null ? "; " + _treeElements.Length + " elementos con dibujo propio" + (AllocatingElements().Length > 0 ? " (asignan: " + AllocatingElements() + ")" : "") : "")
                   + (_panelStep != null ? "; el panel (estilos, layout y mallas) " + PanelAllocations + " en " + PanelSteps + " pasos, " + PanelFramesWithAllocations + " con alguna" : "")
                   + (_clockUsed ? "; reloj de prueba " + ClockAllocations + " en " + ClockFramesWithAllocations + " cuadros" : "")
                   + "; ajenas que el Recorder de antes habría contado: " + Foreign;
        }

        private static ProfilerRecorder StartBytes()
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (var handle in handles)
            {
                if (ProfilerRecorderHandle.GetDescription(handle).Name == "GC Allocated In Frame")
                {
                    var recorder = new ProfilerRecorder(handle, 2, ProfilerRecorderOptions.Default);
                    recorder.Start();
                    return recorder;
                }
            }

            return default;
        }
    }
}
