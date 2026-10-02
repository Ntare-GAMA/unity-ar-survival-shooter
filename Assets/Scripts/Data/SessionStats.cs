using System;

namespace ARSurvival.Data
{
    /// <summary>Live stats for the round in progress. Only the GameManager's states write to it.</summary>
    public class SessionStats
    {
        public int Score { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public float TimeSurvived { get; private set; }
        public float Duration { get; private set; }
        public float TimeRemaining => Math.Max(0f, Duration - TimeSurvived);
        public bool IsTimeUp => TimeSurvived >= Duration;

        public void Reset(float duration)
        {
            Score = 0;
            EnemiesDefeated = 0;
            TimeSurvived = 0f;
            Duration = duration;
        }

        public void Tick(float deltaTime) => TimeSurvived = Math.Min(Duration, TimeSurvived + deltaTime);

        public void AddKill(int scoreValue)
        {
            EnemiesDefeated++;
            Score += scoreValue;
        }

        public SessionResult ToResult(bool survived, string difficulty) => new()
        {
            score = Score,
            enemiesDefeated = EnemiesDefeated,
            timeSurvived = TimeSurvived,
            survived = survived,
            difficulty = difficulty,
            playedAt = DateTime.Now.ToString("s"),
        };
    }
}
