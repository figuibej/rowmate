# It. 12 — Exportar TCX: reporte

**Estado:** cerrada, esperando tu OK. QA: **APROBADO CON OBSERVACIONES**: 0 FAIL, 0 bloqueantes ni mayores, 6 menores.

## 1. Qué se entregó

- **Archivo TCX** fiel al que el legacy armaba para Strava: una vuelta por paso, con pulso, cadencia y potencia (se omiten cuando valen 0), calorías por vuelta con el redondeo de Dart, distancia, hora local con su desfase (también media hora y cambio de horario en medio de la sesión), escape de caracteres y `Sport="Other"`. No lleva metadatos de Strava: el nombre de la rutina va en `<Notes>`, al final.
- **Botón `Exportar TCX`** en la barra del Detalle, a la derecha del título. Mientras exporta muestra un indicador de 18 dp. Solo aparece si la sesión tiene datos que exportar.
- **Windows:** guarda en `Documentos\RowMate\TCX`, sin diálogo. Muestra el aviso `Guardado en Documentos\RowMate\TCX` con el nombre del archivo y `Abrir carpeta` (abre el Explorador con el archivo seleccionado). Si falla, el aviso `No se pudo exportar la sesión` trae `Reintentar`.
- **Android:** abre la hoja de compartir del sistema (el archivo va a la caché y se purga a las 24 h), sin permisos nuevos. El Kotlin compila, pero falta probarlo en un teléfono.
- **Nombres seguros:** `RowMate_AAAA-MM-DD_HHmm_{rutina}.tcx`. El nombre se sanea, se corta en 40 caracteres y, si ya existe, suma `_2`, `_3`… sin pisar nada. Nunca queda un archivo escrito a medias.
- **Simulador:** las sesiones se exportan con `_SIM` en el nombre y `[SIM]` en `Notes`.
- **Nada de Strava:** la app sigue con 4 pestañas y no hace ningún pedido de red. El inspector del build confirma que no quedan credenciales.

## 2. Cómo probarlo

**Windows, build normal (`Builds/Windows/normal`), unos 10 minutos:**

1. Abrí la app. Si no tenés una sesión guardada, remá una rutina corta con pasos.
2. Entrá a Historial → abrí la sesión → tocá el ícono de descarga, arriba a la derecha. Si pasás el cursor encima dice `Exportar TCX`.
3. Verificá que aparezca el aviso `Guardado en Documentos\RowMate\TCX`, con el nombre `RowMate_…tcx` y `Abrir carpeta`.
4. Tocá `Abrir carpeta`: se abre el Explorador con el archivo seleccionado.
5. Exportá la misma sesión otra vez: tiene que aparecer `…_2.tcx` sin pisar el primero.
6. Entrá a strava.com/upload/select (es gratis) y subí el `.tcx`. Verificá:
   - la **hora de inicio** igual a la del Historial;
   - la **distancia**;
   - **una vuelta por paso** (pestaña Vueltas);
   - los gráficos de **pulso**, **cadencia** y **potencia**.
7. El TCX no tiene un tipo "remo": en Strava, editá la actividad y elegí **Remo** (o "Ergómetro"). El nombre de la rutina puede verse en la descripción.
8. (Opcional) Subí el mismo archivo a Garmin Connect o a intervals.icu.

**Simulador (`Builds/Windows/simulator`):** remá una rutina con el simulador → Historial → exportá. El archivo tiene que llamarse `…_{rutina}_SIM.tcx`, y en Strava la descripción empieza con `[SIM]`.

## 3. Resultados reales

