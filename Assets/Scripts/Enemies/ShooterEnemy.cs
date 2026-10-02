using System.Collections;
using ARSurvival.Combat;
using ARSurvival.Core;
using UnityEngine;

namespace ARSurvival.Enemies
{
    /// <summary>
    /// Ranged enemy: approaches until it reaches its shooting distance, then holds position and
    /// fires pooled projectiles from its gun's muzzle at the player, with recoil and a muzzle
    /// flash. Longer range and tougher than the melee enemy.
    /// </summary>
    public class ShooterEnemy : Enemy
    {
        [SerializeField] float shootingDistance = 0.6f;
        [SerializeField] Transform muzzle;
        [SerializeField] MuzzleFlash muzzleFlash;
        [SerializeField] float recoilDistance = 0.015f;

        public override EnemyKind Kind => EnemyKind.Shooter;

        protected override float StopDistance => shootingDistance;

        protected override void PerformAttack()
        {
            var pool = ProjectilePool.For(Team.Enemy);
            if (pool == null)
                return;

            var origin = muzzle != null ? muzzle.position : AimPoint;
            pool.Fire(origin, Target.AimPoint - origin, AttackDamage);
            if (muzzleFlash != null)
                muzzleFlash.Play();
            StartCoroutine(Recoil());
            EventBus<EnemyShotEvent>.Raise(new EnemyShotEvent(origin));
        }

        IEnumerator Recoil()
        {
            if (Model == null)
                yield break;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.15f)
            {
                // Snap back, ease forward.
                Model.localPosition = Vector3.back * (recoilDistance * (1f - t) * (1f - t));
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
