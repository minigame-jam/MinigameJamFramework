using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.SubsceneSplitscreen {
    /// <summary>
    /// Manages minigame state across separate individual subscenes for each player.
    /// Communicates with each StarterSubmanager.
    /// </summary>
    public class StarterGameManager : MonoBehaviour {
        [HideInInspector]public List<StarterSubmanager> subscenes = new();
        
        public static StarterGameManager instance;
        private void Awake() {
            if (instance == null) {
                instance = this;
            }
            else {
                Destroy(gameObject);
            }
        }

        public void EndMinigame() {
            List<int> scores = new();
            foreach (StarterSubmanager subscene in subscenes) {
                scores.Add(subscene.GetScore());
            }
            MinigameManager.Ranking ranking = new();
            ranking.DetermineRankingFromScores(scores);
            // End minigame
            MinigameManager.instance.EndMinigame(ranking);
        }
    }
}