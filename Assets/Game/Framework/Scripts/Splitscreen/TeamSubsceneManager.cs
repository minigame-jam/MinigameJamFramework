using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;

namespace Examples.Splitscreen {
    /// <summary>
    /// Manages camera setup and pawn binding for a subscene containing multiple players created by TeamSplitscreenManager.
    /// </summary>
    public class TeamSubsceneManager : MonoBehaviour {
        [SerializeField] Camera cam;
        public int index { get; private set; }
        
        private TeamSplitscreenManager _superManager;

        public int maxPlayers = 2;

        private Dictionary<int, Pawn> _pawnBindings = new();
        private List<Pawn> _unboundPawns = new();

        private void Awake() {
            _superManager = TeamSplitscreenManager.instance;
            index = _superManager.loadedSubscenes.Count;
            _superManager.loadedSubscenes.Add(this);
            SetSubscenePosition();
            SetupSubsceneCamera();
            SetupPawns();
        }


        /// <summary>
        ///  Sets the position of the subscene to avoid overlapping with other subscenes.
        /// </summary>
        private void SetSubscenePosition() {
            Vector3 subscenePosition = transform.position;
            subscenePosition += _superManager.subsceneOffset * index;
            transform.position = subscenePosition;
        }
        private void SetupSubsceneCamera() {
            cam.rect = _superManager.cameraRect[index];
        }
        private void SetupPawns() {
            _unboundPawns.AddRange(GetComponentsInChildren<Pawn>());
        }
       

        public bool IsPawnAvailable() {
            return _pawnBindings.Count < maxPlayers;
        }
        public void BindPawn(int playerIndex) {
            if(_unboundPawns.Count==0) {
                Debug.LogError($"TeamSubsceneManager::BindPawn: Failed to bind pawn {playerIndex} to subscene {index}, all pawns are bound.");
                return;
            }
            Pawn pawn = _unboundPawns[0];
            PawnBindingManager.BindPlayerInputToPawn(playerIndex, pawn);
            _pawnBindings.Add(playerIndex, pawn);
        }

        public Pawn GetPawn(int playerIndex) {
            if (!_pawnBindings.ContainsKey(playerIndex)) {
                Debug.LogError($"TeamSubsceneManager::GetPawn: Failed to get pawn for player {playerIndex} in subscene {index}, no pawn for that index is bound.");
                return null;
            }
            return _pawnBindings[playerIndex];
        }
    }
}
