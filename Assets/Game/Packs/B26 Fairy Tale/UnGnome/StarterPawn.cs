using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GnomeSpace {
    public class StarterPawn : Pawn {
        Vector2 _moveInput = Vector2.zero;
        Boolean gnome = true;
        Rigidbody p;
        bool grounded = true;

        // Parameters
        [SerializeField] private float jumpForce = 6;
        [SerializeField] private float moveSpeed = 2;

        void Start() {
            p = GetComponent<Rigidbody>();
        }
        void Update() {
            // TODO: Implement movement

            p.AddForce(new Vector3(_moveInput.normalized.x, 0, 0) * moveSpeed);
                
        }

        protected override void OnActionPressed(InputAction.CallbackContext context) {

            if (gnome) {
                if (context.action.name == PawnAction.Move) {
                    _moveInput = context.ReadValue<Vector2>();
                }

                if (context.action.name == PawnAction.ButtonA && grounded) {
                    // Jump
                    print("jump!");
                    p.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
                }

                if (context.action.name == PawnAction.ButtonB) {
                    // Shoot
                    print("shoot");
                }
            } else {//if robot
                if (context.action.name == PawnAction.Move) {
                    _moveInput = context.ReadValue<Vector2>();
                }

                if (context.action.name == PawnAction.ButtonA) {
                    // Jump
                }

                if (context.action.name == PawnAction.ButtonB) {
                    // Shoot
                }
            }
        }

        protected override void OnActionReleased(InputAction.CallbackContext context) {
            if (context.action.name == PawnAction.ButtonB) {
                // Stop shooting
            }
        }


        // Handles grounded detection for gnomes
        // Ground layer is 10
        void OnCollisionEnter(Collision collision) {
            if (collision.gameObject.layer == 10) {
                grounded = true;
            }
        }

        // Handles grounded exit detection for gnomes
        // Ground layer is 10
        void OnCollisionExit(Collision collision) {
            if (collision.gameObject.layer == 10) {
                grounded = false;
            }
        }
    }
}