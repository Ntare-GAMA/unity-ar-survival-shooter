using UnityEngine;

namespace ARSurvival.Combat
{
    public enum Team { Player, Enemy }

    /// <summary>Anything projectiles or melee attacks can hurt (the player and every enemy type).</summary>
    public interface IDamageable
    {
        Team Team { get; }
        bool IsAlive { get; }

        /// <summary>World-space point to aim at (centre of the body, not the feet).</summary>
        Vector3 AimPoint { get; }

        void TakeDamage(int amount, Vector3 hitPoint);
    }
}
