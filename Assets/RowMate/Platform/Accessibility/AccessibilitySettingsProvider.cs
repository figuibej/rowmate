using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Platform.Accessibility
{
    // IAccessibilitySettings de la app (ARCHITECTURE §13.2, AC-APP-05-05, AC-APP-05-08): escala de letra y "reducir
    // movimiento" del sistema. Se lee al crear y en Refresh() (AppRoot.OnApplicationFocus); el observable avisa solo si algo
    // cambió. Windows y Android leen el sistema; en el Editor mandan los menús RowMate/Simular … (DeveloperOverrides).
    public sealed class AccessibilitySettingsProvider : IAccessibilitySettings
    {
        private readonly ObservableValue<AccessibilitySnapshot> _current;

        public AccessibilitySettingsProvider(IAppLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _current = new ObservableValue<AccessibilitySnapshot>(ReadNow(), logger);
            Current = new ReadOnlyObservable<AccessibilitySnapshot>(_current);
        }

        public IReadOnlyObservable<AccessibilitySnapshot> Current { get; }

        public void Refresh()
        {
            _current.Value = ReadNow();
        }

        public static AccessibilitySnapshot ReadNow()
        {
#if UNITY_EDITOR
            var simulatedScale = DeveloperOverrides.TextScale;
            return new AccessibilitySnapshot(simulatedScale > 0 ? simulatedScale : 1.0, DeveloperOverrides.ReduceMotion);
#elif UNITY_STANDALONE_WIN
            return new AccessibilitySnapshot(WindowsAccessibility.TextScale(), WindowsAccessibility.ReduceMotion());
#elif UNITY_ANDROID
            return new AccessibilitySnapshot(AndroidAccessibility.TextScale(), AndroidAccessibility.ReduceMotion());
#else
            return AccessibilitySnapshot.Default;
#endif
        }
    }
}