| Corrida | Resultado |
|---|---|
| `dotnet` (Core) | 3886: 3847 pasan, 39 omitidas (las de siempre), 0 fallan; 0 advertencias |
| `dotnet` (herramientas) | 194/194 |
| Unity EditMode | 3999: 3951 pasan, 0 fallan, 48 ignorados |
| Unity EditMode `-Simulator` | 4193: 4145 pasan, 0 fallan, 48 ignorados |
| Unity PlayMode | 660/660; con `-Simulator`, 735/735 (el aviso llega en 317 ms y el peor cuadro dura 2,4 ms) |
| Matriz de trazabilidad | PASA 1029, FALLA 0, PENDIENTE UNITY 0 |
| `native/android/check.ps1` | OK (Kotlin de exportación incluido) |
| Builds normal, simulator y `-ExportSmoke` | OK. El TCX del build es igual byte a byte al de `tools/sessions`. Ninguna corrida escribió en Documentos |
| Paridad con el legacy | 10 sesiones idénticas byte a byte, salvo `<Notes>`. Con tiempo 0 el legacy daba error y Unity da 0 kcal |
| Repo público | Limpio: sin rutas ni datos del equipo, también en el golden y en las capturas |

## 4. Criterios que fallan o no se pueden verificar

Ninguno falla. **25 PASS, 3 NO VERIFICABLE:**

- **AC-STR-09-24, Android (hoja de compartir):** necesita "Android Build Support" y un teléfono. Lo que se puede ver sin eso pasa: el Kotlin compila, el manifiesto no suma permisos y la purga de 24 h funciona.
- **AC-STR-09-25, Apple:** va con la 10b.
- **AC-STR-09-27, la subida a Strava:** se cierra con tu demo (§2, pasos 6 y 7).

## 5. Decisiones por defecto (rigen si no decís otra cosa)

- **DV-116 y DV-117:** sin metadatos de Strava. `<Notes>` lleva el nombre de la rutina, después de la última vuelta.
- **DV-118:** las vueltas salen de los pasos guardados en cada punto, aunque la rutina se haya editado o borrado.
- **DV-119:** una sesión cortada toma el tiempo y las calorías de sus puntos, sin tocar la base. Con tiempo 0, las calorías son 0.
- **DV-120:** se quitan los caracteres que XML no admite.
- **DV-121:** en Windows guarda directo en `Documentos\RowMate\TCX`, sin diálogo "Guardar como".
- **DV-122:** en Android abre la hoja de compartir, no guarda en Descargas.
- **DV-123:** el simulador exporta con la marca `_SIM` / `[SIM]`.
- **DV-124 y DV-126:** solo desde el Detalle, y el botón aparece solo si la sesión se puede exportar.
- **DV-125 y DV-127:** `Sport="Other"` y la distancia del monitor tal cual (paridad con el legacy).
- **Del PO:**
  - El aviso del Detalle va al pie de la pantalla, porque el Detalle no tiene barra de pestañas.
  - El set de capturas `it9` no se regenera ahora.

## 6. Capturas

| | |
|---|---|
| Botón | [W1280](snapshots/CAP-01-boton-w1280.png) · [TV](snapshots/CAP-02-boton-tv.png) · [TH](snapshots/CAP-03-boton-th.png) · [ayuda](snapshots/CAP-12-ayuda-w1280.png) |
| Exportando | [CAP-04](snapshots/CAP-04-exportando-w1280.png) |
| Guardado | [W1280](snapshots/CAP-05-guardado-w1280.png) · [TV](snapshots/CAP-06-guardado-tv.png) · [inglés 1,3×](snapshots/CAP-10-guardado-w1280-en.png) · [L640](snapshots/CAP-11-guardado-l640.png) |
| Error | [W1280](snapshots/CAP-07-error-w1280.png) · [TH](snapshots/CAP-08-error-th.png) |
| Sin datos (sin botón) | [CAP-09](snapshots/CAP-09-sin-datos-tv.png) |

Detalle menor: en TV ([CAP-06](snapshots/CAP-06-guardado-tv.png)) el aviso cae sobre el gráfico y se ven las líneas del eje detrás del texto. Va a un issue.

## 7. Deuda

Queda en 8 issues de GitHub, agrupados, que se crean con tu OK: íconos, Historial, Android, aviso, abrir carpeta, tests, documentos y casos borde del TCX.

## 8. El plan sin Strava

Queda solo la **It. 14**: pulido EXR, identidad, plataformas y CI. Aparte siguen la 6b, postergada (issue #6), y la 10b, flotante hasta que haya Mac. Hay 133 IDs vivos y la 13 está descartada. Ya corregí el ROADMAP: no le atribuye Strava ni a la 12 ni a la 13.
