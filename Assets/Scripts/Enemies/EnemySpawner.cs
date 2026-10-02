using System.Collections.Generic;
using ARSurvival.Combat;
using ARSurvival.Core;
using ARSurvival.Data;
using ARSurvival.Player;
using UnityEngine;

namespace ARSurvival.Enemies
{
    /// <summary>
    /// Spawns enemies at the placed arena's spawn points during a round, using the difficulty's
    /// interval and melee/shooter mix. The number alive at once ramps up over the round (starting
    /// with a single enemy). Wipes all enemies when the round ends.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        const string SpawnPointsName = "EnemySpawnPoints";

        [SerializeField] EnemyFactory factory;
        [SerializeField] float firstSpawnDelay = 1f;
        [Tooltip("Spawn points closer than this to the player are skipped.")]
        [SerializeField] float minDistanceFromPlayer = 0.5f;

        readonly List<Transform> spawnPoints = new();
        readonly List<Transform> candidates = new();
        DifficultySettings difficulty;
        PlayerHealth player;
        float timer;
        float elapsed;
        bool running;

        /// <summary>Current cap on living enemies (grows over the round).</summary>
        public int CurrentCap => difficulty != null ? difficulty.MaxAliveAt(elapsed / difficulty.RoundDuration) : 0;

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

        void OnRoundStarted(RoundStartedEvent e)
        {
            factory.ReleaseAll();
            difficulty = e.Difficulty;
            player = e.World.GetComponentInChildren<PlayerHealth>(true);

            spawnPoints.Clear();
            var root = e.World.Find(SpawnPointsName);
            if (root != null)
                foreach (Transform point in root)
                    spawnPoints.Add(point);

            timer = firstSpawnDelay;
            elapsed = 0f;
            running = player != null && spawnPoints.Count > 0;
            if (!running)
                Debug.LogError("EnemySpawner: placed world needs a PlayerHealth and EnemySpawnPoints.", e.World);
        }

        void OnRoundEnded(RoundEndedEvent e)
        {
            running = false;
            factory.ReleaseAll();
        }

        void Update()
        {
            if (!running)
                return;

            elapsed += Time.deltaTime;
            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            timer = difficulty.SpawnInterval;
            if (factory.ActiveCount < CurrentCap)
                SpawnOne();
        }

        void SpawnOne()
        {
            var point = PickSpawnPoint();
            var kind = Random.value < difficulty.ShooterSpawnChance ? EnemyKind.Shooter : EnemyKind.Melee;
            var facePlayer = Vector3.ProjectOnPlane(player.transform.position - point.position, Vector3.up);
            var rotation = facePlayer.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facePlayer) : point.rotation;
            factory.Create(kind, point.position, rotation, player, difficulty);
        }

        Transform PickSpawnPoint()
        {
            candidates.Clear();
            float minSqr = minDistanceFromPlayer * minDistanceFromPlayer;
            foreach (var point in spawnPoints)
                if ((point.position - player.transform.position).sqrMagnitude >= minSqr)
                    candidates.Add(point);

            var pool = candidates.Count > 0 ? candidates : spawnPoints;
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
