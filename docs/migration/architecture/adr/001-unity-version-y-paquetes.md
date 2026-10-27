# ADR 001: Unity 6.3 LTS (6000.3.27f1) y versiones de paquetes

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** todo el proyecto

## Contexto

El usuario fijó "Unity 6 LTS + URP". Al 2026-10-27 hay dos LTS de Unity 6: 6.0 (soporte hasta octubre de 2026, o sea, terminando) y 6.3 (publicada el 2025-12-04, soporte hasta diciembre de 2027). Unity 6.7 LTS está en beta desde el 2026-09-17. El último parche de 6.3 es `6000.3.27f1` (2026-10-20). Fuentes en [ARCHITECTURE §23](../ARCHITECTURE.md#23-fuentes).

## Decisión

- Editor **`6000.3.27f1`** (`ProjectSettings/ProjectVersion.txt`).
- Paquetes con versión exacta: URP 17.3.0, Shader Graph 17.3.0, Input System 1.20.1, Test Framework 1.6.0, uGUI 2.0.0, Newtonsoft 3.2.2, Animation Rigging 1.4.1, VS Editor 2.0.28, Android Logcat 1.4.7, `com.gilzoide.sqlite-net` 1.3.3 (git) y los módulos listados en ARCHITECTURE §1.2.
- Todo paquete nuevo entra con versión exacta, motivo y fuente, y lo aprueba QA.

## Alternativas descartadas

- **6.0 LTS**: su soporte termina este mes.
- **6.7 (beta)**: no se desarrolla un producto sobre una beta. Se reevalúa en la It. 14.

## Consecuencias

- UI Shader Graph (nuevo en 6.3) queda disponible para el glass del HUD.
- Subir de parche dentro de 6000.3 es barato; pasar a 6.7 LTS es una decisión explícita futura.
- La dependencia por git necesita `git` en el `PATH` al abrir el proyecto.
- **Pedido del Designer confirmado**: UI Shader Graph (vidrio del HUD) existe desde 6.3 y solo con URP, así que `6000.3.27f1` + URP 17.3 lo cubre.
- **Íconos**: glifos de la fuente "RowMate Icons" (Material Symbols Rounded recortada, decisión del Designer), **sin** el paquete `com.unity.vectorgraphics` (solo *preview* 3.0.0). Unity 6.3 trae además un módulo integrado que importa SVG como *UI Toolkit Vector Image*; queda como opción con aprobación de QA.
- Animation Rigging 1.4.1 trae Burst y Mathematics como dependencias transitivas; el código propio no los usa salvo justificación del Designer. Cinemachine entra solo con BKL-02.

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-09 (alineación con el Designer) | Íconos como glifos de fuente, sin `com.unity.vectorgraphics`; módulo SVG integrado de 6.3 solo como opción (ARCHITECTURE §1.2) |
| QA-P-29 (parte Architect) | Dependencias transitivas de Animation Rigging aclaradas |
