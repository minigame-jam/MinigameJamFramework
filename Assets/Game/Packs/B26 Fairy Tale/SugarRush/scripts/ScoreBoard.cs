using Game.MinigameFramework.Scripts.Framework.Input;
using TMPro;
using UnityEngine;

namespace SugarRush {
    /// <summary>
    /// Shows each player's score in their panel, and hides the panel of any player slot nobody is playing
    /// (no pawn bound to it). Put this on the PlayerScores object and fill in one slot per player, in player order.
    /// Scores come from SugarRushPawnManager, so candy grabs and witch steals show up as they happen.
    /// If the score text has an Animator, its "Increase" or "Decrease" state plays each time the score goes up or down.
    /// </summary>
    public class ScoreBoard : MonoBehaviour {
        [System.Serializable]
        private struct PlayerSlot {
            [Tooltip("The player's whole panel. Hidden while no pawn is bound to this player.")]
            public GameObject container;
            [Tooltip("Text that shows the player's score. An Animator on it plays \"Increase\" or \"Decrease\" when the score changes.")]
            public TMP_Text scoreText;
        }

        private static readonly int IncreaseState = Animator.StringToHash("Increase");
        private static readonly int DecreaseState = Animator.StringToHash("Decrease");

        [Tooltip("One entry per player, in order: element 0 is Player 1.")]
        [SerializeField] private PlayerSlot[] players = new PlayerSlot[4];
        [Tooltip("Where the scores come from. Found automatically if left empty.")]
        [SerializeField] private SugarRushPawnManager pawnManager;
        [Tooltip("Animator on the PlayerScores object with a \"collapse\" state, played when the round ends (see Collapse). " +
                 "Auto-found on this object if left empty.")]
        [SerializeField] private Animator screenAnimator;

        private static readonly int CollapseState = Animator.StringToHash("collapse");

        private Pawn[] _pawns;
        private Animator[] _animators;
        // The score each panel shows now, to tell whether a change went up or down
        private int[] _shownScores;

        private void Awake() {
            if (pawnManager == null) pawnManager = FindAnyObjectByType<SugarRushPawnManager>();
            if (pawnManager == null) Debug.LogWarning($"{nameof(ScoreBoard)} on {name}: no {nameof(SugarRushPawnManager)} in the scene, so scores won't update.", this);
            if (screenAnimator == null) screenAnimator = GetComponent<Animator>();

            // Include hidden pawns: a pawn with no player starts hidden, and appears if one joins later
            _pawns = FindObjectsByType<Pawn>(FindObjectsInactive.Include);

            _shownScores = new int[players.Length];
            _animators = new Animator[players.Length];
            for (int i = 0; i < players.Length; i++) {
                if (players[i].scoreText != null) _animators[i] = players[i].scoreText.GetComponent<Animator>();
            }
        }

        // OnEnable runs before any Start, so the listeners are in place before PawnManager binds the players in its Start
        private void OnEnable() {
            if (pawnManager != null) pawnManager.onScoreChanged.AddListener(OnScoreChanged);
            foreach (Pawn pawn in _pawns) {
                if (pawn != null) pawn.onPlayerIndexChanged.AddListener(OnPawnBindingChanged);
            }

            for (int i = 0; i < players.Length; i++) {
                ShowScore(i, pawnManager != null ? pawnManager.GetScore(i) : 0);
            }
            RefreshVisibility();
        }

        private void OnDisable() {
            if (pawnManager != null) pawnManager.onScoreChanged.RemoveListener(OnScoreChanged);
            foreach (Pawn pawn in _pawns) {
                if (pawn != null) pawn.onPlayerIndexChanged.RemoveListener(OnPawnBindingChanged);
            }
        }

        /// Slides the whole scoreboard away by playing the screen Animator's "collapse" state. RoundTimer calls this when time runs out.
        public void Collapse() {
            if (screenAnimator == null || !screenAnimator.isActiveAndEnabled || screenAnimator.runtimeAnimatorController == null) return;
            if (!screenAnimator.HasState(0, CollapseState)) {
                Debug.LogWarning($"{nameof(ScoreBoard)} on {name}: the screen Animator has no \"collapse\" state, so the scores didn't collapse.", this);
                return;
            }
            screenAnimator.Play(CollapseState, 0, 0f);
        }

        private void OnPawnBindingChanged(int playerIndex) {
            RefreshVisibility();
        }

        private void OnScoreChanged(int playerIndex, int score) {
            if (playerIndex < 0 || playerIndex >= players.Length) return;
            int previous = _shownScores[playerIndex];
            ShowScore(playerIndex, score);

            if (score > previous) PlayAnimation(playerIndex, IncreaseState);
            else if (score < previous) PlayAnimation(playerIndex, DecreaseState);
        }

        private void ShowScore(int playerIndex, int score) {
            if (playerIndex < 0 || playerIndex >= players.Length) return;
            _shownScores[playerIndex] = score;
            TMP_Text text = players[playerIndex].scoreText;
            if (text != null) text.text = score.ToString();
        }

        private void PlayAnimation(int playerIndex, int stateHash) {
            Animator animator = _animators[playerIndex];
            if (animator == null || !animator.isActiveAndEnabled) return;
            // Start from the first frame every time, so quick back-to-back changes each get the full animation
            animator.Play(stateHash, 0, 0f);
        }

        private void RefreshVisibility() {
            for (int i = 0; i < players.Length; i++) {
                GameObject container = players[i].container;
                if (container != null) container.SetActive(IsPlayerPresent(i));
            }
        }

        private bool IsPlayerPresent(int playerIndex) {
            foreach (Pawn pawn in _pawns) {
                if (pawn != null && pawn.playerIndex == playerIndex) return true;
            }
            return false;
        }
    }
}
