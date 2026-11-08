using System;

namespace RowMate.Core.Design
{
    // Números de arte y movimiento del Bluetooth real (It. 10a; 00-arte-bluetooth §1-§5, §8; 02-tech-design §6.4, A10-03, -05, -07, -11).
    // Lo que ya es token (dur.*, ease.*, shift.*) no se repite. Milisegundos y dp. Las funciones son puras y sin asignaciones:
    // las usan SonarView, BluetoothIllustration, LinkIndicator y BleMotion en cada cuadro, y las prueba dotnet.
    public static class BleMotionSpecs
    {
        // ── Sonar de búsqueda (§1.2; P1; D10-02) ──
        // Banda fija entre la ayuda y la lista mientras busca: 8 + 136 + 8. Sin banda con alto compacto (< 560 dp de contenido).
        public const double SonarBandDp = 152;
        public const double SonarBoxDp = 136;
        public const double SonarDiscDp = 56;
        public const double SonarIconDp = 28;
        public const double CompactHeightDp = 560;

        // Anillos: 3, trazo 1,5 (stroke.target), radio 28 → 68 con ease.enter (r = 28 + 40·(1 − (1 − u)³)), alfa 0,45 → 0 lineal.
        // Período 1 600 ms y desfase 533: el anillo i sale a los i·533 ms (en el cuadro 0 solo el primero, sin anillos a mitad).
        public const int SonarRings = 3;
        public const double SonarRingStrokeDp = 1.5;
        public const double SonarRingFromRadiusDp = 28;
        public const double SonarRingToRadiusDp = 68;
        public const double SonarRingAlpha = 0.45;
        public const double SonarPeriodMs = 1600;
        public const double SonarRingOffsetMs = 533;

        // Eco al aparecer un remo nuevo: 4.º anillo en accent.onTonal, alfa 0,70 → 0 en dur.scene (900), misma curva de radio.
        public const double SonarEchoAlpha = 0.70;
        public const double SonarEchoMs = 900;

        // Respiración del disco: 1 → 1,03 → 1 en 1 600 ms (ease.pulse), en fase con el primer anillo.
        public const double SonarBreathScale = 1.03;

        // Segmentos por anillo (4 anillos × 48 × 2 = 384 vértices en búferes reusados, §8).
        public const int SonarSegments = 48;

        // Entrada de la banda: alto 0 → 152 en 0-160 (ease.move) y opacidad 0 → 1 en 60-260 (ease.move).
        public const double SonarInHeightEndMs = 160;
        public const double SonarInFadeStartMs = 60;
        public const double SonarInFadeEndMs = 260;

        // Salida (a los 10 s o al conectar): opacidad → 0 en 0-120 (ease.exit) y alto → 0 en 80-240 (ease.move).
        public const double SonarOutFadeEndMs = 120;
        public const double SonarOutHeightStartMs = 80;
        public const double SonarOutHeightEndMs = 240;

        // ── Tarjeta de remo (§1.3) ──
        public const double CardIconDiscDp = 40;
        public const double CardIconDp = 24;
        public const double CardMinHeightDp = 64;
        public const double CardGapDp = 8;

        // Entrada de un remo nuevo: alto + margen 0 → 72 en 0-160 (ease.move); opacidad 0 → 1 e y −8 → 0 en 60-260 (ease.enter);
        // escalonadas cada 35 ms, 8 como máximo.
        public const double CardGrowEndMs = 160;
        public const double CardFadeStartMs = 60;
        public const double CardFadeEndMs = 260;
        public const double CardDropDp = 8;
        public const double CardStaggerMs = 35;
        public const int CardStaggerMax = 8;

        // Las otras tarjetas mientras se conecta: opacidad 0,38 en 160 (ease.move).
        public const double CardDimOpacity = 0.38;
        public const double CardDimMs = 160;

        // ── Pasos de conexión (§2.1; A10-05) ──
        // Cruce del renglón 2: el viejo funde → 0 en 0-80 y el nuevo entra en 80-200 (no se encolan: arranca desde la opacidad actual).
        public const double StepOutMs = 80;
        public const double StepInEndMs = 200;

        // Check dibujado (20, trazo 2): el círculo se llena en 0-160 y la tilde se traza en 80-240 (ease.enter). D10-04: 240 + 360 = 600.
        public const double CheckDiscEndMs = 160;
        public const double CheckTickStartMs = 80;
        public const double CheckTickEndMs = 240;
        public const double ReadyTotalMs = 600;

