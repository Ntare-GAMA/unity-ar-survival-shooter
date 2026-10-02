using ARSurvival.Core;

namespace ARSurvival.States
{
    /// <summary>Player scans for a horizontal plane and taps to place the arena; then play begins.</summary>
    public class PlacementState : GameState
    {
        public PlacementState(GameManager game) : base(game) { }

        public override GameStateId Id => GameStateId.Placement;

        public override void Enter()
        {
            EventBus<WorldPlacedEvent>.Subscribe(OnWorldPlaced);
            Game.Placement.PlacementEnabled = true;
        }

        public override void Exit()
        {
            Game.Placement.PlacementEnabled = false;
            EventBus<WorldPlacedEvent>.Unsubscribe(OnWorldPlaced);
        }

        void OnWorldPlaced(WorldPlacedEvent e) => Game.BeginRound();
    }
}
