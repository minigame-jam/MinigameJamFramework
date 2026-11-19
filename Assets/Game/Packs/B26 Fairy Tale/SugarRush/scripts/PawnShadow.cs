using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;

namespace SugarRush {
    /// <summary>
    /// Shadow under a pawn, tinted a darker shade of its player's color and partly see-through.
    /// Put this on a child of the pawn, next to a SpriteRenderer showing PawnShadow.png (a white shape, so the tint is the final color).
    /// Keep the SpriteRenderer's Order in Layer below the pawn's sprite and above the floor so it draws between them.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PawnShadow : MonoBehaviour {
        [Tooltip("How much darker than the player's color. 0 = the player's color, 1 = black.")]
        [Range(0f, 1f)]
        [SerializeField] private float darkness = 0.5f;
        [Tooltip("How see-through the shadow is. 0 = invisible, 1 = solid.")]
        [Range(0f, 1f)]
        [SerializeField] private float opacity = 0.5f;

        private SpriteRenderer _spriteRenderer;
        private Pawn _pawn;

        // Awake rather than Start: PawnManager binds players in its Start, which may run before this one's
        private void Awake() {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _pawn = GetComponentInParent<Pawn>();
            if (_pawn == null) {
                Debug.LogWarning($"{nameof(PawnShadow)} on {name} has no Pawn above it, so it can't pick a player color.", this);
                return;
            }

            _pawn.onPlayerIndexChanged.AddListener(ApplyColor);
            ApplyColor(_pawn.playerIndex);
        }

        private void OnDestroy() {
            if (_pawn != null) _pawn.onPlayerIndexChanged.RemoveListener(ApplyColor);
        }

        private void ApplyColor(int playerIndex) {
            Color color = PlayerColors.GetDarker(playerIndex, darkness);
            color.a *= opacity;
            _spriteRenderer.color = color;
        }
    }
}
