using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine.InputSystem;
using Witch;

namespace Frog {
    public class Tongue : MonoBehaviour {

        public Frog.FrogMovement frog;
        public float distFromFrog;
        public float growSpeed = 1;
        public float maxLength;

        private float length = 0;
        private Vector2 moveInput = Vector2.zero;

        void Start() {
            
        }

        public void Extend() {
            moveInput = frog.GetMoveInput();
            Vector3 currentRotation = transform.eulerAngles;
            currentRotation.y = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
            transform.eulerAngles = currentRotation;
        }

        void Update() {
            if (length == 0){
                Extend();
            }

            length += Time.deltaTime*growSpeed;
            transform.localScale = new Vector3(1, length, 1);
            transform.position = (frog.gameObject.transform.position + new Vector3(moveInput.x, 0, moveInput.y) * (distFromFrog + length));

            if (length > maxLength) {
                Stop();
            }

        }

        void OnTriggerEnter(Collider collider) {
            if (collider.GetComponent<Frog.FrogMovement>() != null) {
                Frog.FrogMovement otherFrog = collider.GetComponent<FrogMovement>();

                if (otherFrog.frogNumber != frog.frogNumber){
                    // Make otherFrog jump towards frog
                    Stop();
                }
            }
            else if (collider.GetComponent<Frog.FlyMovement>() != null) {
                // eat fly
                Stop();
            }
            //else if (collider.GetComponent<Frog.GrapplePoint>() != null) {
            //    // jump towards grapple
            //    Stop();
            //}
        }

        void Stop() {
            length = 0;
            gameObject.SetActive(false);
        }

    }
}
