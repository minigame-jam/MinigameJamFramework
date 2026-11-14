using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine.InputSystem;

namespace Frog {
    public class FrogMovement : Pawn {
        // How high the frogs jump
        public float jumpHeight;
        // The forward force a frog gets when jumping directionally
        public float moveSpeed;

        // The direction of the player's movement input
        Vector2 moveInput = Vector2.zero;

        // Reference to the Rigidbody component of the frog
        Rigidbody rigidBody;

        void Start() {
            // Sets the Rigidbody reference
            rigidBody = GetComponent<Rigidbody>();
        }

        void Update() {
            // Update Code
        }

        // Handles input pressing
        protected override void OnActionPressed(InputAction.CallbackContext context) {
            // Stores the player's move input
            if (context.action.name == PawnAction.Move) {
                moveInput = context.ReadValue<Vector2>();
            }

            if (context.action.name == PawnAction.ButtonA) {
                // Jump in the input direction
                if (IsGrounded()) {
                    rigidBody.linearVelocity = new Vector3(moveInput.x*moveSpeed, jumpHeight, moveInput.y*moveSpeed);
                }
            }
        }

        // Handles letting gop of input (not used for anything yet, just kept as a reference for later)
        protected override void OnActionReleased(InputAction.CallbackContext context) {
            if (context.action.name == PawnAction.ButtonB) {
                // Stop shooting
            }
        }

        // Checks to see if the player is no longer moving downwards (aka is on the ground)
        bool IsGrounded() {
            return rigidBody.linearVelocity.y >= 0;
        }
    }
}