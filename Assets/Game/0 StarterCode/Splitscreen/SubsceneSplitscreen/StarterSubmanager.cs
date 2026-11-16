using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.SubsceneSplitscreen {
    /// <summary>
    /// Manages the gameplay for a single player's subscene in a splitscreen minigame.
    /// Communicates with StarterGameManager.
    /// </summary>
    public class StarterSubmanager : MonoBehaviour {
        public int playerIndex { get; private set; }
        [SerializeField] MeshRenderer playerMeshRenderer;
        [SerializeField] List<Material> materials = new();

        private void Awake() {
            playerIndex = StarterGameManager.instance.subscenes.Count;
            StarterGameManager.instance.subscenes.Add(this);
        }
        private void Start() {
            playerMeshRenderer.material = materials[playerIndex];
        }
        
        public int GetScore() {
            // Return player score
            return 0;
        }
    }
}