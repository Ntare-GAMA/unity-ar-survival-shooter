using ARSurvival.Combat;
using ARSurvival.Enemies;
using ARSurvival.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Builds the player and enemy prefabs with placeholder primitive models (~20 cm tall, sized
    /// for a 2 m AR arena). Each is created only if missing, so hand edits and swapped-in art
    /// models are never overwritten. Swap the "Model" child for real art later.
    /// </summary>
    public static class GameplayPrefabs
    {
        const string PlayerPath = "Assets/Prefabs/Player.prefab";
        const string MeleePath = "Assets/Prefabs/MeleeEnemy.prefab";
        const string ShooterPath = "Assets/Prefabs/ShooterEnemy.prefab";

        public static GameObject CreatePlayerIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            if (existing != null)
                return existing;

            var body = Mat("Player_Body", new Color(0.2f, 0.55f, 1f));
            var skin = Mat("Player_Head", new Color(0.93f, 0.85f, 0.75f));
            var dark = Mat("Dark", new Color(0.12f, 0.12f, 0.14f));

            var root = new GameObject("Player");
            AddCapsuleCollider(root, 0.1f, 0.05f, 0.2f);
            AddKinematicBody(root);

            var model = Child(root.transform, "Model");
            Part(model, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.08f, 0f), new Vector3(0.08f, 0.065f, 0.08f), body);
            Part(model, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.17f, 0f), Vector3.one * 0.065f, skin);
            Part(model, PrimitiveType.Cube, "Visor", new Vector3(0f, 0.175f, 0.027f), new Vector3(0.05f, 0.016f, 0.02f), dark);
            Part(model, PrimitiveType.Cube, "Gun", new Vector3(0.045f, 0.09f, 0.04f), new Vector3(0.018f, 0.022f, 0.08f), dark);
            var muzzle = Child(model, "Muzzle");
            muzzle.localPosition = new Vector3(0.045f, 0.09f, 0.09f);

            var flash = root.AddComponent<HitFlash>();
            var health = root.AddComponent<PlayerHealth>();
            var shooter = root.AddComponent<PlayerShooter>();
            var controller = root.AddComponent<PlayerController>();
            Set(health, "hitFlash", flash);
            Set(shooter, "muzzle", muzzle);
            Set(controller, "model", model);

            return Save(root, PlayerPath);
        }

        /// <summary>Melee: squat red brute with horns and fists. 2 bullets to kill on Easy.</summary>
        public static MeleeEnemy CreateMeleeIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MeleeEnemy>(MeleePath);
            if (existing != null)
                return existing;

            var red = Mat("Melee_Body", new Color(0.85f, 0.18f, 0.12f));
            var horn = Mat("Melee_Horn", new Color(0.95f, 0.9f, 0.8f));
            var eye = Mat("Enemy_Eye", new Color(1f, 0.85f, 0.1f));

            var root = new GameObject("MeleeEnemy");
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.055f, 0f);
            box.size = new Vector3(0.11f, 0.11f, 0.11f);
            AddKinematicBody(root);

            var model = Child(root.transform, "Model");
            Part(model, PrimitiveType.Cube, "Body", new Vector3(0f, 0.05f, 0f), new Vector3(0.1f, 0.09f, 0.09f), red);
            Part(model, PrimitiveType.Cube, "HornL", new Vector3(-0.03f, 0.11f, 0.01f), new Vector3(0.014f, 0.04f, 0.014f), horn, new Vector3(0f, 0f, 20f));
            Part(model, PrimitiveType.Cube, "HornR", new Vector3(0.03f, 0.11f, 0.01f), new Vector3(0.014f, 0.04f, 0.014f), horn, new Vector3(0f, 0f, -20f));
            Part(model, PrimitiveType.Sphere, "EyeL", new Vector3(-0.022f, 0.07f, 0.046f), Vector3.one * 0.018f, eye);
            Part(model, PrimitiveType.Sphere, "EyeR", new Vector3(0.022f, 0.07f, 0.046f), Vector3.one * 0.018f, eye);
            Part(model, PrimitiveType.Sphere, "FistL", new Vector3(-0.065f, 0.04f, 0.03f), Vector3.one * 0.035f, red);
            Part(model, PrimitiveType.Sphere, "FistR", new Vector3(0.065f, 0.04f, 0.03f), Vector3.one * 0.035f, red);

            var flash = root.AddComponent<HitFlash>();
            var enemy = root.AddComponent<MeleeEnemy>();
            SetEnemyStats(enemy, model, flash, health: 2, damage: 2, speed: 0.32f, range: 0.13f, cooldown: 1f, score: 10);

            return Save(root, MeleePath).GetComponent<MeleeEnemy>();
        }

        /// <summary>Shooter: tall purple cyclops with a blaster. 4 bullets to kill on Easy.</summary>
        public static ShooterEnemy CreateShooterIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ShooterEnemy>(ShooterPath);
            if (existing != null)
                return existing;

            var purple = Mat("Shooter_Body", new Color(0.55f, 0.22f, 0.9f));
            var eye = Mat("Shooter_Eye", new Color(0.3f, 1f, 0.4f));
            var dark = Mat("Dark", new Color(0.12f, 0.12f, 0.14f));

            var root = new GameObject("ShooterEnemy");
            AddCapsuleCollider(root, 0.11f, 0.045f, 0.22f);
            AddKinematicBody(root);

            var model = Child(root.transform, "Model");
            Part(model, PrimitiveType.Cylinder, "Body", new Vector3(0f, 0.075f, 0f), new Vector3(0.07f, 0.075f, 0.07f), purple);
            Part(model, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.18f, 0f), Vector3.one * 0.07f, purple);
            Part(model, PrimitiveType.Sphere, "Eye", new Vector3(0f, 0.185f, 0.03f), Vector3.one * 0.028f, eye);
            Part(model, PrimitiveType.Cylinder, "Barrel", new Vector3(0f, 0.11f, 0.055f), new Vector3(0.022f, 0.035f, 0.022f), dark, new Vector3(90f, 0f, 0f));
            var muzzle = Child(model, "Muzzle");
            muzzle.localPosition = new Vector3(0f, 0.11f, 0.1f);

            var flash = root.AddComponent<HitFlash>();
            var enemy = root.AddComponent<ShooterEnemy>();
            SetEnemyStats(enemy, model, flash, health: 4, damage: 1, speed: 0.22f, range: 0.75f, cooldown: 1.6f, score: 25);
            Set(enemy, "shootingDistance", 0.6f);
            Set(enemy, "muzzle", muzzle);

            return Save(root, ShooterPath).GetComponent<ShooterEnemy>();
        }

        /// <summary>Puts a Player instance at the GameWorld's PlayerSpawn if it doesn't have one.</summary>
        public static void EnsurePlayerInWorld(string worldPrefabPath, GameObject playerPrefab)
        {
            var contents = PrefabUtility.LoadPrefabContents(worldPrefabPath);
            try
            {
                // Drop placeholders left behind if a nested prefab (e.g. an old Player) was deleted.
                foreach (Transform child in contents.transform)
                    if (PrefabUtility.IsPrefabAssetMissing(child.gameObject))
                        Object.DestroyImmediate(child.gameObject);

                if (contents.GetComponentInChildren<PlayerHealth>(true) != null)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, worldPrefabPath);
                    return;
                }

                var spawn = contents.transform.Find("PlayerSpawn");
                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, contents.transform);
                if (spawn != null)
                    player.transform.SetLocalPositionAndRotation(spawn.localPosition, spawn.localRotation);
                PrefabUtility.SaveAsPrefabAsset(contents, worldPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // --- helpers ---

        static void SetEnemyStats(Enemy enemy, Transform model, HitFlash flash, int health, int damage,
            float speed, float range, float cooldown, int score)
        {
            var so = new SerializedObject(enemy);
            so.FindProperty("baseHealth").intValue = health;
            so.FindProperty("baseAttackDamage").intValue = damage;
            so.FindProperty("moveSpeed").floatValue = speed;
            so.FindProperty("attackRange").floatValue = range;
            so.FindProperty("attackCooldown").floatValue = cooldown;
            so.FindProperty("scoreValue").intValue = score;
            so.FindProperty("model").objectReferenceValue = model;
            so.FindProperty("hitFlash").objectReferenceValue = flash;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material Mat(string materialName, Color color)
        {
            var path = $"Assets/Art/Materials/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            if (!AssetDatabase.IsValidFolder("Assets/Art/Materials"))
                AssetDatabase.CreateFolder("Assets/Art", "Materials");
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Transform Child(Transform parent, string childName)
        {
            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            return child;
        }

        static void Part(Transform parent, PrimitiveType type, string partName, Vector3 position, Vector3 scale,
            Material material, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = partName;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(euler));
            go.transform.localScale = scale;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void AddCapsuleCollider(GameObject go, float centerY, float radius, float height)
        {
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, centerY, 0f);
            capsule.radius = radius;
            capsule.height = height;
        }

        /// <summary>Colliders that move every frame should have a kinematic Rigidbody.</summary>
        static void AddKinematicBody(GameObject go)
        {
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        static GameObject Save(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
