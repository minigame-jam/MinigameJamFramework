using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using UnityEngine;

namespace Examples.Splitscreen {
    /// <summary>
    /// Manages multi-pawn splitscreen subscenes for players to have a separate instance of the game world.
    /// </summary>
    public class TeamSplitscreenManager : MonoBehaviour {
        [SerializeField] private List<TeamSubsceneManager> subscenePrefabs;
        [HideInInspector] public List<TeamSubsceneManager> loadedSubscenes;

        // Define splitscreen camera viewports
        public Rect[] cameraRect =
        { new Rect(0, 0,0.5f,1f), new Rect(0.5f, 0,0.5f,1f)};
        // Offset to position subscenes apart in the world
        public Vector3 subsceneOffset = new Vector3(100, 0, 0);

        private Dictionary<int, TeamSubsceneManager> _playerSubsceneBindings = new();
        
        // Singleton instance allows subscenes to access this manager
        public static TeamSplitscreenManager instance;
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
                ConnectPlayerToSubscene(i);
            }
            // Listen for new player connections
            PlayerManager.onPlayerConnected.AddListener(OnPlayerConnected);
        }

        private void OnPlayerConnected(int playerIndex) {
            // Create new subscene for newly connected player
            if (!_playerSubsceneBindings.ContainsKey(playerIndex)) {
                ConnectPlayerToSubscene(playerIndex);
            }
            // Reconnect player to existing subscene
            else {
                Pawn pawn = _playerSubsceneBindings[playerIndex].GetPawn(playerIndex);
                if (pawn != null) ReconnectPawn(playerIndex);
            }
        }


        private void ConnectPlayerToSubscene(int playerIndex) {
            // check to make sure there are more slots available for players
            TeamSubsceneManager subscene;
            if (loadedSubscenes.Count>0 && loadedSubscenes[loadedSubscenes.Count].IsPawnAvailable()) {
                subscene = loadedSubscenes[loadedSubscenes.Count];
            }
            else subscene = CreateSubscene();

            subscene.BindPawn(playerIndex);
            _playerSubsceneBindings.Add(playerIndex, subscene);
        }
        private TeamSubsceneManager CreateSubscene() {
            if (subscenePrefabs.Count < 1) {
                Debug.LogError("TeamSplitscreenManager: Failed to create subscene. Subscene Prefabs list is empty.");
            }

            // if there are individual prefabs for each subscene, instantitate corresponding object
            if (loadedSubscenes.Count<subscenePrefabs.Count) {
                return Instantiate(subscenePrefabs[loadedSubscenes.Count]);
            }
            // otherwise, instantitate last prefab in list
            return Instantiate(subscenePrefabs[subscenePrefabs.Count - 1]);
        }

        private void ReconnectPawn(int playerIndex) {
            Pawn pawn = _playerSubsceneBindings[playerIndex].GetPawn(playerIndex);
            if(pawn!=null) {
                PawnBindingManager.BindPlayerInputToPawn(playerIndex, pawn);
            } else {
                Debug.LogWarning($"TeamSplitscreenManager: Failed to reconnect pawn for player {playerIndex}. No pawn is bound in the subscene {_playerSubsceneBindings[playerIndex].index}.");
            }
        }
    }
}
