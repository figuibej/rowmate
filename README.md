# RowMate 🚣

**RowMate** es una app Flutter open-source para conectar tu rower con Bluetooth BLE (protocolo **FTMS**) y gestionar rutinas de entrenamiento con intervalos, series y descanso.

> Desarrollada originalmente para el **AMS-670B / Kinomap-XG**, pero compatible con **cualquier monitor que implemente el estándar FTMS**.

---

## ¿Qué monitores son compatibles?

RowMate usa el protocolo **FTMS (Fitness Machine Service)** — un estándar abierto de Bluetooth SIG. Si tu rower tiene BLE y soporta FTMS, debería funcionar.

| Monitor | Estado |
|---------|--------|
| AMS-670B / Kinomap-XG | ✅ Probado |
| Sunny Health & Fitness (SF-RW5623, SF-RW5941, etc.) | ✅ FTMS nativo |
| Domyos / Decathlon (R500, R900) | ✅ FTMS nativo |
| NordicTrack RW700 / RW900 | ⚠️ Parcial |
| WaterRower (con módulo BLE) | ⚠️ Según modelo |
| Genéricos con módulo BLE "FTMS compatible" | ✅ Probable |
| Concept2 (PM5) | ❌ Protocolo propietario |
| Hydrow / Ergatta | ❌ Protocolo propietario |

> **¿Probaste tu rower?** Abrí un [issue](../../issues) o PR para agregarlo a la lista 🙌

---

## Protocolo Bluetooth

| Elemento | UUID |
|----------|------|
| Servicio FTMS | `0x1826` |
| Rower Data (notificaciones) | `0x2AD2` |

Métricas parseadas: **Split /500m · SPM · Vatios · Distancia · Calorías · Pulso**

---

## Funcionalidades

- 📡 **Scan y conexión BLE** automática con reconexión
- 📊 **Métricas en tiempo real** (split, SPM, watts, distancia, BPM)
- 🏋️ **Rutinas de entrenamiento** con intervalos configurables por tiempo o distancia
- 🎯 **Objetivos** de watts y SPM por paso
- 📈 **Historial** de sesiones con telemetría detallada
- 🔒 **Pantalla siempre activa** durante el workout

---

## Estructura del proyecto

```
lib/
├── core/
│   ├── bluetooth/
│   │   ├── ble_service.dart       # Conexión BLE + suscripción
│   │   └── ftms_parser.dart       # Parser del characteristic 0x2AD2
│   ├── database/
│   │   └── database_service.dart  # SQLite (sqflite)
│   └── models/
│       ├── rowing_data.dart        # Métricas en tiempo real
│       ├── routine.dart            # Rutina de entrenamiento
│       ├── interval_step.dart      # Paso individual (trabajo/descanso)
│       └── workout_session.dart    # Sesión grabada + DataPoints
├── features/
│   ├── device/        # Scan BLE + conexión
│   ├── workout/       # Workout en vivo con tracking de rutina
│   ├── routines/      # CRUD de rutinas + editor de pasos
│   └── history/       # Historial de sesiones
└── shared/
    ├── theme.dart
    └── widgets/
```

---

## Instalación rápida

### 1. Requisitos

- [Flutter SDK](https://docs.flutter.dev/get-started/install) ≥ 3.3.0
- Dispositivo Android (API 21+), iOS, o macOS

### 2. Clonar e instalar

```bash
git clone https://github.com/figuibej/rowmate.git
cd rowmate
flutter pub get
```

### 3. Ejecutar

```bash
# Android (con dispositivo conectado o emulador)
flutter run

# macOS desktop
flutter run -d macos

# Ver dispositivos disponibles
flutter devices
```

---

## Configuración por plataforma

### Android
- `minSdkVersion 21` requerido
- Permisos BLE ya configurados en `AndroidManifest.xml`

### iOS
- Requiere dispositivo físico (Bluetooth no funciona en simulador)
- Permisos en `Info.plist` ya configurados

### macOS
- Funciona sin configuración extra

---

## Tipos de pasos en rutinas

| Tipo | Color | Descripción |
|------|-------|-------------|
| Calentamiento | 🟡 Amarillo | Fase inicial suave |
| Trabajo | 🔴 Rojo | Intervalo de esfuerzo |
| Descanso | 🟢 Verde | Recuperación |
| Enfriamiento | 🔵 Azul | Fase final suave |

Cada paso se configura **por tiempo** (min:seg) o **por distancia** (metros), con objetivos opcionales de watts y SPM.

---

## Contribuciones

¡Son bienvenidas! Si tenés un rower compatible no listado, o querés agregar métricas / funcionalidades, abrí un issue o un PR.

---

## Licencia

MIT
