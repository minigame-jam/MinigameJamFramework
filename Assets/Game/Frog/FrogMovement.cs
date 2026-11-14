using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine.InputSystem;
using Witch;

namespace Frog {

    public class FrogMovement : Pawn {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip sfx_hop_1;
        [SerializeField] private AudioClip sfx_hop_2;
        [SerializeField] private AudioClip sfx_hop_3;
        [SerializeField] private AudioClip sfx_hop_4;
        [SerializeField] private AudioClip sfx_land_1;

        public enum FrogState {
            None,
            Grabbed
        }

        public FrogState frogState;

        // How high the frogs jump
        public float jumpHeight;
        // The forward force a frog gets when jumping directionally
        public float moveSpeed;

        public bool justJumped = false;
        public int frogNumber;

        [SerializeField] private int escapeClickAmount;
        private int currentClickAmount = 0;


        // The direction of the player's movement input
        Vector2 moveInput = Vector2.zero;

        // Reference to the Rigidbody component of the frog
        Rigidbody rigidBody;
        SpriteRenderer spriteRenderer;

        [SerializeField] private Sprite[] pickupFrogSprites;

        void Start() {
            // Sets the Rigidbody reference
            rigidBody = GetComponent<Rigidbody>();

            if (transform.GetChild(0).GetComponent<SpriteRenderer>() != null) {
                spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
            }
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
            if (frogState == FrogState.Grabbed) {
                if(context.action.name == PawnAction.ButtonA) {
                    FrogReleased();
                }
            } 
            else {
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
        }

        // Handles letting gop of input (not used for anything yet, just kept as a reference for later)
        protected override void OnActionReleased(InputAction.CallbackContext context) {
            if (context.action.name == PawnAction.ButtonB) {
                // Stop shooting
            }
        }

        public void ChangeCurrentState(FrogState newstate) {
            frogState = newstate;
        }

        public void FrogGrabbed(WitchMovement witch) {

            rigidBody.isKinematic = true;
            transform.SetParent(witch.transform);
            transform.localPosition = new Vector3(0, 1.5f, 0);
            spriteRenderer.sprite = pickupFrogSprites[0];
            ChangeCurrentState(FrogMovement.FrogState.Grabbed);

        }

        public void FrogReleased() {
            transform.GetComponentInParent<WitchMovement>().SetHasFrog();
            rigidBody.isKinematic = false;
            transform.localPosition = Vector3.zero;
            moveInput = Random.insideUnitCircle.normalized;
            rigidBody.linearVelocity = new Vector3(moveInput.x * moveSpeed * 1.5f, jumpHeight, moveInput.y * moveSpeed * 1.5f);
            transform.position = new Vector3(transform.parent.position.x, 1.5f, transform.parent.position.z);
            transform.SetParent(null);
            ChangeCurrentState(FrogMovement.FrogState.None);
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