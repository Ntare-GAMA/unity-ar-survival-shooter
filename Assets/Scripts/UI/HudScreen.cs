using ARSurvival.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARSurvival.UI
{
    /// <summary>
    /// In-game HUD: segmented health, score, kills, countdown, hit marker, kill feed, round-start
    /// banner and the red damage vignette (pulsing at low health). Updated purely from EventBus
    /// events. The twin joysticks are Input System on-screen controls living under this screen.
    /// </summary>
    public class HudScreen : UIScreen
    {
        [Header("Status")]
        [SerializeField] Image[] healthSegments;
        [SerializeField] TMP_Text healthText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text killsText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] Button exitButton;

        [Header("Feedback")]
        [SerializeField] Image damageVignette;
        [SerializeField] Image hitMarker;
        [SerializeField] TMP_Text killFeed;
        [SerializeField] CanvasGroup roundBanner;
        [SerializeField] TMP_Text roundBannerSubtitle;
        [SerializeField, Range(0f, 1f)] float lowHealthThreshold = 0.3f;

        int kills;
        float healthFraction = 1f;
        float damageFlash;
        float hitMarkerTime;
        float killFeedTime;
        float bannerTime;
        float scorePunch;

        protected override void Awake()
        {
            base.Awake();
            Bind(exitButton, () => GameManager.Instance.ReturnToMenu());
            SetAlpha(damageVignette, 0f);
            SetAlpha(hitMarker, 0f);
            killFeed.alpha = 0f;
            roundBanner.alpha = 0f;
        }

        void OnEnable()
        {
            EventBus<PlayerHealthChangedEvent>.Subscribe(OnHealthChanged);
            EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);
            EventBus<ScoreChangedEvent>.Subscribe(OnScoreChanged);
            EventBus<TimerChangedEvent>.Subscribe(OnTimerChanged);
            EventBus<EnemyDamagedEvent>.Subscribe(OnEnemyDamaged);
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            EventBus<RoundStartedEvent>.Subscribe(OnRoundStarted);
        }

        void OnDisable()
        {
            EventBus<PlayerHealthChangedEvent>.Unsubscribe(OnHealthChanged);
            EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
            EventBus<ScoreChangedEvent>.Unsubscribe(OnScoreChanged);
            EventBus<TimerChangedEvent>.Unsubscribe(OnTimerChanged);
            EventBus<EnemyDamagedEvent>.Unsubscribe(OnEnemyDamaged);
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            EventBus<RoundStartedEvent>.Unsubscribe(OnRoundStarted);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Damage flash decays; below the threshold a slow pulse keeps the edges red.
            damageFlash = Mathf.MoveTowards(damageFlash, 0f, 2f * dt);
            float lowHealthPulse = healthFraction > 0f && healthFraction <= lowHealthThreshold
                ? 0.2f + 0.15f * Mathf.Sin(Time.time * 5f)
                : 0f;
            SetAlpha(damageVignette, Mathf.Max(damageFlash, lowHealthPulse));

            hitMarkerTime = Mathf.MoveTowards(hitMarkerTime, 0f, dt);
            SetAlpha(hitMarker, hitMarkerTime / 0.15f);
            hitMarker.rectTransform.localScale = Vector3.one * (1f + hitMarkerTime * 2f);

            killFeedTime = Mathf.MoveTowards(killFeedTime, 0f, dt);
            killFeed.alpha = Mathf.Clamp01(killFeedTime / 0.4f);

            bannerTime = Mathf.MoveTowards(bannerTime, 0f, dt);
            roundBanner.alpha = Mathf.Clamp01(bannerTime / 0.5f);

            if (scorePunch > 0f)
            {
                scorePunch = Mathf.MoveTowards(scorePunch, 0f, 4f * dt);
                scoreText.rectTransform.localScale = Vector3.one * (1f + 0.25f * scorePunch);
            }
        }

        void OnRoundStarted(RoundStartedEvent e)
        {
            kills = 0;
            killsText.text = "KILLS  0";
            damageFlash = 0f;
            killFeedTime = 0f;
            bannerTime = 2.2f;
            roundBannerSubtitle.text = $"SURVIVE {UIStyle.FormatTime(e.Difficulty.RoundDuration)}  ~  {e.Difficulty.DisplayName.ToUpperInvariant()}";
        }

        void OnHealthChanged(PlayerHealthChangedEvent e)
        {
            healthFraction = e.Max > 0 ? (float)e.Current / e.Max : 0f;
            var onColor = healthFraction > 0.5f ? UIStyle.Pink : healthFraction > lowHealthThreshold ? UIStyle.Warning : UIStyle.Danger;
            int lit = Mathf.CeilToInt(healthFraction * healthSegments.Length);
            for (int i = 0; i < healthSegments.Length; i++)
                healthSegments[i].color = i < lit ? onColor : UIStyle.SegmentOff;
            healthText.text = e.Current.ToString();
            healthText.color = healthFraction > 0.5f ? UIStyle.TextPrimary : onColor;
        }

        void OnPlayerDamaged(PlayerDamagedEvent e) => damageFlash = 0.65f;

        void OnEnemyDamaged(EnemyDamagedEvent e) => hitMarkerTime = 0.15f;

        void OnScoreChanged(ScoreChangedEvent e)
        {
            scoreText.text = e.Score.ToString();
            if (e.Score > 0)
                scorePunch = 1f;
        }

        void OnEnemyKilled(EnemyKilledEvent e)
        {
            killsText.text = $"KILLS  {++kills}";
            string what = e.Kind == EnemyKind.Shooter ? "ORK BLASTED!" : "RAPTOR DOWN!";
            killFeed.text = $"{what}  <color=#FFDE33>+{e.ScoreValue}</color>";
            killFeedTime = 1.4f;
        }

        void OnTimerChanged(TimerChangedEvent e)
        {
            timerText.text = UIStyle.FormatTime(e.Remaining);
            timerText.color = e.Remaining <= 10f ? UIStyle.Danger : UIStyle.TextPrimary;
        }

        static void SetAlpha(Graphic graphic, float alpha)
        {
            var c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}
