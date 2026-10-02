using System;
using System.Collections;
using System.Linq;
using ARSurvival.AR;
using ARSurvival.Combat;
using ARSurvival.Core;
using ARSurvival.Enemies;
using ARSurvival.Player;
using ARSurvival.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ARSurvival.Tests
{
    /// <summary>
    /// End-to-end smoke test of the game loop in the real scene, without an AR device: the arena
    /// is placed programmatically instead of by a tap on a detected plane.
    /// </summary>
    public class GameLoopTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            // No AR device in the editor: AR components may log errors that are irrelevant here.
            LogAssert.ignoreFailingMessages = true;
#if UNITY_EDITOR
            // The game scene is the first enabled scene in Build Settings.
            var scenePath = System.Array.Find(UnityEditor.EditorBuildSettings.scenes, s => s.enabled).path;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullRound_SpawnsFightsEndsAndRestarts()
        {
            var game = GameManager.Instance;
            Assert.IsNotNull(game, "GameManager missing from scene");
            Assert.AreEqual(GameStateId.MainMenu, game.CurrentState);
            int leaderboardBefore = game.Leaderboard.Entries.Count;

            AssertOnlyVisible<MainMenuScreen>();

            // Start → Placement → (place) → Playing
            game.StartGame();
            Assert.AreEqual(GameStateId.Placement, game.CurrentState);
            AssertOnlyVisible<PlacementScreen>();

            var placement = UnityEngine.Object.FindAnyObjectByType<ARPlacementController>();
            placement.PlaceWorld(new Pose(new Vector3(0f, -1f, 1.5f), Quaternion.identity));
            Assert.AreEqual(GameStateId.Playing, game.CurrentState);
            AssertOnlyVisible<HudScreen>();

            var player = placement.PlacedWorld.GetComponentInChildren<PlayerHealth>();
            Assert.IsNotNull(player, "Player missing from placed world");
            Assert.AreEqual(player.Max, player.Current);

            var pools = UnityEngine.Object.FindObjectsByType<ProjectilePool>();
            int pooledObjectsBefore = CountChildren(pools);

            // Enemies spawn on the arena, starting with a small number
            yield return WaitUntil(() => Enemy.Active.Count > 0, 5f, "no enemy spawned");
            var spawner = UnityEngine.Object.FindAnyObjectByType<EnemySpawner>();
            Assert.LessOrEqual(Enemy.Active.Count, spawner.CurrentCap, "spawner must respect the current cap");
            var enemy = Enemy.Active[0];
            Assert.AreEqual(placement.PlacedWorld.position.y, enemy.transform.position.y, 0.001f,
                "enemy should spawn on the placed plane");

            // Manual aim only: with the aim stick untouched, the player never fires on their own.
            var bulletPool = ProjectilePool.For(Team.Player);
            Assert.AreEqual(0, bulletPool.ActiveCount, "player must not fire without aiming");

            // Player bullets (from the pool) kill it and award score
            int scoreBefore = game.Session.Score;
            int shotsFired = 0;
            while (enemy.IsAlive && shotsFired < 20)
            {
                var origin = enemy.AimPoint + Vector3.back * 0.3f;
                bulletPool.Fire(origin, enemy.AimPoint - origin, 1);
                shotsFired++;
                yield return new WaitForSeconds(0.25f);
            }
            Assert.IsFalse(enemy.IsAlive, "enemy should die from pooled bullets");
            Assert.GreaterOrEqual(shotsFired, enemy.MaxHealth, "each bullet deals one damage");
            Assert.Greater(game.Session.Score, scoreBefore);
            Assert.GreaterOrEqual(game.Session.EnemiesDefeated, 1);

            // Enemies reach and hurt the player
            yield return WaitUntil(() => player.Current < player.Max, 15f, "player never took damage");

            // Death → Game Over: everything wiped, session saved
            yield return new WaitForSeconds(0.3f); // outlast post-hit invulnerability
            player.TakeDamage(9999, player.AimPoint);
            yield return null;
            Assert.AreEqual(GameStateId.GameOver, game.CurrentState);
            AssertOnlyVisible<GameOverScreen>();
            Assert.AreEqual(0, Enemy.Active.Count, "enemies must be wiped at game over");
            Assert.IsTrue(pools.All(p => p.ActiveCount == 0), "projectiles must be wiped at game over");
            Assert.AreEqual(Math.Min(leaderboardBefore + 1, 5), game.Leaderboard.Entries.Count);
            Assert.IsFalse(game.Leaderboard.Entries[0].survived);

            Assert.AreEqual(pooledObjectsBefore, CountChildren(pools), "pools must not grow during play");

            // Restart → fresh round
            game.RestartGame();
            Assert.AreEqual(GameStateId.Playing, game.CurrentState);
            Assert.AreEqual(player.Max, player.Current);
            Assert.AreEqual(0, game.Session.Score);

            game.ReturnToMenu();
            Assert.AreEqual(GameStateId.MainMenu, game.CurrentState);
            AssertOnlyVisible<MainMenuScreen>();
            Assert.AreEqual(0, Enemy.Active.Count, "quitting mid-round must wipe enemies");
        }

        static void AssertOnlyVisible<T>() where T : UIScreen
        {
            foreach (var screen in UnityEngine.Object.FindObjectsByType<UIScreen>())
                Assert.AreEqual(screen is T, screen.IsVisible, $"{screen.name} visibility (expected only {typeof(T).Name})");
        }

        [Test]
        public void EnemyTypes_NeedDifferentBulletCountsAndRanges()
        {
            var factory = UnityEngine.Object.FindAnyObjectByType<EnemyFactory>();
            Assert.IsNotNull(factory);
#if UNITY_EDITOR
            var melee = UnityEditor.AssetDatabase.LoadAssetAtPath<MeleeEnemy>("Assets/Prefabs/MeleeEnemy.prefab");
            var shooter = UnityEditor.AssetDatabase.LoadAssetAtPath<ShooterEnemy>("Assets/Prefabs/ShooterEnemy.prefab");
            var meleeSo = new UnityEditor.SerializedObject(melee);
            var shooterSo = new UnityEditor.SerializedObject(shooter);
            Assert.AreNotEqual(meleeSo.FindProperty("baseHealth").intValue, shooterSo.FindProperty("baseHealth").intValue);
            Assert.Greater(shooterSo.FindProperty("attackRange").floatValue, meleeSo.FindProperty("attackRange").floatValue);
#endif
        }

        [Test]
        public void EnemyCap_RampsUpFromFewAndNeverExceedsMax()
        {
            foreach (var difficulty in GameManager.Instance.Difficulties)
            {
                Assert.LessOrEqual(difficulty.MaxAliveAt(0f), 2, $"{difficulty.DisplayName} should start with few enemies");
                Assert.AreEqual(difficulty.MaxAliveEnemies, difficulty.MaxAliveAt(1f), $"{difficulty.DisplayName} reaches its cap");
                int previous = 0;
                for (float p = 0f; p <= 1f; p += 0.05f)
                {
                    int cap = difficulty.MaxAliveAt(p);
                    Assert.GreaterOrEqual(cap, previous, "cap never shrinks during a round");
                    Assert.LessOrEqual(cap, difficulty.MaxAliveEnemies);
                    previous = cap;
                }
            }
        }

        static int CountChildren(ProjectilePool[] pools) => pools.Sum(p => p.transform.childCount);

        static IEnumerator WaitUntil(Func<bool> condition, float timeout, string failure)
        {
            float end = Time.time + timeout;
            while (!condition())
            {
                if (Time.time > end)
                    Assert.Fail($"Timed out: {failure}");
                yield return null;
            }
        }
    }
}
