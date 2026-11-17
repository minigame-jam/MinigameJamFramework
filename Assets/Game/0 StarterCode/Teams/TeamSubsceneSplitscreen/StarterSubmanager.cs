using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.TeamSubsceneSplitscreen {
    /// <summary>
    /// Manages the gameplay for a subscene with multiple players in a splitscreen minigame.
    /// Communicates with StarterGameManager.
    /// </summary>
    public class StarterSubmanager : MonoBehaviour {
        [SerializeField]private Team team;

        private void Awake() {
            team = StarterGameManager.instance.AddSubscene(this);
            foreach(StarterPawn pawn in GetComponentsInChildren<StarterPawn>()) {
                pawn.team = team;
            }
        }

        public int GetScore() {
            // TODO: Return team score
            return 0;
        }
    }
}