namespace RowMate.App.Device
{
    // El acceso al diagnóstico oculto (AC-BLE-12-01; device_screen.dart:52-74, :124-134; Q-37, DV-104), sin Unity: la vista le pasa
    // la posición del desplazamiento de la vista conectada y los toques de la cabecera, con la hora.
    // - Desplazamiento (ds:63-74): cada vez que la posición pasa 20 dp por debajo de la última marca, cuenta una y la marca se
    //   mueve; si sube más de 20, solo se mueve la marca. Con 3, aparece "Show BLE debug"; tocarlo abre el panel.
    // - Presión de la cabecera ≥ 0,5 s: abre el panel directo, sin el botón (Progress da la capa de estado de 0 a 1 mientras dura).
    // - El panel queda abierto mientras dure la conexión: la vista llama Reset al salir de la vista conectada (al reconectar hay
    //   que repetir el gesto).
    public sealed class DebugRevealGesture
    {
        public const double ScrollStepDp = 20;
        public const int ScrollsToReveal = 3;
        public const double LongPressSeconds = 0.5;

        private double _lastPosition;
        private double? _pressStart;

        public int ScrollCount { get; private set; }

        public bool PanelOpen { get; private set; }

        // ds:124: el botón aparece con 3 desplazamientos y mientras el panel no esté abierto.
        public bool ShowButton => ScrollCount >= ScrollsToReveal && !PanelOpen;

        public bool Pressing => _pressStart.HasValue;

        // ds:63-74
        public void OnScroll(double position)
        {
            if (position > _lastPosition + ScrollStepDp)
            {
                ScrollCount++;
                _lastPosition = position;
            }
            else if (position < _lastPosition - ScrollStepDp)
            {
                _lastPosition = position;
            }
        }

        // ds:127: tocar "Show BLE debug".
        public void OpenPanel()
        {
            PanelOpen = true;
        }

        public void PressStart(double now)
        {
            _pressStart = now;
        }

        // En cada cuadro mientras dura la presión: abre el panel al llegar a 0,5 s. Devuelve true el cuadro en que lo abre.
        public bool PressUpdate(double now)
        {
            if (!_pressStart.HasValue || now - _pressStart.Value < LongPressSeconds - 1e-9)
            {
                return false;
            }

            _pressStart = null;
            var opened = !PanelOpen;
            PanelOpen = true;
            return opened;
        }

        // Soltar: si llegó a 0,5 s, abre (por si no hubo cuadro en el medio); si no, se cancela.
        public bool PressEnd(double now)
        {
            var opened = PressUpdate(now);
            _pressStart = null;
            return opened;
        }

        // 0 a 1 en los 500 ms de la presión (la capa de estado de la cabecera sube de 0 al 8 %, 00-arte §5.1).
        public double PressProgress(double now)
        {
            if (!_pressStart.HasValue)
            {
                return 0;
            }

            var p = (now - _pressStart.Value) / LongPressSeconds;
            return p < 0 ? 0 : p > 1 ? 1 : p;
        }

        public void Reset()
        {
            ScrollCount = 0;
            _lastPosition = 0;
            _pressStart = null;
            PanelOpen = false;
        }
    }
}
