using ARSurvival.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARSurvival.UI
{
    /// <summary>
    /// End-of-round report: outcome, final score, enemies defeated and time survived.
    /// </summary>
    public class GameOverScreen : UIScreen
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text killsText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] Button restartButton;
        [SerializeField] Button leaderboardButton;
        [SerializeField] Button menuButton;
        [SerializeField] LeaderboardScreen leaderboard;

        protected override void Awake()
        {
            base.Awake();
            Bind(restartButton, () => GameManager.Instance.RestartGame());
            Bind(leaderboardButton, leaderboard.Show);
            Bind(menuButton, () => GameManager.Instance.ReturnToMenu());
        }

        void OnEnable() => EventBus<RoundEndedEvent>.Subscribe(OnRoundEnded);
        void OnDisable() => EventBus<RoundEndedEvent>.Unsubscribe(OnRoundEnded);

        void OnRoundEnded(RoundEndedEvent e)
        {
            var result = e.Result;
            titleText.text = result.survived ? "YOU SURVIVED!" : "WIPED OUT!";
            titleText.color = result.survived ? UIStyle.Accent : UIStyle.Danger;
            subtitleText.text = $"MODE: {result.difficulty.ToUpperInvariant()}";
            scoreText.text = result.score.ToString();
            killsText.text = result.enemiesDefeated.ToString();
            timeText.text = UIStyle.FormatTime(result.timeSurvived);
        }
    }
}
