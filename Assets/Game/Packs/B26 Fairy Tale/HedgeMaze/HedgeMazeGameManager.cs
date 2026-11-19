using Examples.TrumbusTrace;
using Game.Examples;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HedgeMaze {
    public class HedgeMazeGameManager : MonoBehaviour {
        // TIME VARIABLES
        [Header("Time")]
        public float roundDuration = 15;
        [HideInInspector] public float timer = 0;
        [SerializeField] public float textTime = 1;

        // SCORING VARIABLES
        [SerializeField] private GameObject roundOneHedges;
        [SerializeField] private GameObject roundTwoHedges;
        [SerializeField] private GameObject roundThreeHedges;
        [SerializeField] private MaterialUpdater updater;

        [SerializeField] List<HedgeMazePawn> players;

        //audio player
        [SerializeField] AudioSource roundChangePlayer;
        

        // UI VARIABLES
        [Header("UI")]
        [SerializeField] private GameObject roundOneText;
        [SerializeField] private GameObject roundTwoText;
        [SerializeField] private GameObject roundThreeText;
        [SerializeField] private GameObject finishedText;
        [SerializeField] private TextMeshProUGUI timerText;


        private void Start() {

            StartCoroutine(GameTimer());
        }
        IEnumerator GameTimer() {
            updater.SetMode(0);

            // Intro Animation
            //round one
            roundChangePlayer.Play();
            
            HedgeMazePawn.isPawnInputEnabled = false;
            roundOneText.SetActive(true);
            yield return new WaitForSeconds(textTime);
            roundOneText.SetActive(false);
            HedgeMazePawn.isPawnInputEnabled = true;
            
            // Timer
            while (timer < roundDuration) {
                timer += Time.deltaTime;
                timerText.text = "" + ((int)(roundDuration - timer));
                yield return null;
            }

            
            //updater.SetMode(1);
            //round 2
            roundChangePlayer.Play();
            timer = 0f;
            HedgeMazePawn.isPawnInputEnabled = false;
            roundTwoText.SetActive(true);
            roundOneHedges.SetActive(false);
            roundTwoHedges.SetActive(true);
            updater.SetMode(1);
            foreach (HedgeMazePawn pawn in players) {
                pawn.StartRound2();
            }
            yield return new WaitForSeconds(textTime);
            
            roundTwoText.SetActive(false);
            HedgeMazePawn.isPawnInputEnabled = true;
            //updater.SetMode(2);
            // Timer
            timer = 0f;
            while (timer < roundDuration) {
                timer += Time.deltaTime;
                timerText.text = "" + ((int)(roundDuration - timer));
                yield return null;
            }
            //round 3
            roundChangePlayer.Play();

            HedgeMazePawn.isPawnInputEnabled = false;
            roundThreeText.SetActive(true);
            roundTwoHedges.SetActive(false);
            roundThreeHedges.SetActive(true);
            updater.SetMode(2);
            foreach (HedgeMazePawn pawn in players) {
                pawn.StartRound3();
            }
            yield return new WaitForSeconds(textTime);
            roundThreeText.SetActive(false);
            HedgeMazePawn.isPawnInputEnabled = true;

            // Timer
            timer = 0f;
            while (timer < roundDuration) {
                timer += Time.deltaTime;
                timerText.text = "" + ((int)(roundDuration - timer));
                yield return null;
            }

            HedgeMazePawn.isPawnInputEnabled = false;
            finishedText.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(EndMinigame());
        }

        IEnumerator EndMinigame() {
            List<int> scores = new();
            foreach (HedgeMazePawn pawn in players) {
                scores.Add(pawn.score);
            }
            MinigameManager.Ranking ranking = new();
            
            ranking.DetermineRankingFromScores(scores);
            print("score: " + scores.ToString());
            print("Ranking:" + ranking.ToString());
            // End
            yield return new WaitForSeconds(2);
            MinigameManager.instance.EndMinigame(ranking);
            StopAllCoroutines(); // prevent EndMinigame from being called multiple times
        }
    }
}
