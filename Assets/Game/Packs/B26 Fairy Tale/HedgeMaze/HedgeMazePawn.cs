using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HedgeMaze {
    [RequireComponent(typeof(Rigidbody))]
    public class HedgeMazePawn : Pawn {
        [Header("Movement")]
        [SerializeField] private float speed = 8f;

        [SerializeField] private float gravity = -50f;
        [SerializeField] private Vector3 round2Start;
        [SerializeField] private Vector3 round3Start;
        [SerializeField] public TextMeshProUGUI scoreText;

        //audio pickup sound
        [SerializeField] AudioSource pickupSFX;

        public int score = 0;
        private Vector2 _moveInput = Vector2.zero;
        private Rigidbody _rigidbody;
        public static bool isPawnInputEnabled = true;


        // Disable Unity's default gravity when this component is added
        private void Reset() {
            GetComponent<Rigidbody>().useGravity = false;
        }
        private void Awake() {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnTriggerEnter(Collider other) {

            CustomTag tag = other.GetComponent<CustomTag>();
            if (tag != null && tag.HasTag("Collectable")) { //collect item
                pickupSFX.Play();
                score += 1;
                other.gameObject.SetActive(false);
                scoreText.text = ""+score;
            }
        }

        public void StartRound2() {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = new Vector3();
            _rigidbody.angularVelocity = new Vector3();
            _rigidbody.isKinematic = false;
            transform.position = round2Start;
        }

        public void StartRound3() {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = new Vector3();
            _rigidbody.angularVelocity = new Vector3();
            _rigidbody.isKinematic = false;
            transform.position = round3Start;
        }

        // Handle input
        protected override void OnActionPressed(InputAction.CallbackContext context) {
            if (!isPawnInputEnabled) return;
            
            // Move
            if (context.action.name == PawnAction.Move) _moveInput = context.ReadValue<Vector2>();
        }
        
        private void Update() {
            // Gravity
            _rigidbody.linearVelocity += gravity * Time.deltaTime * Vector3.up;
            // Movement
            _rigidbody.angularVelocity += new Vector3(_moveInput.y * speed * Time.deltaTime, 0, -_moveInput.x * speed * Time.deltaTime);
            
        }

    }
}