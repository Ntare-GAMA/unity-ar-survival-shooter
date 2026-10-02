using UnityEngine;

namespace ARSurvival.Environment
{
    /// <summary>
    /// Ambient fish: swims a looping path around the arena centre (its parent), with gentle
    /// bobbing, a varying radius and a side-to-side body sway. The fish models have no animation,
    /// so all motion is procedural. Purely cosmetic: no colliders, so bullets pass through.
    /// </summary>
    public class FishSwimmer : MonoBehaviour
    {
        [SerializeField] float radius = 1.3f;
        [Tooltip("How much the radius breathes in and out along the loop.")]
        [SerializeField] float radiusWobble = 0.12f;
        [SerializeField] float height = 0.3f;
        [SerializeField] float bobAmplitude = 0.04f;
        [Tooltip("Metres per second along the loop.")]
        [SerializeField] float speed = 0.15f;
        [SerializeField] bool clockwise;
        [SerializeField, Range(0f, 360f)] float startAngle;
        [SerializeField] float swayDegrees = 12f;
        [SerializeField] float swayFrequency = 3f;
        [Tooltip("Rotation that makes the model face along +Z (its swimming direction).")]
        [SerializeField] Vector3 modelRotation;

        float angle;
        float phase;

        void Awake()
        {
            angle = startAngle * Mathf.Deg2Rad;
            phase = startAngle * 0.1f;
        }

        void Update()
        {
            float direction = clockwise ? -1f : 1f;
            angle += direction * speed / Mathf.Max(0.05f, radius) * Time.deltaTime;
            Apply(Time.time);
        }

        /// <summary>Places the fish where it would be after <paramref name="seconds"/> of swimming (editor previews).</summary>
        public void PoseAt(float seconds)
        {
            float direction = clockwise ? -1f : 1f;
            phase = startAngle * 0.1f;
            angle = startAngle * Mathf.Deg2Rad + direction * speed / Mathf.Max(0.05f, radius) * seconds;
            Apply(seconds);
        }

        void Apply(float time)
        {
            float direction = clockwise ? -1f : 1f;
            var position = PathPoint(angle);
            var ahead = PathPoint(angle + direction * 0.05f);
            transform.localPosition = position;

            var forward = ahead - position;
            if (forward.sqrMagnitude > 1e-8f)
            {
                float sway = Mathf.Sin(time * swayFrequency + phase) * swayDegrees;
                transform.localRotation = Quaternion.LookRotation(forward, Vector3.up)
                                          * Quaternion.Euler(0f, sway, 0f)
                                          * Quaternion.Euler(modelRotation);
            }
        }

        Vector3 PathPoint(float a)
        {
            float r = radius + Mathf.Sin(a * 2f + phase) * radiusWobble;
            float y = height + Mathf.Sin(a * 3f + phase) * bobAmplitude;
            return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
        }
    }
}
