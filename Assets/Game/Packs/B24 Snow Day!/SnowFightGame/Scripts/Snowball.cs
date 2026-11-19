using Game.MinigameFramework.Scripts.Tags;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SnowDay.SnowFight {
    public class Snowball : MonoBehaviour {
        private Rigidbody rb;
        [SerializeField] private float speedX = 10f;
        [SerializeField] private float speedY = 5f;
        public GameObject player;

        private void Awake() {
            rb = GetComponent<Rigidbody>();
        }

        public void Shoot(Vector3 playerDirection) {
            rb.AddForce(playerDirection * speedX, ForceMode.Impulse);
            rb.AddForce(Vector3.up * speedY, ForceMode.Impulse);
        }

        private void OnTriggerEnter(UnityEngine.Collider other) {
            if (other.CompareTag("Player")) {
                SnowmanController playerController = other.gameObject.GetComponentInParent<SnowmanController>();
                if (playerController != null) {
                    if (player == playerController.gameObject) return;
                    playerController.TakeDamage(1);
                }
                Destroy(gameObject);
            }
        }
        private void OnCollisionEnter(Collision collision) {
            if(collision.gameObject.HasCustomTag("Ground")) {
                Destroy(gameObject);
            }
        }

        private void Update() {
            if (transform.position.y < 2) {
                Destroy(gameObject);
            }
        }
    }
}
