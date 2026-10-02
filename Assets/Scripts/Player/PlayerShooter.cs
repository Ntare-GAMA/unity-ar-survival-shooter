using ARSurvival.Combat;
using ARSurvival.Core;
using UnityEngine;

namespace ARSurvival.Player
{
    /// <summary>
    /// Fires pooled bullets in the direction the player is aiming with the right stick, at a
    /// fixed fire rate, for as long as the stick is held.
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] Transform muzzle;
        [SerializeField] MuzzleFlash muzzleFlash;
        [SerializeField, Min(0.1f)] float fireRate = 5f;
        [SerializeField, Min(1)] int damage = 1;

        float cooldown;

        public void ResetCooldown() => cooldown = 0f;

        /// <param name="aimDirection">World-space horizontal direction, or zero when not aiming.</param>
        public void Tick(Vector3 aimDirection, float deltaTime)
        {
            cooldown -= deltaTime;
            if (aimDirection != Vector3.zero && cooldown <= 0f)
                Fire(aimDirection);
        }

        void Fire(Vector3 direction)
        {
            var pool = ProjectilePool.For(Team.Player);
            if (pool == null)
                return;

            cooldown = 1f / fireRate;
            pool.Fire(muzzle.position, direction, damage);
            if (muzzleFlash != null)
                muzzleFlash.Play();
            EventBus<PlayerShotEvent>.Raise(new PlayerShotEvent(muzzle.position));
        }
    }
}
