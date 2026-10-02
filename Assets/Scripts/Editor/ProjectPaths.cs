using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ARSurvival.EditorTools
{
    /// <summary>Single source of truth for paths the editor tools share.</summary>
    public static class ProjectPaths
    {
        /// <summary>The game scene: the first enabled scene in Build Settings (survives renames).</summary>
        public static string GameScene => EditorBuildSettings.scenes.First(s => s.enabled).path;

        /// <summary>Opens the game scene unless it is already the active one.</summary>
        public static void OpenGameScene()
        {
            if (EditorSceneManager.GetActiveScene().path != GameScene)
                EditorSceneManager.OpenScene(GameScene);
        }
    }
}
