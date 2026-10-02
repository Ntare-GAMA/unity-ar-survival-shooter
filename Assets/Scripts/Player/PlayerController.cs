using System.Collections;
using ARSurvival.Core;
using UnityEngine;

namespace ARSurvival.Player
{
    /// <summary>
    /// Third-person twin-stick control: left stick moves, right stick aims and fires. Both sticks
    /// are relative to the phone's view, so "up" always means away from the camera. The player is
    /// kept inside the arena circle.
    /// </summary>
    [RequireComponent(typeof(PlayerHealth), typeof(PlayerShooter))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 0.5f;
        [SerializeField] float turnSpeed = 720f;
        [SerializeField] float arenaRadius = 0.92f;
        [Tooltip("How far the aim stick must be pushed before the player turns and fires.")]
        [SerializeField, Range(0.1f, 0.9f)] float aimDeadZone = 0.35f;
        [SerializeField] Transform model;
        [Tooltip("Optional. Without one the model simply tips over on death.")]
        [SerializeField] PlayerAnimator animator;

        PlayerInputReader input;
        PlayerHealth health;
        PlayerShooter shooter;
        Transform arena;
        Camera viewCamera;
        Vector3 spawnLocalPosition;
        Quaternion spawnLocalRotation;
        bool roundActive;

        void Awake()
        {
            input = new PlayerInputReader();
            health = GetComponent<PlayerHealth>();
            shooter = GetComponent<PlayerShooter>();
            arena = transform.parent;
            spawnLocalPosition = transform.localPosition;
            spawnLocalRotation = transform.localRotation;
        }

        void OnEnable()
        {
            input.Enable();
            EventBus<RoundStartedEvent>.Subscribe(OnRoundStarted);
            EventBus<RoundEndedEvent>.Subscribe(OnRoundEnded);
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
        }

        void OnDisable()
        {
            input.Disable();
            EventBus<RoundStartedEvent>.Unsubscribe(OnRoundStarted);
            EventBus<RoundEndedEvent>.Unsubscribe(OnRoundEnded);
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
        }

        void OnDestroy() => input.Dispose();

        void Update()
        {
            if (!roundActive || !health.IsAlive)
                return;

            if (viewCamera == null)
                viewCamera = Camera.main;

            var moveDirection = CameraRelative(input.Move);
            Move(moveDirection);

            // Aim stick past the dead zone = aim and fire that way; otherwise face where we walk.
            var aimStick = input.Aim;
            var aimDirection = aimStick.magnitude >= aimDeadZone ? CameraRelative(aimStick).normalized : Vector3.zero;
            Face(aimDirection != Vector3.zero ? aimDirection : moveDirection);
            shooter.Tick(aimDirection, Time.deltaTime);

            if (animator != null)
            {
                animator.SetSpeed(input.Move.magnitude);
                animator.SetAiming(aimDirection != Vector3.zero);
            }
        }

        Vector3 CameraRelative(Vector2 stick)
        {
            if (viewCamera == null || stick.sqrMagnitude < 0.0001f)
                return Vector3.zero;
            var forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, arena.up).normalized;
            var right = Vector3.ProjectOnPlane(viewCamera.transform.right, arena.up).normalized;
            return forward * stick.y + right * stick.x;
        }

        void Move(Vector3 direction)
        {
            if (direction == Vector3.zero)
                return;

            var local = arena.InverseTransformPoint(transform.position + direction * (moveSpeed * Time.deltaTime));
            local.y = 0f;
            if (local.magnitude > arenaRadius)
                local = local.normalized * arenaRadius;
            transform.position = arena.TransformPoint(local);
        }

        void Face(Vector3 direction)
        {
            direction = Vector3.ProjectOnPlane(direction, arena.up);
            if (direction.sqrMagnitude < 0.0001f)
                return;
            var targetRotation = Quaternion.LookRotation(direction, arena.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        void OnRoundStarted(RoundStartedEvent e)
        {
            StopAllCoroutines();
            transform.SetLocalPositionAndRotation(spawnLocalPosition, spawnLocalRotation);
            if (model != null)
                model.localRotation = Quaternion.identity;
            if (animator != null)
                animator.ResetState();
            shooter.ResetCooldown();
            roundActive = true;
        }

        void OnRoundEnded(RoundEndedEvent e)
        {
            roundActive = false;
            if (animator != null)
            {
                animator.SetSpeed(0f);
                animator.SetAiming(false);
            }
        }

        void OnPlayerDied(PlayerDiedEvent e)
        {
            roundActive = false;
            if (animator != null)
                animator.PlayDeath();
            else if (model != null)
                StartCoroutine(FallOver());
        }

        IEnumerator FallOver()
        {
            var start = model.localRotation;
            var end = Quaternion.Euler(-90f, 0f, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
            {
                model.localRotation = Quaternion.Slerp(start, end, t * t);
                yield return null;
            }
            model.localRotation = end;
        }
    }
}
