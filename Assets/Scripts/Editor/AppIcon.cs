using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Sets the app icon from the generated layers in Assets/Art/Icon (see Tools/GenerateAppIcon.ps1):
    /// the default icon, Android adaptive icons (background + foreground layers) and the round and
    /// legacy icons for older devices.
    /// </summary>
    public static class AppIcon
    {
        const string Folder = "Assets/Art/Icon";

        [MenuItem("Tools/AR Survival/Apply App Icon")]
        public static void Apply()
        {
            var full = Configure("Icon_Full");
            var background = Configure("Icon_Background");
            var foreground = Configure("Icon_Foreground");

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { full }, IconKind.Any);

            var android = NamedBuildTarget.Android;
            var adaptive = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Adaptive);
            foreach (var icon in adaptive)
                icon.SetTextures(background, foreground);
            PlayerSettings.SetPlatformIcons(android, AndroidPlatformIconKind.Adaptive, adaptive);

            foreach (var kind in new[] { AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Legacy })
            {
                var icons = PlayerSettings.GetPlatformIcons(android, kind);
                foreach (var icon in icons)
                    icon.SetTexture(full);
                PlayerSettings.SetPlatformIcons(android, kind, icons);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[AR Survival] App icon applied.");
        }

        static Texture2D Configure(string name)
        {
            var path = $"{Folder}/{name}.png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; // crisp icons
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
