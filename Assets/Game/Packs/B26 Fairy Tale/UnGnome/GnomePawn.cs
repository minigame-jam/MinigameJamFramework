using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GnomeSpace {
    public class GnomePawn : Pawn {
        private float speed = 8f;
        private float gravity = -50f;
        private Vector2 _moveInput = Vector2.zero;
        private Rigidbody2D _rigidbody;
        public static bool isPawnInputEnabled = true;
        private bool isGrounded = true;

        private void Reset() {
            GetComponent<Rigidbody>().useGravity = false;
        }

        private void Awake() {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        void Update() {
            if(_rigidbody.position.y > 0){
                isGrounded = false;
            } else{
                isGrounded = true;
            }
            if(isGrounded){
                // Gravity
                _rigidbody.linearVelocity += gravity * Time.deltaTime * Vector2.up;
            }
            // Movement
            _rigidbody.linearVelocity += new Vector2(_moveInput.x * speed * Time.deltaTime, _rigidbody.linearVelocity.y);
        }

        protected override void OnActionPressed(InputAction.CallbackContext context) {

 if (!isPawnInputEnabled) return;
            
            // Move
            if (context.action.name == PawnAction.Move) _moveInput = context.ReadValue<Vector2>();


            // if (context.action.name == PawnAction.Move) {
            //     _moveInput = context.ReadValue<Vector2>();
            // }

            // if (context.action.name == PawnAction.ButtonA) {
            //     // Jump
            //     if(isGrounded){
            //         _rigidbody.linearVelocity = new Vector2(0, 10);
            //     }
            // }
        }
    }
}