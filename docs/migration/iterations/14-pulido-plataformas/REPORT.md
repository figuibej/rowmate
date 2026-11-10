# It. 14 — Pulido, identidad, plataformas y CI: reporte

**Estado:** cerrada, esperando tu OK. Es **la última iteración del plan**. QA: **APROBADO CON OBSERVACIONES** en la ronda 2, con **0 FAIL**, 38 PASS y 6 NO VERIFICABLE. La ronda 1 salió rechazada por 7 mayores; los 7 están corregidos y verificados en el build:
- al cerrar con la X, la app terminaba con un error de acceso a memoria;
- la barra de título quedaba fuera de la pantalla;
- al arrancar se veía ≈ 0,8 s de blanco;
- 4 criterios no tenían prueba.

## 1. Qué se entregó

- **Identidad.** `RowMate.exe` lleva producto RowMate, empresa iguisoft y versión 1.0.0. El ícono es propio: remos cruzados sobre `#0D1B2A`, generado por código, reproducible y sin assets de terceros. Aparece en el `.exe`, la barra de tareas y la barra de título. También está el adaptativo de Android (con sus versiones redonda y heredada) y está preparado el de iOS/macOS. Los ids son `com.iguisoft.rowmate` en Android y `com.iguisoft.rowmate.unity` en Apple.
- **Arranque oscuro.** Fondo `#0D1B2A` con el logo centrado y un fundido de 200 ms al shell. No aparece el logo de Unity y el arranque no tarda más (Δ −15 ms).
- **Ventana de Windows.**
  - Abre centrada en 1280 × 720 lógicos; si no entra, se achica con el marco incluido.
  - El mínimo es 640 × 360.
  - `F11` o `Alt+Enter` pasan a pantalla completa y vuelven al tamaño anterior.
  - Cerrar con la X o con Alt+F4 sale limpio.
- **Rotación y redimensionado.** El HUD entra en 200 ms en el *layout* nuevo y se conservan la sesión, el reloj, el texto y el foco del editor, el diálogo abierto y la pestaña. Android: las 4 orientaciones, respetando el bloqueo del sistema.
- **Textos.** Hay una auditoría automática en es/en sobre más de 50 pantallas y estados. Ningún texto visible queda fuera del catálogo, salvo el simulador y el diagnóstico. Los fijos del legacy quedaron traducidos. El catálogo tiene 256 claves por idioma.
- **Lectores de pantalla.**
  - Narrador en Windows; TalkBack ya cableado para cuando haya teléfono.
  - Se leen pestañas, títulos, botones con su nombre, rol y estado, métricas como `Watts: 150`, listas y diálogos.
  - Se anuncian el cambio de pestaña, los avisos (una vez cada uno) y el cambio de paso (`Paso 2 de 5: Trabajo`). El gráfico se lee como un resumen.
  - Con el lector apagado no cuesta nada.
- **Pulido.** Transiciones auditadas, cifras tabulares también en el Historial y su Detalle, y el diálogo de completado a 1,3× ya no se mueve. En Android, háptica sutil al pausar, al reanudar, al cambiar de paso, al completar y al reconectar.
- **Deuda resuelta:**
  - #40: el nombre del remo se ve completo al reconectar.
  - #41: el botón `Buscando...` es legible.
  - #42: Dispositivo en horizontal de teléfono usa un solo desplazamiento.
  - #44: `Error al escanear: error 2`, sin el prefijo técnico.
  - #69: el aviso de exportación tiene contraste, la ruta se parte bien y el reloj se pausa con el puntero.
  - #36: el Historial a 1,3×.
  - #32: ejes sin etiquetas encimadas y split con rango mínimo de 10 s.
  - #37: extremos redondos, tira de pasos redondeada e ícono en el error.
  - #21: `Preparando escena…` en el primer entrenamiento.
  - #51: háptica al reconectar.
- **CI.**
  - `ci-core` (dotnet) corre siempre.
  - `ci-unity` hace los tests y compila Windows y Android normales, con artefactos de 14 días. Sin secretos se saltea y queda en verde.
  - Hay un job manual de macOS que compila iOS y macOS sin firmar.
  - No hay credenciales versionadas y el YAML se valida con tests.

## 2. Cómo probarlo (Windows, `Builds/Windows/normal`, ≈ 25 min)

