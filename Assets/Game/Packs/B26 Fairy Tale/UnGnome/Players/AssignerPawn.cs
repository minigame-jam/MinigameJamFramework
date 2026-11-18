using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GnomeSpace {
    public class AssignerPawn : Pawn {
        // Check if a relevant child is assigned to determine if gnome or robot
        [SerializeField] GameObject gnomePrefab;
        Gnome gnome = null;
        public bool IsGnome() { return gnome != null; }
        [SerializeField] GameObject robotPrefab;
        Robot robot = null;
        public bool IsRobot() { return robot != null; }

        public bool isDead = false;

        // Placement Parameters
        [SerializeField] Vector3 leftmostGnomePlacement;
        [SerializeField] float gnomePlacementOffset = 2.5f;
        [SerializeField] Vector3 robotPlacement;
        

        /// <summary>
        /// Assigns this pawn to the specified player type
        /// Uses playerIndex in relation to robotIndex to decide
        /// </summary>
        /// <param name="robotIndex">the playerIndex of whoever is assigned robot</param>
        public void AssignPlayerType(int robotIndex) {
            if (robotIndex != playerIndex) // TODO: FIX THIS, INVERTED FOR TESTING PURPOSES
            { // Assigned a gnome!
                gnome = Instantiate(gnomePrefab, transform).GetComponent<Gnome>();
                // gnome placement logic
                var placementIndex = playerIndex;
                if (robotIndex < playerIndex) placementIndex--; // shift down by one if placed the robot already
                gnome.transform.position = leftmostGnomePlacement + Vector3.right * gnomePlacementOffset * placementIndex;
                gnome.SetPlayerColor(playerIndex);
                // gnomes currently get their pawn index via SetPlayerColor
            }
            else
            { // Assigned to robot
                robot = Instantiate(robotPrefab, transform).GetComponent<Robot>();
                robot.transform.position = robotPlacement;
                robot.SetPlayerColor(playerIndex);
            }
        }


        // hands off input to the relevant embodiment
        protected override void OnActionPressed(InputAction.CallbackContext context) {
            // Gnome input passing
            if (!isDead) {
                if (IsGnome()) {
                    // movement logic
                    if (context.action.name == PawnAction.Move) {
                        Vector2 moveInput = context.ReadValue<Vector2>();
                        gnome.SetMovement(moveInput);
                    }

                    // Jump detection
                    if (context.action.name == PawnAction.ButtonA || context.action.name == PawnAction.ButtonB) {
                        gnome.TriggerJump();
                    }
                } 
                else if (IsRobot()) // check if explicitly in case we are waiting to be assigned
                {
                    // movement logic
                    if (context.action.name == PawnAction.Move) {
                        Vector2 moveInput = context.ReadValue<Vector2>();
                        robot.SetMovement(moveInput);
                    }

                    // handle spraying pressed down
                    if (context.action.name == PawnAction.ButtonA || context.action.name == PawnAction.ButtonB) {
                        robot.SetSpraying(true);
                    }
                }
            }
        }

        protected override void OnActionReleased(InputAction.CallbackContext context) {
            if (IsRobot()) {
                if (context.action.name == PawnAction.ButtonA || context.action.name == PawnAction.ButtonB) {
                   robot.SetSpraying(false);
                }
            }
        }
    }
}