using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.Splitscreen {
    public class StarterGameManager : MonoBehaviour {
        private MinigameManager.Ranking _ranking = new();

        public void EndMinigame() {
            // TODO: Determine player rankings
            MinigameManager.instance.EndMinigame(_ranking);
        }
    }
}