using ARSurvival.Core;

namespace ARSurvival.States
{
    /// <summary>
    /// Active round: counts down the survival timer and tallies kills. Ends when the timer runs
    /// out (player survived) or the player dies.
    /// </summary>
    public class PlayingState : GameState
    {
        public PlayingState(GameManager game) : base(game) { }

        public override GameStateId Id => GameStateId.Playing;

        public override void Enter()
        {
            var session = Game.Session;
            session.Reset(Game.Difficulty.RoundDuration);
            Game.SetWorldVisible(true);

            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);

            EventBus<RoundStartedEvent>.Raise(new RoundStartedEvent(Game.Difficulty, Game.Placement.PlacedWorld));
            EventBus<ScoreChangedEvent>.Raise(new ScoreChangedEvent(session.Score));
            RaiseTimer();
        }

        public override void Tick(float deltaTime)
        {
            Game.Session.Tick(deltaTime);
            RaiseTimer();
            if (Game.Session.IsTimeUp)
                Game.EndRound(survived: true);
        }

        public override void Exit()
        {
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
        }

        void RaiseTimer() =>
            EventBus<TimerChangedEvent>.Raise(new TimerChangedEvent(Game.Session.TimeRemaining, Game.Session.Duration));

        void OnPlayerDied(PlayerDiedEvent e) => Game.EndRound(survived: false);

        void OnEnemyKilled(EnemyKilledEvent e)
        {
            Game.Session.AddKill(e.ScoreValue);
            EventBus<ScoreChangedEvent>.Raise(new ScoreChangedEvent(Game.Session.Score));
        }
    }
}
