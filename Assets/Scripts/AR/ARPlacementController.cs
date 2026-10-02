using System.Collections.Generic;
using ARSurvival.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARSurvival.AR
{
    /// <summary>
    /// Tap-to-place for the game world. Spawns exactly one world on the first tap that hits a
    /// detected horizontal plane, anchors it, then stops plane detection and hides the trackers.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager), typeof(ARPlaneManager))]
    public class ARPlacementController : MonoBehaviour
    {
        [SerializeField] GameObject gameWorldPrefab;
        [Tooltip("Stop detecting new planes and hide existing trackers once the world is placed.")]
        [SerializeField] bool disablePlanesAfterPlacement = true;

        static readonly List<ARRaycastHit> s_Hits = new();

        ARRaycastManager raycastManager;
        ARPlaneManager planeManager;
        Camera arCamera;

        /// <summary>The placed world root, or null before placement.</summary>
        public Transform PlacedWorld { get; private set; }
        public bool IsPlaced => PlacedWorld != null;

        /// <summary>Whether taps are currently allowed to place the world (set by the game state).</summary>
        public bool PlacementEnabled { get; set; }

        void Awake()
        {
            raycastManager = GetComponent<ARRaycastManager>();
            planeManager = GetComponent<ARPlaneManager>();
            arCamera = GetComponentInChildren<Camera>();
        }

        void Update()
        {
            if (IsPlaced || !PlacementEnabled || !TryGetTapPosition(out var screenPosition))
                return;

            if (raycastManager.Raycast(screenPosition, s_Hits, TrackableType.PlaneWithinPolygon))
                PlaceWorld(s_Hits[0].pose);
        }

        static bool TryGetTapPosition(out Vector2 position)
        {
            position = default;
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return false;

            // Taps on UI buttons must not place the world.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            position = pointer.position.ReadValue();
            return true;
        }

        /// <summary>Places the world at a pose on a plane. Called on tap; public for tests and debugging.</summary>
        public void PlaceWorld(Pose hitPose)
        {
            if (IsPlaced)
                return;

            // Face the world toward the player, keeping it level on the plane.
            var toCamera = arCamera.transform.position - hitPose.position;
            toCamera.y = 0f;
            var rotation = toCamera.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toCamera) : hitPose.rotation;

            var world = Instantiate(gameWorldPrefab, hitPose.position, rotation);
            world.AddComponent<ARAnchor>();
            PlacedWorld = world.transform;

            if (disablePlanesAfterPlacement)
                SetPlaneDetection(false);

            EventBus<WorldPlacedEvent>.Raise(new WorldPlacedEvent(PlacedWorld));
        }

        /// <summary>Removes the placed world and re-enables plane detection so the player can place again.</summary>
        public void ResetPlacement()
        {
            if (PlacedWorld != null)
                Destroy(PlacedWorld.gameObject);
            PlacedWorld = null;
            SetPlaneDetection(true);
        }

        void SetPlaneDetection(bool active)
        {
            planeManager.enabled = active;
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(active);
        }
    }
}
