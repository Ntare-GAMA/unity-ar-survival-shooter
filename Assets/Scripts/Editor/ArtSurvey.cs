using System.Text;
using UnityEditor;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>Dev tool: builds the enemy prefabs from art and renders them in key poses for review.</summary>
    public static class ArtSurvey
    {
        /// <summary>Renders the raw fish models (to check which way they face) and logs their bounds.</summary>
        public static void Fish()
        {
            var log = new StringBuilder();
            var shots = new System.Collections.Generic.List<ArtPreview.Shot>();
            foreach (var name in new[] { "FishV1", "FishV2", "FishV3", "FishV4", "SharkV1" })
                shots.Add(new() { File = "fish_" + name, Asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Alstra Infinite/Fish - PolyPack/Prefabs/{name}.prefab") });
            ArtPreview.Render(shots, log);
            ArtPreview.WriteLog(log);
        }

        /// <summary>Renders the arena (with environment) with every fish posed 4 s into its swim.</summary>
        public static void World()
        {
            var log = new StringBuilder();
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            var world = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameWorld.prefab"));
            foreach (var fish in world.GetComponentsInChildren<ARSurvival.Environment.FishSwimmer>())
                fish.PoseAt(4f);
            ArtPreview.Render(new[] { new ArtPreview.Shot { File = "world", Asset = world } }, log, newScene: false);
            ArtPreview.WriteLog(log);
        }

        public static void Run()
        {
            CharacterArtBuilder.BuildAll();

            var log = new StringBuilder();
            var melee = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeleeEnemy.prefab");
            var shooter = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ShooterEnemy.prefab");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            const string orkAnim = "Assets/OrkDestroyer2/Animation/Ork_destroyer2@";
            const string maleAnim = "Assets/ToonyTinyPeople/TT_demo/animation/male/";
            const string raptor = "Assets/PBRVelociraptor/Models/5K/Raptor_Animated_FBX_5K.fbx#RaptorArmature|";

            ArtPreview.Render(new ArtPreview.Shot[]
            {
                new() { File = "player_idle", Asset = player, Clip = ArtImport.Clip(maleAnim + "m_pistol_idle_A.FBX"), ClipTime = 0.2f },
                new() { File = "player_run", Asset = player, Clip = ArtImport.Clip(maleAnim + "m_pistol_run.FBX"), ClipTime = 0.3f },
                new() { File = "player_aim", Asset = player, Clip = ArtImport.Clip(maleAnim + "m_pistol_shoot.FBX"), ClipTime = 0.2f },
                new() { File = "player_death", Asset = player, Clip = ArtImport.Clip(maleAnim + "m_death_A.FBX"), ClipTime = 1f },
                new() { File = "melee_idle", Asset = melee, Clip = ArtImport.Clip(raptor + "Raptor_Idle1_Anim"), ClipTime = 0.2f },
                new() { File = "melee_run", Asset = melee, Clip = ArtImport.Clip(raptor + "Raptor_Run1_Anim"), ClipTime = 0.3f },
                new() { File = "melee_attack", Asset = melee, Clip = ArtImport.Clip(raptor + "Raptor_Bite1_Anim"), ClipTime = 0.35f },
                new() { File = "melee_death", Asset = melee, Clip = ArtImport.Clip(raptor + "Raptor_Death1_Anim"), ClipTime = 1f },
                new() { File = "shooter_idle", Asset = shooter, Clip = ArtImport.Clip(orkAnim + "idle1.fbx"), ClipTime = 0.2f },
                new() { File = "shooter_walk", Asset = shooter, Clip = ArtImport.Clip(orkAnim + "walk1.fbx"), ClipTime = 0.3f },
                new() { File = "shooter_death", Asset = shooter, Clip = ArtImport.Clip(orkAnim + "death1.fbx"), ClipTime = 1f },
            }, log);
            ArtPreview.WriteLog(log);
        }
    }
}
