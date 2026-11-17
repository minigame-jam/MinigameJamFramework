using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.TeamSubsceneSplitscreen {
    /// <summary>
    /// Manages minigame state across separate individual subscenes.
    /// Communicates with each StarterSubmanager.
    /// </summary>
    public class StarterGameManager : MonoBehaviour {
        public List<Material> materials = new();
        [NonSerialized]public Dictionary<Team, StarterSubmanager> subscenes = new();
        private Dictionary<int, Team> teamAssignment = new(); // Key: player index, Value: team
        
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
            Team winningTeam = Team.Unassigned;
            int teamAScore = 0, teamBScore = 0;

            // check in case a subscene is missing
            if (subscenes.ContainsKey(Team.TeamA)) teamAScore = subscenes[Team.TeamA].GetScore();
            if (subscenes.ContainsKey(Team.TeamB)) teamBScore = subscenes[Team.TeamB].GetScore();

            if(teamAScore>teamBScore) winningTeam = Team.TeamA;
            else if (teamBScore>teamAScore) winningTeam = Team.TeamB;
            else {
                //TODO: Handle potential tie
            }

            MinigameManager.Ranking ranking = new();

            // TODO: Determine player scores
            foreach(int playerIndex in teamAssignment.Keys) {
                if (teamAssignment[playerIndex]==winningTeam) {
                    // If player is on winning team
                } else {
                    // If player is on losing team
                }
            }

            // End minigame
            MinigameManager.instance.EndMinigame(ranking);
        }

        public void AddPlayerToTeam(int playerIndex, Team team) {
            if (!teamAssignment.ContainsKey(playerIndex)) {
                teamAssignment.Add(playerIndex, team);
            }
        }

        public Team AddSubscene(StarterSubmanager subscene) {
            if(!subscenes.ContainsKey(Team.TeamA)) {
                subscenes.Add(Team.TeamA, subscene);
                return Team.TeamA;
            }
            else if(!subscenes.ContainsKey(Team.TeamA)) {
                subscenes.Add(Team.TeamA, subscene);
                return Team.TeamB;
            }

            return Team.Unassigned;
        }
    }
    public enum Team {
        TeamA,
        TeamB,
        Unassigned = -1
    }
}