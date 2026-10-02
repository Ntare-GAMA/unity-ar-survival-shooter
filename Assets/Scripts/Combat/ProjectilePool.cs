using System.Collections.Generic;
using ARSurvival.Core;
using ARSurvival.Pooling;
using UnityEngine;

namespace ARSurvival.Combat
{
    /// <summary>
    /// Scene component wrapping an <see cref="ObjectPool{T}"/> of projectiles. The scene has one
    /// for player bullets and one for enemy bullets; both are pre-warmed in Awake and cleared
    /// whenever a round starts or ends.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        static readonly Dictionary<Team, ProjectilePool> s_ByTeam = new();

        [SerializeField] Team team;
        [SerializeField] Projectile prefab;
        [SerializeField, Min(1)] int size = 40;

        ObjectPool<Projectile> pool;

        public int ActiveCount => pool.ActiveCount;

        /// <summary>
        /// The pool that fires bullets for the given team. The player and enemies live inside the
        /// placed world prefab, so they look their pool up here instead of holding scene references.
        /// </summary>
        public static ProjectilePool For(Team team) => s_ByTeam.TryGetValue(team, out var pool) ? pool : null;

        void Awake()
        {
            pool = new ObjectPool<Projectile>(prefab, size, transform);
            s_ByTeam[team] = this;
        }

        void OnDestroy()
        {
            if (s_ByTeam.TryGetValue(team, out var registered) && registered == this)
                s_ByTeam.Remove(team);
        }

        void OnEnable()
        {
            EventBus<RoundStartedEvent>.Subscribe(OnRoundStarted);
            EventBus<RoundEndedEvent>.Subscribe(OnRoundEnded);
        }

        void OnDisable()
        {
            EventBus<RoundStartedEvent>.Unsubscribe(OnRoundStarted);
            EventBus<RoundEndedEvent>.Unsubscribe(OnRoundEnded);
        }

        public Projectile Fire(Vector3 position, Vector3 direction, int damage)
        {
            var projectile = pool.Get(position, Quaternion.LookRotation(direction));
            projectile.Launch(this, direction, damage, team);
            return projectile;
        }

        public void Release(Projectile projectile) => pool.Release(projectile);

        void OnRoundStarted(RoundStartedEvent e) => pool.ReleaseAll();
        void OnRoundEnded(RoundEndedEvent e) => pool.ReleaseAll();
    }
}
