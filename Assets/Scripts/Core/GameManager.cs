using System.Collections.Generic;
using ARSurvival.AR;
using ARSurvival.Data;
using ARSurvival.States;
using UnityEngine;

namespace ARSurvival.Core
{
    /// <summary>
    /// Owns the game flow (Main Menu → Placement → Playing → Game Over), the current session
    /// and the leaderboard. UI calls the public commands; everything else reacts via the EventBus.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : Singleton<GameManager>
    {
        const string DifficultyPrefsKey = "ARSurvival.Difficulty";

        [SerializeField] ARPlacementController placement;
        [SerializeField] DifficultySettings[] difficulties;

        readonly StateMachine stateMachine = new();
        MainMenuState mainMenuState;
        PlacementState placementState;
        PlayingState playingState;
        GameOverState gameOverState;
        int difficultyIndex;

        public ARPlacementController Placement => placement;
        public SessionStats Session { get; } = new();
        public Leaderboard Leaderboard { get; private set; }
        public GameStateId CurrentState => stateMachine.Current.Id;

        public IReadOnlyList<DifficultySettings> Difficulties => difficulties;
        public int DifficultyIndex => difficultyIndex;
        public DifficultySettings Difficulty => difficulties[difficultyIndex];

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return;

            Leaderboard = new Leaderboard();
            difficultyIndex = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefsKey, 0), 0, difficulties.Length - 1);

            mainMenuState = new MainMenuState(this);
            placementState = new PlacementState(this);
            playingState = new PlayingState(this);
            gameOverState = new GameOverState(this);
        }

        void Start() => stateMachine.ChangeState(mainMenuState);

        void Update() => stateMachine.Tick(Time.deltaTime);

        // --- Commands (called by UI buttons) ---

        /// <summary>Start button: place the arena first if it hasn't been placed yet.</summary>
        public void StartGame()
        {
            if (placement.IsPlaced)
                BeginRound();
            else
                stateMachine.ChangeState(placementState);
        }

        public void RestartGame() => BeginRound();

        /// <summary>Back to the start menu. Quitting mid-round ends (and records) the round first.</summary>
        public void ReturnToMenu()
        {
            EndRound(survived: false);
            stateMachine.ChangeState(mainMenuState);
        }

        public void SetDifficulty(int index)
        {
            difficultyIndex = Mathf.Clamp(index, 0, difficulties.Length - 1);
            PlayerPrefs.SetInt(DifficultyPrefsKey, difficultyIndex);
        }

        // --- Used by states ---

        internal void BeginRound() => stateMachine.ChangeState(playingState);

        internal void EndRound(bool survived)
        {
            if (stateMachine.Current != playingState)
                return;
            gameOverState.Survived = survived;
            stateMachine.ChangeState(gameOverState);
        }

        /// <summary>
        /// Shows/hides the arena. Toggles the children, not the root, so the root's ARAnchor keeps tracking.
        /// </summary>
        internal void SetWorldVisible(bool visible)
        {
            if (placement.PlacedWorld == null)
                return;
            foreach (Transform child in placement.PlacedWorld)
                child.gameObject.SetActive(visible);
        }
    }
}
