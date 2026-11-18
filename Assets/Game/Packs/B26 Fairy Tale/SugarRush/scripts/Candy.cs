using UnityEngine;

namespace SugarRush {
    /// <summary>
    /// A piece of candy. A Hansel grabs it by touching it: Points are added to that player's score on the
    /// SugarRushPawnManager straight away, the candy's hitbox switches off so nobody else can touch it, and it plays
    /// its grab animation before removing itself. Witches pass straight through without grabbing it.
    /// The collider must be a trigger. Pawns have a Rigidbody2D, which is what makes the trigger fire.
    /// Animation: the Animator (on a child, so clips move only the visual) starts in its spawn state, which moves on to
    /// idle by itself. Grabbing plays the state named grab, which should have Loop Time off.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Candy : MonoBehaviour {
        [Tooltip("Score a Hansel gets for grabbing this candy.")]
        [SerializeField] private int points = 2;
        [Tooltip("Each candy shows one of these at random, e.g. every slice of candy_sheet. Leave empty to keep the Sprite Renderer's sprite.")]
        [SerializeField] private Sprite[] sprites = { };
        [Tooltip("Animator with a grab state, played when the candy is grabbed. Auto-found on this object or a child if left empty.")]
        [SerializeField] private Animator animator;
        [Tooltip("Played when a Hansel grabs this candy. It plays from the Hansel, so it isn't cut off when the candy removes itself.")]
        [SerializeField] private AudioClip pickupSound;
        [Tooltip("Pickup sound loudness, as a fraction of the clip's normal volume (0.5 = half).")]
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.5f;
        [Tooltip("Each pickup plays at a random speed between Min and Max (1 = normal). Faster is also higher pitched.")]
        [SerializeField, Range(0.1f, 3f)] private float pickupMinPitch = 1.2f;
        [SerializeField, Range(0.1f, 3f)] private float pickupMaxPitch = 1.4f;

        private static readonly int GrabState = Animator.StringToHash("grab");

        private static SugarRushPawnManager _pawnManager;
        private Collider2D _collider;
        private bool _isGrabbed;

        public int Points => points;
        /// True from the moment a Hansel touches it. The candy stays in the scene a little longer to play its grab animation.
        public bool IsGrabbed => _isGrabbed;

        private void Awake() {
            _collider = GetComponent<Collider2D>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            PickRandomSprite();
        }

        private void PickRandomSprite() {
            if (sprites == null || sprites.Length == 0) return;

            Sprite sprite = sprites[Random.Range(0, sprites.Length)];
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null && spriteRenderer != null) spriteRenderer.sprite = sprite;
        }

        // Runs when the component is added: candy is picked up by walking through it, not bumped into
        private void Reset() {
            GetComponent<Collider2D>().isTrigger = true;
        }

        // Once grabbed, wait for the grab animation to play out, then remove the candy
        private void Update() {
            if (_isGrabbed && HasGrabAnimationFinished()) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (_isGrabbed) return; // two Hansels reached it in the same physics step, the first one gets it

            HanselPawn hansel = other.GetComponentInParent<HanselPawn>();
            if (hansel == null || !hansel.IsBound) return;

            Grab(hansel);
        }

        private void Grab(HanselPawn hansel) {
            _isGrabbed = true;
            _collider.enabled = false; // lose the hitbox while the grab animation plays
            hansel.PlaySound(pickupSound, pickupVolume, Random.Range(pickupMinPitch, pickupMaxPitch));

            if (_pawnManager == null) _pawnManager = FindAnyObjectByType<SugarRushPawnManager>();
            if (_pawnManager != null) _pawnManager.AddScore(hansel.playerIndex, points);
            else Debug.LogWarning($"{nameof(Candy)}: no {nameof(SugarRushPawnManager)} in the scene, so this candy's points weren't counted.", this);

            if (CanPlayGrabAnimation()) animator.Play(GrabState, 0, 0f);
            else Destroy(gameObject); // nothing to wait for
        }

        private bool CanPlayGrabAnimation() {
            return animator != null && animator.isActiveAndEnabled && animator.HasState(0, GrabState);
        }

        // normalizedTime counts past 1 once the clip has played through (it holds its last frame, since Loop Time is off)
        private bool HasGrabAnimationFinished() {
            if (animator == null) return false; // Grab already destroyed the candy without an animation
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            return state.shortNameHash == GrabState && state.normalizedTime >= 1f;
        }
    }
}
