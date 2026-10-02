using ARSurvival.Combat;
using ARSurvival.Data;
using UnityEditor;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Applies the "ocean night" cartoon look to the scene art that the UI builder doesn't own:
    /// difficulty names, arena and player colours, bullets, plane-tracker outline, hit flash.
    /// </summary>
    public static class OceanTheme
    {
        static readonly Color Teal = new(0.3f, 0.95f, 0.9f);

        public static void Apply()
        {
            Rename("Assets/Data/Difficulty/Difficulty_Easy.asset", "Shallows");
            Rename("Assets/Data/Difficulty/Difficulty_Hard.asset", "Deep Sea");

            Tint("Assets/Art/Arena.mat", new Color(0.93f, 0.86f, 0.66f));                // sandy seabed
            Tint("Assets/Art/Materials/Player_Body.mat", new Color(1f, 0.87f, 0.25f));    // sunny yellow
            Tint("Assets/Art/Materials/Player_Head.mat", new Color(1f, 0.93f, 0.55f));
            Bullet("PlayerBullet", new Color(0.45f, 0.95f, 1f));                          // cyan shots
            Bullet("EnemyBullet", new Color(1f, 0.4f, 0.55f));                            // coral pink

            EditPrefab("Assets/Prefabs/CustomPlaneTracker.prefab", root =>
            {
                var line = root.GetComponent<LineRenderer>();
                line.startColor = line.endColor = Teal;
            });
            EditPrefab("Assets/Prefabs/Player.prefab", root =>
            {
                var so = new SerializedObject(root.GetComponent<HitFlash>());
                so.FindProperty("flashColor").colorValue = new Color(1f, 0.35f, 0.4f);
                so.ApplyModifiedPropertiesWithoutUndo();
            });
            AssetDatabase.SaveAssets();
        }

        static void Bullet(string name, Color color)
        {
            Tint($"Assets/Art/{name}.mat", color);
            EditPrefab($"Assets/Prefabs/{name}.prefab", root =>
            {
                var trail = root.GetComponent<TrailRenderer>();
                trail.startColor = color;
                trail.endColor = new Color(color.r, color.g, color.b, 0f);
            });
        }

        static void Rename(string path, string displayName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<DifficultySettings>(path);
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Tint(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
        }

        static void EditPrefab(string path, System.Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
