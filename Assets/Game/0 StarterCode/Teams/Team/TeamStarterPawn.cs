using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Starter.Team {
    public class TeamStarterPawn : Pawn {
        Vector2 _moveInput = Vector2.zero;

        public Team team;

        private void OnEnable() => onPlayerIndexChanged.AddListener(OnPawnBound);
        private void OnDisable() => onPlayerIndexChanged.RemoveListener(OnPawnBound);
        
        protected override void OnPawnBound(int index) {
            GetComponentInChildren<MeshRenderer>().material = TeamStarterGameManager.instance.materials[index];
        }

        void Update() {
            // TODO: Implement movement
        }

        protected override void OnActionPressed(InputAction.CallbackContext context) {
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

        protected override void OnActionReleased(InputAction.CallbackContext context) {
            if (context.action.name == PawnAction.ButtonB) {
                // Stop shooting
            }
        }
    }
}