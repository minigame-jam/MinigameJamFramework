using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using TMPro;
using UnityEngine;

namespace SnowDay.SnowFight {
    public class SnowFightManager : MonoBehaviour {
        MinigameManager.Ranking ranking = new();
        private int deaths = 0;

        [SerializeField] Camera worldCamera;
        public List<Camera> playerCameras;
        [SerializeField] Animator cameraAnimator;
        [SerializeField] private AnimationClip startAnimation;
        [SerializeField] private AnimationClip endAnimation;
        [SerializeField] private GameObject crossUI;

        [SerializeField] private float timer = 60;
        [SerializeField] private TextMeshProUGUI timerText;
        private bool isGameOver = false;



        private void Start() {
            ranking.SetAllPlayersToRank(1);
            StartCoroutine(IntroCutscene());
        }

        private IEnumerator IntroCutscene() {
            SnowmanController[] pawns = FindObjectsByType<SnowmanController>();
            foreach (SnowmanController pawn in pawns) pawn.isPawnInputEnabled = false;

            cameraAnimator.Play(startAnimation.name);
            yield return new WaitForSeconds(startAnimation.length);

            worldCamera.gameObject.SetActive(false);
            foreach (Camera camera in playerCameras) camera.gameObject.SetActive(true);
            crossUI.gameObject.SetActive(true);
            foreach (SnowmanController pawn in pawns) pawn.isPawnInputEnabled = true;
            StartCoroutine(GameTimer());
        }
        private IEnumerator GameTimer() {
            float t = timer;
            timerText.text = t.ToString("F0");
            while (t>0) {
                yield return null;
                t -= Time.deltaTime;
                timerText.text = t.ToString("F0");
            }
            StopMinigame();
        }

        public void KillPlayer(Pawn pawn) {
            print($"Player {pawn.playerIndex} has been eliminated.");
            
            if(pawn.playerIndex >= 0) { // if pawn is bound to a player
                ranking[pawn.playerIndex] = 4 - deaths;
            }
            deaths++; // also count deaths for pawns not bound to a player

            if (deaths == 3) {
                StopMinigame();
            }
        }

        private void StopMinigame() {
            StartCoroutine(EndMinigame());
        }

        IEnumerator EndMinigame() {
            if (isGameOver) yield return null;
            isGameOver = true;
            yield return new WaitForSeconds(1);
            worldCamera.gameObject.SetActive(true);
            foreach (Camera camera in playerCameras) {
                if (camera == null) continue;
                camera.gameObject.SetActive(false);
            }
            crossUI.gameObject.SetActive(false);
            cameraAnimator.Play(endAnimation.name);
            yield return new WaitForSeconds(endAnimation.length);
            yield return new WaitForSeconds(1);
            MinigameManager.instance.EndMinigame(ranking);
        }
    }
}