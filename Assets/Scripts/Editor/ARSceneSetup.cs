using System.Linq;
using ARSurvival.AR;
using ARSurvival.Audio;
using ARSurvival.Combat;
using ARSurvival.Core;
using ARSurvival.Data;
using ARSurvival.Enemies;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// One-click setup of the AR scene: AR Session, XR Origin, horizontal plane detection with the
    /// custom name plane tracker, raycasting, and the URP AR background renderer feature.
    /// Safe to run repeatedly - it only creates what is missing.
    /// </summary>
    public static class ARSceneSetup
    {
        const string TexturePath = "Assets/Art/PlaneTracker/PlaneTracker_Name.png";
        const string ShaderName = "ARSurvival/PlaneTracker";
        const string MaterialPath = "Assets/Art/PlaneTracker/PlaneTracker.mat";
        const string PrefabPath = "Assets/Prefabs/CustomPlaneTracker.prefab";
        const string GameWorldPrefabPath = "Assets/Prefabs/GameWorld.prefab";
        const string ArenaMaterialPath = "Assets/Art/Arena.mat";
        const string DifficultyFolder = "Assets/Data/Difficulty";

        static readonly Color Accent = new Color(0f, 0.9f, 0.78f, 1f);

        [MenuItem("Tools/AR Survival/Setup AR Scene")]
        public static void SetupScene()
        {
            ProjectPaths.OpenGameScene();

            ConfigureTextureImport();
            var material = CreateOrUpdateMaterial();
            var planePrefab = CreatePlanePrefabIfMissing(material);
            AddARBackgroundFeatureToRenderers();

            RemoveNonARCameras();
            EnsureARSession();
            var origin = EnsureXROrigin();

            var planeManager = GetOrAdd<ARPlaneManager>(origin.gameObject);
            planeManager.planePrefab = planePrefab;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            GetOrAdd<ARRaycastManager>(origin.gameObject);
            EditorUtility.SetDirty(planeManager);

            var placement = GetOrAdd<ARPlacementController>(origin.gameObject);
            var placementSo = new SerializedObject(placement);
            var worldPrefabProp = placementSo.FindProperty("gameWorldPrefab");
            if (worldPrefabProp.objectReferenceValue == null)
                worldPrefabProp.objectReferenceValue = CreateGameWorldPrefabIfMissing();
            placementSo.ApplyModifiedProperties();

            EnsureEventSystem();
            EnsureGameplayManagers(placement);
            UIBuilder.BuildIfMissing();
            ConfigurePlayerSettings();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = origin.gameObject;
            Debug.Log("[AR Survival] AR scene setup complete.");
        }

        /// <summary>TextMesh Pro fonts/shaders. Run once before the UI is built (in its own editor session).</summary>
        [MenuItem("Tools/AR Survival/Import TMP Essentials")]
        public static void ImportTMPEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
                return;
            TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        /// <summary>Portrait-only phone app with a proper name and package id (template defaults replaced).</summary>
        static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "AR Survival Shooter";
            PlayerSettings.companyName = "NTARE GAMA Allan";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,
                "com.ntaregamaallan.arsurvivalshooter");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
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

        static GameObject CreatePlanePrefabIfMissing(Material material)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
                return existing;

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
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
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

        /// <summary>
        /// Placeholder world root placed by tap-to-place: a 2 m arena floor, a player spawn at the
        /// centre and a ring of enemy spawn points on its edge. Real art replaces the Arena child later.
        /// </summary>
        static GameObject CreateGameWorldPrefabIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GameWorldPrefabPath);
            if (existing != null)
                return existing;

            var arenaMaterial = AssetDatabase.LoadAssetAtPath<Material>(ArenaMaterialPath);
            if (arenaMaterial == null)
            {
                arenaMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                arenaMaterial.SetColor("_BaseColor", new Color(0.08f, 0.16f, 0.2f));
                arenaMaterial.SetFloat("_Smoothness", 0.2f);
                AssetDatabase.CreateAsset(arenaMaterial, ArenaMaterialPath);
            }

            var root = new GameObject("GameWorld");

            var arena = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arena.name = "Arena";
            Object.DestroyImmediate(arena.GetComponent<Collider>());
            arena.transform.SetParent(root.transform, false);
            arena.transform.localPosition = new Vector3(0f, 0.0025f, 0f);
            arena.transform.localScale = new Vector3(2f, 0.0025f, 2f);
            var arenaRenderer = arena.GetComponent<MeshRenderer>();
            arenaRenderer.sharedMaterial = arenaMaterial;
            arenaRenderer.shadowCastingMode = ShadowCastingMode.Off;

            new GameObject("PlayerSpawn").transform.SetParent(root.transform, false);

            var spawnRoot = new GameObject("EnemySpawnPoints").transform;
            spawnRoot.SetParent(root.transform, false);
            const int spawnCount = 8;
            const float spawnRadius = 0.9f;
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = i * Mathf.PI * 2f / spawnCount;
                var point = new GameObject($"Spawn_{i}").transform;
                point.SetParent(spawnRoot, false);
                point.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GameWorldPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // --- Core managers, difficulty assets and projectile pools ---

        static void EnsureGameplayManagers(ARPlacementController placement)
        {
            var easy = CreateDifficultyIfMissing("Easy", 90f, 3.5f, 5, 0.3f, 1f, 0.85f, 1f);
            var hard = CreateDifficultyIfMissing("Hard", 120f, 1.8f, 9, 0.45f, 1.5f, 1.25f, 1.5f);

            var gameManager = FindOrCreate<GameManager>("GameManager");
            var gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("placement").objectReferenceValue = placement;
            var difficulties = gmSo.FindProperty("difficulties");
            if (difficulties.arraySize == 0)
            {
                difficulties.arraySize = 2;
                difficulties.GetArrayElementAtIndex(0).objectReferenceValue = easy;
                difficulties.GetArrayElementAtIndex(1).objectReferenceValue = hard;
            }
            gmSo.ApplyModifiedProperties();

            var audioManager = FindOrCreate<AudioManager>("AudioManager");
            var amSo = new SerializedObject(audioManager);
            var sounds = amSo.FindProperty("sounds");
            if (sounds.arraySize == 0)
            {
                var ids = (SoundId[])System.Enum.GetValues(typeof(SoundId));
                sounds.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++)
                {
                    var entry = sounds.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("id").enumValueIndex = (int)ids[i];
                    entry.FindPropertyRelative("volume").floatValue = 1f;
                    entry.FindPropertyRelative("pitchRange").vector2Value = new Vector2(0.95f, 1.05f);
                }
            }
            amSo.ApplyModifiedProperties();

            var pools = GameObject.Find("Pools") ?? new GameObject("Pools");
            EnsurePool(pools.transform, "PlayerBulletPool", Team.Player,
                CreateProjectilePrefabIfMissing("PlayerBullet", new Color(1f, 0.85f, 0.2f), 3f), 40);
            EnsurePool(pools.transform, "EnemyBulletPool", Team.Enemy,
                CreateProjectilePrefabIfMissing("EnemyBullet", new Color(1f, 0.2f, 0.25f), 1.6f), 40);

            var worldPrefab = new SerializedObject(placement).FindProperty("gameWorldPrefab").objectReferenceValue;
            GameplayPrefabs.EnsurePlayerInWorld(AssetDatabase.GetAssetPath(worldPrefab), GameplayPrefabs.CreatePlayerIfMissing());

            var spawner = FindOrCreate<EnemySpawner>("Enemies");
            var factory = GetOrAdd<EnemyFactory>(spawner.gameObject);
            var factorySo = new SerializedObject(factory);
            factorySo.FindProperty("meleePrefab").objectReferenceValue = GameplayPrefabs.CreateMeleeIfMissing();
            factorySo.FindProperty("shooterPrefab").objectReferenceValue = GameplayPrefabs.CreateShooterIfMissing();
            factorySo.ApplyModifiedProperties();
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("factory").objectReferenceValue = factory;
            spawnerSo.ApplyModifiedProperties();
        }

        static DifficultySettings CreateDifficultyIfMissing(string displayName, float duration, float spawnInterval,
            int maxAlive, float shooterChance, float health, float speed, float damage)
        {
            var path = $"{DifficultyFolder}/Difficulty_{displayName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<DifficultySettings>(path);
            if (asset != null)
                return asset;

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(DifficultyFolder))
                AssetDatabase.CreateFolder("Assets/Data", "Difficulty");
            asset = ScriptableObject.CreateInstance<DifficultySettings>();
            AssetDatabase.CreateAsset(asset, path);
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("roundDuration").floatValue = duration;
            so.FindProperty("spawnInterval").floatValue = spawnInterval;
            so.FindProperty("maxAliveEnemies").intValue = maxAlive;
            so.FindProperty("shooterSpawnChance").floatValue = shooterChance;
            so.FindProperty("enemyHealthMultiplier").floatValue = health;
            so.FindProperty("enemySpeedMultiplier").floatValue = speed;
            so.FindProperty("enemyDamageMultiplier").floatValue = damage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        static Projectile CreateProjectilePrefabIfMissing(string prefabName, Color color, float speed)
        {
            var path = $"Assets/Prefabs/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Projectile>(path);
            if (existing != null)
                return existing;

            var materialPath = $"Assets/Art/{prefabName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = prefabName;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.03f;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            trail.time = 0.08f;
            trail.widthMultiplier = 0.015f;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;

            var projectile = go.AddComponent<Projectile>();
            var so = new SerializedObject(projectile);
            so.FindProperty("speed").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path).GetComponent<Projectile>();
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void EnsurePool(Transform parent, string poolName, Team team, Projectile prefab, int size)
        {
            var existing = parent.Find(poolName);
            var pool = existing != null ? existing.GetComponent<ProjectilePool>() : null;
            if (pool == null)
            {
                var go = new GameObject(poolName, typeof(ProjectilePool));
                go.transform.SetParent(parent, false);
                pool = go.GetComponent<ProjectilePool>();
            }

            var so = new SerializedObject(pool);
            so.FindProperty("prefab").objectReferenceValue = prefab;
            so.FindProperty("size").intValue = size;
            so.FindProperty("team").enumValueIndex = (int)team;
            so.ApplyModifiedProperties();
        }

        static T FindOrCreate<T>(string objectName) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>();
            if (existing != null)
                return existing;
            var go = new GameObject(objectName, typeof(T));
            Undo.RegisterCreatedObjectUndo(go, $"Create {objectName}");
            return go.GetComponent<T>();
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }
    }
}
