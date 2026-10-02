using ARSurvival.Core;

namespace ARSurvival.States
{
    /// <summary>Start menu over the live camera feed. The placed world (if any) is hidden.</summary>
    public class MainMenuState : GameState
    {
        public MainMenuState(GameManager game) : base(game) { }

        public override GameStateId Id => GameStateId.MainMenu;

        public override void Enter()
        {
            Game.Placement.PlacementEnabled = false;
            Game.SetWorldVisible(false);
        }
    }
}
