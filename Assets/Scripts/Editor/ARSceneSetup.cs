using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// One-click setup of the AR scene: AR Session, XR Origin, horizontal plane detection with the
    /// custom name plane tracker, raycasting, and the URP AR background renderer feature.
    /// Safe to run repeatedly — it only creates what is missing.
    /// </summary>
    public static class ARSceneSetup
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string TexturePath = "Assets/Art/PlaneTracker/PlaneTracker_Name.png";
        const string ShaderName = "ARSurvival/PlaneTracker";
        const string MaterialPath = "Assets/Art/PlaneTracker/PlaneTracker.mat";
        const string PrefabPath = "Assets/Prefabs/CustomPlaneTracker.prefab";

        static readonly Color Accent = new Color(0f, 0.9f, 0.78f, 1f);

        [MenuItem("Tools/AR Survival/Setup AR Scene")]
        public static void SetupScene()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);

            ConfigureTextureImport();
            var material = CreateOrUpdateMaterial();
            var planePrefab = CreateOrUpdatePlanePrefab(material);
            AddARBackgroundFeatureToRenderers();

            RemoveNonARCameras();
            EnsureARSession();
            var origin = EnsureXROrigin();

            var planeManager = GetOrAdd<ARPlaneManager>(origin.gameObject);
            planeManager.planePrefab = planePrefab;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            GetOrAdd<ARRaycastManager>(origin.gameObject);
            EditorUtility.SetDirty(planeManager);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = origin.gameObject;
            Debug.Log("[AR Survival] AR scene setup complete.");
        }

        static void ConfigureTextureImport()
        {
            AssetDatabase.ImportAsset(TexturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
        }

        static Material CreateOrUpdateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find(ShaderName));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = Shader.Find(ShaderName);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
            // Plane UVs are in metres, so a scale of 2 gives one name tile every 0.5 m.
            material.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject CreateOrUpdatePlanePrefab(Material material)
        {
            var go = new GameObject("CustomPlaneTracker",
                typeof(ARPlane), typeof(ARPlaneMeshVisualizer), typeof(MeshCollider),
                typeof(MeshFilter), typeof(MeshRenderer), typeof(LineRenderer));

            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var line = go.GetComponent<LineRenderer>();
            line.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            line.loop = true;
            line.widthMultiplier = 0.01f;
            line.startColor = Accent;
            line.endColor = Accent;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>URP needs this renderer feature or the camera feed renders black.</summary>
        static void AddARBackgroundFeatureToRenderers()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets" }))
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data.rendererFeatures.Any(f => f is ARBackgroundRendererFeature))
                    continue;

                var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                feature.name = nameof(ARBackgroundRendererFeature);
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

                var so = new SerializedObject(data);
                var features = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedProperties();
                data.SetDirty();
                EditorUtility.SetDirty(data);
            }
        }

        static void RemoveNonARCameras()
        {
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam.GetComponent<ARCameraManager>() == null)
                    Undo.DestroyObjectImmediate(cam.gameObject);
            }
        }

        static void EnsureARSession()
        {
            if (Object.FindAnyObjectByType<ARSession>() != null)
                return;
            Selection.activeObject = null;
            EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
        }

        static XROrigin EnsureXROrigin()
        {
            var origin = Object.FindAnyObjectByType<XROrigin>();
            if (origin != null)
                return origin;
            Selection.activeObject = null;
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");
            return Object.FindAnyObjectByType<XROrigin>();
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }
    }
}
