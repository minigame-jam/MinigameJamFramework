using Game.MinigameFramework.Scripts.Framework.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.Team {
    public class TeamStarterGameManager : MonoBehaviour {
        private MinigameManager.Ranking _ranking = new();
        [SerializeField] public List<Material> materials;
        private Dictionary<int, Team> teamPlayers;

        public static TeamStarterGameManager instance;
        private void Awake() {
            if (instance == null) {
                instance = this;
            } else {
                Destroy(gameObject);
            }
        }

        public void AddPlayerToTeam(int playerIndex, Team team) {
            if(!teamPlayers.ContainsKey(playerIndex)) {
                teamPlayers.Add(playerIndex, team);
            }
        }

        public void EndMinigame() {
            // Find winning team
            Team winningTeam = Team.Unassigned; 
            // Find players on winning team
            foreach (int playerIndex in teamPlayers.Keys) {
                if (teamPlayers[playerIndex]==winningTeam) {
                    // If player is on winning team
                }
                else {
                    // If player is on losing team
                }
            }

            // TODO: Determine player rankings
            MinigameManager.instance.EndMinigame(_ranking);
        }
    }

    public enum Team {
        TeamA,
        TeamB,
        Unassigned = -1
    }
}