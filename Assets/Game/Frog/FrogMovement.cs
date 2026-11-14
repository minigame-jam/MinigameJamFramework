using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine.InputSystem;

namespace Frog {

    public class FrogMovement : Pawn {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip sfx_hop_1;
        [SerializeField] private AudioClip sfx_hop_2;
        [SerializeField] private AudioClip sfx_hop_3;
        [SerializeField] private AudioClip sfx_hop_4;
        [SerializeField] private AudioClip sfx_land_1;

        // How high the frogs jump
        public float jumpHeight;
        // The forward force a frog gets when jumping directionally
        public float moveSpeed;

        public bool justJumped = false;

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
            if (justJumped && IsGrounded()) {
                justJumped = false;
                playSFX("land");
            }
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
                    // Jump further up if not moving
                    if (moveInput == Vector2.zero) rigidBody.linearVelocity += Vector3.up * moveSpeed;
                    playSFX("jump");
                    justJumped = true;
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
            return rigidBody.linearVelocity.y == 0;
        }

        // Plays sound effects
        public void playSFX(string sfxTrigger) {
            int rngSFX = 1;
            if (sfxTrigger == "jump") {
                rngSFX = Random.Range(1,5);
                if (rngSFX == 1) {
                    sfxSource.PlayOneShot(sfx_hop_1);
                }
                else if (rngSFX == 2) {
                    sfxSource.PlayOneShot(sfx_hop_2);
                }
                else if (rngSFX == 3) {
                    sfxSource.PlayOneShot(sfx_hop_3);
                }
                else if (rngSFX == 4) {
                    sfxSource.PlayOneShot(sfx_hop_4);
                }
            }
            else if (sfxTrigger == "land") {
                sfxSource.PlayOneShot(sfx_land_1);
            }
        }

    }
}