// QA-V2-11 de la It. 2 (R-01, ARCHITECTURE §4.2): el simulador nunca llega a un build de distribución, se arme como se arme
// (BuildCli, un Build Profile de Unity 6 con sus propios defines, extraScriptingDefines a mano): en un player sin Development
// este ensamblado no compila. SimulatorBuildGuard (Editor) y BuildCli siguen siendo la primera barrera; esta es la última.
// UNITY_5_3_OR_NEWER deja afuera la compilación con dotnet (tools/dotnet), que define ROWMATE_SIMULATOR y no es un player.
// Lo prueba SimulatorModeTests.SimulatorDoesNotCompileInANonDevelopmentPlayer (compila esta guardia como player).
#if UNITY_5_3_OR_NEWER && ROWMATE_SIMULATOR && !UNITY_EDITOR && !DEVELOPMENT_BUILD
#error RowMate.Simulator en un build sin Development (R-01): el simulador solo va en builds de desarrollo (tools/build/build.ps1 -Simulator).
#endif
