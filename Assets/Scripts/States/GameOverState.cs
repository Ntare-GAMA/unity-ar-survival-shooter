using ARSurvival.Core;

namespace ARSurvival.States
{
    /// <summary>
    /// Round summary. On entry the session is saved to the leaderboard and RoundEndedEvent tells
    /// spawners and pools to wipe all enemies and projectiles.
    /// </summary>
    public class GameOverState : GameState
    {
        public GameOverState(GameManager game) : base(game) { }

        public override GameStateId Id => GameStateId.GameOver;

        /// <summary>Set by the GameManager before transitioning here.</summary>
        public bool Survived { get; set; }

        public override void Enter()
        {
            var result = Game.Session.ToResult(Survived, Game.Difficulty.DisplayName);
            Game.Leaderboard.Add(result);
            EventBus<RoundEndedEvent>.Raise(new RoundEndedEvent(result));
        }
    }
}
