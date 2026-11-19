using Game.MinigameFramework.Scripts.Framework.Input;
using SnowDay.Snowfort;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SnowDay.SnowFight {
    public class SnowmanController : Pawn {
        // Player Movement
        [SerializeField] private float moveSpeed = 15f;
        [SerializeField] private Rigidbody rb;
        private Vector2 moveInputValue;

        public bool isPawnInputEnabled = true;

        // Camera Rotation
        [SerializeField] private float sensitivityX = 1f;
        [SerializeField] private float sensitivityY = 1f;
        private float rotationX = 0f;
        private float rotationY = 0f;
        private Vector2 lookInputValue;

        // Shooting Snowball
        public Transform firePoint;
        public GameObject snowballPrefab;

        // Properites
        public int health = 100;
        public int lives = 3;
        public TextMeshPro playerText;
        public bool isAlive = true;

        private void Awake() {
            rb = GetComponent<Rigidbody>();
        }

        private void Start() {
            rotationX = transform.localEulerAngles.y;
            rotationY = -transform.localEulerAngles.x;
        }

        protected override void OnActionPressed(InputAction.CallbackContext context) {
            if (!isPawnInputEnabled) return;
            switch (context.action.name) {
                case "Move":
                    moveInputValue = context.ReadValue<Vector2>();
                    break;
                case "Look":
                    lookInputValue = context.ReadValue<Vector2>();
                    break;
                case "ButtonR":
                    if (health > 0) {
                        LaunchSnowball();
                    }

                    break;
            }
        }

        private void CameraRotation() {
            rotationX += lookInputValue.x * sensitivityX;
            rotationY += lookInputValue.y * sensitivityY;

            rotationY = Mathf.Clamp(rotationY, -45, 45);

            transform.localRotation = Quaternion.Euler(-rotationY, rotationX, 0f);
        }

        private void Move() {
            //Vector3 movement = new Vector3(moveInputValue.x, 0, moveInputValue.y);
            Vector3 movement =transform.forward*moveInputValue.y + transform.right * moveInputValue.x;
            movement.y = 0;
            rb.linearVelocity = movement * moveSpeed;
        }

        private void LaunchSnowball() {
            Snowball snowball = Instantiate(snowballPrefab, firePoint.position, firePoint.rotation).GetComponent<Snowball>();
            snowball.Shoot(transform.forward);
            snowball.player = gameObject;
        }

        public void TakeDamage(int damage) {
            health -= damage;
            GetComponent<HitJiggle>().Jiggle();
            if (health <= 0 && isAlive) {
                lives--;
                if(lives < 1) {
                    Die();
                }
            }
        }

        private void Update() {
            CameraRotation();

            if (transform.position.y < 0 && isAlive) {
                isAlive = false;
                TakeDamage(100);
            }
        }

        private void FixedUpdate() {
            Move();
        }

        private void Die() {
            isAlive = false;
            SnowFightManager snowFightManager = FindAnyObjectByType<SnowFightManager>();
            snowFightManager.KillPlayer(this);
            StartCoroutine(DestroySelf());
        }
        IEnumerator DestroySelf() {
            yield return new WaitForSeconds(1f);
            Destroy(gameObject);
        }
    }
}