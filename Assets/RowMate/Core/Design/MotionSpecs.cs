namespace RowMate.Core.Design
{
    // Números de motion que no son tokens (02-tech-design §2.9; ANIMATION_BIBLE §2.1, §2.4, §2.9; 04-design-notes §4.7, §5,
    // DN-07, -08, -10, -11, -12, -14, -15, -16). Las duraciones y curvas que SÍ son tokens se toman de Tokens (dur.micro 90,
    // dur.fast 160, dur.base 240, dur.slow 360; ease.*; shift.s 8, shift.m 12) y no se repiten acá. Milisegundos y em.
    public static class MotionSpecs
    {
        // ── Rodillo de dígitos (04-design-notes §5.1, DN-08; duración en DigitRoll.DurationMs = 180, ease.enter) ──
        // Desplazamiento de cada glifo, en em; el saliente se apaga en u ∈ [0; 0,6] y el entrante aparece en u ∈ [0,15; 1].
        public const double RollShiftEm = 0.35;
        public const double RollOutFadeEnd = 0.60;
        public const double RollInFadeStart = 0.15;

        // Reducir movimiento: fundido cruzado lineal en el lugar.
        public const double ReducedFadeMs = 80;

        // Celdas de ':' y '.' (el '-' usa su avance real en la fuente, DN-08 b).
        public const double ColonCellEm = 0.32;

        // ── Pestañas (04-design-notes §5.4, DN-11) ──
        // Contenido: sale 0-120 y entra 80-280. Ícono elegido: 1 → 1,12 (0-90, ease.enter) → 1 (90-260, ease.move).
        // Color del ícono y de la etiqueta: dur.fast 160 con ease.move, por código.
        public const double TabOutMs = 120;
        public const double TabInStartMs = 80;
        public const double TabInEndMs = 280;
        public const double TabIconBump = 1.12;
        public const double TabIconPeakMs = 90;
        public const double TabIconEndMs = 260;

        // Píldora de la barra: spring.snappy (ANIMATION_BIBLE §1.2).
        public const double PillOmega = CriticalSpring.SnappyOmega;

        // Escala de FitBox cuando un dato la cambia: spring.smooth (04-design-notes §5.9, DN-15).
        public const double FitScaleOmega = CriticalSpring.SmoothOmega;

        // ── Latido (04-design-notes §5.3, DN-10; HeartbeatClock) ──
        // s(τ) = 1 + 0,16·p₁(τ) + 0,06·p₂(τ). p₁ sube 0-90 (ease.enter) y baja 90-290 (ease.move); p₂ sube 170-240 (ease.enter)
        // y baja 240-390 (ease.move). Si T = 60/bpm < 420 ms, todos los tiempos × T/420. Parada: a 1 en 120 ms (ease.move).
        public const double BeatMainGain = 0.16;
        public const double BeatRiseMs = 90;
        public const double BeatFallMs = 200;
        public const double SecondBeatGain = 0.06;
        public const double SecondBeatOffsetMs = 170;
        public const double SecondBeatRiseMs = 70;
        public const double SecondBeatFallMs = 150;
        public const double BeatFullCycleMs = 420;
        public const double BeatStopMs = 120;

        // Regla de toma de las capturas (DN-21): el corazón está "en reposo" si |escala − 1| ≤ 0,002.
        public const double BeatRestTolerance = 0.002;

        // ── Acento "dato nuevo" (04-design-notes §5.2, DN-07; AccentPulse) ──
        // k = 1 − 0,4·E_pulse(t / 300): acento = color de la métrica con alfa k (0,60 en reposo, legacy withOpacity(0.6));
        // brillo = 0,14·k. La tarjeta del pulsómetro queda con k = 0,6 fijo (no destella).
        public const double AccentFlashMs = 300;
        public const double AccentRestAlpha = 0.60;
        public const double AccentGlowPeak = 0.14;

        // Opacidad del corazón sin conexión: [CONTRATO] heart_rate_card.dart:40 (withValues(alpha: 0.4)), por código (DN-13).
        public const double DimHeartOpacity = 0.40;

        // ── Vista conectada ↔ búsqueda (04-design-notes §5.5, DN-12) ──
        // Sale 0-120 (al "apagar", escala 0,985); entra 120-360 (shift.s); al "encender", la tarjeta i entra de 120 + 35·i a
        // +240 (shift.m).
        public const double ViewOutMs = 120;
        public const double ViewInEndMs = 360;
        public const double ViewOutScale = 0.985;
        public const double TileStaggerMs = 35;
        public const double TileInMs = 240;

        // ── Llave y panel del simulador (04-design-notes §5.7, DN-14) ──
        // Abrir: opacidad 160 y escala 0,92 → 1 en 240. Cerrar: el panel en el acto y la llave vuelve 0,85 → 1 en 160.
        public const double PanelInScaleFrom = 0.92;
        public const double FabInScaleFrom = 0.85;

        // ── Tarjeta PULSO (04-design-notes §5.9, DN-16): sale con opacidad en 120 y la grilla se recoge en 200 ──
        public const double PulseTileOutMs = 120;
        public const double PulseTileCollapseMs = 200;

        // ── Spinner (04-design-notes §4.7): arco de 270°, una vuelta en 900 ms, lineal ──
        public const double SpinnerTurnMs = 900;
        public const double SpinnerArcDegrees = 270;
    }
}
