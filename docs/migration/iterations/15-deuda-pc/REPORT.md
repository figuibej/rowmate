# It. 15 — Deuda resoluble en la PC: reporte

**Estado:** cerrada por el PO y esperando tu OK. QA la dio RECHAZADA en las dos rondas. El PO la acepta igual, con **2 FAIL** que tienen la causa medida y una prueba que no deja retroceder. Ninguno de los dos se ve en la app. **38 PASS · 2 FAIL · 0 NO VERIFICABLE** (de 40 AC).

## 1. Qué se resolvió

| Issue | Qué cambia | Estado |
|---|---|---|
| #32 | La curva del gráfico ya no hace un pico en cada escalón: entre dos puntos no se pasa de ninguno de los dos | Cerrado |
| #33 | Al tocar una fila de `Rendimiento por paso` (o con Tab y Enter) la fila queda marcada y sus franjas pasan al 22 % | En parte |
| #34 | Sonda `[PERF] history` en el build de desarrollo: recarga 11,8 ms, Detalle 18,2 ms, rueda p95 6,4 ms. Erratas de la 9 corregidas. Bajó la basura por cuadro del gráfico | En parte (AC-D15-04) |
| #35 | Nombres únicos en las columnas de las tarjetas; la sonda de aspecto da 2/2 | Cerrado |
| #37 | Sombra del *tooltip*. Cifras de los ejes en `RowMate Numeric SC` | Cerrado |
| #38 | Casos borde: recarga sin cambios, un solo punto, mensajes de capacidad, rutina agregada con la pestaña oculta | Cerrado |
| #39 | Erratas de los documentos de la 9 | Cerrado |
| #67 | **Exportar desde la tarjeta del Historial** sin abrir el Detalle. Muestra el mismo aviso y el archivo sale igual byte a byte | En parte |
| #66 | El glifo de exportar sale de la fuente (`download` / `ios_share`). Recorte de íconos limpio, sin los de Strava | Cerrado |
| #70 | Si `Abrir carpeta` falla, prueba un respaldo y avisa `No se pudo abrir la carpeta` | En parte (falta el Explorador real) |
| #71 | Destrabadas las 4 pruebas ignoradas de la 12, la exportación de 3 h y "sin red". El sonar no repitió su asignación esporádica en 40 corridas | Cerrado |
| #72 | Documentos de la 12 corregidos y set `it9` regenerado con el glifo | Cerrado |
| #73 | Una vuelta con distancia negativa sale con `0`. El TCX lleva `<Creator>` RowMate 1.0.0.0 | En parte (FIT, fuera) |
| #74 | **La ventana recuerda lugar, tamaño y si estaba maximizada.** `.ico` versionado. `Alt+Enter` ya no aprieta el botón enfocado | En parte |
| #75 | Una sola forma de nombrar cada métrica (`Vatios`/`Watts`, `Split 500m`). El split se escribe `m:ss` en el Historial y el Detalle | Cerrado (decidido por el PO) |
| #76 | *Tracking* de 0,04 em en los rótulos del HUD con minúsculas | Cerrado |
| #78 | Topes con la PC libre (3/3 en la ronda 1). `[PERF]` de 120 s en los 4 escenarios. La matriz muestra aparte las pruebas que piden equipo. La causa original del intermitente quedó corregida | En parte (AC-D15-39) |
| #79 | La auditoría de textos tiene prueba negativa y exentos. Escaneo estático de los argumentos. 258 claves | Cerrado |
| #80 | El lector de pantalla recibe los marcos, el desplazamiento y los paneles expandidos. El foco vuelve al cerrar un diálogo. Con el lector apagado: 0 B por cuadro | En parte (falta el Narrador real) |
| #81 | Las 6 pruebas que faltaban de la 14. El set `it14` completo (29 tomas) | En parte |
| #82 | El HUD entra en cascada. Ancla de desplazamiento. Las pruebas de `SceneLoading`. La cinta `SIMULADOR` ya no tapa el título. Diagnóstico del "rectángulo": es la popa del casco | Cerrado (el resto ya tiene otro issue) |
| #77 | El vidrio del aviso queda como está: ya lo aceptaste | Fuera |

## 2. Cómo probarlo (`Builds/Windows/normal`, ≈ 10 min)

1. Abrí `RowMate.exe`. Movés la ventana, la achicás, la cerrás con la X y la volvés a abrir: tiene que aparecer en el mismo lugar y con el mismo tamaño. Maximizala, cerrá y reabrí: abre maximizada. Con `F11` (pantalla completa), cerrá y reabrí: abre en la última ventana, no en pantalla completa.
2. Enfocá `Iniciar` con Tab y apretá `Alt+Enter`: solo cambia la pantalla completa y el entrenamiento no arranca.
3. **Historial**: en cada tarjeta exportable hay un ícono de descarga antes del chevron. Tocalo: no se abre el Detalle y aparece `Guardado en Documentos\RowMate\TCX` con `Abrir carpeta`. La columna dice `Vatios` y el split se lee `2:13`.
4. Abrí una rutina (por ejemplo `Escalera`) y tocá la fila 2 de `Rendimiento por paso`: la fila queda con borde y su franja se aclara. Tocala de nuevo o apretá `Esc` para desmarcarla. Pasá a `Split 500m`: los escalones salen sin picos.
5. Exportá desde el Detalle y tocá `Abrir carpeta`: se abre el Explorador con el archivo seleccionado (esto lo probás vos: QA no puede).
6. Iniciá un entrenamiento: el HUD dice `Vatios` y `Split 500m`. Cambiá el tamaño de la ventana y fijate que el HUD entre en cascada corta.
7. Opcional: el Narrador (`Win+Ctrl+Enter`) durante un entrenamiento. El HUD tiene que seguir fluido.

