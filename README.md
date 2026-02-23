<p align="center">
  <img src="assets/icon/icon.png" width="120" alt="RowMate icon" />
</p>

# RowMate 🚣

**RowMate** is an open-source Flutter app that connects your rowing machine via Bluetooth BLE (using the **FTMS** standard) and helps you manage interval training routines.

> Originally built for the **AMS-670B / Kinomap-XG**, but compatible with **any monitor that implements the FTMS standard**.

---

## Compatible Rowing Machines

RowMate uses the **FTMS (Fitness Machine Service)** protocol — an open Bluetooth SIG standard. If your rowing machine has BLE and supports FTMS, it should work.

| Monitor | Status |
|---------|--------|
| AMS-670B / Kinomap-XG | ✅ Tested |
| Sunny Health & Fitness (SF-RW5623, SF-RW5941, etc.) | ✅ Native FTMS |
| Domyos / Decathlon (R500, R900) | ✅ Native FTMS |
| NordicTrack RW700 / RW900 | ⚠️ Partial |
| WaterRower (with BLE module) | ⚠️ Model dependent |
| Generic BLE rowing machines "FTMS compatible" | ✅ Likely |
| Concept2 (PM5) | ❌ Proprietary protocol |
| Hydrow / Ergatta | ❌ Proprietary protocol |

> **Tested your rowing machine?** Open an [issue](../../issues) or PR to add it to the list 🙌

---

## Bluetooth Protocol

| Element | UUID |
|---------|------|
| FTMS Service | `0x1826` |
| Rower Data (notifications) | `0x2AD2` |

Parsed metrics: **Split /500m · SPM · Watts · Distance · Calories · Heart Rate**

---

## Features

- 📡 **BLE scan and auto-reconnect** — connects and recovers from drops automatically
- 📊 **Real-time metrics** — split, SPM, watts, distance, BPM
- 🏋️ **Training routines** — configurable intervals by time or distance
- 🎯 **Targets** — optional watts and SPM goals per step
- 📈 **Session history** — with detailed telemetry
- 🔒 **Screen always-on** during workouts

---

## Screenshots

<img src="docs/image-01.png" />
<img src="docs/image-02.jpg" />
<img src="docs/image-03.png" />
<img src="docs/image-04.jpg" />
<img src="docs/image-05.png" />


---

## Project Structure

```
lib/
├── core/
│   ├── bluetooth/
│   │   ├── ble_service.dart       # BLE connection + subscriptions
│   │   └── ftms_parser.dart       # 0x2AD2 characteristic parser
│   ├── database/
│   │   └── database_service.dart  # SQLite (sqflite)
│   └── models/
│       ├── rowing_data.dart        # Real-time metrics
│       ├── routine.dart            # Training routine
│       ├── interval_step.dart      # Individual step (work/rest)
│       └── workout_session.dart    # Saved session + DataPoints
├── features/
│   ├── device/        # BLE scan + connection
│   ├── workout/       # Live workout with routine tracking
│   ├── routines/      # Routine CRUD + step editor
│   └── history/       # Session history
└── shared/
    ├── theme.dart
    └── widgets/
```

---

## Quick Start

### Requirements

- [Flutter SDK](https://docs.flutter.dev/get-started/install) ≥ 3.3.0
- Android (API 21+), iOS device, or macOS

### Clone and install

```bash
git clone https://github.com/figuibeh/rowmate.git
cd rowmate
flutter pub get
```

### Run

```bash
# Android (with connected device or emulator)
flutter run

# macOS desktop
flutter run -d macos

# List available devices
flutter devices
```

---

## Platform Notes

### Android
- `minSdkVersion 21` required
- BLE permissions already configured in `AndroidManifest.xml`

### iOS
- Physical device required (Bluetooth doesn't work on simulator)
- Bluetooth permissions already set in `Info.plist`

### macOS
- Works out of the box, no extra configuration needed

---

## Routines & Interval Steps

A **routine** is a sequence of steps. Each step has a type, a duration (by time or distance), and optional performance targets.

| Step Type | Color | Description |
|-----------|-------|-------------|
| Warmup | 🟡 Yellow | Easy opening phase |
| Work | 🔴 Red | Effort interval |
| Rest | 🟢 Green | Recovery |
| Cooldown | 🔵 Blue | Easy closing phase |

Each step is configured **by time** (min:sec) or **by distance** (meters), with optional **watts** and **SPM** targets that highlight in red during the workout if you fall short.

### Building a Series

You can stack steps to build full training series. A typical interval session looks like this:

```
Warmup (5 min)
  Work (2 min @ 150W+) ─┐
  Rest (1 min)          ├─ repeat × N
  Work (2 min @ 150W+) ─┘
  ...
Cooldown (3 min)
```

Example: a **4×2000m** pyramid might be:

```mermaid
gantt
    title 4×2000m Routine (example)
    dateFormat  mm:ss
    axisFormat  %M:%S
    section Warmup
    Warmup        :warmup,  00:00, 5m
    section Intervals
    Work 2000m    :work1,   after warmup, 8m
    Rest 90s      :rest1,   after work1,  1m30s
    Work 2000m    :work2,   after rest1,  8m
    Rest 90s      :rest2,   after work2,  1m30s
    Work 2000m    :work3,   after rest2,  8m
    Rest 90s      :rest3,   after work3,  1m30s
    Work 2000m    :work4,   after rest3,  8m
    section Cooldown
    Cooldown      :cool,    after work4,  3m
```

Each step in the editor can have an individual watts/SPM target, so the app warns you in real time when you drop below your goal.

---

## Contributing

Contributions are welcome! If you have a compatible rowing machine not on the list, or want to add metrics / features, open an issue or a PR.

See [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines.

---

## License

[MIT](./LICENSE) © 2026 iguisoft
