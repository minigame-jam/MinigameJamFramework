using UnityEngine;
using UnityEngine.Events;

namespace SugarRush {
    /// <summary>
    /// Plays the opening when the scene loads. Part 1 shows and plays its animation from the start; after Part 1 Duration
    /// it's hidden and Part 2 shows and plays its animation once through, then is hidden too.
    /// Each part's animation is its Animator's default state, restarted when the part is shown.
    /// Put this on the object above Part1 and Part2 (OpeningText). Whatever the parts were left showing in the editor,
    /// only Part 1 is visible when the game starts.
    /// The pawns' walk-in (AbstractPawn) and the camera's zoom-in (PawnFollowCamera) start at the same moment and last
    /// as long as Part 1 by default.
    /// The round starts (On Round Start) when Part 1 ends, which is when the players get control.
    /// Each part plays its sound (see Sounds) as it shows.
    /// </summary>
    public class OpeningSequence : MonoBehaviour {
        [Tooltip("Called once, when Part 1 ends and Part 2 shows: the moment the players get control. RoundTimer starts counting here.")]
        public UnityEvent onRoundStart = new();

        /// True once Part 1 has ended and the players have control.
        public bool HasRoundStarted { get; private set; }

        [Header("Part 1")]
        [Tooltip("Shown first, as soon as the game starts.")]
        [SerializeField] private GameObject part1;
        [Tooltip("Animator that plays Part 1's animation. Auto-found under Part 1 if left empty.")]
        [SerializeField] private Animator part1Animator;
        [Tooltip("Seconds Part 1 stays up before Part 2 replaces it.")]
        [SerializeField, Min(0f)] private float part1Duration = 1.5f;

        [Header("Part 2")]
        [Tooltip("Shown when Part 1 is hidden, then hidden once its animation has played once.")]
        [SerializeField] private GameObject part2;
        [Tooltip("Animator that plays Part 2's animation. Auto-found under Part 2 if left empty.")]
        [SerializeField] private Animator part2Animator;

        [Header("Sounds")]
        [Tooltip("Played once when the game starts, as Part 1 shows. It's left to finish, even after Part 2's sound starts.")]
        [SerializeField] private AudioClip part1Sound;
        [Tooltip("Played once when Part 2 shows.")]
        [SerializeField] private AudioClip part2Sound;
        [Tooltip("Plays the sounds. Auto-found on this object, or added, if left empty.")]
        [SerializeField] private AudioSource audioSource;

        // How long Part 2 stays up if it has no animation to wait for
        private const float Part2FallbackDuration = 1f;

        private enum Stage { Part1, Part2, Done }
        private Stage _stage = Stage.Done;
        private float _stageStartTime;

        private void Awake() {
            if (part1Animator == null && part1 != null) part1Animator = part1.GetComponentInChildren<Animator>(true);
            if (part2Animator == null && part2 != null) part2Animator = part2.GetComponentInChildren<Animator>(true);
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void Start() {
            if (part2 != null) part2.SetActive(false);
            ShowPart(part1, part1Animator);
            PlaySound(part1Sound);
            _stage = Stage.Part1;
        }

        // PlayOneShot, so a sound still playing (Part 1's) carries on underneath rather than being cut off
        private void PlaySound(AudioClip clip) {
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
        }

        // LateUpdate, after the animators have moved on this frame: a part is hidden before its clip
        // (which loops) can start over on screen
        private void LateUpdate() {
            switch (_stage) {
                case Stage.Part1:
                    if (GetTimeShown(part1Animator) < part1Duration) return;
                    if (part1 != null) part1.SetActive(false);
                    ShowPart(part2, part2Animator);
                    PlaySound(part2Sound);
                    _stage = Stage.Part2;
                    HasRoundStarted = true;
                    onRoundStart.Invoke();
                    break;

                case Stage.Part2:
                    if (!HasPlayedThrough(part2Animator)) return;
                    if (part2 != null) part2.SetActive(false);
                    _stage = Stage.Done;
                    break;
            }
        }

        private void ShowPart(GameObject part, Animator animator) {
            _stageStartTime = Time.time;
            if (part == null) return;

            part.SetActive(true);
            if (!CanAnimate(animator)) return;
            animator.Rebind();     // back to the default state's first frame, even if it was already playing
            animator.Update(0f);   // pose it now, so it never shows a frame un-animated
        }

        /// <returns>Seconds the current part has been up: its animation's own playback time when it has one,
        /// so the timing matches the frames on screen.</returns>
        private float GetTimeShown(Animator animator) {
            if (!CanAnimate(animator)) return Time.time - _stageStartTime;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            return state.normalizedTime * state.length;
        }

        // normalizedTime counts past 1 once the clip has played through, whether or not it loops
        private bool HasPlayedThrough(Animator animator) {
            if (!CanAnimate(animator)) return Time.time - _stageStartTime >= Part2FallbackDuration;
            return animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
        }

        private static bool CanAnimate(Animator animator) {
            return animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
        }
    }
}