Si el Historial del build normal está vacío (sin remo), hacé los pasos 3 y 4 en `Builds/Windows/simulator`: remá dos sesiones cortas, una libre y `Escalera`.

## 3. Resultados reales (QA, ronda 2)

- **dotnet `-Matrix`**: 4 084 en total, 4 045 pasan, 39 ignorados (de equipo), **0 fallan**. Matriz: PASA 890, FALLA 0, EN EQUIPO 18, SIN TEST 0.
- **Unity EditMode**: 4 163/4 217, y con simulador 4 357/4 411. **0 fallan**.
- **Unity PlayMode**: 731/732. Con simulador: 811/813, 811/813 y 812/813. Fallaron `SceneBudgetTests(High)` (en todas las corridas), `SeriesWorstCaseWork` (1 vez) y el banner de reconexión (1 vez). La VM de WSL ocupó entre 8 y 14 de los 24 procesadores en todas las corridas: con la PC libre (ronda 1) los topes pasaron 3/3, y no cambió ningún archivo de la escena.
- **Builds**: normal (`RowMate 1.0.0`, sin simulador ni diagnósticos), simulador y desarrollo con los humos de arranque, cierre, ventana, base, Bluetooth (solo cuenta) y exportación: todos OK.
- Repo público limpio (sin rutas ni datos de la PC), UTF-8 sin BOM. `ProjectSettings` y los `.mat` del horizonte, sin cambios. Legacy sin tocar.

## 4. AC que fallan o no se pudieron verificar

- **AC-D15-04 (FAIL) · #34**: basura por cuadro del gráfico. Bajó de 15/48 a **2/48** en la entrada y el cursor en caliente quedó en **0/60**. Siguen 10/24 en el cambio de métrica y 2/60 con el cursor en frío. La causa está medida: páginas nuevas de UIR y el cruce de colores del selector de métrica. Vuelve a #34.
- **AC-D15-39 (FAIL) · #78**: el banner de reconexión. La causa original quedó corregida, pero en 1 de 4 corridas completas apareció **otra**: 1 asignación del reloj de prueba. No se relajó la prueba. Vuelve a #78.
- Partes que tenés que ver vos en la demo: el Explorador real (#70) y los 60 fps con el Narrador (#80).

## 5. Decisiones por defecto (rigen si no decís otra cosa)

- DV-140 / DV-141: un nombre por métrica. Split `m:ss` en el Historial y el Detalle, y `mm:ss` en Dispositivo y el HUD.
- DV-142: la curva no se pasa entre dos puntos. Es un cambio frente a `fl_chart`.
- DV-143: exportar también desde la tarjeta.
- DV-144: una vuelta negativa sale con `0`, y el TCX lleva `<Creator>`.
- DV-145: la ventana se recuerda, salvo la pantalla completa.
- DV-146: la fila marcada resalta su franja al 22 %.
- DV-147: `Show BLE debug` sigue en inglés.
- DV-148: no se hace FIT.
- Cierre: los 2 FAIL se aceptan con su dato y vuelven a #34 y #78. #82 se cierra: la estela va con la 6b (#6) y la popa del casco, a un issue nuevo.

## 6. Capturas (`snapshots/`)

- [CAP-01](snapshots/CAP-01-historial-exportar-tv.png): Historial con el botón de exportar, `Vatios` y `2:10`.
- [CAP-02](snapshots/CAP-02-tarjeta-guardado-w1280-en.png): aviso `Saved to …` sobre las pestañas.
- [CAP-03](snapshots/CAP-03-paso-marcado-tv.png): fila 2 marcada.
- [CAP-04](snapshots/CAP-04-split-cursor-w1280.png): `Split 500m` sin picos y el *tooltip* con sombra.
- [CAP-05](snapshots/CAP-05-detalle-glifo-w1280.png): glifo de la fuente en el Detalle.
- [CAP-06](snapshots/CAP-06-abrir-carpeta-fallo-w1280.png): `No se pudo abrir la carpeta`.
- [CAP-07](snapshots/CAP-07-hud-l640-13.png) y [CAP-08](snapshots/CAP-08-hud-tv-en.png): HUD con `Vatios` / `Watts` y `Split 500m`.
- [CAP-09](snapshots/CAP-09-rutinas-simulador-w1280.png): la cinta ya no tapa `Rutinas`.
- [CAP-10](snapshots/CAP-10-lago-w1280.png) contra [AUX-15-01](snapshots/AUX-15-01-lago-sin-casco-w1280.png): el "rectángulo" claro detrás del bote desaparece sin el casco, así que es la popa.

También se regeneraron los sets `it9` (24 tomas) e `it14` (29).

## 7. Qué queda abierto

- **Siguen abiertos con su resto**:
  - #33: zoom y gestos, cursor con 5 métricas, dentro/fuera del objetivo, marca "sin remo" y elemento compartido.
  - #34: AC-D15-04.
  - #67: sello de exportada y exportar varias.
  - #70: el Explorador real.
  - #73: FIT.
  - #74: animación de marca, *launcher* y recordar la pantalla completa.
  - #78: el banner y los topes con la VM de WSL encendida.
  - #80: el Narrador real.
  - #81: sets viejos.
- **Issues nuevos (7, agrupados)**: ancho máximo y columnas parejas, popa del casco, glifos sin uso, pulido de avisos y textos, pruebas que faltan, herramientas de capturas y archivos de Unity, y erratas de los documentos de la 15.
- **CI**: el intermitente del banner puede volver a salir rojo de vez en cuando hasta que se cierre #78.
