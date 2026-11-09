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
        [Tooltip("When enabled, connected players are dsitributed between teams.")]
        [SerializeField] bool fillAlternating = true;


        private Dictionary<int, Pawn> _assignedPawns = new Dictionary<int, Pawn>();



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


            // Randomly assign players to teams
            // Support different team sizes
            // Make sure that each team has at least one player
            // Prioritize connected players
            // Ensure assignment for players that havent joined yet     
            
            int playerCount = PlayerManager.players.Count;
            int teamASize = teamAPawns.Count;
            int teamBSize = teamBPawns.Count;

            if (teamASize + teamBSize < 4) {
                Debug.LogWarning($"TeamPawnManager: Found {teamASize + teamBSize} pawns. Required pawn count is 4.");
            }
            if(playerCount>teamASize+teamBSize) {
                Debug.LogError($"TeamPawnManager: Failed to connect pawns. Found {teamASize + teamBSize} pawns. Found {playerCount} players.");
                return;
            }
            if (teamASize + teamBSize >= 4) {
                Debug.LogError($"TeamPawnManager: Failed to connect pawns. Found {teamASize+teamBSize} pawns. Required pawn count is 4.");
                return;
            }

            List<int> shuffledIndexList = new();
            for(int i = 0; i < playerCount; i++) shuffledIndexList.Add(i);
            shuffledIndexList.Shuffle();

            int a = 0, b = 0;

            AssignTeamToPlayers(shuffledIndexList, 0, 0, out a, out b);

            if(playerCount<4) {
                shuffledIndexList.Clear();
                for (int i = playerCount; i < 4; i++) shuffledIndexList.Add(i);
                shuffledIndexList.Shuffle();
                AssignTeamToPlayers(shuffledIndexList, a, b, out _, out _, offset: playerCount);
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
            if (pawn.playerIndex>=-1) {
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

        private void AssignTeamToPlayers(List<int> indexList, int a, int b, out int aOut, out int bOut, int offset = 0) {
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
            aOut = a;
            bOut = b;
        }


        private void AssignDebugTeams() {
            int teamASize = teamAPawns.Count;
            int teamBSize = teamBPawns.Count;
            // Fill team A and B with alternating players
            if (fillAlternating) {
                int a = 0, b = 0;
                for (int i = 0; i < 4; i++) {
                    if ((i % 2 == 0 || b > teamBSize) && a < teamASize) {
                        _assignedPawns.Add(i, teamAPawns[a]);
                        a++;
                    } else if(b<teamBSize) {
                        _assignedPawns.Add(i, teamBPawns[b]);
                        b++;
                    }
                    else {
                        Debug.LogError($"TeamPawnManager: Both teams full. Found {teamASize + teamBSize} pawns. Required pawn count is 4.");
                    }
                }
            }
            // Fill team A before team B
            else {
                for (int i = 0; i < teamASize; i++) _assignedPawns.Add(i, teamAPawns[i]);
                for (int i = 0; i < teamBSize; i++) _assignedPawns.Add(teamAPawns.Count + i, teamBPawns[i]);
            }
        }
    }
}