using UnityEngine;

namespace SugarRush {
    /// <summary>
    /// Each player's color, looked up by player index (0 = Player 1).
    /// Same shades as the framework's P1-P4 player indicators, so everything for one player matches.
    /// </summary>
    public static class PlayerColors {
        public static readonly Color Blue = new(0.337f, 0.545f, 1f);    // Player 1
        public static readonly Color Red = new(1f, 0.459f, 0.427f);     // Player 2
        public static readonly Color Green = new(0.337f, 1f, 0.482f);   // Player 3
        public static readonly Color Yellow = new(1f, 0.843f, 0.337f);  // Player 4

        /// Used for a pawn with no player bound (index -1), or an index past Player 4.
        public static readonly Color Unassigned = Color.gray;

        private static readonly Color[] ByPlayerIndex = { Blue, Red, Green, Yellow };

        public static Color Get(int playerIndex) {
            if (playerIndex < 0 || playerIndex >= ByPlayerIndex.Length) return Unassigned;
            return ByPlayerIndex[playerIndex];
        }

        /// <param name="darkness">0 = the player's color, 1 = black.</param>
        public static Color GetDarker(int playerIndex, float darkness) {
            Color color = Get(playerIndex);
            Color darker = Color.Lerp(color, Color.black, darkness);
            darker.a = color.a; // darken, don't fade
            return darker;
        }
    }
}