        // Error en la tarjeta: el ícono de error vuelve a la flecha a los 1 200 ms (fundido de 160). Sin sacudidas (D10-05).
        public const double ErrorIconHoldMs = 1200;

        // Reducir movimiento: textos con fundido de 80; check entero con fundido de 160.
        public const double ReducedStepFadeMs = 80;
        public const double ReducedCheckFadeMs = 160;

        // ── Vista de estado (§3.1; D10-06, D10-07; DT10-13: el título en 18 negrita manda sobre el 20/600 del arte) ──
        public const double IllustrationBoxDp = 176;
        public const double IllustrationDiscDp = 112;
        public const double IllustrationIconDp = 64;
        public const double IllustrationInnerRingDp = 64;   // radio
        public const double IllustrationOuterRingDp = 84;   // radio
        public const double IllustrationRingStrokeDp = 1.5;
        public const double IllustrationInnerRingAlpha = 0.08;
        public const double IllustrationOuterRingAlpha = 0.05;

        // Alto compacto (L640, TH): 112, disco 72, ícono 48, anillos de 44 y 56.
        public const double CompactIllustrationBoxDp = 112;
        public const double CompactIllustrationDiscDp = 72;
        public const double CompactIllustrationIconDp = 48;
        public const double CompactIllustrationInnerRingDp = 44;
        public const double CompactIllustrationOuterRingDp = 56;

        // Respiración del ícono (apagado y sin permiso): opacidad 1 → 0,72 → 1 en 3 200 ms (ease.pulse). Sin Bluetooth LE no respira.
        public const double StateBreathMs = 3200;
        public const double StateBreathMinOpacity = 0.72;

        // "Se despierta" (§3.2, apagado o sin permiso → encendido): color danger → accent en 0-240 (ease.move); el glifo cruza en
        // 80-200; el disco 1 → 1,06 en 0-360 (ease.enter) y sale un anillo 56 → 84 (alfa 0,45 → 0, 900); la vista funde → 0 en 360-480
        // (ease.exit) y la búsqueda entra en 480-720 (opacidad e y +8 → 0, ease.enter).
        public const double WakeColorEndMs = 240;
        public const double WakeGlyphStartMs = 80;
        public const double WakeGlyphEndMs = 200;
        public const double WakeDiscEndMs = 360;
        public const double WakeDiscScale = 1.06;
        public const double WakeRingFromRadiusDp = 56;
        public const double WakeRingToRadiusDp = 84;
        public const double WakeRingMs = 900;
        public const double WakeOutStartMs = 360;
        public const double WakeOutEndMs = 480;
        public const double WakeInEndMs = 720;

        // Reducir movimiento: glifo y color en 0-160 y después un cruce de opacidad de 120 + 120.
        public const double ReducedWakeGlyphMs = 160;
        public const double ReducedWakeOutEndMs = 280;
        public const double ReducedWakeInEndMs = 400;

        // Búsqueda → apagado: la vista de estado entra con el disco 0,94 → 1 (ease.enter) en 120-360.
        public const double StateInDiscScaleFrom = 0.94;

        // ── Avisos del enlace (§4) ──
        // Indicador: arco de espera lineal de 3 000 ms; el spinner de siempre (una vuelta en 900).
        public const double WaitArcMs = 3000;

        // Caja de Dispositivo: r.l 12, 48 de alto mínimo y 12 de aire debajo; crece en 0-160 y colapsa en 80-240 (ease.move).
        public const double BannerMinHeightDp = 48;
        public const double BannerGapDp = 12;
        public const double BannerGrowEndMs = 160;
        public const double BannerCollapseStartMs = 80;
        public const double BannerCollapseEndMs = 240;

        // Aviso del HUD: entra en 0-200 (opacidad, ease.enter; y desde ±8); sale en 0-160 (ease.exit; y → ∓4); el ancho sigue al
        // texto en 240 (ease.move).
        public const double NoticeInMs = 200;
        public const double NoticeInShiftDp = 8;
        public const double NoticeOutMs = 160;
        public const double NoticeOutShiftDp = 4;
        public const double NoticeWidthMs = 240;

