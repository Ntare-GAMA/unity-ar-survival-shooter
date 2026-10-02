using ARSurvival.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARSurvival.UI
{
    /// <summary>Title, Start, Leaderboard and the Easy/Hard difficulty selector.</summary>
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] Button startButton;
        [SerializeField] Button leaderboardButton;
        [SerializeField] Button[] difficultyButtons;
        [SerializeField] TMP_Text difficultyHint;
        [SerializeField] LeaderboardScreen leaderboard;

        protected override void Awake()
        {
            base.Awake();
            Bind(startButton, () => GameManager.Instance.StartGame());
            Bind(leaderboardButton, leaderboard.Show);
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                int index = i;
                Bind(difficultyButtons[i], () => SelectDifficulty(index));
            }
        }

        protected override void OnShow() => RefreshDifficulty();

        void SelectDifficulty(int index)
        {
            GameManager.Instance.SetDifficulty(index);
            RefreshDifficulty();
        }

        void RefreshDifficulty()
        {
            var game = GameManager.Instance;
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                var button = difficultyButtons[i];
                bool exists = i < game.Difficulties.Count;
                button.gameObject.SetActive(exists);
                if (!exists)
                    continue;

                bool selected = i == game.DifficultyIndex;
                button.image.color = selected ? UIStyle.Accent : UIStyle.ButtonAlt;
                var label = button.GetComponentInChildren<TMP_Text>();
                label.text = game.Difficulties[i].DisplayName.ToUpperInvariant();
                label.color = selected ? UIStyle.Navy : UIStyle.TextPrimary;
            }

            var difficulty = game.Difficulty;
            difficultyHint.text = $"Survive {UIStyle.FormatTime(difficulty.RoundDuration)}  ~  " +
                                  $"up to {difficulty.MaxAliveEnemies} enemies at once";
        }
    }
}