1. Abrí `RowMate.exe`. Tiene que verse el arranque oscuro con el logo y el ícono en la barra de tareas y en Alt+Tab.
2. Achicá la ventana hasta el tope (se frena en 640 × 360) y volvé a agrandarla. Apretá `F11` y después `F11` otra vez, y probá lo mismo con `Alt+Enter`. Cerrá con la X y volvé a abrir: tiene que abrir en ventana y centrada.
3. **Dispositivo**: buscá y conectá el remo. Si no lo tenés, usá `Builds/Windows/simulator`.
4. **Rutinas**: abrí `Escalera` en el editor y volvé.
5. **Workout**: iniciá `Escalera`. En el primer entrenamiento se ve `Preparando escena…` y después la escena. Remá 1 minuto y tocá `Terminar`.
6. Hacé una libre corta en cada escenario: `Lago alpino`, `Río urbano`, `Costa` y `Canal de regata`.
7. **Historial** → la sesión → cambiá de métrica en el gráfico → `Exportar TCX` → `Abrir carpeta`.
8. **Inglés**: en Configuración → Hora e idioma → Idioma y región, subí English al primer lugar de los idiomas preferidos y reabrí la app. Repetí los pasos 3 a 7: no tiene que aparecer ningún texto en español fuera del simulador. Después dejá el orden como estaba.
9. **Narrador**: encendelo con `Win+Ctrl+Enter`. Recorré con `Tab` y las flechas las pestañas, los botones, las métricas, las rutinas, el Historial y el Detalle. Durante un entrenamiento tenés que escuchar el cambio de paso, y al exportar, el aviso. Fijate también que el HUD siga fluido. Apagalo con la misma combinación.
10. Si tenés dos monitores con escalas distintas, arrastrá la ventana de uno al otro: tiene que verse nítida en menos de 1 s.

## 3. Resultados reales (ronda 2)

| Corrida | Resultado |
|---|---|
| `run-tests.ps1 -Matrix` | 4023 tests: 3984 pasan, 39 ignorados, **0 fallan**, 0 advertencias. Herramientas 194/194. Matriz PASA 867, FALLA 0 |
| Unity EditMode | 4152: 4098 pasan, 54 ignorados, 0 fallan. Con `-Simulator`: 4346, 4292 pasan, 0 fallan |
| Unity PlayMode | 690/690. Con `-Simulator`: 767/767 en la 2.ª corrida; en la 1.ª falló 1 test intermitente de la It. 11 (va a issue) |
| Builds | Normal y simulador OK, con la versión del `.exe` verificada. `-CloseSmoke`: 12/12 cierres limpios. `-BootSmoke`: Δ −15 ms. `-BleSmoke`: OK, sin conectar |
| Android (Kotlin) | `check.ps1 -Golden` OK |
| ProjectSettings | Solo cambian íconos, arranque, orientaciones, ventana, versión e ids. Sin el define del simulador y `Horizon*.mat` sin cambios |
| Repo público | Limpio, sin rutas ni datos del equipo, también en los PNG. Todo en UTF-8 |
| Paridad | Ningún test de las It. 1 a 12 se rompió y todas las cuentas suben |

## 4. Criterios que fallan o no se pueden verificar

