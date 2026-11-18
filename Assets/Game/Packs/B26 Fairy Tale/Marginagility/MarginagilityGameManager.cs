using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using UnityEngine;

namespace Marginagility {
    public class MarginagilityGameManager : MonoBehaviour {
        private MinigameManager.Ranking _ranking = new();

        float _timer;

        [SerializeField]AudioClip intro;
        [SerializeField]AudioClip outro;
        bool outroPlaying = false;
        [SerializeField]AudioClip playerHit;
        [SerializeField]AudioClip lanceHit;
        [SerializeField]AudioClip respawn;

        [SerializeField]GameObject audioPlayer;
        Sound respawnSound;

        bool _gameOver;

        void Start() {
            _timer = 60;
            FindAnyObjectByType<ScoreDisplay>().UpdateTimer(_timer);
            Instantiate(audioPlayer).GetComponent<Sound>().PlaySound(intro);
            outroPlaying = false;
            _gameOver = false;
        }

        void LateUpdate() {
            if (!_gameOver) {
                MarginagilityPawn[] pawns = FindObjectsByType<MarginagilityPawn>();
                List<MarginagilityPawn> hitPawns = new List<MarginagilityPawn>();
                List<MarginagilityPawn> disarmedPawns = new List<MarginagilityPawn>();

                for (int i = 0; i < pawns.Length; i++) {
                    if (pawns[i].playerIndex == -1 || !pawns[i].GetLance() || !pawns[i].GetRiding()) {
                        continue;
                    }

                    List<Collider2D> overlaps = new List<Collider2D>();
                    pawns[i].GetLanceHitbox().Overlap(overlaps);

                    foreach (Collider2D o in overlaps)
                    {
                        MarginagilityPawn oPawn = o.GetComponent<MarginagilityPawn>();
                        if (oPawn != null && oPawn.GetRiding() && oPawn.GetDirection() != pawns[i].GetDirection()) {
                            if (!hitPawns.Contains(oPawn)) {
                                hitPawns.Add(oPawn);
                            }

                            pawns[i].Hit();
                        }

                        oPawn = null;
                        Lance oLance= o.GetComponent<Lance>();
                        if (oLance != null) {
                            oPawn = oLance.GetOwner();
                        }
                    }

                    if (pawns[i].GetDisarm()) {
                        for (int j = 0; j < pawns.Length; j++)
                        {
                            if (i == j || pawns[j].playerIndex == -1 || pawns[j].GetDisarm() 
                                || pawns[i].GetLanceLane() != pawns[j].GetLanceLane()
                                || pawns[i].GetDirection() == pawns[j].GetDirection()
                                || !pawns[i].GetLance() || !pawns[j].GetLance()) continue;

                            BoxCollider2D pLance = pawns[i].GetLanceHitbox();
                            BoxCollider2D oLance = pawns[j].GetLanceHitbox();

                            float distance = Mathf.Abs(pLance.transform.position.x - oLance.transform.position.x);
                            float margin = (pLance.transform.localScale.x + oLance.transform.localScale.x) / 2;

                            if (distance <= margin) {

                                if (!disarmedPawns.Contains(pawns[j])) {
                                    disarmedPawns.Add(pawns[j]);
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < pawns.Length; i++) pawns[i].UnsetDisarm();

                foreach (MarginagilityPawn pawn in hitPawns)
                {
                    pawn.Fall();
                }
                if (hitPawns.Count > 0) Instantiate(audioPlayer).GetComponent<Sound>().PlaySound(playerHit);

                foreach (MarginagilityPawn pawn in disarmedPawns) {
                    pawn.DropLance();
                }
                if (disarmedPawns.Count > 0) Instantiate(audioPlayer).GetComponent<Sound>().PlaySound(lanceHit);
            }

            _timer -= Time.deltaTime;
            if (!_gameOver) FindAnyObjectByType<ScoreDisplay>().UpdateTimer(_timer);
            if (_timer <= 10.0f && !outroPlaying) {
                Instantiate(audioPlayer).GetComponent<Sound>().PlaySound(outro);
                outroPlaying = true;
            }
            if (_timer <= 0.0f) _gameOver = true;
            if (_timer <= -2.0f) EndMinigame(); 
        }

        public void PlayRespawn() {
            if (respawnSound == null) {
                respawnSound = Instantiate(audioPlayer).GetComponent<Sound>();
                respawnSound.PlaySound(respawn, true);
            } else if (respawnSound.isComplete()) {
                respawnSound.PlaySound(respawn, true);
            }
        }

        public void EndMinigame() {
            List<int> scores = FindAnyObjectByType<ScoreDisplay>().GetScores();

            _ranking.DetermineRankingFromScores(scores);
            MinigameManager.instance.EndMinigame(_ranking);
        }

        public bool isGameOver() {
            return _gameOver;
        }

        public float GetTimer() {
            return _timer;
        }
    }
}