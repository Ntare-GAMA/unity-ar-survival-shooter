using UnityEngine;

namespace ARSurvival.Player
{
    /// <summary>
    /// Bridge between player behaviour and its Animator. Base layer: Idle / Run (Speed float) and
    /// Death (Dead bool). Upper-body layer: the aim/shoot pose, faded in while the aim stick is held
    /// so the legs keep running while the arms aim.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int DeadId = Animator.StringToHash("Dead");

        [SerializeField] Animator animator;
        [SerializeField] int aimLayer = 1;
        [SerializeField] float aimBlendSpeed = 8f;

        float aimWeight;
        float targetAim;

        public void ResetState()
        {
            animator.Rebind();
            animator.SetBool(DeadId, false);
            animator.Update(0f);
            aimWeight = targetAim = 0f;
            animator.SetLayerWeight(aimLayer, 0f);
        }

        /// <param name="normalizedSpeed">0 = standing, 1 = full movement speed.</param>
        public void SetSpeed(float normalizedSpeed) => animator.SetFloat(SpeedId, normalizedSpeed, 0.1f, Time.deltaTime);

        public void SetAiming(bool aiming) => targetAim = aiming ? 1f : 0f;

        public void PlayDeath()
        {
            targetAim = 0f;
            animator.SetBool(DeadId, true);
        }

        void Update()
        {
            aimWeight = Mathf.MoveTowards(aimWeight, targetAim, aimBlendSpeed * Time.deltaTime);
            animator.SetLayerWeight(aimLayer, aimWeight);
        }
    }
}
