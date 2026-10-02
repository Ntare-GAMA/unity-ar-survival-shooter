using System.Linq;
using ARSurvival.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Builds the underwater environment inside the placed arena (GameWorld prefab):
    /// a lagoon of animated water around the sandy arena (PolyOne "Free Pack - Water Shader URP")
    /// and a school of low-poly fish plus a shark (Alstra Infinite "Fish - PolyPack") swimming
    /// around it. Everything lives under GameWorld/Environment, so it is anchored with the arena
    /// and hidden with it in the menus. Re-running replaces the previous environment.
    /// </summary>
    public static class EnvironmentBuilder
    {
        const string WorldPrefab = "Assets/Prefabs/GameWorld.prefab";
        const string FishFolder = "Assets/Alstra Infinite/Fish - PolyPack";
        const string OutFolder = "Assets/Art/Environment";

        const string PackWaterMaterial = "Assets/PolyOne/Water URP/Materials/M_Shader Water.mat";
        const string LagoonMaterialPath = OutFolder + "/Lagoon.mat";

        // Arena floor is a 2 m disc (radius 1); the lagoon surrounds it.
        const float LagoonInner = 0.98f, LagoonOuter = 1.7f;

        struct FishSpec
        {
            public string Prefab;
            public float Length, Radius, Height, Speed, Start;
            public bool Clockwise;
        }

        static readonly FishSpec[] School =
        {
            new() { Prefab = "FishV1", Length = 0.10f, Radius = 1.20f, Height = 0.18f, Speed = 0.16f, Start = 0 },
            new() { Prefab = "FishV1", Length = 0.09f, Radius = 1.28f, Height = 0.22f, Speed = 0.16f, Start = 12 },
            new() { Prefab = "FishV2", Length = 0.11f, Radius = 1.35f, Height = 0.35f, Speed = 0.12f, Start = 90, Clockwise = true },
            new() { Prefab = "FishV2", Length = 0.10f, Radius = 1.42f, Height = 0.40f, Speed = 0.12f, Start = 104, Clockwise = true },
            new() { Prefab = "FishV3", Length = 0.10f, Radius = 1.15f, Height = 0.50f, Speed = 0.18f, Start = 200 },
            new() { Prefab = "FishV4", Length = 0.12f, Radius = 1.50f, Height = 0.28f, Speed = 0.10f, Start = 300, Clockwise = true },
            new() { Prefab = "FishV4", Length = 0.11f, Radius = 1.58f, Height = 0.32f, Speed = 0.10f, Start = 312, Clockwise = true },
            new() { Prefab = "SharkV1", Length = 0.30f, Radius = 1.75f, Height = 0.45f, Speed = 0.09f, Start = 150 },
        };

        [MenuItem("Tools/AR Survival/Build Underwater Environment")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(OutFolder))
                AssetDatabase.CreateFolder("Assets/Art", "Environment");
            ArtImport.ConvertToUrp($"{FishFolder}/Materials");

            var contents = PrefabUtility.LoadPrefabContents(WorldPrefab);
            try
            {
                var old = contents.transform.Find("Environment");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
                var environment = new GameObject("Environment").transform;
                environment.SetParent(contents.transform, false);

                BuildLagoon(environment);
                var fishRoot = new GameObject("Fish").transform;
                fishRoot.SetParent(environment, false);
                for (int i = 0; i < School.Length; i++)
                    AddFish(fishRoot, School[i], i);

                PrefabUtility.SaveAsPrefabAsset(contents, WorldPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[AR Survival] Underwater environment built.");
        }

        /// <summary>
        /// Lagoon material: a copy of the pack's water material (shader "Shader Water"), tuned for a
        /// 2 m arena (gentler waves) and tinted toward the theme's deep navy.
        /// </summary>
        static Material LagoonMaterial()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(PackWaterMaterial);
            if (source == null)
                return null;
            var material = AssetDatabase.LoadAssetAtPath<Material>(LagoonMaterialPath);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, LagoonMaterialPath);
            }
            material.shader = source.shader;
            material.CopyPropertiesFromMaterial(source);
            material.SetFloat("_Amplitude_Wave", 0.004f);
            material.SetFloat("_Speed_Wave", 0.6f);
            // The pack's ripple noise is tuned for lakes hundreds of metres wide; at arena scale it
            // turns into static, so it is scaled down to a few ripples per metre.
            material.SetFloat("_Scale_Noise", 60f);
            material.SetFloat("_Speed_Noise", 20f);
            material.SetColor("_Shallow_Water_Color", new Color(0.16f, 0.88f, 0.75f, 0f));
            material.SetColor("_Deep_Water_Color", new Color(0.06f, 0.3f, 0.55f, 0f));
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>The water shader samples scene depth and colour (depth tint, refraction).</summary>
        static void EnableSceneTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets/Settings" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(asset);
                so.FindProperty("m_RequireDepthTexture").boolValue = true;
                so.FindProperty("m_RequireOpaqueTexture").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void BuildLagoon(Transform parent)
        {
            EnableSceneTextures();
            var material = LagoonMaterial();
            if (material == null)
            {
                Debug.LogWarning($"Water material not found at '{PackWaterMaterial}'; lagoon skipped.");
                return;
            }

            var lagoon = new GameObject("Lagoon", typeof(MeshFilter), typeof(MeshRenderer));
            lagoon.transform.SetParent(parent, false);
            lagoon.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            lagoon.GetComponent<MeshFilter>().sharedMesh = RingMesh();
            var renderer = lagoon.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>Flat annulus mesh (lagoon around the arena), UVs in metres, saved as an asset.</summary>
        static Mesh RingMesh()
        {
            const int segments = 128;
            var path = $"{OutFolder}/LagoonRing.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "LagoonRing" };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();

            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i * 2] = dir * LagoonInner;
                vertices[i * 2 + 1] = dir * LagoonOuter;
                uvs[i * 2] = new Vector2(vertices[i * 2].x, vertices[i * 2].z);
                uvs[i * 2 + 1] = new Vector2(vertices[i * 2 + 1].x, vertices[i * 2 + 1].z);
            }
            for (int i = 0; i < segments; i++)
            {
                int v = i * 2, t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static void AddFish(Transform parent, FishSpec spec, int index)
        {
            var swimmer = new GameObject($"{spec.Prefab}_{index}").transform;
            swimmer.SetParent(parent, false);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FishFolder}/Prefabs/{spec.Prefab}.prefab");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, swimmer);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // Cosmetic only: no colliders, so bullets and raycasts pass straight through.
            foreach (var c in model.GetComponentsInChildren<Collider>(true).ToList())
                Object.DestroyImmediate(c);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = ShadowCastingMode.Off;

            // Scale by the longest side so every fish ends up the requested length; centre it.
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(FishModelRotation);
            var bounds = ArtPreview.Bounds(model);
            float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            model.transform.localScale *= spec.Length / longest;
            bounds = ArtPreview.Bounds(model);
            model.transform.localPosition -= swimmer.InverseTransformPoint(bounds.center);

            var component = swimmer.gameObject.AddComponent<FishSwimmer>();
            var so = new SerializedObject(component);
            so.FindProperty("radius").floatValue = spec.Radius;
            so.FindProperty("height").floatValue = spec.Height;
            so.FindProperty("speed").floatValue = spec.Speed;
            so.FindProperty("clockwise").boolValue = spec.Clockwise;
            so.FindProperty("startAngle").floatValue = spec.Start;
            so.FindProperty("swayDegrees").floatValue = spec.Prefab.StartsWith("Shark") ? 6f : 12f;
            so.FindProperty("swayFrequency").floatValue = spec.Prefab.StartsWith("Shark") ? 1.5f : 3f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The fish models point their heads along -X; this turns them to swim along +Z.</summary>
        static readonly Vector3 FishModelRotation = new(0f, 90f, 0f);
    }
}
