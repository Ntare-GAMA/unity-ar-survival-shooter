using UnityEngine;

namespace ARSurvival.Combat
{
    /// <summary>
    /// Brief flash at a gun's muzzle. The flash mesh is a child that is toggled on and off, so
    /// nothing is created or destroyed per shot.
    /// </summary>
    public class MuzzleFlash : MonoBehaviour
    {
        [SerializeField] GameObject flash;
        [SerializeField] float duration = 0.05f;

        float remaining;

        void Awake() => flash.SetActive(false);

        public void Play()
        {
            remaining = duration;
            flash.SetActive(true);
            flash.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }

        void Update()
        {
            if (remaining <= 0f)
                return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
                flash.SetActive(false);
        }

        void OnDisable()
        {
            remaining = 0f;
            flash.SetActive(false);
        }
    }
}
