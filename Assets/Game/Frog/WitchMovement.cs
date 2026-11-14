using Frog;
using Game.MinigameFramework.Scripts.Framework.Input;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Witch {

    public class WitchMovement : Pawn {


        bool hasFrog = false;
        public float moveSpeed;
        Vector2 moveInput = Vector2.zero;
        Rigidbody rigidBody;
        Vector3 boxSize = new Vector3(4f, 2f, 6f);

        void Start() {
            rigidBody = GetComponent<Rigidbody>();
        }
        protected override void OnActionPressed(InputAction.CallbackContext context) {

            if (context.action.name == PawnAction.Move) {
                moveInput = context.ReadValue<Vector2>();
            }

            if (context.action.name == PawnAction.ButtonA) {

                if (hasFrog) return;
                Collider[] collisions = Physics.OverlapBox(transform.position, boxSize / 2f, transform.rotation);

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

        private void GrabFrog(FrogMovement frog) {
            frog.FrogGrabbed(transform.GetComponent<WitchMovement>());
            hasFrog = true;

        }

        public void SetHasFrog(bool hasFrog = false) {
            this.hasFrog = hasFrog;
        }

        private void Update() {
            rigidBody.linearVelocity = new Vector3(moveInput.x * moveSpeed, 0f, moveInput.y * moveSpeed);
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, boxSize);
        }


    }
}
