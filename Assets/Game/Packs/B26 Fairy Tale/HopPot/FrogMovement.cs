using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine.InputSystem;

namespace HopPot {

    public class FrogMovement : Pawn {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip sfx_hop_1;
        [SerializeField] private AudioClip sfx_hop_2;
        [SerializeField] private AudioClip sfx_hop_3;
        [SerializeField] private AudioClip sfx_hop_4;
        [SerializeField] private AudioClip sfx_land_1;
        [SerializeField] private ScoreTracker scoreTracker;

        [SerializeField] private int playerNum;

        public enum FrogState {
            None,
            Grabbed
        }

        public FrogState frogState;
        public static bool isPawnInputEnabled;

        // How high the frogs jump
        public float jumpHeight;
        // The forward force a frog gets when jumping directionally
        public float moveSpeed;

        public bool justJumped = false;
        public int frogNumber;
        public Tongue tongue;

        [SerializeField] private int escapeClickAmount;
        private int currentClickAmount = 0;

        private int fliesEaten = 0;


        // The direction of the player's movement input
        Vector2 moveInput = Vector2.zero;

        // Reference to the Rigidbody component of the frog
        Rigidbody rigidBody;
        SpriteRenderer spriteRenderer;

        private FrogSpritesheet spritesheet;
        [SerializeField] List<FrogSpritesheet> frogSpritesheets;


        private void Awake() {
            // Sets the Rigidbody reference
            rigidBody = GetComponent<Rigidbody>();
            if (transform.GetChild(0).GetComponent<SpriteRenderer>() != null) {
                spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
            }
            spritesheet = frogSpritesheets[playerNum-1]; // so its not null before pawn is bound
        }
        void Start() {
            

            

            currentClickAmount = escapeClickAmount;
        }

        protected override void OnPawnBound(int index) {
            gameObject.SetActive(true);
            spritesheet = frogSpritesheets[index];
            spriteRenderer.sprite = spritesheet.idle;
            playerNum = index + 1;
        }

        public int getPlayerNum() {
            return playerNum;
        }

        public void setPlayerNum(int newNum) {
            playerNum = newNum;
        }

        public void checkPresence(int playerCount, int witchPlayer) {
            if (playerCount < playerNum || witchPlayer == playerNum) {
                gameObject.SetActive(false);
            }
            else {
                gameObject.SetActive(true);
            }
        }

        void Update() {
            // Update Code
            if (justJumped && IsGrounded()) {
                justJumped = false;
                playSFX("land");
                spriteRenderer.sprite = spritesheet.idle;
            }
        }

        // Handles input pressing
        protected override void OnActionPressed(InputAction.CallbackContext context) {
            if (isPawnInputEnabled){
                if (frogState == FrogState.Grabbed) {
                    if (context.action.name == PawnAction.ButtonA) {
                        currentClickAmount--;
                        FrogStruggle();

                        if (currentClickAmount <= 0) {
                            FrogReleased();
                        }
                    }
                } else {
                    // Stores the player's move input
                    if (context.action.name == PawnAction.Move) {
                        moveInput = context.ReadValue<Vector2>();
                    }

                    if (context.action.name == PawnAction.ButtonA) {
                        // Jump in the input direction
                        if (IsGrounded()) {
                            Jump(moveInput);
                        }

                    }
                    // Shoot tongue if b is pressed
                    if (context.action.name == PawnAction.ButtonB) {
                        tongue.gameObject.SetActive(true);
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
            currentClickAmount = escapeClickAmount;
            transform.SetParent(witch.transform);
            transform.localPosition = new Vector3(0, 1.5f, 0);
            if (transform.GetChild(0).GetComponent<SpriteRenderer>() != null)  spriteRenderer.sprite = spritesheet.captured[0];
            ChangeCurrentState(FrogMovement.FrogState.Grabbed);

        }

        public void FrogStruggle() {

            while (true) {
                int randomNumber = Random.Range(0, 4);
                if (spriteRenderer.sprite != spritesheet.captured[randomNumber]) {
                    spriteRenderer.sprite = spritesheet.captured[randomNumber];
                    break;
                }
            }
        }

        public void FrogReleased() {
            transform.GetComponentInParent<WitchMovement>().SetHasFrog();
            TimerBar.Instance.ResetTimer();
            spriteRenderer.sprite = spritesheet.idle;
            ChangeCurrentState(FrogMovement.FrogState.None);
            rigidBody.isKinematic = false;
            transform.localPosition = Vector3.zero;
            moveInput = Random.insideUnitCircle.normalized;
            rigidBody.linearVelocity = new Vector3(moveInput.x * moveSpeed * 1.5f, jumpHeight, moveInput.y * moveSpeed * 1.5f);
            transform.position = new Vector3(transform.parent.position.x, 1.5f, transform.parent.position.z);
            transform.SetParent(null);
        }

        public void Die() {
            Destroy(gameObject);
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

        public Vector2 GetMoveInput() {
            return moveInput;
        }

        public void Jump(Vector2 direction) {
            spriteRenderer.sprite =  spritesheet.jump;
            if ((direction.x < 0) != spriteRenderer.flipX) {
                spriteRenderer.flipX = !spriteRenderer.flipX;
            }
            if(direction.y > 0) {
                spriteRenderer.sprite = spritesheet.back;
            }

            rigidBody.linearVelocity = new Vector3(direction.x * moveSpeed, jumpHeight, direction.y * moveSpeed);
            // Jump further up if not moving
            if (moveInput == Vector2.zero) rigidBody.linearVelocity += Vector3.up * moveSpeed;
            playSFX("jump");
            justJumped = true;

        }

        // Eats the given number of flies and returns the result (just pass in 0 to work as a getter for fliesEaten)
        public int EatFly(int numFlies) {
            fliesEaten += numFlies;
            scoreTracker.addScore(playerNum, 1);
            return fliesEaten;
        }
    }
}