        // ── Diagnóstico (§5) ──
        // El punto de RECEIVING late con cada paquete: opacidad 1 → 0,35 → 1 en 300 (ease.pulse). Con reducir movimiento, fijo en 1.
        public const double DebugDotDp = 6;
        public const double DebugDotPulseMs = 300;
        public const double DebugDotMinOpacity = 0.35;

        // Mantener presionada la cabecera: la capa sube 0 → 8 % lineal en 0-500 ms; soltar antes la devuelve en 160.
        public const double HeaderPressMs = 500;
        public const double HeaderPressLayerOpacity = 0.08;

        // El acceso escondido (Show BLE debug): 24 % de opacidad, aparece en 160.
        public const double DebugToggleOpacity = 0.24;

        // ── Funciones puras ──

        // Radio del anillo del sonar en la fracción u (0 → 28, 1 → 68), ease.enter (cúbica de salida exacta: §1.2).
        public static double SonarRingRadius(double u)
        {
            var c = Clamp01(u);
            var k = 1 - c;
            return SonarRingFromRadiusDp + (SonarRingToRadiusDp - SonarRingFromRadiusDp) * (1 - k * k * k);
        }

        // Alfa del anillo en u (0,45 → 0 lineal).
        public static double SonarRingAlphaAt(double u)
        {
            return SonarRingAlpha * (1 - Clamp01(u));
        }

        // La fracción del anillo index (0 a SonarRings − 1) a elapsedMs desde que empezó la búsqueda; false si todavía no salió.
        public static bool SonarRingPhase(double elapsedMs, int index, out double u)
        {
            var start = index * SonarRingOffsetMs;
            if (elapsedMs < start || index < 0 || index >= SonarRings)
            {
                u = 0;
                return false;
            }

            var t = (elapsedMs - start) % SonarPeriodMs;
            u = t / SonarPeriodMs;
            return true;
        }

        // Escala del disco del sonar (1 → 1,03 → 1 por período, ease.pulse, en fase con el primer anillo).
        public static double SonarDiscScale(double elapsedMs)
        {
            return 1 + (SonarBreathScale - 1) * Pulse(elapsedMs, SonarPeriodMs);
        }

        // Opacidad del ícono de la vista de estado que respira (1 → 0,72 → 1 en 3 200 ms).
        public static double StateBreathOpacity(double elapsedMs)
        {
            return 1 - (1 - StateBreathMinOpacity) * Pulse(elapsedMs, StateBreathMs);
        }

        // Opacidad del punto de RECEIVING a elapsedMs desde el último paquete (1 → 0,35 → 1 en 300; ≥ 300 → 1).
        public static double DebugDotOpacity(double elapsedMs)
        {
            if (elapsedMs <= 0 || elapsedMs >= DebugDotPulseMs)
            {
                return 1;
            }

            return 1 - (1 - DebugDotMinOpacity) * Pulse(elapsedMs, DebugDotPulseMs);
        }

        // Un pulso 0 → 1 → 0 por período con ease.pulse a cada lado (0 en t = 0 y en cada período entero, 1 a la mitad).
        public static double Pulse(double elapsedMs, double periodMs)
        {
            if (periodMs <= 0 || double.IsNaN(elapsedMs))
            {
                return 0;
            }

            var p = elapsedMs % periodMs / periodMs;
            if (p < 0)
            {
                p += 1;
            }

            var half = p < 0.5 ? p * 2 : (1 - p) * 2;
            return Easing.Evaluate(Tokens.EasePulse, half);
        }

        // La fracción del tramo [startMs; endMs] a elapsedMs (0 antes, 1 después).
        public static double Segment(double elapsedMs, double startMs, double endMs)
        {
            if (endMs <= startMs)
            {
                return elapsedMs >= endMs ? 1 : 0;
            }

            return Clamp01((elapsedMs - startMs) / (endMs - startMs));
        }

        // El retardo de la tarjeta nueva número order (0, 1, …) de un mismo lote: 35 ms cada una, hasta 8 (las demás, con la 8.ª).
        public static double CardDelayMs(int order)
        {
            var i = order < 0 ? 0 : Math.Min(order, CardStaggerMax - 1);
            return i * CardStaggerMs;
        }

        private static double Clamp01(double v)
        {
            return double.IsNaN(v) || v <= 0 ? 0 : v >= 1 ? 1 : v;
        }
    }
}
