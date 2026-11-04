using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.MinigameFramework.Scripts.Framework.Input {
    public abstract class Pawn : MonoBehaviour {
        /// Do not set this, it will be set by PawnBindingManager
        [HideInInspector] public int playerIndex {
            get => _playerIndex;
            set {
                _playerIndex = value;
                onPlayerIndexChanged.Invoke(value);
            }
        }
        private int _playerIndex = -1;
        [HideInInspector] public UnityEvent<int> onPlayerIndexChanged = new();

        /// <summary>
        ///     When bound to a PlayerInput by PawnBindingManager, this method will be called when the player presses any button.
        ///     To see which action was pressed, check {context.action.name}
        /// </summary>
        /// <param name="context"></param>
        protected abstract void OnActionPressed(InputAction.CallbackContext context);
        

        /// <summary>
        ///     When bound to a PlayerInput by PawnBindingManager, this method will be called when the player releases any button.
        ///     To see which action was pressed, check {context.action.name}
        /// </summary>
        /// <param name="context"></param>
        protected virtual void OnActionReleased(InputAction.CallbackContext context) { }

        public void HandleInput(InputAction.CallbackContext context) {
            if (context.action.actionMap.name != "Minigame") return;
            if (context.performed || context.action.type == InputActionType.Value) {
                OnActionPressed(context);
            }
            else if (context.canceled && context.action.type != InputActionType.Value) {
                OnActionReleased(context);
            }
        }

        private void OnDestroy() {
            // Unbind from PlayerInput when destroyed
            if (playerIndex != -1) PawnBindingManager.UnbindPlayerInputFromPawn(this);
        }
    }
}