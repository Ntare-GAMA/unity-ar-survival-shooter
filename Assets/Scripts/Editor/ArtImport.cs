using System.Linq;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Makes the imported Asset Store art work in this URP project: upgrades the OrkDestroyer2
    /// materials (Built-in Standard / custom double-sided shader, which render pink in URP) to URP Lit.
    /// </summary>
    public static class ArtImport
    {
        public const string OrkMaterialsFolder = "Assets/OrkDestroyer2/Materials";

        public const string RaptorMaterialsFolder = "Assets/PBRVelociraptor/Materials/Mobile";

        [MenuItem("Tools/AR Survival/Convert Ork Materials to URP")]
        public static void ConvertOrkMaterials() => ConvertToUrp(OrkMaterialsFolder);

        /// <summary>
        /// Upgrades every Built-in pipeline material in a folder (Standard, Standard (Specular setup)
        /// or a custom double-sided Standard) to URP Lit, keeping albedo, normal, metallic/occlusion
        /// maps, smoothness, cutout and double-sidedness.
        /// </summary>
        public static void ConvertToUrp(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.shader == lit)
                    continue;

                // Read Built-in Standard properties before switching shader.
                var albedo = material.GetTexture("_MainTex");
                var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                var normal = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
                var metallicMap = material.HasProperty("_MetallicGlossMap") ? material.GetTexture("_MetallicGlossMap") : null;
                var occlusion = material.HasProperty("_OcclusionMap") ? material.GetTexture("_OcclusionMap") : null;
                float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.5f;
                float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
                bool cutout = material.HasProperty("_Mode") && Mathf.Approximately(material.GetFloat("_Mode"), 1f);
                float cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;
                bool doubleSided = material.shader.name.Contains("Double Sided") || material.shader.name.Contains("Two");

                material.shader = lit;
                material.SetTexture("_BaseMap", albedo);
                material.SetColor("_BaseColor", color);
                material.SetTexture("_BumpMap", normal);
                material.SetTexture("_MetallicGlossMap", metallicMap);
                material.SetTexture("_OcclusionMap", occlusion);
                material.SetFloat("_Smoothness", smoothness);
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_AlphaClip", cutout ? 1f : 0f);
                material.SetFloat("_Cutoff", cutoff);
                material.SetFloat("_Cull", doubleSided ? 0f : 2f); // 0 = render both faces
                material.renderQueue = -1;

                BaseShaderGUI.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Loads an animation clip. "path.fbx" returns the file's first clip; "path.fbx#ClipName" picks a
        /// named clip from a file that contains several (e.g. the raptor pack).
        /// </summary>
        public static AnimationClip Clip(string clipSpec)
        {
            var (path, name) = SplitClipSpec(clipSpec);
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview"));
            return name == null ? clips.First() : clips.First(c => c.name == name);
        }

        public static (string path, string clipName) SplitClipSpec(string clipSpec)
        {
            int hash = clipSpec.IndexOf('#');
            return hash < 0 ? (clipSpec, null) : (clipSpec.Substring(0, hash), clipSpec.Substring(hash + 1));
        }
    }
}
