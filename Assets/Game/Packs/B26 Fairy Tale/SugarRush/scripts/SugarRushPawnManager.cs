using Game.Examples;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using UnityEngine;
using UnityEngine.Events;

namespace SugarRush {
    /// <summary>
    /// The framework's PawnManager (binds each player's input to a pawn) plus every player's score this round.
    /// Scores are kept by player index (0 = Player 1), the same index the Pawns list and the framework's rankings use,
    /// so a player who disconnects and comes back keeps their score.
    /// Everything else (Start, binding, the Pawns list) is inherited unchanged; don't add a Start or OnDisable here,
    /// or the base class's ones stop running.
    /// </summary>
    public class SugarRushPawnManager : PawnManager {
        [Tooltip("Called with (player index, new score) whenever a player's score changes.")]
        public UnityEvent<int, int> onScoreChanged = new();

        private readonly int[] _scores = new int[PlayerManager.maxPlayers];

        /// <returns>The player's score this round, or 0 for an index with no player slot.</returns>
        public int GetScore(int playerIndex) {
            return IsValidIndex(playerIndex) ? _scores[playerIndex] : 0;
        }

        /// <summary>Adds points to a player's score. Negative points take score away.</summary>
        public void AddScore(int playerIndex, int points) {
            if (!IsValidIndex(playerIndex)) {
                Debug.LogWarning($"{nameof(SugarRushPawnManager)}: can't add score for player index {playerIndex}.", this);
                return;
            }

            _scores[playerIndex] += points;
            onScoreChanged.Invoke(playerIndex, _scores[playerIndex]);
        }

        /// <summary>Moves up to this many points from one player's score to another's. Never takes anyone below 0.</summary>
        /// <returns>The points actually moved: less than asked for if the victim didn't have that many.</returns>
        public int StealScore(int fromPlayerIndex, int toPlayerIndex, int points) {
            if (!IsValidIndex(fromPlayerIndex) || !IsValidIndex(toPlayerIndex) || fromPlayerIndex == toPlayerIndex) return 0;

            int stolen = Mathf.Clamp(points, 0, _scores[fromPlayerIndex]);
            if (stolen == 0) return 0;

            AddScore(fromPlayerIndex, -stolen);
            AddScore(toPlayerIndex, stolen);
            return stolen;
        }

        private bool IsValidIndex(int playerIndex) => playerIndex >= 0 && playerIndex < _scores.Length;
    }
}
