using System.Collections;
using UnityEngine;

namespace SugarRush {
    /// <summary>
    /// Ends the minigame once the results have played out: End Delay seconds after the confetti fires, it hands every
    /// player's place from the Results screen (#1 to #4) to the MinigameManager, which awards the points and moves on
    /// to the framework's results scene.
    /// The MinigameManager comes from the Menu scene, so pressing Play on this scene directly shows the results but
    /// stops there, with a warning in the Console.
    /// </summary>
    public class StarterGameManager : MonoBehaviour {
        [Tooltip("Results screen the places come from. Found automatically if left empty.")]
        [SerializeField] private ResultsScreen results;
        [Tooltip("Seconds after the confetti fires before the minigame ends.")]
        [SerializeField, Min(0f)] private float endDelay = 2.5f;

        private bool _isEnding;

        private void Awake() {
            // Include hidden objects: the Results screen hides itself until the round ends
            if (results == null) results = FindAnyObjectByType<ResultsScreen>(FindObjectsInactive.Include);
            if (results == null) Debug.LogWarning($"{nameof(StarterGameManager)} on {name}: no {nameof(ResultsScreen)} in the scene, so the minigame won't end by itself.", this);
        }

        private void OnEnable() {
            if (results != null) results.onResultsFinished.AddListener(EndAfterDelay);
        }

        private void OnDisable() {
            if (results != null) results.onResultsFinished.RemoveListener(EndAfterDelay);
        }

        private void EndAfterDelay() {
            if (_isEnding) return;
            _isEnding = true;
            StartCoroutine(EndAfterDelayRoutine());
        }

        private IEnumerator EndAfterDelayRoutine() {
            if (endDelay > 0f) yield return new WaitForSeconds(endDelay);
            EndMinigame();
        }

        /// <summary>Ends the minigame now, ranking the players by their places on the Results screen.</summary>
        public void EndMinigame() {
            if (MinigameManager.instance == null) {
                Debug.LogWarning($"{nameof(StarterGameManager)}: no {nameof(MinigameManager)}, so the minigame can't end. " +
                                 "It comes from the Menu scene; start from there to play through to the results scene.", this);
                return;
            }

            MinigameManager.instance.EndMinigame(GetRanking());
        }

        // Players with no place (nobody playing them) are left at 0, which the MinigameManager awards no points for
        private MinigameManager.Ranking GetRanking() {
            MinigameManager.Ranking ranking = new();
            if (results == null) return ranking;

            int playerSlots = ranking.ToList().Count;
            for (int i = 0; i < playerSlots; i++) {
                int rank = results.GetRank(i);
                if (rank > 0) ranking.SetRank(i, rank);
            }
            return ranking;
        }
    }
}
