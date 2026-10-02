using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Keeps the repository small: deletes everything in the imported character packs that neither
    /// the game nor the character builder uses (found through Unity's own dependency graph), removes
    /// the retired player asset, and converts large uncompressed .tga textures to 1024 px .png.
    /// </summary>
    public static class PackPruner
    {
        static readonly string[] Packs =
        {
            "Assets/BodyGuards", "Assets/PBRVelociraptor", "Assets/ToonyTinyPeople",
            "Assets/PolyOne", "Assets/Alstra Infinite",
        };
        static readonly string[] Retired = { "Assets/Elizabeth Warren caricature", "Assets/New folder" };

        /// <summary>What the game and the builder need: the game scene, the built prefabs and the builder's sources.</summary>
        static readonly string[] Roots =
        {
            "Assets/Prefabs/Player.prefab",
            "Assets/Prefabs/MeleeEnemy.prefab",
            "Assets/Prefabs/ShooterEnemy.prefab",
            "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_01.fbx",
            "Assets/BodyGuards/Textures/Boduguard_01_D.png",
            "Assets/PBRVelociraptor/Prefabs/Mobile/5K/Raptor_Animated_FBX_5K_Green.prefab",
            "Assets/PBRVelociraptor/Models/5K/Raptor_Animated_FBX_5K.fbx",
            // Environment builder sources: the water material/shader and the fish prefabs.
            "Assets/PolyOne/Water URP/Materials/M_Shader Water.mat",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV1.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV2.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV3.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV4.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/SharkV1.prefab",
        };

        [MenuItem("Tools/AR Survival/Prune Unused Pack Assets")]
        public static void Run()
        {
            var log = new StringBuilder();
            foreach (var path in Retired)
                if (AssetDatabase.DeleteAsset(path))
                    log.AppendLine($"removed {path}");

            var roots = Roots.Append(ProjectPaths.GameScene).Where(p => File.Exists(p)).ToArray();
            var used = new HashSet<string>(AssetDatabase.GetDependencies(roots, true));
            long before = Packs.Sum(FolderBytes);

            foreach (var pack in Packs)
            {
                foreach (var path in AssetDatabase.FindAssets("", new[] { pack })
                             .Select(AssetDatabase.GUIDToAssetPath).Distinct()
                             .Where(p => !AssetDatabase.IsValidFolder(p)).ToList())
                {
                    bool keep = used.Contains(path) || path.EndsWith(".txt") || path.EndsWith(".pdf");
                    if (!keep && AssetDatabase.DeleteAsset(path))
                        log.AppendLine($"deleted {path}");
                }

                foreach (var tga in Directory.GetFiles(pack, "*.tga", SearchOption.AllDirectories))
                    OrkPackSlimmer.ConvertToPng(tga.Replace('\\', '/'), log);
                DeleteEmptyFolders(pack);
            }

            AssetDatabase.Refresh();
            log.AppendLine($"Packs: {before / 1048576f:F0} MB -> {Packs.Sum(FolderBytes) / 1048576f:F0} MB");
            Debug.Log("[AR Survival] Packs pruned.\n" + log);
        }

        static void DeleteEmptyFolders(string folder)
        {
            foreach (var sub in Directory.GetDirectories(folder))
                DeleteEmptyFolders(sub);
            if (Directory.GetFileSystemEntries(folder).Length == 0)
                AssetDatabase.DeleteAsset(folder.Replace('\\', '/'));
        }

        static long FolderBytes(string folder) =>
            Directory.Exists(folder) ? Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length) : 0;
    }
}
