# CLAUDE.md

RowMate en Unity: migración de la app Flutter `../rowmate-legacy` (monitor de remo FTMS por BLE, pulsómetro, rutinas de intervalos, escena inmersiva, historial, Strava, simulador) a Unity 6 LTS + URP, con paridad funcional total y look & feel realista estilo EXR.

## Reglas de la migración

- [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (clon local en `../rowmate-legacy/`) es **solo lectura**: es el contrato de comportamiento. Su `CLAUDE.md`, `docs/superpowers/specs/` y `test/` definen qué tiene que hacer la app.
- Se migra **por iteraciones** (vertical slices) definidas en [docs/migration/ROADMAP.md](docs/migration/ROADMAP.md). El estado actual y la próxima iteración están en [docs/migration/STATUS.md](docs/migration/STATUS.md): leelo antes de continuar.
- Al terminar cada iteración se muestra el resultado al usuario **antes** de empezar la siguiente.

## Agentes ([.claude/agents/](.claude/agents/))

| Agente | Define | Entrega por iteración |
|---|---|---|
| `rowmate-po` | el QUÉ (no escribe código) | `01-po-spec.md` |
| `unity-architect` | el CÓMO, y lo implementa | `02-tech-design.md` + código |
| `rowmate-qa` | armonía QUÉ↔CÓMO, verificación y bugs | `03-qa-harmony.md`, `05-qa-verification.md` |
| `rowmate-designer` | arte, UI y animaciones realistas | `04-design-notes.md` + shaders/USS/animación |

Ciclo por iteración: PO spec → Architect diseño (+ Designer) → QA gate de armonía → implementación → QA verificación (con `dotnet test`) → correcciones → reporte (`REPORT.md`) → STATUS.md.

## Decisiones fijas del usuario

- Assets 3D **gratis**: Mixamo (los descarga el usuario), paquetes free de Unity, procedural y shaders URP propios, siempre detrás de *asset slots*.
- Bluetooth LE con **plugins nativos propios** (Android Kotlin, iOS/macOS Swift, Windows WinRT) y siempre una implementación simulada.
- Unity **6000.3.27f1** (Unity 6.3 LTS) en la máquina de desarrollo; el Core puro se compila y testea también con **.NET 8** (`C:/Program Files/dotnet/dotnet.exe`), igual que en el CI.
- Repo público: nada de rutas de la PC ni datos del equipo en lo que se versiona. El legacy se cita como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (clon local en `../rowmate-legacy/`), el repo con rutas relativas y el scratchpad como `<scratch>`.

## Documentación

[docs/migration/](docs/migration/): `product/` (inventario de funcionalidades del legacy), `architecture/`, `design/` (biblias de arte y animación) e `iterations/NN-nombre/`.
