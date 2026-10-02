using UnityEngine;

namespace ARSurvival.Enemies
{
    /// <summary>
    /// Thin bridge between enemy behaviour and its Animator, so <see cref="Enemy"/> never deals with
    /// parameter names. The controller exposes: Speed (float), Attack (trigger), Hit (trigger),
    /// Dead (bool).
    /// </summary>
    public class EnemyAnimator : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int AttackId = Animator.StringToHash("Attack");
        static readonly int HitId = Animator.StringToHash("Hit");
        static readonly int DeadId = Animator.StringToHash("Dead");

        [SerializeField] Animator animator;
        [Tooltip("How long the death animation plays before the body is removed.")]
        [SerializeField] float deathDuration = 1.3f;

        public float DeathDuration => deathDuration;

        /// <summary>Back to the idle pose, e.g. when a pooled enemy is reused.</summary>
        public void ResetState()
        {
            animator.Rebind();
            animator.SetBool(DeadId, false);
            animator.Update(0f);
        }

        /// <param name="normalizedSpeed">0 = standing still, 1 = full movement speed.</param>
        public void SetSpeed(float normalizedSpeed) => animator.SetFloat(SpeedId, normalizedSpeed, 0.1f, Time.deltaTime);

        public void PlayAttack() => animator.SetTrigger(AttackId);
        public void PlayHit() => animator.SetTrigger(HitId);
        public void PlayDeath() => animator.SetBool(DeadId, true);
    }
}
