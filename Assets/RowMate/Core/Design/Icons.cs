using System;
using System.Collections.Generic;

namespace RowMate.Core.Design
{
    // Íconos de la app: glifos de "RowMate Icons" (Material Symbols Rounded, commit 10ecc14; ASSET_SLOTS §4.3). Cada
    // constante es el texto del Label (el codepoint, un carácter del área privada); la clase USS .rm-icon elige la fuente de
    // contorno y .rm-icon--filled la rellena (Typography.uss). IconsTests compara cada nombre y codepoint con
    // UI/Icons/MaterialSymbolsRounded[...].codepoints y con el cmap de las dos fuentes versionadas.
    // 42 glifos en la fuente de contorno y 7 en la rellena (timer, favorite y person están en las dos).
    public static class Icons
    {
        // ── Contorno (42) ────────────────────────────────────────────────────────
        public const string Rowing = "";
        public const string FitnessCenter = "";
        public const string Whatshot = "";
        public const string PauseCircle = "";
        public const string AcUnit = "";
        public const string Repeat = "";
        public const string Timer = "";                 // también relleno (pestaña Workout elegida)
        public const string Straighten = "";
        public const string Bolt = "";
        public const string Speed = "";
        public const string LocalFireDepartment = "";
        public const string Schedule = "";
        public const string History = "";
        public const string TrendingUp = "";
        public const string Bluetooth = "";
        public const string BluetoothConnected = "";
        public const string BluetoothDisabled = "";
        public const string BluetoothSearching = "";
        public const string BugReport = "";
        public const string Favorite = "";              // también relleno (corazón del pulsómetro y tarjeta PULSO)
        public const string HeartBroken = "";
        public const string Refresh = "";
        public const string Person = "";                // también relleno (pestaña Perfil elegida)
        public const string CloudUpload = "";
        public const string CloudDownload = "";
        public const string DeleteSweep = "";
        public const string Link = "";
        public const string LinkOff = "";
        public const string Warning = "";
        public const string Error = "";
        public const string ChevronRight = "";
        public const string ExpandMore = "";
        public const string ExpandLess = "";
        public const string Add = "";
        public const string Remove = "";
        public const string Close = "";
        public const string Edit = "";
        public const string Delete = "";
        public const string Build = "";
        public const string Landscape = "";
        public const string ArrowDropUp = "";
        public const string ArrowDropDown = "";

        // ── Solo relleno (4; los otros 3 rellenos son Timer, Favorite y Person) ──
        public const string PlayArrow = "";
        public const string Pause = "";
        public const string CheckCircle = "";
        public const string CloudDone = "";

        private static readonly IconInfo[] Table =
        {
            Outline("rowing", Rowing),
            Outline("fitness_center", FitnessCenter),
            Outline("whatshot", Whatshot),
            Outline("pause_circle", PauseCircle),
            Outline("ac_unit", AcUnit),
            Outline("repeat", Repeat),
            Both("timer", Timer),
            Outline("straighten", Straighten),
            Outline("bolt", Bolt),
            Outline("speed", Speed),
            Outline("local_fire_department", LocalFireDepartment),
            Outline("schedule", Schedule),
            Outline("history", History),
            Outline("trending_up", TrendingUp),
            Outline("bluetooth", Bluetooth),
            Outline("bluetooth_connected", BluetoothConnected),
            Outline("bluetooth_disabled", BluetoothDisabled),
            Outline("bluetooth_searching", BluetoothSearching),
            Outline("bug_report", BugReport),
            Both("favorite", Favorite),
            Outline("heart_broken", HeartBroken),
            Outline("refresh", Refresh),
            Both("person", Person),
            Outline("cloud_upload", CloudUpload),
            Filled("cloud_done", CloudDone),
            Outline("cloud_download", CloudDownload),
            Outline("delete_sweep", DeleteSweep),
            Outline("link", Link),
            Outline("link_off", LinkOff),
            Outline("warning", Warning),
            Outline("error", Error),
            Filled("play_arrow", PlayArrow),
            Filled("pause", Pause),
            Filled("check_circle", CheckCircle),
            Outline("chevron_right", ChevronRight),
            Outline("expand_more", ExpandMore),
            Outline("expand_less", ExpandLess),
            Outline("add", Add),
            Outline("remove", Remove),
            Outline("close", Close),
            Outline("edit", Edit),
            Outline("delete", Delete),
            Outline("build", Build),
            Outline("landscape", Landscape),
            Outline("arrow_drop_up", ArrowDropUp),
            Outline("arrow_drop_down", ArrowDropDown),
        };

        // Los 46 nombres de ASSET_SLOTS §4.3, en el orden de esa tabla.
        public static IReadOnlyList<IconInfo> All => Table;

        // Por nombre del catálogo de Material Symbols ("bluetooth_connected"); KeyNotFoundException si no existe.
        public static IconInfo Get(string name)
        {
            for (var i = 0; i < Table.Length; i++)
            {
                if (string.Equals(Table[i].Name, name, StringComparison.Ordinal))
                {
                    return Table[i];
                }
            }

            throw new KeyNotFoundException("Icons: no existe el ícono \"" + name + "\".");
        }

        private static IconInfo Outline(string name, string glyph)
        {
            return new IconInfo(name, glyph, true, false);
        }

        private static IconInfo Filled(string name, string glyph)
        {
            return new IconInfo(name, glyph, false, true);
        }

        private static IconInfo Both(string name, string glyph)
        {
            return new IconInfo(name, glyph, true, true);
        }
    }

    // Un ícono de la tabla: nombre de Material Symbols, glifo (un carácter) y en qué fuentes está.
    public sealed class IconInfo
    {
        public IconInfo(string name, string glyph, bool inOutline, bool inFilled)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Glyph = glyph ?? throw new ArgumentNullException(nameof(glyph));
            if (glyph.Length != 1)
            {
                throw new ArgumentException("IconInfo " + name + ": el glifo es un solo carácter.", nameof(glyph));
            }

            InOutline = inOutline;
            InFilled = inFilled;
        }

        public string Name { get; }

        public string Glyph { get; }

        // Codepoint del glifo (0xE921; en el archivo .codepoints figura en hexadecimal, "e921").
        public int Codepoint => Glyph[0];

        public bool InOutline { get; }

        public bool InFilled { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
