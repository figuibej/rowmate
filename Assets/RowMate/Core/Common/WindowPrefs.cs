using System;
using System.Globalization;
using RowMate.Core.Ports;
using RowMate.Core.Text;

namespace RowMate.Core.Common
{
    // La ventana guardada en prefs.json (It. 15, #74, AC-D15-19; DT15-08): window.x, window.y, window.width, window.height (dp lógicos,
    // enteros, como texto invariante: IPreferences guarda bool y string) y window.maximized. Read da null si falta alguna clave o
    // no es un entero (el llamador abre centrada, DV-130). Write escribe las 5 claves.
    public static class WindowPrefs
    {
        public const string XKey = "window.x";
        public const string YKey = "window.y";
        public const string WidthKey = "window.width";
        public const string HeightKey = "window.height";
        public const string MaximizedKey = "window.maximized";

        public static SavedWindow? Read(IPreferences prefs)
        {
            if (prefs == null)
            {
                throw new ArgumentNullException(nameof(prefs));
            }

            if (!TryInt(prefs, XKey, out var x) || !TryInt(prefs, YKey, out var y) || !TryInt(prefs, WidthKey, out var width)
                || !TryInt(prefs, HeightKey, out var height) || width <= 0 || height <= 0)
            {
                return null;
            }

            prefs.TryGetBool(MaximizedKey, out var maximized);
            return new SavedWindow(x, y, width, height, maximized);
        }

        public static void Write(IPreferences prefs, SavedWindow window)
        {
            if (prefs == null)
            {
                throw new ArgumentNullException(nameof(prefs));
            }

            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            prefs.SetString(XKey, Fmt.Integer(window.X));
            prefs.SetString(YKey, Fmt.Integer(window.Y));
            prefs.SetString(WidthKey, Fmt.Integer(window.Width));
            prefs.SetString(HeightKey, Fmt.Integer(window.Height));
            prefs.SetBool(MaximizedKey, window.Maximized);
        }

        private static bool TryInt(IPreferences prefs, string key, out int value)
        {
            value = 0;
            return prefs.TryGetString(key, out var text)
                && int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }
    }
}
