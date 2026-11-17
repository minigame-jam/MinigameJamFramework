using Game.MinigameFramework.Scripts.Framework.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Starter.TeamSubsceneSplitscreen {
    public class StarterPawn : Pawn {
        Vector2 _moveInput = Vector2.zero;

        public Team team;

        protected override void OnPawnBound(int index) {
            GetComponent<MeshRenderer>().material = StarterGameManager.instance.materials[index];
            StarterGameManager.instance.AddPlayerToTeam(index, team);
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