using System;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using UnityEngine;

namespace Game.Examples {
    /// <summary>
    /// A more lightweight and practical implementation compared to ExamplePawnManager.
    /// </summary>
    public class PawnManager : MonoBehaviour {
        // This script binds pawns to player inputs
        
        [SerializeField] private List<Pawn> pawns;
        
        private void Start() {
            PlayerManager.SetMinigameActionMap();
            BindConnectedPawns();
            PlayerManager.onPlayerConnected.AddListener(BindPawn);
            PlayerManager.onPlayerDisconnected.AddListener(OnPlayerLeft);
        }

        private void BindConnectedPawns() {
            foreach(Player player in PlayerManager.players) {
                int index = player.playerIndex;
                if(index < pawns.Count) {
                    PawnBindingManager.BindPlayerInputToPawn(index, pawns[index]);
                }
            }
        }
        
        private void BindPawn(int playerIndex) {
            if (AreAllPawnsBound()) {
                Debug.LogWarning($"PawnManager: All pawns are already bound. Player index {playerIndex} was not bound.");
                return;
            }
            if(pawns.Count <= playerIndex) {
                Debug.LogError($"PawnManager: Pawn missing for player index {playerIndex}");
                return;
            }
            PawnBindingManager.BindPlayerInputToPawn(playerIndex, pawns[playerIndex]);
        }
        
        private void OnDisable() {
            PlayerManager.onPlayerConnected.RemoveListener(BindPawn); // stop listening when this component is disabled
            PlayerManager.onPlayerDisconnected.RemoveListener(OnPlayerLeft); // stop listening when this component is disabled
        }

        private void OnPlayerLeft(int playerIndex) {
            Pawn pawn = pawns[playerIndex];
            if (pawn != null) PawnBindingManager.UnbindPlayerInputFromPawn(pawn);
            
            PlayerManager.onPlayerConnected.AddListener(BindPawn);
        }
        
        private bool AreAllPawnsBound() {
            return pawns.TrueForAll(pawn => pawn.playerIndex >= 0);
        }
    }
}