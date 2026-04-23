using System;
using RunnerGame.Model;
using RunnerGame.Rendering;

namespace RunnerGame.Presentation
{
    public interface IGameView
    {
        event EventHandler? StartGameRequested;
        event EventHandler? FrameAdvanced;
        event EventHandler? MoveLeftRequested;
        event EventHandler? MoveRightRequested;
        event EventHandler? JumpRequested;

        void Attach(GameState gameState, GameRenderer gameRenderer);
        void ShowMainMenu();
        void HideMainMenu();
        void Start();
        void Stop();
        void RequestSceneRefresh();
        void ShowGameOver(GameState gameState);
    }
}
