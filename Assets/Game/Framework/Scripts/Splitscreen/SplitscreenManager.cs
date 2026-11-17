using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using UnityEngine;

namespace Examples.Splitscreen {
    /// <summary>
    /// Manages splitscreen subscenes for each player to have a separate instance of the game world.
    /// </summary>
    public class SplitscreenManager : MonoBehaviour {
        [SerializeField] private SubsceneManager subscenePrefab;
        [HideInInspector] public List<SubsceneManager> loadedSubscenes;

        // Define splitscreen camera viewports
        public Rect[] cameraRect =
        { new Rect(0, 0.5f,0.5f,0.5f), new Rect(0.5f, 0.5f,0.5f,0.5f), new Rect(0, 0,0.5f,0.5f), new Rect(0.5f, 0,0.5f,0.5f) };
        // Offset to position subscenes apart in the world
        public Vector3 subsceneOffset = new Vector3(100, 0, 0);
        
        // Singleton instance allows subscenes to access this manager
        public static SplitscreenManager instance;
        private void Awake() {
            if (instance == null) {
                instance = this;
            }
            else {
                Destroy(gameObject);
            }
        }

        private void Start() {
            PlayerManager.SetMinigameActionMap(); 
            
            // Instantiate subscenes for already connected players
            for (int i = 0; i < PlayerManager.players.Count; i++) {
                Instantiate(subscenePrefab);
            }
            // Listen for new player connections
            PlayerManager.onPlayerConnected.AddListener(OnPlayerConnected);
        }

        private void OnPlayerConnected(int playerIndex) {
            // Create new subscene for newly connected player
            if (playerIndex == loadedSubscenes.Count) {
                Instantiate(subscenePrefab);
            }
            // Reconnect player to existing subscene
            else if (playerIndex < loadedSubscenes.Count && loadedSubscenes[playerIndex] != null) {
                Pawn pawn = loadedSubscenes[playerIndex].GetComponentInChildren<Pawn>();
                PawnBindingManager.BindPlayerInputToPawn(playerIndex, pawn);
            }
        }
    }
}
