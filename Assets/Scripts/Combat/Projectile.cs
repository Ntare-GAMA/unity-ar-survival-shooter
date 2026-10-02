using ARSurvival.Pooling;
using UnityEngine;

namespace ARSurvival.Combat
{
    /// <summary>
    /// Pooled bullet used by both the player and Shooter enemies. Moves with a sphere-cast each
    /// frame (no tunnelling at AR scale), damages the first hostile it touches, then returns to
    /// its pool. Never destroyed.
    /// </summary>
    public class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] float speed = 2.5f;
        [SerializeField] float lifetime = 2f;
        [SerializeField] float radius = 0.02f;
        [SerializeField] LayerMask hitMask = ~0;

        static readonly RaycastHit[] s_Hits = new RaycastHit[8];

        ProjectilePool owner;
        TrailRenderer trail;
        Vector3 direction;
        Team team;
        int damage;
        float age;

        void Awake() => trail = GetComponentInChildren<TrailRenderer>();

        /// <summary>Configures a freshly spawned projectile. Called by its pool.</summary>
        public void Launch(ProjectilePool pool, Vector3 launchDirection, int launchDamage, Team launchTeam)
        {
            owner = pool;
            direction = launchDirection.normalized;
            damage = launchDamage;
            team = launchTeam;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        public void OnSpawn()
        {
            age = 0f;
            if (trail != null)
                trail.Clear();
        }

        public void OnDespawn()
        {
            owner = null;
            direction = Vector3.zero;
            damage = 0;
            if (trail != null)
                trail.Clear();
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                ReturnToPool();
                return;
            }

            float step = speed * Time.deltaTime;
            if (TryFindHit(step, out var hit))
            {
                var target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive)
                    target.TakeDamage(damage, hit.point);
                ReturnToPool();
                return;
            }

            transform.position += direction * step;
        }

        /// <summary>Nearest hit along this frame's path, ignoring colliders on the shooter's own team.</summary>
        bool TryFindHit(float distance, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, direction, s_Hits, distance,
                hitMask, QueryTriggerInteraction.Collide);

            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.Team == team)
                    continue;
                if (!found || hit.distance < nearest.distance)
                {
                    nearest = hit;
                    found = true;
                }
            }
            return found;
        }

        void ReturnToPool()
        {
            if (owner != null)
                owner.Release(this);
            else
                gameObject.SetActive(false);
        }
    }
}
