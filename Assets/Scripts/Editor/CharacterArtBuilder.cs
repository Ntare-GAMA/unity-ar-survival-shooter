using System.Collections.Generic;
using System.Linq;
using System.Text;
using ARSurvival.Combat;
using ARSurvival.Enemies;
using ARSurvival.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Builds the character prefabs from the imported art:
    /// Player = Elizabeth Warren caricature with the pistol (Toony Tiny People pistol animations,
    /// retargeted through the Humanoid rig),
    /// Melee = ToonyTinyPeople police officer with the assault rifle,
    /// Shooter = OrkDestroyer2 with the pistol.
    /// Handles URP materials, animator controllers, scale, hiding built-in props, gun placement
    /// (muzzle and grip found from the gun mesh) and muzzle flash. Overwrites the prefabs in place
    /// so scene references keep working.
    /// </summary>
    public static class CharacterArtBuilder
    {
        const string AnimFolder = "Assets/Art/Animation";
        const string RaptorArt = "Assets/PBRVelociraptor/Prefabs/Mobile/5K/Raptor_Animated_FBX_5K_Green.prefab";
        const string RaptorClips = "Assets/PBRVelociraptor/Models/5K/Raptor_Animated_FBX_5K.fbx#RaptorArmature|";
        const string OrkAnim = "Assets/OrkDestroyer2/Animation/Ork_destroyer2@";
        const string MaleAnim = "Assets/ToonyTinyPeople/TT_demo/animation/male/";

        // Gun meshes are modelled lying on their side: barrel along -X, top of the gun along +Z.
        static readonly Quaternion GunToForward = Quaternion.Inverse(Quaternion.LookRotation(Vector3.left, Vector3.forward));

        class Spec
        {
            public string Name, PrefabPath, ArtPath, GunPath;
            public string[] HideObjects;
            public float Height, ColliderRadius, GunLength;
            public string Idle, Move, Attack, Hit, Death;
            public string[] LoopClips = System.Array.Empty<string>();
            public float DeathDuration;
            public bool BoxBody;   // long horizontal body (e.g. raptor) -> box collider
            public float ArtYaw;   // corrects models that don't face +Z
            public System.Func<GameObject, (Transform hand, Transform finger)> Hand;
        }

        [MenuItem("Tools/AR Survival/Build Character Prefabs From Art")]
        public static void BuildAll()
        {
            ArtImport.ConvertOrkMaterials();
            var log = new StringBuilder();

            ArtImport.ConvertToUrp(ArtImport.RaptorMaterialsFolder);
            var melee = new Spec
            {
                Name = "MeleeEnemy",
                PrefabPath = "Assets/Prefabs/MeleeEnemy.prefab",
                ArtPath = RaptorArt,
                Height = 0.16f, BoxBody = true,
                ArtYaw = 180f, // the raptor model faces -Z
                Idle = RaptorClips + "Raptor_Idle1_Anim",
                Move = RaptorClips + "Raptor_Run1_Anim",       // in-place version (not the _RM root-motion one)
                Attack = RaptorClips + "Raptor_Bite1_Anim",
                Hit = RaptorClips + "Raptor_Hit1_Anim",
                Death = RaptorClips + "Raptor_Death1_Anim",
                LoopClips = new[] { RaptorClips + "Raptor_Idle1_Anim", RaptorClips + "Raptor_Run1_Anim" },
                DeathDuration = 1.3f,
            };

            var shooter = new Spec
            {
                Name = "ShooterEnemy",
                PrefabPath = "Assets/Prefabs/ShooterEnemy.prefab",
                ArtPath = "Assets/OrkDestroyer2/Prefab/OrkDestroyer2.prefab",
                GunPath = "Assets/Tools (Prefabs)/Shooter Gun/Pistol_5.fbx",
                HideObjects = new[] { "Weapon", "persp1" }, // built-in blades, stray camera
                Height = 0.3f, ColliderRadius = 0.07f, GunLength = 0.06f,
                Idle = OrkAnim + "idle1.fbx",
                Move = OrkAnim + "walk1.fbx",
                Hit = OrkAnim + "gethit.fbx",
                Death = OrkAnim + "death1.fbx",
                LoopClips = new[] { OrkAnim + "idle1.fbx", OrkAnim + "walk1.fbx" },
                DeathDuration = 1.4f,
                Hand = art => (Find(art.transform, "ORK_RightHand"), Find(art.transform, "ORK_RightHandMiddle1")),
            };

            StripAnimationEvents(melee.Idle, melee.Move, melee.Attack, melee.Hit, melee.Death);
            Build(melee, log, (enemy, body) =>
            {
                // Bite reaches from the raptor's centre to its snout, plus the player's radius.
                float reach = body.z * 0.5f + 0.06f;
                SetEnemyStats(enemy, health: 2, damage: 2, speed: 0.38f, range: reach, cooldown: 1.1f, score: 10);
                Set(enemy, "impactDelay", 0.3f);
                SetBool(enemy, "proceduralLunge", false); // the bite animation does the lunge
            });
            Build(shooter, log, (enemy, body) =>
            {
                SetEnemyStats(enemy, health: 4, damage: 1, speed: 0.22f, range: 0.75f, cooldown: 1.6f, score: 25);
                Set(enemy, "shootingDistance", 0.6f);
            });
            BuildPlayer(log);

            AssetDatabase.SaveAssets();
            ArtPreview.WriteLog(log);
            Debug.Log("[AR Survival] Character prefabs built.\n" + log);
        }

        // ------------------------------------------------------------------ Player

        const string PlayerArt = "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_01.fbx";
        const string PlayerIdle = MaleAnim + "m_pistol_idle_A.FBX";
        const string PlayerRun = MaleAnim + "m_pistol_run.FBX";
        const string PlayerAim = MaleAnim + "m_pistol_shoot.FBX";
        const string PlayerDeath = MaleAnim + "m_death_A.FBX";

        /// <summary>
        /// Player prefab: the caricature model (Humanoid rig) driven by the Toony Tiny People pistol
        /// animations, a pistol in the right hand aligned in the aiming pose, muzzle flash, and the
        /// player components. Overwrites Assets/Prefabs/Player.prefab so the arena's nested
        /// instance picks it up.
        /// </summary>
        static void BuildPlayer(StringBuilder log)
        {
            foreach (var clip in new[] { PlayerIdle, PlayerRun, PlayerAim })
                SetLooping(clip);

            var spec = new Spec
            {
                Name = "Player",
                PrefabPath = "Assets/Prefabs/Player.prefab",
                GunPath = "Assets/Tools (Prefabs)/MeleeGun/AssaultRifle2_1.fbx", // a rifle, to stand a chance against raptors
                Height = 0.22f, ColliderRadius = 0.05f, GunLength = 0.11f,
            };

            var root = new GameObject(spec.Name);
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, spec.Height / 2f, 0f);
            capsule.radius = spec.ColliderRadius;
            capsule.height = spec.Height;
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var model = Child(root.transform, "Model");
            var visual = Child(model, "Visual");
            var art = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerArt), visual);
            PrefabUtility.UnpackPrefabInstance(art, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            art.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var extra in art.GetComponentsInChildren<Component>(true).Where(c => c is Camera || c is Light || c is AudioListener).ToList())
                Object.DestroyImmediate(extra.gameObject);
            // Own URP material, so the player doesn't depend on the pack's (Built-in) material files.
            var bodyMaterial = PlayerMaterial();
            foreach (var r in art.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.sharedMaterials = Enumerable.Repeat(bodyMaterial, r.sharedMaterials.Length).ToArray();
            }

            var animator = art.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = BuildPlayerController();

            var idle = ArtImport.Clip(PlayerIdle);
            idle.SampleAnimation(art, 0f);
            var bounds = ArtPreview.Bounds(art);
            float scale = spec.Height / bounds.size.y;
            visual.localScale = Vector3.one * scale;
            visual.localPosition = new Vector3(0f, -bounds.min.y * scale, 0f);
            log.AppendLine($"Player: art height {bounds.size.y:F2} -> scale {scale:F4}");

            // Align the pistol in the aiming pose so shots visibly leave the barrel when firing.
            ArtImport.Clip(PlayerAim).SampleAnimation(art, 0.2f);
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var finger = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) ?? (hand.childCount > 0 ? hand.GetChild(0) : hand);
            var (_, muzzle) = BuildGun(spec, hand, finger, root.transform, log);
            var muzzleFlash = AddMuzzleFlash(muzzle, spec.GunLength * 0.25f);
            idle.SampleAnimation(art, 0f); // back to the rest pose; the gun stays on the hand bone

            var flash = root.AddComponent<HitFlash>();
            var health = root.AddComponent<PlayerHealth>();
            var shooter = root.AddComponent<PlayerShooter>();
            var controller = root.AddComponent<PlayerController>();
            var playerAnimator = root.AddComponent<PlayerAnimator>();
            Set(health, "hitFlash", flash);
            Set(shooter, "muzzle", muzzle);
            Set(shooter, "muzzleFlash", muzzleFlash);
            Set(controller, "model", model);
            Set(controller, "animator", playerAnimator);
            Set(playerAnimator, "animator", animator);

            PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// Base layer: Idle ⇄ Run on Speed, Any State → Death on Dead.
        /// "Aim" layer (upper body only, weight driven by PlayerAnimator): the pistol shooting pose.
        /// </summary>
        static RuntimeAnimatorController BuildPlayerController()
        {
            if (!AssetDatabase.IsValidFolder(AnimFolder))
                AssetDatabase.CreateFolder("Assets/Art", "Animation");
            var path = $"{AnimFolder}/Player.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            var sm = controller.layers[0].stateMachine;
            var idle = sm.AddState("Idle");
            idle.motion = ArtImport.Clip(PlayerIdle);
            sm.defaultState = idle;
            var run = sm.AddState("Run");
            run.motion = ArtImport.Clip(PlayerRun);
            Transition(idle.AddTransition(run), 0.12f).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            Transition(run.AddTransition(idle), 0.12f).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            var death = sm.AddState("Death");
            death.motion = ArtImport.Clip(PlayerDeath);
            var toDeath = Transition(sm.AddAnyStateTransition(death), 0.1f);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            toDeath.canTransitionToSelf = false;

            controller.AddLayer("Aim");
            var layers = controller.layers;
            layers[1].avatarMask = UpperBodyMask();
            layers[1].defaultWeight = 0f;
            controller.layers = layers;
            var aim = layers[1].stateMachine.AddState("Aim");
            aim.motion = ArtImport.Clip(PlayerAim);
            layers[1].stateMachine.defaultState = aim;
            return controller;
        }

        const string PlayerTexture = "Assets/BodyGuards/Textures/Boduguard_01_D.png";
        const string PlayerMaterialPath = "Assets/Art/Materials/Player_Bodyguard.mat";

        static Material PlayerMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, PlayerMaterialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PlayerTexture));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.35f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static AvatarMask UpperBodyMask()
        {
            var path = $"{AnimFolder}/UpperBody.mask";
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, path);
            }
            for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
                mask.SetHumanoidBodyPartActive(part, part != AvatarMaskBodyPart.Root &&
                    part != AvatarMaskBodyPart.LeftLeg && part != AvatarMaskBodyPart.RightLeg &&
                    part != AvatarMaskBodyPart.LeftFootIK && part != AvatarMaskBodyPart.RightFootIK);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        /// <summary>
        /// The caricature's normal map washes the whole model out to flat grey under URP Lit, and at
        /// ~20 cm on screen it adds no visible detail, so it is not used.
        /// </summary>
        static void RemoveNormalMaps(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                material.SetTexture("_BumpMap", null);
                material.DisableKeyword("_NORMALMAP");
                material.DisableKeyword("_SPECGLOSSMAP");
                material.SetFloat("_Smoothness", 0.3f);
                EditorUtility.SetDirty(material);
            }
        }

        /// <param name="configure">Sets stats; receives the enemy and its scaled body size in metres.</param>
        static void Build(Spec spec, StringBuilder log, System.Action<Enemy, Vector3> configure)
        {
            foreach (var loop in spec.LoopClips)
                SetLooping(loop);

            var root = new GameObject(spec.Name);
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            // Root > Model (spawn pop / recoil / lunge) > Visual (fixed art scale) > art
            var model = Child(root.transform, "Model");
            var visual = Child(model, "Visual");
            var art = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(spec.ArtPath), visual);
            PrefabUtility.UnpackPrefabInstance(art, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            art.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, spec.ArtYaw, 0f));
            foreach (var hide in spec.HideObjects ?? System.Array.Empty<string>())
            {
                var t = Find(art.transform, hide);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }
            StripPackComponents(art);
            foreach (var shadow in art.GetComponentsInChildren<Renderer>(true))
                shadow.shadowCastingMode = ShadowCastingMode.Off; // no shadow receiver in AR; saves draw calls

            var animator = art.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // keeps the muzzle correct off-screen
            animator.runtimeAnimatorController = BuildController(spec);

            // Scale to target height with feet on the ground, measured in the idle pose.
            var idle = ArtImport.Clip(spec.Idle);
            idle.SampleAnimation(art, 0f);
            var bounds = ArtPreview.Bounds(art);
            float scale = spec.Height / bounds.size.y;
            visual.localScale = Vector3.one * scale;
            visual.localPosition = new Vector3(0f, -bounds.min.y * scale, 0f);
            idle.SampleAnimation(art, 0f);

            // Collider fitted to the scaled body: an upright capsule for people, a box for long bodies.
            var size = bounds.size * scale;
            var centre = root.transform.InverseTransformPoint(ArtPreview.Bounds(art).center);
            if (spec.BoxBody)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = centre;
                box.size = new Vector3(size.x * 0.7f, size.y * 0.8f, size.z * 0.8f);
            }
            else
            {
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, spec.Height / 2f, 0f);
                capsule.radius = spec.ColliderRadius;
                capsule.height = spec.Height;
            }
            log.AppendLine($"{spec.Name}: art height {bounds.size.y:F2} -> scale {scale:F4}, body {size:F3} m");

            Transform muzzle = null;
            if (spec.GunPath != null)
            {
                var (hand, finger) = spec.Hand(art);
                (_, muzzle) = BuildGun(spec, hand, finger, root.transform, log);
            }

            var flash = root.AddComponent<HitFlash>();
            var enemyAnimator = root.AddComponent<EnemyAnimator>();
            Set(enemyAnimator, "animator", animator);
            Set(enemyAnimator, "deathDuration", spec.DeathDuration);

            Enemy enemy = spec.Name == "MeleeEnemy" ? root.AddComponent<MeleeEnemy>() : root.AddComponent<ShooterEnemy>();
            Set(enemy, "model", model);
            Set(enemy, "hitFlash", flash);
            Set(enemy, "animator", enemyAnimator);
            if (enemy is ShooterEnemy)
            {
                Set(enemy, "muzzle", muzzle);
                Set(enemy, "muzzleFlash", AddMuzzleFlash(muzzle, spec.GunLength * 0.3f));
            }
            configure(enemy, size);

            PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// Removes the art pack's own behaviour (demo sound/blend-shape scripts, per-character
        /// AudioSources, cameras, lights). All gameplay and audio is handled by this project's systems.
        /// </summary>
        static void StripPackComponents(GameObject art)
        {
            foreach (var c in art.GetComponentsInChildren<Component>(true)
                         .Where(c => c is MonoBehaviour || c is AudioSource || c is Camera || c is Light || c is AudioListener)
                         .ToList())
                Object.DestroyImmediate(c);
        }

        /// <summary>
        /// Instantiates the gun, finds its muzzle (front-most vertices) and grip (~68 % back from the
        /// muzzle, lower half), scales it, and parents it to the hand so the grip sits in the palm and
        /// the barrel points along the character's forward in the reference pose.
        /// </summary>
        static (Transform gun, Transform muzzle) BuildGun(Spec spec, Transform hand, Transform finger, Transform character, StringBuilder log)
        {
            var gunRoot = new GameObject("Gun").transform;
            var mesh = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(spec.GunPath), gunRoot);
            PrefabUtility.UnpackPrefabInstance(mesh, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            mesh.transform.SetLocalPositionAndRotation(Vector3.zero, GunToForward);
            foreach (var r in mesh.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;

            // Vertices in gunRoot space: +Z = barrel direction, +Y = top of the gun.
            var points = new List<Vector3>();
            foreach (var mf in mesh.GetComponentsInChildren<MeshFilter>())
                points.AddRange(mf.sharedMesh.vertices.Select(v => gunRoot.InverseTransformPoint(mf.transform.TransformPoint(v))));
            var min = points.Aggregate(Vector3.Min);
            var max = points.Aggregate(Vector3.Max);
            float length = max.z - min.z;
            var tip = points.Where(p => p.z > max.z - length * 0.02f).ToList();
            var muzzlePoint = tip.Aggregate(Vector3.zero, (a, p) => a + p) / tip.Count;
            var grip = new Vector3((min.x + max.x) / 2f, min.y + (max.y - min.y) * 0.42f, max.z - length * 0.68f);

            mesh.transform.localPosition = -grip; // gunRoot origin = grip
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gunRoot, false);
            muzzle.localPosition = muzzlePoint - grip;

            gunRoot.localScale = Vector3.one * (spec.GunLength / length);
            var palm = Vector3.Lerp(hand.position, finger.position, 0.5f);
            gunRoot.SetPositionAndRotation(palm, Quaternion.LookRotation(character.forward, Vector3.up));
            gunRoot.SetParent(hand, true);
            log.AppendLine($"{spec.Name}: gun length {length:F2} units, muzzle {muzzle.position:F3}, palm {palm:F3}");
            return (gunRoot, muzzle);
        }

        static MuzzleFlash AddMuzzleFlash(Transform muzzle, float size)
        {
            const string matPath = "Assets/Art/Materials/MuzzleFlash.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor", new Color(1f, 0.8f, 0.3f));
                AssetDatabase.CreateAsset(material, matPath);
            }

            var holder = new GameObject("MuzzleFlash").transform;
            holder.SetParent(muzzle, false);
            var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "Flash";
            Object.DestroyImmediate(flash.GetComponent<Collider>());
            flash.transform.SetParent(holder, false);
            flash.transform.localPosition = new Vector3(0f, 0f, size * 0.6f / muzzle.lossyScale.z);
            flash.transform.localScale = new Vector3(1f, 1f, 1.8f) * (size / muzzle.lossyScale.x);
            var r = flash.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            flash.SetActive(false); // only shown for a frame or two per shot

            var component = holder.gameObject.AddComponent<MuzzleFlash>();
            Set(component, "flash", flash);
            return component;
        }

        /// <summary>Idle ⇄ Move on Speed; Any State → Attack / Hit (triggers) / Death (Dead bool).</summary>
        static RuntimeAnimatorController BuildController(Spec spec)
        {
            if (!AssetDatabase.IsValidFolder(AnimFolder))
                AssetDatabase.CreateFolder("Assets/Art", "Animation");
            var path = $"{AnimFolder}/{spec.Name}.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            var sm = controller.layers[0].stateMachine;

            var idle = sm.AddState("Idle");
            idle.motion = ArtImport.Clip(spec.Idle);
            sm.defaultState = idle;
            var move = sm.AddState("Move");
            move.motion = ArtImport.Clip(spec.Move);
            Transition(idle.AddTransition(move), 0.15f).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            Transition(move.AddTransition(idle), 0.15f).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Death first: Any State transitions are evaluated in order.
            var death = sm.AddState("Death");
            death.motion = ArtImport.Clip(spec.Death);
            var toDeath = Transition(sm.AddAnyStateTransition(death), 0.1f);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            toDeath.canTransitionToSelf = false;

            if (spec.Attack != null)
                OneShot(sm, idle, "Attack", ArtImport.Clip(spec.Attack), "Attack");
            if (spec.Hit != null)
                OneShot(sm, idle, "Hit", ArtImport.Clip(spec.Hit), "Hit");
            return controller;
        }

        static void OneShot(AnimatorStateMachine sm, AnimatorState returnTo, string stateName, AnimationClip clip, string trigger)
        {
            var state = sm.AddState(stateName);
            state.motion = clip;
            var enter = Transition(sm.AddAnyStateTransition(state), 0.08f);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            enter.canTransitionToSelf = false;
            var exit = Transition(state.AddTransition(returnTo), 0.15f);
            exit.hasExitTime = true;
            exit.exitTime = 0.85f;
        }

        static AnimatorStateTransition Transition(AnimatorStateTransition t, float duration)
        {
            t.hasExitTime = false;
            t.duration = duration;
            return t;
        }

        /// <summary>
        /// Removes animation events from clips. The raptor pack's clips call its own sound script
        /// (e.g. "Yelp"), which is stripped in favour of the AudioManager; without this, every such
        /// event logs "has no receiver" errors.
        /// </summary>
        static void StripAnimationEvents(params string[] clipSpecs)
        {
            foreach (var group in clipSpecs.Select(ArtImport.SplitClipSpec).GroupBy(s => s.path))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(group.Key);
                var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                var names = group.Select(s => s.clipName).ToHashSet();
                bool changed = false;
                foreach (var c in clips.Where(c => names.Contains(c.name) && c.events.Length > 0))
                {
                    c.events = new AnimationEvent[0];
                    changed = true;
                }
                if (!changed)
                    continue;
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        /// <summary>Marks a clip as looping ("path.fbx" = every clip in the file, "path.fbx#Name" = just that one).</summary>
        static void SetLooping(string clipSpec)
        {
            var (path, name) = ArtImport.SplitClipSpec(clipSpec);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            var targets = clips.Where(c => name == null || c.name == name).ToList();
            if (targets.All(c => c.loopTime))
                return;
            foreach (var c in targets)
                c.loopTime = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static void SetEnemyStats(Enemy enemy, int health, int damage, float speed, float range, float cooldown, int score)
        {
            var so = new SerializedObject(enemy);
            so.FindProperty("baseHealth").intValue = health;
            so.FindProperty("baseAttackDamage").intValue = damage;
            so.FindProperty("moveSpeed").floatValue = speed;
            so.FindProperty("attackRange").floatValue = range;
            so.FindProperty("attackCooldown").floatValue = cooldown;
            so.FindProperty("scoreValue").intValue = score;
            so.FindProperty("separationRadius").floatValue = 0.12f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform Find(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
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

        static void SetBool(Object target, string property, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
