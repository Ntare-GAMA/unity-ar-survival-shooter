using ARSurvival.Core;
using UnityEngine;

namespace ARSurvival.UI
{
    /// <summary>
    /// Shows the screen that belongs to the current game state. Knows nothing about gameplay:
    /// it only listens for <see cref="GameStateChangedEvent"/>.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] MainMenuScreen mainMenu;
        [SerializeField] PlacementScreen placement;
        [SerializeField] HudScreen hud;
        [SerializeField] GameOverScreen gameOver;
        [SerializeField] LeaderboardScreen leaderboard;

        UIScreen[] stateScreens;

        void Awake() => stateScreens = new UIScreen[] { mainMenu, placement, hud, gameOver };

        void OnEnable() => EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
        void OnDisable() => EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);

        void OnStateChanged(GameStateChangedEvent e)
        {
            var target = ScreenFor(e.Current);
            leaderboard.Hide();
            foreach (var screen in stateScreens)
                if (screen != target)
                    screen.Hide();
            target.Show();
        }

        UIScreen ScreenFor(GameStateId state) => state switch
        {
            GameStateId.Placement => placement,
            GameStateId.Playing => hud,
            GameStateId.GameOver => gameOver,
            _ => mainMenu,
        };
    }
}
