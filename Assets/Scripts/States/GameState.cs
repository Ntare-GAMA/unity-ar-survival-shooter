using ARSurvival.Core;

namespace ARSurvival.States
{
    /// <summary>
    /// State pattern: each game phase owns its own enter/tick/exit behaviour, so the
    /// GameManager never needs a switch over the current phase.
    /// </summary>
    public abstract class GameState
    {
        protected readonly GameManager Game;

        protected GameState(GameManager game) { Game = game; }

        public abstract GameStateId Id { get; }
        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }
    }

    /// <summary>Holds the current state and performs transitions.</summary>
    public class StateMachine
    {
        public GameState Current { get; private set; }

        public void ChangeState(GameState next)
        {
            var previous = Current;
            previous?.Exit();
            Current = next;
            Current.Enter();
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(previous?.Id ?? next.Id, next.Id));
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
