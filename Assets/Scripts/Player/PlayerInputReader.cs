using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ARSurvival.Player
{
    /// <summary>
    /// Twin-stick input. On a phone the two on-screen joysticks write to a virtual gamepad
    /// (left stick = move, right stick = aim); in the editor WASD moves and the arrow keys aim.
    /// </summary>
    public sealed class PlayerInputReader : IDisposable
    {
        readonly InputAction move;
        readonly InputAction aim;

        public PlayerInputReader()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            aim = new InputAction("Aim", InputActionType.Value, expectedControlType: "Vector2");
            aim.AddBinding("<Gamepad>/rightStick");
            aim.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }

        public Vector2 Move => Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        public Vector2 Aim => Vector2.ClampMagnitude(aim.ReadValue<Vector2>(), 1f);

        public void Enable()
        {
            move.Enable();
            aim.Enable();
        }

        public void Disable()
        {
            move.Disable();
            aim.Disable();
        }

        public void Dispose()
        {
            move.Dispose();
            aim.Dispose();
        }
    }
}