**Ninguno falla.** Quedan **6 NO VERIFICABLE:**
- **AC-APP-15-08**, DPI con dos monitores: lo verificás vos en la demo (paso 10).
- **AC-POL-06**, 60 fps sostenidos con la escena: la pantalla de QA estaba en modo de reserva (1024 × 768) y la GPU no se pudo medir. CPU y memoria dan igual que en la It. 8. Se repite con el monitor normal.
- **AC-APP-19-06, -07 y -08**, la corrida real del CI: el YAML está validado localmente; falta que cargues los secretos.
- **AC-NV-01**, el arrastre acumulado:
  - [Device-Android]: 60 fps durante 30 min (R-12); AC-SCN-01-09, -16 y -19; 200 píldoras (R4b-02); permisos con `aapt2` (AC-APP-13-01); AC-APP-13-02 y -14-05; AC-HRM-HW-02; AC-STR-09-24; AC-APP-16-04 y -12; TalkBack, háptica, arranque y rotación de esta iteración (#7, #8, #46, #68).
  - [BLE-HW]: AC-BLE-06-08, -08-07, -09-03, -91-02 y AC-HRM-HW-01.
  - [Apple]: todo lo de la 10b.
  - Demos tuyas: P-17 (#5) y `powercfg` (AC-APP-14-01 a -03).

## 5. Decisiones por defecto (rigen si no decís otra cosa)

- **DV-128:** ventana mínima de 640 × 360. **DV-129:** pantalla completa con `F11`/`Alt+Enter`, sin recordarla. **DV-130:** la ventana arranca centrada.
- **DV-131:** ids definitivos; en Apple, `.unity` para convivir con el legacy. **DV-132:** Windows sin MSIX.
- **DV-133 y DV-134:** ícono propio y logo fijo con fundido, sin animación de marca.
- **DV-135:** Android en las 4 orientaciones. **DV-136:** el lector no anuncia las métricas solo; se leen al llegar a ellas.
- **DV-137:** se oculta la marca de eje que choca con un borde. **DV-138:** seguimos en Unity 6000.3.27f1. **DV-139:** el APK del CI se firma con la llave de depuración si faltan los secretos de firma.
- **Del PO, al cerrar:**
  - **DV-V14-03:** el arranque oscuro cuenta desde el primer código de la app. Queda ≤ 0,1 s blanco que pinta Windows al crear la ventana; sacarlo pide un *launcher* nativo (issue).
  - **QA-V14-11:** el aviso de exportación sigue de vidrio al 86 %. El gráfico se adivina detrás, pero el contraste es ≈ 14,7:1. Si lo preferís opaco, hay un issue.
  - El test intermitente de la It. 11 se arregla antes de cargar `UNITY_LICENSE`.

## 6. Capturas

| | |
|---|---|
| Ícono | [CAP-01](snapshots/CAP-01.png) (1024 a 16 px) · [CAP-02](snapshots/CAP-02.png) (adaptativo con máscaras) |
| Arranque | [W1280](snapshots/CAP-03-arranque-w1280.png) · [TV](snapshots/CAP-04-arranque-tv.png) |
| Dispositivo | [L640 mínima](snapshots/CAP-05-dispositivo-l640.png) · [TV tabular](snapshots/CAP-07-dispositivo-tv.png) · [#40](snapshots/CAP-15-reconectando-tv-13.png) · [#41](snapshots/CAP-16-buscando-tv.png) · [#42 TH](snapshots/CAP-17-buscando-th.png) · [#44](snapshots/CAP-18-error-w1280.png) |
| Historial | [en, fechas tabulares](snapshots/CAP-12-historial-tv-en.png) · [Detalle TV](snapshots/CAP-22-detalle-tv.png) · [Detalle W1280](snapshots/CAP-23-detalle-w1280.png) |
| Aviso #69 | [TV](snapshots/CAP-19-aviso-tv.png) · [W1280 en, con el puntero](snapshots/CAP-20-aviso-hover-w1280-en.png) |
| Lector | [CAP-27](snapshots/CAP-27-lector-w1280.png) + [árbol](snapshots/CAP-27-a11y.txt) |

Faltan 11 tomas (CAP-06, -08 a -11, -13, -14, -21, -24 a -26) y los videos VID-01/02. CAP-23 salió igual a CAP-27 y no muestra los ejes; esas tomas pasan a un issue. Lo que muestran lo cubren los tests.

## 7. Qué tenés que hacer vos

- **CI (cierra #10).** En GitHub → Settings → Secrets and variables → Actions, cargá `UNITY_LICENSE` (el texto de `Unity_lic.ulf`), `UNITY_EMAIL` y `UNITY_PASSWORD`. Opcionales, para firmar el APK de tienda: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` y `ANDROID_KEYALIAS_PASS`. Después: Actions → `ci-unity` → Run workflow. El detalle está en `tools/unity/README.md` §It. 14: CI. Antes conviene arreglar el test intermitente (issue).
- **Android.** Unity Hub → 6000.3.27f1 → Add modules → Android Build Support (con OpenJDK y SDK & NDK). Necesitás un teléfono de gama media (Android 8+, 64 bits) con depuración USB. Corré `tools/build/build.ps1 -Target Android -Backend IL2CPP` y recorré la lista Android de §4 con TalkBack y la háptica.
- **Apple (10b).** Hace falta una Mac con Xcode, Unity 6000.3.27f1 con iOS y Mac Build Support, un iPhone y una cuenta de Apple. Hasta entonces, el job manual `apple` compila sin firmar.
- **Demos pendientes:** la de esta iteración (§2); el AMS-670B (10a); el pulsómetro (11); subir un TCX (12); suspender el equipo (P-17).

## 8. Balance final de la migración

- **Con paridad funcional con el legacy, en Unity 6.3 y look EXR:**
  - el remo FTMS por BLE (Windows real; Android en Kotlin, compilado);
  - el pulsómetro;
  - rutinas con su editor y su base local;
  - el motor de sesión con guardado incremental;
  - el HUD inmersivo con series y parciales;
  - 4 escenarios 3D con remero y palada;
  - historial y detalle con gráficos;
  - el simulador;
  - es/en;
  - el lector de pantalla;
  - identidad y ventana de escritorio.

  Todo con ≈ 4000 tests de Core y ≈ 4800 de Unity en verde.
- **Cambió por tu decisión:**
  - Strava se descartó (la API es paga) y en su lugar está **Exportar TCX**; la 13 no se hace.
  - La 6b (agua avanzada) quedó **postergada**: la app usa el agua degradada.
  - El *spike* S-1 no se hizo.
  - El panel del simulador queda en español.
  - Los ids de Apple llevan `.unity`.
  - Hay correcciones al legacy: el nombre en Windows, el arranque blanco de iOS y el ícono de Flutter.
- **Lo que queda abierto:**
  - la 6b (#6 y sus satélites #1, #2, #13 y #31);
  - la 10b (Apple);
  - las demos con hardware (remo, pulsómetro, teléfono, P-17);
  - la corrida real del CI (#10);
  - los issues de deuda: 66 de antes siguen abiertos (entre ellos #32, #37 y #51, con su resto) y se suman 10 nuevos de esta iteración, agrupados. Ninguno bloquea el uso.
