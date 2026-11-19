using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using System.Timers;
using Unity.VisualScripting;
using UnityEngine;

namespace GnomeSpace {
    public class Robot : MonoBehaviour
    {
        // Parameters
        [SerializeField] private float moveSpeed = 2;

        [SerializeField] private float ballSpeed = 2;
        private float startMoveSpeed;

        // References
        [SerializeField] SpriteRenderer crosshairSprite;

        SprayTimer t;

        public GameObject b;

        // Movement logic
        Rigidbody p;
        Vector2 moveInput = Vector2.zero;

        // Spraying logic
        bool spraying = false;


        void Start() {
            p = GetComponent<Rigidbody>();
            t = GetComponent<SprayTimer>();

            startMoveSpeed = moveSpeed;
        }

        public void SetPlayerColor(int playerIndex) {
            Color[] playerColors = new Color[] { Color.blue, Color.red, Color.green, Color.yellow };

            crosshairSprite.color = playerColors[playerIndex];
        }

// =====================================================================================================
        // Applies movement input
        void Update() {
            moveSpeed = startMoveSpeed * (float)(1 - GnomeGameManager.instance.getTime() / 45.0 + 1);
            if (!spraying) moveSpeed *= 2.0f;
            
            // apply movement
            p.AddForce(new Vector3(moveInput.x, moveInput.y, 0).normalized * moveSpeed);

            if (spraying) {
                if (!t.ticking) {
                    print(ballspawner.getPosition());
                    shoot();
                    t.startTimer();
                }
            }
        }

        // sets the movement input to the provided
        public void SetMovement(Vector2 newInput) {
            moveInput = newInput;
        }

        // sets spraying boolean
        public void SetSpraying(bool setTo = true) {
            spraying = setTo;
        }

        void shoot() {
            GameObject ball = GameObject.Instantiate(b, ballspawner.getPosition(), this.transform.rotation) as GameObject;

            Rigidbody rb = ball.GetComponent("Rigidbody") as Rigidbody;
            rb.AddForce((transform.position - rb.transform.position).normalized*ballSpeed);
            print(ball);
        }
    }
}