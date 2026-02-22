# Contribuir a RowMate 🚣

¡Gracias por tu interés! Toda contribución es bienvenida, desde reportar tu rower como compatible hasta agregar nuevas funcionalidades.

## ¿Cómo puedo contribuir?

### 🐛 Reportar un bug
Abrí un [issue](../../issues/new) con:
- Modelo de tu rower y módulo BLE
- Versión del sistema operativo / dispositivo
- Pasos para reproducir el problema
- Logs de la consola si los tenés (modo debug BLE disponible en la pantalla de Dispositivo)

### ✅ Reportar compatibilidad con tu rower
Si probaste RowMate con un monitor que no está en la lista, abrí un issue con:
- Nombre y modelo del rower
- Módulo BLE / marca del receptor
- Si funcionó correctamente o qué falló

### 💡 Proponer una funcionalidad
Abrí un issue describiendo qué querés agregar y por qué sería útil para la comunidad.

### 🔧 Enviar un Pull Request

1. Fork del repo
2. Crear una rama descriptiva:
   ```bash
   git checkout -b feature/nombre-de-la-feature
   # o
   git checkout -b fix/descripcion-del-bug
   ```
3. Hacer los cambios y correr los checks:
   ```bash
   flutter analyze
   flutter test
   ```
4. Commit con mensaje claro (en inglés preferentemente):
   ```bash
   git commit -m "feat: add heart rate zone display"
   ```
5. Push y abrir el PR contra `main`

## Estilo de código

- Seguimos las reglas de `flutter_lints` (se chequean automáticamente en CI)
- Nombres en inglés para código, español para UI y comentarios cuando tiene sentido
- Cada feature nueva debería venir con un test si aplica

## Arquitectura

Antes de hacer cambios grandes, revisá el [CLAUDE.md](./CLAUDE.md) que documenta la arquitectura en detalle.

## Código de conducta

Sé respetuoso y constructivo. El objetivo es construir la mejor herramienta posible para la comunidad de remadores. 🚣
