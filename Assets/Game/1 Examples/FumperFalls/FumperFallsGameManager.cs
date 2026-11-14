using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using TMPro;
using UnityEngine;

namespace Examples.FumperFalls {
    public class FumperFallsGameManager : MonoBehaviour {
        // TIME VARIABLES
        [Header("Time")]
        public float duration = 20;
        [HideInInspector] public float timer = 0;
        // SCORING VARIABLES
        private MinigameManager.Ranking _ranking = new();
        private int _deaths = 0;
        // CAMERA ANIMATIONS VARIABLES
        [Header("Camera")]
        [SerializeField] Animator cameraAnimator;
        [SerializeField] private AnimationClip startAnimation;
        [SerializeField] AnimationClip endAnimation;
        // UI VARIABLES
        [Header("UI")]
        [SerializeField] private GameObject readyText;
        

        private void Start() {
            _ranking.SetAllPlayersToRank(1); // set all players to first place
            StartCoroutine(GameTimer());
        }
        IEnumerator GameTimer() {
            // Intro Animation
            FumperFallsPawn.isPawnInputEnabled = false;
            cameraAnimator.Play(startAnimation.name);
            readyText.SetActive(true);
            yield return new WaitForSeconds(startAnimation.length);
            readyText.SetActive(false);
            FumperFallsPawn.isPawnInputEnabled = true;
            // Timer
            while (timer < duration) {
                timer += Time.deltaTime;
                yield return null;
            }
            StartCoroutine(EndMinigame());
        }

        private void OnTriggerEnter(Collider other) {
            Pawn pawn = other.GetComponent<Pawn>();
            if (pawn != null) {
                KillPlayer(pawn);
            }
        }
        private void KillPlayer(Pawn pawn) {
            print($"Player {pawn.playerIndex} has been eliminated.");
            
            if(pawn.playerIndex >= 0) { // if pawn is bound to a player
                _ranking[pawn.playerIndex] = 4 - _deaths;
            }
            _deaths++; // also count deaths for pawns not bound to a player

            if (_deaths >= 3) {
                StartCoroutine(EndMinigame());
            }
        }
        
        IEnumerator EndMinigame() {
            // Animation
            cameraAnimator.Play(endAnimation.name);
            yield return new WaitForSeconds(endAnimation.length);
            // End
            yield return new WaitForSeconds(2);
            MinigameManager.instance.EndMinigame(_ranking);
            StopAllCoroutines(); // prevent EndMinigame from being called multiple times
        }
    }
}
