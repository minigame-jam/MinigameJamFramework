using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SugarRush {
    [RequireComponent(typeof(Rigidbody2D))]
    public class HanselPawn : AbstractPawn {
        [Header("Hurt")]
        [Tooltip("Seconds the red flash and lean last after a witch lands a hit.")]
        [SerializeField, Min(0.01f)] private float hurtDuration = 0.4f;
        [Tooltip("Tint the sprite gets when hit, fading back to normal over Hurt Duration. It multiplies the sprite's own colors, so pure red (1, 0, 0) is the strongest.")]
        [SerializeField] private Color hurtColor = new Color(1f, 0.3f, 0.3f, 1f);
        [Tooltip("Degrees the sprite leans the way the hit pushes it, rocking on its feet. 0 = no lean, just the flash.")]
        [SerializeField] private float hurtTiltAngle = 20f;
        [Tooltip("How many half swings the lean rocks through as it settles (one way, then back the other way...). 0 = it just eases back upright.")]
        [SerializeField, Min(0f)] private float hurtSwings = 2f;

        // The pawn's own SpriteRenderer is on the same object as its collider, shadow and pointer, so tilting it would tilt all of those too.
        // So while hurt, the real sprite is hidden and a copy on a child draws it instead, tinted and tilted. The Animator keeps driving the real one.
        private SpriteRenderer _hurtSprite;
        private Vector3 _hurtPivot; // the pawn's feet, in its local space: the lean rocks around this point
        private float _hurtStartTime;
        private float _hurtTiltSign;
        private bool _isHurt;

        /// True while the hurt flash and lean are playing.
        public bool IsHurt => _isHurt;

        protected override void Awake() {
            base.Awake();
            CreateHurtSprite();
        }

        private void CreateHurtSprite() {
            if (spriteRenderer == null) return;

            var hurtObject = new GameObject("HurtSprite");
            hurtObject.transform.SetParent(spriteRenderer.transform, false);
            _hurtSprite = hurtObject.AddComponent<SpriteRenderer>();
            _hurtSprite.enabled = false;

            // Rock on the bottom of the collider, which sits at the feet
            Collider2D col = GetComponent<Collider2D>();
            Vector2 feet = Vector2.zero;
            if (col is BoxCollider2D box) feet = box.offset + Vector2.down * (box.size.y * 0.5f);
            else if (col != null) feet = col.offset;
            _hurtPivot = spriteRenderer.transform.InverseTransformPoint(transform.TransformPoint(feet));
        }

        /// <summary>
        /// Flashes the sprite red and knocks it into a lean that rocks back upright (see the Hurt settings). The sprite itself isn't changed.
        /// hitDirection is the way the hit pushes the pawn: the sprite leans that way. Hitting again mid-effect restarts it.
        /// </summary>
        public void PlayHurtEffect(Vector2 hitDirection) {
            if (_hurtSprite == null) return;

            _isHurt = true;
            _hurtStartTime = Time.time;
            // Pushed right, the top of the sprite tips right (a negative Z rotation). Straight up or down picks a side at random.
            _hurtTiltSign = Mathf.Abs(hitDirection.x) > 0.01f ? -Mathf.Sign(hitDirection.x) : (Random.value < 0.5f ? -1f : 1f);

            // Draw exactly where and how the real sprite draws
            _hurtSprite.sharedMaterial = spriteRenderer.sharedMaterial;
            _hurtSprite.sortingLayerID = spriteRenderer.sortingLayerID;
            _hurtSprite.sortingOrder = spriteRenderer.sortingOrder;
            _hurtSprite.spriteSortPoint = spriteRenderer.spriteSortPoint;
            _hurtSprite.maskInteraction = spriteRenderer.maskInteraction;
            _hurtSprite.drawMode = spriteRenderer.drawMode;
            _hurtSprite.size = spriteRenderer.size;

            spriteRenderer.enabled = false;
            _hurtSprite.enabled = true;
            UpdateHurtEffect();
        }

        // LateUpdate runs after the Animator, so the copy shows this frame's sprite
        private void LateUpdate() {
            UpdateHurtEffect();
        }

        private void UpdateHurtEffect() {
            if (!_isHurt) return;

            float progress = (Time.time - _hurtStartTime) / hurtDuration;
            if (progress >= 1f) {
                StopHurtEffect();
                return;
            }

            _hurtSprite.sprite = spriteRenderer.sprite;
            _hurtSprite.flipX = spriteRenderer.flipX;
            _hurtSprite.flipY = spriteRenderer.flipY;

            // Full red on the hit, easing back to normal
            _hurtSprite.color = spriteRenderer.color * Color.Lerp(hurtColor, Color.white, progress * progress);

            // Leans hard straight away, then rocks back and forth with smaller and smaller swings until it's upright
            float settle = (1f - progress) * (1f - progress);
            float angle = _hurtTiltSign * hurtTiltAngle * settle * Mathf.Cos(progress * hurtSwings * Mathf.PI);
            Quaternion tilt = Quaternion.Euler(0f, 0f, angle);
            _hurtSprite.transform.localRotation = tilt;
            _hurtSprite.transform.localPosition = _hurtPivot - tilt * _hurtPivot; // turn about the feet rather than the sprite's centre
        }

        private void StopHurtEffect() {
            _isHurt = false;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (_hurtSprite != null) _hurtSprite.enabled = false;
        }

        // Hidden mid-effect (e.g. its player left): put the real sprite back so it shows next time
        private void OnDisable() {
            StopHurtEffect();
        }
    }
}
