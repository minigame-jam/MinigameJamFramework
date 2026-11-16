using Game.MinigameFramework.Scripts.Framework.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.Team {
    public class TeamStarterGameManager : MonoBehaviour {
        private MinigameManager.Ranking _ranking = new();
        [SerializeField] public List<Material> materials;

        public static TeamStarterGameManager instance;

        private void Awake() {
            if (instance == null) {
                instance = this;
            } else {
                Destroy(gameObject);
            }
        }

        public void EndMinigame() {
            // TODO: Determine player rankings
            MinigameManager.instance.EndMinigame(_ranking);
        }
    }

    public enum Team {
        TeamA = 0,
        TeamB = 1
    }
}
