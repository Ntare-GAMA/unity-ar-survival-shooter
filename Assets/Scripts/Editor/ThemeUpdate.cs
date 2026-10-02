using UnityEditor;

namespace ARSurvival.EditorTools
{
    /// <summary>One-click (or batch-mode) rebuild of the art-driven parts: enemies, theme and UI.</summary>
    public static class ThemeUpdate
    {
        [MenuItem("Tools/AR Survival/Rebuild Enemies, Theme and UI")]
        public static void Run()
        {
            CharacterArtBuilder.BuildAll();
            EnvironmentBuilder.Build();  // water lagoon + swimming fish inside the arena prefab
            ARSceneSetup.SetupScene();   // re-links factory prefabs, restores the player if missing
            OceanTheme.Apply();
            UIBuilder.RebuildAndSave();
            SoundGenerator.GenerateInScene(); // sound effects + underwater music on the AudioManager
        }
    }
}
