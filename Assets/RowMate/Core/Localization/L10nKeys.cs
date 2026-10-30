namespace RowMate.Core.Localization
{
    // Las 202 claves del catálogo (F-APP-10): las 159 del legacy en su orden (lib/l10n/app_es.arb), las 15 nuevas de la
    // It. 1 (01-po-spec §4.17.2), las 16 de la It. 3 (03-rutinas/01-po-spec §11.2) y las 12 de la It. 4a
    // (04a-motor-hud-funcional/01-po-spec §11.2) al final. Nombre de la constante = la clave en PascalCase. Se mantiene a mano (D-05): L10nKeysTests falla si una constante no existe en los dos catálogos o si
    // una clave del catálogo no tiene constante.
    public static class L10nKeys
    {
        // ── Legacy (159) ──
        public const string NavDevice = "navDevice";
        public const string NavWorkout = "navWorkout";
        public const string NavRoutines = "navRoutines";
        public const string NavHistory = "navHistory";
        public const string NavProfile = "navProfile";
        public const string DeviceTitle = "deviceTitle";
        public const string DeviceDisconnect = "deviceDisconnect";
        public const string DeviceConnected = "deviceConnected";
        public const string DeviceSearching = "deviceSearching";
        public const string DeviceSearch = "deviceSearch";
        public const string DeviceSearchHint = "deviceSearchHint";
        public const string DeviceNoResults = "deviceNoResults";
        public const string DeviceNoResultsHint = "deviceNoResultsHint";
        public const string DeviceBtOff = "deviceBtOff";
        public const string DeviceBtOffHint = "deviceBtOffHint";
        public const string DeviceBtDenied = "deviceBtDenied";
        public const string DeviceBtDeniedHint = "deviceBtDeniedHint";
        public const string DeviceBtEnable = "deviceBtEnable";
        public const string HrmTitle = "hrmTitle";
        public const string HrmNoSensor = "hrmNoSensor";
        public const string HrmWatchHint = "hrmWatchHint";
        public const string HrmSearch = "hrmSearch";
        public const string HrmSearchOther = "hrmSearchOther";
        public const string HrmSearching = "hrmSearching";
        public const string HrmConnectingTo = "hrmConnectingTo";
        public const string HrmConnect = "hrmConnect";
        public const string HrmDisconnect = "hrmDisconnect";
        public const string HrmForget = "hrmForget";
        public const string HrmReconnecting = "hrmReconnecting";
        public const string HrmNotFound = "hrmNotFound";
        public const string HrmUnknownSensor = "hrmUnknownSensor";
        public const string HrmIncompatible = "hrmIncompatible";
        public const string HrmConnectError = "hrmConnectError";
        public const string HrmBpm = "hrmBpm";
        public const string WorkoutTitle = "workoutTitle";
        public const string WorkoutFree = "workoutFree";
        public const string WorkoutRowerNotConnected = "workoutRowerNotConnected";
        public const string WorkoutOrPickRoutine = "workoutOrPickRoutine";
        public const string WorkoutNoRoutines = "workoutNoRoutines";
        public const string WorkoutInProgress = "workoutInProgress";
        public const string WorkoutScene = "workoutScene";
        public const string SceneLake = "sceneLake";
        public const string SceneRiver = "sceneRiver";
        public const string SceneCoast = "sceneCoast";
        public const string SceneRegatta = "sceneRegatta";
        public const string WorkoutFinish = "workoutFinish";
        public const string WorkoutFinishTitle = "workoutFinishTitle";
        public const string WorkoutFinishContent = "workoutFinishContent";
        public const string WorkoutPause = "workoutPause";
        public const string WorkoutResume = "workoutResume";
        public const string WorkoutStartTitle = "workoutStartTitle";
        public const string WorkoutStartContent = "workoutStartContent";
        public const string WorkoutStart = "workoutStart";
        public const string WorkoutCompleted = "workoutCompleted";
        public const string WorkoutCompletedMessage = "workoutCompletedMessage";
        public const string MetricTime = "metricTime";
        public const string MetricSplit = "metricSplit";
        public const string MetricDistance = "metricDistance";
        public const string MetricWatts = "metricWatts";
        public const string MetricSpm = "metricSpm";
        public const string MetricCalories = "metricCalories";
        public const string MetricHeartRate = "metricHeartRate";
        public const string MetricFreeLabel = "metricFreeLabel";
        public const string RoutinesTitle = "routinesTitle";
        public const string RoutinesEmpty = "routinesEmpty";
        public const string RoutinesEmptyHint = "routinesEmptyHint";
        public const string RoutineEdit = "routineEdit";
        public const string RoutineDelete = "routineDelete";
        public const string RoutineDeleteTitle = "routineDeleteTitle";
        public const string RoutineDeleteContent = "routineDeleteContent";
        public const string HistoryTitle = "historyTitle";
        public const string HistoryEmpty = "historyEmpty";
        public const string HistoryFree = "historyFree";
        public const string HistoryStatTime = "historyStatTime";
        public const string HistoryStatDistance = "historyStatDistance";
        public const string HistoryStatWatts = "historyStatWatts";
        public const string HistoryStatCalories = "historyStatCalories";
        public const string HistoryStatHeartRate = "historyStatHeartRate";
        public const string SessionNoTelemetry = "sessionNoTelemetry";
        public const string SessionSummary = "sessionSummary";
        public const string SessionStepBreakdown = "sessionStepBreakdown";
        public const string SessionChartWatts = "sessionChartWatts";
        public const string SessionChartSpm = "sessionChartSpm";
        public const string SessionChartSplit = "sessionChartSplit";
        public const string SessionChartDistance = "sessionChartDistance";
        public const string SessionChartHr = "sessionChartHr";
        public const string StepTypeWarmup = "stepTypeWarmup";
        public const string StepTypeWork = "stepTypeWork";
        public const string StepTypeRest = "stepTypeRest";
        public const string StepTypeCooldown = "stepTypeCooldown";
        public const string Cancel = "cancel";
        public const string Delete = "delete";
        public const string Ok = "ok";
        public const string Confirm = "confirm";
        public const string Save = "save";
        public const string EditorNewRoutine = "editorNewRoutine";
        public const string EditorEditRoutine = "editorEditRoutine";
        public const string EditorRoutineName = "editorRoutineName";
        public const string EditorDescription = "editorDescription";
        public const string EditorStepsCount = "editorStepsCount";
        public const string EditorStep = "editorStep";
        public const string EditorSeries = "editorSeries";
        public const string EditorEmptySteps = "editorEmptySteps";
        public const string EditorDeleteSeries = "editorDeleteSeries";
        public const string EditorAddStepToSeries = "editorAddStepToSeries";
        public const string EditorRepetitions = "editorRepetitions";
        public const string EditorAmount = "editorAmount";
        public const string EditorNameEmpty = "editorNameEmpty";
        public const string EditorNoSteps = "editorNoSteps";
        public const string EditorNewStep = "editorNewStep";
        public const string EditorEditStep = "editorEditStep";
        public const string EditorStepType = "editorStepType";
        public const string EditorByTime = "editorByTime";
        public const string EditorByDistance = "editorByDistance";
        public const string EditorMinutes = "editorMinutes";
        public const string EditorSeconds = "editorSeconds";
        public const string EditorMeters = "editorMeters";
        public const string EditorOptionalTargets = "editorOptionalTargets";
        public const string EditorWattsMin = "editorWattsMin";
        public const string EditorWattsMax = "editorWattsMax";
        public const string EditorTargetSpm = "editorTargetSpm";
        public const string EditorSplitMin = "editorSplitMin";
        public const string EditorSplitSec = "editorSplitSec";
        public const string EditorProgressions = "editorProgressions";
        public const string EditorProgressionSpm = "editorProgressionSpm";
        public const string EditorProgressionSpmHint = "editorProgressionSpmHint";
        public const string EditorProgressionWatts = "editorProgressionWatts";
        public const string EditorProgressionWattsHint = "editorProgressionWattsHint";
        public const string EditorProgressionSplit = "editorProgressionSplit";
        public const string EditorProgressionSplitHint = "editorProgressionSplitHint";
        public const string ProfileTitle = "profileTitle";
        public const string ProfileConnectStrava = "profileConnectStrava";
        public const string ProfileConnected = "profileConnected";
        public const string ProfileConnectedToStrava = "profileConnectedToStrava";
        public const string ProfileDisconnect = "profileDisconnect";
        public const string ProfileDisconnectTitle = "profileDisconnectTitle";
        public const string ProfileDisconnectContent = "profileDisconnectContent";
        public const string ProfileUploadSettings = "profileUploadSettings";
        public const string ProfileAutoUpload = "profileAutoUpload";
        public const string ProfileAutoUploadDesc = "profileAutoUploadDesc";
        public const string ProfileAskUpload = "profileAskUpload";
        public const string ProfileAskUploadDesc = "profileAskUploadDesc";
        public const string ProfileManualUpload = "profileManualUpload";
        public const string ProfileManualUploadDesc = "profileManualUploadDesc";
        public const string ProfileSync = "profileSync";
        public const string ProfileSyncDesc = "profileSyncDesc";
        public const string ProfileSyncFromStrava = "profileSyncFromStrava";
        public const string ProfileSyncToStrava = "profileSyncToStrava";
        public const string ProfileSyncing = "profileSyncing";
        public const string ProfileUploading = "profileUploading";
        public const string ProfileSyncResult = "profileSyncResult";
        public const string ProfileUploadResult = "profileUploadResult";
        public const string ProfileSyncToDesc = "profileSyncToDesc";
        public const string ProfilePendingUploads = "profilePendingUploads";
        public const string ProfileUploadToStrava = "profileUploadToStrava";
        public const string ProfileUploadingToStrava = "profileUploadingToStrava";
        public const string ProfileUploaded = "profileUploaded";
        public const string ProfileUploadFailed = "profileUploadFailed";
        public const string ProfileAskUploadDialog = "profileAskUploadDialog";

        // ── Nuevas de la It. 1 (Q-01 + Q-02, QA-P-04) ──
        public const string RoutineSummarySteps = "routineSummarySteps";
        public const string RoutineSummaryWorkRest = "routineSummaryWorkRest";
        public const string StepDurationMinSec = "stepDurationMinSec";
        public const string StepDurationMin = "stepDurationMin";
        public const string StepDurationSec = "stepDurationSec";
        public const string StepDurationMeters = "stepDurationMeters";
        public const string StepDurationNone = "stepDurationNone";
        public const string StepTargetWattsRange = "stepTargetWattsRange";
        public const string StepTargetWattsMin = "stepTargetWattsMin";
        public const string StepTargetSpm = "stepTargetSpm";
        public const string StepTargetSplit = "stepTargetSplit";
        public const string StepTargetProgressions = "stepTargetProgressions";
        public const string StepProgressionSpm = "stepProgressionSpm";
        public const string StepProgressionWatts = "stepProgressionWatts";
        public const string StepProgressionSplit = "stepProgressionSplit";

        // ── Nuevas de la It. 3 (Q-01; 03-rutinas/01-po-spec §11.2) ──
        // Ayudas emergentes que en el legacy pone Flutter (material_es.arb:5, :14; P-09 de la It. 1).
        public const string NavBack = "navBack";
        public const string MenuShow = "menuShow";

        // Menú del clic derecho de los campos en la PC (AC-RUT-11-09; material_es.arb:27-32).
        public const string TextCut = "textCut";
        public const string TextCopy = "textCopy";
        public const string TextPaste = "textPaste";
        public const string TextSelectAll = "textSelectAll";

        // Chip ×N de la serie (antes un literal del legacy: routine_editor_screen.dart:403).
        public const string EditorRepeatCount = "editorRepeatCount";

        // Topes de Q-92 (AC-RUT-13-11, -12, AC-RUT-14-05, -06, AC-RUT-11-05).
        public const string EditorErrorRange = "editorErrorRange";
        public const string EditorErrorRangeRep = "editorErrorRangeRep";
        public const string EditorRepetitionsMax = "editorRepetitionsMax";
        public const string EditorRepetitionsOutOfRange = "editorRepetitionsOutOfRange";
        public const string EditorStepOutOfRange = "editorStepOutOfRange";

        // Errores de la base (Q-13: AC-RUT-09-04 a -06).
        public const string EditorSaveError = "editorSaveError";
        public const string RoutineDeleteError = "routineDeleteError";
        public const string RoutinesLoadError = "routinesLoadError";
        public const string Retry = "retry";

        // ── Nuevas de la It. 4a (Q-01; 04a-motor-hud-funcional/01-po-spec §11.2) ──
        // Textos del HUD que el legacy tiene fijos (immersive_workout_screen.dart): tarjetas, unidades, barra libre, "a
        // continuación" (DV-24) y el objetivo debajo del valor (D-ART-09).
        public const string HudSpm = "hudSpm";
        public const string HudSplit = "hudSplit";
        public const string HudWatts = "hudWatts";
        public const string HudUnitWatts = "hudUnitWatts";
        public const string HudKcal = "hudKcal";
        public const string HudBpm = "hudBpm";
        public const string HudUnitMeters = "hudUnitMeters";
        public const string HudFreeTitle = "hudFreeTitle";
        public const string HudUpNext = "hudUpNext";
        public const string HudTarget = "hudTarget";

        // Avisos nuevos (Q-13: AC-WKT-01-11, AC-WKT-06-12; R3-08: AC-WKT-12-08).
        public const string WorkoutSaveError = "workoutSaveError";
        public const string WorkoutRoutineCannotStart = "workoutRoutineCannotStart";

        // ── Nuevas de la It. 4b (Q-01; 04b-hud-exr/01-po-spec §11.2): 202 → 214 ──
        // El chip de fase (immersive_workout_screen.dart:530-534, con el español del legacy) y PAUSA del velo (AC-WKT-13-12).
        public const string HudPhaseActive = "hudPhaseActive";
        public const string HudPhasePaused = "hudPhasePaused";
        public const string HudPhaseFinished = "hudPhaseFinished";
        public const string HudPausedLabel = "hudPausedLabel";

        // El sello (AC-WKT-09-08, -09): el veredicto y un fragmento por objetivo, con {avg} y {target} como texto.
        public const string HudSealMet = "hudSealMet";
        public const string HudSealMissed = "hudSealMissed";
        public const string HudSealWatts = "hudSealWatts";
        public const string HudSealSpm = "hudSealSpm";
        public const string HudSealSplit = "hudSealSplit";

        // El resumen del diálogo de rutina completada (immersive_workout_screen.dart:980-991, con el español del legacy).
        public const string WorkoutCompletedTime = "workoutCompletedTime";
        public const string WorkoutCompletedDistance = "workoutCompletedDistance";
        public const string WorkoutCompletedCalories = "workoutCompletedCalories";
    }
}
