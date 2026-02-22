# Rower Monitor — AMS-670B / Kinomap-XG

App Flutter para conectar con el monitor del rower AMS-670B via Bluetooth BLE
y gestionar rutinas de entrenamiento con intervalos, series y descanso.

## Protocolo Bluetooth

El módulo **Kinomap-XG / H201** implementa **FTMS** (Fitness Machine Service):

| Elemento | UUID |
|----------|------|
| Servicio FTMS | `0x1826` |
| Rower Data (notificaciones) | `0x2AD2` |

Métricas parseadas: Split 500m · SPM · Vatios · Distancia · Calorías · Pulso

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
    └── widgets/       # MetricCard, IntervalTile
```

## Instalación rápida

### 1. Instalar Flutter

```bash
# Windows: descargar desde https://docs.flutter.dev/get-started/install/windows
# Luego agregar al PATH: C:\flutter\bin
flutter doctor
```

### 2. Instalar dependencias

```bash
cd rower_app
flutter pub get
```

### 3. Ejecutar

```bash
# Android (con dispositivo conectado o emulador)
flutter run

# Windows desktop
flutter run -d windows

# Ver dispositivos disponibles
flutter devices
```

## Notas de configuración

### Android
- `minSdkVersion 21` requerido por flutter_blue_plus
- Permisos BLE ya configurados en `AndroidManifest.xml`

### iOS
- Permisos Bluetooth en `Info.plist` ya configurados
- Requiere dispositivo físico (Bluetooth no funciona en simulador)

### Windows
- flutter_blue_plus soporta Windows desde v1.x
- No requiere configuración extra

## Rutinas: tipos de pasos

| Tipo | Color | Descripción |
|------|-------|-------------|
| Calentamiento | Amarillo | Fase inicial suave |
| Trabajo | Rojo | Intervalo de esfuerzo |
| Descanso | Verde | Recuperación |
| Enfriamiento | Azul | Fase final suave |

Cada paso puede configurarse **por tiempo** (min:seg) o **por distancia** (metros)
con objetivos opcionales de vatios y SPM.
