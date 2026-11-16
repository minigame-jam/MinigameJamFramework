using System;
using System.Collections.Generic;
using System.Linq;
using Game.MinigameFramework.Scripts.Framework;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using UnityEngine;

namespace Game.Examples {
    /// <summary>
    /// A pawn manager that randomly assigns players to two teams.
    /// </summary>
    public class TeamPawnManager : MonoBehaviour {

        [Header("Pawns")]
        [SerializeField] private List<Pawn> teamAPawns;
        [SerializeField] private List<Pawn> teamBPawns;
        [Header("Debug")]
        [Tooltip("When debug mode is enabled, team assignments are not randomized.")]
        [SerializeField] bool debugMode = false;
        [Tooltip("When enabled, connected players are disitributed evenly between teams. When disabled, Team A is filled completely before Team B.")]
        [SerializeField] bool fillAlternating = true;


        private Dictionary<int, Pawn> _assignedPawns = new Dictionary<int, Pawn>();

        // Count of players assigned to Team A and Team B
        private int a = 0, b = 0;


        private void Start() {
            PlayerManager.SetMinigameActionMap();
            DetermineTeams();
            BindConnectedPawns();
            PlayerManager.onPlayerConnected.AddListener(BindPawn);
            PlayerManager.onPlayerDisconnected.AddListener(OnPlayerLeft);
        }

        private void DetermineTeams() {
            if (debugMode) {
                AssignDebugTeams();
                return;
            }

            int playerCount = PlayerManager.players.Count;

            #region Error Checking
            if (teamAPawns.Count + teamBPawns.Count < 4) Debug.LogWarning($"TeamPawnManager: Found {teamAPawns.Count + teamBPawns.Count} pawns. Required pawn count is 4.");
            if (teamAPawns.Count + teamBPawns.Count >= 4) {
                Debug.LogError($"TeamPawnManager: Failed to connect pawns. Found {teamAPawns.Count + teamBPawns.Count} pawns. Required pawn count is 4.");
                return;
            }
            if (playerCount> teamAPawns.Count + teamBPawns.Count) {
                Debug.LogError($"TeamPawnManager: Failed to connect pawns. Found {teamAPawns.Count + teamBPawns.Count} pawns. Found {playerCount} players.");
                return;
            }
            #endregion

            List<int> indexList = new();

            // Assign connected players
            for(int i = 0; i < playerCount; i++) indexList.Add(i);
            AssignTeamToPlayers(indexList);

            // Assign non-connected players
            if(playerCount<4) {
                indexList.Clear();
                for (int i = playerCount; i < 4; i++) indexList.Add(i);
                AssignTeamToPlayers(indexList, offset: playerCount);
            }
        }
        
        private void BindConnectedPawns() {
            foreach(Player player in PlayerManager.players) {
                int index = player.playerIndex;
                BindPawn(index);
            }
        }

        private void BindPawn(int index) {
            Pawn pawn = GetPawn(index);
            if (pawn.playerIndex>-1) {
                Debug.LogWarning($"TeamPawnManager: Pawn ({pawn.name}) is already bound. Player index {index} was not bound.");
                return;
            }
            PawnBindingManager.BindPlayerInputToPawn(index, pawn);
        }
        
        private void OnDisable() {
            PlayerManager.onPlayerConnected.RemoveListener(BindPawn); // stop listening when this component is disabled
            PlayerManager.onPlayerDisconnected.RemoveListener(OnPlayerLeft); // stop listening when this component is disabled
        }

        private void OnPlayerLeft(int index) {
            Pawn pawn = GetPawn(index);
            if (pawn != null) PawnBindingManager.UnbindPlayerInputFromPawn(pawn);

            PlayerManager.onPlayerConnected.AddListener(BindPawn);
        }
        
        private bool AreAllPawnsBound() {
            return teamAPawns.TrueForAll(pawn => pawn.playerIndex >= 0) && teamBPawns.TrueForAll(pawn => pawn.playerIndex >= 0);
        }

        private Pawn GetPawn(int index) {
            if(!_assignedPawns.ContainsKey(index)) {
                print($"TeamPawnManager: Pawn missing for player index {index}. Dictionary does not contain key.");
                return null;
            }
            return _assignedPawns[index];
        }

        /// <summary>
        /// Add Key Value pair between player index and random pawn to Dictionary for each player index in list.
        /// </summary>
        /// <param name="indexList">List of player indices to assign</param>
        /// <param name="offset">Number of players already bound. Ensures even alternation for logic between calls.</param>
        private void AssignTeamToPlayers(List<int> indexList, int offset = 0) {
            indexList.Shuffle();
            for (int i = 0; i < indexList.Count; i++) {
                if ((i + offset % 2 == 0 || b > teamBPawns.Count) && a < teamAPawns.Count) {
                    _assignedPawns.Add(indexList[i], teamAPawns[a]);
                    a++;
                } else if (b < teamBPawns.Count) {
                    _assignedPawns.Add(indexList[i], teamBPawns[b]);
                    b++;
                } else {
                    Debug.LogError($"TeamPawnManager: Both teams full. Found {teamAPawns.Count + teamBPawns.Count} pawns. Required pawn count is 4.");
                }
            }
        }


        private void AssignDebugTeams() {
            // Fill team A and B with alternating players
            if (fillAlternating) {
                int a = 0, b = 0;
                for (int i = 0; i < 4; i++) {
                    if ((i % 2 == 0 || b > teamBPawns.Count) && a < teamAPawns.Count) {
                        _assignedPawns.Add(i, teamAPawns[a]);
                        a++;
                    } else if(b< teamBPawns.Count) {
                        _assignedPawns.Add(i, teamBPawns[b]);
                        b++;
                    }
                    else {
                        Debug.LogError($"TeamPawnManager: Both teams full. Found {teamAPawns.Count + teamBPawns.Count} pawns. Required pawn count is 4.");
                    }
                }
            }
            // Fill team A before team B
            else {
                for (int i = 0; i < teamAPawns.Count; i++) _assignedPawns.Add(i, teamAPawns[i]);
                for (int i = 0; i < teamBPawns.Count; i++) _assignedPawns.Add(teamAPawns.Count + i, teamBPawns[i]);
            }
        }
    }
}