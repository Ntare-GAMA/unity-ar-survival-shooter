using ARSurvival.Combat;
using ARSurvival.Core;
using ARSurvival.Data;
using ARSurvival.Pooling;
using UnityEngine;

namespace ARSurvival.Enemies
{
    /// <summary>
    /// Factory pattern: the only place that knows how to produce each <see cref="EnemyKind"/>.
    /// Callers ask for a kind; the factory takes an instance from that kind's pre-warmed pool and
    /// initialises it. Also owns returning enemies to their pools.
    /// </summary>
    public class EnemyFactory : MonoBehaviour
    {
        [SerializeField] MeleeEnemy meleePrefab;
        [SerializeField] ShooterEnemy shooterPrefab;
        [Tooltip("Instances pre-created per enemy kind. Keep >= the highest MaxAliveEnemies.")]
        [SerializeField, Min(1)] int poolSizePerKind = 10;

        ObjectPool<Enemy> meleePool;
        ObjectPool<Enemy> shooterPool;

        public int ActiveCount => meleePool.ActiveCount + shooterPool.ActiveCount;

        void Awake()
        {
            meleePool = new ObjectPool<Enemy>(meleePrefab, poolSizePerKind, transform);
            shooterPool = new ObjectPool<Enemy>(shooterPrefab, poolSizePerKind, transform);
        }

        public Enemy Create(EnemyKind kind, Vector3 position, Quaternion rotation, IDamageable target,
            DifficultySettings difficulty)
        {
            var enemy = PoolFor(kind).Get(position, rotation);
            enemy.Initialize(this, target, difficulty);
            return enemy;
        }

        public void Release(Enemy enemy) => PoolFor(enemy.Kind).Release(enemy);

        /// <summary>Wipes every enemy from the arena.</summary>
        public void ReleaseAll()
        {
            meleePool.ReleaseAll();
            shooterPool.ReleaseAll();
        }

        ObjectPool<Enemy> PoolFor(EnemyKind kind) => kind == EnemyKind.Shooter ? shooterPool : meleePool;
    }
}
