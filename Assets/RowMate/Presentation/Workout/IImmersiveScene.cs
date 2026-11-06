using System;
using RowMate.Core.Common;
using UnityEngine;

namespace RowMate.Presentation.Workout
{
    // En qué está la escena (06a-escena-lago/02-tech-design §3.1): sin armar, armándose por tramos, lista (todo armado, las variantes
    // calentadas, el único dibujo de calentamiento hecho) o fallida (un aviso en el registro: el inmersivo sigue con el fondo
    // provisorio, nunca un error que frene la sesión).
    public enum ImmersiveSceneReadiness
    {
        NotLoaded,
        Loading,
        Ready,
        Failed,
    }

    // El puerto que usa el inmersivo para la escena 3D (06a-escena-lago/02-tech-design §3.3; DT6a-03): vive en Presentation porque
    // lleva la Texture de la escena (App es puro). Lo implementa Bootstrap/SceneBridge sobre Scene/SceneHost: Presentation y Scene
    // no se ven. null en UiContext.Scene = sin escena (todas las pruebas y tomas de las It. 2 a 5: el fondo provisorio de siempre).
    // - Preload: Workout en reposo a la vista (P-14); el armado corre por tramos en los cuadros sin movimiento (SceneWarmupPolicy).
    // - Open: el cuadro del toque (la sesión arranca en 0 y corre aunque no se dibuje, AC-SCN-02-05).
    // - BeginFlight: el primer cuadro en que se ve (≥ 220 ms; el vuelo anclado a los 220 del toque). ShowInPlace: reducir movimiento o
    //   la escena demorada (con la rampa de la respiración de 0,6 s).
    // - SetPauseVeil: el peso del velo de pausa (0-1, el alfa de PauseVeilMotion / 0,15).
    // - BeginExit: el cuadro 0 de la salida. BeginAerial: el diálogo de completado. Close: detrás del negro (400 ms de la salida).
    public interface IImmersiveScene
    {
        IReadOnlyObservable<ImmersiveSceneReadiness> Readiness { get; }

        // La RT de la escena (null hasta que hay ventana); OutputChanged avisa cuando se rehace (otra ventana o resolución).
        Texture? Output { get; }

        event Action? OutputChanged;

        // La escena se dibujó en este cuadro (scene-surface pide el repintado del panel aunque el HUD no cambie: S6a-7).
        event Action? Drawn;

        // La escena se está dibujando (abierta y con la cámara a la vista).
        bool IsShowing { get; }

        void Preload();

        void Open(double now);

        // El cuadro del armado del HUD (negro y quieto; con reducir movimiento, el del toque): lo que la escena tenga pendiente para su
        // primer cuadro a la vista (la orilla de la sesión, si el rebobinado de la anterior no terminó) se termina acá, nunca en un cuadro
        // con la escena a la vista (QA-V6a-REN-04).
        void CatchUp();

        void BeginFlight(double now);

        void ShowInPlace(double now);

        void SetPauseVeil(double weight);

        void BeginExit(double now);

        void BeginAerial(double now);

        void Close();
    }
}
