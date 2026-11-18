using UnityEngine;

namespace GnomeSpace {
    public class Gnome : MonoBehaviour
    {
        // Parameters
        [SerializeField] private float jumpForce = 6;
        [SerializeField] private float moveSpeed = 2;
        [SerializeField] private SpriteRenderer outlineSprite;
        
        // Audio Clips
        [SerializeField] private AudioClip jumpSound, deathSound;
        private AudioSource aus;

        // Movement logic
        bool grounded = true;
        Rigidbody p;
        Vector2 moveInput = Vector2.zero;

        private GnomeGameManager manager;
        private int pawnIndex;
        private bool dead = false;

        void Start() {
            p = GetComponent<Rigidbody>();
            manager = FindAnyObjectByType<GnomeGameManager>();

            aus = transform.parent.GetComponent<AudioSource>();
        }

        public void SetPlayerColor(int playerIndex) {
            Color[] playerColors = new Color[] { Color.blue, Color.red, Color.green, Color.yellow };
            
            outlineSprite.color = playerColors[playerIndex];

            pawnIndex = playerIndex;
        }

// =====================================================================================================
        // Applies movement input
        void Update() {
            p.AddForce(new Vector3(moveInput.normalized.x, 0, 0) * moveSpeed);
        }

        // sets the movement input to the provided
        public void SetMovement(Vector2 newInput) {
            moveInput = newInput;
        }

        // triggers a jump if able
        public void TriggerJump() {
            if (grounded) {
                grounded = false;
                aus.clip = jumpSound;
                aus.Play();
                print(aus);
                p.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
            }
        }

// =====================================================================================================
        // Handles grounded detection for gnomes
        // Ground layer is 10
        // Additionally should respond to being hit by water
        // Water should be on layer 15 "Projectile"
        void OnCollisionEnter(Collision collision) {
            if (collision.gameObject.layer == 10 || collision.gameObject.layer == 12) {
                grounded = true;
            }
            if (collision.gameObject.layer == 15) {
                Die();
            }
        }

        // Handles grounded exit detection for gnomes
        // Ground layer is 10
        void OnCollisionExit(Collision collision) {
            if (collision.gameObject.layer == 10) {
                grounded = false;
            }
        } 
        
// =====================================================================================================
        //Kills and removes a gnome
        private void Die() {
            if (!dead) {
                dead = true;
                manager.GnomeDeath(pawnIndex);
                aus.clip = deathSound;
                aus.Play();
                Destroy(this.gameObject);
            }
        }
    }
}