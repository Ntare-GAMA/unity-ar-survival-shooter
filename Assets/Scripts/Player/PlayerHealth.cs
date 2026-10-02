using ARSurvival.Combat;
using ARSurvival.Core;
using UnityEngine;

namespace ARSurvival.Player
{
    /// <summary>
    /// Player hit points. Publishes health, damage and death events; the HUD, audio and game state
    /// react to those without referencing the player.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] int maxHealth = 20;
        [Tooltip("Brief immunity after a hit so overlapping attacks don't land in the same instant.")]
        [SerializeField] float invulnerabilityTime = 0.2f;
        [SerializeField] HitFlash hitFlash;

        Collider bodyCollider;
        float invulnerableUntil;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public Team Team => Team.Player;
        public bool IsAlive => Current > 0;
        public Vector3 AimPoint => bodyCollider != null ? bodyCollider.bounds.center : transform.position;

        void Awake()
        {
            bodyCollider = GetComponent<Collider>();
            Current = maxHealth;
        }

        void OnEnable() => EventBus<RoundStartedEvent>.Subscribe(OnRoundStarted);
        void OnDisable() => EventBus<RoundStartedEvent>.Unsubscribe(OnRoundStarted);

        void OnRoundStarted(RoundStartedEvent e)
        {
            Current = maxHealth;
            invulnerableUntil = 0f;
            if (hitFlash != null)
                hitFlash.Clear();
            RaiseHealthChanged();
        }

        public void TakeDamage(int amount, Vector3 hitPoint)
        {
            if (!IsAlive || Time.time < invulnerableUntil)
                return;

            Current = Mathf.Max(0, Current - amount);
            invulnerableUntil = Time.time + invulnerabilityTime;
            if (hitFlash != null)
                hitFlash.Flash();

            EventBus<PlayerDamagedEvent>.Raise(new PlayerDamagedEvent(amount));
            RaiseHealthChanged();

            if (Current == 0)
                EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent());
        }

        void RaiseHealthChanged() =>
            EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(Current, maxHealth));
    }
}
