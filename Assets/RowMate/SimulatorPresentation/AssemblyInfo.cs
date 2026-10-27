// Assembly RowMate.Simulator.Presentation: la capa del simulador (cinta, llave y panel; 02-tech-design §3.5). Solo existe con
// ROWMATE_SIMULATOR (defineConstraints): un build normal no la compila (R-01, AC-BLE-13-02).
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RowMate.Tests.UiSupport")]
[assembly: InternalsVisibleTo("RowMate.Tests.PlayMode.Simulator")]
[assembly: InternalsVisibleTo("RowMate.Snapshots")]
