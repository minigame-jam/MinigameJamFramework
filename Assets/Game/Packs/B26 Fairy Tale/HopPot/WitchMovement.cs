using Game.MinigameFramework.Scripts.Framework.Input;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;


namespace HopPot {

    public class WitchMovement : Pawn {

        bool hasFrog = false;
        public float moveSpeed;
        public static bool isPawnInputEnabled;
        public int frogsCaught;
        Vector2 moveInput = Vector2.zero;
        Rigidbody rigidBody;
        Vector3 frogPickupSize = new Vector3(3f, 2f, 3f);
        Vector3 cauldronDetectSize = new Vector3(4f, 4f, 4f);

        public static WitchMovement Instance { get; private set; }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        [SerializeField] float acceleration;
        [SerializeField] float maxSpeed;

        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip sfx_footsteps_1;
        [SerializeField] private AudioClip sfx_footsteps_2;
        [SerializeField] private AudioClip sfx_footsteps_3;
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private GameManager gm;

        [SerializeField] private int playerNum;

        float footstepTime = 0.3f;
        float footstepCooldown = 0.3f;

        bool AButtonHeld;


        void Start() {
            rigidBody = GetComponent<Rigidbody>();
            AButtonHeld = false;
            frogsCaught = 0;
        }
        protected override void OnActionPressed(InputAction.CallbackContext context) {
            if (isPawnInputEnabled){
                if (context.action.name == PawnAction.Move) {
                    moveInput = context.ReadValue<Vector2>();
                }

                if (context.action.name == PawnAction.ButtonA) {

                    if (hasFrog) {

                        AButtonHeld = true;

                        Collider[] collisions = Physics.OverlapBox(transform.position, cauldronDetectSize / 2f, transform.rotation);

                        foreach (Collider collision in collisions) {
                            if (collision.GetComponent<Cauldron>() != null) {
                                TimerBar.Instance.StartTimer();
                            }
                        }
                    } else {

                        Collider[] collisions = Physics.OverlapBox(transform.position, frogPickupSize / 2f, transform.rotation);

                        List<FrogMovement> frogs = new List<FrogMovement>();

                        foreach (Collider collision in collisions) {
                            if (collision.TryGetComponent<FrogMovement>(out FrogMovement frog)) {
                                frogs.Add(frog);
                            }
                        }

                        FrogMovement selectedFrog = null;

                        switch (frogs.Count) {
                            case 0:
                                break;

                            case 1:
                                selectedFrog = frogs[0];
                                Debug.Log(frogs[0].frogNumber);
                                break;

                            case >= 2:

                                FrogMovement closestFrog = null;
                                float closestDistance = Mathf.Infinity;

                                foreach (FrogMovement frog in frogs) {

                                    float distance = (frog.transform.position - transform.transform.position).magnitude;

                                    if (distance < closestDistance) {
                                        closestDistance = distance;
                                        closestFrog = frog;
                                    }
                                }

                                selectedFrog = closestFrog;
                                Debug.Log(closestFrog.frogNumber);

                                break;
                        }

                        if (selectedFrog != null) {
                            GrabFrog(selectedFrog);
                        }
                    }
                }
            }
        }

        protected override void OnActionReleased(InputAction.CallbackContext context) {

            AButtonHeld = false;
            if (context.action.name == PawnAction.ButtonA && hasFrog) {
                TimerBar.Instance.ResetTimer();
            }
        }

        private void GrabFrog(FrogMovement frog) {
            frog.FrogGrabbed(transform.GetComponent<WitchMovement>());
            hasFrog = true;

        }

        private void Update() {
            if (hasFrog && TimerBar.Instance.isCountDown()) {

                Collider[] collisions = Physics.OverlapBox(transform.position, cauldronDetectSize / 2f, transform.rotation);

                foreach (Collider collision in collisions) {
                    if (collision.GetComponent<Cauldron>() != null) {
                        return;
                    }
                }

                TimerBar.Instance.ResetTimer();
            }

            if (hasFrog && !TimerBar.Instance.isCountDown() && AButtonHeld) {

                Collider[] collisions = Physics.OverlapBox(transform.position, cauldronDetectSize / 2f, transform.rotation);

                foreach (Collider collision in collisions) {
                    if (collision.GetComponent<Cauldron>() != null) {
                        TimerBar.Instance.StartTimer();
                    }
                }
            }
        }

        public void SetHasFrog(bool hasFrog = false) {
            this.hasFrog = hasFrog;
        }

        public int getPlayerNum() {
            return playerNum;
        }

        public void setPlayerNum(int newNum) {
            playerNum = newNum;
        }

        private void FixedUpdate() {
            Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            Vector3 horizontalVelocity = new Vector3(
                rigidBody.linearVelocity.x,
                0f,
                rigidBody.linearVelocity.z
            );

            if (horizontalVelocity.magnitude < maxSpeed) {
                rigidBody.AddForce(
                    moveDirection * acceleration,
                    ForceMode.Acceleration
                );
            }

            if (horizontalVelocity.magnitude > 0.1) {
                if (footstepCooldown <= 0) {
                    playSFX("walk");
                    footstepCooldown = Random.Range(footstepTime - 0.02f, footstepTime + 0.02f);
                }
                else {
                    footstepCooldown -= Time.deltaTime;
                }
            }
        }
        void OnDrawGizmos() {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, frogPickupSize);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, cauldronDetectSize);
        }

        // Plays sound effects
        public void playSFX(string sfxTrigger) {
            int rngSFX = 1;
            if (sfxTrigger == "walk") {
                rngSFX = Random.Range(1,4);
                if (rngSFX == 1) {
                    sfxSource.PlayOneShot(sfx_footsteps_1);
                }
                else if (rngSFX == 2) {
                    sfxSource.PlayOneShot(sfx_footsteps_2);
                }
                else if (rngSFX == 3) {
                    sfxSource.PlayOneShot(sfx_footsteps_3);
                }
            }
        }

        public void CookFrog() {
            transform.GetComponentInChildren<FrogMovement>().Die();
            hasFrog = false;
            scoreTracker.addScore(playerNum,8);
            frogsCaught += 1;
            if (frogsCaught >= PlayerManager.GetNumPlayers() - 1) {
                gm.timer = 60;
            }
        }

        private void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
        }



    }
}
