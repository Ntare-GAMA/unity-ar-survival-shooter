using System.Collections;
using ARSurvival.Core;
using UnityEngine;

namespace ARSurvival.Enemies
{
    /// <summary>
    /// Fast, fragile brute: runs straight at the player and strikes only at close range, on a
    /// cooldown. Damage lands partway into the swing, and only if the player is still in reach,
    /// so the player can dodge a strike that has already started.
    /// </summary>
    public class MeleeEnemy : Enemy
    {
        [Tooltip("Seconds from the start of the swing to the moment it connects.")]
        [SerializeField] float impactDelay = 0.35f;
        [Tooltip("Used only when there is no attack animation: a short forward lunge of the model.")]
        [SerializeField] float lungeDistance = 0.03f;
        [SerializeField] bool proceduralLunge = true;

        public override EnemyKind Kind => EnemyKind.Melee;

        protected override void PerformAttack() => StartCoroutine(Strike());

        IEnumerator Strike()
        {
            if (proceduralLunge)
                StartCoroutine(Lunge());
            yield return new WaitForSeconds(impactDelay);
            if (!IsAlive || !TargetInRange)
                yield break;

            Target.TakeDamage(AttackDamage, transform.position);
            EventBus<MeleeAttackEvent>.Raise(new MeleeAttackEvent(transform.position));
        }

        IEnumerator Lunge()
        {
            if (Model == null)
                yield break;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.18f)
            {
                Model.localPosition = Vector3.forward * (Mathf.Sin(t * Mathf.PI) * lungeDistance);
                yield return null;
            }
            Model.localPosition = Vector3.zero;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            if (Model != null)
                Model.localPosition = Vector3.zero;
        }
    }
}
