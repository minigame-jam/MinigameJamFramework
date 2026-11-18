using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HopPot {
    public class GameManager : MonoBehaviour {
        public float startTimer = 3;
        public float duration = 60;
        public float endTimer = 2;
        private MinigameManager.Ranking _ranking;
        [HideInInspector] public float timer = 0;
        [SerializeField] public ScoreTracker scoreTracker;

        [SerializeField] private FrogMovement frog1;
        [SerializeField] private FrogMovement frog2;
        [SerializeField] private FrogMovement frog3;
        [SerializeField] private WitchMovement witch;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start() {
            _ranking = new();
            _ranking.SetAllPlayersToRank(1); // set all players to first place

            //int numPlayers = PlayerManager.GetNumPlayers();
            //int witchNum = 1;
            //frog1.checkPresence(numPlayers,witchNum);
            //frog2.checkPresence(numPlayers,witchNum);
            //frog3.checkPresence(numPlayers,witchNum);

            StartCoroutine(GameTimer());
        }

        IEnumerator GameTimer() {
            // Start countdown
            FrogMovement.isPawnInputEnabled = false;
            WitchMovement.isPawnInputEnabled = false;

            // Maybe some ckind of countdown animation

            yield return new WaitForSeconds(startTimer);

            FrogMovement.isPawnInputEnabled = true;
            WitchMovement.isPawnInputEnabled = true;
            // Timer
            while (timer < duration) {
                timer += Time.deltaTime;
                yield return null;
            }
            StartCoroutine(EndMinigame());
        }

        public int getTime() {
            return (int) timer;
        }

        IEnumerator EndMinigame() {
            
            // Put End animation here

            // Rank players here
            List<int> playerScores = scoreTracker.getScores();
            _ranking.DetermineRankingFromScores(playerScores);

            // End
            FrogMovement.isPawnInputEnabled = false;
            WitchMovement.isPawnInputEnabled = false;
            yield return new WaitForSeconds(endTimer);
            MinigameManager.instance.EndMinigame(_ranking);
            StopAllCoroutines(); // prevent EndMinigame from being called multiple times
        }
    }
}
