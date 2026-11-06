using System.Runtime.CompilerServices;

// Los controles de prueba de SceneHost (DelayReadinessForTest, ForceRegionForTest, SetScrimsForTest…) y los de Core que alcanza por
// sus propios controles *ForTest son internal (06a-escena-lago/02-tech-design §3.1, §6.3; QA-G6aT-14): los ven las pruebas, el
// anfitrión de pruebas y el arnés de capturas, que nunca llegan a un build.
[assembly: InternalsVisibleTo("RowMate.Tests.PlayMode")]
[assembly: InternalsVisibleTo("RowMate.Tests.PlayMode.Simulator")]
[assembly: InternalsVisibleTo("RowMate.Tests.UiSupport")]
[assembly: InternalsVisibleTo("RowMate.Tests.UnityEditMode")]
[assembly: InternalsVisibleTo("RowMate.Snapshots")]
