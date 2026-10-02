using UnityEngine;

namespace ARSurvival.Data
{
    /// <summary>Gameplay tuning for one difficulty mode. Each mode is its own asset.</summary>
    [CreateAssetMenu(menuName = "AR Survival/Difficulty Settings", fileName = "Difficulty")]
    public class DifficultySettings : ScriptableObject
    {
        [SerializeField] string displayName = "Normal";

        [Header("Round")]
        [SerializeField, Min(10f)] float roundDuration = 90f;

        [Header("Spawning")]
        [SerializeField, Min(0.2f)] float spawnInterval = 3f;
        [SerializeField, Min(1)] int maxAliveEnemies = 6;
        [Tooltip("Enemies allowed at once when the round starts; the cap grows to maxAliveEnemies.")]
        [SerializeField, Min(1)] int startMaxAliveEnemies = 1;
        [Tooltip("Fraction of the round after which the full cap applies.")]
        [SerializeField, Range(0.1f, 1f)] float rampUpFraction = 0.66f;
        [SerializeField, Range(0f, 1f)] float shooterSpawnChance = 0.35f;

        [Header("Enemy multipliers")]
        [SerializeField, Min(0.1f)] float enemyHealthMultiplier = 1f;
        [SerializeField, Min(0.1f)] float enemySpeedMultiplier = 1f;
        [SerializeField, Min(0.1f)] float enemyDamageMultiplier = 1f;

        public string DisplayName => displayName;
        public float RoundDuration => roundDuration;
        public float SpawnInterval => spawnInterval;
        public int MaxAliveEnemies => maxAliveEnemies;

        /// <summary>
        /// Enemy cap at a point in the round (0 = start, 1 = end): ramps linearly from
        /// startMaxAliveEnemies to maxAliveEnemies, so rounds ease in and peak near the end.
        /// </summary>
        public int MaxAliveAt(float roundProgress)
        {
            float t = Mathf.Clamp01(roundProgress / rampUpFraction);
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(startMaxAliveEnemies, maxAliveEnemies, t)), 1, maxAliveEnemies);
        }
        public float ShooterSpawnChance => shooterSpawnChance;
        public float EnemyHealthMultiplier => enemyHealthMultiplier;
        public float EnemySpeedMultiplier => enemySpeedMultiplier;
        public float EnemyDamageMultiplier => enemyDamageMultiplier;
    }
}
