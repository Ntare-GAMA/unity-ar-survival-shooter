using ARSurvival.Data;
using UnityEngine;

namespace ARSurvival.Core
{
    public enum GameStateId { MainMenu, Placement, Playing, GameOver }
    public enum EnemyKind { Melee, Shooter }

    // --- Game flow ---
    public readonly struct GameStateChangedEvent : IGameEvent
    {
        public readonly GameStateId Previous;
        public readonly GameStateId Current;
        public GameStateChangedEvent(GameStateId previous, GameStateId current) { Previous = previous; Current = current; }
    }

    public readonly struct WorldPlacedEvent : IGameEvent
    {
        public readonly Transform World;
        public WorldPlacedEvent(Transform world) { World = world; }
    }

    public readonly struct RoundStartedEvent : IGameEvent
    {
        public readonly DifficultySettings Difficulty;
        public readonly Transform World;
        public RoundStartedEvent(DifficultySettings difficulty, Transform world) { Difficulty = difficulty; World = world; }
    }

    /// <summary>Raised when a round ends for any reason; listeners wipe enemies and projectiles.</summary>
    public readonly struct RoundEndedEvent : IGameEvent
    {
        public readonly SessionResult Result;
        public RoundEndedEvent(SessionResult result) { Result = result; }
    }

    public readonly struct ScoreChangedEvent : IGameEvent
    {
        public readonly int Score;
        public ScoreChangedEvent(int score) { Score = score; }
    }

    public readonly struct TimerChangedEvent : IGameEvent
    {
        public readonly float Remaining;
        public readonly float Duration;
        public TimerChangedEvent(float remaining, float duration) { Remaining = remaining; Duration = duration; }
    }

    // --- Player ---
    public readonly struct PlayerHealthChangedEvent : IGameEvent
    {
        public readonly int Current;
        public readonly int Max;
        public PlayerHealthChangedEvent(int current, int max) { Current = current; Max = max; }
    }

    public readonly struct PlayerDamagedEvent : IGameEvent
    {
        public readonly int Amount;
        public PlayerDamagedEvent(int amount) { Amount = amount; }
    }

    public readonly struct PlayerDiedEvent : IGameEvent { }

    public readonly struct PlayerShotEvent : IGameEvent
    {
        public readonly Vector3 Position;
        public PlayerShotEvent(Vector3 position) { Position = position; }
    }

    // --- Enemies ---
    public readonly struct EnemySpawnedEvent : IGameEvent
    {
        public readonly EnemyKind Kind;
        public readonly Vector3 Position;
        public EnemySpawnedEvent(EnemyKind kind, Vector3 position) { Kind = kind; Position = position; }
    }

    public readonly struct EnemyDamagedEvent : IGameEvent
    {
        public readonly EnemyKind Kind;
        public readonly Vector3 Position;
        public EnemyDamagedEvent(EnemyKind kind, Vector3 position) { Kind = kind; Position = position; }
    }

    public readonly struct EnemyKilledEvent : IGameEvent
    {
        public readonly EnemyKind Kind;
        public readonly int ScoreValue;
        public readonly Vector3 Position;
        public EnemyKilledEvent(EnemyKind kind, int scoreValue, Vector3 position) { Kind = kind; ScoreValue = scoreValue; Position = position; }
    }

    public readonly struct EnemyShotEvent : IGameEvent
    {
        public readonly Vector3 Position;
        public EnemyShotEvent(Vector3 position) { Position = position; }
    }

    public readonly struct MeleeAttackEvent : IGameEvent
    {
        public readonly Vector3 Position;
        public MeleeAttackEvent(Vector3 position) { Position = position; }
    }
}
