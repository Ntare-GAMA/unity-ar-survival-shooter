using System.Collections;
using System.Collections.Generic;
using ARSurvival.Combat;
using ARSurvival.Core;
using ARSurvival.Data;
using ARSurvival.Pooling;
using UnityEngine;

namespace ARSurvival.Enemies
{
    /// <summary>
    /// Base class for every enemy (inheritance + template method). Handles health, hit feedback,
    /// scoring, chasing the player and attack cooldowns. Subclasses only decide how close to get
    /// (<see cref="StopDistance"/>) and what an attack does (<see cref="PerformAttack"/>).
    /// Enemies are pooled by the <see cref="EnemyFactory"/>, never destroyed.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class Enemy : MonoBehaviour, IDamageable, IPoolable
    {
        static readonly List<Enemy> s_Active = new();

        /// <summary>Enemies currently spawned (used by the player's auto-aim).</summary>
        public static IReadOnlyList<Enemy> Active => s_Active;

        [Header("Stats (Easy difficulty values)")]
        [SerializeField, Min(1)] int baseHealth = 2;
        [SerializeField, Min(1)] int baseAttackDamage = 1;
        [SerializeField] float moveSpeed = 0.3f;
        [SerializeField] float attackRange = 0.15f;
        [SerializeField] float attackCooldown = 1f;
        [SerializeField] int scoreValue = 10;

        [Header("Movement")]
        [SerializeField] float turnSpeed = 540f;
        [Tooltip("Enemies closer than this push apart so they don't stack on each other.")]
        [SerializeField] float separationRadius = 0.1f;

        [Header("Feedback")]
        [SerializeField] Transform model;
        [SerializeField] HitFlash hitFlash;
        [Tooltip("Optional. Without one the enemy still works; it just isn't animated.")]
        [SerializeField] EnemyAnimator animator;

        EnemyFactory factory;
        Collider bodyCollider;
        float speed;
        float cooldownTimer;
        bool dying;

        public abstract EnemyKind Kind { get; }
        public Team Team => Team.Enemy;
        public bool IsAlive => CurrentHealth > 0 && !dying;
        public Vector3 AimPoint => bodyCollider.bounds.center;
        public int CurrentHealth { get; private set; }
        public int MaxHealth { get; private set; }

        protected IDamageable Target { get; private set; }
        protected int AttackDamage { get; private set; }
        protected float AttackRange => attackRange;
        protected Transform Model => model;

        /// <summary>True while the target is still within striking distance (with a little slack).</summary>
        protected bool TargetInRange => Target != null && Target.IsAlive &&
            Vector3.ProjectOnPlane(Target.AimPoint - transform.position, Vector3.up).magnitude <= attackRange * 1.2f;

        /// <summary>Distance from the target at which this enemy stops advancing.</summary>
        protected virtual float StopDistance => attackRange * 0.8f;

        /// <summary>The enemy-specific attack, called when in range and off cooldown.</summary>
        protected abstract void PerformAttack();

        protected virtual void Awake() => bodyCollider = GetComponent<Collider>();

        /// <summary>Applies difficulty scaling and sets the target. Called by the factory after spawning.</summary>
        public void Initialize(EnemyFactory owner, IDamageable target, DifficultySettings difficulty)
        {
            factory = owner;
            Target = target;
            MaxHealth = Mathf.Max(1, Mathf.RoundToInt(baseHealth * difficulty.EnemyHealthMultiplier));
            CurrentHealth = MaxHealth;
            AttackDamage = Mathf.Max(1, Mathf.RoundToInt(baseAttackDamage * difficulty.EnemyDamageMultiplier));
            speed = moveSpeed * difficulty.EnemySpeedMultiplier;
            cooldownTimer = attackCooldown * 0.5f;

            EventBus<EnemySpawnedEvent>.Raise(new EnemySpawnedEvent(Kind, transform.position));
        }

        public virtual void OnSpawn()
        {
            dying = false;
            s_Active.Add(this);
            if (animator != null)
                animator.ResetState();
            StartCoroutine(ScaleModel(0f, 1f, 0.25f));
        }

        public virtual void OnDespawn()
        {
            s_Active.Remove(this);
            StopAllCoroutines();
            Target = null;
            factory = null;
            CurrentHealth = 0;
            if (hitFlash != null)
                hitFlash.Clear();
            if (model != null)
                model.localScale = Vector3.one;
        }

        protected virtual void Update()
        {
            if (!IsAlive)
                return;
            if (Target == null || !Target.IsAlive)
            {
                SetAnimationSpeed(0f);
                return;
            }

            cooldownTimer -= Time.deltaTime;

            var toTarget = Vector3.ProjectOnPlane(Target.AimPoint - transform.position, Vector3.up);
            float distance = toTarget.magnitude;
            Face(toTarget);

            float maxStep = speed * Time.deltaTime;
            var step = Vector3.zero;
            if (distance > StopDistance)
                step = toTarget / distance * Mathf.Min(maxStep, distance - StopDistance);
            SetAnimationSpeed(maxStep > 0f ? step.magnitude / maxStep : 0f);
            step += Separation() * maxStep;
            transform.position += step;

            if (distance <= attackRange && cooldownTimer <= 0f)
            {
                cooldownTimer = attackCooldown;
                if (animator != null)
                    animator.PlayAttack();
                PerformAttack();
            }
        }

        public void TakeDamage(int amount, Vector3 hitPoint)
        {
            if (!IsAlive)
                return;

            CurrentHealth -= amount;
            if (hitFlash != null)
                hitFlash.Flash();
            EventBus<EnemyDamagedEvent>.Raise(new EnemyDamagedEvent(Kind, hitPoint));

            if (CurrentHealth <= 0)
                Die();
            else if (animator != null)
                animator.PlayHit();
        }

        protected virtual void Die()
        {
            dying = true;
            EventBus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(Kind, scoreValue, transform.position));
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            if (animator != null)
            {
                animator.PlayDeath();
                yield return new WaitForSeconds(animator.DeathDuration);
            }
            yield return ScaleModel(1f, 0f, 0.2f);
            factory.Release(this);
        }

        void SetAnimationSpeed(float normalized)
        {
            if (animator != null)
                animator.SetSpeed(normalized);
        }

        void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;
            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        Vector3 Separation()
        {
            var push = Vector3.zero;
            var position = transform.position;
            foreach (var other in s_Active)
            {
                if (other == this)
                    continue;
                var away = Vector3.ProjectOnPlane(position - other.transform.position, Vector3.up);
                float d = away.magnitude;
                if (d > 0.0001f && d < separationRadius)
                    push += away / d * (1f - d / separationRadius);
            }
            return push;
        }

        IEnumerator ScaleModel(float from, float to, float duration)
        {
            if (model == null)
                yield break;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                model.localScale = Vector3.one * Mathf.Lerp(from, to, t);
                yield return null;
            }
            model.localScale = Vector3.one * to;
        }
    }
}
