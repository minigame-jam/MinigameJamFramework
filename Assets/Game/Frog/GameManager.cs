using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using TMPro;
using UnityEngine;

namespace Frog {
    public class GameManager : MonoBehaviour {
        public float startTimer = 3;
        public float duration = 60;
        public float endTimer = 2;
        private MinigameManager.Ranking _ranking = new();
        [HideInInspector] public float timer = 0;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start() {
            _ranking.SetAllPlayersToRank(1); // set all players to first place
            StartCoroutine(GameTimer());
        }

        IEnumerator GameTimer() {
            // Start countdown
            Frog.FrogMovement.isPawnInputEnabled = false;
            Witch.WitchMovement.isPawnInputEnabled = false;

            // Maybe some ckind of countdown animation

            yield return new WaitForSeconds(startTimer);

            Frog.FrogMovement.isPawnInputEnabled = true;
            Witch.WitchMovement.isPawnInputEnabled = true;
            // Timer
            while (timer < duration) {
                timer += Time.deltaTime;
                yield return null;
            }
            StartCoroutine(EndMinigame());
        }

        IEnumerator EndMinigame() {
            
            // Put End animation here

            // Rank players here

            // End
            Frog.FrogMovement.isPawnInputEnabled = false;
            Witch.WitchMovement.isPawnInputEnabled = false;
            yield return new WaitForSeconds(endTimer);
            MinigameManager.instance.EndMinigame(_ranking);
            StopAllCoroutines(); // prevent EndMinigame from being called multiple times
        }
    }
}
