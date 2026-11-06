// Atributos del assembly RowMate.Core (puro). Los internos se exponen a los assemblies de test y, desde la It. 6a, a
// RowMate.Scene: SceneHost llama los controles de prueba internal de SceneSession (ForceDistanceForTest, FreezeSceneTimeForTest,
// ForceHourForTest, BreathingOffForTest) SOLO desde sus propios controles internal *ForTest (06a-escena-lago/02-tech-design §1.1,
// QA-G6aT-14; la regla de código está en PureCodeRulesTests).
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RowMate.Tests.Support")]
[assembly: InternalsVisibleTo("RowMate.Tests.EditMode")]
[assembly: InternalsVisibleTo("RowMate.Tests.Simulator")]
[assembly: InternalsVisibleTo("RowMate.Tests.UnityApiCheck")]
[assembly: InternalsVisibleTo("RowMate.Scene")]
