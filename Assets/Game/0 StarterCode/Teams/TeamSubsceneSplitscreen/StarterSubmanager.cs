using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.TeamSubsceneSplitscreen {
    /// <summary>
    /// Manages the gameplay for a subscene with multiple players in a splitscreen minigame.
    /// Communicates with StarterGameManager.
    /// </summary>
    public class StarterSubmanager : MonoBehaviour {
        public int playerIndex { get; private set; } // TODO: support a list of player indices
        [SerializeField] MeshRenderer playerMeshRenderer;
        [SerializeField] List<Material> materials = new();

        private void Awake() {
            playerIndex = StarterGameManager.instance.subscenes.Count;
            StarterGameManager.instance.subscenes.Add(this);
        }
        private void Start() {
            // TODO: set the material for all pawns in scene
            // TODO: support pawns connected after subscene is instantiated
            playerMeshRenderer.material = materials[playerIndex];
        }

        public int GetScore() {
            // Return player score
            return 0;
        }
    }
}